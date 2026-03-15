using UnityEngine;
using System.Linq;

/// <summary>
/// 전투 공식 표준 시스템
/// Step 1~6(7) 순차 데미지 계산
/// 확장 포인트: 룬 시스템, 고급 아이템 옵션 대비
/// </summary>
public static class CombatFormula
{
    #region 데이터 구조체
    
    /// <summary>
    /// 공격 입력 데이터
    /// ⚙️ Phase 4: ConditionalModifier 지원을 위한 필드 추가
    /// </summary>
    public struct AttackContext
    {
        public float baseAttack;              // Step 1: 기본 공격력
        public IPlayerClass attackerClass;    // Step 3, 4, 5: 클래스 효과용 (null 가능)
        public float targetDefense;           // Step 6: 대상 방어력
        public Transform targetTransform;     // Step 4: 백어택 각도 판정용
        public Transform attackerTransform;   // Step 4: 백어택 각도 판정용
        public bool isSkillAttack;            // Step 2: 스킬 여부
        public float skillMultiplier;         // Step 2: 스킬 배율 (기본 1.0)
        public float criticalChance;          // Step 5: 크리티컬 확률
        public float criticalMultiplier;      // Step 5: 크리티컬 배율
        public bool isPlayerAttack;           // 플레이어 공격 = true, 몬스터 = false
        public int attackerLevel;             // Step 6: 공격자 레벨 (Dynamic K 계산용)
        
        // ⚙️ Phase 4: ConditionalModifier용 추가 필드
        public IEnemyTarget target;           // 피격자 (보스/엘리트 구분용)
        public float selfHpPercent;           // 공격자 HP 비율 (0.0~1.0)
        public float targetHpPercent;         // 피격자 HP 비율 (0.0~1.0)
    }
    
    /// <summary>
    /// 피격 이펙트 분기용 데미지 출처 타입
    /// </summary>
    public enum DamageSourceType
    {
        Normal = 0,     // 일반 근접/원거리 타격
        Critical = 1,   // 크리티컬 타격
        DOT_Burn = 2,   // 화상 틱 데미지
        DOT_Poison = 3  // 중독 틱 데미지
    }

    /// <summary>
    /// 데미지 계산 결과
    /// ⚙️ Phase 4: ConditionalModifier 적용 결과 추가
    /// </summary>
    public struct DamageResult
    {
        public int finalDamage;               // 최종 데미지 (정수)
        
        // 단계별 중간 결과 (디버그용)
        public float damageAfterStep1;        // Step 1: 기본 공격력
        public float damageAfterStep2;        // Step 2: 스킬 배율 적용 후
        public float damageAfterStep3;        // Step 3: 증폭 배율 적용 후
        public float damageAfterStep4;        // Step 4: 백어택 적용 후
        public float damageAfterStep5;        // Step 5: 크리티컬 적용 후
        public float damageAfterStep6;        // Step 6: 방어력 감소 후
        
        // 판정 결과
        public bool isCritical;               // 크리티컬 발생 여부
        public bool isBackAttack;             // 백어택 발생 여부
        public bool isBlocked;                // 블록 발생 여부 (몬스터→플레이어만)
        public float defenseReduction;        // 방어력 감소율 (0.0~1.0)
        public float totalMultiplier;         // 총 배율 (최종/기본)
        
        // ⚙️ Phase 4: 조건부 모디파이어 적용 결과 (UI/디버그용)
        public string appliedConditions;      // 적용된 조건 목록 (예: "BOSS_DMG_UP, LOW_HP_DR")
        
        // ⚙️ Phase 4-C: Phase 0 면역 시스템
        public string resistedEffects;        // 저항한 상태이상 목록 (예: "Bind, Poison")
        public bool hasImmunity;              // 면역 효과 발동 여부
        
        // ⚙️ Phase 4-C: Phase 7 후처리 시스템
        public float lifeStealAmount;         // 흡혈량 (가한 데미지 × 흡혈 비율)
        public float healingBlockPercent;     // 회복 차단 비율 (0.0~1.0)
        
        // ⭐ 피격 이펙트 정보 (피격자가 발행할 수 있도록)
        public Vector3 hitPosition;           // 피격 지점 (이펙트 발행 위치)
        public ItemGrade attackerGrade;       // 공격자 무기/발사체 등급
        public DamageSourceType sourceType;   // 이펙트 분기용 데미지 출처 타입
    }
    
    #endregion
    
    #region 설정값
    
    private static CombatFormulaConfig _config;
    
