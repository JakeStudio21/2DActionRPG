using UnityEngine;
using UnityEditor;

/// <summary>
/// Phase 4-C 테스트 메뉴
/// Tools → Phase 4-C Test 메뉴 항목 추가
/// </summary>
public class Phase4C_TestMenu
{
    [MenuItem("Tools/Phase 4-C Test")]
    public static void RunPhase4CTest()
    {
        // 씬에서 Phase4C_RuneSystemTest 컴포넌트를 찾거나 생성
        var testComponent = Object.FindObjectOfType<Phase4C_RuneSystemTest>();
        
        if (testComponent == null)
        {
            // 없으면 임시 GameObject에 추가
            GameObject testObject = new GameObject("Phase4C_RuneSystemTest");
            testComponent = testObject.AddComponent<Phase4C_RuneSystemTest>();
            Debug.Log("[Phase4C_TestMenu] 테스트 컴포넌트 생성됨.");
        }
        
        // 모든 테스트 실행
        testComponent.RunAllTests();
    }
}

