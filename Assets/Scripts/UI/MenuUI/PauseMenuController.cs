using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button returnToLobbyButton;
    [SerializeField] private Button continueButton;

    private void Start()
    {
        Debug.Log("TimeScale at Start: " + Time.timeScale);
        // 처음에는 팝업을 숨겨둡니다.
        pauseMenuPanel.SetActive(false);

        // 각 버튼에 함수를 연결합니다.
        returnToLobbyButton.onClick.AddListener(ReturnToLobby);
        continueButton.onClick.AddListener(ContinuePlaying);
    }

    // 이 함수는 인게임 UI의 '나가기' 버튼에 연결됩니다.
    public void ShowPauseMenu()
    {
        Time.timeScale = 0f; // 게임을 멈춥니다.
        pauseMenuPanel.SetActive(true);
    }

    // '계속 플레이' 버튼에 연결됩니다.
    void ContinuePlaying()
    {
        Time.timeScale = 1f; // 게임을 다시 시작합니다.
        pauseMenuPanel.SetActive(false);
        var playerController = FindObjectOfType<PlayerController>();
        playerController.ReEnableControls();
        CameraController.Instance.SetPlayerCameraFollow(); // 카메라 추적 재설정
    }

    // '로비로 이동' 버튼에 연결됩니다.
    void ReturnToLobby()
    {
        StartCoroutine(ReturnToLobbyRoutine());
    }

    IEnumerator ReturnToLobbyRoutine()
    {
        Time.timeScale = 1f; // 시간을 다시 흐르게 합니다.

        // DontDestroyOnLoad 오브젝트들을 정리합니다.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) { Destroy(player); }

        // ⭐ 수정: Managers 파괴 코드 활성화하여 중복 방지
        GameObject managers = GameObject.Find("Managers");
        if (managers != null) 
        { 
            Debug.Log("[PauseMenuController] 기존 Managers를 파괴하여 중복을 방지합니다.");
            Destroy(managers); 
        }
        
        if (Stamina.Instance != null) { Destroy(Stamina.Instance.gameObject); }

        yield return new WaitForEndOfFrame();

        SceneManager.LoadScene("Lobby");
    }
} 