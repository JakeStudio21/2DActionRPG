using UnityEngine;

/// <summary>
/// 플레이어 클래스별 공통 데이터를 관리하는 기본 ScriptableObject
/// ✅ CSV 밸런싱 지원: 모든 기본값을 ScriptableObject에서 설정 가능
/// ✅ 클래스 차별화: Assasin/Warrior/Wizard 고유 능력치 정의
/// </summary>
[CreateAssetMenu(fileName = "BaseClassData", menuName = "Player/BaseClassData")]
public abstract class BaseClassData : ScriptableObject
{
    [Header("📋 클래스 기본 정보")]
    [Tooltip("클래스 이름 (한글)")]
    public string className = "";
    
    [Tooltip("클래스 타입 (enum)")]
    public PlayerType playerType = PlayerType.None;
    
    [Tooltip("클래스 아이콘")]
    public Sprite classIcon;
    
    [Tooltip("클래스 설명")]
    [TextArea(2, 4)]
    public string description = "";

    // ========================================
    // 📈 성장 스탯 (레벨업으로 증가)
    // ========================================
    
    [Header("📈 성장 스탯 - 체력")]
    [Tooltip("1레벨 기준 최대 체력")]
    public float baseMaxHealth = 100f;
    [Tooltip("레벨당 체력 증가량")]
    public float hpGainPerLevel = 20f;
    
    [Header("📈 성장 스탯 - 공격력 (목표 역산 방식)")]
    [Tooltip("1레벨 기준 공격력 (CSV 조정 가능)")]
    public float baseLevelAttackPower = 20f;
    
    [Tooltip("만렙 목표 공격력 (CSV 조정 가능)")]
    public float maxLevelTargetAttackPower = 5000f;
    
    [Tooltip("최고 레벨")]
    public int maxLevel = 60;
    
    [Header("📈 성장 스탯 - 방어력")]
    [Tooltip("1레벨 기준 방어력 (CSV 조정 가능)")]
    public float baseDefense = 5f;
    
    [Tooltip("레벨당 방어력 증가량 (CSV 조정 가능)")]
    public float defenseGainPerLevel = 1f;

    // ========================================
    // 🎯 고정 스탯 (클래스 고유 특성)
    // ========================================
    
    [Header("🎯 고정 스탯 - 크리티컬")]
    [Tooltip("기본 크리티컬 확률 (0.05 = 5%, CSV 조정 가능)")]
    [Range(0f, 1f)]
    public float baseCritRate = 0.05f;
    
    [Tooltip("크리티컬 데미지 배율 (1.5 = 150%, CSV 조정 가능)")]
    public float baseCritDamage = 1.5f;
    
    [Header("🎯 고정 스탯 - 전투")]
    [Tooltip("기본 공격속도 (1.0 = 기본, 1.1 = 10% 빠름, CSV 조정 가능)")]
    public float baseAttackSpeed = 1.0f;
    
    [Header("🎯 고정 스탯 - 이동")]
    [Tooltip("기본 이동 속도 (CSV 조정 가능)")]
    public float baseMoveSpeed = 4f;
    
    [Tooltip("기본 대시 속도")]
    public float baseDashSpeed = 6f;
    
    [Header("🎯 고정 스탯 - 회복")]
    [Tooltip("회복 효율 배율 (1.0 = 100%, 1.2 = 120%, CSV 조정 가능)")]
    public float healMultiplier = 1.0f;

    // ========================================
    // 🎮 컴포넌트 설정 (Knockback, Flash 등)
    // ========================================
    
    [Header("🎮 컴포넌트 설정")]
    [Tooltip("기본 넉백 강도")]
    public float baseKnockbackThrust = 10f;
    
    [Tooltip("넉백 효과 지속 시간")]
    public float knockbackTime = 0.2f;
    
    [Tooltip("플래시 효과 지속 시간")]
    public float flashDuration = 0.1f;
    
    [Tooltip("기본 공격력 (레거시, 현재는 baseLevelAttackPower 사용)")]
    public float baseAttackDamage = 10f;

    // ========================================
    // 🔢 클래스 배율 (추상 - 자식 클래스에서 정의)
    // ========================================
    
