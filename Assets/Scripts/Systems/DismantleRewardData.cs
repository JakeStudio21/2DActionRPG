using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 분해 보상 데이터 (ScriptableObject)
/// </summary>
[CreateAssetMenu(fileName = "DismantleRewardData", menuName = "Data/Dismantle Reward", order = 300)]
public class DismantleRewardData : ScriptableObject
{
    [Header("등급별 기본 재료량 (8등급 → 3단계 통합)")]
    
    [Header("▼ 하급 재료 (강화 파편)")]
    [Tooltip("D등급 장비 분해 시 기본 재료량")]
    public int fragmentD = 5;
    
    [Tooltip("C등급 장비 분해 시 기본 재료량")]
    public int fragmentC = 10;
    
    [Tooltip("B등급 장비 분해 시 기본 재료량")]
    public int fragmentB = 25;
    
    [Header("▼ 중급 재료 (강화 결정)")]
    [Tooltip("A등급 장비 분해 시 기본 재료량")]
    public int fragmentA = 50;
    
    [Tooltip("S등급 장비 분해 시 기본 재료량")]
    public int fragmentS = 100;
    
    [Tooltip("SS등급 장비 분해 시 기본 재료량")]
    public int fragmentSS = 150;
    
    [Header("▼ 최상급 재료 (강화 코어)")]
    [Tooltip("EX등급 장비 분해 시 기본 재료량")]
    public int fragmentEX = 200;
    
    [Tooltip("TR등급 장비 분해 시 기본 재료량")]
    public int fragmentTR = 300;
    
    [Header("강화 보너스")]
    [Tooltip("강화 레벨 +1당 추가 재료 비율 (0.1 = 10%)")]
    [Range(0f, 1f)]
    public float enhancementBonusPerLevel = 0.1f; // +1당 10% 추가
    
    [Header("추가 재료")]
    [Tooltip("분해 시 추가로 획득하는 골드")]
    public int goldPerDismantle = 100;
    
    [Tooltip("강화석 획득 확률 (0~100)")]
    [Range(0, 100)]
    public int enhancementStoneChance = 10; // 10% 확률
    
    [Tooltip("강화석 획득 시 개수")]
    public int enhancementStoneAmount = 1;
    
    /// <summary>
    /// 등급별 기본 재료량 가져오기 (8등급 지원)
    /// </summary>
    public int GetBaseFragmentAmount(ItemGrade grade)
    {
        return grade switch
        {
            // 하급 (파편)
            ItemGrade.D => fragmentD,
            ItemGrade.C => fragmentC,
            ItemGrade.B => fragmentB,
            
            // 중급 (결정)
            ItemGrade.A => fragmentA,
            ItemGrade.S => fragmentS,
            ItemGrade.SS => fragmentSS,
            
            // 최상급 (코어)
            ItemGrade.EX => fragmentEX,
            ItemGrade.TR => fragmentTR,
            
            _ => 0
        };
    }
    
    /// <summary>
    /// 강화 레벨 보너스 계산
    /// </summary>
    public int CalculateEnhancementBonus(int baseAmount, int enhancementLevel)
    {
        if (enhancementLevel <= 0) return 0;
        return Mathf.FloorToInt(baseAmount * enhancementBonusPerLevel * enhancementLevel);
    }
    
    /// <summary>
    /// 총 조각량 계산 (기본 + 강화 보너스)
    /// </summary>
    public int CalculateTotalFragmentAmount(ItemGrade grade, int enhancementLevel)
    {
        int baseAmount = GetBaseFragmentAmount(grade);
        int bonus = CalculateEnhancementBonus(baseAmount, enhancementLevel);
        return baseAmount + bonus;
    }
    
    /// <summary>
    /// 강화석 획득 여부 (확률 기반)
    /// </summary>
    public bool RollEnhancementStone()
    {
        if (enhancementStoneChance <= 0) return false;
        return Random.Range(0, 100) < enhancementStoneChance;
    }
    
    /// <summary>
    /// 분해 보상 계산
    /// </summary>
    public Dictionary<MaterialType, int> CalculateRewards(ItemGrade grade, int enhancementLevel)
    {
        var rewards = new Dictionary<MaterialType, int>();
        
        // 1. 등급별 조각
        MaterialType fragmentType = MaterialTypeExtensions.GetFragmentTypeByGrade(grade);
        if (fragmentType != MaterialType.None)
        {
            int fragmentAmount = CalculateTotalFragmentAmount(grade, enhancementLevel);
            rewards[fragmentType] = fragmentAmount;
        }
        
        // 2. 골드
        if (goldPerDismantle > 0)
        {
            rewards[MaterialType.Gold] = goldPerDismantle;
        }
        
        // 3. 강화석 (확률)
        if (RollEnhancementStone())
        {
            rewards[MaterialType.EnhancementStone] = enhancementStoneAmount;
        }
        
        return rewards;
    }
}

