using UnityEngine;
using CueSystem;

/// <summary>
/// SimpleMob 계열 몬스터의 스탯 데이터 컨테이너 (ScriptableObject)
/// - 모든 SimpleMob 서브클래스가 공유하는 Fat SO 구조
/// - null이면 프리팹 Inspector 직접값 사용 (하위 호환)
/// </summary>
[CreateAssetMenu(fileName = "SimpleMobData", menuName = "Game/SimpleMob/SimpleMobData")]
public class SimpleMobData : ScriptableObject
{
    [Header("성장 시스템")]
    [Tooltip("씬 레벨에 따른 스탯 성장 프로필. null이면 성장 없이 고정 스탯 사용.")]
    public MonsterGrowthProfile growthProfile;

    [Tooltip("스테이지 기준 레벨(StageBaseLevel)에 더할 오프셋.\n" +
             "예: StageBaseLevel=5, levelOffset=2 → 실제 레벨 7.\n" +
             "같은 씬에서 약한/강한 변형을 구분할 때 사용.")]
    public int levelOffset = 0;

    [Header("기본 스탯")]
    [Tooltip("최대 체력")]
    public int maxHp = 10;

    [Tooltip("이동 속도")]
    public float moveSpeed = 2f;

    [Tooltip("플레이어 접촉 시 가하는 데미지")]
    public float collisionDamage = 5f;

    [Tooltip("사망 애니메이션 종료 후 풀 반환까지 대기 시간(초). " +
             "Die 애니메이션 클립 길이와 맞추세요.")]
    public float dieDelay = 0.5f;

    [Header("체력바")]
    [Tooltip("피격 시 표시할 체력바 프리팹 (BasicEnemyHealthBarUI 컴포넌트 포함).")]
    public GameObject healthBarPrefab;

    [Tooltip("몬스터 머리 위 체력바 오프셋 Y값")]
    public float healthBarOffsetY = 1.2f;

    [Header("넉백")]
    [Tooltip("플레이어 공격에 맞았을 때 넉백 강도. 0이면 넉백 없음.")]
    public float knockbackForce = 4f;

    [Header("이동 정지 조건 (접촉형 SimpleMob)")]
    [Tooltip("플레이어와 이 거리 이하가 되면 이동을 멈추고 공격 대기 상태로 전환.\n" +
             "Shooter/OrbitalShooter의 attackRange와는 별개입니다.")]
    public float contactRange = 1.0f;

    [Tooltip("개인 목표점(플레이어 주변 오프셋)에 이 거리 이하로 접근하면 미세 진동 방지를 위해 이동을 멈춥니다.")]
    public float targetStopDistance = 0.15f;

    [Header("군집 분산 (Flocking)")]
    [Tooltip("스폰 시 기본 이동속도에 적용할 랜덤 배율 최솟값 (0.8 = 20% 느리게)")]
    public float speedJitterMin = 0.8f;

    [Tooltip("스폰 시 기본 이동속도에 적용할 랜덤 배율 최댓값 (1.2 = 20% 빠르게)")]
    public float speedJitterMax = 1.2f;

    [Tooltip("플레이어 중심 기준 개인 목표점 오프셋 반경. 클수록 몬스터가 넓게 포위함.")]
    public float targetOffsetRadius = 1.5f;

    [Tooltip("분리력을 적용할 주변 몬스터 감지 반경 (미터). 이 범위 안 몬스터끼리 밀어냄.")]
    public float separationRadius = 1.2f;

    [Tooltip("분리력 강도. 값이 클수록 더 강하게 서로 밀어냄.")]
    public float separationStrength = 2.0f;

    [Header("Splitter 전용 (분열형)")]
    [Tooltip("사망 시 스폰할 자식 몬스터의 풀 태그. 비어있으면 분열 안 함.")]
    public string childPoolTag = "";

    [Tooltip("사망 시 스폰할 자식 몬스터 수")]
    public int childCount = 2;

    [Tooltip("자식 몬스터 스폰 위치 랜덤 반경")]
    public float childSpawnRadius = 0.5f;

    [Header("Shooter 공통 (사수 계열)")]
    [Tooltip("이 거리 이내로 플레이어가 들어오면 발사 모드 진입")]
    public float attackRange = 6f;

    [Tooltip("투사체 발사 쿨다운 (초)")]
    public float fireRate = 2f;

    [Tooltip("발사할 투사체의 풀 태그. GamePoolManager에 등록되어 있어야 합니다.")]
    public string projectilePoolTag = "SimpleMobProjectile";

    [Header("OrbitalShooter 전용 (궤도형 사수)")]
    [Tooltip("플레이어 중심으로 공전할 목표 반경")]
    public float orbitRadius = 4f;

    [Header("피드백 — 피격")]
    [Tooltip("피격 시 재생할 통합 사운드/이펙트 프로필.\n" +
             "CueProfile SO의 entries에 hitCueEventKey와 동일한 eventKey를 등록해야 합니다.")]
    public CueProfile hitCueProfile;

    [Tooltip("hitCueProfile의 entries에 등록된 이벤트 키.\n" +
             "예: \"hit.enemy.normal\"  — CueProfile SO의 eventKey 값과 정확히 일치해야 합니다.")]
    public string hitCueEventKey = "hit.enemy.normal";

    [Header("피드백 — 자폭 (Exploder 전용)")]
    [Tooltip("자폭 시 재생할 통합 사운드/이펙트 프로필.\n" +
             "CueProfile SO의 entries에 explodeCueEventKey와 동일한 eventKey를 등록해야 합니다.")]
    public CueProfile explodeCueProfile;

    [Tooltip("explodeCueProfile의 entries에 등록된 이벤트 키.\n" +
             "예: \"explode.enemy.normal\"  — CueProfile SO의 eventKey 값과 정확히 일치해야 합니다.")]
    public string explodeCueEventKey = "explode.enemy.normal";
}
