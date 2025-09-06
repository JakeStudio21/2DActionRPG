using UnityEngine;
using System.Collections.Generic;

namespace StageSystem
{
    /// <summary>
    /// 웨이브 설정 ScriptableObject
    /// WaveConfig.csv 기반
    /// </summary>
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "Stage/Wave Config")]
    public class WaveConfig : ScriptableObject
    {
        [Header("기본 정보")]
        public string WaveID;
        public string StageID;
        public int WaveIndex;
        
        [Header("시작 조건")]
        public WaveStartCondition StartCondition;
        public int WaveDelaySec;
        public WaveTriggerId TriggerId;

        [Header("✅ Boss Gate 설정")]
        public bool EnablesBossGate = false;        // 이 웨이브 완료 시 Boss Gate 활성화 여부
        public string BossGateTag = "BossGate";     // 활성화할 Boss Gate 태그
        public WaveTriggerId TriggerToActivate = WaveTriggerId.BossGateOpened; // 활성화할 트리거 ID
        
        [Header("런타임 참조")]
        public List<SpawnGroup> SpawnGroups = new List<SpawnGroup>();
        
        /// <summary>
        /// CSV 데이터로부터 생성
        /// </summary>
        public void InitializeFromCsv(Dictionary<string, string> csvData)
        {
            WaveID = csvData.GetValueOrDefault("WaveID", "");
            StageID = csvData.GetValueOrDefault("StageID", "");
            
            int.TryParse(csvData.GetValueOrDefault("WaveIndex", "1"), out WaveIndex);
            int.TryParse(csvData.GetValueOrDefault("WaveDelaySec", "0"), out WaveDelaySec);
            
            // Enum 파싱
            if (System.Enum.TryParse(csvData.GetValueOrDefault("StartCondition", "AutoAfterDelay"), out WaveStartCondition condition))
                StartCondition = condition;
            
            if (System.Enum.TryParse(csvData.GetValueOrDefault("triggerId", "None"), out WaveTriggerId trigger))
                TriggerId = trigger;
        }
        
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(WaveID))
            {
                if (!StageIdValidator.IsValidWaveId(WaveID))
                {
                    Debug.LogWarning($"[WaveConfig] 잘못된 WaveID 형식: {WaveID}");
                }
            }
        }
    }
}
