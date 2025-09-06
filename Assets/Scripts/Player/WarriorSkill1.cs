using UnityEngine;
using System.Collections;

/// <summary>
/// 워리어 스킬1: Dash Attack (돌진 공격)
/// WarriorSkillData 타입만 허용하는 타입 안전 스킬
/// BaseSkill<T> 상속으로 공통 로직 재사용
/// </summary>
public class WarriorSkill1 : BaseSkill<WarriorSkillData>
{
    #region 내부 상태
    
    private bool isExecuting = false;
    private Vector3 originalPosition;
    private Transform nearestEnemy;
    private Rigidbody2D playerRigidbody;
    private AudioSource audioSource;
    
    [Header("⚔️ 돌진 방향 관리")]
    [SerializeField] private Vector2 lastAttackDirection = Vector2.right; // 마지막 공격 방향 기억
    [SerializeField] private bool showDirectionDebug = false; // 방향 디버그 로그 표시 (Inspector에서 조절 가능하도록 하려면 [SerializeField] 추가)
    
    // ⭐ 새로 추가: PlayerController 제어용
    private PlayerController playerController;
    private bool wasPlayerControllerEnabled = true; // 원래 상태 저장
    
    #endregion
    
    #region BaseSkill<T> 구현
    
    /// <summary>
    /// 스킬 실행 시 호출 (돌진 시작)
    /// </summary>
    protected override void OnExecuteSkill()
    {
        if (showDebugLogs)
            Debug.Log($"⚔️ [WarriorSkill1] {SkillName} 실행 시작");
            
        // 돌진할 적 찾기
        FindNearestEnemy();
        
        // 애니메이션 트리거
        if (animationController != null)
        {
            animationController.TriggerSkill1();
        }
        else
        {
            // 애니메이션 없이 직접 실행
            OnAnimationEvent();
        }
    }
    
    /// <summary>
    /// Animation Event에서 호출되는 실제 돌진 공격
    /// </summary>
    public override void OnAnimationEvent()
    {
        Debug.Log($"⚔️ [WarriorSkill1] Animation Event 호출됨!");
        Debug.Log($"   - SkillData 유효성: {IsSkillDataValid}");
        Debug.Log($"   - 현재 시간: {Time.time:F2}");
        Debug.Log($"   - 쿨다운 남은 시간: {GetCooldownRemaining():F2}");
        Debug.Log($"   - isExecuting: {isExecuting}");
        
        if (!IsSkillDataValid)
        {
            Debug.LogError("❌ [WarriorSkill1] SkillData가 유효하지 않습니다!");
            return;
        }
        
        if (isExecuting)
        {
            Debug.LogWarning("🟡 [WarriorSkill1] 이미 스킬 실행 중입니다!");
            return;
        }
        
        Debug.Log($"⚔️ [WarriorSkill1] 돌진 공격 시작");
        Debug.Log($"   - 돌진 속도: {SkillData.dashSpeed}");
        Debug.Log($"   - 돌진 범위: {SkillData.dashRange}");
        Debug.Log($"   - 공격 횟수: {SkillData.attackCount}");
        
        // 원래 위치 저장
        originalPosition = transform.position;
        
        // 실행 중 플래그 설정
        isExecuting = true;
        
        // 적 찾기 및 돌진 시작
        FindNearestEnemy();
        
        // ⭐ 수정: DashAttackSequence() → ExecuteDashAttackSequence()
        StartCoroutine(ExecuteDashAttackSequence());
    }
    
    /// <summary>
    /// 스킬 애니메이션 트리거
    /// </summary>
    protected override void TriggerSkillAnimation()
    {
        if (animationController != null)
        {
            animationController.TriggerSkill1();
        }
    }
    
    /// <summary>
    /// 추가 사용 조건 검사 (이동 중에도 사용 가능하도록 수정)
    /// </summary>
    protected override bool CheckAdditionalConditions()
    {
        // 이미 실행 중인지 확인 (가장 중요한 조건)
        if (isExecuting)
        {
            if (showDebugLogs)
                Debug.Log("🟡 [WarriorSkill1] 스킬 실행 중입니다!");
            return false;
        }
        
        // Rigidbody2D 초기화 (필수 컴포넌트)
        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponent<Rigidbody2D>();
            if (playerRigidbody == null)
                playerRigidbody = GetComponentInParent<Rigidbody2D>();
            
            if (playerRigidbody == null)
            {
                if (showDebugLogs)
                    Debug.LogError("🔴 [WarriorSkill1] Rigidbody2D를 찾을 수 없습니다!");
                return false;
            }
        }
        
