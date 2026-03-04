using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StageSystem;  // 🆕 추가 - WaveController 네임스페이스
using ItemSystem; // PickupDataCache, BaseItemData 사용을 위해

/// <summary>
/// 몬스터 체력 관리 클래스
/// ⭐ [Phase A] 완전한 데이터 기반 시스템으로 전환
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("❌ Fallback 제거됨 - 데이터 필수!")]
    [Tooltip("EnemyData + MonsterGrowthProfile 필수 할당!")]
    
    // ⭐ 이벤트 시스템 (WaveController 연동용)
    public System.Action OnEnemyDeath;
    
    // ⭐ 튜토리얼용 피격 이벤트
    public System.Action OnTakeDamageEvent;

    // ❌ 삭제: fallback 필드들 제거
    // [SerializeField] private int fallbackMaxHealth = 100;
    // [SerializeField] private float fallbackKnockBackThrust = 15f;
    // [SerializeField] private int fallbackExperienceGiven = 10;

    private int currentHealth;
    private Knockback knockback;
    private Flash flash;

    // ⭐ 데이터 기반 시스템
    private BaseEnemy baseEnemy;
    
    // ⭐ 사망 관련 상태 플래그들을 명확히 분리
    public bool isDead = false;                    // 데미지 받기 차단용 (즉시 설정)
    private bool isDeathAnimationPlaying = false;  // 사망 애니메이션 재생 중
    private bool deathEventTriggered = false;      // Animation Event 트리거 플래그
    
    public FinalBossSpawner bossSpawner; // Inspector에서 설정 (특수 케이스)
    
    [Header("🎯 엘리트/보스 체력바 시스템")]
    [Tooltip("엘리트/보스 몬스터 머리 위 체력바 Prefab (World Space)")]
    [SerializeField] private GameObject eliteHealthBarPrefab;
    [Tooltip("체력바 Y축 오프셋 (몬스터 머리 위 높이)")]
    [SerializeField] private float healthBarOffsetY = 2.5f;
    
    [Header("🎯 일반 몬스터 체력바 시스템 (자동 숨김)")]
    [Tooltip("Basic 타입 몬스터 체력바 Prefab (자동 숨김 시간은 Prefab에서 설정)")]
    [SerializeField] private GameObject basicHealthBarPrefab;
    
    // 체력바 인스턴스
    private EliteHealthBarUI activeHealthBar; // 엘리트/보스용
    private BasicEnemyHealthBarUI basicHealthBar; // Basic용
    
    [Header("🛡️ Phase 4-C: 상태이상 면역 시스템")]
    [SerializeField] private float immunityDuration = 0.5f; // 면역 정보 유지 시간 (초)
    private string currentResistedEffects = ""; // 현재 저항 중인 상태이상 목록
    private float immunityExpireTime = 0f; // 면역 만료 시간

    private void Awake()
    {
        flash = GetComponent<Flash>();
        knockback = GetComponent<Knockback>();
        
        // ⭐ 새 시스템: BaseEnemy 참조 획득
        baseEnemy = GetComponent<BaseEnemy>();
    }

    private void Start()
    {
        // ⭐ 새 시스템: 데이터 기반 체력 계산
        currentHealth = CalculateMaxHealth();
        
        Debug.Log($"[EnemyHealth] {gameObject.name} 체력 초기화: {currentHealth}");
        
        // ⭐ 엘리트/보스 체력바 생성
        InitializeHealthBar();
    }

    /// <summary>
    /// ⭐ 데이터 필수 - 최대 체력 계산
    /// </summary>
    private int CalculateMaxHealth()
    {
        if (baseEnemy != null)
        {
            float scaledHealth = baseEnemy.GetScaledMaxHealth();
            Debug.Log($"[EnemyHealth] BaseEnemy 스케일된 체력 사용: {scaledHealth:F1}");
            return Mathf.RoundToInt(scaledHealth);
        }
        
        // ❌ fallback 제거: 에러 처리
        Debug.LogError($"[EnemyHealth] {gameObject.name}: BaseEnemy가 없습니다! 데이터 설정을 확인해주세요.");
        return 1; // 크래시 방지용 최소값
    }

    /// <summary>
    /// ⭐ 데이터 필수 - 넉백 강도 계산
    /// </summary>
    private float CalculateKnockBackThrust()
    {
        if (baseEnemy?.EnemyData != null)
        {
            float knockBack = baseEnemy.EnemyData.KnockBackThrust;
            Debug.Log($"[EnemyHealth] EnemyData 넉백 강도 사용: {knockBack}");
            return knockBack;
        }
        
        // ❌ fallback 제거: 에러 처리
        Debug.LogError($"[EnemyHealth] {gameObject.name}: EnemyData가 없습니다! 넉백 설정을 확인해주세요.");
        return 1f; // 크래시 방지용 최소값
    }

    /// <summary>
    /// ⭐ 데이터 필수 - 경험치 계산
    /// </summary>
    private int CalculateExperienceReward()
    {
        if (baseEnemy?.EnemyData != null && baseEnemy?.GrowthProfile != null)
        {
            int scaledExp = baseEnemy.EnemyData.GetScaledExpReward(baseEnemy.CurrentLevel, baseEnemy.GrowthProfile);
            return scaledExp;
        }
        
        Debug.LogError($"❌ [EnemyHealth] {gameObject.name}: EnemyData 또는 GrowthProfile이 없습니다!");
        return 1; // 크래시 방지용 최소값
    }

    /// <summary>
    /// ⭐ 데이터 필수 - 골드 보상 계산
    /// </summary>
    private int CalculateGoldReward()
    {
        if (baseEnemy?.EnemyData != null && baseEnemy?.GrowthProfile != null)
        {
            int scaledGold = baseEnemy.EnemyData.GetScaledGoldReward(baseEnemy.CurrentLevel, baseEnemy.GrowthProfile);
            return scaledGold;
        }
        
        Debug.LogError($"❌ [EnemyHealth] {gameObject.name}: EnemyData 또는 GrowthProfile이 없습니다!");
        return 1; // 크래시 방지용 최소값
    }

    /// <summary>
    /// ⭐ 데이터 필수 - 보스 여부 확인
    /// </summary>
    public bool IsBoss()
    {
        if (baseEnemy?.EnemyData != null)
        {
            return baseEnemy.EnemyData.IsBoss;
        }
        
        Debug.LogError($"[EnemyHealth] {gameObject.name}: EnemyData가 없어서 보스 여부 확인 불가!");
        return false;
    }

    /// <summary>
    /// ⭐ 데이터 필수 - 미니보스 여부 확인
    /// </summary>
    public bool IsMiniBoss()
    {
        if (baseEnemy?.EnemyData != null)
        {
            return baseEnemy.EnemyData.IsMiniBoss;
        }
        
        Debug.LogError($"[EnemyHealth] {gameObject.name}: EnemyData가 없어서 미니보스 여부 확인 불가!");
        return false;
    }

    /// <summary>
    /// ⭐ 데이터 필수 - 보스 ID 확인
    /// </summary>
    public string GetBossId()
    {
        if (baseEnemy?.EnemyData != null)
        {
            return baseEnemy.EnemyData.BossId;
        }
        
        Debug.LogError($"[EnemyHealth] {gameObject.name}: EnemyData가 없어서 보스 ID 확인 불가!");
        return "";
    }
    
    #region ⭐ Phase 2: 동적 레벨 초기화 시스템
    
    /// <summary>
    /// 동적 레벨 변경 후 체력 리셋
    /// BaseEnemy.InitializeLevel()에서 호출됨
    /// </summary>
    public void ResetHealthToMax()
    {
        // ⭐ BaseEnemy의 currentLevel이 이미 변경된 상태!
        int newMaxHealth = CalculateMaxHealth(); // baseEnemy.GetScaledMaxHealth() 호출
        currentHealth = newMaxHealth;
        
        Debug.Log($"💚 [EnemyHealth] {gameObject.name} 체력 리셋: {currentHealth} (레벨 {baseEnemy?.CurrentLevel})");
        
        // 체력바 갱신 (비율 1.0 = 100%)
        if (activeHealthBar != null)
        {
            activeHealthBar.UpdateHealthBar(1f); // 최대 체력으로 리셋
        }
        
        if (basicHealthBar != null)
        {
            basicHealthBar.UpdateHealthBar(1f); // 최대 체력으로 리셋
        }
    }
    
    // ⭐ MaxHealth, CurrentHealth 프로퍼티는 이미 파일 하단(Line 1015, 1020)에 정의되어 있음
    
    #endregion

    /// <summary>
    /// ⭐ 데이터 필수 - 사망 이펙트 로드
    /// </summary>
    private GameObject LoadDeathVFX()
    {
        if (baseEnemy?.EnemyData != null)
        {
            string vfxPath = baseEnemy.EnemyData.DeathVFXPrefabPath;
            if (!string.IsNullOrEmpty(vfxPath))
            {
                GameObject vfxPrefab = Resources.Load<GameObject>(vfxPath);
                if (vfxPrefab != null)
                {
                    Debug.Log($"[EnemyHealth] 사망 이펙트 로드 성공: {vfxPath}");
                    return vfxPrefab;
                }
                else
                {
                    Debug.LogError($"[EnemyHealth] {gameObject.name}: 사망 이펙트를 찾을 수 없습니다 - {vfxPath}");
                }
            }
        }
        else
        {
            Debug.LogError($"[EnemyHealth] {gameObject.name}: EnemyData가 없어서 사망 이펙트 로드 불가!");
        }
        
        return null; // 이펙트 없음
    }

    /// <summary>
    /// ⚔️ 기본 데미지 받기 (기존 시스템 호환용)
    /// </summary>
    public void TakeDamage(int damage)
    {
        // ⭐ 이미 죽었거나 사망 애니메이션 중이면 데미지 무시
        if (isDead || isDeathAnimationPlaying) 
        {
            Debug.Log($"[EnemyHealth] {gameObject.name}: 이미 죽었거나 사망 중이므로 데미지 무시");
            return;
        }

        currentHealth -= damage;
        
        // ⭐ 데미지 넘버 표시 (Phase 1 + 앵커 시스템)
        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.ShowDamage(
                transform.position, 
                damage, 
                isPlayer: false, 
                targetTransform: transform  // ⭐ 앵커 검색용
            );
        }
        
        // ⭐ Basic 몬스터 체력바 표시 (피격 시에만)
        // ShowAndAutoHide() 파라미터 없이 호출 → BasicEnemyHealthBarUI의 Inspector 값 사용
        if (basicHealthBar != null)
        {
            basicHealthBar.ShowAndAutoHide();
        }
        
        // ⭐ 피격 이벤트 발생 (튜토리얼용)
        OnTakeDamageEvent?.Invoke();
        
        // ⭐ 체력바 업데이트
        UpdateHealthBar();
        
        // ✅ 🎵 Cue 시스템 추가 - 피격 이펙트 발행
        EmitHitCues(damage);
        
        // FSM 기반 Hit/Die 상태 전환 (IEnemy 구현 몬스터만)
        IEnemy enemyFSM = GetComponent<IEnemy>();
        
        if (currentHealth <= 0)
        {
            // ⭐ 즉시 데미지 차단용 플래그 설정
            isDead = true;
            isDeathAnimationPlaying = true;
            
            // 🧹 Phase 4-C: 사망 시 모든 상태이상 제거
            if (StatusEffectManager.Instance != null)
            {
                StatusEffectManager.Instance.ClearEffectsOnTarget(gameObject);
            }
            
            Debug.Log($"[EnemyHealth] {gameObject.name}: 사망 상태 진입, 넉백 스킵!");
            
            // ⭐ 사망 시 넉백 스킵 (Die 애니메이션 방해 방지)
            // Flash만 실행
            StartCoroutine(flash.FlashRoutine());
            
            // FSM 기반 Die 상태 전환 (IEnemy 구현 몬스터만)
            if (enemyFSM != null && enemyFSM.FSMController != null)
            {
                enemyFSM.FSMController.ChangeState(new EnemyDieState(enemyFSM));
            }
            
            StartCoroutine(DieRoutine());
        }
        else
        {
            // ⭐ 살아있을 때만 넉백 및 Hit 상태 전환
            StartCoroutine(flash.FlashRoutine());
            
            // ⭐⭐⭐ 넉백 분기 처리 (NavMesh vs 물리 넉백)
            if (baseEnemy != null && baseEnemy.IsUsingNavMesh)
            {
                // NavMesh 몬스터: EnemyHitState에서 연출 넉백 실행
                // Knockback 컴포넌트 불필요!
                Debug.Log($"[EnemyHealth] {gameObject.name} NavMesh 몬스터 - 연출 넉백 사용 (Knockback 컴포넌트 불필요)");
            }
            else
            {
                // 비-NavMesh 몬스터: 기존 물리 넉백 사용
                if (knockback != null)
                {
                    float knockBackThrust = CalculateKnockBackThrust();
                    knockback.GetKnockedBack(FindObjectOfType<PlayerController>().transform, knockBackThrust);
                    Debug.Log($"[EnemyHealth] {gameObject.name} 물리 넉백 실행 (Knockback 컴포넌트 사용)");
                }
                else
                {
                    Debug.LogWarning($"⚠️ [EnemyHealth] {gameObject.name} 비-NavMesh 몬스터인데 Knockback 컴포넌트가 없습니다!");
                }
            }
            
            // ⭐ 보스 스킬 실행 중이면 강제 취소 (피격 시 스킬 상태가 막히는 버그 방지)
            var bossSkillController = GetComponent<BossSkillController>();
            if (bossSkillController != null)
            {
                bossSkillController.ForceCancelSkill();
            }
            
            if (enemyFSM != null && enemyFSM.FSMController != null)
            {
                // 현재 상태를 저장하고 Hit 상태로 전환
                enemyFSM.FSMController.ChangeState(new EnemyHitState(enemyFSM, null));
            }
        }
    }
    
    /// <summary>
    /// ⚔️ Phase 4-C: DamageResult 기반 데미지 받기 (완전한 전투 공식 연동)
    /// </summary>
    public void TakeDamage(CombatFormula.DamageResult result, Transform hitTransform)
    {
        // ⭐ 이미 죽었거나 사망 애니메이션 중이면 데미지 무시
        if (isDead || isDeathAnimationPlaying) 
        {
            Debug.Log($"[EnemyHealth] {gameObject.name}: 이미 죽었거나 사망 중이므로 데미지 무시");
            return;
        }

        // 1️⃣ 실제 HP 차감
        currentHealth -= result.finalDamage;
        
        // 2️⃣ 데미지 넘버 표시 (Phase 4-C: DamageResult 통합 - 크리티컬, 면역 연출 포함)
        if (DamageNumberManager.Instance != null)
        {
            DamageNumberManager.Instance.ShowDamage(
                transform.position, 
                result,  // ⚙️ DamageResult 통째로 전달
                isPlayer: false,
                targetTransform: transform  // ⭐ 앵커 검색용
            );
        }
        
        // 3️⃣ Basic 몬스터 체력바 표시 (피격 시에만)
        if (basicHealthBar != null)
        {
            basicHealthBar.ShowAndAutoHide();
        }
        
        // 4️⃣ 피격 이벤트 발생 (튜토리얼용)
        OnTakeDamageEvent?.Invoke();
        
        // 5️⃣ 체력바 업데이트
        UpdateHealthBar();
        
        // 6️⃣ Cue 시스템 추가 - 피격 이펙트 발행
        EmitHitCues(result.finalDamage);
        
        // 7️⃣ 🛡️ Phase 4-C: 면역 처리
        if (result.hasImmunity && !string.IsNullOrEmpty(result.resistedEffects))
        {
            // 면역 정보 저장 (StatusEffectManager에서 참조)
            currentResistedEffects = result.resistedEffects;
            immunityExpireTime = Time.time + immunityDuration;
            
            Debug.Log($"🛡️ [EnemyHealth] {gameObject.name} 면역 발동! 저항한 효과: {result.resistedEffects}");
        }
        
        // 8️⃣ 💉 Phase 4-C: 회복 차단 처리
        if (result.healingBlockPercent > 0)
        {
            // TODO: 회복 차단 디버프 구현 (나중에 몬스터 회복 시스템 추가 시)
            Debug.Log($"🚫 [EnemyHealth] {gameObject.name} 회복 차단 {result.healingBlockPercent * 100:F0}% (구현 예정)");
        }
        
        // FSM 기반 Hit/Die 상태 전환 (IEnemy 구현 몬스터만)
        IEnemy enemyFSM = GetComponent<IEnemy>();
        
        if (currentHealth <= 0)
        {
            // ⭐ 즉시 데미지 차단용 플래그 설정
            isDead = true;
            isDeathAnimationPlaying = true;
            
            // 🧹 Phase 4-C: 사망 시 모든 상태이상 제거
            if (StatusEffectManager.Instance != null)
            {
                StatusEffectManager.Instance.ClearEffectsOnTarget(gameObject);
            }
            
            Debug.Log($"[EnemyHealth] {gameObject.name}: 사망 상태 진입, 넉백 스킵!");
            
            // ⭐ 사망 시 넉백 스킵 (Die 애니메이션 방해 방지)
            // Flash만 실행
            StartCoroutine(flash.FlashRoutine());
            
            // FSM 기반 Die 상태 전환 (IEnemy 구현 몬스터만)
            if (enemyFSM != null && enemyFSM.FSMController != null)
            {
                enemyFSM.FSMController.ChangeState(new EnemyDieState(enemyFSM));
            }
            
            StartCoroutine(DieRoutine());
        }
        else
        {
            // ⭐ 살아있을 때만 넉백 및 Hit 상태 전환
            StartCoroutine(flash.FlashRoutine());
            
            // ⭐⭐⭐ 넉백 분기 처리 (NavMesh vs 물리 넉백)
            if (baseEnemy != null && baseEnemy.IsUsingNavMesh)
            {
                // NavMesh 몬스터: EnemyHitState에서 연출 넉백 실행
                // Knockback 컴포넌트 불필요!
                Debug.Log($"[EnemyHealth] {gameObject.name} NavMesh 몬스터 - 연출 넉백 사용 (Knockback 컴포넌트 불필요)");
            }
            else
            {
                // 비-NavMesh 몬스터: 기존 물리 넉백 사용
                if (knockback != null)
                {
                    float knockBackThrust = CalculateKnockBackThrust();
                    knockback.GetKnockedBack(hitTransform, knockBackThrust);
                    Debug.Log($"[EnemyHealth] {gameObject.name} 물리 넉백 실행 (Knockback 컴포넌트 사용)");
                }
                else
                {
                    Debug.LogWarning($"⚠️ [EnemyHealth] {gameObject.name} 비-NavMesh 몬스터인데 Knockback 컴포넌트가 없습니다!");
                }
            }
            
            // ⭐ 보스 스킬 실행 중이면 강제 취소 (피격 시 스킬 상태가 막히는 버그 방지)
            var bossSkillController = GetComponent<BossSkillController>();
            if (bossSkillController != null)
            {
                bossSkillController.ForceCancelSkill();
            }
            
            if (enemyFSM != null && enemyFSM.FSMController != null)
            {
                // 현재 상태를 저장하고 Hit 상태로 전환
                enemyFSM.FSMController.ChangeState(new EnemyHitState(enemyFSM, null));
            }
        }
    }

    private IEnumerator DieRoutine()
    {
        Debug.Log($"[EnemyHealth] {gameObject.name} DieRoutine 실행 시작!");
        
        // ⭐ 플래그 초기화 (이미 TakeDamage에서 설정됨)
        deathEventTriggered = false;
        
        // ⭐ 체력바 제거
        DestroyHealthBar();

        // ⭐ 모든 물리 효과 즉시 중지 (넉백, 이동 등)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            // Rigidbody를 Kinematic으로 변경하여 물리 연산 완전 차단
            rb.bodyType = RigidbodyType2D.Kinematic;
            Debug.Log($"[EnemyHealth] {gameObject.name} 물리 효과 중지 (Kinematic)");
        }
        
        // ⭐ 이동 시스템 중지 (NavMeshAgent)
        BaseEnemy baseEnemy = GetComponent<BaseEnemy>();
        if (baseEnemy != null && baseEnemy.IsUsingNavMesh)
        {
            baseEnemy.Agent.isStopped = true;
            baseEnemy.Agent.ResetPath();
            Debug.Log($"[EnemyHealth] {gameObject.name} NavMeshAgent 정지");
        }

        // ⭐ 사망 애니메이션 재생
        Animator animator = GetComponent<Animator>();
        float animationLength = 1.333f; // 기본값
        
        if (animator != null)
        {
            // 사망 애니메이션 트리거
            animator.SetTrigger("Die");
            
            Debug.Log($"[EnemyHealth] {gameObject.name} 사망 애니메이션 트리거!");
            
            // ⭐ Die State로 전환될 때까지 대기 (최대 0.5초)
            float waitTime = 0f;
            float maxWaitTime = 0.5f;
            
            while (waitTime < maxWaitTime)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                
                // Die State 확인 (이름으로)
                if (stateInfo.IsName("Die") || stateInfo.IsName("Death") || stateInfo.IsName("Dead"))
                {
                    animationLength = stateInfo.length;
                    Debug.Log($"[EnemyHealth] {gameObject.name} Die State 진입 확인! 길이: {animationLength:F2}초");
                    break;
                }
                
                yield return new WaitForSeconds(0.05f);
                waitTime += 0.05f;
            }
            
            if (waitTime >= maxWaitTime)
            {
                Debug.LogWarning($"[EnemyHealth] {gameObject.name} Die State 전환 타임아웃! 기본값 사용");
            }
            
            // ⭐ 애니메이션이 완전히 재생되도록 대기
            Debug.Log($"[EnemyHealth] {gameObject.name} Die 애니메이션 재생 중... ({animationLength:F2}초)");
            yield return new WaitForSeconds(animationLength);
            
            Debug.Log($"[EnemyHealth] {gameObject.name} Die 애니메이션 재생 완료!");
        }
        else
        {
            Debug.LogWarning($"[EnemyHealth] {gameObject.name}: Animator가 없어서 즉시 처리");
            // Animator가 없으면 즉시 사망 처리
            OnDeathAnimationComplete();
            yield break;
        }

        // ⭐ 애니메이션 완료 후 즉시 사망 처리
        Debug.Log($"[EnemyHealth] {gameObject.name} DieRoutine 애니메이션 대기 완료 → 사망 처리 호출");
        OnDeathAnimationComplete();
    }

    /// <summary>
    /// ⭐ Animation Event에서 호출될 메서드 - 사망 처리 완료
    /// </summary>
    public void OnDeathAnimationComplete()
    {
        if (deathEventTriggered) return; // 중복 실행 방지
        
        deathEventTriggered = true;
        Debug.Log($"[EnemyHealth] {gameObject.name} Animation Event: 사망 처리 완료!");

        // ⭐ 경험치/골드 지급 (사망 처리의 최우선!)
        int experience = CalculateExperienceReward();
        int goldReward = CalculateGoldReward();
        
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AddGold(goldReward);
            PlayerDataManager.Instance.AddExp(experience);
        }
        else
        {
            Debug.LogError($"❌ [EnemyHealth] PlayerDataManager.Instance가 null입니다!");
        }

        // ✅ 🎵 Cue 시스템 추가 - 사망 이펙트 발행
        EmitDeathCues();

        // 🆕 WaveController에 적 사망 알림
        var waveController = FindObjectOfType<WaveController>();
        if (waveController != null)
        {
            waveController.OnEnemyDeath?.Invoke(gameObject);
        }

        // 보스 처치 시 스테이지 완료 체크 (FSMStageController에서 처리)
        if (IsBoss())
        {
            string bossId = GetBossId();
            Debug.Log($"[EnemyHealth] 보스 처치: {bossId}");
            
            // 🎁 Phase 2: 보스 처치 보상 지급
            if (BossRewardManager.Instance != null && baseEnemy != null && baseEnemy.EnemyData != null && baseEnemy.EnemyData.BossReward != null)
            {
                var rewardData = baseEnemy.EnemyData.BossReward;
                
                // 유효성 검증
                if (rewardData.IsValid())
                {
                    BossRewardManager.Instance.GrantBossFirstClearReward(
                        rewardData.bossId,
                        rewardData.rewardType,
                        rewardData.rewardAmount
                    );
                }
                else
                {
                    Debug.LogWarning($"[EnemyHealth] 보스 보상 데이터가 유효하지 않음: {rewardData.name}");
                }
            }
            else if (BossRewardManager.Instance == null)
            {
                Debug.LogWarning($"[EnemyHealth] BossRewardManager.Instance가 null입니다!");
            }
            // baseEnemy.EnemyData.BossReward가 null이면 보상 없음 (정상, 로그 불필요)
            
            // FSMStageController가 자동으로 승리 조건 체크함
        }

        // 사망 이펙트 생성
        GameObject deathVFX = LoadDeathVFX();
        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position, Quaternion.identity);
            Debug.Log($"[EnemyHealth] {gameObject.name} 사망 이펙트 생성");
        }

        // ⭐ 새 드롭 시스템: DropResolver 사용
        ExecuteNewDropSystem();
        
        // ⭐ 최종 상태 정리
        isDeathAnimationPlaying = false; // 애니메이션 완료
        
        Debug.Log($"[EnemyHealth] {gameObject.name} 페이드 아웃 시작 후 파괴!");
        
        // ⭐ 페이드 아웃 효과 후 파괴 (0.5~1초)
        StartCoroutine(FadeOutAndDestroy(0.7f));
    }

    /// <summary>
    /// ⭐ Animation Event에서 호출될 메서드 - 아이템 드롭 타이밍 (선택적)
    /// </summary>
    public void OnDeathDropItems()
    {
        Debug.Log($"[EnemyHealth] {gameObject.name} Animation Event: 아이템 드롭!");
        ExecuteNewDropSystem();
    }

    /// <summary>
    /// ⭐ Animation Event에서 호출될 메서드 - 사망 이펙트 타이밍 (선택적)
    /// </summary>
    public void OnDeathEffect()
    {
        Debug.Log($"[EnemyHealth] {gameObject.name} Animation Event: 사망 이펙트!");
        
        GameObject deathVFX = LoadDeathVFX();
        if (deathVFX != null)
        {
            Instantiate(deathVFX, transform.position, Quaternion.identity);
        }
    }

    /// <summary>
    /// ⭐ 페이드 아웃 효과 후 오브젝트 파괴
    /// </summary>
    private IEnumerator FadeOutAndDestroy(float fadeTime)
    {
        Debug.Log($"[EnemyHealth] {gameObject.name} 페이드 아웃 시작 ({fadeTime}초)");
        
        // SpriteRenderer 찾기 (8방향 몬스터용)
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        
        // SpriteRenderer가 없으면 자식에서 찾기 (구조가 복잡한 경우)
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        if (spriteRenderer != null)
        {
            float elapsedTime = 0f;
            Color originalColor = spriteRenderer.color;
            
            // 알파값을 1 → 0으로 부드럽게 감소
            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
                spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                yield return null;
            }
            
            Debug.Log($"[EnemyHealth] {gameObject.name} 페이드 아웃 완료, 파괴 실행");
        }
        else
        {
            // SpriteRenderer가 없으면 그냥 대기만
            Debug.LogWarning($"[EnemyHealth] {gameObject.name} SpriteRenderer 없음, {fadeTime}초 대기 후 파괴");
            yield return new WaitForSeconds(fadeTime);
        }
        
        // 최종 파괴
        Destroy(gameObject);
    }

    /// <summary>
    /// ⭐ 사망 애니메이션 길이 자동 감지
    /// </summary>
    private float GetDeathAnimationDuration(Animator animator)
    {
        if (animator == null) return 0.5f;

        // 현재 애니메이터 상태 정보 가져오기
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // 사망 애니메이션 클립 이름들 (몬스터별로 다를 수 있음)
        string[] deathAnimationNames = { "Die", "Death", "Dead", "Dying" };
        
        foreach (string animName in deathAnimationNames)
        {
            if (stateInfo.IsName(animName))
            {
                float duration = stateInfo.length;
                Debug.Log($"[EnemyHealth] 사망 애니메이션 '{animName}' 감지, 길이: {duration:F2}초");
                return Mathf.Max(duration, 0.3f); // 최소 0.3초 보장
            }
        }

        // 애니메이션 클립에서 직접 찾기
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller != null)
        {
            foreach (AnimationClip clip in controller.animationClips)
            {
                foreach (string deathName in deathAnimationNames)
                {
                    if (clip.name.ToLower().Contains(deathName.ToLower()))
                    {
                        float duration = clip.length;
                        Debug.Log($"[EnemyHealth] 사망 애니메이션 클립 '{clip.name}' 발견, 길이: {duration:F2}초");
                        return Mathf.Max(duration, 0.3f);
                    }
                }
            }
        }

        // 기본값 반환
        Debug.LogWarning($"[EnemyHealth] {gameObject.name}: 사망 애니메이션을 찾을 수 없어서 기본값 사용 (0.5초)");
        return 0.5f;
    }

    /// <summary>
    /// ⭐ 새 드롭 시스템 실행
    /// </summary>
    private void ExecuteNewDropSystem()
    {
        if (baseEnemy?.EnemyData == null)
        {
            Debug.LogError($"[EnemyHealth] {gameObject.name}: 드롭 시스템 실행 불가 - EnemyData가 없습니다!");
            return;
        }

        string dropGroupId = baseEnemy.EnemyData.DropGroupId;
        int dropRolls = baseEnemy.EnemyData.DropRolls;
        int currentStageLevel = GetCurrentStageLevel();

        Debug.Log($"[EnemyHealth] {gameObject.name} 드롭 시스템 실행:");
        Debug.Log($"  - DropGroupId: {dropGroupId}");
        Debug.Log($"  - DropRolls: {dropRolls}");
        Debug.Log($"  - StageLevel: {currentStageLevel}");

        if (string.IsNullOrEmpty(dropGroupId))
        {
            Debug.LogWarning($"[EnemyHealth] {gameObject.name}: DropGroupId가 설정되지 않았습니다. 아이템 드롭을 건너뜁니다.");
            return;
        }

        // 여러 번 드롭 시도
        List<DropResult> allDropResults = new List<DropResult>();
        
        for (int i = 0; i < dropRolls; i++)
        {
            List<DropResult> dropResults = DropResolver.ResolveDrop(dropGroupId, currentStageLevel);
            allDropResults.AddRange(dropResults);
            
            Debug.Log($"[EnemyHealth] 드롭 시도 #{i + 1}: {dropResults.Count}개 드롭");
        }

        // 드롭 결과 로그
        Debug.Log(DropResolver.GetDropResultsDebugInfo(allDropResults));

        // 실제 아이템 스폰 (순차적 드롭)
        StartCoroutine(SpawnDroppedItemsCoroutine(allDropResults));
    }

    /// <summary>
    /// 드롭된 아이템들을 실제로 스폰 (순차적 드롭)
    /// </summary>
    private IEnumerator SpawnDroppedItemsCoroutine(List<DropResult> dropResults)
    {
        foreach (var result in dropResults)
        {
            for (int i = 0; i < result.quantity; i++)
            {
                SpawnSingleItem(result.itemId, result.rarity);
                
                // ⭐ 순차적 드롭: 0.3~0.5초 랜덤 지연
                float delay = Random.Range(0.3f, 0.5f);
                yield return new WaitForSeconds(delay);
            }
        }
    }

    /// <summary>
    /// 개별 아이템 스폰 (아이템 타입별 캐시 분기)
    /// </summary>
    /// <summary>
    /// ✨ 신규 드롭 시스템: 범용 프리팹 + 데이터 주입 방식 [Phase 8-1: 룬 조각 지원]
    /// </summary>
    private void SpawnSingleItem(string itemId, ItemRarity rarity)
    {
        // ⭐ 스폰은 몬스터 위치에서, 드롭 애니메이션으로 퍼짐
        Vector3 spawnPosition = transform.position;
        
        // 재화 아이템 (골드/하트)
        if (itemId.StartsWith("ITEM_GOLD") || itemId.StartsWith("ITEM_HEALTH"))
        {
            SpawnCurrencyItem(itemId, spawnPosition);
        }
        // 📦 재료 아이템 (MAT_로 시작) + 💎 룬 조각 (RUNE_FRAG_로 시작) [Phase 8-1]
        else if (itemId.StartsWith("MAT_") || itemId.StartsWith("RUNE_FRAG_"))
        {
            SpawnMaterialItem(itemId, spawnPosition);
        }
        // 장비 아이템 (무기/방어구)
        else
        {
            SpawnEquipmentItem(itemId, rarity, spawnPosition);
        }
    }
    
    /// <summary>
    /// 재화 아이템 스폰 (Drop_Currency 프리팹 사용)
    /// </summary>
    private void SpawnCurrencyItem(string itemId, Vector3 spawnPosition)
    {
        // 1. 기존 데이터 가져오기
        var pickupDataCache = FindObjectOfType<PickupDataCache>();
        if (pickupDataCache == null)
        {
            Debug.LogError("[EnemyHealth] PickupDataCache를 찾을 수 없습니다!");
            return;
        }
        
        ItemSystem.BaseItemData itemData = pickupDataCache.GetPickupItemData(itemId);
        if (itemData == null)
        {
            Debug.LogError($"[EnemyHealth] PickupItemData를 찾을 수 없습니다: {itemId}");
            return;
        }
        
        // 2. Drop_Currency 프리팹 스폰 (범용 프리팹) ⭐
        GameObject dropObj = GamePoolManager.Instance.SpawnFromPool("Drop_Currency", spawnPosition, Quaternion.identity);
        
        if (dropObj == null)
        {
            Debug.LogError($"[EnemyHealth] 'Drop_Currency' 풀에서 오브젝트를 스폰할 수 없습니다!");
            return;
        }
        
        // 3. 데이터 주입 ⭐
        CurrencyPickup pickup = dropObj.GetComponent<CurrencyPickup>();
        if (pickup != null)
        {
            pickup.Initialize(itemData);
            Debug.Log($"💰 [EnemyHealth] 재화 드롭 성공: {itemData.itemName}");
        }
        else
        {
            Debug.LogError("[EnemyHealth] Drop_Currency 프리팹에 CurrencyPickup 컴포넌트가 없습니다!");
            dropObj.SetActive(false);
        }
    }
    
    /// <summary>
    /// 장비 아이템 스폰 (Drop_Equipment 프리팹 사용)
    /// </summary>
    private void SpawnEquipmentItem(string itemId, ItemRarity rarity, Vector3 spawnPosition)
    {
        // 1. 기존 데이터 가져오기
        var equipmentDataCache = FindObjectOfType<EquipmentDataCache>();
        if (equipmentDataCache == null)
        {
            Debug.LogError("[EnemyHealth] EquipmentDataCache를 찾을 수 없습니다!");
            return;
        }
        
        EquipmentData equipData = equipmentDataCache.GetEquipmentData(itemId);
        if (equipData == null)
        {
            Debug.LogError($"[EnemyHealth] EquipmentData를 찾을 수 없습니다: {itemId}");
            return;
        }
        
        // 🔍 디버그: ItemRarity 확인
        Debug.Log($"🔍 [DEBUG] SpawnEquipmentItem 호출: itemId={itemId}, rarity={rarity}");
        
        // 2. Drop_Equipment 프리팹 스폰 (범용 프리팹) ⭐
        GameObject dropObj = GamePoolManager.Instance.SpawnFromPool("Drop_Equipment", spawnPosition, Quaternion.identity);
        
        if (dropObj == null)
        {
            Debug.LogError($"[EnemyHealth] 'Drop_Equipment' 풀에서 오브젝트를 스폰할 수 없습니다!");
            return;
        }
        
        // 3. 데이터 주입 ⭐
        EquipmentPickup pickup = dropObj.GetComponent<EquipmentPickup>();
        if (pickup != null)
        {
            pickup.Initialize(equipData, rarity);
            Debug.Log($"🎒 [EnemyHealth] 장비 드롭 성공: {equipData.equipmentName} (ItemRarity: {rarity})");
        }
        else
        {
            Debug.LogError("[EnemyHealth] Drop_Equipment 프리팹에 EquipmentPickup 컴포넌트가 없습니다!");
            dropObj.SetActive(false);
        }
    }
    
    /// <summary>
    /// 📦 재료 아이템 스폰 (MaterialPickup 사용) [Phase 8-2: MaterialDatabase 통합]
    /// </summary>
    private void SpawnMaterialItem(string itemId, Vector3 spawnPosition)
    {
        // 1. itemId → MaterialData 검색 (일반 재료 & 룬 조각 통합)
        MaterialData materialData = MaterialDatabase.Instance?.GetDataById(itemId);
        
        if (materialData == null)
        {
            Debug.LogError($"❌ [EnemyHealth] MaterialData를 찾을 수 없습니다: {itemId}");
            return;
        }
        
        MaterialType materialType = materialData.materialType;
        
        // 2. Drop_Material 프리팹 스폰 (범용 프리팹)
        string poolTag = "Drop_Material";
        GameObject dropObj = GamePoolManager.Instance.SpawnFromPool(poolTag, spawnPosition, Quaternion.identity);
        
        if (dropObj == null)
        {
            Debug.LogError($"❌ [EnemyHealth] '{poolTag}' 풀에서 오브젝트를 스폰할 수 없습니다!");
            return;
        }
        
        // 3. 데이터 주입
        MaterialPickup pickup = dropObj.GetComponent<MaterialPickup>();
        if (pickup != null)
        {
            // 재료 수량 (1~3개 랜덤)
            int amount = Random.Range(1, 4);
            pickup.Initialize(materialType, amount, spawnPosition);
            
            Debug.Log($"📦 [EnemyHealth] 재료 드롭 성공: {materialType.GetDisplayName()} x{amount}");
        }
        else
        {
            Debug.LogError($"❌ [EnemyHealth] Drop_Material 프리팹에 MaterialPickup 컴포넌트가 없습니다!");
            GamePoolManager.Instance.ReturnToPool(poolTag, dropObj);
        }
    }

    /// <summary>
    /// 현재 스테이지 레벨 획득
    /// </summary>
    private int GetCurrentStageLevel()
    {
        // FSMStageController나 GameManager에서 현재 스테이지 레벨 획득
        if (FSMStageController.Instance != null)
        {
            // FSMStageController에서 스테이지별 레벨 매핑
            var currentStage = FSMStageController.Instance.GetCurrentStage();
            return MapStageToLevel(currentStage);
        }
        
        // 몬스터 레벨을 스테이지 레벨로 사용 (fallback)
        return baseEnemy?.CurrentLevel ?? 1;
    }

    /// <summary>
    /// 스테이지를 레벨로 매핑
    /// </summary>
    private int MapStageToLevel(FSMStageController.StageState stage)
    {
        return stage switch
        {
            FSMStageController.StageState.Lobby => 1,
            FSMStageController.StageState.Scene1 => 1,
            FSMStageController.StageState.Scene2 => 2,
            FSMStageController.StageState.Scene3 => 3,
            FSMStageController.StageState.Boss => 5,
            _ => 1
        };
    }
    
    // ⭐ UI 시스템용 공개 프로퍼티 추가
    /// <summary>
    /// 최대 체력 (UI 표시용)
    /// </summary>
    public int MaxHealth => CalculateMaxHealth();
    
    /// <summary>
    /// 현재 체력 (UI 표시용)
    /// </summary>
    public int CurrentHealth => currentHealth;
    
    /// <summary>
    /// 체력 비율 (UI 표시용)
    /// </summary>
    public float HealthRatio => MaxHealth > 0 ? (float)currentHealth / MaxHealth : 0f;

    #region ✅ 🎵 Cue 시스템 연동 (Phase B-3 추가)
    
    /// <summary>
    /// 🎵 사망 이펙트 Cue 발행
    /// </summary>
    private void EmitDeathCues()
    {
        try
        {
            // CueContext 생성
            var context = new CueSystem.CueContext
            {
                position = transform.position,
                rotation = transform.rotation,
                normal = Vector3.up,
                facingDir = Vector2.down, // 사망 시 아래 방향
                follow = null,
                actorType = CueSystem.ActorType.Enemy,
                surfaceType = CueSystem.SurfaceType.Default,
                magnitude = IsBoss() ? 2.0f : 1.0f, // 보스는 더 강한 이펙트
                isCritical = false,
                scale = IsBoss() ? 1.5f : 1.0f
            };
            
            // 이벤트 키 결정 (보스 vs 일반)
            string eventKey = IsBoss() ? "death.enemy.boss" : "death.enemy.normal";
            
            // Cue 발행
            bool success = CueSystem.CueEmitter.Emit(eventKey, "Enemy", context);
            
            Debug.Log($"🎵 [EnemyHealth] 사망 Cue 발행: {eventKey} → {(success ? "성공" : "실패")}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🔴 [EnemyHealth] 사망 Cue 발행 오류: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 🎵 피격 이펙트 Cue 발행
    /// </summary>
    private void EmitHitCues(int damage)
    {
        try
        {
            // 크리티컬 판정 (임시: 데미지가 높으면 크리티컬로 간주)
            int maxHealth = MaxHealth; // ✅ 수정: GetMaxHealth() → MaxHealth
            bool isCritical = damage >= (maxHealth * 0.3f); // 최대 체력의 30% 이상이면 크리티컬
            
            // CueContext 생성
            var context = new CueSystem.CueContext
            {
                position = transform.position,
                rotation = transform.rotation,
                normal = Vector3.up,
                facingDir = GetHitDirection(),
                follow = null,
                actorType = CueSystem.ActorType.Enemy,
                surfaceType = CueSystem.SurfaceType.Flesh, // 적은 기본적으로 살점
                magnitude = (float)damage / maxHealth, // 데미지 비율로 강도 결정
                isCritical = isCritical,
                scale = isCritical ? 1.3f : 1.0f,
                damage = damage
            };
            
            // 이벤트 키 결정
            string eventKey = isCritical ? "hit.enemy.critical" : "hit.enemy.normal";
            
            // Cue 발행
            bool success = CueSystem.CueEmitter.Emit(eventKey, "Enemy", context);
            
            Debug.Log($"🎵 [EnemyHealth] 피격 Cue 발행: {eventKey} (데미지: {damage}) → {(success ? "성공" : "실패")}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🔴 [EnemyHealth] 피격 Cue 발행 오류: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 🧭 피격 방향 계산
    /// </summary>
    private Vector2 GetHitDirection()
    {
        // 플레이어 방향에서 오는 피격으로 가정
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            Vector2 direction = (transform.position - player.transform.position).normalized;
            return direction;
        }
        
        return Vector2.up; // 기본값
    }
    
    #endregion
    
    #region 🎯 엘리트/보스 체력바 시스템
    
    /// <summary>
    /// 체력바 초기화 (타입별 분기)
    /// </summary>
    private void InitializeHealthBar()
    {
        if (baseEnemy?.EnemyData == null) return;
        
        EnemyType enemyType = baseEnemy.EnemyData.EnemyType;
        
        if (enemyType == EnemyType.Elite || enemyType == EnemyType.Boss)
        {
            // ✅ 엘리트/보스: 항상 표시되는 체력바
            CreateEliteHealthBar();
        }
        else if (enemyType == EnemyType.Basic)
        {
            // ⭐ Basic: 피격 시에만 표시되는 체력바
            CreateBasicHealthBar();
        }
    }
    
    /// <summary>
    /// 엘리트/보스 체력바 생성
    /// </summary>
    private void CreateEliteHealthBar()
    {
        if (eliteHealthBarPrefab == null)
        {
            Debug.LogWarning($"⚠️ [EnemyHealth] {gameObject.name}: 엘리트/보스인데 체력바 Prefab이 할당되지 않았습니다!");
            return;
        }
        
        // 체력바 생성 위치 계산 (앵커 우선)
        Vector3 healthBarPosition = GetHealthBarPosition();
        GameObject healthBarObj = Instantiate(eliteHealthBarPrefab, healthBarPosition, Quaternion.identity, transform);
        
        activeHealthBar = healthBarObj.GetComponent<EliteHealthBarUI>();
        
        if (activeHealthBar != null)
        {
            activeHealthBar.SetHealthImmediate(HealthRatio);
            Debug.Log($"✅ [EnemyHealth] {gameObject.name} 엘리트 체력바 생성 완료");
        }
        else
        {
            Debug.LogError($"❌ [EnemyHealth] {gameObject.name}: 체력바 Prefab에 EliteHealthBarUI 컴포넌트가 없습니다!");
        }
    }
    
    /// <summary>
    /// ⭐ Basic 몬스터 체력바 생성 (자동 숨김)
    /// </summary>
    private void CreateBasicHealthBar()
    {
        if (basicHealthBarPrefab == null)
        {
            // Basic 타입은 체력바 없어도 경고 안 함 (선택적 기능)
            return;
        }
        
        // 체력바 생성 위치 계산 (앵커 우선)
        Vector3 healthBarPosition = GetHealthBarPosition();
        GameObject healthBarObj = Instantiate(basicHealthBarPrefab, healthBarPosition, Quaternion.identity, transform);
        
        basicHealthBar = healthBarObj.GetComponent<BasicEnemyHealthBarUI>();
        
        if (basicHealthBar != null)
        {
            basicHealthBar.SetHealthImmediate(HealthRatio);
            basicHealthBar.HideHealthBar(); // ⭐ 초기에는 숨김
            
            Debug.Log($"✅ [EnemyHealth] {gameObject.name} Basic 체력바 생성 완료 (초기: 숨김)");
        }
        else
        {
            Debug.LogError($"❌ [EnemyHealth] {gameObject.name}: Basic 체력바 Prefab에 BasicEnemyHealthBarUI 컴포넌트가 없습니다!");
        }
    }
    
    /// <summary>
    /// ⭐ 체력바 생성 위치 계산 (HealthBarAnchor 우선)
    /// </summary>
    private Vector3 GetHealthBarPosition()
    {
        // 1순위: HealthBarAnchor 찾기
        Transform anchor = FindHealthBarAnchor();
        if (anchor != null)
        {
            return anchor.position;
        }
        
        // 2순위: 오프셋 사용
        return transform.position + Vector3.up * healthBarOffsetY;
    }
    
    /// <summary>
    /// ⭐ HealthBarAnchor 찾기 (자식 오브젝트 검색)
    /// </summary>
    private Transform FindHealthBarAnchor()
    {
        // 직접 검색
        Transform anchor = transform.Find("HealthBarAnchor");
        if (anchor != null)
        {
            return anchor;
        }
        
        // 재귀 검색 (깊은 계층 구조 대응)
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "HealthBarAnchor")
            {
                return child;
            }
        }
        
        return null;
    }
    
    // ❌ 제거: ShouldShowHealthBar() - 더 이상 필요 없음 (타입별로 분기 처리)
    
    /// <summary>
    /// 체력바 업데이트
    /// </summary>
    private void UpdateHealthBar()
    {
        // 엘리트/보스 체력바
        if (activeHealthBar != null)
        {
            activeHealthBar.UpdateHealthBar(HealthRatio);
        }
        
        // ⭐ Basic 체력바 (표시 중일 때만 업데이트)
        if (basicHealthBar != null && basicHealthBar.IsVisible)
        {
            basicHealthBar.UpdateHealthBar(HealthRatio);
        }
    }
    
    /// <summary>
    /// 체력바 제거
    /// </summary>
    private void DestroyHealthBar()
    {
        // 엘리트/보스 체력바 제거
        if (activeHealthBar != null)
        {
            Destroy(activeHealthBar.gameObject);
            activeHealthBar = null;
            
            Debug.Log($"🗑️ [EnemyHealth] {gameObject.name} 엘리트 체력바 제거");
        }
        
        // ⭐ Basic 체력바 제거
        if (basicHealthBar != null)
        {
            Destroy(basicHealthBar.gameObject);
            basicHealthBar = null;
            
            Debug.Log($"🗑️ [EnemyHealth] {gameObject.name} Basic 체력바 제거");
        }
    }
    
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