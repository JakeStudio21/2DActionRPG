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
    
    // ⭐ 스킬2 상태 추적 추가 (신규)
    private bool isSkill2 = false;
    private bool canSkill2 = true;
    private float skill2Cooldown = 3f; // 스킬2는 조금 더 긴 쿨다운
    
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
            if (showDebugLogs)
                Debug.Log($"🟢 [PlayerAnimationController] SkillController 찾음: {skillController.name}");
            
            // ⭐ 제거: SkillController에서 쿨다운 시간 가져오기
            // skill1Cooldown = skillController.CooldownTime;
            
            // ⭐ 새로운 방식: 기본값 사용 또는 SkillSet에서 조회
            var skill1 = skillController.SkillSet?.GetSkill(0);
            if (skill1 != null)
            {
                skill1Cooldown = skill1.Cooldown;
                if (showDebugLogs)
                    Debug.Log($"🎯 [PlayerAnimationController] 스킬1 쿨다운: {skill1Cooldown}초");
            }
        }
        
        // 초기 Animation Parameters 설정
        InitializeAnimationParameters();
        
        // 🗺️ 아이소메트릭 데이터 초기화 추가
        InitializePlayerClassReference();
        
        if (showDebugLogs)
            Debug.Log("🟢 [PlayerAnimationController] 초기화 완료!");
    }
    
    /// <summary>
    /// 플레이어 클래스 컴포넌트 참조 초기화
    /// </summary>
    private void InitializePlayerClassReference()
    {
        if (showDebugLogs)
            Debug.Log("🔍 [PlayerAnimationController] 플레이어 클래스 참조 초기화 시작");
            
        // Assasin 또는 Warrior 컴포넌트 찾기
        var assasin = GetComponent<Assasin>();
        var warrior = GetComponent<Warrior>();
        
        if (assasin != null)
        {
            playerClass = assasin;
            currentDirectionPreset = assasin.GetDirectionPreset();
            
            if (showDebugLogs)
                Debug.Log($"🗺️ [PlayerAnimationController] Assasin 감지됨, DirectionPreset: {currentDirectionPreset}");
        }
        else if (warrior != null)
        {
            playerClass = warrior;
            currentDirectionPreset = warrior.GetDirectionPreset();
            
            if (showDebugLogs)
                Debug.Log($"🗺️ [PlayerAnimationController] Warrior 감지됨, DirectionPreset: {currentDirectionPreset}");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [PlayerAnimationController] 플레이어 클래스 미감지 - Assasin/Warrior 컴포넌트 없음");
        }
        
        if (playerClass != null && useIsometricData)
        {
            if (showDebugLogs)
                Debug.Log($"✅ [PlayerAnimationController] 아이소메트릭 데이터 연동 활성화: {playerClass.ClassName}");
        }
        else if (!useIsometricData)
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [PlayerAnimationController] IsometricData 비활성화됨 (useIsometricData = false)");
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
                    attackCooldown = weapon.GetEquipmentData().WeaponCooldown;  // GetWeaponInfo() → GetEquipmentData()
                    Debug.Log($"🟢 [PlayerAnimationController] 무기 쿨다운 설정: {attackCooldown}초");
                }
            }
            
            canAttack = true;
            canSkill1 = true; // ⭐ 스킬1 초기화
            canSkill2 = true; // ⭐ 스킬2 초기화
            isHit = false; // ⭐ 피격 상태 초기화
            Debug.Log("🟢 [PlayerAnimationController] Animation Parameters 초기화 완료!");
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
        
        if (showDebugLogs)
            Debug.Log($"🟢 [PlayerAnimationController] 공격 트리거 실행: BasicAttack (시퀀스 #{currentAttackSequence})");
        
        return true;
    }
    
    /// <summary>
    /// ⭐ 스킬1 실행 (신규 추가)
    /// </summary>
    public bool TriggerSkill1()
    {
        if (showDebugLogs)
            Debug.Log($"🔵 [PlayerAnimationController] TriggerSkill1 요청");
        
        // ⭐ 튜토리얼용 스킬 사용 이벤트 발생
        OnSkill1Used?.Invoke();
        
        // 스킬1 가능 여부 확인 (BaseSkill의 CanUse로 대체)
        if (skillController != null)
        {
            var skill1 = skillController.SkillSet?.GetSkill(0);
            if (skill1 != null && !skill1.CanUse())
            {
                if (showDebugLogs)
                    Debug.LogWarning("🟡 [PlayerAnimationController] 스킬1 쿨다운 중입니다!");
                return false;
            }
        }
        
        // Animation Parameters 설정
        try
        {
            if (HasParameter(animator, "isSkill1"))
                animator.SetBool(IS_SKILL1_HASH, true);
                
            if (HasParameter(animator, "Skill1"))
            {
                animator.SetTrigger(SKILL1_TRIGGER_HASH);
                
                // 🆕 스킬1 이동 제한 적용 (완전정지)
                if (playerController != null)
                {
                    playerController.ApplyAttackMovementRestriction(PlayerAttackType.Skill);
                    Debug.Log("⚔️ [TriggerSkill1] 스킬1 이동 제한 적용 (완전정지)");
                }
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
        
        // ❌ 제거: 중복 쿨다운 시작
        // StartCoroutine(Skill1CooldownRoutine());
        
        if (showDebugLogs)
            Debug.Log($"🟢 [PlayerAnimationController] 스킬1 트리거 실행!");
        
        return true;
    }
    
    /// <summary>
    /// ⭐ 스킬2 실행 (신규 추가)
    /// </summary>
    public bool TriggerSkill2()
    {
        if (showDebugLogs)
            Debug.Log($"🔵 [PlayerAnimationController] TriggerSkill2 요청");
        
        // ⭐ 튜토리얼용 스킬 사용 이벤트 발생
        OnSkill2Used?.Invoke();
        
        // 스킬2 가능 여부 확인 (BaseSkill의 CanUse로 대체)
        if (skillController != null)
        {
            var skill2 = skillController.SkillSet?.GetSkill(1);
            if (skill2 != null && !skill2.CanUse())
            {
                if (showDebugLogs)
                    Debug.LogWarning("🟡 [PlayerAnimationController] 스킬2 쿨다운 중입니다!");
                return false;
            }
        }
        
        // Animation Parameters 설정
        try
        {
            if (HasParameter(animator, "isSkill2"))
                animator.SetBool(IS_SKILL2_HASH, true);
                
            if (HasParameter(animator, "Skill2"))
            {
                animator.SetTrigger(SKILL2_TRIGGER_HASH);
                
                // 🆕 스킬2 이동 제한 적용 (완전정지)
                if (playerController != null)
                {
                    playerController.ApplyAttackMovementRestriction(PlayerAttackType.Skill);
                    Debug.Log("⚔️ [TriggerSkill2] 스킬2 이동 제한 적용 (완전정지)");
                }
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
        
        // ❌ 제거: 중복 쿨다운 시작
        // StartCoroutine(Skill2CooldownRoutine());
        
        if (showDebugLogs)
            Debug.Log($"🟢 [PlayerAnimationController] 스킬2 트리거 실행!");
        
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
            if (showDebugLogs && activeWeapon != null)
                Debug.Log("🔄 [PlayerAnimationController] ActiveWeapon 재검색 성공");
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
        if (showDebugLogs && Time.frameCount % 120 == 0) // 2초마다
        {
            Debug.Log($"🔍 [PlayerAnimationController] 공격 가능 여부 체크:");
            Debug.Log($"   - animatorIsAttacking: {animatorIsAttacking}");
            Debug.Log($"   - animatorIsSkill1: {animatorIsSkill1}");
            Debug.Log($"   - animatorIsSkill2: {animatorIsSkill2}"); // ⭐ 추가
            Debug.Log($"   - animatorIsHit: {animatorIsHit}"); // ⭐ 추가
            Debug.Log($"   - hasWeapon: {hasWeapon}");
            Debug.Log($"   - canAttack (스크립트): {canAttack}");
        }
        
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
        
        // ⭐ 글로벌 쿨다운과 스킬2 활성 상태 추가 체크
        bool result = !animatorIsAttacking && !animatorIsSkill1 && !animatorIsSkill2 && 
                     !animatorIsHit && hasSkillController && canSkill1 && 
                     globalCooldownReady && !isAnySkillActive;
        
        if (showDebugLogs && !result && Time.frameCount % 60 == 0)
        {
            Debug.Log($"🟡 [PlayerAnimationController] 스킬1 불가능 - globalCooldownReady: {globalCooldownReady}, isAnySkillActive: {isAnySkillActive}");
        }
        
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
        
        // ⭐ 글로벌 쿨다운과 스킬1 활성 상태 추가 체크
        bool result = !animatorIsAttacking && !animatorIsSkill1 && !animatorIsSkill2 && 
                     !animatorIsHit && hasSkillController && canSkill2 && 
                     globalCooldownReady && !isAnySkillActive;
        
        if (showDebugLogs && !result && Time.frameCount % 60 == 0)
        {
            Debug.Log($"🟡 [PlayerAnimationController] 스킬2 불가능 - globalCooldownReady: {globalCooldownReady}, isAnySkillActive: {isAnySkillActive}");
        }
        
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
                        Debug.Log($"🕐 [OnSkill1Start] BaseSkill 쿨다운 시작: {skill1.SkillName} (쿨다운: {skill1.Cooldown}초)");
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
        // Animation Parameters 리셋
        if (HasParameter(animator, "isSkill1"))
            animator.SetBool(IS_SKILL1_HASH, false);
        
        // 스킬1 이동 제한 해제
        if (playerController != null)
        {
            playerController.RestoreNormalMovement();
        }
        
        // 스킬1 쿨다운 시작
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
                        Debug.Log($"🕐 [OnSkill2Start] BaseSkill 쿨다운 시작: {skill2.SkillName} (쿨다운: {skill2.Cooldown}초)");
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
        // Animation Parameters 리셋
        if (HasParameter(animator, "isSkill2"))
            animator.SetBool(IS_SKILL2_HASH, false);
        
        // 스킬2 이동 제한 해제
        if (playerController != null)
        {
            playerController.RestoreNormalMovement();
        }
        
        // 스킬2 쿨다운 시작
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
                if (showDebugLogs)
                    Debug.Log("🟢 [PlayerAnimationController] 무기 공격 직접 실행 완료");
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
            if (showDebugLogs)
                Debug.Log("🟢 [PlayerAnimationController] 스킬1 Animation Event 실행 완료");
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
            if (showDebugLogs)
                Debug.Log("🟢 [PlayerAnimationController] 스킬2 Animation Event 실행 완료");
        }
        
        // ❌ 제거: 중복 쿨다운
        // StartCoroutine(Skill2CooldownRoutine());
    }
    
    /// <summary>
    /// 공격 쿨다운 코루틴
    /// </summary>
    private IEnumerator AttackCooldownRoutine()
    {
        if (showDebugLogs)
            Debug.Log($"🔵 [PlayerAnimationController] 공격 쿨다운 시작: {attackCooldown}초");
        
        yield return new WaitForSeconds(attackCooldown);
        
        canAttack = true;
        isAttacking = false;
        
        if (showDebugLogs)
            Debug.Log("🟢 [PlayerAnimationController] 공격 쿨다운 완료!");
    }
    
    /// <summary>
    /// ⭐ 스킬1 쿨다운 코루틴 - 글로벌 상태 리셋 추가
    /// </summary>
    private IEnumerator Skill1CooldownRoutine()
    {
        if (showDebugLogs)
            Debug.Log($"🔵 [PlayerAnimationController] 스킬1 쿨다운 시작: {skill1Cooldown}초");
        
        yield return new WaitForSeconds(skill1Cooldown);
        
        canSkill1 = true;
        isSkill1 = false;
        
        // ⭐ 글로벌 스킬 상태 리셋
        isAnySkillActive = false;
        
        if (showDebugLogs)
            Debug.Log("🟢 [PlayerAnimationController] 스킬1 쿨다운 완료! (글로벌 쿨다운 해제)");
    }
    
    /// <summary>
    /// ⭐ 스킬2 쿨다운 코루틴 - 글로벌 상태 리셋 추가
    /// </summary>
    private IEnumerator Skill2CooldownRoutine()
    {
        if (showDebugLogs)
            Debug.Log($"🔵 [PlayerAnimationController] 스킬2 쿨다운 시작: {skill2Cooldown}초");
        
        yield return new WaitForSeconds(skill2Cooldown);
        
        canSkill2 = true;
        isSkill2 = false;
        
        // ⭐ 글로벌 스킬 상태 리셋
        isAnySkillActive = false;
        
        if (showDebugLogs)
            Debug.Log("🟢 [PlayerAnimationController] 스킬2 쿨다운 완료! (글로벌 쿨다운 해제)");
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
    /// </summary>
    public void SyncWithRuntimeStats()
    {
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

        if (showDebugLogs)
            Debug.Log($"🔵 [PlayerAnimationController] 공격 쿨다운 재계산: 무기기본={baseWeaponCooldown:F3}s / ASPD={atkSpeed:F2} = {attackCooldown:F3}s");
    }
    
    /// <summary>
    /// ⭐ 스킬1 쿨다운 업데이트 (신규 추가)
    /// </summary>
    public void UpdateSkill1Cooldown(float newCooldown)
    {
        skill1Cooldown = newCooldown;
        if (showDebugLogs)
            Debug.Log($"🔵 [PlayerAnimationController] 스킬1 쿨다운 업데이트: {skill1Cooldown}초");
    }
    
    /// <summary>
    /// ⭐ 스킬2 쿨다운 업데이트 (신규 추가)
    /// </summary>
    public void UpdateSkill2Cooldown(float newCooldown)
    {
        skill2Cooldown = newCooldown;
        if (showDebugLogs)
            Debug.Log($"🔵 [PlayerAnimationController] 스킬2 쿨다운 업데이트: {skill2Cooldown}초");
    }
    
    /// <summary>
    /// 피격 트리거 실행 (안전장치 추가)
    /// </summary>
    public bool TriggerHit()
    {
        Debug.Log("🔴 [PlayerAnimationController] 피격 트리거 실행 시작");
        
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
                Debug.Log("🟡 [PlayerAnimationController] 공격 중단됨");
            }
            
            // ⭐ 현재 스킬1 중이면 스킬1 중단
            if (HasParameter(animator, "isSkill1"))
            {
                animator.SetBool(IS_SKILL1_HASH, false);
                Debug.Log("🟡 [PlayerAnimationController] 스킬1 중단됨");
            }

            // ⭐ 현재 스킬2 중이면 스킬2 중단
            if (HasParameter(animator, "isSkill2"))
            {
                animator.SetBool(IS_SKILL2_HASH, false);
                Debug.Log("🟡 [PlayerAnimationController] 스킬2 중단됨");
            }
            
            // ⭐ 피격 상태 시작
            if (HasParameter(animator, "isHit"))
            {
                animator.SetBool(IS_HIT_HASH, true);
                isHit = true;
                Debug.Log("🔴 [PlayerAnimationController] isHit = true 설정됨");
            }
            
            // Hit Trigger 존재 여부 확인 후 실행
            if (HasParameter(animator, "Hit"))
            {
                animator.SetTrigger(HIT_TRIGGER_HASH);
                Debug.Log("🟢 [PlayerAnimationController] Hit Trigger 실행됨!");
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
        Debug.Log("🔴 [PlayerAnimationController] OnHitStart - Animation Event 호출됨!");
            
        // 피격 시 추가 로직 (예: 이동 제한, 공격 중단 등)
        isAttacking = false; // 공격 중단
        isSkill1 = false; // ⭐ 스킬1 중단
        isSkill2 = false; // ⭐ 스킬2 중단
        isHit = true; // ⭐ 피격 상태 유지
        
        // ⭐ Animator Parameter도 확실히 설정
        if (HasParameter(animator, "isHit"))
        {
            animator.SetBool(IS_HIT_HASH, true);
            Debug.Log("🔴 [PlayerAnimationController] OnHitStart: isHit = true 설정 확인");
        }
    }

    /// <summary>
    /// Animation Event: 피격 종료 시점 (Hit 애니메이션이 있을 때만 호출됨)
    /// </summary>
    public void OnHitEnd()
    {
        Debug.Log("🟢 [PlayerAnimationController] OnHitEnd - Animation Event 호출됨!");
        
        // ⭐ 피격 상태 종료
        isHit = false;
        if (HasParameter(animator, "isHit"))
        {
            animator.SetBool(IS_HIT_HASH, false);
            Debug.Log("🟢 [PlayerAnimationController] OnHitEnd: isHit = false 설정");
        }
        
        Debug.Log("🟢 [PlayerAnimationController] 피격 애니메이션 종료");
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
        Debug.Log($"🔍 [PlayerAnimationController] 현재 상태:");
        Debug.Log($"   CanAttack (Script): {canAttack}");
        Debug.Log($"   IsAttacking (Script): {isAttacking}");
        Debug.Log($"   CanSkill1 (Script): {canSkill1}"); // ⭐ 추가
        Debug.Log($"   IsSkill1 (Script): {isSkill1}"); // ⭐ 추가
        Debug.Log($"   IsSkill2 (Script): {isSkill2}"); // ⭐ 스킬2 상태 추가
        Debug.Log($"   IsHit (Script): {isHit}"); // ⭐ 피격 상태 추가
        Debug.Log($"   IsAttacking (Animator): {animator.GetBool(IS_ATTACKING_HASH)}");
        if (HasParameter(animator, "isHit"))
            Debug.Log($"   IsHit (Animator): {animator.GetBool(IS_HIT_HASH)}"); // ⭐ 피격 상태 추가
        Debug.Log($"   Has Weapon: {activeWeapon?.CurrentActiveWeapon != null}");
        Debug.Log($"   Has SkillController: {skillController != null}"); // ⭐ 추가
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
        if (animator == null) return;
        
        try
        {
            Debug.Log($"🔍 [PlayerAnimationController] 현재 Animation 상태:");
                
            if (HasParameter(animator, "isAttacking"))
                Debug.Log($"   - isAttacking: {animator.GetBool(IS_ATTACKING_HASH)}");
                
            // ⭐ 스킬1 상태 로깅 추가
            if (HasParameter(animator, "isSkill1"))
                Debug.Log($"   - isSkill1: {animator.GetBool(IS_SKILL1_HASH)}");
                
            // ⭐ 스킬2 상태 로깅 추가 (신규)
            if (HasParameter(animator, "isSkill2"))
                Debug.Log($"   - isSkill2: {animator.GetBool(IS_SKILL2_HASH)}");
                
            // ⭐ 피격 상태 로깅 추가
            if (HasParameter(animator, "isHit"))
                Debug.Log($"   - isHit: {animator.GetBool(IS_HIT_HASH)}");
                
            if (HasParameter(animator, "moveX"))
                // Debug.Log($"   - moveX: {animator.GetFloat("moveX")}");
                
            if (HasParameter(animator, "moveY"))
                Debug.Log($"   - moveY: {animator.GetFloat("moveY")}");
                
            // 현재 애니메이션 상태 이름
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            string stateName = GetStateName(stateInfo.shortNameHash);
            Debug.Log($"   - State 이름: {stateName} (시간: {stateInfo.normalizedTime})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🔴 [PlayerAnimationController] Animation 상태 확인 실패: {e.Message}");
        }
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
        if (showDebugLogs)
            Debug.Log($"🚀 [PlayerAnimationController] TriggerDash 호출: {direction}");
        
        if (animator == null)
        {
            Debug.LogError("🔴 [PlayerAnimationController] Animator가 null입니다!");
            return false;
        }
        
        if (!canDash)
        {
            if (showDebugLogs)
                Debug.Log("🟡 [PlayerAnimationController] 대시 쿨다운 중입니다!");
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
            
            if (showDebugLogs)
                Debug.Log($"🟢 [PlayerAnimationController] 대시 트리거 성공! 방향: {dashDirection}");
            
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
        
        if (showDebugLogs)
            Debug.Log("🟢 [PlayerAnimationController] 대시 완료");
        
        // 쿨다운
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
        
        if (showDebugLogs)
            Debug.Log("🟢 [PlayerAnimationController] 대시 쿨다운 완료");
    }
    
    /// <summary>
    /// ⭐ Animation Event: 대시 시작 시점
    /// </summary>
    public void OnDashStart()
    {
        if (showDebugLogs)
            Debug.Log("🚀 [PlayerAnimationController] OnDashStart - Animation Event");
        
        // 추가 대시 로직 (파티클 이펙트 등)이 필요하면 여기에 구현
    }
    
    /// <summary>
    /// ⭐ Animation Event: 대시 완료 시점  
    /// </summary>
    public void OnDashComplete()
    {
        if (showDebugLogs)
            Debug.Log("🚀 [PlayerAnimationController] OnDashComplete - Animation Event");
        
        // 대시 완료 시 추가 로직이 필요하면 여기에 구현
    }
} 