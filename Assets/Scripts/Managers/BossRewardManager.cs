using UnityEngine;

/// <summary>
/// 🎁 Phase 2: 보스 처치 보상 시스템
/// 보스 최초 클리어 시 상태이상 저항력을 보상으로 지급
/// </summary>
public class BossRewardManager : Singleton<BossRewardManager>
{
    [Header("디버그")]
    [SerializeField] private bool enableDebugLogs = false;
    
    /// <summary>
    /// 🎁 보스 최초 클리어 보상 지급
    /// </summary>
    /// <param name="bossId">보스 고유 ID (예: BOSS_SAND_ELEMENTAL_001)</param>
    /// <param name="rewardType">보상할 저항 타입</param>
    /// <param name="amount">보상 저항력 (0.0 ~ 1.0, 예: 0.3 = 30%)</param>
    public void GrantBossFirstClearReward(string bossId, EStatusEffectType rewardType, float amount)
    {
        if (string.IsNullOrEmpty(bossId))
        {
            Debug.LogError("[BossRewardManager] bossId가 비어있습니다!");
            return;
        }
        
        if (amount <= 0f || amount > 1.0f)
        {
            Debug.LogWarning($"[BossRewardManager] 보상 저항력이 유효 범위를 벗어남: {amount} (0.0 ~ 1.0만 가능)");
            return;
        }
        
        // 1. PlayerData 접근
        var playerData = PlayerDataManager.Instance?.selectedPlayerData;
        if (playerData == null)
        {
            Debug.LogError("[BossRewardManager] selectedPlayerData가 null입니다!");
            return;
        }
        
        // 2. 이미 클리어한 보스인지 확인
        if (playerData.clearedBossIds.Contains(bossId))
        {
            if (enableDebugLogs)
                Debug.Log($"[BossRewardManager] 이미 클리어한 보스입니다: {bossId} (보상 지급 안 함)");
            return;
        }
        
        // 3. PlayerResistanceStats 찾기
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[BossRewardManager] Player를 찾을 수 없습니다!");
            return;
        }
        
        var resistanceStats = player.GetComponent<PlayerResistanceStats>();
        if (resistanceStats == null)
        {
            Debug.LogError("[BossRewardManager] PlayerResistanceStats 컴포넌트를 찾을 수 없습니다!");
            return;
        }
        
        // 4. 저항력 추가
        resistanceStats.AddResistance(rewardType, amount);
        
        // 5. clearedBossIds에 보스 ID 추가
        playerData.clearedBossIds.Add(bossId);
        
        // 6. 저항 데이터 저장
        resistanceStats.SaveToPlayerData();
        
        // 7. 보상 로그
        Debug.Log($"🎉 [BossRewardManager] 최초 클리어 보상 지급!");
        Debug.Log($"   - 보스: {bossId}");
        Debug.Log($"   - 저항 타입: {rewardType}");
        Debug.Log($"   - 저항력: {amount * 100:F0}%");
        Debug.Log($"   - 현재 저항: {resistanceStats.GetResistance(rewardType) * 100:F0}%");
    }
    
    /// <summary>
    /// 🔍 보스 클리어 여부 확인
    /// </summary>
    /// <param name="bossId">보스 고유 ID</param>
    /// <returns>true면 이미 클리어, false면 미클리어</returns>
    public bool IsBossCleared(string bossId)
    {
        var playerData = PlayerDataManager.Instance?.selectedPlayerData;
        if (playerData == null) return false;
        
        return playerData.clearedBossIds.Contains(bossId);
    }
    
    /// <summary>
    /// 🔍 클리어한 보스 목록 가져오기
    /// </summary>
    public System.Collections.Generic.List<string> GetClearedBossIds()
    {
        var playerData = PlayerDataManager.Instance?.selectedPlayerData;
        if (playerData == null) return new System.Collections.Generic.List<string>();
        
        return new System.Collections.Generic.List<string>(playerData.clearedBossIds);
    }
    
    /// <summary>
    /// 🔍 클리어한 보스 수 가져오기
    /// </summary>
    public int GetClearedBossCount()
    {
        var playerData = PlayerDataManager.Instance?.selectedPlayerData;
        if (playerData == null) return 0;
        
        return playerData.clearedBossIds.Count;
    }
}

