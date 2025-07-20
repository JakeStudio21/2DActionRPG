using UnityEngine;

/// <summary>
/// Grape 몬스터 - BaseEnemy 상속으로 중복 코드 제거
/// </summary>
public class Grape : BaseEnemy
{
    // BaseEnemy 추상 속성들 구현
    public override float PatrolRadius => 4f; // 원거리 몬스터라 조금 더 넓게
    
    public override float AttackRange 
    { 
        get 
        {
            RangedAttack rangedAttack = GetComponent<RangedAttack>();
            return rangedAttack != null ? 3.5f : 3.5f; // 원거리 공격 범위
        } 
    }
    
    public float ChaseRange 
    { 
        get 
        {
            RangedAttack rangedAttack = GetComponent<RangedAttack>();
            return rangedAttack != null ? rangedAttack.GetChaseRange() : 8f;
        } 
    }

    // BaseEnemy 추상 메서드들 구현
    protected override void OnAwakeInitialize()
    {
        // Grape 전용 Awake 초기화 (현재는 없음)
    }

    protected override void OnStartInitialize()
    {
        // Grape 전용 Start 초기화 (현재는 없음)
    }

    protected override void InitializeAttackSystem()
    {
        RangedAttack rangedAttack = GetComponent<RangedAttack>();
        if (rangedAttack != null)
        {
            rangedAttack.Initialize();
            Debug.Log($"[Grape] {gameObject.name} 원거리 공격 시스템 초기화 완료");
        }
        else
        {
            Debug.LogError($"[Grape] {gameObject.name}에 RangedAttack 컴포넌트가 없습니다!");
        }
    }

    public override void Attack()
    {
        RangedAttack rangedAttack = GetComponent<RangedAttack>();
        if (rangedAttack != null)
        {
            if (rangedAttack.CanAttack())
            {
                rangedAttack.Attack();
                Debug.Log($"[Grape] {gameObject.name} - 원거리 공격 실행!");
            }
            else
            {
                Debug.Log($"[Grape] {gameObject.name} - 공격 쿨다운 중...");
            }
        }
        else
        {
            Debug.LogWarning($"[Grape] {gameObject.name}에 RangedAttack 컴포넌트가 없습니다.");
        }
    }
} 