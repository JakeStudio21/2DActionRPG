using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// 스프라이트 이미지를 로드하여 Shadow가 포함된 스프라이트와 함께 프리팹을 생성하는 에디터 툴
/// </summary>
public class PrefabCreatorTool : EditorWindow
{
    // 스프라이트 리스트
    private List<Sprite> sprites = new List<Sprite>();
    
    // 프리팹 저장 경로
    private string savePath = "Assets/Prefabs/Generated";
    
    // 스크롤 위치
    private Vector2 scrollPosition;
    
    // 생성 결과 메시지
    private string resultMessage = "";
    
    [MenuItem("Tools/Prefab Creator")]
    public static void ShowWindow()
    {
        var window = GetWindow<PrefabCreatorTool>("Prefab Creator");
        window.minSize = new Vector2(400, 500);
    }
    
    void OnGUI()
    {
        GUILayout.Label("Prefab Creator Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // 저장 경로 설정
        DrawSavePathSection();
        GUILayout.Space(10);
        
        // 스프라이트 로드 섹션
        DrawSpriteLoadSection();
        GUILayout.Space(10);
        
        // 스프라이트 리스트 표시
        DrawSpriteListSection();
        GUILayout.Space(10);
        
        // 프리팹 생성 버튼
        DrawCreatePrefabSection();
        GUILayout.Space(10);
        
        // 결과 메시지
        DrawResultSection();
    }
    
    /// <summary>
    /// 저장 경로 설정 섹션
    /// </summary>
    private void DrawSavePathSection()
    {
        GUILayout.Label("저장 경로 설정", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        savePath = EditorGUILayout.TextField("Prefab 저장 경로:", savePath);
        
        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string path = EditorUtility.OpenFolderPanel("프리팹 저장 폴더 선택", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                // Unity 프로젝트 경로로 변환
                if (path.StartsWith(Application.dataPath))
                {
                    savePath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
        // 경로 유효성 표시
        if (!string.IsNullOrEmpty(savePath) && !AssetDatabase.IsValidFolder(savePath))
        {
            EditorGUILayout.HelpBox("지정된 경로가 존재하지 않습니다. 프리팹 생성 시 자동으로 생성됩니다.", MessageType.Warning);
        }
    }
    
    /// <summary>
    /// 스프라이트 로드 섹션
    /// </summary>
    private void DrawSpriteLoadSection()
    {
        GUILayout.Label("스프라이트 로드", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox("스프라이트 이미지를 아래 영역에 드래그 앤 드롭하거나, 'Add Sprites' 버튼을 사용하세요.", MessageType.Info);
        
        // 드래그 앤 드롭 영역
        Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "드래그 앤 드롭 영역");
        
        Event evt = Event.current;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition))
                    break;
                
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (Object draggedObject in DragAndDrop.objectReferences)
                    {
                        if (draggedObject is Sprite sprite)
                        {
                            if (!sprites.Contains(sprite))
                            {
                                sprites.Add(sprite);
                            }
                        }
                        else if (draggedObject is Texture2D)
                        {
                            // Texture2D에서 스프라이트 추출
                            string path = AssetDatabase.GetAssetPath(draggedObject);
                            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                            foreach (Object asset in assets)
                            {
                                if (asset is Sprite s && !sprites.Contains(s))
                                {
                                    sprites.Add(s);
                                }
                            }
                        }
                    }
                }
                break;
        }
        
        // 버튼으로 추가
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Sprites"))
        {
            resultMessage = "";
            // Object Picker 열기
            EditorGUIUtility.ShowObjectPicker<Sprite>(null, false, "", 0);
        }
        
        if (GUILayout.Button("Clear All"))
        {
            sprites.Clear();
            resultMessage = "";
        }
        EditorGUILayout.EndHorizontal();
        
        // Object Picker 결과 처리
        if (Event.current.commandName == "ObjectSelectorClosed")
        {
            Sprite selectedSprite = EditorGUIUtility.GetObjectPickerObject() as Sprite;
            if (selectedSprite != null && !sprites.Contains(selectedSprite))
            {
                sprites.Add(selectedSprite);
            }
        }
    }
    
