using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 룬 상세 정보 및 액션 패널
/// ⚙️ Phase 6: Tooltip + Enhance 통합
/// 
/// 역할:
/// - 선택한 룬의 상세 정보 표시 (스탯, 부옵션, 조건 등)
/// - 컨텍스트 기반 단일 액션 버튼 제공
///   * 미보유 → "해금 (파편 100개)"
///   * 보유 & 만렙 미달 → "강화 (파편 10개)"
///   * 보유 & 만렙 도달 → "한계돌파 (파편 200개)"
///   * 최종 Lv.15 → "최고 레벨 도달" (비활성화)
/// 
/// 중요:
/// ⚠️ 액션 실행 후 반드시 RunePanelUI.RefreshUI() 호출
/// </summary>
public class RuneDetailUI : MonoBehaviour
{
    #region UI 컴포넌트
    
    [Header("=== 기본 정보 ===")]
    [SerializeField] private Image runeIconImage;
    [SerializeField] private TextMeshProUGUI runeNameText;
    [SerializeField] private TextMeshProUGUI runeLevelText;
    [SerializeField] private TextMeshProUGUI runeTypeText;
    
    [Header("=== 주 효과 ===")]
    [SerializeField] private TextMeshProUGUI mainStatText;
    [SerializeField] private TextMeshProUGUI mainStatScalingText; // "Lv.15 시 170%"
    
    [Header("=== 부옵션 ===")]
    [SerializeField] private Transform subStatContainer;
    [SerializeField] private GameObject subStatRowPrefab;
    [SerializeField] private TextMeshProUGUI subStatMilestoneText; // "Lv.3, 6, 9 개방"
    
    [Header("=== 조건 ===")]
    [SerializeField] private TextMeshProUGUI conditionText;
    
    [Header("=== 한계돌파 정보 ===")]
    [SerializeField] private TextMeshProUGUI limitBreakText; // "한계돌파: 3 / 5"
    [SerializeField] private TextMeshProUGUI maxLevelText; // "최대 레벨: 13"
    
