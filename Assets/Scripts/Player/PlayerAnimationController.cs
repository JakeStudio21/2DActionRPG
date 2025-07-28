using UnityEngine;
using System.Collections;

/// <summary>
/// Animation Parameters 기반 플레이어 액션 시스템 통합 컨트롤러
/// 향후 피격, 스킬1, 스킬2 등으로 확장 예정
/// </summary>
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Animation Parameters")]
    [SerializeField] private bool showDebugLogs = true;
    
    private Animator animator;
    private ActiveWeapon activeWeapon;
    private SkillController skillController; // ⭐ 스킬 컨트롤러 참조 추가
    
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
    
    // 공격 상태 추적
    private bool isAttacking = false;
    private bool canAttack = true;
    private float attackCooldown = 1f;
    
    // ⭐ 스킬1 상태 추적 추가
    private bool isSkill1 = false;
    private bool canSkill1 = true;
    private float skill1Cooldown = 2f;
    
    // ⭐ 스킬2 상태 추적 추가 (신규)
    private bool isSkill2 = false;
    private bool canSkill2 = true;
    private float skill2Cooldown = 3f; // 스킬2는 조금 더 긴 쿨다운
    
    // ⭐ 글로벌 스킬 쿨다운 추가 (핵심 해결책)
    private bool isAnySkillActive = false;
    private float globalSkillCooldown = 0.3f; // 0.3초 글로벌 쿨다운
    private float lastGlobalSkillTime = -Mathf.Infinity;
    
    // ⭐ 피격 상태 추적 추가
    private bool isHit = false;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        
        // ⭐ 현재 사용 중인 Animation Controller 로그 출력
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            Debug.Log($"🔍 [PlayerAnimationController] 사용 중인 Controller: {animator.runtimeAnimatorController.name}");
            
            // 모든 Parameters 출력
            Debug.Log($"🔍 [PlayerAnimationController] 사용 가능한 Parameters:");
            foreach (var param in animator.parameters)
            {
                Debug.Log($"   - {param.name} ({param.type})");
            }
        }
        
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
                    if (showDebugLogs)
                        Debug.Log("🔍 [PlayerAnimationController] FindObjectOfType으로 ActiveWeapon 찾음");
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
                    if (showDebugLogs)
                        Debug.Log("🔍 [PlayerAnimationController] FindObjectOfType으로 SkillController 찾음");
                }
            }
        }
        
        if (animator == null)
        {
            Debug.LogError("🔴 [PlayerAnimationController] Animator 컴포넌트를 찾을 수 없습니다!");
            return;
        }
        
        if (activeWeapon == null)
        {
            Debug.LogError("🔴 [PlayerAnimationController] ActiveWeapon을 찾을 수 없습니다!");
            return;
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"🟢 [PlayerAnimationController] ActiveWeapon 찾음: {activeWeapon.name}");
        }
        
        if (skillController == null)
        {
            Debug.LogError("🔴 [PlayerAnimationController] SkillController를 찾을 수 없습니다! 동일한 GameObject에 있는지 확인하세요.");
            
            // ⭐ 추가 디버깅: 같은 GameObject의 모든 컴포넌트 확인
            var allComponents = GetComponents<MonoBehaviour>();
            Debug.Log($"🔍 [PlayerAnimationController] 같은 GameObject의 MonoBehaviour 컴포넌트들:");
            foreach (var comp in allComponents)
            {
                if (comp != null)
                    Debug.Log($"   - {comp.GetType().Name}: {comp.name}");
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"🟢 [PlayerAnimationController] SkillController 찾음: {skillController.name}");
            
            // SkillController에서 쿨다운 시간 가져오기
            skill1Cooldown = skillController.CooldownTime;
        }
        
        // 초기 Animation Parameters 설정
        InitializeAnimationParameters();
        
        if (showDebugLogs)
            Debug.Log("🟢 [PlayerAnimationController] 초기화 완료!");
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
        if (showDebugLogs)
            Debug.Log($"🔵 [PlayerAnimationController] TriggerAttack 요청: BasicAttack");
        
        // 공격 가능 여부 확인
        if (!CanPerformAttack())
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [PlayerAnimationController] 공격 불가능 상태입니다!");
            return false;
        }
        
        // Animation Parameters 안전하게 설정
        try
        {
            if (HasParameter(animator, "isAttacking"))
                animator.SetBool(IS_ATTACKING_HASH, true);
                
            if (HasParameter(animator, "Attack"))
                animator.SetTrigger(ATTACK_TRIGGER_HASH);
            else
            {
                Debug.LogWarning("[PlayerAnimationController] 'Attack' Trigger Parameter가 없어서 무기 공격을 직접 실행합니다.");
                ExecuteWeaponAttack();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerAnimationController] Animation Parameters 설정 중 오류: {e.Message}");
            return false;
        }
        
        // 내부 상태 업데이트
        isAttacking = true;
        canAttack = false;
        
        if (showDebugLogs)
            Debug.Log($"🟢 [PlayerAnimationController] 공격 트리거 실행: BasicAttack");
        
        return true;
    }
    
    /// <summary>
    /// ⭐ 스킬1 실행 (신규 추가)
    /// </summary>
    public bool TriggerSkill1()
    {
        if (showDebugLogs)
            Debug.Log($"🔵 [PlayerAnimationController] TriggerSkill1 요청");
        
        // 스킬1 가능 여부 확인
        if (!CanPerformSkill1())
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [PlayerAnimationController] 스킬1 불가능 상태입니다!");
            return false;
        }
        
        // Animation Parameters 안전하게 설정
        try
        {
            if (HasParameter(animator, "isSkill1"))
                animator.SetBool(IS_SKILL1_HASH, true);
                
            if (HasParameter(animator, "Skill1"))
                animator.SetTrigger(SKILL1_TRIGGER_HASH);
            else
            {
                Debug.LogWarning("[PlayerAnimationController] 'Skill1' Trigger Parameter가 없어서 스킬을 직접 실행합니다.");
                ExecuteSkill1();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerAnimationController] Skill1 Animation Parameters 설정 중 오류: {e.Message}");
            return false;
        }
        
        // 내부 상태 업데이트
        isSkill1 = true;
        canSkill1 = false;
        
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
        
        // 스킬2 가능 여부 확인
        if (!CanPerformSkill2())
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [PlayerAnimationController] 스킬2 불가능 상태입니다!");
            return false;
        }
        
        // Animation Parameters 안전하게 설정
        try
        {
            if (HasParameter(animator, "isSkill2"))
                animator.SetBool(IS_SKILL2_HASH, true);
                
            if (HasParameter(animator, "Skill2"))
                animator.SetTrigger(SKILL2_TRIGGER_HASH);
            else
            {
                Debug.LogWarning("[PlayerAnimationController] 'Skill2' Trigger Parameter가 없어서 스킬2를 직접 실행합니다.");
                ExecuteSkill2();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerAnimationController] Skill2 Animation Parameters 설정 중 오류: {e.Message}");
            return false;
        }
        
        // 내부 상태 업데이트
        isSkill2 = true;
        canSkill2 = false;
        
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
        if (showDebugLogs)
            Debug.Log("🟢 [PlayerAnimationController] OnAttackStart - Animation Event");
        
        // 무기의 애니메이션 트리거 (무기별 애니메이션 실행)
        if (activeWeapon != null && activeWeapon.CurrentActiveWeapon != null)
        {
            var weaponAnimator = activeWeapon.CurrentActiveWeapon.GetComponent<Animator>();
            if (weaponAnimator != null)
            {
                // 무기별 애니메이션 트리거 (Bow: "Fire", Sword: "Attack" 등)
                if (activeWeapon.CurrentActiveWeapon.name.Contains("Bow"))
                {
                    weaponAnimator.SetTrigger("Fire");
                }
                else if (activeWeapon.CurrentActiveWeapon.name.Contains("Sword"))
                {
                    weaponAnimator.SetTrigger("Attack");
                }
                else if (activeWeapon.CurrentActiveWeapon.name.Contains("Staff"))
                {
                    weaponAnimator.SetTrigger("Attack");
                }
                
                if (showDebugLogs)
                    Debug.Log($"🟢 [PlayerAnimationController] 무기 애니메이션 트리거: {activeWeapon.CurrentActiveWeapon.name}");
            }
            
            // 무기의 순수 공격 로직도 실행
            var weapon = activeWeapon.CurrentActiveWeapon as IWeapon;
            if (weapon != null)
            {
                weapon.Attack();
            }
        }
    }
    
    /// <summary>
    /// Animation Event에서 호출: 공격 완료 시점
    /// </summary>
    public void OnAttackComplete()
    {
        if (showDebugLogs)
            Debug.Log("🟢 [PlayerAnimationController] OnAttackComplete - Animation Event");
        
        // Animation Parameters 리셋
        animator.SetBool(IS_ATTACKING_HASH, false);
        
        // 쿨다운 시작
        StartCoroutine(AttackCooldownRoutine());
    }
    
    /// <summary>
    /// ⭐ Animation Event에서 호출: 스킬1 시작 시점 (신규 추가)
    /// </summary>
    public void OnSkill1Start()
    {
        if (showDebugLogs)
            Debug.Log("🟢 [PlayerAnimationController] OnSkill1Start - Animation Event");
        
        // 스킬1 실행
        ExecuteSkill1();
    }
    
    /// <summary>
    /// ⭐ Animation Event에서 호출: 스킬1 완료 시점 (신규 추가)
    /// </summary>
    public void OnSkill1Complete()
    {
        if (showDebugLogs)
            Debug.Log("🟢 [PlayerAnimationController] OnSkill1Complete - Animation Event");
        
        // Animation Parameters 리셋
        if (HasParameter(animator, "isSkill1"))
            animator.SetBool(IS_SKILL1_HASH, false);
        
        // 스킬1 쿨다운 시작
        StartCoroutine(Skill1CooldownRoutine());
    }
    
    /// <summary>
    /// ⭐ Animation Event에서 호출: 스킬2 시작 시점 (신규 추가)
    /// </summary>
    public void OnSkill2Start()
    {
        if (showDebugLogs)
            Debug.Log("🟢 [PlayerAnimationController] OnSkill2Start - Animation Event");
        
        // 스킬2 실행
        ExecuteSkill2();
    }
    
    /// <summary>
    /// ⭐ Animation Event에서 호출: 스킬2 완료 시점 (신규 추가)
    /// </summary>
    public void OnSkill2Complete()
    {
        if (showDebugLogs)
            Debug.Log("🟢 [PlayerAnimationController] OnSkill2Complete - Animation Event");
        
        // Animation Parameters 리셋
        if (HasParameter(animator, "isSkill2"))
            animator.SetBool(IS_SKILL2_HASH, false);
        
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
            // ⭐ 수정: TriggerSkill() 대신 OnSkill1AnimationEvent() 호출 (무한 루프 방지)
            skillController.OnSkill1AnimationEvent();
            if (showDebugLogs)
                Debug.Log("🟢 [PlayerAnimationController] 스킬1 Animation Event 실행 완료");
        }
        else
        {
            Debug.LogError("🔴 [PlayerAnimationController] ExecuteSkill1: SkillController가 null입니다! 재검색 시도...");
            
            // ⭐ 실시간 재검색 시도
            skillController = FindObjectOfType<SkillController>();
            if (skillController != null)
            {
                Debug.Log("🟢 [PlayerAnimationController] SkillController 재검색 성공! 스킬1 실행");
                skillController.OnSkill1AnimationEvent();
            }
            else
            {
                Debug.LogError("🔴 [PlayerAnimationController] SkillController 재검색도 실패!");
            }
        }
        
        // 간단한 쿨다운 시작
        StartCoroutine(Skill1CooldownRoutine());
    }
    
    /// <summary>
    /// ⭐ 스킬2 직접 실행 (Animation Event 없이)
    /// </summary>
    private void ExecuteSkill2()
    {
        if (skillController != null)
        {
            // ⭐ 수정: TriggerSkill2() 대신 OnSkill2AnimationEvent() 호출 (무한 루프 방지)
            skillController.OnSkill2AnimationEvent();
            if (showDebugLogs)
                Debug.Log("🟢 [PlayerAnimationController] 스킬2 Animation Event 실행 완료");
        }
        else
        {
            Debug.LogError("🔴 [PlayerAnimationController] ExecuteSkill2: SkillController가 null입니다! 재검색 시도...");
            
            // ⭐ 실시간 재검색 시도
            skillController = FindObjectOfType<SkillController>();
            if (skillController != null)
            {
                Debug.Log("🟢 [PlayerAnimationController] SkillController 재검색 성공! 스킬2 실행");
                skillController.OnSkill2AnimationEvent();
            }
            else
            {
                Debug.LogError("🔴 [PlayerAnimationController] SkillController 재검색도 실패!");
            }
        }
        
        // 간단한 쿨다운 시작
        StartCoroutine(Skill2CooldownRoutine());
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
    /// 무기 변경 시 쿨다운 업데이트
    /// </summary>
    public void UpdateWeaponCooldown(float newCooldown)
    {
        attackCooldown = newCooldown;
        if (showDebugLogs)
            Debug.Log($"🔵 [PlayerAnimationController] 무기 쿨다운 업데이트: {attackCooldown}초");
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
    /// State Hash를 이름으로 변환 (디버깅용)
    /// </summary>
    private string GetStateName(int stateHash)
    {
        // 주요 State Hash들을 문자열로 매핑
        if (stateHash == Animator.StringToHash("Idle")) return "Idle";
        if (stateHash == Animator.StringToHash("Running")) return "Running";
        if (stateHash == Animator.StringToHash("Attack_Assain")) return "Attack_Assain";
        if (stateHash == Animator.StringToHash("Hit_Assain")) return "Hit_Assain";
        if (stateHash == Animator.StringToHash("Skill1_Assain")) return "Skill1_Assain"; // ⭐ 추가
        if (stateHash == Animator.StringToHash("Skill2_Assain")) return "Skill2_Assain"; // ⭐ 추가
        if (stateHash == Animator.StringToHash("Death")) return "Death";
        
        return $"Unknown({stateHash})";
    }
} 