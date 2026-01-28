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
        // 처음에는 팝업을 숨겨둡니다.
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("[PauseMenuController] pauseMenuPanel이 null입니다!");
        }

        // 각 버튼에 함수를 연결합니다.
        if (returnToLobbyButton != null)
            returnToLobbyButton.onClick.AddListener(ReturnToLobby);
        else
            Debug.LogWarning("[PauseMenuController] returnToLobbyButton이 null입니다!");
            
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinuePlaying);
        else
            Debug.LogWarning("[PauseMenuController] continueButton이 null입니다!");
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

        // 🔧 의미 있는 이벤트: 로비 복귀 → 저장
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
        {
            PlayerDataManager.Instance.SaveOnMeaningfulEvent("PauseMenu_ReturnToLobby");
        }

        // ✅ Unity가 자동으로 오브젝트를 정리하므로 수동 파괴 제거
        // 씬 전환 시 모든 오브젝트는 자동으로 정리됨
        
        yield return new WaitForEndOfFrame();

        SceneManager.LoadScene("Lobby");
    }
} 