        // ⭐ 새로 추가: PlayerController 초기화
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
            if (playerController == null)
                playerController = GetComponentInParent<PlayerController>();
        }
        
        if (showDebugLogs)
        {
            float currentSpeed = playerRigidbody.velocity.magnitude;
            Debug.Log($"🏃 [WarriorSkill1] 현재 이동 속도: {currentSpeed:F2} - 스킬 사용 허용!");
        }
        
        return true; // 실행 중이 아니고 Rigidbody2D가 있으면 항상 허용
    }
    
    #endregion
    
    #region 워리어 스킬 전용 로직
    
    /// <summary>
    /// 가장 가까운 적 찾기
    /// </summary>
    private void FindNearestEnemy()
    {
        float detectionRange = SkillData.dashRange;
        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            transform.position, 
            detectionRange, 
            LayerMask.GetMask("Enemy")
        );
        
        float closestDistance = float.MaxValue;
        nearestEnemy = null;
        
        foreach (var enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                nearestEnemy = enemy.transform;
            }
        }
        
        if (showDebugLogs)
        {
            if (nearestEnemy != null)
                Debug.Log($"🎯 [WarriorSkill1] 타겟 발견: {nearestEnemy.name} (거리: {closestDistance:F1})");
            else
                Debug.Log($"🎯 [WarriorSkill1] 범위 내 적 없음 (탐지 범위: {detectionRange})");
        }
    }
    
    /// <summary>
    /// 돌진 공격 시퀀스 실행
    /// </summary>
    private IEnumerator ExecuteDashAttackSequence()
    {
        isExecuting = true;
        originalPosition = transform.position;
        
        // ⭐ 핵심 수정: PlayerController 일시 비활성화
        if (playerController != null)
        {
            wasPlayerControllerEnabled = playerController.enabled;
            playerController.enabled = false; // 이동 입력 차단
            
            if (showDebugLogs)
                Debug.Log("🛑 [WarriorSkill1] PlayerController 일시 비활성화 - 돌진 중 이동 차단");
        }
        
        // 현재 속도 초기화 (이동 관성 제거)
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
            
            if (showDebugLogs)
                Debug.Log("🔄 [WarriorSkill1] 플레이어 속도 초기화");
        }
        
        // 돌진 이펙트 생성
        if (SkillData.dashEffectPrefab != null)
        {
            SpawnEffect(SkillData.dashEffectPrefab, transform.position, transform.rotation);
        }
        
        // 돌진 실행
        yield return StartCoroutine(PerformDash());
        
        // 연속 공격 실행
        yield return StartCoroutine(PerformComboAttacks());
        
        // ⭐ 핵심 수정: PlayerController 재활성화
        if (playerController != null)
        {
            playerController.enabled = wasPlayerControllerEnabled; // 원래 상태로 복원
            
            if (showDebugLogs)
                Debug.Log("✅ [WarriorSkill1] PlayerController 재활성화 - 정상 이동 복원");
        }
        
        isExecuting = false;
        
        if (showDebugLogs)
            Debug.Log($"⚔️ [WarriorSkill1] 돌진 공격 시퀀스 완료!");
    }
    
    /// <summary>
    /// 돌진 실행
    /// </summary>
    private IEnumerator PerformDash()
    {
        Vector3 targetPosition;
        
        if (nearestEnemy != null)
        {
            // 적이 있으면 적 방향으로 돌진
            Vector3 direction = (nearestEnemy.position - transform.position).normalized;
            float dashDistance = Mathf.Min(
                Vector3.Distance(transform.position, nearestEnemy.position), 
                SkillData.dashRange
            );
            targetPosition = transform.position + direction * dashDistance;
            
            if (showDebugLogs)
                Debug.Log($"⚔️ [WarriorSkill1] 적 방향으로 돌진: {direction} (거리: {dashDistance:F1})");
        }
        else
        {
            // ⭐ 개선: 마지막 조이스틱 방향 사용 (기본공격과 동일한 방식)
            Vector2 dashDirection = GetCurrentAttackDirection();
            Vector3 direction = dashDirection.normalized;
            
            targetPosition = transform.position + direction * SkillData.dashRange;
            
            if (showDebugLogs)
                Debug.Log($"⚔️ [WarriorSkill1] 저장된 방향으로 돌진: {direction} (적 없음)");
        }
        
        // ⭐ 추가 로깅: 돌진 정보 출력
        if (showDebugLogs)
        {
            Debug.Log($"🏃 [WarriorSkill1] 돌진 시작:");
            Debug.Log($"   - 시작 위치: {originalPosition}");
            Debug.Log($"   - 목표 위치: {targetPosition}");
            Debug.Log($"   - 이동 거리: {Vector3.Distance(originalPosition, targetPosition):F1}");
            Debug.Log($"   - 돌진 시간: {SkillData.dashRange / SkillData.dashSpeed:F2}초");
        }
        
        // 돌진 애니메이션
        float dashTime = SkillData.dashRange / SkillData.dashSpeed;
        float elapsedTime = 0f;
        
        while (elapsedTime < dashTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / dashTime;
            
            Vector3 currentPos = Vector3.Lerp(originalPosition, targetPosition, progress);
            playerRigidbody.MovePosition(currentPos);
            
            // ⭐ 중간 진행상황 로깅 (5번만)
            if (showDebugLogs && (int)(progress * 5) > (int)((progress - Time.deltaTime / dashTime) * 5))
            {
                Debug.Log($"🏃 [WarriorSkill1] 돌진 진행: {progress * 100:F0}% (현재 위치: {currentPos})");
            }
            
            yield return null;
        }
        
        // 최종 위치 보정
        playerRigidbody.MovePosition(targetPosition);
        
        if (showDebugLogs)
        {
            float actualDistance = Vector3.Distance(originalPosition, transform.position);
            Debug.Log($"🏃 [WarriorSkill1] 돌진 완료 - 실제 이동 거리: {actualDistance:F1}");
        }
    }
    
    /// <summary>
    /// 연속 공격 실행
    /// </summary>
    private IEnumerator PerformComboAttacks()
    {
        int attackCount = SkillData.attackCount;
        float attackDelay = SkillData.attackDelay;
        float attackRadius = SkillData.attackRadius;
        
        for (int i = 0; i < attackCount; i++)
        {
            // 공격 범위 내 적들 탐지
            Collider2D[] enemies = Physics2D.OverlapCircleAll(
                transform.position, 
                attackRadius, 
                LayerMask.GetMask("Enemy")
            );
            
            // 데미지 적용
            foreach (var enemy in enemies)
            {
                var enemyHealth = enemy.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    // EnemyHealth.TakeDamage는 int 하나만 받음
                    enemyHealth.TakeDamage((int)BaseDamage);
                    
                    if (showDebugLogs)
                        Debug.Log($"⚔️ [WarriorSkill1] {enemy.name}에게 {BaseDamage} 데미지!");
                }
            }
            
            // 베기 이펙트 생성
            if (SkillData.slashEffectPrefab != null)
            {
                SpawnEffect(SkillData.slashEffectPrefab, transform.position, transform.rotation);
            }
            
            // 다음 공격까지 대기 (마지막 공격이 아닌 경우)
            if (i < attackCount - 1)
            {
                yield return new WaitForSeconds(attackDelay);
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"⚔️ [WarriorSkill1] {attackCount}연속 공격 완료!");
    }
    
    #endregion
    
    #region Unity 라이프사이클 확장
    
    protected override void Start()
    {
        base.Start(); // BaseSkill<T>의 초기화 실행
        
        // ⭐ 마지막 방향 지속적 업데이트 시작
        StartCoroutine(UpdateLastAttackDirectionCoroutine());
        
        // 워리어 스킬 전용 초기화
        if (IsSkillDataValid && showDebugLogs)
        {
            Debug.Log($"⚔️ [WarriorSkill1] 초기화 완료 - " +
                     $"돌진 속도: {SkillData.dashSpeed}, " +
                     $"돌진 거리: {SkillData.dashRange}, " +
                     $"연속 공격: {SkillData.attackCount}회");
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (SkillData != null)
        {
            // 돌진 범위 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, SkillData.dashRange);
            
            // 공격 범위 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, SkillData.attackRadius);
        }
    }
    
    #endregion

    /// <summary>
    /// 현재 조이스틱 공격 방향 가져오기 (마지막 방향 기억 기능 포함)
    /// </summary>
    private Vector2 GetCurrentAttackDirection()
    {
        // ActiveWeapon에서 AttackJoystickInput 참조 가져오기
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon != null && activeWeapon.attackJoystickInput != null)
        {
            Vector2 joystickDir = activeWeapon.attackJoystickInput.GetAttackDirection();
            
            // ⭐ 핵심 수정: 조이스틱 입력이 있을 때만 마지막 방향 업데이트
            if (joystickDir.magnitude > 0.1f)
            {
                lastAttackDirection = joystickDir.normalized;
                
                if (showDirectionDebug)
                    Debug.Log($"🎮 [WarriorSkill1] 새로운 조이스틱 방향 저장: {lastAttackDirection}");
                
                return lastAttackDirection;
            }
        }
        
        // ⭐ 핵심: 조이스틱 입력이 없으면 마지막 방향 사용
        if (showDirectionDebug)
            Debug.Log($"🎮 [WarriorSkill1] 마지막 저장된 방향 사용: {lastAttackDirection}");
        
        return lastAttackDirection;
    }

    /// <summary>
    /// 백그라운드에서 마지막 공격 방향 지속적 업데이트 (기본공격과 동일한 방식)
    /// </summary>
    private IEnumerator UpdateLastAttackDirectionCoroutine()
    {
        while (true)
        {
            // ActiveWeapon에서 현재 조이스틱 방향 체크
            var activeWeapon = FindObjectOfType<ActiveWeapon>();
            if (activeWeapon != null && activeWeapon.attackJoystickInput != null)
            {
                Vector2 joystickDir = activeWeapon.attackJoystickInput.GetAttackDirection();
                
                // 조이스틱 입력이 있으면 마지막 방향 업데이트
                if (joystickDir.magnitude > 0.1f)
                {
                    lastAttackDirection = joystickDir.normalized;
                }
            }
            
            // 60FPS로 업데이트 (기본공격과 동일한 빈도)
            yield return new WaitForSeconds(1f / 60f);
        }
    }
}