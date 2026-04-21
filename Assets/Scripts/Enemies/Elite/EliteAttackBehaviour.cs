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
    
    [Header("⏱️ 전역 공격 쿨다운")]
    [Tooltip("모든 공격(평타/스킬) 후 대기 시간")]
    [SerializeField] private float globalAttackCooldown = 1.5f;
    private float lastAttackTime = -999f;
    
    [Header("🎲 공격 결정")]
    private EliteAttackDecision attackDecision;
    private EliteAttackDecision.AttackStatistics statistics;
    
    // EnemyAttackState가 Skip 여부를 즉시 확인하여 빈 대기를 없애기 위한 플래그
    private bool lastAttackSkipped = false;
    public bool LastAttackSkipped => lastAttackSkipped;
    
    [Header("🎮 디버그")]
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
        attackDecision = new EliteAttackDecision();
        
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
        
        Dbg.Log($"[EliteAttackBehaviour] {gameObject.name} 초기화 완료");
    }

    protected override void ValidateAttackType()
    {
        // 엘리트는 평타 + 스킬 혼합이므로 특별한 검증 불필요
    }

    protected override void OnAttack()
    {
        // BaseEnemy 참조 확인
        if (baseEnemy == null || cachedPlayer == null)
        {
                Debug.LogWarning($"[EliteAttackBehaviour] BaseEnemy 또는 Player가 null!");
            return;
        }

        // 거리 계산
        float distanceToPlayer = Vector2.Distance(baseEnemy.transform.position, cachedPlayer.transform.position);

        // 평타 사거리 (공격 결정에 사용)
        float meleeRange = GetMeleeRange();

        // 공격 결정 (거리 기반 + 확률 기반)
        var decision = attackDecision.DecideAttack(
            baseEnemy.EnemyData,
            skillController,
            distanceToPlayer,
            meleeRange
        );


        // 결정에 따라 실행
        if (decision.IsSkip)
        {
            lastAttackSkipped = true;
            lastAttackTime = Time.time - globalAttackCooldown * 0.5f;
            return;
        }
        else if (decision.IsSkill)
        {
            lastAttackSkipped = false;
            ExecuteSkillAttack(decision.SelectedSkill);

            if (trackStatistics && decision.SelectedSkill != null)
                statistics?.RecordSkill(decision.SelectedSkill.SkillName);
        }
        else
        {
            lastAttackSkipped = false;
            ExecuteMeleeAttack();

            if (trackStatistics)
                statistics?.RecordMelee();
        }
    }

    /// <summary>
    /// 평타 유효 사거리 반환 (EliteAttackDecision 및 EnemyChaseState에서 사용)
    /// </summary>
    public float GetMeleeRange()
    {
        if (meleeAttack != null && meleeAttack.AttackData != null)
            return meleeAttack.AttackData.AttackRange;
        return 1.8f;
    }
    
    /// <summary>
    /// 플레이어까지 거리 (CanAttack 내부 판단용)
    /// </summary>
    private float GetDistanceToPlayer()
    {
        if (cachedPlayer == null || baseEnemy == null) return 999f;
        return Vector2.Distance(baseEnemy.transform.position, cachedPlayer.transform.position);
    }
    
    #endregion

    /// <summary>
    /// 평타 실행
    /// </summary>
    private void ExecuteMeleeAttack()
    {
        if (meleeAttack == null)
        {
            return;
        }

        if (!meleeAttack.CanAttack())
        {
            return;
        }


        meleeAttack.Attack();
        
        // ⭐ 평타 후 전역 쿨다운 시작 (1.5초)
        lastAttackTime = Time.time;
        
    }

    /// <summary>
    /// 스킬 실행
    /// </summary>
    private void ExecuteSkillAttack(SkillData skill)
    {
        if (skillController == null)
        {
            return;
        }

        if (skill == null)
        {
            return;
        }


        skillController.StartSkillCast(skill);
        
        // ⭐ 수정: 전역 쿨다운은 스킬 완료 시점에 설정 (OnSkillComplete 콜백에서)
        // lastAttackTime은 OnSkillComplete()에서 설정됨
    }

    /// <summary>
    /// Attack 메서드 override (BaseAttackBehaviour의 기본 구현 대체)
    /// </summary>
    public override void Attack()
    {
        if (!CanAttack())
        {
            lastAttackSkipped = true;
            return;
        }
        
        OnAttack();
    }

    public override bool CanAttack()
    {
        if (IsGlobalCooldownActive()) return false;

        float dist = GetDistanceToPlayer();
        bool meleeReady = meleeAttack != null
            && meleeAttack.CanAttack()
            && dist <= GetMeleeRange();

        bool skillReady = HasAvailableSkill();

        return meleeReady || skillReady;
    }

    /// <summary>
    /// 전역 쿨다운이 아직 활성화 중인지 (Chase에서 이동/대기 판단용)
    /// </summary>
    public bool IsGlobalCooldownActive()
    {
        return Time.time < lastAttackTime + globalAttackCooldown;
    }

    /// <summary>
    /// 현재 위치에서 스킬이 준비됐는지 (Chase에서 stoppingDistance 결정용)
    /// </summary>
    public bool HasSkillReadyAndInRange()
    {
        if (IsGlobalCooldownActive()) return false;
        return HasAvailableSkill();
    }

    /// <summary>
    /// 사용 가능한 스킬이 있는지 확인
    /// </summary>
    private bool HasAvailableSkill()
    {
        if (skillController == null)
        {
                Debug.LogWarning($"[EliteAttackBehaviour] HasAvailableSkill: skillController가 null!");
            return false;
        }
        
        if (baseEnemy?.EnemyData == null)
        {
                Debug.LogWarning($"[EliteAttackBehaviour] HasAvailableSkill: EnemyData가 null!");
            return false;
        }
        
        if (!baseEnemy.EnemyData.HasSkillData)
            return false;

        float distanceToPlayer = cachedPlayer != null ? 
            Vector2.Distance(baseEnemy.transform.position, cachedPlayer.transform.position) : 999f;


        // 하나라도 사용 가능한 스킬이 있으면 true
        foreach (var skill in baseEnemy.EnemyData.SkillDataList)
        {
            if (skill == null) continue;
            
            bool canUse = skillController.CanUseSkill(skill);
            bool inRange = skill.IsInRange(distanceToPlayer);
            
            
            if (canUse && inRange)
                return true;
        }

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

    /// <summary>
    /// 스킬 완료 콜백 (EliteSkillController에서 호출)
    /// </summary>
    public void OnSkillComplete()
    {
        // ⭐⭐ 스킬 완료 시점에 전역 쿨다운 시작
        lastAttackTime = Time.time;
        
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
        }
        else
        {
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
            Dbg.Log("[EliteAttackBehaviour] 통계 초기화 완료");
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
        
    }

    #endregion
}

