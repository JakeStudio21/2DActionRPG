using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아레나 구르는 바위 기믹 오케스트레이터.
///
/// 동작 흐름:
///   플레이어가 진입 트리거(이 오브젝트의 Collider2D)에 진입
///   → 아레나 내 랜덤 위치에 Large 바위 boulderCount개 스폰
///   → 각 바위 BoulderArenaRolling.Initialize() 호출
///   → 바위들이 원형 아레나 경계에서 반사하며 굴러다님
///   → 플레이어가 BoulderArenaExitTrigger를 통과 → OnPlayerEscape() 호출
///     OR gimmickDuration(초) 타이머 종료 → 자동 종료
///   → 남은 바위 전부 ForceDestroy → 기믹 종료
///
/// 바위 소멸 추적:
///   BoulderArenaRolling은 소멸 시 OnBoulderDestroyed()를 호출
///   → activeBoulders 리스트에서 제거
///   Large 바위 분열 시 SpawnSmallBoulders()에서 RegisterBoulder()로 Small 등록
///
/// 씬 배치:
///   1. 진입 영역 오브젝트에 이 컴포넌트 + Collider2D(IsTrigger) 추가
///   2. arenaCenterTransform에 아레나 중심 오브젝트 연결
///   3. arenaRadius를 실제 원형 공간 반경과 일치하게 설정
///   4. 출구에 BoulderArenaExitTrigger 배치 후 이 컴포넌트 연결
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BoulderArenaController : MonoBehaviour
{
    // ── 아레나 설정 ───────────────────────────────────────────────────────────

    [Header("🎯 아레나 설정")]
    [Tooltip("아레나 중심 Transform.\n" +
             "비워두면 이 오브젝트 위치를 중심으로 사용.")]
    [SerializeField] private Transform arenaCenterTransform;
    [Tooltip("원형 아레나 반경.\n" +
             "씬의 실제 원형 벽 반경과 일치하게 설정할 것.")]
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

    // ── 기믹 설정 ─────────────────────────────────────────────────────────────

    [Header("⚙️ 기믹 설정")]
    [Tooltip("기믹 최대 지속 시간 (초).\n" +
             "0이면 타이머 없이 플레이어가 탈출할 때까지 무제한 지속.")]
    [SerializeField] private float gimmickDuration = 0f;
    [Tooltip("true: 최초 1회만 발동 (탈출 후 재진입해도 반응 없음)\n" +
             "false: 탈출 후 재진입 시 다시 발동")]
    [SerializeField] private bool oneTimeOnly = true;

    // ── 레이어 ────────────────────────────────────────────────────────────────

    [Header("🔲 레이어")]
    [SerializeField] private LayerMask playerLayer;

    // ── 런타임 상태 ───────────────────────────────────────────────────────────

    private readonly List<BoulderArenaRolling> activeBoulders = new List<BoulderArenaRolling>();
    private Coroutine durationCoroutine;
    private bool isRunning  = false;
    private bool isFinished = false;  // oneTimeOnly용

    private Vector2 cachedArenaCenter;

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

    // ── 기믹 시작/종료 ────────────────────────────────────────────────────────

    private void StartGimmick()
    {
        isRunning = true;
        cachedArenaCenter = ResolveArenaCenter();

        if (spawnDelay > 0f)
            StartCoroutine(SpawnWithDelay());
        else
            SpawnBoulders();

        if (gimmickDuration > 0f)
            durationCoroutine = StartCoroutine(GimmickDurationRoutine());
    }

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
    /// 타이머 만료 또는 플레이어 탈출 시 남은 바위를 모두 정리하고 기믹을 종료한다.
    /// </summary>
    private void ForceEndGimmick()
    {
        if (durationCoroutine != null)
        {
            StopCoroutine(durationCoroutine);
            durationCoroutine = null;
        }

        var remaining = new List<BoulderArenaRolling>(activeBoulders);
        activeBoulders.Clear();

        foreach (var boulder in remaining)
        {
            if (boulder != null)
                boulder.ForceDestroy();
        }

        isRunning  = false;
        isFinished = true;
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

    /// <summary>
    /// 아레나 중심 좌표 결정.
    ///   1) Inspector에 직접 연결된 arenaCenterTransform
    ///   2) 이 오브젝트 위치 (fallback)
    /// </summary>
    private Vector2 ResolveArenaCenter()
    {
        return arenaCenterTransform != null
            ? (Vector2)arenaCenterTransform.position
            : (Vector2)transform.position;
    }

    /// <summary>
    /// 플레이어와 minSpawnDistFromPlayer 이상 떨어진 랜덤 스폰 위치 반환.
    /// </summary>
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

        // 아레나 경계 (흰색 원)
        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
        DrawGizmoCircle(center, arenaRadius, 48);

        // 중심점
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(new Vector3(center.x, center.y, 0f), 0.3f);

        // 진입 트리거 범위 (녹색)
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.3f);
        Gizmos.DrawCube(transform.position, transform.lossyScale);
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
