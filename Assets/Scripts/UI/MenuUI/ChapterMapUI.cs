using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StageSystem;

/// <summary>
/// 챕터 맵 UI 컨트롤러
/// Phase 6 Step 1: 챕터 전환 및 정보 표시
/// </summary>
public class ChapterMapUI : MonoBehaviour
{
    [Header("=== 챕터 전환 버튼 ===")]
    [Tooltip("이전 챕터로 이동 (◀)")]
    public Button prevChapterButton;
    
    [Tooltip("다음 챕터로 이동 (▶)")]
    public Button nextChapterButton;
    
    [Header("=== 챕터 정보 표시 ===")]
    [Tooltip("챕터 제목 (예: Chapter 1: 초원의 시작)")]
    public TMP_Text chapterTitleText;
    
    [Tooltip("챕터 설명")]
    public TMP_Text chapterDescriptionText;
    
    [Tooltip("챕터 진행도 (예: 3/10 클리어)")]
    public TMP_Text chapterProgressText;
    
    [Header("=== 챕터 아이콘/배경 (선택사항) ===")]
    [Tooltip("챕터 대표 아이콘 표시용 Image")]
    public Image chapterIconImage;
    
    [Header("=== 설정 ===")]
    [SerializeField] private int maxChapterId = 5;
    [SerializeField] private bool enableDebugLogs = true;
    
    // 내부 상태
    private int currentChapterId = 1;
    
    // 이벤트
    public System.Action<int> OnChapterChanged;
    
    private void Start()
    {
        RegisterButtonEvents();
    }
    
    /// <summary>
    /// 버튼 이벤트 등록
    /// </summary>
    private void RegisterButtonEvents()
    {
        if (prevChapterButton != null)
        {
            prevChapterButton.onClick.AddListener(OnPrevChapterButton);
        }
        else
        {
            Debug.LogWarning("[ChapterMapUI] prevChapterButton이 null입니다! Inspector에서 할당해주세요.");
        }
        
        if (nextChapterButton != null)
        {
            nextChapterButton.onClick.AddListener(OnNextChapterButton);
        }
        else
        {
            Debug.LogWarning("[ChapterMapUI] nextChapterButton이 null입니다! Inspector에서 할당해주세요.");
        }
        
        if (enableDebugLogs)
            Debug.Log("[ChapterMapUI] 버튼 이벤트 등록 완료");
    }
    
    /// <summary>
    /// 특정 챕터 표시
    /// </summary>
    public void ShowChapter(int chapterId)
    {
        if (chapterId < 1 || chapterId > maxChapterId)
        {
            Debug.LogWarning($"[ChapterMapUI] 잘못된 챕터 ID: {chapterId} (범위: 1~{maxChapterId})");
            return;
        }
        
        currentChapterId = chapterId;
        
        if (enableDebugLogs)
            Debug.Log($"[ChapterMapUI] 챕터 {chapterId} 표시");
        
        RefreshChapterUI();
    }
    
