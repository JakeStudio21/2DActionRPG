using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 공격 시스템의 기본 클래스 - 공통 기능 템플릿화
/// MeleeAttack, RangedAttack, MultiShotRangedAttack의 중복 코드 제거
/// ⭐ [Phase 2] AttackData 기반 확장 지원 (Optional)
/// </summary>
public abstract class BaseAttackBehaviour : MonoBehaviour, IAttackBehaviour
{
    #region ⭐ 새 시스템: AttackData 기반 데이터 (Optional)
    
    [Header("📊 새 공격 데이터 시스템 (Optional)")]
    [Tooltip("AttackData ScriptableObject 참조 (없으면 기존 방식 사용)")]
    [SerializeField] protected AttackData attackData;
    
    // 런타임 계산된 값들 (캐싱용)
    private int? cachedScaledDamage;
    private float? cachedScaledCooldown;
    private float? cachedScaledRange;
    
    // 상태이상 관리
    private List<StatusEffectData> pendingStatusEffects = new List<StatusEffectData>();
    
    #endregion

    #region 기존 시스템 (100% 유지)
    
    [Header("Base Attack Settings")]
    [SerializeField] protected float attackCooldown = 2f;
    [SerializeField] protected float detectionRange = 5f;
    [SerializeField] protected float chaseRange = 8f;
    [SerializeField] protected bool stopMovingWhileAttacking = true;
    [SerializeField] protected AudioClip attackSound;
    
    // 공통 컴포넌트들
    protected Animator animator;
    protected AudioSource audioSource;
    protected EnemyAnimationController animationController;
    protected PlayerController cachedPlayer;
    protected BaseEnemy baseEnemy; // ⭐ BaseEnemy 참조 추가
    
    // 공통 상태 변수
    protected bool canAttack = true;
    
    #endregion

    #region ⭐ 새 시스템: Public Properties (데이터 우선순위 적용)
    
    /// <summary>
    /// AttackData 참조 (읽기 전용)
    /// </summary>
    public AttackData AttackData => attackData;
    
    /// <summary>
    /// BaseEnemy 참조 (읽기 전용)
    /// </summary>
    public BaseEnemy BaseEnemy => baseEnemy;
    
    #endregion

    #region Unity 생명주기 및 초기화 (기존 유지 + 확장)

    public virtual void Initialize()
    {
        // 공통 초기화
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        animationController = GetComponent<EnemyAnimationController>();
        baseEnemy = GetComponent<BaseEnemy>(); // ⭐ BaseEnemy 참조 획득
        canAttack = true;
        
        // ⭐ 새 시스템: 데이터 기반 초기화
        InitializeAttackDataSystem();
        
        // 플레이어 찾기
        StartCoroutine(FindPlayerCoroutine());
        
        // 하위 클래스별 초기화
        OnInitialize();
        
        Debug.Log($"[{GetType().Name}] {gameObject.name} 공격 시스템 초기화 완료");
    }

    /// <summary>
    /// 공격 데이터 시스템 초기화
    /// </summary>
    private void InitializeAttackDataSystem()
    {
        if (attackData != null)
        {
            Debug.Log($"[{GetType().Name}] {gameObject.name} - AttackData 시스템 활성화: {attackData.AttackName}");
            
            // ⭐ GrowthProfile 기반 디버그 정보
            if (baseEnemy != null && baseEnemy.GrowthProfile != null)
            {
                Debug.Log($"[{GetType().Name}] {attackData.GetDebugInfoWithProfile(GetCurrentLevel(), baseEnemy.GrowthProfile, baseEnemy.EnemyData?.EnemyType ?? EnemyType.Basic)}");
            }
            else
            {
                Debug.Log($"[{GetType().Name}] {attackData.GetDebugInfo(GetCurrentLevel())}");
            }
            
            // 공격 타입 검증
            ValidateAttackType();
        }
        else
        {
            Debug.Log($"[{GetType().Name}] {gameObject.name} - 기존 방식 사용 (AttackData 없음)");
        }
    }
    
    /// <summary>
    /// 공격 타입 검증 (추상 메서드로 하위 클래스에서 구현)
    /// </summary>
    protected abstract void ValidateAttackType();
    
    #endregion

    #region ⭐ 새 시스템: 데이터 기반 스탯 계산
    
    /// <summary>
    /// 현재 몬스터 레벨 가져오기
    /// </summary>
    private int GetCurrentLevel()
    {
        return baseEnemy != null ? baseEnemy.CurrentLevel : 1;
    }
    
