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
    
    // 🛡️ Phase 1: 저항 시스템용
    protected float originalDuration; // 원래 지속시간 (디버그/로그용)
    
    // 디버그용
    protected bool enableDebugLogs = false; // ⭐ Production: false
    
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
        this.originalDuration = duration; // 🛡️ Phase 1: 원래 지속시간 저장
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
    
    /// <summary>
    /// 🛡️ Phase 1: 저항력 적용
    /// 피격자의 저항 수치에 따라 상태이상 지속시간을 감소시킴
    /// </summary>
    /// <param name="resistance">저항 수치 (0.0 ~ 1.0, 1.0 = 100% 저항)</param>
    /// <returns>true: 완전 저항 (효과 무효화), false: 부분 저항 (지속시간 감소 후 효과 적용)</returns>
    public virtual bool ApplyResistance(float resistance)
    {
        // 0. 저항값 클램핑 (0.0 ~ 1.0)
        resistance = Mathf.Clamp01(resistance);
        
        // 1. 저항 0%면 아무 처리 안 함 (최적화)
        if (resistance <= 0f)
        {
            return false; // 저항 없음 → 효과 적용
        }
        
        // 2. 100% 저항 = 완전 면역
        if (resistance >= 1.0f)
        {
            if (enableDebugLogs)
                Debug.Log($"🛡️ [BaseStatusEffect] {effectType} 완전 저항! (100% 저항) → {target?.name}");
            
            remainingDuration = 0f;
            return true; // 효과 무효화
        }
        
        // 3. 지속시간 감소 공식: FinalDuration = OriginalDuration × (1.0 - Resistance)
        float reductionMultiplier = 1.0f - resistance;
        float beforeDuration = remainingDuration;
        remainingDuration *= reductionMultiplier;
        
        // 4. 최소 지속시간 체크 (0.1초 미만이면 무효화)
        const float MIN_DURATION = 0.1f;
        if (remainingDuration < MIN_DURATION)
        {
            if (enableDebugLogs)
                Debug.Log($"🛡️ [BaseStatusEffect] {effectType} 저항으로 지속시간 너무 짧음! " +
                          $"{beforeDuration:F2}초 → {remainingDuration:F2}초 (< {MIN_DURATION}초) → 무효화");
            
            remainingDuration = 0f;
            return true; // 효과 무효화
        }
        
        // 5. 디버그 로그 (부분 저항)
        if (enableDebugLogs)
        {
            Debug.Log($"🛡️ [BaseStatusEffect] {effectType} 저항 적용: " +
                      $"{originalDuration:F1}초 → {remainingDuration:F1}초 " +
                      $"(저항 {resistance * 100:F0}%, {(1f - reductionMultiplier) * 100:F0}% 감소) → {target?.name}");
        }
        
        return false; // 부분 저항 (효과 적용됨)
    }
    
    #endregion
}

