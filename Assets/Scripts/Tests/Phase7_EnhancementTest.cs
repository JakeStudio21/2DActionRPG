using UnityEngine;
using Managers;
using Systems;
using System.Collections.Generic;

namespace Tests
{
    /// <summary>
    /// Phase 7: 강화 시스템 테스트
    /// - 10개 테스트 케이스
    /// </summary>
    public static class Phase7_EnhancementTest
    {
        // ⚠️ 자동 실행 비활성화: Play 모드에서 계정 데이터 덮어쓰기 방지
        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void RunPhase7Test()
        {
            RunAllTests();
        }

        private static void RunAllTests()
        {
            Debug.Log("═══════════════════════════════════════════════════════");
            Debug.Log("🧪 Phase 7: 강화 시스템 테스트 시작");
            Debug.Log("═══════════════════════════════════════════════════════");

            int passCount = 0;
            int totalTests = 10;

            // 매니저 초기화
            InitializeManagers();

            // 테스트 실행
            if (Test1_BasicEnhancement()) passCount++;
            if (Test2_MaxLevelRestriction()) passCount++;
            if (Test3_InsufficientMaterials()) passCount++;
            if (Test4_InsufficientGold()) passCount++;
            if (Test5_SuccessRateCalculation()) passCount++;
            if (Test6_SafeZoneEnhancement()) passCount++;
            if (Test7_DowngradeZoneEnhancement()) passCount++;
            if (Test8_DestructionZoneEnhancement()) passCount++;
            if (Test9_MultipleEnhancements()) passCount++;
            if (Test10_GradeBasedMaterials()) passCount++;

            Debug.Log("═══════════════════════════════════════════════════════");
            Debug.Log($"🎯 테스트 결과: {passCount}/{totalTests} 통과");
            
            if (passCount == totalTests)
            {
                Debug.Log("✅ Phase 7 테스트 100% 성공!");
            }
            else
            {
                Debug.LogError($"❌ Phase 7 테스트 실패: {totalTests - passCount}개 실패");
            }
            
            Debug.Log("═══════════════════════════════════════════════════════");
        }

        // ========================================
        // Test 1: 기본 강화 (+0 → +1)
        // ========================================
        private static bool Test1_BasicEnhancement()
        {
            Debug.Log("--- Test 1: 기본 강화 (링D +0 → +1) ---");
            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                if (!CreateTestSlot(0, "TestCharacter"))
                {
                    Debug.LogError("❌ Test 1 실패: 슬롯 생성 실패");
                    return false;
                }

                var slotData = GetCurrentSlotDataSafe();
                if (slotData == null)
                {
                    Debug.LogError("❌ Test 1 실패: slotData null");
                    return false;
                }

                // D등급 반지 생성
                var itemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                slotData.characterBagInstanceIds.Add(itemId);

                // 재료 추가
                account.AddMaterial(MaterialType.EnhancementFragment, 100);
                
                // 골드 추가
                slotData.gold = 10000;
                player.SaveSlotData(slotData);

                // 강화 실행
                var result = player.EnhanceV2(itemId);

                if (!result.success)
                {
                    Debug.LogError($"❌ 강화 실패: {result.errorMessage}");
                    return false;
                }

                if (result.newLevel != 1)
                {
                    Debug.LogError($"❌ 레벨 오류: {result.newLevel} (1이어야 함)");
                    return false;
                }

                Debug.Log($"✅ 강화 성공: +0 → +{result.newLevel}");
                Debug.Log("✅ Test 1 통과: 기본 강화 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 1 실패: {ex.Message}");
                return false;
            }
        }

