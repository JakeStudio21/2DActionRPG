using UnityEngine;

/// <summary>
/// 재료 아이템 픽업 오브젝트
/// - 몬스터 처치 시 드롭
/// - 플레이어 충돌 시 자동 획득
/// - 오브젝트 풀링 지원
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class MaterialPickup : MonoBehaviour, IPoolableObject
{
    [Header("📦 재료 설정")]
    [SerializeField] private MaterialType materialType;
    [SerializeField] private int amount = 1;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("🎨 시각 효과")]
    [SerializeField] private float bounceHeight = 1f;
    [SerializeField] private float bounceDuration = 0.5f;
    [SerializeField] private AnimationCurve bounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("⏱️ 자동 수집")]
    [SerializeField] private float autoCollectDelay = 0.5f; // 드롭 후 자동 수집 가능까지 대기 시간
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = false;
    
    // 내부 상태
    private float spawnTime;
    private Vector3 startPosition;
    private bool isCollected = false;
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 드롭 직후에는 수집 불가 (바운스 애니메이션 중)
        if (Time.time < spawnTime + autoCollectDelay)
            return;
        
        // 이미 수집됨
        if (isCollected)
            return;
        
        // 플레이어만 수집 가능
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
        startPosition = position;
        spawnTime = Time.time;
        isCollected = false;
        
        // 아이콘 설정
        UpdateIcon();
        
        // 바운스 애니메이션 시작
        StartCoroutine(BounceAnimation());
        
        if (showDebugLogs)
            Debug.Log($"📦 [MaterialPickup] 스폰: {materialType.GetDisplayName()} x{amount} at {position}");
    }
    
    #endregion
    
    #region IPoolableObject Implementation
    
    public void OnSpawnFromPool()
    {
        gameObject.SetActive(true);
        isCollected = false;
        
        if (showDebugLogs)
            Debug.Log($"📦 [MaterialPickup] 풀에서 스폰: {materialType}");
    }
    
    public void OnReturnToPool()
    {
        StopAllCoroutines();
        isCollected = false;
        gameObject.SetActive(false);
        
        if (showDebugLogs)
            Debug.Log($"📦 [MaterialPickup] 풀로 반환: {materialType}");
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// 재료 아이콘 업데이트
    /// </summary>
    private void UpdateIcon()
    {
        var materialData = MaterialDatabase.Instance.GetData(materialType);
        if (materialData != null && materialData.icon != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = materialData.icon;
            spriteRenderer.color = Color.white;
        }
        else
        {
            // 기본 아이콘 (색상으로 구분)
            if (spriteRenderer != null)
            {
                Color iconColor = materialType.GetMaterialGrade() switch
                {
                    "파편" => new Color(0.7f, 0.7f, 0.7f), // 회색 (Common)
                    "결정" => new Color(0.3f, 0.9f, 0.3f), // 초록 (Uncommon)
                    "코어" => new Color(0.3f, 0.6f, 1f),   // 파랑 (Rare)
                    _ => Color.white
                };
                spriteRenderer.color = iconColor;
            }
        }
    }
    
    /// <summary>
    /// 바운스 애니메이션
    /// </summary>
    private System.Collections.IEnumerator BounceAnimation()
    {
        float elapsed = 0f;
        
        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bounceDuration;
            float bounce = bounceCurve.Evaluate(t) * bounceHeight;
            
            transform.position = startPosition + Vector3.up * bounce;
            
            yield return null;
        }
        
        // 최종 위치로 복구
        transform.position = startPosition;
    }
    
    /// <summary>
    /// 재료 수집
    /// </summary>
    private void CollectMaterial()
    {
        if (isCollected)
            return;
        
        isCollected = true;
        
        // ⭐ 캐릭터 가방에 재료 추가 (인게임 임시 저장)
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AddMaterialToCharacterBag(materialType, amount);
            
            if (showDebugLogs)
                Debug.Log($"✅ [MaterialPickup] 재료 획득 → 캐릭터 가방: {materialType.GetDisplayName()} x{amount}");
        }
        else
        {
            Debug.LogError("❌ [MaterialPickup] PlayerDataManager가 초기화되지 않았습니다!");
        }
        
        // 풀로 반환
        ReturnToPool();
    }
    
    /// <summary>
    /// 오브젝트 풀로 반환
    /// </summary>
    public void ReturnToPool()
    {
        if (GamePoolManager.Instance != null)
        {
            // GamePoolManager가 OnReturnToPool()을 자동으로 호출함
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

