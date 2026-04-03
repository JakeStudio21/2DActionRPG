using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 미니맵/레이더맵에 표시할 마커 종류.
/// 플레이어 마커는 각 View의 playerMarker 필드에서 별도 처리하므로 열거형에 포함하지 않는다.
/// </summary>
public enum MinimapMarkerType
{
    Boss,
    Enemy,
    Quest,
    Exit,
    Objective // isVictoryTarget=true 바리케이드 등 미션 목표 오브젝트
}

/// <summary>
/// 미니맵에 표시될 월드 오브젝트에 부착하는 컴포넌트.
/// OnEnable/OnDisable 시 MinimapManager에 자동 등록/해제된다.
/// UI 마커 인스턴스 생성·배치는 MinimapManager를 구독하는 View들이 담당한다.
/// </summary>
public class MinimapMarker : MonoBehaviour
{
    [Header("마커 설정")]
    [Tooltip("마커 종류 (보스/적/퀘스트/출구)")]
    public MinimapMarkerType markerType = MinimapMarkerType.Enemy;

    [Tooltip("미니맵 범위 내부일 때 표시할 아이콘 스프라이트")]
    public Sprite defaultSprite;

    [Tooltip("미니맵 테두리 이탈 시 방향을 가리키는 화살표 스프라이트")]
    public Sprite arrowSprite;

    [Tooltip("마커 UI 아이콘 색상")]
    public Color markerColor = Color.white;

    /// <summary>현재 어느 뷰에서든 범위 밖으로 이탈해 있는지 여부 (외부 코드에서 참조용)</summary>
    [HideInInspector] public bool isOutOfBounds;

    // ────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        MinimapManager.Instance?.Register(this);
    }

    private void OnDisable()
    {
        MinimapManager.Instance?.Unregister(this);
    }
}
