using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 벽 충돌 시스템: 벽에 Wall Layer 자동 할당
/// </summary>
public static class WallLayerSetup
{
    /// <summary>
    /// 씬의 모든 벽 Tilemap/GameObject에 Wall Layer 할당
    /// </summary>
    public static void AssignWallLayerToAllWalls()
    {
        int wallLayerIndex = LayerMask.NameToLayer("Wall");
        
        if (wallLayerIndex == -1)
        {
            Debug.LogError("🚨 [WallLayerSetup] 'Wall' Layer가 존재하지 않습니다! Edit → Project Settings → Tags and Layers에서 Layer 10을 'Wall'로 설정하세요.");
            return;
        }
        
        int wallCount = 0;
        
        // 1️⃣ Tilemap 기반 벽 찾기
        Tilemap[] tilemaps = Object.FindObjectsOfType<Tilemap>();
        foreach (Tilemap tilemap in tilemaps)
        {
            string name = tilemap.name.ToLower();
            
            // "wall", "decoration", "tree", "obstacle" 등 벽으로 간주
            if (name.Contains("wall") || 
                name.Contains("decoration") || 
                name.Contains("tree") || 
                name.Contains("obstacle") ||
                name.Contains("barrier"))
            {
                // ⭐ 부모와 모든 자식에게 Layer 할당
                SetLayerRecursively(tilemap.gameObject, wallLayerIndex);
                wallCount++;
                Debug.Log($"✅ [WallLayerSetup] {tilemap.name} (및 자식들) → Wall Layer 할당");
            }
        }
        
        // 2️⃣ Barricade (파괴 가능한 장애물)
        Barricade[] barricades = Object.FindObjectsOfType<Barricade>();
        foreach (Barricade barricade in barricades)
        {
            // ⭐ 부모와 모든 자식에게 Layer 할당
            SetLayerRecursively(barricade.gameObject, wallLayerIndex);
            wallCount++;
            Debug.Log($"✅ [WallLayerSetup] {barricade.name} (Barricade 및 자식들) → Wall Layer 할당");
        }
        
        // 3️⃣ "Wall" 태그가 있는 GameObject
        GameObject[] wallObjects = GameObject.FindGameObjectsWithTag("Wall");
        foreach (GameObject wall in wallObjects)
        {
            // ⭐ 부모와 모든 자식에게 Layer 할당
            SetLayerRecursively(wall, wallLayerIndex);
            wallCount++;
            Debug.Log($"✅ [WallLayerSetup] {wall.name} (Tag: Wall 및 자식들) → Wall Layer 할당");
        }
        
        if (wallCount == 0)
        {
            Debug.LogWarning("⚠️ [WallLayerSetup] 벽을 찾을 수 없습니다. Tilemap 이름에 'wall', 'decoration' 등을 포함하거나 'Wall' 태그를 사용하세요.");
        }
        else
        {
            Debug.Log($"🎉 [WallLayerSetup] {wallCount}개 벽(및 자식들)에 Wall Layer 할당 완료!");
        }
    }
    
    /// <summary>
    /// GameObject와 모든 자식에게 재귀적으로 Layer 할당
    /// </summary>
    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        
        obj.layer = layer;
        
        // 모든 자식에게도 적용
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// Unity Editor 메뉴: Tools → Wall System → Assign Wall Layer
    /// </summary>
    [MenuItem("Tools/Wall System/Assign Wall Layer")]
    private static void MenuAssignWallLayer()
    {
        AssignWallLayerToAllWalls();
        EditorUtility.DisplayDialog(
            "Wall Layer Setup", 
            "벽에 Wall Layer 할당이 완료되었습니다!\n\nConsole에서 결과를 확인하세요.", 
            "OK"
        );
    }
    
