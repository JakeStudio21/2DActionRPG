using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ⚠️⚠️⚠️ [DEPRECATED - Phase 6] ⚠️⚠️⚠️
// 이 파일은 Phase 6 UI 리팩토링으로 인해 더 이상 사용되지 않습니다.
// 대체: RuneDetailUI.cs
// 
// Phase 6에서는 Enhance + Tooltip이 하나의 RuneDetailUI로 통합되었습니다.
// - 기존: 분리된 탭 방식 (레벨업 탭 / 한계돌파 탭)
// - 신규: 컨텍스트 기반 단일 액션 버튼 (상태에 따라 자동 전환)
// 
// 이 파일은 참고용으로만 보관되며, 실제 게임에서는 사용되지 않습니다.
// ⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️

/// <summary>
/// [DEPRECATED] 룬 강화 UI
/// ⚙️ Phase 6: 룬 파편 시스템 (재료 룬 제거)
/// 
/// 역할:
/// - 선택된 룬의 레벨업 및 한계돌파 실행
/// - 강화 전후 스탯 변화 미리보기
/// - 룬 파편 소모 방식
/// 
/// 중요:
/// ⚠️ 실제 강화는 RuneEnhanceManager.TryLevelUp() 호출 (파편 소모)
/// ⚠️ 실제 한계돌파는 RuneEnhanceManager.TryLimitBreak() 호출 (파편 소모)
/// ⚠️ UI는 결과를 표시하는 View 역할만 수행
/// </summary>
[System.Obsolete("Phase 6에서 RuneDetailUI로 대체됨. 이 클래스는 더 이상 사용되지 않습니다.", false)]
public class RuneEnhanceUI : MonoBehaviour
{
    #region UI 컴포넌트
    
    [Header("=== 탭 전환 ===")]
    [SerializeField] private Button levelUpTabButton;
    [SerializeField] private Button limitBreakTabButton;
    [SerializeField] private GameObject levelUpPanel;
    [SerializeField] private GameObject limitBreakPanel;
    
    [Header("=== 공통 정보 ===")]
    [SerializeField] private Image runeIconImage;
    [SerializeField] private TextMeshProUGUI runeNameText;
    [SerializeField] private TextMeshProUGUI currentLevelText;
    
    [Header("=== 레벨업 UI ===")]
    [SerializeField] private TextMeshProUGUI currentStatText;
    [SerializeField] private TextMeshProUGUI nextStatText;
    [SerializeField] private TextMeshProUGUI arrowText;
    [SerializeField] private Button levelUpButton;
    [SerializeField] private TextMeshProUGUI levelUpButtonText;
    [SerializeField] private TextMeshProUGUI levelUpMessageText;
    
    [Header("=== 한계돌파 UI ===")]
    [SerializeField] private TextMeshProUGUI currentMaxLevelText;
    [SerializeField] private TextMeshProUGUI limitBreakCountText;
    [SerializeField] private Transform materialSlotsContainer;
    [SerializeField] private GameObject materialSlotPrefab;
    [SerializeField] private Button limitBreakButton;
    [SerializeField] private TextMeshProUGUI limitBreakButtonText;
    [SerializeField] private TextMeshProUGUI limitBreakMessageText;
    
    [Header("=== 디버그 ===")]
    #endregion
    
    #region 내부 데이터
    
    private RuneInstance selectedRune;
    // [Phase 6] 재료 룬 시스템 제거 - 파편 소모 방식으로 변경
    // private RuneInstance selectedMaterialRune;
    private RuneEnhanceManager enhanceManager;
    private RuneInventoryManager inventoryManager;
    
    // [Phase 6] 재료 슬롯 제거
    // private List<GameObject> materialSlots = new List<GameObject>();
    
    private enum EnhanceTab
    {
        LevelUp,
        LimitBreak
    }
    
    private EnhanceTab currentTab = EnhanceTab.LevelUp;
    
    #endregion
    
    #region 초기화
    
