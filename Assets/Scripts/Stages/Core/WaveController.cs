using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 웨이브 실행 및 그룹 처리 로직
    /// WaveConfig 데이터를 기반으로 순차/병렬 몬스터 스폰 관리
    /// </summary>
public class WaveController : MonoBehaviour
{
        [Header("웨이브 설정")]
        public bool enableDebugLogs = true;
        public bool showSpawnGizmos = true;
        
        [Header("스폰 제어")]
        [SerializeField] private float groupSpawnDelay = 0.5f;
        // [SerializeField] private float monsterSpawnInterval = 0.5f; // 사용하지 않으므로 주석처리
        
        // 이벤트
        public System.Action<WaveConfig> OnWaveStarted;
        public System.Action<WaveConfig> OnWaveCompleted;
        public System.Action<SpawnGroup> OnGroupSpawned;
        public System.Action<SpawnGroup> OnGroupCompleted;
        
        // 적 사망 이벤트 (public으로 변경)
        public System.Action<GameObject> OnEnemyDeath;
        
        // 현재 상태
        private WaveConfig currentWave;
        private List<GameObject> currentWaveEnemies = new List<GameObject>();
        private Dictionary<SpawnGroup, List<GameObject>> groupEnemies = new Dictionary<SpawnGroup, List<GameObject>>();
        private bool isWaveActive = false;
        
        // 외부 시스템 참조
        private SpawnPointManager spawnPointManager;
        
        private void Awake()
        {
            spawnPointManager = FindObjectOfType<SpawnPointManager>();
            
            if (spawnPointManager == null)
            {
                Debug.LogError("[WaveController] SpawnPointManager를 찾을 수 없습니다!");
            }

            // 적 사망 이벤트 구독
            OnEnemyDeath += HandleEnemyDeath;
        }
        
        /// <summary>
        /// 웨이브 실행
        /// </summary>
        public void ExecuteWave(WaveConfig waveConfig)
        {
            if (isWaveActive)
            {
                Debug.LogWarning($"[WaveController] 이미 웨이브가 진행 중입니다: {currentWave?.WaveID}");
                return;
            }
            
            currentWave = waveConfig;
            isWaveActive = true;
            currentWaveEnemies.Clear();
            groupEnemies.Clear();
            
            if (enableDebugLogs)
            {
                Debug.Log($"🌊 [WaveController] 웨이브 시작: {waveConfig.WaveID} ({waveConfig.SpawnGroups.Count}개 그룹)");
            }
            
            OnWaveStarted?.Invoke(waveConfig);
            
            // 웨이브 시작 조건에 따른 처리
            switch (waveConfig.StartCondition)
            {
                case WaveStartCondition.AutoAfterDelay:
                    StartCoroutine(ExecuteWaveWithDelay(waveConfig.WaveDelaySec));
                    break;
                    
                case WaveStartCondition.OnClearPrev:
                    // 이전 웨이브 완료 대기 로직 (StageManager에서 처리)
                    StartCoroutine(ExecuteWaveWithDelay(0));
                    break;
                    
                case WaveStartCondition.OnTrigger:
                    // 트리거 대기 상태 (외부에서 TriggerWave 호출)
                    if (enableDebugLogs)
                        Debug.Log($"🌊 [WaveController] 트리거 대기 중: {waveConfig.TriggerId}");
                    break;
            }
        }
        
        /// <summary>
        /// 트리거 기반 웨이브 시작
        /// </summary>
        public void TriggerWave(WaveTriggerId triggerId)
        {
            if (currentWave != null && currentWave.TriggerId == triggerId)
            {
                if (enableDebugLogs)
                    Debug.Log($"🎯 [WaveController] 트리거 발동: {triggerId}");
                    
                StartCoroutine(ExecuteWaveWithDelay(0));
            }
        }
        