    /// <summary>
    /// 설정 로드 (Resources 폴더에서 자동 로드)
    /// </summary>
    private static CombatFormulaConfig Config
    {
        get
        {
            if (_config == null)
            {
                _config = Resources.Load<CombatFormulaConfig>("Combat/CombatFormulaConfig");
                if (_config == null)
                {
                    Debug.LogWarning("[CombatFormula] CombatFormulaConfig를 찾을 수 없습니다. 기본값 사용.");
                }
            }
            return _config;
        }
    }
    
    /// <summary>
    /// 기본 방어력 상수 (기본 100)
    /// </summary>
    private static float BaseDefenseConstant => Config != null ? Config.baseDefenseConstant : 100f;
    
    /// <summary>
    /// 레벨당 방어력 상수 증가량 (기본 10)
    /// </summary>
    private static float DefenseConstantPerLevel => Config != null ? Config.defenseConstantPerLevel : 10f;
    
    /// <summary>
    /// 상세 로그 활성화 여부
    /// </summary>
    private static bool EnableDetailedLogs => Config != null ? Config.enableDetailedLogs : false;
    
    #endregion
    
    #region 메인 계산 메서드
    
    /// <summary>
    /// 플레이어 → 몬스터 데미지 계산
    /// ⚙️ Phase 4: ConditionalModifier 통합
    /// </summary>
    public static DamageResult CalculatePlayerToEnemyDamage(AttackContext ctx)
    {
        var result = new DamageResult();
        System.Text.StringBuilder appliedConditions = null; // Lazy 초기화
        
        // ⚙️ CombatContext 생성 (조건부 모디파이어용)
        CombatContext combatCtx = CreateCombatContext(ctx);
        
        if (EnableDetailedLogs)
            Debug.Log($"[CombatFormula] === 플레이어 → 몬스터 데미지 계산 시작 ===");
        
        // ⚙️ Phase 0: 면역 체크 (데미지 계산 전)
        var (hasImmunity, resistedEffects) = CheckImmunityEffects(combatCtx, phase: 0);
        result.hasImmunity = hasImmunity;
        result.resistedEffects = resistedEffects;
        
        if (hasImmunity && EnableDetailedLogs)
            Debug.Log($"[CombatFormula] Phase 0: 면역 효과 발동 - {resistedEffects}");
        
        // Step 1: 기본 공격력
        float damage = ctx.baseAttack;
        result.damageAfterStep1 = damage;
        
        if (EnableDetailedLogs)
            Debug.Log($"[CombatFormula] Step 1: 기본 공격력 = {damage:F1}");
        
        // Step 2: 스킬 배율
        damage = ApplySkillMultiplier(damage, ctx.isSkillAttack, ctx.skillMultiplier);
        result.damageAfterStep2 = damage;
        
        if (EnableDetailedLogs)
            Debug.Log($"[CombatFormula] Step 2: 스킬 배율 ({ctx.skillMultiplier}x) = {damage:F1}");
        
        // Step 3: 증폭 배율 (1버킷)
        damage = ApplyMultiplicativeBonus(damage, ctx.attackerClass);
        
        // ⚙️ Phase 3: 조건부 모디파이어 적용 (DamageDealtMult 등)
        var (modifiedDamage, conditionsPhase3) = ApplyConditionalModifiers(damage, combatCtx, phase: 3, EEffectType.DamageDealtMult);
        damage = modifiedDamage;
        if (!string.IsNullOrEmpty(conditionsPhase3))
        {
            if (appliedConditions == null) appliedConditions = new System.Text.StringBuilder();
            appliedConditions.Append(conditionsPhase3);
        }
        
        result.damageAfterStep3 = damage;
        
        if (EnableDetailedLogs)
            Debug.Log($"[CombatFormula] Step 3: 증폭 배율 (조건부 포함) = {damage:F1}");
        
        // Step 4: 백어택 (Assasin 전용)
        bool isBackAttack = false;
        if (ctx.attackerClass != null && ctx.attackerClass.ClassName == "Assasin")
        {
            isBackAttack = CheckBackAttack(ctx.attackerTransform, ctx.targetTransform);
            if (isBackAttack)
            {
                damage = ApplyBackAttack(damage, ctx.attackerClass);
            }
        }
        combatCtx.isBackAttack = isBackAttack; // CombatContext 업데이트
        result.isBackAttack = isBackAttack;
        result.damageAfterStep4 = damage;
        
        if (EnableDetailedLogs && isBackAttack)
            Debug.Log($"[CombatFormula] Step 4: 백어택! = {damage:F1}");
        
        // Step 5: 크리티컬
        // ⚙️ Phase 4: 조건부 크리티컬 확률 보너스 적용
        float finalCritChance = ctx.criticalChance + GetConditionalCritBonus(combatCtx);
        bool isCritical = RollCritical(finalCritChance);
        
        if (isCritical)
        {
            damage = ApplyCritical(damage, ctx.criticalMultiplier);
            
            // ⚙️ Phase 4: 조건부 크리티컬 데미지 증가 (CritDamageMult)
            var (critModifiedDamage, conditionsPhase4) = ApplyConditionalModifiers(damage, combatCtx, phase: 4, EEffectType.CritDamageMult);
            damage = critModifiedDamage;
            if (!string.IsNullOrEmpty(conditionsPhase4))
            {
                if (appliedConditions == null) appliedConditions = new System.Text.StringBuilder();
                else appliedConditions.Append(", ");
                appliedConditions.Append(conditionsPhase4);
            }
        }
        
        combatCtx.isCritical = isCritical; // CombatContext 업데이트
        result.isCritical = isCritical;
        result.damageAfterStep5 = damage;
        
        if (EnableDetailedLogs && isCritical)
            Debug.Log($"[CombatFormula] Step 5: 크리티컬! (조건부 포함) ({ctx.criticalMultiplier}x) = {damage:F1}");
        
        // Step 6: 방어력 감소
        // ⚙️ Phase 5: 조건부 방어 무시 적용
        float defenseIgnore = GetConditionalDefenseIgnore(combatCtx);
        float effectiveDefense = ctx.targetDefense * (1f - defenseIgnore);
        
        float defenseReduction = CalculateDefenseReduction(effectiveDefense, ctx.attackerLevel);
        damage = ApplyDefenseReduction(damage, defenseReduction);
        result.defenseReduction = defenseReduction;
        result.damageAfterStep6 = damage;
        
        if (EnableDetailedLogs)
        {
            if (defenseIgnore > 0)
                Debug.Log($"[CombatFormula] Step 6: 방어 무시 {defenseIgnore*100:F1}% → 유효 방어력 {effectiveDefense:F1}");
            Debug.Log($"[CombatFormula] Step 6: 방어력 감소 (Dynamic K, Lv{ctx.attackerLevel}) ({defenseReduction*100:F1}%) = {damage:F1}");
        }
        
        // 최종 데미지 확정
        result.finalDamage = FinalizeDamage(damage);
        result.totalMultiplier = result.damageAfterStep1 > 0 ? damage / result.damageAfterStep1 : 1f;
        result.appliedConditions = appliedConditions?.ToString() ?? "";
        
        // ⚙️ Phase 7: 후처리 (흡혈, 회복 차단)
        var (lifeStealAmount, healingBlockPercent) = ApplyPostDamageEffects(result.finalDamage, combatCtx, phase: 7);
        result.lifeStealAmount = lifeStealAmount;
        result.healingBlockPercent = healingBlockPercent;
        
        if (EnableDetailedLogs)
        {
            Debug.Log($"[CombatFormula] === 최종 데미지: {result.finalDamage} (배율: {result.totalMultiplier:F2}x) ===");
            if (!string.IsNullOrEmpty(result.appliedConditions))
                Debug.Log($"[CombatFormula] 적용된 조건: {result.appliedConditions}");
            if (lifeStealAmount > 0)
                Debug.Log($"[CombatFormula] Phase 7: 흡혈 {lifeStealAmount:F1}");
            if (healingBlockPercent > 0)
                Debug.Log($"[CombatFormula] Phase 7: 회복 차단 {healingBlockPercent * 100:F0}%");
        }
        
        return result;
    }
    
