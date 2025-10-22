using UnityEngine;

/// <summary>
/// 몬스터 BlendTree Die State에서 사망 처리를 통합 관리하는 StateMachineBehaviour
/// 개별 애니메이션 클립의 Animation Event 대신 State 수준에서 사망 처리
/// ⭐ 8방향 BlendTree에서 Animation Event 중복 호출 문제 해결
/// </summary>
public class EnemyDieStateBehaviour : StateMachineBehaviour
{
    [Header("💀 Death Timing Settings")]
    [Tooltip("사망 처리 실행 시점 (0.0 ~ 1.0, 애니메이션 진행도 기준)")]
    [SerializeField] [Range(0f, 1f)] 
    private float deathCompleteTime = 0.85f;  // 85% 지점에서 사망 처리 완료
    
    [Header("🔧 Debug Settings")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 상태 플래그
    private bool deathCompleteTriggered = false;
    
    // 컴포넌트 캐시
    private EnemyHealth cachedEnemyHealth;
    
    /// <summary>
    /// State 진입 시 호출 - 초기화
    /// </summary>
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (showDebugLogs)
        {
            Debug.Log($"💀 [EnemyDieStateBehaviour] {animator.gameObject.name} - Die State 진입 (길이: {stateInfo.length:F3}초)");
        }
        
        // 상태 플래그 초기화
        deathCompleteTriggered = false;
        
        // EnemyHealth 컴포넌트 캐시 (한 번만 찾기)
        if (cachedEnemyHealth == null)
        {
            cachedEnemyHealth = animator.GetComponent<EnemyHealth>();
            
            if (cachedEnemyHealth == null)
            {
                Debug.LogError($"❌ [EnemyDieStateBehaviour] {animator.gameObject.name} - EnemyHealth 컴포넌트를 찾을 수 없습니다!");
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.Log($"🔍 [EnemyDieStateBehaviour] {animator.gameObject.name} - EnemyHealth 컴포넌트 발견");
                }
            }
        }
    }

    /// <summary>
    /// State 업데이트 - 타이밍 체크 및 사망 처리 실행
    /// </summary>
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float normalizedTime = stateInfo.normalizedTime % 1f; // 루프 고려
        
        // 💀 사망 처리 완료 시점 체크
        if (!deathCompleteTriggered && normalizedTime >= deathCompleteTime)
        {
            deathCompleteTriggered = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"💀 [EnemyDieStateBehaviour] {animator.gameObject.name} - 사망 처리 실행! (진행도: {normalizedTime:F3})");
            }
            
            // 사망 처리 실행
            ExecuteDeathComplete(animator);
        }
    }

    /// <summary>
    /// State 종료 시 호출 - 안전장치
    /// </summary>
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (showDebugLogs)
        {
            Debug.Log($"🚪 [EnemyDieStateBehaviour] {animator.gameObject.name} - Die State 종료 (최종 진행도: {stateInfo.normalizedTime:F3})");
        }
        
        // 🔒 안전장치: 사망 처리가 실행되지 않았다면 강제 실행
        if (!deathCompleteTriggered)
        {
            Debug.LogWarning($"⚠️ [EnemyDieStateBehaviour] {animator.gameObject.name} - State 종료 전 강제 사망 처리!");
            ExecuteDeathComplete(animator);
        }
    }
    
    #region 사망 처리 로직
    
    /// <summary>
    /// 사망 처리 실행 (EnemyHealth.OnDeathAnimationComplete 호출)
    /// </summary>
    private void ExecuteDeathComplete(Animator animator)
    {
        if (cachedEnemyHealth == null)
        {
            Debug.LogError($"❌ [EnemyDieStateBehaviour] {animator.gameObject.name} - EnemyHealth가 없어서 사망 처리 불가!");
            return;
        }
        
        // EnemyHealth의 OnDeathAnimationComplete() 메서드 호출
        cachedEnemyHealth.OnDeathAnimationComplete();
        
        if (showDebugLogs)
        {
            Debug.Log($"✅ [EnemyDieStateBehaviour] {animator.gameObject.name} - OnDeathAnimationComplete() 호출 완료!");
        }
    }
    
    #endregion
    
    #region Inspector 헬퍼
    
    /// <summary>
    /// Inspector에서 값 변경 시 검증
    /// </summary>
    private void OnValidate()
    {
        // Death Complete Time이 너무 빠르면 경고
        if (deathCompleteTime < 0.5f)
        {
            Debug.LogWarning($"⚠️ [EnemyDieStateBehaviour] Death Complete Time ({deathCompleteTime})이 너무 빠릅니다! (최소 0.5 권장)");
        }
    }
    
    #endregion
}

