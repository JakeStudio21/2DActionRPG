using UnityEngine;
using System.Collections;
using CueSystem; // ⭐ Assasin Skill 이펙트 시스템

/// <summary>
/// 어쌔신 스킬1: Multi Arrow (다중 화살 발사)
/// AssasinSkillData 타입만 허용하는 타입 안전 스킬
/// BaseSkill<T> 상속으로 공통 로직 재사용
/// </summary>
public class AssasinSkill1 : BaseSkill<AssasinSkillData>
{
    #region BaseSkill<T> 구현
    
    /// <summary>
    /// 스킬 실행 시 호출 (애니메이션 트리거)
    /// </summary>
    protected override void OnExecuteSkill()
    {
        Debug.Log($"🏹 [AssasinSkill1] {SkillName} 실행 시작");
        Debug.Log($"   - SkillData 유효성: {IsSkillDataValid}");
        Debug.Log($"   - AnimationController: {(animationController != null ? "✅ 존재" : "❌ 없음")}");
            
        // 애니메이션 트리거
        if (animationController != null)
        {
            Debug.Log($"🎬 [AssasinSkill1] 애니메이션 트리거 호출");
            animationController.TriggerSkill1();
        }
        else
        {
            Debug.LogWarning("🟡 [AssasinSkill1] PlayerAnimationController가 없습니다!");
            // 애니메이션 없이 직접 실행
            Debug.Log("�� [AssasinSkill1] 애니메이션 없이 직접 실행");
            OnAnimationEvent();
        }
    }
    
    /// <summary>
    /// Animation Event에서 호출되는 실제 화살 발사
    /// </summary>
    public override void OnAnimationEvent()
    {
        Debug.Log($"🎯 [AssasinSkill1] Animation Event 호출됨!");
        Debug.Log($"   - SkillData 유효성: {IsSkillDataValid}");
        Debug.Log($"   - 현재 시간: {Time.time:F2}");
        Debug.Log($"   - 마지막 스킬 시간: {lastSkillTime:F2}");
        Debug.Log($"   - 쿨다운 남은 시간: {GetCooldownRemaining():F2}");
        
        if (!IsSkillDataValid)
        {
            Debug.LogError("❌ [AssasinSkill1] SkillData가 유효하지 않습니다!");
            return;
        }
        
        Debug.Log($"🎯 [AssasinSkill1] 화살 발사 시작 - {SkillData.projectileCount}개 화살");
        Debug.Log($"   - ProjectilePrefab: {(SkillData.projectilePrefab != null ? "✅ 존재" : "❌ 없음")}");
        Debug.Log($"   - ProjectileSpeed: {SkillData.projectileSpeed}");
        Debug.Log($"   - ProjectilePoolName: {SkillData.projectilePoolName}");
        
        // ⭐ 1단계: Cast 이펙트 (시전 이펙트)
        EmitSkillCastCue();
        
        // 조이스틱 방향 가져오기
        Vector2 baseDirection = GetCurrentAttackDirection();
        
        // ⭐ 2단계: 화살 발사 (순수 비주얼)
        FireMultipleArrows();
        
        // ⭐ 3단계: 0.1초 딜레이 후 AOE 발동 (코루틴)
        StartCoroutine(DelayedAOESpawn(baseDirection, 0.2f));
    }
    
    /// <summary>
    /// 스킬 애니메이션 트리거 (BaseSkill<T>에서 호출)
    /// </summary>
    protected override void TriggerSkillAnimation()
    {
        if (animationController != null)
        {
            animationController.TriggerSkill1();
        }
    }
    
    /// <summary>
    /// 추가 사용 조건 검사 (화살이 있는지 등)
    /// </summary>
    protected override bool CheckAdditionalConditions()
    {
        // 무기 및 발사 지점 확인
        UpdateFirePoint();
        
        if (firePoint == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [AssasinSkill1] 발사 지점을 찾을 수 없습니다!");
            return false;
        }
        
        // GamePoolManager 확인
        if (GamePoolManager.Instance == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("🟡 [AssasinSkill1] GamePoolManager가 없습니다!");
            return false;
        }
        
        return true;
    }
    
