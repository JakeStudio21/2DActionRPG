using UnityEngine;

public class EnemyAttackState : IEnemyState
{
    private readonly IEnemy enemy;
    private float attackTimer = 0f;
    private float attackDuration = 1.5f; // 공격 애니메이션 시간

    public EnemyAttackState(IEnemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 공격 상태 진입!");
        
        // 🔑 공격 시도 (CanAttack 체크는 BlueSlime.Attack()에서 처리)
        enemy.Attack();
        attackTimer = 0f;
    }

    public void Execute()
    {
        attackTimer += Time.deltaTime;
        
        // 🔑 공격 애니메이션 완료 후 상태 전환
        if (attackTimer >= attackDuration)
        {
            // 플레이어가 여전히 범위 내에 있는지 확인
            if (enemy.TargetPlayer != null)
            {
                float dist = Vector2.Distance(enemy.transform.position, enemy.TargetPlayer.transform.position);
                
                // 🔑 공격 범위보다 약간 멀어지면 Chase로, 너무 멀어지면 Idle로
                if (dist <= enemy.AttackRange * 1.2f)
                {
                    // 아직 공격 범위 근처 - 잠시 대기 후 다시 공격 시도할 수 있도록 Chase로
                    Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 공격 완료, 플레이어 추적 계속 (거리: {dist:F2})");
                    enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                }
                else if (dist < enemy.AttackRange * 3f)
                {
                    // 중간 거리 - 추적 계속
                    Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 공격 완료, Chase 상태로 전환 (거리: {dist:F2})");
                    enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                }
                else
                {
                    // 너무 멀어짐 - Idle로
                    Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 공격 완료, 플레이어 멀어짐, Idle 상태로 전환 (거리: {dist:F2})");
                    enemy.FSMController.ChangeState(new EnemyIdleState(enemy));
                }
            }
            else
            {
                Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 공격 완료, 플레이어 없음, Idle 상태로 전환");
                enemy.FSMController.ChangeState(new EnemyIdleState(enemy));
            }
        }
    }

    public void Exit() 
    {
        Debug.Log($"[EnemyAttackState] {enemy.transform.name} - 공격 상태 종료");
    }
} 