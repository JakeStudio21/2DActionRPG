using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : Singleton<PlayerHealth>
{
    public bool isDead { get; private set; }

    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float knockBackThrustAmount = 10f;
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
        isDead = false;
        currentHealth = maxHealth;
        UpdateHealthSlider();
        resultPopup = FindObjectOfType<ResultPopupController>();
        
        // PlayerAnimationController 참조 획득
        playerAnimationController = GetComponent<PlayerAnimationController>();
        if (playerAnimationController == null)
            playerAnimationController = GetComponentInChildren<PlayerAnimationController>();
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

        ScreenShakeManager.Instance.ShakeScreen();
        knockback.GetKnockedBack(hitTransform, knockBackThrustAmount);
        StartCoroutine(flash.FlashRoutine());
        canTakeDamage = false;
        currentHealth -= damageAmount;
        StartCoroutine(DamageRecoveryRoutine());
        
        // ⭐ 새로운 해결책: isHit 플래그 빠른 해제 (근접 전투 최적화)
        StartCoroutine(QuickHitRecoveryRoutine());
        
        UpdateHealthSlider();
        
        Debug.Log($"플레이어 피격! 현재 체력: {currentHealth}/{maxHealth}");

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
            healthSlider = GameObject.Find(HEALTH_SLIDER_TEXT).GetComponent<Slider>();
        }

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }

    private void Update()
    {
        // 디버그용 SkillUIController 코드가 잘못 들어온 부분이므로 삭제
    }
}
