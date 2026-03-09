using UnityEngine;
using System.Collections.Generic;
using CueSystem;

/// <summary>
/// 플레이어 스킬 전용 AOE 데미지 처리 컴포넌트
/// SkillAOESpawner가 생성한 AOE GameObject에 자동으로 추가됨
/// 역할: 콜라이더 기반 자동 Enemy 감지 및 데미지 적용 + Hit Cue 발행
/// </summary>
public class PlayerSkillAOEDamage : MonoBehaviour
{
    #region Inspector 설정
    
    [Header("Damage Settings")]
    [SerializeField] private int damageAmount = 0; // 외부에서 SetDamage()로 설정
    [SerializeField] private float skillMultiplier = 1.0f; // 스킬 배율 (외부에서 설정 가능)
    [SerializeField] private LayerMask enemyLayerMask = 1 << 6; // Enemy layer (기본값 6)
    [SerializeField] private bool damageOnce = true; // 한 번만 데미지 (기본값 true)
    
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    #endregion
    
    #region Private Fields
    
    private HashSet<Collider2D> hitEnemies = new HashSet<Collider2D>(); // 중복 데미지 방지
    
    #endregion
    
    #region Unity Lifecycle
    
    private void OnEnable()
    {
        // 풀링 시스템 대응: 활성화될 때마다 초기화
        hitEnemies.Clear();
        
        if (showDebugLogs)
            Debug.Log($"[PlayerSkillAOEDamage] 활성화 - 데미지: {damageAmount}");
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// 데미지 설정
    /// </summary>
    public void SetDamage(int damage)
    {
        damageAmount = damage;
        
        if (showDebugLogs)
            Debug.Log($"[PlayerSkillAOEDamage] 데미지 설정: {damageAmount}");
    }
    
    /// <summary>
    /// 스킬 배율 설정 (CombatFormula용)
    /// </summary>
    public void SetSkillMultiplier(float multiplier)
    {
        skillMultiplier = multiplier;
        
        if (showDebugLogs)
            Debug.Log($"[PlayerSkillAOEDamage] 스킬 배율 설정: {skillMultiplier}x");
    }
    
    /// <summary>
    /// Enemy LayerMask 설정
    /// </summary>
    public void SetEnemyLayerMask(LayerMask layerMask)
    {
        enemyLayerMask = layerMask;
        
        if (showDebugLogs)
            Debug.Log($"[PlayerSkillAOEDamage] Enemy LayerMask 설정: {layerMask.value}");
    }
    
    /// <summary>
    /// Hit Cue 이벤트 키 설정
    /// </summary>
    #endregion
    
    #region Collision Detection
    
    /// <summary>
    /// 트리거 진입 시 Enemy 감지 및 데미지 처리
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 한 번만 데미지이고 이미 맞은 적이면 스킵
        if (damageOnce && hitEnemies.Contains(other))
        {
            return;
        }
        
        // Enemy Layer 체크
        if (((1 << other.gameObject.layer) & enemyLayerMask) == 0)
        {
            return; // Enemy가 아니면 스킵
        }
        
        // EnemyHealth 컴포넌트 확인
        var enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            // ⚔️ CombatFormula 데미지 계산
            var playerStats = FindObjectOfType<PlayerRuntimeStats>();
            IPlayerClass playerClass = FindObjectOfType<BaseClassBehaviour>();
            
            var ctx = new CombatFormula.AttackContext
            {
                baseAttack = playerStats != null ? playerStats.FinalAttackDamage : damageAmount,
                attackerClass = playerClass,
                targetDefense = GetTargetDefense(other),
                targetTransform = other.transform,
                attackerTransform = transform,
                isSkillAttack = true,
                skillMultiplier = skillMultiplier,
                criticalChance = playerStats != null ? playerStats.FinalCriticalChance : 0f,
                criticalMultiplier = playerStats != null ? playerStats.FinalCriticalDamage : 1.5f,
                isPlayerAttack = true,
                attackerLevel = GetPlayerLevel(),
                
                // ⚙️ Phase 4: ConditionalModifier용 필드
                target = other.GetComponent<IEnemyTarget>(),
                selfHpPercent = GetPlayerHpPercent(),
                targetHpPercent = GetTargetHpPercent(other)
            };
            
            var result = CombatFormula.CalculatePlayerToEnemyDamage(ctx);
            
            // ⭐ 피격 이펙트 정보 추가 (피격자가 발행할 수 있도록)
            result.hitPosition = other.transform.position;
            result.attackerGrade = GetSkillWeaponGrade();  // 현재 무기 등급 사용
            
            // ⚔️ Phase 4-C: DamageResult 통째로 전달 (피격자가 면역/회복차단 처리)
            enemyHealth.TakeDamage(result, transform);
            
            // ⚙️ Phase 4-C: 공격자 측 후처리 (흡혈만)
            ApplyLifeStealOnly(result);
            
            // 중복 데미지 방지용 추가
            hitEnemies.Add(other);
            
            if (showDebugLogs)
                Debug.Log($"💥 [PlayerSkillAOEDamage] {other.name}에게 {result.finalDamage} 데미지! (크리티컬: {result.isCritical}, 백어택: {result.isBackAttack})");
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// 대상의 방어력 가져오기
    /// ✅ 몬스터 방어력 시스템 활성화
    /// </summary>
    private float GetTargetDefense(Collider2D target)
    {
        // BaseEnemy에서 스케일된 방어력 가져오기
        var baseEnemy = target.GetComponent<BaseEnemy>();
        if (baseEnemy != null)
        {
            return baseEnemy.GetScaledDefense();
        }
        
        return 0f;
    }
    
    /// <summary>
    /// 스킬 사용 시 현재 무기 등급 가져오기
    /// </summary>
    private ItemGrade GetSkillWeaponGrade()
    {
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon?.CurrentActiveWeapon == null) return ItemGrade.C;
        
        var weaponComponent = activeWeapon.CurrentActiveWeapon as IWeapon;
        if (weaponComponent == null) return ItemGrade.C;
        
        var equipmentData = weaponComponent.GetEquipmentData();
        if (equipmentData == null) return ItemGrade.C;
        
        return equipmentData.itemGrade;
    }
    
