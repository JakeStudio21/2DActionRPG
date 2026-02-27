using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 룬 시스템 Phase 2 테스트 메뉴
/// ⚙️ Unity Editor 메뉴에서 클릭만으로 모든 기능 테스트 가능
/// 
/// 메뉴 위치: Tools/Rune System/Test Phase 2/...
/// </summary>
public class RunePhase2TestMenu : Editor
{
    private const string MENU_ROOT = "Tools/Rune System/Test Phase 2/";
    
    #region 1. 룬 추가 테스트
    
    [MenuItem(MENU_ROOT + "1-1. 룬 추가 (보스 사냥꾼)")]
    public static void Test_AddRune_BossHunter()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 1-1: 룬 추가 (보스 사냥꾼) ==========");
        
        // 1. RuneData 가져오기
        RuneData data = RuneDatabase.GetRuneData("RUNE_BOSS_HUNTER");
        
        if (data == null)
        {
            Debug.LogError("❌ RuneData를 찾을 수 없습니다!");
            return;
        }
        
        // 2. 인벤토리에 추가
        RuneInstance newRune = RuneInventoryManager.Instance.AddRune(data);
        
        if (newRune != null)
        {
            Debug.Log($"✅ 룬 추가 성공!");
            Debug.Log($"   추가된 룬: {newRune}");
            Debug.Log($"   UID: {newRune.instanceUID}");
            Debug.Log($"   총 룬 개수: {RuneInventoryManager.Instance.GetRuneCount()}개");
        }
        else
        {
            Debug.LogError("❌ 룬 추가 실패!");
        }
        
