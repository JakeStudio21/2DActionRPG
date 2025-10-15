using UnityEngine;

/// <summary>
/// BlendTree의 Attack State에서 Animation Event를 통합 관리하는 StateMachineBehaviour
/// 개별 애니메이션의 Animation Event 대신 State 수준에서 OnAttackStart/Complete 호출
/// </summary>
public class AttackStateBehaviour : StateMachineBehaviour
{
    [Header("🎯 Attack Timing Settings")]
    [SerializeField] private float attackStartTime = 0.4f;  // 40% 지점에서 OnAttackStart
    [SerializeField] private float attackCompleteTime = 0.8f; // 80% 지점에서 OnAttackComplete
    
    private bool attackStartTriggered = false;
    private bool attackCompleteTriggered = false;
    
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"🎬 [AttackStateBehaviour] Attack State 진입 - 길이: {stateInfo.length:F3}초");
        
        // 상태 초기화
        attackStartTriggered = false;
        attackCompleteTriggered = false;
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float normalizedTime = stateInfo.normalizedTime;
        
        // 🎯 OnAttackStart 호출 (40% 지점)
        if (!attackStartTriggered && normalizedTime >= attackStartTime)
        {
            attackStartTriggered = true;
            Debug.Log($"🎯 [AttackStateBehaviour] OnAttackStart 호출 - 진행도: {normalizedTime:F3}");
            
            // PlayerAnimationController의 OnAttackStart 호출
            var playerAnimationController = animator.GetComponent<PlayerAnimationController>();
            if (playerAnimationController != null)
            {
                playerAnimationController.OnAttackStart();
            }
            else
            {
                Debug.LogError("❌ [AttackStateBehaviour] PlayerAnimationController를 찾을 수 없습니다!");
            }
        }
        
        // 🏁 OnAttackComplete 호출 (80% 지점)
        if (!attackCompleteTriggered && normalizedTime >= attackCompleteTime)
        {
            attackCompleteTriggered = true;
            Debug.Log($"🏁 [AttackStateBehaviour] OnAttackComplete 호출 - 진행도: {normalizedTime:F3}");
            
            // PlayerAnimationController의 OnAttackComplete 호출
            var playerAnimationController = animator.GetComponent<PlayerAnimationController>();
            if (playerAnimationController != null)
            {
                playerAnimationController.OnAttackComplete();
            }
            else
            {
                Debug.LogError("❌ [AttackStateBehaviour] PlayerAnimationController를 찾을 수 없습니다!");
            }
        }
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Debug.Log($"🚪 [AttackStateBehaviour] Attack State 종료 - 최종 진행도: {stateInfo.normalizedTime:F3}");
        
        // 🔒 안전장치: State 종료 시 Complete가 호출되지 않았다면 강제 호출
        if (!attackCompleteTriggered)
        {
            Debug.LogWarning($"⚠️ [AttackStateBehaviour] State 종료 전 강제 OnAttackComplete 호출");
            
            var playerAnimationController = animator.GetComponent<PlayerAnimationController>();
            if (playerAnimationController != null)
            {
                playerAnimationController.OnAttackComplete();
            }
        }
    }
}
