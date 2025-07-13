using System.Collections;
using UnityEngine;

public class GrapeLandSplatter : MonoBehaviour
{
    [Header("VFX Settings")]
    [SerializeField] private bool autoStartFade = true;
    [SerializeField] private AudioClip splatterSound;
    
    private SpriteFade spriteFade;
    private AudioSource audioSource;
    
    // 🔑 중복 반환 방지 플래그
    private bool isReturningToPool = false;
    
    private void Awake()
    {
        spriteFade = GetComponent<SpriteFade>();
        audioSource = GetComponent<AudioSource>();
    }
    
    private void OnEnable()
    {
        // 🔑 플래그 초기화
        isReturningToPool = false;
        
        // 사운드 재생
        if (audioSource != null && splatterSound != null)
        {
            audioSource.PlayOneShot(splatterSound);
        }
        
        // 자동 페이드 시작 + 완료 감지
        if (autoStartFade)
        {
            StartFadeEffect();
        }
    }
    
    /// <summary>
    /// 페이드 효과 시작 (외부에서 호출 가능)
    /// </summary>
    public void StartFadeEffect()
    {
        if (spriteFade != null)
        {
            // 🔑 SpriteFade의 destroyOnComplete를 false로 설정하고 직접 처리
            var destroyField = typeof(SpriteFade).GetField("destroyOnComplete", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (destroyField != null)
            {
                destroyField.SetValue(spriteFade, false); // SpriteFade가 직접 반환하지 않도록
            }
            
            spriteFade.StartFade();
            Debug.Log($"[GrapeLandSplatter] VFX 페이드 시작: {gameObject.name}");
            
            // 🔑 페이드 완료 감지 코루틴 시작
            StartCoroutine(WaitForFadeComplete());
        }
        else
        {
            Debug.LogWarning($"[GrapeLandSplatter] SpriteFade 컴포넌트가 없습니다: {gameObject.name}");
            // SpriteFade가 없으면 즉시 반환
            StartCoroutine(ReturnAfterDelay(0.5f));
        }
    }
    
    /// <summary>
    /// 🔑 페이드 완료 감지 및 풀 반환
    /// </summary>
    private IEnumerator WaitForFadeComplete()
    {
        if (spriteFade == null) yield break;
        
        // SpriteFade의 fadeTime 가져오기
        float fadeTime = 2f; // 기본값
        var fadeTimeField = typeof(SpriteFade).GetField("fadeTime", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fadeTimeField != null)
        {
            fadeTime = (float)fadeTimeField.GetValue(spriteFade);
        }
        
        // 페이드 시간만큼 대기 + 약간의 여유
        yield return new WaitForSeconds(fadeTime + 0.1f);
        
        // 풀에 반환
        ReturnToPool();
    }
    
    /// <summary>
    /// 🔑 간단한 딜레이 후 반환
    /// </summary>
    private IEnumerator ReturnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool();
    }
    
    /// <summary>
    /// 🔑 풀 반환 통합 메서드
    /// </summary>
    private void ReturnToPool()
    {
        if (isReturningToPool) return; // 중복 반환 방지
        
        isReturningToPool = true;
        
        if (GamePoolManager.Instance != null)
        {
            GamePoolManager.Instance.ReturnToPool("Grape Projectile Splatter", gameObject);
            Debug.Log($"[GrapeLandSplatter] 풀에 정상 반환: {gameObject.name}");
        }
        else
        {
            // 백업: 기존 방식
            if (transform.parent != null && transform.parent.name.Contains("Pool"))
            {
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
} 