    /// <summary>
    /// 스프라이트 리스트 표시 섹션
    /// </summary>
    private void DrawSpriteListSection()
    {
        GUILayout.Label($"로드된 스프라이트 ({sprites.Count}개)", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        
        for (int i = sprites.Count - 1; i >= 0; i--)
        {
            if (sprites[i] == null)
            {
                sprites.RemoveAt(i);
                continue;
            }
            
            EditorGUILayout.BeginHorizontal();
            
            // 스프라이트 이름 표시
            EditorGUILayout.LabelField(sprites[i].name, GUILayout.Width(300));
            
            // Shadow 여부 표시
            if (sprites[i].name.Contains("Shadow"))
            {
                EditorGUILayout.LabelField("[Shadow]", GUILayout.Width(60));
            }
            
            // 제거 버튼
            if (GUILayout.Button("X", GUILayout.Width(30)))
            {
                sprites.RemoveAt(i);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    /// <summary>
    /// 프리팹 생성 섹션
    /// </summary>
    private void DrawCreatePrefabSection()
    {
        EditorGUI.BeginDisabledGroup(sprites.Count == 0);
        
        if (GUILayout.Button("프리팹 생성", GUILayout.Height(40)))
        {
            CreatePrefabs();
        }
        
        EditorGUI.EndDisabledGroup();
    }
    
    /// <summary>
    /// 결과 메시지 섹션
    /// </summary>
    private void DrawResultSection()
    {
        if (!string.IsNullOrEmpty(resultMessage))
        {
            EditorGUILayout.HelpBox(resultMessage, MessageType.Info);
        }
    }
    
    /// <summary>
    /// 프리팹 생성 메인 로직
    /// </summary>
    private void CreatePrefabs()
    {
        // 스프라이트를 메인과 Shadow로 분류
        Dictionary<string, Sprite> mainSprites = new Dictionary<string, Sprite>();
        Dictionary<string, Sprite> shadowSprites = new Dictionary<string, Sprite>();
        
        foreach (Sprite sprite in sprites)
        {
            if (sprite.name.Contains("Shadow"))
            {
                // Shadow 스프라이트: "Shadow" 제거하여 키 생성
                string baseName = sprite.name.Replace("Shadow", "").Replace("_", "").Trim('_');
                
                // 더 정확한 매칭을 위해 원본 이름에서 Shadow만 제거
                string key = sprite.name.Replace("_Shadow", "").Replace("Shadow_", "").Replace("Shadow", "");
                shadowSprites[key] = sprite;
            }
            else
            {
                // 메인 스프라이트
                mainSprites[sprite.name] = sprite;
            }
        }
        
        // Shadow가 있는 메인 스프라이트만 프리팹 생성
        int createdCount = 0;
        int skippedCount = 0;
        List<string> createdPrefabs = new List<string>();
        
        foreach (var kvp in mainSprites)
        {
            string mainName = kvp.Key;
            Sprite mainSprite = kvp.Value;
            
            // 매칭되는 Shadow 찾기
            if (shadowSprites.ContainsKey(mainName))
            {
                Sprite shadowSprite = shadowSprites[mainName];
                
                // 프리팹 생성
                string prefabPath = CreatePrefabWithShadow(mainName, mainSprite, shadowSprite);
                
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    createdCount++;
                    createdPrefabs.Add(Path.GetFileName(prefabPath));
                }
            }
            else
            {
                skippedCount++;
                Debug.Log($"Shadow가 없어서 스킵: {mainName}");
            }
        }
        
        // 결과 메시지
        resultMessage = $"프리팹 생성 완료!\n생성됨: {createdCount}개\n스킵됨: {skippedCount}개\n\n";
        if (createdPrefabs.Count > 0)
        {
            resultMessage += "생성된 프리팹:\n" + string.Join("\n", createdPrefabs);
        }
        
        Debug.Log($"프리팹 생성 완료: {createdCount}개 생성, {skippedCount}개 스킵");
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    
    /// <summary>
    /// 메인 스프라이트와 Shadow를 포함하는 프리팹 생성
    /// </summary>
    private string CreatePrefabWithShadow(string prefabName, Sprite mainSprite, Sprite shadowSprite)
    {
        // 저장 경로 확인 및 생성
        if (!AssetDatabase.IsValidFolder(savePath))
        {
            CreateFolderRecursive(savePath);
        }
        
        // Root GameObject 생성
        GameObject root = new GameObject(prefabName);
        
        // 메인 스프라이트 GameObject 생성
        GameObject mainObj = new GameObject(mainSprite.name);
        mainObj.transform.SetParent(root.transform);
        SpriteRenderer mainRenderer = mainObj.AddComponent<SpriteRenderer>();
        mainRenderer.sprite = mainSprite;
        
        // Shadow 스프라이트 GameObject 생성
        GameObject shadowObj = new GameObject(shadowSprite.name);
        shadowObj.transform.SetParent(root.transform);
        SpriteRenderer shadowRenderer = shadowObj.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = shadowSprite;
        
        // 프리팹 경로 생성 (중복 시 이름 변경)
        string prefabPath = GetUniquePrefabPath(savePath, prefabName);
        
        // 프리팹 저장
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        
        // 씬에서 임시 GameObject 삭제
        DestroyImmediate(root);
        
        Debug.Log($"프리팹 생성: {prefabPath}");
        
        return prefabPath;
    }
    
    /// <summary>
    /// 폴더 재귀적 생성
    /// </summary>
    private void CreateFolderRecursive(string path)
    {
        string[] folders = path.Split('/');
        string currentPath = folders[0];
        
        for (int i = 1; i < folders.Length; i++)
        {
            string nextPath = currentPath + "/" + folders[i];
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, folders[i]);
            }
            currentPath = nextPath;
        }
    }
    
    /// <summary>
    /// 중복되지 않는 프리팹 경로 생성
    /// </summary>
    private string GetUniquePrefabPath(string folder, string name)
    {
        string path = $"{folder}/{name}.prefab";
        
        if (!File.Exists(path))
        {
            return path;
        }
        
        // 중복 시 _1, _2 등 추가
        int counter = 1;
        while (File.Exists($"{folder}/{name}_{counter}.prefab"))
        {
            counter++;
        }
        
        return $"{folder}/{name}_{counter}.prefab";
    }
}


