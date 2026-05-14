using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 설정 패널을 어디서 열었는지 나타내는 컨텍스트.
/// Close() 시 동작(timeScale 복구 여부 / PauseMenu 복귀 여부)을 결정한다.
/// </summary>
public enum SettingsOpenMode
{
    FromHUD,        // HUD 설정 버튼에서 직접 열림 → 닫을 때 timeScale=1, 게임 즉시 복귀
    FromPauseMenu,  // PauseMenu 설정 버튼에서 열림 → 닫을 때 OnClosed 이벤트, PauseMenu 복귀
}

/// <summary>
/// 게임 환경 설정 패널 통합 컨트롤러
///
/// ■ 책임 범위
///   - 설정값 저장/불러오기 (PlayerPrefs)
///   - 슬라이더 ↔ 매니저 실시간 동기화
///   - 패널 자신(gameObject)의 활성/비활성
///   - OpenMode에 따른 닫힘 후 처리 분기
///     · FromHUD       → timeScale=1 복구 후 게임 복귀 (자기 완결)
///     · FromPauseMenu → OnClosed 이벤트 발행 → PauseMenuController가 복귀 처리
///
/// ■ 확장 가이드 (향후 BGM/SFX 볼륨 등 추가 시)
///   1. 슬라이더 SerializeField 필드 추가
///   2. InitializeSliders()에 SetupSlider 라인 추가
///   3. OnXxxSliderValueChanged 콜백 추가
///   4. SaveSettings() / LoadSettings()에 PlayerPrefs 키 추가
///
/// ■ PlayerPrefs 키 규칙: "Settings.<항목명>"
/// </summary>
public class SettingsUIController : MonoBehaviour
{
    // ───────────────────────────────────────────
    //  Inspector 슬롯
    // ───────────────────────────────────────────

    [Header("📳 화면 흔들림")]
    [Tooltip("화면 흔들림 강도 슬라이더 (0 ~ 1)")]
    [SerializeField] private Slider screenShakeSlider;
    [Tooltip("현재 값을 표시할 레이블 (선택)")]
    [SerializeField] private TMP_Text screenShakeValueLabel;

    [Header("🔊 BGM 볼륨")]
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private TMP_Text bgmValueLabel;

    [Header("🔔 SFX 볼륨")]
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text sfxValueLabel;

    [Header("🔧 버튼")]
    [SerializeField] private Button closeButton;

    // ───────────────────────────────────────────
    //  이벤트 / 상태
    // ───────────────────────────────────────────

    /// <summary>
    /// FromPauseMenu 모드로 닫힐 때 발행.
    /// PauseMenuController가 구독하여 PauseMenuPanel 복귀를 처리한다.
    /// </summary>
    public event Action OnClosed;

    private SettingsOpenMode _currentOpenMode = SettingsOpenMode.FromHUD;

    /// <summary>설정 패널이 현재 열려있는지 여부 (InGamePCInputHandler · MiniMapUIManager 에서 참조)</summary>
    public bool IsOpen => gameObject.activeSelf;

    // ───────────────────────────────────────────
    //  PlayerPrefs 키 상수
    // ───────────────────────────────────────────

    private const string KEY_SCREEN_SHAKE = "Settings.ScreenShake";
    private const string KEY_BGM_VOLUME   = "Settings.BGMVolume";
    private const string KEY_SFX_VOLUME   = "Settings.SFXVolume";

    private const float DEFAULT_SCREEN_SHAKE = 1.0f;
    private const float DEFAULT_BGM_VOLUME   = 1.0f;
    private const float DEFAULT_SFX_VOLUME   = 1.0f;

    // LoadSettings에서 읽은 값 캐시 (슬라이더 초기화 전 보존)
    private float _loadedShakeValue = DEFAULT_SCREEN_SHAKE;
    private float _loadedBGMValue   = DEFAULT_BGM_VOLUME;
    private float _loadedSFXValue   = DEFAULT_SFX_VOLUME;

    // ───────────────────────────────────────────
    //  Unity Lifecycle
    // ───────────────────────────────────────────

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        // 저장된 값 불러오기 → 매니저 즉시 반영 → 슬라이더 초기화
        LoadSettings();
        InitializeSliders();

