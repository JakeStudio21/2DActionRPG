// Unity 2020+ compatible. Place this file under an Editor/ folder.
// Usage:
// 1) Open the window: Tools ▶ 8‑Direction Sprite Exporter.
// 2) Drag your character root (Transform) with an Animator.
// 3) Assign a Camera (orthographic recommended). Set output size & FPS.
// 4) Enter animation state names to export (e.g., Idle, Walk, Attack).
// 5) Click "Export PNG Sequences". It will export 8 folders (N, NE, E, SE, S, SW, W, NW) per state.
//    Output files: Assets/Exports/<State>/<Dir>/frame_0001.png
// Optional: Tick "Also Build Sprite Sheets" to auto‑pack each direction to a single row spritesheet.

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations; // for AnimatorController / AnimationMode
using UnityEngine;

public class EightDirectionSpriteExporter : EditorWindow
{
    // -------------------- Inspector Fields --------------------
    [Header("Scene Targets")]
    public Transform subjectRoot;         // Character root to rotate
    public Animator animator;             // Animator on the subject
    public Camera captureCamera;          // Orthographic camera recommended

    [Header("Capture Settings")] 
    public int width = 512;
    public int height = 512;
    public int fps = 12;
    public float secondsPerState = 1.0f;  // clip segment duration to sample
    public bool rotateSubject = true;     // If false, rotates camera around subject
    public Vector3 cameraOrbitOffset = new Vector3(0, 1.5f, 0); // used when rotating camera
    public bool centerOnSubject = true;   // align camera to subject each frame
    public Vector3 cameraLocalPos = new Vector3(0, 2.5f, -5f);
    public Vector3 cameraLocalEuler = new Vector3(20, 0, 0); // slight tilt for quarter‑view

    [Header("Animator")]
    public string[] stateNames = new[] { "Idle", "Walk" };
    public int animatorLayer = 0;

    [Header("Output")]
    public string exportRoot = "Assets/Exports"; // will be created if missing
    public bool alsoBuildSpriteSheets = false;    // pack each direction row into one PNG

    [Header("Lighting/Render Options")]
    public bool clearBackground = true;
    public Color clearColor = new Color(0,0,0,0);
    public bool disableShadows = true; // for clean cutouts

    [Header("Stability Fixes")]
    [Tooltip("Force Animator to update bones even if offscreen.")]
    public bool forceAlwaysAnimate = true;
    [Tooltip("Force SkinnedMeshRenderer to update when offscreen during capture.")]
    public bool forceUpdateWhenOffscreen = true;
    [Tooltip("Use a tiny dt when sampling instead of 0 (some rigs need non-zero).")]
    public bool useNonZeroEval = true;

    [Header("Direct Clip Sampling (Editor-only)")]
    [Tooltip("Bypass Animator and sample AnimationClip directly (fixes rigs where Animator preview/normalized play fails).")]
    public bool forceDirectClipSampling = true;
    [Tooltip("Temporarily disable Animator while direct sampling to avoid conflicts.")]
    public bool disableAnimatorWhileSampling = true;

    // -------------------- Internals --------------------
    private AnimatorCullingMode _oldCullingMode;
    private AnimatorUpdateMode _oldUpdateMode;

    private struct SmrPrev { public SkinnedMeshRenderer smr; public bool prev; public SmrPrev(SkinnedMeshRenderer s, bool p){ smr=s; prev=p; } }
    private readonly List<SmrPrev> _smrPrev = new List<SmrPrev>();

    // Directions (classic arrays for compatibility)
    private static readonly string[] DIR_NAMES = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
    private static readonly float[]  DIR_YAWS  = {   0f,   45f,  90f, 135f, 180f, 225f, 270f, 315f };

    private RenderTexture _rt;
    private Texture2D _tex;

    // -------------------- Menu --------------------
    [MenuItem("Tools/8‑Direction Sprite Exporter")] 
    public static void Open() {
        GetWindow<EightDirectionSpriteExporter>("8‑Dir Exporter");
    }

