using UnityEngine;
using System.Collections;
using CueSystem; // ⭐ Assasin Skill 이펙트 시스템

/// <summary>
/// 어쌔신 스킬2: Power Arrow (강력한 단일 화살)
/// Phase 1: ActiveSkillData 통합 설계로 변경
/// BaseSkill<T> 상속으로 공통 로직 재사용
/// </summary>
public class AssasinSkill2 : BaseSkill<ActiveSkillData>
{
    // 🔧 추가: 마지막 공격 방향 저장용 필드
    private Vector2 lastAttackDirection = Vector2.right;
    
    #region BaseSkill<T> 구현
    
    /// <summary>
    /// 스킬 실행 시 호출 (애니메이션 트리거)
    /// </summary>
    protected override void OnExecuteSkill()
    {
        if (showDebugLogs)
            Debug.Log($"💥 [AssasinSkill2] {SkillName} 실행 시작");
            
        // 애니메이션 트리거
        if (animationController != null)
        {
            animationController.TriggerSkill2();
        }
        else
        {
            Debug.LogWarning("🟡 [AssasinSkill2] PlayerAnimationController가 없습니다!");
            // 애니메이션 없이 직접 실행
            OnAnimationEvent();
        }
    }
    
    /// <summary>
    /// Animation Event에서 호출되는 실제 Power Arrow 발사
    /// </summary>
    public override void OnAnimationEvent()
    {
        if (!IsSkillDataValid)
        {
            Debug.LogError("❌ [AssasinSkill2] SkillData가 유효하지 않습니다!");
            return;
        }
        
        if (showDebugLogs)
            Debug.Log($"💥 [AssasinSkill2] Power Arrow 발사 시작");
        
        // ⭐ 1단계: Cast 이펙트 (시전 이펙트)
        EmitSkillCastCue();
        
        // 조이스틱 방향 가져오기
        Vector2 shootDirection = GetCurrentAttackDirection();
        
        // ⭐ 2단계: 큰 화살 발사 (비주얼 + 착탄 지점 결정)
        FirePowerArrow(shootDirection);
    }
    
    /// <summary>
    /// 스킬 애니메이션 트리거 (BaseSkill<T>에서 호출)
    /// </summary>
    protected override void TriggerSkillAnimation()
    {
        if (animationController != null)
        {
            animationController.TriggerSkill2();
        }
    }
    
    /// <summary>
    /// 추가 사용 조건 검사 (Power Arrow 전용)
    /// </summary>
    protected override bool CheckAdditionalConditions()
    {
        // 무기 및 발사 지점 확인
        UpdateFirePoint();
        
        if (firePoint == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [AssasinSkill2] 발사 지점을 찾을 수 없습니다!");
            return false;
        }
        
        // GamePoolManager 확인
        if (GamePoolManager.Instance == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [AssasinSkill2] GamePoolManager가 없습니다!");
            return false;
        }
        
        return true;
    }
    
    #endregion
    
    #region 어쌔신 Power Arrow 전용 로직
    
