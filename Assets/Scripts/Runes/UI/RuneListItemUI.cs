using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

/// <summary>
/// 우측 룬 목록의 개별 아이템 UI
/// ⚙️ Phase 6.5: 8종류 고유 조각 시스템
/// 
/// 역할:
/// - 룬 기본 정보 표시 (아이콘, 이름, 레벨, 한계돌파)
/// - 고유 조각 개수 표시 ("보유: X / 필요")
/// - 상태별 액션 버튼 (해금 / 강화 / 한계돌파)
/// - 장착/해제 토글 버튼
/// 
/// 중요:
/// ⚠️ 액션 실행 후 반드시 RefreshUI() 호출
/// ⚠️ 스킬 시스템과 동일하게 Button은 하위 요소로만 존재
/// </summary>
public class RuneListItemUI : MonoBehaviour
{
    #region UI 컴포넌트
    
    [Header("=== 기본 정보 ===")]
    [SerializeField] private Image runeIconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI runeNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    
    [Header("=== 한계돌파 표시 ===")]
    [SerializeField] private Image[] limitBreakStars; // 최대 5개
    [SerializeField] private Color activeStarColor = Color.yellow;
    [SerializeField] private Color inactiveStarColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    
    [Header("=== 고유 조각 표시 (Phase 6.5) ===")]
    [Tooltip("보유 / 필요 조각 표시 (예: 보유: 500 / 100)")]
    [SerializeField] private TextMeshProUGUI fragmentText;
    
    [Header("=== 버튼 (하위 GameObject) ===")]
    [Tooltip("전체 아이템 선택용 버튼 (배경에 부착)")]
    [SerializeField] private Button itemSelectButton;
    
