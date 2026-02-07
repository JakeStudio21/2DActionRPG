using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Phase 3.5: 스테이지 종료 아이템 전송 테스트
/// </summary>
public static class Phase3_5_StageEndTest
{
    public static void RunAllTests()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🧪 Phase 3.5: 스테이지 종료 아이템 전송 테스트 시작");
        Debug.Log("═══════════════════════════════════════════════════════\n");
        
        int passedTests = 0;
        int totalTests = 5;
        
        // 사전 조건: PlayerDataManager + AccountDataManager 초기화
        SetupManagers();
        
        // Test 1: 일반 아이템 창고 전송
        if (Test1_TransferNormalItemsToStorage()) passedTests++;
        
        // Test 2: 귀속 아이템 유지
        if (Test2_KeepBoundItems()) passedTests++;
        
        // Test 3: 창고 가득 참 시 우편함 처리
        if (Test3_MailboxOnStorageFull()) passedTests++;
        
        // Test 4: 빈 가방 처리
        if (Test4_EmptyBagHandling()) passedTests++;
        
        // Test 5: 혼합 시나리오
        if (Test5_MixedScenario()) passedTests++;
        
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"🎯 테스트 결과: {passedTests}/{totalTests} 통과");
        
        if (passedTests == totalTests)
        {
            Debug.Log("✅ Phase 3.5 테스트 100% 성공!");
        }
        else
        {
            Debug.LogError($"❌ Phase 3.5 테스트 실패: {totalTests - passedTests}개 실패");
        }
        
        Debug.Log("═══════════════════════════════════════════════════════\n");
        
        // 정리
        CleanupTestData();
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 1: 일반 아이템 창고 전송
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test1_TransferNormalItemsToStorage()
    {
        Debug.Log("--- Test 1: 일반 아이템 창고 전송 ---");
        
        try
        {
            var playerData = PlayerDataManager.Instance;
            var account = AccountDataManager.Instance;
            
            // 테스트 슬롯 생성
            CreateTestSlot();
            
            // 가방에 아이템 3개 추가 (귀속 없음)
            var item1 = account.RegisterNewInstance("Sword_A_Equipment");
            var item2 = account.RegisterNewInstance("Bow_B_Equipment");
            var item3 = account.RegisterNewInstance("Armor_C_Equipment");
            
            var slotData = playerData.GetSlotData(0);
            slotData.characterBagInstanceIds.Add(item1);
            slotData.characterBagInstanceIds.Add(item2);
            slotData.characterBagInstanceIds.Add(item3);
            playerData.SaveSlotData(slotData);
            
            Debug.Log($"✅ 가방에 아이템 3개 추가: {item1}, {item2}, {item3}");
            
            // 전송 실행
            var transfer = CreateTransferComponent();
            transfer.TransferItemsToAccount();
            
            // 검증: 가방이 비었는지
            var bagAfter = playerData.GetCharacterBagV2();
            if (bagAfter.Count != 0)
            {
                Debug.LogError($"❌ 가방이 비지 않음: {bagAfter.Count}개 남음");
                return false;
            }
            
            // 검증: 창고에 3개 추가되었는지
            var accountData = account.GetAccountData();
            if (!accountData.sharedInventoryIds.Contains(item1) ||
                !accountData.sharedInventoryIds.Contains(item2) ||
                !accountData.sharedInventoryIds.Contains(item3))
            {
                Debug.LogError("❌ 창고에 아이템이 없음");
                return false;
            }
            
            Debug.Log("✅ 가방 비움, 창고에 3개 추가됨");
            Debug.Log("✅ Test 1 통과: 일반 아이템 창고 전송 성공\n");
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 1 실패: {e.Message}");
            return false;
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 2: 귀속 아이템 유지
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test2_KeepBoundItems()
    {
        Debug.Log("--- Test 2: 귀속 아이템 유지 ---");
        
        try
        {
            var playerData = PlayerDataManager.Instance;
            var account = AccountDataManager.Instance;
            
            // 테스트 슬롯 생성
            CreateTestSlot();
            
            // 가방에 아이템 2개 추가
            var itemNormal = account.RegisterNewInstance("Sword_S_Equipment");
            var itemBound = account.RegisterNewInstance("Bow_S_Equipment");
            
            var slotData = playerData.GetSlotData(0);
            slotData.characterBagInstanceIds.Add(itemNormal);
            slotData.characterBagInstanceIds.Add(itemBound);
            playerData.SaveSlotData(slotData);
            
            // itemBound를 슬롯 0에 귀속
            account.SetBind(itemBound, 0);
            
            Debug.Log($"✅ 가방에 일반 아이템: {itemNormal}, 귀속 아이템: {itemBound}");
            
            // 전송 실행
            var transfer = CreateTransferComponent();
            transfer.TransferItemsToAccount();
            
            // 검증: 가방에 귀속 아이템만 남았는지
            var bagAfter = playerData.GetCharacterBagV2();
            if (bagAfter.Count != 1 || !bagAfter.Contains(itemBound))
            {
                Debug.LogError($"❌ 가방에 귀속 아이템만 남아야 함: {bagAfter.Count}개");
                return false;
            }
            
            // 검증: 창고에 일반 아이템만 있는지
            var accountData = account.GetAccountData();
            if (!accountData.sharedInventoryIds.Contains(itemNormal))
            {
                Debug.LogError("❌ 창고에 일반 아이템이 없음");
                return false;
            }
            
            if (accountData.sharedInventoryIds.Contains(itemBound))
            {
                Debug.LogError("❌ 창고에 귀속 아이템이 있으면 안 됨");
                return false;
            }
            
            Debug.Log("✅ 일반 아이템은 창고로, 귀속 아이템은 가방에 유지");
            Debug.Log("✅ Test 2 통과: 귀속 아이템 유지 성공\n");
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 2 실패: {e.Message}");
            return false;
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 3: 창고 가득 참 시 우편함 처리
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test3_MailboxOnStorageFull()
    {
        Debug.Log("--- Test 3: 창고 가득 참 시 우편함 처리 ---");
        
        try
        {
            var playerData = PlayerDataManager.Instance;
            var account = AccountDataManager.Instance;
            
            // 테스트 슬롯 생성
            CreateTestSlot();
            
            // 창고를 가득 채움 (maxSharedInventorySize 사용)
            int maxSize = account.GetAccountData().maxSharedInventorySize;
            for (int i = 0; i < maxSize; i++)
            {
                var dummyItem = account.RegisterNewInstance("DummyItem");
                account.TryAddToShared(dummyItem);
            }
            
            Debug.Log($"✅ 창고를 {maxSize}개로 가득 채움");
            
            // 가방에 아이템 2개 추가
            var item1 = account.RegisterNewInstance("Sword_D_Equipment");
            var item2 = account.RegisterNewInstance("Bow_D_Equipment");
            
            var slotData = playerData.GetSlotData(0);
            slotData.characterBagInstanceIds.Add(item1);
            slotData.characterBagInstanceIds.Add(item2);
            playerData.SaveSlotData(slotData);
            
            Debug.Log($"✅ 가방에 아이템 2개 추가: {item1}, {item2}");
            
            // 전송 실행
            var transfer = CreateTransferComponent();
            transfer.TransferItemsToAccount();
            
            // 검증: 가방이 비었는지
            var bagAfter = playerData.GetCharacterBagV2();
            if (bagAfter.Count != 0)
            {
                Debug.LogError($"❌ 가방이 비지 않음: {bagAfter.Count}개 남음");
                return false;
            }
            
            // 검증: 우편함에 2개 추가되었는지
            var accountData = account.GetAccountData();
            if (!accountData.mailboxIds.Contains(item1) || !accountData.mailboxIds.Contains(item2))
            {
                Debug.LogError("❌ 우편함에 아이템이 없음");
                return false;
            }
            
            Debug.Log("✅ 창고 가득 참 → 우편함으로 2개 이동");
            Debug.Log("✅ Test 3 통과: 우편함 처리 성공\n");
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 3 실패: {e.Message}");
            return false;
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 4: 빈 가방 처리
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test4_EmptyBagHandling()
    {
        Debug.Log("--- Test 4: 빈 가방 처리 ---");
        
        try
        {
            // 테스트 슬롯 생성 (빈 가방)
            CreateTestSlot();
            
            Debug.Log("✅ 빈 가방으로 테스트");
            
            // 전송 실행
            var transfer = CreateTransferComponent();
            transfer.TransferItemsToAccount();
            
            // 검증: 에러 없이 완료되었는지 (로그 확인)
            Debug.Log("✅ 빈 가방 처리 정상 완료");
            Debug.Log("✅ Test 4 통과: 빈 가방 처리 성공\n");
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 4 실패: {e.Message}");
            return false;
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 5: 혼합 시나리오
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test5_MixedScenario()
    {
        Debug.Log("--- Test 5: 혼합 시나리오 ---");
        
        try
        {
            var playerData = PlayerDataManager.Instance;
            var account = AccountDataManager.Instance;
            
            // 테스트 슬롯 생성
            CreateTestSlot();
            
            // 가방에 아이템 5개 추가
            var itemNormal1 = account.RegisterNewInstance("Sword_A_Equipment");
            var itemNormal2 = account.RegisterNewInstance("Bow_B_Equipment");
            var itemBound1 = account.RegisterNewInstance("Armor_S_Equipment");
            var itemBound2 = account.RegisterNewInstance("Ring_S_Equipment");
            var itemNormal3 = account.RegisterNewInstance("Necklace_C_Equipment");
            
            var slotData = playerData.GetSlotData(0);
            slotData.characterBagInstanceIds.Add(itemNormal1);
            slotData.characterBagInstanceIds.Add(itemNormal2);
            slotData.characterBagInstanceIds.Add(itemBound1);
            slotData.characterBagInstanceIds.Add(itemBound2);
            slotData.characterBagInstanceIds.Add(itemNormal3);
            playerData.SaveSlotData(slotData);
            
            // 귀속 설정
            account.SetBind(itemBound1, 0);
            account.SetBind(itemBound2, 0);
            
            Debug.Log("✅ 가방: 일반 3개, 귀속 2개");
            
            // 전송 실행
            var transfer = CreateTransferComponent();
            transfer.TransferItemsToAccount();
            
            // 검증: 가방에 귀속 2개만 남았는지
            var bagAfter = playerData.GetCharacterBagV2();
            if (bagAfter.Count != 2)
            {
                Debug.LogError($"❌ 가방에 귀속 2개만 남아야 함: {bagAfter.Count}개");
                return false;
            }
            
            if (!bagAfter.Contains(itemBound1) || !bagAfter.Contains(itemBound2))
            {
                Debug.LogError("❌ 가방에 귀속 아이템이 없음");
                return false;
            }
            
            // 검증: 창고에 일반 3개 추가되었는지
            var accountData = account.GetAccountData();
            if (!accountData.sharedInventoryIds.Contains(itemNormal1) ||
                !accountData.sharedInventoryIds.Contains(itemNormal2) ||
                !accountData.sharedInventoryIds.Contains(itemNormal3))
            {
                Debug.LogError("❌ 창고에 일반 아이템 3개가 없음");
                return false;
            }
            
            Debug.Log("✅ 일반 3개 → 창고, 귀속 2개 → 가방 유지");
            Debug.Log("✅ Test 5 통과: 혼합 시나리오 성공\n");
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 5 실패: {e.Message}");
            return false;
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 헬퍼 메서드
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static void SetupManagers()
    {
        // PlayerDataManager 초기화
        if (PlayerDataManager.Instance == null)
        {
            var go = new GameObject("PlayerDataManager");
            var manager = go.AddComponent<PlayerDataManager>();
            
            if (manager.selectedPlayerData == null)
            {
                manager.selectedPlayerData = ScriptableObject.CreateInstance<SelectedPlayerData>();
                Debug.Log("✨ [Phase3.5Test] SelectedPlayerData 자동 생성");
            }
            
            Debug.Log("✨ [Phase3.5Test] PlayerDataManager 자동 생성");
        }
        
        // AccountDataManager 초기화
        if (!AccountDataManager.IsInitialized())
        {
            AccountDataManager.Initialize();
            Debug.Log("✨ [Phase3.5Test] AccountDataManager 초기화");
        }
    }
    
    private static void CreateTestSlot()
    {
        var playerData = PlayerDataManager.Instance;
        var account = AccountDataManager.Instance;
        
        // 기존 슬롯 삭제
        if (playerData.GetSlotData(0) != null && playerData.GetSlotData(0).isSlotUsed)
        {
            playerData.DeleteSlot(0);
        }
        
        // AccountData 초기화 (테스트 독립성 보장)
        var accountData = account.GetAccountData();
        accountData.sharedInventoryIds.Clear();
        accountData.mailboxIds.Clear();
        accountData.itemInstances.Clear();
        accountData.binds.Clear();
        account.Save();
        
        Debug.Log("🧹 [Phase3.5Test] AccountData 초기화 완료");
        
        // 새 슬롯 생성
        playerData.CreateNewSlot(0, PlayerType.Warrior, "TestCharacter");
        playerData.SelectSlot(0);
    }
    
    private static StageEndItemTransfer CreateTransferComponent()
    {
        var go = new GameObject("StageEndItemTransfer_Test");
        var transfer = go.AddComponent<StageEndItemTransfer>();
        transfer.enableLogs = true;
        return transfer;
    }
    
    private static void CleanupTestData()
    {
        var playerData = PlayerDataManager.Instance;
        
        if (playerData != null && playerData.GetSlotData(0) != null && playerData.GetSlotData(0).isSlotUsed)
        {
            playerData.DeleteSlot(0);
        }
        
        // AccountData 정리 (선택적)
        // AccountDataManager의 데이터는 계정 단위이므로 필요시 수동 정리
        Debug.Log("🧹 [Phase3.5Test] 테스트 데이터 정리 완료");
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Unity Editor 메뉴
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public static class Phase3_5TestMenu
{
    [MenuItem("Tools/Phase 3.5 Test")]
    public static void RunPhase3_5Test()
    {
        Phase3_5_StageEndTest.RunAllTests();
    }
}

