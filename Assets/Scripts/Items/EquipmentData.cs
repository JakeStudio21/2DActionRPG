using UnityEngine;

/// <summary>
/// 🛡️ 범용 장비 데이터 시스템
/// WeaponInfo + ItemData 통합, 무기/방어구/악세서리 지원
/// </summary>
[CreateAssetMenu(fileName = "NewEquipment", menuName = "Equipment/EquipmentData")]
public class EquipmentData : ScriptableObject
{
    [Header("📋 기본 정보")]
    public string equipmentName;
    public EquipmentType equipmentType;
    public ItemGrade itemGrade;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    
    [Header("🎮 게임 오브젝트")]
    public GameObject equipmentPrefab; // 무기/방어구 프리팹
    
    [Header("⚔️ 무기 전용 설정")]
    [SerializeField] private WeaponType weaponType = WeaponType.None;
    [SerializeField] private float weaponCooldown = 1.0f;
    [SerializeField] private float weaponRange = 5.0f;
    
    [Header("📊 능력치 효과")]
    public int damageBonus = 0;        // 공격력 증가
    public int healthBonus = 0;        // 체력 증가
    public float speedBonus = 0f;      // 이동속도 증가
    public float criticalChance = 0f;  // 크리티컬 확률 증가
    public int defenseBonus = 0;       // 방어력 증가
    
    [Header("🔧 고급 설정")]
    public bool isStackable = false;   // 중복 장착 가능 여부
    public int maxStackCount = 1;      // 최대 중복 수량
    public int sellPrice = 100;        // 판매 가격
    public int buyPrice = 200;         // 구매 가격
    
    // 접근자 프로퍼티
    public WeaponType WeaponType => weaponType;
    public float WeaponCooldown => weaponCooldown;
    public float WeaponRange => weaponRange;
    
    // 무기 타입 확인
    public bool IsWeapon => equipmentType == EquipmentType.Weapon;
    public bool IsArmor => equipmentType == EquipmentType.Armor;
    public bool IsAccessory => equipmentType == EquipmentType.Accessory;
    
    // 무기별 타입 확인
    public bool IsSword => IsWeapon && weaponType == WeaponType.Sword;
    public bool IsBow => IsWeapon && weaponType == WeaponType.Bow;
    public bool IsMagic => IsWeapon && weaponType == WeaponType.Magic;
    
    /// <summary>
    /// 장비 효과 요약 문자열 생성
    /// </summary>
    public string GetStatsDescription()
    {
        var stats = new System.Text.StringBuilder();
        
        if (damageBonus > 0) stats.AppendLine($"공격력 +{damageBonus}");
        if (healthBonus > 0) stats.AppendLine($"체력 +{healthBonus}");
        if (speedBonus > 0) stats.AppendLine($"이동속도 +{speedBonus:F1}");
        if (criticalChance > 0) stats.AppendLine($"크리티컬 +{criticalChance:F1}%");
        if (defenseBonus > 0) stats.AppendLine($"방어력 +{defenseBonus}");
        
        return stats.ToString().TrimEnd();
    }
    
    /// <summary>
    /// 장비 호환성 검사 (특정 클래스 전용 등)
    /// </summary>
    public bool IsCompatibleWith(PlayerClass playerClass)
    {
        // 예: Warrior는 Sword만, Assasin은 Bow만, Wizard는 Magic만
        if (IsWeapon)
        {
            switch (playerClass)
            {
                case PlayerClass.Warrior:
                    return weaponType == WeaponType.Sword;
                case PlayerClass.Assasin:
                    return weaponType == WeaponType.Bow;
                case PlayerClass.Wizard:
                    return weaponType == WeaponType.Magic;
                default:
                    return true; // 범용 클래스는 모든 무기 사용 가능
            }
        }
        
        // 방어구/악세서리는 모든 클래스 호환
        return true;
    }

    /// <summary>
    /// 🔄 기존 시스템 호환성: EquipmentData → WeaponInfo 변환
    /// </summary>
    public WeaponInfo ToWeaponInfo()
    {
        Debug.Log($"�� [EquipmentData] ToWeaponInfo 시작: {equipmentName}");
        Debug.Log($"🔄 [EquipmentData] IsWeapon: {IsWeapon}");
        Debug.Log($"🔄 [EquipmentData] equipmentType: {equipmentType}");
        
        if (!IsWeapon) {
            Debug.LogError($"🔴 [EquipmentData] 무기가 아닙니다: {equipmentType}");
            return null;
        }
        
        // 🔑 equipmentPrefab 확인
        if (equipmentPrefab == null) {
            Debug.LogError($"🔴 [EquipmentData] equipmentPrefab이 null입니다: {equipmentName}");
            return null;
        }
        
        Debug.Log($"🔄 [EquipmentData] equipmentPrefab: {equipmentPrefab.name}");
        
        // 런타임에서 WeaponInfo 생성
        var weaponInfo = ScriptableObject.CreateInstance<WeaponInfo>();
        
        // 🔑 실제 필드명으로 데이터 변환
        weaponInfo.name = equipmentName;
        weaponInfo.weaponPrefab = equipmentPrefab;
        weaponInfo.weaponCooldown = WeaponCooldown;
        weaponInfo.weaponDamage = damageBonus;
        weaponInfo.weaponRange = WeaponRange;
        
        Debug.Log($"✅ [EquipmentData] WeaponInfo 변환 완료: {equipmentName}");
        Debug.Log($"   - 프리팹: {weaponInfo.weaponPrefab.name}");
        Debug.Log($"   - 쿨다운: {weaponInfo.weaponCooldown}");
        Debug.Log($"   - 데미지: {weaponInfo.weaponDamage}");
        Debug.Log($"   - 사거리: {weaponInfo.weaponRange}");
        
        return weaponInfo;
    }
}

/// <summary>
/// 장비 타입 분류
/// </summary>
public enum EquipmentType 
{ 
    Weapon,      // 무기
    Armor,       // 방어구 (헬멧, 갑옷, 신발 등)
    Accessory    // 악세서리 (반지, 목걸이 등)
}

/// <summary>
/// 무기 타입 분류
/// </summary>
public enum WeaponType 
{ 
    None,   // 무기가 아닌 경우
    Sword,  // 검 (Warrior 전용)
    Bow,    // 활 (Assasin 전용)  
    Magic   // 마법 스태프 (Wizard 전용)
}

/// <summary>
/// 🎒 장비 슬롯 분류 (착용 위치)
/// </summary>
public enum EquipmentSlot
{
    // 무기 슬롯
    MainWeapon,    // 주무기
    
    // 방어구 슬롯  
    Helmet,        // 헬멧
    Armor,         // 갑옷
    Boots,         // 신발
    
    // 악세서리 슬롯
    Ring1,         // 반지 1
    Ring2,         // 반지 2
    Necklace,      // 목걸이
    
    // 특수 슬롯
    Shield         // 방패 (Warrior 전용)
}

/// <summary>
/// 플레이어 클래스 (호환성 검사용)
/// </summary>
public enum PlayerClass
{
    Warrior,
    Assasin, 
    Wizard
} 