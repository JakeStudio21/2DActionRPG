using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

namespace LevelDesignEditor
{
    /// <summary>
    /// Scene에 배치된 여러 모듈의 Tilemap을 하나로 병합하는 Editor Tool
    /// Tools → Module System → Merge Tilemaps
    /// </summary>
    public class TilemapMerger : EditorWindow
    {
        private string searchPrefix = "Md_";
        private string mergedGridName = "MergedGrid";
        private bool deleteOriginalGrids = true;
        private bool createBackupScene = false;
        private bool addCompositeCollider = true;
        private MergeStrategy mergeStrategy = MergeStrategy.ByLayer;

        private List<GameObject> foundModules = new List<GameObject>();
        private Dictionary<string, List<Tilemap>> tilemapsByLayer = new Dictionary<string, List<Tilemap>>();
        private int totalTileCount = 0;
        private Vector2 scrollPosition;
        private bool hasScanned = false;

        private enum MergeStrategy
        {
            ByLayer,      // 레이어별 병합 (Floor, Wall, Decoration)
            AllIntoOne    // 모두 하나로
        }

        [MenuItem("Tools/Module System/Merge Tilemaps")]
        public static void ShowWindow()
        {
            var window = GetWindow<TilemapMerger>("Tilemap Merger");
            window.minSize = new Vector2(500, 650);
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Tilemap Merger", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "모듈 프리팹의 Tilemap들을 하나로 병합하여 성능을 최적화합니다.\n" +
                "⚠️ 병합 후에는 개별 모듈 편집이 어렵습니다. 레벨 디자인이 완료된 후 실행하세요.",
                MessageType.Info
            );
            GUILayout.Space(10);

            // 검색 설정
            GUILayout.Label("Search Settings", EditorStyles.boldLabel);
            searchPrefix = EditorGUILayout.TextField("Module Prefix", searchPrefix);
            
            GUILayout.Space(10);

            // 병합 전략
            GUILayout.Label("Merge Strategy", EditorStyles.boldLabel);
            mergeStrategy = (MergeStrategy)EditorGUILayout.EnumPopup("Strategy", mergeStrategy);
            
            if (mergeStrategy == MergeStrategy.ByLayer)
            {
                EditorGUILayout.HelpBox(
                    "레이어별 병합: 동일 Sorting Layer의 Tilemap끼리 병합됩니다.\n" +
                    "예: Floor → Floor, Wall → Wall, Decoration → Decoration",
                    MessageType.None
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "전체 병합: 모든 Tilemap을 하나로 병합합니다.\n" +
                    "⚠️ Sorting Order가 달라 시각적 문제가 발생할 수 있습니다.",
                    MessageType.Warning
                );
            }

            GUILayout.Space(10);

            // 출력 설정
            GUILayout.Label("Output Settings", EditorStyles.boldLabel);
            mergedGridName = EditorGUILayout.TextField("Merged Grid Name", mergedGridName);

            GUILayout.Space(10);

            // 옵션
            GUILayout.Label("Options", EditorStyles.boldLabel);
            deleteOriginalGrids = EditorGUILayout.Toggle("Delete Original Grids", deleteOriginalGrids);
            createBackupScene = EditorGUILayout.Toggle("Create Backup Scene", createBackupScene);
            addCompositeCollider = EditorGUILayout.Toggle("Add CompositeCollider2D", addCompositeCollider);

            GUILayout.Space(10);

            // 스캔 버튼
            if (GUILayout.Button("Scan Tilemaps", GUILayout.Height(30)))
            {
                ScanTilemaps();
            }

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
                        $"Total Tilemaps: {tilemapsByLayer.Values.Sum(list => list.Count)}\n" +
                        $"Estimated Tiles: {totalTileCount:N0}",
                        MessageType.Info
                    );

                    GUILayout.Space(5);

