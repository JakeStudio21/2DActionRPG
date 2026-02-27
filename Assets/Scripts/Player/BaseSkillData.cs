using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 스킬의 최상위 추상 클래스
/// 액티브/패시브 공통 속성만 정의 (Phase 1: 데이터 구조 계층화)
/// </summary>
public abstract class BaseSkillData : ScriptableObject
{
    [Header("📋 기본 정보")]
    [Tooltip("고유 식별자 (예: 'warrior_dash', 'passive_spirit_resonance')")]
    public string skillID;
    
    [Tooltip("스킬 이름")]
    public string skillName = "New Skill";
    
    [Tooltip("스킬 아이콘 (UI용)")]
    public Sprite icon;
    
    [TextArea(3, 5)]
    [Tooltip("스킬 설명")]
    public string description;
    
    [Header("⭐ 해금 및 성장")]
    [Tooltip("해금에 필요한 플레이어 레벨")]
    public int unlockLevel = 1;
    
    [Tooltip("스킬 최대 레벨")]
    public int maxLevel = 10;
    
    /// <summary>
    /// 액티브/패시브 구분
    /// </summary>
    public abstract SkillCategory GetSkillCategory();
}

/// <summary>
/// 스킬 대분류
/// </summary>
public enum SkillCategory
{
    Active,     // 액티브 스킬
    Passive     // 패시브 스킬
}
