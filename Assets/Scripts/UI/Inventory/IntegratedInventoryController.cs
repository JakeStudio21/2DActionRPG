using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 🆕 추가: SceneManager 사용을 위해 필요

/// <summary>
/// 🏠 LobbyInventoryUI - 로비 전용 인벤토리 UI
/// 책임:
/// - 인벤토리 아이템 표시
/// - 아이템 상세 정보 표시 (DetailPanel)
/// - 아이템 착용/해제 기능
/// - 로비 전용 UI 상호작용
/// 
/// 의존성:
/// - PlayerDataManager (데이터 소스)
/// - LobbyInventoryController (제어)
/// </summary>

/// <summary>
/// 🏪 ShopInventoryUI - 상점 전용 인벤토리 UI  
/// 책임:
/// - 판매용 아이템 선택 표시
/// - 상점 거래를 위한 아이템 클릭 처리
/// 
/// 제외 기능:
/// - 아이템 착용 (로비 전용)
/// - 상세 정보 표시 (상점은 DetailPanel 별도)
/// 
/// 의존성:
/// - PlayerDataManager (데이터 소스)
/// - ShopUIController (거래 제어)
/// </summary>

/// <summary>
/// 🎮 IntegratedInventoryController - 인게임 전용 컨트롤러
/// 책임:
/// - 인게임 인벤토리 토글 (I키, 가방 버튼)
/// - 무기 교체 중심 상호작용
/// - ActiveInventory와 연동
/// 
/// 의존성:
/// - PlayerDataManager (데이터 소스)
/// - ActiveInventory (인게임 UI)
/// - ActiveWeapon (무기 교체)
/// </summary>
public class IntegratedInventoryController : MonoBehaviour
{
    [Header("🔧 UI 참조 (자식이 아닌 참조로 관리)")]
    [SerializeField] private GameObject activeInventoryPanel;     // ActiveInventory GameObject 참조
    [SerializeField] private GameObject equippedItemsPanel;      // EquippedItemsPanel GameObject 참조
    
    [Header("🎮 패널 제어")]
    [SerializeField] private Button bagButton;                    // 가방 버튼 (패널 토글용)
    [SerializeField] private KeyCode toggleKey = KeyCode.I;       // 패널 토글 키 (I키)
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;   // 패널 닫기 키 (ESC키)
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 내부 상태
    private bool isInventoryOpen = false;
    
    // 초기화 중 패널을 투명하게 만들기 위한 CanvasGroup 캐시
    private CanvasGroup activeInventoryCanvasGroup;
    private CanvasGroup equippedItemsCanvasGroup;
    
    void Start()
    {
        // 🔧 수정: IntegratedInventoryController는 인게임에서만 활성화
        if (SceneManager.GetActiveScene().name == "Lobby" || 
            SceneManager.GetActiveScene().name.Contains("Lobby"))
        {
            this.enabled = false;
            Debug.Log("🔒 [IntegratedInventoryController] 로비에서 비활성화됨");
            return;
        }
        
        SetupControllerEvents();
        InitializeIntegratedInventory();
    }
    
    void Update()
    {
        HandleInputs();
    }
    
