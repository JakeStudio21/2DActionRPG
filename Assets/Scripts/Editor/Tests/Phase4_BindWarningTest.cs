using UnityEngine;
using UnityEditor;
using Systems;

/// <summary>
/// Phase 4: 귀속 경고 시스템 테스트
/// - 귀속 경고 팝업
/// - 귀속 제약 검증
/// </summary>
public static class Phase4_BindWarningTest
{
    public static void RunAllTests()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🧪 Phase 4: 귀속 경고 시스템 테스트 시작");
        Debug.Log("═══════════════════════════════════════════════════════\n");

        int passedTests = 0;
        int totalTests = 6;

        // 사전 조건: PlayerDataManager, AccountDataManager 초기화
        InitializeManagers();
        
        // 각 테스트 전에 데이터 정리
        CleanupTestData();

        // Test 1: 귀속 경고 데이터 생성
        if (Test1_CreateBindWarningData()) passedTests++;

        CleanupTestData();

        // Test 2: 귀속되지 않은 아이템 장착 시도 (경고 필요)
        if (Test2_ShouldShowWarningForUnboundItem()) passedTests++;

        CleanupTestData();

        // Test 3: 이미 귀속된 아이템 장착 시도 (경고 불필요)
        if (Test3_NoWarningForBoundItem()) passedTests++;

        CleanupTestData();

        // Test 4: 다른 캐릭터 귀속 아이템 장착 시도 (거부)
        if (Test4_CannotEquipOtherCharacterBoundItem()) passedTests++;

        CleanupTestData();

        // Test 5: 귀속 아이템 계정 창고 이동 시도 (거부)
        if (Test5_CannotMoveBoundItemToStorage()) passedTests++;

        CleanupTestData();

