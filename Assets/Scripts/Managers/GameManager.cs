using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 런타임 플레이어 선택 데이터 (메모리에만 존재)
/// ScriptableObject 대신 씬 전환 시 DontDestroyOnLoad로 유지됨
/// </summary>
[System.Serializable]
public class PlayerSelectionData
{
    public PlayerType selectedType = PlayerType.None;
    public string weaponName = "";
    
    public PlayerSelectionData()
    {
        selectedType = PlayerType.None;
        weaponName = "";
    }
    
    public PlayerSelectionData(PlayerType type, string weapon)
    {
        selectedType = type;
        weaponName = weapon;
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
    [SerializeField] private string loadingSceneName = "Loading";
    
    [Header("게임 데이터")]
    public SelectedPlayerData selectedPlayerData;
    
    // 런타임 플레이어 데이터 (DontDestroyOnLoad로 유지됨)
    [System.NonSerialized]
    private PlayerSelectionData runtimePlayerData = new PlayerSelectionData();
    
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
        
        Debug.Log($"[GameManager] Awake - selectedPlayerData: {selectedPlayerData}");
        if (selectedPlayerData != null)
        {
            Debug.Log($"[GameManager] Awake - selectedPlayerType: {selectedPlayerData.selectedPlayerType}, weaponName: {selectedPlayerData.weaponName}");
            Debug.Log($"[GameManager] Awake - selectedPlayerData instanceID: {selectedPlayerData.GetInstanceID()}");
        }
        
        Debug.Log($"[GameManager] 런타임 데이터: {runtimePlayerData.selectedType}, {runtimePlayerData.weaponName}");

        InitializeGame();
    }
    
    private void Start()
    {
        // 초기 씬에서 자동으로 로비로 이동
        if (currentGameState == GameState.None)
        {
            LoadLobbyScene();
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
        
        // 런타임 데이터 초기화 (새로 생성된 GameManager인 경우)
        if (runtimePlayerData == null)
        {
            runtimePlayerData = new PlayerSelectionData();
        }
    }
    
    /// <summary>
    /// 런타임 플레이어 데이터 설정 (LobbyManager에서 호출)
    /// </summary>
    public void SetRuntimePlayerData(PlayerType playerType, string weaponName)
    {
        runtimePlayerData.selectedType = playerType;
        runtimePlayerData.weaponName = weaponName;
        
        Debug.Log($"[GameManager] 런타임 데이터 설정 완료: {playerType}, {weaponName}");
    }

    /// <summary>
    /// 런타임 플레이어 데이터 가져오기 (PlayerSpawner에서 호출)
    /// </summary>
    public PlayerSelectionData GetRuntimePlayerData()
    {
        return runtimePlayerData;
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
    /// 로딩 씬을 거쳐 게임 씬으로 이동
    /// </summary>
    public void LoadGameScene(string sceneName)
    {
        StartCoroutine(LoadGameSceneCoroutine(sceneName));
    }

    private IEnumerator LoadGameSceneCoroutine(string sceneName)
    {
        // ⭐ 수정: 런타임 데이터로 유효성 검증
        if (runtimePlayerData == null || runtimePlayerData.selectedType == PlayerType.None)
        {
            Debug.LogError("[GameManager] 런타임 플레이어 데이터가 유효하지 않습니다! 로비로 돌아갑니다.");
            Debug.LogError($"[GameManager] 현재 런타임 데이터: {runtimePlayerData?.selectedType}, {runtimePlayerData?.weaponName}");
            LoadLobbyScene();
            yield break;
        }

        Debug.Log($"[GameManager] 씬 전환 시작 - {sceneName}");
        Debug.Log($"[GameManager] 전달될 런타임 데이터: {runtimePlayerData.selectedType}, {runtimePlayerData.weaponName}");

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
}