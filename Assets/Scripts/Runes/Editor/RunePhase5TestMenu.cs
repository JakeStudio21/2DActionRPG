using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Phase 5: 룬 UI 시스템 테스트 메뉴
/// ⚙️ Unity Editor에서 UI를 빠르게 검증하기 위한 테스트 도구
/// 
/// 주요 기능:
/// - 테스트 룬 생성 (다양한 레벨/한계돌파 조합)
/// - UI 갱신 확인
/// - 장착/해제 테스트
/// </summary>
public static class RunePhase5TestMenu
{
    private const string MENU_ROOT = "Tools/Rune System/Phase 5 - UI/";
    
    // Phase 6.5 호환성: 테스트용 기본 룬 ID
    private const string DEFAULT_TEST_RUNE_ID = "RUNE_BOSS_HUNTER";
    
    /// <summary>
    /// [Phase 6.5 호환] 테스트용 조각 개수 조회
    /// </summary>
    private static int GetTestFragments()
    {
        var inventory = RuneInventoryManager.Instance;
        return inventory.GetFragmentCount(DEFAULT_TEST_RUNE_ID);
    }
    
    /// <summary>
    /// [Phase 6.5 호환] 테스트용 조각 설정
    /// </summary>
    private static void SetTestFragments(int amount)
    {
        var inventory = RuneInventoryManager.Instance;
        int current = inventory.GetFragmentCount(DEFAULT_TEST_RUNE_ID);
        
        if (current < amount)
        {
            inventory.AddFragments(DEFAULT_TEST_RUNE_ID, amount - current);
        }
        else if (current > amount)
        {
            inventory.TryConsumeFragments(DEFAULT_TEST_RUNE_ID, current - amount);
        }
    }
    
    #region 1. 테스트 데이터 생성
    
