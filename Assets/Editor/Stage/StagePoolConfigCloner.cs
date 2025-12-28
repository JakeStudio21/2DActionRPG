using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace StageSystem.Editor
{
    /// <summary>
    /// 스테이지 PoolConfig 자동 생성 도구
    /// Phase 6-B: 50개 PoolConfig 생성
    /// </summary>
    public class StagePoolConfigCloner : EditorWindow
    {
        private const string TEMPLATE_POOLCONFIG_PATH = "Assets/Resources/Stages/ScenePools/CH01_ST01_PoolConfig.asset";
        private const string POOLCONFIG_FOLDER = "Assets/Resources/Stages/ScenePools/";
        
        private bool[] chapterToggles = new bool[5] { false, true, true, true, true }; // CH01은 ST02~10만
        private Vector2 scrollPos;
        private bool confirmGeneration = false;
        
        [MenuItem("Tools/Stage System/2. Clone PoolConfigs (PoolConfig 복제)")]
        public static void ShowWindow()
        {
            var window = GetWindow<StagePoolConfigCloner>("PoolConfig 복제 도구");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("🎮 PoolConfig 자동 복제", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox($"템플릿: {TEMPLATE_POOLCONFIG_PATH}\n생성 위치: {POOLCONFIG_FOLDER}", MessageType.Info);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("📋 생성할 챕터 선택", EditorStyles.boldLabel);
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            // 챕터 1
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.BeginHorizontal();
            chapterToggles[0] = EditorGUILayout.Toggle(chapterToggles[0], GUILayout.Width(20));
            EditorGUILayout.LabelField("Chapter 1 (CH01_ST02 ~ CH01_ST10)", GUILayout.Width(300));
            EditorGUILayout.LabelField("9개 Config", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("CH01_ST01_PoolConfig는 이미 존재하므로 건너뜁니다.", MessageType.None);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(5);
            
            // 챕터 2~5
            string[] chapterNames = { "", "초원의 시작", "사막의 모험", "던전 탐험", "화산 지대", "최종 결전" };
            int[] configCounts = { 9, 10, 10, 10, 10 };
            
            for (int i = 1; i < 5; i++)
            {
                EditorGUILayout.BeginHorizontal();
                chapterToggles[i] = EditorGUILayout.Toggle(chapterToggles[i], GUILayout.Width(20));
                EditorGUILayout.LabelField($"Chapter {i + 1}: {chapterNames[i + 1]} (CH0{i + 1}_ST01 ~ CH0{i + 1}_ST10)", GUILayout.Width(400));
                EditorGUILayout.LabelField($"{configCounts[i]}개 Config", GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            
            // 생성 예상 정보
            int totalConfigs = 0;
            if (chapterToggles[0]) totalConfigs += 9;
            for (int i = 1; i < 5; i++)
            {
                if (chapterToggles[i]) totalConfigs += 10;
            }
            
            EditorGUILayout.HelpBox($"선택된 챕터: {CountSelectedChapters()}개\n생성될 PoolConfig: {totalConfigs}개", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // 확인 체크박스
            confirmGeneration = EditorGUILayout.Toggle("위 내용을 확인했습니다", confirmGeneration);
            
            EditorGUILayout.Space(10);
            
            // 생성 버튼
            EditorGUI.BeginDisabledGroup(!confirmGeneration || totalConfigs == 0);
            
            if (GUILayout.Button($"✅ {totalConfigs}개 PoolConfig 생성 시작", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog(
                    "PoolConfig 생성 확인",
                    $"{totalConfigs}개의 PoolConfig를 생성하시겠습니까?\n\n작업 시간: 약 {totalConfigs}초",
                    "생성",
                    "취소"))
                {
                    ClonePoolConfigs();
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
        
        private void ClonePoolConfigs()
        {
            // 템플릿 존재 확인
            if (!File.Exists(TEMPLATE_POOLCONFIG_PATH))
            {
                EditorUtility.DisplayDialog("에러", $"템플릿 PoolConfig를 찾을 수 없습니다:\n{TEMPLATE_POOLCONFIG_PATH}", "확인");
                return;
            }
            
            // 폴더 존재 확인
            if (!Directory.Exists(POOLCONFIG_FOLDER))
            {
                Directory.CreateDirectory(POOLCONFIG_FOLDER);
                AssetDatabase.Refresh();
            }
            
            List<string> createdConfigs = new List<string>();
            int totalConfigs = 0;
            int createdCount = 0;
            
            // 총 개수 계산
            if (chapterToggles[0]) totalConfigs += 9;
            for (int i = 1; i < 5; i++)
            {
                if (chapterToggles[i]) totalConfigs += 10;
            }
            
            EditorUtility.DisplayProgressBar("PoolConfig 복제 중", "준비 중...", 0f);
            
            try
            {
                // Chapter 1 (CH01_ST02 ~ CH01_ST10)
                if (chapterToggles[0])
                {
                    for (int stage = 2; stage <= 10; stage++)
                    {
                        string configName = $"CH01_ST{stage:D2}_PoolConfig";
                        string targetPath = $"{POOLCONFIG_FOLDER}{configName}.asset";
                        
                        if (ClonePoolConfig(TEMPLATE_POOLCONFIG_PATH, targetPath, configName))
                        {
                            createdConfigs.Add(targetPath);
                            createdCount++;
                        }
                        
                        float progress = (float)createdCount / totalConfigs;
                        EditorUtility.DisplayProgressBar("PoolConfig 복제 중", $"{configName} 생성 중... ({createdCount}/{totalConfigs})", progress);
                    }
                }
                
                // Chapter 2~5
                for (int chapter = 2; chapter <= 5; chapter++)
                {
                    if (!chapterToggles[chapter - 1])
                        continue;
                    
                    for (int stage = 1; stage <= 10; stage++)
                    {
                        string configName = $"CH0{chapter}_ST{stage:D2}_PoolConfig";
                        string targetPath = $"{POOLCONFIG_FOLDER}{configName}.asset";
                        
                        if (ClonePoolConfig(TEMPLATE_POOLCONFIG_PATH, targetPath, configName))
                        {
                            createdConfigs.Add(targetPath);
                            createdCount++;
                        }
                        
                        float progress = (float)createdCount / totalConfigs;
                        EditorUtility.DisplayProgressBar("PoolConfig 복제 중", $"{configName} 생성 중... ({createdCount}/{totalConfigs})", progress);
                    }
                }
                
                EditorUtility.ClearProgressBar();
                
                // 결과 출력
                string result = $"✅ PoolConfig 생성 완료!\n\n생성된 Config: {createdCount}개\n\n";
                result += "생성된 Config 목록:\n";
                foreach (var config in createdConfigs)
                {
                    result += $"- {Path.GetFileName(config)}\n";
                }
                
                EditorUtility.DisplayDialog("완료", result, "확인");
                
                Debug.Log($"[StagePoolConfigCloner] ✅ {createdCount}개 PoolConfig 생성 완료");
                
                // Asset Database 새로고침
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("에러", $"PoolConfig 생성 중 오류 발생:\n{e.Message}", "확인");
                Debug.LogError($"[StagePoolConfigCloner] ❌ 에러: {e.Message}");
            }
        }
        
        private bool ClonePoolConfig(string sourcePath, string targetPath, string configName)
        {
            try
            {
                // 이미 존재하면 건너뛰기
                if (File.Exists(targetPath))
                {
                    Debug.LogWarning($"[StagePoolConfigCloner] PoolConfig가 이미 존재합니다: {configName}");
                    return false;
                }
                
                // PoolConfig 파일 복사
                AssetDatabase.CopyAsset(sourcePath, targetPath);
                
                Debug.Log($"[StagePoolConfigCloner] ✅ PoolConfig 생성: {configName}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[StagePoolConfigCloner] ❌ {configName} 생성 실패: {e.Message}");
                return false;
            }
        }
    }
}

