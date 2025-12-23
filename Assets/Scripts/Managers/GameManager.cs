using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;



/// <summary>
/// 스테이지 선택 데이터 구조체
/// </summary>
[System.Serializable]
public class StageSelectionData
{
    public int selectedStageNumber = 0; // 0 = 선택안함, 1-3 = 스테이지 번호
    public string selectedSceneName = "";
    
    public StageSelectionData()
    {
        selectedStageNumber = 0;
        selectedSceneName = "";
    }
    
    public StageSelectionData(int stageNumber, string sceneName)
    {
        selectedStageNumber = stageNumber;
        selectedSceneName = sceneName;
    }
    
    public bool IsValid()
    {
        return selectedStageNumber > 0 && !string.IsNullOrEmpty(selectedSceneName);
    }
}

/// <summary>
/// 게임 전체 흐름을 관리하는 통합 매니저
/// 씬 전환, 게임 상태, 정지/재시작 등을 제어
/// </summary>
public class GameManager : Singleton<GameManager>
{
    [Header("게임 상태")]
    public GameState currentGameState = GameState.None;
    public bool isGamePaused = false;
    
    [Header("씬 관리")]
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string loadingSceneName = "Ingame_Loading";  // "Loading"에서 "Ingame_Loading"으로 변경
    [SerializeField] private string introSceneName = "Intro";              // 🆕 인트로 씬
    [SerializeField] private string tutorialSceneName = "Tutorial";        // 🆕 튜토리얼 씬
    
    [Header("게임 데이터")]
    public SelectedPlayerData selectedPlayerData;
    
    // 🆕 최초 실행 플로우 관리
    public enum FlowType
    {
        FirstTime,      // 최초 실행 플로우 (Login → Intro → Tutorial → Lobby)
        ReturnFromGame, // 재실행 플로우 (Login → Lobby)
        ReplayIntro,    // 인트로 다시보기 (Lobby → Intro → Lobby)
        ReplayTutorial  // 튜토리얼 다시보기 (Lobby → Tutorial → Lobby)
    }
    
    public FlowType currentFlow = FlowType.FirstTime;
    
    // 🆕 최초 실행 판정 키 (PlayerPrefs 기반)
    private const string FIRST_LAUNCH_KEY = "HasLaunchedBefore";
    
    // 런타임 스테이지 선택 데이터 (DontDestroyOnLoad로 유지됨)
    [System.NonSerialized]
    private StageSelectionData runtimeStageData = new StageSelectionData();
    
    // 게임 상태 열거형
    public enum GameState
    {
        None,
        Lobby,
        Loading,
        InGame,
        Paused,
        GameOver
    }
    
