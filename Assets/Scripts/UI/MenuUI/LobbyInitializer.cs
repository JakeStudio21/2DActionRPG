using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CueSystem;
using StageSystem;
using System.Collections;

/// <summary>
/// 로비 UI 초기화 관리자
/// 책임: UI 초기화, 검증, 버튼 이벤트 연결, 슬롯 시스템, 초기 상태 설정
/// </summary>
public class LobbyInitializer : MonoBehaviour
{
    [Header("=== 참조 컴포넌트 ===")]
    public LobbyUIController lobbyUIController;
    public LobbyPanelManager panelManager;
    public CharacterSlotController characterSlotController;
    public CharacterCreationController characterCreationController;
    
    [Header("=== UI 요소 ===")]
    public Button startGameButton;
    public Button inventoryButton;
    public Button characterInfoButton;
    public Button shopButton;
    public Button workshopButton;       // 🆕 공방 버튼
    public Button quitGameButton;
    public Button replayIntroButton;
    public Button replayTutorialButton;
    
    [Header("=== 배경 오버레이 ===")]
    public GameObject backgroundOverlay;
    
    /// <summary>
    /// 전체 UI 초기화 (LobbyUIController.Start()에서 호출)
    /// </summary>
    public void InitializeLobby()
    {
        StartCoroutine(InitializeLobbyCoroutine());
    }
    
    /// <summary>
    /// 로비 초기화 (풀 로딩 대기 포함)
    /// </summary>
    private IEnumerator InitializeLobbyCoroutine()
    {
        Debug.Log("📋 [LobbyInitializer] 로비 초기화 시작");
        
        // ⭐ GamePoolManager 풀 로딩 완료 대기
        if (GamePoolManager.Instance != null)
        {
            Debug.Log("⏳ [LobbyInitializer] GamePoolManager 풀 로딩 대기 중...");
            
            float timeout = 5f; // 5초 타임아웃
            float elapsed = 0f;
            
            while (GamePoolManager.Instance.IsLoadingPools && elapsed < timeout)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }
            
            if (elapsed >= timeout)
            {
                Debug.LogWarning("⚠️ [LobbyInitializer] GamePoolManager 풀 로딩 타임아웃 (5초 초과)");
            }
            else
            {
                Debug.Log("✅ [LobbyInitializer] GamePoolManager 풀 로딩 완료!");
            }
        }
        
        CheckManagerInitializationStatus();
        ValidateUIElements();
        ConnectButtonEvents();
        InitializeSlotSystem();
        InitializePanelManager();
        SetInitialState();
        StartCoroutine(CheckPendingChapterClearCutscene());
        ValidateBackgroundOverlay();
        
