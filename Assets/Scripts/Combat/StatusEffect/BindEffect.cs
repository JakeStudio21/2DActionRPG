using UnityEngine;

/// <summary>
/// 속박 효과 - 대상의 이동을 완전히 차단
/// ⚙️ Phase 4-C: 플레이어/몬스터 모두 지원
/// </summary>
public class BindEffect : BaseStatusEffect
{
    #region 필드
    
    // 대상 컴포넌트 캐싱 (⚡ GC 최적화)
    private PlayerController playerController;
    private PlayerRuntimeStats playerStats;
    private BaseEnemy baseEnemy;
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    
    // 원래 이동속도 저장 (복구용)
    private float originalMoveSpeed;
    private bool wasMovementDisabled;
    
    #endregion
    
    #region 생성자
    
    /// <summary>
    /// 속박 효과 생성자
    /// </summary>
    /// <param name="target">적용 대상 (플레이어 또는 몬스터)</param>
    /// <param name="duration">지속 시간 (초)</param>
    public BindEffect(GameObject target, float duration)
        : base(EStatusEffectType.Bind, target, duration, 0f) // value는 사용 안 함
    {
        // 대상 컴포넌트 캐싱
        if (target != null)
        {
            // 플레이어 컴포넌트
            playerController = target.GetComponent<PlayerController>();
            playerStats = target.GetComponent<PlayerRuntimeStats>();
            
            // 몬스터 컴포넌트
            baseEnemy = target.GetComponent<BaseEnemy>();
            if (baseEnemy != null && baseEnemy.IsUsingNavMesh)
            {
                navMeshAgent = baseEnemy.Agent;
            }
        }
    }
    
    #endregion
    
    #region 오버라이드 메서드
    
    /// <summary>
    /// 속박 적용 - 이동속도를 0으로 만듦
    /// </summary>
    public override void Apply()
    {
        if (target == null)
        {
            Debug.LogWarning("[BindEffect] 대상이 null입니다!");
            return;
        }
        
        // 🧑 플레이어 처리
        if (playerStats != null)
        {
            originalMoveSpeed = playerStats.FinalMoveSpeed;
            
            // 이동속도를 현재 값의 음수만큼 차감 (결과적으로 0)
            playerStats.AddTemporaryMoveSpeed(-originalMoveSpeed);
            playerStats.RecalculateAllStats();
            
            wasMovementDisabled = true;
            
            if (enableDebugLogs)
                Debug.Log($"🔗 [BindEffect] 플레이어 속박 적용 (이동 불가) - {remainingDuration:F1}초");
        }
        // 👾 몬스터 처리
        else if (baseEnemy != null)
        {
            if (navMeshAgent != null)
            {
                originalMoveSpeed = navMeshAgent.speed;
                navMeshAgent.speed = 0f;
                navMeshAgent.isStopped = true; // 추가 안전장치
                
                wasMovementDisabled = true;
                
                if (enableDebugLogs)
                    Debug.Log($"🔗 [BindEffect] 몬스터 {target.name} 속박 적용 (NavMesh 정지) - {remainingDuration:F1}초");
            }
            else
            {
                // NavMesh를 사용하지 않는 몬스터 (FSM 기반)
                // TODO: FSM 기반 이동 제어 로직 추가
                if (enableDebugLogs)
                    Debug.LogWarning($"[BindEffect] {target.name}은 NavMesh를 사용하지 않습니다. FSM 기반 이동 제어 필요.");
            }
        }
        else
        {
            Debug.LogWarning($"[BindEffect] {target.name}에 적용 가능한 컴포넌트가 없습니다!");
        }
    }
    
    /// <summary>
    /// 속박 해제 - 이동속도 복구
    /// </summary>
    public override void Remove()
    {
        if (target == null || !wasMovementDisabled)
            return;
        
        // 🧑 플레이어 복구
        if (playerStats != null)
        {
            // 임시 이동속도 제거 (원래대로 복구)
            playerStats.RemoveTemporaryMoveSpeed(-originalMoveSpeed);
            playerStats.RecalculateAllStats();
            
            if (enableDebugLogs)
                Debug.Log($"✅ [BindEffect] 플레이어 속박 해제 (이동 가능)");
        }
        // 👾 몬스터 복구
        else if (baseEnemy != null && navMeshAgent != null)
        {
            navMeshAgent.speed = originalMoveSpeed;
            navMeshAgent.isStopped = false;
            
            if (enableDebugLogs)
                Debug.Log($"✅ [BindEffect] 몬스터 {target.name} 속박 해제 (NavMesh 재개)");
        }
        
        wasMovementDisabled = false;
    }
    
    #endregion
}

