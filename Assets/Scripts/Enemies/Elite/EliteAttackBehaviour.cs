using UnityEngine;

/// <summary>
/// 엘리트 몬스터 공격 행동
/// BaseAttackBehaviour 상속하여 평타 + 스킬 통합 관리
/// EliteAttackDecision으로 평타/스킬 선택
/// </summary>
public class EliteAttackBehaviour : BaseAttackBehaviour
{
    [Header("⚡ 엘리트 전용")]
    [SerializeField] private EliteSkillController skillController;
    [SerializeField] private MeleeAttack meleeAttack; // 평타용
    
    [Header("🎲 공격 결정")]
    private EliteAttackDecision attackDecision;
    private EliteAttackDecision.AttackStatistics statistics;
    
    [Header("🎮 디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool trackStatistics = true;

    #region BaseAttackBehaviour 추상 메서드 구현

    protected override void OnInitialize()
    {
        
        // 컴포넌트 자동 참조
        if (skillController == null)
            skillController = GetComponent<EliteSkillController>();
        
        if (meleeAttack == null)
            meleeAttack = GetComponent<MeleeAttack>();
        
        // 공격 결정 시스템 초기화
        attackDecision = new EliteAttackDecision(enableDebugLogs);
        
        // 통계 시스템 초기화
        if (trackStatistics)
        {
            statistics = new EliteAttackDecision.AttackStatistics();
        }
        
        // 평타 시스템 초기화
        if (meleeAttack != null)
        {
            meleeAttack.Initialize();
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[EliteAttackBehaviour] {gameObject.name} 초기화 완료");
        }
    }

    protected override void ValidateAttackType()
    {
        // 엘리트는 평타 + 스킬 혼합이므로 특별한 검증 불필요
        if (enableDebugLogs)
        {
            Debug.Log($"[EliteAttackBehaviour] {gameObject.name}: 엘리트 공격 타입 (평타 + 스킬)");
        }
    }

    protected override void OnAttack()
    {
        // BaseEnemy 참조 확인
        if (baseEnemy == null || cachedPlayer == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[EliteAttackBehaviour] BaseEnemy 또는 Player가 null!");
            }
            return;
        }

        // 거리 계산
        float distanceToPlayer = Vector2.Distance(baseEnemy.transform.position, cachedPlayer.transform.position);
        
        // 공격 결정
        var decision = attackDecision.DecideAttack(
            baseEnemy.EnemyData,
            skillController,
            distanceToPlayer
        );
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎯 [EliteAttackBehaviour] {baseEnemy.gameObject.name}: {decision.Reason}");
        }

        // 결정에 따라 실행
        if (decision.IsSkill)
        {
            // 스킬 사용
            ExecuteSkillAttack(decision.SelectedSkill);
            
            // 통계 기록
            if (trackStatistics && decision.SelectedSkill != null)
            {
                statistics?.RecordSkill(decision.SelectedSkill.SkillName);
            }
        }
        else
        {
            // 평타 사용
            ExecuteMeleeAttack();
            
            // 통계 기록
            if (trackStatistics)
            {
                statistics?.RecordMelee();
            }
        }
    }
    
    #endregion

    /// <summary>
    /// 평타 실행
    /// </summary>
    private void ExecuteMeleeAttack()
    {
        if (meleeAttack == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogError($"[EliteAttackBehaviour] {gameObject.name}: MeleeAttack 컴포넌트 없음!");
            }
            return;
        }