    /// <summary>
    /// 몬스터 → 플레이어 데미지 계산
    /// ⚙️ Phase 4: ConditionalModifier 통합 (받는 피해 감소)
    /// </summary>
    public static DamageResult CalculateEnemyToPlayerDamage(AttackContext ctx)
    {
        var result = new DamageResult();
        System.Text.StringBuilder appliedConditions = null; // Lazy 초기화
        
        // ⚙️ CombatContext 생성 (조건부 모디파이어용)
        // 주의: 몬스터 공격이므로 target은 null (플레이어는 IEnemyTarget이 아님)
        CombatContext combatCtx = CreateCombatContext(ctx);
        
        if (EnableDetailedLogs)
            Debug.Log($"[CombatFormula] === 몬스터 → 플레이어 데미지 계산 시작 ===");
        
        // ⚙️ Phase 0: 면역 체크 (데미지 계산 전) - 플레이어는 보스 공격에 대한 면역 가능
        var (hasImmunity, resistedEffects) = CheckImmunityEffects(combatCtx, phase: 0);
        result.hasImmunity = hasImmunity;
        result.resistedEffects = resistedEffects;
        
        if (hasImmunity && EnableDetailedLogs)
            Debug.Log($"[CombatFormula] Phase 0: 면역 효과 발동 - {resistedEffects}");
        
        // Step 1: 기본 공격력 (몬스터)
        float damage = ctx.baseAttack;
        result.damageAfterStep1 = damage;
        
        if (EnableDetailedLogs)
            Debug.Log($"[CombatFormula] Step 1: 기본 공격력 = {damage:F1}");
        
        // Step 2: 스킬 배율 (몬스터 스킬)
        damage = ApplySkillMultiplier(damage, ctx.isSkillAttack, ctx.skillMultiplier);
        result.damageAfterStep2 = damage;
        
        if (EnableDetailedLogs && ctx.isSkillAttack)
            Debug.Log($"[CombatFormula] Step 2: 스킬 배율 ({ctx.skillMultiplier}x) = {damage:F1}");
        
        // Step 3, 4: 몬스터는 증폭/백어택 없음
        result.damageAfterStep3 = damage;
        result.damageAfterStep4 = damage;
        
        // Step 5: 크리티컬
        bool isCritical = RollCritical(ctx.criticalChance);
        if (isCritical)
        {
            damage = ApplyCritical(damage, ctx.criticalMultiplier);
        }
        combatCtx.isCritical = isCritical; // CombatContext 업데이트
        result.isCritical = isCritical;
        result.sourceType = isCritical ? DamageSourceType.Critical : DamageSourceType.Normal;
        result.damageAfterStep5 = damage;
        
        if (EnableDetailedLogs && isCritical)
            Debug.Log($"[CombatFormula] Step 5: 크리티컬! ({ctx.criticalMultiplier}x) = {damage:F1}");
        
        // Step 6: 방어력 감소 (플레이어 방어력)
        float defenseReduction = CalculateDefenseReduction(ctx.targetDefense, ctx.attackerLevel);
        damage = ApplyDefenseReduction(damage, defenseReduction);
        
        // ⚙️ Phase 5: 조건부 피해 감소 적용 (DamageTakenReduce)
        // 예: 저체력(30% 이하) 시 받는 피해 25% 감소, 보스 공격 피해 25% 감소
        var (reducedDamage, conditionsPhase5) = ApplyConditionalModifiers(damage, combatCtx, phase: 5, EEffectType.DamageTakenReduce);
        damage = reducedDamage;
        if (!string.IsNullOrEmpty(conditionsPhase5))
        {
            if (appliedConditions == null) appliedConditions = new System.Text.StringBuilder();
            appliedConditions.Append(conditionsPhase5);
        }
        
        result.defenseReduction = defenseReduction;
        result.damageAfterStep6 = damage;
        
        if (EnableDetailedLogs)
        {
            Debug.Log($"[CombatFormula] Step 6: 방어력 감소 ({defenseReduction*100:F1}%) = {damage:F1}");
            if (!string.IsNullOrEmpty(conditionsPhase5))
                Debug.Log($"[CombatFormula] Step 6: 조건부 피해 감소 적용 = {damage:F1}");
        }
        
        // Step 7: 블록 판정 (Warrior 전용) - PlayerHealth에서 처리하므로 여기서는 스킵
        // 이유: 블록은 피격자(PlayerHealth)에서 처리하는 것이 구조상 자연스러움
        result.isBlocked = false;
        
        // 최종 데미지 확정
        result.finalDamage = FinalizeDamage(damage);
        result.totalMultiplier = result.damageAfterStep1 > 0 ? damage / result.damageAfterStep1 : 1f;
        result.appliedConditions = appliedConditions?.ToString() ?? "";
        
        // ⚙️ Phase 7: 후처리 (흡혈, 회복 차단) - 몬스터가 플레이어를 공격할 때도 적용 가능 (예: 엘리트 몬스터 흡혈)
        var (lifeStealAmount, healingBlockPercent) = ApplyPostDamageEffects(result.finalDamage, combatCtx, phase: 7);
        result.lifeStealAmount = lifeStealAmount;
        result.healingBlockPercent = healingBlockPercent;
        
        if (EnableDetailedLogs)
        {
            Debug.Log($"[CombatFormula] === 최종 데미지: {result.finalDamage} (배율: {result.totalMultiplier:F2}x) ===");
            if (!string.IsNullOrEmpty(result.appliedConditions))
                Debug.Log($"[CombatFormula] 적용된 조건: {result.appliedConditions}");
            if (lifeStealAmount > 0)
                Debug.Log($"[CombatFormula] Phase 7: 흡혈 {lifeStealAmount:F1}");
            if (healingBlockPercent > 0)
                Debug.Log($"[CombatFormula] Phase 7: 회복 차단 {healingBlockPercent * 100:F0}%");
        }
        
        return result;
    }
    
