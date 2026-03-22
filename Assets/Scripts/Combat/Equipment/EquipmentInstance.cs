using System;
using System.Collections.Generic;
using UnityEngine;
using Systems; // ⭐ Stage 5: EnhancementSystem 사용

/// <summary>
/// 장비 런타임 인스턴스
/// 정적 데이터(EquipmentData) + 동적 데이터(강화/귀속)
/// </summary>
[Serializable]
public class EquipmentInstance
{
    #region 고유 식별자
    
    /// <summary>
    /// 고유 인스턴스 ID
    /// </summary>
    public ItemInstanceID instanceId;
    
    #endregion
    
    #region 정적 데이터 (ScriptableObject)
    
    /// <summary>
    /// 장비 데이터 (런타임)
    /// </summary>
    [NonSerialized]
    private EquipmentData equipmentData;
    
    /// <summary>
    /// 장비 데이터 이름 (JSON 저장용)
    /// </summary>
    public string equipmentDataName;
    
    /// <summary>
    /// 장비 데이터 접근자
    /// </summary>
    public EquipmentData EquipmentData
    {
        get
        {
            if (equipmentData == null && !string.IsNullOrEmpty(equipmentDataName))
            {
                LoadEquipmentData();
            }
            return equipmentData;
        }
        set
        {
            equipmentData = value;
            if (value != null)
            {
                equipmentDataName = value.name;
            }
        }
    }
    
    #endregion
    
    #region 동적 데이터 (런타임 변경)
    
    /// <summary>
    /// 강화 레벨 (0~15)
    /// </summary>
    public int enhanceLevel;
    
    /// <summary>
    /// 귀속 여부
    /// </summary>
    public bool isBound;
    
    /// <summary>
    /// 등급 배율이 적용된 최종 주옵션 수치 (EquipmentGenerator에서 설정)
    /// </summary>
    public float finalMainStatValue;
    
    /// <summary>
    /// 랜덤으로 뽑힌 부옵션과 수치 (Key: EStatType, Value: 최종 수치)
    /// </summary>
    public Dictionary<EStatType, float> randomSubStats = new Dictionary<EStatType, float>();
    
    #endregion
    
    #region 캐시
    
    /// <summary>
    /// StatModifier 캐시 (성능 최적화)
    /// </summary>
    [NonSerialized]
    private List<StatModifier> cachedModifiers;
    
    /// <summary>
    /// 캐시 유효성
    /// </summary>
    [NonSerialized]
    private bool isCacheValid;
    
    #endregion
    
    #region 생성자
    
    public EquipmentInstance()
    {
    }
    
    public EquipmentInstance(ItemInstanceID instanceId, EquipmentData data, int enhanceLevel = 0, bool isBound = false)
    {
        this.instanceId = instanceId;
        this.EquipmentData = data;
        this.enhanceLevel = enhanceLevel;
        this.isBound = isBound;
    }
    
    #endregion
    
    #region 스탯 변환
    
