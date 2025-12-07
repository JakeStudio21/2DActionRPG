using System.Collections;
using UnityEngine;

/// <summary>
/// 스킬 AOE 생성 헬퍼 클래스
/// Phase 3: 원형, 직사각형, 부채꼴 등 다양한 AOE 형태 지원
/// </summary>
public static class SkillAOESpawner
{
    /// <summary>
    /// AOE 생성
    /// </summary>
    public static GameObject SpawnAOE(
        SkillAOEShape shape,
        Vector3 position,
        Vector2 direction,
        Vector2 size,
        float fanAngle,
        float damage,
        float duration,
        LayerMask targetLayer,
        string hitCueEventKey,  // ⭐ 추가: Hit Cue 이벤트 키
        MonoBehaviour caller = null)
    {
        // 1. AOE GameObject 생성
        GameObject aoe = new GameObject($"SkillAOE_{shape}");
        aoe.transform.position = position;
        
        // 2. 방향에 맞게 회전 (미러링 없음)
        if (direction.magnitude > 0.1f && shape != SkillAOEShape.Circle)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            aoe.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            Debug.Log($"🔄 [SkillAOESpawner] AOE 방향 설정: {shape}, 각도: {angle:F1}°");
        }
        
        // 3. 콜라이더 설정
        SetupCollider(aoe, shape, size, fanAngle, direction);
        
        // 4. PlayerSkillAOEDamage 컴포넌트 추가 (플레이어 → 적 데미지)
        SetupDamage(aoe, damage, targetLayer, hitCueEventKey);
        
        // 5. 비주얼 추가 (선택)
        AddVisualEffect(aoe, shape, size);
        
        // 6. 자동 제거
        if (caller != null)
        {
            caller.StartCoroutine(AutoDestroy(aoe, duration));
        }
        else
        {
            Object.Destroy(aoe, duration);
        }
        
        Debug.Log($"💥 [SkillAOESpawner] AOE 생성: {shape}, 크기: {size}, 데미지: {damage:F0}, Hit Cue: {hitCueEventKey}");
        
