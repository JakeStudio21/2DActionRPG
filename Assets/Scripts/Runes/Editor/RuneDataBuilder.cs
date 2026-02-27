using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 테스트용 RuneData ScriptableObject 자동 생성기
/// ⚙️ Phase 4-D: 엔드 콘텐츠 확장 - 레벨업/한계돌파 시스템
/// </summary>
public class RuneDataBuilder : Editor
{
    private const string RuneFolder = "Assets/Resources/Runes";
    
    [MenuItem("Tools/Rune System/Generate Test Runes")]
    public static void GenerateTestRunes()
    {
        Debug.Log("========================================");
        Debug.Log("🔧 테스트용 룬 자동 생성 시작 (Phase 4-D)");
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
        Debug.Log("");
        Debug.Log("📊 룬 시스템 핵심 룰:");
        Debug.Log("  - 장착 슬롯: 최대 3개");
        Debug.Log("  - 중복 장착: 불가 (같은 RuneId)");
        Debug.Log("  - 부옵션 개방: 3, 6, 9레벨 달성 시 랜덤 1개씩 (총 3개)");
        Debug.Log("  - 한계돌파: 중복 룬으로 최대 레벨 상한 확장 (10→15)");
        Debug.Log("");
        Debug.Log("💡 Tip: Inspector에서 각 룬의 'Preview Effects'를 실행하여 상세 정보를 확인하세요.");
        Debug.Log("========================================");
    }
    
    #region 룬 생성 메서드
    
    /// <summary>
    /// 룬 1: 보스 사냥꾼 (공격형)
    /// </summary>
    private static void CreateBossHunterRune()
    {
        string path = $"{RuneFolder}/RUNE_BOSS_HUNTER.asset";
        
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
        rune.runeType = RuneType.Attack1; // 공격형 1슬롯
        rune.description = "보스 몬스터에게 강력한 추가 피해를 가합니다.\n\n[주옵션 - 획득 시 즉시 적용]\n- 보스에게 +20% 피해 (레벨당 5% 증가)\n- 보스 HP 50% 이상일 때 +25% 추가 피해 (레벨당 5% 증가)\n\n[부옵션 - 3, 6, 9레벨에서 랜덤 개방]\n- 보스 방어 무시 +20%\n- 저체력(30% 이하) 피해 감소 +25%\n- 보스 장판 피해 감소 +50%";
        rune.obtainMethod = "보스 드랍";
        rune.requiredLevel = 10;
        
        // 성장 설정
        rune.baseMaxLevel = 10;
        rune.maxLimitBreak = 5;
        rune.mainStatLevelGrowth = 0.05f; // 레벨당 5% 증가
        
        // 주옵션 (1개만 존재)
        string mainModId = "BOSS_DMG_UP";
        
        // 부옵션 후보군 (가중치 포함, 3/6/9레벨에서 랜덤 개방)
        var subStatPool = new List<SubStatDropInfo>
        {
            new SubStatDropInfo("BOSS_IGNORE_DEF", 30),       // 보스 방어 무시 20% (30% 확률)
            new SubStatDropInfo("LOW_HP_DR", 50),             // 체력 30% 이하 피해 감소 25% (50% 확률, 높음)
            new SubStatDropInfo("BOSS_AREA_DMG_REDUCE", 20)   // 보스 장판 피해 감소 50% (20% 확률, 낮음)
        };
        
        // Reflection으로 private 필드 설정
        SetPrivateField(rune, "mainStatModifierId", mainModId);
        SetPrivateField(rune, "subStatPool", subStatPool);
        
        // 에셋 생성
        AssetDatabase.CreateAsset(rune, path);
        Debug.Log($"✅ [{rune.runeName}] ({rune.runeType})");
        Debug.Log($"   📌 주옵션 1개: {mainModId} (획득 시 즉시 적용, 매 레벨업마다 수치 증가)");
        Debug.Log($"   🎲 부옵션 후보 {subStatPool.Count}개 (3, 6, 9레벨 도달 시 가중치 기반 랜덤 개방)");
    }
    
