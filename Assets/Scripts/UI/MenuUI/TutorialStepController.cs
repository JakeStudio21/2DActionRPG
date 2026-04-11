using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 튜토리얼 단계별 진행 컨트롤러 (5단계)
/// Move → Attack → Dash → Skill1 → Skill2 → Completed
/// </summary>
public class TutorialStepController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private TMP_Text progressText; // 진행률 표시 (옵션)
    
    [Header("플레이어")]
    [SerializeField] private PlayerController playerController;
    
    [Header("튜토리얼 타겟 몬스터")]
    [SerializeField] private GameObject tutorialEnemy;  // 튜토리얼용 몬스터 (Inspector에서 할당)
    
    [Header("이동 단계 설정")]
    [SerializeField] private float moveRequiredDistance = 3f; // 필요 이동 거리 (N미터)
    
    [Header("스포트라이트 (자동 탐색)")]
    [SerializeField] private TutorialSpotlight tutorialSpotlight;
    
    [Header("설정")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private float minTimePerStep = 0.5f; // 각 단계 최소 시간
    
    [Header("⏱️ 타이밍 설정 (테스트용)")]
    [Tooltip("스킬2 성공 후 완료 메시지 표시까지 대기 시간 (초)")]
    [SerializeField] private float skill2SuccessConfirmDelay = 1.0f;
    
    [Tooltip("완료 메시지 표시 후 컷신 재생까지 대기 시간 (초)")]
    [SerializeField] private float completionMessageReadDelay = 2.0f;
    
    public event Action OnTutorialCompleted;
    public event Action<TutorialStep> OnStepChanged; // 단계 변경 이벤트 (스포트라이트용)
    
    public enum TutorialStep
    {
        Move,       // 조이스틱 이동
        Attack,     // 기본 공격
        Dash,       // 대시
        Skill1,     // 스킬1
        Skill2,     // 스킬2
        Completed   // 완료
    }
    
    private TutorialStep currentStep = TutorialStep.Move;
    private float tutorialStartTime = 0f;
    private Vector3 lastPosition;
    
    // 이동 관련
    private float totalMovedDistance = 0f;
    private bool moveStepCompleted = false;
    
    // 공격 관련
    private bool attackStepCompleted = false;
    private bool hasHitMonsterInAttackStep = false;
    
    // 대시 관련
    private bool dashStepCompleted = false;
    private bool hasDashed = false;
    
    // 스킬1 관련
    private bool skill1StepCompleted = false;
    private bool hasUsedSkill1 = false;
    private bool skill1HitMonster = false;
    
    // 스킬2 관련
    private bool skill2StepCompleted = false;
    private bool hasUsedSkill2 = false;
    private bool skill2HitMonster = false;
    
    // 단계별 텍스트 정의
    private readonly Dictionary<TutorialStep, string> stepInstructions = new Dictionary<TutorialStep, string>()
    {
        { TutorialStep.Move, "조이스틱을 건드리면 캐릭터가 움직인다\n(조이스틱을 움직여서 이동해보세요)" },
        { TutorialStep.Attack, "좋아! 다음은 공격버튼을 눌러서 몬스터를 공격해보게" },
        { TutorialStep.Dash, "좋아! 잘했어. 이제 회피 기술을 배워보자구.\n대시버튼을 눌러서 회피기술을 습득해보게" },
        { TutorialStep.Skill1, "좋아! 아주 잘하는군 이제 스킬1을 눌러서\n광역스킬로 몬스터를 공격해보게\n스킬은 강력하지만 쿨타임이 있다는걸 명심하게" },
        { TutorialStep.Skill2, "좋아! 아주 잘하는군 이제 마지막 스킬2를 눌러서\n광역스킬로 몬스터를 공격해보게\n스킬은 강력하지만 쿨타임이 있다는걸 명심하게" },
        { TutorialStep.Completed, "좋아! 모든 훈련과정을 마쳤네.\n이제 모험을 떠날 수 있겠네. 행운이 비네" }
    };
    
    void Start()
    {
        // 플레이어 자동 탐색
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
            
            if (playerController != null && enableDebugLogs)
                Debug.Log("[TutorialStep] PlayerController 자동 탐색 성공");
        }
        
        // TutorialSpotlight 자동 탐색
        if (tutorialSpotlight == null)
        {
            tutorialSpotlight = FindObjectOfType<TutorialSpotlight>();
            
            if (tutorialSpotlight != null && enableDebugLogs)
                Debug.Log("[TutorialStep] ✅ TutorialSpotlight 자동 탐색 성공");
            else if (enableDebugLogs)
                Debug.LogWarning("[TutorialStep] ⚠️ TutorialSpotlight를 찾을 수 없습니다!");
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
            Debug.Log("[TutorialStep] 🎯 튜토리얼 시작 - 5단계 시스템");
        
        // ⭐ 플레이어가 스폰된 후 호출되므로 여기서 재탐색!
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
            
            if (playerController != null && enableDebugLogs)
                Debug.Log("[TutorialStep] ✅ PlayerController 재탐색 성공 (스폰 후)");
            else if (enableDebugLogs)
                Debug.LogError("[TutorialStep] ❌ PlayerController를 찾을 수 없습니다!");
        }
        
        currentStep = TutorialStep.Move;
        tutorialStartTime = Time.time;
        
        // 이동 거리 초기화
        if (playerController != null)
        {
            lastPosition = playerController.transform.position;
            
            if (enableDebugLogs)
                Debug.Log($"[TutorialStep] 이동 시작 위치 초기화: {lastPosition}");
        }
        
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);
        }
        
        // 이벤트 구독
        SubscribeToGameEvents();
        
        UpdateInstructionText();
        UpdateProgressText();
        
        // 스포트라이트 활성화 (첫 단계)
        if (tutorialSpotlight != null)
        {
            tutorialSpotlight.ShowSpotlight(currentStep);
            
            if (enableDebugLogs)
                Debug.Log($"[TutorialStep] 💡 스포트라이트 활성화: {currentStep}");
        }
        
        // 이벤트 발생
        OnStepChanged?.Invoke(currentStep);
    }
    
    void Update()
    {
        if (currentStep == TutorialStep.Completed)
            return;
        
        // 최소 시간 경과 체크
        if (Time.time - tutorialStartTime < minTimePerStep)
            return;
        
        // 디버그: Update 실행 확인 (5초마다)
        if (enableDebugLogs && Time.frameCount % 300 == 0)
        {
            Debug.Log($"[TutorialStep] ⏰ Update 실행 중... 현재 단계: {currentStep}, 경과 시간: {(Time.time - tutorialStartTime):F1}초");
        }
        
        CheckStepProgress();
    }
    
    /// <summary>
    /// 게임 이벤트 구독
    /// </summary>
    private void SubscribeToGameEvents()
    {
        // PlayerController의 대시 이벤트 구독
        if (playerController != null)
        {
            playerController.OnDashPerformed += OnPlayerDashed;
        }
        
        // ⭐ PlayerAnimationController의 스킬 사용 이벤트 구독
        var playerAnimController = FindObjectOfType<PlayerAnimationController>();
        if (playerAnimController != null)
        {
            playerAnimController.OnSkill1Used += OnSkill1ButtonPressed;
            playerAnimController.OnSkill2Used += OnSkill2ButtonPressed;
            
            if (enableDebugLogs)
                Debug.Log("[TutorialStep] ✅ 스킬 사용 이벤트 구독 완료");
        }
        
        // ⭐ 튜토리얼 몬스터 피격 이벤트 구독
        if (tutorialEnemy == null)
        {
            // 자동 탐색: "Tutorial" 태그나 "BlueSlime" 이름으로 찾기
            tutorialEnemy = GameObject.FindGameObjectWithTag("Enemy");
            
            if (tutorialEnemy == null)
            {
                // 태그로 못 찾으면 이름으로 검색
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                foreach (var obj in allObjects)
                {
                    if (obj.name.Contains("BlueSlime") || obj.name.Contains("Slime"))
                    {
                        tutorialEnemy = obj;
                        break;
                    }
                }
            }
            
            if (tutorialEnemy != null && enableDebugLogs)
                Debug.Log($"[TutorialStep] ✅ 튜토리얼 몬스터 자동 탐색 성공: {tutorialEnemy.name}");
        }
        
        if (tutorialEnemy != null)
        {
            EnemyHealth enemyHealth = tutorialEnemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.OnTakeDamageEvent += OnEnemyHit;
                
                if (enableDebugLogs)
                    Debug.Log($"[TutorialStep] ✅ 몬스터 피격 이벤트 구독: {tutorialEnemy.name}");
            }
            else
            {
                Debug.LogWarning($"[TutorialStep] ⚠️ {tutorialEnemy.name}에 EnemyHealth 컴포넌트가 없습니다!");
            }
        }
        else
        {
            Debug.LogWarning("[TutorialStep] ⚠️ 튜토리얼 몬스터를 찾을 수 없습니다! Inspector에서 직접 할당해주세요.");
        }
        
        if (enableDebugLogs)
            Debug.Log("[TutorialStep] 게임 이벤트 구독 완료");
    }
    
    /// <summary>
    /// 게임 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromGameEvents()
    {
        if (playerController != null)
        {
            playerController.OnDashPerformed -= OnPlayerDashed;
        }
        
        // ⭐ PlayerAnimationController 이벤트 구독 해제
        var playerAnimController = FindObjectOfType<PlayerAnimationController>();
        if (playerAnimController != null)
        {
            playerAnimController.OnSkill1Used -= OnSkill1ButtonPressed;
            playerAnimController.OnSkill2Used -= OnSkill2ButtonPressed;
        }
        
        // ⭐ 몬스터 피격 이벤트 구독 해제
        if (tutorialEnemy != null)
        {
            EnemyHealth enemyHealth = tutorialEnemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.OnTakeDamageEvent -= OnEnemyHit;
            }
        }
    }
    
    /// <summary>
    /// 단계별 진행 체크
    /// </summary>
    private void CheckStepProgress()
    {
        switch (currentStep)
        {
            case TutorialStep.Move:
                CheckMoveProgress();
                break;
                
            case TutorialStep.Attack:
                CheckAttackProgress();
                break;
                
            case TutorialStep.Dash:
                CheckDashProgress();
                break;
                
            case TutorialStep.Skill1:
                CheckSkill1Progress();
                break;
                
            case TutorialStep.Skill2:
                CheckSkill2Progress();
                break;
        }
    }
    
    // ==================== Step 1: 이동 ====================
    
    private void CheckMoveProgress()
    {
        if (moveStepCompleted)
            return;
        
        if (playerController == null)
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0) // 2초마다
                Debug.LogWarning("[TutorialStep] ⚠️ PlayerController가 없어서 이동 체크 불가!");
            return;
        }
        
        // 이동 거리 누적 계산
        Vector3 currentPosition = playerController.transform.position;
        float distanceMoved = Vector3.Distance(lastPosition, currentPosition);
        
        if (distanceMoved > 0.01f) // 미세한 움직임 필터링
        {
            totalMovedDistance += distanceMoved;
            lastPosition = currentPosition;
            
            // 디버그 로그 (1초마다 한 번씩만)
            if (enableDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[TutorialStep] 🚶 이동 거리: {totalMovedDistance:F2}m / {moveRequiredDistance}m");
            }
        }
        
        // 클리어 조건: N미터 이상 이동
        if (totalMovedDistance >= moveRequiredDistance)
        {
            moveStepCompleted = true;
            CompleteMoveStep();
        }
    }
    
    private void CompleteMoveStep()
    {
        if (enableDebugLogs)
            Debug.Log($"[TutorialStep] ✅ 이동 단계 완료! (총 {totalMovedDistance:F2}m 이동)");
        
        AdvanceToNextStep(TutorialStep.Attack);
    }
    
    // ==================== Step 2: 공격 ====================
    
    private void CheckAttackProgress()
    {
        if (attackStepCompleted)
            return;
        
        // ⭐ 몬스터 피격 체크 (OnEnemyHit 콜백에서 플래그 설정됨)
        if (hasHitMonsterInAttackStep)
        {
            CompleteAttackStep();
        }
    }
    
    private void CompleteAttackStep()
    {
        if (enableDebugLogs)
            Debug.Log("[TutorialStep] ✅ 공격 단계 완료! (몬스터 피격)");
        
        attackStepCompleted = true;
        AdvanceToNextStep(TutorialStep.Dash);
    }
    
    // ==================== Step 3: 대시 ====================
    
    private void CheckDashProgress()
    {
        if (dashStepCompleted)
            return;
        
        // 대시 이벤트로 체크됨 (OnPlayerDashed 콜백)
    }
    
    private void OnPlayerDashed()
    {
        if (currentStep == TutorialStep.Dash && !dashStepCompleted)
        {
            hasDashed = true;
            CompleteDashStep();
        }
    }
    
    /// <summary>
    /// 몬스터 피격 콜백
    /// </summary>
    private void OnEnemyHit()
    {
        if (enableDebugLogs)
            Debug.Log($"[TutorialStep] 🎯 몬스터 피격! 현재 단계: {currentStep}");
        
        // 현재 단계에 따라 피격 플래그 설정
        if (currentStep == TutorialStep.Attack && !attackStepCompleted)
        {
            hasHitMonsterInAttackStep = true;
            
            if (enableDebugLogs)
                Debug.Log("[TutorialStep] ✅ Attack 단계 몬스터 피격 확인!");
        }
        else if (currentStep == TutorialStep.Skill1 && hasUsedSkill1 && !skill1StepCompleted)
        {
            skill1HitMonster = true;
            
            if (enableDebugLogs)
                Debug.Log("[TutorialStep] ✅ Skill1 단계 몬스터 피격 확인!");
        }
        else if (currentStep == TutorialStep.Skill2 && hasUsedSkill2 && !skill2StepCompleted)
        {
            skill2HitMonster = true;
            
            if (enableDebugLogs)
                Debug.Log("[TutorialStep] ✅ Skill2 단계 몬스터 피격 확인!");
        }
    }
    
    private void CompleteDashStep()
    {
        if (enableDebugLogs)
            Debug.Log("[TutorialStep] ✅ 대시 단계 완료!");
        
        dashStepCompleted = true;
        AdvanceToNextStep(TutorialStep.Skill1);
    }
    
    // ==================== Step 4: 스킬1 ====================
    
    private void CheckSkill1Progress()
    {
        if (skill1StepCompleted)
            return;
        
        // 스킬1 입력 체크 (S키 또는 UI 버튼)
        bool skill1KeyDown = false;
#if UNITY_EDITOR || UNITY_STANDALONE
        skill1KeyDown = Input.GetKeyDown(KeyCode.S);
#endif
        if (!hasUsedSkill1 && (skill1KeyDown || CheckSkill1ButtonPressed()))
        {
            hasUsedSkill1 = true;
            
            if (enableDebugLogs)
                Debug.Log("[TutorialStep] 스킬1 사용 감지! 몬스터 피격 대기 중...");
            
            // 스킬 발동 후 몬스터 피격 체크 (타임아웃)
            StartCoroutine(CheckSkill1HitAfterDelay());
        }
        
        // ⭐ 몬스터 피격 체크 (OnEnemyHit 콜백에서 플래그 설정됨)
        if (skill1HitMonster)
        {
            CompleteSkill1Step();
        }
    }
    
    /// <summary>
    /// 스킬1 버튼 클릭 콜백 (PlayerAnimationController 이벤트)
    /// </summary>
    private void OnSkill1ButtonPressed()
    {
        if (currentStep == TutorialStep.Skill1 && !hasUsedSkill1)
        {
            hasUsedSkill1 = true;
            
            if (enableDebugLogs)
                Debug.Log("[TutorialStep] 🔥 스킬1 UI 버튼 클릭 감지! 몬스터 피격 대기 중...");
            
            // 스킬 발동 후 몬스터 피격 체크 (타임아웃)
            StartCoroutine(CheckSkill1HitAfterDelay());
        }
    }
    
    private bool CheckSkill1ButtonPressed()
    {
        // 더 이상 사용하지 않음 (이벤트 방식으로 대체)
        return false;
    }
    
    private System.Collections.IEnumerator CheckSkill1HitAfterDelay()
    {
        // ⭐ 스킬 발동 후 3초 대기 (스킬이 몬스터에게 닿을 시간)
        yield return new WaitForSeconds(3.0f);
        
        // 3초 안에 피격되지 않으면 경고
        if (!skill1HitMonster && !skill1StepCompleted)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[TutorialStep] ⚠️ 스킬1이 몬스터를 맞추지 못했습니다. 다시 시도하세요!");
            
            // 다시 시도할 수 있도록 플래그 리셋
            hasUsedSkill1 = false;
        }
    }
    
    private void CompleteSkill1Step()
    {
        if (enableDebugLogs)
            Debug.Log("[TutorialStep] ✅ 스킬1 단계 완료! (스킬 발동 + 몬스터 피격)");
        
        skill1StepCompleted = true;
        AdvanceToNextStep(TutorialStep.Skill2);
    }
    
    // ==================== Step 5: 스킬2 ====================
    
    private void CheckSkill2Progress()
    {
        if (skill2StepCompleted)
            return;
        
        // 스킬2 입력 체크 (D키 또는 UI 버튼)
        bool skill2KeyDown = false;
#if UNITY_EDITOR || UNITY_STANDALONE
        skill2KeyDown = Input.GetKeyDown(KeyCode.D);
#endif
        if (!hasUsedSkill2 && (skill2KeyDown || CheckSkill2ButtonPressed()))
        {
            hasUsedSkill2 = true;
            
            if (enableDebugLogs)
                Debug.Log("[TutorialStep] 스킬2 사용 감지! 몬스터 피격 대기 중...");
            
            // 스킬 발동 후 몬스터 피격 체크 (타임아웃)
            StartCoroutine(CheckSkill2HitAfterDelay());
        }
        
        // ⭐ 몬스터 피격 체크 (OnEnemyHit 콜백에서 플래그 설정됨)
        if (skill2HitMonster)
        {
            CompleteSkill2Step();
        }
    }
    
    /// <summary>
    /// 스킬2 버튼 클릭 콜백 (PlayerAnimationController 이벤트)
    /// </summary>
    private void OnSkill2ButtonPressed()
    {
        if (currentStep == TutorialStep.Skill2 && !hasUsedSkill2)
        {
            hasUsedSkill2 = true;
            
            if (enableDebugLogs)
                Debug.Log("[TutorialStep] 🔥 스킬2 UI 버튼 클릭 감지! 몬스터 피격 대기 중...");
            
            // 스킬 발동 후 몬스터 피격 체크 (타임아웃)
            StartCoroutine(CheckSkill2HitAfterDelay());
        }
    }
    
    private bool CheckSkill2ButtonPressed()
    {
        // 더 이상 사용하지 않음 (이벤트 방식으로 대체)
        return false;
    }
    
    private System.Collections.IEnumerator CheckSkill2HitAfterDelay()
    {
        // ⭐ 스킬 발동 후 3초 대기 (스킬이 몬스터에게 닿을 시간)
        yield return new WaitForSeconds(3.0f);
        
        // 3초 안에 피격되지 않으면 경고
        if (!skill2HitMonster && !skill2StepCompleted)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[TutorialStep] ⚠️ 스킬2가 몬스터를 맞추지 못했습니다. 다시 시도하세요!");
            
            // 다시 시도할 수 있도록 플래그 리셋
            hasUsedSkill2 = false;
        }
    }
    
    private void CompleteSkill2Step()
    {
        if (enableDebugLogs)
            Debug.Log("[TutorialStep] ✅ 스킬2 단계 완료! (스킬 발동 + 몬스터 피격)");
        
        skill2StepCompleted = true;
        
        // ⭐ 스킬2 성공 후 1초 대기한 후 완료 메시지 표시
        StartCoroutine(DelayedCompleteStep());
    }
    
    /// <summary>
    /// 스킬2 완료 후 대기 후 완료 메시지 표시
    /// </summary>
    private System.Collections.IEnumerator DelayedCompleteStep()
    {
        if (enableDebugLogs)
            Debug.Log($"[TutorialStep] ⏰ 스킬2 성공 확인 중... ({skill2SuccessConfirmDelay}초 대기)");
        
        yield return new WaitForSeconds(skill2SuccessConfirmDelay);
        
        // 완료 단계로 전환 (완료 메시지 표시)
        AdvanceToNextStep(TutorialStep.Completed);
    }
    
    // ==================== 공통 ====================
    
    /// <summary>
    /// 다음 단계로 진행
    /// </summary>
    private void AdvanceToNextStep(TutorialStep nextStep)
    {
        currentStep = nextStep;
        tutorialStartTime = Time.time; // 단계 시작 시간 갱신
        
        UpdateInstructionText();
        UpdateProgressText();
        
        // 스포트라이트 업데이트
        if (nextStep == TutorialStep.Completed)
        {
            // 완료 단계: 스포트라이트 비활성화
            if (tutorialSpotlight != null)
            {
                tutorialSpotlight.HideSpotlight();
                
                if (enableDebugLogs)
                    Debug.Log("[TutorialStep] 💡 스포트라이트 비활성화 (완료)");
            }
        }
        else
        {
            // 다음 단계: 스포트라이트 이동
            if (tutorialSpotlight != null)
            {
                tutorialSpotlight.ShowSpotlight(nextStep);
                
                if (enableDebugLogs)
                    Debug.Log($"[TutorialStep] 💡 스포트라이트 이동: {nextStep}");
            }
        }
        
        // 이벤트 발생
        OnStepChanged?.Invoke(currentStep);
        
        if (enableDebugLogs)
            Debug.Log($"[TutorialStep] 🎯 단계 진행: {nextStep}");
        
        // 완료 단계 처리
        if (nextStep == TutorialStep.Completed)
        {
            StartCoroutine(CompleteTutorialAfterDelay());
        }
    }
    
    private System.Collections.IEnumerator CompleteTutorialAfterDelay()
    {
        // ⭐ 마지막 성공 메시지를 읽을 시간 추가
        if (enableDebugLogs)
            Debug.Log($"[TutorialStep] ⏰ 마지막 메시지 표시 중... ({completionMessageReadDelay}초 대기)");
        
        yield return new WaitForSeconds(completionMessageReadDelay);
        
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
        
        // 스포트라이트 비활성화
        if (tutorialSpotlight != null)
        {
            tutorialSpotlight.HideSpotlight();
        }
        
        // 이벤트 구독 해제
        UnsubscribeFromGameEvents();
        
        if (enableDebugLogs)
            Debug.Log("[TutorialStep] 🎉 튜토리얼 완전 완료! 컷신 재생 준비");
        
        OnTutorialCompleted?.Invoke();
    }
    
    /// <summary>
    /// 단계별 안내 텍스트 업데이트
    /// </summary>
    private void UpdateInstructionText()
    {
        if (instructionText == null)
            return;
        
        if (stepInstructions.TryGetValue(currentStep, out string text))
        {
            instructionText.text = text;
        }
        else
        {
            instructionText.text = "튜토리얼 진행 중...";
        }
    }
    
    /// <summary>
    /// 진행률 텍스트 업데이트
    /// </summary>
    private void UpdateProgressText()
    {
        if (progressText == null)
            return;
        
        int current = GetCurrentStepNumber();
        int total = 5; // Move, Attack, Dash, Skill1, Skill2
        
        if (currentStep == TutorialStep.Completed)
        {
            progressText.text = "완료!";
        }
        else
        {
            progressText.text = $"{current}/{total} 단계 진행 중";
        }
    }
    
    private int GetCurrentStepNumber()
    {
        switch (currentStep)
        {
            case TutorialStep.Move: return 1;
            case TutorialStep.Attack: return 2;
            case TutorialStep.Dash: return 3;
            case TutorialStep.Skill1: return 4;
            case TutorialStep.Skill2: return 5;
            case TutorialStep.Completed: return 5;
            default: return 0;
        }
    }
    
    /// <summary>
    /// 현재 단계 반환 (읽기 전용)
    /// </summary>
    public TutorialStep CurrentStep => currentStep;
    
    /// <summary>
    /// 외부에서 강제로 튜토리얼 완료 처리
    /// </summary>
    public void ForceComplete()
    {
        if (enableDebugLogs)
            Debug.Log("[TutorialStep] ⚠️ 튜토리얼 강제 완료");
        
        currentStep = TutorialStep.Completed;
        
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
        
        // 스포트라이트 비활성화
        if (tutorialSpotlight != null)
        {
            tutorialSpotlight.HideSpotlight();
        }
        
        UnsubscribeFromGameEvents();
        
        OnTutorialCompleted?.Invoke();
    }
    
    
    void OnDestroy()
    {
        UnsubscribeFromGameEvents();
    }
}
