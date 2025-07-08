using System.Collections;
using UnityEngine;

public class GrapeProjectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float duration = 2f;
    [SerializeField] private AnimationCurve animCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float heightY = 3f;
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Effects")]
    [SerializeField] private GameObject shadowPrefab;
    [SerializeField] private GameObject splatterPrefab;
    [SerializeField] private AudioClip impactSound;
    
    [Header("Damage")]
    [SerializeField] private LayerMask playerLayerMask = 1 << 3; // Player layer
    
    private int damage = 1;
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
        
        // 그림자 정리
        if (activeShadow != null)
        {
            if (Application.isPlaying)
            {
                Destroy(activeShadow);
            }
            activeShadow = null;
        }
        
        isLaunched = false;
    }
    
    public void SetDamage(int newDamage)
    {
        damage = newDamage;
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
            activeShadow = Instantiate(shadowPrefab, shadowStartPos, Quaternion.identity);
            
            if (activeShadow != null)
            {
                StartCoroutine(MoveShadowCoroutine());
            }
        }
    }
    
    private IEnumerator ProjectileMotionCoroutine()
    {
        float timePassed = 0f;
        
        while (timePassed < duration && isLaunched)
        {
            if (!gameObject.activeInHierarchy) yield break;
            
            timePassed += Time.deltaTime;
            float linearT = timePassed / duration;
            
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
        
        while (timePassed < duration && activeShadow != null)
        {
            timePassed += Time.deltaTime;
            float linearT = timePassed / duration;
            
            activeShadow.transform.position = Vector3.Lerp(shadowStart, shadowEnd, linearT);
            
            yield return null;
        }
        
        if (activeShadow != null)
        {
            Destroy(activeShadow);
            activeShadow = null;
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
            GameObject splatter = Instantiate(splatterPrefab, transform.position, Quaternion.identity);
            
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
    
    private void DealDamageToPlayer()
    {
        // 착지 지점 주변의 플레이어 감지
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, 1f, playerLayerMask);
        
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(damage, transform);
                Debug.Log($"[GrapeProjectile] 플레이어에게 {damage} 데미지를 입혔습니다.");
                break;
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