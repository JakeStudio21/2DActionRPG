using UnityEngine;

/// <summary>
/// 로비 관련 기능을 관리하는 매니저
/// 캐릭터 선택, 상점, 게임 시작 등의 기능
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }
    
    [Header("플레이어 선택 데이터")]
    public PlayerSelectionData playerSelection = new PlayerSelectionData();
    
    private void Awake()
    {
        // LobbyManager는 로비 씬에서만 존재 (DontDestroyOnLoad 사용하지 않음)
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[LobbyManager] 생성됨");
        }
        else
        {
            Debug.LogWarning("[LobbyManager] 중복 LobbyManager가 생성되어 파괴됩니다.");
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // 필수 매니저들 존재 확인만 수행 (생성하지 않음)
        CheckRequiredManagers();
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("[LobbyManager] 파괴됨");
        }
    }
    
    /// <summary>
    /// 필수 매니저들이 존재하는지 확인 (경고만 출력)
    /// </summary>
    private void CheckRequiredManagers()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[LobbyManager] GameManager가 없습니다! 게임이 제대로 작동하지 않을 수 있습니다.");
        }
        else
        {
            Debug.Log("[LobbyManager] GameManager 연결 확인됨");
        }
        
        if (GamePoolManager.Instance == null)
        {
            Debug.LogWarning("[LobbyManager] GamePoolManager가 없습니다! 오브젝트 풀링이 작동하지 않을 수 있습니다.");
        }
        else
        {
            Debug.Log("[LobbyManager] GamePoolManager 연결 확인됨");
        }
    }
    
    /// <summary>
    /// 플레이어 클래스 선택
    /// </summary>
    public void SelectClass(PlayerType type, string weapon)
    {
        playerSelection.selectedType = type;
        playerSelection.weaponName = weapon;
        Debug.Log($"[LobbyManager] 클래스 선택: {type}, 무기: {weapon}");
    }
    
    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame(string sceneName)
    {
        if (playerSelection.selectedType == PlayerType.None)
        {
            Debug.LogWarning("[LobbyManager] 플레이어 클래스가 선택되지 않았습니다!");
            return;
        }
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("[LobbyManager] GameManager가 없습니다!");
            return;
        }
        
        // ⭐ 수정: 런타임 데이터로 설정 (ScriptableObject 대신)
        GameManager.Instance.SetRuntimePlayerData(playerSelection.selectedType, playerSelection.weaponName);
        
        Debug.Log($"[LobbyManager] 로비를 떠납니다. 선택된 클래스: {playerSelection.selectedType}, 무기: {playerSelection.weaponName}");
        
        // 게임 씬으로 이동
        GameManager.Instance?.LoadGameScene(sceneName);
    }
}
