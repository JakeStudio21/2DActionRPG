using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Systems
{
    /// <summary>
    /// 일괄 분해 시스템
    /// </summary>
    public static class BatchDismantleSystem
    {
        /// <summary>
        /// 일괄 분해 필터 조건
        /// </summary>
        public class DismantleFilter
        {
            public ItemGrade? maxGrade = null;        // 이 등급 이하만 분해 (null = 제한 없음)
            public int? maxEnhancementLevel = null;        // 이 강화 레벨 이하만 분해
            public bool excludeBound = true;               // 귀속 아이템 제외
            public bool excludeEquipped = true;            // 장착 중인 아이템 제외
        }
        
        /// <summary>
        /// 일괄 분해 결과
        /// </summary>
        public class BatchDismantleResult
        {
            public int totalItems = 0;                              // 총 분해 아이템 수
            public int successCount = 0;                            // 성공한 아이템 수
            public int failedCount = 0;                             // 실패한 아이템 수
            public Dictionary<MaterialType, int> totalRewards = new Dictionary<MaterialType, int>(); // 총 획득 재료
            public List<ItemInstanceId> failedItems = new List<ItemInstanceId>(); // 실패한 아이템 ID
        }
        
        /// <summary>
        /// 필터 조건에 맞는 아이템 찾기
        /// </summary>
        public static List<ItemInstanceId> FindDismantleTargets(DismantleFilter filter)
        {
            var targets = new List<ItemInstanceId>();
            
            if (!AccountDataManager.IsInitialized())
            {
                Debug.LogError("[BatchDismantle] AccountDataManager가 초기화되지 않음");
                return targets;
            }
            
            var account = AccountDataManager.Instance;
            var accountData = account.GetAccountData();
            var allItems = new List<ItemInstanceId>();
            
            // 1. 계정 창고 아이템
            allItems.AddRange(accountData.sharedInventoryIds);
            
            // 2. 현재 캐릭터 가방 아이템 (선택된 슬롯)
            if (PlayerDataManager.Instance != null)
            {
                var playerData = PlayerDataManager.Instance;
                if (playerData.IsSlotSelected)
                {
                    var slotData = playerData.GetCurrentSlotData();
                    if (slotData != null)
                    {
                        allItems.AddRange(slotData.characterBagInstanceIds);
                    }
                }
            }
            
            // 3. 필터링
            foreach (var itemId in allItems)
            {
                if (!ApplyFilter(itemId, filter))
                    continue;
                
                targets.Add(itemId);
            }
            
            return targets;
        }
        
        /// <summary>
        /// 필터 조건 적용
        /// </summary>
        private static bool ApplyFilter(ItemInstanceId instanceId, DismantleFilter filter)
        {
            var account = AccountDataManager.Instance;
            var itemData = account.GetInstance(instanceId);
            
            if (itemData == null) return false;
            
            // 템플릿 로드
            var template = ItemTemplateResolver.Load(itemData.templateName);
            if (template == null) return false;
            
            // 1. 등급 필터
            if (filter.maxGrade.HasValue)
            {
                if (template.itemGrade > filter.maxGrade.Value)
                    return false;
            }
            
            // 2. 강화 레벨 필터
            if (filter.maxEnhancementLevel.HasValue)
            {
                if (itemData.enhancementLevel > filter.maxEnhancementLevel.Value)
                    return false;
            }
            
            // 3. 귀속 제외
            if (filter.excludeBound)
            {
                var bindInfo = account.GetBindInfo(instanceId);
                if (bindInfo.isBound)
                    return false;
            }
            
            // 4. 장착 중 제외
            if (filter.excludeEquipped)
            {
                if (!DismantleSystem.CanDismantle(instanceId, out _))
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 일괄 분해 실행
        /// </summary>
        public static BatchDismantleResult ExecuteBatchDismantle(List<ItemInstanceId> itemIds)
        {
            var result = new BatchDismantleResult
            {
                totalItems = itemIds.Count
            };
            
            foreach (var itemId in itemIds)
            {
                var rewards = DismantleSystem.DismantleItem(itemId);
                
                if (rewards != null && rewards.Count > 0)
                {
                    // 성공
                    result.successCount++;
                    
                    // 재료 합산
                    foreach (var reward in rewards)
                    {
                        if (result.totalRewards.ContainsKey(reward.Key))
                            result.totalRewards[reward.Key] += reward.Value;
                        else
                            result.totalRewards[reward.Key] = reward.Value;
                    }
                }
                else
                {
                    // 실패
                    result.failedCount++;
                    result.failedItems.Add(itemId);
                }
            }
            
            Debug.Log($"🔨 [BatchDismantle] 일괄 분해 완료: {result.successCount}/{result.totalItems} 성공");
            
            return result;
        }
        
        /// <summary>
        /// 일괄 분해 미리보기 (실제 분해하지 않음)
        /// </summary>
        public static Dictionary<MaterialType, int> PreviewBatchDismantle(List<ItemInstanceId> itemIds)
        {
            var totalRewards = new Dictionary<MaterialType, int>();
            
            foreach (var itemId in itemIds)
            {
                var rewards = DismantleSystem.CalculateDismantleReward(itemId);
                
                foreach (var reward in rewards)
                {
                    if (totalRewards.ContainsKey(reward.Key))
                        totalRewards[reward.Key] += reward.Value;
                    else
                        totalRewards[reward.Key] = reward.Value;
                }
            }
            
            return totalRewards;
        }
        
        /// <summary>
        /// 필터 기반 일괄 분해 (찾기 + 분해)
        /// </summary>
        public static BatchDismantleResult ExecuteWithFilter(DismantleFilter filter)
        {
            var targets = FindDismantleTargets(filter);
            
            if (targets.Count == 0)
            {
                Debug.LogWarning("[BatchDismantle] 조건에 맞는 아이템이 없음");
                return new BatchDismantleResult();
            }
            
            Debug.Log($"🔍 [BatchDismantle] 조건에 맞는 아이템 {targets.Count}개 발견");
            
            return ExecuteBatchDismantle(targets);
        }
    }
}

