using System.Collections;
using UnityEngine;

/// <summary>
/// 근접 공격 구현체 (BlueSlime용)
/// MonoBehaviour 컴포넌트로 구현하여 Inspector에서 설정 가능
/// </summary>
public class MeleeAttack : MonoBehaviour, IAttackBehaviour
{
    [Header("Melee Attack Settings")]
    [SerializeField] private int meleeDamage = 1;
    [SerializeField] private bool stopMovingWhileAttacking = true;
    [SerializeField] private AudioClip attackSound;
    
    private EnemyAI cachedEnemyAI;
    private Animator animator;
    private AudioSource audioSource;
    private bool canAttack = true;
    
    readonly int ATTACK_HASH = Animator.StringToHash("Attack");
    
    public void Initialize(EnemyAI enemyAI)
    {
        cachedEnemyAI = enemyAI;
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        canAttack = true;
        
        Debug.Log($"[MeleeAttack] {enemyAI.gameObject.name} 근접 공격 시스템 초기화 완료");
    }
    
    public void Attack(EnemyAI enemyAI)
    {
        if (!CanAttack()) return;
        
        canAttack = false;
        
        // 애니메이션 트리거
        if (animator != null)
        {
            animator.SetTrigger(ATTACK_HASH);
        }
        
        // 공격 사운드 재생
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
        
        Debug.Log($"[MeleeAttack] {enemyAI.gameObject.name} 근접 공격 실행!");
        
        // 공격 쿨다운 시작
        StartCoroutine(AttackCooldownRoutine(enemyAI.GetAttackCooldown()));
    }
    
    public bool CanAttack()
    {
        return canAttack;
    }
    
    public bool ShouldStopMovingWhileAttacking()
    {
        return stopMovingWhileAttacking;
    }
    
    private IEnumerator AttackCooldownRoutine(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        canAttack = true;
    }
    
    /// <summary>
    /// Animation Event에서 호출되는 실제 데미지 처리
    /// 기존 EnemyAI.OnAttackHit() 메서드를 대체
    /// </summary>
    public void AttackHit()
    {
        Debug.Log($"[MeleeAttack] {gameObject.name} - Animation Event 데미지 적용!");
        
        var cachedPlayer = cachedEnemyAI.GetCachedPlayer();
        if (cachedPlayer != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, cachedPlayer.transform.position);
            float attackRange = cachedEnemyAI.GetAttackRange();
            
            if (distanceToPlayer <= attackRange)
            {
                PlayerHealth playerHealth = cachedPlayer.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(meleeDamage, transform);
                    Debug.Log($"[MeleeAttack] {gameObject.name}이 플레이어에게 {meleeDamage} 데미지를 입혔습니다. (Animation Event)");
                }
            }
            else
            {
                Debug.Log($"[MeleeAttack] {gameObject.name} - 공격 히트 시점에 플레이어가 범위를 벗어났습니다. (거리: {distanceToPlayer:F2})");
            }
        }
    }
} 