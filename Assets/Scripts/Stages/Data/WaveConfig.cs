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
        
        [Header("🌊 SimpleMob 웨이브 설정 (선택)")]
        [Tooltip("이 웨이브에서 SimpleMob을 사용할지 여부")]
        public bool UseSimpleMobWave = false;
        
        [Tooltip("SimpleMob 웨이브 데이터 (UseSimpleMobWave = true일 때)")]
        public WaveData SimpleMobWaveData;
        
        [Tooltip("SimpleMob 스폰 위치 기준 (없으면 플레이어 위치)")]
        public Transform SimpleMobSpawnCenter;
        
        [Header("런타임 참조")]
        public List<SpawnGroup> SpawnGroups = new List<SpawnGroup>();
        
        /// <summary>
        /// ⭐ 런타임에서 SpawnGroup 자동 로드 (비어있을 경우)
        /// </summary>
        public void LoadSpawnGroupsIfEmpty()
        {
            if (SpawnGroups != null && SpawnGroups.Count > 0)
            {
                // 이미 연결되어 있으면 스킵
                return;
            }
            
            if (string.IsNullOrEmpty(WaveID))
            {
                Debug.LogWarning($"[WaveConfig] WaveID가 비어있어 SpawnGroup을 로드할 수 없습니다.");
                return;
            }
            
            SpawnGroups = new List<SpawnGroup>();
            
            // Resources/Stages/Spawns/ 폴더에서 이 WaveID에 해당하는 SpawnGroup 찾기
            // 예: CH01_ST01_WAVE_01 → CH01_ST01_G01, CH01_ST01_G02, ...
            for (int i = 1; i <= 10; i++) // 최대 10개 그룹 시도
            {
                string groupId = WaveID.Replace("_WAVE_", "_G") + i.ToString("D2");
                // CH01_ST01_WAVE_01 → CH01_ST01_G01
                
                // WAVE_XX 부분 제거
                if (groupId.Contains("_WAVE_"))
                {
                    int waveIndex = groupId.LastIndexOf("_WAVE_");
                    groupId = groupId.Substring(0, waveIndex) + "_G" + i.ToString("D2");
                }
                
                string groupPath = $"Stages/Spawns/{groupId}_Config";
                SpawnGroup group = Resources.Load<SpawnGroup>(groupPath);
                
                if (group != null && group.WaveID == WaveID)
                {
                    SpawnGroups.Add(group);
                    Debug.Log($"✅ [WaveConfig] SpawnGroup 자동 로드: {groupId}");
                }
            }
            
            if (SpawnGroups.Count > 0)
            {
                Debug.Log($"🔗 [WaveConfig] {WaveID}: {SpawnGroups.Count}개 SpawnGroup 자동 연결 완료");
            }
            else
            {
                Debug.LogWarning($"⚠️ [WaveConfig] {WaveID}에 해당하는 SpawnGroup을 찾을 수 없습니다. 경로: Resources/Stages/Spawns/");
            }
        }
        
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
