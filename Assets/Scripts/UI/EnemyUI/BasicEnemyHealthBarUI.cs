using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일반 몬스터 체력바 UI (자동 숨김 지원)
/// 평상시 숨김, 피격 시만 표시, 1.5초 후 자동 숨김
/// 거리 8 이상이면 즉시 숨김, 화면 밖이면 2.0초 후 숨김
/// </summary>
public class BasicEnemyHealthBarUI : MonoBehaviour
{
    #region Inspector Fields
    
    [Header("UI 컴포넌트")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image hpFillImage;
    
    [Header("색상 설정")]
    [SerializeField] private Color healthyColor = new Color(0f, 1f, 0f); // 초록
    [SerializeField] private Color damagedColor = new Color(1f, 1f, 0f); // 노랑
    [SerializeField] private Color criticalColor = new Color(1f, 0f, 0f); // 빨강
    [SerializeField] private float criticalThreshold = 0.3f;
    [SerializeField] private float damagedThreshold = 0.7f;
    
    [Header("⭐ 자동 숨김 설정")]
    [Tooltip("피격 후 자동 숨김 대기 시간")]
    [SerializeField] private float autoHideDelay = 1.5f;
    
    [Tooltip("최대 표시 거리 (이상이면 즉시 숨김)")]
    [SerializeField] private float maxVisibleDistance = 8f;
    
    [Tooltip("화면 밖 숨김 대기 시간")]
    [SerializeField] private float outOfScreenHideDelay = 2.0f;
    
    [Tooltip("화면 밖 체크 활성화")]
    [SerializeField] private bool checkScreenBounds = true;
    
    [Header("애니메이션 설정")]
    [SerializeField] private float hpChangeAnimSpeed = 5f;
    [SerializeField] private bool enableSmoothTransition = true;
    
    [Header("Billboard 설정")]
    [SerializeField] private bool enableBillboard = true;
    
    [Header("디버그")]
    
    #endregion
    
    #region Private Fields
    
    // 내부 상태
    private float targetHealthRatio = 1f;
    private float currentDisplayRatio = 1f;
    private Camera mainCamera;
    private Canvas canvas;
    private Transform playerTransform;
    
    // 타이머
    private Coroutine autoHideCoroutine;
    private Coroutine outOfScreenHideCoroutine;
    
    // 화면 밖 상태 추적
    private bool wasOutOfScreen = false;
    
    #endregion
    
    #region Properties
    
    public bool IsVisible { get; private set; } = false;
    
    #endregion
    
    #region Unity Lifecycle
    
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
            Debug.LogError($"❌ [BasicHealthBarUI] {gameObject.name}에 Canvas 컴포넌트가 없습니다!");
        }
    }
    
    private void Start()
    {
        mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            Debug.LogWarning($"⚠️ [BasicHealthBarUI] Main Camera를 찾을 수 없습니다!");
        }
        
        // 플레이어 찾기
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            playerTransform = playerController.transform;
        }
        
        // 초기화 (숨김 상태)
        SetHealthImmediate(1f);
        HideHealthBar();
        
    }
    
    private void Update()
    {
        // 부드러운 체력바 애니메이션
        if (enableSmoothTransition && Mathf.Abs(currentDisplayRatio - targetHealthRatio) > 0.001f)
        {
            currentDisplayRatio = Mathf.Lerp(currentDisplayRatio, targetHealthRatio, Time.deltaTime * hpChangeAnimSpeed);
            UpdateSliderValue(currentDisplayRatio);
        }
        
        // ⭐ 가시성 체크 (표시 중일 때만)
        if (IsVisible)
        {
            CheckVisibilityConditions();
        }
    }
    
    private void LateUpdate()
    {
        // Billboard 효과: LateUpdate에서 실행해야 NavMesh/Animator 회전 이후 보정됨
        if (enableBillboard && mainCamera != null && IsVisible)
        {
            ApplyBillboard();
        }
    }
    
    #endregion
    
    #region 표시/숨김 제어
    
    /// <summary>
    /// ⭐ 체력바 표시 + 자동 숨김 타이머 (파라미터 없으면 기본값 사용)
    /// </summary>
    public void ShowAndAutoHide(float delay = -1f)
    {
        ShowHealthBar();
        
        // delay가 -1이면 Inspector 기본값 사용
        float actualDelay = delay > 0 ? delay : autoHideDelay;
        
        // 기존 자동 숨김 타이머 취소
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
        }
        
        // 새 자동 숨김 타이머 시작
        autoHideCoroutine = StartCoroutine(AutoHideCoroutine(actualDelay));
        
    }
    
    /// <summary>
    /// 체력바 표시
    /// </summary>
    public void ShowHealthBar()
    {
        IsVisible = true;
        
        if (canvas != null)
        {
            canvas.enabled = true;
        }
        
        // 화면 밖 타이머 취소 (화면 안으로 들어왔을 때)
        if (outOfScreenHideCoroutine != null)
        {
            StopCoroutine(outOfScreenHideCoroutine);
            outOfScreenHideCoroutine = null;
        }
        
        wasOutOfScreen = false;
        
    }
    
    /// <summary>
    /// 체력바 숨김
    /// </summary>
    public void HideHealthBar()
    {
        IsVisible = false;
        
        if (canvas != null)
        {
            canvas.enabled = false;
        }
        
        // 모든 타이머 취소
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
        
        if (outOfScreenHideCoroutine != null)
        {
            StopCoroutine(outOfScreenHideCoroutine);
            outOfScreenHideCoroutine = null;
        }
        
        wasOutOfScreen = false;
        
    }
    
    /// <summary>
    /// 자동 숨김 코루틴
    /// </summary>
    private IEnumerator AutoHideCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        
        HideHealthBar();
    }
    
    /// <summary>
    /// 화면 밖 숨김 코루틴 (2초 대기)
    /// </summary>
    private IEnumerator OutOfScreenHideCoroutine()
    {
        
        yield return new WaitForSeconds(outOfScreenHideDelay);
        
        // 2초 후에도 여전히 화면 밖이면 숨김
        if (IsOutOfScreen())
        {
            
            HideHealthBar();
        }
    }
    
    #endregion
    
    #region 가시성 체크
    
    /// <summary>
    /// ⭐ 가시성 조건 체크 (거리 + 화면)
    /// </summary>
    private void CheckVisibilityConditions()
    {
        // 1. 거리 체크 (즉시 숨김)
        float distance = GetDistanceToPlayer();
        if (distance > maxVisibleDistance)
        {
            HideHealthBar();
            return;
        }
        
        // 2. 화면 밖 체크 (2초 대기 후 숨김)
        if (checkScreenBounds)
        {
            bool currentlyOutOfScreen = IsOutOfScreen();
            
            // 화면 밖으로 방금 나갔을 때
            if (currentlyOutOfScreen && !wasOutOfScreen)
            {
                wasOutOfScreen = true;
                
                // 화면 밖 숨김 타이머 시작
                if (outOfScreenHideCoroutine != null)
                {
                    StopCoroutine(outOfScreenHideCoroutine);
                }
                outOfScreenHideCoroutine = StartCoroutine(OutOfScreenHideCoroutine());
            }
            // 화면 안으로 다시 들어왔을 때
            else if (!currentlyOutOfScreen && wasOutOfScreen)
            {
                wasOutOfScreen = false;
                
                // 타이머 취소
                if (outOfScreenHideCoroutine != null)
                {
                    StopCoroutine(outOfScreenHideCoroutine);
                    outOfScreenHideCoroutine = null;
                    
                }
            }
        }
    }
    
    /// <summary>
    /// 플레이어와의 거리 계산
    /// </summary>
    private float GetDistanceToPlayer()
    {
        if (playerTransform == null)
        {
            // 플레이어 재검색
            var playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
            }
            else
            {
                return float.MaxValue; // 플레이어 없으면 매우 먼 거리
            }
        }
        
        return Vector3.Distance(transform.position, playerTransform.position);
    }
    
    /// <summary>
    /// 화면 밖 체크
    /// </summary>
    private bool IsOutOfScreen()
    {
        if (mainCamera == null) return false;
        
        Vector3 viewportPoint = mainCamera.WorldToViewportPoint(transform.position);
        
        // Viewport 좌표: (0,0) = 왼쪽 아래, (1,1) = 오른쪽 위
        bool outOfScreen = viewportPoint.x < 0f || viewportPoint.x > 1f ||
                          viewportPoint.y < 0f || viewportPoint.y > 1f ||
                          viewportPoint.z < 0f; // 카메라 뒤쪽
        
        return outOfScreen;
    }
    
    #endregion
    
    #region 체력바 업데이트
    
    /// <summary>
    /// 체력바 업데이트
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
    /// 즉시 체력 설정 (애니메이션 없음)
    /// </summary>
    public void SetHealthImmediate(float healthRatio)
    {
        targetHealthRatio = Mathf.Clamp01(healthRatio);
        currentDisplayRatio = targetHealthRatio;
        
        UpdateSliderValue(currentDisplayRatio);
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
            float t = (ratio - criticalThreshold) / (damagedThreshold - criticalThreshold);
            newColor = Color.Lerp(damagedColor, healthyColor, t);
        }
        else
        {
            // 위험 (30% 이하)
            float t = ratio / criticalThreshold;
            newColor = Color.Lerp(criticalColor, damagedColor, t);
        }
        
        hpFillImage.color = newColor;
    }
    
    #endregion
    
    #region Billboard
    
    /// <summary>
    /// Billboard 효과 적용 (카메라 향하기)
    /// 카메라와 동일한 회전 적용 → 항상 카메라 정면을 향함
    /// 직교/원근 카메라 모두 정확하게 동작하며 부모 회전 영향을 받지 않음
    /// </summary>
    private void ApplyBillboard()
    {
        if (mainCamera == null) return;
        transform.rotation = mainCamera.transform.rotation;
    }
    
    #endregion
    
    #region 디버그 도구
    
    [ContextMenu("Test - Show And Auto Hide")]
    private void TestShowAndAutoHide()
    {
        ShowAndAutoHide(autoHideDelay);
    }
    
    [ContextMenu("Test - Show Health Bar")]
    private void TestShow()
    {
        ShowHealthBar();
    }
    
    [ContextMenu("Test - Hide Health Bar")]
    private void TestHide()
    {
        HideHealthBar();
    }
    
    [ContextMenu("Test - Damage 30%")]
    private void TestDamage()
    {
        UpdateHealthBar(targetHealthRatio - 0.3f);
    }
    
    #endregion
}

