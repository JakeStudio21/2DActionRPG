using UnityEngine;

/// <summary>
/// BlendTree의 Skill State에서 Animation Event를 통합 관리하는 StateMachineBehaviour
/// Skill1, Skill2 애니메이션의 Animation Event 대신 State 수준에서 스킬 실행 호출
/// </summary>
public class SkillStateBehaviour : StateMachineBehaviour
{
    [Header("🎯 Skill Type")]
    [SerializeField] private SkillType skillType = SkillType.Skill1;
    
    [Header("🎯 Skill Timing Settings")]
    [SerializeField] private float skillExecuteTime = 0.4f;  // 40% 지점에서 스킬 실행
    [SerializeField] private float skillCompleteTime = 0.8f; // 80% 지점에서 스킬 완료
    
    private bool skillExecuteTriggered = false;
    private bool skillCompleteTriggered = false;
    
    public enum SkillType
    {
        Skill1,
        Skill2
    }
    
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"🎬 [SkillStateBehaviour] {skillType} State 진입 - 길이: {stateInfo.length:F3}초");
        
        // 상태 초기화
        skillExecuteTriggered = false;
        skillCompleteTriggered = false;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float normalizedTime = stateInfo.normalizedTime;
        
        // 🎯 스킬 실행 호출 (40% 지점)
        if (!skillExecuteTriggered && normalizedTime >= skillExecuteTime)
        {
            skillExecuteTriggered = true;
            Debug.Log($"🎯 [SkillStateBehaviour] {skillType} 실행 호출 - 진행도: {normalizedTime:F3}");
            
            // PlayerAnimationController의 스킬 메서드 호출
            var playerAnimationController = animator.GetComponent<PlayerAnimationController>();
            if (playerAnimationController != null)
            {
                switch (skillType)
                {
                    case SkillType.Skill1:
                        playerAnimationController.OnSkill1Start();
                        break;
                    case SkillType.Skill2:
                        playerAnimationController.OnSkill2Start();
                        break;
                }
            }
            else
            {
                Debug.LogError("❌ [SkillStateBehaviour] PlayerAnimationController를 찾을 수 없습니다!");
            }
        }
        
        // 🏁 스킬 완료 호출 (80% 지점)
        if (!skillCompleteTriggered && normalizedTime >= skillCompleteTime)
        {
            skillCompleteTriggered = true;
            Debug.Log($"🏁 [SkillStateBehaviour] {skillType} 완료 호출 - 진행도: {normalizedTime:F3}");
            
            // PlayerAnimationController의 스킬 완료 메서드 호출
            var playerAnimationController = animator.GetComponent<PlayerAnimationController>();
            if (playerAnimationController != null)
            {
                switch (skillType)
                {
                    case SkillType.Skill1:
                        playerAnimationController.OnSkill1Complete();
                        break;
                    case SkillType.Skill2:
                        playerAnimationController.OnSkill2Complete();
                        break;
                }
            }
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"🚪 [SkillStateBehaviour] {skillType} State 종료 - 최종 진행도: {stateInfo.normalizedTime:F3}");
        
        // 🔒 안전장치: State 종료 시 Complete가 호출되지 않았다면 강제 호출
        if (!skillCompleteTriggered)
        {
            Debug.LogWarning($"⚠️ [SkillStateBehaviour] State 종료 전 강제 {skillType} 완료 호출");
            
            var playerAnimationController = animator.GetComponent<PlayerAnimationController>();
            if (playerAnimationController != null)
            {
                switch (skillType)
                {
                    case SkillType.Skill1:
                        playerAnimationController.OnSkill1Complete();
                        break;
                    case SkillType.Skill2:
                        playerAnimationController.OnSkill2Complete();
                        break;
                }
            }
        }
    }
}
