using UnityEngine;

public class GhostProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float moveSpeed = 22f;
    [SerializeField] private float projectileRange = 10f;
    [SerializeField] private GameObject particleOnHitPrefabVFX;
    
    [Header("Damage")]
    [SerializeField] private LayerMask playerLayerMask = 1 << 3; // Player layer (Layer 3)
    [SerializeField] private int bulletDamage = 1;
    
    private Vector3 startPosition;
    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        circleCollider = GetComponent<CircleCollider2D>();
        
        // Rigidbody2D 설정
        if (rb != null)
        {
            rb.gravityScale = 0f;
        }
        
        // 콜라이더 설정
        if (circleCollider != null)
        {
            circleCollider.isTrigger = true;
        }
    }
    
    private void Start()
    {
        startPosition = transform.position;
    }
    
    private void Update()
    {
        MoveProjectile();
        DetectFireDistance();
    }
    
    public void UpdateProjectileRange(float newRange)
    {
        projectileRange = newRange;
    }
    
    public void UpdateMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
    
    public void SetDamage(int newDamage)
    {
        bulletDamage = newDamage;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어 Layer Mask 체크
        if (((1 << other.gameObject.layer) & playerLayerMask) != 0)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // ⚔️ CombatFormula 데미지 계산
                var playerStats = FindObjectOfType<PlayerRuntimeStats>();
                var enemyTarget = GetComponentInParent<IEnemyTarget>();
                var baseEnemy = GetComponentInParent<BaseEnemy>();
                
                var ctx = new CombatFormula.AttackContext
                {
                    baseAttack = bulletDamage,
                    attackerClass = null,
                    targetDefense = playerStats != null ? playerStats.FinalDefense : 0f,
                    targetTransform = other.transform,
                    attackerTransform = transform,
                    isSkillAttack = false,
                    skillMultiplier = 1.0f,
                    criticalChance = 0f,
                    criticalMultiplier = 2.0f,
                    isPlayerAttack = false,
                    attackerLevel = baseEnemy != null ? baseEnemy.CurrentLevel : 1,
                    
                    // ⚙️ Phase 4: ConditionalModifier용 필드
                    target = null, // 플레이어는 IEnemyTarget 아님
                    selfHpPercent = enemyTarget != null ? enemyTarget.GetCurrentHpPercent() : 1.0f,
                    targetHpPercent = playerHealth != null ? playerHealth.GetCurrentHpPercent() : 1.0f
                };
                
                var result = CombatFormula.CalculateEnemyToPlayerDamage(ctx);
                
                playerHealth.TakeDamage(result.finalDamage, transform);
                
                // ⚙️ Phase 4-C: 면역 체크 (플레이어가 상태이상 저항 가능)
                if (result.hasImmunity && !string.IsNullOrEmpty(result.resistedEffects))
                {
                    Debug.Log($"🛡️ [GhostProjectile] 플레이어 면역 발동! 저항한 효과: {result.resistedEffects}");
                    // 상태이상 부여 차단됨
                }
                
                Debug.Log($"[GhostProjectile] 플레이어에게 {result.finalDamage} 데미지를 입혔습니다.");
                
                OnHitEffect();
                DestroyProjectile();
                return;
            }
        }
        
        // 벽이나 장애물 충돌 처리
        Indestructible indestructible = other.GetComponent<Indestructible>();
        if (!other.isTrigger && indestructible)
        {
            OnHitEffect();
            DestroyProjectile();
        }
    }
    
    private void OnHitEffect()
    {
        if (particleOnHitPrefabVFX != null)
        {
            GamePoolManager.Instance.SpawnFromPool(particleOnHitPrefabVFX.name, transform.position, transform.rotation);
        }
        else
        {
            Debug.LogWarning($"[GhostProjectile] particleOnHitPrefabVFX가 할당되지 않음: {gameObject.name}");
        }
    }
    
    private void DestroyProjectile()
    {
        // ⭐ 핵심 수정: SetActive(false) 대신 ReturnToPool 사용
        if (GamePoolManager.Instance != null)
        {
            GamePoolManager.Instance.ReturnToPool("Ghost Bullet", gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    private void DetectFireDistance()
    {
        if (Vector3.Distance(transform.position, startPosition) > projectileRange)
        {
            DestroyProjectile();
        }
    }
    
    private void MoveProjectile()
    {
        transform.Translate(Vector3.right * Time.deltaTime * moveSpeed);
    }
    
    private void OnDrawGizmosSelected()
    {
        // 범위 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // 레이어 마스크 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
} 