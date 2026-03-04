using System.Collections;
using UnityEngine;

/// <summary>
/// Boss_ForestElemental 보스 몬스터 클래스 - 근접 공격 + 4개 스킬 + 페이즈 전환 (8방향 스프라이트)
/// ⭐ 보스 전용: 평타 + 페이즈별 스킬 시스템 + HP 기반 페이즈 전환
/// ⭐ 완전한 데이터 기반 시스템
/// </summary>
public class Boss_ForestElemental : BaseEnemy
{
    [Header("⭐ Boss_ForestElemental 전용 컴포넌트")]
    [SerializeField] private MeleeAttack meleeAttack;                  // 평타용
    [SerializeField] private BossPhaseController phaseController;      // 페이즈 관리
    [SerializeField] private BossAttackBehaviour bossAttack;           // 통합 공격 관리
    
    [Header("🗺️ 아이소메트릭 데이터")]
    [SerializeField] private IsometricCharacterData isometricData;
    
    #region ⭐ BaseEnemy 추상 속성 구현 - 완전한 데이터 기반
    
    public override float PatrolRadius 
    { 
        get 
        {
            // 스폰 시 설정된 값 우선 사용
            if (patrolRadius > 0) 
                return patrolRadius;
            
            // 데이터 기반 fallback (보스는 넓은 패트롤 범위)
            if (enemyData != null)
                return enemyData.PatrolRadius;
            
            return 5f; // 보스 기본값 (넓은 영역)
        } 
    }
    
    public override float AttackRange 
    { 
        get 
        {
            // 1순위: AttackData (평타)
            if (meleeAttack != null && meleeAttack.AttackData != null)
                return meleeAttack.AttackData.AttackRange;
                
            Debug.LogError($"[Boss_ForestElemental] {gameObject.name}: MeleeAttack 또는 AttackData가 없습니다!");
            return 2.5f; // 보스는 긴 공격 범위
        } 
    }
    
    public override float DetectionRange
    {
        get
        {
            if (EnemyData != null)
            {
                float range = EnemyData.DetectionRange;
                Debug.Log($"[Boss_ForestElemental] {gameObject.name} DetectionRange: {range} (from EnemyData)");
                return range;
            }
                
            Debug.LogError($"[Boss_ForestElemental] {gameObject.name}: EnemyData가 없어서 DetectionRange 확인 불가! fallback 8f 사용");
            return 8f; // 보스는 매우 넓은 감지 범위
        }
    }
    
    public override float ChaseRange 
    { 
        get 
        {
            if (EnemyData != null)
            {
                float range = EnemyData.ChaseRange;
                Debug.Log($"[Boss_ForestElemental] {gameObject.name} ChaseRange: {range} (from EnemyData)");
                return range;
            }
                
            Debug.LogError($"[Boss_ForestElemental] {gameObject.name}: EnemyData가 없어서 ChaseRange 확인 불가! fallback 12f 사용");
            return 12f; // 보스는 매우 긴 추적 범위
        } 
    }
    
    #endregion
    
    #region ⭐ BaseEnemy 추상 메서드 구현
    
    protected override void OnAwakeInitialize()
    {
        // MeleeAttack 컴포넌트 확인
        if (meleeAttack == null)
            meleeAttack = GetComponent<MeleeAttack>();
        
        // BossPhaseController 확인
        if (phaseController == null)
            phaseController = GetComponent<BossPhaseController>();
        
        // BossAttackBehaviour 확인
        if (bossAttack == null)
            bossAttack = GetComponent<BossAttackBehaviour>();
            
        Debug.Log($"[Boss_ForestElemental] {gameObject.name} Awake 초기화 완료");
    }
    
    protected override void OnStartInitialize()
    {
        ApplyBossForestElementalSpecificSettings();
        
        Debug.Log($"[Boss_ForestElemental] {gameObject.name} Start 초기화 완료");
        
        // ⭐ StageManager에 보스 스폰 알림 (모든 초기화 완료 후 마지막에 호출!)
        // 코루틴으로 1프레임 대기 후 호출하여 BaseEnemy.Start() 완전 완료 보장
        StartCoroutine(NotifyBossSpawnedAfterFrame());
    }
    
    /// <summary>
    /// ⭐ 1프레임 대기 후 보스 스폰 알림 (타이밍 이슈 해결)
    /// </summary>
    private IEnumerator NotifyBossSpawnedAfterFrame()
    {
        // 1프레임 대기 → BaseEnemy.Start() 완전 완료
        yield return null;
        
        NotifyBossSpawned();
    }
    
    /// <summary>
    /// ⭐ StageManager에 보스 스폰 알림
    /// StageUI가 OnBossSpawned 이벤트를 받아서 BossHealthUI 자동 연결
    /// </summary>
    private void NotifyBossSpawned()
    {
        if (StageSystem.StageManager.Instance != null)
        {
            StageSystem.StageManager.Instance.OnBossSpawned?.Invoke(gameObject);
            Debug.Log($"🐲 [Boss_ForestElemental] StageManager에 보스 스폰 알림 완료!");
        }
        else
        {
            Debug.LogWarning($"⚠️ [Boss_ForestElemental] StageManager.Instance가 null입니다!");
            
            // Fallback: 직접 StageUI 찾기
            StageSystem.StageUI stageUI = FindObjectOfType<StageSystem.StageUI>();
            if (stageUI != null)
            {
                stageUI.ActivateBossHealthUI(gameObject);
                Debug.Log($"🐲 [Boss_ForestElemental] StageUI 직접 연결 완료 (Fallback)");
            }
        }
    }
    
