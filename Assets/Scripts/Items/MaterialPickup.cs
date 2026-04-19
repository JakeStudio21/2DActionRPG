using UnityEngine;

/// <summary>
/// 재료 아이템 픽업 오브젝트
/// - 몬스터 처치 시 드롭
/// - 플레이어 충돌 시 자동 획득
/// - 오브젝트 풀링 지원
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class MaterialPickup : MonoBehaviour, IPoolableObject
{
    [Header("📦 재료 설정")]
    [SerializeField] private MaterialType materialType;
    [SerializeField] private int amount = 1;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("🧲 자석 설정")]
    [SerializeField] private float pickUpDistance = 5f;
    [SerializeField] private float accelerationRate = 0.2f;
    [SerializeField] private float moveSpeed = 3f;

    [Header("🎨 시각 효과")]
    [SerializeField] private float popSpreadRadius = 2.5f;
    [SerializeField] private float bounceHeight = 1f;
    [SerializeField] private float bounceDuration = 0.5f;
    [SerializeField] private AnimationCurve bounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("⏱️ 자동 수집")]
    [SerializeField] private float autoCollectDelay = 0.5f;

    [Header("📊 디버그")]

    // 컴포넌트
    private Rigidbody2D rb;
    private Transform player;

    // 이동 상태
    private Vector3 moveDirection;
    private float currentMoveSpeed;
    private bool isPopping = false;

    // 내부 상태
    private float spawnTime;
    private bool isCollected = false;

    #region Unity Lifecycle

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.drag = 0f;

        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (player == null || isPopping) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer < pickUpDistance)
        {
            moveDirection = (player.position - transform.position).normalized;
            currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, moveSpeed, accelerationRate);
        }
        else
        {
            currentMoveSpeed = 0f;
            moveDirection = Vector3.zero;
            rb.velocity = Vector2.zero;
        }
    }

    private void FixedUpdate()
    {
        if (rb != null && currentMoveSpeed > 0f)
        {
            rb.velocity = moveDirection * currentMoveSpeed;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < spawnTime + autoCollectDelay)
            return;

        if (isCollected)
            return;

        if (other.CompareTag("Player"))
        {
            CollectMaterial();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 재료 픽업 초기화 (오브젝트 풀에서 스폰 시 호출)
    /// </summary>
    public void Initialize(MaterialType type, int count, Vector3 position)
    {
        materialType = type;
        amount = count;
        transform.position = position;
        spawnTime = Time.time;
        isCollected = false;

        moveDirection = Vector3.zero;
        currentMoveSpeed = 0f;

        UpdateIcon();
        StartCoroutine(PopAnimationRoutine(position));

    }

    #endregion

    #region IPoolableObject Implementation

    public void OnSpawnFromPool()
    {
        gameObject.SetActive(true);
        isCollected = false;

    }

    public void OnReturnToPool()
    {
        StopAllCoroutines();
        isCollected = false;
        isPopping = false;
        moveDirection = Vector3.zero;
        currentMoveSpeed = 0f;

        if (rb != null)
            rb.velocity = Vector2.zero;

        gameObject.SetActive(false);

    }

    #endregion

    #region Private Methods

    private void FindPlayer()
    {
        if (player != null) return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    /// <summary>
    /// 재료 아이콘 업데이트
    /// </summary>
    private void UpdateIcon()
    {
        var materialData = MaterialDatabase.Instance?.GetData(materialType);
        if (materialData != null && materialData.icon != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = materialData.icon;
            spriteRenderer.color = Color.white;
            return;
        }

        if (spriteRenderer != null)
        {
            Color iconColor = materialType.GetMaterialGrade() switch
            {
                "파편" => new Color(0.7f, 0.7f, 0.7f),
                "결정" => new Color(0.3f, 0.9f, 0.3f),
                "코어" => new Color(0.3f, 0.6f, 1f),
                _ => Color.white
            };
            spriteRenderer.color = iconColor;

        }
    }

    /// <summary>
    /// 드롭 팝 애니메이션 (랜덤 위치로 튀어오르며 퍼짐)
    /// </summary>
    private System.Collections.IEnumerator PopAnimationRoutine(Vector3 startPos)
    {
        isPopping = true;

        Vector2 randomOffset = Random.insideUnitCircle * popSpreadRadius;
        Vector3 endPos = startPos + new Vector3(randomOffset.x, randomOffset.y, 0f);

        float elapsed = 0f;

        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bounceDuration;
            float heightOffset = bounceCurve.Evaluate(t) * bounceHeight;

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            currentPos.y += heightOffset;
            transform.position = currentPos;

            yield return null;
        }

        transform.position = endPos;
        isPopping = false;
    }

    /// <summary>
    /// 재료 수집
    /// </summary>
    private void CollectMaterial()
    {
        if (isCollected)
            return;

        isCollected = true;

        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AddMaterialToCharacterBag(materialType, amount);
            Dbg.Log($"[MaterialPickup] 재료 획득: {materialType} x{amount}");
        }
        else
        {
            Debug.LogError("❌ [MaterialPickup] PlayerDataManager가 초기화되지 않았습니다!");
        }

        ReturnToPool();
    }

    /// <summary>
    /// 오브젝트 풀로 반환
    /// </summary>
    public void ReturnToPool()
    {
        if (GamePoolManager.Instance != null)
        {
            GamePoolManager.Instance.ReturnToPool("Drop_Material", gameObject);
        }
        else
        {
            Debug.LogError("[MaterialPickup] GamePoolManager.Instance가 null입니다!");
            Destroy(gameObject);
        }
    }

    #endregion
}
