using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Phase 4: ConditionalModifier 시스템 테스트
/// ⚙️ Unity Editor Context Menu 또는 Play Mode에서 실행
/// </summary>
public class Phase4_ConditionalModifierTest : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private bool runOnStart = false;
    [SerializeField] private bool enableDetailedLogs = true;
    
    private void Start()
    {
        if (runOnStart)
        {
            RunAllTests();
        }
    }
    
    [ContextMenu("Run All Phase 4 Tests")]
    public void RunAllTests()
    {
        Debug.Log("========================================");
        Debug.Log("=== Phase 4: ConditionalModifier 테스트 시작 ===");
        Debug.Log("========================================\n");
        
        Test1_DatabaseInitialization();
        Test2_ConditionChecking();
        Test3_BossDamageBonus();
        Test4_LowHpDefense();
        Test5_CriticalWithCondition();
        Test6_DefenseIgnore();
        
        Debug.Log("\n========================================");
        Debug.Log("=== Phase 4 테스트 완료 ===");
        Debug.Log("========================================");
    }
    
    #region Test 1: Database 초기화
    
    [ContextMenu("Test 1: Database Initialization")]
    private void Test1_DatabaseInitialization()
    {
        Debug.Log("\n--- Test 1: ConditionalModifierDatabase 초기화 ---");
        
        var allModifiers = ConditionalModifierDatabase.GetAll();
        Debug.Log($"총 {allModifiers.Count}개 ConditionalModifier 로드됨");
        
        // Phase별 그룹화 확인
        for (int phase = 0; phase <= 7; phase++)
        {
            var modifiers = ConditionalModifierDatabase.GetModifiersByPhase(phase);
            if (modifiers.Count > 0)
            {
                Debug.Log($"  Phase {phase}: {modifiers.Count}개 모디파이어");
                foreach (var mod in modifiers)
                {
                    Debug.Log($"    - {mod.modifierId}: {mod.displayName}");
                }
            }
        }
        
        Debug.Log("✅ Test 1 완료\n");
    }
    
    #endregion
    
    #region Test 2: 조건 판정 테스트
    
    [ContextMenu("Test 2: Condition Checking")]
    private void Test2_ConditionChecking()
    {
        Debug.Log("\n--- Test 2: 조건 판정 테스트 ---");
        
        // Mock 보스 대상 생성
        var bossTarget = CreateMockBossTarget();
        
        // CombatContext 생성
        var context = new CombatContext
        {
            target = bossTarget,
            defenderHpPercent = 0.6f,   // 피격자(보스) HP: 60%
            attackerHpPercent = 0.4f,   // 공격자(플레이어) HP: 40% (저체력)
            isSkillAttack = false
        };
        
        Debug.Log("테스트 컨텍스트:");
        Debug.Log($"  - 대상: {(bossTarget.IsBoss() ? "보스" : "일반")}");
        Debug.Log($"  - 대상 HP: {context.defenderHpPercent:P0}");
        Debug.Log($"  - 자신 HP: {context.attackerHpPercent:P0}");
        
        // 조건 테스트
        bool isBoss = ConditionChecker.Check(EConditionType.TargetIsBoss, 0f, "", context, applyPhase: 3);
        bool isLowHp = ConditionChecker.Check(EConditionType.SelfHpBelow, 0.5f, "", context, applyPhase: 3);
        bool isTargetHighHp = ConditionChecker.Check(EConditionType.TargetHpAbove, 0.5f, "", context, applyPhase: 3);
        
        Debug.Log($"\n조건 판정 결과:");
        Debug.Log($"  - TargetIsBoss: {isBoss} (예상: true)");
        Debug.Log($"  - SelfHpBelow 50%: {isLowHp} (예상: true)");
        Debug.Log($"  - TargetHpAbove 50%: {isTargetHighHp} (예상: true)");
        
        Debug.Log("✅ Test 2 완료\n");
    }
    
    #endregion
    
    #region Test 3: 보스 대상 피해 증가
    
    [ContextMenu("Test 3: Boss Damage Bonus")]
    private void Test3_BossDamageBonus()
    {
        Debug.Log("\n--- Test 3: 보스 대상 피해 증가 ---");
        
        float baseDamage = 100f;
        
        // 일반 몬스터 대상
        var normalTarget = CreateMockNormalTarget();
        var normalResult = SimulateDamage(baseDamage, normalTarget, 1.0f, 0.8f);
        
        // 보스 대상
        var bossTarget = CreateMockBossTarget();
        var bossResult = SimulateDamage(baseDamage, bossTarget, 1.0f, 0.8f);
        
        Debug.Log($"기본 데미지: {baseDamage}");
        Debug.Log($"  - 일반 몬스터 → 최종: {normalResult.finalDamage} (조건: {normalResult.appliedConditions})");
        Debug.Log($"  - 보스 몬스터 → 최종: {bossResult.finalDamage} (조건: {bossResult.appliedConditions})");
        Debug.Log($"  - 보스 추가 피해: +{bossResult.finalDamage - normalResult.finalDamage} (+{((float)bossResult.finalDamage / normalResult.finalDamage - 1f) * 100:F1}%)");
        
        Debug.Log("✅ Test 3 완료\n");
    }
    
    #endregion
    
    #region Test 4: 저체력 방어 증가
    
    [ContextMenu("Test 4: Low HP Defense")]
    private void Test4_LowHpDefense()
    {
        Debug.Log("\n--- Test 4: 저체력 방어 증가 ---");
        
        float baseDamage = 100f;
        var bossTarget = CreateMockBossTarget();
        
        // 정상 체력 (80%)
        var normalHpResult = SimulateEnemyAttack(baseDamage, bossTarget, 0.8f, 0.8f);
        
        // 저체력 (25%)
        var lowHpResult = SimulateEnemyAttack(baseDamage, bossTarget, 0.8f, 0.25f);
        
        Debug.Log($"몬스터 공격력: {baseDamage}");
        Debug.Log($"  - 정상 체력 (80%) → 받는 피해: {normalHpResult.finalDamage}");
        Debug.Log($"  - 저체력 (25%) → 받는 피해: {lowHpResult.finalDamage} (조건: {lowHpResult.appliedConditions})");
        Debug.Log($"  - 피해 감소: -{normalHpResult.finalDamage - lowHpResult.finalDamage} (-{(1f - (float)lowHpResult.finalDamage / normalHpResult.finalDamage) * 100:F1}%)");
        
        Debug.Log("✅ Test 4 완료\n");
    }
    
    #endregion
    
    #region Test 5: 조건부 크리티컬
    
    [ContextMenu("Test 5: Critical with Condition")]
    private void Test5_CriticalWithCondition()
    {
        Debug.Log("\n--- Test 5: 조건부 크리티컬 ---");
        
        // 임시로 크리티컬 강제 발생 시뮬레이션
        Debug.Log("⚠️ 크리티컬은 확률 기반이므로 여러 번 실행 필요");
        Debug.Log("(Phase 4에서 보스 대상 크리티컬 피해 증가 모디파이어 확인)");
        
        Debug.Log("✅ Test 5 완료\n");
    }
    
    #endregion
    
    #region Test 6: 방어 무시
    
    [ContextMenu("Test 6: Defense Ignore")]
    private void Test6_DefenseIgnore()
    {
        Debug.Log("\n--- Test 6: 방어 무시 테스트 ---");
        
        Debug.Log("⚠️ 방어 무시는 BOSS_IGNORE_DEF 모디파이어 참조");
        Debug.Log("(현재 Phase 5에서 방어력 계산 시 IgnoreDefPercent 적용됨)");
        
        Debug.Log("✅ Test 6 완료\n");
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Mock 보스 대상 생성
    /// </summary>
    private IEnemyTarget CreateMockBossTarget()
    {
        return new MockEnemyTarget(EnemyType.Boss, 0.6f);
    }
    
    /// <summary>
    /// Mock 일반 몬스터 대상 생성
    /// </summary>
    private IEnemyTarget CreateMockNormalTarget()
    {
        return new MockEnemyTarget(EnemyType.Basic, 1.0f);
    }
    
    /// <summary>
    /// 플레이어 → 적 공격 시뮬레이션
    /// </summary>
    private CombatFormula.DamageResult SimulateDamage(
        float baseDamage,
        IEnemyTarget target,
        float selfHpPercent,
        float targetHpPercent
    )
    {
        var ctx = new CombatFormula.AttackContext
        {
            baseAttack = baseDamage,
            attackerClass = null,
            targetDefense = 0f,
            targetTransform = null,
            attackerTransform = null,
            isSkillAttack = false,
            skillMultiplier = 1.0f,
            criticalChance = 0f, // 크리티컬 off
            criticalMultiplier = 2.0f,
            isPlayerAttack = true,
            attackerLevel = 1,
            
            target = target,
            selfHpPercent = selfHpPercent,
            targetHpPercent = targetHpPercent
        };
        
        return CombatFormula.CalculatePlayerToEnemyDamage(ctx);
    }
    
    /// <summary>
    /// 적 → 플레이어 공격 시뮬레이션
    /// </summary>
    private CombatFormula.DamageResult SimulateEnemyAttack(
        float baseDamage,
        IEnemyTarget attacker,
        float attackerHpPercent,
        float playerHpPercent
    )
    {
        var ctx = new CombatFormula.AttackContext
        {
            baseAttack = baseDamage,
            attackerClass = null,
            targetDefense = 10f, // 플레이어 방어력 가정
            targetTransform = null,
            attackerTransform = null,
            isSkillAttack = false,
            skillMultiplier = 1.0f,
            criticalChance = 0f,
            criticalMultiplier = 2.0f,
            isPlayerAttack = false,
            attackerLevel = 1,
            
            target = null, // 플레이어는 IEnemyTarget 아님
            selfHpPercent = attackerHpPercent,
            targetHpPercent = playerHpPercent
        };
        
        return CombatFormula.CalculateEnemyToPlayerDamage(ctx);
    }
    
    #endregion
    
    #region Mock Classes
    
    /// <summary>
    /// 테스트용 Mock Enemy Target
    /// </summary>
    private class MockEnemyTarget : IEnemyTarget
    {
        private EnemyType type;
        private float hpPercent;
        
        public MockEnemyTarget(EnemyType type, float hpPercent)
        {
            this.type = type;
            this.hpPercent = hpPercent;
        }
        
        public EnemyType GetEnemyType() => type;
        public bool IsBoss() => type == EnemyType.Boss;
        public bool IsElite() => type == EnemyType.Elite;
        public float GetCurrentHpPercent() => hpPercent;
        public GameObject GetGameObject() => null;
    }
    
    #endregion
}

