using TMPro;
using UnityEngine;

public class LevelUI : MonoBehaviour
{
    private TextMeshProUGUI _levelText;
    private bool _isSubscribed = false;

    private void Awake()
    {
        _levelText = GetComponent<TextMeshProUGUI>();
        // 시작할 때 텍스트를 잠시 비워두어 깜빡임을 방지할 수 있습니다.
        _levelText.text = ""; 
    }

    // 플레이어가 생성될 때까지 매 프레임 확인합니다.
    private void Update()
    {
        // ⭐ [Phase 1] PlayerDataManager 우선, PlayerLevel 백업 사용
        if (!_isSubscribed)
        {
            if (PlayerDataManager.Instance != null)
            {
                // PlayerDataManager 이벤트에 연결 (int 매개변수 버전 사용)
                PlayerDataManager.Instance.OnLevelChanged += UpdateLevelText;
                _isSubscribed = true;
                UpdateLevelText(PlayerDataManager.Instance.CurrentLevel);
            }
            else if (PlayerLevel.Instance != null)
            {
                // 백업: 기존 PlayerLevel 사용 (호환성, 매개변수 없는 버전 사용)
                PlayerLevel.Instance.OnLevelChanged += UpdateLevelText;
                _isSubscribed = true;
                UpdateLevelText();
            }
        }
    }

    private void OnDisable()
    {
        // ⭐ [Phase 1] PlayerDataManager와 PlayerLevel 둘 다 안전하게 해제
        if (_isSubscribed)
        {
            if (PlayerDataManager.Instance != null)
            {
                // int 매개변수 버전 해제
                PlayerDataManager.Instance.OnLevelChanged -= UpdateLevelText;
            }
            if (PlayerLevel.Instance != null)
            {
                // 매개변수 없는 버전 해제
                PlayerLevel.Instance.OnLevelChanged -= UpdateLevelText;
            }
        }
        _isSubscribed = false; // 비활성화될 때 연결 상태를 리셋합니다.
    }

    /// <summary>
    /// PlayerDataManager 또는 PlayerLevel의 OnLevelChanged 이벤트가 호출될 때 실행되는 함수입니다.
    /// </summary>
    private void UpdateLevelText()
    {
        // ⭐ [Phase 1] PlayerDataManager 우선, PlayerLevel 백업 사용
        if (PlayerDataManager.Instance != null)
        {
            _levelText.text = $"Lv. {PlayerDataManager.Instance.CurrentLevel}";
        }
        else if (PlayerLevel.Instance != null)
        {
            _levelText.text = $"Lv. {PlayerLevel.Instance.CurrentLevel}";
        }
    }
    
    /// <summary>
    /// PlayerDataManager의 OnLevelChanged 이벤트용 오버로드 (int 매개변수 받음)
    /// </summary>
    private void UpdateLevelText(int newLevel)
    {
        _levelText.text = $"Lv. {newLevel}";
    }
} 