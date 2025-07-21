using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Added for List

/// <summary>
/// Warrior 스킬1: Dash Attack (적을 향해 돌진하며 3연속베기)
/// ISkill 인터페이스를 구현하여 모듈식 스킬 시스템에 통합
/// ScriptableObject 기반 데이터 분리 적용
/// </summary>
public class WarriorSkill1 : MonoBehaviour, ISkill
{
    [Header("스킬 데이터 (ScriptableObject)")]
    public SkillData skillData; // Inspector에서 할당
    
    [Header("돌진 설정")]
    [SerializeField] private float dashSpeed = 20f;          // 돌진 속도
    [SerializeField] private float dashRange = 8f;           // 돌진 최대 거리
    [SerializeField] private float attackDelay = 0.3f;       // 각 공격 간 딜레이
    [SerializeField] private int attackCount = 3;            // 연속 공격 횟수
    [SerializeField] private float attackRange = 2f;         // 공격 범위
    
    [Header("이펙트")]
    [SerializeField] private GameObject dashEffectPrefab;    // 돌진 이펙트
    [SerializeField] private GameObject slashEffectPrefab;   // 베기 이펙트
    [SerializeField] private string dashEffectPoolName = "DashEffect";
    [SerializeField] private string slashEffectPoolName = "SlashEffect";
    
    [Header("디버그")]
    public bool showDebugLogs = true;
    
    // ISkill 인터페이스 구현
    public string SkillName => skillData != null ? skillData.skillName : "Dash Attack";
    public float Cooldown => skillData != null ? skillData.cooldown : 5f;
    
    // 내부 상태
    private float lastSkillTime = -Mathf.Infinity;
    private bool isExecuting = false;
    private Transform nearestEnemy;
    private Vector3 originalPosition;
    private PlayerController playerController;
    private PlayerAnimationController animationController;
    private Rigidbody2D playerRigidbody;
    
    // 이벤트
    public System.Action<float> OnSkillCooldownChanged;
    
    void Awake()
    {
        if (showDebugLogs)
            Debug.Log("🟢 [WarriorSkill1] Awake() 시작");
            
        // 컴포넌트 참조 초기화
        playerController = GetComponent<PlayerController>();
        animationController = GetComponent<PlayerAnimationController>();
        playerRigidbody = GetComponent<Rigidbody2D>();
        
        if (playerController == null)
            Debug.LogError("🔴 [WarriorSkill1] PlayerController를 찾을 수 없습니다!");
        if (animationController == null)
            Debug.LogWarning("🟡 [WarriorSkill1] PlayerAnimationController를 찾을 수 없습니다!");
        if (playerRigidbody == null)
            Debug.LogError("🔴 [WarriorSkill1] Rigidbody2D를 찾을 수 없습니다!");
    }
    
