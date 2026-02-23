using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CueSystem;

public class PlayerHealth : MonoBehaviour
{
    public bool isDead { get; private set; }

    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float damageRecoveryTime = 1f;
    
    // 🆕 디버그 로그 제어 변수 추가
    [Header("🔧 디버그")]
    [SerializeField] private bool showDebugLogs = true;

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
    const string TOWN_TEXT = "Stage_001";
    readonly int DEATH_HASH = Animator.StringToHash("Death");
    
    // ✅ 외부 접근용 프로퍼티 추가
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    
    // ⚙️ Phase 4-C: 회복 차단 시스템
    [Header("⚙️ Phase 4-C: 회복 차단 시스템")]
    [SerializeField] private float healingBlockDuration = 5f; // 회복 차단 지속 시간 (초)
    private float healingBlockMultiplier = 0f; // 0~1, 1이면 100% 차단
    private Coroutine healingBlockRoutine = null;
    
    [Header("🛡️ Phase 4-C: 상태이상 면역 시스템")]
    [SerializeField] private float immunityDuration = 0.5f; // 면역 정보 유지 시간 (초)
    private string currentResistedEffects = ""; // 현재 저항 중인 상태이상 목록
    private float immunityExpireTime = 0f; // 면역 만료 시간

    private void Awake() {
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();
        
        // ✅ Knockback 컴포넌트 확인
        if (knockback == null)
        {
            Debug.LogWarning("⚠️ [PlayerHealth] Knockback 컴포넌트를 찾을 수 없습니다. Player GameObject에 Knockback 컴포넌트를 추가해주세요.");
        }
        
        // 씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
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
    
    private void OnDestroy()
    {
        // 씬 로드 이벤트 구독 해제 (메모리 누수 방지)
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
            currentHealth += 1;  // 항상 1씩만 회복
            UpdateUI();
        }

        // 🆕 Cue 이벤트 발행
        var context = new CueContext
        {
            position = transform.position,
            actorType = ActorType.Player,
            magnitude = 1.0f
        };
        
        CueEmitter.Emit("heal.player", "Player", context);
    }

    /// <summary>
    /// 지정된 양만큼 체력 회복
    /// </summary>
    public void HealPlayerAmount(int healAmount) {
        if (currentHealth < maxHealth) {
            // ⚙️ Phase 4-C: 회복 차단 적용
            float blockedPercent = healingBlockMultiplier;
            int blockedAmount = Mathf.RoundToInt(healAmount * blockedPercent);
            int effectiveHeal = healAmount - blockedAmount;
            
            int actualHeal = Mathf.Min(effectiveHeal, maxHealth - currentHealth);
            currentHealth += actualHeal;
            UpdateUI();
            
            // 🆕 Cue 이벤트 발행
            var context = new CueContext
            {
                position = transform.position,
                actorType = ActorType.Player,
                magnitude = actualHeal / 3f, // 회복량에 비례한 효과 크기
                damage = actualHeal // 회복량을 damage 필드에 저장
            };
            
            CueEmitter.Emit("heal.player", "Player", context);
            
            if (showDebugLogs)
            {
                if (blockedPercent > 0)
                {
                    Debug.Log($"❤️ [PlayerHealth] 체력 회복: +{actualHeal} (회복 차단 {blockedPercent * 100:F0}%로 {blockedAmount} 차단됨) ({currentHealth}/{maxHealth})");
                }
                else
                {
                    Debug.Log($"❤️ [PlayerHealth] 체력 회복: +{actualHeal} ({currentHealth}/{maxHealth})");
                }
            }
        }
    }

    /// <summary>
    /// ⚔️ 기본 데미지 받기 (기존 시스템 호환용)
    /// </summary>
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
        
