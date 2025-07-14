using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 로비 UI 통합 컨트롤러 (캐릭터 선택 + 스테이지 선택)
/// 기존 StageSelect 씬 기능을 통합하여 단일 씬에서 모든 선택 처리
/// </summary>
public class LobbyUIController : MonoBehaviour
{
    [Header("=== 캐릭터 선택 UI ===")]
    public TMP_Text playerNameInfoText;
    public Button warriorButton;
    public Button assassinButton;
    public Button mapButton; // 스테이지 선택으로 전환하는 버튼
    public Image warriorPanelImage;
    public Image assassinPanelImage;
    
    [Header("=== UI 패널 관리 ===")]
    public GameObject lobbyPanel;          // 로비 메인 패널
    public GameObject characterSelectPanel; // 캐릭터 선택 패널
    public GameObject stageSelectPanel;     // 스테이지 선택 패널
    public GameObject inventoryPanel;       // 인벤토리 패널
    public GameObject shopPanel;           // 상점 패널
    
    [Header("=== 스테이지 선택 UI ===")]
    public TMP_Text stageSelectTitleText;
    public Button stage1Button;
    public Button stage2Button;
    public Button stage3Button;
    public Button playButton;
    public Button backToCharacterButton;
    
    [Header("=== 스테이지 버튼 이미지 ===")]
    public Image stage1Image;
    public Image stage2Image;
    public Image stage3Image;
    
    [Header("=== UI 스타일 설정 ===")]
    public Color selectedColor = Color.white;
    public Color normalColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    
    // 내부 상태
    private int selectedStageNumber = 0; // 0 = 선택안함, 1-3 = 스테이지 번호
    private string selectedCharacterClass = "None";
    
    void Start()
    {
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        Debug.Log("[LobbyUIController] 통합 UI 초기화 시작");
        
        // UI 요소 유효성 검사
        ValidateUIElements();
        
        // 버튼 이벤트 연결
        ConnectButtonEvents();
        
        // 초기 상태 설정
        SetInitialState();
        
        Debug.Log("[LobbyUIController] 통합 UI 초기화 완료");
    }
    
    private void ValidateUIElements()
    {
        // 캐릭터 선택 UI 검증
        if (playerNameInfoText == null) Debug.LogError("[LobbyUIController] playerNameInfoText 누락!");
        if (warriorButton == null) Debug.LogError("[LobbyUIController] warriorButton 누락!");
        if (assassinButton == null) Debug.LogError("[LobbyUIController] assassinButton 누락!");
        if (mapButton == null) Debug.LogError("[LobbyUIController] mapButton 누락!");
        
        // 패널 검증
        if (lobbyPanel == null) Debug.LogError("[LobbyUIController] lobbyPanel 누락!");
        if (characterSelectPanel == null) Debug.LogError("[LobbyUIController] characterSelectPanel 누락!");
        if (stageSelectPanel == null) Debug.LogError("[LobbyUIController] stageSelectPanel 누락!");
        
        // 스테이지 선택 UI 검증
        if (stageSelectTitleText == null) Debug.LogError("[LobbyUIController] stageSelectTitleText 누락!");
        if (stage1Button == null) Debug.LogError("[LobbyUIController] stage1Button 누락!");
        if (stage2Button == null) Debug.LogError("[LobbyUIController] stage2Button 누락!");
        if (stage3Button == null) Debug.LogError("[LobbyUIController] stage3Button 누락!");
        if (playButton == null) Debug.LogError("[LobbyUIController] playButton 누락!");
        if (backToCharacterButton == null) Debug.LogError("[LobbyUIController] backToCharacterButton 누락!");
    }
    
