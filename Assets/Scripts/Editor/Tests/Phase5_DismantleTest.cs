using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Phase 5: 분해 시스템 테스트
/// </summary>
public class Phase5_DismantleTest
{
    [MenuItem("Tools/Dismantle System/Phase 5 - Dismantle Test")]
    public static void RunPhase5Test()
    {
        RunAllTests();
    }
    
    public static void RunAllTests()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🧪 Phase 5: 분해 시스템 테스트 시작");
        Debug.Log("═══════════════════════════════════════════════════════\n");
        
        int passedTests = 0;
        int totalTests = 8;
        
        // 초기화
        InitializeManagers();
        
        // 테스트 1~8
        if (Test1_BasicDismantle()) passedTests++;
        CleanupTestData();
        
        if (Test2_GradeBasedRewards()) passedTests++;
        CleanupTestData();
        
        if (Test3_EnhancementBonus()) passedTests++;
        CleanupTestData();
        
        if (Test4_CannotDismantleEquipped()) passedTests++;
        CleanupTestData();
        
        if (Test5_MaterialAccumulation()) passedTests++;
        CleanupTestData();
        
        if (Test6_BatchDismantle()) passedTests++;
        CleanupTestData();
        
        if (Test7_WarningNeeded()) passedTests++;
        CleanupTestData();
        
        if (Test8_ItemCompletelyRemoved()) passedTests++;
        CleanupTestData();
        
        // 결과 출력
        Debug.Log("\n═══════════════════════════════════════════════════════");
        Debug.Log($"🎯 테스트 결과: {passedTests}/{totalTests} 통과");
        
        if (passedTests == totalTests)
        {
            Debug.Log("✅ Phase 5 테스트 100% 성공!");
        }
        else
        {
            Debug.LogError($"❌ Phase 5 테스트 실패: {totalTests - passedTests}개 실패");
        }
        
        Debug.Log("═══════════════════════════════════════════════════════\n");
        
