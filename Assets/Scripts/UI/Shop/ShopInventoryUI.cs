using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI.Popups; // ⭐ ItemDetailPopup

/// <summary>
/// 🏪 상점 전용 인벤토리 UI (View Only)
/// - 판매용 아이템 선택만 담당 (착용, 상세 정보 표시 제외)
/// - ShopUIController와 연동하여 판매 처리
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
public class ShopInventoryUI : MonoBehaviour
{
    [Header("🎒 상점 인벤토리 설정")]
    [SerializeField] private ScrollRect scrollRect;         // ⭐ ScrollView의 ScrollRect 컴포넌트
    [SerializeField] private Transform slotContainer;       // 슬롯들이 들어갈 컨테이너 (ScrollView의 Content)
    [SerializeField] private GameObject slotPrefab;         // 상점용 슬롯 프리팹
    
    // ❌ 제거: maxDisplaySlots (AccountData에서 가져옴)
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = false; // 🔧 수정: 기본값 false
    
    // 내부 상태
    private List<InventorySlot> shopInventorySlots = new List<InventorySlot>();
    
    // 🔧 수정: 상점 전용 이벤트 (판매용) - 🆕 V2: ItemInstanceID 추가
    public event Action<EquipmentData, int, ItemInstanceID> OnInventoryItemClicked;
    
    // 🗑️ 제거: 착용, 상세 정보 등 로비 전용 기능 제거
    // (상점에서는 단순히 판매할 아이템 선택만)
    
    void Awake()
    {
        if (showDebugLogs)
            Debug.Log($"🏪 [ShopInventoryUI] 상점 전용 인벤토리 UI 초기화");
        
        SetupEventListeners();
    }

    void Start()
    {
        InitializeShopInventorySlots();
        RefreshInventoryUI();
    }
    
    /// <summary>
    /// 🆕 V2: 패널 활성화 시 자동 갱신
    /// </summary>
    void OnEnable()
    {
        if (showDebugLogs)
            Debug.Log($"🏪 [ShopInventoryUI] OnEnable() - 상점 패널 활성화");
        
        // 슬롯이 초기화된 경우에만 갱신 (Start() 전에 호출 방지)
        if (shopInventorySlots != null && shopInventorySlots.Count > 0)
        {
            RefreshInventoryUI();
        }
    }
    