    private void ConnectButtonEvents()
    {
        // [제거됨] 모든 버튼 이벤트는 Unity Editor에서 직접 연결
        // 이제 Inspector의 OnClick() 이벤트에서 직접 함수를 연결하세요:
        // - Warrior Button -> OnWarriorButtonClick()
        // - Assassin Button -> OnAssassinButtonClick()  
        // - Map Button -> OnMapButtonClicked()
        // - Stage1 Button -> OnStage1Button()
        // - Stage2 Button -> OnStage2Button()
        // - Stage3 Button -> OnStage3Button()
        // - Play Button -> OnPlayButtonClicked()
        // - Back To Character Button -> OnBackToCharacterSelect()
        
        Debug.Log("[LobbyUIController] 버튼 이벤트는 Unity Editor에서 직접 연결됩니다.");
    }
    
    private void SetInitialState()
    {
        // 캐릭터 선택 초기화
        OnClassSelected("None");
        
        // 스테이지 선택 초기화
        selectedStageNumber = 0;
        
        // 패널 상태 설정: 로비 메인 패널을 기본으로 활성화
        ShowLobbyPanel();
    }
    
    #region === 캐릭터 선택 관련 ===
    
    public void OnClassSelected(string className)
    {
        if (LobbyManager.Instance == null)
        {
            Debug.LogError("[LobbyUIController] LobbyManager가 없습니다!");
            return;
        }
        
        selectedCharacterClass = className;
        
        // LobbyManager에 선택 정보 저장
        if (className == "Warrior")
            LobbyManager.Instance.SelectClass(PlayerType.Warrior, "Sword");
        else if (className == "Assassin")
            LobbyManager.Instance.SelectClass(PlayerType.Assassin, "Bow");
        else
            LobbyManager.Instance.SelectClass(PlayerType.None, "");
        
        UpdateClassSelectionUI(className);
        
        Debug.Log($"[LobbyUIController] 캐릭터 선택: {className}");
    }
    
    private void UpdateClassSelectionUI(string className)
    {
        if (playerNameInfoText != null)
        {
            playerNameInfoText.text = className == "None" ? "Character Select" : className;
        }
        
        if (warriorPanelImage != null)
            SetPanelActiveVisual(warriorPanelImage, className == "Warrior");
        if (assassinPanelImage != null)
            SetPanelActiveVisual(assassinPanelImage, className == "Assassin");
    }
    
    private void SetPanelActiveVisual(Image panelImage, bool isActive)
    {
        if (panelImage != null)
        {
            panelImage.color = isActive ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
    }
    
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
        
        if (LobbyManager.Instance.playerSelection.selectedType == PlayerType.None)
        {
            Debug.LogWarning("[LobbyUIController] 캐릭터를 먼저 선택해주세요!");
            return;
        }
        
        // 캐릭터 정보를 GameManager에 저장
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetRuntimePlayerData(
                LobbyManager.Instance.playerSelection.selectedType,
                LobbyManager.Instance.playerSelection.weaponName
            );
        }
        
