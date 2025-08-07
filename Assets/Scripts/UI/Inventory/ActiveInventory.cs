using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ActiveInventory : MonoBehaviour
{
    private int activeSlotIndexNum = 0;

    [Header("🎒 인벤토리 연동")]
    [SerializeField] private bool useDynamicInventory = true; // 동적 인벤토리 사용 여부
    [SerializeField] private int maxDisplaySlots = 16; // 표시할 최대 슬롯 수

    [Header("📊 디버그")]
    [SerializeField] 
    #pragma warning disable 0414
    private bool showDebugLogs = true; // 사용하도록 수정
    #pragma warning restore 0414

    private PlayerControls playerControls;

    private void Awake() {
        playerControls = new PlayerControls();
    }

    private void OnEnable() {
        // playerControls.Enable(); // 키보드 입력을 비활성화하므로 주석 처리
        // playerControls.Inventory.Keyboard.performed += ToggleActiveSlot; // 키보드 입력을 비활성화하므로 주석 처리
    }

    private void OnDisable() {
        // playerControls.Inventory.Keyboard.performed -= ToggleActiveSlot; // 키보드 입력을 비활성화하므로 주석 처리
        // playerControls.Disable(); // 키보드 입력을 비활성화하므로 주석 처리
    }

    void Start()
    {
        if (showDebugLogs)
            Debug.Log("🎒 [ActiveInventory] 디버그 모드 활성화");
        
        // 🔑 안전한 초기화를 위해 코루틴으로 실행
        StartCoroutine(SafeInitialization());
    }
    
    /// <summary>
    /// 안전한 초기화 (ActiveWeapon이 준비될 때까지 대기)
    /// </summary>
    private IEnumerator SafeInitialization()
    {
        // ActiveWeapon이 준비될 때까지 대기
        while (FindObjectOfType<ActiveWeapon>() == null)
        {
            Debug.Log("⏰ [ActiveInventory] ActiveWeapon 대기 중...");
            yield return new WaitForSeconds(0.1f);
        }
        
        // PlayerDataManager 인벤토리 연동 초기화
        StartCoroutine(InitializeInventoryConnection());
        
        yield return new WaitForSeconds(0.2f); // UI 업데이트 대기
        
        // ⭐ 수정: 캐릭터 타입 기반 무기 자동 장착
        EquipWeaponBasedOnCharacterType();
        
        // 🔧 수정: 자동 비활성화 제거 (IntegratedInventoryController가 관리)
        Debug.Log("✅ [ActiveInventory] 모든 초기화 완료");
    }

    /// <summary>
    /// PlayerDataManager와 인벤토리 연동 초기화
    /// </summary>
    private IEnumerator InitializeInventoryConnection()
    {
        Debug.Log("🔗 [ActiveInventory] PlayerDataManager 연동 시작");
        
        // PlayerDataManager가 준비될 때까지 대기
        while (PlayerDataManager.Instance == null)
        {
            Debug.Log("⏰ [ActiveInventory] PlayerDataManager 대기 중...");
            yield return new WaitForSeconds(0.1f);
        }
        
        // 이벤트 구독
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnInventoryChanged += RefreshInventoryUI;
            PlayerDataManager.Instance.OnSlotClicked += OnSlotClickedForInGame;
            Debug.Log("✅ [ActiveInventory] PlayerDataManager 이벤트 구독 완료");
        }
        
        // 초기 UI 새로고침
        RefreshInventoryUI();
    }
    
    /// <summary>
    /// PlayerDataManager 인벤토리 데이터로 UI 새로고침
    /// </summary>
    private void RefreshInventoryUI()
    {
        if (!useDynamicInventory || PlayerDataManager.Instance == null)
            return;
        
        Debug.Log("🔄 [ActiveInventory] 인벤토리 UI 새로고침 시작");
        
        var inventoryItems = PlayerDataManager.Instance.InventoryItems;
        
        Debug.Log($"📊 [ActiveInventory] 새로고침 전 상태:");
        Debug.Log($"   - inventoryItems.Count: {inventoryItems.Count}");
        
        // 각 슬롯에 아이템 할당
        for (int i = 0; i < transform.childCount && i < maxDisplaySlots; i++)
        {
            Transform slotTransform = transform.GetChild(i);
            InventorySlot slot = slotTransform.GetComponent<InventorySlot>();
            
            if (slot != null)
            {
                EquipmentData oldEquipment = slot.GetEquipmentData();
                
                if (i < inventoryItems.Count)
                {
                    EquipmentData newEquipment = inventoryItems[i];
                    slot.SetEquipmentData(newEquipment);
                    
                    if (oldEquipment != newEquipment)
                    {
                        Debug.Log($"🎒 [ActiveInventory] 슬롯 {i}: {oldEquipment?.equipmentName ?? "null"} → {newEquipment?.equipmentName ?? "null"}");
                    }
                }
                else
                {
                    slot.SetEquipmentData(null);
                    if (oldEquipment != null)
                    {
                        Debug.Log($"🗑️ [ActiveInventory] 슬롯 {i}: {oldEquipment.equipmentName} → 비움");
                    }
                }
            }
        }
        
        Debug.Log($"✅ [ActiveInventory] UI 새로고침 완료 - 총 {inventoryItems.Count}개 아이템 표시");
    }
    
    /// <summary>
    /// 인게임 슬롯 클릭 이벤트 처리 (PlayerDataManager에서 호출)
    /// </summary>
    public void OnSlotClickedForInGame(EquipmentData equipmentData, int slotIndex)
    {
        Debug.Log($"🖱️ [ActiveInventory] ============= 슬롯 클릭 분석 시작 =============");
        
        // 🆕 NULL 체크 추가
        if (equipmentData == null)
        {
            Debug.LogWarning($"⚠️ [ActiveInventory] 슬롯 {slotIndex}의 equipmentData가 null입니다 (빈 슬롯 클릭)");
            return;
        }
        
        Debug.Log($"🖱️ [ActiveInventory] 인게임 슬롯 클릭 처리: {equipmentData.equipmentName} (인덱스: {slotIndex})");
        Debug.Log($"🖱️ [ActiveInventory] 전달받은 slotIndex: {slotIndex}");
        Debug.Log($"🖱️ [ActiveInventory] transform.childCount: {transform.childCount}");
        
        // 🆕 추가: 실제 클릭된 슬롯의 Transform 정보 확인
        Transform clickedTransform = transform.GetChild(slotIndex);
        Debug.Log($"🖱️ [ActiveInventory] 클릭된 Transform: {clickedTransform.name}");
        Debug.Log($"🖱️ [ActiveInventory] Transform의 siblingIndex: {clickedTransform.GetSiblingIndex()}");
        
        // 슬롯 인덱스 유효성 검사
        if (slotIndex < 0 || slotIndex >= transform.childCount)
        {
            Debug.LogError($"🔴 [ActiveInventory] 잘못된 슬롯 인덱스: {slotIndex}");
            return;
        }
        
        // 🔧 수정: 클릭한 슬롯의 아이템을 직접 가져와서 처리
        InventorySlot clickedSlot = transform.GetChild(slotIndex).GetComponent<InventorySlot>();
        if (clickedSlot == null || clickedSlot.GetEquipmentData() == null)
        {
            Debug.LogWarning($"⚠️ [ActiveInventory] 슬롯 {slotIndex}에 유효한 장비가 없습니다");
            return;
        }
        
        EquipmentData clickedEquipment = clickedSlot.GetEquipmentData();
        
        // 🆕 디버그: 비교 분석
        Debug.Log($"🔍 [ActiveInventory] 비교 분석:");
        Debug.Log($"   클릭한 슬롯[{slotIndex}]: {clickedEquipment.equipmentName}");
        Debug.Log($"   매니저 데이터[{slotIndex}]: {PlayerDataManager.Instance.InventoryItems[slotIndex].equipmentName}");
        
        // 🆕 오프셋 검증
        Debug.Log($"오프셋: {(clickedEquipment.equipmentName == PlayerDataManager.Instance.InventoryItems[slotIndex].equipmentName ? "일치" : "같은 로그는 안나와")}");
        
        Debug.Log($"🎯 [ActiveInventory] 클릭한 아이템: {clickedEquipment.equipmentName} (슬롯 {slotIndex})");
        
        // 🆕 호환성 검사 먼저 수행
        if (!IsCompatibleWithCurrentPlayer(clickedEquipment))
        {
            Debug.LogWarning($"⚠️ [ActiveInventory] {clickedEquipment.equipmentName}은(는) 현재 클래스와 호환되지 않습니다!");
            return;
        }
        
        // 🔧 수정: 다시 EquipItemFromSlot 직접 호출
        if (PlayerDataManager.Instance != null)
        {
            Debug.Log($"📞 [ActiveInventory] EquipItemFromSlot 호출: 아이템={clickedEquipment.equipmentName}, 인덱스={slotIndex}");
            
            // 새로운 메서드 호출: 슬롯 인덱스를 포함한 장착
            bool success = PlayerDataManager.Instance.EquipItemFromSlot(clickedEquipment, slotIndex);
            if (success)
            {
                Debug.Log($"✅ [ActiveInventory] 슬롯 {slotIndex} 아이템 장착 성공: {clickedEquipment.equipmentName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ [ActiveInventory] 슬롯 {slotIndex} 아이템 장착 실패: {clickedEquipment.equipmentName}");
            }
        }
        
        // 하이라이트 업데이트 (기존 코드 유지)
        activeSlotIndexNum = slotIndex;
        UpdateSlotHighlights();
        
        Debug.Log($"🖱️ [ActiveInventory] ============= 슬롯 클릭 분석 완료 =============");
    }
    
    /// <summary>
    /// 🔍 장비와 현재 플레이어 클래스 호환성 검사
    /// </summary>
    private bool IsCompatibleWithCurrentPlayer(EquipmentData equipment)
    {
        if (equipment == null)
        {
            Debug.LogWarning("🔍 [ActiveInventory] 장비 데이터가 null입니다.");
            return false;
        }
        
        // 현재 플레이어 클래스 확인
        PlayerType currentPlayerType = GetCurrentPlayerType();
        
        // 장비 호환성 검사
        bool isCompatible = IsEquipmentCompatible(equipment, currentPlayerType);
        
        Debug.Log($"🔍 [ActiveInventory] 호환성 검사:");
        Debug.Log($"   - 장비: {equipment.equipmentName} (클래스 제한: {equipment.usableClass})");
        Debug.Log($"   - 현재 플레이어: {currentPlayerType}");
        Debug.Log($"   - 호환 여부: {(isCompatible ? "✅ 호환" : "❌ 비호환")}");
        
        return isCompatible;
    }
    
    /// <summary>
    /// 🎯 현재 플레이어 타입 가져오기
    /// </summary>
    private PlayerType GetCurrentPlayerType()
    {
        // ⭐ 새로운 구조: SelectedPlayerData에서 가져오기
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
        {
            return PlayerDataManager.Instance.CurrentPlayerType;
        }
        
        // Fallback: GameManager에서 가져오기
        if (GameManager.Instance?.selectedPlayerData != null)
        {
            return GameManager.Instance.selectedPlayerData.selectedPlayerType;
        }
        
        return PlayerType.Warrior; // 기본값
    }
    
    /// <summary>
    /// 🔧 장비 호환성 검사 로직
    /// </summary>
    private bool IsEquipmentCompatible(EquipmentData equipment, PlayerType playerType)
    {
        // None이나 Any는 모든 클래스가 사용 가능
        if (equipment.usableClass == PlayerClass.None || equipment.usableClass == PlayerClass.Any)
            return true;
        
        // 플레이어 타입과 장비 제한 클래스 매칭
        return equipment.usableClass switch
        {
            PlayerClass.Warrior => playerType == PlayerType.Warrior,
            PlayerClass.Assasin => playerType == PlayerType.Assasin,
            PlayerClass.Wizard => playerType == PlayerType.Wizard,
            _ => false
        };
    }

    /// <summary>
    /// ⭐ 새로운 메서드: 캐릭터 타입에 맞는 무기 자동 장착
    /// </summary>
    private void EquipWeaponBasedOnCharacterType()
    {
        // 1. 현재 선택된 캐릭터 타입 확인
        if (GameManager.Instance?.selectedPlayerData == null)
        {
            Debug.LogWarning("🟡 [ActiveInventory] 캐릭터 선택 데이터 없음 - 기존 방식 유지");
            return;
        }
        
        PlayerType selectedType = GameManager.Instance.selectedPlayerData.selectedPlayerType;
        string expectedWeaponType = GetExpectedWeaponType(selectedType);
        
        Debug.Log($"🎯 [ActiveInventory] 캐릭터: {selectedType}, 기대 무기: {expectedWeaponType}");
        
        // 2. 인벤토리에서 해당 캐릭터에 맞는 무기 찾기
        int compatibleSlotIndex = FindCompatibleWeaponSlot(expectedWeaponType);
        
        if (compatibleSlotIndex >= 0)
        {
            Debug.Log($"✅ [ActiveInventory] {selectedType}에 맞는 {expectedWeaponType} 무기를 슬롯 {compatibleSlotIndex}에서 발견");
            ToggleActiveHighlight(compatibleSlotIndex);
        }
        else
        {
            Debug.Log($"🚀 [ActiveInventory] 인벤토리에 {expectedWeaponType} 무기 없음 - PlayerSpawner 할당 무기 유지");
            // PlayerSpawner가 이미 올바른 무기를 할당했으므로 그대로 유지
            HighlightCurrentWeaponSlot();
        }
    }

    /// <summary>
    /// 캐릭터 타입에 따른 기대 무기 타입 반환
    /// </summary>
    private string GetExpectedWeaponType(PlayerType playerType)
    {
        return playerType switch
        {
            PlayerType.Warrior => "Sword",
            PlayerType.Assasin => "Bow", 
            PlayerType.Wizard => "Staff",
            _ => ""
        };
    }

    /// <summary>
    /// 인벤토리에서 호환 가능한 무기 슬롯 찾기
    /// </summary>
    private int FindCompatibleWeaponSlot(string weaponType)
    {
        if (string.IsNullOrEmpty(weaponType)) return -1;
        
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform slotTransform = transform.GetChild(i);
            InventorySlot slot = slotTransform.GetComponent<InventorySlot>();
            
            if (slot != null && slot.HasWeapon())
            {
                string weaponName = slot.GetWeaponName();
                
                // 무기 이름에 기대하는 무기 타입이 포함되어 있는지 확인
                if (weaponName.Contains(weaponType))
                {
                    Debug.Log($"🔍 [ActiveInventory] 호환 무기 발견: 슬롯 {i} - {weaponName}");
                    return i;
                }
            }
        }
        
        return -1; // 호환 무기 없음
    }

    /// <summary>
    /// 현재 ActiveWeapon에 맞는 슬롯 하이라이트 (교체하지 않음)
    /// </summary>
    private void HighlightCurrentWeaponSlot()
    {
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon?.CurrentActiveWeapon == null) return;
        
        string currentWeaponName = activeWeapon.CurrentActiveWeapon.name.Replace("(Clone)", "");
        
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform slotTransform = transform.GetChild(i);
            InventorySlot slot = slotTransform.GetComponent<InventorySlot>();
            
            if (slot != null && slot.GetWeaponName().Contains(currentWeaponName))
            {
                // 하이라이트만 설정 (실제 무기 교체는 하지 않음)
                activeSlotIndexNum = i;
                UpdateSlotHighlights();
                Debug.Log($"💡 [ActiveInventory] 현재 무기에 맞는 슬롯 {i} 하이라이트");
                return;
            }
        }
    }

    /// <summary>
    /// 슬롯 하이라이트만 업데이트 (무기 교체 없음)
    /// </summary>
    private void UpdateSlotHighlights()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform inventorySlot = transform.GetChild(i);
            if (inventorySlot.childCount > 0)
            {
                inventorySlot.GetChild(0).gameObject.SetActive(i == activeSlotIndexNum);
            }
        }
    }

    public void EquipStartingweapon() {
        Debug.Log("🚀 [ActiveInventory] 시작 무기 장착 중...");
        ToggleActiveHighlight(0);
    }

    private void ToggleActiveSlot(UnityEngine.InputSystem.InputAction.CallbackContext ctx) {
        int numValue = (int)ctx.ReadValue<float>();
        ToggleActiveHighlight(numValue - 1);
    }

    private void ToggleActiveHighlight(int indexNum) {
        activeSlotIndexNum = indexNum;
        
        Debug.Log($"🔄 [ActiveInventory] 슬롯 {indexNum}번으로 변경 중...");

        foreach (Transform inventorySlot in this.transform)
        {
            inventorySlot.GetChild(0).gameObject.SetActive(false);
        }

        // 🔑 안전한 자식 접근
        Transform targetSlot = this.transform.GetChild(indexNum);
        if (targetSlot != null && targetSlot.childCount > 0) {
            targetSlot.GetChild(0).gameObject.SetActive(true);
        } else {
            Debug.LogWarning($"⚠️ [ActiveInventory] 슬롯 {indexNum}에 하이라이트 자식이 없습니다");
        }

        // ChangeActiveWeapon(); // 🗑️ 제거: 새로운 이벤트 시스템에서 불필요
    }

    // 🗑️ 제거: ChangeActiveWeapon - 새로운 이벤트 시스템에서 불필요
    /*
    private void ChangeActiveWeapon() {
        Debug.Log("⚔️ [ActiveInventory] 장비 교체 시작...");
        // ... 기존 코드 제거됨 ...
    }
    */

    // 🗑️ 제거: Handle* 메서드들 - OnSlotClicked에서 직접 EquipItemFromSlot 호출로 대체됨
    /*
    /// <summary>
    /// 무기 장착 처리 (기존 로직) - 사용하지 않음
    /// </summary>
    private void HandleWeaponEquip(EquipmentData weaponData)
    {
        Debug.Log($"⚔️ [ActiveInventory] 무기 장착: {weaponData.equipmentName}");
        
        // 🆕 호환성 검증 추가
        if (!IsCompatibleWithCurrentPlayer(weaponData))
        {
            Debug.LogWarning($"⚠️ [ActiveInventory] {weaponData.equipmentName}은(는) 현재 클래스와 호환되지 않습니다!");
            return;
        }
        
        // 🔧 수정: PlayerDataManager 시스템으로 통합 (기존 2줄 → 신규 8줄)
        if (PlayerDataManager.Instance != null)
        {
            bool success = PlayerDataManager.Instance.EquipItem(weaponData, EquipmentSlot.MainWeapon);
            if (success)
            {
                Debug.Log($"✅ [ActiveInventory] 무기 장착 성공: {weaponData.equipmentName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ [ActiveInventory] 무기 장착 실패: {weaponData.equipmentName}");
            }
        }
        else
        {
            Debug.LogError("🔴 [ActiveInventory] PlayerDataManager를 찾을 수 없습니다!");
        }
        
        // 🗑️ 기존 코드 제거:
        // var activeWeapon = FindObjectOfType<ActiveWeapon>();
        // if (activeWeapon == null) {
        //     Debug.LogWarning("⚠️ [ActiveInventory] ActiveWeapon을 찾을 수 없습니다!");
        //     return;
        // }
        // activeWeapon.EquipWeapon(weaponData);
        // Debug.Log($"🎯 [ActiveInventory] 무기 교체 완료: {weaponData.equipmentName}");
    }

    /// <summary>
    /// 갑옷/신발 장착 처리 (수정)
    /// </summary>
    private void HandleArmorEquip(EquipmentData armorData)
    {
        Debug.Log($"🛡️ [ActiveInventory] 방어구 장착: {armorData.equipmentName}");
        
        // 🆕 호환성 검증 추가
        if (!IsCompatibleWithCurrentPlayer(armorData))
        {
            Debug.LogWarning($"⚠️ [ActiveInventory] {armorData.equipmentName}은(는) 현재 클래스와 호환되지 않습니다!");
            return;
        }
        
        // 🔧 수정: PlayerDataManager가 자동으로 적절한 슬롯 결정하도록 변경
        if (PlayerDataManager.Instance != null)
        {
            // EquipItem(item) 오버로드 사용 → DetermineEquipmentSlot 자동 호출
            bool success = PlayerDataManager.Instance.EquipItem(armorData); // 슬롯 제거!
            if (success)
            {
                Debug.Log($"✅ [ActiveInventory] 방어구 장착 성공: {armorData.equipmentName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ [ActiveInventory] 방어구 장착 실패: {armorData.equipmentName}");
            }
        }
        else
        {
            Debug.LogError("🔴 [ActiveInventory] PlayerDataManager를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 악세서리 장착 처리 (향후 확장용)
    /// </summary>
    private void HandleAccessoryEquip(EquipmentData accessoryData)
    {
        Debug.Log($"💍 [ActiveInventory] 악세서리 장착: {accessoryData.equipmentName}");
        
        // 🆕 호환성 검증 추가
        if (!IsCompatibleWithCurrentPlayer(accessoryData))
        {
            Debug.LogWarning($"⚠️ [ActiveInventory] {accessoryData.equipmentName}은(는) 현재 클래스와 호환되지 않습니다!");
            return;
        }
        
        // PlayerDataManager를 통한 장비 시스템 사용
        if (PlayerDataManager.Instance != null)
        {
            bool success = PlayerDataManager.Instance.EquipItem(accessoryData, EquipmentSlot.Ring1); // 또는 적절한 슬롯
            if (success)
            {
                Debug.Log($"✅ [ActiveInventory] 악세서리 장착 성공: {accessoryData.equipmentName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ [ActiveInventory] 악세서리 장착 실패: {accessoryData.equipmentName}");
            }
        }
        else
        {
            Debug.LogError("🔴 [ActiveInventory] PlayerDataManager를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 🆕 현재 활성화된 플레이어 클래스 확인
    /// </summary>
    private PlayerClass GetCurrentPlayerClass()
    {
        // 1순위: GameManager의 selectedPlayerData 확인
        if (GameManager.Instance?.selectedPlayerData == null)
        {
            Debug.LogWarning("🟡 [ActiveInventory] 캐릭터 선택 데이터 없음 - 기존 방식 유지");
            return PlayerClass.Warrior; // 기본값
        }
        
        PlayerType selectedType = GameManager.Instance.selectedPlayerData.selectedPlayerType;
        return ConvertPlayerTypeToPlayerClass(selectedType);
    }

    /// <summary>
    /// 🆕 PlayerType을 PlayerClass로 변환
    /// </summary>
    private PlayerClass ConvertPlayerTypeToPlayerClass(PlayerType playerType)
    {
        return playerType switch
        {
            PlayerType.Warrior => PlayerClass.Warrior,
            PlayerType.Assasin => PlayerClass.Assasin,
            PlayerType.Wizard => PlayerClass.Wizard,
            _ => PlayerClass.Warrior
        };
    }

    void Update() {
        // T키로 강제 무기 교체 테스트
        if (Input.GetKeyDown(KeyCode.T)) {
            Debug.Log("🔧 [DEBUG] T키로 강제 무기 교체 테스트");
            ToggleActiveHighlight(1); // 1번 슬롯으로 강제 변경
        }
        
        // Q키로 다음 무기 교체
        // if (Input.GetKeyDown(KeyCode.Q)) {
        //     TestSwitchToNextSlot();
        // }
    }

        // 🗑️ 제거: OnSlotClicked - OnSlotClickedForInGame으로 통합됨



    /// <summary>
    /// 무기 장착 처리 (기존 로직)
    /// </summary>
    private void HandleWeaponEquip(EquipmentData weaponData, int slotIndex)
    {
        Debug.Log($"⚔️ [ActiveInventory] 무기 장착: {weaponData.equipmentName}");
        
        // 🆕 호환성 검증 추가
        if (!IsCompatibleWithCurrentPlayer(weaponData))
        {
            Debug.LogWarning($"⚠️ [ActiveInventory] {weaponData.equipmentName}은(는) 현재 클래스와 호환되지 않습니다!");
            return;
        }
        
        // 🔧 수정: 슬롯 인덱스를 포함한 정확한 1:1 교체
        if (PlayerDataManager.Instance != null)
        {
            bool success = PlayerDataManager.Instance.EquipItemFromSlot(weaponData, slotIndex);
            if (success)
            {
                Debug.Log($"✅ [ActiveInventory] 무기 장착 성공: {weaponData.equipmentName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ [ActiveInventory] 무기 장착 실패: {weaponData.equipmentName}");
            }
        }
        else
        {
            Debug.LogError("🔴 [ActiveInventory] PlayerDataManager를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 갑옷/신발 장착 처리 (수정)
    /// </summary>
    private void HandleArmorEquip(EquipmentData armorData, int slotIndex)
    {
        Debug.Log($"🛡️ [ActiveInventory] 방어구 장착: {armorData.equipmentName}");
        
        // 🆕 호환성 검증 추가
        if (!IsCompatibleWithCurrentPlayer(armorData))
        {
            Debug.LogWarning($"⚠️ [ActiveInventory] {armorData.equipmentName}은(는) 현재 클래스와 호환되지 않습니다!");
            return;
        }
        
        // 🔧 수정: 슬롯 인덱스를 포함한 정확한 1:1 교체
        if (PlayerDataManager.Instance != null)
        {
            bool success = PlayerDataManager.Instance.EquipItemFromSlot(armorData, slotIndex);
            if (success)
            {
                Debug.Log($"✅ [ActiveInventory] 방어구 장착 성공: {armorData.equipmentName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ [ActiveInventory] 방어구 장착 실패: {armorData.equipmentName}");
            }
        }
        else
        {
            Debug.LogError("🔴 [ActiveInventory] PlayerDataManager를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 악세서리 장착 처리 (향후 확장용)
    /// </summary>
    private void HandleAccessoryEquip(EquipmentData accessoryData, int slotIndex)
    {
        Debug.Log($"💍 [ActiveInventory] 악세서리 장착: {accessoryData.equipmentName}");
        
        // 🆕 호환성 검증 추가
        if (!IsCompatibleWithCurrentPlayer(accessoryData))
        {
            Debug.LogWarning($"⚠️ [ActiveInventory] {accessoryData.equipmentName}은(는) 현재 클래스와 호환되지 않습니다!");
            return;
        }
        
        // 🔧 수정: 슬롯 인덱스를 포함한 정확한 1:1 교체
        if (PlayerDataManager.Instance != null)
        {
            bool success = PlayerDataManager.Instance.EquipItemFromSlot(accessoryData, slotIndex);
            if (success)
            {
                Debug.Log($"✅ [ActiveInventory] 악세서리 장착 성공: {accessoryData.equipmentName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ [ActiveInventory] 악세서리 장착 실패: {accessoryData.equipmentName}");
            }
        }
        else
        {
            Debug.LogError("🔴 [ActiveInventory] PlayerDataManager를 찾을 수 없습니다!");
        }
    }
    */
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnInventoryChanged -= RefreshInventoryUI;
            // 🆕 공용 이벤트 구독 해제
            PlayerDataManager.Instance.OnSlotClicked -= OnSlotClickedForInGame;
        }
    }
} 