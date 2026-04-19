using UnityEngine;

/// <summary>
/// 플레이어가 트리거 콜라이더에 진입하면 RollingBoulder를 스폰하는 컴포넌트.
///
/// 씬 배치 방법:
///   1. 빈 오브젝트에 이 컴포넌트 추가
///   2. Collider2D (Is Trigger = true) 추가 — 플레이어 진입 감지 범위
///   3. Inspector에서 pathData, spawnPoint, largeBoulderPrefab 연결
///
/// 반복 스폰:
///   respawnCooldown > 0 이면 cooldown마다 재스폰
///   respawnCooldown = 0 이면 1회만 스폰
/// </summary>
public class BoulderSpawner : MonoBehaviour
{
    [Header("경로 데이터")]
    [Tooltip("바위가 따라갈 경로 ScriptableObject")]
    [SerializeField] private BoulderPathData pathData;

    [Header("스폰 설정")]
    [Tooltip("바위가 생성될 월드 위치 (보통 경로 시작점 근처)")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Large 바위 프리팹 (풀 미사용 시 직접 인스턴스)")]
    [SerializeField] private GameObject largeBoulderPrefab;

    [Tooltip("재스폰 쿨타임 (초). 0이면 1회만 스폰.\n" +
             "autoRepeat = false: 트리거 재진입 시 쿨타임 체크\n" +
             "autoRepeat = true : 첫 스폰 후 이 간격마다 자동 반복")]
    [SerializeField] private float respawnCooldown = 0f;

    [Tooltip("true: 첫 트리거 후 respawnCooldown마다 자동으로 바위를 재스폰 (구간 내내 바위 지속)\n" +
             "false: 트리거를 밟을 때마다 쿨타임 체크 후 스폰 (수동 방식)")]
    [SerializeField] private bool autoRepeat = false;

    [Tooltip("스폰 딜레이 (플레이어 진입 후 바위가 등장하기까지 대기 시간)")]
    [SerializeField] private float spawnDelay = 0.5f;

    [Tooltip("최대 동시 활성 바위 수 (초과 시 스폰 안함)")]
    [SerializeField] private int maxActiveBoulders = 2;

    [Header("레이어")]
    [SerializeField] private LayerMask playerLayer;

    // ── 내부 상태 ─────────────────────────────────────────────────────────────
    private bool isTriggered = false;
    private bool isStopped = false;
    private float lastSpawnTime = -999f;
    private int activeBoulderCount = 0;

    private void OnTriggerEnter2D(Collider2D other)
    {

        if (isStopped)
        {
            return;
        }
        if ((playerLayer.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }
        if (isTriggered && respawnCooldown <= 0f)
        {
            return;
        }
        if (Time.time - lastSpawnTime < respawnCooldown)
        {
            return;
        }
        if (activeBoulderCount >= maxActiveBoulders)
        {
            return;
        }

        isTriggered = true;

        if (spawnDelay > 0f)
            StartCoroutine(SpawnWithDelay());
        else
            SpawnBoulder();

        // 자동 반복 모드: 첫 트리거 시 한 번만 루틴 시작
        if (autoRepeat && respawnCooldown > 0f)
            StartCoroutine(AutoRespawnRoutine());
    }

    private System.Collections.IEnumerator SpawnWithDelay()
    {
        yield return new UnityEngine.WaitForSeconds(spawnDelay);
        if (!isStopped)
            SpawnBoulder();
    }

    /// <summary>
    /// autoRepeat = true 일 때 첫 트리거 후 respawnCooldown마다 자동 스폰.
    /// isStopped = true 또는 컴포넌트 비활성화 시 종료.
    /// </summary>
    private System.Collections.IEnumerator AutoRespawnRoutine()
    {
        while (!isStopped)
        {
            yield return new UnityEngine.WaitForSeconds(respawnCooldown);

            if (isStopped) yield break;
            if (activeBoulderCount >= maxActiveBoulders)
            {
                continue;
            }

            SpawnBoulder();
        }
    }

    /// <summary>
    /// 스폰을 즉시 중지한다. 진행 중인 딜레이 코루틴도 취소된다.
    /// StageManager, EndTrigger 등 외부 조건에서 호출.
    /// </summary>
    public void StopSpawning()
    {
        isStopped = true;
        StopAllCoroutines();
    }

    /// <summary>
    /// 중지된 스폰을 다시 활성화한다 (재사용 시).
    /// </summary>
    public void ResumeSpawning()
    {
        isStopped = false;
    }

    private void SpawnBoulder()
    {
        if (pathData == null)
        {
            Debug.LogWarning($"[BoulderSpawner] {name}: BoulderPathData가 연결되지 않았습니다.");
            return;
        }

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;

        GameObject boulderObj = null;

        // GamePoolManager 사용 시
        if (GamePoolManager.Instance != null)
        {
            boulderObj = GamePoolManager.Instance.SpawnFromPool("LargeBoulder", position, Quaternion.identity);
        }

        // 풀 미등록 시 직접 인스턴스
        if (boulderObj == null && largeBoulderPrefab != null)
        {
            boulderObj = Instantiate(largeBoulderPrefab, position, Quaternion.identity);
        }

        if (boulderObj == null)
        {
            Debug.LogWarning($"[BoulderSpawner] {name}: 바위를 스폰할 수 없습니다. 풀 또는 프리팹을 확인하세요.");
            return;
        }

        RollingBoulder boulder = boulderObj.GetComponent<RollingBoulder>();
        if (boulder == null)
        {
            Debug.LogWarning($"[BoulderSpawner] {name}: 스폰된 오브젝트에 RollingBoulder 컴포넌트가 없습니다.");
            return;
        }

        boulder.Initialize(pathData, 0, this);
        lastSpawnTime = Time.time;
        activeBoulderCount++;
    }

    /// <summary>
    /// BoulderWaveController 등 외부에서 직접 스폰을 요청할 때 사용.
    /// 트리거/쿨타임 조건을 우회하고 maxActiveBoulders만 체크한다.
    /// </summary>
    public void ForceSpawn()
    {
        // WaveController 등 외부에서 직접 호출하는 경우 maxActiveBoulders 제한 없이 스폰
        SpawnBoulder();
    }

    /// <summary>
    /// RollingBoulder가 소멸될 때 호출하거나, 외부에서 카운트를 줄일 때 사용.
    /// </summary>
    public void NotifyBoulderDestroyed()
    {
        activeBoulderCount = Mathf.Max(0, activeBoulderCount - 1);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (pathData == null || pathData.waypoints == null || pathData.waypoints.Length < 2) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < pathData.waypoints.Length - 1; i++)
        {
            Gizmos.DrawLine(pathData.waypoints[i], pathData.waypoints[i + 1]);
            Gizmos.DrawWireSphere(pathData.waypoints[i], 0.15f);
        }
        Gizmos.DrawWireSphere(pathData.waypoints[pathData.waypoints.Length - 1], 0.15f);

        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.3f);
        }
    }
#endif
}
