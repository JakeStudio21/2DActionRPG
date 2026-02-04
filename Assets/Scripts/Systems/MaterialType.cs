using System;

/// <summary>
/// 재료 타입 enum
/// </summary>
[Serializable]
public enum MaterialType
{
    None = 0,
    
    // 분해 재료 (3단계 통합)
    EnhancementFragment = 1,  // 강화 파편 (D/C/B 등급 분해)
    EnhancementCrystal = 2,   // 강화 결정 (A/S/SS 등급 분해)
    EnhancementCore = 3,      // 강화 코어 (EX/TR 등급 분해)
    
    // 강화 재료
    EnhancementStone = 10,    // 강화석 (강화 시 사용)
    
    // 합성 재료
    CraftingEssence = 20,     // 제작 정수 (합성 시 사용)
    
    // 범용 재화
    Gold = 100                // 골드
}

/// <summary>
/// 재료 타입 확장 메서드
/// </summary>
public static class MaterialTypeExtensions
{
    public static string GetDisplayName(this MaterialType type)
    {
        return type switch
        {
            MaterialType.EnhancementFragment => "강화 파편",
            MaterialType.EnhancementCrystal => "강화 결정",
            MaterialType.EnhancementCore => "강화 코어",
            MaterialType.EnhancementStone => "강화석",
            MaterialType.CraftingEssence => "제작 정수",
            MaterialType.Gold => "골드",
            _ => "알 수 없는 재료"
        };
    }
    
    /// <summary>
    /// 아이템 등급에 따른 분해 재료 타입 반환
    /// D/C/B → 파편, A/S/SS → 결정, EX/TR → 코어
    /// </summary>
    public static MaterialType GetFragmentTypeByGrade(ItemGrade grade)
    {
        return grade switch
        {
            // 하급 (D/C/B) → 강화 파편
            ItemGrade.D => MaterialType.EnhancementFragment,
            ItemGrade.C => MaterialType.EnhancementFragment,
            ItemGrade.B => MaterialType.EnhancementFragment,
            
            // 중급 (A/S/SS) → 강화 결정
            ItemGrade.A => MaterialType.EnhancementCrystal,
            ItemGrade.S => MaterialType.EnhancementCrystal,
            ItemGrade.SS => MaterialType.EnhancementCrystal,
            
            // 최상급 (EX/TR) → 강화 코어
            ItemGrade.EX => MaterialType.EnhancementCore,
            ItemGrade.TR => MaterialType.EnhancementCore,
            
            _ => MaterialType.None
        };
    }
}

