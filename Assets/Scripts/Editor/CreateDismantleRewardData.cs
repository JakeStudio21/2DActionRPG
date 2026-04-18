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
        
        // 기본값 설정 (등급별 재료량)
        data.fragmentD = 5;
        data.fragmentC = 10;
        data.fragmentB = 25;
        data.fragmentA = 50;
        data.fragmentS = 100;
        data.fragmentSS = 150;
        data.fragmentEX = 200;
        data.fragmentTR = 300;
        data.enhancementBonusPerLevel = 0.1f; // +1당 10%
        // ⚠️ [삭제됨] 골드 보상 제거 - 분해 시 재료만 획득
        
        AssetDatabase.CreateAsset(data, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        // Inspector에서 선택
        Selection.activeObject = data;
        EditorGUIUtility.PingObject(data);
    }
}

