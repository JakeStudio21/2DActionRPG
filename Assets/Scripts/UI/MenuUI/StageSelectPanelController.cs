using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StageSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 스테이지 선택 패널 전용 컨트롤러
/// LobbyUIController에서 분리 - 단일 책임 원칙
/// Phase 6-Pre: 기존 3개 스테이지 관리
/// Phase 6: 50개 스테이지 + 챕터 시스템 확장 준비
/// </summary>
public class StageSelectPanelController : MonoBehaviour
{
    [Header("=== 패널 참조 ===")]
    public GameObject stageSelectPanel;
    
    [Header("=== 스테이지 선택 UI ===")]
    // ❌ Phase 6: stageSelectTitleText 제거 (ChapterMapUI의 Text_ChapterTitle이 대체)
    public Button playButton;
    public Button backButton;
    
    [Header("=== 📖 Phase 6: 챕터 시스템 ===")]
    [Tooltip("챕터 맵 UI (챕터 전환 및 정보 표시)")]
    public ChapterMapUI chapterMapUI;
    
    [Header("=== 🎯 Phase 6: 동적 스테이지 버튼 ===")]
    [Tooltip("스테이지 버튼 생성될 부모 Transform (Grid Layout)")]
    public Transform stageButtonContainer;
    
    [Tooltip("스테이지 버튼 프리팹 (StageButtonUI 컴포넌트 포함)")]
    public GameObject stageButtonPrefab;
    
    // 동적 생성된 버튼 목록
    private List<StageButtonUI> activeStageButtons = new List<StageButtonUI>();
    
    [Header("=== 스테이지 정보 표시 ===")]
    public TMP_Text stageNameText;
    public TMP_Text stageDescriptionText;
    public TMP_Text bestTimeText;
    public TMP_Text rewardPreviewText;
    
    [Header("=== 색상 참조 ===")]
    public Button colorReferenceButton; // 색상 참조용 버튼
    
    // 내부 상태
    private int currentChapterId = 1; // 현재 표시 중인 챕터
    private string selectedStageId = ""; // 선택된 스테이지 ID
    private string selectedSceneName = "";
    private int selectedSlotIndex = -1; // 현재 선택된 캐릭터 슬롯
    
    // 이벤트
    public System.Action OnBackButtonClicked;
    public System.Action<string> OnPlayButtonClicked; // sceneName 전달
    
    private void Awake()
    {
        // Phase 6: 더 이상 고정 배열 초기화 불필요
    }
    
    private void OnEnable()
    {
        // StageProgressManager 이벤트 구독
        if (StageProgressManager.Instance != null)
        {
            StageProgressManager.Instance.OnStageUnlocked += OnStageUnlocked;
            StageProgressManager.Instance.OnStageCompleted += OnStageCompleted;
            Debug.Log("[StageSelectPanelController] StageProgressManager 이벤트 구독 완료");
        }
        
        // ChapterMapUI 이벤트 구독
        if (chapterMapUI != null)
        {
            chapterMapUI.OnChapterChanged += OnChapterChanged;
            Debug.Log("[StageSelectPanelController] ChapterMapUI 이벤트 구독 완료");
        }
    }
    
    private void OnDisable()
    {
        // StageProgressManager 이벤트 구독 해제
        if (StageProgressManager.Instance != null)
        {
            StageProgressManager.Instance.OnStageUnlocked -= OnStageUnlocked;
            StageProgressManager.Instance.OnStageCompleted -= OnStageCompleted;
            Debug.Log("[StageSelectPanelController] StageProgressManager 이벤트 구독 해제");
        }
        
        // ChapterMapUI 이벤트 구독 해제
        if (chapterMapUI != null)
        {
            chapterMapUI.OnChapterChanged -= OnChapterChanged;
            Debug.Log("[StageSelectPanelController] ChapterMapUI 이벤트 구독 해제");
        }
    }
    
