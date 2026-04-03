using UnityEngine;

/// <summary>
/// 미션 목표 마커 UI의 비주얼 설정 ScriptableObject
/// 목표 종류(바리케이드 파괴, 아이템 수집, 장치 작동 등)마다 하나씩 에셋을 생성해 사용
/// </summary>
[CreateAssetMenu(fileName = "ObjectiveMarkerConfig", menuName = "Game/Objective/Marker Config")]
public class ObjectiveMarkerConfig : ScriptableObject
{
    [Header("아이콘")]
    [Tooltip("목표 종류를 나타내는 아이콘 스프라이트")]
    public Sprite iconSprite;
    
    [Tooltip("아이콘 색상")]
    public Color iconColor = Color.white;

    [Header("진행 바")]
    [Tooltip("ProgressBar: 0~100% 바 (HP형·작동형)\n" +
             "Count: 현재/목표 숫자 (수집형 '3/5')\n" +
             "StateOnly: 아이콘+라벨만 (바 없음)")]
    public ObjectiveDisplayMode displayMode = ObjectiveDisplayMode.ProgressBar;
    
    [Tooltip("진행 바 채움 색상")]
    public Color barFillColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    
    [Tooltip("진행 바 배경 색상")]
    public Color barBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.85f);

    [Header("텍스트")]
    [Tooltip("고정 라벨 텍스트 (예: '파괴', '수집', '작동')")]
    public string labelText = "목표";
    
    [Tooltip("진행도 수치 텍스트 표시 여부 (80% 또는 3/5)")]
    public bool showValueText = true;

    [Header("위치/크기")]
    [Tooltip("오브젝트 중심에서 위로 띄울 거리 (월드 유닛)")]
    public float verticalOffset = 1.2f;
    
    [Tooltip("마커 전체 크기 배율 (1 = 기본)")]
    [Range(0.5f, 3f)]
    public float markerScale = 1f;

    [Header("펄스 애니메이션")]
    [Tooltip("주의 유도용 펄스 스케일 애니메이션 사용 여부")]
    public bool usePulseAnimation = true;
    
    [Tooltip("펄스 속도 (Hz)")]
    [Range(0.5f, 4f)]
    public float pulseSpeed = 1.5f;
    
    [Tooltip("펄스 스케일 진폭 (0.08 = ±8% 크기 변화)")]
    [Range(0.01f, 0.3f)]
    public float pulseScaleAmount = 0.08f;
}

/// <summary>
/// ObjectiveMarkerUI 진행도 표시 방식
/// </summary>
public enum ObjectiveDisplayMode
{
    ProgressBar, // 0~100% 채움 바 (바리케이드 HP, 장치 작동 진행도)
    Count,       // 현재/목표 형식 (아이템 수집: "3/5")
    StateOnly    // 아이콘 + 라벨만 (바 표시 없음)
}
