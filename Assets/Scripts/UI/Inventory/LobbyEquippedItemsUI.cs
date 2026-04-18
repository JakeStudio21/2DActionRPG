using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UI.Popups; // ⭐ ItemDetailPopup

/// <summary>
/// 🏠 로비 전용 착용 장비 UI 시스템
/// EquippedItemsUI 기반, 로비 환경에 최적화 (PlayerRuntimeStats 없이 작동)
/// </summary>
public class LobbyEquippedItemsUI : MonoBehaviour
{
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
    [SerializeField] private TMP_Text playerLevelText;          // 캐릭터 레벨 (이름 왼쪽)
    [SerializeField] private TMP_Text playerNameText;           // 캐릭터명
    [SerializeField] private Image playerClassIcon;             // 클래스 이미지

    [Header("📊 기본 스탯 패널")]
    [SerializeField] private TMP_Text finalAttackDamageText;    // 최종 공격력
    [SerializeField] private TMP_Text finalMaxHealthText;       // 최대 체력
    [SerializeField] private TMP_Text finalDefenseText;         // 최종 방어력
    [SerializeField] private TMP_Text finalCritRateText;        // 크리티컬 확률
    [SerializeField] private TMP_Text finalCritDmgText;         // 크리티컬 데미지
    [SerializeField] private TMP_Text finalAttackSpeedText;     // 최종 공격속도
    [SerializeField] private TMP_Text finalMoveSpeedText;       // 최종 이동속도
    [SerializeField] private TMP_Text finalHealMultText;        // 회복 효율

    [Header("📊 심화 스탯 패널")]
    [SerializeField] private TMP_Text finalSkillDmgText;        // 스킬 피해 증가
    [SerializeField] private TMP_Text finalCdrText;             // 쿨다운 감소
    [SerializeField] private TMP_Text finalLifeStealText;       // 흡혈
    [SerializeField] private TMP_Text finalArmorPenText;        // 방어구 관통
    [SerializeField] private TMP_Text finalDmgReductionText;    // 받는 피해 감소
    [SerializeField] private TMP_Text finalHpRegenText;         // 초당 체력 회복
    [SerializeField] private TMP_Text finalDodgeText;           // 회피 확률
    [SerializeField] private TMP_Text finalBlockText;           // 블록 확률
    [SerializeField] private TMP_Text finalStatusResistText;    // 상태이상 저항
    [SerializeField] private TMP_Text finalPierceRetentionText; // 관통 데미지 유지율
    [SerializeField] private TMP_Text finalExpGainText;         // 경험치 획득 증가

    [Header("🎨 클래스별 아이콘")]
    [SerializeField] private Sprite warriorClassIcon;
    [SerializeField] private Sprite assassinClassIcon; 
    [SerializeField] private Sprite wizardClassIcon;
    
    [Header("📜 스크롤")]
    [SerializeField] private ScrollRect statScrollRect; // 스탯 스크롤 뷰 — 갱신 시 최상단 복귀용

    [Header("🔒 읽기 전용 모드")]
    [SerializeField] private bool isReadOnly = false; // true: 캐릭터 정보창 (읽기 전용), false: 인벤토리창 (읽기/쓰기)

    /// <summary>
    /// 읽기 전용 모드 설정 (CharacterInfoUI에서 호출)
    /// </summary>
    public void SetReadOnly(bool value) => isReadOnly = value;
    
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
        // 각 슬롯에 클릭 이벤트 연결 (9개 슬롯)
        SetupSlotClickEvent(weaponSlot, EquipmentSlot.MainWeapon);
        SetupSlotClickEvent(helmetSlot, EquipmentSlot.Helmet);
        SetupSlotClickEvent(armorSlot, EquipmentSlot.Armor);
        SetupSlotClickEvent(glovesSlot, EquipmentSlot.Gloves);
        SetupSlotClickEvent(bootsSlot, EquipmentSlot.Boots);
        SetupSlotClickEvent(beltSlot, EquipmentSlot.Belt);
        SetupSlotClickEvent(ring1Slot, EquipmentSlot.Ring1);
        SetupSlotClickEvent(ring2Slot, EquipmentSlot.Ring2);
        SetupSlotClickEvent(necklaceSlot, EquipmentSlot.Necklace);
        
