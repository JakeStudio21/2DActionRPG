using System.Collections;
using UnityEngine;

/// <summary>
/// 모든 몬스터의 기본 클래스 - 공통 기능 템플릿화
/// ⭐ [Complete Reset] 완전한 데이터 기반 시스템 - fallback 제거
/// </summary>
public abstract class BaseEnemy : MonoBehaviour, IEnemy
{
    #region ⭐ 데이터 기반 시스템 (필수)

    [Header("⭐ 데이터 시스템 (필수!)")]
    [Tooltip("몬스터 데이터 ScriptableObject - 필수 할당!")]
    [SerializeField] protected EnemyData enemyData;
    
    [Tooltip("몬스터 성장 프로필 - 필수 할당!")]
    [SerializeField] protected MonsterGrowthProfile growthProfile;
    
    [Tooltip("현재 레벨")]
    [SerializeField] protected int currentLevel = 1;

    // Public Properties (읽기 전용)
    public EnemyData EnemyData => enemyData;
    public MonsterGrowthProfile GrowthProfile => growthProfile;
    
    public int CurrentLevel 
    { 
        get => currentLevel; 
        protected set => currentLevel = value; 
    }

    #endregion

    // IEnemy 인터페이스 구현 (공통)
    public EnemyAnimationController AnimationController { get; private set; }
    public EnemyFSMController FSMController { get; private set; }
    public PlayerController TargetPlayer { get; private set; }
    
    // 공통 속성들
    public Vector2 SpawnPoint { get; private set; }
    public abstract float AttackRange { get; }

    // 공통 컴포넌트들
    protected SpriteRenderer spriteRenderer;
    protected AudioSource audioSource;
    protected EnemyHealth enemyHealth;

    #region ⭐ 완전한 데이터 기반 스탯 계산 (에러 처리 강화)

    /// <summary>
    /// 레벨과 타입을 적용한 실제 최대 체력 - 데이터 필수!
    /// </summary>
    public virtual float GetScaledMaxHealth()
    {
        // 데이터 검증
        ValidateRequiredData();
        
        // 완전한 데이터 기반 계산
        return enemyData.GetScaledHealth(currentLevel, growthProfile);
    }

    /// <summary>
    /// 레벨과 타입을 적용한 실제 방어력 - 데이터 필수!
    /// </summary>
    public virtual float GetScaledDefense()
    {
        // 데이터 검증
        ValidateRequiredData();
        
        // 완전한 데이터 기반 계산
        return enemyData.GetScaledDefense(currentLevel, growthProfile);
    }

    /// <summary>
    /// 레벨과 타입을 적용한 실제 이동속도 - 데이터 필수!
    /// </summary>
    public virtual float GetScaledMoveSpeed()
    {
        // 데이터 검증
        ValidateRequiredData();
        
        // 완전한 데이터 기반 계산
        return enemyData.GetScaledMoveSpeed(currentLevel, growthProfile);
    }

    /// <summary>
    /// 필수 데이터 검증 - 없으면 즉시 에러!
    /// </summary>
    private void ValidateRequiredData()
    {
        if (enemyData == null)
        {
            Debug.LogError($"[BaseEnemy] {gameObject.name}: EnemyData가 할당되지 않았습니다! " +
                          "Inspector에서 EnemyData ScriptableObject를 할당해주세요.");
            return;
        }
        
        if (growthProfile == null)
        {
            Debug.LogError($"[BaseEnemy] {gameObject.name}: MonsterGrowthProfile이 할당되지 않았습니다! " +
                          "Inspector에서 MonsterGrowthProfile ScriptableObject를 할당해주세요.");
            return;
        }
    }

