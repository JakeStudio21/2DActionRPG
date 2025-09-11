using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아이소메트릭 방향 계산 헬퍼
/// </summary>
public static class IsometricDirectionHelper
{
    /// <summary>
    /// 8방향 열거형
    /// </summary>
    public enum Direction8
    {
        North = 0,      // N  (0°)
        NorthEast = 1,  // NE (45°)
        East = 2,       // E  (90°)
        SouthEast = 3,  // SE (135°)
        South = 4,      // S  (180°)
        SouthWest = 5,  // SW (225°)
        West = 6,       // W  (270°)
        NorthWest = 7   // NW (315°)
    }
    
    /// <summary>
    /// 4방향 + 미러링 열거형
    /// </summary>
    public enum Direction4M
    {
        East = 0,       // E  (미러링: W)
        SouthEast = 1,  // SE (미러링: SW)
        South = 2,      // S  (미러링 없음)
        SouthWest = 3   // SW (실제 방향)
    }
    
    /// <summary>
    /// Vector2를 8방향으로 변환
    /// </summary>
    /// <param name="direction">입력 방향 벡터</param>
    /// <returns>8방향 중 가장 가까운 방향</returns>
    public static Direction8 VectorToDirection8(Vector2 direction)
    {
        if (direction.magnitude < 0.1f) return Direction8.South; // 기본값
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        
        // 8방향으로 분할 (각 방향당 45도)
        int directionIndex = Mathf.RoundToInt(angle / 45f) % 8;
        return (Direction8)directionIndex;
    }
    
    /// <summary>
    /// Vector2를 4방향+미러링으로 변환
    /// </summary>
    /// <param name="direction">입력 방향 벡터</param>
    /// <param name="needsMirroring">미러링 필요 여부 (왼쪽 방향인 경우)</param>
    /// <returns>4방향 중 하나</returns>
    public static Direction4M VectorToDirection4M(Vector2 direction, out bool needsMirroring)
    {
        needsMirroring = false;
        
        if (direction.magnitude < 0.1f) 
        {
            return Direction4M.South; // 기본값
        }
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        
        // 8방향으로 먼저 판단
        Direction8 dir8 = VectorToDirection8(direction);
        
        switch (dir8)
        {
            case Direction8.North:
            case Direction8.South:
                return Direction4M.South;
                
            case Direction8.East:
                return Direction4M.East;
                
            case Direction8.NorthEast:
            case Direction8.SouthEast:
                return Direction4M.SouthEast;
                
            case Direction8.West:
                needsMirroring = true;
                return Direction4M.East; // 미러링된 East
                
            case Direction8.NorthWest:
            case Direction8.SouthWest:
                needsMirroring = true;
                return Direction4M.SouthEast; // 미러링된 SouthEast
                
            default:
                return Direction4M.South;
        }
    }
    
    /// <summary>
    /// 방향을 Vector2로 변환 (8방향)
    /// </summary>
    public static Vector2 Direction8ToVector(Direction8 direction)
    {
        switch (direction)
        {
            case Direction8.North:     return Vector2.up;
            case Direction8.NorthEast: return new Vector2(1, 1).normalized;
            case Direction8.East:      return Vector2.right;
            case Direction8.SouthEast: return new Vector2(1, -1).normalized;
            case Direction8.South:     return Vector2.down;
            case Direction8.SouthWest: return new Vector2(-1, -1).normalized;
            case Direction8.West:      return Vector2.left;
            case Direction8.NorthWest: return new Vector2(-1, 1).normalized;
            default: return Vector2.down;
        }
    }
    
    /// <summary>
    /// 방향을 Vector2로 변환 (4방향+미러링)
    /// </summary>
    public static Vector2 Direction4MToVector(Direction4M direction, bool mirrored = false)
    {
        Vector2 baseVector;
        switch (direction)
        {
            case Direction4M.East:      baseVector = Vector2.right; break;
            case Direction4M.SouthEast: baseVector = new Vector2(1, -1).normalized; break;
            case Direction4M.South:     baseVector = Vector2.down; break;
            case Direction4M.SouthWest: baseVector = new Vector2(-1, -1).normalized; break;
            default: baseVector = Vector2.down; break;
        }
        
        if (mirrored)
        {
            baseVector.x = -baseVector.x; // X축 미러링
        }
        
        return baseVector;
    }
}
