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
        Debug.Log("[BootstrapManager] 매니저 초기화 시작");
        
        try
        {
            // GameManager 초기화
            if (GameManager.Instance == null)
            {
                if (gameManagerPrefab != null)
                {
                    GameObject gameManager = Instantiate(gameManagerPrefab);
                    gameManager.name = "GameManager";
                    Debug.Log("[BootstrapManager] GameManager 생성 성공");
                }
                else
                {
                    Debug.LogError("[BootstrapManager] GameManager 프리팹이 할당되지 않았습니다!");
                }
            }
            else
            {
                Debug.Log("[BootstrapManager] GameManager가 이미 존재합니다.");
            }
            
            // GamePoolManager 초기화
            if (GamePoolManager.Instance == null)
            {
                if (gamePoolManagerPrefab != null)
                {
                    GameObject poolManager = Instantiate(gamePoolManagerPrefab);
                    poolManager.name = "GamePoolManager";
                    Debug.Log("[BootstrapManager] GamePoolManager 생성 성공");
                }
                else
                {
                    Debug.LogError("[BootstrapManager] GamePoolManager 프리팹이 할당되지 않았습니다!");
                }
            }
            else
            {
                Debug.Log("[BootstrapManager] GamePoolManager가 이미 존재합니다.");
            }
            
            Debug.Log("[BootstrapManager] 매니저 초기화 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BootstrapManager] 매니저 초기화 중 오류 발생: {e.Message}");
            Debug.LogError($"[BootstrapManager] 스택 트레이스: {e.StackTrace}");
        }
        
        // 부트스트랩 완료 후 자신은 제거
        Debug.Log("[BootstrapManager] 자신을 제거합니다.");
        Destroy(gameObject);
    }
}
