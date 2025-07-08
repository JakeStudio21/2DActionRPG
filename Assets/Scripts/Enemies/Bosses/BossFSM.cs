using UnityEngine;

public class BossFSM : EnemyBase
{
    public BossPattern bossPattern;

    protected override void StateUpdate()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                // 대기
                break;
            case EnemyState.Chase:
                // 추적
                break;
            case EnemyState.Attack:
                // 보스 패턴 실행
                if (bossPattern != null)
                    bossPattern.ExecutePattern();
                break;
            case EnemyState.Die:
                // 사망
                break;
        }
    }
} 