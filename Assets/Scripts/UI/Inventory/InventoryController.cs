using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🎮 통합 인벤토리 컨트롤러 (로비/인게임 공통 로직)
/// UI 레이어와 분리된 순수 비즈니스 로직
/// </summary>
public class InventoryController : MonoBehaviour
{
    public static InventoryController Instance { get; private set; }
    
    
    // 이벤트 시스템
    public event System.Action<bool> OnInventoryStateChanged;
    
    // 내부 상태
    private bool isInventoryOpen = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 🔧 수정: 루트 GameObject일 때만 DontDestroyOnLoad 적용
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // 자식 오브젝트인 경우 루트를 찾아서 적용
                DontDestroyOnLoad(transform.root.gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 🆕 LobbyUIController와 연결
        var lobbyUIController = FindObjectOfType<LobbyUIController>();
        if (lobbyUIController != null)
        {
            OnInventoryStateChanged += (isOpen) => {
                if (isOpen)
                {
                    lobbyUIController.ShowInventoryPanel();
                }
            };
        }
    }
    
    /// <summary>
    /// 인벤토리 열기 (토글 기능 제거)
    /// </summary>
    public void OpenInventory()
    {
        
        // 이미 열려있으면 아무것도 하지 않음
        if (isInventoryOpen)
        {
            return;
        }
        
        isInventoryOpen = true;
        
        
        // 구독자 확인
        if (OnInventoryStateChanged != null)
        {
            var subscriberCount = OnInventoryStateChanged.GetInvocationList().Length;
            OnInventoryStateChanged.Invoke(isInventoryOpen);
        }
        else
        {
            Debug.LogError($"🔴 [InventoryController] 구독자가 없습니다!");
        }
        
    }
    
    /// <summary>
    /// 인벤토리 닫기 (나중에 닫기 버튼용)
    /// </summary>
    public void CloseInventory()
    {
        
        if (!isInventoryOpen)
        {
            return;
        }
        
        isInventoryOpen = false;
        
        if (OnInventoryStateChanged != null)
        {
            OnInventoryStateChanged.Invoke(isInventoryOpen);
        }
        
    }
    
    public bool IsInventoryOpen => isInventoryOpen;
}