    protected override void InitializeAttackSystem()
    {
        // 평타 시스템 초기화
        if (meleeAttack != null)
        {
            meleeAttack.Initialize();
            Debug.Log($"[Boss_ForestElemental] {gameObject.name} MeleeAttack 시스템 초기화 완료");
        }
        else
        {
            Debug.LogError($"[Boss_ForestElemental] {gameObject.name}: MeleeAttack 컴포넌트가 없습니다!");
        }
        
        // ⭐ 보스 공격 시스템 초기화 (핵심!)
        if (bossAttack != null)
        {
            bossAttack.Initialize();
            Debug.Log($"[Boss_ForestElemental] {gameObject.name} BossAttackBehaviour 시스템 초기화 완료");
        }
        else
        {
            Debug.LogError($"[Boss_ForestElemental] {gameObject.name}: BossAttackBehaviour 컴포넌트가 없습니다!");
        }
    }
    
    public override void Attack()
    {
        // 보스는 BossAttackBehaviour가 공격 결정 (페이즈 + 거리 기반)
        if (bossAttack != null && bossAttack.CanAttack())
        {
            bossAttack.Attack();
            Debug.Log($"[Boss_ForestElemental] {gameObject.name} 보스 공격 실행 (평타 or 스킬)!");
        }
        else if (meleeAttack != null && meleeAttack.CanAttack())
        {
            // Fallback: BossAttackBehaviour가 없으면 평타만
            meleeAttack.Attack();
            Debug.LogWarning($"[Boss_ForestElemental] {gameObject.name} BossAttackBehaviour 없음 - 평타로 fallback");
        }
    }
    
    #endregion
    
    #region ⭐ Boss_ForestElemental 전용 설정
    
    /// <summary>
    /// Boss_ForestElemental 전용 설정 적용
    /// </summary>
    private void ApplyBossForestElementalSpecificSettings()
    {
        if (EnemyData != null)
        {
            Debug.Log($"[Boss_ForestElemental] {gameObject.name} 보스 전용 설정 적용:");
            Debug.Log($"  - IsBoss: {EnemyData.IsBoss}");
            Debug.Log($"  - EnemyType: {EnemyData.EnemyType}");
            Debug.Log($"  - 체력: {GetScaledMaxHealth():F1}");
            Debug.Log($"  - 공격범위: {AttackRange:F1}");
            Debug.Log($"  - 감지범위: {DetectionRange:F1}");
            Debug.Log($"  - 추적범위: {ChaseRange:F1}");
        }
        
        if (phaseController != null)
        {
            Debug.Log($"[Boss_ForestElemental] 페이즈 시스템 활성화: {phaseController.TotalPhases}개 페이즈");
        }
    }
    
    #endregion
    
    #region 🗺️ 아이소메트릭 데이터 시스템
    
    /// <summary>
    /// 아이소메트릭 데이터 가져오기 (BaseEnemy 추상 메서드 구현)
    /// </summary>
    public override IsometricCharacterData GetIsometricData()
    {
        if (isometricData == null)
        {
            Debug.LogWarning($"[Boss_ForestElemental] {gameObject.name}: IsometricData가 없어서 기본값 생성");
            isometricData = CreateDefaultIsometricData();
        }
        
        return isometricData;
    }
    
    private void OnValidate()
    {
        // 아이소메트릭 데이터 기본값 설정
        if (isometricData == null)
        {
            isometricData = new IsometricCharacterData();
            isometricData.SetDefaults();
        }
        
        if (!isometricData.IsValid())
        {
            Debug.LogWarning($"[Boss_ForestElemental] {name}의 아이소메트릭 데이터가 유효하지 않습니다.");
            isometricData.SetDefaults();
        }
    }
    
    #endregion
    
    #region 🎮 보스 전용 기능
    
    /// <summary>
    /// 현재 페이즈 정보 출력 (디버그용)
    /// </summary>
    [ContextMenu("Debug Boss Phase Info")]
    public void DebugBossPhaseInfo()
    {
        if (phaseController != null)
        {
            phaseController.DebugPhaseInfo();
        }
        else
        {
            Debug.LogWarning("[Boss_ForestElemental] PhaseController가 없습니다!");
        }
    }
    
    /// <summary>
    /// 강제 다음 페이즈로 전환 (테스트용)
    /// </summary>
    [ContextMenu("Force Next Phase")]
    public void ForceNextPhase()
    {
        if (phaseController != null)
        {
            phaseController.ForceNextPhase();
        }
        else
        {
            Debug.LogWarning("[Boss_ForestElemental] PhaseController가 없습니다!");
        }
    }
    
    #endregion
}

