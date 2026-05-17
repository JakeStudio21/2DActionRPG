using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 아레나 구르는 바위 기믹 전용 생존 타이머 UI.
/// StageManager에 의존하지 않고 독자적으로 카운트다운을 수행한다.
///
/// 씬 배치:
///   Canvas (Screen Space - Overlay) 하위에 이 컴포넌트를 배치.
///   BoulderArenaController.timerUI 슬롯에 연결.
///   초기 상태는 비활성화(SetActive false)로 두면 됨.
///
/// 연출 상태:
///   정상 구간  → normalColor  (흰색)
///   경고 구간  → warningColor (노란색, warningThreshold초 미만)
///   위험 구간  → criticalColor (빨간색 + 깜빡임, criticalThreshold초 미만)
///   완료       → UI 자동 숨김
/// </summary>
public class BoulderArenaTimerUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [Tooltip("카운트다운 숫자 텍스트 (MM:SS 형식)")]
    [SerializeField] private TextMeshProUGUI timerText;
    [Tooltip("타이머 배경 이미지 (선택. null이면 배경 색상 변경 없음)")]
    [SerializeField] private Image timerBackground;
    [Tooltip("타이머 레이블 텍스트 (선택. 예: '탈출까지')")]
    [SerializeField] private TextMeshProUGUI labelText;
    [Tooltip("타이머 완료 후 표시할 탈출구 열림 메시지 텍스트\n" +
             "타이머 UI와 별도 오브젝트로 배치 가능.\n" +
             "null이면 메시지 없음.")]
    [SerializeField] private TextMeshProUGUI exitOpenedText;

    [Header("탈출구 열림 메시지 설정")]
    [Tooltip("탈출구 열림 텍스트 내용")]
    [SerializeField] private string exitOpenedMessage = "탈출구 열림!";
    [Tooltip("탈출구 열림 텍스트 색상")]
    [SerializeField] private Color exitOpenedColor = Color.green;
    [Tooltip("탈출구 열림 깜빡임 속도 (초)")]
    [SerializeField] private float exitOpenedBlinkSpeed = 0.4f;

    [Header("경고 연출 설정")]
    [Tooltip("이 시간(초) 미만이면 노란색 경고 상태")]
    [SerializeField] private float warningThreshold = 15f;
    [Tooltip("이 시간(초) 미만이면 빨간색 + 깜빡임 위험 상태")]
    [SerializeField] private float criticalThreshold = 5f;
    [SerializeField] private Color normalColor   = Color.white;
    [SerializeField] private Color warningColor  = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    [Tooltip("깜빡임 한 사이클(페이드 아웃→인) 속도 (초)")]
    [SerializeField] private float blinkSpeed = 0.4f;

    // ── 런타임 상태 ───────────────────────────────────────────────────────────

    private float remainingTime;
    private bool  isActive   = false;
    private bool  isBlinking = false;

    private Coroutine timerCoroutine;
    private Coroutine blinkCoroutine;
    private Coroutine exitOpenedBlinkCoroutine;

    // ── 공개 API ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 타이머를 시작한다. BoulderArenaController.StartGimmick()에서 호출.
    /// </summary>
    /// <param name="duration">생존해야 할 시간 (초)</param>
    public void StartTimer(float duration)
    {
        remainingTime = duration;
        isActive      = true;

        gameObject.SetActive(true);

        // 타이머 UI 표시, 탈출 메시지는 반드시 숨김
        if (timerText       != null) timerText.gameObject.SetActive(true);
        if (labelText       != null) labelText.gameObject.SetActive(true);
        if (timerBackground != null) timerBackground.gameObject.SetActive(true);
        if (exitOpenedText  != null) exitOpenedText.gameObject.SetActive(false);

        ApplyColor(normalColor);

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(TimerRoutine());
    }

    /// <summary>
    /// 타이머를 중단하고 UI를 숨긴다.
    /// BoulderArenaController.ForceEndGimmick() 또는 타이머 완료 후 호출.
    /// </summary>
    public void StopTimer()
    {
        isActive = false;

        if (timerCoroutine != null) { StopCoroutine(timerCoroutine); timerCoroutine = null; }
        StopBlink();
        StopExitOpenedBlink();

        // 탈출구 열림 텍스트 숨김
        if (exitOpenedText  != null) exitOpenedText.gameObject.SetActive(false);

        // 다음 재사용 시 StartTimer()에서 올바르게 표시되도록 자식 상태 복원
        if (timerText       != null) timerText.gameObject.SetActive(true);
        if (labelText       != null) labelText.gameObject.SetActive(true);
        if (timerBackground != null) timerBackground.gameObject.SetActive(true);

        gameObject.SetActive(false);
    }

    // ── 공개 프로퍼티 ─────────────────────────────────────────────────────────

    public float RemainingTime  => remainingTime;
    public bool  IsActive       => isActive;
    public bool  IsInWarning    => isActive && remainingTime <= warningThreshold;
    public bool  IsInCritical   => isActive && remainingTime <= criticalThreshold;

    // ── 타이머 코루틴 ─────────────────────────────────────────────────────────

    private IEnumerator TimerRoutine()
    {
        while (isActive && remainingTime > 0f)
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime < 0f) remainingTime = 0f;

            UpdateDisplay();
            UpdateColorState();

            yield return null;
        }

        // 카운트 완료 → 00:00 표시 후 타이머 UI 숨기고 탈출 메시지 표시
        remainingTime = 0f;
        UpdateDisplay();
        yield return new WaitForSeconds(0.5f);

        // 타이머 텍스트/배경 숨김
        if (timerText   != null) timerText.gameObject.SetActive(false);
        if (labelText   != null) labelText.gameObject.SetActive(false);
        if (timerBackground != null) timerBackground.gameObject.SetActive(false);

        // 탈출구 열림 메시지 표시 + 깜빡임
        ShowExitOpenedMessage();
    }

    // ── 디스플레이 갱신 ───────────────────────────────────────────────────────

    private void UpdateDisplay()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.CeilToInt(remainingTime % 60f);

        // CeilToInt 사용 시 60초가 되는 경계값 보정
        if (seconds == 60) { minutes++; seconds = 0; }

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdateColorState()
    {
        if (remainingTime <= criticalThreshold)
        {
            ApplyColor(criticalColor);
            if (!isBlinking) StartBlink();
        }
        else if (remainingTime <= warningThreshold)
        {
            ApplyColor(warningColor);
            StopBlink();
        }
        else
        {
            ApplyColor(normalColor);
            StopBlink();
        }
    }

    private void ApplyColor(Color color)
    {
        if (timerText != null)
            timerText.color = color;

        if (timerBackground != null)
            timerBackground.color = new Color(color.r, color.g, color.b, 0.3f);

        if (labelText != null)
            labelText.color = color;
    }

    // ── 깜빡임 처리 ───────────────────────────────────────────────────────────

    private void StartBlink()
    {
        if (isBlinking) return;
        isBlinking   = true;
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private void StopBlink()
    {
        if (!isBlinking) return;
        isBlinking = false;

        if (blinkCoroutine != null) { StopCoroutine(blinkCoroutine); blinkCoroutine = null; }

        // 알파값 복구
        SetTextAlpha(1f);
    }

    private IEnumerator BlinkRoutine()
    {
        while (isBlinking)
        {
            yield return StartCoroutine(FadeAlpha(1f, 0.2f, blinkSpeed));
            yield return StartCoroutine(FadeAlpha(0.2f, 1f, blinkSpeed));
        }
    }

    private IEnumerator FadeAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetTextAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
    }

    private void SetTextAlpha(float alpha)
    {
        if (timerText != null)
        {
            Color c = timerText.color;
            c.a = alpha;
            timerText.color = c;
        }

        if (labelText != null)
        {
            Color c = labelText.color;
            c.a = alpha;
            labelText.color = c;
        }
    }

    // ── 탈출구 열림 메시지 ────────────────────────────────────────────────────

    /// <summary>
    /// 타이머 완료 후 탈출구 열림 메시지를 표시하고 깜빡임을 시작한다.
    /// </summary>
    private void ShowExitOpenedMessage()
    {
        if (exitOpenedText == null) return;

        exitOpenedText.text  = exitOpenedMessage;
        exitOpenedText.color = exitOpenedColor;
        exitOpenedText.gameObject.SetActive(true);

        StopExitOpenedBlink();
        exitOpenedBlinkCoroutine = StartCoroutine(ExitOpenedBlinkRoutine());
    }

    private void StopExitOpenedBlink()
    {
        if (exitOpenedBlinkCoroutine != null)
        {
            StopCoroutine(exitOpenedBlinkCoroutine);
            exitOpenedBlinkCoroutine = null;
        }
        // SetActive(false)는 여기서 하지 않는다.
        // 텍스트 숨김은 StopTimer()에서만 처리한다.
    }

    private IEnumerator ExitOpenedBlinkRoutine()
    {
        while (true)
        {
            yield return StartCoroutine(FadeExitTextAlpha(1f, 0.1f, exitOpenedBlinkSpeed));
            yield return StartCoroutine(FadeExitTextAlpha(0.1f, 1f, exitOpenedBlinkSpeed));
        }
    }

    private IEnumerator FadeExitTextAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / duration);

            if (exitOpenedText != null)
            {
                Color c = exitOpenedText.color;
                c.a = exitOpenedText.color.a;
                c   = new Color(c.r, c.g, c.b, alpha);
                exitOpenedText.color = c;
            }

            yield return null;
        }
    }
}
