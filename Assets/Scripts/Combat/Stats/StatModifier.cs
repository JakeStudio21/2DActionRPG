using System;
using UnityEngine;

/// <summary>
/// 스탯 모디파이어 (장비, 버프, 룬 등에서 제공)
/// 최적화: Enum 기반 비교, ApplyPhase 정렬
/// </summary>
[Serializable]
public class StatModifier
{
    #region 핵심 데이터
    
    /// <summary>
    /// 스탯 타입 (Enum - 최적화)
    /// </summary>
    public EStatType statType;
    
    /// <summary>
    /// 값 타입
    /// </summary>
    public StatValueType valueType;
    
    /// <summary>
    /// 단위 (Flat/Percent)
    /// </summary>
    public StatUnit unit;
    
    /// <summary>
    /// 실제 값
    /// </summary>
    public float value;
    
    #endregion
    
    #region 메타데이터
    
    /// <summary>
    /// 출처 (디버깅용)
    /// </summary>
    public string source;
    
    /// <summary>
    /// 적용 순서 (ApplyPhase)
    /// </summary>
    public int applyPhase;
    
    /// <summary>
    /// 합산 규칙
    /// </summary>
    public StatStackRule stackRule;
    
    /// <summary>
    /// 상한값 (있으면)
    /// </summary>
    public float? capValue;
    
    #endregion
    
    #region 생성자
    
    public StatModifier()
    {
    }
    
    public StatModifier(EStatType statType, float value, StatUnit unit, string source = "")
    {
        this.statType = statType;
        this.value = value;
        this.unit = unit;
        this.source = source;
        
        // StatDefinitions에서 자동 로드
        var definition = StatDefinitions.Get(statType);
        if (definition != null)
        {
            this.valueType = definition.valueType;
            this.stackRule = definition.stackRule;
            this.applyPhase = definition.applyPhase;
            this.capValue = definition.capValue;
        }
    }
    
    #endregion
    
    #region 헬퍼 메서드
    
    /// <summary>
    /// 디버그 정보
    /// </summary>
    public override string ToString()
    {
        string unitStr = unit == StatUnit.Percent ? "%" : "";
        return $"[{statType}] +{value}{unitStr} (Phase{applyPhase}, {source})";
    }
    
    #endregion
}

/// <summary>
/// 스탯 값 타입
/// </summary>
public enum StatValueType
{
    Float,
    Bool
}

/// <summary>
/// 스탯 단위
/// </summary>
public enum StatUnit
{
    Flat,       // 고정값 (예: +50 공격력)
    Percent,    // 백분율 (소수 형태, 0.01 = 1%, CSV에서 1 → 코드에서 0.01로 자동 변환)
    Bool        // 불린 (예: 면역)
}

/// <summary>
/// 스탯 합산 규칙
/// </summary>
public enum StatStackRule
{
    Add,              // 단순 합산: A + B + C
    AddThenMultiply,  // 합산 후 곱셈: (1 + A + B + C)
    AddThenCap        // 합산 후 상한 적용: Min(A + B + C, Cap)
}

