using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 튜토리얼 스포트라이트 효과
/// - 검은 오버레이 + 원형 구멍 효과
/// - 단계별 UI 버튼 강조
/// - 부드러운 전환 애니메이션
/// </summary>
public class TutorialSpotlight : MonoBehaviour
{
    [Header("오버레이 설정")]
    [SerializeField] private Image dimmerImage;              // 전체 화면 검은 이미지
    [SerializeField] private Material spotlightMaterial;     // 커스텀 셰이더 머티리얼
    [SerializeField] private float defaultAlpha = 0.7f;      // 어두운 영역 투명도 (0~1)
    
    [Header("구멍 설정")]
    [SerializeField] private bool useInspectorValues = false; // true: Inspector 값 사용, false: Material 값 사용
    [SerializeField] private float holeRadius = 150f;        // 구멍 반지름 (픽셀) - useInspectorValues=true일 때만 사용
    [SerializeField] private float softEdge = 80f;           // 페이드 경계 (픽셀) - useInspectorValues=true일 때만 사용
    [SerializeField] private float transitionDuration = 0.5f;// 전환 애니메이션 시간
    
    [Header("타겟 오브젝트 (자동 탐색)")]
    [SerializeField] private RectTransform joystickTarget;       // 조이스틱
    [SerializeField] private RectTransform attackButtonTarget;   // 공격 버튼
    [SerializeField] private RectTransform dashButtonTarget;     // 대시 버튼
    [SerializeField] private RectTransform skill1ButtonTarget;   // 스킬1 버튼
    [SerializeField] private RectTransform skill2ButtonTarget;   // 스킬2 버튼
    
    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 내부 상태
    private bool isActive = false;
    private Vector2 currentHoleCenter = Vector2.zero;
    private Vector2 targetHoleCenter = Vector2.zero;
    private float currentRadius = 0f;
    private float targetRadius = 0f;
    private Coroutine transitionCoroutine;
    
    // 셰이더 프로퍼티 ID (성능 최적화)
    private static readonly int HoleCenterID = Shader.PropertyToID("_HoleCenter");
    private static readonly int HoleRadiusID = Shader.PropertyToID("_HoleRadius");
    private static readonly int SoftEdgeID = Shader.PropertyToID("_SoftEdge");
    private static readonly int ColorID = Shader.PropertyToID("_Color");
    
    void Start()
    {
        InitializeSpotlight();
    }
    
    /// <summary>
    /// 스포트라이트 초기화
    /// </summary>
    private void InitializeSpotlight()
    {
        // UI 타겟 자동 탐색
        if (joystickTarget == null)
        {
            GameObject joystickObj = GameObject.Find("Dynamic Joystick");
            if (joystickObj != null)
            {
                joystickTarget = joystickObj.GetComponent<RectTransform>();
                if (showDebugLogs)
                    Debug.Log("[TutorialSpotlight] 조이스틱 자동 탐색 성공");
            }
        }
        
        if (attackButtonTarget == null)
        {
            GameObject attackObj = GameObject.Find("AttackButton");
            if (attackObj != null)
            {
                attackButtonTarget = attackObj.GetComponent<RectTransform>();
                if (showDebugLogs)
                    Debug.Log("[TutorialSpotlight] 공격버튼 자동 탐색 성공");
            }
        }
        
        if (dashButtonTarget == null)
        {
            GameObject dashObj = GameObject.Find("DashButton");
            if (dashObj != null)
            {
                dashButtonTarget = dashObj.GetComponent<RectTransform>();
                if (showDebugLogs)
                    Debug.Log("[TutorialSpotlight] 대시버튼 자동 탐색 성공");
            }
        }
        
        if (skill1ButtonTarget == null)
        {
            GameObject skill1Obj = GameObject.Find("Skill1Button");
            if (skill1Obj != null)
            {
                skill1ButtonTarget = skill1Obj.GetComponent<RectTransform>();
                if (showDebugLogs)
                    Debug.Log("[TutorialSpotlight] 스킬1버튼 자동 탐색 성공");
            }
        }
        
        if (skill2ButtonTarget == null)
        {
            GameObject skill2Obj = GameObject.Find("Skill2Button");
            if (skill2Obj != null)
            {
                skill2ButtonTarget = skill2Obj.GetComponent<RectTransform>();
                if (showDebugLogs)
                    Debug.Log("[TutorialSpotlight] 스킬2버튼 자동 탐색 성공");
            }
        }
        
        // Dimmer Image 설정
        if (dimmerImage != null && spotlightMaterial != null)
        {
            dimmerImage.material = spotlightMaterial;
            dimmerImage.color = new Color(0, 0, 0, defaultAlpha);
            dimmerImage.gameObject.SetActive(false);
            
            // Material에서 초기값 읽기 (useInspectorValues=false일 때)
            if (!useInspectorValues)
            {
                holeRadius = spotlightMaterial.GetFloat(HoleRadiusID);
                softEdge = spotlightMaterial.GetFloat(SoftEdgeID);
                
                if (showDebugLogs)
                    Debug.Log($"[TutorialSpotlight] Material에서 값 읽음: Radius={holeRadius}, SoftEdge={softEdge}");
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log($"[TutorialSpotlight] Inspector 값 사용: Radius={holeRadius}, SoftEdge={softEdge}");
            }
            
            if (showDebugLogs)
                Debug.Log("[TutorialSpotlight] 초기화 완료");
        }
        else
        {
            Debug.LogError("[TutorialSpotlight] Dimmer Image 또는 Material이 없습니다!");
        }
    }
    
