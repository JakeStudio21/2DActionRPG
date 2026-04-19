using System.Collections;
using System.IO;
using UnityEngine;

// 실행 순서를 가장 늦게(다른 LateUpdate 이후) 보장
[DefaultExecutionOrder(10000)]
public class AnimationRenderer8Direction_Rotate : MonoBehaviour
{
    [Header("Targets")]
    public Transform subjectRoot;      // ★ 캐릭터 부모 Empty (여기에 45° 보정 적용)
    public Animator animator;          // 실제 움직이는 Animator
    public Transform followTarget;     // 실제 '움직이는' Transform (보통 animator.transform)
    public Transform pivot;            // 8대 카메라의 부모(위치만 따라감, 회전 건드리지 않음)

    [Header("Cameras (Pivot 자식, N,NE,E,SE,S,SW,W,NW 순서)")]
    public Camera[] cams = new Camera[8];
    public bool autoFindByName = true;
    private readonly string[] dirNames = { "N","NE","E","SE","S","SW","W","NW" };

    [Header("Yaw Fix (on CHARACTER)")]
    [Tooltip("SubjectRoot에 한 번만 줄 Y축 보정(+45 또는 -45)")]
    public float yawFixOnSubject = 45f;
    public bool applyYawFixOnStart = true;

    [Header("Capture")]
    public string stateName = "Attack_Forward";
    public int fps = 12;
    public int frames = 36;
    public bool excludeLastFrameLoop = true;

    [Header("Output")]
    public int width = 256;
    public int height = 256;
    public string exportRoot = "Exports_MoveFollow";

    [Header("Follow")]
    public Vector3 pivotOffset = Vector3.zero; // 필요 시 화면 중앙 보정
    public bool forceAttachCameras = true;     // 문제 있으면 카메라를 피벗 무시하고 강제로 붙임(안전장치)

    [Header("Shadow System (Light-based)")]
    public bool enableShadow = true;
    [Tooltip("바닥 Plane (Shadow Catcher 역할)")]
    public GameObject groundPlane;
    [Tooltip("그림자 생성용 Directional Light")]
    public Light shadowLight;
    [Tooltip("바닥 높이 (Plane Y 위치)")]
    public float groundHeight = 0f;
    [Tooltip("그림자 색상 및 투명도")]
    public Color shadowColor = new Color(0f, 0f, 0f, 0.5f);
    [Tooltip("Shadow Catcher Material (없으면 자동 생성)")]
    public Material shadowCatcherMaterial;

    [Header("Outline (Character Clone)")]
    public bool enableOutline = true;
    [Tooltip("아웃라인을 적용할 대상 (보통 Animator와 동일)")]
    public Transform outlineSourceRoot;
    [Range(1.0f, 1.2f)]
    [Tooltip("아웃라인 두께 (캐릭터 스케일 배율)")]
    public float outlineThickness = 1.03f;     // 3% 크게
    public Color outlineColor = new Color(0f, 0f, 0f, 1f); // 검은색 불투명
    [Tooltip("아웃라인 전용 머티리얼 (없으면 자동 생성)")]
    public Material outlineMaterial;

    // 내부
    private RenderTexture[] rts;
    private Texture2D readTex;
    private Quaternion subjectRootOrigRot;
    private Vector3[] camOffsets;
    private Quaternion[] camRotations;
    private GameObject outlineInstance; // 복제된 아웃라인 루트
    private Animator outlineAnimator;   // 아웃라인의 Animator (원본과 동기화용)

    void Awake()
    {
        // ===== 필수 체크 =====
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!followTarget && animator) followTarget = animator.transform;
        if (!subjectRoot) { Debug.LogError("[AnimationRenderer8Direction_Rotate] subjectRoot(부모 Empty) 미지정"); enabled = false; return; }
        if (!pivot)       { Debug.LogError("[AnimationRenderer8Direction_Rotate] pivot 미지정"); enabled = false; return; }

