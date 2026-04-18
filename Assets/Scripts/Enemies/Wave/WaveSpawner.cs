using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI; // NavMesh 검증용

/// <summary>
/// SimpleMob 웨이브 스폰 관리자
/// - WaveData 기반 스폰
/// - 패턴 스폰 지원
/// - 클리어 조건 체크
/// - NavMesh 검증 시스템 (스폰 위치 보정)
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [Header("Wave Configuration")]
    [SerializeField] private WaveData currentWaveData;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform customSpawnCenter; // 커스텀 스폰 중심 (옵션)
    
    [Header("Spawn Settings")]
    [SerializeField] private bool autoStartWave = true;
    [SerializeField] private float waveStartDelay = 1f;
    
    [Header("Debug")]
    [SerializeField] private bool showSpawnGizmos = false;
    
    // 웨이브 상태
    private bool isSpawning = false;
    private int currentWaveNumber = 0;
    private int totalSpawnedCount = 0;
    private int totalKilledCount = 0;
    private List<GameObject> spawnedMobs = new List<GameObject>();
    
    // 코루틴
    private Coroutine spawnCoroutine;
    
    // 이벤트
    public System.Action<int> OnWaveStart;
    public System.Action<int> OnWaveComplete;
    public System.Action<int, int> OnMobKilled; // (killedCount, totalCount)
    
    private void Start()
    {
        // 플레이어 자동 찾기
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        // 자동 시작
        if (autoStartWave && currentWaveData != null)
        {
            Invoke(nameof(StartAutoWave), waveStartDelay);
        }
    }
    
    private void StartAutoWave()
    {
        StartWave(currentWaveData);
    }
    
    /// <summary>
    /// 웨이브 시작
    /// </summary>
    public void StartWave(WaveData waveData)
    {
        if (isSpawning)
        {
            Debug.LogWarning("[WaveSpawner] 이미 웨이브가 진행 중입니다!");
            return;
        }
        
        if (waveData == null)
        {
            Debug.LogError("[WaveSpawner] WaveData가 null입니다!");
            return;
        }
        
        currentWaveData = waveData;
        currentWaveNumber = waveData.waveNumber;
        totalSpawnedCount = 0;
        totalKilledCount = 0;
        spawnedMobs.Clear();
        
            Dbg.Log($"🌊 [WaveSpawner] Wave {currentWaveNumber} 시작!");
        
        OnWaveStart?.Invoke(currentWaveNumber);
        
        spawnCoroutine = StartCoroutine(SpawnWaveCoroutine());
    }
    
    /// <summary>
    /// 외부에서 호출하는 웨이브 시작 (WaveConfig 통합용)
    /// </summary>
    public void StartWaveExternal(WaveData waveData)
    {
            Dbg.Log($"📞 [WaveSpawner] StartWaveExternal() 호출됨 - WaveData: {waveData?.name ?? "null"}");
        
        StartWave(waveData);
    }
    
    /// <summary>
    /// Inspector에 설정된 currentWaveData로 웨이브 시작 (WaveTriggerZone 직접 연결용)
    /// </summary>
    public void TriggerStart()
    {
        if (currentWaveData == null)
        {
            Debug.LogError($"[WaveSpawner] {gameObject.name}: TriggerStart() 호출됐지만 Current Wave Data가 설정되지 않았습니다!");
            return;
        }
        
            Dbg.Log($"📞 [WaveSpawner] TriggerStart() 호출됨 - WaveData: {currentWaveData.name}");
        
        StartWave(currentWaveData);
    }
    
    /// <summary>
    /// 스폰 중심점 설정 (외부 호출용)
    /// </summary>
    public void SetSpawnCenter(Transform spawnCenter)
    {
        customSpawnCenter = spawnCenter;
        
            Dbg.Log($"📍 [WaveSpawner] 커스텀 스폰 중심 설정: {spawnCenter.name}");
    }
    
    /// <summary>
    /// 웨이브 스폰 코루틴
    /// </summary>
    private IEnumerator SpawnWaveCoroutine()
    {
        isSpawning = true;
        
        // 각 SpawnConfig 순차 실행
        foreach (SpawnConfig config in currentWaveData.spawnConfigs)
        {
            if (config.mobPrefab == null)
            {
                Debug.LogWarning("[WaveSpawner] mobPrefab이 null입니다!");
                continue;
            }
            
            // SpawnConfig 스폰
            for (int i = 0; i < config.spawnCount; i++)
            {
                SpawnMob(config, i);
                totalSpawnedCount++;
                
                yield return new WaitForSeconds(config.spawnInterval);
            }
        }
        
            Dbg.Log($"✅ [WaveSpawner] 스폰 완료: {totalSpawnedCount}마리");
        
        // 클리어 조건 체크 시작
        StartCoroutine(CheckClearConditionCoroutine());
    }
    
    /// <summary>
    /// 몬스터 스폰 (NavMesh 검증 포함)
    /// </summary>
    private void SpawnMob(SpawnConfig config, int index)
    {
        Vector3 spawnPosition = CalculateSpawnPosition(config, index);
        
        // ⭐⭐⭐ NavMesh 위치 검증 및 보정 (핵심 수정!)
        spawnPosition = GetValidNavMeshPosition(spawnPosition);
        
        // 풀에서 가져오기
        GameObject mob = null;
        
        if (GamePoolManager.Instance != null && !string.IsNullOrEmpty(config.mobPrefab.name))
        {
            mob = GamePoolManager.Instance.SpawnFromPool(config.mobPrefab.name, spawnPosition, Quaternion.identity);
        }
        
        // 풀에 없으면 인스턴스화
        if (mob == null)
        {
            mob = Instantiate(config.mobPrefab, spawnPosition, Quaternion.identity);
        }
        
        if (mob != null)
        {
            spawnedMobs.Add(mob);
            
            // SimpleMob 컴포넌트 체크 및 설정
            SimpleMob simpleMob = mob.GetComponent<SimpleMob>();
            if (simpleMob == null)
            {
                Debug.LogWarning($"[WaveSpawner] {mob.name}에 SimpleMob 컴포넌트가 없습니다!");
            }
            else
            {
                // 이동 속도 설정 (config.moveSpeed > 0이면 적용)
                if (config.moveSpeed > 0f)
                {
                    simpleMob.SetMoveSpeed(config.moveSpeed);
                }

                // 레벨 초기화 — StageBaseLevel 기반 성장 적용 (growthProfile이 없으면 무시)
                int stageLevel = 1;
                var stageCfg = StageSystem.StageManager.Instance?.CurrentStageConfig;
                if (stageCfg != null) stageLevel = stageCfg.StageBaseLevel;
                simpleMob.InitializeLevel(stageLevel);
            }
            
                Dbg.Log($"📍 [WaveSpawner] 스폰: {mob.name} at {spawnPosition}");
        }
    }
    
    /// <summary>
    /// 스폰 위치 계산
    /// </summary>
    private Vector3 CalculateSpawnPosition(SpawnConfig config, int index)
    {
        // 스폰 중심점 결정: customSpawnCenter > playerTransform > WaveSpawner 위치
        Vector3 spawnCenterPos;
        
        if (customSpawnCenter != null)
        {
            spawnCenterPos = customSpawnCenter.position;
        }
        else if (playerTransform != null)
        {
            spawnCenterPos = playerTransform.position;
        }
        else
        {
            spawnCenterPos = transform.position;
        }
        
        Vector3 playerPos = spawnCenterPos;
        
        // 패턴별 위치 계산
        switch (config.spawnPattern)
        {
            case SpawnPattern.Circle:
                return CalculateCirclePosition(playerPos, config.spawnRadius, index, config.spawnCount);
            
            case SpawnPattern.Random:
                return CalculateRandomPosition(playerPos, config.spawnRadius);
            
            case SpawnPattern.Line:
                return CalculateLinePosition(playerPos, config.lineStart, config.lineEnd, index, config.spawnCount);
            
            case SpawnPattern.Grid:
                return CalculateGridPosition(playerPos, config.gridRows, config.gridColumns, config.gridSpacing, index);
            
            default:
                return playerPos + (Vector3)Random.insideUnitCircle * config.spawnRadius;
        }
    }
    
    /// <summary>
    /// Circle 패턴 위치 계산
    /// </summary>
    private Vector3 CalculateCirclePosition(Vector3 center, float radius, int index, int totalCount)
    {
        float angle = (360f / totalCount) * index * Mathf.Deg2Rad;
        float x = center.x + radius * Mathf.Cos(angle);
        float y = center.y + radius * Mathf.Sin(angle);
        
        // 랜덤 오프셋 추가 (겹침 방지)
        Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
        
        return new Vector3(x + randomOffset.x, y + randomOffset.y, 0f);
    }
    
    /// <summary>
    /// Random 패턴 위치 계산
    /// </summary>
    private Vector3 CalculateRandomPosition(Vector3 center, float radius)
    {
        Vector2 randomPoint = Random.insideUnitCircle * radius;
        return center + new Vector3(randomPoint.x, randomPoint.y, 0f);
    }
    
    /// <summary>
    /// Line 패턴 위치 계산
    /// </summary>
    private Vector3 CalculateLinePosition(Vector3 center, Vector2 start, Vector2 end, int index, int totalCount)
    {
        float t = totalCount > 1 ? (float)index / (totalCount - 1) : 0.5f;
        Vector2 position = Vector2.Lerp(start, end, t);
        return center + new Vector3(position.x, position.y, 0f);
    }
    
    /// <summary>
    /// Grid 패턴 위치 계산
    /// </summary>
    private Vector3 CalculateGridPosition(Vector3 center, int rows, int columns, float spacing, int index)
    {
        int row = index / columns;
        int col = index % columns;
        
        float x = col * spacing - (columns - 1) * spacing / 2f;
        float y = row * spacing - (rows - 1) * spacing / 2f;
        
        return center + new Vector3(x, y, 0f);
    }
    
    /// <summary>
    /// ⭐⭐⭐ NavMesh 위의 유효한 위치 찾기 (모바일 최적화 버전)
    /// </summary>
    private Vector3 GetValidNavMeshPosition(Vector3 targetPosition, int maxAttempts = 5)
    {
        // 1차 시도: 원하는 위치 근처의 NavMesh 위치 찾기
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, 5f, NavMesh.AllAreas))
        {
            // ⭐ 경계에서 안쪽으로 이격 (1.5m 최소 거리 보장)
            Vector3 safePosition = EnsureDistanceFromEdge(hit.position, 1.5f);
            
                Dbg.Log($"✅ [WaveSpawner] NavMesh 안전 위치: {targetPosition} → {safePosition}");
            
            return safePosition;
        }
        
        // 2차 시도: 범위를 넓혀서 재시도 (5f → 10f → 15f)
        float[] searchRadii = { 10f, 15f };
        
        foreach (float searchRadius in searchRadii)
        {
            if (NavMesh.SamplePosition(targetPosition, out hit, searchRadius, NavMesh.AllAreas))
            {
                Vector3 safePosition = EnsureDistanceFromEdge(hit.position, 1.5f);
                
                    Dbg.Log($"⚠️ [WaveSpawner] NavMesh 위치 (범위 {searchRadius}f): {safePosition}");
                
                return safePosition;
            }
        }
        
        // 3차 시도: 다중 후보 중 경계에서 가장 먼 위치 선택 (모바일 최적화: 5개만)
        Vector3 spawnCenterPos = customSpawnCenter != null ? customSpawnCenter.position :
                                 playerTransform != null ? playerTransform.position :
                                 transform.position;
        
        Vector3 bestPosition = spawnCenterPos;
        float maxEdgeDistance = 0f;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            // 중심점 기준 랜덤 위치 생성
            Vector2 randomOffset = Random.insideUnitCircle * 8f;
            Vector3 randomPos = spawnCenterPos + new Vector3(randomOffset.x, randomOffset.y, 0f);
            
            if (NavMesh.SamplePosition(randomPos, out hit, 5f, NavMesh.AllAreas))
            {
                // 경계까지 거리 측정
                NavMeshHit edgeHit;
                if (NavMesh.FindClosestEdge(hit.position, out edgeHit, NavMesh.AllAreas))
                {
                    // 경계에서 가장 먼 위치 선택
                    if (edgeHit.distance > maxEdgeDistance)
                    {
                        maxEdgeDistance = edgeHit.distance;
                        bestPosition = hit.position;
                    }
                }
            }
        }
        
        if (maxEdgeDistance > 0f)
        {
                Dbg.Log($"⚠️ [WaveSpawner] 최적 위치 선택 (경계 거리: {maxEdgeDistance:F1}m): {bestPosition}");
            
            return bestPosition;
        }
        
        // 최종 fallback: 스폰 중심점 사용 (최악의 경우)
        Debug.LogWarning($"❌ [WaveSpawner] NavMesh 위치를 찾지 못함! 중심점 사용: {spawnCenterPos}");
        return spawnCenterPos;
    }
    
    /// <summary>
    /// ⭐ NavMesh 경계에서 안쪽으로 이격 보장 (모바일 최적화)
    /// </summary>
    private Vector3 EnsureDistanceFromEdge(Vector3 position, float minDistanceFromEdge)
    {
        NavMeshHit edgeHit;
        if (!NavMesh.FindClosestEdge(position, out edgeHit, NavMesh.AllAreas))
        {
            return position; // 경계를 찾지 못하면 원래 위치 유지
        }
        
        // 경계에 충분히 멀면 그대로 사용
        if (edgeHit.distance >= minDistanceFromEdge)
        {
            return position;
        }
        
        // 경계에 너무 가까움 → 안쪽으로 밀기
        float pushDistance = minDistanceFromEdge - edgeHit.distance + 0.3f; // 여유분 0.3m
        Vector3 safePosition = position + edgeHit.normal * pushDistance;
        
        // 이동된 위치가 NavMesh 위인지 검증 (빠른 fallback)
        NavMeshHit hit;
        if (NavMesh.SamplePosition(safePosition, out hit, 2f, NavMesh.AllAreas))
        {
                Dbg.Log($"🔧 [WaveSpawner] 경계 이격 보정: {edgeHit.distance:F2}m → {minDistanceFromEdge}m");
            
            return hit.position;
        }
        
        // 보정 실패 시 원래 위치 유지 (안전)
        return position;
    }
    
    /// <summary>
    /// 클리어 조건 체크 코루틴
    /// </summary>
    private IEnumerator CheckClearConditionCoroutine()
    {
        float startTime = Time.time;
        
            Dbg.Log($"🔄 [WaveSpawner] 클리어 조건 체크 시작 - 조건: {currentWaveData.clearCondition}, 총 스폰: {totalSpawnedCount}마리");
        
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // 0.5초마다 체크
            
            // 클리어 조건 체크
            bool isCleared = false;
            
            switch (currentWaveData.clearCondition)
            {
                case WaveClearCondition.KillAll:
                    isCleared = CheckKillAllCondition();
                    break;
                
                case WaveClearCondition.TimeLimit:
                    isCleared = Time.time - startTime >= currentWaveData.timeLimitSeconds;
                        Dbg.Log($"⏱️ [WaveSpawner] 시간 체크: {Time.time - startTime:F1}/{currentWaveData.timeLimitSeconds}초");
                    break;
                
                case WaveClearCondition.KillCount:
                    // 구현 가능 (특정 수 처치)
                    break;
            }
            
            if (isCleared)
            {
                CompleteWave();
                yield break;
            }
        }
    }
    
    /// <summary>
    /// KillAll 조건 체크
    /// </summary>
    private bool CheckKillAllCondition()
    {
        // null과 비활성 오브젝트 제거
        int beforeCount = spawnedMobs.Count;
        spawnedMobs.RemoveAll(mob => mob == null || !mob.activeInHierarchy);
        int afterCount = spawnedMobs.Count;
        
        // SimpleMob 중 살아있는 것만 카운트
        int aliveCount = 0;
        foreach (GameObject mob in spawnedMobs)
        {
            SimpleMob simpleMob = mob.GetComponent<SimpleMob>();
            if (simpleMob != null && !simpleMob.IsDead)
            {
                aliveCount++;
            }
        }
        
        bool isCleared = aliveCount == 0 && afterCount == 0;
        
        if (isCleared)
        {
            Dbg.Log($"✅ [WaveSpawner] 모든 몬스터 처치 완료! 웨이브 클리어!");
        }
        
        return isCleared;
    }
    
    /// <summary>
    /// 웨이브 완료
    /// </summary>
    private void CompleteWave()
    {
        isSpawning = false;
        
            Dbg.Log($"🏆 [WaveSpawner] Wave {currentWaveNumber} 완료!");
        
        // 보상 지급
        GiveRewards();
        
        // 이벤트 발동
        int listenerCount = OnWaveComplete?.GetInvocationList()?.Length ?? 0;
        Dbg.Log($"📣 [WaveSpawner] OnWaveComplete 이벤트 발동 - 구독자 {listenerCount}명");
        
        OnWaveComplete?.Invoke(currentWaveNumber);
        
        Dbg.Log($"✅ [WaveSpawner] 웨이브 완료 처리 끝!");
    }
    
    /// <summary>
    /// 보상 지급
    /// </summary>
    private void GiveRewards()
    {
        if (currentWaveData.rewardGold > 0)
        {
            PlayerDataManager.Instance.AddGold(currentWaveData.rewardGold);
            
                Dbg.Log($"💰 [WaveSpawner] 골드 획득: {currentWaveData.rewardGold}");
        }
        
        if (currentWaveData.rewardExp > 0)
        {
            PlayerDataManager.Instance.AddExp(currentWaveData.rewardExp);
            
                Dbg.Log($"⭐ [WaveSpawner] 경험치 획득: {currentWaveData.rewardExp}");
        }
    }
    
    /// <summary>
    /// 웨이브 중지
    /// </summary>
    public void StopWave()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        
        StopAllCoroutines();
        
        // 모든 스폰된 몬스터 제거
        foreach (GameObject mob in spawnedMobs)
        {
            if (mob != null && GamePoolManager.Instance != null)
            {
                GamePoolManager.Instance.ReturnToPool(mob.tag, mob);
            }
        }
        
        spawnedMobs.Clear();
        isSpawning = false;
        
            Dbg.Log("[WaveSpawner] 웨이브 강제 중지");
    }
    
    private void OnDrawGizmos()
    {
        if (!showSpawnGizmos || currentWaveData == null) return;
        
        Vector3 center = customSpawnCenter != null ? customSpawnCenter.position :
                         playerTransform != null ? playerTransform.position :
                         transform.position;
        
        // 스폰 패턴 시각화
        foreach (SpawnConfig config in currentWaveData.spawnConfigs)
        {
            Gizmos.color = Color.cyan;
            
            switch (config.spawnPattern)
            {
                case SpawnPattern.Circle:
                case SpawnPattern.Random:
                    Gizmos.DrawWireSphere(center, config.spawnRadius);
                    break;
                
                case SpawnPattern.Line:
                    Vector3 start = center + new Vector3(config.lineStart.x, config.lineStart.y, 0f);
                    Vector3 end = center + new Vector3(config.lineEnd.x, config.lineEnd.y, 0f);
                    Gizmos.DrawLine(start, end);
                    break;
            }
        }
    }
}

