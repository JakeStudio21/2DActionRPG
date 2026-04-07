/// <summary>
/// 플레이어 최종 스탯 스냅샷 - 로비/인게임 공통 데이터 구조
/// PlayerStatComputationService.Compute()의 출력값이며 PlayerRuntimeStats가 이 값을 자신의 필드에 복사한다.
/// </summary>
public struct PlayerFinalStatsSnapshot
{
    // ─── 기본 패널 ────────────────────────────────
    public float AttackDamage;
    public float MaxHealth;
    public float Defense;
    public float CriticalChance;      // 0~1 소수 (0.15 = 15%)
    public float CriticalDamage;      // 배율 (1.5 = 150%)
    public float AttackSpeed;         // 1.0 기준 배율
    public float MoveSpeed;           // 절대값 (m/s 단위 기준)
    public float HealMultiplier;      // 1.0 = 100%

    // ─── 심화 패널 ────────────────────────────────
    public float SkillDamageBonus;    // 0~1 소수
    public float CooldownReduction;   // 0~0.5 소수
    public float LifeSteal;           // 0~1 소수
    public float ArmorPenetration;    // 0~1 소수
    public float DamageReduction;     // 0~0.8 소수
    public float HpRegen;             // HP/sec
    public float DodgeChance;         // 0~1 소수
    public float BlockChance;         // 0~1 소수
    public float StatusResist;        // 0~1 소수
    public float PierceDamageRetention; // 0~1 소수 (기본 0.5)
    public float ExpGainBonus;        // 0~ 소수

    // ─── 내부 계산용 (UI 미표시) ──────────────────
    public float MoveSpeedPercentBonus; // 클래스 배율 적용 전 누적 이속%
}
