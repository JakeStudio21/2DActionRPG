using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 레벨별 성장 데이터 로더 (Phase 2)
/// CSV에서 스킬 레벨별 수치를 로드하여 캐싱
/// </summary>
public class SkillLevelDataLoader : MonoBehaviour
{
    private static SkillLevelDataLoader instance;
    public static SkillLevelDataLoader Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("SkillLevelDataLoader");
                instance = go.AddComponent<SkillLevelDataLoader>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    
    [Header("📂 CSV 경로")]
    [Tooltip("스킬 레벨 데이터 CSV 파일명 (Resources/Skills/CSV/ 폴더 내)")]
    public string csvFileName = "SkillLevelData";
    
    [Header("🔧 디버그")]
    public bool showDebugLogs = true;
    
    // 캐시: SkillID → (Level → SkillLevelInfo)
    private Dictionary<string, Dictionary<int, SkillLevelInfo>> skillLevelCache;
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        LoadSkillLevelData();
    }
    
    /// <summary>
    /// CSV 파싱 및 캐싱
    /// </summary>
    private void LoadSkillLevelData()
    {
        skillLevelCache = new Dictionary<string, Dictionary<int, SkillLevelInfo>>();
        
        // Resources 폴더에서 CSV 로드
        TextAsset csvFile = Resources.Load<TextAsset>($"Skills/CSV/{csvFileName}");
        
        if (csvFile == null)
        {
            Debug.LogError($"❌ [SkillLevelDataLoader] CSV 파일을 찾을 수 없습니다: Resources/Skills/CSV/{csvFileName}.csv");
            return;
        }
        
        string[] lines = csvFile.text.Split('\n');
        
        // 헤더 스킵
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            
            string[] values = line.Split(',');
            if (values.Length < 5) continue;
            
            try
            {
                SkillLevelInfo info = new SkillLevelInfo
                {
                    skillID = values[0].Trim(),
                    level = int.Parse(values[1].Trim()),
                    requireSP = int.Parse(values[2].Trim()),
                    value1 = float.Parse(values[3].Trim()),
                    value2 = float.Parse(values[4].Trim()),
                    description = values.Length > 5 ? values[5].Trim() : ""
                };
                
                // 캐시에 추가
                if (!skillLevelCache.ContainsKey(info.skillID))
                {
                    skillLevelCache[info.skillID] = new Dictionary<int, SkillLevelInfo>();
                }
                
                skillLevelCache[info.skillID][info.level] = info;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ [SkillLevelDataLoader] CSV 파싱 실패 (라인 {i}): {e.Message}");
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"✅ [SkillLevelDataLoader] 스킬 레벨 데이터 로드 완료: {skillLevelCache.Count}개 스킬");
    }
    
    /// <summary>
    /// 특정 스킬의 특정 레벨 정보 가져오기
    /// </summary>
    public SkillLevelInfo GetSkillLevelInfo(string skillID, int level)
    {
        if (skillLevelCache == null || !skillLevelCache.ContainsKey(skillID))
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [SkillLevelDataLoader] 스킬 '{skillID}'의 데이터를 찾을 수 없습니다.");
            return default;
        }
        
        if (!skillLevelCache[skillID].ContainsKey(level))
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [SkillLevelDataLoader] 스킬 '{skillID}' 레벨 {level} 데이터를 찾을 수 없습니다.");
            return default;
        }
        
        return skillLevelCache[skillID][level];
    }
    
    /// <summary>
    /// 스킬의 최대 레벨 가져오기
    /// </summary>
    public int GetMaxLevel(string skillID)
    {
        if (skillLevelCache == null || !skillLevelCache.ContainsKey(skillID))
            return 0;
        
        int maxLevel = 0;
        foreach (int level in skillLevelCache[skillID].Keys)
        {
            if (level > maxLevel)
                maxLevel = level;
        }
        return maxLevel;
    }
    
    /// <summary>
    /// 특정 스킬이 CSV에 존재하는지 확인
    /// </summary>
    public bool HasSkillData(string skillID)
    {
        return skillLevelCache != null && skillLevelCache.ContainsKey(skillID);
    }
}

/// <summary>
/// 스킬 레벨 정보 구조체
/// </summary>
[System.Serializable]
public struct SkillLevelInfo
{
    public string skillID;      // 스킬 고유 ID
    public int level;           // 레벨
    public int requireSP;       // 이 레벨에 도달하기 위해 필요한 SP
    public float value1;        // 주 수치 (액티브: 데미지%, 패시브: 스탯 보너스)
    public float value2;        // 부 수치 (액티브: 쿨다운, 패시브: 미사용)
    public string description;  // 설명
    
    public override string ToString()
    {
        return $"[{skillID} Lv.{level}] SP:{requireSP}, Value1:{value1}, Value2:{value2}";
    }
}
