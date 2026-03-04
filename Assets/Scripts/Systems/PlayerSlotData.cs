using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// ⭐ [Phase 3] 캐릭터 슬롯별 저장 데이터 (완전 새 구조)
/// JSON 파일 기반 저장 전용 데이터 컨테이너
/// </summary>
[System.Serializable]
public class PlayerSlotData
{
    [Header("🎮 슬롯 기본 정보")]
    public int slotIndex = 0;
    public string playerName = "Player";
    public PlayerType playerType = PlayerType.Warrior;
    public string lastPlayTime;
    public bool isSlotUsed = false;
    
    [Header("📈 진행 상황")]
    public int level = 1;
    public int exp = 0;
    public int expToNextLevel = 100;
    
    [System.Obsolete("V2: 골드는 AccountData.gold로 이동됨 (계정 공유)")]
    public int gold = 0; // ⚠️ Deprecated: V2에서는 AccountData.gold 사용
    
    [Header("🎯 스테이지 진행도")]
    public List<StageSystem.StageProgress> stageProgresses = new List<StageSystem.StageProgress>();
    
    [Header("✅ Phase 0: 챕터 진행도")]
    [Tooltip("클리어한 챕터 목록 (1~5)")]
    public List<int> clearedChapters = new List<int>();
    
    [Header("🎬 컷신 시청 여부")]
    [Tooltip("챕터 시작 컷신 시청 목록 (예: CH01_START)")]
    public List<string> seenChapterStart = new List<string>();
    
    [Tooltip("챕터 종료 컷신 시청 목록 (예: CH01_CLEAR)")]
    public List<string> seenChapterClear = new List<string>();
    
    [Tooltip("스테이지 입장 컷신 시청 목록 (예: CH01_ST01_ENTER)")]
    public List<string> seenStageEnter = new List<string>();
    
    [Tooltip("스테이지 클리어 컷신 시청 목록 (예: CH01_ST01_CLEAR)")]
    public List<string> seenStageClear = new List<string>();
    
    [Header("🎬 예약된 컷신 (로비 복귀 후 재생)")]
    [Tooltip("로비 진입 후 재생할 컷신 ID")]
    public string pendingCutsceneId = "";
    
    [Tooltip("예약된 컷신의 챕터 번호")]
    public int pendingChapterId = 0;
    
    [Header("🎯 마지막 플레이 위치 (Phase 6)")]
    [Tooltip("마지막으로 플레이한 챕터 (UI 표시용)")]
    public int lastPlayedChapterId = 1;
    
    [Tooltip("마지막으로 플레이한 스테이지 ID (예: CH03_ST05)")]
    public string lastPlayedStageId = "";
    
    [Header("🎒 인벤토리 & 장비 (Legacy)")]
    public List<string> inventoryItemNames = new List<string>();
    // Dictionary<string, string> equippedItemNames = new Dictionary<string, string>(); // 기존 삭제
    [SerializeField] private List<string> equippedSlotKeys = new List<string>();
    [SerializeField] private List<string> equippedSlotValues = new List<string>();
    public int maxInventorySize = 48;
    
    [Header("🎒 V2 인벤토리 & 장비 (병행)")]
    [Tooltip("인게임 가방 - 장비 (48칸, 캐릭터 전용)")]
    public List<ItemInstanceID> characterBagInstanceIds = new List<ItemInstanceID>();
    
    [Tooltip("인게임 가방 - 재료 (임시 저장, 스테이지 클리어 시 자동 전송)")]
    public List<MaterialStack> characterBagMaterials = new List<MaterialStack>();
    
    [Tooltip("장착 아이템 (슬롯 포함)")]
    public List<EquippedRecord> equippedRecords = new List<EquippedRecord>();
    
