using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 스테이지 진행도 관리 시스템
    /// PlayerDataManager와 연동하여 저장/로드 처리
    /// </summary>
    public class StageProgressManager : MonoBehaviour
    {
        public static StageProgressManager Instance { get; private set; }

        [Header("디버그")]
        public bool enableDebugLogs = true;
        
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
            
            // 모든 스테이지 설정 로드
            LoadAllStageConfigs();
            
            // 진행도 캐시 초기화
            InitializeProgressCache();
            
            // 자동 해금 체크
            CheckAutoUnlocks();
            
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
        /// 진행도 캐시 초기화
        /// </summary>
        private void InitializeProgressCache()
        {
            progressCache = new Dictionary<string, StageProgress>();
            
            // PlayerDataManager에서 저장된 진행도 로드
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
                        Debug.Log($"🆕 [StageProgressManager] 신규 진행도 생성: {config.StageID}");
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
        /// 스테이지 해금 상태 확인
        /// </summary>
        public bool IsStageUnlocked(string stageId)
        {
            if (progressCache.TryGetValue(stageId, out StageProgress progress))
            {
                return progress.isUnlocked;
            }
            return false;
        }
        
        /// <summary>
        /// 스테이지 완료 상태 확인
        /// </summary>
        public bool IsStageCompleted(string stageId)
        {
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
        /// 진행도를 PlayerDataManager에 저장
        /// </summary>
        private void SaveProgressesToPlayerData()
        {
            var progressList = progressCache.Values.ToList();
            PlayerDataManager.Instance.UpdateStageProgresses(progressList);
        }
        
        /// <summary>
        /// 디버그용 전체 진행도 출력
        /// </summary>
        [ContextMenu("Print All Progress")]
        public void PrintAllProgress()
        {
            Debug.Log("📊 [StageProgressManager] 전체 진행도:");
            foreach (var progress in progressCache.Values.OrderBy(p => p.stageId))
            {
                Debug.Log($"  {progress}");
            }
        }
    }
}
