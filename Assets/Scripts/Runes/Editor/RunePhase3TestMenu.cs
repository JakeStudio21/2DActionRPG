using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 룬 시스템 Phase 3 테스트 메뉴
/// ⚙️ Unity Editor 메뉴에서 클릭만으로 레벨업/한계돌파 테스트 가능
/// 
/// 메뉴 위치: Tools/Rune System/Test Phase 3/...
/// </summary>
public class RunePhase3TestMenu : Editor
{
    private const string MENU_ROOT = "Tools/Rune System/Test Phase 3/";
    
    #region 1. 레벨업 테스트
    
    [MenuItem(MENU_ROOT + "1-1. 첫 번째 룬 1레벨업")]
    public static void Test_LevelUpOnce()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 1-1: 룬 1레벨업 ==========");
        
        var allRunes = RuneInventoryManager.Instance.GetAllRunes();
        
        if (allRunes.Count == 0)
        {
            Debug.LogWarning("⚠️ 인벤토리가 비어있습니다. 먼저 Phase 2 메뉴로 룬을 추가해주세요.");
            return;
        }
        
        RuneInstance targetRune = allRunes[0];
        Debug.Log($"대상 룬: {targetRune}");
        
        var result = RuneEnhanceManager.Instance.TryLevelUp(targetRune.instanceUID);
        
        Debug.Log($"결과: {result}");
        
        if (result == RuneEnhanceManager.LevelUpResult.Success)
        {
            Debug.Log($"✅ 레벨업 성공! 현재 레벨: Lv.{targetRune.currentLevel}");
        }
        
