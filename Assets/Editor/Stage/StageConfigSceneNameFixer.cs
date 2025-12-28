using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace StageSystem.Editor
{
    /// <summary>
    /// StageConfig의 SceneName 필드 자동 수정 도구
    /// Phase 6-B: 50개 StageConfig의 SceneName을 CH##_ST## 형식으로 변경
    /// </summary>
    public class StageConfigSceneNameFixer : EditorWindow
    {
        private const string STAGE_CONFIG_FOLDER = "Stages/Configs/Chapters";
        
        private List<StageConfigInfo> stageConfigs = new List<StageConfigInfo>();
        private Vector2 scrollPos;
        private bool showOnlyIncorrect = true;
        private bool confirmFix = false;
        
        private class StageConfigInfo
        {
            public StageConfig config;
            public string assetPath;
            public string currentSceneName;
            public string correctSceneName;
            public bool isCorrect;
        }
        
        [MenuItem("Tools/Stage System/3. Fix StageConfig SceneName (StageConfig 수정)")]
        public static void ShowWindow()
        {
            var window = GetWindow<StageConfigSceneNameFixer>("StageConfig SceneName 수정");
            window.minSize = new Vector2(700, 600);
            window.Show();
            window.ScanStageConfigs();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🔧 StageConfig SceneName 자동 수정", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"경로: Resources/{STAGE_CONFIG_FOLDER}\n모든 StageConfig의 SceneName을 StageID와 동일하게 수정합니다.", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🔍 스캔 (Scan)", GUILayout.Height(30)))
            {
                ScanStageConfigs();
            }
            
            showOnlyIncorrect = GUILayout.Toggle(showOnlyIncorrect, "잘못된 것만 표시", GUILayout.Width(150));
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // 통계 정보
            int incorrectCount = stageConfigs.Count(c => !c.isCorrect);
            int correctCount = stageConfigs.Count(c => c.isCorrect);
            
            EditorGUILayout.HelpBox($"전체: {stageConfigs.Count}개 | 정상: {correctCount}개 | 수정 필요: {incorrectCount}개", MessageType.Info);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("📋 StageConfig 목록", EditorStyles.boldLabel);
            
            // 스크롤 뷰
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(350));
            
            EditorGUILayout.BeginVertical("box");
            
            // 헤더
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("StageID", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField("현재 SceneName", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("→", GUILayout.Width(20));
            EditorGUILayout.LabelField("올바른 SceneName", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("상태", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            var displayConfigs = showOnlyIncorrect 
                ? stageConfigs.Where(c => !c.isCorrect).ToList() 
                : stageConfigs;
            
            foreach (var info in displayConfigs)
            {
                EditorGUILayout.BeginHorizontal();
                
                // StageID
                EditorGUILayout.LabelField(info.config.StageID, GUILayout.Width(100));
                
                // 현재 SceneName
                GUI.color = info.isCorrect ? Color.white : Color.yellow;
                EditorGUILayout.LabelField(info.currentSceneName, GUILayout.Width(150));
                GUI.color = Color.white;
                
                // 화살표
                EditorGUILayout.LabelField("→", GUILayout.Width(20));
                
                // 올바른 SceneName
                GUI.color = Color.green;
                EditorGUILayout.LabelField(info.correctSceneName, GUILayout.Width(150));
                GUI.color = Color.white;
                
                // 상태
                if (info.isCorrect)
                {
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("✅ 정상", GUILayout.Width(60));
                }
                else
                {
                    GUI.color = Color.red;
                    EditorGUILayout.LabelField("❌ 수정", GUILayout.Width(60));
                }
                GUI.color = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            
            // 확인 체크박스
            if (incorrectCount > 0)
            {
                confirmFix = EditorGUILayout.Toggle($"{incorrectCount}개의 StageConfig를 수정하겠습니다", confirmFix);
            }
            
            EditorGUILayout.Space(10);
            
            // 수정 버튼
            EditorGUI.BeginDisabledGroup(!confirmFix || incorrectCount == 0);
            
            if (GUILayout.Button($"✅ {incorrectCount}개 StageConfig 수정 실행", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog(
                    "StageConfig 수정 확인",
                    $"{incorrectCount}개의 StageConfig를 수정하시겠습니까?\n\n이 작업은 되돌릴 수 없습니다.",
                    "수정",
                    "취소"))
                {
                    FixStageConfigs();
                }
            }
            
            EditorGUI.EndDisabledGroup();
        }
        
        private void ScanStageConfigs()
        {
            stageConfigs.Clear();
            
            // Resources 폴더에서 모든 StageConfig 로드
            StageConfig[] configs = Resources.LoadAll<StageConfig>(STAGE_CONFIG_FOLDER);
            
            Debug.Log($"[StageConfigSceneNameFixer] 🔍 {configs.Length}개 StageConfig 스캔 중...");
            
            foreach (var config in configs)
            {
                if (config == null)
                    continue;
                
                string stageId = config.StageID;
                string currentSceneName = config.SceneName;
                string correctSceneName = stageId; // StageID와 SceneName이 동일해야 함
                
                bool isCorrect = currentSceneName == correctSceneName;
                
                var info = new StageConfigInfo
                {
                    config = config,
                    assetPath = AssetDatabase.GetAssetPath(config),
                    currentSceneName = currentSceneName,
                    correctSceneName = correctSceneName,
                    isCorrect = isCorrect
                };
                
                stageConfigs.Add(info);
            }
            
            // StageID 순으로 정렬
            stageConfigs = stageConfigs.OrderBy(c => c.config.StageID).ToList();
            
            int incorrectCount = stageConfigs.Count(c => !c.isCorrect);
            Debug.Log($"[StageConfigSceneNameFixer] ✅ 스캔 완료: 전체 {stageConfigs.Count}개, 수정 필요 {incorrectCount}개");
        }
        
        private void FixStageConfigs()
        {
            int fixedCount = 0;
            int errorCount = 0;
            
            var incorrectConfigs = stageConfigs.Where(c => !c.isCorrect).ToList();
            
            EditorUtility.DisplayProgressBar("StageConfig 수정 중", "준비 중...", 0f);
            
            try
            {
                for (int i = 0; i < incorrectConfigs.Count; i++)
                {
                    var info = incorrectConfigs[i];
                    
                    float progress = (float)i / incorrectConfigs.Count;
                    EditorUtility.DisplayProgressBar("StageConfig 수정 중", $"{info.config.StageID} 수정 중... ({i + 1}/{incorrectConfigs.Count})", progress);
                    
                    try
                    {
                        // SceneName 수정
                        SerializedObject serializedObject = new SerializedObject(info.config);
                        SerializedProperty sceneNameProp = serializedObject.FindProperty("SceneName");
                        
                        if (sceneNameProp != null)
                        {
                            sceneNameProp.stringValue = info.correctSceneName;
                            serializedObject.ApplyModifiedProperties();
                            
                            EditorUtility.SetDirty(info.config);
                            
                            Debug.Log($"[StageConfigSceneNameFixer] ✅ {info.config.StageID}: {info.currentSceneName} → {info.correctSceneName}");
                            fixedCount++;
                        }
                        else
                        {
                            Debug.LogError($"[StageConfigSceneNameFixer] ❌ {info.config.StageID}: SceneName 필드를 찾을 수 없습니다.");
                            errorCount++;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[StageConfigSceneNameFixer] ❌ {info.config.StageID} 수정 실패: {e.Message}");
                        errorCount++;
                    }
                }
                
                // 변경사항 저장
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                EditorUtility.ClearProgressBar();
                
                // 결과 출력
                string result = $"✅ StageConfig 수정 완료!\n\n수정 성공: {fixedCount}개\n실패: {errorCount}개";
                
                EditorUtility.DisplayDialog("완료", result, "확인");
                
                Debug.Log($"[StageConfigSceneNameFixer] ✅ 수정 완료: {fixedCount}개 성공, {errorCount}개 실패");
                
                // 재스캔
                ScanStageConfigs();
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("에러", $"StageConfig 수정 중 오류 발생:\n{e.Message}", "확인");
                Debug.LogError($"[StageConfigSceneNameFixer] ❌ 에러: {e.Message}");
            }
        }
    }
}

