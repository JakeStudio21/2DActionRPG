using System.Collections;
using UnityEngine;

public class SpriteFade : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeTime = 2f;
    [SerializeField] private bool fadeOnStart = false;
    [SerializeField] private bool destroyOnComplete = true;
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isFading = false;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }
    
    private void Start()
    {
        if (fadeOnStart)
        {
            StartCoroutine(SlowFadeRoutine());
        }
    }
    
    public IEnumerator SlowFadeRoutine()
    {
        if (isFading || spriteRenderer == null) yield break;
        
        isFading = true;
        float elapsedTime = 0f;
        Color startColor = spriteRenderer.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsedTime / fadeTime);
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        
        spriteRenderer.color = targetColor;
        
        if (destroyOnComplete)
        {
            // 🔑 태그 추정 개선
            string poolTag = gameObject.name.Replace("(Clone)", "").Trim();

            // 🔑 "_숫자" 패턴 제거 (예: "Grape Projectile Splatter_0" → "Grape Projectile Splatter")
            int underscoreIndex = poolTag.LastIndexOf('_');
            if (underscoreIndex > 0)
            {
                string afterUnderscore = poolTag.Substring(underscoreIndex + 1);
                if (int.TryParse(afterUnderscore, out _)) // 숫자면 제거
                {
                    poolTag = poolTag.Substring(0, underscoreIndex);
                }
            }

            GamePoolManager.Instance.ReturnToPool(poolTag, gameObject);
        }
        
        isFading = false;
    }
    
    public void StartFade()
    {
        StartCoroutine(SlowFadeRoutine());
    }
    
    public void ResetFade()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            isFading = false;
        }
    }
} 