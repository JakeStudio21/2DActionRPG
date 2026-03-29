using UnityEngine;
using System.Collections;
using CueSystem;

/// <summary>
/// 자폭형 SimpleMob
/// - 플레이어 근접 시 자폭
/// - 범위 데미지
/// </summary>
public class SimpleMobExploder : SimpleMob
{
    [Header("자폭 설정")]
    [Tooltip("폭발 범위 반경")]
    [SerializeField] private float explosionRadius = 3f;

    [Tooltip("폭발 시 가하는 데미지")]
    [SerializeField] private float explosionDamage = 20f;

    [Tooltip("이 거리 이내로 진입하면 자폭 시작")]
    [SerializeField] private float explosionTriggerDistance = 1.5f;

    [Tooltip("자폭 대기 시간(초). Explode 애니메이션 클립 길이와 맞추세요.\n" +
             "mobData.dieDelay와는 별개로 폭발 연출 전 딜레이입니다.")]
    [SerializeField] private float explosionDelay = 0.5f;
    
    [Header("자폭 연출")]
    [SerializeField] private Color glowColor = Color.red;

    private bool isExploding = false;
    private string _explodeCueEmitDomain;

    protected override void Awake()
    {
        base.Awake();
    }
    
    protected override void OnEnable()
    {
        base.OnEnable();
        isExploding = false;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        // 자폭 CueProfile 등록 — CueRegistry 중복 방어로 반복 호출 안전
        if (mobData != null && mobData.explodeCueProfile != null)
        {
            _explodeCueEmitDomain = mobData.explodeCueProfile.profileId;
            CueRegistry.Instance.RegisterProfile(_explodeCueEmitDomain, mobData.explodeCueProfile);
        }
    }
    
    public override void UpdateAI(float aiUpdateInterval)
    {
        if (isDead || isExploding || playerTransform == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        
        // 자폭 거리 도달 시
        if (distanceToPlayer <= explosionTriggerDistance)
        {
            StartExplosion();
            return;
        }
        
        // 기본 이동 (더 빠르게)
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        rb.velocity = direction * (moveSpeed * 1.5f); // 1.5배 빠름
        
        // 스프라이트 방향
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0;
            
            // 거리에 따라 색상 변화 (가까울수록 빨강)
            float colorIntensity = Mathf.Clamp01(1f - (distanceToPlayer / explosionTriggerDistance));
            spriteRenderer.color = Color.Lerp(Color.white, glowColor, colorIntensity * 0.5f);
        }
    }
    
    /// <summary>
    /// 자폭 시작
    /// </summary>
    private void StartExplosion()
    {
        if (isExploding) return;
        
        isExploding = true;
        rb.velocity = Vector2.zero;
        
        if (enableDebugLogs)
            Debug.Log($"[SimpleMobExploder] {gameObject.name} 자폭 시작!");
        
        // 자폭 애니메이션
        if (animator != null)
        {
            animator.SetTrigger("Explode");
        }
        
        StartCoroutine(ExplodeAfterDelay());
    }
    
    /// <summary>
    /// 딜레이 후 폭발
    /// </summary>
    private IEnumerator ExplodeAfterDelay()
    {
        // 점멸 효과
        if (spriteRenderer != null)
        {
            for (int i = 0; i < 5; i++)
            {
                spriteRenderer.color = glowColor;
                yield return new WaitForSeconds(explosionDelay / 10f);
                spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(explosionDelay / 10f);
            }
        }
        else
        {
            yield return new WaitForSeconds(explosionDelay);
        }
        
        Explode();
    }
    
    /// <summary>
    /// 폭발 처리
    /// </summary>
    private void Explode()
    {
        if (enableDebugLogs)
            Debug.Log($"[SimpleMobExploder] 💥 폭발! 반경: {explosionRadius}");
        
        // 범위 내 플레이어 데미지
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(Mathf.RoundToInt(explosionDamage), transform);
                    
                    if (enableDebugLogs)
                        Debug.Log($"[SimpleMobExploder] 플레이어에게 폭발 데미지: {explosionDamage}");
                }
            }
        }
        
        // 자폭 피드백 — CueSystem (사운드 + VFX 통합)
        if (mobData != null && mobData.explodeCueProfile != null
            && !string.IsNullOrEmpty(_explodeCueEmitDomain)
            && !string.IsNullOrEmpty(mobData.explodeCueEventKey))
        {
            var context = CueContext.From(transform, 1.0f);
            CueEmitter.Emit(mobData.explodeCueEventKey, _explodeCueEmitDomain, context);
        }

        // 자폭 후 즉시 풀로 반환
        isDead = true;
        StartCoroutine(ReturnToPoolAfterDelay(0.1f));
    }
    
    /// <summary>
    /// 트리거 진입 — 박치기 공격 대신 자폭 발동
    /// </summary>
    protected override void OnTriggerStay2D(Collider2D collision)
    {
        if (isDead || isExploding) return;

        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            StartExplosion();
        }
    }

    /// <summary>
    /// 트리거 진입 (백업 폭발 트리거)
    /// </summary>
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead || isExploding) return;
        
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            StartExplosion();
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // 폭발 범위 시각화
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        
        // 자폭 트리거 거리
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionTriggerDistance);
    }
}

