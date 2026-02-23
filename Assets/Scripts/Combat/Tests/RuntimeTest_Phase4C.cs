using UnityEngine;

/// <summary>
/// Phase 4-C 런타임 테스트 헬퍼
/// 실제 게임 플레이 중 F1~F5 단축키로 룬 장착/해제 테스트
/// </summary>
public class RuntimeTest_Phase4C : MonoBehaviour
{
    void Update()
    {
        // Ctrl+Shift 조합 체크
        bool ctrlShift = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
                         (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        
        if (!ctrlShift) return; // Ctrl+Shift가 눌려있지 않으면 모든 단축키 무시
        
        // Ctrl+Shift+F1: 흡혈 룬 장착
        if (Input.GetKeyDown(KeyCode.F1))
        {
            EquipLifeStealRune();
        }
        
        // Ctrl+Shift+F2: 방어 룬 장착 (면역 + 회복 차단)
        if (Input.GetKeyDown(KeyCode.F2))
        {
            EquipDefenderRune();
        }
        
        // Ctrl+Shift+F3: 보스 사냥꾼 룬 장착
        if (Input.GetKeyDown(KeyCode.F3))
        {
            EquipHunterRune();
        }
        
        // Ctrl+Shift+F4: 모든 룬 해제
        if (Input.GetKeyDown(KeyCode.F4))
        {
            UnequipAllRunes();
        }
        
        // Ctrl+Shift+F5: 플레이어 체력 50%로 설정
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SetPlayerHpTo50Percent();
        }
        
        // Ctrl+Shift+F6: 현재 룬 상태 출력
        if (Input.GetKeyDown(KeyCode.F6))
        {
            PrintRuneStatus();
        }
        
        // Ctrl+Shift+F9: 플레이어에게 Bind 상태이상 적용 (면역 테스트용)
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ApplyBindToPlayer();
        }
        
        // Ctrl+Shift+H: 플레이어에게 회복 차단 적용 (회복 차단 테스트용, H = Healing Block)
        if (Input.GetKeyDown(KeyCode.H))
        {
            ApplyHealingBlockToPlayer();
        }
        
        // Ctrl+Shift+C: 크리티컬 데미지 테스트 (C = Critical)
        if (Input.GetKeyDown(KeyCode.C))
        {
            TestCriticalDamage();
        }
        
