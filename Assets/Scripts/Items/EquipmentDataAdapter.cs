using UnityEngine;

/// <summary>
/// 🔄 WeaponInfo ↔ EquipmentData 양방향 호환성 어댑터
/// 기존 WeaponInfo와 신규 EquipmentData 간 변환
/// </summary>
public static class EquipmentDataAdapter
{
    /// <summary>
    /// WeaponInfo를 EquipmentData로 변환
    /// </summary>
    public static EquipmentData ConvertWeaponInfo(WeaponInfo weaponInfo)
    {
        if (weaponInfo == null) return null;
        
        var equipmentData = ScriptableObject.CreateInstance<EquipmentData>();
        
        // 기본 정보
        equipmentData.equipmentName = weaponInfo.name;
        equipmentData.equipmentType = EquipmentType.Weapon;
        equipmentData.equipmentPrefab = weaponInfo.weaponPrefab;
        
        // 무기 설정 (private 필드는 리플렉션으로 설정)
        SetPrivateField(equipmentData, "weaponCooldown", weaponInfo.weaponCooldown);
        SetPrivateField(equipmentData, "weaponRange", weaponInfo.weaponRange);
        
        // 능력치 설정
        // ❌ 삭제할 줄
        // equipmentData.damageBonus = weaponInfo.weaponDamage;
        
        // 무기 타입 추론 (이름 기반)
        if (weaponInfo.name.ToLower().Contains("sword"))
            SetPrivateField(equipmentData, "weaponType", WeaponType.Sword);
        else if (weaponInfo.name.ToLower().Contains("bow"))
            SetPrivateField(equipmentData, "weaponType", WeaponType.Bow);
        else if (weaponInfo.name.ToLower().Contains("staff"))
            SetPrivateField(equipmentData, "weaponType", WeaponType.Magic);
        
        return equipmentData;
    }
    
    /// <summary>
    /// 🆕 EquipmentData를 WeaponInfo로 변환 (기존 시스템 호환용)
    /// </summary>
    public static WeaponInfo ConvertToWeaponInfo(EquipmentData equipmentData)
    {
        if (equipmentData == null || !equipmentData.IsWeapon) return null;
        
        // 런타임에서 WeaponInfo 생성
        var weaponInfo = ScriptableObject.CreateInstance<WeaponInfo>();
        
        // 기본 정보 설정
        weaponInfo.name = equipmentData.equipmentName;
        weaponInfo.weaponPrefab = equipmentData.equipmentPrefab;
        weaponInfo.weaponCooldown = equipmentData.WeaponCooldown;
        weaponInfo.weaponRange = equipmentData.WeaponRange;
        // ✅ 대체 코드 (기본값 사용)
        weaponInfo.weaponDamage = 0; // 무기 데미지는 별도 시스템에서 관리
        
        Debug.Log($"🔄 [Adapter] EquipmentData → WeaponInfo 변환: {equipmentData.equipmentName} (쿨다운: {equipmentData.WeaponCooldown})");
        
        return weaponInfo;
    }
    
    /// <summary>
    /// EquipmentData를 WeaponInfo 형태로 사용할 때의 호환 함수들
    /// </summary>
    public static class WeaponInfoCompatibility
    {
        public static GameObject GetWeaponPrefab(EquipmentData equipment) => equipment?.equipmentPrefab;
        public static float GetWeaponCooldown(EquipmentData equipment) => equipment?.WeaponCooldown ?? 1.0f;
        // ❌ 삭제할 줄
        // public static int GetWeaponDamage(EquipmentData equipment) => equipment?.damageBonus ?? 0;
        // ✅ 대체 코드 (또는 완전 삭제)
        public static int GetWeaponDamage(EquipmentData equipment) => 0; // 별도 스탯 시스템에서 관리
        public static float GetWeaponRange(EquipmentData equipment) => equipment?.WeaponRange ?? 5.0f;
    }
    
    private static void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }
} 