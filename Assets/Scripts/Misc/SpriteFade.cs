using UnityEngine;
using System.Collections;

public class SpriteFade : MonoBehaviour
{
    public float fadeTime = 1f;
    private float elapsedTime = 0f;
    private SpriteRenderer spriteRenderer;
    private bool isInitialized = false;

    void Awake()
    {
        InitializeSafely();
    }

    void OnEnable()
    {
        // OnEnable에서도 다시 한번 안전하게 초기화
        InitializeSafely();
    }

    /// <summary>
    /// 안전한 초기화 (null 체크 포함)
    /// </summary>
    private void InitializeSafely()
    {
        if (!isInitialized || spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            isInitialized = spriteRenderer != null;
            
            if (!isInitialized)
            {
                Debug.LogError($"SpriteFade: SpriteRenderer component not found on {gameObject.name}");
            }
        }
    }

    public void FadeOut()
    {
        InitializeSafely();
        if (!isInitialized || spriteRenderer == null)
        {
            Debug.LogError($"SpriteFade: Cannot fade out - SpriteRenderer is null on {gameObject.name}");
            return;
        }
        StartCoroutine(FadeOutCoroutine());
    }

    private IEnumerator FadeOutCoroutine()
    {
        InitializeSafely();
        if (!isInitialized || spriteRenderer == null) 
        {
            Debug.LogError($"SpriteFade: FadeOutCoroutine stopped - SpriteRenderer is null on {gameObject.name}");
            yield break;
        }
        
        elapsedTime = 0f;
        float startValue = spriteRenderer.color.a;

        while (elapsedTime < fadeTime && spriteRenderer != null)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startValue, 0f, elapsedTime / fadeTime);
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, newAlpha);
            yield return null;
        }

        if (gameObject != null && gameObject.activeInHierarchy)
            gameObject.SetActive(false);
    }

    public IEnumerator SlowFadeRoutine()
    {
        InitializeSafely();
        if (!isInitialized || spriteRenderer == null) 
        {
            Debug.LogError($"SpriteFade: SlowFadeRoutine stopped - SpriteRenderer is null on {gameObject.name}");
            yield break;
        }
        
        elapsedTime = 0f;
        float startValue = spriteRenderer.color.a;

        while (elapsedTime < fadeTime && spriteRenderer != null && gameObject != null)
        {
            elapsedTime += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startValue, 0f, elapsedTime / fadeTime);
            
            // 매 프레임마다 null 체크
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, newAlpha);
            }
            else
            {
                yield break; // spriteRenderer가 파괴되면 코루틴 종료
            }
            
            yield return null;
        }

        if (gameObject != null && gameObject.activeInHierarchy)
            gameObject.SetActive(false);
    }
} 