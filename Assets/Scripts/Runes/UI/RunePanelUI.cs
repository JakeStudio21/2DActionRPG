using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

/// <summary>
/// 룬 패널 메인 컨트롤러
/// ⚙️ Phase 6.5: 4분할 구조 + 8종류 고유 조각 시스템
/// 
/// 구조:
/// - TopPanel: 공용 재화 (골드, 크리스탈)
/// - LeftPanel: 장착 슬롯 (3개)
/// - RightPanel: 룬 목록 (RuneListItemUI)
/// - BottomPanel: 스탯 변화량 & 부옵션 개방 안내
/// 
/// 위치:
/// - SkillBookPanelUI > RuneSubPanel > RunePanelUI (컴포넌트)
/// 
/// 중요:
/// ⚠️ RefreshUI() 호출 시 모든 패널 동기화
/// ⚠️ 스킬 시스템(SkillTabController)과 동일한 구조 유지
/// </summary>
public class RunePanelUI : MonoBehaviour
{
    #region UI 컴포넌트
    
    [Header("=== Top Panel: 공용 재화 ===")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI crystalText;
    [SerializeField] private TextMeshProUGUI titleText; // "룬 시스템"
    
    [Header("=== Left Panel: 장착 슬롯 (3개) ===")]
    [SerializeField] private RuneEquipSlotUI[] equipSlots; // 3개
    
    [Header("=== 경고 메시지 (TopPanel 또는 LeftPanel) ===")]
    [Tooltip("장착 슬롯이 가득 찼을 때 표시되는 경고 메시지")]
    [SerializeField] private GameObject warningMessageObject;
    [SerializeField] private TextMeshProUGUI warningMessageText;
    [SerializeField] private float warningDisplayDuration = 3f;
    
    [Header("=== Right Panel: 룬 목록 ===")]
    [SerializeField] private Transform runeListContainer;
    [SerializeField] private GameObject runeListItemPrefab; // RuneListItemUI
    [SerializeField] private ScrollRect scrollRect;
    
    [Header("=== Bottom Panel: 스탯 변화량 ===")]
    [SerializeField] private Image selectedRuneIconImage; // 선택된 룬 아이콘
    [SerializeField] private TextMeshProUGUI selectedRuneNameText;
    [SerializeField] private TextMeshProUGUI mainStatText; // 주옵션 (한 줄 포맷)
    [SerializeField] private TextMeshProUGUI descriptionText; // 룬 설명
    
    [Header("=== Bottom Panel: 부옵션 (3개 슬롯) ===")]
    [SerializeField] private TextMeshProUGUI subStat1Text;
    [SerializeField] private TextMeshProUGUI subStat2Text;
    [SerializeField] private TextMeshProUGUI subStat3Text;
    [SerializeField] private Color openedSubStatColor = Color.white;
    [SerializeField] private Color lockedSubStatColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    
    [Header("=== 디버그 ===")]
    #endregion
    
    #region 내부 데이터
    
    private RuneInventoryManager inventoryManager;
    private RuneEnhanceManager enhanceManager;
    private RuneManager runeManager;
    
    private List<RuneListItemUI> runeListItemUIList = new List<RuneListItemUI>();
    private RuneListItemUI currentSelectedItem;
    
    #endregion
    
    #region 초기화
    
    private void Awake()
    {
        inventoryManager = RuneInventoryManager.Instance;
        enhanceManager = RuneEnhanceManager.Instance;
        runeManager = RuneManager.Instance;
        
        if (inventoryManager == null)
        {
            Debug.LogError("[RunePanelUI] RuneInventoryManager를 찾을 수 없습니다!");
        }
        
        if (enhanceManager == null)
        {
            Debug.LogError("[RunePanelUI] RuneEnhanceManager를 찾을 수 없습니다!");
        }
        
        if (runeManager == null)
        {
            Debug.LogError("[RunePanelUI] RuneManager를 찾을 수 없습니다!");
        }
        
        // 장착 슬롯 초기화
        InitializeEquipSlots();
    }
    
    /// <summary>
    /// 장착 슬롯 초기화
    /// </summary>
    private void InitializeEquipSlots()
    {
        if (equipSlots == null) return;
        
        for (int i = 0; i < equipSlots.Length; i++)
        {
            if (equipSlots[i] != null)
            {
                equipSlots[i].Setup(i, this);
            }
        }
    }
    
    private void OnEnable()
    {
        // ⚠️ Phase 6.5: OnEnable에서 자동 초기화 제거
        // SkillBookPanelUI에서 명시적으로 OnTabActivated() 호출하도록 변경
        
        // 경고 메시지 초기 숨김
        if (warningMessageObject != null)
        {
            warningMessageObject.SetActive(false);
        }
        
        // 이벤트 구독
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged += OnInventoryChanged;
        }
        
        if (runeManager != null)
        {
            runeManager.OnRunesChanged += OnRunesChanged;
        }
        
        if (enhanceManager != null)
        {
            enhanceManager.OnLevelUp += OnLevelUpHandler;
            enhanceManager.OnLimitBreak += OnLimitBreakHandler;
        }
        
        {
        }
    }
    
