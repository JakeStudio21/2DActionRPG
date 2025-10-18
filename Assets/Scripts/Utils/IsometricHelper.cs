using UnityEngine;

/// <summary>
/// 🗺️ 아이소메트릭 좌표 변환 및 편의 함수 유틸리티
/// 필요시 사용할 수 있는 좌표 변환 헬퍼 함수들 제공
/// </summary>
public static class IsometricHelper
{
    #region Grid 좌표 변환
    
    /// <summary>
    /// 월드 좌표를 그리드 좌표로 변환
    /// </summary>
    /// <param name="worldPosition">월드 좌표</param>
    /// <param name="grid">대상 Grid (null이면 씬에서 자동 찾기)</param>
    /// <returns>그리드 좌표 (Vector3Int)</returns>
    public static Vector3Int WorldToGrid(Vector3 worldPosition, Grid grid = null)
    {
        if (grid == null)
        {
            grid = GetSceneGrid();
        }
        
        if (grid != null)
        {
            return grid.WorldToCell(worldPosition);
        }
        
        // 백업: 기본 변환 (1:1 스케일 가정)
        return new Vector3Int(
            Mathf.RoundToInt(worldPosition.x),
            Mathf.RoundToInt(worldPosition.y),
            Mathf.RoundToInt(worldPosition.z)
        );
    }
    
    /// <summary>
    /// 그리드 좌표를 월드 좌표로 변환
    /// </summary>
    /// <param name="gridPosition">그리드 좌표</param>
    /// <param name="grid">대상 Grid (null이면 씬에서 자동 찾기)</param>
    /// <returns>월드 좌표</returns>
    public static Vector3 GridToWorld(Vector3Int gridPosition, Grid grid = null)
    {
        if (grid == null)
        {
            grid = GetSceneGrid();
        }
        
        if (grid != null)
        {
            return grid.CellToWorld(gridPosition);
        }
        
        // 백업: 기본 변환 (1:1 스케일 가정)
        return new Vector3(gridPosition.x, gridPosition.y, gridPosition.z);
    }
    
    /// <summary>
    /// 월드 좌표를 그리드의 중심점으로 스냅
    /// </summary>
    /// <param name="worldPosition">스냅할 월드 좌표</param>
    /// <param name="grid">대상 Grid (null이면 씬에서 자동 찾기)</param>
    /// <returns>스냅된 월드 좌표</returns>
    public static Vector3 SnapToGrid(Vector3 worldPosition, Grid grid = null)
    {
        Vector3Int gridPos = WorldToGrid(worldPosition, grid);
        return GridToWorld(gridPos, grid);
    }
    
    /// <summary>
    /// 가장 가까운 그리드 포인트 찾기
    /// </summary>
    /// <param name="worldPosition">기준 월드 좌표</param>
    /// <param name="grid">대상 Grid (null이면 씬에서 자동 찾기)</param>
    /// <returns>가장 가까운 그리드 포인트의 월드 좌표</returns>
    public static Vector3 GetNearestGridPoint(Vector3 worldPosition, Grid grid = null)
    {
        return SnapToGrid(worldPosition, grid);
    }
    
    #endregion
    
    #region 스크린 좌표 변환
    
    /// <summary>
    /// 스크린 좌표를 월드 좌표로 변환
    /// </summary>
    /// <param name="screenPosition">스크린 좌표</param>
    /// <param name="camera">대상 카메라 (null이면 메인 카메라 사용)</param>
    /// <param name="distance">카메라에서의 거리 (기본: 0)</param>
    /// <returns>월드 좌표</returns>
    public static Vector3 ScreenToWorld(Vector2 screenPosition, Camera camera = null, float distance = 0f)
    {
        if (camera == null)
        {
            camera = Camera.main;
        }
        
        if (camera != null)
        {
            Vector3 screenPos = new Vector3(screenPosition.x, screenPosition.y, distance);
            return camera.ScreenToWorldPoint(screenPos);
        }
        
        Debug.LogWarning("⚠️ [IsometricHelper] 카메라를 찾을 수 없어 스크린→월드 변환 실패");
        return Vector3.zero;
    }
    
