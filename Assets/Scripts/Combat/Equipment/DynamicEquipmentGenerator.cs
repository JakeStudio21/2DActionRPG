using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🎲 장비 동적 생성 엔진
/// 등급별 배율과 예산 시스템을 기반으로 주옵션/부옵션 랜덤 생성
/// [Stage 2 Implementation - 2026.02.24]
/// </summary>
public static class DynamicEquipmentGenerator
{
    #region 등급별 설정
    
    
    /// <summary>
    /// 등급별 스탯 배율 및 부옵션 개수
    /// </summary>
    private struct GradeConfig
    {
        public int subStatCount;    // 부옵션 개수 (GradeSubStatCount.csv 기반)
        
        public GradeConfig(int subStatCount)
        {
            this.subStatCount = subStatCount;
        }
    }
    
    /// <summary>
    /// 등급별 부옵션 개수 테이블
    /// 수치 기준은 Resources/Combat/CSV/GradeSubStatCount.csv 참조
    /// </summary>
    private static readonly Dictionary<ItemGrade, GradeConfig> gradeConfigs = new Dictionary<ItemGrade, GradeConfig>
    {
        { ItemGrade.TR, new GradeConfig(4) },
        { ItemGrade.EX, new GradeConfig(3) },
        { ItemGrade.SS, new GradeConfig(3) },
        { ItemGrade.S,  new GradeConfig(2) },
        { ItemGrade.A,  new GradeConfig(2) },
        { ItemGrade.B,  new GradeConfig(1) },
        { ItemGrade.C,  new GradeConfig(0) },
        { ItemGrade.D,  new GradeConfig(0) }
    };
    
    #endregion
    
    #region 핵심 생성 로직
    
