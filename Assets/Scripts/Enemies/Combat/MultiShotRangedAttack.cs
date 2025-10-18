using UnityEngine;
using System.Collections.Generic;
using CueSystem;

/// <summary>
/// 멀티샷 원거리 공격 - RangedAttack 기반 단순화 버전
/// ⭐ [Complete Rewrite] 직선형 멀티샷 전용, 불필요한 기능 제거
/// </summary>
public class MultiShotRangedAttack : BaseAttackBehaviour
{
    #region Inspector 설정
    
    [Header("MultiShot Specific Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    
    [Header("Fallback Settings (AttackData 우선)")]
    [Tooltip("발사체 수 (AttackData 없을 때만 사용)")]
    [SerializeField] private int fallbackProjectileCount = 3;
    
    [Tooltip("발사 각도 (AttackData 없을 때만 사용)")]
    [SerializeField] private float fallbackAngleSpread = 30f;
    
    [Tooltip("발사체 속도 (AttackData 없을 때만 사용)")]
    [SerializeField] private float fallbackProjectileSpeed = 10f;
    
    [Tooltip("발사체 수명 (AttackData 없을 때만 사용)")]
    [SerializeField] private float fallbackProjectileLifetime = 5f;
    
    [Header("Advanced Settings")]
    [Tooltip("발사 시작 거리 (몬스터 중심에서)")]
    [SerializeField] private float startingDistance = 0.1f;
    
    [Tooltip("디버그 기즈모 표시")]
    [SerializeField] private bool showDebugGizmos = true;
    
    #endregion
    
    #region Private Fields
    
    // 발사체 추적용
    private List<GameObject> activeProjectiles = new List<GameObject>();
    
    // ⭐ 저장된 공격 방향 (Animation Event 지연 대응)
    private Vector2 savedAttackDirection = Vector2.right;
    
    #endregion
    
    #region BaseAttackBehaviour 추상 메서드 구현
    
    protected override void OnInitialize()
    {
        // 발사 위치가 설정되지 않았으면 자신의 Transform 사용
        if (projectileSpawnPoint == null)
        {
            projectileSpawnPoint = transform;
        }
        
        ValidateMultiShotSettings();
    }
    
    protected override void OnAttack()
    {
        // ⭐ RangedAttack 방식: 월드 좌표를 BlendTree 좌표로 직접 전달
        if (cachedPlayer != null && animationController != null)
        {
            Vector2 toPlayerWorld = (cachedPlayer.transform.position - transform.position).normalized;
            Vector2 toPlayerBlendTree = toPlayerWorld; // 아이소메트릭 변환 제거
            bool shouldFlipX = toPlayerWorld.x < 0;
            
            // ⭐ 방향 저장 (Animation Event에서 사용)
            savedAttackDirection = toPlayerWorld;
            
            animationController.UpdateAttackDirectionWithFlip(toPlayerBlendTree, shouldFlipX);
            
            Debug.Log($"[MultiShotRangedAttack] {gameObject.name} - 공격 방향 저장: World({toPlayerWorld.x:F2}, {toPlayerWorld.y:F2}), flipX: {shouldFlipX}");
        }
    }
    
    protected override void ValidateAttackType()
    {
        if (AttackData != null && AttackData.AttackType != AttackType.Ranged)
        {
            Debug.LogWarning($"[MultiShotRangedAttack] {gameObject.name} - AttackData의 공격 타입이 Ranged가 아닙니다: {AttackData.AttackType}");
        }
    }
    
    protected override int GetFallbackDamage() => 1; // 발사체 자체 데미지 사용
    
    protected override float GetFallbackRange() => 6f; // 원거리는 넓은 범위
    
    #endregion
    
    #region Animation Event 처리
    
    /// <summary>
    /// Animation Event에서 호출되는 멀티샷 발사체 생성
    /// </summary>
    public void SpawnProjectileAnimEvent()
    {
        Debug.Log($"[MultiShotRangedAttack] {gameObject.name} - Animation Event 멀티샷 발사!");
        
        // ✅ Cue 시스템 발행
        EmitMultiShotCues();
        
        // ✅ 사운드 재생
        PlayAttackSound();
        
        // ⭐ 핵심: 멀티샷 발사
        FireMultipleProjectiles();
    }
    
    #endregion
    
    #region 멀티샷 발사 로직 (핵심)
    
    /// <summary>
    /// 여러 발사체를 동시에 발사 (간단한 각도 계산)
    /// </summary>
    private void FireMultipleProjectiles()
    {
        int projectileCount = GetCurrentProjectileCount();
        float angleSpread = GetCurrentAngleSpread();
        GameObject prefabToUse = GetCurrentProjectilePrefab();
        
        if (prefabToUse == null)
        {
            Debug.LogError($"[MultiShotRangedAttack] {gameObject.name}: 발사체 프리팹이 설정되지 않았습니다!");
            return;
        }
        
        Debug.Log($"[MultiShotRangedAttack] 멀티샷 시작: {projectileCount}발, 각도범위: {angleSpread}도");
        
        // ⭐ 플레이어 방향 기준 각도 계산
        float baseAngle = GetTargetAngle();
        
        // ⭐ 각도 계산 (부채꼴 또는 360도 원형)
        if (angleSpread >= 360f)
        {
            // 360도 전방향 발사
            Fire360Degrees(prefabToUse, projectileCount, baseAngle);
        }
        else
        {
            // 부채꼴 발사
            FireSpreadPattern(prefabToUse, projectileCount, angleSpread, baseAngle);
        }
    }
    
    /// <summary>
    /// 부채꼴 패턴 발사
    /// </summary>
    private void FireSpreadPattern(GameObject prefab, int count, float totalSpread, float centerAngle)
    {
        if (count == 1)
        {
            // 1발: 중앙만
            FireSingleProjectile(prefab, centerAngle);
            Debug.Log($"[MultiShotRangedAttack] 단일 발사: {centerAngle:F1}도");
        }
        else
        {
            // 여러 발: 부채꼴
            float startAngle = centerAngle - (totalSpread / 2f);
            float angleStep = totalSpread / (count - 1);
            
            for (int i = 0; i < count; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                FireSingleProjectile(prefab, currentAngle);
                Debug.Log($"[MultiShotRangedAttack] 발사체 {i+1}/{count}: {currentAngle:F1}도");
            }
        }
    }
    
    /// <summary>
    /// 360도 원형 발사
    /// </summary>
    private void Fire360Degrees(GameObject prefab, int count, float startAngle)
    {
        float angleStep = 360f / count;
        
        for (int i = 0; i < count; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            FireSingleProjectile(prefab, currentAngle);
            Debug.Log($"[MultiShotRangedAttack] 360도 발사 {i+1}/{count}: {currentAngle:F1}도");
        }
    }
    
    /// <summary>
    /// 단일 발사체 생성 및 발사
    /// </summary>
    private void FireSingleProjectile(GameObject prefab, float angleInDegrees)
    {
        // 발사 위치 계산
        Vector2 spawnPos = GetSpawnPosition(angleInDegrees);
        
        // 발사체 생성 (풀링)
        GameObject projectile = CreateProjectile(prefab, spawnPos);
        
        if (projectile != null)
        {
            // 발사체 설정 (방향, 속도, 데미지)
            ConfigureProjectile(projectile, angleInDegrees);
            
            // 추적 목록에 추가
            activeProjectiles.Add(projectile);
        }
    }
    
    #endregion
    
    #region 발사체 생성 및 설정
    
    /// <summary>
    /// 발사체 생성 (풀링 시스템)
    /// </summary>
    private GameObject CreateProjectile(GameObject prefab, Vector2 spawnPosition)
    {
        GameObject proj = null;
        
        // GamePoolManager 사용 시도
        try
        {
            if (GamePoolManager.Instance != null)
            {
                string poolTag = GetPoolTagFromPrefab(prefab);
                proj = GamePoolManager.Instance.SpawnFromPool(poolTag, spawnPosition, Quaternion.identity);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MultiShotRangedAttack] GamePoolManager 사용 실패: {e.Message}");
        }
        
        // 풀링 실패 시 직접 생성
        if (proj == null)
        {
            proj = Instantiate(prefab, spawnPosition, Quaternion.identity);
            Debug.Log($"[MultiShotRangedAttack] 프리팹을 직접 생성했습니다: {prefab.name}");
        }
        
        return proj;
    }
    
    /// <summary>
    /// 발사체 설정 (방향, 속도, 데미지)
    /// </summary>
    private void ConfigureProjectile(GameObject projectile, float angleInDegrees)
    {
        // 방향 벡터 계산
        Vector2 direction = AngleToDirection(angleInDegrees);
        
        // 회전 설정
        projectile.transform.right = direction;
        
        // Rigidbody2D로 직선 이동 (핵심!)
        if (projectile.TryGetComponent(out Rigidbody2D rb))
        {
            float speed = GetCurrentProjectileSpeed();
            rb.velocity = direction * speed;
            Debug.Log($"[MultiShotRangedAttack] 발사체 속도 설정: {speed}, 방향: ({direction.x:F2}, {direction.y:F2})");
        }
        
        // 데미지 설정
        SetProjectileDamage(projectile);
        
        // 수명 설정
        float lifetime = GetCurrentProjectileLifetime();
        if (lifetime > 0)
        {
            Destroy(projectile, lifetime);
        }
    }
    
    /// <summary>
    /// 발사체에 데미지 설정
    /// </summary>
    private void SetProjectileDamage(GameObject projectile)
    {
        int currentDamage = GetScaledDamage();
        
        // ⭐ Ghost 전용 발사체 지원
        if (projectile.TryGetComponent(out GhostProjectile ghostProjectile))
        {
            ghostProjectile.SetDamage(currentDamage);
        }
        // ⭐ 인터페이스 기반 설정 (확장성)
        else if (projectile.TryGetComponent(out IProjectileDamage damageComp))
        {
            damageComp.SetDamage(currentDamage);
        }
        // ⭐ Legacy EnemyDamage 지원
        else if (projectile.TryGetComponent(out EnemyDamage enemyDamage))
        {
            enemyDamage.damageAmount = currentDamage;
        }
        
        Debug.Log($"[MultiShotRangedAttack] 발사체 데미지 설정: {currentDamage}");
    }
    
    #endregion
    
    #region 유틸리티 메서드
    
    /// <summary>
    /// 플레이어 방향 각도 계산
    /// </summary>
    private float GetTargetAngle()
    {
        Vector2 direction = savedAttackDirection;
        
        if (cachedPlayer != null)
        {
            direction = (cachedPlayer.transform.position - transform.position).normalized;
        }
        
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }
    
    /// <summary>
    /// 발사 위치 계산
    /// </summary>
    private Vector2 GetSpawnPosition(float angleInDegrees)
    {
        Vector2 basePos = projectileSpawnPoint.position;
        Vector2 offset = AngleToDirection(angleInDegrees) * startingDistance;
        return basePos + offset;
    }
    
    /// <summary>
    /// 각도를 방향 벡터로 변환
    /// </summary>
    private Vector2 AngleToDirection(float angleInDegrees)
    {
        float rad = angleInDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
    
    /// <summary>
    /// 프리팹에서 풀 태그 추출
    /// </summary>
    private string GetPoolTagFromPrefab(GameObject prefab)
    {
        return prefab.name switch
        {
            var name when name.Contains("Ghost") => "Ghost_Bullet",
            var name when name.Contains("Bullet") => "Bullet",
            var name when name.Contains("Arrow") => "Arrow",
            var name when name.Contains("Projectile") => "Projectile",
            _ => prefab.name
        };
    }
    
    #endregion
    
    #region 데이터 기반 속성 계산 (AttackData 우선순위)
    
    /// <summary>
    /// 현재 발사체 프리팹 반환
    /// </summary>
    private GameObject GetCurrentProjectilePrefab()
    {
        // 1순위: AttackData
        if (AttackData != null && AttackData.ProjectilePrefab != null)
        {
            return AttackData.ProjectilePrefab;
        }
        
        // 2순위: Inspector 설정
        return projectilePrefab;
    }
    
    /// <summary>
    /// 현재 발사체 수 반환
    /// </summary>
    private int GetCurrentProjectileCount()
    {
        // 1순위: AttackData
        if (AttackData != null && AttackData.ProjectileCount > 0)
        {
            return AttackData.ProjectileCount;
        }
        
        // 2순위: Inspector 설정
        return fallbackProjectileCount;
    }
    
    /// <summary>
    /// 현재 발사 각도 범위 반환
    /// </summary>
    private float GetCurrentAngleSpread()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.MultiShotAngle;
        }
        
        // 2순위: Inspector 설정
        return fallbackAngleSpread;
    }
    
    /// <summary>
    /// 현재 발사체 속도 반환
    /// </summary>
    private float GetCurrentProjectileSpeed()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.ProjectileSpeed;
        }
        
