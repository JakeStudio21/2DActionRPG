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
        // 테스트 1: StatPoolDataLoader 초기화 확인
        StatPoolDataLoader.Initialize();
        
        if (StatPoolDataLoader.IsInitialized)
        {
            // 풀 내용 출력
            foreach (var pool in StatPoolDataLoader.StatPools)
            {
            }
        }
        else
        {
            Debug.LogError("❌ StatPoolDataLoader 초기화 실패!");
            return;
        }
        
        // 테스트 2: 장비 생성 (하드코딩 예시)
        // Resources 폴더에서 테스트용 장비 로드 시도
        EquipmentData[] allEquipment = Resources.LoadAll<EquipmentData>("Equipment");
        
        if (allEquipment.Length == 0)
        {
            Debug.LogWarning("⚠️ Resources/Equipment 폴더에 EquipmentData가 없습니다.");
            Debug.LogWarning("   테스트를 위해 수동으로 EquipmentData를 생성하고 Resources/Equipment/ 폴더에 배치하세요.");
            
            // 테스트 데이터 생성 안내
            return;
        }
        // 첫 번째 장비로 테스트
        EquipmentData testData = allEquipment[0];
        // 여러 등급으로 생성 테스트
        ItemGrade[] testGrades = { ItemGrade.D, ItemGrade.C, ItemGrade.B, ItemGrade.A, ItemGrade.S, ItemGrade.SS, ItemGrade.EX, ItemGrade.TR };
        
        foreach (var grade in testGrades)
        {
            EquipmentInstance instance = DynamicEquipmentGenerator.Generate(testData, grade);
            
            if (instance != null)
            {
                foreach (var pair in instance.randomSubStats)
                {
                }
            }
            else
            {
                Debug.LogError($"❌ {grade} 등급 생성 실패!");
            }
        }
    }
    
    [MenuItem("Tools/Test: StatPoolDataLoader Only")]
    public static void TestStatPoolDataLoader()
    {
        StatPoolDataLoader.Initialize();
        
        if (StatPoolDataLoader.IsInitialized)
        {
            // 모든 풀 상세 출력
            foreach (var pool in StatPoolDataLoader.StatPools)
            {
                for (int i = 0; i < pool.Value.SubStats.Count; i++)
                {
                }
            }
        }
        else
        {
            Debug.LogError("❌ 초기화 실패!");
        }
    }
}

