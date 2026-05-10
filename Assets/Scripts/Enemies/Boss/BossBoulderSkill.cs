using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 구르는 바위 스킬 오케스트레이터.
/// BossSkillController의 "Boss_BoulderRoll" case에서 Execute()를 호출한다.
///
/// 동작 흐름:
///   Execute(skillEntry, onComplete) 호출
///   → arenaCenter 기준 랜덤 위치에 Large 바위 boulderCount개 스폰
///   → 각 바위에 랜덤 방향 velocity 부여 + Initialize()
///   → skillDuration 초 타이머 → 살아있는 바위 전부 ForceDestroy
///   → onComplete 콜백으로 BossSkillController에 완료 알림
///
/// 바위 소멸 추적:
///   BossRollingBoulder는 소멸 시 OnBoulderDestroyed()를 호출
///   → activeBoulders 리스트에서 제거
///   → 모든 바위가 타이머 전에 소멸되면 조기 완료 가능 (earlyCompleteWhenAllDead 옵션)
/// </summary>
public class BossBoulderSkill : MonoBehaviour
{
    [Header("⚙️ 참조")]
    [SerializeField] private BaseEnemy baseEnemy;

    [Header("🪨 스폰 설정")]
    [Tooltip("Large 바위 프리팹")]
    [SerializeField] private GameObject largeBoulderPrefab;
    [Tooltip("동시 스폰할 Large 바위 수")]
    [Range(1, 6)]
    [SerializeField] private int boulderCount = 3;
    [Tooltip("스킬 지속 시간 (초) — 타이머 종료 시 남은 바위 강제 소멸")]
    [SerializeField] private float skillDuration = 5f;
    [Tooltip("바위 초기 속도 (skillScaleMultiplier 적용됨)")]
    [SerializeField] private float initialSpeed = 5f;
    [Tooltip("모든 바위가 타이머 전에 소멸되면 스킬을 조기 완료할지 여부")]
    [SerializeField] private bool earlyCompleteWhenAllDead = false;

    [Header("📡 텔레그래프")]
    [Tooltip("스폰 위치 표시용 텔레그래프 프리팹\n" +
             "null이면 텔레그래프 없이 즉시 스폰.")]
    [SerializeField] private GameObject telegraphPrefab;

    [Header("🎯 전투 영역")]
    [Tooltip("전투 영역 중심 Transform.\n" +
             "직접 연결하거나, 비워두면 BossArenaTag 태그 오브젝트를 자동 탐색.\n" +
             "둘 다 없으면 보스 스폰 위치를 중심으로 사용.")]
    [SerializeField] private Transform arenaCenterTransform;
    [Tooltip("arenaCenterTransform이 비어있을 때 씬에서 자동 탐색할 태그")]
    [SerializeField] private string bossArenaTag = "BossArena";
    [Tooltip("전투 영역 반경 (BossMeteorShowerSkill과 동일 값 권장)")]
    [SerializeField] private float arenaRadius = 16f;
    [Tooltip("스폰 위치 생성 시 플레이어와의 최소 거리 (즉시 피격 방지)")]
    [SerializeField] private float minSpawnDistanceFromPlayer = 3f;
    [Tooltip("스폰 위치 생성 재시도 횟수 (최소 거리 실패 시)")]
    [SerializeField] private int spawnPositionRetryCount = 8;

    // ── 런타임 상태 ───────────────────────────────────────────────────────────

    private readonly List<BossRollingBoulder> activeBoulders = new List<BossRollingBoulder>();
    private readonly List<GameObject> activeTelegraphs = new List<GameObject>();
    private List<Vector2> cachedSpawnPositions = new List<Vector2>();
    private System.Action onCompleteCallback;
    private Coroutine durationCoroutine;
    private bool isExecuting = false;

    // ── 초기화 ────────────────────────────────────────────────────────────────

    // 스폰 시점의 아레나 중심 (Execute 시 캐싱 — 스킬 진행 중 보스 이동 영향 없음)
    private Vector2 cachedArenaCenter;

    private void Awake()
    {
        if (baseEnemy == null)
            baseEnemy = GetComponent<BaseEnemy>();
    }

    // ── 스킬 실행 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// BossSkillController.OnSkillCastStart()에서 호출.
    /// 스폰 위치를 미리 생성해 캐싱한다 (텔레그래프와 실제 스폰 위치 동기화).
    /// </summary>
    public void PreparePositions(BossSkillEntry skillEntry)
    {
        cachedArenaCenter    = ResolveArenaCenter();
        cachedSpawnPositions = GenerateSpawnPositions(cachedArenaCenter, boulderCount);
    }