        Debug.Log("==========================================");
    }
    
    [MenuItem(MENU_ROOT + "1-2. 룬 추가 (보스 철벽)")]
    public static void Test_AddRune_BossDefender()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 1-2: 룬 추가 (보스 철벽) ==========");
        
        RuneData data = RuneDatabase.GetRuneData("RUNE_BOSS_DEFENDER");
        
        if (data == null)
        {
            Debug.LogError("❌ RuneData를 찾을 수 없습니다!");
            return;
        }
        
        RuneInstance newRune = RuneInventoryManager.Instance.AddRune(data);
        
        if (newRune != null)
        {
            Debug.Log($"✅ 룬 추가 성공: {newRune}");
        }
        
        Debug.Log("==========================================");
    }
    
    [MenuItem(MENU_ROOT + "1-3. 룬 3개 추가 (한계돌파 재료용)")]
    public static void Test_AddMultipleRunes()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 1-3: 룬 3개 추가 (한계돌파 재료용) ==========");
        
        RuneData data = RuneDatabase.GetRuneData("RUNE_BOSS_HUNTER");
        
        if (data == null)
        {
            Debug.LogError("❌ RuneData를 찾을 수 없습니다!");
            return;
        }
        
        Debug.Log("보스 사냥꾼 룬 3개 추가 중...");
        
        for (int i = 0; i < 3; i++)
        {
            RuneInstance newRune = RuneInventoryManager.Instance.AddRune(data);
            Debug.Log($"  [{i + 1}] {newRune}");
        }
        
        Debug.Log($"✅ 총 {RuneInventoryManager.Instance.GetRuneCount()}개 룬 보유 중");
        Debug.Log("==========================================");
    }
    
    #endregion
    
    #region 2. 룬 검색 테스트
    
    [MenuItem(MENU_ROOT + "2-1. 특정 종류 룬 검색 (보스 사냥꾼)")]
    public static void Test_SearchByDataId()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 2-1: 특정 종류 룬 검색 ==========");
        
        List<RuneInstance> bossHunterRunes = RuneInventoryManager.Instance.GetRunesByDataId("RUNE_BOSS_HUNTER");
        
        Debug.Log($"보스 사냥꾼 룬 개수: {bossHunterRunes.Count}개");
        
        if (bossHunterRunes.Count == 0)
        {
            Debug.LogWarning("⚠️ 보스 사냥꾼 룬이 없습니다. 먼저 '1-1. 룬 추가' 메뉴를 실행해주세요.");
        }
        else
        {
            foreach (var rune in bossHunterRunes)
            {
                Debug.Log($"  - UID: {rune.instanceUID.Substring(0, 8)}... | Lv.{rune.currentLevel} | 한돌 {rune.currentLimitBreak}");
            }
        }
        
        Debug.Log("==========================================");
    }
    
    [MenuItem(MENU_ROOT + "2-2. 타입별 룬 검색 (공격형)")]
    public static void Test_SearchByType()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 2-2: 타입별 룬 검색 ==========");
        
        List<RuneInstance> attackRunes = RuneInventoryManager.Instance.GetRunesByType(RuneType.Attack1);
        
        Debug.Log($"공격형 룬 개수: {attackRunes.Count}개");
        
        foreach (var rune in attackRunes)
        {
            Debug.Log($"  - {rune}");
        }
        
        Debug.Log("==========================================");
    }
    
    #endregion
    
    #region 3. 룬 삭제 테스트
    
    [MenuItem(MENU_ROOT + "3-1. 첫 번째 룬 삭제")]
    public static void Test_RemoveFirstRune()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 3-1: 첫 번째 룬 삭제 ==========");
        
        List<RuneInstance> allRunes = RuneInventoryManager.Instance.GetAllRunes();
        
        if (allRunes.Count == 0)
        {
            Debug.LogWarning("⚠️ 인벤토리가 비어있습니다. 먼저 '1-1. 룬 추가' 메뉴를 실행해주세요.");
            return;
        }
        
        RuneInstance firstRune = allRunes[0];
        Debug.Log($"삭제할 룬: {firstRune}");
        
        bool removed = RuneInventoryManager.Instance.RemoveRune(firstRune.instanceUID);
        
        if (removed)
        {
            Debug.Log("✅ 룬 삭제 성공!");
            Debug.Log($"   남은 룬: {RuneInventoryManager.Instance.GetRuneCount()}개");
        }
        else
        {
            Debug.LogError("❌ 룬 삭제 실패!");
        }
        
        Debug.Log("==========================================");
    }
    
    [MenuItem(MENU_ROOT + "3-2. 잠금 테스트 (첫 번째 룬)")]
    public static void Test_LockAndRemove()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 3-2: 잠금 테스트 ==========");
        
        List<RuneInstance> allRunes = RuneInventoryManager.Instance.GetAllRunes();
        
        if (allRunes.Count == 0)
        {
            Debug.LogWarning("⚠️ 인벤토리가 비어있습니다.");
            return;
        }
        
        RuneInstance targetRune = allRunes[0];
        string uid = targetRune.instanceUID;
        
        Debug.Log($"대상 룬: {targetRune}");
        
        // 1. 잠금 설정
        Debug.Log("\n[1단계] 잠금 설정...");
        RuneInventoryManager.Instance.SetLock(uid, true);
        Debug.Log("✅ 잠금 완료");
        
        // 2. 삭제 시도 (실패)
        Debug.Log("\n[2단계] 잠금 상태에서 삭제 시도...");
        bool removed1 = RuneInventoryManager.Instance.RemoveRune(uid);
        Debug.Log($"삭제 결과: {(removed1 ? "성공" : "실패 (예상된 동작)")}");
        
        // 3. 잠금 해제
        Debug.Log("\n[3단계] 잠금 해제...");
        RuneInventoryManager.Instance.SetLock(uid, false);
        Debug.Log("✅ 잠금 해제 완료");
        
        // 4. 삭제 시도 (성공)
        Debug.Log("\n[4단계] 잠금 해제 후 삭제 시도...");
        bool removed2 = RuneInventoryManager.Instance.RemoveRune(uid);
        Debug.Log($"삭제 결과: {(removed2 ? "✅ 성공!" : "❌ 실패")}");
        
        Debug.Log("==========================================");
    }
    
    #endregion
    
    #region 4. 저장/로드 테스트
    
    [MenuItem(MENU_ROOT + "4-1. 인벤토리 저장 (JSON)")]
    public static void Test_SaveInventory()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 4-1: 인벤토리 저장 ==========");
        
        int runeCount = RuneInventoryManager.Instance.GetRuneCount();
        Debug.Log($"현재 룬 개수: {runeCount}개");
        
        if (runeCount == 0)
        {
            Debug.LogWarning("⚠️ 인벤토리가 비어있습니다. 먼저 '1-1. 룬 추가' 메뉴를 실행해주세요.");
        }
        
        bool saved = RuneInventoryManager.Instance.SaveInventory();
        
        if (saved)
        {
            Debug.Log("✅ 저장 완료!");
            Debug.Log("💡 이제 Unity를 재시작하거나 Play 모드를 다시 실행한 후 '4-2. 인벤토리 로드' 메뉴를 실행해보세요.");
        }
        else
        {
            Debug.LogError("❌ 저장 실패!");
        }
        
        Debug.Log("==========================================");
    }
    
    [MenuItem(MENU_ROOT + "4-2. 인벤토리 로드 (JSON)")]
    public static void Test_LoadInventory()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 4-2: 인벤토리 로드 ==========");
        
        Debug.Log("저장된 데이터 로드 중...");
        
        bool loaded = RuneInventoryManager.Instance.LoadInventory();
        
        if (loaded)
        {
            Debug.Log("✅ 로드 완료!");
            Debug.Log($"   복원된 룬 개수: {RuneInventoryManager.Instance.GetRuneCount()}개");
            
            // 로드 후 인벤토리 출력
            RuneInventoryManager.Instance.PrintInventory();
        }
        else
        {
            Debug.LogWarning("⚠️ 로드 실패 또는 저장된 데이터가 없습니다.");
            Debug.Log("💡 먼저 '4-1. 인벤토리 저장' 메뉴를 실행해보세요.");
        }
        
        Debug.Log("==========================================");
    }
    
    [MenuItem(MENU_ROOT + "4-3. 저장 데이터 삭제 (초기화)")]
    public static void Test_DeleteSaveData()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 4-3: 저장 데이터 삭제 ==========");
        
        if (EditorUtility.DisplayDialog("저장 데이터 삭제", 
            "정말로 저장된 룬 인벤토리 데이터를 삭제하시겠습니까?\n\n⚠️ 이 작업은 되돌릴 수 없습니다!", 
            "삭제", "취소"))
        {
            RuneInventoryManager.Instance.DeleteSaveData();
            Debug.Log("✅ 저장 데이터가 삭제되었습니다.");
            Debug.Log($"   현재 룬 개수: {RuneInventoryManager.Instance.GetRuneCount()}개");
        }
        else
        {
            Debug.Log("취소되었습니다.");
        }
        
        Debug.Log("==========================================");
    }
    
    #endregion
    
    #region 5. 인벤토리 조회
    
    [MenuItem(MENU_ROOT + "5-1. 인벤토리 전체 출력")]
    public static void Test_PrintInventory()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        RuneInventoryManager.Instance.PrintInventory();
    }
    
    [MenuItem(MENU_ROOT + "5-2. 인벤토리 정렬 (레벨순)")]
    public static void Test_SortInventory()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 5-2: 인벤토리 정렬 ==========");
        
        RuneInventoryManager.Instance.SortInventory();
        
        Debug.Log("✅ 정렬 완료 (레벨 내림차순 → 한계돌파 내림차순)");
        
        RuneInventoryManager.Instance.PrintInventory();
        
        Debug.Log("==========================================");
    }
    
    #endregion
    
    #region 6. 통합 시나리오 테스트
    
    [MenuItem(MENU_ROOT + "6. 🎯 전체 시나리오 테스트 (자동)")]
    public static void Test_FullScenario()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========================================");
        Debug.Log("🎯 전체 시나리오 테스트 시작");
        Debug.Log("========================================\n");
        
        // 1. 초기 상태 확인
        Debug.Log("[1단계] 초기 상태 확인");
        int initialCount = RuneInventoryManager.Instance.GetRuneCount();
        Debug.Log($"  현재 룬 개수: {initialCount}개\n");
        
        // 2. 룬 3개 추가
        Debug.Log("[2단계] 룬 3개 추가");
        RuneData data1 = RuneDatabase.GetRuneData("RUNE_BOSS_HUNTER");
        RuneData data2 = RuneDatabase.GetRuneData("RUNE_BOSS_DEFENDER");
        RuneData data3 = RuneDatabase.GetRuneData("RUNE_LIFESTEAL");
        
        RuneInstance rune1 = RuneInventoryManager.Instance.AddRune(data1);
        RuneInstance rune2 = RuneInventoryManager.Instance.AddRune(data2);
        RuneInstance rune3 = RuneInventoryManager.Instance.AddRune(data3);
        
        Debug.Log($"  추가 완료: {RuneInventoryManager.Instance.GetRuneCount()}개\n");
        
        // 3. 검색 테스트
        Debug.Log("[3단계] 검색 테스트");
        List<RuneInstance> hunters = RuneInventoryManager.Instance.GetRunesByDataId("RUNE_BOSS_HUNTER");
        Debug.Log($"  보스 사냥꾼 룬: {hunters.Count}개 발견\n");
        
        // 4. 잠금 테스트
        Debug.Log("[4단계] 잠금 테스트");
        RuneInventoryManager.Instance.SetLock(rune1.instanceUID, true);
        bool removeFailed = RuneInventoryManager.Instance.RemoveRune(rune1.instanceUID);
        Debug.Log($"  잠금 상태 삭제 시도: {(removeFailed ? "성공" : "실패 (예상된 동작)")}\n");
        
        // 5. 잠금 해제 후 삭제
        Debug.Log("[5단계] 잠금 해제 후 삭제");
        RuneInventoryManager.Instance.SetLock(rune1.instanceUID, false);
        bool removeSuccess = RuneInventoryManager.Instance.RemoveRune(rune1.instanceUID);
        Debug.Log($"  삭제 성공: {removeSuccess}");
        Debug.Log($"  남은 룬: {RuneInventoryManager.Instance.GetRuneCount()}개\n");
        
        // 6. 저장 테스트
        Debug.Log("[6단계] 저장 테스트");
        bool saved = RuneInventoryManager.Instance.SaveInventory();
        Debug.Log($"  저장 결과: {(saved ? "✅ 성공" : "❌ 실패")}\n");
        
        // 7. 최종 상태 출력
        Debug.Log("[7단계] 최종 상태");
        RuneInventoryManager.Instance.PrintInventory();
        
        Debug.Log("\n========================================");
        Debug.Log("🎉 전체 시나리오 테스트 완료!");
        Debug.Log("💡 이제 Unity를 재시작하거나 Play 모드를 다시 실행한 후");
        Debug.Log("   '4-2. 인벤토리 로드' 메뉴로 저장된 데이터를 복원해보세요.");
        Debug.Log("========================================");
    }
    
    #endregion
    
    #region 7. RuneDatabase 테스트
    
    [MenuItem(MENU_ROOT + "7-1. RuneDatabase 캐시 출력")]
    public static void Test_PrintRuneDatabase()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        RuneDatabase.DebugPrintAll();
    }
    
    [MenuItem(MENU_ROOT + "7-2. 특정 runeId 검색 테스트")]
    public static void Test_GetRuneData()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("⚠️ Play 모드에서 실행해주세요!");
            return;
        }
        
        Debug.Log("========== 테스트 7-2: runeId 검색 ==========");
        
        // 성공 케이스
        RuneData data1 = RuneDatabase.GetRuneData("RUNE_BOSS_HUNTER");
        Debug.Log($"✅ 검색 성공: {(data1 != null ? data1.runeName : "null")}");
        
        // 실패 케이스
        RuneData data2 = RuneDatabase.GetRuneData("RUNE_NOT_EXIST");
        Debug.Log($"❌ 검색 실패 (예상된 동작): {(data2 == null ? "null" : data2.runeName)}");
        
        Debug.Log("==========================================");
    }
    
    #endregion
}

