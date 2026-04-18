using UnityEngine;
using UnityEngine.AI; // NavMeshAgent (Phase 3)

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
    
    // 경로 갱신 쓰로틀 (NavMesh 전용)
    private float lastPathUpdateTime = -999f;
    private const float PathUpdateInterval = 0.1f; // 초당 10회 경로 재계산

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
            moveSpeed = 2f;
        }
    }

    public void Enter()
    {
        // ❌ 제거: enemy.AnimationController?.PlayWalk();
        // ✅ Walking이 기본 상태이므로 별도 애니메이션 호출 불필요
        
        // ⭐⭐⭐ NavMeshAgent 활성화 (Phase 3 - 핵심 수정!)
        BaseEnemy baseEnemy = enemy as BaseEnemy;
        if (baseEnemy != null && baseEnemy.IsUsingNavMesh)
        {
            baseEnemy.Agent.isStopped = false; // Agent 이동 허용!
            lastPathUpdateTime = -999f;         // Chase 재진입 시 즉시 경로 갱신
            
            // ⭐ 엘리트/일반: 공격 범위 안쪽에서 자동 정지 (물리 밀기 방지)
            // attackCheckRange = 0.8x이므로 stoppingDistance는 반드시 0.8x 미만이어야 Attack 전환 조건 충족
            // 0.7x: 멈춘 지점(0.7x)이 attackCheckRange(0.8x) 안쪽 → 즉시 Attack 전환
            if (!(enemy is Boss_SandElemental))
            {
                baseEnemy.Agent.stoppingDistance = enemy.AttackRange * 0.7f;
            }
            
            if (baseEnemy.EnableDebugLogs)
            {
                Debug.Log($"✅ [EnemyChaseState] {enemy.transform.name} NavMeshAgent 활성화 (isStopped = false, stoppingDistance: {baseEnemy.Agent.stoppingDistance:F2})");
            }
        }
        
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
        
        // ⭐ NavMesh 사용 여부 체크 (Phase 3)
        BaseEnemy baseEnemy = enemy as BaseEnemy;
        bool usingNavMesh = baseEnemy != null && baseEnemy.IsUsingNavMesh;
        
        // 🔍 디버그: NavMesh 사용 여부 로그 (첫 실행 시에만)
        if (baseEnemy != null && baseEnemy.EnableDebugLogs && Time.frameCount % 300 == 0)
        {
            Debug.Log($"🔍 [EnemyChaseState] {enemy.transform.name} NavMesh 사용: {usingNavMesh}");
        }
        
        // ⭐ 보스 전용: 리드 타겟팅 (플레이어 앞쪽 오프셋 지점으로 이동)
        Vector2 targetPosition = enemy.TargetPlayer.transform.position;
        if (enemy is Boss_SandElemental)
        {
            Vector2 toPlayer = targetPosition - (Vector2)enemy.transform.position;
            Vector2 playerDirection = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector2.zero;
            targetPosition = targetPosition + playerDirection * leadOffset; // 플레이어 앞쪽 지점
        }
        
        // ⭐⭐⭐ NavMesh 사용 시 (Phase 3 - 핵심 변경!)
        if (usingNavMesh)
        {
            // ⭐ 경로 갱신 쓰로틀: 0.1초마다만 SetDestination 호출 (매 프레임 재계산 시 속도 저하 방지)
            if (Time.time - lastPathUpdateTime >= PathUpdateInterval)
            {
                baseEnemy.Agent.SetDestination(targetPosition);
                lastPathUpdateTime = Time.time;
            }
            
            // ⭐ 보스 전용: 속도 증가 체크 및 적용
            if (enemy is Boss_SandElemental)
            {
                UpdateBossChaseSpeed();
                baseEnemy.Agent.speed = moveSpeed; // NavMeshAgent 속도 동기화
            }
            
            // ⭐ 8방향 애니메이션 업데이트 (NavMeshAgent velocity 사용)
            Vector2 velocity = new Vector2(baseEnemy.Agent.velocity.x, baseEnemy.Agent.velocity.y);
            baseEnemy.AnimationController?.UpdateMovementByVelocity(velocity);
        }
        // ⭐ 기존 직선 이동 방식 (NavMesh 없을 때)
        else
        {
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
            if (baseEnemy != null)
            {
                baseEnemy.AnimationController?.UpdateMovementByVelocity(velocity);
            }
        }
        
        // ⭐⭐⭐ 거리 체크 (Phase 3 - Vector2.Distance 직접 계산!)
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
                    // Chase 상태 유지, 평타 범위까지 계속 접근
                }
            }
        }
        
        // ⭐ 일반/엘리트 몬스터 공격 전환
        float attackCheckRange = enemy.AttackRange * 0.8f;
        if (!(enemy is Boss_SandElemental) && dist <= attackCheckRange)
        {
            // ⭐ 엘리트 전용: CanAttack() 확인 후 Attack 전환 (보스와 동일 방식)
            // 쿨다운 중에는 Attack 상태에 진입해도 no-op이 되므로 Chase 유지
            if (enemy is BaseEnemy baseEnemyForElite)
            {
                var eliteAttack = baseEnemyForElite.GetComponent<EliteAttackBehaviour>();
                if (eliteAttack != null && !eliteAttack.CanAttack())
                {
                    return; // 쿨다운 해제까지 Chase 유지 (NavMesh stoppingDistance로 정지 상태)
                }
            }
            
            Debug.Log($"[EnemyChaseState] {enemy.transform.name} - 공격 범위 도달! Attack 상태로 전환 (거리: {dist:F2}, 범위: {enemy.AttackRange:F2})");
            enemy.FSMController.ChangeState(new EnemyAttackState(enemy));
        }
        else 
        {
            float chaseRange = (enemy is BaseEnemy be) ? be.ChaseRange : 7f;
            
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
        // ⭐⭐⭐ NavMeshAgent 정지 (Phase 3)
        BaseEnemy baseEnemy = enemy as BaseEnemy;
        if (baseEnemy != null && baseEnemy.IsUsingNavMesh)
        {
            baseEnemy.Agent.isStopped = true; // Agent 정지
            baseEnemy.Agent.stoppingDistance = 0f; // ⭐ stoppingDistance 초기화 (다음 Chase 진입 전 오염 방지)
        }
        
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