        // 2순위: Inspector 설정
        return fallbackProjectileSpeed;
    }
    
    /// <summary>
    /// 현재 발사체 수명 반환
    /// </summary>
    private float GetCurrentProjectileLifetime()
    {
        // 1순위: AttackData
        if (AttackData != null)
        {
            return AttackData.ProjectileLifetime;
        }
        
        // 2순위: Inspector 설정
        return fallbackProjectileLifetime;
    }
    
    #endregion
    
    #region Cue 시스템 연동
    
    /// <summary>
    /// 🎵 멀티샷 이펙트 Cue 발행
    /// </summary>
    private void EmitMultiShotCues()
    {
        try
        {
            var context = new CueContext
            {
                position = projectileSpawnPoint.position,
                rotation = projectileSpawnPoint.rotation,
                normal = Vector3.up,
                facingDir = savedAttackDirection,
                follow = null,
                actorType = ActorType.Enemy,
                surfaceType = SurfaceType.Default,
                magnitude = (float)GetCurrentProjectileCount() / 3f,
                isCritical = false,
                scale = 1.0f
            };
            
            string eventKey = "attack.multishot.burst";
            bool success = CueEmitter.Emit(eventKey, "Enemy", context);
            
            Debug.Log($"🎵 [MultiShotRangedAttack] Cue 발행: {eventKey} → {(success ? "성공" : "실패")}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🔴 [MultiShotRangedAttack] Cue 발행 오류: {ex.Message}");
        }
    }
    
    #endregion
    
    #region 검증 및 설정
    
    /// <summary>
    /// 멀티샷 설정 검증
    /// </summary>
    private void ValidateMultiShotSettings()
    {
        if (AttackData != null)
        {
            Debug.Log($"[MultiShotRangedAttack] AttackData 기반 멀티샷 설정:");
            Debug.Log($"  - 공격명: {AttackData.AttackName}");
            Debug.Log($"  - 발사체 수: {GetCurrentProjectileCount()}개");
            Debug.Log($"  - 각도 범위: {GetCurrentAngleSpread():F1}도");
            Debug.Log($"  - 발사체 속도: {GetCurrentProjectileSpeed():F1}");
            Debug.Log($"  - 데미지: {GetScaledDamage()}");
            
            if (GetCurrentProjectileCount() == 1)
            {
                Debug.LogWarning($"[MultiShotRangedAttack] 발사체가 1개뿐입니다. RangedAttack을 사용하는 것이 좋습니다.");
            }
        }
        else
        {
            Debug.Log($"[MultiShotRangedAttack] Fallback 설정 사용:");
            Debug.Log($"  - 발사체: {(projectilePrefab != null ? projectilePrefab.name : "없음")}");
            Debug.Log($"  - 발사체 수: {fallbackProjectileCount}개");
            Debug.Log($"  - 각도: {fallbackAngleSpread}도");
        }
    }
    
    #endregion
    
    #region 디버그 및 시각화
    
    /// <summary>
    /// 멀티샷 범위 시각화 (에디터에서만)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // 공격 범위 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, GetScaledRange());
        
        // 발사 위치들 표시
        if (cachedPlayer != null)
        {
            int count = GetCurrentProjectileCount();
            float angleSpread = GetCurrentAngleSpread();
            float baseAngle = GetTargetAngle();
            
            if (angleSpread >= 360f)
            {
                // 360도 원형 표시
                DrawCirclePattern(count, baseAngle);
            }
            else
            {
                // 부채꼴 표시
                DrawSpreadPattern(count, angleSpread, baseAngle);
            }
        }
        
        // 발사 지점 표시
        if (projectileSpawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(projectileSpawnPoint.position, 0.2f);
        }
    }
    
    private void DrawSpreadPattern(int count, float totalSpread, float centerAngle)
    {
        if (count == 1)
        {
            Vector2 direction = AngleToDirection(centerAngle);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, direction * GetScaledRange());
        }
        else
        {
            float startAngle = centerAngle - (totalSpread / 2f);
            float angleStep = totalSpread / (count - 1);
            
            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + (angleStep * i);
                Vector2 direction = AngleToDirection(angle);
                
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, direction * GetScaledRange());
            }
            
            // 각도 범위 표시
            Gizmos.color = Color.blue;
            Vector2 startDir = AngleToDirection(startAngle);
            Vector2 endDir = AngleToDirection(startAngle + totalSpread);
            Gizmos.DrawRay(transform.position, startDir * GetScaledRange());
            Gizmos.DrawRay(transform.position, endDir * GetScaledRange());
        }
    }
    
    private void DrawCirclePattern(int count, float startAngle)
    {
        float angleStep = 360f / count;
        
        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + (angleStep * i);
            Vector2 direction = AngleToDirection(angle);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, direction * GetScaledRange() * 0.5f);
        }
    }
    
    /// <summary>
    /// 멀티샷 디버그 정보
    /// </summary>
    [ContextMenu("Debug MultiShot Info")]
    public void DebugMultiShotInfo()
    {
        string info = $"=== MultiShotRangedAttack {gameObject.name} ===\n";
        info += $"Projectile Count: {GetCurrentProjectileCount()}개\n";
        info += $"Angle Spread: {GetCurrentAngleSpread():F1}도\n";
        info += $"Projectile Speed: {GetCurrentProjectileSpeed():F1}\n";
        info += $"Projectile Damage: {GetScaledDamage()}\n";
        info += $"Current Range: {GetScaledRange():F1}\n";
        info += $"Active Projectiles: {activeProjectiles.Count}개\n";
        
        if (AttackData != null)
        {
            info += "\n=== AttackData 정보 ===\n";
            info += AttackData.GetDebugInfo(BaseEnemy?.CurrentLevel ?? 1);
        }
        else
        {
            info += "\n=== Fallback 정보 ===\n";
            info += $"Projectile Prefab: {(projectilePrefab != null ? projectilePrefab.name : "없음")}\n";
            info += $"Count: {fallbackProjectileCount}, Angle: {fallbackAngleSpread}도";
        }
        
        Debug.Log(info);
    }
    
    #endregion
}
