using UnityEngine;

/// <summary>
/// 플레이어 클래스별 공통 데이터를 관리하는 기본 ScriptableObject
/// 모든 클래스가 공통으로 사용하는 기본값들과 배율 적용 방식 정의
/// </summary>
[CreateAssetMenu(fileName = "BaseClassData", menuName = "Player/BaseClassData")]
public abstract class BaseClassData : ScriptableObject
{
    [Header("📊 공통 기본값")]
    [Tooltip("모든 클래스의 기본 최대 체력")]
    public float baseMaxHealth = 100f;
    
    [Tooltip("모든 클래스의 기본 이동 속도")]
    public float baseMoveSpeed = 4f;
    
    [Tooltip("모든 클래스의 기본 대시 속도")]
    public float baseDashSpeed = 6f;
    
    // 🆕 기본 전투 스탯 추가
    [Tooltip("모든 클래스의 기본 공격력")]
    public float baseAttackDamage = 10f;
    
    [Tooltip("모든 클래스의 기본 방어력")]
    public float baseDefense = 0f;
    
    [Tooltip("모든 클래스의 기본 피해 회복 시간")]
    public float baseDamageRecoveryTime = 1f;

    [Header("🎮 공통 컴포넌트 설정")]
    [Tooltip("플래시 효과 지속 시간")]
    public float flashDuration = 0.1f;
    
    [Tooltip("넉백 효과 지속 시간")]
    public float knockbackTime = 0.2f;
    
    [Tooltip("기본 넉백 강도")]
    public float baseKnockbackThrust = 10f;

    [Header("📈 레벨 시스템 기본값")]
    // 🗑️ 제거: PlayerDataManager에서 하드코딩으로 관리
    // public int startingLevel = 1;        // ← 삭제
    // public int startingExp = 0;          // ← 삭제
    
    [Tooltip("레벨 2까지 필요한 기본 경험치")]
    public int baseExpToNextLevel = 20;
    
    [Tooltip("레벨당 경험치 증가량")]
    public int expIncreasePerLevel = 5;

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

    // 🔢 능력치 배율 (추상 - 자식 클래스에서 정의)
    [Tooltip("공격력 배율 (1.0 = 기본, 1.2 = 20% 증가)")]
    public abstract float AttackPowerMultiplier { get; }
    
    [Tooltip("이동속도 배율 (1.0 = 기본, 1.3 = 30% 증가)")]
    public abstract float MoveSpeedMultiplier { get; }
    
    [Tooltip("스킬 쿨다운 배율 (1.0 = 기본, 0.8 = 20% 감소)")]
    public abstract float SkillCooldownMultiplier { get; }
    
    [Tooltip("체력 배율 (1.0 = 기본, 1.5 = 50% 증가)")]
    public abstract float HealthMultiplier { get; }
    
    /// <summary>
    /// 최종 능력치 계산 메서드들
    /// </summary>
    public float GetFinalMaxHealth() => baseMaxHealth * HealthMultiplier;
    public float GetFinalMoveSpeed() => baseMoveSpeed * MoveSpeedMultiplier;
    public float GetFinalDashSpeed() => baseDashSpeed * MoveSpeedMultiplier; // 대시도 이동속도 배율 적용
    public float GetFinalDamageRecoveryTime() => baseDamageRecoveryTime; // 회복시간은 배율 미적용
    
    // 🆕 전투 스탯 계산 메서드 추가
    public float GetFinalAttackDamage() => baseAttackDamage * AttackPowerMultiplier;
    public float GetFinalDefense() => baseDefense; // 방어력은 현재 배율 미적용 (필요시 추가 가능)
    
    /// <summary>
    /// 특정 레벨에 필요한 총 경험치 계산
    /// </summary>
    public int GetExpRequiredForLevel(int targetLevel)
    {
        if (targetLevel <= 1) return 0;
        
        int totalExp = 0;
        for (int level = 1; level < targetLevel; level++)
        {
            totalExp += baseExpToNextLevel + (expIncreasePerLevel * (level - 1));
        }
        return totalExp;
    }
    
    /// <summary>
    /// 현재 레벨에서 다음 레벨까지 필요한 경험치 계산
    /// </summary>
    public int GetExpToNextLevel(int currentLevel)
    {
        return baseExpToNextLevel + (expIncreasePerLevel * (currentLevel - 1));
    }

    /// <summary>
    /// 디버그용 정보 출력
    /// </summary>
    public virtual string GetStatsInfo()
    {
        return $"{className}\n" +
               $"체력: {GetFinalMaxHealth():F0} ({HealthMultiplier:F1}x)\n" +
               $"이동속도: {GetFinalMoveSpeed():F1} ({MoveSpeedMultiplier:F1}x)\n" +
               $"공격력: x{AttackPowerMultiplier:F1}\n" +
               $"쿨다운: x{SkillCooldownMultiplier:F1}";
    }
}