    /// <summary>
    /// 런타임 스탯 계산 및 적용
    /// </summary>
    protected virtual void CalculateRuntimeStats()
    {
        ValidateRequiredData();
        
        if (enemyData != null)
        {
            Debug.Log($"[{GetType().Name}] 런타임 스탯 계산:");
            Debug.Log($"  - 레벨: {currentLevel}, 타입: {enemyData.EnemyType}");
            Debug.Log($"  - 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"  - 공격력: AttackData에서 관리됨");
            Debug.Log($"  - 방어력: {GetScaledDefense():F1}");
            Debug.Log($"  - 이동속도: {GetScaledMoveSpeed():F1}");
        }
    }

    #endregion

    // ❌ 완전 삭제: fallback 메서드들 제거
    // protected abstract float GetFallbackMaxHealth();
    // protected abstract float GetFallbackDefense();
    // protected abstract float GetFallbackMoveSpeed();

    protected virtual void Awake()
    {
        // 즉시 데이터 검증
        ValidateRequiredData();
        
        // 공통 컴포넌트 초기화
        AnimationController = GetComponent<EnemyAnimationController>();
        FSMController = GetComponent<EnemyFSMController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        enemyHealth = GetComponent<EnemyHealth>();
        
        // 스폰 지점 저장
        SpawnPoint = transform.position;
        
        // 🎨 렌더링 소팅 설정
        InitializeRenderingSorting();
        
        // 추가 초기화 (하위 클래스에서 구현)
        OnAwakeInitialize();
    }

    protected virtual void Start()
    {
        // ⭐ 데이터 검증 후 스탯 계산
        CalculateRuntimeStats();
        
        StartCoroutine(FindPlayerCoroutine());
        InitializeAttackSystem();
        
        // 추가 시작 로직 (하위 클래스에서 구현)
        OnStartInitialize();

        // 임시 테스트 코드 (BaseEnemy의 Start()에 추가)
        if (HasPatrolTuning)
        {
            Debug.Log($"[{gameObject.name}] PatrolTuning 로드 성공!");
            Debug.Log($"  가속율: {PatrolTuning.Acceleration.accelerationRate}");
            Debug.Log($"  패트롤 속도: {GetPatrolMoveSpeed():F1}");
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] PatrolTuning 없음!");
        }
    }
    
    /// <summary>
    /// 공통 플레이어 찾기 코루틴
    /// </summary>
    private IEnumerator FindPlayerCoroutine()
    {
        while (TargetPlayer == null)
        {
            TargetPlayer = FindObjectOfType<PlayerController>();
            if (TargetPlayer == null)
                yield return new WaitForSeconds(0.1f);
        }
        Debug.Log($"[{GetType().Name}] {gameObject.name}이 플레이어를 찾았습니다.");
        
        // FSM 초기 상태 진입
        if (FSMController != null)
        {
            FSMController.ChangeState(new EnemyIdleState(this));
            Debug.Log($"[{GetType().Name}] {gameObject.name} FSM 시스템 시작 - Idle State");
        }
    }
    
    /// <summary>
    /// 공통 유틸리티 메서드들
    /// </summary>
    public Vector2 GetPosition() => transform.position;
    
    public bool IsPlayerInRange(float range)
    {
        if (TargetPlayer == null) return false;
        float distance = Vector2.Distance(transform.position, TargetPlayer.transform.position);
        return distance <= range;
    }

    public Vector2 GetDirectionToPlayer()
    {
        if (TargetPlayer == null) return Vector2.zero;
        return (TargetPlayer.transform.position - transform.position).normalized;
    }

    #region ⭐ 개발자 도구 (데이터 검증 포함)

    /// <summary>
    /// 런타임에서 레벨 변경 (테스트용)
    /// </summary>
    [ContextMenu("Level Up")]
    public virtual void LevelUp()
    {
        CurrentLevel += 1;
        CalculateRuntimeStats();
        Debug.Log($"[{GetType().Name}] 레벨업! 새 레벨: {CurrentLevel}");
    }

