using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CueSystem; // ⭐ Phase 1-1: 히트 이펙트 Cue 시스템

/// <summary>
/// 🎯 DamageSource - PlayerRuntimeStats 기반 데미지 처리
/// 책임: PlayerRuntimeStats에서 최종 스탯을 가져와 적에게 데미지 적용
/// 개선: 실시간 무기 데미지 반영, 클래스별 특수 효과 통합 관리
/// </summary>
public class DamageSource : MonoBehaviour
{
    [Header("🔗 컴포넌트 참조")]
    [SerializeField] private bool showDebugLogs = true; // ⭐ 벽 차단 테스트를 위해 true로 설정
    
    [Header("🧱 벽 충돌 설정")]
    [SerializeField] private bool checkWallBlocking = true;
    [SerializeField] private LayerMask wallLayer; // Inspector에서 Wall 선택
    
    // 핵심 참조
    private PlayerRuntimeStats playerRuntimeStats;
    private Warrior warrior;
    private Assasin assasin;
    
    // 스킬 데미지 오버라이드 (투사체형 액티브 스킬용)
    private int _skillDamageOverride = 0;
    private bool _hasSkillDamage = false;
    
    private void Start() 
    {
        InitializeReferences();
    }
    
    /// <summary>
    /// 🔗 필수 참조들 초기화
    /// </summary>
    private void InitializeReferences()
    {
        // PlayerRuntimeStats 참조 (최우선)
        playerRuntimeStats = GetComponentInParent<PlayerRuntimeStats>();
        if (playerRuntimeStats == null)
        {
            playerRuntimeStats = FindObjectOfType<PlayerRuntimeStats>();
        }
        
        // 클래스별 컴포넌트 참조 (특수 효과용)
        warrior = FindObjectOfType<Warrior>();
        assasin = FindObjectOfType<Assasin>();
        
        if (showDebugLogs)
        {
            Debug.Log($"🔗 [DamageSource] 참조 초기화:");
            Debug.Log($"   - PlayerRuntimeStats: {(playerRuntimeStats != null ? "연결됨" : "❌ 없음")}");
            Debug.Log($"   - Warrior: {(warrior != null ? "감지됨" : "없음")}");
            Debug.Log($"   - Assasin: {(assasin != null ? "감지됨" : "없음")}");
            
            if (playerRuntimeStats != null)
            {
                Debug.Log($"📊 [DamageSource] 현재 스탯: 공격력 {playerRuntimeStats.FinalAttackDamage:F1}");
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other) 
    {
        // ⛓️ 체인 발사체는 Projectile.OnTriggerEnter2D에서 직접 데미지를 처리하므로 여기서는 스킵
        // (배율 감소 타이밍 및 중복 타격 방지를 Projectile이 완전 제어)
        Projectile proj = GetComponent<Projectile>();
        if (proj != null && proj.IsChainShotActive) return;
        
        // 🆕 SimpleMob 체크 (최우선) — 트리거 콜라이더(접촉 공격용)는 제외, 물리 콜라이더에만 피격 처리
        SimpleMob simpleMob = other.gameObject.GetComponent<SimpleMob>();
        if (simpleMob != null && !simpleMob.IsDead && !other.isTrigger)
        {
            DealDamageToSimpleMob(simpleMob, other);
            return;
        }
        
        // 기존 몬스터 (EnemyHealth)
        EnemyHealth enemyHealth = other.gameObject.GetComponent<EnemyHealth>();
        if (enemyHealth == null) return;
        
        // 🧱 벽 차단 체크
        if (checkWallBlocking && IsBlockedByWall(other.transform))
        {
            if (showDebugLogs)
                Debug.Log($"🚫 [DamageSource] {other.name} - 벽에 막혀서 공격 실패");
            return;
        }
        
        // ⚔️ CombatFormula 데미지 계산
        float critChance = playerRuntimeStats != null ? playerRuntimeStats.FinalCriticalChance : 0f;
        float critDamage = playerRuntimeStats != null ? playerRuntimeStats.FinalCriticalDamage : 1.5f;
        
        // 스킬 데미지 오버라이드 여부에 따라 baseAttack 결정
        float baseAttack = _hasSkillDamage ? _skillDamageOverride : GetCurrentBaseDamage();
        
        // Step 3 동적 플래그 평가: Warrior 버서커 모드 여부를 호출부에서 직접 판단하여 전달
        // IPlayerClass 타입 캐스트 대신 런타임 상태 플래그로 CombatFormula에 전달
        bool isBerserkerState = EvaluateBerserkerState();
        
        if (showDebugLogs)
        {
            Debug.Log($"🎯 [DamageSource] 크리티컬 확률: {critChance:P2} (치명 데미지: {critDamage:F2}x)");
            if (_hasSkillDamage)
                Debug.Log($"🏹 [DamageSource] 스킬 데미지 적용: {_skillDamageOverride} (FinalAttackDamage × 스킬배율 사전 계산)");
            if (isBerserkerState)
                Debug.Log($"🔥 [DamageSource] 버서커 상태 감지 → isBerserkerState = true (Step 3 +50%)");
        }
        
        var ctx = new CombatFormula.AttackContext
        {
            baseAttack = baseAttack,
            attackerClass = GetComponent<IPlayerClass>(),
            targetDefense = GetTargetDefense(other),
            targetTransform = other.transform,
            attackerTransform = transform,
            isSkillAttack = _hasSkillDamage,
            skillMultiplier = 1.0f,
            criticalChance = critChance,
            criticalMultiplier = critDamage,
            isPlayerAttack = true,
            attackerLevel = GetPlayerLevel(),
            isBerserkerState = isBerserkerState,
            
            // Step 5: 스탯 기반 방어구 관통률
            armorPenetration = playerRuntimeStats != null ? playerRuntimeStats.FinalArmorPenetration : 0f,
            // Phase 7: 스탯 기반 기본 흡혈률 + 오버킬 방지용 타격 전 적 현재 체력
            lifeStealPercent = playerRuntimeStats != null ? playerRuntimeStats.FinalLifeSteal : 0f,
            targetCurrentHp = Mathf.Max(0, enemyHealth.CurrentHealth),
            
            // ⚙️ Phase 4: ConditionalModifier용 필드
            target = other.GetComponent<IEnemyTarget>(),
            selfHpPercent = GetPlayerHpPercent(),
            targetHpPercent = GetTargetHpPercent(other),
            
            // Step 6.5: 관통 배율 (Projectile 컴포넌트에서 현재 배율 읽기)
            pierceMultiplier = GetComponent<Projectile>()?.GetCurrentPierceMultiplier() ?? 1.0f,
        };
        
        var result = CombatFormula.CalculatePlayerToEnemyDamage(ctx);
        
        // ⭐ 피격 이펙트 정보 추가 (피격자가 발행할 수 있도록)
        result.hitPosition = other.transform.position;
        result.attackerGrade = GetCurrentWeaponGrade();
        
        // ⚔️ Phase 4-C: DamageResult 통째로 전달 (피격자가 면역/회복차단 처리)
        enemyHealth.TakeDamage(result, transform);
        
        // ⚙️ Phase 4-C: 공격자 측 후처리 (흡혈만 공격자가 처리)
        ApplyLifeStealOnly(result);
        
        // 🏹 관통 배율 감소: 타격 완료 후 다음 적을 위해 배율 진행
        // 순서 보장: GetCurrentPierceMultiplier(1.0) → TakeDamage → AdvancePierceMultiplier(→0.5)
        GetComponent<Projectile>()?.AdvancePierceMultiplier();
        
        if (showDebugLogs)
            Debug.Log($"💥 [DamageSource] 최종 데미지: {result.finalDamage} (크리티컬: {result.isCritical}, 백어택: {result.isBackAttack}, 스킬: {_hasSkillDamage}) → {other.name}");
    }
    
    /// <summary>
    /// 🆕 SimpleMob에 데미지 처리
    /// </summary>
    private void DealDamageToSimpleMob(SimpleMob simpleMob, Collider2D other)
    {
        // 🧱 벽 차단 체크
        if (checkWallBlocking && IsBlockedByWall(other.transform))
        {
            if (showDebugLogs)
                Debug.Log($"🚫 [DamageSource] {other.name} - 벽에 막혀서 공격 실패");
            return;
        }
        
        float baseAttack = _hasSkillDamage ? _skillDamageOverride : GetCurrentBaseDamage();
        bool isBerserkerState = EvaluateBerserkerState();
        
        // ⚔️ CombatFormula 데미지 계산
        var ctx = new CombatFormula.AttackContext
        {
            baseAttack = baseAttack,
            attackerClass = GetComponent<IPlayerClass>(),
            targetDefense = 0f, // SimpleMob은 방어력 없음
            targetTransform = other.transform,
            attackerTransform = transform,
            isSkillAttack = _hasSkillDamage,
            skillMultiplier = 1.0f,
            isBerserkerState = isBerserkerState,
            criticalChance = playerRuntimeStats != null ? playerRuntimeStats.FinalCriticalChance : 0f,
            criticalMultiplier = playerRuntimeStats != null ? playerRuntimeStats.FinalCriticalDamage : 1.5f,
            isPlayerAttack = true,
            attackerLevel = GetPlayerLevel(),
            
            // Step 5: 스탯 기반 방어구 관통률
            armorPenetration = playerRuntimeStats != null ? playerRuntimeStats.FinalArmorPenetration : 0f,
            // Phase 7: 스탯 기반 기본 흡혈률 + 오버킬 방지용 타격 전 적 현재 체력
            lifeStealPercent = playerRuntimeStats != null ? playerRuntimeStats.FinalLifeSteal : 0f,
            targetCurrentHp = Mathf.Max(0, simpleMob.CurrentHealth),
            
            // ⚙️ Phase 4: ConditionalModifier용 필드
            target = null, // SimpleMob은 IEnemyTarget 미구현
            selfHpPercent = GetPlayerHpPercent(),
            targetHpPercent = 1.0f, // SimpleMob은 HP 비율 미지원
            
            // Step 6.5: 관통 배율 (미할당 시 0f → 항상 데미지 1이 되는 버그 방지)
            pierceMultiplier = GetComponent<Projectile>()?.GetCurrentPierceMultiplier() ?? 1.0f,
        };
        
        var result = CombatFormula.CalculatePlayerToEnemyDamage(ctx);
        
        // SimpleMob에 데미지 적용 — transform 전달로 넉백 방향 계산
        simpleMob.TakeDamage(result.finalDamage, transform);
        
        // ⚙️ Phase 4-C: 공격자 측 후처리 (흡혈만)
        ApplyLifeStealOnly(result);
        
        // 🏹 관통 배율 감소: 타격 완료 후 다음 적을 위해 배율 진행
        GetComponent<Projectile>()?.AdvancePierceMultiplier();
        
        if (showDebugLogs)
            Debug.Log($"💥 [DamageSource] SimpleMob 데미지: {result.finalDamage} → {other.name}");
    }
    
    /// <summary>
    /// 🔥 Step 3 동적 플래그 평가: Warrior 버서커 모드 활성 여부
    /// CombatFormula는 IPlayerClass에 의존하지 않고 이 bool 플래그만 받는다.
    /// 새로운 동적 조건(ex. 특정 버프 발동)이 생길 경우 이 메서드에서만 추가하면 된다.
    /// </summary>
    private bool EvaluateBerserkerState()
    {
        if (warrior == null || !warrior.IsActiveClass)
            return false;
        
        return warrior.IsInBerserkerMode();
    }
    
    /// <summary>
    /// 🎯 현재 기본 데미지 가져오기 (PlayerRuntimeStats 우선)
    /// </summary>
    private float GetCurrentBaseDamage()
    {
        if (playerRuntimeStats != null)
        {
            return playerRuntimeStats.FinalAttackDamage;
        }
        
        // Fallback: 기존 방식 (PlayerRuntimeStats가 없을 때만)
        Debug.LogWarning("⚠️ [DamageSource] PlayerRuntimeStats가 없어 Fallback 방식 사용");
        
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon?.CurrentActiveWeapon != null)
        {
            var weaponComponent = activeWeapon.CurrentActiveWeapon as IWeapon;
            if (weaponComponent != null)
            {
                var equipmentData = weaponComponent.GetEquipmentData();
                return equipmentData?.attackDamage ?? 10f;
            }
        }
        
        return 10f; // 최종 기본값
    }
    
    /// <summary>
    /// 🎭 클래스별 특수 효과 적용
    /// </summary>
    private float ApplyClassSpecialEffects(float currentDamage, float baseDamage, Collider2D target)
    {
        float modifiedDamage = currentDamage;
        
        // 🔥 Warrior 특수 효과 (버서커 모드, 반격 등)
        if (warrior != null && warrior.IsActiveClass)
        {
            modifiedDamage = ApplyWarriorEffects(modifiedDamage, baseDamage);
        }
        
        // 🏹 Assasin 특수 효과 (크리티컬, 백어택 등)
        if (assasin != null && assasin.IsActiveClass)
        {
            modifiedDamage = ApplyAssasinEffects(modifiedDamage, baseDamage, target);
        }
        
        return modifiedDamage;
    }
    
    /// <summary>
    /// ⚔️ Warrior 특수 효과 적용
    /// </summary>
    private float ApplyWarriorEffects(float currentDamage, float baseDamage)
    {
        try
        {
            float warriorDamage = warrior.GetModifiedDamage(baseDamage);
            
            if (showDebugLogs)
            {
                Debug.Log($"⚔️ [DamageSource] Warrior 효과 적용: {currentDamage:F1} → {warriorDamage:F1}");
                
                if (warrior.IsInBerserkerMode())
                {
                    Debug.Log($"🔥 [DamageSource] 버서커 모드 활성! 추가 데미지 보너스");
                }
            }
            
            return warriorDamage;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"🟡 [DamageSource] Warrior 효과 적용 실패: {ex.Message}");
            return currentDamage; // 원래 값 반환
        }
    }
    
