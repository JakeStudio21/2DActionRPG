using UnityEngine;
using System.Collections;
using CueSystem; // ⭐ Warrior Skill 이펙트 시스템

/// <summary>
/// 워리어 스킬1: Dash Attack (돌진 공격)
/// Phase 1: ActiveSkillData 통합 설계로 변경
/// BaseSkill<T> 상속으로 공통 로직 재사용
/// </summary>
public class WarriorSkill1 : BaseSkill<ActiveSkillData>
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
        
        
        // ⭐ 1단계: Cast 이펙트를 제일 먼저 발동 (애니메이션 시작과 동시)
        EmitSkillCastCue();
        
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
            
        }
        
        // 현재 속도 초기화 (이동 관성 제거)
        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
            
        }
        
        // ⭐ Cast 이펙트는 OnAnimationEvent()에서 이미 발동됨
        
        // ⭐ 0.3초 딜레이 (차징 느낌)
        yield return new WaitForSeconds(0.3f);
        
        // ⭐ 2단계: AOE 이펙트 (돌진 경로 전체)
        EmitSkillAOECue();
        
        // ⭐ AOE 비주얼 생성 (주황색 반투명 영역)
        SpawnSkillAOE();
        
        // 돌진 실행 (돌진 중 적 감지 및 Hit 이펙트 포함)
        yield return StartCoroutine(PerformDash());
        
        // ⭐ 핵심 수정: PlayerController 재활성화
        if (playerController != null)
        {
            playerController.enabled = wasPlayerControllerEnabled; // 원래 상태로 복원
            
        }
        
        isExecuting = false;
        
    }
    
    /// <summary>
    /// 돌진 실행
    /// </summary>
    private IEnumerator PerformDash()
    {
        Vector3 targetPosition;
        
        // ⭐ 수정: 적이 있더라도 조이스틱 방향 우선 (Telegraph/DamageArea와 일치)
        Vector2 dashDirection = GetCurrentAttackDirection();
        Vector3 direction = dashDirection.normalized;
        
        targetPosition = transform.position + direction * SkillData.dashRange;
        
        
        // ⭐ 추가 로깅: 돌진 정보 출력
        
        // 돌진 애니메이션
        float dashTime = SkillData.dashRange / SkillData.dashSpeed;
        float elapsedTime = 0f;
        
        // ⭐ 돌진 중 맞은 적 추적 (중복 데미지 방지)
        System.Collections.Generic.HashSet<Collider2D> hitEnemies = new System.Collections.Generic.HashSet<Collider2D>();
        
        while (elapsedTime < dashTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / dashTime;
            
            Vector3 currentPos = Vector3.Lerp(originalPosition, targetPosition, progress);
            playerRigidbody.MovePosition(currentPos);
            
            // ⭐ 3단계: 적 감지는 PlayerSkillAOEDamage가 자동 처리
            
            // 중간 진행상황 로깅 제거 (불필요)
            
            yield return null;
        }
        
        // 최종 위치 보정
        playerRigidbody.MovePosition(targetPosition);
        
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
            Dbg.Log($"⚔️ [WarriorSkill1] 초기화 완료 - " +
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
                
                return lastAttackDirection;
            }
        }
        
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
    
    /// <summary>
    /// ⭐ Phase 3: 스킬 AOE 생성 (직사각형) - DamageArea 시스템 사용
    /// </summary>
    private void SpawnSkillAOE()
    {
        if (!IsSkillDataValid) return;
        
        // 방향 결정 (마지막 공격 방향 사용)
        Vector2 direction = lastAttackDirection.magnitude > 0.1f ? lastAttackDirection : Vector2.right;
        
        // DamageArea 기반 AOE 생성
        SpawnSkillAOEAtPosition(transform.position, direction);
        
        // DamageArea 자체가 로그를 출력하므로 생략
    }
    
    /// <summary>
    /// ⭐ DamageArea 기반 AOE 생성 (AssasinSkill2 방식)
    /// </summary>
    private void SpawnSkillAOEAtPosition(Vector3 impactPosition, Vector2 direction)
    {
        if (!IsSkillDataValid) return;
        
        // DamageArea GameObject 생성
        GameObject damageAreaObj = new GameObject($"WarriorSkill1_DamageArea_{Time.time:F2}");
        damageAreaObj.transform.position = impactPosition;
        
        // DamageArea 컴포넌트 추가
        DamageArea damageArea = damageAreaObj.AddComponent<DamageArea>();
        
        // ⭐ 선택적 Telegraph 생성 (telegraphPrefab이 있을 때만)
        if (SkillData.telegraphPrefab != null)
        {
            GameObject telegraphObj = Instantiate(
                SkillData.telegraphPrefab,
                impactPosition,
                Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg)
            );
            
            TelegraphIndicator telegraph = telegraphObj.GetComponent<TelegraphIndicator>();
            if (telegraph != null)
            {
                telegraph.InitializeForPlayer(
                    shape: ConvertToAOEShapeType(SkillData.aoeShape),
                    position: impactPosition,
                    radius: SkillData.aoeRadius,
                    size: SkillData.aoeSize,
                    angle: SkillData.aoeFanAngle,
                    displayDuration: SkillData.telegraphDuration,
                    scaleMultiplier: 1.0f,
                    casterType: AOECasterType.Player,
                    forward: direction  // ⭐ 추가: Forward 방향 전달
                );
                
            }
            
            // Telegraph 표시 시간만큼 DamageArea 실행 지연
            StartCoroutine(DelayedDamageArea(damageArea, impactPosition, direction, SkillData.telegraphDuration));
        }
        else
        {
            // Telegraph 없으면 즉시 실행
            InitializeDamageArea(damageArea, impactPosition, direction);
        }
    }
    
    /// <summary>
    /// Telegraph 표시 후 DamageArea 실행
    /// </summary>
    private IEnumerator DelayedDamageArea(DamageArea damageArea, Vector3 impactPosition, Vector2 direction, float delay)
    {
        yield return new WaitForSeconds(delay);
        InitializeDamageArea(damageArea, impactPosition, direction);
    }
    
    /// <summary>
    /// DamageArea 초기화 및 실행
    /// </summary>
    private void InitializeDamageArea(DamageArea damageArea, Vector3 impactPosition, Vector2 direction)
    {
        damageArea.InitializeForPlayer(
            shape: ConvertToAOEShapeType(SkillData.aoeShape),
            origin: impactPosition,
            forward: direction,
            radius: SkillData.aoeRadius,
            size: SkillData.aoeSize,
            angle: SkillData.aoeFanAngle,
            playerBaseDamage: Mathf.RoundToInt(BaseDamage),
            damageMultiplier: 1.0f,
            scaleMultiplier: 1.0f,
            policy: AOEDamagePolicy.Once,
            hitCueKey: SkillData.hitCueKey
        );
        
        // DamageArea GameObject는 돌진 지속시간 + 0.5초 후 자동 삭제
        Destroy(damageArea.gameObject, SkillData.aoeDuration + 0.5f);
    }
    
    /// <summary>
    /// SkillAOEShape → AOEShapeType 변환 헬퍼
    /// </summary>
    private AOEShapeType ConvertToAOEShapeType(SkillAOEShape skillShape)
    {
        switch (skillShape)
        {
            case SkillAOEShape.Circle:
                return AOEShapeType.Circle;
            case SkillAOEShape.Rectangle:
                return AOEShapeType.Rectangle;
            case SkillAOEShape.Fan:
                return AOEShapeType.Triangle; // Fan은 Triangle로 매핑 (부채꼴)
            case SkillAOEShape.Line:
                return AOEShapeType.Rectangle; // Line은 Rectangle로 매핑
            default:
                return AOEShapeType.Circle;
        }
    }
    
    #region ⭐ 스킬 이펙트 Cue 시스템 (Cast → AOE → Hit)
    
    /// <summary>
    /// 1단계: 스킬 시전 이펙트 (Cast)
    /// </summary>
    private void EmitSkillCastCue()
    {
        // 방향 결정
        Vector2 direction = lastAttackDirection.magnitude > 0.1f ? lastAttackDirection : Vector2.right;
        
        // 각도 계산 (회전만 사용)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        
        var context = new CueContext
        {
            position = transform.position,
            rotation = rotation,
            actorType = ActorType.Player,
            magnitude = 1.5f,
            surfaceType = SurfaceType.Default,
            facingDir = direction,
            follow = transform
        };
        
        bool cueSuccess = CueEmitter.Emit("skill.warrior.skill1.cast", "Player", context);
        
    }
    
    /// <summary>
    /// 2단계: AOE 범위 이펙트 (돌진 경로 전체)
    /// </summary>
    private void EmitSkillAOECue()
    {
        // 방향 결정
        Vector2 direction = lastAttackDirection.magnitude > 0.1f ? lastAttackDirection : Vector2.right;
        
        // 각도 계산 (회전만 사용)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        
        var context = new CueContext
        {
            position = transform.position, // 돌진 시작 위치
            rotation = rotation,
            actorType = ActorType.Player,
            magnitude = 2.0f, // 돌진 강도
            surfaceType = SurfaceType.Default,
            facingDir = direction,
            scale = 1.0f  // ⭐ 명시적 선언 (향후 GetSkillLevelScale()로 변경 가능)
        };
        
        bool cueSuccess = CueEmitter.Emit("skill.warrior.skill1.aoe", "Player", context);
        
    }
    
    #endregion
}