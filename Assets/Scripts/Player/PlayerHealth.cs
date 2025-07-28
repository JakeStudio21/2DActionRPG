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

    // ❌ 제거: private Slider healthSlider;
    private int currentHealth;
    private bool canTakeDamage = true;
    private Knockback knockback;
    private Flash flash;
    private ResultPopupController resultPopup;
    private PlayerAnimationController playerAnimationController;
    
    // ✅ PlayerUIController 참조 추가
    private PlayerUIController playerUIController;

    // ❌ 제거: const string HEALTH_SLIDER_TEXT = "Health Slider";
    const string TOWN_TEXT = "Scene1";
    readonly int DEATH_HASH = Animator.StringToHash("Death");
    
    // ✅ 외부 접근용 프로퍼티 추가
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    protected override void Awake() {
        base.Awake();
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();
        
        // ✅ Knockback 컴포넌트 확인
        if (knockback == null)
        {
            Debug.LogWarning("⚠️ [PlayerHealth] Knockback 컴포넌트를 찾을 수 없습니다. Player GameObject에 Knockback 컴포넌트를 추가해주세요.");
        }
    }

    private void Start()
    {
        isDead = false;
        resultPopup = FindObjectOfType<ResultPopupController>();
        
        playerAnimationController = GetComponent<PlayerAnimationController>();
        if (playerAnimationController == null)
            playerAnimationController = GetComponentInChildren<PlayerAnimationController>();
            
        // ✅ PlayerUIController 참조 획득
        playerUIController = FindObjectOfType<PlayerUIController>();
            
        Debug.Log("🔧 [PlayerHealth] 기본 초기화 완료 (체력은 BaseClassBehaviour에서 설정 예정)");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ❌ 제거: healthSlider = null;
        // ❌ 제거: UpdateHealthSlider();
        resultPopup = FindObjectOfType<ResultPopupController>();
        
        // ✅ PlayerUIController 다시 찾기
        if (playerUIController == null)
        {
            playerUIController = FindObjectOfType<PlayerUIController>();
        }
        
        // ✅ UI 업데이트
        UpdateUI();

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
            UpdateUI(); // ✅ 변경: UpdateHealthSlider() → UpdateUI()
        }
    }

    public void TakeDamage(int damageAmount, Transform hitTransform) {
        if (!canTakeDamage) { return; }

        if (playerAnimationController != null)
        {
            playerAnimationController.OnHitStart();
        }

        // ✅ knockback null 체크 추가
        if (knockback != null)
        {
            knockback.GetKnockedBack(hitTransform, knockback.DefaultKnockBackThrust);
        }
        else
        {
            Debug.LogWarning("⚠️ [PlayerHealth] Knockback 컴포넌트가 없어서 넉백을 적용할 수 없습니다.");
        }
        
        if (flash != null)
        {
            StartCoroutine(flash.FlashRoutine());
        }
        
        canTakeDamage = false;
        currentHealth -= damageAmount;
        
        UpdateUI();
        
        StartCoroutine(DamageRecoveryRoutine());
        StartCoroutine(QuickHitRecoveryRoutine());
        CheckIfPlayerDeath();
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

    // ✅ 새로운 UI 업데이트 메서드
    private void UpdateUI()
    {
        if (playerUIController != null)
        {
            playerUIController.OnHealthChanged(currentHealth, maxHealth);
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
        UpdateUI(); // ✅ 변경: UpdateHealthSlider() → UpdateUI()
        
        Debug.Log($"🔧 [PlayerHealth] 체력 초기화 완료: {currentHealth}/{maxHealth}");
    }
}