    #endregion
    
    #region 어쌔신 스킬 전용 로직
    
    /// <summary>
    /// 다중 화살 발사 메인 로직
    /// </summary>
    private void FireMultipleArrows()
    {
        if (!IsSkillDataValid || firePoint == null) return;
        
        // SkillData에서 설정값 가져오기
        int arrowCount = SkillData.projectileCount;
        float spreadAngle = SkillData.spreadAngle;
        float arrowSpeed = SkillData.projectileSpeed;
        Vector3 arrowScale = SkillData.projectileScale;
        // string poolName = SkillData.projectilePoolName;
        string poolName = SkillData.projectilePrefab != null ? SkillData.projectilePrefab.name : "Arrow";
        
        // ⭐ 수정: 조이스틱 방향 사용 (기본공격과 동일한 방식)
        Vector2 baseDirection = GetCurrentAttackDirection();
        
        // 기준 각도 계산 (Vector2를 각도로 변환)
        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
        
        // 부채꼴 패턴으로 화살 발사
        float startAngle = baseAngle - (spreadAngle * (arrowCount - 1) / 2f);
        int successCount = 0;
        
        for (int i = 0; i < arrowCount; i++)
        {
            float currentAngle = startAngle + (spreadAngle * i);
            
            // ⭐ 핵심 수정: 기본공격과 동일한 회전 방식
            Quaternion arrowRotation = Quaternion.AngleAxis(currentAngle, Vector3.forward);
            
            // 오브젝트 풀에서 화살 가져오기
            var arrow = GamePoolManager.Instance.SpawnFromPool(
                poolName, 
                firePoint.position, 
                arrowRotation  // ← 올바른 회전값 사용
            );
            
            if (arrow != null)
            {
                // ⭐ 수정: Transform 기반 이동만 사용 (Rigidbody2D 건드리지 않음)
                SetupArrow(arrow, arrowSpeed, arrowScale);
                successCount++;
            }
        }
        
        // 이펙트 생성
        if (SkillData.effectPrefab != null)
        {
            SpawnEffect(SkillData.effectPrefab, firePoint.position, firePoint.rotation);
        }
        
        // ⭐ Phase 3: AOE 생성 (부채꼴)
        SpawnSkillAOE(baseDirection);
        
        if (showDebugLogs)
        {
            Debug.Log($"🏹 [AssasinSkill1] 화살 발사 완료: {successCount}/{arrowCount}개 성공 - 기준각도: {baseAngle:F1}°");
        }
    }
    
    /// <summary>
    /// ⭐ 0.3초 딜레이 후 AOE 발동 (차징 느낌)
    /// </summary>
    private IEnumerator DelayedAOESpawn(Vector2 direction, float delay)
    {
        // 딜레이
        yield return new WaitForSeconds(delay);
        
        // AOE 이펙트 발행
        EmitSkillAOECue(direction);
        
        // AOE 비주얼 생성
        SpawnSkillAOE(direction);
        
        if (showDebugLogs)
            Debug.Log($"🏹 [AssasinSkill1] AOE 딜레이 후 발동 ({delay}초)");
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
            SkillData.damage,
            SkillData.aoeDuration,
            LayerMask.GetMask("Enemy"),
            "skill.assasin.skill1.hit",  // ⭐ Hit Cue 이벤트 키
            this
        );
        