    /// <summary>공격력 배율 (1.0 = 기본, 1.2 = 20% 증가)</summary>
    public abstract float AttackPowerMultiplier { get; }
    
    /// <summary>이동속도 배율 (1.0 = 기본, 1.3 = 30% 증가)</summary>
    public abstract float MoveSpeedMultiplier { get; }
    
    /// <summary>스킬 쿨다운 배율 (1.0 = 기본, 0.8 = 20% 감소)</summary>
    public abstract float SkillCooldownMultiplier { get; }
    
    /// <summary>체력 배율 (1.0 = 기본, 1.5 = 50% 증가)</summary>
    public abstract float HealthMultiplier { get; }
    
    // ========================================
    // 🔧 헬퍼 메서드들
    // ========================================
    
    /// <summary>
    /// 최종 능력치 계산 메서드들 (배율 적용 후)
    /// </summary>
    public float GetFinalMaxHealth() => baseMaxHealth * HealthMultiplier;
    public float GetFinalMoveSpeed() => baseMoveSpeed * MoveSpeedMultiplier;
    public float GetFinalDashSpeed() => baseDashSpeed * MoveSpeedMultiplier;
    
    /// <summary>
    /// 레벨별 캐릭터 기본 공격력 (목표 역산 방식)
    /// 공식: baseLevelAttackPower × (growthRate ^ (level - 1))
    /// growthRate = ((maxLevelTargetAttackPower / baseLevelAttackPower) ^ (1 / (maxLevel - 1)))
    /// ✅ CSV 밸런싱: baseLevelAttackPower와 maxLevelTargetAttackPower를 CSV에서 조정 가능
    /// </summary>
    public virtual float GetCharacterBaseAttack(int level)
    {
        // 목표 역산 방식: 성장률 자동 계산
        float growthRate = CalculateGrowthRate();
        
        // 레벨별 공격력 = 1레벨 기준 공격력 × (성장률 ^ (레벨 - 1))
        return baseLevelAttackPower * Mathf.Pow(growthRate, level - 1);
    }
    
    /// <summary>
    /// 목표 역산 방식으로 성장률 계산
    /// r = ((만렙 목표 공격력 / 1레벨 기준 공격력) ^ (1 / (최고 레벨 - 1)))
    /// </summary>
    private float CalculateGrowthRate()
    {
        if (maxLevel <= 1 || baseLevelAttackPower <= 0 || maxLevelTargetAttackPower <= 0)
        {
            Debug.LogWarning($"[{className}] 성장률 계산 불가 - 기본값 1.08 사용");
            return 1.08f;
        }
        
        // r = (targetAttackPower / baseAttackPower) ^ (1 / (maxLevel - 1))
        float ratio = maxLevelTargetAttackPower / baseLevelAttackPower;
        float exponent = 1f / (maxLevel - 1);
        float growthRate = Mathf.Pow(ratio, exponent);
        
        return growthRate;
    }

    /// <summary>
    /// 디버그용 정보 출력
    /// </summary>
    public virtual string GetStatsInfo()
    {
        float growthRate = CalculateGrowthRate();
        float growthPercent = (growthRate - 1f) * 100f;
        
        return $"=== {className} 클래스 정보 ===\n" +
               $"📈 성장 스탯:\n" +
               $"  체력: {baseMaxHealth:F0} (+{hpGainPerLevel}/Lv) → 최종 x{HealthMultiplier:F1}\n" +
               $"  공격력: Lv1 {baseLevelAttackPower:F0} → Lv{maxLevel} {maxLevelTargetAttackPower:F0}\n" +
               $"  방어력: {baseDefense:F0} (+{defenseGainPerLevel}/Lv)\n" +
               $"  성장률: {growthRate:F4} ({growthPercent:F2}%/Lv)\n" +
               $"🎯 고정 스탯:\n" +
               $"  크리티컬: {baseCritRate:P0} (x{baseCritDamage:F1})\n" +
               $"  공격속도: {baseAttackSpeed:F1}\n" +
               $"  이동속도: {baseMoveSpeed:F1} → 최종 x{MoveSpeedMultiplier:F1}\n" +
               $"  쿨다운: x{SkillCooldownMultiplier:F1}\n" +
               $"  회복 효율: {healMultiplier:P0}";
    }
}
