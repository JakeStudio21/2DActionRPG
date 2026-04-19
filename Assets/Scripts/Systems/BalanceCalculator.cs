using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 📐 스탯 예산제 밸런스 계산 엔진
/// 등급 + 슬롯 + 스탯 배분 → 최종 수치 계산
/// </summary>
public static class BalanceCalculator
{
    /// <summary>
    /// 특정 등급/슬롯의 총 예산 계산
    /// </summary>
    /// <param name="grade">장비 등급 (D~TR)</param>
    /// <param name="slot">장비 슬롯 (무기, 헬멧 등)</param>
    /// <param name="settings">밸런스 설정 데이터</param>
    /// <returns>해당 슬롯에 할당된 예산</returns>
    public static float CalculateSlotBudget(ItemGrade grade, EquipmentSlot slot, StatBudgetSettings settings)
    {
        if (settings == null)
        {
            Debug.LogError("❌ [BalanceCalculator] StatBudgetSettings가 null입니다!");
            return 0f;
        }

        // 1. 등급별 총 예산 가져오기
        var gradeBudgets = settings.GradeTotalBudgets;
        if (!gradeBudgets.ContainsKey(grade))
        {
            Debug.LogWarning($"⚠️ [BalanceCalculator] 등급 '{grade}'의 예산을 찾을 수 없습니다.");
            return 0f;
        }
        float totalBudget = gradeBudgets[grade];

        // 2. 슬롯별 가중치 가져오기
        var slotWeights = settings.SlotBudgetWeights;
        if (!slotWeights.ContainsKey(slot))
        {
            Debug.LogWarning($"⚠️ [BalanceCalculator] 슬롯 '{slot}'의 가중치를 찾을 수 없습니다.");
            return 0f;
        }
        float slotWeight = slotWeights[slot];

        // 3. 최종 슬롯 예산 = 총 예산 × 슬롯 가중치
        float slotBudget = totalBudget * slotWeight;

        return slotBudget;
    }

    /// <summary>
    /// 스탯 배분 → 실제 수치 계산
    /// </summary>
    /// <param name="statId">스탯 ID (예: ATK_FLAT, CRIT_RATE)</param>
    /// <param name="allocatedBudget">이 스탯에 할당할 예산</param>
    /// <param name="settings">밸런스 설정 데이터</param>
    /// <returns>최종 스탯 수치</returns>
    public static float CalculateStatValue(string statId, float allocatedBudget, StatBudgetSettings settings)
    {
        if (settings == null)
        {
            Debug.LogError("❌ [BalanceCalculator] StatBudgetSettings가 null입니다!");
            return 0f;
        }

        // 스탯 단가 가져오기
        var statCosts = settings.StatUnitCosts;
        if (!statCosts.ContainsKey(statId))
        {
            Debug.LogWarning($"⚠️ [BalanceCalculator] 스탯 '{statId}'의 단가를 찾을 수 없습니다.");
            return 0f;
        }
        float unitCost = statCosts[statId];

        if (unitCost <= 0)
        {
            Debug.LogWarning($"⚠️ [BalanceCalculator] 스탯 '{statId}'의 단가가 0 이하입니다: {unitCost}");
            return 0f;
        }

        // 최종 수치 = 할당 예산 / 단가
        float statValue = allocatedBudget / unitCost;

        return statValue;
    }

    /// <summary>
    /// 스탯 배분 딕셔너리 → ItemStat 리스트 변환 (일괄 계산)
    /// </summary>
    /// <param name="statBudgetAllocations">Key: StatId, Value: 할당 예산</param>
    /// <param name="settings">밸런스 설정 데이터</param>
    /// <returns>계산된 ItemStat 리스트</returns>
    public static List<ItemStat> CalculateAllStats(Dictionary<string, float> statBudgetAllocations, StatBudgetSettings settings)
    {
        if (settings == null)
        {
            Debug.LogError("❌ [BalanceCalculator] StatBudgetSettings가 null입니다!");
            return new List<ItemStat>();
        }

        List<ItemStat> resultStats = new List<ItemStat>();
        float totalUsedBudget = 0f;


        foreach (var allocation in statBudgetAllocations)
        {
            string statId = allocation.Key;
            float allocatedBudget = allocation.Value;

            if (allocatedBudget <= 0) continue;

            float statValue = CalculateStatValue(statId, allocatedBudget, settings);
            
            // ItemStat 생성
            ItemStat stat = new ItemStat(statId, statValue, GetStatDisplayName(statId));
            resultStats.Add(stat);

            totalUsedBudget += allocatedBudget;
        }


        return resultStats;
    }

