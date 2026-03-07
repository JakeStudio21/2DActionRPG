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
        /// 모든 스테이지 설정 로드 (챕터 스테이지 + 던전)
        /// </summary>
        private void LoadAllStageConfigs()
        {
            allStageConfigs = new List<StageConfig>();
            
            // 챕터 스테이지 로드
            StageConfig[] chapterConfigs = Resources.LoadAll<StageConfig>("Stages/Configs/Chapters");
            allStageConfigs.AddRange(chapterConfigs);
            
            // 🏰 던전 로드
            StageConfig[] dungeonConfigs = Resources.LoadAll<StageConfig>("Stages/Configs/Dungeons");
            allStageConfigs.AddRange(dungeonConfigs);
            
            if (enableDebugLogs)
            {
                int chapterCount = chapterConfigs.Length;
                int dungeonCount = dungeonConfigs.Length;
                Debug.Log($"[StageProgressManager] 스테이지 {chapterCount}개, 던전 {dungeonCount}개 로드 완료 (총 {allStageConfigs.Count}개)");
            }
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
            // 🏰 Phase 1: 던전은 CompleteDungeon()으로 처리해야 함
            if (StageIdValidator.IsDungeon(stageId))
            {
                Debug.LogWarning($"⚠️ [StageProgressManager] 던전({stageId})은 CompleteStage()가 아닌 CompleteDungeon()을 사용해야 합니다!");
                return;
            }
            
            if (progressCache.ContainsKey(stageId))
            {
                var progress = progressCache[stageId];
                progress.isCompleted = true;
                
                if (completionTime > 0)
                {
                    progress.CompleteStage((int)completionTime, isFirstClear); // ⭐ isFirstClear 전달
                }
                
                if (enableDebugLogs)
                {
                    Debug.Log($"[StageProgressManager] 스테이지 완료: {stageId}");
                    Debug.Log($"   - 첫 클리어: {isFirstClear}");
                    Debug.Log($"   - 첫 클리어 보상 지급: {progress.isFirstClearRewarded}");
                }
                
                CheckAutoUnlocks();
                
                // ========================================
                // 📌 Phase 6: SelectedPlayerData 업데이트
                // ========================================
                if (PlayerDataManager.Instance != null)
                {
                    var selectedData = PlayerDataManager.Instance.selectedPlayerData;
                    if (selectedData != null && StageIdValidator.IsValidChapterStageId(stageId))
                    {
                        int chapterId = StageIdValidator.ExtractChapterId(stageId);
                        if (chapterId > 0)
                        {
                            // 마지막 플레이 위치 업데이트
                            selectedData.currentChapterId = chapterId;
                            selectedData.lastPlayedStageId = stageId;
                            
                            if (enableDebugLogs)
                                Debug.Log($"📍 [StageProgressManager] 마지막 플레이 위치 업데이트: Chapter {chapterId}, Stage {stageId}");
                        }
                    }
                }
                
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
                
                // 🔧 의미 있는 이벤트: 스테이지 진행도 변경 → 즉시 저장
                PlayerDataManager.Instance.SaveOnMeaningfulEvent("StageProgressUpdated");
                
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
        /// ⭐ 진행도 캐시 강제 초기화 (슬롯 삭제 시 사용)
        /// </summary>
        public void ClearProgressCache()
        {
            if (progressCache != null)
            {
                progressCache.Clear();
                currentSlotIndex = -1;
                
                if (enableDebugLogs)
                    Debug.Log($"[StageProgressManager] 진행도 캐시 완전 초기화 (currentSlotIndex → -1)");
            }
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
                    
                    // ========================================
                    // 📌 Phase 6: SelectedPlayerData 업데이트
                    // ========================================
                    var selectedData = PlayerDataManager.Instance.selectedPlayerData;
                    if (selectedData != null && !selectedData.clearedChapters.Contains(chapterId))
                    {
                        selectedData.clearedChapters.Add(chapterId);
                        
                        if (enableDebugLogs)
                            Debug.Log($"📊 [StageProgressManager] SelectedPlayerData.clearedChapters 업데이트: {chapterId} 추가");
                    }
                    
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
        
        // ========================================
        // 🏰 Phase 1: 던전 진행도 관리
        // ========================================
        
        /// <summary>
        /// 던전 클리어 여부 확인
        /// </summary>
        public bool IsDungeonCleared(string dungeonId)
    {
        var playerData = PlayerDataManager.Instance?.selectedPlayerData;
        if (playerData == null) return false;
        
        return playerData.clearedDungeons.Contains(dungeonId);
        }
        
        /// <summary>
        /// 던전 진행도 가져오기 (없으면 null)
        /// </summary>
        public DungeonProgress GetDungeonProgress(string dungeonId)
        {
            var playerData = PlayerDataManager.Instance?.selectedPlayerData;
            if (playerData == null) return null;
            
            return playerData.dungeonProgresses.Find(p => p.dungeonId == dungeonId);
        }
        
        /// <summary>
        /// 던전 완료 기록
        /// </summary>
        public void CompleteDungeon(string dungeonId, int clearTime)
        {
            var playerData = PlayerDataManager.Instance?.selectedPlayerData;
            if (playerData == null)
            {
                Debug.LogError("[StageProgressManager] PlayerDataManager 또는 selectedPlayerData가 null입니다!");
                return;
            }
            
            // 첫 클리어 체크
            bool isFirstClear = !playerData.clearedDungeons.Contains(dungeonId);
            
            if (isFirstClear)
            {
                playerData.clearedDungeons.Add(dungeonId);
                
                if (enableDebugLogs)
                    Debug.Log($"🎉 [StageProgressManager] 🏰 던전 첫 클리어: {dungeonId}");
            }
            
            // DungeonProgress 업데이트
            var progress = playerData.dungeonProgresses.Find(p => p.dungeonId == dungeonId);
            
            if (progress == null)
            {
                // 새로운 진행도 생성
                progress = new DungeonProgress(dungeonId);
                playerData.dungeonProgresses.Add(progress);
            }
            
            // 클리어 기록
            progress.RecordClear(clearTime, isFirstClear); // ⭐ isFirstClear 전달
            
            // 저장
            PlayerDataManager.Instance.MarkDirty();
            PlayerDataManager.Instance.SaveOnMeaningfulEvent("DungeonCompleted");
            
            // 이벤트 발행 (기존 OnStageCompleted 재사용)
            OnStageCompleted?.Invoke(dungeonId, isFirstClear);
            
            if (enableDebugLogs)
            {
                Debug.Log($"✅ [StageProgressManager] 🏰 던전 완료 기록: {dungeonId}");
                Debug.Log($"   - 클리어 횟수: {progress.clearCount}");
                Debug.Log($"   - 최단 시간: {progress.bestClearTime}초");
                Debug.Log($"   - 첫 클리어: {isFirstClear}");
                Debug.Log($"   - 첫 클리어 보상 지급: {progress.isFirstClearRewarded}");
            }
        }
    }
}