    void Start()
    {
        // SkillData 검증
        if (skillData == null)
        {
            Debug.LogWarning("🟡 [WarriorSkill1] SkillData가 할당되지 않았습니다. 기본값을 사용합니다.");
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"🟢 [WarriorSkill1] SkillData 로딩 완료: {skillData.skillName}");
        }
    }
    
    #region ISkill 인터페이스 구현
    
    public bool CanUse()
    {
        if (isExecuting)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [WarriorSkill1] 스킬 실행 중입니다!");
            return false;
        }
        
        float remainingCooldown = GetCooldownRemaining();
        bool canUse = remainingCooldown <= 0f;
        
        if (!canUse && showDebugLogs)
            Debug.LogWarning($"🟡 [WarriorSkill1] 쿨다운 중! 남은 시간: {remainingCooldown:F1}초");
            
        return canUse;
    }
    
    public void Execute()
    {
        if (showDebugLogs)
            Debug.Log("🔵 [WarriorSkill1] Execute() 호출됨");
            
        if (!CanUse())
        {
            Debug.LogWarning($"🟡 [WarriorSkill1] 스킬1 쿨다운 중! 남은 시간: {GetCooldownRemaining():F1}초");
            return;
        }
        
        // 쿨다운 시작
        lastSkillTime = Time.time;
        
        // 애니메이션 트리거 (PlayerAnimationController를 통해)
        if (animationController != null)
        {
            bool success = animationController.TriggerSkill1();
            if (success)
            {
                if (OnSkillCooldownChanged != null)
                    OnSkillCooldownChanged(Cooldown);
                    
                Debug.Log($"🟢 [WarriorSkill1] 스킬1(Dash Attack) 애니메이션 트리거 성공!");
            }
            else
            {
                Debug.LogWarning("🟡 [WarriorSkill1] 애니메이션 트리거 실패 - 직접 실행");
                OnAnimationEvent();
            }
        }
        else
        {
            // PlayerAnimationController가 없으면 직접 실행
            Debug.LogWarning("🟡 [WarriorSkill1] PlayerAnimationController가 없음 - 직접 실행");
            OnAnimationEvent();
        }
    }
    
    public void OnAnimationEvent()
    {
        if (showDebugLogs)
            Debug.Log("🔵 [WarriorSkill1] OnAnimationEvent() - 돌진 공격 시작!");
            
        if (isExecuting)
        {
            Debug.LogWarning("🟡 [WarriorSkill1] 이미 스킬이 실행 중입니다!");
            return;
        }
        
        StartCoroutine(DashAttackSequence());
    }
    
    public float GetCooldownRemaining()
    {
        float elapsed = Time.time - lastSkillTime;
        return Mathf.Max(0f, Cooldown - elapsed);
    }
    
    #endregion
    
    #region 돌진 공격 시퀀스
    
    private IEnumerator DashAttackSequence()
    {
        isExecuting = true;
        originalPosition = transform.position;
        
        if (showDebugLogs)
            Debug.Log("⚔️ [WarriorSkill1] 돌진 공격 시퀀스 시작!");
        
        // 1단계: 가장 가까운 적 찾기
        nearestEnemy = FindNearestEnemy();
        if (nearestEnemy == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [WarriorSkill1] 주변에 적이 없습니다!");
            isExecuting = false;
            yield break;
        }
        
        // 2단계: 돌진 실행
        yield return StartCoroutine(PerformDash());
        
        // 3단계: 3연속 공격 실행
        yield return StartCoroutine(PerformComboAttacks());
        
        if (showDebugLogs)
            Debug.Log("🟢 [WarriorSkill1] 돌진 공격 시퀀스 완료!");
        
        isExecuting = false;
    }
    
    private IEnumerator PerformDash()
    {
        if (nearestEnemy == null) yield break;
        
        Vector3 targetPosition = nearestEnemy.position;
        Vector3 dashDirection = (targetPosition - transform.position).normalized;
        
        // 돌진 거리 제한
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        float actualDashDistance = Mathf.Min(distanceToTarget - 1f, dashRange); // 적 바로 앞까지
        Vector3 dashTarget = transform.position + dashDirection * actualDashDistance;
        
        if (showDebugLogs)
            Debug.Log($"⚡ [WarriorSkill1] 돌진 시작! 목표: {dashTarget}");
        
        // 돌진 이펙트 생성
        if (dashEffectPrefab != null && GamePoolManager.Instance != null)
        {
            GamePoolManager.Instance.SpawnFromPool(dashEffectPoolName, transform.position, Quaternion.identity);
        }
        
        // 돌진 애니메이션 (Rigidbody2D 사용)
        float dashTime = actualDashDistance / dashSpeed;
        float elapsed = 0f;
        Vector3 startPosition = transform.position;
        
        while (elapsed < dashTime)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dashTime;
            
            Vector3 currentPosition = Vector3.Lerp(startPosition, dashTarget, progress);
            transform.position = currentPosition;
            
            yield return null;
        }
        
        transform.position = dashTarget;
        
        if (showDebugLogs)
            Debug.Log("🟢 [WarriorSkill1] 돌진 완료!");
    }
    
    private IEnumerator PerformComboAttacks()
    {
        for (int i = 0; i < attackCount; i++)
        {
            if (showDebugLogs)
                Debug.Log($"⚔️ [WarriorSkill1] {i + 1}번째 공격!");
            
            // 공격 실행
            PerformSingleAttack(i + 1);
            
            // 마지막 공격이 아니면 딜레이
            if (i < attackCount - 1)
            {
                yield return new WaitForSeconds(attackDelay);
            }
        }
    }
    
    private void PerformSingleAttack(int attackNumber)
    {
        // 베기 이펙트 생성
        if (slashEffectPrefab != null && GamePoolManager.Instance != null)
        {
            GamePoolManager.Instance.SpawnFromPool(slashEffectPoolName, transform.position, Quaternion.identity);
        }
        
        // 범위 내 적들에게 데미지 - 안전한 방법 사용
        Collider2D[] hitEnemies = null;
        
        try
        {
            // 1순위: Enemy 레이어로 탐지 시도
            hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange, LayerMask.GetMask("Enemy"));
            if (showDebugLogs)
                Debug.Log($"💥 [WarriorSkill1] Enemy 레이어로 {hitEnemies.Length}명의 적 감지!");
        }
        catch (System.Exception)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [WarriorSkill1] Enemy 레이어가 정의되지 않음. 모든 콜라이더 검색...");
            hitEnemies = null;
        }
        
        // 2순위: 모든 콜라이더에서 EnemyHealth 컴포넌트 찾기
        if (hitEnemies == null || hitEnemies.Length == 0)
        {
            Collider2D[] allColliders = Physics2D.OverlapCircleAll(transform.position, attackRange);
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
                Debug.Log($"💥 [WarriorSkill1] EnemyHealth 컴포넌트로 {hitEnemies.Length}명의 적 감지!");
        }
        
        // 3순위: 마지막 안전장치 - null 체크
        if (hitEnemies == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [WarriorSkill1] 적을 찾을 수 없습니다!");
            return;
        }
        
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            if (enemyCollider == null) continue; // null 체크 추가
            
            var enemyHealth = enemyCollider.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                float damage = skillData != null ? skillData.damage : 10f;
                
                // Warrior 클래스의 데미지 배율 적용 - 안전한 방법
                var warrior = GetComponent<Warrior>();
                if (warrior != null)
                {
                    try
                    {
                        damage = warrior.GetModifiedDamage(damage);
                    }
                    catch (System.Exception ex)
                    {
                        if (showDebugLogs)
                            Debug.LogWarning($"🟡 [WarriorSkill1] Warrior 데미지 배율 적용 실패: {ex.Message}");
                    }
                }
                
                enemyHealth.TakeDamage(Mathf.RoundToInt(damage));
                
                if (showDebugLogs)
                    Debug.Log($"💥 [WarriorSkill1] {attackNumber}번째 공격으로 {enemyCollider.name}에게 {damage} 데미지!");
            }
        }
    }
    
    #endregion
    
    #region 유틸리티 메서드
    
    private Transform FindNearestEnemy()
    {
        GameObject[] enemies = null;
        
        try
        {
            // 1순위: Enemy 태그 사용 시도
            enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (showDebugLogs)
                Debug.Log($"🎯 [WarriorSkill1] Enemy 태그로 {enemies.Length}명의 적 발견");
        }
        catch (UnityException)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [WarriorSkill1] Enemy 태그가 정의되지 않음. 다른 방법 시도...");
            enemies = null;
        }
        
        // 2순위: Enemy 태그가 없으면 EnemyHealth 컴포넌트로 찾기
        if (enemies == null || enemies.Length == 0)
        {
            EnemyHealth[] enemyHealths = FindObjectsOfType<EnemyHealth>();
            enemies = new GameObject[enemyHealths.Length];
            for (int i = 0; i < enemyHealths.Length; i++)
            {
                enemies[i] = enemyHealths[i].gameObject;
            }
            
            if (showDebugLogs)
                Debug.Log($"🎯 [WarriorSkill1] EnemyHealth 컴포넌트로 {enemies.Length}명의 적 발견");
        }
        
        if (enemies == null || enemies.Length == 0)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [WarriorSkill1] 적을 찾을 수 없습니다!");
            return null;
        }
        
        Transform nearest = null;
        float minDistance = Mathf.Infinity;
        
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue; // null 체크 추가
            
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < minDistance && distance <= dashRange)
            {
                minDistance = distance;
                nearest = enemy.transform;
            }
        }
        
        if (showDebugLogs && nearest != null)
            Debug.Log($"🎯 [WarriorSkill1] 가장 가까운 적: {nearest.name} (거리: {minDistance:F1})");
        else if (showDebugLogs)
            Debug.LogWarning($"🟡 [WarriorSkill1] 돌진 범위({dashRange}m) 내에 적이 없습니다.");
        
        return nearest;
    }
    
    #endregion
    
    #region 디버그
    
    private void OnDrawGizmosSelected()
    {
        // 돌진 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, dashRange);
        
        // 공격 범위 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    
    #endregion
} 