    // -------------------- GUI --------------------
    private void OnGUI()
    {
        GUILayout.Label("Scene Targets", EditorStyles.boldLabel);
        subjectRoot = (Transform)EditorGUILayout.ObjectField("Subject Root", subjectRoot, typeof(Transform), true);
        animator    = (Animator)EditorGUILayout.ObjectField("Animator", animator, typeof(Animator), true);
        captureCamera = (Camera)EditorGUILayout.ObjectField("Capture Camera", captureCamera, typeof(Camera), true);

        EditorGUILayout.Space();
        GUILayout.Label("Capture Settings", EditorStyles.boldLabel);
        width = EditorGUILayout.IntField("Width", width);
        height = EditorGUILayout.IntField("Height", height);
        fps = Mathf.Clamp(EditorGUILayout.IntField("FPS", fps), 1, 240);
        secondsPerState = Mathf.Max(0.1f, EditorGUILayout.FloatField("Seconds / State", secondsPerState));
        rotateSubject = EditorGUILayout.Toggle("Rotate Subject (Y)", rotateSubject);
        centerOnSubject = EditorGUILayout.Toggle("Center Camera on Subject", centerOnSubject);
        cameraLocalPos = EditorGUILayout.Vector3Field("Camera Local Pos", cameraLocalPos);
        cameraLocalEuler = EditorGUILayout.Vector3Field("Camera Local Euler", cameraLocalEuler);
        cameraOrbitOffset = EditorGUILayout.Vector3Field("Orbit Offset (if rotate cam)", cameraOrbitOffset);

        EditorGUILayout.Space();
        GUILayout.Label("Animator", EditorStyles.boldLabel);
        animatorLayer = EditorGUILayout.IntField("Layer Index", animatorLayer);
        SerializedObject so = new SerializedObject(this);
        SerializedProperty sp = so.FindProperty("stateNames");
        EditorGUILayout.PropertyField(sp, true);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();
        GUILayout.Label("Output", EditorStyles.boldLabel);
        exportRoot = EditorGUILayout.TextField("Export Root", exportRoot);
        alsoBuildSpriteSheets = EditorGUILayout.Toggle("Also Build Sprite Sheets", alsoBuildSpriteSheets);

        EditorGUILayout.Space();
        GUILayout.Label("Lighting/Render", EditorStyles.boldLabel);
        clearBackground = EditorGUILayout.Toggle("Clear Background", clearBackground);
        clearColor = EditorGUILayout.ColorField("Clear Color", clearColor);
        disableShadows = EditorGUILayout.Toggle("Disable Shadows", disableShadows);

        // --- Stability Fixes UI ---
        EditorGUILayout.Space();
        GUILayout.Label("Stability Fixes", EditorStyles.boldLabel);
        forceAlwaysAnimate = EditorGUILayout.Toggle("Force Always Animate", forceAlwaysAnimate);
        forceUpdateWhenOffscreen = EditorGUILayout.Toggle("Force Update When Offscreen", forceUpdateWhenOffscreen);
        useNonZeroEval = EditorGUILayout.Toggle("Use Non-Zero Eval", useNonZeroEval);

        // --- Direct Clip Sampling UI ---
        EditorGUILayout.Space();
        GUILayout.Label("Direct Clip Sampling (Editor-only)", EditorStyles.boldLabel);
        forceDirectClipSampling = EditorGUILayout.Toggle("Force Direct Clip Sampling", forceDirectClipSampling);
        disableAnimatorWhileSampling = EditorGUILayout.Toggle("Disable Animator While Sampling", disableAnimatorWhileSampling);

        EditorGUILayout.Space();
        GUI.enabled = CanExport();
        if (GUILayout.Button("Export PNG Sequences", GUILayout.Height(40)))
        {
            try { ExportAll(); }
            catch (Exception e) { Debug.LogError(e); }
        }
        GUI.enabled = true;

        EditorGUILayout.HelpBox("Tip: Use an Orthographic camera for consistent scale. Ensure your character faces +Z when yaw=0 (N).", MessageType.Info);
    }

    private bool CanExport()
    {
        return subjectRoot && animator && captureCamera && stateNames != null && stateNames.Length > 0 && !Application.isPlaying;
    }

    // -------------------- Camera Prep --------------------
    private void PrepareCamera()
    {
        if (captureCamera.orthographic == false)
            captureCamera.orthographic = true;
        captureCamera.nearClipPlane = -10f;
        captureCamera.farClipPlane = 100f;
        captureCamera.backgroundColor = clearColor;
        captureCamera.clearFlags = clearBackground ? CameraClearFlags.SolidColor : CameraClearFlags.Nothing;

        // Parent camera under subject temporarily for consistent framing
        if (centerOnSubject)
        {
            var tempParent = new GameObject("__CaptureRig").transform;
            tempParent.position = subjectRoot.position;
            tempParent.rotation = Quaternion.identity;
            captureCamera.transform.SetParent(tempParent, true);
            captureCamera.transform.localPosition = cameraLocalPos;
            captureCamera.transform.localEulerAngles = cameraLocalEuler;
        }
    }

