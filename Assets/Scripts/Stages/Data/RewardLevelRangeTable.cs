using System.Collections.Generic;
using UnityEngine;
using ItemSystem;

namespace StageSystem
{
    /// <summary>
    /// 스테이지 레벨 구간별 보상 배율 테이블
    /// 
    /// 사용 구조:
    ///   - RewardSystem 인스펙터에 글로벌 기본값 1개 연결
    ///   - StageConfig.overrideRangeTable 이 null 이 아니면 해당 스테이지 전용값으로 덮어씀
    /// </summary>
    [CreateAssetMenu(fileName = "RewardLevelRangeTable", menuName = "Stage/Reward Level Range Table")]
    public class RewardLevelRangeTable : ScriptableObject
    {
        [Header("📊 레벨 구간 설정")]
        [Tooltip("레벨 구간을 오름차순으로 정의하세요. 구간이 겹치면 첫 번째 매칭을 사용합니다.")]
        [SerializeField] private List<LevelRangeEntry> levelRanges = new List<LevelRangeEntry>();

        // ─────────────────────────────────────────────────────────────
        // 공개 API
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 주어진 스테이지 레벨에 해당하는 구간 설정을 반환합니다.
        /// 일치하는 구간이 없으면 기본값(배율 1.0, D~A 등급)을 반환합니다.
        /// </summary>
        public LevelRangeEntry GetEntry(int stageLevel)
        {
            foreach (var entry in levelRanges)
            {
                if (stageLevel >= entry.minStageLevel && stageLevel <= entry.maxStageLevel)
                    return entry;
            }

            // 가장 높은 구간보다 레벨이 높으면 마지막 구간 적용 (엔드게임 대비)
            if (levelRanges.Count > 0)
            {
                var last = levelRanges[levelRanges.Count - 1];
                if (stageLevel > last.maxStageLevel)
                    return last;
            }

            // 아무 구간도 정의되지 않은 경우: 기본값 반환
            return LevelRangeEntry.Default;
        }

        /// <summary>
        /// Inspector 에서 구간 목록 유효성 검사
        /// </summary>
        private void OnValidate()
        {
            for (int i = 0; i < levelRanges.Count; i++)
            {
                var e = levelRanges[i];

                // minLevel 은 항상 1 이상
                if (e.minStageLevel < 1) e.minStageLevel = 1;

                // maxLevel 은 항상 minLevel 이상
                if (e.maxStageLevel < e.minStageLevel) e.maxStageLevel = e.minStageLevel;

                // 배율은 최소 1.0
                e.materialAmountMultiplier  = Mathf.Max(1f, e.materialAmountMultiplier);
                e.equipmentChanceMultiplier = Mathf.Max(0f, e.equipmentChanceMultiplier);

                // 등급 범위: min 이 max 를 초과하면 교정
                if ((int)e.minRarity > (int)e.maxRarity)
                    e.maxRarity = e.minRarity;

                levelRanges[i] = e;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // 구간별 설정 구조체
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// 레벨 구간 하나에 대한 보상 설정값
    /// </summary>
    [System.Serializable]
    public struct LevelRangeEntry
    {
        [Header("📐 레벨 구간")]
        [Tooltip("이 구간이 적용될 최소 스테이지 레벨")]
        public int minStageLevel;

        [Tooltip("이 구간이 적용될 최대 스테이지 레벨")]
        public int maxStageLevel;

        [Header("📦 재료 보상 배율")]
        [Tooltip("기본 재료 드롭 수량에 곱해지는 배율 (1.0 = 변화 없음)")]
        [Range(1f, 10f)]
        public float materialAmountMultiplier;

        [Header("⚔️ 장비 드롭 배율")]
        [Tooltip("장비 드롭 확률에 곱해지는 배율 (0.0 = 드롭 없음, 2.0 = 2배)")]
        [Range(0f, 5f)]
        public float equipmentChanceMultiplier;

        [Header("🏅 드롭 가능 장비 등급 범위")]
        [Tooltip("이 구간에서 생성 가능한 최소 장비 등급")]
        public EquipmentRank minRarity;

        [Tooltip("이 구간에서 생성 가능한 최대 장비 등급")]
        public EquipmentRank maxRarity;

        [Header("💰 골드/EXP 추가 배율")]
        [Tooltip("스테이지 클리어 골드에 곱해지는 레벨 구간 배율")]
        [Range(1f, 10f)]
        public float goldLevelMultiplier;

        [Tooltip("스테이지 클리어 EXP에 곱해지는 레벨 구간 배율")]
        [Range(1f, 10f)]
        public float expLevelMultiplier;

        /// <summary>
        /// 구간 설정이 없을 때 사용하는 안전 기본값
        /// </summary>
        public static LevelRangeEntry Default => new LevelRangeEntry
        {
            minStageLevel              = 1,
            maxStageLevel              = 999,
            materialAmountMultiplier   = 1f,
            equipmentChanceMultiplier  = 1f,
            minRarity                  = EquipmentRank.D,
            maxRarity                  = EquipmentRank.A,
            goldLevelMultiplier        = 1f,
            expLevelMultiplier         = 1f,
        };
    }
}
