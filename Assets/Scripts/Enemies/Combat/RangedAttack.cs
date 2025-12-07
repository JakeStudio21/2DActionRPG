using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CueSystem; // ✅ 추가

/// <summary>
/// 원거리 공격 구현체 - BaseAttackBehaviour 상속으로 중복 코드 제거
/// ⭐ [Phase 2] AttackData 기반 확장 지원 (원거리 공격 + 발사체 특화)
/// </summary>
public class RangedAttack : BaseAttackBehaviour
{
    #region 기존 시스템 (100% 유지)
    
    [Header("Ranged Specific Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float predictionFactor = 0.5f;
    
    // 추가 컴포넌트
    private SpriteRenderer spriteRenderer;
    
    #endregion

    #region ⭐ 새 시스템: 원거리 공격 전용 설정
    
    [Header("⭐ 원거리 공격 고급 설정")]
    [Tooltip("발사체 수명 (초) - AttackData 우선")]
    [SerializeField] private float projectileLifetime = 5f;
    
    [Tooltip("발사체 속도 - AttackData 우선")]
    [SerializeField] private float projectileSpeed = 10f;
    
    [Tooltip("조준 정확도 (0=완전 부정확, 1=완전 정확)")]
    [Range(0f, 1f)]
    [SerializeField] private float aimAccuracy = 0.8f;
    
    [Tooltip("발사 시 반동 효과")]
    [SerializeField] private bool useRecoilEffect = true;
    
    [Tooltip("발사 시 머즐 플래시 이펙트")]
    [SerializeField] private GameObject muzzleFlashEffect;
    
    [Tooltip("원거리 공격 디버그 표시")]
    [SerializeField] private bool showRangedGizmos = true;
    
    // 발사체 추적용
    private List<GameObject> activProjectiles = new List<GameObject>();
    
    // ⭐ 저장된 공격 방향 (Animation Event 지연 대응)
    private Vector2 savedAttackDirection = Vector2.right;
    
    #endregion

    #region BaseAttackBehaviour 추상 메서드 구현 (기존 + 확장)

    protected override void OnInitialize()
    {
        // RangedAttack 전용 초기화
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 발사 위치가 설정되지 않았으면 자신의 Transform 사용
        if (projectileSpawnPoint == null)
        {
            projectileSpawnPoint = transform;
        }
        
        // ⭐ 새 시스템: 원거리 공격 데이터 검증
        ValidateRangedSettings();
    }

    protected override void OnAttack()
    {
        // ⭐ BlueSlime 방식 적용: 월드 좌표를 BlendTree 좌표로 직접 전달
        if (cachedPlayer != null && animationController != null)
        {
            Vector2 toPlayerWorld = (cachedPlayer.transform.position - transform.position).normalized;
            Vector2 toPlayerBlendTree = toPlayerWorld; // 아이소메트릭 변환 제거
            bool shouldFlipX = toPlayerWorld.x < 0;
            
            // ⭐ 방향 저장 (Animation Event에서 사용)
            savedAttackDirection = toPlayerWorld;
            
            animationController.UpdateAttackDirectionWithFlip(toPlayerBlendTree, shouldFlipX);
            
            Debug.Log($"[RangedAttack] {gameObject.name} - 공격 방향 저장 및 설정: World({toPlayerWorld.x:F2}, {toPlayerWorld.y:F2}), flipX: {shouldFlipX}");
        }
        
        // ⭐ 새 시스템: 반동 효과
        if (useRecoilEffect)
        {
            ApplyRecoilEffect();
        }
    }
    
    /// <summary>
    /// ⭐ 새 시스템: 공격 타입 검증 (BaseAttackBehaviour에서 요구)
    /// </summary>
    protected override void ValidateAttackType()
    {
        if (AttackData != null && AttackData.AttackType != AttackType.Ranged)
        {
            Debug.LogWarning($"[RangedAttack] {gameObject.name} - AttackData의 공격 타입이 Ranged가 아닙니다: {AttackData.AttackType}");
        }
    }
    
    #endregion

    #region ⭐ 새 시스템: Fallback 메서드들 구현
    
    /// <summary>
    /// AttackData가 없을 때 사용할 기본 데미지 (발사체에서 처리)
    /// </summary>
    protected override int GetFallbackDamage() => 1; // 발사체 자체 데미지 사용
    
    /// <summary>
    /// AttackData가 없을 때 사용할 기본 공격 범위
    /// </summary>
    protected override float GetFallbackRange() => 5f; // 원거리는 넓은 범위
    
    #endregion

    #region ⭐ 새 시스템: 개선된 Animation Event 처리
    
    /// <summary>
    /// Animation Event에서 호출되는 발사체 생성 (완전 새로 구현)
    /// </summary>
    public void SpawnProjectileAnimEvent()
    {
        Debug.Log($"[RangedAttack] {gameObject.name} - Animation Event 발사체 생성!");
        
        // ✅ 🎵 Cue 시스템 추가 - 이 줄을 추가하세요!
        EmitProjectileCues();
        
        // ⭐ 새 시스템: 데이터 기반 발사체 정보 가져오기
        GameObject currentProjectilePrefab = GetCurrentProjectilePrefab();
        if (currentProjectilePrefab == null)
        {
            Debug.LogWarning($"[RangedAttack] {gameObject.name}의 발사체 프리팹이 설정되지 않았습니다.");
            return;
        }
        
        // ⭐ 새 시스템: 머즐 플래시 이펙트
        PlayMuzzleFlashEffect();
        
        // ⭐ 동적 발사 위치 계산 (모든 8방향 대응)
        Vector3 spawnPosition = CalculateDynamicSpawnPosition();
        GameObject proj = CreateProjectile(currentProjectilePrefab, spawnPosition);
        
        if (proj != null)
        {
            // ⭐ 새 시스템: 데이터 기반 발사체 설정
            ConfigureProjectile(proj);
            
            // 활성 발사체 목록에 추가 (추적용)
            activProjectiles.Add(proj);
            
            Debug.Log($"[RangedAttack] 발사체 생성 완료: {proj.name} (데미지: {GetScaledDamage()})");
        }
    }
    
    #endregion

    #region ⭐ 새 시스템: 발사체 관리 로직
    
    /// <summary>
    /// 현재 사용할 발사체 프리팹 반환 (데이터 우선순위)
    /// </summary>
    private GameObject GetCurrentProjectilePrefab()
    {
        // 1순위: AttackData
        if (AttackData != null && AttackData.ProjectilePrefab != null)
        {
            return AttackData.ProjectilePrefab;
        }
        
        // 2순위: Inspector 설정 (기존 방식)
        return projectilePrefab;
    }
    
    /// <summary>
    /// ⭐ 동적 발사 위치 계산 - 플레이어 방식 모방 (8방향 대응)
    /// </summary>
    private Vector3 CalculateDynamicSpawnPosition()
    {
        // 저장된 공격 방향 사용 (OnAttack에서 설정됨)
        Vector2 attackDirection = savedAttackDirection.normalized;
        
        // 발사 거리 계산 (ProjectileSpawnPoint의 로컬 위치 magnitude 사용)
        float spawnDistance = 0.8f; // 기본값
        
        if (projectileSpawnPoint != null)
        {
            // ProjectileSpawnPoint의 로컬 위치 magnitude를 발사 거리로 사용
            Vector3 localPos = projectileSpawnPoint.localPosition;
            spawnDistance = new Vector2(localPos.x, localPos.y).magnitude;
            
            Debug.Log($"[RangedAttack] 발사 거리: {spawnDistance:F2} (ProjectileSpawnPoint 기준)");
        }
        
        // ⭐ 핵심 계산: 몬스터 중심 + (공격 방향 * 거리)
        Vector3 dynamicSpawnPosition = transform.position + (Vector3)(attackDirection * spawnDistance);
        
        Debug.Log($"[RangedAttack] 동적 발사 위치 계산 - 방향: {attackDirection}, 거리: {spawnDistance:F2}, 최종 위치: {dynamicSpawnPosition}");
        
        return dynamicSpawnPosition;
    }
    
    /// <summary>
    /// 발사체 생성 (풀링 시스템 고려)
    /// </summary>
    private GameObject CreateProjectile(GameObject prefab, Vector3 spawnPosition)
    {
        GameObject proj = null;
        
        // GamePoolManager 사용 시도
        try
        {
            if (GamePoolManager.Instance != null)
            {
                // ⭐ 개선: 프리팹 이름 기반 풀링
                string poolTag = GetPoolTagFromPrefab(prefab);
                proj = GamePoolManager.Instance.SpawnFromPool(poolTag, spawnPosition, Quaternion.identity);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[RangedAttack] GamePoolManager 사용 실패: {e.Message}");
        }
        
        // 풀링 실패 시 직접 생성
        if (proj == null)
        {
            proj = Instantiate(prefab, spawnPosition, Quaternion.identity);
            Debug.Log($"[RangedAttack] 프리팹을 직접 생성했습니다: {prefab.name}");
        }
        
        return proj;
    }
    
    /// <summary>
    /// 프리팹에서 풀 태그 추출
    /// </summary>
    private string GetPoolTagFromPrefab(GameObject prefab)
    {
        // 기본적으로 프리팹 이름 사용, 필요시 매핑 테이블 확장 가능
        return prefab.name switch
        {
            var name when name.Contains("Grape") => "Grape Projectile",
            var name when name.Contains("Arrow") => "Arrow",
            var name when name.Contains("Bullet") => "Bullet",
            _ => prefab.name
        };
    }
    
    /// <summary>
    /// 발사체 설정 (데이터 기반)
    /// </summary>
    private void ConfigureProjectile(GameObject projectile)
    {
        // ⭐ 새 발사체: ArcProjectile 우선 처리
        if (projectile.TryGetComponent(out ArcProjectile arcProjectile))
        {
            ConfigureArcProjectile(arcProjectile);
            return;
        }
        
        // ⭐ 새 발사체: StraightProjectile 처리
        if (projectile.TryGetComponent(out StraightProjectile straightProjectile))
        {
            ConfigureStraightProjectile(straightProjectile);
            return;
        }
        
        // 기존: GrapeProjectile 처리
        if (projectile.TryGetComponent(out GrapeProjectile grapeProjectile))
        {
            ConfigureGrapeProjectile(grapeProjectile);
            return;
        }
        
        // 범용 발사체 설정 (fallback)
        ConfigureGenericProjectile(projectile);
    }
    
    /// <summary>
    /// ⭐ ArcProjectile 설정 (신규)
    /// </summary>
    private void ConfigureArcProjectile(ArcProjectile arcProjectile)
    {
        // 목표 위치 계산
        Vector3 targetPosition = cachedPlayer != null ? 
            cachedPlayer.transform.position : 
            transform.position + Vector3.right * 5f;
        
        // 발사
        arcProjectile.LaunchToTarget(targetPosition);
        
        // AttackData 기반 설정
        if (attackData != null)
        {
            arcProjectile.SetDamage(GetScaledDamage());
            arcProjectile.SetMoveSpeed(attackData.ProjectileSpeed);
            arcProjectile.SetArcHeight(attackData.ArcHeight);
            
            // Hit 이펙트 설정
            if (attackData.HitEffect != null)
            {
                arcProjectile.SetHitEffect(attackData.HitEffect);
            }
        }
        
        Debug.Log($"🎯 [RangedAttack] ArcProjectile 설정 완료 - Target: {targetPosition}, Height: {attackData?.ArcHeight ?? 3f}");
    }
    
    /// <summary>
    /// ⭐ StraightProjectile 설정 (신규)
    /// </summary>
    private void ConfigureStraightProjectile(StraightProjectile straightProjectile)
    {
        // 플레이어 방향 계산
        Vector2 direction = cachedPlayer != null ? 
            ((Vector2)(cachedPlayer.transform.position - transform.position)).normalized : 
            Vector2.right;
        
        // 발사
        straightProjectile.SetDirection(direction);
        
        // AttackData 기반 설정
        if (attackData != null)
        {
            straightProjectile.SetDamage(GetScaledDamage());
            straightProjectile.SetMoveSpeed(attackData.ProjectileSpeed);
            straightProjectile.SetProjectileRange(attackData.AttackRange);
            
            // Hit 이펙트 설정
            if (attackData.HitEffect != null)
            {
                straightProjectile.SetHitEffect(attackData.HitEffect);
            }
        }
        
        Debug.Log($"🎯 [RangedAttack] StraightProjectile 설정 완료 - Direction: {direction}, Speed: {attackData?.ProjectileSpeed ?? 10f}");
    }
    
    /// <summary>
    /// Grape 전용 발사체 설정
    /// </summary>
    private void ConfigureGrapeProjectile(GrapeProjectile grapeProjectile)
    {
        // ⭐ 예측 시스템 비활성화: 현재 플레이어 위치 사용 (테스트용)
        Vector3 targetPosition;
        
        if (cachedPlayer != null)
        {
            targetPosition = cachedPlayer.transform.position;
            Debug.Log($"[RangedAttack] Grape 발사체 설정 - 현재 플레이어 위치 사용: {targetPosition}");
        }
        else
        {
            targetPosition = transform.position + Vector3.right * 5f;
            Debug.Log($"[RangedAttack] Grape 발사체 설정 - 플레이어 없음, 기본 방향 사용: {targetPosition}");
        }
        
        grapeProjectile.LaunchToTarget(targetPosition);
        
        // ⭐ 새 시스템: 데이터 기반 데미지 및 속도 설정
        ConfigureProjectileStats(grapeProjectile.gameObject);
        
        Debug.Log($"[RangedAttack] Grape 발사체 설정 완료 - 목표: {targetPosition}");
    }
    
    /// <summary>
    /// 범용 발사체 설정
    /// </summary>
    private void ConfigureGenericProjectile(GameObject projectile)
    {
        // ⭐ 저장된 방향 사용 (애니메이션과 일치)
        Vector3 direction = savedAttackDirection;
        projectile.transform.right = direction;
        
        // Rigidbody2D가 있으면 속도 설정
        if (projectile.TryGetComponent(out Rigidbody2D rb))
        {
            float speed = GetCurrentProjectileSpeed();
            rb.velocity = direction * speed;
        }
        
        // ⭐ 새 시스템: 데이터 기반 스탯 설정
        ConfigureProjectileStats(projectile);
        
        Debug.Log($"[RangedAttack] 범용 발사체 설정 완료 - 저장된 방향 사용: ({direction.x:F2}, {direction.y:F2})");
    }
    
    /// <summary>
    /// 발사체 스탯 설정 (데미지, 속도 등)
    /// </summary>
    private void ConfigureProjectileStats(GameObject projectile)
    {
        int currentDamage = GetScaledDamage();
        float currentSpeed = GetCurrentProjectileSpeed();
        float currentLifetime = GetCurrentProjectileLifetime();
        
        // 발사체에 데미지 설정 시도 (다양한 컴포넌트 지원)
        if (projectile.TryGetComponent(out IProjectileDamage damageComponent))
        {
            damageComponent.SetDamage(currentDamage);
        }
        
        // 발사체에 속도 설정 시도
        if (projectile.TryGetComponent(out IProjectileMovement movementComponent))
        {
            movementComponent.SetSpeed(currentSpeed);
        }
        
        // 수명 설정 (자동 파괴)
        if (currentLifetime > 0)
        {
            Destroy(projectile, currentLifetime);
        }
        
        Debug.Log($"[RangedAttack] 발사체 스탯 설정: 데미지={currentDamage}, 속도={currentSpeed:F1}, 수명={currentLifetime:F1}");
    }
    
    #endregion

    #region ⭐ 새 시스템: 조준 및 예측 시스템
    
    /// <summary>
    /// 조준 정확도를 고려한 방향 계산
    /// </summary>
    private Vector3 GetAdjustedAimDirection()
    {
        if (cachedPlayer == null) return transform.right;
        
        Vector3 perfectDirection = (cachedPlayer.transform.position - transform.position).normalized;
        
        // 조준 정확도가 100%가 아니면 오차 추가
        if (aimAccuracy < 1f)
        {
            float maxError = (1f - aimAccuracy) * 45f; // 최대 45도 오차
            float randomError = Random.Range(-maxError, maxError);
            perfectDirection = Quaternion.Euler(0, 0, randomError) * perfectDirection;
        }
        
        return perfectDirection;
    }
    
    /// <summary>
    /// 플레이어의 이동을 예측한 목표 위치 계산 (개선됨)
    /// </summary>
    public Vector3 GetPredictedPlayerPosition()
    {
        if (cachedPlayer == null)
        {
            return Vector3.zero;
        }
        
        Vector3 currentPlayerPos = cachedPlayer.transform.position;
        
        // 플레이어의 이동 속도 계산
        Vector2 playerVelocity = Vector2.zero;
        if (cachedPlayer.TryGetComponent(out Rigidbody2D playerRb))
        {
            playerVelocity = playerRb.velocity;
        }
        
        // ⭐ 개선: 발사체 속도 기반 도달 시간 계산
        float currentSpeed = GetCurrentProjectileSpeed();
        float distance = Vector3.Distance(transform.position, currentPlayerPos);
        float projectileTravelTime = currentSpeed > 0 ? distance / currentSpeed : 2f;
        
        // 예측 위치 계산
        Vector3 predictedPosition = currentPlayerPos + (Vector3)(playerVelocity * projectileTravelTime * predictionFactor);
        
        return predictedPosition;
    }
    
    /// <summary>
    /// ⭐ 신규: 저장된 방향을 기준으로 플레이어 예측 위치 계산
    /// </summary>
    private Vector3 GetPredictedPlayerPositionFromDirection(Vector2 attackDirection)
    {
        if (cachedPlayer == null)
        {
            // fallback: 저장된 방향 기준 기본 거리
            return transform.position + (Vector3)attackDirection * GetScaledRange();
        }
        
        Vector3 currentPlayerPos = cachedPlayer.transform.position;
        
        // 플레이어의 이동 속도 계산
        Vector2 playerVelocity = Vector2.zero;
        if (cachedPlayer.TryGetComponent(out Rigidbody2D playerRb))
        {
            playerVelocity = playerRb.velocity;
        }
        
        // 저장된 방향 기준으로 거리 계산
        float distance = Vector3.Distance(transform.position, currentPlayerPos);
        float currentSpeed = GetCurrentProjectileSpeed();
        float projectileTravelTime = currentSpeed > 0 ? distance / currentSpeed : 2f;
        
        // 예측 위치 계산
        Vector3 predictedPosition = currentPlayerPos + (Vector3)(playerVelocity * projectileTravelTime * predictionFactor);
        
        Debug.Log($"[RangedAttack] 저장된 방향 기반 예측: 현재({currentPlayerPos.x:F1},{currentPlayerPos.y:F1}) → 예측({predictedPosition.x:F1},{predictedPosition.y:F1})");
        
        return predictedPosition;
    }
    
    #endregion

    #region ⭐ 새 시스템: 데이터 기반 속성 계산
    
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
    
    #endregion

    #region ⭐ 새 시스템: 이펙트 및 사운드
    
    /// <summary>
    /// 머즐 플래시 이펙트 재생
    /// </summary>
    private void PlayMuzzleFlashEffect()
    {
        GameObject effectToPlay = null;
        
        // 1순위: AttackData
        if (AttackData != null && AttackData.AttackStartEffect != null)
        {
            effectToPlay = AttackData.AttackStartEffect;
        }
        // 2순위: Inspector 설정
        else if (muzzleFlashEffect != null)
        {
            effectToPlay = muzzleFlashEffect;
        }
        
        if (effectToPlay != null)
        {
            // ⭐ 동적 발사 위치 사용
            Vector3 effectPosition = CalculateDynamicSpawnPosition();
            Quaternion effectRotation = CalculateProjectileRotation();
            
            GameObject effect = Instantiate(effectToPlay, effectPosition, effectRotation);
            Debug.Log($"[RangedAttack] 머즐 플래시 이펙트 재생: {effectToPlay.name} at {effectPosition}");
        }
    }
    
    /// <summary>
    /// 반동 효과 적용
    /// </summary>
    private void ApplyRecoilEffect()
    {
        if (spriteRenderer != null)
        {
            // 간단한 반동 애니메이션 (스케일 조정)
            StartCoroutine(RecoilAnimation());
        }
    }
    
    /// <summary>
    /// 반동 애니메이션 코루틴
    /// </summary>
    private System.Collections.IEnumerator RecoilAnimation()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 recoilScale = originalScale * 0.9f; // 10% 축소
        
        // 축소
        transform.localScale = recoilScale;
        yield return new WaitForSeconds(0.1f);
        
        // 복원
        transform.localScale = originalScale;
    }
    
    #endregion

    #region ⭐ 새 시스템: 원거리 공격 검증 및 설정
    
    /// <summary>
    /// 원거리 공격 설정 검증
    /// </summary>
    private void ValidateRangedSettings()
    {
        if (AttackData != null)
        {
            Debug.Log($"[RangedAttack] AttackData 기반 원거리 공격 설정:");
            Debug.Log($"  - 공격명: {AttackData.AttackName}");
            Debug.Log($"  - 기본 데미지: {AttackData.BaseDamage} → 스케일된 데미지: {GetScaledDamage()}");
            Debug.Log($"  - 발사체 속도: {AttackData.ProjectileSpeed}");
            Debug.Log($"  - 발사체 수명: {AttackData.ProjectileLifetime}초");
            Debug.Log($"  - 상태이상 개수: {AttackData.OnHitEffects.Count}개");
            
            // 원거리 공격 검증
            if (AttackData.ProjectilePrefab == null && projectilePrefab == null)
            {
                Debug.LogError($"[RangedAttack] 발사체 프리팹이 설정되지 않았습니다!");
            }
            
            if (AttackData.ProjectileSpeed <= 0)
            {
                Debug.LogWarning($"[RangedAttack] 발사체 속도가 0 이하입니다: {AttackData.ProjectileSpeed}");
            }
        }
        else
        {
            Debug.Log($"[RangedAttack] 기존 방식 사용:");
            Debug.Log($"  - 발사체: {(projectilePrefab != null ? projectilePrefab.name : "없음")}");
            Debug.Log($"  - 속도: {projectileSpeed}");
            Debug.Log($"  - 예측 계수: {predictionFactor}");
        }
    }
    
    #endregion

    #region 기존 시스템 호환성 유지
    
    /// <summary>
    /// 발사체 스폰 포인트 반환 (기존 호환성 유지)
    /// </summary>
    public Transform GetProjectileSpawnPoint()
    {
        return projectileSpawnPoint;
    }
    
    #endregion

    #region ⭐ 디버그 및 시각화
    
    /// <summary>
    /// 원거리 공격 범위 시각화 (에디터에서만)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showRangedGizmos) return;
        
        // 공격 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, GetScaledRange());
        
        // ⭐ 동적 발사 위치 표시 (Play 모드에서만)
        Vector3 displaySpawnPosition;
        if (Application.isPlaying && savedAttackDirection.magnitude > 0.1f)
        {
            // Play 모드: 동적 계산된 위치 표시
            displaySpawnPosition = CalculateDynamicSpawnPosition();
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(displaySpawnPosition, 0.25f);
            
            // 몬스터 중심에서 발사 위치까지 선 그리기
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, displaySpawnPosition);
        }
        else if (projectileSpawnPoint != null)
        {
            // 에디터 모드: 기존 SpawnPoint 위치 표시
            displaySpawnPosition = projectileSpawnPoint.position;
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(displaySpawnPosition, 0.2f);
        }
        else
        {
            displaySpawnPosition = transform.position;
        }
        
        // 발사 방향 표시
        if (cachedPlayer != null)
        {
            Vector3 direction = GetAdjustedAimDirection();
            Gizmos.color = Color.green;
            Gizmos.DrawRay(displaySpawnPosition, direction * GetScaledRange());
            
            // 예측 위치 표시
            Vector3 predictedPos = GetPredictedPlayerPosition();
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(predictedPos, 0.3f);
            Gizmos.DrawLine(displaySpawnPosition, predictedPos);
        }
    }
    
