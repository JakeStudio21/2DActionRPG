using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CueSystem;

/// <summary>
/// 장판형 DOT 스킬 전용 컴포넌트 (Poison Field 등)
/// 
/// 역할:
///   1. Tick 데미지: tickRate 주기마다 aoeRadius 범위 내 적에게 데미지 (첫 틱은 즉시 적용)
///   2. 슬로우 디버프: Trigger 콜라이더로 진입/퇴장 감지 → 이동속도 감소/복원
///   3. OnDestroy 정리: 수명 만료 시 장판 위 남은 모든 적의 슬로우를 강제 해제
/// 
/// 프리팹 요구사항:
///   - CircleCollider2D (isTrigger = true) — 슬로우 범위용
///   - 이 컴포넌트 부착
/// </summary>
public class DotDamageArea : MonoBehaviour
{
    [Header("🔧 디버그")]
    [SerializeField] private bool enableDebugLogs = false;
    
    [Header("🔵 Gizmos 설정")]
    [Tooltip("Scene View에서 장판 범위 시각화 여부")]
    [SerializeField] private bool showGizmos = true;
    [Tooltip("편집 모드(프리팹 편집)에서 미리볼 범위 반경 (런타임에는 Initialize의 radius 우선)")]
    [SerializeField] private float gizmosPreviewRadius = 2f;
    
    // 초기화 파라미터
    private float aoeRadius;
    private int damagePerTick;
    private float duration;
    private float tickRate;
    private float slowPercentage;
    private string hitCueKey;
    
    // 슬로우 대상 관리 — OnDestroy에서 강제 해제를 위해 HashSet으로 추적
    private readonly HashSet<GameObject> slowedTargets = new HashSet<GameObject>();
    
    // 플레이어 스탯 참조 (CombatFormula 크리티컬/관통 계산용)
    private PlayerRuntimeStats playerRuntimeStats;
    
    // 중복 PerformTick 방지 플래그
    private bool isActive = false;

    /// <summary>
    /// SkillController에서 호출 — 장판 파라미터 주입 후 즉시 가동
    /// </summary>
    public void Initialize(float radius, int damage, float duration, float tickRate, float slowPercentage, string hitCueKey = null)
    {
        this.aoeRadius       = radius;
        this.damagePerTick   = damage;
        this.duration        = duration;
        this.tickRate        = tickRate;
        this.slowPercentage  = slowPercentage;
        this.hitCueKey       = hitCueKey;
        
        playerRuntimeStats = Object.FindObjectOfType<PlayerRuntimeStats>();
        isActive = true;
        
        StartCoroutine(TickRoutine());
        
        // duration 경과 후 자동 파괴 (Tick 코루틴 완료보다 약간 길게 설정)
        Destroy(gameObject, duration + tickRate);
        
        if (enableDebugLogs)
            Debug.Log($"☠️ [DotDamageArea] 초기화: radius={radius}, damage={damage}/tick, duration={duration}s, tickRate={tickRate}s, slow={slowPercentage:P0}");
    }
    
    // ─────────────────────────────────────────────
    // Tick 데미지 시스템
    // ─────────────────────────────────────────────
    
    /// <summary>
    /// 첫 틱은 즉시 적용(게임 필), 이후 tickRate 간격으로 반복
    /// </summary>
    private IEnumerator TickRoutine()
    {
        float elapsed = 0f;
        
        // ⭐ 첫 틱 즉시 적용
        PerformTick();
        
        while (elapsed < duration)
        {
            yield return new WaitForSeconds(tickRate);
            elapsed += tickRate;
            
            if (!isActive) yield break;
            
            PerformTick();
        }
    }
    
    /// <summary>
    /// Physics2D.OverlapCircleAll로 범위 내 적 탐색 후 데미지 적용
    /// </summary>
    private void PerformTick()
    {
        if (!isActive) return;
        
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aoeRadius, LayerMask.GetMask("Enemy"));
        
        if (hits == null || hits.Length == 0) return;
        
        HashSet<GameObject> processed = new HashSet<GameObject>();
        
        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            if (processed.Contains(hit.gameObject)) continue;
            processed.Add(hit.gameObject);
            
