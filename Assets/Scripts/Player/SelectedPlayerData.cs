using UnityEngine;
using System.Collections.Generic;
using System.Linq; // 추가

/// <summary>
/// ⭐ [Phase 3] 인게임 런타임 전용 플레이어 데이터 (ScriptableObject 캐시)
/// 씬 전환 간 데이터 유지 + 임시 변경사항 관리
/// </summary>
[CreateAssetMenu(fileName = "SelectedPlayerData", menuName = "Game/Selected Player Data")]
public class SelectedPlayerData : ScriptableObject
{
    [Header("🎮 현재 선택된 플레이어")]
    public int selectedSlotIndex = 0;
    public string playerName = "Player";
    public PlayerType selectedPlayerType = PlayerType.None;
    public string weaponName = ""; // 기존 호환성 유지
    
    [Header("📈 런타임 진행 상황")]
    public int currentLevel = 1;
    public int currentExp = 0;
    public int expToNextLevel = 100;
    public int currentGold = 0;
    
    [Header("⏰ 세션 정보")]
    [Tooltip("마지막 플레이 시간 (의미 있는 이벤트에서만 갱신)")]
    public string lastPlayTime = "";
    
    [Header("🎒 런타임 인벤토리 & 장비")]
    public List<EquipmentData> runtimeInventoryItems = new List<EquipmentData>();
    [SerializeField] private List<EquipmentSlot> equippedSlotKeys = new List<EquipmentSlot>();
    [SerializeField] private List<EquipmentData> equippedSlotValues = new List<EquipmentData>();
    
    public int maxInventorySize = 16; // 50 → 16으로 변경
    
    [Header("🎯 런타임 클래스 특성")]
    public int classLevel = 1;
    [SerializeField] private List<string> runtimeStatKeys = new List<string>();
    [SerializeField] private List<float> runtimeStatValues = new List<float>();
    
    [Header("🎬 챕터 시스템 (Phase 5)")]
    public string pendingCutsceneId = null;
    public int pendingChapterId = 0;
    
    [Header("📊 챕터 진행도 (Phase 1 근본 해결)")]
    [Tooltip("클리어한 챕터 목록 (1~5)")]
    public List<int> clearedChapters = new List<int>();
    
    [Header("🎬 컷신 시청 여부 (Phase 7 준비)")]
    [Tooltip("챕터 시작 컷신 시청 목록 (예: CH01_START)")]
    public List<string> seenChapterStart = new List<string>();
    
    [Tooltip("챕터 종료 컷신 시청 목록 (예: CH01_CLEAR)")]
    public List<string> seenChapterClear = new List<string>();
    
    [Tooltip("스테이지 입장 컷신 시청 목록 (예: CH01_ST01_ENTER)")]
    public List<string> seenStageEnter = new List<string>();
    
    [Tooltip("스테이지 클리어 컷신 시청 목록 (예: CH01_ST01_CLEAR)")]
    public List<string> seenStageClear = new List<string>();
    
    [Header("🎯 마지막 플레이 위치 (Phase 6)")]
    [Tooltip("마지막으로 플레이한 챕터 (UI 표시용)")]
    public int currentChapterId = 1;
    
    [Tooltip("마지막으로 플레이한 스테이지 ID (예: CH03_ST05)")]
    public string lastPlayedStageId = "";
    
    [Header("🎯 스테이지 진행도")]
    public List<StageSystem.StageProgress> stageProgresses = new List<StageSystem.StageProgress>();
    
    // Dictionary로 변환하여 사용
    private Dictionary<EquipmentSlot, EquipmentData> _runtimeEquippedItems = null;
    public Dictionary<EquipmentSlot, EquipmentData> RuntimeEquippedItems
    {
        get
        {
            if (_runtimeEquippedItems == null)
            {
                _runtimeEquippedItems = new Dictionary<EquipmentSlot, EquipmentData>();
                
                // 모든 슬롯을 null로 초기화
                foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
                {
                    _runtimeEquippedItems[slot] = null;
                }
                
                // 저장된 데이터 복원
                for (int i = 0; i < Mathf.Min(equippedSlotKeys.Count, equippedSlotValues.Count); i++)
                {
                    _runtimeEquippedItems[equippedSlotKeys[i]] = equippedSlotValues[i];
                }
            }
            return _runtimeEquippedItems;
        }
    }
    