    /// <summary>
    /// 장비 스탯 → StatModifier 리스트 변환 (V2: baseStats 기반)
    /// StatDefinition.csv의 Unit 정보를 활용하여 자동 변환
    /// </summary>
    public List<StatModifier> GetStatModifiers()
    {
        // 캐시 확인
        if (isCacheValid && cachedModifiers != null)
        {
            return cachedModifiers;
        }
        
        cachedModifiers = new List<StatModifier>();
        
        if (EquipmentData == null)
        {
            Debug.LogWarning($"[EquipmentInstance] {instanceId} - EquipmentData가 null입니다.");
            return cachedModifiers;
        }
        
        string source = $"{EquipmentData.equipmentName}+{enhanceLevel}";
        
        // finalMainStatValue가 있으면 주옵션 타입을 미리 파악하여 baseStats 루프에서 중복 방지
        EStatType dynamicMainStatType = finalMainStatValue > 0f ? GetMainStatType() : EStatType.None;
        
        // ===== baseStats 루프 (보조 스탯 처리) =====
        foreach (var stat in EquipmentData.baseStats)
        {
            if (stat.value <= 0) continue;
            
            // 1. StatId → EStatType 변환
            EStatType statType = ConvertStatIdToEnum(stat.statId);
            if (statType == EStatType.None)
            {
                Debug.LogWarning($"[EquipmentInstance] 알 수 없는 StatId: {stat.statId}");
                continue;
            }
            
            // finalMainStatValue가 있으면 동일 타입의 baseStats 값은 스킵 (동적 값으로 대체됨)
            if (dynamicMainStatType != EStatType.None && statType == dynamicMainStatType)
            {
                Debug.Log($"[EquipmentInstance] baseStats '{statType}' 스킵 → finalMainStatValue로 대체");
                continue;
            }
            
            // 2. StatDefinition에서 Unit, StackRule, ApplyPhase 가져오기
            var definition = StatDefinitions.Get(statType);
            if (definition == null)
            {
                Debug.LogWarning($"[EquipmentInstance] StatDefinition을 찾을 수 없음: {statType}");
                continue;
            }
            
            // 3. 강화 레벨 적용 (Flat 스탯만)
            float value = stat.value;
            if (definition.unit == StatUnit.Flat && IsEnhanceable(statType))
            {
                value = CalculateEnhancedValue(value, statType);
            }
            
            // 4. Percent 단위 변환 (baseStats용: CSV 1 → 코드 0.01)
            if (definition.unit == StatUnit.Percent)
            {
                // ⚠️ 주의: 이것은 기존 baseStats 전용 변환 로직
                // EquipmentData.baseStats는 CSV에서 백분율로 저장 (1 = 1%)
                float originalValue = value;
                value /= 100f;
                Debug.Log($"🔄 [EquipmentInstance] baseStats Percent 단위 변환: {stat.statId} {originalValue} → {value:F4}");
            }
            
            // 5. StatModifier 생성 (stackRule, applyPhase는 StatDefinitions에서 자동 로드)
            cachedModifiers.Add(new StatModifier(
                statType,
                value,
                definition.unit,
                source
            ));
            
            Debug.Log($"✅ [EquipmentInstance] StatModifier 생성: {statType} = {value} ({definition.unit})");
        }
        
        // ⭐ Stage 5: finalMainStatValue (동적 주옵션) 강화 적용
        if (finalMainStatValue > 0f)
        {
            // 1. 주옵션 타입 결정
            EStatType mainStatType = GetMainStatType();
            
            // 2. StatDefinition 가져오기
            var mainDefinition = StatDefinitions.Get(mainStatType);
            if (mainDefinition != null)
            {
                // 3. 강화 레벨 적용 (Flat 스탯만)
                float mainValue = finalMainStatValue;
                if (mainDefinition.unit == StatUnit.Flat && IsEnhanceable(mainStatType))
                {
                    mainValue = CalculateEnhancedValue(finalMainStatValue, mainStatType);
                    Debug.Log($"🔧 [EquipmentInstance] 주옵션 강화 적용: {mainStatType} base={finalMainStatValue:F1} → enhanced={mainValue:F1} (+{enhanceLevel})");
                }
                
                // 4. Percent 단위 변환 (DynamicEquipmentGenerator → StatModifier)
                if (mainDefinition.unit == StatUnit.Percent)
                {
                    // ⚠️ 중요: DynamicEquipmentGenerator는 백분율 단위로 생성 (2.4 = 2.4%)
                    // 스탯 엔진은 소수점 단위를 요구 (0.024 = 2.4%)
                    float originalValue = mainValue;
                    mainValue /= 100f;
                    Debug.Log($"🔄 [EquipmentInstance] 주옵션 Percent 단위 변환: {originalValue} → {mainValue:F4}");
                }
                
                // 5. StatModifier 생성
                cachedModifiers.Add(new StatModifier(
                    mainStatType,
                    mainValue,
                    mainDefinition.unit,
                    source + " [주옵션]"
                ));
                
                Debug.Log($"✅ [EquipmentInstance] 주옵션 추가: {mainStatType} = {mainValue:F1} ({mainDefinition.unit})");
            }
            else
            {
                Debug.LogWarning($"⚠️ [EquipmentInstance] 주옵션 StatDefinition 없음: {mainStatType}");
            }
        }
        
        // ===== randomSubStats 루프 (동적 부옵션) - ⭐ 강화 영향 없음 =====
        if (randomSubStats != null && randomSubStats.Count > 0)
        {
            Debug.Log($"🎲 [EquipmentInstance] 부옵션 변환 시작: {randomSubStats.Count}개");
            
            foreach (var pair in randomSubStats)
            {
                EStatType statType = pair.Key;
                float value = pair.Value;
                
                if (value <= 0) continue;
                
                // 1. StatDefinition 가져오기
                var definition = StatDefinitions.Get(statType);
                if (definition == null)
                {
                    Debug.LogWarning($"[EquipmentInstance] 부옵션 StatDefinition 없음: {statType}");
                    continue;
                }
                
                // 2. ⚠️ 부옵션은 강화 영향 없음 (원본 수치 유지)
                // - 파밍 재미를 위해 부옵션은 절대 변경하지 않음
                // - CalculateEnhancedValue() 절대 호출 금지!
                float finalValue = value;
                
                // 3. Percent 단위 변환 (DynamicEquipmentGenerator → StatModifier)
                if (definition.unit == StatUnit.Percent)
                {
                    // ⚠️ 중요: DynamicEquipmentGenerator는 백분율 단위로 생성 (2.4 = 2.4%)
                    // 스탯 엔진은 소수점 단위를 요구 (0.024 = 2.4%)
                    // 따라서 100으로 나누어서 변환
                    float originalValue = finalValue;
                    finalValue = value / 100f;
                    Debug.Log($"🔄 [EquipmentInstance] 부옵션 Percent 단위 변환: {originalValue} → {finalValue:F4}");
                }
                
                // 4. StatModifier 생성
                cachedModifiers.Add(new StatModifier(
                    statType,
                    finalValue,
                    definition.unit,
                    source + " [부옵션]"
                ));
                
                Debug.Log($"✅ [EquipmentInstance] 부옵션 유지: {statType} = {finalValue:F1} ({definition.unit}) [강화 영향 없음]");
            }
        }
        
        isCacheValid = true;
        return cachedModifiers;
    }
    