    /// <summary>
    /// Unity Editor 메뉴: Tools → Wall System → Verify Physics Matrix
    /// </summary>
    [MenuItem("Tools/Wall System/Verify Physics Matrix")]
    private static void MenuVerifyPhysicsMatrix()
    {
        int wallLayer = LayerMask.NameToLayer("Wall");
        int projectileLayer = LayerMask.NameToLayer("Projectile");
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        
        string report = "=== Physics 2D Collision Matrix 검증 ===\n\n";
        
        // Layer 존재 여부 확인
        report += "📋 Layer 존재 확인:\n";
        report += $"  Wall (Layer 10): {(wallLayer != -1 ? "✅ 존재" : "❌ 없음")}\n";
        report += $"  Projectile (Layer 15): {(projectileLayer != -1 ? "✅ 존재" : "❌ 없음")}\n";
        report += $"  Player (Layer 3): {(playerLayer != -1 ? "✅ 존재" : "❌ 없음")}\n";
        report += $"  Enemy (Layer 6): {(enemyLayer != -1 ? "✅ 존재" : "❌ 없음")}\n\n";
        
        // Collision Matrix 확인
        if (wallLayer != -1 && projectileLayer != -1)
        {
            report += "🔧 Collision Matrix 설정 확인:\n";
            bool wallProjectile = !Physics2D.GetIgnoreLayerCollision(wallLayer, projectileLayer);
            bool wallPlayer = !Physics2D.GetIgnoreLayerCollision(wallLayer, playerLayer);
            bool wallEnemy = !Physics2D.GetIgnoreLayerCollision(wallLayer, enemyLayer);
            bool projProj = !Physics2D.GetIgnoreLayerCollision(projectileLayer, projectileLayer);
            
            report += $"  Wall × Projectile: {(wallProjectile ? "✅ 충돌 (올바름)" : "❌ 무시 (잘못됨!)")}\n";
            report += $"  Wall × Player: {(wallPlayer ? "✅ 충돌" : "⚠️ 무시")}\n";
            report += $"  Wall × Enemy: {(wallEnemy ? "✅ 충돌" : "⚠️ 무시")}\n";
            report += $"  Projectile × Projectile: {(projProj ? "⚠️ 충돌" : "✅ 무시 (올바름)")}\n\n";
            
            // 잘못된 설정이 있으면 경고
            if (!wallProjectile)
            {
                report += "🚨 경고: Wall과 Projectile이 충돌하지 않습니다!\n";
                report += "   → Edit → Project Settings → Physics 2D → Layer Collision Matrix에서\n";
                report += "   → Wall × Projectile을 체크하세요.\n\n";
            }
            
            if (projProj)
            {
                report += "⚠️ 주의: Projectile끼리 충돌합니다.\n";
                report += "   → 투사체끼리 충돌을 원하지 않으면 해제하세요.\n\n";
            }
        }
        else
        {
            report += "❌ Layer가 생성되지 않아 Collision Matrix를 확인할 수 없습니다.\n";
        }
        
        Debug.Log(report);
        EditorUtility.DisplayDialog("Physics Matrix 검증", report, "OK");
    }
    
    /// <summary>
    /// Unity Editor 메뉴: Tools → Wall System → Check Wall Colliders
    /// </summary>
    [MenuItem("Tools/Wall System/Check Wall Colliders")]
    private static void MenuCheckWallColliders()
    {
        int wallLayerIndex = LayerMask.NameToLayer("Wall");
        
        string report = "=== 벽 Collider 상태 검증 ===\n\n";
        
        if (wallLayerIndex == -1)
        {
            report += "❌ 'Wall' Layer가 존재하지 않습니다!\n";
            Debug.LogError(report);
            EditorUtility.DisplayDialog("벽 Collider 검증", report, "OK");
            return;
        }
        
        // 모든 GameObject 검색
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        int wallObjectCount = 0;
        int colliderMissingCount = 0;
        int wrongLayerCount = 0;
        
        report += "📋 Wall Layer를 가진 오브젝트:\n\n";
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == wallLayerIndex)
            {
                wallObjectCount++;
                Collider2D collider = obj.GetComponent<Collider2D>();
                
                if (collider == null)
                {
                    colliderMissingCount++;
                    report += $"❌ {obj.name}: Collider2D 없음!\n";
                }
                else
                {
                    report += $"✅ {obj.name}: {collider.GetType().Name}";
                    if (collider.isTrigger)
                        report += " (Trigger)";
                    report += $"\n";
                }
            }
        }
        
        report += $"\n📊 통계:\n";
        report += $"  - Wall Layer 오브젝트: {wallObjectCount}개\n";
        report += $"  - Collider 없음: {colliderMissingCount}개\n";
        
        if (colliderMissingCount > 0)
        {
            report += $"\n⚠️ Collider가 없는 벽이 {colliderMissingCount}개 있습니다!\n";
            report += "투사체가 이 벽을 통과할 수 있습니다.\n";
        }
        
        if (wallObjectCount == 0)
        {
            report += "\n❌ Wall Layer를 가진 오브젝트가 없습니다!\n";
            report += "Tools → Wall System → Assign Wall Layer를 먼저 실행하세요.\n";
        }
        
        Debug.Log(report);
        EditorUtility.DisplayDialog("벽 Collider 검증", report, "OK");
    }
    #endif
}

