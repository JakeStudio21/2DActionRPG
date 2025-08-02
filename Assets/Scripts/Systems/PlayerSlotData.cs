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
    public int gold = 0;
    
    [Header("🎒 인벤토리 & 장비")]
    public List<string> inventoryItemNames = new List<string>();
    // Dictionary<string, string> equippedItemNames = new Dictionary<string, string>(); // 기존 삭제
    [SerializeField] private List<string> equippedSlotKeys = new List<string>();
    [SerializeField] private List<string> equippedSlotValues = new List<string>();
    public int maxInventorySize = 16;
    
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
            isSlotUsed = true
        };
        
        // 기본 클래스별 특성 설정
        slot.InitializeDefaultStats(playerType);
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
    /// 디버깅용 문자열 표현
    /// </summary>
    public override string ToString()
    {
        return $"Slot[{slotIndex}] {playerName}({playerType}) Lv.{level} Gold:{gold} Used:{isSlotUsed} Stats:{ExtraStats.Count}";
    }
}
