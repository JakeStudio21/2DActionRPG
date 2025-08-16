using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

namespace StageSystem
{
    /// <summary>
    /// 스테이지 통합 관리자
    /// StageConfig 로드 → 풀 Warmup → 웨이브 시퀀스 실행하는 메인 컨트롤러
    /// </summary>
public class StageManager : MonoBehaviour
{
        public static StageManager Instance { get; private set; }
        
        [Header("스테이지 설정")]
        [SerializeField] private string currentStageId;
        [SerializeField] private StageConfig stageConfig;
        
        [Header("디버그")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool autoStartStage = false;
        
        // 컴포넌트 참조
        private WaveController waveController;
        private SpawnPointManager spawnPointManager;
        
        // 현재 상태
        private bool isStageActive = false;
        private int currentWaveIndex = 0;
        private float stageStartTime;
        
        // 이벤트
        public System.Action<StageConfig> OnStageStarted;
        public System.Action<StageConfig, bool> OnStageCompleted; // success
        public System.Action<WaveConfig> OnWaveChanged;
        
        // UI 업데이트 이벤트 추가
        public System.Action<GameObject> OnBossSpawned; // 보스 스폰 시
        public System.Action<int> OnEnemyKillCountChanged; // 처치수 변경 시
        public System.Action<float> OnStageTimeUpdated; // 타이머 업데이트
        
        // 킬 카운트 추적
        private int totalEnemyKillCount = 0;
        
        // 보스 처치 추적 변수 추가 (클래스 상단에)
        private bool isBossKilled = false;
        
        private void Awake()
        {
            // 싱글톤 설정
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            
            InitializeComponents();
        }
        
        private void Start()
        {
            if (autoStartStage && stageConfig != null)
            {
                StartStage(stageConfig);
            }
        }
        
        /// <summary>
        /// 컴포넌트 초기화
        /// </summary>
        private void InitializeComponents()
        {
            // WaveController 초기화
            waveController = GetComponent<WaveController>();
            if (waveController == null)
            {
                waveController = gameObject.AddComponent<WaveController>();
            }
            
            // SpawnPointManager 찾기
            spawnPointManager = FindObjectOfType<SpawnPointManager>();
            
            // 이벤트 구독
            if (waveController != null)
            {
                waveController.OnWaveCompleted += OnWaveCompleted;
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎮 [StageManager] 초기화 완료");
            }
        }
        
        /// <summary>
        /// StageConfig로 스테이지 시작
        /// </summary>
        public void StartStage(StageConfig config)
        {
            if (isStageActive)
            {
                Debug.LogWarning($"[StageManager] 이미 스테이지가 진행 중입니다: {stageConfig?.StageID}");
                return;
            }
            
            stageConfig = config;
            currentStageId = config.StageID;
            
            if (enableDebugLogs)
            {
                Debug.Log($"🚀 [StageManager] 스테이지 시작: {config.StageID} - {config.StageName}");
            }
            
            StartCoroutine(InitializeStage());
        }
        
        /// <summary>
        /// StageID로 스테이지 시작
        /// </summary>
        public void StartStage(string stageId)
        {
            StageConfig config = LoadStageConfig(stageId);
            if (config != null)
            {
                StartStage(config);
            }
            else
            {
                Debug.LogError($"[StageManager] StageConfig를 찾을 수 없습니다: {stageId}");
            }
        }
        
        /// <summary>
        /// 스테이지 초기화 프로세스
        /// </summary>
        private IEnumerator InitializeStage()
        {
            isStageActive = true;
            stageStartTime = Time.time;
            currentWaveIndex = 0;
            
            // 1단계: 풀 시스템 Warmup
            yield return StartCoroutine(WarmupPoolSystem());
            
            // 2단계: 스테이지 데이터 로드
            yield return StartCoroutine(LoadStageData());
            
            // 3단계: 첫 번째 웨이브 시작
            OnStageStarted?.Invoke(stageConfig);
            
            StartNextWave();
        }
        
        /// <summary>
        /// 풀 시스템 Warmup
        /// </summary>
        private IEnumerator WarmupPoolSystem()
        {
            if (enableDebugLogs)
            {
                Debug.Log($"🔥 [StageManager] 풀 시스템 Warmup 시작...");
            }
            
            // ScenePoolConfig가 있으면 사용, 없으면 StageConfig에서 필요한 풀 계산
            if (GamePoolManager.Instance.currentSceneConfig != null)
            {
                // 기존 풀 시스템 사용
                yield return new WaitForSeconds(0.1f); // 풀 로딩 대기
            }
            else
            {
                // StageConfig 기반 동적 풀 생성 (추후 구현)
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"[StageManager] ScenePoolConfig가 없습니다. 동적 풀 생성 생략.");
                }
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ [StageManager] 풀 시스템 Warmup 완료");
            }
        }
        
