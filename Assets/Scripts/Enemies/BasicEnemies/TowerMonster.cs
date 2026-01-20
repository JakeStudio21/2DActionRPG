using UnityEngine;

/// <summary>
/// TowerMonster 몬스터 클래스 - 고층 타워 전용 원거리 공격
/// 특징: 벽에 둘러싸인 안전한 위치에서 플레이어 공격 (벽 무시)
/// ⭐ 테스트용: 벽 차단 시스템 응용 사례
/// </summary>
public class TowerMonster : BaseEnemy
{
    [Header("⭐ TowerMonster 전용 컴포넌트")]
    [SerializeField] private RangedAttack rangedAttack;

    #region ⭐ BaseEnemy 추상 속성 구현 - 완전한 데이터 기반

    public override float PatrolRadius 
    { 
        get 
        {
            // 스폰 시 설정된 값 우선 사용
            if (patrolRadius > 0) 
                return patrolRadius;
            
            // 데이터 기반 fallback (TowerMonster는 패트롤 안 함 - 고정형)
            if (enemyData != null)
                return enemyData.PatrolRadius;
            
            return 0f; // 기본값: 이동 안 함 (타워 고정)
        } 
    }
    
    public override float AttackRange 
    { 
        get 
        {
            // 1순위: AttackData
            if (rangedAttack != null && rangedAttack.AttackData != null)
                return rangedAttack.AttackData.AttackRange;
                
            Debug.LogError($"[TowerMonster] {gameObject.name}: RangedAttack 또는 AttackData가 없습니다!");
            return 8f; // TowerMonster는 긴 공격 범위 (원거리)
        } 
    }
    
    public override float DetectionRange
    {
        get
        {
            if (EnemyData != null)
            {
                float range = EnemyData.DetectionRange;
                Debug.Log($"[TowerMonster] {gameObject.name} DetectionRange: {range} (from EnemyData)");
                return range;
            }
                
            Debug.LogError($"[TowerMonster] {gameObject.name}: EnemyData가 없어서 DetectionRange 확인 불가! fallback 10f 사용");
            return 10f; // TowerMonster는 매우 넓은 감지 범위
        }
    }
    
    public override float ChaseRange 
    { 
        get 
        {
            if (EnemyData != null)
            {
                float range = EnemyData.ChaseRange;
                Debug.Log($"[TowerMonster] {gameObject.name} ChaseRange: {range} (from EnemyData)");
                return range;
            }
                
            Debug.LogError($"[TowerMonster] {gameObject.name}: EnemyData가 없어서 ChaseRange 확인 불가! fallback 12f 사용");
            return 12f; // TowerMonster는 넓은 추적 범위
        } 
    }

    #endregion

    #region ⭐ BaseEnemy 추상 메서드 구현

    protected override void OnAwakeInitialize()
    {
        // RangedAttack 컴포넌트 확인
        if (rangedAttack == null)
            rangedAttack = GetComponent<RangedAttack>();
            
        Debug.Log($"[TowerMonster] {gameObject.name} Awake 초기화 완료");
    }

    protected override void OnStartInitialize()
    {
        ApplyTowerMonsterSpecificSettings();
        Debug.Log($"[TowerMonster] {gameObject.name} Start 초기화 완료");
    }

    protected override void InitializeAttackSystem()
    {
        if (rangedAttack != null)
        {
            rangedAttack.Initialize();
            Debug.Log($"[TowerMonster] {gameObject.name} RangedAttack 시스템 초기화 완료");
            
            // ⭐ 중요: 벽 무시 설정은 Inspector에서 수동으로!
            // RangedAttack 컴포넌트의 wallLayer를 Nothing으로 설정하세요.
        }
        else
        {
            Debug.LogError($"[TowerMonster] {gameObject.name}: RangedAttack 컴포넌트가 없습니다!");
        }
    }

    public override void Attack()
    {
        if (rangedAttack != null && rangedAttack.CanAttack())
        {
            rangedAttack.Attack();
            Debug.Log($"[TowerMonster] {gameObject.name} 원거리 공격 실행! (고층 타워에서)");
        }
    }

    #endregion

    #region ⭐ TowerMonster 전용 설정

    /// <summary>
    /// TowerMonster 전용 설정 적용
    /// </summary>
    private void ApplyTowerMonsterSpecificSettings()
    {
        if (EnemyData != null)
        {
            Debug.Log($"[TowerMonster] {gameObject.name} 전용 설정 적용:");
            Debug.Log($"  - 타입: {EnemyData.EnemyType}");
            Debug.Log($"  - 레벨: {CurrentLevel}");
            Debug.Log($"  - 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"  - 공격력: AttackData에서 관리됨");
            Debug.Log($"  - 방어력: {GetScaledDefense():F1}");
            Debug.Log($"  - 이동속도: {GetScaledMoveSpeed():F1}");
            Debug.Log($"  - 특성: 고층 타워 원거리 공격, 벽 무시, 고정형 방어, 넓은 사거리");
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
            Debug.Log($"[TowerMonster] Elite 타입 적용 - 새 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"[TowerMonster] 공격력은 AttackData에서 관리됨");
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
            Debug.Log($"[TowerMonster] 실제 발사체 데미지: {actualDamage}");
        }
        else
        {
            Debug.LogError($"[TowerMonster] RangedAttack 컴포넌트가 없어서 데미지 테스트 불가!");
        }
    }

    #endregion

    #region ⭐ 레벨업 시스템

    /// <summary>
    /// TowerMonster 레벨업 처리
    /// </summary>
    public override void LevelUp()
    {
        CurrentLevel += 1;
        
        // 새 스탯 계산
        ApplyTowerMonsterSpecificSettings();
        
        Debug.Log($"[TowerMonster] {gameObject.name} 레벨업! 새 레벨: {CurrentLevel}");
        Debug.Log($"[TowerMonster] 새 체력: {GetScaledMaxHealth():F1}");
        Debug.Log($"[TowerMonster] 공격력은 AttackData에서 관리됨");
    }

    #endregion

    #region ⭐ 디버깅 도구

    /// <summary>
    /// TowerMonster 상태 디버그 출력
    /// </summary>
    [ContextMenu("Debug TowerMonster Info")]
    private void DebugTowerMonsterInfo()
    {
        string info = $"=== TowerMonster {gameObject.name} ===\n";
        info += $"Current Level: {CurrentLevel}\n";
        info += $"Attack Type: 원거리 (Ranged) - 고층 타워\n";
        info += $"Patrol Radius: {PatrolRadius} (고정형)\n";
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
        // 기존 OnValidate 내용들...
        
        // 아이소메트릭 데이터 기본값 설정
        if (isometricData == null)
        {
            isometricData = new IsometricCharacterData();
            isometricData.SetDefaults();
        }
        
        if (!isometricData.IsValid())
        {
            Debug.LogWarning($"[{GetType().Name}] {name}의 아이소메트릭 데이터가 유효하지 않습니다.");
            isometricData.SetDefaults();
        }
    }

    #endregion
}

