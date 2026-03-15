using UnityEngine;
using System.Collections;
using CueSystem; // ⭐ Warrior Skill 이펙트 시스템

/// <summary>
/// 워리어 스킬2: Ground Slam (땅을 내리쳐 원형 충격파 발생)
/// Phase 1: ActiveSkillData 통합 설계로 변경
/// BaseSkill<T> 상속으로 공통 로직 재사용
/// </summary>
public class WarriorSkill2 : BaseSkill<ActiveSkillData>
{
    #region 내부 상태
    
    private bool isExecuting = false;
    private GameObject currentChargeEffect;
    private AudioSource audioSource;
    
    [Header("⚔️ 공격 방향 관리")]
    [SerializeField] private Vector2 lastAttackDirection = Vector2.right; // 마지막 공격 방향 기억
    [SerializeField] private bool showDirectionDebug = false; // 방향 디버그 로그
    
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
        
        // ⭐ 1단계: Cast 이펙트를 제일 먼저 발동 (애니메이션 시작과 동시)
        EmitSkillCastCue();
        
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
    
    /// <summary>
    /// 현재 조이스틱 공격 방향 가져오기 (WarriorSkill1과 동일한 로직)
    /// </summary>
    private Vector2 GetCurrentAttackDirection()
    {
        if (showDebugLogs)
            Debug.Log($"💥 [WarriorSkill2] GetCurrentAttackDirection 호출됨 - 시간: {Time.time:F3}");
        
        // ActiveWeapon에서 AttackJoystickInput 참조 가져오기
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon != null && activeWeapon.attackJoystickInput != null)
        {
            Vector2 joystickDir = activeWeapon.attackJoystickInput.GetAttackDirection();
            
            if (joystickDir.magnitude > 0.1f)
            {
                // ⭐ 마지막 방향 저장
                lastAttackDirection = joystickDir.normalized;
                
                // 🔍 N/S 방향 특별 확인
                bool isNorthSouth = Mathf.Abs(joystickDir.x) < 0.3f && Mathf.Abs(joystickDir.y) > 0.7f;
                if (isNorthSouth)
                {
                    string directionName = joystickDir.y > 0 ? "NORTH" : "SOUTH";
                    Debug.Log($"🧭 [WarriorSkill2] {directionName} 방향 스킬2 사용! 실시간 조이스틱: {joystickDir}");
                }
                
                if (showDirectionDebug)
                    Debug.Log($"🎮 [WarriorSkill2] 새로운 조이스틱 방향 저장: {lastAttackDirection}");
                
                return lastAttackDirection;
            }
        }
        
        // ⭐ 핵심: 조이스틱 입력이 없으면 마지막 방향 사용
        if (showDirectionDebug)
            Debug.Log($"🎮 [WarriorSkill2] 마지막 저장된 방향 사용: {lastAttackDirection}");
        
