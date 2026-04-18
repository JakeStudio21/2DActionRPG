using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDieState : IEnemyState
{
    private readonly IEnemy enemy;
    private bool deathProcessed = false;

    public EnemyDieState(IEnemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        enemy.AnimationController?.PlayDie();
        
        // 사망 처리는 한 번만 실행
        if (!deathProcessed)
        {
            deathProcessed = true;
            ProcessDeath();
        }
    }

    public void Execute()
    {
        // 사망 상태에서는 아무것도 하지 않음 (애니메이션 재생 중)
        // 실제 오브젝트 제거는 EnemyHealth.DieRoutine()에서 처리
    }

    public void Exit()
    {
    }

    private void ProcessDeath()
    {
        // 추가 사망 처리 로직이 필요하면 여기에 구현
        // 예: 사운드 재생, 특수 이펙트 등
        
        // 모든 AI 행동 중단
        if (enemy is BaseEnemy baseEnemy && baseEnemy.IsUsingNavMesh)
        {
            // NavMeshAgent 정지
            baseEnemy.Agent.isStopped = true;
            baseEnemy.Agent.ResetPath();
        }
    }
}
