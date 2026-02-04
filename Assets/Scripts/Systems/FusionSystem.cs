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
        public static bool CanFuse(List<ItemInstanceId> materialIds, out string reason)
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

            // 5. 모든 아이템이 동일한지 확인
            string targetTemplate = firstItem.templateName;
            foreach (var materialId in materialIds)
            {
                var itemData = account.GetInstance(materialId);
                if (itemData == null)
                {
                    reason = "재료 중 존재하지 않는 아이템이 있습니다.";
                    return false;
                }

                if (itemData.templateName != targetTemplate)
                {
                    reason = "모든 재료는 동일한 아이템이어야 합니다.";
                    return false;
                }
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

            // 7. 골드 확인
            int fusionCost = Rule.GetFusionCost(firstTemplate.itemGrade);
            if (PlayerDataManager.Instance != null)
            {
                var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
                if (slotData != null && slotData.gold < fusionCost)
                {
                    reason = $"골드가 부족합니다. (필요: {fusionCost}, 보유: {slotData.gold})";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 강화 경고 필요 여부 (재료 중 강화된 아이템이 있는지)
        /// </summary>
        public static bool NeedsEnhancementWarning(List<ItemInstanceId> materialIds, out int maxEnhancementLevel)
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
        public static bool ExecuteFusion(List<ItemInstanceId> materialIds, out ItemInstanceId resultId)
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

            // 첫 번째 아이템 정보 가져오기
            var firstItem = account.GetInstance(materialIds[0]);
            var firstTemplate = ItemTemplateResolver.Load(firstItem.templateName);

            // 결과 아이템 등급 결정
            ItemGrade nextGrade = Rule.GetNextGrade(firstTemplate.itemGrade);
            
            // 결과 아이템 템플릿 이름 생성 (등급만 변경)
            string resultTemplateName = firstItem.templateName.Replace($"_{firstTemplate.itemGrade}_", $"_{nextGrade}_");

            try
            {
                // 1. 골드 소모
                int fusionCost = Rule.GetFusionCost(firstTemplate.itemGrade);
                if (playerData != null)
                {
                    var slotData = playerData.GetCurrentSlotData();
                    if (slotData != null)
                    {
                        slotData.gold -= fusionCost;
                        playerData.SaveSlotData(slotData);
                        Debug.Log($"💰 [FusionSystem] 골드 소모: -{fusionCost} (잔액: {slotData.gold})");
                    }
                }

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

                // 4. 결과 아이템을 캐릭터 가방에 추가
                if (playerData != null && playerData.IsSlotSelected)
                {
                    var slotData = playerData.GetCurrentSlotData();
                    if (slotData != null)
                    {
                        slotData.characterBagInstanceIds.Add(resultId);
                        playerData.SaveSlotData(slotData);
                    }
                }

                // 5. 저장
                account.Save();
                
                Debug.Log($"✨ [FusionSystem] 합성 성공: {firstTemplate.itemGrade} x{materialIds.Count} → {nextGrade} (ID: {resultId})");
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
        private static bool RemoveItemFromInventory(ItemInstanceId instanceId)
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

