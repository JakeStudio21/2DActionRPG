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
    [SerializeField] private bool enableDebugLogs = true;
    
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
        base.Awake();
    }
    
    private void Start()
    {
        // 플레이어 찾기
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerObject = playerHealth.gameObject;
            if (enableDebugLogs)
                Debug.Log($"[StatusEffectManager] 플레이어 오브젝트 찾음: {playerObject.name}");
        }
        else
        {
            Debug.LogWarning("[StatusEffectManager] PlayerHealth를 찾을 수 없습니다!");
        }
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
    
    private void OnDestroy()
    {
        // 모든 상태이상 제거 (정리)
        ClearAllEffects();
    }
    
    #endregion
    
    #region 상태이상 추가 (신규 인터페이스)
    
    /// <summary>
    /// ⚙️ 상태이상 추가 (IStatusEffect 직접 추가)
    /// </summary>
    public void AddEffect(IStatusEffect newEffect)
    {
        if (newEffect == null)
        {
            Debug.LogWarning("[StatusEffectManager] null 상태이상을 추가하려고 시도");
            return;
        }
        
        // 🛡️ 면역 체크 (Phase 4-C)
        if (IsImmuneToEffect(newEffect.Target, newEffect.EffectType))
        {
            if (enableDebugLogs)
                Debug.Log($"🛡️ [StatusEffectManager] {newEffect.Target.name}이(가) {newEffect.EffectType}에 면역! 차단됨.");
            return; // 면역이 있으면 상태이상 적용 차단
        }
        
        // 동일 타입 효과가 이미 있는지 확인
        IStatusEffect existingEffect = FindEffectByType(newEffect.EffectType);
        
        if (existingEffect != null)
        {
            // 이미 존재하면 갱신/중첩
            existingEffect.RefreshOrStack(newEffect.RemainingDuration, newEffect.Value);
            
            if (enableDebugLogs)
                Debug.Log($"🔄 [StatusEffectManager] {newEffect.EffectType} 갱신/중첩 → {newEffect.Target.name}");
        }
        else
        {
            // 새로 추가
            activeEffects.Add(newEffect);
            newEffect.Apply();
            
            if (enableDebugLogs)
                Debug.Log($"✅ [StatusEffectManager] {newEffect.EffectType} 추가 → {newEffect.Target.name} (지속: {newEffect.RemainingDuration:F1}초)");
        }
    }
    
    /// <summary>
    /// 🛡️ 대상이 특정 상태이상에 면역인지 확인
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
    /// ⚙️ 간편 메서드: 중독 효과 추가
    /// </summary>
    public void AddPoison(GameObject target, float duration, float damagePerTick, float tickInterval = 1.0f)
    {
        var effect = new PoisonEffect(target, duration, damagePerTick, tickInterval);
        AddEffect(effect);
    }
    
    /// <summary>
    /// ⚙️ 간편 메서드: 화상 효과 추가
    /// </summary>
    public void AddBurn(GameObject target, float duration, float damagePerTick, float tickInterval = 1.0f)
    {
        var effect = new BurnEffect(target, duration, damagePerTick, tickInterval);
        AddEffect(effect);
    }
    
    /// <summary>
    /// ⚙️ 간편 메서드: 속박 효과 추가
    /// </summary>
    public void AddBind(GameObject target, float duration)
    {
        var effect = new BindEffect(target, duration);
        AddEffect(effect);
    }
    
    /// <summary>
    /// ⚙️ 간편 메서드: 둔화 효과 추가
    /// </summary>
    public void AddSlow(GameObject target, float duration, float slowAmount)
    {
        var effect = new SlowEffect(target, duration, slowAmount);
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
        
        if (playerObject == null)
        {
            Debug.LogWarning("[StatusEffectManager] 플레이어 오브젝트가 없어서 상태이상 적용 불가");
            return;
        }
        
        // StatusEffectData를 IStatusEffect로 변환
        IStatusEffect effect = ConvertFromStatusEffectData(effectData, playerObject);
        
        if (effect != null)
        {
            AddEffect(effect);
        }
    }
    
    /// <summary>
    /// StatusEffectData → IStatusEffect 변환 헬퍼
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
                return new PoisonEffect(target, duration, value, tickInterval);
                
            case StatusEffectType.Burn:
                return new BurnEffect(target, duration, value, tickInterval);
                
            case StatusEffectType.Slow:
                return new SlowEffect(target, duration, value);
                
            case StatusEffectType.Stun:
                // Bind 효과로 대체 (이동 불가)
                return new BindEffect(target, duration);
                
            default:
                Debug.LogWarning($"[StatusEffectManager] 지원하지 않는 StatusEffectType: {effectData.EffectType}");
                return null;
        }
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
            case StatusEffectType.Stun:
                return EStatusEffectType.Bind; // Stun을 Bind로 매핑
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
