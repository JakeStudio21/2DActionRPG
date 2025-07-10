using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터 타입 열거형
/// </summary>
public enum EnemyType
{
    BlueSlime,
    Ghost,
    Grape,
    Boss
}

public class EnemyAI : MonoBehaviour, IEnemy
{
    [Header("AI Settings")]
    [SerializeField] private float attackRange = 2.5f; // 공격 범위 (적절한 크기로 조정)
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private bool stopMovingWhileAttacking = false;
    
    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 6f; // 플레이어 감지 범위 (조정)
    [SerializeField] private float chaseRange = 10f; // 추적 유지 범위 (조정)
    [SerializeField] private float patrolRadius = 5f; // 순찰 반경 (조정)
    
    [Header("Monster Type")]
    [SerializeField] private EnemyType monsterType = EnemyType.BlueSlime;
    
    [Header("Attack Settings")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private LayerMask playerLayerMask = 1 << 3; // Player layer
    
    [Header("Audio")]
    [SerializeField] private AudioClip attackSound;

    private bool canAttack = true;

    private enum State 
    {
        Idle,       // 대기 상태
        Patrol,     // 순찰 상태  
        Chase,      // 추적 상태
        Attack,     // 공격 상태
        Return      // 복귀 상태
    }   
    
    // AI 상태 관리
    private State currentState = State.Idle;
    private State previousState;
    private float stateTimer = 0f; // 상태별 타이머
    
    // 이동 및 순찰
    private Vector2 spawnPoint; // 스폰 지점
    private Vector2 patrolTarget; // 순찰 목표 지점
    private EnemyPathfinding enemyPathfinding;
    private PlayerController cachedPlayerController; // 캐시된 플레이어 참조
    
    // 컴포넌트 참조들
    private Animator animator;
    private AudioSource audioSource;
    private EnemyHealth enemyHealth;

    private void Awake() 
    {
        enemyPathfinding = GetComponent<EnemyPathfinding>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        enemyHealth = GetComponent<EnemyHealth>();
        
        // 스폰 지점 저장 (월드 좌표로 정확히 설정)
        spawnPoint = transform.position;
        
        // 디버그 로그 추가
        Debug.Log($"[EnemyAI] {gameObject.name} spawnPoint 설정: {spawnPoint}");
        
        // 초기 순찰 목표 설정
        patrolTarget = GetRandomPatrolPoint();
    }

    private void Start() 
    {
        // 플레이어 참조 캐싱 (씬 시작 시 한 번만)
        StartCoroutine(FindPlayerCoroutine());
        
        // 초기 상태를 Idle로 설정
        ChangeState(State.Idle);
    }

    private IEnumerator FindPlayerCoroutine()
    {
        // 플레이어가 스폰될 때까지 대기
        while (cachedPlayerController == null)
        {
            cachedPlayerController = FindObjectOfType<PlayerController>();
            if (cachedPlayerController == null)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        Debug.Log($"[EnemyAI] {gameObject.name}이 플레이어를 찾았습니다.");
    }

    private void Update() 
    {
        // 죽었으면 AI 동작 중단
        if (enemyHealth != null && enemyHealth.isDead)
            return;
            
        // 플레이어가 아직 스폰되지 않았으면 대기
        if (cachedPlayerController == null)
            return;

        // 상태별 타이머 업데이트
        stateTimer += Time.deltaTime;
        
        // 현재 상태에 따른 AI 로직 실행
        StateUpdate();
        
        // 스프라이트 방향 업데이트
        UpdateSpriteDirection();
    }

    /// <summary>
    /// 현재 상태에 따른 AI 업데이트
    /// </summary>
    private void StateUpdate()
    {
        switch (currentState)
        {
            case State.Idle:
                IdleState();
                break;
                
            case State.Patrol:
                PatrolState();
                break;
                
            case State.Chase:
                ChaseState();
                break;
                
            case State.Attack:
                AttackState();
                break;
                
            case State.Return:
                ReturnState();
                break;
        }
    }

    /// <summary>
    /// 대기 상태 - 플레이어 감지 대기
    /// </summary>
    private void IdleState()
    {
        // 일정 시간 후 순찰 시작
        if (stateTimer > 2f)
        {
            ChangeState(State.Patrol);
            return;
        }
        
        // 플레이어 감지 확인
        if (cachedPlayerController != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, cachedPlayerController.transform.position);
            Debug.Log($"[EnemyAI] {gameObject.name} - Idle상태: 플레이어 거리 {distanceToPlayer:F2}, 감지범위 {detectionRange}");
            
            if (distanceToPlayer <= detectionRange)
            {
                Debug.Log($"[EnemyAI] {gameObject.name} - 플레이어 감지! Chase 상태로 전환");
                ChangeState(State.Chase);
            }
        }
        else
        {
            Debug.Log($"[EnemyAI] {gameObject.name} - 플레이어 참조 없음 (Idle)");
        }
    }

    /// <summary>
    /// 순찰 상태 - 스폰 지점 주변 순찰
    /// </summary>
    private void PatrolState()
    {
        // 플레이어 감지 우선 확인
        if (cachedPlayerController != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, cachedPlayerController.transform.position);
            if (distanceToPlayer <= detectionRange)
            {
                ChangeState(State.Chase);
                return;
            }
        }
        
        // 순찰 목표로 이동
        if (enemyPathfinding != null)
        {
            enemyPathfinding.MoveTo(patrolTarget);
        }
        
        // 순찰 목표에 도착했는지 확인
        float distanceToTarget = Vector2.Distance(transform.position, patrolTarget);
        if (distanceToTarget < 1f || stateTimer > 5f)
        {
            // 새로운 순찰 목표 설정
            patrolTarget = GetRandomPatrolPoint();
            ChangeState(State.Idle);
        }
    }

