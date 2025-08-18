using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 체력 회복 아이템 데이터
    /// </summary>
    [CreateAssetMenu(fileName = "HealthItem", menuName = "Items/Health Item")]
    public class HealthItemData : BaseItemData
    {
        [Header("체력 회복 설정")]
        public int healAmount = 50;
        public bool isPercentageHeal = false; // true면 최대 체력의 퍼센트
        [Range(0f, 1f)]
        public float healPercentage = 0.3f; // 30%
        
        private void OnValidate()
        {
            // 기본값 설정
            if (string.IsNullOrEmpty(itemId))
                itemId = $"HEALTH_{healAmount}";
            if (string.IsNullOrEmpty(itemName))
                itemName = $"체력 회복 {healAmount}";
            if (string.IsNullOrEmpty(description))
                description = $"체력을 {healAmount} 회복합니다.";
            
            // pickupType = PickUpType.HealthGlobe; // 🚫 임시 제거
        }
        
        public override void UseItem(PlayerDataManager playerManager)
        {
            var playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null)
            {
                int healValue = isPercentageHeal ? 
                    Mathf.RoundToInt(playerHealth.MaxHealth * healPercentage) : 
                    healAmount;
                
                // 🆕 직접 체력 회복 로직 구현
                ApplyHealing(playerHealth, healValue);
                
                Debug.Log($"❤️ [HealthItem] 체력 회복: +{healValue}");
            }
        }
        
        /// <summary>
        /// 체력 회복 적용
        /// </summary>
        private void ApplyHealing(PlayerHealth playerHealth, int healAmount)
        {
            // PlayerHealth의 private 필드에 직접 접근할 수 없으므로
            // Reflection을 사용하거나 public 메서드를 추가해야 함
            
            // 방법 1: PlayerHealth에 public 메서드 추가 (권장)
            // playerHealth.HealPlayerAmount(healAmount);
            
            // 방법 2: 현재는 기본 HealPlayer() 사용 (임시)
            for (int i = 0; i < healAmount; i++)
            {
                playerHealth.HealPlayer();
            }
        }
        
        public override string GetDescription()
        {
            if (isPercentageHeal)
            {
                return $"{description}\n회복량: 최대 체력의 {healPercentage:P0}";
            }
            else
            {
                return $"{description}\n회복량: +{healAmount}";
            }
        }
    }
}
