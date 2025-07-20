using System.Collections;
using UnityEngine;

/// <summary>
/// 모든 공격 시스템의 기본 클래스 - 공통 기능 템플릿화
/// MeleeAttack, RangedAttack, MultiShotRangedAttack의 중복 코드 제거
/// </summary>
public abstract class BaseAttackBehaviour : MonoBehaviour, IAttackBehaviour
{
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
    
    // 공통 상태 변수
    protected bool canAttack = true;
    
    public virtual void Initialize()
    {
        // 공통 초기화
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        animationController = GetComponent<EnemyAnimationController>();
        canAttack = true;
        
        // 플레이어 찾기
        StartCoroutine(FindPlayerCoroutine());
        
        // 하위 클래스별 초기화
        OnInitialize();
        
        Debug.Log($"[{GetType().Name}] {gameObject.name} 공격 시스템 초기화 완료");
    }
    
    public virtual void Attack()
    {
        if (!CanAttack()) return;
        
        canAttack = false;
        
        // 공통 애니메이션 트리거
        TriggerAttackAnimation();
        
        // 공통 사운드 재생
        PlayAttackSound();
        
        // 하위 클래스별 공격 로직
        OnAttack();
        
        Debug.Log($"[{GetType().Name}] {gameObject.name} 공격 실행!");
        
        // 공통 쿨다운
        StartCoroutine(AttackCooldownRoutine(attackCooldown));
    }
    
    // 공통 인터페이스 구현
    public virtual bool CanAttack() => canAttack;
    public virtual bool ShouldStopMovingWhileAttacking() => stopMovingWhileAttacking;
    
    // 공통 유틸리티 메서드들
    public float GetDetectionRange() => detectionRange;
    public float GetChaseRange() => chaseRange;
    
    /// <summary>
    /// 공통 플레이어 찾기 코루틴
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
    /// 공통 애니메이션 트리거
    /// </summary>
    protected virtual void TriggerAttackAnimation()
    {
        if (animationController != null)
        {
            animationController.PlayAttack();
        }
        else if (animator != null)
        {
            animator.SetTrigger("Attack");
            Debug.LogWarning($"[{GetType().Name}] {gameObject.name} - EnemyAnimationController가 없어 직접 Animator 사용");
        }
    }
    
    /// <summary>
    /// 공통 사운드 재생
    /// </summary>
    protected virtual void PlayAttackSound()
    {
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }
    
    /// <summary>
    /// 공통 쿨다운 코루틴
    /// </summary>
    protected IEnumerator AttackCooldownRoutine(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        canAttack = true;
    }
    
    // 추상 메서드들 - 하위 클래스에서 구현 필수
    protected abstract void OnInitialize();
    protected abstract void OnAttack();
} 