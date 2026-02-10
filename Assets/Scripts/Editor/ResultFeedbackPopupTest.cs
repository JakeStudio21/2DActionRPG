using UnityEngine;
using UnityEditor;
using UI.Workshop;
using System.Collections.Generic;

/// <summary>
/// ResultFeedbackPopup 테스트 메뉴 (Unity Editor 전용)
/// Tools > Phase 0 Test > ResultFeedbackPopup 메뉴로 테스트 가능
/// </summary>
public class ResultFeedbackPopupTest
{
    [MenuItem("Tools/Phase 0 Test/1. 분해 결과 팝업 (3종 재료)")]
    public static void TestDismantleResult_MultiMaterial()
    {
        var popup = FindPopup();
        if (popup == null) return;
        
        // 테스트 데이터: 3종 재료
        var rewards = new Dictionary<MaterialType, int>
        {
            { MaterialType.WeaponFragment, 5 },
            { MaterialType.ArmorCrystal, 3 },
            { MaterialType.AccessoryCore, 1 }
        };
        
        popup.ShowDismantleResult(3, rewards);
        
        Debug.Log("[Phase 0 Test] 분해 결과 팝업 표시: 3개 아이템, 3종 재료");
    }
    
    [MenuItem("Tools/Phase 0 Test/2. 분해 결과 팝업 (1종 재료)")]
    public static void TestDismantleResult_SingleMaterial()
    {
        var popup = FindPopup();
        if (popup == null) return;
        
        // 테스트 데이터: 1종 재료 대량
        var rewards = new Dictionary<MaterialType, int>
        {
            { MaterialType.WeaponFragment, 15 }
        };
        
        popup.ShowDismantleResult(5, rewards);
        
        Debug.Log("[Phase 0 Test] 분해 결과 팝업 표시: 5개 아이템, 1종 재료");
    }
    
    [MenuItem("Tools/Phase 0 Test/3. 합성 결과 팝업 (B등급)")]
    public static void TestFusionResult_GradeB()
    {
        var popup = FindPopup();
        if (popup == null) return;
        
        // 테스트 데이터: B등급 무기
        var testItem = CreateTestEquipmentData("블레이징 소드", ItemGrade.B, EquipmentType.Weapon);
        
        if (testItem != null)
        {
            popup.ShowFusionResult(testItem, 0);
            Debug.Log("[Phase 0 Test] 합성 결과 팝업 표시: B등급 무기");
        }
    }
    
    [MenuItem("Tools/Phase 0 Test/4. 합성 결과 팝업 (S등급)")]
    public static void TestFusionResult_GradeS()
    {
        var popup = FindPopup();
        if (popup == null) return;
        
        // 테스트 데이터: S등급 방어구
        var testItem = CreateTestEquipmentData("드래곤 아머", ItemGrade.S, EquipmentType.Armor);
        
        if (testItem != null)
        {
            popup.ShowFusionResult(testItem, 0);
            Debug.Log("[Phase 0 Test] 합성 결과 팝업 표시: S등급 방어구");
        }
    }
    
    [MenuItem("Tools/Phase 0 Test/5. 팝업 수동 닫기")]
    public static void TestClosePopup()
    {
        var popup = FindPopup();
        if (popup == null) return;
        
        popup.Close();
        
        Debug.Log("[Phase 0 Test] 팝업 닫기");
    }
    
    [MenuItem("Tools/Phase 0 Test/6. 자동 닫기 테스트 (3초)")]
    public static void TestAutoClose()
    {
        var popup = FindPopup();
        if (popup == null) return;
        
        var rewards = new Dictionary<MaterialType, int>
        {
            { MaterialType.WeaponFragment, 10 }
        };
        
        popup.ShowDismantleResult(1, rewards);
        
        Debug.Log("[Phase 0 Test] 자동 닫기 테스트 시작 (3초 후 자동 닫힘)");
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 헬퍼 메서드
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    /// <summary>
    /// ResultFeedbackPopup 찾기
    /// </summary>
    private static ResultFeedbackPopup FindPopup()
    {
        var popup = GameObject.FindObjectOfType<ResultFeedbackPopup>();
        
        if (popup == null)
        {
            Debug.LogError("[Phase 0 Test] ResultFeedbackPopup을 찾을 수 없습니다! " +
                          "Lobby Scene에 프리팹이 생성되어 있는지 확인하세요.");
            return null;
        }
        
        return popup;
    }
    
    /// <summary>
    /// 테스트용 EquipmentData 생성
    /// </summary>
    private static EquipmentData CreateTestEquipmentData(string name, ItemGrade grade, EquipmentType type)
    {
        // ScriptableObject 동적 생성 (테스트용)
        var data = ScriptableObject.CreateInstance<EquipmentData>();
        data.equipmentName = name;
        data.itemGrade = grade;
        data.equipmentType = type;
        
        // 기본 아이콘 (없으면 흰색 사각형)
        // 실제 게임에서는 Resources.Load<Sprite>()로 로드
        
        return data;
    }
}

