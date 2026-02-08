using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // 🆕 씬 관리를 위한 추가

/// <summary>
/// 🏠 로비 인벤토리 UI (View Only)
/// 표시만 담당, 로직은 InventoryController에서 처리
/// 🏠 LobbyInventoryUI - 로비 전용 인벤토리 UI
/// 책임:
/// - 인벤토리 아이템 표시
/// - 아이템 상세 정보 표시 (DetailPanel)
/// - 아이템 착용/해제 기능
/// - 로비 전용 UI 상호작용
/// 
/// 의존성:
/// - PlayerDataManager (데이터 소스)
/// - LobbyInventoryController (제어)
/// </summary>

/// <summary>
/// 🏪 ShopInventoryUI - 상점 전용 인벤토리 UI  
/// 책임:
/// - 판매용 아이템 선택 표시
/// - 상점 거래를 위한 아이템 클릭 처리
/// 
/// 제외 기능:
/// - 아이템 착용 (로비 전용)
/// - 상세 정보 표시 (상점은 DetailPanel 별도)
/// 
/// 의존성:
/// - PlayerDataManager (데이터 소스)
/// - ShopUIController (거래 제어)
/// </summary>

/// <summary>
/// 🎮 IntegratedInventoryController - 인게임 전용 컨트롤러
/// 책임:
/// - 인게임 인벤토리 토글 (I키, 가방 버튼)
/// - 무기 교체 중심 상호작용
/// - ActiveInventory와 연동
/// 
/// 의존성:
/// - PlayerDataManager (데이터 소스)
/// - ActiveInventory (인게임 UI)
/// - ActiveWeapon (무기 교체)
/// </summary>
public class LobbyInventoryUI : MonoBehaviour
{
    /// <summary>
    /// 인벤토리 탭 타입 (확장 가능)
    /// </summary>
    public enum InventoryTabType
    {
        Equipment,  // 장비 (기본)
        Material,   // 재료
        Bound,      // 귀속 (Phase C)
        Quest       // 퀘스트 (Phase D)
    }
    
    [Header("🎒 로비 인벤토리 설정")]
    [SerializeField] private GameObject inventoryPanel;     // 인벤토리 패널
    [SerializeField] private Button inventoryToggleButton;  // 가방 버튼
    [SerializeField] private ScrollRect scrollRect;         // ⭐ ScrollView의 ScrollRect 컴포넌트
    [SerializeField] private Transform slotContainer;       // 슬롯들이 들어갈 컨테이너 (ScrollView의 Content)
    [SerializeField] private GameObject slotPrefab;         // 로비용 슬롯 프리팹
    [SerializeField] private Button closePanelButton;       // 패널 닫기 버튼 (로비로 돌아가기)
    
    [Header("📑 탭 시스템")]
    [SerializeField] private Button equipmentTabButton;     // 장비 탭 버튼
    [SerializeField] private Button materialTabButton;      // 재료 탭 버튼
    [SerializeField] private TMP_Text equipmentTabText;     // 장비 탭 텍스트
    [SerializeField] private TMP_Text materialTabText;      // 재료 탭 텍스트
    
    private InventoryTabType currentTab = InventoryTabType.Equipment; // 현재 활성 탭
    
    // ❌ 제거: maxDisplaySlots (AccountData에서 가져옴)
    
    [Header("💰 골드 표시")]
    [SerializeField] private TMP_Text goldText;             // 플레이어 골드 표시
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = false; // ⭐ 프로덕션 기본값
    
    [Header("🎒 착용 장비 UI 연동")]
    [SerializeField] private LobbyEquippedItemsUI equippedItemsUI; // 🆕 장비창 UI 참조
    
    // 내부 상태
    private List<InventorySlot> lobbySlots = new List<InventorySlot>();

    void Awake()
    {
        Debug.Log($"🚨🚨🚨 [LobbyInventoryUI] 이 로그가 나오면 스크립트가 실행되고 있다는 뜻! GameObject 이름: {gameObject.name}");
        Debug.Log($"🆔 [LobbyInventoryUI] 인스턴스 ID: {GetInstanceID()}");
        Debug.Log($" [LobbyInventoryUI] Awake() 호출됨");
        SetupEventListeners();
        SetupControllerEvents();
        Debug.Log($"✅ [LobbyInventoryUI] Awake() 완료");
    }

    void OnEnable()
    {
        Debug.Log($"🔄 [LobbyInventoryUI] OnEnable() 호출됨 - 인벤토리 새로고침 예약");
        
        // ⭐ 탭 버튼 상태 업데이트 (장비탭 활성화)
        UpdateTabButtonStates();
        
        // 패널이 활성화될 때마다 인벤토리 새로고침
        // (다음 프레임에 실행하여 초기화 완료 보장)
        if (AccountDataManager.IsInitialized())
        {
            Invoke(nameof(RefreshInventoryUI), 0.1f);
        }
        
        // 골드 표시 초기화
        InitializeGoldDisplay();
    }
    
    void OnDisable()
    {
        // ⭐ 탭 리셋: 다음에 열 때 항상 장비탭부터 시작
        currentTab = InventoryTabType.Equipment;
        
        // 골드 변경 이벤트 구독 해제 (PlayerDataManager)
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
        }
        
