using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 🎮 착용된 장비들을 표시하는 전용 UI 시스템
/// Weapon / Armor / Boots 슬롯을 개별적으로 관리
/// </summary>
public class EquippedItemsUI : MonoBehaviour
{
    [Header("🎒 착용 장비 슬롯들")]
    [SerializeField] private InventorySlot weaponSlot;      // 무기 슬롯
    [SerializeField] private InventorySlot armorSlot;       // 갑옷 슬롯  
    [SerializeField] private InventorySlot bootsSlot;       // 신발 슬롯
    [SerializeField] private InventorySlot helmetSlot;      // 헬멧 슬롯 (향후 확장)
    [SerializeField] private InventorySlot shieldSlot;      // 방패 슬롯 (향후 확장)
    [SerializeField] private InventorySlot ring1Slot;       // 반지1 슬롯 (향후 확장)
    [SerializeField] private InventorySlot ring2Slot;       // 반지2 슬롯 (향후 확장)
    [SerializeField] private InventorySlot necklaceSlot;    // 목걸이 슬롯 (향후 확장)
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    private void Start()
    {
        // PlayerDataManager 이벤트 구독
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnItemEquipped += OnItemEquipped;
            PlayerDataManager.Instance.OnItemUnequipped += OnItemUnequipped;
        }
        
        // 🆕 각 슬롯의 클릭 이벤트 구독
        SetupSlotClickEvents();
        