    /// <summary>
    /// 스케일된 공격 데미지 반환 (AttackData 필수)
    /// ⭐ MonsterGrowthProfile 기반 데미지 계산
    /// </summary>
    public virtual int GetScaledDamage()
    {
        // 캐시된 값이 있으면 반환
        if (cachedScaledDamage.HasValue) return cachedScaledDamage.Value;
        
        int level = GetCurrentLevel();
        
        // AttackData 없으면 명확한 에러!
        if (attackData == null) 
        {
            Debug.LogError($"[{GetType().Name}] {gameObject.name}에 AttackData가 할당되지 않았습니다! " +
                          "Inspector에서 AttackData ScriptableObject를 할당해주세요.");
            return 1; // 크래시 방지용 최소값
        }

        // ⭐ BaseEnemy 검증 (null 체크)
        if (baseEnemy == null)
        {
            Debug.LogError($"[{GetType().Name}] {gameObject.name}에 BaseEnemy 참조가 없습니다!");
            return attackData.BaseDamage; // fallback
        }

        // ⭐ MonsterGrowthProfile 기반 데미지 계산
        MonsterGrowthProfile growthProfile = baseEnemy.GrowthProfile;
        EnemyType enemyType = baseEnemy.EnemyData?.EnemyType ?? EnemyType.Basic;
        
        int scaledDamage = attackData.GetScaledDamage(level, growthProfile, enemyType);
        cachedScaledDamage = scaledDamage;
        return scaledDamage;
    }
    
    /// <summary>
    /// 스케일된 공격 쿨다운 반환 (데이터 우선순위 적용)
    /// </summary>
    public virtual float GetScaledCooldown()
    {
        if (cachedScaledCooldown.HasValue) return cachedScaledCooldown.Value;
        
        float cooldown = 0f;
        
        // 1순위: AttackData
        if (attackData != null)
        {
            cooldown = attackData.AttackCooldown;
        }
        else
        {
            // 2순위: Inspector 설정 (기존 방식)
            cooldown = attackCooldown;
        }
        
        cachedScaledCooldown = cooldown;
        return cachedScaledCooldown.Value;
    }
    
    /// <summary>
    /// 스케일된 공격 범위 반환 (데이터 우선순위 적용)
    /// </summary>
    public virtual float GetScaledRange()
    {
        if (cachedScaledRange.HasValue) return cachedScaledRange.Value;
        
        float range = 0f;
        
        // 1순위: AttackData
        if (attackData != null)
        {
            range = attackData.AttackRange;
        }
        else
        {
            // 2순위: 하위 클래스 fallback
            range = GetFallbackRange();
        }
        
        cachedScaledRange = range;
        return cachedScaledRange.Value;
    }
    
    /// <summary>
    /// 스탯 캐시 무효화 (레벨 변경 시 호출)
    /// ⭐ Public으로 변경: BaseEnemy에서 호출 가능
    /// </summary>
    public void InvalidateAttackCache()
    {
        cachedScaledDamage = null;
        cachedScaledCooldown = null;
        cachedScaledRange = null;
    }
    
    #endregion

    #region ⭐ 새 시스템: 상태이상 관리
    
    /// <summary>
    /// 상태이상 효과 적용
    /// </summary>
    protected void ApplyStatusEffects(PlayerHealth targetHealth, Transform targetTransform)
    {
        if (attackData == null || targetHealth == null) return;
        
        // AttackData에서 상태이상 롤
        List<StatusEffectData> appliedEffects = attackData.RollStatusEffects();
        
        foreach (StatusEffectData effect in appliedEffects)
        {
            if (effect != null)
            {
                // 상태이상 적용 (실제 구현은 StatusEffectManager에서)
                ApplySingleStatusEffect(effect, targetHealth, targetTransform);
                Debug.Log($"[{GetType().Name}] {gameObject.name}이 {effect.EffectName} 상태이상을 적용했습니다!");
            }
        }
    }
    
    /// <summary>
    /// 개별 상태이상 적용 (하위 클래스에서 오버라이드 가능)
    /// </summary>
    protected virtual void ApplySingleStatusEffect(StatusEffectData effect, PlayerHealth targetHealth, Transform targetTransform)
    {
        // ⭐ 새 시스템: StatusEffectManager와 연동하여 실제 상태이상 적용
        if (StatusEffectManager.Instance != null)
        {
            StatusEffectManager.Instance.ApplyStatusEffect(effect);
            Debug.Log($"[{GetType().Name}] {effect.EffectName} 상태이상 적용! - {effect.Description}");
        }
        else
        {
            Debug.LogWarning($"[{GetType().Name}] StatusEffectManager가 없어서 상태이상 적용 불가: {effect.EffectName}");
        }
    }
    
    #endregion