        return aoe;
    }
    
    /// <summary>
    /// 콜라이더 설정 (기본 크기 1×1, Transform.localScale로 크기 조절)
    /// </summary>
    private static void SetupCollider(GameObject aoe, SkillAOEShape shape, Vector2 size, float fanAngle, Vector2 direction)
    {
        Collider2D collider = null;
        
        switch (shape)
        {
            case SkillAOEShape.Circle:
                var circleCollider = aoe.AddComponent<CircleCollider2D>();
                circleCollider.radius = 0.5f; // ⭐ 기본 크기: 반지름 0.5 (지름 1)
                circleCollider.isTrigger = true;
                collider = circleCollider;
                Debug.Log($"🔵 [SkillAOESpawner] CircleCollider 생성: radius=0.5 (Scale로 조절: {size.x})");
                break;
                
            case SkillAOEShape.Rectangle:
            case SkillAOEShape.Line:
                var boxCollider = aoe.AddComponent<BoxCollider2D>();
                boxCollider.size = Vector2.one; // ⭐ 기본 크기: 1×1
                boxCollider.isTrigger = true;
                
                // 직사각형은 앞쪽으로 오프셋 (플레이어 중심이 아닌 전방)
                boxCollider.offset = new Vector2(0.5f, 0); // ⭐ 기본 오프셋 (0.5 = 1×1 박스의 절반)
                collider = boxCollider;
                Debug.Log($"🔲 [SkillAOESpawner] BoxCollider 생성: size=1×1 (Scale로 조절: {size})");
                break;
                
            case SkillAOEShape.Fan:
                // 부채꼴은 PolygonCollider2D로 구현
                var polygonCollider = aoe.AddComponent<PolygonCollider2D>();
                SetupFanCollider(polygonCollider, 1f, fanAngle, direction); // ⭐ 기본 반지름: 1
                polygonCollider.isTrigger = true;
                collider = polygonCollider;
                Debug.Log($"🌀 [SkillAOESpawner] PolygonCollider 생성: radius=1 (Scale로 조절: {size.x}), angle={fanAngle}");
                break;
        }
    }
    
    /// <summary>
    /// 부채꼴 콜라이더 생성
    /// </summary>
    private static void SetupFanCollider(PolygonCollider2D polygonCollider, float radius, float angle, Vector2 centerDirection)
    {
        // 부채꼴 점 생성
        int segments = 20; // 부채꼀 해상도
        Vector2[] points = new Vector2[segments + 2];
        
        // 중심점
        points[0] = Vector2.zero;
        
        // 부채꼴 호 생성
        float halfAngle = angle / 2f;
        float startAngle = -halfAngle;
        
        for (int i = 0; i <= segments; i++)
        {
            float currentAngle = startAngle + (angle * i / segments);
            float radians = currentAngle * Mathf.Deg2Rad;
            
            points[i + 1] = new Vector2(
                Mathf.Cos(radians) * radius,
                Mathf.Sin(radians) * radius
            );
        }
        
        polygonCollider.points = points;
    }
    
    /// <summary>
    /// PlayerSkillAOEDamage 컴포넌트 설정 (플레이어 → 적 전용)
    /// </summary>
    private static void SetupDamage(GameObject aoe, float damage, LayerMask targetLayer, string hitCueEventKey)
    {
        var skillAOE = aoe.AddComponent<PlayerSkillAOEDamage>();
        skillAOE.SetDamage((int)damage);
        skillAOE.SetEnemyLayerMask(targetLayer);
        skillAOE.SetHitCueEventKey(hitCueEventKey);
        skillAOE.SetEmitHitCue(true);
        
        Debug.Log($"💥 [SkillAOESpawner] PlayerSkillAOEDamage 설정: 데미지={damage}, Hit Cue={hitCueEventKey}");
    }
    
    /// <summary>
    /// 비주얼 이펙트 추가 (간단한 디버그 표시)
    /// ⭐ Transform.localScale로 콜라이더와 비주얼 동시 조절
    /// </summary>
    private static void AddVisualEffect(GameObject aoe, SkillAOEShape shape, Vector2 size)
    {
        // 간단한 SpriteRenderer 추가 (반투명 표시)
        var spriteRenderer = aoe.AddComponent<SpriteRenderer>();
        
        // 기본 스프라이트 생성 (사각형)
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, new Color(1, 0.5f, 0, 0.7f)); // ⭐ 주황색 반투명 (70% 불투명)
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        spriteRenderer.sprite = sprite;
        
        // ⭐ Transform.localScale로 콜라이더와 비주얼 동시 크기 조절
        switch (shape)
        {
            case SkillAOEShape.Circle:
                // CircleCollider: radius=0.5 (지름 1) → size.x로 스케일
                aoe.transform.localScale = new Vector3(size.x * 2f, size.x * 2f, 1f);
                Debug.Log($"🔵 [SkillAOESpawner] Circle Scale 설정: {size.x * 2f} (반지름 {size.x})");
                break;
                
            case SkillAOEShape.Rectangle:
            case SkillAOEShape.Line:
                // BoxCollider: size=1×1 → size.x, size.y로 스케일
                aoe.transform.localScale = new Vector3(size.x, size.y, 1f);
                Debug.Log($"🔲 [SkillAOESpawner] Rectangle Scale 설정: ({size.x}, {size.y})");
                break;
                
            case SkillAOEShape.Fan:
                // PolygonCollider: radius=1 → size.x로 스케일
                aoe.transform.localScale = new Vector3(size.x, size.x, 1f);
                Debug.Log($"🌀 [SkillAOESpawner] Fan Scale 설정: {size.x}");
                break;
        }
        
        // ⭐ 소팅 레이어 설정
        spriteRenderer.sortingLayerName = "Default"; // Sorting Layer 명시
        spriteRenderer.sortingOrder = 100; // 최상위 표시
    }
    
    /// <summary>
    /// 자동 제거 코루틴
    /// </summary>
    private static IEnumerator AutoDestroy(GameObject aoe, float duration)
    {
        yield return new WaitForSeconds(duration);
        
        if (aoe != null)
        {
            Object.Destroy(aoe);
            Debug.Log($"🗑️ [SkillAOESpawner] AOE 자동 제거");
        }
    }
}

