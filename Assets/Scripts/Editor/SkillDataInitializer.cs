using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 테스트용 스킬 데이터 초기화 도구
/// Tools 메뉴에서 실행
/// Phase 3.5: PlayerDataManager 기반으로 수정
/// </summary>
public class SkillDataInitializer
{
    [MenuItem("Tools/Skills/📚 테스트용 스킬 데이터 초기화")]
    public static void InitializeTestSkillData()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("📚 테스트용 스킬 데이터 초기화 시작 (Phase 3.5)");
        Debug.Log("⚠️ 스킬은 이제 캐릭터별로 저장됩니다!");
        Debug.Log("═══════════════════════════════════════════════════════");
        
        // PlayerDataManager 초기화
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("❌ PlayerDataManager가 없습니다! 플레이 모드에서 실행하거나 씬에 추가하세요.");
            return;
        }
        
        if (!PlayerDataManager.Instance.IsSlotSelected)
        {
            Debug.LogError("❌ 캐릭터 슬롯이 선택되지 않았습니다! 먼저 로비에서 캐릭터를 선택하세요.");
            return;
        }
        
        var playerDataManager = PlayerDataManager.Instance;
        var slotData = playerDataManager.GetCurrentSlotData();
        
        // 기존 스킬 데이터 확인
        int existingCount = (slotData.skills != null) ? slotData.skills.Count : 0;
        
        if (existingCount > 0)
        {
            bool confirm = EditorUtility.DisplayDialog(
                "스킬 데이터 초기화",
                $"이미 {existingCount}개의 스킬이 있습니다.\n\n" +
                "테스트용 스킬 데이터를 추가하시겠습니까?\n\n" +
                "(기존 데이터는 유지됩니다)",
                "추가",
                "취소"
            );
            
            if (!confirm) return;
        }
        
        // Resources 폴더에서 모든 스킬 데이터 로드
        BaseSkillData[] allSkills = Resources.LoadAll<BaseSkillData>("Skills");
        
        if (allSkills.Length == 0)
        {
            Debug.LogError("❌ Resources/Skills/ 폴더에 스킬 데이터가 없습니다!");
            EditorUtility.DisplayDialog(
                "오류",
                "Resources/Skills/ 폴더에 스킬 ScriptableObject가 없습니다.\n\n" +
                "스킬 데이터를 먼저 생성해주세요.",
                "확인"
            );
            return;
        }
        
        Debug.Log($"📂 발견된 스킬 데이터: {allSkills.Length}개");
        
        int addedCount = 0;
        
        // 모든 스킬을 AccountData에 추가
        foreach (var skillData in allSkills)
        {
            if (skillData == null) continue;
            
            // 이미 있는지 확인
            if (slotData.skills == null)
                slotData.skills = new List<SkillInstanceSaveData>();
            
            var existing = slotData.skills.Find(s => s.skillID == skillData.skillID);
            
            if (existing != null)
            {
                Debug.Log($"   ⚠️ 이미 존재: {skillData.skillName} (스킵)");
                continue;
            }
            
            // 새 스킬 추가
            var newSkillSave = new SkillInstanceSaveData
            {
                skillID = skillData.skillID,
                currentLevel = 1,
                isEquipped = false
            };
            slotData.skills.Add(newSkillSave);
            addedCount++;
            
            Debug.Log($"   ✅ 추가: {skillData.skillName} (ID: {skillData.skillID})");
        }
        
        // 테스트용 SP 추가 (레벨 기반)
        slotData.level = 100; // 테스트용 레벨 설정
        slotData.totalSP = slotData.level; // totalSP = level (1:1 동기화)
        slotData.usedSP = 0;
        
        // 저장
        playerDataManager.SaveCurrentSlot();
        
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"✅ 테스트용 스킬 데이터 초기화 완료!");
        Debug.Log($"   - 캐릭터: {slotData.playerName} (슬롯 {slotData.slotIndex})");
        Debug.Log($"   - 추가된 스킬: {addedCount}개");
        Debug.Log($"   - 총 스킬: {slotData.skills.Count}개");
        Debug.Log($"   - SP: {slotData.usedSP}/{slotData.totalSP}");
        Debug.Log($"   - 레벨: {slotData.level}");
        Debug.Log("═══════════════════════════════════════════════════════");
        
        // 결과 다이얼로그
        EditorUtility.DisplayDialog(
            "초기화 완료!",
            $"✅ 테스트용 스킬 데이터가 추가되었습니다!\n\n" +
            $"캐릭터: {slotData.playerName}\n" +
            $"추가된 스킬: {addedCount}개\n" +
            $"SP: {slotData.usedSP}/{slotData.totalSP}\n" +
            $"레벨: {slotData.level}\n\n" +
            "이제 Play 모드에서 스킬북 패널을 열어보세요.",
            "확인"
        );
    }
    
    [MenuItem("Tools/Skills/🗑️ 스킬 데이터 초기화 (전체 삭제)")]
    public static void ClearAllSkillData()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "스킬 데이터 초기화",
            "⚠️ 현재 캐릭터의 모든 스킬 데이터를 삭제하시겠습니까?\n\n" +
            "이 작업은 되돌릴 수 없습니다.",
            "삭제",
            "취소"
        );
        
        if (!confirm) return;
        
        if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
        {
            Debug.LogError("❌ 캐릭터 슬롯이 선택되지 않았습니다!");
            return;
        }
        
        var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
        if (slotData == null) return;
        
        int beforeCount = (slotData.skills != null) ? slotData.skills.Count : 0;
        
        // 스킬 데이터 삭제
        slotData.skills = new List<SkillInstanceSaveData>();
        slotData.equippedActiveSkillIds = new string[2];
        slotData.equippedPassiveSkillIds = new string[3];
        slotData.usedSP = 0;
        
        // 저장
        PlayerDataManager.Instance.SaveCurrentSlot();
        
        Debug.Log($"🗑️ [{slotData.playerName}] 스킬 데이터 {beforeCount}개 삭제 완료");
        
        EditorUtility.DisplayDialog(
            "삭제 완료",
            $"✅ {beforeCount}개의 스킬이 삭제되었습니다.",
            "확인"
        );
    }
    
    [MenuItem("Tools/Skills/📊 현재 스킬 데이터 확인")]
    public static void ShowCurrentSkillData()
    {
        if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
        {
            EditorUtility.DisplayDialog(
                "에러",
                "❌ 캐릭터 슬롯이 선택되지 않았습니다!\n\n플레이 모드에서 로비에서 캐릭터를 선택한 후 실행하세요.",
                "확인"
            );
            return;
        }
        
        var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
        if (slotData == null) return;
        
        int activeCount = 0;
        int passiveCount = 0;
        
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"📊 현재 스킬 데이터 - {slotData.playerName} (슬롯 {slotData.slotIndex})");
        Debug.Log("═══════════════════════════════════════════════════════");
        
        if (slotData.skills != null)
        {
            Debug.Log($"\n📋 전체 스킬: {slotData.skills.Count}개");
            foreach (var saveData in slotData.skills)
            {
                var skill = saveData.ToSkillInstance();
                if (skill != null)
                {
                    if (skill.IsActiveSkill)
                    {
                        activeCount++;
                        Debug.Log($"   ⚔️ {skill.skillData.skillName} (Lv.{skill.currentLevel}/{skill.skillData.maxLevel}) {(skill.isEquipped ? "[장착]" : "")}");
                    }
                    else if (skill.IsPassiveSkill)
                    {
                        passiveCount++;
                        Debug.Log($"   🛡️ {skill.skillData.skillName} (Lv.{skill.currentLevel}/{skill.skillData.maxLevel}) {(skill.isEquipped ? "[장착]" : "")}");
                    }
                }
            }
        }
        
        Debug.Log($"\n💎 SP: {slotData.usedSP}/{slotData.totalSP}");
        Debug.Log($"🎮 레벨: {slotData.level}");
        
        Debug.Log("═══════════════════════════════════════════════════════");
        
        string message = $"캐릭터: {slotData.playerName}\n\n" +
                        $"⚔️ 액티브 스킬: {activeCount}개\n" +
                        $"🛡️ 패시브 스킬: {passiveCount}개\n\n" +
                        $"💎 SP: {slotData.usedSP}/{slotData.totalSP}\n" +
                        $"🎮 레벨: {slotData.level}\n\n" +
                        "자세한 내용은 Console 창을 확인하세요.";
        
        EditorUtility.DisplayDialog(
            "현재 스킬 데이터",
            message,
            "확인"
        );
    }
}
