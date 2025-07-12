using System.Collections;
using UnityEngine;

public class GrapeProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private AnimationCurve animCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float heightY = 3f;
    
    [Header("Effects")]
    [SerializeField] private GameObject shadowPrefab;
    [SerializeField] private GameObject splatterPrefab;
    [SerializeField] private AudioClip impactSound;
    
    [Header("Damage")]
    [SerializeField] private LayerMask playerLayerMask = 1 << 3; // Player layer
    [SerializeField] private int projectileDamage = 10;
    
    private Vector3 targetPosition;
    private Vector3 startPosition;
    private bool isLaunched = false;
    private GameObject activeShadow;
    private AudioSource audioSource;
    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;
    private PlayerController cachedPlayer;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();
        circleCollider = GetComponent<CircleCollider2D>();
        
        // Rigidbody2D 설정
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.isKinematic = true;
        }
        
        // 콜라이더 설정
        if (circleCollider != null)
        {
            circleCollider.isTrigger = true;
        }
    }
    
    private void Start()
    {
        // 플레이어 참조 찾기
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
    }
    
    private void OnEnable()
    {
        isLaunched = false;
        
        // 0.1초 후 발사 시작 (풀링 시스템 호환)
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(DelayedLaunch());
        }
    }
    
    private void OnDisable()
    {
        StopAllCoroutines();
        
        // ⭐ 핵심 수정: 그림자 정리 시 GamePoolManager 사용
        if (activeShadow != null)
        {
            if (Application.isPlaying)
            {
                // Destroy 대신 SetActive(false) 사용
                activeShadow.SetActive(false);
            }
            activeShadow = null;
        }
        
        isLaunched = false;
    }
    
    public void SetDamage(int newDamage)
    {
        projectileDamage = newDamage;
    }
    
    public void LaunchToTarget(Vector3 target)
    {
        targetPosition = target;
        startPosition = transform.position;
        isLaunched = true;
        
        CreateShadow();
        StartCoroutine(ProjectileMotionCoroutine());
    }
    
    private IEnumerator DelayedLaunch()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (gameObject.activeInHierarchy && !isLaunched)
        {
            // 플레이어 위치를 목표로 설정
            if (cachedPlayer != null)
            {
                LaunchToTarget(cachedPlayer.transform.position);
            }
            else
            {
                // 플레이어를 찾을 수 없으면 앞쪽으로 발사
                LaunchToTarget(transform.position + Vector3.right * 5f);
            }
        }
    }
    
    private void CreateShadow()
    {
        if (shadowPrefab != null)
        {
            Vector3 shadowStartPos = startPosition + Vector3.down * 0.3f;
            
            // ⭐ 핵심 수정: Instantiate 대신 GamePoolManager 사용
            if (GamePoolManager.Instance != null)
            {
                activeShadow = GamePoolManager.Instance.SpawnFromPool("GrapeShadow", shadowStartPos, Quaternion.identity);
                
                if (activeShadow != null)
                {
                    StartCoroutine(MoveShadowCoroutine());
                    Debug.Log("[GrapeProjectile] GamePoolManager에서 GrapeShadow 생성 성공");
                }
                else
                {
                    Debug.LogError("[GrapeProjectile] GamePoolManager에서 GrapeShadow 생성 실패!");
                }
            }
            else
            {
                // 백업: GamePoolManager가 없으면 직접 생성
                activeShadow = Instantiate(shadowPrefab, shadowStartPos, Quaternion.identity);
                Debug.LogWarning("[GrapeProjectile] GamePoolManager가 없어서 직접 생성했습니다.");
                
                if (activeShadow != null)
                {
                    StartCoroutine(MoveShadowCoroutine());
                }
            }
        }
    }
    
    private IEnumerator ProjectileMotionCoroutine()
    {
        float timePassed = 0f;
        
        while (timePassed < moveSpeed && isLaunched)
        {
            if (!gameObject.activeInHierarchy) yield break;
            
            timePassed += Time.deltaTime;
            float linearT = timePassed / moveSpeed;
            
            // 높이 계산
            float heightT = animCurve.Evaluate(linearT);
            float currentHeight = Mathf.Lerp(0f, heightY, heightT);
            
            // 위치 계산
            Vector3 horizontalPos = Vector3.Lerp(startPosition, targetPosition, linearT);
            transform.position = horizontalPos + Vector3.up * currentHeight;
            
            yield return null;
        }
        
        // 착지 처리
        if (isLaunched && gameObject.activeInHierarchy)
        {
            OnProjectileLand();
        }
    }
    
    private IEnumerator MoveShadowCoroutine()
    {
        if (activeShadow == null) yield break;
        
        float timePassed = 0f;
        Vector3 shadowStart = activeShadow.transform.position;
        Vector3 shadowEnd = targetPosition + Vector3.down * 0.3f;
        
        while (timePassed < moveSpeed && activeShadow != null)
        {
            timePassed += Time.deltaTime;
            float linearT = timePassed / moveSpeed;
            
            activeShadow.transform.position = Vector3.Lerp(shadowStart, shadowEnd, linearT);
            
            yield return null;
        }
        
        // ⭐ 핵심 수정: 그림자 제거 시 GamePoolManager 사용
        if (activeShadow != null)
        {
            // Destroy 대신 SetActive(false) 사용
            activeShadow.SetActive(false);
            activeShadow = null;
            Debug.Log("[GrapeProjectile] GrapeShadow를 GamePoolManager에 반환");
        }
    }
    
    private void OnProjectileLand()
    {
        // 착지 위치를 정확히 설정
        transform.position = targetPosition;
        
        // ⭐ 핵심 변경: 발사체가 직접 데미지 처리 (먼저)
        DealDamageToPlayer();
        
        // 착지음 재생
        if (audioSource != null && impactSound != null)
        {
            audioSource.PlayOneShot(impactSound);
        }
        
        // 순수 VFX 이펙트만 생성 (데미지 없음)
        CreateSplatterEffect();
        
        // 0.1초 후 제거 (즉시 데미지 처리 후)
        StartCoroutine(DestroyAfterDelay(0.1f));
    }
    
    private void CreateSplatterEffect()
    {
        if (splatterPrefab != null)
        {
            // ⭐ 핵심 수정: Instantiate 대신 GamePoolManager 사용
            GameObject splatter = null;
            
            if (GamePoolManager.Instance != null)
            {
                splatter = GamePoolManager.Instance.SpawnFromPool("Grape Projectile Splatter", transform.position, Quaternion.identity);
                
                if (splatter != null)
                {
                    Debug.Log("[GrapeProjectile] GamePoolManager에서 Grape Projectile Splatter 생성 성공");
                }
                else
                {
                    Debug.LogError("[GrapeProjectile] GamePoolManager에서 Grape Projectile Splatter 생성 실패!");
                }
            }
            else
            {
                // 백업: GamePoolManager가 없으면 직접 생성
                splatter = Instantiate(splatterPrefab, transform.position, Quaternion.identity);
                Debug.LogWarning("[GrapeProjectile] GamePoolManager가 없어서 직접 생성했습니다.");
            }
            
            // 스플래터 이펙트 설정 (GamePoolManager 사용 시에도 동일)
            if (splatter != null)
            {
                // ⭐ 변경: 스플래터는 순수 VFX만 담당
                // SpriteFade가 있으면 자동으로 페이드 시작
                if (splatter.TryGetComponent(out SpriteFade spriteFade))
                {
                    spriteFade.StartFade();
                }
                
                // GrapeLandSplatter 컴포넌트가 있어도 데미지 설정 안함
                if (splatter.TryGetComponent(out GrapeLandSplatter splatterComponent))
                {
                    splatterComponent.StartFadeEffect(); // VFX만
                }
            }
        }
    }
    
    private void DealDamageToPlayer()
    {
        // 범위 내 플레이어 감지
        Collider2D[] playersInRange = Physics2D.OverlapCircleAll(transform.position, 1.5f, playerLayerMask);
        
        foreach (Collider2D playerCollider in playersInRange)
        {
            if (playerCollider.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(projectileDamage, transform);
                Debug.Log($"[GrapeProjectile] 플레이어에게 {projectileDamage} 데미지 적용!");
                break; // 한 명만 데미지 적용
            }
        }
    }
    
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // 풀링 시스템이 있으면 비활성화, 없으면 파괴
        if (transform.parent != null && transform.parent.name.Contains("Pool"))
        {
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // 충돌 감지 (중간에 장애물 등과 충돌 시)
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어와 중간에 충돌하면 즉시 착지
        if ((playerLayerMask.value & (1 << other.gameObject.layer)) > 0)
        {
            if (!isLaunched) return;
            
            targetPosition = transform.position;
            StopAllCoroutines();
            OnProjectileLand();
        }
    }
    
    // 디버그용 Gizmo
    private void OnDrawGizmosSelected()
    {
        if (isLaunched)
        {
            // 발사 경로 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPosition, targetPosition);
            
            // 목표 지점 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 1f);
        }
    }
} 