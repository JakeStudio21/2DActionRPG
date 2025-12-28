using UnityEngine;
using System.Collections.Generic;

namespace CutsceneSystem
{
    /// <summary>
    /// 컷신 시청 여부 추적 유틸리티
    /// Phase 3: PlayerSlotData와 연동하여 컷신 시청 기록 관리
    /// </summary>
    public static class CutsceneProgressTracker
    {
        /// <summary>
        /// 컷신을 본 적이 있는지 확인
        /// </summary>
        /// <param name="cutsceneId">컷신 ID (예: PROLOGUE, CH01_START)</param>
        /// <param name="category">카테고리 (CHAPTER_START, CHAPTER_CLEAR, STAGE_ENTER, STAGE_CLEAR)</param>
        /// <returns>시청 여부</returns>
        public static bool HasSeen(string cutsceneId, string category = "")
        {
            if (string.IsNullOrEmpty(cutsceneId))
            {
                Debug.LogWarning("[CutsceneProgressTracker] cutsceneId가 비어있습니다.");
                return false;
            }
            
            // PlayerDataManager 체크
            if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
            {
                Debug.LogWarning("[CutsceneProgressTracker] PlayerDataManager 또는 슬롯이 선택되지 않음");
                return false;
            }
            
            var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
            if (slotData == null)
            {
                Debug.LogWarning("[CutsceneProgressTracker] PlayerSlotData를 가져올 수 없음");
                return false;
            }
            
            // 카테고리가 비어있으면 자동 감지
            if (string.IsNullOrEmpty(category))
            {
                category = DetectCategory(cutsceneId);
            }
            
            return slotData.HasSeenCutscene(cutsceneId, category);
        }
        
        /// <summary>
        /// 컷신 시청 여부 저장
        /// </summary>
        /// <param name="cutsceneId">컷신 ID</param>
        /// <param name="category">카테고리</param>
        public static void MarkAsSeen(string cutsceneId, string category = "")
        {
            if (string.IsNullOrEmpty(cutsceneId))
            {
                Debug.LogWarning("[CutsceneProgressTracker] cutsceneId가 비어있습니다.");
                return;
            }
            
            // PlayerDataManager 체크
            if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
            {
                Debug.LogWarning("[CutsceneProgressTracker] PlayerDataManager 또는 슬롯이 선택되지 않음");
                return;
            }
            
            var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
            if (slotData == null)
            {
                Debug.LogWarning("[CutsceneProgressTracker] PlayerSlotData를 가져올 수 없음");
                return;
            }
            
            // 카테고리가 비어있으면 자동 감지
            if (string.IsNullOrEmpty(category))
            {
                category = DetectCategory(cutsceneId);
            }
            
            // 이미 시청한 컷신이면 스킵
            if (slotData.HasSeenCutscene(cutsceneId, category))
            {
                return;
            }
            
            // 시청 기록 저장
            slotData.MarkCutsceneAsSeen(cutsceneId, category);
            
            // PlayerDataManager에 저장
            PlayerDataManager.Instance.SaveCurrentSlot();
            
            Debug.Log($"✅ [CutsceneProgressTracker] 컷신 시청 기록: {cutsceneId} (카테고리: {category})");
        }
        
        /// <summary>
        /// PlayerSlotData에 저장 (명시적 호출용)
        /// </summary>
        public static void SaveToPlayerData()
        {
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
            {
                PlayerDataManager.Instance.SaveCurrentSlot();
            }
        }
        
        /// <summary>
        /// PlayerSlotData에서 로드 (명시적 호출용, 실제로는 자동으로 로드됨)
        /// </summary>
        public static void LoadFromPlayerData()
        {
            // PlayerSlotData는 이미 로드되어 있으므로 별도 로드 불필요
            // 이 메서드는 호환성을 위해 제공
        }
        
        /// <summary>
        /// 컷신 ID로 카테고리 자동 감지
        /// </summary>
        /// <param name="cutsceneId">컷신 ID</param>
        /// <returns>감지된 카테고리</returns>
        private static string DetectCategory(string cutsceneId)
        {
            if (string.IsNullOrEmpty(cutsceneId))
                return "";
            
            string upperCutsceneId = cutsceneId.ToUpper();
            
            // 챕터 시작 컷신 (예: CH01_START, CH02_START)
            if (upperCutsceneId.Contains("_START"))
            {
                return "CHAPTER_START";
            }
            
            // 챕터 클리어 컷신 (예: CH01_CLEAR, CH02_CLEAR)
            if (upperCutsceneId.Contains("_CLEAR"))
            {
                return "CHAPTER_CLEAR";
            }
            
            // 스테이지 입장 컷신 (예: CH01_ST01_ENTER)
            if (upperCutsceneId.Contains("_ENTER"))
            {
                return "STAGE_ENTER";
            }
            
            // 스테이지 클리어 컷신 (예: CH01_ST10_CLEAR)
            if (upperCutsceneId.Contains("_ST") && upperCutsceneId.Contains("_CLEAR"))
            {
                return "STAGE_CLEAR";
            }
            
            // 프롤로그/특수 컷신
            if (upperCutsceneId == "PROLOGUE" || upperCutsceneId.Contains("INTRO"))
            {
                return "CHAPTER_START"; // 프롤로그는 챕터 시작으로 분류
            }
            
            // 기본값: 챕터 시작으로 분류
            return "CHAPTER_START";
        }
        
        /// <summary>
        /// 특정 카테고리의 모든 시청 기록 가져오기
        /// </summary>
        public static List<string> GetSeenCutscenes(string category)
        {
            if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
            {
                return new List<string>();
            }
            
            var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
            if (slotData == null)
            {
                return new List<string>();
            }
            
            switch (category.ToUpper())
            {
                case "CHAPTER_START":
                    return new List<string>(slotData.seenChapterStart);
                
                case "CHAPTER_CLEAR":
                    return new List<string>(slotData.seenChapterClear);
                
                case "STAGE_ENTER":
                    return new List<string>(slotData.seenStageEnter);
                
                case "STAGE_CLEAR":
                    return new List<string>(slotData.seenStageClear);
                
                default:
                    Debug.LogWarning($"[CutsceneProgressTracker] 알 수 없는 카테고리: {category}");
                    return new List<string>();
            }
        }
        
        /// <summary>
        /// 모든 컷신 시청 기록 초기화 (디버그/테스트용)
        /// </summary>
        public static void ClearAllProgress()
        {
            if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
            {
                Debug.LogWarning("[CutsceneProgressTracker] PlayerDataManager 또는 슬롯이 선택되지 않음");
                return;
            }
            
            var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
            if (slotData == null)
            {
                Debug.LogWarning("[CutsceneProgressTracker] PlayerSlotData를 가져올 수 없음");
                return;
            }
            
            slotData.seenChapterStart.Clear();
            slotData.seenChapterClear.Clear();
            slotData.seenStageEnter.Clear();
            slotData.seenStageClear.Clear();
            
            PlayerDataManager.Instance.SaveCurrentSlot();
            
            Debug.Log("🗑️ [CutsceneProgressTracker] 모든 컷신 시청 기록 초기화 완료");
        }
    }
}