        if (showDebugLogs)
            Debug.Log("🖱️ [LobbyEquippedItemsUI] 9개 슬롯 클릭 이벤트 설정 완료");
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
            // 🚨 Inspector Persistent Listener 확인 (equipButton 버그와 동일한 문제일 수 있음)
            var persistentEventCount = button.onClick.GetPersistentEventCount();
            if (persistentEventCount > 0)
            {
                Debug.LogError($"⚠️⚠️⚠️ [LobbyEquippedItemsUI] {equipmentSlot} 슬롯에 Inspector Persistent Listener {persistentEventCount}개 발견!");
                Debug.LogError($"⚠️ Unity Inspector에서 {slot.gameObject.name} > Button > On Click () 이벤트를 제거해주세요!");
                for (int i = 0; i < persistentEventCount; i++)
                {
                    var targetObj = button.onClick.GetPersistentTarget(i);
                    var methodName = button.onClick.GetPersistentMethodName(i);
                    Debug.LogError($"   [{i}] Target: {targetObj?.GetType().Name}, Method: {methodName}");
                }
            }
            
            // 기존 클릭 이벤트 제거 (코드로 추가된 것만 제거됨)
            button.onClick.RemoveAllListeners();
            
            // ⭐ ReadOnly 모드든 아니든 항상 클릭 이벤트 연결
            // OnEquippedSlotClicked() 내부에서 ReadOnly 체크하여 분기 처리
            button.onClick.AddListener(() => OnEquippedSlotClicked(equipmentSlot));
            button.interactable = true;
            
            if (showDebugLogs)
            {
                string mode = isReadOnly ? "읽기 전용 (정보 보기)" : "읽기/쓰기 (장비 해제)";
                Debug.Log($"🖱️ [LobbyEquippedItemsUI] {equipmentSlot} 슬롯 클릭 이벤트 연결: {mode}");
            }
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
            Debug.Log($"🖱️ [LobbyEquippedItemsUI] {slot} 슬롯 클릭됨 (ReadOnly: {isReadOnly})");
        
        // PlayerDataManager에서 해당 슬롯의 장비 확인
        if (PlayerDataManager.Instance == null) return;
        
