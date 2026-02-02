using UnityEngine;
using System;
using System.Collections;
using CueSystem;

/// <summary>
/// 로비 패널 전환 관리자
/// 책임: 패널 전환, Z-Order 관리, 패널 우선순위 제어
/// </summary>
public class LobbyPanelManager : MonoBehaviour
{
    [Header("=== 패널 참조 ===")]
    public GameObject lobbyPanel;
    public GameObject stageSelectPanel;
    public GameObject inventoryPanel;
    public GameObject shopPanel;
    public GameObject characterInfoPanel;
    
    [Header("=== 컨트롤러 참조 ===")]
    public StageSelectPanelController stageSelectPanelController;
    
    // 이벤트
    public event Action OnPanelChanged;
    
    // 현재 활성 패널
    private GameObject currentActivePanel;
    
    /// <summary>
    /// 초기화
    /// </summary>
    public void Initialize()
    {
        Debug.Log("[LobbyPanelManager] 초기화 시작");
        
        // 패널 검증
        if (lobbyPanel == null) Debug.LogError("[LobbyPanelManager] lobbyPanel 누락!");
        if (stageSelectPanel == null) Debug.LogError("[LobbyPanelManager] stageSelectPanel 누락!");
        if (inventoryPanel == null) Debug.LogError("[LobbyPanelManager] inventoryPanel 누락!");
        if (shopPanel == null) Debug.LogError("[LobbyPanelManager] shopPanel 누락!");
        if (characterInfoPanel == null) Debug.LogError("[LobbyPanelManager] characterInfoPanel 누락!");
        
        // 초기 활성 패널 설정 (로비)
        currentActivePanel = lobbyPanel;
        
        Debug.Log("[LobbyPanelManager] 초기화 완료");
    }
    
    /// <summary>
    /// 로비 패널 표시
    /// </summary>
    public void ShowLobbyPanel()
    {
        Debug.Log("🏠 [LobbyPanelManager] ShowLobbyPanel 호출됨");
        
        // Cue 이벤트 발행
        EmitPanelCloseCue();
        
        // 패널 전환
        BringPanelToFront(lobbyPanel);
        
        Debug.Log("[LobbyPanelManager] 로비 패널을 최상위로 이동 완료");
    }
    
    /// <summary>
    /// 상점 패널 표시
    /// </summary>
    public void ShowShopPanel()
    {
        Debug.Log("🏪 [LobbyPanelManager] ShowShopPanel 호출됨");
        
        // Cue 이벤트 발행
        EmitButtonClickCue();
        EmitShopOpenCue();
        
        // 패널 전환
        BringPanelToFront(shopPanel);
        
        Debug.Log("[LobbyPanelManager] 상점 패널을 최상위로 이동 완료");
    }
    
    /// <summary>
    /// 인벤토리 패널 표시
    /// </summary>
    public void ShowInventoryPanel()
    {
        Debug.Log("🎒 [LobbyPanelManager] ShowInventoryPanel 호출됨");
        
        // Cue 이벤트 발행
        EmitButtonClickCue();
        EmitInventoryOpenCue();
        
        // 패널 전환
        BringPanelToFront(inventoryPanel);
        
        Debug.Log("[LobbyPanelManager] 인벤토리 패널을 최상위로 이동 완료");
    }
    
    /// <summary>
    /// 캐릭터 정보 패널 표시
    /// </summary>
    public void ShowCharacterInfoPanel()
    {
        Debug.Log("👤 [LobbyPanelManager] ShowCharacterInfoPanel 호출됨");
        
        // Cue 이벤트 발행
        EmitButtonClickCue();
        EmitCharacterOpenCue();
        
        // 패널 전환
        BringPanelToFront(characterInfoPanel);
        
        Debug.Log("[LobbyPanelManager] 캐릭터 정보 패널을 최상위로 이동 완료");
    }
    
    /// <summary>
    /// 스테이지 선택 패널 표시
    /// </summary>
    public void ShowStageSelectPanel()
    {
        Debug.Log("🎯 [LobbyPanelManager] ShowStageSelectPanel 호출됨");
        
        // 패널 전환
        BringPanelToFront(stageSelectPanel);
        
        // StageSelectPanelController로 위임
        if (stageSelectPanelController != null)
        {
            stageSelectPanelController.ShowPanel();
        }
        else
        {
            Debug.LogError("[LobbyPanelManager] stageSelectPanelController가 null입니다!");
        }
        
        Debug.Log("[LobbyPanelManager] 스테이지 선택 패널을 최상위로 이동 완료");
    }
    
    /// <summary>
    /// 패널을 최상위로 가져오기 (Z-Order 제어)
    /// </summary>
    private void BringPanelToFront(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("[LobbyPanelManager] 패널이 null입니다!");
            return;
        }
        
        // Z-Order 변경 (최상위로 이동)
        panel.transform.SetAsLastSibling();
        
        // 현재 활성 패널 업데이트
        currentActivePanel = panel;
        
        // 이벤트 발행
        OnPanelChanged?.Invoke();
        
        Debug.Log($"🔝 [LobbyPanelManager] {panel.name} 패널을 최상위로 이동");
    }
    
    /// <summary>
    /// 현재 활성 패널 반환
    /// </summary>
    public GameObject GetCurrentActivePanel()
    {
        return currentActivePanel;
    }
    
    /// <summary>
    /// 특정 패널이 현재 활성 패널인지 확인
    /// </summary>
    public bool IsActivePanel(GameObject panel)
    {
        return currentActivePanel == panel;
    }
    
    #region Cue 이벤트 발행
    
    private void EmitButtonClickCue()
    {
        var context = new CueContext
        {
            position = Vector3.zero,
            actorType = ActorType.UI
        };
        CueEmitter.Emit("ui.button.click", "UI", context);
    }
    
    private void EmitPanelCloseCue()
    {
        var context = new CueContext
        {
            position = Vector3.zero,
            actorType = ActorType.UI
        };
        CueEmitter.Emit("ui.panel.close", "UI", context);
    }
    
    private void EmitShopOpenCue()
    {
        var context = new CueContext
        {
            position = Vector3.zero,
            actorType = ActorType.UI
        };
        CueEmitter.Emit("ui.shop.open", "UI", context);
    }
    
    private void EmitInventoryOpenCue()
    {
        var context = new CueContext
        {
            position = Vector3.zero,
            actorType = ActorType.UI
        };
        CueEmitter.Emit("ui.inventory.open", "UI", context);
    }
    
    private void EmitCharacterOpenCue()
    {
        var context = new CueContext
        {
            position = Vector3.zero,
            actorType = ActorType.UI
        };
        CueEmitter.Emit("ui.character.open", "UI", context);
    }
    
    #endregion
}

