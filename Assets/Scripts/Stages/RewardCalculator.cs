using System.Collections.Generic;
using UnityEngine;
using ItemSystem;
using StageSystem;

/// <summary>
/// 보상 연산 코어 — 중간 연산 레이어
///
/// 역할:
///   몬스터 처치 / 스테이지 클리어 시점에 필요한 모든 입력값을 받아
///   최종 골드·EXP·재료 수량·장비 생성 의뢰를 계산하여 반환합니다.
///
/// 설계 원칙:
///   - EnemyData, StageConfig, RewardLevelRangeTable 의 기존 뼈대를 수정하지 않습니다.
///   - 이 클래스만 교체해도 보상 공식을 전면 변경할 수 있도록 분리합니다.
///   - 모든 확률/가중치는 인스펙터에서 기획자가 직접 조정 가능합니다.
/// </summary>
public class RewardCalculator : MonoBehaviour
{
    public static RewardCalculator Instance { get; private set; }

    [Header("📊 글로벌 레벨 구간 보상 테이블")]
    [Tooltip("스테이지별 overrideRangeTable 이 null 이면 이 테이블을 사용합니다.")]
    [SerializeField] private RewardLevelRangeTable defaultRangeTable;

    [Header("💰 처치 보상 추가 레벨 배율")]
    [Tooltip("레벨 구간 배율 위에 추가로 곱해지는 글로벌 골드 배율 (1.0 = 그대로)")]
    [SerializeField] private float killGoldMultiplier = 1f;

    [Tooltip("레벨 구간 배율 위에 추가로 곱해지는 글로벌 EXP 배율")]
    [SerializeField] private float killExpMultiplier = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ─────────────────────────────────────────────────────────────────
    // 1. 몬스터 처치 보상 계산
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 몬스터 처치 보상을 계산합니다.
    ///
    /// 호출 위치: EnemyHealth.OnDeathAnimationComplete()
    /// </summary>
    /// <param name="enemyData">처치한 몬스터의 기본 데이터</param>
    /// <param name="enemyType">Basic / Elite / Boss</param>
    /// <param name="monsterLevel">InitializeLevel() 로 결정된 최종 레벨</param>
    /// <param name="growthProfile">몬스터가 참조하는 성장 프로필</param>
    /// <param name="stageConfig">현재 스테이지 설정 (타겟 파밍 + 오버라이드 테이블)</param>
    /// <returns>처치 보상 계산 결과</returns>
    public KillRewardResult CalculateKillReward(
        EnemyData         enemyData,
        EnemyType         enemyType,
        int               monsterLevel,
        MonsterGrowthProfile growthProfile,
        StageConfig       stageConfig)
    {
        var result = new KillRewardResult();

        if (enemyData == null || growthProfile == null)
        {
            Debug.LogWarning("[RewardCalculator] CalculateKillReward: enemyData 또는 growthProfile 이 null 입니다.");
            return result;
        }

        // ── 레벨 구간 테이블 선택 ──────────────────────────────────
        LevelRangeEntry rangeEntry = GetRangeEntry(stageConfig, monsterLevel);

        // ── 골드 계산 ─────────────────────────────────────────────
        // 기본: EnemyData 원본값 × GrowthProfile 레벨 스케일 × 레벨구간 골드배율 × 글로벌 배율
        float scaledGold = enemyData.GetScaledGoldReward(monsterLevel, growthProfile)
                           * rangeEntry.goldLevelMultiplier
                           * killGoldMultiplier;
        result.gold = Mathf.Max(0, Mathf.RoundToInt(scaledGold));

        // ── EXP 계산 ──────────────────────────────────────────────
        float scaledExp = enemyData.GetScaledExpReward(monsterLevel, growthProfile)
                          * rangeEntry.expLevelMultiplier
                          * killExpMultiplier;
        result.exp = Mathf.Max(0, Mathf.RoundToInt(scaledExp));

        // ── 드롭 장비 파라미터 계산 ───────────────────────────────
        result.equipmentChanceMultiplier = rangeEntry.equipmentChanceMultiplier;
        result.materialAmountMultiplier  = rangeEntry.materialAmountMultiplier;
        result.minRarity                 = rangeEntry.minRarity;
        result.maxRarity                 = rangeEntry.maxRarity;

        // ── 보스/엘리트 드롭 보너스 ──────────────────────────────
        // 타입 배율은 전투 스탯에만 적용되던 것을 드롭 품질에도 반영합니다.
        ApplyEnemyTypeDropBonus(enemyType, ref result);

        // ── 타겟 파밍 배율 반영 (드롭 요청 단계에 전달) ──────────
        result.targetFarmingEntries = stageConfig != null
            ? stageConfig.targetFarmingEntries
            : null;

        return result;
    }

