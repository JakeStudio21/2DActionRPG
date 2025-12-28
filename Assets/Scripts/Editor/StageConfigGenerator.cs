using UnityEngine;
using UnityEditor;
using StageSystem;
using System.IO;

/// <summary>
/// ✅ Phase 0.5: 50개 StageConfig 더미 생성 에디터 스크립트
/// Tools → Stage System → Generate 50 Stage Configs
/// </summary>
public class StageConfigGenerator : EditorWindow
{
    private string outputPath = "Assets/Resources/Stages/Configs/Chapters";
    private bool autoCreateFolders = true;
    private bool overwriteExisting = false;
    
    [MenuItem("Tools/Stage System/Generate 50 Stage Configs")]
    public static void ShowWindow()
    {
        var window = GetWindow<StageConfigGenerator>("Stage Config Generator");
        window.minSize = new Vector2(450, 300);
    }
    
    private void OnGUI()
    {
        GUILayout.Label("📦 Stage Config Generator (Phase 0.5)", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // 출력 경로 설정
        GUILayout.BeginHorizontal();
        GUILayout.Label("Output Path:", GUILayout.Width(100));
        outputPath = EditorGUILayout.TextField(outputPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                outputPath = "Assets" + selectedPath.Replace(Application.dataPath, "");
            }
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        
        // 옵션
        autoCreateFolders = EditorGUILayout.Toggle("Auto Create Folders", autoCreateFolders);
        overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing", overwriteExisting);
        
        GUILayout.Space(10);
        
        // 생성 정보 표시
        EditorGUILayout.HelpBox(
            "생성될 StageConfig:\n" +
            "• Chapter 1~5 × Stage 1~10 = 총 50개\n" +
            "• 보스 스테이지: 3, 6, 10 (Victory: BossKill)\n" +
            "• 해금 조건: 순차 해금 (Stage N-1 클리어)\n" +
            "• Chapter 1은 기본 해금",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        // 생성 버튼
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("✅ Generate 50 Configs (CH01~05, ST01~10)", GUILayout.Height(40)))
        {
            Generate50Configs();
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.Space(10);
        
        // 삭제 버튼
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("🗑️ Delete All Generated Configs", GUILayout.Height(30)))
        {
            DeleteAllConfigs();
        }
        GUI.backgroundColor = Color.white;
        
        GUILayout.Space(10);
        
        // 도움말
        EditorGUILayout.HelpBox(
            "⚠️ 주의사항:\n" +
            "• 생성 후 각 StageConfig에 WaveConfig를 수동으로 연결해야 합니다.\n" +
            "• Scene 파일은 별도로 생성해야 합니다.\n" +
            "• DropTable도 별도로 생성해야 합니다.",
            MessageType.Warning
        );
    }
    
    private void Generate50Configs()
    {
        // 확인 다이얼로그
        if (!EditorUtility.DisplayDialog(
            "확인", 
            $"50개의 StageConfig를 생성하시겠습니까?\n\n경로: {outputPath}", 
            "생성", 
            "취소"))
        {
            return;
        }
        
        // 출력 폴더 생성
        if (autoCreateFolders && !Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
            Debug.Log($"📁 [Generator] 폴더 생성: {outputPath}");
        }
        
        int totalCreated = 0;
        int totalSkipped = 0;
        int totalUpdated = 0;
        
        for (int chapter = 1; chapter <= 5; chapter++)
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                string stageId = $"CH{chapter:D2}_ST{stage:D2}";
                string fileName = $"{stageId}_Config.asset";
                string fullPath = Path.Combine(outputPath, fileName);
                
                // 이미 존재하면 스킵 (overwrite 옵션 체크)
                if (File.Exists(fullPath) && !overwriteExisting)
                {
                    totalSkipped++;
                    continue;
                }
                
                bool isUpdate = File.Exists(fullPath);
                
                // StageConfig 생성
                StageConfig config = ScriptableObject.CreateInstance<StageConfig>();
                
                // ========================================
                // 기본 정보
                // ========================================
                config.StageID = stageId;
                config.StageName = $"Chapter {chapter} - Stage {stage}";
                config.SceneName = $"Stage_{chapter:D2}_{stage:D2}"; // 실제 씬은 나중에
                config.BackgroundType = BackgroundType.FIELD;
                config.IsDungeon = false;
                
                // ========================================
                // 챕터 정보 (자동 파싱됨, OnValidate에서)
                // ========================================
                config.chapterId = chapter;
                config.stageIndexInChapter = stage;
                
                // ========================================
                // 진행 조건
                // ========================================
                config.RequiredLevel = chapter * 5;
                
                // 해금 조건
                if (stage == 1)
                {
                    // 챕터 첫 스테이지
                    if (chapter == 1)
                    {
                        config.UnlockCondition = "AlwaysUnlocked"; // Chapter 1, Stage 1은 기본 해금
                    }
                    else
                    {
                        // Chapter N의 Stage 1은 Chapter N-1의 Stage 10 클리어 시
                        config.UnlockCondition = $"Clear:CH{(chapter-1):D2}_ST10";
                    }
                }
                else
                {
                    // 이전 스테이지 클리어 시 해금
                    config.UnlockCondition = $"Clear:CH{chapter:D2}_ST{(stage-1):D2}";
                }
                
                // ========================================
                // 게임플레이
                // ========================================
                config.WaveCount = 3;
                config.TimeLimitSec = 300; // 5분
                config.Victory = VictoryCondition.KillAll;
                
                // 보스 스테이지 처리 (3, 6, 10)
                if (stage == 3 || stage == 6 || stage == 10)
                {
                    config.Victory = VictoryCondition.BossKill;
                    config.TimeLimitSec = 600; // 보스는 10분
                    
                    // Stage 10은 던전
                    if (stage == 10)
                    {
                        config.IsDungeon = true;
                        config.BackgroundType = BackgroundType.BOSS_ROOM;
                    }
                    else
                    {
                        config.BackgroundType = BackgroundType.FIELD;
                    }
                }
                
                // ========================================
                // 보상 (더미)
                // ========================================
                config.FirstClearDropGroupId = $"DROP_{stageId}_FIRST";
                config.RepeatClearDropGroupId = $"DROP_{stageId}_REPEAT";
                
                // ========================================
                // BGM
                // ========================================
                config.BGMPath = $"Prefab/BGM/SCENE_CH{chapter:D2}";
                
                // ========================================
                // 설명
                // ========================================
                string bossInfo = (stage == 3 || stage == 6 || stage == 10) ? "보스" : "일반";
                config.Description = 
                    $"Chapter {chapter} - Stage {stage} 더미 데이터\n" +
                    $"타입: {bossInfo} 스테이지\n" +
                    $"해금 조건: {config.UnlockCondition}";
                
                // ========================================
                // 컷신 ID (일부만 할당)
                // ========================================
                if (stage == 1)
                {
                    // 챕터 시작 스테이지는 입장 컷신
                    config.enterCutsceneId = $"CH{chapter:D2}_START";
                }
                
                if (stage == 10)
                {
                    // 최종 보스는 클리어 컷신
                    config.clearCutsceneId = $"CH{chapter:D2}_ST10_CLEAR";
                }
                
                config.isReplaySkipCutscene = true;
                
                // ========================================
                // 에셋 저장
                // ========================================
                if (isUpdate)
                {
                    // 기존 에셋 업데이트
                    var existingConfig = AssetDatabase.LoadAssetAtPath<StageConfig>(fullPath);
                    EditorUtility.CopySerialized(config, existingConfig);
                    EditorUtility.SetDirty(existingConfig);
                    totalUpdated++;
                    Debug.Log($"🔄 [Generator] 업데이트: {stageId}");
                }
                else
                {
                    // 새 에셋 생성
                    AssetDatabase.CreateAsset(config, fullPath);
                    totalCreated++;
                    Debug.Log($"✅ [Generator] 생성: {stageId}");
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // 결과 다이얼로그
        string resultMessage = 
            $"생성 완료!\n\n" +
            $"• 새로 생성: {totalCreated}개\n" +
            $"• 업데이트: {totalUpdated}개\n" +
            $"• 스킵: {totalSkipped}개\n" +
            $"• 총계: {totalCreated + totalUpdated + totalSkipped}/50개";
        
        EditorUtility.DisplayDialog("완료", resultMessage, "OK");
        
        Debug.Log($"🎉 [Generator] 작업 완료! 생성: {totalCreated}, 업데이트: {totalUpdated}, 스킵: {totalSkipped}");
    }
    
    private void DeleteAllConfigs()
    {
        if (!EditorUtility.DisplayDialog(
            "⚠️ 경고", 
            "생성된 모든 StageConfig(50개)를 삭제하시겠습니까?\n\n이 작업은 되돌릴 수 없습니다!", 
            "삭제", 
            "취소"))
        {
            return;
        }
        
        int totalDeleted = 0;
        
        for (int chapter = 1; chapter <= 5; chapter++)
        {
            for (int stage = 1; stage <= 10; stage++)
            {
                string stageId = $"CH{chapter:D2}_ST{stage:D2}";
                string fileName = $"{stageId}_Config.asset";
                string fullPath = Path.Combine(outputPath, fileName);
                
                if (File.Exists(fullPath))
                {
                    AssetDatabase.DeleteAsset(fullPath);
                    totalDeleted++;
                    Debug.Log($"🗑️ [Generator] 삭제: {stageId}");
                }
            }
        }
        
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("완료", $"{totalDeleted}개 StageConfig 삭제 완료!", "OK");
        Debug.Log($"🗑️ [Generator] 총 {totalDeleted}개 삭제 완료!");
    }
}

