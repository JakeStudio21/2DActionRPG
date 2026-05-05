using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기믹 장치 활성화 트리거 영역 (범용).
///
/// 씬 배치 방법:
///   1. 빈 오브젝트에 이 컴포넌트 추가
///   2. Collider2D (Is Trigger = true) 추가
///   3. Inspector에서 traps 배열에 BaseTrap 구현체들을 연결
///
/// ActivationMode 설명:
///   OnEnter      - 플레이어가 진입할 때 1회 Activate (망치, 무너지는 석판 등)
///   WhileInside  - 플레이어가 영역 안에 있는 동안 Activate 유지. 이탈 시 Deactivate (가시, 화염 등)
///
/// 자동 오프셋 분산 (autoDistributeOffset):
///   true: 연결된 트랩 수에 따라 cycleOffset을 균등 분배 → 여러 망치가 엇갈려서 동작
///   false: 각 트랩의 startDelay를 직접 설정하는 수동 모드
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TrapActivationZone : MonoBehaviour
{
    public enum ActivationMode
    {
        OnEnter,     // 진입 시 1회 활성화
        WhileInside, // 영역 안에 있는 동안 활성화 유지
    }

    [Header("트리거 설정")]
    [SerializeField] private ActivationMode activationMode = ActivationMode.OnEnter;

    [Tooltip("true: 한 번 발동 후 이 트리거 영역을 비활성화")]
    [SerializeField] private bool triggerOnce = false;

    [Tooltip("플레이어 레이어 마스크")]
    [SerializeField] private LayerMask playerLayer;

    [Header("연결된 기믹 장치")]
    [Tooltip("이 영역이 제어할 BaseTrap 목록. 여러 기믹을 동시에 활성화 가능.")]
    [SerializeField] private List<BaseTrap> traps = new List<BaseTrap>();

    [Header("오프셋 자동 분산")]
    [Tooltip("true: 연결된 트랩 수에 따라 cycleOffset을 균등 분배\n" +
             "false: 각 트랩의 startDelay를 수동으로 설정")]
    [SerializeField] private bool autoDistributeOffset = true;

    // ── 내부 상태 ─────────────────────────────────────────────────────────────
    private bool hasTriggered = false;
    private bool isPlayerInside = false;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[TrapActivationZone] {name}: Collider2D를 자동으로 IsTrigger=true로 설정했습니다.");
        }
    }

    private void Start()
    {
        if (autoDistributeOffset)
            DistributeOffsets();
    }

    // ── 오프셋 자동 분산 ──────────────────────────────────────────────────────

    private void DistributeOffsets()
    {
        int count = traps.Count;
        if (count <= 1) return;

        for (int i = 0; i < count; i++)
        {
            if (traps[i] == null) continue;
            float offset = (float)i / count;
            traps[i].SetCycleOffset(offset);
        }
    }

    // ── 트리거 감지 ───────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (triggerOnce && hasTriggered) return;

        isPlayerInside = true;

        if (activationMode == ActivationMode.OnEnter ||
            activationMode == ActivationMode.WhileInside)
        {
            hasTriggered = true;
            ActivateAll();

            if (triggerOnce)
                gameObject.SetActive(false);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        isPlayerInside = false;

        if (activationMode == ActivationMode.WhileInside)
            DeactivateAll();
    }

    // ── 기믹 제어 ─────────────────────────────────────────────────────────────

    private void ActivateAll()
    {
        foreach (var trap in traps)
        {
            if (trap != null)
                trap.Activate();
        }
    }

    private void DeactivateAll()
    {
        foreach (var trap in traps)
        {
            if (trap != null)
                trap.Deactivate();
        }
    }

    /// <summary>
    /// 외부(StageManager 등)에서 이 영역을 강제 리셋할 때 사용.
    /// </summary>
    public void ResetZone()
    {
        hasTriggered = false;
        isPlayerInside = false;
        gameObject.SetActive(true);
    }

    private bool IsPlayer(Collider2D other)
    {
        return (playerLayer.value & (1 << other.gameObject.layer)) != 0;
    }

    // ── 에디터 Gizmos ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        bool triggered = Application.isPlaying ? hasTriggered : false;
        Gizmos.color = triggered
            ? new Color(0.5f, 0.5f, 0.5f, 0.2f)
            : new Color(0.2f, 0.8f, 1f, 0.2f);

        if (col is BoxCollider2D box)
        {
            Matrix4x4 m = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = m;
            Gizmos.DrawCube(box.offset, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (col is CircleCollider2D circle)
        {
            Gizmos.DrawSphere(
                transform.position + (Vector3)circle.offset,
                circle.radius * transform.lossyScale.x);
        }

        // 연결된 트랩에 선 표시
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        foreach (var trap in traps)
        {
            if (trap != null)
                Gizmos.DrawLine(transform.position, trap.transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.5f,
            $"Mode: {activationMode}\nTraps: {traps.Count}\nAutoOffset: {autoDistributeOffset}");
    }
#endif
}
