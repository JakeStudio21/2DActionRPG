using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : Singleton<PlayerHealth>
{
    public bool isDead { get; private set; }

    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float damageRecoveryTime = 1f;

    private Slider healthSlider;
    private int currentHealth;
    private bool canTakeDamage = true;
    private Knockback knockback;
    private Flash flash;
    private ResultPopupController resultPopup;  // 팝업 컨트롤러 참조
    private PlayerAnimationController playerAnimationController; // ⭐ 추가된 변수 선언

    const string HEALTH_SLIDER_TEXT = "Health Slider";
    const string TOWN_TEXT = "Scene1";
    readonly int DEATH_HASH = Animator.StringToHash("Death");

    protected override void Awake() {
        base.Awake();
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();
        // [백업] DontDestroyOnLoad 및 fixedJoystick 관련 코드는 제거
        // if (fixedJoystick == null)
        // {
        //     fixedJoystick = FindObjectOfType<FixedJoystick>();
        // }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // ✅ PlayerHealth 기본 초기화 (체력 제외)
        isDead = false;
        // ❌ currentHealth = maxHealth; // 이 부분만 BaseClassBehaviour에서 처리
        resultPopup = FindObjectOfType<ResultPopupController>();
        
        // PlayerAnimationController 참조 획득
        playerAnimationController = GetComponent<PlayerAnimationController>();
        if (playerAnimationController == null)
            playerAnimationController = GetComponentInChildren<PlayerAnimationController>();
            
        Debug.Log("🔧 [PlayerHealth] 기본 초기화 완료 (체력은 BaseClassBehaviour에서 설정 예정)");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        healthSlider = null; // 씬이 로드되면 슬라이더 참조를 리셋
        UpdateHealthSlider();
        resultPopup = FindObjectOfType<ResultPopupController>();

        // 씬이 로드될 때마다 카메라가 플레이어를 따라가도록 설정합니다.
        if (CameraController.Instance != null)
        {
            CameraController.Instance.SetPlayerCameraFollow();
        }
    }

    private void OnCollisionStay2D(Collision2D other) {
        // 충돌한 오브젝트에서 EnemyDamage 컴포넌트를 가져옵니다.
        EnemyDamage enemyDamage = other.gameObject.GetComponent<EnemyDamage>();

        // EnemyDamage 컴포넌트가 존재한다면, 그 컴포넌트의 공격력으로 데미지를 입힙니다.
        if (enemyDamage != null) {
            TakeDamage(enemyDamage.damageAmount, other.transform);
            return;
        }

        // ⭐ 제거: GrapeLandSplatter는 이제 VFX만 담당 (데미지 없음)
        // GrapeProjectile이 직접 데미지를 처리하므로 여기서는 제거
    }

    public void HealPlayer() {
        if (currentHealth < maxHealth) {
            currentHealth += 1;
            UpdateHealthSlider();
        }
    }

    public void TakeDamage(int damageAmount, Transform hitTransform) {
        if (!canTakeDamage) { return; }

        // ⭐ [Phase B] Warrior 패시브 효과 연동 (기존)
        bool isBlocked = false;
        bool isDodged = false; // 🆕 Assasin 회피용
        int finalDamage = damageAmount;
        
        // Warrior 컴포넌트 확인 (기존)
        var warrior = GetComponent<Warrior>();
        if (warrior != null && warrior.IsActiveClass) {
            // 기존 Warrior 블록 로직...
            if (warrior.TryBlock())
            {
                isBlocked = true;
                // 블록 성공 시 데미지 감소 (기본 50%)
                finalDamage = Mathf.RoundToInt(damageAmount * 0.5f);
                Debug.Log($"🛡️ [PlayerHealth] Warrior 블록 성공! 데미지: {damageAmount} → {finalDamage}");
            }
        }

        // 🆕 Assasin 컴포넌트 확인
        var assasin = GetComponent<Assasin>();
        if (assasin != null && assasin.IsActiveClass) {
            // 1. 회피 판정 시도
            if (assasin.TryDodge()) {
                isDodged = true;
                finalDamage = 0; // 완전 회피
                Debug.Log($"💨 [PlayerHealth] Assasin 회피 성공! 데미지 무효화");
            }
        }

        ScreenShakeManager.Instance.ShakeScreen();
        
        // 🆕 회피 성공 시 넉백도 무효화
        float baseKnockback = knockback.DefaultKnockBackThrust; // 🔧 Knockback에서 기본값 가져오기
        float knockbackAmount = baseKnockback;
        if (isDodged) {
            knockbackAmount = 0f; // 회피 시 넉백 없음
        } else if (warrior != null && isBlocked) {
            // 기존 Warrior 넉백 저항...
            knockbackAmount = warrior.ApplyKnockbackResistance(baseKnockback); // 🔧 수정
            Debug.Log($"🏋️ [PlayerHealth] Warrior 넉백 저항 적용! {baseKnockback} → {knockbackAmount}"); // 🔧 수정
        }
        
        knockback.GetKnockedBack(hitTransform, knockbackAmount);
        StartCoroutine(flash.FlashRoutine());
        canTakeDamage = false;
        
        // ⭐ 최종 데미지 적용 (블록 효과 반영)
        currentHealth -= finalDamage;
        StartCoroutine(DamageRecoveryRoutine());
        
        // ⭐ 새로운 해결책: isHit 플래그 빠른 해제 (근접 전투 최적화)
        StartCoroutine(QuickHitRecoveryRoutine());
        
        UpdateHealthSlider();
        
        Debug.Log($"플레이어 피격! 실제 데미지: {finalDamage}/{damageAmount}, 현재 체력: {currentHealth}/{maxHealth}");

        CheckIfPlayerDeath();
        
        // ⭐ 피격 애니메이션 트리거 다시 활성화 (안전장치 추가)
        if (playerAnimationController != null)
        {
            bool hitResult = playerAnimationController.TriggerHit();
            Debug.Log($"🔴 [PlayerHealth] 피격 애니메이션 트리거 결과: {hitResult}");
        }
        else
        {
            Debug.LogWarning("🟡 [PlayerHealth] PlayerAnimationController를 찾을 수 없습니다!");
        }
    }

    private void CheckIfPlayerDeath() {
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            var activeWeapon = FindObjectOfType<ActiveWeapon>();
            if (activeWeapon != null)
            {
                Destroy(activeWeapon.gameObject);
            }
            currentHealth = 0;
            GetComponent<Animator>().SetTrigger(DEATH_HASH);
            
            // ⭐ 수정: FSMStageController를 통한 Defeat 처리
            if (FSMStageController.Instance != null)
            {
                FSMStageController.Instance.TriggerDefeat();
                StartCoroutine(DeathLoadSceneRoutine()); // 기존 팝업 표시 로직 유지
            }
            else
            {
                // 백업: 기존 방식
                Debug.LogWarning("[PlayerHealth] FSMStageController를 찾을 수 없습니다. 기존 방식 사용.");
                StartCoroutine(DeathLoadSceneRoutine());
            }
        }
    }

    private IEnumerator DeathLoadSceneRoutine() {
        yield return new WaitForSecondsRealtime(2f);

        // 팝업 표시 (Defeat)
        if (resultPopup != null) {
            resultPopup.Show(false);
        }
        
        // 기존 코드 주석 처리 (팝업에서 처리할 것이므로)
        // Destroy(gameObject);
        // SceneManager.LoadScene(TOWN_TEXT);
    }

    private IEnumerator DamageRecoveryRoutine() {
        yield return new WaitForSecondsRealtime(damageRecoveryTime);
        canTakeDamage = true;
        
        // ⭐ 핵심 해결책: PlayerAnimationController의 isHit 플래그도 함께 해제
        if (playerAnimationController != null)
        {
            // OnHitEnd() 메서드 호출로 isHit 플래그 해제
            playerAnimationController.OnHitEnd();
        }
        else
        {
            Debug.LogWarning("🟡 [PlayerHealth] PlayerAnimationController가 null이어서 isHit 플래그 해제 실패!");
        }
    }
    
    /// <summary>
    /// ⭐ 새로운 해결책: isHit 플래그만 빠르게 해제 (근접 전투 최적화)
    /// </summary>
    private IEnumerator QuickHitRecoveryRoutine() {
        // 0.3초 후 빠르게 isHit 플래그만 해제
        yield return new WaitForSecondsRealtime(0.3f);
        
        if (playerAnimationController != null)
        {
            playerAnimationController.OnHitEnd();
        }
    }

    private void UpdateHealthSlider() {
        if (healthSlider == null) {
            GameObject healthSliderObject = GameObject.Find(HEALTH_SLIDER_TEXT);
            if (healthSliderObject != null) {
                healthSlider = healthSliderObject.GetComponent<Slider>();
            }
        }

        // healthSlider가 여전히 null이면 안전하게 처리
        if (healthSlider != null) {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        else {
            // Debug.LogWarning($"⚠️ [PlayerHealth] Health Slider를 찾을 수 없습니다: {HEALTH_SLIDER_TEXT}");
        }
    }

    private void Update()
    {
        // 디버그용 SkillUIController 코드가 잘못 들어온 부분이므로 삭제
    }

    // ✅ 추가: 외부에서 설정 가능한 메서드
    public void InitializeHealth(int newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = maxHealth;
        UpdateHealthSlider();
        
        Debug.Log($"🔧 [PlayerHealth] 체력 초기화 완료: {currentHealth}/{maxHealth}");
    }
}
