using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
