using UnityEngine;

/// <summary>
/// 레이더맵(RadarMapPanel)과 전체 미니맵(MinimapPanel)의 On/Off 토글만 담당한다.
///
/// [원칙] 좌표 계산·마커 관리 등의 로직을 전혀 포함하지 않는다.
///
/// [버튼 연결]
/// - RadarMapPanel의 ExpandButton.onClick → ShowMinimap()
/// - MinimapPanel의 CollapseButton.onClick → ShowRadarMap()
///
/// [Anti-Popping]
/// SetActive(true) 호출 시 Unity가 자동으로 해당 View의 OnEnable()을 호출하며,
/// OnEnable 내부의 ForceRefresh()가 즉시 위치를 갱신하므로 팝핑이 발생하지 않는다.
/// </summary>
public class MiniMapUIManager : MonoBehaviour
{
    public static MiniMapUIManager Instance { get; private set; }

    [Header("패널 참조")]
    [Tooltip("우측 상단 소형 레이더맵 패널")]
    [SerializeField] private GameObject radarMapPanel;

    [Tooltip("화면 중앙 대형 전체 미니맵 패널")]
    [SerializeField] private GameObject minimapPanel;

    [Tooltip("설정 패널 (미니맵 확장 시 열려있으면 자동으로 닫음)")]
    [SerializeField] private SettingsUIController settingsUIController;

    // ────────────────────────────────────────────────────────────────
    //  초기화
    // ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 초기 상태: 레이더맵만 표시
        SetPanelState(showRadar: true);
    }

    // ────────────────────────────────────────────────────────────────
    //  공개 API — 버튼 onClick에 직접 연결
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 전체 미니맵을 닫고 레이더맵을 표시한다.
    /// RadarMapPanel의 CollapseButton 또는 닫기 버튼에 연결한다.
    /// </summary>
    public void ShowRadarMap()
    {
        SetPanelState(showRadar: true);
    }

    /// <summary>
    /// 레이더맵을 닫고 전체 미니맵을 표시한다.
    /// RadarMapPanel의 ExpandButton에 연결한다.
    /// </summary>
    public void ShowMinimap()
    {
        // SettingsPanel이 열려있으면 먼저 닫는다 (패널 겹침 방지)
        if (settingsUIController != null && settingsUIController.gameObject.activeSelf)
            settingsUIController.Close();

        SetPanelState(showRadar: false);
    }

    /// <summary>현재 레이더맵이 활성화되어 있는지 여부.</summary>
    public bool IsRadarMapVisible => radarMapPanel != null && radarMapPanel.activeSelf;

    /// <summary>현재 전체 미니맵이 활성화되어 있는지 여부.</summary>
    public bool IsMinimapVisible => minimapPanel != null && minimapPanel.activeSelf;

    // ────────────────────────────────────────────────────────────────
    //  내부 유틸
    // ────────────────────────────────────────────────────────────────

    private void SetPanelState(bool showRadar)
    {
        if (radarMapPanel != null) radarMapPanel.SetActive(showRadar);
        if (minimapPanel  != null) minimapPanel.SetActive(!showRadar);
    }
}
