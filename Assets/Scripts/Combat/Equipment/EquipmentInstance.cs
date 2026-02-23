using System;
using System.Collections.Generic;
using UnityEngine;

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
        
        // ===== baseStats 루프 (동적 처리) =====
        foreach (var stat in EquipmentData.baseStats)
        {
            if (stat.value <= 0) continue; // 0 이하 스킵
            
            // 1. StatId → EStatType 변환
            EStatType statType = ConvertStatIdToEnum(stat.statId);
            if (statType == EStatType.None)
            {
                Debug.LogWarning($"[EquipmentInstance] 알 수 없는 StatId: {stat.statId}");
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
            
            // 4. Percent 단위 변환 (CSV: 1 → 코드: 0.01)
            if (definition.unit == StatUnit.Percent)
            {
                float originalValue = value;
                value /= 100f;
                Debug.Log($"🔄 [EquipmentInstance] {stat.statId} 단위 변환: {originalValue} → {value:F4} (Percent)");
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
        
        isCacheValid = true;
        return cachedModifiers;
    }
    
    /// <summary>
    /// 강화 레벨 적용 (화이트리스트 필터링)
    /// </summary>
    private float CalculateEnhancedValue(float baseValue, EStatType statType)
    {
        // 강화 영향을 받는 스탯 (화이트리스트)
        if (!IsEnhanceable(statType))
        {
            return baseValue;
        }
        
        // 강화 1레벨당 10% 증가
        float enhanceBonus = baseValue * (enhanceLevel * 0.1f);
        return baseValue + enhanceBonus;
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
            isBound = this.isBound
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

