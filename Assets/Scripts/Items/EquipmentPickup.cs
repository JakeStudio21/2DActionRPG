using UnityEngine;
using System.Collections;
using ItemSystem;

/// <summary>
/// 장비형 픽업 (무기/방어구)
/// Drop_Equipment 프리팹에 부착
/// FX Attach 방식: 등급별 FX를 풀에서 가져와 동적으로 부착
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class EquipmentPickup : MonoBehaviour, IPoolableObject
{
    [Header("컴포넌트")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform fxAttachPoint; // FX 부착 위치
    [SerializeField] private CircleCollider2D itemCollider;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer shadowSpriteRenderer; // ⭐ 그림자 스프라이트
    
    [Header("이동 설정")]
    [SerializeField] private float pickUpDistance = 5f;
    [SerializeField] private float accelerationRate = 0.2f;
    [SerializeField] private float moveSpeed = 3f;
    
    [Header("스폰 애니메이션")]
    [SerializeField] private AnimationCurve popAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // ⭐ GrapeProjectile 방식
    [SerializeField] private float popHeightY = 3.5f; // ⭐ GrapeProjectile 참고 (5.5 → 3.5로 적당히 조정)
    [SerializeField] private float popDuration = 0.8f; // ⭐ GrapeProjectile과 동일 (0.8초)
    
    [Header("아웃라인 설정")]
    [SerializeField] private string outlineColorProperty = "_OutlineColor";
    [SerializeField] private float outlineWidth = 0.05f;
    
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // ⭐ 현재 부착된 FX 참조 (핵심!)
    private GameObject attachedFxInstance;
    
    // 현재 데이터
    private string currentItemId;
    private EquipmentRank currentRank;
    private EquipmentData currentEquipmentData;
    
    // 이동 상태
    private Vector3 moveDirection;
    private float currentMoveSpeed;
    private bool isPopping = false;
    
    // 플레이어 참조
    private Transform player;
    
    // ⭐ 아웃라인 관리 (MaterialPropertyBlock)
    private MaterialPropertyBlock propertyBlock;
    
    private void Awake()
    {
        // 컴포넌트 자동 할당
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (itemCollider == null)
            itemCollider = GetComponent<CircleCollider2D>();
        
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        
        // fxAttachPoint 생성 (없으면)
        if (fxAttachPoint == null)
        {
            GameObject fxPoint = new GameObject("FX_AttachPoint");
            fxPoint.transform.SetParent(transform);
            fxPoint.transform.localPosition = Vector3.zero;
            fxAttachPoint = fxPoint.transform;
        }
        
        // ⭐ Shadow_Circle 찾기 및 초기화
        if (shadowSpriteRenderer == null)
        {
            Transform shadowTransform = transform.Find("Shadow_Circle");
            if (shadowTransform != null)
            {
                shadowSpriteRenderer = shadowTransform.GetComponent<SpriteRenderer>();
                if (shadowSpriteRenderer != null)
                {
                    // 초기 상태: 그림자 숨김
                    shadowSpriteRenderer.enabled = false;
                }
                else
                {
                    Debug.LogWarning("[EquipmentPickup] Shadow_Circle에 SpriteRenderer가 없습니다!");
                }
            }
        }
        
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
        
        // ⭐ MaterialPropertyBlock 초기화
        propertyBlock = new MaterialPropertyBlock();
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
            
            // ⭐ 플레이어 추적 시작 → 그림자 숨김
            HideShadow();
        }
        else
        {
            // 범위 벗어나면 완전 정지 ⭐
            currentMoveSpeed = 0f;
            moveDirection = Vector3.zero;
            if (rb != null)
                rb.velocity = Vector2.zero;
            
            // ⭐ 정지 상태 → 그림자 표시
            ShowShadow();
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
    /// 데이터 주입 방식 초기화 (EquipmentData 기반)
    /// </summary>
    public void Initialize(EquipmentData equipmentData, ItemRarity rarity)
    {
        if (equipmentData == null)
        {
            Debug.LogError("[EquipmentPickup] 초기화 데이터가 null입니다!");
            return;
        }
        
        // 🔍 디버그: ItemRarity → EquipmentRank 변환 과정 확인
        Debug.Log($"🔍 [DEBUG] Initialize 호출: itemId={equipmentData.itemID}, ItemRarity={rarity}");
        
        // ItemRarity → EquipmentRank 변환
        EquipmentRank rank = EquipmentRankExtensions.FromItemRarity(rarity);
        
        Debug.Log($"🔍 [DEBUG] 변환 완료: ItemRarity.{rarity} → EquipmentRank.{rank} ({rank.GetRankName()})");
        
        // 초기화
        Initialize(equipmentData.itemID, equipmentData.icon, rank, equipmentData);
    }
    
    /// <summary>
    /// 데이터 주입 방식 초기화 (개별 파라미터)
    /// </summary>
    public void Initialize(string itemId, Sprite sprite, EquipmentRank rank, EquipmentData equipmentData)
    {
        currentItemId = itemId;
        currentRank = rank;
        currentEquipmentData = equipmentData;
        
        // 1. 스프라이트 변경
        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;
            // ⭐ 스프라이트 본체는 원래 색상 유지
            spriteRenderer.color = Color.white;
        }
        
        // 1-2. ⭐ 등급별 아웃라인 적용
        SetOutlineColor(rank.GetRankColor());
        
        // 2. FX Attach ⭐
        AttachFx(rank);
        
        // 3. 이동 상태 초기화
        moveDirection = Vector3.zero;
        currentMoveSpeed = 0f;
        
        // 4. 활성화
        gameObject.SetActive(true);
        
        // 5. 스폰 애니메이션 시작
        StartCoroutine(PopAnimationRoutine());
        
        if (enableDebugLogs)
            Debug.Log($"⚔️ [EquipmentPickup] 초기화 완료: {itemId} (등급: {rank.GetRankName()})");
    }
    
    /// <summary>
    /// ⭐ FX 부착 (풀에서 가져와 동적으로 부착)
    /// </summary>
    private void AttachFx(EquipmentRank rank)
    {
        // 1. 기존 FX 정리 (안전장치)
        DetachFxIfAny();
        
        if (GamePoolManager.Instance == null)
        {
            Debug.LogError("[EquipmentPickup] GamePoolManager.Instance가 null입니다!");
            return;
        }
        
        // 2. 등급별 FX 풀 태그 생성
        string fxPoolTag = rank.GetFxPoolTag(); // 예: "EquipmentFX_A"
        
        // 3. 풀에서 FX 가져오기
        GameObject fxInstance = GamePoolManager.Instance.SpawnFromPool(fxPoolTag, Vector3.zero, Quaternion.identity);
        
        if (fxInstance != null)
        {
            ParticleSystem ps = fxInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // ⭐ FX 완전 초기화 (필수!)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear();
                ps.Play();
            }
            
            // 4. 부모 설정 (Attach)
            fxInstance.transform.SetParent(fxAttachPoint);
            fxInstance.transform.localPosition = Vector3.zero;
            fxInstance.SetActive(true);
            
            // 5. 참조 저장 ⭐
            attachedFxInstance = fxInstance;
            
            if (enableDebugLogs)
                Debug.Log($"✨ [EquipmentPickup] FX 부착: {fxPoolTag}");
        }
        else
        {
            Debug.LogWarning($"[EquipmentPickup] FX 풀 '{fxPoolTag}'에서 오브젝트를 가져올 수 없습니다!");
        }
    }
    
    /// <summary>
    /// ⭐ FX 분리 (풀 반환)
    /// </summary>
    private void DetachFxIfAny()
    {
        if (attachedFxInstance != null)
        {
            ParticleSystem ps = attachedFxInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // ⭐ FX 완전 정리 (필수!)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Clear();
            }
            
            // 부모 분리
            attachedFxInstance.transform.SetParent(null);
            
            // 풀 반환 (태그 복원)
            string fxTag = currentRank.GetFxPoolTag();
            if (GamePoolManager.Instance != null)
            {
                GamePoolManager.Instance.ReturnToPool(fxTag, attachedFxInstance);
            }
            
            // 참조 초기화 ⭐
            attachedFxInstance = null;
            
            if (enableDebugLogs)
                Debug.Log($"✨ [EquipmentPickup] FX 분리: {fxTag}");
        }
    }
    
    /// <summary>
    /// 픽업 처리
    /// </summary>
    private void OnPickup()
    {
        if (currentEquipmentData == null)
        {
            Debug.LogWarning("[EquipmentPickup] 픽업할 데이터가 없습니다!");
            return;
        }
        
        if (PlayerDataManager.Instance != null)
        {
            bool success = PlayerDataManager.Instance.AddToInventory(currentEquipmentData);
            
            if (success)
            {
                if (enableDebugLogs)
                    Debug.Log($"🎒 [EquipmentPickup] 장비 획득: {currentEquipmentData.equipmentName} (등급: {currentRank.GetRankName()})");
                
                // TODO: 사운드 재생 (추후 추가)
                
                // 풀 반환
                ReturnToPool();
            }
            else
            {
                // ⭐ 인벤토리 가득 참 → 추적 중단 후 자연스럽게 감속
                Debug.LogWarning($"💼 [EquipmentPickup] 인벤토리 가득 참! {currentEquipmentData.equipmentName} 획득 실패");
                
                // 알림 메시지 표시
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.Instance.ShowInventoryFullNotification();
                }
                
                StartCoroutine(DecelerateAndStop(0.5f));
                return;
            }
        }
    }
    
    /// <summary>
    /// ⭐ 스폰 애니메이션 (GrapeProjectile 방식 포물선)
    /// </summary>
    private IEnumerator PopAnimationRoutine()
    {
        isPopping = true;
        
        // ⭐ Pop 애니메이션 중에는 그림자 숨김
        HideShadow();
        
        Vector3 startPos = transform.position;
        
        // ⭐ 랜덤 최종 위치 (넓게 퍼짐)
        Vector2 randomOffset = Random.insideUnitCircle * 2.5f;
        Vector3 endPos = startPos + new Vector3(randomOffset.x, randomOffset.y, 0f);
        
        float elapsedTime = 0f;
        
        while (elapsedTime < popDuration)
        {
            elapsedTime += Time.deltaTime;
            float linearT = elapsedTime / popDuration; // 0 ~ 1 선형 진행
            
            // ⭐ GrapeProjectile 방식: AnimationCurve로 높이 계산
            float heightT = popAnimationCurve.Evaluate(linearT);
            float currentHeight = Mathf.Lerp(0f, popHeightY, heightT);
            
            // ⭐ 수평 이동: Linear (일정한 속도)
            Vector3 horizontalPos = Vector3.Lerp(startPos, endPos, linearT);
            
            // 최종 위치 = 수평 위치 + 높이 오프셋
            transform.position = horizontalPos + Vector3.up * currentHeight;
            
            yield return null;
        }
        
        // ⭐ 최종 위치로 정확히 이동 (착지)
        transform.position = endPos;
        isPopping = false;
        
        // ⭐ Pop 애니메이션 완료 → 착지 상태 → 그림자 표시
        ShowShadow();
    }
    
    /// <summary>
    /// ⭐ 감속 후 정지 (인벤토리 가득 찬 경우)
    /// </summary>
    private IEnumerator DecelerateAndStop(float decelerationTime = 0.5f)
    {
        // ⭐ 감속 시작 시 그림자 숨김 (플레이어가 당기던 중)
        HideShadow();
        
        float startSpeed = currentMoveSpeed;
        float elapsed = 0f;
        
        while (elapsed < decelerationTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / decelerationTime;
            
            // Linear 감속
            currentMoveSpeed = Mathf.Lerp(startSpeed, 0f, t);
            rb.velocity = moveDirection * currentMoveSpeed;
            
            yield return null;
        }
        
        // 완전 정지
        currentMoveSpeed = 0f;
        moveDirection = Vector3.zero;
        rb.velocity = Vector2.zero;
        
        if (enableDebugLogs)
            Debug.Log($"🛑 [EquipmentPickup] 감속 정지 완료: {currentItemId}");
        
        // ⭐ 완전 정지 → 그림자 다시 표시
        ShowShadow();
        
        // Update() 비활성화 (선택)
        enabled = false;
    }
    
    /// <summary>
    /// ⭐ 아웃라인 색상 및 두께 설정 (MaterialPropertyBlock 사용)
    /// </summary>
    private void SetOutlineColor(Color color)
    {
        if (spriteRenderer == null || propertyBlock == null)
            return;
        
        // 현재 PropertyBlock 가져오기
        spriteRenderer.GetPropertyBlock(propertyBlock);
        
        // 아웃라인 색상 설정
        propertyBlock.SetColor(outlineColorProperty, color);
        
        // ⭐ 아웃라인 두께 설정
        propertyBlock.SetFloat("_OutlineWidth", outlineWidth);
        
        // PropertyBlock 적용
        spriteRenderer.SetPropertyBlock(propertyBlock);
        
        if (enableDebugLogs)
            Debug.Log($"✨ [EquipmentPickup] 아웃라인 설정 완료: Color={color}, Width={outlineWidth}");
    }
    
    /// <summary>
    /// ⭐ 아웃라인 리셋 (풀 반환 시)
    /// </summary>
    private void ResetOutline()
    {
        if (spriteRenderer == null || propertyBlock == null)
            return;
        
        // PropertyBlock 초기화
        propertyBlock.Clear();
        spriteRenderer.SetPropertyBlock(propertyBlock);
        
        // 스프라이트 색상도 리셋
        spriteRenderer.color = Color.white;
        
        if (enableDebugLogs)
            Debug.Log($"🔄 [EquipmentPickup] 아웃라인 리셋 완료");
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
        // ⭐ FX 분리 (무조건 호출!)
        DetachFxIfAny();
        
        // ⭐ 아웃라인 리셋
        ResetOutline();
        
        // ⭐ 그림자 숨김
        HideShadow();
        
        // 데이터 초기화
        currentItemId = null;
        currentRank = EquipmentRank.D;
        currentEquipmentData = null;
        
        // 스프라이트 초기화
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = null;
            spriteRenderer.color = Color.white;
        }
        
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
            GamePoolManager.Instance.ReturnToPool("Drop_Equipment", gameObject);
        }
        else
        {
            Debug.LogError("[EquipmentPickup] GamePoolManager.Instance가 null입니다!");
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// ⭐ 그림자 표시 (착지 상태, 정지 상태)
    /// </summary>
    private void ShowShadow()
    {
        if (shadowSpriteRenderer != null)
        {
            shadowSpriteRenderer.enabled = true;
            
            if (enableDebugLogs)
                Debug.Log($"🌑 [EquipmentPickup] 그림자 표시: {currentItemId}");
        }
    }
    
    /// <summary>
    /// ⭐ 그림자 숨김 (Pop 애니메이션 중, 플레이어 추적 중, 풀 반환 시)
    /// </summary>
    private void HideShadow()
    {
        if (shadowSpriteRenderer != null)
        {
            shadowSpriteRenderer.enabled = false;
            
            if (enableDebugLogs)
                Debug.Log($"☀️ [EquipmentPickup] 그림자 숨김: {currentItemId}");
        }
    }
}

