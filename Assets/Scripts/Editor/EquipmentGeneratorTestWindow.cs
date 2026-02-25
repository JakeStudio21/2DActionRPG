using UnityEditor;
using UnityEngine;

/// <summary>
/// 🧪 DynamicEquipmentGenerator 테스트 윈도우
/// Tools → Equipment Generator Test로 실행
/// </summary>
public class EquipmentGeneratorTestWindow : EditorWindow
{
    #region Fields
    
    private EquipmentData testEquipment;
    private ItemGrade testGrade = ItemGrade.S;
    private Vector2 scrollPosition;
    private EquipmentInstance lastGeneratedInstance;
    private string logText = "테스트를 시작하려면 [장비 생성] 버튼을 누르세요.";
    
    #endregion
    
    #region Window Setup
    
    [MenuItem("Tools/Equipment Generator Test")]
    public static void ShowWindow()
    {
        var window = GetWindow<EquipmentGeneratorTestWindow>("장비 생성기 테스트");
        window.minSize = new Vector2(600, 700);
    }
    
    #endregion
    
    #region GUI
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        DrawHeader();
        DrawTestSettings();
        DrawActionButtons();
        DrawResults();
        DrawLog();
        
        EditorGUILayout.EndScrollView();
    }
    
    /// <summary>
    /// 헤더
    /// </summary>
    private void DrawHeader()
    {
        GUILayout.Space(10);
        
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter
        };
        
        EditorGUILayout.LabelField("🧪 장비 생성기 테스트 도구", headerStyle);
        
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "DynamicEquipmentGenerator.Generate() 메서드를 테스트합니다.\n" +
            "1. 테스트할 EquipmentData를 드래그하세요.\n" +
            "2. 등급은 장비 데이터에서 자동으로 읽어옵니다.\n" +
            "3. [장비 생성] 버튼을 누르면 매번 다른 랜덤 스탯이 생성됩니다.",
            MessageType.Info
        );
        
        GUILayout.Space(10);
    }
    
    /// <summary>
    /// 테스트 설정
    /// </summary>
    private void DrawTestSettings()
    {
        EditorGUILayout.LabelField("⚙️ 테스트 설정", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // 장비 데이터 선택
        EditorGUI.BeginChangeCheck();
        testEquipment = (EquipmentData)EditorGUILayout.ObjectField(
            "테스트 장비",
            testEquipment,
            typeof(EquipmentData),
            false
        );
        
        // 장비가 변경되면 자동으로 해당 장비의 등급을 설정
        if (EditorGUI.EndChangeCheck() && testEquipment != null)
        {
            testGrade = testEquipment.itemGrade;
        }
        
        // 등급 선택 (읽기 전용 - 정보 표시용)
        GUI.enabled = false;
        testGrade = (ItemGrade)EditorGUILayout.EnumPopup("생성 등급 (자동)", testGrade);
        GUI.enabled = true;
        
        // 현재 설정 표시
        if (testEquipment != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField($"슬롯: {testEquipment.equipmentSlot}");
            EditorGUILayout.LabelField($"타입: {testEquipment.equipmentType}");
            if (testEquipment.equipmentType == EquipmentType.Weapon)
            {
                EditorGUILayout.LabelField($"무기 타입: {testEquipment.WeaponType}");
            }
            EditorGUILayout.LabelField($"베이스 예산: {testEquipment.baseBudget}");
            EditorGUILayout.LabelField($"주옵션 가중치: {testEquipment.mainStatWeight}");
            EditorGUILayout.LabelField($"부옵션 가중치: {testEquipment.subStatWeight}");
            EditorGUI.indentLevel--;
            
            // 경고 메시지
            EditorGUILayout.HelpBox(
                $"✅ 이 장비는 [{testEquipment.itemGrade}] 등급으로 생성됩니다.\n" +
                "등급은 장비 데이터에서 자동으로 읽어옵니다.",
                MessageType.Info
            );
        }
        
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
    }
    
    /// <summary>
    /// 액션 버튼
    /// </summary>
    private void DrawActionButtons()
    {
        EditorGUILayout.BeginHorizontal();
        
        // 장비 생성 버튼 (메인)
        GUI.enabled = testEquipment != null;
        
        if (GUILayout.Button("🎲 장비 생성", GUILayout.Height(40)))
        {
            GenerateEquipment();
        }
        
        GUI.enabled = true;
        
        // 로그 초기화 버튼
        if (GUILayout.Button("🔄 로그 초기화", GUILayout.Height(40), GUILayout.Width(120)))
        {
            logText = "로그가 초기화되었습니다.";
            lastGeneratedInstance = null;
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // StatPoolDataLoader 초기화 버튼
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("📊 StatPoolDataLoader 강제 초기화", GUILayout.Height(30)))
        {
            StatPoolDataLoader.Initialize();
            logText += "\n\n✅ StatPoolDataLoader 강제 초기화 완료!";
        }
        
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(10);
    }
    
    /// <summary>
    /// 결과 표시
    /// </summary>
    private void DrawResults()
    {
        if (lastGeneratedInstance == null) return;
        
        EditorGUILayout.LabelField("✅ 생성 결과", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // 기본 정보
        EditorGUILayout.LabelField($"장비명: {lastGeneratedInstance.EquipmentData?.equipmentName}");
        EditorGUILayout.LabelField($"등급: {testGrade}");
        EditorGUILayout.LabelField($"강화: +{lastGeneratedInstance.enhanceLevel}");
        EditorGUILayout.LabelField($"귀속: {(lastGeneratedInstance.isBound ? "예" : "아니오")}");
        
        GUILayout.Space(5);
        
        // 주옵션
        EditorGUILayout.LabelField("⚔️ 주옵션:", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField($"수치: {lastGeneratedInstance.finalMainStatValue}");
        EditorGUI.indentLevel--;
        
        GUILayout.Space(5);
        
        // 부옵션
        EditorGUILayout.LabelField($"🎲 부옵션: {lastGeneratedInstance.randomSubStats.Count}개", EditorStyles.boldLabel);
        
        if (lastGeneratedInstance.randomSubStats.Count > 0)
        {
            EditorGUI.indentLevel++;
            
            int index = 1;
            foreach (var pair in lastGeneratedInstance.randomSubStats)
            {
                EditorGUILayout.LabelField($"[{index}] {pair.Key}: {pair.Value}");
                index++;
            }
            
            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("(부옵션 없음 - C/D 등급)");
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
    }
    
    /// <summary>
    /// 로그 표시
    /// </summary>
    private void DrawLog()
    {
        EditorGUILayout.LabelField("📋 실행 로그", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.TextArea(logText, GUILayout.Height(200));
        
        EditorGUILayout.EndVertical();
    }
    
    #endregion
    
    #region Logic
    
    /// <summary>
    /// 장비 생성 실행
    /// </summary>
    private void GenerateEquipment()
    {
        if (testEquipment == null)
        {
            logText = "❌ 테스트 장비를 선택해주세요!";
            return;
        }
        
        logText = $"🎲 {testEquipment.equipmentName} ({testGrade}) 생성 시작...\n";
        logText += "========================================\n";
        
        // 생성 실행
        try
        {
            lastGeneratedInstance = DynamicEquipmentGenerator.Generate(testEquipment, testGrade);
            
            if (lastGeneratedInstance != null)
            {
                logText += "✅ 생성 성공!\n";
                logText += $"   - 주옵션: {lastGeneratedInstance.finalMainStatValue}\n";
                logText += $"   - 부옵션: {lastGeneratedInstance.randomSubStats.Count}개\n";
                
                if (lastGeneratedInstance.randomSubStats.Count > 0)
                {
                    foreach (var pair in lastGeneratedInstance.randomSubStats)
                    {
                        logText += $"     * {pair.Key}: {pair.Value}\n";
                    }
                }
                
                logText += "========================================\n";
                logText += "💡 Unity Console 창에서 상세 로그를 확인하세요.";
            }
            else
            {
                logText += "❌ 생성 실패! (null 반환)\n";
                logText += "Unity Console 창에서 에러 로그를 확인하세요.";
            }
        }
        catch (System.Exception ex)
        {
            logText += $"❌ 예외 발생!\n{ex.Message}\n{ex.StackTrace}";
        }
        
        // UI 갱신
        Repaint();
    }
    
    #endregion
}

