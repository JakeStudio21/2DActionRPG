using UnityEngine;

/// <summary>
/// 보스 SkillAction 상태 StateMachineBehaviour
/// VFX/Damage 타이밍 제어 (SkillData 설정 기반)
/// </summary>
public class BossSkillActionStateBehaviour : StateMachineBehaviour
{
    [Header("⏱️ 레거시 데미지 타이밍 (하위 호환성 - SkillData 설정 우선)")]
    [Tooltip("레거시: 스킬 실행 타이밍 (0~1) - SkillData에 타이밍 설정이 없을 때만 사용")]
    [Range(0f, 1f)]
    [SerializeField] private float damageTimingPoint = 0.5f;
    
    private BossSkillController skillController;
    private bool vfxExecuted = false;
    private bool damageExecuted = false;
    
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (skillController == null)
        {
            skillController = animator.GetComponent<BossSkillController>();
        }
        
        // SkillData 가져오기
        SkillData skillData = skillController?.CurrentSkill;
        
        if (skillData != null)
        {
            // VFX 모드가 OnStateEnter이면 즉시 실행
            if (skillData.VfxMode == SkillTimingMode.OnStateEnter)
            {
                ExecuteVFX();
            }
            
            // Damage 모드가 OnStateEnter이면 즉시 실행
            if (skillData.DamageMode == SkillTimingMode.OnStateEnter)
            {
                ExecuteDamage();
            }
        }
        
        vfxExecuted = false;
        damageExecuted = false;
    }
    
    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (skillController == null) return;
        
        SkillData skillData = skillController.CurrentSkill;
        if (skillData == null) return;
        
        float normalizedTime = stateInfo.normalizedTime % 1f;
        
        // VFX 타이밍 체크
        CheckAndExecuteVFX(skillData, normalizedTime);
        
        // Damage 타이밍 체크
        CheckAndExecuteDamage(skillData, normalizedTime);
    }
    
    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (skillController == null) return;
        
        SkillData skillData = skillController.CurrentSkill;
        if (skillData != null)
        {
            // VFX 모드가 OnStateExit이면 실행 (아직 실행되지 않았을 때만)
            if (skillData.VfxMode == SkillTimingMode.OnStateExit && !vfxExecuted)
            {
                ExecuteVFX();
            }
            
            // Damage 모드가 OnStateExit이면 실행 (아직 실행되지 않았을 때만)
            if (skillData.DamageMode == SkillTimingMode.OnStateExit && !damageExecuted)
            {
                ExecuteDamage();
            }
        }
        
        // 완료 처리
        skillController.OnSkillActionComplete();
        
        vfxExecuted = false;
        damageExecuted = false;
    }
    
    /// <summary>
    /// VFX 타이밍 체크 및 실행
    /// </summary>
    private void CheckAndExecuteVFX(SkillData skillData, float normalizedTime)
    {
        if (vfxExecuted) return;
        
        float triggerTime = GetTriggerTime(skillData.VfxMode, skillData.VfxTime);
        
        if (normalizedTime >= triggerTime)
        {
            ExecuteVFX();
            vfxExecuted = true;
        }
    }
    
    /// <summary>
    /// Damage 타이밍 체크 및 실행
    /// </summary>
    private void CheckAndExecuteDamage(SkillData skillData, float normalizedTime)
    {
        if (damageExecuted) return;
        
        float triggerTime = GetTriggerTime(skillData.DamageMode, skillData.DamageTime);
        
        if (normalizedTime >= triggerTime)
        {
            ExecuteDamage();
            damageExecuted = true;
        }
    }
    
    /// <summary>
    /// 타이밍 모드에 따른 트리거 시간 계산 (0.0 ~ 1.0)
    /// </summary>
    private float GetTriggerTime(SkillTimingMode mode, float customTime)
    {
        switch (mode)
        {
            case SkillTimingMode.OnStateEnter:
                return 0f;
            case SkillTimingMode.OnStateExit:
                return 1f;
            case SkillTimingMode.CustomTime:
                return customTime;
            default:
                return 0.5f;
        }
    }
    
    /// <summary>
    /// VFX 실행
    /// </summary>
    private void ExecuteVFX()
    {
        if (skillController != null)
        {
            skillController.ExecuteSkillVFX();
        }
    }
    
    /// <summary>
    /// Damage 실행
    /// </summary>
    private void ExecuteDamage()
    {
        if (skillController != null)
        {
            skillController.ExecuteSkillDamage();
        }
    }
}




