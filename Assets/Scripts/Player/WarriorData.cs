using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 워리어 클래스 전용 데이터 ScriptableObject
/// BaseClassData를 상속받아 워리어 고유의 능력치와 특성을 정의
/// </summary>
[CreateAssetMenu(fileName = "WarriorData", menuName = "Player/WarriorData")]
public class WarriorData : BaseClassData
{
    [Header("⚔️ 워리어 능력치 배율")]
    [SerializeField] private float attackPowerMultiplier = 1.1f;   // 10% 공격력 증가
    [SerializeField] private float moveSpeedMultiplier = 0.8f;     // 20% 이동속도 감소 (중갑)
    [SerializeField] private float skillCooldownMultiplier = 1.0f; // 기본 쿨다운 (밸런스)
    [SerializeField] private float healthMultiplier = 1.5f;        // 50% 체력 증가 (탱커)


// 아이소메트릭 캐릭터 구현
    [Header("아이소메트릭 설정")]
    [SerializeField] private IsometricCharacterData isometricData = new IsometricCharacterData();

    /// <summary>
    /// 아이소메트릭 캐릭터 데이터 접근
    /// </summary>
    public IsometricCharacterData IsometricData => isometricData;


    // BaseClassData의 추상 속성들 구현
    public override float AttackPowerMultiplier => attackPowerMultiplier;
    public override float MoveSpeedMultiplier => moveSpeedMultiplier;
    public override float SkillCooldownMultiplier => skillCooldownMultiplier;
    public override float HealthMultiplier => healthMultiplier;

    [Header("🛡️ 워리어 고유 특성")]
    [Tooltip("블록 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float warriorBlockChance = 0.2f;           // 20% 블록 확률
    
    [Tooltip("블록 시 데미지 감소율 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float warriorBlockDamageReduction = 0.5f;  // 블록 시 50% 데미지 감소
    
    [Tooltip("반격 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float warriorCounterAttackChance = 0.15f;  // 15% 반격 확률
    
    [Tooltip("반격 데미지 배율")]
    public float warriorCounterAttackDamage = 1.3f;   // 반격 데미지 130%
    
    [Tooltip("버서커 모드 발동 체력 임계점 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float warriorBerserkerThreshold = 0.3f;    // 30% 체력 이하 시 버서커
    
    [Tooltip("버서커 모드 데미지 보너스 배율")]
    public float warriorBerserkerDamageBonus = 1.5f;  // 버서커 모드 50% 데미지 증가
    
    [Tooltip("넉백 저항도 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float warriorKnockbackResistance = 0.5f;   // 50% 넉백 저항

    /// <summary>
    /// 워리어 전용 정보 출력 (부모 메서드 오버라이드)
    /// </summary>
    public override string GetStatsInfo()
    {
        string baseInfo = base.GetStatsInfo();
        string warriorInfo = $"\n--- 워리어 능력치 배율 ---\n" +
                            $"공격력 배율: {attackPowerMultiplier:F1}배 ({(attackPowerMultiplier - 1) * 100:F0}%)\n" +
                            $"이동속도 배율: {moveSpeedMultiplier:F1}배 ({(moveSpeedMultiplier - 1) * 100:F0}%)\n" +
                            $"스킬 쿨다운 배율: {skillCooldownMultiplier:F1}배 ({(skillCooldownMultiplier - 1) * 100:F0}%)\n" +
                            $"체력 배율: {healthMultiplier:F1}배 (+{(healthMultiplier - 1) * 100:F0}%)" +
                            $"\n--- 워리어 고유 특성 ---\n" +
                            $"블록 확률: {warriorBlockChance * 100:F0}%\n" +
                            $"블록 데미지 감소: {warriorBlockDamageReduction * 100:F0}%\n" +
                            $"반격 확률: {warriorCounterAttackChance * 100:F0}%\n" +
                            $"반격 데미지: {warriorCounterAttackDamage:F1}배 (+{(warriorCounterAttackDamage - 1) * 100:F0}%)\n" +
                            $"버서커 임계점: {warriorBerserkerThreshold * 100:F0}% 이하\n" +
                            $"버서커 데미지 보너스: +{(warriorBerserkerDamageBonus - 1) * 100:F0}%\n" +
                            $"넉백 저항: {warriorKnockbackResistance * 100:F0}%";
        
        return baseInfo + warriorInfo;
    }

    /// <summary>
    /// 워리어 데이터 검증 (OnValidate에서 호출)
    /// </summary>
    private void OnValidate()
    {
        // 배율 값들이 음수가 아닌지 확인
        attackPowerMultiplier = Mathf.Max(0f, attackPowerMultiplier);
        moveSpeedMultiplier = Mathf.Max(0f, moveSpeedMultiplier);
        skillCooldownMultiplier = Mathf.Max(0f, skillCooldownMultiplier);
        healthMultiplier = Mathf.Max(0f, healthMultiplier);
        
        // 확률 값들이 0~1 범위인지 확인 (Range 어트리뷰트와 중복 방지)
        warriorBlockChance = Mathf.Clamp01(warriorBlockChance);
        warriorBlockDamageReduction = Mathf.Clamp01(warriorBlockDamageReduction);
        warriorCounterAttackChance = Mathf.Clamp01(warriorCounterAttackChance);
        warriorBerserkerThreshold = Mathf.Clamp01(warriorBerserkerThreshold);
        warriorKnockbackResistance = Mathf.Clamp01(warriorKnockbackResistance);
        
        // 데미지 배율 값들이 음수가 아닌지 확인
        warriorCounterAttackDamage = Mathf.Max(0f, warriorCounterAttackDamage);
        warriorBerserkerDamageBonus = Mathf.Max(0f, warriorBerserkerDamageBonus);
        
        // 🆕 아이소메트릭 데이터 기본값 설정
        if (isometricData == null)
        {
            isometricData = new IsometricCharacterData();
            isometricData.SetDefaults();
        }
        
        // 🆕 아이소메트릭 데이터 유효성 검증
        if (!isometricData.IsValid())
        {
            Debug.LogWarning($"[WarriorData] {name}의 아이소메트릭 데이터가 유효하지 않습니다.");
            isometricData.SetDefaults();
        }
    }

}