            // SimpleMob
            SimpleMob simpleMob = hit.GetComponent<SimpleMob>();
            if (simpleMob != null && !simpleMob.IsDead)
            {
                ApplyDamageToSimpleMob(simpleMob, hit);
                EmitHitCue(hit.transform.position);
                continue;
            }
            
            // EnemyHealth (보스/엘리트)
            EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                ApplyDamageToEnemy(enemyHealth, hit);
                EmitHitCue(hit.transform.position);
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"☠️ [DotDamageArea] Tick 판정: {hits.Length}명 감지");
    }
    
    private void ApplyDamageToSimpleMob(SimpleMob simpleMob, Collider2D hit)
    {
        if (playerRuntimeStats == null)
        {
            simpleMob.TakeDamage(damagePerTick);
            return;
        }
        
        var ctx = new CombatFormula.AttackContext
        {
            baseAttack        = damagePerTick,
            attackerClass     = null,
            targetDefense     = 0f,
            targetTransform   = hit.transform,
            attackerTransform = transform,
            isSkillAttack     = true,
            skillMultiplier   = 1.0f,
            criticalChance    = playerRuntimeStats.FinalCriticalChance,
            criticalMultiplier = playerRuntimeStats.FinalCriticalDamage,
            isPlayerAttack    = true,
            attackerLevel     = playerRuntimeStats.CurrentLevel,
            isBerserkerState  = false,
            armorPenetration  = playerRuntimeStats.FinalArmorPenetration,
            lifeStealPercent  = playerRuntimeStats.FinalLifeSteal,
            target            = null,
            selfHpPercent     = 1.0f,
            targetHpPercent   = 1.0f
        };
        
        var result = CombatFormula.CalculatePlayerToEnemyDamage(ctx);
        simpleMob.TakeDamage(result.finalDamage);
        
        if (enableDebugLogs)
            Debug.Log($"☠️ [DotDamageArea] SimpleMob 데미지: {result.finalDamage} (크리티컬: {result.isCritical}) → {hit.name}");
    }
    
    private void ApplyDamageToEnemy(EnemyHealth enemyHealth, Collider2D hit)
    {
        if (playerRuntimeStats == null)
        {
            enemyHealth.TakeDamage(damagePerTick);
            return;
        }
        
        var baseEnemy = hit.GetComponent<BaseEnemy>();
        float defense = baseEnemy != null ? baseEnemy.GetScaledDefense() : 0f;
        
        var ctx = new CombatFormula.AttackContext
        {
            baseAttack        = damagePerTick,
            attackerClass     = null,
            targetDefense     = defense,
            targetTransform   = hit.transform,
            attackerTransform = transform,
            isSkillAttack     = true,
            skillMultiplier   = 1.0f,
            criticalChance    = playerRuntimeStats.FinalCriticalChance,
            criticalMultiplier = playerRuntimeStats.FinalCriticalDamage,
            isPlayerAttack    = true,
            attackerLevel     = playerRuntimeStats.CurrentLevel,
            isBerserkerState  = false,
            armorPenetration  = playerRuntimeStats.FinalArmorPenetration,
            lifeStealPercent  = playerRuntimeStats.FinalLifeSteal,
            target            = hit.GetComponent<IEnemyTarget>(),
            selfHpPercent     = 1.0f,
            targetHpPercent   = GetTargetHpPercent(hit)
        };
        
        var result = CombatFormula.CalculatePlayerToEnemyDamage(ctx);
        result.hitPosition = hit.transform.position;
        enemyHealth.TakeDamage(result, transform);
        
        if (enableDebugLogs)
            Debug.Log($"☠️ [DotDamageArea] EnemyHealth 데미지: {result.finalDamage} (크리티컬: {result.isCritical}) → {hit.name}");
    }
    
    private void EmitHitCue(Vector3 position)
    {
        if (string.IsNullOrEmpty(hitCueKey)) return;
        CueEmitter.Emit(hitCueKey, "Player", new CueContext { position = position });
    }
    
    private float GetTargetHpPercent(Collider2D hit)
    {
        var enemyTarget = hit.GetComponent<IEnemyTarget>();
        return enemyTarget != null ? enemyTarget.GetCurrentHpPercent() : 1.0f;
    }
    
    // ─────────────────────────────────────────────
    // 슬로우 디버프 시스템 (Trigger 기반)
    // ─────────────────────────────────────────────
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        if (slowedTargets.Contains(other.gameObject)) return;
        
        // SimpleMob 슬로우
        SimpleMob simpleMob = other.GetComponent<SimpleMob>();
        if (simpleMob != null && !simpleMob.IsDead)
        {
            slowedTargets.Add(other.gameObject);
            simpleMob.ApplySlow(slowPercentage);
            return;
        }
        
        // BaseEnemy (NavMesh) 슬로우
        BaseEnemy baseEnemy = other.GetComponent<BaseEnemy>();
        if (baseEnemy != null && baseEnemy.IsUsingNavMesh && baseEnemy.Agent != null)
        {
            slowedTargets.Add(other.gameObject);
            baseEnemy.Agent.speed *= (1f - Mathf.Clamp01(slowPercentage));
            
            if (enableDebugLogs)
                Debug.Log($"☠️ [DotDamageArea] BaseEnemy 슬로우 적용: {other.name}, speed={baseEnemy.Agent.speed:F2}");
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!slowedTargets.Contains(other.gameObject)) return;
        
        slowedTargets.Remove(other.gameObject);
        RemoveSlowFromTarget(other.gameObject);
    }
    
    /// <summary>
    /// 장판 수명 만료 시 호출 — 아직 장판 위에 있는 모든 적의 슬로우를 강제 해제
    /// 영구 이동속도 감소 버그 방지
    /// </summary>
    private void OnDestroy()
    {
        isActive = false;
        
        foreach (GameObject target in slowedTargets)
        {
            if (target != null)
                RemoveSlowFromTarget(target);
        }
        slowedTargets.Clear();
        
        if (enableDebugLogs)
            Debug.Log($"☠️ [DotDamageArea] 소멸 — 잔여 슬로우 모두 해제 완료");
    }
    
    private void RemoveSlowFromTarget(GameObject target)
    {
        // SimpleMob 슬로우 해제
        SimpleMob simpleMob = target.GetComponent<SimpleMob>();
        if (simpleMob != null)
        {
            simpleMob.RemoveSlow();
            return;
        }
        
        // BaseEnemy 슬로우 해제 — GetScaledMoveSpeed()로 원래 속도 복원
        BaseEnemy baseEnemy = target.GetComponent<BaseEnemy>();
        if (baseEnemy != null && baseEnemy.IsUsingNavMesh && baseEnemy.Agent != null)
        {
            baseEnemy.Agent.speed = baseEnemy.GetScaledMoveSpeed();
            
            if (enableDebugLogs)
                Debug.Log($"☠️ [DotDamageArea] BaseEnemy 슬로우 해제: {target.name}, speed={baseEnemy.Agent.speed:F2}");
        }
    }
    
    // ─────────────────────────────────────────────
    // Gizmos — Scene View 시각화
    // ─────────────────────────────────────────────
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // 런타임에는 초기화된 aoeRadius, 편집 모드에서는 Inspector 미리보기 값 사용
        float drawRadius = (Application.isPlaying && isActive) ? aoeRadius : gizmosPreviewRadius;
        
        DrawDotAreaGizmos(drawRadius);
    }
    
    private void DrawDotAreaGizmos(float radius)
    {
        Vector3 center = transform.position;
        int segments = 48;
        
        // 외곽선 — 독 느낌의 초록색
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.9f);
        Vector3 prevPoint = center + Vector3.right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = (360f / segments) * i * Mathf.Deg2Rad;
            Vector3 currentPoint = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
        
        // 채움 — 반투명 초록
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.08f);
        // 삼각형 팬 방식으로 채움 근사
        int fillSegments = 24;
        for (int i = 0; i < fillSegments; i++)
        {
            float a1 = (360f / fillSegments) * i * Mathf.Deg2Rad;
            float a2 = (360f / fillSegments) * (i + 1) * Mathf.Deg2Rad;
            Vector3 p1 = center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * radius;
            Vector3 p2 = center + new Vector3(Mathf.Cos(a2), Mathf.Sin(a2), 0f) * radius;
            Gizmos.DrawLine(center, p1);
            Gizmos.DrawLine(p1, p2);
        }
        
        // 중심점
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 1f);
        Gizmos.DrawSphere(center, 0.15f);
        
        // 반경선 — 범위 크기 직관적 확인용
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.5f);
        Gizmos.DrawLine(center, center + Vector3.right * radius);
    }
}
