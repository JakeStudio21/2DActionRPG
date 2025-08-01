using UnityEngine;
using System.Collections;

/// <summary>
/// 워리어 스킬2: Ground Slam (땅을 내리쳐 원형 충격파 발생)
/// WarriorSkillData 타입만 허용하는 타입 안전 스킬
/// BaseSkill<T> 상속으로 공통 로직 재사용
/// </summary>
public class WarriorSkill2 : BaseSkill<WarriorSkillData>
{
    #region 내부 상태
    
    private bool isExecuting = false;
    private GameObject currentChargeEffect;
    private AudioSource audioSource;
    
    #endregion
    
    #region BaseSkill<T> 구현
    
    /// <summary>
    /// 스킬 실행 시 호출 (Ground Slam 시작)
    /// </summary>
    protected override void OnExecuteSkill()
    {
        if (showDebugLogs)
            Debug.Log($"💥 [WarriorSkill2] {SkillName} 실행 시작");
            
        // 애니메이션 트리거
        if (animationController != null)
        {
            animationController.TriggerSkill2();
        }
        else
        {
            // 애니메이션 없이 직접 실행
            OnAnimationEvent();
        }
    }
    
    /// <summary>
    /// Animation Event에서 호출되는 실제 충격파 공격
    /// </summary>
    public override void OnAnimationEvent()
    {
        Debug.Log($"💥 [WarriorSkill2] Animation Event 호출됨!");
        Debug.Log($"   - SkillData 유효성: {IsSkillDataValid}");
        Debug.Log($"   - 현재 시간: {Time.time:F2}");
        Debug.Log($"   - 쿨다운 남은 시간: {GetCooldownRemaining():F2}");
        Debug.Log($"   - isExecuting: {isExecuting}");
        
        if (!IsSkillDataValid)
        {
            Debug.LogError("❌ [WarriorSkill2] SkillData가 유효하지 않습니다!");
            return;
        }
        
        if (isExecuting)
        {
            Debug.LogWarning("🟡 [WarriorSkill2] 이미 스킬 실행 중입니다!");
            return;
        }
        
        Debug.Log($"💥 [WarriorSkill2] Ground Slam 시작");
        Debug.Log($"   - 기본 데미지: {BaseDamage}");
        Debug.Log($"   - 공격 범위: {SkillData.attackRadius}");
        Debug.Log($"   - 기절 시간: {SkillData.stunDuration}");
        
        // 실행 중 플래그 설정
        isExecuting = true;
        
        // 충격파 공격 시퀀스 시작
        StartCoroutine(GroundSlamSequence());
    }
    
    /// <summary>
    /// 스킬 애니메이션 트리거
    /// </summary>
    protected override void TriggerSkillAnimation()
    {
        if (animationController != null)
        {
            animationController.TriggerSkill2();
        }
    }
    
    /// <summary>
    /// 추가 사용 조건 검사
    /// </summary>
    protected override bool CheckAdditionalConditions()
    {
        // 이미 실행 중인지 확인
        if (isExecuting)
        {
            if (showDebugLogs)
                Debug.Log("🟡 [WarriorSkill2] 스킬 실행 중입니다!");
            return false;
        }
        
        // AudioSource 초기화
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = GetComponentInParent<AudioSource>();
        }
        
