using UnityEngine;

/// <summary>
/// Ghost 몬스터 클래스 - 복합 원거리 공격 특화
/// ⭐ [Complete Reset] 완전한 데이터 기반 시스템
/// </summary>
public class Ghost : BaseEnemy
{
    [Header("⭐ Ghost 전용 컴포넌트")]
    [SerializeField] private MultiShotRangedAttack multiShotAttack;

    #region ⭐ BaseEnemy 추상 속성 구현 - 완전한 데이터 기반

    public override float PatrolRadius 
    { 
        get 
        {
            // 스폰 시 설정된 값 우선 사용
            if (patrolRadius > 0) 
                return patrolRadius;
            
            // 데이터 기반 fallback
            if (enemyData != null)
                return enemyData.PatrolRadius;
            
            return 3f; // 기본값
        } 
    }
    
    public override float AttackRange 
    { 
        get 
        {
            // 1순위: AttackData
            if (multiShotAttack != null && multiShotAttack.AttackData != null)
                return multiShotAttack.AttackData.AttackRange;
                
            Debug.LogError($"[Ghost] {gameObject.name}: MultiShotRangedAttack 또는 AttackData가 없습니다!");
            return 4f; // 최소 안전값
        } 
    }
    
    public override float DetectionRange
    {
        get
        {
            if (EnemyData != null)
                return EnemyData.DetectionRange;
                
            Debug.LogError($"[Ghost] {gameObject.name}: EnemyData가 없어서 DetectionRange 확인 불가!");
            return 7f; // 최소 안전값
        }
    }
    
    public float ChaseRange
    {
        get
        {
            if (EnemyData != null)
                return EnemyData.ChaseRange;
                
            Debug.LogError($"[Ghost] {gameObject.name}: EnemyData가 없어서 ChaseRange 확인 불가!");
            return 10f; // 최소 안전값
        }
    }

    #endregion

    #region ⭐ BaseEnemy 추상 메서드 구현

    protected override void OnAwakeInitialize()
    {
        // MultiShotRangedAttack 컴포넌트 확인
        if (multiShotAttack == null)
            multiShotAttack = GetComponent<MultiShotRangedAttack>();
            
        Debug.Log($"[Ghost] {gameObject.name} Awake 초기화 완료");
    }

    protected override void OnStartInitialize()
    {
        ApplyGhostSpecificSettings();
        Debug.Log($"[Ghost] {gameObject.name} Start 초기화 완료");
    }

    protected override void InitializeAttackSystem()
    {
        if (multiShotAttack != null)
        {
            multiShotAttack.Initialize(); // ⭐ 수정: 매개변수 제거
            Debug.Log($"[Ghost] {gameObject.name} MultiShotRangedAttack 시스템 초기화 완료");
        }
        else
        {
            Debug.LogError($"[Ghost] {gameObject.name}: MultiShotRangedAttack 컴포넌트가 없습니다!");
        }
    }

    public override void Attack()
    {
        if (multiShotAttack != null && multiShotAttack.CanAttack())
        {
            multiShotAttack.Attack();
            Debug.Log($"[Ghost] {gameObject.name} 복합 원거리 공격 실행!");
        }
    }

    #endregion

    #region ⭐ Ghost 전용 설정

    /// <summary>
    /// Ghost 전용 설정 적용
    /// </summary>
    private void ApplyGhostSpecificSettings()
    {
        if (EnemyData != null)
        {
            Debug.Log($"[Ghost] {gameObject.name} 전용 설정 적용:");
            Debug.Log($"  - 타입: {EnemyData.EnemyType}");
            Debug.Log($"  - 레벨: {CurrentLevel}");
            Debug.Log($"  - 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"  - 공격력: AttackData에서 관리됨");
            Debug.Log($"  - 방어력: {GetScaledDefense():F1}");
            Debug.Log($"  - 이동속도: {GetScaledMoveSpeed():F1}");
        }
    }

    /// <summary>
    /// 실제 복합 공격 데미지 테스트 (AttackData 기반)
    /// </summary>
    [ContextMenu("Test MultiShot Damage")]
    private void TestMultiShotDamage()
    {
        if (multiShotAttack != null)
        {
            int actualDamage = multiShotAttack.GetScaledDamage();
            Debug.Log($"[Ghost] 실제 복합 공격 데미지: {actualDamage}");
        }
        else
        {
            Debug.LogError($"[Ghost] MultiShotRangedAttack 컴포넌트가 없어서 데미지 테스트 불가!");
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
            Debug.Log($"[Ghost] Elite 타입 적용 - 새 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"[Ghost] 공격력은 AttackData에서 관리됨");
        }
    }

    #endregion

    #region ⭐ 레벨업 시스템

    /// <summary>
    /// Ghost 레벨업 처리
    /// </summary>
    public override void LevelUp()
    {
        CurrentLevel += 1;
        
        // 새 스탯 계산
        ApplyGhostSpecificSettings();
        
        Debug.Log($"[Ghost] {gameObject.name} 레벨업! 새 레벨: {CurrentLevel}");
        Debug.Log($"[Ghost] 새 체력: {GetScaledMaxHealth():F1}");
        Debug.Log($"[Ghost] 공격력은 AttackData에서 관리됨");
    }

    #endregion

    #region ⭐ 디버깅 도구

    /// <summary>
    /// Ghost 상태 디버그 출력
    /// </summary>
    [ContextMenu("Debug Ghost Info")]
    private void DebugGhostInfo()
    {
        string info = $"=== Ghost {gameObject.name} ===\n";
        info += $"Current Level: {CurrentLevel}\n";
        info += $"Attack Type: 복합 원거리 (MultiShot)\n";
        info += $"Patrol Radius: {PatrolRadius}\n";
        info += $"Attack Range: {AttackRange}\n";
        info += $"Detection Range: {DetectionRange}\n";
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
        if (multiShotAttack?.AttackData != null)
        {
            info += "\n=== AttackData 정보 ===\n";
            info += $"실제 데미지: {multiShotAttack.GetScaledDamage()}\n";
        }
        else
        {
            info += "\n❌ AttackData가 할당되지 않았습니다!\n";
        }
        
        Debug.Log(info);
    }

    [Header("아이소메트릭 설정")]
    [SerializeField] private IsometricCharacterData isometricData = new IsometricCharacterData();

    public IsometricCharacterData IsometricData => isometricData;

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