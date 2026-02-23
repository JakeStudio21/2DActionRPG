using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Phase 4-C: 룬 시스템 테스트
/// 룬 장착/해제, Phase 0 면역, Phase 7 후처리 검증
/// ⚙️ Tools → Phase 4-C Test 메뉴로 실행
/// </summary>
public class Phase4C_RuneSystemTest : MonoBehaviour
{
    [Header("=== 테스트 설정 ===")]
    [Tooltip("테스트용 룬 데이터 (Inspector에서 할당)")]
    public List<RuneData> testRunes;
    
    [ContextMenu("Run All Tests")]
    public void RunAllTests()
    {
        Debug.Log("========================================");
        Debug.Log("🧪 Phase 4-C: 룬 시스템 테스트 시작");
        Debug.Log("========================================\n");
        
        // 초기화
        ConditionalModifierDatabase.PrintDebugInfo();
        
        // 테스트 실행
        Test1_RuneDataValidation();
        Test2_RuneManagerBasics();
        Test3_PlayerRuntimeStatsIntegration();
        Test4_Phase0_ImmunityEffects();
        Test5_Phase7_LifeStealAndHealBlock();
        Test6_CombatFormulaIntegration();
        
        Debug.Log("\n========================================");
        Debug.Log("✅ Phase 4-C: 모든 테스트 완료!");
        Debug.Log("========================================");
    }
    
    #region 테스트 1: RuneData 검증
    
    [ContextMenu("Test 1: RuneData Validation")]
    private void Test1_RuneDataValidation()
    {
        Debug.Log("\n=== Test 1: RuneData 검증 ===");
        
        // 자동으로 Resources/Runes 폴더에서 룬 로드
        if (testRunes == null || testRunes.Count == 0)
        {
            Debug.Log("  testRunes가 비어있음. Resources/Runes에서 자동 로드 시도...");
            testRunes = new List<RuneData>(Resources.LoadAll<RuneData>("Runes"));
            
            if (testRunes.Count == 0)
            {
                Debug.LogWarning("  ⚠️ Resources/Runes 폴더에서 룬을 찾을 수 없습니다.");
                Debug.LogWarning("  Tools → Generate Test Runes를 실행하여 테스트용 룬을 생성하세요.");
                return;
            }
            
            Debug.Log($"  ✅ {testRunes.Count}개 룬 자동 로드 완료!");
        }
        
        foreach (var rune in testRunes)
        {
            if (rune == null) continue;
            
            Debug.Log($"  검증: {rune}");
            
            // 유효성 체크
            if (!rune.IsValid())
            {
                Debug.LogError($"  ❌ 유효하지 않은 룬: {rune.name}");
                continue;
            }
            
            // ConditionalModifier ID 검증
            foreach (var modId in rune.ConditionalModifierIds)
            {
                var mod = ConditionalModifierDatabase.GetModifierById(modId);
                if (mod != null)
                {
                    Debug.Log($"    ✅ 모디파이어 발견: {mod.displayName} ({mod.effectType})");
                }
                else
                {
                    Debug.LogWarning($"    ⚠️ 모디파이어를 찾을 수 없음: {modId}");
                }
            }
        }
        
        Debug.Log("Test 1 완료.\n");
    }
    
    #endregion
    
    #region 테스트 2: RuneManager 기본 기능
    
    [ContextMenu("Test 2: RuneManager Basics")]
    private void Test2_RuneManagerBasics()
    {
        Debug.Log("\n=== Test 2: RuneManager 기본 기능 ===");
        
        var runeManager = RuneManager.Instance;
        if (runeManager == null)
        {
            Debug.LogError("RuneManager를 찾을 수 없습니다!");
            return;
        }
        
        // 초기화
        runeManager.UnequipAllRunes();
        
        // 자동으로 Resources/Runes 폴더에서 룬 로드
        if (testRunes == null || testRunes.Count == 0)
        {
            Debug.Log("  testRunes가 비어있음. Resources/Runes에서 자동 로드 시도...");
            testRunes = new List<RuneData>(Resources.LoadAll<RuneData>("Runes"));
            
            if (testRunes.Count == 0)
            {
                Debug.LogWarning("  ⚠️ Resources/Runes 폴더에서 룬을 찾을 수 없습니다.");
                Debug.LogWarning("  Tools → Generate Test Runes를 실행하여 테스트용 룬을 생성하세요.");
                return;
            }
            
            Debug.Log($"  ✅ {testRunes.Count}개 룬 자동 로드 완료!");
        }
        
        // 룬 장착
        var rune1 = testRunes[0];
        bool equipped = runeManager.EquipRune(rune1, 0);
        Debug.Log($"  룬 장착 (슬롯 0): {equipped} - {rune1.runeName}");
        
        // 활성 룬 확인
        var activeRunes = runeManager.GetActiveRunes();
        Debug.Log($"  활성 룬 개수: {activeRunes.Count}");
        
        // 조건부 모디파이어 확인
        var activeModifiers = runeManager.GetActiveConditionalModifiers();
        Debug.Log($"  활성 조건부 모디파이어: {activeModifiers.Count}개");
        foreach (var mod in activeModifiers)
        {
            Debug.Log($"    - {mod.displayName} ({mod.effectType})");
        }
        
        // 룬 해제
        bool unequipped = runeManager.UnequipRune(0);
        Debug.Log($"  룬 해제 (슬롯 0): {unequipped}");
        
        Debug.Log("Test 2 완료.\n");
    }
    
