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
        if (!enableKeyboardInput) return;

        if (Input.GetKeyDown(KeyCode.A)) PerformAttack();
        if (Input.GetKeyDown(KeyCode.S)) PerformSkill();
        if (Input.GetKeyDown(KeyCode.D)) PerformSkill2();
    }

    #endregion

    // ──────────────────────────────────────────────────────────────────────────
    #region 기본공격 (자동 타겟팅)

    private void PerformAttack()
    {
        // ── 1단계: Check ──────────────────────────────────────────────────────
        if (playerAnimationController == null)
        {
            Debug.LogError("[PlayerAttackInput] PlayerAnimationController 없음");
            return;
        }

        if (!playerAnimationController.CanPerformAttack())
        {
            if (showDebugLogs)
                Debug.Log("[PlayerAttackInput] 기본공격 불가 — 쿨다운 또는 애니메이션 중");
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

        if (showDebugLogs)
            Debug.Log($"[PlayerAttackInput] 기본공격 실행 — dir={attackDir}");
    }

    /// <summary>
    /// 자동 타겟팅으로 공격 방향을 결정합니다.
    /// 탐지된 타겟이 있으면 그 방향, 없으면 이동 방향(또는 FacingDirection)을 반환합니다.
    /// </summary>
    private Vector2 ResolveAttackDirection()
    {
        if (autoTargetResolver != null && basicAttackProfile != null)
        {
            Vector2 aimDir = GetAimDir();
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
        }

        return GetFacingDirection();
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
    /// 이동 중이면 이동 벡터, 정지 중이면 FacingDirection을 반환합니다.
    /// 이 값은 AutoTargetResolver 스코어링의 각도 가중치(AngleWeight)에만 사용됩니다.
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

    private void PerformSkill()
    {
        if (showDebugLogs) Debug.Log("[PlayerAttackInput] S키 스킬1");

        if (playerAnimationController != null)
        {
            bool success = playerAnimationController.TriggerSkill1();
            if (showDebugLogs)
                Debug.Log(success ? "🟢 스킬1 성공" : "🟡 스킬1 실패 (쿨다운 등)");
            return;
        }

        // fallback: PlayerAnimationController 없을 때
        var skillController = FindObjectOfType<SkillController>();
        if (skillController != null)
            skillController.TriggerSkill();
        else
            Debug.LogWarning("[PlayerAttackInput] SkillController 없음");
    }

    private void PerformSkill2()
    {
        if (showDebugLogs) Debug.Log("[PlayerAttackInput] D키 스킬2");

        if (playerAnimationController != null)
        {
            bool success = playerAnimationController.TriggerSkill2();
            if (showDebugLogs)
                Debug.Log(success ? "🟢 스킬2 성공" : "🟡 스킬2 실패 (쿨다운 등)");
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
