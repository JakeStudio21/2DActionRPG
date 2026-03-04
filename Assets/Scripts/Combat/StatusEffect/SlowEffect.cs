using UnityEngine;

/// <summary>
/// 둔화 효과 - 대상의 이동속도를 일정 비율 감소
/// ⚙️ Phase 4-C: 플레이어/몬스터 모두 지원
/// </summary>
public class SlowEffect : BaseStatusEffect
{
    #region 필드
    
    // 대상 컴포넌트 캐싱 (⚡ GC 최적화)
    private PlayerRuntimeStats playerStats;
    private BaseEnemy baseEnemy;
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    
    // 원래 이동속도 저장 (복구용)
    private float originalMoveSpeed;
    private float reducedAmount; // 실제로 감소된 속도값
    private bool wasSlowApplied;
    
    // 🎨 이펙트 관리
    private GameObject persistentEffectPrefab;  // 지속 이펙트 (둔화 표시)
    private GameObject _currentVisual;  // 현재 지속 이펙트 인스턴스
    private Vector3 effectOffset;  // 이펙트 스폰 위치 오프셋
    
    #endregion
    
    #region 생성자
    
    /// <summary>
    /// 둔화 효과 생성자
    /// </summary>
    /// <param name="target">적용 대상 (플레이어 또는 몬스터)</param>
    /// <param name="duration">지속 시간 (초)</param>
    /// <param name="slowAmount">감소 비율 (0.3 = 30% 감소)</param>
    /// <param name="persistentEffect">지속 중 표시할 이펙트</param>
    /// <param name="offset">이펙트 스폰 위치 오프셋 (타겟 기준)</param>
    public SlowEffect(GameObject target, float duration, float slowAmount, GameObject persistentEffect = null, Vector3 offset = default)
        : base(EStatusEffectType.Slow, target, duration, slowAmount)
    {
        this.persistentEffectPrefab = persistentEffect;
        this.effectOffset = offset == default ? new Vector3(0f, 0.5f, 0f) : offset;
        
        // 대상 컴포넌트 캐싱
        if (target != null)
        {
            // 플레이어 컴포넌트
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
    /// 둔화 적용 - 이동속도 감소
    /// </summary>
    public override void Apply()
    {
        if (target == null)
        {
            Debug.LogWarning("[SlowEffect] 대상이 null입니다!");
            return;
        }
        
        // value는 감소 비율 (0.3 = 30% 감소)
        float slowPercent = Mathf.Clamp01(value);
        
        // 🎨 지속 이펙트 생성
        SpawnPersistentEffect();
        
        // 🧑 플레이어 처리
        if (playerStats != null)
        {
            originalMoveSpeed = playerStats.FinalMoveSpeed;
            reducedAmount = originalMoveSpeed * slowPercent;
            
            // 이동속도 감소
            playerStats.AddTemporaryMoveSpeed(-reducedAmount);
            playerStats.RecalculateAllStats();
            
            wasSlowApplied = true;
            
            if (enableDebugLogs)
                Debug.Log($"🐌 [SlowEffect] 플레이어 둔화 적용 ({slowPercent * 100:F0}% 감소) - {remainingDuration:F1}초");
        }
        // 👾 몬스터 처리
        else if (baseEnemy != null)
        {
            if (navMeshAgent != null)
            {
                originalMoveSpeed = navMeshAgent.speed;
                reducedAmount = originalMoveSpeed * slowPercent;
                
                // NavMeshAgent 속도 감소
                navMeshAgent.speed = originalMoveSpeed - reducedAmount;
                
                wasSlowApplied = true;
                
                if (enableDebugLogs)
                    Debug.Log($"🐌 [SlowEffect] 몬스터 {target.name} 둔화 적용 ({slowPercent * 100:F0}% 감소) - {remainingDuration:F1}초");
            }
            else
            {
                // NavMesh를 사용하지 않는 몬스터
                // TODO: FSM 기반 이동속도 제어 로직 추가
                if (enableDebugLogs)
                    Debug.LogWarning($"[SlowEffect] {target.name}은 NavMesh를 사용하지 않습니다. FSM 기반 이동 제어 필요.");
            }
        }
        else
        {
            Debug.LogWarning($"[SlowEffect] {target.name}에 적용 가능한 컴포넌트가 없습니다!");
        }
    }
    
    /// <summary>
    /// 둔화 해제 - 이동속도 복구
    /// </summary>
    public override void Remove()
    {
        if (target == null || !wasSlowApplied)
            return;
        
        // 🎨 지속 이펙트 제거
        DestroyPersistentEffect();
        
        // 🧑 플레이어 복구
        if (playerStats != null)
        {
            // 임시 이동속도 제거 (원래대로 복구)
            playerStats.RemoveTemporaryMoveSpeed(-reducedAmount);
            playerStats.RecalculateAllStats();
            
            if (enableDebugLogs)
                Debug.Log($"✅ [SlowEffect] 플레이어 둔화 해제 (속도 복구)");
        }
        // 👾 몬스터 복구
        else if (baseEnemy != null && navMeshAgent != null)
        {
            navMeshAgent.speed = originalMoveSpeed;
            
            if (enableDebugLogs)
                Debug.Log($"✅ [SlowEffect] 몬스터 {target.name} 둔화 해제 (속도 복구)");
        }
        
        wasSlowApplied = false;
    }
    
    /// <summary>
    /// 중첩 처리 - 더 강한 둔화로 덮어쓰기
    /// </summary>
    public override void RefreshOrStack(float newDuration, float newValue)
    {
        // 더 강한 둔화가 들어오면 기존 효과 제거 후 재적용
        if (newValue > value)
        {
            Remove(); // 기존 효과 제거
            
            value = newValue;
            remainingDuration = newDuration;
            
            Apply(); // 새 효과 적용
            
            if (enableDebugLogs)
                Debug.Log($"🔄 [SlowEffect] 둔화 강화: {newValue * 100:F0}% 감소");
        }
        else
        {
            // 더 약한 둔화는 지속시간만 갱신
            base.RefreshOrStack(newDuration, newValue);
        }
    }
    
    #endregion
    
    #region 이펙트 재생
    
    /// <summary>
    /// 🎨 지속 이펙트 생성 (1회만)
    /// </summary>
    private void SpawnPersistentEffect()
    {
        if (persistentEffectPrefab == null || target == null)
            return;
        
        if (_currentVisual)
            return; // 이미 존재하면 스킵
        
        Vector3 spawnPosition = target.transform.position + effectOffset;
        
        if (GamePoolManager.Instance != null)
        {
            _currentVisual = GamePoolManager.Instance.SpawnFromPool(
                persistentEffectPrefab.name,
                spawnPosition,
                Quaternion.identity);
        }
        else
        {
            _currentVisual = Object.Instantiate(persistentEffectPrefab, spawnPosition, Quaternion.identity);
        }
        
        // 대상 따라다니기
        if (_currentVisual != null)
        {
            _currentVisual.transform.SetParent(target.transform);
            _currentVisual.transform.localPosition = effectOffset;
        }
    }
    
    /// <summary>
    /// 🎨 지속 이펙트 제거
    /// </summary>
    private void DestroyPersistentEffect()
    {
        if (_currentVisual)
        {
            if (GamePoolManager.Instance != null && persistentEffectPrefab != null)
            {
                GamePoolManager.Instance.ReturnToPool(persistentEffectPrefab.name, _currentVisual);
            }
            else
            {
                Object.Destroy(_currentVisual);
            }
            
            _currentVisual = null;
        }
    }
    
    #endregion
}

