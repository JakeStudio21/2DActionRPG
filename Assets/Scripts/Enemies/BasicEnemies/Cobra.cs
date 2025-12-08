using UnityEngine;

/// <summary>
/// Cobra 몬스터 클래스 - 근접 공격 특화 (8방향 스프라이트)
/// ⭐ [Complete Reset] 완전한 데이터 기반 시스템
/// </summary>
public class Cobra : BaseEnemy
{
    [Header("⭐ Cobra 전용 컴포넌트")]
    [SerializeField] private MeleeAttack meleeAttack;

    #region ⭐ BaseEnemy 추상 속성 구현 - 완전한 데이터 기반

    public override float PatrolRadius 
    { 
        get 
        {
            // 스폰 시 설정된 값 우선 사용
            if (patrolRadius > 0) 
                return patrolRadius;
            
            // 데이터 기반 fallback (Cobra는 중간 패트롤 범위)
            if (enemyData != null)
                return enemyData.PatrolRadius;
            
            return 3f; // 기본값 (영역 방어형)
        } 
    }
    
    public override float AttackRange 
    { 
        get 
        {
            // 1순위: AttackData
            if (meleeAttack != null && meleeAttack.AttackData != null)
                return meleeAttack.AttackData.AttackRange;
                
            Debug.LogError($"[Cobra] {gameObject.name}: MeleeAttack 또는 AttackData가 없습니다!");
            return 1.2f; // Cobra는 중간 공격 범위
        } 
    }
    
    public override float DetectionRange
    {
        get
        {
            if (EnemyData != null)
            {
                float range = EnemyData.DetectionRange;
                Debug.Log($"[Cobra] {gameObject.name} DetectionRange: {range} (from EnemyData)");
                return range;
            }
                
            Debug.LogError($"[Cobra] {gameObject.name}: EnemyData가 없어서 DetectionRange 확인 불가! fallback 5f 사용");
            return 5f; // Cobra는 보통 감지 범위
        }
    }
    
    public override float ChaseRange 
    { 
        get 
        {
            if (EnemyData != null)
            {
                float range = EnemyData.ChaseRange;
                Debug.Log($"[Cobra] {gameObject.name} ChaseRange: {range} (from EnemyData)");
                return range;
            }
                
            Debug.LogError($"[Cobra] {gameObject.name}: EnemyData가 없어서 ChaseRange 확인 불가! fallback 7f 사용");
            return 7f; // Cobra는 중간 추적 범위
        } 
    }

    #endregion

    #region ⭐ BaseEnemy 추상 메서드 구현

    protected override void OnAwakeInitialize()
    {
        // MeleeAttack 컴포넌트 확인
        if (meleeAttack == null)
            meleeAttack = GetComponent<MeleeAttack>();
            
        Debug.Log($"[Cobra] {gameObject.name} Awake 초기화 완료");
    }

    protected override void OnStartInitialize()
    {
        ApplyCobraSpecificSettings();
        Debug.Log($"[Cobra] {gameObject.name} Start 초기화 완료");
    }

    protected override void InitializeAttackSystem()
    {
        if (meleeAttack != null)
        {
            meleeAttack.Initialize();
            Debug.Log($"[Cobra] {gameObject.name} MeleeAttack 시스템 초기화 완료");
        }
        else
        {
            Debug.LogError($"[Cobra] {gameObject.name}: MeleeAttack 컴포넌트가 없습니다!");
        }
    }

    public override void Attack()
    {
        if (meleeAttack != null && meleeAttack.CanAttack())
        {
            meleeAttack.Attack();
            Debug.Log($"[Cobra] {gameObject.name} 근접 공격 실행!");
        }
    }

    #endregion

    #region ⭐ Cobra 전용 설정

    /// <summary>
    /// Cobra 전용 설정 적용
    /// </summary>
    private void ApplyCobraSpecificSettings()
    {
        if (EnemyData != null)
        {
            Debug.Log($"[Cobra] {gameObject.name} 전용 설정 적용:");
            Debug.Log($"  - 타입: {EnemyData.EnemyType}");
            Debug.Log($"  - 레벨: {CurrentLevel}");
            Debug.Log($"  - 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"  - 공격력: AttackData에서 관리됨");
            Debug.Log($"  - 방어력: {GetScaledDefense():F1}");
            Debug.Log($"  - 이동속도: {GetScaledMoveSpeed():F1}");
            Debug.Log($"  - 특성: 빠른 이동속도, 독 공격");
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
            Debug.Log($"[Cobra] Elite 타입 적용 - 새 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"[Cobra] 공격력은 AttackData에서 관리됨");
        }
    }

    /// <summary>
    /// 실제 데미지 테스트 (AttackData 기반)
    /// </summary>
    [ContextMenu("Test Melee Damage")]
    private void TestMeleeDamage()
    {
        if (meleeAttack != null)
        {
            int actualDamage = meleeAttack.GetScaledDamage();
            Debug.Log($"[Cobra] 실제 근접 데미지: {actualDamage}");
        }
        else
        {
            Debug.LogError($"[Cobra] MeleeAttack 컴포넌트가 없어서 데미지 테스트 불가!");
        }
    }

    #endregion

    #region ⭐ 레벨업 시스템

    /// <summary>
    /// Cobra 레벨업 처리
    /// </summary>
    public override void LevelUp()
    {
        CurrentLevel += 1;
        
        // 새 스탯 계산
        ApplyCobraSpecificSettings();
        
        Debug.Log($"[Cobra] {gameObject.name} 레벨업! 새 레벨: {CurrentLevel}");
        Debug.Log($"[Cobra] 새 체력: {GetScaledMaxHealth():F1}");
        Debug.Log($"[Cobra] 공격력은 AttackData에서 관리됨");
    }

    #endregion

    #region ⭐ 디버깅 도구

    /// <summary>
    /// Cobra 상태 디버그 출력
    /// </summary>
    [ContextMenu("Debug Cobra Info")]
    private void DebugCobraInfo()
    {
        string info = $"=== Cobra {gameObject.name} ===\n";
        info += $"Current Level: {CurrentLevel}\n";
        info += $"Attack Type: 근접 (Melee)\n";
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
        if (meleeAttack?.AttackData != null)
        {
            info += "\n=== AttackData 정보 ===\n";
            info += $"실제 데미지: {meleeAttack.GetScaledDamage()}\n";
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