    /// <summary>
    /// 장비 타입별 허용 스탯 리스트 가져오기
    /// (Bow/Sword/Staff/Armor/Accessory 구분)
    /// </summary>
    public static List<string> GetAllowedStats(string equipmentKey, StatBudgetSettings settings)
    {
        if (settings == null)
        {
            Debug.LogError("❌ [BalanceCalculator] StatBudgetSettings가 null입니다!");
            return new List<string>();
        }

        var allowedStats = settings.AllowedStatsPerType;
        if (!allowedStats.ContainsKey(equipmentKey))
        {
            Debug.LogWarning($"⚠️ [BalanceCalculator] 장비 키 '{equipmentKey}'의 허용 스탯을 찾을 수 없습니다.");
            return new List<string>();
        }

        return new List<string>(allowedStats[equipmentKey]);
    }

    /// <summary>
    /// EquipmentData → 장비 키 추출 (Bow/Sword/Staff/Armor/Accessory)
    /// </summary>
    public static string GetEquipmentKey(EquipmentData equipment)
    {
        if (equipment == null)
        {
            Debug.LogError("❌ [BalanceCalculator] EquipmentData가 null입니다!");
            return "Unknown";
        }

        switch (equipment.equipmentType)
        {
            case EquipmentType.Weapon:
                // WeaponType으로 Bow/Sword/Magic(Staff) 구분
                switch (equipment.WeaponType)
                {
                    case WeaponType.Bow: return "Bow";
                    case WeaponType.Sword: return "Sword";
                    case WeaponType.Magic: return "Staff"; // Magic → Staff로 매핑
                    default:
                        Debug.LogWarning($"⚠️ [BalanceCalculator] 알 수 없는 무기 타입: {equipment.WeaponType}");
                        return "Sword"; // 기본값
                }

            case EquipmentType.Armor:
                return "Armor";

            case EquipmentType.Accessory:
                return "Accessory";

            default:
                Debug.LogWarning($"⚠️ [BalanceCalculator] 알 수 없는 장비 타입: {equipment.equipmentType}");
                return "Unknown";
        }
    }

    /// <summary>
    /// 예산 사용 현황 검증
    /// </summary>
    public static bool ValidateBudgetUsage(Dictionary<string, float> statBudgetAllocations, float totalBudget, out float usedBudget, out float remainingBudget)
    {
        usedBudget = 0f;
        foreach (var allocation in statBudgetAllocations.Values)
        {
            usedBudget += allocation;
        }

        remainingBudget = totalBudget - usedBudget;
        bool isValid = usedBudget <= totalBudget;

        if (!isValid)
        {
            Debug.LogWarning($"⚠️ [BalanceCalculator] 예산 초과! 사용: {usedBudget}, 한도: {totalBudget}");
        }

        return isValid;
    }

    /// <summary>
    /// StatId → 표시명 변환 (간단한 매핑)
    /// </summary>
    private static string GetStatDisplayName(string statId)
    {
        switch (statId)
        {
            case "ATK_FLAT": return "공격력";
            case "ATK_PERCENT": return "공격력 %";
            case "ASPD": return "공격속도";
            case "CRIT_RATE": return "치명타 확률";
            case "CRIT_DMG": return "치명타 데미지";
            case "DEF_FLAT": return "방어력";
            case "HP_FLAT": return "최대 체력";
            case "HP_REGEN": return "체력 회복";
            case "MOVE_SPEED": return "이동속도";
            case "SKILL_DMG_PERCENT": return "스킬 피해";
            case "COOLDOWN_REDUCTION": return "쿨다운 감소";
            case "DAMAGE_REDUCTION_PERCENT": return "피해 감소";
            case "STATUS_RESIST_ALL": return "상태이상 저항";
            case "EXP_GAIN_PERCENT": return "경험치 획득";
            case "LIFESTEAL": return "흡혈";
            case "ARMOR_PENETRATION": return "방어 관통";
            case "DODGE_CHANCE": return "회피 확률";
            case "BLOCK_CHANCE": return "블록 확률";
            case "PIERCE_DAMAGE_RETENTION": return "관통 데미지 유지율";
            default: return statId;
        }
    }

    /// <summary>
    /// 자동 스탯 배분 (균등 분배, 테스트용)
    /// </summary>
    public static Dictionary<string, float> AutoAllocateBudget(float totalBudget, List<string> statIds, int statCount = 3)
    {
        Dictionary<string, float> allocations = new Dictionary<string, float>();

        if (statIds.Count == 0 || statCount <= 0)
        {
            Debug.LogWarning("⚠️ [BalanceCalculator] 자동 배분 실패: 스탯 리스트가 비어있거나 개수가 0입니다.");
            return allocations;
        }

        // 선택할 스탯 개수 제한
        int actualCount = Mathf.Min(statCount, statIds.Count);
        float budgetPerStat = totalBudget / actualCount;

        for (int i = 0; i < actualCount; i++)
        {
            allocations[statIds[i]] = budgetPerStat;
        }

        return allocations;
    }
}

