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
    [SerializeField] private Image skill1Button_Radial;  // skill1CooldownImage → 변경
    [SerializeField] private Image skill2Button_Radial;  // skill2CooldownImage → 변경
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
        if (levelText == null || attackButton == null || skill1Button == null || skill2Button == null || 
            skill1Button_Radial == null || skill2Button_Radial == null || healthUI == null || goldUI == null)
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
        
        // Skill1Button 찾기  // SkillButton → 변경
        if (skill1Button == null)
        {
            GameObject skillButtonObj = GameObject.Find("Skill1Button");  // "SkillButton" → 변경
            if (skillButtonObj != null)
            {
                skill1Button = skillButtonObj.GetComponent<Button>();
                if (showDebugLogs)
                    Debug.Log("[PlayerUIController] Skill1Button 자동 탐색 완료");  // 로그 메시지 변경
            }
            else
            {
                Debug.LogWarning("[PlayerUIController] Skill1Button GameObject를 찾을 수 없습니다.");  // 로그 메시지 변경
            }
        }
        
        // Skill2Button 찾기  // SkillButton2 → 변경
        if (skill2Button == null)
        {
            GameObject skill2ButtonObj = GameObject.Find("Skill2Button");  // "SkillButton2" → 변경
            if (skill2ButtonObj != null)
            {
                skill2Button = skill2ButtonObj.GetComponent<Button>();
                if (showDebugLogs)
                    Debug.Log("[PlayerUIController] Skill2Button 자동 탐색 완료");  // 로그 메시지 변경
            }
            else
            {
                Debug.LogWarning("[PlayerUIController] Skill2Button GameObject를 찾을 수 없습니다.");  // 로그 메시지 변경
            }
        }
        
        // 쿨다운 이미지들 찾기 - 심플하게 변경
        if (skill1Button_Radial == null && skill1Button != null)
        {
            skill1Button_Radial = skill1Button.transform.Find("Skill1Button_Radial")?.GetComponent<Image>();
            if (showDebugLogs)
                Debug.Log($"[PlayerUIController] Skill1Button_Radial 탐색: {(skill1Button_Radial != null ? "성공" : "실패")}");
        }
        
        if (skill2Button_Radial == null && skill2Button != null)
        {
            skill2Button_Radial = skill2Button.transform.Find("Skill2Button_Radial")?.GetComponent<Image>();
            if (showDebugLogs)
                Debug.Log($"[PlayerUIController] Skill2Button_Radial 탐색: {(skill2Button_Radial != null ? "성공" : "실패")}");
        }
        //  skill1Button이 없으면 실행할 필요 없음

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
                if (showDebugLogs)
                    Debug.Log($"[PlayerUIController] SkillController 찾음! (시도 {i + 1}회)");
                break;
            }
            
            if (showDebugLogs && i < 3) // 처음 3회만 로그 출력
                Debug.Log($"[PlayerUIController] SkillController 검색 중... (시도 {i + 1}/{maxRetries})");
            
            yield return new WaitForSeconds(retryInterval);
        }
        
        if (skillController != null)
        {
            if (showDebugLogs)
                Debug.Log("[PlayerUIController] SkillController 초기화 완료 - ScriptableObject 기반 쿨다운 사용");
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
            if (showDebugLogs)
                Debug.Log("[PlayerUIController] LevelUI 이벤트 구독 완료");
        }
    }

    /// <summary>
    /// 스킬 쿨다운 UI 업데이트
    /// </summary>
    private void UpdateSkillCooldownUI()
    {
        // 🔍 null 체크 디버그 추가
        if (showDebugLogs && Time.frameCount % 120 == 0) // 2초마다 한 번씩 출력
        {
            Debug.Log($"🔍 [UpdateSkillCooldownUI] 상태 체크:" +
                     $"\n - skillController: {(skillController != null ? "OK" : "NULL")}" +
                     $"\n - skill1Button_Radial: {(skill1Button_Radial != null ? "OK" : "NULL")}" +
                     $"\n - skill2Button_Radial: {(skill2Button_Radial != null ? "OK" : "NULL")}" +
                     $"\n - 프레임: {Time.frameCount}", this);
                     
            if (skillController != null && skillController.SkillSet != null)
            {
                var skill1 = skillController.SkillSet.GetSkill(0);
                var skill2 = skillController.SkillSet.GetSkill(1);
                Debug.Log($"🔍 [SkillSet] 상태 체크:" +
                         $"\n - Skill1: {(skill1 != null ? skill1.GetType().Name : "NULL")}" +
                         $"\n - Skill2: {(skill2 != null ? skill2.GetType().Name : "NULL")}", this);
            }
        }
        
        if (skillController == null) return;
        
        // 스킬1 쿨다운 UI
        if (skill1Button_Radial != null)  // skill1CooldownImage → 변경
        {
            var skill1 = skillController.SkillSet?.GetSkill(0);
            if (skill1 != null)
            {
                float remainingTime = skill1.GetCooldownRemaining();
                float totalTime = skill1.Cooldown;
                
                // fillAmount = remainingTime / totalTime
                skill1Button_Radial.fillAmount = remainingTime / totalTime;  // skill1CooldownImage → 변경
            }
        }
        
        // 스킬2 쿨다운 UI  
        if (skill2Button_Radial != null)  // skill2CooldownImage → 변경
        {
            var skill2 = skillController.SkillSet?.GetSkill(1);
            if (skill2 != null)
            {
                float remainingTime = skill2.GetCooldownRemaining();
                float totalTime = skill2.Cooldown;
                
                // fillAmount = remainingTime / totalTime
                skill2Button_Radial.fillAmount = remainingTime / totalTime;  // skill2CooldownImage → 변경
            }
        }
        
        // 🔍 fillAmount 디버그 로그 추가
        if (showDebugLogs && Time.frameCount % 60 == 0) // 1초마다 한 번씩만 출력 (60fps 기준)
        {
            if (skill1Button_Radial != null)
            {
                var skill1 = skillController?.SkillSet?.GetSkill(0);
                float remainingTime = skill1?.GetCooldownRemaining() ?? 0f;
                float totalTime = skill1?.Cooldown ?? 1f;
                float calculatedFillAmount = remainingTime / totalTime;
                
                Debug.Log($"🔍 [Skill1 fillAmount] " +
                         $"실제값: {skill1Button_Radial.fillAmount:F3} | " +
                         $"계산값: {calculatedFillAmount:F3} | " +
                         $"쿨다운: {remainingTime:F1}/{totalTime:F1} | " +
                         $"프레임: {Time.frameCount}", this);
            }
            
            if (skill2Button_Radial != null)
            {
                var skill2 = skillController?.SkillSet?.GetSkill(1);
                float remainingTime = skill2?.GetCooldownRemaining() ?? 0f;
                float totalTime = skill2?.Cooldown ?? 1f;
                float calculatedFillAmount = remainingTime / totalTime;
                
                Debug.Log($"🔍 [Skill2 fillAmount] " +
                         $"실제값: {skill2Button_Radial.fillAmount:F3} | " +
                         $"계산값: {calculatedFillAmount:F3} | " +
                         $"쿨다운: {remainingTime:F1}/{totalTime:F1} | " +
                         $"프레임: {Time.frameCount}", this);
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
        if (showDebugLogs)
            Debug.Log("🔥 [PlayerUIController] 스킬1 버튼 클릭!");
        
        // ⭐ 키보드 S키와 동일한 경로 사용
        var playerAnimationController = FindObjectOfType<PlayerAnimationController>();
        if (playerAnimationController != null)
        {
            bool success = playerAnimationController.TriggerSkill1();
            if (showDebugLogs)
                Debug.Log($"[PlayerUIController] 스킬1 버튼 실행: {success}");
        }
        else
        {
            Debug.LogWarning("[PlayerUIController] PlayerAnimationController를 찾을 수 없어서 스킬1 실행 불가");
        }
    }

    private void OnSkill2ButtonPressed()
    {
        if (showDebugLogs)
            Debug.Log("🔥 [PlayerUIController] 스킬2 버튼 클릭!");
        
        // ⭐ 키보드 D키와 동일한 경로 사용
        var playerAnimationController = FindObjectOfType<PlayerAnimationController>();
        if (playerAnimationController != null)
        {
            bool success = playerAnimationController.TriggerSkill2();
            if (showDebugLogs)
                Debug.Log($"[PlayerUIController] 스킬2 버튼 실행: {success}");
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