    #endregion
    
    #region Step별 개별 메서드
    
    /// <summary>
    /// Step 2: 스킬 배율 적용
    /// </summary>
    private static float ApplySkillMultiplier(float damage, bool isSkillAttack, float skillMultiplier)
    {
        if (!isSkillAttack || skillMultiplier <= 0)
            return damage;
        
        return damage * skillMultiplier;
    }
    
    /// <summary>
    /// Step 3: 증폭 배율 적용 (1버킷 - 모두 합산)
    /// </summary>
    private static float ApplyMultiplicativeBonus(float damage, IPlayerClass playerClass)
    {
        if (playerClass == null)
            return damage;
        
        float bonusTotal = 0f;
        
        // 1. 클래스 기본 배율
        float classMultiplier = playerClass.AttackPowerMultiplier - 1f; // 1.2 → 0.2 (20%)
        bonusTotal += classMultiplier;
        
        // 2. Warrior 버서커 모드 (체력 30% 이하)
        if (playerClass.ClassName == "Warrior")
        {
            var warrior = playerClass as Warrior;
            if (warrior != null && warrior.IsInBerserkerMode())
            {
                bonusTotal += 0.5f; // +50%
                
                if (EnableDetailedLogs)
                    Debug.Log($"[CombatFormula] 버서커 모드 활성화! (+50%)");
            }
        }
        
        // 3. [확장 예정] 룬 시스템, 버프 등
        // TODO: ICombatModifier 인터페이스 구현 시 추가
        
        // 최종 적용: damage × (1 + bonusTotal)
        return damage * (1f + bonusTotal);
    }
    