    [Header("=== 컨텍스트 액션 버튼 ===")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;
    [SerializeField] private TextMeshProUGUI actionMessageText; // 결과 메시지
    
    [Header("=== 디버그 ===")]
    #endregion
    
    #region 내부 데이터
    
    private RuneInstance selectedRune; // 보유 룬
    private RuneData selectedRuneData; // 미보유 룬
    private bool isOwnedRune; // 보유 여부
    
    private RuneEnhanceManager enhanceManager;
    private RuneInventoryManager inventoryManager;
    private RunePanelUI panelUI; // 전체 패널 참조 (RefreshUI 호출용)
    
    private List<GameObject> subStatRows = new List<GameObject>();
    
    private enum ActionType
    {
        None,           // 액션 없음
        Unlock,         // 해금
        LevelUp,        // 레벨업
        LimitBreak,     // 한계돌파
        MaxLevel        // 최고 레벨 도달 (비활성화)
    }
    
    #endregion
    
    #region 초기화
    
    private void Awake()
    {
        enhanceManager = RuneEnhanceManager.Instance;
        inventoryManager = RuneInventoryManager.Instance;
        
        if (enhanceManager == null)
        {
            Debug.LogError("[RuneDetailUI] RuneEnhanceManager를 찾을 수 없습니다!");
        }
        
        if (inventoryManager == null)
        {
            Debug.LogError("[RuneDetailUI] RuneInventoryManager를 찾을 수 없습니다!");
        }
        
        // 버튼 이벤트 연결
        if (actionButton != null)
        {
            actionButton.onClick.AddListener(OnActionButtonClicked);
        }
    }
    
    /// <summary>
    /// RunePanelUI 참조 설정
    /// </summary>
    public void SetPanelReference(RunePanelUI panel)
    {
        panelUI = panel;
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// 보유 룬 선택 (RuneInstance)
    /// </summary>
    public void ShowDetail(RuneInstance rune)
    {
        selectedRune = rune;
        selectedRuneData = null;
        isOwnedRune = true;
        
        if (rune == null || rune.baseData == null)
        {
            Hide();
            return;
        }
        
        gameObject.SetActive(true);
        RefreshUI();
        
        {
        }
    }
    
    /// <summary>
    /// 미보유 룬 선택 (RuneData)
    /// </summary>
    public void ShowDetail(RuneData runeData)
    {
        selectedRune = null;
        selectedRuneData = runeData;
        isOwnedRune = false;
        
        if (runeData == null)
        {
            Hide();
            return;
        }
        
        gameObject.SetActive(true);
        RefreshUI();
        
        {
        }
    }
    
    /// <summary>
    /// 패널 숨김
    /// </summary>
    public void Hide()
    {
        selectedRune = null;
        selectedRuneData = null;
        gameObject.SetActive(false);
    }
    
    #endregion
    
    #region UI 갱신
    
    /// <summary>
    /// 전체 UI 갱신
    /// </summary>
    private void RefreshUI()
    {
        if (isOwnedRune && selectedRune != null)
        {
            RefreshOwnedRuneUI();
        }
        else if (!isOwnedRune && selectedRuneData != null)
        {
            RefreshLockedRuneUI();
        }
    }
    
    /// <summary>
    /// 보유 룬 UI 갱신
    /// </summary>
    private void RefreshOwnedRuneUI()
    {
        var rune = selectedRune;
        var data = rune.baseData;
        
        // 1. 기본 정보
        if (runeIconImage != null && data.icon != null)
        {
            runeIconImage.sprite = data.icon;
            runeIconImage.color = Color.white; // 컬러
        }
        
        if (runeNameText != null)
        {
            runeNameText.text = data.runeName;
        }
        
        if (runeLevelText != null)
        {
            runeLevelText.text = $"Lv.{rune.currentLevel} (한계돌파 +{rune.currentLimitBreak})";
        }
        
        if (runeTypeText != null)
        {
            runeTypeText.text = GetRuneTypeText(data.runeType);
        }
        
        // 2. 주 효과
        UpdateMainStat(rune);
        
        // 3. 부옵션
        UpdateSubStats(rune);
        
        // 4. 조건
        UpdateCondition(data);
        
        // 5. 한계돌파 정보
        if (limitBreakText != null)
        {
            limitBreakText.text = $"한계돌파: {rune.currentLimitBreak} / {data.maxLimitBreak}";
        }
        
        if (maxLevelText != null)
        {
            maxLevelText.text = $"최대 레벨: {rune.GetCurrentMaxLevel()}";
        }
        
        // 6. 액션 버튼
        UpdateActionButton();
    }
    
    /// <summary>
    /// 미보유 룬 UI 갱신
    /// </summary>
    private void RefreshLockedRuneUI()
    {
        var data = selectedRuneData;
        
        // 1. 기본 정보
        if (runeIconImage != null && data.icon != null)
        {
            runeIconImage.sprite = data.icon;
            runeIconImage.color = new Color(0.5f, 0.5f, 0.5f, 1f); // 흑백
        }
        
        if (runeNameText != null)
        {
            runeNameText.text = $"{data.runeName} (미보유)";
        }
        
        if (runeLevelText != null)
        {
            runeLevelText.text = "해금 가능";
        }
        
        if (runeTypeText != null)
        {
            runeTypeText.text = GetRuneTypeText(data.runeType);
        }
        
        // 2. 주 효과 (Lv.1 기준)
        if (mainStatText != null)
        {
            var mainModifier = ConditionalModifierDatabase.GetModifierById(data.MainStatModifierId);
            if (mainModifier != null)
            {
                float baseValue = mainModifier.value * data.GetMainStatMultiplier(1);
                mainStatText.text = $"{mainModifier.displayName}: {FormatStatValue(mainModifier, baseValue)}";
            }
        }
        
        if (mainStatScalingText != null)
        {
            mainStatScalingText.text = "Lv.15 시 170%";
        }
        
        // 3. 부옵션 (개방 안내)
        ClearSubStatRows();
        if (subStatMilestoneText != null)
        {
            subStatMilestoneText.text = "Lv.3, 6, 9 개방";
        }
        
        // 4. 조건
        UpdateCondition(data);
        
        // 5. 한계돌파 정보
        if (limitBreakText != null)
        {
            limitBreakText.text = "한계돌파: 0 / 5";
        }
        
        if (maxLevelText != null)
        {
            maxLevelText.text = "최대 레벨: 10 (기본)";
        }
        
        // 6. 해금 버튼
        UpdateActionButton();
    }
    
    /// <summary>
    /// 주 효과 업데이트
    /// </summary>
    private void UpdateMainStat(RuneInstance rune)
    {
        if (mainStatText == null) return;
        
        var mainModifier = ConditionalModifierDatabase.GetModifierById(rune.baseData.MainStatModifierId);
        if (mainModifier == null)
        {
            mainStatText.text = "주 효과: 정보 없음";
            return;
        }
        
        float currentValue = mainModifier.value * rune.GetMainStatMultiplier();
        mainStatText.text = $"{mainModifier.displayName}: {FormatStatValue(mainModifier, currentValue)}";
        
        // 스케일링 정보
        if (mainStatScalingText != null)
        {
            mainStatScalingText.text = "Lv.15 시 170%";
        }
    }
    
    /// <summary>
    /// 부옵션 업데이트
    /// </summary>
    private void UpdateSubStats(RuneInstance rune)
    {
        // 기존 부옵션 행 제거
        ClearSubStatRows();
        
        if (subStatContainer == null || subStatRowPrefab == null)
        {
            return;
        }
        
        // 부옵션 개방 안내
        if (subStatMilestoneText != null)
        {
            int currentSubStats = rune.allocatedSubStatModifierIds.Count;
            int nextMilestone = GetNextSubStatMilestone(rune.currentLevel);
            
            if (nextMilestone > 0)
            {
                int levelsRemaining = nextMilestone - rune.currentLevel;
                subStatMilestoneText.text = $"다음 부옵션: Lv.{nextMilestone} ({levelsRemaining}레벨 후)";
            }
            else if (currentSubStats < 3)
            {
                subStatMilestoneText.text = "Lv.3, 6, 9 개방";
            }
            else
            {
                subStatMilestoneText.text = "부옵션 최대 획득";
            }
        }
        
        // 현재 보유한 부옵션 표시
        foreach (var modifierId in rune.allocatedSubStatModifierIds)
        {
            CreateSubStatRow(modifierId, rune.currentLevel);
        }
    }
    
    /// <summary>
    /// 부옵션 행 생성
    /// </summary>
    private void CreateSubStatRow(string modifierId, int runeLevel)
    {
        if (subStatRowPrefab == null || subStatContainer == null)
        {
            return;
        }
        
        GameObject row = Instantiate(subStatRowPrefab, subStatContainer);
        subStatRows.Add(row);
        
        var modifier = ConditionalModifierDatabase.GetModifierById(modifierId);
        if (modifier == null)
        {
            Debug.LogWarning($"[RuneDetailUI] 모디파이어를 찾을 수 없습니다: {modifierId}");
            return;
        }
        
        // 텍스트 설정
        var textComponent = row.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            float value = modifier.value; // 부옵션은 레벨 스케일링 없음
            textComponent.text = $"• {modifier.displayName}: {FormatStatValue(modifier, value)}";
        }
    }
    
    /// <summary>
    /// 부옵션 행 전체 제거
    /// </summary>
    private void ClearSubStatRows()
    {
        foreach (var row in subStatRows)
        {
            if (row != null)
            {
                Destroy(row);
            }
        }
        
        subStatRows.Clear();
    }
    
    /// <summary>
    /// 조건 업데이트
    /// </summary>
    private void UpdateCondition(RuneData data)
    {
        if (conditionText == null) return;
        
        var mainModifier = ConditionalModifierDatabase.GetModifierById(data.MainStatModifierId);
        if (mainModifier == null || mainModifier.conditionType == EConditionType.None)
        {
            conditionText.text = "조건: 없음 (항상 적용)";
            return;
        }
        
        // 조건 타입과 파라미터로 설명 생성
        string conditionDesc = GetConditionDescription(mainModifier.conditionType, mainModifier.conditionParam);
        conditionText.text = $"조건: {conditionDesc}";
    }
    
    /// <summary>
    /// 조건 설명 생성
    /// </summary>
    private string GetConditionDescription(EConditionType conditionType, string conditionParam)
    {
        switch (conditionType)
        {
            case EConditionType.None:
                return "없음 (항상 적용)";
            
            case EConditionType.TargetIsBoss:
                return "대상이 보스일 때";
            
            case EConditionType.SelfHpAbove:
                if (float.TryParse(conditionParam, out float above))
                    return $"자신 HP {above * 100:F0}% 이상";
                return "자신 HP 높을 때";
            
            case EConditionType.SelfHpBelow:
                if (float.TryParse(conditionParam, out float below))
                    return $"자신 HP {below * 100:F0}% 이하";
                return "자신 HP 낮을 때";
            
            case EConditionType.TargetHpAbove:
                if (float.TryParse(conditionParam, out float targetAbove))
                    return $"대상 HP {targetAbove * 100:F0}% 이상";
                return "대상 HP 높을 때";
            
            case EConditionType.TargetHpBelow:
                if (float.TryParse(conditionParam, out float targetBelow))
                    return $"대상 HP {targetBelow * 100:F0}% 이하";
                return "대상 HP 낮을 때";
            
            default:
                return conditionType.ToString();
        }
    }
    
    #endregion
    
    #region 액션 버튼
    
    /// <summary>
    /// 컨텍스트 기반 액션 버튼 업데이트 (Phase 6.5: 고유 조각)
    /// </summary>
    private void UpdateActionButton()
    {
        if (actionButton == null || actionButtonText == null)
        {
            return;
        }
        
        ActionType actionType = DetermineActionType();
        
        // Phase 6.5: 해당 룬의 고유 조각 확인
        string runeId = isOwnedRune ? selectedRune.baseData.runeId : selectedRuneData.runeId;
        int currentFragments = inventoryManager.GetFragmentCount(runeId);
        
        switch (actionType)
        {
            case ActionType.Unlock:
                actionButton.interactable = currentFragments >= 100;
                actionButtonText.text = actionButton.interactable 
                    ? "해금 (조각 100개)" 
                    : $"조각 부족 ({currentFragments}/100)";
                break;
                
            case ActionType.LevelUp:
                actionButton.interactable = currentFragments >= 10;
                actionButtonText.text = actionButton.interactable 
                    ? "강화 (조각 10개)" 
                    : $"조각 부족 ({currentFragments}/10)";
                break;
                
            case ActionType.LimitBreak:
                actionButton.interactable = currentFragments >= 200;
                actionButtonText.text = actionButton.interactable 
                    ? "한계돌파 (조각 200개)" 
                    : $"조각 부족 ({currentFragments}/200)";
                break;
                
            case ActionType.MaxLevel:
                actionButton.interactable = false;
                actionButtonText.text = "최고 레벨 도달";
                break;
                
            default:
                actionButton.interactable = false;
                actionButtonText.text = "액션 없음";
                break;
        }
        
        // 메시지 초기화
        if (actionMessageText != null)
        {
            actionMessageText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 현재 룬 상태에 따른 액션 타입 결정
    /// </summary>
    private ActionType DetermineActionType()
    {
        // 1. 미보유 룬 → 해금
        if (!isOwnedRune)
        {
            return ActionType.Unlock;
        }
        
        // 2. 보유 룬
        if (selectedRune == null)
        {
            return ActionType.None;
        }
        
        int currentLevel = selectedRune.currentLevel;
        int maxLevel = selectedRune.GetCurrentMaxLevel();
        int currentLimitBreak = selectedRune.currentLimitBreak;
        int maxLimitBreak = selectedRune.baseData.maxLimitBreak;
        
        // 3. 최종 15레벨 도달 → 완료
        if (currentLevel >= 15)
        {
            return ActionType.MaxLevel;
        }
        
        // 4. 만렙 도달 & 한계돌파 가능 → 한계돌파
        if (currentLevel >= maxLevel && currentLimitBreak < maxLimitBreak)
        {
            return ActionType.LimitBreak;
        }
        
        // 5. 만렙 미달 → 레벨업
        if (currentLevel < maxLevel)
        {
            return ActionType.LevelUp;
        }
        
        // 6. 만렙 + 최대 한돌 도달 (하지만 15레벨 미달) → 완료
        return ActionType.MaxLevel;
    }
    
    /// <summary>
    /// 액션 버튼 클릭
    /// </summary>
    private void OnActionButtonClicked()
    {
        ActionType actionType = DetermineActionType();
        
        bool success = false;
        string resultMessage = "";
        
        switch (actionType)
        {
            case ActionType.Unlock:
                success = ExecuteUnlock(out resultMessage);
                break;
                
            case ActionType.LevelUp:
                success = ExecuteLevelUp(out resultMessage);
                break;
                
            case ActionType.LimitBreak:
                success = ExecuteLimitBreak(out resultMessage);
                break;
                
            default:
                return;
        }
        
        // 결과 메시지 표시
        ShowActionMessage(resultMessage, success);
        
        // 성공 시 전체 UI 갱신
        if (success)
        {
            RefreshUI();
            
            if (panelUI != null)
            {
                panelUI.RefreshUI();
            }
        }
    }
    
    #endregion
    
    #region 액션 실행
    
    /// <summary>
    /// 룬 해금 실행
    /// </summary>
    private bool ExecuteUnlock(out string message)
    {
        if (selectedRuneData == null)
        {
            message = "룬 데이터가 없습니다";
            return false;
        }
        
        var result = enhanceManager.TryUnlockRune(selectedRuneData.runeId);
        
        switch (result)
        {
            case RuneEnhanceManager.UnlockResult.Success:
                message = $"✅ {selectedRuneData.runeName} 해금 성공!";
                
                // 해금 후 보유 룬으로 전환
                var unlockedRune = inventoryManager.GetRunesByDataId(selectedRuneData.runeId)[0];
                ShowDetail(unlockedRune);
                
                return true;
                
            case RuneEnhanceManager.UnlockResult.AlreadyUnlocked:
                message = "이미 보유 중입니다";
                return false;
                
            case RuneEnhanceManager.UnlockResult.InsufficientFragments:
                message = $"파편 부족 (필요: 100개)";
                return false;
                
            default:
                message = "해금 실패";
                return false;
        }
    }
    
    /// <summary>
    /// 레벨업 실행
    /// </summary>
    private bool ExecuteLevelUp(out string message)
    {
        if (selectedRune == null)
        {
            message = "룬 데이터가 없습니다";
            return false;
        }
        
        int oldLevel = selectedRune.currentLevel;
        var result = enhanceManager.TryLevelUp(selectedRune.instanceUID);
        
        switch (result)
        {
            case RuneEnhanceManager.LevelUpResult.Success:
                message = $"✅ Lv.{oldLevel} → Lv.{selectedRune.currentLevel} 강화 성공!";
                return true;
                
            case RuneEnhanceManager.LevelUpResult.AlreadyMaxLevel:
                message = "이미 최대 레벨입니다";
                return false;
                
            case RuneEnhanceManager.LevelUpResult.InsufficientGold:
                message = $"파편 부족 (필요: 10개)";
                return false;
                
            default:
                message = "레벨업 실패";
                return false;
        }
    }
    
    /// <summary>
    /// 한계돌파 실행
    /// </summary>
    private bool ExecuteLimitBreak(out string message)
    {
        if (selectedRune == null)
        {
            message = "룬 데이터가 없습니다";
            return false;
        }
        
        int oldLimitBreak = selectedRune.currentLimitBreak;
        var result = enhanceManager.TryLimitBreak(selectedRune.instanceUID);
        
        switch (result)
        {
            case RuneEnhanceManager.LimitBreakResult.Success:
                message = $"✅ 한계돌파 성공! +{selectedRune.currentLimitBreak} (최대 Lv.{selectedRune.GetCurrentMaxLevel()})";
                return true;
                
            case RuneEnhanceManager.LimitBreakResult.AlreadyMaxLimitBreak:
                message = "이미 최대 한계돌파입니다";
                return false;
                
            case RuneEnhanceManager.LimitBreakResult.BaseRuneNotMaxLevel:
                message = $"최대 레벨 도달 후 가능 (현재: Lv.{selectedRune.currentLevel}/{selectedRune.GetCurrentMaxLevel()})";
                return false;
                
            case RuneEnhanceManager.LimitBreakResult.InsufficientGold:
                message = $"파편 부족 (필요: 200개)";
                return false;
                
            default:
                message = "한계돌파 실패";
                return false;
        }
    }
    
    /// <summary>
    /// 액션 결과 메시지 표시
    /// </summary>
    private void ShowActionMessage(string message, bool isSuccess)
    {
        if (actionMessageText == null) return;
        
        actionMessageText.text = message;
        actionMessageText.color = isSuccess ? Color.green : Color.red;
        actionMessageText.gameObject.SetActive(true);
        
        // 2초 후 자동 숨김
        CancelInvoke(nameof(HideActionMessage));
        Invoke(nameof(HideActionMessage), 2f);
    }
    
    private void HideActionMessage()
    {
        if (actionMessageText != null)
        {
            actionMessageText.gameObject.SetActive(false);
        }
    }
    
    #endregion
    
    #region 유틸리티
    
    /// <summary>
    /// 룬 타입 텍스트 변환
    /// </summary>
    private string GetRuneTypeText(RuneType runeType)
    {
        switch (runeType)
        {
            case RuneType.Attack1: return "공격형 I";
            case RuneType.Attack2: return "공격형 II";
            case RuneType.Attack3: return "공격형 III";
            case RuneType.Survival1: return "생존형 I";
            case RuneType.Survival2: return "생존형 II";
            case RuneType.Survival3: return "생존형 III";
            case RuneType.Utility1: return "유틸리티 I";
            case RuneType.Utility2: return "유틸리티 II";
            default: return runeType.ToString();
        }
    }
    
    /// <summary>
    /// 스탯 값 포맷팅
    /// </summary>
    private string FormatStatValue(ConditionalModifier modifier, float value)
    {
        switch (modifier.unit)
        {
            case StatUnit.Flat:
                return $"+{value:F1}";
            
            case StatUnit.Percent:
                return $"+{value:F1}%";
            
            case StatUnit.Bool:
                return value > 0 ? "활성화" : "비활성화";
            
            default:
                return value.ToString("F1");
        }
    }
    
    /// <summary>
    /// 다음 부옵션 마일스톤 계산
    /// </summary>
    private int GetNextSubStatMilestone(int currentLevel)
    {
        int[] milestones = { 3, 6, 9 };
        
        foreach (int milestone in milestones)
        {
            if (currentLevel < milestone)
            {
                return milestone;
            }
        }
        
        return -1; // 마일스톤 없음
    }
    
    #endregion
}
