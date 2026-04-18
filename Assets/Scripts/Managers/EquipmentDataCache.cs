using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🚀 EquipmentData 캐시 시스템
/// 파일명 의존성 제거 + O(1) 조회 성능
/// </summary>
public class EquipmentDataCache : MonoBehaviour
{
    [Header("📊 캐시 상태")]
    [SerializeField] private int cachedEquipmentCount = 0;
    [SerializeField] private bool isInitialized = false;
    
    [Header("🔧 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 캐시 딕셔너리
    private Dictionary<string, EquipmentData> equipmentCache = new Dictionary<string, EquipmentData>();
    
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
        
        // Resources/Equipment 폴더의 모든 EquipmentData 로드
        EquipmentData[] allEquipment = Resources.LoadAll<EquipmentData>("Equipment");
        
        foreach (var equipment in allEquipment)
        {
            if (equipment != null && !string.IsNullOrEmpty(equipment.itemID))
            {
                equipmentCache[equipment.itemID] = equipment;
                
            }
        }
        
        cachedEquipmentCount = equipmentCache.Count;
        isInitialized = true;
        
        Dbg.Log($"✅ [EquipmentDataCache] 초기화 완료: {cachedEquipmentCount}개 장비 캐시됨");
    }
    
    /// <summary>
    /// itemID로 EquipmentData 조회
    /// </summary>
    public EquipmentData GetEquipmentData(string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogWarning("⚠️ [EquipmentDataCache] itemID가 null 또는 빈 문자열입니다!");
            return null;
        }
        
        if (!isInitialized)
        {
            Debug.LogWarning("⚠️ [EquipmentDataCache] 캐시가 아직 초기화되지 않았습니다!");
            return null;
        }
        
        if (equipmentCache.TryGetValue(itemID, out EquipmentData equipment))
        {
            return equipment;
        }
        
        Debug.LogError($"❌ [EquipmentDataCache] EquipmentData를 찾을 수 없습니다: {itemID}");
        return null;
    }
    
    /// <summary>
    /// 캐시 상태 확인
    /// </summary>
    public bool HasEquipmentData(string itemID)
    {
        return equipmentCache.ContainsKey(itemID);
    }
    
    /// <summary>
    /// 캐시된 모든 itemID 목록 반환
    /// </summary>
    public List<string> GetAllCachedItemIDs()
    {
        return new List<string>(equipmentCache.Keys);
    }
    
    /// <summary>
    /// 캐시 강제 새로고침
    /// </summary>
    [ContextMenu("Refresh Cache")]
    public void RefreshCache()
    {
        equipmentCache.Clear();
        isInitialized = false;
        InitializeCache();
    }
}