    /// <summary>
    /// 추적 상태 - 플레이어 추적
    /// </summary>
    private void ChaseState()
    {
        if (cachedPlayerController == null)
        {
            Debug.Log($"[EnemyAI] {gameObject.name} - Chase상태: 플레이어 참조 없음, Return으로 전환");
            ChangeState(State.Return);
            return;
        }
        
        float distanceToPlayer = Vector2.Distance(transform.position, cachedPlayerController.transform.position);
        Debug.Log($"[EnemyAI] {gameObject.name} - Chase상태: 플레이어 거리 {distanceToPlayer:F2}, 공격범위 {attackRange}, 추적범위 {chaseRange}");
        
        // 공격 범위에 들어오면 공격
        if (distanceToPlayer <= attackRange && canAttack)
        {
            Debug.Log($"[EnemyAI] {gameObject.name} - 공격 범위 진입! Attack 상태로 전환");
            ChangeState(State.Attack);
            return;
        }
        
        // 추적 범위를 벗어나면 복귀
        if (distanceToPlayer > chaseRange)
        {
            Debug.Log($"[EnemyAI] {gameObject.name} - 추적 범위 이탈! Return 상태로 전환");
            ChangeState(State.Return);
            return;
        }
        
        // 플레이어 추적
        if (enemyPathfinding != null)
        {
            Vector2 playerPos = cachedPlayerController.transform.position;
            Debug.Log($"[EnemyAI] {gameObject.name} - 플레이어 추적 중: 목표위치 {playerPos}, 현재위치 {transform.position}");
            enemyPathfinding.MoveTo(playerPos);
        }
        else
        {
            Debug.LogWarning($"[EnemyAI] {gameObject.name} - EnemyPathfinding 컴포넌트 없음!");
        }
    }

    /// <summary>
    /// 공격 상태 - 공격 실행
    /// </summary>
    private void AttackState()
    {
        // 공격 중에는 이동 정지 (설정에 따라)
        if (stopMovingWhileAttacking && enemyPathfinding != null)
        {
            enemyPathfinding.StopMoving();
        }
        
        // 공격 실행 (한 번만)
        if (canAttack)
        {
            canAttack = false;
            Attack(this);
            StartCoroutine(AttackCooldownRoutine());
        }
        
        // 공격 후 추적으로 복귀 (짧은 딜레이)
        if (stateTimer > 1f)
        {
            ChangeState(State.Chase);
        }
    }

