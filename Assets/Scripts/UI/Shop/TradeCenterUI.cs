using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 💰 거래 센터 UI 컴포넌트
/// 구매/판매 아이템 표시 및 거래 버튼 관리
/// </summary>
public class TradeCenterUI : MonoBehaviour
{
    [Header("🛒 구매 슬롯")]
    [SerializeField] private GameObject buySlotContainer;
    [SerializeField] private Image buyItemIcon;
    [SerializeField] private TMP_Text buyItemName;
    [SerializeField] private TMP_Text buyPriceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button buyCancelButton;        // 🆕 구매 취소 버튼
    
    [Header("💸 판매 슬롯")]
    [SerializeField] private GameObject sellSlotContainer;
    [SerializeField] private Image sellItemIcon;
    [SerializeField] private TMP_Text sellItemName;
    [SerializeField] private TMP_Text sellPriceText;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button sellCancelButton;       // 🆕 판매 취소 버튼
    
    [Header("💬 상태 표시")]
    [SerializeField] private TMP_Text playerGoldText;           // 플레이어 골드 표시
    [SerializeField] private TMP_Text transactionStatusText;   // 거래 상태 메시지
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 이벤트
    public event Action OnBuyRequested;
    public event Action OnSellRequested;
    public event Action OnBuyCancelRequested;       // 🆕 구매 취소 이벤트
    public event Action OnSellCancelRequested;      // 🆕 판매 취소 이벤트
    
    // 현재 거래 정보
    private string currentBuyItemID = "";
    private EquipmentData currentSellItem = null;
    private int currentBuyPrice = 0;
    private int currentSellPrice = 0;
    
    void Start()
    {
        InitializeTradeCenterUI();
    }
    
    /// <summary>
    /// 거래 센터 UI 초기화
    /// </summary>
    private void InitializeTradeCenterUI()
    {
        SetupButtonEvents();
        ResetTradeSlots();
        UpdatePlayerGoldDisplay();
        
        if (showDebugLogs)
            Debug.Log("✅ [TradeCenterUI] 거래 센터 초기화 완료");
    }
    
    /// <summary>
    /// 버튼 이벤트 연결
    /// </summary>
    private void SetupButtonEvents()
    {
        buyButton?.onClick.AddListener(() => OnBuyRequested?.Invoke());
        sellButton?.onClick.AddListener(() => OnSellRequested?.Invoke());
        
        // 개별 취소 버튼 이벤트 연결
        buyCancelButton?.onClick.AddListener(() => OnBuyCancelRequested?.Invoke());
        sellCancelButton?.onClick.AddListener(() => OnSellCancelRequested?.Invoke());
    }
    
    /// <summary>
    /// 구매 아이템 설정
    /// </summary>
    public void SetBuyItem(string itemID, EquipmentData equipment, int price)
    {
        if (showDebugLogs)
            Debug.Log($"🛒 [TradeCenterUI] SetBuyItem: {itemID}, 가격: {price}");
        
        currentBuyItemID = itemID;
        currentBuyPrice = price;
        
        if (equipment != null)
        {
            // 구매 슬롯 활성화
            if (buySlotContainer != null)
            {
                buySlotContainer.SetActive(true);
            }
            else
            {
                Debug.LogError($"❌ [TradeCenterUI] buySlotContainer가 null입니다!");
            }
            
            // UI 업데이트
            if (buyItemIcon != null)
            {
                buyItemIcon.sprite = equipment.icon;
                buyItemIcon.color = equipment.icon != null ? Color.white : Color.clear;
            }
            else
            {
                Debug.LogError($"❌ [TradeCenterUI] buyItemIcon이 null입니다!");
            }

            if (buyItemName != null)
            {
                buyItemName.text = equipment.equipmentName;
            }
            else
            {
                Debug.LogError($"❌ [TradeCenterUI] buyItemName가 null입니다!");
            }

            if (buyPriceText != null)
            {
                buyPriceText.text = price.ToString();
            }
            else
            {
                Debug.LogError($"❌ [TradeCenterUI] buyPriceText가 null입니다!");
            }
            
            // 버튼 상태 업데이트
            UpdateButtonStates();
            
            // 거래 상태 메시지 업데이트
            UpdateTransactionStatus();
        }
        else
        {
            Debug.LogError($"❌ [TradeCenterUI] equipment가 null입니다!");
        }
    }
    
