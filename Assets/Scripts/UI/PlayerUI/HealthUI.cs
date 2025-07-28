using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 체력 UI를 관리하는 클래스 (Slider 방식)
/// PlayerHealth와 분리되어 순수 UI 관리만 담당
/// </summary>
public class HealthUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private bool autoFindSlider = true;
    
    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 내부 상태
    private bool isInitialized = false;
    private bool isHealthSubscribed = false;
    private PlayerHealth playerHealth;
    
    const string HEALTH_SLIDER_TEXT = "Health Slider";
    
    private void Awake()
    {
        InitializeHealthSlider();
    }
    
    private void Start()
    {
        if (!isInitialized)
        {
            InitializeHealthSlider();
        }
    }
    
    private void Update()
    {
        // PlayerHealth가 준비될 때까지 대기 후 이벤트 연결
        if (!isHealthSubscribed && PlayerHealth.Instance != null)
        {
            ConnectToPlayerHealth();
        }
    }
    
    /// <summary>
    /// Health Slider 초기화
    /// </summary>
    public void InitializeHealthSlider()
    {
        // 1. Inspector에서 할당된 슬라이더 우선 사용
        if (healthSlider == null && autoFindSlider)
        {
            // 2. 자동으로 컴포넌트에서 찾기
            healthSlider = GetComponent<Slider>();
            
            if (healthSlider == null)
            {
                // 3. "Health Slider" 이름으로 찾기 (기존 방식 호환)
                GameObject healthSliderObject = GameObject.Find(HEALTH_SLIDER_TEXT);
                if (healthSliderObject != null)
                {
                    healthSlider = healthSliderObject.GetComponent<Slider>();
                }
            }
        }
        
        if (healthSlider != null)
        {
            isInitialized = true;
            if (showDebugLogs)
            {
                Debug.Log("✅ [HealthUI] Health Slider 초기화 완료");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("⚠️ [HealthUI] Health Slider를 찾을 수 없습니다.");
            }
        }
    }
    
    /// <summary>
    /// PlayerHealth와 연결
    /// </summary>
    private void ConnectToPlayerHealth()
    {
        playerHealth = PlayerHealth.Instance;
        if (playerHealth != null)
        {
            // 즉시 현재 체력 표시
            UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            isHealthSubscribed = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"🔗 [HealthUI] PlayerHealth 연결 완료: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}");
            }
        }
    }
    
    /// <summary>
    /// 체력 UI 업데이트 (외부 호출용)
    /// </summary>
    public void UpdateHealthUI(int currentHealth, int maxHealth)
    {
        if (!isInitialized || healthSlider == null) return;
        
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
        
        if (showDebugLogs)
        {
            Debug.Log($"❤️ [HealthUI] 체력 UI 업데이트: {currentHealth}/{maxHealth} ({(float)currentHealth/maxHealth*100:F0}%)");
        }
    }
    
    /// <summary>
    /// 체력 비율로 업데이트 (0.0 ~ 1.0)
    /// </summary>
    public void UpdateHealthUI(float healthRatio)
    {
        if (!isInitialized || healthSlider == null) return;
        
        healthSlider.value = healthSlider.maxValue * Mathf.Clamp01(healthRatio);
        
        if (showDebugLogs)
        {
            Debug.Log($"❤️ [HealthUI] 체력 비율 업데이트: {healthRatio*100:F0}%");
        }
    }
    
    /// <summary>
    /// 최대 체력 설정
    /// </summary>
    public void SetMaxHealth(int maxHealth)
    {
        if (!isInitialized || healthSlider == null) return;
        
        healthSlider.maxValue = maxHealth;
        
        if (showDebugLogs)
        {
            Debug.Log($"🔧 [HealthUI] 최대 체력 설정: {maxHealth}");
        }
    }
    
    /// <summary>
    /// 현재 체력만 업데이트
    /// </summary>
    public void SetCurrentHealth(int currentHealth)
    {
        if (!isInitialized || healthSlider == null) return;
        
        healthSlider.value = currentHealth;
        
        if (showDebugLogs)
        {
            Debug.Log($"❤️ [HealthUI] 현재 체력 업데이트: {currentHealth}");
        }
    }
    
    /// <summary>
    /// Health Slider가 준비되었는지 확인
    /// </summary>
    public bool IsReady => isInitialized && healthSlider != null;
    
    /// <summary>
    /// 현재 Health Slider 참조 반환 (읽기 전용)
    /// </summary>
    public Slider HealthSlider => healthSlider;
    
    private void OnDisable()
    {
        isHealthSubscribed = false;
    }
    
    private void OnDestroy()
    {
        isHealthSubscribed = false;
    }
}
