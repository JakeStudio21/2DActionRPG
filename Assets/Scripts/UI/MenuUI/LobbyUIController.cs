using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StageSystem; // 🆕 StageSystem namespace 추가
using System.IO; // 🆕 파일 입출력 네임스페이스 추가
using System.Collections; // 🔧 IEnumerator를 위한 네임스페이스 추가
using System.Collections.Generic; // 🆕 리스트 네임스페이스 추가
using CueSystem; // 🆕 CueSystem namespace 추가

/// <summary>
/// 로비 UI 통합 컨트롤러 (캐릭터 선택 + 스테이지 선택)
/// 기존 StageSelect 씬 기능을 통합하여 단일 씬에서 모든 선택 처리
/// </summary>
public class LobbyUIController : MonoBehaviour
{
    // 🔧 Step 2-2: 슬롯 시스템 추가
    [Header("=== 캐릭터 슬롯 시스템 ===")]
    public CharacterCreationController characterCreationController; // 캐릭터 생성 컨트롤러 참조
    public ConfirmationPopup confirmationPopup; // 🆕 확인 팝업 컨트롤러 참조
    public Button[] characterSlotButtons = new Button[3]; // Slot 0, 1, 2 버튼 배열
    public GameObject[] slotPanels = new GameObject[3]; // 각 슬롯 패널
    
    [Header("=== 슬롯 UI 요소들 ===")]
    // Slot 0 UI 요소
    public Image slot0CharacterIcon;
    public TMP_Text slot0CharacterName;
    public TMP_Text slot0CharacterLevel;
    public TMP_Text slot0CharacterClass;
    public Button slot0DeleteButton;
    public GameObject slot0EmptyPanel; // 빈 슬롯 표시 패널
    
    // Slot 1 UI 요소
    public Image slot1CharacterIcon;
    public TMP_Text slot1CharacterName;
    public TMP_Text slot1CharacterLevel;
    public TMP_Text slot1CharacterClass;
    public Button slot1DeleteButton;
    public GameObject slot1EmptyPanel;
    
    // Slot 2 UI 요소
    public Image slot2CharacterIcon;
    public TMP_Text slot2CharacterName;
    public TMP_Text slot2CharacterLevel;
    public TMP_Text slot2CharacterClass;
    public Button slot2DeleteButton;
    public GameObject slot2EmptyPanel;
    
    [Header("=== 클래스별 아이콘 ===")]
    public Sprite warriorIcon;
    public Sprite assassinIcon;
    public Sprite wizardIcon;
    public Sprite emptySlotIcon;

    // 🗑️ [삭제됨] 기존 캐릭터 선택 UI - 더 이상 사용하지 않음
    // public TMP_Text playerNameInfoText;
    // public Button warriorButton;
    // public Button assassinButton;
    // public Image warriorPanelImage;
    // public Image assassinPanelImage;
    
    [Header("=== 선택된 캐릭터 정보 ===")]
    public TMP_Text selectedPlayerNameText; // 🆕 선택된 캐릭터 이름 표시용
    public Button startGameButton; // 🆕 게임 시작 버튼
    
    [Header("=== UI 패널 관리 ===")]
    public GameObject lobbyPanel;          
    public GameObject stageSelectPanel;     
    public GameObject inventoryPanel;       
    public GameObject shopPanel;
    public GameObject characterInfoPanel;   
    
    [Header("🏪 로비 상점 버튼")]  // 🆕 추가
    public Button shopButton;               // 🆕 상점 버튼 참조
    public Button inventoryButton;          // 🆕 기존 가방 버튼 참조 (일관성)
    public Button characterInfoButton;      //  기존 영웅 버튼 참조 (일관성)
    public Button quitGameButton;           // 🆕 게임 종료 버튼 추가
    
    [Header("🎬 다시보기 버튼")]  // 🆕 Phase 7 추가
    public Button replayIntroButton;        // 🆕 인트로 다시보기 버튼
    public Button replayTutorialButton;     // 🆕 튜토리얼 다시보기 버튼
    
    // 🗑️ [Phase 6-Pre 삭제] 스테이지 관련 필드들 - StageSelectPanelController로 이동
    
    [Header("=== 🎯 스테이지 선택 패널 컨트롤러 ===")]
    public StageSelectPanelController stageSelectPanelController; // Phase 6-Pre 추가
    
    // 내부 상태
    private int selectedSlotIndex = -1; // 현재 선택된 슬롯 (-1: 미선택)
    
    [Header("🎯 배경 오버레이")]
    [SerializeField] private GameObject backgroundOverlay;          // 🆕 투명 배경 오버레이
    [SerializeField] private BackgroundOverlayHandler overlayHandler; // 🆕 오버레이 핸들러 (선택사항)
    
    [Header("🏢 회사 로고 페이드 효과")]  // 🆕 추가
    public Image companyLogoImage;          // 🆕 회사 로고 이미지 (알파값 0으로 시작)
    public float fadeInDuration = 1f;       // 🆕 페이드인 시간
    public float displayDuration = 2f;      // 🆕 로고 표시 시간
    // public float fadeOutDuration = 1f;   // 🗑️ 더 이상 사용 안 함 (페이드아웃 제거)
    
    void Start()
    {
        Debug.Log("🚀 [LobbyUIController] Start() 시작");
        
        // �� 핵심 매니저들 초기화 상태 확인
        CheckManagerInitializationStatus();
        
        InitializeUI();
        InitializeSlotSystem(); // 🔧 슬롯 시스템 초기화
        
        // 🎬 Phase 5: 챕터 종료 컷신 체크 (로비 진입 후)
        StartCoroutine(CheckPendingChapterClearCutscene());
        
        // 🎯 Phase 6-Pre: StageSelectPanelController 이벤트 구독
        if (stageSelectPanelController != null)
        {
            stageSelectPanelController.OnPlayButtonClicked += OnStagePlayButtonClicked;
            stageSelectPanelController.OnBackButtonClicked += OnStageBackButtonClicked;
        }
        
        Debug.Log("✅ [LobbyUIController] Start() 완료");
        
        // 🆕 배경 오버레이 검증 (Start 메서드 끝부분에 추가)
        ValidateBackgroundOverlay();
    }
    
