using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ItemSystem; // 🆕 ItemSystem 네임스페이스 추가

public class Pickup : MonoBehaviour
{
    private enum PickUpType
    {
        GoldCoin = 0,
        StaminaGlobe = 1, // 🔑 호환성 유지를 위해 복원 (사용하지 않아도 enum 슬롯 유지)
        HealthGlobe = 2,  // 🔑 기존 프리팹 값과 일치
        EquipmentItem = 3 // 🆕 장비 아이템 추가
    }
    [SerializeField] private PickUpType pickUpType;
    [SerializeField] private float pickUpDistance = 5f;
    [SerializeField] private float accelartionRate = .2f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private AnimationCurve animCurve;
    [SerializeField] private float heightY = 1.5f;
    [SerializeField] private float popDuration = 1f;

    private Vector3 moveDir;
    private Rigidbody2D rb;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }
    
     private void Start() {
        StartCoroutine(AnimCurveSpawnRoutine());
    }

    private void Update() {
        var playerController = FindObjectOfType<PlayerController>();
        
        // ⭐ 핵심 수정: null 체크 추가하여 씬 전환 시 에러 방지
        if (playerController == null)
        {
            // PlayerController가 없으면 움직임 정지
            moveDir = Vector3.zero;
            moveSpeed = 0;
            return;
        }
        
        Vector3 playerPos = playerController.transform.position;

        if (Vector3.Distance(transform.position, playerPos) < pickUpDistance) {
            moveDir = (playerPos - transform.position).normalized;
            moveSpeed += accelartionRate;
        } else {
            moveDir = Vector3.zero;
            moveSpeed = 0;
        }
    }

    private void FixedUpdate() {
        rb.velocity = moveDir * moveSpeed * Time.deltaTime;
    }

    private void OnTriggerStay2D(Collider2D other) {
        if (other.gameObject.GetComponent<PlayerController>()) {
            DetectPickupType();
            
            // 🔑 핵심 수정: Arrow처럼 GamePoolManager에 정상 반환
            ReturnToPool();
        }
    }

    private IEnumerator AnimCurveSpawnRoutine() {
        Vector2 startPoint = transform.position;
        float randomX = transform.position.x + Random.Range(-2f, 2f);
        float randomY = transform.position.y + Random.Range(-1f, 1f);

        Vector2 endPoint = new Vector2(randomX, randomY);

        float timePassed = 0f;

        while (timePassed < popDuration)
        {
            timePassed += Time.deltaTime;
            float linearT = timePassed / popDuration;
            float heightT = animCurve.Evaluate(linearT);
            float height = Mathf.Lerp(0f, heightY, heightT);

            transform.position = Vector2.Lerp(startPoint, endPoint, linearT) + new Vector2(0f, height);
            yield return null;
        }
    }

    private void DetectPickupType() {
        // 1. ScriptableObject 기반 아이템 처리 (우선순위)
        if (itemData != null)
        {
            ProcessScriptableObjectItem();
            return;
        }
        
        // 2. EquipmentData 처리 (호환성)
        if (equipmentData != null)
        {
            ProcessEquipmentItem();
            return;
        }
        
        // 3. 기존 enum 기반 처리 (레거시 호환성)
        ProcessLegacyPickup();
    }
    
    /// <summary>
    /// ScriptableObject 기반 아이템 처리
    /// </summary>
    private void ProcessScriptableObjectItem()
    {
        if (PlayerDataManager.Instance != null)
        {
            itemData.UseItem(PlayerDataManager.Instance);
            
        }
        else
        {
            Debug.LogWarning("[Pickup] PlayerDataManager를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// EquipmentData 처리
    /// </summary>
    private void ProcessEquipmentItem()
    {
        if (PlayerDataManager.Instance != null && equipmentData != null)
        {
            bool success = PlayerDataManager.Instance.AddToInventory(equipmentData);
            if (success)
            {
            }
            else
            {
                Debug.LogWarning($"💼 [Pickup] 인벤토리가 가득 참! {equipmentData.name} 획득 실패");
            }
        }
        else
        {
            Debug.LogError("[Pickup] PlayerDataManager 또는 equipmentData가 없습니다!");
        }
    }
    
    /// <summary>
    /// 기존 enum 기반 픽업 처리 (호환성 유지)
    /// </summary>
    private void ProcessLegacyPickup()
    {
        switch (pickUpType)
        {
            case PickUpType.GoldCoin:
                if (PlayerDataManager.Instance != null)
                {
                    PlayerDataManager.Instance.AddGold(1);
                }
                break;
                    
            case PickUpType.HealthGlobe:
                var playerHealth = FindObjectOfType<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.HealPlayer();
                }
                break;
                    
            case PickUpType.EquipmentItem:
                ProcessEquipmentItem();
                break;
                    
            default:
                Debug.LogError($"[Pickup] 알 수 없는 픽업 타입: {pickUpType}");
                break;
        }
    }

    /// <summary>
    /// 🔑 Arrow처럼 GamePoolManager로 정상 반환
    /// </summary>
    private void ReturnToPool()
    {
        // 픽업 타입에 따른 풀 태그 결정
        string poolTag = "";
        switch (pickUpType)
        {
            case PickUpType.GoldCoin:
                poolTag = "Gold Coin";
                break;
            case PickUpType.HealthGlobe:
                poolTag = "Health";
                break;
            case PickUpType.StaminaGlobe:
                Debug.LogWarning("[Pickup] StaminaGlobe는 더 이상 지원되지 않습니다.");
                gameObject.SetActive(false);
                return;
            case PickUpType.EquipmentItem:
                poolTag = "Equipment";
                break;
            default:
                Debug.LogError($"[Pickup] 알 수 없는 픽업 타입으로 반환 시도: {pickUpType}");
                gameObject.SetActive(false);
                return;
        }
        
        // GamePoolManager에 정상 반환 (Arrow와 동일한 방식)
        if (GamePoolManager.Instance != null)
        {
            GamePoolManager.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            // 백업: GamePoolManager가 없으면 기존 방식
            Debug.LogWarning("[Pickup] GamePoolManager가 없어 SetActive(false) 사용");
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// EquipmentData 설정 (런타임에 동적으로 연결)
    /// </summary>
    public void SetEquipmentData(EquipmentData data)
    {
        equipmentData = data;
        
    }

    /// <summary>
    /// ScriptableObject 기반 아이템 데이터 설정 (런타임에 동적으로 연결)
    /// </summary>
    public void SetItemData(BaseItemData data)
    {
        itemData = data;
        
    }

    [Header("🎒 아이템 설정")]
    [SerializeField] private BaseItemData itemData; // 모든 아이템 타입 지원 (우선순위 1)
    [SerializeField] private EquipmentData equipmentData; // 장비 아이템 (우선순위 2, 호환성)
    
}