    /// <summary>
    /// 자동으로 GameManager 인스턴스 생성 (없을 경우)
    /// </summary>
    public static void EnsureInstance()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }
    }
    
    protected override void Awake()
    {
        base.Awake(); // Singleton 로직 실행
        if (instance != this) return; // 중복 생성시 초기화 중단
        
        // 🆕 추가: PlayerDataManager 데이터 변경 리스너
        PlayerDataManager.OnSelectedPlayerDataChanged += OnPlayerDataChanged;
        PlayerDataManager.OnPlayerGoldChanged += OnPlayerGoldChanged;
        PlayerDataManager.OnPlayerLevelChanged += OnPlayerLevelChanged;
        
        Debug.Log($"[GameManager] Awake - selectedPlayerData: {selectedPlayerData}");
        if (selectedPlayerData != null)
        {
            Debug.Log($"[GameManager] Awake - selectedPlayerType: {selectedPlayerData.selectedPlayerType}, weaponName: {selectedPlayerData.weaponName}");
            Debug.Log($"[GameManager] Awake - selectedPlayerData instanceID: {selectedPlayerData.GetInstanceID()}");
        }

        InitializeGame();
    }
    
    private void Start()
    {
        // ⭐ 수정: 로비에서만 상태를 Lobby로 설정
        if (currentGameState == GameState.None)
        {
            // 씬 이름으로 현재 상태 판단
            string currentSceneName = SceneManager.GetActiveScene().name;
            if (currentSceneName == lobbySceneName)
            {
                currentGameState = GameState.Lobby;
            }
            // 다른 씬에서는 자동 이동하지 않음
        }
    }
    
    /// <summary>
    /// 게임 초기화
    /// </summary>
    private void InitializeGame()
    {
        currentGameState = GameState.None;
        isGamePaused = false;
        
        // ⭐ 수정: selectedPlayerData가 null인 경우 런타임에서 생성
        if (selectedPlayerData == null)
        {
            Debug.LogWarning("[GameManager] selectedPlayerData가 에디터에서 할당되지 않았습니다! 런타임에서 생성합니다.");
            selectedPlayerData = ScriptableObject.CreateInstance<SelectedPlayerData>();
            selectedPlayerData.Reset(); // 초기값으로 설정
        }
        

        
        // 런타임 스테이지 데이터 초기화
        if (runtimeStageData == null)
        {
            runtimeStageData = new StageSelectionData();
        }
    }
    

    
    /// <summary>
    /// 런타임 스테이지 데이터 설정 (StageSelectUIController에서 호출)
    /// </summary>
    public void SetSelectedStage(int stageNumber, string sceneName)
    {
        runtimeStageData.selectedStageNumber = stageNumber;
        runtimeStageData.selectedSceneName = sceneName;
        
        Debug.Log($"[GameManager] 선택된 스테이지 설정: Stage {stageNumber} ({sceneName})");
    }

    /// <summary>
    /// 런타임 스테이지 데이터 가져오기
    /// </summary>
    public StageSelectionData GetSelectedStageData()
    {
        return runtimeStageData;
    }

    /// <summary>
    /// [사용 중단] StageSelect 씬으로 이동 - 이제 Lobby에서 패널 전환으로 처리됨
    /// </summary>
    [System.Obsolete("StageSelect 씬이 제거되었습니다. Lobby에서 패널 전환으로 처리됩니다.")]
    public void LoadStageSelectScene()
    {
        Debug.LogWarning("[GameManager] LoadStageSelectScene은 더 이상 사용되지 않습니다. Lobby 패널 전환을 사용하세요.");
        // 호환성을 위해 Lobby로 이동
        LoadLobbyScene();
    }
    
    /// <summary>
    /// 로비 씬으로 이동
    /// </summary>
    public void LoadLobbyScene()
    {
        currentGameState = GameState.Lobby;
        SceneManager.LoadScene(lobbySceneName);
    }
    
    /// <summary>
    /// 인게임에서 로비로 돌아가기 (로비 메인 화면으로 설정)
    /// </summary>
    public void ReturnToLobby()
    {
        currentGameState = GameState.Lobby;
        
        // 로비 씬 로드 후 메인 패널 활성화
        SceneManager.sceneLoaded += OnLobbySceneLoaded;
        SceneManager.LoadScene(lobbySceneName);
    }
    
    /// <summary>
    /// 로비 씬 로드 완료 후 메인 패널 활성화
    /// </summary>
    private void OnLobbySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Lobby")
        {
            SceneManager.sceneLoaded -= OnLobbySceneLoaded;
            
            // LobbyUIController 찾아서 메인 패널 활성화
            LobbyUIController lobbyUI = FindObjectOfType<LobbyUIController>();
            if (lobbyUI != null)
            {
                lobbyUI.OnBackToLobby();
            }
        }
    }
    
    /// <summary>
    /// 로딩 씬을 거쳐 게임 씬으로 이동
    /// </summary>
    public void LoadGameScene(string sceneName)
    {
        StartCoroutine(LoadGameSceneCoroutine(sceneName));
    }

    private IEnumerator LoadGameSceneCoroutine(string sceneName)
    {
        // 🔧 수정: PlayerDataManager 기반으로 검증 강화
        var playerDataManager = PlayerDataManager.Instance;
        if (playerDataManager == null)
        {
            Debug.LogError("[GameManager] PlayerDataManager를 찾을 수 없습니다! 로비로 돌아갑니다.");
            LoadLobbyScene();
            yield break;
        }
        
        if (playerDataManager.selectedPlayerData == null || !playerDataManager.selectedPlayerData.IsPlayerSelected())
        {
            Debug.LogError("[GameManager] PlayerDataManager의 플레이어 데이터가 유효하지 않습니다! 로비로 돌아갑니다.");
            Debug.LogError($"[GameManager] 현재 데이터: {playerDataManager.selectedPlayerData}");
            LoadLobbyScene();
            yield break;
        }

        Debug.Log($"[GameManager] 씬 전환 시작 - {sceneName}");
        Debug.Log($"[GameManager] 전달될 데이터: {selectedPlayerData}");
        Debug.Log($"[GameManager] PlayerDataManager 검증: ✅");

        // 로딩 씬으로 이동
        currentGameState = GameState.Loading;
        SceneManager.LoadScene(loadingSceneName);

        yield return new WaitForSeconds(0.1f); // 씬 전환 대기

        // 실제 게임 씬 로드
        currentGameState = GameState.InGame;
        SceneManager.LoadScene(sceneName);

        // ⭐ 추가: 씬 로드 후 조이스틱 재연결 시도
        yield return new WaitForSeconds(0.5f);
        RefreshAllJoystickReferences();       
    }

    /// <summary>
    /// 모든 조이스틱 참조를 새로고침 (강화된 버전)
    /// </summary>
    private void RefreshAllJoystickReferences()
    {
        // ⭐ 여러 번 시도하는 코루틴으로 변경
        StartCoroutine(RefreshJoystickReferencesCoroutine());
    }

    private IEnumerator RefreshJoystickReferencesCoroutine()
    {
        float timeout = 3f;
        float elapsed = 0f;
        
        while (elapsed < timeout)
        {
            // PlayerController의 조이스틱 참조 새로고침
            var playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                playerController.RefreshJoystickReference();
            }
            
            // AttackJoystickInput의 조이스틱 참조 새로고침
            var attackJoystickInput = FindObjectOfType<AttackJoystickInput>();
            if (attackJoystickInput != null)
            {
                attackJoystickInput.RefreshJoystickReference();
            }
            
            // 조이스틱이 제대로 연결되었는지 확인
            var joystick = FindObjectOfType<FixedJoystick>();
            if (joystick != null && playerController != null)
            {
                Debug.Log("[GameManager] 조이스틱 재연결 성공!");
                break;
            }
            
            elapsed += 0.2f;
            yield return new WaitForSeconds(0.2f);
        }
        
        if (elapsed >= timeout)
        {
            Debug.LogError("[GameManager] 조이스틱 재연결 실패!");
        }
    }

    /// <summary>
    /// 게임 일시정지/재개
    /// </summary>
    public void TogglePause()
    {
        isGamePaused = !isGamePaused;
        Time.timeScale = isGamePaused ? 0f : 1f;
        currentGameState = isGamePaused ? GameState.Paused : GameState.InGame;
    }
    
    /// <summary>
    /// 게임 재시작
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        isGamePaused = false;
        currentGameState = GameState.InGame;
        
        // 현재 씬 재로드
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    /// <summary>
    /// 게임 종료
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    /// <summary>
    /// 게임 상태 변경
    /// </summary>
    public void ChangeGameState(GameState newState)
    {
        currentGameState = newState;
        Debug.Log($"[GameManager] 게임 상태 변경: {newState}");
    }

    #region 🆕 Step 5: PlayerDataManager 이벤트 리스너

    private void OnPlayerDataChanged(SelectedPlayerData newData)
    {
        Debug.Log($"🔔 [GameManager] PlayerData 변경 감지: {newData?.selectedPlayerType} Lv.{newData?.CurrentLevel} Gold:{newData?.CurrentGold}");
        
        // 🆕 추가 처리들:
        // - 다른 시스템들에 알림
        // - UI 갱신 요청 
        // - 캐시 무효화 등
    }

    private void OnPlayerGoldChanged(int newGold)
    {
        Debug.Log($"🪙 [GameManager] 골드 변경 감지: {newGold}");
    }

    private void OnPlayerLevelChanged(int newLevel)
    {
        Debug.Log($"⭐ [GameManager] 레벨 변경 감지: {newLevel}");
    }

    #endregion

    #region 🆕 Phase 2: 인트로/튜토리얼 플로우 관리
    
    /// <summary>
    /// 앱 최초 실행 여부 확인 (PlayerPrefs 기반)
    /// </summary>
    public bool IsFirstLaunch()
    {
        // PlayerPrefs에 키가 없으면 최초 실행
        bool hasLaunchedBefore = PlayerPrefs.HasKey(FIRST_LAUNCH_KEY);
        
        if (!hasLaunchedBefore)
        {
            Debug.Log("[GameManager] 🆕 최초 실행 감지");
            return true;
        }
        
        Debug.Log("[GameManager] 🔄 재실행 감지");
        return false;
    }
    
    /// <summary>
    /// 최초 실행 플래그 저장
    /// </summary>
    public void MarkAsLaunched()
    {
        PlayerPrefs.SetInt(FIRST_LAUNCH_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("[GameManager] 최초 실행 플래그 저장 완료");
    }
    
    /// <summary>
    /// 로그인 완료 후 호출 (최초 실행 판정)
    /// </summary>
    public void OnLoginComplete()
    {
        if (IsFirstLaunch())
        {
            // 최초 실행 → 인트로부터 시작
            Debug.Log("[GameManager] 최초 실행 플로우 시작 → 인트로 씬으로 이동");
            currentFlow = FlowType.FirstTime;
            MarkAsLaunched(); // PlayerPrefs 저장
            LoadIntroScene();
        }
        else
        {
            // 재실행 → 바로 로비로
            Debug.Log("[GameManager] 재실행 플로우 → 로비로 직접 이동");
            currentFlow = FlowType.ReturnFromGame;
            LoadLobbyScene();
        }
    }
    
    /// <summary>
    /// 인트로 씬 로드
    /// </summary>
    public void LoadIntroScene()
    {
        Debug.Log($"[GameManager] 인트로 씬 로드 (플로우: {currentFlow})");
        currentGameState = GameState.None;
        SceneManager.LoadScene(introSceneName);
    }
    
    /// <summary>
    /// 튜토리얼 씬 로드
    /// </summary>
    public void LoadTutorialScene()
    {
        Debug.Log($"[GameManager] 튜토리얼 씬 로드 (플로우: {currentFlow})");
        currentGameState = GameState.None;
        SceneManager.LoadScene(tutorialSceneName);
    }
    
    /// <summary>
    /// 인트로 다시보기 (로비에서 호출)
    /// </summary>
    public void ReplayIntro()
    {
        Debug.Log("[GameManager] 인트로 다시보기 시작");
        currentFlow = FlowType.ReplayIntro;
        LoadIntroScene();
    }
    
    /// <summary>
    /// 튜토리얼 다시보기 (로비에서 호출)
    /// </summary>
    public void ReplayTutorial()
    {
        Debug.Log("[GameManager] 튜토리얼 다시보기 시작");
        currentFlow = FlowType.ReplayTutorial;
        LoadTutorialScene();
    }
    
    /// <summary>
    /// 현재 플로우에 따라 다음 씬 결정
    /// </summary>
    public void ProceedToNextScene()
    {
        Debug.Log($"[GameManager] ProceedToNextScene 호출 (현재 플로우: {currentFlow})");
        
        switch (currentFlow)
        {
            case FlowType.FirstTime:
                // 최초 실행: 로비로 (튜토리얼 종료 후)
                Debug.Log("[GameManager] 최초 실행 완료 → 로비로 이동");
                LoadLobbyScene();
                break;
                
            case FlowType.ReplayIntro:
            case FlowType.ReplayTutorial:
                // 다시보기: 로비로 복귀
                Debug.Log("[GameManager] 다시보기 완료 → 로비로 복귀");
                LoadLobbyScene();
                break;
                
            case FlowType.ReturnFromGame:
                // 스테이지에서 복귀 → 로비
                Debug.Log("[GameManager] 게임 복귀 → 로비로 이동");
                LoadLobbyScene();
                break;
        }
    }
    
    #endregion

    protected override void OnDestroy()
    {
        // 🆕 추가: 이벤트 구독 해제
        PlayerDataManager.OnSelectedPlayerDataChanged -= OnPlayerDataChanged;
        PlayerDataManager.OnPlayerGoldChanged -= OnPlayerGoldChanged;
        PlayerDataManager.OnPlayerLevelChanged -= OnPlayerLevelChanged;
        
        base.OnDestroy();
    }
}