    private void RestoreCameraParent()
    {
        if (captureCamera.transform.parent != null && captureCamera.transform.parent.name == "__CaptureRig")
        {
            var rig = captureCamera.transform.parent;
            captureCamera.transform.SetParent(null, true);
            DestroyImmediate(rig.gameObject);
        }
    }

    // -------------------- Export Core --------------------
    private void ExportAll()
    {
        if (!Directory.Exists(exportRoot)) Directory.CreateDirectory(exportRoot);

        // Optionally disable real‑time shadows for clean alpha
        float oldShadowDistance = QualitySettings.shadowDistance;
        if (disableShadows) QualitySettings.shadowDistance = 0f;

        try
        {
            // Stability: force updates so limbs animate even if offscreen
            _oldUpdateMode = animator.updateMode;
            animator.updateMode = AnimatorUpdateMode.Normal;
            if (forceAlwaysAnimate)
            {
                _oldCullingMode = animator.cullingMode;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            if (forceUpdateWhenOffscreen && subjectRoot)
            {
                _smrPrev.Clear();
                var smrs = subjectRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var s in smrs)
                {
                    _smrPrev.Add(new SmrPrev(s, s.updateWhenOffscreen));
                    s.updateWhenOffscreen = true;
                }
            }

            PrepareCamera();
            AllocateRT();

            for (int i = 0; i < stateNames.Length; i++)
            {
                string state = stateNames[i];
                if (string.IsNullOrWhiteSpace(state)) continue;
                ExportState(state.Trim());
            }
        }
        finally
        {
            ReleaseRT();
            RestoreCameraParent();

            // restore stability toggles
            if (forceAlwaysAnimate)
                animator.cullingMode = _oldCullingMode;
            if (forceUpdateWhenOffscreen)
            {
                for (int i = 0; i < _smrPrev.Count; i++)
                {
                    var p = _smrPrev[i];
                    if (p.smr) p.smr.updateWhenOffscreen = p.prev;
                }
                _smrPrev.Clear();
            }
            animator.updateMode = _oldUpdateMode;

            QualitySettings.shadowDistance = oldShadowDistance;
            AssetDatabase.Refresh();
            Debug.Log("8‑Direction export completed.");
        }
    }

    private void ExportState(string stateName)
    {
        int frameCount = Mathf.CeilToInt(secondsPerState * fps);
        string stateRoot = Path.Combine(exportRoot, San(stateName));
        Directory.CreateDirectory(stateRoot);

        Quaternion originalRot = subjectRoot.rotation;

        for (int d = 0; d < DIR_NAMES.Length; d++)
        {
            string dirName = DIR_NAMES[d];
            float yaw = DIR_YAWS[d];

            string dirFolder = Path.Combine(stateRoot, dirName);
            if (!Directory.Exists(dirFolder)) Directory.CreateDirectory(dirFolder);

            // Rotate subject or camera
            if (rotateSubject)
            {
                subjectRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
            }
            else
            {
                // maintain subject rotation, orbit camera rig
                if (captureCamera.transform.parent != null && captureCamera.transform.parent.name == "__CaptureRig")
                {
                    Transform rig = captureCamera.transform.parent;
                    rig.position = subjectRoot.position + cameraOrbitOffset;
                    rig.rotation = Quaternion.Euler(cameraLocalEuler.x, yaw, cameraLocalEuler.z);
                }
            }

            // Reset pose before sampling (avoid pose residue)
            animator.Rebind();
            animator.Update(useNonZeroEval ? 1f/60f : 0f);

            // Prefer direct clip sampling (robust). Fallback to Animator when clip can't be resolved.
            AnimationClip clip = forceDirectClipSampling ? FindClipForState(stateName, animatorLayer) : null;
            if (clip != null && forceDirectClipSampling)
            {
                SampleClipDirect(clip, frameCount, dirFolder);
            }
            else
            {
                SampleWithAnimator(stateName, dirFolder, frameCount);
            }

            if (alsoBuildSpriteSheets)
            {
                string sheetPath = Path.Combine(stateRoot, San(stateName) + "_" + dirName + "_sheet.png");
                BuildRowSpriteSheet(dirFolder, sheetPath, frameCount);
            }
        }

        subjectRoot.rotation = originalRot;
    }

    // -------------------- RT / IO --------------------
    private void AllocateRT()
    {
        _rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
        _tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        captureCamera.targetTexture = _rt;
    }

