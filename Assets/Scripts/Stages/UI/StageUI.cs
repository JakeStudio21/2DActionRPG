using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StageSystem
{
    /// <summary>
    /// 스테이지 UI 통합 관리자
    /// StageManager 이벤트를 구독하여 각종 UI 업데이트 처리
    /// </summary>
    public class StageUI : MonoBehaviour
    {
        [Header("UI 컴포넌트 참조")]
        [SerializeField] private GameObject timerPanel;
        [SerializeField] private GameObject bossPanel;
        [SerializeField] private GameObject objectivePanel;
        [SerializeField] private GameObject victoryConditionPanel;
        
        [Header("개별 UI 컨트롤러")]
        [SerializeField] private StageTimerUI stageTimerUI;
        [SerializeField] private BossHealthUI bossHealthUI;  // ⭐ Inspector에서 직접 연결
        [SerializeField] private KillCountUI killCountUI;
        
        [Header("승리 조건 표시")]
        [SerializeField] private TextMeshProUGUI victoryConditionText;
        
        [Header("⚙️ 설정 버튼")]
        [Tooltip("HUD의 설정 버튼 (OnClick에서 OnSettingsButtonClicked 연결)")]
        [SerializeField] private SettingsUIController settingsUIController;

        [Header("디버그")]
        [SerializeField] private bool enableDebugLogs = true;
        
        // 현재 상태
        private StageConfig currentStage;
        private bool isInitialized = false;
        
        private void Awake()
        {
            // ⭐ 컴포넌트 null 체크 및 경고
            ValidateRequiredComponents();
        }
        
        private void Start()
        {
            InitializeUI();
            SubscribeToStageEvents();
            
            // ⭐ 테스트 함수 비활성화 (실제 보스 스폰 시스템 사용)
            // StartCoroutine(TestBossUI());
        }
        
        /// <summary>
        /// 필수 컴포넌트 검증
        /// </summary>
        private void ValidateRequiredComponents()
        {
            if (bossHealthUI == null)
            {
                Debug.LogError("📱 [StageUI] BossHealthUI가 Inspector에서 연결되지 않았습니다!");
                Debug.LogError("📱 [StageUI] Canvas의 StageUI 컴포넌트에서 Boss Health UI 필드에 BossPanel 할당 필요!");
            }
            else
            {
                Debug.Log("📱 [StageUI] BossHealthUI 연결 확인됨: " + bossHealthUI.name);
            }
            
            if (stageTimerUI == null)
                Debug.LogWarning("📱 [StageUI] StageTimerUI가 Inspector에서 연결되지 않았습니다!");
            
            if (killCountUI == null)
                Debug.LogWarning("📱 [StageUI] KillCountUI가 Inspector에서 연결되지 않았습니다!");
        }
        
        /// <summary>
        /// UI 초기화 (간소화)
        /// </summary>
        private void InitializeUI()
        {
            // ⭐ 패널은 필요 시에만 SetActive (초기 비활성화 제거)
            // 모든 패널을 초기에 비활성화하지 않음
            
            // 개별 UI 컨트롤러는 이미 Inspector에서 연결됨
            isInitialized = true;
            
            if (enableDebugLogs)
                Debug.Log("📱 [StageUI] UI 초기화 완료 (직접 참조 방식)");
        }
        
        /// <summary>
        /// StageManager 이벤트 구독
        /// </summary>
        private void SubscribeToStageEvents()
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.OnStageStarted += OnStageStarted;
                StageManager.Instance.OnStageCompleted += OnStageCompleted;
                StageManager.Instance.OnWaveChanged += OnWaveChanged;
                StageManager.Instance.OnDisplayWaveIndexChanged += OnDisplayWaveIndexChanged;
                
                // ⭐ 보스 스폰 이벤트 구독 추가 (누락되었던 부분)
                StageManager.Instance.OnBossSpawned += OnBossSpawned;
                
                Debug.Log("📱 [StageUI] StageManager 이벤트 구독 완료 (보스 이벤트 포함)");
            }
            else
            {
                Debug.LogError("📱 [StageUI] StageManager.Instance가 null입니다!");
            }
        }
        
        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        private void OnDestroy()
        {
            if (StageManager.Instance != null)
            {
                StageManager.Instance.OnStageStarted -= OnStageStarted;
                StageManager.Instance.OnStageCompleted -= OnStageCompleted;
                StageManager.Instance.OnWaveChanged -= OnWaveChanged;
                StageManager.Instance.OnDisplayWaveIndexChanged -= OnDisplayWaveIndexChanged;
                
                // ⭐ 보스 스폰 이벤트 구독 해제 추가
                StageManager.Instance.OnBossSpawned -= OnBossSpawned;
            }
        }
        
        /// <summary>
        /// 스테이지 시작 이벤트 처리
        /// </summary>
        private void OnStageStarted(StageConfig stageConfig)
        {
            currentStage = stageConfig;
            
            if (enableDebugLogs)
            {
                Debug.Log($"📱 [StageUI] 스테이지 UI 활성화: {stageConfig.StageID}");
                Debug.Log($"📱 [StageUI] Victory: {stageConfig.Victory}, hasTimeLimit: {stageConfig.hasTimeLimit}");
                Debug.Log($"📱 [StageUI] TimeLimitSec: {stageConfig.TimeLimitSec}");
            }
            
            // 승리 조건별 UI 설정
            SetupUIForVictoryCondition(stageConfig.Victory);
            
            // ✅ 타이머 UI 활성화 조건 개선
            bool shouldShowTimer = false;
            bool isCountdown = false;
            
            if (stageConfig.Victory == VictoryCondition.Survival)
            {
                // Survival: 항상 타이머 표시 (카운트다운)
                shouldShowTimer = true;
                isCountdown = true; // Survival도 카운트다운 방식
            }
            else if (stageConfig.hasTimeLimit && stageConfig.TimeLimitSec > 0)
            {
                // KillAll/BossKill + TimeLimit: 카운트다운 타이머
                shouldShowTimer = true;
                isCountdown = true;
            }
            
            if (shouldShowTimer)
            {
                if (enableDebugLogs)
                    Debug.Log($"📱 [StageUI] 타이머 활성화: {stageConfig.TimeLimitSec}초 (카운트다운: {isCountdown})");
                
                ActivateTimerUI(stageConfig.TimeLimitSec);
            }
            
            // 처치 목표가 있는 경우 킬 카운트 활성화
            if (stageConfig.Victory == VictoryCondition.KillAll)
            {
                if (enableDebugLogs)
                    Debug.Log("📱 [StageUI] 킬 카운트 UI 활성화");
                ActivateKillCountUI();
            }
        }
        
        /// <summary>
        /// 승리 조건별 UI 설정
        /// </summary>
        private void SetupUIForVictoryCondition(VictoryCondition condition)
        {
            if (victoryConditionPanel != null)
            {
                victoryConditionPanel.SetActive(true);
                
                string conditionText = "";
                
                // ✅ 타임리미트 조합 처리
                if (currentStage.hasTimeLimit)
                {
                    int minutes = currentStage.TimeLimitSec / 60;
                    int seconds = currentStage.TimeLimitSec % 60;
                    string timeText = minutes > 0
                        ? (seconds > 0 ? $"{minutes}분 {seconds}초" : $"{minutes}분")
                        : $"{seconds}초";
                    
                    conditionText = condition switch
                    {
                        VictoryCondition.KillAll => $"목표: {timeText} 안에 모든 적 처치 ",
                        VictoryCondition.BossKill => $"목표: {timeText} 안에 보스 처치 ",
                        VictoryCondition.Survival => $"목표: {timeText} 생존",
                        VictoryCondition.ObjectiveComplete => $"목표: {timeText} 안에 {GetObjectiveLabel(currentStage.objectiveType)} ",
                        _ => "목표: 스테이지 클리어"
                    };
                }
                else
                {
                    // 타임리미트 없음
                    conditionText = condition switch
                    {
                        VictoryCondition.KillAll => "목표: 모든 적 처치",
                        VictoryCondition.BossKill => "목표: 보스 처치",
                        VictoryCondition.Survival => "목표: 제한시간 생존",
                        VictoryCondition.ObjectiveComplete => $"목표: {GetObjectiveLabel(currentStage.objectiveType)}",
                        _ => "목표: 스테이지 클리어"
                    };
                }
                
                if (victoryConditionText != null)
                    victoryConditionText.text = conditionText;
            }
        }
        
        /// <summary>
        /// ObjectiveType → UI 표시 텍스트 변환
        /// </summary>
        private string GetObjectiveLabel(ObjectiveType type)
        {
            return type switch
            {
                ObjectiveType.BarricadeDestroy => "바리케이드 파괴",
                ObjectiveType.ProtectObject    => "오브젝트 방어",
                ObjectiveType.ItemCollect      => "아이템 수집",
                _                              => "특수 미션 완료"
            };
        }
        
        /// <summary>
        /// 타이머 UI 활성화
        /// </summary>
        private void ActivateTimerUI(int timeLimitSec)
        {
            if (timerPanel != null)
                timerPanel.SetActive(true);
            
            if (stageTimerUI != null)
                stageTimerUI.StartTimer(timeLimitSec);
        }
        
        /// <summary>
        /// 킬 카운트 UI 활성화
        /// </summary>
        private void ActivateKillCountUI()
        {
            if (objectivePanel != null)
                objectivePanel.SetActive(true);
            
            if (killCountUI != null)
                killCountUI.Initialize();
        }
        
        /// <summary>
        /// 보스 HP UI 활성화 (간소화)
        /// </summary>
        public void ActivateBossHealthUI(GameObject bossObject)
        {
            if (enableDebugLogs)
                Debug.Log($"🐲 [StageUI] 보스 HP UI 활성화 시도: {bossObject?.name}");
            
            // ⭐ null 체크 (Awake에서 이미 검증됨)
            if (bossHealthUI == null)
            {
                Debug.LogError("🐲 [StageUI] BossHealthUI가 null입니다! Inspector 설정을 확인해주세요!");
                return;
            }
            
            if (bossPanel != null)
            {
                bossPanel.SetActive(true);  // ⭐ 필요 시에만 활성화
                if (enableDebugLogs)
                    Debug.Log($"🐲 [StageUI] BossPanel 활성화 완료");
            }
            
            bossHealthUI.SetBoss(bossObject);
            if (enableDebugLogs)
                Debug.Log($"🐲 [StageUI] BossHealthUI.SetBoss() 호출 완료");
        }
        
        /// <summary>
        /// 웨이브 변경 이벤트 처리
        /// </summary>
        private void OnWaveChanged(WaveConfig waveConfig)
        {
            if (enableDebugLogs)
                Debug.Log($"📱 [StageUI] 웨이브 변경: {waveConfig.WaveID}");
        }
        
        /// <summary>
        /// UI용 웨이브 진행 이벤트 처리 (실제 스폰 시점에만 발생)
        /// Wave 카운터 UI가 있으면 여기서 업데이트
        /// </summary>
        private void OnDisplayWaveIndexChanged(int current, int total)
        {
            if (enableDebugLogs)
                Debug.Log($"📱 [StageUI] 웨이브 표시 업데이트: {current} / {total}");
            
            // TODO: Wave 카운터 TextMeshPro가 있으면 여기서 업데이트
            // 예: waveCounterText.text = $"Wave {current} / {total}";
        }
        
        /// <summary>
        /// 보스 스폰 이벤트 처리 (신규 추가)
        /// </summary>
        private void OnBossSpawned(GameObject bossObject)
        {
            if (enableDebugLogs)
                Debug.Log($"🐲 [StageUI] 보스 스폰 이벤트 수신: {bossObject.name}");
            
            // 보스 HP UI 활성화
            ActivateBossHealthUI(bossObject);
        }
        
        /// <summary>
        /// 스테이지 완료 이벤트 처리
        /// </summary>
        private void OnStageCompleted(StageConfig stageConfig, bool success)
        {
            if (enableDebugLogs)
                Debug.Log($"📱 [StageUI] 스테이지 {(success ? "성공" : "실패")}: {stageConfig.StageID}");
            
            // 모든 UI 비활성화
            if (timerPanel != null) timerPanel.SetActive(false);
            if (bossPanel != null) bossPanel.SetActive(false);
            if (objectivePanel != null) objectivePanel.SetActive(false);
            if (victoryConditionPanel != null) victoryConditionPanel.SetActive(false);
        }
        
        // ───────────────────────────────────────────
        //  HUD 버튼 핸들러
        // ───────────────────────────────────────────

        /// <summary>
        /// HUD 설정 버튼 OnClick에 연결.
        /// 게임을 일시정지하고 설정 패널을 열어준다.
        /// </summary>
        public void OnSettingsButtonClicked()
        {
            if (settingsUIController != null)
                settingsUIController.Open(SettingsOpenMode.FromHUD);
            else
                Debug.LogWarning("[StageUI] SettingsUIController가 연결되지 않았습니다.");
        }

        // 공개 속성
        public StageConfig CurrentStage => currentStage;
        public bool IsInitialized => isInitialized;

        private IEnumerator TestBossUI()
        {
            yield return new WaitForSeconds(5f);
            
            // 보스 HP UI 수동 활성화 테스트
            if (bossHealthUI != null)
            {
                bossHealthUI.SetBossManually("Test Boss", 800f, 1000f);
                Debug.Log("🐲 [StageUI] 보스 UI 테스트 활성화");
            }
        }
    }
}
