using UnityEngine;

/// <summary>
/// Elite 스킬 Action 단계 제어 (데미지 적용)
/// Animator의 SkillAction 상태에 연결됨
/// </summary>
public class EliteSkillActionStateBehaviour : StateMachineBehaviour
{
    private EliteSkillController skillController;
    private bool actionExecuted = false;
    
    [Header("⏱️ 타이밍 설정")]
    [Tooltip("데미지 적용 시점 (0~1, 0.0=즉시, 0.3=애니메이션 진행 후)")]
    [Range(0f, 1f)]
    [SerializeField] private float damageTimingPoint = 0.3f;
    
    [Tooltip("State 진입 즉시 실행 (true: OnStateEnter에서 실행, false: normalizedTime 기반)")]
    [SerializeField] private bool executeOnEnter = false;
    
    [Header("🎮 디버그")]
    [SerializeField] private bool enableDebugLogs = true;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (skillController == null)
        {
            skillController = animator.GetComponent<EliteSkillController>();
            
            if (skillController == null)
            {
                Debug.LogError($"❌ [EliteSkillActionStateBehaviour] EliteSkillController를 찾을 수 없습니다!");
                return;
            }
        }
        
        actionExecuted = false;
        
        // executeOnEnter 옵션: State 진입 즉시 실행
        if (executeOnEnter)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"⚡ [SkillAction] 즉시 실행 모드 - ExecuteSkillAction() 호출!");
            }
            skillController.ExecuteSkillAction();
            actionExecuted = true;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎬 [SkillAction] 시작 - {animator.name}");
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (skillController == null) return;
        
        float normalizedTime = stateInfo.normalizedTime % 1f;
        
        // ⭐⭐ 디버깅: normalizedTime과 isSkillAction 상태 출력
        if (enableDebugLogs && Time.frameCount % 30 == 0) // 30프레임마다 1번
        {
            bool isSkillActionParam = animator.GetBool("isSkillAction");
            Debug.Log($"🔍 [SkillAction Update] Time: {normalizedTime:F2}, isSkillAction: {isSkillActionParam}");
        }
        
        // 데미지 적용 타이밍
        if (!actionExecuted && normalizedTime >= damageTimingPoint)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"💥 [SkillAction] 데미지 적용! (Time: {normalizedTime:F2})");
            }
            
            skillController.ExecuteSkillAction();
            actionExecuted = true;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (skillController == null) return;
        
        // 안전장치: 타이밍을 놓친 경우 강제 실행
        if (!actionExecuted)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"⚠️ [SkillAction] 타이밍 놓침! Exit에서 강제 실행");
            }
            skillController.ExecuteSkillAction();
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"✅ [SkillAction] 종료 - {animator.name}");
        }
        
        skillController.OnSkillActionComplete();
    }
}

