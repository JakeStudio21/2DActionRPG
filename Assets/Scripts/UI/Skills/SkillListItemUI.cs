using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 우측 스킬 리스트에 표시되는 개별 스킬 아이템 UI
/// Phase 3-Revision: 각 스킬마다 레벨업/장착 버튼 포함
/// </summary>
public class SkillListItemUI : MonoBehaviour
{
    [Header("🎨 기본 UI 요소")]
    [SerializeField] private Image skillIcon;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI skillLevelText;
    [SerializeField] private TextMeshProUGUI skillTypeText;
    [SerializeField] private GameObject lockOverlay;
    
    [Header("🎮 버튼")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI upgradeButtonText;
    [SerializeField] private Button equipButton;
    [SerializeField] private TextMeshProUGUI equipButtonText;
    
    [Header("🎨 상태별 색상")]
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color equippedColor = new Color(1f, 0.8f, 0.3f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.8f, 0.9f, 1f, 1f);
    
    [Header("🎨 미해금 스킬 Material")]
    [Tooltip("미해금 스킬 아이콘에 적용할 Grayscale Material (Assets/Materials/UI/UI_Grayscale)")]
    [SerializeField] private Material grayscaleMaterial;
    
    [Header("🎨 전체 UI 회색 처리 설정")]
    [Tooltip("전체 Prefab (텍스트+아이콘+배경+버튼)을 회색으로 처리")]
    [SerializeField] private bool applyGrayscaleToAllChildren = true;
    
    [Header("🎚️ 밝기 & 투명도 조절")]
    [Tooltip("미해금 스킬의 밝기 (0.0 = 완전히 어두움, 1.0 = 원본 밝기)")]
    [SerializeField] [Range(0f, 1f)] private float lockedBrightness = 0.6f;
    
    [Tooltip("미해금 스킬의 투명도 (0.0 = 완전히 투명, 1.0 = 불투명)")]
    [SerializeField] [Range(0f, 1f)] private float lockedOpacity = 1f;
    
    // 원본 상태 저장용 (복구 시 사용)
    private Dictionary<Image, Material> originalImageMaterials = new Dictionary<Image, Material>();
    private Dictionary<Image, Color> originalImageColors = new Dictionary<Image, Color>();
    private Dictionary<TextMeshProUGUI, Color> originalTextColors = new Dictionary<TextMeshProUGUI, Color>();
    private bool isGrayscaleApplied = false;
    
    [Header("🔗 데이터")]
    private SkillInstance skillInstance;
    private int currentPlayerLevel;
    private SkillTabController tabController;
    private bool isSelected = false;
    
    [Header("🔧 디버그")]
    public bool showDebugLogs = false;
    
    void Awake()
    {
        // 버튼 이벤트 연결
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradeButtonClicked);
        
        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        
        // 원본 Material/Color 저장 (최초 1회)
        SaveOriginalColors();
    }
    
    /// <summary>
    /// 원본 Material과 Color 저장 (복구용)
    /// </summary>
    private void SaveOriginalColors()
    {
        // 모든 Image 컴포넌트 저장
        var images = GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (!originalImageMaterials.ContainsKey(img))
                originalImageMaterials[img] = img.material;
            if (!originalImageColors.ContainsKey(img))
                originalImageColors[img] = img.color;
        }
        