    #region ⭐ 새 시스템: 하위 클래스에서 구현해야 하는 fallback 메서드들
    
    /// <summary>
    /// AttackData가 없을 때 사용할 기본 데미지 (하위 클래스에서 구현)
    /// </summary>
    protected virtual int GetFallbackDamage() => 1;
    
    /// <summary>
    /// AttackData가 없을 때 사용할 기본 공격 범위 (하위 클래스에서 구현)
    /// </summary>
    protected virtual float GetFallbackRange() => 1f;
    
    #endregion

    #region 기존 시스템 메서드들 (100% 유지 + 확장)

    public virtual void Attack()
    {
        if (!CanAttack()) return;
        
        canAttack = false;
        
        // ⭐ 공격 방향 설정 (플레이어 방향으로)
        UpdateAttackDirectionTowardsPlayer();
        
        // ⭐ 새 시스템: 데이터 기반 애니메이션 트리거
        TriggerAttackAnimation();
        
        // ⭐ 새 시스템: 데이터 기반 사운드 재생
        PlayAttackSound();
        
        // ⭐ 새 시스템: 공격 시작 이펙트
        PlayAttackStartEffect();
        
        // 하위 클래스별 공격 로직
        OnAttack();
        
        Debug.Log($"[{GetType().Name}] {gameObject.name} 공격 실행! (데미지: {GetScaledDamage()})");
        
        // ⭐ 새 시스템: 데이터 기반 쿨다운
        StartCoroutine(AttackCooldownRoutine(GetScaledCooldown()));
    }
    
    /// <summary>
    /// ⭐ 신규 추가: 플레이어 방향으로 공격 방향 설정
    /// </summary>
    protected virtual void UpdateAttackDirectionTowardsPlayer()
    {
        if (animationController == null)
        {
            Debug.LogWarning($"❌ [{GetType().Name}] {gameObject.name} - AnimationController가 없습니다!");
            return;
        }
        
        if (cachedPlayer == null)
        {
            Debug.LogWarning($"❌ [{GetType().Name}] {gameObject.name} - cachedPlayer가 없습니다!");
            return;
        }
        
        // 플레이어를 향하는 방향 계산 (월드 좌표계)
        Vector2 toPlayerWorld = (cachedPlayer.transform.position - transform.position).normalized;
        
        // ⭐ 임시: 변환 없이 월드 좌표 그대로 사용 (테스트용)
        Vector2 toPlayerBlendTree = toPlayerWorld;
        
        // ⭐ 강제 디버그 로그 (항상 출력)
        Debug.Log($"🎯 [{GetType().Name}] {gameObject.name} - 공격 방향 설정:");
        Debug.Log($"   플레이어 위치: {cachedPlayer.transform.position}");
        Debug.Log($"   몬스터 위치: {transform.position}");
        Debug.Log($"   월드 좌표 방향: ({toPlayerWorld.x:F2}, {toPlayerWorld.y:F2})");
        Debug.Log($"   BlendTree 방향: ({toPlayerBlendTree.x:F2}, {toPlayerBlendTree.y:F2})");
        
        // ⭐ 월드 좌표계 기반 flipX 결정
        bool shouldFlipX = toPlayerWorld.x < 0;
        
        // 애니메이션 컨트롤러에 방향 + flipX 전달
        animationController.UpdateAttackDirectionWithFlip(toPlayerBlendTree, shouldFlipX);
    }
    
    /// <summary>
    /// ⭐ 개선된 애니메이션 트리거 (데이터 우선순위)
    /// </summary>
    protected virtual void TriggerAttackAnimation()
    {
        string triggerName = "Attack"; // 기본값
        
        // AttackData에서 트리거명 가져오기
        if (attackData != null && !string.IsNullOrEmpty(attackData.AnimationTrigger))
        {
            triggerName = attackData.AnimationTrigger;
        }
        
        if (animationController != null)
        {
            animationController.PlayAttack();
        }
        else if (animator != null)
        {
            animator.SetTrigger(triggerName);
            Debug.Log($"[{GetType().Name}] 애니메이션 트리거: {triggerName}");
        }
    }
    
    /// <summary>
    /// ⭐ 개선된 사운드 재생 (데이터 우선순위)
    /// </summary>
    protected virtual void PlayAttackSound()
    {
        AudioClip soundToPlay = null;
        
        // 1순위: AttackData
        if (attackData != null && attackData.AttackSound != null)
        {
            soundToPlay = attackData.AttackSound;
        }
        // 2순위: Inspector 설정 (기존 방식)
        else if (attackSound != null)
        {
            soundToPlay = attackSound;
        }
        
        if (audioSource != null && soundToPlay != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }
    }
    
