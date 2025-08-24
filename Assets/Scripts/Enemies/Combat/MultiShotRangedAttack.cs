using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CueSystem; // ✅ 추가

/// <summary>
/// 복합 원거리 공격 구현체 - BaseAttackBehaviour 상속으로 중복 코드 제거
/// ⭐ [Phase 2] AttackData 기반 확장 지원 (복합 원거리 공격 + 다중 발사체 특화)
/// </summary>
public class MultiShotRangedAttack : BaseAttackBehaviour
{
    #region 기존 시스템 (100% 유지)
    
    [Header("MultiShot Specific Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private int projectilesPerBurst = 1;
    [SerializeField][Range(0, 359)] private float angleSpread = 0f;
    [SerializeField] private float startingDistance = 0.1f;
    [SerializeField] private float timeBetweenBursts = 0.5f;
    [SerializeField] private float restTime = 1f;
    
    // ⭐ 추가: 누락된 기본 필드들
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileLifetime = 5f;
    
    [Header("Advanced Settings")]
    [SerializeField] private bool stagger = false;
    [Tooltip("Stagger must be enabled for oscillate to function properly.")]
    [SerializeField] private bool oscillate = false;

    private bool isShooting = false;
    
    #endregion

    #region ⭐ 새 시스템: 복합 원거리 공격 고급 설정 (AttackData 우선)
    
    [Header("⭐ 복합 공격 고급 설정 (AttackData 우선)")]
    [Tooltip("발사 패턴 (AttackData 우선, 없으면 이 값 사용)")]
    [SerializeField] private MultiShotPattern shotPattern = MultiShotPattern.Spread;
    
    [Tooltip("발사체 궤도 곡선 여부 (AttackData 우선)")]
    [SerializeField] private bool useProjectileArc = false;
    
    [Tooltip("복합 공격 시 화면 흔들림 (AttackData 우선)")]
    [SerializeField] private bool useScreenShake = false;
    
    [Tooltip("복합 공격 디버그 표시 (AttackData 우선)")]
    [SerializeField] private bool showMultiShotGizmos = true;
    
    // 발사체 추적용
    private List<GameObject> activeBullets = new List<GameObject>();
    private int totalProjectilesFired = 0;
    
    #endregion

    #region BaseAttackBehaviour 추상 메서드 구현 (기존 + 확장)

    protected override void OnInitialize()
    {
        // ⭐ 새 시스템: 복합 원거리 공격 데이터 검증
        ValidateMultiShotSettings();
    }

    protected override void OnAttack()
    {
        // 복합 공격 시작
        if (!isShooting)
        {
            StartCoroutine(ShootRoutine());
        }
    }
    
    /// <summary>
    /// ⭐ 새 시스템: 공격 타입 검증 (BaseAttackBehaviour에서 요구)
    /// </summary>
    protected override void ValidateAttackType()
    {
        if (AttackData != null && AttackData.AttackType != AttackType.Ranged)
        {
            Debug.LogWarning($"[MultiShotRangedAttack] {gameObject.name} - AttackData의 공격 타입이 Ranged가 아닙니다: {AttackData.AttackType}");
        }
    }
    
    #endregion

    #region ⭐ 새 시스템: Fallback 메서드들 구현
    
    /// <summary>
    /// AttackData가 없을 때 사용할 기본 데미지 (개별 발사체용)
    /// </summary>
    protected override int GetFallbackDamage() => projectileDamage;
    
    /// <summary>
    /// AttackData가 없을 때 사용할 기본 공격 범위
    /// </summary>
    protected override float GetFallbackRange() => 6f; // 복합 공격은 더 넓은 범위
    
    #endregion

    #region 기존 시스템 메서드들 (100% 유지 + 확장)

    // 커스텀 CanAttack 오버라이드 (기존 유지)
    public override bool CanAttack()
    {
        return !isShooting && base.CanAttack();
    }

    // BaseAttackBehaviour.Attack() 오버라이드 (기존 + 개선)
    public override void Attack()
    {
        if (!isShooting) 
        {
            Debug.Log($"[MultiShotRangedAttack] {gameObject.name}: Attack() 메서드 호출됨!");
            
            // ⭐ 새 시스템: 데이터 기반 애니메이션 트리거
            TriggerAttackAnimation();
            
            // ⭐ 새 시스템: 복합 공격 시작 이펙트
            PlayMultiShotStartEffect();
            
            StartCoroutine(ShootRoutine());
            Debug.Log($"[MultiShotRangedAttack] {gameObject.name}: 복합 공격 실행! (총 발사체: {GetTotalProjectileCount()}개)");
        }
        else
        {
            Debug.Log($"[MultiShotRangedAttack] {gameObject.name}: 이미 공격 중이므로 스킵");
        }
    }

    /// <summary>
    /// Animation Event에서 호출할 수 있는 공개 메서드 (기존 호환성 유지)
    /// </summary>
    public void SpawnProjectileAnimEvent()
    {
        Debug.Log($"[MultiShotRangedAttack] {gameObject.name} - Animation Event 복합 발사체 생성!");
        
        // ✅ 🎵 Cue 시스템 추가
        EmitMultiShotCues();
        
        Attack();
    }
    
    #endregion

    #region ⭐ 새 시스템: 개선된 복합 공격 로직

    /// <summary>
    /// 복합 공격 루틴 (완전 새로 구현)
    /// </summary>
    private IEnumerator ShootRoutine() 
    {
        isShooting = true;
        totalProjectilesFired = 0;
        
        // ⭐ 패턴별 발사 방식 결정
        MultiShotPattern currentPattern = GetCurrentShotPattern();
        bool useStagger = DetermineStaggerForPattern(currentPattern);
        
        Debug.Log($"[MultiShotRangedAttack] 패턴: {currentPattern}, 순차발사: {useStagger}");
        
        Debug.Log($"[MultiShotRangedAttack] {gameObject.name}: 복합 공격 시작");
        Debug.Log($"[MultiShotRangedAttack] 설정: 버스트={GetCurrentBurstCount()}, 버스트당 발사체={GetCurrentProjectilesPerBurst()}, 각도={GetCurrentAngleSpread()}");

        // ⭐ 새 시스템: 데이터 기반 사운드 재생
        PlayAttackSound();

        float startAngle, currentAngle, angleStep, endAngle;
        float timeBetweenProjectiles = 0f;

        TargetConeOfInfluence(out startAngle, out currentAngle, out angleStep, out endAngle);

        if (useStagger) 
        {
            if (currentPattern == MultiShotPattern.Spiral)
            {
                // Spiral 전용: 매우 짧은 간격 (나선 효과)
                timeBetweenProjectiles = 0.05f;
            }
            else
            {
                // 기존 계산
                timeBetweenProjectiles = GetCurrentTimeBetweenBursts() / GetCurrentProjectilesPerBurst();
            }
        }

        for (int i = 0; i < GetCurrentBurstCount(); i++)
        {
            if (!oscillate) 
            {
                TargetConeOfInfluence(out startAngle, out currentAngle, out angleStep, out endAngle);
            } 
            
            if (oscillate && i % 2 != 1) 
            {
                TargetConeOfInfluence(out startAngle, out currentAngle, out angleStep, out endAngle);
            } 
            else if (oscillate) 
            {
                currentAngle = endAngle;
                endAngle = startAngle;
                startAngle = currentAngle;
                angleStep *= -1;
            }

            // ⭐ 개선: 버스트 시작 이펙트
            PlayBurstStartEffect(i);

            for (int j = 0; j < GetCurrentProjectilesPerBurst(); j++)
            {
                // GamePoolManager null 체크
                if (GamePoolManager.Instance == null)
                {
                    Debug.LogError($"[MultiShotRangedAttack] {gameObject.name}: GamePoolManager가 null입니다!");
                    yield break;
                }

                Vector2 pos = FindBulletSpawnPos(currentAngle);

                // ⭐ 새 시스템: 데이터 기반 발사체 생성
                GameObject newBullet = CreateProjectile(pos);
                
                if (newBullet != null)
                {
                    // ⭐ 새 시스템: 데이터 기반 발사체 설정
                    ConfigureProjectile(newBullet, currentAngle, i, j);
                    
                    // 활성 발사체 목록에 추가
                    activeBullets.Add(newBullet);
                    totalProjectilesFired++;
                }

                currentAngle += angleStep;

                if (useStagger) { yield return new WaitForSeconds(timeBetweenProjectiles); }
            }

            currentAngle = startAngle;

            if (!useStagger) { yield return new WaitForSeconds(GetCurrentTimeBetweenBursts()); }
        }

        // ⭐ 새 시스템: 복합 공격 완료 이펙트
        PlayMultiShotEndEffect();

        yield return new WaitForSeconds(GetCurrentRestTime());
        isShooting = false;
        
        Debug.Log($"[MultiShotRangedAttack] {gameObject.name}: 복합 공격 완료 (총 발사: {totalProjectilesFired}개)");
    }
    
    /// <summary>
    /// 패턴별 발사 방식 결정
    /// </summary>
    private bool DetermineStaggerForPattern(MultiShotPattern pattern)
    {
        switch (pattern)
        {
            case MultiShotPattern.Spread:
                // Spread: 동시 발사 (360도든 부채꼴이든)
                return false;
                
            case MultiShotPattern.Spiral:
                // Spiral: 약간의 시간차로 나선 효과 강화
                return true;
                
            case MultiShotPattern.Random:
                // Random: 동시 발사 (완전 무작위)
                return false;
                
            default:
                return GetCurrentUseStaggeredFiring();
        }
    }
    
    #endregion

    #region ⭐ 새 시스템: 발사체 생성 및 설정

    /// <summary>
    /// 데이터 기반 발사체 생성
    /// </summary>
    private GameObject CreateProjectile(Vector2 spawnPosition)
    {
        GameObject prefabToUse = GetCurrentBulletPrefab();
        if (prefabToUse == null)
        {
            Debug.LogError($"[MultiShotRangedAttack] {gameObject.name}: 발사체 프리팹이 설정되지 않았습니다!");
            return null;
        }

        string poolTag = GetPoolTagFromPrefab(prefabToUse);
        GameObject newBullet = GamePoolManager.Instance.SpawnFromPool(poolTag, spawnPosition, Quaternion.identity);
        
        if (newBullet == null)
        {
            Debug.LogWarning($"[MultiShotRangedAttack] {gameObject.name}: {poolTag} 생성 실패");
            // 직접 생성 시도
            newBullet = Instantiate(prefabToUse, spawnPosition, Quaternion.identity);
        }

        return newBullet;
    }

    /// <summary>
    /// 발사체 설정 (데이터 기반)
    /// </summary>
    private void ConfigureProjectile(GameObject projectile, float angle, int burstIndex, int projectileIndex)
    {
        // ⭐ 수정: 패턴별 정확한 방향 계산
        float actualAngle = CalculateActualAngle(angle, burstIndex, projectileIndex);
        
        // 방향 벡터 계산
        Vector2 direction = new Vector2(Mathf.Cos(actualAngle * Mathf.Deg2Rad), Mathf.Sin(actualAngle * Mathf.Deg2Rad));
        
        // ⭐ 새 시스템: 데이터 기반 스탯 설정
        int currentDamage = GetScaledDamage();
        float currentSpeed = GetCurrentProjectileSpeed();
        float currentLifetime = GetCurrentProjectileLifetime();
        
        // ⭐ Ghost 전용 발사체 설정
        if (projectile.TryGetComponent(out GhostProjectile ghostProjectile))
        {
            ghostProjectile.SetDamage(currentDamage);
            
            // 속도 설정
            if (projectile.TryGetComponent(out Rigidbody2D rb))
            {
                rb.velocity = direction * currentSpeed;
                
                // 패턴별 디버그 로그
                string patternName = GetCurrentShotPattern().ToString();
                Debug.Log($"[MultiShotRangedAttack] {patternName} 발사체 {projectileIndex}: 각도={actualAngle:F1}도");
            }
        }
        // ⭐ 범용 발사체 설정
        else if (projectile.TryGetComponent(out Projectile projectile2))
        {
            // Legacy 지원: EnemyDamage 컴포넌트 설정
            if (projectile.TryGetComponent(out EnemyDamage enemyDamage))
            {
                enemyDamage.damageAmount = currentDamage;
            }
        }
        // ⭐ 인터페이스 기반 설정 (확장성)
        else
        {
            // IProjectileDamage, IProjectileMovement 인터페이스 사용
            if (projectile.TryGetComponent(out IProjectileDamage damageComp))
            {
                damageComp.SetDamage(currentDamage);
            }
            
            if (projectile.TryGetComponent(out IProjectileMovement moveComp))
            {
                moveComp.SetSpeed(currentSpeed);
            }
        }
        
        // ⭐ 새 시스템: 상태이상 효과 준비 (발사체에 정보 전달)
        PrepareStatusEffectsForProjectile(projectile);
        
        // ⭐ 새 시스템: 발사체별 고유 설정
        ApplyProjectileVariation(projectile, burstIndex, projectileIndex);
        
        // 수명 설정
        if (currentLifetime > 0)
        {
            Destroy(projectile, currentLifetime);
        }
        
        Debug.Log($"[MultiShotRangedAttack] 발사체 설정 완료: 데미지={currentDamage}, 속도={currentSpeed:F1}, 각도={angle:F1}");
    }

    /// <summary>
    /// 발사체에 상태이상 정보 전달
    /// </summary>
    private void PrepareStatusEffectsForProjectile(GameObject projectile)
    {
        if (AttackData != null && AttackData.OnHitEffects.Count > 0)
        {
            // 발사체에 상태이상 데이터 전달 (발사체가 지원하는 경우)
            if (projectile.TryGetComponent(out IProjectileStatusEffect statusEffectComp))
            {
                statusEffectComp.SetStatusEffects(AttackData.OnHitEffects, AttackData.EffectChances);
            }
        }
    }

    /// <summary>
    /// 발사체별 고유 변화 적용 (패턴에 따라)
    /// </summary>
    private void ApplyProjectileVariation(GameObject projectile, int burstIndex, int projectileIndex)
    {
        switch (GetCurrentShotPattern())
        {
            case MultiShotPattern.Spread:
                // 기본 부채꼴 패턴 (현재 구현)
                break;
                
            case MultiShotPattern.Spiral:
                // 나선형 패턴 (각도를 시간에 따라 조정)
                ApplySpiralPattern(projectile, burstIndex, projectileIndex);
                break;
                
            case MultiShotPattern.Random:
                // 랜덤 패턴
                ApplyRandomPattern(projectile);
                break;
        }
    }
    
    #endregion

    #region ⭐ 새 시스템: 발사 패턴 구현

    /// <summary>
    /// 나선형 패턴 적용
    /// </summary>
    private void ApplySpiralPattern(GameObject projectile, int burstIndex, int projectileIndex)
    {
        if (projectile.TryGetComponent(out Rigidbody2D rb))
        {
            // 나선형 속도 조정
            float spiralAngle = (burstIndex * 60f + projectileIndex * 15f) * Mathf.Deg2Rad;
            Vector2 spiralDirection = new Vector2(Mathf.Cos(spiralAngle), Mathf.Sin(spiralAngle));
            rb.velocity += spiralDirection * (GetCurrentProjectileSpeed() * 0.3f);
        }
    }

    /// <summary>
    /// 랜덤 패턴 적용
    /// </summary>
    private void ApplyRandomPattern(GameObject projectile)
    {
        if (projectile.TryGetComponent(out Rigidbody2D rb))
        {
            // 랜덤 방향 오차 추가
            float randomAngle = Random.Range(-30f, 30f) * Mathf.Deg2Rad;
            Vector2 randomDirection = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
            rb.velocity += randomDirection * GetCurrentProjectileSpeed() * 0.2f;
        }
    }
    
    #endregion

    #region ⭐ 새 시스템: 데이터 기반 속성 계산

    /// <summary>
    /// 현재 발사체 프리팹 반환 (데이터 우선순위)
    /// </summary>
    private GameObject GetCurrentBulletPrefab()
    {
        // 1순위: AttackData
        if (AttackData != null && AttackData.ProjectilePrefab != null)
        {
            return AttackData.ProjectilePrefab;
        }
        
        // 2순위: Inspector 설정
        return bulletPrefab;
    }

    /// <summary>
    /// 현재 버스트 수 반환 (데이터 우선순위)
    /// </summary>
    private int GetCurrentBurstCount()
    {
        // 1순위: AttackData가 있으면 기본 1번 버스트
        if (AttackData != null)
        {
            return 1; // 모든 발사체를 한 번에 발사
        }
        
        // 2순위: Inspector 설정
        return burstCount;
    }

    /// <summary>
    /// 현재 버스트당 발사체 수 반환 (데이터 우선순위)
    /// </summary>
    private int GetCurrentProjectilesPerBurst()
    {
        // 1순위: AttackData
        if (AttackData != null && AttackData.ProjectileCount > 0)
        {
            // ⭐ 수정: ProjectileCount를 직접 버스트당 발사체 수로 사용
            return AttackData.ProjectileCount;
        }
        
        // 2순위: Inspector 설정
        return projectilesPerBurst;
    }

    /// <summary>
    /// 현재 각도 분산 반환 (데이터 우선순위)
    /// </summary>
    private float GetCurrentAngleSpread()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.MultiShotAngle;
        }
        
        // 2순위: Inspector 설정 (기존 방식)
        return angleSpread;
    }

