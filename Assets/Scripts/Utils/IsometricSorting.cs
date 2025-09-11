using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아이소메트릭 뷰 소팅 규칙 유틸리티 (Grid 기반 해상도 독립)
/// </summary>
public static class IsometricSorting
{
    // 레이어별 기본 오프셋
    public const int GROUND_LAYER = 0;        // 지면 (타일맵, 바닥)
    public const int CHARACTER_LAYER = 1000;  // 캐릭터 (플레이어, 적)
    public const int EFFECT_LAYER = 2000;     // 이펙트 (파티클, 스킬)
    public const int UI_LAYER = 3000;         // 월드 UI (체력바, 데미지)
    
    // 타일당 소팅 분할 수 (8~16 권장)
    public const int ORDERS_PER_TILE = 10;
    
    // Unity 소팅 오더 한계
    public const int MAX_SORTING_ORDER = 32767;
    public const int MIN_SORTING_ORDER = -32768;
    
    /// <summary>
    /// Grid 기반 해상도 독립 소팅 오더 계산
    /// </summary>
    /// <param name="footY">발 위치 Y 좌표</param>
    /// <param name="gridCellSizeY">Grid의 cellSize.y 값</param>
    /// <param name="baseLayer">레이어별 기본 오프셋</param>
    /// <param name="heightOffset">점프/비행 등 가짜 높이</param>
    /// <returns>계산된 소팅 오더</returns>
    public static int CalculateSortingOrder(float footY, float gridCellSizeY = 1f, int baseLayer = CHARACTER_LAYER, int heightOffset = 0)
    {
        // Grid 스케일 고려한 해상도 독립 공식
        int order = baseLayer + 
                   Mathf.RoundToInt((-footY / gridCellSizeY) * ORDERS_PER_TILE) + 
                   heightOffset;
        
        return Mathf.Clamp(order, MIN_SORTING_ORDER, MAX_SORTING_ORDER);
    }
    
    /// <summary>
    /// 씬에서 Grid 찾아서 cellSize.y 자동 가져오기
    /// </summary>
    /// <returns>Grid의 cellSize.y 값 (없으면 1f)</returns>
    public static float GetGridCellSizeY()
    {
        Grid grid = Object.FindObjectOfType<Grid>();
        return grid != null ? grid.cellSize.y : 1f;
    }
    
    /// <summary>
    /// 소팅 우선순위 비교 (Y 좌표 기준)
    /// </summary>
    /// <param name="footY1">첫 번째 오브젝트 Y</param>
    /// <param name="footY2">두 번째 오브젝트 Y</param>
    /// <returns>1이 앞이면 양수, 2가 앞이면 음수</returns>
    public static int CompareSortingPriority(float footY1, float footY2)
    {
        return -footY1.CompareTo(footY2); // Y가 낮을수록 앞에
    }
    
    /// <summary>
    /// 디버그용 소팅 정보 출력
    /// </summary>
    public static void LogSortingInfo(string objName, float footY, int sortingOrder, int baseLayer)
    {
        Debug.Log($"🔢 [IsometricSorting] {objName}: Y={footY:F2} → Order={sortingOrder} (base={baseLayer})");
    }
}