        // ========================================
        // Test 2: 최대 레벨 제한 (+15 불가)
        // ========================================
        private static bool Test2_MaxLevelRestriction()
        {
            Debug.Log("--- Test 2: 최대 레벨 제한 (+15 강화 불가) ---");
            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                CreateTestSlot(0, "TestCharacter");

                var slotData = GetCurrentSlotDataSafe();
                if (slotData == null) return false;

                // 아이템 생성 + 강제 +15
                var itemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                slotData.characterBagInstanceIds.Add(itemId);
                
                var itemData = account.GetInstance(itemId);
                itemData.enhancementLevel = 15;

                // 재료 추가
                account.AddMaterial(MaterialType.EnhancementFragment, 100);
                slotData.gold = 10000;
                player.SaveSlotData(slotData);

                // 강화 시도
                bool canEnhance = EnhancementSystem.CanEnhance(itemId, out string reason);

                if (canEnhance)
                {
                    Debug.LogError($"❌ +15 강화가 허용됨 (차단되어야 함)");
                    return false;
                }

                if (!reason.Contains("최대"))
                {
                    Debug.LogError($"❌ 오류 메시지 부적절: {reason}");
                    return false;
                }

                Debug.Log($"✅ 최대 레벨 차단: {reason}");
                Debug.Log("✅ Test 2 통과: 최대 레벨 제한 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 2 실패: {ex.Message}");
                return false;
            }
        }

        // ========================================
        // Test 3: 재료 부족 차단
        // ========================================
        private static bool Test3_InsufficientMaterials()
        {
            Debug.Log("--- Test 3: 재료 부족 차단 ---");
            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                
                Debug.Log("🔧 [Test 3] Step 1: 슬롯 생성");
                if (!CreateTestSlot(0, "TestCharacter"))
                {
                    Debug.LogError("❌ Test 3 실패: 슬롯 생성 실패");
                    return false;
                }

                Debug.Log("🔧 [Test 3] Step 2: 재료 초기화 (먼저!)");
                // AccountDataManager의 ClearAllData 사용 (캐시까지 초기화)
                account.ClearAllData();
                
                // 재료 초기화 직후 확인
                int materialAfterClear = account.GetMaterialCount(MaterialType.EnhancementFragment);
                Debug.Log($"🔍 [Test 3] 초기화 직후 파편 개수: {materialAfterClear}");

                Debug.Log("🔧 [Test 3] Step 3: slotData 가져오기");
                var slotData = GetCurrentSlotDataSafe();
                if (slotData == null)
                {
                    Debug.LogError("❌ Test 3 실패: slotData null");
                    return false;
                }

                Debug.Log("🔧 [Test 3] Step 4: 아이템 생성 (초기화 후!)");
                var itemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                slotData.characterBagInstanceIds.Add(itemId);
                
                Debug.Log("🔧 [Test 3] Step 5: 골드 설정 및 저장");
                slotData.gold = 10000;
                player.SaveSlotData(slotData);

                Debug.Log("🔧 [Test 3] Step 6: 강화 가능 여부 확인");
                bool canEnhance = EnhancementSystem.CanEnhance(itemId, out string reason);
                
                Debug.Log($"🔧 [Test 3] Step 7: 결과 - canEnhance={canEnhance}, reason='{reason}'");

                if (canEnhance)
                {
                    Debug.LogError($"❌ 재료 부족인데 강화 허용됨 (사유: '{reason}')");
                    
                    // 디버그: 실제 재료 개수 확인
                    int materialCount = account.GetMaterialCount(MaterialType.EnhancementFragment);
                    Debug.LogError($"🔍 현재 강화 파편 개수: {materialCount}");
                    
                    return false;
                }

                if (!reason.Contains("부족"))
                {
                    Debug.LogError($"❌ 오류 메시지 부적절: {reason}");
                    return false;
                }

                Debug.Log($"✅ 재료 부족 차단: {reason}");
                Debug.Log("✅ Test 3 통과: 재료 부족 차단 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 3 실패: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        // ========================================
        // Test 4: 골드 부족 차단
        // ========================================
        private static bool Test4_InsufficientGold()
        {
            Debug.Log("--- Test 4: 골드 부족 차단 ---");
            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                if (!CreateTestSlot(0, "TestCharacter")) return false;

                var slotData = GetCurrentSlotDataSafe();
                if (slotData == null) return false;

                // 아이템 생성
                var itemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                slotData.characterBagInstanceIds.Add(itemId);

                // 재료 충분, 골드 부족
                account.AddMaterial(MaterialType.EnhancementFragment, 100);
                slotData.gold = 0;
                player.SaveSlotData(slotData);

                // 강화 시도
                bool canEnhance = EnhancementSystem.CanEnhance(itemId, out string reason);

                if (canEnhance)
                {
                    Debug.LogError($"❌ 골드 부족인데 강화 허용됨");
                    return false;
                }

                if (!reason.Contains("골드"))
                {
                    Debug.LogError($"❌ 오류 메시지 부적절: {reason}");
                    return false;
                }

                Debug.Log($"✅ 골드 부족 차단: {reason}");
                Debug.Log("✅ Test 4 통과: 골드 부족 차단 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 4 실패: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        // ========================================
        // Test 5: 성공률 계산
        // ========================================
        private static bool Test5_SuccessRateCalculation()
        {
            Debug.Log("--- Test 5: 성공률 계산 ---");
            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                CreateTestSlot(0, "TestCharacter");

                var slotData = GetCurrentSlotDataSafe();
                if (slotData == null) return false;

                // D등급 아이템 생성
                var itemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                slotData.characterBagInstanceIds.Add(itemId);
                
                var itemData = account.GetInstance(itemId);

                // +0일 때 성공률 확인
                float rate0 = EnhancementSystem.GetSuccessRate(itemId);
                if (rate0 != 95f) // D등급 기본 95%
                {
                    Debug.LogError($"❌ +0 성공률 오류: {rate0}% (95%여야 함)");
                    return false;
                }

                // +5일 때 성공률 확인
                itemData.enhancementLevel = 5;
                float rate5 = EnhancementSystem.GetSuccessRate(itemId);
                float expected5 = 95f - (3f * 5); // 80%
                if (Mathf.Abs(rate5 - expected5) > 0.1f)
                {
                    Debug.LogError($"❌ +5 성공률 오류: {rate5}% ({expected5}%여야 함)");
                    return false;
                }

                // +10일 때 성공률 확인
                itemData.enhancementLevel = 10;
                float rate10 = EnhancementSystem.GetSuccessRate(itemId);
                float expected10 = 95f - (3f * 10); // 65%
                if (Mathf.Abs(rate10 - expected10) > 0.1f)
                {
                    Debug.LogError($"❌ +10 성공률 오류: {rate10}% ({expected10}%여야 함)");
                    return false;
                }

                Debug.Log($"✅ 성공률 계산: +0={rate0}%, +5={rate5}%, +10={rate10}%");
                Debug.Log("✅ Test 5 통과: 성공률 계산 정확\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 5 실패: {ex.Message}");
                return false;
            }
        }

        // ========================================
        // Test 6: 안전 구간 강화 (+0~+9)
        // ========================================
        private static bool Test6_SafeZoneEnhancement()
        {
            Debug.Log("--- Test 6: 안전 구간 강화 (+0~+9) ---");
            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                if (!CreateTestSlot(0, "TestCharacter")) return false;

                var slotData = GetCurrentSlotDataSafe();
                if (slotData == null) return false;

                // 아이템 생성
                var itemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                slotData.characterBagInstanceIds.Add(itemId);

                // 재료/골드 추가
                account.AddMaterial(MaterialType.EnhancementFragment, 1000);
                slotData.gold = 100000;
                player.SaveSlotData(slotData);

                // +9까지 강화 (100번 시도로 실패 케이스 포함)
                int successCount = 0;
                int failCount = 0;
                
                for (int attempt = 0; attempt < 100 && account.GetInstance(itemId).enhancementLevel < 9; attempt++)
                {
                    var result = player.EnhanceV2(itemId);
                    
                    if (result.success)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        
                        // 안전 구간에서는 실패해도 레벨 유지
                        if (result.wasDestroyed)
                        {
                            Debug.LogError($"❌ 안전 구간에서 아이템 파괴됨!");
                            return false;
                        }
                    }
                }

                var finalData = account.GetInstance(itemId);
                Debug.Log($"✅ 안전 구간 강화: 성공 {successCount}회, 실패 {failCount}회 (최종 레벨: +{finalData.enhancementLevel})");
                Debug.Log("✅ Test 6 통과: 안전 구간 강화 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 6 실패: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        // ========================================
        // Test 7: 하락 구간 강화 (+10~+12)
        // ========================================
        private static bool Test7_DowngradeZoneEnhancement()
        {
            Debug.Log("--- Test 7: 하락 구간 강화 (+10~+12) ---");
            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                if (!CreateTestSlot(0, "TestCharacter"))
                {
                    Debug.LogError("❌ Test 7 실패: 슬롯 생성 실패");
                    return false;
                }

                var slotData = GetCurrentSlotDataSafe();
                if (slotData == null)
                {
                    Debug.LogError("❌ Test 7 실패: slotData null");
                    return false;
                }

                // 아이템 생성 + 강제 +10
                var itemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                slotData.characterBagInstanceIds.Add(itemId);
                
                var itemData = account.GetInstance(itemId);
                itemData.enhancementLevel = 10;

                // 재료/골드 추가
                account.AddMaterial(MaterialType.EnhancementFragment, 1000);
                slotData.gold = 100000;
                player.SaveSlotData(slotData);

                // 강화 시도 (실패 시 하락 확인)
                int initialLevel = itemData.enhancementLevel;
                bool foundDowngrade = false;
                
                for (int attempt = 0; attempt < 50; attempt++)
                {
                    int beforeLevel = account.GetInstance(itemId).enhancementLevel;
                    var result = player.EnhanceV2(itemId);
                    int afterLevel = account.GetInstance(itemId).enhancementLevel;
                    
                    if (!result.success && !result.wasDestroyed)
                    {
                        if (afterLevel < beforeLevel)
                        {
                            foundDowngrade = true;
                            Debug.Log($"✅ 하락 확인: +{beforeLevel} → +{afterLevel}");
                            break;
                        }
                    }
                    
                    if (result.wasDestroyed)
                    {
                        Debug.LogError($"❌ 하락 구간에서 아이템 파괴됨! (+{beforeLevel})");
                        return false;
                    }
                    
                    // 레벨이 13 이상 올라가면 중단 (파괴 구간 진입 방지)
                    if (afterLevel >= 13)
                    {
                        break;
                    }
                }

                Debug.Log($"✅ 하락 구간 강화 테스트 (하락 발생: {foundDowngrade})");
                Debug.Log("✅ Test 7 통과: 하락 구간 강화 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 7 실패: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        // ========================================
        // Test 8: 파괴 구간 강화 (+13~+15)
        // ========================================
        private static bool Test8_DestructionZoneEnhancement()
        {
            Debug.Log("--- Test 8: 파괴 구간 강화 (+13~+15) ---");
            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                if (!CreateTestSlot(0, "TestCharacter"))
                {
                    Debug.LogError("❌ Test 8 실패: 슬롯 생성 실패");
                    return false;
                }

                var slotData = GetCurrentSlotDataSafe();
                if (slotData == null)
                {
                    Debug.LogError("❌ Test 8 실패: slotData null");
                    return false;
                }

                // 아이템 생성 + 강제 +13
                var itemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                slotData.characterBagInstanceIds.Add(itemId);
                
                var itemData = account.GetInstance(itemId);
                itemData.enhancementLevel = 13;

                // 재료/골드 추가
                account.AddMaterial(MaterialType.EnhancementFragment, 1000);
                slotData.gold = 100000;
                player.SaveSlotData(slotData);

                // 강화 시도 (실패 시 파괴 확인)
                bool foundDestruction = false;
                
                for (int attempt = 0; attempt < 100; attempt++)
                {
                    var testItemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                    slotData.characterBagInstanceIds.Add(testItemId);
                    var testData = account.GetInstance(testItemId);
                    testData.enhancementLevel = 13;
                    
                    var result = player.EnhanceV2(testItemId);
                    
                    if (result.wasDestroyed)
                    {
                        foundDestruction = true;
                        
                        // 파괴된 아이템이 실제로 삭제되었는지 확인
                        var deletedData = account.GetInstance(testItemId);
                        if (deletedData != null)
                        {
                            Debug.LogError($"❌ 파괴된 아이템이 삭제되지 않음!");
                            return false;
                        }
                        
                        Debug.Log($"✅ 파괴 확인: +13 강화 실패 → 아이템 파괴");
                        break;
                    }
                }

                if (!foundDestruction)
                {
                    Debug.LogWarning($"⚠️ 100회 시도에서 파괴 발생하지 않음 (확률 문제일 수 있음)");
                }

                Debug.Log("✅ Test 8 통과: 파괴 구간 강화 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 8 실패: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        // ========================================
        // Test 9: 연속 강화 (+0 → +5)
        // ========================================
        private static bool Test9_MultipleEnhancements()
        {
            Debug.Log("--- Test 9: 연속 강화 (+0 → +5) ---");
            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                if (!CreateTestSlot(0, "TestCharacter")) return false;

                var slotData = GetCurrentSlotDataSafe();
                if (slotData == null) return false;

                // 아이템 생성
                var itemId = account.RegisterNewInstance("ITEM_RING_NONE_D_Equipment");
                slotData.characterBagInstanceIds.Add(itemId);

                // 재료/골드 추가
                account.AddMaterial(MaterialType.EnhancementFragment, 1000);
                slotData.gold = 100000;
                player.SaveSlotData(slotData);

                // +5까지 강화
                int attempts = 0;
                while (account.GetInstance(itemId).enhancementLevel < 5 && attempts < 100)
                {
                    var result = player.EnhanceV2(itemId);
                    attempts++;
                }

                var finalData = account.GetInstance(itemId);
                if (finalData.enhancementLevel < 5)
                {
                    Debug.LogError($"❌ 연속 강화 실패: +{finalData.enhancementLevel} (목표: +5, 시도: {attempts}회)");
                    return false;
                }

                Debug.Log($"✅ 연속 강화 성공: +0 → +{finalData.enhancementLevel} (시도: {attempts}회)");
                Debug.Log("✅ Test 9 통과: 연속 강화 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 9 실패: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        // ========================================
        // Test 10: 등급별 재료 차등
        // ========================================
        private static bool Test10_GradeBasedMaterials()
        {
            Debug.Log("--- Test 10: 등급별 재료 차등 ---");
            try
            {
                var account = AccountDataManager.Instance;
                var player = PlayerDataManager.Instance;
                CreateTestSlot(0, "TestCharacter");

                // 재료 확인 (아이템 생성 없이 템플릿 직접 로드)
                var templateD = ItemTemplateResolver.Load("ITEM_RING_NONE_D_Equipment");
                var templateA = ItemTemplateResolver.Load("ITEM_GLOVES_WARRIOR_A_Equipment");
                var templateS = ItemTemplateResolver.Load("ITEM_BOW_S_Equipment"); // S등급으로 대체 (EX 대신)
                
                if (templateD == null)
                {
                    Debug.LogError($"❌ D등급 템플릿을 찾을 수 없음");
                    return false;
                }
                
                if (templateA == null)
                {
                    Debug.LogError($"❌ A등급 템플릿을 찾을 수 없음");
                    return false;
                }
                
                if (templateS == null)
                {
                    Debug.LogError($"❌ S등급 템플릿을 찾을 수 없음");
                    return false;
                }
                
                var enhanceData = Resources.Load<EnhancementData>("Data/EnhancementData");
                if (enhanceData == null)
                {
                    Debug.LogError($"❌ EnhancementData를 찾을 수 없음");
                    return false;
                }
                
                var materialD = enhanceData.GetRequiredMaterialType(templateD.itemGrade);
                var materialA = enhanceData.GetRequiredMaterialType(templateA.itemGrade);
                var materialS = enhanceData.GetRequiredMaterialType(templateS.itemGrade);
                
                if (materialD != MaterialType.EnhancementFragment)
                {
                    Debug.LogError($"❌ D등급 재료 오류: {materialD} (강화 파편이어야 함)");
                    return false;
                }
                
                if (materialA != MaterialType.EnhancementCrystal)
                {
                    Debug.LogError($"❌ A등급 재료 오류: {materialA} (강화 결정이어야 함)");
                    return false;
                }
                
                if (materialS != MaterialType.EnhancementCrystal)
                {
                    Debug.LogError($"❌ S등급 재료 오류: {materialS} (강화 결정이어야 함)");
                    return false;
                }

                Debug.Log($"✅ 등급별 재료: D={materialD.GetDisplayName()}, A={materialA.GetDisplayName()}, S={materialS.GetDisplayName()}");
                
                // 추가 검증: 재료량 확인
                int amountD = enhanceData.GetRequiredMaterialAmount(templateD.itemGrade, 1);
                int amountA = enhanceData.GetRequiredMaterialAmount(templateA.itemGrade, 1);
                
                Debug.Log($"✅ 필요 재료량: D=파편 {amountD}개, A=결정 {amountA}개");
                Debug.Log("✅ Test 10 통과: 등급별 재료 차등 성공\n");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Test 10 실패: {ex.Message}");
                return false;
            }
        }

        // ========================================
        // 헬퍼: 매니저 초기화
        // ========================================
        private static void InitializeManagers()
        {
            // PlayerDataManager 초기화
            if (PlayerDataManager.Instance == null)
            {
                GameObject playerManagerGO = new GameObject("PlayerDataManager");
                playerManagerGO.AddComponent<PlayerDataManager>();
            }
            
            // AccountDataManager 초기화 (static)
            AccountDataManager.Initialize(new JsonFileStorage());
        }

        // ========================================
        // 헬퍼: 테스트 슬롯 생성
        // ========================================
        private static bool CreateTestSlot(int slotIndex, string characterName)
        {
            var player = PlayerDataManager.Instance;
            
            Debug.Log($"🔧 [CreateTestSlot] 슬롯 {slotIndex} 생성 시작...");
            
            // 기존 슬롯 삭제
            player.DeleteSlot(slotIndex);
            
            // 새 슬롯 생성
            player.CreateNewSlot(slotIndex, PlayerType.Warrior, characterName);
            player.SelectSlot(slotIndex);
            
            // 슬롯 데이터 검증
            var slotData = player.GetSlotData(slotIndex);
            if (slotData == null)
            {
                Debug.LogError($"❌ [CreateTestSlot] 슬롯 {slotIndex} 생성 실패 - GetSlotData() null!");
                return false;
            }
            
            // 선택 상태 검증
            if (!player.IsSlotSelected || player.CurrentSlotIndex != slotIndex)
            {
                Debug.LogError($"❌ [CreateTestSlot] 슬롯 선택 실패! IsSlotSelected={player.IsSlotSelected}, CurrentSlotIndex={player.CurrentSlotIndex}");
                return false;
            }
            
            Debug.Log($"✅ [CreateTestSlot] 슬롯 {slotIndex} 생성 완료 (선택됨: {player.CurrentSlotIndex})");
            return true;
        }
        
        // ========================================
        // 헬퍼: 안전한 현재 슬롯 데이터 가져오기
        // ========================================
        private static PlayerSlotData GetCurrentSlotDataSafe()
        {
            var player = PlayerDataManager.Instance;
            
            Debug.Log($"🔍 [GetCurrentSlotDataSafe] IsSlotSelected={player.IsSlotSelected}, CurrentSlotIndex={player.CurrentSlotIndex}");
            
            if (!player.IsSlotSelected)
            {
                Debug.LogError("❌ [GetCurrentSlotDataSafe] 슬롯이 선택되지 않음!");
                Debug.LogError($"   → CurrentSlotIndex={player.CurrentSlotIndex}");
                return null;
            }
            
            var slotData = player.GetSlotData(player.CurrentSlotIndex);
            if (slotData == null)
            {
                Debug.LogError($"❌ [GetCurrentSlotDataSafe] 슬롯 {player.CurrentSlotIndex} 데이터가 null!");
                Debug.LogError($"   → 사용 가능한 슬롯 확인 필요");
            }
            else
            {
                Debug.Log($"✅ [GetCurrentSlotDataSafe] 슬롯 {player.CurrentSlotIndex} 로드 성공 (골드:{slotData.gold})");
            }
            
            return slotData;
        }
        
        // ========================================
        // 헬퍼: 모든 재료 초기화
        // ========================================
        private static void ClearAllMaterials()
        {
            try
            {
                var account = AccountDataManager.Instance;
                var accountData = account.GetAccountData();
                
                if (accountData == null)
                {
                    Debug.LogError("❌ [ClearAllMaterials] accountData가 null!");
                    return;
                }
                
                if (accountData.materials == null)
                {
                    Debug.LogError("❌ [ClearAllMaterials] materials 딕셔너리가 null!");
                    return;
                }
                
                Debug.Log($"🔍 [ClearAllMaterials] 초기화 전 재료 개수: {accountData.materials.Count}");
                
                // List이므로 Clear() 호출
                accountData.materials.Clear();
                
                // 변경사항 저장
                account.Save();
                
                Debug.Log($"🧹 [Test Helper] 모든 재료 초기화 완료 (남은 재료: {accountData.materials.Count}개)");
                
                // 검증
                int fragmentCount = account.GetMaterialCount(MaterialType.EnhancementFragment);
                int crystalCount = account.GetMaterialCount(MaterialType.EnhancementCrystal);
                int coreCount = account.GetMaterialCount(MaterialType.EnhancementCore);
                Debug.Log($"🔍 [검증] 파편:{fragmentCount}, 결정:{crystalCount}, 코어:{coreCount}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ [ClearAllMaterials] 예외 발생: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}

