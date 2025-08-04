using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 🎮 플레이어 실시간 스탯 표시 UI
/// PlayerRuntimeStats와 연동하여 스탯 변경 시 즉시 업데이트
/// </summary>
public class PlayerStatsUI : MonoBehaviour
{
    [Header("📊 스탯 UI 요소들")]
    [SerializeField] private TextMeshProUGUI attackDamageText;
    [SerializeField] private TextMeshProUGUI moveSpeedText;
    [SerializeField] private TextMeshProUGUI maxHealthText;
    [SerializeField] private TextMeshProUGUI criticalChanceText;
    [SerializeField] private TextMeshProUGUI defenseText;
    
    [Header("🎨 변경 효과")]
    [SerializeField] private Color increaseColor = Color.green;
    [SerializeField] private Color decreaseColor = Color.red;
    [SerializeField] private float flashDuration = 0.5f;
    
    [Header("🔧 설정")]
    [SerializeField] private bool autoFindComponents = true;
    [SerializeField] private bool showDebugLogs = true;
    
    private PlayerRuntimeStats playerRuntimeStats;
    private bool isSubscribed = false;
    
    private void Start()
    {
        if (autoFindComponents)
        {
            AutoFindUIComponents();
        }
        
        ConnectToPlayerRuntimeStats();
    }
    
    /// <summary>
    /// 🔍 UI 컴포넌트 자동 찾기
    /// </summary>
    private void AutoFindUIComponents()
    {
        if (attackDamageText == null)
            attackDamageText = transform.Find("AttackDamage/Text")?.GetComponent<TextMeshProUGUI>();
        
        if (moveSpeedText == null)
            moveSpeedText = transform.Find("MoveSpeed/Text")?.GetComponent<TextMeshProUGUI>();
        
        if (maxHealthText == null)
            maxHealthText = transform.Find("MaxHealth/Text")?.GetComponent<TextMeshProUGUI>();
        
        if (criticalChanceText == null)
            criticalChanceText = transform.Find("CriticalChance/Text")?.GetComponent<TextMeshProUGUI>();
        
        if (defenseText == null)
            defenseText = transform.Find("Defense/Text")?.GetComponent<TextMeshProUGUI>();
        
        if (showDebugLogs)
        {
            Debug.Log($"🔍 [PlayerStatsUI] UI 컴포넌트 찾기 결과:");
            Debug.Log($"   - 공격력: {(attackDamageText != null ? "✅" : "❌")}");
            Debug.Log($"   - 이동속도: {(moveSpeedText != null ? "✅" : "❌")}");
            Debug.Log($"   - 최대체력: {(maxHealthText != null ? "✅" : "❌")}");
            Debug.Log($"   - 크리티컬: {(criticalChanceText != null ? "✅" : "❌")}");
            Debug.Log($"   - 방어력: {(defenseText != null ? "✅" : "❌")}");
        }
    }
    
    /// <summary>
    /// 🔗 PlayerRuntimeStats 연결
    /// </summary>
    private void ConnectToPlayerRuntimeStats()
    {
        playerRuntimeStats = FindObjectOfType<PlayerRuntimeStats>();
        
        if (playerRuntimeStats != null && !isSubscribed)
        {
            // 개별 스탯 변경 이벤트 구독
            playerRuntimeStats.OnAttackDamageChanged += OnAttackDamageChanged;
            playerRuntimeStats.OnMoveSpeedChanged += OnMoveSpeedChanged;
            playerRuntimeStats.OnMaxHealthChanged += OnMaxHealthChanged;
            playerRuntimeStats.OnCriticalChanceChanged += OnCriticalChanceChanged;
            playerRuntimeStats.OnDefenseChanged += OnDefenseChanged;
            
            // 전체 스탯 재계산 이벤트 구독
            playerRuntimeStats.OnStatsRecalculated += RefreshAllStats;
            
            isSubscribed = true;
            
            // 초기 스탯 표시
            RefreshAllStats();
            
            if (showDebugLogs)
                Debug.Log($"🎯 [PlayerStatsUI] PlayerRuntimeStats 연결 완료");
        }
        else if (playerRuntimeStats == null)
        {
            Debug.LogWarning("⚠️ [PlayerStatsUI] PlayerRuntimeStats를 찾을 수 없습니다!");
        }
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (isSubscribed && playerRuntimeStats != null)
        {
            playerRuntimeStats.OnAttackDamageChanged -= OnAttackDamageChanged;
            playerRuntimeStats.OnMoveSpeedChanged -= OnMoveSpeedChanged;
            playerRuntimeStats.OnMaxHealthChanged -= OnMaxHealthChanged;
            playerRuntimeStats.OnCriticalChanceChanged -= OnCriticalChanceChanged;
            playerRuntimeStats.OnDefenseChanged -= OnDefenseChanged;
            playerRuntimeStats.OnStatsRecalculated -= RefreshAllStats;
        }
    }
    
