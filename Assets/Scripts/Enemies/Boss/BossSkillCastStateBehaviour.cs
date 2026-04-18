using UnityEngine;

/// <summary>
/// 보스 SkillCast 상태 StateMachineBehaviour
/// Cast 이펙트 + Telegraph 생성
/// </summary>
public class BossSkillCastStateBehaviour : StateMachineBehaviour
{
    private BossSkillController skillController;
    
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        if (skillController == null)
        {
            skillController = animator.GetComponent<BossSkillController>();
        }
        
        if (skillController != null)
        {
            skillController.OnSkillCastStart();
        }
        else
        {
            Debug.LogError($"❌ [BossSkillCastStateBehaviour] {animator.gameObject.name}: BossSkillController를 찾을 수 없습니다!");
        }
    }
    
    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        // ⭐ 중복 호출 방지: OnSkillCastComplete()는 AutoTriggerSkillActionAfterCastTime() 코루틴에서만 호출
        // skillController.OnSkillCastComplete()는 코루틴에서 Cast Time 후 자동 호출됨
    }
}

