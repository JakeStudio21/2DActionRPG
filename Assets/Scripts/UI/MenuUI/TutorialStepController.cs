using UnityEngine;
using System;
using TMPro;

/// <summary>
/// 튜토리얼 단계별 진행 컨트롤러
/// 이동 → 공격 → 완료 (간소화 버전)
/// </summary>
public class TutorialStepController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private GameObject instructionPanel;
    
    [Header("플레이어")]
    [SerializeField] private PlayerController playerController;
    
    [Header("설정")]
    [SerializeField] private bool enableDebugLogs = true;
    
    public event Action OnTutorialCompleted;
    
    private enum TutorialStep
    {
        Move,
        Attack,
        Completed
    }
    
    private TutorialStep currentStep = TutorialStep.Move;
    private bool hasMoved = false;
    private bool hasAttacked = false;
    private float moveThreshold = 0.3f;
    private float tutorialStartTime = 0f;
    private float minTimePerStep = 1.0f; // 각 단계 최소 시간
    
    void Start()
    {
        // 플레이어 자동 탐색
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
            
            if (playerController != null && enableDebugLogs)
                Debug.Log("[TutorialStep] PlayerController 자동 탐색 성공");
        }
        
        // UI 초기화
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
    }
    
    public void StartTutorial()
    {
        if (enableDebugLogs)
            Debug.Log("[TutorialStep] 튜토리얼 시작");
        
        currentStep = TutorialStep.Move;
        tutorialStartTime = Time.time;
        
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);
        }
        
        UpdateInstructionText();
    }
    
    void Update()
    {
        if (currentStep == TutorialStep.Completed)
            return;
        
        CheckStepProgress();
    }
    
    private void CheckStepProgress()
    {
        // 최소 시간 경과 체크
        if (Time.time - tutorialStartTime < minTimePerStep)
            return;
        
        switch (currentStep)
        {
            case TutorialStep.Move:
                CheckMoveProgress();
                break;
                
            case TutorialStep.Attack:
                CheckAttackProgress();
                break;
        }
    }
    
    private void CheckMoveProgress()
    {
        if (hasMoved)
            return;
        
        // 플레이어 컨트롤러가 있는 경우 (Movement 프로퍼티 사용)
        if (playerController != null)
        {
            Vector2 moveInput = playerController.Movement;
            if (moveInput.magnitude > moveThreshold)
            {
                hasMoved = true;
                CompleteMoveStep();
                return;
            }
        }
        
        // Fallback: 키보드 입력 직접 체크
        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
        {
            hasMoved = true;
            CompleteMoveStep();
        }
    }
    
    private void CheckAttackProgress()
    {
        if (hasAttacked)
            return;
        
        // 공격 입력 체크 (마우스 클릭, 스페이스바, 또는 조이스틱)
        if (Input.GetMouseButtonDown(0) || 
            Input.GetKeyDown(KeyCode.Space) || 
            Input.GetKeyDown(KeyCode.Z) ||
            Input.GetKeyDown(KeyCode.X))
        {
            hasAttacked = true;
            CompleteAttackStep();
        }
    }
    
    private void CompleteMoveStep()
    {
        if (enableDebugLogs)
            Debug.Log("[TutorialStep] 이동 단계 완료");
        
        currentStep = TutorialStep.Attack;
        tutorialStartTime = Time.time; // 단계 시작 시간 갱신
        UpdateInstructionText();
    }
    
    private void CompleteAttackStep()
    {
        if (enableDebugLogs)
            Debug.Log("[TutorialStep] 공격 단계 완료");
        
        currentStep = TutorialStep.Completed;
        UpdateInstructionText();
        
        // 완료 메시지 표시 후 잠시 대기
        StartCoroutine(CompleteTutorialAfterDelay());
    }
    
    private System.Collections.IEnumerator CompleteTutorialAfterDelay()
    {
        yield return new WaitForSeconds(2.0f);
        
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
        
        OnTutorialCompleted?.Invoke();
    }
    
    private void UpdateInstructionText()
    {
        if (instructionText == null)
            return;
        
        switch (currentStep)
        {
            case TutorialStep.Move:
                instructionText.text = "조이스틱 또는 방향키로 이동해보세요!";
                break;
                
            case TutorialStep.Attack:
                instructionText.text = "공격 버튼 또는 스페이스바를 눌러보세요!";
                break;
                
            case TutorialStep.Completed:
                instructionText.text = "튜토리얼 완료! 잘하셨습니다!";
                break;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[TutorialStep] 단계 변경: {currentStep}");
    }
    
    /// <summary>
    /// 외부에서 강제로 튜토리얼 완료 처리
    /// </summary>
    public void ForceComplete()
    {
        if (enableDebugLogs)
            Debug.Log("[TutorialStep] 튜토리얼 강제 완료");
        
        currentStep = TutorialStep.Completed;
        
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
        
        OnTutorialCompleted?.Invoke();
    }
}
