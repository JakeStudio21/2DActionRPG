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
    
    // 🎨 Phase 1: 이펙트 관리
    private GameObject applyEffectPrefab;    // 타격 이펙트 (매번 재생)
    private GameObject persistentEffectPrefab;  // 지속 이펙트 (1회만 생성)
    private GameObject _currentVisual;  // 현재 지속 이펙트 인스턴스
    private Vector3 effectOffset;  // 이펙트 스폰 위치 오프셋
    
    // 🔧 디버그 설정
    private bool enableDebugLogs = false;  // ⭐ Production: false
    
    #endregion
    
    #region 생성자
    
    /// <summary>
    /// 속박 효과 생성자
    /// </summary>
    /// <param name="target">적용 대상 (플레이어 또는 몬스터)</param>
    /// <param name="duration">지속 시간 (초)</param>
    /// <param name="applyEffect">타격 시 재생할 이펙트 (매번)</param>
    /// <param name="persistentEffect">지속 중 표시할 이펙트 (1회만)</param>
    /// <param name="offset">이펙트 스폰 위치 오프셋 (타겟 기준)</param>
    public BindEffect(GameObject target, float duration, GameObject applyEffect = null, GameObject persistentEffect = null, Vector3 offset = default)
        : base(EStatusEffectType.Bind, target, duration, 0f) // value는 사용 안 함
    {
        this.applyEffectPrefab = applyEffect;
        this.persistentEffectPrefab = persistentEffect;
        this.effectOffset = offset == default ? new Vector3(0f, 0.5f, 0f) : offset;
        
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
    /// 🎨 Phase 1: Apply Effect (타격 피드백) 매번 재생
    /// 🎨 Phase 1: Persistent Effect (사슬) 1회만 생성
    /// </summary>
    public override void Apply()
    {
        if (enableDebugLogs)
            Debug.Log($"🔗 [BindEffect] ========== Apply() 시작 ==========");
        
        if (target == null)
        {
            Debug.LogWarning("[BindEffect] 대상이 null입니다!");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"🔗 [BindEffect] Apply() - 타격 이펙트 재생 호출");
        
        // 🎨 타격 이펙트 재생 (매번)
        PlayApplyEffect();
        
        if (enableDebugLogs)
            Debug.Log($"🔗 [BindEffect] Apply() - 지속 이펙트 생성 호출");
        
        // 🎨 지속 이펙트 생성 (1회만)
        SpawnPersistentEffect();
        
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
    /// 🎨 Phase 1: Persistent Effect 제거
    /// </summary>
    public override void Remove()
    {
        if (enableDebugLogs)
            Debug.Log($"✅ [BindEffect] ========== Remove() 시작 ==========");
        
        if (target == null || !wasMovementDisabled)
        {
            if (enableDebugLogs)
                Debug.Log($"✅ [BindEffect] Remove() 스킵 (target null 또는 이동 제한 없음)");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"✅ [BindEffect] Remove() - 지속 이펙트 제거 호출");
        
        // 🎨 지속 이펙트 제거
        DestroyPersistentEffect();
        
        // 🧑 플레이어 복구
        if (playerStats != null)
        {
            // 임시 이동속도 제거 (원래대로 복구)
            playerStats.RemoveTemporaryMoveSpeed(-originalMoveSpeed);
            playerStats.RecalculateAllStats();
            
            // 🔥 [최우선 수정] PlayerController에 즉시 강제 적용 (1~2초 지연 제거!)
            if (playerController != null)
            {
                // RecalculateAllStats()가 다음 프레임에 적용될 수 있으므로 강제 즉시 갱신
                playerController.SetMoveSpeed(playerStats.FinalMoveSpeed);
                
                if (enableDebugLogs)
                    Debug.Log($"🔥 [BindEffect] PlayerController.moveSpeed 즉시 갱신: {playerStats.FinalMoveSpeed:F1}");
            }
            
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
    
    /// <summary>
    /// 🔧 Bug Fix: 중첩 시 타격 이펙트 재생
    /// 속박 중 다시 공격받았을 때 타격 이펙트가 나오도록 함
    /// </summary>
    public override void RefreshOrStack(float newDuration, float newValue)
    {
        if (enableDebugLogs)
            Debug.Log($"🔄 [BindEffect] ========== RefreshOrStack() 시작 ==========");
        
        if (enableDebugLogs)
            Debug.Log($"🔄 [BindEffect] RefreshOrStack() - base.RefreshOrStack() 호출");
        
        // 부모 클래스의 기본 동작 (지속시간 갱신)
        base.RefreshOrStack(newDuration, newValue);
        
        if (enableDebugLogs)
            Debug.Log($"🔄 [BindEffect] RefreshOrStack() - 타격 이펙트 재생 호출");
        
        // 🎨 타격 이펙트는 매번 재생
        PlayApplyEffect();
        
        // 🎨 지속 이펙트 재생성 체크 (혹시 사라졌을 경우 대비)
        if (enableDebugLogs)
            Debug.Log($"🔄 [BindEffect] RefreshOrStack() - 지속 이펙트 재생성 체크");
        
        SpawnPersistentEffect(); // ← _currentVisual이 null이면 재생성, 있으면 스킵
        
        if (enableDebugLogs)
            Debug.Log($"🔄 [BindEffect] RefreshOrStack() 완료 (타격 이펙트 재생됨, 남은시간: {remainingDuration:F1}초)");
    }
    
    #endregion
    
    #region 이펙트 재생
    
    /// <summary>
    /// 🎨 타격 이펙트 재생 (매번)
    /// </summary>
    private void PlayApplyEffect()
    {
        if (enableDebugLogs)
            Debug.Log($"🎨 [BindEffect] PlayApplyEffect() 호출됨");
        
        if (applyEffectPrefab == null)
        {
            if (enableDebugLogs)
                Debug.Log($"🎨 [BindEffect] applyEffectPrefab이 null - 이펙트 없음");
            return;
        }
        
        if (target == null)
        {
            if (enableDebugLogs)
                Debug.Log($"🎨 [BindEffect] target이 null - 이펙트 재생 불가");
            return;
        }
        
        Vector3 spawnPosition = target.transform.position + effectOffset;
        
        if (GamePoolManager.Instance != null)
        {
            // 풀링 시스템 사용
            GameObject effectObj = GamePoolManager.Instance.SpawnFromPool(
                applyEffectPrefab.name,
                spawnPosition,
                Quaternion.identity);
            
            if (enableDebugLogs)
                Debug.Log($"🎨 [BindEffect] ✅ 타격 이펙트 재생 완료 (풀링): {applyEffectPrefab.name}");
        }
        else
        {
            // Fallback: Instantiate
            GameObject effectObj = Object.Instantiate(applyEffectPrefab, spawnPosition, Quaternion.identity);
            Object.Destroy(effectObj, 2f);
            
            if (enableDebugLogs)
                Debug.Log($"🎨 [BindEffect] ✅ 타격 이펙트 재생 완료 (Instantiate): {applyEffectPrefab.name}");
        }
    }
    
    /// <summary>
    /// 🎨 지속 이펙트 생성 (1회만)
    /// 🔧 Bug Fix: Unity Implicit Bool Conversion으로 파괴된 오브젝트 감지
    /// </summary>
    private void SpawnPersistentEffect()
    {
        if (enableDebugLogs)
            Debug.Log($"🎨 [BindEffect] SpawnPersistentEffect() 호출됨");
        
        if (persistentEffectPrefab == null)
        {
            if (enableDebugLogs)
                Debug.Log($"🎨 [BindEffect] persistentEffectPrefab이 null - 지속 이펙트 없음");
            return;
        }
        
        if (target == null)
        {
            if (enableDebugLogs)
                Debug.Log($"🎨 [BindEffect] target이 null - 지속 이펙트 생성 불가");
            return;
        }
        
        // 🔧 Bug Fix: GameObject는 살아있지만 Particle이 정지된 경우 처리
        bool needsRespawn = false;
        
        if (_currentVisual != null)
        {
            // GameObject가 존재하면 Particle 상태 체크
            ParticleSystem ps = _currentVisual.GetComponent<ParticleSystem>();
            
            if (ps != null)
            {
                bool isPlaying = ps.isPlaying;
                bool isAlive = ps.IsAlive(true); // 모든 서브 파티클 포함
                
                if (enableDebugLogs)
                {
                    Debug.Log($"🔍 [BindEffect] Particle 상태 체크:");
                    Debug.Log($"  - GameObject: {_currentVisual.name} (InstanceID: {_currentVisual.GetInstanceID()})");
                    Debug.Log($"  - 활성화: {_currentVisual.activeSelf}");
                    Debug.Log($"  - isPlaying: {isPlaying}");
                    Debug.Log($"  - IsAlive: {isAlive}");
                }
                
                // Particle이 정지되었으면 GameObject 교체 필요
                if (!isPlaying || !isAlive)
                {
                    needsRespawn = true;
                    
                    if (enableDebugLogs)
                        Debug.Log($"🔄 [BindEffect] Particle 정지됨 → GameObject 파괴 후 재생성");
                    
                    // 🔧 GamePool 통합: 기존 GameObject 풀 반환
                    if (GamePoolManager.Instance != null && persistentEffectPrefab != null)
                    {
                        GamePoolManager.Instance.ReturnToPool(persistentEffectPrefab.name, _currentVisual);
                        
                        if (enableDebugLogs)
                            Debug.Log($"🔄 [BindEffect] 기존 이펙트 풀 반환: {persistentEffectPrefab.name}");
                    }
                    else
                    {
                        // Fallback: Destroy
                        Object.Destroy(_currentVisual);
                        
                        if (enableDebugLogs)
                            Debug.Log($"🔄 [BindEffect] 기존 이펙트 Destroy");
                    }
                    
                    _currentVisual = null;
                }
            }
        }
        else
        {
            needsRespawn = true; // null이면 당연히 생성 필요
        }
        
        if (needsRespawn)
        {
            Vector3 spawnPosition = target.transform.position + effectOffset;
            
            // 🔧 GamePool 통합: Instantiate → GetFromPool
            if (GamePoolManager.Instance != null)
            {
                _currentVisual = GamePoolManager.Instance.SpawnFromPool(
                    persistentEffectPrefab.name,
                    spawnPosition,
                    Quaternion.identity);
                
                if (enableDebugLogs)
                    Debug.Log($"🎨 [BindEffect] ✅ 지속 이펙트 생성 완료 (풀링): {persistentEffectPrefab.name}");
            }
            else
            {
                // Fallback: Instantiate
                _currentVisual = Object.Instantiate(persistentEffectPrefab, spawnPosition, Quaternion.identity);
                
                if (enableDebugLogs)
                    Debug.Log($"🎨 [BindEffect] ✅ 지속 이펙트 생성 완료 (Instantiate): {persistentEffectPrefab.name}");
            }
            
            // 대상 따라다니기
            if (_currentVisual != null)
            {
                _currentVisual.transform.SetParent(target.transform);
                _currentVisual.transform.localPosition = effectOffset;
            }
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"🎨 [BindEffect] ✅ 지속 이펙트 정상 작동 중 (중복 생성 불필요)");
        }
    }
    
    /// <summary>
    /// 🎨 지속 이펙트 제거
    /// 🔧 GamePool 통합: Destroy → ReturnToPool
    /// </summary>
    private void DestroyPersistentEffect()
    {
        // 🔧 Unity Implicit Bool Conversion: 진짜 null + 파괴된 오브젝트 모두 감지
        if (_currentVisual)
        {
            if (enableDebugLogs)
                Debug.Log($"🎨 [BindEffect] 지속 이펙트 제거 시작");
            
            // 🔧 GamePool 통합: Destroy → ReturnToPool
            if (GamePoolManager.Instance != null && persistentEffectPrefab != null)
            {
                GamePoolManager.Instance.ReturnToPool(persistentEffectPrefab.name, _currentVisual);
                
                if (enableDebugLogs)
                    Debug.Log($"🎨 [BindEffect] 지속 이펙트 풀 반환 완료: {persistentEffectPrefab.name}");
            }
            else
            {
                // Fallback: Destroy
                Object.Destroy(_currentVisual);
                
                if (enableDebugLogs)
                    Debug.Log($"🎨 [BindEffect] 지속 이펙트 Destroy 완료");
            }
            
            // [핵심] 파괴 직후 반드시 null 대입하여 다음 Apply()에서 재생성 가능하게 함
            _currentVisual = null;
            
            if (enableDebugLogs)
                Debug.Log($"🎨 [BindEffect] 지속 이펙트 제거 완료 (_currentVisual = null)");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"🎨 [BindEffect] 지속 이펙트 이미 없음 (제거 스킵)");
        }
    }
    
    #endregion
}

