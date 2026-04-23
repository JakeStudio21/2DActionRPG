using UnityEngine;

/// <summary>
/// 플레이어 키보드 입력 통합 관리자
///
/// [기본공격 파이프라인 - 3단계 원칙]
///   1. Check  : CanPerformAttack() — 공격 가능 여부 먼저 확인
///   2. Detect : AutoTargetResolver.FindBestTarget() — 가능할 때만 타겟 탐색
///   3. Execute: ForceSetFacing + OverrideDirectionThisFrame + TriggerAttack — 확정 후 실행
///
/// [스킬 - 수동 모드]
///   자동 타겟팅 없이 기존 방식대로 실행.
///   스킬 타입별(AOE, 발사체 등) 고유 설계는 별도로 진행.
/// </summary>
public class PlayerAttackInput : MonoBehaviour
{
    [Header("입력 설정")]
    [SerializeField] private bool enableKeyboardInput = true;

    [Header("기본공격 자동 타겟팅")]
    [SerializeField] private AutoTargetResolver autoTargetResolver;
    [SerializeField] private TargetingProfile basicAttackProfile;

    [Header("컴포넌트 참조 (자동 탐색, 인스펙터에서 오버라이드 가능)")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private ActiveWeapon activeWeapon;
    [SerializeField] private PlayerAnimationController playerAnimationController;

    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = false;

    // PC 빌드 전용: InGamePCInputHandler 가 공격 직전에 설정하는 마우스 에임 방향
    private Vector2 _pcAimDirOverride = Vector2.zero;
    private bool    _hasPCAimOverride = false;

    // ──────────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    void Start()
    {
        if (autoTargetResolver == null)
            autoTargetResolver = GetComponentInParent<AutoTargetResolver>()
                              ?? FindObjectOfType<AutoTargetResolver>();

        if (playerController == null)
            playerController = GetComponentInParent<PlayerController>()
                            ?? FindObjectOfType<PlayerController>();

        if (activeWeapon == null)
            activeWeapon = GetComponentInParent<ActiveWeapon>()
                        ?? FindObjectOfType<ActiveWeapon>();

        if (playerAnimationController == null)
            playerAnimationController = GetComponentInParent<PlayerAnimationController>()
                                     ?? FindObjectOfType<PlayerAnimationController>();
    }

    void Update()
    {
        // 공격/스킬 키 입력은 InGamePCInputHandler 에서 일괄 처리
    }

    #endregion

    // ──────────────────────────────────────────────────────────────────────────
    #region 기본공격 (자동 타겟팅)

    public void PerformAttack()
    {
        // ── 1단계: Check ──────────────────────────────────────────────────────
        if (playerAnimationController == null)
        {
            Debug.LogError("[PlayerAttackInput] PlayerAnimationController 없음");
            return;
        }

        if (!playerAnimationController.CanPerformAttack())
        {
            return;
        }

        // ── 2단계: Detect + Score ─────────────────────────────────────────────
        Vector2 attackDir = ResolveAttackDirection();

        // ── 3단계: Execute ────────────────────────────────────────────────────
        // 단일 진입점: 하나의 attackDir이 애니메이션 방향과 발사체 방향을 동시에 결정
        // 두 잠금은 OnAttackComplete()에서 함께 해제됨
        playerController?.LockAnimationDirection(attackDir);   // moveX/moveY/flipX 고정
        activeWeapon?.LockAttackDirection(attackDir);           // Bow.lastAttackRotation 고정
        playerAnimationController.TriggerAttack();

    }

    /// <summary>
    /// 자동 타겟팅으로 공격 방향을 결정합니다.
    /// - PC: SetPCAimDirection() 으로 주입된 마우스 방향을 기준으로 스코어링.
    ///        타겟이 있으면 그 방향, 없으면 마우스 방향을 반환.
    /// - 모바일: 이동/facing 방향 기준 자동 타겟팅, 없으면 FacingDirection 반환.
    /// </summary>
    private Vector2 ResolveAttackDirection()
    {
        // PC 마우스 에임 오버라이드 소비 (한 번만 사용)
        bool hasPCDir = _hasPCAimOverride;
        Vector2 pcDir = _pcAimDirOverride;
        _hasPCAimOverride = false;

        if (autoTargetResolver == null || basicAttackProfile == null)
            return hasPCDir ? pcDir : GetFacingDirection();

        Vector2 aimDir = hasPCDir ? pcDir : GetAimDir();
        ITargetable target = autoTargetResolver.FindBestTarget(aimDir, basicAttackProfile);

        if (target != null && target.IsAlive())
        {
            Transform t = target.GetTransform();
            if (t != null)
            {
                Vector2 fireOrigin = GetFireOrigin();
                Vector2 toTarget = (Vector2)t.position - fireOrigin;
                if (toTarget.sqrMagnitude > 0.001f)
                    return toTarget.normalized;
            }
        }

        // 타겟 없음: PC면 마우스 방향, 모바일이면 facing 방향
        return hasPCDir ? pcDir : GetFacingDirection();
    }

    /// <summary>
    /// 실제 발사 위치를 반환합니다.
    /// 현재 장착된 무기의 SpawnPoint를 찾고, 없으면 무기 위치, 그것도 없으면 자신의 위치를 사용합니다.
    /// </summary>
    private Vector2 GetFireOrigin()
    {
        if (activeWeapon?.CurrentActiveWeapon != null)
        {
            Transform weapon = activeWeapon.CurrentActiveWeapon.transform;
            string[] spawnNames = { "Arrow Spawn Point", "SpawnPoint", "Spawn Point", "FirePoint", "Fire Point", "Muzzle" };
            foreach (string spawnName in spawnNames)
            {
                Transform sp = weapon.Find(spawnName);
                if (sp != null) return sp.position;
            }
            return weapon.position;
        }
        return transform.position;
    }

    /// <summary>
    /// PC: InGamePCInputHandler 가 공격 전 SetPCAimDirection() 으로 마우스 방향을 주입.
    /// 주입된 방향이 있으면 그것을 우선 사용하고 1회 소비(클리어)한다.
    /// </summary>
    public void SetPCAimDirection(Vector2 dir)
    {
        _pcAimDirOverride = dir;
        _hasPCAimOverride = true;
    }

    /// <summary>
    /// 자동 타겟팅 스코어링에 사용할 에임 방향을 반환합니다.
    /// PC 오버라이드는 ResolveAttackDirection() 에서 직접 소비하므로 여기서는 처리하지 않습니다.
    /// </summary>
    private Vector2 GetAimDir()
    {
        if (playerController == null) return Vector2.right;
        Vector2 move = playerController.Movement;
        return move.sqrMagnitude > 0.01f ? move.normalized : playerController.FacingDirection;
    }

    private Vector2 GetFacingDirection()
    {
        if (playerController == null) return Vector2.right;
        Vector2 facing = playerController.FacingDirection;
        return facing.sqrMagnitude > 0.001f ? facing : Vector2.right;
    }

    #endregion

    // ──────────────────────────────────────────────────────────────────────────
    #region 스킬 (수동 모드 — 자동 타겟팅 없음)

    public void PerformSkill()
    {

        if (playerAnimationController != null)
        {
            bool success = playerAnimationController.TriggerSkill1();
            return;
        }

        // fallback: PlayerAnimationController 없을 때
        var skillController = FindObjectOfType<SkillController>();
        if (skillController != null)
            skillController.TriggerSkill();
        else
            Debug.LogWarning("[PlayerAttackInput] SkillController 없음");
    }

    public void PerformSkill2()
    {

        if (playerAnimationController != null)
        {
            bool success = playerAnimationController.TriggerSkill2();
            return;
        }

        // fallback: PlayerAnimationController 없을 때
        var skillController = FindObjectOfType<SkillController>();
        if (skillController != null)
            skillController.TriggerSkill2();
        else
            Debug.LogWarning("[PlayerAttackInput] SkillController 없음");
    }

    #endregion
}
