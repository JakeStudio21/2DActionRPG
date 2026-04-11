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
        
        /// <summary>
        /// ⭐ Phase 3: 현재 실행 중인 스테이지 설정 (읽기 전용)
        /// 동적 레벨링 시스템에서 StageBaseLevel 접근용
        /// </summary>
        public StageConfig CurrentStageConfig => stageConfig;
        
        [Header("디버그")]
        [SerializeField] private bool enableDebugLogs = false; // NavMesh 통합 완료 후 비활성화
        [SerializeField] private bool autoStartStage = false;
        
        // 컴포넌트 참조
        private WaveController waveController;
        private SpawnPointManager spawnPointManager;
        
        // 현재 상태
        private bool isStageActive = false;
        private int currentWaveIndex = 0;
        private float stageStartTime;
        
        // AutoAfterDelay 병렬 웨이브 관리
        [Header("웨이브 병렬 설정")]
        [Tooltip("AutoAfterDelay 웨이브가 스폰되기 전 허용하는 최대 생존 적 수\n" +
                 "초과 시 적 수가 줄어들 때까지 스폰 대기")]
        [SerializeField] private int maxEnemyLimit = 50;
        
        [Header("⚔️ 공통 승리 연출")]
        [Tooltip("KillAll / ObjectiveComplete 승리 시 팝업 전 대기 시간 (실제 시간 기준, 초) — 마지막 적 Death Effect 길이에 맞게 조정")]
        [SerializeField] private float victoryDelay = 1.5f;
        
        [Header("🐲 보스 처치 연출")]
        [Tooltip("Death Effect 재생 대기 시간 (실제 시간 기준, 초) — VFX 길이에 맞게 조정")]
        [SerializeField] private float bossDeathEffectDuration = 3.0f;
        
        [Tooltip("Death Effect 종료 후 승리 팝업까지 추가 여유 시간 (실제 시간 기준, 초)")]
        [SerializeField] private float bossVictoryDelay = 0.5f;
        
        private HashSet<int> scheduledAutoDelayWaveIndices = new HashSet<int>();
        private List<Coroutine> pendingAutoDelayCoroutines = new List<Coroutine>();
        
        // 병렬 웨이브가 완료됐으나 순차 웨이브가 아직 진행 중이어서 StartNextWave를 보류 중
        private bool pendingNextWaveStart = false;
        
        // UI용 표시 웨이브 인덱스 (내부 currentWaveIndex와 분리)
        private int displayWaveIndex = 0;
        
        // 이벤트
        public System.Action<StageConfig> OnStageStarted;
        public System.Action<StageConfig, bool> OnStageCompleted; // success
        public System.Action<WaveConfig> OnWaveChanged;
        
        /// <summary>
        /// UI용 웨이브 진행 이벤트 (currentIndex: 현재 스폰된 웨이브, total: 전체 웨이브 수)
        /// </summary>
        public System.Action<int, int> OnDisplayWaveIndexChanged;
        
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
            // 자동 시작 모드일 때
            if (autoStartStage)
            {
                // StageConfig가 직접 할당되어 있으면 그것 사용
                if (stageConfig != null)
                {
                    if (enableDebugLogs)
                        Debug.Log($"[StageManager] Inspector에 할당된 StageConfig 사용: {stageConfig.StageID}");
                    StartStage(stageConfig);
                }
                // StageConfig가 비어있으면 현재 씬 이름으로 자동 로드
                else
                {
                    string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                    
                    if (enableDebugLogs)
                        Debug.Log($"[StageManager] 현재 씬 이름으로 StageConfig 자동 로드 시도: {currentSceneName}");
                    
                    // 씬 이름 = StageID로 가정 (예: CH01_ST01)
                    StartStage(currentSceneName);
                }
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"[StageManager] Auto Start가 비활성화되어 있습니다. 수동으로 StartStage()를 호출하세요.");
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
            totalEnemyKillCount = 0;
            isBossKilled = false; // ✅ 보스 처치 플래그 초기화
            displayWaveIndex = 0;
            scheduledAutoDelayWaveIndices.Clear();
            pendingAutoDelayCoroutines.Clear();
            pendingNextWaveStart = false;
            
            // ⚡ Phase D-Revision: 입장 시점에 즉시 재화 차감 (로비 검증 통과 전제)
            ConsumeStageEntryCost();
            
            // ⭐ 0단계: 캐릭터 가방 초기화 (인게임 전용 임시 저장소)
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
            {
                int slotIndex = PlayerDataManager.Instance.GetCurrentSlotIndex();
                var slotData = PlayerDataManager.Instance.GetSlotData(slotIndex);
                
                if (slotData != null)
                {
                    Debug.Log($"🎒 [StageManager] 캐릭터 가방 초기화: {slotData.characterBagInstanceIds.Count}개 아이템 제거");
                    slotData.characterBagInstanceIds.Clear();
                    PlayerDataManager.Instance.SaveSlotData(slotData);
                    
                    // selectedPlayerData도 동기화
                    if (PlayerDataManager.Instance.selectedPlayerData != null)
                    {
                        PlayerDataManager.Instance.selectedPlayerData.LoadFromSlotData(slotData);
                    }
                }
            }
            
            // 1단계: 풀 시스템 Warmup
            yield return StartCoroutine(WarmupPoolSystem());
            
            // ✅ 풀 로딩 완료 후 스테이지 입장 이펙트 발행
            EmitStageEnterCues();
            
            // 🎬 Phase 4: 스테이지 입장 컷신 체크
            if (!string.IsNullOrEmpty(stageConfig.enterCutsceneId))
            {
                // 스테이지 클리어 여부가 아닌 컷신 시청 여부로 판단한다.
                // 실패 후 재입장 시에도 이미 본 컷신은 스킵되도록 한다.
                bool isReplay = CutsceneSystem.CutsceneManager.Instance != null &&
                                CutsceneSystem.CutsceneManager.Instance.HasSeenCutscene(stageConfig.enterCutsceneId);
                
                if (CutsceneSystem.CutsceneManager.Instance != null)
                {
                    bool shouldPlay = CutsceneSystem.CutsceneManager.Instance.ShouldPlayCutscene(
                        stageConfig.enterCutsceneId, 
                        isReplay, 
                        stageConfig.isReplaySkipCutscene
                    );
                    
                    if (shouldPlay)
                    {
                        if (enableDebugLogs)
                            Debug.Log($"🎬 [StageManager] 스테이지 입장 컷신 재생: {stageConfig.enterCutsceneId}");
                        
                        // 컷신 재생
                        CutsceneSystem.CutsceneManager.Instance.PlayCutscene(stageConfig.enterCutsceneId);
                        
                        // 컷신 종료 대기
                        yield return new WaitUntil(() => !CutsceneSystem.CutsceneManager.Instance.IsPlaying);
                    }
                }
            }
            
            // 2단계: 스테이지 데이터 로드
            yield return StartCoroutine(LoadStageData());
            
            // 3단계: 모든 사전 처리(풀 워밍업·컷신) 완료 후 플레이어 이동 잠금 해제
            // PlayerSpawner에서 스폰 직후 걸어둔 잠금을 여기서 일괄 해제한다.
            var playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                playerController.SetMovementLocked(false);
                if (enableDebugLogs)
                    Debug.Log("[StageManager] 스테이지 준비 완료 - 플레이어 이동 잠금 해제");
            }
            
            // 4단계: 첫 번째 웨이브 시작
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
                // ObjectiveComplete는 NotifyBarricadeDestroyed() 등 외부 통지에서만 승리 처리
                // 몬스터를 모두 처치해도 목표 오브젝트가 파괴되기 전까지 승리하지 않음
                if (stageConfig.Victory == VictoryCondition.ObjectiveComplete)
                {
                    if (enableDebugLogs)
                        Debug.Log($"🏚️ [StageManager] 모든 몬스터 처치 완료 - 목표 오브젝트 파괴 대기 중");
                    return;
                }
                
                // BossKill은 NotifyEnemyKilled() → BossKillVictorySequence 코루틴에서 처리
                // 여기서 즉시 CompleteStage를 호출하면 슬로우 연출이 무시됨
                if (stageConfig.Victory == VictoryCondition.BossKill)
                {
                    if (enableDebugLogs)
                        Debug.Log($"🐲 [StageManager] BossKill - 모든 웨이브 완료, 보스 처치 판정 대기 중");
                    return;
                }
                
                // 그 외 조건(KillAll 등) — Death Effect 대기 후 승리
                StartCoroutine(VictorySequence(true, victoryDelay));
                return;
            }
            
            WaveConfig currentWave = stageConfig.WaveConfigs[currentWaveIndex];
            
            if (enableDebugLogs)
            {
                Debug.Log($"🌊 [StageManager] 웨이브 {currentWaveIndex + 1}/{stageConfig.WaveConfigs.Count} 시작: {currentWave.WaveID}");
            }
            
            // ✅ 🎵 웨이브 시작 이펙트 발행
            EmitWaveStartCues(currentWave);
            
            // ✅ 🎵 전투/보스 BGM 전환
            HandleWaveBGM(currentWave, isWaveStart: true);
            
            // UI 표시 인덱스 업데이트
            displayWaveIndex++;
            OnDisplayWaveIndexChanged?.Invoke(displayWaveIndex, stageConfig.WaveConfigs.Count);
            OnWaveChanged?.Invoke(currentWave);
            
            waveController.ExecuteWave(currentWave);
            
            // 다음 웨이브가 AutoAfterDelay이면 지금 바로 타이머 시작 (현재 웨이브 시작과 동시에)
            int nextIndex = currentWaveIndex + 1;
            if (nextIndex < stageConfig.WaveConfigs.Count)
            {
                var nextWave = stageConfig.WaveConfigs[nextIndex];
                if (nextWave.StartCondition == WaveStartCondition.AutoAfterDelay)
                {
                    scheduledAutoDelayWaveIndices.Add(nextIndex);
                    var coroutine = StartCoroutine(AutoDelayWaveCoroutine(nextIndex, nextWave));
                    pendingAutoDelayCoroutines.Add(coroutine);
                    
                    if (enableDebugLogs)
                        Debug.Log($"⏱️ [StageManager] AutoAfterDelay 예약: Wave[{nextIndex}] {nextWave.WaveID} ({nextWave.WaveDelaySec}초 후)");
                }
            }
        }
        
        /// <summary>
        /// AutoAfterDelay 웨이브 독립 타이머 코루틴
        /// 이전 웨이브가 살아있어도 딜레이 후 병렬 스폰 시작
        /// </summary>
        private IEnumerator AutoDelayWaveCoroutine(int waveIndex, WaveConfig wave)
        {
            // 지정 시간 대기
            if (wave.WaveDelaySec > 0)
                yield return new WaitForSeconds(wave.WaveDelaySec);
            
            if (!isStageActive) yield break;
            
            // MaxEnemyLimit: 적이 너무 많으면 줄어들 때까지 대기
            while (waveController.TotalActiveEnemies >= maxEnemyLimit)
            {
                if (!isStageActive) yield break;
                if (enableDebugLogs)
                    Debug.Log($"⏸️ [StageManager] AutoDelay Wave[{waveIndex}] 대기 중: 현재 적 {waveController.TotalActiveEnemies} / 한도 {maxEnemyLimit}");
                yield return new WaitForSeconds(1f);
            }
            
            if (!isStageActive) yield break;
            
            // B안: 이 웨이브 적이 실제로 스폰되는 시점에 다음 AutoAfterDelay 타이머 시작
            int nextIndex = waveIndex + 1;
            if (nextIndex < stageConfig.WaveConfigs.Count)
            {
                var nextWave = stageConfig.WaveConfigs[nextIndex];
                if (nextWave.StartCondition == WaveStartCondition.AutoAfterDelay)
                {
                    scheduledAutoDelayWaveIndices.Add(nextIndex);
                    var coroutine = StartCoroutine(AutoDelayWaveCoroutine(nextIndex, nextWave));
                    pendingAutoDelayCoroutines.Add(coroutine);
                    
                    if (enableDebugLogs)
                        Debug.Log($"⏱️ [StageManager] AutoAfterDelay 체인 예약: Wave[{nextIndex}] {nextWave.WaveID} ({nextWave.WaveDelaySec}초 후)");
                }
            }
            
            // UI 표시 인덱스 업데이트 (실제 스폰 시점)
            displayWaveIndex++;
            OnDisplayWaveIndexChanged?.Invoke(displayWaveIndex, stageConfig.WaveConfigs.Count);
            OnWaveChanged?.Invoke(wave);
            
            if (enableDebugLogs)
                Debug.Log($"⚡ [StageManager] AutoAfterDelay 병렬 스폰 시작: Wave[{waveIndex}] {wave.WaveID}");
            
            // 병렬 스폰 실행 (isWaveActive 체크 없이)
            waveController.StartParallelWave(wave, (completedWave) => OnParallelWaveCompleted(waveIndex, completedWave));
        }
        
        /// <summary>
        /// 병렬 웨이브 완료 콜백
        /// </summary>
        private void OnParallelWaveCompleted(int waveIndex, WaveConfig completedWave)
        {
            scheduledAutoDelayWaveIndices.Remove(waveIndex);
            pendingAutoDelayCoroutines.RemoveAll(c => c == null);
            
            if (!isStageActive) return;
            
            if (enableDebugLogs)
                Debug.Log($"✅ [StageManager] 병렬 웨이브 완료: Wave[{waveIndex}] {completedWave.WaveID}");
            
            // currentWaveIndex를 이 웨이브 다음으로 최소 보장
            if (currentWaveIndex < waveIndex + 1)
                currentWaveIndex = waveIndex + 1;
            
            // 다음 순차 웨이브도 auto-scheduled이면 그 완료 콜백에게 위임
            if (currentWaveIndex < stageConfig.WaveConfigs.Count &&
                scheduledAutoDelayWaveIndices.Contains(currentWaveIndex))
            {
                if (enableDebugLogs)
                    Debug.Log($"[StageManager] Wave[{currentWaveIndex}]도 AutoAfterDelay 예약됨 - 대기");
                return;
            }
            
            // 순차 웨이브(ExecuteWave)가 아직 진행 중이면 isWaveActive 가드에 막힘
            // → 순차 웨이브 완료 시 OnWaveCompleted가 pendingNextWaveStart를 확인해서 실행
            if (waveController.IsWaveActive)
            {
                pendingNextWaveStart = true;
                if (enableDebugLogs)
                    Debug.Log($"[StageManager] 순차 웨이브 진행 중 - 다음 웨이브(Wave[{currentWaveIndex}]) 대기 예약");
                return;
            }
            
            // 순차 웨이브 완료 상태 → 즉시 다음 웨이브 시작
            StartNextWave();
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
            
            // ✅ 🎵 전투/보스 BGM 종료
            HandleWaveBGM(completedWave, isWaveStart: false);
            
            // currentWaveIndex 안전 증가 (병렬 완료로 이미 앞서있을 수 있음)
            int completedIndex = stageConfig.WaveConfigs.IndexOf(completedWave);
            int newNextIndex = completedIndex >= 0 ? completedIndex + 1 : currentWaveIndex + 1;
            if (currentWaveIndex < newNextIndex)
                currentWaveIndex = newNextIndex;

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
            
            // 다음 순차 웨이브가 AutoAfterDelay로 이미 예약/진행 중이면 여기서 체인 중단
            // → AutoDelayWaveCoroutine 또는 OnParallelWaveCompleted가 다음 웨이브를 처리함
            if (newNextIndex < stageConfig.WaveConfigs.Count &&
                scheduledAutoDelayWaveIndices.Contains(newNextIndex))
            {
                if (enableDebugLogs)
                    Debug.Log($"[StageManager] Wave[{newNextIndex}] AutoAfterDelay 예약됨 - 완료 대기");
                return;
            }
            
            // 병렬 완료로 currentWaveIndex가 이미 newNextIndex를 넘어섰을 때
            if (currentWaveIndex > newNextIndex)
            {
                // 병렬 웨이브가 isWaveActive에 막혀 다음 웨이브 시작을 보류해뒀다면 지금 실행
                if (pendingNextWaveStart)
                {
                    pendingNextWaveStart = false;
                    if (enableDebugLogs)
                        Debug.Log($"[StageManager] 순차 완료 → 보류 중이던 Wave[{currentWaveIndex}] 시작");
                    StartNextWave();
                }
                else if (enableDebugLogs)
                {
                    Debug.Log($"[StageManager] 순차 완료({newNextIndex}) 무시: 인덱스가 이미 {currentWaveIndex}");
                }
                return;
            }
            
            // ✅ 승리 조건 체크 (Victory 타입별로 처리)
            if (enableDebugLogs)
                Debug.Log($"🔍 [StageManager] OnWaveCompleted - Victory: {stageConfig.Victory}, WaveIndex: {currentWaveIndex}/{stageConfig.WaveConfigs.Count}");
            
            bool shouldCheckVictory = false;
            
            switch (stageConfig.Victory)
            {
                case VictoryCondition.KillAll:
                    // 모든 웨이브 완료 시에만 승리 체크
                    shouldCheckVictory = (currentWaveIndex >= stageConfig.WaveConfigs.Count);
                    
                    // ✅ 타임리미트가 있는 경우 시간 체크 포함
                    if (shouldCheckVictory && stageConfig.hasTimeLimit)
                    {
                        float elapsedTime = Time.time - stageStartTime;
                        if (elapsedTime > stageConfig.TimeLimitSec)
                        {
                            if (enableDebugLogs)
                                Debug.Log($"⏱️ [StageManager] KillAll 완료했지만 이미 시간 초과");
                            
                            CompleteStage(false); // 시간 초과로 패배
                            return;
                        }
                    }
                    
                    if (enableDebugLogs && shouldCheckVictory)
                        Debug.Log($"📋 [StageManager] KillAll - 모든 웨이브 완료, 승리 체크");
                    break;
                    
                case VictoryCondition.BossKill:
                    // 보스 처치는 NotifyEnemyKilled()에서 이미 처리했으므로 여기서는 체크 안함
                    shouldCheckVictory = false;
                    if (enableDebugLogs)
                        Debug.Log($"📋 [StageManager] BossKill - NotifyEnemyKilled()에서 처리됨");
                    break;
                    
                case VictoryCondition.Survival:
                    // 제한시간은 Update()에서 체크하므로 여기서는 체크 안함
                    shouldCheckVictory = false;
                    if (enableDebugLogs)
                        Debug.Log($"📋 [StageManager] Survival - Update()에서 처리됨");
                    break;
                    
                case VictoryCondition.ObjectiveComplete:
                    // 특수 목표는 별도 로직에서 처리
                    shouldCheckVictory = false;
                    break;
            }
            
            if (shouldCheckVictory && CheckVictoryCondition())
            {
                if (enableDebugLogs)
                    Debug.Log($"🏆 [StageManager] 승리! VictorySequence 시작 ({victoryDelay}초 대기)");
                StartCoroutine(VictorySequence(true, victoryDelay));
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
            float elapsedTime = Time.time - stageStartTime;
            
            switch (stageConfig.Victory)
            {
                case VictoryCondition.KillAll:
                    // 모든 웨이브 완료 체크
                    bool allWavesCleared = currentWaveIndex >= stageConfig.WaveConfigs.Count;
                    
                    // ✅ 타임리미트가 있는 경우 시간 체크
                    if (stageConfig.hasTimeLimit && allWavesCleared)
                    {
                        bool inTime = elapsedTime <= stageConfig.TimeLimitSec;
                        
                        if (enableDebugLogs)
                        {
                            if (inTime)
                                Debug.Log($"🏆 [StageManager] KillAll + 시간 안에 클리어! {elapsedTime:F1}초");
                            else
                                Debug.Log($"⏱️ [StageManager] KillAll 완료했지만 시간 초과: {elapsedTime:F1}초");
                        }
                        
                        return inTime;
                    }
                    
                    return allWavesCleared;
                    
                case VictoryCondition.BossKill:
                    // 보스 처치 체크
                    bool bossKilled = isBossKilled;
                    
                    // ✅ 타임리미트가 있는 경우 시간 체크
                    if (stageConfig.hasTimeLimit && bossKilled)
                    {
                        bool inTime = elapsedTime <= stageConfig.TimeLimitSec;
                        
                        if (enableDebugLogs)
                        {
                            if (inTime)
                                Debug.Log($"🏆 [StageManager] BossKill + 시간 안에 클리어! {elapsedTime:F1}초");
                            else
                                Debug.Log($"⏱️ [StageManager] 보스 처치했지만 시간 초과: {elapsedTime:F1}초");
                        }
                        
                        return inTime;
                    }
                    
                    return bossKilled;
                    
                case VictoryCondition.Survival:
                    // 제한시간 생존 확인
                    return elapsedTime >= stageConfig.TimeLimitSec;
                    
                case VictoryCondition.ObjectiveComplete:
                    // NotifyBarricadeDestroyed() 등 외부 통지로만 트리거됨
                    // 이 함수에서는 판정하지 않음
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
            
            // AutoAfterDelay 대기 코루틴 전부 취소
            foreach (var c in pendingAutoDelayCoroutines)
                if (c != null) StopCoroutine(c);
            pendingAutoDelayCoroutines.Clear();
            scheduledAutoDelayWaveIndices.Clear();
            pendingNextWaveStart = false;
            
            // ✅ 🎵 스테이지 완료 이펙트 발행
            EmitStageCompleteCues(success);
            
            // 진행 중인 웨이브 정지 (병렬 포함)
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
            StageResultData resultData = new StageResultData(success, 0, 0); // 기본값
            
            if (success)
            {
                // ⚡ Phase D-Revision: 재화 차감은 InitializeStage()에서 이미 완료
                
                // 보상 처리 및 결과 저장
                var rewardResult = ProcessStageRewards(clearTime);
                
                // RewardResult → StageResultData 변환
                resultData = StageResultData.FromRewardResult(rewardResult, true);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"📦 [StageManager] StageResultData 생성: 골드 {resultData.goldReward}, EXP {resultData.expReward}, 아이템 {resultData.itemRewards.Count}개");
                }
                
                // 🎒 보상 처리 완료 후 이관: 스테이지 중 드랍 아이템 + 클리어 보상 아이템 모두 포함
                // (이전에는 이관이 보상 처리보다 먼저 호출되어 클리어 보상이 sharedInventory에 저장되지 않는 버그 존재)
                TransferItemsToAccount();
                
                SaveStageProgress(clearTime);
                
                // 🎯 Phase 5: Stage 10 클리어 시 챕터 종료 처리
                if (stageConfig.stageIndexInChapter == 10)
                {
                    // 최초 클리어 여부 확인
                    bool isFirstClear = false;
                    if (StageProgressManager.Instance != null)
                    {
                        isFirstClear = !StageProgressManager.Instance.IsChapterCleared(stageConfig.chapterId);
                    }
                    
                    if (isFirstClear)
                    {
                        // 챕터 완료 기록
                        if (StageProgressManager.Instance != null)
                        {
                            StageProgressManager.Instance.CompleteChapter(stageConfig.chapterId);
                        }
                        
                        // 🎬 Phase 5: 챕터 종료 컷신 예약 (SelectedPlayerData에 저장)
                        string chapterClearCutsceneId = $"CH{stageConfig.chapterId:D2}_CLEAR";
                        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.selectedPlayerData != null)
                        {
                            PlayerDataManager.Instance.selectedPlayerData.pendingCutsceneId = chapterClearCutsceneId;
                            PlayerDataManager.Instance.selectedPlayerData.pendingChapterId = stageConfig.chapterId;
                            
                            // 즉시 저장
                            PlayerDataManager.Instance.SaveCurrentSlot();
                            
                            if (enableDebugLogs)
                                Debug.Log($"🎬 [StageManager] 챕터 {stageConfig.chapterId} 종료 컷신 예약: {chapterClearCutsceneId} (SelectedPlayerData)");
                        }
                    }
                }
                
                // 🎬 Phase 4: 스테이지 클리어 컷신 체크
                if (!string.IsNullOrEmpty(stageConfig.clearCutsceneId))
                {
                    bool isReplay = StageProgressManager.Instance != null && 
                                    StageProgressManager.Instance.IsStageCompleted(stageConfig.StageID);
                    
                    if (CutsceneSystem.CutsceneManager.Instance != null)
                    {
                        bool shouldPlay = CutsceneSystem.CutsceneManager.Instance.ShouldPlayCutscene(
                            stageConfig.clearCutsceneId, 
                            isReplay, 
                            stageConfig.isReplaySkipCutscene
                        );
                        
                        if (shouldPlay)
                        {
                            if (enableDebugLogs)
                                Debug.Log($"🎬 [StageManager] 스테이지 클리어 컷신 재생 예약: {stageConfig.clearCutsceneId}");
                            
                            // 클리어 컷신은 ResultPopup 표시 전에 재생
                            StartCoroutine(PlayClearCutsceneAndShowResult(resultData));
                            return; // FSMStageController 호출은 컷신 종료 후 처리
                        }
                    }
                }
                
                // FSMStageController에 승리 알림 (보상 데이터 포함)
                if (FSMStageController.Instance != null)
                {
                    FSMStageController.Instance.TriggerVictory(resultData);
                }
            }
            else
            {
                // 🎒 실패/포기 시: 스테이지 중 획득한 아이템만 이관 (보상 없음)
                TransferItemsToAccount();
                
                // FSMStageController에 패배 알림
                if (FSMStageController.Instance != null)
                {
                    FSMStageController.Instance.TriggerDefeat();
                }
            }
        }
        
        /// <summary>
        /// 🎬 Phase 4: 클리어 컷신 재생 후 결과 표시
        /// </summary>
        private IEnumerator PlayClearCutsceneAndShowResult(StageResultData resultData)
        {
            if (enableDebugLogs)
                Debug.Log($"🎬 [StageManager] 클리어 컷신 재생 시작: {stageConfig.clearCutsceneId}");
            
            // 컷신 재생
            CutsceneSystem.CutsceneManager.Instance.PlayCutscene(stageConfig.clearCutsceneId);
            
            // 컷신 종료 대기
            yield return new WaitUntil(() => !CutsceneSystem.CutsceneManager.Instance.IsPlaying);
            
            if (enableDebugLogs)
                Debug.Log($"🎬 [StageManager] 클리어 컷신 종료, 결과 화면 표시");
            
            // 컷신 종료 후 FSMStageController에 승리 알림 (보상 데이터 포함)
            if (FSMStageController.Instance != null)
            {
                FSMStageController.Instance.TriggerVictory(resultData);
            }
        }
        
        /// <summary>
        /// 스테이지 보상 처리
        /// </summary>
        private RewardSystem.RewardResult ProcessStageRewards(float clearTime)
        {
            if (RewardSystem.Instance == null)
            {
                Debug.LogWarning("[StageManager] RewardSystem이 없습니다. 보상 처리를 건너뜁니다.");
                return new RewardSystem.RewardResult(); // 빈 결과 반환
            }
            
            // 진행도 확인하여 첫 클리어 여부 판단
            bool isFirstClear = true; // ⭐ 기본값: 첫 클리어 (progress가 없으면 첫 진입)
            
            if (StageProgressManager.Instance != null)
            {
                // 🏰 Phase 1: 던전 vs 스테이지 구분
                if (StageSystem.StageIdValidator.IsDungeon(stageConfig.StageID))
                {
                    // ⭐ 던전 클리어 기록 확인
                    isFirstClear = !StageProgressManager.Instance.IsDungeonCleared(stageConfig.StageID);
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"🎁 [StageManager] 보상 판정 - 던전: {stageConfig.StageID}, 첫 클리어: {isFirstClear}");
                    }
                }
                else
                {
                    // ⭐ 스테이지 완료 기록 확인
                    isFirstClear = !StageProgressManager.Instance.IsStageCompleted(stageConfig.StageID);
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"🎁 [StageManager] 보상 판정 - 스테이지: {stageConfig.StageID}, 첫 클리어: {isFirstClear}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("⚠️ [StageManager] StageProgressManager가 없어 첫 클리어로 간주합니다.");
            }
            
            // 보상 처리
            var rewardResult = RewardSystem.Instance.ProcessStageRewards(stageConfig, isFirstClear, clearTime);
            
            if (enableDebugLogs)
            {
                Debug.Log($"🎁 [StageManager] 보상 처리 완료: 골드 {rewardResult.Gold}, EXP {rewardResult.Exp}");
            }
            
            return rewardResult;
        }
        
        /// <summary>
        /// 스테이지 진행도 저장
        /// </summary>
        private void SaveStageProgress(float clearTime)
        {
            if (StageProgressManager.Instance != null)
            {
                int clearTimeInt = Mathf.RoundToInt(clearTime);
                
                // 🏰 Phase 1: 첫 클리어 여부 판단 (ProcessStageRewards와 동일 로직)
                bool isFirstClear = true;
                
                if (StageSystem.StageIdValidator.IsDungeon(stageConfig.StageID))
                {
                    // 던전 클리어 기록 확인
                    isFirstClear = !StageProgressManager.Instance.IsDungeonCleared(stageConfig.StageID);
                    
                    // 던전 진행도 저장 (CompleteDungeon 내부에서 isFirstClearRewarded 설정)
                    StageProgressManager.Instance.CompleteDungeon(stageConfig.StageID, clearTimeInt);
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"💾 [StageManager] 🏰 던전 진행도 저장: {stageConfig.StageID} - {clearTimeInt}초 (첫 클리어: {isFirstClear})");
                    }
                }
                else
                {
                    // 스테이지 완료 기록 확인
                    isFirstClear = !StageProgressManager.Instance.IsStageCompleted(stageConfig.StageID);
                    
                    // 스테이지 진행도 저장 (isFirstClear 전달)
                    StageProgressManager.Instance.CompleteStage(stageConfig.StageID, clearTimeInt, isFirstClear);
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"💾 [StageManager] 스테이지 진행도 저장: {stageConfig.StageID} - {clearTimeInt}초 (첫 클리어: {isFirstClear})");
                    }
                }
            }
        }
        
        /// <summary>
        /// StageConfig 로드 (챕터 스테이지 + 던전 지원)
        /// </summary>
        private StageConfig LoadStageConfig(string stageId)
        {
            StageConfig config = null;
            
            // ✅ Phase 6: 챕터 기반 경로 (CH01_ST01, CH02_ST05 등)
            if (StageSystem.StageIdValidator.IsValidChapterStageId(stageId))
            {
                string path = $"Stages/Configs/Chapters/{stageId}_Config";
                config = Resources.Load<StageConfig>(path);
                
                if (config == null)
                {
                    Debug.LogError($"[StageManager] StageConfig 로드 실패: {path}");
                }
                else if (enableDebugLogs)
                {
                    Debug.Log($"[StageManager] StageConfig 로드 성공: {path} -> {config.StageName}");
                }
            }
            // 🏰 Phase 1: 던전 경로 (DG01_SB01, DG_DAILY_FOREST_BIND 등)
            else if (StageSystem.StageIdValidator.IsValidDungeonId(stageId))
            {
                string path = $"Stages/Configs/Dungeons/{stageId}_Config";
                config = Resources.Load<StageConfig>(path);
                
                if (config == null)
                {
                    Debug.LogError($"[StageManager] DungeonConfig 로드 실패: {path}");
                }
                else if (enableDebugLogs)
                {
                    Debug.Log($"[StageManager] 🏰 DungeonConfig 로드 성공: {path} -> {config.StageName}");
                }
            }
            else
            {
                Debug.LogError($"[StageManager] 잘못된 StageID 형식: {stageId}");
                Debug.LogError($"[StageManager] 지원 형식: CH##_ST## (스테이지) 또는 DG##_XX## (던전)");
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
        /// Update에서 승리/패배 조건 체크
        /// </summary>
        private void Update()
        {
            if (!isStageActive)
                return;
            
            // ✅ 추가: 플레이어가 스폰되기 전에는 체크하지 않음
            var playerHealth = FindObjectOfType<PlayerHealth>();
            if (playerHealth == null)
            {
                return; // PlayerHealth가 없으면 체크 안함
            }
            
            // ========================================
            // ⏱️ 타임리미트 체크 (조건별 다른 처리)
            // ========================================
            float elapsedTime = Time.time - stageStartTime;
            
            // ✅ Case 1: Survival - 제한시간 도달 시 승리
            if (stageConfig.Victory == VictoryCondition.Survival)
            {
                if (elapsedTime >= stageConfig.TimeLimitSec)
                {
                    if (enableDebugLogs)
                        Debug.Log($"🏆 [StageManager] Survival 승리! {elapsedTime:F1}초 생존");
                    
                    // BGM 정리
                    if (BGMController.Instance != null)
                    {
                        BGMController.Instance.OnBossEnd();
                        BGMController.Instance.OnBattleEnd();
                        
                        if (enableDebugLogs)
                            Debug.Log($"🎵 [StageManager] Survival 승리 - BGM 상태 정리 완료");
                    }
                    
                    CompleteStage(true);
                    return;
                }
            }
            
            // ✅ Case 2: KillAll/BossKill + hasTimeLimit=true - 시간 초과 시 패배
            if (stageConfig.hasTimeLimit && 
                (stageConfig.Victory == VictoryCondition.KillAll || 
                 stageConfig.Victory == VictoryCondition.BossKill))
            {
                if (elapsedTime >= stageConfig.TimeLimitSec)
                {
                    if (enableDebugLogs)
                        Debug.Log($"⏱️ [StageManager] 타임오버! {elapsedTime:F1}초 초과 - 패배");
                    
                    CompleteStage(false); // 시간 초과 패배!
                    return;
                }
            }
            
            // 패배 조건 체크 (플레이어 사망 등)
            if (CheckDefeatCondition())
            {
                CompleteStage(false);
            }
        }
        
        // 공개 속성들
        public bool IsStageActive => isStageActive;
        public StageConfig CurrentStage => stageConfig;
        public int CurrentWaveIndex => currentWaveIndex;
        
        /// <summary>
        /// UI 표시용 웨이브 인덱스 (실제 스폰 시점에만 증가, 내부 인덱스와 분리)
        /// </summary>
        public int DisplayWaveIndex => displayWaveIndex;
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
                        
                        // ⭐⭐⭐ Phase 3: 동적 레벨 초기화 (핵심!)
                        BaseEnemy enemyComponent = spawnedObject.GetComponent<BaseEnemy>();
                        if (enemyComponent != null && stageConfig != null)
                        {
                            int stageLevel = stageConfig.StageBaseLevel;
                            int levelOffset = monsterData.LevelOffset;
                            enemyComponent.InitializeLevel(stageLevel, levelOffset);
                            
                            if (enableDebugLogs)
                            {
                                Debug.Log($"🎯 [StageManager] 레벨 초기화: {spawnedObject.name} Lv.{stageLevel + levelOffset} (Stage:{stageLevel} + Offset:{levelOffset})");
                            }
                        }
                        else if (enemyComponent == null)
                        {
                            Debug.LogWarning($"⚠️ [StageManager] {spawnedObject.name}: BaseEnemy 컴포넌트가 없습니다!");
                        }
                        // ⭐⭐⭐ Phase 3: 동적 레벨 초기화 끝
                        
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
                
                // ⭐⭐⭐ Phase 3: 동적 레벨 초기화 (직접 생성 경로)
                BaseEnemy directSpawnEnemy = directSpawn.GetComponent<BaseEnemy>();
                if (directSpawnEnemy != null && stageConfig != null)
                {
                    int stageLevel = stageConfig.StageBaseLevel;
                    int levelOffset = monsterData.LevelOffset;
                    directSpawnEnemy.InitializeLevel(stageLevel, levelOffset);
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"🎯 [StageManager] 레벨 초기화 (직접생성): {directSpawn.name} Lv.{stageLevel + levelOffset}");
                    }
                }
                // ⭐⭐⭐ Phase 3: 동적 레벨 초기화 끝
                
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
            
            string fileName = GetEnemyDataFileName(monsterID);
            
            // Resources/EnemyData 폴더에서 EnemyData 찾기 (서브폴더 포함)
            string[] possiblePaths = {
                $"EnemyData/{fileName}",                    // 기본 경로
                $"EnemyData/Elite/{fileName}",              // Elite 서브폴더
                $"EnemyData/Boss/{fileName}",               // Boss 서브폴더
                $"EnemyData/Normal/{fileName}",             // Normal 서브폴더
                $"EnemyData/{monsterID}Data",               // 레거시 1
                $"EnemyData/{monsterID}"                    // 레거시 2
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
                Debug.LogWarning($"   시도한 파일명: {fileName}");
                Debug.LogWarning($"   경로 확인: Resources/EnemyData/, Resources/EnemyData/Elite/, Resources/EnemyData/Boss/");
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
            else if (monsterID.Contains("SCORPION"))
            {
                // 색상별 Scorpion 매핑
                if (monsterID.Contains("RED"))
                    return "RedScorpionData";
                else if (monsterID.Contains("BLUE"))
                    return "BlueScorpionData";
                else if (monsterID.Contains("BROWN") || monsterID.Contains("BRWON"))
                    return "BrownScorpionData";
                
                // Boss 버전 (미래 확장용)
                if (monsterID.Contains("BOSS"))
                    return "Scorpion_BossData";
                
                return "RedScorpionData"; // 기본값 (Red)
            }
            else if (monsterID.Contains("COBRA"))
            {
                return monsterID.Contains("BOSS") ? "Cobra_BossData" : "CobraData";
            }
            else if (monsterID.Contains("ANUBIS"))
            {
                return monsterID.Contains("BOSS") ? "Anubis_BossData" : "AnubisData";
            }
            else if (monsterID.Contains("SPHINX"))
            {
                return monsterID.Contains("BOSS") ? "Sphinx_BossData" : "SphinxData";
            }
            else if (monsterID.Contains("BEETLE"))
            {
                return monsterID.Contains("BOSS") ? "Beetle_BossData" : "BeetleData";
            }
            else if (monsterID.Contains("LADYBUG"))
            {
                return monsterID.Contains("BOSS") ? "LadyBug_BossData" : "LadyBugData";
            }
            else if (monsterID.Contains("MIMIC"))
            {
                return monsterID.Contains("BOSS") ? "Mimic_BossData" : "MimicData";
            }
            else if (monsterID.Contains("PLANTS") || monsterID.Contains("PLANT"))
            {
                return monsterID.Contains("BOSS") ? "PlantsMonster_BossData" : "PlantsMonsterData";
            }
            else if (monsterID.Contains("TOWER"))
            {
                return monsterID.Contains("BOSS") ? "TowerMonster_BossData" : "TowerMonsterData";
            }
            else if (monsterID.Contains("SANDGOLEM"))
            {
                if (monsterID.Contains("ELITE"))
                    return "Elite_SandGolemData";
                else if (monsterID.Contains("BOSS"))
                    return "SandGolem_BossData";
                else
                    return "SandGolemData";
            }
            // ⭐ GOBLIN 시리즈는 ORC보다 먼저 체크해야 함! (GOBLINWARRIOR에 "ORC" 포함)
            else if (monsterID.Contains("GOBLINWARRIOR") || monsterID.Contains("GOBLIN_WARRIOR"))
            {
                // ⭐ GoblinWarrior 매핑 추가
                if (monsterID.Contains("ELITE"))
                    return "Elite_GoblinWarriorData";
                else if (monsterID.Contains("BOSS"))
                    return "GoblinWarrior_BossData";
                else
                    return "GoblinWarriorData";
            }
            else if (monsterID.Contains("GOBLINBOMB") || monsterID.Contains("GOBLIN_BOMB"))
            {
                // ⭐ GoblinBomb 매핑 추가
                if (monsterID.Contains("ELITE"))
                    return "Elite_GoblinBombData";
                else if (monsterID.Contains("BOSS"))
                    return "GoblinBomb_BossData";
                else
                    return "GoblinBombData";
            }
            else if (monsterID.Contains("GOBLINCART") || monsterID.Contains("GOBLIN_CART"))
            {
                // ⭐ GoblinCart 매핑 추가
                if (monsterID.Contains("ELITE"))
                    return "Elite_GoblinCartData";
                else if (monsterID.Contains("BOSS"))
                    return "GoblinCart_BossData";
                else
                    return "GoblinCartData";
            }
            else if (monsterID.Contains("ORC"))
            {
                // ⭐ Elite_Orc 매핑 (GOBLIN 체크 이후에 배치)
                if (monsterID.Contains("ELITE"))
                    return "Elite_OrcData";
                else if (monsterID.Contains("BOSS"))
                    return "Orc_BossData";
                else
                    return "OrcData";
            }
            else if (monsterID.Contains("SANDELEMENTAL"))
            {
                // ⭐ Boss_SandElemental 매핑 추가
                if (monsterID.Contains("BOSS"))
                    return "Boss_SandElementalData";
                else if (monsterID.Contains("ELITE"))
                    return "Elite_SandElementalData";
                else
                    return "SandElementalData";
            }
            else if (monsterID.Contains("BOAR"))
            {
                // ⭐ Elite_Boar 매핑 추가
                if (monsterID.Contains("ELITE"))
                    return "Elite_BoarData";
                else if (monsterID.Contains("BOSS"))
                    return "Boar_BossData";
                else
                    return "BoarData";
            }
            else if (monsterID.Contains("FORESTELEMENTAL"))
            {
                // ⭐ Boss_ForestElemental 매핑 추가
                if (monsterID.Contains("BOSS"))
                    return "Boss_ForestElementalData";
                else if (monsterID.Contains("ELITE"))
                    return "Elite_ForestElementalData";
                else
                    return "ForestElementalData";
            }
            else if (monsterID.Contains("MUSHROOM"))
            {
                // 🍄 Mushroom 매핑 추가
                if (monsterID.Contains("ELITE"))
                    return "Elite_MushroomData";
                else if (monsterID.Contains("BOSS"))
                    return "Mushroom_BossData";
                else
                    return "MushroomData";
            }
            else if (monsterID.Contains("RABBIT"))
            {
                if (monsterID.Contains("ELITE"))
                    return "Elite_RabbitData";
                else if (monsterID.Contains("BOSS"))
                    return "Rabbit_BossData";
                else
                    return "RabbitData";
            }
            
            return $"{monsterID}Data"; // 기본값
        }
        
        /// <summary>
        /// 몬스터 ID로부터 풀 태그 생성 - MonsterID 직접 사용
        /// 기존 규칙: Bear(MON_BEAR_001), WaterGolem(MON_WATERGOLEM_001) 등 MonsterID 그대로 사용
        /// </summary>
        private string GetPoolTagFromMonsterID(string monsterID)
        {
            // 기존 규칙: MonsterID를 풀 태그로 직접 사용 (PoolConfig와 일치)
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
                    Debug.Log($"🐲 [StageManager] 보스 처치됨: {enemy.name} (Victory 조건: {stageConfig.Victory})");
                
                // ✅ Victory 조건이 BossKill일 때 승리 체크
                if (stageConfig.Victory == VictoryCondition.BossKill)
                {
                    // ✅ 타임리미트가 있는 경우 시간 체크
                    if (stageConfig.hasTimeLimit)
                    {
                        float elapsedTime = Time.time - stageStartTime;
                        
                        if (elapsedTime <= stageConfig.TimeLimitSec)
                        {
                            if (enableDebugLogs)
                                Debug.Log($"🏆 [StageManager] BossKill + 시간 안에 승리! {elapsedTime:F1}초");
                            
                            StartCoroutine(BossKillVictorySequence(true));
                        }
                        else
                        {
                            if (enableDebugLogs)
                                Debug.Log($"⏱️ [StageManager] 보스 처치했지만 시간 초과: {elapsedTime:F1}초");
                            
                            CompleteStage(false); // 시간 초과 패배는 연출 없이 즉시 처리
                        }
                    }
                    else
                    {
                        // 시간 제한 없음
                        if (enableDebugLogs)
                            Debug.Log($"🏆 [StageManager] 승리 조건 달성! (BossKill) - 보스 처치 연출 시작");
                        
                        if (CheckVictoryCondition())
                        {
                            StartCoroutine(BossKillVictorySequence(true));
                        }
                    }
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.Log($"📋 [StageManager] 보스 처치 완료, 승리 조건: {stageConfig.Victory} - 계속 진행");
                }
            }
            
            if (enableDebugLogs)
                Debug.Log($"🎯 [StageManager] 총 처치수: {totalEnemyKillCount}");
        }
        
        /// <summary>
        /// BossKill 승리 연출 코루틴
        /// 보스 사망 후 Death Effect가 재생되는 동안 대기한 뒤 승리 팝업 출력
        /// </summary>
        private IEnumerator BossKillVictorySequence(bool success)
        {
            if (enableDebugLogs)
                Debug.Log($"🐲 [StageManager] 보스 처치 연출 시작 — Death Effect 대기 {bossDeathEffectDuration}초");
            
            // Death Effect 재생 시간 동안 대기
            yield return new WaitForSecondsRealtime(bossDeathEffectDuration);
            
            if (enableDebugLogs)
                Debug.Log($"🐲 [StageManager] Death Effect 대기 완료 → {bossVictoryDelay}초 후 승리 처리");
            
            // 추가 여유 시간 후 승리
            yield return new WaitForSecondsRealtime(bossVictoryDelay);
            
            CompleteStage(success);
        }
        
        /// <summary>
        /// KillAll / ObjectiveComplete 승리 연출 코루틴
        /// 마지막 적 처치 또는 목표 달성 후 Death/파괴 이펙트 재생 시간만큼 대기한 뒤 승리 팝업 출력
        /// </summary>
        private IEnumerator VictorySequence(bool success, float delay)
        {
            if (enableDebugLogs)
                Debug.Log($"🏆 [StageManager] VictorySequence 시작 — {delay}초 대기 후 승리 처리");
            
            yield return new WaitForSecondsRealtime(delay);
            
            if (enableDebugLogs)
                Debug.Log($"🏆 [StageManager] VictorySequence 완료 → CompleteStage");
            
            CompleteStage(success);
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
        
        /// <summary>
        /// isVictoryTarget=true 바리케이드가 파괴될 때 Barricade.cs에서 호출
        /// Victory=ObjectiveComplete, objectiveType=BarricadeDestroy 스테이지에서만 유효
        /// 씬에 남은 isVictoryTarget=true 바리케이드가 없으면 승리 처리
        /// </summary>
        public void NotifyBarricadeDestroyed(Barricade destroyedBarricade)
        {
            if (stageConfig == null || stageConfig.Victory != VictoryCondition.ObjectiveComplete) return;
            if (!isStageActive) return;
            
            if (enableDebugLogs)
                Debug.Log($"🏚️ [StageManager] 목표 바리케이드 파괴: {destroyedBarricade.name}");
            
            // 씬에 남은 isVictoryTarget=true 바리케이드 중 아직 파괴되지 않은 것 탐색
            var allBarricades = Object.FindObjectsOfType<Barricade>();
            foreach (var barricade in allBarricades)
            {
                if (barricade.IsVictoryTarget && !barricade.IsBroken)
                {
                    if (enableDebugLogs)
                        Debug.Log($"🏚️ [StageManager] 아직 남은 목표 바리케이드: {barricade.name}");
                    return; // 아직 남아 있으면 승리 보류
                }
            }
            
            if (enableDebugLogs)
                Debug.Log($"🏆 [StageManager] 모든 목표 바리케이드 파괴 완료! VictorySequence 시작 ({victoryDelay}초 대기)");
            
            StartCoroutine(VictorySequence(true, victoryDelay));
        }

        #region ✅ 🎵 BGM 시스템 연동 (Phase 1.3 추가)
    
    /// <summary>
    /// 웨이브 시작/종료 시 BGM 처리
    /// </summary>
    private void HandleWaveBGM(WaveConfig waveConfig, bool isWaveStart)
    {
        if (BGMController.Instance == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("⚠️ [StageManager] BGMController가 없습니다!");
            return;
        }
        
        if (isWaveStart)
        {
            // 웨이브 시작: 보스 여부 체크
            bool hasBoss = CheckIfWaveHasBoss(waveConfig);
            
            if (hasBoss)
            {
                // 보스 웨이브: 보스 BGM
                BGMController.Instance.OnBossStart(currentStageId);
                
                if (enableDebugLogs)
                    Debug.Log($"🎵 [StageManager] 보스 BGM 시작: {currentStageId}");
            }
            else
            {
                // 일반 웨이브: 전투 BGM
                BGMController.Instance.OnBattleStart(currentStageId);
                
                if (enableDebugLogs)
                    Debug.Log($"🎵 [StageManager] 전투 BGM 시작: {currentStageId}");
            }
        }
        else
        {
            // 웨이브 종료: 이전 BGM으로 복귀
            bool hasBoss = CheckIfWaveHasBoss(waveConfig);
            
            if (hasBoss)
            {
                BGMController.Instance.OnBossEnd();
                
                if (enableDebugLogs)
                    Debug.Log($"🎵 [StageManager] 보스 BGM 종료");
            }
            else
            {
                BGMController.Instance.OnBattleEnd();
                
                if (enableDebugLogs)
                    Debug.Log($"🎵 [StageManager] 전투 BGM 종료");
            }
        }
    }
    
    /// <summary>
    /// 웨이브에 보스가 있는지 체크 (EnemyType 기반)
    /// </summary>
    private bool CheckIfWaveHasBoss(WaveConfig waveConfig)
    {
        if (waveConfig == null || waveConfig.SpawnGroups == null)
            return false;
        
        foreach (var spawnGroup in waveConfig.SpawnGroups)
        {
            if (spawnGroup.Monsters == null)
                continue;
            
            foreach (var monster in spawnGroup.Monsters)
            {
                // ✅ EnemyData의 EnemyType으로 보스 확인 (가장 정확)
                EnemyData enemyData = GetEnemyDataFromMonsterID(monster.MonsterID);
                if (enemyData != null)
                {
                    // EnemyType이 Boss인 경우
                    if (enemyData.EnemyType == EnemyType.Boss)
                    {
                        if (enableDebugLogs)
                            Debug.Log($"🐲 [StageManager] 보스 발견: {monster.MonsterID} (EnemyType: Boss)");
                        return true;
                    }
                    
                    // 또는 IsBoss 플래그가 true인 경우
                    if (enemyData.IsBoss)
                    {
                        if (enableDebugLogs)
                            Debug.Log($"🐲 [StageManager] 보스 발견: {monster.MonsterID} (IsBoss: true)");
                        return true;
                    }
                }
                else
                {
                    // ✅ Fallback: EnemyData 로드 실패 시 MonsterID로 판단 (레거시 호환)
                    if (monster.MonsterID.Contains("BOSS") || monster.MonsterID.Contains("Boss"))
                    {
                        if (enableDebugLogs)
                            Debug.LogWarning($"⚠️ [StageManager] 보스 감지 (MonsterID 기반): {monster.MonsterID} - EnemyData 로드 실패");
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    #endregion
    
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
    
    #region 🎒 Phase 3.5: V2 인벤토리 자동 전송
    
    /// <summary>
    /// 스테이지 종료 시 V2 가방 아이템을 계정 창고로 자동 이동.
    /// 클리어/실패/포기 모두 이 메서드를 통해 이관하므로 public.
    /// PauseMenuController(포기) 등 외부에서도 호출 가능.
    /// </summary>
    public void TransferItemsToAccount()
    {
        // StageEndItemTransfer 컴포넌트 찾기
        var transfer = GetComponent<StageEndItemTransfer>();
        
        if (transfer == null)
        {
            // 없으면 동적 생성
            transfer = gameObject.AddComponent<StageEndItemTransfer>();
            transfer.enableLogs = enableDebugLogs;
            
            if (enableDebugLogs)
            {
                Debug.Log("✨ [StageManager] StageEndItemTransfer 컴포넌트 동적 생성");
            }
        }
        
        // 아이템 전송 실행
        transfer.TransferItemsToAccount();
    }
    
    #endregion
    
    #region ⚡ 콘텐츠 입장 제한 시스템 (Phase D-Revision)
    
    /// <summary>
    /// 스테이지 입장 재화 실제 차감 (입장 시점에 즉시 호출)
    /// ⚡ Phase D-Revision: 로비에서 검증 완료 전제, 입장 시 무조건 차감
    /// </summary>
    private void ConsumeStageEntryCost()
    {
        if (stageConfig == null || ContentEntryManager.Instance == null)
            return;
        
        // 일반 스테이지: 스태미나 차감
        if (!stageConfig.IsDungeon)
        {
            int requiredStamina = stageConfig.requiredStamina;
            ContentEntryManager.Instance.ConsumeStamina(requiredStamina);
            
            if (enableDebugLogs)
                Debug.Log($"⚡ [StageManager] 스태미나 차감 완료: {stageConfig.StageID} (-{requiredStamina})");
        }
        // 던전: 카테고리 입장 횟수 차감
        else
        {
            // StageConfig의 categoryId 먼저 확인, 없으면 자동 추출
            string categoryId = !string.IsNullOrEmpty(stageConfig.categoryId) 
                ? stageConfig.categoryId 
                : ContentEntryManager.GetCategoryIdFromDungeonId(stageConfig.StageID);
            
            ContentEntryManager.Instance.ConsumeDungeonEntry(categoryId);
            
            if (enableDebugLogs)
                Debug.Log($"🏰 [StageManager] 던전 카테고리 입장 횟수 차감 완료: {stageConfig.StageID} (카테고리: {categoryId})");
        }
    }
    
    #endregion
}  // ✅ StageManager 클래스 닫기
}  // ✅ StageSystem 네임스페이스 닫기