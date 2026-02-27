using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스킬 상세 정보 패널 (하단)
/// Phase 3-Revision: 읽기 전용, 버튼 없음
/// </summary>
public class SkillDetailPanel : MonoBehaviour
{
    [Header("🎨 기본 정보 UI")]
    [SerializeField] private Image skillIcon;
    [SerializeField] private TextMeshProUGUI skillNameText;
    [SerializeField] private TextMeshProUGUI skillDescriptionText;
    [SerializeField] private TextMeshProUGUI skillTypeText;
    
    [Header("📊 레벨 정보 UI")]
    [SerializeField] private TextMeshProUGUI currentLevelText;
    [SerializeField] private TextMeshProUGUI maxLevelText;
    [SerializeField] private TextMeshProUGUI requiredSPText;
    [SerializeField] private TextMeshProUGUI unlockLevelText;
    
    [Header("📈 스탯 정보 UI")]
    [SerializeField] private TextMeshProUGUI currentStatsText;
    [SerializeField] private TextMeshProUGUI nextStatsText;
    [SerializeField] private GameObject statsComparePanel;
    
    [Header("🔗 참조")]
    private SkillInstance currentSkill;
    
    [Header("🔧 디버그")]
    public bool showDebugLogs = false;
    
    /// <summary>
    /// 스킬 상세 정보 표시
    /// </summary>
    public void ShowSkillDetail(SkillInstance skill, int playerLevel)
    {
        currentSkill = skill;
        
        if (skill == null || skill.skillData == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        
        // 기본 정보
        if (skillNameText != null)
            skillNameText.text = skill.skillData.skillName;
        
        if (skillDescriptionText != null)
            skillDescriptionText.text = skill.skillData.description;
        
        if (skillIcon != null && skill.skillData.icon != null)
            skillIcon.sprite = skill.skillData.icon;
        
        // 스킬 타입
        if (skillTypeText != null)
        {
            if (skill.IsActiveSkill)
            {
                var activeData = skill.skillData as ActiveSkillData;
                skillTypeText.text = $"[액티브] {activeData.skillType}";
            }
            else if (skill.IsPassiveSkill)
            {
                var passiveData = skill.skillData as PassiveSkillData;
                skillTypeText.text = $"[패시브] {passiveData.passiveType}";
            }
        }
        
        // 레벨 정보
        UpdateLevelInfo(skill, playerLevel);
        
        // 스탯 정보
        UpdateStatsInfo(skill);
        
        if (showDebugLogs)
            Debug.Log($"📋 [SkillDetailPanel] {skill.skillData.skillName} 상세 정보 표시");
    }
    
    /// <summary>
    /// 레벨 정보 갱신
    /// </summary>
    private void UpdateLevelInfo(SkillInstance skill, int playerLevel)
    {
        // 현재 레벨 / 최대 레벨 통합 표시
        if (currentLevelText != null)
        {
            if (skill.currentLevel > 0)
                currentLevelText.text = $"Lv.{skill.currentLevel}/{skill.skillData.maxLevel}";
            else
                currentLevelText.text = "미해금";
        }
        
        // 최대 레벨 텍스트는 숨김 (통합 표시 사용)
        if (maxLevelText != null)
        {
            maxLevelText.gameObject.SetActive(false);
        }
        
        // 필요 SP
        if (requiredSPText != null)
        {
            if (skill.IsMaxLevel)
            {
                requiredSPText.text = "---";
            }
            else
            {
                int requiredSP = skill.GetRequiredSPForNextLevel();
                requiredSPText.text = $"다음 레벨 SP: {requiredSP}";
            }
        }
        
        // 해금 레벨
        if (unlockLevelText != null)
        {
            bool isLocked = playerLevel < skill.skillData.unlockLevel;
            if (isLocked)
            {
                unlockLevelText.text = $"🔒 필요 레벨: Lv.{skill.skillData.unlockLevel}";
                unlockLevelText.color = Color.red;
            }
            else
            {
                unlockLevelText.text = $"✅ 해금 레벨: Lv.{skill.skillData.unlockLevel}";
                unlockLevelText.color = Color.green;
            }
        }
    }
    
    /// <summary>
    /// 스탯 비교 정보 갱신
    /// </summary>
    private void UpdateStatsInfo(SkillInstance skill)
    {
        if (statsComparePanel != null)
        {
            // 만렙이거나 미해금이면 비교 패널 숨김
            bool showCompare = !skill.IsMaxLevel && skill.currentLevel > 0;
            statsComparePanel.SetActive(showCompare);
        }
        
        if (skill.IsActiveSkill)
        {
            UpdateActiveSkillStats(skill);
        }
        else if (skill.IsPassiveSkill)
        {
            UpdatePassiveSkillStats(skill);
        }
    }
    
    /// <summary>
    /// 액티브 스킬 스탯 표시
    /// </summary>
    private void UpdateActiveSkillStats(SkillInstance skill)
    {
        if (currentStatsText != null)
        {
            if (skill.currentLevel > 0)
            {
                float currentDamage = skill.GetCurrentDamage();
                float currentCooldown = skill.GetCurrentCooldown();
                currentStatsText.text = $"현재:\n데미지 {currentDamage:F0}%\n쿨다운 {currentCooldown:F1}초";
            }
            else
            {
                currentStatsText.text = "미해금";
            }
        }
        
        if (nextStatsText != null && !skill.IsMaxLevel)
        {
            var nextLevelInfo = skill.GetNextLevelInfo();
            if (nextLevelInfo.level > 0)
            {
                nextStatsText.text = $"다음:\n데미지 {nextLevelInfo.value1:F0}%\n쿨다운 {nextLevelInfo.value2:F1}초";
            }
            else
            {
                nextStatsText.text = "데이터 없음";
            }
        }
        else if (nextStatsText != null)
        {
            nextStatsText.text = "최대 레벨";
        }
    }
    
    /// <summary>
    /// 패시브 스킬 스탯 표시
    /// </summary>
    private void UpdatePassiveSkillStats(SkillInstance skill)
    {
        if (skill.skillData is PassiveSkillData passiveData)
        {
            if (currentStatsText != null)
            {
                if (skill.currentLevel > 0)
                {
                    string statsText = "현재:\n";
                    foreach (var modifier in passiveData.statModifiers)
                    {
                        float currentValue = skill.GetPassiveStatValue(modifier.statType);
                        string modType = modifier.modifierType == StatModifierType.Multiplicative ? "%" : "";
                        statsText += $"{modifier.statType} +{currentValue:F1}{modType}\n";
                    }
                    currentStatsText.text = statsText.TrimEnd('\n');
                }
                else
                {
                    currentStatsText.text = "미해금";
                }
            }
            
            if (nextStatsText != null && !skill.IsMaxLevel)
            {
                var nextLevelInfo = skill.GetNextLevelInfo();
                if (nextLevelInfo.level > 0)
                {
                    string statsText = "다음:\n";
                    foreach (var modifier in passiveData.statModifiers)
                    {
                        string modType = modifier.modifierType == StatModifierType.Multiplicative ? "%" : "";
                        statsText += $"{modifier.statType} +{nextLevelInfo.value1:F1}{modType}\n";
                    }
                    nextStatsText.text = statsText.TrimEnd('\n');
                }
                else
                {
                    nextStatsText.text = "데이터 없음";
                }
            }
            else if (nextStatsText != null)
            {
                nextStatsText.text = "최대 레벨";
            }
        }
    }
    
    /// <summary>
    /// 패널 숨기기
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        currentSkill = null;
    }
}
