using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPatrolState : IEnemyState
{
    private readonly IEnemy enemy;
    private Vector2 patrolTarget;
    private Vector2 spawnPoint;
    private float patrolRadius = 3f;
    private float moveSpeed = 1.5f;
    private float patrolTimer = 0f;
    private float maxPatrolTime = 5f;
    private bool returningToSpawn = false; // 🔑 스폰 지점으로 돌아가는 중인지 체크

    public EnemyPatrolState(IEnemy enemy)
    {
        this.enemy = enemy;
        
        // 🔑 수정: BlueSlime, Grape, Ghost의 실제 SpawnPoint 사용, 다른 몬스터는 현재 위치 사용
        if (enemy is BlueSlime blueSlime)
        {
            spawnPoint = blueSlime.SpawnPoint; // 실제 스폰 지점 사용
            patrolRadius = blueSlime.PatrolRadius;
            Debug.Log($"[EnemyPatrolState] {enemy.transform.name} - BlueSlime 실제 스폰 지점 사용: {spawnPoint}, 순찰반지름: {patrolRadius}");
        }
        else if (enemy is Grape grape) // 🔑 Grape 추가
        {
            spawnPoint = grape.SpawnPoint; // 실제 스폰 지점 사용
            patrolRadius = grape.PatrolRadius;
            Debug.Log($"[EnemyPatrolState] {enemy.transform.name} - Grape 실제 스폰 지점 사용: {spawnPoint}, 순찰반지름: {patrolRadius}");
        }
        else if (enemy is Ghost ghost) // 🔑 Ghost 추가
        {
            spawnPoint = ghost.SpawnPoint; // 실제 스폰 지점 사용
            patrolRadius = ghost.PatrolRadius;
            Debug.Log($"[EnemyPatrolState] {enemy.transform.name} - Ghost 실제 스폰 지점 사용: {spawnPoint}, 순찰반지름: {patrolRadius}");
        }
        else
        {
            spawnPoint = enemy.transform.position; // 다른 몬스터는 기존 방식
        }
    }

    public void Enter()
    {
        Debug.Log($"[EnemyPatrolState] {enemy.transform.name} - 순찰 상태 진입");
        Debug.Log($"  현재 위치: {enemy.transform.position}, 스폰 지점: {spawnPoint}");
        
        // 🔑 스폰 지점에서 멀리 떨어져 있으면 먼저 스폰 지점으로 돌아가기
        float distToSpawn = Vector2.Distance(enemy.transform.position, spawnPoint);
        Debug.Log($"  스폰 지점까지 거리: {distToSpawn:F2}f, 순찰 반지름: {patrolRadius:F2}f");
        
        if (distToSpawn > patrolRadius)
        {
            returningToSpawn = true;
            patrolTarget = spawnPoint;
            Debug.Log($"[EnemyPatrolState] {enemy.transform.name} - 스폰 지점으로 직접 복귀 중 (거리: {distToSpawn:F2})");
        }
        else
        {
            returningToSpawn = false;
            GenerateNewPatrolTarget();
            Debug.Log($"[EnemyPatrolState] {enemy.transform.name} - 스폰 지점 근처에서 순찰 시작");
        }
        patrolTimer = 0f;
    }

    public void Execute()
    {
        patrolTimer += Time.deltaTime;

        // 플레이어 감지 체크 (우선순위) - 너무 가까이 있을 때만
        if (enemy.TargetPlayer != null)
        {
            float distToPlayer = Vector2.Distance(enemy.transform.position, enemy.TargetPlayer.transform.position);
            
            // 🔑 순찰 중에는 감지 범위를 좀 더 보수적으로 설정 (감지범위의 80%)
            float patrolDetectionRange = 4f; // 기본값
            if (enemy is BlueSlime blueSlime)
            {
                var meleeAttack = blueSlime.GetComponent<MeleeAttack>();
                if (meleeAttack != null)
                {
                    patrolDetectionRange = meleeAttack.GetDetectionRange() * 0.8f; // 감지범위의 80%
                }
            }
            else if (enemy is Grape grape) // 🔑 Grape 추가
            {
                var rangedAttack = grape.GetComponent<RangedAttack>();
                if (rangedAttack != null)
                {
                    patrolDetectionRange = rangedAttack.GetDetectionRange() * 0.8f; // 감지범위의 80%
                }
            }
            else if (enemy is Ghost ghost) // 🔑 Ghost 추가
            {
                var multiShotAttack = ghost.GetComponent<MultiShotRangedAttack>();
                if (multiShotAttack != null)
                {
                    patrolDetectionRange = multiShotAttack.GetDetectionRange() * 0.8f; // 감지범위의 80%
                }
            }
            
            if (distToPlayer < patrolDetectionRange)
            {
                Debug.Log($"[EnemyPatrolState] {enemy.transform.name} - 순찰 중 플레이어 재감지! Chase 상태로 전환 (거리: {distToPlayer:F2}, 감지: {patrolDetectionRange:F2})");
                enemy.FSMController.ChangeState(new EnemyChaseState(enemy));
                return;
            }
        }

        // 🔑 스폰 지점으로 돌아가는 중
        if (returningToSpawn)
        {
            Vector2 direction = (patrolTarget - (Vector2)enemy.transform.position).normalized;
            enemy.transform.position += (Vector3)direction * moveSpeed * Time.deltaTime;

            float distToSpawn = Vector2.Distance(enemy.transform.position, spawnPoint);
            if (distToSpawn <= 0.5f) // 스폰 지점에 도착
            {
                returningToSpawn = false;
                GenerateNewPatrolTarget();
                patrolTimer = 0f;
                Debug.Log($"[EnemyPatrolState] {enemy.transform.name} - 스폰 지점 도착! 정상 순찰 시작");
            }
        }
        // 🔑 정상 순찰 중
        else
        {
            Vector2 direction = (patrolTarget - (Vector2)enemy.transform.position).normalized;
            enemy.transform.position += (Vector3)direction * moveSpeed * Time.deltaTime;

            // 목표 도달 또는 시간 초과 시 새로운 목표 설정
            float distToTarget = Vector2.Distance(enemy.transform.position, patrolTarget);
            if (distToTarget < 0.5f || patrolTimer > maxPatrolTime)
            {
                GenerateNewPatrolTarget();
                patrolTimer = 0f;
            }

            // 순찰 범위를 벗어나면 스폰 지점으로 복귀
            float distToSpawn = Vector2.Distance(enemy.transform.position, spawnPoint);
            if (distToSpawn > patrolRadius * 1.5f)
            {
                returningToSpawn = true;
                patrolTarget = spawnPoint;
                Debug.Log($"[EnemyPatrolState] {enemy.transform.name} - 순찰 범위 이탈, 스폰 지점으로 복귀");
            }
        }
    }

    public void Exit() { }

    private void GenerateNewPatrolTarget()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(1f, patrolRadius);
        patrolTarget = spawnPoint + (randomDirection * randomDistance);
        
        Debug.Log($"[EnemyPatrolState] {enemy.transform.name} - 새 순찰 목표 생성: {patrolTarget} (스폰지점: {spawnPoint}, 거리: {randomDistance:F2}f, 방향: {randomDirection})");
    }
}
