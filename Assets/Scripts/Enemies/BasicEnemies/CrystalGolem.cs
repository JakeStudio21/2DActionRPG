using UnityEngine;

/// <summary>
/// CrystalGolem 몬스터 클래스 - AOE 공격 특화
/// ⭐ [New System] AOE 공격 시스템 기반 몬스터
/// </summary>
public class CrystalGolem : BaseEnemy
{
    [Header("⭐ CrystalGolem 전용 컴포넌트")]
    [SerializeField] private AOEAttack aoeAttack;

    [Header("아이소메트릭 설정")]
    [SerializeField] private IsometricCharacterData isometricData = new IsometricCharacterData();

    #region ⭐ BaseEnemy 추상 속성 구현 - 완전한 데이터 기반

    public override float PatrolRadius 
    { 
        get 
        {
            // 스폰 시 설정된 값 우선 사용
            if (patrolRadius > 0) 
                return patrolRadius;
            
            // 데이터 기반 fallback (CrystalGolem은 중간 패트롤 범위)
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
            if (aoeAttack != null && aoeAttack.AttackData != null)
                return aoeAttack.AttackData.AttackRange;
                
            Debug.LogError($"[CrystalGolem] {gameObject.name}: AOEAttack 또는 AttackData가 없습니다!");
            return 4f; // CrystalGolem은 AOE 공격으로 더 긴 범위
        } 
    }
    
    public override float DetectionRange
    {
        get
        {
            if (EnemyData != null)
                return EnemyData.DetectionRange;
                
            Debug.LogError($"[CrystalGolem] {gameObject.name}: EnemyData가 없어서 DetectionRange 확인 불가!");
            return 16f; // CrystalGolem은 더 긴 감지 범위
        }
    }
    
    public override float ChaseRange 
    { 
        get 
        {
            if (EnemyData != null)
                return EnemyData.ChaseRange;
                
            Debug.LogError($"[CrystalGolem] {gameObject.name}: EnemyData가 없어서 ChaseRange 확인 불가!");
            return 5f; // CrystalGolem은 더 긴 추적 범위
        } 
    }

    #endregion

    #region ⭐ BaseEnemy 추상 메서드 구현

    public override void Attack()
    {
        if (aoeAttack != null)
        {
            Debug.Log($"[CrystalGolem] {gameObject.name} - AOE 공격 실행!");
            aoeAttack.Attack();
        }
        else
        {
            Debug.LogError($"[CrystalGolem] {gameObject.name}: AOEAttack 컴포넌트가 없습니다!");
        }
    }

    protected override void InitializeAttackSystem()
    {
        Debug.Log($"[CrystalGolem] {gameObject.name} - AOE 공격 시스템 초기화");
        
        // AOEAttack 컴포넌트 초기화
        if (aoeAttack != null)
        {
            aoeAttack.Initialize();
            Debug.Log($"[CrystalGolem] AOEAttack 초기화 완료");
        }
        else
        {
            Debug.LogError($"[CrystalGolem] {gameObject.name}: AOEAttack 컴포넌트가 없습니다!");
        }
    }

    protected override void OnAwakeInitialize()
    {
        Debug.Log($"[CrystalGolem] {gameObject.name} - Awake 초기화");
        
        // AOEAttack 컴포넌트 자동 찾기
        if (aoeAttack == null)
        {
            aoeAttack = GetComponent<AOEAttack>();
        }
    }

    protected override void OnStartInitialize()
    {
        Debug.Log($"[CrystalGolem] {gameObject.name} - Start 초기화");
        
        // CrystalGolem 전용 초기화
        InitializeCrystalGolem();
    }

    public override IsometricCharacterData GetIsometricData()
    {
        // isometricData가 유효하지 않으면 기본값 생성
        if (isometricData == null || !isometricData.IsValid())
        {
            return CreateDefaultIsometricData();
        }
        
        return isometricData;
    }

    #endregion

    #region CrystalGolem 전용 초기화

    /// <summary>
    /// CrystalGolem 전용 초기화
    /// </summary>
    private void InitializeCrystalGolem()
    {
        Debug.Log($"[CrystalGolem] {gameObject.name} - CrystalGolem 초기화 시작");
        
        // AOEAttack 컴포넌트 검증
        ValidateAOEAttackComponent();
        
        // CrystalGolem 전용 설정
        ConfigureCrystalGolemSettings();
        
        Debug.Log($"[CrystalGolem] {gameObject.name} - CrystalGolem 초기화 완료");
    }

