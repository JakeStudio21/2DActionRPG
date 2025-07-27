using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/// <summary>
/// 🎛️ 장비 데이터 관리 Editor Window (단순화된 버전)
/// </summary>
public class EquipmentDataManager : EditorWindow
{
    #region Window Management
    [MenuItem("Tools/Equipment Manager/Open Data Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<EquipmentDataManager>();
        window.titleContent = new GUIContent("🛡️ Equipment Manager");
        window.minSize = new Vector2(800, 500);
        window.Show();
    }
    #endregion

    #region Fields
    [Header("📁 File Paths")]
    private string jsonFolderPath = "Assets/Resources/Data/";
    private Dictionary<string, string> sheetFilePaths = new Dictionary<string, string>();
    
    [Header("📊 Data")]
    private EquipmentJsonData jsonData;
    private List<EquipmentData> allEquipmentObjects;
    private Vector2 leftScrollPos;
    private Vector2 rightScrollPos;
    
    [Header("🎛️ UI State")]
    private int selectedItemIndex = -1;
    private string searchFilter = "";
    private bool isRefreshing = false;
    private bool autoDetected = false;
    
    [Header("🎨 UI Styling")]
    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;
    private bool stylesInitialized = false;
    #endregion

    #region Unity Lifecycle
    private void OnEnable()
    {
        // 🔧 기본 초기화만 (자동 로드 제거)
        if (jsonData == null)
        {
            jsonData = new EquipmentJsonData();
        }
        
        if (allEquipmentObjects == null)
        {
            allEquipmentObjects = new List<EquipmentData>();
        }
        
        Debug.Log("✅ [EquipmentDataManager] 기본 초기화 완료 (자동 로드 비활성화)");
    }

    private void OnGUI()
    {
        try
        {
            if (!stylesInitialized)
            {
                InitializeStyles();
                stylesInitialized = true;
            }

            DrawToolbar();
            
            EditorGUILayout.BeginHorizontal();
            {
                DrawLeftPanel();
                DrawRightPanel();
            }
            EditorGUILayout.EndHorizontal();
            
            DrawStatusBar();
        }
        catch (System.Exception ex)
        {
            EditorGUILayout.HelpBox($"UI 오류: {ex.Message}", MessageType.Error);
            
            if (GUILayout.Button("🔄 Reset"))
            {
                Reset();
            }
        }
    }
    #endregion

    #region UI Styling
    private void InitializeStyles()
    {
        try
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            buttonStyle = new GUIStyle(GUI.skin.button);
            Debug.Log("✅ [EquipmentDataManager] UI 스타일 초기화 완료");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [EquipmentDataManager] 스타일 초기화 실패: {ex.Message}");
            headerStyle = EditorStyles.boldLabel;
            buttonStyle = GUI.skin.button;
        }
    }
    #endregion

