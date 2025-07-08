using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ghost 적의 원거리 공격 행동을 담당하는 스크립트
/// 플레이어를 향해 투사체를 발사하는 기능 제공
/// </summary>
public class Ghost : MonoBehaviour, IEnemy
{
    [Header("Attack Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletMoveSpeed = 5f;
    [SerializeField] private int burstCount = 3;
    [SerializeField] private int projectilesPerBurst = 1;
    [SerializeField][Range(0, 359)] private float angleSpread = 0f;
    [SerializeField] private float startingDistance = 0.1f;
    [SerializeField] private float timeBetweenBursts = 0.5f;
    [SerializeField] private float restTime = 1f;
    
    [Header("Advanced Settings")]
    [SerializeField] private bool stagger = false;
    [Tooltip("Stagger must be enabled for oscillate to function properly.")]
    [SerializeField] private bool oscillate = false;

    private bool isShooting = false;
    private EnemyAI enemyAI;

    private void Start()
    {
        // 컴포넌트 초기화 확인
        if (enemyAI == null)
        {
            enemyAI = GetComponent<EnemyAI>();
        }
        
        Debug.Log($"[Ghost] {gameObject.name} 초기화 완료");
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
        if (bulletMoveSpeed <= 0) { bulletMoveSpeed = 0.1f; }
    }

    public void Attack(EnemyAI enemyAI) 
    {
        // null 체크 추가
        if (enemyAI == null)
        {
            Debug.LogWarning($"[Ghost] {gameObject.name}: EnemyAI가 null입니다!");
            return;
        }
        
        this.enemyAI = enemyAI;
        if (!isShooting) 
        {
            StartCoroutine(ShootRoutine());
        }
        else
        {
            Debug.Log($"[Ghost] {gameObject.name}: 이미 공격 중입니다.");
        }
    }

    private IEnumerator ShootRoutine() 
    {
        isShooting = true;
        Debug.Log($"[Ghost] {gameObject.name}: 공격 시작");

        float startAngle, currentAngle, angleStep, endAngle;
        float timeBetweenProjectiles = 0f;

        TargetConeOfInfluence(out startAngle, out currentAngle, out angleStep, out endAngle);

        if (stagger) { timeBetweenProjectiles = timeBetweenBursts / projectilesPerBurst; }        

        for (int i = 0; i < burstCount; i++)
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

            for (int j = 0; j < projectilesPerBurst; j++)
            {
                // GamePoolManager null 체크
                if (GamePoolManager.Instance == null)
                {
                    Debug.LogError($"[Ghost] {gameObject.name}: GamePoolManager가 null입니다!");
                    yield break;
                }

                Vector2 pos = FindBulletSpawnPos(currentAngle);

                GameObject newBullet = GamePoolManager.Instance.SpawnFromPool("Bullet", pos, Quaternion.identity);
                
                if (newBullet == null)
                {
                    Debug.LogWarning($"[Ghost] {gameObject.name}: 총알 생성 실패");
                    continue;
                }

                newBullet.transform.right = newBullet.transform.position - transform.position;

                // EnemyDamage 컴포넌트 설정
                if (newBullet.TryGetComponent(out EnemyDamage enemyDamage))
                {
                    if (enemyAI != null)
                    {
                        enemyDamage.damageAmount = enemyAI.GetProjectileDamage();
                    }
                }

                // Projectile 컴포넌트 설정
                if (newBullet.TryGetComponent(out Projectile projectile))
                {
                    projectile.UpdateMoveSpeed(bulletMoveSpeed);
                }

                currentAngle += angleStep;

                if (stagger) { yield return new WaitForSeconds(timeBetweenProjectiles); }
            }

            currentAngle = startAngle;

            if (!stagger) { yield return new WaitForSeconds(timeBetweenBursts); }
        }

        yield return new WaitForSeconds(restTime);
        isShooting = false;
        Debug.Log($"[Ghost] {gameObject.name}: 공격 완료");
    }

    private void TargetConeOfInfluence(out float startAngle, out float currentAngle, out float angleStep, out float endAngle)
    {
        // PlayerController null 체크
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning($"[Ghost] {gameObject.name}: PlayerController를 찾을 수 없습니다!");
            startAngle = 0f;
            endAngle = 0f;
            currentAngle = 0f;
            angleStep = 0f;
            return;
        }

        Vector2 targetDirection = playerController.transform.position - transform.position;
        float targetAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        startAngle = targetAngle;
        endAngle = targetAngle;
        currentAngle = targetAngle;
        float halfAngleSpread = 0f;
        angleStep = 0;
        
        if (angleSpread != 0)
        {
            angleStep = angleSpread / (projectilesPerBurst - 1);
            halfAngleSpread = angleSpread / 2f;
            startAngle = targetAngle - halfAngleSpread;
            endAngle = targetAngle + halfAngleSpread;
            currentAngle = startAngle;
        }
    }

    private Vector2 FindBulletSpawnPos(float currentAngle) 
    {
        float x = transform.position.x + startingDistance * Mathf.Cos(currentAngle * Mathf.Deg2Rad);
        float y = transform.position.y + startingDistance * Mathf.Sin(currentAngle * Mathf.Deg2Rad);

        Vector2 pos = new Vector2(x, y);

        return pos;
    }
} 