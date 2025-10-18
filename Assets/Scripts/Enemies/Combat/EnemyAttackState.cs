using UnityEngine;

public class EnemyAttackState : IEnemyState
{
    private readonly IEnemy enemy;
    private float attackTimer = 0f;
    private float attackDuration = 1.5f; // 공격 애니메이션 시간
    private float postAttackDelay = 0.5f; // ⭐ 공격 후 대기 시간 추가

    public EnemyAttackState(IEnemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        Debug.Log($"🗡️ [EnemyAttackState] {enemy.transform.name} - 공격 상태 진입!");
        
        // 🔑 공격 시도 (CanAttack 체크는 BlueSlime.Attack()에서 처리)
        enemy.Attack();
        attackTimer = 0f;
        
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
        
        // ⭐ 공격 애니메이션 + 대기 시간 완료 후 상태 전환
        float totalAttackTime = attackDuration + postAttackDelay;
        
        if (attackTimer >= totalAttackTime)
        {
            // 플레이어가 여전히 범위 내에 있는지 확인
            if (enemy.TargetPlayer != null)
            {
                float dist = Vector2.Distance(enemy.transform.position, enemy.TargetPlayer.transform.position);
                
                // ⭐ 수정: 공격 범위 안에 있으면 다시 Attack 상태로 (쿨타임 대기)
                if (dist <= enemy.AttackRange * 1.2f)
                {
                    // 공격 범위 안 - 다시 Attack 상태로 전환하여 쿨타임 후 재공격
                    Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 공격 범위 내({dist:F2}), 재공격 대기 (거리: {dist:F2})");
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
    }
} 