    /// <summary>
    /// Step 4: 백어택 체크 (Assasin 전용)
    /// </summary>
    private static bool CheckBackAttack(Transform attacker, Transform target)
    {
        if (attacker == null || target == null)
            return false;
        
        // 공격자 → 대상 방향
        Vector3 attackDirection = (target.position - attacker.position).normalized;
        
        // 대상이 바라보는 방향 (2D에서는 localScale.x로 판단)
        Vector3 targetForward = target.localScale.x > 0 ? Vector3.right : Vector3.left;
        
        // 내적으로 각도 계산 (같은 방향을 보고 있으면 백어택)
        float dot = Vector3.Dot(attackDirection, targetForward);
        
        // 120도 범위 (cos(120°) ≈ -0.5)
        // dot > 0.5면 뒤쪽에서 공격
        return dot > 0.5f;
    }
    
    /// <summary>
    /// Step 4: 백어택 배율 적용
    /// </summary>
    private static float ApplyBackAttack(float damage, IPlayerClass playerClass)
    {
        if (playerClass == null || playerClass.ClassName != "Assasin")
            return damage;
        
        // Assasin.BackAttackBonus (기본 1.5배)
        var assasin = playerClass as Assasin;
        if (assasin != null)
        {
            return damage * assasin.BackAttackBonus;
        }
        
        return damage * 1.5f; // Fallback
    }
    
    /// <summary>
    /// Step 5: 크리티컬 판정
    /// </summary>
    private static bool RollCritical(float criticalChance)
    {
        if (criticalChance <= 0)
            return false;
        
        return Random.Range(0f, 1f) < criticalChance;
    }
    
    /// <summary>
    /// Step 5: 크리티컬 배율 적용
    /// </summary>
    private static float ApplyCritical(float damage, float criticalMultiplier)
    {
        return damage * criticalMultiplier;
    }
    