    /// <summary>
    /// 통합 인벤토리 초기화 (참조 방식)
    /// </summary>
    private void InitializeIntegratedInventory()
    {
        if (activeInventoryPanel != null)
        {
            // CanvasGroup 캐시: 초기화 중 패널을 투명하게 유지하기 위해 사용
            activeInventoryCanvasGroup = activeInventoryPanel.GetComponent<CanvasGroup>();
            if (activeInventoryCanvasGroup != null)
            {
                activeInventoryCanvasGroup.alpha = 0f;
                activeInventoryCanvasGroup.blocksRaycasts = false;
                activeInventoryCanvasGroup.interactable = false;
            }
            
            Debug.Log($"✅ [IntegratedInventoryController] ActiveInventory 참조 연결 완료");
            
            // ActiveInventory 초기화 완료 이벤트 구독
            ActiveInventory.OnActiveInventoryInitialized += OnActiveInventoryInitialized;
        }
        else
        {
            Debug.LogError("🔴 [IntegratedInventoryController] ActiveInventory 참조가 설정되지 않았습니다!");
        }
        
        if (equippedItemsPanel != null)
        {
            // CanvasGroup 캐시: 초기화 중 패널을 투명하게 유지하기 위해 사용
            equippedItemsCanvasGroup = equippedItemsPanel.GetComponent<CanvasGroup>();
            if (equippedItemsCanvasGroup != null)
            {
                equippedItemsCanvasGroup.alpha = 0f;
                equippedItemsCanvasGroup.blocksRaycasts = false;
                equippedItemsCanvasGroup.interactable = false;
            }
            
            Debug.Log($"✅ [IntegratedInventoryController] EquippedItemsPanel 참조 연결 완료");
        }
        else
        {
            Debug.LogError("🔴 [IntegratedInventoryController] EquippedItemsPanel 참조가 설정되지 않았습니다!");
        }
        
        // BagButton 이벤트 연결
        if (bagButton != null)
        {
            bagButton.onClick.AddListener(ToggleInventoryPanel);
            Debug.Log($"✅ [IntegratedInventoryController] BagButton 이벤트 연결 완료");
        }
        else
        {
            Debug.LogError("🔴 [IntegratedInventoryController] BagButton 참조가 설정되지 않았습니다!");
        }
        
        isInventoryOpen = false;
        
        if (showDebugLogs)
            Debug.Log("✅ [IntegratedInventoryController] 참조 방식 초기화 완료");
    }

    /// <summary>
    /// ActiveInventory 초기화 완료 시 호출.
    /// CanvasGroup을 원상 복구한 뒤 패널을 비활성화하여 시작 시 깜빡임을 제거한다.
    /// </summary>
    private void OnActiveInventoryInitialized()
    {
        Debug.Log("🎯 [IntegratedInventoryController] ActiveInventory 초기화 완료 - 패널 비활성화 시작");
        
        if (activeInventoryPanel != null)
        {
            // CanvasGroup 원상 복구 후 비활성화 (다음 SetActive(true) 시 정상 표시됨)
            if (activeInventoryCanvasGroup != null)
            {
                activeInventoryCanvasGroup.alpha = 1f;
                activeInventoryCanvasGroup.blocksRaycasts = true;
                activeInventoryCanvasGroup.interactable = true;
            }
            activeInventoryPanel.SetActive(false);
        }
        
        if (equippedItemsPanel != null)
        {
            if (equippedItemsCanvasGroup != null)
            {
                equippedItemsCanvasGroup.alpha = 1f;
                equippedItemsCanvasGroup.blocksRaycasts = true;
                equippedItemsCanvasGroup.interactable = true;
            }
            equippedItemsPanel.SetActive(false);
        }
        
        Debug.Log("✅ [IntegratedInventoryController] 인벤토리 초기화 및 비활성화 완료");
    }

    /// <summary>
    /// 🆕 ActiveInventory 초기화 완료를 기다린 후 패널 숨김
    /// </summary>
    private IEnumerator WaitForInventoryInitializationThenHide()
    {
        Debug.Log("⏰ [IntegratedInventoryController] ActiveInventory 초기화 대기 중...");
        
        // 2초 대기 (ActiveInventory 초기화 완료 시간)
        yield return new WaitForSeconds(2f);
        
        // 이제 안전하게 패널들 숨김
        if (activeInventoryPanel != null)
        {
            activeInventoryPanel.SetActive(false);
            Debug.Log("🔒 [IntegratedInventoryController] ActiveInventory 초기화 완료 후 숨김");
        }
        
        if (equippedItemsPanel != null)
        {
            equippedItemsPanel.SetActive(false);
            Debug.Log("🔒 [IntegratedInventoryController] EquippedItemsPanel 초기화 완료 후 숨김");
        }
    }
    
    /// <summary>
    /// 키보드 입력 처리
    /// </summary>
    private void HandleInputs()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetKeyDown(toggleKey))
            ToggleInventoryPanel();

        if (Input.GetKeyDown(closeKey) && isInventoryOpen)
            CloseInventoryPanel();
