using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 정적 아이템 데이터베이스 (Static Class)
/// EquipmentData, ConsumableData 등 모든 정적 아이템 데이터 캐싱
/// 씬 독립적, 전역 접근 가능
/// </summary>
public static class ItemDatabase
{
    #region 캐시
    
    /// <summary>
    /// EquipmentData 캐시 (이름 기반)
    /// </summary>
    private static Dictionary<string, EquipmentData> equipmentCache;
    
    /// <summary>
    /// 초기화 여부
    /// </summary>
    private static bool isInitialized = false;
    
    #endregion
    
    #region 초기화
    
    /// <summary>
    /// 자동 초기화 (씬 로드 전)
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (isInitialized) return;
        
        LoadAllEquipmentData();
        
        isInitialized = true;
        Debug.Log($"[ItemDatabase] 초기화 완료 - {equipmentCache.Count}개 장비 데이터");
    }
    
    /// <summary>
    /// 모든 EquipmentData 로드
    /// </summary>
    private static void LoadAllEquipmentData()
    {
        equipmentCache = new Dictionary<string, EquipmentData>();
        
        // 우선순위 1: Resources/Equipment/ 폴더 (실제 위치)
        EquipmentData[] allEquipment = Resources.LoadAll<EquipmentData>("Equipment");
        
        foreach (var data in allEquipment)
        {
            if (equipmentCache.ContainsKey(data.name))
            {
                Debug.LogWarning($"[ItemDatabase] 중복된 이름: {data.name}");
                continue;
            }
            
            equipmentCache[data.name] = data;
        }
        
        // 우선순위 2: Resources/Data/EquipmentData/ 폴더 (fallback)
        if (equipmentCache.Count == 0)
        {
            allEquipment = Resources.LoadAll<EquipmentData>("Data/EquipmentData");
            foreach (var data in allEquipment)
            {
                if (!equipmentCache.ContainsKey(data.name))
                {
                    equipmentCache[data.name] = data;
                }
            }
        }
        
        // 우선순위 3: Resources/Data/ 폴더 (최종 fallback)
        if (equipmentCache.Count == 0)
        {
            allEquipment = Resources.LoadAll<EquipmentData>("Data");
            foreach (var data in allEquipment)
            {
                if (!equipmentCache.ContainsKey(data.name))
                {
                    equipmentCache[data.name] = data;
                }
            }
        }
    }
    
    #endregion
    
    #region EquipmentData 접근
    
    /// <summary>
    /// EquipmentData 가져오기 (이름 기반)
    /// </summary>
    public static EquipmentData GetEquipment(string dataName)
    {
        if (!isInitialized)
        {
            Initialize();
        }
        
        if (string.IsNullOrEmpty(dataName))
        {
            Debug.LogWarning("[ItemDatabase] 빈 dataName 요청");
            return null;
        }
        
        if (equipmentCache.TryGetValue(dataName, out var data))
        {
            return data;
        }
        
        Debug.LogWarning($"[ItemDatabase] EquipmentData를 찾을 수 없습니다: {dataName}");
        return null;
    }
    
    /// <summary>
    /// 모든 EquipmentData 가져오기
    /// </summary>
    public static IEnumerable<EquipmentData> GetAllEquipment()
    {
        if (!isInitialized)
        {
            Initialize();
        }
        
        return equipmentCache.Values;
    }
    
    /// <summary>
    /// EquipmentData 존재 여부 체크
    /// </summary>
    public static bool HasEquipment(string dataName)
    {
        if (!isInitialized)
        {
            Initialize();
        }
        
        return equipmentCache.ContainsKey(dataName);
    }
    
    /// <summary>
    /// 캐시된 장비 개수
    /// </summary>
    public static int EquipmentCount
    {
        get
        {
            if (!isInitialized)
            {
                Initialize();
            }
            return equipmentCache.Count;
        }
    }
    
    #endregion
    
    #region 디버그
    
    /// <summary>
    /// 전체 캐시 정보 출력
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void DebugCacheInfo()
    {
        if (!isInitialized) return;
        
        Debug.Log($"=== ItemDatabase 캐시 정보 ===");
        Debug.Log($"총 {equipmentCache.Count}개 EquipmentData:");
        
        foreach (var kvp in equipmentCache)
        {
            Debug.Log($"  - {kvp.Key}: {kvp.Value.equipmentName} (Grade: {kvp.Value.itemGrade})");
        }
    }
    
    #endregion
}

