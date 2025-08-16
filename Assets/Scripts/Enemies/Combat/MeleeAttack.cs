using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 근접 공격 구현체 - BaseAttackBehaviour 상속으로 중복 코드 제거
/// ⭐ [Phase 2] AttackData 기반 확장 지원 (근접 공격 특화)
/// </summary>
public class MeleeAttack : BaseAttackBehaviour
{
    #region 기존 시스템 (100% 유지)
    
    [Header("Melee Specific Settings")]
    [SerializeField] private int meleeDamage = 1;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private LayerMask playerLayerMask = 1 << 3;
    
    #endregion

    #region ⭐ 새 시스템: 근접 공격 전용 설정
    
    [Header("⭐ 근접 공격 고급 설정")]
    [Tooltip("공격 각도 (360도면 전방향, 90도면 정면만)")]
    [SerializeField] private float attackAngle = 90f;
    
    [Tooltip("공격 원점 오프셋 (몬스터 중심에서 얼마나 앞으로)")]
    [SerializeField] private Vector2 attackOriginOffset = Vector2.zero;
    
    [Tooltip("크리티컬 히트 시 추가 이펙트")]
    [SerializeField] private GameObject criticalHitEffect;
    
    [Tooltip("근접 공격 디버그 표시")]
    [SerializeField] private bool showAttackGizmos = true;
    
    #endregion

    #region BaseAttackBehaviour 추상 메서드 구현 (기존 + 확장)

    protected override void OnInitialize()
    {
        // ⭐ 새 시스템: 근접 공격 데이터 검증
        ValidateMeleeSettings();
    }

    protected override void OnAttack()
    {
        // 근접 공격 전용 로직 (현재는 애니메이션 이벤트에서 처리)
        // 필요하다면 즉시 공격 로직을 여기에 추가 가능
        Debug.Log($"[MeleeAttack] {gameObject.name} - 근접 공격 준비 완료");
    }
    
    /// <summary>
    /// ⭐ 새 시스템: 공격 타입 검증 (BaseAttackBehaviour에서 요구)
    /// </summary>
    protected override void ValidateAttackType()
    {
        if (AttackData != null && AttackData.AttackType != AttackType.Melee)
        {
            Debug.LogWarning($"[MeleeAttack] {gameObject.name} - AttackData의 공격 타입이 Melee가 아닙니다: {AttackData.AttackType}");
        }
    }
    
    #endregion

    #region ⭐ 새 시스템: Fallback 메서드들 구현
    
    /// <summary>
    /// AttackData가 없을 때 사용할 기본 데미지
    /// </summary>
    protected override int GetFallbackDamage() => meleeDamage;
    
    /// <summary>
    /// AttackData가 없을 때 사용할 기본 공격 범위
    /// </summary>
    protected override float GetFallbackRange() => attackRange;
    
    #endregion

    #region ⭐ 새 시스템: 개선된 Animation Event 처리
    
