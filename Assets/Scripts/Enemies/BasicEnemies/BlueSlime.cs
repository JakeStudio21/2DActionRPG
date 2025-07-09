using System.Collections;
using UnityEngine;

public class BlueSlime : EnemyBase, IEnemy
{
    [Header("Blue Slime Settings")]
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private LayerMask playerLayerMask = 1 << 3; // Player layer
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackSound;
    
    private bool canAttack = true;
    private PlayerController cachedPlayer;
    private EnemyHealth enemyHealth;
    private CircleCollider2D attackCollider;
    
    private void Awake()
    {
        // 컴포넌트 참조 설정
        if (animator == null)
            animator = GetComponent<Animator>();
            
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        enemyHealth = GetComponent<EnemyHealth>();
        attackCollider = GetComponent<CircleCollider2D>();
    }
    
    private void Start()
    {
        // 플레이어 참조 찾기
        StartCoroutine(FindPlayerCoroutine());
    }
    
    private IEnumerator FindPlayerCoroutine()
    {
        while (cachedPlayer == null)
        {
            cachedPlayer = FindObjectOfType<PlayerController>();
            if (cachedPlayer == null)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        Debug.Log($"[BlueSlime] {gameObject.name}이 플레이어를 찾았습니다.");
    }
    
    protected override void StateUpdate()
    {
        if (enemyHealth != null && enemyHealth.isDead)
            return;
            
        switch (currentState)
        {
            case EnemyState.Idle:
                IdleState();
                break;
                
            case EnemyState.Chase:
                ChasingState();
                break;
                
            case EnemyState.Attack:
                AttackingState();
                break;
        }
        
        // 애니메이션 업데이트는 상태 변경 시에만 호출하도록 최적화
        UpdateSpriteDirection();
    }
    
    private void IdleState()
    {
        // 플레이어가 감지 범위에 들어오면 추적 시작
        if (cachedPlayer != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, cachedPlayer.transform.position);
            if (distanceToPlayer <= attackRange * 2f) // 감지 범위는 공격 범위의 2배
            {
                ChangeState(EnemyState.Chase);
            }
        }
    }
    
    private void ChasingState()
    {
        if (cachedPlayer == null)
        {
            ChangeState(EnemyState.Idle);
            return;
        }
        
        float distanceToPlayer = Vector2.Distance(transform.position, cachedPlayer.transform.position);
        
        // 공격 범위에 들어오면 공격
        if (distanceToPlayer <= attackRange && canAttack)
        {
            ChangeState(EnemyState.Attack);
        }
        // 너무 멀어지면 Idle로 돌아감
        else if (distanceToPlayer > attackRange * 3f)
        {
            ChangeState(EnemyState.Idle);
        }
    }
    
    private void AttackingState()
    {
        // 공격 후 잠시 대기 후 추적으로 돌아감
        StartCoroutine(AttackCoroutine());
    }
    
    private IEnumerator AttackCoroutine()
    {
        canAttack = false;
        
        // 공격 애니메이션 트리거
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // 공격 사운드 재생
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
        
        // 플레이어에게 데미지 주기
        if (cachedPlayer != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, cachedPlayer.transform.position);
            if (distanceToPlayer <= attackRange)
            {
                PlayerHealth playerHealth = cachedPlayer.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage((int)attackDamage, transform);
                    Debug.Log($"[BlueSlime] 플레이어에게 {attackDamage} 데미지를 입혔습니다.");
                }
            }
        }
        
        // 공격 쿨다운 대기
        yield return new WaitForSeconds(attackCooldown);
        
        canAttack = true;
        ChangeState(EnemyState.Chase);
    }
    
    // IEnemy 인터페이스 구현
    public void Attack(EnemyAI enemyAI)
    {
        if (canAttack && currentState != EnemyState.Attack)
        {
            ChangeState(EnemyState.Attack);
        }
    }
    
    // 디버그용 Gizmo 그리기
    private void OnDrawGizmosSelected()
    {
        // 공격 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // 감지 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange * 2f);
    }
    
    // 충돌 감지 (접촉 데미지용)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (enemyHealth != null && enemyHealth.isDead)
            return;
            
        if ((playerLayerMask.value & (1 << other.gameObject.layer)) > 0)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null && canAttack)
            {
                playerHealth.TakeDamage((int)attackDamage, transform);
                canAttack = false;
                StartCoroutine(AttackCooldownRoutine());
            }
        }
    }
    
    private IEnumerator AttackCooldownRoutine()
    {
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    /// <summary>
    /// 스프라이트 방향만 업데이트 (StateIndex 관련 경고 제거)
    /// </summary>
    private void UpdateSpriteDirection()
    {
        // 이동 방향에 따른 스프라이트 뒤집기 (추적 중일 때만)
        if (cachedPlayer != null && currentState == EnemyState.Chase)
        {
            Vector2 direction = (cachedPlayer.transform.position - transform.position).normalized;
            if (direction.x != 0)
            {
                transform.localScale = new Vector3(direction.x > 0 ? 1 : -1, 1, 1);
            }
        }
    }
    
    /// <summary>
    /// 상태 변경 시에만 애니메이션 업데이트 (최적화)
    /// </summary>
    private void UpdateAnimationOnStateChange()
    {
        if (animator == null) return;
        
        // StateIndex 대신 직접 애니메이션 상태 제어
        switch (currentState)
        {
            case EnemyState.Idle:
                // Idle 애니메이션은 기본 상태이므로 특별한 처리 불필요
                break;
                
            case EnemyState.Chase:
                // Chase 상태에서는 기본 애니메이션 유지
                break;
                
            case EnemyState.Attack:
                // 공격 시에만 Attack 트리거 사용 (이미 AttackCoroutine에서 처리됨)
                break;
        }
    }
    
    /// <summary>
    /// 상태 변경 시 호출 (EnemyBase에서 오버라이드)
    /// </summary>
    protected override void OnStateChanged(EnemyState from, EnemyState to)
    {
        UpdateAnimationOnStateChange(); // 상태 변경 시에만 애니메이션 업데이트
        Debug.Log($"[BlueSlime] {gameObject.name} 상태 변경: {from} → {to}");
    }
} 