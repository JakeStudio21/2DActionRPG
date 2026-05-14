using UnityEngine;
using UnityEngine.SceneManagement;
using CutsceneSystem;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// 인트로 씬 전용 컨트롤러
/// 인트로 컷신 재생 → 완료 시 다음 씬으로 전환
/// </summary>
public class IntroSceneController : MonoBehaviour
{
    [Header("BGM 설정")]
    [SerializeField] private string bgmEventKey = "bgm.intro";

    [Header("컷신 설정")]
    [SerializeField] private string[] introCutsceneIds = { "Intro_001", "Intro_002" };
    
    [Header("스킵 버튼 (선택사항)")]
    [SerializeField] private Button skipButton;
    [SerializeField] private GameObject skipButtonObj;
    
    private int currentCutsceneIndex = 0;
    private bool isTransitioning = false;
    
    void Start()
    {
        
        // BGM 재생
        if (BGMController.Instance != null)
        {
            BGMController.Instance.PlayDefaultBGM(bgmEventKey, null);
        }
        
        // 🆕 스킵 버튼 설정 (다시보기 모드에만 표시)
        SetupSkipButton();
        
        // 컷신 재생
        StartCoroutine(PlayIntroCutsceneSequence());
    }
    
    /// <summary>
    /// 스킵 버튼 설정
    /// </summary>
    private void SetupSkipButton()
    {
        // 다시보기 모드인 경우에만 스킵 버튼 표시
        bool showSkipButton = (GameManager.Instance != null && 
                               GameManager.Instance.currentFlow == GameManager.FlowType.ReplayIntro);
        
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
        
        // 모든 컷신 스킵
        currentCutsceneIndex = introCutsceneIds.Length;
        OnAllCutscenesCompleted();
    }
    
    private IEnumerator PlayIntroCutsceneSequence()
    {
        yield return null;
        
        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.OnCutsceneEnd += OnCutsceneEnd;
            CutsceneManager.Instance.OnCutsceneSkip += OnCutsceneSkip;
            
            // 첫 번째 컷신 재생
            PlayCurrentCutscene();
        }
        else
        {
            Debug.LogError("[IntroScene] CutsceneManager가 없습니다!");
            OnAllCutscenesCompleted();
        }
    }
    
    private void PlayCurrentCutscene()
    {
        if (currentCutsceneIndex < introCutsceneIds.Length)
        {
            string cutsceneId = introCutsceneIds[currentCutsceneIndex];
            
            
            CutsceneManager.Instance.PlayCutscene(cutsceneId);
        }
        else
        {
            OnAllCutscenesCompleted();
        }
    }
    
    private void OnCutsceneEnd(string cutsceneId)
    {
        if (System.Array.Exists(introCutsceneIds, id => id == cutsceneId))
        {
            
            currentCutsceneIndex++;
            
            if (currentCutsceneIndex < introCutsceneIds.Length)
            {
                // 다음 컷신 재생
                PlayCurrentCutscene();
            }
            else
            {
                OnAllCutscenesCompleted();
            }
        }
    }
    
    private void OnCutsceneSkip(string cutsceneId)
    {
        if (System.Array.Exists(introCutsceneIds, id => id == cutsceneId))
        {
            
            // ESC 스킵은 현재 컷신만 스킵하고 다음 컷신 재생
            currentCutsceneIndex++;
            
            if (currentCutsceneIndex < introCutsceneIds.Length)
            {
                PlayCurrentCutscene();
            }
            else
            {
                OnAllCutscenesCompleted();
            }
        }
    }
    
    private void OnAllCutscenesCompleted()
    {
        if (isTransitioning)
            return;
        
        isTransitioning = true;
        
        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.OnCutsceneEnd -= OnCutsceneEnd;
            CutsceneManager.Instance.OnCutsceneSkip -= OnCutsceneSkip;
        }
        
        
        TransitionToNextScene();
    }
    
    private void TransitionToNextScene()
    {
        if (GameManager.Instance != null)
        {
            var flowType = GameManager.Instance.currentFlow;
            
            if (flowType == GameManager.FlowType.FirstTime)
            {
                
                GameManager.Instance.LoadTutorialScene();
            }
            else if (flowType == GameManager.FlowType.ReplayIntro)
            {
                
                GameManager.Instance.LoadLobbyScene();
            }
        }
        else
        {
            Debug.LogError("[IntroScene] GameManager가 없습니다!");
        }
    }
    
    void OnDestroy()
    {
        if (CutsceneManager.Instance != null)
        {
            CutsceneManager.Instance.OnCutsceneEnd -= OnCutsceneEnd;
            CutsceneManager.Instance.OnCutsceneSkip -= OnCutsceneSkip;
        }
        
        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
        }
    }
}
