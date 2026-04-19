using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 스킬북 패널 메인 컨트롤러
/// Phase 3-Revision: WorkshopUI 구조 참고
/// 책임: 스킬 탭 / 룬 탭 전환 관리
/// </summary>
public class SkillBookPanelUI : MonoBehaviour
{
    /// <summary>
    /// 스킬북 탭 타입
    /// </summary>
    public enum SkillBookTabType
    {
        Skill,              // 스킬 탭
        SpiritStone,        // 정령 수호석 탭 (기존 Rune)
        SpiritBlessing      // 정령의 가호 탭 (Phase 2: Resistance System)
    }
    
    [Header("📑 탭 버튼")]
    [SerializeField] private Button skillTabButton;
    [SerializeField] private Button spiritStoneTabButton;       // 기존 runeTabButton
    [SerializeField] private Button spiritBlessingTabButton;    // 🆕 신규 추가
    
    [Header("📑 탭 텍스트")]
    [SerializeField] private TMP_Text skillTabText;
    [SerializeField] private TMP_Text spiritStoneTabText;       // 기존 runeTabText
    [SerializeField] private TMP_Text spiritBlessingTabText;    // 🆕 신규 추가
    
    [Header("📦 서브 패널")]
    [SerializeField] private GameObject skillSubPanel;
    [SerializeField] private GameObject spiritStoneSubPanel;    // 기존 runeSubPanel
    [SerializeField] private GameObject spiritBlessingSubPanel; // 🆕 신규 추가
    
    [Header("🔘 공통 버튼")]
    [SerializeField] private Button closeButton;
    
    [Header("🎮 탭 컨트롤러")]
    [SerializeField] private SkillTabController skillTabController;
    [SerializeField] private RunePanelUI runePanelUI;                                  // 정령 수호석 탭
    [SerializeField] private SpiritBlessingTabController spiritBlessingTabController;  // 정령의 가호 탭
    
    [Header("📊 디버그")]
    
    // 현재 활성 탭
    private SkillBookTabType currentTab = SkillBookTabType.Skill;
    
    // 이벤트
    public event Action<SkillBookTabType> OnTabChanged;
    
    void Awake()
    {
    }
    
    void Start()
    {
        SetupEventListeners();
        
        // 초기 탭 설정 (스킬)
        SwitchTab(SkillBookTabType.Skill);
        
    }
    
    /// <summary>
    /// 이벤트 리스너 연결
    /// </summary>
    private void SetupEventListeners()
    {
        // 탭 버튼 이벤트
        if (skillTabButton != null)
        {
            skillTabButton.onClick.AddListener(() => SwitchTab(SkillBookTabType.Skill));
        }
        else
        {
            Debug.LogError("🔴 [SkillBookPanelUI] skillTabButton이 null입니다!");
        }
        
        if (spiritStoneTabButton != null)
        {
            spiritStoneTabButton.onClick.AddListener(() => SwitchTab(SkillBookTabType.SpiritStone));
        }
        else
        {
            Debug.LogError("🔴 [SkillBookPanelUI] spiritStoneTabButton이 null입니다!");
        }
        
        if (spiritBlessingTabButton != null)
        {
            spiritBlessingTabButton.onClick.AddListener(() => SwitchTab(SkillBookTabType.SpiritBlessing));
        }
        else
        {
            Debug.LogError("🔴 [SkillBookPanelUI] spiritBlessingTabButton이 null입니다!");
        }
        
        // 닫기 버튼 이벤트
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
        else
        {
            Debug.LogError("🔴 [SkillBookPanelUI] closeButton이 null입니다!");
        }
    }
    
