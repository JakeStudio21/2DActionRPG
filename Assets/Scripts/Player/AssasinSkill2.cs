using UnityEngine;

/// <summary>
/// 어쌔신 스킬2: Power Arrow (강력한 단일 화살)
/// AssasinSkillData 타입만 허용하는 타입 안전 스킬
/// BaseSkill<T> 상속으로 공통 로직 재사용
/// </summary>
public class AssasinSkill2 : BaseSkill<AssasinSkillData>
{
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
            
        FirePowerArrow();
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
    private void FirePowerArrow()
    {
        if (!IsSkillDataValid || firePoint == null) return;
        
        // SkillData에서 설정값 가져오기
        float arrowSpeed = SkillData.projectileSpeed; // 빠른 속도
        Vector3 arrowScale = SkillData.projectileScale; // 큰 크기
        string poolName = !string.IsNullOrEmpty(SkillData.projectilePoolName) 
            ? SkillData.projectilePoolName 
            : "PowerArrow"; // Power Arrow 전용 풀 사용
        
        // ⭐ 수정: 조이스틱 방향 사용 (기본공격과 동일한 방식)
        Vector2 shootDirection = GetCurrentAttackDirection();
        
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
            
            if (showDebugLogs)
                Debug.Log($"💥 [AssasinSkill2] Power Arrow 발사 성공 - 각도: {shootAngle:F1}°, 속도: {arrowSpeed}, 크기: {arrowScale}");
        }
        else
        {
            Debug.LogError($"❌ [AssasinSkill2] Power Arrow 생성 실패! 풀: {poolName}");
        }
        
        // 강력한 발사 이펙트 생성
        if (SkillData.effectPrefab != null)
        {
            SpawnEffect(SkillData.effectPrefab, firePoint.position, firePoint.rotation);
        }
        
        // 추가 파워 이펙트 (muzzle flash 등)
        CreatePowerEffects();
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
        // ActiveWeapon에서 AttackJoystickInput 참조 가져오기
        var activeWeapon = ActiveWeapon.Instance;
        if (activeWeapon != null && activeWeapon.attackJoystickInput != null)
        {
            Vector2 joystickDir = activeWeapon.attackJoystickInput.GetAttackDirection();
            
            if (joystickDir.magnitude > 0.1f)
            {
                if (showDebugLogs)
                    Debug.Log($"🎮 [AssasinSkill2] 조이스틱 방향 사용: {joystickDir}");
                return joystickDir.normalized;
            }
        }
        
        // 백업: firePoint.right 사용 (조이스틱 입력이 없을 때)
        if (showDebugLogs)
            Debug.Log($"🎮 [AssasinSkill2] 백업 방향 사용: firePoint.right");
        return firePoint.right;
    }
}