    #endregion
    
    #region 테스트 3: PlayerRuntimeStats 통합
    
    [ContextMenu("Test 3: PlayerRuntimeStats Integration")]
    private void Test3_PlayerRuntimeStatsIntegration()
    {
        Debug.Log("\n=== Test 3: PlayerRuntimeStats 통합 ===");
        
        var playerStats = FindObjectOfType<PlayerRuntimeStats>();
        if (playerStats == null)
        {
            Debug.LogWarning("PlayerRuntimeStats를 찾을 수 없습니다. (로비에서는 정상)");
            return;
        }
        
        // 테스트용 조건부 모디파이어 추가
        var testModifier = ConditionalModifierDatabase.GetModifierById("BOSS_DMG_UP");
        if (testModifier != null)
        {
            playerStats.AddConditionalModifier(testModifier);
            Debug.Log($"  조건부 모디파이어 추가: {testModifier.displayName}");
            
            var activeModifiers = playerStats.GetActiveConditionalModifiers();
            Debug.Log($"  활성 조건부 모디파이어: {activeModifiers.Count}개");
            
            // 정리
            playerStats.ClearAllConditionalModifiers();
            Debug.Log($"  조건부 모디파이어 클리어 완료");
        }
        else
        {
            Debug.LogWarning("BOSS_DMG_UP 모디파이어를 찾을 수 없습니다.");
        }
        
        Debug.Log("Test 3 완료.\n");
    }
    
    #endregion
    
    #region 테스트 4: Phase 0 면역 효과
    
    [ContextMenu("Test 4: Phase 0 Immunity Effects")]
    private void Test4_Phase0_ImmunityEffects()
    {
        Debug.Log("\n=== Test 4: Phase 0 면역 효과 ===");
        
        // 가상 보스 타겟 생성
        var bossTarget = new MockEnemyTarget("TestBoss", EnemyType.Boss, 1.0f);
        
        // 전투 컨텍스트 생성
        var context = new CombatContext
        {
            target = bossTarget,
            defenderHpPercent = 1.0f,
            attackerHpPercent = 0.5f,
            isSkillAttack = false,
            damageType = "MELEE"
        };
        
        Debug.Log($"  시나리오: 보스 공격 (플레이어 HP 50%)");
        
        // 면역 효과 없이 테스트
        Debug.Log($"  면역 효과 없음:");
        var (hasImmunity1, resistedEffects1) = CheckImmunityEffectsPublic(context, phase: 0);
        Debug.Log($"    - hasImmunity: {hasImmunity1}");
        Debug.Log($"    - resistedEffects: {resistedEffects1}");
        
        // 면역 효과 추가 (IMMUNE_BIND, IMMUNE_POISON)
        var playerStats = FindObjectOfType<PlayerRuntimeStats>();
        if (playerStats != null)
        {
            var immuneBind = ConditionalModifierDatabase.GetModifierById("IMMUNE_BIND");
            var immunePoison = ConditionalModifierDatabase.GetModifierById("IMMUNE_POISON");
            
            if (immuneBind != null) playerStats.AddConditionalModifier(immuneBind);
            if (immunePoison != null) playerStats.AddConditionalModifier(immunePoison);
            
            Debug.Log($"  면역 효과 추가 (Bind, Poison):");
            var (hasImmunity2, resistedEffects2) = CheckImmunityEffectsPublic(context, phase: 0);
            Debug.Log($"    - hasImmunity: {hasImmunity2}");
            Debug.Log($"    - resistedEffects: {resistedEffects2}");
            
            // 정리
            playerStats.ClearAllConditionalModifiers();
        }
        else
        {
            Debug.LogWarning("PlayerRuntimeStats를 찾을 수 없어 면역 테스트 스킵.");
        }
        
        Debug.Log("Test 4 완료.\n");
    }
    
