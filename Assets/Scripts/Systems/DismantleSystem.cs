using UnityEngine;
using System.Collections.Generic;

namespace Systems
{
    /// <summary>
    /// 아이템 분해 시스템
    /// </summary>
    public static class DismantleSystem
    {
        private static DismantleRewardData _rewardData;
        
        /// <summary>
        /// 분해 보상 데이터 설정 (의존성 주입)
        /// </summary>
        public static void SetRewardData(DismantleRewardData data)
        {
            _rewardData = data;
        }
        
        /// <summary>
        /// 기본 보상 데이터 로드
        /// </summary>
        private static DismantleRewardData GetRewardData()
        {
            if (_rewardData == null)
            {
                _rewardData = Resources.Load<DismantleRewardData>("Data/DismantleRewardData");
                if (_rewardData == null)
                {
                    Debug.LogWarning("[DismantleSystem] DismantleRewardData를 찾을 수 없음. 기본값 사용.");
                }
            }
            return _rewardData;
        }
        
        /// <summary>
        /// 아이템 분해 가능 여부 확인
        /// </summary>
        public static bool CanDismantle(ItemInstanceId instanceId, out string reason)
        {
            reason = "";
            
            if (!AccountDataManager.IsInitialized())
            {
                reason = "AccountDataManager가 초기화되지 않음";
                return false;
            }
            
            var account = AccountDataManager.Instance;
            var itemData = account.GetInstance(instanceId);
            
            if (itemData == null)
            {
                reason = "아이템을 찾을 수 없음";
                return false;
            }
            
            // 장착 중인 아이템은 분해 불가
            if (IsEquipped(instanceId))
            {
                reason = "장착 중인 아이템은 분해할 수 없습니다";
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 아이템이 장착 중인지 확인
        /// </summary>
        private static bool IsEquipped(ItemInstanceId instanceId)
        {
            if (PlayerDataManager.Instance == null) return false;
            
            var playerData = PlayerDataManager.Instance;
            
            // 모든 슬롯 확인
            for (int i = 0; i < 3; i++)
            {
                var slotData = playerData.GetSlotData(i);
                if (slotData == null || !slotData.isSlotUsed) continue;
                
                // 장착된 아이템 확인
                foreach (var record in slotData.equippedRecords)
                {
                    if (record.instanceId.Equals(instanceId))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 분해 보상 계산
        /// </summary>
        public static Dictionary<MaterialType, int> CalculateDismantleReward(ItemInstanceId instanceId)
        {
            var account = AccountDataManager.Instance;
            var itemData = account.GetInstance(instanceId);
            
            if (itemData == null)
            {
                Debug.LogError($"[DismantleSystem] 아이템을 찾을 수 없음: {instanceId}");
                return new Dictionary<MaterialType, int>();
            }
            
            // 템플릿 로드
            var template = ItemTemplateResolver.Load(itemData.templateName);
            if (template == null)
            {
                Debug.LogError($"[DismantleSystem] 템플릿을 찾을 수 없음: {itemData.templateName}");
                return new Dictionary<MaterialType, int>();
            }
            
            // 보상 데이터 가져오기
            var rewardData = GetRewardData();
            if (rewardData == null)
            {
                Debug.LogError("[DismantleSystem] DismantleRewardData가 없음");
                return new Dictionary<MaterialType, int>();
            }
            
            // 보상 계산 (장비 타입 + 등급 기반)
            return rewardData.CalculateRewards(template.equipmentType, template.itemGrade, itemData.enhancementLevel);
        }
        
        /// <summary>
        /// 아이템 분해 실행
        /// </summary>
        /// <returns>획득한 재료 목록</returns>
        public static Dictionary<MaterialType, int> DismantleItem(ItemInstanceId instanceId)
        {
            var emptyReward = new Dictionary<MaterialType, int>();
            
            // 1. 분해 가능 여부 확인
            if (!CanDismantle(instanceId, out string reason))
            {
                Debug.LogError($"[DismantleSystem] 분해 불가: {reason}");
                return emptyReward;
            }
            
            var account = AccountDataManager.Instance;
            var itemData = account.GetInstance(instanceId);
            
            if (itemData == null)
            {
                Debug.LogError($"[DismantleSystem] 아이템을 찾을 수 없음: {instanceId}");
                return emptyReward;
            }
            
            // 2. 보상 계산
            var rewards = CalculateDismantleReward(instanceId);
            
            if (rewards.Count == 0)
            {
                Debug.LogError("[DismantleSystem] 보상 계산 실패");
                return emptyReward;
            }
            
            // 3. 원자적 실행
            try
            {
                // 3-1. 아이템 위치 확인 및 제거
                bool removed = RemoveItemFromInventory(instanceId);
                
                if (!removed)
                {
                    Debug.LogError($"[DismantleSystem] 아이템 제거 실패: {instanceId}");
                    return emptyReward;
                }
                
                // 3-2. 아이템 인스턴스 삭제
                account.RemoveInstance(instanceId);
                
                // 3-3. 재료 추가
                foreach (var reward in rewards)
                {
                    account.AddMaterial(reward.Key, reward.Value);
                }
                
                // 3-4. 귀속 정보 삭제 (있다면)
                var bindInfo = account.GetBindInfo(instanceId);
                if (bindInfo.isBound)
                {
                    account.RemoveBind(instanceId);
                }
                
                // 3-5. 저장
                account.Save();
                
                Debug.Log($"✅ [DismantleSystem] 분해 완료: {itemData.templateName} → {string.Join(", ", rewards)}");
                
                return rewards;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DismantleSystem] 분해 실패: {ex.Message}");
                // 롤백 필요 시 여기서 처리
                return emptyReward;
            }
        }
        
        /// <summary>
        /// 인벤토리에서 아이템 제거 (가방 또는 계정 창고)
        /// </summary>
        private static bool RemoveItemFromInventory(ItemInstanceId instanceId)
        {
            var account = AccountDataManager.Instance;
            var accountData = account.GetAccountData();
            
            // 1. 계정 창고에서 찾기
            if (accountData.sharedInventoryIds.Contains(instanceId))
            {
                accountData.sharedInventoryIds.Remove(instanceId);
                Debug.Log($"[DismantleSystem] 계정 창고에서 제거: {instanceId}");
                return true;
            }
            
            // 2. 우편함에서 찾기
            if (accountData.mailboxIds.Contains(instanceId))
            {
                accountData.mailboxIds.Remove(instanceId);
                Debug.Log($"[DismantleSystem] 우편함에서 제거: {instanceId}");
                return true;
            }
            
            // 3. 캐릭터 가방에서 찾기
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
                        Debug.Log($"[DismantleSystem] 슬롯 {i} 가방에서 제거: {instanceId}");
                        return true;
                    }
                }
            }
            
            Debug.LogWarning($"[DismantleSystem] 아이템을 찾을 수 없음: {instanceId}");
            return false;
        }
        
        /// <summary>
        /// 분해 경고가 필요한지 확인
        /// </summary>
        public static bool NeedsWarning(ItemInstanceId instanceId)
        {
            var account = AccountDataManager.Instance;
            var itemData = account.GetInstance(instanceId);
            
            if (itemData == null) return false;
            
            // 1. 귀속된 아이템
            var bindInfo = account.GetBindInfo(instanceId);
            if (bindInfo.isBound) return true;
            
            // 2. 고등급 아이템 (S, A)
            var template = ItemTemplateResolver.Load(itemData.templateName);
            if (template != null)
            {
                if (template.itemGrade == ItemGrade.S || 
                    template.itemGrade == ItemGrade.A)
                {
                    return true;
                }
            }
            
            // 3. 강화 +10 이상
            if (itemData.enhancementLevel >= 10) return true;
            
            return false;
        }
    }
}

