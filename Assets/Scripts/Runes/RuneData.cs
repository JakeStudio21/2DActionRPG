using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 룬 데이터 ScriptableObject
/// ⚙️ Phase 4-C: 로비에서 장착하는 패시브 스킬 시스템
/// 일반 스탯(StatModifier)이 아닌 특수 조건부 스탯(ConditionalModifier) 전용
/// </summary>
[CreateAssetMenu(fileName = "Rune_", menuName = "Data/Rune", order = 100)]
public class RuneData : ScriptableObject
{
    #region 기본 정보
    
    [Header("=== 기본 정보 ===")]
    [Tooltip("룬의 고유 ID (예: RUNE_BOSS_HUNTER)")]
    public string runeId;
    
    [Tooltip("룬의 표시 이름 (예: 보스 사냥꾼)")]
    public string runeName;
    
    [Tooltip("룬 등급")]
    public RuneRarity rarity = RuneRarity.Common;
    
    [Tooltip("룬 설명 (UI 표시용)")]
    [TextArea(3, 5)]
    public string description;
    
    [Tooltip("룬 아이콘 (UI 표시용)")]
    public Sprite icon;
    
    #endregion
    
    #region 조건부 효과 (핵심)
    
    [Header("=== 조건부 효과 (ConditionalModifier) ===")]
    [Tooltip("이 룬이 발동시킬 ConditionalModifier ID 목록")]
    [SerializeField]
    private List<string> conditionalModifierIds = new List<string>();
    
    /// <summary>
    /// 조건부 모디파이어 ID 목록 (읽기 전용)
    /// </summary>
    public IReadOnlyList<string> ConditionalModifierIds => conditionalModifierIds;
    
    #endregion
    
    #region 메타데이터
    
    [Header("=== 메타데이터 ===")]
    [Tooltip("획득 방법 (예: 보스 드랍, 상점 구매)")]
    public string obtainMethod;
    
    [Tooltip("필요 레벨")]
    public int requiredLevel = 1;
    
    #endregion
    
    #region 유틸리티 메서드
    
    /// <summary>
    /// 룬이 유효한지 검증 (ID와 이름이 있는지)
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(runeId) && !string.IsNullOrEmpty(runeName);
    }
    
    /// <summary>
    /// 디버그용 ToString
    /// </summary>
    public override string ToString()
    {
        return $"[{runeId}] {runeName} ({rarity}) - {conditionalModifierIds.Count}개 효과";
    }
    
    /// <summary>
    /// Inspector에서 효과 목록 미리보기
    /// </summary>
    [ContextMenu("Preview Effects")]
    private void PreviewEffects()
    {
        if (conditionalModifierIds.Count == 0)
        {
            Debug.LogWarning($"[{runeName}] 조건부 효과가 없습니다.");
            return;
        }
        
        Debug.Log($"[{runeName}] 조건부 효과:");
        foreach (var modId in conditionalModifierIds)
        {
            var mod = ConditionalModifierDatabase.GetModifierById(modId);
            if (mod != null)
            {
                Debug.Log($"  - {mod.displayName}: {mod.effectType} ({mod.conditionType})");
            }
            else
            {
                Debug.LogWarning($"  - {modId}: 모디파이어를 찾을 수 없음!");
            }
        }
    }
    
    #endregion
}

/// <summary>
/// 룬 등급
/// </summary>
public enum RuneRarity
{
    Common,     // 일반 (회색)
    Uncommon,   // 고급 (녹색)
    Rare,       // 희귀 (파랑)
    Epic,       // 영웅 (보라)
    Legendary   // 전설 (주황)
}

