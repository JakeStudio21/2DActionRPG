using UnityEngine;

/// <summary>
/// 레이더맵(RadarMapPanel) 전용 렌더러.
/// BaseMapView를 상속받아 '스크롤링(트래킹)' 좌표 변환 방식만 구현한다.
///
/// [스크롤링 방식]
/// - 플레이어 마커는 항상 마스크 정중앙에 고정되며, 회전만 적용된다.
/// - 배경 이미지(bgRect)는 플레이어 이동에 따라 반대 방향으로 anchoredPosition이 이동한다.
/// - 배경 이미지는 zoomMultiplier에 따라 마스크보다 크게 유지된다.
/// - 타겟 마커 위치는 플레이어와의 월드 거리 × 픽셀/월드 비율로 계산된다.
///
/// [부착 위치] RadarMapPanel > MinimapMask GameObject
/// </summary>
public class RadarMapView : BaseMapView
{
    // ── Inspector ────────────────────────────────────────────────────
    [Header("레이더맵 설정")]
    [Tooltip("배경 이미지 크기 = sprite 원본 픽셀 크기 × 이 값\n" +
             "1.0 = 맵 전체가 마스크에 딱 맞음\n" +
             "2.0 = 2배 확대 (마스크의 1/4 범위만 보임)")]
    [SerializeField] private float zoomMultiplier = 2f;

    // ── 캐시 ─────────────────────────────────────────────────────────
    private float _bgWidth;       // 배경 이미지 너비 (px)
    private float _bgHeight;      // 배경 이미지 높이 (px)
    private float _pxPerWorldX;   // X 방향 픽셀 / 월드 단위
    private float _pxPerWorldY;   // Y 방향 픽셀 / 월드 단위

    // ────────────────────────────────────────────────────────────────
    //  InitBackground — 배경을 확대 크기로 설정하고 비율 캐시
    // ────────────────────────────────────────────────────────────────

    protected override void InitBackground()
    {
        var data = MinimapManager.Instance?.Data;
        if (data?.mapSprite == null || bgImage == null) return;

        bgImage.sprite = data.mapSprite;

        // 월드 비율을 유지하는 단일 px/unit 기준을 X 방향으로 계산
        // 스프라이트 너비 기준으로 world unit 당 픽셀 수를 구한 뒤 zoomMultiplier 적용
        float pxPerUnit = (data.mapSprite.rect.width / Mathf.Max(data.WorldSize.x, 0.001f)) * zoomMultiplier;

        // X/Y 모두 동일한 pxPerUnit 적용 → 배경 비율이 월드 비율과 일치
        _bgWidth  = data.WorldSize.x * pxPerUnit;
        _bgHeight = data.WorldSize.y * pxPerUnit;

        bgRect.sizeDelta = new Vector2(_bgWidth, _bgHeight);

        // X/Y 동일한 비율 → 마커 좌표 왜곡 없음
        _pxPerWorldX = pxPerUnit;
        _pxPerWorldY = pxPerUnit;

        float visibleWorldX = maskRect != null ? maskRect.rect.width  / pxPerUnit : 0f;
        float visibleWorldY = maskRect != null ? maskRect.rect.height / pxPerUnit : 0f;
        Debug.Log($"[RadarMapView] zoomMultiplier={zoomMultiplier:F1} → " +
                  $"배경 크기: {_bgWidth:F0}×{_bgHeight:F0} (px), " +
                  $"가시 범위: {visibleWorldX:F1}×{visibleWorldY:F1} 월드 단위");
    }

    // ────────────────────────────────────────────────────────────────
    //  UpdateBackground — 플레이어 위치에 따라 배경 역방향 이동
    // ────────────────────────────────────────────────────────────────

    protected override void UpdateBackground()
    {
        var data      = MinimapManager.Instance?.Data;
        var playerPos = MinimapManager.Instance?.PlayerTransform?.position;

        if (data == null || playerPos == null || _bgWidth <= 0f) return;

        // 플레이어의 월드 정규화 좌표 (0 ~ 1)
        float nx = Mathf.InverseLerp(data.worldMin.x, data.worldMax.x, playerPos.Value.x);
        float ny = Mathf.InverseLerp(data.worldMin.y, data.worldMax.y, playerPos.Value.y);

        // 배경 이동: 플레이어가 월드 중앙(0.5)이면 offset = 0
        // 플레이어가 좌측 끝(nx = 0)이면 배경은 오른쪽으로 이동 (+bgWidth/2)
        var bgPos = new Vector2(
            (0.5f - nx) * _bgWidth,
            (0.5f - ny) * _bgHeight
        );
        bgRect.anchoredPosition = bgPos;
    }

    // ────────────────────────────────────────────────────────────────
    //  UpdatePlayerMarkerPosition — 항상 중앙 고정
    // ────────────────────────────────────────────────────────────────

    protected override void UpdatePlayerMarkerPosition()
    {
        if (playerMarker != null)
            playerMarker.localPosition = Vector2.zero;
    }

    // ────────────────────────────────────────────────────────────────
    //  ComputeMarkerPosition — 플레이어 기준 상대 좌표
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 타겟 월드 좌표를 플레이어 기준 상대 위치(px)로 변환한다.
    /// 마커가 플레이어에 가까울수록 중앙에 가깝고, 멀수록 테두리에 가까워진다.
    /// </summary>
    protected override Vector2 ComputeMarkerPosition(Vector3 worldPos)
    {
        if (_pxPerWorldX <= 0f) return Vector2.zero;

        var playerPos = MinimapManager.Instance?.PlayerTransform?.position ?? Vector3.zero;

        return new Vector2(
            (worldPos.x - playerPos.x) * _pxPerWorldX,
            (worldPos.y - playerPos.y) * _pxPerWorldY
        );
    }
}