    /// <summary>
    /// BossSkillController.OnSkillCastStart()에서 호출.
    /// 캐싱된 위치마다 텔레그래프를 하나씩 스폰한다.
    /// </summary>
    public void SpawnAllTelegraphs(BossSkillEntry skillEntry)
    {
        if (telegraphPrefab == null) return;
        if (cachedSpawnPositions == null || cachedSpawnPositions.Count == 0)
        {
            Debug.LogWarning("[BossBoulderSkill] SpawnAllTelegraphs: PreparePositions()를 먼저 호출하세요.");
            return;
        }

        ClearTelegraphs();

        float duration = (skillEntry?.skillData?.CastTime ?? 1f) + 1f;

        foreach (Vector2 pos in cachedSpawnPositions)
        {
            GameObject tgo = Instantiate(telegraphPrefab, (Vector3)(pos), Quaternion.identity);
            activeTelegraphs.Add(tgo);

            var indicator = tgo.GetComponent<TelegraphIndicator>();
            if (indicator != null)
                indicator.Initialize(skillEntry.skillData, duration, skillEntry?.skillScaleMultiplier ?? 1f);
            else
            {
                var indicatorMesh = tgo.GetComponent<TelegraphIndicatorMesh>();
                if (indicatorMesh != null)
                    indicatorMesh.Initialize(skillEntry.skillData, duration, skillEntry?.skillScaleMultiplier ?? 1f);
            }
        }
    }

    /// <summary>
    /// 텔레그래프를 모두 제거한다.
    /// BossSkillController.ExecuteSkillDamage() 및 Cancel()에서 호출.
    /// </summary>
    public void ClearTelegraphs()
    {
        foreach (GameObject t in activeTelegraphs)
        {
            if (t != null) Destroy(t);
        }
        activeTelegraphs.Clear();
    }

    /// <summary>
    /// BossSkillController.ExecuteSkillDamage()에서 호출.
    /// 캐싱된 위치에 Large 바위를 스폰하고 타이머를 시작한다.
    /// PreparePositions()가 호출되지 않은 경우 즉석에서 위치를 생성한다.
    /// </summary>
    public void Execute(BossSkillEntry skillEntry, System.Action onComplete)
    {
        if (largeBoulderPrefab == null)
        {
            Debug.LogError("[BossBoulderSkill] largeBoulderPrefab이 없습니다!");
            onComplete?.Invoke();
            return;
        }

        activeBoulders.Clear();
        onCompleteCallback = onComplete;
        isExecuting = true;

        // PreparePositions()가 이미 호출됐으면 캐싱된 중심/위치 재사용
        // 그렇지 않으면 즉석 생성 (텔레그래프 없이 바로 실행되는 경우 대비)
        if (cachedSpawnPositions == null || cachedSpawnPositions.Count == 0)
        {
            cachedArenaCenter    = ResolveArenaCenter();
            cachedSpawnPositions = GenerateSpawnPositions(cachedArenaCenter, boulderCount);
        }

        Vector2 center = cachedArenaCenter;
        float speed    = initialSpeed * (skillEntry?.skillScaleMultiplier ?? 1f);

        foreach (Vector2 spawnPos in cachedSpawnPositions)
        {
            Vector2 vel = Random.insideUnitCircle.normalized * speed;

            GameObject obj = Instantiate(largeBoulderPrefab, (Vector3)(spawnPos), Quaternion.identity);
            BossRollingBoulder boulder = obj.GetComponent<BossRollingBoulder>();

            if (boulder != null)
            {
                boulder.Initialize(center, arenaRadius, vel, skillEntry, this);
                activeBoulders.Add(boulder);
            }
            else
            {
                Debug.LogError("[BossBoulderSkill] largeBoulderPrefab에 BossRollingBoulder 컴포넌트가 없습니다!");
                Destroy(obj);
            }
        }

        // 다음 캐스트를 위해 위치 캐시 초기화
        cachedSpawnPositions.Clear();

        durationCoroutine = StartCoroutine(SkillDurationRoutine());
    }

    // ── 타이머 ────────────────────────────────────────────────────────────────

    private IEnumerator SkillDurationRoutine()
    {
        yield return new WaitForSeconds(skillDuration);

        ForceEndSkill();
    }

    /// <summary>
    /// 타이머 종료 시 남은 바위를 모두 강제 소멸시키고 완료를 알린다.
    /// </summary>
    private void ForceEndSkill()
    {
        // 리스트 복사 후 순회 (ForceDestroy 중 리스트 변경 방지)
        var remaining = new List<BossRollingBoulder>(activeBoulders);
        activeBoulders.Clear();

        foreach (var boulder in remaining)
        {
            if (boulder != null)
                boulder.ForceDestroy();
        }

        CompleteSkill();
    }

    private void CompleteSkill()
    {
        isExecuting = false;
        var callback = onCompleteCallback;
        onCompleteCallback = null;
        callback?.Invoke();
    }

    // ── BossRollingBoulder 콜백 ───────────────────────────────────────────────

    /// <summary>
    /// Large 바위 Split 시 생성된 Small 바위를 추적 목록에 등록.
    /// BossRollingBoulder.SpawnSmallBoulders()에서 호출.
    /// </summary>
    public void RegisterBoulder(BossRollingBoulder boulder)
    {
        if (boulder != null && !activeBoulders.Contains(boulder))
            activeBoulders.Add(boulder);
    }

