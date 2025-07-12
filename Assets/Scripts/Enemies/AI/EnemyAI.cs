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
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float attackCooldown = 2f;
    
    [Header("Detection Settings")]
    [SerializeField] private float detectionRange = 8f; // 플레이어 감지 범위 (원거리 몬스터용 확대)
    [SerializeField] private float chaseRange = 12f; // 추적 유지 범위 (원거리 몬스터용 확대)
    [SerializeField] private float patrolRadius = 5f; // 순찰 반경 (조정)
    
    [Header("Monster Type")]
    [SerializeField] private EnemyType monsterType = EnemyType.BlueSlime;
    
    private bool canAttack = true;
    private IAttackBehaviour attackBehaviour;

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
        
        // ⭐ 공격 시스템 초기화
        InitializeAttackSystem();
    }
    
    /// <summary>
    /// 공격 시스템 초기화 - 완전 모듈식 시스템
    /// </summary>
    private void InitializeAttackSystem()
    {
        // 몬스터 타입에 따라 적절한 공격 컴포넌트 찾기
        switch (monsterType)
        {
            case EnemyType.BlueSlime:
                attackBehaviour = GetComponent<MeleeAttack>();
                break;
                
            case EnemyType.Grape:
                attackBehaviour = GetComponent<RangedAttack>();
                break;
                
            case EnemyType.Ghost:
                attackBehaviour = GetComponent<MultiShotRangedAttack>();
                break;
                
            case EnemyType.Boss:
                // 보스는 현재 Ghost 스크립트를 사용하므로 일시적으로 Ghost 컴포넌트 사용
                // 나중에 별도 보스 공격 시스템으로 교체 예정
                var bossGhostComponent = GetComponent<Ghost>();
                if (bossGhostComponent != null)
                {
                    Debug.Log($"[EnemyAI] {gameObject.name} - Boss 타입은 현재 Ghost 컴포넌트를 사용합니다 (Legacy)");
                    return; // Ghost 컴포넌트가 있으면 그것을 사용
                }
                else
                {
                    // Ghost 컴포넌트가 없으면 MultiShotRangedAttack 시도
                    attackBehaviour = GetComponent<MultiShotRangedAttack>();
                }
                break;
                
            default:
                Debug.LogWarning($"[EnemyAI] {monsterType} 타입의 공격 컴포넌트를 찾을 수 없습니다.");
                break;
        }
        
        // 공격 시스템 초기화
        if (attackBehaviour != null)
        {
            attackBehaviour.Initialize(this);
            Debug.Log($"[EnemyAI] {gameObject.name} 모듈식 공격 시스템 적용: {attackBehaviour.GetType().Name}");
        }
        else
        {
            Debug.LogError($"[EnemyAI] {gameObject.name} - {monsterType} 타입의 공격 컴포넌트가 없습니다!");
        }
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
            // Debug.Log($"[EnemyAI] {gameObject.name} - Idle상태: 플레이어 거리 {distanceToPlayer:F2}, 감지범위 {detectionRange}");
            
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
        // Debug.Log($"[EnemyAI] {gameObject.name} - Chase상태: 플레이어 거리 {distanceToPlayer:F2}, 공격범위 {attackRange}, 추적범위 {chaseRange}");
        
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
            // Debug.Log($"[EnemyAI] {gameObject.name} - 플레이어 추적 중: 목표위치 {playerPos}, 현재위치 {transform.position}");
            enemyPathfinding.MoveTo(playerPos);
        }
        else
        {
            Debug.LogWarning($"[EnemyAI] {gameObject.name} - EnemyPathfinding 컴포넌트 없음!");
        }
    }

    /// <summary>
    /// 공격 상태 - 완전 모듈식 시스템
    /// </summary>
    private void AttackState()
    {
        // 공격 중에는 이동 정지
        if (attackBehaviour != null && attackBehaviour.ShouldStopMovingWhileAttacking() && enemyPathfinding != null)
        {
            enemyPathfinding.StopMoving();
        }
        
        // 공격 실행 (한 번만)
        if (attackBehaviour != null && attackBehaviour.CanAttack())
        {
            attackBehaviour.Attack(this);
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
        // 여기서는 별도 처리하지 않음
        // (EnemyPathfinding.cs에서 spriteRenderer.flipX로 방향 조절)
    }

    // ⭐ 모듈식 공격 시스템용 유틸리티 메서드들
    public PlayerController GetCachedPlayer() => cachedPlayerController;
    public float GetAttackRange() => attackRange;
    public float GetAttackCooldown() => attackCooldown;
    
    /// <summary>
    /// Legacy 코드 호환성을 위한 GetProjectileDamage() 메서드
    /// </summary>
    public int GetProjectileDamage()
    {
        // 기본 투사체 데미지 값 반환 (Legacy 호환성)
        return 2;
    }

    /// <summary>
    /// IEnemy 인터페이스 구현 - 완전 모듈식 공격 시스템
    /// </summary>
    public void Attack(EnemyAI enemyAI)
    {
        if (attackBehaviour != null)
        {
            attackBehaviour.Attack(this);
        }
        else
        {
            Debug.LogError($"[EnemyAI] {gameObject.name} - 공격 컴포넌트가 없습니다!");
        }
    }

    // 외부에서 플레이어 참조를 강제로 새로고침할 수 있는 메서드
    public void RefreshPlayerReference()
    {
        cachedPlayerController = FindObjectOfType<PlayerController>();
    }
    
    /// <summary>
    /// 충돌 감지 (접촉 데미지용) - 모듈식 시스템에서는 사용하지 않음
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (enemyHealth != null && enemyHealth.isDead)
            return;
            
        // 모듈식 공격 시스템에서는 접촉 데미지를 사용하지 않음
        if (attackBehaviour == null) // 모듈식 공격 시스템이 활성화되지 않았거나 공격 컴포넌트가 없는 경우
        {
            Debug.LogWarning($"[EnemyAI] {gameObject.name} - 접촉 데미지는 모듈식 공격 시스템에서 지원하지 않습니다.");
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