    /// <summary>
    /// Step 6: 방어율 계산 (Dynamic K)
    /// 공식: K = baseK + (attackerLevel × kGainPerLevel)
    ///       방어율 = 방어력 / (방어력 + K)
    /// </summary>
    private static float CalculateDefenseReduction(float defense, int attackerLevel)
    {
        if (defense <= 0)
            return 0f;
        
        // Dynamic K: 공격자 레벨에 비례하여 방어력 관통력 증가
        float dynamicK = BaseDefenseConstant + (attackerLevel * DefenseConstantPerLevel);
        return defense / (defense + dynamicK);
    }
    
    /// <summary>
    /// Step 6: 방어력 감소 적용
    /// 공식: 최종 데미지 = 공격력 × (1 - 방어율)
    /// </summary>
    private static float ApplyDefenseReduction(float damage, float defenseReduction)
    {
        return damage * (1f - defenseReduction);
    }
    
    /// <summary>
    /// 최종 데미지 확정 (정수 변환 + 최소값 보장)
    /// </summary>
    private static int FinalizeDamage(float damage)
    {
        int finalDamage = Mathf.RoundToInt(damage);
        
        // 최소 데미지 1 보장
        int minDamage = Config != null ? Config.minDamage : 1;
        finalDamage = Mathf.Max(minDamage, finalDamage);
        
        // 최대 데미지 제한 (옵션)
        if (Config != null && Config.maxDamage > 0)
        {
            finalDamage = Mathf.Min(finalDamage, Config.maxDamage);
        }
        
        return finalDamage;
    }
    
    #endregion
    
    #region ⚙️ Phase 4: ConditionalModifier 시스템
    
    /// <summary>
    /// AttackContext에서 CombatContext 생성
    /// </summary>
    private static CombatContext CreateCombatContext(AttackContext ctx)
    {
        return new CombatContext
        {
            attackerClass = ctx.attackerClass,
            attackerHpPercent = ctx.selfHpPercent,    // 공격자 HP (명확한 필드명)
            attackerPosition = ctx.attackerTransform != null ? ctx.attackerTransform.position : Vector3.zero,
            attackerName = ctx.attackerTransform != null ? ctx.attackerTransform.name : "Unknown",
            
            target = ctx.target,
            defenderHpPercent = ctx.targetHpPercent,  // 피격자 HP (명확한 필드명)
            targetPosition = ctx.targetTransform != null ? ctx.targetTransform.position : Vector3.zero,
            targetName = ctx.targetTransform != null ? ctx.targetTransform.name : "Unknown",
            
            isSkillAttack = ctx.isSkillAttack,
            isBackAttack = false, // Phase 4에서 설정됨
            isCritical = false,   // Phase 5에서 설정됨
            damageType = "MELEE"  // TODO: AttackContext에서 전달받도록 확장
        };
    }
    
    /// <summary>
    /// 조건부 모디파이어 적용 (특정 Phase용)
    /// ⚡ GC 최적화: 캐시된 리스트 재사용, foreach 최적화
    /// </summary>
    /// <param name="baseDamage">현재 데미지</param>
    /// <param name="context">전투 컨텍스트</param>
    /// <param name="phase">적용할 Phase (3, 4, 5 등)</param>
    /// <param name="effectFilter">적용할 효과 타입 필터 (null이면 모두 적용)</param>
    /// <returns>수정된 데미지 및 적용된 모디파이어 목록</returns>
    private static (float damage, string appliedIds) ApplyConditionalModifiers(
        float baseDamage,
        CombatContext context,
        int phase,
        EEffectType? effectFilter = null
    )
    {
        // Phase별 모디파이어 가져오기 (캐시됨, GC 없음)
        var modifiers = ConditionalModifierDatabase.GetModifiersByPhase(phase);
        
        if (modifiers == null || modifiers.Count == 0)
            return (baseDamage, "");
        
        float finalDamage = baseDamage;
        System.Text.StringBuilder appliedIds = null; // Lazy 초기화로 GC 최소화
        
        // ⚡ foreach 최적화: 리스트가 캐시되어 있으므로 GC 없음
        foreach (var modifier in modifiers)
        {
            // 효과 타입 필터링 (필요 시)
            if (effectFilter.HasValue && modifier.effectType != effectFilter.Value)
                continue;
            
            // 조건 판정
            if (!modifier.IsConditionMet(context))
                continue;
            
            // 효과 적용
            float appliedValue = ApplyModifierEffect(modifier, finalDamage, context);
            finalDamage = appliedValue;
            
            // 적용된 모디파이어 기록 (Lazy 초기화)
            if (appliedIds == null)
                appliedIds = new System.Text.StringBuilder();
            else
                appliedIds.Append(", ");
            
            appliedIds.Append(modifier.modifierId);
            
            if (EnableDetailedLogs)
            {
                Debug.Log($"[CombatFormula] 조건부 모디파이어 적용: {modifier.modifierId} " +
                         $"({modifier.effectType} {modifier.value:+0.##}) → {finalDamage:F1}");
            }
        }
        
        return (finalDamage, appliedIds?.ToString() ?? "");
    }
    
