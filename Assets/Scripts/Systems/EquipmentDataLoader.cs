using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🌐 외부 데이터(엑셀→JSON) → Unity EquipmentData 적용 시스템
/// 무기, 갑옷, 신발, 악세서리 등 모든 장비 타입 지원
/// </summary>
[System.Serializable]
public class EquipmentDataEntry
{
    [Header("📋 기본 정보")]
    public string itemName;           // "Sword_A", "Armor_B", "Boots_S" 등
    public string equipmentType;      // "Weapon", "Armor", "Accessory"
    public string itemGrade;          // "S", "A", "B", "C", "D"
    public string weaponType;         // "Sword", "Bow", "Magic" (무기인 경우)
    public string description;        // 설명
    
    [Header("⚔️ 무기 전용 속성")]
    public float weaponCooldown = 1.0f;
    public float weaponRange = 5.0f;
}

[System.Serializable]
public class EquipmentDataCollection
{
    public EquipmentDataEntry[] equipment;
}

public class EquipmentDataLoader : MonoBehaviour
{
    [Header("🔧 데이터 로딩 설정")]
    [SerializeField] private string jsonFileName = "EquipmentData"; // Resources/Data/EquipmentData.json
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool validateDataIntegrity = true;

    [Header("📊 통계 정보")]
    [SerializeField] private int loadedWeapons = 0;
    [SerializeField] private int loadedArmors = 0;
    [SerializeField] private int loadedAccessories = 0;
    [SerializeField] private int totalLoadedItems = 0;

    private void Start()
    {
        if (loadOnStart)
        {
            LoadAndApplyEquipmentData();
        }
    }

    [ContextMenu("Load Equipment Data")]
    public void LoadAndApplyEquipmentData()
    {
        try
        {
            // 통계 초기화
            ResetStatistics();
            
            // JSON 파일 로드
            TextAsset jsonFile = Resources.Load<TextAsset>($"Data/{jsonFileName}");
            if (jsonFile == null)
            {
                Debug.LogError($"❌ [EquipmentDataLoader] {jsonFileName}.json 파일을 찾을 수 없습니다!");
                Debug.LogError($"📁 파일 위치: Assets/Resources/Data/{jsonFileName}.json");
                return;
            }

            // JSON 파싱
            string jsonContent = $"{{\"equipment\":{jsonFile.text}}}";
            EquipmentDataCollection equipmentData = JsonUtility.FromJson<EquipmentDataCollection>(jsonContent);
            
            if (equipmentData.equipment == null || equipmentData.equipment.Length == 0)
            {
                Debug.LogWarning("⚠️ [EquipmentDataLoader] JSON에서 장비 데이터를 찾을 수 없습니다!");
                return;
            }

            if (debugMode)
                Debug.Log($"📊 [EquipmentDataLoader] 장비 데이터 로드 완료: {equipmentData.equipment.Length}개");

            // 각 장비 데이터 적용
            foreach (var equipment in equipmentData.equipment)
            {
                if (ValidateEquipmentEntry(equipment))
                {
                    ApplyEquipmentData(equipment);
                }
            }

            // 결과 요약
            LogLoadingSummary();
            
            Debug.Log("✅ [EquipmentDataLoader] 모든 장비 데이터 적용 완료!");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ [EquipmentDataLoader] 장비 데이터 로드 실패: {e.Message}");
            Debug.LogError($"🔍 스택 트레이스: {e.StackTrace}");
        }
    }

    private bool ValidateEquipmentEntry(EquipmentDataEntry entry)
    {
        if (string.IsNullOrEmpty(entry.itemName))
        {
            Debug.LogWarning("⚠️ [EquipmentDataLoader] itemName이 비어있습니다!");
            return false;
        }

        if (!validateDataIntegrity) return true;

        // 장비 타입 검증
        if (!IsValidEquipmentType(entry.equipmentType))
        {
            Debug.LogWarning($"⚠️ [EquipmentDataLoader] 잘못된 equipmentType: {entry.equipmentType} ({entry.itemName})");
            return false;
        }

        // 아이템 등급 검증
        if (!IsValidItemGrade(entry.itemGrade))
        {
            Debug.LogWarning($"⚠️ [EquipmentDataLoader] 잘못된 itemGrade: {entry.itemGrade} ({entry.itemName})");
            return false;
        }

        return true;
    }