    // ─────────────────────────────────────────────────────────────────
    // 2. 스테이지 / 던전 클리어 보상 계산
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 스테이지(또는 던전) 클리어 보상을 계산합니다.
    ///
    /// 호출 위치: RewardSystem.CalculateBaseRewards()
    /// </summary>
    /// <param name="baseGold">DropTable 에 설정된 기본 골드값</param>
    /// <param name="baseExp">DropTable 에 설정된 기본 EXP값</param>
    /// <param name="timeBonus">클리어 시간 보너스 배율 (RewardSystem 이 계산해서 전달)</param>
    /// <param name="stageConfig">현재 스테이지 설정</param>
    /// <returns>레벨 스케일링이 적용된 최종 골드·EXP</returns>
    public StageClearRewardResult CalculateStageClearReward(
        int         baseGold,
        int         baseExp,
        float       timeBonus,
        StageConfig stageConfig)
    {
        var result = new StageClearRewardResult();

        int stageLevel = GetStageBaseLevel(stageConfig);
        LevelRangeEntry rangeEntry = GetRangeEntry(stageConfig, stageLevel);

        // 최종 골드 = 기본값 × 시간보너스 × 레벨구간 골드배율
        result.gold = Mathf.Max(0, Mathf.RoundToInt(baseGold * timeBonus * rangeEntry.goldLevelMultiplier));

        // 최종 EXP = 기본값 × 시간보너스 × 레벨구간 EXP배율
        result.exp  = Mathf.Max(0, Mathf.RoundToInt(baseExp  * timeBonus * rangeEntry.expLevelMultiplier));

        // 아이템 드롭 시 사용할 장비 파라미터도 함께 전달
        result.equipmentChanceMultiplier = rangeEntry.equipmentChanceMultiplier;
        result.materialAmountMultiplier  = rangeEntry.materialAmountMultiplier;
        result.minRarity                 = rangeEntry.minRarity;
        result.maxRarity                 = rangeEntry.maxRarity;

        return result;
    }

    // ─────────────────────────────────────────────────────────────────
    // 내부 헬퍼
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 스테이지 레벨에 맞는 구간 설정을 반환합니다.
    /// stageConfig.overrideRangeTable 이 있으면 우선 사용합니다.
    /// </summary>
    private LevelRangeEntry GetRangeEntry(StageConfig stageConfig, int level)
    {
        RewardLevelRangeTable table = null;

        if (stageConfig != null && stageConfig.overrideRangeTable != null)
            table = stageConfig.overrideRangeTable;
        else
            table = defaultRangeTable;

        if (table == null)
        {
            Debug.LogWarning("[RewardCalculator] RewardLevelRangeTable 이 할당되지 않았습니다. 기본값을 사용합니다.");
            return LevelRangeEntry.Default;
        }

        return table.GetEntry(level);
    }

    /// <summary>
    /// StageConfig 에서 스테이지 기준 레벨을 안전하게 가져옵니다.
    /// </summary>
    private int GetStageBaseLevel(StageConfig stageConfig)
    {
        if (stageConfig != null)
            return Mathf.Max(1, stageConfig.StageBaseLevel);

        // StageManager 에서 현재 Config 를 가져오는 fallback
        if (StageManager.Instance?.CurrentStageConfig != null)
            return Mathf.Max(1, StageManager.Instance.CurrentStageConfig.StageBaseLevel);

        return 1;
    }

