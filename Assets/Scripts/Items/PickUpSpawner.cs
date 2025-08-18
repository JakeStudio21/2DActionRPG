using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ItemSystem;  // 🆕 추가 필요

public class PickUpSpawner : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private bool canDropHealth = true;
    [SerializeField] private bool canDropGold = true;
    
    [Header("Health Drop")]
    [SerializeField] [Range(0f, 100f)] private float healthDropChance = 50f;
    [SerializeField] private int healthDropAmount = 1;
    
    [Header("Gold Drop")]
    [SerializeField] [Range(0f, 100f)] private float goldDropChance = 50f;
    [SerializeField] private int goldDropMinAmount = 1;
    [SerializeField] private int goldDropMaxAmount = 3;

    [Header("Equipment Drop")]
    [SerializeField] private bool canDropEquipment = true;
    [SerializeField] [Range(0f, 100f)] private float equipmentDropChance = 20f;
    [SerializeField] private EquipmentData[] possibleEquipmentDrops; // 드롭 가능한 장비들

    public void DropItems() {
        Debug.Log($"🎯 [PickUpSpawner] DropItems 호출! 몬스터: {gameObject.name}");
        Debug.Log($"🎯 [PickUpSpawner] 현재 플레이어 타입: {(PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected ? PlayerDataManager.Instance.CurrentPlayerType.ToString() : "NULL")}");
        Debug.Log($"🎯 [PickUpSpawner] GamePoolManager 상태: {(GamePoolManager.Instance != null ? "정상" : "NULL")}");
        
        // Health 드랍 체크 (새로운 방식)
        if (canDropHealth && Random.Range(0f, 100f) <= healthDropChance) {
            Debug.Log($"💊 [PickUpSpawner] Health 드랍 성공! 개수: {healthDropAmount}");
            
            for (int i = 0; i < healthDropAmount; i++) {
                SpawnPickupItem("ITEM_HEALTH_POTION");
            }
        }

        // Gold 드랍 체크 (새로운 방식)  
        if (canDropGold && Random.Range(0f, 100f) <= goldDropChance) {
            int goldAmount = Random.Range(goldDropMinAmount, goldDropMaxAmount + 1);
            Debug.Log($"💰 [PickUpSpawner] Gold 드랍 성공! 개수: {goldAmount}");
            
            for (int i = 0; i < goldAmount; i++) {
                SpawnPickupItem("ITEM_GOLD_COIN");
            }
        }
        
        // Equipment 드랍은 기존 방식 유지
        if (canDropEquipment && possibleEquipmentDrops != null && 
            possibleEquipmentDrops.Length > 0 && Random.Range(0f, 100f) <= equipmentDropChance) {
            
            Debug.Log($"🎒 [PickUpSpawner] Equipment 드랍 성공!");
            
            // 🔍 유효한 장비만 필터링
            EquipmentData[] validEquipments = System.Array.FindAll(possibleEquipmentDrops, 
                equipment => equipment != null);
            
            if (validEquipments.Length > 0) {
                // 랜덤하게 장비 선택
                EquipmentData randomEquipment = validEquipments[Random.Range(0, validEquipments.Length)];
                
                // 장비 픽업 오브젝트 생성
                GameObject equipmentPickup = CreateEquipmentPickup(randomEquipment);
                if (equipmentPickup != null)
                {
                    Debug.Log($"🎒 [PickUpSpawner] 장비 드롭 성공: {randomEquipment.equipmentName}");
                }
                else
                {
                    Debug.LogError($"🎒 [PickUpSpawner] 장비 드롭 실패: {randomEquipment.equipmentName}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ [PickUpSpawner] {gameObject.name}에 유효한 장비가 설정되지 않았습니다!");
            }
        } else {
            Debug.Log($"🎒 [PickUpSpawner] Equipment 드랍 실패 - canDrop: {canDropEquipment}, 확률: {equipmentDropChance}%");
        }
        
        Debug.Log($"🎯 [PickUpSpawner] DropItems 완료!");
    }

    /// <summary>
    /// 🆕 개선된 장비 픽업 오브젝트 생성
    /// Equipment 전용 프리팹 시도 → 실패 시 Gold Coin + 아이콘 변경
    /// </summary>
    private GameObject CreateEquipmentPickup(EquipmentData equipmentData)
    {
        // 🎯 아이템별 개별 풀 태그 생성
        string poolTag = GetEquipmentPoolTag(equipmentData);
        
        GameObject equipmentPickup = GamePoolManager.Instance.SpawnFromPool(poolTag, transform.position, Quaternion.identity);
        
        if (equipmentPickup != null)
        {
            SetupPickupComponent(equipmentPickup, equipmentData);
            Debug.Log($"✅ [PickUpSpawner] {poolTag} 드롭: {equipmentData.equipmentName}");
        }
        else
        {
            Debug.LogWarning($"⚠️ [PickUpSpawner] {poolTag} 풀이 없어서 범용 Equipment 풀 사용");
            // 백업: 범용 Equipment 풀 사용
            equipmentPickup = CreateFallbackEquipment(equipmentData);
        }
        
        return equipmentPickup;
    }

    private IEnumerator DelayedAppearanceSetup(GameObject pickup, EquipmentData equipmentData)
    {
        yield return null; // 한 프레임 대기
        
        if (pickup != null && pickup.activeInHierarchy)
        {
            Debug.Log($"🔄 [PickUpSpawner] 지연된 외형 설정: {equipmentData.equipmentName}");
            SetupEquipmentAppearance(pickup, equipmentData);
        }
    }

    private void SetupEquipmentAppearance(GameObject pickup, EquipmentData equipmentData)
    {
        SpriteRenderer spriteRenderer = pickup.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Debug.Log($"🔍 [PickUpSpawner] 외형 설정 시작: {equipmentData.equipmentName}");
            Debug.Log($"🔍 [PickUpSpawner] 현재 스프라이트: {spriteRenderer.sprite?.name}");
            
            if (equipmentData.icon != null)
            {
                // 🔥 강제 아이콘 변경
                spriteRenderer.sprite = equipmentData.icon;
                
                // ⚡ 강제 새로고침
                spriteRenderer.enabled = false;
                spriteRenderer.enabled = true;
                
                Debug.Log($"✅ [PickUpSpawner] 아이콘 강제 변경: {spriteRenderer.sprite.name}");
            }
            
            // 크기 및 색상 설정
            pickup.transform.localScale = Vector3.one * 1.3f;
            spriteRenderer.color = GetItemGradeColor(equipmentData.itemGrade);
        }
        else
        {
            Debug.LogError("❌ [PickUpSpawner] SpriteRenderer 컴포넌트를 찾을 수 없습니다!");
        }
    }

    private float GetEquipmentScale(EquipmentType equipmentType)
    {
        switch (equipmentType)
        {
            case EquipmentType.Weapon: return 1.4f;    // 무기는 크게
            case EquipmentType.Armor: return 1.2f;     // 방어구는 중간
            case EquipmentType.Accessory: return 1.0f; // 악세서리는 작게
            default: return 1.2f;
        }
    }
    
    /// <summary>
    /// 🎨 픽업 아이템을 장비처럼 보이게 변경
    /// </summary>
    private void ChangePickupAppearance(GameObject pickup, EquipmentData equipmentData)
    {
        SpriteRenderer spriteRenderer = pickup.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && equipmentData.icon != null)
        {
            // 아이콘 변경
            spriteRenderer.sprite = equipmentData.icon;
            
            // 장비는 골드보다 크게 표시
            pickup.transform.localScale = Vector3.one * 1.3f;
            
            // 장비 등급별 색상 변경
            Color itemColor = GetItemGradeColor(equipmentData.itemGrade);
            spriteRenderer.color = itemColor;
            
            Debug.Log($"🎨 [PickUpSpawner] 외형 변경: {equipmentData.equipmentName} ({equipmentData.itemGrade})");
        }
    }
    
    /// <summary>
    /// 🌈 아이템 등급별 색상 반환
    /// </summary>
    private Color GetItemGradeColor(ItemGrade grade)
    {
        switch (grade)
        {
            case ItemGrade.S: return new Color(1f, 0.8f, 0f, 1f);    // 금색 (레전더리)
            case ItemGrade.A: return new Color(0.6f, 0f, 1f, 1f);    // 보라색 (에픽)
            case ItemGrade.B: return new Color(0f, 0.6f, 1f, 1f);    // 파란색 (레어)
            case ItemGrade.C: return new Color(0f, 1f, 0f, 1f);      // 초록색 (언커먼)
            case ItemGrade.D: return Color.white;                     // 흰색 (커먼)
            default: return Color.white;
        }
    }
    
    /// <summary>
    /// 🔧 Pickup 컴포넌트 설정 (리플렉션 사용)
    /// </summary>
    private void SetupPickupComponent(GameObject pickup, EquipmentData equipmentData)
    {
        Pickup pickupComponent = pickup.GetComponent<Pickup>();
        if (pickupComponent != null)
        {
            Debug.Log($"🔧 [PickUpSpawner] Pickup 컴포넌트 설정 시작");
            
            // 리플렉션으로 private 필드들 설정
            var pickUpTypeField = typeof(Pickup).GetField("pickUpType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var equipmentDataField = typeof(Pickup).GetField("equipmentData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (pickUpTypeField != null && equipmentDataField != null)
            {
                // PickUpType을 EquipmentItem(3)으로 설정
                pickUpTypeField.SetValue(pickupComponent, 3); // EquipmentItem = 3
                
                // EquipmentData 설정
                equipmentDataField.SetValue(pickupComponent, equipmentData);
                
                Debug.Log($"✅ [PickUpSpawner] PickUpType을 EquipmentItem으로 변경");
            }
            else
            {
                Debug.LogError("❌ [PickUpSpawner] Pickup 컴포넌트의 필드를 찾을 수 없습니다!");
            }
        }
        else
        {
            Debug.LogError("❌ [PickUpSpawner] Pickup 컴포넌트를 찾을 수 없습니다!");
        }
    }

    private IEnumerator ForceAppearanceChange(GameObject pickup, EquipmentData equipmentData)
    {
        yield return null;
        yield return null;
        
        if (pickup != null && pickup.activeInHierarchy)
        {
            // 🔍 Animator 체크
            Animator animator = pickup.GetComponent<Animator>();
            if (animator != null)
            {
                Debug.LogWarning($"⚠️ [PickUpSpawner] Animator 발견! 비활성화: {pickup.name}");
                animator.enabled = false;
            }
            
            SpriteRenderer spriteRenderer = pickup.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && equipmentData.icon != null)
            {
                spriteRenderer.sprite = equipmentData.icon;
                Debug.Log($"✅ [PickUpSpawner] 아이콘 변경 완료: {equipmentData.icon.name}");
            }
        }
    }

    private GameObject CreateFallbackEquipment(EquipmentData equipmentData)
    {
        Debug.LogWarning($"⚠️ [PickUpSpawner] 개별 Equipment 풀이 없어서 범용 Equipment 풀을 사용합니다. 태그: {equipmentData.name}");
        string fallbackPoolTag = "Equipment"; // 범용 Equipment 풀 태그
        return GamePoolManager.Instance.SpawnFromPool(fallbackPoolTag, transform.position, Quaternion.identity);
    }

    private string GetEquipmentPoolTag(EquipmentData equipmentData)
    {
        // 🏷️ ScriptableObject 이름을 풀 태그로 사용
        return equipmentData.name.Replace("_Equipment", "_Pickup");
        // 예: "Sword_A_Equipment" → "Sword_A_Pickup"
    }

    /// <summary>
    /// Pickup 아이템 스폰 (통합 메서드)
    /// </summary>
    private void SpawnPickupItem(string itemID)
    {
        // 1단계: PickupDataCache에서 아이템 데이터 조회
        BaseItemData itemData = PickupDataCache.Instance.GetPickupItemData(itemID);
        if (itemData == null)
        {
            Debug.LogError($"[PickUpSpawner] PickupItemData를 찾을 수 없습니다: {itemID}");
            return;
        }
        
        // 2단계: pickupPrefab 참조 확인
        if (itemData.pickupPrefab == null)
        {
            Debug.LogError($"[PickUpSpawner] {itemID}의 pickupPrefab이 null입니다!");
            return;
        }
        
        // 3단계: itemID로 풀링 시도 (풀 키 = itemID로 통일)
        GameObject spawnedItem = null;
        
        try
        {
            if (GamePoolManager.Instance != null)
            {
                spawnedItem = GamePoolManager.Instance.SpawnFromPool(itemID, transform.position, Quaternion.identity);
                Debug.Log($"[PickUpSpawner] 풀에서 아이템 스폰 성공: {itemID}");
            }
            else
            {
                Debug.LogError("[PickUpSpawner] GamePoolManager.Instance가 null입니다!");
                return;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PickUpSpawner] 풀 스폰 실패: {itemID}, 에러: {e.Message}");
            return;
        }
        
        // 4단계: 풀링 실패 시 즉시 에러 (fallback 제거)
        if (spawnedItem == null)
        {
            Debug.LogError($"[PickUpSpawner] 풀에서 아이템 스폰 실패: {itemID}");
            Debug.LogError($"   - 풀 키: {itemID}");
            Debug.LogError($"   - ItemData: {itemData.itemName}");
            Debug.LogError($"   - PickupPrefab: {itemData.pickupPrefab.name}");
            return;
        }
    }
}