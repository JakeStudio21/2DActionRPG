using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 스킬 인스턴스 저장 데이터
/// Phase 3-Revision: AccountData에 저장되는 구조
/// </summary>
[System.Serializable]
public class SkillInstanceSaveData
{
    // 스킬 데이터 캐시 (성능 최적화)
    private static Dictionary<string, BaseSkillData> skillDataCache = null;
    [Tooltip("스킬 고유 ID (예: passive_spirit_resonance)")]
    public string skillID;
    
    [Tooltip("현재 레벨 (0 = 미해금)")]
    public int currentLevel;
    
    [Tooltip("장착 여부")]
    public bool isEquipped;
    
    /// <summary>
    /// SkillInstance로부터 저장 데이터 생성
    /// </summary>
    public static SkillInstanceSaveData FromSkillInstance(SkillInstance skill)
    {
        if (skill == null || skill.skillData == null) return null;
        
        return new SkillInstanceSaveData
        {
            skillID = skill.skillData.skillID,
            currentLevel = skill.currentLevel,
            isEquipped = skill.isEquipped
        };
    }
    
    /// <summary>
    /// 저장 데이터로부터 SkillInstance 복원
    /// Phase 3-Revision: 하위 폴더 검색 지원
    /// </summary>
    public SkillInstance ToSkillInstance()
    {
        // Resources/Skills/ 하위 폴더를 모두 검색
        BaseSkillData skillData = FindSkillDataRecursive(skillID);
        
        if (skillData == null)
        {
            Debug.LogError($"❌ [SkillInstanceSaveData] 스킬 데이터를 찾을 수 없습니다: {skillID}");
            Debug.LogError($"   검색 경로: Resources/Skills/**/{skillID}.asset");
            return null;
        }
        
        return new SkillInstance(skillData)
        {
            currentLevel = this.currentLevel,
            isEquipped = this.isEquipped
        };
    }
    
    /// <summary>
    /// Resources/Skills/ 하위 폴더에서 skillID로 스킬 데이터 찾기
    /// Active/, Passive/ 등 하위 폴더를 모두 검색
    /// 성능 최적화: 첫 호출 시 캐싱, 이후 캐시에서 검색
    /// </summary>
    private static BaseSkillData FindSkillDataRecursive(string skillID)
    {
        // 캐시 초기화 (첫 호출 시)
        if (skillDataCache == null)
        {
            skillDataCache = new Dictionary<string, BaseSkillData>();
            
            // Resources.LoadAll()은 하위 폴더를 모두 포함 ⭐
            BaseSkillData[] allSkills = Resources.LoadAll<BaseSkillData>("Skills");
            
            foreach (var skill in allSkills)
            {
                if (skill != null && !string.IsNullOrEmpty(skill.skillID))
                {
                    skillDataCache[skill.skillID] = skill;
                }
            }
            
        }
        
        // 캐시에서 검색
        if (skillDataCache.TryGetValue(skillID, out BaseSkillData skillData))
        {
            return skillData;
        }
        
        return null;
    }
    
    /// <summary>
    /// 캐시 초기화 (에디터에서 스킬 데이터 변경 시 호출)
    /// </summary>
    public static void ClearCache()
    {
        skillDataCache = null;
    }
}
