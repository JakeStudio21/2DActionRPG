using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 룬 시스템 Phase 4 테스트 메뉴
/// ⚙️ Unity Editor 메뉴에서 전투 시스템 연동 테스트
/// 
/// 메뉴 위치: Tools/Rune System/Test Phase 4/...
/// </summary>
public class RunePhase4TestMenu : Editor
{
    private const string MENU_ROOT = "Tools/Rune System/Test Phase 4/";
    
    #region 1. 스탯 스케일링 테스트
    
    [MenuItem(MENU_ROOT + "1-1. 룬 장착 및 스탯 스케일링 확인")]
    public static void Test_EquipAndCheckScaling()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 1-1: 룬 장착 및 스탯 스케일링 확인 ==========");
        
        var allRunes = RuneInventoryManager.Instance.GetAllRunes();
        
        if (allRunes.Count == 0)
        {
            Debug.LogWarning("⚠️ 인벤토리가 비어있습니다. 먼저 Phase 2 메뉴로 룬을 추가해주세요.");
            return;
        }
        
        // 첫 번째 룬 선택
        RuneInstance targetRune = allRunes[0];
        Debug.Log($"대상 룬: {targetRune}");
        Debug.Log($"  기본 데이터: {targetRune.baseData.runeName}");
        Debug.Log($"  주옵션: {targetRune.baseData.MainStatModifierId}");
        Debug.Log($"  레벨 배율: {targetRune.GetMainStatMultiplier():F3}x");
        Debug.Log($"  부옵션: {targetRune.allocatedSubStatModifierIds.Count}개\n");
        
        // 슬롯 0에 장착
        bool success = RuneManager.Instance.EquipRune(targetRune, 0);
        
        if (success)
        {
            Debug.Log($"✅ 장착 성공!\n");
            
            // 조건부 모디파이어 확인
            var modifiers = RuneManager.Instance.GetActiveConditionalModifiers();
            Debug.Log($"활성 조건부 모디파이어: {modifiers.Count}개");
            
            foreach (var mod in modifiers)
            {
                Debug.Log($"  [{mod.modifierId}] {mod.displayName}");
                Debug.Log($"    값: {mod.value:F3} ({mod.unit})");
                Debug.Log($"    조건: {mod.conditionType} | 효과: {mod.effectType}");
                Debug.Log($"    출처: {mod.source}");
            }
            
            // PlayerRuntimeStats 연동 확인
            var playerStats = Object.FindObjectOfType<PlayerRuntimeStats>();
            if (playerStats != null)
            {
                var runtimeModifiers = playerStats.GetActiveConditionalModifiers();
                Debug.Log($"\n✅ PlayerRuntimeStats 연동 확인: {runtimeModifiers.Count}개 모디파이어");
                
                if (runtimeModifiers.Count != modifiers.Count)
                {
                    Debug.LogWarning($"⚠️ 모디파이어 개수 불일치! RuneManager: {modifiers.Count}개, PlayerRuntimeStats: {runtimeModifiers.Count}개");
                }
                else
                {
                    Debug.Log("🎉 모디파이어 동기화 완료!");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ PlayerRuntimeStats를 찾을 수 없습니다. (로비에서는 정상)");
            }
        }
        else
        {
            Debug.LogError("❌ 장착 실패!");
        }
        
        Debug.Log("==========================================");
    }
    
    [MenuItem(MENU_ROOT + "1-2. 레벨별 스케일링 비교 (Lv.1 vs Lv.15)")]
    public static void Test_LevelScalingComparison()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 1-2: 레벨별 스케일링 비교 ==========");
        
        var allRunes = RuneInventoryManager.Instance.GetAllRunes();
        var groupedRunes = allRunes.GroupBy(r => r.baseDataId).ToList();
        
        if (groupedRunes.Count < 2)
        {
            Debug.LogWarning("⚠️ 같은 종류의 룬이 2개 이상 필요합니다.");
            return;
        }
        
