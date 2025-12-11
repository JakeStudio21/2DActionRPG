using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 페이즈 데이터 ScriptableObject
/// 페이즈별 공격 비율, 사용 가능한 스킬, 전환 연출 설정
/// </summary>
[CreateAssetMenu(fileName = "BossPhaseData", menuName = "RPG/Boss/Phase Data")]
public class BossPhaseData : ScriptableObject
{
    [Header("⭐ 페이즈 기본 정보")]
    [Tooltip("페이즈 이름 (예: Phase 1, Phase 2)")]
    public string phaseName = "Phase 1";
    
    [Header("📊 HP 임계값")]
    [Tooltip("이 페이즈의 최소 HP % (0~1)")]
    [Range(0f, 1f)]
    public float hpThresholdMin = 0.7f; // 70%
    
    [Tooltip("이 페이즈의 최대 HP % (0~1)")]
    [Range(0f, 1f)]
    public float hpThresholdMax = 1.0f; // 100%
    
    [Header("⚔️ 공격 비율")]
    [Tooltip("평타 확률 가중치 (예: 70)")]
    [Range(0f, 100f)]
    public float meleeAttackWeight = 70f;
    
    [Tooltip("사용 가능한 스킬 목록 + 가중치")]
    public List<BossSkillEntry> availableSkills = new List<BossSkillEntry>();
    
    [Header("🔄 페이즈 전환 연출")]
    [Tooltip("페이즈 전환 애니메이션 있는지")]
    public bool hasTransitionAnimation = true;
    
    [Tooltip("전환 중 무적 시간 (초)")]
    [Range(0f, 3f)]
    public float transitionInvincibleTime = 1f;
    
    [Tooltip("전환 시 모든 쿨다운 리셋")]
    public bool resetCooldownsOnTransition = true;
    
    [Tooltip("전환 시 재생할 이펙트")]
    public GameObject transitionEffect;
    
    [Tooltip("전환 시 재생할 사운드")]
    public AudioClip transitionSound;
    
    [Header("🎮 디버그")]
    [TextArea(3, 5)]
    public string phaseDescription = "페이즈 설명을 입력하세요.";
    
    /// <summary>
    /// 총 가중치 계산 (평타 + 모든 스킬)
    /// </summary>
    public float GetTotalWeight()
    {
        float total = meleeAttackWeight;
        
        foreach (var skill in availableSkills)
        {
            total += skill.weight;
        }
        
        return total;
    }
    
    /// <summary>
    /// 거리별 스킬 필터링
    /// </summary>
    public List<BossSkillEntry> GetSkillsByDistance(BossSkillDistance distanceType)
    {
        List<BossSkillEntry> filtered = new List<BossSkillEntry>();
        
        foreach (var skill in availableSkills)
        {
            if (skill.distanceType == distanceType || skill.distanceType == BossSkillDistance.Both)
            {
                filtered.Add(skill);
            }
        }
        
        return filtered;
    }
    
    /// <summary>
    /// HP가 이 페이즈 범위 내인지 확인
    /// </summary>
    public bool IsInHpRange(float currentHpPercent)
    {
        return currentHpPercent >= hpThresholdMin && currentHpPercent <= hpThresholdMax;
    }
    
    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public string GetDebugInfo()
    {
        string info = $"=== {phaseName} ===\n";
        info += $"HP Range: {hpThresholdMin * 100}% ~ {hpThresholdMax * 100}%\n";
        info += $"Melee Weight: {meleeAttackWeight}\n";
        info += $"Available Skills: {availableSkills.Count}\n";
        
        foreach (var skill in availableSkills)
        {
            info += $"  - {skill.skillData?.SkillName ?? "NULL"}: Weight {skill.weight}, {skill.distanceType}\n";
        }
        
        info += $"Total Weight: {GetTotalWeight()}\n";
        info += $"Transition: {hasTransitionAnimation} (무적: {transitionInvincibleTime}초)\n";
        
        return info;
    }
    
    private void OnValidate()
    {
        // HP 임계값 검증
        if (hpThresholdMin > hpThresholdMax)
        {
            Debug.LogWarning($"[{name}] hpThresholdMin이 hpThresholdMax보다 큽니다! 자동 보정합니다.");
            float temp = hpThresholdMin;
            hpThresholdMin = hpThresholdMax;
            hpThresholdMax = temp;
        }
        
        // 가중치 검증
        float totalWeight = GetTotalWeight();
        if (totalWeight <= 0)
        {
            Debug.LogWarning($"[{name}] 총 가중치가 0 이하입니다! 공격을 선택할 수 없습니다.");
        }
    }
}