    /// <summary>
    /// 이벤트 리스너 설정 (근본 해결: OnSlotClicked 구독 제거)
    /// </summary>
    private void SetupEventListeners()
    {
        // PlayerDataManager 이벤트 구독
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnInventoryChanged += RefreshInventoryUI;
            // 🗑️ 제거: OnSlotClicked 구독 (Button.onClick으로 직접 처리)
            // PlayerDataManager.Instance.OnSlotClicked += HandleSlotClicked;
        }
    }
    
    /// <summary>
    /// 상점 인벤토리 슬롯 초기화
    /// </summary>
    private void InitializeShopInventorySlots()
    {
        if (slotContainer == null || slotPrefab == null)
        {
            Debug.LogError($"❌ [ShopInventoryUI] slotContainer 또는 slotPrefab이 할당되지 않음");
            return;
        }
        
        // 기존 슬롯들 정리
        foreach (Transform child in slotContainer)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
        }
        shopInventorySlots.Clear();
        
        // ⭐ AccountData에서 최대 크기 가져오기 (기본 64칸, 확장 가능)
        int maxSlots = 64; // 기본값 (8x8 그리드)
        if (AccountDataManager.IsInitialized())
        {
            maxSlots = AccountDataManager.Instance.GetAccountData().maxSharedInventorySize;
        }
        
        // 새 슬롯들 생성
        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            slotObj.name = $"ShopInventorySlot_{i}";
            
            InventorySlot inventorySlot = slotObj.GetComponent<InventorySlot>();
            if (inventorySlot != null)
            {
                shopInventorySlots.Add(inventorySlot);
            }
        }
        
        // 🆕 근본 해결: Button.onClick 이벤트 직접 등록
        SetupShopSlotClickEvents();
        
        if (showDebugLogs)
            Debug.Log($"🏪 [ShopInventoryUI] {shopInventorySlots.Count}개 슬롯 생성 완료 (최대: {maxSlots})");
    }
    
    /// <summary>
    /// 🆕 Shop 슬롯 클릭 이벤트 등록 (근본 해결)
    /// </summary>
    private void SetupShopSlotClickEvents()
    {
        for (int i = 0; i < shopInventorySlots.Count; i++)
        {
            if (shopInventorySlots[i] != null)
            {
                int slotIndex = i; // 클로저 문제 방지
                
                var button = shopInventorySlots[i].GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => {
                        var equipmentData = shopInventorySlots[slotIndex].GetEquipmentData();
                        var instanceId = shopInventorySlots[slotIndex].GetItemInstanceID();  // 🆕 V2: ID 가져오기
                        HandleSlotClicked(equipmentData, slotIndex, instanceId);
                    });
                }
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"🏪 [ShopInventoryUI] {shopInventorySlots.Count}개 슬롯 클릭 이벤트 등록 완료");
    }
    
    /// <summary>
    /// 🆕 V2: 인벤토리 UI 새로고침 (계정 공유 창고 표시)
    /// </summary>
    public void RefreshInventoryUI()
    {
        if (showDebugLogs)
            Debug.Log($"═══════════════════════════════════════════════════════");
            
        if (showDebugLogs)
            Debug.Log($"🏪 [ShopInventoryUI] RefreshInventoryUI() 호출 (V2: 계정 공유 창고)");
        
        // ⭐ ScrollRect Position 저장 (스크롤 위치 유지)
        Vector2 savedScrollPosition = Vector2.zero;
        bool hasScrollRect = scrollRect != null;
        if (hasScrollRect)
        {
            savedScrollPosition = scrollRect.normalizedPosition;
            if (showDebugLogs)
                Debug.Log($"💾 [ShopInventoryUI] 스크롤 위치 저장: {savedScrollPosition} (vertical: {savedScrollPosition.y})");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [ShopInventoryUI] scrollRect가 null입니다! Unity Editor에서 ScrollRect 컴포넌트를 할당하세요.");
        }
        
        // 🆕 V2: AccountDataManager 확인
        if (AccountDataManager.Instance == null)
        {
            Debug.LogError("❌ [ShopInventoryUI] AccountDataManager.Instance가 null입니다");
            return;
        }
        
        // 🆕 V2: 계정 공유 창고 데이터 가져오기 (로비 보관창고와 동일)
        var accountData = AccountDataManager.Instance.GetAccountData();
        var sharedInventoryIds = accountData?.sharedInventoryIds;
        
        if (showDebugLogs)
        {
            Debug.Log($"📦 [ShopInventoryUI] 계정 공유 창고 데이터 확인:");
            Debug.Log($"   - 공유 창고 아이템 수: {sharedInventoryIds?.Count ?? 0}");
            Debug.Log($"   - 슬롯 수: {shopInventorySlots?.Count ?? 0}");
            Debug.Log($"🔍 [ShopInventoryUI] accountData 해시코드: {accountData?.GetHashCode() ?? 0}");
        }
        
        // 🆕 V2: ItemInstanceID → EquipmentData 변환 (ID도 함께 저장)
        List<(EquipmentData equipment, ItemInstanceID instanceId)> inventoryItems = new List<(EquipmentData, ItemInstanceID)>();
        
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
                            
                            if (i < 5 && showDebugLogs) // 처음 5개만 로그
                            {
                                string idPreview = instanceId.Value != null && instanceId.Value.Length >= 8 
                                    ? instanceId.Value.Substring(0, 8) 
                                    : instanceId.Value;
                                Debug.Log($"   📦 공유창고[{i}]: {template.equipmentName} (ID: {idPreview}...)");
                            }
                        }
                        else
                        {
                            if (showDebugLogs)
                                Debug.LogWarning($"⚠️ [ShopInventoryUI] 템플릿 로드 실패: {instanceData.templateName}");
                        }
                    }
                    else
                    {
                        if (showDebugLogs)
                            Debug.LogWarning($"⚠️ [ShopInventoryUI] 인스턴스 데이터 없음: {instanceId.Value}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ [ShopInventoryUI] 아이템 로드 중 예외 발생 (인덱스: {i}): {ex.Message}\n{ex.StackTrace}");
                    // 루프 계속 진행 (다른 아이템도 로드)
                }
            }
        }

        // 슬롯 데이터 설정 (🆕 ItemInstanceID도 함께 전달)
        for (int i = 0; i < shopInventorySlots.Count; i++)
        {
            if (i < inventoryItems.Count)
            {
                shopInventorySlots[i].SetEquipmentData(inventoryItems[i].equipment, inventoryItems[i].instanceId);  // 🆕 ID 전달
                
                if (inventoryItems[i].equipment != null && showDebugLogs)
                    Debug.Log($"   ✅ 상점 슬롯 {i}에 설정: {inventoryItems[i].equipment.equipmentName}");
            }
            else
            {
                shopInventorySlots[i].SetEquipmentData(null);  // ID는 default
            }
        }
        
        if (showDebugLogs)
        {
            int maxSlots = AccountDataManager.IsInitialized() 
                ? AccountDataManager.Instance.GetAccountData().maxSharedInventorySize 
                : 64;
            Debug.Log($"🏪 [ShopInventoryUI] 인벤토리 새로고침 완료: {inventoryItems.Count}/{maxSlots}");
            Debug.Log($"═══════════════════════════════════════════════════════");
        }
        
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
        if (showDebugLogs)
            Debug.Log($"⏳ [ShopInventoryUI] 스크롤 복원 대기 중... (목표: {position})");
        
        yield return null; // 1프레임 대기 (Layout 재계산 완료)
        
        if (scrollRect != null)
        {
            Vector2 beforePosition = scrollRect.normalizedPosition;
            scrollRect.normalizedPosition = position;
            Vector2 afterPosition = scrollRect.normalizedPosition;
            
            if (showDebugLogs)
            {
                Debug.Log($"🔄 [ShopInventoryUI] 스크롤 위치 복원 시도:");
                Debug.Log($"   - 목표 위치: {position}");
                Debug.Log($"   - 복원 전: {beforePosition}");
                Debug.Log($"   - 복원 후: {afterPosition}");
                Debug.Log($"   - 성공 여부: {Vector2.Distance(afterPosition, position) < 0.01f}");
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.LogError($"❌ [ShopInventoryUI] scrollRect가 null입니다! (복원 실패)");
        }
    }
    
    /// <summary>
    /// 🆕 V2: 슬롯 클릭 처리 (ItemInstanceID 포함)
    /// </summary>
    private void HandleSlotClicked(EquipmentData equipmentData, int slotIndex, ItemInstanceID instanceId)
    {
        // Shop 환경에서만 처리 (이미 Button.onClick으로 호출되므로 활성화 상태 보장됨)
        if (equipmentData != null && !instanceId.IsEmpty)
        {
            // ⭐ 기존 이벤트 유지 (다른 시스템 호환성)
            OnInventoryItemClicked?.Invoke(equipmentData, slotIndex, instanceId);  // 🆕 V2: ID 전달
            
            // ⭐ ItemDetailPopup 열기 (Shop_Sell 컨텍스트)
            ShowItemDetailPopup(equipmentData, slotIndex, instanceId);
            
            if (showDebugLogs)
                Debug.Log($"🏪 [ShopInventoryUI] 인벤토리 아이템 클릭: {equipmentData.equipmentName} (ID: {instanceId.Value.Substring(0, 8)}...)");
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning($"⚠️ [ShopInventoryUI] 빈 슬롯 클릭 또는 잘못된 ID (슬롯: {slotIndex})");
        }
    }
    
    /// <summary>
    /// ⭐ 아이템 상세 팝업 표시 (상점 판매용)
    /// </summary>
    private void ShowItemDetailPopup(EquipmentData equipmentData, int slotIndex, ItemInstanceID instanceId)
    {
        // PopupCanvas에서 ItemDetailPopup 찾기
        var popup = FindObjectOfType<ItemDetailPopup>(true); // includeInactive = true
        
        if (popup == null)
        {
            Debug.LogError("❌ [ShopInventoryUI] ItemDetailPopup을 찾을 수 없습니다!");
            return;
        }
        
        // Shop_Sell 컨텍스트로 팝업 열기
        popup.Show(equipmentData, ItemDetailContext.Shop_Sell, slotIndex, instanceId);
        
        if (showDebugLogs)
            Debug.Log($"🏪 [ShopInventoryUI] ItemDetailPopup 열기: {equipmentData.equipmentName} (판매 모드)");
    }
    
    /// <summary>
    /// 강제 새로고침 (외부 호출용)
    /// </summary>
    public void ForceRefreshInventory()
    {
        RefreshInventoryUI();
    }
    
    /// <summary>
    /// 🆕 추가: 캐릭터 지연 로드 완료 시 상점 인벤토리 갱신
    /// </summary>
    private void OnSlotLazyLoadedForShop(int slotIndex)
    {
        if (showDebugLogs)
            Debug.Log($"🔄 [ShopInventoryUI] 슬롯 {slotIndex} 지연 로드 완료 - 상점 인벤토리 갱신");
        
        // 상점이 활성화된 상태에서만 갱신
        if (gameObject.activeInHierarchy)
        {
            RefreshInventoryUI();
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnInventoryChanged -= RefreshInventoryUI;
            // 🗑️ 제거: OnSlotClicked 구독 해제 (더 이상 구독 안 함)
            // PlayerDataManager.Instance.OnSlotClicked -= HandleSlotClicked;
            PlayerDataManager.Instance.OnSlotLazyLoaded -= OnSlotLazyLoadedForShop;
        }
    }
}