    /// <summary>
    /// 룬 2: 보스 철벽 (생존형)
    /// </summary>
    private static void CreateBossDefenderRune()
    {
        string path = $"{RuneFolder}/RUNE_BOSS_DEFENDER.asset";
        
        // 기존 파일 있으면 삭제
        if (File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }
        
        // RuneData 생성
        RuneData rune = ScriptableObject.CreateInstance<RuneData>();
        
        // 기본 정보
        rune.runeId = "RUNE_BOSS_DEFENDER";
        rune.runeName = "보스 철벽";
        rune.runeType = RuneType.Survival1; // 생존형 1슬롯
        rune.description = "보스 몬스터로부터 받는 피해를 크게 감소시킵니다.\n\n[주옵션 - 획득 시 즉시 적용]\n- 보스로부터 받는 피해 -30% (레벨당 5% 증가)\n\n[부옵션 - 3, 6, 9레벨에서 랜덤 개방]\n- 보스 장판 피해 감소 +50%\n- 저체력(30% 이하) 피해 감소 +25%\n- 엘리트 몬스터 피해 감소 +20%";
        rune.obtainMethod = "보스 드랍";
        rune.requiredLevel = 15;
        
        // 성장 설정
        rune.baseMaxLevel = 10;
        rune.maxLimitBreak = 5;
        rune.mainStatLevelGrowth = 0.05f; // 레벨당 5% 증가
        
        // 주옵션 (1개만 존재)
        string mainModId = "BOSS_ATK_DMG_REDUCE";
        
        // 부옵션 후보군 (가중치 포함, 3/6/9레벨에서 랜덤 개방)
        var subStatPool = new List<SubStatDropInfo>
        {
            new SubStatDropInfo("BOSS_AREA_DMG_REDUCE", 40), // 보스 장판 피해 감소 50% (40% 확률)
            new SubStatDropInfo("LOW_HP_DR", 40),            // 저체력 피해 감소 25% (40% 확률)
            new SubStatDropInfo("ELITE_DMG_REDUCE", 20)      // 엘리트 피해 감소 20% (20% 확률, 낮음)
        };
        
        // Reflection으로 private 필드 설정
        SetPrivateField(rune, "mainStatModifierId", mainModId);
        SetPrivateField(rune, "subStatPool", subStatPool);
        
        // 에셋 생성
        AssetDatabase.CreateAsset(rune, path);
        Debug.Log($"✅ [{rune.runeName}] ({rune.runeType})");
        Debug.Log($"   📌 주옵션 1개: {mainModId} (획득 시 즉시 적용, 매 레벨업마다 수치 증가)");
        Debug.Log($"   🎲 부옵션 후보 {subStatPool.Count}개 (3, 6, 9레벨 도달 시 가중치 기반 랜덤 개방)");
    }
    
    /// <summary>
    /// 룬 3: 흡혈 룬 (유틸형)
    /// </summary>
    private static void CreateLifeStealRune()
    {
        string path = $"{RuneFolder}/RUNE_LIFESTEAL.asset";
        
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
        rune.runeType = RuneType.Utility1; // 유틸형 1슬롯
        rune.description = "보스에게 가한 피해의 일부를 흡수하여 체력을 회복합니다.\n\n[주옵션 - 획득 시 즉시 적용]\n- 보스 피해의 2%만큼 초당 회복 (레벨당 5% 증가)\n\n[부옵션 - 3, 6, 9레벨에서 랜덤 개방]\n- 저체력(30% 이하) 피해 감소 +25%\n- 보스 장판 피해 감소 +50%\n- 보스 방어 무시 +20%";
        rune.obtainMethod = "전설 보스 드랍";
        rune.requiredLevel = 15;
        
        // 성장 설정
        rune.baseMaxLevel = 10;
        rune.maxLimitBreak = 5;
        rune.mainStatLevelGrowth = 0.05f; // 레벨당 5% 증가
        
        // 주옵션 (1개만 존재)
        string mainModId = "BOSS_DOT_LIFESTEAL";
        
        // 부옵션 후보군 (가중치 포함, 3/6/9레벨에서 랜덤 개방)
        var subStatPool = new List<SubStatDropInfo>
        {
            new SubStatDropInfo("LOW_HP_DR", 50),             // 체력 30% 이하 피해 감소 25% (50% 확률, 높음)
            new SubStatDropInfo("BOSS_AREA_DMG_REDUCE", 30),  // 보스 장판 피해 감소 50% (30% 확률)
            new SubStatDropInfo("BOSS_IGNORE_DEF", 20)        // 보스 방어 무시 20% (20% 확률, 낮음)
        };
        
        // Reflection으로 private 필드 설정
        SetPrivateField(rune, "mainStatModifierId", mainModId);
        SetPrivateField(rune, "subStatPool", subStatPool);
        
        // 에셋 생성
        AssetDatabase.CreateAsset(rune, path);
        Debug.Log($"✅ [{rune.runeName}] ({rune.runeType})");
        Debug.Log($"   📌 주옵션 1개: {mainModId} (획득 시 즉시 적용, 매 레벨업마다 수치 증가)");
        Debug.Log($"   🎲 부옵션 후보 {subStatPool.Count}개 (3, 6, 9레벨 도달 시 가중치 기반 랜덤 개방)");
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
    
    /// <summary>
    /// Reflection으로 private 필드 설정 (헬퍼 메서드)
    /// </summary>
    private static void SetPrivateField(RuneData rune, string fieldName, object value)
    {
        var field = typeof(RuneData).GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (field != null)
        {
            field.SetValue(rune, value);
        }
        else
        {
            Debug.LogWarning($"[RuneDataBuilder] 필드를 찾을 수 없습니다: {fieldName}");
        }
    }
    
    #endregion
}

