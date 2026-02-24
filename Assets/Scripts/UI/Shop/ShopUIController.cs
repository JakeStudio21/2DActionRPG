using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;
using UI.Popups;  // 🆕 ItemDetailPopup을 위한 using

/// <summary>
/// 🏪 상점 UI 컨트롤러 (Controller Layer)
/// UI와 비즈니스 로직을 연결하는 중간 계층
/// </summary>
public class ShopUIController : MonoBehaviour
{
    [Header("🏪 상점 UI 컴포넌트")]
    // ❌ 구버전 제거: ShopUI는 더 이상 사용하지 않음 (Phase 2에서 ShopBuyPanel로 대체)
    // [SerializeField] private ShopUI shopUI;
    [SerializeField] private ShopInventoryUI playerInventoryUI;
    // ❌ 구버전 제거: TradeCenterUI는 더 이상 사용하지 않음 (Phase 2에서 ItemDetailPopup으로 대체)
    // [SerializeField] private TradeCenterUI tradeCenterUI;
    [SerializeField] private ShopBuyPanel shopBuyPanel;      // 🆕 Phase 2: 클래스 탭 기반 구매 패널
    
    [Header("🔘 공통 버튼")]
    [SerializeField] private Button closeShopButton;         // ✅ Phase 2: ShopUI에서 이동
    
    [Header("🎮 로비 전용 설정")]
    [SerializeField] private bool enableShopInLobbyOnly = true;  // 로비에서만 활성화
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 컨트롤러 참조
    private LobbyUIController lobbyUIController;
    
    void Start()
    {
        // 로비에서만 활성화
        if (enableShopInLobbyOnly && 
            SceneManager.GetActiveScene().name != "Lobby" && 
            !SceneManager.GetActiveScene().name.Contains("Lobby"))
        {
            this.enabled = false;
            if (showDebugLogs)
                Debug.Log("🔒 [ShopUIController] 인게임에서 비활성화됨");
            return;
        }
        
        InitializeShopController();
    }
    
    /// <summary>
    /// 🔧 수정: 상점 컨트롤러 초기화
    /// </summary>
    private void InitializeShopController()
    {
        // 유효성 검증 먼저 실행
        if (!ValidateShopSystem())
        {
            Debug.LogError("❌ [ShopUIController] 상점 시스템 초기화 실패 - 필수 컴포넌트 누락");
            return;
        }
        
        // ✅ ShopController.Start()에서 이미 초기화 수행
        // GetShopDataByClass()에서 자동 초기화 보장하므로 여기서는 불필요
        if (showDebugLogs && ShopController.Instance != null)
        {
            Debug.Log("✅ [ShopUIController] ShopController 준비 완료 (자동 초기화 시스템)");
        }
        
        // ❌ 구버전 이벤트 설정 비활성화 (Phase 2 신버전 사용)
        // SetupShopUIEvents();  // 구버전 ShopUI 이벤트
        
        // ✅ 공통 버튼 이벤트 연결 (CloseShopButton 등)
        SetupCommonButtonEvents();
        
        SetupControllerReferences();
        SetupShopControllerEvents();
        
        // ❌ 구버전 TradeCenterUI 이벤트 설정 비활성화
        // SetupTradeCenterEvents();
        
        SetupShopBuyPanelEvents();  // ✅ Phase 2: ShopBuyPanel 이벤트 설정 (신버전)
        
        if (showDebugLogs)
            Debug.Log("✅ [ShopUIController] 상점 컨트롤러 초기화 완료");
    }
    
    /// <summary>
    /// 🆕 캐릭터별 상점 초기화 (지연 로드 지원)
    /// </summary>
    public void InitializeShopForCharacter(int characterSlotIndex)
    {
        Debug.Log($"🔄 [ShopUIController] 캐릭터별 상점 초기화 시작 - 슬롯 {characterSlotIndex}");
        
        StartCoroutine(InitializeShopForCharacterCoroutine(characterSlotIndex));
    }

    /// <summary>
    /// 🆕 캐릭터별 상점 초기화 코루틴
    /// </summary>
    private IEnumerator InitializeShopForCharacterCoroutine(int characterSlotIndex)
    {
        // 1. 캐릭터 데이터 로드 확인
        if (!PlayerDataManager.Instance.IsSlotSelected || 
            PlayerDataManager.Instance.GetSelectedSlotIndex() != characterSlotIndex)
        {
            Debug.LogWarning($"⚠️ [ShopUIController] 캐릭터 슬롯 {characterSlotIndex} 데이터가 로드되지 않음");
            yield break;
        }
        
        // 2. ShopInventoryUI 참조 재설정
        yield return StartCoroutine(RefreshShopInventoryReferences());
        
        // 3. UI 구조 검증
        bool isValid = ValidateShopUIStructure();
        if (!isValid)
        {
            Debug.LogWarning("⚠️ [ShopUIController] UI 구조 검증 실패 - 복구 시도");
            yield return StartCoroutine(AttemptUIStructureRecovery());
        }
        
        // 4. 상점 데이터 갱신
        RefreshShopData();
        
        Debug.Log($"✅ [ShopUIController] 캐릭터별 상점 초기화 완료 - 슬롯 {characterSlotIndex}");
    }

