using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TMPro 네임스페이스 추가

/// <summary>
/// 🗑️ Legacy: 인게임 캐릭터 장비창 UI (더 이상 사용하지 않음)
/// 
/// **변경 이유:**
/// - 이전 구조: 인게임에서 가방 아이템 클릭 → 자동 장착 (Legacy 장비창 사용)
/// - 현재 구조: 인게임에서 가방 아이템 클릭 → InGameItemDetailPanel 표시 (정보 확인만)
/// - 스테이지 클리어 후 자동으로 보관창고로 이동
/// 
/// **히스토리:** 전체 코드는 참고용으로 주석 처리됨
/// 
/// ⚠️ 이 스크립트는 더 이상 사용되지 않으므로 씬에서 제거하거나 GameObject를 비활성화하세요.
/// </summary>
[System.Obsolete("Legacy: 인게임 장비창은 더 이상 사용되지 않습니다. InGameItemDetailPanel을 사용하세요.")]
public class EquippedItemsUI : MonoBehaviour
{
    /* ❌ Legacy 코드 전체 주석 처리 (히스토리 보존용)
    
    [Header("🎒 착용 장비 슬롯들")]
    [SerializeField] private InventorySlot weaponSlot;      // 무기 슬롯
    [SerializeField] private InventorySlot helmetSlot;      // 투구 슬롯
    [SerializeField] private InventorySlot armorSlot;       // 상의 슬롯  
    [SerializeField] private InventorySlot glovesSlot;      // 장갑 슬롯 (신규)
    [SerializeField] private InventorySlot bootsSlot;       // 신발 슬롯
    [SerializeField] private InventorySlot beltSlot;        // 허리띠 슬롯 (신규, Shield→Belt 변경)
    [SerializeField] private InventorySlot ring1Slot;       // 반지1 슬롯
    [SerializeField] private InventorySlot ring2Slot;       // 반지2 슬롯
    [SerializeField] private InventorySlot necklaceSlot;    // 목걸이 슬롯
    
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
            PlayerDataManager.Instance.OnSlotSelected += OnPlayerSlotChanged; // 🔧 수정
        }
        
        // 🆕 PlayerRuntimeStats 준비 이벤트 구독
        PlayerRuntimeStats.OnPlayerRuntimeStatsReady += OnPlayerRuntimeStatsReady;
        
        // 🆕 이미 존재하는 PlayerRuntimeStats 확인 (늦게 초기화되는 경우 대비)
        playerRuntimeStats = FindObjectOfType<PlayerRuntimeStats>();
        if (playerRuntimeStats != null)
        {
            SubscribeToStatsEvents();
        }
        else
        {
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
        }
        else
        {
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
            // finalAttackDamageText.text = $"공격력: {playerRuntimeStats.FinalAttackDamage:F0}";
            finalAttackDamageText.text = $"Attack {playerRuntimeStats.FinalAttackDamage:F0}";
        }
        
        if (finalDefenseText != null)
        {
            // finalDefenseText.text = $"방어력: {playerRuntimeStats.FinalDefense:F0}";
            finalDefenseText.text = $"Defense {playerRuntimeStats.FinalDefense:F0}";
        }
        
        if (finalAttackSpeedText != null)
        {
            // finalAttackSpeedText.text = $"공속: {playerRuntimeStats.FinalAttackSpeed:F1}";
            finalAttackSpeedText.text = $"AttackSpeed {playerRuntimeStats.FinalAttackSpeed:F1}";
        }
        
        if (finalMoveSpeedText != null)
        {
            // finalMoveSpeedText.text = $"이속: {playerRuntimeStats.FinalMoveSpeed:F1}";
            finalMoveSpeedText.text = $"MoveSpeed {playerRuntimeStats.FinalMoveSpeed:F1}";
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
        
        UpdatePlayerInfo();
        UpdatePlayerStats();
        
    }
    
    /// <summary>
    /// 🆕 플레이어 레벨 변경 시 이벤트 처리
    /// </summary>
    private void OnPlayerLevelChanged(int newLevel)
    {
        UpdatePlayerInfo(); // 레벨 변경 시 플레이어 정보 갱신
        UpdatePlayerStats(); // 레벨업으로 인한 스탯 변화 반영
        
    }
    
    /// <summary>
    /// 🆕 플레이어 슬롯 전환 이벤트 처리
    /// </summary>
    private void OnPlayerSlotChanged(int newSlotIndex)
    {
        
        // 플레이어 정보 및 장비 정보 전체 갱신
        UpdatePlayerInfo();
        RefreshAllEquippedItems();
        UpdatePlayerStats();
        
    }
    
    /// <summary>
    /// 모든 착용된 장비 슬롯 새로고침
    /// </summary>
    public void RefreshAllEquippedItems()
    {
        if (PlayerDataManager.Instance == null) return;
        
        var equippedItems = PlayerDataManager.Instance.EquippedItems;
        
        // 각 슬롯별로 업데이트 (9개)
        UpdateSlot(weaponSlot, EquipmentSlot.MainWeapon, equippedItems);
        UpdateSlot(helmetSlot, EquipmentSlot.Helmet, equippedItems);
        UpdateSlot(armorSlot, EquipmentSlot.Armor, equippedItems);
        UpdateSlot(glovesSlot, EquipmentSlot.Gloves, equippedItems);
        UpdateSlot(bootsSlot, EquipmentSlot.Boots, equippedItems);
        UpdateSlot(beltSlot, EquipmentSlot.Belt, equippedItems);
        UpdateSlot(ring1Slot, EquipmentSlot.Ring1, equippedItems);
        UpdateSlot(ring2Slot, EquipmentSlot.Ring2, equippedItems);
        UpdateSlot(necklaceSlot, EquipmentSlot.Necklace, equippedItems);
        
    }
    
    /// <summary>
    /// 🖱️ 각 슬롯의 클릭 이벤트 설정
    /// </summary>
    private void SetupSlotClickEvents()
    {
        // 각 슬롯에 클릭 이벤트 연결 (9개)
        SetupSlotClickEvent(weaponSlot, EquipmentSlot.MainWeapon);
        SetupSlotClickEvent(helmetSlot, EquipmentSlot.Helmet);
        SetupSlotClickEvent(armorSlot, EquipmentSlot.Armor);
        SetupSlotClickEvent(glovesSlot, EquipmentSlot.Gloves);
        SetupSlotClickEvent(bootsSlot, EquipmentSlot.Boots);
        SetupSlotClickEvent(beltSlot, EquipmentSlot.Belt);
        SetupSlotClickEvent(ring1Slot, EquipmentSlot.Ring1);
        SetupSlotClickEvent(ring2Slot, EquipmentSlot.Ring2);
        SetupSlotClickEvent(necklaceSlot, EquipmentSlot.Necklace);
        
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
        
        // PlayerDataManager에서 해당 슬롯의 장비 확인
        if (PlayerDataManager.Instance == null) return;
        
        var equippedItems = PlayerDataManager.Instance.EquippedItems;
        if (equippedItems.ContainsKey(slot) && equippedItems[slot] != null)
        {
            var equippedItem = equippedItems[slot];
            
            
            // 장비 해제 실행
            bool success = PlayerDataManager.Instance.UnequipItem(slot);
            
            if (success)
            {
            }
            else
            {
                    Debug.LogWarning($"⚠️ [EquippedItemsUI] {equippedItem.equipmentName} 해제 실패 (인벤토리가 가득참?)");
            }
        }
        else
        {
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
            
        }
        else
        {
            // 빈 슬롯 표시
            uiSlot.SetEquipmentData(null);
            
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
            case EquipmentSlot.Gloves:
                UpdateSlot(glovesSlot, slot, equippedItems);
                break;
            case EquipmentSlot.Belt:
                UpdateSlot(beltSlot, slot, equippedItems);
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
            case EquipmentSlot.Gloves:
                if (glovesSlot != null) glovesSlot.SetEquipmentData(null);
                break;
            case EquipmentSlot.Belt:
                if (beltSlot != null) beltSlot.SetEquipmentData(null);
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
        
    }
    
    /// <summary>
    /// 🧪 연동 테스트: 이벤트 구독 상태 확인
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestEventSubscriptions()
    {
        
        // PlayerDataManager 이벤트 확인
        if (PlayerDataManager.Instance != null)
        {
        }
        else
        {
            Debug.LogError("❌ [EquippedItemsUI] PlayerDataManager.Instance가 null입니다!");
        }
        
        // PlayerRuntimeStats 이벤트 확인
        if (playerRuntimeStats != null)
        {
            
            // OnStatsRecalculated 구독자 수 확인
            int subscribers = playerRuntimeStats.OnStatsRecalculated?.GetInvocationList().Length ?? 0;
        }
        else
        {
            Debug.LogError("❌ [EquippedItemsUI] PlayerRuntimeStats가 null입니다!");
        }
        
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
        UpdatePlayerInfo();
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
            PlayerDataManager.Instance.OnSlotSelected -= OnPlayerSlotChanged; // 🔧 수정
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
    
    */ // ❌ Legacy 코드 주석 처리 끝
}
