using UnityEngine;

/// <summary>
/// Grape 몬스터 클래스 - 원거리 공격 특화
/// ⭐ [Complete Reset] 완전한 데이터 기반 시스템
/// </summary>
public class Grape : BaseEnemy
{
    [Header("⭐ Grape 전용 컴포넌트")]
    [SerializeField] private RangedAttack rangedAttack;

    #region ⭐ BaseEnemy 추상 속성 구현 - 완전한 데이터 기반

    public override float PatrolRadius 
    { 
        get 
        {
            if (EnemyData != null)
                return EnemyData.PatrolRadius;
            
            Debug.LogError($"[Grape] {gameObject.name}: EnemyData가 없어서 PatrolRadius 확인 불가!");
            return 4f; // 최소 안전값
        }
    }
    
    public override float AttackRange 
    { 
        get 
        {
            // 1순위: AttackData
            if (rangedAttack != null && rangedAttack.AttackData != null)
                return rangedAttack.AttackData.AttackRange;
                
            Debug.LogError($"[Grape] {gameObject.name}: RangedAttack 또는 AttackData가 없습니다!");
            return 3.5f; // 최소 안전값
        } 
    }
    
    public float ChaseRange 
    { 
        get 
        {
            if (EnemyData != null)
                return EnemyData.ChaseRange;
                
            Debug.LogError($"[Grape] {gameObject.name}: EnemyData가 없어서 ChaseRange 확인 불가!");
            return 8f; // 최소 안전값
        } 
    }

    #endregion

    #region ⭐ BaseEnemy 추상 메서드 구현

    protected override void OnAwakeInitialize()
    {
        // RangedAttack 컴포넌트 확인
        if (rangedAttack == null)
            rangedAttack = GetComponent<RangedAttack>();
            
        Debug.Log($"[Grape] {gameObject.name} Awake 초기화 완료");
    }

    protected override void OnStartInitialize()
    {
        ApplyGrapeSpecificSettings();
        Debug.Log($"[Grape] {gameObject.name} Start 초기화 완료");
    }

    protected override void InitializeAttackSystem()
    {
        if (rangedAttack != null)
        {
            rangedAttack.Initialize(); // ⭐ 수정: 매개변수 제거
            Debug.Log($"[Grape] {gameObject.name} RangedAttack 시스템 초기화 완료");
        }
        else
        {
            Debug.LogError($"[Grape] {gameObject.name}: RangedAttack 컴포넌트가 없습니다!");
        }
    }

    public override void Attack()
    {
        if (rangedAttack != null && rangedAttack.CanAttack())
        {
            rangedAttack.Attack();
            Debug.Log($"[Grape] {gameObject.name} 원거리 공격 실행!");
        }
    }

    #endregion

    #region ⭐ Grape 전용 설정

    /// <summary>
    /// Grape 전용 설정 적용
    /// </summary>
    private void ApplyGrapeSpecificSettings()
    {
        if (EnemyData != null)
        {
            Debug.Log($"[Grape] {gameObject.name} 전용 설정 적용:");
            Debug.Log($"  - 타입: {EnemyData.EnemyType}");
            Debug.Log($"  - 레벨: {CurrentLevel}");
            Debug.Log($"  - 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"  - 공격력: AttackData에서 관리됨");
            Debug.Log($"  - 방어력: {GetScaledDefense():F1}");
            Debug.Log($"  - 이동속도: {GetScaledMoveSpeed():F1}");
        }
    }

    /// <summary>
    /// 실제 발사체 데미지 테스트 (AttackData 기반)
    /// </summary>
    [ContextMenu("Test Projectile Damage")]
    private void TestProjectileDamage()
    {
        if (rangedAttack != null)
        {
            int actualDamage = rangedAttack.GetScaledDamage();
            Debug.Log($"[Grape] 실제 발사체 데미지: {actualDamage}");
        }
        else
        {
            Debug.LogError($"[Grape] RangedAttack 컴포넌트가 없어서 데미지 테스트 불가!");
        }
    }

    /// <summary>
    /// Elite 타입으로 강제 변경 (테스트용)
    /// </summary>
    [ContextMenu("Force Elite Type")]
    private void ForceEliteType()
    {
        if (EnemyData != null)
        {
            Debug.Log($"[Grape] Elite 타입 적용 - 새 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"[Grape] 공격력은 AttackData에서 관리됨");
        }
    }

    #endregion

    #region ⭐ 레벨업 시스템

    /// <summary>
    /// Grape 레벨업 처리
    /// </summary>
    public override void LevelUp()
    {
        CurrentLevel += 1;
        
        // 새 스탯 계산
        ApplyGrapeSpecificSettings();
        
        Debug.Log($"[Grape] {gameObject.name} 레벨업! 새 레벨: {CurrentLevel}");
        Debug.Log($"[Grape] 새 체력: {GetScaledMaxHealth():F1}");
        Debug.Log($"[Grape] 공격력은 AttackData에서 관리됨");
    }

    #endregion

    #region ⭐ 디버깅 도구

    /// <summary>
    /// Grape 상태 디버그 출력
    /// </summary>
    [ContextMenu("Debug Grape Info")]
    private void DebugGrapeInfo()
    {
        string info = $"=== Grape {gameObject.name} ===\n";
        info += $"Current Level: {CurrentLevel}\n";
        info += $"Attack Type: 원거리 (Ranged)\n";
        info += $"Patrol Radius: {PatrolRadius}\n";
        info += $"Attack Range: {AttackRange}\n";
        info += $"Chase Range: {ChaseRange}\n\n";
        
        if (EnemyData != null)
        {
            info += "=== EnemyData 정보 ===\n";
            info += EnemyData.GetDebugInfo(CurrentLevel, GrowthProfile);
        }
        else
        {
            info += "❌ EnemyData가 할당되지 않았습니다!\n";
        }

        // AttackData 정보 추가
        if (rangedAttack?.AttackData != null)
        {
            info += "\n=== AttackData 정보 ===\n";
            info += $"실제 데미지: {rangedAttack.GetScaledDamage()}\n";
        }
        else
        {
            info += "\n❌ AttackData가 할당되지 않았습니다!\n";
        }
        
        Debug.Log(info);
    }

    #endregion
} 