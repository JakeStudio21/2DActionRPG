using UnityEngine;

/// <summary>
/// 포이즈(자세 내구도) 시스템 컴포넌트
/// EnemyHealth가 TakeDamage 시 호출 → 스태거 발생 여부와 넉백 배율을 반환
///
/// 사용법:
///   1. Elite/Boss 프리팹에 컴포넌트 추가
///   2. EnemyHealth.Start()에서 Initialize(HitReactionData) 호출
///   3. TakeDamage 내부에서 TryApplyHitReaction() 호출 후 반환값으로 스태거 분기
/// </summary>
public class PoiseHandler : MonoBehaviour
{
    // 런타임 상태
    private HitReactionData data;
    private float currentPoise;
    private float lastHitTime;
    private float staggerImmunityExpireTime;

    // ─────────────────────────────────────────
    // 공개 프로퍼티
    // ─────────────────────────────────────────

    /// <summary>현재 스태거 무적 구간인지 여부</summary>
    public bool IsStaggerImmune => Time.time < staggerImmunityExpireTime;

    /// <summary>스태거 발생 시 곱할 넉백 배율 (0 = 넉백 없음)</summary>
    public float KnockbackMultiplier => data != null ? data.knockbackMultiplier : 1f;

    /// <summary>포이즈 시스템이 유효하게 초기화되었는지</summary>
    public bool IsInitialized => data != null;

    // ─────────────────────────────────────────
    // 초기화
    // ─────────────────────────────────────────

    /// <summary>
    /// EnemyHealth.Start()에서 HitReactionData를 주입하여 초기화
    /// </summary>
    public void Initialize(HitReactionData hitReactionData)
    {
        data = hitReactionData;
        currentPoise = data.poiseMax;
        lastHitTime = -999f;
        staggerImmunityExpireTime = 0f;
    }

    // ─────────────────────────────────────────
    // 포이즈 회복
    // ─────────────────────────────────────────

    private void Update()
    {
        if (data == null || data.poiseMax <= 0) return;
        if (currentPoise >= data.poiseMax) return;

        // 마지막 피격 후 recoveryDelay가 지나면 포이즈 회복 시작
        if (Time.time - lastHitTime >= data.poiseRecoveryDelay)
        {
            currentPoise = Mathf.Min(
                currentPoise + data.poiseRecoveryRate * Time.deltaTime,
                data.poiseMax
            );
        }
    }

    // ─────────────────────────────────────────
    // 피격 처리
    // ─────────────────────────────────────────

    /// <summary>
    /// TakeDamage에서 호출. 스태거 발생 여부를 반환한다.
    ///
    /// true  → 스태거 발생 (넉백 + HitState + 스킬 캔슬 적용)
    /// false → 포이즈로 흡수 (데미지·Flash만, 이동/스킬 유지)
    /// </summary>
    /// <param name="poiseBreak">이 피격이 소모하는 포이즈 양 (기본 1)</param>
    public bool TryApplyHitReaction(int poiseBreak = 1)
    {
        lastHitTime = Time.time;

        // ① 포이즈 없는 몬스터(Normal) → 항상 스태거
        if (data == null || data.poiseMax <= 0)
            return true;

        // ② 이미 스태거 무적 구간 → 포이즈 차감만, 스태거 차단
        if (IsStaggerImmune)
        {
            currentPoise = Mathf.Max(0, currentPoise - poiseBreak);
            return false;
        }

        // ③ 포이즈 차감
        currentPoise -= poiseBreak;

        if (currentPoise > 0)
            return false; // 포이즈 남음 → 스태거 없음

        // ④ 포이즈 소진 → 스태거 발생
        currentPoise = data.poiseMax; // 포이즈 즉시 재충전
        staggerImmunityExpireTime = Time.time + data.staggerImmunityDuration;
        return true;
    }
}
