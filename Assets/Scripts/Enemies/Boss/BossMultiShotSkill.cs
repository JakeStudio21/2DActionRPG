using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 나선형 멀티샷 스킬 패턴
/// Ghost의 MultiShotRangedAttack 기반, 나선형(Spiral) 발사 패턴
/// </summary>
public class BossMultiShotSkill : MonoBehaviour
{
    [Header("⚙️ 참조")]
    [SerializeField] private BaseEnemy baseEnemy;
    [SerializeField] private Transform projectileSpawnPoint;
    
    [Header("🌀 나선형 패턴 설정")]
    [Tooltip("발사할 총 탄환 수")]
    [SerializeField] private int spiralProjectileCount = 24;
    
    [Tooltip("나선 회전 속도 (초당 회전 각도)")]
    [SerializeField] private float spiralRotationSpeed = 360f;
    
    [Tooltip("탄환 발사 간격 (초)")]
    [SerializeField] private float fireInterval = 0.05f;
    
    [Tooltip("발사 시작 거리 (보스 중심에서)")]
    [SerializeField] private float startingDistance = 0.5f;
    
    [Header("🎯 발사체 설정")]
    [Tooltip("발사체 프리팹")]
    [SerializeField] private GameObject projectilePrefab;
    
    [Tooltip("발사체 속도")]
    [SerializeField] private float projectileSpeed = 8f;
    
    [Tooltip("발사체 수명 (초)")]
    [SerializeField] private float projectileLifetime = 5f;
    
    [Header("🎮 디버그")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private BossSkillController skillController;
    
    private void Awake()
    {
        if (baseEnemy == null)
            baseEnemy = GetComponent<BaseEnemy>();
        
        if (projectileSpawnPoint == null)
            projectileSpawnPoint = transform;
        
        skillController = GetComponent<BossSkillController>();
    }
    
    /// <summary>
    /// 나선형 멀티샷 스킬 실행 (외부에서 호출)
    /// </summary>
    /// <param name="skillEntry">스킬 엔트리</param>
    /// <param name="targetDirection">타겟 방향 (Cast 시작 시점 저장됨, null이면 현재 플레이어 방향 사용)</param>
    public void Execute(BossSkillEntry skillEntry, Vector3? targetDirection = null)
    {
        if (skillEntry == null || skillEntry.skillData == null)
        {
            Debug.LogError("[BossMultiShotSkill] SkillEntry가 null!");
            return;
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"🌀 [BossMultiShotSkill] {gameObject.name}: 나선형 난사 시작!");
            if (targetDirection.HasValue)
            {
                Debug.Log($"   📍 저장된 방향 사용: {targetDirection.Value}");
            }
            else
            {
                Debug.Log($"   📍 현재 플레이어 방향 사용");
            }
        }
        
        StartCoroutine(SpiralFireRoutine(skillEntry, targetDirection));
    }
    
