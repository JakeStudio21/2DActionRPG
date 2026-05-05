using UnityEngine;
using UnityEngine.UI;
using StageSystem;

/// <summary>
/// 출구 탈출 포털 — ObjectiveType.ExitReach 스테이지용
/// 플레이어가 포털에 접촉하면 StageManager.NotifyExitReached()를 호출하여 스테이지 클리어
/// </summary>
public class StageExitPortal : MonoBehaviour
{
    [Header("포털 설정")]
    [Tooltip("처음부터 진입 가능 여부. false면 외부에서 ActivatePortal()을 호출해야 활성화됨.")]
    [SerializeField] private bool activateOnStart = true;

    [Header("비주얼")]
    [SerializeField] private GameObject activeEffect;
    [SerializeField] private GameObject inactiveEffect;
    [SerializeField] private Color activeColor  = new Color(0.3f, 1f, 0.5f, 1f);
    [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("상호작용 UI (선택)")]
    [Tooltip("범위 진입 시 표시할 '탈출' 안내 UI. 없으면 자동 진입 방식으로 동작.")]
    [SerializeField] private GameObject interactHintUI;

    [Header("미니맵")]
    [Tooltip("미니맵에 출구 위치 표시 여부")]
    [SerializeField] private bool showOnMinimap = true;

    // 내부 상태
    private bool isActive = false;
    private bool hasTriggered = false;
    private SpriteRenderer spriteRenderer;
    private MinimapMarker minimapMarker;

    /// <summary>포털 활성화 이벤트 (외부 연출 등에 활용)</summary>
    public System.Action OnPortalActivated;
    /// <summary>포털 진입 이벤트</summary>
    public System.Action OnPortalEntered;

    private void Awake()
    {
        spriteRenderer  = GetComponent<SpriteRenderer>();
        minimapMarker   = GetComponent<MinimapMarker>();
    }

    private void Start()
    {
        SetVisual(false);

        if (interactHintUI != null)
            interactHintUI.SetActive(false);

        if (activateOnStart)
            ActivatePortal();
    }

    // ──────────────────────────────────────────────
    // 공개 API
    // ──────────────────────────────────────────────

    /// <summary>
    /// 포털 활성화 — 외부(StageManager 등)에서 조건 달성 후 열고 싶을 때 호출
    /// </summary>
    public void ActivatePortal()
    {
        if (isActive) return;

        isActive = true;
        SetVisual(true);
        OnPortalActivated?.Invoke();
    }

    // ──────────────────────────────────────────────
    // 플레이어 접촉 감지 (2D Trigger)
    // ──────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive || hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        if (interactHintUI != null)
        {
            // 안내 UI가 있으면 표시 후 플레이어가 버튼을 누를 때 진입
            interactHintUI.SetActive(true);
        }
        else
        {
            // 안내 UI 없으면 접촉 즉시 진입
            EnterPortal();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (interactHintUI != null)
            interactHintUI.SetActive(false);
    }

    /// <summary>
    /// 안내 UI의 버튼에서 호출 — 버튼 방식 진입용
    /// </summary>
    public void OnInteractButtonPressed()
    {
        if (!isActive || hasTriggered) return;
        EnterPortal();
    }

    // ──────────────────────────────────────────────
    // 내부 처리
    // ──────────────────────────────────────────────

    private void EnterPortal()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        if (interactHintUI != null)
            interactHintUI.SetActive(false);

        OnPortalEntered?.Invoke();

        StageManager.Instance?.NotifyExitReached();
    }

    private void SetVisual(bool active)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = active ? activeColor : inactiveColor;

        if (activeEffect   != null) activeEffect.SetActive(active);
        if (inactiveEffect != null) inactiveEffect.SetActive(!active);

        // 미니맵 마커: 활성 시에만 표시
        if (minimapMarker != null)
            minimapMarker.enabled = active && showOnMinimap;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isActive ? new Color(0.3f, 1f, 0.5f, 0.4f) : new Color(0.5f, 0.5f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}
