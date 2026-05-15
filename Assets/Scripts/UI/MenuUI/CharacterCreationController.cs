using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

/// <summary>
/// 캐릭터 생성 시스템 컨트롤러 (리팩토링 완료)
/// 책임: 전체 흐름 제어, View 컴포넌트 연결
/// </summary>
public class CharacterCreationController : MonoBehaviour
{
    [Header("=== 외부 참조 ===")]
    public LobbyUIController lobbyUIController;
    
    [Header("=== 캐릭터 생성 UI 패널 ===")]
    public GameObject characterCreationPanel;
    
    [Header("=== View 컴포넌트 ===")]
    public ClassSelectionView classSelectionView;
    public NameInputView nameInputView;
    
    [Header("=== 뒤로가기 버튼 ===")]
    public Button backToLobbyButton;
    
    // 내부 상태
    private int targetSlotIndex = -1;
    private PlayerType selectedClass = PlayerType.Warrior;
    private string inputPlayerName = "";
    
    // Validator
    private CharacterNameValidator nameValidator = new CharacterNameValidator();
    
    // 클래스별 정보
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
            className = "Archer", 
            attack = 9, 
            defense = 5, 
            speed = 10, 
            description = "빠르고 정확한 궁수\n높은 이동속도와 크리티컬 확률" 
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
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (classSelectionView != null)
        {
            classSelectionView.OnClassSelected -= OnClassSelected;
            classSelectionView.OnClassConfirmed -= OnClassConfirmed;
        }
        
