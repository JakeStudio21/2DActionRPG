using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class ResultPopupController : MonoBehaviour
{
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

        // 1. 플레이어 오브젝트 파괴
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) { Destroy(player); }

        // 2. 각종 매니저들의 부모 오브젝트 파괴
        // GameObject managers = GameObject.Find("Managers");
        // if (managers != null) { Destroy(managers); }

        // 3. 씬에 독립적으로 존재하는 싱글톤 오브젝트들 파괴
        if (Stamina.Instance != null) { Destroy(Stamina.Instance.gameObject); }

        // Destroy 명령이 실행될 시간을 벌어주기 위해 한 프레임 대기합니다.
        yield return new WaitForEndOfFrame();

        UnityEngine.SceneManagement.SceneManager.LoadScene("Lobby");
    }

    // void OnConfirm()
    // {
    //     Time.timeScale = 1f; // 혹시 멈춰있으면 재개
    //     SceneManager.LoadScene("Lobby");
    // }
}