    /// <summary>
    /// 개별 모디파이어 효과 적용
    /// </summary>
    private static float ApplyModifierEffect(ConditionalModifier modifier, float currentDamage, CombatContext context)
    {
        switch (modifier.effectType)
        {
            case EEffectType.DamageDealtMult:
                // 주는 피해 증가: damage × (1 + value)
                // 예: 0.2 → ×1.2 (20% 증가)
                return currentDamage * (1f + modifier.value);
            
            case EEffectType.DamageTakenReduce:
                // 받는 피해 감소: damage × (1 - value)
                // 예: 0.25 → ×0.75 (25% 감소)
                return currentDamage * (1f - modifier.value);
            
            case EEffectType.IgnoreDefPercent:
                // 방어 무시는 방어력 계산에서 처리되므로 여기서는 스킵
                // TODO: Phase5에서 방어력 계산 시 적용
                return currentDamage;
            
            case EEffectType.CritDamageMult:
                // 크리티컬 피해 증가: damage × (1 + value)
                // 예: 0.3 → ×1.3 (30% 증가)
                return currentDamage * (1f + modifier.value);
            
            case EEffectType.CritRateBonus:
                // 크리티컬 확률 증가는 RollCritical에서 처리되므로 여기서는 스킵
                return currentDamage;
            
            // === 상태이상 면역 (Phase0_Event에서 처리) ===
            case EEffectType.Immunity_Bind:
            case EEffectType.Immunity_Slow:
            case EEffectType.Immunity_Poison:
            case EEffectType.Immunity_Burn:
                // 데미지에 영향 없음 (상태이상 시스템에서 처리)
                return currentDamage;
            
            // === 회복/흡혈 (Phase7_Post에서 처리) ===
            case EEffectType.BlockHealingPercent:
            case EEffectType.LifeStealFromDamagePerSec:
            case EEffectType.LifeStealImmediate:
                // 데미지에 영향 없음 (힐링 시스템에서 처리)
                return currentDamage;
            
            default:
                Debug.LogWarning($"[CombatFormula] 미구현 효과 타입: {modifier.effectType}");
                return currentDamage;
        }
    }
    
    /// <summary>
    /// 조건부 크리티컬 확률 보너스 계산
    /// Phase 4용: CritRateBonus 효과 적용
    /// </summary>
    private static float GetConditionalCritBonus(CombatContext context)
    {
        var modifiers = ConditionalModifierDatabase.GetModifiersByPhase(4); // Phase4_Crit
        float bonus = 0f;
        
        foreach (var modifier in modifiers)
        {
            if (modifier.effectType == EEffectType.CritRateBonus && modifier.IsConditionMet(context))
            {
                bonus += modifier.value;
            }
        }
        
        return bonus;
    }
    
    /// <summary>
    /// 조건부 방어 무시 계산
    /// Phase 5용: IgnoreDefPercent 효과 적용
    /// </summary>
    private static float GetConditionalDefenseIgnore(CombatContext context)
    {
        var modifiers = ConditionalModifierDatabase.GetModifiersByPhase(5); // Phase5_Defense
        float ignorePercent = 0f;
        
        foreach (var modifier in modifiers)
        {
            if (modifier.effectType == EEffectType.IgnoreDefPercent && modifier.IsConditionMet(context))
            {
                ignorePercent += modifier.value;
            }
        }
        
        return Mathf.Clamp01(ignorePercent); // 최대 100%
    }
    
    #endregion
    
    #region ⚙️ Phase 4-C: Phase 0 & Phase 7 시스템
    