    /// <summary>
    /// AOEAttack 컴포넌트 검증
    /// </summary>
    private void ValidateAOEAttackComponent()
    {
        if (aoeAttack == null)
        {
            Debug.LogError($"[CrystalGolem] {gameObject.name}: AOEAttack 컴포넌트가 할당되지 않았습니다!");
            Debug.LogError("Inspector에서 AOEAttack 컴포넌트를 할당해주세요.");
            return;
        }

        // AttackData 검증
        if (aoeAttack.AttackData == null)
        {
            Debug.LogError($"[CrystalGolem] {gameObject.name}: AOEAttack의 AttackData가 설정되지 않았습니다!");
            Debug.LogError("Inspector에서 AttackData를 할당해주세요.");
            return;
        }

        // AOE 타입 검증
        if (aoeAttack.AttackData.AttackType != AttackType.AOE)
        {
            Debug.LogWarning($"[CrystalGolem] {gameObject.name}: AttackData의 공격 타입이 AOE가 아닙니다: {aoeAttack.AttackData.AttackType}");
        }

        Debug.Log($"[CrystalGolem] AOEAttack 컴포넌트 검증 완료");
    }

    /// <summary>
    /// CrystalGolem 전용 설정
    /// </summary>
    private void ConfigureCrystalGolemSettings()
    {
        // CrystalGolem은 느리지만 강력한 몬스터
        if (enemyData != null)
        {
            Debug.Log($"[CrystalGolem] EnemyData 기반 설정:");
            Debug.Log($"  - 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"  - 이동속도: {GetScaledMoveSpeed():F1}");
            Debug.Log($"  - 공격력: AttackData에서 관리");
            Debug.Log($"  - 공격 범위: {AttackRange}");
            Debug.Log($"  - 패트롤 범위: {PatrolRadius}");
            Debug.Log($"  - 추적 범위: {ChaseRange}");
        }

        // AOE 공격 설정 확인
        if (aoeAttack != null && aoeAttack.AttackData != null)
        {
            Debug.Log($"[CrystalGolem] AOE 공격 설정:");
            Debug.Log($"  - AOE 지속시간: {aoeAttack.AttackData.AOEDuration}초");
            Debug.Log($"  - AOE 모양: {aoeAttack.AttackData.AOEShape}");
            Debug.Log($"  - AOE 크기: {aoeAttack.AttackData.AOEScale}");
        }
    }

    #endregion

    #region 디버그 및 테스트

    /// <summary>
    /// CrystalGolem 디버그 정보
    /// </summary>
    [ContextMenu("Debug CrystalGolem Info")]
    public void DebugCrystalGolemInfo()
    {
        string info = $"=== CrystalGolem {gameObject.name} ===\n";
        info += $"Patrol Radius: {PatrolRadius:F1}\n";
        info += $"Attack Range: {AttackRange:F1}\n";
        info += $"Chase Range: {ChaseRange:F1}\n";
        info += $"Current Level: {CurrentLevel}\n";
        info += $"AOE Attack: {(aoeAttack != null ? "설정됨" : "없음")}\n";
        
        if (aoeAttack != null && aoeAttack.AttackData != null)
        {
            info += "\n=== AOE Attack Data ===\n";
            info += $"AOE Duration: {aoeAttack.AttackData.AOEDuration:F1}초\n";
            info += $"AOE Shape: {aoeAttack.AttackData.AOEShape}\n";
            info += $"AOE Scale: {aoeAttack.AttackData.AOEScale:F1}\n";
            info += $"Damage: {aoeAttack.GetScaledDamage()}\n";
        }
        
        if (enemyData != null)
        {
            info += "\n=== Enemy Data ===\n";
            info += $"Base Health: {enemyData.BaseHealth}\n";
            info += $"Scaled Health: {GetScaledMaxHealth():F1}\n";
            info += $"Base Move Speed: {enemyData.BaseMoveSpeed}\n";
            info += $"Scaled Move Speed: {GetScaledMoveSpeed():F1}\n";
            info += $"Attack Damage: AttackData에서 관리\n";
        }
        
        Debug.Log(info);
    }

    /// <summary>
    /// AOE 공격 테스트
    /// </summary>
    [ContextMenu("Test AOE Attack")]
    public void TestAOEAttack()
    {
        if (aoeAttack != null)
        {
            Debug.Log($"[CrystalGolem] {gameObject.name} - AOE 공격 테스트 실행!");
            aoeAttack.Attack();
        }
        else
        {
            Debug.LogError($"[CrystalGolem] {gameObject.name}: AOEAttack 컴포넌트가 없습니다!");
        }
    }

    #endregion

    #region Unity Editor 지원

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
            Debug.LogWarning($"[{GetType().Name}] {name}의 아이소메트릭 데이터가 유효하지 않습니다.");
            isometricData.SetDefaults();
        }
    }

    #endregion
}
