using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 어쌔신 클래스 전용 데이터 ScriptableObject
/// BaseClassData를 상속받아 어쌔신 고유의 능력치와 특성을 정의
/// </summary>
[CreateAssetMenu(fileName = "AssasinData", menuName = "Player/AssasinData")]
public class AssasinData : BaseClassData
{
    [Header("🏹 어쌔신 능력치 배율")]
    [SerializeField] private float attackPowerMultiplier = 1.2f;   // 20% 공격력 증가
    [SerializeField] private float moveSpeedMultiplier = 1.3f;     // 30% 이동속도 증가  
    [SerializeField] private float skillCooldownMultiplier = 0.8f; // 20% 쿨다운 감소
    [SerializeField] private float healthMultiplier = 0.9f;        // 10% 체력 감소 (유리몸)

    // BaseClassData의 추상 속성들 구현
    public override float AttackPowerMultiplier => attackPowerMultiplier;
    public override float MoveSpeedMultiplier => moveSpeedMultiplier;
    public override float SkillCooldownMultiplier => skillCooldownMultiplier;
    public override float HealthMultiplier => healthMultiplier;

    [Header("🎯 어쌔신 고유 특성")]
    [Tooltip("은신 지속시간 (초)")]
    public float assasinStealthDuration = 2f;        // 은신 지속시간
    
    [Tooltip("회피 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float assasinDodgeChance = 0.1f;          // 10% 회피 확률
    
    [Tooltip("백어택 데미지 보너스 배율")]
    public float assasinBackAttackBonus = 1.3f;      // 백어택 보너스 30%

    /// <summary>
    /// 어쌔신 전용 정보 출력 (부모 메서드 오버라이드)
    /// </summary>
    public override string GetStatsInfo()
    {
        string baseInfo = base.GetStatsInfo();
        string assasinInfo = $"\n--- 어쌔신 고유 특성 ---\n" +
                            $"회피 확률: {assasinDodgeChance * 100:F0}%\n" +
                            $"은신 지속시간: {assasinStealthDuration:F1}초\n" +
                            $"백어택 보너스: +{(assasinBackAttackBonus - 1) * 100:F0}%";
        
        return baseInfo + assasinInfo;
    }

    /// <summary>
    /// 어쌔신 데이터 검증 (OnValidate에서 호출)
    /// </summary>
    private void OnValidate()
    {
        // 확률 값들이 0~1 범위인지 확인
        assasinDodgeChance = Mathf.Clamp01(assasinDodgeChance);
        
        // 배율 값들이 음수가 아닌지 확인
        assasinBackAttackBonus = Mathf.Max(0f, assasinBackAttackBonus);
        assasinStealthDuration = Mathf.Max(0f, assasinStealthDuration);
    }
}
