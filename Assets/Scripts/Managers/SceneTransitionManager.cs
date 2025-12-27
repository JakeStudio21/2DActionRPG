using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 씬 전환 관리자 (싱글톤, DontDestroyOnLoad)
/// 모든 씬 전환에 페이드인/페이드아웃 효과를 적용
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static SceneTransitionManager _instance;
    public static SceneTransitionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 씬에서 찾기
                _instance = FindObjectOfType<SceneTransitionManager>();
                
                // 없으면 생성
                if (_instance == null)
                {
                    GameObject go = new GameObject("SceneTransitionManager");
                    _instance = go.AddComponent<SceneTransitionManager>();
                }
            }
            return _instance;
        }
    }
    
    [Header("=== 페이드 패널 ===")]
    [SerializeField] private Canvas transitionCanvas;
    [SerializeField] private Image fadePanel;
    
    [Header("=== 페이드 설정 ===")]
    [SerializeField] private float defaultFadeOutDuration = 0.5f;
    [SerializeField] private float defaultFadeInDuration = 0.5f;
    [SerializeField] private Color defaultFadeColor = Color.black;
    
    [Header("=== 디버그 ===")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 상태
    private bool isTransitioning = false;
    private Coroutine currentTransition = null;
    
    void Awake()
    {
        // 싱글톤 설정
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // 자동 초기화
        if (transitionCanvas == null || fadePanel == null)
        {
            InitializeTransitionUI();
        }
        
        if (enableDebugLogs)
            Debug.Log("[SceneTransitionManager] 초기화 완료 (DontDestroyOnLoad)");
    }
    
    /// <summary>
    /// 전환 UI 자동 생성
    /// </summary>
    private void InitializeTransitionUI()
    {
        // Canvas 생성
        if (transitionCanvas == null)
        {
            GameObject canvasObj = new GameObject("TransitionCanvas");
            canvasObj.transform.SetParent(transform);
            
            transitionCanvas = canvasObj.AddComponent<Canvas>();
            transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transitionCanvas.sortingOrder = 9999; // 최상위
            
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // FadePanel 생성
        if (fadePanel == null)
        {
            GameObject panelObj = new GameObject("FadePanel");
            panelObj.transform.SetParent(transitionCanvas.transform, false);
            
            fadePanel = panelObj.AddComponent<Image>();
            fadePanel.color = new Color(0, 0, 0, 0); // 초기 투명
            fadePanel.raycastTarget = false; // 🆕 초기 상태는 클릭 허용
            
            // 전체 화면 크기
            RectTransform rt = panelObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        
        if (enableDebugLogs)
            Debug.Log("[SceneTransitionManager] 전환 UI 자동 생성 완료");
    }
    
    /// <summary>
    /// 페이드 효과와 함께 씬 전환 (기본 설정)
    /// </summary>
    public void LoadSceneWithTransition(string sceneName)
    {
        LoadSceneWithTransition(sceneName, defaultFadeOutDuration, defaultFadeInDuration, defaultFadeColor);
    }
    
    /// <summary>
    /// 페이드 효과와 함께 씬 전환 (커스터마이징)
    /// </summary>
    public void LoadSceneWithTransition(string sceneName, float fadeOutTime, float fadeInTime, Color fadeColor)
    {
        if (isTransitioning)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[SceneTransitionManager] 이미 씬 전환 중입니다. 요청 무시: {sceneName}");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[SceneTransitionManager] 씬 전환 시작: {sceneName}");
        
        currentTransition = StartCoroutine(TransitionCoroutine(sceneName, fadeOutTime, fadeInTime, fadeColor));
    }
    
    /// <summary>
    /// 즉시 씬 전환 (페이드 없음)
    /// </summary>
    public void LoadSceneImmediate(string sceneName)
    {
        if (enableDebugLogs)
            Debug.Log($"[SceneTransitionManager] 즉시 씬 전환: {sceneName}");
        
        SceneManager.LoadScene(sceneName);
    }
    
    /// <summary>
    /// 씬 전환 코루틴
    /// </summary>
    private IEnumerator TransitionCoroutine(string sceneName, float fadeOutTime, float fadeInTime, Color fadeColor)
    {
        isTransitioning = true;
        
        // 1단계: 페이드 아웃 (현재 씬 가리기)
        if (enableDebugLogs)
            Debug.Log($"[SceneTransitionManager] 페이드 아웃 시작 ({fadeOutTime}초)");
        
        yield return StartCoroutine(FadeOut(fadeOutTime, fadeColor));
        
        // 2단계: 씬 로딩
        if (enableDebugLogs)
            Debug.Log($"[SceneTransitionManager] 씬 로딩 중: {sceneName}");
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // 로딩 완료 대기
        while (!asyncLoad.isDone)
        {
            // 진행률 출력 (선택 사항)
            // Debug.Log($"로딩 진행률: {asyncLoad.progress * 100}%");
            yield return null;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[SceneTransitionManager] 씬 로드 완료: {sceneName}");
        
        // 3단계: 페이드 인 (새 씬 표시)
        if (enableDebugLogs)
            Debug.Log($"[SceneTransitionManager] 페이드 인 시작 ({fadeInTime}초)");
        
        yield return StartCoroutine(FadeIn(fadeInTime));
        
        if (enableDebugLogs)
            Debug.Log($"[SceneTransitionManager] 씬 전환 완료: {sceneName}");
        
        isTransitioning = false;
        currentTransition = null;
    }
    
    /// <summary>
    /// 페이드 아웃 (투명 → 불투명)
    /// </summary>
    private IEnumerator FadeOut(float duration, Color fadeColor)
    {
        if (fadePanel == null)
        {
            Debug.LogError("[SceneTransitionManager] FadePanel이 null입니다!");
            yield break;
        }
        
        // 🆕 페이드 시작: 클릭 차단 활성화
        fadePanel.raycastTarget = true;
        if (enableDebugLogs)
            Debug.Log("[SceneTransitionManager] 페이드 아웃 중 - 클릭 차단 활성화");
        
        float elapsedTime = 0f;
        Color startColor = fadeColor;
        startColor.a = 0f;
        Color endColor = fadeColor;
        endColor.a = 1f;
        
        fadePanel.color = startColor;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime; // Time.timeScale 영향 안 받음
            float t = Mathf.Clamp01(elapsedTime / duration);
            fadePanel.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
        
        fadePanel.color = endColor;
    }
    
    /// <summary>
    /// 페이드 인 (불투명 → 투명)
    /// </summary>
    private IEnumerator FadeIn(float duration)
    {
        if (fadePanel == null)
        {
            Debug.LogError("[SceneTransitionManager] FadePanel이 null입니다!");
            yield break;
        }
        
        float elapsedTime = 0f;
        Color startColor = fadePanel.color;
        Color endColor = startColor;
        endColor.a = 0f;
        
        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            fadePanel.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
        
        fadePanel.color = endColor;
        
        // 🆕 페이드 완료: 클릭 차단 해제
        fadePanel.raycastTarget = false;
        if (enableDebugLogs)
            Debug.Log("[SceneTransitionManager] 페이드 인 완료 - 클릭 차단 해제 ✅");
    }
    
    /// <summary>
    /// 페이드 설정 변경 (런타임)
    /// </summary>
    public void SetFadeSettings(float fadeOutTime, float fadeInTime, Color fadeColor)
    {
        defaultFadeOutDuration = fadeOutTime;
        defaultFadeInDuration = fadeInTime;
        defaultFadeColor = fadeColor;
        
        if (enableDebugLogs)
            Debug.Log($"[SceneTransitionManager] 페이드 설정 변경: Out={fadeOutTime}, In={fadeInTime}, Color={fadeColor}");
    }
    
    /// <summary>
    /// 현재 전환 중인지 확인
    /// </summary>
    public bool IsTransitioning()
    {
        return isTransitioning;
    }
    
    /// <summary>
    /// 즉시 페이드 아웃 (화면을 검은색으로)
    /// 컷신 종료 직후 사용하여 갭 없이 전환
    /// </summary>
    public void FadeOutImmediate()
    {
        if (fadePanel == null)
        {
            Debug.LogError("[SceneTransitionManager] FadePanel이 null입니다!");
            return;
        }
        
        // 즉시 검은 화면으로
        Color fadeColor = defaultFadeColor;
        fadeColor.a = 1f;
        fadePanel.color = fadeColor;
        fadePanel.raycastTarget = true; // 클릭 차단
        
        if (enableDebugLogs)
            Debug.Log("[SceneTransitionManager] 즉시 페이드 아웃 (검은 화면) ✅");
    }
    
    /// <summary>
    /// 페이드 인 시작 (검은 화면 → 투명)
    /// 외부에서 호출 가능한 public 메서드
    /// </summary>
    public void StartFadeIn(float duration = -1f)
    {
        if (duration < 0)
            duration = defaultFadeInDuration;
        
        StartCoroutine(FadeIn(duration));
    }
    
    /// <summary>
    /// 즉시 페이드 아웃 후 씬 전환 (컷신 종료용)
    /// </summary>
    public void FadeOutImmediateAndLoadScene(string sceneName)
    {
        // 1. 즉시 검은 화면으로
        FadeOutImmediate();
        
        // 2. 약간의 딜레이 후 씬 전환 (검은 화면 유지하며)
        StartCoroutine(LoadSceneAfterBlackScreen(sceneName));
    }
    
    /// <summary>
    /// 검은 화면 상태에서 씬 전환
    /// </summary>
    private IEnumerator LoadSceneAfterBlackScreen(string sceneName)
    {
        // 검은 화면 유지 (0.2초)
        yield return new WaitForSecondsRealtime(0.2f);
        
        if (enableDebugLogs)
            Debug.Log($"[SceneTransitionManager] 검은 화면에서 씬 로딩 시작: {sceneName}");
        
        // 씬 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[SceneTransitionManager] 씬 로드 완료: {sceneName}");
        
        // 페이드 인 (검은 화면 → 투명)
        yield return StartCoroutine(FadeIn(defaultFadeInDuration));
    }
}

