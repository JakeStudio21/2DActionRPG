// Assets/Editor/DirectionalSpriteBaker.cs
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Animations; // AnimatorController 생성용

public class DirectionalSpriteBaker : EditorWindow
{
    // ===== UI 설정 =====
    [Header("Targets")]
    public Animator animator;          // 캐릭터 Animator (씬 오브젝트)
    public Transform dirPivot;         // 8방향 회전용 Pivot(캐릭터 부모)
    public Transform followTarget;     // 카메라가 따라갈 대상(보통 캐릭터 루트)
    public Camera captureCamera;       // Orthographic 카메라

    [Header("Clips & Output")]
    public List<AnimationClip> clips = new List<AnimationClip>();
    public string outputRoot = "Assets/BakeOutput";
    public string filePrefix = "CHAR"; // 예: 캐릭터명

    [Header("Capture Settings")]
    public int width = 512;
    public int height = 512;
    public int fps = 30;
    public bool includeAlpha = true;

    // 방향 각도 (원하면 수정 가능)
    public float[] eightDirAngles = new float[] { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

    public enum FollowMode { None, PositionOnly }
    public FollowMode followMode = FollowMode.PositionOnly;
    public Vector3 cameraOffset = new Vector3(0, 5, -10f); // 3D면 사용, 2D 정면이면 (0,0,-10)

    [Header("Animation Options")]
    public bool applyRootMotion = true;  // 회전/위치 루트모션을 살릴지
    public bool loopLastFrame = false;   // 마지막 프레임 중복 캡처 방지용
    public bool resetPoseBeforeClip = true;

    [MenuItem("Tools/Animation/Directional Sprite Baker")]
    static void Open()
    {
        GetWindow<DirectionalSpriteBaker>("Directional Sprite Baker").Show();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Targets", EditorStyles.boldLabel);
        animator = (Animator)EditorGUILayout.ObjectField("Animator", animator, typeof(Animator), true);
        dirPivot = (Transform)EditorGUILayout.ObjectField("Dir Pivot", dirPivot, typeof(Transform), true);
        followTarget = (Transform)EditorGUILayout.ObjectField("Follow Target", followTarget, typeof(Transform), true);
        captureCamera = (Camera)EditorGUILayout.ObjectField("Capture Camera", captureCamera, typeof(Camera), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Clips & Output", EditorStyles.boldLabel);
        SerializedObject so = new SerializedObject(this);
        SerializedProperty sp = so.FindProperty("clips");
        EditorGUILayout.PropertyField(sp, new GUIContent("Clips"), true);
        so.ApplyModifiedProperties();

        outputRoot = EditorGUILayout.TextField("Output Root", outputRoot);
        filePrefix = EditorGUILayout.TextField("File Prefix", filePrefix);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Capture Settings", EditorStyles.boldLabel);
        width = EditorGUILayout.IntField("Width", width);
        height = EditorGUILayout.IntField("Height", height);
        fps = EditorGUILayout.IntField("FPS", fps);
        includeAlpha = EditorGUILayout.Toggle("Include Alpha", includeAlpha);

        followMode = (FollowMode)EditorGUILayout.EnumPopup("Follow Mode", followMode);
        cameraOffset = EditorGUILayout.Vector3Field("Camera Offset", cameraOffset);

        applyRootMotion = EditorGUILayout.Toggle("Animator ApplyRootMotion", applyRootMotion);
        loopLastFrame = EditorGUILayout.Toggle("Loop Last Frame", loopLastFrame);
        resetPoseBeforeClip = EditorGUILayout.Toggle("Reset Pose Before Clip", resetPoseBeforeClip);

        EditorGUILayout.Space();
        if (GUILayout.Button("Bake 8 Directions"))
        {
            if (ValidateInputs())
            {
                BakeAll();
            }
        }

        EditorGUILayout.HelpBox(
            "TIP\n" +
            "- 카메라는 Orthographic 권장(픽셀 정확도, 왜곡 최소화)\n" +
            "- 배경 투명: Camera Clear Flags = Solid Color, Background A=0\n" +
            "- 휠윈드 등 회전 포함 클립에서 Follow Mode = PositionOnly 사용\n" +
            "- DIR_PIVOT은 8방향을 위해 Y 회전만 바꿉니다.",
            MessageType.Info);
    }

    bool ValidateInputs()
    {
        if (!animator)
        {
            EditorUtility.DisplayDialog("Error", "Animator가 필요합니다.", "OK");
            return false;
        }
        if (!dirPivot)
        {
            EditorUtility.DisplayDialog("Error", "Dir Pivot이 필요합니다.", "OK");
            return false;
        }
        if (!followTarget)
        {
            EditorUtility.DisplayDialog("Error", "Follow Target이 필요합니다.", "OK");
            return false;
        }
        if (!captureCamera)
        {
            EditorUtility.DisplayDialog("Error", "Capture Camera가 필요합니다.", "OK");
            return false;
        }
        if (clips == null || clips.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "클립을 1개 이상 지정하세요.", "OK");
            return false;
        }
        return true;
    }

    void BakeAll()
    {
        // 카메라 설정 점검
        if (captureCamera.orthographic == false)
        {
            if (EditorUtility.DisplayDialog("Camera Warning",
                "카메라가 Perspective입니다. 2D 스프라이트용이면 Orthographic이 권장됩니다.\n계속할까요?",
                "Proceed", "Cancel") == false)
                return;
        }

        // 출력 폴더 준비
        if (!AssetDatabase.IsValidFolder(outputRoot))
        {
            Directory.CreateDirectory(outputRoot);
            AssetDatabase.Refresh();
        }

        // 런타임 컨트롤러(단일 스테이트) 임시 생성
        var controller = new AnimatorController();
        controller.AddLayer("Base");
        var layer = controller.layers[0];
        string stateName = "BakeState";

        // 원래 컨트롤러 백업
        RuntimeAnimatorController originalController = animator.runtimeAnimatorController;
        bool originalApplyRoot = animator.applyRootMotion;

        try
        {
            animator.applyRootMotion = applyRootMotion;

            foreach (var clip in clips)
            {
                if (!clip) continue;

                // 스테이트 갱신
                var sm = layer.stateMachine;
                sm.states = new ChildAnimatorState[0]; // 초기화
                var st = sm.AddState(stateName);
                st.motion = clip;

                animator.runtimeAnimatorController = controller;

                // 클립별 폴더
                string clipFolder = Path.Combine(outputRoot, Sanitize($"{filePrefix}_{clip.name}"));
                Directory.CreateDirectory(clipFolder);

                // 각 방향
                foreach (var angle in eightDirAngles)
                {
                    // 방향 폴더
                    string dirFolder = Path.Combine(clipFolder, $"{Mathf.RoundToInt(angle)}");
                    Directory.CreateDirectory(dirFolder);

                    // 방향 회전
                    dirPivot.localRotation = Quaternion.Euler(0f, angle, 0f);

                    // 초기화
                    if (resetPoseBeforeClip)
                    {
                        animator.Play(stateName, 0, 0f);
                        animator.Update(0f);
                    }

                    float dt = 1f / Mathf.Max(1, fps);
                    float length = Mathf.Max(clip.length, dt);
                    int totalFrames = Mathf.CeilToInt(length / dt);
                    if (!loopLastFrame) totalFrames = Mathf.Max(1, totalFrames - 1);

                    // 프레임 루프
                    float t = 0f;
                    for (int i = 0; i < totalFrames; i++)
                    {
                        // 시간 진행
                        animator.Play(stateName, 0, t / clip.length);
                        animator.Update(0f); // 샘플 반영

                        // 카메라 따라가기
                        if (followMode == FollowMode.PositionOnly && followTarget)
                        {
                            // Orthographic 카메라면 주로 Z만 오프셋. 3D면 XYZ 모두 사용
                            var p = followTarget.position + cameraOffset;
                            captureCamera.transform.position = p;
                            if (!captureCamera.orthographic)
                                captureCamera.transform.LookAt(followTarget.position);
                        }

                        // 렌더 → PNG 저장
                        CaptureFrameToPNG(captureCamera, Path.Combine(dirFolder, $"{clip.name}_{i:D4}.png"));

                        t += dt;
                    }
                }
            }
        }
        finally
        {
            // 복구
            animator.runtimeAnimatorController = originalController;
            animator.applyRootMotion = originalApplyRoot;
            DestroyImmediate(controller);
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog("Bake Complete", "8방향 PNG 시퀀스 출력이 완료되었습니다.", "OK");
    }

    static string Sanitize(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c.ToString(), "_");
        return s;
    }

    void CaptureFrameToPNG(Camera cam, string path)
    {
        // 투명 배경을 위해 SolidColor + A=0 권장
        var prevRT = RenderTexture.active;
        var rt = new RenderTexture(width, height, 24,
            includeAlpha ? RenderTextureFormat.ARGB32 : RenderTextureFormat.RGB565);
        cam.targetTexture = rt;

        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, includeAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        File.WriteAllBytes(path, png);

        cam.targetTexture = null;
        RenderTexture.active = prevRT;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }
}
