using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CSV 기반 스탯 정의 시스템
/// StatDefinition.csv 파싱 및 캐싱
/// </summary>
public static class StatDefinitions
{
    #region 캐시
    
    private static Dictionary<EStatType, StatDefinition> definitions;
    private static bool isInitialized = false;
    
    #endregion
    
    #region 초기화
    
    /// <summary>
    /// CSV 로드 및 파싱
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (isInitialized) return;
        
        definitions = new Dictionary<EStatType, StatDefinition>();
        
        // CSV 파일 로드
        TextAsset csvFile = Resources.Load<TextAsset>("Combat/CSV/StatDefinition");
        if (csvFile == null)
        {
            Debug.LogError("[StatDefinitions] StatDefinition.csv 파일을 찾을 수 없습니다!");
            return;
        }
        
        // 파싱
        ParseCSV(csvFile.text);
        
        isInitialized = true;
        Debug.Log($"[StatDefinitions] {definitions.Count}개 스탯 정의 로드 완료");
    }
    
    /// <summary>
    /// CSV 파싱
    /// </summary>
    private static void ParseCSV(string csvText)
    {
        string[] lines = csvText.Split('\n');
        
        // 첫 줄은 헤더
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            string[] values = line.Split(',');
            if (values.Length < 8) continue;
            
            try
            {
                // CSV 컬럼 파싱
                string statId = values[0].Trim();
                string displayName = values[1].Trim();
                string valueTypeStr = values[2].Trim();
                string unitStr = values[3].Trim();
                string stackRuleStr = values[4].Trim();
                string applyPhaseStr = values[5].Trim();
                string targetScopeStr = values[6].Trim();
                string capValueStr = values[7].Trim();
                
                // EStatType 변환
                EStatType statType = statId.ToStatType();
                if (statType == EStatType.None) continue;
                
                // StatDefinition 생성
                var definition = new StatDefinition
                {
                    statType = statType,
                    statId = statId,
                    displayName = displayName,
                    valueType = ParseValueType(valueTypeStr),
                    unit = ParseUnit(unitStr),
                    stackRule = ParseStackRule(stackRuleStr),
                    applyPhase = ParseApplyPhase(applyPhaseStr),
                    targetScope = targetScopeStr,
                    capValue = ParseCapValue(capValueStr)
                };
                
                definitions[statType] = definition;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StatDefinitions] CSV 파싱 오류 (Line {i}): {e.Message}");
            }
        }
    }
    
    #endregion
    
    #region 접근자
    
    /// <summary>
    /// 스탯 정의 가져오기
    /// </summary>
    public static StatDefinition Get(EStatType statType)
    {
        if (!isInitialized) Initialize();
        
        if (definitions.TryGetValue(statType, out var definition))
        {
            return definition;
        }
        
        Debug.LogWarning($"[StatDefinitions] {statType} 정의를 찾을 수 없습니다.");
        return null;
    }
    
    /// <summary>
    /// 모든 정의 가져오기
    /// </summary>
    public static IEnumerable<StatDefinition> GetAll()
    {
        if (!isInitialized) Initialize();
        return definitions.Values;
    }
    
    #endregion
    
    #region 파싱 헬퍼
    
    private static StatValueType ParseValueType(string str)
    {
        if (Enum.TryParse<StatValueType>(str, out var result))
            return result;
        return StatValueType.Float;
    }
    
    private static StatUnit ParseUnit(string str)
    {
        if (Enum.TryParse<StatUnit>(str, out var result))
            return result;
        return StatUnit.Flat;
    }
    
    private static StatStackRule ParseStackRule(string str)
    {
        if (Enum.TryParse<StatStackRule>(str, out var result))
            return result;
        return StatStackRule.Add;
    }
    
    private static int ParseApplyPhase(string str)
    {
        // "Phase1_AttackBuild" → 1
        // "Phase3_Modifier" → 3
        if (str.StartsWith("Phase") && str.Length > 5)
        {
            char phaseChar = str[5];
            if (char.IsDigit(phaseChar))
            {
                return phaseChar - '0';
            }
        }
        
        // Fallback: Runtime = 99
        if (str == "Runtime") return 99;
        
        return 0;
    }
    
    private static float? ParseCapValue(string str)
    {
        if (string.IsNullOrEmpty(str)) return null;
        if (float.TryParse(str, out float value))
            return value;
        return null;
    }
    
    #endregion
}

/// <summary>
/// 스탯 정의 (CSV 1줄)
/// </summary>
public class StatDefinition
{
    public EStatType statType;
    public string statId;
    public string displayName;
    public StatValueType valueType;
    public StatUnit unit;
    public StatStackRule stackRule;
    public int applyPhase;
    public string targetScope;
    public float? capValue;
}

