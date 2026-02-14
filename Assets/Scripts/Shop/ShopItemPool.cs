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
        
        // 전시용 Instance 저장소 (key: templateName, value: 전시용 ItemInstanceId)
        private Dictionary<string, ItemInstanceId> displayInstances = new Dictionary<string, ItemInstanceId>();
        
        // 전시용 Instance → EquipmentData 매핑 (빠른 조회용)
        private Dictionary<ItemInstanceId, EquipmentData> displayEquipmentData = new Dictionary<ItemInstanceId, EquipmentData>();
        
        // 초기화 완료 플래그
        private bool isInitialized = false;
        
        // 디버그 로그
        private bool showDebugLogs = true;
        
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
        public ItemInstanceId GetDisplayInstance(string templateName)
        {
            if (displayInstances.TryGetValue(templateName, out ItemInstanceId instanceId))
            {
                return instanceId;
            }
            
            Debug.LogWarning($"⚠️ [ShopItemPool] 전시용 Instance를 찾을 수 없습니다: {templateName}");
            return default;
        }
        
        /// <summary>
        /// 전시용 Instance ID로 EquipmentData 조회
        /// </summary>
        public EquipmentData GetEquipmentData(ItemInstanceId displayInstanceId)
        {
            if (displayEquipmentData.TryGetValue(displayInstanceId, out EquipmentData data))
            {
                return data;
            }
            
            Debug.LogWarning($"⚠️ [ShopItemPool] EquipmentData를 찾을 수 없습니다 (ID: {displayInstanceId.id})");
            return null;
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
        public ItemInstanceId CreateNewInstance(string templateName)
        {
            if (AccountDataManager.Instance == null)
            {
                Debug.LogError("❌ [ShopItemPool] AccountDataManager.Instance가 null입니다!");
                return default;
            }
            
            // 새 Instance 생성 (플레이어 전용)
            var newInstanceId = AccountDataManager.Instance.CreateInstance(templateName);
            
            if (newInstanceId.IsValid())
            {
                Log($"🆕 [ShopItemPool] 새 Instance 생성: {templateName} (ID: {newInstanceId.id.Substring(0, 8)}...)");
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
            
            // ✅ 메모리 전용 ItemInstanceId 생성 (AccountData에 저장 안 함)
            var displayInstanceId = new ItemInstanceId { id = System.Guid.NewGuid().ToString() };
            
            if (!displayInstanceId.IsValid())
            {
                Debug.LogError($"❌ [ShopItemPool] 전시용 Instance ID 생성 실패: {templateName}");
                return false;
            }
            
            // 저장 (메모리에만, itemID 기반)
            displayInstances[templateName] = displayInstanceId;
            displayEquipmentData[displayInstanceId] = equipment;
            
            Log($"🎁 [ShopItemPool] 전시용 Instance 생성 (메모리 전용): {equipment.equipmentName} (itemID: {templateName}, 등급: {equipment.itemGrade}, ID: {displayInstanceId.id.Substring(0, 8)}...)");
            
            return true;
        }
        
        /// <summary>
        /// 디버그 로그 출력
        /// </summary>
        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log(message);
            }
        }
        
        #endregion
    }
}

