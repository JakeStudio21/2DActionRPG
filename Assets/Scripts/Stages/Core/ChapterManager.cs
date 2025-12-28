using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace StageSystem
{
    /// <summary>
    /// 챕터 시스템 관리자
    /// Phase 2: 챕터 데이터 로드 및 관리
    /// </summary>
    public class ChapterManager : Singleton<ChapterManager>
    {
        [Header("📚 챕터 데이터 캐시")]
        [SerializeField] private List<ChapterData> chapterDataCache = new List<ChapterData>();
        
        [Header("🔧 설정")]
        [SerializeField] private string chapterResourcePath = "Stages/Chapters";
        [SerializeField] private bool autoLoadOnStart = true;
        [SerializeField] private bool enableDebugLogs = true;
        
        [Header("📊 런타임 정보")]
        [SerializeField] private bool isInitialized = false;
        
        // 빠른 조회를 위한 Dictionary
        private Dictionary<int, ChapterData> chapterDataDict = new Dictionary<int, ChapterData>();
        
        #region Initialization
        
        protected override void Awake()
        {
            base.Awake();
            
            if (autoLoadOnStart)
            {
                LoadAllChapterData();
            }
        }
        
        /// <summary>
        /// 모든 챕터 데이터 로드
        /// </summary>
        public void LoadAllChapterData()
        {
            if (isInitialized)
            {
                if (enableDebugLogs)
                    Debug.Log("[ChapterManager] 이미 초기화되었습니다.");
                return;
            }
            
            try
            {
                // Resources 폴더에서 ChapterData 에셋 로드
                ChapterData[] loadedChapters = Resources.LoadAll<ChapterData>(chapterResourcePath);
                
                if (loadedChapters == null || loadedChapters.Length == 0)
                {
                    Debug.LogWarning($"[ChapterManager] {chapterResourcePath}에서 ChapterData를 찾을 수 없습니다!");
                    return;
                }
                
                // 캐시 초기화
                chapterDataCache.Clear();
                chapterDataDict.Clear();
                
                // 챕터 ID 순으로 정렬
                var sortedChapters = loadedChapters.OrderBy(c => c.chapterId).ToArray();
                
                foreach (var chapter in sortedChapters)
                {
                    if (chapter == null)
                    {
                        Debug.LogWarning("[ChapterManager] null ChapterData 발견, 스킵");
                        continue;
                    }
                    
                    // 중복 체크
                    if (chapterDataDict.ContainsKey(chapter.chapterId))
                    {
                        Debug.LogWarning($"[ChapterManager] 중복된 챕터 ID: {chapter.chapterId} ({chapter.name})");
                        continue;
                    }
                    
                    chapterDataCache.Add(chapter);
                    chapterDataDict[chapter.chapterId] = chapter;
                }
                
                isInitialized = true;
                
                if (enableDebugLogs)
                    Debug.Log($"✅ [ChapterManager] 챕터 데이터 로드 완료: {chapterDataCache.Count}개");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ [ChapterManager] 챕터 데이터 로드 실패: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// 특정 챕터 데이터 가져오기
        /// </summary>
        public ChapterData GetChapterData(int chapterId)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[ChapterManager] 아직 초기화되지 않았습니다. LoadAllChapterData()를 먼저 호출하세요.");
                LoadAllChapterData();
            }
            
            if (chapterDataDict.TryGetValue(chapterId, out ChapterData data))
            {
                return data;
            }
            
            if (enableDebugLogs)
                Debug.LogWarning($"[ChapterManager] 챕터 {chapterId} 데이터를 찾을 수 없습니다.");
            
            return null;
        }
        
        /// <summary>
        /// 모든 챕터 데이터 가져오기
        /// </summary>
        public List<ChapterData> GetAllChapters()
        {
            if (!isInitialized)
            {
                LoadAllChapterData();
            }
            
            return new List<ChapterData>(chapterDataCache);
        }
        
        /// <summary>
        /// 챕터 개수 반환
        /// </summary>
        public int GetChapterCount()
        {
            if (!isInitialized)
            {
                LoadAllChapterData();
            }
            
            return chapterDataCache.Count;
        }
        
        /// <summary>
        /// 챕터가 존재하는지 확인
        /// </summary>
        public bool ChapterExists(int chapterId)
        {
            if (!isInitialized)
            {
                LoadAllChapterData();
            }
            
            return chapterDataDict.ContainsKey(chapterId);
        }
        
        #endregion
        
        #region StageProgressManager 연동
        
        /// <summary>
        /// 챕터 해금 여부 확인 (StageProgressManager 위임)
        /// </summary>
        public bool IsChapterUnlocked(int chapterId)
        {
            if (StageProgressManager.Instance == null)
            {
                Debug.LogWarning("[ChapterManager] StageProgressManager가 없습니다.");
                return chapterId == 1; // Chapter 1은 항상 해금
            }
            
            return StageProgressManager.Instance.IsChapterUnlocked(chapterId);
        }
        
        /// <summary>
        /// 챕터 클리어 여부 확인 (StageProgressManager 위임)
        /// </summary>
        public bool IsChapterCleared(int chapterId)
        {
            if (StageProgressManager.Instance == null)
            {
                Debug.LogWarning("[ChapterManager] StageProgressManager가 없습니다.");
                return false;
            }
            
            return StageProgressManager.Instance.IsChapterCleared(chapterId);
        }
        
        /// <summary>
        /// 클리어한 챕터 목록 가져오기
        /// </summary>
        public List<int> GetClearedChapters()
        {
            if (StageProgressManager.Instance == null)
            {
                Debug.LogWarning("[ChapterManager] StageProgressManager가 없습니다.");
                return new List<int>();
            }
            
            return StageProgressManager.Instance.GetClearedChapters();
        }
        
        #endregion
        
        #region Helper 메서드
        
        /// <summary>
        /// 챕터의 진행률 계산 (0.0 ~ 1.0)
        /// </summary>
        public float GetChapterProgress(int chapterId)
        {
            var chapterData = GetChapterData(chapterId);
            if (chapterData == null || StageProgressManager.Instance == null)
                return 0f;
            
            int clearedCount = 0;
            for (int i = 1; i <= chapterData.stageCount; i++)
            {
                string stageId = chapterData.GetStageId(i);
                if (StageProgressManager.Instance.IsStageCompleted(stageId))
                {
                    clearedCount++;
                }
            }
            
            return (float)clearedCount / chapterData.stageCount;
        }
        
        /// <summary>
        /// 챕터의 클리어한 스테이지 개수 반환
        /// </summary>
        public int GetClearedStageCount(int chapterId)
        {
            var chapterData = GetChapterData(chapterId);
            if (chapterData == null || StageProgressManager.Instance == null)
                return 0;
            
            int clearedCount = 0;
            for (int i = 1; i <= chapterData.stageCount; i++)
            {
                string stageId = chapterData.GetStageId(i);
                if (StageProgressManager.Instance.IsStageCompleted(stageId))
                {
                    clearedCount++;
                }
            }
            
            return clearedCount;
        }
        
        /// <summary>
        /// 다음 플레이 가능한 스테이지 ID 반환
        /// </summary>
        public string GetNextPlayableStage(int chapterId)
        {
            var chapterData = GetChapterData(chapterId);
            if (chapterData == null || StageProgressManager.Instance == null)
                return null;
            
            // 챕터가 해금되지 않았으면 null
            if (!IsChapterUnlocked(chapterId))
                return null;
            
            // 해금된 스테이지 중 클리어하지 않은 첫 번째 스테이지 찾기
            for (int i = 1; i <= chapterData.stageCount; i++)
            {
                string stageId = chapterData.GetStageId(i);
                
                if (StageProgressManager.Instance.IsStageUnlocked(stageId) &&
                    !StageProgressManager.Instance.IsStageCompleted(stageId))
                {
                    return stageId;
                }
            }
            
            // 모든 스테이지를 클리어했으면 마지막 스테이지 반환 (재플레이용)
            return chapterData.GetLastStageId();
        }
        
        /// <summary>
        /// 챕터 정보 디버그 출력
        /// </summary>
        public void PrintChapterInfo(int chapterId)
        {
            var chapter = GetChapterData(chapterId);
            if (chapter == null)
            {
                Debug.Log($"[ChapterManager] 챕터 {chapterId} 데이터 없음");
                return;
            }
            
            bool unlocked = IsChapterUnlocked(chapterId);
            bool cleared = IsChapterCleared(chapterId);
            float progress = GetChapterProgress(chapterId);
            int clearedStages = GetClearedStageCount(chapterId);
            
            Debug.Log($"=== Chapter {chapterId} 정보 ===\n" +
                      $"제목: {chapter.chapterTitle}\n" +
                      $"해금: {unlocked}, 클리어: {cleared}\n" +
                      $"진행률: {progress * 100:F1}% ({clearedStages}/{chapter.stageCount})\n" +
                      $"다음 플레이: {GetNextPlayableStage(chapterId)}\n" +
                      $"설명: {chapter.description}");
        }
        
        #endregion
        
        #region 초기화 상태 확인
        
        /// <summary>
        /// 초기화 완료 여부
        /// </summary>
        public bool IsInitialized => isInitialized;
        
        #endregion
    }
}

