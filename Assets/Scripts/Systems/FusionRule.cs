using UnityEngine;

namespace Systems
{
    /// <summary>
    /// 합성 규칙 데이터 (ScriptableObject)
    /// - 등급별 필요 아이템 개수
    /// - 등급별 합성 비용
    /// </summary>
    [CreateAssetMenu(fileName = "FusionRule", menuName = "Data/Fusion Rule", order = 301)]
    public class FusionRule : ScriptableObject
    {
        [Header("등급별 필요 아이템 개수")]
        [Tooltip("D→C, C→B, B→A 합성 시 필요한 아이템 개수")]
        public int lowTierRequiredCount = 3;
        
        [Tooltip("A→S, S→SS 합성 시 필요한 아이템 개수")]
        public int highTierRequiredCount = 4;
        
        [Tooltip("SS→EX 합성 시 필요한 아이템 개수 (특별)")]
        public int exTierRequiredCount = 5;
        
        [Header("등급별 합성 비용 (골드)")]
        public int costD_to_C = 100;
        public int costC_to_B = 200;
        public int costB_to_A = 500;
        public int costA_to_S = 1000;
        public int costS_to_SS = 2000;
        public int costSS_to_EX = 5000;
        
        /// <summary>
        /// 등급에 따른 필요 아이템 개수 반환
        /// </summary>
        public int GetRequiredCount(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.D => lowTierRequiredCount,   // 3개
                ItemGrade.C => lowTierRequiredCount,   // 3개
                ItemGrade.B => lowTierRequiredCount,   // 3개
                ItemGrade.A => highTierRequiredCount,  // 4개
                ItemGrade.S => highTierRequiredCount,  // 4개
                ItemGrade.SS => exTierRequiredCount,   // 5개 (특별)
                _ => 0
            };
        }
        
        /// <summary>
        /// 등급에 따른 합성 비용 반환
        /// </summary>
        public int GetFusionCost(ItemGrade fromGrade)
        {
            return fromGrade switch
            {
                ItemGrade.D => costD_to_C,
                ItemGrade.C => costC_to_B,
                ItemGrade.B => costB_to_A,
                ItemGrade.A => costA_to_S,
                ItemGrade.S => costS_to_SS,
                ItemGrade.SS => costSS_to_EX,
                _ => 0
            };
        }
        
        /// <summary>
        /// 합성 가능 여부 (EX, TR은 불가)
        /// </summary>
        public bool CanFuseGrade(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.D => true,
                ItemGrade.C => true,
                ItemGrade.B => true,
                ItemGrade.A => true,
                ItemGrade.S => true,
                ItemGrade.SS => true,
                ItemGrade.EX => false, // 의도적 제한
                ItemGrade.TR => false, // 최상위
                _ => false
            };
        }
        
        /// <summary>
        /// 다음 등급 반환
        /// </summary>
        public ItemGrade GetNextGrade(ItemGrade currentGrade)
        {
            return currentGrade switch
            {
                ItemGrade.D => ItemGrade.C,
                ItemGrade.C => ItemGrade.B,
                ItemGrade.B => ItemGrade.A,
                ItemGrade.A => ItemGrade.S,
                ItemGrade.S => ItemGrade.SS,
                ItemGrade.SS => ItemGrade.EX,
                ItemGrade.EX => ItemGrade.TR,
                _ => currentGrade
            };
        }
    }
}

