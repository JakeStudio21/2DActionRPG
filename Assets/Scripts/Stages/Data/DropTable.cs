using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 드롭 테이블 ScriptableObject
    /// DropGroup.csv + DropItem.csv 기반
    /// </summary>
    [CreateAssetMenu(fileName = "DropTable", menuName = "Stage/Drop Table")]
    public class DropTable : ScriptableObject
    {
        [Header("그룹 정보")]
        public string DropGroupID;
        public string StageID;
        public DropGroupType GroupType;
        
        [Header("기본 보상")]
        public int Gold;
        public int Exp;
        
        [Header("아이템 드롭")]
        public List<DropItemData> Items = new List<DropItemData>();
        
        /// <summary>
        /// CSV 데이터로부터 생성 (DropGroup.csv)
        /// </summary>
        public void InitializeFromCsv(Dictionary<string, string> csvData)
        {
            DropGroupID = csvData.GetValueOrDefault("DropGroupID", "");
            StageID = csvData.GetValueOrDefault("StageID", "");
            
            // Enum 파싱
            if (System.Enum.TryParse(csvData.GetValueOrDefault("GroupType", "STAGE_CLEAR_FIRST"), out DropGroupType groupType))
                GroupType = groupType;
            
            // 숫자 파싱
            int.TryParse(csvData.GetValueOrDefault("Gold", "0"), out Gold);
            int.TryParse(csvData.GetValueOrDefault("Exp", "0"), out Exp);
        }
        
        /// <summary>
        /// 아이템 데이터 추가 (DropItem.csv)
        /// </summary>
        public void AddItemData(string itemId, int amount, float dropRate)
        {
            var itemData = new DropItemData(itemId, amount, dropRate);
            Items.Add(itemData);
        }
        
        /// <summary>
        /// 드롭 확률 검증
        /// </summary>
        public bool ValidateDropRates()
        {
            float totalRate = 0f;
            foreach (var item in Items)
            {
                totalRate += item.DropRate;
            }
            
            if (totalRate > 1f)
            {
                Debug.LogWarning($"[DropTable] {DropGroupID}: 드롭 확률 합계가 100%를 초과합니다. ({totalRate:P})");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 확률 기반 드롭 아이템 선택 (First/Repeat 구분)
        /// </summary>
        public List<DropItemData> RollDrops(bool isFirstClear = false)
        {
            var droppedItems = new List<DropItemData>();
            
            foreach (var item in Items)
            {
                float dropRate = item.DropRate;
                
                // 첫 클리어 시 보상 확률 증가 (1.5배)
                if (isFirstClear && GroupType == DropGroupType.STAGE_CLEAR_FIRST)
                {
                    dropRate = Mathf.Min(dropRate * 1.5f, 1.0f);
                }
                
                if (Random.value <= dropRate)
                {
                    droppedItems.Add(item);
                }
            }
            
            return droppedItems;
        }
        
        /// <summary>
        /// 보상 정보 요약 반환
        /// </summary>
        public string GetRewardSummary()
        {
            return $"골드: {Gold}, EXP: {Exp}, 아이템: {Items.Count}개";
        }
        
        private void OnValidate()
        {
            ValidateDropRates();
            
            if (!string.IsNullOrEmpty(DropGroupID))
            {
                if (!StageIdValidator.IsValidDropId(DropGroupID))
                {
                    Debug.LogWarning($"[DropTable] 잘못된 DropGroupID 형식: {DropGroupID}");
                }
            }
        }

        /// <summary>
        /// EquipmentData ID로 Pickup 프리팹 스폰
        /// </summary>
        public GameObject SpawnPickupPrefab(string equipmentId, Vector3 position)
        {
            // 1. EquipmentData 로드
            EquipmentData equipment = Resources.Load<EquipmentData>($"Equipment/{equipmentId}");
            if (equipment == null)
            {
                Debug.LogError($"⚠️ [DropTable] EquipmentData 로드 실패: {equipmentId}");
                return null;
            }
            
            // 2. Pickup 프리팹 스폰
            if (equipment.PickupPrefab != null)
            {
                GameObject pickup = Instantiate(equipment.PickupPrefab, position, Quaternion.identity);
                
                // Pickup 컴포넌트에 EquipmentData 연결
                var pickupComponent = pickup.GetComponent<Pickup>();
                if (pickupComponent != null)
                {
                    pickupComponent.SetEquipmentData(equipment);
                }
                
                return pickup;
            }
            else
            {
                Debug.LogWarning($"⚠️ [DropTable] Pickup 프리팹이 설정되지 않음: {equipmentId}");
                return null;
            }
        }
    }
}
