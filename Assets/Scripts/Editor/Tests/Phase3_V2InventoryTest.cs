using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Phase 3: PlayerDataManager V2 API 테스트
/// - V2 인벤토리/장착 시스템
/// - 가방 ↔ 창고 ↔ 우편함 이동
/// - 원자성 보장 테스트
/// </summary>
public static class Phase3_V2InventoryTest
{
    public static void RunAllTests()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🧪 Phase 3: PlayerDataManager V2 API 테스트 시작");
        Debug.Log("═══════════════════════════════════════════════════════\n");
        
        int passedTests = 0;
        int totalTests = 7;
        
        // 사전 조건: PlayerDataManager 초기화
        if (PlayerDataManager.Instance == null)
        {
            var go = new UnityEngine.GameObject("PlayerDataManager");
            var manager = go.AddComponent<PlayerDataManager>();
            
            // SelectedPlayerData ScriptableObject 생성
            if (manager.selectedPlayerData == null)
            {
                manager.selectedPlayerData = UnityEngine.ScriptableObject.CreateInstance<SelectedPlayerData>();
                Debug.Log("✨ [Phase3Test] SelectedPlayerData 자동 생성");
            }
            
            Debug.Log("✨ [Phase3Test] PlayerDataManager 자동 생성");
        }
        
        // 사전 조건: AccountDataManager 초기화
        if (!AccountDataManager.IsInitialized())
        {
            AccountDataManager.Initialize();
        }
        
        // Test 1: V2 아이템 장착 (정상)
        if (Test1_EquipV2Normal()) passedTests++;
        
        // Test 2: V2 아이템 해제 (정상)
        if (Test2_UnequipV2Normal()) passedTests++;
        
        // Test 3: 가방 → 창고 이동
        if (Test3_MoveToAccountStorage()) passedTests++;
        
        // Test 4: 창고 → 가방 이동 (❌ 삭제됨: V2 시스템에서는 보관창고 → 직접 착용)
        // if (Test4_MoveFromAccountStorage()) passedTests++;
        Debug.Log("⚠️ Test 4 스킵: MoveFromAccountStorage() 기능 제거됨");
        
        // Test 5: 우편함 수령
        if (Test5_ClaimFromMailbox()) passedTests++;
        
        // Test 6: 장착 시 인벤토리 가득 참 (우편함 처리)
        if (Test6_EquipWithFullInventory()) passedTests++;
        
