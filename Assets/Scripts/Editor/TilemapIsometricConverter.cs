#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

/// <summary>
/// 타일맵 아이소메트릭 변환 도구 (좌표/배치/레이어 분리 전용)
/// 주의: 아트 자체는 변환하지 않음 - 아이소메트릭 타일셋이 별도 필요
/// </summary>
public class TilemapIsometricConverter : EditorWindow
{
    [MenuItem("Tools/Isometric/Convert Tilemaps")]
    public static void ShowWindow()
    {
        GetWindow<TilemapIsometricConverter>("Tilemap Converter");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("타일맵 아이소메트릭 변환", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "이 도구는 타일맵의 배치와 레이어만 변환합니다.\n" +
            "아이소메트릭 타일셋(2:1 다이아몬드)은 별도로 준비해야 합니다.", 
            MessageType.Info
        );
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("1. 현재 씬 Grid → Isometric", GUILayout.Height(30)))
        {
            ConvertSceneGrids();
        }
        
        if (GUILayout.Button("2. 타일맵 레이어 분리", GUILayout.Height(30)))
        {
            SeparateTilemapLayers();
        }
        
        if (GUILayout.Button("3. 소팅 레이어 설정", GUILayout.Height(30)))
        {
            SetupSortingLayers();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("전체 변환 실행", GUILayout.Height(40)))
        {
            ConvertAllTilemaps();
        }
    }
    
    /// <summary>
    /// 씬의 모든 Grid를 Isometric으로 변환
    /// </summary>
    private void ConvertSceneGrids()
    {
        Grid[] grids = FindObjectsOfType<Grid>();
        int converted = 0;
        
        foreach (Grid grid in grids)
        {
            if (grid.cellLayout != Grid.CellLayout.IsometricZAsY)
            {
                Undo.RecordObject(grid, "Convert Grid to Isometric");
                grid.cellLayout = Grid.CellLayout.IsometricZAsY;
                grid.cellSize = new Vector3(1f, 1f, 0f);
                converted++;
                
                EditorUtility.SetDirty(grid);
            }
        }
    }
    
    /// <summary>
    /// 타일맵 레이어 분리 (Ground/Decoration/Background)
    /// </summary>
    private void SeparateTilemapLayers()
    {
        Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
        
        foreach (Tilemap tilemap in tilemaps)
        {
            TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
            if (renderer == null) continue;
            
            // 이름 기반으로 레이어 자동 분류
            string tilemapName = tilemap.name.ToLower();
            
            if (tilemapName.Contains("ground") || tilemapName.Contains("floor"))
            {
                SetupGroundLayer(tilemap, renderer);
            }
            else if (tilemapName.Contains("wall") || tilemapName.Contains("decoration") || tilemapName.Contains("tree"))
            {
                SetupDecorationLayer(tilemap, renderer);
            }
            else if (tilemapName.Contains("background") || tilemapName.Contains("pattern"))
            {
                SetupBackgroundLayer(tilemap, renderer);
            }
            else
            {
                // 기본값: Ground Layer
                SetupGroundLayer(tilemap, renderer);
                Debug.LogWarning($"⚠️ [TilemapConverter] {tilemap.name}을 Ground Layer로 분류했습니다.");
            }
        }
    }
    
    /// <summary>
    /// Ground Layer 설정 (충돌 포함)
    /// </summary>
    private void SetupGroundLayer(Tilemap tilemap, TilemapRenderer renderer)
    {
        Undo.RecordObjects(new Object[] { tilemap, renderer }, "Setup Ground Layer");
        
        // Sorting Layer 설정
        renderer.sortingLayerName = "Ground";
        renderer.sortingOrder = 0;
        
        // 충돌체 추가/설정
        TilemapCollider2D collider = tilemap.GetComponent<TilemapCollider2D>();
        if (collider == null)
        {
            collider = tilemap.gameObject.AddComponent<TilemapCollider2D>();
        }
        collider.usedByComposite = true;
        
        // CompositeCollider2D 추가 (성능 최적화)
        CompositeCollider2D composite = tilemap.GetComponent<CompositeCollider2D>();
        if (composite == null)
        {
            composite = tilemap.gameObject.AddComponent<CompositeCollider2D>();
            Rigidbody2D rb = tilemap.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = tilemap.gameObject.AddComponent<Rigidbody2D>();
            }
            rb.bodyType = RigidbodyType2D.Static;
        }
        
        // 레이어 설정
        tilemap.gameObject.layer = LayerMask.NameToLayer("Ground");
        
        EditorUtility.SetDirty(tilemap);
        EditorUtility.SetDirty(renderer);
    }
    
    /// <summary>
    /// Decoration Layer 설정 (오클루더 대상)
    /// </summary>
    private void SetupDecorationLayer(Tilemap tilemap, TilemapRenderer renderer)
    {
        Undo.RecordObjects(new Object[] { tilemap, renderer }, "Setup Decoration Layer");
        
        // Sorting Layer 설정
        renderer.sortingLayerName = "Decoration";
        renderer.sortingOrder = 100; // Ground보다 위에
        
        // 오클루더 태그 추가
        if (!tilemap.gameObject.CompareTag("Occluder"))
        {
            tilemap.gameObject.tag = "Occluder";
        }
        
        // 충돌체 제거 (장식용이므로 충돌 불필요)
        TilemapCollider2D collider = tilemap.GetComponent<TilemapCollider2D>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        // 레이어 설정
        tilemap.gameObject.layer = LayerMask.NameToLayer("Decoration");
        
        EditorUtility.SetDirty(tilemap);
        EditorUtility.SetDirty(renderer);
    }
    
    /// <summary>
    /// Background Layer 설정
    /// </summary>
    private void SetupBackgroundLayer(Tilemap tilemap, TilemapRenderer renderer)
    {
        Undo.RecordObjects(new Object[] { tilemap, renderer }, "Setup Background Layer");
        
        // Sorting Layer 설정
        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = -100; // 최하위
        
        // 충돌체 제거
        TilemapCollider2D collider = tilemap.GetComponent<TilemapCollider2D>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        // 레이어 설정
        tilemap.gameObject.layer = LayerMask.NameToLayer("Background");
        
        EditorUtility.SetDirty(tilemap);
        EditorUtility.SetDirty(renderer);
    }
    
    /// <summary>
    /// Sorting Layer 자동 생성
    /// </summary>
    private void SetupSortingLayers()
    {
        // Sorting Layer는 코드로 생성 불가, 수동 안내
        // Project Settings 창 열기
        SettingsService.OpenProjectSettings("Project/Tags and Layers");
    }
    
    /// <summary>
    /// 전체 타일맵 변환 실행
    /// </summary>
    private void ConvertAllTilemaps()
    {
        if (!EditorUtility.DisplayDialog("전체 타일맵 변환", 
            "현재 씬의 모든 타일맵을 아이소메트릭으로 변환하시겠습니까?\n" +
            "주의: 아트는 변환되지 않으며, 아이소메트릭 타일셋이 필요합니다.", 
            "변환", "취소"))
        {
            return;
        }
        // 1단계: Grid 변환
        ConvertSceneGrids();
        
        // 2단계: 레이어 분리
        SeparateTilemapLayers();
        
        // 3단계: 소팅 레이어 안내
        SetupSortingLayers();
        EditorUtility.DisplayDialog("변환 완료", 
            "타일맵 변환이 완료되었습니다.\n" +
            "Sorting Layer를 수동으로 설정하고,\n" +
            "아이소메트릭 타일셋으로 교체해주세요.", 
            "확인");
    }
}
#endif
