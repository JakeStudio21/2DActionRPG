using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TMPro 네임스페이스 추가

/// <summary>
/// 🎮 착용된 장비들을 표시하는 전용 UI 시스템
/// Weapon / Armor / Boots 슬롯을 개별적으로 관리 + 플레이어 정보 & 실시간 능력치 표시
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
    
    [Header("🎮 플레이어 정보 표시")]
    [SerializeField] private TMP_Text playerNameText;           // 빨간색 1: 캐릭터명
    [SerializeField] private Image playerClassIcon;             // 빨간색 2: 클래스 이미지
    [SerializeField] private TMP_Text finalAttackDamageText;    // 빨간색 3: 최종 공격력
    [SerializeField] private TMP_Text finalDefenseText;         // 빨간색 3: 최종 방어력  
    [SerializeField] private TMP_Text finalAttackSpeedText;     // 빨간색 3: 최종 공격속도
    [SerializeField] private TMP_Text finalMoveSpeedText;       // 빨간색 3: 최종 이동속도

    [Header("🎨 클래스별 아이콘")]
    [SerializeField] private Sprite warriorClassIcon;
    [SerializeField] private Sprite assassinClassIcon; 
    [SerializeField] private Sprite wizardClassIcon;
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 내부 참조
    private PlayerRuntimeStats playerRuntimeStats;
    
    private void Start()
    {
        // PlayerDataManager 이벤트 구독
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnItemEquipped += OnItemEquipped;
            PlayerDataManager.Instance.OnItemUnequipped += OnItemUnequipped;
            PlayerDataManager.Instance.OnLevelChanged += OnPlayerLevelChanged;
        }
        
        // 🆕 PlayerRuntimeStats 준비 이벤트 구독
        PlayerRuntimeStats.OnPlayerRuntimeStatsReady += OnPlayerRuntimeStatsReady;
        
        // 🆕 이미 존재하는 PlayerRuntimeStats 확인 (늦게 초기화되는 경우 대비)
        playerRuntimeStats = FindObjectOfType<PlayerRuntimeStats>();
        if (playerRuntimeStats != null)
        {
            SubscribeToStatsEvents();
            if (showDebugLogs)
                Debug.Log("✅ [EquippedItemsUI] 기존 PlayerRuntimeStats 발견하여 즉시 연결");
        }
        else
        {
            if (showDebugLogs)
                Debug.Log("⏳ [EquippedItemsUI] PlayerRuntimeStats 대기 중...");
        }
        
        // 🆕 각 슬롯의 클릭 이벤트 구독
        SetupSlotClickEvents();
        
        // 초기 정보 표시 (PlayerRuntimeStats 없어도 가능한 것들)
        UpdatePlayerInfo();
        RefreshAllEquippedItems();
        
        // 🆕 PlayerRuntimeStats가 있을 때만 능력치 업데이트
        if (playerRuntimeStats != null)
        {
            UpdatePlayerStats();
        }
    }
    
    /// <summary>
    /// 🎯 PlayerRuntimeStats 준비 완료 시 호출되는 이벤트 핸들러
    /// </summary>
    private void OnPlayerRuntimeStatsReady(PlayerRuntimeStats runtimeStats)
    {
        if (showDebugLogs)
            Debug.Log("🎉 [EquippedItemsUI] PlayerRuntimeStats 준비 완료! 이벤트 연결 시작");
            
        playerRuntimeStats = runtimeStats;
        SubscribeToStatsEvents();
        
        // 즉시 능력치 업데이트
        UpdatePlayerStats();
    }
    
    /// <summary>
    /// 🎯 PlayerRuntimeStats 이벤트 연결
    /// </summary>
    private void SubscribeToStatsEvents()
    {
        if (playerRuntimeStats != null)
        {
            playerRuntimeStats.OnStatsRecalculated += UpdatePlayerStats; // 스탯 변경 시 즉시 업데이트
            if (showDebugLogs)
                Debug.Log("🎯 [EquippedItemsUI] PlayerRuntimeStats 이벤트 연결 완료");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning("⚠️ [EquippedItemsUI] PlayerRuntimeStats를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 🎮 플레이어 기본 정보 업데이트 (이름, 클래스 이미지)
    /// </summary>
    private void UpdatePlayerInfo()
    {
        if (PlayerDataManager.Instance?.selectedPlayerData == null) return;
        
        var playerData = PlayerDataManager.Instance.selectedPlayerData;
        
        // 1. 플레이어명 표시
        if (playerNameText != null)
        {
            playerNameText.text = playerData.playerName;
            if (showDebugLogs)
                Debug.Log($"🎮 [EquippedItemsUI] 플레이어명 업데이트: {playerData.playerName}");
        }
        
        // 2. 클래스 이미지 표시
        if (playerClassIcon != null)
        {
            playerClassIcon.sprite = playerData.selectedPlayerType switch
            {
                PlayerType.Warrior => warriorClassIcon,
                PlayerType.Assasin => assassinClassIcon,
                PlayerType.Wizard => wizardClassIcon,
                _ => null
            };
            
            if (showDebugLogs)
                Debug.Log($"🎮 [EquippedItemsUI] 클래스 아이콘 업데이트: {playerData.selectedPlayerType}");
        }
    }
    
    /// <summary>
    /// 📊 실시간 능력치 업데이트 (공격력, 방어력, 공격속도, 이동속도)
    /// </summary>
    private void UpdatePlayerStats()
    {
        if (playerRuntimeStats == null)
        {
            // 다시 찾기 시도
            playerRuntimeStats = FindObjectOfType<PlayerRuntimeStats>();
            if (playerRuntimeStats == null) 
            {
                if (showDebugLogs)
                    Debug.LogWarning("⚠️ [EquippedItemsUI] PlayerRuntimeStats를 찾을 수 없어 능력치 업데이트 실패!");
                return;
            }
        }
        
        // 🆕 UI 요소 null 체크 강화
        bool hasValidUI = CheckUIElements();
        if (!hasValidUI) return;
        
        // 3. 실시간 능력치 표시
        if (finalAttackDamageText != null)
        {
            finalAttackDamageText.text = $"공격력: {playerRuntimeStats.FinalAttackDamage:F0}";
        }
        
        if (finalDefenseText != null)
        {
            finalDefenseText.text = $"방어력: {playerRuntimeStats.FinalDefense:F0}";
        }
        
        if (finalAttackSpeedText != null)
        {
            finalAttackSpeedText.text = $"공속: {playerRuntimeStats.FinalAttackSpeed:F1}";
        }
        
        if (finalMoveSpeedText != null)
        {
            finalMoveSpeedText.text = $"이속: {playerRuntimeStats.FinalMoveSpeed:F1}";
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"📊 [EquippedItemsUI] 능력치 업데이트 완료 - 공격력:{playerRuntimeStats.FinalAttackDamage:F0}, 방어력:{playerRuntimeStats.FinalDefense:F0}, 공속:{playerRuntimeStats.FinalAttackSpeed:F1}, 이속:{playerRuntimeStats.FinalMoveSpeed:F1}");
        }
    }
    
    /// <summary>
    /// 🔍 UI 요소들이 제대로 연결되었는지 확인
    /// </summary>
    private bool CheckUIElements()
    {
        bool isValid = true;
        
        if (finalAttackDamageText == null)
        {
            Debug.LogWarning("⚠️ [EquippedItemsUI] finalAttackDamageText가 연결되지 않았습니다!");
            isValid = false;
        }
        
        if (finalDefenseText == null)
        {
            Debug.LogWarning("⚠️ [EquippedItemsUI] finalDefenseText가 연결되지 않았습니다!");
            isValid = false;
        }
        
        if (finalAttackSpeedText == null)
        {
            Debug.LogWarning("⚠️ [EquippedItemsUI] finalAttackSpeedText가 연결되지 않았습니다!");
            isValid = false;
        }
        
        if (finalMoveSpeedText == null)
        {
            Debug.LogWarning("⚠️ [EquippedItemsUI] finalMoveSpeedText가 연결되지 않았습니다!");
            isValid = false;
        }
        
        return isValid;
    }
    
    /// <summary>
    /// 🧪 수동 테스트: 능력치 강제 업데이트 (디버깅용)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestForceStatsUpdate()
    {
        Debug.Log("🧪 [EquippedItemsUI] 수동 능력치 업데이트 테스트 시작");
        
        UpdatePlayerInfo();
        UpdatePlayerStats();
        
        Debug.Log("🧪 [EquippedItemsUI] 수동 능력치 업데이트 테스트 완료");
    }
    
    /// <summary>
    /// 🆕 플레이어 레벨 변경 시 이벤트 처리
    /// </summary>
    private void OnPlayerLevelChanged(int newLevel)
    {
        UpdatePlayerInfo(); // 레벨 변경 시 플레이어 정보 갱신
        UpdatePlayerStats(); // 레벨업으로 인한 스탯 변화 반영
        
        if (showDebugLogs)
            Debug.Log($"🆙 [EquippedItemsUI] 플레이어 레벨 변경: {newLevel}");
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
        
        // 🗑️ 제거: 중복 UpdatePlayerStats() 호출 제거
        // PlayerRuntimeStats의 OnStatsRecalculated 이벤트에만 의존
        
        if (showDebugLogs)
            Debug.Log($"🎮 [EquippedItemsUI] {slot} 슬롯에 {item.equipmentName} 장착 완료 - 스탯 업데이트는 OnStatsRecalculated 이벤트에서 처리");
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
        
        // 🗑️ 제거: 중복 UpdatePlayerStats() 호출 제거  
        // PlayerRuntimeStats의 OnStatsRecalculated 이벤트에만 의존
        
        if (showDebugLogs)
            Debug.Log($"🎮 [EquippedItemsUI] {slot} 슬롯에서 {item.equipmentName} 해제 완료 - 스탯 업데이트는 OnStatsRecalculated 이벤트에서 처리");
    }
    
    /// <summary>
    /// 🧪 연동 테스트: 이벤트 구독 상태 확인
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestEventSubscriptions()
    {
        Debug.Log("🧪 [EquippedItemsUI] === 이벤트 구독 상태 테스트 ===");
        
        // PlayerDataManager 이벤트 확인
        if (PlayerDataManager.Instance != null)
        {
            Debug.Log("✅ [EquippedItemsUI] PlayerDataManager.Instance 연결됨");
        }
        else
        {
            Debug.LogError("❌ [EquippedItemsUI] PlayerDataManager.Instance가 null입니다!");
        }
        
        // PlayerRuntimeStats 이벤트 확인
        if (playerRuntimeStats != null)
        {
            Debug.Log("✅ [EquippedItemsUI] PlayerRuntimeStats 연결됨");
            
            // OnStatsRecalculated 구독자 수 확인
            int subscribers = playerRuntimeStats.OnStatsRecalculated?.GetInvocationList().Length ?? 0;
            Debug.Log($"📊 [EquippedItemsUI] OnStatsRecalculated 구독자 수: {subscribers}");
        }
        else
        {
            Debug.LogError("❌ [EquippedItemsUI] PlayerRuntimeStats가 null입니다!");
        }
        
        Debug.Log("🧪 [EquippedItemsUI] === 테스트 완료 ===");
    }

    // Inspector에서 테스트할 수 있는 메서드들
    #if UNITY_EDITOR
    /// <summary>
    /// Inspector에서 호출할 수 있는 테스트 메서드들
    /// </summary>
    [ContextMenu("🧪 수동 업데이트 테스트")]
    private void TestManualUpdate()
    {
        TestForceStatsUpdate();
    }
    
    [ContextMenu("🔗 이벤트 연결 테스트")]  
    private void TestEventConnections()
    {
        TestEventSubscriptions();
    }
    
    [ContextMenu("🎮 플레이어 정보 갱신")]
    private void TestPlayerInfoRefresh()
    {
        Debug.Log("🧪 [EquippedItemsUI] 플레이어 정보 갱신 테스트");
        UpdatePlayerInfo();
        Debug.Log("🧪 [EquippedItemsUI] 플레이어 정보 갱신 완료");
    }
    #endif
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnItemEquipped -= OnItemEquipped;
            PlayerDataManager.Instance.OnItemUnequipped -= OnItemUnequipped;
            PlayerDataManager.Instance.OnLevelChanged -= OnPlayerLevelChanged;
        }
        
        // 🆕 정적 이벤트 구독 해제
        PlayerRuntimeStats.OnPlayerRuntimeStatsReady -= OnPlayerRuntimeStatsReady;
        
        // PlayerRuntimeStats 이벤트 해제
        if (playerRuntimeStats != null)
        {
            playerRuntimeStats.OnStatsRecalculated -= UpdatePlayerStats;
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
