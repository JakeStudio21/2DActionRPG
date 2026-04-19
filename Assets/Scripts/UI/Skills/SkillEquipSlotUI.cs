using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// 스킬 장착 슬롯 UI (하단)
/// Phase 3: 액티브 2개, 패시브 3개 슬롯
/// </summary>
public class SkillEquipSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("🎯 슬롯 정보")]
    [SerializeField] private int slotIndex = 0;
    [SerializeField] private bool isActiveSlot = true; // true: 액티브, false: 패시브
    
    [Header("🎨 UI 요소")]
    [SerializeField] private Image skillIcon;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI slotNumberText;
    [SerializeField] private TextMeshProUGUI skillLevelText;
    [SerializeField] private GameObject emptyOverlay;
    
    [Header("🎨 기본 스프라이트")]
    [SerializeField] private Sprite emptySlotSprite;
    
    private SkillInstance equippedSkill;
    private SkillTabController tabController;
    
    
    void Start()
    {
        // 슬롯 번호 표시
        if (slotNumberText != null)
        {
            string slotType = isActiveSlot ? "액티브" : "패시브";
            slotNumberText.text = $"{slotType} {slotIndex + 1}";
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// 초기화
    /// </summary>
    public void Setup(int index, bool isActive, SkillTabController controller)
    {
        slotIndex = index;
        isActiveSlot = isActive;
        tabController = controller;
        
        UpdateUI();
    }
    
    /// <summary>
    /// 슬롯에 스킬 장착
    /// </summary>
    public void SetSkill(SkillInstance skill)
    {
        equippedSkill = skill;
        UpdateUI();
    }
    
    /// <summary>
    /// UI 갱신
    /// </summary>
    public void UpdateUI()
    {
        if (equippedSkill != null && equippedSkill.skillData != null)
        {
            // 스킬 아이콘 표시
            if (skillIcon != null)
            {
                if (equippedSkill.skillData.icon != null)
                {
                    skillIcon.sprite = equippedSkill.skillData.icon;
                    skillIcon.color = Color.white;
                }
                else
                {
                    skillIcon.sprite = emptySlotSprite;
                    skillIcon.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
            }
            
            // 레벨 표시
            if (skillLevelText != null)
            {
                skillLevelText.text = $"Lv.{equippedSkill.currentLevel}";
                skillLevelText.gameObject.SetActive(true);
            }
            
            // Empty 오버레이 끄기
            if (emptyOverlay != null)
                emptyOverlay.SetActive(false);
            
            // 배경 색상 (장착됨)
            if (backgroundImage != null)
                backgroundImage.color = new Color(1f, 0.9f, 0.7f, 1f);
        }
        else
        {
            // 빈 슬롯
            if (skillIcon != null)
            {
                skillIcon.sprite = emptySlotSprite;
                skillIcon.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
            
            if (skillLevelText != null)
                skillLevelText.gameObject.SetActive(false);
            
            if (emptyOverlay != null)
                emptyOverlay.SetActive(true);
            
            if (backgroundImage != null)
                backgroundImage.color = Color.white;
        }
        
    }
    
    /// <summary>
    /// 슬롯 클릭 (장착된 스킬 해제 또는 상세 정보 표시)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (equippedSkill != null && tabController != null)
        {
            // 우클릭: 해제
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                tabController.UnequipSkill(equippedSkill);
                
            }
            // 좌클릭: 상세 정보 표시
            else
            {
                tabController.ShowSkillDetail(equippedSkill);
            }
        }
    }
    
    /// <summary>
    /// 현재 장착된 스킬 반환
    /// </summary>
    public SkillInstance GetEquippedSkill()
    {
        return equippedSkill;
    }
    
    /// <summary>
    /// 슬롯이 비어있는지 확인
    /// </summary>
    public bool IsEmpty()
    {
        return equippedSkill == null;
    }
}
