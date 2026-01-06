using System.Collections;
using System.IO;
using UnityEngine;

// 실행 순서를 가장 늦게(다른 LateUpdate 이후) 보장
[DefaultExecutionOrder(10000)]
public class AnimationRenderer8Direction : MonoBehaviour
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

    [Header("Ground Shadow (Character Clone)")]
    public bool enableShadow = true;
    [Tooltip("캐릭터를 복제해서 그림자로 사용할 대상 (보통 Animator와 동일)")]
    public Transform shadowSourceRoot;         // 복제할 캐릭터 루트 (보통 animator.transform)
    public Vector3 shadowOffset = new Vector3(0f, 0.01f, 0f); // 바닥 높이
    public Vector3 shadowScale = new Vector3(1f, 0.01f, 1f);  // Y축 0.01로 평평하게
    public Vector3 shadowRotation = new Vector3(0f, 0f, 0f);  // 추가 회전 (기울기 조절)
    public Color shadowColor = new Color(0f, 0f, 0f, 0.5f);   // 검은색 반투명
    [Tooltip("그림자 전용 머티리얼 (없으면 자동 생성)")]
    public Material shadowMaterial;
    
    [Header("Bip Bone Fix (Optional)")]
    [Tooltip("Bip001 Pelvis 본을 사용하는 캐릭터의 Y축 튐 현상 방지 (Bip 방식 전용, 일반 캐릭터는 체크 해제)")]
    public bool fixBipPelvisYAxis = false;

    [Header("Outline (Character Clone)")]
    public bool enableOutline = true;
    [Tooltip("아웃라인을 적용할 대상 (보통 Animator와 동일, 비워두면 shadowSourceRoot 사용)")]
    public Transform outlineSourceRoot;
    [Range(1.0f, 1.2f)]
    [Tooltip("아웃라인 두께 (캐릭터 스케일 배율)")]
    public float outlineThickness = 1.02f;     // 3% 크게
    public Color outlineColor = new Color(0f, 0f, 0f, 1f); // 검은색 불투명
    [Tooltip("아웃라인 전용 머티리얼 (없으면 자동 생성)")]
    public Material outlineMaterial;

    // 내부
    private RenderTexture[] rts;
    private Texture2D readTex;
    private Quaternion subjectRootOrigRot;
    private Vector3[] camOffsets;
    private Quaternion[] camRotations;
    private GameObject shadowInstance;  // 복제된 그림자 루트
    private Animator shadowAnimator;    // 그림자의 Animator (원본과 동기화용)
    private Transform shadowPelvisBone; // ★ Shadow의 Bip001 Pelvis 본 (매 프레임 0,0,0으로 고정)
    private GameObject outlineInstance; // 복제된 아웃라인 루트
    private Animator outlineAnimator;   // 아웃라인의 Animator (원본과 동기화용)

    void Awake()
    {
        // ===== 필수 체크 =====
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!followTarget && animator) followTarget = animator.transform;
        if (!subjectRoot) { Debug.LogError("[AnimationRenderer8Direction] subjectRoot(부모 Empty) 미지정"); enabled = false; return; }
        if (!pivot)       { Debug.LogError("[AnimationRenderer8Direction] pivot 미지정"); enabled = false; return; }

        // 카메라 찾기/세팅
        if (autoFindByName)
        {
            cams = new Camera[8];
            for (int i = 0; i < 8; i++)
            {
                var t = pivot.Find(dirNames[i]);
                if (!t) { Debug.LogError($"[AnimationRenderer8Direction] Pivot 하위 '{dirNames[i]}' 없음"); enabled = false; return; }
                var cam = t.GetComponentInChildren<Camera>();
                if (!cam) { Debug.LogError($"[AnimationRenderer8Direction] '{dirNames[i]}'에 Camera 없음"); enabled = false; return; }
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

        // ★ 캐릭터 복제 그림자 생성
        if (enableShadow)
        {
            Transform sourceRoot = shadowSourceRoot ? shadowSourceRoot : (animator ? animator.transform : null);
            if (sourceRoot != null)
            {
                CreateCharacterShadow(sourceRoot);
                Debug.Log("[AnimationRenderer8Direction] 캐릭터 실루엣 그림자 생성 완료");
            }
            else
            {
                Debug.LogWarning("[AnimationRenderer8Direction] shadowSourceRoot가 지정되지 않아 그림자를 생성할 수 없습니다.");
            }
        }

        // ★ 캐릭터 복제 아웃라인 생성
        if (enableOutline)
        {
            Transform sourceRoot = outlineSourceRoot ? outlineSourceRoot : 
                                   (shadowSourceRoot ? shadowSourceRoot : 
                                   (animator ? animator.transform : null));
            if (sourceRoot != null)
            {
                CreateCharacterOutline(sourceRoot);
                Debug.Log("[AnimationRenderer8Direction] 캐릭터 아웃라인 생성 완료");
            }
            else
            {
                Debug.LogWarning("[AnimationRenderer8Direction] outlineSourceRoot가 지정되지 않아 아웃라인을 생성할 수 없습니다.");
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

        // ★ 그림자 위치/애니메이션 동기화
        if (shadowInstance != null && followTarget != null)
        {
            // 위치: 바닥에 고정
            shadowInstance.transform.position = followTarget.position + shadowOffset;
            
            // 회전: 원본 회전 + 추가 회전
            shadowInstance.transform.rotation = followTarget.rotation * Quaternion.Euler(shadowRotation);
            
            // 스케일: 원본 스케일에 그림자 스케일 곱함 (Y축 0.01로 평평하게)
            Vector3 origScale = followTarget.localScale;
            shadowInstance.transform.localScale = new Vector3(
                origScale.x * shadowScale.x,
                origScale.y * shadowScale.y, // 보통 0.01
                origScale.z * shadowScale.z
            );
            
            // Animator 동기화
            if (shadowAnimator != null && animator != null)
            {
                shadowAnimator.speed = animator.speed;
                shadowAnimator.SetFloat("speed", animator.GetFloat("speed"));
                // 필요하면 다른 파라미터도 동기화
            }
            
            // ★ Bip001 Pelvis 본의 Y축만 0으로 강제 고정 (플래그가 켜져있을 때만)
            if (fixBipPelvisYAxis && shadowPelvisBone != null)
            {
                Vector3 pos = shadowPelvisBone.localPosition;
                pos.y = 0f;
                shadowPelvisBone.localPosition = pos;
            }
        }

        // ★ 아웃라인 위치/애니메이션 동기화
        if (outlineInstance != null && followTarget != null)
        {
            // 위치: 캐릭터와 완전히 동일
            outlineInstance.transform.position = followTarget.position;
            outlineInstance.transform.rotation = followTarget.rotation;
            
            // 스케일: 원본과 동일 (쉐이더가 아웃라인 처리)
            outlineInstance.transform.localScale = followTarget.localScale;
            
            // Animator 동기화
            if (outlineAnimator != null && animator != null)
            {
                outlineAnimator.speed = animator.speed;
                outlineAnimator.SetFloat("speed", animator.GetFloat("speed"));
                // 필요하면 다른 파라미터도 동기화
            }
            
            // ★ 쉐이더 파라미터 실시간 업데이트 (Inspector 값 변경 반영)
            if (outlineMaterial != null && outlineMaterial.HasProperty("_OutlineWidth"))
            {
                float shaderThickness = (outlineThickness - 1.0f) * 0.3f; // 1.02 → 0.006
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
        Debug.Log("[AnimationRenderer8Direction] Done → " + statePath);
    }

    void CreateCharacterShadow(Transform sourceRoot)
    {
        // 캐릭터 복제
        shadowInstance = Instantiate(sourceRoot.gameObject);
        shadowInstance.name = sourceRoot.name + "_Shadow";
        shadowInstance.transform.position = followTarget.position + shadowOffset;
        shadowInstance.transform.rotation = followTarget.rotation;
        shadowInstance.transform.localScale = shadowScale;

        // Animator 찾기 (애니메이션 동기화용)
        shadowAnimator = shadowInstance.GetComponent<Animator>();
        if (shadowAnimator != null)
        {
            shadowAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            shadowAnimator.applyRootMotion = false; // 그림자는 원본을 따라가기만 함
        }

        // ★ Bip001 Pelvis 본 찾기 (플래그가 켜져있을 때만)
        if (fixBipPelvisYAxis)
        {
            shadowPelvisBone = FindPelvisBone(shadowInstance.transform);
            if (shadowPelvisBone != null)
            {
                Debug.Log($"[Shadow] Bip Pelvis 본 발견: {shadowPelvisBone.name}, 초기 localPosition: {shadowPelvisBone.localPosition}");
            }
            else
            {
                Debug.LogWarning("[Shadow] Bip Pelvis 본을 찾을 수 없습니다. fixBipPelvisYAxis를 해제하거나 본 구조를 확인하세요.");
            }
        }

        // 머티리얼 생성 (없으면 자동 생성)
        if (shadowMaterial == null)
        {
            shadowMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            shadowMaterial.name = "ShadowMaterial_Runtime";
            shadowMaterial.SetColor("_BaseColor", shadowColor);
            
            // 투명도 설정
            shadowMaterial.SetFloat("_Surface", 1); // Transparent
            shadowMaterial.SetFloat("_Blend", 0);   // Alpha
            shadowMaterial.SetFloat("_ZWrite", 0);  // 깊이 쓰기 끄기 (투명)
            shadowMaterial.SetOverrideTag("RenderType", "Transparent");
            shadowMaterial.renderQueue = 1999; // 아웃라인보다 먼저 렌더링
            shadowMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            shadowMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        // 모든 렌더러를 그림자 머티리얼로 교체
        var renderers = shadowInstance.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r is SkinnedMeshRenderer || r is MeshRenderer)
            {
                Material[] mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = shadowMaterial;
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        // 불필요한 컴포넌트 제거 (충돌, 스크립트 등)
        var colliders = shadowInstance.GetComponentsInChildren<Collider>(true);
        foreach (var c in colliders) Destroy(c);
        
        var rigidbodies = shadowInstance.GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rigidbodies) Destroy(rb);

        // MonoBehaviour 스크립트 제거 (Animator는 유지)
        var scripts = shadowInstance.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var s in scripts)
        {
            if (s != null && !(s is Animator))
                Destroy(s);
        }

        Debug.Log($"[AnimationRenderer8Direction] 그림자 생성: {renderers.Length}개 렌더러를 검은색으로 변경");
    }

    // ★ Bip001 Pelvis 본을 찾는 함수 (Bip 방식 전용)
    Transform FindPelvisBone(Transform root)
    {
        // 일반적인 Bip 본 구조: root → Bip001 → Bip001 Pelvis
        string[] rootBoneNames = { "Bip001", "Bip01", "root", "Root" };
        string[] pelvisNames = { "Pelvis", "pelvis", "Bip001 Pelvis", "Bip01 Pelvis" };
        
        // 첫 번째 자식 본 찾기 (Bip001)
        if (root.childCount > 0)
        {
            Transform firstChild = root.GetChild(0);
            
            // Bip001 확인
            foreach (string boneName in rootBoneNames)
            {
                if (firstChild.name.Contains(boneName))
                {
                    // Bip001의 자식 중에서 Pelvis 찾기
                    foreach (Transform child in firstChild)
                    {
                        foreach (string pelvisName in pelvisNames)
                        {
                            if (child.name.Contains(pelvisName))
                            {
                                return child; // Pelvis 본 반환
                            }
                        }
                    }
                    
                    // Pelvis 못 찾으면 첫 번째 자식 반환
                    if (firstChild.childCount > 0)
                    {
                        return firstChild.GetChild(0);
                    }
                }
            }
        }
        
        return null; // 못 찾음
    }

    void CreateCharacterOutline(Transform sourceRoot)
    {
        // 캐릭터 복제
        outlineInstance = Instantiate(sourceRoot.gameObject);
        outlineInstance.name = sourceRoot.name + "_Outline";
        outlineInstance.transform.position = followTarget.position;
        outlineInstance.transform.rotation = followTarget.rotation;
        // ★ Scale은 원본과 동일하게 (쉐이더가 아웃라인 두께 처리)
        outlineInstance.transform.localScale = followTarget.localScale;

        // Animator 찾기 (애니메이션 동기화용)
        outlineAnimator = outlineInstance.GetComponent<Animator>();
        if (outlineAnimator != null)
        {
            outlineAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            outlineAnimator.applyRootMotion = false; // 아웃라인은 원본을 따라가기만 함
        }

        // 아웃라인 머티리얼 생성 (없으면 자동 생성)
        if (outlineMaterial == null)
        {
            // 커스텀 아웃라인 쉐이더 사용 (없으면 기본 Unlit)
            Shader outlineShader = Shader.Find("Custom/OutlineInvertedHull");
            if (outlineShader == null)
            {
                outlineShader = Shader.Find("Universal Render Pipeline/Unlit");
                Debug.LogWarning("[AnimationRenderer8Direction] Custom/OutlineInvertedHull 쉐이더를 찾을 수 없어 기본 Unlit 사용");
            }
            
            outlineMaterial = new Material(outlineShader);
            outlineMaterial.name = "OutlineMaterial_Runtime";
            outlineMaterial.SetColor("_OutlineColor", outlineColor);
            outlineMaterial.SetColor("_BaseColor", outlineColor); // Unlit fallback용
            
            // ★ 쉐이더 기반 아웃라인 두께 설정 (균일한 아웃라인)
            float shaderThickness = (outlineThickness - 1.0f) * 0.3f; // 1.02 → 0.006
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

        Debug.Log($"[AnimationRenderer8Direction] 아웃라인 생성: {renderers.Length}개 렌더러, 두께 {outlineThickness}배");
    }

    void OnDestroy()
    {
        // 그림자 정리
        if (shadowInstance != null)
        {
            Destroy(shadowInstance);
            shadowInstance = null;
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