        return lastAttackDirection;
    }
    
    /// <summary>
    /// 백그라운드에서 마지막 공격 방향 지속적 업데이트 (WarriorSkill1과 동일)
    /// </summary>
    private IEnumerator UpdateLastAttackDirectionCoroutine()
    {
        while (true)
        {
            var activeWeapon = FindObjectOfType<ActiveWeapon>();
            if (activeWeapon != null && activeWeapon.attackJoystickInput != null)
            {
                Vector2 joystickDir = activeWeapon.attackJoystickInput.GetAttackDirection();
                
                if (joystickDir.magnitude > 0.1f)
                {
                    lastAttackDirection = joystickDir.normalized;
                }
            }
            
            // 60FPS로 업데이트 (기본공격과 동일한 빈도)
            yield return new WaitForSeconds(1f / 60f);
        }
    }
    
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
        float chargeTime = 0.3f; // ⭐ 0.8초 → 0.3초로 변경 (0.5초 빠르게)
        
        if (showDebugLogs)
            Debug.Log($"⚡ [WarriorSkill2] 차징 시작 ({chargeTime}초)");
        
        // 차징 이펙트 — castCueKey 기반 CueSystem 경유
        if (!string.IsNullOrEmpty(SkillData.castCueKey))
        {
            var ctx = new CueSystem.CueContext
            {
                position = transform.position,
                rotation = transform.rotation,
                actorType = ActorType.Player,
                magnitude = 1.5f,
                follow = transform
            };
            CueSystem.CueEmitter.Emit(SkillData.castCueKey, "Player", ctx);
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
        
        // ⭐ Cast 이펙트는 OnAnimationEvent()에서 이미 발동됨
        
        // 내려치기 사운드 재생
        PlaySound("Slam");
        
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
        
        // ⭐ 2단계: AOE 이펙트 (충격파 확산)
        EmitSkillAOECue();
        
        // ⭐ Phase 3: AOE 생성 (원형) - PlayerSkillAOEDamage가 자동으로 데미지 + Hit Cue 처리
        SpawnSkillAOE();
        
        // 충격파 시각 효과 대기
        yield return new WaitForSeconds(expandTime);
        
        if (showDebugLogs)
            Debug.Log($"💥 [WarriorSkill2] 충격파 확산 완료");
    }
    
    /// <summary>
    /// ⭐ Phase 3: 스킬 AOE 생성 (원형) - DamageArea 시스템 사용
    /// </summary>
    private void SpawnSkillAOE()
    {
        if (!IsSkillDataValid) return;
        
        // ⭐ 실시간 조이스틱 방향 가져오기 (AssasinSkill2와 동일)
        Vector2 direction = GetCurrentAttackDirection();
        
        // DamageArea 기반 AOE 생성
        SpawnSkillAOEAtPosition(transform.position, direction);
        
        if (showDebugLogs)
            Debug.Log($"💥 [WarriorSkill2] DamageArea 생성: Circle, 반경: {SkillData.aoeRadius}, 방향: {direction}, 위치: {transform.position}, 데미지: {BaseDamage}");
    }
    
    /// <summary>
    /// ⭐ DamageArea 기반 AOE 생성 (AssasinSkill2 방식)
    /// </summary>
    private void SpawnSkillAOEAtPosition(Vector3 impactPosition, Vector2 direction)
    {
        if (!IsSkillDataValid) return;
        
        // DamageArea GameObject 생성
        GameObject damageAreaObj = new GameObject($"WarriorSkill2_DamageArea_{Time.time:F2}");
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
                // ⭐ 디버그: 크기 값 확인
                Debug.Log($"🔍 [WarriorSkill2] Telegraph 초기화 전: Shape={SkillData.aoeShape}, Radius={SkillData.aoeRadius}, Size={SkillData.aoeSize}");
                
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
                
                // Telegraph 생성 로그 제거 (DamageArea가 출력)
            }
            else
            {
                Debug.LogError($"❌ [WarriorSkill2] TelegraphIndicator 컴포넌트를 찾을 수 없음!");
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
            hitEffectPrefab: null
        );
        
        // DamageArea GameObject는 0.5초 후 자동 삭제
        Destroy(damageArea.gameObject, 0.5f);
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
    
    #region ⭐ 스킬 이펙트 Cue 시스템 (Cast → AOE → Hit)
    
    /// <summary>
    /// 1단계: 스킬 시전 이펙트 (Cast) - 땅 내리치기
    /// </summary>
    private void EmitSkillCastCue()
    {
        // 땅 내리치기는 방향 없음 (회전 없음)
        Quaternion rotation = Quaternion.identity;
        
        var context = new CueContext
        {
            position = transform.position,
            rotation = rotation,
            actorType = ActorType.Player,
            magnitude = 2.0f, // 강력한 시전 이펙트
            surfaceType = SurfaceType.Stone, // 땅 내리치기
            facingDir = Vector2.down,
            follow = transform
        };
        
        bool cueSuccess = CueEmitter.Emit("skill.warrior.skill2.cast", "Player", context);
        
        if (showDebugLogs)
            Debug.Log($"💥 [WarriorSkill2] Cast Cue 발행 (땅 내리치기) → {cueSuccess}");
    }
    
    /// <summary>
    /// 2단계: AOE 범위 이펙트 (충격파 확산)
    /// </summary>
    private void EmitSkillAOECue()
    {
        // ⭐ 조이스틱 방향 가져오기 (이펙트 회전용)
        Vector2 direction = GetCurrentAttackDirection();
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        var context = new CueContext
        {
            position = transform.position,
            rotation = Quaternion.Euler(0, 0, angle), // ⭐ 조이스틱 방향 적용
            actorType = ActorType.Player,
            magnitude = 2.5f, // 매우 강력한 범위 이펙트
            surfaceType = SurfaceType.Stone,
            facingDir = direction, // ⭐ 방향 정보 추가
            scale = 1.0f  // ⭐ 명시적 선언 (향후 GetSkillLevelScale()로 변경 가능)
        };
        
        bool cueSuccess = CueEmitter.Emit("skill.warrior.skill2.aoe", "Player", context);
        
        if (showDebugLogs)
            Debug.Log($"💥 [WarriorSkill2] AOE Cue 발행 (충격파 확산) → {cueSuccess}");
    }
    
    #endregion
    
    #region Unity 라이프사이클 확장
    
    protected override void Start()
    {
        base.Start(); // BaseSkill<T>의 초기화 실행
        
        // ⭐ 마지막 방향 지속적 업데이트 시작
        StartCoroutine(UpdateLastAttackDirectionCoroutine());
        
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