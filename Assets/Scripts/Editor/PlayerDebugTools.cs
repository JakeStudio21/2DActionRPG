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
    private int runeFragmentToAdd = 100; // 🔷 룬 조각 추가 개수
    private int materialToAdd = 100; // 📦 강화 재료 추가 개수
    private string stageIdToComplete = "CH01_ST01"; // 🎯 완료할 스테이지 ID
    private int staminaToAdd = 10; // ⚡ 스태미나 추가량
    private int dungeonTicketToAdd = 5; // 🎫 던전 티켓 추가량
    private string debugEquipItemId = ""; // ⚔️ 획득할 장비 아이템 ID (예: ITEM_BOW_ARCHER_TR)
    
    // 스크롤 위치 저장 (모바일 고려)
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Player/🎮 플레이어 디버그 도구")]
    public static void ShowWindow()
    {
        var window = GetWindow<PlayerDebugTools>("플레이어 디버그");
        window.minSize = new Vector2(400, 900); // 높이 증가 (스태미나/던전 티켓 섹션 추가)
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
        
        // ========================================
        // 📜 스크롤 시작 (모바일 고려)
        // ========================================
        scrollPosition = EditorGUILayout.BeginScrollView(
            scrollPosition, 
            false, // 가로 스크롤바 없음
            true,  // 세로 스크롤바 항상 표시
            GUILayout.ExpandHeight(true)
        );
        
        // 현재 상태 표시
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("📊 현재 상태", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("캐릭터", $"{slotData.playerName} ({slotData.playerType})");
        EditorGUILayout.LabelField("레벨", $"Lv.{slotData.level}");
        EditorGUILayout.LabelField("경험치", $"{PlayerDataManager.Instance.selectedPlayerData.currentExp}/{PlayerDataManager.Instance.selectedPlayerData.expToNextLevel}");
        EditorGUILayout.LabelField("골드", $"{AccountDataManager.Instance.CurrentGold}G");
        EditorGUILayout.LabelField("SP", $"{slotData.usedSP}/{slotData.totalSP}");
        
        int currentStamina = 0;
        int maxStamina = 50;
        int ticketCount = 0;
        if (ContentEntryManager.Instance != null)
        {
            currentStamina = ContentEntryManager.Instance.GetCurrentStamina();
            maxStamina = ContentEntryManager.Instance.GetMaxStamina();
            ticketCount = ContentEntryManager.Instance.GetTicketCount();
        }
        else if (AccountDataManager.Instance != null)
        {
            var acc = AccountDataManager.Instance.GetAccountData();
            if (acc != null) { currentStamina = acc.currentStamina; ticketCount = acc.dailyDungeonTickets; }
        }
        EditorGUILayout.LabelField("스태미나", $"{currentStamina}/{maxStamina}");
        EditorGUILayout.LabelField("던전 티켓", $"{ticketCount}장");
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
        // 스태미나 추가 (계정 공유)
        // ========================================
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("⚡ 스태미나 추가", EditorStyles.boldLabel);
        staminaToAdd = EditorGUILayout.IntField("추가할 스태미나", staminaToAdd);
        
        if (AccountDataManager.Instance != null)
        {
            var acc = AccountDataManager.Instance.GetAccountData();
            if (acc != null)
            {
                if (GUILayout.Button($"스태미나 +{staminaToAdd} 추가"))
                {
                    acc.currentStamina = Mathf.Min(50, acc.currentStamina + staminaToAdd);
                    if (acc.currentStamina >= 50)
                        acc.lastStaminaUpdateTime = "";
                    AccountDataManager.Instance.Save();
                    Debug.Log($"✅ 스태미나 {staminaToAdd} 추가됨! (현재: {acc.currentStamina}/50)");
                }
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+5")) AddStamina(5);
                if (GUILayout.Button("+10")) AddStamina(10);
                if (GUILayout.Button("MAX(50)")) { acc.currentStamina = 50; acc.lastStaminaUpdateTime = ""; AccountDataManager.Instance.Save(); Debug.Log("✅ 스태미나 MAX(50) 설정!"); }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("AccountDataManager가 없습니다.", MessageType.Warning);
        }
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // ========================================
        // 던전 입장 티켓 추가 (계정 공유)
        // ========================================
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🎫 던전 입장 티켓 추가", EditorStyles.boldLabel);
        dungeonTicketToAdd = EditorGUILayout.IntField("추가할 티켓", dungeonTicketToAdd);
        
        if (ContentEntryManager.Instance != null)
        {
            if (GUILayout.Button($"티켓 +{dungeonTicketToAdd} 추가"))
            {
                ContentEntryManager.Instance.AddDungeonTickets(dungeonTicketToAdd);
                Debug.Log($"✅ 던전 티켓 {dungeonTicketToAdd}장 추가됨!");
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+5")) ContentEntryManager.Instance.AddDungeonTickets(5);
            if (GUILayout.Button("+10")) ContentEntryManager.Instance.AddDungeonTickets(10);
            if (GUILayout.Button("+50")) ContentEntryManager.Instance.AddDungeonTickets(50);
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("ContentEntryManager가 없습니다. (게임 실행 후 로비에서 사용 가능)", MessageType.Warning);
        }
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
            
            // ✅ 레벨 변경 이벤트 발생 (던전 UI 등이 구독 중)
            PlayerDataManager.Instance.TriggerLevelChanged(levelToSet);
            
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
        // 룬 조각 추가 (Phase 9: AccountData Material 연동)
        // ========================================
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🔷 룬 조각 추가 (8종류)", EditorStyles.boldLabel);
        runeFragmentToAdd = EditorGUILayout.IntField("추가할 개수", runeFragmentToAdd);
        
        EditorGUILayout.Space(5);
        GUILayout.Label("공격형 룬 조각 (4종)", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("보스 사냥꾼"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_BOSS_HUNTER, runeFragmentToAdd);
            Debug.Log($"✅ 보스 사냥꾼 룬 조각 {runeFragmentToAdd}개 추가!");
        }
        if (GUILayout.Button("방어 파괴자"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_DEFENSE_BREAKER, runeFragmentToAdd);
            Debug.Log($"✅ 방어 파괴자 룬 조각 {runeFragmentToAdd}개 추가!");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("고체력 사냥꾼"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_HIGH_HP_HUNTER, runeFragmentToAdd);
            Debug.Log($"✅ 고체력 사냥꾼 룬 조각 {runeFragmentToAdd}개 추가!");
        }
        if (GUILayout.Button("처형자"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_EXECUTIONER, runeFragmentToAdd);
            Debug.Log($"✅ 처형자 룬 조각 {runeFragmentToAdd}개 추가!");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        GUILayout.Label("생존형 룬 조각 (3종)", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("보스 철벽"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_BOSS_DEFENDER, runeFragmentToAdd);
            Debug.Log($"✅ 보스 철벽 룬 조각 {runeFragmentToAdd}개 추가!");
        }
        if (GUILayout.Button("불굴의 생존자"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_SURVIVOR, runeFragmentToAdd);
            Debug.Log($"✅ 불굴의 생존자 룬 조각 {runeFragmentToAdd}개 추가!");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("장판 철벽"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_AREA_DEFENDER, runeFragmentToAdd);
            Debug.Log($"✅ 장판 철벽 룬 조각 {runeFragmentToAdd}개 추가!");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        GUILayout.Label("유틸리티 룬 조각 (1종)", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("흡혈 룬"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_LIFESTEAL, runeFragmentToAdd);
            Debug.Log($"✅ 흡혈 룬 조각 {runeFragmentToAdd}개 추가!");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        GUILayout.Label("빠른 추가", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("모든 룬 조각 +100"))
        {
            AddAllRuneFragments(100);
        }
        if (GUILayout.Button("모든 룬 조각 +1000"))
        {
            AddAllRuneFragments(1000);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // ========================================
        // 강화 재료 추가 (9종류)
        // ========================================
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("📦 강화 재료 추가 (9종류)", EditorStyles.boldLabel);
        materialToAdd = EditorGUILayout.IntField("추가할 개수", materialToAdd);
        
        EditorGUILayout.Space(5);
        GUILayout.Label("⚔️ 무기 재료 (3종)", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("무기 파편 (D/C/B)"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.WeaponFragment, materialToAdd);
            Debug.Log($"✅ 무기 강화 파편 {materialToAdd}개 추가!");
        }
        if (GUILayout.Button("무기 결정 (A/S/SS)"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.WeaponCrystal, materialToAdd);
            Debug.Log($"✅ 무기 강화 결정 {materialToAdd}개 추가!");
        }
        if (GUILayout.Button("무기 코어 (EX/TR)"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.WeaponCore, materialToAdd);
            Debug.Log($"✅ 무기 강화 코어 {materialToAdd}개 추가!");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        GUILayout.Label("🛡️ 방어구 재료 (3종)", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("방어구 파편 (D/C/B)"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.ArmorFragment, materialToAdd);
            Debug.Log($"✅ 방어구 강화 파편 {materialToAdd}개 추가!");
        }
        if (GUILayout.Button("방어구 결정 (A/S/SS)"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.ArmorCrystal, materialToAdd);
            Debug.Log($"✅ 방어구 강화 결정 {materialToAdd}개 추가!");
        }
        if (GUILayout.Button("방어구 코어 (EX/TR)"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.ArmorCore, materialToAdd);
            Debug.Log($"✅ 방어구 강화 코어 {materialToAdd}개 추가!");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        GUILayout.Label("💍 악세사리 재료 (3종)", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("악세사리 파편 (D/C/B)"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.AccessoryFragment, materialToAdd);
            Debug.Log($"✅ 악세사리 강화 파편 {materialToAdd}개 추가!");
        }
        if (GUILayout.Button("악세사리 결정 (A/S/SS)"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.AccessoryCrystal, materialToAdd);
            Debug.Log($"✅ 악세사리 강화 결정 {materialToAdd}개 추가!");
        }
        if (GUILayout.Button("악세사리 코어 (EX/TR)"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.AccessoryCore, materialToAdd);
            Debug.Log($"✅ 악세사리 강화 코어 {materialToAdd}개 추가!");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        GUILayout.Label("빠른 추가", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("모든 강화 재료 +100"))
        {
            AddAllMaterials(100);
        }
        if (GUILayout.Button("모든 강화 재료 +1000"))
        {
            AddAllMaterials(1000);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // ========================================
        // 정령의 정수 추가 (4종류) - Phase 9: 저항 시스템
        // ========================================
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🌟 정령의 정수 추가 (4종류)", EditorStyles.boldLabel);
        
        EditorGUILayout.Space(5);
        GUILayout.Label("정령의 가호 (저항 시스템) 재료", EditorStyles.miniBoldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🌳 숲의 정수 (속박)"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.SPIRIT_ESSENCE_FOREST, materialToAdd);
            Debug.Log($"✅ 숲의 정수 {materialToAdd}개 추가! (속박 저항용)");
        }
        if (GUILayout.Button("🔥 불꽃의 정수 (화상)"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.SPIRIT_ESSENCE_FLAME, materialToAdd);
            Debug.Log($"✅ 불꽃의 정수 {materialToAdd}개 추가! (화상 저항용)");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🌍 대지의 정수 (중독)"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.SPIRIT_ESSENCE_EARTH, materialToAdd);
            Debug.Log($"✅ 대지의 정수 {materialToAdd}개 추가! (중독 저항용)");
        }
        if (GUILayout.Button("💧 물결의 정수 (둔화)"))
        {
            AccountDataManager.Instance.AddMaterial(MaterialType.SPIRIT_ESSENCE_WATER, materialToAdd);
            Debug.Log($"✅ 물결의 정수 {materialToAdd}개 추가! (둔화 저항용)");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        GUILayout.Label("빠른 추가", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("모든 정령의 정수 +100"))
        {
            AddAllSpiritEssences(100);
        }
        if (GUILayout.Button("모든 정령의 정수 +1000"))
        {
            AddAllSpiritEssences(1000);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // ========================================
        // 🎯 스테이지 클리어 (해금 안된 스테이지도 가능)
        // ========================================
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🎯 스테이지 클리어 (디버그)", EditorStyles.boldLabel);
        
        // 스테이지 ID 입력
        stageIdToComplete = EditorGUILayout.TextField("스테이지 ID", stageIdToComplete);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("예시: CH01_ST01, CH01_ST02, CH02_ST01 등\n해금되지 않은 스테이지도 클리어 처리 가능합니다.", MessageType.Info);
        
        // 클리어 버튼
        if (GUILayout.Button($"🎯 {stageIdToComplete} 스테이지 클리어", GUILayout.Height(30)))
        {
            CompleteStageDebug(stageIdToComplete);
        }
        
        EditorGUILayout.Space(5);
        GUILayout.Label("빠른 클리어", EditorStyles.miniBoldLabel);
        
        // 챕터 1 빠른 클리어
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("CH01_ST01 클리어")) CompleteStageDebug("CH01_ST01");
        if (GUILayout.Button("CH01_ST02 클리어")) CompleteStageDebug("CH01_ST02");
        if (GUILayout.Button("CH01_ST03 클리어")) CompleteStageDebug("CH01_ST03");
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("CH01_ST05 클리어")) CompleteStageDebug("CH01_ST05");
        if (GUILayout.Button("CH01_ST10 클리어")) CompleteStageDebug("CH01_ST10");
        EditorGUILayout.EndHorizontal();
        
        // 챕터 2 빠른 클리어
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("CH02_ST01 클리어")) CompleteStageDebug("CH02_ST01");
        if (GUILayout.Button("CH02_ST05 클리어")) CompleteStageDebug("CH02_ST05");
        if (GUILayout.Button("CH02_ST10 클리어")) CompleteStageDebug("CH02_ST10");
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // ========================================
        // ⚔️ 장비 획득 (디버그)
        // ========================================
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("⚔️ 장비 획득 (디버그)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("예시: ITEM_BOW_D / ITEM_BOW_TR\n상점 구매와 동일한 경로 — 계정 공유 창고에 저장됩니다.", MessageType.Info);
        
        EditorGUILayout.BeginHorizontal();
        debugEquipItemId = EditorGUILayout.TextField("아이템 ID", debugEquipItemId);
        if (GUILayout.Button("획득", GUILayout.Width(60)))
        {
            AcquireEquipmentForDebug(debugEquipItemId);
        }
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
            AccountDataManager.Instance.Save(); // 룬 조각도 저장
            Debug.Log("✅ 현재 슬롯 저장 완료!");
        }
        EditorGUILayout.EndVertical();
        
        // ========================================
        // 📜 스크롤 종료
        // ========================================
        EditorGUILayout.EndScrollView();
    }
    
    private void AddStamina(int amount)
    {
        if (AccountDataManager.Instance == null) return;
        var acc = AccountDataManager.Instance.GetAccountData();
        if (acc == null) return;
        
        acc.currentStamina = Mathf.Min(50, acc.currentStamina + amount);
        if (acc.currentStamina >= 50)
            acc.lastStaminaUpdateTime = "";
        AccountDataManager.Instance.Save();
        Debug.Log($"✅ 스태미나 +{amount} 추가! (현재: {acc.currentStamina}/50)");
    }
    
    private void SetLevel(int level)
    {
        var selectedData = PlayerDataManager.Instance.selectedPlayerData;
        var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
        
        selectedData.currentLevel = level;
        selectedData.currentExp = 0;
        selectedData.expToNextLevel = CalculateExpForLevel(level);
        
        // ✅ 레벨 변경 이벤트 발생 (던전 UI 등이 구독 중)
        PlayerDataManager.Instance.TriggerLevelChanged(level);
        
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
    
    /// <summary>
    /// 모든 룬 조각 일괄 추가
    /// </summary>
    private void AddAllRuneFragments(int amount)
    {
        if (AccountDataManager.Instance == null)
        {
            Debug.LogError("❌ AccountDataManager를 찾을 수 없습니다!");
            return;
        }
        
        // 8종류 룬 조각 추가
        AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_BOSS_HUNTER, amount);
        AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_BOSS_DEFENDER, amount);
        AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_DEFENSE_BREAKER, amount);
        AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_HIGH_HP_HUNTER, amount);
        AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_EXECUTIONER, amount);
        AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_SURVIVOR, amount);
        AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_AREA_DEFENDER, amount);
        AccountDataManager.Instance.AddMaterial(MaterialType.RUNE_FRAG_RUNE_LIFESTEAL, amount);
        
        AccountDataManager.Instance.Save();
        Debug.Log($"✅ 모든 룬 조각 {amount}개씩 추가 완료! (총 8종류)");
    }
    
    /// <summary>
    /// 모든 강화 재료 일괄 추가
    /// </summary>
    private void AddAllMaterials(int amount)
    {
        if (AccountDataManager.Instance == null)
        {
            Debug.LogError("❌ AccountDataManager를 찾을 수 없습니다!");
            return;
        }
        
        // 9종류 강화 재료 추가
        AccountDataManager.Instance.AddMaterial(MaterialType.WeaponFragment, amount);
        AccountDataManager.Instance.AddMaterial(MaterialType.WeaponCrystal, amount);
        AccountDataManager.Instance.AddMaterial(MaterialType.WeaponCore, amount);
        AccountDataManager.Instance.AddMaterial(MaterialType.ArmorFragment, amount);
        AccountDataManager.Instance.AddMaterial(MaterialType.ArmorCrystal, amount);
        AccountDataManager.Instance.AddMaterial(MaterialType.ArmorCore, amount);
        AccountDataManager.Instance.AddMaterial(MaterialType.AccessoryFragment, amount);
        AccountDataManager.Instance.AddMaterial(MaterialType.AccessoryCrystal, amount);
        AccountDataManager.Instance.AddMaterial(MaterialType.AccessoryCore, amount);
        
        AccountDataManager.Instance.Save();
        Debug.Log($"✅ 모든 강화 재료 {amount}개씩 추가 완료! (총 9종류)");
    }
    
    /// <summary>
    /// 모든 정령의 정수 일괄 추가 (Phase 9: 저항 시스템)
    /// </summary>
    private void AddAllSpiritEssences(int amount)
    {
        if (AccountDataManager.Instance == null)
        {
            Debug.LogError("❌ AccountDataManager를 찾을 수 없습니다!");
            return;
        }
        
        // 4종류 정령의 정수 추가
        AccountDataManager.Instance.AddMaterial(MaterialType.SPIRIT_ESSENCE_FOREST, amount);  // 숲의 정수 (속박 저항)
        AccountDataManager.Instance.AddMaterial(MaterialType.SPIRIT_ESSENCE_FLAME, amount);   // 불꽃의 정수 (화상 저항)
        AccountDataManager.Instance.AddMaterial(MaterialType.SPIRIT_ESSENCE_EARTH, amount);   // 대지의 정수 (중독 저항)
        AccountDataManager.Instance.AddMaterial(MaterialType.SPIRIT_ESSENCE_WATER, amount);   // 물결의 정수 (둔화 저항)
        
        AccountDataManager.Instance.Save();
        Debug.Log($"✅ 모든 정령의 정수 {amount}개씩 추가 완료! (총 4종류)");
    }
    
    /// <summary>
    /// 장비 디버그 획득 — 상점 구매와 동일한 경로로 계정 공유 창고에 저장
    /// RegisterNewInstance → DynamicEquipmentGenerator → TryAddToShared → Save
    /// </summary>
    private void AcquireEquipmentForDebug(string itemId)
    {
        Debug.Log($"[장비 획득] 버튼 클릭됨. 입력 ID: '{itemId}'");

        if (string.IsNullOrWhiteSpace(itemId))
        {
            Debug.LogError("❌ [장비 획득] 아이템 ID가 비어있습니다.");
            return;
        }

        if (AccountDataManager.Instance == null)
        {
            Debug.LogError("❌ [장비 획득] AccountDataManager를 찾을 수 없습니다.");
            return;
        }

        // 1. 인스턴스 등록 (itemInstances)
        var newId = AccountDataManager.Instance.RegisterNewInstance(itemId);
        if (newId.IsEmpty)
        {
            Debug.LogError($"❌ [장비 획득] 인스턴스 등록 실패: '{itemId}'");
            return;
        }

        // 2. 동적 스탯 생성 (주옵션 + 부옵션)
        EquipmentData equipData = ItemTemplateResolver.Load(itemId);
        if (equipData != null)
        {
            EquipmentInstance dynamicInstance = DynamicEquipmentGenerator.Generate(equipData, equipData.itemGrade);
            if (dynamicInstance != null)
            {
                ItemInstanceData instanceData = AccountDataManager.Instance.GetInstance(newId);
                if (instanceData != null)
                    EquipmentInstanceConverter.ApplyDynamicStats(instanceData, dynamicInstance);
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ [장비 획득] EquipmentData 로드 실패: '{itemId}' — 동적 스탯 없이 등록됩니다.");
        }

        // 3. 계정 공유 창고에 추가 (sharedInventoryIds) — 창고 가득 시 우편함
        bool addedToShared = AccountDataManager.Instance.TryAddToShared(newId);
        if (!addedToShared)
        {
            AccountDataManager.Instance.MoveToMailbox(newId);
            Debug.LogWarning($"⚠️ [장비 획득] 공유 창고가 가득 찼습니다. 우편함으로 전송: '{itemId}'");
        }

        // 4. 저장
        AccountDataManager.Instance.Save();

        // 5. UI 갱신 이벤트 발생 (LobbyInventoryController, ShopUIController 등 구독자 일괄 갱신)
        PlayerDataManager.Instance?.TriggerInventoryChanged();

        // 6. 결과 로그
        var saved = AccountDataManager.Instance.GetInstance(newId);
        int subCount = saved?.randomSubStats?.Count ?? 0;
        string destination = addedToShared ? "공유 창고" : "우편함";
        Debug.Log($"✅ [장비 획득] 성공: {itemId} → {destination}\n" +
                  $"   주옵션: {saved?.finalMainStatValue:F1} | 부옵션: {subCount}개");

        if (saved?.randomSubStats != null)
        {
            for (int i = 0; i < saved.randomSubStats.Count; i++)
            {
                var sub = saved.randomSubStats[i];
                Debug.Log($"   부옵션 [{i + 1}] {sub.statType}: {sub.value:F3}");
            }
        }
    }

    /// <summary>
    /// 스테이지 강제 클리어 (해금 안된 스테이지도 가능)
    /// </summary>
    private void CompleteStageDebug(string stageId)
    {
        if (string.IsNullOrEmpty(stageId))
        {
            Debug.LogError("❌ 스테이지 ID가 비어있습니다!");
            return;
        }
        
        // StageProgressManager 확인
        if (StageSystem.StageProgressManager.Instance == null)
        {
            Debug.LogError("❌ StageProgressManager를 찾을 수 없습니다!");
            return;
        }
        
        var progressManager = StageSystem.StageProgressManager.Instance;
        
        // 1단계: 스테이지 해금 (해금되지 않은 경우)
        if (!progressManager.IsStageUnlocked(stageId))
        {
            progressManager.UnlockStage(stageId);
            Debug.Log($"🔓 스테이지 {stageId} 강제 해금!");
        }
        
        // 2단계: 스테이지 클리어
        if (!progressManager.IsStageCompleted(stageId))
        {
            progressManager.CompleteStage(stageId, completionTime: 60f, isFirstClear: true);
            Debug.Log($"✅ 스테이지 {stageId} 클리어 처리 완료!");
        }
        else
        {
            Debug.LogWarning($"⚠️ 스테이지 {stageId}는 이미 클리어된 상태입니다.");
        }
        
        // 3단계: 저장
        PlayerDataManager.Instance.SaveCurrentSlot();
        AccountDataManager.Instance.Save();
        
        Debug.Log($"🎯 [디버그 도구] {stageId} 스테이지 클리어 처리 완료!");
    }
}