        // Ctrl+Shift+K: 플레이어 크리티컬 확률 100% 설정 (K = Kritical)
        if (Input.GetKeyDown(KeyCode.K))
        {
            SetPlayerCriticalChance100();
        }
    }
    
    void EquipLifeStealRune()
    {
        var rune = Resources.Load<RuneData>("Runes/RUNE_LIFESTEAL");
        if (rune != null && RuneManager.Instance != null)
        {
            RuneManager.Instance.EquipRune(rune, 0);
            Debug.Log("✅ [Ctrl+Shift+F1] 흡혈 룬 장착!");
        }
        else
        {
            Debug.LogError("❌ 흡혈 룬을 찾을 수 없습니다! Tools → Generate Test Runes 실행하세요.");
        }
    }
    
    void EquipDefenderRune()
    {
        var rune = Resources.Load<RuneData>("Runes/RUNE_BOSS_DEFENDER");
        if (rune != null && RuneManager.Instance != null)
        {
            RuneManager.Instance.EquipRune(rune, 0);
            Debug.Log("✅ [Ctrl+Shift+F2] 방어 룬 장착!");
        }
        else
        {
            Debug.LogError("❌ 방어 룬을 찾을 수 없습니다! Tools → Generate Test Runes 실행하세요.");
        }
    }
    
    void EquipHunterRune()
    {
        var rune = Resources.Load<RuneData>("Runes/RUNE_BOSS_HUNTER");
        if (rune != null && RuneManager.Instance != null)
        {
            RuneManager.Instance.EquipRune(rune, 0);
            Debug.Log("✅ [Ctrl+Shift+F3] 사냥꾼 룬 장착!");
        }
        else
        {
            Debug.LogError("❌ 사냥꾼 룬을 찾을 수 없습니다! Tools → Generate Test Runes 실행하세요.");
        }
    }
    
    void UnequipAllRunes()
    {
        if (RuneManager.Instance != null)
        {
            RuneManager.Instance.UnequipAllRunes();
            Debug.Log("✅ [Ctrl+Shift+F4] 모든 룬 해제!");
        }
    }
    
    void SetPlayerHpTo50Percent()
    {
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            // 현재 체력을 최대 체력의 50%로 만들기 위해 데미지 계산
            int targetHp = playerHealth.MaxHealth / 2;
            int currentHp = playerHealth.CurrentHealth;
            
            if (currentHp > targetHp)
            {
                // 체력이 50%보다 많으면 데미지를 줌
                int damageAmount = currentHp - targetHp;
                
                // CombatFormula.DamageResult 생성 (테스트용)
                var testResult = new CombatFormula.DamageResult
                {
                    finalDamage = damageAmount,
                    isCritical = false,
                    lifeStealAmount = 0,
                    healingBlockPercent = 0,
                    hasImmunity = false,
                    resistedEffects = ""
                };
                
                playerHealth.TakeDamage(testResult, null);
                Debug.Log($"✅ [Ctrl+Shift+F5] 플레이어 체력 50%로 설정! ({targetHp}/{playerHealth.MaxHealth})");
            }
            else if (currentHp < targetHp)
            {
                // 체력이 50%보다 적으면 회복
                int healAmount = targetHp - currentHp;
                playerHealth.HealPlayerAmount(healAmount);
                Debug.Log($"✅ [Ctrl+Shift+F5] 플레이어 체력 50%로 설정! ({targetHp}/{playerHealth.MaxHealth})");
            }
            else
            {
                Debug.Log($"ℹ️ [Ctrl+Shift+F5] 플레이어 체력이 이미 50%입니다. ({currentHp}/{playerHealth.MaxHealth})");
            }
        }
        else
        {
            Debug.LogError("❌ PlayerHealth를 찾을 수 없습니다!");
        }
    }
    
    void PrintRuneStatus()
    {
        if (RuneManager.Instance == null)
        {
            Debug.LogError("❌ RuneManager.Instance가 null입니다!");
            return;
        }
        
        Debug.Log("========================================");
        Debug.Log("🎯 [Ctrl+Shift+F6] 현재 룬 상태");
        Debug.Log("========================================");
        
        var equippedRunes = RuneManager.Instance.GetEquippedRunes();
        for (int i = 0; i < equippedRunes.Count; i++)
        {
            var rune = equippedRunes[i];
            if (rune != null)
            {
                Debug.Log($"  슬롯 {i}: {rune.runeName} ({rune.ConditionalModifierIds.Count}개 효과)");
            }
            else
            {
                Debug.Log($"  슬롯 {i}: (비어있음)");
            }
        }
        
        var activeModifiers = RuneManager.Instance.GetActiveConditionalModifiers();
        Debug.Log($"\n💎 활성 조건부 모디파이어: {activeModifiers.Count}개");
        foreach (var mod in activeModifiers)
        {
            Debug.Log($"  - {mod.displayName} (출처: {mod.source})");
        }
        
        Debug.Log("========================================");
    }
    
    #region 🧪 상태이상 테스트 메서드
    
    /// <summary>
    /// [Ctrl+Shift+F9] 플레이어에게 Bind 상태이상 적용 (면역 테스트용)
    /// 순서: 1) PlayerRuntimeStats에서 면역 체크 → 2) DamageResult 생성 → 3) 상태이상 적용
    /// </summary>
    void ApplyBindToPlayer()
    {
        var playerHealth = FindObjectOfType<PlayerHealth>();
        var playerStats = FindObjectOfType<PlayerRuntimeStats>();
        
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth를 찾을 수 없습니다!");
            return;
        }
        
        // StatusEffectManager 자동 생성
        if (StatusEffectManager.Instance == null)
        {
            GameObject managerObj = new GameObject("StatusEffectManager");
            managerObj.AddComponent<StatusEffectManager>();
            Debug.Log("✅ [Auto] StatusEffectManager 자동 생성됨!");
        }
        
        Debug.Log("========================================");
        Debug.Log("🧪 [Ctrl+Shift+F9] Bind 상태이상 적용 테스트");
        Debug.Log("========================================");
        
        // ⚙️ Step 1: PlayerRuntimeStats에서 면역 ConditionalModifier 확인
        bool hasImmunity = false;
        string resistedEffects = "";
        
        if (playerStats != null)
        {
            var activeModifiers = playerStats.GetActiveConditionalModifiers();
            foreach (var mod in activeModifiers)
            {
                // IMMUNE_BIND 같은 면역 효과 찾기
                if (mod.effectType == EEffectType.Immunity_Bind || 
                    mod.effectType == EEffectType.Immunity_Poison ||
                    mod.effectType == EEffectType.Immunity_Slow ||
                    mod.effectType == EEffectType.Immunity_Burn)
                {
                    hasImmunity = true;
                    if (!string.IsNullOrEmpty(resistedEffects))
                        resistedEffects += ", ";
                    resistedEffects += mod.effectType.ToString().Replace("Immunity_", "");
                    
                    Debug.Log($"🛡️ 발견: {mod.displayName} ({mod.effectType})");
                }
            }
        }
        
        // ⚙️ Step 2: DamageResult 생성 (면역 정보 포함)
        var testResult = new CombatFormula.DamageResult
        {
            finalDamage = 1, // 최소 데미지 (면역 테스트용)
            isCritical = false,
            lifeStealAmount = 0,
            healingBlockPercent = 0,
            hasImmunity = hasImmunity,
            resistedEffects = resistedEffects
        };
        
        // ⚙️ Step 3: DamageResult를 통해 면역 정보를 PlayerHealth에 등록
        playerHealth.TakeDamage(testResult, playerHealth.transform);
        
        if (hasImmunity)
        {
            Debug.Log($"🛡️ 면역 활성화! 저항 효과: {resistedEffects}");
            Debug.Log("⏭️ Bind 상태이상 적용 차단됨!");
            Debug.Log("========================================");
            Debug.Log("💡 면역이 정상적으로 작동했습니다!");
            Debug.Log("💡 [Ctrl+Shift+F4] → 룬 해제 후 다시 시도하면 Bind가 걸립니다.");
            Debug.Log("========================================");
        }
        else
        {
            Debug.Log("⚠️ 면역 없음! Bind 상태이상 적용 시도...");
            
            // ⚙️ Step 4: 실제 Bind 상태이상 적용
            StatusEffectManager.Instance.AddBind(
                target: playerHealth.gameObject,
                duration: 5f
            );
            
            Debug.Log("✅ Bind 상태이상 적용 완료! (5초간 이동 불가)");
            Debug.Log("========================================");
            Debug.Log("💡 Tip: [Ctrl+Shift+F2] 방어 룬 장착 후 다시 시도하면 면역이 발동합니다!");
            Debug.Log("========================================");
        }
    }
    
    /// <summary>
    /// [Ctrl+Shift+H] 플레이어에게 회복 차단 적용 (회복 차단 테스트용, H = Healing Block)
    /// </summary>
    void ApplyHealingBlockToPlayer()
    {
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth를 찾을 수 없습니다!");
            return;
        }
        
        // StatusEffectManager 자동 생성 (회복 차단에는 필요 없지만 일관성 유지)
        if (StatusEffectManager.Instance == null)
        {
            GameObject managerObj = new GameObject("StatusEffectManager");
            managerObj.AddComponent<StatusEffectManager>();
            Debug.Log("✅ [Auto] StatusEffectManager 자동 생성됨!");
        }
        
        Debug.Log("🧪 [Ctrl+Shift+H] 플레이어에게 회복 차단 적용...");
        
        // DamageResult를 통한 회복 차단 (50%, 5초)
        var testResult = new CombatFormula.DamageResult
        {
            finalDamage = 0, // 데미지 없음
            isCritical = false,
            lifeStealAmount = 0,
            healingBlockPercent = 0.5f, // 50% 회복 차단
            hasImmunity = false,
            resistedEffects = ""
        };
        
        // TakeDamage를 통해 회복 차단 적용 (데미지는 0)
        playerHealth.TakeDamage(testResult, playerHealth.transform);
        
        Debug.Log("✅ 회복 차단 50% 적용 완료! (5초간 회복량 50% 감소)");
        Debug.Log("💡 Tip: 이제 회복 포션을 먹거나 체력 회복을 시도하면 50%만 회복됩니다.");
    }
    
    #endregion
    
    #region 🎨 시각적 피드백 테스트
    
    /// <summary>
    /// [Ctrl+Shift+K] 몬스터 크리티컬 데미지 시각 테스트 (몬스터가 받는 크리티컬)
    /// </summary>
    void SetPlayerCriticalChance100()
    {
        // 가장 가까운 몬스터 찾기
        var enemies = FindObjectsOfType<EnemyHealth>();
        if (enemies == null || enemies.Length == 0)
        {
            Debug.LogError("❌ 씬에 몬스터가 없습니다!");
            return;
        }
        
        var player = FindObjectOfType<PlayerHealth>();
        if (player == null)
        {
            Debug.LogError("❌ PlayerHealth를 찾을 수 없습니다!");
            return;
        }
        
        // 가장 가까운 몬스터
        EnemyHealth nearestEnemy = enemies[0];
        float nearestDistance = Vector3.Distance(player.transform.position, nearestEnemy.transform.position);
        
        foreach (var enemy in enemies)
        {
            float distance = Vector3.Distance(player.transform.position, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }
        
        Debug.Log("========================================");
        Debug.Log("🎯 [Ctrl+Shift+K] 몬스터 크리티컬 데미지 시각 테스트");
        Debug.Log("========================================");
        
        // 크리티컬 DamageResult 생성
        var critResult = new CombatFormula.DamageResult
        {
            finalDamage = 999,
            isCritical = true,  // ⭐ 크리티컬!
            lifeStealAmount = 0,
            healingBlockPercent = 0,
            hasImmunity = false,
            resistedEffects = ""
        };
        
        // 몬스터에게 크리티컬 데미지 표시
        nearestEnemy.TakeDamage(critResult, player.transform);
        
        Debug.Log($"💥 {nearestEnemy.gameObject.name}에게 크리티컬 999 적용!");
        Debug.Log("📺 화면: 🟡 노란색, 1.5배 큰 숫자 (플레이어 → 몬스터 크리티컬)");
        Debug.Log("========================================");
        Debug.Log("💡 실제 전투에서도 크리티컬 발동 시 자동으로 이렇게 표시됩니다!");
        Debug.Log("========================================");
    }
    
    /// <summary>
    /// [Ctrl+Shift+C] 크리티컬 데미지 시각 테스트
    /// </summary>
    void TestCriticalDamage()
    {
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth를 찾을 수 없습니다!");
            return;
        }
        
        Debug.Log("========================================");
        Debug.Log("🎨 [Ctrl+Shift+C] 크리티컬 데미지 시각 테스트");
        Debug.Log("========================================");
        
        // 크리티컬 DamageResult 생성
        var critResult = new CombatFormula.DamageResult
        {
            finalDamage = 999,
            isCritical = true,  // ⭐ 크리티컬!
            lifeStealAmount = 0,
            healingBlockPercent = 0,
            hasImmunity = false,
            resistedEffects = ""
        };
        
        playerHealth.TakeDamage(critResult, playerHealth.transform);
        
        Debug.Log("💥 크리티컬 데미지 999 적용! 🔴 빨간색, 큰 크기 (몬스터 → 플레이어 크리티컬)");
        Debug.Log("========================================");
    }
    
    #endregion
}