    /// <summary>
    /// 스포트라이트 활성화 (단계별)
    /// </summary>
    public void ShowSpotlight(TutorialStepController.TutorialStep step)
    {
        if (dimmerImage == null)
            return;
        
        // 타겟 결정
        RectTransform target = GetTargetForStep(step);
        
        if (target == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[TutorialSpotlight] {step} 단계의 타겟을 찾을 수 없습니다!");
            HideSpotlight();
            return;
        }
        
        if (showDebugLogs)
            Debug.Log($"[TutorialSpotlight] 💡 스포트라이트 활성화: {step} → {target.name}");
        
        // Dimmer 활성화
        if (!isActive)
        {
            dimmerImage.gameObject.SetActive(true);
            isActive = true;
        }
        
        // 타겟 위치로 이동
        MoveHoleTo(target);
    }
    
    /// <summary>
    /// 스포트라이트 비활성화
    /// </summary>
    public void HideSpotlight()
    {
        if (dimmerImage == null)
            return;
        
        if (showDebugLogs)
            Debug.Log("[TutorialSpotlight] 스포트라이트 비활성화");
        
        dimmerImage.gameObject.SetActive(false);
        isActive = false;
        
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
    }
    
    /// <summary>
    /// 단계에 맞는 타겟 반환
    /// </summary>
    private RectTransform GetTargetForStep(TutorialStepController.TutorialStep step)
    {
        switch (step)
        {
            case TutorialStepController.TutorialStep.Move:
                return joystickTarget;
            
            case TutorialStepController.TutorialStep.Attack:
                return attackButtonTarget;
            
            case TutorialStepController.TutorialStep.Dash:
                return dashButtonTarget;
            
            case TutorialStepController.TutorialStep.Skill1:
                return skill1ButtonTarget;
            
            case TutorialStepController.TutorialStep.Skill2:
                return skill2ButtonTarget;
            
            default:
                return null;
        }
    }
    
    /// <summary>
    /// 타겟 위치로 구멍 이동 (부드러운 전환)
    /// </summary>
    private void MoveHoleTo(RectTransform target)
    {
        if (target == null)
            return;
        
        // 타겟의 스크린 좌표 계산
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, target.position);
        
        // 정규화된 좌표로 변환 (0~1)
        Vector2 normalizedPos = new Vector2(
            screenPos.x / Screen.width,
            screenPos.y / Screen.height
        );
        
        targetHoleCenter = normalizedPos;
        targetRadius = holeRadius;
        
        if (showDebugLogs)
            Debug.Log($"[TutorialSpotlight] 타겟 위치: {target.name} → Screen({screenPos}) → Normalized({normalizedPos})");
        
        // 전환 애니메이션 시작
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
        }
        transitionCoroutine = StartCoroutine(TransitionToTarget());
    }
    
    /// <summary>
    /// 타겟으로 부드럽게 전환하는 코루틴
    /// </summary>
    private IEnumerator TransitionToTarget()
    {
        float elapsed = 0f;
        Vector2 startCenter = currentHoleCenter;
        float startRadius = currentRadius;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transitionDuration;
            
            // EaseInOut 커브
            t = t * t * (3f - 2f * t);
            
            currentHoleCenter = Vector2.Lerp(startCenter, targetHoleCenter, t);
            currentRadius = Mathf.Lerp(startRadius, targetRadius, t);
            
            UpdateShaderParameters();
            
            yield return null;
        }
        
        // 최종 위치로 정확히 설정
        currentHoleCenter = targetHoleCenter;
        currentRadius = targetRadius;
        UpdateShaderParameters();
        
        transitionCoroutine = null;
    }
    
    /// <summary>
    /// 셰이더 파라미터 업데이트
    /// </summary>
    private void UpdateShaderParameters()
    {
        if (dimmerImage == null || dimmerImage.material == null)
            return;
        
        Material mat = dimmerImage.material;
        
        // 셰이더에 파라미터 전달
        mat.SetVector(HoleCenterID, currentHoleCenter);
        mat.SetFloat(HoleRadiusID, currentRadius);
        mat.SetFloat(SoftEdgeID, softEdge);
        mat.SetColor(ColorID, new Color(0, 0, 0, defaultAlpha));
    }
    
    /// <summary>
    /// 즉시 타겟으로 이동 (애니메이션 없음)
    /// </summary>
    public void JumpToTarget(TutorialStepController.TutorialStep step)
    {
        RectTransform target = GetTargetForStep(step);
        
        if (target == null)
            return;
        
        // 타겟의 스크린 좌표 계산
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, target.position);
        Vector2 normalizedPos = new Vector2(
            screenPos.x / Screen.width,
            screenPos.y / Screen.height
        );
        
        currentHoleCenter = normalizedPos;
        targetHoleCenter = normalizedPos;
        currentRadius = holeRadius;
        targetRadius = holeRadius;
        
        UpdateShaderParameters();
        
        if (!isActive)
        {
            dimmerImage.gameObject.SetActive(true);
            isActive = true;
        }
    }
    
    #region Public 프로퍼티
    
    /// <summary>
    /// 스포트라이트 활성 상태
    /// </summary>
    public bool IsActive => isActive;
    
    /// <summary>
    /// 구멍 반지름 설정
    /// </summary>
    public void SetHoleRadius(float radius)
    {
        holeRadius = radius;
        targetRadius = radius;
    }
    
    /// <summary>
    /// 페이드 경계 설정
    /// </summary>
    public void SetSoftEdge(float edge)
    {
        softEdge = edge;
        UpdateShaderParameters();
    }
    
    /// <summary>
    /// 오버레이 알파 설정
    /// </summary>
    public void SetOverlayAlpha(float alpha)
    {
        defaultAlpha = Mathf.Clamp01(alpha);
        if (dimmerImage != null)
        {
            dimmerImage.color = new Color(0, 0, 0, defaultAlpha);
        }
        UpdateShaderParameters();
    }
    
    #endregion
}

