using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 스테이지 진행도 관리 시스템 - 슬롯별 독립 관리
    /// </summary>
    public class StageProgressManager : MonoBehaviour
    {
        public static StageProgressManager Instance { get; private set; }

        [Header("📊 진행도 관리")]
        [SerializeField] private bool enableDebugLogs = true;
        private int currentSlotIndex = -1; // 슬롯 전환 감지용
        [SerializeField] private bool isInitialized = false;
        
        // 현재 슬롯 진행도 캐시
        private Dictionary<string, StageProgress> progressCache = new Dictionary<string, StageProgress>();
        
        // Public 프로퍼티
        public bool IsInitialized => isInitialized;
        public int GetCurrentSlotIndex() => currentSlotIndex;
        
        // 이벤트
        public System.Action<string> OnStageUnlocked;
        public System.Action<string, bool> OnStageCompleted; // stageId, isFirstClear
        public System.Action<string> OnFirstClearRewardClaimed;
        
        // 런타임 캐시
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
            DontDestroyOnLoad(gameObject);
            InitializeProgressSystem();
        }
        
        /// <summary>
        /// 진행도 시스템 초기화
        /// </summary>
        private void InitializeProgressSystem()
        {
            if (enableDebugLogs)
                Debug.Log("[StageProgressManager] 진행도 시스템 초기화 시작");
            
            // 현재 선택된 슬롯 인덱스 가져오기
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
            {
                currentSlotIndex = PlayerDataManager.Instance.GetCurrentSlotIndex();
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[StageProgressManager] PlayerDataManager 또는 선택된 슬롯이 없습니다. 기본값(0) 사용");
                currentSlotIndex = 0;
            }
            
            LoadAllStageConfigs();
            InitializeProgressCache();
            CheckAutoUnlocks();
            
            isInitialized = true;
            
            if (enableDebugLogs)
                Debug.Log("[StageProgressManager] 진행도 시스템 초기화 완료");
        }
        
        /// <summary>
        /// 모든 스테이지 설정 로드
        /// </summary>
        private void LoadAllStageConfigs()
        {
            allStageConfigs = new List<StageConfig>();
            StageConfig[] configs = Resources.LoadAll<StageConfig>("Stages/Configs");
            allStageConfigs.AddRange(configs);
            
            if (enableDebugLogs)
                Debug.Log($"[StageProgressManager] {allStageConfigs.Count}개 스테이지 설정 로드됨");
        }
        
        /// <summary>
        /// 진행도 캐시 초기화
        /// </summary>
        private void InitializeProgressCache()
        {
            if (enableDebugLogs)
                Debug.Log($"[StageProgressManager] 슬롯 {currentSlotIndex} 캐시 초기화");
            
            LoadCurrentSlotProgress();
        }
        
        /// <summary>
        /// 현재 슬롯의 진행도 로드
        /// </summary>
        private void LoadCurrentSlotProgress()
        {
            if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[StageProgressManager] PlayerDataManager 또는 선택된 슬롯이 없습니다");
                return;
            }
            
            // 슬롯 전환 감지 및 캐시 초기화
            int newSlotIndex = PlayerDataManager.Instance.CurrentSlotIndex;
            if (newSlotIndex != currentSlotIndex)
            {
                if (enableDebugLogs)
                    Debug.Log($"[StageProgressManager] 슬롯 전환 감지: {currentSlotIndex} → {newSlotIndex}");
                progressCache.Clear();
                currentSlotIndex = newSlotIndex;
            }
            
            var savedProgresses = PlayerDataManager.Instance.GetStageProgresses();
            if (enableDebugLogs)
                Debug.Log($"[StageProgressManager] 슬롯 {currentSlotIndex}에서 로드된 진행도 개수: {savedProgresses.Count}");
            
            // 저장된 데이터로 캐시 업데이트
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
                    
                    // ✅ Phase 6: CH01_ST01 기본 해금 (레거시 STAGE_001 제거)
                    if (config.StageID == "CH01_ST01")
                    {
                        newProgress.isUnlocked = true;
                    }
                    
                    progressCache[config.StageID] = newProgress;
                }
            }
        }
        
        /// <summary>
        /// 스테이지 완료 처리
        /// </summary>
        public void CompleteStage(string stageId, float completionTime = 0f, bool isFirstClear = false)
        {
            if (progressCache.ContainsKey(stageId))
            {
                var progress = progressCache[stageId];
                progress.isCompleted = true;
                
                if (completionTime > 0)
                {
                    progress.CompleteStage((int)completionTime);
                }
                
                if (enableDebugLogs)
                    Debug.Log($"[StageProgressManager] 스테이지 완료: {stageId}");
                
                CheckAutoUnlocks();
                
                // ✅ Phase 1: 챕터 완료 체크 (Stage 10 클리어 시)
                if (StageIdValidator.IsValidChapterStageId(stageId))
                {
                    int stageIndex = StageIdValidator.ExtractStageIndex(stageId);
                    if (stageIndex == 10)
                    {
                        int chapterId = StageIdValidator.ExtractChapterId(stageId);
                        
                        if (enableDebugLogs)
                            Debug.Log($"🎉 [StageProgressManager] 챕터 {chapterId} 최종 스테이지 클리어!");
                        
                        // 챕터 완료는 Phase 5에서 처리 (로비 복귀 후 컷신 재생)
                        // 여기서는 다음 챕터 해금만 체크
                        CheckChapterUnlocks();
                    }
                }
                
                SaveProgressesToPlayerData();
            }
        }
        
        /// <summary>
        /// 자동 해금 체크
        /// </summary>
        private void CheckAutoUnlocks()
        {
            if (enableDebugLogs)
                Debug.Log($"[StageProgressManager] 자동 해금 체크 시작 - 설정된 스테이지 수: {allStageConfigs.Count}");
            
            bool hasNewUnlocks = false;
            
            foreach (var config in allStageConfigs)
            {
                if (enableDebugLogs)
                    Debug.Log($"[StageProgressManager] {config.StageID} 체크 중 - UnlockCondition: '{config.UnlockCondition}'");
                
                if (!IsStageUnlocked(config.StageID))
                {
                    if (config.UnlockCondition == "AlwaysUnlocked" || 
                        (config.UnlockCondition.StartsWith("Clear:") && 
                         IsStageCompleted(config.UnlockCondition.Substring(6))))
                    {
                        AutoUnlockStage(config.StageID);
                        hasNewUnlocks = true;
                    }
                }
            }
            
            if (!hasNewUnlocks && enableDebugLogs)
            {
                Debug.Log("[StageProgressManager] 새로 해금된 스테이지 없음");
            }
        }
        
        /// <summary>
        /// 스테이지 해금 (자동 해금용)
        /// </summary>
        private void AutoUnlockStage(string stageId)
        {
            if (progressCache.ContainsKey(stageId))
            {
                progressCache[stageId].isUnlocked = true;
                
                if (enableDebugLogs)
                    Debug.Log($"[StageProgressManager] 자동 해금: {stageId}");
                
                OnStageUnlocked?.Invoke(stageId);
                SaveProgressesToPlayerData();
            }
        }
        
        /// <summary>
        /// 스테이지 해금 상태 확인
        /// </summary>
        public bool IsStageUnlocked(string stageId)
        {
            if (!isInitialized || progressCache == null)
            {
                // ✅ Phase 6: CH01_ST01 기본 해금 (레거시 STAGE_001 제거)
                return stageId == "CH01_ST01";
            }
            
            if (progressCache.ContainsKey(stageId))
            {
                return progressCache[stageId].isUnlocked;
            }
            
            // ✅ Phase 6: CH01_ST01 기본 해금 (레거시 STAGE_001 제거)
            return stageId == "CH01_ST01";
        }
        
        /// <summary>
        /// 스테이지 완료 상태 확인
        /// </summary>
        public bool IsStageCompleted(string stageId)
        {
            if (!isInitialized || progressCache == null)
            {
                return false;
            }
            
            if (progressCache.ContainsKey(stageId))
            {
                return progressCache[stageId].isCompleted;
            }
            
            return false;
        }
        
        /// <summary>
        /// 진행도를 PlayerDataManager에 저장
        /// </summary>
        private void SaveProgressesToPlayerData()
        {
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
            {
                var progressList = new List<StageProgress>(progressCache.Values);
                PlayerDataManager.Instance.UpdateStageProgresses(progressList);
                
                if (enableDebugLogs)
                    Debug.Log($"[StageProgressManager] 슬롯 {currentSlotIndex} 진행도 저장 완료");
            }
        }
        
        /// <summary>
        /// 특정 슬롯으로 강제 초기화
        /// </summary>
        public void InitializeFor(int slotIndex)
        {
            if (enableDebugLogs)
                Debug.Log($"[StageProgressManager] 슬롯 {slotIndex}로 강제 재초기화");
            
            LoadAllStageConfigs();
            InitializeProgressCache();
            CheckAutoUnlocks();
            
            isInitialized = true;
            
            if (enableDebugLogs)
                Debug.Log($"[StageProgressManager] 슬롯 {slotIndex} 초기화 완료");
        }
        
        /// <summary>
        /// 특정 스테이지 진행도 가져오기
        /// </summary>
        public StageProgress GetStageProgress(string stageId)
        {
            if (!isInitialized || progressCache == null)
            {
                return null;
            }
            
            if (progressCache.ContainsKey(stageId))
            {
                return progressCache[stageId];
            }
            
            return null;
        }
        
        /// <summary>
        /// 스테이지 강제 해금 (디버그용)
        /// </summary>
        public void UnlockStage(string stageId)
        {
            if (progressCache.ContainsKey(stageId))
            {
                progressCache[stageId].isUnlocked = true;
                
                if (enableDebugLogs)
                    Debug.Log($"[StageProgressManager] 강제 해금: {stageId}");
                
                OnStageUnlocked?.Invoke(stageId);
                SaveProgressesToPlayerData();
            }
        }
        
        // ========================================
        // ✅ Phase 1: 챕터 진행도 관리
        // ========================================
        
        /// <summary>
        /// 챕터 해금 여부 확인
        /// </summary>
        public bool IsChapterUnlocked(int chapterId)
        {
            if (chapterId <= 0 || chapterId > 5)
            {
                Debug.LogWarning($"[StageProgressManager] 잘못된 챕터 ID: {chapterId}");
                return false;
            }
            
            // Chapter 1은 기본 해금
            if (chapterId == 1)
                return true;
            
            // Chapter N은 Chapter N-1 클리어 시 해금
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
            {
                var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
                if (slotData != null)
                {
                    return slotData.IsChapterCleared(chapterId - 1);
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 챕터 클리어 여부 확인
        /// </summary>
        public bool IsChapterCleared(int chapterId)
        {
            if (chapterId <= 0 || chapterId > 5)
            {
                Debug.LogWarning($"[StageProgressManager] 잘못된 챕터 ID: {chapterId}");
                return false;
            }
            
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
            {
                var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
                if (slotData != null)
                {
                    return slotData.IsChapterCleared(chapterId);
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 챕터 완료 처리 (명시적 호출용, 테스트/치트 전용)
        /// </summary>
        public void CompleteChapter(int chapterId)
        {
            if (chapterId <= 0 || chapterId > 5)
            {
                Debug.LogWarning($"[StageProgressManager] 잘못된 챕터 ID: {chapterId}");
                return;
            }
            
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
            {
                var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
                if (slotData != null)
                {
                    // ✅ 수정: 이미 클리어된 챕터는 다시 기록하지 않음
                    if (slotData.IsChapterCleared(chapterId))
                    {
                        if (enableDebugLogs)
                            Debug.Log($"⚠️ [StageProgressManager] 챕터 {chapterId}는 이미 클리어됨");
                        return;
                    }
                    
                    slotData.MarkChapterAsCleared(chapterId);
                    PlayerDataManager.Instance.SaveCurrentSlot();
                    
                    if (enableDebugLogs)
                        Debug.Log($"✅ [StageProgressManager] 챕터 {chapterId} 완료 기록!");
                    
                    // ✅ 수정: CheckChapterUnlocks() 호출 제거 (무한 재귀 방지)
                    // CompleteChapter()는 명시적 호출이므로 자동 해금은 하지 않음
                    // 자동 해금은 CompleteStage()의 CheckChapterUnlocks()에서만 처리
                }
            }
        }
        
        /// <summary>
        /// 클리어한 챕터 목록 가져오기
        /// </summary>
        public List<int> GetClearedChapters()
        {
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
            {
                var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
                if (slotData != null)
                {
                    return new List<int>(slotData.clearedChapters);
                }
            }
            
            return new List<int>();
        }
        
        /// <summary>
        /// 챕터 해금 규칙 체크
        /// </summary>
        private void CheckChapterUnlocks()
        {
            // Chapter 1은 항상 해금
            // Chapter N은 Chapter N-1의 Stage 10 클리어 시 자동 해금
            
            if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[StageProgressManager] CheckChapterUnlocks - 슬롯이 선택되지 않음");
                return;
            }
            
            var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
            if (slotData == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[StageProgressManager] CheckChapterUnlocks - slotData가 null");
                return;
            }
            
            for (int chapter = 2; chapter <= 5; chapter++)
            {
                // ✅ 수정: 이미 해금된 챕터는 스킵 (무한 루프 방지)
                if (IsChapterUnlocked(chapter))
                    continue;
                
                // 이전 챕터의 Stage 10 (마지막 스테이지) 확인
                string prevChapterLastStage = $"CH{(chapter-1):D2}_ST10";
                
                if (IsStageCompleted(prevChapterLastStage))
                {
                    // ✅ 수정: CompleteChapter() 호출 제거 (무한 재귀 방지)
                    // 직접 PlayerSlotData에 기록
                    int prevChapter = chapter - 1;
                    
                    if (!slotData.IsChapterCleared(prevChapter))
                    {
                        slotData.MarkChapterAsCleared(prevChapter);
                        
                        if (enableDebugLogs)
                            Debug.Log($"✅ [StageProgressManager] 챕터 {prevChapter} 자동 완료 기록 (Stage 10 클리어)");
                    }
                    
                    // 현재 챕터의 첫 스테이지 해금
                    string currentChapterFirstStage = $"CH{chapter:D2}_ST01";
                    AutoUnlockStage(currentChapterFirstStage);
                    
                    if (enableDebugLogs)
                        Debug.Log($"🎉 [StageProgressManager] 챕터 {chapter} 해금! (챕터 {prevChapter} 완료)");
                    
                    // ✅ 추가: 변경사항 저장
                    PlayerDataManager.Instance.SaveCurrentSlot();
                }
            }
        }
    }
}
