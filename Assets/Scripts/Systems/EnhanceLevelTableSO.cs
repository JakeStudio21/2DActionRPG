using UnityEngine;

namespace Systems
{
    /// <summary>
    /// 강화 레벨별 시도 규칙 테이블
    /// - 비용 (골드, 재료)
    /// - 성공 확률
    /// - 보너스 트리거
    /// 
    /// ⭐ 스탯 증가량은 포함하지 않음 (EnhanceCurveTableSO에서 관리)
    /// </summary>
    [CreateAssetMenu(fileName = "EnhanceLevelTable", menuName = "Data/Enhancement/Level Table", order = 300)]
    public class EnhanceLevelTableSO : ScriptableObject
    {
        [System.Serializable]
        public class LevelData
        {
            [Header("레벨 정보")]
            [Tooltip("강화 단계 (0~15)")]
            public int level;
            
            [Header("비용")]
            [Tooltip("소모 골드")]
            public int goldCost;
            
            [Tooltip("소모 강화 재료 개수")]
            public int materialCount;
            
            [Header("확률 및 실패 처리")]
            [Tooltip("성공 확률 (0.0 ~ 1.0)")]
            [Range(0f, 1f)]
            public float successRate;
            
            [Tooltip("실패 시 처리 방식 (Maintain: 유지, Downgrade: -1, Destroy: 파괴)")]
            public EnhancementFailureType failureType = EnhancementFailureType.Maintain;
            
            [Header("보너스")]
            [Tooltip("구간 보너스 ID (예: BONUS_LV3, BONUS_LV6 등, 없으면 비워둠)")]
            public string bonusId = "";
            
            [Tooltip("보너스 설명 (Inspector 표시용)")]
            public string bonusDescription = "";
            
            /// <summary>
            /// 보너스가 있는지 확인
            /// </summary>
            public bool HasBonus()
            {
                return !string.IsNullOrEmpty(bonusId);
            }
        }
        
        [Header("📊 강화 레벨별 시도 규칙")]
        [Tooltip("Lv 0~15 강화 데이터 (골드, 재료, 성공률, 보너스)")]
        public LevelData[] levelTable = new LevelData[16]; // 0~15
        
        [Header("⚙️ 설정")]
        [Tooltip("최대 강화 레벨")]
        public int maxEnhancementLevel = 15;
        
        /// <summary>
        /// 특정 레벨의 데이터 반환
        /// </summary>
        public LevelData GetLevelData(int level)
        {
            if (levelTable == null || level < 0 || level >= levelTable.Length)
            {
                Debug.LogError($"[EnhanceLevelTableSO] 잘못된 레벨: {level}");
                return null;
            }
            
            return levelTable[level];
        }
        
        /// <summary>
        /// 골드 비용 조회
        /// </summary>
        public int GetGoldCost(int level)
        {
            var data = GetLevelData(level);
            return data?.goldCost ?? 0;
        }
        
        /// <summary>
        /// 재료 개수 조회
        /// </summary>
        public int GetMaterialCount(int level)
        {
            var data = GetLevelData(level);
            return data?.materialCount ?? 0;
        }
        
        /// <summary>
        /// 성공 확률 조회 (0~100%)
        /// </summary>
        public float GetSuccessRate(int level)
        {
            var data = GetLevelData(level);
            return data != null ? data.successRate * 100f : 0f;
        }
        
        /// <summary>
        /// 보너스 확인
        /// </summary>
        public bool HasBonus(int level)
        {
            var data = GetLevelData(level);
            return data != null && data.HasBonus();
        }
        
        /// <summary>
        /// 보너스 ID 반환
        /// </summary>
        public string GetBonusId(int level)
        {
            var data = GetLevelData(level);
            return data?.bonusId ?? "";
        }
        
        /// <summary>
        /// ⭐ 실패 처리 규칙 반환
        /// </summary>
        public EnhancementFailureType GetFailureType(int level)
        {
            var data = GetLevelData(level);
            return data?.failureType ?? EnhancementFailureType.Maintain;
        }
    }
}