        // Test 6: '다시 보지 않기' 설정 동작
        if (Test6_DontShowAgainSetting()) passedTests++;

        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"🎯 테스트 결과: {passedTests}/{totalTests} 통과");

        if (passedTests == totalTests)
        {
            Debug.Log("✅ Phase 4 테스트 100% 성공!");
        }
        else
        {
            Debug.LogError($"❌ Phase 4 테스트 실패: {totalTests - passedTests}개 실패");
        }

        Debug.Log("═══════════════════════════════════════════════════════\n");

        // 모든 테스트 후 최종 정리
        CleanupTestData();
    }

    private static void InitializeManagers()
    {
        if (PlayerDataManager.Instance == null)
        {
            var go = new GameObject("PlayerDataManager");
            var manager = go.AddComponent<PlayerDataManager>();
            if (manager.selectedPlayerData == null)
            {
                manager.selectedPlayerData = ScriptableObject.CreateInstance<SelectedPlayerData>();
                Debug.Log("✨ [Phase4Test] SelectedPlayerData 자동 생성");
            }
            Debug.Log("✨ [Phase4Test] PlayerDataManager 자동 생성");
        }

        if (!AccountDataManager.IsInitialized())
        {
            AccountDataManager.Initialize();
            Debug.Log("✨ [Phase4Test] AccountDataManager 자동 초기화");
        }
        
        // BindWarningManager 초기화
        if (BindWarningManager.Instance == null)
        {
            Debug.Log("✨ [Phase4Test] BindWarningManager 자동 초기화");
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 1: 귀속 경고 데이터 생성
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private static bool Test1_CreateBindWarningData()
    {
        Debug.Log("--- Test 1: 귀속 경고 데이터 생성 ---");
        try
        {
            var account = AccountDataManager.Instance;
            var itemId = account.RegisterNewInstance("Sword_S_Equipment");
            var itemData = account.GetInstance(itemId);
            itemData.enhancementLevel = 5;

            var warningData = new BindWarningData(
                itemId,
                itemData.templateName,
                itemData.enhancementLevel,
                EquipmentSlot.MainWeapon,
                0,
                "TestCharacter"
            );

            string message = warningData.GetWarningMessage();
            string simpleMessage = warningData.GetSimpleMessage();

            if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(simpleMessage))
            {
                Debug.LogError("❌ 경고 메시지 생성 실패");
                return false;
            }

            Debug.Log($"✅ 경고 메시지: {message}");
            Debug.Log($"✅ 간단 메시지: {simpleMessage}");
            Debug.Log("✅ Test 1 통과: 귀속 경고 데이터 생성 성공\n");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 1 실패: {e.Message}");
            return false;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 2: 귀속되지 않은 아이템 장착 시도 (경고 필요)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private static bool Test2_ShouldShowWarningForUnboundItem()
    {
        Debug.Log("--- Test 2: 귀속되지 않은 아이템 장착 시도 (경고 필요) ---");
        try
        {
            var playerData = CreateTestSlot();
            var account = AccountDataManager.Instance;

            // 아직 귀속되지 않은 아이템 생성
            var itemId = account.RegisterNewInstance("Bow_A_Equipment");
            playerData.GetSlotData(0).characterBagInstanceIds.Add(itemId);
            playerData.SaveSlotData(playerData.GetSlotData(0));

            // 경고가 필요한지 확인
            bool shouldShowWarning = BindWarningManager.Instance.ShouldShowWarning(itemId);

            if (!shouldShowWarning)
            {
                Debug.LogError("❌ 귀속되지 않은 아이템에 대해 경고가 필요하지 않다고 판단함");
                return false;
            }

            Debug.Log("✅ 귀속되지 않은 아이템 경고 필요 확인");
            Debug.Log("✅ Test 2 통과: 경고 필요 판정 성공\n");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 2 실패: {e.Message}");
            return false;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 3: 이미 귀속된 아이템 장착 시도 (경고 불필요)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private static bool Test3_NoWarningForBoundItem()
    {
        Debug.Log("--- Test 3: 이미 귀속된 아이템 장착 시도 (경고 불필요) ---");
        try
        {
            var playerData = CreateTestSlot();
            var account = AccountDataManager.Instance;

            // 귀속된 아이템 생성
            var itemId = account.RegisterNewInstance("Armor_S_Equipment");
            account.SetBind(itemId, 0);
            playerData.GetSlotData(0).characterBagInstanceIds.Add(itemId);
            playerData.SaveSlotData(playerData.GetSlotData(0));

            // 경고가 필요한지 확인
            bool shouldShowWarning = BindWarningManager.Instance.ShouldShowWarning(itemId);

            if (shouldShowWarning)
            {
                Debug.LogError("❌ 이미 귀속된 아이템에 대해 경고가 필요하다고 판단함");
                return false;
            }

            Debug.Log("✅ 이미 귀속된 아이템 경고 불필요 확인");
            Debug.Log("✅ Test 3 통과: 경고 불필요 판정 성공\n");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 3 실패: {e.Message}");
            return false;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 4: 다른 캐릭터 귀속 아이템 장착 시도 (거부)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private static bool Test4_CannotEquipOtherCharacterBoundItem()
    {
        Debug.Log("--- Test 4: 다른 캐릭터 귀속 아이템 장착 시도 (거부) ---");
        try
        {
            var playerData = PlayerDataManager.Instance;
            var account = AccountDataManager.Instance;

            // 슬롯 0 생성 및 선택
            CreateTestSlot(0, "Character0");
            
            // 슬롯 0에 귀속된 아이템 생성
            var itemId = account.RegisterNewInstance("Ring_S_Equipment");
            account.SetBind(itemId, 0);
            playerData.GetSlotData(0).characterBagInstanceIds.Add(itemId);
            playerData.SaveSlotData(playerData.GetSlotData(0));
            
            // 슬롯 1 생성 및 선택
            CreateTestSlot(1, "Character1");
            
            // 슬롯 1의 가방에 추가 (일반적으로는 불가능하지만 테스트를 위해)
            playerData.GetSlotData(1).characterBagInstanceIds.Add(itemId);
            playerData.SaveSlotData(playerData.GetSlotData(1));

            // 슬롯 1에서 장착 시도
            bool success = playerData.EquipV2(itemId, EquipmentSlot.Ring1);

            if (success)
            {
                Debug.LogError("❌ 다른 캐릭터 귀속 아이템 장착에 성공함 (실패해야 함)");
                return false;
            }

            Debug.Log("✅ 다른 캐릭터 귀속 아이템 장착 거부");
            Debug.Log("✅ Test 4 통과: 다른 캐릭터 귀속 아이템 장착 거부 성공\n");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 4 실패: {e.Message}");
            return false;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 5: 귀속 아이템 계정 창고 이동 시도 (거부)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private static bool Test5_CannotMoveBoundItemToStorage()
    {
        Debug.Log("--- Test 5: 귀속 아이템 계정 창고 이동 시도 (거부) ---");
        try
        {
            var playerData = CreateTestSlot();
            var account = AccountDataManager.Instance;

            // 귀속된 아이템 생성
            var itemId = account.RegisterNewInstance("Necklace_A_Equipment");
            account.SetBind(itemId, 0);
            playerData.GetSlotData(0).characterBagInstanceIds.Add(itemId);
            playerData.SaveSlotData(playerData.GetSlotData(0));

            // 계정 창고로 이동 시도
            bool success = playerData.MoveToAccountStorage(itemId);

            if (success)
            {
                Debug.LogError("❌ 귀속 아이템 계정 창고 이동에 성공함 (실패해야 함)");
                return false;
            }

            Debug.Log("✅ 귀속 아이템 계정 창고 이동 거부");
            Debug.Log("✅ Test 5 통과: 귀속 아이템 창고 이동 거부 성공\n");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 5 실패: {e.Message}");
            return false;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Test 6: '다시 보지 않기' 설정 동작
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private static bool Test6_DontShowAgainSetting()
    {
        Debug.Log("--- Test 6: '다시 보지 않기' 설정 동작 ---");
        try
        {
            var playerData = CreateTestSlot();
            var account = AccountDataManager.Instance;

            // '다시 보지 않기' 초기화
            UI.Popups.BindWarningPopup.ResetDontShowAgain();

            // 아직 귀속되지 않은 아이템 생성
            var itemId = account.RegisterNewInstance("Helmet_B_Equipment");
            playerData.GetSlotData(0).characterBagInstanceIds.Add(itemId);
            playerData.SaveSlotData(playerData.GetSlotData(0));

            // 초기 상태: 경고 필요
            bool shouldShow1 = BindWarningManager.Instance.ShouldShowWarning(itemId);
            if (!shouldShow1)
            {
                Debug.LogError("❌ '다시 보지 않기' 설정 전에 경고가 필요하지 않다고 판단함");
                return false;
            }

            // '다시 보지 않기' 설정
            PlayerPrefs.SetInt("BindWarning_DontShowAgain", 1);
            PlayerPrefs.Save();

            // '다시 보지 않기' 후: 경고 불필요
            bool shouldShow2 = BindWarningManager.Instance.ShouldShowWarning(itemId);
            if (shouldShow2)
            {
                Debug.LogError("❌ '다시 보지 않기' 설정 후에도 경고가 필요하다고 판단함");
                return false;
            }

            // 설정 초기화
            UI.Popups.BindWarningPopup.ResetDontShowAgain();

            Debug.Log("✅ '다시 보지 않기' 설정 동작 확인");
            Debug.Log("✅ Test 6 통과: '다시 보지 않기' 설정 성공\n");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 6 실패: {e.Message}");
            return false;
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 헬퍼 메서드
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private static PlayerDataManager CreateTestSlot(int slotIndex = 0, string characterName = "TestCharacter")
    {
        var playerData = PlayerDataManager.Instance;
        playerData.LoadAllSlots();

        // 기존 테스트 슬롯 삭제
        if (playerData.GetSlotData(slotIndex) != null && playerData.GetSlotData(slotIndex).isSlotUsed)
        {
            playerData.DeleteSlot(slotIndex);
        }
        
        // 새 슬롯 생성
        playerData.CreateNewSlot(slotIndex, PlayerType.Warrior, characterName);
        playerData.SelectSlot(slotIndex);
        
        return playerData;
    }

    private static void CleanupTestData()
    {
        var playerData = PlayerDataManager.Instance;
        var accountData = AccountDataManager.Instance;

        if (playerData != null)
        {
            // 모든 테스트 슬롯 삭제
            for (int i = 0; i < 3; i++)
            {
                if (playerData.GetSlotData(i) != null && playerData.GetSlotData(i).isSlotUsed)
                {
                    playerData.DeleteSlot(i);
                }
            }
            // SaveAllSlots 대신 개별 저장은 DeleteSlot에서 자동으로 처리됨
        }

        if (accountData != null)
        {
            accountData.ClearAllData();
            accountData.Save();
        }
        
        // '다시 보지 않기' 설정 초기화
        UI.Popups.BindWarningPopup.ResetDontShowAgain();
        
        Debug.Log("🧹 [Phase4Test] 테스트 데이터 정리 완료");
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Unity Editor 메뉴
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public static class Phase4TestMenu
{
    [MenuItem("Tools/Phase 4 Test")]
    public static void RunPhase4Test()
    {
        Phase4_BindWarningTest.RunAllTests();
    }
}

