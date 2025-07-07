using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 몬스터 및 보스 스폰을 관리하는 매니저
/// </summary>
public class SpawnManager : Singleton<SpawnManager>
{
    [Header("스폰 설정")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private GameObject[] bossPrefabs;
    [SerializeField] private Transform[] spawnPoints;
    
    [Header("스폰 제어")]
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxEnemiesOnScreen = 10;
    [SerializeField] private bool autoSpawn = true;
    
    private List<GameObject> activeEnemies = new List<GameObject>();
    private float lastSpawnTime;
    
    protected override void Awake()
    {
        base.Awake();
        InitializeSpawnManager();
    }
    
    private void Update()
    {
        if (autoSpawn && GameManager.Instance.currentGameState == GameManager.GameState.InGame)
        {
            HandleAutoSpawn();
        }
    }
    
    /// <summary>
    /// 스폰 매니저 초기화
    /// </summary>
    private void InitializeSpawnManager()
    {
        lastSpawnTime = Time.time;
        
        // 스폰 포인트가 없으면 자동으로 찾기
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            FindSpawnPoints();
        }
    }
    
    /// <summary>
    /// 스폰 포인트 자동 찾기
    /// </summary>
    private void FindSpawnPoints()
    {
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
        spawnPoints = new Transform[spawnPointObjects.Length];
        
        for (int i = 0; i < spawnPointObjects.Length; i++)
        {
            spawnPoints[i] = spawnPointObjects[i].transform;
        }
        
        Debug.Log($"[SpawnManager] {spawnPoints.Length}개의 스폰 포인트를 찾았습니다.");
    }
    
    /// <summary>
    /// 자동 스폰 처리
    /// </summary>
    private void HandleAutoSpawn()
    {
        if (Time.time - lastSpawnTime >= spawnInterval && activeEnemies.Count < maxEnemiesOnScreen)
        {
            SpawnRandomEnemy();
            lastSpawnTime = Time.time;
        }
    }
    
    /// <summary>
    /// 랜덤 적 스폰
    /// </summary>
    public void SpawnRandomEnemy()
    {
        if (enemyPrefabs.Length == 0 || spawnPoints.Length == 0) return;
        
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        
        GameObject enemy = GamePoolManager.Instance.SpawnFromPool("Enemy", spawnPoint.position, spawnPoint.rotation);
        activeEnemies.Add(enemy);
        
        Debug.Log($"[SpawnManager] 적 스폰: {enemyPrefab.name} at {spawnPoint.position}");
    }
    
    /// <summary>
    /// 보스 스폰
    /// </summary>
    public void SpawnBoss(int bossIndex = 0)
    {
        if (bossPrefabs.Length == 0 || bossIndex >= bossPrefabs.Length) return;
        
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject boss = Instantiate(bossPrefabs[bossIndex], spawnPoint.position, spawnPoint.rotation);
        activeEnemies.Add(boss);
        
        Debug.Log($"[SpawnManager] 보스 스폰: {bossPrefabs[bossIndex].name}");
    }
    
    /// <summary>
    /// 특정 위치에 적 스폰
    /// </summary>
    public GameObject SpawnEnemyAtPosition(GameObject enemyPrefab, Vector3 position)
    {
        GameObject enemy = GamePoolManager.Instance.SpawnFromPool("Enemy", position, Quaternion.identity);
        activeEnemies.Add(enemy);
        return enemy;
    }
    
    /// <summary>
    /// 적 제거 (사망 시)
    /// </summary>
    public void RemoveEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }
    
    /// <summary>
    /// 모든 적 제거
    /// </summary>
    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        activeEnemies.Clear();
    }
    
    /// <summary>
    /// 스폰 설정 변경
    /// </summary>
    public void SetSpawnSettings(float interval, int maxEnemies, bool auto)
    {
        spawnInterval = interval;
        maxEnemiesOnScreen = maxEnemies;
        autoSpawn = auto;
    }
}

