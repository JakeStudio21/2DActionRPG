using UnityEngine;

/// <summary>
/// 정지 사수형 SimpleMob
/// - 사거리(attackRange) 밖 : 플레이어 방향으로 직선 접근
/// - 사거리 안  : 이동 정지 후 fireRate 쿨다운으로 투사체 발사
/// - 모든 타이머는 Manager에서 전달받은 aiUpdateInterval 누적 (Update() 없음)
/// </summary>
public class SimpleMobShooter : SimpleMob
{
    // 발사 타이머 (aiUpdateInterval 누적)
    private float fireTimer = 0f;

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────

    protected override void OnEnable()
    {
        base.OnEnable();
        fireTimer = 0f;
    }

    // ─────────────────────────────────────────────
    // AI 업데이트 (SimpleMobManager에서 호출)
    // ─────────────────────────────────────────────

    public override void UpdateAI(float aiUpdateInterval)
    {
        if (isDead || playerTransform == null) return;
        if (mobData == null)
        {
            // mobData 없으면 기본 직선 이동으로 폴백
            base.UpdateAI(aiUpdateInterval);
            return;
        }

        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distToPlayer > mobData.attackRange)
        {
            // ── 접근 단계 ──────────────────────────
            Vector2 direction = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
            rb.velocity = direction * mobData.moveSpeed;

            if (spriteRenderer != null)
                spriteRenderer.flipX = direction.x < 0f;

            // 접근 중엔 타이머를 초기화해 사거리 진입 직후 바로 발사 가능하게 함
            fireTimer = 0f;
        }
        else
        {
            // ── 발사 단계 ──────────────────────────
            rb.velocity = Vector2.zero;

            // 플레이어 방향으로 스프라이트 전환
            if (spriteRenderer != null)
                spriteRenderer.flipX = (playerTransform.position.x - transform.position.x) < 0f;

            // 발사 쿨다운 누적
            fireTimer += aiUpdateInterval;
            if (fireTimer >= mobData.fireRate)
            {
                fireTimer = 0f;
                FireProjectile();
            }
        }
    }

    // ─────────────────────────────────────────────
    // 투사체 발사
    // ─────────────────────────────────────────────

    private void FireProjectile()
    {
        if (string.IsNullOrEmpty(mobData.projectilePoolTag))
        {
            if (enableDebugLogs)
                Debug.LogWarning("[SimpleMobShooter] projectilePoolTag가 비어있어 발사 스킵");
            return;
        }

        if (GamePoolManager.Instance == null) return;

        Vector2 direction = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        float   angle     = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 자신의 Collider와 즉시 충돌하지 않도록 발사 방향으로 0.5f 오프셋
        Vector3 spawnPos = transform.position + (Vector3)(direction * 0.5f);

        GameObject projectile = GamePoolManager.Instance.SpawnFromPool(
            mobData.projectilePoolTag, spawnPos, rotation);

        if (projectile == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[SimpleMobShooter] 투사체 스폰 실패 — 풀 태그 '{mobData.projectilePoolTag}'");
            return;
        }

        // SimpleMobProjectile 우선 사용 (적 전용 경량 투사체)
        // 없으면 기존 Projectile 컴포넌트로 폴백
        SimpleMobProjectile simpleProjComp = projectile.GetComponent<SimpleMobProjectile>();
        if (simpleProjComp != null)
        {
            simpleProjComp.Launch(direction);
        }
        else
        {
            Projectile proj = projectile.GetComponent<Projectile>();
            if (proj != null) proj.SetAsEnemyProjectile(true);
        }

        if (enableDebugLogs)
            Debug.Log($"[SimpleMobShooter] 투사체 발사 → {direction}");

        if (animator != null)
            animator.SetTrigger("Attack");
    }

    // ─────────────────────────────────────────────
    // 기즈모 (사거리 시각화)
    // ─────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (mobData == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, mobData.attackRange);
    }
}
