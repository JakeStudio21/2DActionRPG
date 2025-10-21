using UnityEngine;

/// <summary>
/// 몬스터 BlendTree Attack State에서 Animation Event를 통합 관리하는 StateMachineBehaviour
/// 개별 애니메이션 클립의 Animation Event 대신 State 수준에서 공격 실행
/// ⭐ 8방향 BlendTree에서 Animation Event 중복 호출 문제 해결
/// </summary>
public class EnemyAttackStateBehaviour : StateMachineBehaviour
{
    [Header("🎯 Attack Timing Settings")]
    [Tooltip("공격 이펙트 발동 시점 (0.0 ~ 1.0, 애니메이션 진행도 기준)")]
    [SerializeField] [Range(0f, 1f)] 
    private float attackExecuteTime = 0.4f;  // 40% 지점에서 공격 실행
    
    [Tooltip("공격 완료 시점 (0.0 ~ 1.0, 애니메이션 진행도 기준)")]
    [SerializeField] [Range(0f, 1f)]
    private float attackCompleteTime = 0.8f; // 80% 지점에서 공격 완료
    
    [Header("🔧 Debug Settings")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 상태 플래그
    private bool attackExecuteTriggered = false;
    private bool attackCompleteTriggered = false;
    
    // 컴포넌트 캐시
    private AOEAttack cachedAOEAttack;
    private MeleeAttack cachedMeleeAttack;
    private RangedAttack cachedRangedAttack;
    private MultiShotRangedAttack cachedMultiShotAttack;
    
    /// <summary>
    /// State 진입 시 호출 - 초기화
    /// </summary>
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (showDebugLogs)
        {
            Debug.Log($"🎬 [EnemyAttackStateBehaviour] {animator.gameObject.name} - Attack State 진입 (길이: {stateInfo.length:F3}초)");
        }
        
        // 상태 플래그 초기화
        attackExecuteTriggered = false;
        attackCompleteTriggered = false;
        
        // 컴포넌트 캐시 (한 번만 찾기)
        if (cachedAOEAttack == null && cachedMeleeAttack == null && 
            cachedRangedAttack == null && cachedMultiShotAttack == null)
        {
            CacheAttackComponents(animator);
        }
    }

    /// <summary>
    /// State 업데이트 - 타이밍 체크 및 공격 실행
    /// </summary>
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        float normalizedTime = stateInfo.normalizedTime % 1f; // 루프 고려
        
