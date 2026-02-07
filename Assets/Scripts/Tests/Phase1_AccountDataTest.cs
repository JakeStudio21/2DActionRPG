using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Phase 1: AccountData 저장/로드 시스템 테스트
/// Unity Editor에서 [Tools → Phase 1 Test] 메뉴로 실행
/// </summary>
public class Phase1_AccountDataTest : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private bool showDetailedLogs = true;
    
    [ContextMenu("Phase 1 전체 테스트 실행")]
    public void RunAllTests()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🧪 Phase 1: AccountData 저장/로드 시스템 테스트 시작");
        Debug.Log("═══════════════════════════════════════════════════════");
        
        int passCount = 0;
        int totalCount = 8;
        
        // Test 1: AccountDataManager 초기화
        if (Test1_Initialize()) passCount++;
        
        // Test 2: 아이템 인스턴스 등록
        if (Test2_RegisterInstance()) passCount++;
        
        // Test 3: 공유 창고 관리
        if (Test3_SharedInventory()) passCount++;
        
        // Test 4: 귀속 시스템
        if (Test4_BindSystem()) passCount++;
        
        // Test 5: 재료 관리
        if (Test5_MaterialManagement()) passCount++;
        
        // Test 6: JSON 저장/로드
        if (Test6_SaveLoad()) passCount++;
        
        // Test 7: 재시작 후 데이터 복원
        if (Test7_ReloadAfterRestart()) passCount++;
        
        // Test 8: 우편함 시스템
        if (Test8_MailboxSystem()) passCount++;
        
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"🎯 테스트 결과: {passCount}/{totalCount} 통과");
        if (passCount == totalCount)
            Debug.Log("✅ Phase 1 테스트 100% 성공!");
        else
            Debug.LogError($"❌ Phase 1 테스트 실패: {totalCount - passCount}개 실패");
        Debug.Log("═══════════════════════════════════════════════════════");
    }
    
    // ========================================
    // Test 1: AccountDataManager 초기화
    // ========================================
    private bool Test1_Initialize()
    {
        Debug.Log("\n--- Test 1: AccountDataManager 초기화 ---");
        
        try
        {
            // 강제 초기화 (이미 초기화되어 있을 수 있음)
            if (!AccountDataManager.IsInitialized())
            {
                AccountDataManager.Initialize();
            }
            
            if (!AccountDataManager.IsInitialized())
            {
                Debug.LogError("❌ Test 1 실패: 초기화 실패");
                return false;
            }
            
            if (showDetailedLogs)
            {
                var storagePath = AccountDataManager.Instance.GetStoragePath();
                Debug.Log($"✅ 초기화 성공, 저장 경로: {storagePath}");
            }
            
            Debug.Log("✅ Test 1 통과: AccountDataManager 초기화 성공");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 1 실패: {e.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 2: 아이템 인스턴스 등록
    // ========================================
    private bool Test2_RegisterInstance()
    {
        Debug.Log("\n--- Test 2: 아이템 인스턴스 등록 ---");
        
        try
        {
            var manager = AccountDataManager.Instance;
            
            // 3개 아이템 등록
            var id1 = manager.RegisterNewInstance("Sword_S_Equipment");
            var id2 = manager.RegisterNewInstance("Sword_A_Equipment");
            var id3 = manager.RegisterNewInstance("Bow_D_Equipment");
            
            // 조회 확인
            var instance1 = manager.GetInstance(id1);
            if (instance1 == null || instance1.templateName != "Sword_S_Equipment")
            {
                Debug.LogError("❌ Test 2 실패: 아이템 조회 실패");
                return false;
            }
            
            if (showDetailedLogs)
            {
                Debug.Log($"✅ 등록된 아이템 1: {instance1}");
                Debug.Log($"✅ 등록된 아이템 2: {manager.GetInstance(id2)}");
                Debug.Log($"✅ 등록된 아이템 3: {manager.GetInstance(id3)}");
            }
            
            Debug.Log("✅ Test 2 통과: 아이템 인스턴스 등록 성공");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 2 실패: {e.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 3: 공유 창고 관리
    // ========================================
    private bool Test3_SharedInventory()
    {
        Debug.Log("\n--- Test 3: 공유 창고 관리 ---");
        
        try
        {
            var manager = AccountDataManager.Instance;
            
            // 아이템 3개 등록
            var id1 = manager.RegisterNewInstance("Sword_B_Equipment");
            var id2 = manager.RegisterNewInstance("Bow_C_Equipment");
            var id3 = manager.RegisterNewInstance("Armor_A_Equipment");
            
            // 창고에 추가
            if (!manager.TryAddToShared(id1))
            {
                Debug.LogError("❌ Test 3 실패: 창고 추가 실패");
                return false;
            }
            
            if (!manager.TryAddToShared(id2))
            {
                Debug.LogError("❌ Test 3 실패: 창고 추가 실패 (2)");
                return false;
            }
            
            // 중복 추가 시도 (실패해야 함)
            if (manager.TryAddToShared(id1))
            {
                Debug.LogError("❌ Test 3 실패: 중복 추가가 성공함 (실패해야 정상)");
                return false;
            }
            
            // 창고 크기 제한 테스트 (maxSize=2)
            if (manager.TryAddToShared(id3, 2))
            {
                Debug.LogError("❌ Test 3 실패: 창고 크기 제한 무시됨");
                return false;
            }
            
            // 제거 테스트
            if (!manager.RemoveFromShared(id1))
            {
                Debug.LogError("❌ Test 3 실패: 창고 제거 실패");
                return false;
            }
            
            if (showDetailedLogs)
            {
                Debug.Log($"✅ 창고 추가/제거 성공");
            }
            
            Debug.Log("✅ Test 3 통과: 공유 창고 관리 성공");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 3 실패: {e.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 4: 귀속 시스템
    // ========================================
    private bool Test4_BindSystem()
    {
        Debug.Log("\n--- Test 4: 귀속 시스템 ---");
        
        try
        {
            var manager = AccountDataManager.Instance;
            
            var id = manager.RegisterNewInstance("Weapon_Epic");
            
            // 귀속 전
            if (manager.IsBound(id))
            {
                Debug.LogError("❌ Test 4 실패: 귀속되지 않은 아이템이 귀속됨");
                return false;
            }
            
            // 슬롯 0에 귀속
            manager.SetBind(id, 0);
            
            if (!manager.IsBound(id))
            {
                Debug.LogError("❌ Test 4 실패: 귀속 설정 실패");
                return false;
            }
            
            // 슬롯 0에서 확인 (다른 캐릭터 아님)
            if (manager.IsBoundToOther(id, 0))
            {
                Debug.LogError("❌ Test 4 실패: 같은 슬롯인데 다른 캐릭터로 인식");
                return false;
            }
            
            // 슬롯 1에서 확인 (다른 캐릭터)
            if (!manager.IsBoundToOther(id, 1))
            {
                Debug.LogError("❌ Test 4 실패: 다른 슬롯인데 같은 캐릭터로 인식");
                return false;
            }
            
            var (isBound, slotIndex) = manager.GetBindInfo(id);
            if (!isBound || slotIndex != 0)
            {
                Debug.LogError($"❌ Test 4 실패: 귀속 정보 오류 (isBound={isBound}, slotIndex={slotIndex})");
                return false;
            }
            
            if (showDetailedLogs)
            {
                Debug.Log($"✅ 귀속 정보: Slot {slotIndex}");
            }
            
            Debug.Log("✅ Test 4 통과: 귀속 시스템 성공");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 4 실패: {e.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 5: 재료 관리
    // ========================================
    private bool Test5_MaterialManagement()
    {
        Debug.Log("\n--- Test 5: 재료 관리 ---");
        
        try
        {
            var manager = AccountDataManager.Instance;
            
            // 재료 추가
            manager.AddMaterial("FRAGMENT_ENHANCE", 100);
            manager.AddMaterial("SPIRITSTONE", 50);
            
            // 추가 누적
            manager.AddMaterial("FRAGMENT_ENHANCE", 50);
            
            // 보유량 확인
            if (manager.GetMaterialCount("FRAGMENT_ENHANCE") != 150)
            {
                Debug.LogError($"❌ Test 5 실패: 파편 수량 오류 (예상: 150, 실제: {manager.GetMaterialCount("FRAGMENT_ENHANCE")})");
                return false;
            }
            
            // 소모
            if (!manager.ConsumeMaterial("FRAGMENT_ENHANCE", 30))
            {
                Debug.LogError("❌ Test 5 실패: 재료 소모 실패");
                return false;
            }
            
            if (manager.GetMaterialCount("FRAGMENT_ENHANCE") != 120)
            {
                Debug.LogError($"❌ Test 5 실패: 소모 후 수량 오류 (예상: 120, 실제: {manager.GetMaterialCount("FRAGMENT_ENHANCE")})");
                return false;
            }
            
            // 부족한 경우
            if (manager.ConsumeMaterial("FRAGMENT_ENHANCE", 200))
            {
                Debug.LogError("❌ Test 5 실패: 부족한데 소모 성공함");
                return false;
            }
            
            if (showDetailedLogs)
            {
                Debug.Log($"✅ 파편: {manager.GetMaterialCount("FRAGMENT_ENHANCE")}개");
                Debug.Log($"✅ 정령석: {manager.GetMaterialCount("SPIRITSTONE")}개");
            }
            
            Debug.Log("✅ Test 5 통과: 재료 관리 성공");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 5 실패: {e.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 6: JSON 저장/로드
    // ========================================
    private bool Test6_SaveLoad()
    {
        Debug.Log("\n--- Test 6: JSON 저장/로드 ---");
        
        try
        {
            var manager = AccountDataManager.Instance;
            
            // 현재 상태 저장
            var beforeData = manager.GetAccountData();
            int itemCountBefore = beforeData.itemInstances.Count;
            int sharedCountBefore = beforeData.sharedInventoryIds.Count;
            int materialCountBefore = beforeData.materials.Count;
            
            // 저장
            manager.Save();
            
            if (showDetailedLogs)
            {
                Debug.Log($"✅ 저장 전: Items={itemCountBefore}, Shared={sharedCountBefore}, Materials={materialCountBefore}");
            }
            
            // 새로운 Manager 인스턴스 (재시작 시뮬레이션은 Test 7에서)
            // 여기서는 Save() 성공 여부만 확인
            
            Debug.Log("✅ Test 6 통과: JSON 저장 성공");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 6 실패: {e.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 7: 재시작 후 데이터 복원
    // ========================================
    private bool Test7_ReloadAfterRestart()
    {
        Debug.Log("\n--- Test 7: 재시작 후 데이터 복원 ---");
        
        try
        {
            var manager = AccountDataManager.Instance;
            
            // 저장 전 데이터
            var data = manager.GetAccountData();
            int expectedItems = data.itemInstances.Count;
            int expectedMaterials = data.materials.Count;
            
            // 저장
            manager.Save();
            
            // Load() 재호출하여 재시작 시뮬레이션
            manager.Load();
            
            // 복원된 데이터 확인
            var reloadedData = manager.GetAccountData();
            
            if (reloadedData.itemInstances.Count != expectedItems)
            {
                Debug.LogError($"❌ Test 7 실패: 아이템 수 불일치 (예상: {expectedItems}, 실제: {reloadedData.itemInstances.Count})");
                return false;
            }
            
            if (reloadedData.materials.Count != expectedMaterials)
            {
                Debug.LogError($"❌ Test 7 실패: 재료 수 불일치 (예상: {expectedMaterials}, 실제: {reloadedData.materials.Count})");
                return false;
            }
            
            if (showDetailedLogs)
            {
                Debug.Log($"✅ 복원 성공: Items={reloadedData.itemInstances.Count}, Materials={reloadedData.materials.Count}");
                manager.PrintStats();
            }
            
            Debug.Log("✅ Test 7 통과: 재시작 후 데이터 복원 성공");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 7 실패: {e.Message}");
            return false;
        }
    }
    
    // ========================================
    // Test 8: 우편함 시스템
    // ========================================
    private bool Test8_MailboxSystem()
    {
        Debug.Log("\n--- Test 8: 우편함 시스템 ---");
        
        try
        {
            var manager = AccountDataManager.Instance;
            
            var id = manager.RegisterNewInstance("Overflow_Item");
            
            // 우편함으로 이동
            if (!manager.MoveToMailbox(id))
            {
                Debug.LogError("❌ Test 8 실패: 우편함 이동 실패");
                return false;
            }
            
            // 중복 이동 시도 (실패해야 함)
            if (manager.MoveToMailbox(id))
            {
                Debug.LogError("❌ Test 8 실패: 우편함 중복 추가 성공 (실패해야 정상)");
                return false;
            }
            
            // 우편함에서 제거
            if (!manager.RemoveFromMailbox(id))
            {
                Debug.LogError("❌ Test 8 실패: 우편함 제거 실패");
                return false;
            }
            
            if (showDetailedLogs)
            {
                Debug.Log($"✅ 우편함 시스템 정상 작동");
            }
            
            Debug.Log("✅ Test 8 통과: 우편함 시스템 성공");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Test 8 실패: {e.Message}");
            return false;
        }
    }
}

#if UNITY_EDITOR
/// <summary>
/// Unity Editor 메뉴 추가
/// </summary>
public static class Phase1TestMenu
{
    [UnityEditor.MenuItem("Tools/Phase 1 Test")]
    public static void RunPhase1Test()
    {
        // 씬에서 테스트 오브젝트 찾기
        var tester = Object.FindObjectOfType<Phase1_AccountDataTest>();
        
        if (tester == null)
        {
            // 없으면 생성
            var go = new GameObject("Phase1_Tester");
            tester = go.AddComponent<Phase1_AccountDataTest>();
        }
        
        tester.RunAllTests();
    }
}
#endif