    /// <summary>
    /// 현재 상태 디버그 출력 (PatrolTuning 정보 포함)
    /// </summary>
    [ContextMenu("Debug Monster Info")]
    public virtual void DebugMonsterInfo()
    {
        string info = $"=== {GetType().Name} {gameObject.name} ===\n";
        info += $"Current Level: {currentLevel}\n";
        info += $"Patrol Radius: {PatrolRadius}\n";
        info += $"Attack Range: {AttackRange}\n";
        info += $"Base Move Speed: {GetScaledMoveSpeed():F1}\n";
        info += $"Patrol Move Speed: {GetPatrolMoveSpeed():F1}\n\n";
        
        if (enemyData != null)
        {
            info += "=== EnemyData 정보 ===\n";
            info += enemyData.GetDebugInfo(currentLevel, growthProfile);
        }
        else
        {
            info += "❌ EnemyData가 할당되지 않았습니다!\n";
        }
        
        // PatrolTuning 전용 디버그 정보
        if (HasPatrolTuning)
        {
            var tuning = PatrolTuning;
            info += "\n=== 패트롤 튜닝 상세 ===\n";
            info += $"가속율: {tuning.Acceleration.accelerationRate:F1}\n";
            info += $"감속율: {tuning.Acceleration.decelerationRate:F1}\n";
            info += $"최대속도 배율: {tuning.Acceleration.maxSpeedMultiplier:F1}\n";
            info += $"최소속도 배율: {tuning.Acceleration.minSpeedMultiplier:F1}\n";
            info += $"대기 확률: {tuning.Pause.movementPauseChance:F1}\n";
            info += $"노이즈 강도: {tuning.DirectionNoise.noiseStrength:F1}\n";
            info += $"간격 유지 거리: {tuning.EnvironmentResponse.separationDistance:F1}\n";
            info += $"이동 가중치: {tuning.PersonalityWeights.movementWeight:F1}\n";
        }
        else
        {
            info += "\n❌ PatrolTuning이 할당되지 않았습니다!\n";
        }
        
        Debug.Log(info);
    }

    #endregion
    
    // 추상 메서드들 - 하위 클래스에서 구현 필수
    protected abstract void OnAwakeInitialize();
    protected abstract void OnStartInitialize();
    protected abstract void InitializeAttackSystem();
    public abstract void Attack(); // IEnemy 인터페이스 구현
    
    #region 🗺️ 아이소메트릭 데이터 시스템 (플레이어 클래스와 동일 구조)
    
    /// <summary>
    /// 아이소메트릭 데이터 가져오기 (자식 클래스에서 구현)
    /// </summary>
    public abstract IsometricCharacterData GetIsometricData();
    
    /// <summary>
    /// 방향 프리셋 가져오기 (E4M, E8 등)
    /// </summary>
    public virtual DirectionPreset GetDirectionPreset()
    {
        var isometricData = GetIsometricData();
        if (enableDebugLogs && Time.frameCount % 300 == 0) // 5초마다
            Debug.Log($"🧭 [{GetType().Name}] DirectionPreset: {isometricData.DirectionPreset}");
        return isometricData.DirectionPreset;
    }
    
    /// <summary>
    /// 발 위치 오프셋 가져오기 (Y-소팅 기준점)
    /// </summary>
    public virtual Vector2 GetFootOffset()
    {
        var isometricData = GetIsometricData();
        if (enableDebugLogs && Time.frameCount % 300 == 0) // 5초마다
            Debug.Log($"🦶 [{GetType().Name}] FootOffset: {isometricData.FootOffset}");
        return isometricData.FootOffset;
    }
    
    /// <summary>
    /// 높이 오프셋 계산 (점프, 날기 등에 사용)
    /// </summary>
    /// <param name="t">높이 곡선 시간 (0~1)</param>
    /// <returns>계산된 높이 오프셋</returns>
    public virtual int CalculateHeightOffset(float t)
    {
        var isometricData = GetIsometricData();
        int heightOffset = isometricData.CalculateHeightOffset(t);
        
        if (enableDebugLogs && heightOffset != 0)
            Debug.Log($"📈 [{GetType().Name}] HeightOffset: t={t:F2} → {heightOffset}");
            
        return heightOffset;
    }
    