    /// <summary>
    /// 나선형 발사 코루틴
    /// </summary>
    private IEnumerator SpiralFireRoutine(BossSkillEntry skillEntry, Vector3? targetDirection)
    {
        SkillData skill = skillEntry.skillData;
        float scaleMultiplier = skillEntry.skillScaleMultiplier;
        
        // ⭐ 시작 각도: 저장된 방향 우선, 없으면 현재 플레이어 방향
        float currentAngle = targetDirection.HasValue 
            ? DirectionToAngle(targetDirection.Value) 
            : GetTargetAngle();
        
        // 각도 증가량 (나선 패턴)
        float angleStep = spiralRotationSpeed * fireInterval;
        
        if (enableDebugLogs)
        {
            Debug.Log($"🌀 나선 난사: {spiralProjectileCount}발, 회전속도 {spiralRotationSpeed}도/초");
        }
        
        // 발사체 순차 발사
        for (int i = 0; i < spiralProjectileCount; i++)
        {
            FireSingleProjectile(skillEntry, currentAngle);
            
            // 각도 회전 (나선 효과)
            currentAngle += angleStep;
            
            // 360도 넘으면 조정
            if (currentAngle >= 360f)
                currentAngle -= 360f;
            
            yield return new WaitForSeconds(fireInterval);
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"🌀 나선 난사 완료!");
        }
    }
    
    /// <summary>
    /// 단일 발사체 생성 및 발사
    /// </summary>
    private void FireSingleProjectile(BossSkillEntry skillEntry, float angleInDegrees)
    {
        // 발사체 프리팹 확인
        GameObject prefabToUse = GetProjectilePrefab(skillEntry);
        if (prefabToUse == null)
        {
            Debug.LogError("[BossMultiShotSkill] 발사체 프리팹이 없습니다!");
            return;
        }
        
        // 발사 위치 계산
        Vector2 spawnPos = GetSpawnPosition(angleInDegrees);
        
        // 발사체 생성
        GameObject projectile = CreateProjectile(prefabToUse, spawnPos);
        
        if (projectile != null)
        {
            // 발사체 설정
            ConfigureProjectile(projectile, angleInDegrees, skillEntry);
        }
    }
    
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
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[BossMultiShotSkill] GamePoolManager 사용 실패: {e.Message}");
            }
        }
        
        // 풀링 실패 시 직접 생성
        if (proj == null)
        {
            proj = Instantiate(prefab, spawnPosition, Quaternion.identity);
        }
        
        return proj;
    }
    
    /// <summary>
    /// 발사체 설정 (방향, 속도, 데미지)
    /// </summary>
    private void ConfigureProjectile(GameObject projectile, float angleInDegrees, BossSkillEntry skillEntry)
    {
        // 방향 벡터 계산
        Vector2 direction = AngleToDirection(angleInDegrees);
        
        // 회전 설정
        projectile.transform.right = direction;
        
        // Rigidbody2D로 직선 이동
        if (projectile.TryGetComponent(out Rigidbody2D rb))
        {
            float speed = projectileSpeed * skillEntry.skillScaleMultiplier;
            rb.velocity = direction * speed;
            
            if (enableDebugLogs && Random.value < 0.1f) // 10%만 로그 (너무 많아서)
            {
                Debug.Log($"[BossMultiShotSkill] 발사: 속도 {speed:F1}, 각도 {angleInDegrees:F0}도");
            }
        }
        
        // 데미지 설정
        SetProjectileDamage(projectile, skillEntry);
        
        // 수명 설정
        float lifetime = projectileLifetime;
        if (lifetime > 0)
        {
            Destroy(projectile, lifetime);
        }
    }
    
    /// <summary>
    /// 발사체에 데미지 설정
    /// </summary>
    private void SetProjectileDamage(GameObject projectile, BossSkillEntry skillEntry)
    {
        // 기본 데미지 획득
        int baseDamage = 10;
        
        if (baseEnemy != null)
        {
            var meleeAttack = baseEnemy.GetComponent<MeleeAttack>();
            if (meleeAttack != null && meleeAttack.AttackData != null)
            {
                baseDamage = meleeAttack.GetScaledDamage();
            }
        }
        
        // 스킬 데미지 계산
        SkillData skill = skillEntry.skillData;
        float totalMultiplier = skill.DamageMultiplier * skillEntry.skillScaleMultiplier;
        int skillDamage = Mathf.RoundToInt(baseDamage * totalMultiplier);
        
        // 발사체별 데미지는 더 낮게 (멀티샷이므로)
        int projectileDamage = Mathf.RoundToInt(skillDamage * 0.3f);
        
        // 다양한 데미지 컴포넌트 지원
        if (projectile.TryGetComponent(out IProjectileDamage damageComp))
        {
            damageComp.SetDamage(projectileDamage);
        }
        else if (projectile.TryGetComponent(out EnemyDamage enemyDamage))
        {
            enemyDamage.damageAmount = projectileDamage;
        }
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
    /// 플레이어 방향 각도 계산
    /// </summary>
    private float GetTargetAngle()
    {
        Vector2 direction = Vector2.down;
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            direction = (player.transform.position - transform.position).normalized;
        }
        
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }
    
    /// <summary>
    /// 방향 벡터를 각도로 변환
    /// </summary>
    private float DirectionToAngle(Vector3 direction)
    {
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }
    
    /// <summary>
    /// 발사체 프리팹 가져오기 (SkillData 우선)
    /// </summary>
    private GameObject GetProjectilePrefab(BossSkillEntry skillEntry)
    {
        // SkillData에서 프리팹 확인 (향후 확장용)
        // if (skillEntry.skillData.ProjectilePrefab != null)
        //     return skillEntry.skillData.ProjectilePrefab;
        
        // Inspector 설정 사용
        return projectilePrefab;
    }
    
    /// <summary>
    /// 프리팹에서 풀 태그 추출
    /// </summary>
    private string GetPoolTagFromPrefab(GameObject prefab)
    {
        return prefab.name switch
        {
            var name when name.Contains("Fire") => "BossFire",
            var name when name.Contains("Bullet") => "BossBullet",
            var name when name.Contains("Projectile") => "BossProjectile",
            _ => prefab.name
        };
    }
}