    /// <summary>
    /// 📊 모든 스탯 새로고침
    /// </summary>
    private void RefreshAllStats()
    {
        if (playerRuntimeStats == null) return;
        
        UpdateAttackDamageText(playerRuntimeStats.FinalAttackDamage);
        UpdateMoveSpeedText(playerRuntimeStats.FinalMoveSpeed);
        UpdateMaxHealthText(playerRuntimeStats.FinalMaxHealth);
        UpdateCriticalChanceText(playerRuntimeStats.FinalCriticalChance);
        UpdateDefenseText(playerRuntimeStats.FinalDefense);
        
        if (showDebugLogs)
            Debug.Log($"📊 [PlayerStatsUI] 모든 스탯 새로고침 완료");
    }
    
    #region 개별 스탯 변경 이벤트 핸들러
    
    private void OnAttackDamageChanged(float oldValue, float newValue)
    {
        UpdateAttackDamageText(newValue);
        FlashText(attackDamageText, newValue > oldValue);
    }
    
    private void OnMoveSpeedChanged(float oldValue, float newValue)
    {
        UpdateMoveSpeedText(newValue);
        FlashText(moveSpeedText, newValue > oldValue);
    }
    
    private void OnMaxHealthChanged(float oldValue, float newValue)
    {
        UpdateMaxHealthText(newValue);
        FlashText(maxHealthText, newValue > oldValue);
    }
    
    private void OnCriticalChanceChanged(float oldValue, float newValue)
    {
        UpdateCriticalChanceText(newValue);
        FlashText(criticalChanceText, newValue > oldValue);
    }
    
    private void OnDefenseChanged(float oldValue, float newValue)
    {
        UpdateDefenseText(newValue);
        FlashText(defenseText, newValue > oldValue);
    }
    
    #endregion
    
    #region UI 텍스트 업데이트 메서드들
    
    private void UpdateAttackDamageText(float value)
    {
        if (attackDamageText != null)
            attackDamageText.text = $"⚔️ {value:F1}";
    }
    
    private void UpdateMoveSpeedText(float value)
    {
        if (moveSpeedText != null)
            moveSpeedText.text = $"🏃 {value:F1}";
    }
    
    private void UpdateMaxHealthText(float value)
    {
        if (maxHealthText != null)
            maxHealthText.text = $"❤️ {value:F0}";
    }
    
    private void UpdateCriticalChanceText(float value)
    {
        if (criticalChanceText != null)
            criticalChanceText.text = $"🎯 {value:P1}";
    }
    
    private void UpdateDefenseText(float value)
    {
        if (defenseText != null)
            defenseText.text = $"🛡️ {value:F1}";
    }
    
    #endregion
    
    /// <summary>
    /// 🎨 텍스트 깜빡임 효과 (스탯 변경 시)
    /// </summary>
    private void FlashText(TextMeshProUGUI text, bool isIncrease)
    {
        if (text == null) return;
        
        Color targetColor = isIncrease ? increaseColor : decreaseColor;
        StartCoroutine(FlashTextCoroutine(text, targetColor));
    }
    
    private System.Collections.IEnumerator FlashTextCoroutine(TextMeshProUGUI text, Color flashColor)
    {
        Color originalColor = text.color;
        
        // 색상 변경
        text.color = flashColor;
        
        // 지속 시간 대기
        yield return new WaitForSeconds(flashDuration);
        
        // 원래 색상으로 복구
        text.color = originalColor;
    }
}