        if (showDebugLogs)
            Debug.Log($"💥 [AssasinSkill1] AOE 생성: {SkillData.aoeShape}, 크기: {SkillData.aoeSize}");
    }
    
    /// <summary>
    /// 벡터 회전 유틸리티
    /// </summary>
    private Vector2 RotateVector(Vector2 vector, float angleDegrees)
    {
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angleRadians);
        float sin = Mathf.Sin(angleRadians);
        
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }
    
    /// <summary>
    /// 화살 오브젝트 설정 (Transform 기반 이동만 사용)
    /// </summary>
    private void SetupArrow(GameObject arrow, float speed, Vector3 scale)
    {
        // 크기 설정
        arrow.transform.localScale = scale;
        
        // ⭐ 핵심 수정: Rigidbody2D.velocity 설정 제거!
        // Projectile.cs의 transform.Translate가 알아서 처리하도록 함
        
        // 속도 설정 (Projectile 컴포넌트에 직접 전달)
        var projectile = arrow.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.UpdateMoveSpeed(speed);
            
            if (showDebugLogs)
                Debug.Log($"🎯 [AssasinSkill1] 화살 속도 설정: {speed}");
        }
        
        // 데미지 설정 (DamageSource 컴포넌트가 있다면)
        var damageSource = arrow.GetComponent<DamageSource>();
        if (damageSource != null && showDebugLogs)
        {
            Debug.Log($"🎯 [AssasinSkill1] DamageSource 감지됨 - 자동 데미지 적용");
        }
    }
    
    #endregion
    
    #region Unity 라이프사이클 확장
    
    protected override void Start()
    {
        base.Start(); // BaseSkill<T>의 초기화 실행
        
        // 어쌔신 스킬 전용 초기화
        if (IsSkillDataValid && showDebugLogs)
        {
            Debug.Log($"🏹 [AssasinSkill1] 초기화 완료 - " +
                     $"화살 수: {SkillData.projectileCount}, " +
                     $"퍼짐각: {SkillData.spreadAngle}°, " +
                     $"속도: {SkillData.projectileSpeed}");
        }
    }
    
    #endregion

    /// <summary>
    /// 현재 조이스틱 공격 방향 가져오기 (기본공격과 동일한 로직)
    /// </summary>
    private Vector2 GetCurrentAttackDirection()
    {
        // 🔍 스킬 방향 감지 비교 로그
        Debug.Log($"🏹 [AssasinSkill1] GetCurrentAttackDirection 호출됨 - 시간: {Time.time:F3}");
        
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
                    Debug.Log($"🧭 [AssasinSkill1] {directionName} 방향 스킬 사용! 실시간 조이스틱: {joystickDir}");
                }
                
                if (showDebugLogs)
                    Debug.Log($"🎮 [AssasinSkill1] 조이스틱 방향 사용: {joystickDir} (실시간 감지)");
                return joystickDir.normalized;
            }
        }
        
        // 백업: firePoint.right 사용 (조이스틱 입력이 없을 때)
        if (showDebugLogs)
            Debug.Log($"🎮 [AssasinSkill1] 백업 방향 사용: firePoint.right");
        return firePoint.right;
    }
    
    
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
            magnitude = 1.2f,
            surfaceType = SurfaceType.Default,
            follow = transform
        };
        
        bool cueSuccess = CueEmitter.Emit("skill.assasin.skill1.cast", "Player", context);
        
        if (showDebugLogs)
            Debug.Log($"🏹 [AssasinSkill1] Cast Cue 발행 (시전 이펙트, 각도: {angle:F1}°) → {cueSuccess}");
    }
    
    /// <summary>
    /// 2단계: AOE 범위 이펙트 (부채꼴)
    /// </summary>
    private void EmitSkillAOECue(Vector2 direction)
    {
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
            scale = 1.0f  // ⭐ 명시적 선언 (향후 GetSkillLevelScale()로 변경 가능)
        };
        
        bool cueSuccess = CueEmitter.Emit("skill.assasin.skill1.aoe", "Player", context);
        
        if (showDebugLogs)
            Debug.Log($"🏹 [AssasinSkill1] AOE Cue 발행 (부채꼴 범위, 각도: {angle:F1}°) → {cueSuccess}");
    }
    
    #endregion
}