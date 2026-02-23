using UnityEngine;

/// <summary>
/// 상태이상 추상 클래스
/// ⚙️ Phase 4-C: 모든 상태이상의 공통 로직
/// </summary>
public abstract class BaseStatusEffect : IStatusEffect
{
    #region 필드
    
    protected EStatusEffectType effectType;
    protected float remainingDuration;
    protected float value;
    protected GameObject target;
    
    // 디버그용
    protected bool enableDebugLogs = false;
    
    #endregion
    
    #region 프로퍼티
    
    public EStatusEffectType EffectType => effectType;
    public float RemainingDuration => remainingDuration;
    public float Value => value;
    public GameObject Target => target;
    
    #endregion
    
    #region 생성자
    
    /// <summary>
    /// 기본 생성자
    /// </summary>
    /// <param name="effectType">상태이상 타입</param>
    /// <param name="target">적용 대상</param>
    /// <param name="duration">지속 시간 (초)</param>
    /// <param name="value">효과 값 (데미지량, 감소율 등)</param>
    protected BaseStatusEffect(EStatusEffectType effectType, GameObject target, float duration, float value)
    {
        this.effectType = effectType;
        this.target = target;
        this.remainingDuration = duration;
        this.value = value;
    }
    
    #endregion
    
    #region 추상 메서드
    
    /// <summary>
    /// 상태이상 적용 (시작)
    /// </summary>
    public abstract void Apply();
    
    /// <summary>
    /// 상태이상 제거 (종료)
    /// </summary>
    public abstract void Remove();
    
    #endregion
    
    #region 공통 메서드
    
    /// <summary>
    /// 매 프레임 업데이트
    /// </summary>
    public virtual bool Tick(float deltaTime)
    {
        remainingDuration -= deltaTime;
        
        if (remainingDuration <= 0f)
        {
            if (enableDebugLogs)
                Debug.Log($"[StatusEffect] {effectType} 지속시간 만료 ({target?.name})");
            
            return true; // 만료됨
        }
        
        return false; // 아직 유효
    }
    
    /// <summary>
    /// 동일 타입 효과 중첩 처리
    /// 기본 동작: 지속시간 갱신 (더 긴 시간으로)
    /// </summary>
    public virtual void RefreshOrStack(float newDuration, float newValue)
    {
        // 지속시간 갱신 (더 긴 시간으로)
        if (newDuration > remainingDuration)
        {
            remainingDuration = newDuration;
            
            if (enableDebugLogs)
                Debug.Log($"[StatusEffect] {effectType} 지속시간 갱신: {remainingDuration:F1}초");
        }
        
        // 값은 기본적으로 갱신하지 않음 (파생 클래스에서 오버라이드 가능)
    }
    
    #endregion
}

