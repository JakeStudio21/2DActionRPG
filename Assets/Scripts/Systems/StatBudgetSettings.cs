using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 📊 스탯 예산제 밸런스 데이터 관리자
/// CSV 파일들을 파싱하여 Dictionary로 저장하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "StatBudgetSettings", menuName = "Balance/Stat Budget Settings")]
public class StatBudgetSettings : ScriptableObject
{
    #region CSV 할당

    [Header("📂 CSV 파일 할당")]
    [Tooltip("StatUnitCost.csv - 스탯별 예산 단가")]
    public TextAsset statUnitCostCSV;
    
    [Tooltip("GradeSlotBudgetWeighted.csv - 등급별 예산")]
    public TextAsset gradeSlotBudgetCSV;
    
    [Tooltip("EquipmentSlotBudget.csv - 부위별 예산 가중치")]
    public TextAsset equipmentSlotBudgetCSV;
    
    [Tooltip("StatSourceMapping.csv - 부위별 허용 스탯")]
    public TextAsset statSourceMappingCSV;

    #endregion

    #region 파싱된 데이터 (Dictionary)

    [Header("📊 파싱된 데이터")]
    [Tooltip("등급별 총 예산 (Key: ItemGrade, Value: 총 예산)")]
    [SerializeField] private SerializableDictionary<ItemGrade, float> gradeTotalBudgets = new SerializableDictionary<ItemGrade, float>();
    
    [Tooltip("부위별 예산 가중치 (Key: EquipmentSlot, Value: 가중치)")]
    [SerializeField] private SerializableDictionary<EquipmentSlot, float> slotBudgetWeights = new SerializableDictionary<EquipmentSlot, float>();
    
    [Tooltip("스탯 단가 (Key: StatId, Value: 1단위당 비용)")]
    [SerializeField] private SerializableDictionary<string, float> statUnitCosts = new SerializableDictionary<string, float>();
    
    [Tooltip("장비 타입별 허용 스탯 (Key: Bow/Sword/Staff/Armor/Accessory, Value: StatId 리스트)")]
    [SerializeField] private SerializableDictionary<string, List<string>> allowedStatsPerType = new SerializableDictionary<string, List<string>>();

    #endregion

    #region Public Accessors

    public Dictionary<ItemGrade, float> GradeTotalBudgets => gradeTotalBudgets.ToDictionary();
    public Dictionary<EquipmentSlot, float> SlotBudgetWeights => slotBudgetWeights.ToDictionary();
    public Dictionary<string, float> StatUnitCosts => statUnitCosts.ToDictionary();
    public Dictionary<string, List<string>> AllowedStatsPerType => allowedStatsPerType.ToDictionary();

    #endregion

    #region CSV 파싱

    /// <summary>
    /// 📥 모든 CSV 파일을 파싱하여 딕셔너리에 저장
    /// Unity Editor 전용 (Inspector 버튼으로 호출)
    /// </summary>
    public void ParseAllCSVData()
    {
        
        ParseStatUnitCost();
        ParseGradeSlotBudget();
        ParseEquipmentSlotBudget();
        ParseStatSourceMapping();
        
    }

    /// <summary>
    /// StatUnitCost.csv 파싱
    /// </summary>
    private void ParseStatUnitCost()
    {
        if (statUnitCostCSV == null)
        {
            Debug.LogWarning("⚠️ [StatBudgetSettings] StatUnitCost.csv가 할당되지 않았습니다.");
            return;
        }

        statUnitCosts.Clear();
        string[] lines = statUnitCostCSV.text.Split('\n');

        for (int i = 1; i < lines.Length; i++) // 첫 줄(Header) 스킵
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] columns = line.Split(',');
            if (columns.Length < 3) continue;