    [Tooltip("상태에 따라 해금/강화/한계돌파 버튼으로 변경")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;
    
    [Tooltip("장착하기 / 해제하기 토글")]
    [SerializeField] private Button equipButton;
    [SerializeField] private TextMeshProUGUI equipButtonText;
    
    [Header("=== 상태 색상 ===")]
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f); // 미보유
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color equippedColor = new Color(0.8f, 1f, 0.8f, 1f);
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.5f, 1f);
    
    [Header("=== Grayscale 처리 (스킬 시스템과 동일) ===")]
    [Tooltip("미해금 룬 아이콘에 적용할 Grayscale Material (Assets/Materials/UI/UI_Grayscale)")]
    [SerializeField] private Material grayscaleMaterial;
    
    [Tooltip("전체 Prefab (텍스트+아이콘+배경+버튼)을 회색으로 처리")]
    [SerializeField] private bool applyGrayscaleToAllChildren = true;
    
    [Header("🎚️ 밝기 & 투명도 조절")]
    [Tooltip("미해금 룬의 밝기 (0.0 = 완전히 어두움, 1.0 = 원본 밝기)")]
    [SerializeField] [Range(0f, 1f)] private float lockedBrightness = 0.6f;
    
    [Tooltip("미해금 룬의 투명도 (0.0 = 완전히 투명, 1.0 = 불투명)")]
    [SerializeField] [Range(0f, 1f)] private float lockedOpacity = 1f;
    
    [Header("=== 디버그 ===")]
    #endregion
    
    #region 내부 데이터
    
    private RuneInstance runeInstance; // 보유 룬 (null이면 미보유)
    private RuneData runeData; // 룬 데이터
    private bool isOwned; // 보유 여부
    private bool isSelected; // 선택 여부
    
    private RuneInventoryManager inventoryManager;
    private RuneEnhanceManager enhanceManager;
    private RuneManager runeManager;
    private RunePanelUI panelUI;
    
    private Action<RuneListItemUI> onItemClicked; // 클릭 콜백
    
    // Grayscale 원본 상태 저장용
    private System.Collections.Generic.Dictionary<Image, Material> originalImageMaterials = new System.Collections.Generic.Dictionary<Image, Material>();
    private System.Collections.Generic.Dictionary<Image, Color> originalImageColors = new System.Collections.Generic.Dictionary<Image, Color>();
    private System.Collections.Generic.Dictionary<TextMeshProUGUI, Color> originalTextColors = new System.Collections.Generic.Dictionary<TextMeshProUGUI, Color>();
    private bool isGrayscaleApplied = false;
    
    #endregion
    
    #region 초기화
    
    private void Awake()
    {
        inventoryManager = RuneInventoryManager.Instance;
        enhanceManager = RuneEnhanceManager.Instance;
        runeManager = RuneManager.Instance;
        
        // 원본 Material/Color 저장 (최초 1회)
        SaveOriginalColors();
        
        // 아이템 선택 버튼 이벤트
        if (itemSelectButton != null)
        {
            itemSelectButton.onClick.AddListener(OnItemClicked);
        }
        
        // 액션 버튼 이벤트
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnActionButtonClicked);
        }
        
        // 장착 버튼 이벤트
        if (equipButton != null)
        {
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// 보유 룬으로 초기화
    /// </summary>
    public void Setup(RuneInstance rune, Action<RuneListItemUI> onClickCallback, RunePanelUI panel)
    {
        this.runeInstance = rune;
        this.runeData = rune.baseData;
        this.isOwned = true;
        this.onItemClicked = onClickCallback;
        this.panelUI = panel;
        
        gameObject.SetActive(true);
        UpdateUI();
    }
    
    /// <summary>
    /// 미보유 룬으로 초기화 (잠금 상태)
    /// </summary>
    public void SetupAsLocked(RuneData data, Action<RuneListItemUI> onClickCallback, RunePanelUI panel)
    {
        this.runeInstance = null;
        this.runeData = data;
        this.isOwned = false;
        this.onItemClicked = onClickCallback;
        this.panelUI = panel;
        
        gameObject.SetActive(true);
        UpdateUI();
    }
    
    /// <summary>
    /// 선택 상태 설정
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateBackgroundColor();
    }
    
    /// <summary>
    /// UI 갱신
    /// </summary>
    public void RefreshUI()
    {
        UpdateUI();
    }
    
    /// <summary>
    /// 현재 룬 인스턴스 반환
    /// </summary>
    public RuneInstance GetRuneInstance()
    {
        return runeInstance;
    }
    
    /// <summary>
    /// 현재 룬 데이터 반환
    /// </summary>
    public RuneData GetRuneData()
    {
        return runeData;
    }
    
    /// <summary>
    /// 보유 여부 반환
    /// </summary>
    public bool IsOwned()
    {
        return isOwned;
    }
    
    #endregion
    
    #region UI 업데이트
    
    /// <summary>
    /// 전체 UI 업데이트
    /// </summary>
    private void UpdateUI()
    {
        if (runeData == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        // 1. 아이콘 & 이름
        UpdateBasicInfo();
        
        // 2. 레벨 & 한계돌파
        UpdateLevelAndStars();
        
        // 3. 고유 조각 표시 (Phase 6.5)
        UpdateFragmentDisplay();
        
        // 4. 액션 버튼 (해금/강화/한계돌파)
        UpdateActionButton();
        
        // 5. 장착 버튼
        UpdateEquipButton();
        
        // 6. 3단계 상태 구분 및 Grayscale 처리 ⭐ 신규
        DetermineStateAndApplyVisuals();
        
        // 7. 배경 색상 (기존 로직 유지)
        UpdateBackgroundColor();
    }
    
    /// <summary>
    /// 기본 정보 업데이트
    /// </summary>
    private void UpdateBasicInfo()
    {
        // 아이콘
        if (runeIconImage != null && runeData.icon != null)
        {
            runeIconImage.sprite = runeData.icon;
            runeIconImage.color = isOwned ? Color.white : lockedColor;
        }
        
        // 이름
        if (runeNameText != null)
        {
            runeNameText.text = isOwned ? runeData.runeName : $"{runeData.runeName} (미보유)";
            runeNameText.color = isOwned ? Color.white : lockedColor;
        }
        
        // 설명 (ConditionalModifier의 Description)
        if (descriptionText != null)
        {
            string description = GetRuneDescription();
            descriptionText.text = description;
            descriptionText.color = isOwned ? Color.white : lockedColor;
        }
    }
    
    /// <summary>
    /// 룬의 Description 가져오기 (ConditionalModifier.csv의 Description 컬럼)
    /// </summary>
    private string GetRuneDescription()
    {
        if (runeData == null || string.IsNullOrEmpty(runeData.MainStatModifierId))
        {
            return "";
        }
        
        // ConditionalModifierDatabase에서 주옵션 모디파이어 찾기
        var modifier = ConditionalModifierDatabase.GetModifierById(runeData.MainStatModifierId);
        
        if (modifier != null && !string.IsNullOrEmpty(modifier.notes))
        {
            return modifier.notes; // notes 필드가 CSV의 Description 컬럼
        }
        
        return "";
    }
    
    /// <summary>
    /// 레벨 & 한계돌파 업데이트
    /// </summary>
    private void UpdateLevelAndStars()
    {
        if (isOwned && runeInstance != null)
        {
            // 레벨 표시
            if (levelText != null)
            {
                int currentLevel = runeInstance.currentLevel;
                int maxLevel = runeInstance.GetCurrentMaxLevel();
                levelText.text = $"Lv.{currentLevel}/{maxLevel}";
                levelText.gameObject.SetActive(true);
            }
            
            // 한계돌파 별
            UpdateLimitBreakStars(runeInstance.currentLimitBreak);
        }
        else
        {
            // 미보유 시 레벨 숨김
            if (levelText != null)
            {
                levelText.gameObject.SetActive(false);
            }
            
            // 별 숨김
            UpdateLimitBreakStars(0);
        }
    }
    
    /// <summary>
    /// 한계돌파 별 업데이트
    /// </summary>
    private void UpdateLimitBreakStars(int limitBreak)
    {
        if (limitBreakStars == null || limitBreakStars.Length == 0)
        {
            return;
        }
        
        for (int i = 0; i < limitBreakStars.Length; i++)
        {
            if (limitBreakStars[i] == null) continue;
            
            bool isActive = i < limitBreak;
            limitBreakStars[i].gameObject.SetActive(isActive);
            limitBreakStars[i].color = isActive ? activeStarColor : inactiveStarColor;
        }
    }
    
    /// <summary>
    /// [Phase 6.5] 고유 조각 개수 표시
    /// </summary>
    private void UpdateFragmentDisplay()
    {
        if (fragmentText == null || runeData == null)
        {
            return;
        }
        
        string runeId = runeData.runeId;
        int currentFragments = inventoryManager.GetFragmentCount(runeId);
        
        // 필요 조각 계산
        int requiredFragments = GetRequiredFragments();
        
        if (requiredFragments < 0)
        {
            // 최고 레벨 도달 시 "MAX" 표시
            fragmentText.text = "MAX";
            fragmentText.color = Color.yellow;
        }
        else
        {
            // "보유: X / 필요" 형식
            bool isEnough = currentFragments >= requiredFragments;
            fragmentText.text = $"보유: {currentFragments} / {requiredFragments}";
            fragmentText.color = isEnough ? Color.white : Color.red;
        }
    }
    
    /// <summary>
    /// 현재 상태에 필요한 조각 개수 반환
    /// </summary>
    private int GetRequiredFragments()
    {
        if (!isOwned)
        {
            // 미보유 → 해금 비용
            return 100;
        }
        
        if (runeInstance == null)
        {
            return 100;
        }
        
        int currentLevel = runeInstance.currentLevel;
        int maxLevel = runeInstance.GetCurrentMaxLevel();
        int currentLimitBreak = runeInstance.currentLimitBreak;
        int maxLimitBreak = runeInstance.baseData.maxLimitBreak;
        
        // 최종 Lv.15 도달 시
        if (currentLevel >= 15)
        {
            return -1; // MAX 표시
        }
        
        // 만렙 도달 & 한계돌파 가능 → 한계돌파 비용
        if (currentLevel >= maxLevel && currentLimitBreak < maxLimitBreak)
        {
            return 200;
        }
        
        // 만렙 미달 → 레벨업 비용
        if (currentLevel < maxLevel)
        {
            return 10;
        }
        
        // 만렙 + 최대 한돌 (하지만 15레벨 미달)
        return -1; // MAX 표시
    }
    
    /// <summary>
    /// 액션 버튼 업데이트 (해금/강화/한계돌파)
    /// </summary>
    private void UpdateActionButton()
    {
        if (actionButton == null || actionButtonText == null || runeData == null)
        {
            return;
        }
        
        string runeId = runeData.runeId;
        int currentFragments = inventoryManager.GetFragmentCount(runeId);
        
        // 미보유 → 해금
        if (!isOwned)
        {
            int unlockCost = 100;
            bool canUnlock = currentFragments >= unlockCost;
            
            actionButton.interactable = canUnlock;
            actionButtonText.text = canUnlock ? "활성화" : $"활성화 (조각 부족)";
            
            return;
        }
        
        // 보유 룬
        if (runeInstance == null)
        {
            actionButton.interactable = false;
            actionButtonText.text = "오류";
            return;
        }
        
        int currentLevel = runeInstance.currentLevel;
        int maxLevel = runeInstance.GetCurrentMaxLevel();
        int currentLimitBreak = runeInstance.currentLimitBreak;
        int maxLimitBreak = runeInstance.baseData.maxLimitBreak;
        
        // 최종 Lv.15 도달 시
        if (currentLevel >= 15)
        {
            actionButton.interactable = false;
            actionButtonText.text = "최고 레벨";
            return;
        }
        
        // 만렙 도달 & 한계돌파 가능 → 한계돌파
        if (currentLevel >= maxLevel && currentLimitBreak < maxLimitBreak)
        {
            int limitBreakCost = 200;
            bool canLimitBreak = currentFragments >= limitBreakCost;
            
            actionButton.interactable = canLimitBreak;
            actionButtonText.text = canLimitBreak ? "한계돌파" : "한계돌파 (조각 부족)";
            
            return;
        }
        
        // 만렙 미달 → 레벨업
        if (currentLevel < maxLevel)
        {
            int levelUpCost = 10;
            bool canLevelUp = currentFragments >= levelUpCost;
            
            actionButton.interactable = canLevelUp;
            actionButtonText.text = canLevelUp ? "강화" : "강화 (조각 부족)";
            
            return;
        }
        
        // 만렙 + 최대 한돌
        actionButton.interactable = false;
        actionButtonText.text = "최고 레벨";
    }
    
    /// <summary>
    /// 장착 버튼 업데이트
    /// </summary>
    private void UpdateEquipButton()
    {
        if (equipButton == null || equipButtonText == null)
        {
            return;
        }
        
        // 미보유 시 비활성화
        if (!isOwned || runeInstance == null)
        {
            equipButton.interactable = false;
            equipButtonText.text = "미보유";
            return;
        }
        
        // 장착 여부 확인 (Phase 6.5: GetActiveRunes로 확인)
        bool isEquipped = IsRuneEquipped(runeInstance.instanceUID);
        
        equipButton.interactable = true;
        equipButtonText.text = isEquipped ? "해제하기" : "장착하기";
    }
    
    /// <summary>
    /// 룬 장착 여부 확인
    /// </summary>
    private bool IsRuneEquipped(string instanceUID)
    {
        if (runeManager == null || string.IsNullOrEmpty(instanceUID))
        {
            return false;
        }
        
        var activeRunes = runeManager.GetActiveRunes();
        return activeRunes.Any(r => r.instanceUID == instanceUID);
    }
    
    /// <summary>
    /// 배경 색상 업데이트
    /// </summary>
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
    /// 전체 UI에 Grayscale 적용
    /// </summary>
    private void ApplyGrayscaleToAllChildren()
    {
        // 모든 Image에 Grayscale Material 적용
        var images = GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img != null && grayscaleMaterial != null)
            {
                img.material = grayscaleMaterial;
            }
            
            // 밝기와 투명도 조절
            Color adjustedColor = img.color;
            adjustedColor.r *= lockedBrightness;
            adjustedColor.g *= lockedBrightness;
            adjustedColor.b *= lockedBrightness;
            adjustedColor.a *= lockedOpacity;
            img.color = adjustedColor;
        }
        
        // 모든 텍스트에 회색 적용
        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var text in texts)
        {
            if (text != null)
            {
                Color adjustedColor = text.color;
                adjustedColor.r *= lockedBrightness;
                adjustedColor.g *= lockedBrightness;
                adjustedColor.b *= lockedBrightness;
                adjustedColor.a *= lockedOpacity;
                text.color = adjustedColor;
            }
        }
    }
    
    /// <summary>
    /// 원본 색상으로 복구
    /// </summary>
    private void RestoreOriginalColors()
    {
        // 모든 Image 복구
        foreach (var kvp in originalImageMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.material = kvp.Value;
            }
        }
        
        foreach (var kvp in originalImageColors)
        {
            if (kvp.Key != null)
            {
                kvp.Key.color = kvp.Value;
            }
        }
        
        // 모든 텍스트 복구
        foreach (var kvp in originalTextColors)
        {
            if (kvp.Key != null)
            {
                kvp.Key.color = kvp.Value;
            }
        }
    }
    
    /// <summary>
    /// ActionButton만 원본 색상으로 복구 (해금 가능 상태 강조용)
    /// </summary>
    private void RestoreActionButtonColor()
    {
        if (actionButton == null) return;
        
        // 버튼 배경 이미지 복구
        var buttonImage = actionButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            // Material 복구 (Grayscale 제거)
            if (originalImageMaterials.ContainsKey(buttonImage))
            {
                buttonImage.material = originalImageMaterials[buttonImage];
            }
            
            // 색상 복구
            if (originalImageColors.ContainsKey(buttonImage))
            {
                buttonImage.color = originalImageColors[buttonImage];
            }
        }
        
        // 버튼 텍스트 복구
        if (actionButtonText != null && originalTextColors.ContainsKey(actionButtonText))
        {
            actionButtonText.color = originalTextColors[actionButtonText];
        }
    }
    
    /// <summary>
    /// 3단계 상태 구분 및 Grayscale 처리
    /// </summary>
    private void DetermineStateAndApplyVisuals()
    {
        if (runeData == null) return;
        
        string runeId = runeData.runeId;
        int currentFragments = inventoryManager.GetFragmentCount(runeId);
        int unlockCost = 100;
        
        // 1️⃣ 조각 부족 → 전체 Grayscale
        bool hasInsufficientFragments = !isOwned && (currentFragments < unlockCost);
        
        // 2️⃣ 조각 충분, 해금 가능 → 전체 Grayscale + ActionButton만 컬러
        bool isUnlockable = !isOwned && (currentFragments >= unlockCost);
        
        // 3️⃣ 해금 완료 → 전체 정상 색상
        bool isUnlocked = isOwned;
        
        if (hasInsufficientFragments)
        {
            // 전체 Grayscale 적용
            if (applyGrayscaleToAllChildren && !isGrayscaleApplied)
            {
                ApplyGrayscaleToAllChildren();
                isGrayscaleApplied = true;
            }
        }
        else if (isUnlockable)
        {
            // 전체 Grayscale + ActionButton만 컬러 ⭐
            if (applyGrayscaleToAllChildren && !isGrayscaleApplied)
            {
                ApplyGrayscaleToAllChildren();
                isGrayscaleApplied = true;
            }
            
            // ActionButton만 원본 색상으로 복구 (강조)
            RestoreActionButtonColor();
        }
        else if (isUnlocked)
        {
            // 전체 정상 색상
            if (isGrayscaleApplied)
            {
                RestoreOriginalColors();
                isGrayscaleApplied = false;
            }
        }
    }
    
    private void UpdateBackgroundColor()
    {
        if (backgroundImage == null)
        {
            return;
        }
        
        if (isSelected)
        {
            backgroundImage.color = selectedColor;
        }
        else if (isOwned && runeInstance != null && IsRuneEquipped(runeInstance.instanceUID))
        {
            backgroundImage.color = equippedColor;
        }
        else if (isOwned)
        {
            backgroundImage.color = normalColor;
        }
        else
        {
            backgroundImage.color = lockedColor;
        }
    }
    
    #endregion
    
    #region 이벤트 처리
    
    /// <summary>
    /// 아이템 클릭
    /// </summary>
    private void OnItemClicked()
    {
        {
        }
        
        onItemClicked?.Invoke(this);
    }
    
    /// <summary>
    /// 액션 버튼 클릭 (해금/강화/한계돌파)
    /// </summary>
    private void OnActionButtonClicked()
    {
        if (runeData == null)
        {
            return;
        }
        
        bool success = false;
        
        // 미보유 → 해금
        if (!isOwned)
        {
            var result = enhanceManager.TryUnlockRune(runeData.runeId);
            success = (result == RuneEnhanceManager.UnlockResult.Success);
            
            if (success)
            {
                Dbg.Log($"✅ [{runeData.runeName}] 해금 성공!");
                
                // 해금 후 보유 룬으로 전환
                var unlockedRune = inventoryManager.GetRunesByDataId(runeData.runeId);
                if (unlockedRune.Count > 0)
                {
                    Setup(unlockedRune[0], onItemClicked, panelUI);
                }
            }
        }
        // 보유 룬 → 레벨업 or 한계돌파
        else if (runeInstance != null)
        {
            int currentLevel = runeInstance.currentLevel;
            int maxLevel = runeInstance.GetCurrentMaxLevel();
            
            // 만렙 도달 시 → 한계돌파
            if (currentLevel >= maxLevel)
            {
                var result = enhanceManager.TryLimitBreak(runeInstance.instanceUID);
                success = (result == RuneEnhanceManager.LimitBreakResult.Success);
                
                if (success)
                {
                    Dbg.Log($"✅ [{runeData.runeName}] 한계돌파 성공!");
                }
            }
            // 만렙 미달 시 → 레벨업
            else
            {
                var result = enhanceManager.TryLevelUp(runeInstance.instanceUID);
                success = (result == RuneEnhanceManager.LevelUpResult.Success);
                
                if (success)
                {
                    Dbg.Log($"✅ [{runeData.runeName}] 레벨업 성공!");
                }
            }
        }
        
        // 성공 시 UI 갱신
        if (success)
        {
            // 자체 UI 갱신
            RefreshUI();
        }
    }
    
    /// <summary>
    /// 장착 버튼 클릭
    /// </summary>
    private void OnEquipButtonClicked()
    {
        if (!isOwned || runeInstance == null)
        {
            return;
        }
        
        bool isEquipped = IsRuneEquipped(runeInstance.instanceUID);
        
        if (isEquipped)
        {
            // 해제: 장착된 슬롯 찾기
            int slotIndex = FindEquippedSlotIndex(runeInstance.instanceUID);
            if (slotIndex >= 0)
            {
                bool success = runeManager.UnequipRune(slotIndex);
                
                if (success)
                {
                    Dbg.Log($"✅ [{runeData.runeName}] 해제 성공!");
                    RefreshUI();
                }
            }
        }
        else
        {
            // 장착 시도 전 슬롯 체크
            var equippedRunes = runeManager.GetEquippedRunes();
            int emptySlotIndex = -1;
            
            // 빈 슬롯 찾기
            for (int i = 0; i < equippedRunes.Count; i++)
            {
                if (equippedRunes[i] == null)
                {
                    emptySlotIndex = i;
                    break;
                }
            }
            
            // 모든 슬롯이 차있으면 경고 메시지 표시
            if (emptySlotIndex < 0)
            {
                if (panelUI != null)
                {
                    panelUI.ShowWarningMessage("⚠️ 장착 슬롯이 가득 찼습니다!\n먼저 장착된 룬을 해제해주세요.");
                }
                Debug.LogWarning($"[{runeData.runeName}] 장착 실패: 슬롯이 가득 참!");
                return;
            }
            
            // 빈 슬롯에 장착
            bool success = runeManager.EquipRune(runeInstance, emptySlotIndex);
            
            if (success)
            {
                Dbg.Log($"✅ [{runeData.runeName}] 슬롯 {emptySlotIndex}에 장착 성공!");
                RefreshUI();
            }
        }
    }
    
    /// <summary>
    /// 장착된 슬롯 인덱스 찾기
    /// </summary>
    private int FindEquippedSlotIndex(string instanceUID)
    {
        var equippedRunes = runeManager.GetEquippedRunes();
        for (int i = 0; i < equippedRunes.Count; i++)
        {
            if (equippedRunes[i] != null && equippedRunes[i].instanceUID == instanceUID)
            {
                return i;
            }
        }
        return -1;
    }
    
    /// <summary>
    /// RuneType에 따른 슬롯 인덱스 결정
    /// Phase 6.5: 슬롯 0, 1, 2 사용 (3개 슬롯)
    /// </summary>
    private int GetSlotIndexForRuneType(RuneType runeType)
    {
        // 간단히 첫 번째 빈 슬롯 반환
        var equippedRunes = runeManager.GetEquippedRunes();
        for (int i = 0; i < equippedRunes.Count; i++)
        {
            if (equippedRunes[i] == null)
            {
                return i;
            }
        }
        
        // 빈 슬롯 없으면 0번에 덮어쓰기
        return 0;
    }
    
    #endregion
}
