using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace StageSystem
{

/// <summary>
/// 스테이지별 Global Volume 효과를 제어하는 컴포넌트.
/// 씬에 하나 배치하고 FogVolume / ClearVolume 두 개의 자식 Global Volume을 연결한다.
///
/// 사용 흐름:
///   1. StageManager.InitializeStage() 완료 시 ApplyStageVolume(config) 호출
///      → fogVolumeProfile 적용, weight=1 (어두운 상태)
///   2. 클리어 판정 시 ApplyClearTransition(duration) 호출
///      → fogVolume weight 1→0, clearVolume weight 0→1 페이드
///
/// StageConfig.fogVolumeProfile 이 null 이면 Volume 효과 없이 스킵된다.
/// </summary>
public class StageVolumeController : MonoBehaviour
{
    public static StageVolumeController Instance { get; private set; }

    [Header("Volume 참조")]
    [Tooltip("스테이지 진행 중 효과를 담당하는 Global Volume (안개·어두운 연출)")]
    [SerializeField] private Volume fogVolume;

    [Tooltip("스테이지 클리어 후 효과를 담당하는 Global Volume (밝아지는 연출)")]
    [SerializeField] private Volume clearVolume;

    private Coroutine transitionCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 초기 상태: 두 Volume 모두 비활성화 (스테이지 설정 전까지 영향 없음)
        if (fogVolume != null) fogVolume.gameObject.SetActive(false);
        if (clearVolume != null) clearVolume.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 스테이지 시작 시 호출.
    /// config.fogVolumeProfile 이 null 이면 Volume 효과 전체를 스킵한다.
    /// </summary>
    public void ApplyStageVolume(StageConfig config)
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        bool hasFog = config != null && config.fogVolumeProfile != null;
        bool hasClear = config != null && config.clearVolumeProfile != null;

        if (fogVolume != null)
        {
            fogVolume.gameObject.SetActive(hasFog);
            if (hasFog)
            {
                fogVolume.profile = config.fogVolumeProfile;
                fogVolume.weight = 1f;
            }
        }

        if (clearVolume != null)
        {
            clearVolume.gameObject.SetActive(hasClear);
            if (hasClear)
            {
                clearVolume.profile = config.clearVolumeProfile;
                clearVolume.weight = 0f;
            }
        }
    }

    /// <summary>
    /// 클리어 판정 시 호출.
    /// fogVolume weight 1→0, clearVolume weight 0→1 을 duration 초에 걸쳐 전환한다.
    /// </summary>
    public void ApplyClearTransition(float duration)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(ClearTransitionCoroutine(duration));
    }

    private IEnumerator ClearTransitionCoroutine(float duration)
    {
        float elapsed = 0f;
        float startFogWeight = (fogVolume != null && fogVolume.gameObject.activeSelf) ? fogVolume.weight : 0f;
        float startClearWeight = (clearVolume != null && clearVolume.gameObject.activeSelf) ? clearVolume.weight : 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            if (fogVolume != null && fogVolume.gameObject.activeSelf)
                fogVolume.weight = Mathf.Lerp(startFogWeight, 0f, t);

            if (clearVolume != null && clearVolume.gameObject.activeSelf)
                clearVolume.weight = Mathf.Lerp(startClearWeight, 1f, t);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (fogVolume != null) fogVolume.weight = 0f;
        if (clearVolume != null) clearVolume.weight = 1f;

        transitionCoroutine = null;
    }

    /// <summary>
    /// 즉시 Volume 효과를 초기화한다 (씬 전환 등 긴급 해제용).
    /// </summary>
    public void ResetVolume()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        if (fogVolume != null) fogVolume.gameObject.SetActive(false);
        if (clearVolume != null) clearVolume.gameObject.SetActive(false);
    }
}

} // namespace StageSystem
