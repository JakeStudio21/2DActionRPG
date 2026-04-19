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
        // 1. 폴더 생성
        string dataFolder = "Assets/Resources/Data";
        string materialsFolder = $"{dataFolder}/Materials";
        
        CreateFolderIfNotExists(dataFolder);
        CreateFolderIfNotExists(materialsFolder);
        
        // 2. MaterialData 21개 생성 (9개 기본 + 8개 룬 + 4개 정령)
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
        
        // 🔷 룬 조각 8종 추가
        materialDataList.Add(CreateMaterialDataAsset("RUNE_FRAG_RUNE_BOSS_HUNTER", MaterialType.RUNE_FRAG_RUNE_BOSS_HUNTER, 
            "보스 사냥꾼 룬 조각", "보스 몬스터에게 강력한 데미지를 주는 룬을 만들 때 사용하는 조각입니다.\n보스 몬스터 처치 시 낮은 확률로 획득할 수 있습니다.", 
            "보스 사냥꾼 룬 합성", "보스 몬스터 처치", MaterialRarity.Uncommon, 200));
        
        materialDataList.Add(CreateMaterialDataAsset("RUNE_FRAG_RUNE_BOSS_DEFENDER", MaterialType.RUNE_FRAG_RUNE_BOSS_DEFENDER, 
            "보스 철벽 룬 조각", "보스 몬스터의 공격을 효과적으로 막아주는 룬을 만들 때 사용하는 조각입니다.\n보스 몬스터 처치 시 낮은 확률로 획득할 수 있습니다.", 
            "보스 철벽 룬 합성", "보스 몬스터 처치", MaterialRarity.Uncommon, 201));
        
        materialDataList.Add(CreateMaterialDataAsset("RUNE_FRAG_RUNE_DEFENSE_BREAKER", MaterialType.RUNE_FRAG_RUNE_DEFENSE_BREAKER, 
            "방어 파괴자 룬 조각", "적의 방어력을 무시하는 룬을 만들 때 사용하는 조각입니다.\n엘리트 몬스터 처치 시 획득할 수 있습니다.", 
            "방어 파괴자 룬 합성", "엘리트 몬스터 처치", MaterialRarity.Uncommon, 202));
        
        materialDataList.Add(CreateMaterialDataAsset("RUNE_FRAG_RUNE_HIGH_HP_HUNTER", MaterialType.RUNE_FRAG_RUNE_HIGH_HP_HUNTER, 
            "고체력 사냥꾼 룬 조각", "체력이 높은 적에게 강력한 데미지를 주는 룬을 만들 때 사용하는 조각입니다.\n엘리트 몬스터 처치 시 획득할 수 있습니다.", 
            "고체력 사냥꾼 룬 합성", "엘리트 몬스터 처치", MaterialRarity.Uncommon, 203));
        
        materialDataList.Add(CreateMaterialDataAsset("RUNE_FRAG_RUNE_EXECUTIONER", MaterialType.RUNE_FRAG_RUNE_EXECUTIONER, 
            "처형자 룬 조각", "체력이 낮은 적에게 치명적인 데미지를 주는 룬을 만들 때 사용하는 조각입니다.\n일반 몬스터 처치 시 획득할 수 있습니다.", 
            "처형자 룬 합성", "몬스터 처치", MaterialRarity.Uncommon, 204));
        
        materialDataList.Add(CreateMaterialDataAsset("RUNE_FRAG_RUNE_SURVIVOR", MaterialType.RUNE_FRAG_RUNE_SURVIVOR, 
            "불굴의 생존자 룬 조각", "치명적인 공격에도 살아남을 수 있게 해주는 룬을 만들 때 사용하는 조각입니다.\n강력한 적을 처치할 때 획득할 수 있습니다.", 
            "불굴의 생존자 룬 합성", "강력한 몬스터 처치", MaterialRarity.Uncommon, 205));
        
        materialDataList.Add(CreateMaterialDataAsset("RUNE_FRAG_RUNE_AREA_DEFENDER", MaterialType.RUNE_FRAG_RUNE_AREA_DEFENDER, 
            "장판 철벽 룬 조각", "적의 범위 공격 데미지를 크게 줄여주는 룬을 만들 때 사용하는 조각입니다.\nAOE 공격을 사용하는 몬스터 처치 시 획득할 수 있습니다.", 
            "장판 철벽 룬 합성", "AOE 몬스터 처치", MaterialRarity.Uncommon, 206));
        
        materialDataList.Add(CreateMaterialDataAsset("RUNE_FRAG_RUNE_LIFESTEAL", MaterialType.RUNE_FRAG_RUNE_LIFESTEAL, 
            "흡혈 룬 조각", "적에게 준 데미지의 일부를 체력으로 흡수하는 룬을 만들 때 사용하는 조각입니다.\n언데드 계열 몬스터 처치 시 획득할 수 있습니다.", 
            "흡혈 룬 합성", "언데드 몬스터 처치", MaterialRarity.Uncommon, 207));
        
        // 🌟 정령의 정수 4종 추가 (Phase 9: 저항 시스템)
        materialDataList.Add(CreateMaterialDataAsset("SpiritEssence_Forest", MaterialType.SPIRIT_ESSENCE_FOREST, 
            "숲의 정수", "깊은 숲의 정령이 내린 축복이 담긴 정수입니다.\n속박 저항을 높이는 정령의 가호를 받을 때 사용합니다.", 
            "속박 저항 강화", "정령 스테이지 또는 특별 이벤트", MaterialRarity.Rare, 300));
        
        materialDataList.Add(CreateMaterialDataAsset("SpiritEssence_Flame", MaterialType.SPIRIT_ESSENCE_FLAME, 
            "불꽃의 정수", "작열하는 불꽃 정령이 내린 축복이 담긴 정수입니다.\n화상 저항을 높이는 정령의 가호를 받을 때 사용합니다.", 
            "화상 저항 강화", "정령 스테이지 또는 특별 이벤트", MaterialRarity.Rare, 301));
        
        materialDataList.Add(CreateMaterialDataAsset("SpiritEssence_Earth", MaterialType.SPIRIT_ESSENCE_EARTH, 
            "대지의 정수", "어둠 속의 대지 정령이 내린 축복이 담긴 정수입니다.\n중독 저항을 높이는 정령의 가호를 받을 때 사용합니다.", 
            "중독 저항 강화", "정령 스테이지 또는 특별 이벤트", MaterialRarity.Rare, 302));
        
        materialDataList.Add(CreateMaterialDataAsset("SpiritEssence_Water", MaterialType.SPIRIT_ESSENCE_WATER, 
            "물결의 정수", "차가운 물결 정령이 내린 축복이 담긴 정수입니다.\n둔화 저항을 높이는 정령의 가호를 받을 때 사용합니다.", 
            "둔화 저항 강화", "정령 스테이지 또는 특별 이벤트", MaterialRarity.Rare, 303));
        
        // 3. MaterialDatabase 생성
        string databasePath = $"{dataFolder}/MaterialDatabase.asset";
        MaterialDatabase database = AssetDatabase.LoadAssetAtPath<MaterialDatabase>(databasePath);
        
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<MaterialDatabase>();
            AssetDatabase.CreateAsset(database, databasePath);
        }
        else
        {
        }
        
        // 4. MaterialData들을 Database에 등록
        database.materials = materialDataList;
        EditorUtility.SetDirty(database);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
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
            // ✅ 새로 생성: 하드코딩된 초기값 설정
            data = ScriptableObject.CreateInstance<MaterialData>();
            
            // 데이터 설정 (초기값)
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
            
            AssetDatabase.CreateAsset(data, path);
            EditorUtility.SetDirty(data);
        }
        else
        {
            // ⏭️ 이미 존재: Inspector에서 수정한 값 유지 (덮어쓰지 않음)
        }
        
        return data;
    }
    
    private static void CreateFolderIfNotExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }
}

