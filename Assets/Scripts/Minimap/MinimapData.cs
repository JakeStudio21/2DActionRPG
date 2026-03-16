using UnityEngine;

/// <summary>
/// 씬별 미니맵 설정 데이터.
/// NavMesh 추출 툴이 worldMin/worldMax를 자동으로 기입하며,
/// mapSprite는 PNG 추출 후 후보정된 이미지를 수동으로 연결한다.
/// </summary>
[CreateAssetMenu(menuName = "Minimap/MinimapData", fileName = "MinimapData")]
public class MinimapData : ScriptableObject
{
    [Header("맵 이미지")]
    [Tooltip("NavMesh 추출 후 후보정된 맵 PNG 스프라이트")]
    public Sprite mapSprite;

    [Header("월드 바운드 (NavMesh 추출 툴이 자동 기입)")]
    [Tooltip("이동 가능 구역의 월드 최솟값 (좌하단)")]
    public Vector2 worldMin;

    [Tooltip("이동 가능 구역의 월드 최댓값 (우상단)")]
    public Vector2 worldMax;

    /// <summary>월드 바운드 크기</summary>
    public Vector2 WorldSize => worldMax - worldMin;

    /// <summary>월드 바운드 중심점</summary>
    public Vector2 WorldCenter => (worldMin + worldMax) * 0.5f;

    /// <summary>데이터가 유효한지 확인</summary>
    public bool IsValid => mapSprite != null && WorldSize.x > 0f && WorldSize.y > 0f;
}
