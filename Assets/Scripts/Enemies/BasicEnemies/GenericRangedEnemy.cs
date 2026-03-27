using UnityEngine;

/// <summary>
/// 범용 원거리 몬스터 클래스
/// Ghost(MultiShot), Grape(Ranged), TowerMonster(Fixed), PlantsMonster(Fixed),
/// GoblinBomb, GoblinCart, CrystalGolem(AOE), WaterGolem(AOE) 등 Ranged형 전체 대체
///
/// 공격 필드 타입이 BaseAttackBehaviour이므로 인스펙터에서 아래 모두 할당 가능:
///   - RangedAttack
///   - MultiShotRangedAttack
///   - AOEAttack
///
/// 고정형 몬스터(TowerMonster 등) 설정:
///   EnemyData.PatrolRadius = 0, EnemyData.ChaseRange = 0 으로 설정
///   → FSM이 자연스럽게 순찰/추격 없이 동작
/// </summary>
public class GenericRangedEnemy : BaseEnemy
{
    [Header("공격 컴포넌트 (필수)")]
    [Tooltip("RangedAttack / MultiShotRangedAttack / AOEAttack 중 하나를 할당\n" +
             "비워두면 같은 오브젝트에서 자동 탐색 (BaseAttackBehaviour 우선)")]
    [SerializeField] private BaseAttackBehaviour rangedAttack;

    [Header("아이소메트릭 설정")]
    [SerializeField] private IsometricCharacterData isometricData = new IsometricCharacterData();

    // ─── BaseEnemy 추상 속성 구현 ───────────────────────────────────

    /// <summary>
    /// 공격 범위 — BaseAttackBehaviour.AttackData에서 읽기
    /// </summary>
    public override float AttackRange
    {
        get
        {
            if (rangedAttack != null && rangedAttack.AttackData != null)
                return rangedAttack.AttackData.AttackRange;

            Debug.LogError($"[GenericRangedEnemy] {gameObject.name}: 공격 컴포넌트 또는 AttackData가 없습니다!");
            return 5f;
        }
    }

    // DetectionRange / ChaseRange / PatrolRadius 는 BaseEnemy의 virtual 구현 사용
    // → EnemyData.DetectionRange / EnemyData.ChaseRange / EnemyData.PatrolRadius 자동 참조

    // ─── BaseEnemy 추상 메서드 구현 ────────────────────────────────

    protected override void OnAwakeInitialize()
    {
        if (rangedAttack == null)
            rangedAttack = GetComponent<BaseAttackBehaviour>();

        if (rangedAttack == null)
            Debug.LogError($"[GenericRangedEnemy] {gameObject.name}: 공격 컴포넌트가 없습니다! " +
                           "(RangedAttack / MultiShotRangedAttack / AOEAttack 중 하나 필요)");
    }

    protected override void OnStartInitialize() { }

    protected override void InitializeAttackSystem()
    {
        if (rangedAttack != null)
            rangedAttack.Initialize();
        else
            Debug.LogError($"[GenericRangedEnemy] {gameObject.name}: 공격 컴포넌트 초기화 실패 — 컴포넌트를 할당해주세요.");
    }

    /// <summary>
    /// FSM EnemyAttackState에서 호출 — 원거리 공격 실행
    /// 실제 발사체 생성은 EnemyAttackStateBehaviour(SMB)가 normalizedTime 기준으로 처리
    /// </summary>
    public override void Attack()
    {
        if (rangedAttack != null && rangedAttack.CanAttack())
            rangedAttack.Attack();
    }

    public override IsometricCharacterData GetIsometricData()
    {
        if (isometricData == null || !isometricData.IsValid())
            return CreateDefaultIsometricData();
        return isometricData;
    }

    // ─── 유틸리티 ──────────────────────────────────────────────────

    [ContextMenu("Test Ranged Damage")]
    private void TestRangedDamage()
    {
        if (rangedAttack != null)
            Debug.Log($"[GenericRangedEnemy] {gameObject.name} 실제 원거리 데미지: {rangedAttack.GetScaledDamage()}");
        else
            Debug.LogError($"[GenericRangedEnemy] 공격 컴포넌트 없음");
    }

    private void OnValidate()
    {
        if (isometricData == null)
        {
            isometricData = new IsometricCharacterData();
            isometricData.SetDefaults();
        }

        if (!isometricData.IsValid())
            isometricData.SetDefaults();
    }
}
