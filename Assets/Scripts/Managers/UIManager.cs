using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 전체를 관리하는 매니저
/// HUD, 버튼, 스킬 UI 등을 통합 관리
/// </summary>
public class UIManager : Singleton<UIManager>
{
    [Header("HUD UI")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider staminaBar;
    [SerializeField] private Text levelText;
    [SerializeField] private Text goldText;
    
    [Header("스킬 UI")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button skill1Button;
    [SerializeField] private Button skill2Button;
    [SerializeField] private Image[] skillCooldownImages;
    
    [Header("메뉴 UI")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject settingsPanel;
    
    [Header("팝업 UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject loadingPanel;
    
    protected override void Awake()
    {
        base.Awake();
        InitializeUI();
    }
    
    private void Start()
    {
        // 게임 상태에 따른 UI 초기 설정
        UpdateUIForGameState(GameManager.Instance.currentGameState);
    }
    
    /// <summary>
    /// UI 초기화
    /// </summary>
    private void InitializeUI()
    {
        // UI 요소들이 할당되지 않았으면 자동으로 찾기
        if (hudPanel == null) hudPanel = GameObject.Find("HUDPanel");
        if (pauseMenu == null) pauseMenu = GameObject.Find("PauseMenu");
        if (inventoryPanel == null) inventoryPanel = GameObject.Find("InventoryPanel");
        
        // 버튼 이벤트 연결
        if (attackButton != null) attackButton.onClick.AddListener(OnAttackButtonClick);
        if (skill1Button != null) skill1Button.onClick.AddListener(OnSkill1ButtonClick);
        if (skill2Button != null) skill2Button.onClick.AddListener(OnSkill2ButtonClick);
    }
    
    /// <summary>
    /// 게임 상태에 따른 UI 업데이트
    /// </summary>
    public void UpdateUIForGameState(GameManager.GameState gameState)
    {
        switch (gameState)
        {
            case GameManager.GameState.Lobby:
                ShowLobbyUI();
                break;
            case GameManager.GameState.InGame:
                ShowGameUI();
                break;
            case GameManager.GameState.Paused:
                ShowPauseUI();
                break;
            case GameManager.GameState.GameOver:
                ShowGameOverUI();
                break;
        }
    }
    
    /// <summary>
    /// 로비 UI 표시
    /// </summary>
    private void ShowLobbyUI()
    {
        SetUIVisibility(hudPanel, false);
        SetUIVisibility(pauseMenu, false);
        SetUIVisibility(inventoryPanel, false);
    }
    
    /// <summary>
    /// 게임 UI 표시
    /// </summary>
    private void ShowGameUI()
    {
        SetUIVisibility(hudPanel, true);
        SetUIVisibility(pauseMenu, false);
        SetUIVisibility(inventoryPanel, false);
    }
    
    /// <summary>
    /// 일시정지 UI 표시
    /// </summary>
    private void ShowPauseUI()
    {
        SetUIVisibility(pauseMenu, true);
    }
    
    /// <summary>
    /// 게임오버 UI 표시
    /// </summary>
    private void ShowGameOverUI()
    {
        SetUIVisibility(gameOverPanel, true);
    }
    
    /// <summary>
    /// UI 가시성 설정
    /// </summary>
    private void SetUIVisibility(GameObject uiElement, bool visible)
    {
        if (uiElement != null)
        {
            uiElement.SetActive(visible);
        }
    }
    
    /// <summary>
    /// 체력바 업데이트
    /// </summary>
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
    }
    
    /// <summary>
    /// 스태미나바 업데이트
    /// </summary>
    public void UpdateStaminaBar(float currentStamina, float maxStamina)
    {
        if (staminaBar != null)
        {
            staminaBar.value = currentStamina / maxStamina;
        }
    }
    
    /// <summary>
    /// 레벨 텍스트 업데이트
    /// </summary>
    public void UpdateLevelText(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Level {level}";
        }
    }
    
    /// <summary>
    /// 골드 텍스트 업데이트
    /// </summary>
    public void UpdateGoldText(int gold)
    {
        if (goldText != null)
        {
            goldText.text = $"Gold: {gold}";
        }
    }
    
    /// <summary>
    /// 스킬 쿨다운 업데이트
    /// </summary>
    public void UpdateSkillCooldown(int skillIndex, float cooldownProgress)
    {
        if (skillIndex < skillCooldownImages.Length && skillCooldownImages[skillIndex] != null)
        {
            skillCooldownImages[skillIndex].fillAmount = cooldownProgress;
        }
    }
    
    // 버튼 이벤트 핸들러들
    private void OnAttackButtonClick()
    {
        // 공격 버튼 클릭 처리
        Debug.Log("[UIManager] 공격 버튼 클릭");
    }
    
    private void OnSkill1ButtonClick()
    {
        // 스킬1 버튼 클릭 처리
        Debug.Log("[UIManager] 스킬1 버튼 클릭");
    }
    
    private void OnSkill2ButtonClick()
    {
        // 스킬2 버튼 클릭 처리
        Debug.Log("[UIManager] 스킬2 버튼 클릭");
    }
    
    /// <summary>
    /// 인벤토리 토글
    /// </summary>
    public void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        }
    }
    
    /// <summary>
    /// 설정 패널 토글
    /// </summary>
    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }
    
    /// <summary>
    /// 로딩 화면 표시/숨김
    /// </summary>
    public void ShowLoadingScreen(bool show)
    {
        SetUIVisibility(loadingPanel, show);
    }
}