    /// <summary>
    /// ⭐ Stage 5: 강화 레벨 적용 (EnhanceCurveTableSO 기반)
    /// - 하드코딩 제거, 곡선 테이블 기반 성장
    /// - 장비 타입별 차별화된 성장률
    /// </summary>
    private float CalculateEnhancedValue(float baseValue, EStatType statType)
    {
        // 강화 영향을 받는 스탯 (화이트리스트)
        if (!IsEnhanceable(statType))
        {
            return baseValue;
        }
        
        // 강화 레벨 0이면 원본 반환
        if (enhanceLevel == 0)
        {
            return baseValue;
        }
        
        // 1. 곡선 그룹 ID 가져오기
        string curveGroupId = GetCurveGroupId();
        
        // 2. EnhancementSystem을 통해 누적 증가율(%) 가져오기
        float totalBonusPercent = EnhancementSystem.GetTotalStatBonus(curveGroupId, enhanceLevel);
        
        if (totalBonusPercent <= 0f)
        {
            Debug.LogWarning($"⚠️ [EquipmentInstance] 강화 배율을 가져올 수 없습니다: curveGroupId={curveGroupId}, level={enhanceLevel}");
            return baseValue;
        }
        
        // 3. 최종 계산: baseValue × (1 + 총증가율%)
        float finalValue = baseValue * (1f + (totalBonusPercent / 100f));
        
        Debug.Log($"🔧 [EquipmentInstance] 강화 계산: {statType} base={baseValue:F1}, level=+{enhanceLevel}, curve={curveGroupId}, bonus={totalBonusPercent:F1}%, final={finalValue:F1}");
        
        return finalValue;
    }
    
    /// <summary>
    /// 강화 가능 스탯 체크 (화이트리스트)
    /// </summary>
    private bool IsEnhanceable(EStatType statType)
    {
        // 무기: ATK_FLAT만
        // 방어구: DEF_FLAT, HP_FLAT만
        // 특수 스탯(크리티컬, 공속 등)은 강화 불가
        return statType == EStatType.ATK_FLAT ||
               statType == EStatType.DEF_FLAT ||
               statType == EStatType.HP_FLAT;
    }
    
