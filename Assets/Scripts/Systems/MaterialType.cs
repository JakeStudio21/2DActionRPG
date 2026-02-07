using System;

/// <summary>
/// 재료 등급 (UI 색상/희귀도 표시용)
/// </summary>
[Serializable]
public enum MaterialRarity
{
    Common,    // 일반 (회색) - 파편
    Uncommon,  // 고급 (초록) - 결정
    Rare,      // 희귀 (파랑) - 코어
    Epic,      // 영웅 (보라)
    Legendary  // 전설 (주황)
}

/// <summary>
/// 재료 타입 enum (장비 카테고리별 분리)
/// </summary>
[Serializable]
public enum MaterialType
{
    None = 0,
    
    // ⚔️ 무기 전용 재료 (Weapon)
    WeaponFragment = 1,   // 무기 강화 파편 (D/C/B 등급 분해/강화)
    WeaponCrystal = 2,    // 무기 강화 결정 (A/S/SS 등급 분해/강화)
    WeaponCore = 3,       // 무기 강화 코어 (EX/TR 등급 분해/강화)
    
    // 🛡️ 방어구 전용 재료 (Armor: Helmet, Armor, Gloves, Boots, Belt)
    ArmorFragment = 4,    // 방어구 강화 파편 (D/C/B 등급 분해/강화)
    ArmorCrystal = 5,     // 방어구 강화 결정 (A/S/SS 등급 분해/강화)
    ArmorCore = 6,        // 방어구 강화 코어 (EX/TR 등급 분해/강화)
    
    // 💍 악세사리 전용 재료 (Accessory: Ring, Necklace)
    AccessoryFragment = 7,  // 악세사리 강화 파편 (D/C/B 등급 분해/강화)
    AccessoryCrystal = 8,   // 악세사리 강화 결정 (A/S/SS 등급 분해/강화)
    AccessoryCore = 9,      // 악세사리 강화 코어 (EX/TR 등급 분해/강화)
    
    // 🔮 합성 재료
    CraftingEssence = 20,   // 제작 정수 (합성 시 사용)
    
    // 💰 범용 재화
    Gold = 100              // 골드
}

/// <summary>
/// 재료 타입 확장 메서드
/// </summary>
public static class MaterialTypeExtensions
{
    /// <summary>
    /// 재료 타입의 한글 표시명 반환
    /// </summary>
    public static string GetDisplayName(this MaterialType type)
    {
        return type switch
        {
            // 무기 재료
            MaterialType.WeaponFragment => "무기 강화 파편",
            MaterialType.WeaponCrystal => "무기 강화 결정",
            MaterialType.WeaponCore => "무기 강화 코어",
            
            // 방어구 재료
            MaterialType.ArmorFragment => "방어구 강화 파편",
            MaterialType.ArmorCrystal => "방어구 강화 결정",
            MaterialType.ArmorCore => "방어구 강화 코어",
            
            // 악세사리 재료
            MaterialType.AccessoryFragment => "악세사리 강화 파편",
            MaterialType.AccessoryCrystal => "악세사리 강화 결정",
            MaterialType.AccessoryCore => "악세사리 강화 코어",
            
            // 기타 재료
            MaterialType.CraftingEssence => "제작 정수",
            MaterialType.Gold => "골드",
            
            _ => "알 수 없는 재료"
        };
    }
    
    /// <summary>
    /// 장비 타입 + 아이템 등급에 따른 재료 타입 반환
    /// (분해 시: 획득할 재료 / 강화 시: 필요한 재료)
    /// </summary>
    /// <param name="equipType">장비 타입 (Weapon/Armor/Accessory)</param>
    /// <param name="grade">아이템 등급 (D~TR)</param>
    /// <returns>해당 장비+등급에 맞는 재료 타입</returns>
    public static MaterialType GetMaterialType(EquipmentType equipType, ItemGrade grade)
    {
        // 등급에 따라 파편/결정/코어 분류
        bool isFragment = grade == ItemGrade.D || grade == ItemGrade.C || grade == ItemGrade.B;
        bool isCrystal = grade == ItemGrade.A || grade == ItemGrade.S || grade == ItemGrade.SS;
        bool isCore = grade == ItemGrade.EX || grade == ItemGrade.TR;
        
        // 장비 타입 + 등급 조합
        return equipType switch
        {
            // 무기
            EquipmentType.Weapon when isFragment => MaterialType.WeaponFragment,
            EquipmentType.Weapon when isCrystal => MaterialType.WeaponCrystal,
            EquipmentType.Weapon when isCore => MaterialType.WeaponCore,
            
            // 방어구
            EquipmentType.Armor when isFragment => MaterialType.ArmorFragment,
            EquipmentType.Armor when isCrystal => MaterialType.ArmorCrystal,
            EquipmentType.Armor when isCore => MaterialType.ArmorCore,
            
            // 악세사리
            EquipmentType.Accessory when isFragment => MaterialType.AccessoryFragment,
            EquipmentType.Accessory when isCrystal => MaterialType.AccessoryCrystal,
            EquipmentType.Accessory when isCore => MaterialType.AccessoryCore,
            
            _ => MaterialType.None
        };
    }
    
    /// <summary>
    /// 재료 타입이 특정 장비 카테고리용인지 확인
    /// </summary>
    public static bool IsForEquipmentType(this MaterialType materialType, EquipmentType equipType)
    {
        return equipType switch
        {
            EquipmentType.Weapon => materialType == MaterialType.WeaponFragment ||
                                   materialType == MaterialType.WeaponCrystal ||
                                   materialType == MaterialType.WeaponCore,
            
            EquipmentType.Armor => materialType == MaterialType.ArmorFragment ||
                                  materialType == MaterialType.ArmorCrystal ||
                                  materialType == MaterialType.ArmorCore,
            
            EquipmentType.Accessory => materialType == MaterialType.AccessoryFragment ||
                                      materialType == MaterialType.AccessoryCrystal ||
                                      materialType == MaterialType.AccessoryCore,
            
            _ => false
        };
    }
    
    /// <summary>
    /// 재료 타입이 파편/결정/코어 중 어떤 등급인지 반환
    /// </summary>
    public static string GetMaterialGrade(this MaterialType materialType)
    {
        return materialType switch
        {
            MaterialType.WeaponFragment or MaterialType.ArmorFragment or MaterialType.AccessoryFragment => "파편",
            MaterialType.WeaponCrystal or MaterialType.ArmorCrystal or MaterialType.AccessoryCrystal => "결정",
            MaterialType.WeaponCore or MaterialType.ArmorCore or MaterialType.AccessoryCore => "코어",
            _ => ""
        };
    }
    
    /// <summary>
    /// MaterialType → MaterialData 변환 (확장 메서드)
    /// </summary>
    /// <param name="type">재료 타입 enum</param>
    /// <returns>MaterialData ScriptableObject 또는 null</returns>
    public static MaterialData GetMaterialData(this MaterialType type)
    {
        return MaterialDatabase.Instance?.GetData(type);
    }
    
    /// <summary>
    /// MaterialType의 아이콘 가져오기 (편의 메서드)
    /// </summary>
    public static UnityEngine.Sprite GetIcon(this MaterialType type)
    {
        return type.GetMaterialData()?.icon;
    }
    
    /// <summary>
    /// MaterialType의 등급별 테두리 색상 가져오기
    /// </summary>
    public static UnityEngine.Color GetBorderColor(this MaterialType type)
    {
        var data = type.GetMaterialData();
        return data != null ? data.GetBorderColor() : UnityEngine.Color.white;
    }
}

