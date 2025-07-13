using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬별 동적 오브젝트 풀링 매니저 - 완전 리팩토링 버전
/// 올바른 풀링 로직 + 씬별 동적 로딩 시스템
/// </summary>
public class GamePoolManager : Singleton<GamePoolManager>
{
    [Header("현재 씬 풀 설정")]
    public ScenePoolConfig currentSceneConfig;
    
    [Header("풀 상태 모니터링")]
    public bool enableDebugMode = false;
    public bool showPoolStats = false;
    
    [Header("성능 설정")]
    [Range(1, 10)]
    public int maxPoolsLoadPerFrame = 3;
    
    // 핵심 데이터 구조
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, GameObject> activePools; // 현재 사용 중인 오브젝트 추적
    private Dictionary<string, ScenePoolConfig.PoolSettings> poolSettings;
    private HashSet<string> loadedPoolTags;
    
    // 씬 관리
    private string currentSceneName;
    private bool isLoadingPools = false;
    
    protected override void Awake()
    {
        base.Awake();
        if (instance != this) return;
        
        InitializeCore();
        RegisterSceneEvents();
    }
    
    private void Start()
    {
        StartCoroutine(LoadCurrentScenePools());
    }
    
    #region Core Initialization
    
    private void InitializeCore()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        activePools = new Dictionary<string, GameObject>();
        poolSettings = new Dictionary<string, ScenePoolConfig.PoolSettings>();
        loadedPoolTags = new HashSet<string>();
        
        currentSceneName = SceneManager.GetActiveScene().name;
        
