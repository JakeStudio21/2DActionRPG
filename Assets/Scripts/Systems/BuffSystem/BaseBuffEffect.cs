using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🔥 버프/디버프 효과 기본 구현 클래스
/// 공통 기능을 제공하고, 자식 클래스에서 Apply/Remove만 구현하면 됨
/// </summary>
[System.Serializable]
public abstract class BaseBuffEffect : IBuffEffect
{
    [Header("📋 기본 정보")]
    [SerializeField] protected string effectID;
    [SerializeField] protected string effectName;
    [SerializeField] protected Sprite effectIcon;
    [SerializeField] protected BuffEffectType effectType;
    
    [Header("⏰ 지속시간 관리")]
    [SerializeField] protected float duration = 10f;  // 기본 10초
    [SerializeField] protected float remainingTime;
    
    [Header("📚 중첩 관리")]
    [SerializeField] protected bool canStack = false;
    [SerializeField] protected int stackCount = 1;
    [SerializeField] protected int maxStackCount = 1;
    
    // IBuffEffect 구현
    public virtual string EffectID => effectID;
    public virtual string EffectName => effectName;
    public virtual Sprite EffectIcon => effectIcon;
    public virtual float Duration => duration;
    public virtual float RemainingTime { get => remainingTime; set => remainingTime = value; }
    public virtual bool CanStack => canStack;
    public virtual int StackCount { get => stackCount; set => stackCount = value; }
    public virtual int MaxStackCount => maxStackCount;
    public virtual BuffEffectType EffectType => effectType;
    
    /// <summary>
    /// 효과 초기화 (생성 시 호출)
    /// </summary>
    public virtual void Initialize()
    {
        remainingTime = duration;
        stackCount = 1;
        
    }
    
    /// <summary>
    /// 효과 갱신 (매 프레임)
    /// </summary>
    public virtual bool Update(float deltaTime)
    {
        if (duration < 0) return false; // 영구 효과
        
        remainingTime -= deltaTime;
        
        if (remainingTime <= 0)
        {
            return true; // 만료
        }
        
        return false; // 계속 유지
    }
    
    /// <summary>
    /// 중첩 처리
    /// </summary>
    public virtual void OnStack(IBuffEffect newEffect)
    {
        if (!canStack) 
        {
            // 중첩 불가능하면 지속시간 갱신
            remainingTime = duration;
            return;
        }
        
        if (stackCount < maxStackCount)
        {
            stackCount++;
            remainingTime = Mathf.Max(remainingTime, newEffect.Duration); // 더 긴 시간으로
            
        }
        else
        {
            // 최대 중첩에서는 지속시간만 갱신
            remainingTime = newEffect.Duration;
        }
    }
    
    /// <summary>
    /// 효과 복사본 생성
    /// </summary>
    public abstract IBuffEffect Clone();
    
    /// <summary>
    /// 효과 적용 (자식 클래스에서 구현)
    /// </summary>
    public abstract void Apply(PlayerRuntimeStats stats);
    
    /// <summary>
    /// 효과 제거 (자식 클래스에서 구현)
    /// </summary>
    public abstract void Remove(PlayerRuntimeStats stats);
    
    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public virtual string GetDebugInfo()
    {
        return $"{effectName} (ID: {effectID}, 남은시간: {remainingTime:F1}초, 중첩: {stackCount}/{maxStackCount})";
    }
}
