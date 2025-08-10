using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private Transform slotContainer;       // 슬롯들이 들어갈 컨테이너
    [SerializeField] private GameObject slotPrefab;         // 상점용 슬롯 프리팹
    [SerializeField] private int maxDisplaySlots = 16;      // 표시할 최대 슬롯 수
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = false; // 🔧 수정: 기본값 false
    
    // 내부 상태
    private List<InventorySlot> shopInventorySlots = new List<InventorySlot>();
    
    // 🔧 수정: 상점 전용 이벤트 (판매용)
    public event Action<EquipmentData, int> OnInventoryItemClicked;
    
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
    /// 이벤트 리스너 설정
    /// </summary>
    private void SetupEventListeners()
    {
        // PlayerDataManager 이벤트 구독
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnInventoryChanged += RefreshInventoryUI;
            PlayerDataManager.Instance.OnSlotClicked += HandleSlotClicked;
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
        
        // 새 슬롯들 생성
        for (int i = 0; i < maxDisplaySlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            slotObj.name = $"ShopInventorySlot_{i}";
            
            InventorySlot inventorySlot = slotObj.GetComponent<InventorySlot>();
            if (inventorySlot != null)
            {
                shopInventorySlots.Add(inventorySlot);
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"🏪 [ShopInventoryUI] {shopInventorySlots.Count}개 슬롯 생성 완료");
    }
    
    /// <summary>
    /// 인벤토리 UI 새로고침
    /// </summary>
    public void RefreshInventoryUI()
    {
        if (PlayerDataManager.Instance == null) return;
        
        var inventoryItems = PlayerDataManager.Instance.InventoryItems;
        
        // 모든 슬롯 초기화
        for (int i = 0; i < shopInventorySlots.Count; i++)
        {
            if (i < inventoryItems.Count)
            {
                shopInventorySlots[i].SetEquipmentData(inventoryItems[i]);
            }
            else
            {
                shopInventorySlots[i].SetEquipmentData(null);
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"🏪 [ShopInventoryUI] 인벤토리 새로고침: {inventoryItems.Count}/{maxDisplaySlots}");
    }
    
    /// <summary>
    /// 슬롯 클릭 처리
    /// </summary>
    private void HandleSlotClicked(EquipmentData equipmentData, int slotIndex)
    {
        // 상점 환경에서만 처리
        if (equipmentData != null)
        {
            OnInventoryItemClicked?.Invoke(equipmentData, slotIndex);
            
            if (showDebugLogs)
                Debug.Log($"🏪 [ShopInventoryUI] 인벤토리 아이템 클릭: {equipmentData.equipmentName}");
        }
    }
    
    /// <summary>
    /// 강제 새로고침 (외부 호출용)
    /// </summary>
    public void ForceRefreshInventory()
    {
        RefreshInventoryUI();
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnInventoryChanged -= RefreshInventoryUI;
            PlayerDataManager.Instance.OnSlotClicked -= HandleSlotClicked;
        }
    }
}