        // 이 컴포넌트가 부착된 GameObject가 곧 SettingsPanel
        gameObject.SetActive(false);
    }

    // ───────────────────────────────────────────
    //  공개 API
    // ───────────────────────────────────────────

    /// <summary>
    /// 설정 패널 열기.
    /// <param name="mode">
    ///   FromHUD        — HUD에서 직접 열림. 자체적으로 timeScale=0 처리.
    ///   FromPauseMenu  — PauseMenu에서 열림. timeScale은 이미 0이므로 건드리지 않음.
    /// </param>
    /// </summary>
    public void Open(SettingsOpenMode mode = SettingsOpenMode.FromHUD)
    {
        _currentOpenMode = mode;

        // MinimapPanel이 열려있으면 레이더맵으로 되돌린다 (패널 겹침 방지)
        if (MiniMapUIManager.Instance != null && MiniMapUIManager.Instance.IsMinimapVisible)
            MiniMapUIManager.Instance.ShowRadarMap();

        if (mode == SettingsOpenMode.FromHUD)
            Time.timeScale = 0f;

        gameObject.SetActive(true);
        SyncSlidersFromManagers();
    }

    /// <summary>
    /// 설정 패널 닫기 + 자동 저장.
    /// · FromHUD       → timeScale=1 복구 후 게임 즉시 복귀 (자기 완결)
    /// · FromPauseMenu → OnClosed 이벤트 발행 → PauseMenuController가 PauseMenu 복귀 처리
    /// </summary>
    public void Close()
    {
        SaveSettings();
        gameObject.SetActive(false);

        if (_currentOpenMode == SettingsOpenMode.FromHUD)
        {
            Time.timeScale = 1f;
        }
        else // FromPauseMenu
        {
            OnClosed?.Invoke();
        }
    }

    // ───────────────────────────────────────────
    //  슬라이더 OnValueChanged 콜백
    // ───────────────────────────────────────────

    /// <summary>화면 흔들림 슬라이더 값 변경 → ScreenShakeManager 즉시 반영</summary>
    public void OnShakeSliderValueChanged(float value)
    {
        if (ScreenShakeManager.Instance != null)
            ScreenShakeManager.Instance.SetGlobalMultiplier(value);

        UpdateLabel(screenShakeValueLabel, value);
    }

    /// <summary>BGM 볼륨 슬라이더 값 변경 → AudioMixer BGM 그룹 실시간 반영</summary>
    public void OnBGMSliderValueChanged(float value)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetBGMVolume(value);

        UpdateLabel(bgmValueLabel, value);
    }

    /// <summary>SFX 볼륨 슬라이더 값 변경 → AudioMixer SFX 그룹 실시간 반영</summary>
    public void OnSFXSliderValueChanged(float value)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetSFXVolume(value);

        UpdateLabel(sfxValueLabel, value);
    }

    // ───────────────────────────────────────────
    //  Internal
    // ───────────────────────────────────────────

    private void LoadSettings()
    {
        _loadedShakeValue = PlayerPrefs.GetFloat(KEY_SCREEN_SHAKE, DEFAULT_SCREEN_SHAKE);
        _loadedBGMValue   = PlayerPrefs.GetFloat(KEY_BGM_VOLUME,   DEFAULT_BGM_VOLUME);
        _loadedSFXValue   = PlayerPrefs.GetFloat(KEY_SFX_VOLUME,   DEFAULT_SFX_VOLUME);

        if (ScreenShakeManager.Instance != null)
            ScreenShakeManager.Instance.SetGlobalMultiplier(_loadedShakeValue);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetBGMVolume(_loadedBGMValue);
            SoundManager.Instance.SetSFXVolume(_loadedSFXValue);
        }
    }

    private void InitializeSliders()
    {
        SetupSlider(screenShakeSlider, _loadedShakeValue, OnShakeSliderValueChanged, screenShakeValueLabel);
        SetupSlider(bgmVolumeSlider,   _loadedBGMValue,   OnBGMSliderValueChanged,   bgmValueLabel);
        SetupSlider(sfxVolumeSlider,   _loadedSFXValue,   OnSFXSliderValueChanged,   sfxValueLabel);
    }

    /// <summary>
    /// 슬라이더 공통 초기화 헬퍼.
    /// 값 설정 후 리스너를 등록해야 초기화 시 콜백이 중복 호출되지 않는다.
    /// </summary>
    private void SetupSlider(Slider slider, float initialValue,
                             UnityEngine.Events.UnityAction<float> callback, TMP_Text label)
    {
        if (slider == null) return;

        slider.onValueChanged.RemoveAllListeners();
        slider.value = initialValue;
        UpdateLabel(label, initialValue);
        slider.onValueChanged.AddListener(callback);
    }

    /// <summary>패널을 열 때 저장된 값으로 슬라이더를 갱신한다.</summary>
    private void SyncSlidersFromManagers()
    {
        if (screenShakeSlider != null && ScreenShakeManager.Instance != null)
        {
            float v = ScreenShakeManager.Instance.GetGlobalMultiplier();
            screenShakeSlider.SetValueWithoutNotify(v);
            UpdateLabel(screenShakeValueLabel, v);
        }

        if (bgmVolumeSlider != null)
        {
            float v = PlayerPrefs.GetFloat(KEY_BGM_VOLUME, DEFAULT_BGM_VOLUME);
            bgmVolumeSlider.SetValueWithoutNotify(v);
            UpdateLabel(bgmValueLabel, v);
        }

        if (sfxVolumeSlider != null)
        {
            float v = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, DEFAULT_SFX_VOLUME);
            sfxVolumeSlider.SetValueWithoutNotify(v);
            UpdateLabel(sfxValueLabel, v);
        }
    }

    private void SaveSettings()
    {
        if (screenShakeSlider != null)
            PlayerPrefs.SetFloat(KEY_SCREEN_SHAKE, screenShakeSlider.value);

        if (bgmVolumeSlider != null)
            PlayerPrefs.SetFloat(KEY_BGM_VOLUME, bgmVolumeSlider.value);

        if (sfxVolumeSlider != null)
            PlayerPrefs.SetFloat(KEY_SFX_VOLUME, sfxVolumeSlider.value);

        PlayerPrefs.Save();
    }

    private void UpdateLabel(TMP_Text label, float value)
    {
        if (label != null)
            label.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
