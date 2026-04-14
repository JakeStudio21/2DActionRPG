using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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
        
        [Header("✅ Phase 0: 챕터 시스템")]
        [Tooltip("챕터 번호 (1~5), 0은 레거시 스테이지")]
        public int chapterId = 0;
        
        [Tooltip("챕터 내 스테이지 번호 (1~10)")]
        public int stageIndexInChapter = 0;
        
        [Header("🎬 컷신 설정")]
        [Tooltip("챕터 시작 컷신 ID (예: CH01_START) - 로비 Panel_Stage에서 재생, Stage 1 Config에만 설정")]
        public string chapterStartCutsceneId = "";
        
        [Tooltip("스테이지 입장 시 재생할 컷신 ID (예: CH01_ST01_ENTER) - 인게임 씬에서 재생")]
        public string enterCutsceneId = "";
        
        [Tooltip("스테이지 클리어 시 재생할 컷신 ID (예: CH01_ST01_CLEAR)")]
        public string clearCutsceneId = "";
        
        [Tooltip("재입장 시 컷신 자동 스킵 여부 (기본: true)")]
        public bool isReplaySkipCutscene = true;
        
        [Header("진행 조건")]
        public int RequiredLevel;
        public string UnlockCondition;
        [SerializeField] private UnlockConditionData unlockData;
        
    [Header("⚡ 입장 재화 (콘텐츠 입장 제한 시스템)")]
    [Tooltip("일반 스테이지 입장 시 소모되는 스태미나\n" +
             "던전(IsDungeon=true)인 경우 이 값은 무시되고 일일 횟수 제한이 적용됩니다.")]
    public int requiredStamina = 5;
    
    [Tooltip("던전 카테고리 ID (던전인 경우에만 필요)\n" +
             "예: Daily_Boss_Dungeon (정령의 가호 던전 4개 공유)\n" +
             "이 값이 비어있으면 dungeonId로부터 자동 추출됩니다.")]
    public string categoryId = "";
        
        [Header("게임플레이")]
        public int WaveCount;
        public VictoryCondition Victory;
        
        [Tooltip("Victory = ObjectiveComplete일 때 세부 목표 종류\n" +
                 "BarricadeDestroy: isVictoryTarget=true 바리케이드를 모두 파괴하면 승리\n" +
                 "ProtectObject / ItemCollect: 향후 구현 예정")]
        public ObjectiveType objectiveType = ObjectiveType.None;
        
        [Tooltip("Victory = BossKill일 때 미션 텍스트에 표시할 보스/목표 이름\n" +
                 "예: '오염된 거대멧돼지'\n" +
                 "비어있으면 '보스 처치'로 표시됨")]
        public string targetBossName = "";
        
        [Header("⭐ Phase 3: 레벨 시스템")]
        [Tooltip("스테이지 기준 레벨 (몬스터 기본 레벨)\n" +
                 "MonsterSpawnData의 LevelOffset과 합산되어 최종 레벨 결정\n" +
                 "예: StageBaseLevel=5, LevelOffset=2 → 몬스터 레벨 7")]
        public int StageBaseLevel = 1;
        
        [Header("⏱️ 타임리미트 설정")]
        [Tooltip("타임리미트 활성화 여부 (KillAll/BossKill과 조합 가능)")]
        public bool hasTimeLimit = false;
        
        [Tooltip("제한시간 (초)\n" +
                 "- Survival: 목표 시간 (도달 시 승리)\n" +
                 "- KillAll/BossKill + hasTimeLimit=true: 실패 시간 (초과 시 패배)\n" +
                 "- KillAll/BossKill + hasTimeLimit=false: 무시됨 (시간 제한 없음)")]
        public int TimeLimitSec;
        
        [Header("보상 연결")]
        public string FirstClearDropGroupId;
        public string RepeatClearDropGroupId;
        
        [Header("오디오/비주얼")]
        public string BGMPath;
        [TextArea(3, 5)]
        public string Description;
        
        [Header("🌫️ Volume 이펙트")]
        [Tooltip("스테이지 진행 중 적용할 Volume Profile (안개·어두운 연출)\nnull이면 Volume 효과 없음")]
        public VolumeProfile fogVolumeProfile;
        
        [Tooltip("스테이지 클리어 후 전환될 Volume Profile\nnull이면 fogVolumeProfile이 페이드 아웃만 됨")]
        public VolumeProfile clearVolumeProfile;
        
        [Tooltip("클리어 시 Volume 전환 시간 (초)")]
        public float volumeTransitionDuration = 2f;
        
        [Header("🎯 타겟 파밍 설정 (특정 아이템 드롭률 강화)")]
        [Tooltip("이 스테이지에서 특정 부위/아이템의 드롭률을 올려주는 설정 목록.\n" +
                 "비어 있으면 타겟 파밍 보정 없이 기본 확률을 사용합니다.")]
        public List<TargetFarmingEntry> targetFarmingEntries = new List<TargetFarmingEntry>();

        [Header("📊 레벨 구간 보상 테이블 (오버라이드)")]
        [Tooltip("null 이면 RewardSystem의 글로벌 기본 테이블을 사용합니다.\n" +
                 "이 스테이지에만 다른 보상 구간을 적용하고 싶을 때 할당하세요.")]
        public RewardLevelRangeTable overrideRangeTable;

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
                    
                    // SpawnGroups가 비어있으면 자동 로드 시도
                    if (waveConfig.SpawnGroups == null || waveConfig.SpawnGroups.Count == 0)
                    {
                        Debug.LogWarning($"[StageConfig] {waveConfigPath}: SpawnGroups가 비어있습니다. 자동 로드 시도...");
                        waveConfig.LoadSpawnGroupsIfEmpty();
                        
                        if (waveConfig.SpawnGroups.Count == 0)
                        {
                            Debug.LogError($"[StageConfig] {waveConfigPath}: SpawnGroups 자동 로드 실패! asset 파일의 SpawnGroups 리스트를 수동으로 연결하세요.");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[StageConfig] WaveConfig를 찾을 수 없습니다: {waveConfigPath}");
                }
            }
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
                
                // ✅ Phase 0: 챕터 스테이지 ID 자동 파싱
                if (StageIdValidator.IsValidChapterStageId(StageID))
                {
                    chapterId = StageIdValidator.ExtractChapterId(StageID);
                    stageIndexInChapter = StageIdValidator.ExtractStageIndex(StageID);
                    
                    Debug.Log($"✅ [StageConfig] {StageID} → Chapter {chapterId}, Stage {stageIndexInChapter}");
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
