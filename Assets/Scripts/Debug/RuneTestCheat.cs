using UnityEngine;

/// <summary>
/// ⚙️ Phase 4-C: 룬 시스템 테스트용 치트 스크립트
/// F1~F5 단축키로 빠르게 룬 장착/해제 및 테스트 설정 가능
/// </summary>
public class RuneTestCheat : MonoBehaviour
{
    [Header("디버그 옵션")]
    [SerializeField] private bool enableCheats = true;
    [SerializeField] private bool showCheatLogs = true;

    private void Update()
    {
        if (!enableCheats) return;

        // F1: 흡혈 룬 장착 (예시 1 테스트용)
        if (Input.GetKeyDown(KeyCode.F1))
        {
            EquipLifeStealRune();
        }
        
        // F2: 방어 룬 장착 (예시 2, 3 테스트용)
        if (Input.GetKeyDown(KeyCode.F2))
        {
            EquipDefenderRune();
        }
        
        // F3: 보스 사냥꾼 룬 장착
        if (Input.GetKeyDown(KeyCode.F3))
        {
            EquipHunterRune();
        }
        
        // F4: 모든 룬 해제
        if (Input.GetKeyDown(KeyCode.F4))
        {
            UnequipAllRunes();
        }
        
        // F5: 플레이어 체력 50%로 설정
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SetPlayerHpTo50Percent();
        }

        // F6: 플레이어 체력 30%로 설정 (LOW_HP_DR 테스트용)
        if (Input.GetKeyDown(KeyCode.F6))
        {
            SetPlayerHpTo30Percent();
        }

        // F7: 플레이어 완전 회복
        if (Input.GetKeyDown(KeyCode.F7))
        {
            HealPlayerFull();
        }

