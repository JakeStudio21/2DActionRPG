using System;

/// <summary>
/// 룬 시스템 관련 Enum 정의
/// ⚙️ Phase 4-D: 엔드 콘텐츠 확장
/// </summary>

/// <summary>
/// 룬 타입 (8종류)
/// 타입별로 슬롯 제한이나 세트 효과에 사용 가능
/// </summary>
public enum RuneType
{
    /// <summary>
    /// 공격형 1슬롯 (예: 치명타 특화)
    /// </summary>
    Attack1,
    
    /// <summary>
    /// 공격형 2슬롯 (예: 관통 특화)
    /// </summary>
    Attack2,
    
    /// <summary>
    /// 공격형 3슬롯 (예: 속성 특화)
    /// </summary>
    Attack3,
    
    /// <summary>
    /// 생존형 1슬롯 (예: 방어 특화)
    /// </summary>
    Survival1,
    
    /// <summary>
    /// 생존형 2슬롯 (예: 회복 특화)
    /// </summary>
    Survival2,
    
    /// <summary>
    /// 생존형 3슬롯 (예: 면역 특화)
    /// </summary>
    Survival3,
    
    /// <summary>
    /// 유틸형 1슬롯 (예: 이동 특화)
    /// </summary>
    Utility1,
    
    /// <summary>
    /// 유틸형 2슬롯 (예: 쿨다운 특화)
    /// </summary>
    Utility2
}

/// <summary>
/// [Obsolete] RuneRarity는 더 이상 사용하지 않습니다.
/// 기획 변경으로 인해 등급 개념 삭제, RuneType으로 대체
/// </summary>
[Obsolete("RuneRarity is deprecated and will be removed. Use RuneType instead.", true)]
public enum RuneRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

