using System;
using UnityEngine;

/// <summary>
/// 전투 공식에 필요한 컨텍스트 정보를 담는 구조체
/// ⚡ GC 최소화를 위한 struct 설계
/// ⚙️ Phase 4: ConditionalModifier 시스템의 조건 판정에 사용
/// </summary>
public struct CombatContext
{
    #region 공격자 정보
    
    /// <summary>
    /// 공격자가 플레이어인 경우 클래스 정보
    /// (Berserker 모드, BackAttack 보너스 등 판정용)
    /// </summary>
    public IPlayerClass attackerClass;
    
    /// <summary>
    /// 공격자의 현재 체력 비율 (0.0 ~ 1.0)
    /// Phase 3 (공격 페이즈)에서 SelfHpBelow/Above 조건 시 사용
    /// </summary>
    public float attackerHpPercent;
    
    /// <summary>
    /// 공격자의 위치 (백어택 판정용)
    /// </summary>
    public Vector3 attackerPosition;
    
    #endregion
    
    #region 피격자 정보
    
    /// <summary>
    /// 피격 대상 (보스/엘리트 구분용)
    /// 조건: TargetIsBoss, TargetIsElite
    /// </summary>
    public IEnemyTarget target;
    
    /// <summary>
    /// 피격자의 현재 체력 비율 (0.0 ~ 1.0)
    /// Phase 5 (방어 페이즈)에서 SelfHpBelow/Above 조건 시 사용
    /// TargetHpAbove, TargetHpBelow 조건에도 사용
    /// </summary>
    public float defenderHpPercent;
    
    /// <summary>
    /// 피격자의 위치 (거리/방향 판정용)
    /// </summary>
    public Vector3 targetPosition;
    
    #endregion
    
    #region 전투 상태
    
    /// <summary>
    /// 스킬 공격 여부
    /// </summary>
    public bool isSkillAttack;
    
    /// <summary>
    /// 백어택 여부
    /// </summary>
    public bool isBackAttack;
    
    /// <summary>
    /// 크리티컬 발생 여부 (Phase4에서 설정)
    /// </summary>
    public bool isCritical;
    
    /// <summary>
    /// 데미지 타입 (향후 확장)
    /// "AREA_GROUND", "PROJECTILE", "MELEE" 등
    /// </summary>
    public string damageType;
    
    #endregion
    
    #region 디버그 정보
    
    /// <summary>
    /// 디버그용 공격자 이름
    /// </summary>
    public string attackerName;
    
    /// <summary>
    /// 디버그용 피격자 이름
    /// </summary>
    public string targetName;
    
    #endregion
    
    #region 생성 헬퍼 메서드
    
    /// <summary>
    /// 플레이어 → 적 공격용 컨텍스트 생성
    /// </summary>
    public static CombatContext CreatePlayerToEnemy(
        IPlayerClass playerClass,
        float playerHpPercent,
        IEnemyTarget enemy,
        float enemyHpPercent,
        bool isSkillAttack,
        bool isBackAttack,
        string damageType = "MELEE"
    )
    {
        return new CombatContext
        {
            attackerClass = playerClass,
            attackerHpPercent = playerHpPercent, // 공격자(플레이어) HP
            attackerPosition = playerClass?.GetGameObject()?.transform.position ?? Vector3.zero,
            attackerName = playerClass?.GetGameObject()?.name ?? "Player",
            
            target = enemy,
            defenderHpPercent = enemyHpPercent, // 피격자(적) HP
            targetPosition = enemy?.GetGameObject()?.transform.position ?? Vector3.zero,
            targetName = enemy?.GetGameObject()?.name ?? "Enemy",
            
            isSkillAttack = isSkillAttack,
            isBackAttack = isBackAttack,
            isCritical = false, // Phase4에서 설정됨
            damageType = damageType
        };
    }
    
    /// <summary>
    /// 적 → 플레이어 공격용 컨텍스트 생성
    /// </summary>
    public static CombatContext CreateEnemyToPlayer(
        IEnemyTarget enemy,
        float enemyHpPercent,
        float playerHpPercent,
        bool isSkillAttack,
        string damageType = "MELEE"
    )
    {
        return new CombatContext
        {
            attackerClass = null,
            attackerHpPercent = enemyHpPercent, // 공격자(적) HP
            attackerPosition = enemy?.GetGameObject()?.transform.position ?? Vector3.zero,
            attackerName = enemy?.GetGameObject()?.name ?? "Enemy",
            
            target = null, // 플레이어는 IEnemyTarget이 아니므로 null
            defenderHpPercent = playerHpPercent, // 피격자(플레이어) HP
            targetPosition = Vector3.zero, // TODO: 플레이어 위치 전달 필요 시 추가
            targetName = "Player",
            
            isSkillAttack = isSkillAttack,
            isBackAttack = false,
            isCritical = false,
            damageType = damageType
        };
    }
    
    #endregion
}

