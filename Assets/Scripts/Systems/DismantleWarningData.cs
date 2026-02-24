using System;
using UnityEngine;

/// <summary>
/// 분해 경고 데이터
/// </summary>
[Serializable]
public class DismantleWarningData
{
    public ItemInstanceID instanceId;
    public string itemName;
    public ItemGrade grade;
    public int enhancementLevel;
    public bool isBound;
    public int boundSlotIndex;
    public string warningMessage;
    public string[] warningReasons;
    
    /// <summary>
    /// 일반 아이템 분해 경고 생성
    /// </summary>
    public static DismantleWarningData CreateWarning(ItemInstanceID instanceId, string itemName, 
        ItemGrade grade, int enhancementLevel, bool isBound, int boundSlotIndex)
    {
        var data = new DismantleWarningData
        {
            instanceId = instanceId,
            itemName = itemName,
            grade = grade,
            enhancementLevel = enhancementLevel,
            isBound = isBound,
            boundSlotIndex = boundSlotIndex
        };
        
        // 경고 사유 생성
        var reasons = new System.Collections.Generic.List<string>();
        
        if (isBound)
        {
            reasons.Add($"• 캐릭터 {boundSlotIndex}에 <color=red>귀속된 아이템</color>입니다");
        }
        
        if (grade == ItemGrade.S || grade == ItemGrade.A)
        {
            reasons.Add($"• <color=yellow>{grade}등급</color> 고급 아이템입니다");
        }
        
        if (enhancementLevel >= 10)
        {
            reasons.Add($"• <color=cyan>+{enhancementLevel} 강화</color>된 아이템입니다");
        }
        
        data.warningReasons = reasons.ToArray();
        
        // 경고 메시지 생성
        string displayName = $"<b>{itemName}</b>";
        if (enhancementLevel > 0)
        {
            displayName += $" <color=cyan>+{enhancementLevel}</color>";
        }
        
        data.warningMessage = $@"<size=18>{displayName}</size>

아래 아이템을 <color=red><b>분해</b></color>하시겠습니까?

{string.Join("\n", data.warningReasons)}

<color=orange>⚠️ 분해한 아이템은 복구할 수 없습니다!</color>

정말 분해하시겠습니까?";
        
        return data;
    }
    
    /// <summary>
    /// 간단한 분해 확인 메시지
    /// </summary>
    public static string GetSimpleWarning(string itemName, int enhancementLevel)
    {
        string display = itemName;
        if (enhancementLevel > 0)
        {
            display += $" +{enhancementLevel}";
        }
        return $"<b>{display}</b>을(를) 분해하시겠습니까?";
    }
}

