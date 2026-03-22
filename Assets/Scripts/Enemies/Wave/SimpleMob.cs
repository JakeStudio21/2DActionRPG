using UnityEngine;
using System.Collections;

/// <summary>
/// 경량 웨이브용 몬스터 (방치형 게임 스타일)
/// - 단순 AI (직선 이동)
/// - 접촉 데미지
/// - 오브젝트 풀링 기반
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class SimpleMob : MonoBehaviour
{
    [Header("기본 스탯")]
    [SerializeField] protected int maxHealth = 10;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float contactDamage = 5f;
    [SerializeField] protected float detectionRange = 15f;
    
    [Header("공격 설정")]
    [SerializeField] protected float attackCooldown = 0.5f;
    
    [Header("사운드/이펙트")]
    [SerializeField] protected AudioClip hitSound;
    [SerializeField] protected AudioClip deathSound;
    [SerializeField] protected GameObject hitEffectPrefab;
    
    [Header("디버그")]
    [SerializeField] protected bool enableDebugLogs = false;
    
    // 컴포넌트
    protected Rigidbody2D rb;
    protected CircleCollider2D triggerCollider;
    protected SpriteRenderer spriteRenderer;
    protected Animator animator;
    
    // 상태
    protected int currentHealth;
    protected bool isDead = false;
    protected Transform playerTransform;
    protected float lastAttackTime = 0f;
    
    // 풀링
    protected string poolTag;
    
    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public Transform PlayerTransform => playerTransform;
    
    /// <summary>
    /// 이동 속도 설정 (WaveSpawner에서 호출)
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        if (speed > 0f)
        {
            moveSpeed = speed;
            if (enableDebugLogs)
                Debug.Log($"[SimpleMob] {gameObject.name} 이동속도 설정: {moveSpeed}");
        }
    }
    
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        
        // Rigidbody2D 설정
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.drag = 0.5f; // 자연스러운 이동
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // Collider 설정 (Trigger용)
        CircleCollider2D[] colliders = GetComponents<CircleCollider2D>();
        foreach (var col in colliders)
        {
            if (col.isTrigger)
            {
                triggerCollider = col;
                break;
            }
        }
        
        poolTag = gameObject.tag;
    }
    
    protected virtual void OnEnable()
    {
        // 풀에서 꺼낼 때마다 초기화
        currentHealth = maxHealth;
        isDead = false;
        lastAttackTime = 0f;
        
        // 플레이어 찾기
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        // SimpleMobManager에 등록
        SimpleMobManager manager = FindObjectOfType<SimpleMobManager>();
        if (manager != null)
        {
            manager.RegisterMob(this);
        }
    }
    
    protected virtual void OnDisable()
    {
        // SimpleMobManager에서 제거
        SimpleMobManager manager = FindObjectOfType<SimpleMobManager>();
        if (manager != null)
        {
            manager.UnregisterMob(this);
        }
    }
    
    /// <summary>
    /// AI 업데이트 (SimpleMobManager에서 호출)
    /// </summary>
    public virtual void UpdateAI()
    {
        if (isDead || playerTransform == null) return;
        
        // 플레이어 방향으로 이동
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        rb.velocity = direction * moveSpeed;
        
        // 스프라이트 방향 전환
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }
    
    /// <summary>
    /// 데미지 처리 (int 오버로드 - 플레이어 공격용)
    /// </summary>
    public void TakeDamage(int damage)
    {
        TakeDamage((float)damage);
    }
    
    /// <summary>
    /// 데미지 처리
    /// </summary>
    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= Mathf.RoundToInt(damage);
        
        // 데미지 숫자 표시
        ShowDamageNumber(damage);
        
        // 히트 사운드
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position, 0.5f);
        }
        
        // 히트 이펙트
        if (hitEffectPrefab != null)
        {
            GameObject hitEffect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(hitEffect, 1f);
        }
        
        // 애니메이션
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 데미지 숫자 표시
    /// </summary>
    protected void ShowDamageNumber(float damage)
    {
        // DamageNumberManager가 있다면 사용
        DamageNumberManager damageNumberManager = FindObjectOfType<DamageNumberManager>();
        if (damageNumberManager != null)
        {
            damageNumberManager.ShowDamage(transform.position, Mathf.RoundToInt(damage), false);
        }
    }
    
    /// <summary>
    /// 죽음 처리
    /// </summary>
    protected virtual void Die()
    {
        if (isDead) return;
        
        isDead = true;
        rb.velocity = Vector2.zero;
        
        if (enableDebugLogs)
            Debug.Log($"[SimpleMob] {gameObject.name} 사망");
        
        // 죽음 사운드
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, 0.5f);
        }
        
        // 애니메이션
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        
        // 풀로 반환 (애니메이션 후)
        StartCoroutine(ReturnToPoolAfterDelay(0.5f));
    }
    
    /// <summary>
    /// 플레이어 공격 (접촉 데미지)
    /// </summary>
    protected virtual void AttackPlayer(Collider2D playerCollider)
    {
        PlayerHealth playerHealth = playerCollider.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(Mathf.RoundToInt(contactDamage), transform);
            lastAttackTime = Time.time;
            
            if (enableDebugLogs)
                Debug.Log($"[SimpleMob] 플레이어 타격! 데미지: {contactDamage}");
            
            // 공격 애니메이션
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
        }
    }
    
    /// <summary>
    /// 트리거 지속 체크 (플레이어 접촉 데미지)
    /// </summary>
    protected virtual void OnTriggerStay2D(Collider2D collision)
    {
        if (isDead) return;
        
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            // 쿨다운 체크
            if (Time.time < lastAttackTime + attackCooldown) return;
            
            AttackPlayer(collision);
        }
    }
    
    /// <summary>
    /// 몬스터 간 충돌 처리
    /// </summary>
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(gameObject.tag))
        {
            // 같은 SimpleMob끼리 충돌 시 밀어냄
            Vector2 pushDirection = (transform.position - collision.transform.position).normalized;
            rb.AddForce(pushDirection * 2f, ForceMode2D.Impulse);
        }
    }
    
    /// <summary>
    /// 풀로 반환
    /// </summary>
    protected IEnumerator ReturnToPoolAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (GamePoolManager.Instance != null && !string.IsNullOrEmpty(poolTag))
        {
            GamePoolManager.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // 탐지 범위 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}

