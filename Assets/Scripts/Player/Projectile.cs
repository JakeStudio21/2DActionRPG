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

    private void Start() {
        startPosition = transform.position;
        // 스킬 레벨별 이펙트 적용
        int skillLevel = 0;
        var player = PlayerController.Instance;
        if (player != null)
            skillLevel = player.GetSkillLevel("Bow");
        if (arrowEffectPrefabs != null && arrowEffectPrefabs.Length > 0)
        {
            int idx = Mathf.Clamp(skillLevel, 0, arrowEffectPrefabs.Length - 1);
            if (arrowEffectPrefabs[idx] != null)
            {
                GamePoolManager.Instance.SpawnFromPool(arrowEffectPrefabs[idx].name, transform.position, Quaternion.identity).transform.SetParent(transform);
            }
        }
        // Debug.Log("발사 위치: " + startPosition); // Projectile 생성 위치 디버깅
    }

    private void Update()
    {
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

                GamePoolManager.Instance.SpawnFromPool(particleOnHitPrefabVFX.name, transform.position, transform.rotation);
                
                // ⭐ 핵심 수정: Destroy 대신 SetActive(false) 사용
                gameObject.SetActive(false);
            } else if (!other.isTrigger && indestructible) {
                GamePoolManager.Instance.SpawnFromPool(particleOnHitPrefabVFX.name, transform.position, transform.rotation);
                
                // ⭐ 핵심 수정: Destroy 대신 SetActive(false) 사용
                gameObject.SetActive(false);
            }
        }
            
    }

    private void DetectFireDistance() {
        if (Vector3.Distance(transform.position, startPosition) > projectileRange) {
            // ⭐ 핵심 수정: Destroy 대신 SetActive(false) 사용
            gameObject.SetActive(false);
        }
    }

    private void MoveProjectile()
    {   
        transform.Translate(Vector3.right * Time.deltaTime * moveSpeed);
    }
}