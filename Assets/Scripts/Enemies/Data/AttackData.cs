using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 공격 데이터 ScriptableObject
/// 근접/원거리 공격 정보와 상태이상 효과를 통합 관리
/// </summary>
[CreateAssetMenu(fileName = "AttackData", menuName = "Enemy/Attack Data")]
public class AttackData : ScriptableObject
{
    [Header("🏷️ 기본 정보")]
    [SerializeField] private string attackName = "기본 공격";
    [SerializeField] private AttackType attackType = AttackType.Melee;
    [TextArea(2, 3)]
    [SerializeField] private string description = "공격 설명";

    [Header("💥 데미지 설정")]
    [Tooltip("기본 데미지 (레벨 스케일링 적용됨)")]
    [SerializeField] private int baseDamage = 10;
    
    [Tooltip("크리티컬 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float criticalChance = 0.05f;
    
    [Tooltip("크리티컬 데미지 배율")]
    [SerializeField] private float criticalMultiplier = 2f;

    [Header("⏱️ 타이밍 설정")]
    [Tooltip("공격 쿨다운 (초)")]
    [SerializeField] private float attackCooldown = 2f;
    
    [Tooltip("애니메이션 지속시간 (초)")]
    [SerializeField] private float animationDuration = 1f;
    
    [Tooltip("애니메이션 트리거명")]
    [SerializeField] private string animationTrigger = "Attack";

    [Header("🎯 범위 설정")]
    [Tooltip("공격 범위")]
    [SerializeField] private float attackRange = 1f;
    
    [Tooltip("공격 각도 (근접 공격용, 360도면 전방향)")]
    [SerializeField] private float attackAngle = 90f;
    
    [Tooltip("공격 중 이동 정지 여부")]
    [SerializeField] private bool stopMovingWhileAttacking = true;

    [Header("🚀 발사체 설정 (원거리 전용)")]
    [Tooltip("발사체 프리팹")]
    [SerializeField] private GameObject projectilePrefab;
    
    [Tooltip("발사체 궤적 타입 (직선/포물선)")]
    [SerializeField] private ProjectileTrajectoryType trajectoryType = ProjectileTrajectoryType.Straight;
    
    [Tooltip("포물선 높이 (trajectoryType이 Arc일 때만 사용)")]
    [SerializeField] private float arcHeight = 3f;
    
    [Tooltip("발사체 속도")]
    [SerializeField] private float projectileSpeed = 10f;
    
    [Tooltip("발사체 생존시간 (초)")]
    [SerializeField] private float projectileLifetime = 5f;
    
    [Tooltip("다중 발사 수 (Ghost용)")]
    [SerializeField] private int projectileCount = 1;
    
    [Tooltip("다중 발사 각도 간격")]
    [SerializeField] private float multiShotAngle = 15f;

    [Header("💥 AOE 설정 (영역 공격 전용)")]
    [Tooltip("AOE 이펙트 프리팹")]
    [SerializeField] private GameObject aoeEffectPrefab;
    
    [Tooltip("AOE 지속시간 (초)")]
    [SerializeField] private float aoeDuration = 1f;
    
    [Tooltip("AOE 모양 타입")]
    [SerializeField] private AOEShapeType aoeShape = AOEShapeType.Circle;
    
    [Tooltip("AOE 크기 조절 (1.0 = 기본 크기)")]
    [SerializeField] private float aoeScale = 1f;


    [Header("☠️ 상태이상 효과")]
    [Tooltip("공격 시 적용할 상태이상들")]
    [SerializeField] private List<StatusEffectData> onHitEffects = new List<StatusEffectData>();
    
    [Tooltip("상태이상 적용 확률 (각각 개별 확률)")]
    [SerializeField] private List<float> effectChances = new List<float>();

    [Header("🎨 이펙트 및 사운드")]
    [Tooltip("공격 시작 이펙트")]
    [SerializeField] private GameObject attackStartEffect;
    
    [Tooltip("히트 이펙트")]
    [SerializeField] private GameObject hitEffect;
    
    [Tooltip("공격 사운드")]
    [SerializeField] private AudioClip attackSound;
    
    [Tooltip("히트 사운드")]
    [SerializeField] private AudioClip hitSound;

    // Public Properties (Read-Only)
    public string AttackName => attackName;
    public AttackType AttackType => attackType;
    public string Description => description;
    public int BaseDamage => baseDamage;
    public float CriticalChance => criticalChance;
    public float CriticalMultiplier => criticalMultiplier;
    public float AttackCooldown => attackCooldown;
    public float AnimationDuration => animationDuration;
    public string AnimationTrigger => animationTrigger;
    public float AttackRange => attackRange;
    public float AttackAngle => attackAngle;
    public bool StopMovingWhileAttacking => stopMovingWhileAttacking;
    public GameObject ProjectilePrefab => projectilePrefab;
    public ProjectileTrajectoryType TrajectoryType => trajectoryType;
    public float ArcHeight => arcHeight;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileLifetime => projectileLifetime;
    public int ProjectileCount => projectileCount;
    public float MultiShotAngle => multiShotAngle;
    public List<StatusEffectData> OnHitEffects => onHitEffects;
    public List<float> EffectChances => effectChances;
    public GameObject AttackStartEffect => attackStartEffect;
    public GameObject HitEffect => hitEffect;
    public AudioClip AttackSound => attackSound;
    public AudioClip HitSound => hitSound;

    // AOE 설정 Properties
    public GameObject AOEEffectPrefab => aoeEffectPrefab;
    public float AOEDuration => aoeDuration;
    public AOEShapeType AOEShape => aoeShape;
    public float AOEScale => aoeScale;


    /// <summary>
    /// 레벨에 따른 스케일된 데미지 계산
    /// </summary>
    public int GetScaledDamage(int level, float levelMultiplier = 1.1f)
    {
        return Mathf.RoundToInt(baseDamage * Mathf.Pow(levelMultiplier, level - 1));
    }

    /// <summary>
    /// 크리티컬 데미지 계산
    /// </summary>
    public int GetCriticalDamage(int baseDamage)
    {
        return Mathf.RoundToInt(baseDamage * criticalMultiplier);
    }

    /// <summary>
    /// 크리티컬 여부 판정
    /// </summary>
    public bool RollCritical()
    {
        return Random.Range(0f, 1f) < criticalChance;
    }

    /// <summary>
    /// 상태이상 적용 여부 판정
    /// </summary>
    public List<StatusEffectData> RollStatusEffects()
    {
        List<StatusEffectData> appliedEffects = new List<StatusEffectData>();
        
        for (int i = 0; i < onHitEffects.Count; i++)
        {
            if (i < effectChances.Count)
            {
                if (Random.Range(0f, 1f) < effectChances[i])
                {
                    appliedEffects.Add(onHitEffects[i]);
                }
            }
            else
            {
                // 확률이 설정되지 않은 경우 100% 적용
                appliedEffects.Add(onHitEffects[i]);
            }
        }
        
        return appliedEffects;
    }

    /// <summary>
    /// 원거리 공격 유효성 검사
    /// </summary>
    public bool IsValidRangedAttack()
    {
        return attackType == AttackType.Ranged && projectilePrefab != null;
    }

    /// <summary>
    /// Inspector에서 설정값 검증
    /// </summary>
    private void OnValidate()
    {
        // 기본값 검증
        baseDamage = Mathf.Max(1, baseDamage);
        criticalChance = Mathf.Clamp01(criticalChance);
        criticalMultiplier = Mathf.Max(1f, criticalMultiplier);
        attackCooldown = Mathf.Max(0.1f, attackCooldown);
        animationDuration = Mathf.Max(0.1f, animationDuration);
        attackRange = Mathf.Max(0.1f, attackRange);
        attackAngle = Mathf.Clamp(attackAngle, 0f, 360f);
        projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
        projectileLifetime = Mathf.Max(0.1f, projectileLifetime);
        projectileCount = Mathf.Max(1, projectileCount);
        multiShotAngle = Mathf.Clamp(multiShotAngle, 0f, 360f);

        // 상태이상 확률 리스트 크기 맞춤
        while (effectChances.Count < onHitEffects.Count)
        {
            effectChances.Add(1f); // 기본 100% 확률
        }
        while (effectChances.Count > onHitEffects.Count)
        {
            effectChances.RemoveAt(effectChances.Count - 1);
        }

        // 확률값 0~1 범위로 제한
        for (int i = 0; i < effectChances.Count; i++)
        {
            effectChances[i] = Mathf.Clamp01(effectChances[i]);
        }

        // 타입별 기본 설정 적용
        ApplyTypeDefaults();
    }

    /// <summary>
    /// 공격 타입에 따른 기본 설정 적용
    /// </summary>
    private void ApplyTypeDefaults()
    {
        switch (attackType)
        {
            case AttackType.Melee:
                // 근접 공격은 발사체 설정 비활성화
                projectileCount = 1;
                // ✅ 수정: 근접 공격 범위 제한 완화 (2 → 5)
                if (attackRange > 5f) attackRange = 5f; // 근접은 최대 5까지
                break;
                
            case AttackType.Ranged:
                // 원거리 공격은 범위가 더 넓어야 함
                if (attackRange < 2f) attackRange = 3f; // 원거리는 최소 3
                if (projectileCount > 1 && multiShotAngle == 0f) multiShotAngle = 15f;
                break;
        }
    }

    /// <summary>
    /// 디버그용 정보 출력
    /// </summary>
    public string GetDebugInfo(int level = 1)
    {
        string info = $"{attackName} ({attackType})\n";
        info += $"Damage: {baseDamage} → Lv.{level}: {GetScaledDamage(level)}\n";
        info += $"Range: {attackRange}, Cooldown: {attackCooldown}s\n";
        info += $"Critical: {criticalChance * 100:F1}% (x{criticalMultiplier})\n";
        
        if (attackType == AttackType.Ranged)
        {
            info += $"Projectile: {(projectilePrefab != null ? projectilePrefab.name : "None")}\n";
            info += $"Speed: {projectileSpeed}, Count: {projectileCount}\n";
        }
        
        if (onHitEffects.Count > 0)
        {
            info += $"Status Effects: {onHitEffects.Count}개\n";
            for (int i = 0; i < onHitEffects.Count; i++)
            {
                if (onHitEffects[i] != null)
                {
                    float chance = i < effectChances.Count ? effectChances[i] : 1f;
                    info += $"  - {onHitEffects[i].EffectName} ({chance * 100:F0}%)\n";
                }
            }
        }
        
        return info;
    }
}

/// <summary>
/// 발사체 궤적 타입
/// </summary>
public enum ProjectileTrajectoryType
{
    Straight,   // 직선
    Arc         // 포물선
}
