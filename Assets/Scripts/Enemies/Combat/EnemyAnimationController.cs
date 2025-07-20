using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    // ❌ 제거: 존재하지 않는 파라미터들
    // public void PlayIdle()    => animator.SetBool("isIdle", true);
    // public void PlayWalk()    => animator.SetBool("isWalking", true);

    // ✅ 실제 동작하는 메서드들 (로그 추가)
    public void PlayAttack()  
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
            Debug.Log($"[EnemyAnimationController] {gameObject.name} - Attack 트리거 실행 성공!");
            
            // 🔍 추가 디버깅: Animator Controller 정보
            if (animator.runtimeAnimatorController != null)
            {
                Debug.Log($"[EnemyAnimationController] {gameObject.name} - 사용 중인 Controller: {animator.runtimeAnimatorController.name}");
            }
            else
            {
                Debug.LogError($"[EnemyAnimationController] {gameObject.name} - Animator Controller가 없습니다!");
            }
        }
        else
        {
            Debug.LogError($"[EnemyAnimationController] {gameObject.name} - Animator가 null입니다!");
        }
    }
    
    public void PlayHit()     
    { 
        animator.SetTrigger("Hit");
        Debug.Log($"[EnemyAnimationController] {gameObject.name} - Hit 트리거 실행!");
    }
    
    public void PlayDie()     
    { 
        animator.SetTrigger("Die");
        Debug.Log($"[EnemyAnimationController] {gameObject.name} - Die 트리거 실행!");
    }
    
    // 🔄 Walking은 모든 몬스터의 기본 상태이므로 별도 트리거 불필요
    // 상태 전환 후 자동으로 Walking으로 복귀됨
}