    [MenuItem(MENU_ROOT + "1. 테스트 룬 생성 (다양한 레벨)")]
    public static void Test_GenerateVariousLevelRunes()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 5 Test] 다양한 레벨의 테스트 룬 생성 시작");
        Debug.Log("========================================");
        
        var inventoryManager = RuneInventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("❌ RuneInventoryManager를 찾을 수 없습니다!");
            return;
        }
        
        var enhanceManager = RuneEnhanceManager.Instance;
        if (enhanceManager == null)
        {
            Debug.LogError("❌ RuneEnhanceManager를 찾을 수 없습니다!");
            return;
        }
        
        // 기존 룬 제거
        var existingRunes = new List<RuneInstance>(inventoryManager.GetAllRunes());
        foreach (var rune in existingRunes)
        {
            inventoryManager.RemoveRune(rune.instanceUID);
        }
        
        Debug.Log("✅ 기존 룬 제거 완료");
        
        // 룬 조각 초기화 (충분한 조각 제공) - Phase 6.5
        SetTestFragments(10000);
        Debug.Log($"💎 룬 조각 초기화: {GetTestFragments()}개");
        
        // 1. Lv.1 룬 (초보자)
        CreateTestRune("RUNE_BOSS_HUNTER", 1, 0, "보스 사냥꾼 (Lv.1)");
        
        // 2. Lv.3 룬 (부옵션 1개)
        CreateTestRune("RUNE_DEFENSE_BREAKER", 3, 0, "방어 파괴자 (Lv.3, 부옵션 1개)");
        
        // 3. Lv.6 룬 (부옵션 2개)
        CreateTestRune("RUNE_HIGH_HP_HUNTER", 6, 0, "고체력 사냥꾼 (Lv.6, 부옵션 2개)");
        
        // 4. Lv.9 룬 (부옵션 3개)
        CreateTestRune("RUNE_LIFESTEAL", 9, 0, "흡혈 룬 (Lv.9, 부옵션 3개)");
        
        // 5. Lv.10 룬 (최대 레벨, 한계돌파 없음)
        CreateTestRune("RUNE_SURVIVOR", 10, 0, "불굴의 생존자 (Lv.10 만렙)");
        
        // 6. Lv.15 룬 (한계돌파 5회)
        CreateTestRune("RUNE_EXECUTIONER", 15, 5, "처형자 (Lv.15 극한 강화)");
        
        // 7. 중간 레벨들 추가 (UI 스크롤 테스트)
        CreateTestRune("RUNE_AREA_DEFENDER", 5, 1, "장판 철벽 (Lv.5, 한돌 1회)");
        CreateTestRune("RUNE_BOSS_DEFENDER", 7, 2, "보스 철벽 (Lv.7, 한돌 2회)");
        
        Debug.Log("========================================");
        Debug.Log($"✅ 총 {inventoryManager.GetAllRunes().Count}개의 테스트 룬 생성 완료!");
        Debug.Log("💡 Tip: 이제 UI에서 목록을 확인해보세요.");
        Debug.Log("========================================");
    }
    
    private static RuneInstance CreateTestRune(string runeId, int targetLevel, int limitBreakCount, string description)
    {
        var inventoryManager = RuneInventoryManager.Instance;
        var enhanceManager = RuneEnhanceManager.Instance;
        
        if (inventoryManager == null || enhanceManager == null)
        {
            Debug.LogError("❌ 매니저를 찾을 수 없습니다!");
            return null;
        }
        
        // RuneData 가져오기
        var runeData = RuneDatabase.GetRuneData(runeId);
        if (runeData == null)
        {
            Debug.LogError($"❌ RuneData를 찾을 수 없습니다: {runeId}");
            return null;
        }
        
        // 룬 생성 (인벤토리에 추가)
        var rune = inventoryManager.AddRune(runeData);
        if (rune == null)
        {
            Debug.LogError($"❌ 룬 생성 실패: {runeId}");
            return null;
        }
        
        // 레벨업 먼저 (기본 최대 레벨까지)
        int baseMaxLevel = 10; // RuneData의 기본 최대 레벨
        int firstLevelUpTarget = Mathf.Min(targetLevel, baseMaxLevel);
        
        for (int i = 1; i < firstLevelUpTarget; i++)
        {
            enhanceManager.TryLevelUp(rune.instanceUID);
        }
        
        // 한계돌파 (최대 레벨 확장)
        for (int i = 0; i < limitBreakCount; i++)
        {
            // 현재 최대 레벨까지 레벨업
            while (rune.currentLevel < rune.GetCurrentMaxLevel())
            {
                enhanceManager.TryLevelUp(rune.instanceUID);
            }
            
            // 한계돌파 (파편 소모 방식)
            enhanceManager.TryLimitBreak(rune.instanceUID);
        }
        
        // 최종 목표 레벨까지 레벨업
        while (rune.currentLevel < targetLevel && rune.currentLevel < rune.GetCurrentMaxLevel())
        {
            enhanceManager.TryLevelUp(rune.instanceUID);
        }
        
        Debug.Log($"✅ {description}");
        
        return rune;
    }
    
    #endregion
    
    #region 2. UI 갱신 테스트
    
    [MenuItem(MENU_ROOT + "2. UI 강제 갱신")]
    public static void Test_RefreshUI()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 6 Test] UI 강제 갱신 (RunePanelUI)");
        Debug.Log("========================================");
        
        // Phase 6: RunePanelUI 찾기
        var panelUI = GameObject.FindObjectOfType<RunePanelUI>(true);
        if (panelUI == null)
        {
            Debug.LogWarning("⚠️ RunePanelUI를 찾을 수 없습니다. (Phase 6 UI 미설치)");
            Debug.LogWarning("💡 Tip: Phase6_UI_Setup_Guide.md를 참고하여 Unity Editor 설정을 완료하세요.");
            
            // Deprecated UI도 시도 (하위 호환성)
            #pragma warning disable CS0618
            var oldInventoryUI = GameObject.FindObjectOfType<RuneInventoryUI>(true);
            #pragma warning restore CS0618
            
            if (oldInventoryUI != null)
            {
                Debug.Log("⚠️ Deprecated RuneInventoryUI 발견. Phase 5 방식으로 갱신합니다.");
                
                bool wasActive = oldInventoryUI.gameObject.activeInHierarchy;
                if (!wasActive)
                {
                    var runeSubPanel = oldInventoryUI.transform.parent;
                    if (runeSubPanel != null)
                    {
                        runeSubPanel.gameObject.SetActive(true);
                    }
                }
                
                oldInventoryUI.RefreshInventory();
                Debug.Log("✅ UI 갱신 완료! (Deprecated 방식)");
                Debug.Log($"📊 표시된 룬 개수: {oldInventoryUI.transform.GetComponentsInChildren<RuneSlotUI>(true).Length}개");
            }
            else
            {
                Debug.LogError("❌ 룬 UI를 찾을 수 없습니다!");
            }
            
            Debug.Log("========================================");
            return;
        }
        
        // Phase 6: RunePanelUI 갱신
        bool wasPanelActive = panelUI.gameObject.activeInHierarchy;
        if (!wasPanelActive)
        {
            Debug.Log("⚠️ RunePanelUI가 비활성화 상태입니다. 임시로 활성화합니다.");
            panelUI.gameObject.SetActive(true);
        }
        
        panelUI.RefreshUI();
        
        Debug.Log("✅ UI 갱신 완료! (Phase 6)");
        Debug.Log($"📊 표시된 룬 슬롯 개수: {panelUI.transform.GetComponentsInChildren<RuneSlotUI>(true).Length}개");
        Debug.Log("========================================");
    }
    
    #endregion
    
    #region 3. 장착/해제 테스트
    
    [MenuItem(MENU_ROOT + "3. 첫 번째 룬 장착")]
    public static void Test_EquipFirstRune()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 5 Test] 첫 번째 룬 장착 테스트");
        Debug.Log("========================================");
        
        var inventoryManager = RuneInventoryManager.Instance;
        var runeManager = RuneManager.Instance;
        
        if (inventoryManager == null || runeManager == null)
        {
            Debug.LogError("❌ 매니저를 찾을 수 없습니다!");
            return;
        }
        
        var allRunes = inventoryManager.GetAllRunes();
        if (allRunes.Count == 0)
        {
            Debug.LogError("❌ 인벤토리가 비어있습니다!");
            Debug.LogWarning("💡 Tip: 먼저 '1. 테스트 룬 생성'을 실행하세요.");
            return;
        }
        
        var firstRune = allRunes[0];
        bool success = runeManager.EquipRune(firstRune, 0);
        
        if (success)
        {
            Debug.Log($"✅ 장착 성공: {firstRune}");
            Debug.Log($"📊 현재 장착 룬 수: {runeManager.GetActiveRunes().Count}");
        }
        else
        {
            Debug.LogError($"❌ 장착 실패: {firstRune}");
        }
        
        Debug.Log("========================================");
    }
    
    [MenuItem(MENU_ROOT + "4. 모든 룬 해제")]
    public static void Test_UnequipAllRunes()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 5 Test] 모든 룬 해제");
        Debug.Log("========================================");
        
        var runeManager = RuneManager.Instance;
        if (runeManager == null)
        {
            Debug.LogError("❌ RuneManager를 찾을 수 없습니다!");
            return;
        }
        
        var equippedRunes = runeManager.GetEquippedRunes();
        
        if (equippedRunes == null || equippedRunes.Count == 0)
        {
            Debug.Log("⚠️ 장착된 룬이 없습니다.");
            return;
        }
        
        int unequippedCount = 0;
        
        // 역순으로 해제 (인덱스 변경 방지)
        for (int i = equippedRunes.Count - 1; i >= 0; i--)
        {
            if (equippedRunes[i] != null)
            {
                Debug.Log($"✅ 해제: {equippedRunes[i]} (슬롯 {i})");
                runeManager.UnequipRune(i);
                unequippedCount++;
            }
        }
        
        Debug.Log($"✅ 총 {unequippedCount}개의 룬 해제 완료!");
        Debug.Log("========================================");
    }
    
    #endregion
    
    #region 4. 통합 시나리오
    
    [MenuItem(MENU_ROOT + "5. 🚀 풀 시나리오 테스트")]
    public static void Test_FullScenario()
    {
        Debug.Log("========================================");
        Debug.Log("🚀 [Phase 5 Test] 풀 시나리오 시작!");
        Debug.Log("========================================");
        
        // 1. 테스트 룬 생성
        Debug.Log("\n[Step 1] 테스트 룬 생성...");
        Test_GenerateVariousLevelRunes();
        
        // 2. 첫 번째 룬 장착
        Debug.Log("\n[Step 2] 첫 번째 룬 장착...");
        Test_EquipFirstRune();
        
        // 3. UI 갱신
        Debug.Log("\n[Step 3] UI 갱신...");
        Test_RefreshUI();
        
        Debug.Log("\n========================================");
        Debug.Log("🎉 풀 시나리오 테스트 완료!");
        Debug.Log("💡 Tip: 이제 UI를 직접 확인해보세요.");
        Debug.Log("========================================");
    }
    
    #endregion
    
    #region 5. 데이터 정리
    
    [MenuItem(MENU_ROOT + "6. 🗑️ 모든 테스트 데이터 삭제")]
    public static void Test_ClearAllData()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 5 Test] 모든 테스트 데이터 삭제");
        Debug.Log("========================================");
        
        var inventoryManager = RuneInventoryManager.Instance;
        var runeManager = RuneManager.Instance;
        
        if (inventoryManager == null || runeManager == null)
        {
            Debug.LogError("❌ 매니저를 찾을 수 없습니다!");
            return;
        }
        
        // 1. 모든 룬 해제
        var equippedRunes = runeManager.GetEquippedRunes();
        int unequippedCount = 0;
        
        for (int i = equippedRunes.Count - 1; i >= 0; i--)
        {
            if (equippedRunes[i] != null)
            {
                runeManager.UnequipRune(i);
                unequippedCount++;
            }
        }
        
        // 2. 모든 룬 삭제
        var allRunes = new List<RuneInstance>(inventoryManager.GetAllRunes());
        foreach (var rune in allRunes)
        {
            inventoryManager.RemoveRune(rune.instanceUID);
        }
        
        Debug.Log($"✅ {unequippedCount}개의 장착 룬 해제 완료");
        Debug.Log($"✅ {allRunes.Count}개의 룬 삭제 완료");
        Debug.Log("========================================");
    }
    
    #endregion
    
    #region Phase 6: 룬 해금 시스템 테스트
    
    [MenuItem(MENU_ROOT + "Phase 6 - 해금/1. 🔓 룬 해금 테스트")]
    public static void Test_UnlockRune()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 6 Test] 룬 해금 테스트");
        Debug.Log("========================================");
        
        var inventoryManager = RuneInventoryManager.Instance;
        var enhanceManager = RuneEnhanceManager.Instance;
        
        if (inventoryManager == null || enhanceManager == null)
        {
            Debug.LogError("❌ 매니저를 찾을 수 없습니다!");
            return;
        }
        
        Debug.Log($"💎 현재 룬 파편: {GetTestFragments()}개");
        
        // 테스트할 룬 ID
        string testRuneId = "RUNE_BOSS_HUNTER";
        
        // 해금 시도
        var result = enhanceManager.TryUnlockRune(testRuneId);
        
        if (result == RuneEnhanceManager.UnlockResult.Success)
        {
            Debug.Log($"✅ 룬 해금 성공: {testRuneId}");
            Debug.Log($"💎 남은 룬 파편: {GetTestFragments()}개");
        }
        else
        {
            Debug.LogWarning($"❌ 룬 해금 실패: {result}");
        }
        
        // UI 갱신
        Test_RefreshUI();
        
        Debug.Log("========================================");
    }
    
    [MenuItem(MENU_ROOT + "Phase 6 - 해금/2. 💎 룬 파편 초기화 (1000개)")]
    public static void Test_ResetFragments()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 6 Test] 룬 파편 초기화");
        Debug.Log("========================================");
        
        var inventoryManager = RuneInventoryManager.Instance;
        
        if (inventoryManager == null)
        {
            Debug.LogError("❌ RuneInventoryManager를 찾을 수 없습니다!");
            return;
        }
        
        int oldFragments = GetTestFragments();
        SetTestFragments(1000);
        
        Debug.Log($"💎 [{DEFAULT_TEST_RUNE_ID}] 조각: {oldFragments}개 → {GetTestFragments()}개");
        Debug.Log("✅ 룬 파편 초기화 완료!");
        Debug.Log("========================================");
    }
    
    [MenuItem(MENU_ROOT + "Phase 6 - 해금/3. 🚀 전체 시나리오 (해금→강화)")]
    public static void Test_FullScenarioUnlockAndEnhance()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 6 Test] 전체 시나리오: 해금 → 강화");
        Debug.Log("========================================");
        
        var inventoryManager = RuneInventoryManager.Instance;
        var enhanceManager = RuneEnhanceManager.Instance;
        
        if (inventoryManager == null || enhanceManager == null)
        {
            Debug.LogError("❌ 매니저를 찾을 수 없습니다!");
            return;
        }
        
        // 1. 데이터 초기화
        Debug.Log("\n📌 1단계: 데이터 초기화");
        Test_ClearAllData();
        Test_ResetFragments();
        
        // 2. 룬 해금
        Debug.Log("\n📌 2단계: 룬 해금 (보스 사냥꾼)");
        string runeId = "RUNE_BOSS_HUNTER";
        var unlockResult = enhanceManager.TryUnlockRune(runeId);
        
        if (unlockResult == RuneEnhanceManager.UnlockResult.Success)
        {
            Debug.Log($"✅ 해금 성공: {runeId}");
        }
        else
        {
            Debug.LogError($"❌ 해금 실패: {unlockResult}");
            return;
        }
        
        // 3. 룬 찾기
        var allRunes = inventoryManager.GetAllRunes();
        if (allRunes.Count == 0)
        {
            Debug.LogError("❌ 해금된 룬을 찾을 수 없습니다!");
            return;
        }
        
        var targetRune = allRunes[0];
        Debug.Log($"📌 대상 룬: {targetRune}");
        
        // 4. 레벨업 (Lv.1 → Lv.3)
        Debug.Log("\n📌 3단계: 레벨업 (Lv.1 → Lv.3)");
        for (int i = 0; i < 2; i++)
        {
            var levelUpResult = enhanceManager.TryLevelUp(targetRune.instanceUID);
            if (levelUpResult == RuneEnhanceManager.LevelUpResult.Success)
            {
                Debug.Log($"✅ 레벨업 성공: Lv.{targetRune.currentLevel}");
            }
        }
        
        // 5. 최대 레벨까지 성장
        Debug.Log("\n📌 4단계: 최대 레벨까지 성장 (Lv.10)");
        while (targetRune.currentLevel < targetRune.GetCurrentMaxLevel())
        {
            enhanceManager.TryLevelUp(targetRune.instanceUID);
        }
        Debug.Log($"✅ 최대 레벨 도달: Lv.{targetRune.currentLevel}");
        
        // 6. 한계돌파
        Debug.Log("\n📌 5단계: 한계돌파");
        var limitBreakResult = enhanceManager.TryLimitBreak(targetRune.instanceUID);
        if (limitBreakResult == RuneEnhanceManager.LimitBreakResult.Success)
        {
            Debug.Log($"✅ 한계돌파 성공: +{targetRune.currentLimitBreak} (최대 Lv.{targetRune.GetCurrentMaxLevel()})");
        }
        
        // 7. UI 갱신
        Debug.Log("\n📌 6단계: UI 갱신");
        Test_RefreshUI();
        
        // 최종 상태
        Debug.Log("\n========================================");
        Debug.Log("[최종 상태]");
        Debug.Log($"  룬: {targetRune}");
        Debug.Log($"  레벨: Lv.{targetRune.currentLevel} / {targetRune.GetCurrentMaxLevel()}");
        Debug.Log($"  한계돌파: +{targetRune.currentLimitBreak}");
        Debug.Log($"  부옵션: {targetRune.allocatedSubStatModifierIds.Count}개");
        Debug.Log($"💎 남은 룬 파편: {GetTestFragments()}개");
        Debug.Log("✅ 전체 시나리오 테스트 완료!");
        Debug.Log("========================================");
    }
    
    #endregion
    
    #region Phase 5-2: 장착 및 강화 UI 테스트
    
    [MenuItem(MENU_ROOT + "Phase 5-2/1. 🎯 장착 슬롯 UI 갱신")]
    public static void Test_RefreshEquipSlots()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 5-2 Test] 장착 슬롯 UI 갱신");
        Debug.Log("========================================");
        
        var equipSlots = GameObject.FindObjectsOfType<RuneEquipSlotUI>(true);
        
        if (equipSlots == null || equipSlots.Length == 0)
        {
            Debug.LogError("❌ RuneEquipSlotUI를 찾을 수 없습니다!");
            return;
        }
        
        Debug.Log($"📌 {equipSlots.Length}개의 장착 슬롯 발견");
        
        foreach (var slot in equipSlots)
        {
            slot.RefreshSlot();
            Debug.Log($"  - 슬롯 {slot.GetSlotIndex()}: {(slot.GetEquippedRune() != null ? slot.GetEquippedRune().ToString() : "비어있음")}");
        }
        
        Debug.Log("✅ 장착 슬롯 UI 갱신 완료");
        Debug.Log("========================================");
    }
    
    [MenuItem(MENU_ROOT + "Phase 5-2/2. 🔍 강화 UI 선택 테스트")]
    public static void Test_EnhanceUISelection()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 5-2 Test] 강화 UI 선택 테스트");
        Debug.Log("========================================");
        
        var enhanceUI = GameObject.FindObjectOfType<RuneEnhanceUI>(true);
        
        if (enhanceUI == null)
        {
            Debug.LogError("❌ RuneEnhanceUI를 찾을 수 없습니다!");
            return;
        }
        
        // 강화 UI 패널 활성화
        if (!enhanceUI.gameObject.activeInHierarchy)
        {
            var parent = enhanceUI.transform.parent;
            if (parent != null)
            {
                parent.gameObject.SetActive(true);
            }
            enhanceUI.gameObject.SetActive(true);
        }
        
        // 첫 번째 룬 선택
        var inventoryManager = RuneInventoryManager.Instance;
        if (inventoryManager == null)
        {
            Debug.LogError("❌ RuneInventoryManager를 찾을 수 없습니다!");
            return;
        }
        
        var allRunes = inventoryManager.GetAllRunes();
        if (allRunes.Count == 0)
        {
            Debug.LogWarning("⚠️ 인벤토리에 룬이 없습니다. 먼저 테스트 룬을 생성하세요.");
            return;
        }
        
        var firstRune = allRunes[0];
        enhanceUI.SelectRune(firstRune);
        
        Debug.Log($"✅ 강화 UI에 룬 선택: {firstRune}");
        Debug.Log("========================================");
    }
    
    [MenuItem(MENU_ROOT + "Phase 5-2/3. ⚡ 선택된 룬 레벨업 테스트")]
    public static void Test_EnhanceLevelUp()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 5-2 Test] 선택된 룬 레벨업 테스트");
        Debug.Log("========================================");
        
        var inventoryManager = RuneInventoryManager.Instance;
        var enhanceManager = RuneEnhanceManager.Instance;
        
        if (inventoryManager == null || enhanceManager == null)
        {
            Debug.LogError("❌ 매니저를 찾을 수 없습니다!");
            return;
        }
        
        var allRunes = inventoryManager.GetAllRunes();
        if (allRunes.Count == 0)
        {
            Debug.LogWarning("⚠️ 인벤토리에 룬이 없습니다.");
            return;
        }
        
        var targetRune = allRunes[0];
        Debug.Log($"📌 레벨업 대상: {targetRune}");
        Debug.Log($"   현재 레벨: {targetRune.currentLevel}/{targetRune.GetCurrentMaxLevel()}");
        
        // 레벨업 시도
        var result = enhanceManager.TryLevelUp(targetRune.instanceUID);
        
        if (result == RuneEnhanceManager.LevelUpResult.Success)
        {
            Debug.Log($"✅ 레벨업 성공: Lv.{targetRune.currentLevel}");
        }
        else
        {
            Debug.LogWarning($"❌ 레벨업 실패: {result}");
        }
        
        // UI 갱신
        Test_RefreshUI();
        
        Debug.Log("========================================");
    }
    
    [MenuItem(MENU_ROOT + "Phase 5-2/4. 🌟 한계돌파 테스트 (파편 소모)")]
    public static void Test_LimitBreakWithFragments()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 5-2 Test] 한계돌파 테스트 (파편 소모)");
        Debug.Log("========================================");
        
        var inventoryManager = RuneInventoryManager.Instance;
        var enhanceManager = RuneEnhanceManager.Instance;
        
        if (inventoryManager == null || enhanceManager == null)
        {
            Debug.LogError("❌ 매니저를 찾을 수 없습니다!");
            return;
        }
        
        // 1. 베이스 룬 찾기 (최대 레벨 도달한 룬)
        var allRunes = inventoryManager.GetAllRunes();
        RuneInstance baseRune = null;
        
        foreach (var rune in allRunes)
        {
            if (rune.currentLevel >= rune.GetCurrentMaxLevel() && rune.currentLimitBreak < 5)
            {
                baseRune = rune;
                break;
            }
        }
        
        if (baseRune == null)
        {
            // 테스트용 룬 생성 및 최대 레벨까지 성장
            Debug.Log("📌 한계돌파 가능한 룬이 없어서 새로 생성합니다...");
            
            var runeData = RuneDatabase.GetRuneData("RUNE_BOSS_HUNTER");
            if (runeData == null)
            {
                Debug.LogError("❌ 룬 데이터를 찾을 수 없습니다!");
                return;
            }
            
            baseRune = inventoryManager.AddRune(runeData);
            
            // 최대 레벨까지 성장
            while (baseRune.currentLevel < baseRune.GetCurrentMaxLevel())
            {
                enhanceManager.TryLevelUp(baseRune.instanceUID);
            }
            
            Debug.Log($"✅ 테스트 룬 생성 및 최대 레벨 도달: {baseRune}");
        }
        
        Debug.Log($"📌 베이스 룬: {baseRune} (Lv.{baseRune.currentLevel}, 한돌+{baseRune.currentLimitBreak})");
        Debug.Log($"💎 현재 룬 파편: {GetTestFragments()}개");
        
        // 2. 한계돌파 실행 (파편 소모)
        var result = enhanceManager.TryLimitBreak(baseRune.instanceUID);
        
        if (result == RuneEnhanceManager.LimitBreakResult.Success)
        {
            Debug.Log($"✅ 한계돌파 성공: +{baseRune.currentLimitBreak} (최대 레벨: {baseRune.GetCurrentMaxLevel()})");
            Debug.Log($"💎 남은 룬 파편: {GetTestFragments()}개");
        }
        else
        {
            Debug.LogWarning($"❌ 한계돌파 실패: {result}");
        }
        
        // UI 갱신
        Test_RefreshUI();
        
        Debug.Log("========================================");
    }
    
    [MenuItem(MENU_ROOT + "Phase 5-2/5. 🚀 풀 시나리오 테스트 (장착+강화)")]
    public static void Test_FullScenarioPhase5_2()
    {
        Debug.Log("========================================");
        Debug.Log("[Phase 5-2 Test] 풀 시나리오 테스트");
        Debug.Log("========================================");
        
        // 매니저 참조
        var inventoryManager = RuneInventoryManager.Instance;
        var enhanceManager = RuneEnhanceManager.Instance;
        
        if (inventoryManager == null || enhanceManager == null)
        {
            Debug.LogError("❌ 매니저를 찾을 수 없습니다!");
            return;
        }
        
        // 1. 기존 데이터 클리어
        Debug.Log("📌 1단계: 기존 데이터 클리어");
        Test_ClearAllData();
        
        // 2. 테스트 룬 3개 생성
        Debug.Log("\n📌 2단계: 테스트 룬 3개 생성");
        
        var runeData1 = RuneDatabase.GetRuneData("RUNE_BOSS_HUNTER");
        var runeData2 = RuneDatabase.GetRuneData("RUNE_DEFENSE_BREAKER");
        var runeData3 = RuneDatabase.GetRuneData("RUNE_HIGH_HP_HUNTER");
        
        if (runeData1 != null)
        {
            inventoryManager.AddRune(runeData1);
            Debug.Log($"✅ 테스트 룬 생성: {runeData1.runeId}");
        }
        if (runeData2 != null)
        {
            inventoryManager.AddRune(runeData2);
            Debug.Log($"✅ 테스트 룬 생성: {runeData2.runeId}");
        }
        if (runeData3 != null)
        {
            inventoryManager.AddRune(runeData3);
            Debug.Log($"✅ 테스트 룬 생성: {runeData3.runeId}");
        }
        
        // 3. 첫 번째 룬 장착
        Debug.Log("\n📌 3단계: 첫 번째 룬 장착");
        Test_EquipFirstRune();
        
        // 4. 첫 번째 룬 레벨업 3회
        Debug.Log("\n📌 4단계: 첫 번째 룬 레벨업");
        
        var allRunes = inventoryManager.GetAllRunes();
        if (allRunes.Count > 0)
        {
            var targetRune = allRunes[0];
            Debug.Log($"   레벨업 대상: {targetRune}");
            
            for (int i = 0; i < 3; i++)
            {
                var result = enhanceManager.TryLevelUp(targetRune.instanceUID);
                if (result == RuneEnhanceManager.LevelUpResult.Success)
                {
                    Debug.Log($"   ✅ 레벨업 {i+1}/3 성공: Lv.{targetRune.currentLevel}");
                }
            }
        }
        
        // 5. 한계돌파 테스트
        Debug.Log("\n📌 5단계: 한계돌파 테스트");
        Test_LimitBreakWithFragments();
        
        // 6. UI 갱신
        Debug.Log("\n📌 6단계: 모든 UI 갱신");
        Test_RefreshUI();
        
        // 최종 상태 출력
        Debug.Log("\n========================================");
        Debug.Log("[최종 상태]");
        var finalRunes = inventoryManager.GetAllRunes();
        Debug.Log($"  총 룬 개수: {finalRunes.Count}");
        
        var runeManager = RuneManager.Instance;
        var equippedCount = runeManager.GetActiveRunes().Count;
        Debug.Log($"  장착된 룬: {equippedCount}");
        
        Debug.Log("✅ Phase 5-2 풀 시나리오 테스트 완료!");
        Debug.Log("========================================");
    }
    
    #endregion
}
