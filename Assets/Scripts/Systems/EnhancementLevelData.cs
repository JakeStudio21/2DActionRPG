using UnityEngine;

namespace Systems
{
    /// <summary>
    /// 강화 레벨별 상세 데이터
    /// - 골드 비용, 재료 개수, 성공률, 스탯 증가율, 보너스
    /// </summary>
    [System.Serializable]
    public class EnhancementLevelData
    {
        [Header("기본 정보")]
        [Tooltip("강화 레벨 (0~15)")]
        public int level;
        
        [Header("비용")]
        [Tooltip("필요 골드")]
        public int goldCost;
        
        [Tooltip("필요 재료 개수")]
        public int materialCount;
        
        [Header("성공률 및 실패 처리")]
        [Tooltip("강화 성공 확률 (0.0 ~ 1.0)")]
        [Range(0f, 1f)]
        public float successRate;
        
        [Tooltip("실패 시 처리 방식 (Maintain: 유지, Downgrade: -1, Destroy: 파괴)")]
        public EnhancementFailureType failureType = EnhancementFailureType.Maintain;
        
        [Header("스탯 증가율 (%)")]
        [Tooltip("무기 타입 스탯 증가율 (예: 1.5 = 1.5%)")]
        public float statRateAdd_Weapon = 1.5f;
        
        [Tooltip("방어구 타입 스탯 증가율 (예: 1.2 = 1.2%)")]
        public float statRateAdd_Armor = 1.2f;
        
        [Tooltip("악세서리 타입 스탯 증가율 (예: 0.8 = 0.8%)")]
        public float statRateAdd_Accessory = 0.8f;
        
        [Header("특별 보너스")]
        [Tooltip("보너스 ID (예: BONUS_LV3, BONUS_LV6 등)")]
        public string bonusId = "";
        
        [Tooltip("보너스 설명 (Inspector 표시용)")]
        public string bonusDescription = "";
        
        /// <summary>
        /// 장비 타입에 따른 스탯 증가율 반환
        /// </summary>
        public float GetStatRateAdd(EquipmentType equipType)
        {
            return equipType switch
            {
                EquipmentType.Weapon => statRateAdd_Weapon,
                EquipmentType.Armor => statRateAdd_Armor,
                EquipmentType.Accessory => statRateAdd_Accessory,
                _ => 0f
            };
        }
        
        /// <summary>
        /// 보너스가 있는지 확인
        /// </summary>
        public bool HasBonus()
        {
            return !string.IsNullOrEmpty(bonusId);
        }
    }
}

