using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 타입 정의
/// MonsterGrowthProfile에서 타입별 배율 적용에 사용
/// </summary>
public enum EnemyType
{
    Basic,   // 일반 몬스터 (1.0배)
    Elite,   // 엘리트 몬스터 (1.5배)
    Boss     // 보스 몬스터 (3.0배)
}

/// <summary>
/// 공격 타입 정의 (단순화)
/// </summary>
public enum AttackType
{
    Melee,   // 근접 공격
    Ranged   // 원거리 공격 (혼합은 리스트로 처리)
}

/// <summary>
/// 상태이상 타입 정의
/// </summary>
public enum StatusEffectType
{
    None,
    Poison,  // 독: 지속 데미지
    Slow,    // 둔화: 이동속도 감소
    Burn,    // 화상: 화염 지속 데미지
    Stun     // 기절: 일시적 행동 불가
}

/// <summary>
/// ⭐ 새 추가: 복합 공격 발사 패턴 정의
/// Ghost 전용이었던 패턴을 범용으로 확장
/// </summary>
public enum MultiShotPattern
{
    Spread,  // 부채꼴 패턴 (기본)
    Spiral,  // 나선형 패턴
    Random   // 랜덤 패턴
}
