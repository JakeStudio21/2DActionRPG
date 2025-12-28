using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StageSystem; // 🆕 StageSystem namespace 추가

public class StageSelectUIController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text titleText;
    public Button stage1Button;
    public Button stage2Button;
    public Button stage3Button;
    public Button playButton;
    public Button backToLobbyButton;
    
    [Header("Stage Button Images")]
    public Image stage1Image;
    public Image stage2Image; 
    public Image stage3Image;
    
    [Header("Visual Feedback")]
    public Color selectedColor = Color.white;
    public Color normalColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public Color completedColor = Color.green; // 🆕 완료된 스테이지 색상
    
    // 🆕 StageProgressManager 연동을 위한 필드들
    [Header("Stage Progress Integration")]
    [SerializeField] private Button[] stageButtons; // 배열로 통합 관리
    [SerializeField] private Image[] stageImages;   // 배열로 통합 관리
    private string[] stageIds = {"STAGE_001", "STAGE_002", "STAGE_003"}; // StageID 매핑
    
    // 🆕 스테이지 정보 표시용 UI
    [Header("Stage Information Display")]
    public TMP_Text stageNameText;
    public TMP_Text stageDescriptionText;
    public TMP_Text bestTimeText;
    public TMP_Text rewardPreviewText;
    
    // 현재 선택된 스테이지
    private int selectedStageNumber = 0; // 0 = 선택안함, 1-3 = 스테이지 번호
    
    void Start()
    {
        InitializeUI();
    }
    
    // 🆕 이벤트 구독 (컴포넌트 활성화 시)
    private void OnEnable()
    {
        // 스테이지 진행도 이벤트 구독
        if (StageProgressManager.Instance != null)
        {
            StageProgressManager.Instance.OnStageUnlocked += OnStageUnlocked;
            StageProgressManager.Instance.OnStageCompleted += OnStageCompleted;
        }
    }

    // 🆕 이벤트 구독 해제 (컴포넌트 비활성화 시)
    private void OnDisable()
    {
        // 이벤트 구독 해제
        if (StageProgressManager.Instance != null)
        {
            StageProgressManager.Instance.OnStageUnlocked -= OnStageUnlocked;
            StageProgressManager.Instance.OnStageCompleted -= OnStageCompleted;
        }
    }
    
    private void InitializeUI()
    {
        // 🆕 배열 초기화 (Inspector에서 할당되지 않은 경우)
        InitializeStageArrays();
        
        // UI 요소들의 null 체크
        ValidateUIElements();
        
        // 버튼 이벤트 연결
        ConnectButtonEvents();
        
        // ⭐ 추가: 선택된 캐릭터 정보 표시
        DisplaySelectedCharacter();
        
        // 🆕 진행도 기반 UI 업데이트
        UpdateStageProgressUI();
        
        // 초기 상태 설정
        ResetStageSelection();
        
        Debug.Log("[StageSelectUIController] 초기화 완료 (StageProgressManager 연동)");
    }
    
    // 🆕 스테이지 배열 초기화
    private void InitializeStageArrays()
    {
        if (stageButtons == null || stageButtons.Length == 0)
        {
            stageButtons = new Button[] { stage1Button, stage2Button, stage3Button };
        }
        
        if (stageImages == null || stageImages.Length == 0)
        {
            stageImages = new Image[] { stage1Image, stage2Image, stage3Image };
        }
    }
    
    // 🆕 진행도 기반 UI 업데이트
    private void UpdateStageProgressUI()
    {
        if (StageProgressManager.Instance == null)
        {
            Debug.LogWarning("[StageSelectUIController] StageProgressManager가 없습니다. 기본 UI로 표시합니다.");
            return;
        }
        
        for (int i = 0; i < stageButtons.Length && i < stageIds.Length; i++)
        {
            string stageId = stageIds[i];
            bool isUnlocked = StageProgressManager.Instance.IsStageUnlocked(stageId);
            bool isCompleted = StageProgressManager.Instance.IsStageCompleted(stageId);
            
            // 버튼 활성화/비활성화
            if (stageButtons[i] != null)
            {
                stageButtons[i].interactable = isUnlocked;
            }
            
            // 시각적 피드백 업데이트
            UpdateStageButtonVisual(stageImages[i], isUnlocked, isCompleted, false);
            
            Debug.Log($"[StageSelectUIController] {stageId}: 해금={isUnlocked}, 완료={isCompleted}");
        }
    }
    
    // 🆕 스테이지 버튼 시각적 상태 업데이트
    private void UpdateStageButtonVisual(Image buttonImage, bool isUnlocked, bool isCompleted, bool isSelected)
    {
        if (buttonImage == null) return;
        
        if (isSelected)
            buttonImage.color = selectedColor;      // 선택된 상태 (최우선)
        else if (!isUnlocked)
            buttonImage.color = disabledColor;      // 잠긴 상태
        else if (isCompleted)
            buttonImage.color = completedColor;     // 완료된 상태
        else
            buttonImage.color = normalColor;        // 해금된 상태
    }
    
    private void ValidateUIElements()
    {
        if (titleText == null)
            Debug.LogError("[StageSelectUIController] titleText가 연결되지 않았습니다!");
            
        if (stage1Button == null)
            Debug.LogError("[StageSelectUIController] stage1Button이 연결되지 않았습니다!");
            
        if (stage2Button == null)
            Debug.LogError("[StageSelectUIController] stage2Button이 연결되지 않았습니다!");
            
        if (stage3Button == null)
            Debug.LogError("[StageSelectUIController] stage3Button이 연결되지 않았습니다!");
            
        if (playButton == null)
            Debug.LogError("[StageSelectUIController] playButton이 연결되지 않았습니다!");
            
        if (backToLobbyButton == null)
            Debug.LogWarning("[StageSelectUIController] backToLobbyButton이 연결되지 않았습니다!");
    }
    
    private void ConnectButtonEvents()
    {
        if (stage1Button != null)
            stage1Button.onClick.AddListener(() => OnStageSelected(1));
            
        if (stage2Button != null)
            stage2Button.onClick.AddListener(() => OnStageSelected(2));
            
        if (stage3Button != null)
            stage3Button.onClick.AddListener(() => OnStageSelected(3));
            
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
            
        if (backToLobbyButton != null)
            backToLobbyButton.onClick.AddListener(OnBackToLobbyClicked);
    }
    
    /// <summary>
    /// ⭐ 추가: 선택된 캐릭터 정보 표시
    /// </summary>
    private void DisplaySelectedCharacter()
    {
        if (GameManager.Instance != null)
        {
            var playerData = GameManager.Instance.selectedPlayerData;
            if (playerData != null && playerData.selectedPlayerType != PlayerType.None)
            {
                string characterName = playerData.selectedPlayerType.ToString();
                Debug.Log($"[StageSelectUIController] 선택된 캐릭터: {characterName}, 무기: {playerData.weaponName}");
            }
            else
            {
                Debug.LogWarning("[StageSelectUIController] 플레이어 데이터가 없습니다!");
                // 플레이어 데이터가 없으면 로비로 돌아가기
                OnBackToLobbyClicked();
                return;
            }
        }
    }
    
    /// <summary>
    /// 🔧 수정: 스테이지 선택 처리 + 스테이지 정보 표시 추가
    /// </summary>
    public void OnStageSelected(int stageNumber)
    {
        if (stageNumber < 1 || stageNumber > 3)
        {
            Debug.LogError($"[StageSelectUIController] 잘못된 스테이지 번호: {stageNumber}");
            return;
        }
        
        // 🆕 해금 상태 체크
        string stageId = stageIds[stageNumber - 1];
        if (StageProgressManager.Instance != null && !StageProgressManager.Instance.IsStageUnlocked(stageId))
        {
            Debug.LogWarning($"[StageSelectUIController] {stageId}는 아직 해금되지 않았습니다!");
            return;
        }
        
        selectedStageNumber = stageNumber;
        UpdateStageSelectionUI();
        
        // 🆕 스테이지 상세 정보 표시
        DisplayStageInfo(stageId);
        
        Debug.Log($"[StageSelectUIController] 스테이지 {stageNumber} ({stageId}) 선택됨");
    }
    
    // 🆕 스테이지 상세 정보 표시
    private void DisplayStageInfo(string stageId)
    {
        var stageConfig = LoadStageConfig(stageId);
        var progress = StageProgressManager.Instance?.GetStageProgress(stageId);
        
        if (stageConfig != null)
        {
            // 스테이지 이름 표시
            if (stageNameText != null)
            {
                stageNameText.text = stageConfig.StageName;
            }
            
            // 스테이지 설명 표시
            if (stageDescriptionText != null)
            {
                stageDescriptionText.text = stageConfig.Description;
            }
            
            // 최고 기록 표시
            if (bestTimeText != null && progress != null)
            {
                if (progress.isCompleted && progress.bestClearTime > 0)
                {
                    bestTimeText.text = $"최고 기록: {progress.bestClearTime}초";
                }
                else
                {
                    bestTimeText.text = "미완료";
                }
            }
            
            // 보상 미리보기 표시
            if (rewardPreviewText != null)
            {
                DisplayRewardPreview(stageConfig, progress);
            }
            
            Debug.Log($"[StageSelectUIController] {stageId} 정보 표시 완료");
        }
        else
        {
            Debug.LogError($"[StageSelectUIController] {stageId} StageConfig 로드 실패");
        }
    }
    
    // 🆕 보상 미리보기 표시
    private void DisplayRewardPreview(StageConfig stageConfig, StageProgress progress)
    {
        if (rewardPreviewText == null) return;
        
        string rewardText = "";
        
        // 첫 클리어 보상 vs 반복 보상 구분
        bool canClaimFirstReward = progress != null && progress.CanClaimFirstClearReward();
        
        if (canClaimFirstReward)
        {
            // 첫 클리어 보상 표시
            if (stageConfig.FirstClearDropTable != null)
            {
                rewardText = $"🎁 첫 클리어: 골드 {stageConfig.FirstClearDropTable.Gold}, EXP {stageConfig.FirstClearDropTable.Exp}";
            }
        }
        else
        {
            // 반복 보상 표시
            if (stageConfig.RepeatClearDropTable != null)
            {
                rewardText = $"💰 반복: 골드 {stageConfig.RepeatClearDropTable.Gold}, EXP {stageConfig.RepeatClearDropTable.Exp}";
            }
        }
        
        rewardPreviewText.text = rewardText;
    }
    
    // 🆕 StageConfig 로드
    private StageConfig LoadStageConfig(string stageId)
    {
        string path = $"Stages/Configs/{stageId}_Config";
        StageConfig config = Resources.Load<StageConfig>(path);
        
        if (config == null)
        {
            Debug.LogError($"[StageSelectUIController] StageConfig 로드 실패: {path}");
        }
        
        return config;
    }
    
    /// <summary>
    /// 🔧 수정: 스테이지 선택 UI 업데이트 (진행도 반영)
    /// </summary>
    private void UpdateStageSelectionUI()
    {
        // ⭐ 수정: 캐릭터 정보 + 스테이지 정보 함께 표시
        if (titleText != null)
        {
            string characterInfo = GetSelectedCharacterInfo();
            
            if (selectedStageNumber > 0)
            {
                string stageId = stageIds[selectedStageNumber - 1];
                titleText.text = $"{characterInfo} | Selected: {stageId}";
            }
            else
                titleText.text = $"{characterInfo} | Select Stage";
        }
        
        // 🔧 수정: 스테이지 버튼 색상 업데이트 (진행도 반영)
        for (int i = 0; i < stageImages.Length && i < stageIds.Length; i++)
        {
            string stageId = stageIds[i];
            bool isSelected = (selectedStageNumber == i + 1);
            bool isUnlocked = StageProgressManager.Instance?.IsStageUnlocked(stageId) ?? true;
            bool isCompleted = StageProgressManager.Instance?.IsStageCompleted(stageId) ?? false;
            
            UpdateStageButtonVisual(stageImages[i], isUnlocked, isCompleted, isSelected);
        }
        
        // Play 버튼 활성화/비활성화
        if (playButton != null)
        {
            bool canPlay = selectedStageNumber > 0;
            if (canPlay && StageProgressManager.Instance != null)
            {
                string selectedStageId = stageIds[selectedStageNumber - 1];
                canPlay = StageProgressManager.Instance.IsStageUnlocked(selectedStageId);
            }
            
            playButton.interactable = canPlay;
        }
    }
    
    /// <summary>
    /// ⭐ 추가: 선택된 캐릭터 정보 가져오기
    /// </summary>
    private string GetSelectedCharacterInfo()
    {
        if (GameManager.Instance != null)
        {
            var playerData = GameManager.Instance.selectedPlayerData;
            if (playerData != null && playerData.selectedPlayerType != PlayerType.None)
            {
                return playerData.selectedPlayerType.ToString();
            }
        }
        return "No Character";
    }
    
    /// <summary>
    /// 🔧 수정: Play 버튼 클릭 처리 (GameManager의 런타임 데이터 사용)
    /// </summary>
    public void OnPlayButtonClicked()
    {
        if (selectedStageNumber <= 0)
        {
            Debug.LogWarning("[StageSelectUIController] 스테이지를 먼저 선택해주세요!");
            return;
        }
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("[StageSelectUIController] GameManager가 없습니다!");
            return;
        }
        
        // ⭐ 수정: GameManager의 SelectedPlayerData 확인 (원래 방식 복구)
        var playerData = GameManager.Instance.selectedPlayerData;
        if (playerData == null || playerData.selectedPlayerType == PlayerType.None)
        {
            Debug.LogError("[StageSelectUIController] 플레이어 클래스가 선택되지 않았습니다!");
            Debug.LogError($"[StageSelectUIController] 현재 플레이어 데이터: {playerData}");
            // 로비로 돌아가서 캐릭터 선택하도록 안내
            OnBackToLobbyClicked();
            return;
        }
        
        // 선택된 스테이지 정보 저장
        string selectedSceneName = GetSceneNameFromStageNumber(selectedStageNumber);
        string stageKey = stageIds[selectedStageNumber - 1];
        var stageConfig = LoadStageConfig(stageKey);
        
        if (stageConfig == null)
        {
            Debug.LogError($"[StageSelectUIController] StageConfig를 찾을 수 없습니다: {stageKey}");
            return;
        }
        
        GameManager.Instance.SetSelectedStage(selectedStageNumber, selectedSceneName);
        
        Debug.Log($"[StageSelectUIController] 스테이지 {selectedStageNumber} ({selectedSceneName})로 게임 시작");
        Debug.Log($"[StageSelectUIController] 플레이어 정보: {playerData.selectedPlayerType}, {playerData.weaponName}");
        
        // 🎬 Phase 4: 챕터 시작 컷신 체크 (Stage 1 첫 진입 시)
        if (stageConfig.stageIndexInChapter == 1)
        {
            var chapterData = StageSystem.ChapterManager.Instance?.GetChapterData(stageConfig.chapterId);
            
            if (chapterData != null && !string.IsNullOrEmpty(chapterData.chapterStartCutsceneId))
            {
                bool hasSeen = CutsceneSystem.CutsceneManager.Instance != null && 
                               CutsceneSystem.CutsceneManager.Instance.HasSeenCutscene(chapterData.chapterStartCutsceneId);
                
                if (!hasSeen)
                {
                    Debug.Log($"🎬 [StageSelectUIController] 챕터 시작 컷신 재생: {chapterData.chapterStartCutsceneId}");
                    
                    // 챕터 시작 컷신 재생
                    if (CutsceneSystem.CutsceneManager.Instance != null)
                    {
                        CutsceneSystem.CutsceneManager.Instance.PlayCutscene(chapterData.chapterStartCutsceneId);
                        
                        // 컷신 종료 후 스테이지 로드
                        CutsceneSystem.CutsceneManager.Instance.OnCutsceneEnd += (cutsceneId) => {
                            if (cutsceneId == chapterData.chapterStartCutsceneId)
                            {
                                // 컷신 종료 후 씬 전환
                                GameManager.Instance.LoadGameScene(selectedSceneName);
                            }
                        };
                        return; // 컷신 재생 중이므로 바로 씬 전환하지 않음
                    }
                }
            }
        }
        
        // 컷신 없으면 바로 스테이지 로드
        GameManager.Instance.LoadGameScene(selectedSceneName);
    }
    
    /// <summary>
    /// 🔧 수정: 스테이지 번호를 씬 이름으로 변환 (동적 로드)
    /// </summary>
    private string GetSceneNameFromStageNumber(int stageNumber)
    {
        if (stageNumber < 1 || stageNumber > stageIds.Length)
        {
            Debug.LogError($"[StageSelectUIController] 잘못된 스테이지 번호: {stageNumber}");
            return "Stage_001"; // 기본값
        }
        
        // 🆕 하드코딩 제거 → StageConfig에서 SceneName 가져오기
        string stageId = stageIds[stageNumber - 1];
        var stageConfig = LoadStageConfig(stageId);
        
        if (stageConfig != null)
        {
            Debug.Log($"[StageSelectUIController] {stageId} → {stageConfig.SceneName}");
            return stageConfig.SceneName;
        }
        
        // 폴백: 기본 씬 이름 생성
        string fallbackScene = $"Stage_{stageId}";
        Debug.LogWarning($"[StageSelectUIController] StageConfig 없음, 폴백 사용: {fallbackScene}");
        return fallbackScene;
    }
    
    /// <summary>
    /// 로비로 돌아가기
    /// </summary>
    public void OnBackToLobbyClicked()
    {
        Debug.Log("[StageSelectUIController] 로비로 돌아갑니다");
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadLobbyScene();
        }
        else
        {
            Debug.LogError("[StageSelectUIController] GameManager가 없습니다!");
        }
    }
    
    /// <summary>
    /// 스테이지 선택 초기화
    /// </summary>
    private void ResetStageSelection()
    {
        selectedStageNumber = 0;
        UpdateStageSelectionUI();
    }
    
    // 🆕 스테이지 해금 이벤트 처리
    private void OnStageUnlocked(string stageId)
    {
        Debug.Log($"[StageSelectUIController] 스테이지 해금됨: {stageId}");
        UpdateStageProgressUI(); // UI 갱신
        
        // 현재 선택된 스테이지 정보도 갱신
        if (selectedStageNumber > 0)
        {
            UpdateStageSelectionUI();
        }
    }

    // 🆕 스테이지 완료 이벤트 처리
    private void OnStageCompleted(string stageId, bool isFirstClear)
    {
        Debug.Log($"[StageSelectUIController] 스테이지 완료됨: {stageId} (첫클리어: {isFirstClear})");
        UpdateStageProgressUI(); // UI 갱신
        
        // 현재 선택된 스테이지가 완료된 스테이지라면 정보 갱신
        if (selectedStageNumber > 0 && stageIds[selectedStageNumber - 1] == stageId)
        {
            DisplayStageInfo(stageId);
        }
    }
    
    // 외부에서 호출 가능한 메서드들 (Unity 에디터에서 버튼에 연결용)
    public void OnStage1Button() => OnStageSelected(1);
    public void OnStage2Button() => OnStageSelected(2);
    public void OnStage3Button() => OnStageSelected(3);
} 