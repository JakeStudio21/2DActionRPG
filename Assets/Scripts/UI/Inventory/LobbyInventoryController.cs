using UnityEngine;

/// <summary>
/// 🏠 로비 전용 인벤토리 컨트롤러
/// 조합(Composition) 방식으로 기존 시스템 재사용 + 로비 전용 로직 추가
/// </summary>
public class LobbyInventoryController : MonoBehaviour
{
    [Header("🔧 통합 UI 제어")]
    [SerializeField] private LobbyInventoryUI lobbyInventoryUI;
    [SerializeField] private EquippedItemsUI equippedItemsUI;  // 🆕 추가
    
    [Header("🎮 로비 전용 설정")]
    [SerializeField] private bool enableDetailPanel = true;      // 상세 패널 활성화 여부
    [SerializeField] private bool enableInventoryToggle = true;  // 인벤토리 토글 활성화 여부
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;

    void Start()
    {
        InitializeIntegratedInventory();
    }

    /// <summary>
    /// 통합 인벤토리 초기화 (인벤토리 + 장착 패널)
    /// </summary>
    private void InitializeIntegratedInventory()
    {
        // 🆕 장착 패널 활성화
        if (equippedItemsUI != null)
        {
            equippedItemsUI.gameObject.SetActive(true);
        }
        
        // 기존 인벤토리 초기화 유지...
    }

    /// <summary>
    /// 로비 전용 기능 설정
    /// </summary>
    private void ConfigureLobbyFeatures()
    {
        // 상세 패널 기능 설정
        if (!enableDetailPanel)
        {
            // 상세 패널 기능 비활성화 (필요시)
            if (showDebugLogs)
                Debug.Log("⚠️ [LobbyInventoryController] 상세 패널 기능이 비활성화되었습니다.");
        }

        // 인벤토리 토글 기능 설정
        if (!enableInventoryToggle)
        {
            // 토글 기능 비활성화 (필요시)
            if (showDebugLogs)
                Debug.Log("⚠️ [LobbyInventoryController] 인벤토리 토글 기능이 비활성화되었습니다.");
        }
    }

    /// <summary>
    /// 외부에서 인벤토리 열기/닫기 (프로그래매틱 제어)
    /// </summary>
    public void ToggleInventory()
    {
        if (lobbyInventoryUI != null)
        {
            lobbyInventoryUI.ToggleInventoryPanel();
        }
    }

    /// <summary>
    /// 인벤토리 열기
    /// </summary>
    public void OpenInventory()
    {
        if (lobbyInventoryUI != null)
        {
            lobbyInventoryUI.ToggleInventoryPanel();
        }
    }

    /// <summary>
    /// 상세 패널 닫기
    /// </summary>
    public void CloseDetailPanel()
    {
        if (lobbyInventoryUI != null)
        {
            lobbyInventoryUI.CloseDetailPanel();
        }
    }

    /// <summary>
    /// 로비 전용 치트 기능 (디버그용)
    /// </summary>
    [ContextMenu("테스트 아이템 추가")]
    public void AddTestItem()
    {
        if (PlayerDataManager.Instance != null)
        {
            // 테스트용 아이템 추가 로직
            Debug.Log("🧪 [LobbyInventoryController] 테스트 아이템 추가 (구현 예정)");
        }
    }
}