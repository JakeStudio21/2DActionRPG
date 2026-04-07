using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 발밑 공격 방향 마커
/// - 공격 시작 시 공격 방향으로 회전하며 페이드 인
/// - 공격 종료 시 페이드 아웃
/// - PlayerController.LockAnimationDirection / UnlockAnimationDirection 에서 호출됨
/// </summary>
public class AttackDirectionMarker : MonoBehaviour
{
    [Header("페이드 설정")]
    [SerializeField] private float fadeInDuration = 0.08f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] [Range(0f, 1f)] private float maxAlpha = 0.75f;

    [Header("스케일 펄스")]
    [SerializeField] private bool usePulse = true;
    [SerializeField] private float pulseScale = 1.15f;
    [SerializeField] private float pulseDuration = 0.06f;

    private SpriteRenderer spriteRenderer;
    private Coroutine activeCoroutine;
    private Vector3 baseScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 초기 상태: 완전 투명
        if (spriteRenderer != null)
        {
            var c = spriteRenderer.color;
            c.a = 0f;
            spriteRenderer.color = c;
        }

        baseScale = transform.localScale;
    }

    /// <summary>
    /// 공격 방향으로 마커를 표시합니다. PlayerController.LockAnimationDirection()에서 호출.
    /// </summary>
    public void Show(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f) return;

        // 방향에 따라 마커 회전 (스프라이트의 위쪽이 기본 방향이라 가정)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        activeCoroutine = StartCoroutine(ShowRoutine());
    }

    /// <summary>
    /// 마커를 숨깁니다. PlayerController.UnlockAnimationDirection()에서 호출.
    /// </summary>
    public void Hide()
    {
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        activeCoroutine = StartCoroutine(HideRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        // 펄스: 살짝 커졌다 원래 크기로
        if (usePulse)
        {
            transform.localScale = baseScale * pulseScale;
            float elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pulseDuration;
                transform.localScale = Vector3.Lerp(baseScale * pulseScale, baseScale, t);
                yield return null;
            }
            transform.localScale = baseScale;
        }

        // 페이드 인
        yield return FadeAlpha(spriteRenderer.color.a, maxAlpha, fadeInDuration);
    }

    private IEnumerator HideRoutine()
    {
        // 페이드 아웃
        yield return FadeAlpha(spriteRenderer.color.a, 0f, fadeOutDuration);
        transform.localScale = baseScale;
    }

    private IEnumerator FadeAlpha(float from, float to, float duration)
    {
        if (spriteRenderer == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            var c = spriteRenderer.color;
            c.a = Mathf.Lerp(from, to, t);
            spriteRenderer.color = c;
            yield return null;
        }

        var finalColor = spriteRenderer.color;
        finalColor.a = to;
        spriteRenderer.color = finalColor;
    }
}
