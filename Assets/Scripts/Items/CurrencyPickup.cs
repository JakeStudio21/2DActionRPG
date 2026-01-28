using UnityEngine;
using System.Collections;
using ItemSystem;

/// <summary>
/// 재화형 픽업 (골드/하트)
/// Drop_Currency 프리팹에 부착
/// FX Embedded 방식: 내장된 ParticleSystem 사용
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class CurrencyPickup : MonoBehaviour, IPoolableObject
{
    [Header("컴포넌트")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private ParticleSystem currencyFxInstance; // 내장 FX 참조
    [SerializeField] private CircleCollider2D itemCollider;
    [SerializeField] private Rigidbody2D rb;
    
    [Header("이동 설정")]
    [SerializeField] private float pickUpDistance = 5f;
    [SerializeField] private float accelerationRate = 0.2f;
    [SerializeField] private float moveSpeed = 3f;
    
    [Header("스폰 애니메이션")]
    [SerializeField] private AnimationCurve popAnimationCurve;
    [SerializeField] private float popHeightY = 1.5f;
    [SerializeField] private float popDuration = 1f;
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // 현재 데이터
    private string currentItemId;
    private int currentAmount;
    private CurrencyType currentType;
    
    // 이동 상태
    private Vector3 moveDirection;
    private float currentMoveSpeed;
    private bool isPopping = false;
    
    // 플레이어 참조
    private Transform player;
    
    private void Awake()
    {
        // 컴포넌트 자동 할당
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (itemCollider == null)
            itemCollider = GetComponent<CircleCollider2D>();
        
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        
        // Rigidbody2D 설정
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.drag = 0f;
        }
        
        // Collider 설정
        if (itemCollider != null)
        {
            itemCollider.isTrigger = true;
        }
    }
    
    private void Start()
    {
        // 플레이어 찾기
        FindPlayer();
    }
    
    private void Update()
    {
        if (player == null || isPopping) return;
        
        // 플레이어와의 거리 계산
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // 픽업 거리 안에 들어오면 플레이어 추적
        if (distanceToPlayer < pickUpDistance)
        {
            moveDirection = (player.position - transform.position).normalized;
            currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, moveSpeed, accelerationRate);
        }
        else
        {
            // 범위 벗어나면 완전 정지 ⭐
            currentMoveSpeed = 0f;
            moveDirection = Vector3.zero;
            if (rb != null)
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
    
    private void OnTriggerStay2D(Collider2D collision)
    {
        // 플레이어와 충돌 시 픽업
        if (collision.CompareTag("Player"))
        {
            OnPickup();
        }
    }
    
    /// <summary>
    /// 데이터 주입 방식 초기화 (BaseItemData 기반)
    /// </summary>
    public void Initialize(BaseItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogError("[CurrencyPickup] 초기화 데이터가 null입니다!");
            return;
        }
        
        // 타입 감지
        CurrencyType type = CurrencyType.Gold;
        int amount = 1;
        
        if (itemData is GoldItemData goldData)
        {
            type = CurrencyType.Gold;
            amount = goldData.isRandomAmount ? 
                Random.Range(goldData.minGoldAmount, goldData.maxGoldAmount + 1) : 
                goldData.goldAmount;
        }
        else if (itemData is HealthItemData healthData)
        {
            type = CurrencyType.Heart;
            amount = healthData.healAmount;
        }
        
        // 초기화
        Initialize(itemData.itemId, itemData.icon, amount, type);
    }
    
    /// <summary>
    /// 데이터 주입 방식 초기화 (개별 파라미터)
    /// </summary>
    public void Initialize(string itemId, Sprite sprite, int amount, CurrencyType type)
    {
        currentItemId = itemId;
        currentAmount = amount;
        currentType = type;
        
        // 1. 스프라이트 변경
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
        
        // 2. FX 초기화 및 재생
        if (currencyFxInstance != null)
        {
            // ⭐ FX 완전 초기화 (필수!)
            currencyFxInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            currencyFxInstance.Clear();
            currencyFxInstance.Play();
        }
        
        // 3. 이동 상태 초기화
        moveDirection = Vector3.zero;
        currentMoveSpeed = 0f;
        
        // 4. 활성화
        gameObject.SetActive(true);
        
        // 5. 스폰 애니메이션 시작
        StartCoroutine(PopAnimationRoutine());
        
        if (enableDebugLogs)
            Debug.Log($"💰 [CurrencyPickup] 초기화 완료: {itemId} ({type}) x{amount}");
    }
    
    /// <summary>
    /// 픽업 처리
    /// </summary>
    private void OnPickup()
    {
        if (string.IsNullOrEmpty(currentItemId))
        {
            Debug.LogWarning("[CurrencyPickup] 픽업할 데이터가 없습니다!");
            return;
        }
        
        if (PlayerDataManager.Instance != null)
        {
            switch (currentType)
            {
                case CurrencyType.Gold:
                    PlayerDataManager.Instance.AddGold(currentAmount);
                    if (enableDebugLogs)
                        Debug.Log($"💰 [CurrencyPickup] 골드 획득: +{currentAmount}");
                    break;
                    
                case CurrencyType.Heart:
                    var playerHealth = FindObjectOfType<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.HealPlayerAmount(currentAmount);
                        if (enableDebugLogs)
                            Debug.Log($"❤️ [CurrencyPickup] 체력 회복: +{currentAmount}");
                    }
                    break;
            }
        }
        
        // TODO: 사운드 재생 (추후 추가)
        
        // 풀 반환
        ReturnToPool();
    }
    
    /// <summary>
    /// 스폰 애니메이션 (튀어오르면서 퍼지기)
    /// </summary>
    private IEnumerator PopAnimationRoutine()
    {
        isPopping = true;
        
        Vector3 startPos = transform.position;
        
        // ⭐ 랜덤 최종 위치 (넓게 퍼짐)
        Vector2 randomOffset = Random.insideUnitCircle * 2.5f;
        Vector3 endPos = startPos + new Vector3(randomOffset.x, randomOffset.y, 0f);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < popDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / popDuration;
            
            // 애니메이션 커브 적용
            float curveValue = popAnimationCurve != null ? 
                popAnimationCurve.Evaluate(progress) : 
                Mathf.Sin(progress * Mathf.PI);
            
            // Y축 오프셋 (튀어오르기)
            float heightOffset = curveValue * popHeightY;
            
            // startPos → endPos로 이동하면서 튀어오르기
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
            currentPos.y += heightOffset;
            
            transform.position = currentPos;
            
            yield return null;
        }
        
        // 최종 위치로 정확히 이동
        transform.position = endPos;
        isPopping = false;
    }
    
    /// <summary>
    /// 플레이어 찾기
    /// </summary>
    private void FindPlayer()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }
    
    /// <summary>
    /// IPoolableObject 구현: 풀에서 스폰될 때 호출
    /// </summary>
    public void OnSpawnFromPool()
    {
        // 스폰 시 특별한 초기화가 필요하면 여기서 처리
        // 현재는 Initialize()에서 모든 초기화를 하므로 비워둠
    }
    
    /// <summary>
    /// IPoolableObject 구현: 풀로 반환될 때 호출
    /// </summary>
    public void OnReturnToPool()
    {
        // ⭐ FX 완전 정리 (필수!)
        if (currencyFxInstance != null)
        {
            currencyFxInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            currencyFxInstance.Clear();
        }
        
        // 데이터 초기화
        currentItemId = null;
        currentAmount = 0;
        currentType = CurrencyType.Gold;
        
        // 스프라이트 초기화
        if (spriteRenderer != null)
            spriteRenderer.sprite = null;
        
        // 이동 상태 초기화
        moveDirection = Vector3.zero;
        currentMoveSpeed = 0f;
        isPopping = false;
        
        // 물리 초기화
        if (rb != null)
            rb.velocity = Vector2.zero;
        
        // 비활성화
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 풀 반환
    /// </summary>
    public void ReturnToPool()
    {
        if (GamePoolManager.Instance != null)
        {
            // GamePoolManager가 OnReturnToPool()을 자동으로 호출함
            GamePoolManager.Instance.ReturnToPool("Drop_Currency", gameObject);
        }
        else
        {
            Debug.LogError("[CurrencyPickup] GamePoolManager.Instance가 null입니다!");
            Destroy(gameObject);
        }
    }
}

