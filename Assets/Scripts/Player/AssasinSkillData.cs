using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 어쌔신 전용 스킬 데이터
/// 원거리 공격, 은신, 크리티컬 관련 속성 특화 (SRP 준수)
/// </summary>
[CreateAssetMenu(fileName = "AssasinSkillData", menuName = "Skill System/Assasin Skill Data")]
public class AssasinSkillData : BaseSkillData
{
    [Header("🏹 어쌔신 전용 - 원거리")]
    [Tooltip("발사할 화살 프리팹")]
    public GameObject projectilePrefab;
    
    [Tooltip("발사체 속도")]
    public float projectileSpeed = 10f;
    
    [Tooltip("동시 발사 화살 개수")]
    public int projectileCount = 1;
    
    [Tooltip("화살 퍼짐 각도")]
    public float spreadAngle = 0f;
    
    [Tooltip("발사체 크기 배율")]
    public Vector3 projectileScale = Vector3.one;
    
    [Tooltip("발사체 풀 이름 (오브젝트 풀링용)")]
    public string projectilePoolName = "Arrow";
    
    [Header("👤 어쌔신 전용 - 은신/크리티컬")]
    [Tooltip("은신 지속시간 (초)")]
    public float stealthDuration = 2f;
    
    [Tooltip("크리티컬 확률 보너스 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float criticalChanceBonus = 0.2f;
    
    [Tooltip("백어택 데미지 배율")]
    public float backAttackMultiplier = 1.5f;
    
    [Tooltip("회피 확률 보너스 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float dodgeChanceBonus = 0.05f;
}
