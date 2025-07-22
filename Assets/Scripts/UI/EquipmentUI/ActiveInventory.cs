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
        
        // 🔑 동적 인벤토리에 아이템이 있으면 시작 무기 장착, 없으면 WeaponNull
        yield return new WaitForSeconds(0.2f); // UI 업데이트 대기
        
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.InventoryItems.Count > 0)
        {
            Debug.Log("🚀 [ActiveInventory] 인벤토리에 아이템 있음 - 첫 번째 아이템으로 시작 무기 장착");
            EquipStartingweapon();
        }
        else
        {
            Debug.Log("🚀 [ActiveInventory] 인벤토리가 비어있음 - WeaponNull 설정");
            var activeWeapon = FindObjectOfType<ActiveWeapon>();
            if (activeWeapon != null)
            {
                activeWeapon.WeaponNull();
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

        if (activeWeapon.CurrentActiveWeapon != null) {
            Debug.Log($"🗑️ [ActiveInventory] 기존 무기 제거: {activeWeapon.CurrentActiveWeapon.name}");
            Destroy(activeWeapon.CurrentActiveWeapon.gameObject);
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
        
        WeaponInfo weaponInfo = inventorySlot.GetWeaponInfo();
        EquipmentData equipmentData = inventorySlot.GetEquipmentData(); // 🆕 EquipmentData도 가져오기

        // 🔑 무기 데이터가 없으면 WeaponNull 처리
        if (weaponInfo == null || equipmentData == null) {
            Debug.Log("⚠️ [ActiveInventory] 무기 데이터 없음 - WeaponNull 호출");
            activeWeapon.WeaponNull();
            return;
        }

        Debug.Log($"✅ [ActiveInventory] 무기 발견: {weaponInfo.name} (공격력: {weaponInfo.weaponDamage})");
        
        // 🔑 EquipmentData.equipmentPrefab 사용 (weaponInfo.weaponPrefab 대신)
        GameObject weaponPrefab = equipmentData.equipmentPrefab;
        if (weaponPrefab == null) {
            Debug.LogError($"🔴 [ActiveInventory] {equipmentData.equipmentName}에 equipmentPrefab이 설정되지 않았습니다!");
            return;
        }

        GameObject newWeapon = null;
        
        // 🔑 직접 생성 (풀링 문제 해결)
        try {
            newWeapon = Instantiate(weaponPrefab, activeWeapon.transform.position, activeWeapon.transform.rotation);
            Debug.Log($"✅ [ActiveInventory] 직접 생성 성공: {newWeapon.name}");
        } catch (System.Exception e) {
            Debug.LogError($"🔴 [ActiveInventory] 무기 생성 실패: {e.Message}");
            return;
        }

        Debug.Log($"✅ [ActiveInventory] 무기 생성 성공: {newWeapon.name}");

        // 🔑 부모 설정
        newWeapon.transform.SetParent(activeWeapon.transform);

        // 🔑 Sword 컴포넌트 확인
        var weaponComponent = newWeapon.GetComponent<MonoBehaviour>();
        if (weaponComponent == null) {
            Debug.LogError($"🔴 [ActiveInventory] {newWeapon.name}에 무기 컴포넌트가 없습니다!");
            return;
        }

        // 🔑 생성된 무기에 WeaponInfo 동적 할당 (중요!)
        var swordComponent = weaponComponent as Sword;
        if (swordComponent != null) {
            // 🔑 리플렉션으로 private weaponInfo 필드에 할당
            var weaponInfoField = typeof(Sword).GetField("weaponInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (weaponInfoField != null) {
                weaponInfoField.SetValue(swordComponent, weaponInfo); // 🆕 동적 변환된 WeaponInfo 할당
                Debug.Log($"✅ [ActiveInventory] Sword 컴포넌트에 WeaponInfo 동적 할당: {weaponInfo.name}");
            } else {
                Debug.LogError("🔴 [ActiveInventory] Sword 클래스에서 weaponInfo 필드를 찾을 수 없습니다!");
            }
        } else {
            Debug.LogWarning($"⚠️ [ActiveInventory] {newWeapon.name}에 Sword 컴포넌트가 없습니다!");
        }

        // 🔑 ActiveWeapon에 무기 등록
        activeWeapon.NewWeapon(weaponComponent);

        Debug.Log($"🎯 [ActiveInventory] 무기 교체 완료! 활성 무기: {newWeapon.name}");
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