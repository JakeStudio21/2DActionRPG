using UnityEngine;
using System.Collections.Generic;

namespace DebugTools
{
    /// <summary>
    /// 치트 서비스 - 실제 아이템/재료/골드 생성 로직
    /// - UI/콘솔/버튼에서 공통으로 사용
    /// - AccountDataManager, PlayerDataManager와 연동
    /// </summary>
    public static class CheatService
    {
        // ========================================
        // 아이템 생성
        // ========================================
        
        /// <summary>
        /// 아이템 생성 (계정 공유 창고에 추가)
        /// </summary>
        /// <param name="templateId">아이템 템플릿 ID (예: Sword_C)</param>
        /// <param name="count">생성 개수</param>
        /// <param name="enhanceLevel">강화 레벨 (0~15)</param>
        /// <param name="isBound">귀속 여부 (TODO: 현재 미구현)</param>
        public static CheatResult CreateItem(string templateId, int count, int enhanceLevel, bool isBound)
        {
            // 1. 사전 검증
            if (!AccountDataManager.IsInitialized())
            {
                return CheatResult.Fail("❌ AccountDataManager가 초기화되지 않았습니다. 로비에서 실행하세요.");
            }
            
            if (count <= 0)
            {
                return CheatResult.Fail($"❌ 개수는 1개 이상이어야 합니다: {count}");
            }
            
            // 2. 템플릿 검증
            var template = ItemTemplateResolver.Load(templateId);
            if (template == null)
            {
                return CheatResult.Fail($"❌ 아이템 템플릿을 찾을 수 없습니다: {templateId}");
            }
            
            // 3. 강화 레벨 검증
            if (enhanceLevel < 0 || enhanceLevel > 15)
            {
                return CheatResult.Fail($"❌ 강화 레벨은 0~15 사이여야 합니다: {enhanceLevel}");
            }
            
            var account = AccountDataManager.Instance;
            var createdIds = new List<ItemInstanceId>();
            
            // 4. 아이템 생성 (count개)
            for (int i = 0; i < count; i++)
            {
                var instanceId = account.CreateInstance(templateId);
                var instanceData = account.GetInstance(instanceId);
                
                if (instanceData != null)
                {
                    // 강화 레벨 설정
                    instanceData.enhancementLevel = enhanceLevel;
                    
                    // TODO: 귀속 설정 (isBound 필드가 ItemInstanceData에 아직 없음)
                    // instanceData.isBound = isBound;
                    
                    // 계정 공유 창고에 추가
                    account.GetAccountData().sharedInventoryIds.Add(instanceId);
                    
                    createdIds.Add(instanceId);
                }
            }
            
            // 5. 결과 메시지 생성
            string enhanceText = enhanceLevel > 0 ? $" +{enhanceLevel}" : "";
            string boundText = isBound ? " (귀속 예정)" : ""; // TODO: 귀속 시스템 구현 후 제거
            string message = $"✅ {template.equipmentName}{enhanceText} x{count} 생성 완료{boundText}";
            
            // 6. UI 갱신 이벤트 발생 (보관창고/상점/강화패널 모두 갱신)
            PlayerDataManager.Instance?.NotifyInventoryChanged();
            
            Debug.Log($"🎮 [CheatService] {message}");
            
            return CheatResult.SuccessWithItems(message, createdIds);
        }
        
        // ========================================
        // 재료 생성
        // ========================================
        
        /// <summary>
        /// 재료 추가 (계정 공유)
        /// </summary>
        /// <param name="materialType">재료 타입</param>
        /// <param name="count">추가할 개수</param>
        public static CheatResult AddMaterial(MaterialType materialType, int count)
        {
            // 1. 사전 검증
            if (!AccountDataManager.IsInitialized())
            {
                return CheatResult.Fail("❌ AccountDataManager가 초기화되지 않았습니다. 로비에서 실행하세요.");
            }
            
            if (count <= 0)
            {
                return CheatResult.Fail($"❌ 개수는 1개 이상이어야 합니다: {count}");
            }
            
            if (materialType == MaterialType.None)
            {
                return CheatResult.Fail("❌ 유효하지 않은 재료 타입입니다.");
            }
            
            // 2. 재료 추가
            var account = AccountDataManager.Instance;
            account.AddMaterial(materialType, count);
            
            // 3. 결과 메시지
            string materialName = materialType.GetDisplayName();
            string message = $"✅ {materialName} x{count} 추가 완료";
            
            Debug.Log($"🎮 [CheatService] {message}");
            
            return CheatResult.Success(message);
        }
        