        // 🎯 공격 실행 시점 체크
        if (!attackExecuteTriggered && normalizedTime >= attackExecuteTime)
        {
            attackExecuteTriggered = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"🎯 [EnemyAttackStateBehaviour] {animator.gameObject.name} - 공격 실행! (진행도: {normalizedTime:F3})");
            }
            
            // 공격 실행
            ExecuteAttack(animator);
        }
        
        // 🏁 공격 완료 시점 체크
        if (!attackCompleteTriggered && normalizedTime >= attackCompleteTime)
        {
            attackCompleteTriggered = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"🏁 [EnemyAttackStateBehaviour] {animator.gameObject.name} - 공격 완료 (진행도: {normalizedTime:F3})");
            }
            
            // 공격 완료 처리 (필요시 추가)
            OnAttackComplete(animator);
        }
    }

    /// <summary>
    /// State 종료 시 호출 - 안전장치
    /// </summary>
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (showDebugLogs)
        {
            Debug.Log($"🚪 [EnemyAttackStateBehaviour] {animator.gameObject.name} - Attack State 종료 (최종 진행도: {stateInfo.normalizedTime:F3})");
        }
        
        // 🔒 안전장치: 공격이 실행되지 않았다면 강제 실행
        if (!attackExecuteTriggered)
        {
            Debug.LogWarning($"⚠️ [EnemyAttackStateBehaviour] {animator.gameObject.name} - State 종료 전 강제 공격 실행!");
            ExecuteAttack(animator);
        }
    }
    
    #region 공격 실행 로직
    
    /// <summary>
    /// 공격 컴포넌트 캐싱
    /// </summary>
    private void CacheAttackComponents(Animator animator)
    {
        GameObject obj = animator.gameObject;
        
        cachedAOEAttack = obj.GetComponent<AOEAttack>();
        cachedMeleeAttack = obj.GetComponent<MeleeAttack>();
        cachedRangedAttack = obj.GetComponent<RangedAttack>();
        cachedMultiShotAttack = obj.GetComponent<MultiShotRangedAttack>();
        
        if (showDebugLogs)
        {
            string foundComponents = "";
            if (cachedAOEAttack != null) foundComponents += "AOEAttack ";
            if (cachedMeleeAttack != null) foundComponents += "MeleeAttack ";
            if (cachedRangedAttack != null) foundComponents += "RangedAttack ";
            if (cachedMultiShotAttack != null) foundComponents += "MultiShotRangedAttack ";
            
            Debug.Log($"🔍 [EnemyAttackStateBehaviour] {obj.name} - 공격 컴포넌트 발견: {foundComponents}");
        }
    }
    
    /// <summary>
    /// 공격 실행 (발견된 공격 컴포넌트 호출)
    /// </summary>
    private void ExecuteAttack(Animator animator)
    {
        bool attackExecuted = false;
        
        // 1순위: AOEAttack (CrystalGolem, WaterGolem 등)
        if (cachedAOEAttack != null)
        {
            cachedAOEAttack.SpawnAOEEffect();
            attackExecuted = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"✅ [EnemyAttackStateBehaviour] {animator.gameObject.name} - AOE 공격 실행!");
            }
        }
        // 2순위: MultiShotRangedAttack (Ghost 등)
        else if (cachedMultiShotAttack != null)
        {
            cachedMultiShotAttack.SpawnProjectileAnimEvent();
            attackExecuted = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"✅ [EnemyAttackStateBehaviour] {animator.gameObject.name} - MultiShot 공격 실행!");
            }
        }
        // 3순위: RangedAttack (Grape 등)
        else if (cachedRangedAttack != null)
        {
            cachedRangedAttack.SpawnProjectileAnimEvent();
            attackExecuted = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"✅ [EnemyAttackStateBehaviour] {animator.gameObject.name} - Ranged 공격 실행!");
            }
        }
        // 4순위: MeleeAttack (BlueSlime 등)
        else if (cachedMeleeAttack != null)
        {
            cachedMeleeAttack.AttackHit();
            attackExecuted = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"✅ [EnemyAttackStateBehaviour] {animator.gameObject.name} - Melee 공격 실행!");
            }
        }
        
        // 공격 컴포넌트를 찾지 못한 경우
        if (!attackExecuted)
        {
            Debug.LogError($"❌ [EnemyAttackStateBehaviour] {animator.gameObject.name} - 공격 컴포넌트를 찾을 수 없습니다!");
            Debug.LogError($"AOEAttack, MeleeAttack, RangedAttack, MultiShotRangedAttack 중 하나가 필요합니다.");
        }
    }
    
    /// <summary>
    /// 공격 완료 처리 (필요시 확장)
    /// </summary>
    private void OnAttackComplete(Animator animator)
    {
        // 필요시 공격 완료 로직 추가 (쿨다운 시작, 상태 리셋 등)
        // 현재는 로그만 출력
    }
    
    #endregion
    
    #region Inspector 헬퍼
    
    /// <summary>
    /// Inspector에서 값 변경 시 검증
    /// </summary>
    private void OnValidate()
    {
        // Execute Time이 Complete Time보다 늦으면 경고
        if (attackExecuteTime > attackCompleteTime)
        {
            Debug.LogWarning($"⚠️ [EnemyAttackStateBehaviour] Attack Execute Time ({attackExecuteTime})이 Complete Time ({attackCompleteTime})보다 늦습니다!");
        }
    }
    
    #endregion
}

