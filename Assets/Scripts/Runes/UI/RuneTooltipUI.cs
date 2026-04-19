using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ⚠️⚠️⚠️ [DEPRECATED - Phase 6] ⚠️⚠️⚠️
// 이 파일은 Phase 6 UI 리팩토링으로 인해 더 이상 사용되지 않습니다.
// 대체: RuneDetailUI.cs
// 
// Phase 6에서는 Tooltip + Enhance가 하나의 RuneDetailUI로 통합되었습니다.
// - 기존: 분리된 Tooltip (상세 정보만)
// - 신규: 통합된 RuneDetailUI (상세 정보 + 액션 버튼)
// 
// 이 파일은 참고용으로만 보관되며, 실제 게임에서는 사용되지 않습니다.
// ⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️

/// <summary>
/// [DEPRECATED] 룬 상세 정보 툴팁 UI
/// ⚙️ Phase 5-1: 룬 UI 시스템
/// 
/// 역할:
/// - 선택된 룬의 상세 스탯 표시
/// - 주옵션: 레벨 스케일링 적용된 값 표시
/// - 부옵션: 원본 값 표시
/// - 빈 부옵션 슬롯: 안내 문구 표시
/// 
/// 중요:
/// ⚠️ 데이터를 수정하지 않고 오직 읽기만!
/// ⚠️ Phase 4에서 검증된 스케일링 로직 활용
/// </summary>
[System.Obsolete("Phase 6에서 RuneDetailUI로 대체됨. 이 클래스는 더 이상 사용되지 않습니다.", false)]
public class RuneTooltipUI : MonoBehaviour
{
    #region UI 컴포넌트
    
    [Header("=== 기본 정보 ===")]
    [SerializeField] private Image runeIconImage;
    [SerializeField] private TextMeshProUGUI runeNameText;
    [SerializeField] private TextMeshProUGUI runeTypeText;
    [SerializeField] private TextMeshProUGUI obtainMethodText;
    
    [Header("=== 레벨 & 한계돌파 ===")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI limitBreakText;
    
    [Header("=== 주옵션 (Main Stat) ===")]
    [SerializeField] private GameObject mainStatContainer;
    [SerializeField] private TextMeshProUGUI mainStatNameText;
    [SerializeField] private TextMeshProUGUI mainStatValueText;
    
    [Header("=== 부옵션 (Sub Stats) ===")]
    [SerializeField] private GameObject subStatsContainer;
    [SerializeField] private Transform subStatsList; // 부옵션들이 들어갈 부모
    [SerializeField] private GameObject subStatRowPrefab; // 개별 부옵션 행 프리팹
    
    [Header("=== 빈 슬롯 안내 ===")]
    [SerializeField] private Color emptySlotTextColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    
    [Header("=== 툴팁 패널 ===")]
    [SerializeField] private GameObject tooltipPanel;
    
    [Header("=== 디버그 ===")]
    #endregion
    
    #region 내부 데이터
    
    private RuneInstance currentRune;
    private List<GameObject> subStatRows = new List<GameObject>();
    
    #endregion
    
    #region 초기화
    
    private void Awake()
    {
        // 초기에는 숨김
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// 툴팁 표시
    /// </summary>
    public void ShowTooltip(RuneInstance rune)
    {
        if (rune == null || rune.baseData == null)
        {
            Debug.LogWarning("[RuneTooltipUI] 룬 데이터가 null입니다.");
            HideTooltip();
            return;
        }
        
        currentRune = rune;
        
        // UI 갱신
        UpdateBasicInfo();
        UpdateLevelInfo();
        UpdateMainStat();
        UpdateSubStats();
        
        // 툴팁 표시
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(true);
        }
        
        {
        }
    }
    
    /// <summary>
    /// 툴팁 숨김
    /// </summary>
    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
        
        currentRune = null;
    }
    
    /// <summary>
    /// 현재 표시 중인 룬 반환
    /// </summary>
    public RuneInstance GetCurrentRune()
    {
        return currentRune;
    }
    
    #endregion
    
    #region UI 업데이트
    
