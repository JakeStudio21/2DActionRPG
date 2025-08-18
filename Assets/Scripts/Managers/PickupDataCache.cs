using System.Collections.Generic;
using UnityEngine;
using ItemSystem;

/// <summary>
/// 🚀 Pickup 아이템 데이터 캐시 시스템 (Gold, Health 통합)
/// 파일명 의존성 제거 + O(1) 조회 성능
/// </summary>
public class PickupDataCache : Singleton<PickupDataCache>
{
    [Header("📊 캐시 상태")]
    [SerializeField] private int cachedGoldItemCount = 0;
    [SerializeField] private int cachedHealthItemCount = 0;
    [SerializeField] private int totalCachedItemCount = 0;
    [SerializeField] private bool isInitialized = false;
    
    [Header("🔧 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 캐시 딕셔너리들
    private Dictionary<string, GoldItemData> goldItemCache = new Dictionary<string, GoldItemData>();
    private Dictionary<string, HealthItemData> healthItemCache = new Dictionary<string, HealthItemData>();
    private Dictionary<string, BaseItemData> allItemCache = new Dictionary<string, BaseItemData>(); // 통합 조회용
    
    protected override void Awake()
    {
        base.Awake();
        
        // 씬 전환 시에도 유지되도록 설정
        DontDestroyOnLoad(gameObject);
        
        InitializeCache();
    }
    
    /// <summary>
    /// 캐시 초기화
    /// </summary>
    private void InitializeCache()
    {
        if (isInitialized) return;
        
        Debug.Log("🔄 [PickupDataCache] 초기화 시작...");
        
        // Gold 아이템 로드
        LoadGoldItems();
        
        // Health 아이템 로드
        LoadHealthItems();
        
        // 총 캐시 수 계산
        totalCachedItemCount = goldItemCache.Count + healthItemCache.Count;
        isInitialized = true;
        
        Debug.Log($"✅ [PickupDataCache] 초기화 완료: 총 {totalCachedItemCount}개 아이템 캐시됨");
        Debug.Log($"   - Gold 아이템: {cachedGoldItemCount}개");
        Debug.Log($"   - Health 아이템: {cachedHealthItemCount}개");
    }
    
    /// <summary>
    /// Gold 아이템 로드
    /// </summary>
    private void LoadGoldItems()
    {
        GoldItemData[] allGoldItems = Resources.LoadAll<GoldItemData>("PickupData");
        
        Debug.Log($"📁 [PickupDataCache] Gold 아이템 로드 결과: {allGoldItems.Length}개");
        
        foreach (var goldItem in allGoldItems)
        {
            if (goldItem != null && !string.IsNullOrEmpty(goldItem.itemId))
            {
                goldItemCache[goldItem.itemId] = goldItem;
                allItemCache[goldItem.itemId] = goldItem; // 통합 캐시에도 추가
                
                if (showDebugLogs)
                {
                    Debug.Log($"💰 [PickupDataCache] Gold 캐시 추가: {goldItem.itemId} → {goldItem.itemName}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ [PickupDataCache] 잘못된 GoldItemData: {goldItem?.name}");
            }
        }
        
        cachedGoldItemCount = goldItemCache.Count;
    }
    
    /// <summary>
    /// Health 아이템 로드
    /// </summary>
    private void LoadHealthItems()
    {
        HealthItemData[] allHealthItems = Resources.LoadAll<HealthItemData>("PickupData");
        
        Debug.Log($"📁 [PickupDataCache] Health 아이템 로드 결과: {allHealthItems.Length}개");
        
        foreach (var healthItem in allHealthItems)
        {
            if (healthItem != null && !string.IsNullOrEmpty(healthItem.itemId))
            {
                healthItemCache[healthItem.itemId] = healthItem;
                allItemCache[healthItem.itemId] = healthItem; // 통합 캐시에도 추가
                
                if (showDebugLogs)
                {
                    Debug.Log($"❤️ [PickupDataCache] Health 캐시 추가: {healthItem.itemId} → {healthItem.itemName}");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ [PickupDataCache] 잘못된 HealthItemData: {healthItem?.name}");
            }
        }
        
        cachedHealthItemCount = healthItemCache.Count;
    }
    
    /// <summary>
    /// itemID로 Gold 아이템 조회
    /// </summary>
    public GoldItemData GetGoldItemData(string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogWarning("⚠️ [PickupDataCache] itemID가 null 또는 빈 문자열입니다!");
            return null;
        }
        
        if (!isInitialized)
        {
            Debug.LogWarning("⚠️ [PickupDataCache] 캐시가 아직 초기화되지 않았습니다!");
            return null;
        }
        
        if (goldItemCache.TryGetValue(itemID, out GoldItemData goldItem))
        {
            if (showDebugLogs)
            {
                Debug.Log($"💰 [PickupDataCache] Gold 캐시 히트: {itemID} → {goldItem.itemName}");
            }
            return goldItem;
        }
        
        Debug.LogError($"❌ [PickupDataCache] GoldItemData를 찾을 수 없습니다: {itemID}");
        return null;
    }
    
    /// <summary>
    /// itemID로 Health 아이템 조회
    /// </summary>
    public HealthItemData GetHealthItemData(string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogWarning("⚠️ [PickupDataCache] itemID가 null 또는 빈 문자열입니다!");
            return null;
        }
        
        if (!isInitialized)
        {
            Debug.LogWarning("⚠️ [PickupDataCache] 캐시가 아직 초기화되지 않았습니다!");
            return null;
        }
        
        if (healthItemCache.TryGetValue(itemID, out HealthItemData healthItem))
        {
            if (showDebugLogs)
            {
                Debug.Log($"❤️ [PickupDataCache] Health 캐시 히트: {itemID} → {healthItem.itemName}");
            }
            return healthItem;
        }
        
        Debug.LogError($"❌ [PickupDataCache] HealthItemData를 찾을 수 없습니다: {itemID}");
        return null;
    }
    
    /// <summary>
    /// itemID로 모든 타입 아이템 조회 (통합)
    /// </summary>
    public BaseItemData GetPickupItemData(string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogWarning("⚠️ [PickupDataCache] itemID가 null 또는 빈 문자열입니다!");
            return null;
        }
        
        if (!isInitialized)
        {
            Debug.LogWarning("⚠️ [PickupDataCache] 캐시가 아직 초기화되지 않았습니다!");
            return null;
        }
        
        if (allItemCache.TryGetValue(itemID, out BaseItemData item))
        {
            if (showDebugLogs)
            {
                Debug.Log($"📦 [PickupDataCache] 통합 캐시 히트: {itemID} → {item.itemName}");
            }
            return item;
        }
        
        Debug.LogError($"❌ [PickupDataCache] PickupItemData를 찾을 수 없습니다: {itemID}");
        return null;
    }
    
    /// <summary>
    /// 캐시 상태 확인
    /// </summary>
    public bool HasPickupItemData(string itemID)
    {
        return allItemCache.ContainsKey(itemID);
    }
    
    /// <summary>
    /// 캐시된 모든 itemID 목록 반환
    /// </summary>
    public List<string> GetAllCachedItemIDs()
    {
        return new List<string>(allItemCache.Keys);
    }
    
    /// <summary>
    /// 아이템 타입별 개수 반환
    /// </summary>
    public (int goldCount, int healthCount, int totalCount) GetCacheStats()
    {
        return (cachedGoldItemCount, cachedHealthItemCount, totalCachedItemCount);
    }
    
    /// <summary>
    /// 캐시 강제 새로고침
    /// </summary>
    [ContextMenu("Refresh Cache")]
    public void RefreshCache()
    {
        goldItemCache.Clear();
        healthItemCache.Clear();
        allItemCache.Clear();
        isInitialized = false;
        InitializeCache();
    }
}