    // 🆕 이벤트 구독 (컴포넌트 활성화 시)
    private void OnEnable()
    {
        // 🗑️ [Phase 6-Pre 삭제] 스테이지 진행도 이벤트 구독 - StageSelectPanelController에서 처리
        
        // 🆕 캐릭터 생성 이벤트 구독
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnCharacterCreated += OnCharacterCreationCompleted;
        }
    }

    // 🗑️ [Phase 6-Pre 삭제] RefreshUIAfterSceneLoad() - StageSelectPanelController에서 처리
    
    // 🆕 이벤트 구독 해제 (컴포넌트 비활성화 시)
    private void OnDisable()
    {
        // 🗑️ [Phase 6-Pre 삭제] 스테이지 진행도 이벤트 구독 해제 - StageSelectPanelController에서 처리
        
        // 🆕 캐릭터 생성 이벤트 구독 해제
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnCharacterCreated -= OnCharacterCreationCompleted;
        }
        
        // 🎯 Phase 6-Pre: StageSelectPanelController 이벤트 구독 해제
        if (stageSelectPanelController != null)
        {
            stageSelectPanelController.OnPlayButtonClicked -= OnStagePlayButtonClicked;
            stageSelectPanelController.OnBackButtonClicked -= OnStageBackButtonClicked;
        }
    }
    
    private void InitializeUI()
    {
        Debug.Log("[LobbyUIController] 통합 UI 초기화 시작");
        
        // 🗑️ [Phase 6-Pre 삭제] 스테이지 배열 초기화 - StageSelectPanelController에서 처리
        
        // UI 요소 유효성 검사
        ValidateUIElements();
        
        // 버튼 이벤트 연결
        ConnectButtonEvents();
        
        // 🆕 Phase 6-Pre: StageSelectPanelController 초기화
        stageSelectPanelController?.RefreshStageProgressUI();
        
        // 초기 상태 설정
        SetInitialState();
        
        Debug.Log("[LobbyUIController] 통합 UI 초기화 완료");
    }
    
    // 🔧 Step 2-2: 슬롯 시스템 초기화
    private void InitializeSlotSystem()
    {
        Debug.Log("[LobbyUIController] 슬롯 시스템 초기화 시작");
        
        // PlayerDataManager에서 모든 슬롯 로드
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.LoadAllSlots();
        }
        
        // 슬롯 버튼 이벤트 연결
        ConnectSlotButtonEvents();
        
        // 🆕 자동 슬롯 선택 (핵심 추가)
        AutoSelectSlotIfNeeded();
        
        // 🔧 수정: StageProgressManager 초기화 조건 개선
        if (StageProgressManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
        {
            // 🔧 수정: 이미 초기화되어 있고 같은 슬롯이면 재초기화 하지 않음
            int currentSlot = PlayerDataManager.Instance.GetCurrentSlotIndex();
            if (!StageProgressManager.Instance.IsInitialized || 
                StageProgressManager.Instance.GetCurrentSlotIndex() != currentSlot)
            {
                StageProgressManager.Instance.InitializeFor(currentSlot);
                Debug.Log($"🔄 [LobbyUIController] StageProgressManager 재초기화 필요: 슬롯 {currentSlot}");
            }
            else
            {
                Debug.Log($"🔄 [LobbyUIController] StageProgressManager 이미 초기화됨: 슬롯 {currentSlot}");
            }
        }
        
        // 슬롯 UI 업데이트
        RefreshAllSlots();
        
        // 🆕 Phase 6-Pre: StageSelectPanelController 초기화
        stageSelectPanelController?.RefreshStageProgressUI();
        
        Debug.Log("[LobbyUIController] 슬롯 시스템 초기화 완료");
    }
    
    // 🆕 스테이지 배열 초기화
    // 🗑️ [Phase 6-Pre 삭제] InitializeStageArrays() - StageSelectPanelController로 이동
    
    // 🆕 진행도 기반 UI 업데이트
    // 🗑️ [Phase 6-Pre 삭제] UpdateStageProgressUI() - StageSelectPanelController.RefreshStageProgressUI()로 대체
    // 🗑️ [Phase 6-Pre 삭제] RetryUpdateStageProgressUI() - StageSelectPanelController로 이동
    
    private void ValidateUIElements()
    {
        // 🗑️ [삭제됨] 기존 캐릭터 선택 UI 검증
        // if (playerNameInfoText == null) Debug.LogError("[LobbyUIController] playerNameInfoText 누락!");
        // if (warriorButton == null) Debug.LogError("[LobbyUIController] warriorButton 누락!");
        // if (assassinButton == null) Debug.LogError("[LobbyUIController] assassinButton 누락!");
        
        // 🆕 새로운 UI 요소들 검증
        if (selectedPlayerNameText == null) Debug.LogError("[LobbyUIController] selectedPlayerNameText 누락!");
        if (startGameButton == null) Debug.LogError("[LobbyUIController] startGameButton 누락!");
        
        // 패널 검증
        if (lobbyPanel == null) Debug.LogError("[LobbyUIController] lobbyPanel 누락!");
        // if (characterSelectPanel == null) Debug.LogError("[LobbyUIController] characterSelectPanel 누락!");  // 🗑️ 삭제
        
        // 🗑️ [Phase 6-Pre 삭제] 스테이지 선택 UI 검증 - StageSelectPanelController에서 처리
        // StageSelectPanelController 검증
        if (stageSelectPanelController == null) Debug.LogError("[LobbyUIController] stageSelectPanelController 누락!");

        // 🔧 Step 2-2: 슬롯 버튼 배열 검증
        if (characterSlotButtons == null || characterSlotButtons.Length != 3) Debug.LogError("[LobbyUIController] characterSlotButtons 누락 또는 3개가 아닙니다!");
        if (slotPanels == null || slotPanels.Length != 3) Debug.LogError("[LobbyUIController] slotPanels 누락 또는 3개가 아닙니다!");

        // 🔧 Step 2-2: 슬롯 UI 요소들 검증
        if (slot0CharacterIcon == null) Debug.LogError("[LobbyUIController] slot0CharacterIcon 누락!");
        if (slot0CharacterName == null) Debug.LogError("[LobbyUIController] slot0CharacterName 누락!");
        if (slot0CharacterLevel == null) Debug.LogError("[LobbyUIController] slot0CharacterLevel 누락!");
        if (slot0CharacterClass == null) Debug.LogError("[LobbyUIController] slot0CharacterClass 누락!");
        if (slot0DeleteButton == null) Debug.LogError("[LobbyUIController] slot0DeleteButton 누락!");
        if (slot0EmptyPanel == null) Debug.LogError("[LobbyUIController] slot0EmptyPanel 누락!");

        if (slot1CharacterIcon == null) Debug.LogError("[LobbyUIController] slot1CharacterIcon 누락!");
        if (slot1CharacterName == null) Debug.LogError("[LobbyUIController] slot1CharacterName 누락!");
        if (slot1CharacterLevel == null) Debug.LogError("[LobbyUIController] slot1CharacterLevel 누락!");
        if (slot1CharacterClass == null) Debug.LogError("[LobbyUIController] slot1CharacterClass 누락!");
        if (slot1DeleteButton == null) Debug.LogError("[LobbyUIController] slot1DeleteButton 누락!");
        if (slot1EmptyPanel == null) Debug.LogError("[LobbyUIController] slot1EmptyPanel 누락!");

        if (slot2CharacterIcon == null) Debug.LogError("[LobbyUIController] slot2CharacterIcon 누락!");
        if (slot2CharacterName == null) Debug.LogError("[LobbyUIController] slot2CharacterName 누락!");
        if (slot2CharacterLevel == null) Debug.LogError("[LobbyUIController] slot2CharacterLevel 누락!");
        if (slot2CharacterClass == null) Debug.LogError("[LobbyUIController] slot2CharacterClass 누락!");
        if (slot2DeleteButton == null) Debug.LogError("[LobbyUIController] slot2DeleteButton 누락!");
        if (slot2EmptyPanel == null) Debug.LogError("[LobbyUIController] slot2EmptyPanel 누락!");

        // 🔧 Step 2-2: 클래스 아이콘 검증
        if (warriorIcon == null) Debug.LogError("[LobbyUIController] warriorIcon 누락!");
        if (assassinIcon == null) Debug.LogError("[LobbyUIController] assassinIcon 누락!");
        if (wizardIcon == null) Debug.LogError("[LobbyUIController] wizardIcon 누락!");
        if (emptySlotIcon == null) Debug.LogError("[LobbyUIController] emptySlotIcon 누락!");
    }
    
    private void ConnectButtonEvents()
    {
        // 🆕 게임 시작 버튼 이벤트 연결
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(() => {
                // 🆕 Cue 이벤트 발행
                var context = new CueContext { position = Vector3.zero, actorType = ActorType.UI };
                CueEmitter.Emit("ui.button.click", "UI", context);
                OnStartGameButtonClicked();
            });
        }
        
        // 🆕 로비 주요 버튼들 연결
        if (inventoryButton != null)
        {
            inventoryButton.onClick.AddListener(() => {
                // 🆕 Cue 이벤트 발행
                var context = new CueContext { position = Vector3.zero, actorType = ActorType.UI };
                CueEmitter.Emit("ui.button.click", "UI", context);
                ShowInventoryPanel();
            });
            Debug.Log("✅ [LobbyUIController] 인벤토리 버튼 이벤트 연결");
        }
        
        if (characterInfoButton != null)
        {
            characterInfoButton.onClick.AddListener(() => {
                // 🆕 Cue 이벤트 발행
                var context = new CueContext { position = Vector3.zero, actorType = ActorType.UI };
                CueEmitter.Emit("ui.button.click", "UI", context);
                ShowCharacterInfoPanel();
            });
            Debug.Log("✅ [LobbyUIController] 캐릭터 정보 버튼 이벤트 연결");
        }
        
        if (shopButton != null)  // 🆕 상점 버튼 연결
        {
            shopButton.onClick.AddListener(() => {
                // 🆕 Cue 이벤트 발행
                var context = new CueContext { position = Vector3.zero, actorType = ActorType.UI };
                CueEmitter.Emit("ui.button.click", "UI", context);
                ShowShopPanel();
            });
            Debug.Log("✅ [LobbyUIController] 상점 버튼 이벤트 연결");
        }
        
        // 🆕 게임 종료 버튼 이벤트 연결
        if (quitGameButton != null)
        {
            quitGameButton.onClick.AddListener(() => {
                // 🆕 Cue 이벤트 발행
                var context = new CueContext { position = Vector3.zero, actorType = ActorType.UI };
                CueEmitter.Emit("ui.button.click", "UI", context);
                OnQuitGameButtonClicked();
            });
            Debug.Log("✅ [LobbyUIController] 게임 종료 버튼 이벤트 연결");
        }
        
        // 🆕 Phase 7: 다시보기 버튼 이벤트 연결
        if (replayIntroButton != null)
        {
            replayIntroButton.onClick.AddListener(() => {
                var context = new CueContext { position = Vector3.zero, actorType = ActorType.UI };
                CueEmitter.Emit("ui.button.click", "UI", context);
                OnReplayIntroButtonClicked();
            });
            Debug.Log("✅ [LobbyUIController] 인트로 다시보기 버튼 이벤트 연결");
        }
        
        if (replayTutorialButton != null)
        {
            replayTutorialButton.onClick.AddListener(() => {
                var context = new CueContext { position = Vector3.zero, actorType = ActorType.UI };
                CueEmitter.Emit("ui.button.click", "UI", context);
                OnReplayTutorialButtonClicked();
            });
            Debug.Log("✅ [LobbyUIController] 튜토리얼 다시보기 버튼 이벤트 연결");
        }
        
        // 기존 슬롯, 스테이지 관련 버튼들...
    }
    
    /// <summary>
    /// 초기 상태 설정 (상점 미리 초기화 포함)
    /// </summary>
    private void SetInitialState()
    {
        Debug.Log("[LobbyUIController] 초기 상태 설정 시작 (Z-Order 방식)");
        
        // 🎯 핵심 변경: 모든 패널을 활성화 상태로 유지
        SetPanelVisibility(lobbyPanel, true);
        SetPanelVisibility(stageSelectPanel, true);      // ✅ 변경: false → true
        SetPanelVisibility(inventoryPanel, true);        // ✅ 변경: false → true
        SetPanelVisibility(shopPanel, true);             // ✅ 변경: false → true
        SetPanelVisibility(characterInfoPanel, true);    // ✅ 변경: false → true
        
        // 🎯 Z-Order 설정: 로비가 최상위
        BringPanelToFront(lobbyPanel);
        
        // 🎯 모든 패널 백그라운드 초기화 (활성화 상태에서)
        StartCoroutine(InitializeAllPanelsInBackground());
        
        Debug.Log("[LobbyUIController] 초기 상태 설정 완료 (Z-Order 방식)");
    }

    /// <summary>
    /// 🔧 수정: 상점 패널 표시 (Z-Order 방식 + 안전한 데이터 동기화)
    /// </summary>
    public void ShowShopPanel()
    {
        if (!EnsureCharacterSelected()) return;
        
        Debug.Log("🏪 [LobbyUIController] ShowShopPanel 호출됨 (Z-Order 방식)");
        
        // Cue 이벤트 발행
        var context = new CueContext
        {
            position = Vector3.zero,
            actorType = ActorType.UI
        };
        CueEmitter.Emit("ui.button.click", "UI", context);
        CueEmitter.Emit("ui.shop.open", "UI", context);
        
        // 🎯 핵심 변경: SetActive 대신 Z-Order 사용
        BringPanelToFront(shopPanel);
        
        // 🆕 추가: 상점 진입 시 안전한 데이터 동기화
        StartCoroutine(SafeRefreshShopData());
        
        Debug.Log("[LobbyUIController] 상점 패널을 최상위로 이동 완료");
    }

    /// <summary>
    /// 🆕 추가: 상점 데이터 안전한 동기화 (LazyLoad 충돌 방지)
    /// </summary>
    private IEnumerator SafeRefreshShopData()
    {
        // 1프레임 대기 (패널 전환 완료 후)
        yield return null;
        
        // 현재 선택된 캐릭터 데이터가 로드되지 않은 경우에만 로드
        if (PlayerDataManager.Instance.IsLazyLoadRequired())
        {
            int selectedSlot = PlayerDataManager.Instance.GetSelectedSlotIndex();
            bool loadSuccess = PlayerDataManager.Instance.LazyLoadSlotData(selectedSlot);
            
            if (loadSuccess)
            {
                Debug.Log($"🔄 [LobbyUIController] 상점 진입 시 캐릭터 {selectedSlot} 데이터 로드 완료");
            }
            else
            {
                Debug.LogError("❌ [LobbyUIController] 상점 진입 시 캐릭터 데이터 로드 실패");
            }
        }
        
        // ShopInventoryUI 강제 새로고침
        var shopUIController = shopPanel.GetComponent<ShopUIController>();
        if (shopUIController != null)
        {
            // 상점 UI 갱신 (기존 메서드 활용)
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    #region === 캐릭터 선택 관련 ===
    
    // ️ [삭제됨] public void OnClassSelected(string className)
    // 🗑️ [삭제됨] private void UpdateClassSelectionUI(string className)
    // 🗑️ [삭제됨] private void SetPanelActiveVisual(Image panelImage, bool isActive)
    
    /// <summary>
    /// Map 버튼 클릭 처리 (스테이지 선택 패널로 전환)
    /// </summary>
    public void OnMapButtonClicked()
    {
        Debug.Log("[LobbyUIController] Map 버튼 클릭! 스테이지 선택으로 전환합니다.");
        
        // 캐릭터가 선택되었는지 확인
        if (LobbyManager.Instance == null)
        {
            Debug.LogError("[LobbyUIController] LobbyManager가 없습니다!");
            return;
        }
        
        // ⭐ 수정: GameManager의 selectedPlayerData 직접 확인 (기존 방식)
        if (GameManager.Instance.selectedPlayerData == null || 
            GameManager.Instance.selectedPlayerData.selectedPlayerType == PlayerType.None)
        {
            Debug.LogWarning("[LobbyUIController] 캐릭터를 먼저 선택해주세요!");
            return;
        }
        
        // 스테이지 선택 패널로 전환
        ShowStageSelectPanel();
    }
    
    #endregion
    
    #region === 스테이지 선택 관련 ===
    
    /// <summary>
    /// 🔧 수정: 스테이지 선택 처리 + 해금 상태 체크 추가
    /// </summary>
    // 🗑️ [Phase 6-Pre 삭제] OnStageSelected(), DisplayStageInfo(), DisplayRewardPreview(), LoadStageConfig() - StageSelectPanelController로 이동
    
    /// <summary>
    /// Play 버튼 클릭 처리 (게임 시작)
    /// </summary>
    // 🗑️ [Phase 6-Pre 삭제] 기존 OnPlayButtonClicked() - StageSelectPanelController 이벤트로 대체
    
    /// <summary>
    /// 🎯 Phase 6-Pre: StageSelectPanelController의 Play 버튼 이벤트 핸들러
    /// </summary>
    private void OnStagePlayButtonClicked(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[LobbyUIController] sceneName이 비어있습니다!");
            return;
        }
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("[LobbyUIController] GameManager가 없습니다!");
            return;
        }
        
        // 플레이어 데이터 확인
        var playerData = GameManager.Instance.selectedPlayerData;
        if (playerData == null || playerData.selectedPlayerType == PlayerType.None)
        {
            Debug.LogError("[LobbyUIController] 플레이어 클래스가 선택되지 않았습니다!");
            OnBackToCharacterSelect();
            return;
        }
        
        Debug.Log($"[LobbyUIController] 게임 시작: {sceneName}");
        Debug.Log($"[LobbyUIController] 플레이어 정보: {playerData.selectedPlayerType}");
        
        // 게임 씬으로 이동
        GameManager.Instance.LoadGameScene(sceneName);
    }
    
    /// <summary>
    /// 🎯 Phase 6-Pre: StageSelectPanelController의 Back 버튼 이벤트 핸들러
    /// </summary>
    private void OnStageBackButtonClicked()
    {
        Debug.Log("[LobbyUIController] 캐릭터 선택 화면으로 돌아갑니다.");
        
        // 로비 패널로 전환
        BringPanelToFront(lobbyPanel);
        
        // StageSelectPanelController 숨기기
        if (stageSelectPanelController != null)
        {
            stageSelectPanelController.HidePanel();
        }
    }
    
    // 🗑️ [Phase 6-Pre 삭제] GetSceneNameFromStageNumber() - StageSelectPanelController에서 처리
    
    /// <summary>
    /// 캐릭터 선택으로 돌아가기
    /// </summary>
    public void OnBackToCharacterSelect()
    {
        Debug.Log("[LobbyUIController] 캐릭터 선택으로 돌아갑니다");
        ShowLobbyPanel();
    }
    
    /// <summary>
    /// 로비 메인으로 돌아가기 (외부 호출용)
    /// </summary>
    public void OnBackToLobby()
    {
        Debug.Log("[LobbyUIController] 로비 메인으로 돌아갑니다");
        ShowLobbyPanel();
        RefreshAllSlots(); // 🔧 슬롯 새로고침 추가
    }
    
    #endregion
    
    #region === 패널 전환 관리 ===
    
    /// <summary>
    /// 🔧 수정: 로비 패널 표시 (Z-Order 방식 + 캐릭터 선택 UI 갱신)
    /// </summary>
    public void ShowLobbyPanel()
    {
        Debug.Log("🏠 [LobbyUIController] ShowLobbyPanel 호출됨 (Z-Order 방식)");
        
        // Cue 이벤트 발행
        var context = new CueContext
        {
            position = Vector3.zero,
            actorType = ActorType.UI
        };
        CueEmitter.Emit("ui.panel.close", "UI", context);
        
        // 🎯 핵심 변경: SetActive 대신 Z-Order 사용
        BringPanelToFront(lobbyPanel);
        
        // 🆕 캐릭터 선택 UI 갱신 (다른 패널에서 돌아올 때 필수)
        RefreshCharacterSelectionUI();
        
        Debug.Log("[LobbyUIController] 로비 패널을 최상위로 이동 완료");
    }
    
    // 🗑️ [삭제됨] ShowCharacterSelectPanel() - 더 이상 사용하지 않음
    
    /// <summary>
    /// 🔧 수정: 스테이지 선택 패널 표시 (Z-Order 방식)
    /// </summary>
    private void ShowStageSelectPanel()
    {
        if (!EnsureCharacterSelected()) return;
        
        Debug.Log("🎯 [LobbyUIController] ShowStageSelectPanel 호출됨 - StageSelectPanelController로 위임");
        
        // 🎯 Phase 6-Pre: Z-Order 방식으로 패널을 최상위로 이동
        BringPanelToFront(stageSelectPanel);
        
        // 🎯 Phase 6-Pre: StageSelectPanelController로 위임
        if (stageSelectPanelController != null)
        {
            // 선택된 캐릭터 슬롯 인덱스 전달
            stageSelectPanelController.SetSelectedSlotIndex(selectedSlotIndex);
            stageSelectPanelController.ShowPanel();
        }
        else
        {
            Debug.LogError("[LobbyUIController] stageSelectPanelController가 null입니다!");
        }
    }
    
    /// <summary>
    /// 🔧 수정: 인벤토리 패널 표시 (컴포넌트 찾기 개선)
    /// </summary>
    public void ShowInventoryPanel()
    {
        if (!EnsureCharacterSelected()) return;
        
        Debug.Log("🎒 [LobbyUIController] ShowInventoryPanel 호출됨 (Z-Order 방식)");
        
        // Cue 이벤트 발행
        var context = new CueContext
        {
            position = Vector3.zero,
            actorType = ActorType.UI
        };
        CueEmitter.Emit("ui.button.click", "UI", context);
        CueEmitter.Emit("ui.inventory.open", "UI", context);
        
        // 🎯 핵심 변경: Z-Order 방식
        BringPanelToFront(inventoryPanel);
        
        // 🔧 수정: 다양한 방법으로 LobbyInventoryUI 컴포넌트 찾기
        var inventoryUI = inventoryPanel.GetComponent<LobbyInventoryUI>();
        if (inventoryUI == null)
        {
            // 하위 오브젝트에서 찾기
            inventoryUI = inventoryPanel.GetComponentInChildren<LobbyInventoryUI>();
        }
        if (inventoryUI == null)
        {
            // 전체에서 찾기 (최후 수단)
            inventoryUI = FindObjectOfType<LobbyInventoryUI>();
        }
        
        if (inventoryUI != null)
        {
            Debug.Log($"🔍 [LobbyUIController] LobbyInventoryUI 컴포넌트 발견: {inventoryUI.gameObject.name}");
            inventoryUI.ForceRefreshWithLobbySelectedCharacter(selectedSlotIndex);
        }
        else
        {
            Debug.LogError("❌ [LobbyUIController] LobbyInventoryUI 컴포넌트를 찾을 수 없습니다!");
        }
        
        Debug.Log($"[LobbyUIController] 인벤토리 패널을 최상위로 이동 완료 (슬롯 {selectedSlotIndex})");
    }
    
    /// <summary>
    /// 🆕 인벤토리 지연 로드 및 표시
    /// </summary>
    private IEnumerator LazyLoadAndShowInventory()
    {
        Debug.Log("🔄 [LobbyUIController] 인벤토리 진입 - 캐릭터 데이터 지연 로드 시작");
        
        // 1. 선택된 캐릭터 데이터 완전 로드
        int selectedSlot = PlayerDataManager.Instance.GetSelectedSlotIndex();
        bool loadSuccess = PlayerDataManager.Instance.LazyLoadSlotData(selectedSlot);
        
        if (!loadSuccess)
        {
            Debug.LogError("❌ [LobbyUIController] 캐릭터 데이터 로드 실패");
            yield break;
        }
        
        // 2. 인벤토리 관련 UI들 갱신 대기
        yield return StartCoroutine(RefreshInventoryRelatedUIs());
        
        // 3. Cue 이벤트 발행
        var context = new CueContext
        {
            position = Vector3.zero,
            actorType = ActorType.UI
        };
        CueEmitter.Emit("ui.button.click", "UI", context);
        CueEmitter.Emit("ui.inventory.open", "UI", context);
        
        // 4. 인벤토리 패널 활성화
        SetPanelVisibility(lobbyPanel, false);
        SetPanelVisibility(stageSelectPanel, false);
        SetPanelVisibility(inventoryPanel, true);
        SetPanelVisibility(shopPanel, false);
        SetPanelVisibility(characterInfoPanel, false);
        
        Debug.Log("[LobbyUIController] 인벤토리 패널 활성화");
        Debug.Log("✅ [LobbyUIController] 인벤토리 진입 완료 (지연 로드)");
    }

    /// <summary>
    /// 🆕 인벤토리 관련 UI 갱신
    /// </summary>
    private IEnumerator RefreshInventoryRelatedUIs()
    {
        Debug.Log("🔄 [LobbyUIController] 인벤토리 관련 UI 갱신 시작");
        
        // LobbyInventoryUI 갱신 대기
        var lobbyInventoryUI = FindObjectOfType<LobbyInventoryUI>();
        if (lobbyInventoryUI != null)
        {
            // 인벤토리 UI 강제 갱신 (필요시)
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log("✅ [LobbyUIController] 인벤토리 관련 UI 갱신 완료");
    }

    /// <summary>
    /// 🆕 캐릭터 정보창 지연 로드 및 표시
    /// </summary>
    private IEnumerator LazyLoadAndShowCharacterInfo()
    {
        Debug.Log("🔄 [LobbyUIController] 캐릭터 정보창 진입 - 캐릭터 데이터 지연 로드 시작");
        
        // 1. 선택된 캐릭터 데이터 완전 로드
        int selectedSlot = PlayerDataManager.Instance.GetSelectedSlotIndex();
        bool loadSuccess = PlayerDataManager.Instance.LazyLoadSlotData(selectedSlot);
        
        if (!loadSuccess)
        {
            Debug.LogError("❌ [LobbyUIController] 캐릭터 데이터 로드 실패");
            yield break;
        }
        
        // 2. 캐릭터 정보 관련 UI들 갱신 대기
        yield return StartCoroutine(RefreshCharacterInfoRelatedUIs());
        
        // 3. Cue 이벤트 발행
        var context = new CueContext
        {
            position = Vector3.zero,
            actorType = ActorType.UI
        };
        CueEmitter.Emit("ui.button.click", "UI", context);
        CueEmitter.Emit("ui.panel.open", "UI", context);
        
        // 4. 캐릭터 정보 패널 활성화
        SetPanelVisibility(lobbyPanel, false);
        SetPanelVisibility(stageSelectPanel, false);
        SetPanelVisibility(inventoryPanel, false);
        SetPanelVisibility(shopPanel, false);
        SetPanelVisibility(characterInfoPanel, true);
        
        Debug.Log("[LobbyUIController] 캐릭터 정보 패널 활성화");
        Debug.Log("✅ [LobbyUIController] 캐릭터 정보창 진입 완료 (지연 로드)");
    }

    /// <summary>
    /// 🆕 캐릭터 정보 관련 UI 갱신
    /// </summary>
    private IEnumerator RefreshCharacterInfoRelatedUIs()
    {
        Debug.Log("🔄 [LobbyUIController] 캐릭터 정보 관련 UI 갱신 시작");
        
        // CharacterInfoUI 갱신 대기
        var characterInfoUI = FindObjectOfType<CharacterInfoUI>();
        if (characterInfoUI != null)
        {
            // 캐릭터 정보 UI 강제 갱신 (필요시)
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log("✅ [LobbyUIController] 캐릭터 정보 관련 UI 갱신 완료");
    }
    
    private void SetPanelVisibility(GameObject panel, bool isVisible)
    {
        if (panel != null)
        {
            //  디버그: 어떤 패널이 언제 변경되는지 확인
            Debug.Log($"�� [LobbyUIController] SetPanelVisibility: {panel.name} → {isVisible}");
            panel.SetActive(isVisible);
        }
    }
    
    #endregion
    
    #region === 기존 호환성 메서드 (Unity Editor 연결용) ===
    
    public void OnLogoutButton()
    {
        Debug.Log("[LobbyUIController] 로그아웃 버튼 클릭!");
    }
    
    // 🗑️ [삭제됨] public void OnCharacterSelectButton()
    // 🗑️ [삭제됨] public void OnGameMapButton()
    // 🗑️ [삭제됨] public void OnEnterBattleButton()
    // 🗑️ [삭제됨] public void OnCharacterImageClick(string characterName)
    
    // Unity Editor에서 직접 연결 가능한 스테이지 선택 메서드
    // 🗑️ [Phase 6-Pre 삭제] OnStage1Button(), OnStage2Button(), OnStage3Button() - StageSelectPanelController로 이동
    
    // 🗑️ [삭제됨] 기존 클래스 선택 메서드들
    // public void OnWarriorButtonClick() { ... }
    // public void OnAssassinButtonClick() { ... }
    // public void OnWizardButtonClick() { ... }

    /// <summary>
    /// 🔄 슬롯 인덱스와 클래스를 직접 연결 (완전 데이터 교체 방식)
    /// </summary>
    private void SelectSlotAndClass(int slotIndex, string className)
    {
        if (PlayerDataManager.Instance != null)
        {
            var slotData = PlayerDataManager.Instance.GetSlotData(slotIndex);
            if (slotData != null && slotData.isSlotUsed)
            {
                // 🔧 SelectSlot 방식: 완전한 데이터 교체
                bool success = PlayerDataManager.Instance.SelectSlot(slotIndex);
                if (success)
                {
                    Debug.Log($"🔄 [LobbyUIController] 슬롯 완전 전환 완료: {className} → 슬롯 {slotIndex}");
                    // OnClassSelected(className); // 🗑️ 제거
                }
                else
                {
                    Debug.LogError($"💥 [LobbyUIController] 슬롯 {slotIndex} 전환 실패");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ [LobbyUIController] 슬롯 {slotIndex}가 비어있습니다.");
            }
        }
    }
    
    /// <summary>
    /// Bag 버튼 클릭 (Unity Editor OnClick 연결용)
    /// </summary>
    // public void OnBagButton()
    // {
    //     Debug.Log("[LobbyUIController] Bag 버튼 클릭!");
    //     ShowInventoryPanel();
    // }
    
    /// <summary>
    /// Shop 버튼 클릭 (Unity Editor OnClick 연결용)
    /// </summary>
    public void OnShopButton()
    {
        Debug.Log("[LobbyUIController] Shop 버튼 클릭!");
        ShowShopPanel();
    }

    /// <summary>
    /// Hero 버튼 클릭 (Unity Editor OnClick 연결용)
    /// </summary>
    public void OnHeroButton()
    {
        Debug.Log("[LobbyUIController] Hero 버튼 클릭!");
        ShowCharacterInfoPanel();
    }
    
    #endregion

    // 🔧 Step 2-2: 슬롯 버튼 이벤트 연결
    private void ConnectSlotButtonEvents()
    {
        for (int i = 0; i < characterSlotButtons.Length; i++)
        {
            if (characterSlotButtons[i] != null)
            {
                int slotIndex = i; // 클로저 문제 방지
                characterSlotButtons[i].onClick.AddListener(() => OnSlotButtonClicked(slotIndex));
            }
        }
        
        // 삭제 버튼들 이벤트 연결
        if (slot0DeleteButton != null) slot0DeleteButton.onClick.AddListener(() => OnDeleteSlotButtonClicked(0));
        if (slot1DeleteButton != null) slot1DeleteButton.onClick.AddListener(() => OnDeleteSlotButtonClicked(1));
        if (slot2DeleteButton != null) slot2DeleteButton.onClick.AddListener(() => OnDeleteSlotButtonClicked(2));
    }
    
    // 🔧 Step 2-2: 모든 슬롯 UI 새로고침
    public void RefreshAllSlots()
    {
        Debug.Log("[LobbyUIController] 모든 슬롯 UI 새로고침");
        
        for (int i = 0; i < 3; i++)
        {
            RefreshSlotUI(i);
        }
    }
    
    // 🔧 Step 2-2: 특정 슬롯 UI 새로고침
    private void RefreshSlotUI(int slotIndex)
    {
        if (PlayerDataManager.Instance == null) return;
        
        var slotData = PlayerDataManager.Instance.GetSlotData(slotIndex);
        
        // UI 요소들 가져오기
        var characterIcon = GetSlotCharacterIcon(slotIndex);
        var characterName = GetSlotCharacterName(slotIndex);
        var characterLevel = GetSlotCharacterLevel(slotIndex);
        var characterClass = GetSlotCharacterClass(slotIndex);
        var deleteButton = GetSlotDeleteButton(slotIndex);
        var emptyPanel = GetSlotEmptyPanel(slotIndex);
        
        if (slotData != null && slotData.isSlotUsed)
        {
            // 캐릭터 있음 - 캐릭터 정보 표시
            ShowCharacterSlot(slotIndex, slotData, characterIcon, characterName, characterLevel, characterClass, deleteButton, emptyPanel);
        }
        else
        {
            // 빈 슬롯 - 빈 슬롯 UI 표시
            ShowEmptySlot(slotIndex, characterIcon, characterName, characterLevel, characterClass, deleteButton, emptyPanel);
        }
    }
    
    // 🔧 Step 2-2: 캐릭터 있는 슬롯 표시
    private void ShowCharacterSlot(int slotIndex, PlayerSlotData slotData, Image icon, TMP_Text name, TMP_Text level, TMP_Text playerClass, Button deleteBtn, GameObject emptyPanel)
    {
        // 아이콘 설정
        if (icon != null)
        {
            icon.sprite = GetClassIcon(slotData.playerType);
            icon.gameObject.SetActive(true);
        }
        
        // 텍스트 설정
        if (name != null)
        {
            name.text = slotData.playerName;
            name.gameObject.SetActive(true);
        }
        
        if (level != null)
        {
            level.text = $"Lv.{slotData.level}";
            level.gameObject.SetActive(true);
        }
        
        if (playerClass != null)
        {
            playerClass.text = slotData.playerType.ToString();
            playerClass.gameObject.SetActive(true);
        }
        
        // 🆕 Button의 Selected 상태 활용 (하드코딩 제거)
        Button slotButton = GetSlotButton(slotIndex);
        if (slotButton != null)
        {
            bool isSelected = (selectedSlotIndex == slotIndex);
            
            if (isSelected)
            {
                // Unity Button의 Select() 메서드 사용
                slotButton.Select();
                Debug.Log($"[LobbyUIController] 슬롯 {slotIndex} Button.Select() 적용");
            }
            else
            {
                // 선택 해제 (다른 버튼이 선택되면 자동으로 해제됨)
                if (slotButton == UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.GetComponent<Button>())
                {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                }
            }
        }
        
        // 삭제 버튼 활성화
        if (deleteBtn != null)
        {
            deleteBtn.gameObject.SetActive(true);
        }
        
        // 빈 슬롯 패널 비활성화
        if (emptyPanel != null)
        {
            emptyPanel.SetActive(false);
        }
        
        Debug.Log($"[LobbyUIController] 슬롯 {slotIndex} 캐릭터 표시: {slotData.playerName} (Lv.{slotData.level}, {slotData.playerType})");
    }
    
    // 🔧 Step 2-2: 빈 슬롯 표시
    private void ShowEmptySlot(int slotIndex, Image icon, TMP_Text name, TMP_Text level, TMP_Text playerClass, Button deleteBtn, GameObject emptyPanel)
    {
        // 🆕 빈 슬롯은 선택 해제
        Button slotButton = GetSlotButton(slotIndex);
        if (slotButton != null && slotButton == UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.GetComponent<Button>())
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
        
        // 아이콘 설정 (빈 슬롯 아이콘)
        if (icon != null)
        {
            icon.sprite = emptySlotIcon;
            icon.gameObject.SetActive(true);
        }
        
        // 텍스트 비활성화
        if (name != null) name.gameObject.SetActive(false);
        if (level != null) level.gameObject.SetActive(false);
        if (playerClass != null) playerClass.gameObject.SetActive(false);
        
        // 삭제 버튼 비활성화
        if (deleteBtn != null)
        {
            deleteBtn.gameObject.SetActive(false);
        }
        
        // 빈 슬롯 패널 활성화
        if (emptyPanel != null)
        {
            emptyPanel.SetActive(true);
        }
        
        Debug.Log($"[LobbyUIController] 슬롯 {slotIndex} 빈 슬롯 표시");
    }
    
    // 🔧 Step 2-2: 클래스별 아이콘 가져오기
    private Sprite GetClassIcon(PlayerType playerType)
    {
        switch (playerType)
        {
            case PlayerType.Warrior: return warriorIcon;
            case PlayerType.Assasin: return assassinIcon;
            case PlayerType.Wizard: return wizardIcon;
            default: return emptySlotIcon;
        }
    }
    
    // 🔧 Step 2-2: 슬롯별 UI 요소 가져오기 헬퍼 메서드들
    private Image GetSlotCharacterIcon(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return slot0CharacterIcon;
            case 1: return slot1CharacterIcon;
            case 2: return slot2CharacterIcon;
            default: return null;
        }
    }
    
    // 🆕 슬롯 버튼 가져오기 (하이라이트용)
    private Button GetSlotButton(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < characterSlotButtons.Length)
        {
            return characterSlotButtons[slotIndex];
        }
        return null;
    }
    
    private TMP_Text GetSlotCharacterName(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return slot0CharacterName;
            case 1: return slot1CharacterName;
            case 2: return slot2CharacterName;
            default: return null;
        }
    }
    
    private TMP_Text GetSlotCharacterLevel(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return slot0CharacterLevel;
            case 1: return slot1CharacterLevel;
            case 2: return slot2CharacterLevel;
            default: return null;
        }
    }
    
    private TMP_Text GetSlotCharacterClass(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return slot0CharacterClass;
            case 1: return slot1CharacterClass;
            case 2: return slot2CharacterClass;
            default: return null;
        }
    }
    
    private Button GetSlotDeleteButton(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return slot0DeleteButton;
            case 1: return slot1DeleteButton;
            case 2: return slot2DeleteButton;
            default: return null;
        }
    }
    
    private GameObject GetSlotEmptyPanel(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return slot0EmptyPanel;
            case 1: return slot1EmptyPanel;
            case 2: return slot2EmptyPanel;
            default: return null;
        }
    }
    
    // 🔧 Step 2-2: 슬롯 버튼 클릭 이벤트
    public void OnSlotButtonClicked(int slotIndex)
    {
        Debug.Log($"[LobbyUIController] 슬롯 {slotIndex} 버튼 클릭");
        
        if (PlayerDataManager.Instance == null) return;
        
        var slotData = PlayerDataManager.Instance.GetSlotData(slotIndex);
        
        if (slotData != null && slotData.isSlotUsed)
        {
            // 캐릭터 있음 - 캐릭터 선택
            SelectCharacterSlot(slotIndex, slotData);
        }
        else
        {
            // 빈 슬롯 - 캐릭터 생성
            StartCharacterCreation(slotIndex);
        }
    }
    
    // 🔧 Step 2-2: 캐릭터 선택 (기존 캐릭터)
    // 🔧 Step 2-4: 캐릭터 선택 시 스테이지 선택으로 바로 이동하지 않고 대기
    private void SelectCharacterSlot(int slotIndex, PlayerSlotData slotData)
    {
        Debug.Log($"[LobbyUIController] 캐릭터 선택: 슬롯 {slotIndex}, {slotData.playerName} (Lv.{slotData.level}, {slotData.playerType})");
        
        selectedSlotIndex = slotIndex;
        
        // 🔧 지연 갱신: PlayerDataManager에 슬롯 ID만 저장 (UI 갱신 없음)
        PlayerDataManager.Instance.SetSelectedSlotIndex(slotIndex);
        
        // 🔧 최소 UI 업데이트 (로비에서 보이는 기본 정보만)
        UpdateSelectedCharacterInfo(slotData);
        EnableStartGameButton(true);
        
        // 🔧 StageProgressManager는 유지 (로비 스테이지 선택 UI에 필요)
        if (StageProgressManager.Instance != null)
        {
            StageProgressManager.Instance.InitializeFor(slotIndex);
            Debug.Log($"🔄 [LobbyUIController] 캐릭터 전환: 슬롯 {slotIndex} StageProgressManager 업데이트");
        }
        
        // 🆕 Phase 6-Pre: StageSelectPanelController 초기화
        stageSelectPanelController?.RefreshStageProgressUI();
        
        Debug.Log($"[LobbyUIController] 캐릭터 선택 완료 (지연 갱신 모드) - 패널 진입 시 완전 로드됨");
    }
    
    // 🔧 Step 2-2: 캐릭터 생성 시작 (빈 슬롯)
    private void StartCharacterCreation(int slotIndex)
    {
        Debug.Log($"[LobbyUIController] 🎯 캐릭터 생성 시작: 슬롯 {slotIndex}");
        
        // 🔍 디버그: CharacterCreationController 참조 상태
        if (characterCreationController == null)
        {
            Debug.LogError("[LobbyUIController] ❌ CharacterCreationController 참조가 null입니다!");
            
            // 자동 검색 시도
            characterCreationController = FindObjectOfType<CharacterCreationController>();
            if (characterCreationController != null)
            {
                Debug.LogWarning("[LobbyUIController] ⚠️ CharacterCreationController를 자동으로 찾았습니다. Inspector에서 연결해주세요.");
            }
            else
            {
                Debug.LogError("[LobbyUIController] ❌ CharacterCreationController를 찾을 수 없습니다!");
                return;
            }
        }
        
        Debug.Log($"[LobbyUIController] ✅ CharacterCreationController 참조 정상, StartCharacterCreation() 호출 중...");
        characterCreationController.StartCharacterCreation(slotIndex);
        Debug.Log($"[LobbyUIController] ✅ CharacterCreationController.StartCharacterCreation() 호출 완료");
    }
    
    // 🔧 Step 2-2: 선택된 캐릭터 정보 업데이트
    private void UpdateSelectedCharacterInfo(PlayerSlotData slotData)
    {
        // 🔧 수정: 간단한 형식으로 변경
        if (selectedPlayerNameText != null)
        {
            selectedPlayerNameText.text = $"{slotData.playerName} ({slotData.playerType})";
        }
        
        // 🗑️ [Phase 6-Pre 삭제] stageSelectTitleText 업데이트는 StageSelectPanelController에서 처리
        
        // ✅ 유지: LobbyPlayerInfoUI 업데이트
        var lobbyPlayerInfoUI = FindObjectOfType<LobbyPlayerInfoUI>();
        if (lobbyPlayerInfoUI != null)
        {
            lobbyPlayerInfoUI.UpdatePlayerInfoFromSlotData(slotData);
        }
    }
    
    // 🔧 Step 2-2: 캐릭터 삭제 버튼 클릭
    public void OnDeleteSlotButtonClicked(int slotIndex)
    {
        Debug.Log($"[LobbyUIController] 슬롯 {slotIndex} 삭제 버튼 클릭");
        
        if (PlayerDataManager.Instance == null) return;
        
        var slotData = PlayerDataManager.Instance.GetSlotData(slotIndex);
        if (slotData != null && slotData.isSlotUsed)
        {
            // 🆕 확인 팝업 표시
            ShowDeleteConfirmationPopup(slotIndex, slotData);
        }
    }
    
    /// <summary>
    /// 🆕 캐릭터 삭제 확인 팝업 표시
    /// </summary>
    private void ShowDeleteConfirmationPopup(int slotIndex, PlayerSlotData slotData)
    {
        Debug.Log($"[LobbyUIController] 캐릭터 삭제 확인 팝업 표시: 슬롯 {slotIndex}");
        
        // 🔍 디버그: ConfirmationPopup 참조 상태
        if (confirmationPopup == null)
        {
            Debug.LogError("[LobbyUIController] ❌ ConfirmationPopup 참조가 null입니다! Inspector에서 연결해주세요.");
            Debug.LogWarning("[LobbyUIController] ⚠️ 팝업 없이 바로 삭제합니다.");
            
            // Fallback: 바로 삭제
            DeleteCharacterSlot(slotIndex, slotData);
            return;
        }
        
        // 팝업 메시지 구성
        string title = "캐릭터 삭제";
        string message = "정말 삭제하시겠습니까?";
        string detail = $"{slotData.playerName} (Lv.{slotData.level}, {slotData.playerType})";
        
        // 확인 팝업 표시
        confirmationPopup.Show(
            title,
            message,
            detail,
            onConfirm: () => {
                Debug.Log($"[LobbyUIController] 사용자가 삭제를 확인했습니다.");
                DeleteCharacterSlot(slotIndex, slotData);
                ShowLobbyPanel();  // 🆕 삭제 후 로비로 복귀
            },
            onCancel: () => {
                Debug.Log($"[LobbyUIController] 사용자가 삭제를 취소했습니다.");
                ShowLobbyPanel();  // 🆕 취소 시 로비로 복귀
            }
        );
    }
    
    // 🔧 수정: 캐릭터 삭제 실행 (모든 UI 갱신 포함)
    private void DeleteCharacterSlot(int slotIndex, PlayerSlotData slotData)
    {
        Debug.Log($"[LobbyUIController] 캐릭터 삭제 실행: 슬롯 {slotIndex}, {slotData.playerName}");
        
        bool success = PlayerDataManager.Instance.DeleteSlot(slotIndex);
        
        if (success)
        {
            Debug.Log($"[LobbyUIController] ✅ 캐릭터 삭제 성공: {slotData.playerName}");
            
            // 삭제된 슬롯이 현재 선택된 슬롯이라면 다른 슬롯 자동 선택
            if (selectedSlotIndex == slotIndex)
            {
                int newSelectedSlot = FindNextValidSlot(slotIndex);
                if (newSelectedSlot >= 0)
                {
                    var newSlotData = PlayerDataManager.Instance.GetSlotData(newSelectedSlot);
                    SelectCharacterSlot(newSelectedSlot, newSlotData);
                }
                else
                {
                    selectedSlotIndex = -1;
                    EnableStartGameButton(false);
                }
            }
            
            // 🎯 로비 슬롯 UI 새로고침
            RefreshSlotUI(slotIndex);
            
            // 🆕 CharacterInfoUI도 갱신 (활성화된 경우)
            var characterInfoUI = characterInfoPanel.GetComponent<CharacterInfoUI>();
            if (characterInfoUI != null && characterInfoPanel.activeInHierarchy)
            {
                characterInfoUI.ForceRefreshWithCurrentSlot(selectedSlotIndex);
            }
            
            // 🆕 LobbyEquippedItemsUI도 직접 갱신 (활성화된 경우)
            var equippedItemsUI = characterInfoPanel.GetComponentInChildren<LobbyEquippedItemsUI>();
            if (equippedItemsUI != null && characterInfoPanel.activeInHierarchy)
            {
                equippedItemsUI.ForceRefreshEquippedItems();
            }
            
            Debug.Log($"[LobbyUIController] 캐릭터 삭제 후 모든 UI 갱신 완료");
        }
        else
        {
            Debug.LogError($"[LobbyUIController] ❌ 캐릭터 삭제 실패: {slotData.playerName}");
        }
    }

    /// <summary>
    /// 🆕 삭제된 슬롯 다음으로 유효한 슬롯 찾기
    /// </summary>
    private int FindNextValidSlot(int deletedSlot)
    {
        // 다음 슬롯부터 검색
        for (int i = 0; i < 3; i++)
        {
            if (i != deletedSlot)
            {
                var slotData = PlayerDataManager.Instance.GetSlotData(i);
                if (slotData != null && slotData.isSlotUsed)
                {
                    return i;
                }
            }
        }
        return -1; // 유효한 슬롯 없음
    }

    // 🆕 게임 시작 버튼 활성화/비활성화
    private void EnableStartGameButton(bool enable)
    {
        if (startGameButton != null)
        {
            startGameButton.interactable = enable;
            
            // 🗑️ [Phase 6-Pre 삭제] colorReferenceButton 사용 제거 - interactable로 충분
        }
    }
    
    // 🆕 게임 시작 버튼 클릭 이벤트
    public void OnStartGameButtonClicked()
    {
        if (selectedSlotIndex == -1)
        {
            Debug.LogWarning("[LobbyUIController] 슬롯이 선택되지 않음");
            return;
        }
        
        // 🆕 추가: 게임 시작 전 데이터 유효성 검증
        if (PlayerDataManager.Instance != null && !PlayerDataManager.Instance.ValidateSelectedPlayerData())
        {
            Debug.LogError("[LobbyUIController] SelectedPlayerData가 유효하지 않음 - 게임 시작 취소");
            
            // UI에서도 선택 해제
            selectedSlotIndex = -1;
            RefreshAllSlots(); // 🔧 슬롯 새로고침 추가
            return;
        }
        
        var slotData = PlayerDataManager.Instance.GetSlotData(selectedSlotIndex);
        if (slotData == null || !slotData.isSlotUsed)
        {
            Debug.LogWarning("[LobbyUIController] 유효하지 않은 슬롯");
            return;
        }
        
        // 선택된 슬롯으로 PlayerDataManager 설정
        bool success = PlayerDataManager.Instance.SelectSlot(selectedSlotIndex);
        if (!success)
        {
            Debug.LogError("[LobbyUIController] 슬롯 선택 실패");
            return;
        }
        
        Debug.Log($"[LobbyUIController] 게임 시작: {slotData.playerName}({slotData.playerType})");
        
        // 스테이지 선택 UI 활성화
        ShowStageSelectPanel();
    }

    // 🆕 게임 종료 버튼 클릭 이벤트
    public void OnQuitGameButtonClicked()
    {
        Debug.Log("[LobbyUIController] 게임 종료 요청 - 회사 로고 페이드 효과 시작");
        
        // 현재 데이터 저장
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
        {
            PlayerDataManager.Instance.SaveCurrentSlot();
            Debug.Log("[LobbyUIController] 게임 종료 전 데이터 저장 완료");
        }
        
        // 🆕 회사 로고 페이드 효과 시작
        if (companyLogoImage != null)
        {
            StartCoroutine(ShowCompanyLogoAndQuit());
        }
        else
        {
            Debug.LogWarning("[LobbyUIController] CompanyLogoImage가 할당되지 않음 - 즉시 종료");
            QuitGameDirectly();
        }
    }
    
    // 🆕 회사 로고 페이드 효과 코루틴 (페이드아웃 제거 버전)
    private IEnumerator ShowCompanyLogoAndQuit()
    {
        Debug.Log("[LobbyUIController] 회사 로고 표시 및 종료 시작");
        
        // 🎯 0단계: CompanyLogoImage를 최상위로 이동 (모든 UI 위에 표시)
        if (companyLogoImage != null)
        {
            companyLogoImage.transform.SetAsLastSibling();
            Debug.Log($"[LobbyUIController] 🔝 CompanyLogoImage를 최상위로 이동 (Sibling Index: {companyLogoImage.transform.GetSiblingIndex()})");
        }
        
        // 초기 설정: 알파값 0으로 시작
        Color logoColor = companyLogoImage.color;
        logoColor.a = 0f;
        companyLogoImage.color = logoColor;
        
        // 1단계: 페이드인 (1초)
        Debug.Log("[LobbyUIController] 페이드인 시작");
        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeInDuration);
            logoColor.a = alpha;
            companyLogoImage.color = logoColor;
            yield return null;
        }
        
        // 완전히 불투명하게 설정
        logoColor.a = 1f;
        companyLogoImage.color = logoColor;
        Debug.Log("[LobbyUIController] 페이드인 완료 - 로고 표시 중");
        
        // 2단계: 로고 표시 유지 (2초)
        yield return new WaitForSeconds(displayDuration);
        
        // 🎯 3단계: 페이드아웃 제거 - 로고가 보이는 상태에서 바로 종료!
        Debug.Log("[LobbyUIController] 로고 표시 완료 - 게임 종료 (페이드아웃 없음)");
        
        // 게임 종료
        QuitGameDirectly();
    }
    
    // 🆕 실제 게임 종료 처리
    private void QuitGameDirectly()
    {
        // GameManager를 통한 게임 종료
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
        else
        {
            // 직접 종료
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }

    // 🆕 스테이지 해금 이벤트 처리
    // 🗑️ [Phase 6-Pre 삭제] OnStageUnlocked(), OnStageCompleted(), UpdateStageSelectionUI(), UpdateStageButtonVisual() - StageSelectPanelController로 이동
    
    #region 🎯 로비 자동 선택 시스템
    
    /// <summary>
    /// 필요 시 자동 슬롯 선택 (핵심 로직)
    /// </summary>
    private void AutoSelectSlotIfNeeded()
    {
        // 이미 선택되어 있으면 패스
        if (PlayerDataManager.Instance.IsSlotSelected) return;
        
        // 🛡️ 0명 케이스: 즉시 온보딩
        if (!PlayerDataManager.Instance.HasAnyCharacter())
        {
            Debug.Log("[Lobby] No characters → Onboarding");
            StartCharacterCreationOnboarding();
            return;
        }
        
        // 우선순위 선택
        int targetSlot = GetAutoSelectSlot();
        if (targetSlot >= 0)
        {
            PlayerDataManager.Instance.SelectSlot(targetSlot);
            
            // 🆕 UI selectedSlotIndex 업데이트
            selectedSlotIndex = targetSlot;
            
            // 🆕 자동 선택 시에도 StageProgressManager 업데이트 (핵심 추가)
            if (StageProgressManager.Instance != null)
            {
                StageProgressManager.Instance.InitializeFor(targetSlot);
                Debug.Log($"🔄 [LobbyUIController] 자동 선택: 슬롯 {targetSlot} StageProgressManager 업데이트");
            }
            
            // 🆕 선택된 캐릭터 정보 UI 업데이트
            var slotData = PlayerDataManager.Instance.GetSlotData(targetSlot);
            if (slotData != null)
            {
                UpdateSelectedCharacterInfo(slotData);
                EnableStartGameButton(true);
            }
            
            string reason = GetSelectionReason(targetSlot);
            Debug.Log($"[Lobby] AutoSelect → Slot {targetSlot} (reason: {reason})");
        }
        else
        {
            Debug.LogError("[Lobby] AutoSelect failed - no valid slots");
        }
    }
    
    /// <summary>
    /// 자동 선택할 슬롯 결정 (우선순위 로직)
    /// </summary>
    private int GetAutoSelectSlot()
    {
        // 1순위: 마지막 선택 슬롯 (유효한 경우)
        int lastSlot = PlayerDataManager.Instance.GetLastSelectedSlotIndex();
        if (PlayerDataManager.Instance.IsSlotValid(lastSlot)) return lastSlot;
        
        // 2순위: 첫 번째 유효 슬롯
        for (int i = 0; i < 3; i++)
        {
            if (PlayerDataManager.Instance.IsSlotValid(i)) return i;
        }
        
        // 여기 도달하면 안 됨 (HasAnyCharacter에서 걸러짐)
        return -1;
    }
    
    /// <summary>
    /// 선택 이유 반환 (로그용)
    /// </summary>
    private string GetSelectionReason(int selectedSlot)
    {
        int lastSlot = PlayerDataManager.Instance.GetLastSelectedSlotIndex();
        if (selectedSlot == lastSlot && PlayerDataManager.Instance.IsSlotValid(lastSlot))
            return "last";
        else
            return "first";
    }
    
    /// <summary>
    /// 캐릭터 생성 온보딩 시작
    /// </summary>
    private void StartCharacterCreationOnboarding()
    {
        Debug.Log("[LobbyUI] 📝 캐릭터 생성 온보딩 시작: Slot 0");
        
        // 🔍 디버그: PlayerDataManager 상태 확인
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("[LobbyUI] ❌ PlayerDataManager.Instance가 null입니다!");
            return;
        }
        
        var slotData = PlayerDataManager.Instance.GetSlotData(0);
        if (slotData == null)
        {
            Debug.LogError("[LobbyUI] ❌ 슬롯 0 데이터가 null입니다!");
            return;
        }
        
        Debug.Log($"[LobbyUI] 🔍 슬롯 0 상태: isSlotUsed={slotData.isSlotUsed}, playerName={slotData.playerName}");
        
        // 첫 번째 빈 슬롯으로 캐릭터 생성 시작
        Debug.Log("[LobbyUI] 🚀 StartCharacterCreation(0) 호출");
        StartCharacterCreation(0);
        Debug.Log("[LobbyUI] ✅ StartCharacterCreation(0) 호출 완료");
    }
    
    /// <summary>
    /// 🆕 캐릭터 선택 상태 보장 (3단계: 패널 가드)
    /// </summary>
    private bool EnsureCharacterSelected()
    {
        if (PlayerDataManager.Instance.IsSlotSelected) return true;
        
        Debug.LogWarning("[Panel] Character not selected - attempting auto-recovery");
        
        // 조용히 자동 선택 시도
        AutoSelectSlotIfNeeded();
        
        // 여전히 실패하면 온보딩
        if (!PlayerDataManager.Instance.IsSlotSelected)
        {
            Debug.Log("[Panel] Recovery failed → Onboarding");
            StartCharacterCreationOnboarding();
            return false;
        }
        
        Debug.Log($"[Panel] Auto-recovered → Slot {selectedSlotIndex}");
        return true;
    }
    
    #endregion

    /// <summary>
    /// 🆕 캐릭터 생성 완료 후 처리 (PlayerDataManager에서 호출)
    /// </summary>
    public void OnCharacterCreationCompleted(int slotIndex)
    {
        Debug.Log($"[LobbyUIController] 캐릭터 생성 완료: 슬롯 {slotIndex}");
        
        // 🆕 안전성 검사 추가
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("[LobbyUIController] PlayerDataManager.Instance가 null - 처리 중단");
            return;
        }
        
        var newSlotData = PlayerDataManager.Instance.GetSlotData(slotIndex);
        if (newSlotData == null || !newSlotData.isSlotUsed)
        {
            Debug.LogError($"[LobbyUIController] 슬롯 {slotIndex} 데이터가 유효하지 않음 - 처리 중단");
            return;
        }
        
        // 🆕 UI selectedSlotIndex 즉시 업데이트
        selectedSlotIndex = slotIndex;
        
        // 슬롯 UI 새로고침
        RefreshAllSlots();
        
        // 🆕 신규 생성된 캐릭터 정보 UI 업데이트
        UpdateSelectedCharacterInfo(newSlotData);
        EnableStartGameButton(true);
        Debug.Log($"[Lobby] New character selected → Slot {slotIndex}");
        
        // 🆕 StageProgressManager 강제 재초기화 (확실히 하기 위해)
        if (StageProgressManager.Instance != null)
        {
            StageProgressManager.Instance.InitializeFor(slotIndex);
        }
        
        // 🆕 Phase 6-Pre: StageSelectPanelController 초기화
        stageSelectPanelController?.RefreshStageProgressUI();
        
        Debug.Log($"[LobbyUIController] 신규 캐릭터 {slotIndex} 완전 설정 완료");
    }

    // 🔍 매니저 초기화 상태 확인 메서드 추가
    private void CheckManagerInitializationStatus()
    {
        Debug.Log("🔍 [LobbyUIController] 매니저 초기화 상태 확인:");
        
        // GamePoolManager 상태
        if (GamePoolManager.Instance != null)
        {
            Debug.Log($"   - GamePoolManager: ✅ 존재, 로딩 중: {GamePoolManager.Instance.IsLoadingPools}");
        }
        else
        {
            Debug.LogError("   - GamePoolManager: ❌ 없음");
        }
        
        // SoundManager 상태
        if (SoundManager.Instance != null)
        {
            Debug.Log("   - SoundManager: ✅ 존재");
        }
        else
        {
            Debug.LogError("   - SoundManager: ❌ 없음");
        }
        
        // CuePlayer 상태 (CueSystem)
        if (CueSystem.CuePlayer.Instance != null)
        {
            Debug.Log("   - CuePlayer: ✅ 존재");
        }
        else
        {
            Debug.LogError("   - CuePlayer: ❌ 없음");
        }
        
        // CueRegistry 상태
        if (CueSystem.CueRegistry.Instance != null)
        {
            Debug.Log("   - CueRegistry: ✅ 존재");
        }
        else
        {
            Debug.LogError("   - CueRegistry: ❌ 없음");
        }
        
        // StageProgressManager 상태
        if (StageProgressManager.Instance != null)
        {
            Debug.Log($"   - StageProgressManager: ✅ 존재, 초기화됨: {StageProgressManager.Instance.IsInitialized}");
        }
        else
        {
            Debug.LogError("   - StageProgressManager: ❌ 없음");
        }
        
        // PlayerDataManager 상태
        if (PlayerDataManager.Instance != null)
        {
            Debug.Log($"   - PlayerDataManager: ✅ 존재, 슬롯 선택됨: {PlayerDataManager.Instance.IsSlotSelected}");
        }
        else
        {
            Debug.LogError("   - PlayerDataManager: ❌ 없음");
        }
    }

    // 🔍 매니저 초기화 완료 대기 메서드
    private IEnumerator WaitForManagersInitialization(System.Action onComplete)
    {
        Debug.Log("⏳ [LobbyUIController] 매니저 초기화 완료 대기 중...");
        
        float timeout = 5f; // 5초 타임아웃
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            bool allReady = true;
            
            // GamePoolManager 체크
            if (GamePoolManager.Instance == null || GamePoolManager.Instance.IsLoadingPools)
            {
                allReady = false;
            }
            
            // SoundManager 체크
            if (SoundManager.Instance == null)
            {
                allReady = false;
            }
            
            // CuePlayer 체크
            if (CueSystem.CuePlayer.Instance == null)
            {
                allReady = false;
            }
            
            if (allReady)
            {
                Debug.Log("✅ [LobbyUIController] 모든 매니저 초기화 완료!");
                onComplete?.Invoke();
                yield break;
            }
            
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.LogWarning("⚠️ [LobbyUIController] 매니저 초기화 대기 타임아웃!");
        onComplete?.Invoke(); // 타임아웃이어도 실행
    }

    /// <summary>
    /// 🔧 수정: 캐릭터 정보창 패널 표시 (Z-Order 방식)
    /// </summary>
    public void ShowCharacterInfoPanel()
    {
        if (!EnsureCharacterSelected()) return;
        
        Debug.Log("👤 [LobbyUIController] ShowCharacterInfoPanel 호출됨 (Z-Order 방식)");
        
        // Cue 이벤트 발행
        var context = new CueContext
        {
            position = Vector3.zero,
            actorType = ActorType.UI
        };
        CueEmitter.Emit("ui.button.click", "UI", context);
        CueEmitter.Emit("ui.character.open", "UI", context);
        
        // 🎯 핵심 변경: Z-Order 방식
        BringPanelToFront(characterInfoPanel);
        
        // �� 현재 선택된 캐릭터로 강제 갱신
        var characterInfoUI = characterInfoPanel.GetComponent<CharacterInfoUI>();
        if (characterInfoUI != null)
        {
            characterInfoUI.ForceRefreshWithCurrentSlot(selectedSlotIndex);
        }
        
        Debug.Log($"[LobbyUIController] 캐릭터 정보 패널을 최상위로 이동 완료 (슬롯 {selectedSlotIndex})");
    }

    /// <summary>
    /// 🆕 패널을 최상위로 가져오기 (Z-Order 제어)
    /// </summary>
    private void BringPanelToFront(GameObject panel)
    {
        if (panel != null)
        {
            panel.transform.SetAsLastSibling();
            Debug.Log($"🔝 [LobbyUIController] {panel.name} 패널을 최상위로 이동");
        }
    }

    /// <summary>
    /// 🆕 백그라운드에서 모든 패널 초기화 (단순화)
    /// </summary>
    private IEnumerator InitializeAllPanelsInBackground()
    {
        Debug.Log("🔄 [LobbyUIController] 백그라운드 패널 초기화 시작");
        
        // 매니저 초기화 대기
        yield return StartCoroutine(WaitForManagersInitialization(() => {}));
        
        // 캐릭터 선택 대기
        while (!PlayerDataManager.Instance.IsSlotSelected)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // 🎯 상점 초기화 (활성화 상태에서)
        if (ShopInventoryManager.Instance != null)
        {
            ShopInventoryManager.Instance.LoadItemsForShop();
        }
        
        var shopUIController = shopPanel.GetComponent<ShopUIController>();
        if (shopUIController != null)
        {
            shopUIController.OnShopOpened();
        }
        
        // 🎯 인벤토리 초기화 (활성화 상태에서)
        var inventoryUI = inventoryPanel.GetComponent<LobbyInventoryUI>();
        if (inventoryUI != null)
        {
            inventoryUI.ForceRefreshInventory();
        }
        
        // 🎯 캐릭터 정보 초기화 (활성화 상태에서)
        var characterInfoUI = characterInfoPanel.GetComponent<CharacterInfoUI>();
        if (characterInfoUI != null)
        {
            // 캐릭터 정보 UI 초기화 (필요시)
        }
        
        Debug.Log("✅ [LobbyUIController] 백그라운드 패널 초기화 완료");
    }

    /// <summary>
    /// 🆕 캐릭터 선택 UI 갱신 (Button Selected Color 복원)
    /// </summary>
    private void RefreshCharacterSelectionUI()
    {
        if (PlayerDataManager.Instance == null) return;
        
        // 현재 선택된 슬롯 인덱스 가져오기
        int currentSelectedSlot = PlayerDataManager.Instance.GetSelectedSlotIndex();
        
        // LobbyUIController의 selectedSlotIndex와 동기화
        if (selectedSlotIndex != currentSelectedSlot)
        {
            selectedSlotIndex = currentSelectedSlot;
            Debug.Log($"🔄 [LobbyUIController] 선택된 슬롯 동기화: {currentSelectedSlot}");
        }
        
        // 모든 슬롯 버튼의 선택 상태 갱신
        for (int i = 0; i < characterSlotButtons.Length; i++)
        {
            Button slotButton = GetSlotButton(i);
            if (slotButton != null)
            {
                bool isSelected = (selectedSlotIndex == i);
                
                if (isSelected)
                {
                    // 🎯 핵심: Unity Button의 Select() 메서드로 Selected Color 적용
                    slotButton.Select();
                    Debug.Log($"✅ [LobbyUIController] 슬롯 {i} Button Selected Color 적용");
                }
                else
                {
                    // 선택 해제 (EventSystem에서 현재 선택된 객체가 이 버튼이면 해제)
                    if (slotButton == UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.GetComponent<Button>())
                    {
                        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                    }
                }
            }
        }
        
        // 선택된 캐릭터 정보도 갱신
        if (selectedSlotIndex >= 0)
        {
            var selectedSlotData = PlayerDataManager.Instance.GetSlotData(selectedSlotIndex);
            if (selectedSlotData != null && selectedSlotData.isSlotUsed)
            {
                UpdateSelectedCharacterInfo(selectedSlotData);
                EnableStartGameButton(true);
                Debug.Log($"🎯 [LobbyUIController] 선택된 캐릭터 정보 갱신: {selectedSlotData.playerName}");
            }
        }
        
        Debug.Log($"✅ [LobbyUIController] 캐릭터 선택 UI 갱신 완료 (선택된 슬롯: {selectedSlotIndex})");
    }

    // 🆕 배경 오버레이 검증 (Start 메서드 끝부분에 추가)
    private void ValidateBackgroundOverlay()
    {
        if (backgroundOverlay != null)
        {
            // 오버레이가 최하위 레이어(Index 0)에 있는지 확인
            int siblingIndex = backgroundOverlay.transform.GetSiblingIndex();
            if (siblingIndex != 0)
            {
                backgroundOverlay.transform.SetAsFirstSibling();
                Debug.Log($"🔧 [LobbyUIController] BackgroundOverlay를 최하위 레이어로 이동 (Index: {siblingIndex} → 0)");
            }
            
            // Image 컴포넌트 Raycast Target 확인
            var overlayImage = backgroundOverlay.GetComponent<UnityEngine.UI.Image>();
            if (overlayImage != null && !overlayImage.raycastTarget)
            {
                Debug.LogWarning("⚠️ [LobbyUIController] BackgroundOverlay Image의 Raycast Target이 비활성화되어 있습니다!");
            }
            
            // BackgroundOverlayHandler 확인
            var handler = backgroundOverlay.GetComponent<BackgroundOverlayHandler>();
            if (handler == null)
            {
                Debug.LogWarning("⚠️ [LobbyUIController] BackgroundOverlay에 BackgroundOverlayHandler가 없습니다!");
            }
            
            Debug.Log("✅ [LobbyUIController] BackgroundOverlay 검증 완료");
        }
        else
        {
            Debug.LogWarning("⚠️ [LobbyUIController] backgroundOverlay가 할당되지 않았습니다!");
        }
    }

    /// <summary>
    /// 🔄 현재 선택된 캐릭터의 Button Selection 복원 (외부 호출용)
    /// </summary>
    public void RestoreCharacterSelection()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < characterSlotButtons.Length)
        {
            Button targetButton = characterSlotButtons[selectedSlotIndex];
            if (targetButton != null)
            {
                targetButton.Select();
                Debug.Log($"🔄 [LobbyUIController] 캐릭터 선택 복원: 슬롯 {selectedSlotIndex}");
            }
        }
    }
    
    #region 🎬 Phase 5: 챕터 종료 처리
    
    /// <summary>
    /// 🎬 Phase 5: 챕터 종료 컷신 체크 (로비 진입 후)
    /// </summary>
    private IEnumerator CheckPendingChapterClearCutscene()
    {
        // 로비 UI 초기화 대기
        yield return new WaitForSeconds(0.5f);
        
        if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
        {
            Debug.Log("[LobbyUIController] 슬롯이 선택되지 않음 - 챕터 종료 컷신 체크 스킵");
            yield break;
        }
        
        // 🎬 Phase 5: SelectedPlayerData에서 pendingCutsceneId 확인
        var selectedData = PlayerDataManager.Instance.selectedPlayerData;
        if (selectedData == null)
        {
            Debug.LogWarning("[LobbyUIController] SelectedPlayerData가 없음 - 챕터 종료 컷신 체크 스킵");
            yield break;
        }
        
        // pendingCutsceneId 확인
        if (string.IsNullOrEmpty(selectedData.pendingCutsceneId))
        {
            Debug.Log("[LobbyUIController] 예약된 챕터 종료 컷신 없음");
            yield break;
        }
        
        string cutsceneId = selectedData.pendingCutsceneId;
        int chapterId = selectedData.pendingChapterId;
        
        Debug.Log($"🎬 [LobbyUIController] 챕터 {chapterId} 종료 컷신 발견: {cutsceneId} (SelectedPlayerData)");
        
        // pendingCutsceneId 정리 (재생 전에 먼저 제거)
        selectedData.pendingCutsceneId = null;
        selectedData.pendingChapterId = 0;
        PlayerDataManager.Instance.SaveCurrentSlot();
        
        // CutsceneManager 확인
        if (CutsceneSystem.CutsceneManager.Instance == null)
        {
            Debug.LogError("[LobbyUIController] CutsceneManager가 없습니다!");
            yield break;
        }
        
        // 챕터 종료 컷신 재생 (오버레이 모드)
        Debug.Log($"🎬 [LobbyUIController] 챕터 종료 컷신 재생: {cutsceneId}");
        CutsceneSystem.CutsceneManager.Instance.PlayCutscene(cutsceneId);
        
        // 컷신 종료 대기
        yield return new WaitUntil(() => !CutsceneSystem.CutsceneManager.Instance.IsPlaying);
        
        Debug.Log($"🎬 [LobbyUIController] 챕터 종료 컷신 재생 완료");
        
        // 컷신 종료 후 처리
        OnChapterClearCutsceneEnd(chapterId);
    }
    
    /// <summary>
    /// 🎬 Phase 5: 챕터 종료 컷신 종료 후 처리
    /// </summary>
    private void OnChapterClearCutsceneEnd(int completedChapterId)
    {
        Debug.Log($"🎉 [LobbyUIController] 챕터 {completedChapterId} 완료!");
        
        // 다음 챕터 오픈 확인
        int nextChapterId = completedChapterId + 1;
        
        if (nextChapterId <= 5) // 최대 Chapter 5
        {
            // ChapterManager 확인
            if (StageSystem.ChapterManager.Instance != null)
            {
                var nextChapterData = StageSystem.ChapterManager.Instance.GetChapterData(nextChapterId);
                if (nextChapterData != null)
                {
                    Debug.Log($"🔓 [LobbyUIController] 챕터 {nextChapterId} 오픈: {nextChapterData.chapterTitle}");
                    
                    // 다음 챕터 오픈 축하 메시지 (TODO: 나중에 UI 추가 가능)
                    // ShowChapterUnlockNotification(nextChapterId, nextChapterData.chapterTitle);
                }
            }
            
            // 🆕 Phase 6-Pre: StageSelectPanelController 초기화
            stageSelectPanelController?.RefreshStageProgressUI();
        }
        else
        {
            Debug.Log($"🏆 [LobbyUIController] 모든 챕터 완료! 축하합니다!");
            
            // 전체 완료 축하 메시지 (TODO: 나중에 UI 추가 가능)
            // ShowAllChaptersCompletedNotification();
        }
    }
    
    #endregion
    
    #region 🎬 Phase 7: 다시보기 기능
    
    /// <summary>
    /// 인트로 다시보기 버튼 클릭
    /// </summary>
    public void OnReplayIntroButtonClicked()
    {
        Debug.Log("[LobbyUIController] 인트로 다시보기 요청");
        
        // 현재 데이터 저장
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
        {
            PlayerDataManager.Instance.SaveCurrentSlot();
        }
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReplayIntro();
        }
        else
        {
            Debug.LogError("[LobbyUIController] GameManager가 없습니다!");
        }
    }
    
    /// <summary>
    /// 튜토리얼 다시보기 버튼 클릭
    /// </summary>
    public void OnReplayTutorialButtonClicked()
    {
        Debug.Log("[LobbyUIController] 튜토리얼 다시보기 요청");
        
        // 현재 데이터 저장
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
        {
            PlayerDataManager.Instance.SaveCurrentSlot();
        }
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReplayTutorial();
        }
        else
        {
            Debug.LogError("[LobbyUIController] GameManager가 없습니다!");
        }
    }
    
    #endregion
}