using System.Collections;
using UnityEngine;

/// <summary>
/// 모든 몬스터의 기본 클래스 - 공통 기능 템플릿화
/// BlueSlime, Grape, Ghost의 중복 코드 제거
/// </summary>
public abstract class BaseEnemy : MonoBehaviour, IEnemy
{
    // IEnemy 인터페이스 구현 (공통)
    public EnemyAnimationController AnimationController { get; private set; }
    public EnemyFSMController FSMController { get; private set; }
    public PlayerController TargetPlayer { get; private set; }
    
    // 공통 속성들
    public Vector2 SpawnPoint { get; private set; }
    public abstract float PatrolRadius { get; } // 각 몬스터마다 다르므로 추상화
    public abstract float AttackRange { get; }  // 각 몬스터마다 다르므로 추상화

    // 공통 컴포넌트들
    protected SpriteRenderer spriteRenderer;
    protected AudioSource audioSource;
    protected EnemyHealth enemyHealth;

    protected virtual void Awake()
    {
        // 공통 컴포넌트 초기화
        AnimationController = GetComponent<EnemyAnimationController>();
        FSMController = GetComponent<EnemyFSMController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        enemyHealth = GetComponent<EnemyHealth>();
        
        // 스폰 지점 저장
        SpawnPoint = transform.position;
        // Debug.Log($"[{GetType().Name}] {gameObject.name} 스폰 지점 저장: {SpawnPoint}");
        
        // 추가 초기화 (하위 클래스에서 구현)
        OnAwakeInitialize();
    }

    protected virtual void Start()
    {
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
    
    // 추상 메서드들 - 하위 클래스에서 구현 필수
    protected abstract void OnAwakeInitialize();
    protected abstract void OnStartInitialize();
    protected abstract void InitializeAttackSystem();
    public abstract void Attack(); // IEnemy 인터페이스 구현
} 