    /// <summary>
    /// ⭐ Stage 5: 곡선 그룹 ID 가져오기
    /// </summary>
    private string GetCurveGroupId()
    {
        if (EquipmentData == null)
        {
            Debug.LogWarning("[EquipmentInstance] EquipmentData가 null입니다.");
            return "CURVE_STANDARD";
        }
        
        // 1순위: EquipmentData.enhancementCurveGroupId
        if (!string.IsNullOrEmpty(EquipmentData.enhancementCurveGroupId))
        {
            return EquipmentData.enhancementCurveGroupId;
        }
        
        // 2순위: equipmentType 기반 자동 추론
        return EquipmentData.equipmentType switch
        {
            EquipmentType.Weapon => "CURVE_WEAPON",
            EquipmentType.Armor => "CURVE_ARMOR",
            EquipmentType.Accessory => "CURVE_ACCESSORY",
            _ => "CURVE_STANDARD" // Fallback
        };
    }
    
    /// <summary>
    /// ⭐ Stage 5: 주옵션 스탯 타입 가져오기
    /// </summary>
    private EStatType GetMainStatType()
    {
        if (EquipmentData == null)
        {
            Debug.LogWarning("[EquipmentInstance] EquipmentData가 null입니다.");
            return EStatType.ATK_FLAT;
        }
        
        // 1순위: StatPoolDataLoader (CSV 기반)
        if (StatPoolDataLoader.TryGetStatPool(EquipmentData.equipmentSlot.ToString(), 
            out EStatType mainStat, out _))
        {
            return mainStat;
        }
        
        // 2순위: equipmentType 기반 추론
        return EquipmentData.equipmentType switch
        {
            EquipmentType.Weapon => EStatType.ATK_FLAT,
            EquipmentType.Armor => EStatType.DEF_FLAT,
            EquipmentType.Accessory => EStatType.HP_FLAT,
            _ => EStatType.ATK_FLAT // Fallback
        };
    }
    
    /// <summary>
    /// 캐시 무효화 (강화/변경 시 호출)
    /// </summary>
    public void InvalidateCache()
    {
        isCacheValid = false;
        cachedModifiers = null;
    }
    
    /// <summary>
    /// StatId 문자열 → EStatType 변환
    /// </summary>
    private EStatType ConvertStatIdToEnum(string statId)
    {
        if (string.IsNullOrEmpty(statId))
            return EStatType.None;
        
        // EStatType enum에 정의된 값으로 파싱
        if (System.Enum.TryParse<EStatType>(statId, out var result))
        {
            return result;
        }
        
        return EStatType.None;
    }
    
    #endregion
    
    #region 로드
    
    /// <summary>
    /// EquipmentData 로드 (ItemDatabase 정적 캐싱 사용)
    /// </summary>
    private void LoadEquipmentData()
    {
        // ItemDatabase의 정적 캐시에서 로드
        equipmentData = ItemDatabase.GetEquipment(equipmentDataName);
        
        if (equipmentData == null)
        {
            Debug.LogError($"[EquipmentInstance] EquipmentData를 찾을 수 없습니다: {equipmentDataName}");
        }
    }
    
    #endregion
    
    #region 유틸리티
    
    /// <summary>
    /// 인스턴스 복제
    /// </summary>
    public EquipmentInstance Clone()
    {
        return new EquipmentInstance
        {
            instanceId = ItemInstanceID.Generate(),
            EquipmentData = this.EquipmentData,
            enhanceLevel = this.enhanceLevel,
            isBound = this.isBound,
            finalMainStatValue = this.finalMainStatValue,
            randomSubStats = new Dictionary<EStatType, float>(this.randomSubStats) // Deep Copy
        };
    }
    
    /// <summary>
    /// 디버그 정보
    /// </summary>
    public override string ToString()
    {
        string boundStr = isBound ? "[귀속]" : "";
        return $"{EquipmentData?.equipmentName}+{enhanceLevel} {boundStr} ({instanceId})";
    }
    
    #endregion
}

