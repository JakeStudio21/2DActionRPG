using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 경험치 UI 관리 (Heart Container와 동일한 방식)
/// </summary>
public class ExpUI : MonoBehaviour
{
    [Header("경험치 UI 설정")]
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI expText; // 경험치 숫자 표시 (선택사항)
    [SerializeField] private bool autoFindSlider = true;
    [SerializeField] private bool autoFindText = true; // 텍스트 자동 찾기
    
    private bool _isSubscribed = false;
    
    public bool IsReady => expSlider != null;
    
    private void Awake()
    {
        // Slider 자동 찾기
        if (autoFindSlider && expSlider == null)
        {
            expSlider = GetComponentInChildren<Slider>();
            if (expSlider == null)
            {
                expSlider = GetComponent<Slider>();
            }
        }
        
        // Text 자동 찾기 (선택사항)
        if (autoFindText && expText == null)
        {
            expText = GetComponentInChildren<TextMeshProUGUI>();
        }
        
        if (expSlider == null)
        {
            Debug.LogError($"❌ [ExpUI] Slider를 찾을 수 없습니다! Inspector에서 'Exp Slider' 필드를 확인하세요.");
        }
    }
    
    private void Update()
    {
        if (!_isSubscribed && PlayerDataManager.Instance != null)
        {
            if (expSlider == null) return;
            
            // PlayerDataManager 이벤트에 연결
            PlayerDataManager.Instance.OnExpChanged += UpdateExpUI;
            _isSubscribed = true;
            
            // 즉시 현재 경험치 표시
            UpdateExpUI(PlayerDataManager.Instance.CurrentExp, PlayerDataManager.Instance.ExpToNextLevel);
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
        
        // 텍스트 업데이트 (있으면)
        if (expText != null)
        {
            expText.text = $"{currentExp}/{expToNextLevel}";
        }
    }
}
