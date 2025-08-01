using UnityEngine;

/// <summary>
/// 위저드 전용 스킬 데이터
/// 마법, 마나, 원소 속성 관련 특화 (SRP 준수)
/// </summary>
[CreateAssetMenu(fileName = "WizardSkillData", menuName = "Skill System/Wizard Skill Data")]
public class WizardSkillData : BaseSkillData
{
    [Header("🔮 위저드 전용 - 마법")]
    [Tooltip("마나 소모량")]
    public float manaCost = 20f;
    
    [Tooltip("시전 시간 (초)")]
    public float castTime = 1f;
    
    [Tooltip("마법 효과 범위 반경")]
    public float spellRadius = 3f;
    
    [Tooltip("원소 타입")]
    public ElementType elementType = ElementType.Fire;
    
    [Tooltip("마법력 배율 보너스")]
    public float spellPowerMultiplier = 1.0f;
    
    [Header("✨ 위저드 전용 - 마법 이펙트")]
    [Tooltip("마법 발사체 프리팹")]
    public GameObject magicProjectilePrefab;
    
    [Tooltip("광역 효과 프리팹")]
    public GameObject areaEffectPrefab;
    
    [Tooltip("마법 발사체 속도")]
    public float magicSpeed = 8f;
    
    [Tooltip("동시 타겟 가능 수")]
    public int targetCount = 1;
    
    [Tooltip("마법 이펙트 풀 이름")]
    public string magicEffectPoolName = "MagicEffect";
    
    [Header("🌟 위저드 전용 - 특수 효과")]
    [Tooltip("관통 가능 적 수")]
    public int pierceCount = 0;
    
    [Tooltip("지속 효과 시간 (도트 데미지 등)")]
    public float durationTime = 0f;
    
    [Tooltip("반복 효과 간격 (도트 데미지 간격)")]
    public float tickInterval = 1f;
    
    [Tooltip("넉백 강도")]
    public float knockbackForce = 5f;
}

/// <summary>
/// 마법 원소 타입 열거형
/// </summary>
public enum ElementType
{
    Fire,       // 화염 - 지속 데미지
    Ice,        // 얼음 - 감속 효과
    Lightning,  // 번개 - 연쇄 공격
    Earth,      // 대지 - 광역 효과
    Arcane      // 신비 - 마나 회복
}