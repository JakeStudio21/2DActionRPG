using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;

/// <summary>
/// 🏪 상점 UI 컨트롤러 (Controller Layer)
/// UI와 비즈니스 로직을 연결하는 중간 계층
/// </summary>
public class ShopUIController : MonoBehaviour
{
    [Header("🏪 상점 UI 컴포넌트")]
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private ShopInventoryUI playerInventoryUI;
    [SerializeField] private TradeCenterUI tradeCenterUI;    // 🆕 직접 참조 추가
    
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
        
        SetupShopUIEvents();
        SetupControllerReferences();
        SetupShopControllerEvents();
        SetupTradeCenterEvents();
        
        if (showDebugLogs)
            Debug.Log("✅ [ShopUIController] 상점 컨트롤러 초기화 완료");
    }
    
    /// <summary>
    /// UI 이벤트 연결
    /// </summary>
    private void SetupShopUIEvents()
    {
        if (shopUI != null)
        {
            shopUI.OnCloseShopRequested += HandleCloseShop;
            shopUI.OnTabChanged += HandleTabChanged;
            shopUI.OnShopItemClicked += HandleShopItemClicked;
            shopUI.OnBuyRequested += HandleBuyRequest;
            shopUI.OnSellRequested += HandleSellRequest;
            
            // 🆕 개별 취소 이벤트 구독
            shopUI.OnBuyCancelRequested += HandleBuyCancelRequest;
            shopUI.OnSellCancelRequested += HandleSellCancelRequest;
        }
    }
    
    private void SetupTradeCenterEvents()
    {
        if (shopUI != null && shopUI.GetComponent<TradeCenterUI>() != null)
        {
            var tradeCenterUI = shopUI.GetComponent<TradeCenterUI>();
            tradeCenterUI.OnBuyCancelRequested += HandleBuyCancelRequest;
            tradeCenterUI.OnSellCancelRequested += HandleSellCancelRequest;
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
            playerInventoryUI.OnInventoryItemClicked += HandleInventorySlotClicked;
            
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
        // 거래 센터 초기화
        if (shopUI != null)
        {
            shopUI.ResetTradeCenter();
        }
        
        // 로비로 돌아가기
        if (lobbyUIController != null)
        {
            lobbyUIController.ShowLobbyPanel();
        }
        
        if (showDebugLogs)
            Debug.Log("🏠 [ShopUIController] 상점 닫기 → 로비로 돌아가기");
    }
    
    /// <summary>
    /// 🆕 상점 열기 시 초기화 (디버깅 강화)
    /// </summary>
    public void OnShopOpened()
    {
        Debug.Log("🚀 [ShopUIController] OnShopOpened 시작");
        
        if (shopUI != null)
        {
            Debug.Log("✅ [ShopUIController] shopUI 존재함");
            
            // 상점 패널 활성화
            shopUI.SetShopPanelActive(true);
            
            // 기본 탭으로 설정
            shopUI.SwitchTab(EquipmentType.Weapon);
            
            // 🆕 디버깅: PlayerDataManager 상태 확인
            CheckPlayerDataManagerStatus();
            
            // 🆕 디버깅: playerInventoryUI 상태 확인
            CheckPlayerInventoryUIStatus();
            
            // 🆕 플레이어 인벤토리 UI 강제 새로고침
            RefreshPlayerInventory();
        }
        else
        {
            Debug.LogError("❌ [ShopUIController] shopUI가 null입니다!");
        }
        
        Debug.Log("🏁 [ShopUIController] OnShopOpened 완료");
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
    /// 🆕 playerInventoryUI 상태 확인
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
        
        // LobbyInventoryUI의 슬롯 상태 확인
        if (playerInventoryUI.gameObject.activeInHierarchy)
        {
            // playerInventoryUI.LogSlotStatus(); // 🗑️ 디버그 로그 제거
        }
        else
        {
            Debug.LogWarning("⚠️ playerInventoryUI GameObject가 비활성화되어 있습니다!");
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
        
        // 탭 변경 시 거래 센터 초기화
        if (shopUI != null)
        {
            shopUI.ResetTradeCenter();
        }
        
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
            
            // UI에 구매 아이템 설정
            if (shopUI != null)
            {
                shopUI.SetBuyItem(itemID, buyPrice);
            }
            else
            {
                Debug.LogError($"❌ [ShopUIController] shopUI가 null입니다!");
            }
        }
        else
        {
            Debug.LogError($"❌ [ShopUIController] ShopController.Instance가 null입니다!");
        }
    }
    
    /// <summary>
    /// 인벤토리 아이템 클릭 처리 (기존 LobbyInventoryUI 연동)
    /// </summary>
    private void HandleInventoryItemClicked(EquipmentData item)
    {
        if (ShopController.Instance != null && item != null)
        {
            int sellPrice = ShopController.Instance.GetItemSellPrice(item.itemID);
            
            // UI에 판매 아이템 설정
            if (shopUI != null)
            {
                shopUI.SetSellItem(item, sellPrice);
            }
            
            if (showDebugLogs)
                Debug.Log($"💸 [ShopUIController] 인벤토리 아이템 선택: {item.equipmentName}");
        }
    }
    
    /// <summary>
    /// 🔧 수정: 구매 요청 처리 (향상된 버전)
    /// </summary>
    private void HandleBuyRequest()
    {
        if (shopUI != null && !string.IsNullOrEmpty(shopUI.SelectedBuyItemID))
        {
            string itemID = shopUI.SelectedBuyItemID;
            
            if (ShopController.Instance != null)
            {
                // 골드 및 인벤토리 공간 사전 체크
                int buyPrice = ShopController.Instance.GetItemBuyPrice(itemID);
                
                if (PlayerDataManager.Instance.CurrentGold < buyPrice)
                {
                    ShowTransactionMessage($"골드가 부족합니다! (필요: {buyPrice}, 보유: {PlayerDataManager.Instance.CurrentGold})");
                    return;
                }
                
                if (PlayerDataManager.Instance.IsInventoryFull)
                {
                    ShowTransactionMessage("인벤토리가 가득 참!");
                    return;
                }
                
                // 구매 실행
                ShopController.Instance.TryPurchaseItem(itemID);
            }
            else
            {
                Debug.LogError($"❌ [ShopUIController] ShopController.Instance가 null입니다!");
            }
        }
        else
        {
            ShowTransactionMessage("구매할 아이템을 선택해주세요!");
        }
    }
    
    /// <summary>
    /// 🔧 수정: 판매 요청 처리 (향상된 버전)
    /// </summary>
    private void HandleSellRequest()
    {
        if (shopUI != null && shopUI.SelectedSellItem != null)
        {
            var item = shopUI.SelectedSellItem;
            
            // 판매 가능 여부 체크
            if (!item.isTradable)
            {
                ShowTransactionMessage($"{item.equipmentName}은(는) 판매할 수 없는 아이템입니다!");
                return;
            }
            
            if (ShopController.Instance != null)
            {
                ShopController.Instance.TrySellItem(item);
            }
            else
            {
                Debug.LogError($"❌ [ShopUIController] ShopController.Instance가 null입니다!");
            }
        }
        else
        {
            ShowTransactionMessage("판매할 아이템을 선택해주세요!");
        }
    }
    
    /// <summary>
    /// 🆕 구매 취소 요청 처리
    /// </summary>
    private void HandleBuyCancelRequest()
    {
        if (tradeCenterUI != null)
        {
            tradeCenterUI.ResetBuySlot();
            ShowTransactionMessage("구매가 취소되었습니다.");
        }
        else
        {
            Debug.LogError($"❌ [ShopUIController] tradeCenterUI가 할당되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 🆕 판매 취소 요청 처리
    /// </summary>
    private void HandleSellCancelRequest()
    {
        TradeCenterUI tradeCenterUI = FindObjectOfType<TradeCenterUI>();
        
        if (tradeCenterUI != null)
        {
            tradeCenterUI.ResetSellSlot();
            ShowTransactionMessage("판매가 취소되었습니다.");
        }
        else
        {
            Debug.LogError($"❌ [ShopUIController] 씬에서 TradeCenterUI를 찾을 수 없습니다!");
            
            // 대안: shopUI를 통한 방법 시도
            if (shopUI != null)
            {
                shopUI.ResetTradeCenter();
                ShowTransactionMessage("판매가 취소되었습니다.");
            }
        }
    }
    
    /// <summary>
    /// 🔧 수정: 아이템 구매 완료 처리
    /// </summary>
    private void HandleItemPurchased(string itemID)
    {
        if (shopUI != null)
        {
            shopUI.ShowTransactionPopup($"구매 완료!");
            shopUI.OnTransactionCompleted();  // 🆕 UI 상태 갱신
        }
        
        RefreshPlayerInventory();
        
        if (showDebugLogs)
            Debug.Log($"🛒 [ShopUIController] 구매 완료: {itemID}");
    }
    
    /// <summary>
    /// 🔧 수정: 아이템 판매 완료 처리
    /// </summary>
    private void HandleItemSold(string itemID)
    {
        if (shopUI != null)
        {
            shopUI.ShowTransactionPopup($"판매 완료!");
            shopUI.OnTransactionCompleted();  // 🆕 UI 상태 갱신
        }
        
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
    private void RefreshShopItems(EquipmentType equipmentType)
    {
        if (shopUI != null)
        {
            shopUI.UpdateShopDisplay(equipmentType);
        }
        
        if (showDebugLogs)
            Debug.Log($"🔄 [ShopUIController] {equipmentType} 아이템 목록 새로고침 완료");
    }
    
    /// <summary>
    /// 🔧 수정: 상점에서 인벤토리 새로고침 처리
    /// </summary>
    private void RefreshPlayerInventory()
    {
        Debug.Log("🔄 [ShopUIController] RefreshPlayerInventory 시작");
        
        if (playerInventoryUI == null)
        {
            Debug.LogWarning("⚠️ [ShopUIController] playerInventoryUI가 null입니다!");
            return;
        }
        
        // ShopInventoryUI 활성화 확인 및 강제 새로고침
        if (playerInventoryUI.gameObject.activeInHierarchy)
        {
            // 🗑️ 제거: LogSlotStatus는 ShopInventoryUI에 없음
            if (showDebugLogs)
                Debug.Log("✅ [ShopUIController] ShopInventoryUI 활성화 상태 확인됨");
        }
        else
        {
            Debug.LogWarning("⚠️ ShopInventoryUI GameObject가 비활성화되어 있습니다!");
        }
        
        // playerInventoryUI 활성화
        if (!playerInventoryUI.enabled)
        {
            playerInventoryUI.enabled = true;
        }
        
        // 코루틴으로 지연 실행
        StartCoroutine(ForceRefreshInventoryDelayed());
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
            
            // UI에 판매 아이템 설정
            if (shopUI != null)
            {
                shopUI.SetSellItem(item, sellPrice);
            }
            
            if (showDebugLogs)
                Debug.Log($"💸 [ShopUIController] 상점 인벤토리 슬롯 클릭: {item.equipmentName} (슬롯: {slotIndex})");
        }
    }
    
    /// <summary>
    /// 🆕 골드 변경 이벤트 처리
    /// </summary>
    private void HandleGoldChanged(int newGoldAmount)
    {
        // TradeCenterUI의 골드 표시 업데이트
        if (shopUI != null && shopUI.gameObject.activeInHierarchy)
        {
            var tradeCenterUI = shopUI.GetComponentInChildren<TradeCenterUI>();
            if (tradeCenterUI != null)
            {
                tradeCenterUI.UpdatePlayerGoldDisplay();
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"💰 [ShopUIController] 골드 변경됨: {newGoldAmount}");
    }
    
    /// <summary>
    /// 🆕 거래 메시지 표시
    /// </summary>
    private void ShowTransactionMessage(string message)
    {
        if (shopUI != null)
        {
            shopUI.ShowTransactionPopup(message);
        }
        
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
        
        if (shopUI != null && shopUI.gameObject.activeInHierarchy)
        {
            OnShopOpened();
        }
        // 🔧 수정: 워닝 제거 (정상적인 초기화 대기 상황)
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (shopUI != null)
        {
            shopUI.OnCloseShopRequested -= HandleCloseShop;
            shopUI.OnTabChanged -= HandleTabChanged;
            shopUI.OnShopItemClicked -= HandleShopItemClicked;
            shopUI.OnBuyRequested -= HandleBuyRequest;
            shopUI.OnSellRequested -= HandleSellRequest;
            
            // 🆕 개별 취소 이벤트 구독 해제
            shopUI.OnBuyCancelRequested -= HandleBuyCancelRequest;
            shopUI.OnSellCancelRequested -= HandleSellCancelRequest;
        }
        
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
        
        if (shopUI == null)
        {
            Debug.LogError("❌ [ShopUIController] ShopUI가 할당되지 않았습니다!");
            isValid = false;
        }
        
        if (isValid && showDebugLogs)
            Debug.Log("✅ [ShopUIController] 상점 시스템 유효성 검증 통과");
        
        return isValid;
    }
}
