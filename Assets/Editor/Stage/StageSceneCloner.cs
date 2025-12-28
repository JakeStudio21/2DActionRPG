using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

namespace StageSystem.Editor
{
    /// <summary>
    /// 스테이지 씬 자동 복제 도구
    /// Phase 6-B: 50개 스테이지 씬 생성
    /// </summary>
    public class StageSceneCloner : EditorWindow
    {
        private const string TEMPLATE_SCENE_PATH = "Assets/Scenes/CH01_ST01.unity";
        private const string SCENES_FOLDER = "Assets/Scenes/";
        
        private bool[] chapterToggles = new bool[5] { false, true, true, true, true }; // CH01은 건너뛰기
        private Vector2 scrollPos;
        private bool confirmGeneration = false;
        
        [MenuItem("Tools/Stage System/1. Clone Scenes (씬 복제)")]
        public static void ShowWindow()
        {
            var window = GetWindow<StageSceneCloner>("씬 복제 도구");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🎬 스테이지 씬 자동 복제", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"템플릿 씬: {TEMPLATE_SCENE_PATH}\n생성 위치: {SCENES_FOLDER}", MessageType.Info);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("📋 생성할 챕터 선택", EditorStyles.boldLabel);
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            // 챕터 1
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.BeginHorizontal();
            chapterToggles[0] = EditorGUILayout.Toggle(chapterToggles[0], GUILayout.Width(20));
            EditorGUILayout.LabelField("Chapter 1 (CH01_ST04 ~ CH01_ST10)", GUILayout.Width(300));
            EditorGUILayout.LabelField("7개 씬", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("CH01_ST01~03은 이미 존재하므로 건너뜁니다.", MessageType.None);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(5);
            
            // 챕터 2~5
            string[] chapterNames = { "", "초원의 시작", "사막의 모험", "던전 탐험", "화산 지대", "최종 결전" };
            int[] sceneCounts = { 7, 10, 10, 10, 10 };
            
            for (int i = 1; i < 5; i++)
            {
                EditorGUILayout.BeginHorizontal();
                chapterToggles[i] = EditorGUILayout.Toggle(chapterToggles[i], GUILayout.Width(20));
                EditorGUILayout.LabelField($"Chapter {i + 1}: {chapterNames[i + 1]} (CH0{i + 1}_ST01 ~ CH0{i + 1}_ST10)", GUILayout.Width(400));
                EditorGUILayout.LabelField($"{sceneCounts[i]}개 씬", GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            
            // 생성 예상 정보
            int totalScenes = 0;
            if (chapterToggles[0]) totalScenes += 7;
            for (int i = 1; i < 5; i++)
            {
                if (chapterToggles[i]) totalScenes += 10;
            }
            
            EditorGUILayout.HelpBox($"선택된 챕터: {CountSelectedChapters()}개\n생성될 씬: {totalScenes}개", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // 확인 체크박스
            confirmGeneration = EditorGUILayout.Toggle("위 내용을 확인했습니다", confirmGeneration);
            
            EditorGUILayout.Space(10);
            
            // 생성 버튼
            EditorGUI.BeginDisabledGroup(!confirmGeneration || totalScenes == 0);
            
            if (GUILayout.Button($"✅ {totalScenes}개 씬 생성 시작", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog(
                    "씬 생성 확인",
                    $"{totalScenes}개의 씬을 생성하시겠습니까?\n\n작업 시간: 약 {totalScenes * 2}초",
                    "생성",
                    "취소"))
                {
                    CloneScenes();
                }
            }
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("🔄 모두 선택", GUILayout.Height(30)))
            {
                for (int i = 1; i < 5; i++) chapterToggles[i] = true;
            }
            
            if (GUILayout.Button("❌ 모두 해제", GUILayout.Height(30)))
            {
                for (int i = 1; i < 5; i++) chapterToggles[i] = false;
            }
        }
        
        private int CountSelectedChapters()
        {
            int count = 0;
            for (int i = 0; i < 5; i++)
            {
                if (chapterToggles[i]) count++;
            }
            return count;
        }
        
        private void CloneScenes()
        {
            // 템플릿 씬 존재 확인
            if (!File.Exists(TEMPLATE_SCENE_PATH))
            {
                EditorUtility.DisplayDialog("에러", $"템플릿 씬을 찾을 수 없습니다:\n{TEMPLATE_SCENE_PATH}", "확인");
                return;
            }
            
            List<string> createdScenes = new List<string>();
            int totalScenes = 0;
            int createdCount = 0;
            
            // 총 씬 개수 계산
            if (chapterToggles[0]) totalScenes += 7;
            for (int i = 1; i < 5; i++)
            {
                if (chapterToggles[i]) totalScenes += 10;
            }
            
            EditorUtility.DisplayProgressBar("씬 복제 중", "준비 중...", 0f);
            
            try
            {
                // Chapter 1 (CH01_ST04 ~ CH01_ST10)
                if (chapterToggles[0])
                {
                    for (int stage = 4; stage <= 10; stage++)
                    {
                        string sceneName = $"CH01_ST{stage:D2}";
                        string targetPath = $"{SCENES_FOLDER}{sceneName}.unity";
                        
                        if (CloneScene(TEMPLATE_SCENE_PATH, targetPath, sceneName))
                        {
                            createdScenes.Add(targetPath);
                            createdCount++;
                        }
                        
                        float progress = (float)createdCount / totalScenes;
                        EditorUtility.DisplayProgressBar("씬 복제 중", $"{sceneName} 생성 중... ({createdCount}/{totalScenes})", progress);
                    }
                }
                
                // Chapter 2~5
                for (int chapter = 2; chapter <= 5; chapter++)
                {
                    if (!chapterToggles[chapter - 1])
                        continue;
                    
                    for (int stage = 1; stage <= 10; stage++)
                    {
                        string sceneName = $"CH0{chapter}_ST{stage:D2}";
                        string targetPath = $"{SCENES_FOLDER}{sceneName}.unity";
                        
                        if (CloneScene(TEMPLATE_SCENE_PATH, targetPath, sceneName))
                        {
                            createdScenes.Add(targetPath);
                            createdCount++;
                        }
                        
                        float progress = (float)createdCount / totalScenes;
                        EditorUtility.DisplayProgressBar("씬 복제 중", $"{sceneName} 생성 중... ({createdCount}/{totalScenes})", progress);
                    }
                }
                
                EditorUtility.ClearProgressBar();
                
                // 결과 출력
                string result = $"✅ 씬 생성 완료!\n\n생성된 씬: {createdCount}개\n\n";
                result += "생성된 씬 목록:\n";
                foreach (var scene in createdScenes)
                {
                    result += $"- {Path.GetFileName(scene)}\n";
                }
                
                EditorUtility.DisplayDialog("완료", result, "확인");
                
                Debug.Log($"[StageSceneCloner] ✅ {createdCount}개 씬 생성 완료");
                
                // Asset Database 새로고침
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("에러", $"씬 생성 중 오류 발생:\n{e.Message}", "확인");
                Debug.LogError($"[StageSceneCloner] ❌ 에러: {e.Message}");
            }
        }
        
        private bool CloneScene(string sourcePath, string targetPath, string sceneName)
        {
            try
            {
                // 이미 존재하면 건너뛰기
                if (File.Exists(targetPath))
                {
                    Debug.LogWarning($"[StageSceneCloner] 씬이 이미 존재합니다: {sceneName}");
                    return false;
                }
                
                // 씬 파일 복사
                AssetDatabase.CopyAsset(sourcePath, targetPath);
                
                Debug.Log($"[StageSceneCloner] ✅ 씬 생성: {sceneName}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StageSceneCloner] ❌ {sceneName} 생성 실패: {e.Message}");
                return false;
            }
        }
    }
}

