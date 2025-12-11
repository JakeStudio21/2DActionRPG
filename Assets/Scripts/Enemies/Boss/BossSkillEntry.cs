using UnityEngine;

/// <summary>
/// 보스 스킬 거리 분류
/// </summary>
public enum BossSkillDistance
{
    Melee,    // 근거리 (공격범위 안)
    Ranged,   // 원거리 (공격범위 밖)
    Both      // 거리 무관
}

/// <summary>
/// 보스 스킬 엔트리 - 페이즈별 스킬 설정
/// </summary>
[System.Serializable]
public class BossSkillEntry
{
    [Header("⚡ 스킬 정보")]
    [Tooltip("스킬 데이터 ScriptableObject")]
    public SkillData skillData;
    
    [Header("🎲 확률 설정")]
    [Tooltip("이 스킬의 확률 가중치 (예: 15)")]
    [Range(0f, 100f)]
    public float weight = 15f;
    
    [Header("📏 거리 분류")]
    [Tooltip("이 스킬이 사용되는 거리 조건")]
    public BossSkillDistance distanceType = BossSkillDistance.Both;
    
    [Header("⏱️ 쿨다운")]
    [Tooltip("이 스킬의 개별 쿨다운 (초)")]
    [Range(0f, 30f)]
    public float individualCooldown = 5f;
    
    [Header("📊 페이즈별 스킬 변화")]
    [Tooltip("스킬 스케일 배율 (속도, 범위 등)")]
    [Range(0.5f, 3f)]
    public float skillScaleMultiplier = 1f;
    
    [Header("🎮 디버그")]
    [TextArea(2, 3)]
    public string skillDescription = "";
    
    /// <summary>
    /// 스킬이 유효한지 확인
    /// </summary>
    public bool IsValid()
    {
        return skillData != null && weight > 0;
    }
    
    /// <summary>
    /// 디버그 정보
    /// </summary>
    public string GetDebugInfo()
    {
        string info = $"Skill: {skillData?.SkillName ?? "NULL"}\n";
        info += $"Weight: {weight}, Distance: {distanceType}\n";
        info += $"Cooldown: {individualCooldown}s, Scale: {skillScaleMultiplier}x\n";
        return info;
    }
}


