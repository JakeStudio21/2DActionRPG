using UnityEngine;

namespace ItemSystem
{
    /// <summary>
    /// 장비 등급 시스템
    /// </summary>
    public enum EquipmentRank
    {
        D = 0,    // 일반 (Common)
        C = 1,    // 고급 (Uncommon)
        B = 2,    // 희귀 (Rare)
        A = 3,    // 영웅 (Heroic)
        S = 4,    // 전설 (Legendary)
        SS = 5,   // 신화 (Mythic)
        EX = 6,   // 고대 (Ancient)
        TR = 7    // 초월 (Transcendent)
    }
    
    /// <summary>
    /// EquipmentRank 확장 메서드
    /// </summary>
    public static class EquipmentRankExtensions
    {
        /// <summary>
        /// FX 풀 태그 반환 (예: "Fx_Equipment_A")
        /// 8등급 모두 지원: D, C, B, A, S, SS, EX, TR
        /// </summary>
        public static string GetFxPoolTag(this EquipmentRank rank)
        {
            return $"Fx_Equipment_{rank}";
        }
        
        /// <summary>
        /// 등급별 컬러 반환
        /// </summary>
        public static Color GetRankColor(this EquipmentRank rank)
        {
            switch (rank)
            {
                case EquipmentRank.D:
                    return new Color(0.7f, 0.7f, 0.7f, 1f);     // 회색 (Gray)
                    
                case EquipmentRank.C:
                    return new Color(0f, 1f, 0f, 1f);           // 초록 (Green)
                    
                case EquipmentRank.B:
                    return new Color(0.2f, 0.5f, 1f, 1f);       // 파랑 (Blue)
                    
                case EquipmentRank.A:
                    return new Color(0.7f, 0.2f, 1f, 1f);       // 보라 (Purple)
                    
                case EquipmentRank.S:
                    return new Color(1f, 0.6f, 0f, 1f);         // 주황 (Orange)
                    
                case EquipmentRank.SS:
                    return new Color(1f, 0.2f, 0.2f, 1f);       // 빨강 (Red)
                    
                case EquipmentRank.EX:
                    return new Color(1f, 0.85f, 0f, 1f);        // 금색 (Gold)
                    
                case EquipmentRank.TR:
                    // 무지개색 (기본: 밝은 시안)
                    // ⚠️ 실제 무지개 효과는 Shader나 ParticleSystem으로 구현 권장
                    return new Color(0.5f, 1f, 1f, 1f);         // 밝은 시안 (Bright Cyan)
                    
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// 등급 이름 반환 (한글)
        /// </summary>
        public static string GetRankName(this EquipmentRank rank)
        {
            switch (rank)
            {
                case EquipmentRank.D: return "일반";
                case EquipmentRank.C: return "고급";
                case EquipmentRank.B: return "희귀";
                case EquipmentRank.A: return "영웅";
                case EquipmentRank.S: return "전설";
                case EquipmentRank.SS: return "신화";
                case EquipmentRank.EX: return "고대";
                case EquipmentRank.TR: return "초월";
                default: return "알 수 없음";
            }
        }
        
        /// <summary>
        /// ItemRarity → EquipmentRank 변환 (8등급 완전 매핑)
        /// </summary>
        public static EquipmentRank FromItemRarity(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return EquipmentRank.D;
                case ItemRarity.Uncommon: return EquipmentRank.C;
                case ItemRarity.Rare: return EquipmentRank.B;
                case ItemRarity.Epic: return EquipmentRank.A;
                case ItemRarity.Legendary: return EquipmentRank.S;
                case ItemRarity.Mythic: return EquipmentRank.SS;           // 신화
                case ItemRarity.Ancient: return EquipmentRank.EX;          // 고대
                case ItemRarity.Transcendent: return EquipmentRank.TR;     // 초월
                default: return EquipmentRank.D;
            }
        }
    }
}