    /// <summary>
    /// ⭐ 새 기능: 공격 시작 이펙트 재생
    /// </summary>
    protected virtual void PlayAttackStartEffect()
    {
        if (attackData != null && attackData.AttackStartEffect != null)
        {
            GameObject effect = Instantiate(attackData.AttackStartEffect, transform.position, transform.rotation);
            Debug.Log($"[{GetType().Name}] 공격 시작 이펙트 재생: {attackData.AttackStartEffect.name}");
        }
    }
    
    /// <summary>
    /// ⭐ 개선된 히트 이펙트 재생
    /// </summary>
    protected virtual void PlayHitEffect(Vector3 hitPosition)
    {
        GameObject effectToPlay = null;
        
        // 1순위: AttackData
        if (attackData != null && attackData.HitEffect != null)
        {
            effectToPlay = attackData.HitEffect;
        }
        
        if (effectToPlay != null)
        {
            GameObject effect = Instantiate(effectToPlay, hitPosition, Quaternion.identity);
            Debug.Log($"[{GetType().Name}] 히트 이펙트 재생: {effectToPlay.name}");
        }
    }
    
    /// <summary>
    /// ⭐ 개선된 히트 사운드 재생
    /// </summary>
    protected virtual void PlayHitSound()
    {
        if (attackData != null && attackData.HitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackData.HitSound);
        }
    }
    
    // 공통 인터페이스 구현 (기존 유지)
    public virtual bool CanAttack() => canAttack;
    
    /// <summary>
    /// ⭐ 개선된 이동 정지 여부 (데이터 우선순위)
    /// </summary>
    public virtual bool ShouldStopMovingWhileAttacking()
    {
        // 1순위: AttackData
        if (attackData != null)
        {
            return attackData.StopMovingWhileAttacking;
        }
        // 2순위: Inspector 설정 (기존 방식)
        return stopMovingWhileAttacking;
    }
    
    // 공통 유틸리티 메서드들 (기존 유지)
    public float GetDetectionRange() => detectionRange;
    public float GetChaseRange() => chaseRange;
    
    /// <summary>
    /// 공통 플레이어 찾기 코루틴 (기존 유지)
    /// </summary>
    protected IEnumerator FindPlayerCoroutine()
    {
        while (cachedPlayer == null)
        {
            cachedPlayer = FindObjectOfType<PlayerController>();
            if (cachedPlayer == null)
                yield return new WaitForSeconds(0.1f);
        }
        Debug.Log($"[{GetType().Name}] {gameObject.name}이 플레이어를 찾았습니다.");
    }
    
    /// <summary>
    /// 공통 쿨다운 코루틴 (기존 유지)
    /// </summary>
    protected IEnumerator AttackCooldownRoutine(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        canAttack = true;
    }
    
    // 추상 메서드들 - 하위 클래스에서 구현 필수 (기존 유지)
    protected abstract void OnInitialize();
    protected abstract void OnAttack();
    
    #endregion

    #region ⭐ 디버그 및 에디터 지원
    
    /// <summary>
    /// Inspector에서 값 변경 시 캐시 무효화
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            InvalidateAttackCache();
        }
    }
    
    /// <summary>
    /// 현재 공격 시스템 상태 디버그 정보
    /// </summary>
    [ContextMenu("Debug Attack Info")]
    public void DebugAttackInfo()
    {
        string info = $"=== {GetType().Name} {gameObject.name} Debug Info ===\n";
        
        if (attackData != null)
        {
            info += $"AttackData: {attackData.name}\n";
            
            // ⭐ GrowthProfile 기반 디버그 정보
            if (baseEnemy != null && baseEnemy.GrowthProfile != null)
            {
                info += attackData.GetDebugInfoWithProfile(GetCurrentLevel(), baseEnemy.GrowthProfile, baseEnemy.EnemyData?.EnemyType ?? EnemyType.Basic) + "\n";
            }
            else
            {
                info += attackData.GetDebugInfo(GetCurrentLevel()) + "\n";
            }
            
            info += $"Scaled Stats (Runtime):\n";
            info += $"  Damage: {GetScaledDamage()}\n";
            info += $"  Cooldown: {GetScaledCooldown():F1}s\n";
            info += $"  Range: {GetScaledRange():F1}\n";
        }
        else
        {
            info += "기존 방식 사용 (AttackData 없음)\n";
            info += $"Fallback Stats:\n";
            info += $"  Damage: {GetFallbackDamage()}\n";
            info += $"  Cooldown: {attackCooldown}\n";
            info += $"  Range: {GetFallbackRange()}\n";
        }
        
        Debug.Log(info);
    }
    
    #endregion
} 