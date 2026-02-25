using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🎲 장비 슬롯별 스탯 풀 데이터 로더
/// StatPoolConfig.csv를 읽어서 주옵션/부옵션 후보군을 캐싱
/// </summary>
public static class StatPoolDataLoader
{
    #region 캐시 데이터
    
    /// <summary>
    /// 슬롯별 스탯 풀 (Key: PoolID, Value: (주옵션, 부옵션 리스트))
    /// </summary>
    public static Dictionary<string, (EStatType MainStat, List<EStatType> SubStats)> StatPools { get; private set; }
    
    /// <summary>
    /// 초기화 여부
    /// </summary>
    public static bool IsInitialized { get; private set; } = false;
    
    #endregion
    
    #region 초기화
    
    /// <summary>
    /// Unity 자동 초기화 (씬 로드 전)
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        Initialize();
    }
    
    /// <summary>
    /// CSV 파일 로드 및 파싱
    /// </summary>
    public static void Initialize()
    {
        if (IsInitialized)
        {
            Debug.Log("✅ [StatPoolDataLoader] 이미 초기화됨 (캐시 사용)");
            return;
        }
        
        Debug.Log("📊 [StatPoolDataLoader] CSV 파싱 시작...");
        
        StatPools = new Dictionary<string, (EStatType, List<EStatType>)>();
        
        // CSV 파일 로드
        TextAsset csvFile = Resources.Load<TextAsset>("Combat/CSV/StatPoolConfig");
        
        if (csvFile == null)
        {
            Debug.LogError("❌ [StatPoolDataLoader] StatPoolConfig.csv를 찾을 수 없습니다!");
            Debug.LogError("   경로: Resources/Combat/CSV/StatPoolConfig.csv");
            return;
        }
        
        // 라인 분할
        string[] lines = csvFile.text.Split('\n');
        
        if (lines.Length <= 1)
        {
            Debug.LogError("❌ [StatPoolDataLoader] CSV 파일이 비어있거나 Header만 존재합니다!");
            return;
        }
        
        // 파싱 (Header 스킵)
        int parsedCount = 0;
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            
            // 빈 줄 스킵
            if (string.IsNullOrEmpty(line)) continue;
            
            // 컬럼 분할
            string[] columns = line.Split(',');
            
            // 최소 3개 컬럼 필요 (PoolID, MainStat, SubStat1...)
            if (columns.Length < 3)
            {
                Debug.LogWarning($"⚠️ [StatPoolDataLoader] Line {i+1}: 컬럼 부족 (최소 3개 필요), 스킵");
                continue;
            }
            
            // 0번: PoolID
            string poolId = columns[0].Trim();
            
            if (string.IsNullOrEmpty(poolId))
            {
                Debug.LogWarning($"⚠️ [StatPoolDataLoader] Line {i+1}: PoolID가 비어있음, 스킵");
                continue;
            }
            
            // 1번: MainStat
            EStatType mainStat = ParseStatType(columns[1].Trim());
            
            if (mainStat == EStatType.None)
            {
                Debug.LogWarning($"⚠️ [StatPoolDataLoader] Line {i+1}: MainStat 파싱 실패 ({columns[1]}), 스킵");
                continue;
            }
            
            // 2번~끝: SubStats (가변 길이)
            List<EStatType> subStats = new List<EStatType>();
            
            for (int j = 2; j < columns.Length; j++)
            {
                string subStatStr = columns[j].Trim();
                
                // 빈 칸 스킵
                if (string.IsNullOrEmpty(subStatStr)) continue;
                
                EStatType subStat = ParseStatType(subStatStr);
                
                if (subStat != EStatType.None)
                {
                    subStats.Add(subStat);
                }
                else
                {
                    Debug.LogWarning($"⚠️ [StatPoolDataLoader] Line {i+1}, Column {j+1}: SubStat 파싱 실패 ({subStatStr}), 스킵");
                }
            }
            
            // Dictionary에 저장
            StatPools[poolId] = (mainStat, subStats);
            parsedCount++;
            
            Debug.Log($"✅ [StatPoolDataLoader] {poolId}: MainStat={mainStat}, SubStats={subStats.Count}개");
        }
        
        IsInitialized = true;
        
        Debug.Log("========================================");
        Debug.Log($"✅ [StatPoolDataLoader] 초기화 완료: {parsedCount}개 풀 로드");
        Debug.Log("========================================");
    }
    
    #endregion
    
    #region 헬퍼 메서드
    
    /// <summary>
    /// 문자열 → EStatType 변환
    /// </summary>
    private static EStatType ParseStatType(string statStr)
    {
        if (string.IsNullOrEmpty(statStr))
            return EStatType.None;
        
        // EStatType enum으로 파싱
        if (Enum.TryParse<EStatType>(statStr, out EStatType result))
        {
            return result;
        }
        
        return EStatType.None;
    }
    
    /// <summary>
    /// 특정 PoolID의 스탯 풀 가져오기
    /// </summary>
    public static bool TryGetStatPool(string poolId, out EStatType mainStat, out List<EStatType> subStats)
    {
        // 초기화 확인
        if (!IsInitialized)
        {
            Debug.LogWarning("⚠️ [StatPoolDataLoader] 초기화되지 않았습니다. Initialize() 호출 중...");
            Initialize();
        }
        
        if (StatPools.ContainsKey(poolId))
        {
            var pool = StatPools[poolId];
            mainStat = pool.MainStat;
            subStats = new List<EStatType>(pool.SubStats); // 복사본 반환
            return true;
        }
        
        mainStat = EStatType.None;
        subStats = new List<EStatType>();
        return false;
    }
    
    #endregion
}

