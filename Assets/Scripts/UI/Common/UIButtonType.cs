using System;

/// <summary>
/// 🎯 UI 버튼 타입 분류
/// - Navigation: 화면 전환 버튼 (인벤토리, 상점, 캐릭터 정보 등)
/// - Action: 실제 행동 버튼 (구매, 장착, 판매 등)
/// - Tab: 상태 유지 탭 버튼 (카테고리, 캐릭터 선택 등)
/// - Utility: 보조 버튼 (닫기, 뒤로가기 등)
/// </summary>
[Serializable]
public enum UIButtonType
{
    /// <summary>네비게이션 버튼 - 다른 화면으로 이동</summary>
    Navigation,
    
    /// <summary>행동 버튼 - 실제 결과가 발생하는 액션</summary>
    Action,
    
    /// <summary>탭 버튼 - 선택 상태 유지 (UITabController 사용)</summary>
    Tab,
    
    /// <summary>보조 버튼 - 닫기, 뒤로가기 등</summary>
    Utility
}

