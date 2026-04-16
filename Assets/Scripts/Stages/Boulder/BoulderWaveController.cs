using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 여러 BoulderSpawner를 각각 독립적인 타이밍으로 제어하는 마스터 컨트롤러.
///
/// 각 SpawnEntry는 자신만의 초기 딜레이, 반복 횟수, 반복 간격을 가진다.
/// 웨이브 시작 시 모든 엔트리가 독립 코루틴으로 병렬 실행된다.
///
/// 발동 조건 (둘 다 활성화 가능):
///   1. 트리거 모드: 플레이어가 Collider2D 영역에 진입하면 웨이브 시작
///   2. AutoStart 모드: 씬 로드 후 n초 뒤 자동으로 웨이브 시작
///
/// 씬 배치 방법:
///   1. 빈 오브젝트에 이 컴포넌트 추가
///   2. [트리거 모드 사용 시] Collider2D (Is Trigger = true) 함께 추가
///   3. Inspector에서 SpawnEntries 배열에 각 스포너 설정
///   4. 각 BoulderSpawner는 씬에 별도 배치, 자신의 BoulderPathData 연결
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BoulderWaveController : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        [Tooltip("발동시킬 BoulderSpawner 컴포넌트")]
        public BoulderSpawner spawner;

        [Tooltip("웨이브 시작 후 이 스포너의 첫 스폰까지 대기 시간 (초).\n0이면 즉시 발동")]
        [Min(0f)]
        public float delay = 0f;

        [Tooltip("이 스포너의 반복 횟수.\n0 = 무한 반복  /  1 = 1회  /  5 = 5회")]
        [Min(0)]
        public int repeatCount = 1;

        [Tooltip("이 스포너의 반복 간격 (초).\nrepeatCount가 1이면 무시됨")]
        [Min(0f)]
        public float repeatInterval = 5f;
    }

    // ── 스폰 엔트리 ───────────────────────────────────────────────────────────
    [Header("스폰 엔트리")]
    [Tooltip("발동할 BoulderSpawner 목록. 각 항목이 독립적으로 동작한다.")]
    [SerializeField] private SpawnEntry[] spawnEntries = new SpawnEntry[0];

    // ── 트리거 설정 ───────────────────────────────────────────────────────────
    [Header("트리거 설정")]
    [Tooltip("플레이어 레이어 (트리거 모드에서 감지할 레이어)\n트리거 모드를 사용하지 않으면 None으로 두어도 됨")]
    [SerializeField] private LayerMask playerLayer;

    [Tooltip("true = 웨이브가 한 번 시작되면 트리거를 다시 밟아도 재발동 안 함")]
    [SerializeField] private bool oneShot = true;

    // ── 자동 시작 설정 ────────────────────────────────────────────────────────
    [Header("자동 시작 설정")]
    [Tooltip("true = 씬 로드 후 autoStartDelay 초 뒤 자동 웨이브 시작")]
    [SerializeField] private bool autoStart = false;

    [Tooltip("AutoStart까지 대기 시간 (초)")]
    [Min(0f)]
    [SerializeField] private float autoStartDelay = 3f;

    // ── 내부 상태 ─────────────────────────────────────────────────────────────
    private bool hasStarted = false;
    private readonly List<Coroutine> entryCoroutines = new List<Coroutine>();

    // ── Unity 이벤트 ──────────────────────────────────────────────────────────

    private void Start()
    {
        if (autoStart)
            StartCoroutine(AutoStartRoutine());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (playerLayer.value == 0) return;
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;
        if (oneShot && hasStarted) return;

        Debug.Log($"[BoulderWaveController] {name}: 플레이어 진입 감지 → 웨이브 시작");
        StartWave();
    }

    // ── 웨이브 제어 ───────────────────────────────────────────────────────────

    private IEnumerator AutoStartRoutine()
    {
        if (autoStartDelay > 0f)
            yield return new WaitForSeconds(autoStartDelay);

        if (oneShot && hasStarted)
        {
            Debug.Log($"[BoulderWaveController] {name}: AutoStart — 이미 웨이브 시작됨, 스킵");
            yield break;
        }

        Debug.Log($"[BoulderWaveController] {name}: AutoStart 발동 (딜레이: {autoStartDelay}초)");
        StartWave();
    }

    /// <summary>
    /// 웨이브를 시작한다. 모든 SpawnEntry가 독립 코루틴으로 병렬 실행된다.
    /// 외부(이벤트 등)에서도 직접 호출 가능.
    /// </summary>
    public void StartWave()
    {
        if (oneShot && hasStarted)
        {
            Debug.Log($"[BoulderWaveController] {name}: StartWave 무시 — oneShot 이미 발동됨");
            return;
        }

        hasStarted = true;
        entryCoroutines.Clear();

        Debug.Log($"[BoulderWaveController] {name}: 웨이브 시작 — 엔트리 수: {spawnEntries.Length}");

        foreach (var entry in spawnEntries)
        {
            if (entry.spawner == null)
            {
                Debug.LogWarning($"[BoulderWaveController] {name}: SpawnEntry에 null Spawner 발견, 스킵");
                continue;
            }

            Coroutine co = StartCoroutine(EntryRepeatRoutine(entry));
            entryCoroutines.Add(co);
        }
    }

    /// <summary>
    /// 진행 중인 모든 엔트리 코루틴을 즉시 중단한다.
    /// </summary>
    public void StopWave()
    {
        foreach (var co in entryCoroutines)
        {
            if (co != null) StopCoroutine(co);
        }
        entryCoroutines.Clear();
        Debug.Log($"[BoulderWaveController] {name}: 웨이브 중단");
    }

    /// <summary>
    /// 웨이브를 리셋하고 처음부터 다시 발동 가능하게 한다.
    /// </summary>
    public void ResetWave()
    {
        StopWave();
        hasStarted = false;
        Debug.Log($"[BoulderWaveController] {name}: 웨이브 리셋 완료");
    }

    // ── 엔트리별 독립 코루틴 ─────────────────────────────────────────────────

    private IEnumerator EntryRepeatRoutine(SpawnEntry entry)
    {
        // 첫 스폰 전 초기 딜레이
        if (entry.delay > 0f)
        {
            Debug.Log($"[BoulderWaveController] {entry.spawner.name} → 첫 스폰까지 {entry.delay:F1}초 대기");
            yield return new WaitForSeconds(entry.delay);
        }

        int fired = 0;

        while (entry.repeatCount == 0 || fired < entry.repeatCount)
        {
            string countLabel = entry.repeatCount == 0 ? "∞" : $"{fired + 1}/{entry.repeatCount}";
            Debug.Log($"[BoulderWaveController] {entry.spawner.name} → 스폰 ({countLabel})");

            entry.spawner.ForceSpawn();
            fired++;

            if (entry.repeatCount != 0 && fired >= entry.repeatCount)
                break;

            Debug.Log($"[BoulderWaveController] {entry.spawner.name} → 다음 스폰까지 {entry.repeatInterval:F1}초 대기");
            yield return new WaitForSeconds(entry.repeatInterval);
        }

        Debug.Log($"[BoulderWaveController] {entry.spawner.name} → 스폰 완료");
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.25f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.forward,
            col is CircleCollider2D cc ? cc.radius : 1f);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.4f);

        if (spawnEntries == null) return;
        foreach (var entry in spawnEntries)
        {
            if (entry?.spawner == null) continue;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, entry.spawner.transform.position);
            Gizmos.DrawWireSphere(entry.spawner.transform.position, 0.3f);
        }
    }
#endif
}
