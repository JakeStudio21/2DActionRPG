using UnityEngine;

[CreateAssetMenu(fileName = "PatrolTuning_", menuName = "Enemy System/Patrol Tuning", order = 2)]
public class PatrolTuning : ScriptableObject
{
    [Header("=== 가감속 시스템 ===")]
    [SerializeField] private AccelerationSettings acceleration;
    
    [Header("=== 대기 시스템 ===")]
    [SerializeField] private PauseSettings pause;
    
    [Header("=== 방향 노이즈 시스템 ===")]
    [SerializeField] private DirectionNoiseSettings directionNoise;
    
    [Header("=== 환경 반응 시스템 ===")]
    [SerializeField] private EnvironmentResponseSettings environmentResponse;
    
    [Header("=== 개성 가중치 시스템 ===")]
    [SerializeField] private PersonalityWeights personalityWeights;

    // 프로퍼티
    public AccelerationSettings Acceleration => acceleration;
    public PauseSettings Pause => pause;
    public DirectionNoiseSettings DirectionNoise => directionNoise;
    public EnvironmentResponseSettings EnvironmentResponse => environmentResponse;
    public PersonalityWeights PersonalityWeights => personalityWeights;

    private void OnValidate()
    {
        // 기본값 설정
        if (acceleration == null) acceleration = new AccelerationSettings();
        if (pause == null) pause = new PauseSettings();
        if (directionNoise == null) directionNoise = new DirectionNoiseSettings();
        if (environmentResponse == null) environmentResponse = new EnvironmentResponseSettings();
        if (personalityWeights == null) personalityWeights = new PersonalityWeights();
    }
}

[System.Serializable]
public class AccelerationSettings
{
    [Header("가속도 기반 속도 제어")]
    [Range(1.0f, 3.0f)]
    public float accelerationRate = 2.0f;
    
    [Range(2.0f, 5.0f)]
    public float decelerationRate = 3.0f;
    
    [Range(0.8f, 1.5f)]
    public float maxSpeedMultiplier = 1.2f;
    
    [Range(0.3f, 0.8f)]
    public float minSpeedMultiplier = 0.5f;
    
    [Range(0.1f, 1.0f)]
    public float speedTransitionSmoothing = 0.3f;
}

[System.Serializable]
public class PauseSettings
{
    [Header("대기 거리 범위")]
    [Range(1f, 5f)]
    public float pauseDistanceMin = 2f;
    [Range(2f, 8f)]
    public float pauseDistanceMax = 4f;
    
    [Header("대기 시간 범위")]
    [Range(0.5f, 2f)]
    public float pauseDurationMin = 1f;
    [Range(1f, 5f)]
    public float pauseDurationMax = 3f;
    
    [Header("웨이포인트 대기 범위")]
    [Range(0.5f, 2f)]
    public float waypointPauseMin = 1f;
    [Range(1f, 4f)]
    public float waypointPauseMax = 2f;
    
    [Header("이동 중 멈춤 가능성")]
    [Range(0f, 1f)]
    public float movementPauseChance = 0.2f;
}

[System.Serializable]
public class DirectionNoiseSettings
{
    [Header("노이즈 강도")]
    [Range(0f, 1f)]
    public float noiseStrength = 0.5f;
    
    [Range(0.5f, 3.0f)]
    public float noiseFrequency = 1.5f;
    
    [Header("방향 전환 부드러움")]
    [Range(0.1f, 0.5f)]
    public float directionSmoothTime = 0.2f;
    
    [Range(5f, 45f)]
    public float maxDeviationAngle = 20f;
    
    [Range(0.1f, 0.5f)]
    public float noiseUpdateInterval = 0.2f;
}

[System.Serializable]
public class EnvironmentResponseSettings
{
    [Header("간격 유지 (Separation)")]
    [Range(1f, 3f)]
    public float separationDistance = 2f;
    [Range(0.5f, 2.0f)]
    public float separationStrength = 1.0f;
    
    [Header("장애물 회피")]
    [Range(0.5f, 2.0f)]
    public float obstacleAvoidDistance = 1.5f;
    [Range(1f, 3f)]
    public float obstacleAvoidStrength = 2f;
    
    [Header("급턴 감속")]
    [Range(30f, 90f)]
    public float turnSlowdownAngle = 60f;
    [Range(0.3f, 0.8f)]
    public float turnSlowdownRate = 0.6f;
}

[System.Serializable]
public class PersonalityWeights
{
    [Header("개성 가중치 (1.0 = 기본값)")]
    [Range(0.5f, 2.0f)]
    public float movementWeight = 1.0f;
    
    [Range(0.3f, 2.5f)]
    public float pauseWeight = 1.0f;
    
    [Range(0.2f, 2.0f)]
    public float noiseWeight = 1.0f;
    
    [Range(0.5f, 2.0f)]
    public float alertnessWeight = 1.0f;
    
    [Range(0.3f, 2.0f)]
    public float socialWeight = 1.0f;
}
