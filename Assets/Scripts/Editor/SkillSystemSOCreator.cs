using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 스킬 시스템 ScriptableObject 자동 생성 도구 (Phase 1)
/// </summary>
public class SkillSystemSOCreator : EditorWindow
{
    [MenuItem("Tools/Skill System/Create Sample Skills")]
    public static void CreateSampleSkills()
    {
        // 폴더 생성
        string folderPath = "Assets/Resources/Skills";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Skills");
        }
        
        string passiveFolderPath = folderPath + "/Passive";
        if (!AssetDatabase.IsValidFolder(passiveFolderPath))
        {
            AssetDatabase.CreateFolder(folderPath, "Passive");
        }
        
        string activeFolderPath = folderPath + "/Active";
        if (!AssetDatabase.IsValidFolder(activeFolderPath))
        {
            AssetDatabase.CreateFolder(folderPath, "Active");
        }
        
        // '정령의 공명' 패시브 생성
        CreateSpiritResonancePassive(passiveFolderPath);
        
        // 기본 액티브 스킬 생성 (테스트용)
        CreateBasicActiveSkill(activeFolderPath);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    
    private static void CreateSpiritResonancePassive(string folderPath)
    {
        string assetPath = $"{folderPath}/PassiveSkill_SpiritResonance.asset";
        
        // 이미 존재하면 스킵
        if (File.Exists(assetPath))
        {
            return;
        }
        
        PassiveSkillData passive = ScriptableObject.CreateInstance<PassiveSkillData>();
        
        // 기본 정보
        passive.skillID = "passive_spirit_resonance";
        passive.skillName = "정령의 공명";
        passive.description = "구출된 정령들의 힘이 활시위에 깃들어, 화살의 절대적인 파괴력을 증폭시킵니다.";
        passive.unlockLevel = 5;
        passive.maxLevel = 10;
        
        // 패시브 타입
        passive.passiveType = PassiveSkillType.Combat;
        
        // 스탯 보정 설정 (ATK_PERCENT +5% per level)
        passive.statModifiers = new System.Collections.Generic.List<PassiveStatModifier>
        {
            new PassiveStatModifier
            {
                statType = EStatType.ATK_PERCENT,
                baseValue = 5f, // 레벨당 5%
                modifierType = StatModifierType.Multiplicative
            }
        };
        
        AssetDatabase.CreateAsset(passive, assetPath);
    }
    
    private static void CreateBasicActiveSkill(string folderPath)
    {
        string assetPath = $"{folderPath}/ActiveSkill_MultiShot.asset";
        
        // 이미 존재하면 스킵
        if (File.Exists(assetPath))
        {
            return;
        }
        
        ActiveSkillData active = ScriptableObject.CreateInstance<ActiveSkillData>();
        
        // 기본 정보
        active.skillID = "active_multi_shot";
        active.skillName = "갈래 화살";
        active.description = "부채꼴 발사로 3갈래의 하얀 바람 궤적을 그리며 날아가는 기본기.";
        active.unlockLevel = 1;
        active.maxLevel = 10;
        
        // 액티브 타입
        active.skillType = ActiveSkillType.WaveClear;
        
        // 전투 수치
        active.baseCooldown = 3f;
        active.baseDamageMultiplier = 150f; // 150% 데미지
        active.range = 8f;
        
        // AOE 설정
        active.aoeShape = SkillAOEShape.Fan;
        active.aoeFanAngle = 60f;
        active.aoeRadius = 5f;
        active.aoeDuration = 0.3f;
        
        // 특수 속성
        active.projectileCount = 3;
        
        AssetDatabase.CreateAsset(active, assetPath);
    }
}
