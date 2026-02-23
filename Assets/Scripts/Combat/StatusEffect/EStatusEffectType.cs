/// <summary>
/// 상태이상 타입
/// ⚙️ Phase 4-C: ConditionalModifier 시스템과 연동
/// </summary>
public enum EStatusEffectType
{
    None = 0,
    
    // === 이동 방해 ===
    Bind = 1,       // 속박: 이동 불가
    Slow = 2,       // 둔화: 이동속도 감소
    Stun = 3,       // 기절: 모든 행동 불가
    
    // === 지속 피해 ===
    Poison = 10,    // 중독: 초당 독 데미지
    Burn = 11,      // 화상: 초당 화상 데미지
    Bleed = 12,     // 출혈: 초당 출혈 데미지
    
    // === 능력치 변화 ===
    Weaken = 20,    // 약화: 공격력 감소
    Vulnerable = 21,// 취약: 받는 피해 증가
    
    // === 회복 방해 ===
    HealBlock = 30  // 회복 차단: 회복 효과 감소
}

