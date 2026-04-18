using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using LevelDesign;

namespace LevelDesignEditor
{
    /// <summary>
    /// Scene에 배치된 모듈 프리팹을 다른 테마로 교체하는 Editor Tool
    /// Tools → Module System → Replace Theme
    /// </summary>
    public class ModulePrefabReplacer : EditorWindow
    {
        private ModuleThemeMapping mappingAsset;
        private string searchPrefix = "Md_";
        private bool keepTransform = true;
        private bool keepLayerAndTag = true;
        private bool registerUndo = true;
        private bool searchInActiveScene = true;
        private bool searchInSelectedOnly = false;

        private List<GameObject> foundModules = new List<GameObject>();
        private List<ReplaceInfo> replaceInfos = new List<ReplaceInfo>();
        private Vector2 scrollPosition;
        private bool hasScanned = false;

        [MenuItem("Tools/Module System/Replace Theme")]
        public static void ShowWindow()
        {
            var window = GetWindow<ModulePrefabReplacer>("Theme Replacer");
            window.minSize = new Vector2(500, 700);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Module Theme Replacer", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Mapping Asset 선택
            GUILayout.Label("Mapping Table", EditorStyles.boldLabel);
            mappingAsset = (ModuleThemeMapping)EditorGUILayout.ObjectField(
                "Theme Mapping",
                mappingAsset,
                typeof(ModuleThemeMapping),
                false
            );

            if (mappingAsset != null)
            {
                EditorGUILayout.HelpBox(
                    $"Theme: {mappingAsset.themeName}\n" +
                    $"Source: {mappingAsset.sourceTheme}\n" +
                    $"Mappings: {mappingAsset.GetValidMappingCount()} / {mappingAsset.GetMappingCount()}",
                    MessageType.Info
                );
            }

            GUILayout.Space(10);

            // 검색 설정
            GUILayout.Label("Search Settings", EditorStyles.boldLabel);
            searchPrefix = EditorGUILayout.TextField("Search Prefix", searchPrefix);

            EditorGUILayout.BeginHorizontal();
            searchInActiveScene = EditorGUILayout.ToggleLeft("Active Scene", searchInActiveScene, GUILayout.Width(120));
            searchInSelectedOnly = EditorGUILayout.ToggleLeft("Selected Only", searchInSelectedOnly);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 옵션
            GUILayout.Label("Options", EditorStyles.boldLabel);
            keepTransform = EditorGUILayout.Toggle("Keep Transform", keepTransform);
            keepLayerAndTag = EditorGUILayout.Toggle("Keep Layer & Tag", keepLayerAndTag);
            registerUndo = EditorGUILayout.Toggle("Register Undo", registerUndo);

            GUILayout.Space(10);

            // 스캔 버튼
            GUI.enabled = mappingAsset != null;
            if (GUILayout.Button("Scan Modules", GUILayout.Height(30)))
            {
                ScanModules();
            }
            GUI.enabled = true;

            GUILayout.Space(10);

            // 스캔 결과 표시
            if (hasScanned)
            {
                GUILayout.Label("Scan Results", EditorStyles.boldLabel);
                
                if (foundModules.Count == 0)
                {
                    EditorGUILayout.HelpBox("No modules found with prefix: " + searchPrefix, MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        $"Found Modules: {foundModules.Count}\n" +
                        $"Can Replace: {replaceInfos.Count}",
                        replaceInfos.Count > 0 ? MessageType.Info : MessageType.Warning
                    );

                    GUILayout.Space(5);

                    // 교체 가능 항목 표시
                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

                    foreach (var info in replaceInfos)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        
                        EditorGUILayout.BeginHorizontal();
                        
                        // 상태 아이콘
                        string statusIcon = info.targetPrefab != null ? "✓" : "✗";
                        Color statusColor = info.targetPrefab != null ? Color.green : Color.red;
                        
                        GUIStyle iconStyle = new GUIStyle(EditorStyles.label);
                        iconStyle.normal.textColor = statusColor;
                        iconStyle.fontStyle = FontStyle.Bold;
                        
                        EditorGUILayout.LabelField(statusIcon, iconStyle, GUILayout.Width(20));
                        
                        // 원본 오브젝트
                        EditorGUILayout.ObjectField(info.sourceObject, typeof(GameObject), true, GUILayout.Width(180));
                        
                        EditorGUILayout.LabelField("→", GUILayout.Width(20));
                        
                        // 대상 프리팹
                        if (info.targetPrefab != null)
                        {
                            EditorGUILayout.ObjectField(info.targetPrefab, typeof(GameObject), false, GUILayout.Width(180));
                        }
                        else
                        {
                            EditorGUILayout.LabelField("(No Mapping)", EditorStyles.helpBox);
                        }
                        
                        EditorGUILayout.EndHorizontal();
                        
                        // 위치 정보
                        EditorGUILayout.LabelField(
                            $"Position: {info.sourceObject.transform.position}",
                            EditorStyles.miniLabel
                        );
                        
                        EditorGUILayout.EndVertical();
                        GUILayout.Space(2);
                    }

                    EditorGUILayout.EndScrollView();

                    GUILayout.Space(10);

                    // 교체 버튼
                    int validCount = 0;
                    foreach (var info in replaceInfos)
                    {
                        if (info.targetPrefab != null)
                            validCount++;
                    }

                    GUI.enabled = validCount > 0;
                    
                    if (GUILayout.Button($"Replace {validCount} Modules", GUILayout.Height(40)))
                    {
                        if (EditorUtility.DisplayDialog(
                            "Confirm Replace",
                            $"Replace {validCount} modules with {mappingAsset.themeName} theme?\n\n" +
                            (registerUndo ? "You can undo this operation (Ctrl+Z)." : "This operation cannot be undone!"),
                            "Replace",
                            "Cancel"))
                        {
                            ReplaceModules();
                        }
                    }
                    
                    GUI.enabled = true;
                }
            }
        }

