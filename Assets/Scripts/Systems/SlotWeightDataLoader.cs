using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 📊 장비 슬롯별 가중치 데이터 로더
/// EquipmentSlotBudget.csv를 파싱하여 캐싱
/// [Single Source of Truth for Weights]
/// </summary>
public static class SlotWeightDataLoader
{
    #region 캐시 데이터
    
    /// <summary>
    /// 슬롯별 가중치 (Key: EquipmentSlot, Value: (주옵션 가중치, 부옵션 가중치))
    /// 예: WeightTable[EquipmentSlot.Gloves] = (1.0f, 0.3f)
    /// </summary>
    public static Dictionary<EquipmentSlot, (float mainWeight, float subWeight)> WeightTable { get; private set; }
    
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
            Debug.Log("✅ [SlotWeightDataLoader] 이미 초기화됨 (캐시 사용)");
            return;
        }
        
        Debug.Log("📊 [SlotWeightDataLoader] CSV 파싱 시작...");
        
        WeightTable = new Dictionary<EquipmentSlot, (float, float)>();
        
        // CSV 파일 로드
        TextAsset csvFile = Resources.Load<TextAsset>("Combat/CSV/EquipmentSlotBudget");
        
        if (csvFile == null)
        {
            Debug.LogError("❌ [SlotWeightDataLoader] EquipmentSlotBudget.csv를 찾을 수 없습니다!");
            Debug.LogError("   경로: Resources/Combat/CSV/EquipmentSlotBudget.csv");
            return;
        }
        
        // 라인 분할
        string[] lines = csvFile.text.Split('\n');
        
        if (lines.Length <= 1)
        {
            Debug.LogError("❌ [SlotWeightDataLoader] CSV 파일이 비어있거나 Header만 존재합니다!");
            return;
        }
        
        // ===== Header 파싱 (컬럼 인덱스 찾기) =====
        string[] headerColumns = lines[0].Split(',');
        int slotNameColIndex = -1;
        int mainWeightColIndex = -1;
        int subWeightColIndex = -1;
        
        for (int col = 0; col < headerColumns.Length; col++)
        {
            string columnName = headerColumns[col].Trim().ToLower();
            
            if (columnName == "slotname") slotNameColIndex = col;
            if (columnName == "mainstatweight" || columnName == "mainweight") mainWeightColIndex = col;
            if (columnName == "substatweight" || columnName == "subweight") subWeightColIndex = col;
        }
        
        if (slotNameColIndex < 0)
        {
            Debug.LogError("❌ [SlotWeightDataLoader] 'slotName' 컬럼을 찾을 수 없습니다!");
            return;
        }
        
        if (mainWeightColIndex < 0 || subWeightColIndex < 0)
        {
            Debug.LogWarning("⚠️ [SlotWeightDataLoader] mainStatWeight 또는 subStatWeight 컬럼이 없습니다. 기본값(1.0, 0.3) 사용");
        }
        
        Debug.Log($"   📍 slotName: Column {slotNameColIndex}");
        Debug.Log($"   📍 mainStatWeight: Column {mainWeightColIndex}");
        Debug.Log($"   📍 subStatWeight: Column {subWeightColIndex}");
        
        // ===== 데이터 행 파싱 =====
        int parsedCount = 0;
        
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            string[] columns = line.Split(',');
            
            if (columns.Length <= slotNameColIndex)
            {
                Debug.LogWarning($"⚠️ [SlotWeightDataLoader] Line {i+1}: 컬럼 부족, 스킵");
                continue;
            }
            
            // Slot 매핑
            string slotName = columns[slotNameColIndex].Trim();
            EquipmentSlot? slot = MapSlotNameToEnum(slotName);
            
            if (!slot.HasValue)
            {
                Debug.LogWarning($"⚠️ [SlotWeightDataLoader] Line {i+1}: 알 수 없는 슬롯명 ({slotName}), 스킵");
                continue;
            }
            
            // 가중치 파싱 (기본값: 1.0, 0.3)
            float mainWeight = 1.0f;
            float subWeight = 0.3f;
            
            if (mainWeightColIndex >= 0 && mainWeightColIndex < columns.Length)
            {
                if (float.TryParse(columns[mainWeightColIndex].Trim(), out float mw))
                    mainWeight = mw;
            }
            
            if (subWeightColIndex >= 0 && subWeightColIndex < columns.Length)
            {
                if (float.TryParse(columns[subWeightColIndex].Trim(), out float sw))
                    subWeight = sw;
            }
            
            // Dictionary에 저장
            WeightTable[slot.Value] = (mainWeight, subWeight);
            parsedCount++;
            
            Debug.Log($"✅ [SlotWeightDataLoader] {slot.Value}: Main={mainWeight}, Sub={subWeight}");
        }
        
        IsInitialized = true;
        
        Debug.Log("========================================");
        Debug.Log($"✅ [SlotWeightDataLoader] 초기화 완료: {parsedCount}개 슬롯 로드");
        Debug.Log("========================================");
    }
    
    #endregion
    
    #region 조회 메서드
    
    /// <summary>
    /// 슬롯의 주옵션 가중치 가져오기
    /// </summary>
    public static float GetMainWeight(EquipmentSlot slot)
    {
        // 초기화 확인
        if (!IsInitialized)
        {
            Debug.LogWarning("⚠️ [SlotWeightDataLoader] 초기화되지 않았습니다. Initialize() 호출 중...");
            Initialize();
        }
        
        if (WeightTable != null && WeightTable.ContainsKey(slot))
        {
            return WeightTable[slot].mainWeight;
        }
        
        Debug.LogWarning($"⚠️ [SlotWeightDataLoader] MainWeight 없음: {slot}, Fallback 1.0 반환");
        return 1.0f; // Fallback
    }
    
    /// <summary>
    /// 슬롯의 부옵션 가중치 가져오기
    /// </summary>
    public static float GetSubWeight(EquipmentSlot slot)
    {
        // 초기화 확인
        if (!IsInitialized)
        {
            Debug.LogWarning("⚠️ [SlotWeightDataLoader] 초기화되지 않았습니다. Initialize() 호출 중...");
            Initialize();
        }
        
        if (WeightTable != null && WeightTable.ContainsKey(slot))
        {
            return WeightTable[slot].subWeight;
        }
        
        Debug.LogWarning($"⚠️ [SlotWeightDataLoader] SubWeight 없음: {slot}, Fallback 0.3 반환");
        return 0.3f; // Fallback
    }
    
    /// <summary>
    /// TryGet 패턴
    /// </summary>
    public static bool TryGetWeights(EquipmentSlot slot, out float mainWeight, out float subWeight)
    {
        mainWeight = 1.0f;
        subWeight = 0.3f;
        
        if (!IsInitialized)
            Initialize();
        
        if (WeightTable != null && WeightTable.ContainsKey(slot))
        {
            var weights = WeightTable[slot];
            mainWeight = weights.mainWeight;
            subWeight = weights.subWeight;
            return true;
        }
        
        return false;
    }
    
    #endregion
    
    #region 헬퍼 메서드
    
    /// <summary>
    /// SlotName 문자열 → EquipmentSlot enum 변환
    /// </summary>
    private static EquipmentSlot? MapSlotNameToEnum(string slotName)
    {
        switch (slotName.ToLower().Replace("_", ""))
        {
            case "weapon": return EquipmentSlot.MainWeapon;
            case "helmet": return EquipmentSlot.Helmet;
            case "gloves": return EquipmentSlot.Gloves;
            case "armor": return EquipmentSlot.Armor;
            case "boots": return EquipmentSlot.Boots;
            case "belt": return EquipmentSlot.Belt;
            case "necklace": return EquipmentSlot.Necklace;
            case "ring1": return EquipmentSlot.Ring1;
            case "ring2": return EquipmentSlot.Ring2;
            default:
                return null;
        }
    }
    
    /// <summary>
    /// Grade 문자열 → ItemGrade enum 변환
    /// </summary>
    private static ItemGrade? ParseGrade(string gradeStr)
    {
        if (string.IsNullOrEmpty(gradeStr))
            return null;
        
        gradeStr = gradeStr.ToUpper().Trim();
        
        if (Enum.TryParse<ItemGrade>(gradeStr, out ItemGrade result))
        {
            return result;
        }
        
        return null;
    }
    
    #endregion
}
