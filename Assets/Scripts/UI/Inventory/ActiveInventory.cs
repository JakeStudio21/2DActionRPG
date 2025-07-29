using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveInventory : MonoBehaviour
{
    private int activeSlotIndexNum = 0;

    [Header("🎒 인벤토리 연동")]
    [SerializeField] private bool useDynamicInventory = true; // 동적 인벤토리 사용 여부
    [SerializeField] private int maxDisplaySlots = 6; // 표시할 최대 슬롯 수

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
        Debug.Log("⚔️ [ActiveInventory] 무기 교체 시작...");

        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        
        // 🔑 ActiveWeapon null 체크 추가
        if (activeWeapon == null) {
            Debug.LogWarning("⚠️ [ActiveInventory] ActiveWeapon을 찾을 수 없습니다! 무기 교체 건너뜀");
            return;
        }

        // 🔑 슬롯 범위 체크 추가
        if (activeSlotIndexNum < 0 || activeSlotIndexNum >= transform.childCount) {
            Debug.LogWarning($"⚠️ [ActiveInventory] 잘못된 슬롯 인덱스: {activeSlotIndexNum}");
            return;
        }
        
        Transform childTransform = transform.GetChild(activeSlotIndexNum);
        InventorySlot inventorySlot = childTransform.GetComponentInChildren<InventorySlot>();
        
        if (inventorySlot == null) {
            Debug.LogError("❌ [ActiveInventory] InventorySlot 컴포넌트를 찾을 수 없습니다!");
            return;
        }
        
        // 🆕 장비 정보 디버그 로그
        Debug.Log($"📋 [ActiveInventory] 슬롯 정보 - 무기명: {inventorySlot.GetWeaponName()}, 능력치: {inventorySlot.GetWeaponStats()}");
        
        // WeaponInfo 관련 코드 제거, EquipmentData만 사용
        EquipmentData equipmentData = inventorySlot.GetEquipmentData();

        // 🔑 무기 데이터가 없으면 WeaponNull 처리
        if (equipmentData == null) {  // weaponInfo 체크 제거
            Debug.Log("⚠️ [ActiveInventory] 무기 데이터 없음 - WeaponNull 호출");
            activeWeapon.WeaponNull();
            return;
        }

        // 🆕 클래스 호환성 검사 (기존 무기 파괴 전에 실행!)
        PlayerClass currentPlayerClass = GetCurrentPlayerClass();
        if (!equipmentData.IsCompatibleWith(currentPlayerClass))
        {
            Debug.LogWarning($"🚫 [ActiveInventory] 클래스 호환성 오류: {currentPlayerClass}는 {equipmentData.equipmentName} 사용 불가");
            
            // 🎨 UI 메시지 표시
            inventorySlot.ShowIncompatibilityMessage();
            
            // 🔑 중요: 기존 무기를 파괴하지 않고 그대로 유지!
            return;
        }

        // 🔑 호환성 검사 통과 후에만 기존 무기 제거
        if (activeWeapon.CurrentActiveWeapon != null) {
            Debug.Log($"🗑️ [ActiveInventory] 기존 무기 제거: {activeWeapon.CurrentActiveWeapon.name}");
            Destroy(activeWeapon.CurrentActiveWeapon.gameObject);
        }

        Debug.Log($"✅ [ActiveInventory] 무기 발견: {equipmentData.equipmentName} (공격력: {equipmentData.attackDamage})");
        
        // ✅ 새로운 방식: ActiveWeapon.EquipWeapon() 한 번 호출로 모든 것 해결
        activeWeapon.EquipWeapon(equipmentData);
        
        Debug.Log($"🎯 [ActiveInventory] 무기 교체 완료! 활성 무기: {equipmentData.equipmentName}");
    }

    /// <summary>
    /// 🆕 현재 활성화된 플레이어 클래스 확인
    /// </summary>
    private PlayerClass GetCurrentPlayerClass()
    {
        // 1순위: GameManager의 selectedPlayerData 확인
        if (GameManager.Instance?.selectedPlayerData != null)
        {
            PlayerType selectedType = GameManager.Instance.selectedPlayerData.selectedPlayerType;
            return ConvertPlayerTypeToPlayerClass(selectedType);
        }
        
        // 2순위: 활성화된 클래스 컴포넌트 직접 확인
        var warrior = FindObjectOfType<Warrior>();
        if (warrior != null && warrior.IsActiveClass)
        {
            return PlayerClass.Warrior;
        }
        
        var assasin = FindObjectOfType<Assasin>();
        if (assasin != null && assasin.IsActiveClass)
        {
            return PlayerClass.Assasin;
        }
        
        // 기본값
        Debug.LogWarning("🟡 [ActiveInventory] 활성 클래스를 찾을 수 없어 Warrior로 가정합니다.");
        return PlayerClass.Warrior;
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
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnInventoryChanged -= RefreshInventoryUI;
        }
    }
} 