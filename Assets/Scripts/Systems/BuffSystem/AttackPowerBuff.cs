using UnityEngine;

/// <summary>
/// 🗡️ 공격력 증가 버프 (포션, 스킬 효과 등)
/// 사용 예: "공격력 +10, 30초간"
/// </summary>
[System.Serializable]
public class AttackPowerBuff : BaseBuffEffect
{
    [Header("🗡️ 공격력 버프 설정")]
    [SerializeField] private float attackDamageBonus = 10f;  // 고정값 증가
    // [SerializeField] private float attackDamageMultiplier = 0f; // 배율 증가 (향후 확장용, 현재 미사용)
    
    public AttackPowerBuff()
    {
        effectID = "BUFF_ATTACK_POWER";
        effectName = "공격력 증가";
        effectType = BuffEffectType.Buff;
        duration = 30f;
        canStack = true;
        maxStackCount = 5;
    }
    
    public AttackPowerBuff(float bonusAmount, float durationTime)
    {
        effectID = "BUFF_ATTACK_POWER";
        effectName = "공격력 증가";
        effectType = BuffEffectType.Buff;
        attackDamageBonus = bonusAmount;
        duration = durationTime;
        canStack = true;
        maxStackCount = 5;
        Initialize();
    }
    
    public override void Apply(PlayerRuntimeStats stats)
    {
        if (stats == null) return;
        
        // PlayerRuntimeStats에 임시 공격력 증가 적용
        float totalBonus = attackDamageBonus * stackCount;
        stats.AddTemporaryAttackDamage(totalBonus);
        
    }
    
    public override void Remove(PlayerRuntimeStats stats)
    {
        if (stats == null) return;
        
        // PlayerRuntimeStats에서 임시 공격력 증가 제거
        float totalBonus = attackDamageBonus * stackCount;
        stats.RemoveTemporaryAttackDamage(totalBonus);
        
    }
    
    public override IBuffEffect Clone()
    {
        var clone = new AttackPowerBuff(attackDamageBonus, duration);
        clone.effectIcon = effectIcon;
        return clone;
    }
}