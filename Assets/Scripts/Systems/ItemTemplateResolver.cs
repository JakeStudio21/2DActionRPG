using UnityEngine;

/// <summary>
/// EquipmentData 템플릿 로딩 정적 클래스
/// - 기본: Resources.Load 방식
/// - 확장: SetResolver()로 커스텀 구현 주입 가능
/// 
/// 사용 예시:
/// - 기본: ItemTemplateResolver.Load("Sword_S_Equipment")
/// - 커스텀: ItemTemplateResolver.SetResolver(new AddressablesResolver())
/// </summary>
public static class ItemTemplateResolver
{
    private static IEquipmentResolver _customResolver = null;
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 공개 API
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    /// <summary>
    /// 커스텀 리졸버 설정 (Dependency Injection)
    /// </summary>
    public static void SetResolver(IEquipmentResolver resolver)
    {
        _customResolver = resolver;
        Debug.Log($"✨ [ItemTemplateResolver] 커스텀 리졸버 설정: {resolver?.GetType().Name ?? "null"}");
    }
    
    /// <summary>
    /// 템플릿 로드 (동기)
    /// </summary>
    public static EquipmentData Load(string templateName)
    {
        if (string.IsNullOrEmpty(templateName))
        {
            Debug.LogWarning("[ItemTemplateResolver] 템플릿 이름이 비어있음");
            return null;
        }
        
        // 커스텀 리졸버 우선
        if (_customResolver != null)
        {
            return _customResolver.Resolve(templateName);
        }
        
        // 기본: Resources 로드
        return LoadFromResources(templateName);
    }
    
    /// <summary>
    /// 템플릿 로드 (비동기, 지원 시)
    /// </summary>
    public static void LoadAsync(string templateName, System.Action<EquipmentData> onComplete)
    {
        if (_customResolver != null && _customResolver.SupportsAsync)
        {
            _customResolver.ResolveAsync(templateName, onComplete);
        }
        else
        {
            // 기본: 동기 로드 후 콜백
            var result = Load(templateName);
            onComplete?.Invoke(result);
        }
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 기본 구현 (Resources)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static EquipmentData LoadFromResources(string templateName)
    {
        // 1차 시도: Equipment 폴더 (현재 프로젝트 구조)
        string path = $"Equipment/{templateName}";
        var data = Resources.Load<EquipmentData>(path);
        
        if (data != null)
        {
            return data;
        }
        
        // 2차 시도: EquipmentData 폴더 (대체 경로)
        path = $"EquipmentData/{templateName}";
        data = Resources.Load<EquipmentData>(path);
        
        if (data != null)
        {
            return data;
        }
        
        // 3차 시도: 루트 경로
        data = Resources.Load<EquipmentData>(templateName);
        
        if (data != null)
        {
            return data;
        }
        
        // ⚠️ 장비 아이템이 아닐 수 있으므로 에러가 아니라 null 반환
        return null;
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 기본 제공 구현체 예시
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

/// <summary>
/// Resources 기반 리졸버 (명시적 구현)
/// </summary>
public class ResourcesEquipmentResolver : IEquipmentResolver
{
    private readonly string _basePath;
    
    public ResourcesEquipmentResolver(string basePath = "EquipmentData")
    {
        _basePath = basePath;
    }
    
    public bool SupportsAsync => false;
    
    public EquipmentData Resolve(string templateName)
    {
        string path = string.IsNullOrEmpty(_basePath) 
            ? templateName 
            : $"{_basePath}/{templateName}";
        
        var data = Resources.Load<EquipmentData>(path);
        
        if (data == null)
        {
            Debug.LogError($"❌ [ResourcesResolver] 템플릿 없음: {path}");
        }
        
        return data;
    }
    
    public void ResolveAsync(string templateName, System.Action<EquipmentData> onComplete)
    {
        // Resources는 비동기 미지원
        var result = Resolve(templateName);
        onComplete?.Invoke(result);
    }
}

/// <summary>
/// 캐시 기반 리졸버 (성능 최적화)
/// </summary>
public class CachedEquipmentResolver : IEquipmentResolver
{
    private readonly IEquipmentResolver _innerResolver;
    private readonly System.Collections.Generic.Dictionary<string, EquipmentData> _cache 
        = new System.Collections.Generic.Dictionary<string, EquipmentData>();
    
    public CachedEquipmentResolver(IEquipmentResolver innerResolver)
    {
        _innerResolver = innerResolver;
    }
    
    public bool SupportsAsync => _innerResolver.SupportsAsync;
    
    public EquipmentData Resolve(string templateName)
    {
        if (_cache.TryGetValue(templateName, out var cached))
        {
            return cached;
        }
        
        var data = _innerResolver.Resolve(templateName);
        
        if (data != null)
        {
            _cache[templateName] = data;
        }
        
        return data;
    }
    
    public void ResolveAsync(string templateName, System.Action<EquipmentData> onComplete)
    {
        if (_cache.TryGetValue(templateName, out var cached))
        {
            onComplete?.Invoke(cached);
            return;
        }
        
        _innerResolver.ResolveAsync(templateName, (data) =>
        {
            if (data != null)
            {
                _cache[templateName] = data;
            }
            onComplete?.Invoke(data);
        });
    }
    
    public void ClearCache()
    {
        _cache.Clear();
        Debug.Log("🗑️ [CachedResolver] 캐시 클리어");
    }
}

