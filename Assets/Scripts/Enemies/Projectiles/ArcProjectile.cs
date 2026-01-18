using System.Collections;
using UnityEngine;

/// <summary>
/// 포물선 발사체 - 몬스터 전용
/// AttackData 기반 자동 이펙트 재생 지원
/// </summary>
public class ArcProjectile : MonoBehaviour
{
    [Header("포물선 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float arcHeight = 3f;
    [SerializeField] private AnimationCurve trajectoryCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("데미지 설정")]
    [SerializeField] private LayerMask playerLayerMask = 1 << 3; // Player layer
    [SerializeField] private int projectileDamage = 10;
    
    [Header("이펙트")]
    [SerializeField] private GameObject hitEffectPrefab;
    
    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = false;
    
    // 궤도 계산용
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float traveledDistance = 0f;
    private float totalDistance;
    private bool isLaunched = false;
    
    // 풀링 관리
    private bool isReturningToPool = false;
    
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
        traveledDistance = 0f;
    }
    
    private void OnDisable()
    {
        StopAllCoroutines();
        isLaunched = false;
        isReturningToPool = false;
    }
    
    /// <summary>
    /// 발사체 발사 (목표 위치로)
    /// </summary>
    public void LaunchToTarget(Vector3 target)
    {
        if (isReturningToPool) return;
        
        startPosition = transform.position;
        targetPosition = target;
        totalDistance = Vector3.Distance(startPosition, targetPosition);
        isLaunched = true;
        
        if (showDebugLogs)
            Debug.Log($"🎯 [ArcProjectile] 발사! Start: {startPosition}, Target: {targetPosition}, Distance: {totalDistance:F2}");
    }
    
    /// <summary>
    /// 데미지 설정
    /// </summary>
    public void SetDamage(int damage)
    {
        projectileDamage = damage;
    }
    
    /// <summary>
    /// 이동 속도 설정
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }
    
    /// <summary>
    /// 포물선 높이 설정
    /// </summary>
    public void SetArcHeight(float height)
    {
        arcHeight = height;
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
        
        MoveInArc();
    }
    
    /// <summary>
    /// 포물선 이동
    /// </summary>
    private void MoveInArc()
    {
        // 거리 업데이트
        float deltaDistance = moveSpeed * Time.deltaTime;
        traveledDistance += deltaDistance;
        
        // 진행률 계산 (0 ~ 1)
        float progress = Mathf.Clamp01(traveledDistance / totalDistance);
        
        // 직선 보간으로 기본 위치 계산
        Vector3 linearPosition = Vector3.Lerp(startPosition, targetPosition, progress);
        
        // Y축 오프셋 계산 (포물선 곡선)
        float heightOffset = arcHeight * trajectoryCurve.Evaluate(progress);
        
        // 최종 위치 설정
        transform.position = linearPosition + Vector3.up * heightOffset;
        
        // 착탄 판정
        if (progress >= 1.0f)
        {
            OnProjectileLand();
            return;
        }
        
        // 포물선 방향으로 회전
        if (progress < 1f)
        {
            Vector3 nextPos = Vector3.Lerp(startPosition, targetPosition, progress + 0.01f);
            float nextHeightOffset = arcHeight * trajectoryCurve.Evaluate(progress + 0.01f);
            Vector3 nextPosition = nextPos + Vector3.up * nextHeightOffset;
            
            Vector3 direction = (nextPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }
    
    /// <summary>
    /// 착지 처리
    /// </summary>
    private void OnProjectileLand()
    {
        if (isReturningToPool) return;
        
        // 착지 위치 정확히 설정
        transform.position = targetPosition;
        
        // 데미지 적용
        DealDamageToPlayer();
        
        // Hit 이펙트 재생
        PlayHitEffect();
        
        // 풀 반환
        ReturnToPool();
    }
    
    /// <summary>
    /// 플레이어에게 데미지 적용
    /// </summary>
    private void DealDamageToPlayer()
    {
        Collider2D[] playersInRange = Physics2D.OverlapCircleAll(transform.position, 1.5f, playerLayerMask);
        
        foreach (Collider2D playerCollider in playersInRange)
        {
            if (playerCollider.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(projectileDamage, transform);
                
                if (showDebugLogs)
                    Debug.Log($"💥 [ArcProjectile] 플레이어에게 {projectileDamage} 데미지!");
                
                break; // 한 명만 데미지
            }
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
                    Quaternion.identity);
                
                if (showDebugLogs)
                    Debug.Log($"🎨 [ArcProjectile] Hit 이펙트 재생: {hitEffectPrefab.name}");
            }
            else
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }
        }
    }
    
    /// <summary>
    /// 충돌 감지 (중간 충돌)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isReturningToPool) return;
        
        // 🧱 벽 충돌 감지 (최우선 - Wall Layer 기반)
        int wallLayerIndex = LayerMask.NameToLayer("Wall");
        
        if (wallLayerIndex != -1 && other.gameObject.layer == wallLayerIndex)
        {
            if (showDebugLogs)
                Debug.Log($"🧱 [ArcProjectile] 벽 충돌! {gameObject.name} → {other.name}");
            
            // 즉시 착지 처리 (벽 앞에서 폭발)
            targetPosition = transform.position;
            OnProjectileLand();
            return;
        }
        
        // 플레이어와 충돌 시 즉시 착지
        if ((playerLayerMask.value & (1 << other.gameObject.layer)) > 0)
        {
            if (!isLaunched) return;
            
            targetPosition = transform.position;
            OnProjectileLand();
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
            GamePoolManager.Instance.ReturnToPool("ArcProjectile", gameObject);
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
            // 발사 경로 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPosition, targetPosition);
            
            // 목표 지점 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 1f);
            
            // 포물선 경로 표시
            Gizmos.color = Color.cyan;
            Vector3 prevPos = startPosition;
            for (float i = 0; i <= 1f; i += 0.1f)
            {
                Vector3 linearPos = Vector3.Lerp(startPosition, targetPosition, i);
                float heightOffset = arcHeight * trajectoryCurve.Evaluate(i);
                Vector3 arcPos = linearPos + Vector3.up * heightOffset;
                
                Gizmos.DrawLine(prevPos, arcPos);
                prevPos = arcPos;
            }
        }
    }
}