        Debug.Log("✅ [LobbyInitializer] 로비 초기화 완료");
    }
    
    #region 매니저 초기화 상태 확인
    
    private void CheckManagerInitializationStatus()
    {
        Debug.Log("🔍 [LobbyInitializer] 매니저 초기화 상태 확인:");
        
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
    
    #endregion
    
    #region UI 요소 검증
    
    private void ValidateUIElements()
    {
        Debug.Log("🔍 [LobbyInitializer] UI 요소 검증 시작");
        
        if (lobbyUIController == null)
            Debug.LogError("[LobbyInitializer] LobbyUIController 참조가 없습니다!");
        
        if (panelManager == null)
            Debug.LogError("[LobbyInitializer] LobbyPanelManager 참조가 없습니다!");
        
        if (characterSlotController == null)
            Debug.LogError("[LobbyInitializer] CharacterSlotController 참조가 없습니다!");
        
        if (startGameButton == null) 
            Debug.LogError("[LobbyInitializer] startGameButton 누락!");
        
        if (inventoryButton == null) 
            Debug.LogWarning("[LobbyInitializer] inventoryButton 누락!");
        
        if (characterInfoButton == null) 
            Debug.LogWarning("[LobbyInitializer] characterInfoButton 누락!");
        
        if (shopButton == null) 
            Debug.LogWarning("[LobbyInitializer] shopButton 누락!");
        
        if (quitGameButton == null) 
            Debug.LogWarning("[LobbyInitializer] quitGameButton 누락!");
        
        Debug.Log("✅ [LobbyInitializer] UI 요소 검증 완료");
    }
    
    #endregion
    
    #region 버튼 이벤트 연결
    
    private void ConnectButtonEvents()
    {
        Debug.Log("🔗 [LobbyInitializer] 버튼 이벤트 연결 시작");
        
        if (lobbyUIController == null)
        {
            Debug.LogError("[LobbyInitializer] LobbyUIController가 없어서 버튼 이벤트를 연결할 수 없습니다!");
            return;
        }
        
        // 게임 시작 버튼
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(() => {
                EmitButtonClickCue();
                lobbyUIController.OnStartGameButtonClicked();
            });
            Debug.Log("   ✅ 게임 시작 버튼");
        }
        
        // 인벤토리 버튼
        if (inventoryButton != null)
        {
            inventoryButton.onClick.AddListener(() => {
                EmitButtonClickCue();
                lobbyUIController.ShowInventoryPanel();
            });
            Debug.Log("   ✅ 인벤토리 버튼");
        }
        
        // 캐릭터 정보 버튼
        if (characterInfoButton != null)
        {
            characterInfoButton.onClick.AddListener(() => {
                EmitButtonClickCue();
                lobbyUIController.ShowCharacterInfoPanel();
            });
            Debug.Log("   ✅ 캐릭터 정보 버튼");
        }
        
        // 상점 버튼
        if (shopButton != null)
        {
            shopButton.onClick.AddListener(() => {
                EmitButtonClickCue();
                lobbyUIController.ShowShopPanel();
            });
            Debug.Log("   ✅ 상점 버튼");
        }
        
        // 🆕 공방 버튼
        if (workshopButton != null)
        {
            workshopButton.onClick.AddListener(() => {
                EmitButtonClickCue();
                lobbyUIController.ShowWorkshopPanel();
            });
            Debug.Log("   ✅ 공방 버튼");
        }
        
        // 게임 종료 버튼
        if (quitGameButton != null)
        {
            quitGameButton.onClick.AddListener(() => {
                EmitButtonClickCue();
                lobbyUIController.OnQuitGameButtonClicked();
            });
            Debug.Log("   ✅ 게임 종료 버튼");
        }
        
        // 인트로 다시보기 버튼
        if (replayIntroButton != null)
        {
            replayIntroButton.onClick.AddListener(() => {
                EmitButtonClickCue();
                lobbyUIController.OnReplayIntroButtonClicked();
            });
            Debug.Log("   ✅ 인트로 다시보기 버튼");
        }
        
        // 튜토리얼 다시보기 버튼
        if (replayTutorialButton != null)
        {
            replayTutorialButton.onClick.AddListener(() => {
                EmitButtonClickCue();
                lobbyUIController.OnReplayTutorialButtonClicked();
            });
            Debug.Log("   ✅ 튜토리얼 다시보기 버튼");
        }
        
        Debug.Log("✅ [LobbyInitializer] 버튼 이벤트 연결 완료");
    }
    
    private void EmitButtonClickCue()
    {
        var context = new CueContext 
        { 
            position = Vector3.zero, 
            actorType = ActorType.UI 
        };
        CueEmitter.Emit("ui.button.click", "UI", context);
    }
    
    #endregion
    
    #region 슬롯 시스템 초기화
    
    private void InitializeSlotSystem()
    {
        Debug.Log("🎮 [LobbyInitializer] 슬롯 시스템 초기화 시작");
        
        // PlayerDataManager에서 모든 슬롯 로드
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.LoadAllSlots();
        }
        
        // CharacterSlotController 초기화
        if (characterSlotController != null)
        {
            characterSlotController.Initialize();
        }
        else
        {
            Debug.LogError("[LobbyInitializer] CharacterSlotController 참조가 없습니다!");
        }
        
        // 자동 슬롯 선택
        if (lobbyUIController != null)
        {
            lobbyUIController.AutoSelectSlotIfNeeded();
        }
        
        // StageProgressManager 초기화
        if (StageProgressManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
        {
            int currentSlot = PlayerDataManager.Instance.GetCurrentSlotIndex();
            if (!StageProgressManager.Instance.IsInitialized || 
                StageProgressManager.Instance.GetCurrentSlotIndex() != currentSlot)
            {
                StageProgressManager.Instance.InitializeFor(currentSlot);
                Debug.Log($"   🔄 StageProgressManager 재초기화: 슬롯 {currentSlot}");
            }
        }
        
        // 슬롯 UI 업데이트
        if (lobbyUIController != null)
        {
            lobbyUIController.RefreshAllSlots();
        }
        
        // StageSelectPanelController 초기화
        if (panelManager != null && panelManager.stageSelectPanelController != null)
        {
            panelManager.stageSelectPanelController.RefreshStageProgressUI();
        }
        
        Debug.Log("✅ [LobbyInitializer] 슬롯 시스템 초기화 완료");
    }
    
    #endregion
    
    #region 패널 관리자 초기화
    
    private void InitializePanelManager()
    {
        Debug.Log("🎨 [LobbyInitializer] 패널 관리자 초기화 시작");
        
        if (panelManager != null)
        {
            panelManager.Initialize();
        }
        else
        {
            Debug.LogError("[LobbyInitializer] LobbyPanelManager 참조가 없습니다!");
        }
        
        Debug.Log("✅ [LobbyInitializer] 패널 관리자 초기화 완료");
    }
    
    #endregion
    
    #region 초기 상태 설정
    
    private void SetInitialState()
    {
        Debug.Log("⚙️ [LobbyInitializer] 초기 상태 설정 시작");
        
        if (panelManager != null)
        {
            // 모든 패널을 활성화 상태로 유지
            SetPanelVisibility(panelManager.lobbyPanel, true);
            SetPanelVisibility(panelManager.stageSelectPanel, true);
            SetPanelVisibility(panelManager.inventoryPanel, true);
            SetPanelVisibility(panelManager.shopPanel, true);
            SetPanelVisibility(panelManager.characterInfoPanel, true);
            
            // Z-Order 설정: 로비가 최상위
            panelManager.ShowLobbyPanel();
        }
        
        // 모든 패널 백그라운드 초기화 (활성화 상태에서)
        if (lobbyUIController != null)
        {
            lobbyUIController.StartCoroutine(lobbyUIController.InitializeAllPanelsInBackground());
        }
        
        Debug.Log("✅ [LobbyInitializer] 초기 상태 설정 완료");
    }
    
    private void SetPanelVisibility(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }
    
    #endregion
    
    #region 챕터 종료 컷신 체크
    
    private IEnumerator CheckPendingChapterClearCutscene()
    {
        // 로비 UI 초기화 대기
        yield return new WaitForSeconds(0.5f);
        
        if (PlayerDataManager.Instance == null || !PlayerDataManager.Instance.IsSlotSelected)
        {
            Debug.Log("   [LobbyInitializer] 슬롯이 선택되지 않음 - 챕터 종료 컷신 체크 스킵");
            yield break;
        }
        
        // SelectedPlayerData에서 pendingCutsceneId 확인
        var selectedData = PlayerDataManager.Instance.selectedPlayerData;
        if (selectedData == null)
        {
            Debug.LogWarning("   [LobbyInitializer] SelectedPlayerData가 없음 - 챕터 종료 컷신 체크 스킵");
            yield break;
        }
        
        // pendingCutsceneId 확인
        if (string.IsNullOrEmpty(selectedData.pendingCutsceneId))
        {
            Debug.Log("   [LobbyInitializer] 예약된 챕터 종료 컷신 없음");
            yield break;
        }
        
        string cutsceneId = selectedData.pendingCutsceneId;
        int chapterId = selectedData.pendingChapterId;
        
        Debug.Log($"   🎬 챕터 {chapterId} 종료 컷신 발견: {cutsceneId}");
        
        // pendingCutsceneId 정리 (재생 전에 먼저 제거)
        selectedData.pendingCutsceneId = null;
        selectedData.pendingChapterId = 0;
        PlayerDataManager.Instance.MarkDirty();
        PlayerDataManager.Instance.SaveCurrentSlot();
        
        // CutsceneManager 확인
        if (CutsceneSystem.CutsceneManager.Instance == null)
        {
            Debug.LogError("   [LobbyInitializer] CutsceneManager가 없습니다!");
            yield break;
        }
        
        // 챕터 종료 컷신 재생 (오버레이 모드)
        Debug.Log($"   🎬 챕터 종료 컷신 재생: {cutsceneId}");
        CutsceneSystem.CutsceneManager.Instance.PlayCutscene(cutsceneId);
        
        // 컷신 종료 대기
        yield return new WaitUntil(() => !CutsceneSystem.CutsceneManager.Instance.IsPlaying);
        
        Debug.Log($"   ✅ 챕터 종료 컷신 재생 완료");
        
        // 컷신 종료 후 처리
        if (lobbyUIController != null)
        {
            lobbyUIController.OnChapterClearCutsceneEnd(chapterId);
        }
    }
    
    #endregion
    
    #region 배경 오버레이 검증
    
    private void ValidateBackgroundOverlay()
    {
        if (backgroundOverlay != null)
        {
            // 오버레이가 최하위 레이어(Index 0)에 있는지 확인
            int siblingIndex = backgroundOverlay.transform.GetSiblingIndex();
            if (siblingIndex != 0)
            {
                backgroundOverlay.transform.SetAsFirstSibling();
                Debug.Log($"   🔧 BackgroundOverlay를 최하위 레이어로 이동 (Index: {siblingIndex} → 0)");
            }
            
            // Image 컴포넌트 Raycast Target 확인
            var overlayImage = backgroundOverlay.GetComponent<UnityEngine.UI.Image>();
            if (overlayImage != null && !overlayImage.raycastTarget)
            {
                Debug.LogWarning("   ⚠️ BackgroundOverlay Image의 Raycast Target이 비활성화되어 있습니다!");
            }
            
            // BackgroundOverlayHandler 확인
            var handler = backgroundOverlay.GetComponent<BackgroundOverlayHandler>();
            if (handler == null)
            {
                Debug.LogWarning("   ⚠️ BackgroundOverlay에 BackgroundOverlayHandler가 없습니다!");
            }
            
            Debug.Log("   ✅ BackgroundOverlay 검증 완료");
        }
        else
        {
            Debug.LogWarning("   ⚠️ backgroundOverlay가 할당되지 않았습니다!");
        }
    }
    
    #endregion
}
