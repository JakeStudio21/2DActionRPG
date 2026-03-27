using UnityEngine;

/// <summary>
/// 적 전용 경량 투사체 (SimpleMobShooter / SimpleMobOrbitalShooter 사용)
/// - DamageSource 없이 단순 이동 + 플레이어 피격만 처리
/// - rb.velocity로 이동 (Update 이동 없음, 물리 기반)
/// - 풀링 지원: OnEnable에서 초기화, ReturnToPool로 반환
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SimpleMobProjectile : MonoBehaviour
{
    [Header("기본 설정")]
    [Tooltip("발사 속도 (Launch() 호출 시 speed > 0 이면 덮어씀)")]
    [SerializeField] private float moveSpeed = 8f;

    [Tooltip("최대 사거리 (초과 시 풀 반환)")]
    [SerializeField] private float maxRange = 12f;

    [Header("충돌 설정")]
    [Tooltip("벽 레이어 마스크 (Inspector에서 Wall 선택)")]
    [SerializeField] private LayerMask wallLayer;

    // 런타임 상태
    private Rigidbody2D rb;
    private Vector3 startPosition;
    private string poolTag;
    private bool isReturning = false;

    // ─────────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        poolTag = gameObject.tag;
    }

    private void OnEnable()
    {
        isReturning = false;
        rb.velocity = Vector2.zero;
        startPosition = transform.position;
        poolTag = gameObject.tag;
    }

    private void Update()
    {
        if (isReturning) return;

        // 최대 사거리 초과 시 풀 반환
        if (Vector3.Distance(transform.position, startPosition) > maxRange)
            ReturnToPool();
    }

    // ─────────────────────────────────────────────
    // 외부 초기화 (Shooter에서 스폰 직후 호출)
    // ─────────────────────────────────────────────

    /// <summary>
    /// 발사 방향과 선택적 파라미터 설정.
    /// SimpleMobShooter / SimpleMobOrbitalShooter에서 SpawnFromPool 직후 호출.
    /// </summary>
    /// <param name="direction">정규화된 발사 방향</param>
    /// <param name="speed">0이면 Inspector 기본값 사용</param>
    /// <param name="range">0이면 Inspector 기본값 사용</param>
    public void Launch(Vector2 direction, float speed = 0f, float range = 0f)
    {
        if (speed > 0f) moveSpeed = speed;
        if (range > 0f) maxRange = range;

        startPosition = transform.position;
        rb.velocity = direction * moveSpeed;
    }

    // ─────────────────────────────────────────────
    // 충돌 처리
    // ─────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isReturning) return;

        // 벽 충돌 → 즉시 반환
        if (wallLayer != 0 && ((1 << other.gameObject.layer) & wallLayer) != 0)
        {
            ReturnToPool();
            return;
        }

        // 플레이어 충돌 → 데미지 후 반환
        PlayerHealth player = other.GetComponent<PlayerHealth>();
        if (player != null)
        {
            EnemyDamage enemyDamage = GetComponent<EnemyDamage>();
            if (enemyDamage != null)
            {
                // SimpleMob.AttackPlayer와 동일한 2-파라미터 형식 사용
                player.TakeDamage(enemyDamage.damageAmount, transform);
            }
            ReturnToPool();
        }
    }

    // ─────────────────────────────────────────────
    // 풀 반환
    // ─────────────────────────────────────────────

    private void ReturnToPool()
    {
        if (isReturning) return;
        isReturning = true;
        rb.velocity = Vector2.zero;

        if (GamePoolManager.Instance != null && !string.IsNullOrEmpty(poolTag))
            GamePoolManager.Instance.ReturnToPool(poolTag, gameObject);
        else
            gameObject.SetActive(false);
    }
}
