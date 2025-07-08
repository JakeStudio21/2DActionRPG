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
            if (transform.parent != null && transform.parent.name.Contains("Pool"))
            {
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
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