        // ⭐ 데미지 넘버 표시 (Phase 1 + 앵커 시스템)
        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.ShowDamage(
                transform.position, 
                damageAmount, 
                isPlayer: true, 
                targetTransform: transform  // ⭐ 앵커 검색용
            );
        }
        
        UpdateUI();
        
        StartCoroutine(DamageRecoveryRoutine());
        StartCoroutine(QuickHitRecoveryRoutine());
        CheckIfPlayerDeath();

        // 🆕 Cue 이벤트 발행
        var context = new CueContext
        {
            position = transform.position,
            rotation = transform.rotation,
            normal = (transform.position - hitTransform.position).normalized,
            actorType = ActorType.Player,
            magnitude = damageAmount / 10f,
            damage = damageAmount
        };
        
        // 크리티컬 여부는 추후 확장 가능
        string eventKey = "hit.player.normal";
        CueEmitter.Emit(eventKey, "Player", context);
    }
    
    /// <summary>
    /// ⚔️ Phase 4-C: DamageResult 기반 데미지 받기 (완전한 전투 공식 연동)
    /// </summary>
    public void TakeDamage(CombatFormula.DamageResult result, Transform hitTransform) {
        if (!canTakeDamage) { return; }

        // 1️⃣ 🛡️ Phase 4-C: 면역 체크 (상태이상 차단)
        if (result.hasImmunity && !string.IsNullOrEmpty(result.resistedEffects))
        {
            // 면역 정보 저장 (StatusEffectManager에서 참조)
            currentResistedEffects = result.resistedEffects;
            immunityExpireTime = Time.time + immunityDuration;
            
            if (showDebugLogs)
                Debug.Log($"🛡️ [PlayerHealth] 플레이어 면역 발동! 저항한 효과: {result.resistedEffects}");
            
            // ⚙️ 향후: UI에 "면역!" 텍스트 표시 이벤트 발행
        }

        // 2️⃣ 💉 Phase 4-C: 회복 차단 디버프 적용
        if (result.healingBlockPercent > 0)
        {
            ApplyHealingBlock(result.healingBlockPercent);
        }

        // 3️⃣ 애니메이션 및 효과
        if (playerAnimationController != null)
        {
            playerAnimationController.OnHitStart();
        }

        // 4️⃣ 넉백
        if (knockback != null)
        {
            knockback.GetKnockedBack(hitTransform, knockback.DefaultKnockBackThrust);
        }
        else
        {
            Debug.LogWarning("⚠️ [PlayerHealth] Knockback 컴포넌트가 없어서 넉백을 적용할 수 없습니다.");
        }
        
        // 5️⃣ Flash 효과
        if (flash != null)
        {
            StartCoroutine(flash.FlashRoutine());
        }
        
        // 6️⃣ 실제 HP 차감
        canTakeDamage = false;
        currentHealth -= result.finalDamage;
        
        // 7️⃣ 데미지 넘버 표시 (Phase 4-C: DamageResult 통합 - 크리티컬, 면역 연출 포함)
        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.ShowDamage(
                transform.position, 
                result,  // ⚙️ DamageResult 통째로 전달
                isPlayer: true, 
                targetTransform: transform  // ⭐ 앵커 검색용
            );
        }
        
        // 8️⃣ UI 업데이트
        UpdateUI();
        
        // 9️⃣ 회복 루틴 시작
        StartCoroutine(DamageRecoveryRoutine());
        StartCoroutine(QuickHitRecoveryRoutine());
        
        // 🔟 사망 체크
        CheckIfPlayerDeath();

        // 1️⃣1️⃣ Cue 이벤트 발행
        var context = new CueContext
        {
            position = transform.position,
            rotation = transform.rotation,
            normal = (transform.position - hitTransform.position).normalized,
            actorType = ActorType.Player,
            magnitude = result.finalDamage / 10f,
            damage = result.finalDamage,
            isCritical = result.isCritical
        };
        
        // 크리티컬 여부 반영
        string eventKey = result.isCritical ? "hit.player.critical" : "hit.player.normal";
        CueEmitter.Emit(eventKey, "Player", context);
        
        if (showDebugLogs)
        {
            Debug.Log($"💥 [PlayerHealth] {result.finalDamage} 데미지 받음 (크리티컬: {result.isCritical}, 백어택: {result.isBackAttack}) ({currentHealth}/{maxHealth})");
        }
    }

    private void CheckIfPlayerDeath() {
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            
            // 🧹 Phase 4-C: 사망 시 모든 상태이상 제거
            if (StatusEffectManager.Instance != null)
            {
                StatusEffectManager.Instance.ClearEffectsOnTarget(gameObject);
            }
            
            var activeWeapon = FindObjectOfType<ActiveWeapon>();
            if (activeWeapon != null)
            {
                Destroy(activeWeapon.gameObject);
            }
            currentHealth = 0;
            GetComponent<Animator>().SetTrigger(DEATH_HASH);
            
            // ✅ 수정: FSMStageController 호출 제거 (SRP 준수)
            // StageManager가 isDead 상태를 감지하여 패배 처리하도록 위임
            Debug.Log("💀 [PlayerHealth] 플레이어 사망 - StageManager가 패배 조건을 감지할 것입니다.");
            
            // 기존 팝업 표시 로직 유지 (UI 책임)
            StartCoroutine(DeathLoadSceneRoutine());

            // 🆕 Cue 이벤트 발행
            var context = new CueContext
            {
                position = transform.position,
                actorType = ActorType.Player,
                magnitude = 2.0f
            };
            
            CueEmitter.Emit("death.player", "Player", context);
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
    
    // 🔧 장비 시스템용 체력 설정 메서드 추가
    public void SetMaxHealth(int newMaxHealth)
    {
        // 현재 체력 비율 계산
        float healthRatio = (float)currentHealth / maxHealth;
        
        // 새로운 최대 체력 설정
        maxHealth = newMaxHealth;
        
        // 체력 비율 유지하여 현재 체력 조정
        currentHealth = Mathf.RoundToInt(maxHealth * healthRatio);
        currentHealth = Mathf.Clamp(currentHealth, 1, maxHealth); // 최소 1 보장
        
        UpdateUI();
        
        Debug.Log($"🔧 [PlayerHealth] 최대 체력 설정: {currentHealth}/{maxHealth} (비율 {healthRatio:P0} 유지)");
    }
    
    /// <summary>
    /// 🎯 PlayerRuntimeStats와 동기화
    /// </summary>
    public void SyncWithRuntimeStats()
    {
        var playerRuntimeStats = FindObjectOfType<PlayerRuntimeStats>();
        if (playerRuntimeStats != null)
        {
            int newMaxHealth = Mathf.RoundToInt(playerRuntimeStats.FinalMaxHealth);
            
            // 🆕 현재 체력이 1 이하면 (게임 시작/사망 후 부활) 체력을 가득 채움
            if (currentHealth <= 1)
            {
                maxHealth = newMaxHealth;
                currentHealth = maxHealth;
                if (showDebugLogs)
                    Debug.Log($"🆕 [PlayerHealth] 체력 가득 채움: {currentHealth}/{maxHealth}");
            }
            else
            {
                // 런타임 중: 기존 비율 유지
                SetMaxHealth(newMaxHealth);
            }
            
            UpdateUI();
            if (showDebugLogs)
                Debug.Log($"🎯 [PlayerHealth] PlayerRuntimeStats와 동기화: 최대체력 {maxHealth}");
        }
    }
    
    /// <summary>
    /// 현재 체력 비율 반환 (0.0 ~ 1.0)
    /// ⚙️ Phase 4: ConditionalModifier 시스템에서 CombatContext 생성 시 사용
    /// </summary>
    public float GetCurrentHpPercent()
    {
        if (maxHealth <= 0)
            return 0f;
        
        return Mathf.Clamp01((float)currentHealth / maxHealth);
    }
    
    #region ⚙️ Phase 4-C: 회복 차단 시스템
    
    /// <summary>
    /// 회복 차단 디버프 적용 (5초 지속)
    /// </summary>
    private void ApplyHealingBlock(float blockPercent)
    {
        // 기존 회복 차단 코루틴 중단
        if (healingBlockRoutine != null)
        {
            StopCoroutine(healingBlockRoutine);
        }
        
        // 새로운 회복 차단 적용
        healingBlockMultiplier = blockPercent;
        healingBlockRoutine = StartCoroutine(HealingBlockRoutine());
        
        if (showDebugLogs)
        {
            Debug.Log($"🚫 [PlayerHealth] 회복 차단 디버프 적용: {blockPercent * 100:F0}% ({healingBlockDuration}초 지속)");
        }
    }
    
    /// <summary>
    /// 회복 차단 디버프 지속 시간 관리
    /// </summary>
    private IEnumerator HealingBlockRoutine()
    {
        // healingBlockDuration만큼 대기
        yield return new WaitForSeconds(healingBlockDuration);
        
        // 회복 차단 해제
        healingBlockMultiplier = 0f;
        healingBlockRoutine = null;
        
        if (showDebugLogs)
        {
            Debug.Log($"✅ [PlayerHealth] 회복 차단 디버프 해제");
        }
    }
    
    #endregion

    #region 🔧 DEBUG: 테스트용 메서드 (에디터 전용)
    
#if UNITY_EDITOR
    /// <summary>
    /// 디버그 전용: 체력을 특정 값으로 설정합니다.
    /// </summary>
    public void DEBUG_SetHealth(int newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        UpdateUI();
        
        if (showDebugLogs)
            Debug.Log($"🔧 [DEBUG] 플레이어 체력 설정: {currentHealth}/{maxHealth}");
    }
    
    /// <summary>
    /// 디버그 전용: 체력을 특정 비율(%)로 설정합니다.
    /// </summary>
    public void DEBUG_SetHealthPercent(float percent)
    {
        int targetHealth = Mathf.RoundToInt(maxHealth * Mathf.Clamp01(percent));
        DEBUG_SetHealth(targetHealth);
    }
    
    /// <summary>
    /// 디버그 전용: 체력을 완전히 회복합니다.
    /// </summary>
    public void DEBUG_HealFull()
    {
        currentHealth = maxHealth;
        UpdateUI();
        
        if (showDebugLogs)
            Debug.Log($"🔧 [DEBUG] 플레이어 완전 회복: {currentHealth}/{maxHealth}");
    }
#endif
    
    #endregion
    
    #region 🛡️ Phase 4-C: 상태이상 면역 시스템
    
    /// <summary>
    /// 🛡️ 특정 상태이상에 면역인지 확인
    /// StatusEffectManager에서 호출됨
    /// </summary>
    public bool IsImmuneToEffect(EStatusEffectType effectType)
    {
        // 면역 만료 시간 체크
        if (Time.time > immunityExpireTime)
        {
            currentResistedEffects = "";
            return false;
        }
        
        // resistedEffects에 해당 상태이상이 포함되어 있는지 확인
        string effectName = effectType.ToString();
        return currentResistedEffects.Contains(effectName);
    }
    
    #endregion
}
