using UnityEngine;
using UnityEngine.AI; // NavMeshAgent (Phase 3)

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
        bool debugEnabled = enemy is BaseEnemy be0 && be0.EnableDebugLogs;
        if (debugEnabled) Debug.Log($"🗡️ [EnemyAttackState] {enemy.transform.name} - 공격 상태 진입!");
        
        // ⭐ 1번: 공격 시작 전 거리 체크 (엄격한 범위 체크로 허공 공격 방지)
        if (enemy.TargetPlayer != null)
        {
            float dist = Vector2.Distance(enemy.transform.position, enemy.TargetPlayer.transform.position);
            
            // ⭐⭐⭐ 모든 몬스터 실제 공격 범위로 엄격하게 체크 (플레이어 넉백 고려)
            float rangeThreshold = enemy.AttackRange; // 1.0배 (엄격)
            
            // 공격 범위 밖이면 추격 상태로 전환
            if (dist > rangeThreshold)
            {
                enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                return;
            }
        }
        
        // ⭐⭐⭐ NavMeshAgent 정지 (Phase 3 - 공격 중 이동 방지)
        if (enemy is BaseEnemy baseEnemyNav && baseEnemyNav.IsUsingNavMesh)
        {
            baseEnemyNav.Agent.isStopped = true;
        }
        
        // 🔑 공격 시도 (CanAttack 체크는 각 Attack 컴포넌트에서 처리)
        enemy.Attack();
        attackTimer = 0f;
        hasCheckedAfterAnimation = false;
        
        // ⭐ 방향 확인 로그
        if (debugEnabled && enemy is BaseEnemy baseEnemy && baseEnemy.AnimationController != null)
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
                return; // 스킬 완료까지 대기
            }
            
            // ⭐ 3번: 연속 미스 체크 (안전장치)
            if (CheckConsecutiveMiss())
            {
                consecutiveMissCount = 0; // 리셋
                enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                return;
            }
            
            // 거리 체크 (애니메이션 완료 시점)
            if (enemy.TargetPlayer != null)
            {
                float dist = Vector2.Distance(enemy.transform.position, enemy.TargetPlayer.transform.position);
                
                // ⭐⭐⭐ 공격 범위 밖이면 즉시 추격 상태로 전환 (엄격한 체크)
                if (enemy is Boss_SandElemental)
                {
                    float chaseStartRange = enemy.AttackRange + 1.5f;
                    if (dist > chaseStartRange)
                    {
                        enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                        return;
                    }
                }
                else
                {
                    if (dist > enemy.AttackRange)
                    {
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
                    float chaseEndRange = attackRange - 0.5f;
                    float chaseStartRange = attackRange + 1.5f;
                    float rangedSkillRange = 10f;
                    
                    if (dist <= chaseEndRange)
                        enemy.FSMController.ChangeState(new EnemyAttackState(enemy));
                    else if (dist <= chaseStartRange)
                        enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                    else if (dist <= rangedSkillRange)
                        enemy.FSMController.ChangeState(new EnemyAttackState(enemy));
                    else
                        enemy.FSMController.ChangeState(new EnemyIdleState(enemy));
                }
                // 일반 몬스터는 엄격한 범위 체크 적용
                else
                {
                    if (dist <= enemy.AttackRange * 0.95f)
                    {
                        // ⭐ 엘리트 전용: 실제 공격 가능할 때만 Attack 전환 (빈 공격 방지)
                        if (enemy is BaseEnemy baseEnemyElite)
                        {
                            var eliteAttack = baseEnemyElite.GetComponent<EliteAttackBehaviour>();
                            if (eliteAttack != null && !eliteAttack.CanAttack())
                            {
                                enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                                return;
                            }
                        }
                        enemy.FSMController.ChangeState(new EnemyAttackState(enemy));
                    }
                    else if (dist < enemy.AttackRange * 2f)
                        enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                    else
                        enemy.FSMController.ChangeState(new EnemyIdleState(enemy));
                }
            }
            else
            {
                enemy.FSMController.ChangeState(new EnemyIdleState(enemy));
            }
        }
    }

    public void Exit() 
    {
        
        // ⭐⭐⭐ NavMeshAgent 재개 (Phase 3)
        if (enemy is BaseEnemy baseEnemyNav && baseEnemyNav.IsUsingNavMesh)
        {
            baseEnemyNav.Agent.isStopped = false;
        }
        
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
            }
            else
            {
                consecutiveMissCount = 0; // 히트 시 리셋
            }
        }
        
        return consecutiveMissCount >= 2;
    }
} 