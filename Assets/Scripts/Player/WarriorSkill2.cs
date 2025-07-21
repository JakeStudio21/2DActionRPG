using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Added for List

/// <summary>
/// Warrior 스킬2: Ground Slam (땅을 내리쳐 원형 충격파 발생)
/// ISkill 인터페이스를 구현하여 모듈식 스킬 시스템에 통합
/// ScriptableObject 기반 데이터 분리 적용
/// </summary>
public class WarriorSkill2 : MonoBehaviour, ISkill
{
    [Header("스킬 데이터 (ScriptableObject)")]
    public SkillData skillData; // Inspector에서 할당
    
    [Header("충격파 설정")]
    [SerializeField] private float shockwaveRadius = 5f;         // 충격파 범위
    [SerializeField] private float chargeTime = 0.8f;            // 차징 시간
    [SerializeField] private float knockbackForce = 15f;         // 넉백 힘
    [SerializeField] private float shockwaveExpandTime = 0.3f;   // 충격파 확장 시간
    [SerializeField] private AnimationCurve damageFalloff;       // 거리별 데미지 감소 곡선
    
    [Header("이펙트")]
    [SerializeField] private GameObject chargeEffectPrefab;      // 차징 이펙트
    [SerializeField] private GameObject shockwaveEffectPrefab;   // 충격파 이펙트
    [SerializeField] private GameObject groundCrackEffectPrefab; // 바닥 균열 이펙트
    [SerializeField] private string chargeEffectPoolName = "ChargeEffect";
    [SerializeField] private string shockwaveEffectPoolName = "ShockwaveEffect";
    [SerializeField] private string groundCrackEffectPoolName = "GroundCrackEffect";
    
    [Header("오디오")]
    [SerializeField] private AudioClip chargeSound;             // 차징 사운드
    [SerializeField] private AudioClip slamSound;               // 내려치기 사운드
    [SerializeField] private AudioClip shockwaveSound;          // 충격파 사운드
    
    [Header("디버그")]
    public bool showDebugLogs = true;
    
    // ISkill 인터페이스 구현
    public string SkillName => skillData != null ? skillData.skillName : "Ground Slam";
    public float Cooldown => skillData != null ? skillData.cooldown : 6f;
    
    // 내부 상태
    private float lastSkillTime = -Mathf.Infinity;
    private bool isExecuting = false;
    private PlayerAnimationController animationController;
    private AudioSource audioSource;
    private GameObject currentChargeEffect;
    
    // 이벤트
    public System.Action<float> OnSkillCooldownChanged;
    