        // 최종 정리
        CleanupTestData();
    }
    
    // ========================================
    // Test 1: 기본 분해 (D등급 → 조각 5개)
    // ========================================
    
    private static bool Test1_BasicDismantle()
    {
        Debug.Log("--- Test 1: 기본 분해 (D등급) ---");
        
        try
        {
            var account = AccountDataManager.Instance;
            var accountData = account.GetAccountData();
            
            // D등급 아이템 생성
            var itemId = account.RegisterNewInstance("ITEM_ARMOR_ASSASIN_D_Equipment");
            accountData.sharedInventoryIds.Add(itemId);
            
            // 분해 전 재료 확인 (D등급 방어구 → 방어구 파편)
            int fragmentsBefore = account.GetMaterialCount(MaterialType.ArmorFragment);
            
            // 분해 실행
            var rewards = Systems.DismantleSystem.DismantleItem(itemId);
            
            // 검증 1: 재료 획득
            if (!rewards.ContainsKey(MaterialType.ArmorFragment))
            {
                Debug.LogError("❌ 방어구 강화 파편을 획득하지 못함");
                return false;
            }
            
            int fragmentsGained = rewards[MaterialType.ArmorFragment];
            if (fragmentsGained != 5)
            {
                Debug.LogError($"❌ 방어구 강화 파편 획득량 오류: {fragmentsGained} (예상: 5)");
                return false;
            }
            
            // 검증 2: 재료 저장 확인
            int fragmentsAfter = account.GetMaterialCount(MaterialType.ArmorFragment);
            if (fragmentsAfter != fragmentsBefore + 5)
            {
                Debug.LogError($"❌ 재료 저장 오류: {fragmentsAfter} (예상: {fragmentsBefore + 5})");
                return false;
            }
            
            // 검증 3: 아이템 삭제 확인
            if (account.GetInstance(itemId) != null)
            {
                Debug.LogError("❌ 아이템이 삭제되지 않음");
                return false;
            }
            
            Debug.Log($"✅ D등급 분해: 조각 {fragmentsGained}개 획득");
            Debug.Log("✅ Test 1 통과: 기본 분해 성공\n");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Test 1 실패: {ex.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 2: 등급별 재료 차등 (S/A/B/C/D)
    // ========================================
    
    private static bool Test2_GradeBasedRewards()
    {
        Debug.Log("--- Test 2: 등급별 재료 차등 ---");
        
        try
        {
            var account = AccountDataManager.Instance;
            var accountData = account.GetAccountData();
            
            // 등급별 + 장비 타입별 예상 재료량
            var expectedFragments = new Dictionary<string, (MaterialType type, int amount)>
            {
                // 방어구 (D/C/B) → 방어구 강화 파편
                { "ITEM_ARMOR_ASSASIN_D_Equipment", (MaterialType.ArmorFragment, 5) },
                { "ITEM_BELT_ASSASIN_C_Equipment", (MaterialType.ArmorFragment, 10) },
                { "ITEM_BOOTS_WARRIOR_B_Equipment", (MaterialType.ArmorFragment, 25) },
                
                // 방어구 (A) → 방어구 강화 결정
                { "ITEM_GLOVES_WARRIOR_A_Equipment", (MaterialType.ArmorCrystal, 50) },
                
                // 무기 (S) → 무기 강화 결정
                { "ITEM_BOW_S_Equipment", (MaterialType.WeaponCrystal, 100) }
            };
            
            foreach (var kvp in expectedFragments)
            {
                string templateName = kvp.Key;
                MaterialType expectedType = kvp.Value.type;
                int expectedAmount = kvp.Value.amount;
                
                // 아이템 생성
                var itemId = account.RegisterNewInstance(templateName);
                accountData.sharedInventoryIds.Add(itemId);
                
                // 분해
                var rewards = Systems.DismantleSystem.DismantleItem(itemId);
                
                // 검증
                if (!rewards.ContainsKey(expectedType))
                {
                    Debug.LogError($"❌ {templateName}: {expectedType} 조각을 획득하지 못함");
                    return false;
                }
                
                int actualAmount = rewards[expectedType];
                if (actualAmount != expectedAmount)
                {
                    Debug.LogError($"❌ {templateName}: 조각량 오류 (실제: {actualAmount}, 예상: {expectedAmount})");
                    return false;
                }
                
                Debug.Log($"✅ {templateName}: {expectedType} {actualAmount}개");
            }
            
            Debug.Log("✅ Test 2 통과: 등급별 재료 차등 성공\n");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Test 2 실패: {ex.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 3: 강화 보너스 (+5 아이템 → 50% 추가)
    // ========================================
    
    private static bool Test3_EnhancementBonus()
    {
        Debug.Log("--- Test 3: 강화 보너스 ---");
        
        try
        {
            var account = AccountDataManager.Instance;
            var accountData = account.GetAccountData();
            
            // +5 강화 B등급 아이템 생성
            var itemId = account.RegisterNewInstance("ITEM_BOOTS_WARRIOR_B_Equipment");
            var itemData = account.GetInstance(itemId);
            itemData.enhancementLevel = 5;
            accountData.sharedInventoryIds.Add(itemId);
            
            // 분해
            var rewards = Systems.DismantleSystem.DismantleItem(itemId);
            
            // 검증: 기본 25 + 보너스 12 (25 * 0.1 * 5) = 37 (방어구 파편)
            int expectedAmount = 25 + Mathf.FloorToInt(25 * 0.1f * 5);
            int actualAmount = rewards[MaterialType.ArmorFragment];
            
            if (actualAmount != expectedAmount)
            {
                Debug.LogError($"❌ 강화 보너스 오류: {actualAmount} (예상: {expectedAmount})");
                return false;
            }
            
            Debug.Log($"✅ +5 강화 B등급: 강화 파편 {actualAmount}개 (기본 25 + 보너스 {actualAmount - 25})");
            Debug.Log("✅ Test 3 통과: 강화 보너스 적용 성공\n");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Test 3 실패: {ex.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 4: 장착 아이템 분해 거부
    // ========================================
    
    private static bool Test4_CannotDismantleEquipped()
    {
        Debug.Log("--- Test 4: 장착 아이템 분해 거부 ---");
        
        try
        {
            var account = AccountDataManager.Instance;
            var playerData = PlayerDataManager.Instance;
            
            // 테스트 슬롯 생성
            CreateTestSlot(0, "TestCharacter");
            
            // 아이템 생성 및 장착
            var itemId = account.RegisterNewInstance("ITEM_BOW_S_Equipment");
            playerData.GetSlotData(0).characterBagInstanceIds.Add(itemId);
            bool equipped = playerData.EquipV2(itemId, EquipmentSlot.MainWeapon);
            
            if (!equipped)
            {
                Debug.LogError("❌ 아이템 장착 실패");
                return false;
            }
            
            // 분해 시도 (실패해야 함)
            var rewards = Systems.DismantleSystem.DismantleItem(itemId);
            
            if (rewards.Count > 0)
            {
                Debug.LogError("❌ 장착 중인 아이템 분해에 성공함 (실패해야 함)");
                return false;
            }
            
            // 아이템이 여전히 존재하는지 확인
            if (account.GetInstance(itemId) == null)
            {
                Debug.LogError("❌ 장착 중인 아이템이 삭제됨");
                return false;
            }
            
            Debug.Log("✅ 장착 중인 아이템 분해 거부됨");
            Debug.Log("✅ Test 4 통과: 장착 아이템 보호 성공\n");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Test 4 실패: {ex.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 5: 재료 누적 (2번 분해 → 합산)
    // ========================================
    
    private static bool Test5_MaterialAccumulation()
    {
        Debug.Log("--- Test 5: 재료 누적 ---");
        
        try
        {
            var account = AccountDataManager.Instance;
            var accountData = account.GetAccountData();
            
            // 초기 재료 확인 (방어구 파편)
            int fragmentsBefore = account.GetMaterialCount(MaterialType.ArmorFragment);
            
            // 첫 번째 C등급 방어구 분해
            var itemId1 = account.RegisterNewInstance("ITEM_BELT_ASSASIN_C_Equipment");
            accountData.sharedInventoryIds.Add(itemId1);
            Systems.DismantleSystem.DismantleItem(itemId1);
            
            int fragmentsAfterFirst = account.GetMaterialCount(MaterialType.ArmorFragment);
            
            // 두 번째 C등급 방어구 분해
            var itemId2 = account.RegisterNewInstance("ITEM_BELT_ASSASIN_C_Equipment");
            accountData.sharedInventoryIds.Add(itemId2);
            Systems.DismantleSystem.DismantleItem(itemId2);
            
            int fragmentsAfterSecond = account.GetMaterialCount(MaterialType.ArmorFragment);
            
            // 검증
            int expectedAfterFirst = fragmentsBefore + 10;
            int expectedAfterSecond = fragmentsBefore + 20;
            
            if (fragmentsAfterFirst != expectedAfterFirst)
            {
                Debug.LogError($"❌ 첫 번째 분해 후: {fragmentsAfterFirst} (예상: {expectedAfterFirst})");
                return false;
            }
            
            if (fragmentsAfterSecond != expectedAfterSecond)
            {
                Debug.LogError($"❌ 두 번째 분해 후: {fragmentsAfterSecond} (예상: {expectedAfterSecond})");
                return false;
            }
            
            Debug.Log($"✅ 재료 누적: {fragmentsBefore} → {fragmentsAfterFirst} → {fragmentsAfterSecond}");
            Debug.Log("✅ Test 5 통과: 재료 누적 성공\n");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Test 5 실패: {ex.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 6: 일괄 분해 (C등급 이하 10개)
    // ========================================
    
    private static bool Test6_BatchDismantle()
    {
        Debug.Log("--- Test 6: 일괄 분해 ---");
        
        try
        {
            var account = AccountDataManager.Instance;
            var accountData = account.GetAccountData();
            
            // C등급 5개, D등급 5개 생성
            for (int i = 0; i < 5; i++)
            {
                var itemC = account.RegisterNewInstance("ITEM_BELT_ASSASIN_C_Equipment");
                accountData.sharedInventoryIds.Add(itemC);
                
                var itemD = account.RegisterNewInstance("ITEM_ARMOR_ASSASIN_D_Equipment");
                accountData.sharedInventoryIds.Add(itemD);
            }
            
            // 필터 설정: C등급 이하
            var filter = new Systems.BatchDismantleSystem.DismantleFilter
            {
                maxGrade = ItemGrade.C,
                excludeBound = true,
                excludeEquipped = true
            };
            
            // 일괄 분해 실행
            var result = Systems.BatchDismantleSystem.ExecuteWithFilter(filter);
            
            // 검증
            if (result.totalItems != 10)
            {
                Debug.LogError($"❌ 분해 대상 수 오류: {result.totalItems} (예상: 10)");
                return false;
            }
            
            if (result.successCount != 10)
            {
                Debug.LogError($"❌ 성공 수 오류: {result.successCount} (예상: 10)");
                return false;
            }
            
            // 재료 확인: 방어구 강화 파편 75개 (C 5개*10 + D 5개*5 = 50+25)
            int expectedFragment = (5 * 10) + (5 * 5); // C: 50, D: 25
            
            if (!result.totalRewards.ContainsKey(MaterialType.ArmorFragment) || 
                result.totalRewards[MaterialType.ArmorFragment] != expectedFragment)
            {
                int actual = result.totalRewards.GetValueOrDefault(MaterialType.ArmorFragment);
                Debug.LogError($"❌ 방어구 강화 파편 오류: {actual} (예상: {expectedFragment})");
                return false;
            }
            
            Debug.Log($"✅ 일괄 분해: {result.successCount}개 성공");
            Debug.Log($"✅ 획득 재료: 방어구 강화 파편 {result.totalRewards[MaterialType.ArmorFragment]}개 (C:50 + D:25)");
            Debug.Log("✅ Test 6 통과: 일괄 분해 성공\n");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Test 6 실패: {ex.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 7: 경고 필요 여부 (S등급, 귀속, +10)
    // ========================================
    
    private static bool Test7_WarningNeeded()
    {
        Debug.Log("--- Test 7: 경고 필요 여부 판단 ---");
        
        try
        {
            var account = AccountDataManager.Instance;
            var accountData = account.GetAccountData();
            
            // 1. 일반 D등급 (경고 불필요)
            var itemD = account.RegisterNewInstance("ITEM_ARMOR_ASSASIN_D_Equipment");
            accountData.sharedInventoryIds.Add(itemD);
            
            if (Systems.DismantleSystem.NeedsWarning(itemD))
            {
                Debug.LogError("❌ D등급 아이템에 경고 필요 (불필요해야 함)");
                return false;
            }
            
            // 2. S등급 (경고 필요)
            var itemS = account.RegisterNewInstance("ITEM_BOW_S_Equipment");
            accountData.sharedInventoryIds.Add(itemS);
            
            if (!Systems.DismantleSystem.NeedsWarning(itemS))
            {
                Debug.LogError("❌ S등급 아이템에 경고 불필요 (필요해야 함)");
                return false;
            }
            
            // 3. +10 강화 (경고 필요)
            var itemEnhanced = account.RegisterNewInstance("ITEM_BELT_ASSASIN_C_Equipment");
            var itemData = account.GetInstance(itemEnhanced);
            itemData.enhancementLevel = 10;
            accountData.sharedInventoryIds.Add(itemEnhanced);
            
            if (!Systems.DismantleSystem.NeedsWarning(itemEnhanced))
            {
                Debug.LogError("❌ +10 강화 아이템에 경고 불필요 (필요해야 함)");
                return false;
            }
            
            // 4. 귀속 아이템 (경고 필요)
            var itemBound = account.RegisterNewInstance("ITEM_BOOTS_WARRIOR_B_Equipment");
            account.SetBind(itemBound, 0);
            accountData.sharedInventoryIds.Add(itemBound);
            
            if (!Systems.DismantleSystem.NeedsWarning(itemBound))
            {
                Debug.LogError("❌ 귀속 아이템에 경고 불필요 (필요해야 함)");
                return false;
            }
            
            Debug.Log("✅ D등급: 경고 불필요");
            Debug.Log("✅ S등급: 경고 필요");
            Debug.Log("✅ +10 강화: 경고 필요");
            Debug.Log("✅ 귀속: 경고 필요");
            Debug.Log("✅ Test 7 통과: 경고 판단 성공\n");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Test 7 실패: {ex.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 8: 분해 후 아이템 완전 삭제 검증
    // ========================================
    
    private static bool Test8_ItemCompletelyRemoved()
    {
        Debug.Log("--- Test 8: 아이템 완전 삭제 검증 ---");
        
        try
        {
            var account = AccountDataManager.Instance;
            var accountData = account.GetAccountData();
            
            // 아이템 생성 및 귀속
            var itemId = account.RegisterNewInstance("ITEM_GLOVES_WARRIOR_A_Equipment");
            accountData.sharedInventoryIds.Add(itemId);
            account.SetBind(itemId, 0);
            
            // 분해 전 존재 확인
            if (account.GetInstance(itemId) == null)
            {
                Debug.LogError("❌ 분해 전 아이템이 없음");
                return false;
            }
            
            if (!accountData.sharedInventoryIds.Contains(itemId))
            {
                Debug.LogError("❌ 분해 전 창고에 아이템이 없음");
                return false;
            }
            
            var bindInfoBefore = account.GetBindInfo(itemId);
            if (!bindInfoBefore.isBound)
            {
                Debug.LogError("❌ 분해 전 귀속 정보가 없음");
                return false;
            }
            
            // 분해 실행
            Systems.DismantleSystem.DismantleItem(itemId);
            
            // 분해 후 완전 삭제 확인
            var accountDataAfter = account.GetAccountData(); // 분해 후 최신 상태 가져오기
            
            // 1. 인스턴스 데이터 삭제
            if (account.GetInstance(itemId) != null)
            {
                Debug.LogError("❌ 인스턴스 데이터가 남아있음");
                return false;
            }
            
            // 2. 창고에서 제거
            if (accountDataAfter.sharedInventoryIds.Contains(itemId))
            {
                Debug.LogError("❌ 창고에 아이템이 남아있음");
                return false;
            }
            
            // 3. 귀속 정보 제거
            var bindInfoAfter = account.GetBindInfo(itemId);
            if (bindInfoAfter.isBound)
            {
                Debug.LogError("❌ 귀속 정보가 남아있음");
                return false;
            }
            
            Debug.Log("✅ 인스턴스 데이터 삭제됨");
            Debug.Log("✅ 창고에서 제거됨");
            Debug.Log("✅ 귀속 정보 제거됨");
            Debug.Log("✅ Test 8 통과: 완전 삭제 검증 성공\n");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Test 8 실패: {ex.Message}");
            return false;
        }
    }
    
    // ========================================
    // 유틸리티
    // ========================================
    
    private static void InitializeManagers()
    {
        // PlayerDataManager 초기화
        if (PlayerDataManager.Instance == null)
        {
            var go = new GameObject("PlayerDataManager_Test");
            go.AddComponent<PlayerDataManager>();
            
            // SelectedPlayerData ScriptableObject 생성
            if (PlayerDataManager.Instance.selectedPlayerData == null)
            {
                PlayerDataManager.Instance.selectedPlayerData = ScriptableObject.CreateInstance<SelectedPlayerData>();
            }
        }
        
        // AccountDataManager 초기화
        if (!AccountDataManager.IsInitialized())
        {
            AccountDataManager.Initialize(new JsonFileStorage());
        }
        
        // DismantleWarningManager 초기화
        var _ = Managers.DismantleWarningManager.Instance;
    }
    
    private static void CreateTestSlot(int slotIndex, string characterName)
    {
        var playerData = PlayerDataManager.Instance;
        playerData.LoadAllSlots();
        
        if (playerData.GetSlotData(slotIndex) == null || !playerData.GetSlotData(slotIndex).isSlotUsed)
        {
            playerData.CreateNewSlot(slotIndex, PlayerType.Warrior, characterName);
        }
        
        playerData.SelectSlot(slotIndex);
    }
    
    private static void CleanupTestData()
    {
        var playerData = PlayerDataManager.Instance;
        var accountData = AccountDataManager.Instance;
        
        if (playerData != null)
        {
            for (int i = 0; i < 3; i++)
            {
                if (playerData.GetSlotData(i) != null && playerData.GetSlotData(i).isSlotUsed)
                {
                    playerData.DeleteSlot(i);
                }
            }
        }
        
        if (accountData != null)
        {
            accountData.ClearAllData();
            accountData.Save();
        }
        
        Debug.Log("🧹 [Phase5Test] 테스트 데이터 정리 완료");
    }
}

/// <summary>
/// Unity Editor 메뉴에 Phase 5 테스트 추가
/// </summary>
public static class Phase5TestMenu
{
    [MenuItem("Tools/Dismantle System/Run All Dismantle Tests")]
    public static void RunPhase5Test()
    {
        Phase5_DismantleTest.RunPhase5Test();
    }
}

