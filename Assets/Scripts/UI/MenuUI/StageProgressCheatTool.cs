using UnityEngine;
using StageSystem;

/// <summary>
/// 스테이지 진행도 치트 도구
/// Phase 6 테스트용: 챕터/스테이지 진행도를 빠르게 조작
/// </summary>
public class StageProgressCheatTool : MonoBehaviour
{
    [Header("=== 치트 키 설정 ===")]
    [Tooltip("챕터 1 클리어 (F1)")]
    public KeyCode clearChapter1Key = KeyCode.F1;
    
    [Tooltip("챕터 2 클리어 (F2)")]
    public KeyCode clearChapter2Key = KeyCode.F2;
    
    [Tooltip("챕터 3 클리어 (F3)")]
    public KeyCode clearChapter3Key = KeyCode.F3;
    
    [Tooltip("챕터 4 클리어 (F4)")]
    public KeyCode clearChapter4Key = KeyCode.F4;
    
    [Tooltip("진행도 초기화 (F5)")]
    public KeyCode resetProgressKey = KeyCode.F5;
    
    [Tooltip("현재 챕터의 다음 스테이지 해금 (F6)")]
    public KeyCode unlockNextStageKey = KeyCode.F6;
    
    [Tooltip("현재 챕터의 모든 스테이지 해금 (F7)")]
    public KeyCode unlockAllStagesKey = KeyCode.F7;
    
    [Header("=== 설정 ===")]
    [Tooltip("치트 기능 활성화")]
    public bool enableCheats = true;
    
    [Tooltip("치트 사용 시 로그 출력")]
    public bool showLogs = true;
    
    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (!enableCheats) return;

