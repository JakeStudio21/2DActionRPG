using UnityEngine;

/// <summary>
/// 워리어 전용 스킬 데이터
/// 근접 전투, 돌진, 방어 관련 속성 특화 (SRP 준수)
/// </summary>
[CreateAssetMenu(fileName = "WarriorSkillData", menuName = "Skill System/Warrior Skill Data")]
public class WarriorSkillData : BaseSkillData
{
    [Header("⚔️ 워리어 전용 - 근접 전투")]
    [Tooltip("돌진 속도")]
    public float dashSpeed = 20f;
    
    [Tooltip("돌진 최대 거리")]
    public float dashRange = 8f;
    
    [Tooltip("연속 공격 횟수")]
    public int attackCount = 3;
    
    [Tooltip("각 공격 간 딜레이")]
    public float attackDelay = 0.3f;
    
    [Tooltip("공격 범위 반경")]
    public float attackRadius = 2f;
    
    [Header("🛡️ 워리어 전용 - 방어/반격")]
    [Tooltip("블록 확률 보너스 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float blockChanceBonus = 0.1f;
    
    [Tooltip("반격 데미지 배율")]
    public float counterAttackMultiplier = 1.5f;
    
    [Tooltip("적 기절 지속시간")]
    public float stunDuration = 1f;
    
    [Header("🎭 워리어 전용 - 이펙트")]
    [Tooltip("돌진 이펙트 프리팹")]
    public GameObject dashEffectPrefab;
    
    [Tooltip("베기 이펙트 프리팹")]
    public GameObject slashEffectPrefab;
    
    [Tooltip("돌진 이펙트 풀 이름")]
    public string dashEffectPoolName = "DashEffect";
    
    [Tooltip("베기 이펙트 풀 이름")]
    public string slashEffectPoolName = "SlashEffect";
}
