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

        ChangeActiveWeapon();
    }

    private void ChangeActiveWeapon() {
        Debug.Log("⚔️ [ActiveInventory] 장비 교체 시작...");

        // 현재 선택된 슬롯의 EquipmentData 가져오기
        InventorySlot selectedSlot = this.transform.GetChild(activeSlotIndexNum).GetComponent<InventorySlot>();
        if (selectedSlot == null || selectedSlot.GetEquipmentData() == null)
        {
            Debug.LogWarning("⚠️ [ActiveInventory] 선택된 슬롯에 유효한 장비가 없습니다");
            return;
        }

        EquipmentData equipmentData = selectedSlot.GetEquipmentData();
        
        // 🆕 장비 타입별 분기 처리
        switch (equipmentData.equipmentType)
        {
            case EquipmentType.Weapon:
                HandleWeaponEquip(equipmentData);
                break;
                
            case EquipmentType.Armor:
                HandleArmorEquip(equipmentData);
                break;
                
            case EquipmentType.Accessory:
                HandleAccessoryEquip(equipmentData);
                break;
                
            default:
                Debug.LogWarning($"⚠️ [ActiveInventory] 지원하지 않는 장비 타입: {equipmentData.equipmentType}");
                break;
        }
    }

    /// <summary>
    /// 무기 장착 처리 (기존 로직)
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

    /// <summary>
    /// 슬롯 클릭 이벤트 (InventorySlot에서 호출)
    /// </summary>
    public void OnSlotClicked(int slotIndex)
    {
        Debug.Log($"🖱️ [ActiveInventory] 슬롯 {slotIndex} 클릭됨");
        
        // 슬롯 인덱스 유효성 검사
        if (slotIndex < 0 || slotIndex >= transform.childCount)
        {
            Debug.LogError($"🔴 [ActiveInventory] 잘못된 슬롯 인덱스: {slotIndex}");
            return;
        }
        
        // 클릭된 슬롯으로 전환
        ToggleActiveHighlight(slotIndex);
    }

    /// <summary>
    /// 다음 슬롯으로 무기 교체 (테스트용)
    /// </summary>
    public void TestSwitchToNextSlot()
    {
        int totalSlots = transform.childCount;
        int nextSlot = (activeSlotIndexNum + 1) % totalSlots;
        
        Debug.Log($"🔄 [ActiveInventory] 무기 교체: 슬롯 {activeSlotIndexNum} → {nextSlot}");
        ToggleActiveHighlight(nextSlot);
    }

    /// <summary>
    /// 특정 슬롯으로 무기 교체
    /// </summary>
    public void SwitchToSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= transform.childCount)
        {
            Debug.LogError($"🔴 [ActiveInventory] 잘못된 슬롯 번호: {slotIndex}");
            return;
        }
        
        Debug.Log($"🎯 [ActiveInventory] 슬롯 {slotIndex}로 교체");
        ToggleActiveHighlight(slotIndex);
    }

    /// <summary>
    /// PlayerDataManager 연결 초기화 (안전한 버전)
    /// </summary>
    private IEnumerator InitializeInventoryConnection()
    {
        // PlayerDataManager가 준비될 때까지 대기
        while (PlayerDataManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        if (useDynamicInventory)
        {
            PlayerDataManager.Instance.OnInventoryChanged += RefreshInventoryUI;
            RefreshInventoryUI(); // 초기 UI 업데이트
            Debug.Log("✅ [ActiveInventory] PlayerDataManager 이벤트 구독 완료");
        }
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
        
        // 🛡️ 안전성 검사 추가
        for (int i = 0; i < transform.childCount && i < maxDisplaySlots; i++)
        {
            Transform slotTransform = transform.GetChild(i);
            InventorySlot slot = slotTransform.GetComponent<InventorySlot>();
            
            if (slot != null)
            {
                // 🔍 인덱스 범위 확인
                if (i < inventoryItems.Count && inventoryItems[i] != null)
                {
                    slot.SetEquipmentData(inventoryItems[i]);
                    Debug.Log($"🎒 [ActiveInventory] 슬롯 {i}: {inventoryItems[i].equipmentName} 설정");
                }
                else
                {
                    slot.SetEquipmentData(null);
                    Debug.Log($"��️ [ActiveInventory] 슬롯 {i}: 비움");
                }
            }
        }
        
        Debug.Log($"✅ [ActiveInventory] UI 새로고침 완료 - 총 {inventoryItems.Count}개 아이템 표시");
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
        
        // 🔧 showDebugLogs 조건 제거하고 항상 로그 출력
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
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnInventoryChanged -= RefreshInventoryUI;
        }
    }
} 