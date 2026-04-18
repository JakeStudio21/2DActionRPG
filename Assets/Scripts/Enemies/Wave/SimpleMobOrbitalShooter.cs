using UnityEngine;

/// <summary>
/// 궤도형 사수 SimpleMob
/// - 플레이어 주위를 공전하며 투사체 발사
/// - rb.velocity + 거리 보정(구심력)으로 안정적인 원형 궤도 유지
/// - 모든 타이머 연산은 Manager에서 전달받은 aiUpdateInterval로만 처리 (Update() 없음)
/// </summary>
public class SimpleMobOrbitalShooter : SimpleMob
{
    // 발사 타이머 (aiUpdateInterval 누적)
    private float fireTimer = 0f;

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────

    protected override void OnEnable()
    {
        base.OnEnable();

        // 발사 타이머 초기화
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

        Vector2 playerPos = playerTransform.position;
        Vector2 myPos     = transform.position;

        UpdateOrbitMovement(playerPos, myPos);
        UpdateFireTimer(playerPos, myPos, aiUpdateInterval);
        UpdateSpriteDirection(playerPos, myPos);
    }

    // ─────────────────────────────────────────────
    // 공전 이동 (접선 벡터 + 구심력 보정)
    // ─────────────────────────────────────────────

    private void UpdateOrbitMovement(Vector2 playerPos, Vector2 myPos)
    {
        Vector2 toPlayer      = playerPos - myPos;
        float   currentDist   = toPlayer.magnitude;

        // 거리가 0에 수렴하면 연산 중단 (ZeroDivision 방지)
        if (currentDist < 0.01f) return;

        Vector2 dirToPlayer = toPlayer / currentDist;

        // 1. 접선 방향 (시계 방향 공전)
        Vector2 tangent = new Vector2(-dirToPlayer.y, dirToPlayer.x);

        // 2. 거리 보정 (구심력)
        //    멀면 플레이어 쪽으로 당기고, 가까우면 밀어내 orbitRadius를 유지
        float   distanceError      = currentDist - mobData.orbitRadius;
        const float correctionStrength = 2f;
        Vector2 correction         = dirToPlayer * (distanceError * correctionStrength);

        // 3. 최종 속도 = (접선 + 보정) 정규화 후 moveSpeed 적용
        rb.velocity = (tangent + correction).normalized * mobData.moveSpeed;
    }

    // ─────────────────────────────────────────────
    // 발사 타이머 및 투사체 스폰
    // ─────────────────────────────────────────────

    private void UpdateFireTimer(Vector2 playerPos, Vector2 myPos, float aiUpdateInterval)
    {
        fireTimer += aiUpdateInterval;

        if (fireTimer < mobData.fireRate) return;

        fireTimer = 0f;
        FireProjectile(playerPos, myPos);
    }

    private void FireProjectile(Vector2 playerPos, Vector2 myPos)
    {
        if (string.IsNullOrEmpty(mobData.projectilePoolTag))
        {
                Debug.LogWarning("[SimpleMobOrbitalShooter] projectilePoolTag가 비어있어 발사 스킵");
            return;
        }

        if (GamePoolManager.Instance == null) return;

        // 플레이어 방향 회전 계산
        Vector2 direction = (playerPos - myPos).normalized;
        float   angle     = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // 자신의 Collider와 즉시 충돌하지 않도록 발사 방향으로 0.5f 오프셋
        Vector3 spawnPos = transform.position + (Vector3)(direction * 0.5f);

        GameObject projectile = GamePoolManager.Instance.SpawnFromPool(
            mobData.projectilePoolTag, spawnPos, rotation);

        if (projectile == null)
        {
                Debug.LogWarning($"[SimpleMobOrbitalShooter] 투사체 스폰 실패 — 풀 태그 '{mobData.projectilePoolTag}'");
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


        // 공격 애니메이션 트리거
        if (animator != null)
            animator.SetTrigger("Attack");
    }

    // ─────────────────────────────────────────────
    // 스프라이트 방향 (플레이어 기준)
    // ─────────────────────────────────────────────

    private void UpdateSpriteDirection(Vector2 playerPos, Vector2 myPos)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = (playerPos.x - myPos.x) < 0f;
    }

    // ─────────────────────────────────────────────
    // 기즈모 (궤도 반경 시각화)
    // ─────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (mobData == null || playerTransform == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerTransform.position, mobData.orbitRadius);
    }
}