            string statId = columns[0].Trim();
            if (float.TryParse(columns[2].Trim(), out float unitCost))
            {
                statUnitCosts[statId] = unitCost;
            }
        }

    }

    /// <summary>
    /// GradeSlotBudgetWeighted.csv 파싱
    /// </summary>
    private void ParseGradeSlotBudget()
    {
        if (gradeSlotBudgetCSV == null)
        {
            Debug.LogWarning("⚠️ [StatBudgetSettings] GradeSlotBudget.csv가 할당되지 않았습니다.");
            return;
        }

        gradeTotalBudgets.Clear();
        string[] lines = gradeSlotBudgetCSV.text.Split('\n');

        for (int i = 1; i < lines.Length; i++) // 첫 줄(Header) 스킵
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] columns = line.Split(',');
            if (columns.Length < 11) continue;

            string gradeStr = columns[0].Trim();
            if (System.Enum.TryParse(gradeStr, out ItemGrade grade))
            {
                // totalBudget (마지막 컬럼)
                if (float.TryParse(columns[10].Trim(), out float totalBudget))
                {
                    gradeTotalBudgets[grade] = totalBudget;
                }
            }
        }

    }

    /// <summary>
    /// EquipmentSlotBudget.csv 파싱
    /// </summary>
    private void ParseEquipmentSlotBudget()
    {
        if (equipmentSlotBudgetCSV == null)
        {
            Debug.LogWarning("⚠️ [StatBudgetSettings] EquipmentSlotBudget.csv가 할당되지 않았습니다.");
            return;
        }

        slotBudgetWeights.Clear();
        string[] lines = equipmentSlotBudgetCSV.text.Split('\n');

        for (int i = 1; i < lines.Length; i++) // 첫 줄(Header) 스킵
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] columns = line.Split(',');
            if (columns.Length < 3) continue;

            string slotName = columns[1].Trim(); // "Weapon", "Helmet" 등
            if (float.TryParse(columns[2].Trim(), out float weight))
            {
                // CSV 슬롯명 → EquipmentSlot enum 매핑
                EquipmentSlot slot = MapSlotNameToEnum(slotName);
                if (slot != EquipmentSlot.MainWeapon || slotName == "Weapon")
                {
                    slotBudgetWeights[slot] = weight;
                }
            }
        }

    }

    /// <summary>
    /// StatSourceMapping.csv 파싱
    /// Bow, Sword, Staff를 개별적으로 파싱
    /// </summary>
    private void ParseStatSourceMapping()
    {
        if (statSourceMappingCSV == null)
        {
            Debug.LogWarning("⚠️ [StatBudgetSettings] StatSourceMapping.csv가 할당되지 않았습니다.");
            return;
        }

        allowedStatsPerType.Clear();
        allowedStatsPerType["Bow"] = new List<string>();
        allowedStatsPerType["Sword"] = new List<string>();
        allowedStatsPerType["Staff"] = new List<string>();
        allowedStatsPerType["Armor"] = new List<string>();
        allowedStatsPerType["Accessory"] = new List<string>();

        string[] lines = statSourceMappingCSV.text.Split('\n');

        for (int i = 1; i < lines.Length; i++) // 첫 줄(Header) 스킵
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] columns = line.Split(',');
            if (columns.Length < 7) continue; // Bow, Sword, Staff, Armor, Accessory 최소 7개

            string statId = columns[0].Trim();
            string bowFlag = columns[2].Trim();        // Bow 열
            string swordFlag = columns[3].Trim();      // Sword 열
            string staffFlag = columns[4].Trim();      // Staff 열
            string armorFlag = columns[5].Trim();      // Armor 열
            string accessoryFlag = columns[6].Trim();  // Accessory 열

            if (bowFlag == "1")
                allowedStatsPerType["Bow"].Add(statId);

            if (swordFlag == "1")
                allowedStatsPerType["Sword"].Add(statId);

            if (staffFlag == "1")
                allowedStatsPerType["Staff"].Add(statId);

            if (armorFlag == "1")
                allowedStatsPerType["Armor"].Add(statId);

            if (accessoryFlag == "1")
                allowedStatsPerType["Accessory"].Add(statId);
        }

    }

    /// <summary>
    /// CSV 슬롯명 → EquipmentSlot enum 매핑
    /// </summary>
    private EquipmentSlot MapSlotNameToEnum(string slotName)
    {
        switch (slotName)
        {
            case "Weapon": return EquipmentSlot.MainWeapon;
            case "Helmet": return EquipmentSlot.Helmet;
            case "Armor": return EquipmentSlot.Armor;
            case "Gloves": return EquipmentSlot.Gloves;
            case "Boots": return EquipmentSlot.Boots;
            case "Belt": return EquipmentSlot.Belt;
            case "Necklace": return EquipmentSlot.Necklace;
            case "Ring_1": return EquipmentSlot.Ring1;
            case "Ring_2": return EquipmentSlot.Ring2;
            default:
                Debug.LogWarning($"⚠️ [StatBudgetSettings] 알 수 없는 슬롯명: {slotName}");
                return EquipmentSlot.MainWeapon;
        }
    }

    /// <summary>
    /// 파싱된 데이터 로그 출력
    /// </summary>
    private void LogParsedData()
    {
    }

    #endregion
}

/// <summary>
/// Serializable Dictionary (Inspector에서 표시 가능)
/// </summary>
[System.Serializable]
public class SerializableDictionary<TKey, TValue>
{
    [SerializeField] private List<TKey> keys = new List<TKey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    public TValue this[TKey key]
    {
        get
        {
            int index = keys.IndexOf(key);
            if (index >= 0 && index < values.Count)
                return values[index];
            return default(TValue);
        }
        set
        {
            int index = keys.IndexOf(key);
            if (index >= 0)
            {
                values[index] = value;
            }
            else
            {
                keys.Add(key);
                values.Add(value);
            }
        }
    }

    public void Clear()
    {
        keys.Clear();
        values.Clear();
    }

    public int Count => keys.Count;

    public Dictionary<TKey, TValue> ToDictionary()
    {
        var dict = new Dictionary<TKey, TValue>();
        for (int i = 0; i < keys.Count && i < values.Count; i++)
        {
            dict[keys[i]] = values[i];
        }
        return dict;
    }
}

