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

    // ✅ 외부 접근용 프로퍼티 추가
    public bool IsLoadingPools => isLoadingPools;
    
    protected override void Awake()
    {
        base.Awake();
        if (instance != this) return;
        
        InitializeCore();
        RegisterSceneEvents();
    }
    
    private void Start()
    {
        Debug.Log($"🔍 [GamePoolManager] 현재 씬: {currentSceneName}");
        Debug.Log($"🔍 [GamePoolManager] currentSceneConfig: {(currentSceneConfig != null ? currentSceneConfig.name : "NULL")}");
        
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
        
        if (enableDebugMode)
        {
            Debug.Log($"🔄 [GamePoolManager] 씬 전환: → {newSceneName}");
        }
        
        // 🔥 핵심: 완전한 풀 리셋
        DestroyAllPools();
        
        currentSceneName = newSceneName;
        StartCoroutine(LoadScenePoolsCoroutine(newSceneName));
    }
    
    private void OnSceneUnloaded(Scene scene)
    {
        // ⭐ 씬 언로드 시에도 즉시 정리
        StartCoroutine(SafeCleanupAllActiveObjects());
        StartCoroutine(UnloadUnusedPools());
    }
    
    /// <summary>
    /// ⭐ 새로운 메서드: 안전한 모든 활성 오브젝트 정리
    /// </summary>
    private IEnumerator SafeCleanupAllActiveObjects()
    {
        if (enableDebugMode)
        {
            Debug.Log("[GamePoolManager] 안전한 활성 오브젝트 정리 시작...");
        }
        
        // 1. activePools 딕셔너리에서 안전하게 정리
        List<string> keysToRemove = new List<string>();
        List<GameObject> objectsToDestroy = new List<GameObject>();
        
        foreach (var kvp in activePools.ToList()) // ToList()로 안전한 복사본 생성
        {
            try
            {
                GameObject obj = kvp.Value;
                if (obj == null) // 이미 파괴된 오브젝트
                {
                    keysToRemove.Add(kvp.Key);
                    continue;
                }
                
                // 픽업 아이템인지 확인
                if (IsPickupObject(obj))
                {
                    objectsToDestroy.Add(obj);
                    keysToRemove.Add(kvp.Key);
                }
            }
            catch (MissingReferenceException)
            {
                // 이미 파괴된 오브젝트이므로 키만 제거
                keysToRemove.Add(kvp.Key);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[GamePoolManager] activePools 정리 중 예외: {ex.Message}");
                keysToRemove.Add(kvp.Key);
            }
        }
        
        // 2. 씬에서 직접 픽업 오브젝트 찾기 (더 안전한 방법)
        try
        {
            Pickup[] pickups = FindObjectsOfType<Pickup>();
            foreach (Pickup pickup in pickups)
            {
                if (pickup != null && pickup.gameObject != null && !objectsToDestroy.Contains(pickup.gameObject))
                {
                    objectsToDestroy.Add(pickup.gameObject);
                }
            }
            
            if (enableDebugMode)
            {
                Debug.Log($"[GamePoolManager] 발견된 Pickup 오브젝트: {pickups.Length}개");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GamePoolManager] Pickup 오브젝트 찾기 중 예외: {ex.Message}");
        }
        
        // 3. DontDestroyOnLoad에서 픽업 관련 오브젝트 찾기
        try
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj != null && obj.scene.name == "DontDestroyOnLoad" && 
                    obj != this.gameObject && IsPickupObject(obj) && 
                    !objectsToDestroy.Contains(obj))
                {
                    objectsToDestroy.Add(obj);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GamePoolManager] DontDestroyOnLoad 정리 중 예외: {ex.Message}");
        }
        
        // 4. 찾은 오브젝트들 안전하게 정리
        if (objectsToDestroy.Count > 0)
        {
            if (enableDebugMode)
            {
                Debug.Log($"[GamePoolManager] {objectsToDestroy.Count}개 오브젝트 정리 중...");
            }
            
            foreach (GameObject obj in objectsToDestroy)
            {
                try
                {
                    if (obj != null)
                    {
                        // 풀로 반환 시도
                        string poolTag = DeterminePickupPoolTag(obj);
                        if (!string.IsNullOrEmpty(poolTag) && poolDictionary.ContainsKey(poolTag))
                        {
                            obj.SetActive(false);
                            obj.transform.SetParent(transform);
                            obj.transform.localPosition = Vector3.zero;
                            poolDictionary[poolTag].Enqueue(obj);
                            
                            if (enableDebugMode)
                            {
                                Debug.Log($"[GamePoolManager] '{obj.name}'을 '{poolTag}' 풀로 반환");
                            }
                        }
                        else
                        {
                            // 풀이 없으면 파괴
                            DestroyImmediate(obj);
                            if (enableDebugMode)
                            {
                                Debug.Log($"[GamePoolManager] '{obj.name}' 파괴 (풀 없음)");
                            }
                        }
                    }
                }
                catch (MissingReferenceException)
                {
                    // 이미 파괴된 오브젝트이므로 무시
                    if (enableDebugMode)
                    {
                        Debug.Log("[GamePoolManager] 이미 파괴된 오브젝트 건너뜀");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[GamePoolManager] 오브젝트 정리 중 예외: {ex.Message}");
                }
                
                yield return null; // 매 오브젝트마다 프레임 대기
            }
        }
        
        // 5. activePools에서 키 제거
        foreach (string key in keysToRemove)
        {
            activePools.Remove(key);
        }
        
        if (enableDebugMode)
        {
            Debug.Log($"[GamePoolManager] 활성 오브젝트 정리 완료 - 정리된 오브젝트: {objectsToDestroy.Count}개, 제거된 키: {keysToRemove.Count}개");
        }
    }
    
    /// <summary>
    /// ⭐ 헬퍼 메서드: 픽업 오브젝트인지 안전하게 판단
    /// </summary>
    private bool IsPickupObject(GameObject obj)
    {
        if (obj == null) return false;
        
        try
        {
            // Pickup 컴포넌트 확인
            if (obj.GetComponent<Pickup>() != null)
            {
                return true;
            }
            
            // 이름으로 확인
            string name = obj.name.ToLower();
            if (name.Contains("gold") || name.Contains("coin") || 
                name.Contains("health") || name.Contains("stamina"))
            {
                return true;
            }
        }
        catch (MissingReferenceException)
        {
            return false; // 이미 파괴된 오브젝트
        }
        catch (System.Exception)
        {
            return false; // 기타 예외
        }
        
        return false;
    }
    
    /// <summary>
    /// ⭐ 새로운 메서드: 씬 전환 시 활성화된 픽업 아이템들을 풀로 반환
    /// </summary>
    private IEnumerator ReturnActivePickupsToPool()
    {
        // 현재 씬에 활성화된 모든 Pickup 오브젝트 찾기
        Pickup[] activePickups = FindObjectsOfType<Pickup>();
        
        if (activePickups.Length > 0)
        {
            if (enableDebugMode)
            {
                Debug.Log($"[GamePoolManager] 씬 전환 시 활성화된 픽업 아이템 {activePickups.Length}개 발견, 풀로 반환 중...");
            }
            
            foreach (Pickup pickup in activePickups)
            {
                if (pickup != null && pickup.gameObject.activeInHierarchy)
                {
                    // Pickup 오브젝트를 즉시 비활성화하여 Update() 실행 중지
                    pickup.gameObject.SetActive(false);
                    
                    // 적절한 풀 태그 결정 후 반환
                    string poolTag = DeterminePickupPoolTag(pickup.gameObject);
                    if (!string.IsNullOrEmpty(poolTag) && poolDictionary.ContainsKey(poolTag))
                    {
                        ReturnToPool(poolTag, pickup.gameObject);
                    }
                    else
                    {
                        // 풀이 없으면 파괴
                        DestroyImmediate(pickup.gameObject);
                        if (enableDebugMode)
                        {
                            Debug.LogWarning($"[GamePoolManager] 픽업 아이템 '{pickup.name}'의 풀을 찾을 수 없어 파괴했습니다.");
                        }
                    }
                }
                
                yield return null; // 매 아이템마다 프레임 대기
            }
            
            if (enableDebugMode)
            {
                Debug.Log("[GamePoolManager] 활성화된 픽업 아이템 정리 완료");
            }
        }
    }
    
    /// <summary>
    /// ⭐ 새로운 메서드: clearOnSceneExit = true인 풀들의 활성 오브젝트 즉시 정리
    /// </summary>
    private IEnumerator CleanupClearOnExitPools()
    {
        if (poolSettings == null || poolSettings.Count == 0)
        {
            yield break;
        }
        
        List<string> tagsToCleanup = new List<string>();
        
        // clearOnSceneExit = true인 태그들 찾기
        foreach (var kvp in poolSettings)
        {
            if (kvp.Value.clearOnSceneExit)
            {
                tagsToCleanup.Add(kvp.Key);
            }
        }
        
        if (tagsToCleanup.Count > 0)
        {
            if (enableDebugMode)
            {
                Debug.Log($"[GamePoolManager] clearOnSceneExit 태그들의 활성 오브젝트 정리: {string.Join(", ", tagsToCleanup)}");
            }
            
            // 각 태그별로 활성 오브젝트 정리
            foreach (string tag in tagsToCleanup)
            {
                yield return StartCoroutine(CleanupActiveObjectsByTag(tag));
            }
            
            if (enableDebugMode)
            {
                Debug.Log("[GamePoolManager] clearOnSceneExit 활성 오브젝트 정리 완료");
            }
        }
    }
    
    /// <summary>
    /// ⭐ 헬퍼 메서드: 픽업 오브젝트의 풀 태그 결정
    /// </summary>
    private string DeterminePickupPoolTag(GameObject pickupObject)
    {
        string objectName = pickupObject.name.Replace("(Clone)", "").Trim();
        
        // 일반적인 픽업 아이템 매핑
        if (objectName.Contains("Gold") || objectName.Contains("Coin"))
        {
            return "Gold Coin";
        }
        else if (objectName.Contains("Health"))
        {
            return "Health";
        }
        
        // 정확한 이름 매핑이 안되면 원본 이름 반환
        return objectName;
    }
    
    private IEnumerator LoadCurrentScenePools()
    {
        yield return LoadScenePoolsCoroutine(currentSceneName);
    }
    
    private IEnumerator LoadScenePoolsCoroutine(string sceneName)
    {
        Debug.Log($"🔄 [GamePoolManager] 풀 로딩 시작: {sceneName}");
        
        if (isLoadingPools) 
        {
            Debug.LogWarning($"⚠️ [GamePoolManager] 이미 풀 로딩 중입니다. 중복 요청 무시.");
            yield break;
        }
        
        isLoadingPools = true;
        Debug.Log($"🔒 [GamePoolManager] 풀 로딩 상태: isLoadingPools = true");
        
        // 기존 풀 정리
        yield return UnloadUnusedPools();
        
        // 씬 설정 찾기
        ScenePoolConfig config = FindSceneConfig(sceneName);
        if (config == null)
        {
            Debug.LogWarning($"[GamePoolManager] 씬 '{sceneName}'의 풀 설정을 찾을 수 없습니다.");
            isLoadingPools = false;
            Debug.Log($"🔓 [GamePoolManager] 풀 로딩 상태: isLoadingPools = false (설정 없음)");
            yield break;
        }
        
        Debug.Log($"📋 [GamePoolManager] 씬 설정 발견: {config.name}, 필수 풀: {config.requiredPools?.Count}개");
        
        currentSceneConfig = config;
        
        if (!config.ValidateConfig())
        {
            Debug.LogError($"[GamePoolManager] 씬 '{sceneName}'의 풀 설정이 유효하지 않습니다.");
            isLoadingPools = false;
            Debug.Log($"🔓 [GamePoolManager] 풀 로딩 상태: isLoadingPools = false (설정 무효)");
            yield break;
        }
        
        // 필수 풀 로드
        yield return LoadRequiredPools(config);
        
        isLoadingPools = false;
        Debug.Log($"🔓 [GamePoolManager] 풀 로딩 상태: isLoadingPools = false (완료)");
        
        Debug.Log($"✅ [GamePoolManager] 씬 '{sceneName}' 풀 로딩 완료. 총 풀: {poolDictionary.Count}개");
        
        // 🔍 로딩된 풀 목록 출력
        Debug.Log($"📊 [GamePoolManager] 로딩된 풀 목록:");
        foreach (var poolTag in poolDictionary.Keys)
        {
            Debug.Log($"   - {poolTag}: {poolDictionary[poolTag].Count}개");
        }
    }
    
    /// <summary>
    /// 개별 풀 생성 메서드 (단순화 완료)
    /// </summary>
    private IEnumerator CreatePool(ScenePoolConfig.PoolSettings poolSetting)
    {
        if (poolDictionary.ContainsKey(poolSetting.tag))
        {
            Debug.LogWarning($"[GamePoolManager] 풀 '{poolSetting.tag}'가 이미 존재합니다.");
            yield break;
        }
        
        // 🔧 단순화: 직접 프리팹만 사용
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
    
    /// <summary>
    /// 🆕 풀링된 적 오브젝트 초기화 (EnemyData 연결)
    /// </summary>
    private void InitializePooledEnemyObject(GameObject pooledObject, EnemyData enemyData)
    {
        // BaseEnemy 컴포넌트에 EnemyData 연결
        BaseEnemy baseEnemy = pooledObject.GetComponent<BaseEnemy>();
        if (baseEnemy != null)
        {
            // 리플렉션을 통한 EnemyData 설정
            var enemyDataField = typeof(BaseEnemy).GetField("enemyData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (enemyDataField != null)
            {
                enemyDataField.SetValue(baseEnemy, enemyData);
                
                if (enableDebugMode)
                {
                    Debug.Log($"🔗 [GamePoolManager] 풀 오브젝트에 EnemyData 연결: {pooledObject.name} ← {enemyData.EnemyName}");
                }
            }
        }
    }
    
    #endregion
    
    #region Pool Operations (핵심 수정된 로직)
    
    /// <summary>
    /// 풀에서 오브젝트 스폰
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        // 풀 존재 확인
        if (!poolDictionary.ContainsKey(tag))
        {
            if (enableDebugMode)
            {
                Debug.LogError($"🏭 [GamePoolManager] 풀에 없는 태그: {tag}");
                Debug.LogError($"🔍 [GamePoolManager] 현재 사용 가능한 풀들:");
                foreach (var poolTag in poolDictionary.Keys)
                {
                    Debug.LogError($"  - {poolTag}: {poolDictionary[poolTag].Count}개");
                }
            }
            return null;
        }
        
        // 풀에서 오브젝트 가져오기
        Queue<GameObject> objectPool = poolDictionary[tag];
        Debug.Log($"📊 [테스트] {tag} 풀 크기: {objectPool.Count}개 대기 중");
        
        // 풀이 비어있으면 확장
        if (objectPool.Count == 0)
        {
            Debug.LogWarning($"🏭 [GamePoolManager] 풀 '{tag}'가 비어있음, 확장 중...");
            
            ExpandPool(tag, 5); // 긴급 확장
            
            if (objectPool.Count == 0)
            {
                Debug.LogError($"🏭 [GamePoolManager] 풀 '{tag}' 확장 실패!");
                return null;
            }
        }
        
        // ⭐ 핵심 수정: Dequeue만 하고 다시 Enqueue하지 않음
        GameObject objectToSpawn = objectPool.Dequeue();
        
        // null 체크 및 재생성
        if (objectToSpawn == null)
        {
            Debug.LogError($"🏭 [GamePoolManager] {tag} Dequeue했는데 null!");
            
            if (poolSettings.ContainsKey(tag))
            {
                objectToSpawn = Instantiate(poolSettings[tag].prefab);
                objectToSpawn.name = $"{poolSettings[tag].prefab.name}_Emergency";
                Debug.Log($"🏭 [GamePoolManager] 긴급 생성: {objectToSpawn.name}");
            }
            else
            {
                Debug.LogError($"🏭 [GamePoolManager] 풀 '{tag}'의 설정을 찾을 수 없습니다!");
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
        
        Debug.Log($"🏭 [GamePoolManager] 오브젝트 생성 성공: {tag} (이름: {objectToSpawn.name}, 위치: {objectToSpawn.transform.position}, 활성화: {objectToSpawn.activeInHierarchy})");
        Debug.Log($"📊 [GamePoolManager] 풀 남은 개수: {objectPool.Count}");
        
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
        
        // 2. 🆕 신규 경로 우선 시도 (Stages/ScenePools)
        string newConfigPath = $"Stages/ScenePools/{sceneName}_PoolConfig";
        ScenePoolConfig newConfig = Resources.Load<ScenePoolConfig>(newConfigPath);
        
        if (newConfig != null)
        {
            if (enableDebugMode)
            {
                Debug.Log($"🎯 [GamePoolManager] 신규 경로에서 풀 설정 로드: {sceneName}");
            }
            return newConfig;
        }
        
        // 3. 🔄 기존 경로 fallback (PoolConfigs)
        ScenePoolConfig[] configs = Resources.LoadAll<ScenePoolConfig>("PoolConfigs");
        
        foreach (var config in configs)
        {
            if (config.sceneName == sceneName)
            {
                if (enableDebugMode)
                {
                    Debug.Log($"⚠️ [GamePoolManager] 기존 경로에서 풀 설정 로드: {sceneName} (신규 경로로 이전 권장)");
                }
                return config;
            }
        }
        
        // 4. 설정 파일이 없으면 기본 설정 사용 (임시)
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
        if (sceneName.Equals("Lobby", System.StringComparison.OrdinalIgnoreCase))
        {
            // 🆕 Lobby 전용: UI VFX 풀들 포함
            defaultConfig.requiredPools = new List<ScenePoolConfig.PoolSettings>
            {
                // UI VFX 풀들
                CreatePoolSetting("ButtonClickVFX", "ButtonClickVFX", 5),
                CreatePoolSetting("PanelOpenVFX", "PanelOpenVFX", 3),
                CreatePoolSetting("PanelCloseVFX", "PanelCloseVFX", 3),
                CreatePoolSetting("ItemEquipVFX", "ItemEquipVFX", 5),
                CreatePoolSetting("ShopBuyVFX", "ShopBuyVFX", 3),
                CreatePoolSetting("InventoryFullVFX", "InventoryFullVFX", 3),
                
                // 기본 VFX (fallback용)
                CreatePoolSetting("Death VFX", "Death VFX", 10),
                CreatePoolSetting("Barrel VFX", "Barrel VFX", 5)
            };
            
            defaultConfig.optionalPools = new List<ScenePoolConfig.PoolSettings>();
            
            Debug.Log($"🎮 [GamePoolManager] Lobby 씬 - UI VFX 풀 설정 사용 ({defaultConfig.requiredPools.Count}개 풀)");
        }
        else if (IsUIOnlyScene(sceneName))
        {
            // 다른 UI 전용 씬: 빈 설정
            defaultConfig.requiredPools = new List<ScenePoolConfig.PoolSettings>();
            defaultConfig.optionalPools = new List<ScenePoolConfig.PoolSettings>();
            
            if (enableDebugMode)
            {
                Debug.Log($"[GamePoolManager] UI 전용 씬 '{sceneName}' - 빈 풀 설정 사용");
            }
        }
        else if (IsGameplayScene(sceneName))
        {
            // 🔧 단순화: 기본적인 공통 풀들만 포함, ScenePoolConfig 우선 사용
            defaultConfig.requiredPools = new List<ScenePoolConfig.PoolSettings>
            {
                // 기본 발사체들 (모든 씬에서 공통 사용)
                CreatePoolSetting("Arrow", "Arrow", 15),
                CreatePoolSetting("Ghost Bullet", "Ghost_Bullet", 20),
                CreatePoolSetting("Grape Projectile", "Grape Projectile", 10),
                CreatePoolSetting("Grape Projectile Splatter", "Grape Projectile Splatter", 10),
                CreatePoolSetting("GrapeShadow", "Grape_Shadow", 10),
                
                // 기본 VFX
                CreatePoolSetting("Death VFX", "Death VFX", 10)
            };
            
            if (enableDebugMode)
            {
                Debug.Log($"[GamePoolManager] 게임플레이 씬 '{sceneName}' - 기본 공통 풀만 설정, 나머지는 ScenePoolConfig 우선 사용");
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
    
    /// <summary>
    /// 🆕 EnemyData로부터 풀 태그 생성
    /// </summary>
    private string GeneratePoolTagFromEnemyData(EnemyData enemyData)
    {
        // EnemyId 기반 태그 생성: MON_BLUESLIME_001 → Blue_slime
        string enemyId = enemyData.EnemyId;
        
        if (enemyId.Contains("BLUESLIME"))
            return "Blue_slime";
        else if (enemyId.Contains("GRAPE"))
            return "Enemie1";
        else if (enemyId.Contains("GHOST"))
            return "Ghost";
        else if (enemyId.Contains("FINALBOSSA"))
            return "FinalBossA";
        else if (enemyId.Contains("FINALBOSSB"))
            return "FinalBossB";
        else if (enemyId.Contains("FINALBOSSC"))
            return "FinalBossC";
        
        // 기본값: EnemyName을 태그로 사용
        return enemyData.EnemyName.Replace(" ", "_");
    }
    
    private ScenePoolConfig.PoolSettings CreatePoolSetting(string tag, string prefabName, int size)
    {
        var setting = new ScenePoolConfig.PoolSettings();
        setting.tag = tag;
        setting.size = size;
        setting.preloadOnSceneStart = true;
        setting.clearOnSceneExit = false;
        setting.maxInstancesPerFrame = 10;
        
        // 🔧 수정: 다양한 경로에서 프리팹 찾기 시도
        GameObject prefab = Resources.Load<GameObject>(prefabName);
        if (prefab == null)
        {
            // Prefabs 폴더에서 찾기
            prefab = Resources.Load<GameObject>("Prefabs/" + prefabName);
        }
        if (prefab == null)
        {
            // 🆕 추가: VFX 폴더에서 찾기
            prefab = Resources.Load<GameObject>("Prefabs/VFX/" + prefabName);
        }
        if (prefab == null)
        {
            // 🆕 추가: Pickup 폴더에서 찾기
            prefab = Resources.Load<GameObject>("Prefabs/Pickup/" + prefabName);
        }
        
        setting.prefab = prefab;
        
        if (prefab == null && enableDebugMode)
        {
            Debug.LogWarning($"[GamePoolManager] 프리팹을 찾을 수 없습니다: {prefabName}");
        }
        else if (prefab != null && enableDebugMode)
        {
            Debug.Log($"✅ [GamePoolManager] 프리팹 발견: {prefabName} → {prefab.name}");
        }
        
        return setting;
    }
    
    private IEnumerator LoadRequiredPools(ScenePoolConfig config)
    {
        Debug.Log($"🔄 [GamePoolManager] 필수 풀 로딩 시작: {config.sceneName}");
        
        if (config == null || config.requiredPools == null)
        {
            Debug.LogWarning("[GamePoolManager] 로드할 필수 풀이 없습니다.");
            yield break;
        }
        
        Debug.Log($"📋 [GamePoolManager] 로드할 풀 개수: {config.requiredPools.Count}");
        
        List<ScenePoolConfig.PoolSettings> poolsToLoad = config.requiredPools;
        int loadedThisFrame = 0;
        
        foreach (var poolSetting in poolsToLoad)
        {
            if (poolSetting == null || string.IsNullOrEmpty(poolSetting.tag))
            {
                Debug.LogWarning($"⚠️ [GamePoolManager] 잘못된 풀 설정 건너뜀");
                continue;
            }
            
            Debug.Log($"🔍 [GamePoolManager] 풀 로딩 시도: '{poolSetting.tag}' (프리팹: {poolSetting.prefab?.name})");
            
            if (loadedPoolTags.Contains(poolSetting.tag))
            {
                Debug.Log($"✅ [GamePoolManager] 풀 '{poolSetting.tag}' 이미 로드됨, 건너뜀");
                continue;
            }
            
            yield return CreatePool(poolSetting);
            
            // 🔍 로딩 후 상태 확인
            if (poolDictionary.ContainsKey(poolSetting.tag))
            {
                Debug.Log($"✅ [GamePoolManager] 풀 '{poolSetting.tag}' 로딩 성공: {poolDictionary[poolSetting.tag].Count}개");
            }
            else
            {
                Debug.LogError($"❌ [GamePoolManager] 풀 '{poolSetting.tag}' 로딩 실패!");
            }
            
            loadedThisFrame++;
            if (loadedThisFrame >= maxPoolsLoadPerFrame)
            {
                loadedThisFrame = 0;
                yield return null; // 다음 프레임까지 대기
            }
        }
        
        Debug.Log($"🎯 [GamePoolManager] 필수 풀 로딩 완료. 총 풀 개수: {poolDictionary.Count}");
    }
    
    private IEnumerator UnloadUnusedPools()
    {
        if (poolDictionary == null || poolDictionary.Count == 0)
        {
            yield break;
        }
        
        // 🆕 영구 보존할 공통 풀들 정의 (개별 장비 풀 추가)
        HashSet<string> essentialPools = new HashSet<string>
        {
            "Health", "Gold Coin", "Equipment", "Death VFX",
            "Arrow", "Ghost Bullet", "Grape Projectile", "Grape Projectile Splatter",
            // 🆕 개별 장비 풀들도 보존
            "Sword_A_Pickup", "Sword_B_Pickup", "Sword_C_Pickup", "Sword_S_Pickup",
            "Bow_A_Pickup", "Bow_B_Pickup", "Bow_C_Pickup", "Bow_S_Pickup"
        };
        
        List<string> poolsToRemove = new List<string>();
        
        foreach (var kvp in poolDictionary.ToList())
        {
            string tag = kvp.Key;
            Queue<GameObject> pool = kvp.Value;
            
            // 🔑 핵심 수정: 필수 풀들은 절대 삭제하지 않음
            if (essentialPools.Contains(tag))
            {
                if (enableDebugMode)
                {
                    Debug.Log($"[GamePoolManager] 필수 풀 보존: {tag} ({pool.Count}개)");
                }
                continue; // 삭제 대상에서 제외
            }
            
            // 나머지 로직은 동일...
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
                bool shouldClear = true;
                if (poolSettings.ContainsKey(tag))
                {
                    shouldClear = poolSettings[tag].clearOnSceneExit;
                }
                
                if (shouldClear)
                {
                    while (pool.Count > 0)
                    {
                        GameObject obj = pool.Dequeue();
                        if (obj != null)
                        {
                            try
                            {
                                DestroyImmediate(obj);
                            }
                            catch (MissingReferenceException)
                            {
                                // 이미 파괴된 오브젝트, 무시
                            }
                        }
                    }
                    
                    poolsToRemove.Add(tag);
                    
                    if (enableDebugMode)
                    {
                        Debug.Log($"[GamePoolManager] 임시 풀 언로드: {tag}");
                    }
                }
            }
            
            yield return null;
        }
        
        // 제거 대상 풀들 정리
        foreach (string tag in poolsToRemove)
        {
            poolDictionary.Remove(tag);
            poolSettings.Remove(tag);
            loadedPoolTags.Remove(tag);
        }
        
        if (enableDebugMode)
        {
            Debug.Log($"[GamePoolManager] 풀 정리 완료 - 보존: {poolDictionary.Count}개, 제거: {poolsToRemove.Count}개");
        }
    }
    
    /// <summary>
    /// ⭐ 새로운 메서드: 특정 태그의 활성화된 오브젝트들 정리
    /// </summary>
    private IEnumerator CleanupActiveObjectsByTag(string tag)
    {
        List<string> keysToRemove = new List<string>();
        List<GameObject> objectsToDestroy = new List<GameObject>();
        
        // activePools에서 해당 태그의 오브젝트들 찾기
        foreach (var kvp in activePools)
        {
            GameObject obj = kvp.Value;
            if (obj != null && ShouldObjectBeCleanedByTag(obj, tag))
            {
                keysToRemove.Add(kvp.Key);
                objectsToDestroy.Add(obj);
            }
        }
        
        // 추가로 씬에서 직접 찾기 (activePools에 등록되지 않은 것들)
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj != null && ShouldObjectBeCleanedByTag(obj, tag) && !objectsToDestroy.Contains(obj))
            {
                objectsToDestroy.Add(obj);
            }
        }
        
        if (objectsToDestroy.Count > 0)
        {
            if (enableDebugMode)
            {
                Debug.Log($"[GamePoolManager] '{tag}' 태그의 활성 오브젝트 {objectsToDestroy.Count}개 정리 중...");
            }
            
            // 활성 오브젝트들 정리
            foreach (GameObject obj in objectsToDestroy)
            {
                if (obj != null)
                {
                    // activePools에서 제거
                    string instanceId = obj.GetInstanceID().ToString();
                    if (activePools.ContainsKey(instanceId))
                    {
                        activePools.Remove(instanceId);
                    }
                    
                    // 즉시 파괴
                    DestroyImmediate(obj);
                    
                    if (enableDebugMode)
                    {
                        Debug.Log($"[GamePoolManager] 활성 오브젝트 정리: {obj.name} (태그: {tag})");
                    }
                }
                
                yield return null; // 매 오브젝트마다 프레임 대기
            }
            
            if (enableDebugMode)
            {
                Debug.Log($"[GamePoolManager] '{tag}' 태그의 활성 오브젝트 정리 완료");
            }
        }
    }
    
    /// <summary>
    /// ⭐ 헬퍼 메서드: 오브젝트가 특정 태그로 정리되어야 하는지 판단
    /// </summary>
    private bool ShouldObjectBeCleanedByTag(GameObject obj, string tag)
    {
        if (obj == null) return false;
        
        // 오브젝트 이름으로 태그 매칭
        string objectName = obj.name.Replace("(Clone)", "").Trim();
        
        // "_숫자" 패턴 제거
        int underscoreIndex = objectName.LastIndexOf('_');
        if (underscoreIndex > 0)
        {
            string afterUnderscore = objectName.Substring(underscoreIndex + 1);
            if (int.TryParse(afterUnderscore, out _))
            {
                objectName = objectName.Substring(0, underscoreIndex);
            }
        }
        
        // 태그와 매칭 확인
        if (objectName.Equals(tag, System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        // 픽업 아이템 특별 처리
        if (tag == "Health" && (objectName.Contains("Health") || obj.GetComponent<Pickup>() != null))
        {
            return true;
        }
        if (tag == "Gold Coin" && (objectName.Contains("Gold") || objectName.Contains("Coin") || obj.GetComponent<Pickup>() != null))
        {
            return true;
        }
        
        return false;
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
    
    /// <summary>
    /// 🆕 모든 풀 완전 파괴
    /// </summary>
    private void DestroyAllPools()
    {
        if (enableDebugMode)
            Debug.Log("🧹 [GamePoolManager] 모든 풀 완전 파괴 시작");
        
        // 1. 모든 풀 오브젝트 파괴
        foreach (var pool in poolDictionary.Values)
        {
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                    DestroyImmediate(obj);
            }
        }
        
        // 2. 활성 오브젝트들도 파괴
        foreach (var activeObj in activePools.Values)
        {
            if (activeObj != null)
                DestroyImmediate(activeObj);
        }
        
        // 3. 모든 딕셔너리 클리어
        poolDictionary.Clear();
        activePools.Clear();
        poolSettings.Clear();
        loadedPoolTags.Clear();
        
        if (enableDebugMode)
            Debug.Log("✅ [GamePoolManager] 모든 풀 완전 파괴 완료");
    }
    
    #region 🎯 스테이지 풀링 시스템
    
    /// <summary>
    /// 스테이지별 풀 설정 로드
    /// </summary>
    public bool LoadStagePoolConfig(string stageId)
    {
        string configPath = $"Stages/ScenePools/{stageId}_PoolConfig";
        ScenePoolConfig stagePoolConfig = Resources.Load<ScenePoolConfig>(configPath);
        
        if (stagePoolConfig != null)
        {
            Debug.Log($"🎯 [GamePoolManager] 스테이지 풀 설정 로드됨: {stageId}");
            
            // 기존 currentSceneConfig와 병합
            MergeWithCurrentConfig(stagePoolConfig);
            
            // 스테이지 전용 풀들 사전 로드
            StartCoroutine(WarmupStageRequiredPools(stagePoolConfig));
            
            return true;
        }
        else
        {
            Debug.LogWarning($"⚠️ [GamePoolManager] 스테이지 풀 설정을 찾을 수 없음: {stageId}. 기본 설정 사용.");
            return false;
        }
    }
    
    /// <summary>
    /// 스테이지 풀 설정과 현재 설정 병합
    /// </summary>
    private void MergeWithCurrentConfig(ScenePoolConfig stagePoolConfig)
    {
        if (currentSceneConfig == null)
        {
            currentSceneConfig = stagePoolConfig;
            return;
        }
        
        // 스테이지 전용 풀들을 현재 설정에 추가
        foreach (var stagePool in stagePoolConfig.requiredPools)
        {
            // 중복 체크
            bool exists = currentSceneConfig.requiredPools.Exists(p => p.tag == stagePool.tag);
            if (!exists)
            {
                currentSceneConfig.requiredPools.Add(stagePool);
                Debug.Log($"[GamePoolManager] 스테이지 풀 추가: {stagePool.tag} (사이즈: {stagePool.size})");
            }
            else
            {
                // 기존 풀 사이즈 업데이트
                var existingPool = currentSceneConfig.requiredPools.Find(p => p.tag == stagePool.tag);
                if (existingPool != null && stagePool.size > existingPool.size)
                {
                    existingPool.size = stagePool.size;
                    Debug.Log($"[GamePoolManager] 풀 사이즈 업데이트: {stagePool.tag} → {stagePool.size}");
                }
            }
        }
    }
    
    /// <summary>
    /// 스테이지 필요 풀들 사전 로드
    /// </summary>
    private IEnumerator WarmupStageRequiredPools(ScenePoolConfig stagePoolConfig)
    {
        Debug.Log($"🔥 [GamePoolManager] 스테이지 풀 Warmup 시작: {stagePoolConfig.requiredPools.Count}개 풀");
        
        int loadedCount = 0;
        int totalCount = stagePoolConfig.requiredPools.Count;
        
        foreach (var poolSetting in stagePoolConfig.requiredPools)
        {
            if (!poolDictionary.ContainsKey(poolSetting.tag))
            {
                yield return StartCoroutine(CreatePoolAsync(poolSetting));
                loadedCount++;
                
                if (enableDebugMode)
                {
                    Debug.Log($"[GamePoolManager] 스테이지 풀 로드 진행: {loadedCount}/{totalCount}");
                }
                
                // 프레임 분산 로딩
                if (loadedCount % maxPoolsLoadPerFrame == 0)
                {
                    yield return null;
                }
            }
        }
        
        Debug.Log($"✅ [GamePoolManager] 스테이지 풀 Warmup 완료: {loadedCount}개 풀 로드됨");
    }
    
    /// <summary>
    /// 비동기 풀 생성
    /// </summary>
    private IEnumerator CreatePoolAsync(ScenePoolConfig.PoolSettings poolSetting)
    {
        if (poolSetting.prefab == null)
        {
            Debug.LogWarning($"[GamePoolManager] 프리팹이 null입니다: {poolSetting.tag}");
            yield break;
        }
        
        Queue<GameObject> pool = new Queue<GameObject>();
        
        for (int i = 0; i < poolSetting.size; i++)
        {
            GameObject obj = Instantiate(poolSetting.prefab);
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            pool.Enqueue(obj);
            
            // 프레임당 생성 수 제한
            if (i % poolSetting.maxInstancesPerFrame == 0)
            {
                yield return null;
            }
        }
        
        poolDictionary[poolSetting.tag] = pool;
        poolSettings[poolSetting.tag] = poolSetting;
        loadedPoolTags.Add(poolSetting.tag);
        
        if (enableDebugMode)
        {
            Debug.Log($"[GamePoolManager] 비동기 풀 생성 완료: {poolSetting.tag} ({poolSetting.size}개)");
        }
    }
    
    /// <summary>
    /// 스테이지 종료 시 풀 정리 (선택적)
    /// </summary>
    public void CleanupStageSpecificPools()
    {
        // 스테이지 전용 풀들만 정리 (기본 풀들은 유지)
        var stageSpecificTags = new List<string> { "Blue_slime", "Enemie1", "Ghost", "FinalBossA", "FinalBossB", "FinalBossC" };
        
        foreach (string tag in stageSpecificTags)
        {
            if (poolDictionary.ContainsKey(tag))
            {
                // 활성 오브젝트들 비활성화
                foreach (var obj in activePools.Values)
                {
                    if (obj != null && obj.CompareTag(tag))
                    {
                        obj.SetActive(false);
                    }
                }
                
                Debug.Log($"[GamePoolManager] 스테이지 풀 정리: {tag}");
            }
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    #endregion
    
    #region 🧪 테스트 및 검증 메서드
    
    /// <summary>
    /// 🧪 몬스터 풀링 시스템 전체 테스트
    /// </summary>
    [ContextMenu("🧪 Test Monster Pooling System")]
    public void TestMonsterPoolingSystem()
    {
        Debug.Log($"🧪 ===== 몬스터 풀링 시스템 전체 테스트 시작 =====");
        
        // 1. EnemyData 로드 테스트
        TestEnemyDataLoading();
        
        // 2. 풀 생성 테스트
        TestPoolCreation();
        
        // 3. 스폰 테스트
        TestMonsterSpawning();
        
        Debug.Log($"🧪 ===== 몬스터 풀링 시스템 전체 테스트 완료 =====");
    }
    
    /// <summary>
    /// 🧪 EnemyData 로드 테스트
    /// </summary>
    private void TestEnemyDataLoading()
    {
        Debug.Log($"🔍 [테스트] EnemyData 로드 테스트 시작");
        
        string[] testMonsterIds = {
            "MON_BLUESLIME_001",
            "MON_GRAPE_001", 
            "MON_GHOST_001",
            "MON_BLUESLIME_001_BOSS"
        };
        
        foreach (string monsterId in testMonsterIds)
        {
            // StageManager의 GetEnemyDataFromMonsterID 로직 시뮬레이션
            string[] possiblePaths = {
                $"EnemyData/{GetEnemyDataFileNameForTest(monsterId)}",
                $"EnemyData/{monsterId}Data",
                $"EnemyData/{monsterId}"
            };
            
            bool found = false;
            foreach (string path in possiblePaths)
            {
                EnemyData enemyData = Resources.Load<EnemyData>(path);
                if (enemyData != null)
                {
                    GameObject prefab = enemyData.GetPoolingPrefab();
                    Debug.Log($"✅ [테스트] {monsterId}: EnemyData 로드 성공 → {path}");
                    Debug.Log($"  - EnemyName: {enemyData.EnemyName}");
                    Debug.Log($"  - Prefab: {(prefab != null ? prefab.name : "NULL")}");
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                Debug.LogError($"❌ [테스트] {monsterId}: EnemyData 로드 실패!");
            }
        }
    }
    
    /// <summary>
    /// 🧪 풀 생성 테스트
    /// </summary>
    private void TestPoolCreation()
    {
        Debug.Log($"🏭 [테스트] 풀 생성 테스트 시작");
        
        string[] expectedPoolTags = {
            "Blue_slime",
            "Enemie1",
            "Ghost"
        };
        
        foreach (string poolTag in expectedPoolTags)
        {
            if (poolDictionary.ContainsKey(poolTag))
            {
                int poolSize = poolDictionary[poolTag].Count;
                Debug.Log($"✅ [테스트] 풀 존재 확인: {poolTag} ({poolSize}개)");
            }
            else
            {
                Debug.LogError($"❌ [테스트] 풀 누락: {poolTag}");
            }
        }
        
        Debug.Log($"📊 [테스트] 전체 풀 현황: {poolDictionary.Count}개 풀 등록됨");
    }
    
    /// <summary>
    /// 🧪 몬스터 스폰 테스트
    /// </summary>
    private void TestMonsterSpawning()
    {
        Debug.Log($"🎯 [테스트] 몬스터 스폰 테스트 시작");
        
        // StageManager가 있는지 확인
        if (StageSystem.StageManager.Instance == null)
        {
            Debug.LogWarning($"⚠️ [테스트] StageManager가 없어서 스폰 테스트 건너뜀");
            return;
        }
        
        // 테스트용 MonsterSpawnData 생성
        var testMonsterData = new StageSystem.MonsterSpawnData("MON_BLUESLIME_001", 1, 1f, false);
        Vector3 testPosition = Vector3.zero;
        
        // 스폰 시도
        GameObject spawnedMonster = StageSystem.StageManager.Instance.SpawnMonster(testMonsterData, testPosition);
        
        if (spawnedMonster != null)
        {
            Debug.Log($"✅ [테스트] 몬스터 스폰 성공: {spawnedMonster.name}");
            
            // 즉시 정리 (테스트용)
            if (Application.isPlaying)
            {
                Destroy(spawnedMonster);
            }
        }
        else
        {
            Debug.LogError($"❌ [테스트] 몬스터 스폰 실패!");
        }
    }
    
    /// <summary>
    /// 🧪 테스트용 EnemyData 파일명 변환
    /// </summary>
    private string GetEnemyDataFileNameForTest(string monsterID)
    {
        if (monsterID.Contains("BLUESLIME"))
        {
            return monsterID.Contains("BOSS") ? "BlueSlime_BossData" : "BlueSlimeData";
        }
        else if (monsterID.Contains("GRAPE"))
        {
            return monsterID.Contains("BOSS") ? "Grape_BossData" : "GrapeData";
        }
        else if (monsterID.Contains("GHOST"))
        {
            return monsterID.Contains("BOSS") ? "Ghost_BossData" : "GhostData";
        }
        
        return $"{monsterID}Data";
    }
    
    #endregion
}
