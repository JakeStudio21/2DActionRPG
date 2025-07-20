using UnityEngine;

/// <summary>
/// BlueSlime 몬스터 - BaseEnemy 상속으로 중복 코드 제거
/// </summary>
public class BlueSlime : BaseEnemy
{
    // BaseEnemy 추상 속성들 구현
    public override float PatrolRadius => 3f;
    
    public override float AttackRange 
    { 
        get 
        {
            MeleeAttack meleeAttack = GetComponent<MeleeAttack>();
            return meleeAttack != null ? meleeAttack.GetAttackRange() : 1f;
        } 
    }
    
    public float ChaseRange 
    { 
        get 
        {
            MeleeAttack meleeAttack = GetComponent<MeleeAttack>();
            return meleeAttack != null ? meleeAttack.GetChaseRange() : 7f;
        } 
    }

    // BaseEnemy 추상 메서드들 구현
    protected override void OnAwakeInitialize()
    {
        // BlueSlime 전용 Awake 초기화 (현재는 없음)
    }

    protected override void OnStartInitialize()
    {
        // BlueSlime 전용 Start 초기화 (현재는 없음)
    }

    protected override void InitializeAttackSystem()
    {
        MeleeAttack meleeAttack = GetComponent<MeleeAttack>();
        if (meleeAttack != null)
        {
            meleeAttack.Initialize();
            Debug.Log($"[BlueSlime] {gameObject.name} 근접 공격 시스템 초기화 완료");
        }
        else
        {
            Debug.LogError($"[BlueSlime] {gameObject.name}에 MeleeAttack 컴포넌트가 없습니다!");
        }
    }

    public override void Attack()
    {
        MeleeAttack meleeAttack = GetComponent<MeleeAttack>();
        if (meleeAttack != null)
        {
            if (meleeAttack.CanAttack())
            {
                meleeAttack.Attack();
                Debug.Log($"[BlueSlime] {gameObject.name} - 공격 실행!");
            }
            else
            {
                Debug.Log($"[BlueSlime] {gameObject.name} - 공격 쿨다운 중...");
            }
        }
    }
} 