using System;

/// <summary>
/// 조건부 모디파이어의 조건 타입 정의
/// ConditionalModifier.csv의 ConditionType 컬럼과 1:1 매핑
/// </summary>
public enum EConditionType
{
    None = 0,
    
    // === 타겟 관련 ===
    /// <summary>
    /// 대상이 보스 몬스터인지 확인
    /// </summary>
    TargetIsBoss,
    
    /// <summary>
    /// 대상이 엘리트 몬스터인지 확인
    /// </summary>
    TargetIsElite,
    
    /// <summary>
    /// 대상이 보스이면서 특정 데미지 타입인지 확인
    /// ConditionParam: "AREA_GROUND", "PROJECTILE" 등
    /// </summary>
    TargetIsBossAndDamageType,
    
    /// <summary>
    /// 대상이 보스이면서 HP가 특정 비율 이상인지 확인 (복합 조건)
    /// ConditionParam: 0.5 = 보스 HP 50% 이상
    /// </summary>
    TargetIsBossAndHpAbove,
    
    /// <summary>
    /// 대상의 HP가 특정 비율 이상인지 확인
    /// ConditionParam: 0.5 = 50% 이상
    /// </summary>
    TargetHpAbove,
    
    /// <summary>
    /// 대상의 HP가 특정 비율 이하인지 확인
    /// ConditionParam: 0.3 = 30% 이하
    /// </summary>
    TargetHpBelow,
    
    // === 자신 관련 ===
    /// <summary>
    /// 자신의 HP가 특정 비율 이하인지 확인
    /// ConditionParam: 0.3 = 30% 이하
    /// </summary>
    SelfHpBelow,
    
    /// <summary>
    /// 자신의 HP가 특정 비율 이상인지 확인
    /// ConditionParam: 0.5 = 50% 이상
    /// </summary>
    SelfHpAbove,
    
    // === 환경/상태 관련 (향후 확장) ===
    /// <summary>
    /// 백어택 조건
    /// </summary>
    IsBackAttack,
    
    /// <summary>
    /// 특정 버프 활성화 중
    /// ConditionParam: "Berserker", "Stealth" 등
    /// </summary>
    HasBuff,
    
    /// <summary>
    /// 특정 디버프 활성화 중
    /// ConditionParam: "Poison", "Burn" 등
    /// </summary>
    HasDebuff,
}

