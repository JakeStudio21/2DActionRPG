using System;
using UnityEngine;

/// <summary>
/// 장비 스탯 데이터 (StatDefinition.csv 기반)
/// 장비의 개별 스탯을 저장하는 구조체
/// </summary>
[Serializable]
public class ItemStat
{
    [Tooltip("스탯 ID (예: ATK_FLAT, CRIT_RATE)")]
    public string statId;
    
    [Tooltip("스탯 값 (예: ATK_FLAT = 50, CRIT_RATE = 0.15)")]
    public float value;
    
    [Tooltip("표시명 (에디터 전용, 선택사항)")]
    public string displayName;
    
    /// <summary>
    /// 기본 생성자
    /// </summary>
    public ItemStat()
    {
        statId = "";
        value = 0f;
        displayName = "";
    }
    
    /// <summary>
    /// 값 포함 생성자
    /// </summary>
    public ItemStat(string statId, float value, string displayName = "")
    {
        this.statId = statId;
        this.value = value;
        this.displayName = displayName;
    }
    
    /// <summary>
    /// 디버그용 문자열
    /// </summary>
    public override string ToString()
    {
        return $"{statId}: {value} ({displayName})";
    }
}