    /// <summary>
    /// 기본 정보 업데이트
    /// </summary>
    private void UpdateBasicInfo()
    {
        if (currentRune == null) return;
        
        // 아이콘
        if (runeIconImage != null && currentRune.baseData.icon != null)
        {
            runeIconImage.sprite = currentRune.baseData.icon;
        }
        
        // 이름
        if (runeNameText != null)
        {
            runeNameText.text = currentRune.baseData.runeName;
        }
        
        // 타입
        if (runeTypeText != null)
        {
            runeTypeText.text = $"타입: {currentRune.baseData.runeType}";
        }
        
        // 획득처
        if (obtainMethodText != null)
        {
            obtainMethodText.text = $"획득처: {currentRune.baseData.obtainMethod}";
        }
    }
    
    /// <summary>
    /// 레벨 & 한계돌파 정보 업데이트
    /// </summary>
    private void UpdateLevelInfo()
    {
        if (currentRune == null) return;
        
        // 레벨
        if (levelText != null)
        {
            int currentLevel = currentRune.currentLevel;
            int maxLevel = currentRune.GetCurrentMaxLevel();
            
            levelText.text = $"레벨: {currentLevel} / {maxLevel}";
            
            // 최대 레벨 도달 시 강조
            if (currentLevel >= maxLevel)
            {
                levelText.color = Color.yellow;
            }
            else
            {
                levelText.color = Color.white;
            }
        }
        
        // 한계돌파
        if (limitBreakText != null)
        {
            int currentLimitBreak = currentRune.currentLimitBreak;
            int maxLimitBreak = currentRune.baseData.maxLimitBreak;
            
            limitBreakText.text = $"한계돌파: {currentLimitBreak} / {maxLimitBreak}";
            
            // 최대 한계돌파 도달 시 강조
            if (currentLimitBreak >= maxLimitBreak)
            {
                limitBreakText.color = Color.cyan;
            }
            else
            {
                limitBreakText.color = Color.white;
            }
        }
    }
    
    /// <summary>
    /// 주옵션 업데이트 (레벨 스케일링 적용)
    /// ⚡ Phase 4에서 검증된 로직 활용
    /// </summary>
    private void UpdateMainStat()
    {
        if (currentRune == null) return;
        
        string mainModId = currentRune.baseData.MainStatModifierId;
        
        if (string.IsNullOrEmpty(mainModId))
        {
            // 주옵션 없음
            if (mainStatContainer != null)
            {
                mainStatContainer.SetActive(false);
            }
            return;
        }
        
        // 주옵션 컨테이너 활성화
        if (mainStatContainer != null)
        {
            mainStatContainer.SetActive(true);
        }
        
        // ConditionalModifierDatabase에서 원본 가져오기
        var modifier = ConditionalModifierDatabase.GetModifierById(mainModId);
        
        if (modifier == null)
        {
            Debug.LogWarning($"[RuneTooltipUI] 주옵션 ID를 찾을 수 없습니다: {mainModId}");
            
            if (mainStatNameText != null)
            {
                mainStatNameText.text = "주옵션: 알 수 없음";
            }
            if (mainStatValueText != null)
            {
                mainStatValueText.text = "???";
            }
            return;
        }
        
        // ⚡ 레벨 스케일링 적용
        float scaledValue = modifier.value * currentRune.GetMainStatMultiplier();
        
        // 주옵션 이름
        if (mainStatNameText != null)
        {
            mainStatNameText.text = $"주옵션: {modifier.displayName}";
        }
        
        // 주옵션 값 (단위 포함)
        if (mainStatValueText != null)
        {
            string valueStr = FormatStatValue(scaledValue, modifier.unit);
            mainStatValueText.text = valueStr;
        }
        
        {
        }
    }
    
