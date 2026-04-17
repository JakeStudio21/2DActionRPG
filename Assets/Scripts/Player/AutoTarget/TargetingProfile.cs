using UnityEngine;

/// <summary>
/// 자동 타겟팅 스코어링 가중치 설정 ScriptableObject.
/// 기본공격 / 스킬별로 각각 별도 에셋을 만들어 할당합니다.
/// ManualAimThreshold는 의도적으로 존재하지 않습니다.
/// (이동 조이스틱 방향은 항상 AngleWeight 계산에만 사용됩니다.)
/// </summary>
[CreateAssetMenu(fileName = "TargetingProfile", menuName = "AutoTarget/TargetingProfile")]
public class TargetingProfile : ScriptableObject
{
    [Header("탐지 범위")]
    [Tooltip("자동 탐지 반경 (월드 단위)")]
    [Range(1f, 20f)] public float detectionRadius = 6f;

    [Header("스코어링 가중치")]
    [Tooltip("거리가 가까울수록 부여하는 점수 배율. 높을수록 가까운 적 우선.")]
    [Range(0f, 10f)] public float distanceWeight = 3f;

    [Tooltip("이동(혹은 페이싱) 방향과 일치할수록 부여하는 점수 배율. 높을수록 보고 있는 쪽 적 우선.")]
    [Range(0f, 10f)] public float angleWeight = 5f;

    [Header("랭크 보너스 (고정 점수)")]
    [Tooltip("Elite 등급 적에게 추가하는 보너스 점수")]
    public float eliteBonus    = 5f;
    [Tooltip("MiniBoss 등급 적에게 추가하는 보너스 점수")]
    public float miniBossBonus = 15f;
    [Tooltip("Boss 등급 적에게 추가하는 보너스 점수")]
    public float bossBonus     = 30f;

    [Header("미션·장애물 보너스 (고정 점수)")]
    [Tooltip("미션 목표 오브젝트(isVictoryTarget 바리케이드)에 추가하는 보너스 점수.\n" +
             "Normal(0)보다 높고 Elite보다 낮게 설정하세요. (기본값: 2.5)")]
    public float missionObjectBonus = 2.5f;
    [Tooltip("일반 파괴 오브젝트(일반 바리케이드·구르는 바위)의 보너스 점수.\n" +
             "음수로 설정하면 다른 조건이 같을 때 Normal 몬스터보다 낮은 우선순위가 됩니다. (기본값: -5)")]
    public float obstacleBonus      = -5f;

    [Header("근접 보호망 (Close Protection)")]
    [Tooltip("이 반경 안에 들어온 적은 다른 조건을 무시하고 최우선 타겟이 됩니다.")]
    [Range(0f, 5f)] public float closeProtectionRadius = 1.5f;
    [Tooltip("근접 보호망 발동 시 추가되는 점수 (다른 모든 점수를 압도해야 함)")]
    public float closeProtectionBonus = 100f;

    [Header("타겟 고정 (Stickiness)")]
    [Tooltip("현재 타겟에게 주는 유지 보너스 점수. 값이 클수록 타겟이 잘 바뀌지 않습니다.")]
    public float stickinessBonus = 10f;
    [Tooltip("현재 타겟이 이 반경 밖으로 나가면 강제로 타겟을 해제합니다.")]
    [Range(0f, 30f)] public float targetLostRadius = 10f;

    /// <summary>적의 랭크에 맞는 보너스 점수를 반환합니다.</summary>
    public float GetRankBonus(EnemyRank rank)
    {
        return rank switch
        {
            EnemyRank.Elite         => eliteBonus,
            EnemyRank.MiniBoss      => miniBossBonus,
            EnemyRank.Boss          => bossBonus,
            EnemyRank.MissionObject => missionObjectBonus,
            EnemyRank.Obstacle      => obstacleBonus,
            _                       => 0f,
        };
    }
}
