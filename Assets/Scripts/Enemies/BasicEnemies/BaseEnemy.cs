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
    public abstract float PatrolRadius { get; } 
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
    /// 현재 상태 디버그 출력
    /// </summary>
    [ContextMenu("Debug Monster Info")]
    public virtual void DebugMonsterInfo()
    {
        string info = $"=== {GetType().Name} {gameObject.name} ===\n";
        info += $"Current Level: {currentLevel}\n";
        info += $"Patrol Radius: {PatrolRadius}\n";
        info += $"Attack Range: {AttackRange}\n\n";
        
        if (enemyData != null)
        {
            info += "=== EnemyData 정보 ===\n";
            info += enemyData.GetDebugInfo(currentLevel, growthProfile);
        }
        else
        {
            info += "❌ EnemyData가 할당되지 않았습니다!\n";
        }
        
        Debug.Log(info);
    }

    #endregion
    
    // 추상 메서드들 - 하위 클래스에서 구현 필수
    protected abstract void OnAwakeInitialize();
    protected abstract void OnStartInitialize();
    protected abstract void InitializeAttackSystem();
    public abstract void Attack(); // IEnemy 인터페이스 구현
} 