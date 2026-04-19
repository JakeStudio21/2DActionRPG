using TMPro;
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
    
    [Header("이름 표시 (선택)")]
    [Tooltip("몬스터 이름을 표시할 TextMeshProUGUI. 연결하지 않으면 이름 표시 없음.")]
    [SerializeField] private TextMeshProUGUI nameText;
    
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
    
    [Header("디버그")]
    
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
        
    }
    
    private void Update()
    {
        // 부드러운 체력바 애니메이션
        if (enableSmoothTransition && Mathf.Abs(currentDisplayRatio - targetHealthRatio) > 0.001f)
        {
            currentDisplayRatio = Mathf.Lerp(currentDisplayRatio, targetHealthRatio, Time.deltaTime * hpChangeAnimSpeed);
            UpdateSliderValue(currentDisplayRatio);
        }
    }
    
    private void LateUpdate()
    {
        // Billboard 효과: LateUpdate에서 실행해야 NavMesh/Animator 회전 이후 보정됨
        if (enableBillboard && mainCamera != null)
        {
            ApplyBillboard();
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
    /// World Space Canvas는 -Z 방향이 앞면이므로
    /// 카메라와 동일한 회전(transform.rotation = camera.rotation)이 가장 정확함.
    /// </summary>
    private void ApplyBillboard()
    {
        // 카메라와 동일한 회전 적용 → 항상 카메라 정면을 향함
        // 이 방식은 직교/원근 카메라 모두에서 텍스트가 올바르게 표시됨
        transform.rotation = mainCamera.transform.rotation;
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
    /// 몬스터 이름 설정. nameText가 연결되지 않은 경우 조용히 무시.
    /// EnemyHealth.CreateEliteHealthBar()에서 호출됨.
    /// </summary>
    public void SetName(string monsterName)
    {
        if (nameText == null) return;
        nameText.text = monsterName;
        nameText.gameObject.SetActive(!string.IsNullOrEmpty(monsterName));
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

