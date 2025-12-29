using UnityEngine;

public class EnemyChaseState : IEnemyState
{
    private readonly IEnemy enemy;
    private float moveSpeed; // 필요시 enemy에서 가져오도록 개선 가능
    private float baseMoveSpeed; // 기본 이동 속도 저장
    
    // ⭐ 보스 전용 추격 시스템
    private float chaseStartTime = 0f;           // 추격 시작 시간
    private float chaseTimeoutDuration = 2f;     // 타임아웃 지속시간 (2초)
    private float speedBoostMultiplier = 1.25f;  // 속도 증가 배율 (25%)
    private float speedBoostDuration = 3f;       // 속도 증가 지속시간 (3초)
    private float speedBoostEndTime = -999f;     // 속도 증가 종료 시간
    private bool isSpeedBoostActive = false;     // 속도 증가 활성화 여부
    
    // 히스테리시스
    private float chaseStartHysteresis = 1.5f;   // 추격 시작 여유값 (AttackRange + 1.5f)
    private float chaseEndHysteresis = 0.5f;     // 추격 종료 여유값 (AttackRange - 0.5f)
    private bool isChasing = false;              // 추격 상태 플래그
    
    // 리드 타겟팅
    private float leadOffset = 1.5f;             // 플레이어 앞쪽 오프셋 거리

    public EnemyChaseState(IEnemy enemy)
    {
        this.enemy = enemy;
        
        // 🔑 데이터 기반 이동속도 사용 (추격 시에는 1.1배 빠르게)
        baseMoveSpeed = enemy.MoveSpeed;
        moveSpeed = baseMoveSpeed * 1.1f; // BaseEnemy의 GetScaledMoveSpeed() * 1.1
        
        // 보스 전용 추격 시스템 초기화
        if (enemy is Boss_SandElemental)
        {
            isChasing = false;
            isSpeedBoostActive = false;
            speedBoostEndTime = -999f;
        }
        
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
        
        // ⭐ 보스 전용: 추격 시작 시간 기록
        if (enemy is Boss_SandElemental)
        {
            chaseStartTime = Time.time;
            isSpeedBoostActive = false;
            speedBoostEndTime = -999f;
        }
    }

