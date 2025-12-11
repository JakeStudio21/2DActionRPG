using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 몬스터 공격 행동
/// BaseAttackBehaviour 상속하여 평타 + 페이즈별 스킬 통합 관리
/// 거리 기반 스킬 선택 + 개별 쿨다운 시스템
/// </summary>
public class BossAttackBehaviour : BaseAttackBehaviour
{
    [Header("⚡ 보스 전용")]
    [SerializeField] private BossPhaseController phaseController;
    [SerializeField] private BossSkillController skillController;
    [SerializeField] private MeleeAttack meleeAttack; // 평타용
    
    [Header("⏱️ 전역 공격 쿨다운")]
    [Tooltip("모든 공격(평타/스킬) 후 대기 시간")]
    [SerializeField] private float globalAttackCooldown = 1.5f;
    private float lastAttackTime = -999f;
    
    [Header("📏 거리 설정")]
    [Tooltip("근거리 공격 범위 (이 안에 있으면 근거리 스킬 사용)")]
    [SerializeField] private float meleeAttackRange = 6f;
    
    [Header("⏱️ 스킬 개별 쿨다운")]
    private Dictionary<SkillData, float> skillCooldowns = new Dictionary<SkillData, float>();
    
    [Header("🎮 디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    #region BaseAttackBehaviour 추상 메서드 구현
    
    protected override void OnInitialize()
    {
        // 컴포넌트 자동 참조
        if (phaseController == null)
            phaseController = GetComponent<BossPhaseController>();
        
        if (skillController == null)
            skillController = GetComponent<BossSkillController>();
        
        if (meleeAttack == null)
            meleeAttack = GetComponent<MeleeAttack>();
        
        // 평타 시스템 초기화
        if (meleeAttack != null)
        {
            meleeAttack.Initialize();
        }
        
        // 페이즈 컨트롤러 이벤트 구독
        if (phaseController != null)
        {
            phaseController.OnPhaseChanged += OnPhaseChanged;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BossAttackBehaviour] {gameObject.name} 초기화 완료");
            Debug.Log($"  - meleeAttackRange: {meleeAttackRange}f");
            Debug.Log($"  - globalAttackCooldown: {globalAttackCooldown}초");
        }
    }
    
    protected override void ValidateAttackType()
    {
        // 보스는 평타 + 페이즈별 스킬 혼합
        if (enableDebugLogs)
        {
            Debug.Log($"[BossAttackBehaviour] {gameObject.name}: 보스 공격 타입 (평타 + 페이즈별 스킬)");
        }
    }
    
    protected override void OnAttack()
    {
        // BaseEnemy 참조 확인
        if (baseEnemy == null || cachedPlayer == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[BossAttackBehaviour] BaseEnemy 또는 Player가 null!");
            }
            return;
        }
        
        // 페이즈 전환 중이면 공격 안 함
        if (phaseController != null && phaseController.IsTransitioning)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[BossAttackBehaviour] 페이즈 전환 중 - 공격 불가");
            }
            return;
        }
        
        // 현재 페이즈 가져오기
        BossPhaseData currentPhase = phaseController?.CurrentPhase;
        if (currentPhase == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[BossAttackBehaviour] 현재 페이즈가 없습니다!");
            }
            return;
        }
        
        // 거리 계산
        float distanceToPlayer = Vector2.Distance(baseEnemy.transform.position, cachedPlayer.transform.position);
        
        // 거리 분류
        BossSkillDistance distanceCategory = distanceToPlayer > meleeAttackRange ? 
            BossSkillDistance.Ranged : BossSkillDistance.Melee;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🎯 [BossAttackBehaviour] 거리: {distanceToPlayer:F1}f → {distanceCategory}");
        }
        
        // 공격 선택 (거리 기반 + 확률 기반)
        DecideAndExecuteAttack(currentPhase, distanceCategory, distanceToPlayer);
    }
    
    #endregion
    
    /// <summary>
    /// 공격 선택 및 실행 (거리 기반 + 확률 기반)
    /// </summary>
    private void DecideAndExecuteAttack(BossPhaseData phase, BossSkillDistance distanceCategory, float distanceToPlayer)
    {
        // 해당 거리에서 사용 가능한 스킬 필터링
        List<BossSkillEntry> availableSkills = GetAvailableSkills(phase, distanceCategory);
        
        // ⭐ 원거리에서는 평타 가중치 0으로 설정 (스킬만 사용)
        float meleeWeight = (distanceCategory == BossSkillDistance.Ranged) ? 0f : phase.meleeAttackWeight;
        
        // 가중치 기반 랜덤 선택
        float totalWeight = meleeWeight;
        
        // 사용 가능한 스킬 가중치 합산 (쿨다운 체크)
        List<BossSkillEntry> usableSkills = new List<BossSkillEntry>();
        foreach (var skillEntry in availableSkills)
        {
            if (CanUseSkill(skillEntry.skillData))
            {
                totalWeight += skillEntry.weight;
                usableSkills.Add(skillEntry);
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BossAttackBehaviour] 총 가중치: {totalWeight} (평타: {meleeWeight}, 스킬: {usableSkills.Count}개, 거리: {distanceCategory})");
        }
        
        // 가중치 기반 랜덤 선택
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        // 평타 확률 체크 (원거리면 meleeWeight=0이므로 건너뜀)
        currentWeight += meleeWeight;
        if (randomValue < currentWeight)
        {
            ExecuteMeleeAttack();
            return;
        }
        
        // 스킬 확률 체크
        foreach (var skillEntry in usableSkills)
        {
            currentWeight += skillEntry.weight;
            if (randomValue < currentWeight)
            {
                ExecuteSkillAttack(skillEntry);
                return;
            }
        }
        
        // Fallback: 평타
        if (enableDebugLogs)
        {
            Debug.LogWarning($"[BossAttackBehaviour] 공격 선택 실패 → 평타로 Fallback");
        }
        ExecuteMeleeAttack();
    }
    
    /// <summary>
    /// 해당 거리에서 사용 가능한 스킬 필터링
    /// </summary>
    private List<BossSkillEntry> GetAvailableSkills(BossPhaseData phase, BossSkillDistance distanceCategory)
    {
        List<BossSkillEntry> available = new List<BossSkillEntry>();
        
        foreach (var skillEntry in phase.availableSkills)
        {
            // 거리 조건 확인
            if (skillEntry.distanceType == distanceCategory || skillEntry.distanceType == BossSkillDistance.Both)
            {
                available.Add(skillEntry);
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BossAttackBehaviour] {distanceCategory} 거리 스킬: {available.Count}개");
            foreach (var skill in available)
            {
                Debug.Log($"  - {skill.skillData?.SkillName ?? "NULL"} (가중치: {skill.weight})");
            }
        }
        
        return available;
    }
    
    /// <summary>
    /// 평타 실행
    /// </summary>
    private void ExecuteMeleeAttack()
    {
        if (meleeAttack == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogError($"[BossAttackBehaviour] {gameObject.name}: MeleeAttack 컴포넌트 없음!");
            }
            return;
        }
        
        if (!meleeAttack.CanAttack())
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[BossAttackBehaviour] {gameObject.name}: 평타 쿨다운 중");
            }
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"⚔️ [BossAttackBehaviour] {gameObject.name}: 평타 실행");
        }
        
        meleeAttack.Attack();
        
        // ⭐ 평타 후 전역 쿨다운 시작
        lastAttackTime = Time.time;
    }
    
    /// <summary>
    /// 스킬 실행
    /// </summary>
    private void ExecuteSkillAttack(BossSkillEntry skillEntry)
    {
        if (skillEntry == null || skillEntry.skillData == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogError($"[BossAttackBehaviour] SkillEntry 또는 SkillData가 null!");
            }
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"🔮 [BossAttackBehaviour] {gameObject.name}: {skillEntry.skillData.SkillName} 실행 (스케일: {skillEntry.skillScaleMultiplier}x)");
        }
        
        // ⭐ BossSkillController로 스킬 실행
        if (skillController != null)
        {
            skillController.StartSkillCast(skillEntry);
        }
        else
        {
            Debug.LogError($"[BossAttackBehaviour] BossSkillController가 없습니다!");
        }
        
        // 스킬 쿨다운 시작
        if (skillCooldowns.ContainsKey(skillEntry.skillData))
        {
            skillCooldowns[skillEntry.skillData] = skillEntry.individualCooldown;
        }
        else
        {
            skillCooldowns.Add(skillEntry.skillData, skillEntry.individualCooldown);
        }
        
        // ⭐ 스킬 완료는 나중에 BossSkillController에서 OnSkillComplete 콜백으로 처리
        // lastAttackTime은 OnSkillComplete()에서 설정됨
    }
    
    /// <summary>
    /// 스킬 사용 가능 여부 확인 (개별 쿨다운)
    /// </summary>
    private bool CanUseSkill(SkillData skill)
    {
        if (skill == null) return false;
        
        // 쿨다운 체크
        if (skillCooldowns.ContainsKey(skill))
        {
            bool canUse = skillCooldowns[skill] <= 0f;
            if (!canUse && enableDebugLogs)
            {
                Debug.Log($"[BossAttackBehaviour] {skill.SkillName} 쿨다운 중: {skillCooldowns[skill]:F1}초");
            }
            return canUse;
        }
        
        // 딕셔너리에 없으면 사용 가능
        return true;
    }
    
    private void Update()
    {
        // 스킬 쿨다운 업데이트
        UpdateSkillCooldowns();
    }
    
    /// <summary>
    /// 스킬 쿨다운 타이머 업데이트
    /// </summary>
    private void UpdateSkillCooldowns()
    {
        List<SkillData> skills = new List<SkillData>(skillCooldowns.Keys);
        
        foreach (var skill in skills)
        {
            if (skillCooldowns[skill] > 0f)
            {
                skillCooldowns[skill] -= Time.deltaTime;
                
                if (skillCooldowns[skill] <= 0f)
                {
                    skillCooldowns[skill] = 0f;
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[BossAttackBehaviour] {skill.SkillName} 쿨다운 완료!");
                    }
                }
            }
        }
    }
    
    public override void Attack()
    {
        if (!CanAttack())
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[BossAttackBehaviour] {gameObject.name}: 공격 불가 상태");
            }
            return;
        }
        
        // 공격 실행
        OnAttack();
    }
    
    public override bool CanAttack()
    {
        // ⭐ 전역 공격 쿨다운 체크
        if (Time.time < lastAttackTime + globalAttackCooldown)
        {
            if (enableDebugLogs)
            {
                float remaining = (lastAttackTime + globalAttackCooldown) - Time.time;
                Debug.Log($"[BossAttackBehaviour] 전역 쿨다운 중 (남은 시간: {remaining:F1}초)");
            }
            return false;
        }
        
        // 페이즈 전환 중이면 공격 불가
        if (phaseController != null && phaseController.IsTransitioning)
        {
            return false;
        }
        
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
        if (phaseController == null || phaseController.CurrentPhase == null)
        {
            return false;
        }
        
        BossPhaseData currentPhase = phaseController.CurrentPhase;
        
        // 하나라도 사용 가능한 스킬이 있으면 true
        foreach (var skillEntry in currentPhase.availableSkills)
        {
            if (skillEntry.skillData != null && CanUseSkill(skillEntry.skillData))
            {
                return true;
            }
        }
        
        return false;
    }
    
    public override bool ShouldStopMovingWhileAttacking()
    {
        // ⭐ 스킬 캐스팅/실행 중이면 이동 정지
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
    /// 스킬 완료 콜백 (BossSkillController에서 호출)
    /// </summary>
    public void OnSkillComplete()
    {
        // ⭐⭐ 스킬 완료 시점에 전역 쿨다운 시작
        lastAttackTime = Time.time;
        
        if (enableDebugLogs)
        {
            Debug.Log($"⏰ [BossAttackBehaviour] {gameObject.name} 스킬 완료 → 전역 쿨다운 {globalAttackCooldown}초 시작!");
        }
    }
    
    /// <summary>
    /// 모든 쿨다운 리셋 (페이즈 전환 시)
    /// </summary>
    public void ResetAllCooldowns()
    {
        // 스킬 쿨다운 리셋
        List<SkillData> skills = new List<SkillData>(skillCooldowns.Keys);
        foreach (var skill in skills)
        {
            skillCooldowns[skill] = 0f;
        }
        
        // 전역 쿨다운 리셋
        lastAttackTime = -999f;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🔄 [BossAttackBehaviour] 모든 쿨다운 리셋 완료!");
        }
    }
    
    /// <summary>
    /// 페이즈 변경 콜백
    /// </summary>
    private void OnPhaseChanged(BossPhaseData newPhase)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"📊 [BossAttackBehaviour] 페이즈 변경: {newPhase.phaseName}");
            Debug.Log($"   평타 확률: {newPhase.meleeAttackWeight}");
            Debug.Log($"   스킬 개수: {newPhase.availableSkills.Count}");
        }
        
        // 새 페이즈의 스킬들을 쿨다운 딕셔너리에 추가
        foreach (var skillEntry in newPhase.availableSkills)
        {
            if (skillEntry.skillData != null && !skillCooldowns.ContainsKey(skillEntry.skillData))
            {
                skillCooldowns.Add(skillEntry.skillData, 0f);
            }
        }
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (phaseController != null)
        {
            phaseController.OnPhaseChanged -= OnPhaseChanged;
        }
    }
    
    #region 디버그 도구
    
    [ContextMenu("Debug Attack State")]
    private void DebugAttackState()
    {
        string info = $"=== {baseEnemy?.gameObject.name} Boss Attack State ===\n";
        info += $"Can Attack: {CanAttack()}\n";
        info += $"Melee Ready: {meleeAttack != null && meleeAttack.CanAttack()}\n";
        info += $"Skill Ready: {HasAvailableSkill()}\n";
        info += $"Is Transitioning: {phaseController?.IsTransitioning ?? false}\n";
        info += $"Global Cooldown: {Mathf.Max(0, (lastAttackTime + globalAttackCooldown) - Time.time):F1}초\n\n";
        
        if (phaseController != null && phaseController.CurrentPhase != null)
        {
            info += phaseController.CurrentPhase.GetDebugInfo();
        }
        
        info += "\n=== Skill Cooldowns ===\n";
        foreach (var kvp in skillCooldowns)
        {
            info += $"{kvp.Key.SkillName}: {kvp.Value:F1}초\n";
        }
        
        Debug.Log(info);
    }
    
    #endregion
}

