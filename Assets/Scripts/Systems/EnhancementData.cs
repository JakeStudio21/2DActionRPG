using UnityEngine;

namespace Systems
{
    /// <summary>
    /// 강화 규칙 데이터 (ScriptableObject)
    /// - 등급별/레벨별 성공 확률
    /// - 필요 재료량
    /// - 실패 시 처리 방식
    /// </summary>
    [CreateAssetMenu(fileName = "EnhancementData", menuName = "Data/Enhancement Data", order = 302)]
    public class EnhancementData : ScriptableObject
    {
        [Header("📊 강화 레벨별 상세 테이블")]
        [Tooltip("레벨 0~15 강화 데이터 (골드, 재료, 성공률, 스탯 증가율)")]
        public EnhancementLevelData[] levelTable = new EnhancementLevelData[16]; // 0~15
        
        [Header("강화 레벨 제한")]
        [Tooltip("최대 강화 레벨")]
        public int maxEnhancementLevel = 15;
        
        [Header("등급별 기본 성공률 (%)")]
        [Range(0, 100)] public float baseSuccessRate_D = 95f;
        [Range(0, 100)] public float baseSuccessRate_C = 90f;
        [Range(0, 100)] public float baseSuccessRate_B = 85f;
        [Range(0, 100)] public float baseSuccessRate_A = 80f;
        [Range(0, 100)] public float baseSuccessRate_S = 75f;
        [Range(0, 100)] public float baseSuccessRate_SS = 70f;
        [Range(0, 100)] public float baseSuccessRate_EX = 65f;
        [Range(0, 100)] public float baseSuccessRate_TR = 60f;
        
        [Header("강화 레벨당 성공률 감소 (%)")]
        [Tooltip("+1마다 성공률 감소량")]
        [Range(0, 10)] public float successRateDecreasePerLevel = 3f;
        
        [Header("등급별 레벨당 필요 재료량")]
        [Tooltip("D~B등급: 강화 파편 필요량 (레벨당)")]
        public int fragmentPerLevel_Low = 5;
        
        [Tooltip("A~SS등급: 강화 결정 필요량 (레벨당)")]
        public int fragmentPerLevel_Mid = 10;
        
        [Tooltip("EX~TR등급: 강화 코어 필요량 (레벨당)")]
        public int fragmentPerLevel_High = 20;
        
        [Header("등급별 레벨당 골드 비용")]
        public int goldPerLevel_D = 100;
        public int goldPerLevel_C = 200;
        public int goldPerLevel_B = 500;
        public int goldPerLevel_A = 1000;
        public int goldPerLevel_S = 2000;
        public int goldPerLevel_SS = 5000;
        public int goldPerLevel_EX = 10000;
        public int goldPerLevel_TR = 20000;
        
        [Header("실패 처리 구간")]
        [Tooltip("+0 ~ safeLevel: 실패 시 유지")]
        public int safeLevel = 9;
        
        [Tooltip("safeLevel+1 ~ downgradeLevel: 실패 시 1단계 하락")]
        public int downgradeLevel = 12;
        
        [Tooltip("downgradeLevel+1 ~ max: 실패 시 파괴")]
        // (파괴 구간은 downgradeLevel+1부터 자동)
        
        /// <summary>
        /// 등급별 기본 성공률 반환
        /// </summary>
        public float GetBaseSuccessRate(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.D => baseSuccessRate_D,
                ItemGrade.C => baseSuccessRate_C,
                ItemGrade.B => baseSuccessRate_B,
                ItemGrade.A => baseSuccessRate_A,
                ItemGrade.S => baseSuccessRate_S,
                ItemGrade.SS => baseSuccessRate_SS,
                ItemGrade.EX => baseSuccessRate_EX,
                ItemGrade.TR => baseSuccessRate_TR,
                _ => 50f
            };
        }
        
        /// <summary>
        /// 현재 강화 레벨 기준 성공률 계산
        /// </summary>
        public float CalculateSuccessRate(ItemGrade grade, int currentLevel)
        {
            float baseRate = GetBaseSuccessRate(grade);
            float penalty = successRateDecreasePerLevel * currentLevel;
            float finalRate = Mathf.Max(baseRate - penalty, 1f); // 최소 1%
            return finalRate;
        }
        
        /// <summary>
        /// 필요 재료 타입 반환
        /// </summary>
        public MaterialType GetRequiredMaterialType(EquipmentType equipType, ItemGrade grade)
        {
            return MaterialTypeExtensions.GetMaterialType(equipType, grade);
        }
        
