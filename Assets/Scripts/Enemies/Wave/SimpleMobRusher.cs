using UnityEngine;
using System.Collections;

/// <summary>
/// 돌진형 SimpleMob
/// - 매우 빠른 속도로 돌진
/// - 돌진 중 데미지 증가
/// </summary>
public class SimpleMobRusher : SimpleMob
{
    [Header("돌진 설정")]
    [SerializeField] private float rushSpeed = 8f; // 기본 moveSpeed의 4배
    [SerializeField] private float rushDuration = 1.5f;
    [SerializeField] private float rushCooldown = 3f;
    [SerializeField] private float rushDamageMultiplier = 2f; // 돌진 중 데미지 2배
    
    [Header("돌진 이펙트")]
    [SerializeField] private Color rushColor = Color.cyan;
    [SerializeField] private ParticleSystem rushEffect;
    [SerializeField] private AudioClip rushSound;
    
    private bool isRushing = false;
    private float lastRushTime = 0f;
    private Vector2 rushDirection;
    
    protected override void OnEnable()
    {
        base.OnEnable();
        isRushing = false;
        lastRushTime = 0f;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
    }
    
    public override void UpdateAI(float aiUpdateInterval)
    {
        if (isDead || playerTransform == null) return;
        
        // 돌진 중이면 직진
        if (isRushing)
        {
            rb.velocity = rushDirection * rushSpeed;
            return;
        }
        
        // 돌진 쿨다운 체크
        if (Time.time >= lastRushTime + rushCooldown)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            
            // 일정 거리 이내면 돌진 시작
            if (distanceToPlayer <= detectionRange && distanceToPlayer >= 3f)
            {
                StartRush();
                return;
            }
        }
        
        // 기본 이동 (일반 속도)
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        rb.velocity = direction * moveSpeed;
        
        // 스프라이트 방향
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }
    
    /// <summary>
    /// 돌진 시작
    /// </summary>
    private void StartRush()
    {
        if (isRushing) return;
        
        isRushing = true;
        lastRushTime = Time.time;
        
        // 돌진 방향 설정 (플레이어 방향)
        rushDirection = (playerTransform.position - transform.position).normalized;
        
        
        // 돌진 애니메이션
        if (animator != null)
        {
            animator.SetTrigger("Rush");
        }
        
        // 돌진 이펙트
        if (rushEffect != null)
        {
            rushEffect.Play();
        }
        
        // 돌진 사운드
        if (rushSound != null)
        {
            AudioSource.PlayClipAtPoint(rushSound, transform.position, 0.7f);
        }
        
        // 색상 변경
        if (spriteRenderer != null)
        {
            spriteRenderer.color = rushColor;
        }
        
        // 돌진 종료 예약
        StartCoroutine(StopRushAfterDuration());
    }
    
    /// <summary>
    /// 돌진 종료
    /// </summary>
    private IEnumerator StopRushAfterDuration()
    {
        yield return new WaitForSeconds(rushDuration);
        StopRush();
    }
    
    private void StopRush()
    {
        if (!isRushing) return;
        
        isRushing = false;
        
        // 돌진 이펙트 정지
        if (rushEffect != null)
        {
            rushEffect.Stop();
        }
        
        // 색상 복구
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
        
    }
    
    /// <summary>
    /// 트리거 지속 체크 (플레이어 접촉 데미지 + 돌진 데미지 배율)
    /// </summary>
    protected override void OnTriggerStay2D(Collider2D collision)
    {
        if (isDead) return;
        
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            // 쿨다운 체크
            if (Time.time < lastAttackTime + attackCooldown) return;
            
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // 돌진 중이면 데미지 배율 적용
                float finalDamage = isRushing ? contactDamage * rushDamageMultiplier : contactDamage;
                
                playerHealth.TakeDamage(Mathf.RoundToInt(finalDamage), transform);
                lastAttackTime = Time.time;
                
                
                // 돌진 중 충돌 시 돌진 종료
                if (isRushing)
                {
                    StopRush();
                }
                
                // 공격 애니메이션
                if (animator != null)
                {
                    animator.SetTrigger("Attack");
                }
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // 탐지 범위 시각화
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // 돌진 방향 시각화
        if (isRushing)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, rushDirection * rushSpeed);
        }
    }
}

