using UnityEngine;

/// <summary>
/// 원거리 공격 구현체 - BaseAttackBehaviour 상속으로 중복 코드 제거
/// </summary>
public class RangedAttack : BaseAttackBehaviour
{
    [Header("Ranged Specific Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float predictionFactor = 0.5f;
    
    // 추가 컴포넌트
    private SpriteRenderer spriteRenderer;
    
    // BaseAttackBehaviour 추상 메서드 구현
    protected override void OnInitialize()
    {
        // RangedAttack 전용 초기화
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 발사 위치가 설정되지 않았으면 자신의 Transform 사용
        if (projectileSpawnPoint == null)
        {
            projectileSpawnPoint = transform;
        }
    }

    protected override void OnAttack()
    {
        // 플레이어 방향으로 스프라이트 회전
        if (cachedPlayer != null && spriteRenderer != null)
        {
            spriteRenderer.flipX = (transform.position.x - cachedPlayer.transform.position.x >= 0);
        }
    }
    
    /// <summary>
    /// Animation Event에서 호출되는 발사체 생성
    /// </summary>
    public void SpawnProjectileAnimEvent()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[RangedAttack] {gameObject.name}의 projectilePrefab이 설정되지 않았습니다.");
            return;
        }
        
        Vector3 spawnPosition = projectileSpawnPoint.position;
        GameObject proj = null;
        
        // GamePoolManager 사용 시도
        try
        {
            if (GamePoolManager.Instance != null)
            {
                proj = GamePoolManager.Instance.SpawnFromPool("Grape Projectile", spawnPosition, Quaternion.identity);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[RangedAttack] GamePoolManager 사용 실패: {e.Message}");
        }
        
        // 풀링 실패 시 직접 생성
        if (proj == null)
        {
            proj = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            Debug.Log("[RangedAttack] 프리팹을 직접 생성했습니다.");
        }
        
        // 발사체 설정 및 예측 조준
        if (proj != null && proj.TryGetComponent(out GrapeProjectile grapeProjectile))
        {
            Vector3 targetPosition = GetPredictedPlayerPosition();
            grapeProjectile.LaunchToTarget(targetPosition);
            Debug.Log($"[RangedAttack] 예측 조준: 목표 위치 {targetPosition}");
        }
        
        Debug.Log($"[RangedAttack] 발사체 생성 완료: {proj?.name}");
    }
    
    /// <summary>
    /// 플레이어의 이동을 예측한 목표 위치 계산
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
        
        // 발사체 도달 시간
        float projectileTravelTime = 2f;
        
        // 예측 위치 계산
        Vector3 predictedPosition = currentPlayerPos + (Vector3)(playerVelocity * projectileTravelTime * predictionFactor);
        
        return predictedPosition;
    }
    
    /// <summary>
    /// 발사체 스폰 포인트 반환
    /// </summary>
    public Transform GetProjectileSpawnPoint()
    {
        return projectileSpawnPoint;
    }
} 