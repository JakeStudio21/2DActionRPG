using UnityEngine;

/// <summary>
/// 엘리트 스킬 캐스팅 State Behaviour
/// SkillCast BlendTree에 부착하여 타이밍 제어
/// normalizedTime 기반으로 정확한 타이밍에 이벤트 발동
/// </summary>
public class EliteSkillCastStateBehaviour : StateMachineBehaviour
{
    [Header("⏱️ 타이밍 설정")]
    [Tooltip("Cast 시작 타이밍 (0.0 = 0%, 0.5 = 50%, 1.0 = 100%)")]
    [Range(0f, 1f)]
    [SerializeField] private float castStartTime = 0.0f;
    
    [Tooltip("Cast 완료 타이밍 (보통 95% 지점)")]
    [Range(0f, 1f)]
    [SerializeField] private float castCompleteTime = 0.95f;
    
    [Header("🎮 디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 중복 호출 방지 플래그
    private bool castStartTriggered = false;
    private bool castCompleteTriggered = false;
    
    // 컴포넌트 캐시
    private EliteSkillController skillController;
    private EnemyAnimationController animController;

    /// <summary>
    /// State 진입 시 호출
    /// </summary>
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 플래그 초기화
        castStartTriggered = false;
        castCompleteTriggered = false;
        
        // 컴포넌트 캐시 (최초 1회만)
        if (skillController == null)
        {
            skillController = animator.GetComponent<EliteSkillController>();
        }
        
        if (animController == null)
        {
            animController = animator.GetComponent<EnemyAnimationController>();
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"🔮 [EliteSkillCastStateBehaviour] {animator.gameObject.name} - SkillCast State 진입");
        }
    }

    /// <summary>
    /// State 업데이트 (매 프레임)
    /// </summary>
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // normalizedTime: 0.0 ~ 1.0 (루프 고려)
        float normalizedTime = stateInfo.normalizedTime % 1f;
        
        // Cast 시작 이벤트 (최초 1회)
        if (!castStartTriggered && normalizedTime >= castStartTime)
        {
            castStartTriggered = true;
            OnCastStart(animator);
            
            if (enableDebugLogs)
            {
                Debug.Log($"✨ [EliteSkillCastStateBehaviour] {animator.gameObject.name} - Cast 시작! (진행도: {normalizedTime:F3})");
            }
        }
        
        // Cast 완료 이벤트 (최초 1회)
        if (!castCompleteTriggered && normalizedTime >= castCompleteTime)
        {
            castCompleteTriggered = true;
            OnCastComplete(animator);
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎯 [EliteSkillCastStateBehaviour] {animator.gameObject.name} - Cast 완료! (진행도: {normalizedTime:F3})");
            }
        }
    }

    /// <summary>
    /// State 종료 시 호출
    /// </summary>
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 안전장치: Cast 완료 플래그 보장
        if (!castCompleteTriggered)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"⚠️ [EliteSkillCastStateBehaviour] {animator.gameObject.name} - Cast 완료 전 State 종료! 강제 완료 처리");
            }
            OnCastComplete(animator);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"🚪 [EliteSkillCastStateBehaviour] {animator.gameObject.name} - SkillCast State 종료 (최종 진행도: {stateInfo.normalizedTime:F3})");
        }
    }

    /// <summary>
    /// Cast 시작 처리
    /// </summary>
    private void OnCastStart(Animator animator)
    {
        if (skillController != null)
        {
            // EliteSkillController의 콜백 호출
            skillController.OnSkillCastStart();
        }
        else
        {
            Debug.LogError($"[EliteSkillCastStateBehaviour] {animator.gameObject.name}: EliteSkillController가 없습니다!");
        }
    }

    /// <summary>
    /// Cast 완료 처리
    /// </summary>
    private void OnCastComplete(Animator animator)
    {
        if (skillController != null)
        {
            // EliteSkillController의 콜백 호출
            skillController.OnSkillCastComplete();
        }
        else
        {
            Debug.LogError($"[EliteSkillCastStateBehaviour] {animator.gameObject.name}: EliteSkillController가 없습니다!");
        }
        
        // 애니메이션 상태 업데이트
        if (animController != null)
        {
            animController.SetSkillCasting(false);
        }
    }
}