    /// <summary>
    /// 판매 아이템 설정
    /// </summary>
    public void SetSellItem(EquipmentData equipment, int price)
    {
        if (showDebugLogs)
            Debug.Log($"💸 [TradeCenterUI] SetSellItem: {equipment?.equipmentName}, 가격: {price}");
        
        currentSellItem = equipment;
        currentSellPrice = price;
        
        if (equipment != null)
        {
            // UI 업데이트
            if (sellItemIcon != null)
            {
                sellItemIcon.sprite = equipment.icon;
                sellItemIcon.color = equipment.icon != null ? Color.white : Color.clear;
            }
            
            if (sellItemName != null)
                sellItemName.text = equipment.equipmentName;
            
            if (sellPriceText != null)
                sellPriceText.text = price.ToString();
            
            // 슬롯 활성화
            if (sellSlotContainer != null)
                sellSlotContainer.SetActive(true);
            
            // 판매 버튼 상태 업데이트
            UpdateSellButtonState();
        }
        
        UpdateTransactionStatus();
    }
    
    /// <summary>
    /// 거래 슬롯 초기화
    /// </summary>
    public void ResetTradeSlots()
    {
        // 구매 슬롯 초기화
        currentBuyItemID = "";
        currentBuyPrice = 0;
        ClearSlotUI(buySlotContainer, buyItemIcon, buyItemName, buyPriceText);
        
        // 판매 슬롯 초기화
        currentSellItem = null;
        currentSellPrice = 0;
        ClearSlotUI(sellSlotContainer, sellItemIcon, sellItemName, sellPriceText);
        
        // 버튼 상태 업데이트
        UpdateButtonStates();
        // UpdateTransactionStatus("아이템을 선택해주세요");
        UpdateTransactionStatus("Please select an item");
        
        if (showDebugLogs)
            Debug.Log("🔄 [TradeCenterUI] 거래 슬롯 초기화");
    }
    
    /// <summary>
    /// 🆕 구매 슬롯만 초기화
    /// </summary>
    public void ResetBuySlot()
    {
        currentBuyItemID = "";
        currentBuyPrice = 0;
        ClearSlotUI(buySlotContainer, buyItemIcon, buyItemName, buyPriceText);
        
        UpdateButtonStates();
        UpdateTransactionStatus();
        
        if (showDebugLogs)
            Debug.Log("🔄 [TradeCenterUI] 구매 슬롯 초기화");
    }
    
    /// <summary>
    /// 🆕 판매 슬롯만 초기화
    /// </summary>
    public void ResetSellSlot()
    {
        currentSellItem = null;
        currentSellPrice = 0;
        ClearSlotUI(sellSlotContainer, sellItemIcon, sellItemName, sellPriceText);
        
        UpdateButtonStates();
        UpdateTransactionStatus();
        
        if (showDebugLogs)
            Debug.Log("🔄 [TradeCenterUI] 판매 슬롯 초기화");
    }
    
    /// <summary>
    /// 슬롯 UI 정리
    /// </summary>
    private void ClearSlotUI(GameObject container, Image icon, TMP_Text nameText, TMP_Text priceText)
    {
        if (container != null)
            container.SetActive(false);
        
        if (icon != null)
            icon.color = Color.clear;
        
        if (nameText != null)
            nameText.text = "";
        
        if (priceText != null)
            priceText.text = "0";
    }
    
    /// <summary>
    /// 플레이어 골드 표시 업데이트
    /// </summary>
    public void UpdatePlayerGoldDisplay()
    {
        if (playerGoldText != null && PlayerDataManager.Instance != null)
        {
            playerGoldText.text = PlayerDataManager.Instance.CurrentGold.ToString();
        }
    }
    
    /// <summary>
    /// 구매 버튼 상태 업데이트
    /// </summary>
    private void UpdateBuyButtonState()
    {
        if (buyButton == null) 
        {
            Debug.LogError($"❌ [TradeCenterUI] buyButton이 null입니다!");
            return;
        }
        
        // 각 조건별 확인
        bool hasItemID = !string.IsNullOrEmpty(currentBuyItemID);
        bool hasPlayerManager = PlayerDataManager.Instance != null;
        bool hasEnoughGold = hasPlayerManager && PlayerDataManager.Instance.CurrentGold >= currentBuyPrice;
        bool hasInventorySpace = hasPlayerManager && !PlayerDataManager.Instance.IsInventoryFull;
        
        bool canBuy = hasItemID && hasPlayerManager && hasEnoughGold && hasInventorySpace;
        
        buyButton.interactable = canBuy;
        
        // 시각적 피드백
        var colors = buyButton.colors;
        colors.normalColor = canBuy ? Color.white : Color.gray;
        buyButton.colors = colors;
        
        // 구매 취소 버튼 상태 (구매 아이템이 있으면 활성화)
        if (buyCancelButton != null)
        {
            bool canCancelBuy = hasItemID;
            buyCancelButton.interactable = canCancelBuy;
            
            var cancelColors = buyCancelButton.colors;
            cancelColors.normalColor = canCancelBuy ? Color.white : Color.gray;
            buyCancelButton.colors = cancelColors;
        }
    }
    
