using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // 씬 전환을 위해 필요

public class LobbyUIController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text playerNameInfoText;
    public Button warriorButton;
    public Button assassinButton;
    public Button mapButton; // Map 버튼 연결
    public Image warriorPanelImage;   // Warrior 패널의 배경 Image
    public Image assassinPanelImage;  // Assassin 패널의 배경 Image

    void Start()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        // UI 요소들의 null 체크
        if (playerNameInfoText == null)
        {
            Debug.LogError("[LobbyUIController] playerNameInfoText가 연결되지 않았습니다!");
        }
        
        if (warriorButton == null)
        {
            Debug.LogError("[LobbyUIController] warriorButton이 연결되지 않았습니다!");
        }
        
        if (assassinButton == null)
        {
            Debug.LogError("[LobbyUIController] assassinButton이 연결되지 않았습니다!");
        }
        
        if (mapButton == null)
        {
            Debug.LogError("[LobbyUIController] mapButton이 연결되지 않았습니다!");
        }
        
        if (warriorPanelImage == null)
        {
            Debug.LogError("[LobbyUIController] warriorPanelImage가 연결되지 않았습니다!");
        }
        
        if (assassinPanelImage == null)
        {
            Debug.LogError("[LobbyUIController] assassinPanelImage가 연결되지 않았습니다!");
        }

        // 게임 시작 시 선택 상태를 'None'으로 초기화하여 항상 캐릭터 선택부터 시작하도록 합니다.
        OnClassSelected("None");

        // 버튼에 클릭 이벤트 연결 (null 체크 포함)
        if (warriorButton != null)
            warriorButton.onClick.AddListener(() => OnClassSelected("Warrior"));
            
        if (assassinButton != null)
            assassinButton.onClick.AddListener(() => OnClassSelected("Assassin"));
            
        if (mapButton != null)
            mapButton.onClick.AddListener(OnMapButtonClicked); // Map 버튼 리스너 연결
            
        Debug.Log("[LobbyUIController] 초기화 완료");
    }

    public void OnClassSelected(string className)
    {
        if (LobbyManager.Instance == null)
        {
            Debug.LogError("[LobbyUIController] LobbyManager가 없습니다!");
            return;
        }

        if (className == "Warrior")
            LobbyManager.Instance.SelectClass(PlayerType.Warrior, "Sword");
        else if (className == "Assassin")
            LobbyManager.Instance.SelectClass(PlayerType.Assassin, "Bow");
        else
            LobbyManager.Instance.SelectClass(PlayerType.None, "");

        UpdateClassSelectionUI(className);
    }

    void UpdateClassSelectionUI(string className)
    {
        if (playerNameInfoText != null)
        {
            playerNameInfoText.text = className == "None" ? "Character Select" : className;
        }

        if (warriorPanelImage != null)
            SetPanelActive(warriorPanelImage, className == "Warrior");
            
        if (assassinPanelImage != null)
            SetPanelActive(assassinPanelImage, className == "Assassin");
    }

    void SetPanelActive(Image panelImage, bool isActive)
    {
        if (panelImage != null)
        {
            panelImage.color = isActive ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
    }

    public void OnLogoutButton()
    {
        Debug.Log("로그아웃 버튼 클릭!");
    }

    public void OnCharacterSelectButton()
    {
        Debug.Log("캐릭터선택창 버튼 클릭!");
    }

    public void OnGameMapButton()
    {
        OnMapButtonClicked(); // 새로운 메서드로 리다이렉트
    }

    /// <summary>
    /// Map 버튼 클릭 처리 (StageSelect 씬으로 이동)
    /// </summary>
    public void OnMapButtonClicked()
    {
        Debug.Log("[LobbyUIController] Map 버튼 클릭! StageSelect 씬으로 이동합니다.");
        
        // 캐릭터가 선택되었는지 확인
        if (LobbyManager.Instance == null)
        {
            Debug.LogError("[LobbyUIController] LobbyManager가 없습니다!");
            return;
        }

        if (LobbyManager.Instance.playerSelection.selectedType == PlayerType.None)
        {
            Debug.LogWarning("[LobbyUIController] 캐릭터를 먼저 선택해주세요!");
            // UI 피드백 추가 가능 (예: 텍스트 깜빡임, 알림 등)
            return;
        }

        // 선택된 캐릭터 정보를 GameManager에 저장
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetRuntimePlayerData(
                LobbyManager.Instance.playerSelection.selectedType,
                LobbyManager.Instance.playerSelection.weaponName
            );
            
            // StageSelect 씬으로 이동
            GameManager.Instance.LoadStageSelectScene();
        }
        else
        {
            Debug.LogError("[LobbyUIController] GameManager가 없습니다!");
        }
    }

    public void OnEnterBattleButton()
    {
        Debug.Log("전투입장 버튼 클릭!");
    }

    // 필요하다면 캐릭터 이미지 클릭도 추가 가능
    public void OnCharacterImageClick(string characterName)
    {
        Debug.Log(characterName + " 캐릭터 클릭!");
    }
} 