        private void ScanModules()
        {
            foundModules.Clear();
            replaceInfos.Clear();
            hasScanned = true;

            GameObject[] objectsToSearch;

            // 검색 대상 결정
            if (searchInSelectedOnly)
            {
                objectsToSearch = Selection.gameObjects;
            }
            else if (searchInActiveScene)
            {
                objectsToSearch = UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                    .GetRootGameObjects();
                
                // 하위 오브젝트 포함
                List<GameObject> allObjects = new List<GameObject>();
                foreach (var root in objectsToSearch)
                {
                    allObjects.Add(root);
                    allObjects.AddRange(GetAllChildren(root));
                }
                objectsToSearch = allObjects.ToArray();
            }
            else
            {
                objectsToSearch = FindObjectsOfType<GameObject>();
            }

            // 모듈 프리팹 필터링
            foreach (var obj in objectsToSearch)
            {
                if (obj.name.StartsWith(searchPrefix))
                {
                    foundModules.Add(obj);

                    // 프리팹 확인
                    PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(obj);
                    if (prefabType != PrefabAssetType.NotAPrefab)
                    {
                        GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                        if (sourcePrefab != null)
                        {
                            GameObject targetPrefab = mappingAsset.GetTargetPrefab(sourcePrefab.name);
                            
                            ReplaceInfo info = new ReplaceInfo
                            {
                                sourceObject = obj,
                                sourcePrefab = sourcePrefab,
                                targetPrefab = targetPrefab
                            };
                            
                            replaceInfos.Add(info);
                        }
                    }
                }
            }
        }

        private void ReplaceModules()
        {
            int successCount = 0;
            int failCount = 0;

            foreach (var info in replaceInfos)
            {
                if (info.targetPrefab == null)
                {
                    failCount++;
                    continue;
                }

                try
                {
                    // Undo 등록
                    if (registerUndo)
                    {
                        Undo.RegisterCompleteObjectUndo(info.sourceObject, "Replace Module Prefab");
                    }

                    // Transform 정보 백업
                    Vector3 position = info.sourceObject.transform.position;
                    Quaternion rotation = info.sourceObject.transform.rotation;
                    Vector3 scale = info.sourceObject.transform.localScale;
                    Transform parent = info.sourceObject.transform.parent;
                    int siblingIndex = info.sourceObject.transform.GetSiblingIndex();
                    int layer = info.sourceObject.layer;
                    string tag = info.sourceObject.tag;

                    // 새 인스턴스 생성
                    GameObject newInstance = (GameObject)PrefabUtility.InstantiatePrefab(info.targetPrefab);

                    // Transform 복원
                    if (keepTransform)
                    {
                        newInstance.transform.position = position;
                        newInstance.transform.rotation = rotation;
                        newInstance.transform.localScale = scale;
                        newInstance.transform.SetParent(parent);
                        newInstance.transform.SetSiblingIndex(siblingIndex);
                    }

                    // Layer & Tag 복원
                    if (keepLayerAndTag)
                    {
                        newInstance.layer = layer;
                        if (!string.IsNullOrEmpty(tag))
                        {
                            newInstance.tag = tag;
                        }
                    }

                    // Undo 등록 (생성)
                    if (registerUndo)
                    {
                        Undo.RegisterCreatedObjectUndo(newInstance, "Replace Module Prefab");
                    }

                    // 원본 삭제
                    if (registerUndo)
                    {
                        Undo.DestroyObjectImmediate(info.sourceObject);
                    }
                    else
                    {
                        DestroyImmediate(info.sourceObject);
                    }

                    successCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[PrefabReplacer] 교체 실패: {info.sourceObject.name} - {e.Message}");
                    failCount++;
                }
            }

            // 결과 표시
            string message = $"Replacement complete!\n\n" +
                             $"Success: {successCount}\n" +
                             $"Failed: {failCount}";

            EditorUtility.DisplayDialog("Replace Complete", message, "OK");
            // 재스캔
            hasScanned = false;
            ScanModules();
        }

        private List<GameObject> GetAllChildren(GameObject parent)
        {
            List<GameObject> children = new List<GameObject>();
            
            foreach (Transform child in parent.transform)
            {
                children.Add(child.gameObject);
                children.AddRange(GetAllChildren(child.gameObject));
            }
            
            return children;
        }

        private class ReplaceInfo
        {
            public GameObject sourceObject;
            public GameObject sourcePrefab;
            public GameObject targetPrefab;
        }
    }
}

