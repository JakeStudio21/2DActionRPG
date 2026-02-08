using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// MaterialDatabase + MaterialData SO 자동 생성 Editor 스크립트
/// Tools → Create Material Database
/// </summary>
public class CreateMaterialDatabase
{
    [MenuItem("Tools/Create Material Database")]
    public static void CreateDatabase()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🔧 [CreateMaterialDatabase] MaterialDatabase 생성 시작");
        Debug.Log("═══════════════════════════════════════════════════════");
        
        // 1. 폴더 생성
        string dataFolder = "Assets/Resources/Data";
        string materialsFolder = $"{dataFolder}/Materials";
        
        CreateFolderIfNotExists(dataFolder);
        CreateFolderIfNotExists(materialsFolder);
        
        // 2. MaterialData 9개 생성
        var materialDataList = new List<MaterialData>();
        
        materialDataList.Add(CreateMaterialDataAsset("WeaponFragment", MaterialType.WeaponFragment, 
            "무기 강화 파편", "무기를 강화할 때 사용하는 파편입니다.\nD/C/B 등급 무기를 분해하면 획득할 수 있습니다.", 
            "무기 강화 (D/C/B)", "무기 분해 또는 몬스터 처치", MaterialRarity.Common, 1));
        
        materialDataList.Add(CreateMaterialDataAsset("WeaponCrystal", MaterialType.WeaponCrystal, 
            "무기 강화 결정", "무기를 강화할 때 사용하는 결정입니다.\nA/S/SS 등급 무기를 분해하면 획득할 수 있습니다.", 
            "무기 강화 (A/S/SS)", "무기 분해 또는 강력한 몬스터 처치", MaterialRarity.Uncommon, 2));
        
        materialDataList.Add(CreateMaterialDataAsset("WeaponCore", MaterialType.WeaponCore, 
            "무기 강화 코어", "무기를 강화할 때 사용하는 코어입니다.\nEX/TR 등급 무기를 분해하면 획득할 수 있습니다.", 
            "무기 강화 (EX/TR)", "전설 무기 분해 또는 보스 처치", MaterialRarity.Rare, 3));
        
        materialDataList.Add(CreateMaterialDataAsset("ArmorFragment", MaterialType.ArmorFragment, 
            "방어구 강화 파편", "방어구를 강화할 때 사용하는 파편입니다.\nD/C/B 등급 방어구를 분해하면 획득할 수 있습니다.", 
            "방어구 강화 (D/C/B)", "방어구 분해 또는 몬스터 처치", MaterialRarity.Common, 4));
        
        materialDataList.Add(CreateMaterialDataAsset("ArmorCrystal", MaterialType.ArmorCrystal, 
            "방어구 강화 결정", "방어구를 강화할 때 사용하는 결정입니다.\nA/S/SS 등급 방어구를 분해하면 획득할 수 있습니다.", 
            "방어구 강화 (A/S/SS)", "방어구 분해 또는 강력한 몬스터 처치", MaterialRarity.Uncommon, 5));
        
        materialDataList.Add(CreateMaterialDataAsset("ArmorCore", MaterialType.ArmorCore, 
            "방어구 강화 코어", "방어구를 강화할 때 사용하는 코어입니다.\nEX/TR 등급 방어구를 분해하면 획득할 수 있습니다.", 
            "방어구 강화 (EX/TR)", "전설 방어구 분해 또는 보스 처치", MaterialRarity.Rare, 6));
        
        materialDataList.Add(CreateMaterialDataAsset("AccessoryFragment", MaterialType.AccessoryFragment, 
            "악세사리 강화 파편", "악세사리를 강화할 때 사용하는 파편입니다.\nD/C/B 등급 악세사리를 분해하면 획득할 수 있습니다.", 
            "악세사리 강화 (D/C/B)", "악세사리 분해 또는 몬스터 처치", MaterialRarity.Common, 7));
        