    // 테스트용 Public 래퍼 (CombatFormula의 private 메서드 접근 불가)
    private (bool hasImmunity, string resistedEffects) CheckImmunityEffectsPublic(CombatContext context, int phase)
    {
        // 실제로는 CombatFormula.CalculatePlayerToEnemyDamage를 호출해서 result.hasImmunity를 확인해야 함
        // 여기서는 간단히 PlayerRuntimeStats에서 직접 체크
        
        var playerStats = FindObjectOfType<PlayerRuntimeStats>();
        if (playerStats == null)
            return (false, "");
        
        var activeModifiers = playerStats.GetActiveConditionalModifiers();
        var immunityModifiers = activeModifiers
            .FindAll(m => m.applyPhase == phase && IsImmunityEffect(m.effectType));
        
        if (immunityModifiers.Count == 0)
            return (false, "");
        
        var resistedList = new List<string>();
        foreach (var modifier in immunityModifiers)
        {
            if (modifier.IsConditionMet(context))
            {
                string effectName = modifier.effectType.ToString().Replace("Immunity_", "");
                resistedList.Add(effectName);
            }
        }
        
        if (resistedList.Count > 0)
            return (true, string.Join(", ", resistedList));
        
        return (false, "");
    }
    
    private bool IsImmunityEffect(EEffectType effectType)
    {
        return effectType == EEffectType.Immunity_Bind ||
               effectType == EEffectType.Immunity_Slow ||
               effectType == EEffectType.Immunity_Poison ||
               effectType == EEffectType.Immunity_Burn;
    }
    
    #endregion
    
    #region 테스트 5: Phase 7 흡혈 & 회복 차단
    
    [ContextMenu("Test 5: Phase 7 LifeSteal and Heal Block")]
    private void Test5_Phase7_LifeStealAndHealBlock()
    {
        Debug.Log("\n=== Test 5: Phase 7 흡혈 & 회복 차단 ===");
        
        // 가상 보스 타겟 생성
        var bossTarget = new MockEnemyTarget("TestBoss", EnemyType.Boss, 1.0f);
        
        // 전투 컨텍스트 생성
        var context = new CombatContext
        {
            target = bossTarget,
            defenderHpPercent = 1.0f,
            attackerHpPercent = 1.0f,
            isSkillAttack = false,
            damageType = "MELEE"
        };
        
        int testDamage = 100;
        Debug.Log($"  시나리오: 보스에게 {testDamage} 데미지 가함");
        
        // Phase 7 효과 없이 테스트
        Debug.Log($"  Phase 7 효과 없음:");
        var (lifeSteal1, healBlock1) = ApplyPostDamageEffectsPublic(testDamage, context, phase: 7);
        Debug.Log($"    - 흡혈: {lifeSteal1:F1}");
        Debug.Log($"    - 회복 차단: {healBlock1 * 100:F0}%");
        
        // Phase 7 효과 추가
        var playerStats = FindObjectOfType<PlayerRuntimeStats>();
        if (playerStats != null)
        {
            var dotLifeSteal = ConditionalModifierDatabase.GetModifierById("BOSS_DOT_LIFESTEAL");
            var blockHeal = ConditionalModifierDatabase.GetModifierById("BOSS_BLOCK_HEAL");
            
            if (dotLifeSteal != null) playerStats.AddConditionalModifier(dotLifeSteal);
            if (blockHeal != null) playerStats.AddConditionalModifier(blockHeal);
            
            Debug.Log($"  Phase 7 효과 추가 (DOT 흡혈 2%, 회복 차단 50%):");
            var (lifeSteal2, healBlock2) = ApplyPostDamageEffectsPublic(testDamage, context, phase: 7);
            Debug.Log($"    - 흡혈: {lifeSteal2:F1} (기대값: {testDamage * 0.02f:F1})");
            Debug.Log($"    - 회복 차단: {healBlock2 * 100:F0}% (기대값: 50%)");
            
            // 정리
            playerStats.ClearAllConditionalModifiers();
        }
        else
        {
            Debug.LogWarning("PlayerRuntimeStats를 찾을 수 없어 Phase 7 테스트 스킵.");
        }
        
        Debug.Log("Test 5 완료.\n");
    }
    
