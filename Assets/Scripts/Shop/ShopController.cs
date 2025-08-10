using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 🏪 상점 시스템 컨트롤러 (Service Layer)
/// UI와 분리된 순수 비즈니스 로직 담당
/// </summary>
public class ShopController : MonoBehaviour
{
    public static ShopController Instance { get; private set; }
    
    [Header("💰 가격 제공자")]
    [SerializeField] private MonoBehaviour priceProviderBehaviour;
    private IPriceProvider priceProvider;
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 이벤트 시스템
    public event Action<string> OnItemPurchased;     // 아이템 구매 완료
    public event Action<string> OnItemSold;          // 아이템 판매 완료
    public event Action<string> OnTransactionFailed; // 거래 실패
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 🗑️ 제거: DontDestroyOnLoad(gameObject);
            // 로비에서만 사용하는 상점은 씬과 함께 파괴되어야 함
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        InitializePriceProvider();
    }
    
    /// <summary>
    /// 가격 제공자 초기화
    /// </summary>
    private void InitializePriceProvider()
    {
        if (priceProviderBehaviour != null)
        {
            priceProvider = priceProviderBehaviour as IPriceProvider;
            if (priceProvider == null)
            {
                Debug.LogError("❌ [ShopController] priceProviderBehaviour가 IPriceProvider를 구현하지 않습니다!");
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log($"✅ [ShopController] 가격 제공자 연결: {priceProviderBehaviour.GetType().Name}");
            }
        }
        else
        {
            Debug.LogError("❌ [ShopController] priceProviderBehaviour가 할당되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 아이템 구매 시도
    /// </summary>
    public bool TryPurchaseItem(string itemID)
    {
        if (priceProvider == null || PlayerDataManager.Instance == null)
        {
            if (showDebugLogs)
                Debug.LogError("❌ [ShopController] 필수 컴포넌트가 없습니다!");
            return false;
        }
        
        // 구매 가능 여부 확인
        if (!priceProvider.IsItemAvailable(itemID))
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [ShopController] {itemID}는 구매할 수 없는 아이템입니다!");
            OnTransactionFailed?.Invoke(itemID);
            return false;
        }
        
        int buyPrice = priceProvider.GetBuyPrice(itemID);
        
        // 골드 확인
        if (PlayerDataManager.Instance.CurrentGold < buyPrice)
        {
            if (showDebugLogs)
                Debug.LogWarning($"💸 [ShopController] 골드 부족! 필요: {buyPrice}, 보유: {PlayerDataManager.Instance.CurrentGold}");
            OnTransactionFailed?.Invoke(itemID);
            return false;
        }
        
        // EquipmentData 로드
        EquipmentData equipment = Resources.Load<EquipmentData>($"Equipment/{itemID}_Equipment");
        if (equipment == null)
        {
            if (showDebugLogs)
                Debug.LogError($"❌ [ShopController] {itemID} EquipmentData를 찾을 수 없습니다!");
            OnTransactionFailed?.Invoke(itemID);
            return false;
        }
        
        // 인벤토리 공간 확인
        if (PlayerDataManager.Instance.IsInventoryFull)
        {
            if (showDebugLogs)
                Debug.LogWarning($"📦 [ShopController] 인벤토리가 가득 참!");
            OnTransactionFailed?.Invoke(itemID);
            return false;
        }
        
        // 거래 실행
        if (PlayerDataManager.Instance.SpendGold(buyPrice) && 
            PlayerDataManager.Instance.AddToInventory(equipment))
        {
            if (showDebugLogs)
                Debug.Log($"✅ [ShopController] {itemID} 구매 성공! 가격: {buyPrice}");
            OnItemPurchased?.Invoke(itemID);
            return true;
        }
        
        if (showDebugLogs)
            Debug.LogError($"❌ [ShopController] {itemID} 구매 실패!");
        OnTransactionFailed?.Invoke(itemID);
        return false;
    }
    
    /// <summary>
    /// 아이템 판매 시도
    /// </summary>
    public bool TrySellItem(EquipmentData equipment)
    {
        if (priceProvider == null || PlayerDataManager.Instance == null || equipment == null)
        {
            if (showDebugLogs)
                Debug.LogError("❌ [ShopController] 필수 컴포넌트가 없습니다!");
            return false;
        }
        
        // 판매 가능 여부 확인
        if (!equipment.isTradable)
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [ShopController] {equipment.itemID}는 판매할 수 없는 아이템입니다!");
            OnTransactionFailed?.Invoke(equipment.itemID);
            return false;
        }
        
        int sellPrice = priceProvider.GetSellPrice(equipment.itemID);
        
        // 거래 실행
        if (PlayerDataManager.Instance.RemoveFromInventory(equipment))
        {
            PlayerDataManager.Instance.AddGold(sellPrice);
            if (showDebugLogs)
                Debug.Log($"✅ [ShopController] {equipment.itemID} 판매 성공! 가격: {sellPrice}");
            OnItemSold?.Invoke(equipment.itemID);
            return true;
        }
        
        if (showDebugLogs)
            Debug.LogError($"❌ [ShopController] {equipment.itemID} 판매 실패!");
        OnTransactionFailed?.Invoke(equipment.itemID);
        return false;
    }
    
    /// <summary>
    /// 아이템 가격 조회 (UI용)
    /// </summary>
    public int GetItemBuyPrice(string itemID)
    {
        return priceProvider?.GetBuyPrice(itemID) ?? 0;
    }
    
    public int GetItemSellPrice(string itemID)
    {
        return priceProvider?.GetSellPrice(itemID) ?? 0;
    }
}
