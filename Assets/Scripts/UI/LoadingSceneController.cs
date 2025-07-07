using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class LoadingSceneController : MonoBehaviour
{
    [Header("UI References")]
    public Slider progressBar;
    public TextMeshProUGUI loadingText;
    public GameObject tapToStartObj;

    // 이 static 변수에 다음에 로드할 씬의 이름이 저장됩니다.
    public static string nextSceneName;
    private bool isLoading = false;

    private void Start()
    {
        // 만약 nextSceneName이 비어있다면, Lobby로 설정 (최초 실행)
        if (string.IsNullOrEmpty(nextSceneName))
        {
            nextSceneName = "Lobby";
        }

        // 'Tap to Start' 오브젝트가 연결되어 있고 활성화 되어야 할 때만 보여줌
        if (tapToStartObj != null && nextSceneName == "Lobby")
        {
            tapToStartObj.SetActive(true);
            if(loadingText) loadingText.text = "Tap to Start!";
            if(progressBar) progressBar.value = 0f;
        }
        else // 'Tap to Start'가 없거나, 게임 중 씬 전환일 경우
        {
            if(tapToStartObj) tapToStartObj.SetActive(false);
            if(loadingText) loadingText.text = "Loading...";
            StartCoroutine(LoadSceneProcess());
        }
    }
    
    // "Tap to Start" 버튼이 눌렸을 때 호출됩니다.
    public void OnTapToStart()
    {
        if (isLoading) return;
        isLoading = true;

        if(tapToStartObj) tapToStartObj.SetActive(false);
        if(loadingText) loadingText.text = "Loading...";

        StartCoroutine(LoadSceneProcess());
    }

    // 다른 씬에서 이 함수를 호출하여 씬 전환을 시작합니다.
    public static void LoadScene(string sceneName, string loadingSceneName = "Loading")
    {
        SceneManager.LoadScene(sceneName); // 동기 방식으로 바로 씬 전환
    }

    private IEnumerator LoadSceneProcess()
    {
        // 비동기적으로 다음 씬을 로드합니다.
        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
        op.allowSceneActivation = false; // 씬 로드가 완료되어도 바로 활성화하지 않습니다.

        float timer = 0.0f;
        while (!op.isDone)
        {
            yield return null;

            timer += Time.deltaTime;

            // 로딩 진행률이 90% 이상이면 (거의 완료)
            if (op.progress >= 0.9f)
            {
                // 프로그레스 바를 1로 채우고, 잠시 대기한 후 씬을 활성화합니다.
                progressBar.value = Mathf.Lerp(progressBar.value, 1f, timer);
                if (progressBar.value == 1.0f)
                {
                    op.allowSceneActivation = true;
                    yield break;
                }
            }
            else
            {
                // 프로그레스 바를 실제 진행률에 맞춰 업데이트합니다.
                progressBar.value = Mathf.Lerp(progressBar.value, op.progress, timer);
                if (progressBar.value >= op.progress)
                {
                    timer = 0f;
                }
            }
        }
    }
}