    public void Execute()
    {
        if (enemy.TargetPlayer == null)
        {
            Debug.Log($"[EnemyChaseState] {enemy.transform.name} - 플레이어 없음, Idle 상태로 전환");
            enemy.FSMController.ChangeState(new EnemyIdleState(enemy));
            return;
        }
        
        // ⭐ 보스 전용: 리드 타겟팅 (플레이어 앞쪽 오프셋 지점으로 이동)
        Vector2 targetPosition = enemy.TargetPlayer.transform.position;
        if (enemy is Boss_SandElemental)
        {
            Vector2 toPlayer = targetPosition - (Vector2)enemy.transform.position;
            Vector2 playerDirection = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.zero;
            targetPosition = targetPosition + playerDirection * leadOffset; // 플레이어 앞쪽 지점
        }
        
        Vector2 toTarget = targetPosition - (Vector2)enemy.transform.position;
        Vector2 dir = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.zero;
        
        // ⭐ 보스 전용: 속도 증가 체크 및 적용
        if (enemy is Boss_SandElemental)
        {
            UpdateBossChaseSpeed();
        }
        
        Vector2 velocity = dir * moveSpeed;
        enemy.transform.position += (Vector3)(velocity * Time.deltaTime);
        
        // ⭐ 8방향 애니메이션 업데이트 (실제 속도 기반)
        if (enemy is BaseEnemy baseEnemy)
        {
            baseEnemy.AnimationController?.UpdateMovementByVelocity(velocity);
        }
        
        float dist = Vector2.Distance(enemy.transform.position, enemy.TargetPlayer.transform.position);
        
        // ⭐ 보스 전용: 히스테리시스 적용 공격 범위 체크
        if (enemy is Boss_SandElemental boss)
        {
            float attackRange = boss.AttackRange;
            float chaseEndRange = attackRange - chaseEndHysteresis; // 추격 종료 거리
            float chaseStartRange = attackRange + chaseStartHysteresis; // 추격 시작 거리
            float rangedSkillRange = 10f; // 원거리 스킬 사용 가능 범위
            
            // 평타 범위 내 진입 → Attack 상태 (평타 7:3 비율 적용)
            if (!isChasing && dist <= chaseEndRange)
            {
                isChasing = false;
                isSpeedBoostActive = false; // 속도 증가 해제
                moveSpeed = baseMoveSpeed * 1.1f;
                Debug.Log($"[EnemyChaseState] {enemy.transform.name} (BOSS) - 평타 범위 도달! Attack 상태로 전환 (거리: {dist:F2}, 범위: {attackRange:F2})");
                enemy.FSMController.ChangeState(new EnemyAttackState(enemy));
                return;
            }
            
            // 추격 중 범위 내 진입 → Attack 상태
            if (isChasing && dist <= chaseEndRange)
            {
                isChasing = false;
                isSpeedBoostActive = false; // 속도 증가 해제
                moveSpeed = baseMoveSpeed * 1.1f;
                Debug.Log($"[EnemyChaseState] {enemy.transform.name} (BOSS) - 추격 중 범위 도달! Attack 상태로 전환 (거리: {dist:F2})");
                enemy.FSMController.ChangeState(new EnemyAttackState(enemy));
                return;
            }
            
            // 추격 시작 조건 (AttackRange + 여유값)
            if (!isChasing && dist > chaseStartRange)
            {
                isChasing = true;
                chaseStartTime = Time.time; // 추격 시작 시간 재설정
            }
            
            // 원거리 스킬 범위 내 → Attack 상태 (스킬 사용 가능할 때만)
            if (dist <= rangedSkillRange)
            {
                // ⭐ 스킬 사용 가능할 때만 Attack 상태로 전환 (빈 공격 방지)
                var bossAttack = boss.GetComponent<BossAttackBehaviour>();
                if (bossAttack != null && bossAttack.CanAttack())
                {
                    Debug.Log($"[EnemyChaseState] {enemy.transform.name} (BOSS) - 원거리 스킬 범위 도달! Attack 상태로 전환 (거리: {dist:F2})");
                    enemy.FSMController.ChangeState(new EnemyAttackState(enemy));
                    return;
                }
                else
                {
                    Debug.Log($"[EnemyChaseState] {enemy.transform.name} (BOSS) - 원거리 스킬 범위 내지만 공격 불가 (쿨다운 중), 계속 접근 중... (거리: {dist:F2})");
                    // Chase 상태 유지, 평타 범위까지 계속 접근
                }
            }
        }
        
        // ⭐ 일반 몬스터는 기존 로직
        float attackCheckRange = enemy.AttackRange * 0.8f;
        if (!(enemy is Boss_SandElemental) && dist <= attackCheckRange) // 일반 몬스터는 기존 로직
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

    public void Exit() 
    {
        // ⭐ 보스 전용: 추격 종료 시 상태 초기화
        if (enemy is Boss_SandElemental)
        {
            isSpeedBoostActive = false;
            moveSpeed = baseMoveSpeed * 1.1f;
        }
    }
    
    /// <summary>
    /// ⭐ 보스 전용: 추격 타임아웃 및 속도 증가 체크
    /// </summary>
    private void UpdateBossChaseSpeed()
    {
        float currentTime = Time.time;
        float dist = Vector2.Distance(enemy.transform.position, enemy.TargetPlayer.transform.position);
        
        // 속도 증가 해제 조건: 평타 범위 내 진입 또는 시간 초과
        if (isSpeedBoostActive)
        {
            if (dist <= enemy.AttackRange || currentTime >= speedBoostEndTime)
            {
                isSpeedBoostActive = false;
                moveSpeed = baseMoveSpeed * 1.1f;
                if (dist <= enemy.AttackRange)
                {
                    Debug.Log($"[EnemyChaseState] {enemy.transform.name} (BOSS) - 평타 범위 진입, 속도 증가 해제");
                }
            }
        }
        
        // 추격 타임아웃 체크: 2초 동안 평타 범위 밖에 있으면 속도 증가
        if (!isSpeedBoostActive && isChasing)
        {
            float chaseDuration = currentTime - chaseStartTime;
            if (chaseDuration >= chaseTimeoutDuration && dist > enemy.AttackRange)
            {
                isSpeedBoostActive = true;
                speedBoostEndTime = currentTime + speedBoostDuration;
                moveSpeed = baseMoveSpeed * 1.1f * speedBoostMultiplier;
                Debug.Log($"[EnemyChaseState] {enemy.transform.name} (BOSS) - 추격 타임아웃! 속도 증가 활성화 ({moveSpeed:F2}, 지속: {speedBoostDuration}초)");
            }
        }
    }
} 