    private void Awake()
    {
        enhanceManager = RuneEnhanceManager.Instance;
        inventoryManager = RuneInventoryManager.Instance;
        
        if (enhanceManager == null)
        {
            Debug.LogError("[RuneEnhanceUI] RuneEnhanceManager를 찾을 수 없습니다!");
        }
        
        if (inventoryManager == null)
        {
            Debug.LogError("[RuneEnhanceUI] RuneInventoryManager를 찾을 수 없습니다!");
        }
        
        // 버튼 이벤트 등록
        if (levelUpTabButton != null)
        {
            levelUpTabButton.onClick.AddListener(() => SwitchTab(EnhanceTab.LevelUp));
        }
        
        if (limitBreakTabButton != null)
        {
            limitBreakTabButton.onClick.AddListener(() => SwitchTab(EnhanceTab.LimitBreak));
        }
        
        if (levelUpButton != null)
        {
            levelUpButton.onClick.AddListener(OnLevelUpButtonClick);
        }
        
        if (limitBreakButton != null)
        {
            limitBreakButton.onClick.AddListener(OnLimitBreakButtonClick);
        }
    }
    
    private void Start()
    {
        // 초기 탭 표시
        SwitchTab(EnhanceTab.LevelUp);
        
        // 초기 상태 (룬 선택 없음)
        ClearSelection();
    }
    
    #endregion
    
    #region 탭 전환
    
    /// <summary>
    /// 탭 전환
    /// </summary>
    private void SwitchTab(EnhanceTab tab)
    {
        currentTab = tab;
        
        // 패널 전환
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(tab == EnhanceTab.LevelUp);
        }
        
        if (limitBreakPanel != null)
        {
            limitBreakPanel.SetActive(tab == EnhanceTab.LimitBreak);
        }
        
