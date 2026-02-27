using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 룬 데이터베이스 (정적 캐싱)
/// ⚙️ Phase 4-D-2: 인벤토리 및 세이브/로드 시스템
/// 
/// 역할:
/// - Resources/Runes/ 경로의 모든 RuneData ScriptableObject를 게임 시작 시 로드
/// - Dictionary로 캐싱하여 runeId(문자열)만으로 빠른 검색 지원
/// - 세이브 데이터 로드 시 runeId → RuneData 재연결에 사용
/// </summary>
public static class RuneDatabase
{
    #region 필드
    
    /// <summary>
    /// 룬 데이터 캐시 (Key: runeId, Value: RuneData)
    /// </summary>
    private static Dictionary<string, RuneData> runeDataCache;
    
    /// <summary>
    /// 초기화 완료 여부
    /// </summary>
    private static bool isInitialized = false;
    
    /// <summary>
    /// Resources 폴더 경로
    /// </summary>
    private const string RUNE_RESOURCES_PATH = "Runes";
    
    #endregion
    
    #region 초기화
    
    /// <summary>
    /// 게임 시작 시 자동 초기화 (RuntimeInitializeOnLoadMethod)
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        Initialize();
    }
    
    /// <summary>
    /// 데이터베이스 초기화
    /// Resources/Runes/ 폴더의 모든 RuneData를 로드하여 캐싱
    /// </summary>
    public static void Initialize()
    {
        if (isInitialized)
        {
            Debug.LogWarning("[RuneDatabase] 이미 초기화되었습니다.");
            return;
        }
        
        Debug.Log("[RuneDatabase] 초기화 시작...");
        
        runeDataCache = new Dictionary<string, RuneData>();
        
        // Resources/Runes/ 폴더의 모든 RuneData 로드
        RuneData[] allRunes = Resources.LoadAll<RuneData>(RUNE_RESOURCES_PATH);
        
        if (allRunes == null || allRunes.Length == 0)
        {
            Debug.LogWarning($"[RuneDatabase] {RUNE_RESOURCES_PATH} 경로에서 RuneData를 찾을 수 없습니다!");
            isInitialized = true;
            return;
        }
        
        // Dictionary에 캐싱
        int successCount = 0;
        int duplicateCount = 0;
        int invalidCount = 0;
        
        foreach (var rune in allRunes)
        {
            if (rune == null)
            {
                Debug.LogWarning("[RuneDatabase] null 룬 데이터 발견 (스킵)");
                continue;
            }
            
            // 유효성 검증
            if (!rune.IsValid())
            {
                Debug.LogWarning($"[RuneDatabase] 유효하지 않은 룬: {rune.name} (runeId 또는 runeName이 비어있음)");
                invalidCount++;
                continue;
            }
            
            // 중복 체크
            if (runeDataCache.ContainsKey(rune.runeId))
            {
                Debug.LogWarning($"[RuneDatabase] 중복된 runeId 발견: {rune.runeId} (기존: {runeDataCache[rune.runeId].name}, 새로운: {rune.name})");
                duplicateCount++;
                continue;
            }
            
            // 캐시에 추가
            runeDataCache.Add(rune.runeId, rune);
            successCount++;
        }
        
        isInitialized = true;
        
        Debug.Log($"[RuneDatabase] 초기화 완료!");
        Debug.Log($"  ✅ 성공: {successCount}개");
        if (duplicateCount > 0)
            Debug.LogWarning($"  ⚠️ 중복: {duplicateCount}개");
        if (invalidCount > 0)
            Debug.LogWarning($"  ❌ 무효: {invalidCount}개");
    }
    
    #endregion
    
    #region API
    
    /// <summary>
    /// runeId로 RuneData 가져오기
    /// </summary>
    /// <param name="runeId">룬 ID (예: RUNE_BOSS_HUNTER)</param>
    /// <returns>RuneData (없으면 null)</returns>
    public static RuneData GetRuneData(string runeId)
    {
        // 초기화 확인
        if (!isInitialized)
        {
            Debug.LogWarning("[RuneDatabase] 초기화되지 않았습니다. 자동 초기화 시도...");
            Initialize();
        }
        
        // 빈 문자열 체크
        if (string.IsNullOrEmpty(runeId))
        {
            Debug.LogWarning("[RuneDatabase] runeId가 비어있습니다.");
            return null;
        }
        
        // Dictionary에서 검색
        if (runeDataCache.TryGetValue(runeId, out RuneData runeData))
        {
            return runeData;
        }
        else
        {
            Debug.LogWarning($"[RuneDatabase] runeId를 찾을 수 없습니다: {runeId}");
            return null;
        }
    }
    
    /// <summary>
    /// 모든 룬 데이터 가져오기 (읽기 전용)
    /// </summary>
    /// <returns>모든 RuneData 목록</returns>
    public static IReadOnlyCollection<RuneData> GetAllRuneData()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[RuneDatabase] 초기화되지 않았습니다. 자동 초기화 시도...");
            Initialize();
        }
        
        return runeDataCache.Values;
    }
    
    /// <summary>
    /// 특정 타입의 룬 데이터 가져오기
    /// </summary>
    /// <param name="runeType">룬 타입 (예: Attack1, Survival2)</param>
    /// <returns>해당 타입의 RuneData 목록</returns>
    public static List<RuneData> GetRuneDataByType(RuneType runeType)
    {
        if (!isInitialized)
        {
            Initialize();
        }
        
        var result = new List<RuneData>();
        
        foreach (var rune in runeDataCache.Values)
        {
            if (rune.runeType == runeType)
            {
                result.Add(rune);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 캐시된 룬 데이터 개수
    /// </summary>
    public static int GetCachedCount()
    {
        return runeDataCache?.Count ?? 0;
    }
    
    /// <summary>
    /// runeId 존재 여부 확인
    /// </summary>
    public static bool Contains(string runeId)
    {
        if (!isInitialized)
        {
            Initialize();
        }
        
        return !string.IsNullOrEmpty(runeId) && runeDataCache.ContainsKey(runeId);
    }
    
    #endregion
    
    #region 디버그
    
    /// <summary>
    /// 캐시된 모든 룬 데이터 로그 출력
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void DebugPrintAll()
    {
        if (!isInitialized)
        {
            Initialize();
        }
        
        Debug.Log("========== [RuneDatabase] 캐시 목록 ==========");
        Debug.Log($"총 {runeDataCache.Count}개의 룬 데이터:");
        
        foreach (var kvp in runeDataCache)
        {
            var rune = kvp.Value;
            Debug.Log($"  [{kvp.Key}] {rune.runeName} ({rune.runeType})");
        }
        
        Debug.Log("==========================================");
    }
    
    #endregion
}