    /// <summary>
    /// 원거리 공격 전용 디버그 정보
    /// </summary>
    [ContextMenu("Debug Ranged Attack Info")]
    public void DebugRangedAttackInfo()
    {
        string info = $"=== RangedAttack {gameObject.name} ===\n";
        info += $"Current Damage: {GetScaledDamage()}\n";
        info += $"Current Range: {GetScaledRange():F1}\n";
        info += $"Current Cooldown: {GetScaledCooldown():F1}s\n";
        info += $"Projectile Speed: {GetCurrentProjectileSpeed():F1}\n";
        info += $"Projectile Lifetime: {GetCurrentProjectileLifetime():F1}s\n";
        info += $"Aim Accuracy: {aimAccuracy * 100:F0}%\n";
        info += $"Prediction Factor: {predictionFactor:F1}\n";
        info += $"Active Projectiles: {activProjectiles.Count}개\n";
        
        // ⭐ 동적 발사 위치 정보 추가
        info += "\n=== 발사 위치 시스템 ===\n";
        info += $"Saved Attack Direction: {savedAttackDirection}\n";
        if (projectileSpawnPoint != null)
        {
            float spawnDistance = new Vector2(projectileSpawnPoint.localPosition.x, projectileSpawnPoint.localPosition.y).magnitude;
            info += $"Spawn Distance: {spawnDistance:F2}\n";
            info += $"Dynamic Spawn Position: {CalculateDynamicSpawnPosition()}\n";
        }
        else
        {
            info += "ProjectileSpawnPoint: 없음\n";
        }
        
        if (AttackData != null)
        {
            info += "\n=== AttackData 정보 ===\n";
            info += AttackData.GetDebugInfo(BaseEnemy?.CurrentLevel ?? 1);
        }
        else
        {
            info += "\n=== Fallback 정보 ===\n";
            info += $"Projectile Prefab: {(projectilePrefab != null ? projectilePrefab.name : "없음")}\n";
            info += $"Speed: {projectileSpeed}\n";
            info += $"Lifetime: {projectileLifetime}";
        }
        
        Debug.Log(info);
    }
    
