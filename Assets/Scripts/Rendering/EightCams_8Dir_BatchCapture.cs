using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EightCams_8Dir_BatchCapture : MonoBehaviour
{
    [Header("Target")]
    public Animator animator;            // 캐릭터 Animator
    public Transform subjectRoot;        // (선택) 캐릭터 루트. 끝난 뒤 복구에 사용

    [Header("Cameras (8방향, 순서 고정)")]
    // 순서: N, NE, E, SE, S, SW, W, NW
    public Camera[] cams = new Camera[8];

    [Header("Animation (여러 상태)")]
    public string[] stateNames = { "Idle", "Walk", "Death" };
    public int animatorLayer = 0;
    public float secondsPerState = 1.0f;   // 기본 상태 길이(초)
    public int fps = 12;                   // 프레임/초
    public bool excludeLastFrameLoop = true;

    [System.Serializable]
    public struct StateFramesOverride { public string state; public int frames; }
    [Header("Per-State Frames (Overrides)")]
    [Tooltip("여기에 적은 상태는 secondsPerState*fps 대신 지정 프레임 수를 사용")]
    public List<StateFramesOverride> stateFrameOverrides = new List<StateFramesOverride>();

    [Header("Root Motion per State")]
    [Tooltip("여기에 적은 상태(예: Death)는 dt로 시간을 진행하여 루트모션을 누적 캡처")]
    public string[] rootMotionStates = new string[] { /* "Death" */ };

    [Header("Capture Size")]
    public int width = 512;
    public int height = 512;

    [Header("Output")]
    public string exportRoot = "Exports8Cam"; // Assets/Exports8Cam/<State>/<Dir>/

    [Header("Run Options")]
    public bool autoStartOnPlay = true;       // ▶ 누르면 자동 시작
    public bool freezeAnimatorOnAwake = true; // 플레이 진입 시 선재생 방지
    public bool restorePoseAfterEachState = true; // 상태별 캡처 끝나면 포즈 복구

    [Header("Sprite Sheet (옵션)")]
    public bool buildRowSpriteSheets = false; // 방향별 1행 시트
    public bool build8xNGridPerState = true;  // 8행×N열 시트
    public string spriteSheetSuffix = "_sheet";

    // 내부
    readonly string[] _dirNames = { "N","NE","E","SE","S","SW","W","NW" };
    RenderTexture[] _rts;
    Texture2D _readTex;
    bool _animatorPrevEnabled;
    bool _prevApplyRootMotion;

    // 포즈 저장용
    Vector3 savedPos, savedLocalPos;
    Quaternion savedRot, savedLocalRot;
    Vector3 savedAnimPos, savedAnimLocalPos;
    Quaternion savedAnimRot, savedAnimLocalRot;

    void Awake()
    {
        if (!animator)
        {
            Debug.LogError("[EightCams] Animator 지정 필요");
            return;
        }
        // 선재생 방지: 플레이 들어가며 자동으로 눕거나 걷는 걸 막음
        _animatorPrevEnabled = animator.enabled;
        if (freezeAnimatorOnAwake) animator.enabled = false;

        // 시작 포즈 저장
        if (subjectRoot)
        {
            savedPos = subjectRoot.position;
            savedRot = subjectRoot.rotation;
            savedLocalPos = subjectRoot.localPosition;
            savedLocalRot = subjectRoot.localRotation;
        }
        savedAnimPos = animator.transform.position;
        savedAnimRot = animator.transform.rotation;
        savedAnimLocalPos = animator.transform.localPosition;
        savedAnimLocalRot = animator.transform.localRotation;
    }

    void Start()
    {
        if (autoStartOnPlay) StartCapture();
    }

    [ContextMenu("Start Capture")]
    public void StartCapture() { StartCoroutine(CaptureRoutine()); }

    IEnumerator CaptureRoutine()
    {
        // 기본 체크
        if (animator == null || cams == null || cams.Length != 8)
        {
            Debug.LogError("[EightCams] Animator 또는 8개 카메라 배열 확인");
            yield break;
        }

        // 출력 폴더 준비
        string root = Path.Combine(Application.dataPath, exportRoot);
        Directory.CreateDirectory(root);

        // 카메라 공통 세팅
        foreach (var cam in cams)
        {
            if (!cam) { Debug.LogError("[EightCams] 카메라 누락"); yield break; }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0,0,0,0);
        }

        // RT 준비
        _rts = new RenderTexture[8];
        for (int i = 0; i < 8; i++)
            _rts[i] = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        _readTex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // Animator 활성화 및 RM 보관
        animator.enabled = true;
        _prevApplyRootMotion = animator.applyRootMotion;

        // 상태들 캡처
        foreach (var raw in stateNames)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            string state = raw.Trim();
            string statePath = Path.Combine(root, San(state));
            Directory.CreateDirectory(statePath);

            // 방향 폴더 미리
            string[] dirPaths = new string[8];
            for (int i = 0; i < 8; i++)
            {
                dirPaths[i] = Path.Combine(statePath, _dirNames[i]);
                Directory.CreateDirectory(dirPaths[i]);
            }

            // 프레임 수 계산
            int frames = FramesForState(state);
            if (excludeLastFrameLoop && frames > 1) frames -= 1;
            frames = Mathf.Max(frames, 1);

            // 루트모션 상태 여부
            bool wantRM = IsInList(state, rootMotionStates);
            animator.applyRootMotion = wantRM;

            // 포즈 원점으로 리셋
            RestorePoseImmediate();

            // 상태 0초에서 시작
            int stateHash = Animator.StringToHash(state);
            animator.Rebind(); animator.Update(0f);
            animator.Play(stateHash, animatorLayer, 0f);
            animator.Update(0f);

            // 진행 방식 선택
            float dt = 1f / Mathf.Max(1, fps);

            for (int f = 0; f < frames; f++)
            {
                if (wantRM)
                {
                    if (f > 0) animator.Update(dt); // 루트모션 누적
                }
                else
                {
                    float norm = (frames <= 1) ? 0f : (float)f / (frames - 1);
                    animator.Play(stateHash, animatorLayer, norm);
                    animator.Update(0f); // 제자리 샘플
                }

                // 한 프레임 대기 후 8카메라 동시 캡처
                yield return new WaitForEndOfFrame();

                for (int i = 0; i < 8; i++)
                {
                    var cam = cams[i];
                    cam.targetTexture = _rts[i];
                    RenderTexture.active = _rts[i];
                    cam.Render();

                    _readTex.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                    _readTex.Apply(false, false);

                    File.WriteAllBytes(
                        Path.Combine(dirPaths[i], $"frame_{(f+1).ToString("0000")}.png"),
                        _readTex.EncodeToPNG()
                    );
                    cam.targetTexture = null;
                }
                RenderTexture.active = null;
            }

            // 시트 만들기(선택)
            if (buildRowSpriteSheets)
            {
                for (int i = 0; i < 8; i++)
                {
                    string sheetPath = Path.Combine(statePath, $"{San(state)}_{_dirNames[i]}{spriteSheetSuffix}.png");
                    BuildRowSpriteSheet(dirPaths[i], sheetPath, frames);
                }
            }
            if (build8xNGridPerState)
            {
                string gridPath = Path.Combine(statePath, $"{San(state)}_8x{frames}{spriteSheetSuffix}.png");
                Build8xNGrid(statePath, frames);
            }

            // 상태 끝 → 복구(선택)
            if (restorePoseAfterEachState) RestorePoseAndRebind();
        }

        // 정리
        for (int i = 0; i < 8; i++)
        {
            if (_rts[i] != null) { _rts[i].Release(); Destroy(_rts[i]); }
        }
        Destroy(_readTex);
        animator.applyRootMotion = _prevApplyRootMotion;
        animator.enabled = _animatorPrevEnabled;

        Debug.Log($"[EightCams] Done → {root}");
    }

    // ============ Helpers ============
    int FramesForState(string state)
    {
        // Override 우선
        for (int i = 0; i < stateFrameOverrides.Count; i++)
        {
            var o = stateFrameOverrides[i];
            if (!string.IsNullOrEmpty(o.state) &&
                string.Equals(o.state.Trim(), state, System.StringComparison.OrdinalIgnoreCase) &&
                o.frames > 0)
                return o.frames;
        }
        // 기본값
        int frames = Mathf.CeilToInt(secondsPerState * fps);
        return Mathf.Max(frames, 1);
    }

    bool IsInList(string value, string[] list)
    {
        if (list == null || list.Length == 0) return false;
        for (int i = 0; i < list.Length; i++)
        {
            var it = list[i];
            if (!string.IsNullOrWhiteSpace(it) &&
                string.Equals(it.Trim(), value, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    void RestorePoseImmediate()
    {
        // 루트모션이 즉시 또 밀지 않게 잠깐 끄기
        bool tmp = animator.applyRootMotion;
        animator.applyRootMotion = false;

        if (subjectRoot)
        {
            subjectRoot.localPosition = savedLocalPos;
            subjectRoot.localRotation = savedLocalRot;
            subjectRoot.position      = savedPos;
            subjectRoot.rotation      = savedRot;
        }
        animator.transform.localPosition = savedAnimLocalPos;
        animator.transform.localRotation = savedAnimLocalRot;
        animator.transform.position      = savedAnimPos;
        animator.transform.rotation      = savedAnimRot;

        animator.applyRootMotion = tmp;
    }

    void RestorePoseAndRebind()
    {
        bool tmp = animator.applyRootMotion;
        animator.applyRootMotion = false;

        if (subjectRoot)
        {
            subjectRoot.localPosition = savedLocalPos;
            subjectRoot.localRotation = savedLocalRot;
            subjectRoot.position      = savedPos;
            subjectRoot.rotation      = savedRot;
        }
        animator.transform.localPosition = savedAnimLocalPos;
        animator.transform.localRotation = savedAnimLocalRot;
        animator.transform.position      = savedAnimPos;
        animator.transform.rotation      = savedAnimRot;

        animator.Rebind();
        animator.Update(0f);

        animator.applyRootMotion = tmp;
    }

    void BuildRowSpriteSheet(string framesFolder, string outPath, int frames)
    {
        var files = new List<string>();
        for (int i = 1; i <= frames; i++)
        {
            string fp = Path.Combine(framesFolder, "frame_" + i.ToString("0000") + ".png");
            if (File.Exists(fp)) files.Add(fp);
        }
        if (files.Count == 0) return;

        var sheet = new Texture2D(width * files.Count, height, TextureFormat.RGBA32, false);
        int x = 0;
        for (int i = 0; i < files.Count; i++)
        {
            byte[] bytes = File.ReadAllBytes(files[i]);
            var texIn = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texIn.LoadImage(bytes, false);
            sheet.SetPixels(x, 0, width, height, texIn.GetPixels());
            x += width;
            Destroy(texIn);
        }
        sheet.Apply(false, false);
        File.WriteAllBytes(outPath, sheet.EncodeToPNG());
        Destroy(sheet);
    }

    void Build8xNGrid(string stateRootPath, int frames)
    {
        int cols = frames;
        int rows = 8;
        var sheet = new Texture2D(width * cols, height * rows, TextureFormat.RGBA32, false);
        var clear = new Color[width * height];
        for (int i = 0; i < clear.Length; i++) clear[i] = new Color(0,0,0,0);

        for (int r = 0; r < rows; r++)
        {
            string dirFolder = Path.Combine(stateRootPath, _dirNames[r]);
            for (int c = 0; c < cols; c++)
            {
                string fp = Path.Combine(dirFolder, "frame_" + (c + 1).ToString("0000") + ".png");
                int x = c * width;
                int y = (rows - 1 - r) * height;
                if (File.Exists(fp))
                {
                    byte[] bytes = File.ReadAllBytes(fp);
                    var texIn = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    texIn.LoadImage(bytes, false);
                    sheet.SetPixels(x, y, width, height, texIn.GetPixels());
                    Destroy(texIn);
                }
                else
                {
                    sheet.SetPixels(x, y, width, height, clear);
                }
            }
        }
        sheet.Apply(false, false);
        string outPath = Path.Combine(stateRootPath, $"_8x{frames}{spriteSheetSuffix}.png");
        File.WriteAllBytes(outPath, sheet.EncodeToPNG());
        Destroy(sheet);
    }

    static string San(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Trim();
    }
}
