using System.Collections;
using UnityEngine;

/// <summary>
/// 원거리 공격 구현체 (Grape용)
/// MonoBehaviour 컴포넌트로 구현하여 Inspector에서 설정 가능
/// 기존 Grape.cs의 기능을 통합
/// </summary>
public class RangedAttack : MonoBehaviour, IAttackBehaviour
{
    [Header("Ranged Attack Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float predictionFactor = 0.5f;
    [SerializeField] private bool stopMovingWhileAttacking = true;
    [SerializeField] private AudioClip attackSound;
    
    private EnemyAI cachedEnemyAI;
    private Animator animator;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    private PlayerController cachedPlayer;
    private bool canAttack = true;
    
    readonly int ATTACK_HASH = Animator.StringToHash("Attack");
    
    private void Awake()
    {
        // 발사 위치가 설정되지 않았으면 자신의 Transform 사용
        if (projectileSpawnPoint == null)
        {
            projectileSpawnPoint = transform;
        }
    }
    
    public void Initialize(EnemyAI enemyAI)
    {
        cachedEnemyAI = enemyAI;
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        canAttack = true;
        
        // 플레이어 참조 캐싱
        StartCoroutine(FindPlayerCoroutine());
        
        Debug.Log($"[RangedAttack] {enemyAI.gameObject.name} 원거리 공격 시스템 초기화 완료");
    }
    
    private IEnumerator FindPlayerCoroutine()
    {
        while (cachedPlayer == null)
        {
            cachedPlayer = FindObjectOfType<PlayerController>();
            if (cachedPlayer == null)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        Debug.Log($"[RangedAttack] {cachedEnemyAI.gameObject.name}이 플레이어를 찾았습니다.");
    }
    
    public void Attack(EnemyAI enemyAI)
    {
        if (!CanAttack()) return;
        
        canAttack = false;
        
        // 플레이어 방향으로 스프라이트 회전
        if (cachedPlayer != null && spriteRenderer != null)
        {
            if (transform.position.x - cachedPlayer.transform.position.x < 0)
            {
                spriteRenderer.flipX = false;
            }
            else
            {
                spriteRenderer.flipX = true;
            }
        }
        
        // 애니메이션 트리거
        if (animator != null)
        {
            animator.SetTrigger(ATTACK_HASH);
        }
        
        // 공격 사운드 재생
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
        
        Debug.Log($"[RangedAttack] {gameObject.name} 원거리 공격 실행!");
        
        // 공격 쿨다운 시작
        StartCoroutine(AttackCooldownRoutine(enemyAI.GetAttackCooldown()));
    }
    
    public bool CanAttack()
    {
        return canAttack;
    }
    
    public bool ShouldStopMovingWhileAttacking()
    {
        return stopMovingWhileAttacking;
    }
    
    private IEnumerator AttackCooldownRoutine(float cooldown)
    {
        yield return new WaitForSeconds(cooldown);
        canAttack = true;
    }
    
    /// <summary>
    /// Animation Event에서 호출되는 발사체 생성
    /// 기존 Grape.SpawnProjectileAnimEvent() 메서드를 대체
    /// </summary>
    public void SpawnProjectileAnimEvent()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[RangedAttack] {cachedEnemyAI.gameObject.name}의 projectilePrefab이 설정되지 않았습니다.");
            return;
        }
        
        Vector3 spawnPosition = projectileSpawnPoint.position;
        
        // GamePoolManager 사용 시도, 실패하면 Instantiate 사용
        GameObject proj = null;
        
        try
        {
            if (GamePoolManager.Instance != null)
            {
                proj = GamePoolManager.Instance.SpawnFromPool("GrapeProjectile", spawnPosition, Quaternion.identity);
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
            // ⭐ 개선: 예측 조준으로 정확도 향상
            Vector3 targetPosition = GetPredictedPlayerPosition();
            grapeProjectile.LaunchToTarget(targetPosition);
            
            Debug.Log($"[RangedAttack] 예측 조준: 목표 위치 {targetPosition}");
        }
        
        Debug.Log($"[RangedAttack] 발사체 생성 완료: {proj?.name}");
    }
    
    /// <summary>
    /// 플레이어의 이동을 예측한 목표 위치 계산
    /// </summary>
    private Vector3 GetPredictedPlayerPosition()
    {
        if (cachedPlayer == null)
        {
            return cachedEnemyAI.transform.position + Vector3.right * 5f; // 기본 방향
        }
        
        Vector3 currentPlayerPos = cachedPlayer.transform.position;
        
        // 플레이어의 이동 속도 계산 (Rigidbody2D 사용)
        Vector2 playerVelocity = Vector2.zero;
        if (cachedPlayer.TryGetComponent(out Rigidbody2D playerRb))
        {
            playerVelocity = playerRb.velocity;
        }
        
        // 발사체 도달 시간 (GrapeProjectile의 moveSpeed와 일치)
        float projectileTravelTime = 2f;
        
        // 예측 위치 = 현재 위치 + (속도 * 시간 * 예측 계수)
        Vector3 predictedPosition = currentPlayerPos + (Vector3)(playerVelocity * projectileTravelTime * predictionFactor);
        
        // Debug.Log($"[RangedAttack] 플레이어 현재위치: {currentPlayerPos}, 속도: {playerVelocity}, 예측위치: {predictedPosition}");
        
        return predictedPosition;
    }
    
    // 디버그용 Gizmo - 항상 표시 (Blue_slime 패턴 적용)
    private void OnDrawGizmos()
    {
        // 발사 위치 시각화 (빨간색 구체)
        if (projectileSpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(projectileSpawnPoint.position, 0.3f);
            
            // 원거리 공격 범위 시각화 (반투명 빨간색)
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 3.5f); // 원거리 공격 범위
        }
        
        // 런타임 중 예측 조준선 표시
        if (Application.isPlaying && cachedPlayer != null)
        {
            Vector3 predictedPos = GetPredictedPlayerPosition();
            
            // 조준선 (노란색)
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(projectileSpawnPoint.position, predictedPos);
            
            // 예측 위치 (노란색 큐브)
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(predictedPos, Vector3.one * 0.5f);
        }
        
        #if UNITY_EDITOR
        // 몬스터 타입 정보 표시
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, "Grape (원거리)");
        }
        #endif
    }
} 