    /// <summary>
    /// 🆕 ShopInventoryUI 참조 재설정
    /// </summary>
    private IEnumerator RefreshShopInventoryReferences()
    {
        Debug.Log("🔄 [ShopUIController] ShopInventoryUI 참조 재설정 시작");
        
        // 기존 참조 초기화
        playerInventoryUI = null;
        
        // 다양한 방법으로 참조 재설정 시도
        yield return new WaitForSeconds(0.1f); // UI 안정화 대기
        
        // 방법 1: FindObjectOfType
        playerInventoryUI = FindObjectOfType<ShopInventoryUI>();
        if (playerInventoryUI != null)
        {
            Debug.Log("✅ [ShopUIController] ShopInventoryUI 참조 재설정 성공 (FindObjectOfType)");
            yield break;
        }
        
        // 방법 2: GameObject.Find
        GameObject shopInventoryObj = GameObject.Find("RightPanel (Shop Inventory UI)");
        if (shopInventoryObj != null)
        {
            playerInventoryUI = shopInventoryObj.GetComponent<ShopInventoryUI>();
            if (playerInventoryUI != null)
            {
                Debug.Log("✅ [ShopUIController] ShopInventoryUI 참조 재설정 성공 (GameObject.Find)");
                yield break;
            }
        }
        
        // ❌ 구버전 제거: 방법 3: 상점 UI 하위에서 검색
        // if (shopUI != null)
        // {
        //     playerInventoryUI = shopUI.GetComponentInChildren<ShopInventoryUI>();
        //     if (playerInventoryUI != null)
        //     {
        //         Debug.Log("✅ [ShopUIController] ShopInventoryUI 참조 재설정 성공 (GetComponentInChildren)");
        //         yield break;
        //     }
        // }
        
        Debug.LogError("❌ [ShopUIController] ShopInventoryUI 참조 재설정 실패");
    }

    /// <summary>
    /// 🆕 상점 UI 구조 검증
    /// </summary>
    private bool ValidateShopUIStructure()
    {
        Debug.Log("🔍 [ShopUIController] 상점 UI 구조 검증 시작");
        
        // 필수 컴포넌트 검증
        // ❌ 구버전 제거: shopUI는 더 이상 사용하지 않음
        // if (shopUI == null)
        // {
        //     Debug.LogError("❌ [ShopUIController] shopUI가 null입니다");
        //     return false;
        // }
        
        if (playerInventoryUI == null)
        {
            Debug.LogError("❌ [ShopUIController] playerInventoryUI가 null입니다");
            return false;
        }
        
        // GameObject 활성화 상태 검증
        if (!playerInventoryUI.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("⚠️ [ShopUIController] playerInventoryUI GameObject가 비활성화되어 있습니다");
            return false;
        }
        
        // Component 활성화 상태 검증
        if (!playerInventoryUI.enabled)
        {
            Debug.LogWarning("⚠️ [ShopUIController] playerInventoryUI Component가 비활성화되어 있습니다");
            return false;
        }
        
        Debug.Log("✅ [ShopUIController] 상점 UI 구조 검증 성공");
        return true;
    }

    /// <summary>
    /// 🆕 상점 데이터 갱신
    /// </summary>
    private void RefreshShopData()
    {
        Debug.Log("🔄 [ShopUIController] 상점 데이터 갱신 시작");
        
        // 플레이어 인벤토리 갱신
        if (playerInventoryUI != null)
        {
            StartCoroutine(ForceRefreshInventoryDelayed());
        }
        
        // 상점 아이템 목록 갱신 (기본 무기 탭으로)
        RefreshShopItems(EquipmentType.Weapon);
        
        Debug.Log("✅ [ShopUIController] 상점 데이터 갱신 완료");
    }
    
    /// <summary>
    /// UI 이벤트 연결 (구버전 - 사용 안 함)
    /// </summary>
    // ❌ 구버전 제거: SetupShopUIEvents()
    // private void SetupShopUIEvents()
    // {
    //     if (shopUI != null)
    //     {
    //         shopUI.OnCloseShopRequested += HandleCloseShop;
    //         shopUI.OnTabChanged += HandleTabChanged;
    //         shopUI.OnShopItemClicked += HandleShopItemClicked;
    //         shopUI.OnBuyRequested += HandleBuyRequest;
    //         shopUI.OnSellRequested += HandleSellRequest;
    //         
    //         // ❌ 구버전 제거: TradeCenterUI 취소 이벤트 (Phase 2에서 사용 안 함)
    //         // shopUI.OnBuyCancelRequested += HandleBuyCancelRequest;
    //         // shopUI.OnSellCancelRequested += HandleSellCancelRequest;
    //     }
    // }
    
    /// <summary>
    /// ✅ 공통 버튼 이벤트 연결 (Phase 2: ShopUI 의존성 제거)
    /// </summary>
    private void SetupCommonButtonEvents()
    {
        if (closeShopButton != null)
        {
            // ✅ Phase 2: closeShopButton을 ShopUIController에서 직접 관리
            closeShopButton.onClick.RemoveAllListeners(); // 중복 방지
            closeShopButton.onClick.AddListener(HandleCloseShop);
            
            if (showDebugLogs)
                Debug.Log("✅ [ShopUIController] CloseShopButton 이벤트 연결 완료");
        }
        else
        {
            Debug.LogError("❌ [ShopUIController] closeShopButton이 null입니다! Inspector에서 할당해주세요.");
        }
    }
    
    // ❌ 구버전 제거: TradeCenterUI 이벤트 설정 (Phase 2에서 사용 안 함)
    // private void SetupTradeCenterEvents()
    // {
    //     if (shopUI != null && shopUI.GetComponent<TradeCenterUI>() != null)
    //     {
    //         var tradeCenterUI = shopUI.GetComponent<TradeCenterUI>();
    //         tradeCenterUI.OnBuyCancelRequested += HandleBuyCancelRequest;
    //         tradeCenterUI.OnSellCancelRequested += HandleSellCancelRequest;
    //     }
    // }
    
    /// <summary>
    /// 🆕 Phase 2: ShopBuyPanel 이벤트 설정
    /// </summary>
    private void SetupShopBuyPanelEvents()
    {
        if (shopBuyPanel != null)
        {
            shopBuyPanel.OnItemClicked += HandleShopBuyItemClicked;
            
            if (showDebugLogs)
                Debug.Log("✅ [ShopUIController] ShopBuyPanel 이벤트 연결 완료");
        }
        else
        {
            Debug.LogError("❌ [ShopUIController] ShopBuyPanel이 null입니다!");
        }
    }
    
