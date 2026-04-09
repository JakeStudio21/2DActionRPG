using UnityEngine;

/// <summary>
/// Elite_SandGolem 엘리트 몬스터 클래스 - 근접 공격 + 스킬 (8방향 스프라이트)
/// ⭐ 엘리트 전용: 평타 60% + 스킬 40% 확률 시스템
/// ⭐ [Complete Reset] 완전한 데이터 기반 시스템
/// </summary>
public class Elite_SandGolem : BaseEnemy
{
    [Header("⭐ Elite_SandGolem 전용 컴포넌트")]
    [SerializeField] private MeleeAttack meleeAttack;              // 평타용
    [SerializeField] private EliteSkillController skillController; // 스킬용
    [SerializeField] private EliteAttackBehaviour eliteAttack;     // 통합 공격 관리

    #region ⭐ BaseEnemy 추상 속성 구현 - 완전한 데이터 기반

    public override float AttackRange 
    { 
        get 
        {
            if (meleeAttack != null && meleeAttack.AttackData != null)
                return meleeAttack.AttackData.AttackRange;
            return 1.5f;
        } 
    }
    
    public override float DetectionRange
    {
        get
        {
            if (EnemyData != null)
                return EnemyData.DetectionRange;
            return 6f;
        }
    }
    
    public override float ChaseRange 
    { 
        get 
        {
            if (EnemyData != null)
                return EnemyData.ChaseRange;
            return 8f;
        } 
    }

    #endregion

    #region ⭐ BaseEnemy 추상 메서드 구현

    protected override void OnAwakeInitialize()
    {
        // MeleeAttack 컴포넌트 확인
        if (meleeAttack == null)
            meleeAttack = GetComponent<MeleeAttack>();
        
        // EliteSkillController 확인
        if (skillController == null)
            skillController = GetComponent<EliteSkillController>();
        
        // EliteAttackBehaviour 확인
        if (eliteAttack == null)
            eliteAttack = GetComponent<EliteAttackBehaviour>();
            
        Debug.Log($"[Elite_SandGolem] {gameObject.name} Awake 초기화 완료");
    }

    protected override void OnStartInitialize()
    {
        ApplyEliteSandGolemSpecificSettings();
        Debug.Log($"[Elite_SandGolem] {gameObject.name} Start 초기화 완료");
    }

    protected override void InitializeAttackSystem()
    {
        // 평타 시스템 초기화
        if (meleeAttack != null)
        {
            meleeAttack.Initialize();
            Debug.Log($"[Elite_SandGolem] {gameObject.name} MeleeAttack 시스템 초기화 완료");
        }
        else
        {
            Debug.LogError($"[Elite_SandGolem] {gameObject.name}: MeleeAttack 컴포넌트가 없습니다!");
        }
        
        // ⭐ 엘리트 공격 시스템 초기화 (핵심!)
        if (eliteAttack != null)
        {
            eliteAttack.Initialize();
            Debug.Log($"[Elite_SandGolem] {gameObject.name} EliteAttackBehaviour 시스템 초기화 완료");
        }
        else
        {
            Debug.LogError($"[Elite_SandGolem] {gameObject.name}: EliteAttackBehaviour 컴포넌트가 없습니다!");
        }
    }

    public override void Attack()
    {
        // 엘리트는 EliteAttackBehaviour가 공격 결정
        if (eliteAttack != null && eliteAttack.CanAttack())
        {
            eliteAttack.Attack();
            Debug.Log($"[Elite_SandGolem] {gameObject.name} 엘리트 공격 실행 (평타 or 스킬)!");
        }
        else if (meleeAttack != null && meleeAttack.CanAttack())
        {
            // Fallback: EliteAttackBehaviour가 없으면 평타만
            meleeAttack.Attack();
            Debug.LogWarning($"[Elite_SandGolem] {gameObject.name} EliteAttackBehaviour 없음 - 평타로 fallback");
        }
    }

    #endregion

    #region ⭐ Elite_SandGolem 전용 설정

