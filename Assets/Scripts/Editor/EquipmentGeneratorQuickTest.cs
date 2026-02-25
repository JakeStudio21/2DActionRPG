using UnityEditor;
using UnityEngine;

/// <summary>
/// 🧪 DynamicEquipmentGenerator 빠른 테스트
/// Tools → Quick Test: Equipment Generator로 실행
/// </summary>
public static class EquipmentGeneratorQuickTest
{
    [MenuItem("Tools/Quick Test: Equipment Generator")]
    public static void RunQuickTest()
    {
        Debug.Log("========================================");
        Debug.Log("🧪 [Quick Test] EquipmentGenerator 테스트 시작");
        Debug.Log("========================================");
        
        // 테스트 1: StatPoolDataLoader 초기화 확인
        Debug.Log("\n📊 [Test 1] StatPoolDataLoader 초기화 테스트");
        StatPoolDataLoader.Initialize();
        
        if (StatPoolDataLoader.IsInitialized)
        {
            Debug.Log($"✅ StatPoolDataLoader 초기화 성공!");
            Debug.Log($"   - 로드된 풀 개수: {StatPoolDataLoader.StatPools.Count}개");
            
            // 풀 내용 출력
            foreach (var pool in StatPoolDataLoader.StatPools)
            {
                Debug.Log($"   - {pool.Key}: MainStat={pool.Value.MainStat}, SubStats={pool.Value.SubStats.Count}개");
            }
        }
        else
        {
            Debug.LogError("❌ StatPoolDataLoader 초기화 실패!");
            return;
        }
        
        // 테스트 2: 장비 생성 (하드코딩 예시)
        Debug.Log("\n🎲 [Test 2] 장비 생성 테스트");
        
        // Resources 폴더에서 테스트용 장비 로드 시도
        EquipmentData[] allEquipment = Resources.LoadAll<EquipmentData>("Equipment");
        
        if (allEquipment.Length == 0)
        {
            Debug.LogWarning("⚠️ Resources/Equipment 폴더에 EquipmentData가 없습니다.");
            Debug.LogWarning("   테스트를 위해 수동으로 EquipmentData를 생성하고 Resources/Equipment/ 폴더에 배치하세요.");
            
            // 테스트 데이터 생성 안내
            Debug.Log("\n💡 테스트 데이터 생성 방법:");
            Debug.Log("   1. Project 창에서 우클릭 → Create → Equipment → EquipmentData");
            Debug.Log("   2. 생성된 파일을 Resources/Equipment/ 폴더로 이동");
            Debug.Log("   3. Inspector에서 설정:");
            Debug.Log("      - equipmentName: Test_Sword");
            Debug.Log("      - equipmentSlot: MainWeapon");
            Debug.Log("      - equipmentType: Weapon");
            Debug.Log("      - baseBudget: 150");
            Debug.Log("      - mainStatWeight: 1.0");
            Debug.Log("      - subStatWeight: 0.3");
            Debug.Log("   4. Tools → Quick Test 다시 실행");
            
            return;
        }
        
        Debug.Log($"✅ 발견된 EquipmentData: {allEquipment.Length}개");
        
        // 첫 번째 장비로 테스트
        EquipmentData testData = allEquipment[0];
        Debug.Log($"   테스트 장비: {testData.equipmentName}");
        Debug.Log($"   슬롯: {testData.equipmentSlot}");
        Debug.Log($"   원본 등급: {testData.itemGrade}");
        Debug.Log($"   베이스 예산: {testData.baseBudget}");
        Debug.Log("\n⚠️ 주의: 이 테스트는 모든 등급으로 생성하여 시스템을 검증합니다.");
        Debug.Log("   실제 게임에서는 장비의 원본 등급({testData.itemGrade})으로만 생성됩니다.\n");
        
        // 여러 등급으로 생성 테스트
        ItemGrade[] testGrades = { ItemGrade.D, ItemGrade.C, ItemGrade.B, ItemGrade.A, ItemGrade.S, ItemGrade.SS, ItemGrade.EX, ItemGrade.TR };
        
        foreach (var grade in testGrades)
        {
            Debug.Log($"\n--- {grade} 등급 생성 ---");
            
            EquipmentInstance instance = DynamicEquipmentGenerator.Generate(testData, grade);
            
            if (instance != null)
            {
                Debug.Log($"✅ {grade} 등급 생성 성공!");
                Debug.Log($"   - 주옵션: {instance.finalMainStatValue}");
                Debug.Log($"   - 부옵션: {instance.randomSubStats.Count}개");
                
                foreach (var pair in instance.randomSubStats)
                {
                    Debug.Log($"     * {pair.Key}: {pair.Value}");
                }
            }
            else
            {
                Debug.LogError($"❌ {grade} 등급 생성 실패!");
            }
        }
        
        Debug.Log("\n========================================");
        Debug.Log("🎉 [Quick Test] 테스트 완료!");
        Debug.Log("========================================");
    }
    
    [MenuItem("Tools/Test: StatPoolDataLoader Only")]
    public static void TestStatPoolDataLoader()
    {
        Debug.Log("========================================");
        Debug.Log("📊 [Test] StatPoolDataLoader 단독 테스트");
        Debug.Log("========================================");
        
        StatPoolDataLoader.Initialize();
        
        if (StatPoolDataLoader.IsInitialized)
        {
            Debug.Log($"✅ 초기화 성공! 총 {StatPoolDataLoader.StatPools.Count}개 풀 로드됨");
            
            // 모든 풀 상세 출력
            foreach (var pool in StatPoolDataLoader.StatPools)
            {
                Debug.Log($"\n[{pool.Key}]");
                Debug.Log($"  MainStat: {pool.Value.MainStat}");
                Debug.Log($"  SubStats ({pool.Value.SubStats.Count}개):");
                
                for (int i = 0; i < pool.Value.SubStats.Count; i++)
                {
                    Debug.Log($"    [{i+1}] {pool.Value.SubStats[i]}");
                }
            }
        }
        else
        {
            Debug.LogError("❌ 초기화 실패!");
        }
        
        Debug.Log("\n========================================");
    }
}

