using UnityEngine;
using DG.Tweening;

/// <summary>
/// 바위 이동 경로 데이터 ScriptableObject.
///
/// 분열 모드:
///   Corridor  - 좁은 통로: 퍼짐 후 Large 남은 경로(다음 WP)를 이어받아 이동
///   Scatter   - 넓은 광장: 퍼짐 후 scatterDuration 초 뒤 자연 소멸 (경로 없음)
///   OwnPath   - 특수 연출: 퍼짐 후 smallPathOptions 중 랜덤 경로를 따라 이동
/// </summary>
[CreateAssetMenu(fileName = "BoulderPathData", menuName = "Stage/Boulder Path Data")]
public class BoulderPathData : ScriptableObject
{
    [Header("경로 설정")]
    [Tooltip("씬 월드 좌표 기준 웨이포인트 배열 (최소 2개 이상)\nY값에 기복을 주면 울퉁불퉁 지형 표현 가능")]
    public Vector3[] waypoints = new Vector3[0];

    [Tooltip("전체 이동 시간 (초). 짧을수록 빠름")]
    public float duration = 5f;

    [Tooltip("이동 가속 커브. InQuad = 내려올수록 가속 (경사면 표현)")]
    public Ease easeType = Ease.InQuad;

    [Header("울퉁불퉁 진동 설정")]
    [Tooltip("진동 강도. 0이면 진동 없음 (0.03~0.08 권장)")]
    public float bumpStrength = 0.05f;

    [Tooltip("진동 빈도 (높을수록 잦은 진동)")]
    [Range(1, 20)]
    public int bumpVibrato = 6;

    [Header("카메라 셰이크 설정")]
    [Tooltip("굴러가는 동안 boulder.rolling 이벤트를 발행하는 주기 (초).\n" +
             "0이면 rolling 셰이크 비활성화.\n" +
             "0.2~0.4 권장 (너무 짧으면 CuePlayer 쿨다운에 막힘)")]
    [Min(0f)]
    public float rollingShakeInterval = 0.3f;

    [Header("분열 설정")]
    [Tooltip("Large 바위 분열 시 생성할 Small 바위 수")]
    [Range(1, 5)]
    public int splitCount = 3;

    [Tooltip("분열 시 Small 바위가 퍼지는 초기 힘")]
    public float splitScatterForce = 4f;

    [Tooltip("분열 후 다음 동작(경로 시작 또는 소멸)까지 대기 시간 (초)")]
    public float splitSnapDelay = 0.3f;

    [Tooltip(
        "Corridor  : 통로용. 퍼짐 후 Large 남은 경로 이어받기\n" +
        "Scatter   : 광장용. 퍼짐 후 scatterDuration 초 뒤 자연 소멸\n" +
        "OwnPath   : 특수연출. 퍼짐 후 smallPathOptions 중 랜덤 경로 이동")]
    public SplitMode splitMode = SplitMode.Corridor;

    [Header("Scatter 모드 설정 (splitMode = Scatter 일 때만 사용)")]
    [Tooltip("산란 후 Small 바위가 굴러다니다 소멸하기까지 시간 (초)")]
    [Min(0.5f)]
    public float scatterDuration = 3f;

    [Tooltip("산란 중 감속 계수. 0에 가까울수록 오래 굴러감 (0.1~0.5 권장).\n" +
             "1.5 이상이면 즉시 멈춤, 0.1이면 끝까지 굴러다님")]
    [Range(0f, 3f)]
    public float scatterDrag = 0.3f;

    [Header("OwnPath 모드 설정 (splitMode = OwnPath 일 때만 사용)")]
    [Tooltip("Small 바위 전용 경로 목록. 각 Small 바위가 랜덤 선택.\n" +
             "1개 = 항상 그 경로 / 여러 개 = 랜덤 선택")]
    public BoulderPathData[] smallPathOptions;
}

/// <summary>Small 바위 분열 후 동작 모드</summary>
public enum SplitMode
{
    Corridor,   // 통로: Large 남은 경로 이어받기
    Scatter,    // 광장: 랜덤 산란 후 소멸
    OwnPath,    // 특수: 사전 설계 전용 경로 사용
}