    /// <summary>
    /// 복귀 상태 - 스폰 지점으로 복귀
    /// </summary>
    private void ReturnState()
    {
        // 스폰 지점으로 이동
        if (enemyPathfinding != null)
        {
            enemyPathfinding.MoveTo(spawnPoint);
        }
        
        // 스폰 지점에 도착하면 Idle 상태로
        float distanceToSpawn = Vector2.Distance(transform.position, spawnPoint);
        if (distanceToSpawn < 2f)
        {
            ChangeState(State.Idle);
        }
        
        // 복귀 중에도 가까운 플레이어는 감지
        if (cachedPlayerController != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, cachedPlayerController.transform.position);
            if (distanceToPlayer <= detectionRange * 0.5f) // 절반 거리에서 재감지
            {
                ChangeState(State.Chase);
            }
        }
    }

    /// <summary>
    /// AI 상태 변경
    /// </summary>
    private void ChangeState(State newState)
    {
        if (currentState != newState)
        {
            previousState = currentState;
            currentState = newState;
            stateTimer = 0f; // 타이머 리셋
            
            Debug.Log($"[EnemyAI] {gameObject.name} 상태 변경: {previousState} → {newState}");
        }
    }

    /// <summary>
    /// 랜덤 순찰 지점 생성
    /// </summary>
    private Vector2 GetRandomPatrolPoint()
    {
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        float randomDistance = Random.Range(2f, patrolRadius);
        return spawnPoint + (randomDirection * randomDistance);
    }

    /// <summary>
    /// 스프라이트 방향 업데이트 (이동 방향에 따라)
    /// </summary>
    private void UpdateSpriteDirection()
    {
        // EnemyPathfinding은 자체적으로 스프라이트 방향을 처리하므로
        // 여기서는 별도 처리하지 않음
        // (EnemyPathfinding.cs에서 spriteRenderer.flipX로 방향 조절)
    }

    /// <summary>
    /// IEnemy 인터페이스 구현 - 몬스터별 공격 로직
    /// </summary>
    public void Attack(EnemyAI enemyAI)
    {
        StartCoroutine(AttackCoroutine());
    }
    
    /// <summary>
    /// 몬스터별 공격 실행 코루틴
    /// </summary>
    private IEnumerator AttackCoroutine()
    {
        switch (monsterType)
        {
            case EnemyType.BlueSlime:
                yield return StartCoroutine(BlueSlimeAttack());
                break;
                
            case EnemyType.Ghost:
                // Ghost 공격 로직은 별도 컴포넌트에서 처리
                var ghost = GetComponent<Ghost>();
                if (ghost != null)
                {
                    ghost.Attack(this);
                }
                break;
                
            case EnemyType.Grape:
                // Grape 공격 로직은 별도 컴포넌트에서 처리
                var grape = GetComponent<Grape>();
                if (grape != null)
                {
                    grape.Attack(this);
                }
                break;
                
            default:
                Debug.LogWarning($"[EnemyAI] {monsterType} 타입의 공격이 구현되지 않았습니다.");
                break;
        }
    }
    
    /// <summary>
    /// BlueSlime 전용 공격 로직 - Animation Event 방식
    /// </summary>
    private IEnumerator BlueSlimeAttack()
    {
        Debug.Log($"[EnemyAI] {gameObject.name} - 공격 애니메이션 시작!");
        
        // 공격 애니메이션 트리거 (안전 처리)
        if (animator != null)
        {
            // Attack 트리거가 존재하는지 확인 후 실행
            foreach (var param in animator.parameters)
            {
                if (param.name == "Attack" && param.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.SetTrigger("Attack");
                    break;
                }
            }
        }
        
        // 공격 사운드 재생
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
        
        // 애니메이션이 끝날 때까지 대기 (Animation Event에서 데미지 처리)
        yield return new WaitForSeconds(1f); // 애니메이션 길이에 맞춰 조정
    }
    
    /// <summary>
    /// Animation Event에서 호출되는 데미지 처리 함수
    /// </summary>
    public void OnAttackHit()
    {
        Debug.Log($"[EnemyAI] {gameObject.name} - Animation Event 데미지 적용!");
        
        // 플레이어에게 데미지 주기 (애니메이션 정확한 타이밍에 실행)
        if (cachedPlayerController != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, cachedPlayerController.transform.position);
            if (distanceToPlayer <= attackRange)
            {
                PlayerHealth playerHealth = cachedPlayerController.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage, transform);
                    Debug.Log($"[EnemyAI] {gameObject.name}이 플레이어에게 {attackDamage} 데미지를 입혔습니다. (Animation Event)");
                }
            }
            else
            {
                Debug.Log($"[EnemyAI] {gameObject.name} - 공격 히트 시점에 플레이어가 범위를 벗어났습니다. (거리: {distanceToPlayer:F2})");
            }
        }
    }

    private IEnumerator AttackCooldownRoutine() 
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }



    public int GetProjectileDamage()
    {
        return projectileDamage;
    }

    // 외부에서 플레이어 참조를 강제로 새로고침할 수 있는 메서드
    public void RefreshPlayerReference()
    {
        cachedPlayerController = FindObjectOfType<PlayerController>();
    }
    
    /// <summary>
    /// 충돌 감지 (접촉 데미지용)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (enemyHealth != null && enemyHealth.isDead)
            return;
            
        if ((playerLayerMask.value & (1 << other.gameObject.layer)) > 0)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null && canAttack)
            {
                playerHealth.TakeDamage(attackDamage, transform);
                Debug.Log($"[EnemyAI] {gameObject.name}이 접촉으로 플레이어에게 {attackDamage} 데미지를 입혔습니다.");
            }
        }
    }
    
    /// <summary>
    /// 디버그용 AI 범위들 시각화 - 항상 표시
    /// </summary>
    private void OnDrawGizmos()
    {
        // 공격 범위 시각화 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // 감지 범위 시각화 (노란색)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // 추적 범위 시각화 (주황색)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        
        // 순찰 범위 시각화 (스폰 지점 기준, 파란색)
        Gizmos.color = new Color(0f, 0f, 1f, 0.2f);
        if (Application.isPlaying)
        {
            // 런타임 중에는 저장된 spawnPoint 사용
            Gizmos.DrawWireSphere(spawnPoint, patrolRadius);
        }
        else
        {
            // 에디터에서는 현재 위치 사용
            Gizmos.DrawWireSphere(transform.position, patrolRadius);
        }
        
        // 스폰 지점 시각화 (초록색 큐브)
        Gizmos.color = Color.green;
        if (Application.isPlaying)
        {
            Gizmos.DrawCube(spawnPoint, Vector3.one * 0.5f);
        }
        else
        {
            Gizmos.DrawCube(transform.position, Vector3.one * 0.5f);
        }
        
        // 현재 순찰 목표 시각화 (자홍색 큐브)
        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawCube(patrolTarget, Vector3.one * 0.3f);
        }
        
        #if UNITY_EDITOR
        // 상태 정보 텍스트
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, $"State: {currentState}\nTimer: {stateTimer:F1}s");
        }
        #endif
    }
} 