    /// <summary>
    /// 강력한 단일 화살 발사 메인 로직
    /// </summary>
    private void FirePowerArrow(Vector2 shootDirection)
    {
        if (!IsSkillDataValid || firePoint == null) return;
        
        // SkillData에서 설정값 가져오기
        float arrowSpeed = SkillData.projectileSpeed; // 빠른 속도
        Vector3 arrowScale = SkillData.projectileScale; // 큰 크기
        string poolName = SkillData.projectilePrefab != null ? SkillData.projectilePrefab.name : "PowerArrow";
        
        // 방향을 각도로 변환
        float shootAngle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
        
        // ⭐ 핵심 수정: 기본공격과 동일한 회전 방식
        Quaternion arrowRotation = Quaternion.AngleAxis(shootAngle, Vector3.forward);
        
        // 강력한 화살 발사
        var powerArrow = GamePoolManager.Instance.SpawnFromPool(
            poolName, 
            firePoint.position, 
            arrowRotation  // ← 올바른 회전값 사용
        );
        
        if (powerArrow != null)
        {
            // Power Arrow 설정
            SetupPowerArrow(powerArrow, arrowSpeed, arrowScale);
            
            // ⭐ 화살 추적 → 착탄 시 AOE 폭발
            StartCoroutine(TrackPowerArrowAndExplode(powerArrow, shootDirection, firePoint.position));
            
            if (showDebugLogs)
                Debug.Log($"🏹 [AssasinSkill2] Power Arrow 발사 - 시작위치: {firePoint.position}, 각도: {shootAngle:F1}°, 방향: {shootDirection}");
        }
        else
        {
            Debug.LogError($"❌ [AssasinSkill2] Power Arrow 생성 실패! 풀: {poolName}");
        }
        
        // 시전 이펙트 — castCueKey 기반 CueSystem 경유
        if (!string.IsNullOrEmpty(SkillData.castCueKey))
        {
            var ctx = new CueSystem.CueContext
            {
                position = firePoint.position,
                rotation = firePoint.rotation,
                actorType = ActorType.Player,
                magnitude = 1.5f
            };
            CueSystem.CueEmitter.Emit(SkillData.castCueKey, "Player", ctx);
        }
        
        // 추가 파워 이펙트 (muzzle flash 등)
        CreatePowerEffects();
    }
    
    /// <summary>
    /// ⭐ Phase 3: 스킬 AOE 생성
    /// </summary>
    private void SpawnSkillAOE(Vector2 direction)
    {
        if (!IsSkillDataValid) return;
        
        // AOE 생성 (PlayerSkillAOEDamage가 자동으로 데미지 + Hit Cue 처리)
        SkillAOESpawner.SpawnAOE(
            SkillData.aoeShape,
            transform.position,
            direction,
            SkillData.aoeSize,
            SkillData.aoeFanAngle,
            SkillData.baseDamageMultiplier,
            SkillData.aoeDuration,
            LayerMask.GetMask("Enemy"),
            "skill.assasin.skill2.hit",  // ⭐ Hit Cue 이벤트 키
            this
        );
        
        if (showDebugLogs)
            Debug.Log($"💥 [AssasinSkill2] AOE 생성: {SkillData.aoeShape}, 크기: {SkillData.aoeSize}");
    }
    
    /// <summary>
    /// Power Arrow 오브젝트 설정 (Transform 기반 이동만 사용)
    /// </summary>
    private void SetupPowerArrow(GameObject arrow, float speed, Vector3 scale)
    {
        // 큰 크기 설정
        arrow.transform.localScale = scale;

        // ⭐ 핵심 수정: Rigidbody2D.velocity 설정 제거!
        // Projectile.cs의 transform.Translate가 알아서 처리하도록 함
        
        // 높은 속도 설정 (Projectile 컴포넌트에 직접 전달)
        var projectile = arrow.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.UpdateMoveSpeed(speed);
            
            if (showDebugLogs)
                Debug.Log($"💥 [AssasinSkill2] Power Arrow 속도 설정: {speed}");
        }
        
        // 강화된 데미지 설정
        var damageSource = arrow.GetComponent<DamageSource>();
        if (damageSource != null && showDebugLogs)
        {
            Debug.Log($"💥 [AssasinSkill2] Power Arrow 데미지 소스 감지됨");
        }
        
