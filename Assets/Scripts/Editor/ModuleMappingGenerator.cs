using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using LevelDesign;

namespace LevelDesignEditor
{
    /// <summary>
    /// 모듈 프리팹 매핑 테이블을 자동으로 생성하는 Editor Tool
    /// Tools → Module System → Generate Mapping Table
    /// </summary>
    public class ModuleMappingGenerator : EditorWindow
    {
        private string sourceTheme = "Sample";
        private string targetTheme = "Forest";
        private string modulePrefix = "Md_";
        private string sourceFolderPath = "Assets/Prefabs/Modules/Sample";
        private string targetFolderPath = "Assets/Prefabs/Modules/Forest";
        private string outputFolderPath = "Assets/Prefabs/Modules/ScriptableObjects";

        private List<GameObject> sourcePrefabs = new List<GameObject>();
        private List<GameObject> targetPrefabs = new List<GameObject>();
        private int matchedCount = 0;

        private Vector2 scrollPosition;

        [MenuItem("Tools/Module System/Generate Mapping Table")]
        public static void ShowWindow()
        {
            var window = GetWindow<ModuleMappingGenerator>("Mapping Generator");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Module Mapping Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // 테마 설정
            GUILayout.Label("Theme Settings", EditorStyles.boldLabel);
            sourceTheme = EditorGUILayout.TextField("Source Theme", sourceTheme);
            targetTheme = EditorGUILayout.TextField("Target Theme", targetTheme);
            modulePrefix = EditorGUILayout.TextField("Module Prefix", modulePrefix);

            GUILayout.Space(10);

            // 폴더 경로 설정
            GUILayout.Label("Folder Paths", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            sourceFolderPath = EditorGUILayout.TextField("Source Folder", sourceFolderPath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Source Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    sourceFolderPath = GetRelativePath(path);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            targetFolderPath = EditorGUILayout.TextField("Target Folder", targetFolderPath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Target Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    targetFolderPath = GetRelativePath(path);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            outputFolderPath = EditorGUILayout.TextField("Output Folder", outputFolderPath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    outputFolderPath = GetRelativePath(path);
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 스캔 버튼
            if (GUILayout.Button("Scan Prefabs", GUILayout.Height(30)))
            {
                ScanPrefabs();
            }

            GUILayout.Space(10);

            // 스캔 결과 표시
            if (sourcePrefabs.Count > 0 || targetPrefabs.Count > 0)
            {
                GUILayout.Label("Scan Results", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    $"Source Prefabs: {sourcePrefabs.Count}\n" +
                    $"Target Prefabs: {targetPrefabs.Count}\n" +
                    $"Matched: {matchedCount}",
                    MessageType.Info
                );

                GUILayout.Space(10);

                // 매칭 결과 상세 표시
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                
                foreach (var sourcePrefab in sourcePrefabs)
                {
                    string sourceName = sourcePrefab.name;
                    string expectedTargetName = sourceName.Replace(
                        $"{modulePrefix}{sourceTheme}_",
                        $"{modulePrefix}{targetTheme}_"
                    );

                    GameObject matchedTarget = FindTargetPrefab(expectedTargetName);
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    if (matchedTarget != null)
                    {
                        EditorGUILayout.LabelField("✓", GUILayout.Width(20));
                        EditorGUILayout.LabelField(sourceName, GUILayout.Width(200));
                        EditorGUILayout.LabelField("→", GUILayout.Width(20));
                        EditorGUILayout.LabelField(matchedTarget.name);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("✗", GUILayout.Width(20));
                        EditorGUILayout.LabelField(sourceName, GUILayout.Width(200));
                        EditorGUILayout.LabelField("→", GUILayout.Width(20));
                        EditorGUILayout.LabelField("(No Match)", EditorStyles.helpBox);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();

                GUILayout.Space(10);

                // 생성 버튼
                GUI.enabled = matchedCount > 0;
                if (GUILayout.Button("Generate Mapping Asset", GUILayout.Height(40)))
                {
                    GenerateMappingAsset();
                }
                GUI.enabled = true;
            }
        }

        private void ScanPrefabs()
        {
            sourcePrefabs.Clear();
            targetPrefabs.Clear();
            matchedCount = 0;

            // Source 프리팹 스캔
            if (Directory.Exists(sourceFolderPath))
            {
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { sourceFolderPath });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    
                    if (prefab != null && prefab.name.StartsWith($"{modulePrefix}{sourceTheme}_"))
                    {
                        sourcePrefabs.Add(prefab);
                    }
                }
            }

            // Target 프리팹 스캔
            if (Directory.Exists(targetFolderPath))
            {
                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { targetFolderPath });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    
                    if (prefab != null && prefab.name.StartsWith($"{modulePrefix}{targetTheme}_"))
                    {
                        targetPrefabs.Add(prefab);
                    }
                }
            }

            // 매칭 개수 계산
            foreach (var sourcePrefab in sourcePrefabs)
            {
                string expectedTargetName = sourcePrefab.name.Replace(
                    $"{modulePrefix}{sourceTheme}_",
                    $"{modulePrefix}{targetTheme}_"
                );

                if (FindTargetPrefab(expectedTargetName) != null)
                {
                    matchedCount++;
                }
            }
        }

        private GameObject FindTargetPrefab(string targetName)
        {
            foreach (var targetPrefab in targetPrefabs)
            {
                if (targetPrefab.name == targetName)
                {
                    return targetPrefab;
                }
            }
            return null;
        }

        private void GenerateMappingAsset()
        {
            // 출력 폴더 확인
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
                AssetDatabase.Refresh();
            }

            // ScriptableObject 생성
            ModuleThemeMapping mapping = ScriptableObject.CreateInstance<ModuleThemeMapping>();
            mapping.themeName = targetTheme;
            mapping.sourceTheme = sourceTheme;
            mapping.modulePrefix = modulePrefix;
            mapping.mappings = new List<ModulePrefabMapping>();

            // 매핑 추가
            foreach (var sourcePrefab in sourcePrefabs)
            {
                string expectedTargetName = sourcePrefab.name.Replace(
                    $"{modulePrefix}{sourceTheme}_",
                    $"{modulePrefix}{targetTheme}_"
                );

                GameObject targetPrefab = FindTargetPrefab(expectedTargetName);
                
                ModulePrefabMapping prefabMapping = new ModulePrefabMapping
                {
                    sourcePrefabName = sourcePrefab.name,
                    targetPrefab = targetPrefab
                };

                mapping.mappings.Add(prefabMapping);
            }

            // 파일로 저장
            string assetPath = $"{outputFolderPath}/{targetTheme}ThemeMapping.asset";
            AssetDatabase.CreateAsset(mapping, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 저장된 에셋 선택
            EditorGUIUtility.PingObject(mapping);
            Selection.activeObject = mapping;
            EditorUtility.DisplayDialog(
                "Success",
                $"Mapping asset created successfully!\n\n" +
                $"File: {assetPath}\n" +
                $"Mappings: {matchedCount} / {sourcePrefabs.Count}",
                "OK"
            );
        }

        private string GetRelativePath(string absolutePath)
        {
            if (absolutePath.StartsWith(Application.dataPath))
            {
                return "Assets" + absolutePath.Substring(Application.dataPath.Length);
            }
            return absolutePath;
        }
    }
}

