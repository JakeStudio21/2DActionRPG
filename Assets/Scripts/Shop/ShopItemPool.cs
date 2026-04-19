using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shop
{
    /// <summary>
    /// 상점 전시용 ItemInstance 풀 관리
    /// - 상점 초기화 시 1회만 전시용 Instance 생성
    /// - 구매 시에만 새로운 플레이어 전용 Instance 생성
    /// </summary>
    public class ShopItemPool
    {
        #region Fields
        
        // 전시용 Instance 저장소 (key: templateName, value: 전시용 ItemInstanceID)
        private Dictionary<string, ItemInstanceID> displayInstances = new Dictionary<string, ItemInstanceID>();
        
        // 전시용 Instance → EquipmentData 매핑 (빠른 조회용)
        private Dictionary<ItemInstanceID, EquipmentData> displayEquipmentData = new Dictionary<ItemInstanceID, EquipmentData>();
        
        // 🆕 동적 스탯 캐시 (메모리 전용, Lazy Generation)
        // - 클릭한 아이템만 생성 (메모리 효율)
        // - 동일 아이템 재클릭 시 동일 스탯 표시 (일관성)
        // - AccountData 오염 방지 (고아 아이템 방지)
        private Dictionary<ItemInstanceID, EquipmentInstance> dynamicStatsCache = new Dictionary<ItemInstanceID, EquipmentInstance>();
        
        // 초기화 완료 플래그
        private bool isInitialized = false;
        
        // 디버그 로그
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 상점 초기화 (1회만 실행)
        /// D/C/B/A 등급 장비의 전시용 Instance 생성
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning("⚠️ [ShopItemPool] 이미 초기화되었습니다.");
                return;
            }
            
            Log("🏪 [ShopItemPool] 상점 초기화 시작...");
            
            // 1. Resources/Equipment/ 모든 장비 로드
            EquipmentData[] allEquipments = Resources.LoadAll<EquipmentData>("Equipment");
            
            if (allEquipments == null || allEquipments.Length == 0)
            {
                Debug.LogError("❌ [ShopItemPool] EquipmentData를 찾을 수 없습니다!");
                return;
            }
            
            Log($"📦 [ShopItemPool] 총 {allEquipments.Length}개 장비 로드 완료");
            
            // 2. D/C/B/A 등급만 필터링
            var shopEquipments = allEquipments.Where(eq => IsShopItem(eq)).ToList();
            
            Log($"🎯 [ShopItemPool] 상점 노출 장비: {shopEquipments.Count}개 (D/C/B/A 등급만)");
            
            // 3. 각 장비마다 전시용 ItemInstance 생성
            int successCount = 0;
            foreach (var equipment in shopEquipments)
            {
                if (CreateDisplayInstance(equipment))
                {
                    successCount++;
                }
            }
            
            isInitialized = true;
            Log($"✅ [ShopItemPool] 초기화 완료! 전시용 Instance {successCount}개 생성");
        }
        
        /// <summary>
        /// 전시용 Instance 조회
        /// </summary>
        public ItemInstanceID GetDisplayInstance(string templateName)
        {
            if (displayInstances.TryGetValue(templateName, out ItemInstanceID instanceId))
            {
                return instanceId;
            }
            
            Debug.LogWarning($"⚠️ [ShopItemPool] 전시용 Instance를 찾을 수 없습니다: {templateName}");
            return default;
        }
        
        /// <summary>
        /// 전시용 Instance ID로 EquipmentData 조회
        /// </summary>
        public EquipmentData GetEquipmentData(ItemInstanceID displayInstanceId)
        {
            if (displayEquipmentData.TryGetValue(displayInstanceId, out EquipmentData data))
            {
                return data;
            }
            
            Debug.LogWarning($"⚠️ [ShopItemPool] EquipmentData를 찾을 수 없습니다 (ID: {displayInstanceId.Value})");
            return null;
        }
        
        /// <summary>
        /// 🆕 동적 스탯 Lazy Generation (캐시 우선)
        /// - 팝업에서 본 스탯 = 구매 시 받을 스탯 (일관성)
        /// - 클릭 시 생성, 재클릭 시 동일 스탯 표시
        /// - AccountData 오염 없음 (메모리 전용 캐시)
        /// </summary>
        public EquipmentInstance GetOrCreateDynamicStats(ItemInstanceID displayInstanceId)
        {
            // 1. 캐시 확인 (이미 생성됨)
            if (dynamicStatsCache.TryGetValue(displayInstanceId, out EquipmentInstance cachedInstance))
            {
                Log($"♻️ [ShopItemPool] 캐시된 동적 스탯 반환: {cachedInstance.EquipmentData.equipmentName}");
                return cachedInstance;
            }
            
            // 2. EquipmentData 조회
            EquipmentData equipment = GetEquipmentData(displayInstanceId);
            if (equipment == null)
            {
                Debug.LogError($"❌ [ShopItemPool] EquipmentData를 찾을 수 없습니다: {displayInstanceId.Value}");
                return null;
            }
            
            // 3. 동적 스탯 생성 (첫 클릭)
            EquipmentInstance dynamicInstance = DynamicEquipmentGenerator.Generate(equipment, equipment.itemGrade);
            
            if (dynamicInstance == null)
            {
                Debug.LogError($"❌ [ShopItemPool] 동적 스탯 생성 실패: {equipment.equipmentName}");
                return null;
            }
            
            // 4. 캐시 저장 (메모리 전용)
            dynamicStatsCache[displayInstanceId] = dynamicInstance;
            
            Log($"🎲 [ShopItemPool] 동적 스탯 생성 완료: {equipment.equipmentName} (주옵션: {dynamicInstance.finalMainStatValue:F1}, 부옵션: {dynamicInstance.randomSubStats.Count}개)");
            
            return dynamicInstance;
        }
        
        /// <summary>
        /// 🆕 동적 스탯 캐시 초기화 (상점 새로고침용)
        /// </summary>
        public void ClearDynamicStatsCache()
        {
            int cacheCount = dynamicStatsCache.Count;
            dynamicStatsCache.Clear();
            
            Log($"🔄 [ShopItemPool] 동적 스탯 캐시 초기화 완료 ({cacheCount}개 제거)");
        }
        
        /// <summary>
        /// 전시용 Instance가 존재하는지 확인
        /// </summary>
        public bool HasDisplayInstance(string templateName)
        {
            return displayInstances.ContainsKey(templateName);
        }
        
        /// <summary>
        /// 구매 시 새로운 플레이어 전용 Instance 생성
        /// </summary>
        public ItemInstanceID CreateNewInstance(string templateName)
        {
            if (AccountDataManager.Instance == null)
            {
                Debug.LogError("❌ [ShopItemPool] AccountDataManager.Instance가 null입니다!");
                return default;
            }
            
            // 새 Instance 생성 (플레이어 전용)
            var newInstanceId = AccountDataManager.Instance.CreateInstance(templateName);
            
            if (!newInstanceId.IsEmpty)
            {
                Log($"🆕 [ShopItemPool] 새 Instance 생성: {templateName} (ID: {newInstanceId.Value.Substring(0, 8)}...)");
                return newInstanceId;
            }
            else
            {
                Debug.LogError($"❌ [ShopItemPool] Instance 생성 실패: {templateName}");
                return default;
            }
        }
        
        /// <summary>
        /// 초기화 상태 확인
        /// </summary>
        public bool IsInitialized => isInitialized;
        
        /// <summary>
        /// 전시용 Instance 개수
        /// </summary>
        public int DisplayInstanceCount => displayInstances.Count;
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// 상점에 노출할 아이템인지 체크
        /// </summary>
        private bool IsShopItem(EquipmentData equipment)
        {
            if (equipment == null)
                return false;
            
            // 1. D/C/B/A 등급만 (S/SS/EX/TR 제외)
            if (equipment.itemGrade != ItemGrade.D &&
                equipment.itemGrade != ItemGrade.C &&
                equipment.itemGrade != ItemGrade.B &&
                equipment.itemGrade != ItemGrade.A)
            {
                return false;
            }
            
            // 2. 무기 또는 방어구만
            if (equipment.equipmentType != EquipmentType.Weapon &&
                equipment.equipmentType != EquipmentType.Armor)
            {
                return false;
            }
            
            // 3. 클래스 제한 (Warrior, Assasin, Wizard, Any만)
            if (equipment.usableClass != PlayerClass.Warrior &&
                equipment.usableClass != PlayerClass.Assasin &&
                equipment.usableClass != PlayerClass.Wizard &&
                equipment.usableClass != PlayerClass.Any &&
                equipment.usableClass != PlayerClass.None)  // None은 Any와 동일하게 처리
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 전시용 Instance 생성 (메모리 전용, JSON 저장 안 함)
        /// </summary>
        private bool CreateDisplayInstance(EquipmentData equipment)
        {
            if (equipment == null)
            {
                Debug.LogError("❌ [ShopItemPool] equipment가 null입니다!");
                return false;
            }
            
            // ⭐ itemID를 templateName으로 사용 (V2 시스템 표준)
            string templateName = equipment.itemID;
            
            if (string.IsNullOrEmpty(templateName))
            {
                Debug.LogError($"❌ [ShopItemPool] equipment.itemID가 비어있습니다! (equipmentName: {equipment.equipmentName})");
                return false;
            }
            
            // 이미 존재하면 스킵
            if (displayInstances.ContainsKey(templateName))
            {
                Log($"♻️ [ShopItemPool] 기존 전시용 Instance 재사용: {templateName}");
                return true; // 스킵하되 성공으로 처리
            }
            
            // ✅ 메모리 전용 ItemInstanceID 생성 (AccountData에 저장 안 함)
            var displayInstanceId = ItemInstanceID.Generate();
            
            if (displayInstanceId.IsEmpty)
            {
                Debug.LogError($"❌ [ShopItemPool] 전시용 Instance ID 생성 실패: {templateName}");
                return false;
            }
            
            // 저장 (메모리에만, itemID 기반)
            displayInstances[templateName] = displayInstanceId;
            displayEquipmentData[displayInstanceId] = equipment;
            
            Log($"🎁 [ShopItemPool] 전시용 Instance 생성 (메모리 전용): {equipment.equipmentName} (itemID: {templateName}, 등급: {equipment.itemGrade}, ID: {displayInstanceId.Value.Substring(0, 8)}...)");
            
            return true;
        }
        
        /// <summary>
        /// 디버그 로그 출력
        /// </summary>
        private void Log(string message)
        {
        }
        
        #endregion
    }
}

