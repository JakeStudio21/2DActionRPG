using UnityEngine;

public class EnemyAttackState : IEnemyState
{
    private readonly IEnemy enemy;
    private float attackTimer = 0f;
    private float attackDuration = 1.5f; // 공격 애니메이션 시간
    private float postAttackDelay = 0.5f; // ⭐ 공격 후 대기 시간 추가
    
    // ⭐ 연속 미스 카운트 (안전장치)
    private int consecutiveMissCount = 0;
    private bool hasCheckedAfterAnimation = false; // 공격 애니메이션 완료 후 거리 체크 여부

    public EnemyAttackState(IEnemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        Debug.Log($"🗡️ [EnemyAttackState] {enemy.transform.name} - 공격 상태 진입!");
        
        // ⭐ 1번: 공격 시작 전 거리 체크
        if (enemy.TargetPlayer != null)
        {
            float dist = Vector2.Distance(enemy.transform.position, enemy.TargetPlayer.transform.position);
            
            // ⭐ 엘리트/보스는 실제 공격 범위로 엄격하게 체크 (빈 공격 방지)
            bool isEliteOrBoss = false;
            float rangeThreshold = enemy.AttackRange * 1.2f; // 기본: 1.2배 여유
            
            if (enemy is BaseEnemy baseEnemyEnter)
            {
                var eliteAttack = baseEnemyEnter.GetComponent<EliteAttackBehaviour>();
                var bossAttack = baseEnemyEnter.GetComponent<BossAttackBehaviour>();
                
                if (eliteAttack != null || bossAttack != null)
                {
                    isEliteOrBoss = true;
                    rangeThreshold = enemy.AttackRange; // 엘리트/보스: 실제 범위로 엄격하게
                }
            }
            
            // 공격 범위 밖이면 추격 상태로 전환
            if (dist > rangeThreshold)
            {
                string monsterType = isEliteOrBoss ? "(ELITE/BOSS)" : "";
                Debug.Log($"[EnemyAttackState] {enemy.transform.name} {monsterType} - 공격 시작 시 거리 밖 감지! Chase 상태로 전환 (거리: {dist:F2}, 범위: {rangeThreshold:F2})");
                enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                return;
            }
        }
        
        // 🔑 공격 시도 (CanAttack 체크는 각 Attack 컴포넌트에서 처리)
        enemy.Attack();
        attackTimer = 0f;
        hasCheckedAfterAnimation = false;
        
        // ⭐ 방향 확인 로그
        if (enemy is BaseEnemy baseEnemy && baseEnemy.AnimationController != null)
        {
            var animController = baseEnemy.AnimationController;
            var animator = animController.GetComponent<Animator>();
            if (animator != null)
            {
                float moveX = animator.GetFloat("moveX");
                float moveY = animator.GetFloat("moveY");
                Debug.Log($"   📍 공격 시작 직후 방향: moveX={moveX:F2}, moveY={moveY:F2}");
            }
        }
    }

