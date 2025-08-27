using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 🏠 로비 전용 착용 장비 UI 시스템
/// EquippedItemsUI 기반, 로비 환경에 최적화 (PlayerRuntimeStats 없이 작동)
/// </summary>
public class LobbyEquippedItemsUI : MonoBehaviour
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
    [SerializeField] private TMP_Text playerNameText;           // 캐릭터명
    [SerializeField] private Image playerClassIcon;             // 클래스 이미지
    [SerializeField] private TMP_Text finalAttackDamageText;    // 최종 공격력
    [SerializeField] private TMP_Text finalDefenseText;         // 최종 방어력  
    [SerializeField] private TMP_Text finalAttackSpeedText;     // 최종 공격속도
    [SerializeField] private TMP_Text finalMoveSpeedText;       // 최종 이동속도

    [Header("🎨 클래스별 아이콘")]
    [SerializeField] private Sprite warriorClassIcon;
    [SerializeField] private Sprite assassinClassIcon; 
    [SerializeField] private Sprite wizardClassIcon;
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    private void Start()
    {
        // 🔧 로비에서만 활성화
        if (SceneManager.GetActiveScene().name != "Lobby" && 
            !SceneManager.GetActiveScene().name.Contains("Lobby"))
        {
            this.enabled = false;
            if (showDebugLogs)
                Debug.Log("🔒 [LobbyEquippedItemsUI] 인게임에서 비활성화됨");
            return;
        }
        
        InitializeLobbyEquippedItems();
    }
    
    /// <summary>
    /// 🏠 로비 전용 착용 장비 시스템 초기화 (지연 갱신 지원)
    /// </summary>
    private void InitializeLobbyEquippedItems()
    {
        // PlayerDataManager 이벤트 구독 (지연 갱신 지원)
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnItemEquipped += OnItemEquipped;
            PlayerDataManager.Instance.OnItemUnequipped += OnItemUnequipped;
            PlayerDataManager.Instance.OnLevelChanged += OnPlayerLevelChanged;
            
            // 🔧 지연 갱신: OnSlotSelected 이벤트 구독을 조건부로 변경
            // 캐릭터 정보창이 활성화된 상태에서만 실시간 갱신
            // PlayerDataManager.Instance.OnSlotSelected += OnPlayerSlotChanged; // 제거
            
            // 🆕 지연 로드 완료 이벤트 구독
            PlayerDataManager.Instance.OnSlotLazyLoaded += OnSlotLazyLoadedForEquippedItems;
        }
        
        // 각 슬롯의 클릭 이벤트 구독
        SetupSlotClickEvents();
        
        // 초기 정보 표시 (지연 갱신 지원)
        StartCoroutine(InitializeEquippedItemsWithLazyLoad());
        
        if (showDebugLogs)
            Debug.Log("🏠 [LobbyEquippedItemsUI] 로비 착용 장비 시스템 초기화 완료 (지연 갱신 지원)");
    }

    /// <summary>
    /// 🆕 지연 로드 지원으로 착용 장비 초기화
    /// </summary>
    private IEnumerator InitializeEquippedItemsWithLazyLoad()
    {
        // 캐릭터 데이터 로드 상태 확인
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsLazyLoadRequired())
        {
            Debug.Log("🔄 [LobbyEquippedItemsUI] 지연 로드 필요 - 캐릭터 데이터 로드 중...");
            
            int selectedSlot = PlayerDataManager.Instance.GetSelectedSlotIndex();
            bool loadSuccess = PlayerDataManager.Instance.LazyLoadSlotData(selectedSlot);
            
            if (!loadSuccess)
            {
                Debug.LogError("❌ [LobbyEquippedItemsUI] 캐릭터 데이터 로드 실패");
                yield break;
            }
            
            yield return new WaitForSeconds(0.1f); // 로드 완료 대기
        }
        
        // 초기 정보 표시
        UpdatePlayerInfo();
        RefreshAllEquippedItems();
        UpdatePlayerStats();
    }

    /// <summary>
    /// 🆕 지연 로드 완료 시 착용 장비 갱신
    /// </summary>
    private void OnSlotLazyLoadedForEquippedItems(int slotIndex)
    {
        if (showDebugLogs)
            Debug.Log($"🔄 [LobbyEquippedItemsUI] 슬롯 {slotIndex} 지연 로드 완료 - 착용 장비 갱신");
        
        // 캐릭터 정보창이 활성화된 상태에서만 갱신
        if (gameObject.activeInHierarchy)
        {
            UpdatePlayerInfo();
            RefreshAllEquippedItems();
            UpdatePlayerStats();
        }
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
            Debug.Log("🖱️ [LobbyEquippedItemsUI] 모든 슬롯 클릭 이벤트 설정 완료");
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
                Debug.Log($"🖱️ [LobbyEquippedItemsUI] {equipmentSlot} 슬롯 클릭 이벤트 연결");
        }
        else
        {
            Debug.LogWarning($"⚠️ [LobbyEquippedItemsUI] {equipmentSlot} 슬롯에 Button 컴포넌트가 없습니다!");
        }
    }
    
    /// <summary>
    /// 🖱️ 착용 장비 슬롯 클릭 시 처리 (장비 해제)
    /// </summary>
    private void OnEquippedSlotClicked(EquipmentSlot slot)
    {
        if (showDebugLogs)
            Debug.Log($"🖱️ [LobbyEquippedItemsUI] {slot} 슬롯 클릭됨");
        
        // PlayerDataManager에서 해당 슬롯의 장비 확인
        if (PlayerDataManager.Instance == null) return;
        
        var equippedItems = PlayerDataManager.Instance.EquippedItems;
        if (equippedItems.ContainsKey(slot) && equippedItems[slot] != null)
        {
            var equippedItem = equippedItems[slot];
            
            if (showDebugLogs)
                Debug.Log($"🖱️ [LobbyEquippedItemsUI] {slot}에서 {equippedItem.equipmentName} 해제 시도");
            
            // 장비 해제 실행
            bool success = PlayerDataManager.Instance.UnequipItem(slot);
            
            if (success)
            {
                if (showDebugLogs)
                    Debug.Log($"✅ [LobbyEquippedItemsUI] {equippedItem.equipmentName} 해제 성공");
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning($"⚠️ [LobbyEquippedItemsUI] {equippedItem.equipmentName} 해제 실패 (인벤토리가 가득참?)");
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"🖱️ [LobbyEquippedItemsUI] {slot} 슬롯이 비어있습니다");
        }
    }
    
    /// <summary>
    /// 장비 착용 이벤트 처리 (🔧 올바른 시그니처)
    /// </summary>
    private void OnItemEquipped(EquipmentSlot slot, EquipmentData item)
    {
        if (showDebugLogs)
            Debug.Log($"🎮 [LobbyEquippedItemsUI] {item.equipmentName} 장착됨 → {slot}");
        
        RefreshAllEquippedItems();
        UpdatePlayerStats();
    }
    
    /// <summary>
    /// 장비 해제 이벤트 처리 (🔧 올바른 시그니처)
    /// </summary>
    private void OnItemUnequipped(EquipmentSlot slot, EquipmentData item)
    {
        if (showDebugLogs)
            Debug.Log($"🎮 [LobbyEquippedItemsUI] {item.equipmentName} 해제됨 ← {slot}");
        
        RefreshAllEquippedItems();
        UpdatePlayerStats();
    }
    
    /// <summary>
    /// 플레이어 레벨 변경 이벤트 처리
    /// </summary>
    private void OnPlayerLevelChanged(int newLevel)
    {
        UpdatePlayerInfo();
        UpdatePlayerStats();
    }
    
    /// <summary>
    /// 🎮 플레이어 정보 업데이트 (이름, 클래스 아이콘)
    /// </summary>
    private void UpdatePlayerInfo()
    {
        if (GameManager.Instance?.selectedPlayerData == null) return;
        
        var playerData = GameManager.Instance.selectedPlayerData;
        
        // 플레이어명 업데이트
        if (playerNameText != null)
        {
            playerNameText.text = playerData.playerName;
            if (showDebugLogs)
                Debug.Log($"🎮 [LobbyEquippedItemsUI] 플레이어명 업데이트: {playerData.playerName}");
        }
        
        // 클래스 아이콘 업데이트
        if (playerClassIcon != null)
        {
            Sprite classIcon = GetClassIcon(playerData.selectedPlayerType);
            if (classIcon != null)
            {
                playerClassIcon.sprite = classIcon;
                playerClassIcon.color = Color.white;
                if (showDebugLogs)
                    Debug.Log($"🎮 [LobbyEquippedItemsUI] 클래스 아이콘 업데이트: {playerData.selectedPlayerType}");
            }
        }
    }
    
    /// <summary>
    /// 🎨 클래스별 아이콘 가져오기
    /// </summary>
    private Sprite GetClassIcon(PlayerType playerType)
    {
        switch (playerType)
        {
            case PlayerType.Warrior: return warriorClassIcon;
            case PlayerType.Assasin: return assassinClassIcon;
            case PlayerType.Wizard: return wizardClassIcon;
            default: return null;
        }
    }
    
    /// <summary>
    /// 🎮 모든 착용 장비 슬롯 새로고침
    /// </summary>
    private void RefreshAllEquippedItems()
    {
        if (PlayerDataManager.Instance == null) return;
        
        var equippedItems = PlayerDataManager.Instance.EquippedItems;
        
        // 각 슬롯 업데이트
        UpdateSlot(weaponSlot, EquipmentSlot.MainWeapon, equippedItems);
        UpdateSlot(armorSlot, EquipmentSlot.Armor, equippedItems);
        UpdateSlot(bootsSlot, EquipmentSlot.Boots, equippedItems);
        UpdateSlot(helmetSlot, EquipmentSlot.Helmet, equippedItems);
        UpdateSlot(shieldSlot, EquipmentSlot.Shield, equippedItems);
        UpdateSlot(ring1Slot, EquipmentSlot.Ring1, equippedItems);
        UpdateSlot(ring2Slot, EquipmentSlot.Ring2, equippedItems);
        UpdateSlot(necklaceSlot, EquipmentSlot.Necklace, equippedItems);
        
        if (showDebugLogs)
            Debug.Log($"🎮 [LobbyEquippedItemsUI] 모든 착용 장비 슬롯 새로고침 완료");
    }
    
    /// <summary>
    /// 개별 슬롯 업데이트
    /// </summary>
    private void UpdateSlot(InventorySlot uiSlot, EquipmentSlot equipmentSlot, Dictionary<EquipmentSlot, EquipmentData> equippedItems)
    {
        if (uiSlot == null) return;
        
        if (equippedItems.ContainsKey(equipmentSlot) && equippedItems[equipmentSlot] != null)
        {
            // 장비가 있는 경우
            var equipment = equippedItems[equipmentSlot];
            uiSlot.SetEquipmentData(equipment);
            
            if (showDebugLogs)
                Debug.Log($"🎮 [LobbyEquippedItemsUI] {equipmentSlot} 슬롯 업데이트: {equipment.equipmentName}");
        }
        else
        {
            // 장비가 없는 경우
            uiSlot.SetEquipmentData(null);
            
            if (showDebugLogs)
                Debug.Log($"🎮 [LobbyEquippedItemsUI] {equipmentSlot} 슬롯 비움");
        }
    }
    
    /// <summary>
    /// 🔍 UI 요소들이 제대로 연결되었는지 확인 (인게임 EquippedItemsUI와 동일)
    /// </summary>
    private bool CheckUIElements()
    {
        bool isValid = true;
        
        if (finalAttackDamageText == null)
        {
            Debug.LogWarning("⚠️ [LobbyEquippedItemsUI] finalAttackDamageText가 연결되지 않았습니다!");
            isValid = false;
        }
        
        if (finalDefenseText == null)
        {
            Debug.LogWarning("⚠️ [LobbyEquippedItemsUI] finalDefenseText가 연결되지 않았습니다!");
            isValid = false;
        }
        
        if (finalAttackSpeedText == null)
        {
            Debug.LogWarning("⚠️ [LobbyEquippedItemsUI] finalAttackSpeedText가 연결되지 않았습니다!");
            isValid = false;
        }
        
        if (finalMoveSpeedText == null)
        {
            Debug.LogWarning("⚠️ [LobbyEquippedItemsUI] finalMoveSpeedText가 연결되지 않았습니다!");
            isValid = false;
        }
        
        return isValid;
    }
    
    /// <summary>
    /// 🎮 로비 전용 능력치 업데이트 (PlayerRuntimeStats 없이)
    /// </summary>
    private void UpdatePlayerStats()
    {
        if (PlayerDataManager.Instance == null) return;
        
        // 🆕 UI 요소 null 체크 강화
        bool hasValidUI = CheckUIElements();
        if (!hasValidUI) 
        {
            Debug.LogWarning("⚠️ [LobbyEquippedItemsUI] UI 요소가 연결되지 않아 능력치 업데이트를 건너뜁니다!");
            return;
        }
        
        // 🆕 로비에서는 직접 계산 (PlayerRuntimeStats 의존성 제거)
        var stats = CalculateLobbyPlayerStats();
        
        // 🔧 인게임과 동일한 형식으로 UI 업데이트
        if (finalAttackDamageText != null)
            finalAttackDamageText.text = $"공격력: {stats.attackDamage:F0}";
            
        if (finalDefenseText != null)
            finalDefenseText.text = $"방어력: {stats.defense:F0}";
            
        if (finalAttackSpeedText != null)
            finalAttackSpeedText.text = $"공속: {stats.attackSpeed:F1}";
            
        if (finalMoveSpeedText != null)
            finalMoveSpeedText.text = $"이속: {stats.moveSpeed:F1}";
        
        if (showDebugLogs)
            Debug.Log($"🎮 [LobbyEquippedItemsUI] 능력치 업데이트 완료 - 공격력:{stats.attackDamage:F0}, 방어력:{stats.defense:F0}, 공속:{stats.attackSpeed:F1}, 이속:{stats.moveSpeed:F1}");
    }
    
    /// <summary>
    /// 🧮 로비 전용 플레이어 능력치 계산
    /// </summary>
    private (float attackDamage, float defense, float attackSpeed, float moveSpeed) CalculateLobbyPlayerStats()
    {
        var equippedItems = PlayerDataManager.Instance.EquippedItems;
        var playerData = GameManager.Instance?.selectedPlayerData;
        
        // 🔧 currentLevel 사용
        float baseAttack = playerData != null ? playerData.currentLevel * 10f : 50f;
        float baseDefense = playerData != null ? playerData.currentLevel * 5f : 25f;
        float baseAttackSpeed = 1.0f;
        float baseMoveSpeed = 4.0f;
        
        // 장비 보너스 계산
        float equipmentAttack = 0f;
        float equipmentDefense = 0f;
        float equipmentAttackSpeed = 0f;
        float equipmentMoveSpeed = 0f;
        
        foreach (var kvp in equippedItems)
        {
            var equipment = kvp.Value;
            if (equipment == null) continue;
            
            equipmentAttack += equipment.attackDamage;
            equipmentDefense += equipment.defenseBonus;
            equipmentAttackSpeed += equipment.attackSpeed;
            equipmentMoveSpeed += equipment.speedBonus;
        }
        
        return (
            attackDamage: baseAttack + equipmentAttack,
            defense: baseDefense + equipmentDefense,
            attackSpeed: baseAttackSpeed + equipmentAttackSpeed,
            moveSpeed: baseMoveSpeed + equipmentMoveSpeed
        );
    }
    
    /// <summary>
    /// 🆕 플레이어 슬롯 전환 이벤트 처리 (지연 갱신 지원)
    /// </summary>
    private void OnPlayerSlotChanged(int newSlotIndex)
    {
        // 🔧 지연 갱신: 캐릭터 정보창이 활성화된 상태에서만 즉시 갱신
        if (!gameObject.activeInHierarchy)
        {
            if (showDebugLogs)
                Debug.Log($"🔄 [LobbyEquippedItemsUI] 캐릭터 정보창 비활성화 상태 - 갱신 지연");
            return;
        }
        
        if (showDebugLogs)
            Debug.Log($"🔄 [LobbyEquippedItemsUI] 플레이어 슬롯 전환됨: {newSlotIndex}");
        
        // 플레이어 정보 및 장비 정보 전체 갱신
        UpdatePlayerInfo();
        RefreshAllEquippedItems();
        UpdatePlayerStats();
        
        if (showDebugLogs)
            Debug.Log($"✅ [LobbyEquippedItemsUI] 슬롯 {newSlotIndex} 전환 완료");
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제 (지연 갱신 지원)
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnItemEquipped -= OnItemEquipped;
            PlayerDataManager.Instance.OnItemUnequipped -= OnItemUnequipped;
            PlayerDataManager.Instance.OnLevelChanged -= OnPlayerLevelChanged;
            // PlayerDataManager.Instance.OnSlotSelected -= OnPlayerSlotChanged; // 제거
            PlayerDataManager.Instance.OnSlotLazyLoaded -= OnSlotLazyLoadedForEquippedItems; // 🆕 추가
        }
    }

    /// <summary>
    /// 🆕 외부에서 호출 가능한 강제 갱신 메서드 (Z-Order 방식 지원)
    /// </summary>
    public void ForceRefreshEquippedItems()
    {
        if (showDebugLogs)
            Debug.Log("🔄 [LobbyEquippedItemsUI] 강제 갱신 시작");
        
        // 지연 로드가 필요한 경우 처리
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsLazyLoadRequired())
        {
            int selectedSlot = PlayerDataManager.Instance.GetSelectedSlotIndex();
            bool loadSuccess = PlayerDataManager.Instance.LazyLoadSlotData(selectedSlot);
            
            if (!loadSuccess)
            {
                Debug.LogError("❌ [LobbyEquippedItemsUI] 강제 갱신 시 데이터 로드 실패");
                return;
            }
        }
        
        // 모든 정보 갱신
        UpdatePlayerInfo();
        RefreshAllEquippedItems();
        UpdatePlayerStats();
        
        if (showDebugLogs)
            Debug.Log("✅ [LobbyEquippedItemsUI] 강제 갱신 완료");
    }

    /// <summary>
    /// 🆕 빈 슬롯 상태 표시 (모든 장비 슬롯 비우기)
    /// </summary>
    public void ShowEmptySlotState()
    {
        if (showDebugLogs)
            Debug.Log("🔄 [LobbyEquippedItemsUI] 빈 슬롯 상태로 전환");
        
        // 모든 장비 슬롯을 빈 상태로 설정
        ClearSlot(weaponSlot);
        ClearSlot(armorSlot);
        ClearSlot(bootsSlot);
        ClearSlot(helmetSlot);
        ClearSlot(shieldSlot);
        ClearSlot(ring1Slot);
        ClearSlot(ring2Slot);
        ClearSlot(necklaceSlot);
        
        // 플레이어 정보도 빈 상태로 설정
        if (playerNameText != null)
            playerNameText.text = "빈 슬롯";
            
        if (playerClassIcon != null)
            playerClassIcon.sprite = null;
            
        // 스탯 정보도 초기화
        if (finalAttackDamageText != null)
            finalAttackDamageText.text = "-";
            
        if (finalDefenseText != null)
            finalDefenseText.text = "-";
            
        if (finalAttackSpeedText != null)
            finalAttackSpeedText.text = "-";
            
        if (finalMoveSpeedText != null)
            finalMoveSpeedText.text = "-";
        
        if (showDebugLogs)
            Debug.Log("✅ [LobbyEquippedItemsUI] 빈 슬롯 상태 표시 완료");
    }

    /// <summary>
    /// 🆕 개별 슬롯 비우기
    /// </summary>
    private void ClearSlot(InventorySlot slot)
    {
        if (slot != null)
        {
            slot.SetEquipmentData(null);
        }
    }
}