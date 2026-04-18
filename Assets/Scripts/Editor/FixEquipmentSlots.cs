using UnityEditor;
using UnityEngine;

/// <summary>
/// 장비 슬롯 자동 수정 에디터 유틸리티
/// ArmorType → EquipmentSlot 자동 매핑
/// </summary>
public class FixEquipmentSlots : EditorWindow
{
    [MenuItem("Tools/Fix Equipment Slots")]
    public static void FixAllEquipmentSlots()
    {
        // 모든 EquipmentData 에셋 찾기
        string[] guids = AssetDatabase.FindAssets("t:EquipmentData");
        int fixedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EquipmentData equipment = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);

            if (equipment == null) continue;

            bool needsFix = false;

            // Armor 타입인 경우 ArmorType에 맞게 슬롯 설정
            if (equipment.equipmentType == EquipmentType.Armor)
            {
                EquipmentSlot correctSlot = GetSlotFromArmorType(equipment.ArmorType);
                if (equipment.equipmentSlot != correctSlot)
                {
                    equipment.equipmentSlot = correctSlot;
                    needsFix = true;
                }
            }
            // Weapon 타입인 경우 MainWeapon으로 설정
            else if (equipment.equipmentType == EquipmentType.Weapon)
            {
                if (equipment.equipmentSlot != EquipmentSlot.MainWeapon)
                {
                    equipment.equipmentSlot = EquipmentSlot.MainWeapon;
                    needsFix = true;
                }
            }
            // Accessory 타입인 경우 AccessoryType에 맞게 슬롯 설정
            else if (equipment.equipmentType == EquipmentType.Accessory)
            {
                EquipmentSlot correctSlot = GetSlotFromAccessoryType(equipment.AccessoryType);
                if (equipment.equipmentSlot != correctSlot)
                {
                    equipment.equipmentSlot = correctSlot;
                    needsFix = true;
                }
            }

            if (needsFix)
            {
                EditorUtility.SetDirty(equipment);
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "슬롯 자동 수정 완료",
            $"{fixedCount}개의 장비 슬롯이 수정되었습니다!",
            "확인"
        );
    }

    /// <summary>
    /// ArmorType → EquipmentSlot 매핑
    /// </summary>
    private static EquipmentSlot GetSlotFromArmorType(ArmorType armorType)
    {
        switch (armorType)
        {
            case ArmorType.Helmet: return EquipmentSlot.Helmet;
            case ArmorType.Armor: return EquipmentSlot.Armor;
            case ArmorType.Gloves: return EquipmentSlot.Gloves;
            case ArmorType.Boots: return EquipmentSlot.Boots;
            case ArmorType.Belt: return EquipmentSlot.Belt;
            default: return EquipmentSlot.Helmet;
        }
    }

    /// <summary>
    /// AccessoryType → EquipmentSlot 매핑
    /// </summary>
    private static EquipmentSlot GetSlotFromAccessoryType(AccessoryType accessoryType)
    {
        switch (accessoryType)
        {
            case AccessoryType.Necklace: return EquipmentSlot.Necklace;
            case AccessoryType.Ring: return EquipmentSlot.Ring1; // 기본값, 수동 조정 필요
            default: return EquipmentSlot.Necklace;
        }
    }
}

