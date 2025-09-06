using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 🏪 상점 UI (View Only)
/// 표시만 담당, 로직은 ShopUIController에서 처리
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("🏪 메인 상점 패널")]
    [SerializeField] private GameObject shopMainPanel;
    [SerializeField] private Button closeShopButton;           
    
    [Header("📋 카테고리 탭 시스템")]
    [SerializeField] private Button weaponTabButton;           
    [SerializeField] private Button armorTabButton;            
    [SerializeField] private Button bootsTabButton;            
    [SerializeField] private GameObject weaponTabContent;      
    [SerializeField] private GameObject armorTabContent;       
    [SerializeField] private GameObject bootsTabContent;   

    [Header("🎨 탭 버튼 색상 설정")]
    [SerializeField] private Color tabNormalColor = new Color(0.7f, 0.7f, 0.7f, 1f);    // 기본 색상
    [SerializeField] private Color tabSelectedColor = Color.white;                        // 선택된 탭 색상
    
    [Header("🎯 상점 아이템 진열소")]
    [SerializeField] private Transform weaponItemContainer;    // 무기 탭 전용 Container
    [SerializeField] private Transform armorItemContainer;     // 방어구 탭 전용 Container  
    [SerializeField] private Transform bootsItemContainer;     // 신발 탭 전용 Container
    [SerializeField] private GameObject shopItemSlotPrefab;    
    [SerializeField] private GridLayoutGroup shopItemGrid;    
    
    [Header("💰 거래 센터")]
    [SerializeField] private TradeCenterUI tradeCenterUI;      
    [SerializeField] private GameObject tradeCenter;           
    
    [Header("🎒 플레이어 인벤토리 (상점 전용)")]
    [SerializeField] private ShopInventoryUI playerInventoryUI;
    
    [Header("🎯 상세 패널 (재사용)")]
    [SerializeField] private GameObject itemDetailPanel;       
    
    [Header("💬 팝업 메시지")]
    [SerializeField] private GameObject transactionPopup;      
    [SerializeField] private TMP_Text popupMessageText;        
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 이벤트 시스템
    public event Action OnCloseShopRequested;
    public event Action<EquipmentType> OnTabChanged;
    public event Action<string> OnShopItemClicked;          
    public event Action OnBuyRequested;
    public event Action OnSellRequested;
    
    // 개별 취소 이벤트
    public event Action OnBuyCancelRequested;
    public event Action OnSellCancelRequested;
    
    // 내부 상태
    private EquipmentType currentTab = EquipmentType.Weapon;
    private string selectedBuyItemID = "";
    private EquipmentData selectedSellItem = null;
    
    // 탭별 슬롯 리스트로 분리
    private Dictionary<EquipmentType, List<ShopItemSlot>> tabShopSlots = new Dictionary<EquipmentType, List<ShopItemSlot>>();

    void Awake()
    {
        // 탭별 슬롯 리스트 초기화
        tabShopSlots[EquipmentType.Weapon] = new List<ShopItemSlot>();
        tabShopSlots[EquipmentType.Armor] = new List<ShopItemSlot>();
        tabShopSlots[EquipmentType.Accessory] = new List<ShopItemSlot>();
    }
    
    void Start()
    {
        InitializeShopUI();
    }
    
    /// <summary>
    /// 상점 UI 초기화
    /// </summary>
    private void InitializeShopUI()
    {
        SetupButtonEvents();
        SetupTabSystem();
        ResetTradeCenter();
        HideTransactionPopup();
        
        if (showDebugLogs)
            Debug.Log("✅ [ShopUI] 상점 UI 초기화 완료");
    }
    
    /// <summary>
    /// 버튼 이벤트 연결
    /// </summary>
    private void SetupButtonEvents()
    {
        // 메인 버튼들
        closeShopButton?.onClick.AddListener(() => OnCloseShopRequested?.Invoke());
        
        // 탭 버튼들
        weaponTabButton?.onClick.AddListener(() => SwitchTab(EquipmentType.Weapon));
        armorTabButton?.onClick.AddListener(() => SwitchTab(EquipmentType.Armor));
        bootsTabButton?.onClick.AddListener(() => SwitchTab(EquipmentType.Accessory));
        
        // 거래 센터 이벤트 연결
        if (tradeCenterUI != null)
        {
            tradeCenterUI.OnBuyRequested += () => OnBuyRequested?.Invoke();
            tradeCenterUI.OnSellRequested += () => OnSellRequested?.Invoke();
            
            // 개별 취소 이벤트 연결
            tradeCenterUI.OnBuyCancelRequested += () => OnBuyCancelRequested?.Invoke();
            tradeCenterUI.OnSellCancelRequested += () => OnSellCancelRequested?.Invoke();
        }
    }
    
    /// <summary>
    /// 탭 시스템 초기화
    /// </summary>
    private void SetupTabSystem()
    {
        // ✅ 이미 올바름: 모든 탭 내용 강제 활성화
        weaponTabContent?.SetActive(true);
        armorTabContent?.SetActive(true);
        bootsTabContent?.SetActive(true);
        
        // 각 탭별로 슬롯 생성
        StartCoroutine(InitializeAllTabsWithSlots());
    }

    /// <summary>
    /// 모든 탭에 슬롯 생성 후 기본 탭으로 전환
    /// </summary>
    private System.Collections.IEnumerator InitializeAllTabsWithSlots()
    {
        // 1프레임 대기 (모든 Container 활성화 완료 대기)
        yield return null;
        
        // 각 탭별로 슬롯 생성
        CreateSlotsForTab(EquipmentType.Weapon);
        CreateSlotsForTab(EquipmentType.Armor);
        CreateSlotsForTab(EquipmentType.Accessory);
        
        // 1프레임 더 대기
        yield return null;
        
        // 기본 탭(무기)으로 전환 (다른 탭들 비활성화)
        SwitchTabWithoutSlotGeneration(EquipmentType.Weapon);
    }

    /// <summary>
    /// 특정 탭의 슬롯 생성
    /// </summary>
    private void CreateSlotsForTab(EquipmentType equipmentType)
    {
        // 임시로 currentTab 변경
        EquipmentType originalTab = currentTab;
        currentTab = equipmentType;
        
        // Container 확인
        Transform currentContainer = GetCurrentTabContainer();
        
        if (currentContainer == null)
        {
            Debug.LogError($"❌ [ShopUI] {equipmentType} Container가 null입니다!");
            currentTab = originalTab;
            return;
        }
        
        if (!currentContainer.gameObject.activeInHierarchy)
        {
            Debug.LogError($"❌ [ShopUI] {equipmentType} Container가 비활성화되어 있습니다!");
            currentTab = originalTab;
            return;
        }
        
        if (ShopInventoryManager.Instance != null)
        {
            // 3x4 그리드 데이터 가져오기
            EquipmentData[,] displayGrid = ShopInventoryManager.Instance.GetDisplayGrid(equipmentType);
            
            // 필요한 슬롯들이 존재하는지 확인하고 생성
            EnsureShopSlotsExist(equipmentType);
            
            int slotIndex = 0;
            // 기존 슬롯들에 데이터만 업데이트
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    EquipmentData equipment = displayGrid[col, row];
                    
                    if (slotIndex < tabShopSlots[equipmentType].Count)
                    {
                        var slot = tabShopSlots[equipmentType][slotIndex];
                        if (slot != null)
                        {
                            slot.SetEquipmentData(equipment);
                        }
                    }
                    
                    slotIndex++;
                }
            }
        }
        else
        {
            Debug.LogError($"❌ [ShopUI] ShopInventoryManager.Instance가 null입니다!");
        }
        
        // currentTab 복원
        currentTab = originalTab;
    }

    /// <summary>
    /// 슬롯 생성 없이 탭 전환 (초기화 전용)
    /// </summary>
    private void SwitchTabWithoutSlotGeneration(EquipmentType tabType)
    {
        currentTab = tabType;
        
        // 🎯 탭 내용 전환 (이제 Z-Order 방식 사용)
        UpdateTabContent();
        
        // 탭 버튼 시각적 업데이트
        UpdateTabVisuals();
    }

    /// <summary>
    /// 특정 탭의 슬롯 개수 반환
    /// </summary>
    private int GetTabSlotCount(EquipmentType tabType)
    {
        return tabShopSlots.ContainsKey(tabType) ? tabShopSlots[tabType].Count : 0;
    }
    
    /// <summary>
    /// 탭 전환 (슬롯은 이미 생성되어 있음, 표시/숨김만 처리)
    /// </summary>
    public void SwitchTab(EquipmentType tabType)
    {
        currentTab = tabType;
        
        // 탭 내용 전환 (Container 활성화/비활성화)
        UpdateTabContent();
        
        // 탭 버튼 시각적 업데이트
        UpdateTabVisuals();
        
        // 컨트롤러에 탭 변경 알림
        OnTabChanged?.Invoke(tabType);
    }
    
    /// <summary>
    /// 탭 버튼 시각적 업데이트
    /// </summary>
    private void UpdateTabVisuals()
    {
        // 모든 탭 버튼을 기본 색상으로 설정
        ResetTabButtonColors();
        
        // 선택된 탭 강조
        switch (currentTab)
        {
            case EquipmentType.Weapon:
                if (weaponTabButton != null)
                    weaponTabButton.GetComponent<Image>().color = tabSelectedColor;
                break;
            case EquipmentType.Armor:
                if (armorTabButton != null)
                    armorTabButton.GetComponent<Image>().color = tabSelectedColor;
                break;
            case EquipmentType.Accessory:
                if (bootsTabButton != null)
                    bootsTabButton.GetComponent<Image>().color = tabSelectedColor;
                break;
        }
    }
    
    /// <summary>
    /// 🔧 수정: 탭 내용 전환 (Z-Order 방식)
    /// </summary>
    private void UpdateTabContent()
    {
        // 🎯 핵심 변경: 비활성화하지 않고 Z-Order로 제어
        // 모든 탭 내용을 활성화 상태로 유지
        weaponTabContent?.SetActive(true);
        armorTabContent?.SetActive(true);
        bootsTabContent?.SetActive(true);
        
        // 선택된 탭을 최상위로 이동
        switch (currentTab)
        {
            case EquipmentType.Weapon:
                weaponTabContent?.transform.SetAsLastSibling();
                break;
            case EquipmentType.Armor:
                armorTabContent?.transform.SetAsLastSibling();
                break;
            case EquipmentType.Accessory:
                bootsTabContent?.transform.SetAsLastSibling();
                break;
        }
    }
    
    /// <summary>
    /// 탭 버튼 색상 초기화
    /// </summary>
    private void ResetTabButtonColors()
    {
        if (weaponTabButton != null)
            weaponTabButton.GetComponent<Image>().color = tabNormalColor;
        if (armorTabButton != null)
            armorTabButton.GetComponent<Image>().color = tabNormalColor;
        if (bootsTabButton != null)
            bootsTabButton.GetComponent<Image>().color = tabNormalColor;
    }
    
    /// <summary>
    /// 거래 센터 초기화 (TradeCenterUI 활용)
    /// </summary>
    public void ResetTradeCenter()
    {
        selectedBuyItemID = "";
        selectedSellItem = null;
        
        if (tradeCenterUI != null)
        {
            tradeCenterUI.ResetTradeSlots();
        }
        
        if (showDebugLogs)
            Debug.Log("🔄 [ShopUI] 거래 센터 초기화");
    }
    
    /// <summary>
    /// 구매 아이템 설정 (TradeCenterUI 활용)
    /// </summary>
    public void SetBuyItem(string itemID, int price)
    {
        selectedBuyItemID = itemID;
        
        // EquipmentData 로드
        EquipmentData equipment = Resources.Load<EquipmentData>($"Equipment/{itemID}_Equipment");
        
        if (equipment == null)
        {
            Debug.LogError($"❌ [ShopUI] EquipmentData 로드 실패: Equipment/{itemID}_Equipment");
            return;
        }
        
        if (tradeCenterUI != null)
        {
            tradeCenterUI.SetBuyItem(itemID, equipment, price);
        }
        else
        {
            Debug.LogError($"❌ [ShopUI] tradeCenterUI가 null입니다!");
        }
        
        if (showDebugLogs)
            Debug.Log($"💰 [ShopUI] 구매 아이템 설정 완료: {itemID}, 가격: {price}");
    }
    
    /// <summary>
    /// 판매 아이템 설정 (TradeCenterUI 활용)
    /// </summary>
    public void SetSellItem(EquipmentData item, int price)
    {
        selectedSellItem = item;
        
        if (tradeCenterUI != null)
        {
            tradeCenterUI.SetSellItem(item, price);
        }
        else
        {
            Debug.LogError($"❌ [ShopUI] tradeCenterUI가 null입니다!");
        }
        
        if (showDebugLogs)
            Debug.Log($"💰 [ShopUI] 판매 아이템 설정: {item?.equipmentName}, 가격: {price}");
    }
    
    /// <summary>
    /// 거래 완료 후 UI 업데이트
    /// </summary>
    public void OnTransactionCompleted()
    {
        if (tradeCenterUI != null)
        {
            tradeCenterUI.OnTransactionCompleted();
        }
    }
    
    /// <summary>
    /// 거래 완료 팝업 표시
    /// </summary>
    public void ShowTransactionPopup(string message)
    {
        if (transactionPopup != null && popupMessageText != null)
        {
            popupMessageText.text = message;
            transactionPopup.SetActive(true);
            
            // 3초 후 자동 닫기
            Invoke(nameof(HideTransactionPopup), 3f);
        }
        
        if (showDebugLogs)
            Debug.Log($"💬 [ShopUI] 팝업 표시: {message}");
    }
    
    /// <summary>
    /// 거래 완료 팝업 숨기기
    /// </summary>
    private void HideTransactionPopup()
    {
        if (transactionPopup != null)
            transactionPopup.SetActive(false);
    }
    
    /// <summary>
    /// 상점 패널 활성화/비활성화
    /// </summary>
    public void SetShopPanelActive(bool isActive)
    {
        if (shopMainPanel != null)
        {
            shopMainPanel.SetActive(isActive);
            
            if (isActive)
            {
                // 상점 열릴 때 초기화
                ResetTradeCenter();
                SwitchTab(EquipmentType.Weapon);
                HideTransactionPopup();
            }
        }
    }
    
    // 현재 선택된 아이템들 접근자
    public string SelectedBuyItemID => selectedBuyItemID;
    public EquipmentData SelectedSellItem => selectedSellItem;
    public EquipmentType CurrentTab => currentTab;

    /// <summary>
    /// 상점 아이템 진열 업데이트 (최적화)
    /// </summary>
    public void UpdateShopDisplay(EquipmentType equipmentType)
    {
        // 슬롯 재사용으로 최적화
        EnsureShopSlotsExist(equipmentType);
        
        if (ShopInventoryManager.Instance != null)
        {
            // 3x4 그리드 데이터 가져오기
            EquipmentData[,] displayGrid = ShopInventoryManager.Instance.GetDisplayGrid(equipmentType);
            
            // 기존 슬롯들에 데이터만 업데이트
            int slotIndex = 0;
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (slotIndex < tabShopSlots[equipmentType].Count)
                    {
                        var slot = tabShopSlots[equipmentType][slotIndex];
                        if (slot != null)
                        {
                            slot.SetEquipmentData(displayGrid[col, row]);
                        }
                    }
                    slotIndex++;
                }
            }
        }
    }

    /// <summary>
    /// 필요한 슬롯들이 존재하는지 확인하고 생성
    /// </summary>
    private void EnsureShopSlotsExist(EquipmentType equipmentType)
    {
        if (!tabShopSlots.ContainsKey(equipmentType))
        {
            tabShopSlots[equipmentType] = new List<ShopItemSlot>();
        }
        
        Transform currentContainer = GetCurrentTabContainer();
        int requiredSlots = 12; // 3x4 그리드
        
        // 부족한 슬롯만 생성
        while (tabShopSlots[equipmentType].Count < requiredSlots)
        {
            if (shopItemSlotPrefab != null && currentContainer != null)
            {
                GameObject slotObj = Instantiate(shopItemSlotPrefab, currentContainer);
                ShopItemSlot slot = slotObj.GetComponent<ShopItemSlot>();
                
                if (slot != null)
                {
                    // 이벤트는 한 번만 연결
                    slot.OnItemClicked += (itemID) => OnShopItemClicked?.Invoke(itemID);
                    tabShopSlots[equipmentType].Add(slot);
                }
            }
            else
            {
                Debug.LogError($"❌ [ShopUI] 슬롯 생성 실패: Prefab={shopItemSlotPrefab != null}, Container={currentContainer != null}");
                break; // 무한 루프 방지
            }
        }
    }
    
    /// <summary>
    /// 현재 탭에 맞는 Container 반환
    /// </summary>
    private Transform GetCurrentTabContainer()
    {
        switch (currentTab)
        {
            case EquipmentType.Weapon:
                return weaponItemContainer;
            case EquipmentType.Armor:
                return armorItemContainer;
            case EquipmentType.Accessory:
                return bootsItemContainer;
            default:
                return weaponItemContainer;
        }
    }
    
    /// <summary>
    /// 슬롯 정리 최적화 (파괴 대신 데이터만 초기화)
    /// </summary>
    private void ClearShopSlots()
    {
        if (tabShopSlots.ContainsKey(currentTab))
        {
            foreach (var slot in tabShopSlots[currentTab])
            {
                if (slot != null)
                {
                    // 파괴 대신 데이터만 초기화
                    slot.SetEquipmentData(null);
                }
            }
        }
    }
    
    /// <summary>
    /// 아이템 상세 정보 표시 (DetailPanel 활용)
    /// </summary>
    public void ShowItemDetail(EquipmentData equipment)
    {
        if (itemDetailPanel != null && equipment != null)
        {
            if (showDebugLogs)
                Debug.Log($"📋 [ShopUI] 아이템 상세 정보 표시: {equipment.equipmentName}");
        }
    }
    
    /// <summary>
    /// 상세 패널 닫기
    /// </summary>
    public void HideItemDetail()
    {
        // 추후 구현 예정
    }

    /// <summary>
    /// 🆕 모든 탭의 슬롯을 미리 생성 (상점 열기 시 한 번만 실행)
    /// </summary>
    public void InitializeAllTabSlots()
    {
        if (showDebugLogs)
            Debug.Log("🔄 [ShopUI] 모든 탭 슬롯 미리 생성 시작");
        
        // 현재 탭 백업
        EquipmentType originalTab = currentTab;
        
        // 각 탭별로 슬롯 생성
        EquipmentType[] allTabs = { EquipmentType.Weapon, EquipmentType.Armor, EquipmentType.Accessory };
        
        foreach (EquipmentType tabType in allTabs)
        {
            // 임시로 탭 변경 (Container 참조를 위해)
            currentTab = tabType;
            
            // 해당 탭의 슬롯들 생성
            EnsureShopSlotsExist(tabType);
            
            if (showDebugLogs)
                Debug.Log($"✅ [ShopUI] {tabType} 탭 슬롯 생성 완료: {tabShopSlots[tabType].Count}개");
        }
        
        // 원래 탭으로 복원
        currentTab = originalTab;
        
        if (showDebugLogs)
            Debug.Log("✅ [ShopUI] 모든 탭 슬롯 미리 생성 완료");
    }
}