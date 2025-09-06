using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 22f;
    [SerializeField] private GameObject particleOnHitPrefabVFX;
    [SerializeField] private bool isEnemyProjectile = false;
    [SerializeField] private float projectileRange = 10f;

    // 스킬 레벨별 이펙트 프리팹 배열 (Inspector에서 할당)
    public GameObject[] arrowEffectPrefabs;

    private Vector3 startPosition;
    private bool isReturningToPool = false; // 🔑 중복 반환 방지 플래그
    private bool needsStartPositionUpdate = false; // 🔑 startPosition 업데이트 플래그

    void Start() {
        // 스킬 레벨별 이펙트 적용
        int skillLevel = 0;
        var player = FindObjectOfType<PlayerController>();  // 변경: PlayerController.Instance → FindObjectOfType<PlayerController>()
        if (player != null)
            skillLevel = player.GetSkillLevel("Bow");

        // 🔧 제거: 스킬 레벨에 따른 이펙트 적용 (메서드가 없음)
        // ApplySkillLevelEffects(skillLevel);
    }

    private void Update()
    {
        if (isReturningToPool) return; // 🔑 반환 중이면 업데이트 중단
        
        // ⭐ 핵심 수정: 발사할 때마다 startPosition 업데이트
        if (needsStartPositionUpdate)
        {
            startPosition = transform.position;
            needsStartPositionUpdate = false;
        }
        
        MoveProjectile();
        DetectFireDistance();
    }

    public void UpdateProjectileRange(float projectileRange){
        this.projectileRange = projectileRange;
    }

    public void UpdateMoveSpeed(float moveSpeed)
    {
        this.moveSpeed = moveSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (isReturningToPool) return; // 🔑 이미 반환 중이면 무시
        
        EnemyHealth enemyHealth = other.gameObject.GetComponent<EnemyHealth>();
        Indestructible indestructible = other.gameObject.GetComponent<Indestructible>();
        PlayerHealth player = other.gameObject.GetComponent<PlayerHealth>();

        if (!other.isTrigger && (enemyHealth || indestructible || player)) {

            if ((player && isEnemyProjectile) || (enemyHealth && !isEnemyProjectile))
            {
                // 데미지를 입히는 로직을 PlayerHealth와 EnemyHealth의 OnCollision/OnTrigger가 담당하도록 변경합니다.
                // Projectile은 시각 효과와 소멸만 처리합니다.
                
                // EnemyDamage 컴포넌트에서 데미지 값을 가져와서 적용
                EnemyDamage enemyDamage = GetComponent<EnemyDamage>();
                if (player && isEnemyProjectile && enemyDamage != null) {
                    player.TakeDamage(enemyDamage.damageAmount, transform);
                } else if (enemyHealth && !isEnemyProjectile) {
                    // 플레이어가 쏘는 발사체의 데미지 로직 (필요 시 수정)
                    int playerProjectileDamage = 1; // 예시 데미지
                    enemyHealth.TakeDamage(playerProjectileDamage);
                }

                // 🔑 VFX 생성
                if (particleOnHitPrefabVFX != null)
                {
                    GamePoolManager.Instance.SpawnFromPool(particleOnHitPrefabVFX.name, transform.position, transform.rotation);
                }
                else
                {
                    Debug.LogWarning($"[Projectile] particleOnHitPrefabVFX가 할당되지 않음: {gameObject.name}");
                }

                // 🔑 한 번만 반환
                ReturnProjectileToPool();
                
            } else if (!other.isTrigger && indestructible) {
                // 🔑 VFX 생성
                if (particleOnHitPrefabVFX != null)
                {
                    GamePoolManager.Instance.SpawnFromPool(particleOnHitPrefabVFX.name, transform.position, transform.rotation);
                }
                else
                {
                    Debug.LogWarning($"[Projectile] particleOnHitPrefabVFX가 할당되지 않음: {gameObject.name}");
                }
                
                // 🔑 한 번만 반환
                ReturnProjectileToPool();
            }
        }
            
    }

    private void DetectFireDistance() {
        if (isReturningToPool) return; // 🔑 이미 반환 중이면 무시
        
        float currentDistance = Vector3.Distance(transform.position, startPosition);
        
        if (currentDistance > projectileRange) {
            ReturnProjectileToPool();
        }
    }

    // 🔑 새로운 통합 반환 메서드
    private void ReturnProjectileToPool()
    {
        if (isReturningToPool) return; // 🔑 중복 반환 방지
        
        isReturningToPool = true; // 🔑 반환 중 플래그 설정
        
        if (GamePoolManager.Instance != null)
        {
            GamePoolManager.Instance.ReturnToPool("Arrow", gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // 🔑 풀에서 다시 사용할 때 초기화 - startPosition 업데이트 플래그 설정
    private void OnEnable()
    {
        isReturningToPool = false;
        needsStartPositionUpdate = true; // ⭐ 핵심 수정: 다음 Update에서 startPosition 업데이트하도록 플래그 설정
    }

    private void MoveProjectile()
    {   
        transform.Translate(Vector3.right * Time.deltaTime * moveSpeed);
    }
} 