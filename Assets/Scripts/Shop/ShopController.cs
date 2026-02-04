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
                // Debug.LogWarning($"💸 [ShopController] 골드 부족! 필요: {buyPrice}, 보유: {PlayerDataManager.Instance.CurrentGold}");
                Debug.LogWarning($"💸 [ShopController] Gold shortage! (Required: {buyPrice}, 보유: {PlayerDataManager.Instance.CurrentGold}");
            OnTransactionFailed?.Invoke(itemID);
            return false;
        }
        
        // V2 시스템: templateName 구성
        // itemID = "Sword_A" → templateName = "Sword_A_Equipment"
        string templateName = $"{itemID}_Equipment";
        
        // 골드 차감
        if (!PlayerDataManager.Instance.SpendGold(buyPrice))
        {
            if (showDebugLogs)
                Debug.LogWarning($"💰 [ShopController] 골드 부족!");
            OnTransactionFailed?.Invoke(itemID);
            return false;
        }
        
        // 🆕 V2: 계정 공유 창고에 아이템 추가
        // 1. 새 인스턴스 생성
        ItemInstanceId newItemId = AccountDataManager.Instance.RegisterNewInstance(templateName);
        
        if (!newItemId.IsValid())
        {
            // 실패 시 골드 환불
            PlayerDataManager.Instance.AddGold(buyPrice);
            
            if (showDebugLogs)
                Debug.LogError($"❌ [ShopController] {itemID} 인스턴스 생성 실패! (골드 환불 완료)");
            OnTransactionFailed?.Invoke(itemID);
            return false;
        }
        
        // 2. 공유 창고에 추가 (가득 차면 우편함)
        bool addedToShared = AccountDataManager.Instance.TryAddToShared(newItemId, 50);
        
        if (!addedToShared)
        {
            // 창고 가득 참 → 우편함으로 전송
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [ShopController] 보관창고 가득 참! 우편함으로 전송: {itemID}");
            
            AccountDataManager.Instance.MoveToMailbox(newItemId);  // 🔧 수정: TryAddToMailbox → MoveToMailbox
        }
        
        // 3. 계정 데이터 저장
        AccountDataManager.Instance.Save();
        
        if (showDebugLogs)
        {
            string destination = addedToShared ? "보관창고" : "우편함";
            Debug.Log($"✅ [ShopController] {itemID} 구매 성공 (V2)! 가격: {buyPrice}, 위치: {destination}, ID: {newItemId.id.Substring(0, 8)}...");
        }
        
        OnItemPurchased?.Invoke(itemID);
        return true;
    }
    
    /// <summary>
    /// 🆕 V2: 아이템 판매 시도 (ItemInstanceId 기반)
    /// </summary>
    public bool TrySellItem(EquipmentData equipment, ItemInstanceId instanceId)
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
        
        // 🆕 V2: ItemInstanceId 유효성 검사
        if (!instanceId.IsValid())
        {
            if (showDebugLogs)
                Debug.LogError($"❌ [ShopController] 잘못된 ItemInstanceId!");
            OnTransactionFailed?.Invoke(equipment.itemID);
            return false;
        }
        
        int sellPrice = priceProvider.GetSellPrice(equipment.itemID);
        
        // 🆕 V2: 계정 공유 창고에서 제거
        if (AccountDataManager.Instance.RemoveFromShared(instanceId))
        {
            PlayerDataManager.Instance.AddGold(sellPrice);
            AccountDataManager.Instance.Save();  // 🆕 V2: 계정 데이터 저장
            
            if (showDebugLogs)
                Debug.Log($"✅ [ShopController] {equipment.itemID} 판매 성공! 가격: {sellPrice}, ID: {instanceId.id.Substring(0, 8)}...");
            
            OnItemSold?.Invoke(equipment.itemID);
            return true;
        }
        
        if (showDebugLogs)
            Debug.LogError($"❌ [ShopController] {equipment.itemID} 판매 실패! (보관창고에서 제거 실패)");
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
