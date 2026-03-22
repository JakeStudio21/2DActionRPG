using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어가 보유한 스킬의 런타임 상태
/// SO 데이터 + 현재 레벨 + 장착 상태를 관리 (Phase 1)
/// </summary>
[System.Serializable]
public class SkillInstance
{
    [Header("📊 스킬 데이터 참조")]
    [Tooltip("원본 SO 데이터")]
    public BaseSkillData skillData;
    
    [Header("⭐ 런타임 상태")]
    [Tooltip("현재 스킬 레벨 (0 = 미해금)")]
    public int currentLevel = 0;
    
    [Tooltip("슬롯에 장착 여부")]
    public bool isEquipped = false;
    
    [Tooltip("마지막 사용 시간 (쿨다운 계산용, 액티브만)")]
    public float lastUsedTime = -Mathf.Infinity;
    
    // ===== 생성자 =====
    public SkillInstance() { }
    
    public SkillInstance(BaseSkillData data, int level = 0)
    {
        skillData = data;
        currentLevel = level;
    }
    
    public SkillInstance(BaseSkillData data, int level, bool equipped)
    {
        skillData = data;
        currentLevel = level;
        isEquipped = equipped;
        lastUsedTime = -Mathf.Infinity;
    }
    
    // ===== 상태 확인 =====
    
    /// <summary>
    /// 해금 여부
    /// </summary>
    public bool IsUnlocked => currentLevel > 0;
    
    /// <summary>
    /// 최대 레벨 도달 여부
    /// </summary>
    public bool IsMaxLevel => skillData != null && currentLevel >= skillData.maxLevel;
    
    /// <summary>
    /// 액티브 스킬 여부
    /// </summary>
    public bool IsActiveSkill => skillData is ActiveSkillData;
    
    /// <summary>
    /// 패시브 스킬 여부
    /// </summary>
    public bool IsPassiveSkill => skillData is PassiveSkillData;
    
    // ===== 액티브 스킬 수치 계산 (CSV 연동) =====
    
    /// <summary>
    /// 현재 레벨의 쿨다운 시간 (초)
    /// Phase 2: CSV SkillLevelData.Value2 사용
    /// </summary>
    public float GetCurrentCooldown()
    {
        // ⚠️ 미해금 방어
        if (currentLevel <= 0) return 999f; // 미해금 스킬은 사용 불가
        
        if (skillData is ActiveSkillData activeData)
        {
            // CSV에서 레벨별 쿨다운 가져오기
            var levelInfo = SkillLevelDataLoader.Instance.GetSkillLevelInfo(skillData.skillID, currentLevel);
            
            if (levelInfo.level > 0) // CSV 데이터 존재
            {
                return levelInfo.value2; // Value2 = 쿨다운
            }
            
            // CSV 데이터 없으면 기본값 사용
            return activeData.baseCooldown;
        }
        return 0f;
    }
    
    /// <summary>
    /// 현재 레벨의 데미지 배율 (%)
    /// Phase 2: CSV SkillLevelData.Value1 사용
    /// </summary>
    public float GetCurrentDamage()
    {
        // ⚠️ 미해금 방어
        if (currentLevel <= 0) return 0f; // 미해금 스킬은 데미지 0
        
        if (skillData is ActiveSkillData activeData)
        {
            // CSV에서 레벨별 데미지 가져오기
            var levelInfo = SkillLevelDataLoader.Instance.GetSkillLevelInfo(skillData.skillID, currentLevel);
            
            if (levelInfo.level > 0) // CSV 데이터 존재
            {
                return levelInfo.value1; // Value1 = 데미지 배율
            }
            
            // CSV 데이터 없으면 기본값 사용
            return activeData.baseDamageMultiplier;
        }
        return 100f;
    }
    
    /// <summary>
    /// ⭐ Phase 4: 데미지 배율 (GetCurrentDamage의 별칭)
    /// PlayerRuntimeStats.FinalAttackDamage와 곱할 배율 반환
    /// </summary>
    public float GetCurrentDamageMultiplier()
    {
        return GetCurrentDamage(); // CSV Value1 값 (%) 반환
    }
    
    /// <summary>
    /// 쿨다운 남은 시간 (초)
    /// </summary>
    public float GetCooldownRemaining()
    {
        if (!IsActiveSkill) return 0f;
        
        float elapsed = Time.time - lastUsedTime;
        float cooldown = GetCurrentCooldown();
        return Mathf.Max(0f, cooldown - elapsed);
    }
    
    /// <summary>
    /// 스킬 사용 가능 여부
    /// </summary>
    public bool CanUse()
    {
        if (!IsUnlocked) return false;
        if (!IsActiveSkill) return false;
        return GetCooldownRemaining() <= 0f;
    }
    
    // ===== 패시브 스킬 수치 계산 =====
    
    /// <summary>
    /// 특정 스탯에 대한 패시브 보너스 값
    /// CSV의 StatType1/StatType2와 매핑하여 올바른 value 반환
    /// </summary>
    public float GetPassiveStatValue(EStatType statType)
    {
        if (currentLevel <= 0) return 0f;
        
        if (skillData is PassiveSkillData passiveData)
        {
            var levelInfo = SkillLevelDataLoader.Instance.GetSkillLevelInfo(skillData.skillID, currentLevel);
            
            if (levelInfo.level > 0)
            {
                // StatType1 매핑
                if (levelInfo.StatType1 == statType) return levelInfo.value1;
                // StatType2 매핑 (이중 스탯 패시브)
                if (levelInfo.StatType2 == statType) return levelInfo.value2;
                // 해당 스탯이 CSV에 없는 경우
                return 0f;
            }
            
            // CSV 데이터 없으면 SO 폴백
            return passiveData.GetStatValueAtLevel(statType, currentLevel);
        }
        return 0f;
    }
    
    /// <summary>
    /// 이 패시브가 제공하는 모든 스탯 보정 목록
    /// </summary>
    public List<PassiveStatModifier> GetAllStatModifiers()
    {
        if (skillData is PassiveSkillData passiveData)
        {
            return passiveData.statModifiers;
        }
        return new List<PassiveStatModifier>();
    }
    
    // ===== 레벨업 정보 조회 =====
    
    /// <summary>
    /// 다음 레벨로 올리는 데 필요한 SP
    /// </summary>
    public int GetRequiredSPForNextLevel()
    {
        if (IsMaxLevel) return 0;
        
        int nextLevel = currentLevel + 1;
        var levelInfo = SkillLevelDataLoader.Instance.GetSkillLevelInfo(skillData.skillID, nextLevel);
        
        if (levelInfo.level > 0)
            return levelInfo.requireSP;
        
        // CSV 데이터 없으면 기본값
        return nextLevel;
    }
    
    /// <summary>
    /// 다음 레벨 정보 가져오기
    /// </summary>
    public SkillLevelInfo GetNextLevelInfo()
    {
        if (IsMaxLevel) return default;
        
        int nextLevel = currentLevel + 1;
        return SkillLevelDataLoader.Instance.GetSkillLevelInfo(skillData.skillID, nextLevel);
    }
}
