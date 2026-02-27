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
    
    [Header("📊 스탯 효과")]
    [Tooltip("이 패시브가 적용하는 스탯 보너스 목록")]
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
    /// 특정 레벨에서의 스탯 보너스 계산
    /// </summary>
    public float GetStatValueAtLevel(EStatType statType, int level)
    {
        var modifier = statModifiers.Find(m => m.statType == statType);
        if (modifier == null) return 0f;
        
        // 임시: 레벨당 선형 증가
        // Phase 2에서 CSV 곡선으로 교체 예정
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
/// 패시브 스탯 보정 데이터
/// 기존 EStatType (Combat/Stats/EStatType.cs) 사용
/// </summary>
[System.Serializable]
public class PassiveStatModifier
{
    [Tooltip("영향을 주는 스탯 타입 (ATK_FLAT, CRIT_RATE 등)")]
    public EStatType statType;
    
    [Tooltip("레벨 1 기준 기본 증가량")]
    public float baseValue;
    
    [Tooltip("적용 방식 (가산/배수)")]
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