        return true;
    }
    
    #endregion
    
    #region 워리어 Ground Slam 전용 로직
    
    /// <summary>
    /// 충격파 공격 전체 시퀀스
    /// </summary>
    private IEnumerator GroundSlamSequence()
    {
        isExecuting = true;
        
        if (showDebugLogs)
            Debug.Log("💥 [WarriorSkill2] 충격파 공격 시퀀스 시작!");
        
        // 1단계: 차징 단계 (0.8초)
        yield return StartCoroutine(ChargePhase());
        
        // 2단계: 내려치기 단계
        yield return StartCoroutine(SlamPhase());
        
        // 3단계: 충격파 확산 단계 (0.3초)
        yield return StartCoroutine(ShockwavePhase());
        
        if (showDebugLogs)
            Debug.Log("🟢 [WarriorSkill2] 충격파 공격 시퀀스 완료!");
        
        isExecuting = false;
    }
    
    /// <summary>
    /// 1단계: 차징 단계
    /// </summary>
    private IEnumerator ChargePhase()
    {
        float chargeTime = 0.8f; // WarriorSkillData에서 가져올 수 있도록 확장 가능
        
        if (showDebugLogs)
            Debug.Log($"⚡ [WarriorSkill2] 차징 시작 ({chargeTime}초)");
        
        // 차징 이펙트 생성
        if (SkillData.effectPrefab != null)
        {
            currentChargeEffect = GamePoolManager.Instance?.SpawnFromPool(
                SkillData.effectPrefab.name + "_Charge",
                transform.position,
                transform.rotation
            );
        }
        
        // 차징 사운드 재생
        PlaySound("Charge");
        
        // 차징 시간 대기
        yield return new WaitForSeconds(chargeTime);
        
        // 차징 이펙트 제거
        if (currentChargeEffect != null)
        {
            currentChargeEffect.SetActive(false);
            currentChargeEffect = null;
        }
        
        if (showDebugLogs)
            Debug.Log("⚡ [WarriorSkill2] 차징 완료");
    }
    
    /// <summary>
    /// 2단계: 내려치기 단계
    /// </summary>
    private IEnumerator SlamPhase()
    {
        if (showDebugLogs)
            Debug.Log("🔨 [WarriorSkill2] 내려치기 시작");
        
        // 내려치기 사운드 재생
        PlaySound("Slam");
        
        // 땅 균열 이펙트 생성
        if (SkillData.effectPrefab != null)
        {
            SpawnEffect(SkillData.effectPrefab, transform.position, transform.rotation);
        }
        
        // 내려치기 애니메이션 시간 대기
        yield return new WaitForSeconds(0.2f);
        
        if (showDebugLogs)
            Debug.Log("🔨 [WarriorSkill2] 내려치기 완료");
    }
    
    /// <summary>
    /// 3단계: 충격파 확산 단계
    /// </summary>
    private IEnumerator ShockwavePhase()
    {
        float shockwaveRadius = 5f; // WarriorSkillData에서 확장 가능
        float expandTime = 0.3f;
        
        if (showDebugLogs)
            Debug.Log($"💥 [WarriorSkill2] 충격파 확산 시작 (반경: {shockwaveRadius})");
        
        // 충격파 사운드 재생
        PlaySound("Shockwave");
        
        // 충격파 범위 내 적들 감지 및 데미지 적용
        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            transform.position, 
            shockwaveRadius, 
            LayerMask.GetMask("Enemy")
        );
        
        // 데미지 적용
        foreach (var enemy in enemies)
        {
            var enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // 거리별 데미지 감소 적용
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                float damageMultiplier = Mathf.Lerp(1f, 0.3f, distance / shockwaveRadius);
                int finalDamage = Mathf.RoundToInt(BaseDamage * damageMultiplier);
                
                enemyHealth.TakeDamage(finalDamage);
                
                // 넉백 효과 적용
                ApplyKnockback(enemy.transform, distance, shockwaveRadius);
                
                if (showDebugLogs)
                    Debug.Log($"💥 [WarriorSkill2] {enemy.name}에게 {finalDamage} 데미지! (거리: {distance:F1})");
            }
        }
        
        // 충격파 시각 효과 대기
        yield return new WaitForSeconds(expandTime);
        
        if (showDebugLogs)
            Debug.Log($"💥 [WarriorSkill2] 충격파 확산 완료 - {enemies.Length}명 타격");
    }
    
    /// <summary>
    /// 넉백 효과 적용
    /// </summary>
    private void ApplyKnockback(Transform target, float distance, float maxRange)
    {
        var knockback = target.GetComponent<Knockback>();
        if (knockback != null)
        {
            Vector2 direction = (target.position - transform.position).normalized;
            float knockbackForce = Mathf.Lerp(15f, 5f, distance / maxRange);
            
            knockback.GetKnockedBack(transform, knockbackForce);
        }
    }
    
    /// <summary>
    /// 사운드 재생
    /// </summary>
    private void PlaySound(string soundType)
    {
        if (audioSource != null && showDebugLogs)
        {
            Debug.Log($"🔊 [WarriorSkill2] {soundType} 사운드 재생");
            // 실제 AudioClip 재생은 나중에 오디오 시스템과 연동
        }
    }
    
    #endregion
    
    #region Unity 라이프사이클 확장
    
    protected override void Start()
    {
        base.Start(); // BaseSkill<T>의 초기화 실행
        
        // Ground Slam 스킬 전용 초기화
        if (IsSkillDataValid && showDebugLogs)
        {
            Debug.Log($"💥 [WarriorSkill2] 초기화 완료 - " +
                     $"충격파 데미지: {BaseDamage}, " +
                     $"쿨다운: {Cooldown}초");
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (SkillData != null)
        {
            // 충격파 범위 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 5f); // 기본 충격파 범위
        }
    }
    
    #endregion
}