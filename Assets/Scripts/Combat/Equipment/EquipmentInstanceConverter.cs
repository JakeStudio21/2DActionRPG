using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🔄 EquipmentInstance ↔ ItemInstanceData 변환 헬퍼
/// 동적 스탯 데이터를 저장/로드 가능한 형태로 변환
/// </summary>
public static class EquipmentInstanceConverter
{
    /// <summary>
    /// Dictionary → List 변환 (저장용)
    /// EquipmentInstance.randomSubStats → ItemInstanceData.randomSubStats
    /// </summary>
    /// <param name="subStats">런타임 Dictionary</param>
    /// <returns>직렬화 가능한 List</returns>
    public static List<SubStatSaveData> ConvertToSaveData(Dictionary<EStatType, float> subStats)
    {
        if (subStats == null || subStats.Count == 0)
        {
            return new List<SubStatSaveData>();
        }
        
        var result = new List<SubStatSaveData>();
        foreach (var kvp in subStats)
        {
            result.Add(new SubStatSaveData
            {
                statType = kvp.Key,
                value = kvp.Value
            });
        }
        
        Debug.Log($"🔄 [Converter] Dictionary → List 변환: {subStats.Count}개 → {result.Count}개");
        return result;
    }
    
    /// <summary>
    /// List → Dictionary 변환 (로드용)
    /// ItemInstanceData.randomSubStats → EquipmentInstance.randomSubStats
    /// </summary>
    /// <param name="subStats">직렬화된 List</param>
    /// <returns>런타임 Dictionary</returns>
    public static Dictionary<EStatType, float> ConvertToDictionary(List<SubStatSaveData> subStats)
    {
        if (subStats == null || subStats.Count == 0)
        {
            return new Dictionary<EStatType, float>();
        }
        
        var result = new Dictionary<EStatType, float>();
        foreach (var data in subStats)
        {
            // 중복 키 방지
            if (!result.ContainsKey(data.statType))
            {
                result[data.statType] = data.value;
            }
            else
            {
                Debug.LogWarning($"⚠️ [Converter] 중복된 스탯 타입: {data.statType}, 건너뜀");
            }
        }
        
        Debug.Log($"🔄 [Converter] List → Dictionary 변환: {subStats.Count}개 → {result.Count}개");
        return result;
    }
    
    /// <summary>
    /// ItemInstanceData에 동적 스탯 적용
    /// DynamicEquipmentGenerator 결과를 ItemInstanceData에 저장
    /// </summary>
    /// <param name="instanceData">저장할 대상</param>
    /// <param name="generatedInstance">생성된 장비 인스턴스</param>
    public static void ApplyDynamicStats(ItemInstanceData instanceData, EquipmentInstance generatedInstance)
    {
        if (instanceData == null)
        {
            Debug.LogError("❌ [Converter] instanceData가 null입니다!");
            return;
        }
        
        if (generatedInstance == null)
        {
            Debug.LogError("❌ [Converter] generatedInstance가 null입니다!");
            return;
        }
        
        // 주옵션 복사
        instanceData.finalMainStatValue = generatedInstance.finalMainStatValue;
        
        // 부옵션 복사 (Dictionary → List)
        instanceData.randomSubStats = ConvertToSaveData(generatedInstance.randomSubStats);
        
        Debug.Log($"✅ [Converter] 동적 스탯 적용 완료:");
        Debug.Log($"   - 주옵션: {instanceData.finalMainStatValue}");
        Debug.Log($"   - 부옵션: {instanceData.randomSubStats.Count}개");
    }
    
    /// <summary>
    /// ItemInstanceData에서 EquipmentInstance로 동적 스탯 복원
    /// 로드 시 또는 런타임 인스턴스 생성 시 사용
    /// </summary>
    /// <param name="equipmentInstance">복원할 대상</param>
    /// <param name="instanceData">저장된 데이터</param>
    public static void RestoreDynamicStats(EquipmentInstance equipmentInstance, ItemInstanceData instanceData)
    {
        if (equipmentInstance == null)
        {
            Debug.LogError("❌ [Converter] equipmentInstance가 null입니다!");
            return;
        }
        
        if (instanceData == null)
        {
            Debug.LogError("❌ [Converter] instanceData가 null입니다!");
            return;
        }
        
        // 주옵션 복원
        equipmentInstance.finalMainStatValue = instanceData.finalMainStatValue;
        
        // 부옵션 복원 (List → Dictionary)
        equipmentInstance.randomSubStats = ConvertToDictionary(instanceData.randomSubStats);
        
        Debug.Log($"✅ [Converter] 동적 스탯 복원 완료:");
        Debug.Log($"   - 주옵션: {equipmentInstance.finalMainStatValue}");
        Debug.Log($"   - 부옵션: {equipmentInstance.randomSubStats.Count}개");
    }
}

