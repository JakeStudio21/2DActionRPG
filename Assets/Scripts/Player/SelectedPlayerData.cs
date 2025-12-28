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
    
    [Header("🎯 스테이지 진행도")]
    public List<StageSystem.StageProgress> stageProgresses = new List<StageSystem.StageProgress>();
    
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
        
        if (slotData.equippedItemNames != null)
        {
            foreach (var kvp in slotData.equippedItemNames)
            {
                if (System.Enum.TryParse<EquipmentSlot>(kvp.Key, out EquipmentSlot slot))
                {
                    var item = Resources.Load<EquipmentData>($"Equipment/{kvp.Value}");
                    if (item != null) 
                    {
                        RuntimeEquippedItems[slot] = item;
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ [SelectedPlayerData] 장비 아이템을 찾을 수 없음: {kvp.Value}");
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
        
        // 스테이지 진행도 로드
        stageProgresses = slotData.stageProgresses ?? new List<StageSystem.StageProgress>();
        
        // 🎬 Phase 5: 예약된 컷신 로드
        pendingCutsceneId = slotData.pendingCutsceneId;
        pendingChapterId = slotData.pendingChapterId;
        
        SyncDictionaries();
        
        Debug.Log($"📥 [SelectedPlayerData] 슬롯 {slotData.slotIndex} 데이터 로드 완료: {slotData}");
    }
    
    /// <summary>
    /// 런타임 데이터를 PlayerSlotData로 저장
    /// </summary>
    public PlayerSlotData SaveToSlotData()
    {
        var slotData = new PlayerSlotData
        {
            slotIndex = selectedSlotIndex,
            playerName = playerName,
            playerType = selectedPlayerType,
            lastPlayTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
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
        
        // 스테이지 진행도 저장
        slotData.stageProgresses = new List<StageSystem.StageProgress>(stageProgresses);
        
        // 🎬 Phase 5: 예약된 컷신 저장
        slotData.pendingCutsceneId = pendingCutsceneId;
        slotData.pendingChapterId = pendingChapterId;
        
        Debug.Log($"📤 [SelectedPlayerData] 슬롯 {selectedSlotIndex} 데이터 저장 준비 완료: {slotData}");
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
