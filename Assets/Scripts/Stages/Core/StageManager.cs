using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using CueSystem; // ✅ 추가

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
            
            // ✅ Boss Portal 초기 숨김 처리
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
#endif
                var allAreaExits = FindObjectsOfType<AreaExit>();
                foreach (var areaExit in allAreaExits)
                {
                    if (areaExit != null && (areaExit.name.Contains("BossPortal") || 
                        areaExit.name.Contains("Portal") ||
                        areaExit.CompareTag("BossPortal")))
                    {
                        areaExit.gameObject.SetActive(false);
                    }
                }
#if UNITY_EDITOR
            }
#endif
            
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
            
            // ✅ 풀 로딩 완료 후 스테이지 입장 이펙트 발행
            EmitStageEnterCues();
            
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
            
            // ✅ GamePoolManager가 로딩 중일 때만 대기
            if (GamePoolManager.Instance != null)
            {
                float timeout = 5f; // 최대 5초 대기
                float elapsed = 0f;
                
                while (GamePoolManager.Instance.IsLoadingPools && elapsed < timeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsed += 0.1f;
                }
                
                if (elapsed >= timeout)
                {
                    Debug.LogWarning($"⚠️ [StageManager] 풀 로딩 타임아웃! 강제 진행합니다.");
                }
                
                // 추가 안전 대기 (풀 로딩 완료 확인)
                yield return new WaitForSeconds(0.1f);
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
            
            // ✅ 🎵 웨이브 시작 이펙트 발행
            EmitWaveStartCues(currentWave);
            
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
            
            // ✅ 🎵 웨이브 완료 이펙트 발행
            EmitWaveCompleteCues(completedWave);
            
            currentWaveIndex++;

            // ✅ Boss Gate 활성화 체크
            if (completedWave.EnablesBossGate)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
#endif
                    var allAreaExits = FindObjectsOfType<AreaExit>(true);
                    foreach (var areaExit in allAreaExits)
                    {
                        if (areaExit != null && areaExit.name.Contains("BossPortal"))
                        {
                            areaExit.gameObject.SetActive(true);
                        }
                    }
#if UNITY_EDITOR
                }