        /// <summary>
        /// 필요 재료 개수 반환
        /// </summary>
        public int GetRequiredMaterialAmount(ItemGrade grade, int targetLevel)
        {
            int baseAmount = grade switch
            {
                ItemGrade.D => fragmentPerLevel_Low,
                ItemGrade.C => fragmentPerLevel_Low,
                ItemGrade.B => fragmentPerLevel_Low,
                ItemGrade.A => fragmentPerLevel_Mid,
                ItemGrade.S => fragmentPerLevel_Mid,
                ItemGrade.SS => fragmentPerLevel_Mid,
                ItemGrade.EX => fragmentPerLevel_High,
                ItemGrade.TR => fragmentPerLevel_High,
                _ => 0
            };
            
            // 레벨이 높을수록 재료 증가 (예: +10 이상은 2배)
            if (targetLevel >= 10)
            {
                baseAmount = (int)(baseAmount * 1.5f);
            }
            
            return baseAmount;
        }
        
        /// <summary>
        /// 필요 골드 계산
        /// </summary>
        public int GetRequiredGold(ItemGrade grade, int targetLevel)
        {
            int baseGold = grade switch
            {
                ItemGrade.D => goldPerLevel_D,
                ItemGrade.C => goldPerLevel_C,
                ItemGrade.B => goldPerLevel_B,
                ItemGrade.A => goldPerLevel_A,
                ItemGrade.S => goldPerLevel_S,
                ItemGrade.SS => goldPerLevel_SS,
                ItemGrade.EX => goldPerLevel_EX,
                ItemGrade.TR => goldPerLevel_TR,
                _ => 0
            };
            
            // 레벨에 따른 골드 증가
            return baseGold * targetLevel;
        }
        
        /// <summary>
        /// 실패 시 처리 방식 반환
        /// </summary>
        public EnhancementFailureType GetFailureType(int currentLevel)
        {
            if (currentLevel <= safeLevel)
                return EnhancementFailureType.Maintain;
            else if (currentLevel <= downgradeLevel)
                return EnhancementFailureType.Downgrade;
            else
                return EnhancementFailureType.Destroy;
        }
        
        // ========================================
        // ⭐ 레벨별 테이블 접근 메서드
        // ========================================
        
        /// <summary>
        /// 특정 레벨의 강화 데이터 반환
        /// </summary>
        public EnhancementLevelData GetLevelData(int level)
        {
            if (levelTable == null || level < 0 || level >= levelTable.Length)
            {
                Debug.LogError($"[EnhancementData] 잘못된 레벨: {level}");
                return null;
            }
            
            return levelTable[level];
        }
        
        /// <summary>
        /// 레벨별 골드 비용 (테이블 우선)
        /// </summary>
        public int GetRequiredGoldFromTable(int targetLevel)
        {
            var levelData = GetLevelData(targetLevel);
            if (levelData != null)
            {
                return levelData.goldCost;
            }
            
            // Fallback: 기존 방식
            return GetRequiredGold(ItemGrade.A, targetLevel);
        }
        
        /// <summary>
        /// 레벨별 재료 개수 (테이블 우선)
        /// </summary>
        public int GetRequiredMaterialCountFromTable(int targetLevel)
        {
            var levelData = GetLevelData(targetLevel);
            if (levelData != null)
            {
                return levelData.materialCount;
            }
            
            // Fallback: 기존 방식
            return GetRequiredMaterialAmount(ItemGrade.A, targetLevel);
        }
        
        /// <summary>
        /// 레벨별 성공률 (테이블 우선)
        /// </summary>
        public float GetSuccessRateFromTable(int targetLevel)
        {
            var levelData = GetLevelData(targetLevel);
            if (levelData != null)
            {
                return levelData.successRate * 100f; // 0.0~1.0 → 0~100%
            }
            
            // Fallback: 기존 방식
            return CalculateSuccessRate(ItemGrade.A, targetLevel);
        }
        
        /// <summary>
        /// 레벨별 스탯 증가율 반환
        /// </summary>
        public float GetStatRateAdd(int level, EquipmentType equipType)
        {
            var levelData = GetLevelData(level);
            if (levelData != null)
            {
                return levelData.GetStatRateAdd(equipType);
            }
            
            return 0f;
        }
        
        /// <summary>
        /// 누적 스탯 증가율 계산 (0 ~ targetLevel)
        /// </summary>
        public float GetTotalStatBonus(int targetLevel, EquipmentType equipType)
        {
            float total = 0f;
            
            for (int i = 1; i <= targetLevel; i++)
            {
                total += GetStatRateAdd(i, equipType);
            }
            
            return total;
        }
        
        /// <summary>
        /// 보너스 확인
        /// </summary>
        public bool HasBonus(int level)
        {
            var levelData = GetLevelData(level);
            return levelData != null && levelData.HasBonus();
        }
        
        /// <summary>
        /// 보너스 ID 반환
        /// </summary>
        public string GetBonusId(int level)
        {
            var levelData = GetLevelData(level);
            return levelData?.bonusId ?? "";
        }
    }
    
    /// <summary>
    /// 강화 실패 시 처리 타입
    /// </summary>
    public enum EnhancementFailureType
    {
        Maintain,   // 유지 (레벨 변화 없음)
        Downgrade,  // 1단계 하락
        Destroy     // 아이템 파괴
    }
}

