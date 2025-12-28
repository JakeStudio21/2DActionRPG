using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 챕터 데이터 ScriptableObject
    /// Phase 2: 챕터 시스템 기반
    /// </summary>
    [CreateAssetMenu(fileName = "ChapterData", menuName = "Stage/Chapter Data", order = 2)]
    public class ChapterData : ScriptableObject
    {
        [Header("📖 챕터 기본 정보")]
        [Tooltip("챕터 번호 (1~5)")]
        public int chapterId = 1;
        
        [Tooltip("챕터 제목 (예: Chapter 1: 초원의 시작)")]
        public string chapterTitle = "Chapter 1";
        
        [Tooltip("챕터 내 스테이지 개수 (기본: 10)")]
        public int stageCount = 10;
        
        [Header("🎬 컷신 설정")]
        [Tooltip("챕터 시작 컷신 ID (예: CH01_START)")]
        public string chapterStartCutsceneId = "";
        
        [Tooltip("챕터 클리어 컷신 ID (예: CH01_CLEAR)")]
        public string chapterClearCutsceneId = "";
        
        [Header("🔓 해금 조건")]
        [Tooltip("해금 조건 설명 (예: '기본 해금' 또는 '챕터 1 클리어')")]
        public string unlockCondition = "기본 해금";
        
        [Header("🎨 UI 리소스")]
        [Tooltip("챕터 대표 아이콘")]
        public Sprite chapterIcon;
        
        [Tooltip("챕터 배경 이미지 (선택사항)")]
        public Sprite chapterBackground;
        
        [Header("🎵 사운드")]
        [Tooltip("챕터 BGM 키 (AudioManager 키)")]
        public string bgmKey = "";
        
        [Header("📝 설명")]
        [TextArea(3, 5)]
        [Tooltip("챕터 스토리 설명")]
        public string description = "챕터 설명을 입력하세요.";
        
        [Header("🎯 난이도 정보 (선택사항)")]
        [Tooltip("권장 레벨")]
        public int recommendedLevel = 1;
        
        [Tooltip("난이도 표시 (1~5)")]
        [Range(1, 5)]
        public int difficultyLevel = 1;
        
        #region Helper 메서드
        
        /// <summary>
        /// 챕터의 첫 번째 스테이지 ID
        /// </summary>
        public string GetFirstStageId()
        {
            return $"CH{chapterId:D2}_ST01";
        }
        
        /// <summary>
        /// 챕터의 마지막 스테이지 ID
        /// </summary>
        public string GetLastStageId()
        {
            return $"CH{chapterId:D2}_ST{stageCount:D2}";
        }
        
        /// <summary>
        /// 특정 스테이지 번호의 ID 반환
        /// </summary>
        public string GetStageId(int stageIndex)
        {
            if (stageIndex < 1 || stageIndex > stageCount)
            {
                Debug.LogWarning($"[ChapterData] 잘못된 스테이지 인덱스: {stageIndex} (범위: 1~{stageCount})");
                return null;
            }
            
            return $"CH{chapterId:D2}_ST{stageIndex:D2}";
        }
        
        /// <summary>
        /// 챕터 정보를 문자열로 반환 (디버깅용)
        /// </summary>
        public override string ToString()
        {
            return $"[Chapter {chapterId}] {chapterTitle} ({stageCount} stages)";
        }
        
        #endregion
        
        #region Validation
        
        private void OnValidate()
        {
#if UNITY_EDITOR
            // 챕터 ID 범위 검증
            if (chapterId < 1 || chapterId > 5)
            {
                Debug.LogWarning($"[ChapterData] 챕터 ID는 1~5 범위여야 합니다. 현재: {chapterId}");
            }
            
            // 스테이지 개수 검증
            if (stageCount < 1 || stageCount > 20)
            {
                Debug.LogWarning($"[ChapterData] 스테이지 개수는 1~20 범위를 권장합니다. 현재: {stageCount}");
            }
            
            // 에셋 이름 자동 업데이트 (선택사항)
            if (!string.IsNullOrEmpty(name) && !name.StartsWith("CH"))
            {
                // 에셋 이름을 CH01_Data 형식으로 변경 권장
                Debug.Log($"[ChapterData] 에셋 이름을 'CH{chapterId:D2}_Data'로 변경하는 것을 권장합니다.");
            }
#endif
        }
        
        #endregion
    }
}

