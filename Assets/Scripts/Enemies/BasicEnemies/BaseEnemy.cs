using System.Collections;
using UnityEngine;
using UnityEngine.AI; // NavMeshAgent (Phase 2)

/// <summary>
/// 모든 몬스터의 기본 클래스 - 공통 기능 템플릿화
/// ⭐ [Complete Reset] 완전한 데이터 기반 시스템 - fallback 제거
/// ⚙️ [Phase 4] IEnemyTarget 구현 추가 - 조건부 모디파이어용
/// </summary>
public abstract class BaseEnemy : MonoBehaviour, IEnemy, IEnemyTarget, ITargetable
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

    #region 🎵 CueSystem 전용 프로필

    [Header("🎵 Cue 프로필 (선택)")]
    [Tooltip("전용 이펙트가 필요할 때 할당. 비워두면 공통 Enemy 프로필 사용.")]
    [SerializeField] private CueSystem.CueProfile cueProfile;

    /// <summary>
    /// 이 몬스터의 CueEmitter 도메인 키.
    /// cueProfile이 할당되면 "Enemy_{enemyId}", 없으면 "Enemy" (공통).
    /// </summary>
    public string CueEmitDomain { get; private set; } = "Enemy";

    /// <summary>
    /// CueProfile 등록 — Awake 이후 EnemyData가 준비된 시점에 호출
    /// </summary>
    protected void InitializeCueProfile()
    {
        if (cueProfile == null) return;

        string id = enemyData != null && !string.IsNullOrEmpty(enemyData.EnemyId)
            ? enemyData.EnemyId
            : gameObject.name;

        CueEmitDomain = $"Enemy_{id}";
        CueSystem.CueRegistry.Instance.RegisterProfile(CueEmitDomain, cueProfile);

    }

    #endregion

    // IEnemy 인터페이스 구현 (공통)
    public EnemyAnimationController AnimationController { get; private set; }
    public EnemyFSMController FSMController { get; private set; }
    public PlayerController TargetPlayer { get; private set; }
    
    // 공통 속성들
    public Vector2 SpawnPoint { get; private set; }
    public abstract float AttackRange { get; }
    
    #region 🗺️ NavMeshAgent 시스템 (Phase 2)
    
    [Header("NavMesh 설정")]
    [Tooltip("NavMesh 사용 여부 토글")]
    [SerializeField] protected bool useNavMesh = false;
    [Tooltip("NavMeshAgent 가속도 (0 = 자동: speed×20). 기본값 0 권장 — 특수 움직임(느린 기동감)이 필요한 몬스터만 수동 설정")]
    [SerializeField] protected float navMeshAcceleration = 0f;
    
    /// <summary>
    /// NavMeshAgent 컴포넌트 (Phase 2)
    /// </summary>
    public UnityEngine.AI.NavMeshAgent Agent { get; private set; }
    
    /// <summary>
    /// NavMesh 사용 여부
    /// ⚠️ Agent.enabled 체크 제거 (순환 논리 방지)
    /// </summary>
    public bool IsUsingNavMesh => useNavMesh && Agent != null;

    /// <summary>
    /// 다음 스태거 시 적용할 넉백 배율 (0 = 넉백 없음, 1 = 기본)
    /// EnemyHealth.TakeDamage → PoiseHandler → 이 값을 설정 → PerformKnockbackEffect에서 소비
    /// </summary>
    public float PendingKnockbackScale { get; set; } = 1f;

    /// <summary>
    /// FSM이 AttackState에 진입하는 기준 거리 (범용 유틸)
    /// 근접 공격 범위 · 엘리트 스킬 범위 · 보스 원거리 스킬 범위 중 최댓값을 반환한다.
    ///
    /// 사용처: Elite/Boss 클래스의 AttackRange override
    ///   public override float AttackRange => GetFSMAttackRange();
    ///
    /// 주의: GenericMeleeEnemy / GenericRangedEnemy 등 스킬이 없는 클래스는
    ///       각자의 AttackRange 구현을 유지하고 이 메서드를 호출하지 않는다.
    /// </summary>
    protected float GetFSMAttackRange()
    {
        float meleeRange = 0f;
        float skillRange = 0f;

        // 평타 공격 범위
        var melee = GetComponent<MeleeAttack>();
        if (melee?.AttackData != null)
            meleeRange = melee.AttackData.AttackRange;

        // 엘리트 스킬 최대 사거리 (SkillData.MaxRange 중 최대)
        var eliteSkill = GetComponent<EliteSkillController>();
        if (eliteSkill != null)
            skillRange = Mathf.Max(skillRange, eliteSkill.GetMaxSkillRange());

        // 보스 원거리 스킬 사거리 (BossAttackBehaviour.RangedSkillRange)
        var bossAttack = GetComponent<BossAttackBehaviour>();
        if (bossAttack != null)
            skillRange = Mathf.Max(skillRange, bossAttack.RangedSkillRange);

        return Mathf.Max(meleeRange, skillRange);
    }

    #endregion
    
    /// <summary>
    /// 스킬 시전 중 여부 (엘리트/보스 전용)
    /// Cast 중이거나 Action 실행 중이면 true
    /// FSM이 공격 중 이동/상태 전환을 막기 위해 사용
    /// </summary>
    public bool IsPerformingSkill
    {
        get
        {
            // 엘리트 스킬 체크
            var eliteSkill = GetComponent<EliteSkillController>();
            if (eliteSkill != null)
                return eliteSkill.IsCasting || eliteSkill.IsActionExecuting || eliteSkill.IsAsyncSkillPending;
            
            // 보스 스킬 체크
            var bossSkill = GetComponent<BossSkillController>();
            if (bossSkill != null)
                return bossSkill.IsCasting || bossSkill.IsActionExecuting;
            
            return false;
        }
    }

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
        }
        
        // ⭐ Phase 2-추가: 공격 데미지 캐시 무효화 (레벨 변경 시 필수!)
        InvalidateAllAttackCaches();
    }
    
    /// <summary>
    /// 모든 공격 시스템의 캐시 무효화 (레벨 변경 시 호출)
    /// </summary>
    private void InvalidateAllAttackCaches()
    {
        // 모든 BaseAttackBehaviour 컴포넌트 찾기
        var attackBehaviours = GetComponents<BaseAttackBehaviour>();
        
        if (attackBehaviours != null && attackBehaviours.Length > 0)
        {
            foreach (var attackBehaviour in attackBehaviours)
            {
                attackBehaviour.InvalidateAttackCache();
            }
            
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
        
        // ⭐⭐⭐ 추가 초기화 먼저 실행 (하위 클래스에서 MeleeAttack 등 초기화)
        OnAwakeInitialize();
        
        // ⭐⭐⭐ NavMeshAgent 초기화 (Phase 2) - OnAwakeInitialize 이후 실행!
        Agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        
        // NavMesh 시스템 상태 확인 (디버그 로그 제거)
        
        if (useNavMesh && Agent != null)
        {
            // ⭐ NavMeshAgent 비활성화 (위치 설정 전까지)
            Agent.enabled = false;
            
            InitializeNavMeshAgent();
        }
        else
        {
            if (!useNavMesh)
                Debug.LogWarning($"⚠️ [BaseEnemy] {gameObject.name}: useNavMesh = false! NavMesh 비활성화 상태!");
            if (Agent == null)
                Debug.LogWarning($"⚠️ [BaseEnemy] {gameObject.name}: NavMeshAgent 컴포넌트가 없습니다!");
        }
        
        // 스폰 지점 저장
        SpawnPoint = transform.position;
        
        // 🎨 렌더링 소팅 설정
        InitializeRenderingSorting();
    }

    protected virtual void Start()
    {
        // ⭐ 데이터 검증 후 스탯 계산
        CalculateRuntimeStats();
        
        // ⭐⭐⭐ HomePosition 초기화 (NavMesh 필수!)
        if (homePosition == Vector3.zero)
        {
            SetHomePosition(transform.position, PatrolRadius);
        }
        
        // ⭐⭐⭐ NavMeshAgent 활성화 (위치 설정 완료 후!)
        if (IsUsingNavMesh)
        {
            // Z축을 0으로 보정
            Vector3 correctedPosition = new Vector3(transform.position.x, transform.position.y, 0f);
            transform.position = correctedPosition;
            
            // NavMeshAgent 활성화
            Agent.enabled = true;
            
            // Warp로 현재 위치를 NavMesh 위로 인식시킴 (자동 이동 방지)
            Agent.Warp(correctedPosition);
            
            // ⭐ 활성화 후 속도/가속도 재적용 (비활성화 상태에서 설정한 값이 Inspector 기본값으로 덮어써지기 때문)
            float scaledSpeed = GetScaledMoveSpeed();
            Agent.speed = scaledSpeed;
            Agent.acceleration = navMeshAcceleration > 0f ? navMeshAcceleration : scaledSpeed * 20f;
        }
        
        StartCoroutine(FindPlayerCoroutine());
        
        // 🎵 CueProfile 등록 (InitializeAttackSystem 보다 먼저 실행해야 cueEmitDomain이 올바르게 동기화됨)
        InitializeCueProfile();
        
        InitializeAttackSystem();
        
        // 추가 시작 로직 (하위 클래스에서 구현)
        OnStartInitialize();
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
        
        // FSM 초기 상태 진입
        if (FSMController != null)
        {
            FSMController.ChangeState(new EnemyIdleState(this));
            Dbg.Log($"[{GetType().Name}] {gameObject.name} FSM 시스템 시작 - Idle State");
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
        
    }
    
    /// <summary>
    /// ⭐ Phase 2: 런타임에 스테이지 기반 레벨 동적 설정
    /// 스폰 직후 StageManager에서 호출되어야 함!
    /// </summary>
    /// <param name="stageBaseLevel">스테이지 기준 레벨</param>
    /// <param name="levelOffset">레벨 오프셋 (특수 몬스터 강화용)</param>
    public virtual void InitializeLevel(int stageBaseLevel, int levelOffset = 0)
    {
        // 1. 최종 레벨 계산
        currentLevel = stageBaseLevel + levelOffset;
        
        
        // 2. 스탯 재계산 (SO 기반)
        CalculateRuntimeStats();
        
        // 3. 체력 리셋 (새 최대 체력으로 가득 채움)
        var enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.ResetHealthToMax();
        }
        else
        {
            Debug.LogWarning($"⚠️ [BaseEnemy] {gameObject.name}: EnemyHealth 컴포넌트가 없습니다!");
        }
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
        return isometricData.DirectionPreset;
    }
    
    /// <summary>
    /// 발 위치 오프셋 가져오기 (Y-소팅 기준점)
    /// </summary>
    public virtual Vector2 GetFootOffset()
    {
        var isometricData = GetIsometricData();
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
        
            
        return heightOffset;
    }
    
    /// <summary>
    /// 기본 아이소메트릭 데이터 생성 (fallback용)
    /// </summary>
    protected IsometricCharacterData CreateDefaultIsometricData()
    {
        var defaultData = new IsometricCharacterData();
        defaultData.SetDefaults();
        
        
        return defaultData;
    }
    
    #endregion
    
    #region 🗺️ NavMeshAgent 초기화 및 관리 (Phase 2)
    
    /// <summary>
    /// NavMeshAgent 초기화 - 2D 아이소메트릭 최적화 설정
    /// </summary>
    protected virtual void InitializeNavMeshAgent()
    {
        if (Agent == null) return;
        
        // ⭐⭐⭐ NavMesh 사용 시 Rigidbody2D를 Kinematic으로 전환 (필수!)
        // NavMeshAgent는 Transform을 직접 제어하므로 물리 시뮬레이션과 충돌 방지
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (rb.bodyType != RigidbodyType2D.Kinematic)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ [BaseEnemy] {gameObject.name}: Rigidbody2D 컴포넌트가 없습니다!");
        }
        
        // ⭐ 2D 필수 설정 (Phase 2-1)
        Agent.updateRotation = false;  // 2D 스프라이트 회전 방지
        Agent.updateUpAxis = false;    // Z축 기울임 방지
        
        // ⭐ 속도 동기화 (데이터 기반)
        Agent.speed = GetScaledMoveSpeed();
        
        // ⭐ 가속도 설정: navMeshAcceleration이 0이면 speed 비례 자동 계산
        // speed / acceleration = 최고속도 도달 시간 (0 = speed*20으로 자동 설정 → 0.05초)
        Agent.acceleration = navMeshAcceleration > 0f ? navMeshAcceleration : Agent.speed * 20f;
        
        // ⭐⭐⭐ 정지 거리 설정 (공격 범위보다 약간 작게)
        // 안전장치: AttackRange 접근 시 에러 발생하면 기본값 사용
        float attackRange = 2.0f; // 기본값
        try
        {
            attackRange = AttackRange;
        }
        catch (System.Exception)
        {
            // AttackRange 접근 불가 시 기본값 사용 (나중에 Start()에서 재설정됨)
        }
        Agent.stoppingDistance = Mathf.Max(0.1f, attackRange - 0.5f);
        
        // ⭐ Agent Radius 설정 (발바닥 크기 기준 70~80%)
        // 기본값으로 0.3f 사용, 필요시 Inspector에서 조정
        Agent.radius = 0.3f;
        
        // 높이 설정 (2D에서는 크게 중요하지 않음)
        Agent.height = 1.0f;
        
        // ⭐ 자동 브레이킹 비활성화 (Chase 중 감속 방지)
        Agent.autoBraking = false; // 추격 중에는 일정한 속도 유지
        
        // ⭐ 회전 속도 0 (회전은 코드로 제어)
        Agent.angularSpeed = 0f;
        
        // ⭐ 장애물 회피 설정 (몬스터끼리 충돌 회피)
        Agent.obstacleAvoidanceType = UnityEngine.AI.ObstacleAvoidanceType.LowQualityObstacleAvoidance; // 가볍게 회피
        Agent.avoidancePriority = 50; // 우선순위 (0=최고, 99=최저)
        
        // NavMeshAgent 초기화 완료 (로그 제거)
    }
    
    /// <summary>
    /// ⭐⭐⭐ Z축 강제 고정 (Phase 2 - 가장 중요!)
    /// NavMeshAgent가 미세하게 Z값을 틀어놓는 문제 해결
    /// 아이소메트릭 정렬 보호를 위한 필수 코드
    /// </summary>
    protected virtual void LateUpdate()
    {
        // NavMesh 사용 중일 때만 Z축 고정
        if (IsUsingNavMesh && Agent.enabled)
        {
            Vector3 pos = transform.position;
            transform.position = new Vector3(pos.x, pos.y, 0f);
        }
    }
    
    #endregion

    // BaseEnemy 클래스에 추가할 필드들
    [Header("위치 정보")]
    [SerializeField] protected Vector3 homePosition; // 집 위치
    [SerializeField] protected float patrolRadius = 0f; // 순찰 반경 (0 = EnemyData 사용)
    [SerializeField] protected Vector3 spawnPosition; // 스폰된 위치

    // 프로퍼티 추가
    public Vector3 HomePosition => homePosition;
    public virtual float PatrolRadius 
    { 
        get 
        {
            // ⭐⭐⭐ 우선순위: EnemyData → 스폰 설정 → 기본값
            
            // 1순위: EnemyData (ScriptableObject)
            if (enemyData != null && enemyData.PatrolRadius > 0)
            {
                return enemyData.PatrolRadius;
            }
            
            // 2순위: 스폰 시 설정된 값 (SetHomePosition으로 설정)
            // SpawnSystem에서 0이 아닌 값으로 설정한 경우
            if (patrolRadius > 0) 
            {
                return patrolRadius;
            }
                
            // 3순위: 최후 기본값
            return 3f;
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
                // EnemyData의 DetectionRange 프로퍼티 사용
                return enemyData.DetectionRange;
            }
                
            // 기본값 반환
            return 5f;
        } 
    }

    // ChaseRange 프로퍼티 추가 (데이터 기반)
    public virtual float ChaseRange 
    { 
        get 
        {
            if (enemyData != null)
            {
                // EnemyData의 ChaseRange 프로퍼티 사용
                return enemyData.ChaseRange;
            }
                
            // 기본값 반환
            return 8f;
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

    }

    #endregion
    
    #region 🎯 연출 넉백 시스템 (NavMesh 호환)
    
    /// <summary>
    /// NavMesh 몬스터용 연출 넉백 (물리 없이 Transform 이동)
    /// ⭐ EnemyHitState에서 호출됨
    /// ⚠️ isStopped 관리는 HitState에서 담당! (멈칫거림 방지)
    /// </summary>
    public IEnumerator PerformKnockbackEffect(Vector2 damageSourcePosition)
    {
        // NavMesh 미사용 몬스터는 기존 Knockback.cs가 처리하므로 스킵
        if (!IsUsingNavMesh)
        {
            yield break;
        }
        
        // 넉백 배율 소비 (0이면 넉백 없이 종료)
        float scale = PendingKnockbackScale;
        PendingKnockbackScale = 1f; // 소비 후 기본값 복원
        if (scale <= 0f) yield break;
        
        // ⚙️ 연출 넉백 설정값
        float retreatDistance = 0.8f * scale; // 후퇴 거리에 배율 적용
        float retreatDuration = 0.25f;         // 후퇴 시간 (0.25초)
        float elapsed = 0f;
        
        // 후퇴 방향 계산 (데미지 받은 반대쪽)
        Vector2 knockbackDirection = ((Vector2)transform.position - damageSourcePosition).normalized;
        
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + (Vector3)knockbackDirection * retreatDistance;
        
        
        // 부드러운 후퇴 애니메이션
        while (elapsed < retreatDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / retreatDuration;
            
            // EaseOut 곡선 (빠르게 시작 → 부드럽게 감속)
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
            
            // Z축 고정하면서 위치 이동
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, smoothT);
            newPos.z = 0f;
            transform.position = newPos;
            
            yield return null;
        }
        
        // 최종 위치 설정
        Vector3 finalPos = targetPos;
        finalPos.z = 0f;
        transform.position = finalPos;
        
        // ⭐ NavMeshAgent 위치 동기화 (중요!)
        // isStopped는 HitState에서 관리하므로 여기서는 위치만 동기화
        if (Agent.enabled && Agent.isOnNavMesh)
        {
            Agent.Warp(finalPos);
        }
        
    }
    
    #endregion
    
    #region ⚙️ IEnemyTarget 인터페이스 구현 (Phase 4: ConditionalModifier)
    
    /// <summary>
    /// 몬스터 타입 반환 (Basic, Elite, Boss)
    /// </summary>
    public EnemyType GetEnemyType()
    {
        // EnemyData가 없으면 Basic으로 간주
        if (enemyData == null)
        {
            Debug.LogWarning($"[BaseEnemy] {gameObject.name}: EnemyData가 없어 Basic 타입으로 간주합니다.");
            return EnemyType.Basic;
        }
        
        return enemyData.EnemyType; // Public property 사용
    }
    
    /// <summary>
    /// 보스 몬스터인지 확인
    /// </summary>
    public bool IsBoss()
    {
        return GetEnemyType() == EnemyType.Boss;
    }
    
    /// <summary>
    /// 엘리트 몬스터인지 확인
    /// </summary>
    public bool IsElite()
    {
        return GetEnemyType() == EnemyType.Elite;
    }
    
    /// <summary>
    /// 현재 체력 비율 반환 (0.0 ~ 1.0)
    /// </summary>
    public float GetCurrentHpPercent()
    {
        if (enemyHealth == null)
        {
            Debug.LogWarning($"[BaseEnemy] {gameObject.name}: EnemyHealth 컴포넌트가 없습니다.");
            return 1.0f; // 안전 값
        }
        
        // EnemyHealth에서 현재 체력과 최대 체력 가져오기
        float currentHp = enemyHealth.CurrentHealth;
        float maxHp = enemyHealth.MaxHealth;
        
        if (maxHp <= 0)
        {
            Debug.LogWarning($"[BaseEnemy] {gameObject.name}: MaxHealth가 0 이하입니다.");
            return 0f;
        }
        
        return Mathf.Clamp01(currentHp / maxHp);
    }
    
    /// <summary>
    /// GameObject 참조 반환 (디버그/위치 확인용)
    /// </summary>
    public GameObject GetGameObject()
    {
        return gameObject;
    }
    
    #endregion

    #region ITargetable 구현 (자동 타겟팅 시스템)

    private TargetOutlineEffect _outlineEffect;

    bool ITargetable.IsAlive()
    {
        return enemyHealth != null && !enemyHealth.isDead;
    }

    EnemyRank ITargetable.GetRank()
    {
        if (enemyData != null && enemyData.IsMiniBoss) return EnemyRank.MiniBoss;
        EnemyType type = GetEnemyType();
        return type switch
        {
            EnemyType.Boss  => EnemyRank.Boss,
            EnemyType.Elite => EnemyRank.Elite,
            _               => EnemyRank.Normal,
        };
    }

    Transform ITargetable.GetTransform() => transform;

    void ITargetable.ActivateLockOn()
    {
        if (_outlineEffect == null) _outlineEffect = GetComponentInChildren<TargetOutlineEffect>();
        _outlineEffect?.Activate();
    }

    void ITargetable.DeactivateLockOn()
    {
        _outlineEffect?.Deactivate();
    }

    #endregion
} 