    /// <summary>
    /// Phase 0: 면역 효과 체크
    /// 룬 시스템에서 보스 공격에 대한 상태이상 면역 판정
    /// </summary>
    /// <param name="context">전투 컨텍스트</param>
    /// <param name="phase">Phase 번호 (0 = Event)</param>
    /// <returns>(hasImmunity: 면역 발동 여부, resistedEffects: 저항한 효과 목록)</returns>
    private static (bool hasImmunity, string resistedEffects) CheckImmunityEffects(CombatContext context, int phase)
    {
        // PlayerRuntimeStats에서 활성 조건부 모디파이어 가져오기
        var playerStats = UnityEngine.Object.FindObjectOfType<PlayerRuntimeStats>();
        if (playerStats == null)
        {
            return (false, "");
        }
        
        var activeModifiers = playerStats.GetActiveConditionalModifiers();
        if (activeModifiers == null || activeModifiers.Count == 0)
        {
            return (false, "");
        }
        
        // Phase 0 면역 모디파이어 필터링
        var immunityModifiers = activeModifiers
            .Where(m => m.applyPhase == phase && IsImmunityEffect(m.effectType))
            .ToList();
        
        if (immunityModifiers.Count == 0)
        {
            return (false, "");
        }
        
        // 조건 체크 및 면역 효과 수집
        var resistedList = new System.Collections.Generic.List<string>();
        
        foreach (var modifier in immunityModifiers)
        {
            if (modifier.IsConditionMet(context))
            {
                // 면역 효과 추출 (예: Immunity_Bind → "Bind")
                string effectName = ExtractImmunityEffectName(modifier.effectType);
                if (!string.IsNullOrEmpty(effectName))
                {
                    resistedList.Add(effectName);
                }
            }
        }
        
        if (resistedList.Count > 0)
        {
            string resistedEffects = string.Join(", ", resistedList);
            return (true, resistedEffects);
        }
        
        return (false, "");
    }
    
    /// <summary>
    /// Phase 7: 후처리 효과 적용
    /// 흡혈(LifeSteal), 회복 차단(BlockHealing) 계산
    /// </summary>
    /// <param name="finalDamage">최종 데미지</param>
    /// <param name="context">전투 컨텍스트</param>
    /// <param name="phase">Phase 번호 (7 = Post)</param>
    /// <returns>(lifeStealAmount: 흡혈량, healingBlockPercent: 회복 차단 비율)</returns>
    private static (float lifeStealAmount, float healingBlockPercent) ApplyPostDamageEffects(int finalDamage, CombatContext context, int phase)
    {
        // PlayerRuntimeStats에서 활성 조건부 모디파이어 가져오기
        var playerStats = UnityEngine.Object.FindObjectOfType<PlayerRuntimeStats>();
        if (playerStats == null)
        {
            return (0f, 0f);
        }
        
        var activeModifiers = playerStats.GetActiveConditionalModifiers();
        if (activeModifiers == null || activeModifiers.Count == 0)
        {
            return (0f, 0f);
        }
        
        // Phase 7 후처리 모디파이어 필터링
        var postModifiers = activeModifiers
            .Where(m => m.applyPhase == phase)
            .ToList();
        
        if (postModifiers.Count == 0)
        {
            return (0f, 0f);
        }
        
        float lifeStealPercent = 0f;
        float healingBlockPercent = 0f;
        
        foreach (var modifier in postModifiers)
        {
            if (!modifier.IsConditionMet(context))
                continue;
            
            switch (modifier.effectType)
            {
                case EEffectType.LifeStealFromDamagePerSec:
                    // 흡혈: 가한 데미지의 N%를 회복
                    lifeStealPercent += modifier.value;
                    break;
                
                case EEffectType.BlockHealingPercent:
                    // 회복 차단: 대상의 회복 효과를 N% 차단
                    healingBlockPercent += modifier.value;
                    break;
            }
        }
        
        // 흡혈량 계산
        float lifeStealAmount = finalDamage * lifeStealPercent;
        
        // 회복 차단 비율 제한 (최대 100%)
        healingBlockPercent = Mathf.Clamp01(healingBlockPercent);
        
        return (lifeStealAmount, healingBlockPercent);
    }
    
    /// <summary>
    /// 면역 효과 타입인지 확인
    /// </summary>
    private static bool IsImmunityEffect(EEffectType effectType)
    {
        return effectType == EEffectType.Immunity_Bind ||
               effectType == EEffectType.Immunity_Slow ||
               effectType == EEffectType.Immunity_Poison ||
               effectType == EEffectType.Immunity_Burn;
    }
    
    /// <summary>
    /// 면역 효과 타입에서 효과 이름 추출
    /// 예: Immunity_Bind → "Bind"
    /// </summary>
    private static string ExtractImmunityEffectName(EEffectType effectType)
    {
        string fullName = effectType.ToString();
        if (fullName.StartsWith("Immunity_"))
        {
            return fullName.Substring(9); // "Immunity_" 제거
        }
        return "";
    }
    
    #endregion
}

