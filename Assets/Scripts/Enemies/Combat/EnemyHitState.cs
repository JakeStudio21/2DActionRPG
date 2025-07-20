using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHitState : IEnemyState
{
    private readonly IEnemy enemy;
    private readonly IEnemyState previousState;
    private float hitStunDuration = 0.3f;
    private float hitTimer = 0f;

    public EnemyHitState(IEnemy enemy, IEnemyState previousState)
    {
        this.enemy = enemy;
        this.previousState = previousState;
    }

    public void Enter()
    {
        enemy.AnimationController?.PlayHit();
        hitTimer = 0f;
        
        // 넉백 및 플래시 효과는 EnemyHealth에서 처리되므로 여기서는 애니메이션과 타이머만 관리
        Debug.Log($"[EnemyHitState] {enemy.transform.name} 피격 상태 진입");
    }

    public void Execute()
    {
        hitTimer += Time.deltaTime;

        // 피격 스턴 시간이 끝나면 이전 상태로 복귀
        if (hitTimer >= hitStunDuration)
        {
            // 체력이 0 이하면 Die 상태로 전환
            EnemyHealth enemyHealth = enemy.transform.GetComponent<EnemyHealth>();
            if (enemyHealth != null && enemyHealth.isDead)
            {
                enemy.FSMController.ChangeState(new EnemyDieState(enemy));
                return;
            }

            // 이전 상태로 복귀 (단, null이면 Idle로)
            if (previousState != null)
            {
                enemy.FSMController.ChangeState(previousState);
            }
            else
            {
                enemy.FSMController.ChangeState(new EnemyIdleState(enemy));
            }
        }
    }

    public void Exit()
    {
        Debug.Log($"[EnemyHitState] {enemy.transform.name} 피격 상태 종료");
    }
}