        // 초기 장비 상태 표시
        RefreshAllEquippedItems();
    }
    
    /// <summary>
    /// 모든 착용된 장비 슬롯 새로고침
    /// </summary>
    public void RefreshAllEquippedItems()
    {
        if (PlayerDataManager.Instance == null) return;
        
        var equippedItems = PlayerDataManager.Instance.EquippedItems;
        
        // 각 슬롯별로 업데이트
        UpdateSlot(weaponSlot, EquipmentSlot.MainWeapon, equippedItems);
        UpdateSlot(armorSlot, EquipmentSlot.Armor, equippedItems);
        UpdateSlot(bootsSlot, EquipmentSlot.Boots, equippedItems);
        UpdateSlot(helmetSlot, EquipmentSlot.Helmet, equippedItems);
        UpdateSlot(shieldSlot, EquipmentSlot.Shield, equippedItems);
        UpdateSlot(ring1Slot, EquipmentSlot.Ring1, equippedItems);
        UpdateSlot(ring2Slot, EquipmentSlot.Ring2, equippedItems);
        UpdateSlot(necklaceSlot, EquipmentSlot.Necklace, equippedItems);
        
        if (showDebugLogs)
            Debug.Log("🎮 [EquippedItemsUI] 모든 착용 장비 슬롯 새로고침 완료");
    }
    
    /// <summary>
    /// 🖱️ 각 슬롯의 클릭 이벤트 설정
    /// </summary>
    private void SetupSlotClickEvents()
    {
        // 각 슬롯에 클릭 이벤트 연결
        SetupSlotClickEvent(weaponSlot, EquipmentSlot.MainWeapon);
        SetupSlotClickEvent(armorSlot, EquipmentSlot.Armor);
        SetupSlotClickEvent(bootsSlot, EquipmentSlot.Boots);
        SetupSlotClickEvent(helmetSlot, EquipmentSlot.Helmet);
        SetupSlotClickEvent(shieldSlot, EquipmentSlot.Shield);
        SetupSlotClickEvent(ring1Slot, EquipmentSlot.Ring1);
        SetupSlotClickEvent(ring2Slot, EquipmentSlot.Ring2);
        SetupSlotClickEvent(necklaceSlot, EquipmentSlot.Necklace);
        
        if (showDebugLogs)
            Debug.Log("🖱️ [EquippedItemsUI] 모든 슬롯 클릭 이벤트 설정 완료");
    }
    
    /// <summary>
    /// 🖱️ 개별 슬롯 클릭 이벤트 설정
    /// </summary>
    private void SetupSlotClickEvent(InventorySlot slot, EquipmentSlot equipmentSlot)
    {
        if (slot == null) return;
        
        // InventorySlot의 Button 컴포넌트 가져오기
        var button = slot.GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            // 기존 클릭 이벤트 제거 후 새로 추가
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnEquippedSlotClicked(equipmentSlot));
            
            if (showDebugLogs)
                Debug.Log($"🖱️ [EquippedItemsUI] {equipmentSlot} 슬롯 클릭 이벤트 연결");
        }
        else
        {
            Debug.LogWarning($"⚠️ [EquippedItemsUI] {equipmentSlot} 슬롯에 Button 컴포넌트가 없습니다!");
        }
    }
    
    /// <summary>
    /// 🖱️ 착용 장비 슬롯 클릭 시 처리
    /// </summary>
    private void OnEquippedSlotClicked(EquipmentSlot slot)
    {
        if (showDebugLogs)
            Debug.Log($"🖱️ [EquippedItemsUI] {slot} 슬롯 클릭됨");
        
        // PlayerDataManager에서 해당 슬롯의 장비 확인
        if (PlayerDataManager.Instance == null) return;
        
        var equippedItems = PlayerDataManager.Instance.EquippedItems;
        if (equippedItems.ContainsKey(slot) && equippedItems[slot] != null)
        {
            var equippedItem = equippedItems[slot];
            
            if (showDebugLogs)
                Debug.Log($"🖱️ [EquippedItemsUI] {slot}에서 {equippedItem.equipmentName} 해제 시도");
            
            // 장비 해제 실행
            bool success = PlayerDataManager.Instance.UnequipItem(slot);
            
            if (success)
            {
                if (showDebugLogs)
                    Debug.Log($"✅ [EquippedItemsUI] {equippedItem.equipmentName} 해제 성공");
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning($"⚠️ [EquippedItemsUI] {equippedItem.equipmentName} 해제 실패 (인벤토리가 가득참?)");
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"🖱️ [EquippedItemsUI] {slot} 슬롯이 비어있습니다");
        }
    }
    
    /// <summary>
    /// 개별 슬롯 업데이트
    /// </summary>
    private void UpdateSlot(InventorySlot uiSlot, EquipmentSlot equipmentSlot, Dictionary<EquipmentSlot, EquipmentData> equippedItems)
    {
        if (uiSlot == null) return;
        
        if (equippedItems.ContainsKey(equipmentSlot) && equippedItems[equipmentSlot] != null)
        {
            // 장착된 아이템 표시
            uiSlot.SetEquipmentData(equippedItems[equipmentSlot]);
            
            if (showDebugLogs)
                Debug.Log($"🎮 [EquippedItemsUI] {equipmentSlot} 슬롯 업데이트: {equippedItems[equipmentSlot].equipmentName}");
        }
        else
        {
            // 빈 슬롯 표시
            uiSlot.SetEquipmentData(null);
            
            if (showDebugLogs)
                Debug.Log($"🎮 [EquippedItemsUI] {equipmentSlot} 슬롯 비움");
        }
    }
    
    /// <summary>
    /// 아이템 장착 시 이벤트 처리
    /// </summary>
    private void OnItemEquipped(EquipmentSlot slot, EquipmentData item)
    {
        // 해당 슬롯만 업데이트
        var equippedItems = PlayerDataManager.Instance.EquippedItems;
        
        switch (slot)
        {
            case EquipmentSlot.MainWeapon:
                UpdateSlot(weaponSlot, slot, equippedItems);
                break;
            case EquipmentSlot.Armor:
                UpdateSlot(armorSlot, slot, equippedItems);
                break;
            case EquipmentSlot.Boots:
                UpdateSlot(bootsSlot, slot, equippedItems);
                break;
            case EquipmentSlot.Helmet:
                UpdateSlot(helmetSlot, slot, equippedItems);
                break;
            case EquipmentSlot.Shield:
                UpdateSlot(shieldSlot, slot, equippedItems);
                break;
            case EquipmentSlot.Ring1:
                UpdateSlot(ring1Slot, slot, equippedItems);
                break;
            case EquipmentSlot.Ring2:
                UpdateSlot(ring2Slot, slot, equippedItems);
                break;
            case EquipmentSlot.Necklace:
                UpdateSlot(necklaceSlot, slot, equippedItems);
                break;
        }
        
        if (showDebugLogs)
            Debug.Log($"🎮 [EquippedItemsUI] {slot} 슬롯에 {item.equipmentName} 장착 표시");
    }
    
    /// <summary>
    /// 아이템 해제 시 이벤트 처리
    /// </summary>
    private void OnItemUnequipped(EquipmentSlot slot, EquipmentData item)
    {
        // 해당 슬롯 비우기
        switch (slot)
        {
            case EquipmentSlot.MainWeapon:
                if (weaponSlot != null) weaponSlot.SetEquipmentData(null);
                break;
            case EquipmentSlot.Armor:
                if (armorSlot != null) armorSlot.SetEquipmentData(null);
                break;
            case EquipmentSlot.Boots:
                if (bootsSlot != null) bootsSlot.SetEquipmentData(null);
                break;
            case EquipmentSlot.Helmet:
                if (helmetSlot != null) helmetSlot.SetEquipmentData(null);
                break;
            case EquipmentSlot.Shield:
                if (shieldSlot != null) shieldSlot.SetEquipmentData(null);
                break;
            case EquipmentSlot.Ring1:
                if (ring1Slot != null) ring1Slot.SetEquipmentData(null);
                break;
            case EquipmentSlot.Ring2:
                if (ring2Slot != null) ring2Slot.SetEquipmentData(null);
                break;
            case EquipmentSlot.Necklace:
                if (necklaceSlot != null) necklaceSlot.SetEquipmentData(null);
                break;
        }
        
        if (showDebugLogs)
            Debug.Log($"🎮 [EquippedItemsUI] {slot} 슬롯에서 {item.equipmentName} 해제 표시");
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnItemEquipped -= OnItemEquipped;
            PlayerDataManager.Instance.OnItemUnequipped -= OnItemUnequipped;
        }
        
        // 🆕 클릭 이벤트 해제
        RemoveSlotClickEvents();
    }
    
    /// <summary>
    /// 🗑️ 모든 슬롯 클릭 이벤트 해제
    /// </summary>
    private void RemoveSlotClickEvents()
    {
        RemoveSlotClickEvent(weaponSlot);
        RemoveSlotClickEvent(armorSlot);
        RemoveSlotClickEvent(bootsSlot);
        RemoveSlotClickEvent(helmetSlot);
        RemoveSlotClickEvent(shieldSlot);
        RemoveSlotClickEvent(ring1Slot);
        RemoveSlotClickEvent(ring2Slot);
        RemoveSlotClickEvent(necklaceSlot);
    }
    
    /// <summary>
    /// 🗑️ 개별 슬롯 클릭 이벤트 해제
    /// </summary>
    private void RemoveSlotClickEvent(InventorySlot slot)
    {
        if (slot == null) return;
        
        var button = slot.GetComponent<UnityEngine.UI.Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
        }
    }
}
