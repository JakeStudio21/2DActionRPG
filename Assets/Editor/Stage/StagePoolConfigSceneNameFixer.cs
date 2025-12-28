using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace StageSystem.Editor
{
    /// <summary>
    /// ScenePoolConfig의 sceneName 필드 자동 수정 도구
    /// Phase 6-B: PoolConfig의 Scene Name을 파일명과 동일하게 변경
    /// </summary>
    public class StagePoolConfigSceneNameFixer : EditorWindow
    {
        private const string POOLCONFIG_FOLDER = "Stages/ScenePools";
        
        private List<PoolConfigInfo> poolConfigs = new List<PoolConfigInfo>();
        private Vector2 scrollPos;
        private bool showOnlyIncorrect = true;
        private bool confirmFix = false;
        
        private class PoolConfigInfo
        {
            public ScenePoolConfig config;
            public string assetPath;
            public string assetName;
            public string currentSceneName;
            public string correctSceneName;
            public bool isCorrect;
        }
        
        [MenuItem("Tools/Stage System/2-1. Fix PoolConfig SceneName (PoolConfig SceneName 수정)")]
        public static void ShowWindow()
        {
            var window = GetWindow<StagePoolConfigSceneNameFixer>("PoolConfig SceneName 수정");
            window.minSize = new Vector2(700, 600);
            window.Show();
            window.ScanPoolConfigs();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🔧 PoolConfig SceneName 자동 수정", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"경로: Resources/{POOLCONFIG_FOLDER}\n모든 PoolConfig의 sceneName을 파일명과 동일하게 수정합니다.", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🔍 스캔 (Scan)", GUILayout.Height(30)))
            {
                ScanPoolConfigs();
            }
            
            showOnlyIncorrect = GUILayout.Toggle(showOnlyIncorrect, "잘못된 것만 표시", GUILayout.Width(150));
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // 통계 정보
            int incorrectCount = poolConfigs.Count(c => !c.isCorrect);
            int correctCount = poolConfigs.Count(c => c.isCorrect);
            
            EditorGUILayout.HelpBox($"전체: {poolConfigs.Count}개 | 정상: {correctCount}개 | 수정 필요: {incorrectCount}개", MessageType.Info);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("📋 PoolConfig 목록", EditorStyles.boldLabel);
            
            // 스크롤 뷰
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(350));
            
            EditorGUILayout.BeginVertical("box");
            
            // 헤더
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("파일명", EditorStyles.boldLabel, GUILayout.Width(180));
            EditorGUILayout.LabelField("현재 sceneName", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("→", GUILayout.Width(20));
            EditorGUILayout.LabelField("올바른 sceneName", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("상태", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            var displayConfigs = showOnlyIncorrect 
                ? poolConfigs.Where(c => !c.isCorrect).ToList() 
                : poolConfigs;
            
            foreach (var info in displayConfigs)
            {
                EditorGUILayout.BeginHorizontal();
                
                // 파일명
                EditorGUILayout.LabelField(info.assetName, GUILayout.Width(180));
                
                // 현재 sceneName
                GUI.color = info.isCorrect ? Color.white : Color.yellow;
                EditorGUILayout.LabelField(info.currentSceneName, GUILayout.Width(150));
                GUI.color = Color.white;
                
                // 화살표
                EditorGUILayout.LabelField("→", GUILayout.Width(20));
                
                // 올바른 sceneName
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
                confirmFix = EditorGUILayout.Toggle($"{incorrectCount}개의 PoolConfig를 수정하겠습니다", confirmFix);
            }
            
            EditorGUILayout.Space(10);
            
            // 수정 버튼
            EditorGUI.BeginDisabledGroup(!confirmFix || incorrectCount == 0);
            
            if (GUILayout.Button($"✅ {incorrectCount}개 PoolConfig 수정 실행", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog(
                    "PoolConfig 수정 확인",
                    $"{incorrectCount}개의 PoolConfig를 수정하시겠습니까?\n\n이 작업은 되돌릴 수 없습니다.",
                    "수정",
                    "취소"))
                {
                    FixPoolConfigs();
                }
            }
            
            EditorGUI.EndDisabledGroup();
        }
        
        private void ScanPoolConfigs()
        {
            poolConfigs.Clear();
            
            // Resources 폴더에서 모든 ScenePoolConfig 로드
            ScenePoolConfig[] configs = Resources.LoadAll<ScenePoolConfig>(POOLCONFIG_FOLDER);
            
            Debug.Log($"[StagePoolConfigSceneNameFixer] 🔍 {configs.Length}개 PoolConfig 스캔 중...");
            
            foreach (var config in configs)
            {
                if (config == null)
                    continue;
                
                string assetPath = AssetDatabase.GetAssetPath(config);
                string assetName = Path.GetFileNameWithoutExtension(assetPath);
                string currentSceneName = config.sceneName;
                
                // 파일명에서 "_PoolConfig" 제거하면 올바른 sceneName
                string correctSceneName = assetName.Replace("_PoolConfig", "");
                
                bool isCorrect = currentSceneName == correctSceneName;
                
                var info = new PoolConfigInfo
                {
                    config = config,
                    assetPath = assetPath,
                    assetName = assetName,
                    currentSceneName = currentSceneName,
                    correctSceneName = correctSceneName,
                    isCorrect = isCorrect
                };
                
                poolConfigs.Add(info);
            }
            
            // 파일명 순으로 정렬
            poolConfigs = poolConfigs.OrderBy(c => c.assetName).ToList();
            
            int incorrectCount = poolConfigs.Count(c => !c.isCorrect);
            Debug.Log($"[StagePoolConfigSceneNameFixer] ✅ 스캔 완료: 전체 {poolConfigs.Count}개, 수정 필요 {incorrectCount}개");
        }
        
        private void FixPoolConfigs()
        {
            int fixedCount = 0;
            int errorCount = 0;
            
            var incorrectConfigs = poolConfigs.Where(c => !c.isCorrect).ToList();
            
            EditorUtility.DisplayProgressBar("PoolConfig 수정 중", "준비 중...", 0f);
            
            try
            {
                for (int i = 0; i < incorrectConfigs.Count; i++)
                {
                    var info = incorrectConfigs[i];
                    
                    float progress = (float)i / incorrectConfigs.Count;
                    EditorUtility.DisplayProgressBar("PoolConfig 수정 중", $"{info.assetName} 수정 중... ({i + 1}/{incorrectConfigs.Count})", progress);
                    
                    try
                    {
                        // sceneName 수정
                        SerializedObject serializedObject = new SerializedObject(info.config);
                        SerializedProperty sceneNameProp = serializedObject.FindProperty("sceneName");
                        
                        if (sceneNameProp != null)
                        {
                            sceneNameProp.stringValue = info.correctSceneName;
                            serializedObject.ApplyModifiedProperties();
                            
                            EditorUtility.SetDirty(info.config);
                            
                            Debug.Log($"[StagePoolConfigSceneNameFixer] ✅ {info.assetName}: {info.currentSceneName} → {info.correctSceneName}");
                            fixedCount++;
                        }
                        else
                        {
                            Debug.LogError($"[StagePoolConfigSceneNameFixer] ❌ {info.assetName}: sceneName 필드를 찾을 수 없습니다.");
                            errorCount++;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[StagePoolConfigSceneNameFixer] ❌ {info.assetName} 수정 실패: {e.Message}");
                        errorCount++;
                    }
                }
                
                // 변경사항 저장
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                EditorUtility.ClearProgressBar();
                
                // 결과 출력
                string result = $"✅ PoolConfig 수정 완료!\n\n수정 성공: {fixedCount}개\n실패: {errorCount}개";
                
                EditorUtility.DisplayDialog("완료", result, "확인");
                
                Debug.Log($"[StagePoolConfigSceneNameFixer] ✅ 수정 완료: {fixedCount}개 성공, {errorCount}개 실패");
                
                // 재스캔
                ScanPoolConfigs();
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("에러", $"PoolConfig 수정 중 오류 발생:\n{e.Message}", "확인");
                Debug.LogError($"[StagePoolConfigSceneNameFixer] ❌ 에러: {e.Message}");
            }
        }
    }
}

