using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 스테이지 진행도 관리 시스템
    /// 🔧 수정: 캐릭터별(슬롯별) 진행도 관리
    /// </summary>
    public class StageProgressManager : MonoBehaviour
    {
        public static StageProgressManager Instance { get; private set; }

        [Header("디버그")]
        public bool enableDebugLogs = true;
        
        // 🆕 초기화 상태 확인용 프로퍼티 추가
        public bool IsInitialized { get; private set; } = false;
        
        // 🆕 현재 슬롯 추적
        private int currentSlotIndex = -1;
        
        // 이벤트
        public System.Action<string> OnStageUnlocked;
        public System.Action<string, bool> OnStageCompleted; // stageId, isFirstClear
        public System.Action<string> OnFirstClearRewardClaimed;
        
        // 런타임 캐시
        private Dictionary<string, StageProgress> progressCache;
        private List<StageConfig> allStageConfigs;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // 씬 전환 시에도 유지
            DontDestroyOnLoad(gameObject);
            
            InitializeProgressSystem();
        }
        
        /// <summary>
        /// 진행도 시스템 초기화
        /// </summary>
        private void InitializeProgressSystem()
        {
            if (enableDebugLogs)
                Debug.Log("🎯 [StageProgressManager] 진행도 시스템 초기화 시작...");
            
            // 🆕 현재 선택된 슬롯 인덱스 가져오기
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
            {
                currentSlotIndex = PlayerDataManager.Instance.GetCurrentSlotIndex();
                if (enableDebugLogs)
                    Debug.Log($"🎯 [StageProgressManager] 현재 슬롯: {currentSlotIndex}");
            }
            else
            {
                Debug.LogWarning("[StageProgressManager] PlayerDataManager 또는 선택된 슬롯이 없습니다. 기본값(0) 사용.");
                currentSlotIndex = 0; // 🆕 기본값 설정
            }
            
            // 모든 스테이지 설정 로드
            LoadAllStageConfigs();
            
            // 진행도 캐시 초기화
            InitializeProgressCache();
            
            // 자동 해금 체크
            CheckAutoUnlocks();
            
            // 🆕 초기화 완료 플래그 설정
            IsInitialized = true;
            
            if (enableDebugLogs)
                Debug.Log("✅ [StageProgressManager] 진행도 시스템 초기화 완료");
        }
        
        /// <summary>
        /// 모든 스테이지 설정 로드
        /// </summary>
        private void LoadAllStageConfigs()
        {
            allStageConfigs = new List<StageConfig>();
            
            // Resources/Stages/Configs 폴더에서 모든 StageConfig 로드
            StageConfig[] configs = Resources.LoadAll<StageConfig>("Stages/Configs");
            allStageConfigs.AddRange(configs);
            
            if (enableDebugLogs)
                Debug.Log($"📄 [StageProgressManager] {allStageConfigs.Count}개 스테이지 설정 로드됨");
        }
        
        /// <summary>
        /// 🔧 수정: 진행도 캐시 초기화 (슬롯별)
        /// </summary>
        private void InitializeProgressCache()
        {
            progressCache = new Dictionary<string, StageProgress>();
            
            // 🆕 현재 선택된 슬롯의 진행도만 로드
            RefreshProgressForCurrentSlot();
            
            if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
            {
                Debug.LogWarning("[StageProgressManager] PlayerDataManager 또는 선택된 슬롯이 없습니다.");
                return;
            }
            
            // 🔧 수정: 현재 슬롯의 진행도만 로드
            var savedProgresses = PlayerDataManager.Instance.GetStageProgresses();
            
            foreach (var progress in savedProgresses)
            {
                progressCache[progress.stageId] = progress;
            }
            
            // 설정에 있지만 진행도가 없는 스테이지들 기본값 생성
            foreach (var config in allStageConfigs)
            {
                if (!progressCache.ContainsKey(config.StageID))
                {
                    var newProgress = new StageProgress(config.StageID);
                    
                    // STAGE_001은 기본 해금
                    if (config.StageID == "STAGE_001")
                    {
                        newProgress.isUnlocked = true;
                    }
                    
                    progressCache[config.StageID] = newProgress;
                    
                    if (enableDebugLogs)
                        Debug.Log($"🆕 [StageProgressManager] 슬롯 {currentSlotIndex} 신규 진행도 생성: {config.StageID}");
                }
            }
            
            // 변경사항 저장
            SaveProgressesToPlayerData();
        }
        
        /// <summary>
        /// 자동 해금 체크
        /// </summary>
        private void CheckAutoUnlocks()
        {
            bool hasNewUnlocks = false;
            
            foreach (var config in allStageConfigs)
            {
                if (IsStageUnlocked(config.StageID))
                    continue;
                
                var unlockData = config.UnlockData;
                
                if (unlockData.IsAlwaysUnlocked)
                {
                    UnlockStageInternal(config.StageID);
                    hasNewUnlocks = true;
                }
                else if (!string.IsNullOrEmpty(unlockData.RequiredStageId))
                {
                    if (IsStageCompleted(unlockData.RequiredStageId))
                    {
                        UnlockStageInternal(config.StageID);
                        hasNewUnlocks = true;
                        
                        if (enableDebugLogs)
                            Debug.Log($"🔓 [StageProgressManager] 자동 해금: {config.StageID} (선행: {unlockData.RequiredStageId})");
                    }
                }
            }
            
            if (hasNewUnlocks)
            {
                SaveProgressesToPlayerData();
            }
        }
        
        /// <summary>
        /// 🔧 수정: 안전한 스테이지 해금 상태 확인 (디버깅 강화)
        /// </summary>
        public bool IsStageUnlocked(string stageId)
        {
            // 🆕 초기화 확인 추가
            if (!IsInitialized || progressCache == null)
            {
                Debug.LogWarning($"[StageProgressManager] 아직 초기화되지 않음. {stageId} 기본값 반환.");
                return stageId == "STAGE_001"; // STAGE_001만 기본 해금
            }
            
            if (progressCache.TryGetValue(stageId, out StageProgress progress))
            {
                // 🆕 상세 디버깅 로그 추가
                if (enableDebugLogs)
                    Debug.Log($"🔍 [StageProgressManager] {stageId} 해금상태: {progress.isUnlocked} (슬롯: {currentSlotIndex})");
                
                return progress.isUnlocked;
            }
            
            // 🆕 progressCache에 없는 경우 디버깅
            Debug.LogWarning($"[StageProgressManager] {stageId}가 progressCache에 없습니다. 현재 캐시 개수: {progressCache.Count}");
            
            // progressCache에 있는 모든 키 출력
            if (enableDebugLogs)
            {
                Debug.Log($"[StageProgressManager] 현재 캐시 키들: {string.Join(", ", progressCache.Keys)}");
            }
            
            return false;
        }
        
        /// <summary>
        /// 🔧 수정: 안전한 스테이지 완료 상태 확인
        /// </summary>
        public bool IsStageCompleted(string stageId)
        {
            // 🆕 초기화 확인 추가
            if (!IsInitialized || progressCache == null)
            {
                Debug.LogWarning($"[StageProgressManager] 아직 초기화되지 않음. {stageId} 기본값 반환.");
                return false;
            }
            
            if (progressCache.TryGetValue(stageId, out StageProgress progress))
            {
                return progress.isCompleted;
            }
            return false;
        }
        
        /// <summary>
        /// 스테이지 해금
        /// </summary>
        public void UnlockStage(string stageId)
        {
            UnlockStageInternal(stageId);
            SaveProgressesToPlayerData();
            
            OnStageUnlocked?.Invoke(stageId);
            
            if (enableDebugLogs)
                Debug.Log($"🔓 [StageProgressManager] 스테이지 해금: {stageId}");
        }
        
        /// <summary>
        /// 내부 해금 처리 (저장 안함)
        /// </summary>
        private void UnlockStageInternal(string stageId)
        {
            if (progressCache.TryGetValue(stageId, out StageProgress progress))
            {
                progress.isUnlocked = true;
            }
            else
            {
                var newProgress = new StageProgress(stageId, true);
                progressCache[stageId] = newProgress;
            }
        }
        
        /// <summary>
        /// 스테이지 완료 처리
        /// </summary>
        public void CompleteStage(string stageId, int clearTime)
        {
            if (!progressCache.TryGetValue(stageId, out StageProgress progress))
            {
                Debug.LogWarning($"⚠️ [StageProgressManager] 존재하지 않는 스테이지: {stageId}");
                return;
            }
            
            bool isFirstClear = !progress.isCompleted;
            progress.CompleteStage(clearTime);
            
            // 자동 해금 체크
            CheckAutoUnlocks();
            
            SaveProgressesToPlayerData();
            
            OnStageCompleted?.Invoke(stageId, isFirstClear);
            
            if (enableDebugLogs)
            {
                Debug.Log($"🏆 [StageProgressManager] 스테이지 완료: {stageId} " +
                         $"(시간: {clearTime}초, 첫클리어: {isFirstClear})");
            }
        }
        
        /// <summary>
        /// 첫 클리어 보상 수령 가능 여부
        /// </summary>
        public bool CanClaimFirstClearReward(string stageId)
        {
            if (progressCache.TryGetValue(stageId, out StageProgress progress))
            {
                return progress.CanClaimFirstClearReward();
            }
            return false;
        }
        
        /// <summary>
        /// 첫 클리어 보상 수령
        /// </summary>
        public void ClaimFirstClearReward(string stageId)
        {
            if (progressCache.TryGetValue(stageId, out StageProgress progress))
            {
                if (progress.CanClaimFirstClearReward())
                {
                    progress.ClaimFirstClearReward();
                    SaveProgressesToPlayerData();
                    
                    OnFirstClearRewardClaimed?.Invoke(stageId);
                    
                    if (enableDebugLogs)
                        Debug.Log($"🎁 [StageProgressManager] 첫 클리어 보상 수령: {stageId}");
                }
            }
        }
        
        /// <summary>
        /// 스테이지 진행도 조회
        /// </summary>
        public StageProgress GetStageProgress(string stageId)
        {
            progressCache.TryGetValue(stageId, out StageProgress progress);
            return progress;
        }
        
        /// <summary>
        /// 해금된 스테이지 ID 목록
        /// </summary>
        public List<string> GetUnlockedStageIds()
        {
            return progressCache.Values
                .Where(p => p.isUnlocked)
                .Select(p => p.stageId)
                .ToList();
        }
        
        /// <summary>
        /// 완료된 스테이지 ID 목록
        /// </summary>
        public List<string> GetCompletedStageIds()
        {
            return progressCache.Values
                .Where(p => p.isCompleted)
                .Select(p => p.stageId)
                .ToList();
        }
        
        /// <summary>
        /// 🔧 수정: 진행도를 현재 슬롯에 저장
        /// </summary>
        private void SaveProgressesToPlayerData()
        {
            if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
            {
                Debug.LogWarning("[StageProgressManager] 저장할 슬롯이 선택되지 않았습니다.");
                return;
            }
            
            var progressList = progressCache.Values.ToList();
            PlayerDataManager.Instance.UpdateStageProgresses(progressList);
            
            if (enableDebugLogs)
                Debug.Log($"💾 [StageProgressManager] 슬롯 {currentSlotIndex} 진행도 저장 완료");
        }
        
        /// <summary>
        /// 🆕 현재 선택된 슬롯 변경 시 진행도 갱신
        /// </summary>
        public void RefreshProgressForCurrentSlot()
        {
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
            {
                int newSlotIndex = PlayerDataManager.Instance.GetCurrentSlotIndex();
                
                // 슬롯이 변경되었거나 처음 초기화하는 경우
                if (newSlotIndex != currentSlotIndex)
                {
                    currentSlotIndex = newSlotIndex;
                    
                    if (enableDebugLogs)
                        Debug.Log($"🔄 [StageProgressManager] 슬롯 변경: {currentSlotIndex}");
                    
                    // 🔧 수정: 진행도 캐시 다시 로드
                    if (IsInitialized)
                    {
                        InitializeProgressCache();
                    }
                }
            }
            else
            {
                Debug.LogWarning("[StageProgressManager] PlayerDataManager 또는 선택된 슬롯이 없습니다.");
                currentSlotIndex = 0; // 🆕 기본값 설정
            }
        }
        
        /// <summary>
        /// 🆕 특정 슬롯으로 강제 초기화 (2단계: 순서 고정)
        /// </summary>
        public void InitializeFor(int slotIndex)
        {
            if (enableDebugLogs)
                Debug.Log($"🔄 [StageProgressManager] 슬롯 {slotIndex}로 강제 재초기화");
            
            currentSlotIndex = slotIndex;
            
            // 🔧 수정: 스테이지 설정 먼저 로드
            if (allStageConfigs == null || allStageConfigs.Count == 0)
            {
                LoadAllStageConfigs();
            }
            
            // 진행도 캐시 다시 로드
            InitializeProgressCache();
            
            // 자동 해금 체크
            CheckAutoUnlocks();
            
            // 초기화 완료
            IsInitialized = true;
            
            if (enableDebugLogs)
                Debug.Log($"✅ [StageProgressManager] 슬롯 {slotIndex} 초기화 완료");
        }
    }
}