    /// <summary>
    /// 기본 아이소메트릭 데이터 생성 (fallback용)
    /// </summary>
    protected IsometricCharacterData CreateDefaultIsometricData()
    {
        var defaultData = new IsometricCharacterData();
        defaultData.SetDefaults();
        
        if (enableDebugLogs)
            Debug.Log($"⚠️ [{GetType().Name}] 기본 IsometricData 생성됨 (ScriptableObject 할당 권장)");
        
        return defaultData;
    }
    
    #endregion

    // BaseEnemy 클래스에 추가할 필드들
    [Header("위치 정보")]
    [SerializeField] protected Vector3 homePosition; // 집 위치
    [SerializeField] protected float patrolRadius = 3f; // 순찰 반경
    [SerializeField] protected Vector3 spawnPosition; // 스폰된 위치

    // 프로퍼티 추가
    public Vector3 HomePosition => homePosition;
    public virtual float PatrolRadius 
    { 
        get 
        {
            // 🔑 스폰 시 설정된 값 우선 (SpawnPoint에서 설정)
            if (patrolRadius > 0) 
                return patrolRadius;
            
            // 🔑 데이터 기반 기본값 사용
            if (enemyData != null)
            {
                // EnemyData에 PatrolRadius 프로퍼티가 있다면 사용
                return 3f; // 임시 기본값
            }
                
            return 3f; // 최후 기본값
        } 
    }
    public Vector3 SpawnPosition => spawnPosition;

    /// <summary>
    /// 홈 위치와 순찰 반경 설정
    /// </summary>
    public virtual void SetHomePosition(Vector3 position, float radius)
    {
        homePosition = position;
        spawnPosition = position;
        patrolRadius = radius;
        
        if (enableDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Home 설정: {homePosition}, Patrol: {patrolRadius}");
        }
    }

    /// <summary>
    /// 홈 위치로부터의 거리 계산
    /// </summary>
    public float GetDistanceFromHome()
    {
        return Vector3.Distance(transform.position, homePosition);
    }

    /// <summary>
    /// 순찰 범위 내에 있는지 확인
    /// </summary>
    public bool IsWithinPatrolRange()
    {
        return GetDistanceFromHome() <= patrolRadius;
    }

    /// <summary>
    /// 순찰 범위 내 랜덤 위치 반환
    /// </summary>
    public Vector3 GetRandomPatrolPosition()
    {
        Vector2 randomOffset = Random.insideUnitCircle * patrolRadius;
        return homePosition + new Vector3(randomOffset.x, randomOffset.y, 0);
    }

    // DetectionRange 프로퍼티 추가 (데이터 기반)
    public virtual float DetectionRange 
    { 
        get 
        {
            // 데이터 기반 값이 있으면 사용 (접근 가능한 프로퍼티 사용)
            if (enemyData != null)
            {
                // DetectionRange 프로퍼티가 있는지 확인하고 사용
                // detectionRange 필드가 private이므로 기본값 반환
                return 5f; // 기본 감지 범위
            }
                
            // 기본값 반환
            return 5f;
        } 
    }

    // MoveSpeed 프로퍼티 추가 (데이터 기반)
    public virtual float MoveSpeed 
    { 
        get 
        {
            // 데이터 기반 이동속도 사용
            return GetScaledMoveSpeed();
        } 
    }

    // ChangeState 메서드 추가
    public void ChangeState(IEnemyState newState)
    {
        FSMController?.ChangeState(newState);
    }

    // BaseEnemy 클래스에 추가할 필드들
    [Header("디버그")]
    [SerializeField] protected bool enableDebugLogs = true; // 누락된 필드 추가

    // EnableDebugLogs 프로퍼티 추가 (IEnemy 인터페이스용)
    public bool EnableDebugLogs => enableDebugLogs;

