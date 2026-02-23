using System;

/// <summary>
/// 조건부 모디파이어의 효과 타입 정의
/// ConditionalModifier.csv의 EffectType 컬럼과 1:1 매핑
/// </summary>
public enum EEffectType
{
    None = 0,
    
    // === 피해 증가/감소 (Phase3_Modifier) ===
    /// <summary>
    /// 주는 피해 배율 증가
    /// Value: 0.2 = +20%
    /// </summary>
    DamageDealtMult,
    
    /// <summary>
    /// 받는 피해 감소
    /// Value: 0.25 = -25%
    /// </summary>
    DamageTakenReduce,
    
    // === 방어 관련 (Phase5_Defense) ===
    /// <summary>
    /// 대상 방어력 무시 (%)
    /// Value: 0.2 = 20% 방어 무시
    /// </summary>
    IgnoreDefPercent,
    
    // === 상태이상 면역 (Phase0_Event) ===
    /// <summary>
    /// 속박 면역
    /// Value: 1 = 면역, 0 = 비면역
    /// </summary>
    Immunity_Bind,
    
    /// <summary>
    /// 슬로우 면역
    /// </summary>
    Immunity_Slow,
    
    /// <summary>
    /// 독 면역
    /// </summary>
    Immunity_Poison,
    
    /// <summary>
    /// 화상 면역
    /// </summary>
    Immunity_Burn,
    
    // === 회복/흡혈 (Phase7_Post) ===
    /// <summary>
    /// 회복 차단 (%)
    /// Value: 0.5 = 50% 회복 차단
    /// </summary>
    BlockHealingPercent,
    
    /// <summary>
    /// DOT 피해 기반 초당 흡혈 (%)
    /// Value: 0.02 = 피해의 2%만큼 초당 회복
    /// </summary>
    LifeStealFromDamagePerSec,
    
    /// <summary>
    /// 즉시 흡혈 (%)
    /// Value: 0.1 = 피해의 10%만큼 즉시 회복
    /// </summary>
    LifeStealImmediate,
    
    // === 크리티컬 관련 (Phase4_Crit) ===
    /// <summary>
    /// 크리티컬 피해 배율 추가 증가
    /// Value: 0.3 = +30%
    /// </summary>
    CritDamageMult,
    
    /// <summary>
    /// 크리티컬 확률 추가 증가
    /// Value: 0.1 = +10%
    /// </summary>
    CritRateBonus,
    
    // === 향후 확장 ===
    /// <summary>
    /// 스킬 쿨다운 감소
    /// Value: 0.2 = -20% 쿨타임
    /// </summary>
    CooldownReduce,
    
    /// <summary>
    /// 이동속도 증가
    /// Value: 0.15 = +15% 이동속도
    /// </summary>
    MoveSpeedBonus,
}

