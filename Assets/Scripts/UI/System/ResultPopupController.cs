using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class ResultPopupController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject popupPanel;        // ResultPopupPanel
    public GameObject victoryImage;      // Victory 이미지
    public GameObject defeatImage;       // Defeat 이미지
    public Button confirmButton;         // OK 버튼

    void Awake()
    {
        popupPanel.SetActive(false);     // 팝업 패널 숨기기
        victoryImage.SetActive(false);   // Victory 이미지 숨기기
        defeatImage.SetActive(false);    // Defeat 이미지 숨기기
        confirmButton.onClick.AddListener(OnConfirm);
    }

    public void Show(bool isVictory)
    {
        if (gameObject == null) return;

        popupPanel.SetActive(true);
        
        // Victory/Defeat 이미지 중 하나만 보이게 하기
        victoryImage.SetActive(isVictory);
        defeatImage.SetActive(!isVictory);
    }

    void OnConfirm()
    {
        // 중복 클릭 방지를 위해 리스너를 잠시 제거하고, 코루틴을 통해 로비로 돌아갑니다.
        confirmButton.interactable = false;
        StartCoroutine(ReturnToLobbyRoutine());
    }

    IEnumerator ReturnToLobbyRoutine()
    {
        Time.timeScale = 1f;

        // ✅ Unity가 자동으로 오브젝트를 정리하므로 수동 파괴 제거
        // 씬 전환 시 모든 오브젝트는 자동으로 정리됨
        
        yield return new WaitForEndOfFrame();

        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }

    // void OnConfirm()
    // {
    //     Time.timeScale = 1f; // 혹시 멈춰있으면 재개
    //     SceneManager.LoadScene("Lobby");
    // }
} 