        // ⭐ V2: AccountDataManager 이벤트 구독 해제
        if (AccountDataManager.Instance != null)
        {
            AccountDataManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
            AccountDataManager.Instance.OnMaterialChanged -= OnMaterialChangedHandler;
        }
    }

    void Start()
    {
        Debug.Log($"🏠 [LobbyInventoryUI] Start() 호출됨");
        
        // 슬롯 생성
        SetupSlots();
        
        // ⭐ 골드 초기화 (OnEnable이 호출되지 않을 경우 대비)
        InitializeGoldDisplay();
        
        if (showDebugLogs)
            Debug.Log($"✅ [LobbyInventoryUI] Start() 완료");
    }
    
    // ========================================
    // 💰 골드 관련 헬퍼 메서드
    // ========================================
    
    /// <summary>
    /// 재귀적으로 자식 GameObject 찾기
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            
            Transform result = FindChildRecursive(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
    
    /// <summary>
    /// GameObject의 전체 경로 가져오기
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }
    
    /// <summary>
    /// 골드 표시 초기화 (Start 및 OnEnable에서 호출)
    /// </summary>
    private void InitializeGoldDisplay()
    {
        Debug.Log($"🔍 [LobbyInventoryUI] InitializeGoldDisplay() 시작");
        Debug.Log($"   - PlayerDataManager: {(PlayerDataManager.Instance != null ? "있음" : "NULL")}");
        Debug.Log($"   - AccountDataManager: {(AccountDataManager.Instance != null ? "있음" : "NULL")}");
        Debug.Log($"   - goldText: {(goldText != null ? "할당됨" : "NULL")}");
        
        // ⭐ goldText가 null인 경우 자동으로 찾기 시도
        if (goldText == null)
        {
            Debug.LogWarning($"⚠️ [LobbyInventoryUI] goldText가 NULL! 자식 GameObject에서 'GoldText' 찾는 중...");
            
            // 자식 GameObject에서 "GoldText" 이름으로 찾기
            Transform goldTextTransform = transform.Find("GoldPanel/GoldText");
            if (goldTextTransform == null)
            {
                // 재귀적으로 모든 자식에서 찾기
                goldTextTransform = FindChildRecursive(transform, "GoldText");
            }
            
            if (goldTextTransform != null)
            {
                goldText = goldTextTransform.GetComponent<TMP_Text>();
                if (goldText != null)
                {
                    Debug.Log($"✅ [LobbyInventoryUI] goldText 자동 할당 성공: {goldTextTransform.name}");
                }
                else
                {
                    Debug.LogError($"❌ [LobbyInventoryUI] GoldText GameObject는 찾았지만 TMP_Text 컴포넌트가 없음!");
                }
            }
            else
            {
                Debug.LogError($"❌ [LobbyInventoryUI] GoldText GameObject를 찾을 수 없음!");
            }
        }
        
        if (goldText != null)
        {
            Debug.Log($"📋 [LobbyInventoryUI] goldText 상세 정보:");
            Debug.Log($"   - GameObject 이름: {goldText.gameObject.name}");
            Debug.Log($"   - 활성화 상태: {goldText.gameObject.activeInHierarchy}");
            Debug.Log($"   - 부모: {(goldText.transform.parent != null ? goldText.transform.parent.name : "없음")}");
            Debug.Log($"   - 경로: {GetGameObjectPath(goldText.gameObject)}");
        }
        
        // 골드 변경 이벤트 구독 (PlayerDataManager)
        if (PlayerDataManager.Instance != null)
        {
            // 중복 구독 방지
            PlayerDataManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
            PlayerDataManager.Instance.OnGoldChanged += UpdateGoldDisplay;
            Debug.Log($"✅ [LobbyInventoryUI] PlayerDataManager.OnGoldChanged 구독 완료");
        }
        else
        {
            Debug.LogError($"❌ [LobbyInventoryUI] PlayerDataManager.Instance가 NULL입니다!");
        }
        
        // ⭐ V2: AccountDataManager 이벤트도 구독
        if (AccountDataManager.Instance != null)
        {
            // 중복 구독 방지
            AccountDataManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
            AccountDataManager.Instance.OnGoldChanged += UpdateGoldDisplay;
            Debug.Log($"✅ [LobbyInventoryUI] AccountDataManager.OnGoldChanged 구독 완료");
        }
        else
        {
            Debug.LogError($"❌ [LobbyInventoryUI] AccountDataManager.Instance가 NULL입니다!");
        }
        
        // 초기 골드 표시
        int currentGold = PlayerDataManager.Instance?.CurrentGold ?? 0;
        Debug.Log($"🔍 [LobbyInventoryUI] 현재 골드: {currentGold}");
        UpdateGoldDisplay(currentGold);
    }

    /// <summary>
    /// 슬롯 생성 및 초기화
    /// </summary>
    private void SetupSlots()
    {
        if (slotContainer == null || slotPrefab == null)
        {
            Debug.LogError($"🔴 [LobbyInventoryUI] SlotContainer 또는 SlotPrefab이 설정되지 않았습니다!");
            return;
        }

        // 기존 슬롯들 정리
        foreach (Transform child in slotContainer)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        lobbySlots.Clear();

        // ⭐ AccountData에서 최대 크기 가져오기 (기본 64칸, 확장 가능)
        int maxSlots = 64; // 기본값 (8x8 그리드)
        if (AccountDataManager.IsInitialized())
        {
            maxSlots = AccountDataManager.Instance.GetAccountData().maxSharedInventorySize;
        }

        // 새 슬롯들 생성
        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObject = Instantiate(slotPrefab, slotContainer);
            slotObject.name = $"LobbyInventorySlot_{i}";  // 이름 설정
            
            InventorySlot slot = slotObject.GetComponent<InventorySlot>();
            if (slot != null)
            {
                slot.SetEquipmentData(null); // 빈 슬롯으로 초기화
                lobbySlots.Add(slot);
            }
        }

        if (showDebugLogs)
            Debug.Log($"✅ [LobbyInventoryUI] {lobbySlots.Count}개 슬롯 생성 완료 (최대: {maxSlots})");
        
        // 🔧 중요: SetupSlotClickEvents() 호출 제거
        // InventorySlot.Awake()의 기본 리스너 사용 (UIButtonClickEffect 호환)
        // InventorySlot.OnSlotClicked()가 PlayerDataManager 이벤트를 통해 LobbyInventoryUI.OnSlotClicked() 호출
    }

    private void SetupEventListeners()
    {
        // 🔧 UI 이벤트만 처리 (InventoryController 의존성 제거)
        if (inventoryToggleButton != null)
        {
            inventoryToggleButton.onClick.AddListener(() => {
                Debug.Log($"🖱️ [LobbyInventoryUI] 가방버튼 클릭됨!");
                
                // 🔧 수정: LobbyInventoryController를 통해 처리
                var lobbyInventoryController = FindObjectOfType<LobbyInventoryController>();
                if (lobbyInventoryController != null)
                {
                    lobbyInventoryController.OpenInventory();
                }
                else
                {
                    Debug.LogWarning("⚠️ [LobbyInventoryUI] LobbyInventoryController를 찾을 수 없음");
                }
            });
            Debug.Log($"✅ [LobbyInventoryUI] 가방버튼 이벤트 연결 완료");
        }
        else
        {
            Debug.LogError($"🔴 [LobbyInventoryUI] inventoryToggleButton이 null입니다!");
        }

        // 패널 닫기 버튼
        if (closePanelButton != null)
        {
            closePanelButton.onClick.AddListener(ClosePanel);
        }
        
        // 📑 탭 버튼 이벤트
        if (equipmentTabButton != null)
        {
            equipmentTabButton.onClick.AddListener(() => SwitchTab(InventoryTabType.Equipment));
            Debug.Log("✅ [LobbyInventoryUI] 장비 탭 버튼 이벤트 연결 완료");
        }
        
        if (materialTabButton != null)
        {
            materialTabButton.onClick.AddListener(() => SwitchTab(InventoryTabType.Material));
            Debug.Log("✅ [LobbyInventoryUI] 재료 탭 버튼 이벤트 연결 완료");
        }

        // PlayerDataManager 이벤트 구독 (지연 갱신 지원)
        if (PlayerDataManager.Instance != null)
        {
            // 🆕 장비 착용/해제 이벤트 구독 (인벤토리 + 장비창 동기화)
            PlayerDataManager.Instance.OnItemEquipped += OnItemEquippedHandler;
            PlayerDataManager.Instance.OnItemUnequipped += OnItemUnequippedHandler;
            
            PlayerDataManager.Instance.OnItemAddedToInventory += OnItemAdded;
            PlayerDataManager.Instance.OnItemRemovedFromInventory += OnItemRemoved;
            
            // 🆕 지연 로드 완료 이벤트 구독
            PlayerDataManager.Instance.OnSlotLazyLoaded += OnSlotLazyLoaded;
        }
        
        // AccountDataManager 이벤트 구독 (재료 변경)
        if (AccountDataManager.Instance != null)
        {
            AccountDataManager.Instance.OnMaterialChanged += OnMaterialChangedHandler;
            Debug.Log("✅ [LobbyInventoryUI] AccountDataManager 재료 이벤트 구독 완료");
        }
    }
    
    /// <summary>
    /// 재료 변경 이벤트 핸들러
    /// </summary>
    private void OnMaterialChangedHandler(MaterialType materialType, int newCount)
    {
        if (showDebugLogs)
            Debug.Log($"🔄 [LobbyInventoryUI] 재료 변경 감지: {materialType.GetDisplayName()} = {newCount}개");
        
        // 재료 탭이 활성화된 경우에만 갱신
        if (currentTab == InventoryTabType.Material && inventoryPanel != null && inventoryPanel.activeInHierarchy)
        {
            RefreshCurrentTab();
        }
    }

    /// <summary>
    /// 🆕 지연 로드 완료 시 인벤토리 갱신
    /// </summary>
    private void OnSlotLazyLoaded(int slotIndex)
    {
        if (showDebugLogs)
            Debug.Log($"🔄 [LobbyInventoryUI] 슬롯 {slotIndex} 지연 로드 완료 - 인벤토리 갱신");
        
        // 인벤토리 패널이 활성화된 상태에서만 갱신
        if (inventoryPanel != null && inventoryPanel.activeInHierarchy)
        {
            RefreshInventoryUI();
        }
    }

    
    /// <summary>
    /// 🔧 V2: 보관창고에서 직접 장비 착용 (ItemInstanceId 기반)
    /// </summary>
    private bool TryEquipItem(EquipmentData equipment, int uiSlotIndex)
    {
        if (PlayerDataManager.Instance == null) return false;
        
        if (showDebugLogs)
            Debug.Log($"🎯 [LobbyInventoryUI] 보관창고에서 착용 시도: {equipment.equipmentName} (슬롯 인덱스: {uiSlotIndex})");
        
        // 🆕 V2: UI 슬롯에서 ItemInstanceId 가져오기
        if (uiSlotIndex < 0 || uiSlotIndex >= lobbySlots.Count)
        {
            Debug.LogError($"🔴 [LobbyInventoryUI] 잘못된 슬롯 인덱스: {uiSlotIndex}");
            return false;
        }
        
        ItemInstanceId itemId = lobbySlots[uiSlotIndex].GetItemInstanceId();
        if (!itemId.IsValid())
        {
            Debug.LogError($"🔴 [LobbyInventoryUI] 슬롯 {uiSlotIndex}에 유효한 ItemInstanceId 없음");
            return false;
        }
        
        Debug.Log($"   - ItemInstanceId: {itemId.id.Substring(0, 8)}...");
        
        // ⭐ V2: 클래스 호환성 체크는 EquipItemFromSharedStorage()에서 처리
        // 보관창고 → 직접 장착 (단일 소스: PlayerDataManager.Instance.selectedPlayerData)
        bool success = PlayerDataManager.Instance.EquipItemFromSharedStorage(itemId);  // 🆕 ID 전달
        
        if (showDebugLogs)
            Debug.Log($"{(success ? "✅" : "❌")} [LobbyInventoryUI] 보관창고 착용 {(success ? "성공" : "실패")}: {equipment.equipmentName}");
        
        return success;
    }
    
    /// <summary>
    /// 🔄 PlayerType을 PlayerClass로 변환
    /// </summary>
    private PlayerClass ConvertToPlayerClass(PlayerType playerType)
    {
        switch (playerType)
        {
            case PlayerType.Warrior: return PlayerClass.Warrior;
            case PlayerType.Assasin: return PlayerClass.Assasin;
            case PlayerType.Wizard: return PlayerClass.Wizard;
            case PlayerType.None: return PlayerClass.None;
            default: return PlayerClass.None;
        }
    }
    
    /// <summary>
    /// 🎯 장비 타입에 따른 대상 슬롯 결정
    /// </summary>
    private EquipmentSlot GetTargetSlot(EquipmentData equipment)
    {
        switch (equipment.equipmentType)
        {
            case EquipmentType.Weapon:
                return EquipmentSlot.MainWeapon;
            case EquipmentType.Armor:
                return EquipmentSlot.Armor;
            case EquipmentType.Accessory:
                // 악세서리는 세부 타입에 따라 결정
                if (equipment.equipmentName.Contains("Boots"))
                    return EquipmentSlot.Boots;
                else if (equipment.equipmentName.Contains("Helmet"))
                    return EquipmentSlot.Helmet;
                else if (equipment.equipmentName.Contains("Gloves"))
                    return EquipmentSlot.Gloves;
                else if (equipment.equipmentName.Contains("Belt"))
                    return EquipmentSlot.Belt;
                else if (equipment.equipmentName.Contains("Ring"))
                    return GetAvailableRingSlot();
                else if (equipment.equipmentName.Contains("Necklace"))
                    return EquipmentSlot.Necklace;
                break;
        }
        
        return (EquipmentSlot)(-1); // 🔧 None 대신 -1 반환
    }
    
    /// <summary>
    /// 🔍 사용 가능한 반지 슬롯 찾기
    /// </summary>
    private EquipmentSlot GetAvailableRingSlot()
    {
        var equippedItems = PlayerDataManager.Instance.EquippedItems;
        
        // Ring1이 비어있으면 Ring1 사용
        if (!equippedItems.ContainsKey(EquipmentSlot.Ring1) || equippedItems[EquipmentSlot.Ring1] == null)
            return EquipmentSlot.Ring1;
        
        // Ring1이 차있으면 Ring2 사용
        if (!equippedItems.ContainsKey(EquipmentSlot.Ring2) || equippedItems[EquipmentSlot.Ring2] == null)
            return EquipmentSlot.Ring2;
        
        // 둘 다 차있으면 Ring1에 교체
        return EquipmentSlot.Ring1;
    }

    // 🗑️ Phase 3: DetailPanel 제거됨 - ItemDetailPopup으로 대체
    /*
    /// <summary>
    /// 상세 정보 패널 표시 - 🆕 착용 버튼 포함
    /// </summary>
    public void ShowItemDetailPanel(EquipmentData equipmentData)
    {
        if (itemDetailPanel == null) return;

        // 🔧 수정: equipmentData가 null이면 빈 상태 표시
        if (equipmentData == null)
        {
            ShowEmptyDetailPanel();
            return;
        }

        // 패널 활성화
        itemDetailPanel.SetActive(true);

        // 🔧 아이템 이미지 설정 (우선순위: equipmentData.icon)
        if (detailItemIcon != null)
        {
            if (equipmentData.icon != null)
            {
                detailItemIcon.sprite = equipmentData.icon;
                detailItemIcon.color = Color.white;
                detailItemIcon.gameObject.SetActive(true);
                if (showDebugLogs)
                    Debug.Log($"🖼️ [LobbyInventoryUI] 아이템 이미지 설정 성공: {equipmentData.equipmentName}");
            }
            else if (showDebugLogs) // 🔧 수정: defaultItemIcon 제거
                Debug.LogWarning($"⚠️ [LobbyInventoryUI] 표시할 이미지 없음: {equipmentData.equipmentName}");
        }

        // 🔧 텍스트 요소들 활성화
        if (itemNameText != null)
        {
            itemNameText.text = equipmentData.equipmentName;
            itemNameText.gameObject.SetActive(true);
        }

        if (itemGradeText != null)
        {
            itemGradeText.text = GetGradeText(equipmentData);
            itemGradeText.gameObject.SetActive(true);
        }

        // 🔧 스탯 텍스트들 활성화 후 설정
        if (stat1Text != null) stat1Text.gameObject.SetActive(true);
        if (stat2Text != null) stat2Text.gameObject.SetActive(true);
        if (stat3Text != null) stat3Text.gameObject.SetActive(true);

        // 타입별 스탯 정보 설정
        SetStatsByEquipmentType(equipmentData);

        // 🆕 착용 버튼 및 경고 메시지 제어 (기존 SetupEquipButton 사용)
        bool isCompatible = IsItemCompatibleWithCurrentClass(equipmentData);
        SetupEquipButton(isCompatible);

        // 🔧 착용 안내 텍스트 숨김 (아이템이 있을 때)
        if (equipText != null)
        {
            equipText.gameObject.SetActive(false);
        }

        // �� 추가 디버깅: 버튼 상태 재확인
        if (equipButton != null)
        {
            Debug.Log($"🔘 [LobbyInventoryUI] 아이템 선택 후 버튼 상태:");
            Debug.Log($"   - interactable: {equipButton.interactable}");
            Debug.Log($"   - activeInHierarchy: {equipButton.gameObject.activeInHierarchy}");
            Debug.Log($"   - 호환성: {isCompatible}");
        }

        if (showDebugLogs)
            Debug.Log($"✅ [LobbyInventoryUI] 상세 정보 패널 표시 완료: {equipmentData.equipmentName}");
    }
    
    /// <summary>
    /// 🆕 착용 버튼 설정 (경고 메시지는 버튼 클릭 시에만)
    /// </summary>
    private void SetupEquipButton(bool isCompatible)
    {
        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(true);
            equipButton.interactable = true; // 🔧 버튼 활성화
            
            // 버튼 텍스트 설정
            var buttonText = equipButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                if (isCompatible)
                {
                    buttonText.text = "착용";
                    buttonText.color = Color.white;
                }
                else
                {
                    buttonText.text = "착용 불가";
                    buttonText.color = Color.gray;
                }
            }
        }
        
        // 🔧 착용불가 메시지 표시 (호환되지 않을 때)
        if (equipWarningText != null)
        {
            if (!isCompatible)
            {
                equipWarningText.text = "현재 클래스가 사용할 수 없는 장비입니다";
                equipWarningText.gameObject.SetActive(true);
                
                if (showDebugLogs)
                    Debug.Log("⚠️ [LobbyInventoryUI] 착용불가 메시지 표시");
            }
            else
            {
                equipWarningText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 🔍 아이템이 현재 클래스와 호환되는지 확인
    /// </summary>
    private bool IsItemCompatibleWithCurrentClass(EquipmentData equipment)
    {
        if (PlayerDataManager.Instance?.selectedPlayerData == null) return false;
        
        var currentClass = PlayerDataManager.Instance.selectedPlayerData.selectedPlayerType;
        PlayerClass playerClass = currentClass switch
        {
            PlayerType.Warrior => PlayerClass.Warrior,
            PlayerType.Assasin => PlayerClass.Assasin, // 🔧 수정: Assassin → Assasin
            PlayerType.Wizard => PlayerClass.Wizard,
            _ => PlayerClass.Warrior
        };
        
        return equipment.IsCompatibleWith(playerClass);
    }
    
    /// <summary>
    /// 🆕 착용 버튼 클릭 처리 (Phase 4: 귀속 경고 통합)
    /// </summary>
    private void OnEquipButtonClicked()
    {
        if (currentSelectedItem == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("⚠️ [LobbyInventoryUI] 착용할 아이템이 선택되지 않음");
            return;
        }
        
        if (showDebugLogs)
            Debug.Log($"🎯 [LobbyInventoryUI] 착용 버튼 클릭: {currentSelectedItem.equipmentName}");
        
        // 클래스 호환성 체크
        if (!IsItemCompatibleWithCurrentClass(currentSelectedItem))
        {
            // 🔧 착용 불가능한 클래스 - 일시적 경고 메시지 표시
            Debug.Log($"🚫 [LobbyInventoryUI] {currentSelectedItem.equipmentName}는 현재 클래스가 사용할 수 없는 장비입니다!");
            
            // 🆕 착용 실패 메시지 표시 (착용 성공과 동일한 방식)
            if (equipWarningText != null)
            {
                StartCoroutine(ShowTemporaryMessage("클래스가 다릅니다", Color.red, 2f));
            }
            return;
        }
        
        // ⭐ Phase 4: 귀속 경고 체크
        TryEquipItemWithBindWarning(currentSelectedItem, currentSelectedSlotIndex);
    }
    
    /// <summary>
    /// 🆕 Phase 4: 귀속 경고 팝업 통합 장착 (비동기)
    /// </summary>
    private void TryEquipItemWithBindWarning(EquipmentData equipment, int uiSlotIndex)
    {
        // 1. UI 슬롯에서 ItemInstanceId 가져오기
        if (uiSlotIndex < 0 || uiSlotIndex >= lobbySlots.Count)
        {
            Debug.LogError($"🔴 [LobbyInventoryUI] 잘못된 슬롯 인덱스: {uiSlotIndex}");
            if (equipWarningText != null)
            {
                StartCoroutine(ShowTemporaryMessage("착용 실패", Color.red, 2f));
            }
            return;
        }
        
        ItemInstanceId itemId = lobbySlots[uiSlotIndex].GetItemInstanceId();
        if (!itemId.IsValid())
        {
            Debug.LogError($"🔴 [LobbyInventoryUI] 슬롯 {uiSlotIndex}에 유효한 ItemInstanceId 없음");
            if (equipWarningText != null)
            {
                StartCoroutine(ShowTemporaryMessage("착용 실패", Color.red, 2f));
            }
            return;
        }
        
        // 2. 귀속 경고가 필요한지 확인
        if (BindWarningManager.Instance != null && BindWarningManager.Instance.ShouldShowWarning(itemId))
        {
            // 3. 귀속 경고 데이터 생성
            var account = AccountDataManager.Instance;
            var instance = account.GetInstance(itemId);
            var playerData = PlayerDataManager.Instance;
            var slotData = playerData.GetSlotData(playerData.CurrentSlotIndex);
            
            var warningData = new Systems.BindWarningData(
                itemId,
                instance.templateName,
                instance.enhancementLevel,
                GetTargetSlot(equipment),
                playerData.CurrentSlotIndex,
                slotData?.playerName ?? "Unknown"
            );
            
            // 4. 귀속 경고 팝업 표시 및 사용자 응답 대기
            BindWarningManager.Instance.ShowWarningAndWaitForResponse(warningData, (userConfirmed) =>
            {
                if (userConfirmed)
                {
                    // 사용자 확인 → 장착 진행
                    ExecuteEquipItem(equipment, uiSlotIndex);
                }
                else
                {
                    // 사용자 취소 → 장착 중단
                    if (showDebugLogs)
                        Debug.Log($"🚫 [LobbyInventoryUI] 사용자가 장착 취소: {equipment.equipmentName}");
                    
                    if (equipWarningText != null)
                    {
                        StartCoroutine(ShowTemporaryMessage("장착 취소", Color.yellow, 1.5f));
                    }
                }
            });
        }
        else
        {
            // 귀속 경고 불필요 → 바로 장착
            ExecuteEquipItem(equipment, uiSlotIndex);
        }
    }
    
    /// <summary>
    /// 🆕 Phase 4: 실제 장착 실행 (귀속 경고 후 호출)
    /// </summary>
    private void ExecuteEquipItem(EquipmentData equipment, int uiSlotIndex)
    {
        bool equipped = TryEquipItem(equipment, uiSlotIndex);
        
        if (equipped)
        {
            if (showDebugLogs)
                Debug.Log($"✅ [LobbyInventoryUI] 아이템 착용 성공: {equipment.equipmentName}");
            
            // 🔧 착용 성공 후 빈 상태 DetailPanel 표시
            ShowEmptyDetailPanel();
            
            // 인벤토리 UI 새로고침 (착용된 아이템이 인벤토리에서 제거됨)
            RefreshInventoryUI();
            
            // 🆕 착용 성공 메시지 표시
            if (equipWarningText != null)
            {
                StartCoroutine(ShowTemporaryMessage("착용 완료!", Color.green, 1.5f));
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.LogError($"❌ [LobbyInventoryUI] 아이템 착용 실패: {equipment.equipmentName}");
                
            // 🆕 착용 실패 메시지 표시
            if (equipWarningText != null)
            {
                StartCoroutine(ShowTemporaryMessage("착용 실패", Color.red, 2f));
            }
        }
    }
    
    /// <summary>
    /// 🆕 통합된 일시적 메시지 표시 (성공/실패 공통)
    /// </summary>
    private IEnumerator ShowTemporaryMessage(string message, Color color, float duration)
    {
        if (equipWarningText == null) yield break;
        
        // 메시지 표시
        equipWarningText.gameObject.SetActive(true);
        equipWarningText.text = message;
        equipWarningText.color = color;
        equipWarningText.fontSize = 8f;
        
        if (showDebugLogs)
            Debug.Log($"📢 [LobbyInventoryUI] 메시지 표시: {message}");
        
        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(duration);
        
        // 메시지 숨김
        equipWarningText.gameObject.SetActive(false);
        
        if (showDebugLogs)
            Debug.Log($"📢 [LobbyInventoryUI] 메시지 숨김: {message}");
    }

    /// <summary>
    /// 장비 타입별 스탯 정보 설정
    /// </summary>
    private void SetStatsByEquipmentType(EquipmentData equipmentData)
    {
        switch (equipmentData.equipmentType)
        {
            case EquipmentType.Weapon:
                // 무기: 공격력, 공격속도, 치명타율
                SetStatText(stat1Text, "공격력", equipmentData.attackDamage);
                SetStatText(stat2Text, "공격속도", equipmentData.attackSpeed);
                SetStatText(stat3Text, "치명타율", equipmentData.criticalChance);
                break;

            case EquipmentType.Armor:
                // 방어구: 방어력, 체력, 이동속도
                SetStatText(stat1Text, "방어력", equipmentData.defenseBonus);
                SetStatText(stat2Text, "체력", equipmentData.healthBonus);
                SetStatText(stat3Text, "이동속도", equipmentData.speedBonus);
                break;

            case EquipmentType.Accessory:
                // 악세서리: 다양한 효과
                SetStatText(stat1Text, "방어력", equipmentData.defenseBonus);
                SetStatText(stat2Text, "체력", equipmentData.healthBonus);
                SetStatText(stat3Text, "이동속도", equipmentData.speedBonus);
                break;

            default:
                // 기타 아이템
                SetStatText(stat1Text, "효과", 0);
                SetStatText(stat2Text, "", 0);
                SetStatText(stat3Text, "", 0);
                break;
        }
    }

    /// <summary>
    /// 개별 스탯 텍스트 설정
    /// </summary>
    private void SetStatText(TMP_Text textComponent, string statName, float value)
    {
        if (textComponent != null)
        {
            if (string.IsNullOrEmpty(statName))
            {
                textComponent.text = "";
            }
            else if (value > 0)
            {
                textComponent.text = $"{statName}: +{value:F1}";
            }
            else
            {
                textComponent.text = $"{statName}: {value:F1}";
            }
        }
    }

    /// <summary>
    /// 등급 텍스트 생성
    /// </summary>
    private string GetGradeText(EquipmentData equipmentData)
    {
        // 장비 이름에서 등급 추출 또는 기본값 반환
        if (equipmentData.equipmentName.Contains("_S_"))
            return "S급";
        else if (equipmentData.equipmentName.Contains("_A_"))
            return "A급";
        else if (equipmentData.equipmentName.Contains("_B_"))
            return "B급";
        else if (equipmentData.equipmentName.Contains("_C_"))
            return "C급";
        else
            return "일반";
    }

    */
    
    /// <summary>
    /// 🔧 수정: 패널 닫기 (InventoryController 의존성 제거 + P0 버그 수정)
    /// </summary>
    public void ClosePanel()
    {
        // ⭐ 탭 리셋: 다음에 열 때 항상 장비탭부터 시작
        currentTab = InventoryTabType.Equipment;
        UpdateTabButtonStates();
        
        if (showDebugLogs)
            Debug.Log($"🏠 [LobbyInventoryUI] ClosePanel 호출 - 로비로 전환 시작 (탭 리셋: 장비)");
        
        // ✅ 수정: OnBackToLobby() 호출하여 저장 로직 실행
        // LobbyUIController를 찾아서 로비 전환 요청
        LobbyUIController lobbyUIController = FindObjectOfType<LobbyUIController>();
        if (lobbyUIController != null)
        {
            // ✅ ShowLobbyPanel() 대신 OnBackToLobby() 호출 (저장 포함)
            lobbyUIController.OnBackToLobby();
            
            if (showDebugLogs)
                Debug.Log($"✅ [LobbyInventoryUI] 로비 전환 완료");
        }
        else
        {
            Debug.LogError($"❌ [LobbyInventoryUI] LobbyUIController를 찾을 수 없습니다!");
        }
    }
    
    // 🗑️ Phase 3: DetailPanel 제거됨
    /*
    /// <summary>
    /// 🔧 수정: 상세 패널 닫기 제거 (항상 활성화 상태 유지)
    /// </summary>
    public void CloseDetailPanel()
    {
        // 🔧 수정: DetailPanel 비활성화 로직 제거
        // DetailPanel은 항상 활성화 상태 유지
        if (showDebugLogs)
            Debug.Log($"🎯 [LobbyInventoryUI] DetailPanel 활성화 상태 유지 (비활성화하지 않음)");
    }

    */
    
    /// <summary>
    /// 🔄 인벤토리 UI 새로고침 (V2: 계정 공유 창고)
    /// </summary>
    public void RefreshInventoryUI()
    {
        Debug.Log($"═══════════════════════════════════════════════════════");
        Debug.Log($"🔄 [LobbyInventoryUI] RefreshInventoryUI() 호출 (V2: 계정 공유 창고)");
        Debug.Log($"═══════════════════════════════════════════════════════");
        
        // ⭐ ScrollRect Position 저장 (스크롤 위치 유지)
        Vector2 savedScrollPosition = Vector2.zero;
        bool hasScrollRect = scrollRect != null;
        if (hasScrollRect)
        {
            savedScrollPosition = scrollRect.normalizedPosition;
            Debug.Log($"💾 [LobbyInventoryUI] 스크롤 위치 저장: {savedScrollPosition} (vertical: {savedScrollPosition.y})");
        }
        else
        {
            Debug.LogWarning($"⚠️ [LobbyInventoryUI] scrollRect가 null입니다! Unity Editor에서 ScrollRect 컴포넌트를 할당하세요.");
        }
        
        // 🆕 V2: AccountDataManager 확인
        if (AccountDataManager.Instance == null)
        {
            Debug.LogError("❌ [LobbyInventoryUI] AccountDataManager.Instance가 null입니다");
            return;
        }
        
        // 🆕 슬롯이 생성되지 않았으면 생성 후 재시도
        if (lobbySlots == null || lobbySlots.Count == 0)
        {
            Debug.Log("🔄 [LobbyInventoryUI] 슬롯이 없음 - 슬롯 생성 후 재시도");
            
            SetupSlots();
            
            // 슬롯 생성 후에도 없으면 에러
            if (lobbySlots == null || lobbySlots.Count == 0)
            {
                Debug.LogError("❌ [LobbyInventoryUI] 슬롯 생성 실패!");
                return;
            }
        }
        
        // 🆕 V2: 계정 공유 창고 데이터 가져오기
        var accountData = AccountDataManager.Instance.GetAccountData();
        var sharedInventoryIds = accountData?.sharedInventoryIds;
        
        Debug.Log($"📦 [LobbyInventoryUI] 계정 공유 창고 데이터 확인:");
        Debug.Log($"   - 공유 창고 아이템 수: {sharedInventoryIds?.Count ?? 0}");
        Debug.Log($"   - 슬롯 수: {lobbySlots?.Count ?? 0}");
        Debug.Log($"🔍 [LobbyInventoryUI] accountData 해시코드: {accountData?.GetHashCode() ?? 0}");
        
        // 🆕 V2: ItemInstanceId → EquipmentData 변환 (ID도 함께 저장)
        List<(EquipmentData equipment, ItemInstanceId instanceId)> inventoryItems = new List<(EquipmentData, ItemInstanceId)>();
        
        if (sharedInventoryIds != null)
        {
            for (int i = 0; i < sharedInventoryIds.Count; i++)
            {
                try
                {
                    var instanceId = sharedInventoryIds[i];
                    var instanceData = AccountDataManager.Instance.GetInstance(instanceId);
                    
                    if (instanceData != null)
                    {
                        var template = ItemTemplateResolver.Load(instanceData.templateName);
                        if (template != null)
                        {
                            inventoryItems.Add((template, instanceId));  // 🆕 ID도 함께 저장
                            
                            if (i < 5) // 처음 5개만 로그
                            {
                                string idPreview = instanceId.id != null && instanceId.id.Length >= 8 
                                    ? instanceId.id.Substring(0, 8) 
                                    : instanceId.id;
                                Debug.Log($"   📦 공유창고[{i}]: {template.equipmentName} (ID: {idPreview}...)");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"⚠️ [LobbyInventoryUI] 템플릿 로드 실패: {instanceData.templateName}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ [LobbyInventoryUI] 인스턴스 데이터 없음: {instanceId.id}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ [LobbyInventoryUI] 아이템 로드 중 예외 발생 (인덱스: {i}): {ex.Message}\n{ex.StackTrace}");
                    // 루프 계속 진행 (다른 아이템도 로드)
                }
            }
        }

        // ⭐ 슬롯 데이터 설정 (🆕 ItemInstanceId도 함께 전달)
        // 중요: 재료 탭에서 비활성화된 슬롯을 복원하기 위해 명시적으로 활성화
        for (int i = 0; i < lobbySlots.Count; i++)
        {
            lobbySlots[i].gameObject.SetActive(true); // ⭐ 모든 슬롯 활성화 (재료 탭 복원)
            
            if (i < inventoryItems.Count)
            {
                lobbySlots[i].SetEquipmentData(inventoryItems[i].equipment, inventoryItems[i].instanceId);  // 🆕 ID 전달
                
                if (inventoryItems[i].equipment != null && showDebugLogs)
                    Debug.Log($"   ✅ UI 슬롯 {i}에 설정: {inventoryItems[i].equipment.equipmentName}");
            }
            else
            {
                lobbySlots[i].SetEquipmentData(null);  // ID는 default
            }
        }

        Debug.Log($"═══════════════════════════════════════════════════════");
        int maxSlots = AccountDataManager.IsInitialized() 
            ? AccountDataManager.Instance.GetAccountData().maxSharedInventorySize 
            : 64;
        Debug.Log($"✅ [LobbyInventoryUI] 계정 공유 창고 새로고침 완료: {inventoryItems.Count}/{maxSlots}");
        Debug.Log($"═══════════════════════════════════════════════════════");
        
        // ⭐ ScrollRect Position 복원 (다음 프레임에 실행하여 Layout 재계산 완료 후 적용)
        if (hasScrollRect)
        {
            StartCoroutine(RestoreScrollPositionNextFrame(savedScrollPosition));
        }
    }
    
    /// <summary>
    /// ⭐ ScrollRect Position 복원 (다음 프레임)
    /// </summary>
    private IEnumerator RestoreScrollPositionNextFrame(Vector2 position)
    {
        Debug.Log($"⏳ [LobbyInventoryUI] 스크롤 복원 대기 중... (목표: {position})");
        
        yield return null; // 1프레임 대기 (Layout 재계산 완료)
        
        if (scrollRect != null)
        {
            Vector2 beforePosition = scrollRect.normalizedPosition;
            scrollRect.normalizedPosition = position;
            Vector2 afterPosition = scrollRect.normalizedPosition;
            
            Debug.Log($"🔄 [LobbyInventoryUI] 스크롤 위치 복원 시도:");
            Debug.Log($"   - 목표 위치: {position}");
            Debug.Log($"   - 복원 전: {beforePosition}");
            Debug.Log($"   - 복원 후: {afterPosition}");
            Debug.Log($"   - 성공 여부: {Vector2.Distance(afterPosition, position) < 0.01f}");
        }
        else
        {
            Debug.LogError($"❌ [LobbyInventoryUI] scrollRect가 null입니다! (복원 실패)");
        }
    }

    /// <summary>
    /// 🆕 지연 로드와 함께 인벤토리 UI 갱신
    /// </summary>
    private IEnumerator RefreshInventoryUIWithLazyLoad()
    {
        // 캐릭터 데이터 로드
        int selectedSlot = PlayerDataManager.Instance.GetSelectedSlotIndex();
        bool loadSuccess = PlayerDataManager.Instance.LazyLoadSlotData(selectedSlot);
        
        if (!loadSuccess)
        {
            Debug.LogError("❌ [LobbyInventoryUI] 캐릭터 데이터 로드 실패");
            yield break;
        }
        
        yield return new WaitForSeconds(0.1f); // 로드 완료 대기
        
        // UI 갱신 재시도
        RefreshInventoryUI();
    }

    /// <summary>
    /// 아이템 추가 이벤트 처리
    /// </summary>
    private void OnItemAdded(EquipmentData item)
    {
        if (showDebugLogs)
            Debug.Log($"➕ [LobbyInventoryUI] 아이템 추가됨: {item.equipmentName}");
        RefreshInventoryUI();
    }

    /// <summary>
    /// 아이템 제거 이벤트 처리
    /// </summary>
    private void OnItemRemoved(EquipmentData item)
    {
        if (showDebugLogs)
            Debug.Log($"➖ [LobbyInventoryUI] 아이템 제거됨: {item.equipmentName}");
        RefreshInventoryUI();
    }

    /// <summary>
    /// 🔧 수정: 컨트롤러 이벤트 설정 (간소화)
    /// </summary>
    private void SetupControllerEvents()
    {
        // 로비에서는 LobbyInventoryController가 직접 UI 제어
        if (showDebugLogs)
            Debug.Log($"ℹ️ [LobbyInventoryUI] 로비 전용 UI 초기화 완료");
    }
    
    // 🗑️ 완전 제거: 주석 처리된 메서드들 삭제
    // RetrySetupControllerEvents, OnInventoryStateChanged 등

    /// <summary>
    /// 🔍 인벤토리에서 아이템의 슬롯 인덱스 찾기
    /// </summary>
    private int FindInventorySlotIndex(EquipmentData equipment)
    {
        if (PlayerDataManager.Instance?.InventoryItems == null) return -1;
        
        var inventoryItems = PlayerDataManager.Instance.InventoryItems;
        for (int i = 0; i < inventoryItems.Count; i++)
        {
            if (inventoryItems[i] == equipment)
            {
                return i;
            }
        }
        
        return -1; // 찾지 못함
    }

    /// <summary>
    /// 🔍 인벤토리 상태 로깅 (디버깅용)
    /// </summary>
    private void LogInventoryState(string phase)
    {
        if (!showDebugLogs) return;
        
        Debug.Log($"📊 [LobbyInventoryUI] 인벤토리 상태 ({phase}):");
        
        if (PlayerDataManager.Instance?.InventoryItems != null)
        {
            var items = PlayerDataManager.Instance.InventoryItems;
            for (int i = 0; i < Mathf.Min(items.Count, 8); i++)
            {
                Debug.Log($"   [{i}]: {items[i]?.equipmentName ?? "빈 슬롯"}");
            }
            Debug.Log($"   총 {items.Count}개 아이템");
        }
        else
        {
            Debug.Log("   인벤토리 없음");
        }
    }

    /// <summary>
    /// 🆕 외부에서 호출 가능한 인벤토리 새로고침 메서드
    /// </summary>
    public void ForceRefreshInventory()
    {
        if (showDebugLogs)
            Debug.Log("🔄 [LobbyInventoryUI] 강제 인벤토리 새로고침");
        
        // 현재 선택된 캐릭터 기준으로 갱신
        RefreshInventoryUI();
        
        if (showDebugLogs)
            Debug.Log("✅ [LobbyInventoryUI] 강제 인벤토리 새로고침 완료");
    }
    
    /// <summary>
    /// 🆕 외부에서 슬롯 상태 확인용
    /// </summary>
    public void LogSlotStatus()
    {
        if (showDebugLogs)
        {
            Debug.Log($"📊 [LobbyInventoryUI] 슬롯 상태:");
            Debug.Log($"   - 총 슬롯 수: {lobbySlots.Count}");
            Debug.Log($"   - 인벤토리 아이템 수: {(PlayerDataManager.Instance?.InventoryItems?.Count ?? 0)}");
            
            for (int i = 0; i < lobbySlots.Count && i < 5; i++) // 처음 5개만 로그
            {
                var slot = lobbySlots[i];
                var equipmentData = slot.GetEquipmentData();
                Debug.Log($"   - 슬롯 {i}: {(equipmentData?.equipmentName ?? "비어있음")}");
            }
        }
    }

    /// <summary>
    /// 🆕 로비에서 선택된 캐릭터로 인벤토리 강제 갱신 (Z-Order 방식 지원)
    /// </summary>
    public void ForceRefreshWithLobbySelectedCharacter(int lobbySelectedSlot)
    {
        if (showDebugLogs)
            Debug.Log($"🔄 [LobbyInventoryUI] 로비 선택 캐릭터 {lobbySelectedSlot}로 강제 갱신");
        
        // 1. PlayerDataManager의 선택 슬롯을 로비 선택과 동기화
        if (PlayerDataManager.Instance != null)
        {
            int currentSlot = PlayerDataManager.Instance.GetSelectedSlotIndex();
            
            // 로비 선택과 다르면 동기화
            if (currentSlot != lobbySelectedSlot)
            {
                // 🔧 수정: 완전 로드로 변경하여 데이터 손상 방지
                PlayerDataManager.Instance.SelectSlot(lobbySelectedSlot);
                
                if (showDebugLogs)
                    Debug.Log($"🔄 [LobbyInventoryUI] 캐릭터 슬롯 동기화 및 로드: {currentSlot} → {lobbySelectedSlot}");
            }
            
            // 3. 인벤토리 UI 갱신
            RefreshInventoryUI();
            
            // 3.5. 🆕 장비창 UI 갱신 (참조가 있으면)
            if (equippedItemsUI != null)
            {
                equippedItemsUI.ForceRefreshEquippedItems();
                
                if (showDebugLogs)
                    Debug.Log($"✅ [LobbyInventoryUI] 패널 전환 시 장비창 UI 갱신 완료");
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning("⚠️ [LobbyInventoryUI] equippedItemsUI 참조가 null입니다!");
            }
            
            // 4. 🔧 중요: SetupSlotClickEvents() 호출 제거
            // → UIButtonClickEffect와 충돌 (RemoveAllListeners()가 UIButtonClickEffect 리스너 제거)
            // → InventorySlot.Awake()의 기본 리스너 사용 (UIButtonClickEffect 호환)
            
            // 🗑️ Phase 3: DetailPanel 제거됨 - 초기화 로직 불필요
            /*
            // 5. 🔧 수정: DetailPanel 초기화 (아이템 선택 상태가 없을 때만)
            // 📌 중요: 이미 아이템이 선택되어 있으면 초기화하지 않음 (착용 버튼 클릭 보호)
            if (currentSelectedItem == null)
            {
                ShowEmptyDetailPanel();
                
                if (showDebugLogs)
                    Debug.Log($"🎯 [LobbyInventoryUI] 인벤토리 열 때 DetailPanel 초기화 완료");
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log($"⏭️ [LobbyInventoryUI] DetailPanel 초기화 건너뜀 (아이템 선택 상태 유지: {currentSelectedItem.equipmentName})");
            }
            */
            
            if (showDebugLogs)
                Debug.Log($"✅ [LobbyInventoryUI] 로비 선택 캐릭터 {lobbySelectedSlot} 인벤토리 갱신 완료");
        }
    }

    /// <summary>
    /// 🆕 슬롯 클릭 이벤트 재연결
    /// </summary>
    private void SetupSlotClickEvents()
    {
        for (int i = 0; i < lobbySlots.Count; i++)
        {
            if (lobbySlots[i] != null)
            {
                int slotIndex = i; // 클로저 문제 방지
                
                // 기존 이벤트 완전 제거 (Inspector + 코드)
                var button = lobbySlots[i].GetComponent<Button>();
                if (button != null)
                {
                    // 🔧 수정: Inspector Persistent Listeners도 제거
                    button.onClick.RemoveAllListeners(); // 코드로 추가한 리스너 제거
                    
                    // 🔧 추가: Persistent Listeners 제거 (Inspector에서 연결된 것)
                    var persistentEventCount = button.onClick.GetPersistentEventCount();
                    for (int j = persistentEventCount - 1; j >= 0; j--)
                    {
                        // Persistent 이벤트는 제거할 수 없지만, 비활성화할 수 있음
                        // → 근본 해결: Inspector에서 제거하거나, 여기서 새 Button 생성
                        Debug.LogWarning($"⚠️ [LobbyInventoryUI] 슬롯 {i}의 Button에 Inspector Persistent Listener 발견! 제거 필요!");
                    }
                    
                    // 🗑️ Phase 3: OnSlotClicked 제거됨 - ItemDetailPopup이 자동 처리
                    // button.onClick.AddListener(() => OnSlotClicked(lobbySlots[slotIndex].GetEquipmentData(), slotIndex));
                }
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"🖱️ [LobbyInventoryUI] {lobbySlots.Count}개 슬롯 클릭 이벤트 재연결 완료");
    }

    /// <summary>
    /// 🆕 빈 상태 DetailPanel 표시 (원래 Source Image 복원)
    /// </summary>
    // 🗑️ Phase 3: DetailPanel 제거됨
    /*
    public void ShowEmptyDetailPanel()
    {
        // ... 코드 생략 (약 70줄)
    }
    
    private void ShowEquippedItemInDetailPanel()
    {
        // ... 코드 생략 (약 40줄)
    }
    */
    
    /// <summary>
    /// 🆕 장비 착용 이벤트 핸들러 (인벤토리 + 장비창 동기화)
    /// </summary>
    private void OnItemEquippedHandler(EquipmentSlot slot, EquipmentData item)
    {
        if (showDebugLogs)
            Debug.Log($"🔄 [LobbyInventoryUI] 장비 착용 감지: {item.equipmentName} → {slot}");
        
        // 1. 인벤토리 UI 갱신
        RefreshInventoryUI();
        
        // 2. 장비창 UI 갱신 (참조가 있으면)
        if (equippedItemsUI != null)
        {
            equippedItemsUI.ForceRefreshEquippedItems();
            if (showDebugLogs)
                Debug.Log($"✅ [LobbyInventoryUI] 장비창 UI 갱신 완료");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning("⚠️ [LobbyInventoryUI] equippedItemsUI 참조가 null입니다!");
        }
    }
    
    /// <summary>
    /// 🆕 장비 해제 이벤트 핸들러 (인벤토리 + 장비창 동기화)
    /// </summary>
    private void OnItemUnequippedHandler(EquipmentSlot slot, EquipmentData item)
    {
        if (showDebugLogs)
            Debug.Log($"🔄 [LobbyInventoryUI] 장비 해제 감지: {item.equipmentName} ← {slot}");
        
        // 1. 인벤토리 UI 갱신
        RefreshInventoryUI();
        
        // 2. 장비창 UI 갱신 (참조가 있으면)
        if (equippedItemsUI != null)
        {
            equippedItemsUI.ForceRefreshEquippedItems();
            if (showDebugLogs)
                Debug.Log($"✅ [LobbyInventoryUI] 장비창 UI 갱신 완료");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning("⚠️ [LobbyInventoryUI] equippedItemsUI 참조가 null입니다!");
        }
    }
    
    /// <summary>
    /// 🆕 이벤트 구독 해제
    /// </summary>
    private void OnDestroy()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnItemEquipped -= OnItemEquippedHandler;
            PlayerDataManager.Instance.OnItemUnequipped -= OnItemUnequippedHandler;
            PlayerDataManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
        }
        
        // ⭐ V2: AccountDataManager 이벤트 구독 해제
        if (AccountDataManager.Instance != null)
        {
            AccountDataManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
        }
        
        if (showDebugLogs)
            Debug.Log("🔄 [LobbyInventoryUI] 이벤트 구독 해제 완료");
    }
    
    /// <summary>
    /// 골드 표시 업데이트
    /// </summary>
    private void UpdateGoldDisplay(int gold)
    {
        Debug.Log($"🔍 [LobbyInventoryUI] UpdateGoldDisplay 호출됨 - gold: {gold}, goldText: {(goldText != null ? "할당됨" : "NULL")}");
        
        if (goldText != null)
        {
            // 상세 정보 출력
            Debug.Log($"📋 [LobbyInventoryUI] goldText 상세 정보:");
            Debug.Log($"   - GameObject 이름: {goldText.gameObject.name}");
            Debug.Log($"   - 활성화 상태: {goldText.gameObject.activeInHierarchy}");
            Debug.Log($"   - 부모: {(goldText.transform.parent != null ? goldText.transform.parent.name : "없음")}");
            Debug.Log($"   - 현재 텍스트 (업데이트 전): '{goldText.text}'");
            
            // 텍스트 업데이트
            goldText.text = gold.ToString();
            
            Debug.Log($"   - 업데이트 후 텍스트: '{goldText.text}'");
            Debug.Log($"💰 [LobbyInventoryUI] 골드 텍스트 업데이트 완료: {gold}");
        }
        else
        {
            Debug.LogError($"❌ [LobbyInventoryUI] goldText가 NULL입니다! Inspector에서 할당해주세요.");
        }
    }
    
    // ========================================
    // 📑 탭 시스템
    // ========================================
    
    /// <summary>
    /// 탭 전환
    /// </summary>
    public void SwitchTab(InventoryTabType tabType)
    {
        if (currentTab == tabType)
        {
            if (showDebugLogs)
                Debug.Log($"📑 [LobbyInventoryUI] 이미 {tabType} 탭이 활성화되어 있습니다.");
            return;
        }
        
        currentTab = tabType;
        
        if (showDebugLogs)
            Debug.Log($"📑 [LobbyInventoryUI] 탭 전환: {tabType}");
        
        RefreshCurrentTab();
        UpdateTabButtonStates();
    }
    
    /// <summary>
    /// 현재 탭 갱신
    /// </summary>
    private void RefreshCurrentTab()
    {
        switch (currentTab)
        {
            case InventoryTabType.Equipment:
                RefreshEquipmentTab();
                break;
            
            case InventoryTabType.Material:
                RefreshMaterialTab();
                break;
            
            default:
                Debug.LogWarning($"⚠️ [LobbyInventoryUI] 지원하지 않는 탭: {currentTab}");
                break;
        }
    }
    
    /// <summary>
    /// 장비 탭 갱신 (기존 RefreshInventoryUI)
    /// </summary>
    private void RefreshEquipmentTab()
    {
        // 기존 RefreshInventoryUI() 로직을 여기로 이동할 예정
        RefreshInventoryUI();
    }
    
    /// <summary>
    /// 재료 탭 갱신
    /// </summary>
    private void RefreshMaterialTab()
    {
        if (!AccountDataManager.IsInitialized())
        {
            Debug.LogWarning("⚠️ [LobbyInventoryUI] AccountDataManager가 초기화되지 않았습니다.");
            return;
        }
        
        var materials = AccountDataManager.Instance.GetMaterialsForDisplay();
        
        if (showDebugLogs)
            Debug.Log($"📦 [LobbyInventoryUI] 재료 탭 갱신: {materials.Count}개 재료");
        
        // 슬롯 초기화 (16칸만 사용: 8칸 x 2행)
        int maxMaterialSlots = 16;
        
        // 재료 표시
        for (int i = 0; i < maxMaterialSlots; i++)
        {
            if (i < materials.Count)
            {
                lobbySlots[i].SetupMaterial(materials[i]);
                lobbySlots[i].gameObject.SetActive(true);
            }
            else
            {
                lobbySlots[i].ClearSlot();
                lobbySlots[i].gameObject.SetActive(false); // 빈 칸 숨김
            }
        }
        
        // 나머지 슬롯 숨김
        for (int i = maxMaterialSlots; i < lobbySlots.Count; i++)
        {
            lobbySlots[i].gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 탭 버튼 상태 업데이트 (활성/비활성 색상)
    /// </summary>
    private void UpdateTabButtonStates()
    {
        // 활성 탭: 밝게 (Alpha 1.0)
        // 비활성 탭: 어둡게 (Alpha 0.6)
        
        if (equipmentTabText != null)
        {
            Color color = equipmentTabText.color;
            color.a = (currentTab == InventoryTabType.Equipment) ? 1.0f : 0.6f;
            equipmentTabText.color = color;
        }
        
        if (materialTabText != null)
        {
            Color color = materialTabText.color;
            color.a = (currentTab == InventoryTabType.Material) ? 1.0f : 0.6f;
            materialTabText.color = color;
        }
        
        if (showDebugLogs)
            Debug.Log($"📑 [LobbyInventoryUI] 탭 버튼 상태 업데이트 완료 (현재: {currentTab})");
    }
}