    /// <summary>
    /// 탭 활성화 시 호출 (외부에서 - SkillBookPanelUI)
    /// Phase 6.5: 명시적 초기화 방식으로 변경
    /// </summary>
    public void OnTabActivated()
    {
        {
        }
        
        RefreshUI();
    }
    
    /// <summary>
    /// 탭 비활성화 시 호출 (외부에서 - SkillBookPanelUI)
    /// Phase 6.5: 명시적 정리 방식으로 변경
    /// </summary>
    public void OnTabDeactivated()
    {
        {
        }
        
        // 필요한 정리 작업
        currentSelectedItem = null;
    }
    
    private void OnDisable()
    {
        // 이벤트 구독 해제
        if (inventoryManager != null)
        {
            inventoryManager.OnInventoryChanged -= OnInventoryChanged;
        }
        
        if (runeManager != null)
        {
            runeManager.OnRunesChanged -= OnRunesChanged;
        }
        
        if (enhanceManager != null)
        {
            enhanceManager.OnLevelUp -= OnLevelUpHandler;
            enhanceManager.OnLimitBreak -= OnLimitBreakHandler;
        }
        
        {
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// 전체 UI 갱신
    /// </summary>
    public void RefreshUI()
    {
        // 1. Top Panel: 재화 표시
        UpdateTopPanel();
        
        // 2. Left Panel: 장착 슬롯
        UpdateEquipSlots();
        
        // 3. Right Panel: 룬 목록
        UpdateRuneList();
        
        // 4. Bottom Panel: 스탯 변화량 (선택된 룬이 있을 때만)
        UpdateBottomPanel();
        
        {
        }
    }
    
    #endregion
    
    #region Top Panel
    
    /// <summary>
    /// Top Panel 갱신 (재화 표시)
    /// </summary>
    private void UpdateTopPanel()
    {
        // TODO: 실제 CurrencyManager 연동
        if (goldText != null)
        {
            goldText.text = "골드: -"; // 추후 연동
        }
        
        if (crystalText != null)
        {
            crystalText.text = "크리스탈: -"; // 추후 연동
        }
        
        if (titleText != null)
        {
            titleText.text = "룬 시스템";
        }
    }
    
    #endregion
    
    #region Left Panel
    
    /// <summary>
    /// 장착 슬롯 갱신
    /// </summary>
    private void UpdateEquipSlots()
    {
        if (equipSlots == null || equipSlots.Length == 0)
        {
            return;
        }
        
        var equippedRunes = runeManager.GetEquippedRunes();
        
        for (int i = 0; i < equipSlots.Length; i++)
        {
            if (equipSlots[i] != null)
            {
                // 해당 슬롯에 룬이 있으면 설정, 없으면 비우기
                if (i < equippedRunes.Count && equippedRunes[i] != null)
                {
                    equipSlots[i].SetRune(equippedRunes[i]);
                }
                else
                {
                    equipSlots[i].Clear();
                }
            }
        }
    }
    
    #endregion
    
    #region Right Panel
    
    /// <summary>
    /// 룬 목록 갱신
    /// </summary>
    private void UpdateRuneList()
    {
        if (runeListContainer == null || runeListItemPrefab == null)
        {
            Debug.LogError("[RunePanelUI] runeListContainer 또는 runeListItemPrefab이 null입니다!");
            return;
        }
        
        // 기존 목록 제거
        ClearRuneList();
        
        // 전체 룬 데이터 로드
        var allRuneData = LoadAllRuneData();
        
        // 룬 아이템 생성
        foreach (var runeData in allRuneData)
        {
            // 보유 여부 확인
            var ownedRunes = inventoryManager.GetRunesByDataId(runeData.runeId);
            
            if (ownedRunes.Count > 0)
            {
                // 보유 룬
                CreateRuneListItem(ownedRunes[0]);
            }
            else
            {
                // 미보유 룬
                CreateRuneListItem(runeData);
            }
        }
        
        {
        }
    }
    
    /// <summary>
    /// 전체 룬 데이터 로드
    /// </summary>
    private List<RuneData> LoadAllRuneData()
    {
        var allRunes = Resources.LoadAll<RuneData>("Runes");
        
        return allRunes
            .OrderBy(r => r.runeType)
            .ThenBy(r => r.runeName)
            .ToList();
    }
    
    /// <summary>
    /// 보유 룬 아이템 생성
    /// </summary>
    private void CreateRuneListItem(RuneInstance rune)
    {
        GameObject itemObj = Instantiate(runeListItemPrefab, runeListContainer);
        var itemUI = itemObj.GetComponent<RuneListItemUI>();
        
        if (itemUI == null)
        {
            Debug.LogError("[RunePanelUI] RuneListItemUI 컴포넌트를 찾을 수 없습니다!");
            Destroy(itemObj);
            return;
        }
        
        itemUI.Setup(rune, OnRuneListItemClicked, this);
        runeListItemUIList.Add(itemUI);
    }
    
    /// <summary>
    /// 미보유 룬 아이템 생성
    /// </summary>
    private void CreateRuneListItem(RuneData runeData)
    {
        GameObject itemObj = Instantiate(runeListItemPrefab, runeListContainer);
        var itemUI = itemObj.GetComponent<RuneListItemUI>();
        
        if (itemUI == null)
        {
            Debug.LogError("[RunePanelUI] RuneListItemUI 컴포넌트를 찾을 수 없습니다!");
            Destroy(itemObj);
            return;
        }
        
        itemUI.SetupAsLocked(runeData, OnRuneListItemClicked, this);
        runeListItemUIList.Add(itemUI);
    }
    
    /// <summary>
    /// 룬 목록 전체 제거
    /// </summary>
    private void ClearRuneList()
    {
        foreach (var itemUI in runeListItemUIList)
        {
            if (itemUI != null && itemUI.gameObject != null)
            {
                Destroy(itemUI.gameObject);
            }
        }
        
        runeListItemUIList.Clear();
        currentSelectedItem = null;
    }
    
    #endregion
    
    #region Bottom Panel
    
    /// <summary>
    /// Bottom Panel 갱신 (스탯 변화량)
    /// </summary>
    private void UpdateBottomPanel()
    {
        if (currentSelectedItem == null)
        {
            // 선택된 룬 없음
            HideBottomPanel();
            return;
        }
        
        var rune = currentSelectedItem.GetRuneInstance();
        var runeData = currentSelectedItem.GetRuneData();
        
        if (runeData == null)
        {
            HideBottomPanel();
            return;
        }
        
        // 선택된 룬 아이콘
        if (selectedRuneIconImage != null)
        {
            selectedRuneIconImage.sprite = runeData.icon;
            selectedRuneIconImage.gameObject.SetActive(true);
        }
        
        // 선택된 룬 이름
        if (selectedRuneNameText != null)
        {
            selectedRuneNameText.text = currentSelectedItem.IsOwned() 
                ? $"{runeData.runeName} (Lv.{rune.currentLevel})" 
                : $"{runeData.runeName} (미보유)";
        }
        
        // 스탯 변화량
        if (currentSelectedItem.IsOwned() && rune != null)
        {
            UpdateStatComparison(rune);
            UpdateSubStatInfo(rune);
        }
        else
        {
            UpdateLockedRuneInfo(runeData);
        }
    }
    
    /// <summary>
    /// 보유 룬 스탯 비교 표시 (Phase 7-2: 한 줄 포맷)
    /// </summary>
    private void UpdateStatComparison(RuneInstance rune)
    {
        var mainModifier = ConditionalModifierDatabase.GetModifierById(rune.baseData.MainStatModifierId);
        
        if (mainModifier == null)
        {
            if (mainStatText != null) mainStatText.text = "주옵션: -";
            return;
        }
        
        int currentLevel = rune.currentLevel;
        int maxLevel = rune.GetCurrentMaxLevel();
        
        // 현재 스탯
        float currentValue = mainModifier.value * rune.GetMainStatMultiplier();
        string currentStr = FormatStatValueOnly(mainModifier, currentValue);
        
        if (mainStatText != null)
        {
            if (currentLevel >= maxLevel)
            {
                // 만렙: "보스 피해 증가: 34% (MAX)"
                mainStatText.text = $"{mainModifier.displayName}: {currentStr} (MAX)";
            }
            else
            {
                // 일반: "보스 피해 증가: 20% -> 22%"
                float nextMultiplier = rune.baseData.GetMainStatMultiplier(currentLevel + 1);
                float nextValue = mainModifier.value * nextMultiplier;
                string nextStr = FormatStatValueOnly(mainModifier, nextValue);
                
                mainStatText.text = $"{mainModifier.displayName}: {currentStr} -> {nextStr}";
            }
        }
        
        // 룬 설명 표시
        if (descriptionText != null)
        {
            descriptionText.text = rune.baseData.description ?? "";
        }
    }
    
    /// <summary>
    /// 부옵션 개방 정보 표시 (Phase 7-2: 3개 슬롯 상세 표시)
    /// </summary>
    private void UpdateSubStatInfo(RuneInstance rune)
    {
        var subStatTexts = new[] { subStat1Text, subStat2Text, subStat3Text };
        int[] milestones = { 3, 6, 9 };
        
        for (int i = 0; i < 3; i++)
        {
            if (subStatTexts[i] == null) continue;
            
            if (i < rune.allocatedSubStatModifierIds.Count)
            {
                // 개방된 부옵션: 실제 스탯 표시
                string modifierId = rune.allocatedSubStatModifierIds[i];
                var modifier = ConditionalModifierDatabase.GetModifierById(modifierId);
                
                if (modifier != null)
                {
                    string valueStr = FormatStatValueOnly(modifier, modifier.value);
                    subStatTexts[i].text = $"• {modifier.displayName}: {valueStr}";
                    subStatTexts[i].color = openedSubStatColor;
                }
                else
                {
                    subStatTexts[i].text = $"• 부옵션 {i + 1}";
                    subStatTexts[i].color = openedSubStatColor;
                }
            }
            else
            {
                // 미개방 슬롯: "Lv.X 개방" 안내
                subStatTexts[i].text = $"• Lv.{milestones[i]} 개방";
                subStatTexts[i].color = lockedSubStatColor;
            }
        }
    }
    
    /// <summary>
    /// 미보유 룬 정보 표시 (Phase 7-2)
    /// </summary>
    private void UpdateLockedRuneInfo(RuneData runeData)
    {
        var mainModifier = ConditionalModifierDatabase.GetModifierById(runeData.MainStatModifierId);
        
        if (mainStatText != null)
        {
            if (mainModifier != null)
            {
                float baseValue = mainModifier.value * runeData.GetMainStatMultiplier(1);
                string valueStr = FormatStatValueOnly(mainModifier, baseValue);
                mainStatText.text = $"{mainModifier.displayName}: {valueStr} (Lv.1)";
            }
            else
            {
                mainStatText.text = "주옵션: -";
            }
        }
        
        // 룬 설명
        if (descriptionText != null)
        {
            descriptionText.text = runeData.description ?? "해금 후 성장 가능";
        }
        
        // 부옵션 슬롯 (미개방 안내)
        var subStatTexts = new[] { subStat1Text, subStat2Text, subStat3Text };
        int[] milestones = { 3, 6, 9 };
        
        for (int i = 0; i < 3; i++)
        {
            if (subStatTexts[i] != null)
            {
                subStatTexts[i].text = $"• Lv.{milestones[i]} 개방";
                subStatTexts[i].color = lockedSubStatColor;
            }
        }
    }
    
    /// <summary>
    /// Bottom Panel 숨김 (Phase 7-2)
    /// </summary>
    private void HideBottomPanel()
    {
        if (selectedRuneIconImage != null) selectedRuneIconImage.gameObject.SetActive(false);
        if (selectedRuneNameText != null) selectedRuneNameText.text = "룬을 선택하세요";
        if (mainStatText != null) mainStatText.text = "";
        if (descriptionText != null) descriptionText.text = "";
        
        // 부옵션 슬롯 비우기
        var subStatTexts = new[] { subStat1Text, subStat2Text, subStat3Text };
        foreach (var text in subStatTexts)
        {
            if (text != null) text.text = "";
        }
    }
    
    #endregion
    
    #region 이벤트 처리
    
    /// <summary>
    /// 룬 목록 아이템 클릭
    /// </summary>
    private void OnRuneListItemClicked(RuneListItemUI clickedItem)
    {
        if (clickedItem == null)
        {
            return;
        }
        
        // 이전 선택 해제
        if (currentSelectedItem != null)
        {
            currentSelectedItem.SetSelected(false);
        }
        
        // 새로운 선택
        currentSelectedItem = clickedItem;
        currentSelectedItem.SetSelected(true);
        
        // Bottom Panel 업데이트
        UpdateBottomPanel();
        
        {
            var rune = clickedItem.GetRuneInstance();
            var data = clickedItem.GetRuneData();
            Dbg.Log($"[RunePanelUI] 선택: {(rune != null ? rune.ToString() : data.runeName)}");
        }
    }
    
    /// <summary>
    /// 장착된 룬 상세 정보 표시 (Phase 7-2: 좌측 슬롯 클릭 시)
    /// </summary>
    public void ShowEquippedRuneDetail(RuneInstance rune)
    {
        if (rune == null) return;
        
        // 이전 선택 해제
        if (currentSelectedItem != null)
        {
            currentSelectedItem.SetSelected(false);
            currentSelectedItem = null;
        }
        
        // 하단창에 룬 정보 표시
        if (selectedRuneIconImage != null)
        {
            selectedRuneIconImage.sprite = rune.baseData.icon;
            selectedRuneIconImage.gameObject.SetActive(true);
        }
        
        if (selectedRuneNameText != null)
        {
            selectedRuneNameText.text = $"{rune.baseData.runeName} (Lv.{rune.currentLevel})";
        }
        
        UpdateStatComparison(rune);
        UpdateSubStatInfo(rune);
        
        {
            Dbg.Log($"[RunePanelUI] 장착 슬롯에서 선택: {rune}");
        }
    }
    
    /// <summary>
    /// 경고 메시지 표시 (장착 슬롯이 가득 찼을 때)
    /// </summary>
    public void ShowWarningMessage(string message)
    {
        if (warningMessageObject == null || warningMessageText == null)
        {
            Debug.LogWarning("[RunePanelUI] 경고 메시지 UI가 설정되지 않았습니다!");
            return;
        }
        
        // 기존 코루틴 중지
        StopAllCoroutines();
        
        // 메시지 설정 및 표시
        warningMessageText.text = message;
        warningMessageObject.SetActive(true);
        
        // 일정 시간 후 자동 숨김
        StartCoroutine(HideWarningMessageAfterDelay());
        
        {
        }
    }
    
    /// <summary>
    /// 경고 메시지 자동 숨김 (코루틴)
    /// </summary>
    private System.Collections.IEnumerator HideWarningMessageAfterDelay()
    {
        yield return new WaitForSeconds(warningDisplayDuration);
        
        if (warningMessageObject != null)
        {
            warningMessageObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 인벤토리 변경 이벤트
    /// </summary>
    private void OnInventoryChanged()
    {
        RefreshUI();
        
        {
        }
    }
    
    /// <summary>
    /// 장착 룬 변경 이벤트
    /// </summary>
    private void OnRunesChanged()
    {
        // 장착 슬롯과 목록 갱신
        UpdateEquipSlots();
        UpdateRuneList();
        
        {
        }
    }
    
    /// <summary>
    /// 룬 레벨업 이벤트 핸들러
    /// OnInventoryChanged(조각 차감)가 구버전 레벨로 목록을 먼저 그리므로,
    /// currentLevel이 실제로 올라간 이후 이 이벤트로 목록을 재갱신
    /// </summary>
    private void OnLevelUpHandler(string runeUID, int newLevel)
    {
        UpdateRuneList();
    }
    
    /// <summary>
    /// 룬 한계돌파 이벤트 핸들러
    /// OnInventoryChanged(조각 차감)가 구버전 상태로 목록을 먼저 그리므로,
    /// currentLimitBreak가 실제로 올라간 이후 이 이벤트로 목록을 재갱신
    /// </summary>
    private void OnLimitBreakHandler(string runeUID, int newLimitBreak)
    {
        UpdateRuneList();
    }
    
    #endregion
    
    #region 유틸리티
    
    /// <summary>
    /// 스탯 값 포맷팅 (기호 포함)
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
    /// 스탯 값 포맷팅 (숫자만, Phase 7-2용)
    /// </summary>
    private string FormatStatValueOnly(ConditionalModifier modifier, float value)
    {
        switch (modifier.unit)
        {
            case StatUnit.Flat:
                return $"{value:F1}";
            
            case StatUnit.Percent:
                return $"{value:F1}%";
            
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
        
        return -1;
    }
    
    #endregion
}
