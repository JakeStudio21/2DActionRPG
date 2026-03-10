using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ⚡ 콘텐츠 입장 제한 경고 팝업
/// - 스태미나 부족
/// - 던전 입장 횟수 소진
/// </summary>
public class ContentEntryWarningPopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private GameObject blockerPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button confirmButton;
    
    [Header("Content")]
    [SerializeField] private string defaultTitle = "입장 불가";
    
    private System.Action onConfirmCallback;
    
    private void Awake()
    {
        // 확인 버튼 이벤트 연결
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        
        // 블로커 없으면 자동 생성 (반투명 검정, 클릭 차단)
        if (blockerPanel == null)
        {
            blockerPanel = CreateBlockerPanel();
        }
        
        // 초기 상태: 숨김
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
        
        if (blockerPanel != null)
        {
            blockerPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 반투명 검정 블로커 패널 생성 (배경 클릭 차단)
    /// </summary>
    private GameObject CreateBlockerPanel()
    {
        var blocker = new GameObject("BlockerPanel");
        blocker.transform.SetParent(transform, false);
        
        var rect = blocker.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();
        
        var image = blocker.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.6f);
        image.raycastTarget = true;
        
        return blocker;
    }
    
    /// <summary>
    /// 스태미나 부족 팝업 표시
    /// </summary>
    public void ShowStaminaInsufficient(int required, int current)
    {
        string title = "스태미나 부족";
        string message = $"스태미나가 부족합니다!\n\n";
        message += $"필요: <color=#FF6B6B>{required}</color>\n";
        message += $"보유: <color=#FFFF00>{current}</color> / 50";
        
        Show(title, message);
    }
    
    /// <summary>
    /// 던전 입장 횟수 소진 팝업 표시
    /// </summary>
    public void ShowDungeonEntryLimit(string dungeonName, int remainCount, int tickets)
    {
        string title = "입장 횟수 소진";
        string message = $"<color=#FFD700>{dungeonName}</color>\n입장 횟수를 모두 소진했습니다!\n\n";
        message += $"기본 횟수: <color=#FF6B6B>{remainCount}/3</color>\n";
        
        if (tickets > 0)
        {
            message += $"보유 티켓: <color=#00FF00>{tickets}장</color>";
        }
        else
        {
            message += $"보유 티켓: <color=#888888>0장</color>";
        }
        
        Show(title, message);
    }
    
    /// <summary>
    /// 범용 메시지 표시
    /// </summary>
    public void Show(string title, string message, System.Action onConfirm = null)
    {
        if (blockerPanel != null)
        {
            blockerPanel.SetActive(true);
        }
        
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
        }
        
        if (titleText != null)
        {
            titleText.text = title;
        }
        
        if (messageText != null)
        {
            messageText.text = message;
        }
        
        onConfirmCallback = onConfirm;
    }
    
    /// <summary>
    /// 팝업 숨김
    /// </summary>
    public void Hide()
    {
        if (blockerPanel != null)
        {
            blockerPanel.SetActive(false);
        }
        
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
        
        onConfirmCallback = null;
    }
    
    /// <summary>
    /// 확인 버튼 클릭
    /// </summary>
    private void OnConfirmClicked()
    {
        onConfirmCallback?.Invoke();
        Hide();
    }
}
