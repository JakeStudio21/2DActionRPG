using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 씬 상태를 FSM(Finite State Machine)으로 관리하는 컨트롤러
/// AreaEntrance, AreaExit, SceneManagement의 기능을 통합
/// </summary>
public class FSMStageController : Singleton<FSMStageController>
{
    [System.Serializable]
    public enum StageState
    {
        Lobby,
        Loading,
        Scene1,
        Scene2,
        Scene3,
        Boss,
        GameOver,
        Victory
    }

    [System.Serializable]
    public class StageInfo
    {
        public StageState stage;
        public string sceneName;
        public string displayName;
        public bool requiresBossDefeat;
        public StageState nextStage;
        public Vector3 playerSpawnPosition = Vector3.zero; // ⭐ 추가: 플레이어 스폰 위치
        public string transitionName = ""; // ⭐ 추가: 전환 식별자
    }

    [Header("스테이지 설정")]
    [SerializeField] private StageState currentStage = StageState.Lobby;
    [SerializeField] private List<StageInfo> stageInfos = new List<StageInfo>();

    [Header("전환 설정")]
    [SerializeField] private float sceneTransitionDelay = 1f;
    [SerializeField] private string loadingSceneName = "Loading";
    
    [Header("플레이어 관리")]
    [SerializeField] private bool autoSetPlayerPosition = true; // ⭐ 추가: 자동 플레이어 위치 설정

    // 이벤트
    public System.Action<StageState> OnStageChanged;
    public System.Action<StageState> OnStageTransitionStarted;
    public System.Action<StageState> OnStageTransitionCompleted;

    // 내부 상태
    private bool isTransitioning = false;
    private Dictionary<StageState, StageInfo> stageDict = new Dictionary<StageState, StageInfo>();
    private bool victoryTriggered = false; // ⭐ 추가: Victory 중복 방지
    private bool defeatTriggered = false;  // ⭐ 추가: Defeat 중복 방지
    private string lastSceneName = ""; // ⭐ 추가: 씬 변경 감지용

    protected override void Awake()
    {
        base.Awake();
        InitializeStageSystem();
    }

    private void Start()
    {
        SetupDefaultStages();
        UpdateCurrentStageFromScene();
    }

    /// <summary>
    /// ⭐ 수정: 승리 조건 자동 감지 + 씬 변경 감지
    /// </summary>
    private void Update()
    {
        // ⭐ 추가: 씬 변경 감지 (매우 가벼운 체크)
        CheckSceneChange();
        
        // 게임 플레이 중일 때만 승리 조건 체크
        if (!victoryTriggered && (currentStage == StageState.Scene1 || currentStage == StageState.Scene2 || currentStage == StageState.Scene3))
        {
            CheckVictoryCondition();
        }
    }

