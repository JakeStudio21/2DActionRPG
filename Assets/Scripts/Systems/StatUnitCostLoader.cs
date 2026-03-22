using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// 📊 스탯 단가(unitCost) 데이터 로더
/// StatUnitCost.csv를 파싱하여 캐싱
/// [Single Source of Truth for Stat Unit Costs]
/// </summary>
public static class StatUnitCostLoader
{
    #region 캐시 데이터

    /// <summary>
    /// 스탯 타입 → 단가 (Key: EStatType, Value: unitCost)
    /// </summary>
    public static Dictionary<EStatType, float> UnitCostTable { get; private set; }

    /// <summary>
    /// 초기화 여부
    /// </summary>
    public static bool IsInitialized { get; private set; } = false;

    private const float DEFAULT_UNIT_COST = 1.0f;

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
            Debug.Log("✅ [StatUnitCostLoader] 이미 초기화됨 (캐시 사용)");
            return;
        }

        Debug.Log("📊 [StatUnitCostLoader] CSV 파싱 시작...");

        UnitCostTable = new Dictionary<EStatType, float>();

        TextAsset csvFile = Resources.Load<TextAsset>("Combat/CSV/StatUnitCost");

        if (csvFile == null)
        {
            Debug.LogError("❌ [StatUnitCostLoader] StatUnitCost.csv를 찾을 수 없습니다!");
            Debug.LogError("   경로: Resources/Combat/CSV/StatUnitCost.csv");
            IsInitialized = true; // 빈 테이블로라도 초기화 완료 처리
            return;
        }

        string[] lines = csvFile.text.Split('\n');

        if (lines.Length <= 1)
        {
            Debug.LogError("❌ [StatUnitCostLoader] CSV 파일이 비어있거나 Header만 존재합니다!");
            IsInitialized = true;
            return;
        }

        int parsedCount = 0;

        // Header(0번 줄) 스킵, 1번 줄부터 파싱
        // CSV 구조: StatId, statName, unitCost, unitDisplay, note
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] columns = line.Split(',');

            // 최소 3개 컬럼 필요 (StatId, statName, unitCost)
            if (columns.Length < 3)
            {
                Debug.LogWarning($"⚠️ [StatUnitCostLoader] Line {i + 1}: 컬럼 부족, 스킵");
                continue;
            }

            string statId = columns[0].Trim();
            string unitCostStr = columns[2].Trim();

            if (string.IsNullOrEmpty(statId)) continue;

            if (!float.TryParse(unitCostStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float unitCost))
            {
                Debug.LogWarning($"⚠️ [StatUnitCostLoader] Line {i + 1}: unitCost 파싱 실패 ({unitCostStr}), 스킵");
                continue;
            }

            if (Enum.TryParse<EStatType>(statId, out EStatType statType))
            {
                UnitCostTable[statType] = unitCost;
                parsedCount++;
                Debug.Log($"   ✅ {statType}: unitCost = {unitCost}");
            }
            else
            {
                Debug.LogWarning($"⚠️ [StatUnitCostLoader] 알 수 없는 StatId: {statId}, 스킵");
            }
        }

        IsInitialized = true;

        Debug.Log("========================================");
        Debug.Log($"✅ [StatUnitCostLoader] 초기화 완료: {parsedCount}개 단가 로드");
        Debug.Log("========================================");
    }

    #endregion

    #region 공개 API

    /// <summary>
    /// 스탯 단가 조회
    /// CSV에 정의되지 않은 스탯은 기본값(1.0) 반환
    /// </summary>
    public static float GetUnitCost(EStatType statType)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("⚠️ [StatUnitCostLoader] 초기화되지 않았습니다. Initialize() 호출 중...");
            Initialize();
        }

        if (UnitCostTable.TryGetValue(statType, out float cost))
            return cost;

        Debug.LogWarning($"⚠️ [StatUnitCostLoader] '{statType}' 단가 미정의 — 기본값 {DEFAULT_UNIT_COST} 사용");
        return DEFAULT_UNIT_COST;
    }

    #endregion
}