    private Dictionary<string, float> _runtimeExtraStats = null;
    public Dictionary<string, float> RuntimeExtraStats
    {
        get
        {
            if (_runtimeExtraStats == null)
            {
                _runtimeExtraStats = new Dictionary<string, float>();
                for (int i = 0; i < Mathf.Min(runtimeStatKeys.Count, runtimeStatValues.Count); i++)
                {
                    _runtimeExtraStats[runtimeStatKeys[i]] = runtimeStatValues[i];
                }
            }
            return _runtimeExtraStats;
        }
    }
    
    /// <summary>
    /// PlayerSlotData에서 런타임 데이터로 복사
    /// </summary>
    public void LoadFromSlotData(PlayerSlotData slotData)
    {
        if (slotData == null) return;
        
        selectedSlotIndex = slotData.slotIndex;
        playerName = slotData.playerName;
        selectedPlayerType = slotData.playerType;
        weaponName = selectedPlayerType.GetDefaultWeapon(); // 기존 호환성
        
        currentLevel = slotData.level;
        currentExp = slotData.exp;
        expToNextLevel = slotData.expToNextLevel;
        currentGold = slotData.gold;
        
        lastPlayTime = slotData.lastPlayTime ?? ""; // 세션 정보
        
        classLevel = slotData.classLevel;
        maxInventorySize = slotData.maxInventorySize;
        
        // 인벤토리 복사 (Resources에서 로드)
        runtimeInventoryItems.Clear();
        foreach (string itemName in slotData.inventoryItemNames)
        {
            var item = Resources.Load<EquipmentData>($"Equipment/{itemName}");
            if (item != null) 
            {
                runtimeInventoryItems.Add(item);
            }
            else
            {
                Debug.LogWarning($"⚠️ [SelectedPlayerData] 인벤토리 아이템을 찾을 수 없음: {itemName}");
            }
        }
        
        // 장비 복사
        RuntimeEquippedItems.Clear();
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            RuntimeEquippedItems[slot] = null;
        }
        
