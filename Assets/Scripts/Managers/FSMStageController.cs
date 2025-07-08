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
            stageInfos.Add(new StageInfo { stage = StageState.Scene1, sceneName = "Scene1", displayName = "1단계", requiresBossDefeat = false, nextStage = StageState.Scene2 });
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
            if (currentStageInfo.requiresBossDefeat && !IsBossDefeated())
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
            if (enemy.isBoss && !enemy.isDead)
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
            if (!IsBossDefeated())
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