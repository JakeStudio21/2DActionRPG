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
        /// 확률 기반 드롭 아이템 선택
        /// </summary>
        public List<DropItemData> RollDrops()
        {
            var droppedItems = new List<DropItemData>();
            
            foreach (var item in Items)
            {
                if (Random.value <= item.DropRate)
                {
                    droppedItems.Add(item);
                }
            }
            
            return droppedItems;
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
    }
}
