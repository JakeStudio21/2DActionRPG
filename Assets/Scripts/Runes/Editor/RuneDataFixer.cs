using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 룬 데이터 일괄 수정 도구
/// ⚙️ ScriptableObject 에셋의 주옵션/부옵션을 코드로 설정
/// 
/// 메뉴 위치: Tools/Rune System/Fix Rune Data/...
/// </summary>
public class RuneDataFixer : Editor
{
    private const string MENU_ROOT = "Tools/Rune System/Fix Rune Data/";
    
    [MenuItem(MENU_ROOT + "1. 모든 룬 데이터 검증 및 수정")]
    public static void FixAllRuneData()
    {
        Debug.Log("========== 룬 데이터 일괄 수정 시작 ==========");
        
        // 1. 모든 RuneData ScriptableObject 찾기
        string[] guids = AssetDatabase.FindAssets("t:RuneData");
        
        if (guids.Length == 0)
        {
            Debug.LogWarning("⚠️ RuneData ScriptableObject를 찾을 수 없습니다.");
            Debug.LogWarning("💡 Tip: Assets/Data/Runes/ 폴더에 룬 데이터 에셋이 있는지 확인하세요.");
            return;
        }
        
        Debug.Log($"총 {guids.Length}개 RuneData 발견\n");
        
        int fixedCount = 0;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            RuneData runeData = AssetDatabase.LoadAssetAtPath<RuneData>(path);
            
            if (runeData == null) continue;
            
            Debug.Log($"[{runeData.runeName}] 검증 중...");
            
            // 주옵션 검증 및 수정
            bool modified = false;
            
            if (string.IsNullOrEmpty(runeData.MainStatModifierId))
            {
                Debug.LogWarning($"  ⚠️ 주옵션이 비어있음! 자동 설정 중...");
                
                // runeId 기반으로 적절한 주옵션 할당
                string suggestedMainStat = GetSuggestedMainStat(runeData.runeId, runeData.runeName);
                
                if (!string.IsNullOrEmpty(suggestedMainStat))
                {
                    // Reflection으로 private 필드 수정
                    var field = typeof(RuneData).GetField("mainStatModifierId", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (field != null)
                    {
                        field.SetValue(runeData, suggestedMainStat);
                        modified = true;
                        Debug.Log($"    ✅ 주옵션 설정: {suggestedMainStat}");
                    }
                }
                else
                {
                    Debug.LogError($"    ❌ 적절한 주옵션을 찾을 수 없습니다. 수동 설정이 필요합니다.");
                }
            }
            else
            {
                Debug.Log($"    ✅ 주옵션: {runeData.MainStatModifierId}");
            }
            
            // 부옵션 후보 검증
            if (runeData.SubStatPool.Count == 0)
            {
                Debug.LogWarning($"  ⚠️ 부옵션 후보가 비어있음!");
            }
            else
            {
                Debug.Log($"    ✅ 부옵션 후보: {runeData.SubStatPool.Count}개");
            }
            
            // 변경사항 저장
            if (modified)
            {
                EditorUtility.SetDirty(runeData);
                fixedCount++;
            }
            
            Debug.Log("");
        }
        
        if (fixedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"✅ {fixedCount}개 룬 데이터 수정 완료!");
        }
        else
        {
            Debug.Log("✅ 수정이 필요한 룬이 없습니다.");
        }
        
