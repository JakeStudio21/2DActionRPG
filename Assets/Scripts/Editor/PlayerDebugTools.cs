using UnityEngine;
using UnityEditor;

/// <summary>
/// 플레이어 데이터 디버그 도구
/// 경험치, 골드, 레벨 등을 빠르게 조작
/// </summary>
public class PlayerDebugTools : EditorWindow
{
    private int expToAdd = 100;
    private int goldToAdd = 1000;
    private int levelToSet = 1;
    private int spToAdd = 10;
    
    [MenuItem("Tools/Player/🎮 플레이어 디버그 도구")]
    public static void ShowWindow()
    {
        var window = GetWindow<PlayerDebugTools>("플레이어 디버그");
        window.minSize = new Vector2(400, 500);
    }
    
    void OnGUI()
    {
        GUILayout.Label("🎮 플레이어 디버그 도구", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("⚠️ 플레이 모드에서만 사용 가능합니다!", MessageType.Warning);
            return;
        }
        
        if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
        {
            EditorGUILayout.HelpBox("❌ 캐릭터가 선택되지 않았습니다!\n로비에서 캐릭터를 선택하세요.", MessageType.Error);
            return;
        }
        
        var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
        if (slotData == null)
        {
            EditorGUILayout.HelpBox("❌ 슬롯 데이터를 불러올 수 없습니다!", MessageType.Error);
            return;
        }
        
        // 현재 상태 표시
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("📊 현재 상태", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("캐릭터", $"{slotData.playerName} ({slotData.playerType})");
        EditorGUILayout.LabelField("레벨", $"Lv.{slotData.level}");
        EditorGUILayout.LabelField("경험치", $"{PlayerDataManager.Instance.selectedPlayerData.currentExp}/{PlayerDataManager.Instance.selectedPlayerData.expToNextLevel}");
        EditorGUILayout.LabelField("골드", $"{AccountDataManager.Instance.CurrentGold}G");
        EditorGUILayout.LabelField("SP", $"{slotData.usedSP}/{slotData.totalSP}");
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // ========================================
        // 경험치 추가
        // ========================================
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("💎 경험치 추가", EditorStyles.boldLabel);
        expToAdd = EditorGUILayout.IntField("추가할 경험치", expToAdd);
        
        if (GUILayout.Button($"경험치 +{expToAdd} 추가"))
        {
            PlayerDataManager.Instance.AddExp(expToAdd);
            Debug.Log($"✅ 경험치 {expToAdd} 추가됨!");
        }
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("경험치 +100")) PlayerDataManager.Instance.AddExp(100);
        if (GUILayout.Button("경험치 +500")) PlayerDataManager.Instance.AddExp(500);
        if (GUILayout.Button("경험치 +1000")) PlayerDataManager.Instance.AddExp(1000);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // ========================================
        // 골드 추가
        // ========================================
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("💰 골드 추가", EditorStyles.boldLabel);
        goldToAdd = EditorGUILayout.IntField("추가할 골드", goldToAdd);
        
        if (GUILayout.Button($"골드 +{goldToAdd} 추가"))
        {
            AccountDataManager.Instance.AddGold(goldToAdd);
            Debug.Log($"✅ 골드 {goldToAdd} 추가됨!");
        }
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("골드 +1000")) AccountDataManager.Instance.AddGold(1000);
        if (GUILayout.Button("골드 +10000")) AccountDataManager.Instance.AddGold(10000);
        if (GUILayout.Button("골드 +100000")) AccountDataManager.Instance.AddGold(100000);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // ========================================
        // SP 추가 (직접)
        // ========================================
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("📚 SP 추가 (테스트용)", EditorStyles.boldLabel);
        spToAdd = EditorGUILayout.IntField("추가할 SP", spToAdd);
        
