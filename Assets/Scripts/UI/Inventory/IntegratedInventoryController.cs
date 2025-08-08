using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 🆕 추가: SceneManager 사용을 위해 필요

/// <summary>
/// 🎮 인게임 인벤토리 UI (View Only)
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
        // 🗑️ 제거: 독립적인 이벤트 연결 삭제
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
        // 🔧 수정: 패널들의 초기화를 기다린 후 비활성화하도록 변경
        if (activeInventoryPanel != null)
        {
            // activeInventoryPanel.SetActive(false); // 🗑️ 제거: 즉시 비활성화 금지
            Debug.Log($"✅ [IntegratedInventoryController] ActiveInventory 참조 연결 완료");
            
            // 🆕 추가: ActiveInventory 초기화 완료까지 대기 후 비활성화
            StartCoroutine(WaitForInventoryInitializationThenHide());
        }
        else
        {
            Debug.LogError("🔴 [IntegratedInventoryController] ActiveInventory 참조가 설정되지 않았습니다!");
        }
        
        if (equippedItemsPanel != null)
        {
            // equippedItemsPanel.SetActive(false); // 🗑️ 제거: 즉시 비활성화 금지
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
        
        isInventoryOpen = false; // 🔧 수정: 논리적 상태만 false로 설정
        
        if (showDebugLogs)
            Debug.Log("✅ [IntegratedInventoryController] 참조 방식 초기화 완료");
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
        // I키 토글
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleInventoryPanel();
        }
        
        // ESC키 닫기
        if (Input.GetKeyDown(closeKey) && isInventoryOpen)
        {
            CloseInventoryPanel();
        }
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

    private void SetupControllerEvents()
    {
        // 🔧 Controller 이벤트 구독
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryStateChanged += OnInventoryStateChanged;
        }
        
        // 🔧 버튼 이벤트를 Controller로 연결
        if (bagButton != null)
        {
            bagButton.onClick.AddListener(() => {
                InventoryController.Instance?.OpenInventory();
            });
        }
    }
    
    private void OnInventoryStateChanged(bool isOpen)
    {
        // 인게임 UI 업데이트 로직
        if (activeInventoryPanel != null)
        {
            activeInventoryPanel.SetActive(isOpen);
        }
        
        if (equippedItemsPanel != null)
        {
            equippedItemsPanel.SetActive(isOpen);
        }
        
        isInventoryOpen = isOpen;
    }
}