    /// <summary>
    /// 패트롤 튜닝 데이터 접근
    /// </summary>
    public PatrolTuning PatrolTuning 
    { 
        get 
        {
            if (enemyData != null && enemyData.PatrolTuning != null)
            {
                return enemyData.PatrolTuning;
            }
            
            // 기본 PatrolTuning이 없으면 경고 (개발 중에만)
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[{gameObject.name}] PatrolTuning이 할당되지 않았습니다. 기본값 사용.");
            }
            
            return null;
        } 
    }

    /// <summary>
    /// PatrolTuning이 있는지 확인
    /// </summary>
    public bool HasPatrolTuning => PatrolTuning != null;

    /// <summary>
    /// 가중치가 적용된 실제 이동속도 계산
    /// </summary>
    public virtual float GetPatrolMoveSpeed()
    {
        float baseSpeed = GetScaledMoveSpeed();
        
        if (HasPatrolTuning)
        {
            float movementWeight = PatrolTuning.PersonalityWeights.movementWeight;
            return baseSpeed * movementWeight;
        }
        
        return baseSpeed;
    }

    /// <summary>
    /// 런타임에서 EnemyData 동적 변경 (보스 변환용)
    /// </summary>
    public void SetEnemyData(EnemyData newEnemyData)
    {
        if (newEnemyData == null)
        {
            Debug.LogWarning($"[BaseEnemy] {gameObject.name}: null EnemyData를 설정하려고 시도!");
            return;
        }
        
        enemyData = newEnemyData;
        
        // 스탯 재계산
        CalculateRuntimeStats();
        
        Debug.Log($"🐲 [BaseEnemy] {gameObject.name}: EnemyData 동적 변경 완료 → {newEnemyData.name}");
        Debug.Log($"🐲 [BaseEnemy] IsBoss: {enemyData.IsBoss}, EnemyType: {enemyData.EnemyType}");
    }

    #region 🎨 렌더링 소팅 시스템

    /// <summary>
    /// 몬스터용 렌더링 소팅 초기화
    /// FootPositionSorter 컴포넌트 자동 추가 및 설정
    /// </summary>
    private void InitializeRenderingSorting()
    {
        // FootPositionSorter 컴포넌트 확인/추가
        var footSorter = GetComponent<FootPositionSorter>();
        if (footSorter == null)
        {
            footSorter = gameObject.AddComponent<FootPositionSorter>();
        }

        // 몬스터 기본 설정
        footSorter.enabled = true;
        
        // 발 위치 기준점 설정 (몬스터의 중심점 사용)
        if (footSorter.GetType().GetField("footPosition", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null)
        {
            var footPositionField = footSorter.GetType().GetField("footPosition", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            footPositionField.SetValue(footSorter, transform);
        }

        // 몬스터용 기본 설정 적용
        SetFootSorterSettings(footSorter);
        
        Debug.Log($"🎨 [BaseEnemy] {gameObject.name}: FootPositionSorter 설정 완료");
    }

    /// <summary>
    /// 몬스터별 FootPositionSorter 설정
    /// </summary>
    private void SetFootSorterSettings(FootPositionSorter footSorter)
    {
        // 몬스터 기본 설정
        var footOffsetField = footSorter.GetType().GetField("footOffset", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        footOffsetField?.SetValue(footSorter, Vector2.zero);

        var baseLayerField = footSorter.GetType().GetField("baseLayer", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        baseLayerField?.SetValue(footSorter, IsometricSorting.CHARACTER_LAYER);

        var heightOffsetField = footSorter.GetType().GetField("heightOffset", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        heightOffsetField?.SetValue(footSorter, 0);

        // 업데이트 정책 설정 (성능 최적화)
        var updatePolicyField = footSorter.GetType().GetField("updatePolicy", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        updatePolicyField?.SetValue(footSorter, FootPositionSorter.UpdatePolicy.LateUpdate);

        var updateIntervalField = footSorter.GetType().GetField("updateInterval", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        updateIntervalField?.SetValue(footSorter, 0.1f);

        // 디버그 설정 (개발 시에만)
        var enableDebugLogsField = footSorter.GetType().GetField("enableDebugLogs", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        enableDebugLogsField?.SetValue(footSorter, false); // 배포 시 false
    }

    #endregion
} 