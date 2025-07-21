using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ⭐ [Phase C] 모든 플레이어 클래스의 공통 저장 데이터 구조
/// 확장 가능한 Dictionary 기반으로 클래스별 특성 저장
/// </summary>
[System.Serializable]
public class BaseClassSaveData
{
    [Header("기본 정보")]
    public PlayerType classType = PlayerType.None;
    public int classLevel = 1;
    public bool isUnlocked = false;
    public bool wasActiveLastTime = false;
    
    [Header("확장 가능한 특성 데이터")]
    [SerializeField] private List<string> propertyKeys = new List<string>();
    [SerializeField] private List<float> propertyValues = new List<float>();
    
    // Dictionary로 변환하여 사용
    private Dictionary<string, float> _properties = null;
    public Dictionary<string, float> Properties
    {
        get
        {
            if (_properties == null)
            {
                _properties = new Dictionary<string, float>();
                for (int i = 0; i < Mathf.Min(propertyKeys.Count, propertyValues.Count); i++)
                {
                    _properties[propertyKeys[i]] = propertyValues[i];
                }
            }
            return _properties;
        }
    }
    
    /// <summary>
    /// 클래스별 특성 값 설정
    /// </summary>
    public void SetProperty(string key, float value)
    {
        Properties[key] = value;
        SyncDictionary();
    }
    
    /// <summary>
    /// 클래스별 특성 값 가져오기
    /// </summary>
    public float GetProperty(string key, float defaultValue = 0f)
    {
        return Properties.ContainsKey(key) ? Properties[key] : defaultValue;
    }
    
    /// <summary>
    /// Dictionary를 SerializeField로 동기화
    /// </summary>
    private void SyncDictionary()
    {
        propertyKeys.Clear();
        propertyValues.Clear();
        
        foreach (var kvp in Properties)
        {
            propertyKeys.Add(kvp.Key);
            propertyValues.Add(kvp.Value);
        }
    }
    
    /// <summary>
    /// JSON 문자열로 변환
    /// </summary>
    public string ToJson()
    {
        SyncDictionary();
        return JsonUtility.ToJson(this, true);
    }
    
    /// <summary>
    /// JSON 문자열에서 복원
    /// </summary>
    public static BaseClassSaveData FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new BaseClassSaveData();
            
        var data = JsonUtility.FromJson<BaseClassSaveData>(json);
        data._properties = null; // Dictionary 재생성 강제
        return data;
    }
    
    /// <summary>
    /// 기본값으로 초기화
    /// </summary>
    public void Reset(PlayerType playerType)
    {
        classType = playerType;
        classLevel = 1;
        isUnlocked = (playerType == PlayerType.None); // 기본 클래스는 언락
        wasActiveLastTime = false;
        
        Properties.Clear();
        
        // 클래스별 기본값 설정
        switch (playerType)
        {
            case PlayerType.Warrior:
                SetProperty("blockChance", 0.15f);
                SetProperty("counterChance", 0.1f);
                SetProperty("berserkerThreshold", 0.3f);
                break;
                
            case PlayerType.Assasin:
                SetProperty("criticalChance", 0.15f);
                SetProperty("dodgeChance", 0.05f);
                SetProperty("stealthDuration", 2.0f);
                break;
                
            case PlayerType.Wizard:
                SetProperty("manaCapacity", 100f);
                SetProperty("manaRegenRate", 10f);
                SetProperty("spellPowerBonus", 1.0f);
                break;
        }
        
        SyncDictionary();
    }
    
    /// <summary>
    /// 디버깅용 문자열 표현
    /// </summary>
    public override string ToString()
    {
        return $"ClassData[{classType}] Lv.{classLevel} Unlocked:{isUnlocked} Active:{wasActiveLastTime} Properties:{Properties.Count}";
    }
} 