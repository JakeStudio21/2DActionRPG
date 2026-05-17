using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아레나 구르는 바위 기믹 오케스트레이터.
///
/// 동작 흐름:
///   플레이어가 진입 트리거(이 오브젝트의 Collider2D)에 진입
///   → [Feature 1] ArenaBarrier 스폰 (플레이어 이동 봉쇄)
///   → [Feature 2] EntryGate 스폰 (입구 차단)
///   → 아레나 내 랜덤 위치에 Large 바위 boulderCount개 스폰
///   → [Feature 3] survivalDuration 타이머 시작 → 완료 시 출구 활성화
///   → 플레이어가 BoulderArenaExitTrigger를 통과 → OnPlayerEscape() 호출
///   → 남은 바위 + 장벽 + 문 전부 정리 → 기믹 종료
///
/// Feature 1 — 아레나 장벽:
///   ArenaBarrier 프리팹은 어떤 Collider2D 형태든 사용 가능 (Circle, Box, Polygon).
///   바위 반사 경계(arenaRadius)와 독립적으로 동작하므로,
///   바위 영역보다 작은 영역으로 설정하면 플레이어가 원 가장자리로 도망 불가.
///
/// Feature 2 — 진입문:
///   entryGateSpawnPoint 위치에 문을 스폰해 입구를 물리적으로 차단.
///   기믹 종료 시 자동 Destroy.
///
/// Feature 3 — 생존 타이머 + 출구문:
///   survivalDuration > 0 이면 타이머 완료 후 출구가 열린다.
///   useStageTimer = true 이면 자체 타이머 대신 기존 StageTimerUI를 사용 (생존 미션 연동).
///   survivalDuration = 0 이면 진입 즉시 출구를 활성화한다.
///
/// 바위 소멸 추적:
///   BoulderArenaRolling은 소멸 시 OnBoulderDestroyed()를 호출
///   Large 바위 분열 시 SpawnSmallBoulders()에서 RegisterBoulder()로 Small 등록
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BoulderArenaController : MonoBehaviour
{
    // ── 아레나 설정 ───────────────────────────────────────────────────────────

    [Header("🎯 아레나 설정")]
    [Tooltip("아레나 중심 Transform.\n" +
             "비워두면 이 오브젝트 위치를 중심으로 사용.")]
    [SerializeField] private Transform arenaCenterTransform;
    [Tooltip("바위 반사에 사용되는 원형 아레나 반경.\n" +
             "플레이어 이동 제한 영역(ArenaBarrier)과는 독립적.")]
    [SerializeField] private float arenaRadius = 10f;

    // ── 스폰 설정 ─────────────────────────────────────────────────────────────

    [Header("🪨 스폰 설정")]
    [Tooltip("Large 바위 프리팹 (BoulderArenaRolling 컴포넌트 필수)")]
    [SerializeField] private GameObject largeBoulderPrefab;
    [Tooltip("동시 스폰할 Large 바위 수")]
    [Range(1, 6)]
    [SerializeField] private int boulderCount = 2;
    [Tooltip("바위 초기 속도")]
    [SerializeField] private float initialSpeed = 5f;
    [Tooltip("플레이어 진입 후 첫 바위 등장까지 딜레이 (초)")]
    [SerializeField] private float spawnDelay = 0.5f;
    [Tooltip("스폰 위치 생성 시 플레이어와의 최소 거리 (즉시 피격 방지)")]
    [SerializeField] private float minSpawnDistFromPlayer = 3f;
    [Tooltip("스폰 위치 생성 재시도 횟수")]
    [SerializeField] private int spawnRetryCount = 8;

    // ── Feature 1: 아레나 장벽 ───────────────────────────────────────────────

    [Header("🧱 Feature 1 — 아레나 장벽")]
    [Tooltip("플레이어 이동을 제한할 장벽 프리팹.\n" +
             "Collider2D 형태(Circle/Box/Polygon) 자유롭게 사용 가능.\n" +
             "null이면 장벽 스폰 없음.")]
    [SerializeField] private GameObject arenaBarrierPrefab;

    // ── Feature 2: 진입문 ─────────────────────────────────────────────────────

    [Header("🚪 Feature 2 — 진입문")]
    [Tooltip("진입 후 입구를 막는 문 프리팹.\n" +
             "null이면 진입문 스폰 없음.")]
    [SerializeField] private GameObject entryGatePrefab;
    [Tooltip("진입문 스폰 위치.\n" +
             "null이면 이 오브젝트 위치에 스폰.")]
    [SerializeField] private Transform entryGateSpawnPoint;

    // ── Feature 3: 생존 타이머 + 출구문 ──────────────────────────────────────

    [Header("⏱️ Feature 3 — 생존 타이머 + 출구문")]
    [Tooltip("출구가 열리기까지 생존해야 할 시간 (초).\n" +
             "0이면 진입 즉시 출구를 활성화.")]
    [SerializeField] private float survivalDuration = 30f;
    [Tooltip("true: BoulderArenaTimerUI 대신 기존 StageTimerUI를 사용 (생존 미션 연동).\n" +
             "false: 자체 타이머 UI 사용.")]
    [SerializeField] private bool useStageTimer = false;
    [Tooltip("생존 타이머를 표시할 UI 컴포넌트 (Screen Overlay HUD).\n" +
             "useStageTimer = true이면 사용하지 않음.")]
    [SerializeField] private BoulderArenaTimerUI timerUI;
    [Tooltip("진입 즉시 출구를 막는 문 프리팹.\n" +
             "타이머 완료 시 ArenaBarrier, EntryGate와 함께 제거된다.\n" +
             "null이면 출구문 없이 ExitTrigger만 비활성 상태로 대기.")]
    [SerializeField] private GameObject exitDoorPrefab;
    [Tooltip("출구문 스폰 위치.\n" +
             "null이면 exitTriggers[0] 위치에 스폰.")]
    [SerializeField] private Transform exitDoorSpawnPoint;
    [Tooltip("타이머 완료 후 활성화할 BoulderArenaExitTrigger 목록.\n" +
             "씬에서 초기 SetActive(false)로 배치해둘 것.")]
    [SerializeField] private BoulderArenaExitTrigger[] exitTriggers;

    // ── 기믹 공통 설정 ────────────────────────────────────────────────────────

    [Header("⚙️ 기믹 공통 설정")]
    [Tooltip("기믹 최대 지속 시간 (초). 안전망 역할.\n" +
             "0이면 제한 없이 플레이어가 탈출할 때까지 지속.")]
    [SerializeField] private float gimmickDuration = 0f;
    [Tooltip("true: 최초 1회만 발동\n" +
             "false: 탈출 후 재진입 시 다시 발동")]
    [SerializeField] private bool oneTimeOnly = true;

    // ── 레이어 ────────────────────────────────────────────────────────────────

    [Header("🔲 레이어")]
    [SerializeField] private LayerMask playerLayer;

    // ── 런타임 상태 ───────────────────────────────────────────────────────────

    private readonly List<BoulderArenaRolling> activeBoulders = new List<BoulderArenaRolling>();
    private Coroutine durationCoroutine;
    private Coroutine survivalCoroutine;
    private bool isRunning  = false;
    private bool isFinished = false;
    private bool exitOpened = false;

    private Vector2 cachedArenaCenter;

    // 스폰된 기믹 오브젝트 (종료 시 정리용)
    private GameObject spawnedBarrier;
    private GameObject spawnedEntryGate;
    private GameObject spawnedExitDoor;

    // ── 초기화 ────────────────────────────────────────────────────────────────

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning($"[BoulderArenaController] {name}: Collider2D를 자동으로 IsTrigger=true로 설정했습니다.");
        }
    }

    // ── 진입 트리거 ───────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0) return;
        if (isRunning) return;
        if (oneTimeOnly && isFinished) return;

        StartGimmick();
    }

    // ── 기믹 시작 ─────────────────────────────────────────────────────────────

    private void StartGimmick()
    {
        isRunning  = true;
        exitOpened = false;
        cachedArenaCenter = ResolveArenaCenter();

        // Feature 1: 아레나 장벽 스폰
        SpawnBarrier();

        // Feature 2: 진입문 스폰
        SpawnEntryGate();

        // Feature 3: 출구문 스폰 (타이머형 — 진입 즉시 출구를 차단)
        //   survivalDuration > 0 이면 출구문으로 출구를 막고 타이머 종료 시 제거
        //   survivalDuration = 0 이면 출구문 없이 즉시 ExitTrigger 활성화
        if (survivalDuration > 0f)
            SpawnExitDoor();

        // 바위 스폰 (기존)
        if (spawnDelay > 0f)
            StartCoroutine(SpawnWithDelay());
        else
            SpawnBoulders();

        // Feature 3: 생존 타이머 또는 즉시 출구 활성화
        if (survivalDuration > 0f)
            survivalCoroutine = StartCoroutine(SurvivalTimerRoutine());
        else
            OpenExit();

        // 기믹 강제 종료 타이머 (안전망)
        if (gimmickDuration > 0f)
            durationCoroutine = StartCoroutine(GimmickDurationRoutine());
    }

    // ── Feature 1: 아레나 장벽 ───────────────────────────────────────────────

    private void SpawnBarrier()
    {
        if (arenaBarrierPrefab == null) return;

        Vector3 center = arenaCenterTransform != null
            ? arenaCenterTransform.position
            : transform.position;

        spawnedBarrier = Instantiate(arenaBarrierPrefab, center, Quaternion.identity);
    }

    // ── Feature 2: 진입문 ─────────────────────────────────────────────────────

    private void SpawnEntryGate()
    {
        if (entryGatePrefab == null) return;

        Vector3 pos = entryGateSpawnPoint != null
            ? entryGateSpawnPoint.position
            : transform.position;

        spawnedEntryGate = Instantiate(entryGatePrefab, pos, Quaternion.identity);
    }

    // ── Feature 3: 출구문 / 생존 타이머 ──────────────────────────────────────

    /// <summary>
    /// 진입 즉시 출구를 막는 문을 스폰한다 (타이머형 전용).
    /// 타이머 완료 시 OpenExit()에서 ArenaBarrier, EntryGate와 함께 제거된다.
    /// </summary>
    private void SpawnExitDoor()
    {
        if (exitDoorPrefab == null) return;

        Vector3 doorPos = exitDoorSpawnPoint != null
            ? exitDoorSpawnPoint.position
            : (exitTriggers != null && exitTriggers.Length > 0 && exitTriggers[0] != null
                ? exitTriggers[0].transform.position
                : transform.position);

        spawnedExitDoor = Instantiate(exitDoorPrefab, doorPos, Quaternion.identity);
    }

    private IEnumerator SurvivalTimerRoutine()
    {
        // 자체 타이머 UI 시작 (useStageTimer = false 일 때만)
        if (!useStageTimer)
            timerUI?.StartTimer(survivalDuration);

        yield return new WaitForSeconds(survivalDuration);

        // 타이머 완료 → 출구 오픈
        // StopTimer()는 여기서 호출하지 않는다.
        // BoulderArenaTimerUI 내부에서 카운트 완료 시 자동으로 타이머 텍스트를 숨기고
        // "탈출구 열림!" 메시지를 표시한다.
        // StopTimer()는 플레이어가 ExitTrigger를 통과해 ForceEndGimmick()이 호출될 때
        // CleanupSpawnedObjects()에서 실행된다.
        survivalCoroutine = null;
        OpenExit();
    }

    /// <summary>
    /// 생존 타이머 완료 또는 survivalDuration=0 시 호출.
    /// ArenaBarrier + EntryGate + ExitDoor 를 모두 제거하고 ExitTrigger를 활성화한다.
    /// 플레이어는 이제 자유롭게 이동해 ExitTrigger를 통과할 수 있다.
    /// </summary>
    private void OpenExit()
    {
        if (exitOpened) return;
        exitOpened = true;

        // 타이머 완료 = 봉쇄 해제 — 장벽/진입문/출구문 전부 제거
        if (spawnedBarrier   != null) { Destroy(spawnedBarrier);   spawnedBarrier   = null; }
        if (spawnedEntryGate != null) { Destroy(spawnedEntryGate); spawnedEntryGate = null; }
        if (spawnedExitDoor  != null) { Destroy(spawnedExitDoor);  spawnedExitDoor  = null; }

        // ExitTrigger 활성화 → 플레이어가 통과하면 ForceEndGimmick() 호출
        if (exitTriggers != null)
        {
            foreach (var trigger in exitTriggers)
            {
                if (trigger != null)
                    trigger.gameObject.SetActive(true);
            }
        }
    }

    // ── 바위 스폰 (기존) ──────────────────────────────────────────────────────

    private IEnumerator SpawnWithDelay()
    {
        yield return new WaitForSeconds(spawnDelay);
        if (isRunning)
            SpawnBoulders();
    }

    private void SpawnBoulders()
    {
        if (largeBoulderPrefab == null)
        {
            Debug.LogError($"[BoulderArenaController] {name}: largeBoulderPrefab이 연결되지 않았습니다.");
            ForceEndGimmick();
            return;
        }

        Vector2 center = cachedArenaCenter;
        var     player = GameObject.FindGameObjectWithTag("Player");

        for (int i = 0; i < boulderCount; i++)
        {
            Vector2 spawnPos = GetSpawnPosition(center, player);
            Vector2 velocity = Random.insideUnitCircle.normalized * initialSpeed;

            GameObject obj = Instantiate(largeBoulderPrefab, (Vector3)spawnPos, Quaternion.identity);
            BoulderArenaRolling boulder = obj.GetComponent<BoulderArenaRolling>();

            if (boulder != null)
            {
                boulder.Initialize(center, arenaRadius, velocity, this);
                activeBoulders.Add(boulder);
            }
            else
            {
                Debug.LogError($"[BoulderArenaController] largeBoulderPrefab에 BoulderArenaRolling 컴포넌트가 없습니다.");
                Destroy(obj);
            }
        }
    }

    // ── 기믹 종료 ─────────────────────────────────────────────────────────────

    private IEnumerator GimmickDurationRoutine()
    {
        yield return new WaitForSeconds(gimmickDuration);
        ForceEndGimmick();
    }

    /// <summary>
    /// 플레이어가 BoulderArenaExitTrigger를 통과했을 때 호출.
    /// </summary>
    public void OnPlayerEscape()
    {
        if (!isRunning) return;
        ForceEndGimmick();
    }

    /// <summary>
    /// 타이머 만료, 플레이어 탈출 시 바위/장벽/문을 모두 정리하고 기믹을 종료한다.
    /// </summary>
    private void ForceEndGimmick()
    {
        // 코루틴 정리
        if (durationCoroutine != null) { StopCoroutine(durationCoroutine); durationCoroutine = null; }
        if (survivalCoroutine  != null) { StopCoroutine(survivalCoroutine);  survivalCoroutine  = null; }

        // 바위 정리
        var remaining = new List<BoulderArenaRolling>(activeBoulders);
        activeBoulders.Clear();
        foreach (var boulder in remaining)
        {
            if (boulder != null)
                boulder.ForceDestroy();
        }

        // 스폰된 기믹 오브젝트 정리
        CleanupSpawnedObjects();

        isRunning  = false;
        isFinished = true;
    }

    /// <summary>
    /// 장벽, 진입문, 출구문 Destroy + 타이머 UI 정리.
    /// </summary>
    private void CleanupSpawnedObjects()
    {
        timerUI?.StopTimer();

        if (spawnedBarrier   != null) { Destroy(spawnedBarrier);   spawnedBarrier   = null; }
        if (spawnedEntryGate != null) { Destroy(spawnedEntryGate); spawnedEntryGate = null; }
        if (spawnedExitDoor  != null) { Destroy(spawnedExitDoor);  spawnedExitDoor  = null; }
    }

    // ── BoulderArenaRolling 콜백 ──────────────────────────────────────────────

    /// <summary>
    /// Large 바위 분열 시 생성된 Small 바위를 추적 목록에 등록.
    /// BoulderArenaRolling.SpawnSmallBoulders()에서 호출.
    /// </summary>
    public void RegisterBoulder(BoulderArenaRolling boulder)
    {
        if (boulder != null && !activeBoulders.Contains(boulder))
            activeBoulders.Add(boulder);
    }

    /// <summary>
    /// BoulderArenaRolling 소멸 시 호출 (Die / Split 완료 후).
    /// </summary>
    public void OnBoulderDestroyed(BoulderArenaRolling boulder)
    {
        activeBoulders.Remove(boulder);
    }

    // ── 유틸리티 ──────────────────────────────────────────────────────────────

    private Vector2 ResolveArenaCenter()
    {
        return arenaCenterTransform != null
            ? (Vector2)arenaCenterTransform.position
            : (Vector2)transform.position;
    }

    private Vector2 GetSpawnPosition(Vector2 center, GameObject player)
    {
        Vector2 pos = GetRandomPositionInRadius(center, arenaRadius);

        for (int attempt = 0; attempt < spawnRetryCount; attempt++)
        {
            Vector2 candidate = GetRandomPositionInRadius(center, arenaRadius);
            if (player == null ||
                Vector2.Distance(candidate, player.transform.position) >= minSpawnDistFromPlayer)
            {
                pos = candidate;
                break;
            }
        }

        return pos;
    }

    private static Vector2 GetRandomPositionInRadius(Vector2 center, float radius)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist  = Mathf.Sqrt(Random.Range(0f, 1f)) * radius;
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
    }

    // ── 에디터 Gizmo ──────────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Vector2 center = arenaCenterTransform != null
            ? (Vector2)arenaCenterTransform.position
            : (Vector2)transform.position;

        // 바위 반사 경계 (흰색 원)
        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
        DrawGizmoCircle(center, arenaRadius, 48);

        // 중심점
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(new Vector3(center.x, center.y, 0f), 0.3f);

        // 진입 트리거 범위 (녹색)
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.3f);
        Gizmos.DrawCube(transform.position, transform.lossyScale);

        // 진입문 스폰 위치 (주황색)
        if (entryGateSpawnPoint != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawWireCube(entryGateSpawnPoint.position, Vector3.one * 0.5f);
            Gizmos.DrawLine(transform.position, entryGateSpawnPoint.position);
        }

        // 출구문 스폰 위치 (하늘색)
        if (exitDoorSpawnPoint != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireCube(exitDoorSpawnPoint.position, Vector3.one * 0.5f);
            Gizmos.DrawLine(transform.position, exitDoorSpawnPoint.position);
        }
    }

    private static void DrawGizmoCircle(Vector2 center, float radius, int segments)
    {
        float step = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float   a0 = i       * step * Mathf.Deg2Rad;
            float   a1 = (i + 1) * step * Mathf.Deg2Rad;
            Vector3 p0 = new Vector3(center.x + Mathf.Cos(a0) * radius,
                                     center.y + Mathf.Sin(a0) * radius, 0f);
            Vector3 p1 = new Vector3(center.x + Mathf.Cos(a1) * radius,
                                     center.y + Mathf.Sin(a1) * radius, 0f);
            Gizmos.DrawLine(p0, p1);
        }
    }
#endif
}
