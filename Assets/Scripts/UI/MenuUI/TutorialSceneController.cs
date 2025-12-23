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
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("튜토리얼 단계")]
    [SerializeField] private TutorialStepController stepController;
    
    [Header("스킵 버튼 (선택사항)")]
    [SerializeField] private Button skipButton;
    [SerializeField] private GameObject skipButtonObj;
    
    private bool isTutorialCompleted = false;
    private bool isTransitioning = false;
    
    void Start()
    {
        if (enableDebugLogs)
            Debug.Log("[TutorialScene] 튜토리얼 씬 시작");
        
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
        
        if (enableDebugLogs)
            Debug.Log($"[TutorialScene] 스킵 버튼 표시: {showSkipButton}");
    }
    
    /// <summary>
    /// 스킵 버튼 클릭
    /// </summary>
    public void OnSkipButtonClicked()
    {
        if (isTransitioning)
            return;
        
        if (enableDebugLogs)
            Debug.Log("[TutorialScene] 스킵 버튼 클릭 - 튜토리얼 전체 스킵");
        
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
            
            if (enableDebugLogs)
                Debug.Log("[TutorialScene] 시작 컷신 종료 → 튜토리얼 단계 시작");
            
            StartTutorialSteps();
        }
    }
    
    private void StartTutorialSteps()
    {
        if (stepController != null)
        {
            stepController.StartTutorial();
            stepController.OnTutorialCompleted += OnTutorialStepsCompleted;
        }
        else
        {
            Debug.LogError("[TutorialScene] TutorialStepController가 없습니다!");
            // 컨트롤러 없으면 바로 종료 컷신 재생
            OnTutorialStepsCompleted();
        }
    }
    
    private void OnTutorialStepsCompleted()
    {
        if (isTransitioning)
            return;
        
        if (enableDebugLogs)
            Debug.Log("[TutorialScene] 튜토리얼 모든 단계 완료 → 종료 컷신 재생");
        
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
            
            if (enableDebugLogs)
                Debug.Log("[TutorialScene] 종료 컷신 완료 → 로비로 전환");
            
            TransitionToNextScene();
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
