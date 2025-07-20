using UnityEngine;

/// <summary>
/// 몬스터 공격 방식을 정의하는 인터페이스
/// Strategy Pattern을 활용하여 근접/원거리 공격을 모듈화
/// </summary>
public interface IAttackBehaviour
{
    /// <summary>
    /// 공격 가능 여부 확인
    /// </summary>
    /// <returns>공격 가능하면 true</returns>
    bool CanAttack();
    
    /// <summary>
    /// 공격 중 이동 정지 여부
    /// </summary>
    /// <returns>공격 중 이동을 정지해야 하면 true</returns>
    bool ShouldStopMovingWhileAttacking();
} 