        Debug.Log("==========================================");
    }
    
    [MenuItem(MENU_ROOT + "1-2. 첫 번째 룬 3레벨까지 레벨업 (부옵션 테스트)")]
    public static void Test_LevelUpToLevel3()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 1-2: 3레벨까지 레벨업 (부옵션 테스트) ==========");
        
        var allRunes = RuneInventoryManager.Instance.GetAllRunes();
        
        if (allRunes.Count == 0)
        {
            Debug.LogWarning("⚠️ 인벤토리가 비어있습니다.");
            return;
        }
        
        RuneInstance targetRune = allRunes[0];
        Debug.Log($"대상 룬: {targetRune}");
        Debug.Log($"시작 레벨: Lv.{targetRune.currentLevel}");
        Debug.Log($"시작 부옵션: {targetRune.allocatedSubStatModifierIds.Count}개\n");
        
        // 3레벨까지 레벨업
        int targetLevel = 3;
        int successCount = 0;
        
        while (targetRune.currentLevel < targetLevel)
        {
            var result = RuneEnhanceManager.Instance.TryLevelUp(targetRune.instanceUID);
            
            if (result == RuneEnhanceManager.LevelUpResult.Success)
            {
                successCount++;
            }
            else
            {
                Debug.LogWarning($"레벨업 실패: {result}");
                break;
            }
        }
        
        Debug.Log($"\n✅ 레벨업 완료!");
        Debug.Log($"  총 레벨업 횟수: {successCount}");
        Debug.Log($"  최종 레벨: Lv.{targetRune.currentLevel}");
        Debug.Log($"  최종 부옵션: {targetRune.allocatedSubStatModifierIds.Count}개");
        
        if (targetRune.allocatedSubStatModifierIds.Count > 0)
        {
            Debug.Log($"\n🎉 부옵션 획득 성공!");
            foreach (var modId in targetRune.allocatedSubStatModifierIds)
            {
                var mod = ConditionalModifierDatabase.GetModifierById(modId);
                string modName = mod != null ? mod.displayName : modId;
                Debug.Log($"    - [{modId}] {modName}");
            }
        }
        
        Debug.Log("==========================================");
    }
    
    [MenuItem(MENU_ROOT + "1-3. 첫 번째 룬 9레벨까지 레벨업 (모든 부옵션 테스트)")]
    public static void Test_LevelUpToLevel9()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 1-3: 9레벨까지 레벨업 (모든 부옵션 테스트) ==========");
        
        var allRunes = RuneInventoryManager.Instance.GetAllRunes();
        
        if (allRunes.Count == 0)
        {
            Debug.LogWarning("⚠️ 인벤토리가 비어있습니다.");
            return;
        }
        
        RuneInstance targetRune = allRunes[0];
        Debug.Log($"대상 룬: {targetRune}");
        Debug.Log($"시작 레벨: Lv.{targetRune.currentLevel}");
        Debug.Log($"시작 부옵션: {targetRune.allocatedSubStatModifierIds.Count}개\n");
        
        int initialSubStats = targetRune.allocatedSubStatModifierIds.Count;
        
        // 9레벨까지 레벨업
        int targetLevel = 9;
        int successCount = 0;
        
        while (targetRune.currentLevel < targetLevel)
        {
            var result = RuneEnhanceManager.Instance.TryLevelUp(targetRune.instanceUID);
            
            if (result == RuneEnhanceManager.LevelUpResult.Success)
            {
                successCount++;
            }
            else
            {
                Debug.LogWarning($"레벨업 실패: {result}");
                break;
            }
        }
        
        Debug.Log($"\n✅ 레벨업 완료!");
        Debug.Log($"  총 레벨업 횟수: {successCount}");
        Debug.Log($"  최종 레벨: Lv.{targetRune.currentLevel}");
        Debug.Log($"  획득한 부옵션: {targetRune.allocatedSubStatModifierIds.Count - initialSubStats}개");
        Debug.Log($"  최종 부옵션: {targetRune.allocatedSubStatModifierIds.Count}개");
        
        if (targetRune.allocatedSubStatModifierIds.Count > 0)
        {
            Debug.Log($"\n🎉 부옵션 목록:");
            for (int i = 0; i < targetRune.allocatedSubStatModifierIds.Count; i++)
            {
                var modId = targetRune.allocatedSubStatModifierIds[i];
                var mod = ConditionalModifierDatabase.GetModifierById(modId);
                string modName = mod != null ? mod.displayName : modId;
                Debug.Log($"    [{i + 1}] [{modId}] {modName}");
            }
        }
        
        Debug.Log("==========================================");
    }
    
    #endregion
    
    #region 2. 한계돌파 테스트
    
    [MenuItem(MENU_ROOT + "2-1. 한계돌파 테스트 (자동)")]
    public static void Test_LimitBreakAuto()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 2-1: 한계돌파 테스트 (자동) ==========");
        
        var allRunes = RuneInventoryManager.Instance.GetAllRunes();
        
        if (allRunes.Count < 2)
        {
            Debug.LogWarning("⚠️ 한계돌파 테스트를 위해서는 최소 2개 이상의 룬이 필요합니다.");
            Debug.LogWarning("💡 Tip: Phase 2 메뉴의 '1-3. 룬 3개 추가'를 실행해주세요.");
            return;
        }
        
        // 1. 같은 종류의 룬 찾기
        var groupedRunes = allRunes
            .GroupBy(r => r.baseDataId)
            .Where(g => g.Count() >= 2)
            .FirstOrDefault();
        
        if (groupedRunes == null)
        {
            Debug.LogWarning("⚠️ 같은 종류의 룬이 2개 이상 없습니다.");
            Debug.LogWarning("💡 Tip: Phase 2 메뉴의 '1-3. 룬 3개 추가 (보스 사냥꾼)'을 실행해주세요.");
            return;
        }
        
        var runeList = groupedRunes.ToList();
        RuneInstance baseRune = runeList[0];
        RuneInstance materialRune = runeList[1];
        
        Debug.Log($"베이스 룬: {baseRune}");
        Debug.Log($"재료 룬: {materialRune}");
        Debug.Log($"\n[1단계] 베이스 룬을 최대 레벨까지 레벨업...");
        
        // 2. 베이스 룬을 최대 레벨까지 레벨업
        int levelUpCount = 0;
        while (baseRune.CanLevelUp())
        {
            var result = RuneEnhanceManager.Instance.TryLevelUp(baseRune.instanceUID);
            if (result == RuneEnhanceManager.LevelUpResult.Success)
            {
                levelUpCount++;
            }
            else
            {
                break;
            }
        }
        
        Debug.Log($"  레벨업 {levelUpCount}회 완료");
        Debug.Log($"  현재 레벨: Lv.{baseRune.currentLevel} / {baseRune.GetCurrentMaxLevel()}");
        
        Debug.Log($"\n[2단계] 한계돌파 시도...");
        
        // 3. 한계돌파 실행
        var limitBreakResult = RuneEnhanceManager.Instance.TryLimitBreak(
            baseRune.instanceUID,
            materialRune.instanceUID
        );
        
        Debug.Log($"\n결과: {limitBreakResult}");
        
        if (limitBreakResult == RuneEnhanceManager.LimitBreakResult.Success)
        {
            Debug.Log($"✅ 한계돌파 성공!");
            Debug.Log($"  한계돌파: {baseRune.currentLimitBreak}");
            Debug.Log($"  최대 레벨: Lv.{baseRune.GetCurrentMaxLevel()}");
            Debug.Log($"  재료 룬 소모됨");
        }
        
        Debug.Log("==========================================");
    }
    
    [MenuItem(MENU_ROOT + "2-2. 한계돌파 5회 연속 (최대 레벨 15 달성)")]
    public static void Test_LimitBreak5Times()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 2-2: 한계돌파 5회 연속 (최대 레벨 15 달성) ==========");
        
        // 1. 같은 종류의 룬 6개 찾기 (베이스 1 + 재료 5)
        var allRunes = RuneInventoryManager.Instance.GetAllRunes();
        
        var groupedRunes = allRunes
            .GroupBy(r => r.baseDataId)
            .Where(g => g.Count() >= 6)
            .FirstOrDefault();
        
        if (groupedRunes == null)
        {
            Debug.LogWarning("⚠️ 같은 종류의 룬이 6개 이상 필요합니다.");
            Debug.LogWarning("💡 Tip: Phase 2 메뉴의 '1-3. 룬 3개 추가'를 여러 번 실행해주세요.");
            return;
        }
        
        var runeList = groupedRunes.ToList();
        RuneInstance baseRune = runeList[0];
        
        Debug.Log($"베이스 룬: {baseRune}");
        Debug.Log($"재료 룬: {runeList.Count - 1}개\n");
        
        // 2. 베이스 룬을 최대 레벨까지 레벨업
        Debug.Log("[1단계] 베이스 룬을 최대 레벨까지 레벨업...");
        while (baseRune.CanLevelUp())
        {
            RuneEnhanceManager.Instance.TryLevelUp(baseRune.instanceUID);
        }
        Debug.Log($"  레벨업 완료: Lv.{baseRune.currentLevel}\n");
        
        // 3. 한계돌파 5회 실행
        Debug.Log("[2단계] 한계돌파 5회 연속...");
        int successCount = 0;
        
        for (int i = 1; i <= 5 && i < runeList.Count; i++)
        {
            RuneInstance materialRune = runeList[i];
            
            Debug.Log($"\n  [{i}회차] 한계돌파 시도...");
            
            var result = RuneEnhanceManager.Instance.TryLimitBreak(
                baseRune.instanceUID,
                materialRune.instanceUID
            );
            
            if (result == RuneEnhanceManager.LimitBreakResult.Success)
            {
                successCount++;
                Debug.Log($"    ✅ 성공! 한계돌파: {baseRune.currentLimitBreak}, 최대 레벨: Lv.{baseRune.GetCurrentMaxLevel()}");
                
                // 새로 늘어난 레벨까지 레벨업
                while (baseRune.CanLevelUp())
                {
                    RuneEnhanceManager.Instance.TryLevelUp(baseRune.instanceUID);
                }
                Debug.Log($"    레벨업 완료: Lv.{baseRune.currentLevel}");
            }
            else
            {
                Debug.LogWarning($"    ❌ 실패: {result}");
                break;
            }
        }
        
        Debug.Log($"\n========================================");
        Debug.Log($"🎉 한계돌파 {successCount}회 성공!");
        Debug.Log($"  최종 한계돌파: {baseRune.currentLimitBreak}/5");
        Debug.Log($"  최종 레벨: Lv.{baseRune.currentLevel}");
        Debug.Log($"  최종 최대 레벨: Lv.{baseRune.GetCurrentMaxLevel()}");
        Debug.Log($"  부옵션: {baseRune.allocatedSubStatModifierIds.Count}개");
        
        if (baseRune.GetCurrentMaxLevel() == 15)
        {
            Debug.Log($"\n🎊 축하합니다! 최대 레벨 15에 도달했습니다!");
        }
        
        Debug.Log("==========================================");
    }
    
    #endregion
    
    #region 3. 예외 처리 테스트
    
    [MenuItem(MENU_ROOT + "3-1. 예외 처리 테스트 (최대 레벨 레벨업 시도)")]
    public static void Test_LevelUpMaxLevel()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 3-1: 예외 처리 (최대 레벨 레벨업) ==========");
        
        var allRunes = RuneInventoryManager.Instance.GetAllRunes();
        
        if (allRunes.Count == 0)
        {
            Debug.LogWarning("⚠️ 인벤토리가 비어있습니다.");
            return;
        }
        
        RuneInstance targetRune = allRunes[0];
        
        // 최대 레벨까지 레벨업
        Debug.Log($"대상 룬: {targetRune}");
        Debug.Log($"최대 레벨까지 레벨업 중...\n");
        
        while (targetRune.CanLevelUp())
        {
            RuneEnhanceManager.Instance.TryLevelUp(targetRune.instanceUID);
        }
        
        Debug.Log($"현재 레벨: Lv.{targetRune.currentLevel} / {targetRune.GetCurrentMaxLevel()}");
        Debug.Log($"\n추가 레벨업 시도...");
        
        // 최대 레벨에서 레벨업 시도 (실패 예상)
        var result = RuneEnhanceManager.Instance.TryLevelUp(targetRune.instanceUID);
        
        Debug.Log($"결과: {result}");
        
        if (result == RuneEnhanceManager.LevelUpResult.AlreadyMaxLevel)
        {
            Debug.Log("✅ 예외 처리 정상 작동! (최대 레벨 도달)");
        }
        
        Debug.Log("==========================================");
    }
    
    [MenuItem(MENU_ROOT + "3-2. 예외 처리 테스트 (다른 종류 룬 한계돌파)")]
    public static void Test_LimitBreakDifferentType()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 3-2: 예외 처리 (다른 종류 룬 한계돌파) ==========");
        
        var allRunes = RuneInventoryManager.Instance.GetAllRunes();
        
        if (allRunes.Count < 2)
        {
            Debug.LogWarning("⚠️ 최소 2개 이상의 룬이 필요합니다.");
            return;
        }
        
        // 서로 다른 종류의 룬 찾기
        RuneInstance rune1 = null;
        RuneInstance rune2 = null;
        
        for (int i = 0; i < allRunes.Count - 1; i++)
        {
            if (allRunes[i].baseDataId != allRunes[i + 1].baseDataId)
            {
                rune1 = allRunes[i];
                rune2 = allRunes[i + 1];
                break;
            }
        }
        
        if (rune1 == null || rune2 == null)
        {
            Debug.LogWarning("⚠️ 서로 다른 종류의 룬이 없습니다.");
            Debug.LogWarning("💡 Tip: Phase 2 메뉴로 다른 종류의 룬을 추가해주세요.");
            return;
        }
        
        Debug.Log($"베이스 룬: {rune1}");
        Debug.Log($"재료 룬: {rune2}");
        Debug.Log($"\n한계돌파 시도...");
        
        var result = RuneEnhanceManager.Instance.TryLimitBreak(rune1.instanceUID, rune2.instanceUID);
        
        Debug.Log($"결과: {result}");
        
        if (result == RuneEnhanceManager.LimitBreakResult.DifferentRuneType)
        {
            Debug.Log("✅ 예외 처리 정상 작동! (다른 종류의 룬)");
        }
        
        Debug.Log("==========================================");
    }
    
    #endregion
    
    #region 4. 강화 정보 조회
    
    [MenuItem(MENU_ROOT + "4-1. 강화 정보 출력")]
    public static void Test_PrintEnhanceInfo()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        RuneEnhanceManager.Instance.PrintRuneEnhanceInfo();
    }
    
    #endregion
    
    #region 5. 통합 시나리오
    
    [MenuItem(MENU_ROOT + "5. 🎯 전체 시나리오 테스트 (레벨업+한계돌파)")]
    public static void Test_FullEnhanceScenario()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========================================");
        Debug.Log("🎯 전체 강화 시나리오 테스트 시작");
        Debug.Log("========================================\n");
        
        // 1. 초기 상태
        Debug.Log("[1단계] 초기 상태 확인");
        int runeCount = RuneInventoryManager.Instance.GetRuneCount();
        Debug.Log($"  현재 룬 개수: {runeCount}개\n");
        
        if (runeCount < 6)
        {
            Debug.LogWarning("⚠️ 충분한 룬이 없습니다. 최소 6개 필요 (베이스 1 + 재료 5)");
            Debug.LogWarning("💡 Tip: Phase 2 메뉴의 '1-3. 룬 3개 추가'를 여러 번 실행해주세요.");
            return;
        }
        
        // 2. 같은 종류의 룬 찾기
        Debug.Log("[2단계] 같은 종류의 룬 검색");
        var allRunes = RuneInventoryManager.Instance.GetAllRunes();
        var groupedRunes = allRunes
            .GroupBy(r => r.baseDataId)
            .Where(g => g.Count() >= 6)
            .FirstOrDefault();
        
        if (groupedRunes == null)
        {
            Debug.LogWarning("⚠️ 같은 종류의 룬이 6개 이상 없습니다.");
            return;
        }
        
        var runeList = groupedRunes.ToList();
        RuneInstance baseRune = runeList[0];
        
        Debug.Log($"  베이스 룬: {baseRune.baseData.runeName}");
        Debug.Log($"  재료 룬: {runeList.Count - 1}개\n");
        
        // 3. 9레벨까지 레벨업 (부옵션 3개 획득)
        Debug.Log("[3단계] 9레벨까지 레벨업 (부옵션 획득 테스트)");
        int levelUpCount = 0;
        
        while (baseRune.currentLevel < 9)
        {
            var result = RuneEnhanceManager.Instance.TryLevelUp(baseRune.instanceUID);
            if (result == RuneEnhanceManager.LevelUpResult.Success)
            {
                levelUpCount++;
            }
            else
            {
                break;
            }
        }
        
        Debug.Log($"  레벨업 {levelUpCount}회 완료");
        Debug.Log($"  현재 레벨: Lv.{baseRune.currentLevel}");
        Debug.Log($"  부옵션: {baseRune.allocatedSubStatModifierIds.Count}개\n");
        
        // 4. 10레벨 도달
        Debug.Log("[4단계] 10레벨 도달 (최대 레벨)");
        while (baseRune.CanLevelUp() && baseRune.currentLevel < 10)
        {
            RuneEnhanceManager.Instance.TryLevelUp(baseRune.instanceUID);
        }
        Debug.Log($"  레벨: Lv.{baseRune.currentLevel} / {baseRune.GetCurrentMaxLevel()}\n");
        
        // 5. 한계돌파 5회
        Debug.Log("[5단계] 한계돌파 5회 → 최대 레벨 15 달성");
        int limitBreakCount = 0;
        
        for (int i = 1; i <= 5 && i < runeList.Count; i++)
        {
            var result = RuneEnhanceManager.Instance.TryLimitBreak(
                baseRune.instanceUID,
                runeList[i].instanceUID
            );
            
            if (result == RuneEnhanceManager.LimitBreakResult.Success)
            {
                limitBreakCount++;
                
                // 새로 늘어난 레벨까지 레벨업
                while (baseRune.CanLevelUp())
                {
                    RuneEnhanceManager.Instance.TryLevelUp(baseRune.instanceUID);
                }
            }
        }
        
        Debug.Log($"  한계돌파 {limitBreakCount}회 성공");
        Debug.Log($"  최종 레벨: Lv.{baseRune.currentLevel}\n");
        
        // 6. 최종 결과
        Debug.Log("[6단계] 최종 결과");
        Debug.Log($"  룬: {baseRune.baseData.runeName}");
        Debug.Log($"  레벨: Lv.{baseRune.currentLevel} / {baseRune.GetCurrentMaxLevel()}");
        Debug.Log($"  한계돌파: {baseRune.currentLimitBreak}/5");
        Debug.Log($"  부옵션: {baseRune.allocatedSubStatModifierIds.Count}개");
        
        if (baseRune.allocatedSubStatModifierIds.Count > 0)
        {
            Debug.Log($"\n  🎉 부옵션 목록:");
            foreach (var modId in baseRune.allocatedSubStatModifierIds)
            {
                var mod = ConditionalModifierDatabase.GetModifierById(modId);
                string modName = mod != null ? mod.displayName : modId;
                Debug.Log($"    - [{modId}] {modName}");
            }
        }
        
        Debug.Log("\n========================================");
        Debug.Log("🎉 전체 강화 시나리오 테스트 완료!");
        
        if (baseRune.currentLevel == 15)
        {
            Debug.Log("🎊 축하합니다! 최강 룬 달성!");
        }
        
        Debug.Log("========================================");
    }
    
    #endregion
}