    private void Start()
    {
        RegisterButtonEvents();
        
        // Phase 6: 챕터 시스템 초기화
        if (chapterMapUI != null)
        {
            chapterMapUI.ShowChapter(currentChapterId);
        }
    }
    
    // ========================================
    // ✅ Phase 6: 레거시 메서드 제거됨
    // InitializeStageArrays() → 더 이상 사용 안 함
    // ========================================
    
    /// <summary>
    /// 버튼 이벤트 등록
    /// </summary>
    private void RegisterButtonEvents()
    {
        // Play/Back 버튼 이벤트 등록
        if (playButton != null) playButton.onClick.AddListener(OnPlayButton);
        if (backButton != null) backButton.onClick.AddListener(OnBack);
        
        Debug.Log($"[StageSelectPanelController] 버튼 이벤트 등록 완료");
    }
    
    /// <summary>
    /// 패널 표시
    /// </summary>
    public void ShowPanel()
    {
        if (stageSelectPanel != null)
        {
            Debug.Log("[StageSelectPanelController] 패널 표시 시작");
            stageSelectPanel.SetActive(true);
            Debug.Log($"[StageSelectPanelController] 패널 활성화 완료: {stageSelectPanel.name}, Active={stageSelectPanel.activeSelf}");
        }
        else
        {
            Debug.LogError("[StageSelectPanelController] stageSelectPanel이 null입니다! Inspector에서 할당해주세요.");
        }
        
        // ========================================
        // 📌 Phase 6: 마지막 플레이 챕터 로드
        // ========================================
        int targetChapterId = GetLastPlayedChapterId();
        
        // 🎬 챕터1 시작 컷신 체크 (최초 진입 시)
        if (targetChapterId == 1)
        {
            CheckAndPlayChapterStartCutscene(targetChapterId);
        }
        
        // Phase 6: 챕터 맵 UI 표시
        if (chapterMapUI != null)
        {
            chapterMapUI.ShowChapter(targetChapterId);
        }
        
        // Phase 6: 동적 스테이지 버튼 생성
        CreateStageButtons(targetChapterId);
        
        // 선택 초기화
        selectedStageId = "";
        selectedSceneName = "";
        UpdatePlayButtonState();
    }
    
