using UnityEngine;
using System.Collections;
using CueSystem;

/// <summary>
/// 경량 웨이브용 몬스터 (방치형 게임 스타일)
/// - 단순 AI (직선 이동)
/// - 접촉 데미지
/// - 오브젝트 풀링 기반
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class SimpleMob : MonoBehaviour, ITargetable
{
    [Header("📦 데이터 (선택 — null이면 아래 Inspector 값 사용)")]
    [SerializeField] protected SimpleMobData mobData;

    [Header("기본 스탯")]
    [SerializeField] protected int maxHealth = 10;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float contactDamage = 5f;
    [SerializeField] protected float detectionRange = 15f;
    
    [Header("공격 설정")]
    [SerializeField] protected float attackCooldown = 0.5f;
    
    [Header("디버그")]
    
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

    // CueSystem
    private string _cueEmitDomain;

    // 체력바
    protected BasicEnemyHealthBarUI healthBar;

    // 군집 분산
    private Vector2 _personalTargetOffset;
    private bool _isInContactRange = false;

    // 풀링
    protected string poolTag;
    
    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public Transform PlayerTransform => playerTransform;
    public SimpleMobData MobData => mobData;
    /// <summary>플레이어 contactRange 이내에 정지한 상태 — Manager가 분리력 스킵 여부 판단에 사용</summary>
    public bool IsInContactRange => _isInContactRange;
    
    /// <summary>
    /// 이동 속도 설정 (WaveSpawner에서 호출)
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        if (speed > 0f)
        {
            // 외부에서 속도를 지정해도 지터를 다시 적용하여 개체마다 다른 속도 유지
            float jitterMin = mobData != null ? mobData.speedJitterMin : 0.8f;
            float jitterMax = mobData != null ? mobData.speedJitterMax : 1.2f;
            moveSpeed = speed * Random.Range(jitterMin, jitterMax);

        }
    }
    
    // 슬로우 상태 관리 (DotDamageArea에서 사용)
    private float _baseSpeedBeforeSlow;
    private bool _isSlowed = false;
    
    /// <summary>
    /// 이동속도 감소 적용 (DotDamageArea 장판 진입 시 호출)
    /// 중복 적용을 방지하며, 원래 속도를 보존하여 정확한 복원을 보장합니다.
    /// </summary>
    public void ApplySlow(float slowPercentage)
    {
        if (_isSlowed) return;
        _baseSpeedBeforeSlow = moveSpeed;
        moveSpeed = moveSpeed * (1f - Mathf.Clamp01(slowPercentage));
        _isSlowed = true;
        
    }
    
    /// <summary>
    /// 이동속도 감소 해제 (DotDamageArea 장판 퇴장 또는 소멸 시 호출)
    /// </summary>
    public void RemoveSlow()
    {
        if (!_isSlowed) return;
        moveSpeed = _baseSpeedBeforeSlow;
        _isSlowed = false;
        
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
        // mobData가 있으면 SO 수치로 덮어쓰기 (없으면 Inspector 직접값 유지)
        if (mobData != null)
        {
            maxHealth     = mobData.maxHp;
            moveSpeed     = mobData.moveSpeed;
            contactDamage = mobData.collisionDamage;

            // CueProfile 등록 — CueRegistry 내부 중복 체크로 풀링 반복 호출 시에도 안전
            if (mobData.hitCueProfile != null)
            {
                _cueEmitDomain = mobData.hitCueProfile.profileId;
                CueRegistry.Instance.RegisterProfile(_cueEmitDomain, mobData.hitCueProfile);
            }
        }

        // ① Speed Jitter — mobData 유무와 관계없이 항상 적용
        // mobData가 없으면 기본 배율(0.8~1.2) 사용
        float jitterMin = mobData != null ? mobData.speedJitterMin : 0.8f;
        float jitterMax = mobData != null ? mobData.speedJitterMax : 1.2f;
        moveSpeed *= Random.Range(jitterMin, jitterMax);

        // ② Target Offset — mobData 유무와 관계없이 항상 적용
        float offsetRadius = mobData != null ? mobData.targetOffsetRadius : 1.5f;
        _personalTargetOffset = Random.insideUnitCircle * offsetRadius;

        // 풀에서 꺼낼 때마다 초기화
        currentHealth = maxHealth;
        isDead = false;
        lastAttackTime = Time.time;
        _isInContactRange = false;

        // 체력바 — 최초 1회만 생성, 이후 재스폰 시 초기화만 수행
        if (mobData != null && mobData.healthBarPrefab != null)
        {
            if (healthBar == null)
            {
                var offset = Vector3.up * mobData.healthBarOffsetY;
                var hbGo = Instantiate(mobData.healthBarPrefab,
                                       transform.position + offset,
                                       Quaternion.identity,
                                       transform);
                healthBar = hbGo.GetComponent<BasicEnemyHealthBarUI>();
            }

            if (healthBar != null)
            {
                healthBar.SetHealthImmediate(1f);
                healthBar.HideHealthBar();
            }
        }

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
    /// <param name="aiUpdateInterval">Manager의 호출 주기(초) — 타이머 누적에 사용</param>
    public virtual void UpdateAI(float aiUpdateInterval)
    {
        if (isDead || playerTransform == null) return;

        Vector2 myPos       = transform.position;
        Vector2 playerPos   = playerTransform.position;
        float distToPlayer  = Vector2.Distance(myPos, playerPos);

        float stopRange  = mobData != null ? mobData.contactRange      : 1.0f;
        float deadzone   = mobData != null ? mobData.targetStopDistance : 0.15f;

        // ─────────────────────────────────────────────────
        // [1단계] 플레이어 contactRange 이내 → 완전 정지
        //   - rb.velocity 초기화로 관성 즉시 제거
        //   - IsInContactRange = true → Manager가 분리력 스킵
        // ─────────────────────────────────────────────────
        if (distToPlayer <= stopRange)
        {
            _isInContactRange = true;
            rb.velocity = Vector2.zero;

            // 스프라이트는 플레이어 방향 유지
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = (playerPos.x - myPos.x) < 0;
            }
            return;
        }

        // ─────────────────────────────────────────────────
        // [2단계] 개인 목표점 데드존 이내 → 미세 이동 중지
        //   - 목표 근처에서 세밀하게 왔다갔다 하는 진동 방지
        //   - IsInContactRange = false → 분리력은 계속 적용
        // ─────────────────────────────────────────────────
        _isInContactRange = false;

        Vector2 personalTarget  = playerPos + _personalTargetOffset;
        float   distToTarget    = Vector2.Distance(myPos, personalTarget);

        if (distToTarget <= deadzone)
        {
            rb.velocity = Vector2.zero;

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = (playerPos.x - myPos.x) < 0;
            }
            return;
        }

        // ─────────────────────────────────────────────────
        // [3단계] 일반 이동 — 개인 목표점을 향해 이동
        // ─────────────────────────────────────────────────
        Vector2 direction = (personalTarget - myPos).normalized;
        rb.velocity = direction * moveSpeed;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }
    
    /// <summary>
    /// 데미지 처리 (int 오버로드 — 공격자 방향 없음, 넉백 미적용)
    /// </summary>
    public void TakeDamage(int damage)
    {
        TakeDamage((float)damage);
    }

    /// <summary>
    /// 데미지 처리 (int + 공격자 Transform — 넉백 방향 계산용)
    /// DamageSource에서 호출 시 사용
    /// </summary>
    public void TakeDamage(int damage, Transform damageSource)
    {
        // 넉백: 데미지 처리 전에 적용해야 사망 시에도 튕기는 연출이 자연스러움
        float force = (mobData != null) ? mobData.knockbackForce : 0f;
        if (!isDead && force > 0f && rb != null && damageSource != null)
        {
            Vector2 dir = ((Vector2)transform.position - (Vector2)damageSource.position).normalized;
            rb.AddForce(dir * force, ForceMode2D.Impulse);
            StartCoroutine(ResetVelocityAfterKnockback());
        }

        TakeDamage((float)damage);
    }

    /// <summary>
    /// 데미지 처리 (float — 실제 연산 진입점)
    /// </summary>
    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= Mathf.RoundToInt(damage);
        
        // 데미지 숫자 표시
        ShowDamageNumber(damage);

        // 체력바 업데이트
        if (healthBar != null)
        {
            float ratio = (float)currentHealth / maxHealth;
            healthBar.UpdateHealthBar(ratio);
            healthBar.ShowAndAutoHide();
        }

        // 피격 피드백 — CueSystem (사운드 + VFX 통합)
        if (mobData != null && mobData.hitCueProfile != null
            && !string.IsNullOrEmpty(_cueEmitDomain)
            && !string.IsNullOrEmpty(mobData.hitCueEventKey))
        {
            var context = CueContext.From(transform, 1.0f);
            CueEmitter.Emit(mobData.hitCueEventKey, _cueEmitDomain, context);
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
    /// 넉백 후 속도 초기화 (AI 이동 재개를 위해)
    /// </summary>
    private IEnumerator ResetVelocityAfterKnockback()
    {
        yield return new WaitForSeconds(0.15f);
        if (!isDead && rb != null)
            rb.velocity = Vector2.zero;
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
        

        // 체력바 즉시 숨김
        if (healthBar != null)
            healthBar.HideHealthBar();

        // 애니메이션
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        
        // 풀로 반환 (애니메이션 후) — mobData.dieDelay 또는 기본값 0.5f
        float delay = (mobData != null) ? mobData.dieDelay : 0.5f;
        StartCoroutine(ReturnToPoolAfterDelay(delay));
    }
    
    /// <summary>
    /// 플레이어 공격 (접촉 데미지)
    /// </summary>
    protected virtual void AttackPlayer(Collider2D playerCollider)
    {
        PlayerHealth playerHealth = playerCollider.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            // hitPosition을 전달하는 오버로드를 사용해야 EmitHitEffect가 호출됨
            // PlayerHealth.canTakeDamage 플래그가 다수몹 동시 이펙트 난사를 자동 차단
            playerHealth.TakeDamage(Mathf.RoundToInt(contactDamage), transform, transform.position);
            lastAttackTime = Time.time;
            
            
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
    /// 분리력 적용 (SimpleMobManager에서 호출)
    /// 주변 몬스터로부터 받은 합산 분리 벡터를 rb에 Force로 가함
    /// </summary>
    public void ApplySeparationForce(Vector2 separationForce)
    {
        if (isDead || rb == null) return;
        rb.AddForce(separationForce, ForceMode2D.Force);
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
    
    /// <summary>
    /// 씬 레벨 기반 스탯 스케일링.
    /// WaveSpawner가 스폰 직후 호출한다.
    /// growthProfile이 없으면 mobData 기본값 그대로 사용.
    /// </summary>
    public void InitializeLevel(int stageBaseLevel)
    {
        if (mobData == null || mobData.growthProfile == null) return;

        int level = Mathf.Max(1, stageBaseLevel + mobData.levelOffset);

        float hpMult  = mobData.growthProfile.GetHealthMultiplier(level, EnemyType.Basic);
        float atkMult = mobData.growthProfile.GetAttackMultiplier(level, EnemyType.Basic);
        float spdMult = mobData.growthProfile.GetSpeedMultiplier(level, EnemyType.Basic);

        // maxHp · contactDamage는 SO 기준값에 배율 적용
        maxHealth     = Mathf.Max(1, Mathf.RoundToInt(mobData.maxHp * hpMult));
        currentHealth = maxHealth;
        contactDamage = mobData.collisionDamage * atkMult;

        // 속도는 OnEnable에서 이미 jitter가 적용된 moveSpeed에 성장 배율을 곱함
        moveSpeed *= spdMult;

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }

    #region ITargetable 구현 (자동 타겟팅 시스템)

    private TargetOutlineEffect _outlineEffect;

    bool ITargetable.IsAlive() => !isDead;
    EnemyRank ITargetable.GetRank() => EnemyRank.Normal;
    Transform ITargetable.GetTransform() => transform;

    void ITargetable.ActivateLockOn()
    {
        if (_outlineEffect == null) _outlineEffect = GetComponentInChildren<TargetOutlineEffect>();
        _outlineEffect?.Activate();
    }

    void ITargetable.DeactivateLockOn()
    {
        _outlineEffect?.Deactivate();
    }

    #endregion
}

