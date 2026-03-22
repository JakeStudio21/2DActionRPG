using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 패시브 스킬 전용 데이터
/// 캐릭터 스탯에 영구 보너스 제공 (Phase 1)
/// </summary>
[CreateAssetMenu(fileName = "PassiveSkill_", menuName = "Skill System/Passive Skill Data")]
public class PassiveSkillData : BaseSkillData
{
    [Header("🛡️ 패시브 스킬 타입")]
    [Tooltip("패시브 카테고리 (전투/생존/특화)")]
    public PassiveSkillType passiveType = PassiveSkillType.Combat;
    
    [Header("📊 스탯 효과 (참조용)")]
    [Tooltip("인스펙터 확인용. 실제 스탯 타입과 수치는 SkillLevelData.csv의 StatType1/StatType2가 단독 진실 소스입니다.")]
    public List<PassiveStatModifier> statModifiers = new List<PassiveStatModifier>();
    
    [Header("⚡ 특수 효과 (선택)")]
    [Tooltip("발동형 패시브 여부 (예: 피격 시 반격)")]
    public bool isTriggerBased = false;
    
    [Tooltip("트리거 타입 (TriggerBased일 때만)")]
    public PassiveTriggerType triggerType = PassiveTriggerType.None;
    
    [Tooltip("발동 확률 (0~1)")]
    [Range(0f, 1f)]
    public float triggerChance = 0f;
    
    [Tooltip("트리거 이펙트 프리팹 (선택)")]
    public GameObject triggerEffectPrefab;
    
    public override SkillCategory GetSkillCategory() => SkillCategory.Passive;
    
    /// <summary>
    /// SO 기반 폴백 계산 (CSV에 해당 스킬 행이 없을 때만 사용)
    /// 정상 상태에서는 SkillLevelData.csv의 값이 우선됩니다.
    /// </summary>
    public float GetStatValueAtLevel(EStatType statType, int level)
    {
        var modifier = statModifiers.Find(m => m.statType == statType);
        if (modifier == null) return 0f;
        return modifier.baseValue * level;
    }
}

/// <summary>
/// 패시브 스킬 타입 (기획서 3가지 카테고리)
/// </summary>
public enum PassiveSkillType
{
    [Tooltip("전투형: 공격력, 크리티컬, 방어구 관통 등")]
    Combat,
    
    [Tooltip("생존형: 체력, 방어력, 회피율, 회복 등")]
    Survival,
    
    [Tooltip("특화형: 쿨다운 감소, 골드 획득량, 경험치 등")]
    Utility
}

/// <summary>
/// PassiveSkillType을 한글 문자열로 변환하는 확장 메서드
/// </summary>
public static class PassiveSkillTypeExtensions
{
    public static string ToKoreanString(this PassiveSkillType type)
    {
        switch (type)
        {
            case PassiveSkillType.Combat:
                return "전투형";
            case PassiveSkillType.Survival:
                return "생존형";
            case PassiveSkillType.Utility:
                return "보조형";
            default:
                return type.ToString();
        }
    }
}

/// <summary>
/// 패시브 스탯 보정 데이터 (SO 인스펙터 표시용)
/// 실제 런타임 수치는 SkillLevelData.csv가 단독 진실 소스입니다.
/// statType / baseValue 는 CSV 미등록 스킬의 폴백 또는 인스펙터 참조용으로만 사용하세요.
/// </summary>
[System.Serializable]
public class PassiveStatModifier
{
    [Tooltip("영향을 주는 스탯 타입 (CSV의 StatType1/StatType2와 일치시킬 것)")]
    public EStatType statType;
    
    [Tooltip("CSV 미등록 시 폴백 기준값 (레벨 1 기준). 정상 플로우에서는 사용되지 않음")]
    public float baseValue;
    
    [Tooltip("적용 방식 — 현재는 GetModifierType()이 statType 기반으로 자동 결정하므로 참조 전용")]
    public StatModifierType modifierType = StatModifierType.Additive;
}

/// <summary>
/// 스탯 보정 방식
/// </summary>
public enum StatModifierType
{
    Additive,       // 가산 (예: +10)
    Multiplicative  // 배수 (예: +10%)
}

/// <summary>
/// 패시브 트리거 타입 (발동형 패시브용)
/// </summary>
public enum PassiveTriggerType
{
    None,           // 트리거 없음 (영구 효과)
    OnHit,          // 적 타격 시
    OnDamaged,      // 피격 시
    OnKill,         // 적 처치 시
    OnLowHP,        // 체력 낮을 때 (조건부)
    OnCritical      // 크리티컬 발생 시
}
