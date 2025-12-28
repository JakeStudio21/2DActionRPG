using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace StageSystem.Editor
{
    /// <summary>
    /// 스테이지 씬 Build Settings 자동 등록 도구
    /// Phase 6-B: 50개 스테이지 씬을 Build Settings에 자동 등록
    /// </summary>
    public class StageBuildSettingsRegistrar : EditorWindow
    {
        private const string SCENES_FOLDER = "Assets/Scenes/";
        
        private List<SceneInfo> allScenes = new List<SceneInfo>();
        private List<SceneInfo> stageScenes = new List<SceneInfo>();
        private Vector2 scrollPos;
        private bool showAllScenes = false;
        private bool confirmRegister = false;
        
        private class SceneInfo
        {
            public string sceneName;
            public string scenePath;
            public bool isRegistered;
            public bool isStageScene;
        }
        
        [MenuItem("Tools/Stage System/4. Register to Build Settings (Build Settings 등록)")]
        public static void ShowWindow()
        {
            var window = GetWindow<StageBuildSettingsRegistrar>("Build Settings 등록");
            window.minSize = new Vector2(700, 600);
            window.Show();
            window.ScanScenes();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🔨 Build Settings 자동 등록", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"경로: {SCENES_FOLDER}\n스테이지 씬(CH##_ST##)을 Build Settings에 자동 등록합니다.", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🔍 스캔 (Scan)", GUILayout.Height(30)))
            {
                ScanScenes();
            }
            
            showAllScenes = GUILayout.Toggle(showAllScenes, "모든 씬 표시", GUILayout.Width(150));
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // 통계 정보
            int registeredCount = stageScenes.Count(s => s.isRegistered);
            int notRegisteredCount = stageScenes.Count(s => !s.isRegistered);
            
            EditorGUILayout.HelpBox($"스테이지 씬: {stageScenes.Count}개 | 등록됨: {registeredCount}개 | 미등록: {notRegisteredCount}개", MessageType.Info);
            
            if (notRegisteredCount > 0)
            {
                EditorGUILayout.HelpBox($"⚠️ {notRegisteredCount}개의 스테이지 씬이 Build Settings에 등록되지 않았습니다!", MessageType.Warning);
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("📋 씬 목록", EditorStyles.boldLabel);
            
            // 스크롤 뷰
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(350));
            
            EditorGUILayout.BeginVertical("box");
            
            // 헤더
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("씬 이름", EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField("경로", EditorStyles.boldLabel, GUILayout.Width(350));
            EditorGUILayout.LabelField("상태", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            var displayScenes = showAllScenes ? allScenes : stageScenes;
            
            foreach (var scene in displayScenes)
            {
                EditorGUILayout.BeginHorizontal();
                
                // 씬 이름
                if (scene.isStageScene)
                {
                    GUI.color = Color.cyan;
                    EditorGUILayout.LabelField($"🎮 {scene.sceneName}", GUILayout.Width(150));
                    GUI.color = Color.white;
                }
                else
                {
                    EditorGUILayout.LabelField(scene.sceneName, GUILayout.Width(150));
                }
                
                // 경로
                EditorGUILayout.LabelField(scene.scenePath, GUILayout.Width(350));
                
                // 상태
                if (scene.isRegistered)
                {
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("✅ 등록됨", GUILayout.Width(100));
                }
                else
                {
                    GUI.color = scene.isStageScene ? Color.red : Color.gray;
                    EditorGUILayout.LabelField(scene.isStageScene ? "❌ 미등록" : "- 미등록", GUILayout.Width(100));
                }
                GUI.color = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            
            // 확인 체크박스
            if (notRegisteredCount > 0)
            {
                confirmRegister = EditorGUILayout.Toggle($"{notRegisteredCount}개 씬을 Build Settings에 등록하겠습니다", confirmRegister);
            }
            
            EditorGUILayout.Space(10);
            
            // 등록 버튼
            EditorGUI.BeginDisabledGroup(!confirmRegister || notRegisteredCount == 0);
            
            if (GUILayout.Button($"✅ {notRegisteredCount}개 씬 Build Settings 등록", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog(
                    "Build Settings 등록 확인",
                    $"{notRegisteredCount}개의 씬을 Build Settings에 등록하시겠습니까?\n\n기존 Build Settings에 추가됩니다.",
                    "등록",
                    "취소"))
                {
                    RegisterScenes();
                }
            }
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(10);
            
            // 추가 버튼
            if (GUILayout.Button("📂 Build Settings 열기", GUILayout.Height(30)))
            {
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            }
        }
        
        private void ScanScenes()
        {
            allScenes.Clear();
            stageScenes.Clear();
            
            // 현재 Build Settings에 등록된 씬 목록
            var buildScenes = EditorBuildSettings.scenes.Select(s => s.path).ToHashSet();
            
            Debug.Log($"[StageBuildSettingsRegistrar] 🔍 씬 스캔 중...");
            
            // Scenes 폴더의 모든 씬 파일 찾기
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { SCENES_FOLDER });
            
            foreach (string guid in sceneGuids)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                
                bool isRegistered = buildScenes.Contains(scenePath);
                bool isStageScene = IsStageScene(sceneName);
                
                var info = new SceneInfo
                {
                    sceneName = sceneName,
                    scenePath = scenePath,
                    isRegistered = isRegistered,
                    isStageScene = isStageScene
                };
                
                allScenes.Add(info);
                
                if (isStageScene)
                {
                    stageScenes.Add(info);
                }
            }
            
            // 씬 이름 순으로 정렬
            allScenes = allScenes.OrderBy(s => s.sceneName).ToList();
            stageScenes = stageScenes.OrderBy(s => s.sceneName).ToList();
            
            int notRegisteredCount = stageScenes.Count(s => !s.isRegistered);
            Debug.Log($"[StageBuildSettingsRegistrar] ✅ 스캔 완료: 전체 {allScenes.Count}개, 스테이지 {stageScenes.Count}개, 미등록 {notRegisteredCount}개");
        }
        
        private bool IsStageScene(string sceneName)
        {
            // CH##_ST## 형식인지 확인
            if (sceneName.Length != 10)
                return false;
            
            if (!sceneName.StartsWith("CH"))
                return false;
            
            if (sceneName[4] != '_' || sceneName[5] != 'S' || sceneName[6] != 'T')
                return false;
            
            // 챕터 번호 확인 (01~99)
            if (!char.IsDigit(sceneName[2]) || !char.IsDigit(sceneName[3]))
                return false;
            
            // 스테이지 번호 확인 (01~99)
            if (!char.IsDigit(sceneName[7]) || !char.IsDigit(sceneName[8]))
                return false;
            
            return true;
        }
        
        private void RegisterScenes()
        {
            int registeredCount = 0;
            
            var notRegisteredScenes = stageScenes.Where(s => !s.isRegistered).ToList();
            
            if (notRegisteredScenes.Count == 0)
            {
                EditorUtility.DisplayDialog("알림", "등록할 씬이 없습니다.", "확인");
                return;
            }
            
            EditorUtility.DisplayProgressBar("Build Settings 등록 중", "준비 중...", 0f);
            
            try
            {
                // 기존 Build Settings 씬 목록 가져오기
                List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                
                for (int i = 0; i < notRegisteredScenes.Count; i++)
                {
                    var scene = notRegisteredScenes[i];
                    
                    float progress = (float)i / notRegisteredScenes.Count;
                    EditorUtility.DisplayProgressBar("Build Settings 등록 중", $"{scene.sceneName} 등록 중... ({i + 1}/{notRegisteredScenes.Count})", progress);
                    
                    // 새 씬 추가 (활성화 상태로)
                    buildScenes.Add(new EditorBuildSettingsScene(scene.scenePath, true));
                    
                    Debug.Log($"[StageBuildSettingsRegistrar] ✅ 등록: {scene.sceneName}");
                    registeredCount++;
                }
                
                // Build Settings 업데이트
                EditorBuildSettings.scenes = buildScenes.ToArray();
                
                EditorUtility.ClearProgressBar();
                
                // 결과 출력
                string result = $"✅ Build Settings 등록 완료!\n\n등록된 씬: {registeredCount}개\n전체 Build Scenes: {buildScenes.Count}개";
                
                EditorUtility.DisplayDialog("완료", result, "확인");
                
                Debug.Log($"[StageBuildSettingsRegistrar] ✅ 등록 완료: {registeredCount}개");
                
                // 재스캔
                ScanScenes();
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("에러", $"Build Settings 등록 중 오류 발생:\n{e.Message}", "확인");
                Debug.LogError($"[StageBuildSettingsRegistrar] ❌ 에러: {e.Message}");
            }
        }
    }
}

