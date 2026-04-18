using UnityEngine;

/// <summary>
/// 범용 근접 몬스터 클래스
/// BlueSlime, GoblinWarrior, Bear, Beetle, Spider, LadyBug,
/// Cobra, Scorpion, Sphinx, Anubis, Mimic, StoneGolem 등 Melee형 전체 대체
///
/// 사용법:
///   1. 이 스크립트를 몬스터 프리팹에 추가
///   2. MeleeAttack 컴포넌트를 같은 오브젝트에 추가하고 AttackData 할당
///   3. EnemyData, GrowthProfile, IsometricData 인스펙터에서 할당
/// </summary>
public class GenericMeleeEnemy : BaseEnemy
{
    [Header("공격 컴포넌트 (필수)")]
    [Tooltip("같은 오브젝트의 MeleeAttack 컴포넌트를 할당하거나 비워두면 자동 탐색")]
    [SerializeField] private MeleeAttack meleeAttack;

    [Header("아이소메트릭 설정")]
    [SerializeField] private IsometricCharacterData isometricData = new IsometricCharacterData();

    // ─── BaseEnemy 추상 속성 구현 ───────────────────────────────────

    /// <summary>
    /// 공격 범위 — MeleeAttack의 AttackData에서 읽기
    /// </summary>
    public override float AttackRange
    {
        get
        {
            if (meleeAttack != null && meleeAttack.AttackData != null)
                return meleeAttack.AttackData.AttackRange;

            Debug.LogError($"[GenericMeleeEnemy] {gameObject.name}: MeleeAttack 또는 AttackData가 없습니다!");
            return 1f;
        }
    }

    // DetectionRange / ChaseRange / PatrolRadius 는 BaseEnemy의 virtual 구현 사용
    // → EnemyData.DetectionRange / EnemyData.ChaseRange / EnemyData.PatrolRadius 자동 참조

    // ─── BaseEnemy 추상 메서드 구현 ────────────────────────────────

    protected override void OnAwakeInitialize()
    {
        if (meleeAttack == null)
            meleeAttack = GetComponent<MeleeAttack>();

        if (meleeAttack == null)
            Debug.LogError($"[GenericMeleeEnemy] {gameObject.name}: MeleeAttack 컴포넌트가 없습니다!");
    }

    protected override void OnStartInitialize() { }

    protected override void InitializeAttackSystem()
    {
        if (meleeAttack != null)
            meleeAttack.Initialize();
        else
            Debug.LogError($"[GenericMeleeEnemy] {gameObject.name}: MeleeAttack 초기화 실패 — 컴포넌트를 할당해주세요.");
    }

    /// <summary>
    /// FSM EnemyAttackState에서 호출 — 근접 공격 실행
    /// 실제 히트 판정은 EnemyAttackStateBehaviour(SMB)가 normalizedTime 기준으로 처리
    /// </summary>
    public override void Attack()
    {
        if (meleeAttack != null && meleeAttack.CanAttack())
            meleeAttack.Attack();
    }

    public override IsometricCharacterData GetIsometricData()
    {
        if (isometricData == null || !isometricData.IsValid())
            return CreateDefaultIsometricData();
        return isometricData;
    }

    // ─── 유틸리티 ──────────────────────────────────────────────────

    [ContextMenu("Test Melee Damage")]
    private void TestMeleeDamage()
    {
        if (meleeAttack == null)
            Debug.LogError($"[GenericMeleeEnemy] MeleeAttack 없음");
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
