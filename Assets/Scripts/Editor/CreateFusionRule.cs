using UnityEditor;
using UnityEngine;
using Systems;

/// <summary>
/// FusionRule ScriptableObject 에셋 생성 에디터 도구
/// </summary>
public static class CreateFusionRule
{
    [MenuItem("Tools/Create Default FusionRule", priority = 101)]
    public static void CreateDefaultAsset()
    {
        FusionRule asset = ScriptableObject.CreateInstance<FusionRule>();

        // 기본값 설정 (이미 클래스에 정의되어 있지만 명시적으로)
        asset.lowTierRequiredCount = 3;   // D/C/B → 3개
        asset.highTierRequiredCount = 4;  // A/S/SS → 4개
        asset.exTierRequiredCount = 5;    // SS→EX → 5개 (특별)

        asset.costD_to_C = 100;
        asset.costC_to_B = 200;
        asset.costB_to_A = 500;
        asset.costA_to_S = 1000;
        asset.costS_to_SS = 2000;
        asset.costSS_to_EX = 5000;

        string path = "Assets/Resources/Data";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Data");
        }

        string fullPath = $"{path}/FusionRule.asset";
        AssetDatabase.CreateAsset(asset, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✨ [CreateFusionRule] FusionRule 에셋 생성 완료: {fullPath}");
    }
}

