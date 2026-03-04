using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🆕 상태이상 매니저 - Update 기반 통합 시스템
/// ⚙️ Phase 4-C: 플레이어/몬스터 모두 지원, 면역 시스템 통합
/// ⚙️ Singleton 패턴 유지 (BaseAttackBehaviour 호환성)
/// ⚡ GC 최적화: List 사전 할당, Update 내 new 제거
/// </summary>
public class StatusEffectManager : Singleton<StatusEffectManager>
{
    #region 필드
    
    [Header("=== 디버그 설정 ===")]
    [SerializeField] private bool enableDebugLogs = false;  // ⭐ Production: false
    
    /// <summary>
    /// 활성화된 상태이상 목록
    /// ⚡ GC 최소화: 사전 할당된 리스트
    /// </summary>
    private List<IStatusEffect> activeEffects = new List<IStatusEffect>(16);
    
    /// <summary>
    /// 제거 대기 목록 (⚡ GC 최소화: 매 프레임 new 방지)
    /// </summary>
    private List<IStatusEffect> effectsToRemove = new List<IStatusEffect>(16);
    
    // 플레이어 참조 (캐싱)
    private GameObject playerObject;
    
    #endregion
    
    #region Unity 생명주기
    
    protected override void Awake()
    {
        base.Awake(); // Singleton: DontDestroyOnLoad 자동 처리
    }
    
    private void OnEnable()
    {
        // 씬 로드 이벤트 구독
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        // 씬 로드 이벤트 구독 해제
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void Start()
    {
        // 🔄 시작 시 플레이어 찾기 (코루틴 방식)
        StartCoroutine(FindPlayerCoroutine());
    }
    
    /// <summary>
    /// 🔄 씬 전환 시 플레이어 다시 찾기
    /// </summary>
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (enableDebugLogs)
            Debug.Log($"[StatusEffectManager] 씬 로드됨: {scene.name}, 플레이어 재탐색 시작...");
        
        // 🔄 동적 플레이어 생성 대응: 반복 재시도 방식
        StartCoroutine(FindPlayerCoroutine());
    }
    
    /// <summary>
    /// 🔄 플레이어 찾기 코루틴 (기존 시스템 패턴)
    /// </summary>
    private System.Collections.IEnumerator FindPlayerCoroutine()
    {
        float timeout = 10f;  // 10초 타임아웃 (여유있게)
        float elapsed = 0f;
        float retryInterval = 0.5f;  // 0.5초마다 재시도 (기존보다 여유있게)

        while (playerObject == null && elapsed < timeout)
        {
            var playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
            {
                playerObject = playerHealth.gameObject;
                if (enableDebugLogs)
                    Debug.Log($"✅ [StatusEffectManager] 플레이어 오브젝트 찾음: {playerObject.name} ({elapsed:F1}초 경과)");
                yield break;  // 찾았으면 즉시 종료
            }

            elapsed += retryInterval;
            yield return new UnityEngine.WaitForSeconds(retryInterval);
            
            if (enableDebugLogs && elapsed % 2f < retryInterval)
                Debug.Log($"[StatusEffectManager] 플레이어 검색 중... ({elapsed:F1}초)");
        }

        if (playerObject == null)
        {
            // 플레이어가 없을 수 있음 (Lobby, Loading 씬 등)
            if (enableDebugLogs)
                Debug.Log($"[StatusEffectManager] 플레이어를 찾을 수 없습니다 ({timeout}초 타임아웃, Lobby 등 플레이어가 없는 씬일 수 있음)");
        }
    }
    
