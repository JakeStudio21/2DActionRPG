#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace StageSystem
{
    /// <summary>
    /// SpawnPoint 커스텀 에디터
    /// Inspector 기능 확장 및 씬 편집 도구
    /// </summary>
    [CustomEditor(typeof(SpawnPoint))]
    public class SpawnPointEditor : UnityEditor.Editor
    {
        private SpawnPoint spawnPoint;
        
        public override void OnInspectorGUI()
        {
            spawnPoint = (SpawnPoint)target;
            
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            DrawCustomButtons();
            
            EditorGUILayout.Space();
            DrawInfoPanel();
        }
        
        /// <summary>
        /// 커스텀 버튼들
        /// </summary>
        private void DrawCustomButtons()
        {
            EditorGUILayout.LabelField("빠른 설정", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Point 타입"))
            {
                SetSpawnType(SpawnType.Point);
            }
            
            if (GUILayout.Button("Circle 타입"))
            {
                SetSpawnType(SpawnType.Circle);
            }
            
            if (GUILayout.Button("Rectangle 타입"))
            {
                SetSpawnType(SpawnType.Rectangle);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("기본 설정"))
            {
                SetDefaultSettings();
            }
            
            if (GUILayout.Button("테스트 스폰"))
            {
                TestSpawn();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// 정보 패널
        /// </summary>
        private void DrawInfoPanel()
        {
            EditorGUILayout.LabelField("스폰 포인트 정보", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("ID", spawnPoint.spawnPointID);
            EditorGUILayout.EnumPopup("타입", spawnPoint.spawnType);
            EditorGUILayout.Toggle("활성화", spawnPoint.isActive);
            EditorGUI.EndDisabledGroup();
            
            // 예상 스폰 영역 크기
            float area = CalculateSpawnArea();
            EditorGUILayout.LabelField("스폰 영역", $"{area:F1} 평방 유닛");
        }
        
        /// <summary>
        /// 스폰 타입 설정
        /// </summary>
        private void SetSpawnType(SpawnType type)
        {
            Undo.RecordObject(spawnPoint, "Change Spawn Type");
            spawnPoint.spawnType = type;
            
            // 타입별 기본값 설정
            switch (type)
            {
                case SpawnType.Point:
                    spawnPoint.radius = 0f;
                    break;
                case SpawnType.Circle:
                case SpawnType.Area:
                    spawnPoint.radius = 2f;
                    break;
                case SpawnType.Rectangle:
                    spawnPoint.rectangleSize = new Vector2(3f, 3f);
                    break;
            }
            
            EditorUtility.SetDirty(spawnPoint);
        }
        
        /// <summary>
        /// 기본 설정 적용
        /// </summary>
        private void SetDefaultSettings()
        {
            Undo.RecordObject(spawnPoint, "Set Default Settings");
            
            spawnPoint.minDistanceFromPlayer = 3f;
            spawnPoint.minDistanceFromOtherEnemies = 1f;
            spawnPoint.isActive = true;
            spawnPoint.showGizmos = true;
            
            EditorUtility.SetDirty(spawnPoint);
        }
        
        /// <summary>
        /// 테스트 스폰 (에디터에서만)
        /// </summary>
        private void TestSpawn()
        {
            Vector3 testPos = spawnPoint.GetSafeSpawnPosition();
            Debug.Log($"[SpawnPointEditor] {spawnPoint.spawnPointID} 테스트 스폰 위치: {testPos}");
            
            // Scene View에서 임시 표시
            SceneView.RepaintAll();
        }
        
        /// <summary>
        /// 스폰 영역 크기 계산
        /// </summary>
        private float CalculateSpawnArea()
        {
            switch (spawnPoint.spawnType)
            {
                case SpawnType.Point:
                    return 0f;
                case SpawnType.Circle:
                case SpawnType.Area:
                    return Mathf.PI * spawnPoint.radius * spawnPoint.radius;
                case SpawnType.Rectangle:
                    return spawnPoint.rectangleSize.x * spawnPoint.rectangleSize.y;
                default:
                    return 0f;
            }
        }
        
        /// <summary>
        /// Scene View에서 핸들 그리기
        /// </summary>
        private void OnSceneGUI()
        {
            if (!spawnPoint.showGizmos) return;
            
            Handles.color = spawnPoint.gizmoColor;
            Vector3 position = spawnPoint.transform.position;
            
            // 타입별 핸들 그리기
            switch (spawnPoint.spawnType)
            {
                case SpawnType.Circle:
                case SpawnType.Area:
                    // 반지름 조절 핸들
                    EditorGUI.BeginChangeCheck();
                    float newRadius = Handles.RadiusHandle(Quaternion.identity, position, spawnPoint.radius);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(spawnPoint, "Change Spawn Radius");
                        spawnPoint.radius = newRadius;
                        EditorUtility.SetDirty(spawnPoint);
                    }
                    break;
                    
                case SpawnType.Rectangle:
                    // 크기 조절 핸들
                    Handles.DrawWireCube(position, new Vector3(spawnPoint.rectangleSize.x, spawnPoint.rectangleSize.y, 0));
                    break;
            }
            
            // ID 라벨 표시
            Handles.Label(position + Vector3.up * 1.5f, spawnPoint.spawnPointID);
        }
    }
    
    /// <summary>
    /// SpawnPoint 생성 메뉴
    /// </summary>
    public class SpawnPointCreator
    {
        [MenuItem("GameObject/Stage System/Create Spawn Point", false, 0)]
        public static void CreateSpawnPoint()
        {
            GameObject spawnPointObj = new GameObject("SpawnPoint");
            SpawnPoint spawnPoint = spawnPointObj.AddComponent<SpawnPoint>();
            
            // 기본 설정
            spawnPoint.spawnPointID = GenerateUniqueID();
            spawnPoint.spawnType = SpawnType.Point;
            spawnPoint.isActive = true;
            
            // Selection 설정
            Selection.activeGameObject = spawnPointObj;
            
            // Undo 지원
            Undo.RegisterCreatedObjectUndo(spawnPointObj, "Create Spawn Point");
        }
        
        private static string GenerateUniqueID()
        {
            SpawnPoint[] existingPoints = Object.FindObjectsOfType<SpawnPoint>();
            for (int i = 1; i <= 99; i++)
            {
                string candidateId = $"SP_{i:D2}";
                bool exists = false;
                foreach (var point in existingPoints)
                {
                    if (point.spawnPointID == candidateId)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    return candidateId;
                }
            }
            return "SP_01";
        }
    }
}
#endif