        // ⭐ V2 시스템 우선 처리 (Phase 4.5)
        if (slotData.equippedRecords != null && slotData.equippedRecords.Count > 0)
        {
            // V2: equippedRecords에서 ItemInstanceId → EquipmentData 변환
            var account = AccountDataManager.Instance;
            foreach (var record in slotData.equippedRecords)
            {
                if (record.instanceId.IsValid())
                {
                    var instance = account?.GetInstance(record.instanceId);
                    if (instance != null)
                    {
                        Debug.Log($"🔍 [SelectedPlayerData] V2 장비 로드 시도: templateName={instance.templateName}, slot={record.slot}");
                        
                        var item = ItemTemplateResolver.Load(instance.templateName);
                        if (item != null)
                        {
                            RuntimeEquippedItems[record.slot] = item;
                            
                            // ⚠️ 디버그: EquipmentData 상세 정보
                            Debug.Log($"✅ [SelectedPlayerData] V2 장비 로드 성공:");
                            Debug.Log($"   - equipmentName: {item.equipmentName}");
                            Debug.Log($"   - name (asset): {item.name}");
                            Debug.Log($"   - equipmentType: {item.equipmentType}");
                            Debug.Log($"   - slot: {record.slot}");
                            Debug.Log($"   - equipmentPrefab: {(item.equipmentPrefab != null ? item.equipmentPrefab.name : "null")}");
                        }
                        else
                        {
                            Debug.LogError($"❌ [SelectedPlayerData] V2 장비 템플릿을 찾을 수 없음: {instance.templateName}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ [SelectedPlayerData] V2 장비 인스턴스를 찾을 수 없음: {record.instanceId.id}");
                    }
                }
            }
        }
        // Legacy 시스템 (V2 데이터가 없을 때만)
        else if (slotData.equippedItemNames != null)
        {
            foreach (var kvp in slotData.equippedItemNames)
            {
                if (System.Enum.TryParse<EquipmentSlot>(kvp.Key, out EquipmentSlot slot))
                {
                    var item = Resources.Load<EquipmentData>($"Equipment/{kvp.Value}");
                    if (item != null) 
                    {
                        RuntimeEquippedItems[slot] = item;
                        Debug.Log($"✅ [SelectedPlayerData] Legacy 장비 로드: {item.equipmentName} → {slot}");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ [SelectedPlayerData] Legacy 장비 아이템을 찾을 수 없음: {kvp.Value}");
                    }
                }
            }
        }
        
        // 특성 복사
        RuntimeExtraStats.Clear();
        foreach (var kvp in slotData.ExtraStats)
        {
            RuntimeExtraStats[kvp.Key] = kvp.Value;
        }
        
        // ========================================
        // 📌 스테이지 진행도
        // ========================================
        stageProgresses = slotData.stageProgresses != null
            ? new List<StageSystem.StageProgress>(slotData.stageProgresses)
            : new List<StageSystem.StageProgress>();
        
        // ========================================
        // 📌 챕터 진행도 (Phase 1 근본 해결)
        // ========================================
        clearedChapters = slotData.clearedChapters != null
            ? new List<int>(slotData.clearedChapters)
            : new List<int>();
        
        // ========================================
        // 📌 컷신 시청 여부 (Phase 7 준비)
        // ========================================
        seenChapterStart = slotData.seenChapterStart != null
            ? new List<string>(slotData.seenChapterStart)
            : new List<string>();
            
        seenChapterClear = slotData.seenChapterClear != null
            ? new List<string>(slotData.seenChapterClear)
            : new List<string>();
            
        seenStageEnter = slotData.seenStageEnter != null
            ? new List<string>(slotData.seenStageEnter)
            : new List<string>();
            
        seenStageClear = slotData.seenStageClear != null
            ? new List<string>(slotData.seenStageClear)
            : new List<string>();
        
        // ========================================
        // 📌 예약된 컷신 (Phase 5)
        // ========================================
        pendingCutsceneId = slotData.pendingCutsceneId;
        pendingChapterId = slotData.pendingChapterId;
        
        // ========================================
        // 📌 마지막 플레이 위치 (Phase 6)
        // ========================================
        currentChapterId = slotData.lastPlayedChapterId > 0 
            ? slotData.lastPlayedChapterId 
            : CalculateLastPlayedChapter(slotData.stageProgresses);
        
        lastPlayedStageId = slotData.lastPlayedStageId ?? "";
        
        SyncDictionaries();
        
        Debug.Log($"📥 [SelectedPlayerData] 슬롯 {slotData.slotIndex} 데이터 완전 로드 완료");
    }
    
    /// <summary>
    /// stageProgresses에서 마지막 플레이 챕터 계산 (fallback)
    /// </summary>
    private int CalculateLastPlayedChapter(List<StageSystem.StageProgress> progresses)
    {
        if (progresses == null || progresses.Count == 0) return 1;
        
        int maxChapter = 1;
        foreach (var progress in progresses)
        {
            if (StageSystem.StageIdValidator.IsValidChapterStageId(progress.stageId))
            {
                int chapterId = StageSystem.StageIdValidator.ExtractChapterId(progress.stageId);
                if (chapterId > maxChapter)
                    maxChapter = chapterId;
            }
        }
        return maxChapter;
    }
    
