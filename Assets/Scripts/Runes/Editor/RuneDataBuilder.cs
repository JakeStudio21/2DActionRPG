using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 테스트용 RuneData ScriptableObject 자동 생성기
/// ⚙️ Phase 4-C: Tools → Generate Test Runes 메뉴
/// </summary>
public class RuneDataBuilder : Editor
{
    private const string RuneFolder = "Assets/Resources/Runes";
    
    [MenuItem("Tools/Generate Test Runes")]
    public static void GenerateTestRunes()
    {
        Debug.Log("========================================");
        Debug.Log("🔧 테스트용 룬 자동 생성 시작");
        Debug.Log("========================================\n");
        
        // 폴더 생성 (없으면)
        EnsureFolder(RuneFolder);
        
        // 3개 테스트 룬 생성
        CreateBossHunterRune();
        CreateBossDefenderRune();
        CreateLifeStealRune();
        
        // AssetDatabase 갱신
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("\n========================================");
        Debug.Log("✅ 테스트용 룬 3개 생성 완료!");
        Debug.Log($"📁 위치: {RuneFolder}");
        Debug.Log("========================================");
    }
    
    #region 룬 생성 메서드
    
    /// <summary>
    /// 룬 1: 보스 사냥꾼
    /// </summary>
    private static void CreateBossHunterRune()
    {
        string path = $"{RuneFolder}/RUNE_BOSS_HUNTER.asset"; // 수정: Rune_BossHunter → RUNE_BOSS_HUNTER
        
        // 기존 파일 있으면 삭제
        if (File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }
        
        // RuneData 생성
        RuneData rune = ScriptableObject.CreateInstance<RuneData>();
        
        // 기본 정보
        rune.runeId = "RUNE_BOSS_HUNTER";
        rune.runeName = "보스 사냥꾼";
        rune.rarity = RuneRarity.Epic;
        rune.description = "보스 몬스터에게 강력한 추가 피해를 가합니다.\n- 보스에게 +20% 피해\n- 보스 HP 50% 이상일 때 +25% 추가 피해";
        rune.obtainMethod = "보스 드랍";
        rune.requiredLevel = 10;
        
        // ConditionalModifier ID 추가
        var modifierIds = new List<string>
        {
            "BOSS_DMG_UP",
            "BOSS_HP_HIGH_BONUS"
        };
        
        // Reflection으로 private 필드 설정
        var field = typeof(RuneData).GetField("conditionalModifierIds", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(rune, modifierIds);
        }
        
        // 에셋 생성
        AssetDatabase.CreateAsset(rune, path);
        Debug.Log($"✅ 생성: {rune.runeName} ({modifierIds.Count}개 효과)");
    }
    
    /// <summary>
    /// 룬 2: 보스 방어자
    /// </summary>
    private static void CreateBossDefenderRune()
    {
        string path = $"{RuneFolder}/RUNE_BOSS_DEFENDER.asset"; // 수정: Rune_BossDefender → RUNE_BOSS_DEFENDER
        
        // 기존 파일 있으면 삭제
        if (File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }
        
        // RuneData 생성
        RuneData rune = ScriptableObject.CreateInstance<RuneData>();
        
        // 기본 정보
        rune.runeId = "RUNE_BOSS_DEFENDER";
        rune.runeName = "보스 방어자";
        rune.rarity = RuneRarity.Rare;
        rune.description = "보스 공격에 대한 방어력을 강화합니다.\n- 속박 면역\n- 보스 공격 피해 -25%";
        rune.obtainMethod = "상점 구매";
        rune.requiredLevel = 5;
        
        // ConditionalModifier ID 추가
        var modifierIds = new List<string>
        {
            "IMMUNE_BIND",
            "BOSS_ATK_DMG_REDUCE"
        };
        
        // Reflection으로 private 필드 설정
        var field = typeof(RuneData).GetField("conditionalModifierIds", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(rune, modifierIds);
        }
        
        // 에셋 생성
        AssetDatabase.CreateAsset(rune, path);
        Debug.Log($"✅ 생성: {rune.runeName} ({modifierIds.Count}개 효과)");
    }
    
    /// <summary>
    /// 룬 3: 흡혈 룬
    /// </summary>
    private static void CreateLifeStealRune()
    {
        string path = $"{RuneFolder}/RUNE_LIFESTEAL.asset"; // 수정: Rune_LifeSteal → RUNE_LIFESTEAL
        
        // 기존 파일 있으면 삭제
        if (File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }
        
        // RuneData 생성
        RuneData rune = ScriptableObject.CreateInstance<RuneData>();
        
        // 기본 정보
        rune.runeId = "RUNE_LIFESTEAL";
        rune.runeName = "흡혈 룬";
        rune.rarity = RuneRarity.Legendary;
        rune.description = "보스에게 가한 피해의 일부를 흡수하여 체력을 회복합니다.\n- 보스 피해의 2%만큼 초당 회복";
        rune.obtainMethod = "전설 보스 드랍";
        rune.requiredLevel = 15;
        
        // ConditionalModifier ID 추가
        var modifierIds = new List<string>
        {
            "BOSS_DOT_LIFESTEAL"
        };
        
        // Reflection으로 private 필드 설정
        var field = typeof(RuneData).GetField("conditionalModifierIds", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(rune, modifierIds);
        }
        
        // 에셋 생성
        AssetDatabase.CreateAsset(rune, path);
        Debug.Log($"✅ 생성: {rune.runeName} ({modifierIds.Count}개 효과)");
    }
    
    #endregion
    
    #region 유틸리티
    
    /// <summary>
    /// 폴더 생성 (없으면)
    /// </summary>
    private static void EnsureFolder(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            // 상위 폴더부터 차례로 생성
            string[] folders = folderPath.Split('/');
            string currentPath = folders[0];
            
            for (int i = 1; i < folders.Length; i++)
            {
                string parentPath = currentPath;
                currentPath = $"{currentPath}/{folders[i]}";
                
                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    AssetDatabase.CreateFolder(parentPath, folders[i]);
                    Debug.Log($"📁 폴더 생성: {currentPath}");
                }
            }
        }
    }
    
    #endregion
}

