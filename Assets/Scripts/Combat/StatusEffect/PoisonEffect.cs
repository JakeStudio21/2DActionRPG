using UnityEngine;

/// <summary>
/// 중독 효과 - 일정 간격으로 독 데미지
/// ⚙️ Phase 4-C: 플레이어/몬스터 모두 지원, CombatFormula 통합
/// </summary>
public class PoisonEffect : BaseStatusEffect
{
    #region 필드
    
    // 대상 컴포넌트 캐싱 (⚡ GC 최적화)
    private PlayerHealth playerHealth;
    private EnemyHealth enemyHealth;
    
    // 틱 데미지 설정
    private float tickInterval; // 틱 간격 (초)
    private float lastTickTime; // 마지막 틱 시간
    
    #endregion
    
    #region 생성자
    
    /// <summary>
    /// 중독 효과 생성자
    /// </summary>
    /// <param name="target">적용 대상 (플레이어 또는 몬스터)</param>
    /// <param name="duration">지속 시간 (초)</param>
    /// <param name="damagePerTick">틱당 데미지</param>
    /// <param name="tickInterval">틱 간격 (초, 기본값 1초)</param>
    public PoisonEffect(GameObject target, float duration, float damagePerTick, float tickInterval = 1.0f)
        : base(EStatusEffectType.Poison, target, duration, damagePerTick)
    {
        this.tickInterval = tickInterval;
        this.lastTickTime = 0f;
        
        // 대상 컴포넌트 캐싱
        if (target != null)
        {
            playerHealth = target.GetComponent<PlayerHealth>();
            enemyHealth = target.GetComponent<EnemyHealth>();
            
            if (playerHealth == null && enemyHealth == null)
            {
                Debug.LogError($"[PoisonEffect] {target.name}에 PlayerHealth 또는 EnemyHealth 컴포넌트가 없습니다!");
            }
        }
    }
    
    #endregion
    
    #region 오버라이드 메서드
    
    /// <summary>
    /// 중독 적용 - 첫 틱 시간 초기화
    /// </summary>
    public override void Apply()
    {
        if (target == null)
        {
            Debug.LogWarning("[PoisonEffect] 대상이 null입니다!");
            return;
        }
        
        lastTickTime = Time.time;
        
        if (enableDebugLogs)
            Debug.Log($"☠️ [PoisonEffect] 중독 적용: {value} 데미지/{tickInterval}초, {remainingDuration:F1}초 지속 → {target.name}");
    }
    
    /// <summary>
    /// 중독 제거 - 로그만 출력
    /// </summary>
    public override void Remove()
    {
        if (enableDebugLogs && target != null)
            Debug.Log($"✅ [PoisonEffect] 중독 해제 → {target.name}");
    }
    
    /// <summary>
    /// 매 프레임 틱 처리 - 지속시간 감소 + 주기적 데미지
    /// </summary>
    public override bool Tick(float deltaTime)
    {
        // 지속시간 감소 (부모 메서드 호출)
        bool isExpired = base.Tick(deltaTime);
        
        // 틱 데미지 처리
        if (!isExpired && Time.time >= lastTickTime + tickInterval)
        {
            ApplyTickDamage();
            lastTickTime = Time.time;
        }
        
        return isExpired;
    }
    
    #endregion
    
    #region 틱 데미지 처리
    
    /// <summary>
    /// 틱 데미지 적용
    /// ⚙️ CombatFormula.DamageResult를 사용하여 정상적인 파이프라인 통과
    /// </summary>
    private void ApplyTickDamage()
    {
        if (target == null)
            return;
        
        int damage = Mathf.RoundToInt(value);
        
        // DamageResult 생성 (상태이상 데미지는 흡혈/회복차단 없음)
        CombatFormula.DamageResult damageResult = new CombatFormula.DamageResult
        {
            finalDamage = damage,
            lifeStealAmount = 0,
            healingBlockPercent = 0f,
            hasImmunity = false,
            resistedEffects = ""
        };
        
        // 🧑 플레이어에게 틱 데미지
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damageResult, target.transform);
            
            if (enableDebugLogs)
                Debug.Log($"☠️ [PoisonEffect] 플레이어 중독 틱 데미지: {damage}");
        }
        // 👾 몬스터에게 틱 데미지
        else if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageResult, target.transform);
            
            if (enableDebugLogs)
                Debug.Log($"☠️ [PoisonEffect] 몬스터 {target.name} 중독 틱 데미지: {damage}");
        }
    }
    
    #endregion
    
    #region 중첩 처리
    
    /// <summary>
    /// 중독 중첩 - 데미지 누적 (최대 3스택)
    /// </summary>
    public override void RefreshOrStack(float newDuration, float newValue)
    {
        const int maxStacks = 3;
        float currentDamagePerStack = value / Mathf.Max(1, GetCurrentStacks());
        
        // 스택 증가 (데미지 누적)
        if (GetCurrentStacks() < maxStacks)
        {
            value += newValue;
            remainingDuration = Mathf.Max(remainingDuration, newDuration); // 더 긴 시간으로
            
            if (enableDebugLogs)
                Debug.Log($"☠️💀 [PoisonEffect] 중독 중첩: {GetCurrentStacks()}스택 (틱당 {value} 데미지)");
        }
        else
        {
            // 최대 스택이면 지속시간만 갱신
            base.RefreshOrStack(newDuration, newValue);
            
            if (enableDebugLogs)
                Debug.Log($"☠️ [PoisonEffect] 중독 최대 스택 ({maxStacks}), 지속시간 갱신");
        }
    }
    
    /// <summary>
    /// 현재 중첩 수 계산 (간이 계산)
    /// </summary>
    private int GetCurrentStacks()
    {
        // 기본 데미지를 기준으로 현재 스택 수 추정
        // 실제로는 별도 필드로 관리하는 것이 더 정확하지만, 간단하게 구현
        return Mathf.CeilToInt(value / 5f); // 예: 틱당 5 데미지가 기본
    }
    
    #endregion
}

