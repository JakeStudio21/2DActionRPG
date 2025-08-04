using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

/// <summary>
/// 캐릭터 생성 시스템 컨트롤러
/// 로비에서 빈 슬롯 클릭 → 클래스 선택 → 이름 입력 → 캐릭터 생성
/// </summary>
public class CharacterCreationController : MonoBehaviour
{
    [Header("=== 외부 참조 (Inspector 연결) ===")]
    public LobbyUIController lobbyUIController; // 🔧 개선 2: 직접 참조
    
    [Header("=== 캐릭터 생성 UI 패널 ===")]
    public GameObject characterCreationPanel;
    public GameObject classSelectionPanel;
    public GameObject nameInputPanel;
    
    [Header("=== 클래스 선택 UI ===")]
    public Button warriorButton;
    public Button assassinButton;
    public Button wizardButton;
    public Button classConfirmButton;
    
    [Header("=== 클래스 정보 표시 ===")]
    public TMP_Text classNameText;
    public TMP_Text attackText;
    public TMP_Text defenseText;
    public TMP_Text speedText;
    public TMP_Text descriptionText;
    
    [Header("=== 이름 입력 UI ===")]
    public TMP_InputField nameInputField;
    public Button nameConfirmButton;
    public Button nameCancelButton;
    public TMP_Text nameErrorText; // 🔧 개선 3: 오류 메시지 표시
    
    [Header("=== 뒤로가기 버튼 ===")]
    public Button backToLobbyButton;
    
    // 내부 상태
    private int targetSlotIndex = -1;
    private PlayerType selectedClass = PlayerType.Warrior;
    private string inputPlayerName = "";
    
    // 클래스별 정보 (간단한 수치)
    private readonly ClassInfo[] classInfos = new ClassInfo[]
    {
        new ClassInfo 
        { 
            playerType = PlayerType.Warrior, 
            className = "Warrior", 
            attack = 8, 
            defense = 9, 
            speed = 6, 
            description = "균형잡힌 근접 전사\n높은 방어력과 안정적인 공격력" 
        },
        new ClassInfo 
        { 
            playerType = PlayerType.Assasin, 
            className = "Assassin", 
            attack = 9, 
            defense = 5, 
            speed = 10, 
            description = "빠르고 치명적인 암살자\n높은 이동속도와 크리티컬 확률" 
        },
        new ClassInfo 
        { 
            playerType = PlayerType.Wizard, 
            className = "Wizard", 
            attack = 10, 
            defense = 4, 
            speed = 7, 
            description = "강력한 마법 공격\n원거리 공격과 광역 스킬" 
        }
    };
    
    void Start()
    {
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        Debug.Log("[CharacterCreationController] 캐릭터 생성 UI 초기화");
        
        // 🔧 개선 2: 직접 참조 검증
        if (lobbyUIController == null)
        {
            lobbyUIController = FindObjectOfType<LobbyUIController>();
            if (lobbyUIController == null)
            {
                Debug.LogError("[CharacterCreationController] ❌ LobbyUIController 참조가 없습니다! Inspector에서 연결해주세요.");
            }
        }
        
        // 버튼 이벤트 연결
        if (warriorButton != null) warriorButton.onClick.AddListener(() => OnClassSelected(PlayerType.Warrior));
        if (assassinButton != null) assassinButton.onClick.AddListener(() => OnClassSelected(PlayerType.Assasin));
        if (wizardButton != null) wizardButton.onClick.AddListener(() => OnClassSelected(PlayerType.Wizard));
        if (classConfirmButton != null) classConfirmButton.onClick.AddListener(OnClassConfirmed);
        
        if (nameConfirmButton != null) nameConfirmButton.onClick.AddListener(OnNameConfirmed);
        if (nameCancelButton != null) nameCancelButton.onClick.AddListener(OnNameCancelled);
        if (backToLobbyButton != null) backToLobbyButton.onClick.AddListener(OnBackToLobby);
        
        // 초기 상태 설정
        HideAllPanels();
        ClearNameError(); // 🔧 개선 3: 오류 메시지 초기화
        
        // 기본 클래스 선택
        OnClassSelected(PlayerType.Warrior);
    }
    
    /// <summary>
    /// 캐릭터 생성 시작 (외부 호출용)
    /// </summary>
    public void StartCharacterCreation(int slotIndex)
    {
        Debug.Log($"[CharacterCreationController] 슬롯 {slotIndex} 캐릭터 생성 시작");
        
        // 🔧 개선 3: 슬롯 유효성 검사
        if (!IsValidSlotForCreation(slotIndex))
        {
            ShowErrorMessage($"슬롯 {slotIndex}는 이미 사용 중이거나 유효하지 않습니다.");
            return;
        }
        
        targetSlotIndex = slotIndex;
        ShowClassSelectionPanel();
    }
    
