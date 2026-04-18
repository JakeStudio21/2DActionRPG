using UnityEditor;
using UnityEngine;
using Systems;

/// <summary>
/// EnhancementData ScriptableObject 에셋 생성 에디터 도구
/// </summary>
public static class CreateEnhancementData
{
    [MenuItem("Tools/Create Default EnhancementData", priority = 102)]
    public static void CreateDefaultAsset()
    {
        EnhancementData asset = ScriptableObject.CreateInstance<EnhancementData>();

        // 기본값 설정
        asset.maxEnhancementLevel = 15;
        
        // 등급별 기본 성공률
        asset.baseSuccessRate_D = 95f;
        asset.baseSuccessRate_C = 90f;
        asset.baseSuccessRate_B = 85f;
        asset.baseSuccessRate_A = 80f;
        asset.baseSuccessRate_S = 75f;
        asset.baseSuccessRate_SS = 70f;
        asset.baseSuccessRate_EX = 65f;
        asset.baseSuccessRate_TR = 60f;
        
        asset.successRateDecreasePerLevel = 3f;
        
        // 재료량
        asset.fragmentPerLevel_Low = 5;
        asset.fragmentPerLevel_Mid = 10;
        asset.fragmentPerLevel_High = 20;
        
        // 골드 비용
        asset.goldPerLevel_D = 100;
        asset.goldPerLevel_C = 200;
        asset.goldPerLevel_B = 500;
        asset.goldPerLevel_A = 1000;
        asset.goldPerLevel_S = 2000;
        asset.goldPerLevel_SS = 5000;
        asset.goldPerLevel_EX = 10000;
        asset.goldPerLevel_TR = 20000;
        
        // 실패 처리 구간
        asset.safeLevel = 9;
        asset.downgradeLevel = 12;

        string path = "Assets/Resources/Data";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Data");
        }

        string fullPath = $"{path}/EnhancementData.asset";
        AssetDatabase.CreateAsset(asset, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

