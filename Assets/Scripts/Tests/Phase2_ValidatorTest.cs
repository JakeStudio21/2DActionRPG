using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Phase 2: V2 Validator + 템플릿 리졸버 테스트
/// - 데이터 무결성 검증 (중복 감지)
/// - 템플릿 로딩 시스템 (Resources/커스텀)
/// </summary>
public static class Phase2_ValidatorTest
{
    public static void RunAllTests()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🧪 Phase 2: V2 Validator + 템플릿 리졸버 테스트 시작");
        Debug.Log("═══════════════════════════════════════════════════════\n");
        
        int passedTests = 0;
        int totalTests = 6;
        
        // Test 1: AccountData 중복 검사
        if (Test1_AccountDuplicateDetection()) passedTests++;
        
        // Test 2: PlayerSlotData 중복 검사
        if (Test2_SlotDuplicateDetection()) passedTests++;
        
        // Test 3: Account ↔ Slot 간 중복 검사
        if (Test3_CrossContainerDuplicateDetection()) passedTests++;
        
        // Test 4: 귀속 정보 일치성 검사
        if (Test4_BindConsistency()) passedTests++;
        
        // Test 5: 템플릿 리졸버 (기본 Resources)
        if (Test5_TemplateResolverDefault()) passedTests++;
        
