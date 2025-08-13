using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 몬스터 데이터 ScriptableObject - 모든 몬스터 공용
/// Basic/Elite/Boss는 enemyType enum과 MonsterGrowthProfile 배율로 구분
/// ⭐ [Phase 1] 중복 필드 제거: attackDamage, attackCooldown, attackRange → AttackData로 이관
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("🏷️ 기본 정보")]
    [Tooltip("고유 몬스터 ID (JSON 관리용)")]
    [SerializeField] private string enemyId = "";
    [SerializeField] private string enemyName = "몬스터";
    [SerializeField] private EnemyType enemyType = EnemyType.Basic;
    [TextArea(2, 4)]
    [SerializeField] private string description = "몬스터 설명";

    [Header("📊 기본 스탯")]
    [SerializeField] private float baseHealth = 50f;

    // ❌ 삭제: [SerializeField] private float baseAttack = 10f;
    [SerializeField] private float baseDefense = 0f;
    [SerializeField] private float baseMoveSpeed = 2f;

    [Header("🎯 전투 설정")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float chaseRange = 8f;
    [SerializeField] private float patrolRadius = 3f;
    
    [Header("⚡ 피격 효과 설정")]
    [Tooltip("넉백 강도")]
    [SerializeField] private float knockBackThrust = 15f;
    
    [Tooltip("사망 이펙트 프리팹 경로 (Resources 폴더 기준)")]
    [SerializeField] private string deathVFXPrefabPath = "";

    [Header("⚔️ 공격 설정")]
    [SerializeField] private AttackType primaryAttackType = AttackType.Melee;
    // ❌ 삭제: attackCooldown → AttackData에서 관리
    // ❌ 삭제: attackDamage → AttackData에서 관리

    [Header("🎮 보스 전용 (Boss 타입만)")]
    [SerializeField] private bool hasSkills = false;
    [SerializeField] private List<string> skillIds = new List<string>();
    [SerializeField] private List<StatusEffectType> immuneEffects = new List<StatusEffectType>();
    
    [Header("🏆 보스 식별")]
    [Tooltip("보스 몬스터 여부")]
    [SerializeField] private bool isBoss = false;
    
    [Tooltip("미니보스 여부")]
    [SerializeField] private bool isMiniBoss = false;
    
    [Tooltip("보스 ID (BossA, BossB 등)")]
    [SerializeField] private string bossId = "";

    [Header("💰 보상 설정")]
    [Tooltip("기본 골드 보상 (레벨 스케일링 적용)")]
    [SerializeField] private int baseGoldReward = 10;
    
    [Tooltip("기본 경험치 보상 (레벨 스케일링 적용)")]
    [SerializeField] private int baseExpReward = 25;
    
    [Header("🎁 드롭 시스템")]
    [Tooltip("드롭 그룹 ID (DropTable 참조용)")]
    [SerializeField] private string dropGroupId = "";
    
    [Tooltip("드롭 시도 횟수 (추가 드롭 기회)")]
    [Range(1, 5)]
    [SerializeField] private int dropRolls = 1;

    // Public Properties (Read-Only) - 새 필드들 추가
    public string EnemyId => enemyId;
    public string EnemyName => enemyName;
    public EnemyType EnemyType => enemyType;
    public string Description => description;
    
    public float BaseHealth => baseHealth;
    public float BaseDefense => baseDefense;
    public float BaseMoveSpeed => baseMoveSpeed;
    
    public float DetectionRange => detectionRange;
    public float ChaseRange => chaseRange;
    public float PatrolRadius => patrolRadius;
    
    // ⭐ 새 필드들 Properties
    public float KnockBackThrust => knockBackThrust;
    public string DeathVFXPrefabPath => deathVFXPrefabPath;
    
    public AttackType PrimaryAttackType => primaryAttackType;
    
    public bool HasSkills => hasSkills;
    public List<string> SkillIds => skillIds;
    public List<StatusEffectType> ImmuneEffects => immuneEffects;
    
    public bool IsBoss => isBoss;
    public bool IsMiniBoss => isMiniBoss;
    public string BossId => bossId;
    
    public int BaseGoldReward => baseGoldReward;
    public int BaseExpReward => baseExpReward;
    
    // ⭐ 새 드롭 시스템 Properties
    public string DropGroupId => dropGroupId;
    public int DropRolls => dropRolls;

    /// <summary>
    /// 레벨과 성장 프로필을 적용한 실제 체력 계산
    /// </summary>
    public float GetScaledHealth(int level, MonsterGrowthProfile growthProfile)
    {
        if (growthProfile == null) return baseHealth;
        return baseHealth * growthProfile.GetHealthMultiplier(level, enemyType);
    }

    /// <summary>
    /// 레벨과 성장 프로필을 적용한 실제 방어력 계산
    /// </summary>
    public float GetScaledDefense(int level, MonsterGrowthProfile growthProfile)
    {
        if (growthProfile == null) return baseDefense;
        return baseDefense * growthProfile.GetDefenseMultiplier(level, enemyType);
    }

    /// <summary>
    /// 레벨과 성장 프로필을 적용한 실제 이동속도 계산
    /// </summary>
    public float GetScaledMoveSpeed(int level, MonsterGrowthProfile growthProfile)
    {
        if (growthProfile == null) return baseMoveSpeed;
        return baseMoveSpeed * growthProfile.GetSpeedMultiplier(level, enemyType);
    }

    /// <summary>
    /// 레벨을 적용한 골드 보상 계산
    /// </summary>
    public int GetScaledGoldReward(int level, MonsterGrowthProfile growthProfile)
    {
        if (growthProfile == null) return baseGoldReward;
        return Mathf.RoundToInt(baseGoldReward * growthProfile.GetRewardMultiplier(level));
    }

    /// <summary>
    /// 레벨을 적용한 경험치 보상 계산
    /// </summary>
    public int GetScaledExpReward(int level, MonsterGrowthProfile growthProfile)
    {
        if (growthProfile == null) return baseExpReward;
        return Mathf.RoundToInt(baseExpReward * growthProfile.GetRewardMultiplier(level));
    }

    /// <summary>
    /// Inspector에서 설정값 검증
    /// </summary>
    private void OnValidate()
    {
        // 기본 스탯 최소값 보장
        baseHealth = Mathf.Max(1f, baseHealth);
        baseDefense = Mathf.Max(0f, baseDefense);
        baseMoveSpeed = Mathf.Max(0.1f, baseMoveSpeed);

        // 범위 값들 최소값 보장
        detectionRange = Mathf.Max(0.5f, detectionRange);
        chaseRange = Mathf.Max(detectionRange, chaseRange);
        patrolRadius = Mathf.Max(0.5f, patrolRadius);

        // ⭐ 새 필드들 검증
        knockBackThrust = Mathf.Max(0f, knockBackThrust);
        
        // 보스 설정 검증
        if (isBoss || isMiniBoss)
        {
            if (string.IsNullOrEmpty(bossId))
            {
                Debug.LogWarning($"[EnemyData] {enemyName}: 보스/미니보스인데 bossId가 설정되지 않았습니다!");
            }
        }
        
        // 보상 최소값 보장
        baseGoldReward = Mathf.Max(0, baseGoldReward);
        baseExpReward = Mathf.Max(0, baseExpReward);

        // ⭐ 드롭 시스템 검증 추가
        if (string.IsNullOrEmpty(dropGroupId))
        {
            Debug.LogWarning($"[EnemyData] {enemyName}: DropGroupId가 설정되지 않았습니다! 아이템 드롭이 작동하지 않을 수 있습니다.");
        }
        
        dropRolls = Mathf.Clamp(dropRolls, 1, 5);

        // Boss 타입이 아니면 스킬 설정 비활성화
        if (enemyType != EnemyType.Boss)
        {
            hasSkills = false;
            skillIds.Clear();
        }
    }

    /// <summary>
    /// 디버그용 정보 출력
    /// </summary>
    public string GetDebugInfo(int level = 1, MonsterGrowthProfile growthProfile = null)
    {
        if (growthProfile == null)
        {
            return $"[EnemyData] ID: {enemyId}, Name: {enemyName}, Type: {enemyType}\n" +
                   $"Health: {baseHealth}, Speed: {baseMoveSpeed}\n" +
                   $"DropGroup: {dropGroupId}, Rolls: {dropRolls}";
        }
        else
        {
            return $"{enemyName} Lv.{level} ({enemyType})\n" +
                   $"ID: {enemyId}\n" +
                   $"HP: {GetScaledHealth(level, growthProfile):F1}\n" +
                   $"ATK: AttackData에서 관리됨\n" +
                   $"DEF: {GetScaledDefense(level, growthProfile):F1}\n" +
                   $"Speed: {GetScaledMoveSpeed(level, growthProfile):F1}\n" +
                   $"Gold: {GetScaledGoldReward(level, growthProfile)}, Exp: {GetScaledExpReward(level, growthProfile)}\n" +
                   $"DropGroup: {dropGroupId} (x{dropRolls})";
        }
    }
}
