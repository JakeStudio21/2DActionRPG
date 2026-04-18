using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// Animation Parameters 기반 플레이어 액션 시스템 통합 컨트롤러
/// 향후 피격, 스킬1, 스킬2 등으로 확장 예정
/// </summary>
public class PlayerAnimationController : MonoBehaviour
{
    // ⭐ 튜토리얼용 스킬 사용 이벤트
    public event Action OnSkill1Used;
    public event Action OnSkill2Used;
    
    [Header("Animation Parameters")]
    [SerializeField] private bool showDebugLogs = false;
    
    // ⭐ 아이소메트릭 데이터 연동 추가
    [Header("🗺️ 아이소메트릭 설정")]
    [SerializeField] private bool useIsometricData = true;
    
    private Animator animator;
    private ActiveWeapon activeWeapon;
    private MonoBehaviour currentWeapon;
    private SkillController skillController; // ⭐ 스킬 컨트롤러 참조 추가
    private PlayerController playerController; // 🆕 이동 제어를 위한 PlayerController 참조
    
    // ⭐ 플레이어 클래스 참조 추가
    private BaseClassBehaviour playerClass;
    private DirectionPreset currentDirectionPreset = DirectionPreset.E8; // 기본값
    
    // Animation Parameter Hashes (성능 최적화)
    readonly int IS_ATTACKING_HASH = Animator.StringToHash("isAttacking");
    readonly int ATTACK_TRIGGER_HASH = Animator.StringToHash("Attack");
    readonly int HIT_TRIGGER_HASH = Animator.StringToHash("Hit");
    readonly int IS_HIT_HASH = Animator.StringToHash("isHit"); // ⭐ isHit Bool Parameter 추가
    // ⭐ 스킬1 Parameters 추가
    readonly int IS_SKILL1_HASH = Animator.StringToHash("isSkill1");
    readonly int SKILL1_TRIGGER_HASH = Animator.StringToHash("Skill1");
    // ⭐ 스킬2 Parameters 추가 (신규)
    readonly int IS_SKILL2_HASH = Animator.StringToHash("isSkill2");
    readonly int SKILL2_TRIGGER_HASH = Animator.StringToHash("Skill2");
    
    // ⭐ Dash Parameters 추가 (E8 방식)
    readonly int IS_DASHING_HASH = Animator.StringToHash("isDashing");
    readonly int DASH_TRIGGER_HASH = Animator.StringToHash("Dash");
    readonly int LAST_MOVE_X_HASH = Animator.StringToHash("lastMoveX");
    readonly int LAST_MOVE_Y_HASH = Animator.StringToHash("lastMoveY");
    
    // 공격 상태 추적
    private bool isAttacking = false;
    private bool canAttack = true;
    private float attackCooldown = 1f;
    private float baseWeaponCooldown = 1f; // 무기 원본 쿨다운 (ASPD 배율 적용 전)
    private float attackCooldownStartTime = -999f; // Radial용 쿨다운 시작 시각
    
    // ⭐ 기본적인 상태 추적만 유지
    private int currentAttackSequence = 0;
    
    // 임시 디버깅 변수들 (나중에 정리 예정)
    private static int onAttackStartCallCount = 0;
    private static float sessionStartTime = -1f;
    private int lastProcessedAttackSequence = -1;
    private int lastAttackFrameCount = -1;
    private float lastOnAttackStartTime = -1f;
    private float lastOnAttackCompleteTime = -1f;
    private float animationEventCooldown = 0.2f;
    
    // ⭐ 스킬1 상태 추적 추가
    private bool isSkill1 = false;
    private bool canSkill1 = true;
    private float skill1Cooldown = 2f;
    private float skill1CooldownStartTime = -999f; // Radial용 쿨다운 시작 시각
    
    // ⭐ 스킬2 상태 추적 추가 (신규)
    private bool isSkill2 = false;
    private bool canSkill2 = true;
    private float skill2Cooldown = 3f; // 스킬2는 조금 더 긴 쿨다운
    private float skill2CooldownStartTime = -999f; // Radial용 쿨다운 시작 시각
    
    // ⭐ Dash 상태 추적 추가 (E8 방식)
    private bool isDashing = false;
    private bool canDash = true;
    private float dashCooldown = 0.25f; // PlayerController와 동일
    private Vector2 lastMoveDirection = Vector2.down; // 기본값: 남쪽
    
    // ⭐ 글로벌 스킬 쿨다운 추가 (핵심 해결책)
    private bool isAnySkillActive = false;
    private float globalSkillCooldown = 0.3f; // 0.3초 글로벌 쿨다운
    private float lastGlobalSkillTime = -Mathf.Infinity;
    
    // ⭐ 피격 상태 추적 추가
    private bool isHit = false;
    
    // ─── SkillMovementState ───────────────────────────────────────────────────
    // 이동 해제 권한을 단일 경로(TryReleaseSkillMovement)에 집중.
    // 애니메이션 완료(80%)와 이펙트 완료 둘 다 true 일 때만 해제.
    private bool isSkillMovementActive = false;
    private bool isSkillAnimDone       = false;
    private bool isSkillEffectDone     = false;
    // ─────────────────────────────────────────────────────────────────────────
    
    void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>(); // 🆕 PlayerController 참조 초기화
        
        
        // ActiveWeapon을 더 넓은 범위에서 찾기
        activeWeapon = GetComponent<ActiveWeapon>();
        if (activeWeapon == null)
        {
            activeWeapon = GetComponentInParent<ActiveWeapon>();
            if (activeWeapon == null)
            {
                activeWeapon = GetComponentInChildren<ActiveWeapon>();
                if (activeWeapon == null)
                {
                    activeWeapon = FindObjectOfType<ActiveWeapon>();
                }
            }
        }
        