    /// <summary>
    /// 판매 버튼 상태 업데이트
    /// </summary>
    private void UpdateSellButtonState()
    {
        if (sellButton == null) return;
        
        bool canSell = currentSellItem != null && 
                      currentSellItem.isTradable;
        
        sellButton.interactable = canSell;
        
        // 시각적 피드백
        var colors = sellButton.colors;
        colors.normalColor = canSell ? Color.white : Color.gray;
        sellButton.colors = colors;
        
        // 🆕 판매 취소 버튼 상태 (판매 아이템이 있으면 활성화)
        if (sellCancelButton != null)
        {
            bool canCancelSell = currentSellItem != null;
            sellCancelButton.interactable = canCancelSell;
            
            var cancelColors = sellCancelButton.colors;
            cancelColors.normalColor = canCancelSell ? Color.white : Color.gray;
            sellCancelButton.colors = cancelColors;
            
            if (showDebugLogs)
                Debug.Log($"✅ [TradeCenterUI] 판매 취소 버튼 활성화: {canCancelSell}");
        }
    }
    
    /// <summary>
    /// 모든 버튼 상태 업데이트
    /// </summary>
    private void UpdateButtonStates()
    {
        UpdateBuyButtonState();
        UpdateSellButtonState();
    }
    
    /// <summary>
    /// 거래 상태 메시지 업데이트
    /// </summary>
    private void UpdateTransactionStatus(string message = "")
    {
        if (transactionStatusText == null) 
        {
            Debug.LogError($"❌ [TradeCenterUI] transactionStatusText가 null입니다!");
            return;
        }
        
        if (!string.IsNullOrEmpty(message))
        {
            transactionStatusText.text = message;
            return;
        }
        
        // 자동 상태 메시지 생성
        if (!string.IsNullOrEmpty(currentBuyItemID))
        {
            if (PlayerDataManager.Instance != null)
            {
                int currentGold = PlayerDataManager.Instance.CurrentGold;
                bool isInventoryFull = PlayerDataManager.Instance.IsInventoryFull;
                
                if (currentGold < currentBuyPrice)
                {
                    // transactionStatusText.text = $"골드 부족! (필요: {currentBuyPrice})";
                    transactionStatusText.text = $"Gold shortage! (Required: {currentBuyPrice})";
                    transactionStatusText.color = Color.red;
                }
                else if (isInventoryFull)
                {
                    // transactionStatusText.text = "인벤토리가 가득 참!";
                    transactionStatusText.text = "Inventory is full!";
                    transactionStatusText.color = Color.red;
                }
                else
                {
                    // transactionStatusText.text = "구매 준비 완료";
                    transactionStatusText.text = "Ready to buy";                    
                    transactionStatusText.color = Color.green;
                }
            }
            else
            {
                Debug.LogError($"❌ [TradeCenterUI] PlayerDataManager.Instance가 null입니다!");
            }
        }
        else if (currentSellItem != null)
        {
            if (currentSellItem.isTradable)
            {
                // transactionStatusText.text = "판매 준비 완료";
                transactionStatusText.text = "Ready to sell";
                transactionStatusText.color = Color.green;
            }
            else
            {
                // transactionStatusText.text = "판매할 수 없는 아이템";
                transactionStatusText.text = "Items that cannot be sold";
                transactionStatusText.color = Color.red;
            }
        }
        else
        {
            // transactionStatusText.text = "아이템을 선택해주세요";
            transactionStatusText.text = "Please select an item";

            transactionStatusText.color = Color.white;
        }
    }
    
    /// <summary>
    /// 거래 완료 후 상태 갱신
    /// </summary>
    public void OnTransactionCompleted()
    {
        UpdatePlayerGoldDisplay();
        UpdateButtonStates();
        ResetTradeSlots();
    }
    
    // 현재 거래 정보 접근자
    public string CurrentBuyItemID => currentBuyItemID;
    public EquipmentData CurrentSellItem => currentSellItem;
    public int CurrentBuyPrice => currentBuyPrice;
    public int CurrentSellPrice => currentSellPrice;
}
