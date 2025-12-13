using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 스킬 타입 (패턴별 분류)
/// </summary>
public enum SkillType
{
    AOE,        // 일반 AOE (Circle, Fan, Rectangle)
    Dash,       // 돌진 스킬
    Projectile, // 멀티샷/발사체
    Buff,       // 버프/디버프 (향후 확장)
    Summon      // 소환 (향후 확장)
}

/// <summary>
/// 스킬 데이터 ScriptableObject
/// 엘리트/보스 몬스터의 스킬 정보를 정의
/// AttackData와 분리하여 스킬만의 특성 관리
/// </summary>
[CreateAssetMenu(fileName = "SkillData", menuName = "Enemy/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("🏷️ 스킬 정보")]
    [Tooltip("스킬 타입 (패턴별 분류)")]
    [SerializeField] private SkillType skillType = SkillType.AOE;
    
    [Tooltip("스킬 표시 이름")]
    [SerializeField] private string skillName = "스킬";
    
    [Tooltip("스킬 고유 ID (시스템 내부용)")]
    [SerializeField] private string skillId = "";
    
    [TextArea(2, 3)]
    [SerializeField] private string description = "스킬 설명";

    [Header("⏱️ 타이밍 설정")]
    [Tooltip("캐스팅 시간 (초) - Telegraph 표시 시간")]
    [SerializeField] private float castTime = 0.8f;
    
    [Tooltip("스킬 쿨다운 (초) - Inspector에서 조절 가능")]
    [SerializeField] private float cooldown = 8f;
    
    [Tooltip("스킬 실행 시간 (초) - Action 애니메이션 길이")]
    [SerializeField] private float actionDuration = 0.5f;

    [Header("💥 데미지 설정")]
    [Tooltip("평타 대비 데미지 배율 (2.0 = 평타의 2배)")]
    [SerializeField] private float damageMultiplier = 2.5f;
    
    [Tooltip("크리티컬 가능 여부")]
    [SerializeField] private bool canCritical = true;
    
    [Tooltip("추가 크리티컬 확률 보너스 (기본 확률에 더해짐)")]
    [Range(0f, 1f)]
    [SerializeField] private float criticalChanceBonus = 0.05f;

    [Header("🎯 AOE 설정")]
    [Tooltip("AOE 형태 (원형/부채꼴/직사각형)")]
    [SerializeField] private AOEShapeType aoeShape = AOEShapeType.Circle;
    
    [Tooltip("AOE 반경 (원형/부채꼴용)")]
    [SerializeField] private float aoeRadius = 5f;
    
    [Tooltip("AOE 크기 (직사각형용 - X: 너비, Y: 높이)")]
    [SerializeField] private Vector2 aoeSize = new Vector2(4f, 6f);
    
    [Tooltip("부채꼴 각도 (0~360도)")]
    [Range(0f, 360f)]
    [SerializeField] private float aoeAngle = 90f;
    
    [Tooltip("AOE 중심점 오프셋 (몬스터로부터의 거리) - Deprecated: EliteSkillController의 SpawnPoint 사용 권장")]
    [SerializeField] private Vector2 aoeOffset = Vector2.zero;
    
    [Tooltip("AOE 중심점 계산 모드 (Centered: 보스 중심, ForwardAnchored: 보스 앞쪽으로 오프셋)")]
    [SerializeField] private AOECenterMode aoeCenterMode = AOECenterMode.Centered;
    
    [Tooltip("AOE 중심점 오프셋 거리 (ForwardAnchored 모드일 때만 사용, AoeRadius 대비 비율 또는 절대값)")]
    [SerializeField] private float aoeCenterOffset = 0f;

    [Header("📍 텔레그래프 (경고 표시)")]
    [Tooltip("텔레그래프 프리팹 (바닥 경고 이펙트)")]
    [SerializeField] private GameObject telegraphPrefab;
    
    [Tooltip("텔레그래프 표시 시간 (초) - 플레이어 회피 시간")]
    [SerializeField] private float telegraphDuration = 0.5f;
    
    [Tooltip("텔레그래프 색상")]
    [SerializeField] private Color telegraphColor = new Color(1f, 0f, 0f, 0.5f); // 반투명 빨간색

    [Header("🎨 이펙트")]
    [Tooltip("캐스팅 시작 이펙트 (몬스터 주변)")]
    [SerializeField] private GameObject castEffect;
    
    [Tooltip("AOE 공격 이펙트 (폭발, 충격파 등)")]
    [SerializeField] private GameObject aoeEffect;
    
    [Tooltip("플레이어 타격 이펙트")]
    [SerializeField] private GameObject hitEffect;

    [Header("🔊 사운드")]
    [Tooltip("캐스팅 시작 사운드")]
    [SerializeField] private AudioClip castSound;
    
    [Tooltip("스킬 발동 사운드 (폭발음, 충격음 등)")]
    [SerializeField] private AudioClip skillSound;
    
    [Tooltip("플레이어 타격 사운드")]
    [SerializeField] private AudioClip hitSound;

    [Header("📳 스크린 셰이크")]
    [Tooltip("스크린 셰이크 강도 (0.5=약함, 1.0=보통, 2.0=강함)")]
    [SerializeField] private float shakeIntensity = 0.5f;

    [Header("🎮 추가 설정")]
    [Tooltip("스킬 사용 가능 최소 거리")]
    [SerializeField] private float minRange = 0f;
    
    [Tooltip("스킬 사용 가능 최대 거리")]
    [SerializeField] private float maxRange = 10f;
    
    [Tooltip("스킬 사용 시 이동 정지 여부")]
    [SerializeField] private bool stopMovingWhileCasting = true;

    // Public Properties (Read-Only)
    public SkillType SkillType => skillType;
    public string SkillName => skillName;
    public string SkillId => skillId;
    public string Description => description;
    public float CastTime => castTime;
    public float Cooldown => cooldown;
    public float ActionDuration => actionDuration;
    public float DamageMultiplier => damageMultiplier;
    public bool CanCritical => canCritical;
    public float CriticalChanceBonus => criticalChanceBonus;
    public AOEShapeType AoeShape => aoeShape;
    public float AoeRadius => aoeRadius;
    public Vector2 AoeSize => aoeSize;
    public float AoeAngle => aoeAngle;
    public Vector2 AoeOffset => aoeOffset;
    public AOECenterMode AoeCenterMode => aoeCenterMode;
    public float AoeCenterOffset => aoeCenterOffset;
    public GameObject TelegraphPrefab => telegraphPrefab;
    public float TelegraphDuration => telegraphDuration;
    public Color TelegraphColor => telegraphColor;
    public GameObject CastEffect => castEffect;
    public GameObject AoeEffect => aoeEffect;
    public GameObject HitEffect => hitEffect;
    public AudioClip CastSound => castSound;
    public AudioClip SkillSound => skillSound;
    public AudioClip HitSound => hitSound;
    public float ShakeIntensity => shakeIntensity;
    public float MinRange => minRange;
    public float MaxRange => maxRange;
    public bool StopMovingWhileCasting => stopMovingWhileCasting;

    /// <summary>
    /// 레벨과 기본 데미지를 적용한 실제 스킬 데미지 계산
    /// </summary>
    public int GetScaledDamage(int baseAttackDamage)
    {
        return Mathf.RoundToInt(baseAttackDamage * damageMultiplier);
    }

    /// <summary>
    /// 거리 체크 - 스킬 사용 가능 범위 내인지 확인
    /// </summary>
    public bool IsInRange(float distance)
    {
        return distance >= minRange && distance <= maxRange;
    }

    /// <summary>
    /// Inspector에서 설정값 검증
    /// </summary>
    private void OnValidate()
    {
        // 기본값 검증
        castTime = Mathf.Max(0.1f, castTime);
        cooldown = Mathf.Max(0.5f, cooldown);
        actionDuration = Mathf.Max(0.1f, actionDuration);
        damageMultiplier = Mathf.Max(1f, damageMultiplier);
        criticalChanceBonus = Mathf.Clamp01(criticalChanceBonus);
        
        // AOE 설정 검증
        aoeRadius = Mathf.Max(0.5f, aoeRadius);
        aoeSize.x = Mathf.Max(0.5f, aoeSize.x);
        aoeSize.y = Mathf.Max(0.5f, aoeSize.y);
        aoeAngle = Mathf.Clamp(aoeAngle, 0f, 360f);
        
        // Telegraph 설정 검증
        telegraphDuration = Mathf.Max(0.1f, telegraphDuration);
        
        // 범위 검증
        minRange = Mathf.Max(0f, minRange);
        maxRange = Mathf.Max(minRange, maxRange);
        
        // 스크린 셰이크 검증
        shakeIntensity = Mathf.Max(0f, shakeIntensity);

        // SkillId가 비어있으면 경고
        if (string.IsNullOrEmpty(skillId))
        {
            Debug.LogWarning($"[SkillData] {skillName}: SkillId가 설정되지 않았습니다!");
        }
    }

    /// <summary>
    /// 디버그용 정보 출력
    /// </summary>
    public string GetDebugInfo(int baseAttackDamage = 10)
    {
        string info = $"=== {skillName} ({skillId}) ===\n";
        info += $"Type: {skillType}\n";
        info += $"Cast: {castTime}s, Cooldown: {cooldown}s\n";
        info += $"Damage: {baseAttackDamage} x {damageMultiplier} = {GetScaledDamage(baseAttackDamage)}\n";
        info += $"AOE: {aoeShape}";
        
        switch (aoeShape)
        {
            case AOEShapeType.Circle:
                info += $" (Radius: {aoeRadius})\n";
                break;
            case AOEShapeType.Triangle: // Fan
                info += $" (Radius: {aoeRadius}, Angle: {aoeAngle}°)\n";
                break;
            case AOEShapeType.Rectangle:
                info += $" (Size: {aoeSize.x} x {aoeSize.y})\n";
                break;
        }
        
        info += $"Range: {minRange}~{maxRange}\n";
        info += $"Telegraph: {telegraphDuration}s\n";
        info += $"Shake: {shakeIntensity}\n";
        
        return info;
    }

    /// <summary>
    /// CueSystem 이벤트 키 자동 생성
    /// </summary>
    public string GetCastEventKey()
    {
        return $"skill.{(string.IsNullOrEmpty(skillId) ? "generic" : skillId)}.cast";
    }

    public string GetTelegraphEventKey()
    {
        return $"skill.{(string.IsNullOrEmpty(skillId) ? "generic" : skillId)}.telegraph";
    }

    public string GetAoeEventKey()
    {
        return $"skill.{(string.IsNullOrEmpty(skillId) ? "generic" : skillId)}.aoe";
    }

    public string GetHitEventKey()
    {
        return $"skill.{(string.IsNullOrEmpty(skillId) ? "generic" : skillId)}.hit";
    }
}


