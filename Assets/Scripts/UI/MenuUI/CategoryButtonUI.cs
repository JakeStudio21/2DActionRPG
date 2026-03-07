using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 카테고리 버튼 UI 컴포넌트
/// 배경 이미지, Coming Soon 표시 등 관리
/// </summary>
public class CategoryButtonUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image backgroundImage;           // 배경 이미지
    public Image overlayImage;              // Coming Soon 오버레이 (어두운 반투명)
    public Image iconImage;                 // 카테고리 아이콘 (선택)
    public TextMeshProUGUI nameText;        // 카테고리 이름
    public TextMeshProUGUI comingSoonText;  // "Coming Soon" 텍스트
    public GameObject lockIcon;             // 자물쇠 아이콘 (선택)
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
    /// 카테고리 이름 설정
    /// </summary>
    public void SetName(string text)
    {
        if (nameText != null)
        {
            nameText.text = text;
        }
    }
    
    /// <summary>
    /// Coming Soon 표시
    /// </summary>
    public void SetComingSoon(bool isComingSoon)
    {
        if (overlayImage != null)
        {
            overlayImage.gameObject.SetActive(isComingSoon);
        }
        
        if (comingSoonText != null)
        {
            comingSoonText.gameObject.SetActive(isComingSoon);
        }
    }
    
    /// <summary>
    /// 잠금 표시
    /// </summary>
    public void SetLocked(bool isLocked)
    {
        if (lockIcon != null)
        {
            lockIcon.SetActive(isLocked);
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

