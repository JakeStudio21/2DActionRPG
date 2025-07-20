using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 복합 원거리 공격 구현체 - BaseAttackBehaviour 상속으로 중복 코드 제거
/// </summary>
public class MultiShotRangedAttack : BaseAttackBehaviour
{
    [Header("MultiShot Specific Settings")]
    [SerializeField] private GameObject bulletPrefab;
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

    // BaseAttackBehaviour 추상 메서드 구현
    protected override void OnInitialize()
    {
        // MultiShotRangedAttack 전용 초기화 (현재는 없음)
    }

    protected override void OnAttack()
    {
        // 복합 공격 시작
        if (!isShooting)
        {
            StartCoroutine(ShootRoutine());
        }
    }

    // 커스텀 CanAttack 오버라이드
    public override bool CanAttack()
    {
        return !isShooting && base.CanAttack();
    }

    // BaseAttackBehaviour.Attack() 오버라이드
    public override void Attack()
    {
        if (!isShooting) 
        {
            Debug.Log($"[MultiShotRangedAttack] {gameObject.name}: Attack() 메서드 호출됨!");
            
            // BaseAttackBehaviour의 공통 애니메이션 트리거 사용
            TriggerAttackAnimation();
            
            StartCoroutine(ShootRoutine());
            Debug.Log($"[MultiShotRangedAttack] {gameObject.name}: 복합 공격 실행!");
        }
        else
        {
            Debug.Log($"[MultiShotRangedAttack] {gameObject.name}: 이미 공격 중이므로 스킵");
        }
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

    private IEnumerator ShootRoutine() 
    {
        isShooting = true;
        Debug.Log($"[MultiShotRangedAttack] {gameObject.name}: 복합 공격 시작");

        // BaseAttackBehaviour의 공통 사운드 재생 사용
        PlayAttackSound();

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
                    Debug.LogError($"[MultiShotRangedAttack] {gameObject.name}: GamePoolManager가 null입니다!");
                    yield break;
                }

                Vector2 pos = FindBulletSpawnPos(currentAngle);

                // ⭐ 핵심 수정: bulletPrefab의 이름을 풀 태그로 사용
                string poolTag = bulletPrefab != null ? bulletPrefab.name : "Bullet";
                GameObject newBullet = GamePoolManager.Instance.SpawnFromPool(poolTag, pos, Quaternion.identity);
                
                if (newBullet == null)
                {
                    Debug.LogWarning($"[MultiShotRangedAttack] {gameObject.name}: {poolTag} 생성 실패");
                    continue;
                }

                newBullet.transform.right = newBullet.transform.position - transform.position;

                // ⭐ 발사체 자체의 속도와 범위 사용: UpdateMoveSpeed 호출 제거
                if (newBullet.TryGetComponent(out GhostProjectile ghostProjectile))
                {
                    // 기본 데미지 사용 (EnemyAI 의존성 제거)
                    ghostProjectile.SetDamage(1);
                }
                else if (newBullet.TryGetComponent(out Projectile projectile))
                {
                    // Legacy 지원: EnemyDamage 컴포넌트 설정
                    if (newBullet.TryGetComponent(out EnemyDamage enemyDamage))
                    {
                        enemyDamage.damageAmount = 1; // 기본 데미지 사용
                    }
                }

                currentAngle += angleStep;

                if (stagger) { yield return new WaitForSeconds(timeBetweenProjectiles); }
            }

            currentAngle = startAngle;

            if (!stagger) { yield return new WaitForSeconds(timeBetweenBursts); }
        }

        yield return new WaitForSeconds(restTime);
        isShooting = false;
        Debug.Log($"[MultiShotRangedAttack] {gameObject.name}: 복합 공격 완료");
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

    // Animation Event에서 호출할 수 있는 공개 메서드
    public void SpawnProjectileAnimEvent()
    {
        // EnemyAI 없어도 작동
        Attack();
    }
} 