                    // 레이어별 Tilemap 표시
                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(250));

                    foreach (var kvp in tilemapsByLayer)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        
                        EditorGUILayout.LabelField($"Layer: {kvp.Key} ({kvp.Value.Count} Tilemaps)", EditorStyles.boldLabel);
                        
                        foreach (var tilemap in kvp.Value)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("  •", GUILayout.Width(20));
                            EditorGUILayout.ObjectField(tilemap.gameObject, typeof(GameObject), true);
                            EditorGUILayout.LabelField($"({GetTileCount(tilemap)} tiles)", GUILayout.Width(100));
                            EditorGUILayout.EndHorizontal();
                        }
                        
                        EditorGUILayout.EndVertical();
                        GUILayout.Space(5);
                    }

                    EditorGUILayout.EndScrollView();

                    GUILayout.Space(10);

                    // 병합 버튼
                    GUI.enabled = tilemapsByLayer.Count > 0;
                    
                    if (GUILayout.Button("Merge Tilemaps", GUILayout.Height(40)))
                    {
                        if (EditorUtility.DisplayDialog(
                            "Confirm Merge",
                            $"Merge {tilemapsByLayer.Values.Sum(list => list.Count)} Tilemaps?\n\n" +
                            $"Estimated tiles: {totalTileCount:N0}\n" +
                            (deleteOriginalGrids ? "⚠️ Original grids will be deleted!\n" : "") +
                            (createBackupScene ? "✓ Backup scene will be created." : ""),
                            "Merge",
                            "Cancel"))
                        {
                            MergeTilemaps();
                        }
                    }
                    
                    GUI.enabled = true;
                }
            }
        }

        private void ScanTilemaps()
        {
            foundModules.Clear();
            tilemapsByLayer.Clear();
            totalTileCount = 0;
            hasScanned = true;

            // Scene의 모든 루트 오브젝트 검색
            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                .GetRootGameObjects();

            // 모듈 프리팹 찾기
            foreach (var root in rootObjects)
            {
                SearchModulesRecursive(root);
            }

            // 레이어별 그룹핑
            foreach (var module in foundModules)
            {
                Tilemap[] tilemaps = module.GetComponentsInChildren<Tilemap>();
                
                foreach (var tilemap in tilemaps)
                {
                    string layerKey = GetLayerKey(tilemap);
                    
                    if (!tilemapsByLayer.ContainsKey(layerKey))
                    {
                        tilemapsByLayer[layerKey] = new List<Tilemap>();
                    }
                    
                    tilemapsByLayer[layerKey].Add(tilemap);
                    totalTileCount += GetTileCount(tilemap);
                }
            }
        }

        private void SearchModulesRecursive(GameObject obj)
        {
            if (obj.name.StartsWith(searchPrefix))
            {
                foundModules.Add(obj);
            }

            foreach (Transform child in obj.transform)
            {
                SearchModulesRecursive(child.gameObject);
            }
        }

        private string GetLayerKey(Tilemap tilemap)
        {
            TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
            
            if (mergeStrategy == MergeStrategy.AllIntoOne)
            {
                return "All";
            }
            
            if (renderer != null)
            {
                return $"{renderer.sortingLayerName}_{renderer.sortingOrder}";
            }
            
            return "Default_0";
        }

        private int GetTileCount(Tilemap tilemap)
        {
            int count = 0;
            
            foreach (var pos in tilemap.cellBounds.allPositionsWithin)
            {
                if (tilemap.HasTile(pos))
                {
                    count++;
                }
            }
            
            return count;
        }

        private void MergeTilemaps()
        {
            // 백업 생성
            if (createBackupScene)
            {
                CreateBackupScene();
            }

            // 병합된 Grid 생성
            GameObject mergedGridObj = new GameObject(mergedGridName);
            Grid mergedGrid = mergedGridObj.AddComponent<Grid>();
            mergedGrid.cellLayout = GridLayout.CellLayout.Rectangle;
            mergedGrid.cellSize = new Vector3(1, 1, 0);

            Undo.RegisterCreatedObjectUndo(mergedGridObj, "Merge Tilemaps");

            // 레이어별 병합
            foreach (var kvp in tilemapsByLayer)
            {
                string layerKey = kvp.Key;
                List<Tilemap> tilemaps = kvp.Value;

                if (tilemaps.Count == 0)
                    continue;

                // 병합된 Tilemap 생성
                GameObject tilemapObj = new GameObject($"Tilemap_{layerKey}");
                tilemapObj.transform.SetParent(mergedGridObj.transform);
                
                Tilemap mergedTilemap = tilemapObj.AddComponent<Tilemap>();
                TilemapRenderer mergedRenderer = tilemapObj.AddComponent<TilemapRenderer>();

                // 첫 번째 Tilemap의 설정 복사
                Tilemap firstTilemap = tilemaps[0];
                TilemapRenderer firstRenderer = firstTilemap.GetComponent<TilemapRenderer>();
                
                if (firstRenderer != null)
                {
                    mergedRenderer.sortingLayerID = firstRenderer.sortingLayerID;
                    mergedRenderer.sortingOrder = firstRenderer.sortingOrder;
                    mergedRenderer.mode = firstRenderer.mode;
                    mergedRenderer.detectChunkCullingBounds = firstRenderer.detectChunkCullingBounds;
                    mergedRenderer.chunkCullingBounds = firstRenderer.chunkCullingBounds;
                }

                // 모든 타일 복사
                int copiedTileCount = 0;
                
                foreach (var tilemap in tilemaps)
                {
                    foreach (var pos in tilemap.cellBounds.allPositionsWithin)
                    {
                        if (tilemap.HasTile(pos))
                        {
                            TileBase tile = tilemap.GetTile(pos);
                            
                            // World 좌표로 변환
                            Vector3 worldPos = tilemap.CellToWorld(pos);
                            Vector3Int targetPos = mergedTilemap.WorldToCell(worldPos);
                            
                            mergedTilemap.SetTile(targetPos, tile);
                            mergedTilemap.SetTransformMatrix(targetPos, tilemap.GetTransformMatrix(pos));
                            mergedTilemap.SetColor(targetPos, tilemap.GetColor(pos));
                            
                            copiedTileCount++;
                        }
                    }
                }
                // CompositeCollider2D 추가
                if (addCompositeCollider)
                {
                    TilemapCollider2D tilemapCollider = tilemapObj.AddComponent<TilemapCollider2D>();
                    tilemapCollider.usedByComposite = true;
                    
                    Rigidbody2D rb = mergedGridObj.GetComponent<Rigidbody2D>();
                    if (rb == null)
                    {
                        rb = mergedGridObj.AddComponent<Rigidbody2D>();
                        rb.bodyType = RigidbodyType2D.Static;
                    }
                    
                    CompositeCollider2D compositeCollider = mergedGridObj.GetComponent<CompositeCollider2D>();
                    if (compositeCollider == null)
                    {
                        compositeCollider = mergedGridObj.AddComponent<CompositeCollider2D>();
                        compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
                    }
                }

                Undo.RegisterCreatedObjectUndo(tilemapObj, "Merge Tilemaps");
            }

            // 원본 Grid 삭제
            if (deleteOriginalGrids)
            {
                List<GameObject> gridsToDelete = new List<GameObject>();
                
                foreach (var module in foundModules)
                {
                    Grid[] grids = module.GetComponentsInChildren<Grid>();
                    foreach (var grid in grids)
                    {
                        gridsToDelete.Add(grid.gameObject);
                    }
                }

                foreach (var grid in gridsToDelete)
                {
                    Undo.DestroyObjectImmediate(grid);
                }
            }

            // 결과 표시
            string message = $"Tilemap merge complete!\n\n" +
                             $"Merged Grid: {mergedGridName}\n" +
                             $"Layers: {tilemapsByLayer.Count}\n" +
                             $"Total Tiles: {totalTileCount:N0}";

            EditorUtility.DisplayDialog("Merge Complete", message, "OK");
            
            // 병합된 Grid 선택
            Selection.activeGameObject = mergedGridObj;
            EditorGUIUtility.PingObject(mergedGridObj);

            // 재스캔
            hasScanned = false;
        }

        private void CreateBackupScene()
        {
            string currentScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            
            if (string.IsNullOrEmpty(currentScenePath))
            {
                Debug.LogWarning("[TilemapMerger] Scene이 저장되지 않아 백업을 만들 수 없습니다.");
                return;
            }

            string backupPath = currentScenePath.Replace(".unity", "_Backup.unity");
            
            if (AssetDatabase.CopyAsset(currentScenePath, backupPath))
            {
            }
            else
            {
                Debug.LogError($"[TilemapMerger] 백업 Scene 생성 실패: {backupPath}");
            }
        }
    }
}

