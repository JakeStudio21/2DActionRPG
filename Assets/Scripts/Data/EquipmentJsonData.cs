using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🗂️ JSON 파일과 1:1 매칭되는 데이터 구조
/// </summary>
[Serializable]
public class EquipmentJsonData
{
    public List<WeaponBaseData> WeaponBaseTable = new List<WeaponBaseData>();
    public List<MeleeWeaponData> MeleeWeaponTable = new List<MeleeWeaponData>();
    public List<ProjectileWeaponData> ProjectileWeaponTable = new List<ProjectileWeaponData>();
    public List<ProjectileData> ProjectileDataTable = new List<ProjectileData>();
}

[Serializable]
public class WeaponBaseData
{
    public string ItemID;
    public string EquipmentName;    // "Equipment Name" 매핑
    public string WeaponType;       // "Weapon Type" 매핑
    public string AttackType;       // "Attack Type" 매핑
    public string EquipmentType;    // "Equipment Type" 매핑
    public string UsableClass;
    public bool isTradable;
    public string ItemGrade;        // "Item Grade" 매핑
    public int RequiredLevel;
    public string ResourceID;
    public string EquipmentPrefab;  // "Equipment Prefab" 매핑
    public string Icon;
    public string Description;
}

[Serializable]
public class MeleeWeaponData
{
    public string ItemID;
    public float Cooldown;
    public float AttackDamage;
    public float AttackSpeed;
    public float Attackrange;
    public int AttackShape;
    public float CriticalChance;
    public float CriticalDamage;
}

[Serializable]
public class ProjectileWeaponData
{
    public string ItemID;
    public float Cooldown;
    public float criticalChance;
    public float criticalDamage;
    public string ProjectileId;
}

[Serializable]
public class ProjectileData
{
    public string ProjectileId;
    public float AttackDamage;
    public float AttackSpeed;
    public float Attackrange;
    public int AttackShape;
    public string ResourceID;
    public string Prefab;
    public string Icon;
    public string Description;
}
