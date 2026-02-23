using System;
using UnityEngine;

/// <summary>
/// 조건부 모디파이어 클래스
/// ConditionalModifier.csv의 데이터를 담는 런타임 클래스
/// ⚡ StatModifier와 유사하지만 조건(Condition)과 효과(Effect)가 추가됨
/// ⚙️ Phase 4: ConditionalModifier 시스템
/// </summary>
[Serializable]
public class ConditionalModifier
{
    #region 기본 정보
    
    /// <summary>
    /// 모디파이어 고유 ID (CSV의 ModifierId)
    /// </summary>
    public string modifierId;
    
    /// <summary>
    /// UI 표시용 이름
    /// </summary>
    public string displayName;
    
    #endregion
    
    #region 값 타입
    
    /// <summary>
    /// 값의 타입 (Float, Bool)
    /// </summary>
    public StatValueType valueType;
    
    /// <summary>
    /// 값의 단위 (Flat, Percent, Bool)
    /// </summary>
    public StatUnit unit;
    
    /// <summary>
    /// 모디파이어 값
    /// 예: DamageDealtMult = 0.2 → +20% 피해
    /// </summary>
    public float value;
    
    #endregion
    
    #region 조건 정보
    
    /// <summary>
    /// 조건 타입 (TargetIsBoss, SelfHpBelow 등)
    /// </summary>
    public EConditionType conditionType;
    
    /// <summary>
    /// 조건 파라미터 (HP 비율, 데미지 타입 등)
    /// 예: SelfHpBelow의 경우 0.3 = 30%
    /// 예: TargetIsBossAndDamageType의 경우 "AREA_GROUND"
    /// </summary>
    public string conditionParam;
    
    #endregion
    
    #region 효과 정보
    
    /// <summary>
    /// 효과 타입 (DamageDealtMult, Immunity_Bind 등)
    /// </summary>
    public EEffectType effectType;
    
    /// <summary>
    /// 적용 단계 (Phase3_Modifier, Phase5_Defense 등)
    /// 예: 3 = Phase3, 5 = Phase5, 99 = Runtime
    /// </summary>
    public int applyPhase;
    
    /// <summary>
    /// 적용 대상 (Self, Target)
    /// </summary>
    public string targetScope;
    
    #endregion
    
    #region 메타데이터
    
    /// <summary>
    /// 설명/노트
    /// </summary>
    public string notes;
    
    /// <summary>
    /// 출처 (어떤 장비/룬에서 왔는지)
    /// 예: "Rune_Boss_Hunter", "Accessory_Ring_S"
    /// </summary>
    [NonSerialized]
    public string source;
    
    #endregion
    
    #region 생성자
    
    /// <summary>
    /// 기본 생성자
    /// </summary>
    public ConditionalModifier()
    {
    }
    
    /// <summary>
    /// CSV 파싱용 생성자
    /// </summary>
    public ConditionalModifier(
        string modifierId,
        string displayName,
        StatValueType valueType,
        StatUnit unit,
        EConditionType conditionType,
        string conditionParam,
        EEffectType effectType,
        float value,
        int applyPhase,
        string targetScope,
        string notes,
        string source = ""
    )
    {
        this.modifierId = modifierId;
        this.displayName = displayName;
        this.valueType = valueType;
        this.unit = unit;
        this.conditionType = conditionType;
        this.conditionParam = conditionParam;
        this.effectType = effectType;
        this.value = value;
        this.applyPhase = applyPhase;
        this.targetScope = targetScope;
        this.notes = notes;
        this.source = source;
    }
    
    #endregion
    
    #region 유틸리티 메서드
    
    /// <summary>
    /// 조건이 만족되는지 확인
    /// </summary>
    public bool IsConditionMet(CombatContext context)
    {
        // ConditionParam을 float로 파싱 시도
        float paramValue = 0f;
        if (!string.IsNullOrEmpty(conditionParam))
        {
            float.TryParse(conditionParam, out paramValue);
        }
        
        // applyPhase를 전달하여 페이즈 기반 스마트 스위칭 활성화
        return ConditionChecker.Check(conditionType, paramValue, conditionParam, context, applyPhase);
    }
    
    /// <summary>
    /// 디버그 출력
    /// </summary>
    public override string ToString()
    {
        return $"[{modifierId}] {displayName} | Condition: {conditionType}({conditionParam}) → Effect: {effectType}({value})";
    }
    
    #endregion
}