    /// <summary>
    /// Elite_SandGolem 전용 설정 적용
    /// </summary>
    private void ApplyEliteSandGolemSpecificSettings()
    {
        if (EnemyData != null)
        {
            Debug.Log($"[Elite_SandGolem] {gameObject.name} 전용 설정 적용:");
            Debug.Log($"  - 타입: {EnemyData.EnemyType} (Elite)");
            Debug.Log($"  - 레벨: {CurrentLevel}");
            Debug.Log($"  - 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"  - 공격력: AttackData에서 관리됨");
            Debug.Log($"  - 방어력: {GetScaledDefense():F1}");
            Debug.Log($"  - 이동속도: {GetScaledMoveSpeed():F1}");
            Debug.Log($"  - 특성: 엘리트 모래 골렘, 평타 60% + 스킬 40%");
            
            // 스킬 정보 출력
            if (EnemyData.HasSkillData)
            {
                Debug.Log($"  - 스킬 개수: {EnemyData.SkillDataList.Count}");
                for (int i = 0; i < EnemyData.SkillDataList.Count; i++)
                {
                    if (EnemyData.SkillDataList[i] != null)
                    {
                        Debug.Log($"    └─ {EnemyData.SkillDataList[i].SkillName}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Elite 타입 강제 적용 (이미 Elite이지만 재확인용)
    /// </summary>
    [ContextMenu("Verify Elite Type")]
    private void VerifyEliteType()
    {
        if (EnemyData != null)
        {
            Debug.Log($"[Elite_SandGolem] 타입 확인: {EnemyData.EnemyType}");
            Debug.Log($"[Elite_SandGolem] Elite 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"[Elite_SandGolem] 평타 확률: {EnemyData.MeleeAttackProbability}%");
            Debug.Log($"[Elite_SandGolem] 스킬 확률: {EnemyData.SkillUseProbability}%");
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
            Debug.Log($"[Elite_SandGolem] 실제 근접 데미지: {actualDamage}");
        }
        else
        {
            Debug.LogError($"[Elite_SandGolem] MeleeAttack 컴포넌트가 없어서 데미지 테스트 불가!");
        }
    }

    /// <summary>
    /// 스킬 쿨다운 확인 (디버그용)
    /// </summary>
    [ContextMenu("Check Skill Cooldowns")]
    private void CheckSkillCooldowns()
    {
        if (skillController != null)
        {
            // EliteSkillController의 Debug Skill Cooldowns 호출
            skillController.SendMessage("DebugSkillCooldowns", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.LogWarning($"[Elite_SandGolem] EliteSkillController가 없습니다!");
        }
    }

    /// <summary>
    /// 공격 통계 확인 (디버그용)
    /// </summary>
    [ContextMenu("Check Attack Statistics")]
    private void CheckAttackStatistics()
    {
        if (eliteAttack != null)
        {
            // EliteAttackBehaviour의 Debug Attack Statistics 호출
            eliteAttack.SendMessage("DebugAttackStatistics", SendMessageOptions.DontRequireReceiver);
        }
        else
        {
            Debug.LogWarning($"[Elite_SandGolem] EliteAttackBehaviour가 없습니다!");
        }
    }

    #endregion

    #region ⭐ 레벨업 시스템

    /// <summary>
    /// Elite_SandGolem 레벨업 처리
    /// </summary>
    public override void LevelUp()
    {
        CurrentLevel += 1;
        
        // 새 스탯 계산
        ApplyEliteSandGolemSpecificSettings();
        
        Debug.Log($"[Elite_SandGolem] {gameObject.name} 레벨업! 새 레벨: {CurrentLevel}");
        Debug.Log($"[Elite_SandGolem] 새 체력: {GetScaledMaxHealth():F1}");
        Debug.Log($"[Elite_SandGolem] 공격력은 AttackData에서 관리됨");
    }

    #endregion

    #region ⭐ 디버깅 도구

    /// <summary>
    /// Elite_SandGolem 상태 디버그 출력
    /// </summary>
    [ContextMenu("Debug Elite_SandGolem Info")]
    private void DebugEliteSandGolemInfo()
    {
        string info = $"=== Elite_SandGolem {gameObject.name} ===\n";
        info += $"Current Level: {CurrentLevel}\n";
        info += $"Attack Type: 근접 + 스킬 (Elite Melee + Skill)\n";
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
            info += "\n=== AttackData 정보 (평타) ===\n";
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
        // 아이소메트릭 데이터 기본값 설정
        if (isometricData == null)
        {
            isometricData = new IsometricCharacterData();
            isometricData.SetDefaults();
        }
        
        if (!isometricData.IsValid())
        {
            Debug.LogWarning($"[Elite_SandGolem] {name}의 아이소메트릭 데이터가 유효하지 않습니다.");
            isometricData.SetDefaults();
        }
    }

    #endregion
}

