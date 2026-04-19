using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 🏠 로비 전용 인벤토리 컨트롤러
/// InventoryController 의존성 제거하고 직접 UI 제어
/// </summary>
public class LobbyInventoryController : MonoBehaviour
{
    [Header("🔧 통합 UI 제어")]
    [SerializeField] private LobbyInventoryUI lobbyInventoryUI;
    [SerializeField] private EquippedItemsUI equippedItemsUI;
    
    [Header("🎮 로비 전용 설정")]
    // 🗑️ 제거: 사용하지 않는 필드들
    // [SerializeField] private bool enableDetailPanel = true;
    // [SerializeField] private bool enableInventoryToggle = true;
    
    [Header("📊 디버그")]
    
    // 🆕 로비 UI 컨트롤러 참조
    private LobbyUIController lobbyUIController;
    
    void Start()
    {
        if (SceneManager.GetActiveScene().name != "Lobby" && 
            !SceneManager.GetActiveScene().name.Contains("Lobby"))
        {
            this.enabled = false;
            return;
        }
        
        InitializeController();
    }

    /// <summary>
    /// 🔧 수정: 컨트롤러 초기화 (설정 필드 활용)
    /// </summary>
    private void InitializeController()
    {
        // LobbyUIController 찾기
        lobbyUIController = FindObjectOfType<LobbyUIController>();
        if (lobbyUIController == null)
        {
            Debug.LogError("❌ [LobbyInventoryController] LobbyUIController를 찾을 수 없습니다!");
            return;
        }
        
        // PlayerDataManager 이벤트 구독
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnInventoryChanged += RefreshInventoryUI;
            PlayerDataManager.Instance.OnSlotClicked += HandleSlotClicked;
            
        }
        
        InitializeIntegratedInventory();
        
        // 🗑️ 제거: 삭제된 필드 참조 제거
    }

    /// <summary>
    /// 통합 인벤토리 초기화
    /// </summary>
    private void InitializeIntegratedInventory()
    {
        if (equippedItemsUI != null)
        {
            equippedItemsUI.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 🔧 수정: 인벤토리 열기 (직접 LobbyUIController 제어)
    /// </summary>
    public void OpenInventory()
    {
        if (lobbyUIController != null)
        {
            lobbyUIController.ShowInventoryPanel();
            
        }
        else
        {
            Debug.LogError("❌ [LobbyInventoryController] LobbyUIController 참조가 없습니다!");
        }
    }

    /// <summary>
    /// 🔧 수정: 인벤토리 토글 (직접 제어)
    /// </summary>
    public void ToggleInventory()
    {
        // 로비에서는 단순히 열기만 수행 (닫기는 LobbyUIController.ShowLobbyPanel()로)
        OpenInventory();
    }

    /// <summary>
    /// 상세 패널 닫기
    /// </summary>
    public void CloseDetailPanel()
    {
        if (lobbyInventoryUI != null)
        {
            // DetailPanel 닫기 로직 (필요시 구현)
        }
    }
    
    /// <summary>
    /// 🆕 인벤토리 UI 새로고침
    /// </summary>
    private void RefreshInventoryUI()
    {
        if (lobbyInventoryUI != null)
        {
            lobbyInventoryUI.RefreshInventoryUI();
        }
    }
    
    /// <summary>
    /// 🆕 슬롯 클릭 처리
    /// V2: ItemInstanceID 추가 (로비는 ItemDetailPopup이 자동 처리)
    /// </summary>
    private void HandleSlotClicked(EquipmentData equipmentData, int slotIndex, ItemInstanceID instanceId = default)
    {
        
        // 필요시 추가 처리 (DetailPanel 표시 등)
        // ItemDetailPopup이 OnSlotClicked 이벤트를 구독하여 자동으로 팝업 표시
    }
    
    void OnDestroy()
    {
        // 🆕 이벤트 구독 해제
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnInventoryChanged -= RefreshInventoryUI;
            PlayerDataManager.Instance.OnSlotClicked -= HandleSlotClicked;
        }
    }
}