        // Test 7: 해제 시 인벤토리 가득 참 (우편함 처리)
        if (Test7_UnequipWithFullInventory()) passedTests++;
        
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"🎯 테스트 결과: {passedTests}/{totalTests} 통과");
        
        if (passedTests == totalTests)
        {
            Debug.Log("✅ Phase 3 테스트 100% 성공!");
        }
        else
        {
            Debug.LogError($"❌ Phase 3 테스트 실패: {totalTests - passedTests}개 실패");
        }
        
        Debug.Log("═══════════════════════════════════════════════════════\n");
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 1: V2 아이템 장착 (정상)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test1_EquipV2Normal()
    {
        Debug.Log("--- Test 1: V2 아이템 장착 (정상) ---");
        
        try
        {
            // 테스트용 슬롯 생성
            var playerData = CreateTestSlot();
            var account = AccountDataManager.Instance;
            
            // 테스트용 아이템 생성
            var itemId = account.RegisterNewInstance("Sword_S_Equipment");
            
            // 가방에 추가
            playerData.GetSlotData(0).characterBagInstanceIds.Add(itemId);
            playerData.SaveSlotData(playerData.GetSlotData(0));
            
            Debug.Log($"✅ 테스트 아이템 생성: {itemId}");
            
            // 장착 시도
            playerData.SelectSlot(0);
            bool result = playerData.EquipV2(itemId, EquipmentSlot.MainWeapon);
            
            if (!result)
            {
                Debug.LogError("❌ 장착 실패");
                return false;
            }
            
            // 검증: 가방에서 제거되었는지
            var bag = playerData.GetCharacterBagV2();
            if (bag.Contains(itemId))
            {
                Debug.LogError("❌ 가방에서 제거되지 않음");
                return false;
            }
            
            // 검증: 장착 목록에 추가되었는지
            var equipped = playerData.GetEquippedRecordsV2();
            bool found = equipped.Exists(r => r.slot == EquipmentSlot.MainWeapon && r.instanceId == itemId);
            
            if (!found)
            {
                Debug.LogError("❌ 장착 목록에 없음");
                return false;
            }
            
            // 검증: 귀속되었는지
            var bindInfo = account.GetBindInfo(itemId);
            if (!bindInfo.isBound || bindInfo.characterSlotIndex != 0)
            {
                Debug.LogError("❌ 귀속되지 않음");
                return false;
            }
            
            Debug.Log("✅ 장착 성공, 가방에서 제거됨, 귀속됨");
            Debug.Log("✅ Test 1 통과: V2 아이템 장착 성공\n");
            
            // 정리
            CleanupTestSlot();
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 1 실패: {e.Message}");
            CleanupTestSlot();
            return false;
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 2: V2 아이템 해제 (정상)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test2_UnequipV2Normal()
    {
        Debug.Log("--- Test 2: V2 아이템 해제 (정상) ---");
        
        try
        {
            // 테스트용 슬롯 생성
            var playerData = CreateTestSlot();
            var account = AccountDataManager.Instance;
            
            // 테스트용 아이템 생성 및 장착
            var itemId = account.RegisterNewInstance("Sword_A_Equipment");
            var slotData = playerData.GetSlotData(0);
            slotData.equippedRecords.Add(new EquippedRecord 
            { 
                slot = EquipmentSlot.MainWeapon, 
                instanceId = itemId 
            });
            playerData.SaveSlotData(slotData);
            account.SetBind(itemId, 0);
            
            Debug.Log($"✅ 테스트 아이템 장착: {itemId}");
            
            // 해제 시도
            playerData.SelectSlot(0);
            bool result = playerData.UnequipV2(EquipmentSlot.MainWeapon);
            
            if (!result)
            {
                Debug.LogError("❌ 해제 실패");
                return false;
            }
            
            // 검증: 가방에 추가되었는지
            var bag = playerData.GetCharacterBagV2();
            if (!bag.Contains(itemId))
            {
                Debug.LogError("❌ 가방에 추가되지 않음");
                return false;
            }
            
            // 검증: 장착 목록에서 제거되었는지
            var equipped = playerData.GetEquippedRecordsV2();
            bool stillEquipped = equipped.Exists(r => r.slot == EquipmentSlot.MainWeapon && r.instanceId == itemId);
            
            if (stillEquipped)
            {
                Debug.LogError("❌ 장착 목록에 여전히 존재");
                return false;
            }
            
            Debug.Log("✅ 해제 성공, 가방에 추가됨");
            Debug.Log("✅ Test 2 통과: V2 아이템 해제 성공\n");
            
            // 정리
            CleanupTestSlot();
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 2 실패: {e.Message}");
            CleanupTestSlot();
            return false;
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 3: 가방 → 창고 이동
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test3_MoveToAccountStorage()
    {
        Debug.Log("--- Test 3: 가방 → 창고 이동 ---");
        
        try
        {
            // 테스트용 슬롯 생성
            var playerData = CreateTestSlot();
            var account = AccountDataManager.Instance;
            
            // 테스트용 아이템 생성
            var itemId = account.RegisterNewInstance("Bow_D_Equipment");
            
            // 가방에 추가
            var slotData = playerData.GetSlotData(0);
            slotData.characterBagInstanceIds.Add(itemId);
            playerData.SaveSlotData(slotData);
            
            Debug.Log($"✅ 테스트 아이템 가방에 추가: {itemId}");
            
            // 창고로 이동 시도
            playerData.SelectSlot(0);
            bool result = playerData.MoveToAccountStorage(itemId);
            
            if (!result)
            {
                Debug.LogError("❌ 창고 이동 실패");
                return false;
            }
            
            // 검증: 가방에서 제거되었는지
            var bag = playerData.GetCharacterBagV2();
            if (bag.Contains(itemId))
            {
                Debug.LogError("❌ 가방에서 제거되지 않음");
                return false;
            }
            
            // 검증: 창고에 추가되었는지
            var accountData = account.GetAccountData();
            if (!accountData.sharedInventoryIds.Contains(itemId))
            {
                Debug.LogError("❌ 창고에 추가되지 않음");
                return false;
            }
            
            Debug.Log("✅ 창고 이동 성공");
            Debug.Log("✅ Test 3 통과: 가방 → 창고 이동 성공\n");
            
            // 정리
            CleanupTestSlot();
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 3 실패: {e.Message}");
            CleanupTestSlot();
            return false;
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 4: 창고 → 가방 이동 (❌ 삭제됨: V2 시스템에서는 보관창고 → 직접 착용)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    /* ❌ MoveFromAccountStorage() 기능 제거로 인한 주석 처리
    private static bool Test4_MoveFromAccountStorage()
    {
        Debug.Log("--- Test 4: 창고 → 가방 이동 ---");
        
        try
        {
            // 테스트용 슬롯 생성
            var playerData = CreateTestSlot();
            var account = AccountDataManager.Instance;
            
            // 테스트용 아이템 생성
            var itemId = account.RegisterNewInstance("Armor_C_Equipment");
            
            // 창고에 추가
            account.TryAddToShared(itemId);
            
            Debug.Log($"✅ 테스트 아이템 창고에 추가: {itemId}");
            
            // 가방으로 이동 시도
            playerData.SelectSlot(0);
            bool result = playerData.MoveFromAccountStorage(itemId);
            
            if (!result)
            {
                Debug.LogError("❌ 가방 이동 실패");
                return false;
            }
            
            // 검증: 창고에서 제거되었는지
            var accountData = account.GetAccountData();
            if (accountData.sharedInventoryIds.Contains(itemId))
            {
                Debug.LogError("❌ 창고에서 제거되지 않음");
                return false;
            }
            
            // 검증: 가방에 추가되었는지
            var bag = playerData.GetCharacterBagV2();
            if (!bag.Contains(itemId))
            {
                Debug.LogError("❌ 가방에 추가되지 않음");
                return false;
            }
            
            Debug.Log("✅ 가방 이동 성공");
            Debug.Log("✅ Test 4 통과: 창고 → 가방 이동 성공\n");
            
            // 정리
            CleanupTestSlot();
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 4 실패: {e.Message}");
            CleanupTestSlot();
            return false;
        }
    }
    */  // ❌ Test4_MoveFromAccountStorage() 주석 처리 끝
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 5: 우편함 수령
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test5_ClaimFromMailbox()
    {
        Debug.Log("--- Test 5: 우편함 수령 ---");
        
        try
        {
            // 테스트용 슬롯 생성
            var playerData = CreateTestSlot();
            var account = AccountDataManager.Instance;
            
            // 테스트용 아이템 생성
            var itemId = account.RegisterNewInstance("Ring_A_Equipment");
            
            // 우편함에 추가
            account.MoveToMailbox(itemId);
            
            Debug.Log($"✅ 테스트 아이템 우편함에 추가: {itemId}");
            
            // 우편함에서 수령
            playerData.SelectSlot(0);
            bool result = playerData.ClaimFromMailbox(itemId);
            
            if (!result)
            {
                Debug.LogError("❌ 우편함 수령 실패");
                return false;
            }
            
            // 검증: 우편함에서 제거되었는지
            var accountData = account.GetAccountData();
            if (accountData.mailboxIds.Contains(itemId))
            {
                Debug.LogError("❌ 우편함에서 제거되지 않음");
                return false;
            }
            
            // 검증: 가방에 추가되었는지
            var bag = playerData.GetCharacterBagV2();
            if (!bag.Contains(itemId))
            {
                Debug.LogError("❌ 가방에 추가되지 않음");
                return false;
            }
            
            Debug.Log("✅ 우편함 수령 성공");
            Debug.Log("✅ Test 5 통과: 우편함 수령 성공\n");
            
            // 정리
            CleanupTestSlot();
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 5 실패: {e.Message}");
            CleanupTestSlot();
            return false;
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 6: 장착 시 인벤토리 가득 참 (우편함 처리)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test6_EquipWithFullInventory()
    {
        Debug.Log("--- Test 6: 장착 시 인벤토리 가득 참 (우편함 처리) ---");
        
        try
        {
            // 테스트용 슬롯 생성
            var playerData = CreateTestSlot();
            var account = AccountDataManager.Instance;
            
            // 가방을 가득 채움 (MaxInventorySize = 16)
            var slotData = playerData.GetSlotData(0);
            for (int i = 0; i < 16; i++)
            {
                var dummyId = account.RegisterNewInstance("DummyItem");
                slotData.characterBagInstanceIds.Add(dummyId);
            }
            
            // 기존 장착 아이템
            var oldItemId = account.RegisterNewInstance("Old_Sword");
            slotData.equippedRecords.Add(new EquippedRecord 
            { 
                slot = EquipmentSlot.MainWeapon, 
                instanceId = oldItemId 
            });
            
            // 새로 장착할 아이템 (가방에는 없음, 일시적으로 추가)
            var newItemId = account.RegisterNewInstance("New_Sword");
            slotData.characterBagInstanceIds.Add(newItemId); // 17개째 (가득 참 상태)
            
            playerData.SaveSlotData(slotData);
            
            Debug.Log($"✅ 가방 가득 참 (16개), 새 아이템: {newItemId}");
            
            // 장착 시도 (우편함 처리 허용)
            playerData.SelectSlot(0);
            bool result = playerData.EquipV2(newItemId, EquipmentSlot.MainWeapon, allowMailboxOnFull: true);
            
            if (!result)
            {
                Debug.LogError("❌ 장착 실패");
                return false;
            }
            
            // 검증: 기존 아이템이 우편함으로 이동했는지
            var accountData = account.GetAccountData();
            if (!accountData.mailboxIds.Contains(oldItemId))
            {
                Debug.LogError("❌ 기존 아이템이 우편함으로 이동하지 않음");
                return false;
            }
            
            // 검증: 새 아이템이 장착되었는지
            var equipped = playerData.GetEquippedRecordsV2();
            bool found = equipped.Exists(r => r.slot == EquipmentSlot.MainWeapon && r.instanceId == newItemId);
            
            if (!found)
            {
                Debug.LogError("❌ 새 아이템이 장착되지 않음");
                return false;
            }
            
            Debug.Log("✅ 장착 성공, 기존 아이템 우편함으로 이동");
            Debug.Log("✅ Test 6 통과: 인벤토리 가득 참 시 우편함 처리 성공\n");
            
            // 정리
            CleanupTestSlot();
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 6 실패: {e.Message}");
            CleanupTestSlot();
            return false;
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 7: 해제 시 인벤토리 가득 참 (우편함 처리)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test7_UnequipWithFullInventory()
    {
        Debug.Log("--- Test 7: 해제 시 인벤토리 가득 참 (우편함 처리) ---");
        
        try
        {
            // 테스트용 슬롯 생성
            var playerData = CreateTestSlot();
            var account = AccountDataManager.Instance;
            
            // 가방을 가득 채움 (MaxInventorySize = 16)
            var slotData = playerData.GetSlotData(0);
            for (int i = 0; i < 16; i++)
            {
                var dummyId = account.RegisterNewInstance("DummyItem2");
                slotData.characterBagInstanceIds.Add(dummyId);
            }
            
            // 장착된 아이템
            var equippedItemId = account.RegisterNewInstance("Equipped_Sword");
            slotData.equippedRecords.Add(new EquippedRecord 
            { 
                slot = EquipmentSlot.MainWeapon, 
                instanceId = equippedItemId 
            });
            
            playerData.SaveSlotData(slotData);
            
            Debug.Log($"✅ 가방 가득 참 (16개), 장착 아이템: {equippedItemId}");
            
            // 해제 시도 (우편함 처리 허용)
            playerData.SelectSlot(0);
            bool result = playerData.UnequipV2(EquipmentSlot.MainWeapon, allowMailboxOnFull: true);
            
            if (!result)
            {
                Debug.LogError("❌ 해제 실패");
                return false;
            }
            
            // 검증: 아이템이 우편함으로 이동했는지
            var accountData = account.GetAccountData();
            if (!accountData.mailboxIds.Contains(equippedItemId))
            {
                Debug.LogError("❌ 아이템이 우편함으로 이동하지 않음");
                return false;
            }
            
            // 검증: 장착 해제되었는지
            var equipped = playerData.GetEquippedRecordsV2();
            bool stillEquipped = equipped.Exists(r => r.slot == EquipmentSlot.MainWeapon && r.instanceId == equippedItemId);
            
            if (stillEquipped)
            {
                Debug.LogError("❌ 장착 해제되지 않음");
                return false;
            }
            
            Debug.Log("✅ 해제 성공, 아이템 우편함으로 이동");
            Debug.Log("✅ Test 7 통과: 인벤토리 가득 참 시 우편함 처리 성공\n");
            
            // 정리
            CleanupTestSlot();
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 7 실패: {e.Message}");
            CleanupTestSlot();
            return false;
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 헬퍼 메서드
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static PlayerDataManager CreateTestSlot()
    {
        var playerData = PlayerDataManager.Instance;
        
        if (playerData == null)
        {
            Debug.LogError("❌ PlayerDataManager.Instance가 null입니다");
            return null;
        }
        
        // 기존 테스트 슬롯 삭제
        if (playerData.GetSlotData(0) != null && playerData.GetSlotData(0).isSlotUsed)
        {
            playerData.DeleteSlot(0);
        }
        
        // 새 슬롯 생성
        playerData.CreateNewSlot(0, PlayerType.Warrior, "TestCharacter");
        
        return playerData;
    }
    
    private static void CleanupTestSlot()
    {
        var playerData = PlayerDataManager.Instance;
        
        if (playerData != null && playerData.GetSlotData(0) != null && playerData.GetSlotData(0).isSlotUsed)
        {
            playerData.DeleteSlot(0);
        }
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Unity Editor 메뉴
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public static class Phase3TestMenu
{
    [MenuItem("Tools/Phase 3 Test")]
    public static void RunPhase3Test()
    {
        Phase3_V2InventoryTest.RunAllTests();
    }
}