        /// <summary>
        /// 딜레이 후 웨이브 실행
        /// </summary>
        private IEnumerator ExecuteWaveWithDelay(int delaySec)
        {
            if (delaySec > 0)
            {
                if (enableDebugLogs)
                    Debug.Log($"⏰ [WaveController] {delaySec}초 대기 중...");
                    
                yield return new WaitForSeconds(delaySec);
            }
            
            // 각 그룹별 스폰 실행
            foreach (var group in currentWave.SpawnGroups)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"📦 [WaveController] 그룹 스폰 시작: {group.SpawnGroupID} ({group.Monsters.Count}종 몬스터)");
                }
                
                // 🆕 SpawnPoint에서 직접 위치 계산 (SpawnShapeCalculator 제거)
                // List<Vector3> spawnPositions = SpawnShapeCalculator.CalculateSpawnPositions(
                //     group, spawnCenter, totalMonsters);
                
                // 디버그 시각화 제거 (SpawnShapeCalculator 의존성 제거)
                // if (showSpawnGizmos)
                // {
                //     SpawnShapeCalculator.DrawSpawnPositions(spawnPositions, Color.green, 3f);
                // }
                
                // 몬스터별 스폰 (분산 스폰 적용)
                foreach (var monsterData in group.Monsters)
                {
                    // SpawnPoint 찾기
                    SpawnPoint targetSpawnPoint = FindSpawnPointByGroupID(group.SpawnGroupID);
                    if (targetSpawnPoint == null)
                    {
                        Debug.LogWarning($"⚠️ [WaveController] SpawnGroup '{group.SpawnGroupID}'에 할당된 SpawnPoint 없음!");
                        continue;
                    }
                    
                    // 🔑 분산 스폰 적용 (각 몬스터마다 다른 위치)
                    for (int i = 0; i < monsterData.Count; i++)
                    {
                        Vector3 spawnPosition;
                        
                        // StageManager의 분산 스폰 로직 사용
                        if (StageManager.Instance != null && targetSpawnPoint.GetComponent<SpawnPoint>() != null)
                        {
                            // 분산된 위치 계산
                            spawnPosition = targetSpawnPoint.GetDistributedSpawnPosition(i, monsterData.Count);
                        }
                        else
                        {
                            // fallback: 기존 방식
                            spawnPosition = targetSpawnPoint.GetSpawnPosition();
                        }
                        
                        GameObject enemy = null;
                        if (StageManager.Instance != null)
                        {
                            // StageManager의 SpawnMonster 사용 (Cue 시스템 포함)
                            enemy = StageManager.Instance.SpawnMonster(monsterData, spawnPosition);
                        }
                        else
                        {
                            // fallback: 기존 방식
                            enemy = SpawnMonster(monsterData.MonsterID, spawnPosition);
                        }
                        
                        if (enemy != null)
                        {
                            // 🔑 BaseEnemy Home 위치 설정 추가
                            BaseEnemy enemyComponent = enemy.GetComponent<BaseEnemy>();
                            if (enemyComponent != null)
                            {
                                enemyComponent.SetHomePosition(spawnPosition, targetSpawnPoint.PatrolRadius);
                                
                                if (enableDebugLogs)
                                {
                                    Debug.Log($"[WaveController] {enemy.name} 스폰: {spawnPosition}, Patrol: {targetSpawnPoint.PatrolRadius}");
                                }
                            }
                            
                            currentWaveEnemies.Add(enemy);
                        }
                        
                        yield return new WaitForSeconds(groupSpawnDelay);
                    }
                }
            }
            
            // 🆕 웨이브 완료 대기 시작
            if (enableDebugLogs)
            {
                Debug.Log($"⏳ [WaveController] 웨이브 완료 대기 시작: {currentWaveEnemies.Count}마리 적");
            }
            
            StartCoroutine(WaitForWaveCompletion());
        }
        
        /// <summary>
        /// 모든 스폰 그룹 실행
        /// </summary>
        private IEnumerator SpawnAllGroups()
        {
            for (int i = 0; i < currentWave.SpawnGroups.Count; i++)
            {
                SpawnGroup group = currentWave.SpawnGroups[i];
                
                if (enableDebugLogs)
                {
                    Debug.Log($"👥 [WaveController] 그룹 {i+1}/{currentWave.SpawnGroups.Count} 스폰: {group.SpawnGroupID}");
                }
                
                // 그룹 스폰 딜레이
                if (group.SpawnDelaySec > 0)
                {
                    yield return new WaitForSeconds(group.SpawnDelaySec);
                }
                
                // 그룹 스폰 실행
                yield return StartCoroutine(SpawnGroup(group));
                
                // 그룹 간 딜레이
                if (i < currentWave.SpawnGroups.Count - 1)
                {
                    yield return new WaitForSeconds(groupSpawnDelay);
                }
            }
            
            // 웨이브 완료 대기
            StartCoroutine(WaitForWaveCompletion());
        }
        
        /// <summary>
        /// 개별 스폰 그룹 실행
        /// </summary>
        private IEnumerator SpawnGroup(SpawnGroup group)
        {
            List<GameObject> groupEnemyList = new List<GameObject>();
            groupEnemies[group] = groupEnemyList;
            
            // 반복 스폰 처리
            int repeatCount = Mathf.Max(1, Mathf.RoundToInt(group.RepeatCount));
            
            for (int repeat = 0; repeat < repeatCount; repeat++)
            {
                if (enableDebugLogs && repeatCount > 1)
                {
                    Debug.Log($"🔄 [WaveController] 반복 {repeat+1}/{repeatCount}");
                }
                
                // 스폰 위치 계산
                Vector3 spawnCenter = GetGroupSpawnCenter(group);
                int totalMonsters = group.Monsters.Sum(m => m.Count);
                
                // 디버그 시각화 제거 (SpawnShapeCalculator 의존성 제거)
                // if (showSpawnGizmos)
                // {
                //     SpawnShapeCalculator.DrawSpawnPositions(spawnPositions, Color.green, 3f);
                // }
                
                // 몬스터별 스폰 (SpawnPoint 직접 사용)
                foreach (var monsterData in group.Monsters)
                {
                    for (int i = 0; i < monsterData.Count; i++)
                    {
                        // SpawnPoint에서 직접 위치 계산
                        SpawnPoint targetSpawnPoint = FindSpawnPointByGroupID(group.SpawnGroupID);
                        if (targetSpawnPoint == null)
                        {
                            Debug.LogWarning($"⚠️ [WaveController] SpawnGroup '{group.SpawnGroupID}'에 할당된 SpawnPoint 없음!");
                            continue;
                        }
                        Vector3 spawnPosition = targetSpawnPoint.GetSpawnPosition();
                        
                        GameObject enemy = SpawnMonster(monsterData.MonsterID, spawnPosition);
                        
                        if (enemy != null)
                        {
                            groupEnemyList.Add(enemy);
                            currentWaveEnemies.Add(enemy);
                        }
                        
                        yield return new WaitForSeconds(groupSpawnDelay);
                    }
                }
                
                // 반복 간 대기
                if (repeat < repeatCount - 1 && group.RepeatIntervalSec > 0)
                {
                    yield return new WaitForSeconds(group.RepeatIntervalSec);
                }
            }
            
            OnGroupSpawned?.Invoke(group);
        }
        
        /// <summary>
        /// 몬스터 스폰 실행
        /// </summary>
        private GameObject SpawnMonster(string monsterId, Vector3 position)
        {
            // MonsterIdMapper를 통한 풀 태그 변환
            string poolTag = MonsterIdMapper.GetPoolTag(monsterId);
            
            if (string.IsNullOrEmpty(poolTag))
            {
                Debug.LogError($"[WaveController] 몬스터 ID를 풀 태그로 변환 실패: {monsterId}");
                return null;
            }
            
            // GamePoolManager를 통한 스폰
            GameObject enemy = GamePoolManager.Instance.SpawnFromPool(poolTag, position, Quaternion.identity);
            
            if (enemy != null)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"👹 [WaveController] 몬스터 스폰: {monsterId} → {poolTag} at {position}");
                }
                
                // ⭐ 보스 전용 EnemyData 동적 할당
                if (monsterId.Contains("BOSS"))
                {
                    AssignBossData(enemy, monsterId);
                }
                
                // 몬스터 사망 이벤트 구독
                var enemyHealth = enemy.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.OnEnemyDeath += () => OnEnemyDeath(enemy);
                    
                    // ⭐ 보스 감지 및 UI 알림 추가
                    if (enemyHealth.IsBoss())
                    {
                        Debug.Log($"🐲 [WaveController] 보스 스폰 감지: {enemy.name}");
                        
                        // StageManager에 보스 스폰 알림
                        if (StageManager.Instance != null)
                        {
                            StageManager.Instance.NotifyBossSpawned(enemy);
                        }
                    }
                }
            }
            else
            {
                Debug.LogError($"[WaveController] 몬스터 스폰 실패: {poolTag}");
            }
            
            return enemy;
        }
        
        /// <summary>
        /// 보스 전용 EnemyData 동적 할당 (신규 추가)
        /// </summary>
        private void AssignBossData(GameObject enemy, string monsterId)
        {
            var baseEnemy = enemy.GetComponent<BaseEnemy>();
            if (baseEnemy == null)
            {
                Debug.LogWarning($"[WaveController] {enemy.name}에서 BaseEnemy 컴포넌트를 찾을 수 없습니다!");
                return;
            }
            
            // 보스 EnemyData 경로 매핑
            string bossDataPath = GetBossDataPath(monsterId);
            if (string.IsNullOrEmpty(bossDataPath))
            {
                Debug.LogWarning($"[WaveController] 보스 데이터 경로를 찾을 수 없습니다: {monsterId}");
                return;
            }
            
            // Resources에서 보스 EnemyData 로드
            EnemyData bossData = Resources.Load<EnemyData>(bossDataPath);
            if (bossData != null)
            {
                // BaseEnemy의 EnemyData를 동적으로 변경
                baseEnemy.SetEnemyData(bossData);
                
                Debug.Log($"🐲 [WaveController] 보스 데이터 할당 성공: {enemy.name} → {bossDataPath}");
            }
            else
            {
                Debug.LogError($"[WaveController] 보스 데이터 로드 실패: {bossDataPath}");
            }
        }
        
        /// <summary>
        /// MonsterID로부터 보스 EnemyData 경로 생성
        /// </summary>
        private string GetBossDataPath(string monsterId)
        {
            if (monsterId.Contains("BLUESLIME") && monsterId.Contains("BOSS"))
            {
                return "EnemyData/BlueSlime_BossData"; // 실제 경로에 맞게 수정
            }
            // 다른 보스들도 추가 가능
            
            return null;
        }
        
        /// <summary>
        /// 스폰 그룹의 중심 위치 계산 (단순화)
        /// </summary>
        private Vector3 GetGroupSpawnCenter(SpawnGroup group)
        {
            // SpawnPoint 기반으로 위치 계산
            SpawnPoint targetSpawnPoint = FindSpawnPointByGroupID(group.SpawnGroupID);
            if (targetSpawnPoint != null)
            {
                return targetSpawnPoint.transform.position;
            }
            
            // Fallback: 랜덤 위치
            var spawnPointManager = FindObjectOfType<SpawnPointManager>();
            if (spawnPointManager != null)
            {
                return spawnPointManager.GetRandomSpawnPosition();
            }
            
            return Vector3.zero;
        }
        
        /// <summary>
        /// SpawnGroup에 맞는 유효한 스폰 위치 계산 (최종 방식 - SpawnPoint 직접 사용)
        /// </summary>
        private Vector3 GetValidSpawnPosition(SpawnGroup spawnGroup)
        {
            // SpawnGroupID가 할당된 SpawnPoint 찾기
            SpawnPoint targetSpawnPoint = FindSpawnPointByGroupID(spawnGroup.SpawnGroupID);
            
            if (targetSpawnPoint != null)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"🎯 [WaveController] '{spawnGroup.SpawnGroupID}' → SpawnPoint '{targetSpawnPoint.spawnPointID}' 매칭");
                }
                
                // SpawnPoint의 설정을 사용하여 위치 계산
                return targetSpawnPoint.GetSpawnPosition();
            }
            
            // Fallback: 랜덤 SpawnPoint
            if (enableDebugLogs)
            {
                Debug.LogWarning($"⚠️ [WaveController] SpawnGroup '{spawnGroup.SpawnGroupID}'에 할당된 SpawnPoint 없음. 랜덤 사용.");
            }
            return GetRandomSpawnPosition();
        }
        
        /// <summary>
        /// SpawnGroupID가 할당된 SpawnPoint 찾기 (런타임 매칭)
        /// </summary>
        private SpawnPoint FindSpawnPointByGroupID(string spawnGroupID)
        {
            SpawnPoint[] allSpawnPoints = FindObjectsOfType<SpawnPoint>();
            
            foreach (var spawnPoint in allSpawnPoints)
            {
                if (spawnPoint.isActive && 
                    !string.IsNullOrEmpty(spawnPoint.assignedSpawnGroupID) &&
                    spawnPoint.assignedSpawnGroupID == spawnGroupID)
                {
                    return spawnPoint;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 랜덤 SpawnPoint 위치 반환
        /// </summary>
        private Vector3 GetRandomSpawnPosition()
        {
            SpawnPointManager spawnPointManager = FindObjectOfType<SpawnPointManager>();
            if (spawnPointManager != null)
            {
                return spawnPointManager.GetRandomSpawnPosition();
            }
            
            // 최종 Fallback: 플레이어 주변
            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                Vector2 randomOffset = Random.insideUnitCircle * 5f;
                return player.transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);
            }
            
            return Vector3.zero;
        }
        
        /// <summary>
        /// 적 사망 처리 (이름 변경)
        /// </summary>
        private void HandleEnemyDeath(GameObject enemy)
        {
            currentWaveEnemies.Remove(enemy);
            
            // 그룹별 적 제거
            foreach (var groupPair in groupEnemies.ToList())
            {
                if (groupPair.Value.Contains(enemy))
                {
                    groupPair.Value.Remove(enemy);
                    
                    // 그룹 완료 체크
                    if (groupPair.Value.Count == 0)
                    {
                        OnGroupCompleted?.Invoke(groupPair.Key);
                        
                        if (enableDebugLogs)
                        {
                            Debug.Log($"✅ [WaveController] 그룹 완료: {groupPair.Key.SpawnGroupID}");
                        }
                    }
                    break;
                }
            }
        }
        
        /// <summary>
        /// 웨이브 완료 대기
        /// </summary>
        private IEnumerator WaitForWaveCompletion()
        {
            while (currentWaveEnemies.Count > 0)
            {
                yield return new WaitForSeconds(0.5f);
            }
            
            // 웨이브 완료
            isWaveActive = false;
            
            if (enableDebugLogs)
            {
                Debug.Log($"🏆 [WaveController] 웨이브 완료: {currentWave.WaveID}");
            }
            
            OnWaveCompleted?.Invoke(currentWave);
        }
        
        /// <summary>
        /// 현재 웨이브 강제 정지
        /// </summary>
        public void StopCurrentWave()
        {
            if (isWaveActive)
            {
                StopAllCoroutines();
                
                // 모든 적 정리
                foreach (GameObject enemy in currentWaveEnemies.ToList())
                {
                    if (enemy != null)
                    {
                        // 풀 태그 결정 필요
                        string poolTag = DetermineEnemyPoolTag(enemy);
                        if (!string.IsNullOrEmpty(poolTag))
                        {
                            GamePoolManager.Instance.ReturnToPool(poolTag, enemy);
                        }
                        else
                        {
                            // 풀 태그를 찾을 수 없으면 파괴
                            Destroy(enemy);
                        }
                    }
                }
                
                currentWaveEnemies.Clear();
                groupEnemies.Clear();
                isWaveActive = false;
                
                Debug.Log($"⏹️ [WaveController] 웨이브 강제 정지: {currentWave?.WaveID}");
            }
        }
        
        /// <summary>
        /// 적 오브젝트로부터 풀 태그 결정
        /// </summary>
        private string DetermineEnemyPoolTag(GameObject enemy)
        {
            // 오브젝트 이름으로부터 풀 태그 추정
            string objectName = enemy.name.Replace("(Clone)", "").Trim();
            
            // 숫자 suffix 제거 (예: "BlueSlime_0" → "BlueSlime")
            int underscoreIndex = objectName.LastIndexOf('_');
            if (underscoreIndex > 0)
            {
                string afterUnderscore = objectName.Substring(underscoreIndex + 1);
                if (int.TryParse(afterUnderscore, out _))
                {
                    objectName = objectName.Substring(0, underscoreIndex);
                }
            }
            
            return objectName;
        }
        
        /// <summary>
        /// 현재 웨이브 상태 정보
        /// </summary>
        public bool IsWaveActive => isWaveActive;
        public int CurrentWaveEnemyCount => currentWaveEnemies.Count;
        public WaveConfig CurrentWave => currentWave;
    }
}

