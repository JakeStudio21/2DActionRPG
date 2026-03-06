using UnityEngine;

/// <summary>
/// 정령의 가호 데이터 (상태이상 저항 시스템)
/// Phase 2: Resistance System UI
/// </summary>
[CreateAssetMenu(fileName = "SpiritBlessing_", menuName = "Data/Spirit Blessing", order = 52)]
public class SpiritBlessingData : ScriptableObject
{
    [Header("=== 기본 정보 ===")]
    [Tooltip("연결될 상태이상 타입")]
    public EStatusEffectType targetEffectType;
    
    [Tooltip("UI에 표시될 이름 (예: 숲의 정령의 가호)")]
    public string blessingName = "정령의 가호";
    
    [Tooltip("UI 아이콘")]
    public Sprite blessingIcon;
    
    [Header("=== 설명 ===")]
    [TextArea(3, 5)]
    [Tooltip("하단 패널에 표시될 상세 설명")]
    public string description = "깊은 곳에서 꿈틀대는 악의 기운을 씻어내고, 정령이 내리는 따뜻한 가호를 받습니다.";
    
    [Header("=== 성장 수치 ===")]
    [Tooltip("1회 강화 시 증가하는 수치 (기본 3%)")]
    [Range(0.01f, 0.1f)]
    public float incrementPerLevel = 0.03f;
    
    [Tooltip("최대 저항 수치 (기본 100%)")]
    [Range(0.5f, 1.0f)]
    public float maxResistance = 1.0f;
    
    [Header("=== 재화 (Phase 9: 정령의 정수) ===")]
    [Tooltip("강화에 필요한 재료 타입 (자동 설정)")]
    public MaterialType requiredMaterialType = MaterialType.SPIRIT_ESSENCE_FOREST;
    
    [Tooltip("강화에 필요한 재료 수량")]
    public int costPerLevel = 10;
    
    /// <summary>
    /// 효과 타입 이름 반환 (UI 표시용)
    /// </summary>
    public string GetEffectTypeName()
    {
        switch (targetEffectType)
        {
            case EStatusEffectType.Bind:
                return "속박 내성";
            case EStatusEffectType.Poison:
                return "독 내성";
            case EStatusEffectType.Burn:
                return "화상 내성";
            case EStatusEffectType.Slow:
                return "둔화 내성";
            default:
                return "알 수 없음";
        }
    }
    
    /// <summary>
    /// 효과 타입 이모지 반환 (UI 표시용)
    /// </summary>
    public string GetEffectTypeEmoji()
    {
        switch (targetEffectType)
        {
            case EStatusEffectType.Bind:
                return "🌳";
            case EStatusEffectType.Poison:
                return "☠️";
            case EStatusEffectType.Burn:
                return "🔥";
            case EStatusEffectType.Slow:
                return "❄️";
            default:
                return "❓";
        }
    }
    
    /// <summary>
    /// 데이터 유효성 검증
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(blessingName))
        {
            Debug.LogError($"[SpiritBlessingData] blessingName이 비어있습니다: {name}");
            return false;
        }
        
        if (blessingIcon == null)
        {
            Debug.LogWarning($"[SpiritBlessingData] blessingIcon이 null입니다: {name}");
        }
        
        if (incrementPerLevel <= 0f)
        {
            Debug.LogError($"[SpiritBlessingData] incrementPerLevel이 0 이하입니다: {name}");
            return false;
        }
        
        if (maxResistance <= 0f || maxResistance > 1.0f)
        {
            Debug.LogError($"[SpiritBlessingData] maxResistance가 유효 범위(0~1)를 벗어났습니다: {name}");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Inspector에서 자동 설정
    /// </summary>
    private void OnValidate()
    {
        // 이름이 비어있으면 기본 이름 설정
        if (string.IsNullOrEmpty(blessingName))
        {
            blessingName = $"{GetEffectTypeEmoji()} {GetEffectTypeName()}의 가호";
        }
        
        // 상태이상 타입에 따라 자동으로 재료 매핑
        requiredMaterialType = targetEffectType switch
        {
            EStatusEffectType.Bind => MaterialType.SPIRIT_ESSENCE_FOREST,  // 속박 → 숲의 정수
            EStatusEffectType.Poison => MaterialType.SPIRIT_ESSENCE_EARTH, // 중독 → 대지의 정수
            EStatusEffectType.Burn => MaterialType.SPIRIT_ESSENCE_FLAME,   // 화상 → 불꽃의 정수
            EStatusEffectType.Slow => MaterialType.SPIRIT_ESSENCE_WATER,   // 둔화 → 물결의 정수
            _ => MaterialType.SPIRIT_ESSENCE_FOREST
        };
    }
}

