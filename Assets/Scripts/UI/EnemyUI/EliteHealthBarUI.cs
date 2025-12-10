using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 엘리트/보스 몬스터 머리 위 체력바 UI (World Space)
/// 개별 몬스터에 부착되어 체력 상태를 표시
/// </summary>
public class EliteHealthBarUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image hpFillImage;
    
    [Header("색상 설정")]
    [SerializeField] private Color healthyColor = new Color(0f, 1f, 0f); // 초록색
    [SerializeField] private Color damagedColor = new Color(1f, 1f, 0f); // 노란색
    [SerializeField] private Color criticalColor = new Color(1f, 0f, 0f); // 빨간색
    [SerializeField] private float criticalThreshold = 0.3f; // 30% 이하
    [SerializeField] private float damagedThreshold = 0.7f;  // 70% 이하
    
    [Header("애니메이션 설정")]
    [SerializeField] private float hpChangeAnimSpeed = 5f;
    [SerializeField] private bool enableSmoothTransition = true;
    
    [Header("Billboard 설정")]
    [SerializeField] private bool enableBillboard = true;
    [SerializeField] private bool lockYAxis = true; // Y축 회전만 허용
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // 내부 상태
    private float targetHealthRatio = 1f;
    private float currentDisplayRatio = 1f;
    private Camera mainCamera;
    private Canvas canvas;
    
    private void Awake()
    {
        // 컴포넌트 자동 참조
        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>();
        
        if (hpFillImage == null && healthSlider != null)
            hpFillImage = healthSlider.fillRect?.GetComponent<Image>();
        
        canvas = GetComponent<Canvas>();
        
        if (canvas == null)
        {
            Debug.LogError($"❌ [EliteHealthBarUI] {gameObject.name}에 Canvas 컴포넌트가 없습니다!");
        }
    }
    
    private void Start()
    {
        mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            Debug.LogWarning($"⚠️ [EliteHealthBarUI] Main Camera를 찾을 수 없습니다!");
        }
        
        // 초기화
        UpdateHealthBar(1f);
        
        if (enableDebugLogs)
        {
            Debug.Log($"✅ [EliteHealthBarUI] {gameObject.name} 초기화 완료");
        }
    }
    
    private void Update()
    {
        // Billboard 효과 (카메라 향하기)
        if (enableBillboard && mainCamera != null)
        {
            ApplyBillboard();
        }
        
        // 부드러운 체력바 애니메이션
        if (enableSmoothTransition && Mathf.Abs(currentDisplayRatio - targetHealthRatio) > 0.001f)
        {
            currentDisplayRatio = Mathf.Lerp(currentDisplayRatio, targetHealthRatio, Time.deltaTime * hpChangeAnimSpeed);
            UpdateSliderValue(currentDisplayRatio);
        }
    }
    
    /// <summary>
    /// 체력바 업데이트 (외부 호출)
    /// </summary>
    public void UpdateHealthBar(float healthRatio)
    {
        targetHealthRatio = Mathf.Clamp01(healthRatio);
        
        if (!enableSmoothTransition)
        {
            currentDisplayRatio = targetHealthRatio;
            UpdateSliderValue(currentDisplayRatio);
        }
        
        UpdateHealthColor(targetHealthRatio);
        
        if (enableDebugLogs)
        {
            Debug.Log($"🩹 [EliteHealthBarUI] 체력바 업데이트: {targetHealthRatio:P0}");
        }
    }
    
    /// <summary>
    /// Slider 값 설정
    /// </summary>
    private void UpdateSliderValue(float ratio)
    {
        if (healthSlider != null)
        {
            healthSlider.value = ratio;
        }
    }
    
    /// <summary>
    /// 체력 비율에 따라 색상 변경
    /// </summary>
    private void UpdateHealthColor(float ratio)
    {
        if (hpFillImage == null) return;
        
        Color newColor;
        
        if (ratio >= damagedThreshold)
        {
            // 건강 (70% 이상)
            newColor = healthyColor;
        }
        else if (ratio >= criticalThreshold)
        {
            // 손상 (30~70%)
            // 노란색으로 부드럽게 전환
            float t = (ratio - criticalThreshold) / (damagedThreshold - criticalThreshold);
            newColor = Color.Lerp(damagedColor, healthyColor, t);
        }
        else
        {
            // 위험 (30% 이하)
            // 빨간색으로 부드럽게 전환
            float t = ratio / criticalThreshold;
            newColor = Color.Lerp(criticalColor, damagedColor, t);
        }
        
        hpFillImage.color = newColor;
    }
    
    /// <summary>
    /// Billboard 효과 적용 (카메라 향하기)
    /// </summary>
    private void ApplyBillboard()
    {
        if (lockYAxis)
        {
            // Y축 회전만 (자연스러운 느낌)
            Vector3 directionToCamera = mainCamera.transform.position - transform.position;
            directionToCamera.y = 0; // Y축 고정
            
            if (directionToCamera.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
                transform.rotation = targetRotation;
            }
        }
        else
        {
            // 완전히 카메라를 향함
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
    }
    
    /// <summary>
    /// 체력바 표시/숨김
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (canvas != null)
        {
            canvas.enabled = visible;
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 즉시 체력 설정 (애니메이션 없음)
    /// </summary>
    public void SetHealthImmediate(float healthRatio)
    {
        targetHealthRatio = Mathf.Clamp01(healthRatio);
        currentDisplayRatio = targetHealthRatio;
        
        UpdateSliderValue(currentDisplayRatio);
        UpdateHealthColor(targetHealthRatio);
    }
    
    #region 디버그 도구
    
    /// <summary>
    /// 테스트용: 체력 감소 시뮬레이션
    /// </summary>
    [ContextMenu("Test - Damage 10%")]
    private void TestDamage()
    {
        UpdateHealthBar(targetHealthRatio - 0.1f);
    }
    
    /// <summary>
    /// 테스트용: 체력 회복
    /// </summary>
    [ContextMenu("Test - Heal Full")]
    private void TestHealFull()
    {
        UpdateHealthBar(1f);
    }
    
    /// <summary>
    /// 테스트용: 위험 상태
    /// </summary>
    [ContextMenu("Test - Critical HP")]
    private void TestCritical()
    {
        UpdateHealthBar(0.2f);
    }
    
    #endregion
}

