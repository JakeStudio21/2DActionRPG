using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 스킬 데이터의 기본 클래스
/// 직업별 공통 속성만 포함 (SRP 준수)
/// </summary>
public abstract class BaseSkillData : ScriptableObject
{
    [Header("📋 기본 정보")]
    public string skillName = "New Skill";
    public Sprite icon;
    
    [Header("⚡ 기본 수치")]
    public float cooldown = 2f;
    public float damage = 10f;
    public float range = 5f;
    
    [Header("🎭 공통 이펙트")]
    public GameObject effectPrefab;
}