        var equippedItems = PlayerDataManager.Instance.EquippedItems;
        if (equippedItems.ContainsKey(slot) && equippedItems[slot] != null)
        {
            var equippedItem = equippedItems[slot];
            
            if (isReadOnly)
            {
                // ⭐ 캐릭터 정보창: 읽기 전용 (ReadOnly 컨텍스트)
                ShowItemDetailPopupReadOnly(equippedItem, slot);
            }
            else
            {
                // ⭐ 로비 인벤토리: 해제 가능 (Equipment 컨텍스트)
                ShowItemDetailPopupEquipment(equippedItem, slot);
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"🖱️ [LobbyEquippedItemsUI] {slot} 슬롯이 비어있습니다");
        }
    }
    
    /// <summary>
    /// ⭐ 아이템 상세 팝업 표시 (읽기 전용 모드)
    /// </summary>
    private void ShowItemDetailPopupReadOnly(EquipmentData equipmentData, EquipmentSlot slot)
    {
        if (equipmentData == null)
        {
            Debug.LogWarning("⚠️ [LobbyEquippedItemsUI] 빈 슬롯입니다.");
            return;
        }
        
        // PopupCanvas에서 ItemDetailPopup 찾기
        var popup = FindObjectOfType<ItemDetailPopup>(true); // includeInactive = true
        
        if (popup == null)
        {
            Debug.LogError("❌ [LobbyEquippedItemsUI] ItemDetailPopup을 찾을 수 없습니다!");
            return;
        }
        
        // ⭐ ReadOnly 컨텍스트로 팝업 열기 — instanceId가 있어야 V2 동적 스탯(공격력/방어력)이 표시됨
        ItemInstanceID instanceId = GetItemInstanceIDForSlot(slot);
        popup.Show(equipmentData, ItemDetailContext.ReadOnly, -1, instanceId);
        
        if (showDebugLogs)
            Debug.Log($"📖 [LobbyEquippedItemsUI] ItemDetailPopup 열기: {equipmentData.equipmentName} (읽기 전용)");
    }
    
    /// <summary>
    /// ⭐ 아이템 상세 팝업 표시 (장비 해제 모드)
    /// </summary>
    private void ShowItemDetailPopupEquipment(EquipmentData equipmentData, EquipmentSlot slot)
    {
        if (equipmentData == null)
        {
            Debug.LogWarning("⚠️ [LobbyEquippedItemsUI] 빈 슬롯입니다.");
            return;
        }
        
        // PopupCanvas에서 ItemDetailPopup 찾기
        var popup = FindObjectOfType<ItemDetailPopup>(true); // includeInactive = true
        
        if (popup == null)
        {
            Debug.LogError("❌ [LobbyEquippedItemsUI] ItemDetailPopup을 찾을 수 없습니다!");
            return;
        }
        
        // ⭐ ItemInstanceID 가져오기
        ItemInstanceID instanceId = GetItemInstanceIDForSlot(slot);
        
        // ⭐ Equipment 컨텍스트로 팝업 열기 (slot 명시 전달 → Ring1/Ring2 정확히 구분)
        popup.Show(equipmentData, ItemDetailContext.Equipment, -1, instanceId, slot);
        
        if (showDebugLogs)
            Debug.Log($"🎒 [LobbyEquippedItemsUI] ItemDetailPopup 열기: {equipmentData.equipmentName} (해제 모드, ID: {(!instanceId.IsEmpty ? instanceId.Value.Substring(0, 8) + "..." : "없음")})");
    }
    
    /// <summary>
    /// ⭐ 슬롯의 ItemInstanceID 가져오기 (V2 시스템)
    /// </summary>
    private ItemInstanceID GetItemInstanceIDForSlot(EquipmentSlot slot)
    {
        var playerData = PlayerDataManager.Instance;
        
        if (playerData == null || playerData.selectedPlayerData == null)
        {
            Debug.LogWarning("⚠️ [LobbyEquippedItemsUI] PlayerDataManager가 null입니다.");
            return default;
        }
        
        // ⭐ V2 시스템: RuntimeEquippedInstanceIds에서 직접 조회
        var equippedInstanceIds = playerData.selectedPlayerData.RuntimeEquippedInstanceIds;
        if (equippedInstanceIds.ContainsKey(slot))
        {
            var instanceId = equippedInstanceIds[slot];
            if (!instanceId.IsEmpty)
            {
                if (showDebugLogs)
                    Debug.Log($"✅ [LobbyEquippedItemsUI] {slot} 슬롯의 ItemInstanceID 찾음: {instanceId.Value.Substring(0, 8)}...");
                return instanceId;
            }
        }
        
        if (showDebugLogs)
            Debug.LogWarning($"⚠️ [LobbyEquippedItemsUI] {slot} 슬롯의 ItemInstanceID를 찾을 수 없습니다.");
        return default;
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
        
        // 레벨 업데이트
        if (playerLevelText != null)
            playerLevelText.text = $"Lv.{playerData.currentLevel}";

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
    /// 🎮 모든 착용 장비 슬롯 새로고침 (9개 슬롯)
    /// </summary>
    private void RefreshAllEquippedItems()
    {
        if (PlayerDataManager.Instance == null) return;
        
        var equippedItems = PlayerDataManager.Instance.EquippedItems;
        
        // 각 슬롯 업데이트 (9개)
        UpdateSlot(weaponSlot, EquipmentSlot.MainWeapon, equippedItems);
        UpdateSlot(helmetSlot, EquipmentSlot.Helmet, equippedItems);
        UpdateSlot(armorSlot, EquipmentSlot.Armor, equippedItems);
        UpdateSlot(glovesSlot, EquipmentSlot.Gloves, equippedItems);
        UpdateSlot(bootsSlot, EquipmentSlot.Boots, equippedItems);
        UpdateSlot(beltSlot, EquipmentSlot.Belt, equippedItems);
        UpdateSlot(ring1Slot, EquipmentSlot.Ring1, equippedItems);
        UpdateSlot(ring2Slot, EquipmentSlot.Ring2, equippedItems);
        UpdateSlot(necklaceSlot, EquipmentSlot.Necklace, equippedItems);
        
        if (showDebugLogs)
            Debug.Log($"🎮 [LobbyEquippedItemsUI] 9개 착용 장비 슬롯 새로고침 완료");
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
    /// 🎮 로비 전용 능력치 업데이트 — PlayerStatComputationService 사용
    /// </summary>
    private void UpdatePlayerStats()
    {
        if (PlayerDataManager.Instance == null) return;

        var playerData = PlayerDataManager.Instance.selectedPlayerData;
        if (playerData == null) return;

        var snap = PlayerStatComputationService.Compute(playerData, playerData.selectedPlayerType);

        // ── 기본 패널 ──
        SetText(finalAttackDamageText, FormatStat("공격력",    $"{snap.AttackDamage:F0}"));
        SetText(finalMaxHealthText,    FormatStat("체력",      $"{snap.MaxHealth:F0}"));
        SetText(finalDefenseText,      FormatStat("방어력",    $"{snap.Defense:F0}"));
        SetText(finalCritRateText,     FormatStat("치명타",    $"{snap.CriticalChance:P1}"));
        SetText(finalCritDmgText,      FormatStat("치명 피해", $"x{snap.CriticalDamage:F2}"));
        SetText(finalAttackSpeedText,  FormatStat("공격속도",  $"{snap.AttackSpeed:F2}"));
        SetText(finalMoveSpeedText,    FormatStat("이동속도",  $"{snap.MoveSpeed:F1}"));
        SetText(finalHealMultText,     FormatStat("회복 효율", $"{snap.HealMultiplier:P0}"));

        // ── 심화 패널 ──
        SetText(finalSkillDmgText,        FormatStat("스킬 피해",    $"+{snap.SkillDamageBonus:P0}"));
        SetText(finalCdrText,             FormatStat("쿨다운 감소",  $"{snap.CooldownReduction:P0}"));
        SetText(finalLifeStealText,       FormatStat("흡혈",         $"{snap.LifeSteal:P0}"));
        SetText(finalArmorPenText,        FormatStat("방어구 관통",  $"{snap.ArmorPenetration:P0}"));
        SetText(finalDmgReductionText,    FormatStat("피해 감소",    $"{snap.DamageReduction:P0}"));
        SetText(finalHpRegenText,         FormatStat("체력 재생",    $"{snap.HpRegen:F1}/s"));
        SetText(finalDodgeText,           FormatStat("회피",         $"{snap.DodgeChance:P0}"));
        SetText(finalBlockText,           FormatStat("블록",         $"{snap.BlockChance:P0}"));
        SetText(finalStatusResistText,    FormatStat("상태저항",     $"{snap.StatusResist:P0}"));
        SetText(finalPierceRetentionText, FormatStat("관통 유지",    $"{snap.PierceDamageRetention:P0}"));
        SetText(finalExpGainText,         FormatStat("경험치 획득",  $"+{snap.ExpGainBonus:P0}"));

        // 스크롤 최상단 복귀 (슬롯 전환 시 심화 패널이 보이는 채로 남지 않도록)
        if (statScrollRect != null)
            statScrollRect.verticalNormalizedPosition = 1f;

        if (showDebugLogs)
            Debug.Log($"🎮 [LobbyEquippedItemsUI] 스탯 업데이트 완료 — ATK:{snap.AttackDamage:F0} DEF:{snap.Defense:F0} SPD:{snap.AttackSpeed:F2} MOV:{snap.MoveSpeed:F1}");
    }

    /// <summary>
    /// 스탯 행 텍스트 포맷 — 라벨과 수치 사이를 공백 패딩으로 채워 정렬감을 만든다.
    /// 예) FormatStat("공격력", "123")  →  "공격력           123"
    /// </summary>
    private static string FormatStat(string label, string value)
    {
        // TMP Rich Text: 라벨은 좌측, 수치는 우측 고정폭 정렬
        // 스크롤 콘텐츠 너비에 맞춰 <margin> 또는 <pos> 태그로 수치를 오른쪽에 배치
        return $"{label}<pos=65%>{value}";
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label != null) label.text = value;
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
    /// 🆕 빈 슬롯 상태 표시 (모든 장비 슬롯 비우기) - 9개 슬롯
    /// </summary>
    public void ShowEmptySlotState()
    {
        if (showDebugLogs)
            Debug.Log("🔄 [LobbyEquippedItemsUI] 빈 슬롯 상태로 전환 (9개)");
        
        // 모든 장비 슬롯을 빈 상태로 설정 (9개)
        ClearSlot(weaponSlot);
        ClearSlot(helmetSlot);
        ClearSlot(armorSlot);
        ClearSlot(glovesSlot);
        ClearSlot(bootsSlot);
        ClearSlot(beltSlot);
        ClearSlot(ring1Slot);
        ClearSlot(ring2Slot);
        ClearSlot(necklaceSlot);
        
        // 플레이어 정보도 빈 상태로 설정
        if (playerLevelText != null)
            playerLevelText.text = "";

        if (playerNameText != null)
            playerNameText.text = "빈 슬롯";
            
        if (playerClassIcon != null)
            playerClassIcon.sprite = null;
            
        // 스탯 정보 초기화 (기본 + 심화 패널 전체)
        TMP_Text[] statTexts = {
            finalAttackDamageText, finalMaxHealthText, finalDefenseText,
            finalCritRateText, finalCritDmgText, finalAttackSpeedText,
            finalMoveSpeedText, finalHealMultText,
            finalSkillDmgText, finalCdrText, finalLifeStealText,
            finalArmorPenText, finalDmgReductionText, finalHpRegenText,
            finalDodgeText, finalBlockText, finalStatusResistText,
            finalPierceRetentionText, finalExpGainText
        };
        foreach (var t in statTexts)
            if (t != null) t.text = "-";
        
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