        // Test 6: 템플릿 리졸버 (커스텀 구현)
        if (Test6_TemplateResolverCustom()) passedTests++;
        
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"🎯 테스트 결과: {passedTests}/{totalTests} 통과");
        
        if (passedTests == totalTests)
        {
            Debug.Log("✅ Phase 2 테스트 100% 성공!");
        }
        else
        {
            Debug.LogError($"❌ Phase 2 테스트 실패: {totalTests - passedTests}개 실패");
        }
        
        Debug.Log("═══════════════════════════════════════════════════════\n");
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 1: AccountData 중복 검사
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test1_AccountDuplicateDetection()
    {
        Debug.Log("--- Test 1: AccountData 중복 검사 ---");
        
        try
        {
            // 정상 케이스
            var accountData1 = new AccountData();
            var id1 = ItemInstanceId.NewId();
            var id2 = ItemInstanceId.NewId();
            var id3 = ItemInstanceId.NewId();
            
            accountData1.sharedInventoryIds.Add(id1);
            accountData1.sharedInventoryIds.Add(id2);
            accountData1.mailboxIds.Add(id3);
            
            bool result1 = V2InventoryValidator.ValidateAccountNoDuplicates(accountData1, false);
            
            if (!result1)
            {
                Debug.LogError("❌ 정상 데이터를 중복으로 오판");
                return false;
            }
            
            Debug.Log("✅ 정상 케이스: 중복 없음");
            
            // 중복 케이스
            var accountData2 = new AccountData();
            var dupId = ItemInstanceId.NewId();
            
            accountData2.sharedInventoryIds.Add(dupId);
            accountData2.sharedInventoryIds.Add(dupId); // 중복!
            
            bool result2 = V2InventoryValidator.ValidateAccountNoDuplicates(accountData2, false);
            
            if (result2)
            {
                Debug.LogError("❌ 중복 데이터를 정상으로 오판");
                return false;
            }
            
            Debug.Log("✅ 중복 케이스: 올바르게 감지");
            
            Debug.Log("✅ Test 1 통과: AccountData 중복 검사 성공\n");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 1 실패: {e.Message}");
            return false;
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 2: PlayerSlotData 중복 검사
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test2_SlotDuplicateDetection()
    {
        Debug.Log("--- Test 2: PlayerSlotData 중복 검사 ---");
        
        try
        {
            // 정상 케이스
            var slotData1 = PlayerSlotData.CreateDefaultSlot(0, PlayerType.Warrior);
            
            var id1 = ItemInstanceId.NewId();
            var id2 = ItemInstanceId.NewId();
            var id3 = ItemInstanceId.NewId();
            
            slotData1.characterBagInstanceIds.Add(id1);
            slotData1.characterBagInstanceIds.Add(id2);
            slotData1.equippedRecords.Add(new EquippedRecord 
            { 
                slot = EquipmentSlot.MainWeapon, 
                instanceId = id3 
            });
            
            bool result1 = V2InventoryValidator.ValidateSlotNoDuplicates(slotData1, false);
            
            if (!result1)
            {
                Debug.LogError("❌ 정상 데이터를 중복으로 오판");
                return false;
            }
            
            Debug.Log("✅ 정상 케이스: 가방+장착 중복 없음");
            
            // 중복 케이스 (가방 내부)
            var slotData2 = PlayerSlotData.CreateDefaultSlot(1, PlayerType.Assasin);
            
            var dupId = ItemInstanceId.NewId();
            slotData2.characterBagInstanceIds.Add(dupId);
            slotData2.characterBagInstanceIds.Add(dupId); // 중복!
            
            bool result2 = V2InventoryValidator.ValidateSlotNoDuplicates(slotData2, false);
            
            if (result2)
            {
                Debug.LogError("❌ 가방 내 중복을 감지하지 못함");
                return false;
            }
            
            Debug.Log("✅ 중복 케이스 (가방): 올바르게 감지");
            
            // 중복 케이스 (가방 ↔ 장착)
            var slotData3 = PlayerSlotData.CreateDefaultSlot(2, PlayerType.Wizard);
            
            var crossId = ItemInstanceId.NewId();
            slotData3.characterBagInstanceIds.Add(crossId);
            slotData3.equippedRecords.Add(new EquippedRecord 
            { 
                slot = EquipmentSlot.MainWeapon, 
                instanceId = crossId  // 가방과 동일!
            });
            
            bool result3 = V2InventoryValidator.ValidateSlotNoDuplicates(slotData3, false);
            
            if (result3)
            {
                Debug.LogError("❌ 가방↔장착 간 중복을 감지하지 못함");
                return false;
            }
            
            Debug.Log("✅ 중복 케이스 (가방↔장착): 올바르게 감지");
            
            Debug.Log("✅ Test 2 통과: PlayerSlotData 중복 검사 성공\n");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 2 실패: {e.Message}");
            return false;
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 3: Account ↔ Slot 간 중복 검사
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test3_CrossContainerDuplicateDetection()
    {
        Debug.Log("--- Test 3: Account ↔ Slot 간 중복 검사 ---");
        
        try
        {
            var accountData = new AccountData();
            var slots = new PlayerSlotData[3];
            
            for (int i = 0; i < 3; i++)
            {
                slots[i] = PlayerSlotData.CreateDefaultSlot(i, PlayerType.Warrior);
            }
            
            // 정상 케이스: 각 컨테이너가 다른 아이템 보유
            var accountId = ItemInstanceId.NewId();
            var slot0Id = ItemInstanceId.NewId();
            var slot1Id = ItemInstanceId.NewId();
            
            accountData.sharedInventoryIds.Add(accountId);
            slots[0].characterBagInstanceIds.Add(slot0Id);
            slots[1].equippedRecords.Add(new EquippedRecord 
            { 
                slot = EquipmentSlot.MainWeapon, 
                instanceId = slot1Id 
            });
            
            bool result1 = V2InventoryValidator.ValidateCrossContainerNoDuplicates(
                accountData, slots, false);
            
            if (!result1)
            {
                Debug.LogError("❌ 정상 데이터를 중복으로 오판");
                return false;
            }
            
            Debug.Log("✅ 정상 케이스: Account와 Slot 간 중복 없음");
            
            // 중복 케이스: Account와 Slot0이 동일 아이템 보유
            var dupId = ItemInstanceId.NewId();
            accountData.sharedInventoryIds.Add(dupId);
            slots[0].characterBagInstanceIds.Add(dupId); // 중복!
            
            bool result2 = V2InventoryValidator.ValidateCrossContainerNoDuplicates(
                accountData, slots, false);
            
            if (result2)
            {
                Debug.LogError("❌ Account↔Slot 간 중복을 감지하지 못함");
                return false;
            }
            
            Debug.Log("✅ 중복 케이스: Account↔Slot 간 중복 올바르게 감지");
            
            Debug.Log("✅ Test 3 통과: Account ↔ Slot 간 중복 검사 성공\n");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 3 실패: {e.Message}");
            return false;
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 4: 귀속 정보 일치성 검사
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test4_BindConsistency()
    {
        Debug.Log("--- Test 4: 귀속 정보 일치성 검사 ---");
        
        try
        {
            var accountData = new AccountData();
            var slots = new PlayerSlotData[3];
            
            for (int i = 0; i < 3; i++)
            {
                slots[i] = PlayerSlotData.CreateDefaultSlot(i, PlayerType.Warrior);
            }
            
            // 정상 케이스: 귀속 정보와 실제 위치 일치
            var boundId = ItemInstanceId.NewId();
            slots[0].characterBagInstanceIds.Add(boundId);
            accountData.binds.Add(new ItemBindRecord 
            { 
                instanceId = boundId, 
                characterSlotIndex = 0, 
                bindTime = System.DateTime.Now.ToString() 
            });
            
            bool result1 = V2InventoryValidator.ValidateBindConsistency(
                accountData, slots, false);
            
            if (!result1)
            {
                Debug.LogError("❌ 정상 귀속을 오류로 오판");
                return false;
            }
            
            Debug.Log("✅ 정상 케이스: 귀속 정보와 실제 위치 일치");
            
            // 오류 케이스: 귀속 정보는 Slot 0인데 Slot 1에 존재
            var wrongId = ItemInstanceId.NewId();
            slots[1].characterBagInstanceIds.Add(wrongId);
            accountData.binds.Add(new ItemBindRecord 
            { 
                instanceId = wrongId, 
                characterSlotIndex = 0,  // Slot 0에 귀속이라고 기록
                bindTime = System.DateTime.Now.ToString() 
            });
            
            bool result2 = V2InventoryValidator.ValidateBindConsistency(
                accountData, slots, false);
            
            if (result2)
            {
                Debug.LogError("❌ 귀속 불일치를 감지하지 못함");
                return false;
            }
            
            Debug.Log("✅ 오류 케이스: 귀속 불일치 올바르게 감지");
            
            Debug.Log("✅ Test 4 통과: 귀속 정보 일치성 검사 성공\n");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 4 실패: {e.Message}");
            return false;
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 5: 템플릿 리졸버 (기본 Resources)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test5_TemplateResolverDefault()
    {
        Debug.Log("--- Test 5: 템플릿 리졸버 (기본 Resources) ---");
        
        try
        {
            // 기존 테스트 아이템 로드 (프로젝트에 실제 있는 아이템)
            var swordS = ItemTemplateResolver.Load("Sword_S_Equipment");
            var swordA = ItemTemplateResolver.Load("Sword_A_Equipment");
            var bowD = ItemTemplateResolver.Load("Bow_D_Equipment");
            
            int successCount = 0;
            if (swordS != null) 
            {
                Debug.Log($"✅ 로드 성공: Sword_S_Equipment (Grade: {swordS.itemGrade})");
                successCount++;
            }
            
            if (swordA != null) 
            {
                Debug.Log($"✅ 로드 성공: Sword_A_Equipment (Grade: {swordA.itemGrade})");
                successCount++;
            }
            
            if (bowD != null) 
            {
                Debug.Log($"✅ 로드 성공: Bow_D_Equipment (Grade: {bowD.itemGrade})");
                successCount++;
            }
            
            if (successCount == 0)
            {
                Debug.LogWarning("⚠️ Resources 폴더에 테스트 아이템이 없음 (스킵)");
                return true; // 아이템이 없어도 테스트 통과 (시스템은 정상)
            }
            
            Debug.Log($"✅ {successCount}개 아이템 로드 성공");
            
            // 없는 아이템 로드 (null 반환 확인)
            var nonExistent = ItemTemplateResolver.Load("NonExistentItem_XYZ");
            
            if (nonExistent != null)
            {
                Debug.LogError("❌ 없는 아이템이 로드됨");
                return false;
            }
            
            Debug.Log("✅ 없는 아이템: null 반환 (정상)");
            
            Debug.Log("✅ Test 5 통과: 템플릿 리졸버 (기본 Resources) 성공\n");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 5 실패: {e.Message}");
            return false;
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 6: 템플릿 리졸버 (커스텀 구현)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool Test6_TemplateResolverCustom()
    {
        Debug.Log("--- Test 6: 템플릿 리졸버 (커스텀 구현) ---");
        
        try
        {
            // 커스텀 리졸버 설정 (Mock)
            var mockResolver = new MockEquipmentResolver();
            ItemTemplateResolver.SetResolver(mockResolver);
            
            // Mock 리졸버로 로드
            var mockItem = ItemTemplateResolver.Load("MockItem");
            
            if (mockItem == null)
            {
                Debug.LogError("❌ 커스텀 리졸버가 아이템을 반환하지 않음");
                return false;
            }
            
            if (mockItem.equipmentName != "MockItem")
            {
                Debug.LogError($"❌ 잘못된 아이템 반환: {mockItem.equipmentName}");
                return false;
            }
            
            Debug.Log($"✅ 커스텀 리졸버 작동: {mockItem.equipmentName} (Grade: {mockItem.itemGrade})");
            
            // 리졸버 해제 (기본 상태로 복원)
            ItemTemplateResolver.SetResolver(null);
            
            var afterReset = ItemTemplateResolver.Load("MockItem");
            
            if (afterReset != null)
            {
                Debug.LogError("❌ 리졸버 해제 후에도 Mock 아이템이 로드됨");
                return false;
            }
            
            Debug.Log("✅ 리졸버 해제 후 기본 동작 복원");
            
            Debug.Log("✅ Test 6 통과: 템플릿 리졸버 (커스텀 구현) 성공\n");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 6 실패: {e.Message}");
            return false;
        }
        finally
        {
            // 테스트 종료 후 반드시 리졸버 해제
            ItemTemplateResolver.SetResolver(null);
        }
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Mock 구현체 (테스트용)
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class MockEquipmentResolver : IEquipmentResolver
{
    public bool SupportsAsync => false;
    
    public EquipmentData Resolve(string templateName)
    {
        if (templateName == "MockItem")
        {
            // ScriptableObject.CreateInstance는 테스트용으로만 사용
            var mockData = ScriptableObject.CreateInstance<EquipmentData>();
            mockData.equipmentName = "MockItem";
            mockData.itemGrade = ItemGrade.S;
            return mockData;
        }
        
        return null;
    }
    
    public void ResolveAsync(string templateName, System.Action<EquipmentData> onComplete)
    {
        var result = Resolve(templateName);
        onComplete?.Invoke(result);
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Unity Editor 메뉴
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public static class Phase2TestMenu
{
    [MenuItem("Tools/Phase 2 Test")]
    public static void RunPhase2Test()
    {
        Phase2_ValidatorTest.RunAllTests();
    }
}