        Debug.Log("==========================================");
    }
    
    [MenuItem(MENU_ROOT + "2. 주옵션 ID 참고표 출력")]
    public static void PrintMainStatReference()
    {
        Debug.Log("========== 주옵션 ID 참고표 (CSV 기준) ==========");
        Debug.Log("이 ID들을 Unity Inspector의 Main Stat Modifier Id 필드에 입력하세요.\n");
        
        Debug.Log("=== 🗡️ 공격형 룬 (4개) ===");
        Debug.Log("  BOSS_IGNORE_DEF          - 보스 방어 무시 % (물리 딜러)");
        Debug.Log("  BOSS_DMG_UP              - 보스 피해 증가 % (범용)");
        Debug.Log("  BOSS_HP_HIGH_BONUS       - 보스 체력 50% 이상 추가 피해 (고체력 특화)");
        Debug.Log("  BOSS_BLOCK_HEAL          - 보스 회복 차단 % (힐러 카운터)");
        Debug.Log("");
        
        Debug.Log("=== 🛡️ 생존형 룬 (4개) ===");
        Debug.Log("  BOSS_DOT_LIFESTEAL       - 보스 피해 HP DOT 회복 초당% (지속 회복)");
        Debug.Log("  LOW_HP_DR                - 체력 30% 이하 피해 감소 (위기 대응)");
        Debug.Log("  BOSS_AREA_DMG_REDUCE     - 보스 장판 피해 감소 % (메카닉 대응)");
        Debug.Log("  BOSS_ATK_DMG_REDUCE      - 보스 공격피해 감소 % (탱커)");
        Debug.Log("");
        
        Debug.Log("💡 사용법:");
        Debug.Log("   1. Project 창에서 룬 데이터 에셋 선택");
        Debug.Log("   2. Inspector에서 'Main Stat Modifier Id' 필드 찾기");
        Debug.Log("   3. 위 ID 중 하나 복사 & 붙여넣기");
        Debug.Log("");
        Debug.Log("⚠️ 주의: 총 8개 ID만 사용 가능합니다 (ConditionalModifier.csv 기준)");
        Debug.Log("🔒 면역 시스템(BIND/SLOW/POISON/BURN)은 보스 저항 시스템 전용으로 이동했습니다.");
        Debug.Log("==========================================");
    }
    
    /// <summary>
    /// runeId/runeName 기반으로 적절한 주옵션 추천
    /// ⚠️ 총 8개 ID만 사용 가능 (면역 시스템은 보스 저항 시스템으로 이동)
    /// </summary>
    private static string GetSuggestedMainStat(string runeId, string runeName)
    {
        // runeId 기반 매칭
        if (!string.IsNullOrEmpty(runeId))
        {
            string id = runeId.ToUpper();
            
            // 공격형 (4개)
            if (id.Contains("BOSS") && id.Contains("HUNTER"))
                return "BOSS_IGNORE_DEF";
            
            if (id.Contains("DAMAGE") || id.Contains("ATTACK"))
                return "BOSS_DMG_UP";
            
            if (id.Contains("LIFESTEAL") || id.Contains("VAMPIRE"))
                return "BOSS_DOT_LIFESTEAL";
            
            if (id.Contains("HEAL") && id.Contains("BLOCK"))
                return "BOSS_BLOCK_HEAL";
            
            // 생존형 (4개)
            if (id.Contains("DEFENDER") || id.Contains("SHIELD"))
                return "BOSS_AREA_DMG_REDUCE";
            
            if (id.Contains("LOWHP") || id.Contains("SURVIVE"))
                return "LOW_HP_DR";
        }
        
        // runeName 기반 매칭
        if (!string.IsNullOrEmpty(runeName))
        {
            string name = runeName.ToLower();
            
            // 공격형 (4개)
            if (name.Contains("보스") && name.Contains("사냥"))
                return "BOSS_IGNORE_DEF";
            
            if (name.Contains("파괴") || name.Contains("공격"))
                return "BOSS_DMG_UP";
            
            if (name.Contains("흡혈") || name.Contains("생명"))
                return "BOSS_DOT_LIFESTEAL";
            
            if (name.Contains("회복") && name.Contains("차단"))
                return "BOSS_BLOCK_HEAL";
            
            if (name.Contains("처형") || name.Contains("힐러"))
                return "BOSS_BLOCK_HEAL";
            
            if (name.Contains("고체력") || name.Contains("체력특화"))
                return "BOSS_HP_HIGH_BONUS";
            
            // 생존형 (4개)
            if (name.Contains("보스") && name.Contains("철벽"))
                return "BOSS_AREA_DMG_REDUCE";
            
            if (name.Contains("장판") || name.Contains("메카닉"))
                return "BOSS_AREA_DMG_REDUCE";
            
            if (name.Contains("저체력") || name.Contains("생존"))
                return "LOW_HP_DR";
            
            if (name.Contains("탱커") || name.Contains("방패"))
                return "BOSS_ATK_DMG_REDUCE";
        }
        
        // 기본값 (CSV에 실제 존재하는 ID, 범용 공격형)
        return "BOSS_DMG_UP";
    }
}
