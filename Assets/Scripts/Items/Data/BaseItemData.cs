using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 모든 아이템의 기본 데이터 클래스
    /// </summary>
    public abstract class BaseItemData : ScriptableObject
    {
        [Header("기본 정보")]
        public string itemId;
        public string itemName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        
        [Header("픽업 설정")]
        public GameObject pickupPrefab; // 실제 드롭되는 프리팹
        public PickUpType pickupType;
        
        [Header("드롭 설정")]
        [Range(0f, 1f)]
        public float dropRate = 0.1f;
        public int minQuantity = 1;
        public int maxQuantity = 1;
        
        /// <summary>
        /// 아이템 사용/적용
        /// </summary>
        public abstract void UseItem(PlayerDataManager playerManager);
        
        /// <summary>
        /// 아이템 설명 반환
        /// </summary>
        public virtual string GetDescription()
        {
            return description;
        }
        
        /// <summary>
        /// 아이템 ID 검증
        /// </summary>
        public virtual bool ValidateItemId()
        {
            return !string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(itemName);
        }
    }
}