        if (!meleeAttack.CanAttack())
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[EliteAttackBehaviour] {gameObject.name}: 평타 쿨다운 중");
            }
            return;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"⚔️ [EliteAttackBehaviour] {gameObject.name}: 평타 실행");
        }

        meleeAttack.Attack();
    }

    /// <summary>
    /// 스킬 실행
    /// </summary>
    private void ExecuteSkillAttack(SkillData skill)
    {
        if (skillController == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogError($"[EliteAttackBehaviour] {gameObject.name}: EliteSkillController 없음!");
            }
            return;
        }

        if (skill == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogError($"[EliteAttackBehaviour] {gameObject.name}: Skill이 null!");
            }
            return;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"🔮 [EliteAttackBehaviour] {gameObject.name}: {skill.SkillName} 실행");
        }

        skillController.StartSkillCast(skill);
    }

    /// <summary>
    /// Attack 메서드 override (BaseAttackBehaviour의 기본 구현 대체)
    /// </summary>
    public override void Attack()
    {
        // ⭐ 초기화 상태 체크
        if (enableDebugLogs)
        {
            Debug.Log($"🎯 [EliteAttackBehaviour] {gameObject.name} Attack() 호출!");
            Debug.Log($"   - skillController: {(skillController != null ? "OK" : "NULL")}");
            Debug.Log($"   - meleeAttack: {(meleeAttack != null ? "OK" : "NULL")}");
            Debug.Log($"   - baseEnemy: {(baseEnemy != null ? "OK" : "NULL")}");
            Debug.Log($"   - EnemyData: {(baseEnemy?.EnemyData != null ? "OK" : "NULL")}");
            Debug.Log($"   - HasSkillData: {baseEnemy?.EnemyData?.HasSkillData ?? false}");
        }
        
        // EliteAttackBehaviour는 attackData가 필요 없으므로
        // BaseAttackBehaviour.Attack()를 우회하고 직접 공격 로직 실행
        
        if (!CanAttack())
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[EliteAttackBehaviour] {gameObject.name}: 공격 불가 상태");
            }
            return;
        }
        
        // 공격 실행 (OnAttack 직접 호출)
        OnAttack();
    }

    public override bool CanAttack()
    {
        // 평타 또는 스킬 중 하나라도 사용 가능하면 true
        bool meleeReady = meleeAttack != null && meleeAttack.CanAttack();
        bool skillReady = HasAvailableSkill();
        
        return meleeReady || skillReady;
    }

    /// <summary>
    /// 사용 가능한 스킬이 있는지 확인
    /// </summary>
    private bool HasAvailableSkill()
    {
        if (skillController == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[EliteAttackBehaviour] HasAvailableSkill: skillController가 null!");
            return false;
        }
        
        if (baseEnemy?.EnemyData == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[EliteAttackBehaviour] HasAvailableSkill: EnemyData가 null!");
            return false;
        }
        
        if (!baseEnemy.EnemyData.HasSkillData)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[EliteAttackBehaviour] HasAvailableSkill: SkillDataList가 비어있음!");
            return false;
        }

        float distanceToPlayer = cachedPlayer != null ? 
            Vector2.Distance(baseEnemy.transform.position, cachedPlayer.transform.position) : 999f;

        if (enableDebugLogs)
        {
            Debug.Log($"[EliteAttackBehaviour] HasAvailableSkill 체크:");
            Debug.Log($"  - 플레이어 거리: {distanceToPlayer:F2}");
            Debug.Log($"  - 스킬 개수: {baseEnemy.EnemyData.SkillDataList.Count}");
        }

        // 하나라도 사용 가능한 스킬이 있으면 true
        foreach (var skill in baseEnemy.EnemyData.SkillDataList)
        {
            if (skill == null) continue;
            
            bool canUse = skillController.CanUseSkill(skill);
            bool inRange = skill.IsInRange(distanceToPlayer);
            
            if (enableDebugLogs)
            {
                Debug.Log($"  - {skill.SkillName}: CanUse={canUse}, InRange={inRange} (범위: {skill.MinRange}-{skill.MaxRange})");
            }
            
            if (canUse && inRange)
            {
                if (enableDebugLogs)
                    Debug.Log($"  ✅ {skill.SkillName} 사용 가능!");
                return true;
            }
        }

        if (enableDebugLogs)
            Debug.LogWarning($"[EliteAttackBehaviour] 사용 가능한 스킬 없음!");
        return false;
    }

    public override bool ShouldStopMovingWhileAttacking()
    {
        // 스킬 캐스팅 중이거나 실행 중이면 이동 정지
        if (skillController != null && 
            (skillController.IsCasting || skillController.IsActionExecuting))
        {
            return true;
        }

        // 평타는 AttackData 설정 따름
        if (meleeAttack != null && meleeAttack.AttackData != null)
        {
            return meleeAttack.AttackData.StopMovingWhileAttacking;
        }

        return true; // 기본값: 정지
    }

    #region 디버그 도구

    /// <summary>
    /// 통계 정보 출력
    /// </summary>
    [ContextMenu("Debug Attack Statistics")]
    private void DebugAttackStatistics()
    {
        if (statistics != null)
        {
            Debug.Log(statistics.GetSummary());
        }
        else
        {
            Debug.Log("[EliteAttackBehaviour] 통계 추적 비활성화 상태");
        }
    }

    /// <summary>
    /// 통계 초기화
    /// </summary>
    [ContextMenu("Reset Statistics")]
    private void ResetStatistics()
    {
        if (statistics != null)
        {
            statistics.Reset();
            Debug.Log("[EliteAttackBehaviour] 통계 초기화 완료");
        }
    }

    /// <summary>
    /// 강제 평타 테스트
    /// </summary>
    [ContextMenu("Test Force Melee")]
    private void TestForceMelee()
    {
        ExecuteMeleeAttack();
    }

    /// <summary>
    /// 강제 스킬 테스트 (첫 번째 스킬)
    /// </summary>
    [ContextMenu("Test Force Skill")]
    private void TestForceSkill()
    {
        if (baseEnemy?.EnemyData?.SkillDataList != null && baseEnemy.EnemyData.SkillDataList.Count > 0)
        {
            var firstSkill = baseEnemy.EnemyData.SkillDataList[0];
            if (firstSkill != null)
            {
                ExecuteSkillAttack(firstSkill);
            }
        }
        else
        {
            Debug.LogWarning("[EliteAttackBehaviour] 테스트할 스킬이 없습니다!");
        }
    }

    /// <summary>
    /// 현재 상태 출력
    /// </summary>
    [ContextMenu("Debug Current State")]
    private void DebugCurrentState()
    {
        string info = $"=== {baseEnemy?.gameObject.name} Elite Attack State ===\n";
        info += $"Can Attack: {CanAttack()}\n";
        info += $"Melee Ready: {meleeAttack != null && meleeAttack.CanAttack()}\n";
        info += $"Skill Ready: {HasAvailableSkill()}\n";
        info += $"Is Casting: {skillController?.IsCasting ?? false}\n";
        info += $"Is Action Executing: {skillController?.IsActionExecuting ?? false}\n";
        info += $"Should Stop Moving: {ShouldStopMovingWhileAttacking()}\n";
        
        if (baseEnemy?.EnemyData != null)
        {
            info += $"\n=== Attack Probabilities ===\n";
            info += $"Melee: {baseEnemy.EnemyData.MeleeAttackProbability}%\n";
            info += $"Skill: {baseEnemy.EnemyData.SkillUseProbability}%\n";
        }
        
        Debug.Log(info);
    }

    #endregion
}

