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
    public GameObject dungeonSelectPanel;   // 🏰 던전 선택 패널 (Phase 1)
    public GameObject inventoryPanel;
    public GameObject shopPanel;
    public GameObject characterInfoPanel;
    public GameObject workshopPanel;        // 🆕 공방 패널
    public GameObject skillBookPanel;       // 🆕 스킬북 패널 (Phase 3-Revision)
    
    [Header("=== 컨트롤러 참조 ===")]
    public StageSelectPanelController stageSelectPanelController;
    public DungeonSelectPanelController dungeonSelectPanelController; // 🏰 던전 컨트롤러 (Phase 1)
    
    // 이벤트
    public event Action OnPanelChanged;
    
    // 현재 활성 패널
    private GameObject currentActivePanel;
    
    /// <summary>
    /// 초기화
    /// </summary>
    public void Initialize()
    {
        
        // 패널 검증
        if (lobbyPanel == null) Debug.LogError("[LobbyPanelManager] lobbyPanel 누락!");
        if (stageSelectPanel == null) Debug.LogError("[LobbyPanelManager] stageSelectPanel 누락!");
        if (dungeonSelectPanel == null) Debug.LogError("[LobbyPanelManager] dungeonSelectPanel 누락!"); // 🏰 Phase 1
        if (inventoryPanel == null) Debug.LogError("[LobbyPanelManager] inventoryPanel 누락!");
        if (shopPanel == null) Debug.LogError("[LobbyPanelManager] shopPanel 누락!");
        if (characterInfoPanel == null) Debug.LogError("[LobbyPanelManager] characterInfoPanel 누락!");
        if (workshopPanel == null) Debug.LogError("[LobbyPanelManager] workshopPanel 누락!");
        if (skillBookPanel == null) Debug.LogError("[LobbyPanelManager] skillBookPanel 누락!");  // 🆕 Phase 3-Revision
        
        // 초기 활성 패널 설정 (로비)
        currentActivePanel = lobbyPanel;
        
    }
    
    /// <summary>
    /// 로비 패널 표시
    /// </summary>
    public void ShowLobbyPanel()
    {
        
        // Cue 이벤트 발행
        EmitPanelCloseCue();
        
        // 패널 전환
        BringPanelToFront(lobbyPanel);
        
    }
    
    /// <summary>
    /// 상점 패널 표시
    /// </summary>
    public void ShowShopPanel()
    {
        
        // Cue 이벤트 발행
        EmitShopOpenCue();
        
        // 패널 전환
        BringPanelToFront(shopPanel);
        
    }
    
    /// <summary>
    /// 인벤토리 패널 표시
    /// </summary>
    public void ShowInventoryPanel()
    {
        
        // Cue 이벤트 발행
        EmitInventoryOpenCue();
        
        // 패널 전환
        BringPanelToFront(inventoryPanel);
        
    }
    
    /// <summary>
    /// 캐릭터 정보 패널 표시
    /// </summary>
    public void ShowCharacterInfoPanel()
    {
        
        // Cue 이벤트 발행
        EmitCharacterOpenCue();
        
        // 패널 전환
        BringPanelToFront(characterInfoPanel);
        
    }
    
    /// <summary>
    /// 스테이지 선택 패널 표시
    /// </summary>
    public void ShowStageSelectPanel()
    {
        
        // Cue 이벤트 발행
        EmitStageOpenCue();
        
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
        
    }
    
    /// <summary>
    /// 🏰 던전 선택 패널 표시 (Phase 1)
    /// </summary>
    public void ShowDungeonSelectPanel()
    {
        
        // Cue 이벤트 발행
        EmitDungeonOpenCue();
        
        // 패널 전환
        BringPanelToFront(dungeonSelectPanel);
        
        // DungeonSelectPanelController로 위임
        if (dungeonSelectPanelController != null)
        {
            dungeonSelectPanelController.ShowPanel();
        }
        else
        {
            Debug.LogError("[LobbyPanelManager] dungeonSelectPanelController가 null입니다!");
        }
        
    }
    
    /// <summary>
    /// 🆕 공방(제작) 패널 표시
    /// </summary>
    public void ShowWorkshopPanel()
    {
        
        // Cue 이벤트 발행
        EmitWorkshopOpenCue();
        
        // 패널 전환
        BringPanelToFront(workshopPanel);
        
        // WorkshopUI 초기화
        var workshopUI = workshopPanel?.GetComponent<UI.Workshop.WorkshopUI>();
        if (workshopUI != null)
        {
            workshopUI.OnPanelOpened();
        }
        else
        {
            Debug.LogError("[LobbyPanelManager] WorkshopUI 컴포넌트를 찾을 수 없습니다!");
        }
        
    }
    
    /// <summary>
    /// 🆕 스킬북 패널 표시 (Phase 3-Revision)
    /// </summary>
    public void ShowSkillBookPanel()
    {
        
        // Cue 이벤트 발행
        EmitSkillBookOpenCue();
        
        // 패널 전환
        BringPanelToFront(skillBookPanel);
        
        // SkillBookPanelUI 초기화
        var skillBookUI = skillBookPanel?.GetComponent<SkillBookPanelUI>();
        if (skillBookUI != null)
        {
            skillBookUI.OnPanelOpened();
        }
        else
        {
            Debug.LogError("[LobbyPanelManager] SkillBookPanelUI 컴포넌트를 찾을 수 없습니다!");
        }
        
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
    
    private void EmitWorkshopOpenCue()
    {
        var context = new CueContext
        {
            position = Vector3.zero,
            actorType = ActorType.UI
        };
        CueEmitter.Emit("ui.workshop.open", "UI", context);
    }
    
    private void EmitSkillBookOpenCue()
    {
        var context = new CueContext
        {
            position = Vector3.zero,
            actorType = ActorType.UI
        };
        CueEmitter.Emit("ui.skillbook.open", "UI", context);
    }
    
    private void EmitDungeonOpenCue()
    {
        var context = new CueContext
        {
            position = Vector3.zero,
            actorType = ActorType.UI
        };
        CueEmitter.Emit("ui.dungeon.open", "UI", context);
    }
    
    private void EmitStageOpenCue()
    {
        var context = new CueContext
        {
            position = Vector3.zero,
            actorType = ActorType.UI
        };
        CueEmitter.Emit("ui.stage.open", "UI", context);
    }
    
    #endregion
}

