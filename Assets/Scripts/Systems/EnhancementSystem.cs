using System.Collections.Generic;
using UnityEngine;
using Managers;

namespace Systems
{
    /// <summary>
    /// 강화 시스템 핵심 로직
    /// - 강화 시도 (재료 소모 + 성공/실패 판정)
    /// - 강화 검증
    /// - 강화 결과 적용
    /// </summary>
    public static class EnhancementSystem
    {
        private static EnhancementData _data;
        private static EnhancementData Data
        {
            get
            {
                if (_data == null)
                {
                    _data = Resources.Load<EnhancementData>("Data/EnhancementData");
                    if (_data == null)
                    {
                        Debug.LogError("[EnhancementSystem] EnhancementData를 찾을 수 없습니다. (경로: Resources/Data/EnhancementData)");
                    }
                }
                return _data;
            }
        }
        
        /// <summary>
        /// 강화 가능 여부 검증
        /// </summary>
        public static bool CanEnhance(ItemInstanceId instanceId, out string reason)
        {
            reason = "";
            
            if (!AccountDataManager.IsInitialized())
            {
                reason = "AccountDataManager가 초기화되지 않았습니다.";
                return false;
            }
            
            var account = AccountDataManager.Instance;
            var itemData = account.GetInstance(instanceId);
            
            if (itemData == null)
            {
                reason = "아이템을 찾을 수 없습니다.";
                return false;
            }
            
            // 템플릿 로드
            var template = ItemTemplateResolver.Load(itemData.templateName);
            if (template == null)
            {
                reason = $"템플릿을 찾을 수 없습니다: {itemData.templateName}";
                return false;
            }
            
            // 최대 레벨 확인
            if (itemData.enhancementLevel >= Data.maxEnhancementLevel)
            {
                reason = $"이미 최대 강화 레벨입니다. (+{Data.maxEnhancementLevel})";
                return false;
            }
            
            // 재료 확인 (장비 타입 + 등급 기반)
            MaterialType requiredMaterial = Data.GetRequiredMaterialType(template.equipmentType, template.itemGrade);
            int requiredAmount = Data.GetRequiredMaterialAmount(template.itemGrade, itemData.enhancementLevel + 1);
            int ownedAmount = account.GetMaterialCount(requiredMaterial);
            
            if (ownedAmount < requiredAmount)
            {
                reason = $"{requiredMaterial.GetDisplayName()}이(가) 부족합니다. (필요: {requiredAmount}, 보유: {ownedAmount})";
                return false;
            }
            
            // 골드 확인
            int requiredGold = Data.GetRequiredGold(template.itemGrade, itemData.enhancementLevel + 1);
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
            {
                var slotData = PlayerDataManager.Instance.GetSlotData(PlayerDataManager.Instance.CurrentSlotIndex);
                if (slotData != null && slotData.gold < requiredGold)
                {
                    reason = $"골드가 부족합니다. (필요: {requiredGold}, 보유: {slotData.gold})";
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 파괴 경고 필요 여부
        /// </summary>
        public static bool NeedsDestructionWarning(ItemInstanceId instanceId)
        {
            if (!AccountDataManager.IsInitialized()) return false;
            
            var account = AccountDataManager.Instance;
            var itemData = account.GetInstance(instanceId);
            
            if (itemData == null) return false;
            
            // 현재 레벨이 파괴 구간인지 확인
            var failureType = Data.GetFailureType(itemData.enhancementLevel);
            return failureType == EnhancementFailureType.Destroy;
        }
        
        /// <summary>
        /// 강화 성공률 조회
        /// </summary>
        public static float GetSuccessRate(ItemInstanceId instanceId)
        {
            if (!AccountDataManager.IsInitialized()) return 0f;
            
            var account = AccountDataManager.Instance;
            var itemData = account.GetInstance(instanceId);
            
            if (itemData == null) return 0f;
            
            var template = ItemTemplateResolver.Load(itemData.templateName);
            if (template == null) return 0f;
            
            return Data.CalculateSuccessRate(template.itemGrade, itemData.enhancementLevel);
        }
        
        /// <summary>
        /// 강화 시도 (실제 실행)
        /// </summary>
        public static EnhancementResult ExecuteEnhancement(ItemInstanceId instanceId)
        {
            var result = new EnhancementResult { success = false };
            
            // 검증
            if (!CanEnhance(instanceId, out string reason))
            {
                result.errorMessage = reason;
                Debug.LogError($"[EnhancementSystem] 강화 불가: {reason}");
                return result;
            }
            
            var account = AccountDataManager.Instance;
            var playerData = PlayerDataManager.Instance;
            var itemData = account.GetInstance(instanceId);
            var template = ItemTemplateResolver.Load(itemData.templateName);
            
            int currentLevel = itemData.enhancementLevel;
            int targetLevel = currentLevel + 1;
            
            try
            {
                // 1. 재료 소모 (장비 타입 + 등급 기반)
                MaterialType materialType = Data.GetRequiredMaterialType(template.equipmentType, template.itemGrade);
                int materialAmount = Data.GetRequiredMaterialAmount(template.itemGrade, targetLevel);
                
                if (!account.RemoveMaterial(materialType, materialAmount))
                {
                    result.errorMessage = "재료 소모 실패";
                    return result;
                }
                
                Debug.Log($"💎 [EnhancementSystem] 재료 소모: {materialType.GetDisplayName()} -{materialAmount}");
                
                // 2. 골드 소모
                int goldCost = Data.GetRequiredGold(template.itemGrade, targetLevel);
                if (playerData != null && playerData.IsSlotSelected)
                {
                    var slotData = playerData.GetSlotData(playerData.CurrentSlotIndex);
                    if (slotData != null)
                    {
                        slotData.gold -= goldCost;
                        playerData.SaveSlotData(slotData);
                        Debug.Log($"💰 [EnhancementSystem] 골드 소모: -{goldCost} (잔액: {slotData.gold})");
                    }
                }
                
                // 3. 성공/실패 판정
                float successRate = Data.CalculateSuccessRate(template.itemGrade, currentLevel);
                float randomValue = Random.Range(0f, 100f);
                bool enhancementSuccess = randomValue <= successRate;
                
                Debug.Log($"🎲 [EnhancementSystem] 판정: {randomValue:F2}% (성공률: {successRate:F2}%) → {(enhancementSuccess ? "성공" : "실패")}");
                
                // 4. 결과 적용
                if (enhancementSuccess)
                {
                    // 성공: 레벨 증가
                    itemData.enhancementLevel = targetLevel;
                    itemData.enhancementAttempts++;
                    
                    result.success = true;
                    result.newLevel = targetLevel;
                    result.wasDestroyed = false;
                    
                    Debug.Log($"✨ [EnhancementSystem] 강화 성공: {template.equipmentName} +{currentLevel} → +{targetLevel}");
                }
                else
                {
                    // 실패: 실패 타입에 따라 처리
                    var failureType = Data.GetFailureType(currentLevel);
                    itemData.enhancementAttempts++;
                    
                    switch (failureType)
                    {
                        case EnhancementFailureType.Maintain:
                            // 유지
                            result.success = false;
                            result.newLevel = currentLevel;
                            result.wasDestroyed = false;
                            Debug.Log($"⚠️ [EnhancementSystem] 강화 실패 (유지): {template.equipmentName} +{currentLevel}");
                            break;
                            
                        case EnhancementFailureType.Downgrade:
                            // 1단계 하락
                            itemData.enhancementLevel = Mathf.Max(0, currentLevel - 1);
                            result.success = false;
                            result.newLevel = itemData.enhancementLevel;
                            result.wasDestroyed = false;
                            Debug.Log($"⬇️ [EnhancementSystem] 강화 실패 (하락): {template.equipmentName} +{currentLevel} → +{itemData.enhancementLevel}");
                            break;
                            
                        case EnhancementFailureType.Destroy:
                            // 아이템 파괴
                            result.success = false;
                            result.newLevel = 0;
                            result.wasDestroyed = true;
                            
                            // 인벤토리에서 제거
                            RemoveItemFromInventory(instanceId);
                            
                            // 아이템 인스턴스 삭제
                            account.RemoveInstance(instanceId);
                            
                            Debug.Log($"💥 [EnhancementSystem] 강화 실패 (파괴): {template.equipmentName} +{currentLevel}");
                            break;
                    }
                }
                
                // 5. 저장
                account.Save();
                
                return result;
            }
            catch (System.Exception ex)
            {
                result.errorMessage = ex.Message;
                Debug.LogError($"[EnhancementSystem] 강화 실패: {ex.Message}");
                return result;
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

            Debug.LogWarning($"[EnhancementSystem] 아이템을 인벤토리에서 찾을 수 없음: {instanceId}");
            return false;
        }
    }
    
    /// <summary>
    /// 강화 결과 데이터
    /// </summary>
    public class EnhancementResult
    {
        public bool success;            // 강화 성공 여부
        public int newLevel;            // 새 강화 레벨
        public bool wasDestroyed;       // 아이템 파괴 여부
        public string errorMessage;     // 에러 메시지
    }
}