    /// <summary>
    /// 월드 좌표를 스크린 좌표로 변환
    /// </summary>
    /// <param name="worldPosition">월드 좌표</param>
    /// <param name="camera">대상 카메라 (null이면 메인 카메라 사용)</param>
    /// <returns>스크린 좌표</returns>
    public static Vector2 WorldToScreen(Vector3 worldPosition, Camera camera = null)
    {
        if (camera == null)
        {
            camera = Camera.main;
        }
        
        if (camera != null)
        {
            return camera.WorldToScreenPoint(worldPosition);
        }
        
        Debug.LogWarning("⚠️ [IsometricHelper] 카메라를 찾을 수 없어 월드→스크린 변환 실패");
        return Vector2.zero;
    }
    
    /// <summary>
    /// 스크린 좌표를 그리드 좌표로 직접 변환
    /// </summary>
    /// <param name="screenPosition">스크린 좌표</param>
    /// <param name="camera">대상 카메라 (null이면 메인 카메라 사용)</param>
    /// <param name="grid">대상 Grid (null이면 씬에서 자동 찾기)</param>
    /// <returns>그리드 좌표</returns>
    public static Vector3Int ScreenToGrid(Vector2 screenPosition, Camera camera = null, Grid grid = null)
    {
        Vector3 worldPos = ScreenToWorld(screenPosition, camera);
        return WorldToGrid(worldPos, grid);
    }
    
    #endregion
    
    #region 아이소메트릭 좌표 변환
    
    /// <summary>
    /// 아이소메트릭 좌표를 직교 좌표로 변환
    /// 2:1 다이아몬드 아이소메트릭 기준
    /// </summary>
    /// <param name="isoPosition">아이소메트릭 좌표</param>
    /// <returns>직교 좌표</returns>
    public static Vector2 IsometricToCartesian(Vector2 isoPosition)
    {
        float cartX = (isoPosition.x - isoPosition.y) * 0.5f;
        float cartY = (isoPosition.x + isoPosition.y) * 0.25f; // 2:1 비율
        
        return new Vector2(cartX, cartY);
    }
    
    /// <summary>
    /// 직교 좌표를 아이소메트릭 좌표로 변환
    /// 2:1 다이아몬드 아이소메트릭 기준
    /// </summary>
    /// <param name="cartPosition">직교 좌표</param>
    /// <returns>아이소메트릭 좌표</returns>
    public static Vector2 CartesianToIsometric(Vector2 cartPosition)
    {
        float isoX = cartPosition.x + cartPosition.y * 2f; // 2:1 비율 역변환
        float isoY = -cartPosition.x + cartPosition.y * 2f;
        
        return new Vector2(isoX, isoY);
    }
    
    /// <summary>
    /// 아이소메트릭 타일 좌표를 픽셀 좌표로 변환
    /// </summary>
    /// <param name="tilePosition">타일 좌표 (그리드 기준)</param>
    /// <param name="tileSize">타일 크기 (픽셀)</param>
    /// <returns>픽셀 좌표</returns>
    public static Vector2 TileToPixel(Vector2Int tilePosition, Vector2Int tileSize)
    {
        float pixelX = (tilePosition.x - tilePosition.y) * (tileSize.x * 0.5f);
        float pixelY = (tilePosition.x + tilePosition.y) * (tileSize.y * 0.25f);
        
        return new Vector2(pixelX, pixelY);
    }
    
