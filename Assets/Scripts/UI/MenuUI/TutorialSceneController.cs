using UnityEngine;
using CutsceneSystem;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// 튜토리얼 씬 전용 컨트롤러
/// 튜토리얼 진행 → 완료 시 다음 씬으로 전환
/// </summary>
public class TutorialSceneController : MonoBehaviour
{
    [Header("컷신 설정")]
    [SerializeField] private string tutorialStartCutsceneId = "Tutorial_Start";
    [SerializeField] private string tutorialEndCutsceneId = "Tutorial_End";
    
    [Header("튜토리얼 단계")]
    [SerializeField] private TutorialStepController stepController;
    
    [Header("스킵 버튼 (선택사항)")]
    [SerializeField] private Button skipButton;
    [SerializeField] private GameObject skipButtonObj;
    
    private bool isTutorialCompleted = false;
    private bool isTransitioning = false;
    
    void Start()
    {
            Dbg.Log("[TutorialScene] 튜토리얼 씬 시작");
        
        // BGM 재생
        if (BGMController.Instance != null)
        {
            BGMController.Instance.PlayDefaultBGM("bgm.tutorial", null);
        }
        
        // 🆕 스킵 버튼 설정 (다시보기 모드에만 표시)
        SetupSkipButton();
        
        // 튜토리얼 시작 컷신 재생
        StartCoroutine(PlayStartCutscene());
    }
    
    /// <summary>
    /// 스킵 버튼 설정
    /// </summary>
    private void SetupSkipButton()
    {
        // 다시보기 모드인 경우에만 스킵 버튼 표시
        bool showSkipButton = (GameManager.Instance != null && 
                               GameManager.Instance.currentFlow == GameManager.FlowType.ReplayTutorial);
        
        if (skipButtonObj != null)
        {
            skipButtonObj.SetActive(showSkipButton);
        }
        
        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }
        
    }
    
    /// <summary>
    /// 스킵 버튼 클릭
    /// </summary>
    public void OnSkipButtonClicked()
    {
        if (isTransitioning)
            return;
        
        
        // 컷신 강제 종료
        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.StopCutscene();
        }
        
        // 튜토리얼 강제 완료
        isTutorialCompleted = true;
        TransitionToNextScene();
    }
    
    private IEnumerator PlayStartCutscene()
    {
        yield return null;
        
        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.OnCutsceneEnd += OnStartCutsceneEnd;
            CutsceneManager.Instance.PlayCutscene(tutorialStartCutsceneId);
        }
        else
        {
            // CutsceneManager 없으면 바로 튜토리얼 시작
            Debug.LogWarning("[TutorialScene] CutsceneManager가 없습니다. 바로 튜토리얼 시작");
            StartTutorialSteps();
        }
    }
    
    private void OnStartCutsceneEnd(string cutsceneId)
    {
        if (cutsceneId == tutorialStartCutsceneId)
        {
            CutsceneManager.Instance.OnCutsceneEnd -= OnStartCutsceneEnd;
            
            
            // ⭐ 컷신 종료 후 페이드 인으로 튜토리얼 씬 표시
            StartCoroutine(FadeInAndStartTutorial());
        }
    }
    
    private IEnumerator FadeInAndStartTutorial()
    {
        // 페이드 인 (검은 화면 → 튜토리얼 씬)
        if (SceneTransitionManager.Instance != null)
        {
            
            SceneTransitionManager.Instance.StartFadeIn(0.5f);
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // 튜토리얼 단계 시작
        StartTutorialSteps();
    }
    
    private void StartTutorialSteps()
    {
        if (stepController != null)
        {
            // ⭐ 플레이어 스폰을 기다린 후 튜토리얼 시작
            StartCoroutine(WaitForPlayerAndStartTutorial());
        }
        else
        {
            Debug.LogError("[TutorialScene] TutorialStepController가 없습니다!");
            // 컨트롤러 없으면 바로 종료 컷신 재생
            OnTutorialStepsCompleted();
        }
    }
    
    /// <summary>
    /// 플레이어 스폰을 기다린 후 튜토리얼 시작
    /// </summary>
    private IEnumerator WaitForPlayerAndStartTutorial()
    {
        
        // PlayerController가 나타날 때까지 대기
        int maxRetries = 50; // 최대 5초 대기 (0.1초 * 50)
        int retryCount = 0;
        
        while (retryCount < maxRetries)
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            
            if (player != null)
            {
                
                // 추가로 0.5초 대기 (컴포넌트 초기화 완료 보장)
                yield return new WaitForSeconds(0.5f);
                
                // 튜토리얼 시작
                stepController.StartTutorial();
                stepController.OnTutorialCompleted += OnTutorialStepsCompleted;
                
                yield break;
            }
            
            retryCount++;
            yield return new WaitForSeconds(0.1f);
        }
        
        // 타임아웃
        Debug.LogError($"[TutorialScene] ❌ 플레이어를 찾을 수 없습니다! ({maxRetries * 0.1f}초 대기)");
        Debug.LogError("[TutorialScene] TutorialPlayerSpawner가 제대로 작동하지 않는 것 같습니다!");
        
        // 그래도 튜토리얼 시작 시도
        stepController.StartTutorial();
        stepController.OnTutorialCompleted += OnTutorialStepsCompleted;
    }
    
    private void OnTutorialStepsCompleted()
    {
        if (isTransitioning)
            return;
        
        
        if (stepController != null)
        {
            stepController.OnTutorialCompleted -= OnTutorialStepsCompleted;
        }
        
        isTutorialCompleted = true;
        
        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.OnCutsceneEnd += OnEndCutsceneEnd;
            CutsceneManager.Instance.PlayCutscene(tutorialEndCutsceneId);
        }
        else
        {
            TransitionToNextScene();
        }
    }
    
    private void OnEndCutsceneEnd(string cutsceneId)
    {
        if (cutsceneId == tutorialEndCutsceneId)
        {
            CutsceneManager.Instance.OnCutsceneEnd -= OnEndCutsceneEnd;
            
            
            // ⭐ Tutorial 완료 알림 (최초 실행 플래그 저장)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnTutorialCompleted();
            }
            
            // ⭐ 즉시 검은 화면 + 씬 전환 (튜토리얼 씬이 전혀 보이지 않음)
            if (SceneTransitionManager.Instance != null)
            {
                
                SceneTransitionManager.Instance.FadeOutImmediateAndLoadScene("Lobby");
                isTransitioning = true;
            }
            else
            {
                // Fallback: 기존 방식
                TransitionToNextScene();
            }
        }
    }
    
    private void TransitionToNextScene()
    {
        if (isTransitioning)
            return;
        
        isTransitioning = true;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ProceedToNextScene();
        }
        else
        {
            Debug.LogError("[TutorialScene] GameManager가 없습니다!");
        }
    }
    
    void OnDestroy()
    {
        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.OnCutsceneEnd -= OnStartCutsceneEnd;
            CutsceneManager.Instance.OnCutsceneEnd -= OnEndCutsceneEnd;
        }
        
        if (stepController != null)
        {
            stepController.OnTutorialCompleted -= OnTutorialStepsCompleted;
        }
        
        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
        }
    }
}
