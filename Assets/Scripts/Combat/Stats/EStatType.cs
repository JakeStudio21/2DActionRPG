/// <summary>
/// 스탯 타입 Enum (CSV StatId와 1:1 매핑)
/// 최적화: string 비교 대신 int 비교 사용
/// </summary>
public enum EStatType
{
    None = 0,
    
    // === 공격 관련 ===
    ATK_FLAT = 1,               // 공격력 고정값
    ATK_PERCENT = 2,            // 공격력 %
    ASPD = 3,                   // 공격속도 %
    
    // === 치명타 관련 ===
    CRIT_RATE = 10,             // 치명 확률
    CRIT_DMG = 11,              // 치명 피해 %
    
    // === 스킬 관련 ===
    SKILL_DMG_PERCENT = 20,     // 스킬 피해 증가 %
    COOLDOWN_REDUCTION = 21,    // 쿨다운 감소 %
    
    // === 방어 관련 ===
    DEF_FLAT = 30,              // 방어력 고정값
    DAMAGE_REDUCTION_PERCENT = 31, // 받는 피해 감소 %
    
    // === 체력 관련 ===
    HP_FLAT = 40,               // 최대 체력
    HP_REGEN = 41,              // 초당 체력 회복
    
    // === 저항 관련 ===
    STATUS_RESIST_ALL = 50,     // 상태이상 저항
    
    // === 기타 ===
    MOVE_SPEED = 60,            // 이동속도
    EXP_GAIN_PERCENT = 70,      // 경험치 획득 증가 %
    
    // === Phase 1: 패시브 스킬용 추가 스탯 ===
    DODGE_CHANCE = 80,          // 회피 확률 %
    BLOCK_CHANCE = 81,          // 블록 확률 %
    LIFESTEAL = 82,             // 흡혈 %
    ARMOR_PENETRATION = 83,     // 방어구 관통 %
    PIERCE_DAMAGE_RETENTION = 84, // 관통 시 데미지 유지율 %
}

/// <summary>
/// EStatType 확장 메서드
/// </summary>
public static class EStatTypeExtensions
{
    /// <summary>
    /// CSV StatId → EStatType 변환
    /// </summary>
    public static EStatType ToStatType(this string statId)
    {
        switch (statId)
        {
            case "ATK_FLAT": return EStatType.ATK_FLAT;
            case "ATK_PERCENT": return EStatType.ATK_PERCENT;
            case "ASPD": return EStatType.ASPD;
            case "CRIT_RATE": return EStatType.CRIT_RATE;
            case "CRIT_DMG": return EStatType.CRIT_DMG;
            case "SKILL_DMG_PERCENT": return EStatType.SKILL_DMG_PERCENT;
            case "COOLDOWN_REDUCTION": return EStatType.COOLDOWN_REDUCTION;
            case "DEF_FLAT": return EStatType.DEF_FLAT;
            case "DAMAGE_REDUCTION_PERCENT": return EStatType.DAMAGE_REDUCTION_PERCENT;
            case "HP_FLAT": return EStatType.HP_FLAT;
            case "HP_REGEN": return EStatType.HP_REGEN;
            case "STATUS_RESIST_ALL": return EStatType.STATUS_RESIST_ALL;
            case "MOVE_SPEED": return EStatType.MOVE_SPEED;
            case "EXP_GAIN_PERCENT": return EStatType.EXP_GAIN_PERCENT;
            case "DODGE_CHANCE": return EStatType.DODGE_CHANCE;
            case "BLOCK_CHANCE": return EStatType.BLOCK_CHANCE;
            case "LIFESTEAL": return EStatType.LIFESTEAL;
            case "ARMOR_PENETRATION": return EStatType.ARMOR_PENETRATION;
            case "PIERCE_DAMAGE_RETENTION": return EStatType.PIERCE_DAMAGE_RETENTION;
            default: return EStatType.None;
        }
    }
    
    /// <summary>
    /// EStatType → CSV StatId 변환
    /// </summary>
    public static string ToStatId(this EStatType statType)
    {
        return statType.ToString();
    }
}