    /// <summary>
    /// 픽셀 좌표를 아이소메트릭 타일 좌표로 변환
    /// </summary>
    /// <param name="pixelPosition">픽셀 좌표</param>
    /// <param name="tileSize">타일 크기 (픽셀)</param>
    /// <returns>타일 좌표 (반올림됨)</returns>
    public static Vector2Int PixelToTile(Vector2 pixelPosition, Vector2Int tileSize)
    {
        float tempX = pixelPosition.x / (tileSize.x * 0.5f);
        float tempY = pixelPosition.y / (tileSize.y * 0.25f);
        
        int tileX = Mathf.RoundToInt((tempX + tempY) * 0.5f);
        int tileY = Mathf.RoundToInt((tempY - tempX) * 0.5f);
        
        return new Vector2Int(tileX, tileY);
    }
    
    #endregion
    
    #region 거리 및 범위 계산
    
    /// <summary>
    /// 아이소메트릭 좌표계에서의 맨하탄 거리 계산
    /// </summary>
    /// <param name="from">시작 그리드 좌표</param>
    /// <param name="to">끝 그리드 좌표</param>
    /// <returns>맨하탄 거리</returns>
    public static int ManhattanDistance(Vector3Int from, Vector3Int to)
    {
        return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
    }
    
    /// <summary>
    /// 아이소메트릭 좌표계에서의 다이아몬드 거리 계산
    /// </summary>
    /// <param name="from">시작 그리드 좌표</param>
    /// <param name="to">끝 그리드 좌표</param>
    /// <returns>다이아몬드 거리</returns>
    public static int DiamondDistance(Vector3Int from, Vector3Int to)
    {
        int dx = Mathf.Abs(from.x - to.x);
        int dy = Mathf.Abs(from.y - to.y);
        
        return Mathf.Max(dx, dy);
    }
    