        // 같은 종류에서 레벨이 다른 룬 2개 찾기
        RuneInstance lowLevelRune = null;
        RuneInstance highLevelRune = null;
        
        foreach (var group in groupedRunes)
        {
            var runeList = group.OrderBy(r => r.currentLevel).ToList();
            if (runeList.Count >= 2)
            {
                lowLevelRune = runeList[0];
                highLevelRune = runeList[runeList.Count - 1];
                break;
            }
        }
        
        if (lowLevelRune == null || highLevelRune == null)
        {
            Debug.LogWarning("⚠️ 레벨이 다른 같은 종류의 룬을 찾을 수 없습니다.");
            return;
        }
        
        Debug.Log($"비교 대상:");
        Debug.Log($"  룬 A: {lowLevelRune}");
        Debug.Log($"    레벨 배율: {lowLevelRune.GetMainStatMultiplier():F3}x");
        Debug.Log($"  룬 B: {highLevelRune}");
        Debug.Log($"    레벨 배율: {highLevelRune.GetMainStatMultiplier():F3}x\n");
        
        // 원본 모디파이어 값 확인
        string mainModId = lowLevelRune.baseData.MainStatModifierId;
        var originalMod = ConditionalModifierDatabase.GetModifierById(mainModId);
        
        if (originalMod != null)
        {
            Debug.Log($"원본 모디파이어: [{mainModId}] {originalMod.displayName}");
            Debug.Log($"  원본 값: {originalMod.value:F3}");
            
            float scaledValueLow = originalMod.value * lowLevelRune.GetMainStatMultiplier();
            float scaledValueHigh = originalMod.value * highLevelRune.GetMainStatMultiplier();
            
            Debug.Log($"\n레벨 스케일링 결과:");
            Debug.Log($"  Lv.{lowLevelRune.currentLevel}: {originalMod.value:F3} × {lowLevelRune.GetMainStatMultiplier():F3} = {scaledValueLow:F3}");
            Debug.Log($"  Lv.{highLevelRune.currentLevel}: {originalMod.value:F3} × {highLevelRune.GetMainStatMultiplier():F3} = {scaledValueHigh:F3}");
            Debug.Log($"  증가율: {(scaledValueHigh / scaledValueLow - 1f) * 100f:F1}%");
        }
        