    /// <summary>
    /// 장비 인스턴스 동적 생성
    /// </summary>
    /// <param name="baseData">템플릿 장비 데이터</param>
    /// <param name="grade">생성할 등급 (템플릿과 다를 수 있음)</param>
    /// <returns>주옵션/부옵션이 랜덤 생성된 장비 인스턴스</returns>
    public static EquipmentInstance Generate(EquipmentData baseData, ItemGrade grade)
    {
        // ===== Step 1: 유효성 검사 =====
        if (baseData == null)
        {
            Debug.LogError("❌ [EquipmentGenerator] baseData가 null입니다!");
            return null;
        }
        
        if (!gradeConfigs.ContainsKey(grade))
        {
            Debug.LogError($"❌ [EquipmentGenerator] 알 수 없는 등급: {grade}");
            return null;
        }
        
        Debug.Log("========================================");
        Debug.Log($"📊 [EquipmentGenerator] {baseData.equipmentName} ({grade}) 생성 시작");
        Debug.Log("========================================");
        
        // ===== Step 2: 등급별 설정 로드 (부옵션 개수) =====
        GradeConfig config = gradeConfigs[grade];
        int subStatCount = config.subStatCount;
        
        Debug.Log($"⚙️ [EquipmentGenerator] 등급 설정:");
        Debug.Log($"   - 부옵션 개수: {subStatCount}개");
        
        // ===== Step 3: 스탯 풀 가져오기 =====
        var (mainStatType, subStatPool) = GetStatPool(baseData);
        
        if (mainStatType == EStatType.None)
        {
            Debug.LogError($"❌ [EquipmentGenerator] {baseData.equipmentName}: MainStat이 None입니다!");
            return null;
        }
        
        Debug.Log($"🎲 [EquipmentGenerator] 스탯 풀:");
        Debug.Log($"   - 주옵션: {mainStatType}");
        Debug.Log($"   - 부옵션 후보: {subStatPool.Count}개");
        
        // ===== Step 4: CSV 기반 예산 및 가중치 로드 (Single Source of Truth) =====
        // ⚠️ 중요: 모든 예산/가중치는 CSV에서 관리됨 (SO 하드코딩 제거)
        float totalBudget = GradeBudgetDataLoader.GetBudget(grade, baseData.equipmentSlot);
        float mainWeight = SlotWeightDataLoader.GetMainWeight(baseData.equipmentSlot);
        float subWeight = SlotWeightDataLoader.GetSubWeight(baseData.equipmentSlot);
        
        // 독립 예산 분배 (Option 3)
        float mainStatBudget = totalBudget * mainWeight;
        float subStatBudgetPerSlot = subStatCount > 0 ? totalBudget * subWeight : 0f;
        
        Debug.Log($"💰 [EquipmentGenerator] CSV 기반 예산:");
        Debug.Log($"   - 등급: {grade}, 슬롯: {baseData.equipmentSlot}");
        Debug.Log($"   - 총 예산: {totalBudget:F2} (CSV 직접 로드)");
        Debug.Log($"   - 가중치: 주옵션 {mainWeight} / 부옵션 {subWeight} (CSV 로드)");
        Debug.Log($"   - 주옵션 예산: {mainStatBudget:F2}");
        Debug.Log($"   - 부옵션 예산/슬롯: {subStatBudgetPerSlot:F2}");
        
        // ===== Step 5: 인스턴스 생성 =====
        EquipmentInstance newInstance = new EquipmentInstance(
            ItemInstanceID.Generate(),
            baseData,
            enhanceLevel: 0,
            isBound: false
        );
        
        // ===== Step 6: 주옵션 생성 =====
        float mainRandomFactor = UnityEngine.Random.Range(0.9f, 1.1f);
        float mainAdjustedBudget = mainStatBudget * mainRandomFactor;
        float mainUnitCost = GetStatUnitCost(mainStatType);
        float mainRawValue = mainAdjustedBudget / mainUnitCost;
        newInstance.finalMainStatValue = Mathf.Round(mainRawValue * 10f) / 10f;
        
        Debug.Log($"⚔️ [EquipmentGenerator] 주옵션 생성:");
        Debug.Log($"   - 타입: {mainStatType}");
        Debug.Log($"   - 예산: {mainStatBudget:F1} → {mainAdjustedBudget:F1} (×{mainRandomFactor:F2})");
        Debug.Log($"   - 단가: {mainUnitCost}");
        Debug.Log($"   - 최종: {newInstance.finalMainStatValue} ({mainRawValue:F2} → 반올림)");
        
        // ===== Step 7: 부옵션 생성 =====
        newInstance.randomSubStats = new Dictionary<EStatType, float>();
        
        if (subStatCount > 0)
        {
            // 부옵션 풀 복사 (원본 보호)
            List<EStatType> availablePool = new List<EStatType>(subStatPool);
            
            if (availablePool.Count < subStatCount)
            {
                Debug.LogWarning($"⚠️ [EquipmentGenerator] 부옵션 풀 부족! 요구: {subStatCount}, 풀: {availablePool.Count}");
                subStatCount = availablePool.Count; // 실제 개수로 조정
            }
            
            Debug.Log($"🎲 [EquipmentGenerator] 부옵션 생성: {subStatCount}개");
            
            // 중복 없이 랜덤 추출
            for (int i = 0; i < subStatCount; i++)
            {
                // 랜덤 인덱스 선택
                int randomIndex = UnityEngine.Random.Range(0, availablePool.Count);
                EStatType selectedStat = availablePool[randomIndex];
                availablePool.RemoveAt(randomIndex); // 중복 방지
                
                // 랜덤 범위 적용 (0.8 ~ 1.2)
                float randomFactor = UnityEngine.Random.Range(0.8f, 1.2f);
                float adjustedBudget = subStatBudgetPerSlot * randomFactor;
                
                // 단가로 나누기
                float unitCost = GetStatUnitCost(selectedStat);
                float rawValue = adjustedBudget / unitCost;
                
                // 반올림 (소수점 첫째 자리)
                float finalValue = Mathf.Round(rawValue * 10f) / 10f;
                
                // Dictionary에 추가
                newInstance.randomSubStats[selectedStat] = finalValue;
                
                Debug.Log($"   [{i+1}] {selectedStat}: {finalValue} (예산 {adjustedBudget:F1} ÷ 단가 {unitCost})");
            }
        }
        else
        {
            Debug.Log($"🎲 [EquipmentGenerator] 부옵션 없음 (C/D 등급)");
        }
        
        // ===== Step 8: 캐시 무효화 및 반환 =====
        newInstance.InvalidateCache();
        
        Debug.Log("========================================");
        Debug.Log($"✅ [EquipmentGenerator] {baseData.equipmentName} ({grade}) 생성 완료!");
        Debug.Log($"   - 주옵션: {mainStatType} = {newInstance.finalMainStatValue}");
        Debug.Log($"   - 부옵션: {newInstance.randomSubStats.Count}개");
        Debug.Log("========================================");
        
        return newInstance;
    }
    
    #endregion
    
    #region 스탯 풀 로드
    
