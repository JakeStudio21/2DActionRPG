using UnityEngine;

/// <summary>
/// 게임 시작 시 필요한 매니저들을 초기화하는 부트스트랩 매니저
/// 게임 시작 씬에만 배치하고, 다른 씬에는 매니저들을 직접 배치하지 않음
/// </summary>
public class BootstrapManager : MonoBehaviour
{
    [Header("매니저 프리팹들")]
    [SerializeField] private GameObject gameManagerPrefab;
    [SerializeField] private GameObject gamePoolManagerPrefab;
    
    private void Awake()
    {
        Debug.Log("[BootstrapManager] 게임 초기화 시작");
        InitializeManagers();
    }
    
    /// <summary>
    /// 필수 매니저들을 초기화
    /// </summary>
    private void InitializeManagers()
    {
        // GameManager 초기화
        if (GameManager.Instance == null && gameManagerPrefab != null)
        {
            GameObject gameManager = Instantiate(gameManagerPrefab);
            gameManager.name = "GameManager";
            Debug.Log("[BootstrapManager] GameManager 생성");
        }
        
        // GamePoolManager 초기화
        if (GamePoolManager.Instance == null && gamePoolManagerPrefab != null)
        {
            GameObject poolManager = Instantiate(gamePoolManagerPrefab);
            poolManager.name = "GamePoolManager";
            Debug.Log("[BootstrapManager] GamePoolManager 생성");
        }
        
        // 부트스트랩 완료 후 자신은 제거
        Destroy(gameObject);
    }
}