        Debug.Log("==========================================");
    }
    
    [MenuItem(MENU_ROOT + "1-3. 15레벨 보스 사냥꾼 스케일링 검증")]
    public static void Test_BossHunterLv15Scaling()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 1-3: 15레벨 보스 사냥꾼 스케일링 검증 ==========");
        
        var allRunes = RuneInventoryManager.Instance.GetAllRunes();
        
        // "보스 사냥꾼" 룬 중 레벨이 가장 높은 것 찾기
        var bossHunterRunes = allRunes
            .Where(r => r.baseData != null && r.baseData.runeName.Contains("보스 사냥꾼"))
            .OrderByDescending(r => r.currentLevel)
            .ToList();
        
        if (bossHunterRunes.Count == 0)
        {
            Debug.LogWarning("⚠️ '보스 사냥꾼' 룬을 찾을 수 없습니다.");
            Debug.LogWarning("💡 Tip: Phase 2 메뉴의 '1-3. 룬 3개 추가 (보스 사냥꾼)'을 실행한 뒤,");
            Debug.LogWarning("         Phase 3 메뉴의 '2-2. 한계돌파 5회 연속'으로 15레벨까지 성장시켜주세요.");
            return;
        }
        
        RuneInstance targetRune = bossHunterRunes[0];
        Debug.Log($"대상 룬: {targetRune}");
        Debug.Log($"  레벨: Lv.{targetRune.currentLevel}/{targetRune.GetCurrentMaxLevel()}");
        Debug.Log($"  한계돌파: {targetRune.currentLimitBreak}/{targetRune.baseData.maxLimitBreak}");
        Debug.Log($"  레벨 배율: {targetRune.GetMainStatMultiplier():F3}x");
        Debug.Log($"  부옵션: {targetRune.allocatedSubStatModifierIds.Count}개\n");
        
        // 주옵션 스케일링 검증
        string mainModId = targetRune.baseData.MainStatModifierId;
        var originalMod = ConditionalModifierDatabase.GetModifierById(mainModId);
        
        if (originalMod != null)
        {
            Debug.Log($"[주옵션 스케일링 검증]");
            Debug.Log($"  모디파이어: [{mainModId}] {originalMod.displayName}");
            Debug.Log($"  원본 값: {originalMod.value:F3} ({originalMod.unit})");
            
            float scaledValue = originalMod.value * targetRune.GetMainStatMultiplier();
            Debug.Log($"  스케일링 값: {originalMod.value:F3} × {targetRune.GetMainStatMultiplier():F3} = {scaledValue:F3}");
            
            if (originalMod.unit == StatUnit.Percent)
            {
                Debug.Log($"  실제 효과: {originalMod.value * 100f:F1}% → {scaledValue * 100f:F1}%");
            }
        }
        
        // 부옵션 확인 (레벨 스케일링 없음)
        if (targetRune.allocatedSubStatModifierIds.Count > 0)
        {
            Debug.Log($"\n[부옵션 확인] (레벨 스케일링 없음)");
            foreach (var subModId in targetRune.allocatedSubStatModifierIds)
            {
                var subMod = ConditionalModifierDatabase.GetModifierById(subModId);
                if (subMod != null)
                {
                    Debug.Log($"  [{subModId}] {subMod.displayName}");
                    Debug.Log($"    값: {subMod.value:F3} ({subMod.unit}) - 원본 그대로!");
                }
            }
        }
        
        // 장착 및 PlayerRuntimeStats 확인
        Debug.Log($"\n[PlayerRuntimeStats 연동 검증]");
        bool success = RuneManager.Instance.EquipRune(targetRune, 0);
        
        if (success)
        {
            var modifiers = RuneManager.Instance.GetActiveConditionalModifiers();
            Debug.Log($"✅ 장착 성공! 활성 모디파이어: {modifiers.Count}개");
            
            // 주옵션 모디파이어 찾기
            var mainModifier = modifiers.FirstOrDefault(m => m.modifierId == mainModId);
            if (mainModifier != null)
            {
                Debug.Log($"\n🎉 주옵션 모디파이어 확인:");
                Debug.Log($"  [{mainModifier.modifierId}] {mainModifier.displayName}");
                Debug.Log($"  스케일링된 값: {mainModifier.value:F3}");
                Debug.Log($"  출처: {mainModifier.source}");
                
                // 원본과 비교
                if (Mathf.Approximately(mainModifier.value, originalMod.value * targetRune.GetMainStatMultiplier()))
                {
                    Debug.Log($"  ✅ 스케일링 정상 작동!");
                }
                else
                {
                    Debug.LogWarning($"  ⚠️ 스케일링 오류! 예상: {originalMod.value * targetRune.GetMainStatMultiplier():F3}, 실제: {mainModifier.value:F3}");
                }
            }
            
            // PlayerRuntimeStats 확인
            var playerStats = Object.FindObjectOfType<PlayerRuntimeStats>();
            if (playerStats != null)
            {
                var runtimeModifiers = playerStats.GetActiveConditionalModifiers();
                Debug.Log($"\n✅ PlayerRuntimeStats에 전달된 모디파이어: {runtimeModifiers.Count}개");
                
                foreach (var mod in runtimeModifiers)
                {
                    Debug.Log($"  [{mod.modifierId}] {mod.displayName}: {mod.value:F3}");
                }
            }
        }
        
        Debug.Log("==========================================");
    }
    
    #endregion
    
    #region 2. 중복 장착 방지 테스트
    
    [MenuItem(MENU_ROOT + "2-1. 중복 장착 방지 테스트")]
    public static void Test_DuplicateEquipPrevention()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 2-1: 중복 장착 방지 테스트 ==========");
        
        var allRunes = RuneInventoryManager.Instance.GetAllRunes();
        
        // 같은 종류의 룬 2개 찾기
        var groupedRunes = allRunes
            .GroupBy(r => r.baseDataId)
            .Where(g => g.Count() >= 2)
            .FirstOrDefault();
        
        if (groupedRunes == null)
        {
            Debug.LogWarning("⚠️ 같은 종류의 룬이 2개 이상 없습니다.");
            Debug.LogWarning("💡 Tip: Phase 2 메뉴의 '1-3. 룬 3개 추가'를 실행해주세요.");
            return;
        }
        
        var runeList = groupedRunes.ToList();
        RuneInstance rune1 = runeList[0];
        RuneInstance rune2 = runeList[1];
        
        Debug.Log($"테스트 룬:");
        Debug.Log($"  룬 1: {rune1}");
        Debug.Log($"  룬 2: {rune2}");
        Debug.Log($"  같은 종류: {rune1.baseDataId == rune2.baseDataId}\n");
        
        // 1. 첫 번째 룬 장착 (슬롯 0)
        Debug.Log("[1단계] 첫 번째 룬 장착 (슬롯 0)");
        bool success1 = RuneManager.Instance.EquipRune(rune1, 0);
        Debug.Log($"  결과: {(success1 ? "✅ 성공" : "❌ 실패")}\n");
        
        // 2. 같은 종류의 두 번째 룬 장착 시도 (슬롯 1) - 실패 예상
        Debug.Log("[2단계] 같은 종류의 룬 장착 시도 (슬롯 1) - 실패 예상");
        bool success2 = RuneManager.Instance.EquipRune(rune2, 1);
        Debug.Log($"  결과: {(success2 ? "❌ 예상 밖 성공" : "✅ 정상적으로 차단됨")}\n");
        
        if (!success2)
        {
            Debug.Log("🎉 중복 장착 방지 기능 정상 작동!");
        }
        else
        {
            Debug.LogError("⚠️ 중복 장착 방지 기능 오류! 같은 종류의 룬이 중복 장착되었습니다.");
        }
        
        // 3. 다른 종류의 룬 찾아서 장착 시도
        var differentRune = allRunes.FirstOrDefault(r => r.baseDataId != rune1.baseDataId);
        if (differentRune != null)
        {
            Debug.Log($"[3단계] 다른 종류의 룬 장착 시도 (슬롯 1)");
            Debug.Log($"  대상: {differentRune}");
            bool success3 = RuneManager.Instance.EquipRune(differentRune, 1);
            Debug.Log($"  결과: {(success3 ? "✅ 성공" : "❌ 실패")}\n");
        }
        
        // 최종 장착 상태 확인
        Debug.Log("[최종 장착 상태]");
        var equippedRunes = RuneManager.Instance.GetEquippedRunes();
        for (int i = 0; i < equippedRunes.Count; i++)
        {
            var rune = equippedRunes[i];
            if (rune != null)
            {
                Debug.Log($"  슬롯 {i}: {rune.baseData.runeName} (ID: {rune.baseDataId})");
            }
            else
            {
                Debug.Log($"  슬롯 {i}: (비어있음)");
            }
        }
        
        Debug.Log("==========================================");
    }
    
    #endregion
    
    #region 3. 모디파이어 깊은 복사 검증
    
    [MenuItem(MENU_ROOT + "3-1. 모디파이어 깊은 복사 검증 (데이터 오염 방지)")]
    public static void Test_ModifierDeepCopyVerification()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 3-1: 모디파이어 깊은 복사 검증 ==========");
        
        var allRunes = RuneInventoryManager.Instance.GetAllRunes();
        
        if (allRunes.Count == 0)
        {
            Debug.LogWarning("⚠️ 인벤토리가 비어있습니다.");
            return;
        }
        
        RuneInstance targetRune = allRunes[0];
        string mainModId = targetRune.baseData.MainStatModifierId;
        
        // 1. 원본 모디파이어 값 기록
        var originalMod = ConditionalModifierDatabase.GetModifierById(mainModId);
        if (originalMod == null)
        {
            Debug.LogWarning("⚠️ 모디파이어를 찾을 수 없습니다.");
            return;
        }
        
        float originalValue = originalMod.value;
        Debug.Log($"[1단계] 원본 모디파이어 값 기록");
        Debug.Log($"  [{mainModId}] {originalMod.displayName}");
        Debug.Log($"  원본 값: {originalValue:F6}\n");
        
        // 2. 룬 장착 (스케일링 적용)
        Debug.Log($"[2단계] 룬 장착 (레벨 배율: {targetRune.GetMainStatMultiplier():F3}x)");
        RuneManager.Instance.EquipRune(targetRune, 0);
        
        var modifiers = RuneManager.Instance.GetActiveConditionalModifiers();
        var scaledMod = modifiers.FirstOrDefault(m => m.modifierId == mainModId);
        
        if (scaledMod != null)
        {
            Debug.Log($"  스케일링된 값: {scaledMod.value:F6}\n");
        }
        
        // 3. 원본 모디파이어 값 재확인 (변경되지 않았어야 함!)
        var originalModCheck = ConditionalModifierDatabase.GetModifierById(mainModId);
        float originalValueAfter = originalModCheck.value;
        
        Debug.Log($"[3단계] 원본 모디파이어 값 재확인");
        Debug.Log($"  변경 전: {originalValue:F6}");
        Debug.Log($"  변경 후: {originalValueAfter:F6}\n");
        
        // 검증
        if (Mathf.Approximately(originalValue, originalValueAfter))
        {
            Debug.Log("✅ 원본 데이터 오염 없음! 깊은 복사 정상 작동!");
            Debug.Log("   ConditionalModifierDatabase의 정적 캐시가 보호되었습니다.");
        }
        else
        {
            Debug.LogError("❌ 원본 데이터 오염 발생!");
            Debug.LogError($"   원본 값이 {originalValue:F6}에서 {originalValueAfter:F6}로 변경되었습니다.");
            Debug.LogError("   모디파이어 복제 로직을 확인해주세요!");
        }
        
        // 4. 룬 해제 후 재확인
        Debug.Log("\n[4단계] 룬 해제 후 재확인");
        RuneManager.Instance.UnequipRune(0);
        
        var originalModFinal = ConditionalModifierDatabase.GetModifierById(mainModId);
        Debug.Log($"  최종 원본 값: {originalModFinal.value:F6}");
        
        if (Mathf.Approximately(originalValue, originalModFinal.value))
        {
            Debug.Log("✅ 해제 후에도 원본 데이터 정상!");
        }
        
        Debug.Log("==========================================");
    }
    
    #endregion
    
    #region 3.5. 디버그: 룬 데이터 검증
    
    [MenuItem(MENU_ROOT + "3-2. 🔍 디버그: 모든 룬의 주옵션 확인")]
    public static void Test_DebugAllRuneMainStats()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 디버그: 모든 룬의 주옵션 확인 ==========");
        
        var allRunes = RuneInventoryManager.Instance.GetAllRunes();
        
        if (allRunes.Count == 0)
        {
            Debug.LogWarning("⚠️ 인벤토리가 비어있습니다.");
            return;
        }
        
        Debug.Log($"총 {allRunes.Count}개 룬 분석\n");
        
        int validCount = 0;
        int emptyMainStatCount = 0;
        int invalidModifierCount = 0;
        
        foreach (var rune in allRunes)
        {
            Debug.Log($"[{rune.baseData.runeName}] Lv.{rune.currentLevel}");
            Debug.Log($"  baseDataId: {rune.baseDataId}");
            
            // 주옵션 ID 확인
            string mainModId = rune.baseData.MainStatModifierId;
            Debug.Log($"  mainStatModifierId: {(string.IsNullOrEmpty(mainModId) ? "(비어있음!) ❌" : mainModId)}");
            
            if (string.IsNullOrEmpty(mainModId))
            {
                emptyMainStatCount++;
                Debug.LogWarning($"    ⚠️ 주옵션 ID가 비어있습니다!");
                continue;
            }
            
            // 데이터베이스에서 찾기
            var modifier = ConditionalModifierDatabase.GetModifierById(mainModId);
            
            if (modifier == null)
            {
                invalidModifierCount++;
                Debug.LogError($"    ❌ 모디파이어를 찾을 수 없습니다: {mainModId}");
                Debug.LogError($"    💡 ConditionalModifier.csv에 [{mainModId}]가 있는지 확인하세요!");
            }
            else
            {
                validCount++;
                Debug.Log($"    ✅ 모디파이어 찾음: {modifier.displayName}");
                Debug.Log($"       원본 값: {modifier.value:F3} ({modifier.unit})");
                Debug.Log($"       조건: {modifier.conditionType} | 효과: {modifier.effectType}");
                
                // 스케일링 계산
                float scaledValue = modifier.value * rune.GetMainStatMultiplier();
                Debug.Log($"       레벨 배율: {rune.GetMainStatMultiplier():F3}x");
                Debug.Log($"       스케일링 값: {scaledValue:F3}");
            }
            
            Debug.Log("");
        }
        
        Debug.Log("========== 분석 결과 ==========");
        Debug.Log($"  총 룬: {allRunes.Count}개");
        Debug.Log($"  정상: {validCount}개 ✅");
        Debug.Log($"  주옵션 ID 비어있음: {emptyMainStatCount}개 {(emptyMainStatCount > 0 ? "❌" : "")}");
        Debug.Log($"  모디파이어 찾을 수 없음: {invalidModifierCount}개 {(invalidModifierCount > 0 ? "❌" : "")}");
        
        if (emptyMainStatCount > 0 || invalidModifierCount > 0)
        {
            Debug.LogError("\n⚠️ 문제가 발견되었습니다!");
            Debug.LogError("💡 해결 방법:");
            Debug.LogError("   1. RuneData ScriptableObject 인스펙터에서 mainStatModifierId 필드 확인");
            Debug.LogError("   2. ConditionalModifier.csv에 해당 ID가 있는지 확인");
            Debug.LogError("   3. ConditionalModifierDatabase가 정상적으로 로드되었는지 확인");
        }
        else
        {
            Debug.Log("\n✅ 모든 룬의 주옵션이 정상입니다!");
        }
        
        Debug.Log("==========================================");
    }
    
    #endregion
    
    #region 4. 통합 시나리오
    
    [MenuItem(MENU_ROOT + "4. 🎯 전체 시나리오 테스트 (장착→스케일링→연동)")]
    public static void Test_FullIntegrationScenario()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========================================");
        Debug.Log("🎯 전체 통합 시나리오 테스트 시작");
        Debug.Log("========================================\n");
        
        // 0. 초기화
        Debug.Log("[0단계] 초기화");
        RuneManager.Instance.UnequipAllRunes();
        Debug.Log("  모든 룬 해제 완료\n");
        
        var allRunes = RuneInventoryManager.Instance.GetAllRunes();
        
        if (allRunes.Count < 3)
        {
            Debug.LogWarning("⚠️ 충분한 룬이 없습니다. 최소 3개 필요");
            return;
        }
        
        // 1. 서로 다른 종류의 룬 3개 선택
        Debug.Log("[1단계] 서로 다른 종류의 룬 3개 선택");
        var uniqueRunes = allRunes
            .GroupBy(r => r.baseDataId)
            .Select(g => g.OrderByDescending(r => r.currentLevel).First())
            .Take(3)
            .ToList();
        
        if (uniqueRunes.Count < 3)
        {
            Debug.LogWarning("⚠️ 서로 다른 종류의 룬이 3개 미만입니다.");
            uniqueRunes = allRunes.Take(3).ToList();
        }
        
        for (int i = 0; i < uniqueRunes.Count; i++)
        {
            Debug.Log($"  룬 {i + 1}: {uniqueRunes[i]}");
        }
        Debug.Log("");
        
        // 2. 순차적으로 장착
        Debug.Log("[2단계] 룬 장착 (최대 3개)");
        int equippedCount = 0;
        
        for (int i = 0; i < Mathf.Min(uniqueRunes.Count, 3); i++)
        {
            var rune = uniqueRunes[i];
            bool success = RuneManager.Instance.EquipRune(rune, i);
            
            if (success)
            {
                equippedCount++;
                Debug.Log($"  ✅ 슬롯 {i}: {rune.baseData.runeName} 장착 성공");
            }
            else
            {
                Debug.LogWarning($"  ❌ 슬롯 {i}: 장착 실패");
            }
        }
        
        Debug.Log($"  총 {equippedCount}개 룬 장착 완료\n");
        
        // 3. 스케일링 검증
        Debug.Log("[3단계] 스케일링 검증");
        var modifiers = RuneManager.Instance.GetActiveConditionalModifiers();
        Debug.Log($"  활성 조건부 모디파이어: {modifiers.Count}개");
        
        int mainStatCount = 0;
        int subStatCount = 0;
        
        foreach (var mod in modifiers)
        {
            if (mod.source.Contains("(부옵션)"))
            {
                subStatCount++;
            }
            else
            {
                mainStatCount++;
                Debug.Log($"    [주옵션] {mod.displayName}: {mod.value:F3}");
            }
        }
        
        Debug.Log($"  주옵션: {mainStatCount}개 (레벨 스케일링 적용)");
        Debug.Log($"  부옵션: {subStatCount}개 (원본 값 유지)\n");
        
        // 4. PlayerRuntimeStats 연동 확인
        Debug.Log("[4단계] PlayerRuntimeStats 연동 확인");
        var playerStats = Object.FindObjectOfType<PlayerRuntimeStats>();
        
        if (playerStats != null)
        {
            var runtimeModifiers = playerStats.GetActiveConditionalModifiers();
            Debug.Log($"  PlayerRuntimeStats에 전달된 모디파이어: {runtimeModifiers.Count}개");
            
            if (runtimeModifiers.Count == modifiers.Count)
            {
                Debug.Log("  ✅ 모디파이어 동기화 완료!");
            }
            else
            {
                Debug.LogWarning($"  ⚠️ 모디파이어 개수 불일치! (RuneManager: {modifiers.Count}, PlayerRuntimeStats: {runtimeModifiers.Count})");
            }
            
            // 세부 확인
            foreach (var mod in runtimeModifiers)
            {
                Debug.Log($"    [{mod.modifierId}] {mod.displayName}: {mod.value:F3}");
            }
        }
        else
        {
            Debug.LogWarning("  ⚠️ PlayerRuntimeStats를 찾을 수 없습니다. (로비에서는 정상)");
        }
        
        // 5. 최종 결과
        Debug.Log("\n[5단계] 최종 결과");
        Debug.Log($"  장착된 룬: {equippedCount}/3개");
        Debug.Log($"  활성 모디파이어: {modifiers.Count}개");
        Debug.Log($"  PlayerRuntimeStats 연동: {(playerStats != null ? "✅" : "❌")}");
        
        Debug.Log("\n========================================");
        Debug.Log("🎉 전체 통합 시나리오 테스트 완료!");
        Debug.Log("========================================");
    }
    
    #endregion
}
