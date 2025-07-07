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
    public Button playButton; // Play 버튼 연결
    public Image warriorPanelImage;   // Warrior 패널의 배경 Image
    public Image assassinPanelImage;  // Assassin 패널의 배경 Image

    void Start()
    {
        // 게임 시작 시 선택 상태를 'None'으로 초기화하여 항상 캐릭터 선택부터 시작하도록 합니다.
        OnClassSelected("None");

        // 버튼에 클릭 이벤트 연결
        warriorButton.onClick.AddListener(() => OnClassSelected("Warrior"));
        assassinButton.onClick.AddListener(() => OnClassSelected("Assassin"));
        playButton.onClick.AddListener(OnClickPlay); // Play 버튼 리스너 연결
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
        playerNameInfoText.text = className == "None" ? "Character Select" : className;

        SetPanelActive(warriorPanelImage, className == "Warrior");
        SetPanelActive(assassinPanelImage, className == "Assassin");
    }

    void SetPanelActive(Image panelImage, bool isActive)
    {
        panelImage.color = isActive ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
    }
    
    public void OnClickPlay()
    {
        if (LobbyManager.Instance == null)
        {
            Debug.LogError("[LobbyUIController] LobbyManager가 없습니다!");
            return;
        }

        if (LobbyManager.Instance.playerSelection.selectedType == PlayerType.None)
        {
            Debug.LogWarning("Character Select!!");
            return;
        }

        Debug.Log($"[Lobby] 로비를 떠납니다. 선택된 클래스: {LobbyManager.Instance.playerSelection.selectedType}, 무기: {LobbyManager.Instance.playerSelection.weaponName}");
        
        // ⭐ 수정: LobbyManager의 StartGame 메서드를 사용하여 데이터를 전달
        LobbyManager.Instance.StartGame("Scene1");
    }

    public void OnPlayButton()
    {
        Debug.Log("Play 버튼 클릭! Scene1로 이동합니다.");
        OnClickPlay(); // 동일한 로직 사용
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
        Debug.Log("게임맵 버튼 클릭!");
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