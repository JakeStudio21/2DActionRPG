using UnityEngine;
using UnityEngine.Tilemaps;


/// <summary>
/// 타일맵 레이어 설정 헬퍼 (런타임 지원)
/// </summary>
public static class TilemapLayerSetup
{
    // 소팅 레이어 이름 상수
    public const string BACKGROUND_LAYER = "Background";
    public const string GROUND_LAYER = "Ground";
    public const string CHARACTER_LAYER = "Character";
    public const string DECORATION_LAYER = "Decoration";
    public const string EFFECTS_LAYER = "Effects";
    public const string UI_LAYER = "UI";
    
    // 소팅 오더 기본값
    public const int BACKGROUND_ORDER = -100;
    public const int GROUND_ORDER = 0;
    public const int CHARACTER_ORDER = 100;
    public const int DECORATION_ORDER = 200;
    public const int EFFECTS_ORDER = 300;
    public const int UI_ORDER = 400;
    
    /// <summary>
    /// 타일맵을 Ground Layer로 설정 (충돌 포함)
    /// </summary>
    /// <param name="tilemap">설정할 타일맵</param>
    public static void SetupAsGroundLayer(Tilemap tilemap)
    {
        if (tilemap == null) return;
        
        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = GROUND_LAYER;
            renderer.sortingOrder = GROUND_ORDER;
        }
        
        // 충돌체 설정
        TilemapCollider2D collider = tilemap.GetComponent<TilemapCollider2D>();
        if (collider == null)
        {
            collider = tilemap.gameObject.AddComponent<TilemapCollider2D>();
        }
        
        tilemap.gameObject.layer = LayerMask.NameToLayer("Ground");
        
        Debug.Log($"🌍 [TilemapLayerSetup] {tilemap.name} → Ground Layer 설정");
    }
    
    /// <summary>
    /// 타일맵을 Decoration Layer로 설정 (오클루더)
    /// </summary>
    /// <param name="tilemap">설정할 타일맵</param>
    public static void SetupAsDecorationLayer(Tilemap tilemap)
    {
        if (tilemap == null) return;
        
        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = DECORATION_LAYER;
            renderer.sortingOrder = DECORATION_ORDER;
        }
        
        // 오클루더 태그
        tilemap.gameObject.tag = "Occluder";
        tilemap.gameObject.layer = LayerMask.NameToLayer("Decoration");
        
        Debug.Log($"🎨 [TilemapLayerSetup] {tilemap.name} → Decoration Layer 설정");
    }
    
    /// <summary>
    /// 타일맵을 Background Layer로 설정
    /// </summary>
    /// <param name="tilemap">설정할 타일맵</param>
    public static void SetupAsBackgroundLayer(Tilemap tilemap)
    {
        if (tilemap == null) return;
        
        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = BACKGROUND_LAYER;
            renderer.sortingOrder = BACKGROUND_ORDER;
        }
        
        tilemap.gameObject.layer = LayerMask.NameToLayer("Background");
        
        Debug.Log($"🖼️ [TilemapLayerSetup] {tilemap.name} → Background Layer 설정");
    }
    
    /// <summary>
    /// 씬의 모든 타일맵 자동 분류 및 설정
    /// </summary>
    public static void AutoSetupAllTilemaps()
    {
        Tilemap[] tilemaps = Object.FindObjectsOfType<Tilemap>();
        int setupCount = 0;
        
        foreach (Tilemap tilemap in tilemaps)
        {
            string name = tilemap.name.ToLower();
            
            if (name.Contains("ground") || name.Contains("floor"))
            {
                SetupAsGroundLayer(tilemap);
                setupCount++;
            }
            else if (name.Contains("wall") || name.Contains("decoration") || name.Contains("tree"))
            {
                SetupAsDecorationLayer(tilemap);
                setupCount++;
            }
            else if (name.Contains("background") || name.Contains("pattern"))
            {
                SetupAsBackgroundLayer(tilemap);
                setupCount++;
            }
        }
        
        Debug.Log($"🗂️ [TilemapLayerSetup] {setupCount}개 타일맵 자동 설정 완료");
    }
}
