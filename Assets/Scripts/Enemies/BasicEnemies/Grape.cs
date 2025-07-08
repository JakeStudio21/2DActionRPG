using System.Collections;
using UnityEngine;

public class Grape : MonoBehaviour, IEnemy
{
    [Header("Grape Settings")]
    [SerializeField] private GameObject grapeProjectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpeed = 5f;
    [SerializeField] private AudioClip attackSound;

    private Animator myAnimator;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private EnemyAI enemyAI;
    private PlayerController cachedPlayer;

    readonly int ATTACK_HASH = Animator.StringToHash("Attack");

    private void Awake() 
    {
        myAnimator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (projectileSpawnPoint == null)
        {
            projectileSpawnPoint = transform;
        }
    }

    private void Start()
    {
        // 플레이어 참조 캐싱
        StartCoroutine(FindPlayerCoroutine());
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
        Debug.Log($"[Grape] {gameObject.name}이 플레이어를 찾았습니다.");
    }

    public void Attack(EnemyAI enemyAI) 
    {
        this.enemyAI = enemyAI;
        
        if (myAnimator != null)
        {
            myAnimator.SetTrigger(ATTACK_HASH);
        }

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

        // 공격 사운드 재생
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }

    // 애니메이션 이벤트에서 호출됨
    public void SpawnProjectileAnimEvent() 
    {
        if (grapeProjectilePrefab == null) 
        {
            Debug.LogWarning($"[Grape] {gameObject.name}의 grapeProjectilePrefab이 설정되지 않았습니다.");
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
            Debug.LogWarning($"[Grape] GamePoolManager 사용 실패: {e.Message}");
        }

        // 풀링 실패 시 직접 생성
        if (proj == null)
        {
            proj = Instantiate(grapeProjectilePrefab, spawnPosition, Quaternion.identity);
            Debug.Log("[Grape] 프리팹을 직접 생성했습니다.");
        }

        // 발사체 데미지 설정
        if (proj != null && proj.TryGetComponent(out GrapeProjectile grapeProjectile))
        {
            if (enemyAI != null)
            {
                grapeProjectile.SetDamage(enemyAI.GetProjectileDamage());
            }
            else
            {
                grapeProjectile.SetDamage(1); // 기본 데미지
            }
        }
        
        Debug.Log($"[Grape] 발사체 생성 완료: {proj?.name}");
    }

    // 디버그용 Gizmo
    private void OnDrawGizmosSelected()
    {
        if (projectileSpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(projectileSpawnPoint.position, 0.2f);
        }
    }
} 