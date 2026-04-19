using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // ⭐ 이 줄 추가

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
    [SerializeField] private Image skill1Button_Radial;
    [SerializeField] private Image skill2Button_Radial;
    [SerializeField] private Image attackButton_Radial;  // 기본공격 Radial (선택적)
    [SerializeField] private Image dashButton_Radial;    // 대시 Radial (선택적)
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
    
    [Header("디버그")]
    
    // 내부 상태
    private bool isInitialized = false;
    private bool isLevelUISubscribed = false;
    private SkillController skillController;
    private PlayerAnimationController animController;
    private PlayerController playerController;
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
        
        // UI 요소 자동 탐색 (Inspector에서 할당되지 않은 경우)
        if (levelText == null || attackButton == null || skill1Button == null || skill2Button == null || 
            skill1Button_Radial == null || skill2Button_Radial == null || healthUI == null || goldUI == null)
        {
            AutoFindUIElements();
        }

        // 컴포넌트 초기화
        InitializeComponents();
        
        isInitialized = true;
        
            Dbg.Log("[PlayerUIController] 플레이어 UI 초기화 완료");
    }

    /// <summary>
    /// UI 요소들 자동 탐색
    /// </summary>
    private void AutoFindUIElements()
    {

        // Level Text 자동 탐색
        if (levelText == null)
        {
            GameObject levelObject = GameObject.Find("LevelText");
            if (levelObject != null)
            {
                levelText = levelObject.GetComponent<TextMeshProUGUI>();
            }
            else
            {
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
                
            }
            else
            {
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
                
            }
            else
            {
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
            }
            else
            {
                Debug.LogWarning("[PlayerUIController] AttackButton GameObject를 찾을 수 없습니다.");
            }
        }
        
        // Skill1Button 찾기
        if (skill1Button == null)
        {
            GameObject skillButtonObj = GameObject.Find("Skill1Button");
            if (skillButtonObj != null)
            {
                skill1Button = skillButtonObj.GetComponent<Button>();
            }
            else
            {
                Debug.LogWarning("[PlayerUIController] Skill1Button GameObject를 찾을 수 없습니다.");
            }
        }

        // Skill2Button 찾기
        if (skill2Button == null)
        {
            GameObject skill2ButtonObj = GameObject.Find("Skill2Button");
            if (skill2ButtonObj != null)
            {
                skill2Button = skill2ButtonObj.GetComponent<Button>();
            }
            else
            {
                Debug.LogWarning("[PlayerUIController] Skill2Button GameObject를 찾을 수 없습니다.");
            }
        }

        // 쿨다운 Radial 찾기
        if (skill1Button_Radial == null && skill1Button != null)
        {
            skill1Button_Radial = skill1Button.transform.Find("Skill1Button_Radial")?.GetComponent<Image>();
        }

        if (skill2Button_Radial == null && skill2Button != null)
        {
            skill2Button_Radial = skill2Button.transform.Find("Skill2Button_Radial")?.GetComponent<Image>();
        }
        
        // 기본공격 Radial 자동 탐색 (선택적)
        if (attackButton_Radial == null && attackButton != null)
        {
            attackButton_Radial = attackButton.transform.Find("AttackButton_Radial")?.GetComponent<Image>();
        }
        
        // 대시 Radial 자동 탐색 (선택적)
        if (dashButton_Radial == null)
        {
            GameObject dashButtonObj = GameObject.Find("DashButton");
            if (dashButtonObj != null)
                dashButton_Radial = dashButtonObj.transform.Find("DashButton_Radial")?.GetComponent<Image>();
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
        }

        // GoldUI 초기화
        if (goldUI != null)
        {
            goldUI.InitializeGoldText(); // GoldUI 내부에서 자동 탐색 및 초기화
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
        }
        
        if (skill2Button != null)
        {
            skill2Button.onClick.RemoveAllListeners();
            skill2Button.onClick.AddListener(OnSkill2ButtonPressed);
        }
    }

    /// <summary>
    /// SkillController 초기화 (Coroutine 기반 재시도)
    /// </summary>
    private void InitializeSkillController()
    {
        StartCoroutine(InitializeSkillControllerCoroutine());
    }
    
    /// <summary>
    /// SkillController 초기화 Coroutine (재시도 로직)
    /// </summary>
    private IEnumerator InitializeSkillControllerCoroutine()
    {
        int maxRetries = 10; // 최대 10회 시도
        float retryInterval = 0.1f; // 0.1초마다 재시도
        
        for (int i = 0; i < maxRetries; i++)
        {
            skillController = FindObjectOfType<SkillController>();
            
            if (skillController != null)
            {
                break;
            }
            
            
            yield return new WaitForSeconds(retryInterval);
        }
        
        if (skillController != null)
        {
        }
        else
        {
            Debug.LogError("[PlayerUIController] SkillController를 찾을 수 없습니다! 모든 재시도 실패");
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
        }
    }

    /// <summary>
    /// 스킬/기본공격/대시 쿨다운 Radial UI 업데이트
    /// PlayerAnimationController / PlayerController 에서 실제 쿨다운 값을 읽음
    /// </summary>
    private void UpdateSkillCooldownUI()
    {
        // PlayerAnimationController 캐싱 (처음 한 번)
        if (animController == null)
            animController = FindObjectOfType<PlayerAnimationController>();
        
        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();
        
        // ── 스킬1 Radial ──────────────────────────────────────
        if (skill1Button_Radial != null && animController != null)
        {
            animController.GetSkill1CooldownInfo(out float remaining, out float total);
            skill1Button_Radial.fillAmount = (total > 0f) ? (remaining / total) : 0f;
        }
        
        // ── 스킬2 Radial ──────────────────────────────────────
        if (skill2Button_Radial != null && animController != null)
        {
            animController.GetSkill2CooldownInfo(out float remaining, out float total);
            skill2Button_Radial.fillAmount = (total > 0f) ? (remaining / total) : 0f;
        }
        
        // ── 기본공격 Radial (선택적) ──────────────────────────
        if (attackButton_Radial != null && animController != null)
        {
            animController.GetAttackCooldownInfo(out float remaining, out float total);
            attackButton_Radial.fillAmount = (total > 0f) ? (remaining / total) : 0f;
        }
        
        // ── 대시 Radial (선택적) ──────────────────────────────
        if (dashButton_Radial != null && playerController != null)
        {
            playerController.GetDashCooldownInfo(out float remaining, out float total);
            dashButton_Radial.fillAmount = (total > 0f) ? (remaining / total) : 0f;
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
        }
    }

    /// <summary>
    /// ✅ Health UI 업데이트
    /// </summary>
    private void UpdateHealthUI()
    {
        if (healthUI == null || !healthUI.IsReady) return;
        
        var playerHealth = FindObjectOfType<PlayerHealth>();  // 변경: PlayerHealth.Instance → FindObjectOfType<PlayerHealth>()
        if (playerHealth != null)
        {
            healthUI.UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);  // 변경: PlayerHealth.Instance → playerHealth
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
        
        // ⭐ 키보드 S키와 동일한 경로 사용
        var playerAnimationController = FindObjectOfType<PlayerAnimationController>();
        if (playerAnimationController != null)
        {
            bool success = playerAnimationController.TriggerSkill1();
        }
        else
        {
            Debug.LogWarning("[PlayerUIController] PlayerAnimationController를 찾을 수 없어서 스킬1 실행 불가");
        }
    }

    private void OnSkill2ButtonPressed()
    {
        
        // ⭐ 키보드 D키와 동일한 경로 사용
        var playerAnimationController = FindObjectOfType<PlayerAnimationController>();
        if (playerAnimationController != null)
        {
            bool success = playerAnimationController.TriggerSkill2();
        }
        else
        {
            Debug.LogWarning("[PlayerUIController] PlayerAnimationController를 찾을 수 없어서 스킬2 실행 불가");
        }
    }

    #endregion

    #region 공용 메서드들 (기존 호환성 메서드들 제거)

    // ⭐ 제거: SetSkill1CooldownTime, SetSkill2CooldownTime 메서드들
    // 새로운 구조에서는 각 스킬이 자체 SkillData에서 쿨다운 관리

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