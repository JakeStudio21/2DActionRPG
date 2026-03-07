using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 던전 버튼 UI 컴포넌트
/// 배경 이미지, 잠금 표시 등 관리
/// </summary>
public class DungeonButtonUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image backgroundImage;           // 배경 이미지
    public Image overlayImage;              // 잠금 오버레이 (어두운 반투명)
    public Image iconImage;                 // 던전 아이콘 (선택)
    public TextMeshProUGUI nameText;        // 던전 이름
    public TextMeshProUGUI levelText;       // 권장 레벨 ("권장 Lv.10")
    public GameObject lockIcon;             // 자물쇠 아이콘
    public Button button;                   // 버튼 컴포넌트
    
    private void Awake()
    {
        // 버튼 컴포넌트 자동 가져오기
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }
    
    /// <summary>
    /// 배경 이미지 설정
    /// </summary>
    public void SetBackgroundImage(Sprite sprite)
    {
        if (backgroundImage != null && sprite != null)
        {
            backgroundImage.sprite = sprite;
        }
    }
    
    /// <summary>
    /// 던전 이름 설정
    /// </summary>
    public void SetName(string text)
    {
        if (nameText != null)
        {
            nameText.text = text;
        }
    }
    
    /// <summary>
    /// 권장 레벨 설정
    /// </summary>
    public void SetRecommendedLevel(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"권장 Lv.{level}";
        }
    }
    
    /// <summary>
    /// 잠금 상태 설정
    /// </summary>
    public void SetLocked(bool isLocked)
    {
        if (overlayImage != null)
        {
            overlayImage.gameObject.SetActive(isLocked);
        }
        
        if (lockIcon != null)
        {
            lockIcon.SetActive(isLocked);
        }
        
        if (nameText != null && isLocked)
        {
            nameText.color = Color.gray;
        }
    }
    
    /// <summary>
    /// 버튼 활성화 상태 설정
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }
}

