using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상태이상 효과 데이터 ScriptableObject
/// 독, 둔화, 화상, 기절 등의 상태이상 정의
/// </summary>
[CreateAssetMenu(fileName = "StatusEffectData", menuName = "Enemy/Status Effect Data")]
public class StatusEffectData : ScriptableObject
{
    [Header("🏷️ 기본 정보")]
    [SerializeField] private string effectName = "상태이상";
    [SerializeField] private StatusEffectType effectType = StatusEffectType.None;
    [TextArea(2, 3)]
    [SerializeField] private string description = "상태이상 설명";

    [Header("⏱️ 지속시간 설정")]
    [Tooltip("상태이상 지속시간 (초)")]
    [SerializeField] private float duration = 3f;
    
    [Tooltip("틱 간격 - 독/화상용 (초), 0이면 즉시 효과")]
    [SerializeField] private float tickInterval = 1f;

    [Header("💪 효과 강도")]
    [Tooltip("데미지 또는 감소율 (독: 데미지, 둔화: 0.5 = 50% 감소)")]
    [SerializeField] private float effectValue = 1f;
    
    [Tooltip("효과가 시간에 따라 감소하는지")]
    [SerializeField] private bool degradeOverTime = false;
    
    [Tooltip("감소율 (degradeOverTime이 true일 때)")]
    [SerializeField] private float degradeRate = 0.1f;

    [Header("🎮 게임플레이 설정")]
    [Tooltip("동일 효과 중첩 가능 여부")]
    [SerializeField] private bool stackable = false;
    
    [Tooltip("최대 중첩 수 (stackable이 true일 때)")]
    [SerializeField] private int maxStacks = 3;
    
    [Tooltip("상태이상 해제 조건")]
    [SerializeField] private StatusRemovalCondition removalCondition = StatusRemovalCondition.TimeExpired;

    [Header("🎨 시각/음향 효과")]
    [Tooltip("상태이상 적용 시 이펙트")]
    [SerializeField] private GameObject applyEffect;
    
    [Tooltip("상태이상 진행 중 이펙트 (계속 표시)")]
    [SerializeField] private GameObject persistentEffect;
    
    [Tooltip("상태이상 제거 시 이펙트")]
    [SerializeField] private GameObject removeEffect;
    
    [Tooltip("상태이상 적용 사운드")]
    [SerializeField] private AudioClip applySound;

    // Public Properties (Read-Only)
    public string EffectName => effectName;
    public StatusEffectType EffectType => effectType;
    public string Description => description;
    public float Duration => duration;
    public float TickInterval => tickInterval;
    public float EffectValue => effectValue;
    public bool DegradeOverTime => degradeOverTime;
    public float DegradeRate => degradeRate;
    public bool Stackable => stackable;
    public int MaxStacks => maxStacks;
    public StatusRemovalCondition RemovalCondition => removalCondition;
    public GameObject ApplyEffect => applyEffect;
    public GameObject PersistentEffect => persistentEffect;
    public GameObject RemoveEffect => removeEffect;
    public AudioClip ApplySound => applySound;

    /// <summary>
    /// 레벨에 따른 스케일된 효과값 계산
    /// </summary>
    public float GetScaledEffectValue(int level, float levelMultiplier = 1.1f)
    {
        return effectValue * Mathf.Pow(levelMultiplier, level - 1);
    }

    /// <summary>
    /// 현재 스택과 시간에 따른 실제 효과값 계산
    /// </summary>
    public float GetCurrentEffectValue(int level, int currentStacks, float elapsedTime)
    {
        float baseValue = GetScaledEffectValue(level);
        
        // 스택 적용
        if (stackable && currentStacks > 1)
        {
            baseValue *= Mathf.Min(currentStacks, maxStacks);
        }
        
        // 시간에 따른 감소 적용
        if (degradeOverTime && elapsedTime > 0)
        {
            float degradeFactor = 1f - (elapsedTime / duration * degradeRate);
            baseValue *= Mathf.Max(0.1f, degradeFactor); // 최소 10%는 유지
        }
        
        return baseValue;
    }

    /// <summary>
    /// 상태이상 타입별 기본 동작 확인
    /// </summary>
    public bool IsInstantEffect()
    {
        return effectType == StatusEffectType.Stun || tickInterval <= 0f;
    }

    /// <summary>
    /// 틱 기반 상태이상인지 확인
    /// </summary>
    public bool IsTickBasedEffect()
    {
        return (effectType == StatusEffectType.Poison || effectType == StatusEffectType.Burn) 
               && tickInterval > 0f;
    }

    /// <summary>
    /// Inspector에서 설정값 검증
    /// </summary>
    private void OnValidate()
    {
        // 기본값 검증
        duration = Mathf.Max(0.1f, duration);
        tickInterval = Mathf.Max(0f, tickInterval);
        effectValue = Mathf.Max(0f, effectValue);
        degradeRate = Mathf.Clamp01(degradeRate);
        maxStacks = Mathf.Max(1, maxStacks);

        // 타입별 기본 설정 적용
        ApplyTypeDefaults();
    }

    /// <summary>
    /// 상태이상 타입에 따른 기본 설정 적용
    /// </summary>
    private void ApplyTypeDefaults()
    {
        switch (effectType)
        {
            case StatusEffectType.Poison:
                if (tickInterval == 0f) tickInterval = 1f; // 독은 기본 1초 틱
                stackable = true; // 독은 중첩 가능
                break;
                
            case StatusEffectType.Burn:
                if (tickInterval == 0f) tickInterval = 0.5f; // 화상은 빠른 틱
                degradeOverTime = true; // 화상은 시간에 따라 약해짐
                break;
                
            case StatusEffectType.Slow:
                tickInterval = 0f; // 둔화는 즉시 적용
                effectValue = Mathf.Clamp01(effectValue); // 둔화는 0~1 범위
                break;
                
            case StatusEffectType.Stun:
                tickInterval = 0f; // 기절은 즉시 적용
                stackable = false; // 기절은 중첩 불가
                break;
        }
    }

    /// <summary>
    /// 디버그용 정보 출력
    /// </summary>
    public string GetDebugInfo(int level = 1, int stacks = 1, float elapsedTime = 0f)
    {
        return $"{effectName} ({effectType})\n" +
               $"Duration: {duration}s, Tick: {tickInterval}s\n" +
               $"Base Value: {effectValue} → Scaled: {GetScaledEffectValue(level):F1}\n" +
               $"Current Effect: {GetCurrentEffectValue(level, stacks, elapsedTime):F1}\n" +
               $"Stackable: {stackable} (Max: {maxStacks})\n" +
               $"Instant: {IsInstantEffect()}, Tick-based: {IsTickBasedEffect()}";
    }
}

/// <summary>
/// 상태이상 해제 조건
/// </summary>
public enum StatusRemovalCondition
{
    TimeExpired,    // 시간 만료
    OnDamage,       // 데미지 받을 때
    OnMove,         // 이동할 때
    OnAction,       // 행동할 때
    Manual          // 수동 해제
}