    /// <summary>
    /// 주옵션/부옵션 풀 가져오기
    /// - 주옵션: StatPoolDataLoader(CSV) → 하드코딩 Fallback 순서 (baseStats 의존성 없음)
    /// - 부옵션: availableSubStats 수동 설정 → StatPoolDataLoader(CSV) → 하드코딩 Fallback 순서
    /// </summary>
    private static (EStatType MainStat, List<EStatType> SubStats) GetStatPool(EquipmentData baseData)
    {
        string poolId = baseData.equipmentSlot.ToString();
        
        // ===== 주옵션: 항상 StatPoolDataLoader 기준 =====
        EStatType mainStat;
        List<EStatType> csvSubStats;
        
        if (StatPoolDataLoader.TryGetStatPool(poolId, out EStatType loadedMainStat, out List<EStatType> loadedSubStats))
        {
            mainStat = loadedMainStat;
            csvSubStats = loadedSubStats;
        }
        else
        {
            Debug.LogWarning($"⚠️ [EquipmentGenerator] {baseData.equipmentName}: StatPoolDataLoader 없음, 하드코딩 사용");
            var defaultPool = GetDefaultStatPool(baseData.equipmentType, baseData.WeaponType);
            mainStat = defaultPool.Item1;
            csvSubStats = defaultPool.Item2;
        }
        
        // ===== 부옵션: availableSubStats 우선, 없으면 CSV 사용 =====
        List<EStatType> subStats;
        
        if (baseData.availableSubStats != null && baseData.availableSubStats.Count > 0)
        {
            subStats = new List<EStatType>(baseData.availableSubStats);
            Debug.Log($"📋 [EquipmentGenerator] {baseData.equipmentName}: 부옵션 수동 풀 사용 ({subStats.Count}개)");
        }
        else
        {
            subStats = csvSubStats;
            Debug.Log($"🤖 [EquipmentGenerator] {baseData.equipmentName}: 부옵션 CSV 풀 사용 (키: {poolId}, {subStats.Count}개)");
        }
        
        Debug.Log($"   - MainStat: {mainStat}");
        Debug.Log($"   - SubStats: {subStats.Count}개");
        
        return (mainStat, subStats);
    }
    
    /// <summary>
    /// 장비 타입별 기본 풀 (Fallback)
    /// </summary>
    private static (EStatType, List<EStatType>) GetDefaultStatPool(EquipmentType equipType, WeaponType weaponType)
    {
        // 무기
        if (equipType == EquipmentType.Weapon)
        {
            switch (weaponType)
            {
                case WeaponType.Sword:
                    return (EStatType.ATK_FLAT, new List<EStatType> {
                        EStatType.CRIT_DMG, EStatType.DEF_FLAT, EStatType.HP_FLAT, EStatType.ASPD
                    });
                
                case WeaponType.Bow:
                    return (EStatType.ATK_FLAT, new List<EStatType> {
                        EStatType.CRIT_RATE, EStatType.ASPD, EStatType.MOVE_SPEED, EStatType.CRIT_DMG
                    });
                
                case WeaponType.Magic:
                    return (EStatType.ATK_FLAT, new List<EStatType> {
                        EStatType.SKILL_DMG_PERCENT, EStatType.COOLDOWN_REDUCTION, EStatType.HP_FLAT, EStatType.CRIT_RATE
                    });
            }
        }
        
        // 방어구
        if (equipType == EquipmentType.Armor)
        {
            return (EStatType.DEF_FLAT, new List<EStatType> {
                EStatType.HP_FLAT, EStatType.DAMAGE_REDUCTION_PERCENT, EStatType.STATUS_RESIST_ALL
            });
        }
        
        // 악세서리
        if (equipType == EquipmentType.Accessory)
        {
            return (EStatType.HP_FLAT, new List<EStatType> {
                EStatType.ATK_FLAT, EStatType.STATUS_RESIST_ALL, EStatType.HP_REGEN, EStatType.EXP_GAIN_PERCENT
            });
        }
        
        // 기본값
        Debug.LogError($"❌ [EquipmentGenerator] 알 수 없는 장비 타입: {equipType}/{weaponType}");
        return (EStatType.ATK_FLAT, new List<EStatType> { EStatType.HP_FLAT });
    }
    
    #endregion
    
    #region 헬퍼 메서드
    
    /// <summary>
    /// 스탯 단가 조회 — StatUnitCost.csv 기반 (StatUnitCostLoader 위임)
    /// </summary>
    private static float GetStatUnitCost(EStatType statType)
    {
        return StatUnitCostLoader.GetUnitCost(statType);
    }
    
    /// <summary>
    /// StatId 문자열 → EStatType 변환
    /// </summary>
    private static EStatType ConvertStatIdToEnum(string statId)
    {
        if (string.IsNullOrEmpty(statId))
            return EStatType.None;
        
        // EStatType enum으로 파싱
        if (Enum.TryParse<EStatType>(statId, out EStatType result))
        {
            return result;
        }
        
        Debug.LogWarning($"⚠️ [EquipmentGenerator] 알 수 없는 StatId: {statId}");
        return EStatType.None;
    }
    
    #endregion
}

