using UnityEngine;
using UnityEditor;
using CutsceneSystem;
using System.IO;

/// <summary>
/// CutsceneData 에셋 자동 생성 Editor 스크립트
/// Phase 2.5: 최소 3개 컷신 데이터 생성
/// </summary>
public class CutsceneDataGenerator : EditorWindow
{
    private string outputPath = "Assets/Resources/Cutscenes";
    
    [MenuItem("Tools/Stage System/Generate Cutscene Data")]
    public static void ShowWindow()
    {
        GetWindow<CutsceneDataGenerator>("Cutscene Data Generator");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("🎬 CutsceneData 에셋 생성", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox("Phase 2.5: 챕터 시스템 테스트용 더미 컷신 데이터를 생성합니다.\n" +
                                "- PROLOGUE (프롤로그)\n" +
                                "- CH01_START (챕터 1 시작)\n" +
                                "- CH01_CLEAR (챕터 1 클리어)\n\n" +
                                "실제 이미지/사운드 리소스는 나중에 추가하세요.",
                                MessageType.Info);
        
        GUILayout.Space(10);
        
        outputPath = EditorGUILayout.TextField("출력 경로:", outputPath);
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("✅ 컷신 데이터 3개 생성", GUILayout.Height(40)))
        {
            Generate3CutsceneData();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🗑️ 기존 컷신 데이터 삭제", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("삭제 확인",
                "정말로 모든 CutsceneData 에셋을 삭제하시겠습니까?",
                "삭제", "취소"))
            {
                DeleteAllCutsceneData();
            }
        }
    }
    
    /// <summary>
    /// 컷신 데이터 3개 생성
    /// </summary>
    private void Generate3CutsceneData()
    {
        // 출력 폴더 생성
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }
        
        int createdCount = 0;
        
        // 1. PROLOGUE 컷신
        if (CreateCutsceneData(
            "PROLOGUE",
            "프롤로그",
            CutsceneType.Intro,
            new (CutsceneStepType, string, string)[]
            {
                (CutsceneStepType.Dialogue, "", "옛날 옛적, 평화로운 왕국이 있었습니다..."),
                (CutsceneStepType.Dialogue, "내레이터", "하지만 어둠의 세력이 나타나 왕국을 위협하기 시작했습니다."),
                (CutsceneStepType.Dialogue, "내레이터", "용감한 모험가여, 이 세계를 구해주십시오!")
            },
            "게임 시작 시 재생되는 프롤로그 컷신입니다."
        ))
        {
            createdCount++;
        }
        
        // 2. CH01_START 컷신 (챕터 1 시작)
        if (CreateCutsceneData(
            "CH01_START",
            "챕터 1 시작",
            CutsceneType.ChapterMid,
            new (CutsceneStepType, string, string)[]
            {
                (CutsceneStepType.Dialogue, "가이드", "초원 지역에 오신 것을 환영합니다!"),
                (CutsceneStepType.Dialogue, "가이드", "여기서 기본적인 전투와 탐험을 배울 수 있습니다."),
                (CutsceneStepType.Dialogue, "가이드", "준비되셨나요? 그럼 시작해봅시다!")
            },
            "챕터 1 첫 진입 시 재생되는 시작 컷신입니다."
        ))
        {
            createdCount++;
        }
        
        // 3. CH01_CLEAR 컷신 (챕터 1 클리어)
        if (CreateCutsceneData(
            "CH01_CLEAR",
            "챕터 1 클리어",
            CutsceneType.ChapterMid,
            new (CutsceneStepType, string, string)[]
            {
                (CutsceneStepType.Dialogue, "", "축하합니다! 초원 지역을 완료했습니다!"),
                (CutsceneStepType.Dialogue, "가이드", "이제 사막 지역으로 떠날 준비가 되었습니다."),
                (CutsceneStepType.Dialogue, "가이드", "더 강력한 적들이 기다리고 있을 것입니다. 조심하세요!")
            },
            "챕터 1 (Stage 10) 클리어 후 로비에서 재생되는 종료 컷신입니다."
        ))
        {
            createdCount++;
        }
        
        // AssetDatabase 저장 및 갱신
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("생성 완료",
            $"컷신 데이터 {createdCount}개 생성 완료!\n" +
            $"경로: {outputPath}\n\n" +
            "✅ PROLOGUE\n" +
            "✅ CH01_START\n" +
            "✅ CH01_CLEAR",
            "확인");
    }
    
    /// <summary>
    /// 개별 컷신 데이터 생성
    /// </summary>
    private bool CreateCutsceneData(
        string cutsceneId,
        string cutsceneName,
        CutsceneType cutsceneType,
        (CutsceneStepType stepType, string speakerName, string dialogueText)[] dialogues,
        string description)
    {
        string fileName = $"{cutsceneId}.asset";
        string fullPath = Path.Combine(outputPath, fileName);
        
        // 이미 존재하는 경우 스킵 (덮어쓰기 방지)
        if (File.Exists(fullPath))
        {
            Debug.LogWarning($"⚠️ [CutsceneDataGenerator] 이미 존재함, 스킵: {fileName}");
            return false;
        }
        
        // CutsceneData 인스턴스 생성
        CutsceneData cutsceneData = ScriptableObject.CreateInstance<CutsceneData>();
        
        // 기본 정보
        cutsceneData.cutsceneId = cutsceneId;
        cutsceneData.cutsceneName = cutsceneName;
        cutsceneData.cutsceneType = cutsceneType;
        
        // Step 추가
        cutsceneData.steps = new System.Collections.Generic.List<CutsceneStep>();
        
        foreach (var dialogue in dialogues)
        {
            CutsceneStep step = new CutsceneStep
            {
                stepType = dialogue.stepType,
                speakerName = dialogue.speakerName,
                dialogueText = dialogue.dialogueText,
                typingSpeed = 20f // 기본 타이핑 속도
            };
            
            cutsceneData.steps.Add(step);
        }
        
        // 설정
        cutsceneData.pauseGameOnStart = true;
        cutsceneData.canSkip = true;
        cutsceneData.playOnce = false; // 테스트를 위해 반복 재생 가능
        
        // BGM 설정 (선택사항)
        if (cutsceneId == "PROLOGUE")
        {
            cutsceneData.bgmEventKey = "bgm.cutscene.prologue";
            cutsceneData.resumePreviousBGM = false; // 프롤로그 후 메인 BGM 재생
        }
        else
        {
            cutsceneData.resumePreviousBGM = true;
        }
        
        // 설명
        cutsceneData.description = description;
        
        // 에셋 생성
        AssetDatabase.CreateAsset(cutsceneData, fullPath);
        return true;
    }
    
    /// <summary>
    /// 모든 CutsceneData 에셋 삭제
    /// </summary>
    private void DeleteAllCutsceneData()
    {
        if (!Directory.Exists(outputPath))
        {
            EditorUtility.DisplayDialog("삭제 실패", "폴더가 존재하지 않습니다.", "확인");
            return;
        }
        
        string[] assetFiles = Directory.GetFiles(outputPath, "*.asset");
        
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
            $"컷신 데이터 {deletedCount}개 삭제 완료!",
            "확인");
    }
}

