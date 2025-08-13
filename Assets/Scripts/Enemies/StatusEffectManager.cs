using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상태이상 효과 관리자
/// 플레이어에게 독, 둔화, 화상, 기절 등의 상태이상을 적용하고 관리
/// </summary>
public class StatusEffectManager : Singleton<StatusEffectManager>
{
    [Header("🔧 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 활성 상태이상들
    private List<ActiveStatusEffect> activeEffects = new List<ActiveStatusEffect>();
    
    // 상태이상별 코루틴 관리
    private Dictionary<StatusEffectType, Coroutine> effectCoroutines = new Dictionary<StatusEffectType, Coroutine>();
    
    // 플레이어 참조
    private PlayerHealth playerHealth;
    private PlayerController playerController;
    
    /// <summary>
    /// 활성 상태이상 데이터 구조
    /// </summary>
    [System.Serializable]
    private class ActiveStatusEffect
    {
        public StatusEffectData effectData;
        public float remainingTime;
        public int stackCount;
        public float lastTickTime;
        
        public ActiveStatusEffect(StatusEffectData data)
        {
            effectData = data;
            remainingTime = data.Duration;
            stackCount = 1;
            lastTickTime = 0f;
        }
    }

    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        // 플레이어 참조 획득
        playerHealth = FindObjectOfType<PlayerHealth>();
        playerController = FindObjectOfType<PlayerController>();
        
        if (playerHealth == null)
            Debug.LogWarning("[StatusEffectManager] PlayerHealth를 찾을 수 없습니다!");
        if (playerController == null)
            Debug.LogWarning("[StatusEffectManager] PlayerController를 찾을 수 없습니다!");
    }

    #region 상태이상 적용/해제

    /// <summary>
    /// 상태이상 적용
    /// </summary>
    public void ApplyStatusEffect(StatusEffectData effectData)
    {
        if (effectData == null || playerHealth == null) return;

        // 이미 같은 효과가 있는지 확인
        var existingEffect = activeEffects.Find(e => e.effectData.EffectType == effectData.EffectType);
        
        if (existingEffect != null)
        {
            // 중첩 가능한 효과인지 확인
            if (effectData.Stackable && existingEffect.stackCount < effectData.MaxStacks)
            {
                existingEffect.stackCount++;
                existingEffect.remainingTime = effectData.Duration; // 지속시간 갱신
                
                if (showDebugLogs)
                    Debug.Log($"[StatusEffect] {effectData.EffectName} 중첩 적용 (스택: {existingEffect.stackCount})");
            }
            else
            {
                // 중첩 불가능하면 지속시간만 갱신
                existingEffect.remainingTime = effectData.Duration;
                
                if (showDebugLogs)
                    Debug.Log($"[StatusEffect] {effectData.EffectName} 지속시간 갱신");
            }
        }
        else
        {
            // 새로운 상태이상 추가
            var newEffect = new ActiveStatusEffect(effectData);
            activeEffects.Add(newEffect);
            
            // 즉시 효과 적용
            ApplyImmediateEffect(effectData);
            
            // 지속 효과 시작
            if (effectData.TickInterval > 0f)
            {
                if (effectCoroutines.ContainsKey(effectData.EffectType))
                {
                    StopCoroutine(effectCoroutines[effectData.EffectType]);
                }
                effectCoroutines[effectData.EffectType] = StartCoroutine(TickEffectCoroutine(newEffect));
            }
            
            if (showDebugLogs)
                Debug.Log($"[StatusEffect] {effectData.EffectName} 새로 적용 (지속: {effectData.Duration}초)");
        }

        // 이펙트 및 사운드 재생
        PlayEffectAndSound(effectData);
    }

    /// <summary>
    /// 특정 타입의 상태이상 제거
    /// </summary>
    public void RemoveStatusEffect(StatusEffectType effectType)
    {
        var effect = activeEffects.Find(e => e.effectData.EffectType == effectType);
        if (effect != null)
        {
            RemoveEffectImpact(effect.effectData);
            activeEffects.Remove(effect);
            
            if (effectCoroutines.ContainsKey(effectType))
            {
                StopCoroutine(effectCoroutines[effectType]);
                effectCoroutines.Remove(effectType);
            }
            
            if (showDebugLogs)
                Debug.Log($"[StatusEffect] {effect.effectData.EffectName} 제거됨");
        }
    }

    /// <summary>
    /// 모든 상태이상 제거
    /// </summary>
    public void RemoveAllStatusEffects()
    {
        foreach (var effect in activeEffects.ToArray())
        {
            RemoveStatusEffect(effect.effectData.EffectType);
        }
    }

    #endregion

    #region 상태이상 효과 구현

    /// <summary>
    /// 즉시 효과 적용 (기절, 둔화 등)
    /// </summary>
    private void ApplyImmediateEffect(StatusEffectData effectData)
    {
        switch (effectData.EffectType)
        {
            case StatusEffectType.Slow:
                ApplySlowEffect(effectData);
                break;
                
            case StatusEffectType.Stun:
                ApplyStunEffect(effectData);
                break;
        }
    }

    /// <summary>
    /// 지속 효과 처리 (독, 화상 등)
    /// </summary>
    private IEnumerator TickEffectCoroutine(ActiveStatusEffect effect)
    {
        while (effect.remainingTime > 0f)
        {
            // 틱 간격 대기
            yield return new WaitForSeconds(effect.effectData.TickInterval);
            
            // 효과 적용
            ApplyTickEffect(effect);
            
            // 시간 감소
            effect.remainingTime -= effect.effectData.TickInterval;
            effect.lastTickTime += effect.effectData.TickInterval;
        }
        
        // 효과 제거
        RemoveStatusEffect(effect.effectData.EffectType);
    }

    /// <summary>
    /// 틱 기반 효과 적용
    /// </summary>
    private void ApplyTickEffect(ActiveStatusEffect effect)
    {
        switch (effect.effectData.EffectType)
        {
            case StatusEffectType.Poison:
                ApplyPoisonTick(effect);
                break;
                
            case StatusEffectType.Burn:
                ApplyBurnTick(effect);
                break;
        }
    }

    /// <summary>
    /// 독 효과 적용
    /// </summary>
    private void ApplyPoisonTick(ActiveStatusEffect effect)
    {
        if (playerHealth == null) return;

        float damage = effect.effectData.GetCurrentEffectValue(1, effect.stackCount, effect.lastTickTime);
        int intDamage = Mathf.RoundToInt(damage);
        
        playerHealth.TakeDamage(intDamage, null);
        
        if (showDebugLogs)
            Debug.Log($"[StatusEffect] 독 데미지: {intDamage} (스택: {effect.stackCount})");
    }

    /// <summary>
    /// 화상 효과 적용
    /// </summary>
    private void ApplyBurnTick(ActiveStatusEffect effect)
    {
        if (playerHealth == null) return;

        float damage = effect.effectData.GetCurrentEffectValue(1, effect.stackCount, effect.lastTickTime);
        int intDamage = Mathf.RoundToInt(damage);
        
        playerHealth.TakeDamage(intDamage, null);
        
        if (showDebugLogs)
            Debug.Log($"[StatusEffect] 화상 데미지: {intDamage} (시간: {effect.lastTickTime:F1}초)");
    }

    /// <summary>
    /// 둔화 효과 적용
    /// </summary>
    private void ApplySlowEffect(StatusEffectData effectData)
    {
        if (playerController == null) return;

        // 이동속도 감소 (effectValue = 0.5면 50% 감소)
        float speedMultiplier = 1f - effectData.EffectValue;
        
        // PlayerController의 이동속도에 배율 적용 (실제 구현은 PlayerController에 따라 다름)
        // 여기서는 로그만 출력
        if (showDebugLogs)
            Debug.Log($"[StatusEffect] 둔화 적용: 이동속도 {speedMultiplier * 100:F0}%로 감소");
    }

    /// <summary>
    /// 기절 효과 적용
    /// </summary>
    private void ApplyStunEffect(StatusEffectData effectData)
    {
        if (playerController == null) return;

        // 플레이어 행동 제한 (실제 구현은 PlayerController에 따라 다름)
        if (showDebugLogs)
            Debug.Log($"[StatusEffect] 기절 적용: {effectData.Duration}초간 행동 불가");
    }

    /// <summary>
    /// 상태이상 제거 시 영향 해제
    /// </summary>
    private void RemoveEffectImpact(StatusEffectData effectData)
    {
        switch (effectData.EffectType)
        {
            case StatusEffectType.Slow:
                // 이동속도 원복
                if (showDebugLogs)
                    Debug.Log($"[StatusEffect] 둔화 해제: 이동속도 원복");
                break;
                
            case StatusEffectType.Stun:
                // 행동 제한 해제
                if (showDebugLogs)
                    Debug.Log($"[StatusEffect] 기절 해제: 행동 가능");
                break;
        }
    }

    #endregion

    #region 이펙트 및 사운드

    /// <summary>
    /// 상태이상 이펙트 및 사운드 재생
    /// </summary>
    private void PlayEffectAndSound(StatusEffectData effectData)
    {
        // 적용 이펙트 재생
        if (effectData.ApplyEffect != null)
        {
            Instantiate(effectData.ApplyEffect, playerController.transform.position, Quaternion.identity);
        }

        // 적용 사운드 재생
        if (effectData.ApplySound != null)
        {
            AudioSource.PlayClipAtPoint(effectData.ApplySound, playerController.transform.position);
        }
    }

    #endregion

    #region 상태 확인 및 디버그

    /// <summary>
    /// 특정 상태이상이 활성화되어 있는지 확인
    /// </summary>
    public bool HasStatusEffect(StatusEffectType effectType)
    {
        return activeEffects.Exists(e => e.effectData.EffectType == effectType);
    }

    /// <summary>
    /// 활성 상태이상 개수 반환
    /// </summary>
    public int GetActiveEffectCount()
    {
        return activeEffects.Count;
    }

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
            info += $"- {effect.effectData.EffectName}: ";
            info += $"남은시간 {effect.remainingTime:F1}초, ";
            info += $"스택 {effect.stackCount}\n";
        }
        
        Debug.Log(info);
    }

    #endregion
}
