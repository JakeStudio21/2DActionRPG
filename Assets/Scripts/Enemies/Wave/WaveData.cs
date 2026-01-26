using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// SimpleMob 웨이브 데이터 (ScriptableObject)
/// </summary>
[CreateAssetMenu(fileName = "WaveData", menuName = "2DActionRPG/Wave System/Wave Data", order = 1)]
public class WaveData : ScriptableObject
{
    [Header("웨이브 정보")]
    public int waveNumber = 1;
    public int totalWaves = 1;
    
    [Header("스폰 설정")]
    public List<SpawnConfig> spawnConfigs = new List<SpawnConfig>();
    
    [Header("클리어 조건")]
    public WaveClearCondition clearCondition = WaveClearCondition.KillAll;
    public float timeLimitSeconds = 60f; // TimeLimit용
    
    [Header("보상")]
    public int rewardGold = 50;
    public int rewardExp = 20;
}

/// <summary>
/// 스폰 설정
/// </summary>
[System.Serializable]
public class SpawnConfig
{
    [Header("몬스터 설정")]
    [Tooltip("스폰할 SimpleMob 프리팹")]
    public GameObject mobPrefab;
    
    [Tooltip("스폰 개수")]
    public int spawnCount = 10;
    
    [Tooltip("스폰 간격 (초)")]
    public float spawnInterval = 0.2f;
    
    [Tooltip("이동 속도 (0이면 프리팹 기본값 사용)")]
    public float moveSpeed = 0f;
    
    [Header("스폰 패턴")]
    [Tooltip("스폰 패턴")]
    public SpawnPattern spawnPattern = SpawnPattern.Circle;
    
    [Tooltip("스폰 반경 (Circle, Random 패턴용)")]
    public float spawnRadius = 10f;
    
    [Tooltip("Line 패턴 시작/끝 오프셋")]
    public Vector2 lineStart = new Vector2(-5f, 0f);
    public Vector2 lineEnd = new Vector2(5f, 0f);
    
    [Tooltip("Grid 패턴 행/열 개수")]
    public int gridRows = 3;
    public int gridColumns = 3;
    public float gridSpacing = 2f;
}

/// <summary>
/// 스폰 패턴
/// </summary>
public enum SpawnPattern
{
    Circle,     // 원형 배치
    Random,     // 랜덤 위치
    Line,       // 직선 배치
    Grid        // 그리드 배치
}

/// <summary>
/// 웨이브 클리어 조건
/// </summary>
public enum WaveClearCondition
{
    KillAll,        // 모두 처치
    TimeLimit,      // 시간 제한 생존
    KillCount       // 특정 수 처치
}