    private void ReleaseRT()
    {
        if (_rt != null)
        {
            captureCamera.targetTexture = null;
            _rt.Release();
            DestroyImmediate(_rt);
            _rt = null;
        }
        if (_tex != null)
        {
            DestroyImmediate(_tex);
            _tex = null;
        }
    }

    private void CapturePNG(string path)
    {
        RenderTexture.active = _rt;
        captureCamera.Render();
        _tex.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
        _tex.Apply(false, false);
        byte[] png = _tex.EncodeToPNG();
        File.WriteAllBytes(path, png);
    }

    private void BuildRowSpriteSheet(string framesFolder, string outPath, int frameCount)
    {
        var files = Enumerable.Range(1, frameCount)
            .Select(i => Path.Combine(framesFolder, "frame_" + i.ToString("0000") + ".png"))
            .Where(File.Exists).ToList();
        if (files.Count == 0) return;

        var sheet = new Texture2D(width * files.Count, height, TextureFormat.RGBA32, false);
        int x = 0;
        for (int i = 0; i < files.Count; i++)
        {
            string fp = files[i];
            byte[] bytes = File.ReadAllBytes(fp);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes, false);
            sheet.SetPixels(x, 0, width, height, tex.GetPixels());
            x += width;
            UnityEngine.Object.DestroyImmediate(tex);
        }
        sheet.Apply(false, false);
        File.WriteAllBytes(outPath, sheet.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(sheet);
    }

    // -------------------- Sampling Helpers --------------------
    // Robust path: sample the AnimationClip directly using AnimationMode (Editor-only)
    private void SampleClipDirect(AnimationClip clip, int frameCount, string dirFolder)
    {
        bool stopAnimMode = false;
        if (!AnimationMode.InAnimationMode()) { AnimationMode.StartAnimationMode(); stopAnimMode = true; }
        bool prevEnabled = animator.enabled;
        if (disableAnimatorWhileSampling) animator.enabled = false;

        for (int f = 0; f < frameCount; f++)
        {
            float norm = frameCount <= 1 ? 0f : (float)f / (frameCount - 1);
            float t = Mathf.Clamp01(norm) * Mathf.Max(clip.length, 0.0001f);

            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(animator.gameObject, clip, t);
            AnimationMode.EndSampling();

            string file = Path.Combine(dirFolder, "frame_" + (f + 1).ToString("0000") + ".png");
            CapturePNG(file);
        }

        if (disableAnimatorWhileSampling) animator.enabled = prevEnabled;
        if (stopAnimMode) AnimationMode.StopAnimationMode();
    }

    // Fallback path: use Animator normalized-time sampling
    private void SampleWithAnimator(string stateName, string dirFolder, int frameCount)
    {
        int stateHash = Animator.StringToHash(stateName);
        animator.speed = 1f; // let Animator evaluate; we'll jump time explicitly

        // Ensure layer is on the desired state at t=0
        animator.Play(stateHash, animatorLayer, 0f);
        animator.Update(0f); // apply immediately

        for (int f = 0; f < frameCount; f++)
        {
            float normTime = frameCount <= 1 ? 0f : (float)f / (frameCount - 1);
            // Jump to exact time and force-evaluate
            animator.Play(stateHash, animatorLayer, normTime);
            animator.Update(0f);
            animator.Update(useNonZeroEval ? 1f/120f : 0f); // tiny step helps some rigs

            string file = Path.Combine(dirFolder, "frame_" + (f + 1).ToString("0000") + ".png");
            CapturePNG(file);
        }
    }

    // Resolve a state name to its bound AnimationClip (null for blend trees)
    private AnimationClip FindClipForState(string stateName, int layer)
    {
        var ac = animator.runtimeAnimatorController as AnimatorController;
        if (ac == null) return null;
        if (layer < 0 || layer >= ac.layers.Length) return null;
        var sm = ac.layers[layer].stateMachine;
        return FindClipRecursive(sm, stateName);
    }

    private AnimationClip FindClipRecursive(AnimatorStateMachine sm, string stateName)
    {
        foreach (var st in sm.states)
        {
            if (st.state != null && st.state.name == stateName)
            {
                Motion m = st.state.motion;
                AnimationClip c = m as AnimationClip;
                if (c != null) return c; // direct clip
                return null;             // blend tree or other motion
            }
        }
        foreach (var cs in sm.stateMachines)
        {
            AnimationClip c = FindClipRecursive(cs.stateMachine, stateName);
            if (c != null) return c;
        }
        return null;
    }

    // -------------------- Utils --------------------
    private static string San(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Trim();
    }
}
