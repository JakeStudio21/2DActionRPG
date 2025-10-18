using UnityEngine;

public class GolemProjectile : MonoBehaviour
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

    // 👉 추가: 발사 방향 (오른쪽 기본)
    private Vector2 fireDirection = Vector2.right;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        circleCollider = GetComponent<CircleCollider2D>();

        // Rigidbody2D 설정
        if (rb != null)
            rb.gravityScale = 0f;

        // 콜라이더 설정
        if (circleCollider != null)
            circleCollider.isTrigger = true;
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

    // ✅ 방향 설정 함수
    public void SetDirection(Vector2 dir)
    {
        fireDirection = dir.normalized;

        // 좌/우 반전
        Vector3 scale = transform.localScale;
        scale.x = (fireDirection.x < 0) ? Mathf.Abs(scale.x) * -1 : Mathf.Abs(scale.x);
        transform.localScale = scale;
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
        // 플레이어 충돌
        if (((1 << other.gameObject.layer) & playerLayerMask) != 0)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(bulletDamage, transform);
                Debug.Log($"[GolemProjectile] 플레이어에게 {bulletDamage} 데미지를 입혔습니다.");

                OnHitEffect();
                DestroyProjectile();
                return;
            }
        }

        // 벽/장애물 충돌
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
            GamePoolManager.Instance.SpawnFromPool(
                particleOnHitPrefabVFX.name,
                transform.position,
                transform.rotation
            );
        }
        else
        {
            Debug.LogWarning($"[GolemProjectile] particleOnHitPrefabVFX가 할당되지 않음: {gameObject.name}");
        }
    }

    private void DestroyProjectile()
    {
        // 풀 매니저 대응
        if (GamePoolManager.Instance != null)
        {
            GamePoolManager.Instance.ReturnToPool("Golem Bullet", gameObject);
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

    // ✅ 수정된 이동 로직
    private void MoveProjectile()
    {
        transform.Translate(fireDirection * Time.deltaTime * moveSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
