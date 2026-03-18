using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 인게임 일시정지 흐름 컨트롤러
///
/// ■ 책임 범위 (SRP)
///   - Time.timeScale 일시정지 / 재개 권한을 단독 보유
///   - PauseMenuPanel 활성/비활성 전환
///
/// ■ 동작 흐름
///   QuitButton
///     └─ ShowPauseMenu()  → timeScale=0, PauseMenuPanel ON
///          ├─ [계속하기]  → ContinuePlaying()  → timeScale=1, PauseMenuPanel OFF
///          └─ [로비]      → ReturnToLobby()    → timeScale=1, 패배 화면
///
/// ■ 설정(Settings)은 HUD의 설정 버튼 → StageUI.OnSettingsButtonClicked()로 독립 처리
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    // ───────────────────────────────────────────
    //  Inspector 슬롯
    // ───────────────────────────────────────────

    [Header("🎮 PauseMenu 패널")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("🔘 버튼")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button returnToLobbyButton;

    [Header("🔗 연결 컨트롤러")]
    [SerializeField] private ResultPopupController resultPopupController;

    // ───────────────────────────────────────────
    //  Unity Lifecycle
    // ───────────────────────────────────────────

    private void Start()
    {
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        else
            Debug.LogError("[PauseMenuController] pauseMenuPanel이 연결되지 않았습니다!");

        if (continueButton != null)
            continueButton.onClick.AddListener(ContinuePlaying);
        else
            Debug.LogWarning("[PauseMenuController] continueButton이 연결되지 않았습니다!");

        if (returnToLobbyButton != null)
            returnToLobbyButton.onClick.AddListener(ReturnToLobby);
        else
            Debug.LogWarning("[PauseMenuController] returnToLobbyButton이 연결되지 않았습니다!");
    }

    // ───────────────────────────────────────────
    //  공개 API
    // ───────────────────────────────────────────

    /// <summary>
    /// 인게임 종료 버튼에 연결. 게임 일시정지 + PauseMenuPanel 표시.
    /// </summary>
    public void ShowPauseMenu()
    {
        Time.timeScale = 0f;
        pauseMenuPanel.SetActive(true);
    }

    // ───────────────────────────────────────────
    //  버튼 핸들러
    // ───────────────────────────────────────────

    /// <summary>계속하기 — timeScale 복구 + PauseMenu 닫기</summary>
    private void ContinuePlaying()
    {
        Time.timeScale = 1f;
        pauseMenuPanel.SetActive(false);

        var playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
            playerController.ReEnableControls();

        CameraController.Instance.SetPlayerCameraFollow();
    }

    /// <summary>로비로 이동 — timeScale 복구 + 저장 + 패배 화면</summary>
    private void ReturnToLobby()
    {
        Time.timeScale = 1f;

        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
            PlayerDataManager.Instance.SaveOnMeaningfulEvent("PauseMenu_ForfeitGame");

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);

        if (resultPopupController != null)
        {
            resultPopupController.ShowDefeat();
            Debug.Log("💀 [PauseMenuController] 게임 포기 → 패배 화면 표시");
        }
        else
        {
            Debug.LogError("[PauseMenuController] ResultPopupController가 연결되지 않았습니다! 로비로 직행합니다.");
            SceneManager.LoadScene("Lobby");
        }
    }
}
