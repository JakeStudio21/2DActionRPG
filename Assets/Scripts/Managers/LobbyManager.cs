using UnityEngine;

/// <summary>
/// 로비 관련 기능을 관리하는 매니저
/// 캐릭터 선택, 상점, 게임 시작 등의 기능
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }
    
    [Header("플레이어 선택 데이터")]
    private PlayerType selectedPlayerType = PlayerType.None;
    private string selectedWeaponName = "";
    
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
        selectedPlayerType = type;
        selectedWeaponName = weapon;
        
        // ⭐ 기존: GameManager.selectedPlayerData에 설정
        if (GameManager.Instance != null && GameManager.Instance.selectedPlayerData != null)
        {
            GameManager.Instance.selectedPlayerData.selectedPlayerType = type;
            GameManager.Instance.selectedPlayerData.weaponName = weapon;
            Debug.Log($"[LobbyManager] GameManager.selectedPlayerData 설정 완료: {type}, 무기: {weapon}");
        }
        else
        {
            Debug.LogError("[LobbyManager] GameManager 또는 selectedPlayerData가 없습니다!");
        }
        
        // 🆕 핵심 추가: PlayerDataManager에도 즉시 설정
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SetCurrentPlayerType(type);
            Debug.Log($"💾 [LobbyManager] PlayerDataManager에 캐릭터 타입 설정: {type}");
        }
        else
        {
            Debug.LogWarning("⚠️ [LobbyManager] PlayerDataManager가 없어서 설정을 건너뜁니다.");
        }
        
        Debug.Log($"[LobbyManager] 클래스 선택: {type}, 무기: {weapon}");
    }
    
    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame(string sceneName)
    {
        if (selectedPlayerType == PlayerType.None)
        {
            Debug.LogWarning("[LobbyManager] 플레이어 클래스가 선택되지 않았습니다!");
            return;
        }
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("[LobbyManager] GameManager가 없습니다!");
            return;
        }
        
        // ⭐ 수정: 기존 SelectedPlayerData로 직접 설정 (원래 방식 복구)
        GameManager.Instance.selectedPlayerData.selectedPlayerType = selectedPlayerType;
        GameManager.Instance.selectedPlayerData.weaponName = selectedWeaponName;
        
        Debug.Log($"[LobbyManager] 로비를 떠납니다. 선택된 클래스: {selectedPlayerType}, 무기: {selectedWeaponName}");
        
        // 게임 씬으로 이동
        GameManager.Instance?.LoadGameScene(sceneName);
    }
}
