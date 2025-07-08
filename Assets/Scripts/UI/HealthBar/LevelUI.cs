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
        // 아직 연결되지 않았고, 플레이어 레벨 시스템이 존재한다면
        if (!_isSubscribed && PlayerLevel.Instance != null)
        {
            // 레벨 변경 이벤트에 연결합니다.
            PlayerLevel.Instance.OnLevelChanged += UpdateLevelText;
            _isSubscribed = true;

            // 연결되자마자 현재 레벨로 즉시 업데이트합니다.
            UpdateLevelText();
        }
    }

    private void OnDisable()
    {
        // 연결된 상태였고, 플레이어 레벨 시스템이 아직 존재한다면
        if (_isSubscribed && PlayerLevel.Instance != null)
        {
            // 에러 방지를 위해 연결을 해제합니다.
            PlayerLevel.Instance.OnLevelChanged -= UpdateLevelText;
        }
        _isSubscribed = false; // 비활성화될 때 연결 상태를 리셋합니다.
    }

    /// <summary>
    /// PlayerLevel의 OnLevelChanged 이벤트가 호출될 때 실행되는 함수입니다.
    /// </summary>
    private void UpdateLevelText()
    {
        if (PlayerLevel.Instance != null)
        {
            _levelText.text = $"Lv. {PlayerLevel.Instance.CurrentLevel}";
        }
    }
} 