        // ========================================
        // 골드 생성
        // ========================================
        
        /// <summary>
        /// 골드 추가 (현재 슬롯)
        /// </summary>
        /// <param name="amount">추가할 골드</param>
        public static CheatResult AddGold(int amount)
        {
            // 1. 사전 검증
            if (PlayerDataManager.Instance == null)
            {
                return CheatResult.Fail("❌ PlayerDataManager가 초기화되지 않았습니다.");
            }
            
            if (!PlayerDataManager.Instance.IsSlotSelected)
            {
                return CheatResult.Fail("❌ 슬롯이 선택되지 않았습니다. 로비에서 실행하세요.");
            }
            
            if (amount <= 0)
            {
                return CheatResult.Fail($"❌ 금액은 1 이상이어야 합니다: {amount}");
            }
            
            // 2. 골드 추가 (계정 공유)
            int oldGold = PlayerDataManager.Instance.CurrentGold;
            AccountDataManager.Instance.AddGold(amount);
            int newGold = PlayerDataManager.Instance.CurrentGold;
            
            // 3. 결과 메시지
            string message = $"✅ 골드 {amount:N0}G 추가 완료 (잔액: {newGold:N0}G)";
            
            Debug.Log($"🎮 [CheatService] {message}");
            
            return CheatResult.Success(message)
                .WithMetadata("oldGold", oldGold)
                .WithMetadata("newGold", newGold);
        }
        
        // ========================================
        // 강화 레벨 설정
        // ========================================
        
        /// <summary>
        /// 기존 아이템의 강화 레벨 변경
        /// </summary>
        /// <param name="instanceId">아이템 인스턴스 ID</param>
        /// <param name="level">새 강화 레벨 (0~15)</param>
        public static CheatResult SetEnhanceLevel(ItemInstanceId instanceId, int level)
        {
            // 1. 사전 검증
            if (!AccountDataManager.IsInitialized())
            {
                return CheatResult.Fail("❌ AccountDataManager가 초기화되지 않았습니다.");
            }
            
            if (level < 0 || level > 15)
            {
                return CheatResult.Fail($"❌ 강화 레벨은 0~15 사이여야 합니다: {level}");
            }
            
            // 2. 아이템 조회
            var account = AccountDataManager.Instance;
            var itemData = account.GetInstance(instanceId);
            
            if (itemData == null)
            {
                return CheatResult.Fail($"❌ 아이템 인스턴스를 찾을 수 없습니다: {instanceId}");
            }
            
            // 3. 템플릿 로드 (이름 표시용)
            var template = ItemTemplateResolver.Load(itemData.templateName);
            string itemName = template != null ? template.equipmentName : itemData.templateName;
            
            // 4. 강화 레벨 변경
            int oldLevel = itemData.enhancementLevel;
            itemData.enhancementLevel = level;
            
            // 5. UI 갱신 이벤트 발생 (보관창고/상점/강화패널 모두 갱신)
            PlayerDataManager.Instance?.NotifyInventoryChanged();
            
            // 6. 결과 메시지
            string message = $"✅ {itemName} 강화 레벨 변경: +{oldLevel} → +{level}";
            
            Debug.Log($"🎮 [CheatService] {message}");
            
            return CheatResult.Success(message)
                .WithMetadata("instanceId", instanceId)
                .WithMetadata("oldLevel", oldLevel)
                .WithMetadata("newLevel", level);
        }
        
        // ========================================
        // 헬퍼 메서드
        // ========================================
        
        /// <summary>
        /// 재료 타입 문자열 → DisplayName 변환
        /// </summary>
        public static string GetMaterialDisplayName(MaterialType materialType)
        {
            return materialType switch
            {
                MaterialType.WeaponFragment => "무기 강화 파편",
                MaterialType.WeaponCrystal => "무기 강화 결정",
                MaterialType.WeaponCore => "무기 강화 코어",
                MaterialType.ArmorFragment => "방어구 강화 파편",
                MaterialType.ArmorCrystal => "방어구 강화 결정",
                MaterialType.ArmorCore => "방어구 강화 코어",
                MaterialType.AccessoryFragment => "악세서리 강화 파편",
                MaterialType.AccessoryCrystal => "악세서리 강화 결정",
                MaterialType.AccessoryCore => "악세서리 강화 코어",
                MaterialType.CraftingEssence => "제작 정수",
                _ => materialType.ToString()
            };
        }
    }
}

