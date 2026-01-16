using UnityEngine;
using UnityEngine.AI; // NavMeshAgent (Phase 3)

public class EnemyIdleState : IEnemyState
{
    private readonly IEnemy enemy;
    private float idleTimer = 0f;
    // 📍 7번 라인 - 사용하지 않는 필드 제거 또는 주석처리
    // private float maxIdleTime = 3f; // 사용하지 않으므로 주석처리

    public EnemyIdleState(IEnemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        // ❌ 제거: enemy.AnimationController?.PlayIdle();
        // ✅ Walking이 기본 상태이므로 별도 애니메이션 호출 불필요
        idleTimer = 0f;
        
        // ⭐ 8방향 애니메이션: Idle 강제 적용 (speed=0, isMoving=false)
        if (enemy is BaseEnemy baseEnemy)
        {
            baseEnemy.AnimationController?.ForceIdle();
            
            // ⭐⭐⭐ NavMeshAgent 정지 (Phase 3)
            if (baseEnemy.IsUsingNavMesh)
            {
                baseEnemy.Agent.isStopped = true;
                baseEnemy.Agent.ResetPath();
            }
        }
    }

    public void Execute()
    {
        // ⭐ 8방향 애니메이션: 매 프레임 Idle 유지 (안전)
        if (enemy is BaseEnemy baseEnemy)
        {
            baseEnemy.AnimationController?.ForceIdle();
        }
        
        // 플레이어 감지
        if (enemy.IsPlayerInRange(enemy.DetectionRange))
        {
            enemy.ChangeState(new EnemyChaseState(enemy));
            return;
        }
        
        // 홈 위치에서 너무 멀어졌으면 돌아가기
        if (!enemy.IsWithinPatrolRange())
        {
            enemy.ChangeState(new EnemyReturnToHomeState(enemy));
            return;
        }
        
        // 랜덤하게 순찰 시작
        idleTimer += Time.deltaTime;
        if (idleTimer >= Random.Range(2f, 5f))
        {
            enemy.ChangeState(new EnemyPatrolState(enemy));
        }
    }

    public void Exit() 
    {
        // ⭐⭐⭐ NavMeshAgent 재개 (Phase 3)
        if (enemy is BaseEnemy baseEnemy && baseEnemy.IsUsingNavMesh)
        {
            baseEnemy.Agent.isStopped = false;
        }
    }
} 