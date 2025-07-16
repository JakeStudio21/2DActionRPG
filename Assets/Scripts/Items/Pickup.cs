using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    private enum PickUpType
    {
        GoldCoin = 0,
        StaminaGlobe = 1, // 🔑 호환성 유지를 위해 복원 (사용하지 않아도 enum 슬롯 유지)
        HealthGlobe = 2,  // 🔑 기존 프리팹 값과 일치
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
        switch (pickUpType)
        {
            case PickUpType.GoldCoin:
                // ⭐ 수정: PlayerManager로 통합하여 중복 제거
                if (PlayerManager.Instance != null)
                {
                    PlayerManager.Instance.AddGold(1);
                }
                else
                {
                    Debug.LogWarning("[Pickup] PlayerManager를 찾을 수 없습니다!");
                }
                
                Debug.Log("GoldCoin");
                break;
            
            case PickUpType.HealthGlobe:
                var playerHealth = FindObjectOfType<PlayerHealth>();
                playerHealth.HealPlayer();
                Debug.Log("HealthGlobe");
                break;
            
            // 🔑 StaminaGlobe 케이스 추가 (사용하지 않지만 enum 호환성 유지)
            case PickUpType.StaminaGlobe:
                Debug.LogWarning("[Pickup] StaminaGlobe는 더 이상 사용되지 않습니다.");
                break;
            
            default:
                Debug.LogError($"[Pickup] 알 수 없는 픽업 타입: {pickUpType} (값: {(int)pickUpType})");
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
            default:
                Debug.LogError($"[Pickup] 알 수 없는 픽업 타입으로 반환 시도: {pickUpType}");
                gameObject.SetActive(false);
                return;
        }
        
        // GamePoolManager에 정상 반환 (Arrow와 동일한 방식)
        if (GamePoolManager.Instance != null)
        {
            GamePoolManager.Instance.ReturnToPool(poolTag, gameObject);
            Debug.Log($"[Pickup] {poolTag} 픽업을 풀에 정상 반환");
        }
        else
        {
            // 백업: GamePoolManager가 없으면 기존 방식
            Debug.LogWarning("[Pickup] GamePoolManager가 없어 SetActive(false) 사용");
            gameObject.SetActive(false);
        }
    }
}