    /// <summary>
    /// 🏹 Assasin 특수 효과 적용
    /// </summary>
    private float ApplyAssasinEffects(float currentDamage, float baseDamage, Collider2D target)
    {
        try
        {
            // 기본 Assasin 데미지 계산 (크리티컬 포함)
            float assasinDamage = assasin.GetModifiedDamage(baseDamage);
            
            // 백어택 보너스 추가 계산
            float backAttackMultiplier = assasin.GetBackAttackMultiplier(
                transform.position, 
                target.transform.position, 
                target.transform.right // 적의 방향
            );
            
            float finalAssasinDamage = assasinDamage * backAttackMultiplier;
            
            if (showDebugLogs)
            {
                Debug.Log($"🏹 [DamageSource] Assasin 효과 적용:");
                Debug.Log($"   - 기본 → 크리티컬: {baseDamage:F1} → {assasinDamage:F1}");
                Debug.Log($"   - 백어택 배율: x{backAttackMultiplier:F2}");
                Debug.Log($"   - 최종 데미지: {finalAssasinDamage:F1}");
            }
            
            return finalAssasinDamage;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"🟡 [DamageSource] Assasin 효과 적용 실패: {ex.Message}");
            return currentDamage; // 원래 값 반환
        }
    }
    
    /// <summary>
    /// 🔧 디버그용: 현재 데미지 정보 출력
    /// </summary>
    [ContextMenu("Print Current Damage Info")]
    private void PrintCurrentDamageInfo()
    {
        if (playerRuntimeStats != null)
        {
            Debug.Log($"📊 [DamageSource] 현재 데미지 정보:");
            Debug.Log($"   - 최종 공격력: {playerRuntimeStats.FinalAttackDamage:F1}");
            Debug.Log($"   - 크리티컬 확률: {playerRuntimeStats.FinalCriticalChance:P1}");
            Debug.Log($"   - 크리티컬 배율: x{playerRuntimeStats.FinalCriticalDamage:F1}");
        }
        else
        {
            Debug.LogWarning("⚠️ [DamageSource] PlayerRuntimeStats를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 🔄 PlayerRuntimeStats 참조 새로고침 (런타임 중 필요시)
    /// </summary>
    public void RefreshReferences()
    {
        InitializeReferences();
    }
    
    /// <summary>
    /// 🏹 투사체형 액티브 스킬 전용: 사전 계산된 스킬 데미지를 주입
    /// SkillController에서 FinalAttackDamage × 스킬배율을 계산한 값을 전달
    /// 크리티컬/방어력 감소는 CombatFormula에서 이 값 기준으로 정상 적용됨
    /// </summary>
    public void SetSkillDamage(int preCalculatedDamage)
    {
        _skillDamageOverride = preCalculatedDamage;
        _hasSkillDamage = true;
        
        if (showDebugLogs)
            Debug.Log($"🏹 [DamageSource] 스킬 데미지 설정: {preCalculatedDamage}");
    }
    
    /// <summary>
    /// 오브젝트 풀 반환 시 스킬 데미지 오버라이드 초기화
    /// </summary>
    public void ClearSkillDamage()
    {
        _skillDamageOverride = 0;
        _hasSkillDamage = false;
    }
    
    #region ⭐ Phase 1-1: 히트 이펙트 시스템
    
    /// <summary>
    /// 현재 무기 등급 가져오기
    /// </summary>
    private ItemGrade GetCurrentWeaponGrade()
    {
        var activeWeapon = FindObjectOfType<ActiveWeapon>();
        if (activeWeapon?.CurrentActiveWeapon == null)
        {
            return ItemGrade.C; // 기본값
        }
        
        var weaponComponent = activeWeapon.CurrentActiveWeapon as IWeapon;
        if (weaponComponent == null)
        {
            return ItemGrade.C;
        }
        
        var equipmentData = weaponComponent.GetEquipmentData();
        if (equipmentData == null)
        {
            return ItemGrade.C;
        }
        
        return equipmentData.itemGrade;
    }
    
    #endregion
    
    #region 🧱 벽 충돌 시스템
    
    /// <summary>
    /// 벽이 중간에 있는지 Raycast로 체크
    /// </summary>
    private bool IsBlockedByWall(Transform target)
    {
        if (wallLayer == 0)
        {
            Debug.LogWarning("🚨 [DamageSource] wallLayer가 설정되지 않았습니다!");
            return false;
        }
        
        Vector2 origin = transform.position;
        Vector2 direction = (target.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, target.position);
        
        // 🔍 디버그: Raycast 정보
        if (showDebugLogs)
        {
            Debug.Log($"🔍 [DamageSource] Raycast 체크:");
            Debug.Log($"   - Origin: {origin}");
            Debug.Log($"   - Target: {target.position} ({target.name})");
            Debug.Log($"   - Direction: {direction}");
            Debug.Log($"   - Distance: {distance:F2}");
            Debug.Log($"   - wallLayer: {wallLayer.value}");
        }
        
        // Raycast로 벽 감지
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, wallLayer);
        
        if (hit.collider != null)
        {
            Debug.Log($"🧱 [DamageSource] 벽 감지! {hit.collider.name} (거리: {hit.distance:F2}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
            return true;
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"✅ [DamageSource] 벽 없음 - 공격 가능");
            return false;
        }
    }
    
    #endregion
    
    #region ⚔️ 전투 공식 연동
    
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
            float defense = baseEnemy.GetScaledDefense();
            if (showDebugLogs)
                Debug.Log($"🛡️ [DamageSource] {target.name} 방어력: {defense:F1}");
            return defense;
        }
        
        return 0f;
    }
    
    /// <summary>
    /// 플레이어 레벨 가져오기 (Dynamic K 계산용)
    /// </summary>
    private int GetPlayerLevel()
    {
        // PlayerRuntimeStats를 통해 레벨 가져오기 (일관성 있는 데이터 소스)
        if (playerRuntimeStats != null)
        {
            return playerRuntimeStats.CurrentLevel;
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
        var playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();
        
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
        
        var playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();
        
        if (playerHealth != null)
        {
            int healAmount = Mathf.RoundToInt(amount);
            playerHealth.HealPlayerAmount(healAmount);
            
            // 흡혈 회복 숫자 표시: 플레이어 머리 위에 표시 (+N, 초록색)
            if (DamageNumberManager.Instance != null)
                DamageNumberManager.Instance.ShowHealNumber(playerHealth.transform.position, healAmount, playerHealth.transform);
            
            if (showDebugLogs)
                Debug.Log($"💚 [DamageSource] 흡혈: {healAmount} HP 회복");
        }
    }
    
    #endregion
}
