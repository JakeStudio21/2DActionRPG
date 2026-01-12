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
            
            // ✅ SelectedPlayerData 직접 접근
            var selectedData = PlayerDataManager.Instance.selectedPlayerData;
            if (selectedData == null)
            {
                Debug.LogWarning("[CutsceneProgressTracker] SelectedPlayerData를 가져올 수 없음");
                return false;
            }
            
            // 카테고리가 비어있으면 자동 감지
            if (string.IsNullOrEmpty(category))
            {
                category = DetectCategory(cutsceneId);
            }
            
            // 🔧 SelectedPlayerData의 컷신 리스트에서 직접 확인
            switch (category.ToUpper())
            {
                case "CHAPTER_START":
                    return selectedData.seenChapterStart.Contains(cutsceneId);
                
                case "CHAPTER_CLEAR":
                    return selectedData.seenChapterClear.Contains(cutsceneId);
                
                case "STAGE_ENTER":
                    return selectedData.seenStageEnter.Contains(cutsceneId);
                
                case "STAGE_CLEAR":
                    return selectedData.seenStageClear.Contains(cutsceneId);
                
                default:
                    Debug.LogWarning($"[CutsceneProgressTracker] 알 수 없는 카테고리: {category}");
                    return false;
            }
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
            
            // ✅ SelectedPlayerData 직접 접근 (저장 소스)
            var selectedData = PlayerDataManager.Instance.selectedPlayerData;
            if (selectedData == null)
            {
                Debug.LogWarning("[CutsceneProgressTracker] SelectedPlayerData를 가져올 수 없음");
                return;
            }
            
            // 카테고리가 비어있으면 자동 감지
            if (string.IsNullOrEmpty(category))
            {
                category = DetectCategory(cutsceneId);
            }
            
            // 🔧 SelectedPlayerData의 컷신 리스트에 직접 추가
            switch (category.ToUpper())
            {
                case "CHAPTER_START":
                    if (!selectedData.seenChapterStart.Contains(cutsceneId))
                    {
                        selectedData.seenChapterStart.Add(cutsceneId);
                    }
                    else
                    {
                        return; // 이미 시청함
                    }
                    break;
                
                case "CHAPTER_CLEAR":
                    if (!selectedData.seenChapterClear.Contains(cutsceneId))
                    {
                        selectedData.seenChapterClear.Add(cutsceneId);
                    }
                    else
                    {
                        return;
                    }
                    break;
                
                case "STAGE_ENTER":
                    if (!selectedData.seenStageEnter.Contains(cutsceneId))
                    {
                        selectedData.seenStageEnter.Add(cutsceneId);
                    }
                    else
                    {
                        return;
                    }
                    break;
                
                case "STAGE_CLEAR":
                    if (!selectedData.seenStageClear.Contains(cutsceneId))
                    {
                        selectedData.seenStageClear.Add(cutsceneId);
                    }
                    else
                    {
                        return;
                    }
                    break;
                
                default:
                    Debug.LogWarning($"[CutsceneProgressTracker] 알 수 없는 카테고리: {category}");
                    return;
            }
            
            // 🔧 Dirty Flag 설정 (저장 필수!)
            PlayerDataManager.Instance.MarkDirty();
            
            // PlayerDataManager에 즉시 저장
            PlayerDataManager.Instance.SaveOnMeaningfulEvent($"Cutscene_{cutsceneId}");
            
            Debug.Log($"✅ [CutsceneProgressTracker] 컷신 시청 기록 및 저장 완료: {cutsceneId} (카테고리: {category})");
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
        /// ✅ 수정: if문 순서 변경 (구체적 조건 → 일반적 조건)
        /// </summary>
        /// <param name="cutsceneId">컷신 ID</param>
        /// <returns>감지된 카테고리</returns>
        private static string DetectCategory(string cutsceneId)
        {
            if (string.IsNullOrEmpty(cutsceneId))
                return "";
            
            string upperCutsceneId = cutsceneId.ToUpper();
            
            // ⭐ 우선순위 1: 스테이지 입장 컷신 (예: CH01_ST01_START, CH01_ST01_ENTER)
            // _ST 뒤에 숫자 2개 오는 패턴 체크 (CH01_START와 구분하기 위함)
            if (System.Text.RegularExpressions.Regex.IsMatch(upperCutsceneId, @"_ST\d{2}") && 
                (upperCutsceneId.Contains("_START") || upperCutsceneId.Contains("_ENTER")))
            {
                #if UNITY_EDITOR
                Debug.Log($"[CutsceneProgressTracker] {cutsceneId} → STAGE_ENTER");
                #endif
                return "STAGE_ENTER";
            }
            
            // ⭐ 우선순위 2: 스테이지 클리어 컷신 (예: CH01_ST10_CLEAR)
            // _ST 뒤에 숫자 2개 오는 패턴 체크
            if (System.Text.RegularExpressions.Regex.IsMatch(upperCutsceneId, @"_ST\d{2}") && 
                upperCutsceneId.Contains("_CLEAR"))
            {
                #if UNITY_EDITOR
                Debug.Log($"[CutsceneProgressTracker] {cutsceneId} → STAGE_CLEAR");
                #endif
                return "STAGE_CLEAR";
            }
            
            // ⭐ 우선순위 3: 챕터 시작 컷신 (예: CH01_START, CH02_START)
            // _ST가 없고 _START만 포함하면 챕터 시작
            if (upperCutsceneId.Contains("_START"))
            {
                #if UNITY_EDITOR
                Debug.Log($"[CutsceneProgressTracker] {cutsceneId} → CHAPTER_START");
                #endif
                return "CHAPTER_START";
            }
            
            // ⭐ 우선순위 4: 챕터 클리어 컷신 (예: CH01_CLEAR, CH02_CLEAR)
            // _ST가 없고 _CLEAR만 포함하면 챕터 클리어
            if (upperCutsceneId.Contains("_CLEAR"))
            {
                #if UNITY_EDITOR
                Debug.Log($"[CutsceneProgressTracker] {cutsceneId} → CHAPTER_CLEAR");
                #endif
                return "CHAPTER_CLEAR";
            }
            
            // 프롤로그/특수 컷신
            if (upperCutsceneId == "PROLOGUE" || upperCutsceneId.Contains("INTRO"))
            {
                #if UNITY_EDITOR
                Debug.Log($"[CutsceneProgressTracker] {cutsceneId} → CHAPTER_START (프롤로그)");
                #endif
                return "CHAPTER_START"; // 프롤로그는 챕터 시작으로 분류
            }
            
            // 기본값: 챕터 시작으로 분류
            #if UNITY_EDITOR
            Debug.Log($"[CutsceneProgressTracker] {cutsceneId} → CHAPTER_START (기본값)");
            #endif
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
            
            // ✅ SelectedPlayerData 직접 접근
            var selectedData = PlayerDataManager.Instance.selectedPlayerData;
            if (selectedData == null)
            {
                return new List<string>();
            }
            
            switch (category.ToUpper())
            {
                case "CHAPTER_START":
                    return new List<string>(selectedData.seenChapterStart);
                
                case "CHAPTER_CLEAR":
                    return new List<string>(selectedData.seenChapterClear);
                
                case "STAGE_ENTER":
                    return new List<string>(selectedData.seenStageEnter);
                
                case "STAGE_CLEAR":
                    return new List<string>(selectedData.seenStageClear);
                
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
            
            // ✅ SelectedPlayerData 직접 접근
            var selectedData = PlayerDataManager.Instance.selectedPlayerData;
            if (selectedData == null)
            {
                Debug.LogWarning("[CutsceneProgressTracker] SelectedPlayerData를 가져올 수 없음");
                return;
            }
            
            selectedData.seenChapterStart.Clear();
            selectedData.seenChapterClear.Clear();
            selectedData.seenStageEnter.Clear();
            selectedData.seenStageClear.Clear();
            
            PlayerDataManager.Instance.MarkDirty();
            PlayerDataManager.Instance.SaveCurrentSlot();
            
            Debug.Log("🗑️ [CutsceneProgressTracker] 모든 컷신 시청 기록 초기화 완료");
        }
    }
}

