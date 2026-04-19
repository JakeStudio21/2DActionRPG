using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 풀/장식 오브젝트를 영역 내 랜덤 배치하는 자동 스포너
/// BoxCollider2D, PolygonCollider2D, CompositeCollider2D 모두 지원
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GrassAutoSpawner : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("🌿 Grass Prefabs (가중치 기반)")]
    [Tooltip("배치할 프리팹과 출현 확률 (weight가 높을수록 자주 출현)")]
    public List<WeightedGrassPrefab> grassPrefabs = new List<WeightedGrassPrefab>();
    
    [Header("📊 Spawn Settings")]
    [Tooltip("생성할 풀의 총 개수")]
    [SerializeField] private int spawnCount = 100;
    
    [Tooltip("랜덤 포인트 생성 최대 재시도 횟수 (Collider 내부 찾기)")]
    [SerializeField] private int maxAttempts = 50;
    
    [Header("📍 Randomization")]
    [Tooltip("위치 랜덤 오프셋 (X, Y)")]
    [SerializeField] private Vector2 positionOffset = new Vector2(0.2f, 0.2f);
    
    [Tooltip("회전 랜덤 범위 (Z축, 도 단위)")]
    [SerializeField] private Vector2 rotationRange = new Vector2(-15f, 15f);
    
    [Tooltip("스케일 랜덤 범위 (Min, Max)")]
    [SerializeField] private Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    
    [Header("🚫 Min Distance")]
    [Tooltip("풀 간 최소 거리 (0이면 체크 안함)")]
    [SerializeField] private float minDistance = 0.5f;
    
    [Header("✅ Y-Sorting")]
    [Tooltip("Y-Sorting 레이어 이름")]
    [SerializeField] private string sortingLayerName = "Ground";
    
    [Tooltip("Sorting Order 오프셋")]
    [SerializeField] private int sortingOrderOffset = 0;
    
    [Header("🔗 Container")]
    [Tooltip("생성된 풀의 부모 Transform (비워두면 자동 생성)")]
    [SerializeField] private Transform spawnRoot;
    
    #endregion
    
    #region Private Fields
    
    private Collider2D areaCollider;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        areaCollider = GetComponent<Collider2D>();
        
        if (areaCollider != null)
        {
            // Collider 타입 로그
            string colliderType = areaCollider.GetType().Name;
        }
        else
        {
            Debug.LogError($"[GrassAutoSpawner] Collider2D를 찾을 수 없습니다! ({gameObject.name})");
        }
    }
    
    private void OnDrawGizmos()
    {
        // 영역 시각화
        if (areaCollider == null)
            areaCollider = GetComponent<Collider2D>();
        
        if (areaCollider != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            
            // Bounds 표시
            Bounds bounds = areaCollider.bounds;
            Gizmos.DrawCube(bounds.center, bounds.size);
            
            // 테두리
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// 풀 생성 실행 (Editor에서 호출)
    /// </summary>
    public void GenerateGrass()
    {
        if (!ValidateSetup())
            return;
        
        PrepareSpawnRoot();
        
        
        List<Vector2> spawnPoints = GenerateSpawnPoints();
        
        
        int successCount = 0;
        
        foreach (var point in spawnPoints)
        {
            GameObject prefab = GetRandomWeightedPrefab();
            
            if (prefab == null)
            {
                Debug.LogWarning("[GrassAutoSpawner] 프리팹을 가져올 수 없습니다.");
                continue;
            }
            
            GameObject grass = Instantiate(prefab, point, Quaternion.identity);
            grass.transform.parent = spawnRoot;
            
            // 랜덤 적용
            ApplyRandomization(grass.transform, point);
            
            // Y-Sorting 설정
            SetupYSorting(grass);
            
            successCount++;
        }
        
    }
    
    /// <summary>
    /// 생성된 풀 전체 삭제 (Editor에서 호출)
    /// </summary>
    public void ClearAll()
    {
        if (spawnRoot == null)
        {
            Debug.LogWarning("[GrassAutoSpawner] spawnRoot가 없습니다. 삭제할 것이 없습니다.");
            return;
        }
        
        int count = spawnRoot.childCount;
        
        for (int i = count - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            DestroyImmediate(spawnRoot.GetChild(i).gameObject);
#else
            Destroy(spawnRoot.GetChild(i).gameObject);
#endif
        }
        
    }
    
    /// <summary>
    /// 프리뷰용 스폰 포인트 생성 (실제 Instantiate 없음)
    /// </summary>
    public List<Vector2> GeneratePreviewPoints()
    {
        if (!ValidateSetup())
            return new List<Vector2>();
        
        
        List<Vector2> points = GenerateSpawnPoints();
        
        
        return points;
    }
    
    /// <summary>
    /// 현재 생성된 풀 개수 반환
    /// </summary>
    public int GetSpawnedCount()
    {
        if (spawnRoot == null)
            return 0;
        
        return spawnRoot.childCount;
    }
    
    #endregion
    
    #region Private Methods - Core Logic
    
    /// <summary>
    /// 스폰 포인트 리스트 생성 (거리 체크 포함)
    /// </summary>
    private List<Vector2> GenerateSpawnPoints()
    {
        List<Vector2> points = new List<Vector2>();
        int failCount = 0;
        
        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 point = GetRandomPointInArea(maxAttempts);
            
            if (point == Vector2.zero)
            {
                failCount++;
                continue;
            }
            
            // 최소 거리 체크
            if (minDistance > 0 && IsTooClose(point, points, minDistance))
            {
                failCount++;
                continue;
            }
            
            points.Add(point);
        }
        
        if (failCount > 0)
        {
            Debug.LogWarning($"[GrassAutoSpawner] {failCount}개의 포인트 생성 실패 (재시도 한계 또는 거리 제약)");
        }
        
        return points;
    }
    
    /// <summary>
    /// Collider 내부의 랜덤 포인트 생성 (재시도 로직)
    /// </summary>
    private Vector2 GetRandomPointInArea(int maxAttempts)
    {
        Bounds bounds = areaCollider.bounds;
        
        for (int i = 0; i < maxAttempts; i++)
        {
            float x = Random.Range(bounds.min.x, bounds.max.x);
            float y = Random.Range(bounds.min.y, bounds.max.y);
            Vector2 point = new Vector2(x, y);
            
            // OverlapPoint로 Collider 내부 확인
            if (areaCollider.OverlapPoint(point))
            {
                return point;
            }
        }
        
        // 재시도 실패
        return Vector2.zero;
    }
    
    /// <summary>
    /// 최소 거리 체크 (v1: 단순 리스트 비교)
    /// </summary>
    private bool IsTooClose(Vector2 point, List<Vector2> existing, float minDist)
    {
        foreach (var pos in existing)
        {
            if (Vector2.Distance(point, pos) < minDist)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 가중치 기반 랜덤 프리팹 선택
    /// </summary>
    private GameObject GetRandomWeightedPrefab()
    {
        if (grassPrefabs == null || grassPrefabs.Count == 0)
        {
            Debug.LogError("[GrassAutoSpawner] grassPrefabs가 비어있습니다!");
            return null;
        }
        
        // 총 가중치 계산
        int totalWeight = 0;
        foreach (var item in grassPrefabs)
        {
            if (item.prefab != null)
                totalWeight += item.weight;
        }
        
        if (totalWeight == 0)
        {
            Debug.LogError("[GrassAutoSpawner] 총 가중치가 0입니다!");
            return null;
        }
        
        // 가중치 기반 선택
        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;
        
        foreach (var item in grassPrefabs)
        {
            if (item.prefab == null)
                continue;
            
            currentWeight += item.weight;
            
            if (randomValue < currentWeight)
            {
                return item.prefab;
            }
        }
        
        // Fallback (첫 번째 유효한 프리팹)
        foreach (var item in grassPrefabs)
        {
            if (item.prefab != null)
                return item.prefab;
        }
        
        return null;
    }
    
    #endregion
    
    #region Private Methods - Setup & Validation
    
    /// <summary>
    /// 설정 유효성 검사
    /// </summary>
    private bool ValidateSetup()
    {
        if (areaCollider == null)
        {
            Debug.LogError("[GrassAutoSpawner] Collider2D가 없습니다! BoxCollider2D, PolygonCollider2D 등을 추가하세요.");
            return false;
        }
        
        if (grassPrefabs == null || grassPrefabs.Count == 0)
        {
            Debug.LogError("[GrassAutoSpawner] grassPrefabs가 비어있습니다! 프리팹을 할당하세요.");
            return false;
        }
        
        bool hasValidPrefab = false;
        foreach (var item in grassPrefabs)
        {
            if (item.prefab != null)
            {
                hasValidPrefab = true;
                break;
            }
        }
        
        if (!hasValidPrefab)
        {
            Debug.LogError("[GrassAutoSpawner] 유효한 프리팹이 없습니다!");
            return false;
        }
        
        if (spawnCount <= 0)
        {
            Debug.LogError("[GrassAutoSpawner] spawnCount는 1 이상이어야 합니다!");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// spawnRoot 준비 (없으면 자동 생성)
    /// </summary>
    private void PrepareSpawnRoot()
    {
        if (spawnRoot == null)
        {
            GameObject container = new GameObject("GrassContainer");
            container.transform.parent = transform;
            container.transform.localPosition = Vector3.zero;
            spawnRoot = container.transform;
            
        }
    }
    
    #endregion
    
    #region Private Methods - Randomization & Y-Sorting
    
    /// <summary>
    /// 랜덤 변화 적용 (위치, 회전, 스케일)
    /// </summary>
    private void ApplyRandomization(Transform target, Vector2 basePosition)
    {
        // 위치 오프셋
        if (positionOffset != Vector2.zero)
        {
            float offsetX = Random.Range(-positionOffset.x, positionOffset.x);
            float offsetY = Random.Range(-positionOffset.y, positionOffset.y);
            target.position = new Vector3(basePosition.x + offsetX, basePosition.y + offsetY, target.position.z);
        }
        
        // 회전 (Z축)
        if (rotationRange != Vector2.zero)
        {
            float randomRotation = Random.Range(rotationRange.x, rotationRange.y);
            target.rotation = Quaternion.Euler(0f, 0f, randomRotation);
        }
        
        // 스케일
        if (scaleRange.x != scaleRange.y)
        {
            float randomScale = Random.Range(scaleRange.x, scaleRange.y);
            target.localScale = Vector3.one * randomScale;
        }
    }
    
    /// <summary>
    /// Y-Sorting 설정 (SpriteRenderer)
    /// </summary>
    private void SetupYSorting(GameObject target)
    {
        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
        
        foreach (var renderer in renderers)
        {
            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = sortingOrderOffset;
        }
    }
    
    #endregion
}

/// <summary>
/// 가중치 기반 프리팹 데이터
/// </summary>
[System.Serializable]
public class WeightedGrassPrefab
{
    [Tooltip("배치할 프리팹")]
    public GameObject prefab;
    
    [Tooltip("출현 가중치 (높을수록 자주 출현)")]
    [Range(1, 1000)]
    public int weight = 100;
}