    /// <summary>
    /// 플레이어 찾기 (즉시 1회 시도 - ApplyStatusEffect용)
    /// </summary>
    private void FindPlayer()
    {
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerObject = playerHealth.gameObject;
            if (enableDebugLogs)
                Debug.Log($"✅ [StatusEffectManager] 플레이어 오브젝트 찾음: {playerObject.name}");
        }
        else
        {
            // 플레이어가 없을 수 있음 (Lobby, Loading 씬 등)
            playerObject = null;
            
            if (enableDebugLogs)
                Debug.Log($"[StatusEffectManager] PlayerHealth를 찾을 수 없습니다 (코루틴 재탐색 진행 중일 수 있음)");
        }
    }
    
    /// <summary>
    /// 🔄 플레이어 강제 재탐색 (외부 호출용)
    /// </summary>
    public void ForceRefindPlayer()
    {
        StartCoroutine(FindPlayerCoroutine());
    }
    
    private void Update()
    {
        // ⚡ GC 최소화: foreach 사용 (List는 struct enumerator 사용)
        foreach (var effect in activeEffects)
        {
            // 틱 처리 (지속시간 감소 + 지속 피해 등)
            bool isExpired = effect.Tick(Time.deltaTime);
            
            if (isExpired)
            {
                effectsToRemove.Add(effect);
            }
        }
        
        // 만료된 효과 제거
        if (effectsToRemove.Count > 0)
        {
            foreach (var effect in effectsToRemove)
            {
                RemoveEffect(effect);
            }
            
            effectsToRemove.Clear(); // ⚡ Clear()는 capacity 유지
        }
    }
    
    protected override void OnDestroy()
    {
        // 부모 클래스 OnDestroy 호출 (Singleton 정리)
        base.OnDestroy();
        
        // 모든 상태이상 제거 (정리)
        ClearAllEffects();
    }
    
    #endregion
    
    #region 상태이상 추가 (신규 인터페이스)
    
    /// <summary>
    /// ⚙️ 상태이상 추가 (IStatusEffect 직접 추가)
    /// 🛡️ Phase 1: [면역 체크 → 저항 체크 → 적용] 순서로 처리
    /// </summary>
    public void AddEffect(IStatusEffect newEffect)
    {
        if (newEffect == null)
        {
            Debug.LogWarning("[StatusEffectManager] null 상태이상을 추가하려고 시도");
            return;
        }
        
        // 1️⃣ 🛡️ Phase 4-C: 기존 면역 체크 (버프/패시브 기반 면역)
        if (IsImmuneToEffect(newEffect.Target, newEffect.EffectType))
        {
            if (enableDebugLogs)
                Debug.Log($"🛡️ [StatusEffectManager] {newEffect.Target.name}이(가) " +
                          $"{newEffect.EffectType}에 면역! (버프/패시브 면역) 차단됨.");
            return; // 면역이 있으면 상태이상 적용 차단
        }
        
        // 2️⃣ 🛡️ Phase 1: 저항 체크 및 적용 (스탯 기반 저항)
        bool isFullyResisted = ApplyResistanceToEffect(newEffect);
        if (isFullyResisted)
        {
            if (enableDebugLogs)
                Debug.Log($"🛡️ [StatusEffectManager] {newEffect.Target.name}이(가) " +
                          $"{newEffect.EffectType}를 완전 저항! (100% 저항 or 지속시간 너무 짧음)");
            return; // 완전 저항 시 상태이상 적용 안 됨
        }
        
        // 3️⃣ 동일 타입 효과가 이미 있는지 확인
        IStatusEffect existingEffect = FindEffectByType(newEffect.EffectType);
        
        if (existingEffect != null)
        {
            // 이미 존재하면 갱신/중첩
            if (enableDebugLogs)
                Debug.Log($"🔄 [StatusEffectManager] {newEffect.EffectType} 중첩 감지! RefreshOrStack() 호출 → {newEffect.Target.name}");
            
            existingEffect.RefreshOrStack(newEffect.RemainingDuration, newEffect.Value);
            
            if (enableDebugLogs)
                Debug.Log($"🔄 [StatusEffectManager] {newEffect.EffectType} RefreshOrStack() 완료 → {newEffect.Target.name}");
        }
        else
        {
            // 새로 추가
            if (enableDebugLogs)
                Debug.Log($"✅ [StatusEffectManager] {newEffect.EffectType} 신규 추가! Apply() 호출 → {newEffect.Target.name}");
            
            activeEffects.Add(newEffect);
            newEffect.Apply();
            
            if (enableDebugLogs)
                Debug.Log($"✅ [StatusEffectManager] {newEffect.EffectType} Apply() 완료 → {newEffect.Target.name} (지속: {newEffect.RemainingDuration:F1}초)");
        }
    }
    
    /// <summary>
    /// 🛡️ 대상이 특정 상태이상에 면역인지 확인 (Phase 4-C: 버프/패시브 기반)
    /// </summary>
    private bool IsImmuneToEffect(GameObject target, EStatusEffectType effectType)
    {
        if (target == null)
            return false;
        
        // 플레이어 면역 체크
        var playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            return playerHealth.IsImmuneToEffect(effectType);
        }
        
        // 몬스터 면역 체크
        var enemyHealth = target.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            return enemyHealth.IsImmuneToEffect(effectType);
        }
        
        return false;
    }
    
    /// <summary>
    /// 🛡️ Phase 1: 저항력을 효과에 적용 (스탯 기반)
    /// </summary>
    /// <param name="effect">적용할 상태이상 효과</param>
    /// <returns>true: 완전 저항 (효과 무효화), false: 부분 저항 (효과 적용)</returns>
    private bool ApplyResistanceToEffect(IStatusEffect effect)
    {
        if (effect == null || effect.Target == null)
            return false;
        
        // PlayerResistanceStats 컴포넌트 찾기 (플레이어 전용)
        var resistanceStats = effect.Target.GetComponent<PlayerResistanceStats>();
        if (resistanceStats == null)
        {
            // 플레이어가 아니거나 컴포넌트 없음 → 저항 없음 (몬스터는 저항 없음)
            return false;
        }
        
        // 저항 수치 가져오기
        float resistance = resistanceStats.GetResistance(effect.EffectType);
        
        // 저항 적용
        bool isFullyResisted = effect.ApplyResistance(resistance);
        
        if (enableDebugLogs && resistance > 0f)
        {
            if (isFullyResisted)
            {
                Debug.Log($"🛡️ [StatusEffectManager] {effect.Target.name} → {effect.EffectType} 완전 저항! " +
                          $"(저항 {resistance * 100:F0}%)");
            }
            else
            {
                Debug.Log($"🛡️ [StatusEffectManager] {effect.Target.name} → {effect.EffectType} 부분 저항 적용 " +
                          $"(저항 {resistance * 100:F0}%, 지속시간 감소)");
            }
        }
        
        return isFullyResisted;
    }
    
    /// <summary>
    /// ⚙️ 간편 메서드: 중독 효과 추가
    /// </summary>
    public void AddPoison(GameObject target, float duration, float damagePerTick, float tickInterval = 1.0f, GameObject tickEffect = null, Vector3 offset = default)
    {
        var effect = new PoisonEffect(target, duration, damagePerTick, tickInterval, tickEffect, offset);
        AddEffect(effect);
    }
    
    /// <summary>
    /// ⚙️ 간편 메서드: 화상 효과 추가
    /// </summary>
    public void AddBurn(GameObject target, float duration, float damagePerTick, float tickInterval = 1.0f, GameObject tickEffect = null, Vector3 offset = default)
    {
        var effect = new BurnEffect(target, duration, damagePerTick, tickInterval, tickEffect, offset);
        AddEffect(effect);
    }
    
    /// <summary>
    /// ⚙️ 간편 메서드: 속박 효과 추가
    /// </summary>
    public void AddBind(GameObject target, float duration, GameObject applyEffect = null, GameObject persistentEffect = null, Vector3 offset = default)
    {
        var effect = new BindEffect(target, duration, applyEffect, persistentEffect, offset);
        AddEffect(effect);
    }
    
    /// <summary>
    /// ⚙️ 간편 메서드: 둔화 효과 추가
    /// </summary>
    public void AddSlow(GameObject target, float duration, float slowAmount, GameObject persistentEffect = null, Vector3 offset = default)
    {
        var effect = new SlowEffect(target, duration, slowAmount, persistentEffect, offset);
        AddEffect(effect);
    }
    
    #endregion
    
    #region 상태이상 추가 (기존 StatusEffectData 호환)
    
    /// <summary>
    /// ⚙️ 레거시 지원: StatusEffectData → IStatusEffect 변환
    /// BaseAttackBehaviour와의 호환성을 위해 유지
    /// </summary>
    public void ApplyStatusEffect(StatusEffectData effectData)
    {
        if (effectData == null)
        {
            Debug.LogWarning("[StatusEffectManager] null StatusEffectData를 적용하려고 시도");
            return;
        }
        
        // 🔄 플레이어가 없으면 다시 찾기 (동적 생성 대응)
        if (playerObject == null)
        {
            FindPlayer();
            
            if (playerObject == null)
            {
                Debug.LogWarning("[StatusEffectManager] 플레이어를 찾을 수 없어서 상태이상 적용 불가");
                return;
            }
        }
        
        // StatusEffectData를 IStatusEffect로 변환
        IStatusEffect effect = ConvertFromStatusEffectData(effectData, playerObject);
        
        if (effect != null)
        {
            // 🎨 Phase 1: 이펙트 재생
            // BindEffect는 자체적으로 이펙트 관리 (중복 방지)
            if (effectData.EffectType != StatusEffectType.Bind)
            {
                PlayStatusEffectVisuals(effectData, playerObject.transform);
            }
            
            AddEffect(effect);
        }
    }
    
    /// <summary>
    /// StatusEffectData → IStatusEffect 변환 헬퍼
    /// 🎨 Phase 1: Persistent Effect 지원 추가
    /// </summary>
    private IStatusEffect ConvertFromStatusEffectData(StatusEffectData effectData, GameObject target)
    {
        // EffectValue = 초당 데미지 or 감소율
        float value = effectData.GetCurrentEffectValue(1, 1, 0f);
        float duration = effectData.Duration;
        float tickInterval = effectData.TickInterval > 0f ? effectData.TickInterval : 1.0f;
        
        switch (effectData.EffectType)
        {
            case StatusEffectType.Poison:
                // 🎨 Persistent Effect를 틱 이펙트로 전달
                return new PoisonEffect(target, duration, value, tickInterval, effectData.PersistentEffect, effectData.EffectOffset);
                
            case StatusEffectType.Burn:
                return new BurnEffect(target, duration, value, tickInterval, effectData.PersistentEffect, effectData.EffectOffset);
                
            case StatusEffectType.Slow:
                return new SlowEffect(target, duration, value, effectData.PersistentEffect, effectData.EffectOffset);
                
            case StatusEffectType.Bind:
                // 이동 불가 (🎨 Phase 1: Apply/Persistent Effect 전달)
                return new BindEffect(target, duration, effectData.ApplyEffect, effectData.PersistentEffect, effectData.EffectOffset);
                
            default:
                Debug.LogWarning($"[StatusEffectManager] 지원하지 않는 StatusEffectType: {effectData.EffectType}");
                return null;
        }
    }
    
    /// <summary>
    /// 🎨 Phase 1: 상태이상 이펙트 재생
    /// </summary>
    private void PlayStatusEffectVisuals(StatusEffectData effectData, Transform target)
    {
        if (effectData == null || target == null) return;
        
        // 🎨 Apply 이펙트 (적용 시 1회 재생)
        if (effectData.ApplyEffect != null)
        {
            Vector3 spawnPosition = target.position + Vector3.up * 0.5f; // 대상 위쪽에 생성
            
            if (GamePoolManager.Instance != null)
            {
                // 풀링 시스템 사용
                GameObject effectObj = GamePoolManager.Instance.SpawnFromPool(
                    effectData.ApplyEffect.name, 
                    spawnPosition, 
                    Quaternion.identity);
                
                if (enableDebugLogs)
                    Debug.Log($"🎨 [StatusEffectManager] Apply 이펙트 재생: {effectData.ApplyEffect.name}");
            }
            else
            {
                // Fallback: Instantiate
                GameObject effectObj = Instantiate(effectData.ApplyEffect, spawnPosition, Quaternion.identity);
                Destroy(effectObj, 3f); // 3초 후 제거
                
                if (enableDebugLogs)
                    Debug.Log($"🎨 [StatusEffectManager] Apply 이펙트 재생 (Instantiate): {effectData.ApplyEffect.name}");
            }
        }
        
        // 🔊 Apply 사운드 재생
        if (effectData.ApplySound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(effectData.ApplySound);
            
            if (enableDebugLogs)
                Debug.Log($"🔊 [StatusEffectManager] Apply 사운드 재생: {effectData.ApplySound.name}");
        }
        
        // TODO Phase 2: Persistent 이펙트 (지속 중 따라다니는 이펙트)
        // effectData.PersistentEffect는 Follow 시스템 필요
    }
    
    #endregion
    
    #region 상태이상 제거
    
    /// <summary>
    /// 상태이상 제거 (내부 메서드)
    /// </summary>
    private void RemoveEffect(IStatusEffect effect)
    {
        if (effect == null)
            return;
        
        effect.Remove();
        activeEffects.Remove(effect);
        
        if (enableDebugLogs)
            Debug.Log($"❌ [StatusEffectManager] {effect.EffectType} 제거 ← {effect.Target.name}");
    }
    
    /// <summary>
    /// ⚙️ 특정 타입의 상태이상 제거
    /// </summary>
    public void RemoveEffectByType(EStatusEffectType effectType)
    {
        var effect = FindEffectByType(effectType);
        if (effect != null)
        {
            RemoveEffect(effect);
        }
    }
    
    /// <summary>
    /// ⚙️ 레거시 지원: StatusEffectType 제거
    /// </summary>
    public void RemoveStatusEffect(StatusEffectType effectType)
    {
        // StatusEffectType → EStatusEffectType 변환
        EStatusEffectType convertedType = ConvertStatusEffectType(effectType);
        RemoveEffectByType(convertedType);
    }
    
    /// <summary>
    /// ⚙️ 모든 상태이상 제거
    /// </summary>
    public void ClearAllEffects()
    {
        foreach (var effect in activeEffects)
        {
            effect.Remove();
        }
        
        activeEffects.Clear();
        
        if (enableDebugLogs)
            Debug.Log($"🧹 [StatusEffectManager] 모든 상태이상 제거됨 ({activeEffects.Count}개)");
    }
    
    /// <summary>
    /// ⚙️ 레거시 지원: RemoveAllStatusEffects
    /// </summary>
    public void RemoveAllStatusEffects()
    {
        ClearAllEffects();
    }
    
    /// <summary>
    /// ⚙️ 특정 대상의 모든 상태이상 제거 (사망 시 호출)
    /// </summary>
    public void ClearEffectsOnTarget(GameObject target)
    {
        if (target == null)
            return;
        
        // 대상에게 적용된 효과 찾기
        effectsToRemove.Clear();
        foreach (var effect in activeEffects)
        {
            if (effect.Target == target)
            {
                effectsToRemove.Add(effect);
            }
        }
        
        // 효과 제거
        foreach (var effect in effectsToRemove)
        {
            RemoveEffect(effect);
        }
        
        if (enableDebugLogs && effectsToRemove.Count > 0)
            Debug.Log($"🧹 [StatusEffectManager] {target.name}의 상태이상 {effectsToRemove.Count}개 제거됨");
        
        effectsToRemove.Clear();
    }
    
    #endregion
    
    #region 조회 메서드
    
    /// <summary>
    /// 특정 타입의 상태이상 찾기
    /// </summary>
    private IStatusEffect FindEffectByType(EStatusEffectType effectType)
    {
        // ⚡ GC 최소화: foreach 사용
        foreach (var effect in activeEffects)
        {
            if (effect.EffectType == effectType)
            {
                return effect;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// 특정 타입의 상태이상이 활성화되어 있는지 확인
    /// </summary>
    public bool HasEffect(EStatusEffectType effectType)
    {
        return FindEffectByType(effectType) != null;
    }
    
    /// <summary>
    /// ⚙️ 레거시 지원: HasStatusEffect
    /// </summary>
    public bool HasStatusEffect(StatusEffectType effectType)
    {
        EStatusEffectType convertedType = ConvertStatusEffectType(effectType);
        return HasEffect(convertedType);
    }
    
    /// <summary>
    /// 활성화된 모든 상태이상 목록
    /// </summary>
    public IReadOnlyList<IStatusEffect> GetActiveEffects()
    {
        return activeEffects;
    }
    
    /// <summary>
    /// 활성 상태이상 개수 반환
    /// </summary>
    public int GetActiveEffectCount()
    {
        return activeEffects.Count;
    }
    
    #endregion
    
    #region 타입 변환 헬퍼
    
    /// <summary>
    /// StatusEffectType (레거시) → EStatusEffectType 변환
    /// </summary>
    private EStatusEffectType ConvertStatusEffectType(StatusEffectType oldType)
    {
        switch (oldType)
        {
            case StatusEffectType.Poison:
                return EStatusEffectType.Poison;
            case StatusEffectType.Burn:
                return EStatusEffectType.Burn;
            case StatusEffectType.Slow:
                return EStatusEffectType.Slow;
            case StatusEffectType.Bind:
                return EStatusEffectType.Bind;
            default:
                return EStatusEffectType.None;
        }
    }
    
    #endregion
    
    #region 디버그
    
    /// <summary>
    /// 현재 상태이상 디버그 정보 출력
    /// </summary>
    [ContextMenu("Debug Status Effects")]
    public void DebugStatusEffects()
    {
        if (activeEffects.Count == 0)
        {
            Debug.Log("[StatusEffectManager] 활성 상태이상 없음");
            return;
        }

        string info = "=== 활성 상태이상 목록 ===\n";
        foreach (var effect in activeEffects)
        {
            info += $"  - {effect.EffectType}: {effect.RemainingDuration:F1}초 남음 (값: {effect.Value})\n";
        }
        
        Debug.Log(info);
    }
    
    [ContextMenu("Print Active Effects")]
    private void PrintActiveEffects()
    {
        DebugStatusEffects();
    }
    
    #endregion
}