    /// <summary>
    /// 부옵션 업데이트 (원본 값 그대로)
    /// </summary>
    private void UpdateSubStats()
    {
        if (currentRune == null) return;
        
        // 기존 부옵션 행 제거
        ClearSubStatRows();
        
        var subStatIds = currentRune.allocatedSubStatModifierIds;
        
        // 부옵션이 없으면 컨테이너 숨김
        if (subStatsContainer != null)
        {
            bool hasSubStats = subStatIds.Count > 0;
            subStatsContainer.SetActive(hasSubStats || ShowEmptySlotGuides());
        }
        
        // 부옵션 표시
        foreach (var subModId in subStatIds)
        {
            var modifier = ConditionalModifierDatabase.GetModifierById(subModId);
            
            if (modifier != null)
            {
                CreateSubStatRow(modifier.displayName, modifier.value, modifier.unit, false);
            }
            else
            {
                Debug.LogWarning($"[RuneTooltipUI] 부옵션 ID를 찾을 수 없습니다: {subModId}");
                CreateSubStatRow("알 수 없음", 0f, StatUnit.Flat, false);
            }
        }
        
        // 빈 슬롯 안내 표시 (3, 6, 9레벨 마일스톤)
        if (ShowEmptySlotGuides())
        {
            int currentLevel = currentRune.currentLevel;
            
            // Lv.3 미도달
            if (currentLevel < 3)
            {
                CreateEmptySlotGuide("Lv.3 도달 시 부옵션 개방");
            }
            
            // Lv.6 미도달
            if (currentLevel < 6)
            {
                CreateEmptySlotGuide("Lv.6 도달 시 부옵션 개방");
            }
            
            // Lv.9 미도달
            if (currentLevel < 9)
            {
                CreateEmptySlotGuide("Lv.9 도달 시 부옵션 개방");
            }
        }
    }
    
    /// <summary>
    /// 빈 슬롯 안내를 표시할지 여부
    /// </summary>
    private bool ShowEmptySlotGuides()
    {
        if (currentRune == null) return false;
        
        int currentLevel = currentRune.currentLevel;
        int subStatCount = currentRune.allocatedSubStatModifierIds.Count;
        
        // 3개 미만이고, 아직 개방 가능한 레벨이 남아있으면 표시
        return subStatCount < 3 && currentLevel < 9;
    }
    
    /// <summary>
    /// 부옵션 행 생성
    /// </summary>
    private void CreateSubStatRow(string statName, float value, StatUnit unit, bool isEmpty)
    {
        if (subStatRowPrefab == null || subStatsList == null)
        {
            Debug.LogWarning("[RuneTooltipUI] 부옵션 행 프리팹 또는 리스트가 null입니다.");
            return;
        }
        
        GameObject row = Instantiate(subStatRowPrefab, subStatsList);
        subStatRows.Add(row);
        
        // 행 내부의 TextMeshProUGUI 찾기
        var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
        
        if (texts.Length >= 2)
        {
            // 첫 번째: 부옵션 이름
            texts[0].text = statName;
            
            // 두 번째: 부옵션 값
            texts[1].text = FormatStatValue(value, unit);
            
            // 색상 설정
            if (isEmpty)
            {
                texts[0].color = emptySlotTextColor;
                texts[1].color = emptySlotTextColor;
            }
        }
    }
    
    /// <summary>
    /// 빈 슬롯 안내 행 생성
    /// </summary>
    private void CreateEmptySlotGuide(string guideText)
    {
        if (subStatRowPrefab == null || subStatsList == null)
        {
            return;
        }
        
        GameObject row = Instantiate(subStatRowPrefab, subStatsList);
        subStatRows.Add(row);
        
        // 행 내부의 TextMeshProUGUI 찾기
        var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
        
        if (texts.Length >= 1)
        {
            // 첫 번째만 사용 (가이드 텍스트)
            texts[0].text = guideText;
            texts[0].color = emptySlotTextColor;
            
            // 두 번째는 숨김
            if (texts.Length >= 2)
            {
                texts[1].text = "";
            }
        }
    }
    
    /// <summary>
    /// 기존 부옵션 행 제거
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
    
    #endregion
    
    #region 유틸리티
    
    /// <summary>
    /// 스탯 값 포맷팅 (단위 포함)
    /// </summary>
    private string FormatStatValue(float value, StatUnit unit)
    {
        switch (unit)
        {
            case StatUnit.Percent:
                return $"{value * 100f:F1}%";
            
            case StatUnit.Flat:
                return $"+{value:F0}";
            
            case StatUnit.Bool:
                return value > 0 ? "활성" : "비활성";
            
            default:
                return $"{value:F1}";
        }
    }
    
    #endregion
}
