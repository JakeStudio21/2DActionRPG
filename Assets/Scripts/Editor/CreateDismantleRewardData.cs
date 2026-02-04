using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// DismantleRewardData 기본 에셋 생성 Editor 스크립트
/// </summary>
public class CreateDismantleRewardData
{
    [MenuItem("Tools/Create Default DismantleRewardData")]
    public static void CreateDefaultData()
    {
        // Resources/Data 폴더 생성
        string folderPath = "Assets/Resources/Data";
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            AssetDatabase.Refresh();
        }
        
        // 기본 DismantleRewardData 생성
        string assetPath = $"{folderPath}/DismantleRewardData.asset";
        
        // 이미 존재하면 건너뛰기
        if (AssetDatabase.LoadAssetAtPath<DismantleRewardData>(assetPath) != null)
        {
            Debug.LogWarning($"[CreateDismantleRewardData] 이미 존재: {assetPath}");
            return;
        }
        
        var data = ScriptableObject.CreateInstance<DismantleRewardData>();
        
        // 기본값 설정
        data.fragmentS = 100;
        data.fragmentA = 50;
        data.fragmentB = 25;
        data.fragmentC = 10;
        data.fragmentD = 5;
        data.enhancementBonusPerLevel = 0.1f; // +1당 10%
        data.goldPerDismantle = 100;
        data.enhancementStoneChance = 10; // 10%
        data.enhancementStoneAmount = 1;
        
        AssetDatabase.CreateAsset(data, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"✅ [CreateDismantleRewardData] 생성 완료: {assetPath}");
        
        // Inspector에서 선택
        Selection.activeObject = data;
        EditorGUIUtility.PingObject(data);
    }
}

