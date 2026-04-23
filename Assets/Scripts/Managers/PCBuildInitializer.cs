using UnityEngine;

/// <summary>
/// PC 빌드 전용 초기화 스크립트
///
/// ■ 담당
///   - 화면 해상도 / 창 모드 설정 (1920×1080 Windowed 기본값)
///   - 타겟 프레임레이트 설정
///   - 커서 표시
///
/// ■ 배치
///   Managers 프리팹에 컴포넌트로 추가 (DontDestroyOnLoad 오브젝트)
///   → 로비 씬 포함 게임 전체에서 단 한 번만 초기화
///
/// ■ 동작 조건
///   UNITY_STANDALONE 빌드에서만 실행 (에디터에서는 적용 안 함)
/// </summary>
public class PCBuildInitializer : MonoBehaviour
{
    [Header("화면 해상도")]
    [Tooltip("PC 기본 해상도 가로 (픽셀)")]
    [SerializeField] private int defaultWidth = 1920;

    [Tooltip("PC 기본 해상도 세로 (픽셀)")]
    [SerializeField] private int defaultHeight = 1080;

    [Tooltip("창 모드 (Windowed = 창, FullScreenWindow = 전체화면 창, ExclusiveFullScreen = 전용 전체화면)")]
    [SerializeField] private FullScreenMode screenMode = FullScreenMode.FullScreenWindow;

    [Header("성능")]
    [Tooltip("목표 프레임레이트 (-1 = 무제한)")]
    [SerializeField] private int targetFrameRate = 60;

    [Header("커서")]
    [Tooltip("게임 시작 시 커서를 표시할지 여부")]
    [SerializeField] private bool showCursor = true;

    private void Awake()
    {
#if UNITY_STANDALONE && !UNITY_EDITOR
        ApplyScreenSettings();
        ApplyPerformanceSettings();
        ApplyCursorSettings();
#endif
    }

    // ─────────────────────────────────────────────────────────────

    private void ApplyScreenSettings()
    {
        // 이미 원하는 해상도라면 재설정 스킵 (불필요한 깜빡임 방지)
        if (Screen.width  == defaultWidth  &&
            Screen.height == defaultHeight &&
            Screen.fullScreenMode == screenMode)
            return;

        Screen.SetResolution(defaultWidth, defaultHeight, screenMode);
    }

    private void ApplyPerformanceSettings()
    {
        Application.targetFrameRate = targetFrameRate;

        // VSync: FullScreenWindow 에서 자동 VSync 되므로 수동 설정
        // 0 = VSync 끔, 1 = 모니터 주사율 동기화
        QualitySettings.vSyncCount = (screenMode == FullScreenMode.FullScreenWindow) ? 1 : 0;
    }

    private void ApplyCursorSettings()
    {
        Cursor.visible   = showCursor;
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
