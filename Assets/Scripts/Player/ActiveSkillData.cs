using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 액티브 스킬 전용 데이터 (통합 설계)
/// 근거리/원거리, 투사체/즉발형 등 모든 액티브 스킬을 커버 (Phase 1)
/// </summary>
[CreateAssetMenu(fileName = "ActiveSkill_", menuName = "Skill System/Active Skill Data")]
public class ActiveSkillData : BaseSkillData
{
    [Header("🎯 스킬 분류")]
    [Tooltip("요구 직업 (None = 공용)")]
    public PlayerType requiredClass = PlayerType.None;
    
    [Tooltip("스킬 특성 (광역기/단일기)")]
    public ActiveSkillType skillType = ActiveSkillType.WaveClear;
    
    [Tooltip("투사체 발사 여부 (true: 화살/마법탄, false: 근접/즉발)")]
    public bool isProjectile = false;
    
    [Tooltip("애니메이션 트리거 이름 (예: 'Skill1', 'Dash')")]
    public string animTriggerName = "Skill1";
    
    [Header("⚡ 전투 수치 (기본값, 레벨 1 기준)")]
    [Tooltip("기본 쿨다운 시간 (초)")]
    public float baseCooldown = 2f;
    
    [Tooltip("기본 데미지 배율 (%) - 플레이어 공격력의 배수")]
    public float baseDamageMultiplier = 150f; // 150% = 기본 공격력의 1.5배
    
    [Tooltip("사거리")]
    public float range = 5f;
    
    [Header("🎵 CueSystem 이벤트 키 (VFX/SFX 연결)")]
    [Tooltip("시전 이펙트 키 (예: skill.assasin.multi_arrow.cast)\n비어 있으면 player_base의 generic 키로 fallback")]
    public string castCueKey;
    
    [Tooltip("AOE 범위 이펙트 키 (예: skill.assasin.multi_arrow.aoe)\n즉발형(isProjectile=false)에서 AOE 시각화에 사용")]
    public string aoeCueKey;
    
    [Tooltip("타격 이펙트 키 (예: skill.assasin.multi_arrow.hit)\n피격 대상 위치에 VFX/SFX/Shake를 발동시킴\n비어 있으면 타격 연출 없음")]
    public string hitCueKey;
    
    [Header("🎭 AOE 설정")]
    [Tooltip("AOE 형태 (기존 시스템 호환)")]
    public SkillAOEShape aoeShape = SkillAOEShape.Circle;
    
    [Tooltip("AOE 크기")]
    public Vector2 aoeSize = new Vector2(3f, 3f);
    
    [Tooltip("AOE 반경 (Circle일 때)")]
    public float aoeRadius = 3f;
    
    [Tooltip("부채꼴 각도 (Fan일 때)")]
    public float aoeFanAngle = 90f;
    
    [Tooltip("AOE 지속시간")]
    public float aoeDuration = 0.5f;
    
    [Header("📍 Telegraph 설정")]
    [Tooltip("경고 텔레그래프 프리팹 (선택)")]
    public GameObject telegraphPrefab;
    
    [Tooltip("텔레그래프 표시 시간")]
    public float telegraphDuration = 0.3f;
    
    [Tooltip("텔레그래프 위치 오프셋 (공격 방향 기준)\nX: 전방 거리 (양수 = 앞, 음수 = 뒤)\nY: 측면 거리 (양수 = 우측, 음수 = 좌측)")]
    public Vector2 telegraphOffset = Vector2.zero;
    
    [Header("⏱️ 이펙트 타이밍")]
    [Tooltip("Animation Event 이후 Cast Effect 발동까지의 지연 (초)\n0 = 즉시 발동")]
    public float castEffectDelay = 0f;
    
    [Tooltip("Telegraph 종료 이후 AOE Effect/데미지 발동까지의 추가 지연 (초)\nTelegraph 없는 스킬: Cast Effect 이후 지연\n0 = 즉시 발동")]
    public float aoeEffectDelay = 0f;
    
    [Header("🎯 특수 속성")]
    [Tooltip("관통 공격 여부")]
    public bool isPiercing = false;
    
    [Tooltip("다단 히트 횟수")]
    public int multiHitCount = 1;
    
    [Tooltip("발사체 개수 (투사체 스킬용) - 기본값 1")]
    public int projectileCount = 1;
    
    [Header("🔄 투사체 설정 (isProjectile = true일 때 사용)")]
    [Tooltip("투사체 프리팹 (화살, 마법탄 등)")]
    public GameObject projectilePrefab;
    
    [Tooltip("투사체 속도")]
    public float projectileSpeed = 10f;
    
    [Tooltip("투사체 퍼짐 각도 (다발 발사 시)")]
    public float spreadAngle = 0f;
    
    [Tooltip("투사체 크기 배율")]
    public Vector3 projectileScale = Vector3.one;
    