    void Awake()
    {
        if (showDebugLogs)
            Debug.Log("🟢 [WarriorSkill2] Awake() 시작");
            
        // 컴포넌트 참조 초기화
        animationController = GetComponent<PlayerAnimationController>();
        audioSource = GetComponent<AudioSource>();
        
        if (animationController == null)
            Debug.LogWarning("🟡 [WarriorSkill2] PlayerAnimationController를 찾을 수 없습니다!");
        if (audioSource == null)
            Debug.LogWarning("🟡 [WarriorSkill2] AudioSource를 찾을 수 없습니다!");
        
        // 데미지 감소 곡선 기본값 설정
        if (damageFalloff == null || damageFalloff.length == 0)
        {
            damageFalloff = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.3f));
        }
    }
    
    void Start()
    {
        // SkillData 검증
        if (skillData == null)
        {
            Debug.LogWarning("🟡 [WarriorSkill2] SkillData가 할당되지 않았습니다. 기본값을 사용합니다.");
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"🟢 [WarriorSkill2] SkillData 로딩 완료: {skillData.skillName}");
        }
    }
    
    #region ISkill 인터페이스 구현
    
    public bool CanUse()
    {
        if (isExecuting)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [WarriorSkill2] 스킬 실행 중입니다!");
            return false;
        }
        
        float remainingCooldown = GetCooldownRemaining();
        bool canUse = remainingCooldown <= 0f;
        
        if (!canUse && showDebugLogs)
            Debug.LogWarning($"🟡 [WarriorSkill2] 쿨다운 중! 남은 시간: {remainingCooldown:F1}초");
            
        return canUse;
    }
    
    public void Execute()
    {
        if (showDebugLogs)
            Debug.Log("🔵 [WarriorSkill2] Execute() 호출됨");
            
        if (!CanUse())
        {
            Debug.LogWarning($"🟡 [WarriorSkill2] 스킬2 쿨다운 중! 남은 시간: {GetCooldownRemaining():F1}초");
            return;
        }
        
        // 쿨다운 시작
        lastSkillTime = Time.time;
        
        // 애니메이션 트리거 (PlayerAnimationController를 통해)
        if (animationController != null)
        {
            bool success = animationController.TriggerSkill2();
            if (success)
            {
                if (OnSkillCooldownChanged != null)
                    OnSkillCooldownChanged(Cooldown);
                    
                Debug.Log($"🟢 [WarriorSkill2] 스킬2(Ground Slam) 애니메이션 트리거 성공!");
            }
            else
            {
                Debug.LogWarning("🟡 [WarriorSkill2] 애니메이션 트리거 실패 - 직접 실행");
                OnAnimationEvent();
            }
        }
        else
        {
            // PlayerAnimationController가 없으면 직접 실행
            Debug.LogWarning("🟡 [WarriorSkill2] PlayerAnimationController가 없음 - 직접 실행");
            OnAnimationEvent();
        }
    }
    
    public void OnAnimationEvent()
    {
        if (showDebugLogs)
            Debug.Log("🔵 [WarriorSkill2] OnAnimationEvent() - 충격파 공격 시작!");
            
        if (isExecuting)
        {
            Debug.LogWarning("🟡 [WarriorSkill2] 이미 스킬이 실행 중입니다!");
            return;
        }
        
        StartCoroutine(GroundSlamSequence());
    }
    
    public float GetCooldownRemaining()
    {
        float elapsed = Time.time - lastSkillTime;
        return Mathf.Max(0f, Cooldown - elapsed);
    }
    
    #endregion
    
    #region 충격파 공격 시퀀스
    
    private IEnumerator GroundSlamSequence()
    {
        isExecuting = true;
        
        if (showDebugLogs)
            Debug.Log("💥 [WarriorSkill2] 충격파 공격 시퀀스 시작!");
        
        // 1단계: 차징 단계
        yield return StartCoroutine(ChargePhase());
        
        // 2단계: 내려치기 단계
        yield return StartCoroutine(SlamPhase());
        
        // 3단계: 충격파 확산 단계
        yield return StartCoroutine(ShockwavePhase());
        
        if (showDebugLogs)
            Debug.Log("🟢 [WarriorSkill2] 충격파 공격 시퀀스 완료!");
        
        isExecuting = false;
    }
    
    private IEnumerator ChargePhase()
    {
        if (showDebugLogs)
            Debug.Log("⚡ [WarriorSkill2] 차징 시작!");
        
        // 차징 이펙트 생성
        if (chargeEffectPrefab != null && GamePoolManager.Instance != null)
        {
            currentChargeEffect = GamePoolManager.Instance.SpawnFromPool(chargeEffectPoolName, transform.position, Quaternion.identity);
            if (currentChargeEffect != null)
            {
                currentChargeEffect.transform.SetParent(transform);
            }
        }
        
        // 차징 사운드 재생
        if (audioSource != null && chargeSound != null)
        {
            audioSource.PlayOneShot(chargeSound);
        }
        
        // 차징 시간 대기
        yield return new WaitForSeconds(chargeTime);
        
        // 차징 이펙트 제거
        if (currentChargeEffect != null)
        {
            GamePoolManager.Instance.ReturnToPool(chargeEffectPoolName, currentChargeEffect);
            currentChargeEffect = null;
        }
        
        if (showDebugLogs)
            Debug.Log("🟢 [WarriorSkill2] 차징 완료!");
    }
    
    private IEnumerator SlamPhase()
    {
        if (showDebugLogs)
            Debug.Log("🔨 [WarriorSkill2] 내려치기!");
        
        // 내려치기 사운드 재생
        if (audioSource != null && slamSound != null)
        {
            audioSource.PlayOneShot(slamSound);
        }
        
        // 바닥 균열 이펙트 생성
        if (groundCrackEffectPrefab != null && GamePoolManager.Instance != null)
        {
            GamePoolManager.Instance.SpawnFromPool(groundCrackEffectPoolName, transform.position, Quaternion.identity);
        }
        
        // 짧은 대기 (타격감)
        yield return new WaitForSeconds(0.1f);
    }
    
    private IEnumerator ShockwavePhase()
    {
        if (showDebugLogs)
            Debug.Log("🌊 [WarriorSkill2] 충격파 확산!");
        
        // 충격파 사운드 재생
        if (audioSource != null && shockwaveSound != null)
        {
            audioSource.PlayOneShot(shockwaveSound);
        }
        
        // 충격파 이펙트 생성
        if (shockwaveEffectPrefab != null && GamePoolManager.Instance != null)
        {
            var shockwaveEffect = GamePoolManager.Instance.SpawnFromPool(shockwaveEffectPoolName, transform.position, Quaternion.identity);
            if (shockwaveEffect != null)
            {
                // 충격파 이펙트 크기 조절
                shockwaveEffect.transform.localScale = Vector3.one * shockwaveRadius;
            }
        }
        
        // 충격파 데미지 처리
        PerformShockwaveDamage();
        
        // 충격파 확산 시간 대기
        yield return new WaitForSeconds(shockwaveExpandTime);
    }
    
    private void PerformShockwaveDamage()
    {
        // 범위 내 적들 탐지 - 안전한 방법 사용
        Collider2D[] hitEnemies = null;
        
        try
        {
            // 1순위: Enemy 레이어로 탐지 시도
            hitEnemies = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius, LayerMask.GetMask("Enemy"));
            if (showDebugLogs)
                Debug.Log($"💥 [WarriorSkill2] Enemy 레이어로 {hitEnemies.Length}명의 적 감지!");
        }
        catch (System.Exception)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [WarriorSkill2] Enemy 레이어가 정의되지 않음. 모든 콜라이더 검색...");
            hitEnemies = null;
        }
        
        // 2순위: 모든 콜라이더에서 EnemyHealth 컴포넌트 찾기
        if (hitEnemies == null || hitEnemies.Length == 0)
        {
            Collider2D[] allColliders = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius);
            List<Collider2D> enemyColliders = new List<Collider2D>();
            
            foreach (Collider2D collider in allColliders)
            {
                if (collider != null && collider.GetComponent<EnemyHealth>() != null)
                {
                    enemyColliders.Add(collider);
                }
            }
            
            hitEnemies = enemyColliders.ToArray();
            if (showDebugLogs)
                Debug.Log($"💥 [WarriorSkill2] EnemyHealth 컴포넌트로 {hitEnemies.Length}명의 적 감지!");
        }
        
        // 3순위: 마지막 안전장치 - null 체크
        if (hitEnemies == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [WarriorSkill2] 적을 찾을 수 없습니다!");
            return;
        }
        
        // 데미지 처리 - 추가 안전장치 적용
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            if (enemyCollider == null) continue; // null 체크 추가
            
            var enemyHealth = enemyCollider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // 거리 계산 - 안전한 방법
                float distance = Vector3.Distance(transform.position, enemyCollider.transform.position);
                float normalizedDistance = distance / shockwaveRadius;
                
                // 거리별 데미지 계산
                float baseDamage = skillData != null ? skillData.damage : 15f;
                float damageMultiplier = damageFalloff.Evaluate(normalizedDistance);
                float finalDamage = baseDamage * damageMultiplier;
                
                // Warrior 클래스의 데미지 배율 적용 - 안전한 방법
                var warrior = GetComponent<Warrior>();
                if (warrior != null)
                {
                    try
                    {
                        finalDamage = warrior.GetModifiedDamage(finalDamage);
                    }
                    catch (System.Exception ex)
                    {
                        if (showDebugLogs)
                            Debug.LogWarning($"🟡 [WarriorSkill2] Warrior 데미지 배율 적용 실패: {ex.Message}");
                    }
                }
                
                // 데미지 적용
                enemyHealth.TakeDamage(Mathf.RoundToInt(finalDamage));
                
                // 넉백 효과 적용
                ApplyKnockback(enemyCollider.transform, distance);
                
                if (showDebugLogs)
                    Debug.Log($"💥 [WarriorSkill2] {enemyCollider.name}에게 {finalDamage:F1} 데미지! (거리: {distance:F1})");
            }
        }
    }
    
    private void ApplyKnockback(Transform enemy, float distance)
    {
        // 넉백 방향 계산
        Vector3 knockbackDirection = (enemy.position - transform.position).normalized;
        
        // 거리별 넉백 강도 계산 (가까울수록 더 강함)
        float normalizedDistance = distance / shockwaveRadius;
        float knockbackMultiplier = Mathf.Lerp(1f, 0.3f, normalizedDistance);
        float finalKnockbackForce = knockbackForce * knockbackMultiplier;
        
        // 적의 Rigidbody2D 찾기
        var enemyRigidbody = enemy.GetComponent<Rigidbody2D>();
        if (enemyRigidbody != null)
        {
            enemyRigidbody.AddForce(knockbackDirection * finalKnockbackForce, ForceMode2D.Impulse);
            
            if (showDebugLogs)
                Debug.Log($"🌪️ [WarriorSkill2] {enemy.name}에게 넉백 적용! 힘: {finalKnockbackForce:F1}");
        }
    }
    
    #endregion
    
    #region 유틸리티 메서드
    
    /// <summary>
    /// 충격파 범위 내 적의 수 반환
    /// </summary>
    public int GetEnemiesInRange()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius, LayerMask.GetMask("Enemy"));
        return enemies.Length;
    }
    
    #endregion
    
    #region 디버그
    
    private void OnDrawGizmosSelected()
    {
        // 충격파 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
        
        // 최대 데미지 범위 (30%) 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shockwaveRadius * 0.3f);
    }
    
    #endregion
} 