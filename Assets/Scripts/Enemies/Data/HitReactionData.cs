using UnityEngine;

/// <summary>
/// 피격 반응 설정 데이터 (ScriptableObject)
/// EnemyData에 참조하여 등급별 포이즈·넉백·스태거 동작을 Inspector에서 설정
/// </summary>
[CreateAssetMenu(fileName = "HitReactionData", menuName = "Enemy/Hit Reaction Data")]
public class HitReactionData : ScriptableObject
{
    [Header("⚔️ 포이즈 (자세 내구도)")]
    [Tooltip("최대 포이즈.\n0 = 포이즈 없음, 모든 피격에 즉시 스태거 (Normal 몬스터 기본값)\n값이 클수록 여러 타격을 버팀")]
    [Min(0)]
    public int poiseMax = 0;

    [Tooltip("마지막 피격 후 포이즈 회복이 시작될 때까지 대기 시간 (초)")]
    [Min(0f)]
    public float poiseRecoveryDelay = 2f;

    [Tooltip("초당 포이즈 회복량 (poiseRecoveryDelay 경과 후 적용)")]
    [Min(0.1f)]
    public float poiseRecoveryRate = 5f;

    [Header("🛡️ 스태거 무적")]
    [Tooltip("스태거 발생 직후 추가 스태거를 차단하는 시간 (초)\n이 시간 안에 들어오는 피격은 포이즈만 소모하고 스태거 무시")]
    [Min(0f)]
    public float staggerImmunityDuration = 0.5f;

    [Header("💨 넉백 배율")]
    [Tooltip("스태거 발생 시 적용할 넉백 강도 배율\n0.0 = 넉백 완전 무시 (보스 권장)\n0.3 = 약한 넉백 (엘리트 권장)\n1.0 = 기본 넉백 (일반 몬스터)")]
    [Range(0f, 1f)]
    public float knockbackMultiplier = 1f;
}
