using UnityEngine;

/// <summary>
/// Ghost 몬스터 - BaseEnemy 상속으로 중복 코드 제거
/// </summary>
public class Ghost : BaseEnemy
{
    // BaseEnemy 추상 속성들 구현
    public override float PatrolRadius => 5f; // 복합 공격 몬스터라 넓게
    
    public override float AttackRange 
    { 
        get 
        {
            MultiShotRangedAttack multiShotAttack = GetComponent<MultiShotRangedAttack>();
            return multiShotAttack != null ? 4f : 4f; // 복합 공격 범위
        } 
    }
    
    // 감지 범위는 MultiShotRangedAttack에서 가져오기
    public float DetectionRange
    {
        get
        {
            MultiShotRangedAttack multiShotAttack = GetComponent<MultiShotRangedAttack>();
            return multiShotAttack != null ? multiShotAttack.GetDetectionRange() : 7f;
        }
    }
    
    // 추격 범위는 MultiShotRangedAttack에서 가져오기
    public float ChaseRange
    {
        get
        {
            MultiShotRangedAttack multiShotAttack = GetComponent<MultiShotRangedAttack>();
            return multiShotAttack != null ? multiShotAttack.GetChaseRange() : 10f;
        }
    }
    // BaseEnemy 추상 메서드들 구현
    protected override void OnAwakeInitialize()
    {
        // Ghost 전용 Awake 초기화 (현재는 없음)
    }

    protected override void OnStartInitialize()
    {
        // Ghost 전용 Start 초기화 (현재는 없음)
    }

    protected override void InitializeAttackSystem()
    {
        MultiShotRangedAttack multiShotAttack = GetComponent<MultiShotRangedAttack>();
        if (multiShotAttack != null)
        {
            multiShotAttack.Initialize();
            Debug.Log($"[Ghost] {gameObject.name} 복합 원거리 공격 시스템 초기화 완료");
        }
        else
        {
            Debug.LogError($"[Ghost] {gameObject.name}에 MultiShotRangedAttack 컴포넌트가 없습니다!");
        }
    }

    public override void Attack()
    {
        MultiShotRangedAttack multiShotAttack = GetComponent<MultiShotRangedAttack>();
        if (multiShotAttack != null)
        {
            if (multiShotAttack.CanAttack())
            {
                multiShotAttack.Attack();
                Debug.Log($"[Ghost] {gameObject.name} - 복합 원거리 공격 실행!");
            }
            else
            {
                Debug.Log($"[Ghost] {gameObject.name} - 공격 쿨다운 중...");
            }
        }
        else
        {
            Debug.LogWarning($"[Ghost] {gameObject.name}에 MultiShotRangedAttack 컴포넌트가 없습니다.");
        }
    }

    /// <summary>
    /// Animation Event에서 호출 - MultiShotRangedAttack에 위임
    /// </summary>
    public void SpawnProjectileAnimEvent()
    {
        MultiShotRangedAttack multiShotAttack = GetComponent<MultiShotRangedAttack>();
        if (multiShotAttack != null)
        {
            multiShotAttack.SpawnProjectileAnimEvent();
            Debug.Log($"[Ghost] {gameObject.name} - Animation Event 발사체 생성 위임");
        }
        else
        {
            Debug.LogWarning($"[Ghost] {gameObject.name}에 MultiShotRangedAttack 컴포넌트가 없습니다.");
        }
    }
} 