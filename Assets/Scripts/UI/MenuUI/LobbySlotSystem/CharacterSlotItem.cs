using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// 개별 캐릭터 슬롯 UI 아이템
/// 책임: 단일 슬롯 UI 표시, 버튼 이벤트 처리
/// </summary>
public class CharacterSlotItem : MonoBehaviour
{
    [Header("=== UI 요소 ===")]
    public Image characterIcon;
    public TMP_Text characterName;
    public TMP_Text characterLevel;
    public TMP_Text characterClass;
    public Button deleteButton;
    public GameObject emptyPanel;
    public Button slotButton;
    
    // 내부 상태
    private int slotIndex = -1;
    private CharacterSlotController controller;
    
    /// <summary>
    /// 초기화
    /// </summary>
    public void Initialize(int index, CharacterSlotController slotController)
    {
        slotIndex = index;
        controller = slotController;
        
        // 버튼 이벤트 연결
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotButtonClicked);
        }
        
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }
        
    }
    
    /// <summary>
    /// 캐릭터 정보 표시
    /// </summary>
    public void ShowCharacter(PlayerSlotData slotData, Sprite icon, bool isSelected)
    {
        // 아이콘 설정
        if (characterIcon != null)
        {
            characterIcon.sprite = icon;
            characterIcon.gameObject.SetActive(true);
        }
        
        // 텍스트 설정
        if (characterName != null)
        {
            characterName.text = slotData.playerName;
            characterName.gameObject.SetActive(true);
        }
        
        if (characterLevel != null)
        {
            characterLevel.text = $"Lv.{slotData.level}";
            characterLevel.gameObject.SetActive(true);
        }
        
        if (characterClass != null)
        {
            characterClass.text = slotData.playerType switch
            {
                PlayerType.Assasin => "Archer",
                _ => slotData.playerType.ToString()
            };
            characterClass.gameObject.SetActive(true);
        }
        
        // 선택 상태 표시 (Unity Button의 Selected Color 활용)
        if (slotButton != null)
        {
            if (isSelected)
            {
                slotButton.Select();
            }
            else
            {
                // 선택 해제
                if (slotButton == EventSystem.current.currentSelectedGameObject?.GetComponent<Button>())
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
            }
        }
        
        // 삭제 버튼 활성화
        if (deleteButton != null)
        {
            deleteButton.gameObject.SetActive(true);
        }
        
        // 빈 슬롯 패널 비활성화
        if (emptyPanel != null)
        {
            emptyPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 빈 슬롯 표시
    /// </summary>
    public void ShowEmpty(Sprite emptyIcon)
    {
        // 선택 해제
        if (slotButton != null && slotButton == EventSystem.current.currentSelectedGameObject?.GetComponent<Button>())
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        
        // 아이콘 설정 (빈 슬롯 아이콘)
        if (characterIcon != null)
        {
            characterIcon.sprite = emptyIcon;
            characterIcon.gameObject.SetActive(true);
        }
        
        // 텍스트 비활성화
        if (characterName != null) characterName.gameObject.SetActive(false);
        if (characterLevel != null) characterLevel.gameObject.SetActive(false);
        if (characterClass != null) characterClass.gameObject.SetActive(false);
        
        // 삭제 버튼 비활성화
        if (deleteButton != null)
        {
            deleteButton.gameObject.SetActive(false);
        }
        
        // 빈 슬롯 패널 활성화
        if (emptyPanel != null)
        {
            emptyPanel.SetActive(true);
        }
    }
    
    /// <summary>
    /// 슬롯 버튼 클릭 이벤트
    /// </summary>
    private void OnSlotButtonClicked()
    {
        if (controller != null)
        {
            controller.HandleSlotClicked(slotIndex);
        }
    }
    
    /// <summary>
    /// 삭제 버튼 클릭 이벤트
    /// </summary>
    private void OnDeleteButtonClicked()
    {
        if (controller != null)
        {
            controller.HandleDeleteClicked(slotIndex);
        }
    }
}