    public void Execute()
    {
        attackTimer += Time.deltaTime;
        
        // ⭐ 공격 중에는 방향을 유지하면서 Idle 상태만 적용
        if (enemy is BaseEnemy baseEnemy && baseEnemy.AnimationController != null)
        {
            baseEnemy.AnimationController.ForceIdleKeepDirection();
        }
        
        // ⭐ 2번: 공격 애니메이션 완료 시점(attackDuration) 거리 체크
        if (!hasCheckedAfterAnimation && attackTimer >= attackDuration)
        {
            hasCheckedAfterAnimation = true;
            
            // ⭐ 스킬 시전 중이면 거리 체크 스킵 (엘리트/보스 전용)
            if (enemy is BaseEnemy baseEnemyCheck && baseEnemyCheck.IsPerformingSkill)
            {
                Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 스킬 시전 중! 거리 체크 스킵 (완료까지 대기)");
                return; // 스킬 완료까지 대기
            }
            
            // ⭐ 3번: 연속 미스 체크 (안전장치)
            if (CheckConsecutiveMiss())
            {
                Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 연속 미스 2회 감지! Chase 상태로 전환");
                consecutiveMissCount = 0; // 리셋
                enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                return;
            }
            
            // 거리 체크
            if (enemy.TargetPlayer != null)
            {
                float dist = Vector2.Distance(enemy.transform.position, enemy.TargetPlayer.transform.position);
                
                // 공격 범위 밖이면 즉시 추격 상태로 전환 (postAttackDelay 대기 안 함)
                if (enemy is Boss_SandElemental)
                {
                    float attackRange = enemy.AttackRange;
                    float chaseEndRange = attackRange - 0.5f;
                    float chaseStartRange = attackRange + 1.5f;
                    
                    if (dist > chaseStartRange)
                    {
                        Debug.Log($"[EnemyAttackState] {enemy.transform.name} (BOSS) - 공격 완료 후 거리 밖! Chase 상태로 전환 (거리: {dist:F2})");
                        enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                        return;
                    }
                }
                else
                {
                    if (dist > enemy.AttackRange * 1.2f)
                    {
                        Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 공격 완료 후 거리 밖! Chase 상태로 전환 (거리: {dist:F2}, 범위: {enemy.AttackRange:F2})");
                        enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                        return;
                    }
                }
            }
        }
        
        // ⭐ 공격 애니메이션 + 대기 시간 완료 후 상태 전환
        float totalAttackTime = attackDuration + postAttackDelay;
        
        if (attackTimer >= totalAttackTime)
        {
            // ⭐ 스킬 시전 중이면 상태 전환 스킵 (엘리트/보스 전용)
            if (enemy is BaseEnemy baseEnemyCheck2 && baseEnemyCheck2.IsPerformingSkill)
            {
                Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 스킬 시전 중! 상태 전환 스킵 (완료까지 대기)");
                return; // 스킬 완료까지 대기
            }
            
            // 플레이어가 여전히 범위 내에 있는지 확인
            if (enemy.TargetPlayer != null)
            {
                float dist = Vector2.Distance(enemy.transform.position, enemy.TargetPlayer.transform.position);
                
                // ⭐ 보스 전용: 히스테리시스 적용
                if (enemy is Boss_SandElemental)
                {
                    float attackRange = enemy.AttackRange;
                    float chaseEndRange = attackRange - 0.5f; // 추격 종료 거리 (히스테리시스)
                    float chaseStartRange = attackRange + 1.5f; // 추격 시작 거리 (히스테리시스)
                    float rangedSkillRange = 10f; // 원거리 스킬 범위
                    
                    // 평타 범위 내 → 재공격
                    if (dist <= chaseEndRange)
                    {
                        Debug.Log($"[EnemyAttackState] {enemy.transform.name} (BOSS) - 평타 범위 내, 재공격 대기 (거리: {dist:F2}, 범위: {attackRange:F2})");
                        enemy.FSMController.ChangeState(new EnemyAttackState(enemy));
                    }
                    // 추격 시작 거리 내 → Chase 상태
                    else if (dist <= chaseStartRange)
                    {
                        Debug.Log($"[EnemyAttackState] {enemy.transform.name} (BOSS) - 추격 시작 거리, Chase 상태로 전환 (거리: {dist:F2})");
                        enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                    }
                    // 원거리 스킬 범위 내 → 재공격 (스킬만 사용)
                    else if (dist <= rangedSkillRange)
                    {
                        Debug.Log($"[EnemyAttackState] {enemy.transform.name} (BOSS) - 원거리 스킬 범위 내, 재공격 대기 (거리: {dist:F2})");
                        enemy.FSMController.ChangeState(new EnemyAttackState(enemy));
                    }
                    // 너무 멀어짐 → Idle
                    else
                    {
                        Debug.Log($"[EnemyAttackState] {enemy.transform.name} (BOSS) - 플레이어 멀어짐, Idle 상태로 전환 (거리: {dist:F2})");
                        enemy.FSMController.ChangeState(new EnemyIdleState(enemy));
                    }
                }
                // 일반 몬스터는 기존 로직
                else
                {
                    // ⭐ 공격 범위 안에 있으면 다시 Attack 상태로
                    if (dist <= enemy.AttackRange * 1.2f)
                    {
                        // ⭐ 엘리트 전용: 실제 공격 가능할 때만 Attack 전환 (빈 공격 방지)
                        if (enemy is BaseEnemy baseEnemyElite)
                        {
                            var eliteAttack = baseEnemyElite.GetComponent<EliteAttackBehaviour>();
                            if (eliteAttack != null && !eliteAttack.CanAttack())
                            {
                                // 공격 불가 (쿨다운 중) → Chase로 전환
                                Debug.Log($"[EnemyAttackState] {enemy.transform.name} (ELITE) - 공격 범위 내지만 공격 불가 (쿨다운 중), Chase 전환 (거리: {dist:F2})");
                                enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                                return;
                            }
                        }
                        
                        // 공격 가능 - 다시 Attack 상태로 전환
                        Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 공격 범위 내, 재공격 대기 (거리: {dist:F2})");
                        enemy.FSMController.ChangeState(new EnemyAttackState(enemy));
                    }
                    else if (dist < enemy.AttackRange * 3f)
                    {
                        // 중간 거리 - 추적 계속
                        Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 중간 거리, Chase 상태로 전환 (거리: {dist:F2})");
                        enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                    }
                    else
                    {
                        // 너무 멀어짐 - Idle로
                        Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 플레이어 멀어짐, Idle 상태로 전환 (거리: {dist:F2})");
                        enemy.FSMController.ChangeState(new EnemyIdleState(enemy));
                    }
                }
            }
            else
            {
                Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 플레이어 없음, Idle 상태로 전환");
                enemy.FSMController.ChangeState(new EnemyIdleState(enemy));
            }
        }
    }

    public void Exit() 
    {
        Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 공격 상태 종료");
        // 상태 종료 시 미스 카운트는 유지 (다음 공격 상태 진입 시 연속성 유지)
    }
    
    /// <summary>
    /// ⭐ 연속 미스 체크 (안전장치)
    /// </summary>
    private bool CheckConsecutiveMiss()
    {
        // MeleeAttack 컴포넌트 찾기
        MeleeAttack meleeAttack = null;
        if (enemy is BaseEnemy baseEnemy)
        {
            meleeAttack = baseEnemy.GetComponent<MeleeAttack>();
        }
        
        if (meleeAttack != null)
        {
            // 마지막 공격 결과 확인
            bool lastAttackHit = meleeAttack.LastAttackHit;
            
            if (!lastAttackHit)
            {
                consecutiveMissCount++;
                Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 미스 감지! 연속 미스: {consecutiveMissCount}회");
            }
            else
            {
                consecutiveMissCount = 0; // 히트 시 리셋
            }
        }
        
        return consecutiveMissCount >= 2;
    }
} 