#endif
    }
    
    /// <summary>
    /// 인벤토리 패널 토글 (참조 방식)
    /// </summary>
    public void ToggleInventoryPanel()
    {
        if (isInventoryOpen)
        {
            CloseInventoryPanel();
        }
        else
        {
            OpenInventoryPanel();
        }
    }
    
    /// <summary>
    /// 인벤토리 패널 열기 (참조 방식)
    /// </summary>
    public void OpenInventoryPanel()
    {
        if (activeInventoryPanel != null)
        {
            activeInventoryPanel.SetActive(true);
        }
        
        if (equippedItemsPanel != null)
        {
            equippedItemsPanel.SetActive(true);
        }
        
        isInventoryOpen = true;
        
        // BagButton 시각적 피드백
        if (bagButton != null)
        {
            var colors = bagButton.colors;
            colors.normalColor = Color.yellow;
            bagButton.colors = colors;
        }
        
        if (showDebugLogs)
            Debug.Log("🎒 [IntegratedInventoryController] 인벤토리 패널 열림");
    }
    
    /// <summary>
    /// 인벤토리 패널 닫기 (참조 방식)
    /// </summary>
    public void CloseInventoryPanel()
    {
        if (activeInventoryPanel != null)
        {
            activeInventoryPanel.SetActive(false);
        }
        
        if (equippedItemsPanel != null)
        {
            equippedItemsPanel.SetActive(false);
        }
        
        isInventoryOpen = false;
        
        // BagButton 시각적 피드백 원상복구
        if (bagButton != null)
        {
            var colors = bagButton.colors;
            colors.normalColor = Color.white;
            bagButton.colors = colors;
        }
        
        if (showDebugLogs)
            Debug.Log("🎒 [IntegratedInventoryController] 인벤토리 패널 닫힘");
    }
    
    /// <summary>
    /// 현재 패널 열림 상태 확인
    /// </summary>
    public bool IsInventoryOpen
    {
        get { return isInventoryOpen; }
    }

    /// <summary>
    /// 🔧 수정: 컨트롤러 이벤트 설정 (통합)
    /// </summary>
    private void SetupControllerEvents()
    {
        // 🗑️ 제거: BagButton 이벤트 연결 (InitializeIntegratedInventory에서 처리)
        // if (bagButton != null)
        // {
        //     bagButton.onClick.AddListener(() => {
        //         ToggleInventoryPanel();
        //     });
        // }
        
        if (showDebugLogs)
            Debug.Log("✅ [IntegratedInventoryController] 컨트롤러 이벤트 설정 완료");
    }
    
    /// <summary>
    /// 🆕 인게임 인벤토리 UI 새로고침
    /// </summary>
    private void RefreshInventoryUI()
    {
        // 🔧 수정: ActiveInventory는 자체적으로 PlayerDataManager 이벤트를 구독하므로
        // 추가 새로고침 호출이 불필요함
        
        if (showDebugLogs)
            Debug.Log("🎮 [IntegratedInventoryController] 인벤토리 변경 감지됨");
        
        // 필요시 인게임 전용 UI 업데이트 로직 추가
        // (예: 인벤토리 개수 표시, 가방 버튼 상태 등)
    }
    
    /// <summary>
    /// 🆕 인게임 슬롯 클릭 처리 (무기 교체 중심)
    /// V2: ItemInstanceID 추가 (인게임은 사용하지 않음)
    /// </summary>
    private void HandleSlotClicked(EquipmentData equipmentData, int slotIndex, ItemInstanceID instanceId = default)
    {
        if (equipmentData == null) return;
        
        // 🔧 수정: 인게임에서는 주로 무기 교체
        if (equipmentData.equipmentType == EquipmentType.Weapon)
        {
            // 무기 교체 로직 (ActiveWeapon과 연동)
            var activeWeapon = FindObjectOfType<ActiveWeapon>();
            if (activeWeapon != null)
            {
                // 무기 교체 처리
                // activeWeapon.ChangeWeapon(equipmentData);
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"🎮 [IntegratedInventoryController] 인게임 슬롯 클릭: {equipmentData.equipmentName}");
    }
    
    void OnDestroy()
    {
        // 🆕 이벤트 구독 해제
        ActiveInventory.OnActiveInventoryInitialized -= OnActiveInventoryInitialized;
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnInventoryChanged -= RefreshInventoryUI;
            PlayerDataManager.Instance.OnSlotClicked -= HandleSlotClicked;
        }
    }
}