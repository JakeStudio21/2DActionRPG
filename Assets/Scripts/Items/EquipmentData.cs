using System.Collections.Generic;
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
    
    [Header("💍 악세사리 전용 설정")]
    [SerializeField] private AccessoryType accessoryType = AccessoryType.None;
    
    [Header("📊 통합 스탯 시스템 (예산제)")]
    [Tooltip("이 장비가 어느 슬롯에 장착되는지 (예산 계산용)")]
    public EquipmentSlot equipmentSlot = EquipmentSlot.MainWeapon;
    
    [Tooltip("모든 스탯을 담는 통합 리스트 (StatDefinition.csv 기반)")]
    public List<ItemStat> baseStats = new List<ItemStat>();
    
    [Header("🏹 원거리 무기 전용")]
    public string projectileId;         // "ITEM_ARROW_1" 형태 (활/지팡이용)
    
    [Header("🎲 동적 스탯 생성 (Random Stats)")]
    [Tooltip("이 장비에 부여될 수 있는 부옵션 스탯 풀 (EquipmentGenerator가 참조)")]
    public List<EStatType> availableSubStats = new List<EStatType>();
    
    [Header("⚠️ 레거시 (사용 안 함 - CSV 자동 로드)")]
    [HideInInspector] [Tooltip("⚠️ 사용 안 함: GradeSlotBudgetWeighted.csv 참조")]
    public float baseBudget = 100f;
    
    [HideInInspector] [Tooltip("⚠️ 사용 안 함: EquipmentSlotBudget.csv 참조")]
    public float mainStatWeight = 1.0f;
    
    [HideInInspector] [Tooltip("⚠️ 사용 안 함: EquipmentSlotBudget.csv 참조")]
    public float subStatWeight = 0.3f;
    
    [Header("⚔️ 공격 사거리/형태 (비스탯 속성)")]
    [Tooltip("공격 사거리")]
    public float attackRange = 1f;
    [Tooltip("공격 형태 (1=근접, 2=원거리)")]
    public int attackShape = 1;
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 🔧 레거시 호환 프로퍼티 (기존 코드용, 읽기/쓰기 지원)
    // Get: baseStats에서 자동 계산
    // Set: baseStats에 자동 저장
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    /// <summary>공격력 (ATK_FLAT)</summary>
    public float attackDamage
    {
        get => GetStatValue("ATK_FLAT");
        set => SetStatValue("ATK_FLAT", value, "공격력");
    }
    
    /// <summary>공격속도 배율 (ASPD + 1.0, 저장 시 -1.0)</summary>
    public float attackSpeed
    {
        get => 1f + (GetStatValue("ASPD") / 100f); // CSV: 12.5 → 코드: 1.125
        set => SetStatValue("ASPD", (value - 1f) * 100f, "공격속도"); // 코드: 1.125 → CSV: 12.5
    }
    
    /// <summary>크리티컬 확률 (CRIT_RATE)</summary>
    public float criticalChance
    {
        get => GetStatValue("CRIT_RATE") / 100f; // CSV: 1 → 코드: 0.01
        set => SetStatValue("CRIT_RATE", value * 100f, "크리티컬 확률"); // 코드: 0.01 → CSV: 1
    }
    
    /// <summary>크리티컬 데미지 배수 (CRIT_DMG + 1.0, 저장 시 -1.0)</summary>
    public float criticalDamage
    {
        get => 1f + (GetStatValue("CRIT_DMG") / 100f); // CSV: 50 → 코드: 1.5
        set => SetStatValue("CRIT_DMG", (value - 1f) * 100f, "크리티컬 데미지"); // 코드: 1.5 → CSV: 50
    }
    
    /// <summary>방어력 (DEF_FLAT)</summary>
    public float defenseBonus
    {
        get => GetStatValue("DEF_FLAT");
        set => SetStatValue("DEF_FLAT", value, "방어력");
    }
    
    /// <summary>이동속도 보너스 (MOVE_SPEED)</summary>
    public float speedBonus
    {
        get => GetStatValue("MOVE_SPEED") / 100f; // CSV: 10 → 코드: 0.1
        set => SetStatValue("MOVE_SPEED", value * 100f, "이동속도"); // 코드: 0.1 → CSV: 10
    }
    
    /// <summary>체력 보너스 (HP_FLAT)</summary>
    public float healthBonus
    {
        get => GetStatValue("HP_FLAT");
        set => SetStatValue("HP_FLAT", value, "체력");
    }
    
    [Header("💰 상점 시스템")]
    public int buyPrice = 100;          // 상점에서 구매 가격
    public int sellPrice = 50;          // 상점에 판매 가격 (구매가의 50%)
    public bool isLimited = false;      // 한정 판매 여부 (예: 1개만 구매 가능)
    public int quantityLimit = 0;       // 한정 수량 (0이면 무제한)
    
    [Header("⚡ 강화 시스템")]
    [Tooltip("강화 성장 곡선 ID (예: CURVE_WEAPON, CURVE_ARMOR, CURVE_ACCESSORY)")]
    public string enhancementCurveGroupId = "CURVE_STANDARD"; // ⭐ 이 장비가 사용하는 성장 곡선
    
    [Header("드롭/픽업 연결")]
    [SerializeField] private GameObject pickupPrefab; // 실제 드롭되는 프리팹
    
    // 접근자 프로퍼티
    public WeaponType WeaponType => weaponType;
    public float WeaponCooldown => weaponCooldown;
    public float WeaponRange => weaponRange;
    public ArmorType ArmorType => armorType; // 🆕 방어구 타입 프로퍼티
    public AccessoryType AccessoryType => accessoryType; // 🆕 악세사리 타입 프로퍼티
    
    /// <summary>
    /// Pickup 프리팹 반환
    /// </summary>
    public GameObject PickupPrefab => pickupPrefab;
    
    /// <summary>
    /// Pickup 프리팹 설정
    /// </summary>
    public void SetPickupPrefab(GameObject prefab)
    {
        pickupPrefab = prefab;
    }

    // 무기 타입 확인
    public bool IsWeapon => equipmentType == EquipmentType.Weapon;
    public bool IsArmor => equipmentType == EquipmentType.Armor;
    public bool IsAccessory => equipmentType == EquipmentType.Accessory;
    
    // 무기별 타입 확인
    public bool IsSword => IsWeapon && weaponType == WeaponType.Sword;
    public bool IsBow => IsWeapon && weaponType == WeaponType.Bow;
    public bool IsMagic => IsWeapon && weaponType == WeaponType.Magic;
    
    /// <summary>
    /// 📊 baseStats에서 특정 스탯 값 가져오기
    /// </summary>
    public float GetStatValue(string statId)
    {
        ItemStat stat = baseStats.Find(s => s.statId == statId);
        return stat != null ? stat.value : 0f;
    }
    
    /// <summary>
    /// 📊 baseStats에 스탯 추가 또는 업데이트
    /// </summary>
    public void SetStatValue(string statId, float value, string displayName = "")
    {
        ItemStat stat = baseStats.Find(s => s.statId == statId);
        if (stat != null)
        {
            stat.value = value;
            if (!string.IsNullOrEmpty(displayName))
                stat.displayName = displayName;
        }
        else
        {
            baseStats.Add(new ItemStat(statId, value, displayName));
        }
    }
    
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
    
    /// <summary>
    /// ⚔️ baseStats → StatModifier 변환 (Phase A 완전 전환용)
    /// EquipmentInstance 없이도 StatModifier를 얻을 수 있음
    /// </summary>
    public List<StatModifier> GetStatModifiers()
    {
        List<StatModifier> modifiers = new List<StatModifier>();
        
        if (baseStats == null || baseStats.Count == 0)
        {
            return modifiers;
        }
        
        string source = $"{equipmentName}";
        
        foreach (var itemStat in baseStats)
        {
            EStatType statType = ConvertStatIdToEnum(itemStat.statId);
            if (statType == EStatType.None)
            {
                Debug.LogWarning($"[EquipmentData] 알 수 없는 StatId: {itemStat.statId}");
                continue;
            }
            
            var definition = StatDefinitions.Get(statType);
            if (definition == null)
            {
                Debug.LogWarning($"[EquipmentData] StatDefinition을 찾을 수 없습니다: {statType}");
                continue;
            }
            
            float value = itemStat.value;
            
            // StatDefinition에 따라 Percent 단위 변환 (CSV: 30 → 코드: 0.30)
            if (definition.unit == StatUnit.Percent)
            {
                value /= 100f;
            }
            
            // StatModifier 생성자는 4개 인자만 받음 (stackRule, applyPhase는 StatDefinitions에서 자동 로드)
            modifiers.Add(new StatModifier(
                statType,
                value,
                definition.unit,
                source
            ));
        }
        
        return modifiers;
    }
    
    /// <summary>
    /// StatId 문자열 → EStatType 변환
    /// </summary>
    private EStatType ConvertStatIdToEnum(string statId)
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
            default: return EStatType.None;
        }
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
    Helmet,  // 투구
    Armor,   // 상의 (갑옷)
    Gloves,  // 장갑
    Boots,   // 신발
    Belt     // 허리띠
}

/// <summary>
/// 악세사리 타입 분류
/// </summary>
public enum AccessoryType
{
    None,      // 악세사리가 아닌 경우
    Ring,      // 반지
    Necklace   // 목걸이
}

/// <summary>
/// 🎒 장비 슬롯 분류 (착용 위치)
/// </summary>
public enum EquipmentSlot
{
    // 무기 슬롯
    MainWeapon,    // 주무기 (공격력)
    
    // 방어구 슬롯  
    Helmet,        // 투구 (방어력)
    Armor,         // 상의 (방어력)
    Gloves,        // 장갑 (공격력)
    Boots,         // 신발 (이동속도)
    Belt,          // 허리띠 (체력)
    
    // 악세서리 슬롯
    Ring1,         // 반지 1 (공격력 or 체력)
    Ring2,         // 반지 2 (공격력 or 체력)
    Necklace       // 목걸이 (체력)
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