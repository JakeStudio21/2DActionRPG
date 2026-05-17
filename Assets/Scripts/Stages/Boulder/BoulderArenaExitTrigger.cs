using UnityEngine;

/// <summary>
/// 아레나 탈출 트리거.
/// 플레이어가 이 트리거 영역에 진입하면 연결된 BoulderArenaController에
/// 탈출을 알려 기믹을 종료한다.
///
/// 씬 배치:
///   1. 아레나 출구에 빈 오브젝트 생성
///   2. Collider2D (Is Trigger = true) 추가 — 출구 감지 범위
///   3. Inspector에서 target에 BoulderArenaController 연결
///
/// 예시 구조:
///   [BoulderArenaController] ←→ [ExitTrigger_L] (BoulderArenaExitTrigger → target: controller)
///                            ←→ [ExitTrigger_R] (BoulderArenaExitTrigger → target: controller)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BoulderArenaExitTrigger : MonoBehaviour
{
    [Header("연결 설정")]
    [Tooltip("플레이어 탈출 시 종료를 통보할 BoulderArenaController")]
    [SerializeField] private BoulderArenaController target;

    [Tooltip("플레이어 레이어 마스크")]
    [SerializeField] private LayerMask playerLayer;

    [Tooltip("true: 한 번 작동 후 이 트리거 비활성화 / false: 반복 감지")]
    [SerializeField] private bool disableAfterTrigger = true;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[BoulderArenaExitTrigger] {name}: Collider2D를 자동으로 IsTrigger=true로 설정했습니다.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

        if (target == null)
        {
            Debug.LogWarning($"[BoulderArenaExitTrigger] {name}: target BoulderArenaController가 연결되지 않았습니다.");
            return;
        }

        target.OnPlayerEscape();

        if (disableAfterTrigger)
            gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
        var col = GetComponent<Collider2D>();
        if (col != null)
            Gizmos.DrawCube(transform.position, transform.lossyScale);

        if (target != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.DrawLine(transform.position, target.transform.position);
        }
    }
#endif
}
