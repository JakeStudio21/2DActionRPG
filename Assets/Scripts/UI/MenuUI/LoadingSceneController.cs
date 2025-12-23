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
    
    [Header("🆕 프리로딩 시스템")]
    public LoadingProgressController progressController; // 🆕 진행률 컨트롤러
    public LobbyPreloadManager preloadManager; // 🆕 프리로딩 매니저

    // 이 static 변수에 다음에 로드할 씬의 이름이 저장됩니다.
    public static string nextSceneName;
    private bool isLoading = false;

    private void Start()
    {
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        // UI 요소들의 null 체크
        if (progressBar == null)
        {
            Debug.LogError("[LoadingSceneController] progressBar가 연결되지 않았습니다!");
        }
        
        if (loadingText == null)
        {
            Debug.LogError("[LoadingSceneController] loadingText가 연결되지 않았습니다!");
        }
        
        if (tapToStartObj == null)
        {
            Debug.LogWarning("[LoadingSceneController] tapToStartObj가 연결되지 않았습니다! (선택적 요소)");
        }
        
        // 🆕 프리로딩 시스템 null 체크
        if (progressController == null)
        {
            Debug.LogWarning("[LoadingSceneController] LoadingProgressController가 연결되지 않았습니다!");
        }
        
        if (preloadManager == null)
        {
            Debug.LogWarning("[LoadingSceneController] LobbyPreloadManager가 연결되지 않았습니다!");
        }

        // 🆕 Phase 3: nextSceneName이 비어있다면, GameManager의 로그인 판정 사용
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log("[LoadingSceneController] nextSceneName이 비어있음 - 로그인 씬으로 추정");
            // nextSceneName을 설정하지 않음 (Tap to Start 버튼에서 처리)
        }
        else
        {
            Debug.Log($"[LoadingSceneController] 다음 로드할 씬: {nextSceneName}");
        }

        // 'Tap to Start' 오브젝트가 연결되어 있고 활성화 되어야 할 때만 보여줌
        // 🔧 수정: 최초 실행(nextSceneName 비어있음) 또는 로비 전환 시 버튼 활성화
        if (tapToStartObj != null && (string.IsNullOrEmpty(nextSceneName) || nextSceneName == "Lobby"))
        {
            tapToStartObj.SetActive(true);
            if(loadingText != null) 
                loadingText.text = "Tap to Start!";
            if(progressBar != null) 
                progressBar.value = 0f;
            Debug.Log("[LoadingSceneController] Tap to Start 모드로 초기화");
        }
        else // 'Tap to Start'가 없거나, 게임 중 씬 전환일 경우 (Stage → Loading → Stage)
        {
            if(tapToStartObj != null) 
                tapToStartObj.SetActive(false);
            if(loadingText != null) 
                loadingText.text = "Loading...";
            Debug.Log("[LoadingSceneController] 자동 로딩 모드로 시작");
            StartCoroutine(LoadSceneProcess());
        }
        
        Debug.Log("[LoadingSceneController] 초기화 완료");
    }
    
    // "Tap to Start" 버튼이 눌렸을 때 호출됩니다.
    public void OnTapToStart()
    {
        if (isLoading) 
        {
            Debug.Log("[LoadingSceneController] 이미 로딩 중입니다.");
            return;
        }
        
        isLoading = true;
        Debug.Log("[LoadingSceneController] Tap to Start 버튼이 눌렸습니다.");

        if(tapToStartObj != null) 
            tapToStartObj.SetActive(false);
        if(loadingText != null) 
            loadingText.text = "Loading...";

        // 🆕 Phase 3: nextSceneName이 비어있으면 GameManager의 로그인 판정 사용
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log("[LoadingSceneController] 로그인 완료 - GameManager로 플로우 판정");
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLoginComplete();
                // GameManager가 씬 전환을 처리하므로 여기서는 종료
                return;
            }
            else
            {
                Debug.LogError("[LoadingSceneController] GameManager가 없습니다! 기본 로비로 이동");
                nextSceneName = "Lobby";
            }
        }

        StartCoroutine(LoadSceneProcess());
    }

    // 다른 씬에서 이 함수를 호출하여 씬 전환을 시작합니다.
    public static void LoadScene(string sceneName, string loadingSceneName = "Loading")
    {
        Debug.Log($"[LoadingSceneController] 씬 전환 요청: {sceneName}");
        SceneManager.LoadScene(sceneName); // 동기 방식으로 바로 씬 전환
    }

    private IEnumerator LoadSceneProcess()
    {
        Debug.Log($"[LoadingSceneController] {nextSceneName} 씬 로딩 시작");
        
        // 🆕 로비 씬인 경우 프리로딩 시스템 사용
        if (nextSceneName == "Lobby" && preloadManager != null && progressController != null)
        {
            yield return StartCoroutine(LoadLobbyWithPreloading());
        }
        else
        {
            // 기존 로딩 방식 (다른 씬들)
            yield return StartCoroutine(LoadSceneAsync());
        }
    }
    
    /// <summary>
    /// 🆕 로비 프리로딩 시스템을 사용한 로딩
    /// </summary>
    private IEnumerator LoadLobbyWithPreloading()
    {
        Debug.Log("🚀 [LoadingSceneController] 로비 프리로딩 시스템 시작");
        
        // 🔧 프리로딩 매니저 null 체크 강화
        if (preloadManager == null)
        {
            Debug.LogError("❌ [LoadingSceneController] preloadManager가 null입니다!");
            yield return StartCoroutine(LoadSceneAsync());
            yield break;
        }
        
        if (progressController == null)
        {
            Debug.LogError("❌ [LoadingSceneController] progressController가 null입니다!");
            yield return StartCoroutine(LoadSceneAsync());
            yield break;
        }
        
        Debug.Log("✅ [LoadingSceneController] 프리로딩 컴포넌트 확인 완료");
        
        // 프리로딩 매니저 이벤트 연결
        preloadManager.OnProgressUpdated.AddListener(progressController.UpdateProgress);
        preloadManager.OnStepMessageUpdated.AddListener(progressController.UpdateLoadingMessage);
        preloadManager.OnPreloadingComplete.AddListener(OnPreloadingComplete);
        preloadManager.OnPreloadingFailed.AddListener(OnPreloadingFailed);
        
        Debug.Log("✅ [LoadingSceneController] 이벤트 연결 완료");
        
        // 🔧 먼저 프리로딩 시작 (씬 로드와 병렬 진행)
        Debug.Log("🚀 [LoadingSceneController] 프리로딩 시작");
        preloadManager.StartPreloading();
        
        // 🔧 비동기 씬 로드 시작
        Debug.Log("🔄 [LoadingSceneController] 비동기 씬 로드 시작");
        AsyncOperation sceneLoadOp = SceneManager.LoadSceneAsync(nextSceneName);
        sceneLoadOp.allowSceneActivation = false;
        
        // 씬 로드 진행률 표시와 프리로딩 병렬 진행
        bool sceneLoadComplete = false;
        bool preloadingComplete = false;
        
        while (!sceneLoadComplete || !preloadingComplete)
        {
            // 씬 로드 상태 확인
            if (!sceneLoadComplete && sceneLoadOp.progress >= 0.9f)
            {
                Debug.Log("✅ [LoadingSceneController] 씬 로드 90% 완료");
                sceneLoadComplete = true;
            }
            
            // 프리로딩 상태 확인
            if (!preloadingComplete && preloadManager.IsPreloadingComplete)
            {
                Debug.Log("✅ [LoadingSceneController] 프리로딩 완료");
                preloadingComplete = true;
            }
            
            yield return null;
        }
        
        // 🔧 모든 작업 완료 후 씬 활성화
        Debug.Log("🎯 [LoadingSceneController] 씬 활성화 시작");
        sceneLoadOp.allowSceneActivation = true;
        
        // 씬 활성화 완료 대기
        while (!sceneLoadOp.isDone)
        {
            yield return null;
        }
        
        Debug.Log("✅ [LoadingSceneController] 로비 진입 완료");
    }
    
    /// <summary>
    /// 🆕 프리로딩 완료 콜백
    /// </summary>
    private void OnPreloadingComplete()
    {
        progressController.OnLoadingComplete();
        Debug.Log("✅ [LoadingSceneController] 프리로딩 완료 - 로비 진입 준비됨");
    }
    
    /// <summary>
    /// 🆕 프리로딩 실패 콜백
    /// </summary>
    private void OnPreloadingFailed(string errorMessage)
    {
        Debug.LogError($"❌ [LoadingSceneController] 프리로딩 실패: {errorMessage}");
        progressController.UpdateLoadingMessage("기본 모드로 진입합니다...");
        
        // 기본 로딩 방식으로 폴백
        StartCoroutine(LoadSceneAsync());
    }
    
    /// <summary>
    /// 기존 씬 로딩 방식 (프리로딩 미사용)
    /// </summary>
    private IEnumerator LoadSceneAsync()
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
                if (progressBar != null)
                {
                    progressBar.value = Mathf.Lerp(progressBar.value, 1f, timer);
                    if (progressBar.value >= 0.99f)
                    {
                        progressBar.value = 1.0f;
                        Debug.Log($"[LoadingSceneController] {nextSceneName} 씬 로딩 완료, 씬 활성화");
                        op.allowSceneActivation = true;
                        yield break;
                    }
                }
                else
                {
                    // progressBar가 없으면 바로 활성화
                    op.allowSceneActivation = true;
                    yield break;
                }
            }
            else
            {
                // 프로그레스 바를 실제 진행률에 맞춰 업데이트합니다.
                if (progressBar != null)
                {
                    progressBar.value = Mathf.Lerp(progressBar.value, op.progress, timer);
                    if (progressBar.value >= op.progress)
                    {
                        timer = 0f;
                    }
                }
            }
        }
    }
} 