using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Shop;

/// <summary>
/// 🛒 구매 실패 이유
/// </summary>
public enum PurchaseFailReason
{
    None,               // 성공
    InsufficientGold,   // 골드 부족
    InventoryFull,      // 인벤토리 가득 참
    InvalidItem,        // 잘못된 아이템
    SystemError         // 시스템 오류
}

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
    
    [Header("🏪 상점 아이템 풀 (전시용)")]
    private ShopItemPool itemPool;
    
    // Public 접근자
    public ShopItemPool ItemPool => itemPool;
    
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
        InitializeShop();
    }
    




// ShopController.cs에 추가 (Update 메서드)
void Update()
{
#if UNITY_EDITOR || UNITY_STANDALONE
    if (Input.GetKeyDown(KeyCode.B))
        TestBuyItem();
#endif
}

private void TestBuyItem()
{
    Debug.Log("=== 🧪 구매 테스트 시작 ===");
    
    // Assasin 클래스 데이터 조회
    var assasinData = GetShopDataByClass(PlayerClass.Assasin);
    
    if (assasinData.Count > 0 && assasinData[0].items.Count > 0)
    {
        var firstItem = assasinData[0].items[0];
        
        Debug.Log($"💰 구매 시도: {firstItem.equipmentData.equipmentName} (가격: {firstItem.equipmentData.buyPrice})");
        Debug.Log($"💰 현재 골드: {PlayerDataManager.Instance.CurrentGold}"); // ⭐ V2: 계정 공유 골드
        
        bool success = BuyItemV2(firstItem.displayInstanceId);
        
        if (success)
        {
            Debug.Log($"✅ 구매 성공!");
            Debug.Log($"💰 남은 골드: {PlayerDataManager.Instance.CurrentGold}"); // ⭐ V2: 계정 공유 골드
        }
        else
        {
            Debug.Log($"❌ 구매 실패!");
        }
    }
    else
    {
        Debug.Log("⚠️ 구매할 아이템이 없습니다!");
    }
    
    Debug.Log("=== ✅ 구매 테스트 완료 ===");
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
        
        // V2 시스템: templateName = itemID 그대로 사용
        // itemID = "ITEM_ARMOR_WIZARD_B" → templateName = "ITEM_ARMOR_WIZARD_B"
        // ItemTemplateResolver가 자동으로 "_Equipment" 붙은 Asset 파일을 찾아줌
        string templateName = itemID;
        
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
        ItemInstanceID newItemId = AccountDataManager.Instance.RegisterNewInstance(templateName);
        
        if (newItemId.IsEmpty)
        {
            // 실패 시 골드 환불
            PlayerDataManager.Instance.AddGold(buyPrice);
            
            if (showDebugLogs)
                Debug.LogError($"❌ [ShopController] {itemID} 인스턴스 생성 실패! (골드 환불 완료)");
            OnTransactionFailed?.Invoke(itemID);
            return false;
        }
        
        // ⭐ Stage 3: 동적 스탯 생성 및 적용
        EquipmentData equipData = ItemTemplateResolver.Load(templateName);
        if (equipData != null)
        {
            // DynamicEquipmentGenerator로 랜덤 스탯 생성
            EquipmentInstance dynamicInstance = DynamicEquipmentGenerator.Generate(equipData, equipData.itemGrade);
            
            if (dynamicInstance != null)
            {
                // ItemInstanceData에 동적 스탯 저장
                ItemInstanceData instanceData = AccountDataManager.Instance.GetInstance(newItemId);
                if (instanceData != null)
                {
                    EquipmentInstanceConverter.ApplyDynamicStats(instanceData, dynamicInstance);
                    
                    if (showDebugLogs)
                        Debug.Log($"🎲 [ShopController] 동적 스탯 생성 완료: 주옵션={instanceData.finalMainStatValue}, 부옵션={instanceData.randomSubStats.Count}개");
                }
            }
        }
        
        // 2. 공유 창고에 추가 (가득 차면 우편함)
        bool addedToShared = AccountDataManager.Instance.TryAddToShared(newItemId);
        
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
            Debug.Log($"✅ [ShopController] {itemID} 구매 성공 (V2)! 가격: {buyPrice}, 위치: {destination}, ID: {newItemId.Value.Substring(0, 8)}...");
        }
        
        OnItemPurchased?.Invoke(itemID);
        return true;
    }
    
    /// <summary>
    /// 🆕 V2: 아이템 판매 시도 (ItemInstanceID 기반)
    /// </summary>
    public bool TrySellItem(EquipmentData equipment, ItemInstanceID instanceId)
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
        
        // 🆕 V2: ItemInstanceID 유효성 검사
        if (instanceId.IsEmpty)
        {
            if (showDebugLogs)
                Debug.LogError($"❌ [ShopController] 잘못된 ItemInstanceID!");
            OnTransactionFailed?.Invoke(equipment.itemID);
            return false;
        }
        
        int sellPrice = priceProvider.GetSellPrice(equipment.itemID);
        
        // 판매 = 아이템 영구 소멸 → DestroyItemInstance로 원본 + 참조 동시 제거
        // (기존 RemoveFromShared는 sharedInventoryIds 참조만 지우고 itemInstances 원본이 잔존하는 구조적 결함이 있었음)
        var account = AccountDataManager.Instance;

        // 창고에 없는 아이템은 판매 거부 (존재 여부 = sharedInventoryIds 포함 여부로 판단)
        if (!account.IsInSharedInventory(instanceId))
        {
            if (showDebugLogs)
                Debug.LogError($"❌ [ShopController] {equipment.itemID} 판매 실패! (보관창고에 아이템 없음)");
            OnTransactionFailed?.Invoke(equipment.itemID);
            return false;
        }

        account.DestroyItemInstance(instanceId);    // ① 원본(itemInstances) + 모든 참조 동시 제거
        PlayerDataManager.Instance.AddGold(sellPrice);
        account.Save();                             // ② 디스크 반영

        if (showDebugLogs)
            Debug.Log($"✅ [ShopController] {equipment.itemID} 판매 성공! 가격: {sellPrice}, ID: {instanceId.Value.Substring(0, 8)}...");

        OnItemSold?.Invoke(equipment.itemID);
        return true;
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
    
    #region Shop V2 System (전시용 Instance)
    
    /// <summary>
    /// ⭐ 상점 초기화 (1회만 실행, 멱등성 보장)
    /// - 전시용 ItemInstance 풀 생성
    /// - 자동 초기화 시스템으로 언제든 호출 가능
    /// </summary>
    public void InitializeShop()
    {
        // ✅ 이미 초기화되었으면 조용히 리턴 (멱등성)
        if (itemPool != null && itemPool.IsInitialized)
        {
            return;
        }
        
        if (showDebugLogs)
            Debug.Log("🏪 [ShopController] 상점 초기화 시작...");
        
        itemPool = new ShopItemPool();
        itemPool.Initialize();
        
        if (showDebugLogs)
            Debug.Log($"✅ [ShopController] 상점 초기화 완료! (전시용 Instance: {itemPool.DisplayInstanceCount}개)");
    }
    
    /// <summary>
    /// ⭐ 클래스별 상점 데이터 조회
    /// - usableClass 기반 자동 필터링
    /// - 6개 카테고리별로 그룹화
    /// - 등급순 정렬 (D → C → B → A)
    /// </summary>
    public List<ShopInventoryData> GetShopDataByClass(PlayerClass playerClass)
    {
        Debug.Log($"🔄 [ShopController] GetShopDataByClass({playerClass}) 시작");
        
        // ✅ 자동 초기화 보장 (Unity Start() 순서 문제 해결)
        if (itemPool == null || !itemPool.IsInitialized)
        {
            Debug.LogWarning("⚠️ [ShopController] 상점이 초기화되지 않아 자동 초기화 수행...");
            
            InitializeShop();
            
            // 초기화 실패 시
            if (itemPool == null || !itemPool.IsInitialized)
            {
                Debug.LogError("❌ [ShopController] 상점 자동 초기화 실패!");
                Debug.LogError("   💡 AccountDataManager가 초기화되었는지 확인하세요!");
                return new List<ShopInventoryData>();
            }
            
            Debug.Log("   ✅ 상점 자동 초기화 완료");
        }
        
        Debug.Log("   ✅ itemPool 확인됨 (초기화 완료)");
        
        // 1. 모든 D/C/B/A 등급 장비 로드
        EquipmentData[] allEquipments = Resources.LoadAll<EquipmentData>("Equipment");
        
        // 2. usableClass 필터링
        var filteredEquipments = allEquipments.Where(eq =>
            (eq.usableClass == playerClass || 
             eq.usableClass == PlayerClass.Any || 
             eq.usableClass == PlayerClass.None) &&
            (eq.itemGrade == ItemGrade.D || eq.itemGrade == ItemGrade.C || 
             eq.itemGrade == ItemGrade.B || eq.itemGrade == ItemGrade.A)
        ).ToList();
        
        if (showDebugLogs)
            Debug.Log($"🎯 [ShopController] {playerClass} 클래스 장비: {filteredEquipments.Count}개");
        
        // 3. 6개 카테고리별로 그룹화
        var result = new List<ShopInventoryData>();
        var categories = ShopCategoryHelper.GetAllCategories();
        
        foreach (var category in categories)
        {
            var slot = ShopCategoryHelper.ToEquipmentSlot(category);
            var shopData = new ShopInventoryData(playerClass, slot);
            
            // 해당 카테고리에 맞는 장비 필터링
            var categoryItems = filteredEquipments.Where(eq => IsMatchingCategory(eq, category)).ToList();
            
            // ShopItemEntry 생성
            foreach (var equipment in categoryItems)
            {
                // ⭐ itemID를 키로 사용 (V2 시스템 표준)
                var displayInstanceId = itemPool.GetDisplayInstance(equipment.itemID);
                if (!displayInstanceId.IsEmpty)
                {
                    var entry = new ShopItemEntry(displayInstanceId, equipment);
                    shopData.AddItem(entry);
                }
            }
            
            // 등급순 정렬 (D → C → B → A)
            shopData.SortByGrade();
            
            result.Add(shopData);
            
            if (showDebugLogs)
                Debug.Log($"  📦 {ShopCategoryHelper.GetCategoryName(category)}: {shopData.items.Count}개");
        }
        
        return result;
    }
    
    /// <summary>
    /// ⭐ 아이템 구매 (V2 시스템)
    /// - 전시용 displayInstanceId를 받아서 새로운 플레이어 전용 Instance 생성
    /// </summary>
    public bool BuyItemV2(ItemInstanceID displayInstanceId, out PurchaseFailReason failReason)
    {
        failReason = PurchaseFailReason.None;
        
        if (displayInstanceId.IsEmpty)
        {
            if (showDebugLogs)
                Debug.LogError("❌ [ShopController] 잘못된 displayInstanceId입니다!");
            failReason = PurchaseFailReason.InvalidItem;
            return false;
        }
        
        // ✅ 자동 초기화 보장
        if (itemPool == null || !itemPool.IsInitialized)
        {
            if (showDebugLogs)
                Debug.LogWarning("⚠️ [ShopController] 구매 시도 시 상점 자동 초기화...");
            
            InitializeShop();
            
            if (itemPool == null || !itemPool.IsInitialized)
            {
                Debug.LogError("❌ [ShopController] 상점 자동 초기화 실패!");
                failReason = PurchaseFailReason.SystemError;
                return false;
            }
        }
        
        // 1. 전시용 ID로 EquipmentData 조회 (이미 로드되어 있음)
        EquipmentData equipment = itemPool.GetEquipmentData(displayInstanceId);
        if (equipment == null)
        {
            Debug.LogError($"❌ [ShopController] EquipmentData를 찾을 수 없습니다: {displayInstanceId.Value}");
            failReason = PurchaseFailReason.InvalidItem;
            return false;
        }
        
        string templateName = equipment.itemID;
        
        int buyPrice = equipment.buyPrice;
        
        // 3. 골드 체크 (⭐ V2: AccountDataManager 사용)
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("❌ [ShopController] PlayerDataManager.Instance가 null입니다!");
            failReason = PurchaseFailReason.SystemError;
            return false;
        }
        
        int currentGold = PlayerDataManager.Instance.CurrentGold; // ⭐ V2: 계정 공유 골드
        if (currentGold < buyPrice)
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [ShopController] 골드 부족! 필요: {buyPrice}, 보유: {currentGold}");
            OnTransactionFailed?.Invoke(templateName);
            failReason = PurchaseFailReason.InsufficientGold;
            return false;
        }
        
        // 4. 캐시된 동적 스탯 가져오기 (팝업에서 본 스탯 = 구매 스탯)
        EquipmentInstance cachedInstance = itemPool.GetOrCreateDynamicStats(displayInstanceId);
        
        if (cachedInstance == null)
        {
            Debug.LogError($"❌ [ShopController] 캐시된 동적 스탯을 가져올 수 없습니다: {templateName}");
            OnTransactionFailed?.Invoke(templateName);
            failReason = PurchaseFailReason.SystemError;
            return false;
        }
        
        // 5. 새 ItemInstance 생성 (플레이어 전용)
        var newInstanceId = itemPool.CreateNewInstance(templateName);
        if (newInstanceId.IsEmpty)
        {
            Debug.LogError($"❌ [ShopController] 새 Instance 생성 실패: {templateName}");
            OnTransactionFailed?.Invoke(templateName);
            failReason = PurchaseFailReason.SystemError;
            return false;
        }
        
        // ⭐ Stage 4: 캐시된 동적 스탯을 Deep Copy하여 적용 (참조 꼬임 방지)
        // - cachedInstance.Clone()으로 완전한 복제본 생성
        // - newInstanceId를 할당하여 새로운 인스턴스로 만듦
        EquipmentInstance clonedInstance = cachedInstance.Clone();
        clonedInstance.instanceId = newInstanceId; // 🔑 새 ID 할당 (Deep Copy 완성)
        
        if (clonedInstance != null)
        {
            // ItemInstanceData에 동적 스탯 저장 (복제본 사용)
            ItemInstanceData instanceData = AccountDataManager.Instance.GetInstance(newInstanceId);
            if (instanceData != null)
            {
                EquipmentInstanceConverter.ApplyDynamicStats(instanceData, clonedInstance);
                
                if (showDebugLogs)
                    Debug.Log($"🎲 [ShopController] 캐시된 동적 스탯 복제 완료: 주옵션={instanceData.finalMainStatValue:F1}, 부옵션={instanceData.randomSubStats.Count}개 (원본 ID: {displayInstanceId.Value.Substring(0, 8)}... → 새 ID: {newInstanceId.Value.Substring(0, 8)}...)");
            }
        }
        
        // 6. 계정 공유 창고에 추가
        if (!AccountDataManager.Instance.TryAddToShared(newInstanceId))
        {
            Debug.LogError($"❌ [ShopController] 보관창고 추가 실패: {templateName}");
            OnTransactionFailed?.Invoke(templateName);
            failReason = PurchaseFailReason.InventoryFull;
            return false;
        }
        
        // 7. 골드 차감
        if (!PlayerDataManager.Instance.SpendGold(buyPrice))
        {
            Debug.LogError($"❌ [ShopController] 골드 차감 실패: {buyPrice}");
            failReason = PurchaseFailReason.InsufficientGold;
            return false;
        }
        PlayerDataManager.Instance.SaveOnMeaningfulEvent("ItemPurchased");
        
        // 8. UI 새로고침 이벤트 발생 (보관창고 업데이트)
        PlayerDataManager.Instance.NotifyInventoryChanged();
        
        if (showDebugLogs)
            Debug.Log($"✅ [ShopController] 구매 성공! {templateName} (가격: {buyPrice}, ID: {newInstanceId.Value.Substring(0, 8)}...)");
        
        // 9. 이벤트 발생
        OnItemPurchased?.Invoke(templateName);
        
        return true;
    }
    
    /// <summary>
    /// ⭐ 아이템 구매 (오버로드: 실패 이유 없음)
    /// </summary>
    public bool BuyItemV2(ItemInstanceID displayInstanceId)
    {
        return BuyItemV2(displayInstanceId, out _);
    }
    
    /// <summary>
    /// 장비가 특정 카테고리에 속하는지 체크
    /// </summary>
    private bool IsMatchingCategory(EquipmentData equipment, ShopCategory category)
    {
        if (equipment == null)
            return false;
        
        switch (category)
        {
            case ShopCategory.Weapon:
                return equipment.equipmentType == EquipmentType.Weapon;
                
            case ShopCategory.Armor:
                return equipment.equipmentType == EquipmentType.Armor && 
                       equipment.ArmorType == ArmorType.Armor;
                
            case ShopCategory.Boots:
                return equipment.equipmentType == EquipmentType.Armor && 
                       equipment.ArmorType == ArmorType.Boots;
                
            case ShopCategory.Helmet:
                return equipment.equipmentType == EquipmentType.Armor && 
                       equipment.ArmorType == ArmorType.Helmet;
                
            case ShopCategory.Belt:
                return equipment.equipmentType == EquipmentType.Armor && 
                       equipment.ArmorType == ArmorType.Belt;
                
            case ShopCategory.Gloves:
                return equipment.equipmentType == EquipmentType.Armor && 
                       equipment.ArmorType == ArmorType.Gloves;
                
            default:
                return false;
        }
    }
    
    #endregion
}
