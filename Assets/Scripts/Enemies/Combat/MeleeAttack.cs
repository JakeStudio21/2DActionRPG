using UnityEngine;

/// <summary>
/// 근접 공격 구현체 - BaseAttackBehaviour 상속으로 중복 코드 제거
/// </summary>
public class MeleeAttack : BaseAttackBehaviour
{
    [Header("Melee Specific Settings")]
    [SerializeField] private int meleeDamage = 1;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private LayerMask playerLayerMask = 1 << 3;
    
    // BaseAttackBehaviour 추상 메서드 구현
    protected override void OnInitialize()
    {
        // MeleeAttack 전용 초기화 (현재는 없음)
    }

    protected override void OnAttack()
    {
        // 근접 공격 전용 로직 (현재는 애니메이션 이벤트에서 처리)
    }
    
    /// <summary>
    /// Animation Event에서 호출되는 데미지 적용
    /// </summary>
    public void AttackHit()
    {
        Debug.Log($"[MeleeAttack] {gameObject.name} - Animation Event 데미지 적용!");
        
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackRange, playerLayerMask);
        
        foreach (Collider2D hitCollider in hitColliders)
        {
            PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(meleeDamage, transform);
                Debug.Log($"[MeleeAttack] {gameObject.name}이 플레이어에게 {meleeDamage} 데미지를 입혔습니다.");
                break;
            }
        }
    }
    
    // 추가 유틸리티 메서드들
    public float GetAttackRange() => attackRange;
}