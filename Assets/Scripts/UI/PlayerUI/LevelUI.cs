using TMPro;
using UnityEngine;

public class LevelUI : MonoBehaviour
{
    private TextMeshProUGUI _levelText;
    private bool _isSubscribed = false;

    private void Awake()
    {
        _levelText = GetComponent<TextMeshProUGUI>();
        _levelText.text = "Lv. 1"; // 기본값 표시
    }

    private void Update()
    {
        if (!_isSubscribed)
        {
            if (PlayerDataManager.Instance != null)
            {
                // PlayerDataManager 이벤트에 연결
                PlayerDataManager.Instance.OnLevelChanged += UpdateLevelText;
                _isSubscribed = true;
                
                // 🔧 즉시 현재 레벨 표시
                UpdateLevelText(PlayerDataManager.Instance.CurrentLevel);
                
                Debug.Log($"🎯 [LevelUI] PlayerDataManager 연결 완료, 현재 레벨: {PlayerDataManager.Instance.CurrentLevel}");
            }
        }
    }

    private void OnDisable()
    {
        if (_isSubscribed && PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnLevelChanged -= UpdateLevelText;
        }
        _isSubscribed = false;
    }

    /// <summary>
    /// PlayerDataManager의 OnLevelChanged 이벤트용 (int 매개변수)
    /// </summary>
    private void UpdateLevelText(int newLevel)
    {
        _levelText.text = $"Lv. {newLevel}";
        Debug.Log($"🎯 [LevelUI] 레벨 UI 업데이트: Lv. {newLevel}");
    }
} 