    /// <summary>
    /// 패널 숨김
    /// </summary>
    public void HidePanel()
    {
        if (stageSelectPanel != null)
        {
            stageSelectPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 현재 선택된 캐릭터 슬롯 인덱스 설정
    /// </summary>
    public void SetSelectedSlotIndex(int slotIndex)
    {
        selectedSlotIndex = slotIndex;
        Debug.Log($"[StageSelectPanelController] 선택된 슬롯 인덱스: {slotIndex}");
    }
    
    // ========================================
    // ✅ Phase 6: 동적 스테이지 버튼 생성 시스템
    // ========================================
    
    /// <summary>
    /// 스테이지 버튼 동적 생성
    /// </summary>
    private void CreateStageButtons(int chapterId)
    {
        if (stageButtonContainer == null)
        {
            Debug.LogError("[StageSelectPanelController] stageButtonContainer가 null입니다! Inspector에서 할당해주세요.");
            return;
        }
        
        if (stageButtonPrefab == null)
        {
            Debug.LogError("[StageSelectPanelController] stageButtonPrefab이 null입니다! Inspector에서 할당해주세요.");
            return;
        }
        
        Debug.Log($"[StageSelectPanelController] 챕터 {chapterId} 스테이지 버튼 생성 시작");
        
        // 기존 버튼 제거
        DestroyStageButtons();
        
        // ChapterData 가져오기
        var chapterData = ChapterManager.Instance?.GetChapterData(chapterId);
        if (chapterData == null)
        {
            Debug.LogWarning($"[StageSelectPanelController] ChapterData를 찾을 수 없습니다: Chapter {chapterId}");
            return;
        }
        
        // 스테이지 버튼 생성 (1~10)
        for (int i = 1; i <= chapterData.stageCount; i++)
        {
            string stageId = chapterData.GetStageId(i);
            bool isUnlocked = StageProgressManager.Instance?.IsStageUnlocked(stageId) ?? false;
            bool isCleared = StageProgressManager.Instance?.IsStageCompleted(stageId) ?? false;
            bool isBoss = (i == 10); // Stage 10은 보스
            
            // 버튼 생성
            GameObject buttonObj = Instantiate(stageButtonPrefab, stageButtonContainer);
            StageButtonUI buttonUI = buttonObj.GetComponent<StageButtonUI>();
            
            if (buttonUI == null)
            {
                Debug.LogError("[StageSelectPanelController] stageButtonPrefab에 StageButtonUI 컴포넌트가 없습니다!");
                Destroy(buttonObj);
                continue;
            }
            
            // 버튼 초기화
            buttonUI.Setup(stageId, i, isUnlocked, isCleared, isBoss);
            buttonUI.OnStageSelected += OnStageButtonClickedByStageId;
            
            activeStageButtons.Add(buttonUI);
            
            Debug.Log($"[StageSelectPanelController] 버튼 생성: {stageId} (Index: {i}, Unlocked: {isUnlocked}, Cleared: {isCleared}, Boss: {isBoss})");
        }
        
        Debug.Log($"[StageSelectPanelController] 스테이지 버튼 생성 완료: {activeStageButtons.Count}개");
    }
    
    /// <summary>
    /// 스테이지 버튼 제거
    /// </summary>
    private void DestroyStageButtons()
    {
        Debug.Log($"[StageSelectPanelController] 스테이지 버튼 제거: {activeStageButtons.Count}개");
        
        foreach (var button in activeStageButtons)
        {
            if (button != null)
            {
                button.OnStageSelected -= OnStageButtonClickedByStageId;
                Destroy(button.gameObject);
            }
        }
        
        activeStageButtons.Clear();
    }
    
    /// <summary>
    /// 챕터 전환 이벤트 핸들러
    /// </summary>
    private void OnChapterChanged(int newChapterId)
    {
        Debug.Log($"[StageSelectPanelController] 챕터 전환: {currentChapterId} → {newChapterId}");
        
        currentChapterId = newChapterId;
        
        // 스테이지 버튼 재생성
        CreateStageButtons(currentChapterId);
        
        // 선택 초기화
        selectedStageId = "";
        selectedSceneName = "";
        UpdatePlayButtonState();
        
        // 스테이지 정보 패널 초기화
        ClearStageInfo();
    }
    
    /// <summary>
    /// 스테이지 정보 패널 초기화
    /// </summary>
    private void ClearStageInfo()
    {
        if (stageNameText != null)
            stageNameText.text = "스테이지를 선택하세요";
        
        if (stageDescriptionText != null)
            stageDescriptionText.text = "";
        
        if (bestTimeText != null)
            bestTimeText.text = "";
        
        if (rewardPreviewText != null)
            rewardPreviewText.text = "";
    }
    
    /// <summary>
    /// 스테이지 진행도 UI 갱신 (챕터 전체)
    /// </summary>
    public void RefreshStageProgressUI()
    {
        Debug.Log("[StageSelectPanelController] RefreshStageProgressUI 호출됨");
        
        if (StageProgressManager.Instance == null)
        {
            Debug.LogWarning("[StageSelectPanelController] StageProgressManager가 없습니다.");
            return;
        }
        
        // 초기화 확인
        if (!StageProgressManager.Instance.IsInitialized)
        {
            Debug.LogWarning("[StageSelectPanelController] StageProgressManager가 아직 초기화되지 않았습니다. 재시도 중...");
            StartCoroutine(RetryRefreshStageProgressUI());
            return;
        }
        
        // Phase 6: 동적 버튼 업데이트
        foreach (var button in activeStageButtons)
        {
            if (button == null) continue;
            
            string stageId = button.GetStageId();
            bool isUnlocked = StageProgressManager.Instance.IsStageUnlocked(stageId);
            bool isCleared = StageProgressManager.Instance.IsStageCompleted(stageId);
            bool isSelected = (stageId == selectedStageId);
            
            button.UpdateVisualState(isUnlocked, isCleared, isSelected);
        }
        
        // 챕터 맵 UI도 갱신
        if (chapterMapUI != null)
        {
            chapterMapUI.ForceRefresh();
        }
        
        Debug.Log("[StageSelectPanelController] 스테이지 진행도 UI 갱신 완료");
    }
    
    /// <summary>
    /// 재시도 코루틴
    /// </summary>
    private IEnumerator RetryRefreshStageProgressUI()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (StageProgressManager.Instance != null && StageProgressManager.Instance.IsInitialized)
        {
            RefreshStageProgressUI();
        }
    }
    
    /// <summary>
    /// 스테이지 버튼 클릭 처리 (StageID 기반)
    /// </summary>
    private void OnStageButtonClickedByStageId(string stageId)
    {
        // 해금 상태 체크
        if (StageProgressManager.Instance != null && !StageProgressManager.Instance.IsStageUnlocked(stageId))
        {
            Debug.LogWarning($"[StageSelectPanelController] 잠긴 스테이지 선택 시도: {stageId}");
            return;
        }
        
        selectedStageId = stageId;
        
        // 스테이지 정보 표시
        DisplayStageInfo(stageId);
        
        // 선택 상태 UI 업데이트
        RefreshStageSelectionUI();
        
        // Play 버튼 활성화
        UpdatePlayButtonState();
        
        Debug.Log($"[StageSelectPanelController] 스테이지 선택됨: {stageId}");
    }
    
    /// <summary>
    /// 스테이지 정보 표시
    /// </summary>
    private void DisplayStageInfo(string stageId)
    {
        Debug.Log($"[StageSelectPanelController] DisplayStageInfo 호출: {stageId}");
        
        var stageConfig = LoadStageConfig(stageId);
        var progress = StageProgressManager.Instance?.GetStageProgress(stageId);
        
        if (stageConfig == null)
        {
            Debug.LogError($"[StageSelectPanelController] StageConfig를 찾을 수 없습니다: {stageId}");
            return;
        }
        
        Debug.Log($"[StageSelectPanelController] StageConfig 로드 성공: {stageConfig.StageName}");
        
        // 스테이지 이름
        if (stageNameText != null)
        {
            stageNameText.text = stageConfig.StageName;
            Debug.Log($"[StageSelectPanelController] 스테이지 이름 설정: {stageConfig.StageName}");
        }
        else
        {
            Debug.LogWarning("[StageSelectPanelController] stageNameText가 null입니다!");
        }
        
        // 스테이지 설명
        if (stageDescriptionText != null)
        {
            stageDescriptionText.text = stageConfig.Description;
        }
        
        // 최고 기록
        if (bestTimeText != null && progress != null)
        {
            if (progress.isCompleted && progress.bestClearTime > 0)
            {
                bestTimeText.text = $"최고 기록: {progress.bestClearTime:F2}초";
            }
            else
            {
                bestTimeText.text = "미클리어";
            }
        }
        
        // 보상 미리보기
        if (rewardPreviewText != null)
        {
            if (!string.IsNullOrEmpty(stageConfig.FirstClearDropGroupId))
            {
                rewardPreviewText.text = $"보상: {stageConfig.FirstClearDropGroupId}";
            }
            else
            {
                rewardPreviewText.text = "보상: 없음";
            }
        }
        
        // 씬 이름 저장
        selectedSceneName = stageConfig.SceneName;
        Debug.Log($"[StageSelectPanelController] 씬 이름 저장: {selectedSceneName}");
    }
    
    /// <summary>
    /// StageConfig 로드
    /// </summary>
    private StageConfig LoadStageConfig(string stageId)
    {
        StageConfig config = null;
        
        // ✅ 챕터 기반 스테이지 (CH01_ST01, CH02_ST05 등)
        if (StageSystem.StageIdValidator.IsValidChapterStageId(stageId))
        {
            string path = $"Stages/Configs/Chapters/{stageId}_Config";
            config = Resources.Load<StageConfig>(path);
            
            if (config == null)
            {
                Debug.LogError($"[StageSelectPanelController] StageConfig 로드 실패: {path}");
            }
            else
            {
                Debug.Log($"[StageSelectPanelController] StageConfig 로드 성공 (챕터): {path} -> {config.StageName}");
            }
        }
        // ❌ Phase 6: 레거시 시스템 제거됨 (STAGE_001~003)
        // CH01_ST01 방식만 사용하도록 단순화
        else
        {
            Debug.LogError($"[StageSelectPanelController] 잘못된 StageID 형식: {stageId}");
            Debug.LogError($"[StageSelectPanelController] CH01_ST01 형식만 지원됩니다 (레거시 STAGE_001 제거)");
        }
        
        return config;
    }
    
    /// <summary>
    /// 선택 상태 UI 갱신
    /// </summary>
    private void RefreshStageSelectionUI()
    {
        Debug.Log($"[StageSelectPanelController] RefreshStageSelectionUI 호출 (selectedStageId={selectedStageId})");
        
        // Phase 6: 동적 버튼 선택 상태 업데이트
        foreach (var button in activeStageButtons)
        {
            if (button == null) continue;
            
            string stageId = button.GetStageId();
            bool isSelected = (stageId == selectedStageId);
            
            button.SetSelected(isSelected);
        }
    }
    
    /// <summary>
    /// Play 버튼 상태 업데이트
    /// </summary>
    private void UpdatePlayButtonState()
    {
        if (playButton != null)
        {
            bool canPlay = !string.IsNullOrEmpty(selectedStageId) && !string.IsNullOrEmpty(selectedSceneName);
            playButton.interactable = canPlay;
            Debug.Log($"[StageSelectPanelController] Play 버튼 상태 업데이트: interactable={canPlay}, stageId={selectedStageId}, sceneName={selectedSceneName}");
        }
        else
        {
            Debug.LogWarning("[StageSelectPanelController] playButton이 null입니다!");
        }
    }
    
    /// <summary>
    /// Play 버튼 클릭
    /// </summary>
    private void OnPlayButton()
    {
        Debug.Log($"[StageSelectPanelController] OnPlayButton 호출됨 (stageId={selectedStageId}, scene={selectedSceneName})");
        
        if (string.IsNullOrEmpty(selectedStageId) || string.IsNullOrEmpty(selectedSceneName))
        {
            Debug.LogWarning("[StageSelectPanelController] 스테이지가 선택되지 않았습니다!");
            return;
        }
        
        Debug.Log($"[StageSelectPanelController] 게임 시작 이벤트 발생: {selectedSceneName}");
        
        // 이벤트 발생
        OnPlayButtonClicked?.Invoke(selectedSceneName);
    }
    
    /// <summary>
    /// 뒤로 가기 버튼 클릭
    /// </summary>
    private void OnBack()
    {
        Debug.Log("[StageSelectPanelController] 뒤로 가기");
        
        // 이벤트 발생
        OnBackButtonClicked?.Invoke();
    }
    
    /// <summary>
    /// 스테이지 해금 이벤트 핸들러
    /// </summary>
    private void OnStageUnlocked(string stageId)
    {
        Debug.Log($"[StageSelectPanelController] 스테이지 해금됨: {stageId}");
        RefreshStageProgressUI();
    }
    
    /// <summary>
    /// 스테이지 완료 이벤트 핸들러
    /// </summary>
    private void OnStageCompleted(string stageId, bool isFirstClear)
    {
        Debug.Log($"[StageSelectPanelController] 스테이지 완료됨: {stageId} (첫클리어: {isFirstClear})");
        RefreshStageProgressUI();
        
        // 현재 선택된 스테이지라면 정보 갱신
        if (selectedStageId == stageId)
        {
            DisplayStageInfo(stageId);
        }
    }
    
    /// <summary>
    /// 📌 Phase 6: 마지막 플레이 챕터 ID 가져오기
    /// </summary>
    private int GetLastPlayedChapterId()
    {
        // PlayerDataManager에서 마지막 플레이 챕터 가져오기
        if (PlayerDataManager.Instance != null && 
            PlayerDataManager.Instance.selectedPlayerData != null)
        {
            int lastChapter = PlayerDataManager.Instance.selectedPlayerData.currentChapterId;
            
            // 유효성 검사 (1~5 범위)
            if (lastChapter >= 1 && lastChapter <= 5)
            {
                Debug.Log($"📍 [StageSelectPanelController] 마지막 플레이 챕터 로드: Chapter {lastChapter}");
                return lastChapter;
            }
        }
        
        // 기본값: 챕터 1
        Debug.Log($"📍 [StageSelectPanelController] 기본 챕터1 사용");
        return 1;
    }
    
    // ========================================
    // 🎬 챕터 시작 컷신 시스템
    // ========================================
    
    /// <summary>
    /// 챕터 시작 컷신 재생 체크
    /// </summary>
    private void CheckAndPlayChapterStartCutscene(int chapterId)
    {
        // Stage 1 Config 로드
        string stageId = $"CH{chapterId:D2}_ST01";
        StageConfig stageConfig = LoadStageConfig(stageId);
        
        if (stageConfig == null)
        {
            Debug.LogWarning($"🎬 [StageSelectPanelController] StageConfig 로드 실패: {stageId}");
            return;
        }
        
        if (string.IsNullOrEmpty(stageConfig.chapterStartCutsceneId))
        {
            Debug.Log($"🎬 [StageSelectPanelController] 챕터 {chapterId} 시작 컷신 없음");
            return;
        }
        
        // 컷신 재생 여부 확인
        if (CutsceneSystem.CutsceneManager.Instance == null)
        {
            Debug.LogWarning("🎬 [StageSelectPanelController] CutsceneManager가 없습니다!");
            return;
        }
        
        // ✅ 챕터 시작 컷신은 이미 본 적 있으면 스킵
        bool hasSeen = CutsceneSystem.CutsceneManager.Instance.HasSeenCutscene(
            stageConfig.chapterStartCutsceneId
        );
        
        if (hasSeen)
        {
            Debug.Log($"🎬 [StageSelectPanelController] 챕터 {chapterId} 시작 컷신 스킵 (이미 시청)");
            return;
        }
        
        // 최초 시청 - 컷신 재생
        Debug.Log($"🎬 [StageSelectPanelController] 챕터 {chapterId} 시작 컷신 재생: {stageConfig.chapterStartCutsceneId}");
        StartCoroutine(PlayChapterStartCutsceneCoroutine(stageConfig.chapterStartCutsceneId));
    }
    
    /// <summary>
    /// 챕터 시작 컷신 재생 코루틴
    /// </summary>
    private IEnumerator PlayChapterStartCutsceneCoroutine(string cutsceneId)
    {
        Debug.Log($"🎬 [StageSelectPanelController] 컷신 재생 시작: {cutsceneId}");
        
        CutsceneSystem.CutsceneManager.Instance.PlayCutscene(cutsceneId);
        
        // 컷신 종료 대기
        yield return new WaitUntil(() => !CutsceneSystem.CutsceneManager.Instance.IsPlaying);
        
        Debug.Log($"🎬 [StageSelectPanelController] 컷신 재생 완료: {cutsceneId}");
    }
}

