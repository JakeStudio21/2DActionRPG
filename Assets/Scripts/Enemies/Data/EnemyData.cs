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

    [Tooltip("피격 반응 설정 (포이즈·스태거·넉백 배율)\n" +
             "비워두면 항상 스태거 (일반 몬스터 기본 동작 유지)\n" +
             "Elite → HitReaction_Elite / Boss → HitReaction_Boss 할당")]
    [SerializeField] private HitReactionData hitReactionData;
    
    [Tooltip("사망 이펙트 프리팹 경로 (Resources 폴더 기준)")]
    [SerializeField] private string deathVFXPrefabPath = "";

    [Header("🎮 프리팹 참조 (풀링 시스템)")]
    [Tooltip("실제 몬스터 프리팹 (풀링용 직접 참조)")]
    [SerializeField] private GameObject enemyPrefab;
    
    [Tooltip("사망 이펙트 프리팹 (직접 참조, deathVFXPrefabPath보다 우선)")]
    [SerializeField] private GameObject deathVFXPrefab;

    [Header("⚔️ 공격 설정")]
    [SerializeField] private AttackType primaryAttackType = AttackType.Melee;
    // ❌ 삭제: attackCooldown → AttackData에서 관리
    // ❌ 삭제: attackDamage → AttackData에서 관리

    [Header("⚡ 스킬 시스템 (엘리트/보스)")]
    [Tooltip("스킬 데이터 리스트 (일반 몬스터는 비워둠)")]
    [SerializeField] private List<SkillData> skillDataList = new List<SkillData>();
    
    [Header("🎲 공격 확률 설정")]
    [Tooltip("평타 사용 확률 (%) - 일반: 100, 엘리트: 60, 보스: 30")]
    [Range(0, 100)]
    [SerializeField] private int meleeAttackProbability = 100;
    
    [Tooltip("스킬 사용 확률 (%) - 일반: 0, 엘리트: 40, 보스: 70")]
    [Range(0, 100)]
    [SerializeField] private int skillUseProbability = 0;
    
    [Tooltip("개별 스킬 확률 (%) - skillDataList 개수만큼 설정, 합계 100%")]
    [SerializeField] private List<int> skillProbabilities = new List<int>();

    [Header("🎮 보스 전용 (Boss 타입만)")]
    [SerializeField] private bool hasSkills = false; // Deprecated: skillDataList 사용
    [SerializeField] private List<string> skillIds = new List<string>(); // Deprecated
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
    
    [Tooltip("🎁 Phase 2: 보스 처치 보상 (보스만 할당, 일반 몬스터는 null)")]
    [SerializeField] private BossRewardData bossReward;
    
    [Header("🎁 드롭 시스템")]
    [Tooltip("드롭 그룹 ID (DropTable 참조용)")]
    [SerializeField] private string dropGroupId = "";
    
    [Tooltip("드롭 시도 횟수 (추가 드롭 기회)")]
    [Range(1, 5)]
    [SerializeField] private int dropRolls = 1;

    [Header("=== 패트롤 행동 튜닝 ===")]
    [Tooltip("패트롤 움직임 튜닝 데이터 - 선택적 할당")]
    [SerializeField] private PatrolTuning patrolTuning;

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
    public HitReactionData HitReactionData => hitReactionData;
    public string DeathVFXPrefabPath => deathVFXPrefabPath;
    
    // ⭐ 프리팹 참조 Properties (풀링 시스템용)
    public GameObject EnemyPrefab => enemyPrefab;
    public GameObject DeathVFXPrefab => deathVFXPrefab;
    
    public AttackType PrimaryAttackType => primaryAttackType;
    
    // ⭐ 스킬 시스템 Properties
    public List<SkillData> SkillDataList => skillDataList;
    public int MeleeAttackProbability => meleeAttackProbability;
    public int SkillUseProbability => skillUseProbability;
    public List<int> SkillProbabilities => skillProbabilities;
    public bool HasSkillData => skillDataList != null && skillDataList.Count > 0;
    
    // Deprecated: 호환성 유지용
    public bool HasSkills => hasSkills || HasSkillData;
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
    
    // 🎁 Phase 2: 보스 보상 시스템
    public BossRewardData BossReward => bossReward;

    public PatrolTuning PatrolTuning => patrolTuning;

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
        
        // ⭐ 스킬 시스템 검증
        meleeAttackProbability = Mathf.Clamp(meleeAttackProbability, 0, 100);
        skillUseProbability = Mathf.Clamp(skillUseProbability, 0, 100);
        
        // 스킬 확률 리스트 크기 자동 조정
        if (skillDataList != null && skillDataList.Count > 0)
        {
            // 스킬 개수만큼 확률 리스트 조정
            while (skillProbabilities.Count < skillDataList.Count)
            {
                // 균등 분배로 초기화 (예: 스킬 2개면 각각 50%)
                int defaultProb = 100 / Mathf.Max(1, skillDataList.Count);
                skillProbabilities.Add(defaultProb);
            }
            while (skillProbabilities.Count > skillDataList.Count)
            {
                skillProbabilities.RemoveAt(skillProbabilities.Count - 1);
            }
            
            // 각 확률값 범위 제한
            for (int i = 0; i < skillProbabilities.Count; i++)
            {
                skillProbabilities[i] = Mathf.Clamp(skillProbabilities[i], 0, 100);
            }
            
            // 확률 합계 검증 (경고만)
            int totalSkillProb = 0;
            foreach (int prob in skillProbabilities)
            {
                totalSkillProb += prob;
            }
            if (totalSkillProb != 100 && skillProbabilities.Count > 1)
            {
                Debug.LogWarning($"[EnemyData] {enemyName}: 스킬 확률 합계가 100%가 아닙니다! (현재: {totalSkillProb}%)");
            }
        }
        else
        {
            // 스킬이 없으면 확률 리스트 비우기
            skillProbabilities.Clear();
            skillUseProbability = 0;
        }
        
        // 확률 합계 검증 (평타 + 스킬)
        int totalProb = meleeAttackProbability + skillUseProbability;
        if (totalProb != 100)
        {
            Debug.LogWarning($"[EnemyData] {enemyName}: 평타({meleeAttackProbability}%) + 스킬({skillUseProbability}%) 합계가 100%가 아닙니다! (현재: {totalProb}%)");
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

        // PatrolTuning 검증
        if (patrolTuning == null)
        {
            Debug.LogWarning($"[EnemyData] {enemyName}: PatrolTuning이 할당되지 않았습니다!");
        }

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
            string info = $"{enemyName} Lv.{level} ({enemyType})\n" +
                   $"ID: {enemyId}\n" +
                   $"HP: {GetScaledHealth(level, growthProfile):F1}\n" +
                   $"ATK: AttackData에서 관리됨\n" +
                   $"DEF: {GetScaledDefense(level, growthProfile):F1}\n" +
                   $"Speed: {GetScaledMoveSpeed(level, growthProfile):F1}\n" +
                   $"Gold: {GetScaledGoldReward(level, growthProfile)}, Exp: {GetScaledExpReward(level, growthProfile)}\n" +
                   $"DropGroup: {dropGroupId} (x{dropRolls})";
            
            // ⭐ 스킬 정보 추가
            if (HasSkillData)
            {
                info += $"\n\n=== 공격 확률 ===\n";
                info += $"평타: {meleeAttackProbability}%, 스킬: {skillUseProbability}%\n";
                info += $"\n=== 스킬 목록 ({skillDataList.Count}개) ===\n";
                for (int i = 0; i < skillDataList.Count; i++)
                {
                    if (skillDataList[i] != null)
                    {
                        int prob = i < skillProbabilities.Count ? skillProbabilities[i] : 0;
                        info += $"{i + 1}. {skillDataList[i].SkillName} ({prob}%)\n";
                        info += $"   - Cooldown: {skillDataList[i].Cooldown}s, Damage: x{skillDataList[i].DamageMultiplier}\n";
                    }
                }
            }
            
            return info;
        }
    }
    
    /// <summary>
    /// 풀링용 프리팹 가져오기 (직접 참조 우선, 경로 기반 fallback)
    /// </summary>
    public GameObject GetPoolingPrefab()
    {
        // 1순위: 직접 참조된 프리팹
        if (enemyPrefab != null)
        {
            return enemyPrefab;
        }
        
        // 2순위: EnemyId 기반 Resources 로드 (호환성)
        if (!string.IsNullOrEmpty(enemyId))
        {
            // MON_BLUESLIME_001 → Blue_slime 변환 로직
            string resourcePath = ConvertEnemyIdToResourcePath(enemyId);
            GameObject fallbackPrefab = Resources.Load<GameObject>(resourcePath);
            
            if (fallbackPrefab != null)
            {
                Debug.LogWarning($"[EnemyData] {enemyId}: enemyPrefab이 null이어서 Resources에서 로드함: {resourcePath}");
                return fallbackPrefab;
            }
        }
        
        Debug.LogError($"[EnemyData] {enemyId}: 프리팹을 찾을 수 없습니다! enemyPrefab을 Inspector에서 할당해주세요.");
        return null;
    }
    
    /// <summary>
    /// 사망 이펙트 프리팹 가져오기 (직접 참조 우선, 경로 기반 fallback)
    /// </summary>
    public GameObject GetDeathVFXPrefab()
    {
        // 1순위: 직접 참조된 프리팹
        if (deathVFXPrefab != null)
        {
            return deathVFXPrefab;
        }
        
        // 2순위: 경로 기반 Resources 로드 (호환성)
        if (!string.IsNullOrEmpty(deathVFXPrefabPath))
        {
            GameObject fallbackVFX = Resources.Load<GameObject>(deathVFXPrefabPath);
            
            if (fallbackVFX != null)
            {
                return fallbackVFX;
            }
        }
        
        return null; // 사망 이펙트는 선택사항이므로 에러 로그 없음
    }
    
    /// <summary>
    /// EnemyId를 Resources 경로로 변환 (기존 호환성용)
    /// </summary>
    private string ConvertEnemyIdToResourcePath(string enemyId)
    {
        // MON_BLUESLIME_001 → Blue_slime
        if (enemyId.Contains("BLUESLIME")) return "Blue_slime";
        if (enemyId.Contains("GRAPE")) return "Enemie1";
        if (enemyId.Contains("GHOST")) return "Ghost";
        if (enemyId.Contains("FINALBOSSA")) return "FinalBossA";
        if (enemyId.Contains("FINALBOSSB")) return "FinalBossB";
        if (enemyId.Contains("FINALBOSSC")) return "FinalBossC";
        
        return enemyId; // 기본값
    }
}