    /// <summary>
    /// 예측 조준 테스트 (개발용)
    /// </summary>
    [ContextMenu("Test Prediction")]
    public void TestPrediction()
    {
        if (cachedPlayer == null)
        {
            Debug.LogWarning("[RangedAttack] 플레이어를 찾을 수 없습니다.");
            return;
        }
        
        Vector3 currentPos = cachedPlayer.transform.position;
        Vector3 predictedPos = GetPredictedPlayerPosition();
        float distance = Vector3.Distance(currentPos, predictedPos);
        
        Debug.Log($"[RangedAttack] 예측 조준 테스트:");
        Debug.Log($"  - 현재 플레이어 위치: {currentPos}");
        Debug.Log($"  - 예측 위치: {predictedPos}");
        Debug.Log($"  - 예측 거리: {distance:F1}");
        Debug.Log($"  - 예측 계수: {predictionFactor}");
    }
    
    #endregion

    #region ✅ 🎵 Cue 시스템 연동 (Phase B-1 추가)
    
    /// <summary>
    /// 🎵 발사체 이펙트 Cue 발행
    /// </summary>
    private void EmitProjectileCues()
    {
        try
        {
            // ⭐ 동적 발사 위치 사용
            Vector3 dynamicPosition = CalculateDynamicSpawnPosition();
            
            // CueContext 생성
            var context = new CueContext
            {
                position = dynamicPosition,
                rotation = CalculateProjectileRotation(),
                normal = Vector3.up,
                facingDir = GetAimDirection(),
                follow = null,
                actorType = ActorType.Enemy,
                surfaceType = SurfaceType.Default,
                magnitude = GetCurrentProjectileSpeed() / 10f,
                isCritical = false, // ✅ 수정: isCrit → isCritical
                scale = 1.0f
            };
            
            // 이벤트 키
            string eventKey = "attack.ranged.fire";
            
            // Cue 발행
            bool success = CueEmitter.Emit(eventKey, "Enemy", context);
            
            Debug.Log($"🎵 [RangedAttack] Cue 발행: {eventKey} → {(success ? "성공" : "실패")}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🔴 [RangedAttack] Cue 발행 오류: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 🧭 조준 방향 계산
    /// </summary>
    private Vector2 GetAimDirection()
    {
        if (cachedPlayer == null) return Vector2.right;
        
        Vector2 direction = (cachedPlayer.transform.position - projectileSpawnPoint.position).normalized;
        return direction;
    }
    
    /// <summary>
    /// 🎯 발사체 회전 계산
    /// </summary>
    private Quaternion CalculateProjectileRotation()
    {
        Vector2 aimDirection = GetAimDirection();
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        return Quaternion.AngleAxis(angle, Vector3.forward);
    }
    
    #endregion
}

/// <summary>
/// 발사체 데미지 인터페이스 (확장 가능)
/// </summary>
public interface IProjectileDamage
{
    void SetDamage(int damage);
}

/// <summary>
/// 발사체 움직임 인터페이스 (확장 가능)
/// </summary>
public interface IProjectileMovement
{
    void SetSpeed(float speed);
} 