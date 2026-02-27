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
        Skill,      // 스킬 탭
        Rune        // 룬 탭 (Phase 4 이후)
    }
    
    [Header("📑 탭 버튼")]
    [SerializeField] private Button skillTabButton;
    [SerializeField] private Button runeTabButton;
    
    [Header("📑 탭 텍스트")]
    [SerializeField] private TMP_Text skillTabText;
    [SerializeField] private TMP_Text runeTabText;
    
    [Header("📦 서브 패널")]
    [SerializeField] private GameObject skillSubPanel;
    [SerializeField] private GameObject runeSubPanel;
    
    [Header("🔘 공통 버튼")]
    [SerializeField] private Button closeButton;
    
    [Header("🎮 탭 컨트롤러")]
    [SerializeField] private SkillTabController skillTabController;
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 현재 활성 탭
    private SkillBookTabType currentTab = SkillBookTabType.Skill;
    
    // 이벤트
    public event Action<SkillBookTabType> OnTabChanged;
    
    void Awake()
    {
        if (showDebugLogs)
            Debug.Log("📚 [SkillBookPanelUI] Awake() - 스킬북 패널 초기화");
    }
    
    void Start()
    {
        SetupEventListeners();
        
        // 초기 탭 설정 (스킬)
        SwitchTab(SkillBookTabType.Skill);
        
        if (showDebugLogs)
            Debug.Log("✅ [SkillBookPanelUI] Start() - 스킬북 패널 준비 완료");
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
            if (showDebugLogs)
                Debug.Log("✅ [SkillBookPanelUI] 스킬 탭 버튼 이벤트 연결");
        }
        else
        {
            Debug.LogError("🔴 [SkillBookPanelUI] skillTabButton이 null입니다!");
        }
        
        if (runeTabButton != null)
        {
            runeTabButton.onClick.AddListener(() => SwitchTab(SkillBookTabType.Rune));
            if (showDebugLogs)
                Debug.Log("✅ [SkillBookPanelUI] 룬 탭 버튼 이벤트 연결");
        }
        else
        {
            Debug.LogError("🔴 [SkillBookPanelUI] runeTabButton이 null입니다!");
        }
        
        // 닫기 버튼 이벤트
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
            if (showDebugLogs)
                Debug.Log("✅ [SkillBookPanelUI] 닫기 버튼 이벤트 연결");
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
        if (showDebugLogs)
            Debug.Log($"🔄 [SkillBookPanelUI] 탭 전환: {currentTab} → {tab}");
        
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
                        if (showDebugLogs)
                            Debug.Log("📚 [SkillBookPanelUI] 스킬 탭 컨트롤러 초기화 완료");
                    }
                    else
                    {
                        Debug.LogError("🔴 [SkillBookPanelUI] SkillTabController 참조가 null입니다!");
                    }
                    
                    if (showDebugLogs)
                        Debug.Log("📚 [SkillBookPanelUI] 스킬 패널 활성화");
                }
                break;
                
            case SkillBookTabType.Rune:
                if (runeSubPanel != null)
                {
                    runeSubPanel.SetActive(true);
                    
                    // TODO: Phase 4 이후 RuneTabController 초기화
                    
                    if (showDebugLogs)
                        Debug.Log("🔮 [SkillBookPanelUI] 룬 패널 활성화 (미구현)");
                }
                else
                {
                    Debug.LogWarning("⚠️ [SkillBookPanelUI] 룬 패널은 아직 구현되지 않았습니다.");
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
        
        if (runeSubPanel != null)
            runeSubPanel.SetActive(false);
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
        
        // 룬 탭
        if (runeTabText != null)
        {
            Color color = runeTabText.color;
            color.a = (currentTab == SkillBookTabType.Rune) ? 1.0f : 0.5f;
            runeTabText.color = color;
        }
        
        if (showDebugLogs)
            Debug.Log($"✅ [SkillBookPanelUI] 탭 버튼 상태 업데이트: {currentTab}");
    }
    
    /// <summary>
    /// 닫기 버튼 클릭
    /// </summary>
    private void OnCloseButtonClicked()
    {
        if (showDebugLogs)
            Debug.Log("🚪 [SkillBookPanelUI] 닫기 버튼 클릭");
        
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
        if (showDebugLogs)
            Debug.Log("📚 [SkillBookPanelUI] 스킬북 패널 열림");
        
        // 초기 탭으로 리셋
        SwitchTab(SkillBookTabType.Skill);
    }
    
    /// <summary>
    /// 스킬북 패널 닫힐 때 호출 (외부에서)
    /// </summary>
    public void OnPanelClosed()
    {
        if (showDebugLogs)
            Debug.Log("📚 [SkillBookPanelUI] 스킬북 패널 닫힘");
        
        // 필요한 정리 작업
        if (skillTabController != null)
        {
            skillTabController.OnTabDeactivated();
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
