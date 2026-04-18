using UnityEngine;
using UnityEditor;
using StageSystem;
using System.IO;

/// <summary>
/// ChapterData 에셋 자동 생성 Editor 스크립트
/// Phase 2: 챕터 1~5 데이터 에셋 생성
/// </summary>
public class ChapterDataGenerator : EditorWindow
{
    private string outputPath = "Assets/Resources/Stages/Chapters";
    
    [MenuItem("Tools/Stage System/Generate Chapter Data")]
    public static void ShowWindow()
    {
        GetWindow<ChapterDataGenerator>("Chapter Data Generator");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("📚 ChapterData 에셋 생성", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox("챕터 1~5 데이터 에셋을 자동 생성합니다.\n" +
                                "경로: Assets/Resources/Stages/Chapters/", MessageType.Info);
        
        GUILayout.Space(10);
        
        outputPath = EditorGUILayout.TextField("출력 경로:", outputPath);
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("✅ 챕터 데이터 5개 생성", GUILayout.Height(40)))
        {
            Generate5ChapterData();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🗑️ 기존 챕터 데이터 삭제", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("삭제 확인",
                "정말로 모든 ChapterData 에셋을 삭제하시겠습니까?",
                "삭제", "취소"))
            {
                DeleteAllChapterData();
            }
        }
    }
    
    /// <summary>
    /// 챕터 1~5 데이터 생성
    /// </summary>
    private void Generate5ChapterData()
    {
        // 출력 폴더 생성
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }
        
        string[] chapterTitles = new string[]
        {
            "Chapter 1: 초원의 시작",
            "Chapter 2: 사막의 모험",
            "Chapter 3: 던전 탐험",
            "Chapter 4: 화산 지대",
            "Chapter 5: 최종 결전"
        };
        
        string[] descriptions = new string[]
        {
            "평화로운 초원에서 모험이 시작됩니다. 기본적인 전투와 탐험을 배우세요.",
            "뜨거운 사막을 횡단하며 강력한 적들과 맞서 싸우세요.",
            "어두운 던전 깊숙이 숨겨진 보물과 위험이 기다리고 있습니다.",
            "용암이 흐르는 화산 지대에서 불의 시련을 극복하세요.",
            "모든 여정의 끝, 최후의 적과 결전을 벌이세요."
        };
        
        string[] unlockConditions = new string[]
        {
            "기본 해금",
            "챕터 1 클리어",
            "챕터 2 클리어",
            "챕터 3 클리어",
            "챕터 4 클리어"
        };
        
        int[] recommendedLevels = new int[] { 1, 5, 10, 15, 20 };
        
        int createdCount = 0;
        
        for (int i = 1; i <= 5; i++)
        {
            string fileName = $"CH{i:D2}_Data.asset";
            string fullPath = Path.Combine(outputPath, fileName);
            
            // 이미 존재하는 경우 스킵 (덮어쓰기 방지)
            if (File.Exists(fullPath))
            {
                Debug.LogWarning($"⚠️ [ChapterDataGenerator] 이미 존재함, 스킵: {fileName}");
                continue;
            }
            
            // ChapterData 인스턴스 생성
            ChapterData chapterData = ScriptableObject.CreateInstance<ChapterData>();
            
            // 기본 정보
            chapterData.chapterId = i;
            chapterData.chapterTitle = chapterTitles[i - 1];
            chapterData.stageCount = 10;
            
            // 컷신 설정 (더미)
            chapterData.chapterStartCutsceneId = $"CH{i:D2}_START";
            chapterData.chapterClearCutsceneId = $"CH{i:D2}_CLEAR";
            
            // 해금 조건
            chapterData.unlockCondition = unlockConditions[i - 1];
            
            // 사운드
            chapterData.bgmKey = $"BGM_Chapter{i}";
            
            // 설명
            chapterData.description = descriptions[i - 1];
            
            // 난이도 정보
            chapterData.recommendedLevel = recommendedLevels[i - 1];
            chapterData.difficultyLevel = i; // 1~5
            
            // 에셋 생성
            AssetDatabase.CreateAsset(chapterData, fullPath);
            
            createdCount++;
        }
        
        // AssetDatabase 저장 및 갱신
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("생성 완료",
            $"챕터 데이터 {createdCount}개 생성 완료!\n" +
            $"경로: {outputPath}",
            "확인");
    }
    
    /// <summary>
    /// 모든 ChapterData 에셋 삭제
    /// </summary>
    private void DeleteAllChapterData()
    {
        if (!Directory.Exists(outputPath))
        {
            EditorUtility.DisplayDialog("삭제 실패", "폴더가 존재하지 않습니다.", "확인");
            return;
        }
        
        string[] assetFiles = Directory.GetFiles(outputPath, "CH*.asset");
        
        int deletedCount = 0;
        foreach (string file in assetFiles)
        {
            string relativePath = file.Replace("\\", "/");
            if (relativePath.StartsWith(Application.dataPath))
            {
                relativePath = "Assets" + relativePath.Substring(Application.dataPath.Length);
            }
            
            if (AssetDatabase.DeleteAsset(relativePath))
            {
                deletedCount++;
            }
        }
        
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("삭제 완료",
            $"챕터 데이터 {deletedCount}개 삭제 완료!",
            "확인");
    }
}