        if (enableDebugMode)
        {
            Debug.Log($"[GamePoolManager] 초기화 완료 - 현재 씬: {currentSceneName}");
        }
    }
    
    private void RegisterSceneEvents()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }
    
    protected override void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        
        base.OnDestroy(); // 부모 클래스의 OnDestroy 호출
    }
    
    #endregion
    
    #region Scene Management
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string newSceneName = scene.name;
        if (newSceneName == currentSceneName) return;
        
        if (enableDebugMode)
        {
            Debug.Log($"[GamePoolManager] 씬 변경 감지: {currentSceneName} → {newSceneName}");
        }
        
        currentSceneName = newSceneName;
        StartCoroutine(LoadScenePoolsCoroutine(newSceneName));
    }
    
    private void OnSceneUnloaded(Scene scene)
    {
        StartCoroutine(UnloadUnusedPools());
    }
    
    private IEnumerator LoadCurrentScenePools()
    {
        yield return LoadScenePoolsCoroutine(currentSceneName);
    }
    
    private IEnumerator LoadScenePoolsCoroutine(string sceneName)
    {
        if (isLoadingPools) yield break;
        
        isLoadingPools = true;
        
        // 기존 풀 정리
        yield return UnloadUnusedPools();
        
        // 씬 설정 찾기
        ScenePoolConfig config = FindSceneConfig(sceneName);
        if (config == null)
        {
            Debug.LogWarning($"[GamePoolManager] 씬 '{sceneName}'의 풀 설정을 찾을 수 없습니다.");
            isLoadingPools = false;
            yield break;
        }
        
        currentSceneConfig = config;
        
        if (!config.ValidateConfig())
        {
            Debug.LogError($"[GamePoolManager] 씬 '{sceneName}'의 풀 설정이 유효하지 않습니다.");
            isLoadingPools = false;
            yield break;
        }
        
        // 필수 풀 로드
        yield return LoadRequiredPools(config);
        
        isLoadingPools = false;
        
        if (enableDebugMode)
        {
            Debug.Log($"[GamePoolManager] 씬 '{sceneName}' 풀 로딩 완료");
            if (showPoolStats) PrintPoolStats();
        }
    }
    
    /// <summary>
    /// 개별 풀 생성 메서드 (누락된 메서드 추가)
    /// </summary>
    private IEnumerator CreatePool(ScenePoolConfig.PoolSettings poolSetting)
    {
        if (poolDictionary.ContainsKey(poolSetting.tag))
        {
            Debug.LogWarning($"[GamePoolManager] 풀 '{poolSetting.tag}'가 이미 존재합니다.");
            yield break;
        }
        
        if (poolSetting.prefab == null)
        {
            Debug.LogError($"[GamePoolManager] 풀 '{poolSetting.tag}'의 프리팹이 null입니다!");
            yield break;
        }
        
        Queue<GameObject> objectPool = new Queue<GameObject>();
        poolSettings[poolSetting.tag] = poolSetting;
        
        int createdThisFrame = 0;
        
        for (int i = 0; i < poolSetting.size; i++)
        {
            GameObject obj = Instantiate(poolSetting.prefab);
            obj.name = $"{poolSetting.prefab.name}_{i}";
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            
            objectPool.Enqueue(obj);
            
            createdThisFrame++;
            if (createdThisFrame >= poolSetting.maxInstancesPerFrame)
            {
                createdThisFrame = 0;
                yield return null; // 다음 프레임까지 대기
            }
        }
        
        poolDictionary[poolSetting.tag] = objectPool;
        loadedPoolTags.Add(poolSetting.tag);
        
        if (enableDebugMode)
        {
            Debug.Log($"[GamePoolManager] 풀 생성 완료: {poolSetting.tag} ({objectPool.Count}개)");
        }
    }
    
    #endregion
    
    #region Pool Operations (핵심 수정된 로직)
    
    /// <summary>
    /// 풀에서 오브젝트 가져오기 - 올바른 로직
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        // 풀 존재 확인
        if (!poolDictionary.ContainsKey(tag))
        {
            if (enableDebugMode)
            {
                Debug.LogWarning($"[GamePoolManager] 풀에 없는 태그: {tag}");
            }
            return null;
        }
        
        Queue<GameObject> pool = poolDictionary[tag];
        
        // 풀이 비어있으면 확장
        if (pool.Count == 0)
        {
            if (enableDebugMode)
            {
                Debug.LogWarning($"[GamePoolManager] 풀 '{tag}'가 비어있음, 확장 중...");
            }
            
            ExpandPool(tag, 5); // 긴급 확장
            
            if (pool.Count == 0)
            {
                Debug.LogError($"[GamePoolManager] 풀 '{tag}' 확장 실패!");
                return null;
            }
        }
        
        // ⭐ 핵심 수정: Dequeue만 하고 다시 Enqueue하지 않음
        GameObject objectToSpawn = pool.Dequeue();
        
        // null 체크 및 재생성
        if (objectToSpawn == null)
        {
            if (poolSettings.ContainsKey(tag))
            {
                objectToSpawn = Instantiate(poolSettings[tag].prefab);
                objectToSpawn.name = $"{poolSettings[tag].prefab.name}_Emergency";
            }
            else
            {
                Debug.LogError($"[GamePoolManager] 풀 '{tag}'의 설정을 찾을 수 없습니다!");
                return null;
            }
        }
        
        // 오브젝트 활성화 및 위치 설정
        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        objectToSpawn.transform.SetParent(null); // 씬 루트로 이동
        
        // 활성 풀에 등록
        if (!activePools.ContainsKey(objectToSpawn.GetInstanceID().ToString()))
        {
            activePools[objectToSpawn.GetInstanceID().ToString()] = objectToSpawn;
        }
        
        if (enableDebugMode)
        {
            Debug.Log($"[GamePoolManager] 오브젝트 생성: {tag} (풀 남은 개수: {pool.Count})");
        }
        
        return objectToSpawn;
    }
    
    /// <summary>
    /// 오브젝트를 풀로 반환 - 올바른 로직
    /// </summary>
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("[GamePoolManager] null 오브젝트를 반환하려고 시도했습니다.");
            return;
        }
        
        if (!poolDictionary.ContainsKey(tag))
        {
            if (enableDebugMode)
            {
                Debug.LogWarning($"[GamePoolManager] 풀에 없는 태그로 반환 시도: {tag}");
            }
            
            Destroy(obj);
            return;
        }
        
        // 오브젝트 비활성화 및 정리
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        
        // ⭐ 핵심 수정: 실제로 풀에 다시 추가
        poolDictionary[tag].Enqueue(obj);
        
        // 활성 풀에서 제거
        string instanceId = obj.GetInstanceID().ToString();
        if (activePools.ContainsKey(instanceId))
        {
            activePools.Remove(instanceId);
        }
        
        if (enableDebugMode)
        {
            Debug.Log($"[GamePoolManager] 오브젝트 반환: {tag} (풀 개수: {poolDictionary[tag].Count})");
        }
        if (enableDebugMode && obj.transform.parent != this.transform)
{
        Debug.LogWarning($"[GamePoolManager] 반환된 오브젝트가 잘못된 위치에 있습니다: {obj.name}");
}
    }
    
    /// <summary>
    /// 풀 확장
    /// </summary>
    public void ExpandPool(string tag, int additionalSize)
    {
        if (!poolDictionary.ContainsKey(tag) || !poolSettings.ContainsKey(tag))
        {
            Debug.LogWarning($"[GamePoolManager] 확장할 수 없는 풀: {tag}");
            return;
        }
        
        Queue<GameObject> pool = poolDictionary[tag];
        ScenePoolConfig.PoolSettings setting = poolSettings[tag];
        
        for (int i = 0; i < additionalSize; i++)
        {
            GameObject obj = Instantiate(setting.prefab);
            obj.name = $"{setting.prefab.name}_Expanded_{i}";
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            pool.Enqueue(obj);
        }
        
        if (enableDebugMode)
        {
            Debug.Log($"[GamePoolManager] 풀 확장: {tag} (+{additionalSize}개, 총: {pool.Count}개)");
        }
    }
    
    #endregion
    
    // 헬퍼 메서드들 (완전 구현)
    private ScenePoolConfig FindSceneConfig(string sceneName)
    {
        // 1. 현재 설정된 config가 일치하면 사용
        if (currentSceneConfig != null && currentSceneConfig.sceneName == sceneName)
        {
            return currentSceneConfig;
        }
        
        // 2. Resources 폴더에서 씬별 설정 파일 찾기
        ScenePoolConfig[] configs = Resources.LoadAll<ScenePoolConfig>("PoolConfigs");
        
        foreach (var config in configs)
        {
            if (config.sceneName == sceneName)
            {
                return config;
            }
        }
        
        // 3. 설정 파일이 없으면 기본 설정 사용 (임시)
        if (enableDebugMode)
        {
            Debug.LogWarning($"[GamePoolManager] 씬 '{sceneName}'의 전용 설정을 찾을 수 없어 기본 설정을 사용합니다.");
        }
        
        return CreateDefaultConfig(sceneName);
    }
    
    private ScenePoolConfig CreateDefaultConfig(string sceneName)
    {
        // 임시 기본 설정 생성
        ScenePoolConfig defaultConfig = ScriptableObject.CreateInstance<ScenePoolConfig>();
        defaultConfig.sceneName = sceneName;
        defaultConfig.enableDebugLogs = enableDebugMode;
        
        // ⭐ 핵심 개선: 씬 타입별 지능적 기본 설정
        if (IsUIOnlyScene(sceneName))
        {
            // UI 전용 씬: 빈 설정
            defaultConfig.requiredPools = new List<ScenePoolConfig.PoolSettings>();
            defaultConfig.optionalPools = new List<ScenePoolConfig.PoolSettings>();
            
            if (enableDebugMode)
            {
                Debug.Log($"[GamePoolManager] UI 전용 씬 '{sceneName}' - 빈 풀 설정 사용");
            }
        }
        else if (IsGameplayScene(sceneName))
        {
            // 게임플레이 씬: 기본 게임플레이 풀들
            defaultConfig.requiredPools = new List<ScenePoolConfig.PoolSettings>
            {
                CreatePoolSetting("Arrow", "Arrow", 15),
                CreatePoolSetting("Ghost Bullet", "Ghost_Bullet", 20),
                CreatePoolSetting("Grape Projectile", "Grape Projectile", 10),
                CreatePoolSetting("Grape Projectile Splatter", "Grape Projectile Splatter", 10),
                CreatePoolSetting("GrapeShadow", "Grape_Shadow", 10)
            };
            defaultConfig.optionalPools = new List<ScenePoolConfig.PoolSettings>();
            
            if (enableDebugMode)
            {
                Debug.Log($"[GamePoolManager] 게임플레이 씬 '{sceneName}' - 기본 게임플레이 풀 설정 사용");
            }
        }
        else
        {
            // 알 수 없는 씬: 안전한 최소 설정
            defaultConfig.requiredPools = new List<ScenePoolConfig.PoolSettings>();
            defaultConfig.optionalPools = new List<ScenePoolConfig.PoolSettings>();
            
            if (enableDebugMode)
            {
                Debug.Log($"[GamePoolManager] 알 수 없는 씬 '{sceneName}' - 빈 풀 설정 사용");
            }
        }
        
        return defaultConfig;
    }
    
    /// <summary>
    /// UI 전용 씬인지 판단 (풀링 불필요한 씬들)
    /// </summary>
    private bool IsUIOnlyScene(string sceneName)
    {
        string[] uiScenes = {
            "Lobby", "StageSelect", "Loading", "Ingame_Loading", 
            "Menu", "Settings", "Credits", "GameOver", "Victory"
        };
        
        foreach (string uiScene in uiScenes)
        {
            if (sceneName.Equals(uiScene, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 게임플레이 씬인지 판단 (전투/액션이 일어나는 씬들)
    /// </summary>
    private bool IsGameplayScene(string sceneName)
    {
        // Scene으로 시작하는 씬들 (Scene1, Scene2, Scene3 등)
        if (sceneName.StartsWith("Scene", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        // 기타 게임플레이 씬 패턴들
        string[] gameplayPatterns = {
            "Battle", "Combat", "Dungeon", "Boss", "Stage", "Level"
        };
        
        foreach (string pattern in gameplayPatterns)
        {
            if (sceneName.Contains(pattern))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private ScenePoolConfig.PoolSettings CreatePoolSetting(string tag, string prefabName, int size)
    {
        var setting = new ScenePoolConfig.PoolSettings();
        setting.tag = tag;
        setting.size = size;
        setting.preloadOnSceneStart = true;
        setting.clearOnSceneExit = false;
        setting.maxInstancesPerFrame = 10;
        
        // Resources나 현재 설정에서 프리팹 찾기 시도
        GameObject prefab = Resources.Load<GameObject>(prefabName);
        if (prefab == null)
        {
            // Prefabs 폴더에서 찾기
            prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        }
        
        setting.prefab = prefab;
        
        if (prefab == null && enableDebugMode)
        {
            Debug.LogWarning($"[GamePoolManager] 프리팹을 찾을 수 없습니다: {prefabName}");
        }
        
        return setting;
    }
    
    private IEnumerator LoadRequiredPools(ScenePoolConfig config)
    {
        if (config == null || config.requiredPools == null)
        {
            if (enableDebugMode)
            {
                Debug.LogWarning("[GamePoolManager] 로드할 필수 풀이 없습니다.");
            }
            yield break;
        }
        
        List<ScenePoolConfig.PoolSettings> poolsToLoad = config.requiredPools;
        int loadedThisFrame = 0;
        
        foreach (var poolSetting in poolsToLoad)
        {
            if (poolSetting == null || string.IsNullOrEmpty(poolSetting.tag))
            {
                continue;
            }
            
            if (loadedPoolTags.Contains(poolSetting.tag))
            {
                if (enableDebugMode)
                {
                    Debug.Log($"[GamePoolManager] 풀 '{poolSetting.tag}' 이미 로드됨, 건너뜀");
                }
                continue;
            }
            
            yield return CreatePool(poolSetting);
            
            loadedThisFrame++;
            if (loadedThisFrame >= maxPoolsLoadPerFrame)
            {
                loadedThisFrame = 0;
                yield return null; // 다음 프레임까지 대기
            }
        }
    }
    
    private IEnumerator UnloadUnusedPools()
    {
        if (poolDictionary == null || poolDictionary.Count == 0)
        {
            yield break;
        }
        
        List<string> poolsToRemove = new List<string>();
        
        foreach (var kvp in poolDictionary)
        {
            string tag = kvp.Key;
            Queue<GameObject> pool = kvp.Value;
            
            // 현재 씬에서 사용하지 않는 풀인지 확인
            bool shouldKeep = false;
            
            if (currentSceneConfig != null)
            {
                var poolSetting = currentSceneConfig.GetPoolByTag(tag);
                if (poolSetting != null)
                {
                    shouldKeep = true;
                }
            }
            
            if (!shouldKeep)
            {
                // clearOnSceneExit 설정 확인
                bool shouldClear = true;
                if (poolSettings.ContainsKey(tag))
                {
                    shouldClear = poolSettings[tag].clearOnSceneExit;
                }
                
                if (shouldClear)
                {
                    // 풀의 모든 오브젝트 파괴
                    while (pool.Count > 0)
                    {
                        GameObject obj = pool.Dequeue();
                        if (obj != null)
                        {
                            DestroyImmediate(obj);
                        }
                    }
                    
                    poolsToRemove.Add(tag);
                    
                    if (enableDebugMode)
                    {
                        Debug.Log($"[GamePoolManager] 풀 언로드: {tag}");
                    }
                }
            }
            
            yield return null; // 매 풀마다 프레임 대기
        }
        
        // 제거 대상 풀들 정리
        foreach (string tag in poolsToRemove)
        {
            poolDictionary.Remove(tag);
            poolSettings.Remove(tag);
            loadedPoolTags.Remove(tag);
        }
    }
    
    private void PrintPoolStats()
    {
        Debug.Log("=== GamePoolManager 풀 상태 ===");
        foreach (var kvp in poolDictionary)
        {
            Debug.Log($"[풀] {kvp.Key}: {kvp.Value.Count}개 대기 중");
        }
        Debug.Log($"[활성] 사용 중인 오브젝트: {activePools.Count}개");
        
        // 🔑 DontDestroyOnLoad 오브젝트 감지
        var allObjects = FindObjectsOfType<GameObject>();
        var dontDestroyObjects = allObjects.Where(obj => 
            obj.scene.name == "DontDestroyOnLoad" && obj.name.Contains("Arrow")).ToList();
        
        if (dontDestroyObjects.Any())
        {
            Debug.LogWarning($"[문제] DontDestroyOnLoad에 Arrow {dontDestroyObjects.Count}개 발견!");
            foreach (var obj in dontDestroyObjects)
            {
                Debug.LogWarning($"  - {obj.name} (활성: {obj.activeInHierarchy})");
            }
        }
        else
        {
            Debug.Log("[정상] DontDestroyOnLoad에 Arrow 누적 없음");
        }
        
        Debug.Log("================================");
    }
    
}