    // Dictionary로 변환하여 사용
    private Dictionary<string, string> _equippedItemNames = null;
    public Dictionary<string, string> equippedItemNames
    {
        get
        {
            if (_equippedItemNames == null)
            {
                _equippedItemNames = new Dictionary<string, string>();
                for (int i = 0; i < Mathf.Min(equippedSlotKeys.Count, equippedSlotValues.Count); i++)
                {
                    _equippedItemNames[equippedSlotKeys[i]] = equippedSlotValues[i];
                }
            }
            return _equippedItemNames;
        }
    }

    /// <summary>
    /// 장비 설정
    /// </summary>
    public void SetEquippedItem(string slotKey, string itemName)
    {
        equippedItemNames[slotKey] = itemName;
        SyncEquippedItems();
    }

    /// <summary>
    /// 장비 제거
    /// </summary>
    public void RemoveEquippedItem(string slotKey)
    {
        if (equippedItemNames.ContainsKey(slotKey))
        {
            equippedItemNames.Remove(slotKey);
            SyncEquippedItems();
        }
    }

    /// <summary>
    /// Dictionary를 SerializeField로 동기화
    /// </summary>
    private void SyncEquippedItems()
    {
        equippedSlotKeys.Clear();
        equippedSlotValues.Clear();
        
        foreach (var kvp in _equippedItemNames ?? new Dictionary<string, string>())
        {
            equippedSlotKeys.Add(kvp.Key);
            equippedSlotValues.Add(kvp.Value);
        }
    }
    
    [Header("🎯 클래스별 세부 스탯")]
    public int classLevel = 1;
    public bool isClassUnlocked = true;
    
    // 클래스별 고유 특성 (BaseClassSaveData 통합)
    [SerializeField] private List<string> extraStatKeys = new List<string>();
    [SerializeField] private List<float> extraStatValues = new List<float>();
    
    // Dictionary로 변환하여 사용
    private Dictionary<string, float> _extraStats = null;
    public Dictionary<string, float> ExtraStats
    {
        get
        {
            if (_extraStats == null)
            {
                _extraStats = new Dictionary<string, float>();
                for (int i = 0; i < Mathf.Min(extraStatKeys.Count, extraStatValues.Count); i++)
                {
                    _extraStats[extraStatKeys[i]] = extraStatValues[i];
                }
            }
            return _extraStats;
        }
    }
    
    /// <summary>
    /// 클래스별 특성 값 설정
    /// </summary>
    public void SetExtraStat(string key, float value)
    {
        ExtraStats[key] = value;
        SyncExtraStats();
    }
    
    /// <summary>
    /// 클래스별 특성 값 가져오기
    /// </summary>
    public float GetExtraStat(string key, float defaultValue = 0f)
    {
        return ExtraStats.ContainsKey(key) ? ExtraStats[key] : defaultValue;
    }
    
    /// <summary>
    /// Dictionary를 SerializeField로 동기화
    /// </summary>
    private void SyncExtraStats()
    {
        extraStatKeys.Clear();
        extraStatValues.Clear();
        
        foreach (var kvp in ExtraStats)
        {
            extraStatKeys.Add(kvp.Key);
            extraStatValues.Add(kvp.Value);
        }
    }
    
    // ========================================
    // 📚 Phase 3.5: 스킬 & SP 시스템 (캐릭터별)
    // ========================================
    
    [Header("📚 스킬 & SP 시스템 (캐릭터별)")]
    [Tooltip("보유 중인 모든 스킬 (액티브 + 패시브)")]
    public List<SkillInstanceSaveData> skills = new List<SkillInstanceSaveData>();
    
    [Tooltip("장착된 액티브 스킬 슬롯 (2개, skillID 저장)")]
    public string[] equippedActiveSkillIds = new string[2];
    
    [Tooltip("장착된 패시브 스킬 슬롯 (3개, skillID 저장)")]
    public string[] equippedPassiveSkillIds = new string[3];
    
    [Tooltip("총 획득 SP (레벨업 시 자동 증가, totalSP = level)")]
    public int totalSP = 0;
    
