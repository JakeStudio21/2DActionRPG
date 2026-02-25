using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Managers;

namespace Systems
{
    /// <summary>
    /// 합성 시스템 (Fusion System)
    /// - 동일 아이템 여러 개 → 상위 등급 아이템 1개
    /// - 강화 수치 초기화
    /// - 골드 소모
    /// </summary>
    public static class FusionSystem
    {
        private static FusionRule _rule;
        private static FusionRule Rule
        {
            get
            {
                if (_rule == null)
                {
                    _rule = Resources.Load<FusionRule>("Data/FusionRule");
                    if (_rule == null)
                    {
                        Debug.LogError("[FusionSystem] FusionRule을 찾을 수 없습니다. Resources/Data 폴더에 있는지 확인하세요.");
                    }
                }
                return _rule;
            }
        }

        /// <summary>
        /// 합성 가능 여부 검증
        /// </summary>
        public static bool CanFuse(List<ItemInstanceID> materialIds, out string reason)
        {
            reason = "";

            if (!AccountDataManager.IsInitialized())
            {
                reason = "AccountDataManager가 초기화되지 않았습니다.";
                return false;
            }

            var account = AccountDataManager.Instance;

            // 1. 재료 개수 확인
            if (materialIds == null || materialIds.Count == 0)
            {
                reason = "재료가 없습니다.";
                return false;
            }

            // 2. 첫 번째 아이템 정보 가져오기
            var firstItem = account.GetInstance(materialIds[0]);
            if (firstItem == null)
            {
                reason = "재료 아이템을 찾을 수 없습니다.";
                return false;
            }

            var firstTemplate = ItemTemplateResolver.Load(firstItem.templateName);
            if (firstTemplate == null)
            {
                reason = $"템플릿을 찾을 수 없습니다: {firstItem.templateName}";
                return false;
            }

            // 3. 합성 가능 등급 확인
            if (Rule == null)
            {
                reason = "FusionRule이 로드되지 않았습니다.";
                return false;
            }

            if (!Rule.CanFuseGrade(firstTemplate.itemGrade))
            {
                reason = $"{firstTemplate.itemGrade}등급은 합성할 수 없습니다.";
                return false;
            }

            // 4. 필요 개수 확인
            int requiredCount = Rule.GetRequiredCount(firstTemplate.itemGrade);
            if (materialIds.Count != requiredCount)
            {
                reason = $"{firstTemplate.itemGrade}등급 합성에는 {requiredCount}개가 필요합니다. (현재: {materialIds.Count}개)";
                return false;
            }

            // 5. 같은 등급 + 같은 EquipmentType 확인 (클래스 무관)
            ItemGrade targetGrade = firstTemplate.itemGrade;
            EquipmentType targetType = firstTemplate.equipmentType;

            foreach (var materialId in materialIds)
            {
                var itemData = account.GetInstance(materialId);
                if (itemData == null)
                {
                    reason = "재료 중 존재하지 않는 아이템이 있습니다.";
                    return false;
                }

                var template = ItemTemplateResolver.Load(itemData.templateName);
                if (template == null)
                {
                    reason = $"재료 템플릿을 찾을 수 없습니다: {itemData.templateName}";
                    return false;
                }

                // 등급 체크
                if (template.itemGrade != targetGrade)
                {
                    reason = "모든 재료는 같은 등급이어야 합니다.";
                    return false;
                }

                // 장비 타입 체크 (Weapon/Armor/Belt/Boots/Gloves/Ring/Helmet/Necklace)
                if (template.equipmentType != targetType)
                {
                    reason = "모든 재료는 같은 장비 종류여야 합니다.";
                    return false;
                }

                // 클래스는 체크하지 않음 (Warrior/Assasin/Wizard 혼합 가능)
            }

            // 6. 장착 중인 아이템 확인
            if (PlayerDataManager.Instance != null)
            {
                var playerData = PlayerDataManager.Instance;
                if (playerData.IsSlotSelected)
                {
                    var slotData = playerData.GetSlotData(playerData.CurrentSlotIndex);
                    if (slotData != null)
                    {
                        foreach (var materialId in materialIds)
                        {
                            // equippedRecords에서 확인
                            if (slotData.equippedRecords.Exists(r => r.instanceId == materialId))
                            {
                                reason = "장착 중인 아이템은 합성할 수 없습니다.";
                                return false;
                            }
                        }
                    }
                }
            }

            // 7. ⭐ 골드 확인 (PlayerDataManager.CurrentGold 사용)
            int fusionCost = Rule.GetFusionCost(firstTemplate.itemGrade);
            int currentGold = PlayerDataManager.Instance.CurrentGold;
            if (currentGold < fusionCost)
            {
                reason = $"골드가 부족합니다. (필요: {fusionCost}, 보유: {currentGold})";
                return false;
            }

            return true;
        }

