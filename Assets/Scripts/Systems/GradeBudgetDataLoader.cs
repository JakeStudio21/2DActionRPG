using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 📊 등급+슬롯별 예산 데이터 로더
/// GradeSlotBudgetWeighted.csv를 파싱하여 캐싱
/// [Single Source of Truth for Budget]
/// </summary>
public static class GradeBudgetDataLoader
{
    #region 캐시 데이터
    
    /// <summary>
    /// 이중 Dictionary: Grade → Slot → Budget
    /// 예: BudgetTable[ItemGrade.A][EquipmentSlot.Gloves] = 93.75
    /// </summary>
    public static Dictionary<ItemGrade, Dictionary<EquipmentSlot, float>> BudgetTable { get; private set; }
    
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
            Debug.Log("✅ [GradeBudgetDataLoader] 이미 초기화됨 (캐시 사용)");
            return;
        }
        
        Debug.Log("📊 [GradeBudgetDataLoader] CSV 파싱 시작...");
        
        BudgetTable = new Dictionary<ItemGrade, Dictionary<EquipmentSlot, float>>();
        
        // CSV 파일 로드
        TextAsset csvFile = Resources.Load<TextAsset>("Combat/CSV/GradeSlotBudgetWeighted");
        
        if (csvFile == null)
        {
            Debug.LogError("❌ [GradeBudgetDataLoader] GradeSlotBudgetWeighted.csv를 찾을 수 없습니다!");
            Debug.LogError("   경로: Resources/Combat/CSV/GradeSlotBudgetWeighted.csv");
            return;
        }
        
        // 라인 분할
        string[] lines = csvFile.text.Split('\n');
        
        if (lines.Length <= 1)
        {
            Debug.LogError("❌ [GradeBudgetDataLoader] CSV 파일이 비어있거나 Header만 존재합니다!");
            return;
        }
        
        // ===== Header 파싱 (컬럼명 → EquipmentSlot 매핑) =====
        string[] headerColumns = lines[0].Split(',');
        Dictionary<int, EquipmentSlot> columnIndexToSlot = new Dictionary<int, EquipmentSlot>();
        
        for (int col = 0; col < headerColumns.Length; col++)
        {
            string columnName = headerColumns[col].Trim().ToLower();
            EquipmentSlot? slot = MapColumnToSlot(columnName);
            
            if (slot.HasValue)
            {
                columnIndexToSlot[col] = slot.Value;
                Debug.Log($"   📍 Column {col} ({headerColumns[col]}) → {slot.Value}");
            }
        }
        
        if (columnIndexToSlot.Count == 0)
        {
            Debug.LogError("❌ [GradeBudgetDataLoader] 슬롯 컬럼을 찾을 수 없습니다!");
            return;
        }
        
        // ===== 데이터 행 파싱 =====
        int parsedCount = 0;
        
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            string[] columns = line.Split(',');
            
            // Grade 파싱 (0번 컬럼)
            ItemGrade? grade = ParseGrade(columns[0].Trim());
            
            if (!grade.HasValue)
            {
                Debug.LogWarning($"⚠️ [GradeBudgetDataLoader] Line {i+1}: Grade 파싱 실패 ({columns[0]}), 스킵");
                continue;
            }
            
            // 슬롯별 예산 파싱
            Dictionary<EquipmentSlot, float> slotBudgets = new Dictionary<EquipmentSlot, float>();
            
            foreach (var kvp in columnIndexToSlot)
            {
                int colIndex = kvp.Key;
                EquipmentSlot slot = kvp.Value;
                
                if (colIndex < columns.Length && float.TryParse(columns[colIndex].Trim(), out float budget))
                {
                    slotBudgets[slot] = budget;
                }
                else
                {
                    Debug.LogWarning($"⚠️ [GradeBudgetDataLoader] {grade} {slot}: 예산 파싱 실패");
                }
            }
            
            BudgetTable[grade.Value] = slotBudgets;
            parsedCount++;
            
            Debug.Log($"✅ [GradeBudgetDataLoader] {grade.Value}: {slotBudgets.Count}개 슬롯 로드");
        }
        
        IsInitialized = true;
        
        Debug.Log("========================================");
        Debug.Log($"✅ [GradeBudgetDataLoader] 초기화 완료: {parsedCount}개 등급 로드");
        Debug.Log("========================================");
    }
    
    #endregion
    
    #region 조회 메서드
    
    /// <summary>
    /// 특정 등급+슬롯의 예산 가져오기
    /// </summary>
    public static float GetBudget(ItemGrade grade, EquipmentSlot slot)
    {
        // 초기화 확인
        if (!IsInitialized)
        {
            Debug.LogWarning("⚠️ [GradeBudgetDataLoader] 초기화되지 않았습니다. Initialize() 호출 중...");
            Initialize();
        }
        
        // 조회
        if (BudgetTable != null && 
            BudgetTable.ContainsKey(grade) && 
            BudgetTable[grade].ContainsKey(slot))
        {
            float budget = BudgetTable[grade][slot];
            Debug.Log($"💰 [GradeBudgetDataLoader] {grade} {slot} = {budget:F2}");
            return budget;
        }
        
        Debug.LogWarning($"⚠️ [GradeBudgetDataLoader] 예산 없음: {grade} {slot}, Fallback 100 반환");
        return 100f; // Fallback
    }
    
    /// <summary>
    /// TryGet 패턴
    /// </summary>
    public static bool TryGetBudget(ItemGrade grade, EquipmentSlot slot, out float budget)
    {
        budget = 0f;
        
        if (!IsInitialized)
            Initialize();
        
        if (BudgetTable != null && 
            BudgetTable.ContainsKey(grade) && 
            BudgetTable[grade].ContainsKey(slot))
        {
            budget = BudgetTable[grade][slot];
            return true;
        }
        
        return false;
    }
    
    #endregion
    
    #region 헬퍼 메서드
    
    /// <summary>
    /// 컬럼명 → EquipmentSlot 매핑
    /// </summary>
    private static EquipmentSlot? MapColumnToSlot(string columnName)
    {
        switch (columnName)
        {
            case "weaponbudget": return EquipmentSlot.MainWeapon;
            case "helmetbudget": return EquipmentSlot.Helmet;
            case "glovesbudget": return EquipmentSlot.Gloves;
            case "armorbudget": return EquipmentSlot.Armor;
            case "bootsbudget": return EquipmentSlot.Boots;
            case "beltbudget": return EquipmentSlot.Belt;
            case "necklacebudget": return EquipmentSlot.Necklace;
            case "ring1budget": return EquipmentSlot.Ring1;
            case "ring2budget": return EquipmentSlot.Ring2;
            default: return null; // 매핑 없음 (grade, multiplier, totalBudget 등)
        }
    }
    
    /// <summary>
    /// Grade 문자열 → ItemGrade enum 변환
    /// </summary>
    private static ItemGrade? ParseGrade(string gradeStr)
    {
        if (string.IsNullOrEmpty(gradeStr))
            return null;
        
        // 대문자로 변환 후 파싱
        gradeStr = gradeStr.ToUpper().Trim();
        
        if (Enum.TryParse<ItemGrade>(gradeStr, out ItemGrade result))
        {
            return result;
        }
        
        return null;
    }
    
    #endregion
}
