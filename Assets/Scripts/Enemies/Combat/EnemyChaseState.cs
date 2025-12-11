using UnityEngine;

public class EnemyChaseState : IEnemyState
{
    private readonly IEnemy enemy;
    private float moveSpeed; // 필요시 enemy에서 가져오도록 개선 가능

    public EnemyChaseState(IEnemy enemy)
    {
        this.enemy = enemy;
        
        // 🔑 데이터 기반 이동속도 사용 (추격 시에는 1.1배 빠르게)
        moveSpeed = enemy.MoveSpeed * 1.1f; // BaseEnemy의 GetScaledMoveSpeed() * 1.2
        
        // BaseEnemy의 HomePosition 시스템 사용
        if (enemy is BaseEnemy baseEnemy)
        {
            // 🔑 디버그 로그 추가
            if (baseEnemy.EnableDebugLogs)
            {
                Debug.Log($"[EnemyChaseState] {enemy.name} 추격속도: {moveSpeed:F1} (기본속도 * 1.1)");
            }
        }
        else
        {
            // 기존 시스템 fallback
            if (enemy is BlueSlime blueSlime)
            {
                // BlueSlime과 Grape의 경우 실제 스폰 지점 정보도 표시
                // if (enemy is BlueSlime blueSlimeInfo)
                // {
                //     Debug.Log($"  실제 스폰 지점: {blueSlimeInfo.SpawnPoint}, 스폰 거리: {Vector2.Distance(enemy.transform.position, blueSlimeInfo.SpawnPoint):F2}f");
                // }
                // else if (enemy is Grape grapeInfo) // 🔑 Grape 추가
                // {
                //     Debug.Log($"  실제 스폰 지점: {grapeInfo.SpawnPoint}, 스폰 거리: {Vector2.Distance(enemy.transform.position, grapeInfo.SpawnPoint):F2}f");
                // }
                // else if (enemy is Ghost ghostInfo) // 🔑 Ghost 추가
                // {
                //     Debug.Log($"  실제 스폰 지점: {ghostInfo.SpawnPoint}, 스폰 거리: {Vector2.Distance(enemy.transform.position, ghostInfo.SpawnPoint):F2}f");
                // }
                
                // ✅ 수정: Idle 대신 Patrol로 전환하여 스폰 지점으로 돌아감
                // enemy.FSMController.ChangeState(new EnemyPatrolState(enemy));
            }
            else if (enemy is Grape grape) // 🔑 Grape 추가
            {
                // Grape의 경우 실제 스폰 지점 정보도 표시
                // if (enemy is Grape grapeInfo) // 🔑 Grape 추가
                // {
                //     Debug.Log($"  실제 스폰 지점: {grapeInfo.SpawnPoint}, 스폰 거리: {Vector2.Distance(enemy.transform.position, grapeInfo.SpawnPoint):F2}f");
                // }
                // else if (enemy is Ghost ghostInfo) // 🔑 Ghost 추가
                // {
                //     Debug.Log($"  실제 스폰 지점: {ghostInfo.SpawnPoint}, 스폰 거리: {Vector2.Distance(enemy.transform.position, ghostInfo.SpawnPoint):F2}f");
                // }
                
                // ✅ 수정: Idle 대신 Patrol로 전환하여 스폰 지점으로 돌아감
                // enemy.FSMController.ChangeState(new EnemyPatrolState(enemy));
            }
            else if (enemy is Ghost ghost) // 🔑 Ghost 추가
            {
                // Ghost의 경우 실제 스폰 지점 정보도 표시
                // if (enemy is Ghost ghostInfo) // 🔑 Ghost 추가
                // {
                //     Debug.Log($"  실제 스폰 지점: {ghostInfo.SpawnPoint}, 스폰 거리: {Vector2.Distance(enemy.transform.position, ghostInfo.SpawnPoint):F2}f");
                // }
                
                // ✅ 수정: Idle 대신 Patrol로 전환하여 스폰 지점으로 돌아감
                // enemy.FSMController.ChangeState(new EnemyPatrolState(enemy));
            }
            else
            {
                // 기본값 사용
                moveSpeed = 2f; // 기본값
            }
        }
    }

    public void Enter()
    {
        // ❌ 제거: enemy.AnimationController?.PlayWalk();
        // ✅ Walking이 기본 상태이므로 별도 애니메이션 호출 불필요
    }