        // 카메라 찾기/세팅
        if (autoFindByName)
        {
            cams = new Camera[8];
            for (int i = 0; i < 8; i++)
            {
                var t = pivot.Find(dirNames[i]);
                if (!t) { Debug.LogError($"[AnimationRenderer8Direction_Rotate] Pivot 하위 '{dirNames[i]}' 없음"); enabled = false; return; }
                var cam = t.GetComponentInChildren<Camera>();
                if (!cam) { Debug.LogError($"[AnimationRenderer8Direction_Rotate] '{dirNames[i]}'에 Camera 없음"); enabled = false; return; }
                cams[i] = cam;
            }
        }
        foreach (var cam in cams)
        {
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
        }

        // 오프스크린 스키닝 안전
        if (animator)
        {
            foreach (var s in animator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                s.updateWhenOffscreen = true;
        }

        // 카메라 오프셋/회전 백업(강제붙임용)
        camOffsets   = new Vector3[8];
        camRotations = new Quaternion[8];
        for (int i = 0; i < 8; i++)
        {
            camOffsets[i]   = cams[i].transform.position - pivot.position; // 피벗 기준 초기 오프셋
            camRotations[i] = cams[i].transform.rotation;                  // 초기 회전 유지
        }

        // ★ 캐릭터 부모에 45° 적용 (한 번만)
        subjectRootOrigRot = subjectRoot.rotation;
        if (applyYawFixOnStart)
            subjectRoot.rotation = Quaternion.Euler(0f, yawFixOnSubject, 0f) * subjectRootOrigRot;

        // ★ Shadow Catcher 시스템 초기화
        if (enableShadow)
        {
            InitializeShadowCatcher();
        }

        // ★ 캐릭터 복제 아웃라인 생성
        if (enableOutline)
        {
            Transform sourceRoot = outlineSourceRoot ? outlineSourceRoot : (animator ? animator.transform : null);
            if (sourceRoot != null)
            {
                CreateCharacterOutline(sourceRoot);
            }
            else
            {
                Debug.LogWarning("[AnimationRenderer8Direction_Rotate] outlineSourceRoot가 지정되지 않아 아웃라인을 생성할 수 없습니다.");
            }
        }
    }

    void LateUpdate()
    {
        if (!followTarget || !pivot) return;

        // 루트모션 적용 끝난 뒤, Pivot은 "위치만" 따라감
        pivot.position = followTarget.position + pivotOffset;

        // 안전장치: 필요 시 카메라를 Pivot에 강제로 붙임(다른 스크립트가 건드려도 이김)
        if (forceAttachCameras)
        {
            for (int i = 0; i < 8; i++)
            {
                var t = cams[i].transform;
                t.position = pivot.position + camOffsets[i];
                t.rotation = camRotations[i]; // 카메라 각도는 에디터에서 정한 그대로 유지
            }
        }

        // ★ Shadow Light 위치 업데이트 (캐릭터 위에서 비추도록)
        if (enableShadow && shadowLight != null && followTarget != null)
        {
            // Light를 캐릭터 바로 위에 배치 (위에서 아래로)
            Vector3 lightPos = followTarget.position + new Vector3(0, 10f, 0);
            shadowLight.transform.position = lightPos;
            shadowLight.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // 위에서 아래로
        }

        // ★ 아웃라인 애니메이션 동기화 (Apply Root Motion으로 자동 동기화)
        if (outlineInstance != null && outlineAnimator != null && animator != null)
        {
            // Transform 동기화는 Apply Root Motion으로 자동 처리됨
            // Animator 파라미터 동기화만 수행
            outlineAnimator.speed = animator.speed;
            outlineAnimator.SetFloat("speed", animator.GetFloat("speed"));
            // 필요하면 다른 파라미터도 동기화
            
            // ★ 쉐이더 파라미터 실시간 업데이트 (Inspector 값 변경 반영)
            if (outlineMaterial != null && outlineMaterial.HasProperty("_OutlineWidth"))
            {
                float shaderThickness = (outlineThickness - 1.0f) * 0.3f; // 1.03 → 0.009
                outlineMaterial.SetFloat("_OutlineWidth", shaderThickness);
            }
        }
    }

    void Start() { StartCoroutine(CaptureRoutine()); }

