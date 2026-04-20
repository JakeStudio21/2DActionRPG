using UnityEngine;

/// <summary>
/// 카메라 줌(OrthographicSize) 설정을 담는 순수 데이터 컨테이너.
/// StageConfig, TriggerZone 등 어디서든 [SerializeField]로 인스펙터에 노출 가능.
/// ShakeData와 동일 패턴으로 설계 — useZoom = false면 전체 무시.
/// </summary>
[System.Serializable]
public class CameraZoomData
{
    [Tooltip("카메라 줌 사용 여부 (false면 기본 OrthographicSize 유지, 기존 스테이지에 영향 없음)")]
    public bool useZoom = false;

    [Tooltip("목표 OrthographicSize (기본 8, 클수록 화면이 넓어짐)")]
    [Min(1f)]
    public float targetSize = 12f;

    [Tooltip("줌 전환 시간 (초) — 이 시간 동안 부드럽게 목표 크기로 변함")]
    [Min(0f)]
    public float transitionDuration = 1.0f;

    [Tooltip("컷신/입장 연출 후 줌 시작까지 대기 시간 (초)")]
    [Min(0f)]
    public float preZoomDelay = 0.5f;

    [Tooltip("줌 이징 커브 (기본: EaseInOut — 부드럽게 시작·끝)")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
}
