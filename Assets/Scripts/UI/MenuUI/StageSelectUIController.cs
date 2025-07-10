using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    
    // 현재 선택된 스테이지
    private int selectedStageNumber = 0; // 0 = 선택안함, 1-3 = 스테이지 번호
    
    void Start()
    {
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        // UI 요소들의 null 체크
        ValidateUIElements();
        
        // 버튼 이벤트 연결
        ConnectButtonEvents();
        
        // ⭐ 추가: 선택된 캐릭터 정보 표시
        DisplaySelectedCharacter();
        
        // 초기 상태 설정
        ResetStageSelection();
        
        Debug.Log("[StageSelectUIController] 초기화 완료");
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
            var playerData = GameManager.Instance.GetRuntimePlayerData();
            if (playerData != null && playerData.selectedType != PlayerType.None)
            {
                string characterName = playerData.selectedType.ToString();
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
    /// 스테이지 선택 처리
    /// </summary>
    public void OnStageSelected(int stageNumber)
    {
        if (stageNumber < 1 || stageNumber > 3)
        {
            Debug.LogError($"[StageSelectUIController] 잘못된 스테이지 번호: {stageNumber}");
            return;
        }
        
        selectedStageNumber = stageNumber;
        UpdateStageSelectionUI();
        
        Debug.Log($"[StageSelectUIController] 스테이지 {stageNumber} 선택됨");
    }
    
    /// <summary>
    /// ⭐ 수정: 스테이지 선택 UI 업데이트 (캐릭터 정보 포함)
    /// </summary>
    private void UpdateStageSelectionUI()
    {
        // ⭐ 수정: 캐릭터 정보 + 스테이지 정보 함께 표시
        if (titleText != null)
        {
            string characterInfo = GetSelectedCharacterInfo();
            
            if (selectedStageNumber > 0)
                titleText.text = $"{characterInfo} | Selected: Stage {selectedStageNumber}";
            else
                titleText.text = $"{characterInfo} | Select Stage";
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
    
    /// <summary>
    /// ⭐ 추가: 선택된 캐릭터 정보 가져오기
    /// </summary>
    private string GetSelectedCharacterInfo()
    {
        if (GameManager.Instance != null)
        {
            var playerData = GameManager.Instance.GetRuntimePlayerData();
            if (playerData != null && playerData.selectedType != PlayerType.None)
            {
                return playerData.selectedType.ToString();
            }
        }
        return "No Character";
    }
    
    /// <summary>
    /// 스테이지 버튼 색상 업데이트
    /// </summary>
    private void UpdateStageButtonColor(Image buttonImage, bool isSelected)
    {
        if (buttonImage == null) return;
        
        buttonImage.color = isSelected ? selectedColor : normalColor;
    }
    
    /// <summary>
    /// ⭐ 수정: Play 버튼 클릭 처리 (GameManager의 런타임 데이터 사용)
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
        
        // ⭐ 수정: GameManager의 런타임 플레이어 데이터 확인
        var playerData = GameManager.Instance.GetRuntimePlayerData();
        if (playerData == null || playerData.selectedType == PlayerType.None)
        {
            Debug.LogError("[StageSelectUIController] 플레이어 클래스가 선택되지 않았습니다!");
            Debug.LogError($"[StageSelectUIController] 현재 플레이어 데이터: {playerData?.selectedType}, {playerData?.weaponName}");
            // 로비로 돌아가서 캐릭터 선택하도록 안내
            OnBackToLobbyClicked();
            return;
        }
        
        // 선택된 스테이지 정보 저장
        string selectedSceneName = GetSceneNameFromStageNumber(selectedStageNumber);
        GameManager.Instance.SetSelectedStage(selectedStageNumber, selectedSceneName);
        
        Debug.Log($"[StageSelectUIController] 스테이지 {selectedStageNumber} ({selectedSceneName})로 게임 시작");
        Debug.Log($"[StageSelectUIController] 플레이어 정보: {playerData.selectedType}, {playerData.weaponName}");
        
        // ⭐ 수정: GameManager를 통한 씬 전환
        GameManager.Instance.LoadGameScene(selectedSceneName);
    }
    
    /// <summary>
    /// 스테이지 번호를 씬 이름으로 변환
    /// </summary>
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
    
    // 외부에서 호출 가능한 메서드들 (Unity 에디터에서 버튼에 연결용)
    public void OnStage1Button() => OnStageSelected(1);
    public void OnStage2Button() => OnStageSelected(2);
    public void OnStage3Button() => OnStageSelected(3);
} 