    /// <summary>
    /// 탭 전환
    /// </summary>
    public void SwitchTab(SkillBookTabType tab)
    {
        
        currentTab = tab;
        
        // 모든 서브 패널 비활성화
        DeactivateAllSubPanels();
        
        // 선택된 서브 패널만 활성화
        switch (tab)
        {
            case SkillBookTabType.Skill:
                if (skillSubPanel != null)
                {
                    skillSubPanel.SetActive(true);
                    
                    // SkillTabController 명시적 초기화
                    if (skillTabController != null)
                    {
                        skillTabController.OnTabActivated();
                    }
                    else
                    {
                        Debug.LogError("🔴 [SkillBookPanelUI] SkillTabController 참조가 null입니다!");
                    }
                    
                }
                break;
                
            case SkillBookTabType.SpiritStone:
                if (spiritStoneSubPanel != null)
                {
                    spiritStoneSubPanel.SetActive(true);
                    
                    // RunePanelUI 명시적 초기화
                    if (runePanelUI != null)
                    {
                        runePanelUI.OnTabActivated();
                    }
                    else
                    {
                        Debug.LogError("🔴 [SkillBookPanelUI] RunePanelUI 참조가 null입니다!");
                    }
                    
                }
                else
                {
                    Debug.LogWarning("⚠️ [SkillBookPanelUI] 정령 수호석 패널이 연결되지 않았습니다.");
                }
                break;
                
            case SkillBookTabType.SpiritBlessing:
                if (spiritBlessingSubPanel != null)
                {
                    spiritBlessingSubPanel.SetActive(true);
                    
                    // SpiritBlessingTabController 명시적 초기화
                    if (spiritBlessingTabController != null)
                    {
                        spiritBlessingTabController.OnTabActivated();
                    }
                    else
                    {
                        Debug.LogError("🔴 [SkillBookPanelUI] SpiritBlessingTabController 참조가 null입니다!");
                    }
                    
                }
                else
                {
                    Debug.LogWarning("⚠️ [SkillBookPanelUI] 정령의 가호 패널이 연결되지 않았습니다.");
                }
                break;
        }
        
        // 탭 버튼 상태 업데이트
        UpdateTabButtonStates();
        
        // 이벤트 발행
        OnTabChanged?.Invoke(tab);
    }
    
    /// <summary>
    /// 모든 서브 패널 비활성화
    /// </summary>
    private void DeactivateAllSubPanels()
    {
        if (skillSubPanel != null)
            skillSubPanel.SetActive(false);
        
        if (spiritStoneSubPanel != null)
            spiritStoneSubPanel.SetActive(false);
        
        if (spiritBlessingSubPanel != null)
            spiritBlessingSubPanel.SetActive(false);
    }
    
    /// <summary>
    /// 탭 버튼 상태 업데이트
    /// </summary>
    private void UpdateTabButtonStates()
    {
        // 스킬 탭
        if (skillTabText != null)
        {
            Color color = skillTabText.color;
            color.a = (currentTab == SkillBookTabType.Skill) ? 1.0f : 0.5f;
            skillTabText.color = color;
        }
        
        // 정령 수호석 탭
        if (spiritStoneTabText != null)
        {
            Color color = spiritStoneTabText.color;
            color.a = (currentTab == SkillBookTabType.SpiritStone) ? 1.0f : 0.5f;
            spiritStoneTabText.color = color;
        }
        
        // 정령의 가호 탭
        if (spiritBlessingTabText != null)
        {
            Color color = spiritBlessingTabText.color;
            color.a = (currentTab == SkillBookTabType.SpiritBlessing) ? 1.0f : 0.5f;
            spiritBlessingTabText.color = color;
        }
        
    }
    
    /// <summary>
    /// 닫기 버튼 클릭
    /// </summary>
    private void OnCloseButtonClicked()
    {
        
        // LobbyUIController를 통해 로비로 복귀
        var lobbyUIController = FindObjectOfType<LobbyUIController>();
        if (lobbyUIController != null)
        {
            lobbyUIController.OnBackToLobby();
        }
        else
        {
            Debug.LogError("🔴 [SkillBookPanelUI] LobbyUIController를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 스킬북 패널 열릴 때 호출 (외부에서)
    /// </summary>
    public void OnPanelOpened()
    {
        
        // 초기 탭으로 리셋
        SwitchTab(SkillBookTabType.Skill);
    }
    
    /// <summary>
    /// 스킬북 패널 닫힐 때 호출 (외부에서)
    /// </summary>
    public void OnPanelClosed()
    {
        
        // 필요한 정리 작업
        if (skillTabController != null)
        {
            skillTabController.OnTabDeactivated();
        }
        
        if (runePanelUI != null)
        {
            runePanelUI.OnTabDeactivated();
        }
        
        if (spiritBlessingTabController != null)
        {
            spiritBlessingTabController.OnTabDeactivated();
        }
    }
    
    /// <summary>
    /// 현재 활성 탭 반환
    /// </summary>
    public SkillBookTabType GetCurrentTab()
    {
        return currentTab;
    }
}
