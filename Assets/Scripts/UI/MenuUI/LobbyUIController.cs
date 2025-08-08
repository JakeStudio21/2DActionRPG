using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 로비 UI 통합 컨트롤러 (캐릭터 선택 + 스테이지 선택)
/// 기존 StageSelect 씬 기능을 통합하여 단일 씬에서 모든 선택 처리
/// </summary>
public class LobbyUIController : MonoBehaviour
{
    // 🔧 Step 2-2: 슬롯 시스템 추가
    [Header("=== 캐릭터 슬롯 시스템 ===")]
    public CharacterCreationController characterCreationController; // 캐릭터 생성 컨트롤러 참조
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
    public GameObject characterSelectPanel; 
    public GameObject stageSelectPanel;     
    public GameObject inventoryPanel;       // 🔧 수정: LobbyInventorySystem으로 연결
    public GameObject shopPanel;           
    
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
    private int selectedSlotIndex = -1; // 현재 선택된 슬롯 (-1: 미선택)
    
    void Start()
    {
        InitializeUI();
        InitializeSlotSystem(); // 🔧 슬롯 시스템 초기화 추가
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
        
        // 슬롯 UI 업데이트
        RefreshAllSlots();
        
        Debug.Log("[LobbyUIController] 슬롯 시스템 초기화 완료");
    }
    
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
        if (characterSelectPanel == null) Debug.LogError("[LobbyUIController] characterSelectPanel 누락!");
        
