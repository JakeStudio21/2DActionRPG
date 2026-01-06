using UnityEngine;

/// <summary>
/// GoblinCart 몬스터 클래스 - 원거리 공격 특화 (8방향 스프라이트)
/// ⭐ [Complete Reset] 완전한 데이터 기반 시스템
/// </summary>
public class GoblinCart : BaseEnemy
{
    [Header("⭐ GoblinCart 전용 컴포넌트")]
    [SerializeField] private RangedAttack rangedAttack;

    #region ⭐ BaseEnemy 추상 속성 구현 - 완전한 데이터 기반

    public override float PatrolRadius 
    { 
        get 
        {
            // 스폰 시 설정된 값 우선 사용
            if (patrolRadius > 0) 
                return patrolRadius;
            
            // 데이터 기반 fallback (GoblinCart는 넓은 패트롤 범위 - 카트 이동형)
            if (enemyData != null)
                return enemyData.PatrolRadius;
            
            return 4f; // 기본값 (카트로 이동하며 공격)
        } 
    }
    
    public override float AttackRange 
    { 
        get 
        {
            // 1순위: AttackData
            if (rangedAttack != null && rangedAttack.AttackData != null)
                return rangedAttack.AttackData.AttackRange;
                
            Debug.LogError($"[GoblinCart] {gameObject.name}: RangedAttack 또는 AttackData가 없습니다!");
            return 7f; // GoblinCart는 긴 공격 범위 (카트 발사)
        } 
    }
    
    public override float DetectionRange
    {
        get
        {
            if (EnemyData != null)
            {
                float range = EnemyData.DetectionRange;
                Debug.Log($"[GoblinCart] {gameObject.name} DetectionRange: {range} (from EnemyData)");
                return range;
            }
                
            Debug.LogError($"[GoblinCart] {gameObject.name}: EnemyData가 없어서 DetectionRange 확인 불가! fallback 7f 사용");
            return 7f; // GoblinCart는 넓은 감지 범위
        }
    }
    
    public override float ChaseRange 
    { 
        get 
        {
            if (EnemyData != null)
            {
                float range = EnemyData.ChaseRange;
                Debug.Log($"[GoblinCart] {gameObject.name} ChaseRange: {range} (from EnemyData)");
                return range;
            }
                
            Debug.LogError($"[GoblinCart] {gameObject.name}: EnemyData가 없어서 ChaseRange 확인 불가! fallback 9f 사용");
            return 9f; // GoblinCart는 긴 추적 범위 (원거리형 + 이동형)
        } 
    }

    #endregion

    #region ⭐ BaseEnemy 추상 메서드 구현

    protected override void OnAwakeInitialize()
    {
        // RangedAttack 컴포넌트 확인
        if (rangedAttack == null)
            rangedAttack = GetComponent<RangedAttack>();
            
        Debug.Log($"[GoblinCart] {gameObject.name} Awake 초기화 완료");
    }

    protected override void OnStartInitialize()
    {
        ApplyGoblinCartSpecificSettings();
        Debug.Log($"[GoblinCart] {gameObject.name} Start 초기화 완료");
    }

    protected override void InitializeAttackSystem()
    {
        if (rangedAttack != null)
        {
            rangedAttack.Initialize();
            Debug.Log($"[GoblinCart] {gameObject.name} RangedAttack 시스템 초기화 완료");
        }
        else
        {
            Debug.LogError($"[GoblinCart] {gameObject.name}: RangedAttack 컴포넌트가 없습니다!");
        }
    }

    public override void Attack()
    {
        if (rangedAttack != null && rangedAttack.CanAttack())
        {
            rangedAttack.Attack();
            Debug.Log($"[GoblinCart] {gameObject.name} 원거리 공격 실행!");
        }
    }

    #endregion

    #region ⭐ GoblinCart 전용 설정

    /// <summary>
    /// GoblinCart 전용 설정 적용
    /// </summary>
    private void ApplyGoblinCartSpecificSettings()
    {
        if (EnemyData != null)
        {
            Debug.Log($"[GoblinCart] {gameObject.name} 전용 설정 적용:");
            Debug.Log($"  - 타입: {EnemyData.EnemyType}");
            Debug.Log($"  - 레벨: {CurrentLevel}");
            Debug.Log($"  - 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"  - 공격력: AttackData에서 관리됨");
            Debug.Log($"  - 방어력: {GetScaledDefense():F1}");
            Debug.Log($"  - 이동속도: {GetScaledMoveSpeed():F1}");
            Debug.Log($"  - 특성: 카트 발사, 직선 궤적, 긴 사거리, 빠른 이동속도");
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
            Debug.Log($"[GoblinCart] Elite 타입 적용 - 새 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"[GoblinCart] 공격력은 AttackData에서 관리됨");
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
            Debug.Log($"[GoblinCart] 실제 발사체 데미지: {actualDamage}");
        }
        else
        {
            Debug.LogError($"[GoblinCart] RangedAttack 컴포넌트가 없어서 데미지 테스트 불가!");
        }
    }

    #endregion

    #region ⭐ 레벨업 시스템

    /// <summary>
    /// GoblinCart 레벨업 처리
    /// </summary>
    public override void LevelUp()
    {
        CurrentLevel += 1;
        
        // 새 스탯 계산
        ApplyGoblinCartSpecificSettings();
        
        Debug.Log($"[GoblinCart] {gameObject.name} 레벨업! 새 레벨: {CurrentLevel}");
        Debug.Log($"[GoblinCart] 새 체력: {GetScaledMaxHealth():F1}");
        Debug.Log($"[GoblinCart] 공격력은 AttackData에서 관리됨");
    }

    #endregion

    #region ⭐ 디버깅 도구

    /// <summary>
    /// GoblinCart 상태 디버그 출력
    /// </summary>
    [ContextMenu("Debug GoblinCart Info")]
    private void DebugGoblinCartInfo()
    {
        string info = $"=== GoblinCart {gameObject.name} ===\n";
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
            info += $"발사체: {rangedAttack.AttackData.ProjectilePrefab?.name ?? "None"}\n";
            info += $"궤적: {rangedAttack.AttackData.TrajectoryType}\n";
        }
        else
        {
            info += "\n❌ AttackData가 할당되지 않았습니다!\n";
        }
        
        Debug.Log(info);
    }

    #endregion
    
    #region 🗺️ 아이소메트릭 데이터 시스템 (BaseEnemy 추상 메서드 구현)
    
    [Header("아이소메트릭 설정")]
    [SerializeField] private IsometricCharacterData isometricData = new IsometricCharacterData();

    /// <summary>
    /// BaseEnemy 추상 메서드 구현 - 아이소메트릭 데이터 반환
    /// </summary>
    public override IsometricCharacterData GetIsometricData()
    {
        // isometricData가 유효하지 않으면 기본값 생성
        if (isometricData == null || !isometricData.IsValid())
        {
            return CreateDefaultIsometricData();
        }
        
        return isometricData;
    }

    private void OnValidate()
    {
        // 아이소메트릭 데이터 기본값 설정
        if (isometricData == null)
        {
            isometricData = new IsometricCharacterData();
            isometricData.SetDefaults();
        }
        
        if (!isometricData.IsValid())
        {
            Debug.LogWarning($"[GoblinCart] {name}의 아이소메트릭 데이터가 유효하지 않습니다.");
            isometricData.SetDefaults();
        }
    }

    #endregion
}

