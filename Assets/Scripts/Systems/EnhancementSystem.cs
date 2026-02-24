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
        // ⭐ 새로운 2개 SO 시스템
        private static EnhanceLevelTableSO _levelTable;
        private static EnhanceCurveTableSO _curveTable;
        
        /// <summary>
        /// 강화 레벨 테이블 (비용/확률/보너스)
        /// </summary>
        private static EnhanceLevelTableSO LevelTable
        {
            get
            {
                if (_levelTable == null)
                {
                    _levelTable = Resources.Load<EnhanceLevelTableSO>("Data/EnhanceLevelTable");
                    if (_levelTable == null)
                    {
                        Debug.LogError("[EnhancementSystem] EnhanceLevelTableSO를 찾을 수 없습니다. (경로: Resources/Data/EnhanceLevelTable)");
                    }
                }
                return _levelTable;
            }
        }
        
        /// <summary>
        /// 강화 곡선 테이블 (스탯 증가율)
        /// </summary>
        private static EnhanceCurveTableSO CurveTable
        {
            get
            {
                if (_curveTable == null)
                {
                    _curveTable = Resources.Load<EnhanceCurveTableSO>("Data/EnhanceCurveTable");
                    if (_curveTable == null)
                    {
                        Debug.LogError("[EnhancementSystem] EnhanceCurveTableSO를 찾을 수 없습니다. (경로: Resources/Data/EnhanceCurveTable)");
                    }
                }
                return _curveTable;
            }
        }
        
        // ⚠️ 기존 EnhancementData (Deprecated - 하위 호환성 유지용)
        private static EnhancementData _legacyData;
        private static EnhancementData LegacyData
        {
            get
            {
                if (_legacyData == null)
                {
                    _legacyData = Resources.Load<EnhancementData>("Data/EnhancementData");
                }
                return _legacyData;
            }
        }
        
        /// <summary>
        /// 강화 가능 여부 검증
        /// </summary>
        public static bool CanEnhance(ItemInstanceID instanceId, out string reason)
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
            if (itemData.enhancementLevel >= LevelTable.maxEnhancementLevel)
            {
                reason = $"이미 최대 강화 레벨입니다. (+{LevelTable.maxEnhancementLevel})";
                return false;
            }
            
            int targetLevel = itemData.enhancementLevel + 1;
            
            // ⭐ 새 SO: 재료 확인
            MaterialType requiredMaterial = GetRequiredMaterialType(template.equipmentType, template.itemGrade);
            int requiredAmount = LevelTable.GetMaterialCount(targetLevel); // ⭐ 새 SO
            int ownedAmount = account.GetMaterialCount(requiredMaterial);
            
            if (ownedAmount < requiredAmount)
            {
                reason = $"{requiredMaterial.GetDisplayName()}이(가) 부족합니다. (필요: {requiredAmount}, 보유: {ownedAmount})";
                return false;
            }
            
            // ⭐ 새 SO: 골드 확인
            int requiredGold = LevelTable.GetGoldCost(targetLevel); // ⭐ 새 SO
            if (PlayerDataManager.Instance != null)
            {
                int currentGold = PlayerDataManager.Instance.CurrentGold;
                if (currentGold < requiredGold)
                {
                    reason = $"골드가 부족합니다. (필요: {requiredGold}, 보유: {currentGold})";
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 파괴 경고 필요 여부
        /// </summary>
        public static bool NeedsDestructionWarning(ItemInstanceID instanceId)
        {
            if (!AccountDataManager.IsInitialized()) return false;
            
            var account = AccountDataManager.Instance;
            var itemData = account.GetInstance(instanceId);
            
            if (itemData == null) return false;
            
            // ⭐ 새 SO: 실패 처리 규칙
            int targetLevel = itemData.enhancementLevel + 1;
            var failureType = LevelTable.GetFailureType(targetLevel);
            return failureType == EnhancementFailureType.Destroy;
        }
        
        /// <summary>
        /// 강화 성공률 조회 (⭐ 새 SO 기반)
        /// </summary>
        public static float GetSuccessRate(ItemInstanceID instanceId)
        {
            if (!AccountDataManager.IsInitialized()) return 0f;
            
            var account = AccountDataManager.Instance;
            var itemData = account.GetInstance(instanceId);
            
            if (itemData == null) return 0f;
            
            int targetLevel = itemData.enhancementLevel + 1;
            
            // ⭐ 새 SO: 성공률 (0~100%)
            return LevelTable.GetSuccessRate(targetLevel);
        }
        
        /// <summary>
        /// 강화 시도 (실제 실행)
        /// </summary>
        public static EnhancementResult ExecuteEnhancement(ItemInstanceID instanceId)
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
                // ⭐ 1. 재료 소모 (새 SO)
                MaterialType materialType = GetRequiredMaterialType(template.equipmentType, template.itemGrade);
                int materialAmount = LevelTable.GetMaterialCount(targetLevel); // ⭐ 새 SO
                
                if (!account.RemoveMaterial(materialType, materialAmount))
                {
                    result.errorMessage = "재료 소모 실패";
                    return result;
                }
                
                Debug.Log($"💎 [EnhancementSystem] 재료 소모: {materialType.GetDisplayName()} -{materialAmount}");
                
                // ⭐ 2. 골드 소모 (새 SO) - V2 계정 공유 골드
                int goldCost = LevelTable.GetGoldCost(targetLevel); // ⭐ 새 SO
                
                // ⭐ PlayerDataManager.SpendGold() 사용 (UI 이벤트 자동 발행)
                if (!playerData.SpendGold(goldCost))
                {
                    result.errorMessage = "골드 소모 실패";
                    Debug.LogError($"❌ [EnhancementSystem] 골드 소모 실패: {goldCost}");
                    return result;
                }
                
                Debug.Log($"💰 [EnhancementSystem] 골드 소모: -{goldCost} (잔액: {playerData.CurrentGold})");
                
                // ⭐ 3. 성공/실패 판정 (새 SO)
                float successRate = LevelTable.GetSuccessRate(targetLevel); // ⭐ 새 SO (0~100%)
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
                    // 실패: 실패 타입에 따라 처리 (⭐ 새 SO 기반)
                    var failureType = LevelTable.GetFailureType(targetLevel);
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
                
                // 6. ⭐ UI 이벤트 발생 (상점 UI 갱신용)
                if (PlayerDataManager.Instance != null)
                {
                    PlayerDataManager.Instance.NotifyInventoryChanged();
                }
                
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
        /// <summary>
        /// 장비 타입과 등급에 따른 필요 재료 타입 반환 (헬퍼 메서드)
        /// </summary>
        private static MaterialType GetRequiredMaterialType(EquipmentType equipType, ItemGrade grade)
        {
            // ⭐ 장비 타입 × 등급에 따른 재료 매핑
            // 무기: WeaponFragment/Crystal/Core
            // 방어구: ArmorFragment/Crystal/Core
            // 악세사리: AccessoryFragment/Crystal/Core
            
            if (equipType == EquipmentType.Weapon)
            {
                return grade switch
                {
                    ItemGrade.D or ItemGrade.C or ItemGrade.B => MaterialType.WeaponFragment,
                    ItemGrade.A or ItemGrade.S or ItemGrade.SS => MaterialType.WeaponCrystal,
                    ItemGrade.EX or ItemGrade.TR => MaterialType.WeaponCore,
                    _ => MaterialType.WeaponFragment
                };
            }
            else if (equipType == EquipmentType.Armor)
            {
                return grade switch
                {
                    ItemGrade.D or ItemGrade.C or ItemGrade.B => MaterialType.ArmorFragment,
                    ItemGrade.A or ItemGrade.S or ItemGrade.SS => MaterialType.ArmorCrystal,
                    ItemGrade.EX or ItemGrade.TR => MaterialType.ArmorCore,
                    _ => MaterialType.ArmorFragment
                };
            }
            else // Accessory
            {
                return grade switch
                {
                    ItemGrade.D or ItemGrade.C or ItemGrade.B => MaterialType.AccessoryFragment,
                    ItemGrade.A or ItemGrade.S or ItemGrade.SS => MaterialType.AccessoryCrystal,
                    ItemGrade.EX or ItemGrade.TR => MaterialType.AccessoryCore,
                    _ => MaterialType.AccessoryFragment
                };
            }
        }
        
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


