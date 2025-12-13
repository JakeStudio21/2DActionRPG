using UnityEngine;

/// <summary>
/// 보스 SkillAction 상태 StateMachineBehaviour
/// 데미지 판정 타이밍 제어
/// </summary>
public class BossSkillActionStateBehaviour : StateMachineBehaviour
{
    [Header("⏱️ 데미지 타이밍")]
    [Tooltip("스킬 실행 타이밍 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float damageTimingPoint = 0.5f;
    
    private BossSkillController skillController;
    private bool damageExecuted = false;
    
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (skillController == null)
        {
            skillController = animator.GetComponent<BossSkillController>();
        }
        
        damageExecuted = false;
    }
    
    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 데미지 타이밍 도달 시 실행 (한 번만)
        if (!damageExecuted && stateInfo.normalizedTime >= damageTimingPoint)
        {
            if (skillController != null)
            {
                skillController.ExecuteSkillAction();
                damageExecuted = true;
            }
        }
    }
    
    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (skillController != null)
        {
            skillController.OnSkillActionComplete();
        }
        
        damageExecuted = false;
    }
}