    /// <summary>
    /// EnemyType 에 따라 드롭 품질 파라미터에 보너스를 적용합니다.
    ///
    /// Elite: 장비 드롭 확률 1.5배, 등급 하한 1단계 상향
    /// Boss:  장비 드롭 확률 3.0배, 등급 하한 2단계 상향
    /// </summary>
    private void ApplyEnemyTypeDropBonus(EnemyType enemyType, ref KillRewardResult result)
    {
        switch (enemyType)
        {
            case EnemyType.Elite:
                result.equipmentChanceMultiplier *= 1.5f;
                result.minRarity = BumpRank(result.minRarity, 1);
                break;

            case EnemyType.Boss:
                result.equipmentChanceMultiplier *= 3.0f;
                result.minRarity = BumpRank(result.minRarity, 2);
                result.maxRarity = BumpRank(result.maxRarity, 1);
                break;
        }
    }

    /// <summary>
    /// EquipmentRank 를 clamp 를 유지하며 n 단계 올립니다.
    /// </summary>
    private EquipmentRank BumpRank(EquipmentRank rank, int step)
    {
        int next = Mathf.Min((int)rank + step, (int)EquipmentRank.TR);
        return (EquipmentRank)next;
    }

    // ─────────────────────────────────────────────────────────────────
    // 공개 유틸 — StageBaseLevel 취득 (EnemyHealth 버그 수정용)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 현재 스테이지 기준 레벨을 반환합니다.
    /// StageManager → CurrentStageConfig.StageBaseLevel 을 우선 참조하며,
    /// 없을 경우 monsterLevel 을 fallback 으로 사용합니다.
    ///
    /// 호출 위치: EnemyHealth.GetCurrentStageLevel() 교체용
    /// </summary>
    public static int GetCurrentStageLevel(int monsterLevelFallback = 1)
    {
        if (StageManager.Instance?.CurrentStageConfig != null)
            return Mathf.Max(1, StageManager.Instance.CurrentStageConfig.StageBaseLevel);

        return Mathf.Max(1, monsterLevelFallback);
    }
}

// ─────────────────────────────────────────────────────────────────────
// 반환 데이터 구조체
// ─────────────────────────────────────────────────────────────────────

/// <summary>
/// 몬스터 처치 보상 계산 결과
/// </summary>
[System.Serializable]
public class KillRewardResult
{
    /// <summary>지급할 골드량</summary>
    public int gold;

    /// <summary>지급할 경험치량</summary>
    public int exp;

    /// <summary>장비 드롭 확률에 곱해지는 배율</summary>
    public float equipmentChanceMultiplier = 1f;

    /// <summary>재료 드롭 수량에 곱해지는 배율</summary>
    public float materialAmountMultiplier = 1f;

    /// <summary>이 처치에서 생성 가능한 최소 장비 등급</summary>
    public EquipmentRank minRarity = EquipmentRank.D;

    /// <summary>이 처치에서 생성 가능한 최대 장비 등급</summary>
    public EquipmentRank maxRarity = EquipmentRank.A;

    /// <summary>타겟 파밍 엔트리 목록 (ItemGenerator / DropResolver 에 전달)</summary>
    public List<StageSystem.TargetFarmingEntry> targetFarmingEntries;
}

/// <summary>
/// 스테이지 클리어 보상 계산 결과
/// </summary>
[System.Serializable]
public class StageClearRewardResult
{
    /// <summary>레벨 스케일링이 적용된 최종 골드</summary>
    public int gold;

    /// <summary>레벨 스케일링이 적용된 최종 EXP</summary>
    public int exp;

    /// <summary>아이템 드롭 파라미터 — 장비 드롭 확률 배율</summary>
    public float equipmentChanceMultiplier = 1f;

    /// <summary>아이템 드롭 파라미터 — 재료 수량 배율</summary>
    public float materialAmountMultiplier = 1f;

    /// <summary>드롭 가능한 최소 장비 등급</summary>
    public EquipmentRank minRarity = EquipmentRank.D;

    /// <summary>드롭 가능한 최대 장비 등급</summary>
    public EquipmentRank maxRarity = EquipmentRank.A;
}