        // ⭐ SkillController 참조 찾기
        skillController = GetComponent<SkillController>();
        if (skillController == null)
        {
            skillController = GetComponentInParent<SkillController>();
            if (skillController == null)
            {
                skillController = GetComponentInChildren<SkillController>();
                if (skillController == null)
                {
                    skillController = FindObjectOfType<SkillController>();
                }
            }
        }
        
        if (animator == null)
        {
            return;
        }
        
        if (activeWeapon == null)
        {
            return;
        }
        
        if (skillController != null)
        {
            
            // ⭐ 새로운 방식: 기본값 사용 또는 SkillSet에서 조회
            var skill1 = skillController.SkillSet?.GetSkill(0);
            if (skill1 != null)
            {
                skill1Cooldown = skill1.Cooldown;
            }

            // 스킬 실행 완료 이벤트 구독 (중복 방지 — -= 후 +=)
            skillController.OnSkillExecutionComplete -= HandleSkillExecutionComplete;
            skillController.OnSkillExecutionComplete += HandleSkillExecutionComplete;
        }
        
        // 초기 Animation Parameters 설정
        InitializeAnimationParameters();
        
        // 🗺️ 아이소메트릭 데이터 초기화 추가
        InitializePlayerClassReference();
        
        Dbg.Log("🟢 [PlayerAnimationController] 초기화 완료!");
    }
    
    /// <summary>
    /// 플레이어 클래스 컴포넌트 참조 초기화
    /// </summary>
    private void InitializePlayerClassReference()
    {
            
        // Assasin 또는 Warrior 컴포넌트 찾기
        var assasin = GetComponent<Assasin>();
        var warrior = GetComponent<Warrior>();
        
        if (assasin != null)
        {
            playerClass = assasin;
            currentDirectionPreset = assasin.GetDirectionPreset();
            
        }
        else if (warrior != null)
        {
            playerClass = warrior;
            currentDirectionPreset = warrior.GetDirectionPreset();
            
        }
    }
    
    /// <summary>
    /// Animation Parameters 초기화
    /// </summary>
    private void InitializeAnimationParameters()
    {
        try
        {
            // 공격 상태 초기화
            if (HasParameter(animator, "isAttacking"))
                animator.SetBool(IS_ATTACKING_HASH, false);
            else
                Debug.LogWarning("[PlayerAnimationController] 'isAttacking' Parameter가 Animation Controller에 없습니다.");
            
            // ⭐ 스킬1 상태 초기화
            if (HasParameter(animator, "isSkill1"))
                animator.SetBool(IS_SKILL1_HASH, false);
            else
                Debug.LogWarning("[PlayerAnimationController] 'isSkill1' Parameter가 Animation Controller에 없습니다.");
            
            // ⭐ 스킬2 상태 초기화 (신규)
            if (HasParameter(animator, "isSkill2"))
                animator.SetBool(IS_SKILL2_HASH, false);
            else
                Debug.LogWarning("[PlayerAnimationController] 'isSkill2' Parameter가 Animation Controller에 없습니다.");
            
            // ⭐ 피격 상태 초기화
            if (HasParameter(animator, "isHit"))
                animator.SetBool(IS_HIT_HASH, false);
            else
                Debug.LogWarning("[PlayerAnimationController] 'isHit' Parameter가 Animation Controller에 없습니다.");
            
            // 쿨다운 설정
            if (activeWeapon != null && activeWeapon.CurrentActiveWeapon != null)
            {
                var weapon = activeWeapon.CurrentActiveWeapon as IWeapon;
                if (weapon != null)
                {
                    UpdateWeaponCooldown(weapon.GetEquipmentData().WeaponCooldown);
                }
            }
            
            canAttack = true;
            canSkill1 = true; // ⭐ 스킬1 초기화
            canSkill2 = true; // ⭐ 스킬2 초기화
            isHit = false; // ⭐ 피격 상태 초기화
            Dbg.Log("🟢 [PlayerAnimationController] Animation Parameters 초기화 완료!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🔴 [PlayerAnimationController] Animation Parameters 초기화 실패: {e.Message}");
        }
    }
    
    /// <summary>
    /// Animator에 Parameter가 존재하는지 확인
    /// </summary>
    private bool HasParameter(Animator animator, string parameterName)
    {
        foreach (var parameter in animator.parameters)
        {
            if (parameter.name == parameterName)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// 공격 실행 (기본 공격만)
    /// </summary>
    public bool TriggerAttack()
    {
        // 공격 가능 여부 확인
        if (!CanPerformAttack())
        {
            return false;
        }
        
        // ⭐ 새로운 공격 시퀀스 시작
        currentAttackSequence++;
        
        // Animation Parameters 설정
        if (HasParameter(animator, "isAttacking"))
        {
            animator.SetBool(IS_ATTACKING_HASH, true);
        }
            
        if (HasParameter(animator, "Attack"))
        {
            animator.SetTrigger(ATTACK_TRIGGER_HASH);
            
            // 기본공격 이동 제한 적용
            if (playerController != null)
            {
                playerController.ApplyAttackMovementRestriction(PlayerAttackType.BasicAttack);
            }
        }
        else
        {
            ExecuteWeaponAttack();
        }
        
        // 내부 상태 업데이트
        isAttacking = true;
        canAttack = false;
        attackCooldownStartTime = Time.time;
        
        
        return true;
    }
    
    /// <summary>
    /// ⭐ 스킬1 실행 (신규 추가)
    /// </summary>
    public bool TriggerSkill1()
    {
        
        // ⭐ 튜토리얼용 스킬 사용 이벤트 발생
        OnSkill1Used?.Invoke();
        
        // 쿨다운 게이트: canSkill1이 주 게이트 (Skill1CooldownRoutine이 관리)
        if (!canSkill1)
        {
            return false;
        }
        
        // 이동 잠금 게이트: 다른 스킬 애니메이션이 이동을 점유 중이면 발동 불가
        if (isSkillMovementActive)
        {
            return false;
        }
        
        // Animation Parameters 설정
        try
        {
            if (HasParameter(animator, "isSkill1"))
                animator.SetBool(IS_SKILL1_HASH, true);
                
            if (HasParameter(animator, "Skill1"))
            {
                animator.SetTrigger(SKILL1_TRIGGER_HASH);
                StartSkillMovement();
            }
            else
            {
                ExecuteSkill1();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerAnimationController] Skill1 Animation Parameters 설정 중 오류: {e.Message}");
            return false;
        }
        
        // 스킬 발동 즉시 canSkill1 = false → Skill1CooldownRoutine 완료 전까지 재발동 불가
        canSkill1 = false;
        skill1CooldownStartTime = Time.time;
        
        
        return true;
    }
    
    /// <summary>
    /// ⭐ 스킬2 실행 (신규 추가)
    /// </summary>
    public bool TriggerSkill2()
    {
        
        // ⭐ 튜토리얼용 스킬 사용 이벤트 발생
        OnSkill2Used?.Invoke();
        
        // 쿨다운 게이트: canSkill2가 주 게이트 (Skill2CooldownRoutine이 관리)
        if (!canSkill2)
        {
            return false;
        }
        
        // 이동 잠금 게이트: 다른 스킬 애니메이션이 이동을 점유 중이면 발동 불가
        if (isSkillMovementActive)
        {
            return false;
        }
        
        // Animation Parameters 설정
        try
        {
            if (HasParameter(animator, "isSkill2"))
                animator.SetBool(IS_SKILL2_HASH, true);
                
            if (HasParameter(animator, "Skill2"))
            {
                animator.SetTrigger(SKILL2_TRIGGER_HASH);
                StartSkillMovement();
            }
            else
            {
                ExecuteSkill2();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerAnimationController] Skill2 Animation Parameters 설정 중 오류: {e.Message}");
            return false;
        }
        
        // 스킬 발동 즉시 canSkill2 = false → Skill2CooldownRoutine 완료 전까지 재발동 불가
        canSkill2 = false;
        skill2CooldownStartTime = Time.time;
        
        
        return true;
    }
    
    /// <summary>
    /// 공격 가능 여부 확인
    /// </summary>
    public bool CanPerformAttack()
    {
        // ActiveWeapon이 null이면 다시 찾기 시도
        if (activeWeapon == null)
        {
            activeWeapon = FindObjectOfType<ActiveWeapon>();
        }
        
        bool animatorIsAttacking = false;
        bool animatorIsSkill1 = false;
        bool animatorIsSkill2 = false; // ⭐ 스킬2 상태 체크
        bool animatorIsHit = false; // ⭐ 피격 중 공격 불가
        bool hasWeapon = activeWeapon != null && activeWeapon.CurrentActiveWeapon != null;
        
        try
        {
            if (HasParameter(animator, "isAttacking"))
                animatorIsAttacking = animator.GetBool(IS_ATTACKING_HASH);
            if (HasParameter(animator, "isSkill1"))
                animatorIsSkill1 = animator.GetBool(IS_SKILL1_HASH);
            if (HasParameter(animator, "isSkill2")) // ⭐ 스킬2 상태 체크
                animatorIsSkill2 = animator.GetBool(IS_SKILL2_HASH);
            if (HasParameter(animator, "isHit"))
                animatorIsHit = animator.GetBool(IS_HIT_HASH); // ⭐ isHit 상태 체크
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerAnimationController] Animator Parameter 접근 중 오류: {e.Message}");
        }
        
        // ⭐ isHit으로 인한 공격 차단 확인 로그 추가
        if (animatorIsHit && showDebugLogs)
        {
            Debug.LogWarning("🔴 [PlayerAnimationController] 공격 차단됨 - 피격 중 (isHit = true)");
        }
        
        // 디버깅을 위한 상세 로그 (빈도 줄임)
        bool result = !animatorIsAttacking && !animatorIsSkill1 && !animatorIsSkill2 && !animatorIsHit && hasWeapon && canAttack; // ⭐ isHit 조건 추가
        
        return result;
    }
    
    /// <summary>
    /// ⭐ 스킬1 가능 여부 확인 - 글로벌 쿨다운 추가
    /// </summary>
    public bool CanPerformSkill1()
    {
        bool animatorIsAttacking = false;
        bool animatorIsSkill1 = false;
        bool animatorIsSkill2 = false; // ⭐ 스킬2 상태 체크
        bool animatorIsHit = false; // ⭐ 피격 중 스킬1 불가
        bool hasSkillController = skillController != null;
        
        // ⭐ 글로벌 스킬 쿨다운 체크
        bool globalCooldownReady = Time.time >= lastGlobalSkillTime + globalSkillCooldown;
        
        try
        {
            if (HasParameter(animator, "isAttacking"))
                animatorIsAttacking = animator.GetBool(IS_ATTACKING_HASH);
            if (HasParameter(animator, "isSkill1"))
                animatorIsSkill1 = animator.GetBool(IS_SKILL1_HASH);
            if (HasParameter(animator, "isSkill2")) // ⭐ 스킬2 상태 체크
                animatorIsSkill2 = animator.GetBool(IS_SKILL2_HASH);
            if (HasParameter(animator, "isHit"))
                animatorIsHit = animator.GetBool(IS_HIT_HASH); // ⭐ isHit 상태 체크
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerAnimationController] Animator Parameter 접근 중 오류: {e.Message}");
        }
        
        // 스킬 이동 잠금 중이면 다른 스킬 발동 불가 (isSkillMovementActive가 실제 상태 반영)
        bool result = !animatorIsAttacking && !animatorIsSkill1 && !animatorIsSkill2 && 
                     !animatorIsHit && hasSkillController && canSkill1 && 
                     globalCooldownReady && !isSkillMovementActive;
        
        return result;
    }
    
    /// <summary>
    /// ⭐ 스킬2 가능 여부 확인 - 글로벌 쿨다운 추가
    /// </summary>
    public bool CanPerformSkill2()
    {
        bool animatorIsAttacking = false;
        bool animatorIsSkill1 = false;
        bool animatorIsSkill2 = false;
        bool animatorIsHit = false;
        bool hasSkillController = skillController != null;
        
        // ⭐ 글로벌 스킬 쿨다운 체크
        bool globalCooldownReady = Time.time >= lastGlobalSkillTime + globalSkillCooldown;
        
        try
        {
            if (HasParameter(animator, "isAttacking"))
                animatorIsAttacking = animator.GetBool(IS_ATTACKING_HASH);
            if (HasParameter(animator, "isSkill1"))
                animatorIsSkill1 = animator.GetBool(IS_SKILL1_HASH);
            if (HasParameter(animator, "isSkill2"))
                animatorIsSkill2 = animator.GetBool(IS_SKILL2_HASH);
            if (HasParameter(animator, "isHit"))
                animatorIsHit = animator.GetBool(IS_HIT_HASH);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerAnimationController] Animator Parameter 접근 중 오류: {e.Message}");
        }
        
        // 스킬 이동 잠금 중이면 다른 스킬 발동 불가 (isSkillMovementActive가 실제 상태 반영)
        bool result = !animatorIsAttacking && !animatorIsSkill1 && !animatorIsSkill2 && 
                     !animatorIsHit && hasSkillController && canSkill2 && 
                     globalCooldownReady && !isSkillMovementActive;
        
        return result;
    }
    
    /// <summary>
    /// Animation Event에서 호출: 공격 시작 시점
    /// </summary>
    public void OnAttackStart()
    {
        if (activeWeapon != null && activeWeapon.CurrentActiveWeapon != null)
        {
            currentWeapon = activeWeapon.CurrentActiveWeapon;
        }
        
        if (currentWeapon != null)
        {
            IWeapon weaponInterface = currentWeapon.GetComponent<IWeapon>();
            if (weaponInterface != null)
            {
                weaponInterface.Attack();
            }
        }
    }
    
    /// <summary>
    /// Animation Event에서 호출: 공격 완료 시점
    /// </summary>
    public void OnAttackComplete()
    {
        // Animation Parameters 리셋
        if (animator != null)
        {
            animator.SetBool(IS_ATTACKING_HASH, false);
        }
        
        // 이동 제한 해제
        if (playerController != null)
        {
            playerController.RestoreNormalMovement();
            // 공격 애니메이션 방향 잠금 해제 → PlayerController가 이동 방향 갱신 재개
            playerController.UnlockAnimationDirection();
        }
        
        // 무기 방향 잠금 해제 → ActiveWeapon이 조이스틱 입력으로 복귀
        if (activeWeapon != null)
        {
            activeWeapon.UnlockAttackDirection();
        }
        
        // 공격 쿨다운 시작
        StartCoroutine(AttackCooldownRoutine());
    }
    
    /// <summary>
    /// Animation Event에서 호출: 스킬1 시작 시점
    /// </summary>
    public void OnSkill1Start()
    {
        // ⭐ BaseSkill 쿨다운 시작
        if (skillController != null)
        {
            var skill1 = skillController.SkillSet?.GetSkill(0);
            if (skill1 != null)
            {
                // Reflection으로 lastSkillTime 설정
                var baseType = skill1.GetType().BaseType;
                while (baseType != null && !baseType.IsGenericType)
                {
                    baseType = baseType.BaseType;
                }
                
                if (baseType != null)
                {
                    var field = baseType.GetField("lastSkillTime", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(skill1, Time.time);
                    }
                }
            }
        }
        
        ExecuteSkill1();
    }
    
    /// <summary>
    /// Animation Event에서 호출: 스킬1 완료 시점
    /// </summary>
    public void OnSkill1Complete()
    {
        if (HasParameter(animator, "isSkill1"))
            animator.SetBool(IS_SKILL1_HASH, false);
        
        // 애니메이션 완료 신호 — 이동 해제는 TryReleaseSkillMovement에서 결정
        NotifySkillAnimDone();
        
        // 쿨다운 루틴 시작 전 CSV 기반 유효 쿨다운 동기화
        if (skillController != null)
        {
            float effective = skillController.GetEffectiveCooldown(0);
            if (effective > 0f)
                UpdateSkill1Cooldown(effective);
        }
        
        StartCoroutine(Skill1CooldownRoutine());
    }
    
    /// <summary>
    /// Animation Event에서 호출: 스킬2 시작 시점
    /// </summary>
    public void OnSkill2Start()
    {
        // ⭐ BaseSkill 쿨다운 시작
        if (skillController != null)
        {
            var skill2 = skillController.SkillSet?.GetSkill(1);
            if (skill2 != null)
            {
                // Reflection으로 lastSkillTime 설정
                var baseType = skill2.GetType().BaseType;
                while (baseType != null && !baseType.IsGenericType)
                {
                    baseType = baseType.BaseType;
                }
                
                if (baseType != null)
                {
                    var field = baseType.GetField("lastSkillTime", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(skill2, Time.time);
                    }
                }
            }
        }
        
        ExecuteSkill2();
    }
    
    /// <summary>
    /// Animation Event에서 호출: 스킬2 완료 시점
    /// </summary>
    public void OnSkill2Complete()
    {
        if (HasParameter(animator, "isSkill2"))
            animator.SetBool(IS_SKILL2_HASH, false);
        
        // 애니메이션 완료 신호 — 이동 해제는 TryReleaseSkillMovement에서 결정
        NotifySkillAnimDone();
        
        // 쿨다운 루틴 시작 전 CSV 기반 유효 쿨다운 동기화
        if (skillController != null)
        {
            float effective = skillController.GetEffectiveCooldown(1);
            if (effective > 0f)
                UpdateSkill2Cooldown(effective);
        }
        
        StartCoroutine(Skill2CooldownRoutine());
    }
    
    /// <summary>
    /// 무기 공격 직접 실행 (Animation Event 없이)
    /// </summary>
    private void ExecuteWeaponAttack()
    {
        if (activeWeapon != null && activeWeapon.CurrentActiveWeapon != null)
        {
            var weapon = activeWeapon.CurrentActiveWeapon as IWeapon;
            if (weapon != null)
            {
                weapon.Attack();
            }
        }
        
        // 간단한 쿨다운 시작
        StartCoroutine(AttackCooldownRoutine());
    }
    
    /// <summary>
    /// ⭐ 스킬1 직접 실행 (Animation Event 없이)
    /// </summary>
    private void ExecuteSkill1()
    {
        if (skillController != null)
        {
            skillController.OnSkill1AnimationEvent();
        }
        
        // ❌ 제거: 중복 쿨다운
        // StartCoroutine(Skill1CooldownRoutine());
    }
    
    /// <summary>
    /// ⭐ 스킬2 직접 실행 (Animation Event 없이)
    /// </summary>
    private void ExecuteSkill2()
    {
        if (skillController != null)
        {
            skillController.OnSkill2AnimationEvent();
        }
        
        // ❌ 제거: 중복 쿨다운
        // StartCoroutine(Skill2CooldownRoutine());
    }
    
    /// <summary>
    /// 공격 쿨다운 코루틴
    /// </summary>
    private IEnumerator AttackCooldownRoutine()
    {
        // TriggerAttack() 시점(attackCooldownStartTime)부터 경과된 시간을 빼서
        // Radial 표시와 canAttack 해제 시점을 동기화
        float elapsed = Time.time - attackCooldownStartTime;
        float remaining = Mathf.Max(0f, attackCooldown - elapsed);
        
        
        yield return new WaitForSeconds(remaining);
        
        canAttack = true;
        isAttacking = false;
        
    }
    
    /// <summary>
    /// ⭐ 스킬1 쿨다운 코루틴 - 글로벌 상태 리셋 추가
    /// </summary>
    private IEnumerator Skill1CooldownRoutine()
    {
        // TriggerSkill1() 시점(skill1CooldownStartTime)부터 경과된 시간을 빼서
        // Radial 표시와 canSkill1 해제 시점을 동기화
        float elapsed = Time.time - skill1CooldownStartTime;
        float remaining = Mathf.Max(0f, skill1Cooldown - elapsed);
        
        
        yield return new WaitForSeconds(remaining);
        
        canSkill1 = true;
        isSkill1 = false;
        isAnySkillActive = false;
        
    }
    
    /// <summary>
    /// ⭐ 스킬2 쿨다운 코루틴 - 글로벌 상태 리셋 추가
    /// </summary>
    private IEnumerator Skill2CooldownRoutine()
    {
        float elapsed = Time.time - skill2CooldownStartTime;
        float remaining = Mathf.Max(0f, skill2Cooldown - elapsed);
        
        
        yield return new WaitForSeconds(remaining);
        
        canSkill2 = true;
        isSkill2 = false;
        isAnySkillActive = false;
        
    }
    
    /// <summary>
    /// 무기 변경 시 쿨다운 업데이트 (ActiveWeapon이 호출)
    /// 원본 weaponCooldown을 보관한 뒤 ASPD 배율을 적용해 최종 쿨다운 재계산
    /// </summary>
    public void UpdateWeaponCooldown(float newCooldown)
    {
        baseWeaponCooldown = newCooldown;
        RecalculateAttackCooldown();
    }

    /// <summary>
    /// PlayerRuntimeStats 스탯 변경 시 공격 쿨다운 재동기화 (PlayerRuntimeStats가 호출)
    /// baseWeaponCooldown을 현재 장착 무기에서 다시 읽어 ASPD 재계산
    /// </summary>
    public void SyncWithRuntimeStats()
    {
        // 현재 장착 무기의 WeaponCooldown으로 baseWeaponCooldown 동기화 (초기화 순서 문제 방지)
        if (activeWeapon == null)
            activeWeapon = FindObjectOfType<ActiveWeapon>();
        
        if (activeWeapon?.CurrentWeaponData != null)
        {
            float weaponCooldown = activeWeapon.CurrentWeaponData.WeaponCooldown;
            if (baseWeaponCooldown != weaponCooldown)
            {
                baseWeaponCooldown = weaponCooldown;
            }
        }
        
        RecalculateAttackCooldown();
    }

    /// <summary>
    /// 실제 공격 쿨다운 계산 — baseWeaponCooldown / FinalAttackSpeed
    /// 이 한 곳에서만 attackCooldown 값을 결정
    /// </summary>
    private void RecalculateAttackCooldown()
    {
        var runtimeStats = FindObjectOfType<PlayerRuntimeStats>();
        float atkSpeed = (runtimeStats != null && runtimeStats.FinalAttackSpeed > 0f)
            ? runtimeStats.FinalAttackSpeed
            : 1f;

        attackCooldown = baseWeaponCooldown / atkSpeed;

    }
    
    /// <summary>
    /// ⭐ 스킬1 쿨다운 업데이트 (신규 추가)
    /// </summary>
    public void UpdateSkill1Cooldown(float newCooldown)
    {
        skill1Cooldown = newCooldown;
    }
    
    /// <summary>
    /// ⭐ 스킬2 쿨다운 업데이트 (신규 추가)
    /// </summary>
    public void UpdateSkill2Cooldown(float newCooldown)
    {
        skill2Cooldown = newCooldown;
    }
    
    // ===== Radial UI용 쿨다운 정보 공개 메서드 =====
    
    /// <summary>
    /// 기본공격 Radial용: (잔여시간, 총쿨다운) 반환.
    /// 쿨다운 중이 아니면 remaining = 0.
    /// </summary>
    public void GetAttackCooldownInfo(out float remaining, out float total)
    {
        total = attackCooldown > 0f ? attackCooldown : 1f;
        if (canAttack)
        {
            remaining = 0f;
        }
        else
        {
            float elapsed = Time.time - attackCooldownStartTime;
            remaining = Mathf.Max(0f, total - elapsed);
        }
    }
    
    /// <summary>
    /// 스킬1 Radial용: (잔여시간, 총쿨다운) 반환.
    /// </summary>
    public void GetSkill1CooldownInfo(out float remaining, out float total)
    {
        total = skill1Cooldown > 0f ? skill1Cooldown : 1f;
        if (canSkill1)
        {
            remaining = 0f;
        }
        else
        {
            float elapsed = Time.time - skill1CooldownStartTime;
            remaining = Mathf.Max(0f, total - elapsed);
        }
    }
    
    /// <summary>
    /// 스킬2 Radial용: (잔여시간, 총쿨다운) 반환.
    /// </summary>
    public void GetSkill2CooldownInfo(out float remaining, out float total)
    {
        total = skill2Cooldown > 0f ? skill2Cooldown : 1f;
        if (canSkill2)
        {
            remaining = 0f;
        }
        else
        {
            float elapsed = Time.time - skill2CooldownStartTime;
            remaining = Mathf.Max(0f, total - elapsed);
        }
    }
    
    /// <summary>
    /// 피격 트리거 실행 (안전장치 추가)
    /// </summary>
    public bool TriggerHit()
    {
        
        if (animator == null)
        {
            Debug.LogError("🔴 [PlayerAnimationController] Animator가 null입니다!");
            return false;
        }
        
        try
        {
            // 현재 공격 중이면 공격 중단
            if (HasParameter(animator, "isAttacking"))
            {
                animator.SetBool(IS_ATTACKING_HASH, false);
            }
            
            // ⭐ 현재 스킬1 중이면 스킬1 중단
            if (HasParameter(animator, "isSkill1"))
            {
                animator.SetBool(IS_SKILL1_HASH, false);
            }

            // ⭐ 현재 스킬2 중이면 스킬2 중단
            if (HasParameter(animator, "isSkill2"))
            {
                animator.SetBool(IS_SKILL2_HASH, false);
            }

            // ⭐ 스킬 실행 중(Telegraph/딜레이 대기 포함)이면 강제 취소 → 이동 해제 이벤트 발행
            if (skillController != null && skillController.IsSkillPendingExecution)
            {
                skillController.CancelSkillExecution();
            }
            
            // ⭐ 피격 상태 시작
            if (HasParameter(animator, "isHit"))
            {
                animator.SetBool(IS_HIT_HASH, true);
                isHit = true;
            }
            
            // Hit Trigger 존재 여부 확인 후 실행
            if (HasParameter(animator, "Hit"))
            {
                animator.SetTrigger(HIT_TRIGGER_HASH);
                return true;
            }
            else
            {
                Debug.LogWarning("🟡 [PlayerAnimationController] 'Hit' Trigger가 Animation Controller에 없습니다. 무시됩니다.");
                // Hit Trigger가 없어도 에러 없이 처리 (다른 피격 효과는 계속 작동)
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🔴 [PlayerAnimationController] 피격 트리거 실행 실패: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Animation Event: 피격 시작 시점 (Hit 애니메이션이 있을 때만 호출됨)
    /// </summary>
    public void OnHitStart()
    {
            
        // 피격 시 추가 로직 (예: 이동 제한, 공격 중단 등)
        isAttacking = false; // 공격 중단
        isSkill1 = false; // ⭐ 스킬1 중단
        isSkill2 = false; // ⭐ 스킬2 중단
        isHit = true; // ⭐ 피격 상태 유지
        
        // ⭐ Animator Parameter도 확실히 설정
        if (HasParameter(animator, "isHit"))
        {
            animator.SetBool(IS_HIT_HASH, true);
        }
    }

    /// <summary>
    /// Animation Event: 피격 종료 시점 (Hit 애니메이션이 있을 때만 호출됨)
    /// </summary>
    public void OnHitEnd()
    {
        
        // ⭐ 피격 상태 종료
        isHit = false;
        if (HasParameter(animator, "isHit"))
        {
            animator.SetBool(IS_HIT_HASH, false);
        }
        
    }

    /// <summary>
    /// 피격 중인지 확인 (isHit Bool Parameter 기반)
    /// </summary>
    public bool IsHit()
    {
        try
        {
            if (HasParameter(animator, "isHit"))
                return animator.GetBool(IS_HIT_HASH);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerAnimationController] IsHit() 체크 실패: {e.Message}");
        }
        
        return isHit; // fallback to script variable
    }
    
    /// <summary>
    /// 현재 공격 상태 정보 (디버깅용)
    /// </summary>
    public void LogCurrentState()
    {
    }

    private void OnDestroy()
    {
        if (skillController != null)
            skillController.OnSkillExecutionComplete -= HandleSkillExecutionComplete;
    }

    /// <summary>
    /// SkillController.OnSkillExecutionComplete 수신 — 이동 해제 처리
    /// slotIndex == -1 은 취소(피격 등)를 의미
    /// </summary>
    private void HandleSkillExecutionComplete(int slotIndex)
    {
        // 이펙트 완료 신호 — 이동 해제는 TryReleaseSkillMovement에서 결정
        NotifySkillEffectDone();

    }

    // =========================================================================
    // SkillMovementState — 이동 해제 단일 권한 메서드
    // =========================================================================

    /// <summary>스킬 트리거 시 호출 — 방향 잠금 + 플래그 리셋 + 이동 잠금</summary>
    public void StartSkillMovement()
    {
        // 트리거 시점의 조이스틱 방향을 고정 — 이후 변경에 영향받지 않음
        if (skillController != null)
            skillController.LockCurrentSkillDirection();

        isSkillMovementActive = true;
        isSkillAnimDone       = false;
        isSkillEffectDone     = false;
        if (playerController != null)
            playerController.ApplyAttackMovementRestriction(PlayerAttackType.Skill);
    }

    /// <summary>애니메이션 80% 도달 시 SkillStateBehaviour → OnSkill1/2Complete 경유하여 호출</summary>
    public void NotifySkillAnimDone()
    {
        isSkillAnimDone = true;
        TryReleaseSkillMovement();
    }

    /// <summary>이펙트 코루틴 완료 시 HandleSkillExecutionComplete 경유하여 호출</summary>
    public void NotifySkillEffectDone()
    {
        isSkillEffectDone = true;
        TryReleaseSkillMovement();
    }

    /// <summary>피격/취소/State 강제 종료 시 호출 — 두 플래그를 강제로 true 처리</summary>
    public void ForceSkillMovementRelease()
    {
        if (!isSkillMovementActive) return;
        isSkillAnimDone   = true;
        isSkillEffectDone = true;
        TryReleaseSkillMovement();
    }

    /// <summary>단일 해제 게이트 — animDone && effectDone 둘 다 true 일 때만 RestoreNormalMovement 호출</summary>
    private void TryReleaseSkillMovement()
    {
        if (!isSkillMovementActive) return;
        if (!isSkillAnimDone || !isSkillEffectDone) return;
        isSkillMovementActive = false;
        if (playerController != null)
            playerController.RestoreNormalMovement();
    }

    void Update()
    {
        // ⭐ 5초마다 Animation Parameter 상태 확인 (빈도 더 줄임)
        if (Time.frameCount % 300 == 0 && showDebugLogs)
        {
            LogCurrentAnimationState();
        }
    }

    /// <summary>
    /// 현재 Animation Parameter 상태 로깅 (디버깅용)
    /// </summary>
    private void LogCurrentAnimationState()
    {
    }

    /// <summary>
    /// State Hash를 이름으로 변환 (디버깅용) - 강화된 버전
    /// </summary>
    private string GetStateName(int stateHash)
    {
        // 기본 상태들
        if (stateHash == Animator.StringToHash("Idle")) return "Idle";
        if (stateHash == Animator.StringToHash("Running")) return "Running";
        if (stateHash == Animator.StringToHash("Death")) return "Death";
        
        // 기본 공격 애니메이션들
        if (stateHash == Animator.StringToHash("Attack_Assain")) return "Attack_Assain";
        if (stateHash == Animator.StringToHash("Attack_Assain_E")) return "Attack_Assain_E";
        if (stateHash == Animator.StringToHash("Attack_Assain_N")) return "Attack_Assain_N";
        if (stateHash == Animator.StringToHash("Attack_Assain_S")) return "Attack_Assain_S";
        if (stateHash == Animator.StringToHash("Attack_Assain_NE")) return "Attack_Assain_NE";
        if (stateHash == Animator.StringToHash("Attack_Assain_SE")) return "Attack_Assain_SE";
        if (stateHash == Animator.StringToHash("Attack_Assain_NW")) return "Attack_Assain_NW";
        if (stateHash == Animator.StringToHash("Attack_Assain_SW")) return "Attack_Assain_SW";
        if (stateHash == Animator.StringToHash("Attack_Assain_W")) return "Attack_Assain_W";
        
        // 스킬 애니메이션들
        if (stateHash == Animator.StringToHash("Skill1_Assain")) return "Skill1_Assain";
        if (stateHash == Animator.StringToHash("Skill2_Assain")) return "Skill2_Assain";
        
        // 피격 애니메이션들
        if (stateHash == Animator.StringToHash("Hit_Assain")) return "Hit_Assain";
        
        // 🔍 실제 클립 이름들도 확인
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            var clips = animator.runtimeAnimatorController.animationClips;
            foreach (var clip in clips)
            {
                if (Animator.StringToHash(clip.name) == stateHash)
                {
                    return $"CLIP:{clip.name}";
                }
            }
            
            // 🔍 State 이름들도 확인 (Editor 전용)
#if UNITY_EDITOR
            if (animator.runtimeAnimatorController is UnityEditor.Animations.AnimatorController animatorController)
            {
                var layers = animatorController.layers;
                foreach (var layer in layers)
                {
                    var stateMachine = layer.stateMachine;
                    foreach (var state in stateMachine.states)
                    {
                        if (Animator.StringToHash(state.state.name) == stateHash)
                        {
                            return $"STATE:{state.state.name}";
                        }
                    }
                }
            }
#endif
        }
        
        return $"Unknown({stateHash})";
    }

    /// <summary>
    /// ⭐ 대시 트리거 (E8 방식 방향 저장)
    /// </summary>
    public bool TriggerDash(Vector2 direction)
    {
        
        if (animator == null)
        {
            Debug.LogError("🔴 [PlayerAnimationController] Animator가 null입니다!");
            return false;
        }
        
        if (!canDash)
        {
            return false;
        }
        
        // 방향이 거의 0이면 마지막 저장된 방향 사용
        if (direction.magnitude < 0.1f)
        {
            direction = lastMoveDirection;
        }
        
        // 방향 저장 (E8 방식 변환)
        Vector2 dashDirection = ConvertToDirection(direction);
        lastMoveDirection = dashDirection;
        
        // 애니메이션 파라미터 설정
        try
        {
            animator.SetFloat(LAST_MOVE_X_HASH, dashDirection.x);
            animator.SetFloat(LAST_MOVE_Y_HASH, dashDirection.y);
            animator.SetBool(IS_DASHING_HASH, true);
            animator.SetTrigger(DASH_TRIGGER_HASH);
            
            isDashing = true;
            canDash = false;
            
            
            // 대시 지속시간 후 상태 리셋
            StartCoroutine(DashRoutine());
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🔴 [PlayerAnimationController] 대시 트리거 실패: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// ⭐ 동적 방향 변환 (DirectionPreset 기반)
    /// </summary>
    private Vector2 ConvertToDirection(Vector2 direction)
    {
        direction = direction.normalized;
        
        // 아이소메트릭 데이터 사용하지 않으면 기존 E5 방식 (하위 호환)
        if (!useIsometricData)
        {
            return ConvertToE5Legacy(direction);
        }
        
        // DirectionPreset에 따른 분기
        switch (currentDirectionPreset)
        {
            case DirectionPreset.E4M:
                return ConvertToE4M(direction);
            case DirectionPreset.E8:
                return ConvertToE8(direction);
            default:
                return ConvertToE8(direction); // 기본값
        }
    }
    
    /// <summary>
    /// E8 (8방향) 변환 
    /// </summary>
    private Vector2 ConvertToE8(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        
        // 8방향 정밀 변환 (22.5도 간격)
        if (angle >= 337.5f || angle < 22.5f)
            return Vector2.right;                    // E (0°)
        else if (angle >= 22.5f && angle < 67.5f)
            return new Vector2(0.7071f, 0.7071f);   // NE (45°)
        else if (angle >= 67.5f && angle < 112.5f)
            return Vector2.up;                       // N (90°)
        else if (angle >= 112.5f && angle < 157.5f)
            return new Vector2(-0.7071f, 0.7071f);  // NW (135°)
        else if (angle >= 157.5f && angle < 202.5f)
            return Vector2.left;                     // W (180°)
        else if (angle >= 202.5f && angle < 247.5f)
            return new Vector2(-0.7071f, -0.7071f); // SW (225°)
        else if (angle >= 247.5f && angle < 292.5f)
            return Vector2.down;                     // S (270°)
        else
            return new Vector2(0.7071f, -0.7071f);  // SE (315°)
    }
    
    /// <summary>
    /// E4M (4방향+미러링) 변환
    /// </summary>
    private Vector2 ConvertToE4M(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        
        // 4방향 스냅핑 (45도 간격)
        if (angle >= 315f || angle < 45f)
            return Vector2.right;                    // E (0°)
        else if (angle >= 45f && angle < 135f)
            return Vector2.up;                       // N (90°)
        else if (angle >= 135f && angle < 225f)
            return Vector2.left;                     // W (180°)
        else
            return Vector2.down;                     // S (270°)
    }
    
    /// <summary>
    /// 기존 E5 방식 (하위 호환용)
    /// </summary>
    private Vector2 ConvertToE5Legacy(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        
        // 기존 E5 로직 유지
        if (angle >= 337.5f || angle < 22.5f)
            return Vector2.right;
        else if (angle >= 22.5f && angle < 67.5f)
            return new Vector2(0.7f, 0.7f);
        else if (angle >= 67.5f && angle < 112.5f)
            return Vector2.up;
        else if (angle >= 112.5f && angle < 157.5f)
            return new Vector2(0.7f, 0.7f);  // NW → NE
        else if (angle >= 157.5f && angle < 202.5f)
            return Vector2.right;    // W → E
        else if (angle >= 202.5f && angle < 247.5f)
            return new Vector2(0.7f, -0.7f);  // SW → SE
        else if (angle >= 247.5f && angle < 292.5f)
            return Vector2.down;
        else
            return new Vector2(0.7f, -0.7f);  // SE
    }
    
    /// <summary>
    /// ⭐ 대시 상태 관리 코루틴
    /// </summary>
    private IEnumerator DashRoutine()
    {
        // 대시 지속시간 (PlayerController와 동일)
        yield return new WaitForSeconds(0.2f);
        
        // isDashing 해제
        if (animator != null)
        {
            animator.SetBool(IS_DASHING_HASH, false);
        }
        isDashing = false;
        
        
        // 쿨다운
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
        
    }
    
    /// <summary>
    /// ⭐ Animation Event: 대시 시작 시점
    /// </summary>
    public void OnDashStart()
    {
        
        // 추가 대시 로직 (파티클 이펙트 등)이 필요하면 여기에 구현
    }
    
    /// <summary>
    /// ⭐ Animation Event: 대시 완료 시점  
    /// </summary>
    public void OnDashComplete()
    {
        
        // 대시 완료 시 추가 로직이 필요하면 여기에 구현
    }
} 