    /// <summary>
    /// BossRollingBoulder 소멸 시 호출 (Die / Split 완료 후).
    /// </summary>
    public void OnBoulderDestroyed(BossRollingBoulder boulder)
    {
        activeBoulders.Remove(boulder);

        // 조기 완료: 모든 바위가 타이머 전에 소멸됐을 때
        if (earlyCompleteWhenAllDead && isExecuting && activeBoulders.Count == 0)
        {
            if (durationCoroutine != null)
            {
                StopCoroutine(durationCoroutine);
                durationCoroutine = null;
            }
            CompleteSkill();
        }
    }

    // ── 강제 취소 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// BossSkillController.ForceCancelSkill()에서 호출.
    /// 타이머와 바위를 모두 즉시 정리한다.
    /// </summary>
    public void Cancel()
    {
        if (!isExecuting) return;

        if (durationCoroutine != null)
        {
            StopCoroutine(durationCoroutine);
            durationCoroutine = null;
        }

        ClearTelegraphs();
        cachedSpawnPositions.Clear();

        var remaining = new List<BossRollingBoulder>(activeBoulders);
        activeBoulders.Clear();

        foreach (var boulder in remaining)
        {
            if (boulder != null)
                boulder.ForceDestroy();
        }

        isExecuting = false;
        onCompleteCallback = null;
    }

    // ── 유틸리티 ──────────────────────────────────────────────────────────────

    /// <summary>
    /// 전투 영역 중심 좌표 결정 (우선순위 순):
    ///   1) Inspector에 직접 연결된 arenaCenterTransform
    ///   2) 씬에서 bossArenaTag 태그 오브젝트 자동 탐색
    ///   3) 보스 현재 위치 (fallback)
    /// </summary>
    private Vector2 ResolveArenaCenter()
    {
        // 1) 직접 연결
        if (arenaCenterTransform != null)
            return arenaCenterTransform.position;

        // 2) 태그 자동 탐색
        if (!string.IsNullOrEmpty(bossArenaTag))
        {
            GameObject arenaObj = GameObject.FindGameObjectWithTag(bossArenaTag);
            if (arenaObj != null)
            {
                arenaCenterTransform = arenaObj.transform; // 이후 재탐색 방지
                return arenaObj.transform.position;
            }
            else
            {
                Debug.LogWarning($"[BossBoulderSkill] '{bossArenaTag}' 태그 오브젝트를 찾을 수 없습니다. 보스 위치를 중심으로 사용합니다.");
            }
        }

        // 3) fallback: 보스 위치
        return transform.position;
    }

    /// <summary>
    /// count개의 스폰 위치를 생성해 반환한다.
    /// 플레이어와 minSpawnDistanceFromPlayer 이상 떨어진 위치를 우선 선택.
    /// </summary>
    private List<Vector2> GenerateSpawnPositions(Vector2 center, int count)
    {
        var positions = new List<Vector2>(count);
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        for (int i = 0; i < count; i++)
        {
            Vector2 pos = GetRandomPositionInRadius(center, arenaRadius);

            for (int attempt = 0; attempt < spawnPositionRetryCount; attempt++)
            {
                Vector2 candidate = GetRandomPositionInRadius(center, arenaRadius);
                if (player == null ||
                    Vector2.Distance(candidate, player.transform.position) >= minSpawnDistanceFromPlayer)
                {
                    pos = candidate;
                    break;
                }
            }

            positions.Add(pos);
        }

        return positions;
    }

    private static Vector2 GetRandomPositionInRadius(Vector2 center, float radius)
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist  = Mathf.Sqrt(Random.Range(0f, 1f)) * radius;
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
    }

    // ── 에디터 Gizmo ──────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        // 에디터에서는 태그 탐색 없이 연결된 Transform 또는 보스 위치로 표시
        Vector2 center = arenaCenterTransform != null
            ? (Vector2)arenaCenterTransform.position
            : (Vector2)transform.position;

        // 전투 영역 경계 (흰색 원)
        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
        DrawGizmoCircle(center, arenaRadius, 48);

        // 중심점 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(new Vector3(center.x, center.y, 0f), 0.3f);
    }

    private static void DrawGizmoCircle(Vector2 center, float radius, int segments)
    {
        float step = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float a0 = i       * step * Mathf.Deg2Rad;
            float a1 = (i + 1) * step * Mathf.Deg2Rad;
            Vector3 p0 = new Vector3(center.x + Mathf.Cos(a0) * radius,
                                     center.y + Mathf.Sin(a0) * radius, 0f);
            Vector3 p1 = new Vector3(center.x + Mathf.Cos(a1) * radius,
                                     center.y + Mathf.Sin(a1) * radius, 0f);
            Gizmos.DrawLine(p0, p1);
        }
    }
}
