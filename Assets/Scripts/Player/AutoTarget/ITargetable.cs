using UnityEngine;

/// <summary>
/// 자동 타겟팅 시스템이 인식할 수 있는 대상 인터페이스.
/// BaseEnemy와 SimpleMob 양쪽에 구현됩니다.
/// </summary>
public interface ITargetable
{
    bool IsAlive();
    EnemyRank GetRank();
    Transform GetTransform();
    void ActivateLockOn();
    void DeactivateLockOn();
}

/// <summary>
/// 타겟팅 우선순위 랭크. 값이 클수록 보너스 점수가 높습니다.
/// </summary>
public enum EnemyRank
{
    Normal   = 0,
    Elite    = 1,
    MiniBoss = 2,
    Boss     = 3,
}