        // 스테이지 선택 패널로 전환
        ShowStageSelectPanel();
    }
    
    #endregion
    
    #region === 스테이지 선택 관련 ===
    
    /// <summary>
    /// 스테이지 선택 처리
    /// </summary>
    public void OnStageSelected(int stageNumber)
    {
        if (stageNumber < 1 || stageNumber > 3)
        {
            Debug.LogError($"[LobbyUIController] 잘못된 스테이지 번호: {stageNumber}");
            return;
        }
        
        selectedStageNumber = stageNumber;
        UpdateStageSelectionUI();
        
        Debug.Log($"[LobbyUIController] 스테이지 {stageNumber} 선택됨");
    }
    
    private void UpdateStageSelectionUI()
    {
        // 타이틀 텍스트 업데이트
        if (stageSelectTitleText != null)
        {
            string characterInfo = selectedCharacterClass;
            if (selectedStageNumber > 0)
                stageSelectTitleText.text = $"{characterInfo} | Selected: Stage {selectedStageNumber}";
            else
                stageSelectTitleText.text = $"{characterInfo} | Select Stage";
        }
        
        // 스테이지 버튼 색상 업데이트
        UpdateStageButtonColor(stage1Image, selectedStageNumber == 1);
        UpdateStageButtonColor(stage2Image, selectedStageNumber == 2);
        UpdateStageButtonColor(stage3Image, selectedStageNumber == 3);
        
        // Play 버튼 활성화/비활성화
        if (playButton != null)
        {
            playButton.interactable = selectedStageNumber > 0;
        }
    }
    
    private void UpdateStageButtonColor(Image buttonImage, bool isSelected)
    {
        if (buttonImage == null) return;
        buttonImage.color = isSelected ? selectedColor : normalColor;
    }
    
    /// <summary>
    /// Play 버튼 클릭 처리 (게임 시작)
    /// </summary>
    public void OnPlayButtonClicked()
    {
        if (selectedStageNumber <= 0)
        {
            Debug.LogWarning("[LobbyUIController] 스테이지를 먼저 선택해주세요!");
            return;
        }
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("[LobbyUIController] GameManager가 없습니다!");
            return;
        }
        
        // 플레이어 데이터 확인
        var playerData = GameManager.Instance.GetRuntimePlayerData();
        if (playerData == null || playerData.selectedType == PlayerType.None)
        {
            Debug.LogError("[LobbyUIController] 플레이어 클래스가 선택되지 않았습니다!");
            OnBackToCharacterSelect();
            return;
        }
        
        // 선택된 스테이지 정보 저장
        string selectedSceneName = GetSceneNameFromStageNumber(selectedStageNumber);
        GameManager.Instance.SetSelectedStage(selectedStageNumber, selectedSceneName);
        
        Debug.Log($"[LobbyUIController] 스테이지 {selectedStageNumber} ({selectedSceneName})로 게임 시작");
        Debug.Log($"[LobbyUIController] 플레이어 정보: {playerData.selectedType}, {playerData.weaponName}");
        
        // 게임 씬으로 직접 이동
        GameManager.Instance.LoadGameScene(selectedSceneName);
    }
    
    private string GetSceneNameFromStageNumber(int stageNumber)
    {
        switch (stageNumber)
        {
            case 1: return "Scene1";
            case 2: return "Scene2";
            case 3: return "Scene3";
            default: return "Scene1";
        }
    }
    
    /// <summary>
    /// 캐릭터 선택으로 돌아가기
    /// </summary>
    public void OnBackToCharacterSelect()
    {
        Debug.Log("[LobbyUIController] 캐릭터 선택으로 돌아갑니다");
        ShowCharacterSelectPanel();
    }
    
    /// <summary>
    /// 로비 메인으로 돌아가기 (외부 호출용)
    /// </summary>
    public void OnBackToLobby()
    {
        Debug.Log("[LobbyUIController] 로비 메인으로 돌아갑니다");
        ShowLobbyPanel();
    }
    
    #endregion
    
    #region === 패널 전환 관리 ===
    
    /// <summary>
    /// 로비 메인 패널 표시 (기본 화면)
    /// </summary>
    private void ShowLobbyPanel()
    {
        SetPanelVisibility(lobbyPanel, true);
        SetPanelVisibility(characterSelectPanel, false);
        SetPanelVisibility(stageSelectPanel, false);
        SetPanelVisibility(inventoryPanel, false);
        SetPanelVisibility(shopPanel, false);
        
        Debug.Log("[LobbyUIController] 로비 메인 패널 활성화");
    }
    
    /// <summary>
    /// 캐릭터 선택 패널 표시
    /// </summary>
    private void ShowCharacterSelectPanel()
    {
        SetPanelVisibility(lobbyPanel, false);
        SetPanelVisibility(characterSelectPanel, true);
        SetPanelVisibility(stageSelectPanel, false);
        SetPanelVisibility(inventoryPanel, false);
        SetPanelVisibility(shopPanel, false);
        
        Debug.Log("[LobbyUIController] 캐릭터 선택 패널 활성화");
    }
    
    /// <summary>
    /// 스테이지 선택 패널 표시
    /// </summary>
    private void ShowStageSelectPanel()
    {
        SetPanelVisibility(lobbyPanel, false);
        SetPanelVisibility(characterSelectPanel, false);
        SetPanelVisibility(stageSelectPanel, true);
        SetPanelVisibility(inventoryPanel, false);
        SetPanelVisibility(shopPanel, false);
        
        // 스테이지 선택 UI 초기화
        selectedStageNumber = 0;
        UpdateStageSelectionUI();
        
        Debug.Log("[LobbyUIController] 스테이지 선택 패널 활성화");
    }
    
    /// <summary>
    /// 인벤토리 패널 표시
    /// </summary>
    public void ShowInventoryPanel()
    {
        SetPanelVisibility(lobbyPanel, false);
        SetPanelVisibility(characterSelectPanel, false);
        SetPanelVisibility(stageSelectPanel, false);
        SetPanelVisibility(inventoryPanel, true);
        SetPanelVisibility(shopPanel, false);
        
        Debug.Log("[LobbyUIController] 인벤토리 패널 활성화");
    }
    
    /// <summary>
    /// 상점 패널 표시
    /// </summary>
    public void ShowShopPanel()
    {
        SetPanelVisibility(lobbyPanel, false);
        SetPanelVisibility(characterSelectPanel, false);
        SetPanelVisibility(stageSelectPanel, false);
        SetPanelVisibility(inventoryPanel, false);
        SetPanelVisibility(shopPanel, true);
        
        Debug.Log("[LobbyUIController] 상점 패널 활성화");
    }
    
    private void SetPanelVisibility(GameObject panel, bool isVisible)
    {
        if (panel != null)
        {
            panel.SetActive(isVisible);
        }
    }
    
    #endregion
    
    #region === 기존 호환성 메서드 (Unity Editor 연결용) ===
    
    public void OnLogoutButton()
    {
        Debug.Log("[LobbyUIController] 로그아웃 버튼 클릭!");
    }
    
    public void OnCharacterSelectButton()
    {
        Debug.Log("[LobbyUIController] 캐릭터 선택창 버튼 클릭!");
        ShowCharacterSelectPanel();
    }
    
    public void OnGameMapButton()
    {
        OnMapButtonClicked();
    }
    
    public void OnEnterBattleButton()
    {
        Debug.Log("[LobbyUIController] 전투입장 버튼 클릭!");
        OnMapButtonClicked();
    }
    
    public void OnCharacterImageClick(string characterName)
    {
        Debug.Log($"[LobbyUIController] {characterName} 캐릭터 클릭!");
        OnClassSelected(characterName);
    }
    
    // Unity Editor에서 직접 연결 가능한 스테이지 선택 메서드
    public void OnStage1Button() => OnStageSelected(1);
    public void OnStage2Button() => OnStageSelected(2);
    public void OnStage3Button() => OnStageSelected(3);
    
    /// <summary>
    /// Warrior 버튼 클릭 (Unity Editor OnClick 연결용)
    /// </summary>
    public void OnWarriorButtonClick()
    {
        OnClassSelected("Warrior");
    }
    
    /// <summary>
    /// Assassin 버튼 클릭 (Unity Editor OnClick 연결용)
    /// </summary>
    public void OnAssassinButtonClick()
    {
        OnClassSelected("Assassin");
    }
    
    /// <summary>
    /// Bag 버튼 클릭 (Unity Editor OnClick 연결용)
    /// </summary>
    public void OnBagButton()
    {
        Debug.Log("[LobbyUIController] Bag 버튼 클릭!");
        ShowInventoryPanel();
    }
    
    /// <summary>
    /// Shop 버튼 클릭 (Unity Editor OnClick 연결용)
    /// </summary>
    public void OnShopButton()
    {
        Debug.Log("[LobbyUIController] Shop 버튼 클릭!");
        ShowShopPanel();
    }
    
    #endregion
} 