        // 스테이지 선택 UI 검증
        if (stageSelectTitleText == null) Debug.LogError("[LobbyUIController] stageSelectTitleText 누락!");
        if (stage1Button == null) Debug.LogError("[LobbyUIController] stage1Button 누락!");
        if (stage2Button == null) Debug.LogError("[LobbyUIController] stage2Button 누락!");
        if (stage3Button == null) Debug.LogError("[LobbyUIController] stage3Button 누락!");
        if (playButton == null) Debug.LogError("[LobbyUIController] playButton 누락!");
        if (backToCharacterButton == null) Debug.LogError("[LobbyUIController] backToCharacterButton 누락!");

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
            startGameButton.onClick.AddListener(OnStartGameButtonClicked);
        }
        
        // 기존 스테이지 버튼들은 Unity Editor에서 직접 연결
    }
    
    private void SetInitialState()
    {
        // 캐릭터 선택 초기화
        // OnClassSelected("None"); // 🗑️ 제거
        
        // 스테이지 선택 초기화
        selectedStageNumber = 0;
        
        // 패널 상태 설정: 로비 메인 패널을 기본으로 활성화
        ShowLobbyPanel();
    }
    
    #region === 캐릭터 선택 관련 ===
    
    // 🗑️ [삭제됨] public void OnClassSelected(string className)
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
            string characterInfo = "캐릭터 선택"; // 🗑️ 제거
            if (selectedSlotIndex != -1) // 🆕 새로운 캐릭터 선택 상태 확인
            {
                characterInfo = PlayerDataManager.Instance.GetSlotData(selectedSlotIndex).playerName;
            }
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
        
        // 플레이어 데이터 확인 (기존 방식 복구)
        var playerData = GameManager.Instance.selectedPlayerData;
        if (playerData == null || playerData.selectedPlayerType == PlayerType.None)
        {
            Debug.LogError("[LobbyUIController] 플레이어 클래스가 선택되지 않았습니다!");
            OnBackToCharacterSelect();
            return;
        }
        
        // 선택된 스테이지 정보 저장
        string selectedSceneName = GetSceneNameFromStageNumber(selectedStageNumber);
        GameManager.Instance.SetSelectedStage(selectedStageNumber, selectedSceneName);
        
        Debug.Log($"[LobbyUIController] 스테이지 {selectedStageNumber} ({selectedSceneName})로 게임 시작");
        Debug.Log($"[LobbyUIController] 플레이어 정보: {playerData.selectedPlayerType}, {playerData.weaponName}");
        
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
    /// 로비 메인 패널 표시 (기본 화면)
    /// </summary>
    private void ShowLobbyPanel()
    {
        SetPanelVisibility(lobbyPanel, true);
        SetPanelVisibility(characterSelectPanel, false);
        SetPanelVisibility(stageSelectPanel, false);
        SetPanelVisibility(inventoryPanel, false);  // 🔧 LobbyInventorySystem 비활성화
        SetPanelVisibility(shopPanel, false);
        
        Debug.Log("[LobbyUIController] 로비 메인 패널 활성화");
    }
    
    // 🗑️ [삭제됨] ShowCharacterSelectPanel() - 더 이상 사용하지 않음
    
    /// <summary>
    /// 스테이지 선택 패널 표시
    /// </summary>
    private void ShowStageSelectPanel()
    {
        SetPanelVisibility(lobbyPanel, false);
        SetPanelVisibility(characterSelectPanel, false); // 🗑️ 추후 완전 제거 예정
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
        SetPanelVisibility(characterSelectPanel, false); // 🗑️ 추후 완전 제거 예정
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
        SetPanelVisibility(characterSelectPanel, false); // 🗑️ 추후 완전 제거 예정
        SetPanelVisibility(stageSelectPanel, false);
        SetPanelVisibility(inventoryPanel, false);
        SetPanelVisibility(shopPanel, true);
        
        Debug.Log("[LobbyUIController] 상점 패널 활성화");
    }
    
    private void SetPanelVisibility(GameObject panel, bool isVisible)
    {
        if (panel != null)
        {
            // �� 디버그: 어떤 패널이 언제 변경되는지 확인
            Debug.Log($"🎯 [LobbyUIController] SetPanelVisibility: {panel.name} → {isVisible}");
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
    public void OnStage1Button() => OnStageSelected(1);
    public void OnStage2Button() => OnStageSelected(2);
    public void OnStage3Button() => OnStageSelected(3);
    
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
        // 아이콘 설정 (빈 슬롯 아이콘)
        if (icon != null)
        {
            icon.sprite = emptySlotIcon;
            icon.gameObject.SetActive(true);
        }
        
        // 텍스트 숨기기
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
        
        // PlayerDataManager에 슬롯 선택 알림
        PlayerDataManager.Instance.SelectSlot(slotIndex);
        
        // 🔧 개선: 바로 스테이지 선택으로 가지 않고 로비에서 대기
        // ShowStageSelectPanel(); // 🗑️ 제거
        
        // 선택된 캐릭터 정보 업데이트 및 게임 시작 버튼 활성화
        UpdateSelectedCharacterInfo(slotData);
        EnableStartGameButton(true);
        
        Debug.Log($"[LobbyUIController] 캐릭터 선택 완료. 게임 시작 버튼을 클릭하세요.");
    }
    
    // 🔧 Step 2-2: 캐릭터 생성 시작 (빈 슬롯)
    private void StartCharacterCreation(int slotIndex)
    {
        Debug.Log($"[LobbyUIController] 캐릭터 생성 시작: 슬롯 {slotIndex}");
        
        if (characterCreationController != null)
        {
            characterCreationController.StartCharacterCreation(slotIndex);
        }
        else
        {
            Debug.LogError("[LobbyUIController] CharacterCreationController 참조가 없습니다!");
        }
    }
    
    // 🔧 Step 2-2: 선택된 캐릭터 정보 업데이트
    private void UpdateSelectedCharacterInfo(PlayerSlotData slotData)
    {
        if (selectedPlayerNameText != null)
        {
            selectedPlayerNameText.text = $"선택된 캐릭터: {slotData.playerName} (Lv.{slotData.level}) - {slotData.playerType}";
        }
        
        // 스테이지 선택 제목 업데이트 (미리 준비)
        if (stageSelectTitleText != null)
        {
            stageSelectTitleText.text = $"{slotData.playerName}의 모험";
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
            // TODO: 삭제 확인 팝업 표시 (추후 구현)
            // 현재는 바로 삭제
            DeleteCharacterSlot(slotIndex, slotData);
        }
    }
    
    // 🔧 Step 2-2: 캐릭터 삭제 실행
    private void DeleteCharacterSlot(int slotIndex, PlayerSlotData slotData)
    {
        Debug.Log($"[LobbyUIController] 캐릭터 삭제 실행: 슬롯 {slotIndex}, {slotData.playerName}");
        
        bool success = PlayerDataManager.Instance.DeleteSlot(slotIndex);
        
        if (success)
        {
            Debug.Log($"[LobbyUIController] ✅ 캐릭터 삭제 성공: {slotData.playerName}");
            
            // 삭제된 슬롯이 현재 선택된 슬롯이라면 선택 해제
            if (selectedSlotIndex == slotIndex)
            {
                selectedSlotIndex = -1;
            }
            
            // 슬롯 UI 새로고침
            RefreshSlotUI(slotIndex);
        }
        else
        {
            Debug.LogError($"[LobbyUIController] ❌ 캐릭터 삭제 실패: {slotData.playerName}");
        }
    }

    // 🆕 게임 시작 버튼 활성화/비활성화
    private void EnableStartGameButton(bool enable)
    {
        if (startGameButton != null)
        {
            startGameButton.interactable = enable;
            
            // 버튼 색상 변경
            var colors = startGameButton.colors;
            colors.normalColor = enable ? Color.white : disabledColor;
            startGameButton.colors = colors;
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
} 