        materialDataList.Add(CreateMaterialDataAsset("AccessoryCrystal", MaterialType.AccessoryCrystal, 
            "악세사리 강화 결정", "악세사리를 강화할 때 사용하는 결정입니다.\nA/S/SS 등급 악세사리를 분해하면 획득할 수 있습니다.", 
            "악세사리 강화 (A/S/SS)", "악세사리 분해 또는 강력한 몬스터 처치", MaterialRarity.Uncommon, 8));
        
        materialDataList.Add(CreateMaterialDataAsset("AccessoryCore", MaterialType.AccessoryCore, 
            "악세사리 강화 코어", "악세사리를 강화할 때 사용하는 코어입니다.\nEX/TR 등급 악세사리를 분해하면 획득할 수 있습니다.", 
            "악세사리 강화 (EX/TR)", "전설 악세사리 분해 또는 보스 처치", MaterialRarity.Rare, 9));
        
        // 3. MaterialDatabase 생성
        string databasePath = $"{dataFolder}/MaterialDatabase.asset";
        MaterialDatabase database = AssetDatabase.LoadAssetAtPath<MaterialDatabase>(databasePath);
        
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<MaterialDatabase>();
            AssetDatabase.CreateAsset(database, databasePath);
            Debug.Log($"✅ MaterialDatabase 생성: {databasePath}");
        }
        else
        {
            Debug.Log($"⚠️ MaterialDatabase 이미 존재: {databasePath}");
        }
        
        // 4. MaterialData들을 Database에 등록
        database.materials = materialDataList;
        EditorUtility.SetDirty(database);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"✅ MaterialDatabase 생성 완료: {materialDataList.Count}개 재료 등록");
        Debug.Log("═══════════════════════════════════════════════════════");
        
        // 5. 검증
        database.ValidateDatabase();
        
        // 6. Inspector에서 선택
        Selection.activeObject = database;
        EditorGUIUtility.PingObject(database);
    }
    
    private static MaterialData CreateMaterialDataAsset(string fileName, MaterialType type, 
        string displayName, string description, string usage, string obtain, 
        MaterialRarity rarity, int sortOrder)
    {
        string path = $"Assets/Resources/Data/Materials/{fileName}.asset";
        
        MaterialData data = AssetDatabase.LoadAssetAtPath<MaterialData>(path);
        
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MaterialData>();
            AssetDatabase.CreateAsset(data, path);
            Debug.Log($"  ✅ {fileName}.asset 생성");
        }
        else
        {
            Debug.Log($"  ⚠️ {fileName}.asset 이미 존재 (업데이트)");
        }
        
        // 데이터 설정
        data.materialId = type.ToItemId(); // ⭐ materialId 자동 할당 (DropTable 연동용)
        data.materialType = type;
        data.displayName = displayName;
        data.description = description;
        data.usageHint = usage;
        data.obtainHint = obtain;
        data.rarity = rarity;
        data.sortOrder = sortOrder;
        data.maxStackSize = 9999;
        data.canDrop = true;
        
        // 등급별 배경 색상
        data.iconBackgroundColor = rarity switch
        {
            MaterialRarity.Common => new Color(0.8f, 0.8f, 0.8f, 1f),      // 회색
            MaterialRarity.Uncommon => new Color(0.8f, 1f, 0.8f, 1f),      // 연두
            MaterialRarity.Rare => new Color(0.8f, 0.9f, 1f, 1f),          // 연파랑
            MaterialRarity.Epic => new Color(0.95f, 0.8f, 1f, 1f),         // 연보라
            MaterialRarity.Legendary => new Color(1f, 0.9f, 0.7f, 1f),     // 연주황
            _ => Color.white
        };
        
        EditorUtility.SetDirty(data);
        
        return data;
    }
    
    private static void CreateFolderIfNotExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
            Debug.Log($"📁 폴더 생성: {path}");
        }
    }
}

