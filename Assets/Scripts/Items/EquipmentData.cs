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
    public string itemID;               // 🆕 "ITEM_SWORD_1"
    public EquipmentType equipmentType;
    public ItemGrade itemGrade;
    public PlayerClass usableClass = PlayerClass.None;     // 🔧 string → PlayerClass enum으로 변경
    public bool isTradable = true;      // 🆕 거래 가능
    public int requiredLevel = 1;       // 🆕 요구 레벨
    public string resourceID;           // 🆕 "RES_WEAPON_SWORD_1"
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    
    [Header("🎮 게임 오브젝트")]
    public GameObject equipmentPrefab;
    
    [Header("⚔️ 무기 전용 설정")]
    [SerializeField] private WeaponType weaponType = WeaponType.None;
    [SerializeField] private float weaponCooldown = 1.0f;
    [SerializeField] private float weaponRange = 5.0f;
    
    [Header("🛡️ 방어구 전용 설정")]
    [SerializeField] private ArmorType armorType = ArmorType.None;
    
    [Header("⚔️ 무기 전투 스탯")]
    public float attackDamage = 0f;     // 공격 데미지
    public float attackSpeed = 1f;      // 공격 속도  
    public float attackRange = 1f;      // 공격 사거리
    public int attackShape = 1;         // 공격 형태 (1=근접, 2=원거리)
    public float criticalChance = 0f;   // 크리티컬 확률
    public float criticalDamage = 1f;   // 크리티컬 데미지 배수
    
    [Header("🏹 원거리 무기 전용")]
    public string projectileId;         // "ITEM_ARROW_1" 형태 (활/지팡이용)
    
    [Header("🛡️ 방어구 전용 스탯")]
    public float defenseBonus = 0f;     // 방어력 (갑옷)
    public float speedBonus = 0f;       // 이동속도 (신발)
    public float healthBonus = 0f;      // 체력 (방어구 공통)
    
    [Header("💰 상점 시스템")]
    public int buyPrice = 100;          // 상점에서 구매 가격
    public int sellPrice = 50;          // 상점에 판매 가격 (구매가의 50%)
    public bool isLimited = false;      // 한정 판매 여부 (예: 1개만 구매 가능)
    public int quantityLimit = 0;       // 한정 수량 (0이면 무제한)
    
    // 접근자 프로퍼티
    public WeaponType WeaponType => weaponType;
    public float WeaponCooldown => weaponCooldown;
    public float WeaponRange => weaponRange;
    public ArmorType ArmorType => armorType; // 🆕 방어구 타입 프로퍼티
    
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
        
        // 🆕 무기 전용 정보만 표시
        if (IsWeapon)
        {
            stats.AppendLine($"쿨다운: {WeaponCooldown:F1}초");
            stats.AppendLine($"사거리: {WeaponRange:F1}");
            stats.AppendLine($"무기 타입: {WeaponType}");
        }
        
        return stats.ToString().TrimEnd();
    }
    
    /// <summary>
    /// 장비 호환성 검사 (특정 클래스 전용 등)
    /// </summary>
    public bool IsCompatibleWith(PlayerClass playerClass)
    {
        // None 또는 Any인 경우 모든 클래스 호환
        if (usableClass == PlayerClass.None || usableClass == PlayerClass.Any)
            return true;
        
        // 특정 클래스 제한
        if (usableClass != playerClass)
            return false;
        
        // 무기 타입별 추가 호환성 검사
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
                    return true;
            }
        }
        
        return true;
    }

    /// <summary>
    /// 🔄 기존 시스템 호환성: EquipmentData → WeaponInfo 변환
    /// </summary>
    public WeaponInfo ToWeaponInfo()
    {
        Debug.Log($" [EquipmentData] ToWeaponInfo 시작: {equipmentName}");
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
        // ✅ 대체 코드 (기본값 사용)
        weaponInfo.weaponDamage = 0; // 무기 자체 데미지는 별도 시스템에서 관리
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
/// 방어구 타입 분류
/// </summary>
public enum ArmorType
{
    None,    // 방어구가 아닌 경우
    Helmet,  // 헬멧
    Armor,   // 갑옷
    Boots,   // 신발
    Shield   // 방패
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
    None,      // 제한 없음 (모든 클래스 사용 가능)
    Warrior,
    Assasin, 
    Wizard,
    Any        // 명시적으로 모든 클래스 허용
} 