    /// <summary>
    /// 그리드 범위 내의 모든 좌표 가져오기
    /// </summary>
    /// <param name="center">중심 좌표</param>
    /// <param name="radius">반경 (그리드 단위)</param>
    /// <returns>범위 내 그리드 좌표들</returns>
    public static Vector3Int[] GetGridRange(Vector3Int center, int radius)
    {
        var positions = new System.Collections.Generic.List<Vector3Int>();
        
        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, center.z);
                if (ManhattanDistance(center, pos) <= radius)
                {
                    positions.Add(pos);
                }
            }
        }
        
        return positions.ToArray();
    }
    
    /// <summary>
    /// 다이아몬드 모양 범위 내의 그리드 좌표들 가져오기
    /// </summary>
    /// <param name="center">중심 좌표</param>
    /// <param name="radius">반경 (그리드 단위)</param>
    /// <returns>다이아몬드 범위 내 그리드 좌표들</returns>
    public static Vector3Int[] GetDiamondRange(Vector3Int center, int radius)
    {
        var positions = new System.Collections.Generic.List<Vector3Int>();
        
        for (int x = center.x - radius; x <= center.x + radius; x++)
        {
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, center.z);
                if (DiamondDistance(center, pos) <= radius)
                {
                    positions.Add(pos);
                }
            }
        }
        
        return positions.ToArray();
    }
    
    #endregion
    
    #region 유틸리티 함수
    
    /// <summary>
    /// 씬에서 Grid 컴포넌트 찾기 (캐시됨)
    /// </summary>
    /// <returns>씬의 Grid 컴포넌트 (없으면 null)</returns>
    public static Grid GetSceneGrid()
    {
        // 간단한 캐시 구현 (씬 변경 시 초기화됨)
        if (_cachedGrid == null || _cachedGrid.gameObject.scene.name == null)
        {
            _cachedGrid = Object.FindObjectOfType<Grid>();
        }
        
        return _cachedGrid;
    }
    private static Grid _cachedGrid;
    
    /// <summary>
    /// 그리드 셀 크기 가져오기
    /// </summary>
    /// <param name="grid">대상 Grid (null이면 씬에서 자동 찾기)</param>
    /// <returns>셀 크기</returns>
    public static Vector3 GetCellSize(Grid grid = null)
    {
        if (grid == null)
        {
            grid = GetSceneGrid();
        }
        
        return grid != null ? grid.cellSize : Vector3.one;
    }
    
    /// <summary>
    /// 유효한 그리드 위치인지 확인
    /// </summary>
    /// <param name="gridPosition">확인할 그리드 좌표</param>
    /// <param name="bounds">유효 범위 (기본: 무제한)</param>
    /// <returns>유효하면 true</returns>
    public static bool IsValidGridPosition(Vector3Int gridPosition, BoundsInt? bounds = null)
    {
        if (!bounds.HasValue)
        {
            return true; // 범위 제한 없음
        }
        
        return bounds.Value.Contains(gridPosition);
    }
    
    /// <summary>
    /// 그리드 바운드 계산
    /// </summary>
    /// <param name="positions">그리드 좌표 배열</param>
    /// <returns>포함하는 최소 바운드</returns>
    public static BoundsInt CalculateGridBounds(Vector3Int[] positions)
    {
        if (positions.Length == 0)
        {
            return new BoundsInt(0, 0, 0, 0, 0, 0);
        }
        
        Vector3Int min = positions[0];
        Vector3Int max = positions[0];
        
        foreach (Vector3Int pos in positions)
        {
            min = Vector3Int.Min(min, pos);
            max = Vector3Int.Max(max, pos);
        }
        
        return new BoundsInt(min, max - min + Vector3Int.one);
    }
    
    /// <summary>
    /// 두 점 사이의 직선상 그리드 좌표들 계산 (브레젠함 알고리즘)
    /// </summary>
    /// <param name="from">시작 좌표</param>
    /// <param name="to">끝 좌표</param>
    /// <returns>직선상의 모든 그리드 좌표</returns>
    public static Vector3Int[] GetLinePositions(Vector3Int from, Vector3Int to)
    {
        var positions = new System.Collections.Generic.List<Vector3Int>();
        
        int x0 = from.x, y0 = from.y;
        int x1 = to.x, y1 = to.y;
        
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        
        int x = x0, y = y0;
        
        while (true)
        {
            positions.Add(new Vector3Int(x, y, from.z));
            
            if (x == x1 && y == y1) break;
            
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }
        
        return positions.ToArray();
    }
    
    #endregion
    
    #region 디버그 유틸리티
    
    /// <summary>
    /// 그리드 좌표 디버그 정보 출력
    /// </summary>
    /// <param name="name">오브젝트 이름</param>
    /// <param name="worldPos">월드 좌표</param>
    /// <param name="grid">대상 Grid</param>
    public static void LogGridInfo(string name, Vector3 worldPos, Grid grid = null)
    {
        Vector3Int gridPos = WorldToGrid(worldPos, grid);
        Vector3 snappedPos = GridToWorld(gridPos, grid);
        Vector3 cellSize = GetCellSize(grid);
        
        Debug.Log($"🗺️ [IsometricHelper] {name}:");
        Debug.Log($"   World: {worldPos}");
        Debug.Log($"   Grid: {gridPos}");
        Debug.Log($"   Snapped: {snappedPos}");
        Debug.Log($"   CellSize: {cellSize}");
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// 씬 뷰에 그리드 가이드 그리기
    /// </summary>
    /// <param name="center">중심 좌표</param>
    /// <param name="size">그리드 크기</param>
    /// <param name="grid">대상 Grid</param>
    public static void DrawGridGizmos(Vector3Int center, Vector2Int size, Grid grid = null)
    {
        if (grid == null) grid = GetSceneGrid();
        if (grid == null) return;
        
        Gizmos.color = Color.cyan;
        Vector3 cellSize = grid.cellSize;
        
        for (int x = center.x - size.x/2; x <= center.x + size.x/2; x++)
        {
            for (int y = center.y - size.y/2; y <= center.y + size.y/2; y++)
            {
                Vector3Int gridPos = new Vector3Int(x, y, center.z);
                Vector3 worldPos = grid.CellToWorld(gridPos);
                
                // 셀 중심점 표시
                Gizmos.DrawWireCube(worldPos + cellSize * 0.5f, cellSize * 0.8f);
            }
        }
    }
    #endif
    
    #endregion
}