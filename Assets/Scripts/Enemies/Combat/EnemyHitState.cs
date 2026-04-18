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
        
        
        // ⭐⭐⭐ NavMeshAgent 완전 정지 (핵심!)
        BaseEnemy baseEnemy = enemy as BaseEnemy;
        if (baseEnemy != null && baseEnemy.IsUsingNavMesh)
        {
            // 경로 제거 + 정지 (Chase 상태의 SetDestination 무시)
            baseEnemy.Agent.ResetPath();
            baseEnemy.Agent.isStopped = true;
            
            
            // 플레이어 위치 가져오기
            PlayerController player = Object.FindObjectOfType<PlayerController>();
            if (player != null)
            {
                // 연출 넉백 코루틴 시작
                baseEnemy.StartCoroutine(baseEnemy.PerformKnockbackEffect(player.transform.position));
            }
        }
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
        
        // ⭐⭐⭐ NavMeshAgent 재개 (핵심!)
        BaseEnemy baseEnemy = enemy as BaseEnemy;
        if (baseEnemy != null && baseEnemy.IsUsingNavMesh)
        {
            baseEnemy.Agent.isStopped = false;
        }
    }
}
