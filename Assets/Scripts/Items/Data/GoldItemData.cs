using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 골드 아이템 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "GoldItem", menuName = "Items/Gold Item")]
    public class GoldItemData : BaseItemData
    {
        [Header("골드 설정")]
        public int goldAmount = 1;
        public bool isRandomAmount = false;
        public int minGoldAmount = 1;
        public int maxGoldAmount = 10;
        
        private void OnValidate()
        {
            // 기본값 설정
            if (string.IsNullOrEmpty(itemId))
                itemId = $"GOLD_{goldAmount}";
            if (string.IsNullOrEmpty(itemName))
                itemName = $"골드 {goldAmount}개";
            if (string.IsNullOrEmpty(description))
                description = $"골드를 {goldAmount}개 획득합니다.";
            
            // pickupType = PickUpType.GoldCoin; // 🚫 임시 제거
        }
        
        public override void UseItem(PlayerDataManager playerManager)
        {
            if (playerManager != null)
            {
                int amount = isRandomAmount ? 
                    Random.Range(minGoldAmount, maxGoldAmount + 1) : 
                    goldAmount;
                
                playerManager.AddGold(amount);
                
                Dbg.Log($"💰 [GoldItem] 골드 획득: +{amount}");
            }
        }
        
        public override string GetDescription()
        {
            if (isRandomAmount)
            {
                return $"{description}\n골드: {minGoldAmount}~{maxGoldAmount}";
            }
            else
            {
                return $"{description}\n골드: +{goldAmount}";
            }
        }
    }
}