    private void ApplyEquipmentData(EquipmentDataEntry entry)
    {
        // Resources에서 EquipmentData 찾기
        string resourcePath = $"{entry.itemName}_Equipment";
        EquipmentData equipment = Resources.Load<EquipmentData>(resourcePath);

        if (equipment == null)
        {
            Debug.LogWarning($"⚠️ [EquipmentDataLoader] {resourcePath}를 찾을 수 없습니다!");
            return;
        }

        // 데이터 업데이트
        UpdateEquipmentData(equipment, entry);
        
        // 통계 업데이트
        UpdateStatistics(entry.equipmentType);

        if (debugMode)
        {
            Debug.Log($"🔄 [EquipmentDataLoader] {entry.itemName} 업데이트 완료");
            Debug.Log($"   📊 타입: {entry.equipmentType}, 등급: {entry.itemGrade}");
        }
    }

    private void UpdateEquipmentData(EquipmentData equipment, EquipmentDataEntry entry)
    {
        // 기본 정보 업데이트
        equipment.equipmentName = entry.itemName;
        if (!string.IsNullOrEmpty(entry.description))
            equipment.description = entry.description;

        // Enum 변환 및 설정
        if (Enum.TryParse<EquipmentType>(entry.equipmentType, out EquipmentType equipType))
            equipment.equipmentType = equipType;

        if (Enum.TryParse<ItemGrade>(entry.itemGrade, out ItemGrade grade))
            equipment.itemGrade = grade;

        // 무기 전용 설정 (리플렉션 사용)
        if (equipment.IsWeapon)
        {
            SetPrivateField(equipment, "weaponCooldown", entry.weaponCooldown);
            SetPrivateField(equipment, "weaponRange", entry.weaponRange);
            
            if (!string.IsNullOrEmpty(entry.weaponType) && 
                Enum.TryParse<WeaponType>(entry.weaponType, out WeaponType wepType))
            {
                SetPrivateField(equipment, "weaponType", wepType);
            }
        }
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(obj, value);
        }
        else if (debugMode)
        {
            Debug.LogWarning($"⚠️ [EquipmentDataLoader] 필드를 찾을 수 없습니다: {fieldName}");
        }
    }

    private bool IsValidEquipmentType(string equipmentType)
    {
        return Enum.TryParse<EquipmentType>(equipmentType, out _);
    }

    private bool IsValidItemGrade(string itemGrade)
    {
        return Enum.TryParse<ItemGrade>(itemGrade, out _);
    }

    private void ResetStatistics()
    {
        loadedWeapons = 0;
        loadedArmors = 0;
        loadedAccessories = 0;
        totalLoadedItems = 0;
    }

    private void UpdateStatistics(string equipmentType)
    {
        totalLoadedItems++;
        
        switch (equipmentType)
        {
            case "Weapon": loadedWeapons++; break;
            case "Armor": loadedArmors++; break;
            case "Accessory": loadedAccessories++; break;
        }
    }

    private void LogLoadingSummary()
    {
        Debug.Log($"📈 [EquipmentDataLoader] 로딩 결과 요약:");
        Debug.Log($"   ⚔️ 무기: {loadedWeapons}개");
        Debug.Log($"   🛡️ 방어구: {loadedArmors}개");
        Debug.Log($"   💍 악세서리: {loadedAccessories}개");
        Debug.Log($"   📦 총합: {totalLoadedItems}개");
    }

    [ContextMenu("Print Current Equipment Stats")]
    public void PrintCurrentEquipmentStats()
    {
        // 🔧 메모리 누수 방지: AssetDatabase 사용으로 변경
        #if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:EquipmentData");
        int equipmentCount = guids.Length;
        
        Debug.Log($"🔍 [EquipmentDataLoader] 프로젝트 내 장비 파일 수: {equipmentCount}개");
        
        // 처음 5개만 예시로 출력 (메모리 절약)
        for (int i = 0; i < Mathf.Min(5, equipmentCount); i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            Debug.Log($"   📋 {i+1}. {fileName} ({path})");
        }
        
        if (equipmentCount > 5)
        {
            Debug.Log($"   ... 외 {equipmentCount - 5}개 더");
        }
        #else
        Debug.Log("⚠️ [EquipmentDataLoader] 에디터에서만 사용 가능한 기능입니다.");
        #endif
    }
}