        // 모든 TextMeshProUGUI 컴포넌트 저장
        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in texts)
        {
            if (!originalTextColors.ContainsKey(text))
                originalTextColors[text] = text.color;
        }
    }
    
    /// <summary>
    /// 스킬 아이템 초기화
    /// </summary>
    public void Setup(SkillInstance skill, int playerLevel, SkillTabController controller)
    {
        skillInstance = skill;
        currentPlayerLevel = playerLevel;
        tabController = controller;
        
        if (skill == null || skill.skillData == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        UpdateUI();
    }
    
    /// <summary>
    /// UI 갱신
    /// </summary>
    public void UpdateUI()
    {
        if (skillInstance == null || skillInstance.skillData == null) return;
        
        // 기본 정보
        if (skillNameText != null)
            skillNameText.text = skillInstance.skillData.skillName;
        
        // 아이콘
        if (skillIcon != null && skillInstance.skillData.icon != null)
        {
            skillIcon.sprite = skillInstance.skillData.icon;
        }
        
        // 스킬 타입
        if (skillTypeText != null)
        {
            if (skillInstance.IsActiveSkill)
            {
                var activeData = skillInstance.skillData as ActiveSkillData;
                skillTypeText.text = activeData.skillType.ToKoreanString();
            }
            else if (skillInstance.IsPassiveSkill)
            {
                var passiveData = skillInstance.skillData as PassiveSkillData;
                skillTypeText.text = passiveData.passiveType.ToKoreanString();
            }
        }
        
        // 레벨 텍스트
        if (skillLevelText != null)
        {
            if (skillInstance.currentLevel > 0)
            {
                skillLevelText.text = $"Lv.{skillInstance.currentLevel}/{skillInstance.skillData.maxLevel}";
                skillLevelText.gameObject.SetActive(true);
            }
            else
            {
                skillLevelText.text = "미해금";
                skillLevelText.gameObject.SetActive(true);
            }
        }
        
        // 잠금 상태
        bool isLocked = currentPlayerLevel < skillInstance.skillData.unlockLevel;
        
        if (lockOverlay != null)
        {
            lockOverlay.SetActive(isLocked);
        }
        
        // 배경 색상
        UpdateBackgroundColor(isLocked);
        
        // 버튼 상태
        UpdateButtonStates(isLocked);
        
        if (showDebugLogs)
            Debug.Log($"🎨 [SkillListItemUI] {skillInstance.skillData.skillName} UI 갱신 완료");
    }
    
    /// <summary>
    /// 배경 색상 업데이트
    /// </summary>
    private void UpdateBackgroundColor(bool isLocked)
    {
        if (backgroundImage == null) return;
        
        if (isLocked)
        {
            backgroundImage.color = lockedColor;
            
            // ⭐ 전체 UI 요소 회색 처리
            if (applyGrayscaleToAllChildren && !isGrayscaleApplied)
            {
                ApplyGrayscaleToAllChildren();
                isGrayscaleApplied = true;
                
                if (showDebugLogs)
                    Debug.Log($"🎨 [SkillListItemUI] {skillInstance.skillData.skillName}: 전체 Prefab 회색 처리 완료 (텍스트+아이콘+배경+버튼)");
            }
        }
        else
        {
            // ⭐ 회색 처리 복구 (원본 색상으로)
            if (isGrayscaleApplied)
            {
                RestoreOriginalColors();
                isGrayscaleApplied = false;
                
                if (showDebugLogs)
                    Debug.Log($"🎨 [SkillListItemUI] {skillInstance.skillData.skillName}: 원본 색상 복구 완료");
            }
            
            if (isSelected)
            {
                backgroundImage.color = selectedColor;
            }
            else if (skillInstance.isEquipped)
            {
                backgroundImage.color = equippedColor;
            }
            else
            {
                backgroundImage.color = unlockedColor;
            }
        }
    }
    
    /// <summary>
    /// 버튼 상태 업데이트 (Phase 3-Revision 핵심)
    /// </summary>
    private void UpdateButtonStates(bool isLocked)
    {
        // 레벨업 버튼
        if (upgradeButton != null)
        {
            bool canUpgrade = !skillInstance.IsMaxLevel && 
                             !isLocked &&
                             tabController != null &&
                             tabController.CanAffordSP(skillInstance.GetRequiredSPForNextLevel());
            
            upgradeButton.interactable = canUpgrade;
            
            if (upgradeButtonText != null)
            {
                if (skillInstance.IsMaxLevel)
                    upgradeButtonText.text = "만렙";
                else if (isLocked)
                    upgradeButtonText.text = $"Lv.{skillInstance.skillData.unlockLevel}";
                else
                {
                    int requiredSP = skillInstance.GetRequiredSPForNextLevel();
                    
                    // ⭐ 첫 해금(Lv.0→Lv.1)인지 확인
                    bool isFirstUnlock = (skillInstance.currentLevel == 0);
                    
                    if (!canUpgrade)
                        upgradeButtonText.text = $"SP부족 ({requiredSP})";
                    else if (isFirstUnlock)
                        upgradeButtonText.text = $"활성화 (SP:{requiredSP})";
                    else
                        upgradeButtonText.text = $"레벨업 (SP:{requiredSP})";
                }
            }
        }
        
        // 장착 버튼
        if (equipButton != null)
        {
            bool canEquip = skillInstance.IsUnlocked && !isLocked;
            equipButton.interactable = canEquip;
            
            if (equipButtonText != null)
            {
                if (skillInstance.isEquipped)
                    equipButtonText.text = "장착중";
                else if (!skillInstance.IsUnlocked)
                    equipButtonText.text = "미해금";
                else
                    equipButtonText.text = "장착";
            }
        }
    }
    
    /// <summary>
    /// 선택 상태 설정 (하단 상세 패널 표시용)
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateUI();
    }
    
    /// <summary>
    /// 레벨업 버튼 클릭 (Phase 3-Revision: 우측 리스트에서 바로 처리)
    /// </summary>
    private void OnUpgradeButtonClicked()
    {
        if (skillInstance == null || tabController == null) return;
        
        bool success = tabController.TryUpgradeSkill(skillInstance);
        
        if (success)
        {
            UpdateUI();
            
            // 하단 상세 패널도 갱신 (현재 선택된 스킬인 경우)
            if (isSelected)
            {
                tabController.ShowSkillDetail(skillInstance);
            }
            
            if (showDebugLogs)
                Debug.Log($"✅ [SkillListItemUI] {skillInstance.skillData.skillName} 레벨업 성공!");
        }
    }
    
    /// <summary>
    /// 장착 버튼 클릭 (Phase 3-Revision: 우측 리스트에서 바로 처리)
    /// </summary>
    private void OnEquipButtonClicked()
    {
        if (skillInstance == null || !skillInstance.IsUnlocked) return;
        
        if (tabController != null)
        {
            tabController.EquipSkill(skillInstance);
            
            if (showDebugLogs)
                Debug.Log($"🎯 [SkillListItemUI] {skillInstance.skillData.skillName} 장착 시도");
        }
    }
    
    /// <summary>
    /// 아이템 클릭 (하단 상세 패널 표시)
    /// </summary>
    public void OnItemClicked()
    {
        if (skillInstance == null || skillInstance.skillData == null) return;
        
        if (tabController != null)
        {
            tabController.ShowSkillDetail(skillInstance);
            tabController.SetSelectedSkillItem(this);
            
            if (showDebugLogs)
                Debug.Log($"🖱️ [SkillListItemUI] {skillInstance.skillData.skillName} 선택됨");
        }
    }
    
    /// <summary>
    /// 외부에서 플레이어 레벨 업데이트 시 호출
    /// </summary>
    public void UpdatePlayerLevel(int newPlayerLevel)
    {
        currentPlayerLevel = newPlayerLevel;
        UpdateUI();
    }
    
    /// <summary>
    /// Button 컴포넌트로 클릭 이벤트 받기 (Unity Editor 연결용)
    /// </summary>
    public void OnItemClickedFromButton()
    {
        OnItemClicked();
    }
    
    // ========================================
    // 🎨 전체 UI 회색 처리 메서드
    // ========================================
    
    /// <summary>
    /// 전체 Prefab의 모든 UI 요소를 회색으로 변환
    /// (텍스트, 아이콘, 배경, 버튼 등 모두 포함)
    /// </summary>
    private void ApplyGrayscaleToAllChildren()
    {
        // 1. 모든 Image에 Grayscale Material 적용
        var images = GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (grayscaleMaterial != null)
            {
                img.material = grayscaleMaterial;
                // ⭐ Inspector에서 조절 가능한 밝기 & 투명도 적용
                img.color = new Color(
                    img.color.r * lockedBrightness, 
                    img.color.g * lockedBrightness, 
                    img.color.b * lockedBrightness, 
                    img.color.a * lockedOpacity
                );
            }
            else
            {
                // Material 없으면 Color만 회색으로
                float gray = img.color.grayscale * lockedBrightness;
                img.color = new Color(gray, gray, gray, img.color.a * lockedOpacity);
            }
        }
        
        // 2. 모든 TextMeshProUGUI를 회색으로 변환
        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in texts)
        {
            // ⭐ Inspector에서 조절 가능한 밝기 & 투명도 적용
            float gray = text.color.grayscale * lockedBrightness;
            text.color = new Color(gray, gray, gray, text.color.a * lockedOpacity);
        }
        
        if (showDebugLogs)
            Debug.Log($"🎨 [SkillListItemUI] 회색 처리 완료 - 밝기: {lockedBrightness:F2}, 투명도: {lockedOpacity:F2}");
    }
    
    /// <summary>
    /// 원본 색상으로 복구
    /// </summary>
    private void RestoreOriginalColors()
    {
        // 1. Image 복구
        foreach (var kvp in originalImageMaterials)
        {
            if (kvp.Key != null)
                kvp.Key.material = kvp.Value;
        }
        
        foreach (var kvp in originalImageColors)
        {
            if (kvp.Key != null)
                kvp.Key.color = kvp.Value;
        }
        
        // 2. TextMeshProUGUI 복구
        foreach (var kvp in originalTextColors)
        {
            if (kvp.Key != null)
                kvp.Key.color = kvp.Value;
        }
    }
}
