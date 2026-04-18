using System.Collections.Generic;
using UnityEngine;
using ItemSystem;

/// <summary>
/// 🚀 Pickup 아이템 데이터 캐시 시스템 (Gold, Health 통합)
/// 파일명 의존성 제거 + O(1) 조회 성능
/// </summary>
public class PickupDataCache : MonoBehaviour
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
    
    private void Awake()
    {
        InitializeCache();
    }
    
    /// <summary>
    /// 캐시 초기화
    /// </summary>
    private void InitializeCache()
    {
        if (isInitialized) return;
        
        
        // Gold 아이템 로드
        LoadGoldItems();
        
        // Health 아이템 로드
        LoadHealthItems();
        
        // 총 캐시 수 계산
        totalCachedItemCount = goldItemCache.Count + healthItemCache.Count;
        isInitialized = true;
        
        Dbg.Log($"✅ [PickupDataCache] 초기화 완료: 총 {totalCachedItemCount}개 아이템 캐시됨");
    }
    
    /// <summary>
    /// Gold 아이템 로드
    /// </summary>
    private void LoadGoldItems()
    {
        GoldItemData[] allGoldItems = Resources.LoadAll<GoldItemData>("PickupData");
        
        
        foreach (var goldItem in allGoldItems)
        {
            if (goldItem != null && !string.IsNullOrEmpty(goldItem.itemId))
            {
                goldItemCache[goldItem.itemId] = goldItem;
                allItemCache[goldItem.itemId] = goldItem; // 통합 캐시에도 추가
                
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
        
        
        foreach (var healthItem in allHealthItems)
        {
            if (healthItem != null && !string.IsNullOrEmpty(healthItem.itemId))
            {
                healthItemCache[healthItem.itemId] = healthItem;
                allItemCache[healthItem.itemId] = healthItem; // 통합 캐시에도 추가
                
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