    /// <summary>
    /// Animation Event에서 호출되는 데미지 적용 (완전 새로 구현)
    /// </summary>
    public void AttackHit()
    {
        Debug.Log($"[MeleeAttack] {gameObject.name} - Animation Event 데미지 적용!");
        
        // ⭐ 새 시스템: 데이터 기반 공격 범위 및 데미지 사용
        float currentRange = GetScaledRange();
        int currentDamage = GetScaledDamage();
        
        // ⭐ 새 시스템: 공격 원점 계산 (오프셋 적용)
        Vector2 attackOrigin = GetAttackOrigin();
        
        Debug.Log($"[MeleeAttack] 공격 실행 - 범위: {currentRange:F1}, 데미지: {currentDamage}, 원점: {attackOrigin}");
        
        // ⭐ 개선된 히트 감지 (각도 고려)
        List<Collider2D> hitTargets = GetHitTargets(attackOrigin, currentRange);
        
        foreach (Collider2D hitCollider in hitTargets)
        {
            PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // ⭐ 새 시스템: 크리티컬 판정
                bool isCritical = RollCriticalHit();
                int finalDamage = isCritical ? GetCriticalDamage(currentDamage) : currentDamage;
                
                // 데미지 적용
                playerHealth.TakeDamage(finalDamage, transform);
                
                // ⭐ 새 시스템: 상태이상 적용
                ApplyStatusEffects(playerHealth, hitCollider.transform);
                
                // ⭐ 새 시스템: 이펙트 및 사운드
                PlayHitEffectsAndSounds(hitCollider.transform.position, isCritical);
                
                Debug.Log($"[MeleeAttack] {gameObject.name}이 플레이어에게 {finalDamage} 데미지를 입혔습니다. {(isCritical ? "(크리티컬!)" : "")}");
                break; // 한 번에 하나의 플레이어만 타격
            }
        }
    }
    
    #endregion

    #region ⭐ 새 시스템: 근접 공격 전용 로직
    
    /// <summary>
    /// 공격 원점 계산 (오프셋 적용)
    /// </summary>
    private Vector2 GetAttackOrigin()
    {
        Vector2 basePosition = transform.position;
        
        // 몬스터가 바라보는 방향으로 오프셋 적용
        Vector2 forward = transform.right; // 또는 몬스터의 방향에 따라 조정
        Vector2 offsetPosition = basePosition + Vector2.Scale(attackOriginOffset, forward);
        
        return offsetPosition;
    }
    
    /// <summary>
    /// 히트 타겟 감지 (각도 고려)
    /// </summary>
    private List<Collider2D> GetHitTargets(Vector2 attackOrigin, float range)
    {
        List<Collider2D> validTargets = new List<Collider2D>();
        
        // 기본 원형 감지
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackOrigin, range, playerLayerMask);
        
        foreach (Collider2D collider in hitColliders)
        {
            // ⭐ 각도 제한 적용 (360도가 아닌 경우)
            if (IsWithinAttackAngle(attackOrigin, collider.transform.position))
            {
                validTargets.Add(collider);
            }
        }
        
        return validTargets;
    }
    
    /// <summary>
    /// 공격 각도 내에 있는지 확인
    /// </summary>
    private bool IsWithinAttackAngle(Vector2 attackOrigin, Vector2 targetPosition)
    {
        // ⭐ AttackData에서 각도 가져오기 (우선순위 적용)
        float currentAngle = GetEffectiveAttackAngle();
        
        // 360도면 각도 제한 없음
        if (Mathf.Approximately(currentAngle, 360f))
        {
            return true;
        }
        
        // 몬스터가 바라보는 방향
        Vector2 forward = transform.right; // 또는 몬스터의 실제 방향
        
        // 타겟 방향
        Vector2 toTarget = (targetPosition - attackOrigin).normalized;
        
        // 각도 계산
        float angle = Vector2.Angle(forward, toTarget);
        
        return angle <= currentAngle * 0.5f; // 양쪽으로 절반씩
    }
    
    /// <summary>
    /// 실제 사용할 공격 각도 가져오기 (우선순위: AttackData → Inspector 설정값)
    /// </summary>
    private float GetEffectiveAttackAngle()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.AttackAngle;
        }
        
        // 2순위: Inspector 하드코딩 값 (fallback)
        return attackAngle;
    }
    
    /// <summary>
    /// 크리티컬 히트 판정
    /// </summary>
    private bool RollCriticalHit()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.RollCritical();
        }
        
        // 2순위: 기본 크리티컬 확률 (5%)
        return Random.Range(0f, 1f) < 0.05f;
    }
    
    /// <summary>
    /// 크리티컬 데미지 계산
    /// </summary>
    private int GetCriticalDamage(int baseDamage)
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.GetCriticalDamage(baseDamage);
        }
        
        // 2순위: 기본 크리티컬 배율 (2배)
        return Mathf.RoundToInt(baseDamage * 2f);
    }
    
    /// <summary>
    /// 히트 이펙트 및 사운드 재생
    /// </summary>
    private void PlayHitEffectsAndSounds(Vector3 hitPosition, bool isCritical)
    {
        // 기본 히트 이펙트
        PlayHitEffect(hitPosition);
        
        // 크리티컬 추가 이펙트
        if (isCritical && criticalHitEffect != null)
        {
            GameObject critEffect = Instantiate(criticalHitEffect, hitPosition, Quaternion.identity);
            Debug.Log($"[MeleeAttack] 크리티컬 히트 이펙트 재생!");
        }
        
        // 히트 사운드
        PlayHitSound();
    }
    
    /// <summary>
    /// 근접 공격 설정 검증
    /// </summary>
    private void ValidateMeleeSettings()
    {
        if (AttackData != null)
        {
            Debug.Log($"[MeleeAttack] AttackData 기반 근접 공격 설정:");
            Debug.Log($"  - 공격명: {AttackData.AttackName}");
            Debug.Log($"  - 기본 데미지: {AttackData.BaseDamage} → 스케일된 데미지: {GetScaledDamage()}");
            Debug.Log($"  - 공격 범위: {AttackData.AttackRange}");
            Debug.Log($"  - 크리티컬 확률: {AttackData.CriticalChance * 100:F1}%");
            Debug.Log($"  - 상태이상 개수: {AttackData.OnHitEffects.Count}개");
            
            // 근접 공격에 맞지 않는 설정 경고
            if (AttackData.ProjectilePrefab != null)
            {
                Debug.LogWarning($"[MeleeAttack] 근접 공격인데 발사체가 설정되어 있습니다!");
            }
        }
        else
        {
            Debug.Log($"[MeleeAttack] 기존 방식 사용:");
            Debug.Log($"  - 데미지: {meleeDamage}");
            Debug.Log($"  - 범위: {attackRange}");
        }
    }
    
    #endregion

    #region 기존 시스템 호환성 유지
    
    /// <summary>
    /// 기존 공격 범위 반환 메서드 (호환성 유지)
    /// </summary>
    public float GetAttackRange()
    {
        // ⭐ 새 시스템 우선 사용
        return GetScaledRange();
    }
    
    #endregion

    #region ⭐ 디버그 및 시각화
    
    /// <summary>
    /// 기즈모 표시 (선택 시)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showAttackGizmos) return;
        
        Vector2 attackOrigin = GetAttackOrigin();
        float currentRange = GetScaledRange();
        float currentAngle = GetEffectiveAttackAngle(); // ⭐ AttackData 값 사용
        
        // 공격 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackOrigin, currentRange);
        
        // 공격 원점 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackOrigin, 0.1f);
        
        // 공격 각도 표시 (360도가 아닌 경우)
        if (!Mathf.Approximately(currentAngle, 360f)) // ⭐ currentAngle 사용
        {
            Gizmos.color = Color.blue;
            Vector3 forward = transform.right;
            Vector3 leftBound = Quaternion.Euler(0, 0, currentAngle * 0.5f) * forward; // ⭐ currentAngle 사용
            Vector3 rightBound = Quaternion.Euler(0, 0, -currentAngle * 0.5f) * forward; // ⭐ currentAngle 사용
            
            Gizmos.DrawRay(attackOrigin, leftBound * currentRange);
            Gizmos.DrawRay(attackOrigin, rightBound * currentRange);
        }
        
        // 오프셋 표시
        if (attackOriginOffset != Vector2.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, attackOrigin);
        }
    }
    
    /// <summary>
    /// 근접 공격 전용 디버그 정보
    /// </summary>
    [ContextMenu("Debug Melee Attack Info")]
    public void DebugMeleeAttackInfo()
    {
        string info = $"=== MeleeAttack {gameObject.name} ===\n";
        info += $"Current Damage: {GetScaledDamage()}\n";
        info += $"Current Range: {GetScaledRange():F1}\n";
        info += $"Current Cooldown: {GetScaledCooldown():F1}s\n";
        info += $"Attack Angle: {GetEffectiveAttackAngle()}도 (AttackData: {(AttackData?.AttackAngle ?? 0)}도, Fallback: {attackAngle}도)\n"; // ⭐ 상세 정보 표시
        info += $"Attack Origin: {GetAttackOrigin()}\n";
        
        if (AttackData != null)
        {
            info += "\n=== AttackData 정보 ===\n";
            info += AttackData.GetDebugInfo(BaseEnemy?.CurrentLevel ?? 1);
        }
        else
        {
            info += "\n=== Fallback 정보 ===\n";
            info += $"Melee Damage: {meleeDamage}\n";
            info += $"Attack Range: {attackRange}";
        }
        
        Debug.Log(info);
    }
    
    /// <summary>
    /// 크리티컬 테스트 (개발용)
    /// </summary>
    [ContextMenu("Test Critical Hit")]
    public void TestCriticalHit()
    {
        bool isCritical = RollCriticalHit();
        int baseDamage = GetScaledDamage();
        int finalDamage = isCritical ? GetCriticalDamage(baseDamage) : baseDamage;
        
        Debug.Log($"[MeleeAttack] 크리티컬 테스트:");
        Debug.Log($"  - 기본 데미지: {baseDamage}");
        Debug.Log($"  - 크리티컬 여부: {(isCritical ? "성공!" : "실패")}");
        Debug.Log($"  - 최종 데미지: {finalDamage}");
        
        if (AttackData != null)
        {
            Debug.Log($"  - 크리티컬 확률: {AttackData.CriticalChance * 100:F1}%");
            Debug.Log($"  - 크리티컬 배율: {AttackData.CriticalMultiplier}배");
        }
    }
    
    #endregion
}