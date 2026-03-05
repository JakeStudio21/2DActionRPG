using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button returnToLobbyButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private ResultPopupController resultPopupController; // 🆕 ResultPopupController 참조
    
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
        // Time.timeScale 복구 (게임이 멈춰있던 상태를 복원)
        Time.timeScale = 1f;
        
        // 🔧 의미 있는 이벤트: 게임 포기 → 저장
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
        {
            PlayerDataManager.Instance.SaveOnMeaningfulEvent("PauseMenu_ForfeitGame");
        }
        
        // 일시정지 메뉴 닫기
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        // 🆕 패배 화면 표시 (Result_Defeat)
        if (resultPopupController != null)
        {
            resultPopupController.ShowDefeat();
            Debug.Log("💀 [PauseMenuController] 게임 포기 → 패배 화면 표시");
        }
        else
        {
            Debug.LogError("❌ [PauseMenuController] ResultPopupController가 할당되지 않았습니다! 바로 로비로 이동합니다.");
            // Fallback: ResultPopupController가 없으면 바로 로비 이동
            SceneManager.LoadScene("Lobby");
        }
    }
} 