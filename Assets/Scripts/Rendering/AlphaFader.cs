using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 🎨 알파 페이드 컴포넌트 (Occluder용)
/// SpriteRenderer, TilemapRenderer 지원하는 부드러운 알파 페이드
/// </summary>
public class AlphaFader : MonoBehaviour
{
    [Header("🎨 페이드 설정")]
    [SerializeField] private float fadeDuration = 0.3f; // 페이드 지속시간
    [SerializeField] private float targetAlpha = 0.3f; // 페이드아웃 시 목표 알파값 (0~1)
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 페이드 곡선
    
    [Header("🔧 렌더러 설정")]
    [SerializeField] private bool autoDetectRenderers = true; // 렌더러 자동 감지
    [SerializeField] private SpriteRenderer[] spriteRenderers; // 수동 설정용
    [SerializeField] private TilemapRenderer[] tilemapRenderers; // 수동 설정용
    
    
    // 페이드 상태
    private bool isFading = false;
    private bool isFadedOut = false;
    private float originalAlpha = 1f;
    private Coroutine currentFadeCoroutine;
    
    // 렌더러 캐시
    private SpriteRenderer[] cachedSpriteRenderers;
    private TilemapRenderer[] cachedTilemapRenderers;
    private bool renderersInitialized = false;
    
    void Start()
    {
        InitializeRenderers();
        CacheOriginalAlpha();
        
    }
    
    /// <summary>
    /// 렌더러 초기화 및 캐시
    /// </summary>
    private void InitializeRenderers()
    {
        if (renderersInitialized) return;
        
        if (autoDetectRenderers)
        {
            // 자동 감지: 자식 포함 모든 렌더러
            cachedSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
            cachedTilemapRenderers = GetComponentsInChildren<TilemapRenderer>();
        }
        else
        {
            // 수동 설정값 사용
            cachedSpriteRenderers = spriteRenderers ?? new SpriteRenderer[0];
            cachedTilemapRenderers = tilemapRenderers ?? new TilemapRenderer[0];
        }
        
        renderersInitialized = true;
    }
    
    /// <summary>
    /// 원본 알파값 캐시
    /// </summary>
    private void CacheOriginalAlpha()
    {
        if (cachedSpriteRenderers.Length > 0)
        {
            originalAlpha = cachedSpriteRenderers[0].color.a;
        }
        else if (cachedTilemapRenderers.Length > 0)
        {
            // TilemapRenderer는 Material을 통해 알파 조절
            Material material = cachedTilemapRenderers[0].material;
            if (material != null && material.HasProperty("_Color"))
            {
                originalAlpha = material.color.a;
            }
            else
            {
                originalAlpha = 1f; // 기본값
            }
        }
    }
    
    /// <summary>
    /// 🌅 페이드아웃 시작 (투명하게)
    /// </summary>
    public void StartFadeOut()
    {
        if (isFadedOut && !isFading) return; // 이미 페이드아웃 상태
        
        StopCurrentFade();
        currentFadeCoroutine = StartCoroutine(FadeToAlpha(targetAlpha, true));
        
    }
    
    /// <summary>
    /// 🌄 페이드인 시작 (원래대로)
    /// </summary>
    public void StartFadeIn()
    {
        if (!isFadedOut && !isFading) return; // 이미 원래 상태
        
        StopCurrentFade();
        currentFadeCoroutine = StartCoroutine(FadeToAlpha(originalAlpha, false));
        
    }
    
    /// <summary>
    /// 현재 페이드 중단
    /// </summary>
    private void StopCurrentFade()
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
            currentFadeCoroutine = null;
        }
    }
    
    /// <summary>
    /// 알파값으로 페이드하는 코루틴
    /// </summary>
    private IEnumerator FadeToAlpha(float targetAlphaValue, bool fadingOut)
    {
        isFading = true;
        float startAlpha = GetCurrentAlpha();
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeDuration;
            float curveValue = fadeCurve.Evaluate(progress);
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlphaValue, curveValue);
            
            SetAlpha(currentAlpha);
            
            yield return null;
        }
        
        // 최종값 설정
        SetAlpha(targetAlphaValue);
        
        isFading = false;
        isFadedOut = fadingOut;
        currentFadeCoroutine = null;
        
    }
    
    /// <summary>
    /// 현재 알파값 가져오기
    /// </summary>
    private float GetCurrentAlpha()
    {
        if (cachedSpriteRenderers.Length > 0)
        {
            return cachedSpriteRenderers[0].color.a;
        }
        else if (cachedTilemapRenderers.Length > 0)
        {
            // TilemapRenderer는 Material을 통해 알파 확인
            Material material = cachedTilemapRenderers[0].material;
            if (material != null && material.HasProperty("_Color"))
            {
                return material.color.a;
            }
            return 1f;
        }
        
        return 1f;
    }
    
    /// <summary>
    /// 모든 렌더러에 알파값 설정
    /// </summary>
    private void SetAlpha(float alpha)
    {
        // SpriteRenderer들 처리
        foreach (SpriteRenderer renderer in cachedSpriteRenderers)
        {
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = alpha;
                renderer.color = color;
            }
        }
        
        // TilemapRenderer들 처리 (Material을 통해)
        foreach (TilemapRenderer renderer in cachedTilemapRenderers)
        {
            if (renderer != null)
            {
                Material material = renderer.material;
                if (material != null && material.HasProperty("_Color"))
                {
                    Color color = material.color;
                    color.a = alpha;
                    material.color = color;
                }
            }
        }
    }
    
    /// <summary>
    /// 소팅 오더 가져오기 (OcclusionDetector용)
    /// </summary>
    public int GetSortingOrder()
    {
        if (cachedSpriteRenderers.Length > 0 && cachedSpriteRenderers[0] != null)
        {
            return cachedSpriteRenderers[0].sortingOrder;
        }
        else if (cachedTilemapRenderers.Length > 0 && cachedTilemapRenderers[0] != null)
        {
            return cachedTilemapRenderers[0].sortingOrder;
        }
        
        return 0;
    }
    
    /// <summary>
    /// 즉시 원래 상태로 복구
    /// </summary>
    public void ResetToOriginal()
    {
        StopCurrentFade();
        SetAlpha(originalAlpha);
        isFadedOut = false;
        
    }
    
    /// <summary>
    /// 페이드 설정 변경
    /// </summary>
    public void SetFadeSettings(float duration, float alpha)
    {
        fadeDuration = duration;
        targetAlpha = Mathf.Clamp01(alpha);
    }
    
    void OnDisable()
    {
        // 비활성화 시 페이드 중단 및 원래 상태로
        ResetToOriginal();
    }
    
    #if UNITY_EDITOR
    void OnValidate()
    {
        // Inspector 값 검증
        fadeDuration = Mathf.Max(0.1f, fadeDuration);
        targetAlpha = Mathf.Clamp01(targetAlpha);
        
        if (Application.isPlaying && renderersInitialized)
        {
            InitializeRenderers();
        }
    }
    #endif
}