    // 테스트용 Public 래퍼
    private (float lifeStealAmount, float healingBlockPercent) ApplyPostDamageEffectsPublic(int finalDamage, CombatContext context, int phase)
    {
        var playerStats = FindObjectOfType<PlayerRuntimeStats>();
        if (playerStats == null)
            return (0f, 0f);
        
        var activeModifiers = playerStats.GetActiveConditionalModifiers();
        var postModifiers = activeModifiers.FindAll(m => m.applyPhase == phase);
        
        if (postModifiers.Count == 0)
            return (0f, 0f);
        
        float lifeStealPercent = 0f;
        float healingBlockPercent = 0f;
        
        foreach (var modifier in postModifiers)
        {
            if (!modifier.IsConditionMet(context))
                continue;
            
            switch (modifier.effectType)
            {
                case EEffectType.LifeStealFromDamagePerSec:
                    lifeStealPercent += modifier.value;
                    break;
                
                case EEffectType.BlockHealingPercent:
                    healingBlockPercent += modifier.value;
                    break;
            }
        }
        
        float lifeStealAmount = finalDamage * lifeStealPercent;
        healingBlockPercent = Mathf.Clamp01(healingBlockPercent);
        
        return (lifeStealAmount, healingBlockPercent);
    }
    
    #endregion
    
    #region 테스트 6: CombatFormula 통합
    
    [ContextMenu("Test 6: CombatFormula Integration")]
    private void Test6_CombatFormulaIntegration()
    {
        Debug.Log("\n=== Test 6: CombatFormula 통합 ===");
        
        // 가상 보스 타겟 생성
        var bossTarget = new MockEnemyTarget("IntegrationTestBoss", EnemyType.Boss, 0.6f);
        
        var playerStats = FindObjectOfType<PlayerRuntimeStats>();
        if (playerStats == null)
        {
            Debug.LogWarning("PlayerRuntimeStats를 찾을 수 없습니다. 통합 테스트 스킵.");
            return;
        }
        
        // 룬 효과 추가 (BOSS_DMG_UP, IMMUNE_BIND, BOSS_DOT_LIFESTEAL)
        var bossDmgUp = ConditionalModifierDatabase.GetModifierById("BOSS_DMG_UP");
        var immuneBind = ConditionalModifierDatabase.GetModifierById("IMMUNE_BIND");
        var dotLifeSteal = ConditionalModifierDatabase.GetModifierById("BOSS_DOT_LIFESTEAL");
        
        if (bossDmgUp != null) playerStats.AddConditionalModifier(bossDmgUp);
        if (immuneBind != null) playerStats.AddConditionalModifier(immuneBind);
        if (dotLifeSteal != null) playerStats.AddConditionalModifier(dotLifeSteal);
        
        Debug.Log($"  룬 효과 추가: BOSS_DMG_UP, IMMUNE_BIND, BOSS_DOT_LIFESTEAL");
        
        // CombatFormula로 데미지 계산
        var ctx = new CombatFormula.AttackContext
        {
            baseAttack = 50f,
            isSkillAttack = false,
            skillMultiplier = 1.0f,
            attackerClass = null,
            target = bossTarget,
            selfHpPercent = 1.0f,
            targetHpPercent = 0.6f,
            attackerTransform = this.transform,
            targetTransform = this.transform,
            isPlayerAttack = true,
            attackerLevel = 1
        };
        
        var result = CombatFormula.CalculatePlayerToEnemyDamage(ctx);
        
        Debug.Log($"  === 데미지 계산 결과 ===");
        Debug.Log($"  최종 데미지: {result.finalDamage}");
        Debug.Log($"  배율: {result.totalMultiplier:F2}x");
        Debug.Log($"  적용된 조건: {result.appliedConditions}");
        Debug.Log($"  면역 발동: {result.hasImmunity}");
        Debug.Log($"  저항한 효과: {result.resistedEffects}");
        Debug.Log($"  흡혈량: {result.lifeStealAmount:F1}");
        Debug.Log($"  회복 차단: {result.healingBlockPercent * 100:F0}%");
        
        // 정리
        playerStats.ClearAllConditionalModifiers();
        
        Debug.Log("Test 6 완료.\n");
    }
    
    #endregion
    
    #region Mock 클래스
    
    /// <summary>
    /// 테스트용 MockEnemyTarget
    /// </summary>
    private class MockEnemyTarget : IEnemyTarget
    {
        private string name;
        private EnemyType enemyType;
        private float hpPercent;
        
        public MockEnemyTarget(string name, EnemyType type, float hpPercent)
        {
            this.name = name;
            this.enemyType = type;
            this.hpPercent = hpPercent;
        }
        
        public EnemyType GetEnemyType() => enemyType;
        public bool IsBoss() => enemyType == EnemyType.Boss;
        public bool IsElite() => enemyType == EnemyType.Elite;
        public float GetCurrentHpPercent() => hpPercent;
        public GameObject GetGameObject() => null;
    }
    
    #endregion
}

