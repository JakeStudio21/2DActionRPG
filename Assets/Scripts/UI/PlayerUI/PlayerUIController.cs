using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 플레이어 관련 모든 UI를 통합 관리하는 컨트롤러 
/// 개별 UI 컨트롤러 대신 이 컨트롤러가 모든 UI를 직접 관리
/// </summary>
public class PlayerUIController : MonoBehaviour
{
    [Header("UI 요소 직접 참조")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button skill1Button;
    [SerializeField] private Button skill2Button;
    [SerializeField] private Image skill1CooldownImage;
    [SerializeField] private Image skill2CooldownImage;
    // ✅ Health UI, Gold UI 추가
    [SerializeField] private HealthUI healthUI;
    [SerializeField] private GoldUI goldUI;
    
    [Header("UI 패널 참조 (선택사항)")]
    [SerializeField] private GameObject levelPanel;
    [SerializeField] private GameObject skillPanel;
    [SerializeField] private GameObject attackPanel;
    // ✅ Health Panel, Gold Panel 추가
    [SerializeField] private GameObject healthPanel;
    [SerializeField] private GameObject goldPanel;
    
    [Header("스킬 설정")]
    [SerializeField] private float skill1CooldownTime = 2f;
    [SerializeField] private float skill2CooldownTime = 3f;
    
    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 내부 상태
    private bool isInitialized = false;
    private bool isLevelUISubscribed = false;
    private SkillController skillController;
    private float lastUpdateTime;
    private const float UPDATE_INTERVAL = 0.1f;

    void Start()
    {
        InitializePlayerUI();
    }

    void Update()
    {
        if (!isInitialized) return;
        
        // UI 업데이트를 0.1초마다만 실행하여 성능 최적화
        if (Time.time - lastUpdateTime >= UPDATE_INTERVAL)
        {
            UpdateLevelUI();
            UpdateSkillCooldownUI();
            UpdateHealthUI();
            UpdateGoldUI(); // ✅ 추가
            lastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// 플레이어 UI 초기화
    /// </summary>
    private void InitializePlayerUI()
    {
        if (showDebugLogs)
            Debug.Log("[PlayerUIController] 플레이어 UI 초기화 시작");
        
        // UI 요소 자동 탐색 (Inspector에서 할당되지 않은 경우)
        if (levelText == null || attackButton == null || skill1Button == null || skill2Button == null || healthUI == null || goldUI == null)
        {
            AutoFindUIElements();
        }

        // 컴포넌트 초기화
        InitializeComponents();
        
        isInitialized = true;
        
        if (showDebugLogs)
            Debug.Log("[PlayerUIController] 플레이어 UI 초기화 완료");
    }

    /// <summary>
    /// UI 요소들 자동 탐색
    /// </summary>
    private void AutoFindUIElements()
    {
        if (showDebugLogs)
            Debug.Log("[PlayerUIController] UI 요소 자동 탐색 시작");

        // Level Text 자동 탐색
        if (levelText == null)
        {
            GameObject levelObject = GameObject.Find("LevelText");
            if (levelObject != null)
            {
                levelText = levelObject.GetComponent<TextMeshProUGUI>();
                if (showDebugLogs)
                    Debug.Log("[PlayerUIController] Level Text 자동 탐색 완료");
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning("[PlayerUIController] Level Text GameObject를 찾을 수 없습니다.");
            }
        }

        // ✅ Health UI 자동 탐색 (Health Slider 방식)
        if (healthUI == null)
        {
            // "Health Slider" 오브젝트에서 HealthUI 컴포넌트 찾기
            GameObject healthSliderObject = GameObject.Find("Health Slider");
            if (healthSliderObject != null)
            {
                healthUI = healthSliderObject.GetComponent<HealthUI>();
                if (healthUI == null)
                {
                    // HealthUI 컴포넌트가 없으면 추가
                    healthUI = healthSliderObject.AddComponent<HealthUI>();
                }
                
                if (showDebugLogs)
                    Debug.Log("[PlayerUIController] Health UI 자동 탐색 완료 (Slider 방식)");
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning("[PlayerUIController] Health Slider GameObject를 찾을 수 없습니다.");
            }
        }

        // ✅ Gold UI 자동 탐색
        if (goldUI == null)
        {
            GameObject goldAmountObject = GameObject.Find("Gold Amount Text");
            if (goldAmountObject != null)
            {
                goldUI = goldAmountObject.GetComponent<GoldUI>();
                if (goldUI == null)
                {
                    goldUI = goldAmountObject.AddComponent<GoldUI>();
                }
                
                if (showDebugLogs)
                    Debug.Log("[PlayerUIController] Gold UI 자동 탐색 완료");
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning("[PlayerUIController] Gold Amount Text GameObject를 찾을 수 없습니다.");
            }
        }

        // AttackButton 찾기
        if (attackButton == null)
        {
            GameObject attackButtonObj = GameObject.Find("AttackButton");
            if (attackButtonObj != null)
            {
                attackButton = attackButtonObj.GetComponent<Button>();
                if (showDebugLogs)
                    Debug.Log("[PlayerUIController] AttackButton 자동 탐색 완료");
            }
            else
            {
                Debug.LogWarning("[PlayerUIController] AttackButton GameObject를 찾을 수 없습니다.");
            }
        }
        
        // SkillButton 찾기
        if (skill1Button == null)
        {
            GameObject skillButtonObj = GameObject.Find("SkillButton");
            if (skillButtonObj != null)
            {
                skill1Button = skillButtonObj.GetComponent<Button>();
                if (showDebugLogs)
                    Debug.Log("[PlayerUIController] SkillButton 자동 탐색 완료");
            }
            else
            {
                Debug.LogWarning("[PlayerUIController] SkillButton GameObject를 찾을 수 없습니다.");
            }
        }
        
        // SkillButton2 찾기
        if (skill2Button == null)
        {
            GameObject skill2ButtonObj = GameObject.Find("SkillButton2");
            if (skill2ButtonObj != null)
            {
                skill2Button = skill2ButtonObj.GetComponent<Button>();
                if (showDebugLogs)
                    Debug.Log("[PlayerUIController] SkillButton2 자동 탐색 완료");
            }
            else
            {
                Debug.LogWarning("[PlayerUIController] SkillButton2 GameObject를 찾을 수 없습니다.");
            }
        }
        
        // 쿨다운 이미지들 찾기
        if (skill1CooldownImage == null && skill1Button != null)
        {
            // SkillButton의 하위에서 쿨다운 이미지 찾기
            skill1CooldownImage = skill1Button.transform.Find("CooldownImage")?.GetComponent<Image>();
            if (skill1CooldownImage == null)
            {
                // 비활성화된 SkillUIController에서 참조 가져오기
                var skillUIComp = skill1Button.GetComponent<SkillUIController>();
                if (skillUIComp != null)
                {
                    skill1CooldownImage = skillUIComp.cooldownImage;
                }
            }
        }
        
        if (skill2CooldownImage == null && skill2Button != null)
        {
            // SkillButton2의 하위에서 쿨다운 이미지 찾기
            skill2CooldownImage = skill2Button.transform.Find("CooldownImage")?.GetComponent<Image>();
            if (skill2CooldownImage == null)
            {
                // 비활성화된 Skill2UIController에서 참조 가져오기
                var skill2UIComp = skill2Button.GetComponent<Skill2UIController>();
                if (skill2UIComp != null)
                {
                    skill2CooldownImage = skill2UIComp.cooldownImage;
                }
            }
        }
    }

    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // 버튼 이벤트 연결
        SetupButtonEvents();
        
        // SkillController 찾기
        InitializeSkillController();
        
        // HealthUI 초기화
        if (healthUI != null)
        {
            healthUI.InitializeHealthSlider(); // HealthUI 내부에서 자동 탐색 및 초기화
            if (showDebugLogs)
                Debug.Log("[PlayerUIController] HealthUI 컴포넌트 초기화 완료");
        }

        // GoldUI 초기화
        if (goldUI != null)
        {
            goldUI.InitializeGoldText(); // GoldUI 내부에서 자동 탐색 및 초기화
            if (showDebugLogs)
                Debug.Log("[PlayerUIController] GoldUI 컴포넌트 초기화 완료");
        }
    }

    /// <summary>
    /// 버튼 이벤트 설정
    /// </summary>
    private void SetupButtonEvents()
    {
        // AttackButton은 기존 AttackButtonController를 그대로 사용
        // (AttackButtonController.OnAttackButtonPressed가 이미 Button OnClick에 연결되어 있음)
        
        if (skill1Button != null)
        {
            skill1Button.onClick.RemoveAllListeners();
            skill1Button.onClick.AddListener(OnSkill1ButtonPressed);
            if (showDebugLogs)
                Debug.Log("[PlayerUIController] Skill1Button 이벤트 연결 완료");
        }
        
        if (skill2Button != null)
        {
            skill2Button.onClick.RemoveAllListeners();
            skill2Button.onClick.AddListener(OnSkill2ButtonPressed);
            if (showDebugLogs)
                Debug.Log("[PlayerUIController] Skill2Button 이벤트 연결 완료");
        }
    }

    /// <summary>
    /// SkillController 초기화
    /// </summary>
    private void InitializeSkillController()
    {
        skillController = FindObjectOfType<SkillController>();
        if (skillController != null)
        {
            skillController.cooldownTime = skill1CooldownTime;
            skillController.skill2CooldownTime = skill2CooldownTime;
            if (showDebugLogs)
                Debug.Log("[PlayerUIController] SkillController 초기화 완료");
        }
        else
        {
            Debug.LogWarning("[PlayerUIController] SkillController를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 레벨 UI 업데이트
    /// </summary>
    private void UpdateLevelUI()
    {
        if (!isLevelUISubscribed && PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnLevelChanged += UpdateLevelText;
            isLevelUISubscribed = true;
            UpdateLevelText(PlayerDataManager.Instance.CurrentLevel);
            if (showDebugLogs)
                Debug.Log("[PlayerUIController] LevelUI 이벤트 구독 완료");
        }
    }

    /// <summary>
    /// 스킬 쿨다운 UI 업데이트
    /// </summary>
    private void UpdateSkillCooldownUI()
    {
        if (skillController == null) return;
        
        // 스킬1 쿨다운
        if (skill1CooldownImage != null)
        {
            float remain = skillController.GetCooldownRemaining();
            float newFillAmount = remain / skillController.CooldownTime;
            if (Mathf.Abs(skill1CooldownImage.fillAmount - newFillAmount) > 0.01f)
            {
                skill1CooldownImage.fillAmount = newFillAmount;
            }
        }
        
        // 스킬2 쿨다운
        if (skill2CooldownImage != null)
        {
            float remain = skillController.GetSkill2CooldownRemaining();
            float newFillAmount = remain / skillController.Skill2CooldownTime;
            if (Mathf.Abs(skill2CooldownImage.fillAmount - newFillAmount) > 0.01f)
            {
                skill2CooldownImage.fillAmount = newFillAmount;
            }
        }
    }

    /// <summary>
    /// 레벨 텍스트 업데이트
    /// </summary>
    private void UpdateLevelText(int newLevel)
    {
        if (levelText != null)
        {
            levelText.text = $"Lv. {newLevel}";
            if (showDebugLogs)
                Debug.Log($"[PlayerUIController] 레벨 UI 업데이트: Lv. {newLevel}");
        }
    }

    /// <summary>
    /// ✅ Health UI 업데이트
    /// </summary>
    private void UpdateHealthUI()
    {
        if (healthUI == null || !healthUI.IsReady) return;
        
        if (PlayerHealth.Instance != null)
        {
            healthUI.UpdateHealthUI(PlayerHealth.Instance.CurrentHealth, PlayerHealth.Instance.MaxHealth);
        }
    }

    /// <summary>
    /// ✅ 외부에서 Health UI 업데이트 호출용 (PlayerHealth에서 사용)
    /// </summary>
    public void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (healthUI != null && healthUI.IsReady)
        {
            healthUI.UpdateHealthUI(currentHealth, maxHealth);
            
            if (showDebugLogs)
                Debug.Log($"[PlayerUIController] Health UI 외부 업데이트: {currentHealth}/{maxHealth}");
        }
    }

    /// <summary>
    /// ✅ Gold UI 업데이트
    /// </summary>
    private void UpdateGoldUI()
    {
        if (goldUI == null || !goldUI.IsReady) return;
        
        if (PlayerDataManager.Instance != null)
        {
            goldUI.UpdateGoldUI(PlayerDataManager.Instance.CurrentGold);
        }
    }

    /// <summary>
    /// ✅ 외부에서 Gold UI 업데이트 호출용 (PlayerDataManager에서 사용)
    /// </summary>
    public void OnGoldChanged(int currentGold)
    {
        if (goldUI != null && goldUI.IsReady)
        {
            goldUI.UpdateGoldUI(currentGold);
            
            if (showDebugLogs)
                Debug.Log($"[PlayerUIController] Gold UI 외부 업데이트: {currentGold}");
        }
    }

    /// <summary>
    /// ✅ Health UI, Gold UI 참조 반환 (읽기 전용)
    /// </summary>
    public HealthUI HealthUI => healthUI;
    public GoldUI GoldUI => goldUI;

    #region 버튼 이벤트 핸들러들

    private void OnSkill1ButtonPressed()
    {
        if (skillController != null)
        {
            bool success = skillController.SkillSet.ExecuteSkill(0);
            if (showDebugLogs)
                Debug.Log($"[PlayerUIController] 스킬1 버튼 실행: {success}");
        }
        else
        {
            Debug.LogWarning("[PlayerUIController] SkillController가 없어서 스킬1 실행 불가");
        }
    }

    private void OnSkill2ButtonPressed()
    {
        if (skillController != null)
        {
            bool success = skillController.SkillSet.ExecuteSkill(1);
            if (showDebugLogs)
                Debug.Log($"[PlayerUIController] 스킬2 버튼 실행: {success}");
        }
        else
        {
            Debug.LogWarning("[PlayerUIController] SkillController가 없어서 스킬2 실행 불가");
        }
    }

    #endregion

    #region 공용 메서드들

    /// <summary>
    /// 스킬1 쿨다운 시간 설정
    /// </summary>
    public void SetSkill1CooldownTime(float cooldownTime)
    {
        skill1CooldownTime = cooldownTime;
        if (skillController != null)
        {
            skillController.cooldownTime = cooldownTime;
        }
    }

    /// <summary>
    /// 스킬2 쿨다운 시간 설정
    /// </summary>
    public void SetSkill2CooldownTime(float cooldownTime)
    {
        skill2CooldownTime = cooldownTime;
        if (skillController != null)
        {
            skillController.skill2CooldownTime = cooldownTime;
        }
    }

    #endregion

    private void OnDisable()
    {
        if (isLevelUISubscribed && PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnLevelChanged -= UpdateLevelText;
            isLevelUISubscribed = false;
        }
    }
}
