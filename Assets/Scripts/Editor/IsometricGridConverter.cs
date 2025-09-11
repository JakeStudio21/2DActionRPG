#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// 씬의 모든 Grid를 아이소메트릭으로 변환하는 에디터 도구
/// </summary>
public class IsometricGridConverter : EditorWindow
{
    [MenuItem("Tools/Isometric/Convert All Grids to Isometric")]
    public static void ShowWindow()
    {
        GetWindow<IsometricGridConverter>("Grid Converter");
    }

    [MenuItem("Tools/Isometric/Show Project Settings")]
    public static void ShowProjectSettings()
    {
        SettingsService.OpenProjectSettings("Project/Graphics");
        Debug.Log("📋 [IsometricConverter] Graphics 설정을 열었습니다.");
        Debug.Log("   → Transparency Sort Mode를 'Custom Axis'로 변경하세요.");
        Debug.Log("   → Transparency Sort Axis를 (0, 1, 0)으로 설정하세요.");
    }

    private void OnGUI()
    {
        GUILayout.Label("Grid → Isometric 변환 도구", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        // Project Settings 바로가기
        if (GUILayout.Button("Project Settings → Graphics 열기", GUILayout.Height(30)))
        {
            ShowProjectSettings();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("현재 씬의 모든 Grid 변환", GUILayout.Height(40)))
        {
            ConvertCurrentSceneGrids();
        }
        
        if (GUILayout.Button("모든 씬의 Grid 변환", GUILayout.Height(40)))
        {
            ConvertAllSceneGrids();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "1. 먼저 'Project Settings → Graphics 열기' 버튼을 클릭하세요.\n" +
            "2. Transparency Sort Mode를 'Custom Axis'로 변경하세요.\n" +
            "3. Transparency Sort Axis를 (0, 1, 0)으로 설정하세요.\n" +
            "4. 그 다음 Grid 변환 버튼을 사용하세요.", 
            MessageType.Info
        );
    }
    
    private void ConvertCurrentSceneGrids()
    {
        Grid[] grids = Object.FindObjectsOfType<Grid>();
        int converted = 0;
        
        foreach (Grid grid in grids)
        {
            if (grid.cellLayout != Grid.CellLayout.IsometricZAsY)
            {
                Undo.RecordObject(grid, "Convert to Isometric");
                grid.cellLayout = Grid.CellLayout.IsometricZAsY;
                grid.cellSize = new Vector3(1f, 1f, 0f);
                converted++;
                
                EditorUtility.SetDirty(grid);
                Debug.Log($"🔄 [IsometricConverter] {grid.name} Grid를 Isometric으로 변경했습니다.");
            }
        }
        
        if (converted > 0)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        
        Debug.Log($"✅ [IsometricConverter] 현재 씬에서 {converted}개의 Grid를 변환했습니다.");
    }
    
    private void ConvertAllSceneGrids()
    {
        if (!EditorUtility.DisplayDialog("모든 씬 변환", 
            "프로젝트의 모든 씬을 변환하시겠습니까?\n이 작업은 되돌릴 수 없습니다.", 
            "변환", "취소"))
        {
            return;
        }
        
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
        int totalConverted = 0;
        
        // 현재 씬 저장
        string currentScenePath = SceneManager.GetActiveScene().path;
        
        for (int i = 0; i < sceneGuids.Length; i++)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
            
            // Progress Bar 표시
            EditorUtility.DisplayProgressBar("Grid 변환", 
                $"변환 중: {System.IO.Path.GetFileName(scenePath)}", 
                (float)i / sceneGuids.Length);
            
            // 씬 열기
            var scene = EditorSceneManager.OpenScene(scenePath);
            
            Grid[] grids = Object.FindObjectsOfType<Grid>();
            bool sceneChanged = false;
            
            foreach (Grid grid in grids)
            {
                if (grid.cellLayout != Grid.CellLayout.IsometricZAsY)
                {
                    grid.cellLayout = Grid.CellLayout.IsometricZAsY;
                    grid.cellSize = new Vector3(1f, 1f, 0f);
                    EditorUtility.SetDirty(grid);
                    totalConverted++;
                    sceneChanged = true;
                }
            }
            
            if (sceneChanged)
            {
                EditorSceneManager.SaveScene(scene);
            }
        }
        
        EditorUtility.ClearProgressBar();
        
        // 원래 씬으로 돌아가기
        if (!string.IsNullOrEmpty(currentScenePath))
        {
            EditorSceneManager.OpenScene(currentScenePath);
        }
        
        Debug.Log($"🎉 [IsometricConverter] 전체 프로젝트에서 {totalConverted}개의 Grid를 변환했습니다.");
        
        EditorUtility.DisplayDialog("변환 완료", 
            $"{totalConverted}개의 Grid가 성공적으로 변환되었습니다.", 
            "확인");
    }
}
#endif
