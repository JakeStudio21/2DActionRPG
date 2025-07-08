using System.Collections;
using UnityEngine;

public class GrapeLandSplatter : MonoBehaviour
{
    [Header("VFX Settings")]
    [SerializeField] private bool autoStartFade = true;
    [SerializeField] private AudioClip splatterSound;
    [SerializeField] private float autoDestroyTime = 3f;
    
    private SpriteFade spriteFade;
    private AudioSource audioSource;
    
    private void Awake()
    {
        spriteFade = GetComponent<SpriteFade>();
        audioSource = GetComponent<AudioSource>();
    }
    
    private void OnEnable()
    {
        // 사운드 재생
        if (audioSource != null && splatterSound != null)
        {
            audioSource.PlayOneShot(splatterSound);
        }
        
        // 자동 페이드 시작
        if (autoStartFade)
        {
            StartFadeEffect();
        }
        
        // 안전 장치: 일정 시간 후 강제 제거
        StartCoroutine(SafetyDestroy());
    }
    
    private void OnDisable()
    {
        StopAllCoroutines();
    }
    
    /// <summary>
    /// 페이드 효과 시작 (외부에서 호출 가능)
    /// </summary>
    public void StartFadeEffect()
    {
        if (spriteFade != null)
        {
            spriteFade.StartFade();
            Debug.Log($"[GrapeLandSplatter] VFX 페이드 시작: {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[GrapeLandSplatter] SpriteFade 없음, 기본 제거: {gameObject.name}");
            StartCoroutine(DestroyAfterDelay(2f));
        }
    }
    
    /// <summary>
    /// 안전 장치: 일정 시간 후 강제 제거
    /// </summary>
    private IEnumerator SafetyDestroy()
    {
        yield return new WaitForSeconds(autoDestroyTime);
        DestroyObject();
    }
    
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        DestroyObject();
    }
    
    private void DestroyObject()
    {
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
} 