    /// <summary>
    /// ⭐ SelectedPlayerData를 PlayerSlotData로 완전 복제 (Full Dump)
    /// 이 메서드는 PlayerSlotData의 모든 필드를 채워야 한다!
    /// </summary>
    public PlayerSlotData SaveToSlotData()
    {
        var slotData = new PlayerSlotData
        {
            slotIndex = selectedSlotIndex,
            playerName = playerName,
            playerType = selectedPlayerType,
            lastPlayTime = this.lastPlayTime, // 🔧 SelectedPlayerData의 값 사용 (의미있는 이벤트에서만 갱신)
            isSlotUsed = true,
            
            level = currentLevel,
            exp = currentExp,
            expToNextLevel = expToNextLevel,
            gold = currentGold,
            
            classLevel = classLevel,
            isClassUnlocked = true,
            maxInventorySize = maxInventorySize
        };
        
        // 인벤토리 저장
        slotData.inventoryItemNames.Clear();
        foreach (var item in runtimeInventoryItems)
        {
            if (item != null) slotData.inventoryItemNames.Add(item.name);
        }
        
        // 장비 저장
        slotData.equippedItemNames.Clear();
        foreach (var kvp in RuntimeEquippedItems)
        {
            if (kvp.Value != null)
            {
                slotData.SetEquippedItem(kvp.Key.ToString(), kvp.Value.name);
            }
        }
        
        // 특성 저장
        foreach (var kvp in RuntimeExtraStats)
        {
            slotData.SetExtraStat(kvp.Key, kvp.Value);
        }
        
        // ========================================
        // 📌 스테이지 진행도 (Full Dump)
        // ========================================
        slotData.stageProgresses = this.stageProgresses != null
            ? new List<StageSystem.StageProgress>(this.stageProgresses)
            : new List<StageSystem.StageProgress>();
        
        // ========================================
        // 📌 챕터 진행도 (Phase 1 근본 해결)
        // ========================================
        slotData.clearedChapters = this.clearedChapters != null
            ? new List<int>(this.clearedChapters)
            : new List<int>();
        
        // ========================================
        // 📌 컷신 시청 여부 (Phase 7 준비)
        // ========================================
        slotData.seenChapterStart = this.seenChapterStart != null
            ? new List<string>(this.seenChapterStart)
            : new List<string>();
            
        slotData.seenChapterClear = this.seenChapterClear != null
            ? new List<string>(this.seenChapterClear)
            : new List<string>();
            
        slotData.seenStageEnter = this.seenStageEnter != null
            ? new List<string>(this.seenStageEnter)
            : new List<string>();
            
        slotData.seenStageClear = this.seenStageClear != null
            ? new List<string>(this.seenStageClear)
            : new List<string>();
        
        // ========================================
        // 📌 예약된 컷신 (Phase 5)
        // ========================================
        slotData.pendingCutsceneId = this.pendingCutsceneId ?? "";
        slotData.pendingChapterId = this.pendingChapterId;
        
        // ========================================
        // 📌 마지막 플레이 위치 (Phase 6)
        // ========================================
        slotData.lastPlayedChapterId = this.currentChapterId;
        slotData.lastPlayedStageId = this.lastPlayedStageId ?? "";
        
        // ========================================
        // 📌 V2 인벤토리 & 장비 (Phase 0-7) ⭐ 중요!
        // ========================================
        // ⚠️ 주의: PlayerSlotData가 실제 V2 데이터를 가지고 있으므로,
        // SelectedPlayerData는 이를 직접 저장하지 않습니다.
        // 대신 PlayerDataManager.GetSlotData()에서 최신 데이터를 가져와야 합니다!
        // 여기서는 비워둡니다 (PlayerDataManager.AddItemV2에서 직접 처리).
        
        Debug.Log($"💾 [SelectedPlayerData] PlayerSlotData 완전 복제 완료: Lv.{slotData.level}, Gold:{slotData.gold}, Chapters:{slotData.clearedChapters.Count}");
        Debug.Log($"⚠️ [SelectedPlayerData] V2 데이터는 PlayerDataManager.GetSlotData()의 최신 데이터 사용");
        return slotData;
    }
    
    /// <summary>
    /// Dictionary를 SerializeField로 동기화
    /// </summary>
    public void SyncDictionaries()
    {
        // 장비 동기화
        equippedSlotKeys.Clear();
        equippedSlotValues.Clear();
        foreach (var kvp in RuntimeEquippedItems)
        {
            equippedSlotKeys.Add(kvp.Key);
            equippedSlotValues.Add(kvp.Value);
        }
        
        // 특성 동기화
        runtimeStatKeys.Clear();
        runtimeStatValues.Clear();
        foreach (var kvp in RuntimeExtraStats)
        {
            runtimeStatKeys.Add(kvp.Key);
            runtimeStatValues.Add(kvp.Value);
        }
    }
    
