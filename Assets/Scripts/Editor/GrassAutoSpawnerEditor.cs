using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// GrassAutoSpawner의 Custom Inspector
/// Preview, Generate, Clear 버튼 제공
/// </summary>
[CustomEditor(typeof(GrassAutoSpawner))]
public class GrassAutoSpawnerEditor : Editor
{
    #region Private Fields
    
    private GrassAutoSpawner spawner;
    private List<Vector2> previewPoints;
    private bool showPreview = false;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void OnEnable()
    {
        spawner = (GrassAutoSpawner)target;
        previewPoints = new List<Vector2>();
        
        // Scene View Repaint 활성화
        SceneView.duringSceneGui += OnSceneGUI;
    }
    
    private void OnDisable()
    {
        // Scene View Repaint 비활성화
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    
    #endregion
    
    #region Inspector GUI
    
    public override void OnInspectorGUI()
    {
        // 기본 Inspector 표시
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("🌿 Grass Auto Spawner", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // 에러 체크
        DrawWarnings();
        
        EditorGUILayout.Space(5);
        
        // 버튼들
        DrawButtons();
        
        EditorGUILayout.Space(5);
        
        // 정보 표시
        DrawInfo();
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", EditorStyles.boldLabel);
    }
    
    /// <summary>
    /// 경고 메시지 표시
    /// </summary>
    private void DrawWarnings()
    {
        // Collider2D 체크
        Collider2D collider = spawner.GetComponent<Collider2D>();
        if (collider == null)
        {
            EditorGUILayout.HelpBox(
                "⚠️ Collider2D가 없습니다!\n" +
                "BoxCollider2D 또는 PolygonCollider2D를 추가하세요.",
                MessageType.Error
            );
            
            if (GUILayout.Button("BoxCollider2D 추가"))
            {
                Undo.AddComponent<BoxCollider2D>(spawner.gameObject);
                BoxCollider2D box = spawner.GetComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = new Vector2(10f, 10f);
            }
            
            return;
        }
        
        // 프리팹 체크
        SerializedProperty grassPrefabsProp = serializedObject.FindProperty("grassPrefabs");
        
        if (grassPrefabsProp.arraySize == 0)
        {
            EditorGUILayout.HelpBox(
                "⚠️ Grass Prefabs가 비어있습니다!\n" +
                "배치할 프리팹을 추가하세요.",
                MessageType.Warning
            );
            return;
        }
        
        // 유효한 프리팹 체크
        bool hasValidPrefab = false;
        int totalWeight = 0;
        
        for (int i = 0; i < grassPrefabsProp.arraySize; i++)
        {
            SerializedProperty element = grassPrefabsProp.GetArrayElementAtIndex(i);
            SerializedProperty prefabProp = element.FindPropertyRelative("prefab");
            SerializedProperty weightProp = element.FindPropertyRelative("weight");
            
            if (prefabProp.objectReferenceValue != null)
            {
                hasValidPrefab = true;
                totalWeight += weightProp.intValue;
            }
        }
        
        if (!hasValidPrefab)
        {
            EditorGUILayout.HelpBox(
                "⚠️ 유효한 프리팹이 없습니다!\n" +
                "최소 1개 이상의 프리팹을 할당하세요.",
                MessageType.Warning
            );
            return;
        }
        
        // 총 가중치 표시
        EditorGUILayout.HelpBox(
            $"✅ 설정 완료!\n총 가중치: {totalWeight}",
            MessageType.Info
        );
        
        // 가중치 비율 표시
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("📊 출현 확률", EditorStyles.boldLabel);
        
        for (int i = 0; i < grassPrefabsProp.arraySize; i++)
        {
            SerializedProperty element = grassPrefabsProp.GetArrayElementAtIndex(i);
            SerializedProperty prefabProp = element.FindPropertyRelative("prefab");
            SerializedProperty weightProp = element.FindPropertyRelative("weight");
            
            GameObject prefab = prefabProp.objectReferenceValue as GameObject;
            
            if (prefab != null)
            {
                float percentage = (weightProp.intValue / (float)totalWeight) * 100f;
                EditorGUILayout.LabelField(
                    $"  • {prefab.name}: {percentage:F1}% (weight: {weightProp.intValue})"
                );
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    /// <summary>
    /// 버튼 UI
    /// </summary>
    private void DrawButtons()
    {
        // Preview 버튼
        GUI.backgroundColor = showPreview ? Color.yellow : Color.white;
        
        if (GUILayout.Button("👁️  Preview", GUILayout.Height(35)))
        {
            previewPoints = spawner.GeneratePreviewPoints();
            showPreview = true;
            SceneView.RepaintAll();
        }
        
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(3);
        
        // Generate 버튼
        GUI.backgroundColor = Color.green;
        
        bool canGenerate = spawner.GetComponent<Collider2D>() != null;
        GUI.enabled = canGenerate;
        
        if (GUILayout.Button("🌿  Generate Grass", GUILayout.Height(40)))
        {
            Undo.RecordObject(spawner, "Generate Grass");
            spawner.GenerateGrass();
            
            // 프리뷰 숨김
            showPreview = false;
            previewPoints.Clear();
            
            EditorUtility.SetDirty(spawner);
        }
        
        GUI.enabled = true;
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(3);
        
        // Clear 버튼
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        
        int spawnedCount = spawner.GetSpawnedCount();
        bool canClear = spawnedCount > 0;
        GUI.enabled = canClear;
        
        if (GUILayout.Button($"🗑️  Clear All ({spawnedCount})", GUILayout.Height(35)))
        {
            if (EditorUtility.DisplayDialog(
                "풀 전체 삭제",
                $"정말로 {spawnedCount}개의 풀을 삭제하시겠습니까?",
                "삭제",
                "취소"))
            {
                Undo.RecordObject(spawner, "Clear Grass");
                spawner.ClearAll();
                
                // 프리뷰도 숨김
                showPreview = false;
                previewPoints.Clear();
                
                EditorUtility.SetDirty(spawner);
                SceneView.RepaintAll();
            }
        }
        
        GUI.enabled = true;
        GUI.backgroundColor = Color.white;
    }
    
    /// <summary>
    /// 정보 표시
    /// </summary>
    private void DrawInfo()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        int spawnedCount = spawner.GetSpawnedCount();
        SerializedProperty spawnCountProp = serializedObject.FindProperty("spawnCount");
        int targetCount = spawnCountProp.intValue;
        
        EditorGUILayout.LabelField("ℹ️ 상태", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"  • 생성됨: {spawnedCount} 개");
        EditorGUILayout.LabelField($"  • 목표: {targetCount} 개");
        
        if (showPreview && previewPoints != null)
        {
            EditorGUILayout.LabelField($"  • 프리뷰: {previewPoints.Count} 개 (Scene View에 표시 중)");
        }
        
        EditorGUILayout.EndVertical();
        
        // 프리뷰 숨김 버튼
        if (showPreview)
        {
            EditorGUILayout.Space(3);
            
            if (GUILayout.Button("프리뷰 숨김"))
            {
                showPreview = false;
                previewPoints.Clear();
                SceneView.RepaintAll();
            }
        }
    }
    
    #endregion
    
    #region Scene GUI
    
    /// <summary>
    /// Scene View에 프리뷰 표시
    /// </summary>
    private void OnSceneGUI(SceneView sceneView)
    {
        if (!showPreview || previewPoints == null || previewPoints.Count == 0)
            return;
        
        // 초록색 반투명 원으로 프리뷰 표시
        Handles.color = new Color(0f, 1f, 0f, 0.6f);
        
        foreach (var point in previewPoints)
        {
            Handles.DrawSolidDisc(point, Vector3.forward, 0.15f);
        }
        
        // 첫 번째 포인트에 라벨 표시
        if (previewPoints.Count > 0)
        {
            Handles.color = Color.white;
            
            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = Color.green;
            style.fontSize = 14;
            
            Vector3 labelPos = previewPoints[0] + new Vector2(0f, 0.5f);
            
            Handles.Label(
                labelPos,
                $"Preview: {previewPoints.Count} 개",
                style
            );
        }
        
        // Scene View 강제 Repaint
        sceneView.Repaint();
    }
    
    #endregion
}