    #region UI Drawing
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            if (GUILayout.Button("🔍 Auto-Detect", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                AutoDetectJsonFiles();
            }
            
            if (GUILayout.Button("📥 Load JSON", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                LoadJsonData();
            }
            
            if (GUILayout.Button("🔄 Refresh Unity", EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                RefreshEquipmentObjects();
            }
            
            if (GUILayout.Button("🏭 Generate All", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                GenerateAllFromJson();
            }
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("🔄 Reset", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                Reset();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLeftPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(250));
        {
            EditorGUILayout.LabelField("🔍 Equipment List", EditorStyles.boldLabel);
            
            searchFilter = EditorGUILayout.TextField("Search", searchFilter);
            EditorGUILayout.Space(5);
            
            leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
            {
                DrawEquipmentList();
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawEquipmentList()
    {
        if (allEquipmentObjects == null || allEquipmentObjects.Count == 0)
        {
            EditorGUILayout.LabelField("📭 No equipment data found", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("🔍 Load from Unity Project"))
            {
                RefreshEquipmentObjects();
            }
            
            if (GUILayout.Button("📥 Load from JSON"))
            {
                LoadJsonData();
            }
            return;
        }

        var filteredItems = GetFilteredEquipmentList();
        
        EditorGUILayout.LabelField($"📋 Found {filteredItems.Count} items:", EditorStyles.miniLabel);
        EditorGUILayout.Space(3);
        
        for (int i = 0; i < filteredItems.Count; i++)
        {
            var equipment = filteredItems[i];
            if (equipment == null) continue;
            
            bool isSelected = selectedItemIndex == i;
            
            // 🎨 선택된 아이템 스타일 강화
            GUIStyle itemStyle = new GUIStyle(GUI.skin.button);
            Color originalBgColor = GUI.backgroundColor;
            Color originalContentColor = GUI.contentColor;
            
            if (isSelected)
            {
                // 🔥 선택된 아이템: 진한 파란색 배경 + 흰색 텍스트
                GUI.backgroundColor = new Color(0.2f, 0.4f, 0.8f, 1f); // 진한 파란색
                GUI.contentColor = Color.white;
                itemStyle.fontStyle = FontStyle.Bold;
                itemStyle.normal.textColor = Color.white;
                itemStyle.hover.textColor = Color.white;
                itemStyle.active.textColor = Color.white;
            }
            else
            {
                // 🔘 일반 아이템: 연한 회색 배경
                GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
                GUI.contentColor = Color.white;
                itemStyle.fontStyle = FontStyle.Normal;
            }
            
            EditorGUILayout.BeginHorizontal();
            {
                // 🎯 선택 표시 아이콘 + 아이템 정보
                string selectionIcon = isSelected ? "🔸" : "  ";
                string displayName = $"{selectionIcon} {GetEquipmentIcon(equipment)} {equipment.equipmentName ?? "Unnamed"}";
                
                if (GUILayout.Button(displayName, itemStyle, GUILayout.Height(25)))
                {
                    selectedItemIndex = i;
                    Repaint(); // UI 즉시 새로고침
                }
                
                // 🏷️ 등급 표시
                GUIStyle gradeStyle = new GUIStyle(EditorStyles.miniLabel);
                if (isSelected)
                {
                    gradeStyle.normal.textColor = Color.white;
                    gradeStyle.fontStyle = FontStyle.Bold;
                }
                
                GUILayout.Label(equipment.itemGrade.ToString(), gradeStyle, GUILayout.Width(20));
            }
            EditorGUILayout.EndHorizontal();
            
            // 색상 복원
            GUI.backgroundColor = originalBgColor;
            GUI.contentColor = originalContentColor;
            
            EditorGUILayout.Space(1); // 아이템 간격
        }
    }

    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical();
        {
            var filteredItems = GetFilteredEquipmentList();
            
            if (selectedItemIndex >= 0 && selectedItemIndex < filteredItems.Count)
            {
                var equipment = filteredItems[selectedItemIndex];
                if (equipment != null)
                {
                    DrawEquipmentEditor(equipment);
                }
            }
            else
            {
                DrawStatusPanel();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawEquipmentEditor(EquipmentData equipment)
    {
        EditorGUILayout.LabelField($"🛡️ {equipment.equipmentName}", EditorStyles.largeLabel);
        EditorGUILayout.Space(5);
        
        rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);
        {
            var serializedObject = new SerializedObject(equipment);
            serializedObject.Update();
            
            // 📋 기본 정보 (확장)
            EditorGUILayout.LabelField("📋 기본 정보", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                DrawProperty(serializedObject, "equipmentName", "Equipment Name");
                DrawProperty(serializedObject, "itemID", "Item ID");
                DrawProperty(serializedObject, "equipmentType", "Equipment Type");
                DrawProperty(serializedObject, "itemGrade", "Item Grade");
                DrawProperty(serializedObject, "usableClass", "Usable Class");
                DrawProperty(serializedObject, "isTradable", "Is Tradable");
                DrawProperty(serializedObject, "requiredLevel", "Required Level");
                DrawProperty(serializedObject, "resourceID", "Resource ID");
                DrawProperty(serializedObject, "description", "Description");
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // 🎨 Icon
            EditorGUILayout.LabelField("🎨 Icon", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                DrawProperty(serializedObject, "icon", "Icon");
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // 🎮 게임 오브젝트
            EditorGUILayout.LabelField("🎮 게임 오브젝트", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                DrawProperty(serializedObject, "equipmentPrefab", "Equipment Prefab");
            }
            EditorGUILayout.EndVertical();
            
            // 무기 관련 정보 (무기인 경우만)
            if (equipment.IsWeapon)
            {
                EditorGUILayout.Space(5);
                
                // ⚔️ 무기 정보 설정 (🔧 실제 필드명으로 수정)
                EditorGUILayout.LabelField("⚔️ 무기 정보 설정", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    DrawProperty(serializedObject, "weaponType", "Weapon Type");
                    DrawProperty(serializedObject, "weaponCooldown", "Weapon Cooldown");
                    DrawProperty(serializedObject, "weaponRange", "Weapon Range");
                }
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.Space(5);
                
                // 📊 무기 정보 스텟
                EditorGUILayout.LabelField("📊 무기 정보 스텟", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    DrawProperty(serializedObject, "attackDamage", "Attack Damage");
                    DrawProperty(serializedObject, "attackSpeed", "Attack Speed");
                    DrawProperty(serializedObject, "attackRange", "Attack Range");
                    DrawProperty(serializedObject, "attackShape", "Attack Shape");
                    DrawProperty(serializedObject, "criticalChance", "Critical Chance");
                    DrawProperty(serializedObject, "criticalDamage", "Critical Damage");
                }
                EditorGUILayout.EndVertical();
                
                // 🏹 원거리 무기 정보 (원거리 무기인 경우만)
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("🏹 원거리 무기 전용", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    DrawProperty(serializedObject, "projectileId", "Projectile Id");
                }
                EditorGUILayout.EndVertical();
            }
            
            // 🔧 액션 버튼들
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("🔄 Sync from JSON"))
                {
                    SyncEquipmentFromJson(equipment);
                }
                
                if (GUILayout.Button("📤 Update JSON"))
                {
                    UpdateJsonFromEquipment(equipment);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            serializedObject.ApplyModifiedProperties();
        }
        EditorGUILayout.EndScrollView();
    }

    private void DrawStatusPanel()
    {
        EditorGUILayout.LabelField("📋 System Status", EditorStyles.boldLabel);
        
        int totalItems = allEquipmentObjects?.Count ?? 0;
        EditorGUILayout.LabelField($"Unity Items: {totalItems}");
        
        if (jsonData != null)
        {
            int jsonItems = (jsonData.WeaponBaseTable?.Count ?? 0) +
                           (jsonData.MeleeWeaponTable?.Count ?? 0) +
                           (jsonData.ProjectileWeaponTable?.Count ?? 0) +
                           (jsonData.ProjectileDataTable?.Count ?? 0);
            EditorGUILayout.LabelField($"JSON Items: {jsonItems}");
        }
        
        EditorGUILayout.Space(10);
        
        EditorGUILayout.LabelField("📁 Detected Files:", EditorStyles.boldLabel);
        if (sheetFilePaths.Count == 0)
        {
            EditorGUILayout.LabelField("No files detected. Click 'Auto-Detect'");
        }
        else
        {
            foreach (var file in sheetFilePaths)
            {
                bool exists = File.Exists(file.Value);
                string status = exists ? "✅" : "❌";
                EditorGUILayout.LabelField($"{status} {file.Key}: {Path.GetFileName(file.Value)}");
            }
        }
        
        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("🔍 Auto-Detect Files"))
        {
            AutoDetectJsonFiles();
        }
        
        if (GUILayout.Button("📥 Load All JSON"))
        {
            LoadJsonData();
        }
    }

    private void DrawProperty(SerializedObject obj, string propertyName)
    {
        var property = obj.FindProperty(propertyName);
        if (property != null)
        {
            EditorGUILayout.PropertyField(property);
        }
        else
        {
            EditorGUILayout.LabelField($"⚠️ Property '{propertyName}' not found", EditorStyles.miniLabel);
        }
    }

    // 🆕 라벨이 있는 버전
    private void DrawProperty(SerializedObject obj, string propertyName, string displayLabel)
    {
        var property = obj.FindProperty(propertyName);
        if (property != null)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(displayLabel));
        }
        else
        {
            EditorGUILayout.LabelField($"⚠️ Property '{propertyName}' not found", EditorStyles.miniLabel);
        }
    }

    private void DrawStatusBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        {
            int itemCount = allEquipmentObjects?.Count ?? 0;
            int jsonCount = 0;
            
            if (jsonData != null)
            {
                jsonCount = (jsonData.WeaponBaseTable?.Count ?? 0) +
                           (jsonData.MeleeWeaponTable?.Count ?? 0) +
                           (jsonData.ProjectileWeaponTable?.Count ?? 0) +
                           (jsonData.ProjectileDataTable?.Count ?? 0);
            }
            
            EditorGUILayout.LabelField($"Unity: {itemCount} | JSON: {jsonCount} | Files: {sheetFilePaths.Count}", EditorStyles.miniLabel);
            
            GUILayout.FlexibleSpace();
            
            if (isRefreshing)
            {
                EditorGUILayout.LabelField("🔄 Loading...", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("✅ Ready", EditorStyles.miniLabel);
            }
        }
        EditorGUILayout.EndHorizontal();
    }
    #endregion

    #region Core Methods
    public void Reset()
    {
        if (isRefreshing) return;
        
        try
        {
            Debug.Log("🔄 [Reset] 시스템 초기화 시작");
            
            selectedItemIndex = -1;
            searchFilter = "";
            autoDetected = false;
            
            if (jsonData != null)
            {
                jsonData.WeaponBaseTable?.Clear();
                jsonData.MeleeWeaponTable?.Clear();
                jsonData.ProjectileWeaponTable?.Clear();
                jsonData.ProjectileDataTable?.Clear();
            }
            
            allEquipmentObjects?.Clear();
            sheetFilePaths.Clear();
            
            jsonData = new EquipmentJsonData();
            allEquipmentObjects = new List<EquipmentData>();
            
            Repaint();
            
            Debug.Log("✅ [Reset] 시스템 초기화 완료");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [Reset] 초기화 실패: {ex.Message}");
        }
    }

    public void AutoDetectJsonFiles()
    {
        try
        {
            Debug.Log("🔍 [Auto-Detect] JSON 파일 자동 감지 시작");
            
            sheetFilePaths.Clear();
            
            if (!Directory.Exists(jsonFolderPath))
            {
                Debug.LogWarning($"⚠️ [Auto-Detect] 폴더가 존재하지 않습니다: {jsonFolderPath}");
                return;
            }
            
            string[] jsonFiles = Directory.GetFiles(jsonFolderPath, "*.json", SearchOption.TopDirectoryOnly);
            Debug.Log($"🔍 [Auto-Detect] 찾은 JSON 파일 수: {jsonFiles.Length}개");
            
            foreach (string filePath in jsonFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                
                // 🔧 Unity 호환 경로 생성: 슬래시 사용, Assets로 시작
                string unityPath = filePath.Replace('\\', '/');
                if (unityPath.StartsWith(Application.dataPath))
                {
                    unityPath = "Assets" + unityPath.Substring(Application.dataPath.Length);
                }
                
                Debug.Log($"🔍 [Auto-Detect] 처리 중: {fileName}");
                Debug.Log($"🔍 [Auto-Detect] 원본 경로: {filePath}");
                Debug.Log($"🔍 [Auto-Detect] Unity 경로: {unityPath}");
                
                // AssetDatabase로 파일 존재 확인
                TextAsset testAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(unityPath);
                if (testAsset == null)
                {
                    Debug.LogWarning($"⚠️ [Auto-Detect] AssetDatabase에서 로드 실패: {unityPath}");
                    continue;
                }
                Debug.Log($"✅ [Auto-Detect] AssetDatabase 로드 성공: {unityPath}");
                
                string sheetName = InferSheetNameFromFileName(fileName);
                if (!string.IsNullOrEmpty(sheetName))
                {
                    sheetFilePaths[sheetName] = unityPath;
                    Debug.Log($"✅ [Auto-Detect] 매핑: {sheetName} ← {fileName} → {unityPath}");
                }
            }
            
            autoDetected = true;
            Debug.Log($"✅ [Auto-Detect] 완료: {sheetFilePaths.Count}개 파일 감지");
            Repaint();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [Auto-Detect] 실패: {ex.Message}");
        }
    }

    public void LoadJsonData()
    {
        if (isRefreshing) return;
        
        isRefreshing = true;
        
        try
        {
            Debug.Log("📥 [LoadJSON] JSON 데이터 로드 시작");
            
            if (!autoDetected)
            {
                Debug.Log("🔍 [LoadJSON] Auto-Detect가 실행되지 않았으므로 먼저 실행합니다.");
                AutoDetectJsonFiles();
            }
            
            Debug.Log($"🔍 [LoadJSON] 감지된 파일 수: {sheetFilePaths.Count}개");
            
            if (sheetFilePaths.Count == 0)
            {
                Debug.LogWarning("⚠️ [LoadJSON] 감지된 JSON 파일이 없습니다");
                return;
            }
            
            // 감지된 파일들 출력
            foreach (var file in sheetFilePaths)
            {
                Debug.Log($"📁 [LoadJSON] 감지된 파일: {file.Key} → {file.Value}");
            }
            
            jsonData = new EquipmentJsonData();
            int loadedSheets = 0;
            
            foreach (var sheet in sheetFilePaths)
            {
                string sheetName = sheet.Key;
                string filePath = sheet.Value;
                
                Debug.Log($"🔄 [LoadJSON] 처리 중: {sheetName} ← {filePath}");
                
                // 🔧 Unity 방식으로 파일 읽기
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"❌ [LoadJSON] 파일이 존재하지 않습니다: {filePath}");
                    continue;
                }
                
                Debug.Log($"✅ [LoadJSON] 파일 존재 확인: {filePath}");
                
                try
                {
                    // 🔧 Unity AssetDatabase 방식으로 읽기
                    TextAsset jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(filePath);
                    
                    if (jsonAsset == null)
                    {
                        Debug.LogError($"❌ [LoadJSON] AssetDatabase로 로드 실패: {filePath}");
                        continue;
                    }
                    
                    string content = jsonAsset.text;
                    Debug.Log($"✅ [LoadJSON] 파일 읽기 성공: {content.Length}자");
                    
                    if (LoadSheetData(sheetName, content))
                    {
                        loadedSheets++;
                        Debug.Log($"✅ [LoadJSON] {sheetName} 로드 성공!");
                    }
                    else
                    {
                        Debug.LogError($"❌ [LoadJSON] {sheetName} 파싱 실패!");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ [LoadJSON] {sheetName} 처리 중 예외: {ex.Message}");
                }
            }
            
            Debug.Log($"✅ [LoadJSON] 완료: {loadedSheets}/{sheetFilePaths.Count}개 파일");
            
            // 로드 결과 출력
            int totalItems = (jsonData.WeaponBaseTable?.Count ?? 0) +
                            (jsonData.MeleeWeaponTable?.Count ?? 0) +
                            (jsonData.ProjectileWeaponTable?.Count ?? 0) +
                            (jsonData.ProjectileDataTable?.Count ?? 0);
            
            Debug.Log($"📊 [LoadJSON] 총 {totalItems}개 아이템 로드됨");
            Debug.Log($"📊 [LoadJSON] WeaponBase: {jsonData.WeaponBaseTable?.Count ?? 0}개");
            Debug.Log($"📊 [LoadJSON] MeleeWeapon: {jsonData.MeleeWeaponTable?.Count ?? 0}개");
            Debug.Log($"📊 [LoadJSON] ProjectileWeapon: {jsonData.ProjectileWeaponTable?.Count ?? 0}개");
            Debug.Log($"📊 [LoadJSON] ProjectileData: {jsonData.ProjectileDataTable?.Count ?? 0}개");
            
            Repaint();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [LoadJSON] JSON 로드 실패: {ex.Message}");
            Debug.LogError($"❌ [LoadJSON] StackTrace: {ex.StackTrace}");
        }
        finally
        {
            isRefreshing = false;
        }
    }

    public void RefreshEquipmentObjects()
    {
        if (isRefreshing) return;
        
        isRefreshing = true;
        
        try
        {
            Debug.Log("🔄 [RefreshUnity] Unity 장비 객체 새로고침 시작");
            
            if (allEquipmentObjects == null)
                allEquipmentObjects = new List<EquipmentData>();
            
            allEquipmentObjects.Clear();
            
            // Unity 프로젝트에서 모든 EquipmentData 찾기
            string[] guids = AssetDatabase.FindAssets("t:EquipmentData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                // 백업 폴더 제외
                if (path.Contains("Backup_Equipment/")) continue;
                
                var equipment = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
                if (equipment != null)
                {
                    allEquipmentObjects.Add(equipment);
                }
            }
            
            Debug.Log($"✅ [RefreshUnity] {allEquipmentObjects.Count}개 장비 객체 로드됨");
            
            selectedItemIndex = -1; // 선택 초기화
            Repaint();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [RefreshUnity] Unity 새로고침 실패: {ex.Message}");
        }
        finally
        {
            isRefreshing = false;
        }
    }

    public void GenerateAllFromJson()
    {
        if (jsonData?.WeaponBaseTable == null || jsonData.WeaponBaseTable.Count == 0)
        {
            Debug.LogWarning("⚠️ [Generate] JSON 데이터가 없습니다. 먼저 JSON을 로드해주세요.");
            return;
        }
        
        try
        {
            Debug.Log("🏭 [Generate] JSON에서 Unity 객체 생성 시작");
            
            int createdCount = 0;
            
            foreach (var weaponBase in jsonData.WeaponBaseTable)
            {
                if (!string.IsNullOrEmpty(weaponBase.ItemID))
                {
                    var equipment = GetOrCreateEquipmentData(weaponBase.ItemID);
                    if (equipment != null)
                    {
                        ApplyJsonToEquipment(weaponBase, equipment);
                        EditorUtility.SetDirty(equipment);
                        createdCount++;
                    }
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshEquipmentObjects();
            
            Debug.Log($"✅ [Generate] {createdCount}개 장비 생성/업데이트 완료");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [Generate] 생성 실패: {ex.Message}");
        }
    }
    #endregion

    #region Helper Methods
    private List<EquipmentData> GetFilteredEquipmentList()
    {
        if (allEquipmentObjects == null) return new List<EquipmentData>();
        
        return allEquipmentObjects.Where(equipment => {
            if (equipment == null) return false;
            
            bool matchesSearch = string.IsNullOrEmpty(searchFilter) || 
                               (equipment.equipmentName?.ToLower().Contains(searchFilter.ToLower()) ?? false) ||
                               (equipment.itemID?.ToLower().Contains(searchFilter.ToLower()) ?? false);
            
            return matchesSearch;
        }).ToList();
    }

    private string GetEquipmentIcon(EquipmentData equipment)
    {
        if (equipment == null) return "❓";
        
        // 선택된 아이템은 더 밝은 아이콘
        bool isCurrentSelected = false;
        var filteredItems = GetFilteredEquipmentList();
        if (selectedItemIndex >= 0 && selectedItemIndex < filteredItems.Count)
        {
            isCurrentSelected = filteredItems[selectedItemIndex] == equipment;
        }
        
        string baseIcon = "";
        switch (equipment.equipmentType)
        {
            case EquipmentType.Weapon:
                if (equipment.IsSword) baseIcon = isCurrentSelected ? "🗡️" : "⚔️";
                else if (equipment.IsBow) baseIcon = isCurrentSelected ? "🏹" : "🏹";
                else if (equipment.IsMagic) baseIcon = isCurrentSelected ? "🔮" : "✨";
                else baseIcon = isCurrentSelected ? "🗡️" : "⚔️";
                break;
            case EquipmentType.Armor:
                baseIcon = isCurrentSelected ? "🛡️" : "🛡️";
                break;
            case EquipmentType.Accessory:
                baseIcon = isCurrentSelected ? "💍" : "💍";
                break;
            default:
                baseIcon = isCurrentSelected ? "⭐" : "❓";
                break;
        }
        
        return baseIcon;
    }

    private string InferSheetNameFromFileName(string fileName)
    {
        Debug.Log($"🔍 [InferSheetName] 입력 파일명: '{fileName}'");
        
        string lowerFileName = fileName.ToLower();
        Debug.Log($"🔍 [InferSheetName] 소문자 변환: '{lowerFileName}'");
        
        if (lowerFileName.Contains("weaponbasetable") || lowerFileName.Contains("weaponbase"))
        {
            Debug.Log($"✅ [InferSheetName] WeaponBaseTable로 매핑됨");
            return "WeaponBaseTable";
        }
        
        if (lowerFileName.Contains("meleeweapontable") || lowerFileName.Contains("meleeweapon"))
        {
            Debug.Log($"✅ [InferSheetName] MeleeWeaponTable로 매핑됨");
            return "MeleeWeaponTable";
        }
        
        if (lowerFileName.Contains("projectileweapontable") || lowerFileName.Contains("projectileweapon"))
        {
            Debug.Log($"✅ [InferSheetName] ProjectileWeaponTable로 매핑됨");
            return "ProjectileWeaponTable";
        }
        
        if (lowerFileName.Contains("projectiledatatable") || lowerFileName.Contains("projectiledata"))
        {
            Debug.Log($"✅ [InferSheetName] ProjectileDataTable로 매핑됨");
            return "ProjectileDataTable";
        }
        
        Debug.LogWarning($"⚠️ [InferSheetName] 매핑 실패: '{fileName}' → null");
        return null;
    }

    private bool LoadSheetData(string sheetName, string content)
    {
        try
        {
            Debug.Log($"🔄 [LoadSheetData] 시작: {sheetName}");
            Debug.Log($"🔄 [LoadSheetData] switch문 진입 전");
            
            switch (sheetName)
            {
                case "WeaponBaseTable":
                    Debug.Log("🎯 [LoadSheetData] WeaponBaseTable case 선택됨");
                    return LoadWeaponBaseTable(content);
                    
                case "MeleeWeaponTable":
                    Debug.Log("🎯 [LoadSheetData] MeleeWeaponTable case 선택됨");
                    return LoadMeleeWeaponTable(content);
                    
                case "ProjectileWeaponTable":
                    Debug.Log("🎯 [LoadSheetData] ProjectileWeaponTable case 선택됨");
                    return LoadProjectileWeaponTable(content);
                    
                case "ProjectileDataTable":
                    Debug.Log("🎯 [LoadSheetData] ProjectileDataTable case 선택됨");
                    return LoadProjectileDataTable(content);
                    
                default:
                    Debug.LogWarning($"⚠️ [LoadSheetData] 알 수 없는 시트: '{sheetName}'");
                    Debug.Log($"🔍 [LoadSheetData] 가능한 값들: WeaponBaseTable, MeleeWeaponTable, ProjectileWeaponTable, ProjectileDataTable");
                    return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [LoadSheetData] {sheetName} 로드 실패: {ex.Message}");
            Debug.LogError($"❌ [LoadSheetData] StackTrace: {ex.StackTrace}");
            return false;
        }
    }

    private bool LoadWeaponBaseTable(string content)
    {
        try
        {
            Debug.Log($" [WeaponBase] JSON 파싱 시작");
            Debug.Log($"🔍 [WeaponBase] 파일 크기: {content.Length} 문자");
            Debug.Log($"🔍 [WeaponBase] 첫 200자: {content.Substring(0, Math.Min(200, content.Length))}");
            
            // 🔧 단계별 디버깅
            Debug.Log("🔧 [WeaponBase] JsonUtility.FromJson 시도 중...");
            
            var wrapper = JsonUtility.FromJson<WeaponBaseTableWrapper>(content);
            
            if (wrapper == null)
            {
                Debug.LogError("❌ [WeaponBase] JsonUtility.FromJson이 null을 반환했습니다!");
                
                // 🔧 테스트: 간단한 JSON으로 시도
                Debug.Log("🔧 [WeaponBase] 테스트용 간단한 JSON으로 시도...");
                string testJson = "{\"WeaponBaseTable\":[{\"ItemID\":\"TEST\",\"EquipmentName\":\"Test\"}]}";
                var testWrapper = JsonUtility.FromJson<WeaponBaseTableWrapper>(testJson);
                if (testWrapper != null)
                {
                    Debug.Log("✅ [WeaponBase] 테스트 JSON은 성공했습니다. 원본 JSON에 문제가 있을 수 있습니다.");
                }
                else
                {
                    Debug.LogError("❌ [WeaponBase] 테스트 JSON도 실패했습니다. 클래스 정의에 문제가 있을 수 있습니다.");
                }
                return false;
            }
            
            Debug.Log($"✅ [WeaponBase] JsonUtility.FromJson 성공!");
            
            if (wrapper.WeaponBaseTable == null)
            {
                Debug.LogError("❌ [WeaponBase] wrapper.WeaponBaseTable이 null입니다!");
                return false;
            }
            
            Debug.Log($"✅ [WeaponBase] 배열 파싱 성공! 총 {wrapper.WeaponBaseTable.Length}개 아이템");
            
            // 첫 번째 아이템 디버깅
            if (wrapper.WeaponBaseTable.Length > 0)
            {
                var firstItem = wrapper.WeaponBaseTable[0];
                Debug.Log($"🔍 [WeaponBase] 첫 번째 아이템: ItemID={firstItem.ItemID}, EquipmentName={firstItem.EquipmentName}");
            }
            
            // 🔧 완전한 타입 정보 제거 로직
            var validData = wrapper.WeaponBaseTable.Where(item => 
                !string.IsNullOrEmpty(item.ItemID) && 
                item.ItemID != "string" &&           // 타입 정보 제거
                item.EquipmentName != "string" &&    // 타입 정보 제거
                item.WeaponType != "string" &&       // 타입 정보 제거
                !item.ItemID.ToLower().Contains("string") &&  // 안전장치
                !item.ItemID.ToLower().Contains("float") &&   // 안전장치
                !item.ItemID.ToLower().Contains("enum") &&    // 안전장치
                item.ItemID.StartsWith("ITEM_")).ToList();
            
            Debug.Log($"🔍 [WeaponBase] 필터링 결과: {validData.Count}개 유효한 아이템");
            
            if (validData.Count == 0)
            {
                Debug.LogWarning("⚠️ [WeaponBase] 필터링 후 유효한 데이터가 없습니다!");
                Debug.Log("🔍 [WeaponBase] 필터 조건을 확인합니다...");
                
                foreach (var item in wrapper.WeaponBaseTable.Take(3))
                {
                    Debug.Log($"   - ItemID: '{item.ItemID}' (isEmpty: {string.IsNullOrEmpty(item.ItemID)}, isString: {item.ItemID == "string"}, startsWithITEM: {item.ItemID?.StartsWith("ITEM_")})");
                }
            }
            
            jsonData.WeaponBaseTable = validData;
            Debug.Log($"✅ [WeaponBase] {validData.Count}개 로드 성공!");
            return validData.Count > 0;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [WeaponBase] 파싱 실패: {ex.Message}");
            Debug.LogError($"❌ [WeaponBase] StackTrace: {ex.StackTrace}");
            return false;
        }
    }

    private bool LoadMeleeWeaponTable(string content)
    {
        try
        {
            Debug.Log($"🔍 [MeleeWeapon] JSON 내용 확인 (첫 100자): {content.Substring(0, Math.Min(100, content.Length))}...");
            
            // 🔧 객체 형태 파싱: {"MeleeWeaponTable": [...]}
            var wrapper = JsonUtility.FromJson<MeleeWeaponTableWrapper>(content);
            if (wrapper != null && wrapper.MeleeWeaponTable != null && wrapper.MeleeWeaponTable.Length > 0)
            {
                var validData = wrapper.MeleeWeaponTable.Where(item => 
                    !string.IsNullOrEmpty(item.ItemID) && 
                    item.ItemID != "string" && 
                    item.ItemID.StartsWith("ITEM_")).ToList();
                
                jsonData.MeleeWeaponTable = validData;
                Debug.Log($"✅ [MeleeWeapon] {validData.Count}개 로드 성공 (전체 {wrapper.MeleeWeaponTable.Length}개에서 필터링)");
                return validData.Count > 0;
            }
            else
            {
                Debug.LogError("❌ [MeleeWeapon] JSON 구조가 올바르지 않습니다. MeleeWeaponTable 배열을 찾을 수 없습니다.");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [MeleeWeapon] 로드 실패: {ex.Message}");
            Debug.LogError($"❌ [MeleeWeapon] JSON 내용: {content.Substring(0, Math.Min(200, content.Length))}...");
            return false;
        }
    }

    private bool LoadProjectileWeaponTable(string content)
    {
        try
        {
            Debug.Log($"🔍 [ProjectileWeapon] JSON 내용 확인 (첫 100자): {content.Substring(0, Math.Min(100, content.Length))}...");
            
            // 🔧 객체 형태 파싱: {"ProjectileWeaponTable": [...]}
            var wrapper = JsonUtility.FromJson<ProjectileWeaponTableWrapper>(content);
            if (wrapper != null && wrapper.ProjectileWeaponTable != null && wrapper.ProjectileWeaponTable.Length > 0)
            {
                var validData = wrapper.ProjectileWeaponTable.Where(item => 
                    !string.IsNullOrEmpty(item.ItemID) && 
                    item.ItemID != "string" && 
                    item.ItemID.StartsWith("ITEM_")).ToList();
                
                jsonData.ProjectileWeaponTable = validData;
                Debug.Log($"✅ [ProjectileWeapon] {validData.Count}개 로드 성공 (전체 {wrapper.ProjectileWeaponTable.Length}개에서 필터링)");
                return validData.Count > 0;
            }
            else
            {
                Debug.LogError("❌ [ProjectileWeapon] JSON 구조가 올바르지 않습니다. ProjectileWeaponTable 배열을 찾을 수 없습니다.");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [ProjectileWeapon] 로드 실패: {ex.Message}");
            Debug.LogError($"❌ [ProjectileWeapon] JSON 내용: {content.Substring(0, Math.Min(200, content.Length))}...");
            return false;
        }
    }

    private bool LoadProjectileDataTable(string content)
    {
        try
        {
            Debug.Log($"🔍 [ProjectileData] JSON 내용 확인 (첫 100자): {content.Substring(0, Math.Min(100, content.Length))}...");
            
            // 🔧 객체 형태 파싱: {"ProjectileDataTable": [...]}
            var wrapper = JsonUtility.FromJson<ProjectileDataTableWrapper>(content);
            if (wrapper != null && wrapper.ProjectileDataTable != null && wrapper.ProjectileDataTable.Length > 0)
            {
                var validData = wrapper.ProjectileDataTable.Where(item => 
                    !string.IsNullOrEmpty(item.ProjectileId) && 
                    item.ProjectileId != "string" && 
                    item.ProjectileId.StartsWith("ITEM_")).ToList();
                
                jsonData.ProjectileDataTable = validData;
                Debug.Log($"✅ [ProjectileData] {validData.Count}개 로드 성공 (전체 {wrapper.ProjectileDataTable.Length}개에서 필터링)");
                return validData.Count > 0;
            }
            else
            {
                Debug.LogError("❌ [ProjectileData] JSON 구조가 올바르지 않습니다. ProjectileDataTable 배열을 찾을 수 없습니다.");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [ProjectileData] 로드 실패: {ex.Message}");
            Debug.LogError($"❌ [ProjectileData] JSON 내용: {content.Substring(0, Math.Min(200, content.Length))}...");
            return false;
        }
    }

    private EquipmentData GetOrCreateEquipmentData(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return null;
        
        var existing = allEquipmentObjects?.FirstOrDefault(e => e != null && e.itemID == itemID);
        if (existing != null) return existing;
        
        var newEquipment = ScriptableObject.CreateInstance<EquipmentData>();
        newEquipment.itemID = itemID;
        newEquipment.equipmentName = itemID.Replace("ITEM_", "").Replace("_", " ");
        
        string fileName = $"{itemID}_Equipment.asset";
        string assetPath = $"Assets/Resources/Equipment/{fileName}";
        
        try
        {
            string resourcesDir = "Assets/Resources/Equipment";
            if (!Directory.Exists(resourcesDir))
            {
                Directory.CreateDirectory(resourcesDir);
            }
            
            AssetDatabase.CreateAsset(newEquipment, assetPath);
            AssetDatabase.SaveAssets();
            
            if (allEquipmentObjects == null)
                allEquipmentObjects = new List<EquipmentData>();
            
            allEquipmentObjects.Add(newEquipment);
            
            Debug.Log($"🆕 [Create] 새 장비 생성: {itemID}");
            return newEquipment;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [Create] 새 장비 생성 실패: {ex.Message}");
            return null;
        }
    }

    private void ApplyJsonToEquipment(WeaponBaseData weaponBase, EquipmentData equipment)
    {
        if (weaponBase == null || equipment == null) return;
        
        equipment.equipmentName = weaponBase.EquipmentName ?? "";
        equipment.itemID = weaponBase.ItemID ?? "";
        equipment.description = weaponBase.Description ?? "";
        // string을 PlayerClass enum으로 변환
        if (System.Enum.TryParse<PlayerClass>(weaponBase.UsableClass, out PlayerClass parsedClass))
        {
            equipment.usableClass = parsedClass;
        }
        else
        {
            equipment.usableClass = PlayerClass.None; // 기본값
        }
        equipment.isTradable = weaponBase.isTradable;
        equipment.requiredLevel = weaponBase.RequiredLevel;
        equipment.resourceID = weaponBase.ResourceID ?? "";
        
        if (!string.IsNullOrEmpty(weaponBase.ItemGrade) && 
            System.Enum.TryParse<ItemGrade>(weaponBase.ItemGrade, out ItemGrade grade))
        {
            equipment.itemGrade = grade;
        }
        
        if (!string.IsNullOrEmpty(weaponBase.EquipmentType) && 
            System.Enum.TryParse<EquipmentType>(weaponBase.EquipmentType, out EquipmentType equipType))
        {
            equipment.equipmentType = equipType;
        }
    }

    private void SyncEquipmentFromJson(EquipmentData equipment)
    {
        if (equipment == null || jsonData?.WeaponBaseTable == null) return;
        
        var weaponBase = jsonData.WeaponBaseTable.FirstOrDefault(w => w?.ItemID == equipment.itemID);
        if (weaponBase != null)
        {
            ApplyJsonToEquipment(weaponBase, equipment);
            EditorUtility.SetDirty(equipment);
            Debug.Log($"🔄 [Sync] {equipment.itemID} 동기화 완료");
        }
        else
        {
            Debug.LogWarning($"⚠️ [Sync] JSON에서 {equipment.itemID}를 찾을 수 없습니다");
        }
    }

    private void UpdateJsonFromEquipment(EquipmentData equipment)
    {
        if (equipment == null)
        {
            Debug.LogError("❌ [UpdateJSON] equipment가 null입니다.");
            return;
        }

        // 🆕 Update JSON 실행 전 최신 JSON 데이터 로드
        if (jsonData == null || jsonData.WeaponBaseTable == null || jsonData.WeaponBaseTable.Count == 0)
        {
            Debug.Log("�� [UpdateJSON] JSON 데이터가 없어서 자동 로드를 실행합니다.");
            
            // Auto-Detect가 실행되지 않았다면 먼저 실행
            if (sheetFilePaths.Count == 0)
            {
                AutoDetectJsonFiles();
            }
            
            // JSON 데이터 로드
            LoadJsonData();
            
            // 로드 실패 시 중단
            if (jsonData == null)
            {
                Debug.LogError("❌ [UpdateJSON] JSON 데이터 로드에 실패했습니다. Update JSON을 중단합니다.");
                return;
            }
        }

        try
        {
            Debug.Log($"📤 [UpdateJSON] {equipment.itemID} JSON 업데이트 시작");
            Debug.Log($"🔍 [UpdateJSON] 장비 정보 확인:");
            Debug.Log($"   - equipmentName: {equipment.equipmentName}");
            Debug.Log($"   - equipmentPrefab: {(equipment.equipmentPrefab != null ? equipment.equipmentPrefab.name : "null")}");
            Debug.Log($"   - icon: {(equipment.icon != null ? equipment.icon.name : "null")}");
            Debug.Log($"   - IsWeapon: {equipment.IsWeapon}");
            Debug.Log($"   - IsSword: {equipment.IsSword}");

            bool updated = false;

            // 1. WeaponBaseTable 업데이트
            updated |= UpdateWeaponBaseTableEntry(equipment);

            // 2. 무기 타입에 따른 상세 테이블 업데이트
            if (equipment.IsWeapon)
            {
                if (equipment.IsSword)
                {
                    updated |= UpdateMeleeWeaponTableEntry(equipment);
                }
                else if (equipment.IsBow || equipment.IsMagic)
                {
                    updated |= UpdateProjectileWeaponTableEntry(equipment);
                }
            }

            if (updated)
            {
                // 3. JSON 파일에 저장
                SaveUpdatedJsonData();
                Debug.Log($"✅ [UpdateJSON] {equipment.itemID} JSON 업데이트 완료");
            }
            else
            {
                Debug.LogWarning($"⚠️ [UpdateJSON] {equipment.itemID}에 대한 업데이트할 데이터를 찾을 수 없습니다.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [UpdateJSON] {equipment.itemID} 업데이트 실패: {ex.Message}");
            Debug.LogError($"❌ [UpdateJSON] StackTrace: {ex.StackTrace}");
        }
    }

    private bool UpdateWeaponBaseTableEntry(EquipmentData equipment)
    {
        if (jsonData.WeaponBaseTable == null)
        {
            jsonData.WeaponBaseTable = new List<WeaponBaseData>();
        }

        // 기존 항목 찾기
        var existingEntry = jsonData.WeaponBaseTable.FirstOrDefault(w => w.ItemID == equipment.itemID);
        
        if (existingEntry != null)
        {
            // 기존 항목 업데이트
            UpdateWeaponBaseDataFromEquipment(existingEntry, equipment);
            Debug.Log($"🔄 [UpdateJSON] WeaponBaseTable 기존 항목 업데이트: {equipment.itemID}");
            return true;
        }
        else
        {
            // 새 항목 추가
            var newEntry = CreateWeaponBaseDataFromEquipment(equipment);
            jsonData.WeaponBaseTable.Add(newEntry);
            Debug.Log($"🆕 [UpdateJSON] WeaponBaseTable 새 항목 추가: {equipment.itemID}");
            return true;
        }
    }

    private bool UpdateMeleeWeaponTableEntry(EquipmentData equipment)
    {
        if (jsonData.MeleeWeaponTable == null)
        {
            jsonData.MeleeWeaponTable = new List<MeleeWeaponData>();
        }

        var existingEntry = jsonData.MeleeWeaponTable.FirstOrDefault(m => m.ItemID == equipment.itemID);
        
        if (existingEntry != null)
        {
            UpdateMeleeWeaponDataFromEquipment(existingEntry, equipment);
            Debug.Log($"🔄 [UpdateJSON] MeleeWeaponTable 기존 항목 업데이트: {equipment.itemID}");
            return true;
        }
        else
        {
            var newEntry = CreateMeleeWeaponDataFromEquipment(equipment);
            jsonData.MeleeWeaponTable.Add(newEntry);
            Debug.Log($"🆕 [UpdateJSON] MeleeWeaponTable 새 항목 추가: {equipment.itemID}");
            return true;
        }
    }

    private bool UpdateProjectileWeaponTableEntry(EquipmentData equipment)
    {
        if (jsonData.ProjectileWeaponTable == null)
        {
            jsonData.ProjectileWeaponTable = new List<ProjectileWeaponData>();
        }

        var existingEntry = jsonData.ProjectileWeaponTable.FirstOrDefault(p => p.ItemID == equipment.itemID);
        
        if (existingEntry != null)
        {
            UpdateProjectileWeaponDataFromEquipment(existingEntry, equipment);
            Debug.Log($"🔄 [UpdateJSON] ProjectileWeaponTable 기존 항목 업데이트: {equipment.itemID}");
            return true;
        }
        else
        {
            var newEntry = CreateProjectileWeaponDataFromEquipment(equipment);
            jsonData.ProjectileWeaponTable.Add(newEntry);
            Debug.Log($"🆕 [UpdateJSON] ProjectileWeaponTable 새 항목 추가: {equipment.itemID}");
            return true;
        }
    }

    private void SaveUpdatedJsonData()
    {
        try
        {
            Debug.Log("💾 [SaveJSON] JSON 파일 저장 시작");

            int savedFiles = 0;

            // 각 테이블별로 개별 파일에 저장
            foreach (var sheetFile in sheetFilePaths)
            {
                string sheetName = sheetFile.Key;
                string filePath = sheetFile.Value;

                var sheetData = GetSheetDataForSave(sheetName);
                if (sheetData != null && sheetData.Count > 0)
                {
                    try
                    {
                        SaveSheetToFile(sheetName, sheetData, filePath);
                        savedFiles++;
                        Debug.Log($"✅ [SaveJSON] {sheetName} 저장 완료: {filePath}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"❌ [SaveJSON] {sheetName} 저장 실패: {ex.Message}");
                    }
                }
            }

            // AssetDatabase 갱신
            AssetDatabase.Refresh();
            Debug.Log($"✅ [SaveJSON] 전체 저장 완료: {savedFiles}개 파일");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [SaveJSON] 저장 실패: {ex.Message}");
        }
    }

    private void SaveSheetToFile(string sheetName, System.Collections.IList sheetData, string filePath)
    {
        try
        {
            Debug.Log($"💾 [SaveSheet] {sheetName} 저장 시작: {filePath}");
            
            // 새로운 JSON 생성 (List를 Array로 변환 필요!)
            string newJsonContent = "";
            switch (sheetName)
            {
                case "WeaponBaseTable":
                    newJsonContent = JsonUtility.ToJson(new WeaponBaseTableWrapper { 
                        WeaponBaseTable = sheetData.Cast<WeaponBaseData>().ToArray() 
                    }, true);
                    break;
                case "MeleeWeaponTable":
                    newJsonContent = JsonUtility.ToJson(new MeleeWeaponTableWrapper { 
                        MeleeWeaponTable = sheetData.Cast<MeleeWeaponData>().ToArray() 
                    }, true);
                    break;
                case "ProjectileWeaponTable":
                    newJsonContent = JsonUtility.ToJson(new ProjectileWeaponTableWrapper { 
                        ProjectileWeaponTable = sheetData.Cast<ProjectileWeaponData>().ToArray() 
                    }, true);
                    break;
                default:
                    Debug.LogError($"❌ [SaveSheet] 알 수 없는 시트: {sheetName}");
                    return;
            }

            // 엑셀 스타일 숫자 형식으로 변환
            newJsonContent = FormatToExcelStyle(newJsonContent);

            // 파일에 쓰기
            string absolutePath = System.IO.Path.Combine(Application.dataPath, filePath.Replace("Assets/", ""));
            System.IO.File.WriteAllText(absolutePath, newJsonContent, System.Text.Encoding.UTF8);
            
            AssetDatabase.Refresh();
            Debug.Log($"✅ [SaveSheet] {sheetName} 저장 완료");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ [SaveSheet] {sheetName} 저장 실패: {ex.Message}");
        }
    }

    private string FormatToExcelStyle(string jsonContent)
    {
        try
        {
            Debug.Log($"🔧 [FormatJSON] 엑셀 스타일로 변환 중...");
            
            string result = jsonContent;
            
            // 1. 정수 필드: .0 제거 (20.0 → 20)
            result = System.Text.RegularExpressions.Regex.Replace(
                result, 
                "\"(RequiredLevel|AttackShape|AttackDamage)\":\\s*(\\d+)\\.0+(?!\\d)", 
                "\"$1\": $2"
            );
            
            // 2. Float 필드: 1자리 소수점으로 정리
            result = System.Text.RegularExpressions.Regex.Replace(
                result,
                "\"(Cooldown|AttackSpeed|Attackrange|CriticalChance|CriticalDamage|criticalChance|criticalDamage)\":\\s*([\\d]+\\.[\\d]+)",
                match => {
                    string fieldName = match.Groups[1].Value;
                    string numberStr = match.Groups[2].Value;
                    
                    if (float.TryParse(numberStr, out float number))
                    {
                        // 소수점 1자리로 포맷 (끝자리 0 제거)
                        string formatted = number.ToString("0.#");
                        return $"\"{fieldName}\": {formatted}";
                    }
                    return match.Value;
                }
            );
            
            Debug.Log($"✅ [FormatJSON] 엑셀 스타일 변환 완료");
            return result;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"⚠️ [FormatJSON] 변환 실패: {ex.Message}");
            return jsonContent;
        }
    }

    private System.Collections.IList GetSheetDataForSave(string sheetName)
    {
        switch (sheetName)
        {
            case "WeaponBaseTable":
                return jsonData.WeaponBaseTable;
            case "MeleeWeaponTable":
                return jsonData.MeleeWeaponTable;
            case "ProjectileWeaponTable":
                return jsonData.ProjectileWeaponTable;
            case "ProjectileDataTable":
                return jsonData.ProjectileDataTable;
            default:
                return null;
        }
    }

    // Unity 객체 → JSON 데이터 변환 메서드들 (안전한 버전)
    private WeaponBaseData CreateWeaponBaseDataFromEquipment(EquipmentData equipment)
    {
        Debug.Log($"🔄 [CreateWeaponBase] {equipment.itemID} 변환 시작");
        
        // null 체크 및 안전한 변환
        string equipmentPrefabName = "";
        if (equipment.equipmentPrefab != null)
        {
            equipmentPrefabName = equipment.equipmentPrefab.name + ".prefab";
            Debug.Log($"🔄 [CreateWeaponBase] equipmentPrefab: {equipmentPrefabName}");
        }
        else
        {
            Debug.LogWarning($"⚠️ [CreateWeaponBase] {equipment.itemID}: equipmentPrefab이 null입니다.");
        }
        
        string iconName = "";
        if (equipment.icon != null)
        {
            iconName = equipment.icon.name + ".png";
            Debug.Log($"🔄 [CreateWeaponBase] icon: {iconName}");
        }
        else
        {
            Debug.LogWarning($"⚠️ [CreateWeaponBase] {equipment.itemID}: icon이 null입니다.");
        }

        return new WeaponBaseData
        {
            ItemID = equipment.itemID ?? "",
            EquipmentName = equipment.equipmentName ?? "",
            WeaponType = equipment.WeaponType.ToString(),
            AttackType = equipment.IsSword ? "Melee" : "Ranged",
            EquipmentType = equipment.equipmentType.ToString(),
            UsableClass = equipment.usableClass.ToString(),
            isTradable = equipment.isTradable,
            ItemGrade = equipment.itemGrade.ToString(),
            RequiredLevel = equipment.requiredLevel,
            ResourceID = equipment.resourceID ?? "",
            EquipmentPrefab = equipmentPrefabName,
            Icon = iconName,
            Description = equipment.description ?? ""
        };
    }

    private void UpdateWeaponBaseDataFromEquipment(WeaponBaseData target, EquipmentData source)
    {
        Debug.Log($"🔄 [UpdateWeaponBase] {source.itemID} 업데이트 시작");
        
        target.EquipmentName = source.equipmentName ?? "";
        target.WeaponType = source.WeaponType.ToString();
        target.AttackType = source.IsSword ? "Melee" : "Ranged";
        target.EquipmentType = source.equipmentType.ToString();
        target.UsableClass = source.usableClass.ToString();
        target.isTradable = source.isTradable;
        target.ItemGrade = source.itemGrade.ToString();
        target.RequiredLevel = source.requiredLevel;
        target.ResourceID = source.resourceID ?? "";
        target.Description = source.description ?? "";
        
        // 안전한 프리팹 이름 처리
        if (source.equipmentPrefab != null)
        {
            target.EquipmentPrefab = source.equipmentPrefab.name + ".prefab";
            Debug.Log($"🔄 [UpdateWeaponBase] equipmentPrefab 업데이트: {target.EquipmentPrefab}");
        }
        else
        {
            target.EquipmentPrefab = "";
            Debug.LogWarning($"⚠️ [UpdateWeaponBase] {source.itemID}: equipmentPrefab이 null이므로 빈 문자열로 설정");
        }
        
        // 안전한 아이콘 이름 처리
        if (source.icon != null)
        {
            target.Icon = source.icon.name + ".png";
            Debug.Log($"🔄 [UpdateWeaponBase] icon 업데이트: {target.Icon}");
        }
        else
        {
            target.Icon = "";
            Debug.LogWarning($"⚠️ [UpdateWeaponBase] {source.itemID}: icon이 null이므로 빈 문자열로 설정");
        }
    }

    private MeleeWeaponData CreateMeleeWeaponDataFromEquipment(EquipmentData equipment)
    {
        Debug.Log($"🔄 [CreateMeleeWeapon] {equipment.itemID} 변환 시작");
        
        return new MeleeWeaponData
        {
            ItemID = equipment.itemID ?? "",
            Cooldown = equipment.WeaponCooldown,
            AttackDamage = equipment.attackDamage,
            AttackSpeed = equipment.attackSpeed,
            Attackrange = equipment.attackRange,
            AttackShape = equipment.attackShape,
            CriticalChance = equipment.criticalChance,
            CriticalDamage = equipment.criticalDamage
        };
    }

    private void UpdateMeleeWeaponDataFromEquipment(MeleeWeaponData target, EquipmentData source)
    {
        Debug.Log($"🔄 [UpdateMeleeWeapon] {source.itemID} 업데이트 시작");
        
        target.Cooldown = source.WeaponCooldown;
        target.AttackDamage = source.attackDamage;
        target.AttackSpeed = source.attackSpeed;
        target.Attackrange = source.attackRange;
        target.AttackShape = source.attackShape;
        target.CriticalChance = source.criticalChance;
        target.CriticalDamage = source.criticalDamage;
    }

    private ProjectileWeaponData CreateProjectileWeaponDataFromEquipment(EquipmentData equipment)
    {
        Debug.Log($"🔄 [CreateProjectileWeapon] {equipment.itemID} 변환 시작");
        
        return new ProjectileWeaponData
        {
            ItemID = equipment.itemID ?? "",
            ProjectileId = equipment.projectileId ?? "",
            criticalChance = equipment.criticalChance,
            criticalDamage = equipment.criticalDamage,
            Cooldown = equipment.WeaponCooldown
        };
    }

    private void UpdateProjectileWeaponDataFromEquipment(ProjectileWeaponData target, EquipmentData source)
    {
        Debug.Log($"🔄 [UpdateProjectileWeapon] {source.itemID} 업데이트 시작");
        
        target.ProjectileId = source.projectileId ?? "";
        target.criticalChance = source.criticalChance;
        target.criticalDamage = source.criticalDamage;
        target.Cooldown = source.WeaponCooldown;
    }
    #endregion

    #region JSON Helper
    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            try
            {
                if (!json.Trim().StartsWith("["))
                {
                    json = "[" + json + "]";
                }
                
                string newJson = "{ \"array\": " + json + "}";
                Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
                
                return wrapper?.array ?? new T[0];
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ [JsonHelper] 파싱 실패: {ex.Message}");
                return new T[0];
            }
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] array;
        }
    }
    #endregion

    // 🆕 WeaponBaseTable JSON 래퍼 클래스 추가
    [System.Serializable]
    private class WeaponBaseTableWrapper
    {
        public WeaponBaseData[] WeaponBaseTable;
    }

    // 🆕 MeleeWeaponTable JSON 래퍼 클래스 추가
    [System.Serializable]
    private class MeleeWeaponTableWrapper
    {
        public MeleeWeaponData[] MeleeWeaponTable;
    }

    // 🆕 ProjectileWeaponTable JSON 래퍼 클래스 추가
    [System.Serializable]
    private class ProjectileWeaponTableWrapper
    {
        public ProjectileWeaponData[] ProjectileWeaponTable;
    }

    // 🆕 ProjectileDataTable JSON 래퍼 클래스 추가
    [System.Serializable]
    private class ProjectileDataTableWrapper
    {
        public ProjectileData[] ProjectileDataTable;
    }

    private bool IsValidDataEntry(WeaponBaseData item)
    {
        if (string.IsNullOrEmpty(item.ItemID)) return false;
        
        // 타입 키워드 체크
        string[] typeKeywords = { "string", "float", "int", "bool", "enum" };
        string itemIdLower = item.ItemID.ToLower();
        
        foreach (string keyword in typeKeywords)
        {
            if (itemIdLower.Contains(keyword)) return false;
        }
        
        // 실제 데이터 형식 체크
        return item.ItemID.StartsWith("ITEM_");
    }
}
