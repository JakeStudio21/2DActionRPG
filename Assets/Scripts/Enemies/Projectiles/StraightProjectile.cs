using UnityEngine;

/// <summary>
/// 직선 발사체 - 몬스터 전용
/// AttackData 기반 자동 이펙트 재생 지원
/// </summary>
public class StraightProjectile : MonoBehaviour
{
    [Header("직선 이동 설정")]
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float projectileRange = 10f;
    
    [Header("데미지 설정")]
    [SerializeField] private LayerMask playerLayerMask = 1 << 3; // Player layer
    [SerializeField] private int projectileDamage = 10;
    
    [Header("이펙트")]
    [SerializeField] private GameObject hitEffectPrefab;
    
    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = false;
    
    // 이동 관리
    private Vector3 startPosition;
    private Vector2 fireDirection = Vector2.right;
    private bool isLaunched = false;
    
    // 풀링 관리
    private bool isReturningToPool = false;
    
    // 🛡️ Phase 1: 상태이상 적용용
    private BaseAttackBehaviour attacker = null;
    
    // 컴포넌트
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
            rb.isKinematic = true;
        }
        
        // 콜라이더 설정
        if (circleCollider != null)
        {
            circleCollider.isTrigger = true;
        }
    }
    
    private void OnEnable()
    {
        isReturningToPool = false;
        isLaunched = false;
        startPosition = transform.position;
    }
    
    private void OnDisable()
    {
        isLaunched = false;
        isReturningToPool = false;
    }
    
    /// <summary>
    /// 발사 방향 설정
    /// </summary>
    public void SetDirection(Vector2 direction)
    {
        fireDirection = direction.normalized;
        isLaunched = true;
        startPosition = transform.position;
        
        // ✅ 아이소메트릭: 회전만 사용 (미러링 제거)
        // 미러링은 이펙트/자식 오브젝트의 방향을 망가뜨림
        float angle = Mathf.Atan2(fireDirection.y, fireDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        
        if (showDebugLogs)
            Debug.Log($"🎯 [StraightProjectile] 발사! 방향: {fireDirection}, 각도: {angle:F1}도");
    }
    
    /// <summary>
    /// 데미지 설정
    /// </summary>
    public void SetDamage(int damage)
    {
        projectileDamage = damage;
    }
    
    /// <summary>
    /// 🛡️ Phase 1: 공격자 설정 (상태이상 적용용)
    /// </summary>
    public void SetAttacker(BaseAttackBehaviour attackerBehaviour)
    {
        attacker = attackerBehaviour;
        
        if (showDebugLogs && attacker != null)
            Debug.Log($"🎯 [StraightProjectile] 공격자 설정: {attacker.gameObject.name}");
    }
    
    /// <summary>
    /// 이동 속도 설정
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    /// <summary>
    /// 사거리 설정
    /// </summary>
    public void SetProjectileRange(float range)
    {
        projectileRange = range;
    }
    
    /// <summary>
    /// Hit 이펙트 설정 (AttackData에서 자동 설정)
    /// </summary>
    public void SetHitEffect(GameObject effect)
    {
        hitEffectPrefab = effect;
    }
    
    private void Update()
    {
        if (!isLaunched || isReturningToPool) return;
        
        MoveProjectile();
        DetectFireDistance();
    }
    
    /// <summary>
    /// 직선 이동
    /// </summary>
    private void MoveProjectile()
    {
        transform.Translate(fireDirection * Time.deltaTime * moveSpeed, Space.World);
    }
    
    /// <summary>
    /// 사거리 체크
    /// </summary>
    private void DetectFireDistance()
    {
        float currentDistance = Vector3.Distance(transform.position, startPosition);
        
        if (currentDistance > projectileRange)
        {
            if (showDebugLogs)
                Debug.Log($"📏 [StraightProjectile] 사거리 초과: {currentDistance:F2} > {projectileRange:F2}");
            
            ReturnToPool();
        }
    }
    
    /// <summary>
    /// 충돌 감지
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isReturningToPool) return;
        
        // 🧱 벽 충돌 감지 (최우선 - Wall Layer 기반)
        int wallLayerIndex = LayerMask.NameToLayer("Wall");
        
        if (wallLayerIndex != -1 && other.gameObject.layer == wallLayerIndex)
        {
            if (showDebugLogs)
                Debug.Log($"🧱 [StraightProjectile] 벽 충돌! {gameObject.name} → {other.name}");
            
            // 🎵 CueSystem: 벽 충돌 이펙트 + 사운드 재생
            Vector3 hitPosition = transform.position;
            Vector3 hitNormal = (hitPosition - other.transform.position).normalized;
            
            var context = new CueSystem.CueContext
            {
                position = hitPosition,
                rotation = transform.rotation,
                normal = hitNormal,
                facingDir = transform.right,
                follow = null,
                actorType = CueSystem.ActorType.Enemy,
                surfaceType = CueSystem.SurfaceType.Stone,
                magnitude = 1.0f,
                isCritical = false,
                scale = 1.0f
            };
            
            CueSystem.CueEmitter.Emit("projectile.hit.wall", "Enemy", context);
            
            // 기존 이펙트도 재생 (Fallback)
            PlayHitEffect();
            ReturnToPool();
            return;
        }
        
        // 플레이어 충돌
        if ((playerLayerMask.value & (1 << other.gameObject.layer)) > 0)
        {
            if (other.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(projectileDamage, transform);
                
                // 🛡️ Phase 1: 상태이상 적용 (저항 시스템 적용됨)
                if (attacker != null)
                {
                    attacker.ApplyStatusEffects(playerHealth, other.transform);
                    
                    if (showDebugLogs)
                        Debug.Log($"☠️ [StraightProjectile] 상태이상 적용 시도 (공격자: {attacker.gameObject.name})");
                }
                
                if (showDebugLogs)
                    Debug.Log($"💥 [StraightProjectile] 플레이어에게 {projectileDamage} 데미지!");
                
                PlayHitEffect();
                ReturnToPool();
                return;
            }
        }
        
        // 벽/장애물 충돌 (Indestructible 기반 - 하위 호환성)
        Indestructible indestructible = other.GetComponent<Indestructible>();
        if (!other.isTrigger && indestructible)
        {
            if (showDebugLogs)
                Debug.Log($"🧱 [StraightProjectile] Indestructible 장애물 충돌: {other.gameObject.name}");
            
            PlayHitEffect();
            ReturnToPool();
        }
    }
    
    /// <summary>
    /// Hit 이펙트 재생
    /// </summary>
    private void PlayHitEffect()
    {
        if (hitEffectPrefab != null)
        {
            if (GamePoolManager.Instance != null)
            {
                GameObject effect = GamePoolManager.Instance.SpawnFromPool(
                    hitEffectPrefab.name, 
                    transform.position, 
                    transform.rotation);
                
                if (showDebugLogs)
                    Debug.Log($"🎨 [StraightProjectile] Hit 이펙트 재생: {hitEffectPrefab.name}");
            }
            else
            {
                Instantiate(hitEffectPrefab, transform.position, transform.rotation);
            }
        }
    }
    
    /// <summary>
    /// 풀 반환
    /// </summary>
    private void ReturnToPool()
    {
        if (isReturningToPool) return;
        
        isReturningToPool = true;
        
        if (GamePoolManager.Instance != null)
        {
            GamePoolManager.Instance.ReturnToPool("StraightProjectile", gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 디버그용 Gizmo
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (isLaunched)
        {
            // 현재 위치
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            
            // 발사 방향 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)fireDirection * 2f);
            
            // 사거리 표시
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(startPosition, projectileRange);
        }
    }
}

