using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ItemSystem;

namespace StageSystem
{
    /// <summary>
    /// 몬스터 스폰 데이터 구조체
    /// SpawnGroupMonster.csv 매핑
    /// </summary>
    [System.Serializable]
    public class MonsterSpawnData
    {
        [Header("몬스터 정보")]
        public string MonsterID;
        public int Count;
        public float SpawnCount;
        public bool IsBoss;
        
        [Header("⭐ Phase 1: 동적 레벨링")]
        [Tooltip("스테이지 기준 레벨에서의 레벨 오프셋 (StageBaseLevel + LevelOffset = 최종 레벨)")]
        public int LevelOffset = 0;
        
        [Header("런타임 연결")]
        public GameObject MonsterPrefab; // 런타임에 Resources.Load로 연결 (레거시 호환성)
        
        public MonsterSpawnData(string monsterId, int count, float spawnCount, bool isBoss)
        {
            MonsterID = monsterId;
            Count = count;
            SpawnCount = spawnCount;
            IsBoss = isBoss;
        }
    }
    
    /// <summary>
    /// 드롭 아이템 데이터 구조체
    /// DropItem.csv 매핑
    /// </summary>
    [System.Serializable]
    public class DropItemData
    {
        [Header("아이템 정보")]
        public string ItemID;
        public int Amount;
        [Range(0f, 1f)]
        public float DropRate;
        
        [Header("런타임 연결")]
        public GameObject ItemPrefab; // 런타임에 Resources.Load로 연결
        
        public DropItemData(string itemId, int amount, float dropRate)
        {
            ItemID = itemId;
            Amount = amount;
            DropRate = Mathf.Clamp01(dropRate);
        }
    }
    
    /// <summary>
    /// 타겟 파밍 엔트리 — 특정 장비 부위 또는 아이템 ID의 드롭률/수량을 강화합니다.
    ///
    /// 우선순위:
    ///   1. itemIdFilter 가 비어 있지 않으면 해당 아이템 ID 에만 적용
    ///   2. itemIdFilter 가 비어 있으면 slotFilter 와 일치하는 모든 장비에 적용
    ///   3. slotFilter 가 None 이면 전체 장비에 적용 (글로벌 배율)
    /// </summary>
    [System.Serializable]
    public class TargetFarmingEntry
    {
        [Header("🎯 필터 조건 (둘 다 비우면 전체 장비에 적용)")]
        [Tooltip("특정 아이템 ID 직접 지정 (예: ITEM_SWORD_KNIGHT_S)\n비어 있으면 슬롯 필터를 사용합니다.")]
        public string itemIdFilter = "";

        [Tooltip("특정 장비 슬롯 필터 (예: MainWeapon, Helmet)\n" +
                 "None 이면 슬롯 조건 없이 적용됩니다.")]
        public EquipmentSlot slotFilter = EquipmentSlot.MainWeapon;

        [Tooltip("슬롯 필터를 사용할지 여부. false 이면 slotFilter 무시")]
        public bool useSlotFilter = false;

        [Header("📈 강화 배율")]
        [Tooltip("드롭 확률(weight/chance)에 곱해지는 배율 (1.0 = 변화 없음, 2.0 = 2배)")]
        [Range(0.1f, 10f)]
        public float weightMultiplier = 1f;

        [Tooltip("드롭 수량에 곱해지는 배율 (1.0 = 변화 없음, 2.0 = 2배, 소수점은 반올림)")]
        [Range(1f, 10f)]
        public float amountMultiplier = 1f;

        /// <summary>
        /// 주어진 itemId 와 슬롯에 이 엔트리가 적용되는지 확인합니다.
        /// </summary>
        public bool Matches(string itemId, EquipmentSlot slot)
        {
            // 1. 아이템 ID 직접 매칭
            if (!string.IsNullOrEmpty(itemIdFilter))
                return string.Equals(itemId, itemIdFilter, System.StringComparison.OrdinalIgnoreCase);

            // 2. 슬롯 필터
            if (useSlotFilter)
                return slot == slotFilter;

            // 3. 전체 적용
            return true;
        }
    }

    /// <summary>
    /// 스테이지 해금 조건 파싱용 구조체
    /// </summary>
    [System.Serializable]
    public class UnlockConditionData
    {
        public bool IsAlwaysUnlocked;
        public string RequiredStageId;
        
        public static UnlockConditionData Parse(string condition)
        {
            var result = new UnlockConditionData();
            
            if (string.IsNullOrEmpty(condition) || condition == "AlwaysUnlocked")
            {
                result.IsAlwaysUnlocked = true;
            }
            else if (condition.StartsWith("Clear:"))
            {
                result.IsAlwaysUnlocked = false;
                result.RequiredStageId = condition.Substring(6); // "Clear:" 제거
            }
            
            return result;
        }
    }
}