        if (GUILayout.Button($"SP +{spToAdd} 추가"))
        {
            slotData.totalSP += spToAdd;
            if (PlayerDataManager.Instance.selectedPlayerData != null)
                PlayerDataManager.Instance.selectedPlayerData.totalSP = slotData.totalSP;
            
            PlayerDataManager.Instance.SaveCurrentSlot();
            Debug.Log($"✅ SP {spToAdd} 추가됨! (현재: {slotData.totalSP})");
        }
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("SP +10")) 
        {
            slotData.totalSP += 10;
            if (PlayerDataManager.Instance.selectedPlayerData != null)
                PlayerDataManager.Instance.selectedPlayerData.totalSP = slotData.totalSP;
            PlayerDataManager.Instance.SaveCurrentSlot();
        }
        if (GUILayout.Button("SP +30")) 
        {
            slotData.totalSP += 30;
            if (PlayerDataManager.Instance.selectedPlayerData != null)
                PlayerDataManager.Instance.selectedPlayerData.totalSP = slotData.totalSP;
            PlayerDataManager.Instance.SaveCurrentSlot();
        }
        if (GUILayout.Button("SP 초기화 (레벨과 동기화)")) 
        {
            slotData.totalSP = slotData.level;
            slotData.usedSP = 0;
            if (PlayerDataManager.Instance.selectedPlayerData != null)
            {
                PlayerDataManager.Instance.selectedPlayerData.totalSP = slotData.totalSP;
                PlayerDataManager.Instance.selectedPlayerData.usedSP = slotData.usedSP;
            }
            PlayerDataManager.Instance.SaveCurrentSlot();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // ========================================
        // 레벨 직접 설정
        // ========================================
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🆙 레벨 직접 설정", EditorStyles.boldLabel);
        levelToSet = EditorGUILayout.IntField("설정할 레벨", levelToSet);
        
        if (GUILayout.Button($"레벨을 {levelToSet}로 설정"))
        {
            var selectedData = PlayerDataManager.Instance.selectedPlayerData;
            selectedData.currentLevel = levelToSet;
            selectedData.currentExp = 0;
            selectedData.expToNextLevel = CalculateExpForLevel(levelToSet);
            
            slotData.level = levelToSet;
            slotData.exp = 0;
            slotData.expToNextLevel = selectedData.expToNextLevel;
            slotData.totalSP = levelToSet;  // SP = 레벨
            
            if (selectedData != null)
                selectedData.totalSP = slotData.totalSP;
            
            PlayerDataManager.Instance.SaveCurrentSlot();
            Debug.Log($"✅ 레벨이 {levelToSet}로 설정됨! (SP: {slotData.totalSP})");
        }
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Lv.5 설정")) SetLevel(5);
        if (GUILayout.Button("Lv.10 설정")) SetLevel(10);
        if (GUILayout.Button("Lv.20 설정")) SetLevel(20);
        if (GUILayout.Button("Lv.30 설정")) SetLevel(30);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // ========================================
        // 저장
        // ========================================
        EditorGUILayout.BeginVertical("box");
        if (GUILayout.Button("💾 현재 상태 저장", GUILayout.Height(30)))
        {
            PlayerDataManager.Instance.SaveCurrentSlot();
            Debug.Log("✅ 현재 슬롯 저장 완료!");
        }
        EditorGUILayout.EndVertical();
    }
    
    private void SetLevel(int level)
    {
        var selectedData = PlayerDataManager.Instance.selectedPlayerData;
        var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
        
        selectedData.currentLevel = level;
        selectedData.currentExp = 0;
        selectedData.expToNextLevel = CalculateExpForLevel(level);
        
        slotData.level = level;
        slotData.exp = 0;
        slotData.expToNextLevel = selectedData.expToNextLevel;
        slotData.totalSP = level;
        
        if (selectedData != null)
            selectedData.totalSP = slotData.totalSP;
        
        PlayerDataManager.Instance.SaveCurrentSlot();
        Debug.Log($"✅ 레벨이 {level}로 설정됨! (SP: {slotData.totalSP})");
    }
    
    /// <summary>
    /// 경험치 계산 (PlayerDataManager와 동일한 공식)
    /// </summary>
    private static int CalculateExpForLevel(int level)
    {
        return 100 + (level - 1) * 50; // 기본 100 + 레벨당 50씩 증가
    }
}
