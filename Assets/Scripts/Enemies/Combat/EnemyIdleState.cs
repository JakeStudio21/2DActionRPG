using UnityEngine;

public class EnemyIdleState : IEnemyState
{
    private readonly IEnemy enemy;
    private float idleTimer = 0f;
    private float maxIdleTime = 3f; // 3초 후 순찰 시작

    public EnemyIdleState(IEnemy enemy)
    {
        this.enemy = enemy;
    }

    public void Enter()
    {
        // ❌ 제거: enemy.AnimationController?.PlayIdle();
        // ✅ Walking이 기본 상태이므로 별도 애니메이션 호출 불필요
        idleTimer = 0f;
    }

    public void Execute()
    {
        idleTimer += Time.deltaTime;

        // 플레이어 감지 (우선순위)
        if (enemy.TargetPlayer != null)
        {
            float dist = Vector2.Distance(enemy.transform.position, enemy.TargetPlayer.transform.position);
            
            // 🔑 BlueSlime과 Grape의 경우 각각의 detectionRange 사용, 다른 몬스터는 기본값 사용
            float detectionRange = 5f; // 기본값
            if (enemy is BlueSlime blueSlime)
            {
                var meleeAttack = blueSlime.GetComponent<MeleeAttack>();
                if (meleeAttack != null)
                {
                    detectionRange = meleeAttack.GetDetectionRange();
                }
            }
            else if (enemy is Grape grape) // 🔑 Grape 추가
            {
                var rangedAttack = grape.GetComponent<RangedAttack>();
                if (rangedAttack != null)
                {
                    detectionRange = rangedAttack.GetDetectionRange();
                }
            }
            else if (enemy is Ghost ghost) // 🔑 Ghost 추가
            {
                var multiShotAttack = ghost.GetComponent<MultiShotRangedAttack>();
                if (multiShotAttack != null)
                {
                    detectionRange = multiShotAttack.GetDetectionRange();
                    // Debug.Log($"[EnemyIdleState] Ghost {enemy.transform.name} - MultiShotAttack 감지범위: {detectionRange}"); // 필요시 활성화
                }
                else
                {
                    Debug.LogError($"[EnemyIdleState] Ghost {enemy.transform.name} - MultiShotRangedAttack 컴포넌트가 없습니다!");
                }
            }
            
            Debug.Log($"[EnemyIdleState] {enemy.transform.name} - 플레이어 거리: {dist:F2}, 최종 감지범위: {detectionRange:F2}");
            
            if (dist < detectionRange)
            {
                Debug.Log($"[EnemyIdleState] {enemy.transform.name} - 플레이어 감지! Chase 상태로 전환");
                enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                return;
            }
        }

        // 일정 시간 후 순찰 시작
        if (idleTimer >= maxIdleTime)
        {
            Debug.Log($"[EnemyIdleState] {enemy.transform.name} - Idle 시간 초과, Patrol 상태로 전환");
            enemy.FSMController.ChangeState(new EnemyPatrolState(enemy));
        }
    }

    public void Exit() { }
} 