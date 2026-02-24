using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Managers;
using Systems;

namespace Tests
{
    /// <summary>
    /// Phase 6: 합성 시스템 테스트
    /// </summary>
    public class Phase6_FusionTest
    {
        [MenuItem("Tools/Phase 6 Test")]
        public static void RunPhase6Test()
        {
            RunAllTests();
        }

        public static void RunAllTests()
        {
            Debug.Log("═══════════════════════════════════════════════════════");
            Debug.Log("🧪 Phase 6: 합성 시스템 테스트 시작");
            Debug.Log("═══════════════════════════════════════════════════════\n");

            int passedTests = 0;
            int totalTests = 8;

            // 초기화
            InitializeManagers();

            // 테스트 1~8
            if (Test1_BasicFusion()) passedTests++;
            CleanupTestData();

            if (Test2_DifferentGradeFusion()) passedTests++;
            CleanupTestData();

            if (Test3_DifferentItemFusion()) passedTests++;
            CleanupTestData();

            if (Test4_InsufficientMaterials()) passedTests++;
            CleanupTestData();

            if (Test5_EnhancementWarning()) passedTests++;
            CleanupTestData();

            if (Test6_GoldCost()) passedTests++;
            CleanupTestData();

            if (Test7_EquippedItemFusion()) passedTests++;
            CleanupTestData();

            if (Test8_EX_TR_Fusion()) passedTests++;
            CleanupTestData();

            Debug.Log("═══════════════════════════════════════════════════════");
            Debug.Log($"🎯 테스트 결과: {passedTests}/{totalTests} 통과");

            if (passedTests == totalTests)
            {
                Debug.Log("✅ Phase 6 테스트 100% 성공!");
            }
            else
            {
                Debug.LogError($"❌ Phase 6 테스트 실패: {totalTests - passedTests}개 실패");
            }
            Debug.Log("═══════════════════════════════════════════════════════");

            CleanupTestData();
        }

        // ========================================
        // Test 1: 기본 합성 (링D x3 → 링C)
        // ========================================

        private static bool Test1_BasicFusion()
        {
            Debug.Log("--- Test 1: 기본 합성 (링D x3 → 링C) ---");

            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                CreateTestSlot(0, "TestCharacter");

                // D등급 반지 3개 생성
                var materials = new List<ItemInstanceID>();
                for (int i = 0; i < 3; i++)
                {
                    var itemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                    player.GetCurrentSlotData().characterBagInstanceIds.Add(itemId);
                    materials.Add(itemId);
                }

                // 골드 추가
                player.GetCurrentSlotData().gold = 1000;
                player.SaveSlotData(player.GetCurrentSlotData());

                // 합성 실행
                bool success = FusionSystem.ExecuteFusion(materials, out ItemInstanceID resultId);

                if (!success)
                {
                    Debug.LogError("❌ 합성 실패");
                    return false;
                }

                // 검증
                var resultData = account.GetInstance(resultId);
                if (resultData == null)
                {
                    Debug.LogError("❌ 결과 아이템을 찾을 수 없음");
                    return false;
                }

                if (!resultData.templateName.Contains("_C_"))
                {
                    Debug.LogError($"❌ 등급 오류: {resultData.templateName} (C등급이어야 함)");
                    return false;
                }

                if (resultData.enhancementLevel != 0)
                {
                    Debug.LogError($"❌ 강화 수치 오류: +{resultData.enhancementLevel} (0이어야 함)");
                    return false;
                }

                // 재료 삭제 확인
                foreach (var materialId in materials)
                {
                    if (account.GetInstance(materialId) != null)
                    {
                        Debug.LogError($"❌ 재료가 삭제되지 않음: {materialId}");
                        return false;
                    }
                }

                Debug.Log($"✅ 합성 성공: D등급 x3 → C등급 (ID: {resultId})");
                Debug.Log("✅ Test 1 통과: 기본 합성 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 1 실패: {ex.Message}");
                return false;
            }
        }

        // ========================================
        // Test 2: 다른 등급 합성 시도 (실패)
        // ========================================

