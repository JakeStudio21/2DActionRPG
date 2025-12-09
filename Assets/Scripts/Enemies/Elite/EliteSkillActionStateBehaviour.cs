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
    [Tooltip("데미지 적용 시점 (0~1, 0.5 = 애니메이션 중간)")]
    [Range(0f, 1f)]
    [SerializeField] private float damageTimingPoint = 0.5f;
    
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
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎬 [SkillAction] 시작 - {animator.name}");
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (skillController == null) return;
        
        float normalizedTime = stateInfo.normalizedTime % 1f;
        
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
        
        if (enableDebugLogs)
        {
            Debug.Log($"✅ [SkillAction] 종료 - {animator.name}");
        }
        
        skillController.OnSkillActionComplete();
    }
}