    public void Execute()
    {
        if (enemy.TargetPlayer == null)
        {
            Debug.Log($"[EnemyChaseState] {enemy.transform.name} - 플레이어 없음, Idle 상태로 전환");
            enemy.FSMController.ChangeState(new EnemyIdleState(enemy));
            return;
        }
        
        Vector2 toPlayer = (enemy.TargetPlayer.transform.position - enemy.transform.position);
        Vector2 dir = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.zero;
        Vector2 velocity = dir * moveSpeed;
        enemy.transform.position += (Vector3)(velocity * Time.deltaTime);
        
        // ⭐ 8방향 애니메이션 업데이트 (실제 속도 기반)
        if (enemy is BaseEnemy baseEnemy)
        {
            baseEnemy.AnimationController?.UpdateMovementByVelocity(velocity);
        }
        
        float dist = Vector2.Distance(enemy.transform.position, enemy.TargetPlayer.transform.position);
        
        // ⭐ 보스는 원거리 스킬을 사용하므로 더 넓은 범위에서 공격 시도
        float attackCheckRange = enemy.AttackRange * 0.8f;
        
        if (enemy is Boss_SandElemental)
        {
            // 보스는 원거리 스킬 최대 범위(10f) 내에서 공격 가능
            attackCheckRange = 10f; // 원거리 스킬 사용 가능 범위
            
            if (dist <= attackCheckRange)
            {
                Debug.Log($"[EnemyChaseState] {enemy.transform.name} (BOSS) - 스킬 사용 범위 도달! Attack 상태로 전환 (거리: {dist:F2})");
                enemy.FSMController.ChangeState(new EnemyAttackState(enemy));
            }
        }
        else if (dist <= attackCheckRange) // 일반 몬스터는 기존 로직
        {
            Debug.Log($"[EnemyChaseState] {enemy.transform.name} - 공격 범위 도달! Attack 상태로 전환 (거리: {dist:F2}, 범위: {enemy.AttackRange:F2})");
            enemy.FSMController.ChangeState(new EnemyAttackState(enemy));
        }
        else 
        {
            // 🔑 BlueSlime과 Grape의 경우 각각의 ChaseRange 사용, 다른 몬스터는 기본값 사용
            float chaseRange = 7f; // 기본값
            if (enemy is BlueSlime blueSlime)
            {
                chaseRange = blueSlime.ChaseRange;
            }
            else if (enemy is Grape grape) // 🔑 Grape 추가
            {
                chaseRange = grape.ChaseRange;
            }
            else if (enemy is Ghost ghost) // 🔑 Ghost 추가
            {
                chaseRange = ghost.ChaseRange;
            }
            
            // Debug.Log($"[EnemyChaseState] {enemy.transform.name} - 플레이어 거리: {dist:F2}, 최종 추격범위: {chaseRange:F2}");
            
            if (dist > chaseRange)
            {
                // Debug.Log($"[EnemyChaseState] {enemy.transform.name} - 추격 범위 초과, 스폰 지점으로 복귀");
                // Debug.Log($"  현재 위치: {enemy.transform.position}, 플레이어 거리: {dist:F2}f, 추격범위: {chaseRange:F2}f");
                
                // // BlueSlime과 Grape의 경우 실제 스폰 지점 정보도 표시
                // if (enemy is BlueSlime blueSlimeInfo)
                // {
                //     Debug.Log($"  실제 스폰 지점: {blueSlimeInfo.SpawnPoint}, 스폰 거리: {Vector2.Distance(enemy.transform.position, blueSlimeInfo.SpawnPoint):F2}f");
                // }
                // else if (enemy is Grape grapeInfo) // 🔑 Grape 추가
                // {
                //     Debug.Log($"  실제 스폰 지점: {grapeInfo.SpawnPoint}, 스폰 거리: {Vector2.Distance(enemy.transform.position, grapeInfo.SpawnPoint):F2}f");
                // }
                // else if (enemy is Ghost ghostInfo) // 🔑 Ghost 추가
                // {
                //     Debug.Log($"  실제 스폰 지점: {ghostInfo.SpawnPoint}, 스폰 거리: {Vector2.Distance(enemy.transform.position, ghostInfo.SpawnPoint):F2}f");
                // }
                
                // ✅ 수정: Idle 대신 Patrol로 전환하여 스폰 지점으로 돌아감
                enemy.FSMController.ChangeState(new EnemyPatrolState(enemy));
            }
        }
    }

    public void Exit() { }
} 