using UnityEngine;

/// <summary>
/// 화상 효과 - 일정 간격으로 화상 데미지
/// ⚙️ Phase 4-C: 플레이어/몬스터 모두 지원, CombatFormula 통합
/// </summary>
public class BurnEffect : BaseStatusEffect
{
    #region 필드
    
    // 대상 컴포넌트 캐싱 (⚡ GC 최적화)
    private PlayerHealth playerHealth;
    private EnemyHealth enemyHealth;
    
    // 틱 데미지 설정
    private float tickInterval; // 틱 간격 (초)
    private float lastTickTime; // 마지막 틱 시간
    
    // 🎨 틱 이펙트
    private GameObject tickEffectPrefab;
    private Vector3 effectOffset;  // 이펙트 스폰 위치 오프셋
    
    #endregion
    
    #region 생성자
    
    /// <summary>
    /// 화상 효과 생성자
    /// </summary>
    /// <param name="target">적용 대상 (플레이어 또는 몬스터)</param>
    /// <param name="duration">지속 시간 (초)</param>
    /// <param name="damagePerTick">틱당 데미지</param>
    /// <param name="tickInterval">틱 간격 (초, 기본값 1초)</param>
    /// <param name="tickEffect">틱마다 재생할 이펙트</param>
    /// <param name="offset">이펙트 스폰 위치 오프셋 (타겟 기준)</param>
    public BurnEffect(GameObject target, float duration, float damagePerTick, float tickInterval = 1.0f, GameObject tickEffect = null, Vector3 offset = default)
        : base(EStatusEffectType.Burn, target, duration, damagePerTick)
    {
        this.tickInterval = tickInterval;
        this.lastTickTime = 0f;
        this.tickEffectPrefab = tickEffect;
        this.effectOffset = offset == default ? new Vector3(0f, 0.5f, 0f) : offset;
        
        // 대상 컴포넌트 캐싱
        if (target != null)
        {
            playerHealth = target.GetComponent<PlayerHealth>();
            enemyHealth = target.GetComponent<EnemyHealth>();
            
            if (playerHealth == null && enemyHealth == null)
            {
                Debug.LogError($"[BurnEffect] {target.name}에 PlayerHealth 또는 EnemyHealth 컴포넌트가 없습니다!");
            }
        }
    }
    
    #endregion
    
    #region 오버라이드 메서드
    
    /// <summary>
    /// 화상 적용 - 첫 틱 시간 초기화
    /// </summary>
    public override void Apply()
    {
        if (target == null)
        {
            Debug.LogWarning("[BurnEffect] 대상이 null입니다!");
            return;
        }
        
        lastTickTime = Time.time;
        
        if (enableDebugLogs)
            Debug.Log($"🔥 [BurnEffect] 화상 적용: {value} 데미지/{tickInterval}초, {remainingDuration:F1}초 지속 → {target.name}");
    }
    
    /// <summary>
    /// 화상 제거 - 로그만 출력
    /// </summary>
    public override void Remove()
    {
        if (enableDebugLogs && target != null)
            Debug.Log($"✅ [BurnEffect] 화상 해제 → {target.name}");
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
                Debug.Log($"🔥 [BurnEffect] 플레이어 화상 틱 데미지: {damage}");
        }
        // 👾 몬스터에게 틱 데미지
        else if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damageResult, target.transform);
            
            if (enableDebugLogs)
                Debug.Log($"🔥 [BurnEffect] 몬스터 {target.name} 화상 틱 데미지: {damage}");
        }
        
        // 🎨 틱 이펙트 재생
        PlayTickEffect();
    }
    
    /// <summary>
    /// 🎨 틱마다 재생되는 이펙트
    /// </summary>
    private void PlayTickEffect()
    {
        if (tickEffectPrefab == null || target == null)
            return;
        
        Vector3 spawnPosition = target.transform.position + effectOffset;
        
        if (GamePoolManager.Instance != null)
        {
            GameObject effectObj = GamePoolManager.Instance.SpawnFromPool(
                tickEffectPrefab.name,
                spawnPosition,
                Quaternion.identity);
        }
        else
        {
            GameObject effectObj = Object.Instantiate(tickEffectPrefab, spawnPosition, Quaternion.identity);
            Object.Destroy(effectObj, 2f);
        }
    }
    
    #endregion
    
    #region 중첩 처리
    
    /// <summary>
    /// 화상 중첩 - 지속시간 갱신 (중복 불가)
    /// 화상은 독과 달리 중첩되지 않고 지속시간만 연장
    /// </summary>
    public override void RefreshOrStack(float newDuration, float newValue)
    {
        // 더 긴 지속시간으로 갱신
        if (newDuration > remainingDuration)
        {
            remainingDuration = newDuration;
            
            if (enableDebugLogs)
                Debug.Log($"🔥 [BurnEffect] 화상 지속시간 갱신: {remainingDuration:F1}초");
        }
        
        // 더 강한 데미지로 덮어쓰기
        if (newValue > value)
        {
            value = newValue;
            
            if (enableDebugLogs)
                Debug.Log($"🔥 [BurnEffect] 화상 데미지 강화: {value} 데미지/틱");
        }
    }
    
    #endregion
}

