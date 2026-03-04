using UnityEngine;

/// <summary>
/// 🎁 Phase 2: 보스 처치 보상 데이터
/// 보스별로 지급할 상태이상 저항 보상을 정의
/// </summary>
[CreateAssetMenu(fileName = "BossReward", menuName = "Data/Boss Reward", order = 52)]
public class BossRewardData : ScriptableObject
{
    [Header("📌 보스 정보")]
    [Tooltip("보스 고유 ID (예: BOSS_SAND_ELEMENTAL_001)")]
    public string bossId;
    
    [Tooltip("보스 이름 (UI 표시용)")]
    public string bossName = "Sand Elemental";
    
    [Header("🎁 보상 정보")]
    [Tooltip("보상할 저항 타입")]
    public EStatusEffectType rewardType = EStatusEffectType.Poison;
    
    [Tooltip("보상 저항력 (0.0 ~ 1.0, 예: 0.3 = 30%)")]
    [Range(0f, 1.0f)]
    public float rewardAmount = 0.3f;
    
    [Tooltip("보상 설명 (UI용)")]
    [TextArea(2, 4)]
    public string rewardDescription = "독 저항 30% 획득!";
    
    [Header("🎨 UI 관련 (선택적)")]
    [Tooltip("보상 아이콘 (UI에 표시할 이미지)")]
    public Sprite rewardIcon;
    
    /// <summary>
    /// 유효성 검증
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(bossId))
        {
            Debug.LogError($"[BossRewardData] {name}: bossId가 비어있습니다!");
            return false;
        }
        
        if (rewardAmount <= 0f || rewardAmount > 1.0f)
        {
            Debug.LogError($"[BossRewardData] {name}: rewardAmount가 유효 범위를 벗어남: {rewardAmount} (0.0 ~ 1.0만 가능)");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Inspector에서 값 변경 시 자동 검증
    /// </summary>
    private void OnValidate()
    {
        // bossId 자동 생성 (비어있을 경우)
        if (string.IsNullOrEmpty(bossId) && !string.IsNullOrEmpty(bossName))
        {
            bossId = $"BOSS_{bossName.ToUpper().Replace(" ", "_")}_001";
        }
        
        // rewardAmount 클램프
        rewardAmount = Mathf.Clamp(rewardAmount, 0f, 1.0f);
        
        // rewardDescription 자동 생성 (비어있을 경우)
        if (string.IsNullOrEmpty(rewardDescription))
        {
            string effectName = rewardType.ToString();
            rewardDescription = $"{effectName} 저항 {rewardAmount * 100:F0}% 획득!";
        }
    }
}