    /// <summary>
    /// 런타임 특성 값 설정
    /// </summary>
    public void SetRuntimeStat(string key, float value)
    {
        RuntimeExtraStats[key] = value;
        SyncDictionaries();
    }
    
    /// <summary>
    /// 런타임 특성 값 가져오기
    /// </summary>
    public float GetRuntimeStat(string key, float defaultValue = 0f)
    {
        return RuntimeExtraStats.ContainsKey(key) ? RuntimeExtraStats[key] : defaultValue;
    }
    
    // 기존 PlayerDataManager 인터페이스 호환성을 위한 프로퍼티들
    public int CurrentGold => currentGold;
    public int CurrentLevel => currentLevel;
    public int CurrentExp => currentExp;
    public int ExpToNextLevel => expToNextLevel;
    public List<EquipmentData> InventoryItems => new List<EquipmentData>(runtimeInventoryItems);
    public Dictionary<EquipmentSlot, EquipmentData> EquippedItems => new Dictionary<EquipmentSlot, EquipmentData>(RuntimeEquippedItems);
    public int CurrentInventorySize => runtimeInventoryItems.Count;
    public int MaxInventorySize => maxInventorySize;
    public bool IsInventoryFull => CurrentInventorySize >= MaxInventorySize;
    
    /// <summary>
    /// 🔄 기존 호환성: 데이터 초기화
    /// </summary>
    public void Reset()
    {
        selectedPlayerType = PlayerType.None;
        weaponName = "";
        currentLevel = 1;
        currentGold = 0;
        currentExp = 0;
        expToNextLevel = 100;
        lastPlayTime = ""; // 세션 정보 초기화
        
        runtimeInventoryItems.Clear();
        RuntimeEquippedItems.Clear();
        RuntimeExtraStats.Clear();
        SyncDictionaries();
    }
    
    /// <summary>
    /// 🔄 기존 호환성: 플레이어가 선택되었는지 확인
    /// </summary>
    public bool IsPlayerSelected()
    {
        return selectedPlayerType != PlayerType.None && selectedSlotIndex >= 0;
    }
    
    /// <summary>
    /// 디버그용 정보 출력
    /// </summary>
    public override string ToString()
    {
        return $"Slot[{selectedSlotIndex}] {playerName}({selectedPlayerType}) Lv.{currentLevel} Gold:{currentGold} Inv:{CurrentInventorySize}/{MaxInventorySize}";
    }
}

/// <summary>
/// 플레이어 캐릭터 타입 열거형
/// </summary>
public enum PlayerType
{
    None = 0,
    Warrior = 1,    // 전사 - 검 사용
    Assasin = 2,    // 어쌔신 - 활 사용  
    Wizard = 3      // 마법사 - 스태프 사용
}

/// <summary>
/// PlayerType 관련 유틸리티 메서드
/// </summary>
public static class PlayerTypeExtensions
{
    /// <summary>
    /// 플레이어 타입에 따른 기본 무기 이름 반환
    /// </summary>
    public static string GetDefaultWeapon(this PlayerType playerType)
    {
        return playerType switch
        {
            PlayerType.Warrior => "Sword",
            PlayerType.Assasin => "Bow", 
            PlayerType.Wizard => "Staff",
            _ => ""
        };
    }
    
    /// <summary>
    /// 플레이어 타입에 따른 표시 이름 반환
    /// </summary>
    public static string GetDisplayName(this PlayerType playerType)
    {
        return playerType switch
        {
            PlayerType.Warrior => "전사",
            PlayerType.Assasin => "어쌔신",
            PlayerType.Wizard => "마법사",
            _ => "선택 안함"
        };
    }
    
    /// <summary>
    /// 플레이어 타입이 유효한지 확인
    /// </summary>
    public static bool IsValid(this PlayerType playerType)
    {
        return playerType != PlayerType.None;
    }
}
