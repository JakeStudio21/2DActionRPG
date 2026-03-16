using UnityEngine;

/// <summary>
/// 전체 미니맵(MinimapPanel) 전용 렌더러.
/// BaseMapView를 상속받아 '고정형' 좌표 변환 방식만 구현한다.
///
/// [고정형 방식]
/// - 배경 이미지는 월드 비율을 유지하면서 마스크 안에 최대한 크게 표시된다.
/// - 플레이어/타겟 마커는 배경 이미지 위에 정확히 겹치도록 배치된다.
///
/// [부착 위치] MinimapPanel > MinimapMask GameObject
/// </summary>
public class MinimapView : BaseMapView
{
    // ── 런타임 캐시 ─────────────────────────────────────────────────
    // 배경 이미지의 실제 표시 크기 (월드 비율 유지, 마스크 내 최대 크기)
    private float _displayWidth;
    private float _displayHeight;

    // ────────────────────────────────────────────────────────────────
    //  InitBackground — 월드 비율을 유지하며 마스크 안에 최대 크기로 배치
    // ────────────────────────────────────────────────────────────────

    protected override void InitBackground()
    {
        var data = MinimapManager.Instance?.Data;
        if (data?.mapSprite == null || bgImage == null || maskRect == null) return;

        bgImage.sprite = data.mapSprite;

        float maskW = maskRect.rect.width;
        float maskH = maskRect.rect.height;

        // 월드의 실제 가로:세로 비율 계산
        float worldAspect = data.WorldSize.x / Mathf.Max(data.WorldSize.y, 0.001f);

        // 마스크 안에서 비율을 유지하며 가능한 최대 크기 (letterbox/pillarbox 방식)
        if (worldAspect >= maskW / maskH)
        {
            // 가로가 상대적으로 넓은 맵 → 가로를 마스크 너비에 맞춤
            _displayWidth  = maskW;
            _displayHeight = maskW / worldAspect;
        }
        else
        {
            // 세로가 상대적으로 긴 맵 → 세로를 마스크 높이에 맞춤
            _displayHeight = maskH;
            _displayWidth  = maskH * worldAspect;
        }

        bgRect.sizeDelta        = new Vector2(_displayWidth, _displayHeight);
        bgRect.anchoredPosition = Vector2.zero;

        Debug.Log($"[MinimapView] 배경 크기 설정: {_displayWidth:F0}×{_displayHeight:F0} " +
                  $"(월드비율 {worldAspect:F2}:1, 마스크 {maskW:F0}×{maskH:F0})");
    }

    // ────────────────────────────────────────────────────────────────
    //  UpdateBackground — 고정형이므로 매 프레임 조작 불필요
    // ────────────────────────────────────────────────────────────────

    protected override void UpdateBackground()
    {
        // 고정형: 배경이 이동하지 않으므로 빈 구현
    }

    // ────────────────────────────────────────────────────────────────
    //  UpdatePlayerMarkerPosition — 플레이어 월드 위치 → 배경 이미지 공간
    // ────────────────────────────────────────────────────────────────

    protected override void UpdatePlayerMarkerPosition()
    {
        if (playerMarker == null || MinimapManager.Instance?.PlayerTransform == null) return;

        playerMarker.localPosition = ComputeMarkerPosition(MinimapManager.Instance.PlayerTransform.position);
    }

    // ────────────────────────────────────────────────────────────────
    //  ComputeMarkerPosition — 월드 좌표 → 배경 이미지 기준 Local Position
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 월드 좌표를 배경 이미지 위의 위치로 변환한다.
    /// 배경 이미지가 월드 비율을 유지하므로 마커와 이미지가 정확히 일치한다.
    /// </summary>
    protected override Vector2 ComputeMarkerPosition(Vector3 worldPos)
    {
        var data = MinimapManager.Instance?.Data;
        if (data == null || _displayWidth <= 0f) return Vector2.zero;

        float nx = Mathf.InverseLerp(data.worldMin.x, data.worldMax.x, worldPos.x);
        float ny = Mathf.InverseLerp(data.worldMin.y, data.worldMax.y, worldPos.y);

        // 배경 이미지 크기 기준으로 위치 계산 (마스크 크기가 아님)
        return new Vector2(
            (nx - 0.5f) * _displayWidth,
            (ny - 0.5f) * _displayHeight
        );
    }
}
