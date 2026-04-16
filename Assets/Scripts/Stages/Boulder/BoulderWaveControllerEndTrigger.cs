using UnityEngine;

/// <summary>
/// BoulderWaveController 전용 종료 트리거.
/// 플레이어가 이 트리거 영역에 진입하면 연결된 BoulderWaveController의 웨이브를 중지한다.
///
/// 씬 배치:
///   1. 빈 오브젝트(WaveTriggerEnd 등)에 이 컴포넌트 추가
///   2. Collider2D (Is Trigger = true) 추가 — 종료 감지 범위
///   3. Inspector에서 target에 제어할 BoulderWaveController 연결
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BoulderWaveControllerEndTrigger : MonoBehaviour
{
    [Header("연결 설정")]
    [Tooltip("플레이어 진입 시 중지할 BoulderWaveController")]
    [SerializeField] private BoulderWaveController target;

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
            Debug.LogWarning($"[BoulderWaveControllerEndTrigger] {name}: Collider2D를 자동으로 IsTrigger=true로 설정했습니다.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;

        if (target == null)
        {
            Debug.LogWarning($"[BoulderWaveControllerEndTrigger] {name}: target BoulderWaveController가 연결되지 않았습니다.");
            return;
        }

        target.StopWave();
        Debug.Log($"[BoulderWaveControllerEndTrigger] {name}: {target.name} 웨이브 중지 신호 전달");

        if (disableAfterTrigger)
            gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        var col = GetComponent<Collider2D>();
        if (col != null)
            Gizmos.DrawCube(transform.position, transform.lossyScale);

        if (target != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawLine(transform.position, target.transform.position);
        }
    }
#endif
}
