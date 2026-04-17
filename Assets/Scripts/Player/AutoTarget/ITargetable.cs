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
/// 타겟팅 우선순위 랭크.
/// 실제 점수는 TargetingProfile의 보너스 값으로 결정됩니다.
///   Obstacle      : Normal보다 낮음  (일반 바리케이드·구르는 바위)
///   Normal        : 기본 몬스터
///   MissionObject : Normal 초과 Elite 미만  (isVictoryTarget 바리케이드)
///   Elite / MiniBoss / Boss : 전투 적 고등급
/// </summary>
public enum EnemyRank
{
    Normal        = 0,
    Elite         = 1,
    MiniBoss      = 2,
    Boss          = 3,
    MissionObject = 4,  // 미션 목표 오브젝트 (Normal < score < Elite)
    Obstacle      = 5,  // 일반 파괴 오브젝트 (score < Normal)
}