        // F8: 룬 상태 출력
        if (Input.GetKeyDown(KeyCode.F8))
        {
            PrintRuneStatus();
        }
    }

    #region 룬 장착/해제
    
    private void EquipLifeStealRune()
    {
        if (RuneManager.Instance == null)
        {
            Debug.LogError("❌ RuneManager.Instance가 null입니다! 씬에 RuneManager를 추가하세요.");
            return;
        }

        var rune = Resources.Load<RuneData>("Runes/RUNE_LIFESTEAL");
        if (rune == null)
        {
            Debug.LogError("❌ RUNE_LIFESTEAL을 찾을 수 없습니다! Tools → Generate Test Runes 실행이 필요합니다.");
            return;
        }

        RuneManager.Instance.EquipRune(rune, 0);
        if (showCheatLogs)
            Debug.Log("✅ [F1] 흡혈 룬 장착! (BOSS_DOT_LIFESTEAL) - 보스 공격 시 2% 흡혈");
    }
    
    private void EquipDefenderRune()
    {
        if (RuneManager.Instance == null)
        {
            Debug.LogError("❌ RuneManager.Instance가 null입니다!");
            return;
        }

        var rune = Resources.Load<RuneData>("Runes/RUNE_BOSS_DEFENDER");
        if (rune == null)
        {
            Debug.LogError("❌ RUNE_BOSS_DEFENDER를 찾을 수 없습니다!");
            return;
        }

        RuneManager.Instance.EquipRune(rune, 0);
        if (showCheatLogs)
            Debug.Log("✅ [F2] 방어 룬 장착! (IMMUNE_BIND, BOSS_BLOCK_HEAL) - 보스 속박 면역 + 회복 차단");
    }
    
    private void EquipHunterRune()
    {
        if (RuneManager.Instance == null)
        {
            Debug.LogError("❌ RuneManager.Instance가 null입니다!");
            return;
        }

        var rune = Resources.Load<RuneData>("Runes/RUNE_BOSS_HUNTER");
        if (rune == null)
        {
            Debug.LogError("❌ RUNE_BOSS_HUNTER를 찾을 수 없습니다!");
            return;
        }

        RuneManager.Instance.EquipRune(rune, 0);
        if (showCheatLogs)
            Debug.Log("✅ [F3] 사냥꾼 룬 장착! (BOSS_DMG_UP, BOSS_HP_HIGH_BONUS) - 보스 피해 +20%, HP 50% 이상 +25%");
    }
    
    private void UnequipAllRunes()
    {
        if (RuneManager.Instance == null)
        {
            Debug.LogError("❌ RuneManager.Instance가 null입니다!");
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            RuneManager.Instance.UnequipRune(i);
        }
        if (showCheatLogs)
            Debug.Log("✅ [F4] 모든 룬 해제!");
    }
    
    #endregion

    #region 플레이어 체력 조작
    
    private void SetPlayerHpTo50Percent()
    {
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth를 찾을 수 없습니다!");
            return;
        }

        playerHealth.DEBUG_SetHealthPercent(0.5f);
        
        if (showCheatLogs)
            Debug.Log($"✅ [F5] 플레이어 체력 50%로 설정! ({playerHealth.CurrentHealth}/{playerHealth.MaxHealth})");
    }

    private void SetPlayerHpTo30Percent()
    {
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth를 찾을 수 없습니다!");
            return;
        }

        playerHealth.DEBUG_SetHealthPercent(0.3f);
        
        if (showCheatLogs)
            Debug.Log($"✅ [F6] 플레이어 체력 30%로 설정! ({playerHealth.CurrentHealth}/{playerHealth.MaxHealth}) - LOW_HP_DR 테스트용");
    }

    private void HealPlayerFull()
    {
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth를 찾을 수 없습니다!");
            return;
        }

        playerHealth.DEBUG_HealFull();
        
        if (showCheatLogs)
            Debug.Log($"✅ [F7] 플레이어 완전 회복! ({playerHealth.CurrentHealth}/{playerHealth.MaxHealth})");
    }

    #endregion

    #region 상태 출력
    
    private void PrintRuneStatus()
    {
        if (RuneManager.Instance == null)
        {
            Debug.LogError("❌ RuneManager.Instance가 null입니다!");
            return;
        }

        Debug.Log("=== [F8] 현재 룬 장착 상태 ===");
        
        var equippedRunes = RuneManager.Instance.GetEquippedRunes(); // 수정: GetAllEquippedRunes → GetEquippedRunes
        bool hasRune = false;
        
        for (int i = 0; i < equippedRunes.Count; i++) // Length → Count
        {
            if (equippedRunes[i] != null)
            {
                hasRune = true;
                Debug.Log($"  슬롯 {i}: {equippedRunes[i].runeName} ({equippedRunes[i].runeId})");
                Debug.Log($"    → 효과: {string.Join(", ", equippedRunes[i].ConditionalModifierIds)}"); // conditionalModifierIds → ConditionalModifierIds
            }
        }
        
        if (!hasRune)
        {
            Debug.Log("  (장착된 룬 없음)");
        }

        // PlayerRuntimeStats 상태 출력
        var playerRuntimeStats = FindObjectOfType<PlayerRuntimeStats>();
        if (playerRuntimeStats != null)
        {
            var activeModifiers = playerRuntimeStats.GetActiveConditionalModifiers();
            Debug.Log($"  활성 조건부 모디파이어: {activeModifiers.Count}개");
        }

        // 플레이어 체력 출력
        var playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth != null)
        {
            float hpPercent = (float)playerHealth.CurrentHealth / playerHealth.MaxHealth * 100f;
            Debug.Log($"  플레이어 체력: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth} ({hpPercent:F0}%)");
        }

        Debug.Log("==============================");
    }

    #endregion

    #region 도움말 표시 (게임 시작 시 자동)
    
    private void Start()
    {
        if (enableCheats && showCheatLogs)
        {
            Debug.Log("=== 🎮 룬 테스트 치트 활성화 ===");
            Debug.Log("  F1: 흡혈 룬 장착 (예시 1 테스트)");
            Debug.Log("  F2: 방어 룬 장착 (예시 2, 3 테스트)");
            Debug.Log("  F3: 사냥꾼 룬 장착");
            Debug.Log("  F4: 모든 룬 해제");
            Debug.Log("  F5: 플레이어 체력 50%");
            Debug.Log("  F6: 플레이어 체력 30%");
            Debug.Log("  F7: 플레이어 완전 회복");
            Debug.Log("  F8: 룬 상태 출력");
            Debug.Log("=================================");
        }
    }

    #endregion
}

