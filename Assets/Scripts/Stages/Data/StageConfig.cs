using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StageSystem
{
    /// <summary>
    /// 스테이지 설정 ScriptableObject
    /// StageConfig.csv 기반
    /// </summary>
    [CreateAssetMenu(fileName = "StageConfig", menuName = "Stage/Stage Config")]
    public class StageConfig : ScriptableObject
    {
        [Header("기본 정보")]
        public string StageID;
        public string StageName;
        public string SceneName;
        public BackgroundType BackgroundType;
        public bool IsDungeon;
        
        [Header("진행 조건")]
        public int RequiredLevel;
        public string UnlockCondition;
        [SerializeField] private UnlockConditionData unlockData;
        
        [Header("게임플레이")]
        public int WaveCount;
        public int TimeLimitSec;
        public VictoryCondition Victory;
        
        [Header("보상 연결")]
        public string FirstClearDropGroupId;
        public string RepeatClearDropGroupId;
        
        [Header("오디오/비주얼")]
        public string BGMPath;
        [TextArea(3, 5)]
        public string Description;
        
        [Header("런타임 참조")]
        public List<WaveConfig> WaveConfigs = new List<WaveConfig>();
        public DropTable FirstClearDropTable;
        public DropTable RepeatClearDropTable;
        
        /// <summary>
        /// 해금 조건 데이터 접근자
        /// </summary>
        public UnlockConditionData UnlockData
        {
            get
            {
                if (unlockData == null)
                    unlockData = UnlockConditionData.Parse(UnlockCondition);
                return unlockData;
            }
        }
        
        /// <summary>
        /// CSV 데이터로부터 생성
        /// </summary>
        public void InitializeFromCsv(Dictionary<string, string> csvData)
        {
            StageID = csvData.GetValueOrDefault("StageID", "");
            StageName = csvData.GetValueOrDefault("StageName", "");
            SceneName = csvData.GetValueOrDefault("SceneName", "");
            
            // Enum 파싱
            if (System.Enum.TryParse(csvData.GetValueOrDefault("BackgroundType", "FIELD"), out BackgroundType bgType))
                BackgroundType = bgType;
            
            if (System.Enum.TryParse(csvData.GetValueOrDefault("Victory", "KillAll"), out VictoryCondition victory))
                Victory = victory;
            
            // 숫자 파싱
            int.TryParse(csvData.GetValueOrDefault("RequiredLevel", "1"), out RequiredLevel);
            int.TryParse(csvData.GetValueOrDefault("WaveCount", "1"), out WaveCount);
            int.TryParse(csvData.GetValueOrDefault("TimeLimitSec", "300"), out TimeLimitSec);
            
            // 기타 필드
            bool.TryParse(csvData.GetValueOrDefault("IsDungeon", "False"), out IsDungeon);
            UnlockCondition = csvData.GetValueOrDefault("UnlockCondition", "AlwaysUnlocked");
            FirstClearDropGroupId = csvData.GetValueOrDefault("FirstClearDropGroupId", "");
            RepeatClearDropGroupId = csvData.GetValueOrDefault("RepeatClearDropGroupId", "");
            BGMPath = csvData.GetValueOrDefault("BGM Path", "");
            Description = csvData.GetValueOrDefault("Description", "");
            
            unlockData = UnlockConditionData.Parse(UnlockCondition);
        }
        
        /// <summary>
        /// 런타임에서 WaveConfig 자동 로드
        /// </summary>
        [ContextMenu("Load Wave Configs")]
        public void LoadWaveConfigs()
        {
            WaveConfigs.Clear();
            
            for (int i = 1; i <= WaveCount; i++)
            {
                string waveConfigPath = $"Stages/Waves/{StageID}_WAVE_{i:D2}_Config";
                WaveConfig waveConfig = Resources.Load<WaveConfig>(waveConfigPath);
                
                if (waveConfig != null)
                {
                    WaveConfigs.Add(waveConfig);
                    Debug.Log($"✅ [StageConfig] WaveConfig 로드 성공: {waveConfigPath}");
                }
                else
                {
                    Debug.LogWarning($"⚠️ [StageConfig] WaveConfig를 찾을 수 없습니다: {waveConfigPath}");
                }
            }
            
            Debug.Log($"🌊 [StageConfig] {StageID}: {WaveConfigs.Count}/{WaveCount} 웨이브 로드 완료");
        }
        
        /// <summary>
        /// 에디터에서 자동 검증 및 로드
        /// </summary>
        private void OnValidate()
        {
#if UNITY_EDITOR
            // 런타임이 아닐 때만 실행
            if (!Application.isPlaying && !string.IsNullOrEmpty(StageID))
            {
                // WaveConfigs가 비어있거나 개수가 맞지 않으면 자동 로드 시도
                if (WaveConfigs.Count != WaveCount)
                {
                    LoadWaveConfigs();
                }
            }
#endif
            if (!string.IsNullOrEmpty(StageID))
            {
                if (!StageIdValidator.IsValidStageId(StageID))
                {
                    Debug.LogWarning($"[StageConfig] 잘못된 StageID 형식: {StageID}");
                }
            }
        }
    }
}