        // UI 갱신
        if (selectedRune != null)
        {
            if (tab == EnhanceTab.LevelUp)
            {
                RefreshLevelUpUI();
            }
            else
            {
                RefreshLimitBreakUI();
            }
        }
    }
    
    #endregion
    
    #region 공개 API
    
    /// <summary>
    /// 룬 선택
    /// </summary>
    public void SelectRune(RuneInstance rune)
    {
        selectedRune = rune;
        
        {
        }
        
        // 공통 정보 갱신
        RefreshCommonInfo();
        
        // 현재 탭 갱신
        if (currentTab == EnhanceTab.LevelUp)
        {
            RefreshLevelUpUI();
        }
        else
        {
            RefreshLimitBreakUI();
        }
    }
    
    /// <summary>
    /// 선택 해제
    /// </summary>
    public void ClearSelection()
    {
        selectedRune = null;
        
        // 공통 정보 숨김
        if (runeIconImage != null) runeIconImage.gameObject.SetActive(false);
        if (runeNameText != null) runeNameText.text = "룬을 선택하세요";
        if (currentLevelText != null) currentLevelText.text = "";
        
        // 레벨업 UI 숨김
        if (currentStatText != null) currentStatText.text = "";
        if (nextStatText != null) nextStatText.text = "";
        if (arrowText != null) arrowText.gameObject.SetActive(false);
        if (levelUpButton != null) levelUpButton.interactable = false;
        if (levelUpMessageText != null) levelUpMessageText.gameObject.SetActive(false);
        
        // 한계돌파 UI 숨김
        if (currentMaxLevelText != null) currentMaxLevelText.text = "";
        if (limitBreakCountText != null) limitBreakCountText.text = "";
        if (limitBreakButton != null) limitBreakButton.interactable = false;
        if (limitBreakMessageText != null) limitBreakMessageText.gameObject.SetActive(false);
    }
    
    #endregion
    
    #region 공통 정보 갱신
    
    /// <summary>
    /// 공통 정보 갱신
    /// </summary>
    private void RefreshCommonInfo()
    {
        if (selectedRune == null) return;
        
        // 아이콘
        if (runeIconImage != null && selectedRune.baseData.icon != null)
        {
            runeIconImage.sprite = selectedRune.baseData.icon;
            runeIconImage.gameObject.SetActive(true);
        }
        
        // 이름
        if (runeNameText != null)
        {
            runeNameText.text = selectedRune.baseData.runeName;
        }
        
        // 현재 레벨
        if (currentLevelText != null)
        {
            currentLevelText.text = $"Lv.{selectedRune.currentLevel} (한계돌파 +{selectedRune.currentLimitBreak})";
        }
    }
    
    #endregion
    
    #region 레벨업 UI
    
    /// <summary>
    /// 레벨업 UI 갱신
    /// </summary>
    private void RefreshLevelUpUI()
    {
        if (selectedRune == null) return;
        
        int currentLevel = selectedRune.currentLevel;
        int maxLevel = selectedRune.GetCurrentMaxLevel();
        
        // 최대 레벨 체크
        if (currentLevel >= maxLevel)
        {
            ShowLevelUpMaxMessage();
            return;
        }
        
        // 현재/다음 레벨 스탯 계산
        var mainModifier = GetMainModifier();
        if (mainModifier == null)
        {
            Debug.LogWarning("[RuneEnhanceUI] 주옵션 모디파이어를 찾을 수 없습니다.");
            return;
        }
        
        float currentValue = mainModifier.value * selectedRune.GetMainStatMultiplier();
        float nextValue = mainModifier.value * selectedRune.baseData.GetMainStatMultiplier(currentLevel + 1);
        
        // UI 표시
        if (currentStatText != null)
        {
            currentStatText.text = FormatStatValue(mainModifier, currentValue);
        }
        
        if (nextStatText != null)
        {
            nextStatText.text = FormatStatValue(mainModifier, nextValue);
        }
        
        if (arrowText != null)
        {
            arrowText.gameObject.SetActive(true);
            arrowText.text = "→";
        }
        
        // 버튼 활성화
        if (levelUpButton != null)
        {
            levelUpButton.interactable = true;
        }
        
        if (levelUpButtonText != null)
        {
            levelUpButtonText.text = "강화";
        }
        
        // 메시지 숨김
        if (levelUpMessageText != null)
        {
            levelUpMessageText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 최대 레벨 도달 메시지 표시
    /// </summary>
    private void ShowLevelUpMaxMessage()
    {
        // 현재 스탯만 표시
        var mainModifier = GetMainModifier();
        if (mainModifier != null && currentStatText != null)
        {
            float currentValue = mainModifier.value * selectedRune.GetMainStatMultiplier();
            currentStatText.text = FormatStatValue(mainModifier, currentValue);
        }
        
        // 다음 스탯 숨김
        if (nextStatText != null) nextStatText.text = "";
        if (arrowText != null) arrowText.gameObject.SetActive(false);
        
        // 버튼 비활성화
        if (levelUpButton != null)
        {
            levelUpButton.interactable = false;
        }
        
        // 메시지 표시
        if (levelUpMessageText != null)
        {
            levelUpMessageText.text = "최대 레벨 도달 (한계돌파 필요)";
            levelUpMessageText.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// 레벨업 버튼 클릭
    /// </summary>
    private void OnLevelUpButtonClick()
    {
        if (selectedRune == null || enhanceManager == null)
        {
            return;
        }
        
        {
        }
        
        // 레벨업 실행
        var result = enhanceManager.TryLevelUp(selectedRune.instanceUID);
        
        if (result == RuneEnhanceManager.LevelUpResult.Success)
        {
            {
            }
            
            // UI 갱신
            RefreshCommonInfo();
            RefreshLevelUpUI();
            RefreshAllUI();
        }
        else
        {
            {
                Debug.LogWarning($"[RuneEnhanceUI] ❌ 레벨업 실패: {result}");
            }
        }
    }
    
    #endregion
    
    #region 한계돌파 UI
    
    /// <summary>
    /// 한계돌파 UI 갱신
    /// [Phase 6] 파편 소모 방식으로 변경
    /// </summary>
    private void RefreshLimitBreakUI()
    {
        if (selectedRune == null) return;
        
        int currentLimitBreak = selectedRune.currentLimitBreak;
        int currentLevel = selectedRune.currentLevel;
        int maxLevel = selectedRune.GetCurrentMaxLevel();
        
        // 현재 최대 레벨 표시
        if (currentMaxLevelText != null)
        {
            currentMaxLevelText.text = $"현재 최대 레벨: {maxLevel}";
        }
        
        // 한계돌파 횟수 표시
        if (limitBreakCountText != null)
        {
            limitBreakCountText.text = $"한계돌파: {currentLimitBreak} / 5";
        }
        
        // 최종 한계돌파 체크
        if (currentLimitBreak >= 5)
        {
            ShowLimitBreakMaxMessage();
            return;
        }
        
        // 레벨 체크 (최대 레벨에 도달해야 한계돌파 가능)
        if (currentLevel < maxLevel)
        {
            ShowLimitBreakLevelRequiredMessage();
            return;
        }
        
        // Phase 6.5: 고유 조각 체크
        if (selectedRune != null && inventoryManager != null)
        {
            string runeId = selectedRune.baseData.runeId;
            int currentFragments = inventoryManager.GetFragmentCount(runeId);
            if (currentFragments < 200)
            {
                ShowLimitBreakInsufficientFragmentsMessage();
                return;
            }
        }
        
        // 버튼 활성화
        if (limitBreakButton != null)
        {
            limitBreakButton.interactable = true;
        }
        
        if (limitBreakButtonText != null)
        {
            limitBreakButtonText.text = "한계돌파 (파편 200개)";
        }
        
        // 메시지 숨김
        if (limitBreakMessageText != null)
        {
            limitBreakMessageText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 최종 한계돌파 완료 메시지 표시
    /// </summary>
    private void ShowLimitBreakMaxMessage()
    {
        // 버튼 비활성화
        if (limitBreakButton != null)
        {
            limitBreakButton.interactable = false;
        }
        
        // 메시지 표시
        if (limitBreakMessageText != null)
        {
            limitBreakMessageText.text = "✨ 최종 한계돌파 완료 ✨";
            limitBreakMessageText.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// 레벨 부족 메시지 표시
    /// </summary>
    private void ShowLimitBreakLevelRequiredMessage()
    {
        // 버튼 비활성화
        if (limitBreakButton != null)
        {
            limitBreakButton.interactable = false;
        }
        
        // 메시지 표시
        if (limitBreakMessageText != null)
        {
            limitBreakMessageText.text = $"최대 레벨 {selectedRune.GetCurrentMaxLevel()} 도달 시 한계돌파 가능";
            limitBreakMessageText.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// [Phase 6] 파편 부족 메시지 표시
    /// </summary>
    private void ShowLimitBreakInsufficientFragmentsMessage()
    {
        // 버튼 비활성화
        if (limitBreakButton != null)
        {
            limitBreakButton.interactable = false;
        }
        
        // 메시지 표시
        if (limitBreakMessageText != null && selectedRune != null && inventoryManager != null)
        {
            string runeId = selectedRune.baseData.runeId;
            int currentFragments = inventoryManager.GetFragmentCount(runeId);
            limitBreakMessageText.text = $"{selectedRune.baseData.runeName} 조각 부족 (보유: {currentFragments}개 / 필요: 200개)";
            limitBreakMessageText.gameObject.SetActive(true);
        }
    }
    
    // [Phase 6] 재료 슬롯 시스템 제거 - 파편 소모 방식으로 변경
    // 아래 메서드들은 더 이상 사용되지 않으며, UI 컴포넌트 참조 제거 후 삭제 가능
    
    /* [Deprecated - Phase 6]
    private void RefreshMaterialSlots() { ... }
    private void CreateMaterialSlot(RuneInstance materialRune) { ... }
    private void ClearMaterialSlots() { ... }
    private void UpdateMaterialSlotSelection() { ... }
    */
    
    /// <summary>
    /// 한계돌파 버튼 클릭
    /// [Phase 6] 파편 소모 방식으로 변경
    /// </summary>
    private void OnLimitBreakButtonClick()
    {
        if (selectedRune == null || enhanceManager == null)
        {
            return;
        }
        
        {
        }
        
        // 한계돌파 실행 (파편 200개 소모)
        var result = enhanceManager.TryLimitBreak(selectedRune.instanceUID);
        
        if (result == RuneEnhanceManager.LimitBreakResult.Success)
        {
            {
            }
            
            // UI 갱신
            RefreshCommonInfo();
            RefreshLimitBreakUI();
            RefreshAllUI();
        }
        else
        {
            {
                Debug.LogWarning($"[RuneEnhanceUI] ❌ 한계돌파 실패: {result}");
            }
            
            if (limitBreakMessageText != null)
            {
                limitBreakMessageText.text = $"한계돌파 실패: {GetLimitBreakResultMessage(result)}";
                limitBreakMessageText.gameObject.SetActive(true);
            }
        }
    }
    
    #endregion
    
    #region 스탯 포맷팅
    
    /// <summary>
    /// 주옵션 모디파이어 가져오기
    /// </summary>
    private ConditionalModifier GetMainModifier()
    {
        if (selectedRune == null)
        {
            return null;
        }
        
        return ConditionalModifierDatabase.GetModifierById(selectedRune.baseData.MainStatModifierId);
    }
    
    /// <summary>
    /// 스탯 값 포맷팅
    /// </summary>
    private string FormatStatValue(ConditionalModifier modifier, float value)
    {
        string formattedValue = "";
        
        switch (modifier.unit)
        {
            case StatUnit.Flat:
                formattedValue = $"+{value:F1}";
                break;
            
            case StatUnit.Percent:
                formattedValue = $"+{value:F1}%";
                break;
            
            case StatUnit.Bool:
                formattedValue = value > 0 ? "활성화" : "비활성화";
                break;
            
            default:
                formattedValue = value.ToString("F1");
                break;
        }
        
        return $"{modifier.displayName} {formattedValue}";
    }
    
    #endregion
    
    #region 유틸리티
    
    /// <summary>
    /// 한계돌파 결과 메시지 변환
    /// [Phase 6] 파편 시스템 메시지로 업데이트
    /// </summary>
    private string GetLimitBreakResultMessage(RuneEnhanceManager.LimitBreakResult result)
    {
        switch (result)
        {
            case RuneEnhanceManager.LimitBreakResult.BaseRuneNotFound:
                return "베이스 룬을 찾을 수 없습니다";
            case RuneEnhanceManager.LimitBreakResult.AlreadyMaxLimitBreak:
                return "이미 최대 한계돌파입니다";
            case RuneEnhanceManager.LimitBreakResult.BaseRuneNotMaxLevel:
                return "베이스 룬이 최대 레벨에 도달하지 않았습니다";
            case RuneEnhanceManager.LimitBreakResult.InsufficientGold:
                return "룬 파편이 부족합니다 (필요: 200개)";
            
            // [Deprecated - Phase 6] 재료 룬 관련 에러 (더 이상 발생하지 않음)
            case RuneEnhanceManager.LimitBreakResult.MaterialRuneNotFound:
            case RuneEnhanceManager.LimitBreakResult.SameRune:
            case RuneEnhanceManager.LimitBreakResult.DifferentRuneType:
            case RuneEnhanceManager.LimitBreakResult.MaterialRuneLocked:
                return "시스템 오류 (재료 룬 시스템 제거됨)";
            
            default:
                return "알 수 없는 오류";
        }
    }
    
    #endregion
    
    #region 전체 UI 갱신
    
    /// <summary>
    /// 모든 관련 UI 갱신
    /// </summary>
    private void RefreshAllUI()
    {
        // 인벤토리 UI 갱신
        var inventoryUI = FindObjectOfType<RuneInventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.RefreshInventory();
        }
        
        // 툴팁 UI 갱신 (현재 선택된 룬 유지)
        var tooltipUI = FindObjectOfType<RuneTooltipUI>();
        if (tooltipUI != null && selectedRune != null)
        {
            tooltipUI.ShowTooltip(selectedRune);
        }
        
        // 장착 슬롯 UI 갱신
        var equipSlots = FindObjectsOfType<RuneEquipSlotUI>();
        foreach (var slot in equipSlots)
        {
            slot.RefreshSlot();
        }
    }
    
    #endregion
}
