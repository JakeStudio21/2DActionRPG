using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 홈 위치로 돌아가는 상태
/// </summary>
public class EnemyReturnToHomeState : IEnemyState
{
    private readonly IEnemy enemy;
    private Vector2 homePosition;
    private float moveSpeed; // 🔑 초기값 제거

    public EnemyReturnToHomeState(IEnemy enemy)
    {
        this.enemy = enemy;
        
        // 🔑 데이터 기반 이동속도 사용 (복귀 시에는 1.2배 빠르게)
        moveSpeed = enemy.MoveSpeed * 1.2f; // BaseEnemy의 GetScaledMoveSpeed() * 1.2
        
        // BaseEnemy의 HomePosition 시스템 사용
        if (enemy is BaseEnemy baseEnemy)
        {
            homePosition = baseEnemy.HomePosition;
            
            // 🔑 디버그 로그 추가
            if (baseEnemy.EnableDebugLogs)
            {
                Debug.Log($"[EnemyReturnToHomeState] {enemy.name} 복귀속도: {moveSpeed:F1} (기본속도 * 1.2)");
            }
        }
        else
        {
            // 기존 시스템 fallback
            if (enemy is BlueSlime blueSlime)
            {
                homePosition = blueSlime.SpawnPoint;
            }
            else if (enemy is Grape grape)
            {
                homePosition = grape.SpawnPoint;
            }
            else if (enemy is Ghost ghost)
            {
                homePosition = ghost.SpawnPoint;
            }
            else
            {
                homePosition = enemy.transform.position;
            }
            
            // fallback 이동속도
            if (moveSpeed <= 0)
            {
                moveSpeed = 2f; // 기본값
            }
        }
    }

    public void Enter()
    {
        // 🔑 디버그 로그 추가
        if (enemy.EnableDebugLogs)
        {
            Debug.Log($"[EnemyReturnToHomeState] {enemy.name} 집으로 돌아가는 중...");
        }
    }

    public void Execute() // 🔑 Update() → Execute()로 변경
    {
        // 플레이어 감지 체크 (우선순위) - 복귀 중에도 플레이어가 너무 가까이 오면 추격
        if (enemy.TargetPlayer != null)
        {
            float distToPlayer = Vector2.Distance(enemy.transform.position, enemy.TargetPlayer.transform.position);
            
            // 복귀 중에는 감지 범위를 더 보수적으로 (50%)
            float returnDetectionRange = GetReturnDetectionRange() * 0.5f;
            
            if (distToPlayer < returnDetectionRange)
            {
                // 🔑 디버그 로그 추가
                if (enemy is BaseEnemy baseEnemy && baseEnemy.EnableDebugLogs)
                {
                    Debug.Log($"[EnemyReturnToHomeState] {enemy.name} 복귀 중 플레이어 감지! 추격 시작");
                }
                
                enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                return;
            }
        }

        // 홈 위치로 이동
        Vector2 direction = (homePosition - (Vector2)enemy.transform.position).normalized;
        enemy.transform.position += (Vector3)direction * moveSpeed * Time.deltaTime;
        
        // 홈 근처 도달 시 대기 상태로
        float distanceToHome = Vector2.Distance(enemy.transform.position, homePosition);
        if (distanceToHome < 1f)
        {
            // 🔑 디버그 로그 추가
            if (enemy is BaseEnemy baseEnemy && baseEnemy.EnableDebugLogs)
            {
                Debug.Log($"[EnemyReturnToHomeState] {enemy.name} 집 도착! 대기 상태로 전환");
            }
            
            enemy.FSMController.ChangeState(new EnemyIdleState(enemy));
        }
    }

    public void Exit()
    {
        // 🔑 디버그 로그 추가
        if (enemy is BaseEnemy baseEnemy && baseEnemy.EnableDebugLogs)
        {
            Debug.Log($"[EnemyReturnToHomeState] {enemy.name} 복귀 상태 종료");
        }
    }

    /// <summary>
    /// 🔑 복귀 중 플레이어 감지 범위 계산
    /// </summary>
    private float GetReturnDetectionRange()
    {
        float detectionRange = 4f; // 기본값
        
        // BaseEnemy 시스템 우선 사용
        if (enemy is BaseEnemy baseEnemy)
        {
            detectionRange = baseEnemy.DetectionRange;
        }
        else
        {
            // 기존 시스템 fallback
            if (enemy is BlueSlime blueSlime)
            {
                var meleeAttack = blueSlime.GetComponent<MeleeAttack>();
                if (meleeAttack != null)
                {
                    detectionRange = meleeAttack.GetDetectionRange();
                }
            }
            else if (enemy is Grape grape)
            {
                var rangedAttack = grape.GetComponent<RangedAttack>();
                if (rangedAttack != null)
                {
                    detectionRange = rangedAttack.GetDetectionRange();
                }
            }
            else if (enemy is Ghost ghost)
            {
                var multiShotAttack = ghost.GetComponent<MultiShotRangedAttack>();
                if (multiShotAttack != null)
                {
                    detectionRange = multiShotAttack.GetDetectionRange();
                }
            }
        }
        
        return detectionRange;
    }
}