#endif
            }
            
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
            // 🔍 디버깅: 스테이지 활성 상태 체크
            if (!isStageActive)
            {
                return false; // 스테이지가 비활성 상태면 패배 조건 체크 안함
            }
            
            // 🔍 디버깅: stageConfig 유효성 체크
            if (stageConfig == null)
            {
                Debug.LogError("🔴 [StageManager] CheckDefeatCondition - stageConfig가 null입니다!");
                return false; // stageConfig가 없으면 패배 조건 체크 안함
            }
            
            // 플레이어 사망 체크 (이미 Update에서 PlayerHealth 존재 확인함)
            var playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth != null && playerHealth.isDead)
            {
                Debug.Log($"💀 [StageManager] 패배 감지 - 플레이어 사망! isDead = {playerHealth.isDead}");
                return true;
            }
            
            // 제한시간 초과 (Survival 모드가 아닌 경우)
            if (stageConfig.Victory != VictoryCondition.Survival && stageConfig.TimeLimitSec > 0)
            {
                float elapsedTime = Time.time - stageStartTime;
                if (elapsedTime >= stageConfig.TimeLimitSec)
                {
                    Debug.Log($"⏰ [StageManager] 패배 감지 - 제한시간 초과! {elapsedTime:F1}초 >= {stageConfig.TimeLimitSec}초");
                    return true;
                }
                
                // 🔍 디버깅: 시간 정보 주기적 출력 (10초마다)
                if (Mathf.FloorToInt(elapsedTime) % 10 == 0 && elapsedTime > 0)
                {
                    Debug.Log($"⏰ [StageManager] 경과시간: {elapsedTime:F1}초 / {stageConfig.TimeLimitSec}초");
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
            
            // ✅ 🎵 스테이지 완료 이펙트 발행
            EmitStageCompleteCues(success);
            
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
            
            // 성공 시 보상 처리 및 진행도 저장
            if (success)
            {
                ProcessStageRewards(clearTime);
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
        /// 스테이지 보상 처리
        /// </summary>
        private void ProcessStageRewards(float clearTime)
        {
            if (RewardSystem.Instance == null)
            {
                Debug.LogWarning("[StageManager] RewardSystem이 없습니다. 보상 처리를 건너뜁니다.");
                return;
            }
            
            // 진행도 확인하여 첫 클리어 여부 판단
            bool isFirstClear = false;
            if (StageProgressManager.Instance != null)
            {
                var progress = StageProgressManager.Instance.GetStageProgress(stageConfig.StageID);
                isFirstClear = progress != null && !progress.isFirstClearRewarded;
            }
            
            // 보상 처리
            var rewardResult = RewardSystem.Instance.ProcessStageRewards(stageConfig, isFirstClear, clearTime);
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎁 [StageManager] 보상 처리 완료: 골드 {rewardResult.Gold}, EXP {rewardResult.Exp}");
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
            // ✅ 추가: 플레이어가 스폰되기 전에는 패배 조건 체크하지 않음
            var playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth == null)
            {
                return; // PlayerHealth가 없으면 패배 조건 체크 안함
            }
            
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
        /// 개별 몬스터 스폰 - 단순화된 EnemyData 기반 시스템
        /// </summary>
        public GameObject SpawnMonster(MonsterSpawnData monsterData, Vector3 position)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"🎯 [StageManager] 몬스터 스폰 요청: {monsterData.MonsterID} at {position}");
            }
            
            // 1단계: EnemyData에서 프리팹 가져오기
            EnemyData enemyData = GetEnemyDataFromMonsterID(monsterData.MonsterID);
            GameObject prefabToSpawn = null;
            
            if (enemyData != null)
            {
                prefabToSpawn = enemyData.GetPoolingPrefab();
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.LogError($"❌ [StageManager] EnemyData 로드 실패: {monsterData.MonsterID}");
                }
            }
            
            // 2단계: 풀링 시스템을 통한 스폰 (올바른 풀 태그 사용)
            if (prefabToSpawn != null)
            {
                // MonsterID → 풀 태그 매핑 사용
                string poolTag = GetPoolTagFromMonsterID(monsterData.MonsterID);
                
                if (GamePoolManager.Instance != null)
                {
                    GameObject spawnedObject = GamePoolManager.Instance.SpawnFromPool(poolTag, position, Quaternion.identity);
                    
                    if (spawnedObject != null)
                    {
                        if (enableDebugLogs)
                        {
                            Debug.Log($"✅ [StageManager] 풀링 스폰 성공: {poolTag} at {position}");
                        }
                        
                        // 🔧 VFX 시스템 재활성화 (풀 에러 해결 후)
                        EmitSpawnCues(spawnedObject, monsterData, position);
                        
                        return spawnedObject;
                    }
                    else
                    {
                        if (enableDebugLogs)
                        {
                            Debug.LogWarning($"⚠️ [StageManager] 풀링 실패, 직접 생성: {poolTag}");
                        }
                    }
                }
                
                // 풀링 실패 시 직접 생성 (fallback)
                GameObject directSpawn = Instantiate(prefabToSpawn, position, Quaternion.identity);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"🔧 [StageManager] 직접 생성: {directSpawn.name} at {position}");
                }
                
                EmitSpawnCues(directSpawn, monsterData, position);
                return directSpawn;
            }
            
            // 3단계: 레거시 시스템 fallback (기존 호환성)
            if (monsterData.MonsterPrefab != null)
            {
                GameObject legacySpawn = Instantiate(monsterData.MonsterPrefab, position, Quaternion.identity);
                
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"⚠️ [StageManager] 레거시 MonsterPrefab 사용: {legacySpawn.name}");
                }
                
                EmitSpawnCues(legacySpawn, monsterData, position);
                return legacySpawn;
            }
            
            Debug.LogError($"❌ [StageManager] 몬스터 스폰 완전 실패: {monsterData.MonsterID}");
            return null;
        }
        
        /// <summary>
        /// 🆕 MonsterID로부터 EnemyData 가져오기 (Resources 기반)
        /// </summary>
        private EnemyData GetEnemyDataFromMonsterID(string monsterID)
        {
            if (string.IsNullOrEmpty(monsterID))
                return null;
            
            // Resources/EnemyData 폴더에서 EnemyData 찾기
            string[] possiblePaths = {
                $"EnemyData/{GetEnemyDataFileName(monsterID)}",
                $"EnemyData/{monsterID}Data",
                $"EnemyData/{monsterID}"
            };
            
            foreach (string path in possiblePaths)
            {
                EnemyData enemyData = Resources.Load<EnemyData>(path);
                if (enemyData != null)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"✅ [StageManager] EnemyData 로드: {monsterID} → {path}");
                    }
                    return enemyData;
                }
            }
            
            if (enableDebugLogs)
            {
                Debug.LogWarning($"⚠️ [StageManager] EnemyData 없음: {monsterID}");
            }
            
            return null;
        }
        
        /// <summary>
        /// 🆕 MonsterID를 EnemyData 파일명으로 변환
        /// </summary>
        private string GetEnemyDataFileName(string monsterID)
        {
            // MON_BLUESLIME_001 → BlueSlimeData
            if (monsterID.Contains("BLUESLIME"))
            {
                return monsterID.Contains("BOSS") ? "BlueSlime_BossData" : "BlueSlimeData";
            }
            else if (monsterID.Contains("GRAPE"))
            {
                return monsterID.Contains("BOSS") ? "Grape_BossData" : "GrapeData";
            }
            else if (monsterID.Contains("GHOST"))
            {
                return monsterID.Contains("BOSS") ? "Ghost_BossData" : "GhostData";
            }
            else if (monsterID.Contains("FINALBOSSA"))
            {
                return "FinalBossAData";
            }
            else if (monsterID.Contains("FINALBOSSB"))
            {
                return "FinalBossBData";
            }
            else if (monsterID.Contains("FINALBOSSC"))
            {
                return "FinalBossCData";
            }
            else if (monsterID.Contains("STONEGOLEM"))
            {
                return monsterID.Contains("BOSS") ? "StoneGolem_BossData" : "StoneGolemData";
            }
            else if (monsterID.Contains("CRYSTALGOLEM"))
            {
                return monsterID.Contains("BOSS") ? "CrystalGolem_BossData" : "CrystalGolemData";
            }
            else if (monsterID.Contains("WATERGOLEM"))
            {
                return monsterID.Contains("BOSS") ? "WaterGolem_BossData" : "WaterGolemData";
            }
            else if (monsterID.Contains("SPIDER"))
            {
                return monsterID.Contains("BOSS") ? "Spider_BossData" : "SpiderData";
            }
            else if (monsterID.Contains("BEAR"))
            {
                return monsterID.Contains("BOSS") ? "Bear_BossData" : "BearData";
            }
            
            return $"{monsterID}Data"; // 기본값
        }
        
        /// <summary>
        /// 몬스터 ID로부터 풀 태그 생성 - MonsterID 직접 사용
        /// </summary>
        private string GetPoolTagFromMonsterID(string monsterID)
        {
            // MonsterID를 풀 태그로 직접 사용 (PoolConfig와 일치)
            return monsterID;
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

        #region ✅ 🎵 Cue 시스템 연동 (Phase C-3 추가)
    
    /// <summary>
    /// 🎵 몬스터 스폰 이펙트 Cue 발행
    /// </summary>
    private void EmitSpawnCues(GameObject spawnedMonster, MonsterSpawnData monsterData, Vector3 position)
    {
        try
        {
            // 보스 여부 확인
            bool isBoss = monsterData.MonsterID.Contains("BOSS") || monsterData.MonsterID.Contains("Boss");
            
            // CueContext 생성
            var context = new CueSystem.CueContext
            {
                position = position,
                rotation = Quaternion.identity,
                normal = Vector3.up,
                facingDir = Vector2.down, // 스폰 시 아래 방향
                follow = spawnedMonster.transform,
                actorType = CueSystem.ActorType.Environment, // 스테이지 환경 이벤트
                surfaceType = CueSystem.SurfaceType.Default,
                magnitude = isBoss ? 2.0f : 1.0f,
                isCritical = isBoss,
                scale = isBoss ? 1.5f : 1.0f
            };
            
            // 이벤트 키 결정
            string eventKey = isBoss ? "spawn.enemy.boss" : "spawn.enemy.normal";
            
            // Cue 발행 (Stage 도메인 사용)
            bool success = CueSystem.CueEmitter.Emit(eventKey, "Stage", context);
            
            Debug.Log($"🎵 [StageManager] 스폰 Cue 발행: {eventKey} ({monsterData.MonsterID}) → {(success ? "성공" : "실패")}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🔴 [StageManager] 스폰 Cue 발행 오류: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 🎵 스테이지 입장 이펙트 Cue 발행
    /// </summary>
    private void EmitStageEnterCues()
    {
        try
        {
            var context = new CueSystem.CueContext
            {
                position = Vector3.zero, // 화면 중앙
                rotation = Quaternion.identity,
                normal = Vector3.up,
                facingDir = Vector2.down,
                follow = null,
                actorType = CueSystem.ActorType.Environment,
                surfaceType = CueSystem.SurfaceType.Default,
                magnitude = 1.5f,
                isCritical = false,
                scale = 1.2f
            };
            
            bool success = CueSystem.CueEmitter.Emit("stage.enter", "Stage", context);
            Debug.Log($"🎵 [StageManager] 스테이지 입장 Cue 발행: stage.enter → {(success ? "성공" : "실패")}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🔴 [StageManager] 스테이지 입장 Cue 발행 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 🎵 웨이브 시작 이펙트 Cue 발행
    /// </summary>
    private void EmitWaveStartCues(WaveConfig waveConfig)
    {
        try
        {
            var context = new CueSystem.CueContext
            {
                position = Vector3.zero,
                rotation = Quaternion.identity,
                normal = Vector3.up,
                facingDir = Vector2.down,
                follow = null,
                actorType = CueSystem.ActorType.Environment,
                surfaceType = CueSystem.SurfaceType.Default,
                magnitude = 1.0f,
                isCritical = false,
                scale = 1.0f
            };
            
            bool success = CueSystem.CueEmitter.Emit("wave.start", "Stage", context);
            Debug.Log($"🎵 [StageManager] 웨이브 시작 Cue 발행: wave.start ({waveConfig.WaveID}) → {(success ? "성공" : "실패")}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🔴 [StageManager] 웨이브 시작 Cue 발행 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 🎵 웨이브 완료 이펙트 Cue 발행
    /// </summary>
    private void EmitWaveCompleteCues(WaveConfig completedWave)
    {
        try
        {
            var context = new CueSystem.CueContext
            {
                position = Vector3.zero,
                rotation = Quaternion.identity,
                normal = Vector3.up,
                facingDir = Vector2.down,
                follow = null,
                actorType = CueSystem.ActorType.Environment,
                surfaceType = CueSystem.SurfaceType.Default,
                magnitude = 1.2f,
                isCritical = false,
                scale = 1.1f
            };
            
            bool success = CueSystem.CueEmitter.Emit("wave.complete", "Stage", context);
            Debug.Log($"🎵 [StageManager] 웨이브 완료 Cue 발행: wave.complete ({completedWave.WaveID}) → {(success ? "성공" : "실패")}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🔴 [StageManager] 웨이브 완료 Cue 발행 오류: {ex.Message}");
        }
    }

    /// <summary>
    /// 🎵 스테이지 완료 이펙트 Cue 발행
    /// </summary>
    private void EmitStageCompleteCues(bool success)
    {
        try
        {
            var context = new CueSystem.CueContext
            {
                position = Vector3.zero,
                rotation = Quaternion.identity,
                normal = Vector3.up,
                facingDir = Vector2.down,
                follow = null,
                actorType = CueSystem.ActorType.Environment,
                surfaceType = CueSystem.SurfaceType.Default,
                magnitude = success ? 2.0f : 1.0f,
                isCritical = success,
                scale = success ? 2.0f : 1.0f
            };
            
            string eventKey = "stage.complete";
            bool cueSuccess = CueSystem.CueEmitter.Emit(eventKey, "Stage", context);
            Debug.Log($"🎵 [StageManager] 스테이지 완료 Cue 발행: {eventKey} (성공: {success}) → {(cueSuccess ? "성공" : "실패")}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🔴 [StageManager] 스테이지 완료 Cue 발행 오류: {ex.Message}");
        }
    }

    #endregion
    
    /// <summary>
    /// ✅ 범용 웨이브 트리거
    /// </summary>
    public void TriggerWave(WaveTriggerId triggerId)
    {
        if (waveController != null)
        {
            waveController.TriggerWave(triggerId);
        }
    }
}  // ✅ StageManager 클래스 닫기
}  // ✅ StageSystem 네임스페이스 닫기