    IEnumerator CaptureRoutine()
    {
        if (!animator || followTarget == null) yield break;

        // 출력 폴더
        string root = Path.IsPathRooted(exportRoot) ? exportRoot : Path.Combine(Application.dataPath, exportRoot);
        Directory.CreateDirectory(root);
        string statePath = Path.Combine(root, San(stateName));
        Directory.CreateDirectory(statePath);

        string[] dirNames = { "N","NE","E","SE","S","SW","W","NW" };
        string[] dirPaths = new string[8];
        for (int i = 0; i < 8; i++)
        {
            dirPaths[i] = Path.Combine(statePath, dirNames[i]);
            Directory.CreateDirectory(dirPaths[i]);
        }

        // 버퍼
        rts = new RenderTexture[8];
        for (int i = 0; i < 8; i++) rts[i] = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        readTex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // Animator 고정
        animator.cullingMode   = AnimatorCullingMode.AlwaysAnimate;
        animator.applyRootMotion = true;
        animator.speed         = 1f;
        animator.updateMode    = AnimatorUpdateMode.Normal;

        // 프레임 고정
        int fCount = Mathf.Max(frames, 1);
        if (excludeLastFrameLoop && fCount > 1) fCount -= 1;
        int prevCap = Time.captureFramerate;
        Time.captureFramerate = fps;

        // 상태 시작
        animator.Rebind(); animator.Update(0f);
        animator.Play(Animator.StringToHash(stateName), 0, 0f);
        animator.Update(0f);

        for (int f = 0; f < fCount; f++)
        {
            yield return new WaitForEndOfFrame(); // LateUpdate(팔로우) 후에 캡처

            for (int i = 0; i < 8; i++)
            {
                cams[i].targetTexture = rts[i];
                RenderTexture.active  = rts[i];
                cams[i].Render();

                readTex.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                readTex.Apply(false, false);
                File.WriteAllBytes(Path.Combine(dirPaths[i], $"frame_{(f + 1):0000}.png"), readTex.EncodeToPNG());
                cams[i].targetTexture = null;
            }
            RenderTexture.active = null;
        }

        Time.captureFramerate = prevCap;
    }

