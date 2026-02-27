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
    [Header("=== 초기화 시스템 ===")]
    public LobbyInitializer lobbyInitializer; // 초기화 관리자
    
    [Header("=== 캐릭터 슬롯 시스템 ===")]
    public CharacterSlotController characterSlotController; // 슬롯 관리 컨트롤러
    public CharacterCreationController characterCreationController; // 캐릭터 생성 컨트롤러
    public ConfirmationPopup confirmationPopup; // 확인 팝업 컨트롤러

    // 🗑️ [삭제됨] 기존 캐릭터 선택 UI - 더 이상 사용하지 않음
    // public TMP_Text playerNameInfoText;
    // public Button warriorButton;
    // public Button assassinButton;
    // public Image warriorPanelImage;
    // public Image assassinPanelImage;
    
    [Header("=== 선택된 캐릭터 정보 ===")]
    public TMP_Text selectedPlayerNameText; // 🆕 선택된 캐릭터 이름 표시용
    public Button startGameButton; // 🆕 게임 시작 버튼
    
    [Header("=== 패널 관리 시스템 ===")]
    public LobbyPanelManager panelManager; // 패널 전환 관리자   
    
    [Header("🏪 로비 메뉴 버튼")]  // 🔧 수정
    public Button shopButton;               // 🆕 상점 버튼 참조
    public Button inventoryButton;          // 🆕 기존 가방 버튼 참조 (일관성)
    public Button characterInfoButton;      //  기존 영웅 버튼 참조 (일관성)
    public Button workshopButton;           // 🆕 공방 버튼 참조
    public Button skillBookButton;          // 🆕 스킬북 버튼 참조 (Phase 3-Revision)
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
        
        // LobbyInitializer로 모든 UI 초기화 위임
        if (lobbyInitializer != null)
        {
            lobbyInitializer.InitializeLobby();
        }
        else
        {
            Debug.LogError("❌ [LobbyUIController] LobbyInitializer 참조가 없습니다!");
        }
        
        // StageSelectPanelController 이벤트 구독
        if (panelManager != null && panelManager.stageSelectPanelController != null)
        {
            panelManager.stageSelectPanelController.OnPlayButtonClicked += OnStagePlayButtonClicked;
            panelManager.stageSelectPanelController.OnBackButtonClicked += OnStageBackButtonClicked;
        }
        
        Debug.Log("✅ [LobbyUIController] Start() 완료");
    }
    
    /// <summary>
    /// 이벤트 구독 (컴포넌트 활성화 시)
    /// </summary>
    private void OnEnable()
    {
        // CharacterSlotController 이벤트 구독
        if (characterSlotController != null)
        {
            characterSlotController.OnSlotClicked += OnSlotButtonClicked;
            characterSlotController.OnDeleteClicked += OnDeleteSlotButtonClicked;
            characterSlotController.OnSlotSelected += OnSlotSelectionChanged;
        }
        
        // PlayerDataManager 이벤트 구독
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnCharacterCreated += OnCharacterCreationCompleted;
        }
    }

    // 🗑️ [Phase 6-Pre 삭제] RefreshUIAfterSceneLoad() - StageSelectPanelController에서 처리
    
    /// <summary>
    /// 이벤트 구독 해제 (컴포넌트 비활성화 시)
    /// </summary>
    private void OnDisable()
    {
        // CharacterSlotController 이벤트 구독 해제
        if (characterSlotController != null)
        {
            characterSlotController.OnSlotClicked -= OnSlotButtonClicked;
            characterSlotController.OnDeleteClicked -= OnDeleteSlotButtonClicked;
            characterSlotController.OnSlotSelected -= OnSlotSelectionChanged;
        }
        
        // PlayerDataManager 이벤트 구독 해제
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnCharacterCreated -= OnCharacterCreationCompleted;
        }
        
        // StageSelectPanelController 이벤트 구독 해제
        if (stageSelectPanelController != null)
        {
            stageSelectPanelController.OnPlayButtonClicked -= OnStagePlayButtonClicked;
            stageSelectPanelController.OnBackButtonClicked -= OnStageBackButtonClicked;
        }
    }
    
    
    /// <summary>
    /// 슬롯 시스템 초기화
    /// </summary>
    
    // 🆕 스테이지 배열 초기화
    // 🗑️ [Phase 6-Pre 삭제] InitializeStageArrays() - StageSelectPanelController로 이동
    
    // 🆕 진행도 기반 UI 업데이트
    // 🗑️ [Phase 6-Pre 삭제] UpdateStageProgressUI() - StageSelectPanelController.RefreshStageProgressUI()로 대체
    // 🗑️ [Phase 6-Pre 삭제] RetryUpdateStageProgressUI() - StageSelectPanelController로 이동
    
    
    

    /// <summary>
    /// 🔧 수정: 상점 패널 표시 (Z-Order 방식 + 안전한 데이터 동기화)
    /// </summary>
    /// <summary>
    /// 상점 패널 표시 (LobbyPanelManager로 위임)
    /// </summary>
    public void ShowShopPanel()
    {
        if (!EnsureCharacterSelected()) return;
        
        Debug.Log("🏪 [LobbyUIController] ShowShopPanel 호출됨");
        
        // 패널 전환 (LobbyPanelManager로 위임)
        if (panelManager != null)
        {
            panelManager.ShowShopPanel();
        }
        
        // 상점 데이터 동기화
        StartCoroutine(SafeRefreshShopData());
        
        Debug.Log("[LobbyUIController] 상점 패널 표시 완료");
    }

    /// <summary>
    /// 상점 데이터 안전한 동기화 (LazyLoad 충돌 방지)
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
        if (panelManager != null)
        {
            var shopUIController = panelManager.shopPanel.GetComponent<ShopUIController>();
            if (shopUIController != null)
            {
                // 상점 UI 갱신 (기존 메서드 활용)
                yield return new WaitForSeconds(0.1f);
            }
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
    /// StageSelectPanelController의 Back 버튼 이벤트 핸들러
    /// </summary>
    private void OnStageBackButtonClicked()
    {
        Debug.Log("[LobbyUIController] 캐릭터 선택 화면으로 돌아갑니다.");
        
        // 로비 패널로 전환
        if (panelManager != null)
        {
            panelManager.ShowLobbyPanel();
        }
        
        // StageSelectPanelController 숨기기
        if (panelManager != null && panelManager.stageSelectPanelController != null)
        {
            panelManager.stageSelectPanelController.HidePanel();
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
        
        // ✅ 명확한 저장 시점: 로비로 돌아가기 = 인벤토리/상점 작업 완료
        if (PlayerDataManager.Instance != null)
        {
            Debug.Log("💾 [LobbyUIController] 로비 복귀 - 데이터 저장");
            PlayerDataManager.Instance.SaveOnMeaningfulEvent("ReturnToLobby");
        }
        
        ShowLobbyPanel();
        RefreshAllSlots(); // 🔧 슬롯 새로고침 추가
    }
    
    #endregion
    
    #region === 패널 전환 관리 ===
    
    /// <summary>
    /// 로비 패널 표시 (LobbyPanelManager로 위임)
    /// </summary>
    public void ShowLobbyPanel()
    {
        if (panelManager != null)
        {
            panelManager.ShowLobbyPanel();
        }
        
        // 캐릭터 선택 UI 갱신 (다른 패널에서 돌아올 때 필수)
        RefreshCharacterSelectionUI();
    }
    
    // 🗑️ [삭제됨] ShowCharacterSelectPanel() - 더 이상 사용하지 않음
    
    /// <summary>
    /// 스테이지 선택 패널 표시 (LobbyPanelManager로 위임)
    /// </summary>
    private void ShowStageSelectPanel()
    {
        if (!EnsureCharacterSelected()) return;
        
        if (panelManager != null)
        {
            panelManager.ShowStageSelectPanel();
            
            // 선택된 캐릭터 슬롯 인덱스 전달
            if (panelManager.stageSelectPanelController != null)
            {
                panelManager.stageSelectPanelController.SetSelectedSlotIndex(selectedSlotIndex);
            }
        }
    }
    
    /// <summary>
    /// 인벤토리 패널 표시 (LobbyPanelManager로 위임)
    /// </summary>
    public void ShowInventoryPanel()
    {
        Debug.Log($"🎒 [LobbyUIController] ShowInventoryPanel() 호출 (슬롯 {selectedSlotIndex})");
        
        if (!EnsureCharacterSelected()) 
        {
            Debug.LogError($"❌ [LobbyUIController] 캐릭터 선택 실패");
            return;
        }
        
        // 패널 전환 (LobbyPanelManager로 위임)
        if (panelManager != null)
        {
            panelManager.ShowInventoryPanel();
        }
        
        // 인벤토리 UI 갱신
        var inventoryUI = panelManager?.inventoryPanel.GetComponent<LobbyInventoryUI>();
        if (inventoryUI == null)
        {
            inventoryUI = panelManager?.inventoryPanel.GetComponentInChildren<LobbyInventoryUI>();
        }
        
        if (inventoryUI != null)
        {
            Debug.Log($"🔄 [LobbyUIController] ForceRefreshWithLobbySelectedCharacter({selectedSlotIndex}) 호출");
            inventoryUI.ForceRefreshWithLobbySelectedCharacter(selectedSlotIndex);
        }
        else
        {
            Debug.LogError("❌ [LobbyUIController] LobbyInventoryUI 컴포넌트를 찾을 수 없습니다!");
        }
        
        Debug.Log($"✅ [LobbyUIController] 인벤토리 패널 표시 완료");
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
        
        // 4. 인벤토리 패널 전환
        if (panelManager != null)
        {
            panelManager.ShowInventoryPanel();
        }
        
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
        
        // 4. 캐릭터 정보 패널 전환
        if (panelManager != null)
        {
            panelManager.ShowCharacterInfoPanel();
        }
        
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
    
    /// <summary>
    /// 🆕 Workshop 버튼 클릭 (Unity Editor OnClick 연결용)
    /// </summary>
    public void OnWorkshopButton()
    {
        Debug.Log("[LobbyUIController] Workshop 버튼 클릭!");
        ShowWorkshopPanel();
    }
    
    /// <summary>
    /// 🆕 SkillBook 버튼 클릭 (Unity Editor OnClick 연결용)
    /// Phase 3-Revision
    /// </summary>
    public void OnSkillBookButton()
    {
        Debug.Log("[LobbyUIController] SkillBook 버튼 클릭!");
        ShowSkillBookPanel();
    }
    
    #endregion

    /// <summary>
    /// 모든 슬롯 UI 새로고침 (CharacterSlotController로 위임)
    /// </summary>
    public void RefreshAllSlots()
    {
        if (characterSlotController != null)
        {
            characterSlotController.RefreshAllSlots();
        }
    }
    
    // 🔧 Step 2-2: 캐릭터 있는 슬롯 표시
    /// <summary>
    /// 슬롯 버튼 클릭 이벤트 (CharacterSlotController에서 호출)
    /// </summary>
    public void OnSlotButtonClicked(int slotIndex)
    {
        Debug.Log($"[LobbyUIController] 슬롯 {slotIndex} 버튼 클릭");
        
        if (PlayerDataManager.Instance == null) return;
        
        var slotData = PlayerDataManager.Instance.GetSlotData(slotIndex);
        
        if (slotData != null && slotData.isSlotUsed)
        {
            // 캐릭터 있음 - 슬롯 선택
            if (characterSlotController != null)
            {
                characterSlotController.SelectSlot(slotIndex);
            }
        }
        else
        {
            // 빈 슬롯 - 캐릭터 생성
            StartCharacterCreation(slotIndex);
        }
    }
    
    // 🔧 Step 2-2: 캐릭터 선택 (기존 캐릭터)
    // 🔧 Step 2-4: 캐릭터 선택 시 스테이지 선택으로 바로 이동하지 않고 대기
    /// <summary>
    /// 캐릭터 슬롯 선택 (CharacterSlotController.OnSlotSelected 이벤트에서 호출)
    /// </summary>
    private void OnSlotSelectionChanged(int slotIndex)
    {
        var slotData = PlayerDataManager.Instance?.GetSlotData(slotIndex);
        if (slotData == null || !slotData.isSlotUsed) return;
        
        Debug.Log($"🎯 [LobbyUIController] 슬롯 {slotIndex} 선택됨: {slotData.playerName}");
        
        // 내부 selectedSlotIndex 동기화
        selectedSlotIndex = slotIndex;
        
        // PlayerDataManager에 슬롯 선택 알림
        PlayerDataManager.Instance.SelectSlot(slotIndex);
        
        // 선택된 캐릭터 정보 UI 업데이트
        UpdateSelectedCharacterInfo(slotData);
        EnableStartGameButton(true);
        
        // StageProgressManager 초기화
        if (StageProgressManager.Instance != null)
        {
            StageProgressManager.Instance.InitializeFor(slotIndex);
        }
        
        // StageSelectPanelController 초기화
        stageSelectPanelController?.RefreshStageProgressUI();
        
        Debug.Log($"✅ [LobbyUIController] 슬롯 선택 완료");
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
    
    /// <summary>
    /// 캐릭터 삭제 실행
    /// </summary>
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
                if (newSelectedSlot >= 0 && characterSlotController != null)
                {
                    characterSlotController.SelectSlot(newSelectedSlot);
                }
                else
                {
                    selectedSlotIndex = -1;
                    EnableStartGameButton(false);
                }
            }
            
            // 로비 슬롯 UI 새로고침
            RefreshAllSlots();
            
            // CharacterInfoUI 갱신 (활성화된 경우)
            if (panelManager != null && panelManager.characterInfoPanel != null)
            {
                var characterInfoUI = panelManager.characterInfoPanel.GetComponent<CharacterInfoUI>();
                if (characterInfoUI != null && panelManager.characterInfoPanel.activeInHierarchy)
                {
                    characterInfoUI.ForceRefreshWithCurrentSlot(selectedSlotIndex);
                }
                
                // LobbyEquippedItemsUI 갱신 (활성화된 경우)
                var equippedItemsUI = panelManager.characterInfoPanel.GetComponentInChildren<LobbyEquippedItemsUI>();
                if (equippedItemsUI != null && panelManager.characterInfoPanel.activeInHierarchy)
                {
                    equippedItemsUI.ForceRefreshEquippedItems();
                }
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
    /// <summary>
    /// 자동 슬롯 선택 (로비 진입 시)
    /// </summary>
    public void AutoSelectSlotIfNeeded()
    {
        // 이미 선택되어 있으면 패스
        if (PlayerDataManager.Instance.IsSlotSelected) return;
        
        // 캐릭터가 없으면 온보딩
        if (!PlayerDataManager.Instance.HasAnyCharacter())
        {
            Debug.Log("[Lobby] No characters → Onboarding");
            StartCharacterCreationOnboarding();
            return;
        }
        
        // 자동 선택할 슬롯 결정
        int targetSlot = GetAutoSelectSlot();
        if (targetSlot >= 0 && characterSlotController != null)
        {
            characterSlotController.SelectSlot(targetSlot);
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
    /// <summary>
    /// 캐릭터 생성 완료 콜백 (PlayerDataManager 이벤트)
    /// </summary>
    public void OnCharacterCreationCompleted(int slotIndex)
    {
        Debug.Log($"[LobbyUIController] 캐릭터 생성 완료: 슬롯 {slotIndex}");
        
        // 안전성 검사
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("[LobbyUIController] PlayerDataManager.Instance가 null");
            return;
        }
        
        var newSlotData = PlayerDataManager.Instance.GetSlotData(slotIndex);
        if (newSlotData == null || !newSlotData.isSlotUsed)
        {
            Debug.LogError($"[LobbyUIController] 슬롯 {slotIndex} 데이터가 유효하지 않음");
            return;
        }
        
        // 슬롯 UI 새로고침
        RefreshAllSlots();
        
        // 신규 생성된 캐릭터 자동 선택
        if (characterSlotController != null)
        {
            characterSlotController.SelectSlot(slotIndex);
            Debug.Log($"[Lobby] New character selected → Slot {slotIndex}");
        }
        
        Debug.Log($"[LobbyUIController] 신규 캐릭터 {slotIndex} 설정 완료");
    }

    // 🔍 매니저 초기화 상태 확인 메서드 추가

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
        
        // 패널 전환 (LobbyPanelManager로 위임)
        if (panelManager != null)
        {
            panelManager.ShowCharacterInfoPanel();
            
            // 현재 선택된 캐릭터로 강제 갱신
            var characterInfoUI = panelManager.characterInfoPanel.GetComponent<CharacterInfoUI>();
            if (characterInfoUI != null)
            {
                characterInfoUI.ForceRefreshWithCurrentSlot(selectedSlotIndex);
            }
        }
        
        Debug.Log($"[LobbyUIController] 캐릭터 정보 패널 표시 완료 (슬롯 {selectedSlotIndex})");
    }
    
    /// <summary>
    /// 🆕 공방(제작) 패널 표시
    /// </summary>
    public void ShowWorkshopPanel()
    {
        if (!EnsureCharacterSelected()) return;
        
        Debug.Log("🏭 [LobbyUIController] ShowWorkshopPanel 호출됨");
        
        // 패널 전환 (LobbyPanelManager로 위임)
        if (panelManager != null)
        {
            panelManager.ShowWorkshopPanel();
        }
        
        Debug.Log("[LobbyUIController] 공방 패널 표시 완료");
    }
    
    /// <summary>
    /// 🆕 스킬북 패널 표시 (Phase 3-Revision)
    /// </summary>
    public void ShowSkillBookPanel()
    {
        if (!EnsureCharacterSelected()) return;
        
        Debug.Log("📚 [LobbyUIController] ShowSkillBookPanel 호출됨");
        
        // 패널 전환 (LobbyPanelManager로 위임)
        if (panelManager != null)
        {
            panelManager.ShowSkillBookPanel();
        }
        
        Debug.Log("[LobbyUIController] 스킬북 패널 표시 완료");
    }


    /// <summary>
    /// 🆕 백그라운드에서 모든 패널 초기화 (단순화)
    /// </summary>
    public IEnumerator InitializeAllPanelsInBackground()
    {
        Debug.Log("🔄 [LobbyUIController] 백그라운드 패널 초기화 시작");
        
        // 매니저 초기화 대기
        yield return StartCoroutine(WaitForManagersInitialization(() => {}));
        
        // 캐릭터 선택 대기
        while (!PlayerDataManager.Instance.IsSlotSelected)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        if (panelManager != null)
        {
            // 상점 초기화
            if (ShopInventoryManager.Instance != null)
            {
                ShopInventoryManager.Instance.LoadItemsForShop();
            }
            
            var shopUIController = panelManager.shopPanel.GetComponent<ShopUIController>();
            if (shopUIController != null)
            {
                shopUIController.OnShopOpened();
            }
            
            // 인벤토리 초기화
            var inventoryUI = panelManager.inventoryPanel.GetComponent<LobbyInventoryUI>();
            if (inventoryUI != null)
            {
                inventoryUI.ForceRefreshInventory();
            }
            
            // 캐릭터 정보 초기화
            var characterInfoUI = panelManager.characterInfoPanel.GetComponent<CharacterInfoUI>();
            if (characterInfoUI != null)
            {
                // 캐릭터 정보 UI 초기화 (필요시)
            }
        }
        
        Debug.Log("✅ [LobbyUIController] 백그라운드 패널 초기화 완료");
    }

    /// <summary>
    /// <summary>
    /// 캐릭터 선택 UI 갱신 (CharacterSlotController로 위임)
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
        
        // 모든 슬롯 UI 새로고침 (CharacterSlotController로 위임)
        RefreshAllSlots();
        
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

    /// <summary>
    /// 현재 선택된 캐릭터의 Button Selection 복원 (외부 호출용)
    /// </summary>
    public void RestoreCharacterSelection()
    {
        if (characterSlotController != null && selectedSlotIndex >= 0)
        {
            characterSlotController.RefreshSlot(selectedSlotIndex);
            Debug.Log($"🔄 [LobbyUIController] 캐릭터 선택 복원: 슬롯 {selectedSlotIndex}");
        }
    }
    
    #region 🎬 Phase 5: 챕터 종료 처리
    
    
    /// <summary>
    /// 🎬 Phase 5: 챕터 종료 컷신 종료 후 처리
    /// </summary>
    public void OnChapterClearCutsceneEnd(int completedChapterId)
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