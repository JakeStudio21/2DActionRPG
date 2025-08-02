using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 경험치 UI 관리 (Heart Container와 동일한 방식)
/// </summary>
public class ExpUI : MonoBehaviour
{
    [Header("경험치 UI 설정")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private bool autoFindSlider = true;
    [SerializeField] private bool showDebugLogs = true;
    
    private bool _isSubscribed = false;
    
    public bool IsReady => expSlider != null;
    
    private void Awake()
    {
        if (autoFindSlider && expSlider == null)
        {
            expSlider = GetComponentInChildren<Slider>();
            if (expSlider == null)
            {
                expSlider = GetComponent<Slider>();
            }
        }
        
        if (expSlider == null && showDebugLogs)
        {
            Debug.LogWarning("⚠️ [ExpUI] Slider를 찾을 수 없습니다!");
        }
    }
    
    private void Update()
    {
        if (!_isSubscribed && PlayerDataManager.Instance != null)
        {
            // PlayerDataManager 이벤트에 연결
            PlayerDataManager.Instance.OnExpChanged += UpdateExpUI;
            _isSubscribed = true;
            
            // 즉시 현재 경험치 표시
            UpdateExpUI(PlayerDataManager.Instance.CurrentExp, PlayerDataManager.Instance.ExpToNextLevel);
            
            if (showDebugLogs)
                Debug.Log($"🎯 [ExpUI] PlayerDataManager 연결 완료, 현재 경험치: {PlayerDataManager.Instance.CurrentExp}/{PlayerDataManager.Instance.ExpToNextLevel}");
        }
    }
    
    private void OnDisable()
    {
        if (_isSubscribed && PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnExpChanged -= UpdateExpUI;
        }
        _isSubscribed = false;
    }
    
    /// <summary>
    /// 경험치 UI 업데이트
    /// </summary>
    public void UpdateExpUI(int currentExp, int expToNextLevel)
    {
        if (expSlider == null) return;
        
        float fillAmount = expToNextLevel > 0 ? (float)currentExp / expToNextLevel : 0f;
        expSlider.value = fillAmount;
        
        if (showDebugLogs)
            Debug.Log($"💫 [ExpUI] 경험치 UI 업데이트: {currentExp}/{expToNextLevel} ({fillAmount:P1})");
    }
}