        if (Input.GetKeyDown(clearChapter1Key))   ClearChapter(1);
        if (Input.GetKeyDown(clearChapter2Key))   ClearChapter(2);
        if (Input.GetKeyDown(clearChapter3Key))   ClearChapter(3);
        if (Input.GetKeyDown(clearChapter4Key))   ClearChapter(4);
        if (Input.GetKeyDown(resetProgressKey))   ResetAllProgress();
        if (Input.GetKeyDown(unlockNextStageKey)) UnlockNextStage();
        if (Input.GetKeyDown(unlockAllStagesKey)) UnlockAllStagesInCurrentChapter();
#endif
    }
    
    /// <summary>
    /// 특정 챕터 전체 클리어
    /// </summary>
    private void ClearChapter(int chapterId)
    {
        if (StageProgressManager.Instance == null)
        {
            Debug.LogWarning("[StageProgressCheatTool] StageProgressManager가 없습니다.");
            return;
        }
        
        // ChapterData 가져오기
        var chapterData = ChapterManager.Instance?.GetChapterData(chapterId);
        if (chapterData == null)
        {
            Debug.LogError($"[StageProgressCheatTool] ChapterData를 찾을 수 없습니다: Chapter {chapterId}");
            return;
        }
        
        if (showLogs)
            Debug.Log($"[StageProgressCheatTool] 🎮 치트: Chapter {chapterId} 모든 스테이지 클리어");
        
        // 모든 스테이지 클리어
        for (int i = 1; i <= chapterData.stageCount; i++)
        {
            string stageId = chapterData.GetStageId(i);
            
            // 해금
            StageProgressManager.Instance.UnlockStage(stageId);
            
            // 클리어
            StageProgressManager.Instance.CompleteStage(stageId, completionTime: 30f + i, isFirstClear: true);
        }
        
        // 챕터 클리어 처리
        StageProgressManager.Instance.CompleteChapter(chapterId);
        
        if (showLogs)
            Debug.Log($"[StageProgressCheatTool] ✅ Chapter {chapterId} 클리어 완료! Chapter {chapterId + 1} 해금됨");
        
        // UI 갱신
        RefreshUI();
    }
    
    /// <summary>
    /// 모든 진행도 초기화
    /// </summary>
    private void ResetAllProgress()
    {
        if (StageProgressManager.Instance == null || PlayerDataManager.Instance == null)
        {
            Debug.LogWarning("[StageProgressCheatTool] StageProgressManager 또는 PlayerDataManager가 없습니다.");
            return;
        }
        
        if (!PlayerDataManager.Instance.IsSlotSelected)
        {
            Debug.LogWarning("[StageProgressCheatTool] 슬롯이 선택되지 않았습니다.");
            return;
        }
        
        if (showLogs)
            Debug.Log("[StageProgressCheatTool] 🔄 치트: 모든 진행도 초기화");
        
        // 현재 슬롯 데이터 가져오기
        var slotData = PlayerDataManager.Instance.GetCurrentSlotData();
        if (slotData != null)
        {
            // 스테이지 진행도 초기화
            slotData.stageProgresses.Clear();
            
            // 챕터 진행도 초기화
            slotData.clearedChapters.Clear();
            
            // 컷신 시청 기록 초기화 (선택사항)
            slotData.seenChapterStart.Clear();
            slotData.seenChapterClear.Clear();
            slotData.seenStageEnter.Clear();
            slotData.seenStageClear.Clear();
            
            // 저장
            PlayerDataManager.Instance.SaveCurrentSlot();
            
            // StageProgressManager 재초기화
            int currentSlot = PlayerDataManager.Instance.GetCurrentSlotIndex();
            StageProgressManager.Instance.InitializeFor(currentSlot);
        }
        
        if (showLogs)
            Debug.Log("[StageProgressCheatTool] ✅ 진행도 초기화 완료! CH01_ST01만 해금됨");
        
        // UI 갱신
        RefreshUI();
    }
    
    /// <summary>
    /// 현재 챕터의 다음 스테이지 해금
    /// </summary>
    private void UnlockNextStage()
    {
        if (StageProgressManager.Instance == null)
        {
            Debug.LogWarning("[StageProgressCheatTool] StageProgressManager가 없습니다.");
            return;
        }
        
        // 현재 챕터 가져오기
        var chapterMapUI = FindObjectOfType<ChapterMapUI>();
        if (chapterMapUI == null)
        {
            Debug.LogWarning("[StageProgressCheatTool] ChapterMapUI를 찾을 수 없습니다.");
            return;
        }
        
        int currentChapterId = chapterMapUI.GetCurrentChapterId();
        var chapterData = ChapterManager.Instance?.GetChapterData(currentChapterId);
        
        if (chapterData == null)
        {
            Debug.LogError($"[StageProgressCheatTool] ChapterData를 찾을 수 없습니다: Chapter {currentChapterId}");
            return;
        }
        
        // 다음 해금 대상 찾기
        for (int i = 1; i <= chapterData.stageCount; i++)
        {
            string stageId = chapterData.GetStageId(i);
            
            if (!StageProgressManager.Instance.IsStageUnlocked(stageId))
            {
                StageProgressManager.Instance.UnlockStage(stageId);
                
                if (showLogs)
                    Debug.Log($"[StageProgressCheatTool] 🔓 치트: {stageId} 해금");
                
                RefreshUI();
                return;
            }
        }
        
        if (showLogs)
            Debug.Log($"[StageProgressCheatTool] ℹ️ Chapter {currentChapterId}의 모든 스테이지가 이미 해금되었습니다.");
    }
    
    /// <summary>
    /// 현재 챕터의 모든 스테이지 해금
    /// </summary>
    private void UnlockAllStagesInCurrentChapter()
    {
        if (StageProgressManager.Instance == null)
        {
            Debug.LogWarning("[StageProgressCheatTool] StageProgressManager가 없습니다.");
            return;
        }
        
        // 현재 챕터 가져오기
        var chapterMapUI = FindObjectOfType<ChapterMapUI>();
        if (chapterMapUI == null)
        {
            Debug.LogWarning("[StageProgressCheatTool] ChapterMapUI를 찾을 수 없습니다.");
            return;
        }
        
        int currentChapterId = chapterMapUI.GetCurrentChapterId();
        var chapterData = ChapterManager.Instance?.GetChapterData(currentChapterId);
        
        if (chapterData == null)
        {
            Debug.LogError($"[StageProgressCheatTool] ChapterData를 찾을 수 없습니다: Chapter {currentChapterId}");
            return;
        }
        
        if (showLogs)
            Debug.Log($"[StageProgressCheatTool] 🔓 치트: Chapter {currentChapterId} 모든 스테이지 해금");
        
        // 모든 스테이지 해금
        for (int i = 1; i <= chapterData.stageCount; i++)
        {
            string stageId = chapterData.GetStageId(i);
            StageProgressManager.Instance.UnlockStage(stageId);
        }
        
        if (showLogs)
            Debug.Log($"[StageProgressCheatTool] ✅ Chapter {currentChapterId} 모든 스테이지 해금 완료");
        
        RefreshUI();
    }
    
    /// <summary>
    /// UI 갱신
    /// </summary>
    private void RefreshUI()
    {
        // StageSelectPanelController 갱신
        var stageSelectPanel = FindObjectOfType<StageSelectPanelController>();
        if (stageSelectPanel != null)
        {
            stageSelectPanel.RefreshStageProgressUI();
        }
        
        // ChapterMapUI 갱신
        var chapterMapUI = FindObjectOfType<ChapterMapUI>();
        if (chapterMapUI != null)
        {
            chapterMapUI.ForceRefresh();
        }
    }
    
    /// <summary>
    /// 치트 키 안내 표시 (GUI)
    /// </summary>
    private void OnGUI()
    {
        if (!enableCheats) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 14;
        style.normal.textColor = Color.yellow;
        
        string cheatInfo = "=== 🎮 스테이지 진행도 치트 ===\n" +
                          $"F1: Chapter 1 클리어\n" +
                          $"F2: Chapter 2 클리어\n" +
                          $"F3: Chapter 3 클리어\n" +
                          $"F4: Chapter 4 클리어\n" +
                          $"F5: 진행도 초기화\n" +
                          $"F6: 다음 스테이지 해금\n" +
                          $"F7: 현재 챕터 모두 해금";
        
        GUI.Box(new Rect(10, 10, 300, 180), cheatInfo, style);
    }
}