    /// <summary>
    /// ⭐ 추가: 씬 변경 감지 및 스테이지 업데이트
    /// </summary>
    private void CheckSceneChange()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        // 씬이 변경된 경우에만 실행
        if (lastSceneName != currentSceneName)
        {
            Debug.Log($"[FSMStageController] 씬 변경 감지: {lastSceneName} → {currentSceneName}");
            lastSceneName = currentSceneName;
            
            // Victory/Defeat 플래그 리셋 (새 씬에서 다시 판정 가능)
            victoryTriggered = false;
            defeatTriggered = false;
            
            // 스테이지 상태 업데이트
            UpdateCurrentStageFromScene();
        }
    }

    /// <summary>
    /// ⭐ 추가: 승리 조건 확인 및 처리
    /// </summary>
    private void CheckVictoryCondition()
    {
        var currentStageInfo = GetCurrentStageInfo();
        if (currentStageInfo != null && currentStageInfo.requiresBossDefeat)
        {
            if (AreAllBossesDefeated())
            {
                TriggerVictory();
            }
        }
        else
        {
            // 보스 격파가 필요없는 스테이지에서도 보스가 있고 죽었다면 승리
            if (AreAllBossesDefeated() && HasAnyBoss())
            {
                TriggerVictory();
            }
        }
    }

    /// <summary>
    /// ⭐ 단순화: StageProgressManager 사용
    /// </summary>
    private bool AreAllBossesDefeated()
    {
        return StageProgressManager.Instance.AreAllStageeBossesDefeated();
    }

    /// <summary>
    /// ⭐ 단순화: StageProgressManager 사용  
    /// </summary>
    private bool HasAnyBoss()
    {
        return !StageProgressManager.Instance.AreAllStageeBossesDefeated();
    }

    /// <summary>
    /// ⭐ 추가: Victory 상태 전환 및 팝업 표시
    /// </summary>
    public void TriggerVictory()
    {
        if (victoryTriggered) return; // 중복 방지

        victoryTriggered = true;
        currentStage = StageState.Victory;
        OnStageChanged?.Invoke(currentStage);
        
        Debug.Log("[FSMStageController] Victory! 미션 완료");
        
        // Victory 팝업 표시
        StartCoroutine(ShowVictoryPopupRoutine());
    }

    /// <summary>
    /// ⭐ 추가: Victory 팝업 표시 코루틴
    /// </summary>
    private IEnumerator ShowVictoryPopupRoutine()
    {
        yield return new WaitForSeconds(0.5f); // 보스 죽음 연출 대기
        
        var resultPopup = FindObjectOfType<ResultPopupController>();
        if (resultPopup != null)
        {
            resultPopup.Show(true); // Victory
        }
        else
        {
            Debug.LogError("[FSMStageController] ResultPopupController를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// ⭐ 추가: Defeat 상태 전환 및 팝업 표시
    /// </summary>
    public void TriggerDefeat()
    {
        if (defeatTriggered) return; // 중복 방지

        defeatTriggered = true;
        currentStage = StageState.GameOver;
        OnStageChanged?.Invoke(currentStage);
        
        Debug.Log("[FSMStageController] Defeat! 게임 오버");
        
        // Defeat 팝업 표시는 PlayerHealth에서 기존대로 처리
    }

    /// <summary>
    /// ⭐ 추가: 씬 내 포털 이동
    /// </summary>
    public void TriggerPortalMovement(Vector3 targetPosition, string transitionName = "")
    {
        if (isTransitioning) 
        {
            Debug.LogWarning("[FSMStageController] 이미 이동 중입니다.");
            return;
        }

        StartCoroutine(PortalMovementCoroutine(targetPosition, transitionName));
    }

    /// <summary>
    /// ⭐ 추가: 포털 이동 코루틴
    /// </summary>
    private IEnumerator PortalMovementCoroutine(Vector3 targetPosition, string transitionName)
    {
        isTransitioning = true;
        
        // 페이드 효과
        if (UIFadeManager.Instance != null)
        {
            UIFadeManager.Instance.FadeToBlack();
            yield return new WaitForSeconds(0.5f);
        }

        // 플레이어 이동
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.transform.position = targetPosition;
            Debug.Log($"[FSMStageController] 포털 이동: {targetPosition}");
        }

        // 전환 정보 설정
        if (!string.IsNullOrEmpty(transitionName))
        {
            SetTransitionInfo(transitionName);
        }

        // 카메라 추적 재설정
        if (CameraController.Instance != null)
        {
            CameraController.Instance.SetPlayerCameraFollow();
        }

        // 페이드 해제
        if (UIFadeManager.Instance != null)
        {
            yield return new WaitForSeconds(0.2f);
            UIFadeManager.Instance.FadeToClear();
        }

        isTransitioning = false;
    }

    /// <summary>
    /// ⭐ 수정: 보스 격파 확인 후 포털 이동 가능 여부 체크
    /// </summary>
    public bool TryPortalMovementWithBossCheck(Vector3 targetPosition, string transitionName = "", bool requiresBossDefeat = false)
    {
        if (requiresBossDefeat && !AreAllBossesDefeated())
        {
            Debug.LogWarning("[FSMStageController] 보스를 먼저 처치해야 합니다!");
            return false;
        }

        TriggerPortalMovement(targetPosition, transitionName);
        return true;
    }

    /// <summary>
    /// 스테이지 시스템 초기화
    /// </summary>
    private void InitializeStageSystem()
    {
        // 스테이지 딕셔너리 구성
        stageDict.Clear();
        foreach (var stageInfo in stageInfos)
        {
            if (!stageDict.ContainsKey(stageInfo.stage))
            {
                stageDict.Add(stageInfo.stage, stageInfo);
            }
        }
    }

    /// <summary>
    /// 기본 스테이지 설정
    /// </summary>
    private void SetupDefaultStages()
    {
        if (stageInfos.Count == 0)
        {
            stageInfos.Add(new StageInfo { stage = StageState.Lobby, sceneName = "Lobby", displayName = "로비", requiresBossDefeat = false, nextStage = StageState.Loading });
            stageInfos.Add(new StageInfo { stage = StageState.Loading, sceneName = "Loading", displayName = "로딩", requiresBossDefeat = false, nextStage = StageState.Scene1 });
            stageInfos.Add(new StageInfo { stage = StageState.Scene1, sceneName = "Scene1", displayName = "1단계", requiresBossDefeat = true, nextStage = StageState.Scene2 });
            stageInfos.Add(new StageInfo { stage = StageState.Scene2, sceneName = "Scene2", displayName = "2단계", requiresBossDefeat = false, nextStage = StageState.Scene3 });
            stageInfos.Add(new StageInfo { stage = StageState.Scene3, sceneName = "Scene3", displayName = "3단계", requiresBossDefeat = true, nextStage = StageState.Victory });
        }
        InitializeStageSystem();
    }

    /// <summary>
    /// 현재 씬에서 스테이지 상태 업데이트
    /// </summary>
    private void UpdateCurrentStageFromScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        foreach (var stageInfo in stageInfos)
        {
            if (stageInfo.sceneName == currentSceneName)
            {
                currentStage = stageInfo.stage;
                OnStageChanged?.Invoke(currentStage);
                Debug.Log($"[FSMStageController] 현재 스테이지: {currentStage} ({stageInfo.displayName})");
                break;
            }
        }
    }

    /// <summary>
    /// 다음 스테이지로 전환
    /// </summary>
    public void TransitionToNextStage()
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[FSMStageController] 이미 전환 중입니다.");
            return;
        }

        if (stageDict.ContainsKey(currentStage))
        {
            var currentStageInfo = stageDict[currentStage];
            
            // 보스 격파 조건 확인
            if (currentStageInfo.requiresBossDefeat && !AreAllBossesDefeated())
            {
                Debug.LogWarning("[FSMStageController] 보스를 먼저 처치해야 합니다!");
                return;
            }

            TransitionToStage(currentStageInfo.nextStage);
        }
    }

    /// <summary>
    /// 특정 스테이지로 전환
    /// </summary>
    public void TransitionToStage(StageState targetStage)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[FSMStageController] 이미 전환 중입니다.");
            return;
        }

        if (!stageDict.ContainsKey(targetStage))
        {
            Debug.LogError($"[FSMStageController] 알 수 없는 스테이지: {targetStage}");
            return;
        }

        StartCoroutine(TransitionToStageCoroutine(targetStage));
    }

    /// <summary>
    /// 스테이지 전환 코루틴
    /// </summary>
    private IEnumerator TransitionToStageCoroutine(StageState targetStage)
    {
        isTransitioning = true;
        OnStageTransitionStarted?.Invoke(targetStage);

        var targetStageInfo = stageDict[targetStage];
        Debug.Log($"[FSMStageController] {currentStage} → {targetStage} 전환 시작");

        // 로딩 씬을 거쳐야 하는 경우
        if (targetStage != StageState.Loading && currentStage != StageState.Loading)
        {
            // 로딩 씬으로 먼저 이동
            currentStage = StageState.Loading;
            OnStageChanged?.Invoke(currentStage);
            SceneManager.LoadScene(loadingSceneName);
            
            yield return new WaitForSeconds(sceneTransitionDelay);
        }

        // 최종 목표 씬 로드
        currentStage = targetStage;
        OnStageChanged?.Invoke(currentStage);
        SceneManager.LoadScene(targetStageInfo.sceneName);

        yield return new WaitForSeconds(0.1f);

        // 씬 로드 후 추가 처리
        yield return StartCoroutine(PostSceneLoadSetup());

        isTransitioning = false;
        OnStageTransitionCompleted?.Invoke(targetStage);
        Debug.Log($"[FSMStageController] {targetStage} 전환 완료");
    }

    /// <summary>
    /// 씬 로드 후 설정 (AreaEntrance 기능 통합)
    /// </summary>
    private IEnumerator PostSceneLoadSetup()
    {
        // 플레이어 스폰 대기
        yield return new WaitForSeconds(0.5f);

        // ⭐ 추가: 플레이어 위치 설정 (AreaEntrance 기능 통합)
        if (autoSetPlayerPosition)
        {
            SetPlayerPosition();
        }

        // GameManager 조이스틱 재연결
        if (GameManager.Instance != null)
        {
            // GameManager의 RefreshAllJoystickReferences 메서드 호출
            GameManager.Instance.SendMessage("RefreshAllJoystickReferences", SendMessageOptions.DontRequireReceiver);
        }

        // 카메라 설정
        if (CameraController.Instance != null)
        {
            CameraController.Instance.SetPlayerCameraFollow();
        }
    }

    /// <summary>
    /// 플레이어 위치 설정 (AreaEntrance 대체)
    /// </summary>
    private void SetPlayerPosition()
    {
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("[FSMStageController] PlayerController를 찾을 수 없습니다!");
            return;
        }

        var currentStageInfo = GetCurrentStageInfo();
        if (currentStageInfo != null && currentStageInfo.playerSpawnPosition != Vector3.zero)
        {
            // 설정된 스폰 위치로 이동
            playerController.transform.position = currentStageInfo.playerSpawnPosition;
            Debug.Log($"[FSMStageController] 플레이어 위치 설정: {currentStageInfo.playerSpawnPosition}");
        }
        else
        {
            // AreaEntrance가 있는지 확인
            AreaEntrance[] entrances = FindObjectsOfType<AreaEntrance>();
            if (entrances.Length > 0)
            {
                // 기존 AreaEntrance 시스템 사용 (하위 호환성)
                Debug.Log("[FSMStageController] 기존 AreaEntrance 시스템 사용");
            }
        }
    }

    /// <summary>
    /// 보스 격파 여부 확인
    /// </summary>
    private bool IsBossDefeated()
    {
        // 씬에 있는 모든 EnemyHealth 중 isBoss == true인 적 찾기
        EnemyHealth[] allEnemies = FindObjectsOfType<EnemyHealth>();
        foreach (var enemy in allEnemies)
        {
            if (enemy.IsBoss() && !enemy.isDead)
            {
                return false; // 살아있는 보스가 있음
            }
        }
        return true; // 모든 보스가 죽었거나 보스가 없음
    }

    /// <summary>
    /// 로비로 돌아가기
    /// </summary>
    public void ReturnToLobby()
    {
        TransitionToStage(StageState.Lobby);
    }

    /// <summary>
    /// 게임 재시작 (현재 스테이지 다시 로드)
    /// </summary>
    public void RestartCurrentStage()
    {
        if (stageDict.ContainsKey(currentStage))
        {
            var currentStageInfo = stageDict[currentStage];
            SceneManager.LoadScene(currentStageInfo.sceneName);
        }
    }

    /// <summary>
    /// 현재 스테이지 정보 가져오기
    /// </summary>
    public StageInfo GetCurrentStageInfo()
    {
        return stageDict.ContainsKey(currentStage) ? stageDict[currentStage] : null;
    }

    /// <summary>
    /// 스테이지 상태 가져오기
    /// </summary>
    public StageState GetCurrentStage()
    {
        return currentStage;
    }

    /// <summary>
    /// 전환 중인지 확인
    /// </summary>
    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    /// <summary>
    /// 트리거 기반 씬 전환 (AreaExit 대체)
    /// </summary>
    public void TriggerSceneTransition(string targetSceneName = "")
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[FSMStageController] 이미 전환 중입니다.");
            return;
        }

        // 특정 씬 이름이 주어진 경우 해당 씬으로 이동
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            foreach (var stageInfo in stageInfos)
            {
                if (stageInfo.sceneName == targetSceneName)
                {
                    TransitionToStage(stageInfo.stage);
                    return;
                }
            }
            Debug.LogError($"[FSMStageController] 알 수 없는 씬 이름: {targetSceneName}");
            return;
        }

        // 기본적으로는 다음 스테이지로 전환
        TransitionToNextStage();
    }

    /// <summary>
    /// 보스 격파 확인 후 씬 전환 (AreaExit 로직 통합)
    /// </summary>
    public bool TryTransitionWithBossCheck(string targetSceneName = "")
    {
        var currentStageInfo = GetCurrentStageInfo();
        if (currentStageInfo != null && currentStageInfo.requiresBossDefeat)
        {
            if (!AreAllBossesDefeated())
            {
                Debug.LogWarning("[FSMStageController] 보스를 먼저 처치해야 합니다!");
                return false;
            }
        }

        TriggerSceneTransition(targetSceneName);
        return true;
    }

    /// <summary>
    /// SceneManagement.cs 대체: 전환 정보 설정
    /// </summary>
    public void SetTransitionInfo(string transitionName)
    {
        var currentStageInfo = GetCurrentStageInfo();
        if (currentStageInfo != null)
        {
            currentStageInfo.transitionName = transitionName;
            Debug.Log($"[FSMStageController] 전환 정보 설정: {transitionName}");
        }
    }

    /// <summary>
    /// SceneManagement.cs 대체: 전환 정보 가져오기
    /// </summary>
    public string GetTransitionInfo()
    {
        var currentStageInfo = GetCurrentStageInfo();
        return currentStageInfo?.transitionName ?? "";
    }
} 