    void InitializeShadowCatcher()
    {
        // Ground Plane 생성 또는 확인
        if (groundPlane == null)
        {
            groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundPlane.name = "ShadowCatcher_Plane";
            groundPlane.transform.position = new Vector3(0, groundHeight, 0);
            groundPlane.transform.localScale = new Vector3(5, 1, 5); // 충분히 큰 바닥
            
            // Collider 제거 (렌더링 전용)
            var collider = groundPlane.GetComponent<Collider>();
            if (collider) Destroy(collider);
            
        }

        // Shadow Catcher Material 생성 또는 적용
        if (shadowCatcherMaterial == null)
        {
            Shader shadowCatcherShader = Shader.Find("Custom/ShadowCatcher");
            if (shadowCatcherShader != null)
            {
                shadowCatcherMaterial = new Material(shadowCatcherShader);
                shadowCatcherMaterial.name = "ShadowCatcher_Runtime";
                shadowCatcherMaterial.SetColor("_ShadowColor", shadowColor);
                shadowCatcherMaterial.SetFloat("_ShadowIntensity", shadowColor.a);
            }
            else
            {
                Debug.LogError("[AnimationRenderer8Direction_Rotate] Custom/ShadowCatcher 쉐이더를 찾을 수 없습니다!");
                return;
            }
        }

        // Ground Plane에 Material 적용
        var renderer = groundPlane.GetComponent<MeshRenderer>();
        if (renderer)
        {
            renderer.material = shadowCatcherMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true; // 그림자 받기
        }

        // Shadow Light 생성 또는 확인
        if (shadowLight == null)
        {
            GameObject lightObj = new GameObject("ShadowLight");
            shadowLight = lightObj.AddComponent<Light>();
            shadowLight.type = LightType.Directional;
            shadowLight.intensity = 1f;
            shadowLight.shadows = LightShadows.Soft;
            shadowLight.shadowStrength = 1f;
            shadowLight.transform.position = followTarget ? followTarget.position + new Vector3(0, 10f, 0) : new Vector3(0, 10f, 0);
            shadowLight.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            
        }

        // 캐릭터 그림자 설정 확인
        if (animator)
        {
            var renderers = animator.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; // 그림자 생성
                r.receiveShadows = false; // 캐릭터는 그림자 안 받음
            }
        }

    }

    void CreateCharacterOutline(Transform sourceRoot)
    {
        // 캐릭터 복제
        outlineInstance = Instantiate(sourceRoot.gameObject);
        outlineInstance.name = sourceRoot.name + "_Outline";
        
        // ★ 아웃라인을 CaptureRoot_Skill1 하위로 강제 이동 (올바른 계층 구조)
        Transform captureRoot = subjectRoot.parent; // SubjectRoot의 부모 = CaptureRoot_Skill1
        outlineInstance.transform.SetParent(captureRoot, false);
        outlineInstance.transform.localPosition = followTarget.position - captureRoot.position; // 월드 좌표 기준으로 초기 위치 설정
        outlineInstance.transform.localRotation = Quaternion.identity; // 캐릭터와 동일한 회전
        // ★ Scale은 원본과 동일하게 (쉐이더가 아웃라인 두께 처리)
        outlineInstance.transform.localScale = Vector3.one;
        
        // 디버그: 아웃라인 부모 설정 확인

        // Animator 찾기 (애니메이션 동기화용)
        outlineAnimator = outlineInstance.GetComponent<Animator>();
        if (outlineAnimator != null)
        {
            outlineAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            outlineAnimator.applyRootMotion = true; // ★ 루트 모션 적용으로 캐릭터와 동기화
        }

        // 아웃라인 머티리얼 생성 (없으면 자동 생성)
        if (outlineMaterial == null)
        {
            // 커스텀 아웃라인 쉐이더 사용 (없으면 기본 Unlit)
            Shader outlineShader = Shader.Find("Custom/OutlineInvertedHull");
            if (outlineShader == null)
            {
                outlineShader = Shader.Find("Universal Render Pipeline/Unlit");
                Debug.LogWarning("[AnimationRenderer8Direction_Rotate] Custom/OutlineInvertedHull 쉐이더를 찾을 수 없어 기본 Unlit 사용");
            }
            
            outlineMaterial = new Material(outlineShader);
            outlineMaterial.name = "OutlineMaterial_Runtime";
            outlineMaterial.SetColor("_OutlineColor", outlineColor);
            outlineMaterial.SetColor("_BaseColor", outlineColor); // Unlit fallback용
            
            // ★ 쉐이더 기반 아웃라인 두께 설정 (균일한 아웃라인)
            float shaderThickness = (outlineThickness - 1.0f) * 0.3f; // 1.03 → 0.009
            outlineMaterial.SetFloat("_OutlineWidth", shaderThickness);
            
            // Render Queue 설정
            outlineMaterial.renderQueue = 2000; // 원본 캐릭터보다 먼저 렌더링
        }

        // 모든 렌더러를 아웃라인 머티리얼로 교체
        var renderers = outlineInstance.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r is SkinnedMeshRenderer || r is MeshRenderer)
            {
                Material[] mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = outlineMaterial;
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        // 불필요한 컴포넌트 제거 (충돌, 스크립트 등)
        var colliders = outlineInstance.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) Destroy(c);
        
        var rigidbodies = outlineInstance.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rigidbodies) Destroy(rb);

        // MonoBehaviour 스크립트 제거 (Animator는 유지)
        var scripts = outlineInstance.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var s in scripts)
        {
            if (s != null && !(s is Animator))
                Destroy(s);
        }

    }

    void OnDestroy()
    {
        // Shadow Catcher 정리
        if (groundPlane != null && groundPlane.name == "ShadowCatcher_Plane")
        {
            Destroy(groundPlane);
            groundPlane = null;
        }

        if (shadowLight != null && shadowLight.gameObject.name == "ShadowLight")
        {
            Destroy(shadowLight.gameObject);
            shadowLight = null;
        }

        // 아웃라인 정리
        if (outlineInstance != null)
        {
            Destroy(outlineInstance);
            outlineInstance = null;
        }
    }

    static string San(string s)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Trim();
    }
}