    [Tooltip("사용한 SP (스킬 레벨업 시 증가)")]
    public int usedSP = 0;
    
    // ========================================
    // 🛡️ Phase 2: 상태이상 저항 시스템
    // ========================================
    
    [Header("🛡️ 상태이상 저항 시스템")]
    [Tooltip("상태이상 저항 스탯 (보스 보상으로 획득)")]
    public List<ResistanceSaveData> resistanceStats = new List<ResistanceSaveData>();
    
    [Tooltip("보스 최초 클리어 여부 확인용 (보스 ID 저장)")]
    public List<string> clearedBossIds = new List<string>();
    
    // ========================================
    // 💎 Phase 9: 룬 시스템 (캐릭터별)
    // ========================================
    
    [Header("💎 룬 시스템 (캐릭터별)")]
    [Tooltip("해금된 룬 인스턴스 (레벨, 한계돌파 상태)")]
    public List<RuneInstanceSaveData> runes = new List<RuneInstanceSaveData>();
    
    [Tooltip("장착된 룬 슬롯 (3개, runeUID 저장)")]
    public string[] equippedRuneUids = new string[3];
    
    /// <summary>
    /// JSON 문자열로 변환
    /// </summary>
    public string ToJson()
    {
        SyncExtraStats();
        return JsonUtility.ToJson(this, true);
    }
    
    /// <summary>
    /// JSON 문자열에서 복원
    /// </summary>
    public static PlayerSlotData FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
            
        var data = JsonUtility.FromJson<PlayerSlotData>(json);
        data._extraStats = null; // Dictionary 재생성 강제
        
        // ⚠️ Phase B 진단: equippedRecords 로드 확인
        Debug.Log($"🔍 [PlayerSlotData.FromJson] equippedRecords 역직렬화 결과: {(data.equippedRecords == null ? "null" : $"{data.equippedRecords.Count}개")}");
        if (data.equippedRecords != null && data.equippedRecords.Count > 0)
        {
            foreach (var record in data.equippedRecords)
            {
                Debug.Log($"  📦 {record.slot} → {record.instanceId.Value}");
            }
        }
        