        // Power Arrow 특수 효과 (관통, 폭발 등)
        if (showDebugLogs)
            Debug.Log($"💥 [AssasinSkill2] Power Arrow '{arrow.name}' 특수 효과 적용 준비");
    }
    
    /// <summary>
    /// Power Arrow 전용 추가 이펙트
    /// </summary>
    private void CreatePowerEffects()
    {
        // 발사 순간 강력한 이펙트
        if (firePoint != null)
        {
            // 추가 이펙트는 나중에 VFX 시스템과 연동
            if (showDebugLogs)
                Debug.Log($"✨ [AssasinSkill2] Power Arrow 추가 이펙트 생성");
        }
    }
    
    #endregion
    
    #region Unity 라이프사이클 확장
    
    protected override void Start()
    {
        base.Start(); // BaseSkill<T>의 초기화 실행
        
        // Power Arrow 스킬 전용 초기화
        if (IsSkillDataValid && showDebugLogs)
        {
            Debug.Log($"💥 [AssasinSkill2] 초기화 완료 - " +
                     $"Power Arrow 속도: {SkillData.projectileSpeed * 1.5f}, " +
                     $"크기: {SkillData.projectileScale}, " +
                     $"쿨다운: {Cooldown}초");
        }
    }
    
    #endregion

    /// <summary>
    /// 현재 조이스틱 공격 방향 가져오기 (기본공격과 동일한 로직)
    /// </summary>
    private Vector2 GetCurrentAttackDirection()
    {
        // 🔍 스킬2 방향 감지 비교 로그
        Debug.Log($"💥 [AssasinSkill2] GetCurrentAttackDirection 호출됨 - 시간: {Time.time:F3}");
        
        // ActiveWeapon에서 AttackJoystickInput 참조 가져오기
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon != null && activeWeapon.attackJoystickInput != null)
        {
            Vector2 joystickDir = activeWeapon.attackJoystickInput.GetAttackDirection();
            
            if (joystickDir.magnitude > 0.1f)
            {
                // 🔍 N/S 방향 특별 확인
                bool isNorthSouth = Mathf.Abs(joystickDir.x) < 0.3f && Mathf.Abs(joystickDir.y) > 0.7f;
                if (isNorthSouth)
                {
                    string directionName = joystickDir.y > 0 ? "NORTH" : "SOUTH";
                    Debug.Log($"🧭 [AssasinSkill2] {directionName} 방향 스킬2 사용! 실시간 조이스틱: {joystickDir}");
                }
                
                if (showDebugLogs)
                    Debug.Log($"🎮 [AssasinSkill2] 조이스틱 방향 사용: {joystickDir} (실시간 감지)");
                return joystickDir.normalized;
            }
        }
        
        // 백업: firePoint.right 사용 (조이스틱 입력이 없을 때)
        if (showDebugLogs)
            Debug.Log($"🎮 [AssasinSkill2] 백업 방향 사용: firePoint.right");
        return firePoint.right;
    }
    
    #region ⭐ Power Arrow 추적 및 원형 AOE 폭발
    
    /// <summary>
    /// ⭐ Power Arrow 추적 및 착탄 시 원형 AOE 폭발
    /// </summary>
    private IEnumerator TrackPowerArrowAndExplode(GameObject arrow, Vector2 direction, Vector3 startPosition)
    {
        if (arrow == null) yield break;
        
        var projectile = arrow.GetComponent<Projectile>();
        float maxTime = 3f; // 최대 추적 시간
        float elapsedTime = 0f;
        
        // ⭐ 마지막 위치 추적 (풀 반환 전 위치 저장)
        Vector3 lastPosition = arrow.transform.position;
        
        // 화살이 활성화되어 있는 동안 추적
        while (arrow != null && arrow.activeInHierarchy && elapsedTime < maxTime)
        {
            lastPosition = arrow.transform.position; // ⭐ 매 프레임 위치 업데이트
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // ⭐ 착탄 지점 (마지막 저장된 위치 사용)
        Vector3 impactPosition = lastPosition;
        
        // ⭐ 거리 계산
        float travelDistance = Vector2.Distance(
            new Vector2(startPosition.x, startPosition.y), 
            new Vector2(impactPosition.x, impactPosition.y)
        );
        
        if (showDebugLogs)
            Debug.Log($"🎯 [AssasinSkill2] Power Arrow 착탄! 발사: {startPosition} → 착탄: {impactPosition}, 비행거리: {travelDistance:F2}");
        
        // ⭐ 3단계: AOE Cue (원형 범위 이펙트)
        EmitSkillAOECue(impactPosition);
        
        // ⭐ 4단계: AOE 비주얼 생성 - PlayerSkillAOEDamage가 자동으로 데미지 + Hit Cue 처리
        SpawnSkillAOEAtPosition(impactPosition, direction);
    }
    
    /// <summary>
    /// ⭐ 착탄 지점에 AOE 데미지 판정 (Phase 3: Telegraph + DamageArea 통합)
    /// </summary>
    private void SpawnSkillAOEAtPosition(Vector3 position, Vector2 direction)
    {
        if (!IsSkillDataValid) return;
        
        // SkillAOEShape → AOEShapeType 변환
        AOEShapeType shapeType = ConvertToAOEShapeType(SkillData.aoeShape);
        
        // ⭐ Circle은 aoeRadius, Rectangle은 aoeSize 사용
        float circleRadius = SkillData.aoeRadius;
        Vector2 rectSize = SkillData.aoeSize;
        
        if (showDebugLogs)
            Debug.Log($"🔍 [AssasinSkill2] AOE 크기 확인: aoeRadius={circleRadius}, aoeSize={rectSize}");
        
        // ⭐ Phase 3: Telegraph 선택적 생성 (프리팹이 있으면 사용)
        float telegraphDelay = 0f;
        
        // ⭐ 강제 로그: Telegraph 프리팹 상태 확인
        Debug.Log($"🔍 [AssasinSkill2] Telegraph 체크: prefab={(SkillData.telegraphPrefab != null ? "있음" : "없음")}");
        
        if (SkillData.telegraphPrefab != null)
        {
            telegraphDelay = SkillData.telegraphDuration;
            
            // 1. Telegraph 생성
            GameObject telegraphObj = Instantiate(SkillData.telegraphPrefab, position, Quaternion.identity);
            Debug.Log($"🔍 [AssasinSkill2] Telegraph GameObject 생성: {telegraphObj.name}");
            
            TelegraphIndicator telegraph = telegraphObj.GetComponent<TelegraphIndicator>();
            Debug.Log($"🔍 [AssasinSkill2] TelegraphIndicator 컴포넌트: {(telegraph != null ? "있음" : "없음")}");
            
            if (telegraph != null)
            {
                telegraph.InitializeForPlayer(
                    shape: shapeType,
                    position: position,          // ⭐ origin → position
                    radius: circleRadius,        // ⭐ Circle: aoeRadius 사용
                    size: rectSize,              // ⭐ Rectangle: aoeSize 사용
                    angle: SkillData.aoeFanAngle,
                    displayDuration: telegraphDelay,  // ⭐ duration → displayDuration
                    scaleMultiplier: 1.0f,
                    casterType: AOECasterType.Player,
                    forward: direction  // ⭐ 추가: Forward 방향 전달
                );
                
                Debug.Log($"📍 [AssasinSkill2] Telegraph 생성 완료: {telegraphDelay}초 경고 (반경: {circleRadius}, 방향: {direction})");
            }
            else
            {
                Debug.LogError($"❌ [AssasinSkill2] TelegraphIndicator 컴포넌트를 찾을 수 없음!");
            }
        }
        else
        {
            Debug.Log($"📍 [AssasinSkill2] Telegraph 프리팹 없음 - 즉시 DamageArea 실행");
        }
        
        // ⭐ 2. DamageArea 실행 (Telegraph 지연 후 또는 즉시)
        StartCoroutine(DelayedDamageArea(position, direction, shapeType, telegraphDelay));
    }
    
    /// <summary>
    /// ⭐ Phase 3: 지연 후 DamageArea 실행
    /// </summary>
    private IEnumerator DelayedDamageArea(Vector3 position, Vector2 direction, AOEShapeType shapeType, float delay)
    {
        // Telegraph 대기 (0이면 즉시 실행)
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }
        
        // DamageArea GameObject 생성
        GameObject damageAreaObj = new GameObject("AssasinSkill2_DamageArea");
        damageAreaObj.transform.position = position;
        
        // DamageArea 컴포넌트 추가
        DamageArea damageArea = damageAreaObj.AddComponent<DamageArea>();
        
        // ⭐ Circle은 aoeRadius, Rectangle은 aoeSize 사용
        float circleRadius = SkillData.aoeRadius;
        Vector2 rectSize = SkillData.aoeSize;
        
        // 오버로드된 InitializeForPlayer 호출 (개별 파라미터)
        damageArea.InitializeForPlayer(
            shape: shapeType,
            origin: position,
            forward: direction,
            radius: circleRadius,               // ⭐ Circle: aoeRadius 사용
            size: rectSize,                     // ⭐ Rectangle: aoeSize 사용
            angle: SkillData.aoeFanAngle,       // 부채꼴 각도
            playerBaseDamage: Mathf.RoundToInt(SkillData.baseDamageMultiplier),
            damageMultiplier: 1.0f,
            scaleMultiplier: 1.0f,
            policy: AOEDamagePolicy.Once,
            hitCueKey: SkillData.hitCueKey
        );
        
        // DamageArea는 즉시 판정 후 제거
        Destroy(damageAreaObj, 0.1f);
        
        // DamageArea 자체가 로그를 출력하므로 생략
    }
    
    #endregion
    
    #region ⭐ 스킬 이펙트 Cue 시스템 (Cast → AOE → Hit)
    
    /// <summary>
    /// 1단계: 스킬 시전 이펙트 (Cast)
    /// </summary>
    private void EmitSkillCastCue()
    {
        // 조이스틱 방향 가져오기
        Vector2 direction = GetCurrentAttackDirection();
        
        // 각도 계산 (회전만 사용)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0, 0, angle);
        
        var context = new CueContext
        {
            position = transform.position,
            rotation = rotation,
            actorType = ActorType.Player,
            magnitude = 2.0f, // 강력한 시전
            surfaceType = SurfaceType.Default,
            follow = transform
        };
        
        bool cueSuccess = CueEmitter.Emit("skill.assasin.skill2.cast", "Player", context);
        
        if (showDebugLogs)
            Debug.Log($"💥 [AssasinSkill2] Cast Cue 발행 (시전 이펙트, 각도: {angle:F1}°) → {cueSuccess}");
    }
    
    /// <summary>
    /// ⭐ Phase C-2: SkillAOEShape → AOEShapeType 변환 헬퍼
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
                return AOEShapeType.Triangle; // Fan → Triangle
            case SkillAOEShape.Line:
                return AOEShapeType.Rectangle; // Line → Rectangle
            default:
                Debug.LogWarning($"[AssasinSkill2] 알 수 없는 SkillAOEShape: {skillShape}, Circle로 기본 설정");
                return AOEShapeType.Circle;
        }
    }
    
    /// <summary>
    /// 2단계: AOE 범위 이펙트 (원형) - 착탄 지점에서
    /// </summary>
    private void EmitSkillAOECue(Vector3 impactPosition)
    {
        var context = new CueContext
        {
            position = impactPosition, // ⭐ 착탄 지점!
            rotation = Quaternion.identity,
            actorType = ActorType.Player,
            magnitude = 2.5f, // 큰 범위
            surfaceType = SurfaceType.Default,
            scale = 1.0f  // ⭐ 명시적 선언 (향후 GetSkillLevelScale()로 변경 가능)
        };
        
        bool cueSuccess = CueEmitter.Emit("skill.assasin.skill2.aoe", "Player", context);
        
        if (showDebugLogs)
            Debug.Log($"💥 [AssasinSkill2] AOE Cue 발행 (원형, 착탄지점: {impactPosition}) → {cueSuccess}");
    }
    
    #endregion
}