        /// <summary>
        /// 강화 경고 필요 여부 (재료 중 강화된 아이템이 있는지)
        /// </summary>
        public static bool NeedsEnhancementWarning(List<ItemInstanceID> materialIds, out int maxEnhancementLevel)
        {
            maxEnhancementLevel = 0;

            if (!AccountDataManager.IsInitialized()) return false;
            var account = AccountDataManager.Instance;

            foreach (var materialId in materialIds)
            {
                var itemData = account.GetInstance(materialId);
                if (itemData != null && itemData.enhancementLevel > maxEnhancementLevel)
                {
                    maxEnhancementLevel = itemData.enhancementLevel;
                }
            }

            return maxEnhancementLevel > 0;
        }

        /// <summary>
        /// 합성 실행
        /// </summary>
        public static bool ExecuteFusion(List<ItemInstanceID> materialIds, out ItemInstanceID resultId)
        {
            resultId = default;

            // 검증
            if (!CanFuse(materialIds, out string reason))
            {
                Debug.LogError($"[FusionSystem] 합성 불가: {reason}");
                return false;
            }

            var account = AccountDataManager.Instance;
            var playerData = PlayerDataManager.Instance;

            // 📌 결과물 결정: materialIds[0]를 기준으로 상위 등급 생성 (명확성 우선)
            // - 개별 선택 모드: UI가 "첫 선택 = 결과물"을 보장
            // - 일괄 선택 모드: UI가 자동으로 정렬하여 전달
            var baseItem = account.GetInstance(materialIds[0]);
            var baseTemplate = ItemTemplateResolver.Load(baseItem.templateName);

            // 결과 아이템 등급 결정
            ItemGrade nextGrade = Rule.GetNextGrade(baseTemplate.itemGrade);
            
            // ⭐ 결과 아이템 템플릿 이름 생성 (등급만 변경, 클래스 유지)
            // 예: "ITEM_ARMOR_WIZARD_B" → "ITEM_ARMOR_WIZARD_A"
            string templateName = baseItem.templateName;
            
            // ⭐ 1단계: "_Equipment" 접미사 제거 (상점 버그로 인한 잘못된 데이터 대응)
            // "ITEM_ARMOR_WIZARD_B_Equipment" → "ITEM_ARMOR_WIZARD_B"
            if (templateName.EndsWith("_Equipment"))
            {
                templateName = templateName.Substring(0, templateName.Length - "_Equipment".Length);
                Debug.Log($"🔧 [FusionSystem] _Equipment 접미사 제거: {baseItem.templateName} → {templateName}");
            }
            
            // ⭐ 2단계: 마지막 언더스코어 이후 등급 문자열 교체
            string resultTemplateName = templateName;
            int lastUnderscoreIndex = templateName.LastIndexOf('_');
            
            if (lastUnderscoreIndex >= 0)
            {
                string prefix = templateName.Substring(0, lastUnderscoreIndex + 1); // "ITEM_ARMOR_WIZARD_"
                resultTemplateName = prefix + nextGrade.ToString(); // "ITEM_ARMOR_WIZARD_A"
            }
            else
            {
                // 언더스코어가 없으면 그냥 뒤에 추가
                resultTemplateName = templateName + "_" + nextGrade.ToString();
            }
            
            Debug.Log($"🔄 [FusionSystem] 합성 결과 템플릿: {baseItem.templateName} ({baseTemplate.itemGrade}) → {resultTemplateName} ({nextGrade})");

            try
            {
                // 1. ⭐ 골드 소모 (PlayerDataManager.SpendGold 사용 필수! UI 이벤트 발생)
                int fusionCost = Rule.GetFusionCost(baseTemplate.itemGrade);
                if (!PlayerDataManager.Instance.SpendGold(fusionCost))
                {
                    Debug.LogError($"❌ [FusionSystem] 골드 소모 실패: {fusionCost} (잔액 부족)");
                    return false;
                }
                Debug.Log($"💰 [FusionSystem] 골드 소모: -{fusionCost}");

                // 2. 재료 아이템 삭제
                foreach (var materialId in materialIds)
                {
                    // 인벤토리에서 제거
                    RemoveItemFromInventory(materialId);
                    
                    // 아이템 인스턴스 삭제 (RemoveInstance가 자동으로 귀속도 제거함)
                    account.RemoveInstance(materialId);
                }
                Debug.Log($"🗑️ [FusionSystem] 재료 {materialIds.Count}개 소모");

                // 3. 결과 아이템 생성 (강화 +0)
                resultId = account.RegisterNewInstance(resultTemplateName);
                var resultData = account.GetInstance(resultId);
                resultData.enhancementLevel = 0; // 강화 초기화 (이미 참조로 수정됨)

                // ⭐ Stage 3: 동적 스탯 생성 및 적용
                EquipmentData resultEquipData = ItemTemplateResolver.Load(resultTemplateName);
                if (resultEquipData != null)
                {
                    // 중요: 합성 결과 등급(nextGrade) 사용, EquipmentData의 원본 등급이 아님!
                    EquipmentInstance dynamicResult = DynamicEquipmentGenerator.Generate(resultEquipData, nextGrade);
                    
                    if (dynamicResult != null)
                    {
                        EquipmentInstanceConverter.ApplyDynamicStats(resultData, dynamicResult);
                        Debug.Log($"🎲 [FusionSystem] 동적 스탯 생성 완료: 주옵션={resultData.finalMainStatValue}, 부옵션={resultData.randomSubStats.Count}개");
                    }
                }

                // 4. ⭐ 결과 아이템을 계정 공유 창고에 추가 (로비 공방/보관창고에서 표시)
                // ✅ TryAddToShared() 사용 (이벤트 발행 + 크기 체크)
                if (!account.TryAddToShared(resultId))
                {
                    // 창고 가득 참 (재료 삭제 후에도) → 결과 아이템 제거
                    account.RemoveInstance(resultId);
                    Debug.LogError($"❌ [FusionSystem] 창고 가득 참! 합성 결과 아이템 추가 실패: {resultTemplateName}");
                    resultId = default;
                    return false;
                }
                Debug.Log($"📦 [FusionSystem] 결과 아이템 추가: {resultTemplateName} (ID: {resultId})");

                // 5. 저장
                account.Save();
                
                // 6. ⭐ UI 이벤트 발생 (상점 UI 갱신용)
                if (PlayerDataManager.Instance != null)
                {
                    PlayerDataManager.Instance.NotifyInventoryChanged();
                    Debug.Log("🔔 [FusionSystem] OnInventoryChanged 이벤트 발생 (상점 UI 갱신)");
                }
                
                Debug.Log($"✨ [FusionSystem] 합성 성공: {baseTemplate.itemGrade} {baseTemplate.equipmentType} x{materialIds.Count} → {nextGrade} {baseTemplate.equipmentType} (결과 ID: {resultId})");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FusionSystem] 합성 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 인벤토리에서 아이템 제거 (내부 헬퍼)
        /// </summary>
        private static bool RemoveItemFromInventory(ItemInstanceID instanceId)
        {
            var account = AccountDataManager.Instance;
            var accountData = account.GetAccountData();

            // 1. 계정 창고
            if (accountData.sharedInventoryIds.Contains(instanceId))
            {
                accountData.sharedInventoryIds.Remove(instanceId);
                return true;
            }

            // 2. 우편함
            if (accountData.mailboxIds.Contains(instanceId))
            {
                accountData.mailboxIds.Remove(instanceId);
                return true;
            }

            // 3. 캐릭터 가방
            if (PlayerDataManager.Instance != null)
            {
                var playerData = PlayerDataManager.Instance;
                for (int i = 0; i < 3; i++)
                {
                    var slotData = playerData.GetSlotData(i);
                    if (slotData == null || !slotData.isSlotUsed) continue;

                    if (slotData.characterBagInstanceIds.Contains(instanceId))
                    {
                        slotData.characterBagInstanceIds.Remove(instanceId);
                        playerData.SaveSlotData(slotData);
                        return true;
                    }
                }
            }

            Debug.LogWarning($"[FusionSystem] 아이템을 인벤토리에서 찾을 수 없음: {instanceId}");
            return false;
        }
    }
}

