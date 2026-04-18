using UnityEngine;
using UnityEditor;

namespace LevelDesignEditor
{
    /// <summary>
    /// Scene의 모듈 프리팹에 CompositeCollider2D를 자동으로 설정하는 Editor Tool
    /// Tools → Module System → Setup Composite Collider
    /// </summary>
    public class CompositeColliderSetup : EditorWindow
    {
        private string modulePrefix = "Md_";
        private string colliderMapName = "ColliderMap";
        private bool setUsedByComposite = true;
        private bool createColliderMap = true;
        private CompositeCollider2D.GeometryType geometryType = CompositeCollider2D.GeometryType.Polygons;

        [MenuItem("Tools/Module System/Setup Composite Collider")]
        public static void ShowWindow()
        {
            var window = GetWindow<CompositeColliderSetup>("Composite Collider Setup");
            window.minSize = new Vector2(400, 350);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Composite Collider Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scene의 모듈 프리팹에 CompositeCollider2D를 자동으로 설정합니다.\n" +
                "프리팹 경계에서 걸림 현상을 제거합니다.",
                MessageType.Info
            );
            GUILayout.Space(10);

            // 설정
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            modulePrefix = EditorGUILayout.TextField("Module Prefix", modulePrefix);
            colliderMapName = EditorGUILayout.TextField("Collider Map Name", colliderMapName);
            
            GUILayout.Space(10);

            // 옵션
            GUILayout.Label("Options", EditorStyles.boldLabel);
            createColliderMap = EditorGUILayout.Toggle("Create ColliderMap", createColliderMap);
            setUsedByComposite = EditorGUILayout.Toggle("Set 'Used By Composite'", setUsedByComposite);
            geometryType = (CompositeCollider2D.GeometryType)EditorGUILayout.EnumPopup("Geometry Type", geometryType);

            GUILayout.Space(10);

            // 실행 버튼
            if (GUILayout.Button("Setup Composite Collider", GUILayout.Height(40)))
            {
                SetupCompositeCollider();
            }

            GUILayout.Space(10);

            // 도움말
            EditorGUILayout.HelpBox(
                "작동 순서:\n" +
                "1. Scene의 모든 모듈 프리팹 검색\n" +
                "2. ColliderMap GameObject 생성\n" +
                "3. Rigidbody2D + CompositeCollider2D 추가\n" +
                "4. 모든 BoxCollider2D를 'Used By Composite' 설정\n" +
                "5. 모듈을 ColliderMap 하위로 이동",
                MessageType.None
            );
        }

        private void SetupCompositeCollider()
        {
            // ColliderMap 찾기 또는 생성
            GameObject colliderMapObj = GameObject.Find(colliderMapName);
            
            if (colliderMapObj == null && createColliderMap)
            {
                colliderMapObj = new GameObject(colliderMapName);
                Undo.RegisterCreatedObjectUndo(colliderMapObj, "Create ColliderMap");
            }
            else if (colliderMapObj == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"ColliderMap '{colliderMapName}'를 찾을 수 없습니다.\n" +
                    "'Create ColliderMap' 옵션을 활성화하세요.",
                    "OK"
                );
                return;
            }

            // Rigidbody2D 확인/추가
            Rigidbody2D rb = colliderMapObj.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = colliderMapObj.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
                Undo.RegisterCreatedObjectUndo(rb, "Add Rigidbody2D");
            }

            // CompositeCollider2D 확인/추가
            CompositeCollider2D compositeCollider = colliderMapObj.GetComponent<CompositeCollider2D>();
            if (compositeCollider == null)
            {
                compositeCollider = colliderMapObj.AddComponent<CompositeCollider2D>();
                compositeCollider.geometryType = geometryType;
                Undo.RegisterCreatedObjectUndo(compositeCollider, "Add CompositeCollider2D");
            }

            // Scene의 모든 루트 오브젝트 검색
            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                .GetRootGameObjects();

            int moduleCount = 0;
            int colliderCount = 0;

            // 모듈 프리팹 찾기
            foreach (var root in rootObjects)
            {
                SearchAndSetupModules(root, colliderMapObj.transform, ref moduleCount, ref colliderCount);
            }

            // 결과 표시
            string message = $"Composite Collider setup complete!\n\n" +
                             $"ColliderMap: {colliderMapName}\n" +
                             $"Modules: {moduleCount}\n" +
                             $"Colliders: {colliderCount}";

            EditorUtility.DisplayDialog("Setup Complete", message, "OK");
            // ColliderMap 선택
            Selection.activeGameObject = colliderMapObj;
            EditorGUIUtility.PingObject(colliderMapObj);
        }

        private void SearchAndSetupModules(GameObject obj, Transform colliderMapTransform, ref int moduleCount, ref int colliderCount)
        {
            // 모듈 프리팹인지 확인
            if (obj.name.StartsWith(modulePrefix))
            {
                // ColliderMap 하위로 이동
                if (obj.transform.parent != colliderMapTransform)
                {
                    Undo.SetTransformParent(obj.transform, colliderMapTransform, "Move to ColliderMap");
                    moduleCount++;
                }

                // 하위 BoxCollider2D 찾기
                BoxCollider2D[] colliders = obj.GetComponentsInChildren<BoxCollider2D>();
                foreach (var collider in colliders)
                {
                    if (setUsedByComposite && !collider.usedByComposite)
                    {
                        Undo.RecordObject(collider, "Set Used By Composite");
                        collider.usedByComposite = true;
                        colliderCount++;
                    }
                }

                // TilemapCollider2D도 처리
                UnityEngine.Tilemaps.TilemapCollider2D[] tilemapColliders = obj.GetComponentsInChildren<UnityEngine.Tilemaps.TilemapCollider2D>();
                foreach (var tilemapCollider in tilemapColliders)
                {
                    if (setUsedByComposite && !tilemapCollider.usedByComposite)
                    {
                        Undo.RecordObject(tilemapCollider, "Set Used By Composite");
                        tilemapCollider.usedByComposite = true;
                        colliderCount++;
                    }
                }
            }

            // 자식 오브젝트 재귀 검색
            foreach (Transform child in obj.transform)
            {
                SearchAndSetupModules(child.gameObject, colliderMapTransform, ref moduleCount, ref colliderCount);
            }
        }
    }
}