    /// <summary>
    /// 🔧 개선 3: 슬롯 생성 가능 여부 검사
    /// </summary>
    private bool IsValidSlotForCreation(int slotIndex)
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("[CharacterCreationController] PlayerDataManager가 없습니다!");
            return false;
        }
        
        var slotData = PlayerDataManager.Instance.GetSlotData(slotIndex);
        return slotData != null && !slotData.isSlotUsed;
    }
    
    /// <summary>
    /// 클래스 선택 패널 표시
    /// </summary>
    private void ShowClassSelectionPanel()
    {
        HideAllPanels();
        ClearNameError(); // 🔧 개선 3: 오류 메시지 초기화
        
        if (characterCreationPanel != null) characterCreationPanel.SetActive(true);
        if (classSelectionPanel != null) classSelectionPanel.SetActive(true);
        
        Debug.Log("[CharacterCreationController] 클래스 선택 패널 활성화");
    }
    
    /// <summary>
    /// 이름 입력 패널 표시
    /// </summary>
    private void ShowNameInputPanel()
    {
        if (classSelectionPanel != null) classSelectionPanel.SetActive(false);
        if (nameInputPanel != null) nameInputPanel.SetActive(true);
        
        // 입력 필드 초기화 및 포커스
        if (nameInputField != null)
        {
            nameInputField.text = "";
            nameInputField.Select();
        }
        
        ClearNameError(); // 🔧 개선 3: 오류 메시지 초기화
        
        Debug.Log("[CharacterCreationController] 이름 입력 패널 활성화");
    }
    
    /// <summary>
    /// 모든 패널 숨기기
    /// </summary>
    private void HideAllPanels()
    {
        if (characterCreationPanel != null) characterCreationPanel.SetActive(false);
        if (classSelectionPanel != null) classSelectionPanel.SetActive(false);
        if (nameInputPanel != null) nameInputPanel.SetActive(false);
    }
    
    /// <summary>
    /// 클래스 선택 이벤트
    /// </summary>
    private void OnClassSelected(PlayerType playerType)
    {
        selectedClass = playerType;
        UpdateClassInfoDisplay();
        
        Debug.Log($"[CharacterCreationController] 클래스 선택: {playerType}");
    }
    
    /// <summary>
    /// 클래스 정보 UI 업데이트
    /// </summary>
    private void UpdateClassInfoDisplay()
    {
        var classInfo = Array.Find(classInfos, info => info.playerType == selectedClass);
        if (classInfo == null) return;
        
        if (classNameText != null) classNameText.text = classInfo.className;
        if (attackText != null) attackText.text = $"공격력: {classInfo.attack}";
        if (defenseText != null) defenseText.text = $"방어력: {classInfo.defense}";
        if (speedText != null) speedText.text = $"이동속도: {classInfo.speed}";
        if (descriptionText != null) descriptionText.text = classInfo.description;
    }
    
    /// <summary>
    /// 클래스 선택 확인 버튼
    /// </summary>
    private void OnClassConfirmed()
    {
        Debug.Log($"[CharacterCreationController] 클래스 확정: {selectedClass}");
        ShowNameInputPanel();
    }
    
    /// <summary>
    /// 캐릭터 이름 확인 버튼
    /// </summary>
    private void OnNameConfirmed()
    {
        inputPlayerName = nameInputField?.text?.Trim() ?? "";
        
        // 🔧 개선 1 & 3: 이름 유효성 검사 + UI 피드백
        string validationError = ValidatePlayerName(inputPlayerName);
        if (!string.IsNullOrEmpty(validationError))
        {
            ShowNameError(validationError);
            return;
        }
        
        CreateCharacter();
    }
    
    /// <summary>
    /// 🔧 개선 1: 플레이어 이름 유효성 검사 (중복 체크 포함)
    /// </summary>
    private string ValidatePlayerName(string playerName)
    {
        // 기본 유효성 검사
        if (string.IsNullOrEmpty(playerName))
        {
            return "캐릭터 이름을 입력해주세요.";
        }
        
        if (playerName.Length < 2)
        {
            return "캐릭터 이름은 최소 2자 이상이어야 합니다.";
        }
        
        if (playerName.Length > 12)
        {
            return "캐릭터 이름은 12자 이하여야 합니다.";
        }
        
        // 특수문자 검사 (한글, 영문, 숫자만 허용)
        if (!System.Text.RegularExpressions.Regex.IsMatch(playerName, @"^[가-힣a-zA-Z0-9]+$"))
        {
            return "캐릭터 이름은 한글, 영문, 숫자만 사용할 수 있습니다.";
        }
        
        // 🔧 개선 1: 중복 이름 검사
        if (IsNameAlreadyUsed(playerName))
        {
            return "이미 사용 중인 캐릭터 이름입니다.";
        }
        
        return null; // 유효함
    }
    
    /// <summary>
    /// 🔧 개선 1: 이름 중복 검사
    /// </summary>
    private bool IsNameAlreadyUsed(string playerName)
    {
        if (PlayerDataManager.Instance == null) return false;
        
        // 모든 슬롯을 확인하여 동일한 이름이 있는지 검사
        for (int i = 0; i < 3; i++) // maxSlots = 3
        {
            var slotData = PlayerDataManager.Instance.GetSlotData(i);
            if (slotData != null && slotData.isSlotUsed)
            {
                if (string.Equals(slotData.playerName, playerName, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[CharacterCreationController] 이름 중복 발견: '{playerName}' (슬롯 {i})");
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 캐릭터 생성 실행
    /// </summary>
    private void CreateCharacter()
    {
        Debug.Log($"[CharacterCreationController] 캐릭터 생성: 슬롯{targetSlotIndex}, {selectedClass}, {inputPlayerName}");
        
        // 🔧 개선 3: 생성 전 최종 검증
        if (PlayerDataManager.Instance == null)
        {
            ShowErrorMessage("게임 데이터 관리자를 찾을 수 없습니다.");
            return;
        }
        
        // PlayerDataManager를 통해 캐릭터 생성
        bool success = PlayerDataManager.Instance.CreateNewSlot(targetSlotIndex, selectedClass, inputPlayerName);
        
        if (success)
        {
            Debug.Log($"[CharacterCreationController] ✅ 캐릭터 생성 성공!");
            OnBackToLobby();
        }
        else
        {
            // 🔧 개선 3: 실패 시 상세한 UI 피드백
            Debug.LogError($"[CharacterCreationController] ❌ 캐릭터 생성 실패!");
            ShowErrorMessage("캐릭터 생성에 실패했습니다. 잠시 후 다시 시도해주세요.");
        }
    }
    
    /// <summary>
    /// 🔧 개선 3: 이름 오류 메시지 표시
    /// </summary>
    private void ShowNameError(string message)
    {
        if (nameErrorText != null)
        {
            nameErrorText.text = message;
            nameErrorText.gameObject.SetActive(true);
        }
        
        Debug.LogWarning($"[CharacterCreationController] 이름 검증 실패: {message}");
    }
    
    /// <summary>
    /// 🔧 개선 3: 이름 오류 메시지 지우기
    /// </summary>
    private void ClearNameError()
    {
        if (nameErrorText != null)
        {
            nameErrorText.text = "";
            nameErrorText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 🔧 개선 3: 일반적인 오류 메시지 표시 (추후 Popup 연동 예정)
    /// </summary>
    private void ShowErrorMessage(string message)
    {
        Debug.LogError($"[CharacterCreationController] 오류: {message}");
        
        // TODO: 추후 팝업 시스템 연동 시 여기서 처리
        // PopupManager.Instance.ShowErrorPopup(message);
        
        // 임시로 이름 오류 텍스트에 표시
        ShowNameError(message);
    }
    
    /// <summary>
    /// 이름 입력 취소
    /// </summary>
    private void OnNameCancelled()
    {
        Debug.Log("[CharacterCreationController] 이름 입력 취소");
        ClearNameError(); // 🔧 개선 3: 오류 메시지 지우기
        ShowClassSelectionPanel();
    }
    
    /// <summary>
    /// 로비로 돌아가기
    /// </summary>
    private void OnBackToLobby()
    {
        Debug.Log("[CharacterCreationController] 로비로 돌아갑니다");
        
        HideAllPanels();
        ClearNameError(); // 🔧 개선 3: 오류 메시지 지우기
        targetSlotIndex = -1;
        inputPlayerName = "";
        
        // 🔧 개선 2: 직접 참조 사용
        if (lobbyUIController != null)
        {
            lobbyUIController.OnBackToLobby();
        }
        else
        {
            Debug.LogError("[CharacterCreationController] LobbyUIController 참조가 없습니다!");
        }
    }
}

/// <summary>
/// 클래스 정보 구조체
/// </summary>
[System.Serializable]
public class ClassInfo
{
    public PlayerType playerType;
    public string className;
    public int attack;
    public int defense;
    public int speed;
    public string description;
}