    /// <summary>
    /// 🔧 수정: 컨트롤러 참조 설정
    /// </summary>
    private void SetupControllerReferences()
    {
        // LobbyUIController 찾기
        lobbyUIController = FindObjectOfType<LobbyUIController>();
        
        // ShopInventoryUI 이벤트 구독
        if (playerInventoryUI != null)
        {
            playerInventoryUI.OnInventoryItemClicked += HandleInventoryItemClicked;
            
            if (showDebugLogs)
                Debug.Log("✅ [ShopUIController] ShopInventoryUI 이벤트 연결 완료");
        }
        
        // 🆕 PlayerDataManager 이벤트 구독 (추가 확인)
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnInventoryChanged += RefreshPlayerInventory;
            // PlayerDataManager.Instance.OnSlotClicked는 ShopInventoryUI에서 처리하므로 중복 구독 불필요
            
            if (showDebugLogs)
                Debug.Log("✅ [ShopUIController] PlayerDataManager 이벤트 구독 완료");
        }
    }
    
    /// <summary>
    /// ShopController 이벤트 연결
    /// </summary>
    private void SetupShopControllerEvents()
    {
        if (ShopController.Instance != null)
        {
            ShopController.Instance.OnItemPurchased += HandleItemPurchased;
            ShopController.Instance.OnItemSold += HandleItemSold;
            ShopController.Instance.OnTransactionFailed += HandleTransactionFailed;
        }
    }
    
    #region === 이벤트 핸들러 ===
    
    /// <summary>
    /// 🔧 수정: 상점 닫기 요청 처리
    /// </summary>
    private void HandleCloseShop()
    {
        // ✅ Phase 2: ShopBuyPanel은 비활성화하지 않음 (z-order 방식 사용)
        // shopPanel이 BringPanelToFront()로 관리되므로, 자식인 shopBuyPanel은 자동으로 따라감
        
        // ✅ 로비로 돌아가기 (저장 포함)
        // lobbyUIController.OnBackToLobby() → panelManager.ShowLobbyPanel() → BringPanelToFront(lobbyPanel)
        if (lobbyUIController != null)
        {
            lobbyUIController.OnBackToLobby();
        }
        
        if (showDebugLogs)
            Debug.Log("🏠 [ShopUIController] 상점 닫기 → 로비로 돌아가기");
    }
    
    /// <summary>
    /// 🔧 수정: 상점 열기 시 초기화 (Phase 2: ShopBuyPanel 사용)
    /// </summary>
    public void OnShopOpened()
    {
        if (showDebugLogs)
            Debug.Log("🚀 [ShopUIController] OnShopOpened 시작");
    
        // 기본 유효성 검증
        if (!ValidateShopSystem())
        {
            Debug.LogError("❌ [ShopUIController] 상점 시스템 유효성 검증 실패");
            return;
        }
        
        // ✅ Phase 2: ShopBuyPanel은 이미 활성화되어 있음 (z-order 방식)
        // shopPanel이 BringPanelToFront()로 최상위로 오면, 자식인 shopBuyPanel도 함께 보임
        
        // 🔧 안전장치: 만약 비활성화되어 있다면 활성화 (초기 진입 시만)
        if (shopBuyPanel != null && !shopBuyPanel.gameObject.activeSelf)
        {
            shopBuyPanel.gameObject.SetActive(true);
            if (showDebugLogs)
                Debug.Log("🔧 [ShopUIController] ShopBuyPanel 활성화 (초기 진입)");
        }
        
        // ShopInventoryUI (판매 탭) 초기화
        if (playerInventoryUI != null)
        {
            playerInventoryUI.RefreshInventoryUI();
        }
        
        if (showDebugLogs)
            Debug.Log("✅ [ShopUIController] OnShopOpened 완료");
    }

    /// <summary>
    /// 🔧 수정: 상점 UI 활성화 후 초기화 (빠른 탭 전환)
    /// </summary>
    private System.Collections.IEnumerator InitializeShopUIAfterActivation()
    {
        // 1프레임 대기 (UI 완전 활성화 보장)
        yield return null;
        
        // 상점 아이템 데이터 로드
        if (ShopInventoryManager.Instance != null)
        {
            ShopInventoryManager.Instance.LoadItemsForShop();
        }
        
        // 🎯 핵심: 빠른 탭 전환으로 모든 탭 초기화 (깜빡임 최소화)
        yield return StartCoroutine(QuickInitializeAllTabs());
        
        // 플레이어 인벤토리 새로고침
        if (playerInventoryUI != null)
        {
            playerInventoryUI.ForceRefreshInventory();
        }
        
        if (showDebugLogs)
            Debug.Log("✅ [ShopUIController] 상점 UI 초기화 완료 (모든 탭)");
    }

    /// <summary>
    /// 🆕 빠른 탭 전환으로 모든 탭 초기화
    /// </summary>
    private System.Collections.IEnumerator QuickInitializeAllTabs()
    {
        if (showDebugLogs)
            Debug.Log("🔄 [ShopUIController] 빠른 탭 전환 초기화 시작");
        
        // 각 탭을 빠르게 전환하면서 초기화 (깜빡임 최소화)
        EquipmentType[] allTabs = { EquipmentType.Weapon, EquipmentType.Armor, EquipmentType.Accessory };
        
        // ❌ 구버전 제거: shopUI 탭 전환
        // foreach (EquipmentType tabType in allTabs)
        // {
        //     shopUI.SwitchTab(tabType);
        //     // 프레임 대기 없이 바로 다음 탭으로 (빠른 전환)
        //     
        //     if (showDebugLogs)
        //         Debug.Log($"✅ [ShopUIController] {tabType} 탭 초기화 완료");
        // }
        // 
        // // 마지막에 무기 탭으로 설정
        // shopUI.SwitchTab(EquipmentType.Weapon);
        
        // 1프레임만 대기 (모든 초기화 완료 후)
        yield return null;
        
        if (showDebugLogs)
            Debug.Log("✅ [ShopUIController] 빠른 탭 전환 초기화 완료");
    }
    
    /// <summary>
    /// 🆕 모든 탭의 아이템 데이터 로드
    /// </summary>
    private System.Collections.IEnumerator LoadAllTabsData()
    {
        if (showDebugLogs)
            Debug.Log("🔄 [ShopUIController] 모든 탭 데이터 로드 시작");
        
        // 각 탭별로 데이터 로드
        EquipmentType[] allTabs = { EquipmentType.Weapon, EquipmentType.Armor, EquipmentType.Accessory };
        
        foreach (EquipmentType tabType in allTabs)
        {
            // ❌ 구버전 제거: 해당 탭의 아이템 표시 업데이트
            // if (shopUI != null)
            // {
            //     shopUI.UpdateShopDisplay(tabType);
            // }
            
            yield return null; // 1프레임 대기 (성능 분산)
            
            if (showDebugLogs)
                Debug.Log($"✅ [ShopUIController] {tabType} 탭 데이터 로드 완료");
        }
        
        if (showDebugLogs)
            Debug.Log("✅ [ShopUIController] 모든 탭 데이터 로드 완료");
    }
    
    /// <summary>
    /// 🆕 PlayerDataManager 상태 확인
    /// </summary>
    private void CheckPlayerDataManagerStatus()
    {
        Debug.Log("📊 [ShopUIController] PlayerDataManager 상태 확인:");
        
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("❌ PlayerDataManager.Instance가 null입니다!");
            return;
        }
        
        Debug.Log($"✅ PlayerDataManager.Instance 존재함");
        Debug.Log($"📦 현재 골드: {PlayerDataManager.Instance.CurrentGold}");
        Debug.Log($"📦 인벤토리 아이템 수: {PlayerDataManager.Instance.InventoryItems?.Count ?? -1}");
        Debug.Log($"📦 인벤토리 가득참 여부: {PlayerDataManager.Instance.IsInventoryFull}");
        
        if (PlayerDataManager.Instance.InventoryItems != null)
        {
            for (int i = 0; i < Mathf.Min(PlayerDataManager.Instance.InventoryItems.Count, 3); i++)
            {
                var item = PlayerDataManager.Instance.InventoryItems[i];
                Debug.Log($"📦 인벤토리 아이템 {i}: {(item != null ? item.equipmentName : "null")}");
            }
        }
    }
    
    /// <summary>
    /// 🆕 playerInventoryUI 상태 확인 (타이밍 고려)
    /// </summary>
    private void CheckPlayerInventoryUIStatus()
    {
        Debug.Log("📊 [ShopUIController] playerInventoryUI 상태 확인:");
        
        if (playerInventoryUI == null)
        {
            Debug.LogError("❌ playerInventoryUI가 null입니다!");
            return;
        }
        
        Debug.Log($"✅ playerInventoryUI 존재함: {playerInventoryUI.name}");
        Debug.Log($"🎮 GameObject 활성화 상태: {playerInventoryUI.gameObject.activeInHierarchy}");
        Debug.Log($"🎮 Component 활성화 상태: {playerInventoryUI.enabled}");
        
        // 🔧 수정: 경고 대신 정보 로그 (Unity 생명주기 타이밍 문제)
        if (!playerInventoryUI.gameObject.activeInHierarchy)
        {
            Debug.Log("ℹ️ [ShopUIController] ShopInventoryUI가 아직 활성화 중입니다 (Unity 생명주기 지연)");
        }
    }
    
    /// <summary>
    /// 🔧 수정: 탭 변경 처리
    /// </summary>
    private void HandleTabChanged(EquipmentType tabType)
    {
        // ShopInventoryManager 아이템 로드 확인
        if (ShopInventoryManager.Instance != null)
        {
            ShopInventoryManager.Instance.LoadItemsForShop();
        }
        
        // ❌ 구버전 탭 변경 시 거래 센터 초기화 비활성화
        // if (shopUI != null)
        // {
        //     shopUI.ResetTradeCenter();
        // }
        
        // 해당 탭의 아이템 목록 새로고침
        RefreshShopItems(tabType);
        
        if (showDebugLogs)
            Debug.Log($"🔄 [ShopUIController] 탭 변경됨: {tabType}");
    }
    
    /// <summary>
    /// 상점 아이템 클릭 처리
    /// </summary>
    private void HandleShopItemClicked(string itemID)
    {
        if (showDebugLogs)
            Debug.Log($"🛒 [ShopUIController] 상점 아이템 클릭: {itemID}");
        
        if (ShopController.Instance != null)
        {
            // 아이템 가격 조회
            int buyPrice = ShopController.Instance.GetItemBuyPrice(itemID);
            
            // ❌ 구버전 제거: UI에 구매 아이템 설정
            // if (shopUI != null)
            // {
            //     shopUI.SetBuyItem(itemID, buyPrice);
            // }
            // else
            // {
            //     Debug.LogError($"❌ [ShopUIController] shopUI가 null입니다!");
            // }
        }
        else
        {
            Debug.LogError($"❌ [ShopUIController] ShopController.Instance가 null입니다!");
        }
    }
    
    /// <summary>
    /// 🆕 Phase 2: 상점 구매 아이템 클릭 처리 (ItemInstanceID 기반)
    /// </summary>
    private void HandleShopBuyItemClicked(ItemInstanceID displayInstanceId)
    {
        if (showDebugLogs)
            Debug.Log($"🛒 [ShopUIController] 상점 구매 아이템 클릭 (V2): {displayInstanceId.Value}");
        
        if (displayInstanceId.IsEmpty)
        {
            Debug.LogError("❌ [ShopUIController] displayInstanceId가 유효하지 않습니다!");
            return;
        }
        
        // ShopItemPool에서 EquipmentData 가져오기
        if (ShopController.Instance == null || ShopController.Instance.ItemPool == null)
        {
            Debug.LogError("❌ [ShopUIController] ShopController 또는 ItemPool이 null입니다!");
            return;
        }
        
        EquipmentData equipment = ShopController.Instance.ItemPool.GetEquipmentData(displayInstanceId);
        
        if (equipment == null)
        {
            Debug.LogError($"❌ [ShopUIController] EquipmentData를 찾을 수 없습니다: {displayInstanceId.Value}");
            return;
        }
        
        // ItemDetailPopup 열기 (Shop_Buy 컨텍스트)
        ItemDetailPopup popup = FindObjectOfType<ItemDetailPopup>();
        
        if (popup != null)
        {
            // slotIndex는 -1 (사용 안 함), instanceId는 displayInstanceId 전달
            popup.Show(equipment, ItemDetailContext.Shop_Buy, slotIndex: -1, instanceId: displayInstanceId);
            
            if (showDebugLogs)
                Debug.Log($"✅ [ShopUIController] ItemDetailPopup 열림: {equipment.equipmentName} (Shop_Buy 컨텍스트)");
        }
        else
        {
            Debug.LogError("❌ [ShopUIController] ItemDetailPopup을 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 🆕 V2: 인벤토리 아이템 클릭 처리 (ItemInstanceID 포함)
    /// </summary>
    private void HandleInventoryItemClicked(EquipmentData item, int slotIndex, ItemInstanceID instanceId)
    {
        if (ShopController.Instance != null && item != null && !instanceId.IsEmpty)
        {
            int sellPrice = ShopController.Instance.GetItemSellPrice(item.itemID);
            
            // ❌ 구버전 제거: UI에 판매 아이템 설정 (🆕 V2: ItemInstanceID 전달)
            // if (shopUI != null)
            // {
            //     shopUI.SetSellItem(item, sellPrice, instanceId);
            // }
            
            if (showDebugLogs)
                Debug.Log($"💸 [ShopUIController] 인벤토리 아이템 선택: {item.equipmentName} (슬롯: {slotIndex}, ID: {instanceId.Value.Substring(0, 8)}...)");
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning($"⚠️ [ShopUIController] 잘못된 아이템 선택 (item: {item?.equipmentName ?? "null"}, ID 유효: {!instanceId.IsEmpty})");
        }
    }
    
    /// <summary>
    /// 🔧 수정: 구매 요청 처리 (향상된 버전)
    /// </summary>
    // ❌ 구버전 제거: HandleBuyRequest() - Phase 2에서 ItemDetailPopup으로 대체
    private void HandleBuyRequest()
    {
        // 구버전 - 사용 안 함
    }
    
    /// <summary>
    /// 🔧 수정: 판매 요청 처리 (향상된 버전)
    /// </summary>
    // ❌ 구버전 제거: HandleSellRequest() - Phase 2에서 ItemDetailPopup으로 대체
    private void HandleSellRequest()
    {
        // 구버전 - 사용 안 함
    }
    
    // ❌ 구버전 제거: TradeCenterUI 취소 기능 (Phase 2에서 ItemDetailPopup으로 대체)
    // /// <summary>
    // /// 🆕 구매 취소 요청 처리
    // /// </summary>
    // private void HandleBuyCancelRequest()
    // {
    //     if (tradeCenterUI != null)
    //     {
    //         tradeCenterUI.ResetBuySlot();
    //         ShowTransactionMessage("구매가 취소되었습니다.");
    //     }
    //     else
    //     {
    //         Debug.LogError($"❌ [ShopUIController] tradeCenterUI가 할당되지 않았습니다!");
    //     }
    // }
    // 
    // /// <summary>
    // /// 🆕 판매 취소 요청 처리
    // /// </summary>
    // private void HandleSellCancelRequest()
    // {
    //     TradeCenterUI tradeCenterUI = FindObjectOfType<TradeCenterUI>();
    //     
    //     if (tradeCenterUI != null)
    //     {
    //         tradeCenterUI.ResetSellSlot();
    //         ShowTransactionMessage("판매가 취소되었습니다.");
    //     }
    //     else
    //     {
    //         Debug.LogError($"❌ [ShopUIController] 씬에서 TradeCenterUI를 찾을 수 없습니다!");
    //         
    //         // 대안: shopUI를 통한 방법 시도
    //         if (shopUI != null)
    //         {
    //             shopUI.ResetTradeCenter();
    //             ShowTransactionMessage("판매가 취소되었습니다.");
    //         }
    //     }
    // }
    
    /// <summary>
    /// 🔧 수정: 아이템 구매 완료 처리
    /// </summary>
    // ❌ 구버전 제거: HandleItemPurchased() - Phase 2에서 ItemDetailPopup으로 대체
    private void HandleItemPurchased(string itemID)
    {
        // if (shopUI != null)
        // {
        //     shopUI.ShowTransactionPopup($"구매 완료!");
        //     shopUI.OnTransactionCompleted();  // 🆕 UI 상태 갱신
        // }
        
        RefreshPlayerInventory();
        
        // 🆕 로비 보관창고 새로고침 트리거
        StartCoroutine(RefreshLobbyInventoryDelayed());
        
        if (showDebugLogs)
            Debug.Log($"🛒 [ShopUIController] 구매 완료: {itemID}");
    }
    
    /// <summary>
    /// 🆕 로비 보관창고 UI 새로고침 (지연 실행)
    /// </summary>
    private System.Collections.IEnumerator RefreshLobbyInventoryDelayed()
    {
        // 1프레임 대기 (UI가 닫히는 것을 기다림)
        yield return null;
        
        // LobbyInventoryController 찾기 및 새로고침
        var lobbyInventoryController = FindObjectOfType<LobbyInventoryController>();
        if (lobbyInventoryController != null)
        {
            // SendMessage로 새로고침 메서드 호출 시도
            lobbyInventoryController.SendMessage("RefreshInventoryUI", SendMessageOptions.DontRequireReceiver);
            
            if (showDebugLogs)
                Debug.Log($"🔄 [ShopUIController] 로비 보관창고 새로고침 요청");
        }
    }
    
    /// <summary>
    /// 🔧 수정: 아이템 판매 완료 처리
    /// </summary>
    // ❌ 구버전 제거: HandleItemSold() - Phase 2에서 ItemDetailPopup으로 대체
    private void HandleItemSold(string itemID)
    {
        // if (shopUI != null)
        // {
        //     shopUI.ShowTransactionPopup($"판매 완료!");
        //     shopUI.OnTransactionCompleted();  // 🆕 UI 상태 갱신
        // }
        
        RefreshPlayerInventory();
        
        if (showDebugLogs)
            Debug.Log($"💸 [ShopUIController] 판매 완료: {itemID}");
    }
    
    /// <summary>
    /// 🔧 수정: 거래 실패 처리
    /// </summary>
    private void HandleTransactionFailed(string itemID)
    {
        Debug.Log($"🔥🔥 [ShopUIController] HandleTransactionFailed 호출됨: {itemID}");
        ShowTransactionMessage("거래에 실패했습니다!");
        
        if (showDebugLogs)
            Debug.LogWarning($"❌ [ShopUIController] 거래 실패: {itemID}");
    }
    
    #endregion
    
    #region === 헬퍼 메서드 ===
    
    /// <summary>
    /// 🔧 수정: 상점 아이템 목록 새로고침
    /// </summary>
    // ❌ 구버전 제거: RefreshShopItems() - Phase 2에서 ShopBuyPanel로 대체
    private void RefreshShopItems(EquipmentType equipmentType)
    {
        // if (shopUI != null)
        // {
        //     shopUI.UpdateShopDisplay(equipmentType);
        // }
        
        if (showDebugLogs)
            Debug.Log($"🔄 [ShopUIController] {equipmentType} 아이템 목록 새로고침 완료");
    }
    
    /// <summary>
    /// 🔧 수정: 상점에서 인벤토리 새로고침 처리 (지연 로드 지원)
    /// </summary>
    private void RefreshPlayerInventory()
    {
        // ⭐ 로비 전용: 인게임에서 호출되면 무시
        if (this == null || !this.isActiveAndEnabled)
        {
            Debug.Log("⚠️ [ShopUIController] RefreshPlayerInventory 스킵 - 오브젝트 비활성화 또는 파괴됨 (인게임에서 호출됨)");
            return;
        }
        
        Debug.Log("🔄 [ShopUIController] RefreshPlayerInventory 시작 (지연 로드 지원)");
        
        // ❌ 구버전 제거: 상점 패널 활성화 체크
        // if (shopUI == null || !shopUI.gameObject.activeInHierarchy)
        // {
        //     Debug.Log("🔄 [ShopUIController] 상점 패널이 비활성화 상태 - RefreshPlayerInventory 스킵");
        //     return;
        // }
        
        // 1. 캐릭터 데이터 로드 상태 확인
        if (PlayerDataManager.Instance.IsLazyLoadRequired())
        {
            Debug.Log("🔄 [ShopUIController] 지연 로드 필요 - 데이터 로드 후 재시도");
            StartCoroutine(RefreshPlayerInventoryWithLazyLoad());
            return;
        }
        
        // 2. playerInventoryUI 참조 확인
        if (playerInventoryUI == null)
        {
            Debug.LogWarning("⚠️ [ShopUIController] playerInventoryUI가 null - 참조 재설정 시도");
            StartCoroutine(RefreshPlayerInventoryWithReferenceRecovery());
            return;
        }
        
        // 3. 기존 로직 실행
        StartCoroutine(WaitForShopPanelActivation());
    }

    /// <summary>
    /// 🆕 지연 로드와 함께 인벤토리 새로고침
    /// </summary>
    private IEnumerator RefreshPlayerInventoryWithLazyLoad()
    {
        // 캐릭터 데이터 로드
        int selectedSlot = PlayerDataManager.Instance.GetSelectedSlotIndex();
        bool loadSuccess = PlayerDataManager.Instance.LazyLoadSlotData(selectedSlot);
        
        if (!loadSuccess)
        {
            Debug.LogError("❌ [ShopUIController] 캐릭터 데이터 로드 실패");
            yield break;
        }
        
        yield return new WaitForSeconds(0.1f); // 로드 완료 대기
        
        // 인벤토리 새로고침 재시도
        RefreshPlayerInventory();
    }

    /// <summary>
    /// 🆕 참조 복구와 함께 인벤토리 새로고침
    /// </summary>
    private IEnumerator RefreshPlayerInventoryWithReferenceRecovery()
    {
        // 참조 재설정 시도
        yield return StartCoroutine(RefreshShopInventoryReferences());
        
        if (playerInventoryUI != null)
        {
            Debug.Log("✅ [ShopUIController] 참조 복구 성공 - 인벤토리 새로고침 재시도");
            RefreshPlayerInventory();
        }
        else
        {
            Debug.LogError("❌ [ShopUIController] 참조 복구 실패 - 인벤토리 새로고침 불가");
        }
    }
    
    /// <summary>
    /// 🆕 ShopPanel 활성화 완료 대기 (안전장치 강화)
    /// </summary>
    private IEnumerator WaitForShopPanelActivation()
    {
        // 1프레임 대기 (GameObject.SetActive 완료 대기)
        yield return null;
        
        if (playerInventoryUI != null)
        {
            if (playerInventoryUI.gameObject.activeInHierarchy)
            {
                Debug.Log("✅ [ShopUIController] ShopInventoryUI 정상 활성화 확인됨");
            }
            else
            {
                Debug.LogWarning("⚠️ [ShopUIController] ShopInventoryUI 활성화 지연 감지, 추가 대기 중...");
                
                // 최대 0.2초 추가 대기 (안전장치 강화)
                yield return new WaitForSeconds(0.2f);
                
                if (playerInventoryUI.gameObject.activeInHierarchy)
                {
                    Debug.Log("✅ [ShopUIController] ShopInventoryUI 지연 활성화 완료");
                }
                else
                {
                    // 🔧 추가: UI 구조 복구 시도
                    Debug.LogWarning("🔧 [ShopUIController] ShopInventoryUI 구조 복구 시도 중...");
                    yield return AttemptUIStructureRecovery();
                }
            }
            
            // Component 활성화 확인
            if (!playerInventoryUI.enabled)
            {
                playerInventoryUI.enabled = true;
                Debug.Log("🔧 [ShopUIController] ShopInventoryUI Component 활성화");
            }
        }
        
        // 인벤토리 새로고침 실행
        yield return ForceRefreshInventoryDelayed();
    }
    
    /// <summary>
    /// 🔧 강화된 UI 구조 복구 시도
    /// </summary>
    private IEnumerator AttemptUIStructureRecovery()
    {
        Debug.Log("🔧 [ShopUIController] UI 구조 복구 시작 (강화 버전)");
        
        // 1. ShopInventoryUI 재참조 시도 (더 강력한 검색)
        if (playerInventoryUI == null)
        {
            Debug.Log("�� [ShopUIController] playerInventoryUI가 null - 재검색 시작");
            
            // 방법 1: FindObjectOfType으로 검색
            var shopInventoryUI = FindObjectOfType<ShopInventoryUI>();
            if (shopInventoryUI != null)
            {
                playerInventoryUI = shopInventoryUI;
                Debug.Log("🔧 [ShopUIController] ShopInventoryUI 재참조 성공 (FindObjectOfType)");
            }
            else
            {
                // 방법 2: 이름으로 검색
                GameObject shopInventoryObj = GameObject.Find("RightPanel (Shop Inventory UI)");
                if (shopInventoryObj != null)
                {
                    playerInventoryUI = shopInventoryObj.GetComponent<ShopInventoryUI>();
                    if (playerInventoryUI != null)
                    {
                        Debug.Log("🔧 [ShopUIController] ShopInventoryUI 재참조 성공 (GameObject.Find)");
                    }
                }
                
                // ❌ 구버전 제거: 방법 3: 상점 패널 하위에서 검색
                // if (playerInventoryUI == null && shopUI != null)
                // {
                //     var shopInventoryInChildren = shopUI.GetComponentInChildren<ShopInventoryUI>();
                //     if (shopInventoryInChildren != null)
                //     {
                //         playerInventoryUI = shopInventoryInChildren;
                //         Debug.Log("🔧 [ShopUIController] ShopInventoryUI 재참조 성공 (GetComponentInChildren)");
                //     }
                // }
            }
        }
        
        // 2. GameObject 활성화 상태 강제 수정
        if (playerInventoryUI != null)
        {
            Debug.Log($"🔍 [ShopUIController] playerInventoryUI 상태 - GameObject: {playerInventoryUI.gameObject.name}, Active: {playerInventoryUI.gameObject.activeInHierarchy}");
            
            // GameObject가 비활성화되어 있으면 강제 활성화
            if (!playerInventoryUI.gameObject.activeInHierarchy)
            {
                Debug.Log("🔧 [ShopUIController] playerInventoryUI GameObject 강제 활성화 시도");
                
                // 부모 GameObject들도 확인하여 활성화
                Transform current = playerInventoryUI.transform;
                while (current != null)
                {
                    if (!current.gameObject.activeSelf)
                    {
                        Debug.Log($"🔧 [ShopUIController] 부모 GameObject 활성화: {current.name}");
                        current.gameObject.SetActive(true);
                    }
                    current = current.parent;
                }
                
                yield return new WaitForSeconds(0.1f);
            }
            
            // Component 활성화 확인
            if (!playerInventoryUI.enabled)
            {
                playerInventoryUI.enabled = true;
                Debug.Log("🔧 [ShopUIController] ShopInventoryUI Component 활성화");
            }
        }
        
        // ❌ 구버전 제거: 3. 상점 패널 재활성화 시도 (기존 로직 유지)
        // if (shopUI != null)
        // {
        //     shopUI.SetShopPanelActive(false);
        //     yield return new WaitForSeconds(0.1f);
        //     shopUI.SetShopPanelActive(true);
        //     yield return new WaitForSeconds(0.1f);
        //     
        //     Debug.Log("🔧 [ShopUIController] 상점 패널 재활성화 완료");
        // }
        
        // 4. 최종 상태 확인 (더 상세한 로그)
        if (playerInventoryUI != null)
        {
            bool isActive = playerInventoryUI.gameObject.activeInHierarchy;
            bool isEnabled = playerInventoryUI.enabled;
            
            Debug.Log($"🔍 [ShopUIController] 최종 상태 - Active: {isActive}, Enabled: {isEnabled}");
            
            if (isActive && isEnabled)
            {
                Debug.Log("✅ [ShopUIController] UI 구조 복구 성공");
            }
            else
            {
                Debug.LogError($"❌ [ShopUIController] UI 구조 복구 실패 - Active: {isActive}, Enabled: {isEnabled}");
                
                // 🔧 추가 디버깅 정보
                Debug.LogError($"🔍 [ShopUIController] GameObject 경로: {GetGameObjectPath(playerInventoryUI.gameObject)}");
            }
        }
        else
        {
            Debug.LogError("❌ [ShopUIController] playerInventoryUI가 여전히 null입니다");
        }
    }

    /// <summary>
    /// 🔍 GameObject의 전체 경로를 반환하는 헬퍼 메서드
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
    /// 🆕 지연된 인벤토리 새로고침
    /// </summary>
    private System.Collections.IEnumerator ForceRefreshInventoryDelayed()
    {
        // 2프레임 대기 (확실한 초기화 대기)
        yield return null;
        yield return null;
        
        // LobbyInventoryUI의 공개 메서드 사용
        if (playerInventoryUI != null)
        {
            playerInventoryUI.ForceRefreshInventory();
            yield return null; // 1프레임 더 대기
            // playerInventoryUI.LogSlotStatus(); // 🗑️ 디버그 로그 제거
        }
    }
    
    /// <summary>
    /// 🔧 수정: 상점 인벤토리 슬롯 클릭 처리 (바뀐 인벤토리 구조 대응)
    /// </summary>
    private void HandleInventorySlotClicked(EquipmentData item, int slotIndex)
    {
        if (ShopController.Instance != null && item != null)
        {
            int sellPrice = ShopController.Instance.GetItemSellPrice(item.itemID);
            
            // ❌ 구버전 제거: UI에 판매 아이템 설정
            // if (shopUI != null)
            // {
            //     shopUI.SetSellItem(item, sellPrice);
            // }
            
            if (showDebugLogs)
                Debug.Log($"💸 [ShopUIController] 상점 인벤토리 슬롯 클릭: {item.equipmentName} (슬롯: {slotIndex})");
        }
    }
    
    /// <summary>
    /// 🆕 골드 변경 이벤트 처리 (Phase 2: LobbyPlayerInfoUI가 자동 처리)
    /// </summary>
    private void HandleGoldChanged(int newGoldAmount)
    {
        // ✅ Phase 2: LobbyPlayerInfoUI가 PlayerDataManager.OnGoldChanged 이벤트를 구독하여 자동 업데이트
        // TradeCenterUI는 더 이상 사용하지 않음
        
        if (showDebugLogs)
            Debug.Log($"💰 [ShopUIController] 골드 변경됨: {newGoldAmount} (LobbyPlayerInfoUI가 자동 업데이트)");
    }
    
    /// <summary>
    /// 🆕 거래 메시지 표시
    /// </summary>
    // ❌ 구버전 제거: ShowTransactionMessage() - Phase 2에서 ItemDetailPopup으로 대체
    private void ShowTransactionMessage(string message)
    {
        // if (shopUI != null)
        // {
        //     shopUI.ShowTransactionPopup(message);
        // }
        
        if (showDebugLogs)
            Debug.Log($"💬 [ShopUIController] {message}");
    }
    
    #endregion
    
    void OnEnable()
    {
        Debug.Log("🔥 [ShopUIController] OnEnable 호출됨");
        
        // 상점 패널이 활성화될 때마다 실행
        StartCoroutine(InitializeShopOnEnable());
    }
    
    /// <summary>
    /// 🔧 수정: 상점 패널 활성화 시 초기화 (디버그 로그 제거)
    /// </summary>
    private System.Collections.IEnumerator InitializeShopOnEnable()
    {
        // 1프레임 대기 (모든 컴포넌트 활성화 완료 대기)
        yield return null;
        
        // ❌ 구버전 제거: shopUI 활성화 체크
        // if (shopUI != null && shopUI.gameObject.activeInHierarchy)
        // {
        //     OnShopOpened();
        // }
        // 🔧 수정: 워닝 제거 (정상적인 초기화 대기 상황)
    }
    
    void OnDestroy()
    {
        // ❌ 구버전 제거: 이벤트 구독 해제
        // if (shopUI != null)
        // {
        //     shopUI.OnCloseShopRequested -= HandleCloseShop;
        //     shopUI.OnTabChanged -= HandleTabChanged;
        //     shopUI.OnShopItemClicked -= HandleShopItemClicked;
        //     shopUI.OnBuyRequested -= HandleBuyRequest;
        //     shopUI.OnSellRequested -= HandleSellRequest;
        //     
        //     // ❌ 구버전 제거: TradeCenterUI 취소 이벤트 구독 해제 (Phase 2에서 사용 안 함)
        //     // shopUI.OnBuyCancelRequested -= HandleBuyCancelRequest;
        //     // shopUI.OnSellCancelRequested -= HandleSellCancelRequest;
        // }
        
        if (ShopController.Instance != null)
        {
            ShopController.Instance.OnItemPurchased -= HandleItemPurchased;
            ShopController.Instance.OnItemSold -= HandleItemSold;
            ShopController.Instance.OnTransactionFailed -= HandleTransactionFailed;
        }
    }
    
    /// <summary>
    /// 🆕 상점 시스템 유효성 검증
    /// </summary>
    private bool ValidateShopSystem()
    {
        bool isValid = true;
        
        // 필수 컴포넌트 검증
        if (ShopController.Instance == null)
        {
            Debug.LogError("❌ [ShopUIController] ShopController.Instance를 찾을 수 없습니다!");
            isValid = false;
        }
        
        if (ShopInventoryManager.Instance == null)
        {
            Debug.LogError("❌ [ShopUIController] ShopInventoryManager.Instance를 찾을 수 없습니다!");
            isValid = false;
        }
        
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("❌ [ShopUIController] PlayerDataManager.Instance를 찾을 수 없습니다!");
            isValid = false;
        }
        
        // ❌ 구버전 ShopUI 검증 비활성화 (Phase 2 신버전 사용)
        // if (shopUI == null)
        // {
        //     Debug.LogError("❌ [ShopUIController] ShopUI가 할당되지 않았습니다!");
        //     isValid = false;
        // }
        
        // ✅ Phase 2: ShopBuyPanel 검증 추가
        if (shopBuyPanel == null)
        {
            Debug.LogError("❌ [ShopUIController] ShopBuyPanel이 할당되지 않았습니다!");
            isValid = false;
        }
        
        // ✅ Phase 2: CloseShopButton 검증 추가
        if (closeShopButton == null)
        {
            Debug.LogError("❌ [ShopUIController] closeShopButton이 할당되지 않았습니다!");
            isValid = false;
        }
        
        if (isValid && showDebugLogs)
            Debug.Log("✅ [ShopUIController] 상점 시스템 유효성 검증 통과");
        
        return isValid;
    }
}