        return data;
    }
    
    /// <summary>
    /// 기본 슬롯 데이터 생성
    /// </summary>
    public static PlayerSlotData CreateDefaultSlot(int slotIndex, PlayerType playerType = PlayerType.Warrior)
    {
        var slot = new PlayerSlotData
        {
            slotIndex = slotIndex,
            playerName = $"Player{slotIndex + 1}",
            playerType = playerType,
            lastPlayTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            isSlotUsed = true,
            level = 1
        };
        
        // 기본 클래스별 특성 설정
        slot.InitializeDefaultStats(playerType);
        
        // Phase 3.5: 기본 스킬 설정 (1레벨, 2개 액티브 스킬 자동 장착)
        slot.InitializeDefaultSkills(playerType);
        
        return slot;
    }
    
    /// <summary>
    /// 클래스별 기본 특성 초기화
    /// </summary>
    public void InitializeDefaultStats(PlayerType classType)
    {
        ExtraStats.Clear();
        
        switch (classType)
        {
            case PlayerType.Warrior:
                SetExtraStat("blockChance", 0.15f);
                SetExtraStat("counterChance", 0.1f);
                SetExtraStat("berserkerThreshold", 0.3f);
                break;
                
            case PlayerType.Assasin:
                SetExtraStat("criticalChance", 0.15f);
                SetExtraStat("dodgeChance", 0.05f);
                SetExtraStat("stealthDuration", 2.0f);
                SetExtraStat("backAttackBonus", 1.5f);
                break;
                
            case PlayerType.Wizard:
                SetExtraStat("manaCapacity", 100f);
                SetExtraStat("manaRegenRate", 10f);
                SetExtraStat("spellPowerBonus", 1.0f);
                break;
        }
        
        SyncExtraStats();
    }
    
    /// <summary>
    /// 클래스별 기본 스킬 초기화 (Phase 3.5)
    /// 신규 캐릭터 생성 시 호출되어 기본 액티브 스킬 2개를 해금하고 장착
    /// </summary>
    public void InitializeDefaultSkills(PlayerType classType)
    {
        // 스킬 리스트 초기화
        if (skills == null)
            skills = new List<SkillInstanceSaveData>();
        else
            skills.Clear();
        
        equippedActiveSkillIds = new string[2];
        equippedPassiveSkillIds = new string[3];
        
        // SP 초기화 (레벨과 1:1 동기화)
        totalSP = level;
        usedSP = 0;
        
        Debug.Log($"🎯 [PlayerSlotData.InitializeDefaultSkills] SP 초기화: totalSP={totalSP}, level={level}");
        
        // 클래스별 기본 액티브 스킬 ID 목록
        string[] defaultActiveSkillIds = GetDefaultActiveSkillIds(classType);
        
        // 기본 스킬을 Resources에서 로드하여 추가
        foreach (string skillId in defaultActiveSkillIds)
        {
            if (string.IsNullOrEmpty(skillId)) continue;
            
            // Resources/Skills/ 하위에서 스킬 찾기
            BaseSkillData[] allSkills = Resources.LoadAll<BaseSkillData>("Skills");
            BaseSkillData skillData = System.Array.Find(allSkills, s => s != null && s.skillID == skillId);
            
            if (skillData != null)
            {
                // 1레벨 상태로 추가
                var saveData = new SkillInstanceSaveData
                {
                    skillID = skillData.skillID,
                    currentLevel = 1,
                    isEquipped = true
                };
                skills.Add(saveData);
                
                Debug.Log($"✅ [PlayerSlotData] 기본 스킬 추가: {skillData.skillName} (Lv.1)");
            }
            else
            {
                Debug.LogWarning($"⚠️ [PlayerSlotData] 기본 스킬을 찾을 수 없습니다: {skillId}");
            }
        }
        
        // 장착 슬롯에 할당 (최대 2개)
        for (int i = 0; i < Mathf.Min(defaultActiveSkillIds.Length, 2); i++)
        {
            if (!string.IsNullOrEmpty(defaultActiveSkillIds[i]))
            {
                equippedActiveSkillIds[i] = defaultActiveSkillIds[i];
                Debug.Log($"🎯 [PlayerSlotData] 액티브 슬롯 {i}에 장착: {defaultActiveSkillIds[i]}");
            }
        }
        
        Debug.Log($"✅ [PlayerSlotData] {classType} 기본 스킬 초기화 완료 (총 {skills.Count}개, SP: {totalSP})");
    }
    
    /// <summary>
    /// 클래스별 기본 스킬 ID 목록 반환
    /// </summary>
    private string[] GetDefaultActiveSkillIds(PlayerType classType)
    {
        switch (classType)
        {
            case PlayerType.Warrior:
                // TODO: 전사 전용 스킬 추가 시 수정
                return new string[] { "SKILL_MULTISHOT", "SKILL_FOCUSE_STRIKE" }; // 임시: 암살자 스킬 사용
                
            case PlayerType.Assasin:
                // Skill1: 광역기 (WaveClear), Skill2: 단일기 (BossBurst)
                return new string[] { "SKILL_MULTISHOT", "SKILL_FOCUSE_STRIKE" };
                
            case PlayerType.Wizard:
                // TODO: 마법사 전용 스킬 추가 시 수정
                return new string[] { "SKILL_MULTISHOT", "SKILL_FOCUSE_STRIKE" }; // 임시: 암살자 스킬 사용
                
            default:
                return new string[0];
        }
    }
    
    // ========================================
    // ✅ Phase 0: 챕터 진행도 Helper 메서드
    // ========================================
    
    /// <summary>
    /// 챕터 클리어 여부 확인
    /// </summary>
    public bool IsChapterCleared(int chapterId)
    {
        return clearedChapters.Contains(chapterId);
    }
    
    /// <summary>
    /// 챕터 클리어 기록
    /// </summary>
    public void MarkChapterAsCleared(int chapterId)
    {
        if (!clearedChapters.Contains(chapterId))
        {
            clearedChapters.Add(chapterId);
            Debug.Log($"✅ [PlayerSlotData] 챕터 {chapterId} 클리어 기록");
        }
    }
    
    /// <summary>
    /// 컷신 시청 여부 확인
    /// </summary>
    /// <param name="cutsceneId">컷신 ID (예: CH01_START)</param>
    /// <param name="category">카테고리 (CHAPTER_START, CHAPTER_CLEAR, STAGE_ENTER, STAGE_CLEAR)</param>
    public bool HasSeenCutscene(string cutsceneId, string category)
    {
        switch (category)
        {
            case "CHAPTER_START": 
                return seenChapterStart.Contains(cutsceneId);
            case "CHAPTER_CLEAR": 
                return seenChapterClear.Contains(cutsceneId);
            case "STAGE_ENTER": 
                return seenStageEnter.Contains(cutsceneId);
            case "STAGE_CLEAR": 
                return seenStageClear.Contains(cutsceneId);
            default: 
                Debug.LogWarning($"[PlayerSlotData] 알 수 없는 컷신 카테고리: {category}");
                return false;
        }
    }
    
    /// <summary>
    /// 컷신 시청 기록
    /// </summary>
    /// <param name="cutsceneId">컷신 ID (예: CH01_START)</param>
    /// <param name="category">카테고리 (CHAPTER_START, CHAPTER_CLEAR, STAGE_ENTER, STAGE_CLEAR)</param>
    public void MarkCutsceneAsSeen(string cutsceneId, string category)
    {
        switch (category)
        {
            case "CHAPTER_START":
                if (!seenChapterStart.Contains(cutsceneId))
                {
                    seenChapterStart.Add(cutsceneId);
                    Debug.Log($"🎬 [PlayerSlotData] 컷신 시청 기록: {cutsceneId} (챕터 시작)");
                }
                break;
                
            case "CHAPTER_CLEAR":
                if (!seenChapterClear.Contains(cutsceneId))
                {
                    seenChapterClear.Add(cutsceneId);
                    Debug.Log($"🎬 [PlayerSlotData] 컷신 시청 기록: {cutsceneId} (챕터 종료)");
                }
                break;
                
            case "STAGE_ENTER":
                if (!seenStageEnter.Contains(cutsceneId))
                {
                    seenStageEnter.Add(cutsceneId);
                    Debug.Log($"🎬 [PlayerSlotData] 컷신 시청 기록: {cutsceneId} (스테이지 입장)");
                }
                break;
                
            case "STAGE_CLEAR":
                if (!seenStageClear.Contains(cutsceneId))
                {
                    seenStageClear.Add(cutsceneId);
                    Debug.Log($"🎬 [PlayerSlotData] 컷신 시청 기록: {cutsceneId} (스테이지 클리어)");
                }
                break;
                
            default:
                Debug.LogWarning($"[PlayerSlotData] 알 수 없는 컷신 카테고리: {category}");
                break;
        }
    }
    
    /// <summary>
    /// 디버깅용 문자열 표현
    /// </summary>
    public override string ToString()
    {
        return $"Slot[{slotIndex}] {playerName}({playerType}) Lv.{level} Gold:{gold} Used:{isSlotUsed} Stats:{ExtraStats.Count}";
    }
}

/// <summary>
/// 상태이상 저항 저장 데이터 (JsonUtility 호환)
/// </summary>
[System.Serializable]
public struct ResistanceSaveData
{
    public EStatusEffectType type;
    public float value;
    
    public ResistanceSaveData(EStatusEffectType type, float value)
    {
        this.type = type;
        this.value = value;
    }
}