    [Tooltip("투사체 풀 이름 (오브젝트 풀링용)")]
    public string projectilePoolName = "Projectile";
    
    [Header("🗡️ 근접 스킬 설정 (isProjectile = false일 때 사용)")]
    [Tooltip("돌진 속도 (근접 돌진형 스킬용)")]
    public float dashSpeed = 20f;
    
    [Tooltip("돌진 최대 거리")]
    public float dashRange = 8f;
    
    [Tooltip("연속 공격 횟수")]
    public int attackCount = 1;
    
    [Tooltip("각 공격 간 딜레이")]
    public float attackDelay = 0.3f;
    
    [Tooltip("공격 범위 반경 (근접 판정용)")]
    public float attackRadius = 2f;
    
    [Tooltip("적 기절 지속시간 (초)")]
    public float stunDuration = 1f;
    
    [Header("💥 폭발형 설정 (Explosive Arrow 등)")]
    [Tooltip("투사체 소멸 시 폭발 AOE 생성 여부")]
    public bool hasExplosionOnHit = false;
    
    [Tooltip("폭발 데미지 = 직격 최종 데미지 × 이 배율 (1.2 = 120%)")]
    public float explosionDamageRatio = 1.2f;
    
    // 폭발 반경은 위의 aoeRadius 필드를 공용으로 사용합니다
    
    [Tooltip("폭발 VFX/SFX Cue 키 (예: skill.archer.explosive_arrow.explosion)")]
    public string explosionCueKey;
    
    [Header("🔫 연사형 설정 (Double Shot 등)")]
    [Tooltip("연사 모드 여부 (true = 발사체를 순차 발사)")]
    public bool isBurstFire = false;
    
    [Tooltip("연사 발사 간격 (초) — 각 발사체 사이의 딜레이")]
    public float burstInterval = 0.15f;
    
    [Header("⛓️ 체인 샷 설정 (Chain Shot 등)")]
    [Tooltip("체인 발사체 여부 — true이면 적 적중 시 파괴 없이 다음 적으로 방향을 꺾어 날아감")]
    public bool isChainShot = false;
    
    [Tooltip("최대 연쇄 횟수 (CSV CHAIN_COUNT로 레벨별 오버라이드 가능)")]
    public int maxChainCount = 4;
    
    [Tooltip("다음 적 탐색 반경 (Unity units)")]
    public float chainRadius = 5.0f;
    
    [Tooltip("연쇄 1회당 데미지 감소율 (0.1 = 10% 감소, 첫 타격은 100%)")]
    public float chainDamageReduction = 0.1f;
    
    [Tooltip("체인 타격 이펙트 CueKey (CueProfile에 등록된 키 — 예: chain_shot.hit)\n" +
             "VFX + SFX + CameraShake 모두 CueEntry 한 곳에서 관리 (Explosive Arrow의 explosionCueKey 패턴)")]
    public string chainHitCueKey;
    
    [Tooltip("적 타격 후 다음 타겟으로 날아가기 전 공중 정지 시간(초) — 타격감(Hit-Stop) 제어용\n" +
             "0.05: 빠른 연쇄, 0.15: 명확한 타격감, 0.0: 즉시 이동(기존 동작)")]
    public float chainDelay = 0.05f;
    
    [Header("☠️ DOT 장판 설정 (Poison Field 등)")]
    [Tooltip("장판형 DOT 스킬 여부 (true = DotDamageArea 사용, isProjectile=false와 함께 사용)")]
    public bool isDotAoe = false;
    
    [Tooltip("장판 유지 시간 (초)")]
    public float dotDuration = 4.0f;
    
    [Tooltip("데미지 틱 주기 (초) — 이 주기마다 1회 데미지 판정")]
    public float dotTickRate = 0.5f;
    
    [Tooltip("이동속도 감소 비율 (0.25 = 25% 감소)")]
    public float slowPercentage = 0.25f;
    
    public override SkillCategory GetSkillCategory() => SkillCategory.Active;
}

/// <summary>
/// 액티브 스킬 타입 (기획서 기준)
/// </summary>
public enum ActiveSkillType
{
    [Tooltip("광역 클리어형 스킬")]
    WaveClear,      // 광역기 (다수 적 처리)
    
    [Tooltip("단일 대상 고화력 스킬")]
    BossBurst       // 단일기 (보스 딜링)
}

/// <summary>
/// ActiveSkillType을 한글 문자열로 변환하는 확장 메서드
/// </summary>
public static class ActiveSkillTypeExtensions
{
    public static string ToKoreanString(this ActiveSkillType type)
    {
        switch (type)
        {
            case ActiveSkillType.WaveClear:
                return "웨이브형";
            case ActiveSkillType.BossBurst:
                return "보스형";
            default:
                return type.ToString();
        }
    }
}