        private static bool Test2_DifferentGradeFusion()
        {
            Debug.Log("--- Test 2: 다른 등급 합성 시도 ---");

            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                CreateTestSlot(0, "TestCharacter");

                // D등급 2개 + C등급 1개
                var materials = new List<ItemInstanceID>();
                for (int i = 0; i < 2; i++)
                {
                    var itemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                    player.GetCurrentSlotData().characterBagInstanceIds.Add(itemId);
                    materials.Add(itemId);
                }
                var itemC = account.RegisterNewInstance("ITEM_RING_NONE_C_Equipment");
                player.GetCurrentSlotData().characterBagInstanceIds.Add(itemC);
                materials.Add(itemC);

                // 검증
                bool canFuse = FusionSystem.CanFuse(materials, out string reason);

                if (canFuse)
                {
                    Debug.LogError("❌ 다른 등급 합성이 허용됨 (차단되어야 함)");
                    return false;
                }

                Debug.Log($"✅ 합성 거부: {reason}");
                Debug.Log("✅ Test 2 통과: 다른 등급 합성 거부 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 2 실패: {ex.Message}");
                return false;
            }
        }

        // ========================================
        // Test 3: 다른 아이템 합성 시도 (실패)
        // ========================================

        private static bool Test3_DifferentItemFusion()
        {
            Debug.Log("--- Test 3: 다른 아이템 합성 시도 ---");

            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                CreateTestSlot(0, "TestCharacter");

                // 반지D 2개 + 목걸이D 1개
                var materials = new List<ItemInstanceID>();
                for (int i = 0; i < 2; i++)
                {
                    var itemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                    player.GetCurrentSlotData().characterBagInstanceIds.Add(itemId);
                    materials.Add(itemId);
                }
                var necklace = account.RegisterNewInstance("ITEM_NECKLACE_NONE_D_Equipment");
                player.GetCurrentSlotData().characterBagInstanceIds.Add(necklace);
                materials.Add(necklace);

                // 검증
                bool canFuse = FusionSystem.CanFuse(materials, out string reason);

                if (canFuse)
                {
                    Debug.LogError("❌ 다른 아이템 합성이 허용됨 (차단되어야 함)");
                    return false;
                }

                Debug.Log($"✅ 합성 거부: {reason}");
                Debug.Log("✅ Test 3 통과: 다른 아이템 합성 거부 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 3 실패: {ex.Message}");
                return false;
            }
        }

        // ========================================
        // Test 4: 재료 부족 (실패)
        // ========================================

        private static bool Test4_InsufficientMaterials()
        {
            Debug.Log("--- Test 4: 재료 부족 ---");

            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                CreateTestSlot(0, "TestCharacter");

                // D등급 2개만 (3개 필요)
                var materials = new List<ItemInstanceID>();
                for (int i = 0; i < 2; i++)
                {
                    var itemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                    player.GetCurrentSlotData().characterBagInstanceIds.Add(itemId);
                    materials.Add(itemId);
                }

                // 검증
                bool canFuse = FusionSystem.CanFuse(materials, out string reason);

                if (canFuse)
                {
                    Debug.LogError("❌ 재료 부족인데 합성이 허용됨");
                    return false;
                }

                Debug.Log($"✅ 합성 거부: {reason}");
                Debug.Log("✅ Test 4 통과: 재료 부족 거부 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 4 실패: {ex.Message}");
                return false;
            }
        }

        // ========================================
        // Test 5: 강화 경고 (강화된 아이템 합성)
        // ========================================

        private static bool Test5_EnhancementWarning()
        {
            Debug.Log("--- Test 5: 강화 경고 ---");

            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                CreateTestSlot(0, "TestCharacter");

                // D등급 반지 3개 생성 (1개는 +7 강화)
                var materials = new List<ItemInstanceID>();
                for (int i = 0; i < 3; i++)
                {
                    var itemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                    var itemData = account.GetInstance(itemId);
                    
                    if (i == 0) itemData.enhancementLevel = 7; // 첫 번째만 +7 (참조로 수정됨)
                    
                    player.GetCurrentSlotData().characterBagInstanceIds.Add(itemId);
                    materials.Add(itemId);
                }

                // 경고 필요 여부 확인
                bool needsWarning = FusionSystem.NeedsEnhancementWarning(materials, out int maxLevel);

                if (!needsWarning)
                {
                    Debug.LogError("❌ 강화 경고가 필요 없다고 판단함");
                    return false;
                }

                if (maxLevel != 7)
                {
                    Debug.LogError($"❌ 최대 강화 수치 오류: {maxLevel} (7이어야 함)");
                    return false;
                }

                Debug.Log($"✅ 강화 경고 필요: 최대 +{maxLevel}");
                Debug.Log("✅ Test 5 통과: 강화 경고 시스템 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 5 실패: {ex.Message}");
                return false;
            }
        }

        // ========================================
        // Test 6: 골드 비용 확인
        // ========================================

        private static bool Test6_GoldCost()
        {
            Debug.Log("--- Test 6: 골드 비용 확인 ---");

            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                CreateTestSlot(0, "TestCharacter");

                // 초기 골드: 50 (부족)
                player.GetCurrentSlotData().gold = 50;
                player.SaveSlotData(player.GetCurrentSlotData());

                // D등급 반지 3개 생성
                var materials = new List<ItemInstanceID>();
                for (int i = 0; i < 3; i++)
                {
                    var itemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                    player.GetCurrentSlotData().characterBagInstanceIds.Add(itemId);
                    materials.Add(itemId);
                }

                // 골드 부족 확인
                bool canFuse = FusionSystem.CanFuse(materials, out string reason);
                if (canFuse)
                {
                    Debug.LogError("❌ 골드 부족인데 합성이 허용됨");
                    return false;
                }

                Debug.Log($"✅ 골드 부족 거부: {reason}");

                // 골드 충분히 추가 후 재시도
                player.GetCurrentSlotData().gold = 1000;
                player.SaveSlotData(player.GetCurrentSlotData());

                int goldBefore = player.GetCurrentSlotData().gold;
                bool success = FusionSystem.ExecuteFusion(materials, out _);

                if (!success)
                {
                    Debug.LogError("❌ 골드가 충분한데 합성 실패");
                    return false;
                }

                int goldAfter = player.GetCurrentSlotData().gold;
                int goldSpent = goldBefore - goldAfter;

                if (goldSpent != 100)
                {
                    Debug.LogError($"❌ 골드 소모 오류: {goldSpent} (100이어야 함)");
                    return false;
                }

                Debug.Log($"✅ 골드 소모: -{goldSpent} (잔액: {goldAfter})");
                Debug.Log("✅ Test 6 통과: 골드 비용 시스템 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 6 실패: {ex.Message}");
                return false;
            }
        }

        // ========================================
        // Test 7: 장착 아이템 합성 거부
        // ========================================

        private static bool Test7_EquippedItemFusion()
        {
            Debug.Log("--- Test 7: 장착 아이템 합성 거부 ---");

            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                CreateTestSlot(0, "TestCharacter");

                // D등급 반지 3개 생성 (1개는 장착)
                var materials = new List<ItemInstanceID>();
                for (int i = 0; i < 3; i++)
                {
                    var itemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                    player.GetCurrentSlotData().characterBagInstanceIds.Add(itemId);
                    materials.Add(itemId);
                }

                // 첫 번째 반지 장착
                player.EquipV2(materials[0], EquipmentSlot.Ring1);

                // 검증
                bool canFuse = FusionSystem.CanFuse(materials, out string reason);

                if (canFuse)
                {
                    Debug.LogError("❌ 장착 아이템 합성이 허용됨 (차단되어야 함)");
                    return false;
                }

                Debug.Log($"✅ 합성 거부: {reason}");
                Debug.Log("✅ Test 7 통과: 장착 아이템 보호 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 7 실패: {ex.Message}");
                return false;
            }
        }

        // ========================================
        // Test 8: EX/TR 등급 합성 불가
        // ========================================

        private static bool Test8_EX_TR_Fusion()
        {
            Debug.Log("--- Test 8: EX/TR 등급 합성 불가 ---");

            try
            {
                // FusionRule 로드
                var rule = Resources.Load<FusionRule>("Data/FusionRule");
                if (rule == null)
                {
                    Debug.LogError("❌ FusionRule을 찾을 수 없음");
                    return false;
                }

                // EX 등급 합성 불가 확인
                bool canFuseEX = rule.CanFuseGrade(ItemGrade.EX);
                if (canFuseEX)
                {
                    Debug.LogError("❌ EX등급 합성이 허용됨 (의도적 제한)");
                    return false;
                }

                // TR 등급 합성 불가 확인
                bool canFuseTR = rule.CanFuseGrade(ItemGrade.TR);
                if (canFuseTR)
                {
                    Debug.LogError("❌ TR등급 합성이 허용됨 (최상위)");
                    return false;
                }

                Debug.Log("✅ EX/TR 등급 합성 불가 확인");
                Debug.Log("✅ Test 8 통과: 최상위 등급 보호 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 8 실패: {ex.Message}");
                return false;
            }
        }

        // ========================================
        // 헬퍼 메서드
        // ========================================

        private static void InitializeManagers()
        {
            // PlayerDataManager 초기화
            if (PlayerDataManager.Instance == null)
            {
                GameObject playerGo = new GameObject("PlayerDataManager");
                playerGo.AddComponent<PlayerDataManager>();
            }

            // AccountDataManager 초기화 (정적 메서드 호출)
            if (!AccountDataManager.IsInitialized())
            {
                AccountDataManager.Initialize(new JsonFileStorage());
            }

            // FusionManager 초기화
            if (FusionManager.Instance == null)
            {
                // Instance getter에서 자동 생성됨
                var _ = FusionManager.Instance;
            }
        }

        private static void CleanupTestData()
        {
            if (PlayerDataManager.Instance != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var slotData = PlayerDataManager.Instance.GetSlotData(i);
                    if (slotData != null && slotData.isSlotUsed)
                    {
                        PlayerDataManager.Instance.DeleteSlot(i);
                    }
                }
            }

            if (AccountDataManager.IsInitialized())
            {
                AccountDataManager.Instance.ClearAllData();
                AccountDataManager.Instance.Save();
            }

            Debug.Log("🧹 [Phase6Test] 테스트 데이터 정리 완료");
        }

        private static void CreateTestSlot(int slotIndex, string characterName)
        {
            PlayerDataManager.Instance.CreateNewSlot(slotIndex, PlayerType.Warrior, characterName);
            PlayerDataManager.Instance.SelectSlot(slotIndex);
        }
    }
}