        /// <summary>
        /// 스테이지 데이터 로드
        /// </summary>
        private IEnumerator LoadStageData()
        {
            if (enableDebugLogs)
            {
                Debug.Log($"📂 [StageManager] 스테이지 데이터 로딩...");
            }
            
            // WaveConfig 자동 로드 시도
            if (stageConfig.WaveConfigs == null || stageConfig.WaveConfigs.Count == 0)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"⚠️ [StageManager] WaveConfigs가 비어있음, 자동 로드 시도...");
                }
                
                stageConfig.LoadWaveConfigs();
            }
            
            // WaveConfig 로드 및 검증
            if (stageConfig.WaveConfigs == null || stageConfig.WaveConfigs.Count == 0)
            {
                Debug.LogError($"[StageManager] WaveConfig가 없습니다: {stageConfig.StageID}");
                Debug.LogError($"[StageManager] 경로 확인: Resources/Stages/Waves/{stageConfig.StageID}_WAVE_XX_Config");
                yield break;
            }
            
            // DropTable 로드
            if (stageConfig.FirstClearDropTable == null)
            {
                Debug.LogWarning($"[StageManager] FirstClearDropTable이 없습니다: {stageConfig.StageID}");
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"📊 [StageManager] 로딩 완료: {stageConfig.WaveConfigs.Count}개 웨이브, " +
                         $"제한시간: {stageConfig.TimeLimitSec}초, 승리조건: {stageConfig.Victory}");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// 다음 웨이브 시작
        /// </summary>
        private void StartNextWave()
        {
            if (currentWaveIndex >= stageConfig.WaveConfigs.Count)
            {
                // 모든 웨이브 완료
                CompleteStage(true);
                return;
            }
            
            WaveConfig currentWave = stageConfig.WaveConfigs[currentWaveIndex];
            
            if (enableDebugLogs)
            {
                Debug.Log($"🌊 [StageManager] 웨이브 {currentWaveIndex + 1}/{stageConfig.WaveConfigs.Count} 시작: {currentWave.WaveID}");
            }
            
            OnWaveChanged?.Invoke(currentWave);
            waveController.ExecuteWave(currentWave);
        }
        
        /// <summary>
        /// 웨이브 완료 콜백에 보스 감지 로직 추가
        /// </summary>
        private void OnWaveCompleted(WaveConfig completedWave)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"✅ [StageManager] 웨이브 완료: {completedWave.WaveID}");
            }
            
            currentWaveIndex++;
            
            // 다음 웨이브에 보스가 있는지 체크
            if (currentWaveIndex < stageConfig.WaveConfigs.Count)
            {
                CheckForBossInNextWave();
            }
            
            // 승리 조건 체크
            if (CheckVictoryCondition())
            {
                CompleteStage(true);
            }
            else
            {
                // 다음 웨이브 시작
                StartNextWave();
            }
        }
        
        /// <summary>
        /// 다음 웨이브의 보스 체크
        /// </summary>
        private void CheckForBossInNextWave()
        {
            if (currentWaveIndex >= stageConfig.WaveConfigs.Count) return;
            
            var nextWave = stageConfig.WaveConfigs[currentWaveIndex];
            
            // 보스 몬스터 찾기
            foreach (var spawnGroup in nextWave.SpawnGroups)
            {
                foreach (var monster in spawnGroup.Monsters)
                {
                    // 보스 태그나 특정 조건으로 보스 판별
                    if (monster.MonsterPrefab != null &&   // prefab → MonsterPrefab
                        (monster.MonsterPrefab.name.Contains("Boss") || monster.MonsterPrefab.CompareTag("Boss")))
                    {
                        // 보스 발견 시 UI에 알림 (실제 스폰 시점에서 호출될 예정)
                        if (enableDebugLogs)
                            Debug.Log($"🐲 [StageManager] 다음 웨이브에 보스 발견: {monster.MonsterPrefab.name}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 승리 조건 체크
        /// </summary>
        private bool CheckVictoryCondition()
        {
            switch (stageConfig.Victory)
            {
                case VictoryCondition.KillAll:
                    // 모든 웨이브 완료 시 승리
                    return currentWaveIndex >= stageConfig.WaveConfigs.Count;
                    
                case VictoryCondition.BossKill:
                    // ✅ 보스 처치 확인 구현
                    return isBossKilled;
                    
                case VictoryCondition.Survival:
                    // 제한시간 생존 확인
                    float elapsedTime = Time.time - stageStartTime;
                    return elapsedTime >= stageConfig.TimeLimitSec;
                    
                case VictoryCondition.ObjectiveComplete:
                    // 특수 목표 달성 확인 (추후 구현)
                    return false;
                    
                default:
                    return currentWaveIndex >= stageConfig.WaveConfigs.Count;
            }
        }
        
        /// <summary>
        /// 패배 조건 체크
        /// </summary>
        private bool CheckDefeatCondition()
        {
            // 플레이어 사망 체크
            var playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null && playerHealth.isDead)  // IsDead() → isDead로 변경
            {
                return true;
            }
            
            // 제한시간 초과 (Survival 모드가 아닌 경우)
            if (stageConfig.Victory != VictoryCondition.Survival && stageConfig.TimeLimitSec > 0)
            {
                float elapsedTime = Time.time - stageStartTime;
                if (elapsedTime >= stageConfig.TimeLimitSec)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 스테이지 완료
        /// </summary>
        private void CompleteStage(bool success)
        {
            if (!isStageActive) return;
            
            isStageActive = false;
            
            // 진행 중인 웨이브 정지
            if (waveController != null)
            {
                waveController.StopCurrentWave();
            }
            
            float clearTime = Time.time - stageStartTime;
            
            if (enableDebugLogs)
            {
                Debug.Log($"🏁 [StageManager] 스테이지 {(success ? "성공" : "실패")}: " +
                         $"{stageConfig.StageID} (소요시간: {clearTime:F1}초)");
            }
            
            // 이벤트 발생
            OnStageCompleted?.Invoke(stageConfig, success);
            
            // 진행도 저장 (성공 시만)
            if (success)
            {
                SaveStageProgress(clearTime);
                
                // FSMStageController에 승리 알림
                if (FSMStageController.Instance != null)
                {
                    FSMStageController.Instance.TriggerVictory();
                }
            }
            else
            {
                // FSMStageController에 패배 알림
                if (FSMStageController.Instance != null)
                {
                    FSMStageController.Instance.TriggerDefeat();
                }
            }
        }
        
        /// <summary>
        /// 스테이지 진행도 저장
        /// </summary>
        private void SaveStageProgress(float clearTime)
        {
            if (StageProgressManager.Instance != null)
            {
                int clearTimeInt = Mathf.RoundToInt(clearTime);
                StageProgressManager.Instance.CompleteStage(stageConfig.StageID, clearTimeInt);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"💾 [StageManager] 진행도 저장: {stageConfig.StageID} - {clearTimeInt}초");
                }
            }
        }
        
        /// <summary>
        /// StageConfig 로드
        /// </summary>
        private StageConfig LoadStageConfig(string stageId)
        {
            string path = $"Stages/Configs/{stageId}_Config";
            StageConfig config = Resources.Load<StageConfig>(path);
            
            if (config == null)
            {
                Debug.LogError($"[StageManager] StageConfig 로드 실패: {path}");
            }
            
            return config;
        }
        
        /// <summary>
        /// 스테이지 강제 종료
        /// </summary>
        public void ForceStopStage()
        {
            if (isStageActive)
            {
                CompleteStage(false);
            }
        }
    
        
        /// <summary>
        /// Update에서 패배 조건 체크
        /// </summary>
        private void Update()
        {
            if (isStageActive && CheckDefeatCondition())
            {
                CompleteStage(false);
            }
        }
        
        // 공개 속성들
        public bool IsStageActive => isStageActive;
        public StageConfig CurrentStage => stageConfig;
        public int CurrentWaveIndex => currentWaveIndex;
        public float StageElapsedTime => isStageActive ? Time.time - stageStartTime : 0f;
        public WaveController WaveController => waveController;
        
        /// <summary>
        /// 몬스터 스폰 (위치 분산 적용) - WaveController에서도 사용 가능하도록 public
        /// </summary>
        public void SpawnMonstersWithDistribution(SpawnGroup spawnGroup, SpawnPoint spawnPoint)
        {
            var monsters = spawnGroup.Monsters;
            int totalMonsters = monsters.Sum(m => m.Count);
            int currentIndex = 0;
            
            foreach (var monsterData in monsters)
            {
                for (int i = 0; i < monsterData.Count; i++)
                {
                    // 분산된 스폰 위치 계산
                    Vector3 spawnPos = spawnPoint.GetDistributedSpawnPosition(currentIndex, totalMonsters);
                    
                    // 몬스터 스폰
                    GameObject monster = SpawnMonster(monsterData, spawnPos);
                    
                    if (monster != null)
                    {
                        // BaseEnemy 컴포넌트 찾기 및 Home 설정
                        BaseEnemy enemyComponent = monster.GetComponent<BaseEnemy>();
                        if (enemyComponent != null)
                        {
                            // Home 위치와 순찰 반경 설정
                            enemyComponent.SetHomePosition(spawnPos, spawnPoint.PatrolRadius);
                            
                            if (enableDebugLogs)
                            {
                                Debug.Log($"[StageManager] {monster.name} 스폰: {spawnPos}, Patrol: {spawnPoint.PatrolRadius}");
                            }
                        }
                        
                        // 🔑 WaveController 연동 부분 제거 (에러 원인)
                        // if (WaveController != null && WaveController.CurrentWaveEnemies != null)
                        // {
                        //     WaveController.CurrentWaveEnemies.Add(monster);
                        // }
                    }
                    
                    currentIndex++;
                }
            }
        }
        
        /// <summary>
        /// 개별 몬스터 스폰 - 실제 GamePoolManager API 사용
        /// </summary>
        public GameObject SpawnMonster(MonsterSpawnData monsterData, Vector3 position)
        {
            string poolTag = GetPoolTagFromMonsterID(monsterData.MonsterID);
            
            // 실제 GamePoolManager API 사용: SpawnFromPool
            if (GamePoolManager.Instance != null)
            {
                // GamePoolManager의 실제 메서드: SpawnFromPool 사용
                GameObject spawnedObject = GamePoolManager.Instance.SpawnFromPool(poolTag, position, Quaternion.identity);
                
                if (spawnedObject != null)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[StageManager] 풀에서 스폰 성공: {poolTag} at {position}");
                    }
                    return spawnedObject;
                }
                else
                {
                    if (enableDebugLogs)
                    {
                        Debug.LogWarning($"[StageManager] 풀에서 스폰 실패: {poolTag}");
                    }
                }
            }
            
            // 풀에서 실패하면 프리팹 직접 생성 (fallback)
            if (monsterData.MonsterPrefab != null)
            {
                GameObject directSpawn = Instantiate(monsterData.MonsterPrefab, position, Quaternion.identity);
                if (enableDebugLogs)
                {
                    Debug.Log($"[StageManager] 직접 생성: {directSpawn.name} at {position}");
                }
                return directSpawn;
            }
            
            Debug.LogError($"[StageManager] 몬스터 스폰 완전 실패: {monsterData.MonsterID}");
            return null;
        }

        /// <summary>
        /// 몬스터 ID로부터 풀 태그 생성
        /// </summary>
        private string GetPoolTagFromMonsterID(string monsterID)
        {
            // 몬스터 ID에서 풀 태그 추출
            // 예: MON_BLUESLIME_001 → BlueSlime
            if (monsterID.Contains("BLUESLIME"))
                return "BlueSlime";
            else if (monsterID.Contains("GRAPE"))
                return "Enemie1"; // Grape의 실제 풀 태그
            else if (monsterID.Contains("GHOST"))
                return "Ghost";
            else
                return monsterID; // 기본적으로 그대로 반환
        }

        /// <summary>
        /// 적 처치 시 호출되는 메서드 (WaveController에서 호출)
        /// </summary>
        public void NotifyEnemyKilled(GameObject enemy)
        {
            totalEnemyKillCount++;
            
            // 킬 카운트 UI 업데이트 이벤트
            OnEnemyKillCountChanged?.Invoke(totalEnemyKillCount);
            
            // ⭐ 보스 사망 체크 구현
            var enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null && enemyHealth.IsBoss())
            {
                isBossKilled = true;
                
                if (enableDebugLogs)
                    Debug.Log($"🐲 [StageManager] 보스 처치됨: {enemy.name} - 승리 조건 달성!");
                
                // 즉시 승리 조건 체크
                if (CheckVictoryCondition())
                {
                    CompleteStage(true);
                }
            }
            
            if (enableDebugLogs)
                Debug.Log($"🎯 [StageManager] 총 처치수: {totalEnemyKillCount}");
        }
        
        /// <summary>
        /// 보스 스폰 시 호출되는 메서드
        /// </summary>
        public void NotifyBossSpawned(GameObject bossObject)
        {
            if (enableDebugLogs)
                Debug.Log($"🐲 [StageManager] 보스 스폰: {bossObject.name}");
            
            // 보스 스폰 UI 이벤트
            OnBossSpawned?.Invoke(bossObject);
        }
    }  // StageManager 클래스 닫기
}      // StageSystem 네임스페이스 닫기