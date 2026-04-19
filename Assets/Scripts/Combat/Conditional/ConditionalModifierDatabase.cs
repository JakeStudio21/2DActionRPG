using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 조건부 모디파이어 데이터베이스
/// ConditionalModifier.csv를 파싱하고 Phase별로 그룹화하여 캐싱
/// ⚡ Static 클래스로 GC 최소화
/// ⚙️ Phase 4: ConditionalModifier 시스템
/// </summary>
public static class ConditionalModifierDatabase
{
    #region 캐시
    
    private static List<ConditionalModifier> allModifiers;
    private static Dictionary<int, List<ConditionalModifier>> modifiersByPhase;
    private static bool isInitialized = false;
    
    #endregion
    
    #region 초기화
    
    /// <summary>
    /// 게임 시작 시 자동 초기화
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (isInitialized) return;
        
        allModifiers = new List<ConditionalModifier>();
        modifiersByPhase = new Dictionary<int, List<ConditionalModifier>>();
        
        LoadFromCSV();
        
        isInitialized = true;
        Dbg.Log($"[ConditionalModifierDatabase] 초기화 완료. 총 {allModifiers.Count}개 모디파이어 로드됨.");
    }
    
    /// <summary>
    /// CSV 파일 로드 및 파싱
    /// </summary>
    private static void LoadFromCSV()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("Combat/CSV/ConditionalModifier");
        
        if (csvFile == null)
        {
            Debug.LogError("[ConditionalModifierDatabase] ConditionalModifier.csv 파일을 찾을 수 없습니다!");
            return;
        }
        
        ParseCSV(csvFile.text);
        GroupByPhase();
    }
    
    /// <summary>
    /// CSV 텍스트 파싱
    /// </summary>
    private static void ParseCSV(string csvText)
    {
        string[] lines = csvText.Split('\n');
        
        // 첫 줄은 헤더, 건너뜀
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            string[] values = line.Split(',');
            if (values.Length < 11) // 최소 11개 컬럼 필요
            {
                Debug.LogWarning($"[ConditionalModifierDatabase] CSV 파싱 오류 (Line {i+1}): 컬럼 부족");
                continue;
            }
            
            try
            {
                // CSV 컬럼 파싱 (순서: ModifierId,DisplayName,ValueType,Unit,ApplyPhase,TargetScope,ConditionType,ConditionParam,EffectType,Value,Notes)
                string modifierId = values[0].Trim();
                string displayName = values[1].Trim();
                string valueTypeStr = values[2].Trim();
                string unitStr = values[3].Trim();
                string applyPhaseStr = values[4].Trim();
                string targetScope = values[5].Trim();
                string conditionTypeStr = values[6].Trim();
                string conditionParam = values[7].Trim();
                string effectTypeStr = values[8].Trim();
                string valueStr = values[9].Trim();
                string notes = values[10].Trim();
                
                // Enum 변환
                StatValueType valueType = ParseValueType(valueTypeStr);
                StatUnit unit = ParseUnit(unitStr);
                int applyPhase = ParseApplyPhase(applyPhaseStr);
                EConditionType conditionType = ParseConditionType(conditionTypeStr);
                EEffectType effectType = ParseEffectType(effectTypeStr);
                float value = float.Parse(valueStr);
                
                // ConditionalModifier 생성
                var modifier = new ConditionalModifier(
                    modifierId,
                    displayName,
                    valueType,
                    unit,
                    conditionType,
                    conditionParam,
                    effectType,
                    value,
                    applyPhase,
                    targetScope,
                    notes,
                    source: "CSV"
                );
                
                allModifiers.Add(modifier);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConditionalModifierDatabase] CSV 파싱 오류 (Line {i+1}): {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Phase별로 그룹화 (빠른 조회를 위해)
    /// </summary>
    private static void GroupByPhase()
    {
        modifiersByPhase.Clear();
        
        foreach (var modifier in allModifiers)
        {
            if (!modifiersByPhase.ContainsKey(modifier.applyPhase))
            {
                modifiersByPhase[modifier.applyPhase] = new List<ConditionalModifier>();
            }
            
            modifiersByPhase[modifier.applyPhase].Add(modifier);
        }
        
    }
    
    #endregion
    
    #region 접근자
    
    /// <summary>
    /// 모든 조건부 모디파이어 가져오기
    /// </summary>
    public static List<ConditionalModifier> GetAll()
    {
        if (!isInitialized) Initialize();
        return allModifiers;
    }
    
    /// <summary>
    /// 특정 Phase에 해당하는 모디파이어만 가져오기
    /// ⚡ GC 없음: 캐시된 리스트 직접 반환
    /// </summary>
    /// <param name="phase">Phase 번호 (3 = Phase3_Modifier, 5 = Phase5_Defense 등)</param>
    /// <returns>해당 Phase의 모디파이어 리스트 (없으면 빈 리스트)</returns>
    public static List<ConditionalModifier> GetModifiersByPhase(int phase)
    {
        if (!isInitialized) Initialize();
        
        if (modifiersByPhase.TryGetValue(phase, out var modifiers))
        {
            return modifiers;
        }
        
        // 빈 리스트 반환 (GC 없음)
        return EmptyList;
    }
    
    /// <summary>
    /// Phase 문자열로 검색 (예: "Phase3_Modifier")
    /// </summary>
    public static List<ConditionalModifier> GetModifiersByPhase(string phaseString)
    {
        int phase = ParseApplyPhase(phaseString);
        return GetModifiersByPhase(phase);
    }
    
    /// <summary>
    /// ModifierId로 단일 ConditionalModifier 조회
    /// ⚙️ Phase 4-C: 룬 시스템에서 사용
    /// </summary>
    /// <param name="modifierId">조회할 ModifierId (예: "BOSS_DMG_UP")</param>
    /// <returns>찾은 ConditionalModifier 또는 null</returns>
    public static ConditionalModifier GetModifierById(string modifierId)
    {
        if (!isInitialized) Initialize();
        
        if (string.IsNullOrEmpty(modifierId))
            return null;
        
        // allModifiers에서 선형 검색 (O(n), but 모디파이어 개수가 적으므로 괜찮음)
        return allModifiers.FirstOrDefault(m => m.modifierId == modifierId);
    }
    
    // 빈 리스트 재사용 (GC 최적화)
    private static readonly List<ConditionalModifier> EmptyList = new List<ConditionalModifier>();
    
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
    
    private static int ParseApplyPhase(string str)
    {
        // "Phase3_Modifier" → 3
        // "Phase5_Defense" → 5
        // "Phase0_Event" → 0
        if (str.StartsWith("Phase") && str.Length > 5)
        {
            char phaseChar = str[5];
            if (char.IsDigit(phaseChar))
            {
                return phaseChar - '0';
            }
        }
        
        // Runtime = 99
        if (str == "Runtime") return 99;
        
        return 0;
    }
    
    private static EConditionType ParseConditionType(string str)
    {
        if (string.IsNullOrEmpty(str))
            return EConditionType.None;
        
        if (Enum.TryParse<EConditionType>(str, out var result))
            return result;
        
        Debug.LogWarning($"[ConditionalModifierDatabase] 알 수 없는 ConditionType: {str}");
        return EConditionType.None;
    }
    
    private static EEffectType ParseEffectType(string str)
    {
        if (string.IsNullOrEmpty(str))
            return EEffectType.None;
        
        if (Enum.TryParse<EEffectType>(str, out var result))
            return result;
        
        Debug.LogWarning($"[ConditionalModifierDatabase] 알 수 없는 EffectType: {str}");
        return EEffectType.None;
    }
    
    #endregion
    
}

