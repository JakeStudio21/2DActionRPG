using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 재료 데이터베이스 (중앙 관리자)
/// - MaterialType → MaterialData 변환
/// - 모든 재료 데이터 관리
/// </summary>
[CreateAssetMenu(fileName = "MaterialDatabase", menuName = "Data/Material Database", order = 399)]
public class MaterialDatabase : ScriptableObject
{
    private static MaterialDatabase _instance;
    
    /// <summary>
    /// 싱글톤 인스턴스 (Resources/Data/MaterialDatabase.asset)
    /// </summary>
    public static MaterialDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<MaterialDatabase>("Data/MaterialDatabase");
                if (_instance == null)
                {
                    Debug.LogError("[MaterialDatabase] MaterialDatabase.asset을 찾을 수 없습니다. (경로: Resources/Data/MaterialDatabase)");
                }
            }
            return _instance;
        }
    }
    
    [Header("📦 재료 데이터베이스")]
    [Tooltip("모든 재료 데이터 목록 (9개)")]
    public List<MaterialData> materials = new List<MaterialData>();
    
    private Dictionary<MaterialType, MaterialData> _cache;
    
    /// <summary>
    /// 캐시 초기화 (Dictionary 생성)
    /// </summary>
    private void BuildCache()
    {
        if (_cache != null) return;
        
        _cache = new Dictionary<MaterialType, MaterialData>();
        
        foreach (var mat in materials)
        {
            if (mat == null)
            {
                Debug.LogWarning("[MaterialDatabase] null MaterialData가 포함되어 있습니다.");
                continue;
            }
            
            if (_cache.ContainsKey(mat.materialType))
            {
                Debug.LogWarning($"[MaterialDatabase] 중복된 MaterialType: {mat.materialType} ({mat.name})");
                continue;
            }
            
            _cache[mat.materialType] = mat;
        }
        
        Debug.Log($"[MaterialDatabase] 캐시 빌드 완료: {_cache.Count}개 재료");
    }
    
    /// <summary>
    /// MaterialType → MaterialData 변환
    /// </summary>
    /// <param name="type">재료 타입 enum</param>
    /// <returns>MaterialData 또는 null</returns>
    public MaterialData GetData(MaterialType type)
    {
        BuildCache();
        
        if (_cache.TryGetValue(type, out var data))
        {
            return data;
        }
        
        Debug.LogWarning($"[MaterialDatabase] MaterialType을 찾을 수 없음: {type}");
        return null;
    }
    
    /// <summary>
    /// 📦 materialId → MaterialData 변환 (DropTable 연동용)
    /// </summary>
    /// <param name="materialId">재료 고유 ID (예: "MAT_WEAPON_FRAGMENT")</param>
    /// <returns>MaterialData 또는 null</returns>
    public MaterialData GetDataById(string materialId)
    {
        if (string.IsNullOrEmpty(materialId))
        {
            Debug.LogWarning("[MaterialDatabase] materialId가 null 또는 빈 문자열입니다.");
            return null;
        }
        
        // ⭐ materialId로 직접 검색 (JSON 외부 연동 안전)
        foreach (var mat in materials)
        {
            if (mat != null && mat.materialId == materialId)
            {
                return mat;
            }
        }
        
        Debug.LogWarning($"[MaterialDatabase] materialId를 찾을 수 없음: {materialId}");
        return null;
    }
    
    /// <summary>
    /// 모든 재료 가져오기 (정렬 순서대로)
    /// </summary>
    /// <returns>정렬된 MaterialData 리스트</returns>
    public List<MaterialData> GetAllMaterials()
    {
        var sorted = new List<MaterialData>(materials);
        sorted.RemoveAll(m => m == null);
        sorted.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
        return sorted;
    }
    
    /// <summary>
    /// 드롭 가능한 재료만 가져오기
    /// </summary>
    public List<MaterialData> GetDroppableMaterials()
    {
        var result = new List<MaterialData>();
        foreach (var mat in materials)
        {
            if (mat != null && mat.canDrop)
            {
                result.Add(mat);
            }
        }
        result.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
        return result;
    }
    
    /// <summary>
    /// 등급별 재료 가져오기
    /// </summary>
    public List<MaterialData> GetMaterialsByRarity(MaterialRarity rarity)
    {
        var result = new List<MaterialData>();
        foreach (var mat in materials)
        {
            if (mat != null && mat.rarity == rarity)
            {
                result.Add(mat);
            }
        }
        result.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
        return result;
    }
    
    /// <summary>
    /// 데이터베이스 유효성 검사
    /// </summary>
    [ContextMenu("Validate Database")]
    public void ValidateDatabase()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🔍 [MaterialDatabase] 유효성 검사 시작");
        Debug.Log("═══════════════════════════════════════════════════════");
        
        int validCount = 0;
        int nullCount = 0;
        int duplicateCount = 0;
        int missingIconCount = 0;
        int missingIdCount = 0;
        
        var typeSet = new HashSet<MaterialType>();
        var idSet = new HashSet<string>();
        
        foreach (var mat in materials)
        {
            if (mat == null)
            {
                nullCount++;
                Debug.LogWarning("❌ null MaterialData 발견");
                continue;
            }
            
            // ⭐ materialId 중복 체크 (JSON 연동 안전성)
            if (string.IsNullOrEmpty(mat.materialId))
            {
                missingIdCount++;
                Debug.LogWarning($"❌ materialId 없음: {mat.displayName} ({mat.name})");
            }
            else if (idSet.Contains(mat.materialId))
            {
                duplicateCount++;
                Debug.LogWarning($"❌ 중복된 materialId: {mat.materialId} ({mat.name})");
                continue;
            }
            else
            {
                idSet.Add(mat.materialId);
            }
            
            // MaterialType 중복 체크
            if (typeSet.Contains(mat.materialType))
            {
                duplicateCount++;
                Debug.LogWarning($"❌ 중복된 MaterialType: {mat.materialType} ({mat.name})");
                continue;
            }
            typeSet.Add(mat.materialType);
            
            // 아이콘 체크
            if (mat.icon == null)
            {
                missingIconCount++;
                Debug.LogWarning($"⚠️ 아이콘 없음: {mat.displayName} ({mat.name})");
            }
            
            validCount++;
            Debug.Log($"✅ {mat.materialType}: {mat.displayName} [{mat.rarity}] (ID: {mat.materialId}, 정렬: {mat.sortOrder})");
        }
        
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"📊 검사 결과:");
        Debug.Log($"  - 유효: {validCount}개");
        Debug.Log($"  - Null: {nullCount}개");
        Debug.Log($"  - 중복: {duplicateCount}개");
        Debug.Log($"  - materialId 없음: {missingIdCount}개");
        Debug.Log($"  - 아이콘 없음: {missingIconCount}개");
        
        if (validCount == 9 && nullCount == 0 && duplicateCount == 0)
        {
            Debug.Log("✅ 데이터베이스 완벽!");
        }
        else
        {
            Debug.LogWarning("⚠️ 데이터베이스에 문제가 있습니다.");
        }
        Debug.Log("═══════════════════════════════════════════════════════");
    }
}