        if (nameInputView != null)
        {
            nameInputView.OnNameConfirmed -= OnNameConfirmed;
            nameInputView.OnNameCancelled -= OnNameCancelled;
        }
    }
    
    private void InitializeUI()
    {
        
        // LobbyUIController 참조 검증
        if (lobbyUIController == null)
        {
            lobbyUIController = FindObjectOfType<LobbyUIController>();
            if (lobbyUIController == null)
            {
                Debug.LogError("[CharacterCreationController] LobbyUIController 참조가 없습니다!");
            }
        }
        
        // View 컴포넌트 검증
        if (classSelectionView == null)
        {
            Debug.LogError("[CharacterCreationController] ClassSelectionView 참조가 없습니다!");
        }
        if (nameInputView == null)
        {
            Debug.LogError("[CharacterCreationController] NameInputView 참조가 없습니다!");
        }
        
        // View 이벤트 구독
        if (classSelectionView != null)
        {
            classSelectionView.OnClassSelected += OnClassSelected;
            classSelectionView.OnClassConfirmed += OnClassConfirmed;
        }
        
        if (nameInputView != null)
        {
            nameInputView.OnNameConfirmed += OnNameConfirmed;
            nameInputView.OnNameCancelled += OnNameCancelled;
        }
        
        // 뒤로가기 버튼 이벤트 연결
        if (backToLobbyButton != null) 
            backToLobbyButton.onClick.AddListener(OnBackToLobby);
        
        // 초기 상태 설정
        HideAllPanels();
        
    }
    
    /// <summary>
    /// 캐릭터 생성 시작
    /// </summary>
    public void StartCharacterCreation(int slotIndex)
    {
        
        // 슬롯 유효성 검사
        if (!IsValidSlotForCreation(slotIndex))
        {
            Debug.LogError($"[CharacterCreation] 슬롯 {slotIndex} 유효성 검사 실패!");
            return;
        }
        
        targetSlotIndex = slotIndex;
        
        // 전체 캐릭터 생성 패널을 최상위로 (Z-Order)
        BringCharacterCreationToFront();
        
        // 클래스 선택 View 표시
        if (classSelectionView != null)
        {
            classSelectionView.ShowPanel();
        }
        
        // 이름 입력 View 숨김
        if (nameInputView != null)
        {
            nameInputView.HidePanel();
        }
        
    }
    
    /// <summary>
    /// 캐릭터 생성 패널을 최상위로 가져오기 (Z-Order)
    /// </summary>
    private void BringCharacterCreationToFront()
    {
        if (characterCreationPanel == null)
        {
            Debug.LogError("[CharacterCreation] characterCreationPanel이 null입니다!");
            return;
        }
        
        // 전체 캐릭터 생성 패널 활성화
        characterCreationPanel.SetActive(true);
        
        // 로비의 다른 패널들보다 앞으로
        characterCreationPanel.transform.SetAsLastSibling();
        
    }
    
    /// <summary>
    /// 슬롯 생성 가능 여부 검사
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
    /// 모든 패널 숨기기
    /// </summary>
    private void HideAllPanels()
    {
        if (characterCreationPanel != null) 
            characterCreationPanel.SetActive(false);
        
        if (classSelectionView != null) 
            classSelectionView.HidePanel();
        
        if (nameInputView != null) 
            nameInputView.HidePanel();
    }
    
    /// <summary>
    /// 클래스 선택 이벤트 (ClassSelectionView에서 호출)
    /// </summary>
    private void OnClassSelected(PlayerType playerType)
    {
        selectedClass = playerType;
        
        // 클래스 정보 업데이트
        var classInfo = Array.Find(classInfos, info => info.playerType == selectedClass);
        if (classInfo != null && classSelectionView != null)
        {
            classSelectionView.UpdateClassInfo(classInfo);
        }
        
    }
    
    /// <summary>
    /// 클래스 확정 이벤트 (ClassSelectionView에서 호출)
    /// </summary>
    private void OnClassConfirmed(PlayerType playerType)
    {
        selectedClass = playerType;
        
        // 클래스 선택 View 숨김
        if (classSelectionView != null)
        {
            classSelectionView.HidePanel();
        }
        
        // 이름 입력 View 표시
        if (nameInputView != null)
        {
            nameInputView.ShowPanel();
        }
        
    }
    
    /// <summary>
    /// 이름 확정 이벤트 (NameInputView에서 호출)
    /// </summary>
    private void OnNameConfirmed(string playerName)
    {
        // Validator로 유효성 검사
        var validationResult = nameValidator.ValidateName(playerName);
        
        if (!validationResult.IsValid)
        {
            // 에러 메시지 표시
            if (nameInputView != null)
            {
                nameInputView.ShowError(validationResult.ErrorMessage);
            }
            return;
        }
        
        // 유효성 검사 통과 - 캐릭터 생성
        inputPlayerName = playerName;
        CreateCharacter();
    }
    
    /// <summary>
    /// 이름 입력 취소 이벤트 (NameInputView에서 호출)
    /// </summary>
    private void OnNameCancelled()
    {
        // 이름 입력 View 숨김
        if (nameInputView != null)
        {
            nameInputView.HidePanel();
        }
        
        // 클래스 선택 View 표시
        if (classSelectionView != null)
        {
            classSelectionView.ShowPanel();
        }
        
    }
    
    /// <summary>
    /// 캐릭터 생성 실행
    /// </summary>
    private void CreateCharacter()
    {
        Dbg.Log($"[CharacterCreationController] 캐릭터 생성: 슬롯{targetSlotIndex}, {selectedClass}, {inputPlayerName}");
        
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("[CharacterCreationController] PlayerDataManager가 없습니다!");
            return;
        }
        
        // PlayerDataManager를 통해 캐릭터 생성
        bool success = PlayerDataManager.Instance.CreateNewSlot(targetSlotIndex, selectedClass, inputPlayerName);
        
        if (success)
        {
            OnBackToLobby();
        }
        else
        {
            Debug.LogError($"[CharacterCreationController] ❌ 캐릭터 생성 실패!");
            
            // View에 에러 메시지 표시
            if (nameInputView != null)
            {
                nameInputView.ShowError("캐릭터 생성에 실패했습니다. 잠시 후 다시 시도해주세요.");
            }
        }
    }
    
    /// <summary>
    /// 로비로 돌아가기
    /// </summary>
    private void OnBackToLobby()
    {
        
        // 전체 캐릭터 생성 패널 숨기기
        HideAllPanels();
        
        // 내부 상태 초기화
        targetSlotIndex = -1;
        inputPlayerName = "";
        
        // 로비를 최상위로 (저장 포함)
        if (lobbyUIController != null)
        {
            lobbyUIController.OnBackToLobby();
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