    /// <summary>
    /// 플레이어 레벨 가져오기 (Dynamic K 계산용)
    /// </summary>
    private int GetPlayerLevel()
    {
        // PlayerRuntimeStats를 통해 레벨 가져오기 (일관성 있는 데이터 소스)
        var playerStats = FindObjectOfType<PlayerRuntimeStats>();
        if (playerStats != null)
        {
            return playerStats.CurrentLevel;
        }
        
        // Fallback: PlayerDataManager에서 직접 가져오기
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.selectedPlayerData != null)
        {
            return PlayerDataManager.Instance.selectedPlayerData.currentLevel;
        }
        
        return 1; // 기본값
    }
    
    /// <summary>
    /// 플레이어 HP 비율 가져오기 (조건부 모디파이어용)
    /// ⚙️ Phase 4: ConditionalModifier
    /// </summary>
    private float GetPlayerHpPercent()
    {
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            return playerHealth.GetCurrentHpPercent();
        }
        
        return 1.0f; // 안전 값
    }
    
    /// <summary>
    /// 대상 HP 비율 가져오기 (조건부 모디파이어용)
    /// ⚙️ Phase 4: ConditionalModifier
    /// </summary>
    private float GetTargetHpPercent(Collider2D target)
    {
        var enemyTarget = target.GetComponent<IEnemyTarget>();
        if (enemyTarget != null)
        {
            return enemyTarget.GetCurrentHpPercent();
        }
        
        return 1.0f; // 안전 값
    }
    
    #endregion
    
    #region Debug
    
    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    [ContextMenu("Debug PlayerSkillAOEDamage Info")]
    public void DebugInfo()
    {
        string info = $"=== PlayerSkillAOEDamage {gameObject.name} ===\n";
        info += $"Damage Amount: {damageAmount}\n";
        info += $"Enemy LayerMask: {enemyLayerMask.value}\n";
        info += $"Damage Once: {damageOnce}\n";
        info += $"Hit Enemies Count: {hitEnemies.Count}\n";
        
        Debug.Log(info);
    }
    
    #endregion
    
    #region ⚙️ Phase 4-C: 후처리 효과 적용 (공격자 측: 흡혈만)
    
    /// <summary>
    /// Phase 4-C: 공격자 측 후처리 (흡혈만 공격자가 처리)
    /// </summary>
    private void ApplyLifeStealOnly(CombatFormula.DamageResult result)
    {
        // 흡혈 처리 (공격자가 체력 회복)
        if (result.lifeStealAmount > 0)
        {
            ApplyLifeSteal(result.lifeStealAmount);
        }
    }
    
    /// <summary>
    /// 흡혈: 플레이어 체력 회복
    /// </summary>
    private void ApplyLifeSteal(float amount)
    {
        if (amount <= 0) return;
        
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            int healAmount = Mathf.RoundToInt(amount);
            playerHealth.HealPlayerAmount(healAmount);
            
            if (showDebugLogs)
                Debug.Log($"💚 [PlayerSkillAOEDamage] 흡혈: {healAmount} HP 회복");
        }
    }
    
    #endregion
}

