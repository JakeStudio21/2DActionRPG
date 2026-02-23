using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 스폰 그룹 ScriptableObject
    /// SpawnGroup.csv + SpawnGroupMonster.csv 기반
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnGroup", menuName = "Stage/Spawn Group")]
    public class SpawnGroup : ScriptableObject
    {
        [Header("기본 정보")]
        public string SpawnGroupID;
        public string WaveID;
        
        [Header("타이밍")]
        public float SpawnDelaySec;
        public float RepeatCount;
        public float RepeatIntervalSec;
        
        [Header("몬스터 구성")]
        public List<MonsterSpawnData> Monsters = new List<MonsterSpawnData>();
        
        /// <summary>
        /// CSV 데이터로부터 생성 (SpawnGroup.csv)
        /// </summary>
        public void InitializeFromCsv(Dictionary<string, string> csvData)
        {
            SpawnGroupID = csvData.GetValueOrDefault("SpawnGroupID", "");
            WaveID = csvData.GetValueOrDefault("WaveID", "");
            
            // 🚫 제거: PathId, SpawnType, Radius (SpawnPoint에서 관리)
            
            // 숫자 파싱 (타이밍만)
            float.TryParse(csvData.GetValueOrDefault("SpawnDelaySec", "0"), out SpawnDelaySec);
            float.TryParse(csvData.GetValueOrDefault("RepeatCount", "1"), out RepeatCount);
            float.TryParse(csvData.GetValueOrDefault("RepeatIntervalSec", "0"), out RepeatIntervalSec);
        }
        
        /// <summary>
        /// 몬스터 데이터 추가 (SpawnGroupMonster.csv)
        /// ⭐ Phase 1: levelOffset 파라미터 추가
        /// </summary>
        public void AddMonsterData(string monsterId, int count, float spawnCount, bool isBoss, int levelOffset = 0)
        {
            var monsterData = new MonsterSpawnData(monsterId, count, spawnCount, isBoss);
            monsterData.LevelOffset = levelOffset; // ⭐ Phase 1: 동적 레벨 오프셋 설정
            Monsters.Add(monsterData);
        }
        
        /// <summary>
        /// 총 몬스터 수 계산
        /// </summary>
        public int GetTotalMonsterCount()
        {
            int total = 0;
            foreach (var monster in Monsters)
            {
                total += monster.Count;
            }
            return total;
        }
        
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(SpawnGroupID))
            {
                if (!StageIdValidator.IsValidGroupId(SpawnGroupID))
                {
                    Debug.LogWarning($"[SpawnGroup] 잘못된 GroupID 형식: {SpawnGroupID}");
                }
            }
        }
    }
}
