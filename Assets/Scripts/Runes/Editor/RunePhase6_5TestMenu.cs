using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Phase 6.5: 8종류 고유 조각 시스템 테스트
/// </summary>
public class RunePhase6_5TestMenu
{
    private const string MENU_ROOT = "Tools/Rune System/Phase 6.5 - 고유 조각/";
    
    [MenuItem(MENU_ROOT + "1. 💎 모든 룬 조각 확인")]
    public static void Test_CheckAllFragments()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 6.5 Test] 모든 룬 조각 확인");
        Debug.Log("========================================");
        
        var inventoryManager = RuneInventoryManager.Instance;
        var allFragments = inventoryManager.GetAllFragments();
        
        Debug.Log($"총 {allFragments.Count}종류의 룬 조각:");
        
        foreach (var kvp in allFragments.OrderBy(x => x.Key))
        {
            string runeId = kvp.Key;
            int count = kvp.Value;
            
            // RuneData 이름 가져오기
            var runeData = RuneDatabase.GetRuneData(runeId);
            string runeName = runeData != null ? runeData.runeName : runeId;
            
            Debug.Log($"  💎 [{runeName}] {count}개");
        }
        
        Debug.Log("========================================");
    }
    
    [MenuItem(MENU_ROOT + "2. 🔄 모든 룬 조각 초기화 (1000개씩)")]
    public static void Test_ResetAllFragments()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 6.5 Test] 모든 룬 조각 초기화");
        Debug.Log("========================================");
        
        var inventoryManager = RuneInventoryManager.Instance;
        var allFragments = inventoryManager.GetAllFragments();
        
        foreach (var runeId in allFragments.Keys.ToList())
        {
            int oldCount = inventoryManager.GetFragmentCount(runeId);
            
            // 1000개로 맞추기
            if (oldCount < 1000)
            {
                inventoryManager.AddFragments(runeId, 1000 - oldCount);
            }
            else if (oldCount > 1000)
            {
                inventoryManager.TryConsumeFragments(runeId, oldCount - 1000);
            }
            
            var runeData = RuneDatabase.GetRuneData(runeId);
            string runeName = runeData != null ? runeData.runeName : runeId;
            
            Debug.Log($"  💎 [{runeName}] {oldCount}개 → 1000개");
        }
        
        Debug.Log("✅ 모든 룬 조각 초기화 완료!");
        Debug.Log("========================================");
    }
    
    [MenuItem(MENU_ROOT + "3. 🚀 전체 시나리오 (해금→강화→한계돌파)")]
    public static void Test_FullScenario()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 6.5 Test] 전체 시나리오 테스트");
        Debug.Log("========================================");
        
        var inventoryManager = RuneInventoryManager.Instance;
        var enhanceManager = RuneEnhanceManager.Instance;
        
        // 테스트할 룬
        const string testRuneId = "RUNE_BOSS_HUNTER";
        
        // 0. 기존 룬 삭제
        var existingRunes = inventoryManager.GetRunesByDataId(testRuneId);
        foreach (var rune in existingRunes)
        {
            inventoryManager.RemoveRune(rune.instanceUID);
        }
        
        // 1. 조각 확인
        int initialFragments = inventoryManager.GetFragmentCount(testRuneId);
        Debug.Log($"\n📌 1단계: 초기 조각 확인");
        Debug.Log($"  💎 [보스 사냥꾼] 조각: {initialFragments}개");
        
        // 2. 해금 (100개 소모)
        Debug.Log($"\n📌 2단계: 룬 해금");
        var unlockResult = enhanceManager.TryUnlockRune(testRuneId);
        
        if (unlockResult != RuneEnhanceManager.UnlockResult.Success)
        {
            Debug.LogError($"❌ 해금 실패: {unlockResult}");
            return;
        }
        
        var unlockedRunes = inventoryManager.GetRunesByDataId(testRuneId);
        if (unlockedRunes.Count == 0)
        {
            Debug.LogError("❌ 해금된 룬을 찾을 수 없습니다!");
            return;
        }
        
        var targetRune = unlockedRunes[0];
        Debug.Log($"  ✅ 해금 성공: {targetRune}");
        Debug.Log($"  💎 남은 조각: {inventoryManager.GetFragmentCount(testRuneId)}개");
        
        // 3. 레벨업 (Lv.1 → Lv.10, 90개 소모)
        Debug.Log($"\n📌 3단계: 레벨업 (Lv.1 → Lv.10)");
        for (int i = 1; i < 10; i++)
        {
            var levelUpResult = enhanceManager.TryLevelUp(targetRune.instanceUID);
            if (levelUpResult != RuneEnhanceManager.LevelUpResult.Success)
            {
                Debug.LogWarning($"⚠️ 레벨업 실패 (Lv.{i}): {levelUpResult}");
                break;
            }
        }
        
        Debug.Log($"  ✅ 레벨업 완료: Lv.{targetRune.currentLevel}");
        Debug.Log($"  📊 부옵션: {targetRune.allocatedSubStatModifierIds.Count}개");
        Debug.Log($"  💎 남은 조각: {inventoryManager.GetFragmentCount(testRuneId)}개");
        
        // 4. 한계돌파 (200개 소모)
        Debug.Log($"\n📌 4단계: 한계돌파");
        var limitBreakResult = enhanceManager.TryLimitBreak(targetRune.instanceUID);
        
        if (limitBreakResult == RuneEnhanceManager.LimitBreakResult.Success)
        {
            Debug.Log($"  ✅ 한계돌파 성공: +{targetRune.currentLimitBreak}");
            Debug.Log($"  📊 최대 레벨: {targetRune.GetCurrentMaxLevel()}");
            Debug.Log($"  💎 남은 조각: {inventoryManager.GetFragmentCount(testRuneId)}개");
        }
        else
        {
            Debug.LogWarning($"⚠️ 한계돌파 실패: {limitBreakResult}");
        }
        
        // 5. 추가 레벨업 (Lv.10 → Lv.11, 10개 소모)
        Debug.Log($"\n📌 5단계: 추가 레벨업 (Lv.10 → Lv.11)");
        var finalLevelUpResult = enhanceManager.TryLevelUp(targetRune.instanceUID);
        
        if (finalLevelUpResult == RuneEnhanceManager.LevelUpResult.Success)
        {
            Debug.Log($"  ✅ 레벨업 성공: Lv.{targetRune.currentLevel}");
            Debug.Log($"  💎 남은 조각: {inventoryManager.GetFragmentCount(testRuneId)}개");
        }
        
        // 최종 결과
        Debug.Log($"\n========================================");
        Debug.Log("[최종 결과]");
        Debug.Log($"  룬: {targetRune}");
        Debug.Log($"  레벨: Lv.{targetRune.currentLevel}/{targetRune.GetCurrentMaxLevel()}");
        Debug.Log($"  한계돌파: +{targetRune.currentLimitBreak}");
        Debug.Log($"  부옵션: {targetRune.allocatedSubStatModifierIds.Count}개");
        Debug.Log($"  💎 최종 조각: {inventoryManager.GetFragmentCount(testRuneId)}개");
        Debug.Log($"  💰 소모량: {initialFragments - inventoryManager.GetFragmentCount(testRuneId)}개 (해금 100 + 레벨업 100 + 한계돌파 200)");
        Debug.Log("✅ 전체 시나리오 테스트 완료!");
        Debug.Log("========================================");
    }
    
    [MenuItem(MENU_ROOT + "4. 🔬 특정 룬 조각 추가 (보스 사냥꾼 +500)")]
    public static void Test_AddFragments()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 6.5 Test] 조각 추가 테스트");
        Debug.Log("========================================");
        
        const string testRuneId = "RUNE_BOSS_HUNTER";
        var inventoryManager = RuneInventoryManager.Instance;
        
        int before = inventoryManager.GetFragmentCount(testRuneId);
        inventoryManager.AddFragments(testRuneId, 500);
        int after = inventoryManager.GetFragmentCount(testRuneId);
        
        Debug.Log($"💎 [보스 사냥꾼] 조각: {before}개 → {after}개 (+500)");
        Debug.Log("========================================");
    }
    
    [MenuItem(MENU_ROOT + "5. 🧹 모든 룬 조각 소진 테스트")]
    public static void Test_ConsumeAllFragments()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 6.5 Test] 조각 소진 테스트");
        Debug.Log("========================================");
        
        const string testRuneId = "RUNE_BOSS_HUNTER";
        var inventoryManager = RuneInventoryManager.Instance;
        
        int current = inventoryManager.GetFragmentCount(testRuneId);
        Debug.Log($"💎 현재 [보스 사냥꾼] 조각: {current}개");
        
        // 100개 소진 시도
        bool success = inventoryManager.TryConsumeFragments(testRuneId, 100);
        
        if (success)
        {
            int after = inventoryManager.GetFragmentCount(testRuneId);
            Debug.Log($"✅ 소진 성공: {current}개 → {after}개 (-100)");
        }
        else
        {
            Debug.LogWarning($"❌ 소진 실패: 조각 부족 (필요: 100개, 보유: {current}개)");
        }
        
        Debug.Log("========================================");
    }
}