    /// <summary>
    /// 이전 챕터 버튼 클릭
    /// </summary>
    public void OnPrevChapterButton()
    {
        int newChapterId = currentChapterId - 1;
        
        if (newChapterId < 1)
        {
            if (enableDebugLogs)
                Debug.Log("[ChapterMapUI] 이미 첫 번째 챕터입니다.");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[ChapterMapUI] 이전 챕터로 이동: {currentChapterId} → {newChapterId}");
        
        currentChapterId = newChapterId;
        RefreshChapterUI();
        
        // ========================================
        // 📌 Phase 6: 챕터 전환 시 currentChapterId 업데이트
        // ========================================
        UpdateCurrentChapterId(currentChapterId);
        
        // 이벤트 발생
        OnChapterChanged?.Invoke(currentChapterId);
    }
    
    /// <summary>
    /// 다음 챕터 버튼 클릭
    /// </summary>
    public void OnNextChapterButton()
    {
        int newChapterId = currentChapterId + 1;
        
        if (newChapterId > maxChapterId)
        {
            if (enableDebugLogs)
                Debug.Log("[ChapterMapUI] 이미 마지막 챕터입니다.");
            return;
        }
        
        // 해금 체크
        if (!IsChapterUnlocked(newChapterId))
        {
            if (enableDebugLogs)
                Debug.Log($"[ChapterMapUI] 챕터 {newChapterId}는 아직 잠겨있습니다.");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[ChapterMapUI] 다음 챕터로 이동: {currentChapterId} → {newChapterId}");
        
        currentChapterId = newChapterId;
        RefreshChapterUI();
        
        // ========================================
        // 📌 Phase 6: 챕터 전환 시 currentChapterId 업데이트
        // ========================================
        UpdateCurrentChapterId(currentChapterId);
        
        // 이벤트 발생
        OnChapterChanged?.Invoke(currentChapterId);
    }
    
    /// <summary>
    /// 챕터 UI 갱신
    /// </summary>
    private void RefreshChapterUI()
    {
        if (enableDebugLogs)
            Debug.Log($"[ChapterMapUI] RefreshChapterUI 호출됨 (Chapter {currentChapterId})");
        
        // ChapterData 로드
        ChapterData chapterData = null;
        if (ChapterManager.Instance != null)
        {
            chapterData = ChapterManager.Instance.GetChapterData(currentChapterId);
        }
        
        if (chapterData == null)
        {
            Debug.LogWarning($"[ChapterMapUI] ChapterData를 찾을 수 없습니다: Chapter {currentChapterId}");
            SetDefaultChapterInfo();
            return;
        }
        
        // 챕터 제목
        if (chapterTitleText != null)
        {
            chapterTitleText.text = chapterData.chapterTitle;
        }
        
        // 챕터 설명
        if (chapterDescriptionText != null)
        {
            chapterDescriptionText.text = chapterData.description;
        }
        
        // 챕터 진행도
        if (chapterProgressText != null)
        {
            UpdateChapterProgress(chapterData);
        }
        
        // 챕터 아이콘
        if (chapterIconImage != null && chapterData.chapterIcon != null)
        {
            chapterIconImage.sprite = chapterData.chapterIcon;
            chapterIconImage.gameObject.SetActive(true);
        }
        else if (chapterIconImage != null)
        {
            chapterIconImage.gameObject.SetActive(false);
        }
        
        // 버튼 상태 업데이트
        UpdateButtonStates();
        
        if (enableDebugLogs)
            Debug.Log($"[ChapterMapUI] 챕터 UI 갱신 완료: {chapterData.chapterTitle}");
    }
    
    /// <summary>
    /// 챕터 진행도 업데이트
    /// </summary>
    private void UpdateChapterProgress(ChapterData chapterData)
    {
        if (ChapterManager.Instance == null || StageProgressManager.Instance == null)
        {
            chapterProgressText.text = "0/10 클리어";
            return;
        }
        
        int clearedCount = ChapterManager.Instance.GetClearedStageCount(currentChapterId);
        int totalCount = chapterData.stageCount;
        
        chapterProgressText.text = $"{clearedCount}/{totalCount} 클리어";
        
        // 챕터 클리어 여부 표시 (선택사항)
        bool isChapterCleared = ChapterManager.Instance.IsChapterCleared(currentChapterId);
        if (isChapterCleared)
        {
            chapterProgressText.text += " ✅";
        }
    }
    
    /// <summary>
    /// 버튼 상태 업데이트
    /// </summary>
    private void UpdateButtonStates()
    {
        // 이전 챕터 버튼
        if (prevChapterButton != null)
        {
            prevChapterButton.interactable = (currentChapterId > 1);
        }
        
        // 다음 챕터 버튼
        if (nextChapterButton != null)
        {
            bool hasNextChapter = (currentChapterId < maxChapterId);
            bool isNextChapterUnlocked = IsChapterUnlocked(currentChapterId + 1);
            
            nextChapterButton.interactable = (hasNextChapter && isNextChapterUnlocked);
        }
    }
    
    /// <summary>
    /// 챕터 해금 여부 확인
    /// </summary>
    private bool IsChapterUnlocked(int chapterId)
    {
        if (chapterId < 1 || chapterId > maxChapterId)
            return false;
        
        // Chapter 1은 항상 해금
        if (chapterId == 1)
            return true;
        
        // ChapterManager 또는 StageProgressManager에 위임
        if (ChapterManager.Instance != null)
        {
            return ChapterManager.Instance.IsChapterUnlocked(chapterId);
        }
        
        if (StageProgressManager.Instance != null)
        {
            return StageProgressManager.Instance.IsChapterUnlocked(chapterId);
        }
        
        return false;
    }
    
    /// <summary>
    /// 기본 챕터 정보 설정 (ChapterData가 없을 때)
    /// </summary>
    private void SetDefaultChapterInfo()
    {
        if (chapterTitleText != null)
        {
            chapterTitleText.text = $"Chapter {currentChapterId}";
        }
        
        if (chapterDescriptionText != null)
        {
            chapterDescriptionText.text = "챕터 정보를 불러올 수 없습니다.";
        }
        
        if (chapterProgressText != null)
        {
            chapterProgressText.text = "0/10 클리어";
        }
    }
    
    /// <summary>
    /// 현재 챕터 ID 가져오기
    /// </summary>
    public int GetCurrentChapterId()
    {
        return currentChapterId;
    }
    
    /// <summary>
    /// 📌 Phase 6: 챕터 전환 시 SelectedPlayerData 업데이트
    /// </summary>
    private void UpdateCurrentChapterId(int chapterId)
    {
        if (PlayerDataManager.Instance != null && 
            PlayerDataManager.Instance.selectedPlayerData != null)
        {
            PlayerDataManager.Instance.selectedPlayerData.currentChapterId = chapterId;
            
            if (enableDebugLogs)
                Debug.Log($"📍 [ChapterMapUI] 현재 챕터 ID 업데이트: {chapterId}");
        }
    }
    
    /// <summary>
    /// 강제 UI 갱신 (외부 호출용)
    /// </summary>
    public void ForceRefresh()
    {
        RefreshChapterUI();
    }
    
    #region Unity Editor Helper
    
    private void OnValidate()
    {
#if UNITY_EDITOR
        // maxChapterId 범위 검증
        if (maxChapterId < 1)
        {
            maxChapterId = 1;
        }
        else if (maxChapterId > 10)
        {
            Debug.LogWarning("[ChapterMapUI] maxChapterId는 10 이하를 권장합니다.");
        }
#endif
    }
    
    #endregion
}

