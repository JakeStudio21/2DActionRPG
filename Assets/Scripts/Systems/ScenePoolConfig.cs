using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ScenePoolConfig", menuName = "Pool/Scene Pool Config")]
public class ScenePoolConfig : ScriptableObject
{
    [System.Serializable]
    public class PoolSettings
    {
        [Header("Pool Configuration")]
        public string tag;
        public GameObject prefab;
        public int size = 10;
        
        [Header("Auto Management")]
        public bool preloadOnSceneStart = true;
        public bool clearOnSceneExit = false;
        
        [Header("Performance")]
        [Range(5, 100)]
        public int maxInstancesPerFrame = 20;
    }
    
    [Header("Scene Pool Configuration")]
    public string sceneName;
    
    [Header("Required Pools")]
    public List<PoolSettings> requiredPools = new List<PoolSettings>();
    
    [Header("Optional Pools (Load on Demand)")]
    public List<PoolSettings> optionalPools = new List<PoolSettings>();
    
    [Header("Debug")]
    public bool enableDebugLogs = false;
    
    /// <summary>
    /// 모든 풀 설정 가져오기 (필수 + 선택적)
    /// </summary>
    public List<PoolSettings> GetAllPools()
    {
        List<PoolSettings> allPools = new List<PoolSettings>();
        allPools.AddRange(requiredPools);
        allPools.AddRange(optionalPools);
        return allPools;
    }
    
    /// <summary>
    /// 특정 태그의 풀 설정 찾기
    /// </summary>
    public PoolSettings GetPoolByTag(string tag)
    {
        foreach (var pool in requiredPools)
        {
            if (pool.tag == tag) return pool;
        }
        
        foreach (var pool in optionalPools)
        {
            if (pool.tag == tag) return pool;
        }
        
        return null;
    }
    
    /// <summary>
    /// 설정 유효성 검사
    /// </summary>
    public bool ValidateConfig()
    {
        HashSet<string> usedTags = new HashSet<string>();
        
        foreach (var pool in GetAllPools())
        {
            if (string.IsNullOrEmpty(pool.tag))
            {
                Debug.LogError($"[ScenePoolConfig] Empty tag found in {sceneName}");
                return false;
            }
            
            if (pool.prefab == null)
            {
                Debug.LogError($"[ScenePoolConfig] Missing prefab for tag '{pool.tag}' in {sceneName}");
                return false;
            }
            
            if (usedTags.Contains(pool.tag))
            {
                Debug.LogError($"[ScenePoolConfig] Duplicate tag '{pool.tag}' in {sceneName}");
                return false;
            }
            
            usedTags.Add(pool.tag);
        }
        
        return true;
    }
}