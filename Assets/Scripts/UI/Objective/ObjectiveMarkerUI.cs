using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 미션 목표 오브젝트 위에 표시되는 범용 마커 UI
///
/// [사용법]
///   1. 미션 목표 오브젝트의 자식에 World Space Canvas + 이 컴포넌트 부착
///   2. 인스펙터에서 UI 요소 연결 (iconImage, barFillImage 등)
///   3. 호스트 오브젝트(Barricade 등)에서:
///      - Initialize(config)        : 시작 시 설정 적용 및 활성화
///      - SetProgress(0f ~ 1f)      : HP형·작동형 진행도 업데이트
///      - SetCount(current, total)  : 수집형 카운트 업데이트
///      - ShowComplete()            : 완료 연출 후 자동 비활성화
///      - Hide()                    : 즉시 비활성화
/// </summary>
public class ObjectiveMarkerUI : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────
    [Header("UI 요소")]
    [SerializeField] private Image       iconImage;
    [SerializeField] private GameObject  barRoot;
    [SerializeField] private Image       barBackgroundImage;
    [SerializeField] private Image       barFillImage;
    [SerializeField] private TMP_Text    labelText;
    [SerializeField] private TMP_Text    valueText;

    // ── 런타임 ────────────────────────────────────────────────────
    private ObjectiveMarkerConfig config;
    private Coroutine             pulseCoroutine;
    private Vector3               baseScale;          // 애니메이션 기준 스케일 (prefabScale * markerScale)
    private Vector3               prefabScale;        // 프리팹 원본 스케일 (0.01, 0.01, 0.01 등)
    private CanvasGroup           canvasGroup;
    private bool                  isInitialized;
    private bool                  isComponentsReady;  // EnsureReady 중복 실행 방지 guard

    // ────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ────────────────────────────────────────────────────────────

    private void Awake()
    {
        EnsureReady();
    }

    /// <summary>
    /// 컴포넌트 참조와 프리팹 원본 스케일을 초기화한다.
    /// Awake와 Initialize 양쪽에서 호출되며, guard 플래그로 한 번만 실행된다.
    ///
    /// [이 메서드가 필요한 이유]
    /// 비활성 GameObject는 Awake가 실행되지 않으므로,
    /// 비활성 상태에서 Initialize()가 호출될 때 canvasGroup이 null이 되어
    /// NullReferenceException으로 SetActive(true)까지 도달하지 못하는 문제를 방지한다.
    /// </summary>
    private void EnsureReady()
    {
        if (isComponentsReady) return;
        isComponentsReady = true;

        prefabScale = transform.localScale; // 프리팹에 설정된 원본 스케일 저장 (0.01 등)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    // ────────────────────────────────────────────────────────────
    //  공개 메서드 (호스트 오브젝트에서 호출)
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// ObjectiveMarkerConfig를 기반으로 마커를 초기화하고 활성화한다.
    /// isVictoryTarget=true인 오브젝트의 Start()에서 호출.
    /// </summary>
    public void Initialize(ObjectiveMarkerConfig markerConfig)
    {
        EnsureReady(); // 비활성 상태에서 호출 시 Awake 미실행 대비

        config = markerConfig;
        if (config == null)
        {
            Debug.LogWarning($"[ObjectiveMarkerUI] {gameObject.name}: Config가 null입니다.");
            return;
        }

        ApplyConfig();

        canvasGroup.alpha = 1f;
        isInitialized     = true;
        gameObject.SetActive(true);

        if (config.usePulseAnimation)
            pulseCoroutine = StartCoroutine(PulseRoutine());

        Debug.Log($"[ObjectiveMarkerUI] Initialize 완료 - pos:{transform.localPosition} scale:{transform.localScale} active:{gameObject.activeSelf}");
    }

    /// <summary>
    /// 0~1 비율로 진행도 바를 업데이트한다. (ProgressBar 모드)
    /// 바리케이드 HP, 장치 작동 진행도 등에 사용.
    /// </summary>
    public void SetProgress(float progress)
    {
        if (!isInitialized) return;
        progress = Mathf.Clamp01(progress);

        if (barFillImage != null)
            barFillImage.fillAmount = progress;

        if (valueText != null && config.showValueText)
            valueText.text = $"{Mathf.RoundToInt(progress * 100)}%";
    }

    /// <summary>
    /// 현재/목표 카운트로 진행도를 업데이트한다. (Count 모드)
    /// 아이템 수집형에서 사용 (예: "3/5").
    /// </summary>
    public void SetCount(int current, int total)
    {
        if (!isInitialized) return;

        float progress = total > 0 ? (float)current / total : 0f;

        if (barFillImage != null)
            barFillImage.fillAmount = progress;

        if (valueText != null)
            valueText.text = $"{current}/{total}";
    }

    /// <summary>
    /// 목표 완료 시 호출. 스케일 업 → 페이드 아웃 연출 후 자동 비활성화.
    /// </summary>
    public void ShowComplete()
    {
        if (!isInitialized) return;
        StopPulse();
        StartCoroutine(CompleteRoutine());
    }

    /// <summary>
    /// 즉시 마커를 숨긴다. 오브젝트 파괴·씬 전환 시 사용.
    /// </summary>
    public void Hide()
    {
        StopPulse();
        gameObject.SetActive(false);
    }

    // ────────────────────────────────────────────────────────────
    //  내부 로직
    // ────────────────────────────────────────────────────────────

    private void ApplyConfig()
    {
        // 아이콘
        if (iconImage != null)
        {
            iconImage.sprite = config.iconSprite;
            iconImage.color  = config.iconColor;
        }

        // 바 색상
        if (barFillImage != null)
            barFillImage.color = config.barFillColor;

        if (barBackgroundImage != null)
            barBackgroundImage.color = config.barBackgroundColor;

        // 라벨
        if (labelText != null)
            labelText.text = config.labelText;

        // 초기 진행도 (100%)
        if (barFillImage != null)
            barFillImage.fillAmount = 1f;

        // 초기 수치 텍스트
        if (valueText != null)
            valueText.text = config.displayMode == ObjectiveDisplayMode.Count ? "" : "100%";

        // 바 표시 여부 (StateOnly이면 숨김)
        if (barRoot != null)
            barRoot.SetActive(config.displayMode != ObjectiveDisplayMode.StateOnly);

        // 위치 오프셋
        transform.localPosition = new Vector3(0f, config.verticalOffset, 0f);
        // prefabScale(0.01 등 프리팹 원본) × markerScale(config 배율) = 최종 기준 스케일
        // Vector3.one으로 덮어쓰면 Canvas가 100배로 커져 화면 밖으로 나가므로 반드시 곱셈 사용
        baseScale            = prefabScale * config.markerScale;
        transform.localScale = baseScale;
    }

    private void StopPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine    = null;
            transform.localScale = baseScale;
        }
    }

    // ────────────────────────────────────────────────────────────
    //  코루틴
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// 사인 곡선 기반 펄스 스케일 애니메이션 (주의 유도)
    /// </summary>
    private IEnumerator PulseRoutine()
    {
        while (true)
        {
            float t     = (Mathf.Sin(Time.time * config.pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            float scale = 1f + Mathf.Lerp(-config.pulseScaleAmount, config.pulseScaleAmount, t);
            transform.localScale = baseScale * scale;
            yield return null;
        }
    }

    /// <summary>
    /// 완료 연출: 스케일 업 → 페이드 아웃 → 비활성화
    /// </summary>
    private IEnumerator CompleteRoutine()
    {
        const float halfDuration = 0.25f;
        float elapsed = 0f;

        // 1단계: 스케일 업
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.localScale = baseScale * Mathf.Lerp(1f, 1.5f, t);
            yield return null;
        }

        // 2단계: 스케일 다운 + 페이드 아웃
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed         += Time.deltaTime;
            float t          = elapsed / halfDuration;
            transform.localScale = baseScale * Mathf.Lerp(1.5f, 0f, t);
            canvasGroup.alpha    = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}