    /// <summary>
    /// 현재 발사체 속도 반환 (데이터 우선순위)
    /// </summary>
    private float GetCurrentProjectileSpeed()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.ProjectileSpeed;
        }
        
        // 2순위: Inspector 설정
        return projectileSpeed;
    }

    /// <summary>
    /// 현재 발사체 수명 반환 (데이터 우선순위)
    /// </summary>
    private float GetCurrentProjectileLifetime()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.ProjectileLifetime;
        }
        
        // 2순위: Inspector 설정
        return projectileLifetime;
    }

    /// <summary>
    /// 현재 버스트 간격 반환 (데이터 우선순위)
    /// </summary>
    private float GetCurrentTimeBetweenBursts()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.TimeBetweenBursts;
        }
        
        // 2순위: Inspector 설정
        return timeBetweenBursts;
    }

    /// <summary>
    /// 현재 휴식 시간 반환 (데이터 우선순위)
    /// </summary>
    private float GetCurrentRestTime()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.RestTime;
        }
        
        // 2순위: Inspector 설정
        return restTime;
    }

    /// <summary>
    /// 총 발사체 수 계산
    /// </summary>
    private int GetTotalProjectileCount()
    {
        return GetCurrentBurstCount() * GetCurrentProjectilesPerBurst();
    }
    
    #endregion

    #region ⭐ 새 시스템: 복합 공격 이펙트 메서드들

    /// <summary>
    /// 복합 공격 시작 이펙트
    /// </summary>
    private void PlayMultiShotStartEffect()
    {
        if (AttackData != null && AttackData.AttackStartEffect != null)
        {
            GameObject effect = Instantiate(AttackData.AttackStartEffect, transform.position, Quaternion.identity);
            
            // 복합 공격용 스케일 증가
            effect.transform.localScale *= 1.5f;
            
            Debug.Log($"[MultiShotRangedAttack] 복합 공격 시작 이펙트 재생");
        }
    }

    /// <summary>
    /// 버스트 시작 이펙트
    /// </summary>
    private void PlayBurstStartEffect(int burstIndex)
    {
        // AttackData 기반 이펙트 재생
        if (AttackData != null && AttackData.AttackStartEffect != null)
        {
            GameObject effect = Instantiate(AttackData.AttackStartEffect, transform.position, Quaternion.identity);
            
            // 버스트별 색상 변경이나 크기 조절 등 가능
            if (burstIndex > 0)
            {
                var renderer = effect.GetComponent<Renderer>();
                if (renderer != null)
                {
                    // 버스트 진행에 따라 색상 변경 예시
                    Color color = Color.Lerp(Color.white, Color.red, (float)burstIndex / GetCurrentBurstCount());
                    renderer.material.color = color;
                }
            }
            
            Debug.Log($"[MultiShotRangedAttack] 버스트 {burstIndex + 1} 시작 이펙트 재생");
        }
    }

    /// <summary>
    /// 복합 공격 완료 이펙트
    /// </summary>
    private void PlayMultiShotEndEffect()
    {
        Debug.Log($"[MultiShotRangedAttack] 복합 공격 완료 - 총 {totalProjectilesFired}개 발사");
    }
    
    #endregion

    #region ⭐ 새 시스템: 복합 공격 이펙트 메서드들

    /// <summary>
    /// 화면 흔들림 효과 트리거 (데이터 기반)
    /// </summary>
    private void TriggerScreenShake()
    {
        // Camera Shake 시스템이 있으면 사용 (안전한 방식)
        if (Camera.main != null)
        {
            // CameraShake 컴포넌트가 있다면 사용
            var cameraShake = Camera.main.GetComponent<MonoBehaviour>();
            if (cameraShake != null && cameraShake.GetType().Name == "CameraShake")
            {
                // 리플렉션을 통한 안전한 호출
                var shakeMethod = cameraShake.GetType().GetMethod("Shake");
                if (shakeMethod != null)
                {
                    shakeMethod.Invoke(cameraShake, new object[] { 0.3f, 0.2f });
                    Debug.Log($"[MultiShotRangedAttack] 화면 흔들림 효과 적용!");
                    return;
                }
            }
        }
        
        // 대안: 로그로 효과 표시
        Debug.Log($"[MultiShotRangedAttack] 화면 흔들림 효과 (CameraShake 없음)");
        
        // 대안: 플레이어 진동 효과 (모바일)
        // if (Handheld.Vibrate != null) Handheld.Vibrate();
    }

    #endregion

    #region ✅ 🎵 Cue 시스템 연동 (Phase B-1 추가)
    
    /// <summary>
    /// 🎵 복합 공격 이펙트 Cue 발행
    /// </summary>
    private void EmitMultiShotCues()
    {
        try
        {
            // CueContext 생성
            var context = new CueContext
            {
                position = transform.position, // ✅ 수정: projectileSpawnPoint → transform
                rotation = transform.rotation,
                normal = Vector3.up,
                facingDir = GetBaseAimDirection(),
                follow = null,
                actorType = ActorType.Enemy,
                surfaceType = SurfaceType.Default,
                magnitude = (float)GetCurrentProjectilesPerBurst() / 3f,
                isCritical = false,
                scale = 1.0f
            };
            
            // 이벤트 키
            string eventKey = "attack.multishot.burst";
            
            // Cue 발행
            bool success = CueEmitter.Emit(eventKey, "Enemy", context);
            
            Debug.Log($"🎵 [MultiShotRangedAttack] Cue 발행: {eventKey} → {(success ? "성공" : "실패")}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🔴 [MultiShotRangedAttack] Cue 발행 오류: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 🧭 기본 조준 방향 계산
    /// </summary>
    private Vector2 GetBaseAimDirection()
    {
        if (cachedPlayer == null) return Vector2.right; // ✅ 수정: playerTransform → cachedPlayer
        
        Vector2 direction = (cachedPlayer.transform.position - transform.position).normalized; // ✅ 수정: projectileSpawnPoint → transform
        return direction;
    }
    
    #endregion

    #region 기존 시스템 메서드들 (100% 유지)

    /// <summary>
    /// 프리팹에서 풀 태그 추출 (RangedAttack과 동일)
    /// </summary>
    private string GetPoolTagFromPrefab(GameObject prefab)
    {
        return prefab.name switch
        {
            var name when name.Contains("Ghost") => "Ghost_Bullet",
            var name when name.Contains("Bullet") => "Bullet",
            var name when name.Contains("Projectile") => "Projectile",
            _ => prefab.name
        };
    }

    private void OnValidate() 
    {
        if (oscillate) { stagger = true; }
        if (!oscillate) { stagger = false; }
        if (projectilesPerBurst < 1) { projectilesPerBurst = 1; }
        if (burstCount < 1) { burstCount = 1; }
        if (timeBetweenBursts < 0.1f) { timeBetweenBursts = 0.1f; }
        if (restTime < 0.1f) { restTime = 0.1f; }
        if (startingDistance < 0.1f) { startingDistance = 0.1f; }
        if (angleSpread == 0) { projectilesPerBurst = 1; }
    }

    private void TargetConeOfInfluence(out float startAngle, out float currentAngle, out float angleStep, out float endAngle)
    {
        // BaseAttackBehaviour의 cachedPlayer 사용
        if (cachedPlayer == null)
        {
            Debug.LogWarning($"[MultiShotRangedAttack] {gameObject.name}: PlayerController를 찾을 수 없습니다!");
            startAngle = 0f;
            endAngle = 0f;
            currentAngle = 0f;
            angleStep = 0f;
            return;
        }

        Vector2 targetDirection = cachedPlayer.transform.position - transform.position;
        float targetAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        startAngle = targetAngle;
        endAngle = targetAngle;
        currentAngle = targetAngle;
        float halfAngleSpread = 0f;
        angleStep = 0;
        
        // ⭐ 개선: 데이터 기반 각도 분산 사용
        float currentSpread = GetCurrentAngleSpread();
        
        if (currentSpread != 0)
        {
            int currentProjectiles = GetCurrentProjectilesPerBurst();
            
            // ⭐ 수정: 정확한 각도 간격 계산
            if (currentSpread >= 360f)
            {
                // 360도 전방향 발사: 각도 간격 = 360 / 발사체 수
                angleStep = 360f / currentProjectiles;
                startAngle = targetAngle;
                endAngle = targetAngle + 360f;
                currentAngle = startAngle;
                
                Debug.Log($"[MultiShotRangedAttack] 360도 전방향 발사: {currentProjectiles}개, 간격: {angleStep:F1}도");
            }
            else
            {
                // 부채꼴 발사: 기존 로직 유지
                angleStep = currentSpread / (currentProjectiles - 1);
                halfAngleSpread = currentSpread / 2f;
                startAngle = targetAngle - halfAngleSpread;
                endAngle = targetAngle + halfAngleSpread;
                currentAngle = startAngle;
                
                Debug.Log($"[MultiShotRangedAttack] 부채꼴 발사: {currentProjectiles}개, 총각도: {currentSpread}도, 간격: {angleStep:F1}도");
            }
        }
    }

    private Vector2 FindBulletSpawnPos(float currentAngle) 
    {
        float x = transform.position.x + startingDistance * Mathf.Cos(currentAngle * Mathf.Deg2Rad);
        float y = transform.position.y + startingDistance * Mathf.Sin(currentAngle * Mathf.Deg2Rad);

        Vector2 pos = new Vector2(x, y);

        return pos;
    }
    
    #endregion

    #region ⭐ 새 시스템: 복합 공격 검증 및 설정
    
    /// <summary>
    /// 복합 원거리 공격 설정 검증
    /// </summary>
    private void ValidateMultiShotSettings()
    {
        if (AttackData != null)
        {
            Debug.Log($"[MultiShotRangedAttack] AttackData 기반 복합 원거리 공격 설정:");
            Debug.Log($"  - 공격명: {AttackData.AttackName}");
            Debug.Log($"  - 총 발사체 수: {GetTotalProjectileCount()}개");
            Debug.Log($"  - 버스트 수: {GetCurrentBurstCount()}");
            Debug.Log($"  - 버스트당 발사체: {GetCurrentProjectilesPerBurst()}개");
            Debug.Log($"  - 각도 분산: {GetCurrentAngleSpread():F1}도");
            Debug.Log($"  - 발사체 속도: {GetCurrentProjectileSpeed():F1}");
            Debug.Log($"  - 상태이상 개수: {AttackData.OnHitEffects.Count}개");
            
            // 복합 공격 검증
            if (GetCurrentBurstCount() == 1 && GetCurrentProjectilesPerBurst() == 1)
            {
                Debug.LogWarning($"[MultiShotRangedAttack] 복합 공격치고 발사체가 1개뿐입니다. RangedAttack을 사용하는 것이 좋습니다.");
            }
        }
        else
        {
            Debug.Log($"[MultiShotRangedAttack] 기존 방식 사용:");
            Debug.Log($"  - 발사체: {(bulletPrefab != null ? bulletPrefab.name : "없음")}");
            Debug.Log($"  - 총 발사체: {burstCount * projectilesPerBurst}개");
            Debug.Log($"  - 버스트: {burstCount}, 각도: {angleSpread}도");
        }
    }
    
    #endregion

    #region ⭐ 디버그 및 시각화
    
    /// <summary>
    /// 복합 공격 범위 시각화 (에디터에서만)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // ⭐ 새 시스템: 데이터 기반 기즈모 표시 여부
        if (!GetCurrentShowDebugGizmos()) return;
        
        // 공격 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, GetScaledRange());
        
        // ⭐ 새 시스템: 패턴별 기즈모 표시
        MultiShotPattern currentPattern = GetCurrentShotPattern();
        
        // 발사 위치들 표시
        if (cachedPlayer != null)
        {
            switch (currentPattern)
            {
                case MultiShotPattern.Spread:
                    DrawSpreadPatternGizmos();
                    break;
                    
                case MultiShotPattern.Spiral:
                    DrawSpiralPatternGizmos();
                    break;
                    
                case MultiShotPattern.Random:
                    DrawRandomPatternGizmos();
                    break;
            }
        }
        
        // ⭐ 새 시스템: 패턴 정보 표시
        Gizmos.color = Color.white;
        // Unity Scene View에 패턴 정보 텍스트 표시 (Handles 사용 시)
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
            $"Pattern: {currentPattern}\nBursts: {GetCurrentBurstCount()}\nProj/Burst: {GetCurrentProjectilesPerBurst()}");
        #endif
    }

    /// <summary>
    /// 부채꼴 패턴 기즈모 표시
    /// </summary>
    private void DrawSpreadPatternGizmos()
    {
        float startAngle, currentAngle, angleStep, endAngle;
        TargetConeOfInfluence(out startAngle, out currentAngle, out angleStep, out endAngle);
        
        // 각 발사체 위치 및 방향 표시
        for (int i = 0; i < GetCurrentProjectilesPerBurst(); i++)
        {
            Vector2 spawnPos = FindBulletSpawnPos(currentAngle);
            
            // 발사 위치
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnPos, 0.1f);
            
            // 발사 방향
            Vector2 direction = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad));
            Gizmos.color = Color.green;
            Gizmos.DrawRay(spawnPos, direction * 2f);
            
            currentAngle += angleStep;
        }
        
        // 각도 분산 표시
        Gizmos.color = Color.blue;
        Vector2 startDir = new Vector2(Mathf.Cos(startAngle * Mathf.Deg2Rad), Mathf.Sin(startAngle * Mathf.Deg2Rad));
        Vector2 endDir = new Vector2(Mathf.Cos(endAngle * Mathf.Deg2Rad), Mathf.Sin(endAngle * Mathf.Deg2Rad));
        Gizmos.DrawRay(transform.position, startDir * GetScaledRange());
        Gizmos.DrawRay(transform.position, endDir * GetScaledRange());
    }

    /// <summary>
    /// 나선형 패턴 기즈모 표시
    /// </summary>
    private void DrawSpiralPatternGizmos()
    {
        Gizmos.color = Color.cyan;
        
        // 나선형 궤적 표시
        float angleIncrement = 360f / GetCurrentProjectilesPerBurst();
        for (int i = 0; i < GetCurrentProjectilesPerBurst(); i++)
        {
            float angle = i * angleIncrement + (Time.time * 90f); // 시간에 따라 회전
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            Vector2 spawnPos = (Vector2)transform.position + direction * 0.5f;
            
            Gizmos.DrawWireSphere(spawnPos, 0.08f);
            Gizmos.DrawRay(spawnPos, direction * 1.5f);
        }
    }

    /// <summary>
    /// 랜덤 패턴 기즈모 표시
    /// </summary>
    private void DrawRandomPatternGizmos()
    {
        Gizmos.color = Color.magenta;
        
        // 랜덤 영역 표시
        float randomRange = GetCurrentAngleSpread();
        for (int i = 0; i < 8; i++) // 예시로 8개 위치 표시
        {
            float randomAngle = UnityEngine.Random.Range(-randomRange, randomRange);
            Vector2 direction = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad));
            Vector2 spawnPos = (Vector2)transform.position + direction * 0.3f;
            
            Gizmos.DrawWireSphere(spawnPos, 0.06f);
            Gizmos.DrawRay(spawnPos, direction * 1f);
        }
    }
    
    /// <summary>
    /// 복합 공격 전용 디버그 정보
    /// </summary>
    [ContextMenu("Debug MultiShot Attack Info")]
    public void DebugMultiShotAttackInfo()
    {
        string info = $"=== MultiShotRangedAttack {gameObject.name} ===\n";
        info += $"Total Projectiles: {GetTotalProjectileCount()}개\n";
        info += $"Burst Count: {GetCurrentBurstCount()}\n";
        info += $"Projectiles Per Burst: {GetCurrentProjectilesPerBurst()}\n";
        info += $"Angle Spread: {GetCurrentAngleSpread():F1}도\n";
        info += $"Projectile Speed: {GetCurrentProjectileSpeed():F1}\n";
        info += $"Projectile Damage: {GetScaledDamage()}\n";
        info += $"Shot Pattern: {GetCurrentShotPattern()}\n";
        info += $"Is Shooting: {isShooting}\n";
        info += $"Active Bullets: {activeBullets.Count}개\n";
        
        if (AttackData != null)
        {
            info += "\n=== AttackData 정보 ===\n";
            info += AttackData.GetDebugInfo(BaseEnemy?.CurrentLevel ?? 1);
        }
        else
        {
            info += "\n=== Fallback 정보 ===\n";
            info += $"Bullet Prefab: {(bulletPrefab != null ? bulletPrefab.name : "없음")}\n";
            info += $"Burst: {burstCount}, Per Burst: {projectilesPerBurst}\n";
            info += $"Angle: {angleSpread}, Stagger: {stagger}, Oscillate: {oscillate}";
        }
        
        Debug.Log(info);
    }
    
    /// <summary>
    /// 복합 공격 패턴 테스트 (개발용)
    /// </summary>
    [ContextMenu("Test Pattern")]
    public void TestPattern()
    {
        Debug.Log($"[MultiShotRangedAttack] 패턴 테스트:");
        Debug.Log($"  - 현재 패턴: {GetCurrentShotPattern()}");
        Debug.Log($"  - 총 발사체: {GetTotalProjectileCount()}개");
        Debug.Log($"  - 예상 지속시간: {GetCurrentBurstCount() * GetCurrentTimeBetweenBursts() + GetCurrentRestTime():F1}초");
    }
    
    #endregion

    #region ⭐ 새 시스템: 패턴별 방향 계산 메서드들

    /// <summary>
    /// 부채꼴 패턴 방향 계산 (기존 로직)
    /// </summary>
    private float CalculateSpreadDirection(int burstIndex, int projectileIndex)
    {
        float startAngle, currentAngle, angleStep, endAngle;
        TargetConeOfInfluence(out startAngle, out currentAngle, out angleStep, out endAngle);
        
        return currentAngle + (angleStep * projectileIndex);
    }

    /// <summary>
    /// 나선형 패턴 방향 계산
    /// </summary>
    private float CalculateSpiralDirection(int burstIndex, int projectileIndex)
    {
        // 기본 플레이어 방향
        if (cachedPlayer == null) return 0f;
        
        Vector2 playerDirection = (cachedPlayer.transform.position - transform.position).normalized;
        float baseAngle = Mathf.Atan2(playerDirection.y, playerDirection.x) * Mathf.Rad2Deg;
        
        // 나선형 오프셋 (시간과 발사체 인덱스 기반)
        float spiralOffset = (projectileIndex * 45f) + (Time.time * 180f); // 45도씩 간격, 시간에 따라 회전
        
        return baseAngle + spiralOffset;
    }

    /// <summary>
    /// 랜덤 패턴 방향 계산
    /// </summary>
    private float CalculateRandomDirection(int burstIndex, int projectileIndex)
    {
        // 기본 플레이어 방향
        if (cachedPlayer == null) return UnityEngine.Random.Range(0f, 360f);
        
        Vector2 playerDirection = (cachedPlayer.transform.position - transform.position).normalized;
        float baseAngle = Mathf.Atan2(playerDirection.y, playerDirection.x) * Mathf.Rad2Deg;
        
        // 랜덤 오프셋
        float randomOffset = UnityEngine.Random.Range(-GetCurrentAngleSpread(), GetCurrentAngleSpread());
        
        return baseAngle + randomOffset;
    }

    /// <summary>
    /// 패턴별 실제 발사 각도 계산
    /// </summary>
    private float CalculateActualAngle(float baseAngle, int burstIndex, int projectileIndex)
    {
        MultiShotPattern pattern = GetCurrentShotPattern();
        
        switch (pattern)
        {
            case MultiShotPattern.Spread:
                // 기본 각도 그대로 사용 (TargetConeOfInfluence에서 계산됨)
                return baseAngle;
                
            case MultiShotPattern.Spiral:
                // 나선형: 시간에 따라 회전하는 각도
                float spiralOffset = (Time.time * 90f) + (projectileIndex * 20f);
                return spiralOffset % 360f; // 플레이어 방향 무시하고 순수 나선형
                
            case MultiShotPattern.Random:
                // 랜덤: 완전 무작위 방향
                return UnityEngine.Random.Range(0f, 360f);
                
            default:
                return baseAngle;
        }
    }

    #endregion

    #region ⭐ 새 시스템: 복합 공격 설정 데이터 우선순위 메서드들
    
    /// <summary>
    /// 현재 발사 패턴 반환 (데이터 우선순위)
    /// </summary>
    private MultiShotPattern GetCurrentShotPattern()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.ShotPattern;
        }
        
        // 2순위: Inspector 설정 (기존 방식)
        return shotPattern;
    }
    
    /// <summary>
    /// 현재 궤도 곡선 사용 여부 반환 (데이터 우선순위)
    /// </summary>
    private bool GetCurrentUseProjectileArc()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.UseProjectileArc;
        }
        
        // 2순위: Inspector 설정
        return useProjectileArc;
    }
    
    /// <summary>
    /// 현재 화면 흔들림 사용 여부 반환 (데이터 우선순위)
    /// </summary>
    private bool GetCurrentUseScreenShake()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.UseScreenShake;
        }
        
        // 2순위: Inspector 설정
        return useScreenShake;
    }
    
    /// <summary>
    /// 현재 디버그 기즈모 표시 여부 반환 (데이터 우선순위)
    /// </summary>
    private bool GetCurrentShowDebugGizmos()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.ShowDebugGizmos;
        }
        
        // 2순위: Inspector 설정
        return showMultiShotGizmos;
    }
    
    /// <summary>
    /// 현재 순차 발사 여부 반환 (데이터 우선순위)
    /// </summary>
    private bool GetCurrentUseStaggeredFiring()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.UseStaggeredFiring;
        }
        
        // 2순위: Inspector 설정
        return stagger;
    }
    
    #endregion
}

/// <summary>
/// 발사체 상태이상 인터페이스 (확장 가능)
/// </summary>
public interface IProjectileStatusEffect
{
    void SetStatusEffects(System.Collections.Generic.List<StatusEffectData> effects, System.Collections.Generic.List<float> chances);
} 