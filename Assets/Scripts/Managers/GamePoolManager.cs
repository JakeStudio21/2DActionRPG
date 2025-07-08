using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 오브젝트 풀링을 관리하는 매니저
/// 투사체, 몬스터, 이펙트 등의 재사용을 통해 성능 최적화
/// </summary>
public class GamePoolManager : Singleton<GamePoolManager>
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    [Header("풀 설정")]
    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    protected override void Awake()
    {
        base.Awake(); // Singleton 로직 실행
        if (instance != this) return; // 중복 생성시 초기화 중단
        
        InitializePools();
    }

    /// <summary>
    /// 오브젝트 풀 초기화 (안전한 방식)
    /// </summary>
    private void InitializePools()
    {
        Debug.Log("[GamePoolManager] 풀 초기화 시작");
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        
        foreach (Pool pool in pools)
        {
            // null 체크 및 유효성 검사
            if (pool == null)
            {
                Debug.LogError("[GamePoolManager] null 풀 발견! 건너뜁니다.");
                continue;
            }
            
            if (string.IsNullOrEmpty(pool.tag))
            {
                Debug.LogError("[GamePoolManager] 빈 태그를 가진 풀 발견! 건너뜁니다.");
                continue;
            }
            
            if (pool.prefab == null)
            {
                Debug.LogError($"[GamePoolManager] '{pool.tag}' 풀의 프리팹이 null입니다! 건너뜁니다.");
                continue;
            }
            
            // 이미 존재하는 태그 체크
            if (poolDictionary.ContainsKey(pool.tag))
            {
                Debug.LogWarning($"[GamePoolManager] 중복된 태그 '{pool.tag}' 발견! 건너뜁니다.");
                continue;
            }
            
            Queue<GameObject> objectPool = new Queue<GameObject>();
            
            try
            {
                for (int i = 0; i < pool.size; i++)
                {
                    // ⭐ 핵심 수정: 비활성화 상태로 생성
                    GameObject obj = Instantiate(pool.prefab);
                    
                    if (obj == null)
                    {
                        Debug.LogError($"[GamePoolManager] '{pool.tag}' 프리팹 인스턴스 생성 실패!");
                        continue;
                    }
                    
                    // ⭐ 즉시 비활성화하여 OnEnable 문제 방지
                    if (obj.activeInHierarchy)
                    {
                        obj.SetActive(false);
                    }
                    
                    obj.transform.SetParent(transform); // 풀 매니저 하위로 정리
                    objectPool.Enqueue(obj);
                }
                
                poolDictionary.Add(pool.tag, objectPool);
                Debug.Log($"[GamePoolManager] 풀 생성 성공: {pool.tag} ({objectPool.Count}개)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GamePoolManager] '{pool.tag}' 풀 생성 중 오류: {e.Message}");
            }
        }
        
        Debug.Log($"[GamePoolManager] 풀 초기화 완료. 총 {poolDictionary.Count}개 풀 생성됨.");
    }

    /// <summary>
    /// 풀에서 오브젝트 가져오기
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"[GamePoolManager] 풀에 없는 태그: {tag}");
            return null;
        }

        GameObject objectToSpawn = poolDictionary[tag].Dequeue();
        
        // ⭐ 핵심 추가: null 체크
        if (objectToSpawn == null)
        {
            Debug.LogWarning($"[GamePoolManager] 풀에서 파괴된 오브젝트 발견: {tag}");
            
            // 새 오브젝트 생성해서 풀에 추가
            Pool targetPool = pools.Find(p => p.tag == tag);
            if (targetPool != null)
            {
                objectToSpawn = Instantiate(targetPool.prefab);
            }
        }
        
        if (objectToSpawn != null)
        {
            objectToSpawn.SetActive(true);
            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;
        }
        
        poolDictionary[tag].Enqueue(objectToSpawn);
        return objectToSpawn;
    }

    /// <summary>
    /// 오브젝트를 풀로 반환
    /// </summary>
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"[GamePoolManager] 풀에 없는 태그: {tag}");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(transform);
    }

    /// <summary>
    /// 특정 태그의 풀 크기 확장
    /// </summary>
    public void ExpandPool(string tag, int additionalSize)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"[GamePoolManager] 풀에 없는 태그: {tag}");
            return;
        }

        Pool targetPool = pools.Find(p => p.tag == tag);
        if (targetPool == null) return;

        Queue<GameObject> pool = poolDictionary[tag];
        
        for (int i = 0; i < additionalSize; i++)
        {
            GameObject obj = Instantiate(targetPool.prefab);
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            pool.Enqueue(obj);
        }

        Debug.Log($"[GamePoolManager] 풀 확장: {tag} (+{additionalSize}개)");
    }
}
