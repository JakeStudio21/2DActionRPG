using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아이소메트릭 캐릭터 전용 데이터 구조
/// </summary>
[System.Serializable]
public class IsometricCharacterData
{
    [Header("방향 시스템")]
    [SerializeField] private DirectionPreset directionPreset = DirectionPreset.E4M;
    
    [Header("발 위치 보정")]
    [SerializeField] private Vector2 footOffset = Vector2.zero; // 월드 단위(m), +x=우, +y=상
    
    [Header("높이 시스템")]
    [SerializeField] private AnimationCurve heightCurve = AnimationCurve.Constant(0f, 1f, 0f); // 입력/출력 0~1
    [SerializeField] private int heightAmplitude = 0; // 정렬 보정 스케일
    
    // Properties
    public DirectionPreset DirectionPreset => directionPreset;
    public Vector2 FootOffset => footOffset;
    public AnimationCurve HeightCurve => heightCurve;
    public int HeightAmplitude => heightAmplitude;
    
    /// <summary>
    /// 런타임 높이 오프셋 계산
    /// </summary>
    /// <param name="t">높이 곡선 시간 (0~1)</param>
    /// <returns>계산된 높이 오프셋</returns>
    public int CalculateHeightOffset(float t)
    {
        if (heightAmplitude == 0) return 0;
        
        float normalizedHeight = Mathf.Clamp01(t);
        float curveValue = heightCurve.Evaluate(normalizedHeight);
        return Mathf.RoundToInt(curveValue * heightAmplitude);
    }
    
    /// <summary>
    /// 월드 좌표 기준 발 위치 계산
    /// </summary>
    /// <param name="centerPosition">캐릭터 중심 위치</param>
    /// <returns>발 위치 월드 좌표</returns>
    public Vector3 GetFootWorldPosition(Vector3 centerPosition)
    {
        return centerPosition + (Vector3)footOffset;
    }
    
    /// <summary>
    /// 하위 호환을 위한 기본값 설정
    /// </summary>
    public void SetDefaults()
    {
        directionPreset = DirectionPreset.E4M;
        footOffset = Vector2.zero;
        heightCurve = AnimationCurve.Constant(0f, 1f, 0f);
        heightAmplitude = 0;
    }
    
    /// <summary>
    /// 데이터 유효성 검증
    /// </summary>
    /// <returns>데이터가 유효하면 true</returns>
    public bool IsValid()
    {
        return heightCurve != null && heightAmplitude >= 0;
    }
}

/// <summary>
/// 방향 프리셋 타입
/// </summary>
public enum DirectionPreset
{
    [Tooltip("4방향 + 미러링 (E, SE, S, SW + 좌우 미러링)")]
    E4M = 0,
    
    [Tooltip("8방향 (N, NE, E, SE, S, SW, W, NW)")]
    E8 = 1
}
