using System;
using UnityEngine;

namespace Systems
{
    /// <summary>
    /// 강화 성장 곡선 테이블
    /// - 레벨별 스탯 증가율
    /// - 장비 타입별 곡선 그룹
    /// 
    /// ⭐ 비용/확률/보너스는 포함하지 않음 (EnhanceLevelTableSO에서 관리)
    /// </summary>
    [CreateAssetMenu(fileName = "EnhanceCurveTable", menuName = "Data/Enhancement/Curve Table", order = 301)]
    public class EnhanceCurveTableSO : ScriptableObject
    {
        [System.Serializable]
        public class LevelRange
        {
            [Header("레벨 구간")]
            [Tooltip("시작 강화 레벨 (예: 1)")]
            public int startLevel;
            
            [Tooltip("끝 강화 레벨 (예: 5)")]
            public int endLevel;
            
            [Header("스탯 증가율")]
            [Tooltip("레벨당 스탯 증가율 (%) (예: 1.5 = 1.5%)")]
            public float statRateAdd;
            
            /// <summary>
            /// 특정 레벨이 이 구간에 포함되는지 확인
            /// </summary>
            public bool Contains(int level)
            {
                return level >= startLevel && level <= endLevel;
            }
        }
        
        [System.Serializable]
        public class CurveGroup
        {
            [Header("그룹 정보")]
            [Tooltip("곡선 그룹 ID (예: CURVE_WEAPON, CURVE_ARMOR)")]
            public string groupId;
            
            [Tooltip("인스펙터 표시용 이름 (예: 무기 곡선, 방어구 곡선)")]
            public string curveName;
            
            [Header("레벨별 증가율")]
            [Tooltip("구간별 스탯 증가율 (예: 1~5레벨 1.5%, 6~10레벨 2.0%)")]
            public LevelRange[] levelRanges;
            
            /// <summary>
            /// 특정 레벨의 스탯 증가율 반환
            /// </summary>
            public float GetStatRateAdd(int level)
            {
                if (levelRanges == null) return 0f;
                
                foreach (var range in levelRanges)
                {
                    if (range.Contains(level))
                    {
                        return range.statRateAdd;
                    }
                }
                
                return 0f;
            }
        }
        
        [Header("📈 강화 성장 곡선")]
        [Tooltip("장비 타입별 곡선 그룹 (예: CURVE_WEAPON, CURVE_ARMOR, CURVE_ACCESSORY)")]
        public CurveGroup[] curveGroups;
        
        /// <summary>
        /// 특정 그룹의 특정 레벨 스탯 증가율 반환
        /// </summary>
        public float GetStatRateAdd(string groupId, int level)
        {
            if (curveGroups == null) return 0f;
            
            var group = Array.Find(curveGroups, g => g.groupId == groupId);
            if (group == null)
            {
                Debug.LogWarning($"[EnhanceCurveTableSO] 곡선 그룹을 찾을 수 없습니다: {groupId}");
                return 0f;
            }
            
            return group.GetStatRateAdd(level);
        }
        
        /// <summary>
        /// 누적 스탯 증가율 계산 (0 ~ targetLevel)
        /// </summary>
        public float GetTotalStatBonus(string groupId, int targetLevel)
        {
            if (curveGroups == null) return 0f;
            
            var group = Array.Find(curveGroups, g => g.groupId == groupId);
            if (group == null) return 0f;
            
            float total = 0f;
            for (int i = 1; i <= targetLevel; i++)
            {
                total += group.GetStatRateAdd(i);
            }
            
            return total;
        }
        
        /// <summary>
        /// 특정 그룹이 존재하는지 확인
        /// </summary>
        public bool HasCurveGroup(string groupId)
        {
            return Array.Exists(curveGroups, g => g.groupId == groupId);
        }
        
        /// <summary>
        /// 모든 그룹 ID 목록 반환 (디버그용)
        /// </summary>
        public string[] GetAllGroupIds()
        {
            if (curveGroups == null) return new string[0];
            
            string[] ids = new string[curveGroups.Length];
            for (int i = 0; i < curveGroups.Length; i++)
            {
                ids[i] = curveGroups[i].groupId;
            }
            return ids;
        }
    }
}

