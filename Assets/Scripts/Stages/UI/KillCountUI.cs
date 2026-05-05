using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StageSystem
{
    /// <summary>
    /// 처치수 카운트 표시 UI
    /// 현재 처치수/목표 처치수 표시 및 애니메이션 효과
    /// </summary>
    public class KillCountUI : MonoBehaviour
    {
        [Header("UI 컴포넌트")]
        [SerializeField] private TextMeshProUGUI killCountText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image progressFill;
        
        [Header("색상 설정")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color progressColor = Color.yellow;
        [SerializeField] private Color completeColor = Color.green;
        
        [Header("애니메이션 설정")]
        [SerializeField] private float countUpDuration = 0.3f;
        [SerializeField] private float scaleAnimDuration = 0.2f;
        [SerializeField] private Vector3 scaleUpSize = Vector3.one * 1.2f;
        
        [Header("디버그")]
        
        // 카운트 상태
        private int currentKillCount = 0;
        private int targetKillCount = 0;
        private bool isInitialized = false;
        
        // 애니메이션 참조
        private Coroutine countAnimCoroutine;
        private Vector3 originalScale;

        
        private void Start()
        {
            // 컴포넌트 자동 찾기
            if (killCountText == null)
                killCountText = GetComponentInChildren<TextMeshProUGUI>();
            
            if (progressSlider == null)
                progressSlider = GetComponentInChildren<Slider>();
            
            if (progressFill == null && progressSlider != null)
                progressFill = progressSlider.fillRect.GetComponent<Image>();
            
            // 원본 스케일 저장
            originalScale = transform.localScale;
            
            // 초기 비활성화
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 킬 카운트 UI 초기화
        /// </summary>
        public void Initialize()
        {
            currentKillCount = 0;
            targetKillCount = CalculateTargetKillCount();
            isInitialized = true;
            
            gameObject.SetActive(true);
            
            // UI 초기 설정
            UpdateKillCountDisplay();
            UpdateProgressSlider();
            
            // WaveController 이벤트 구독
            SubscribeToEnemyEvents();
            
        }
        
        /// <summary>
        /// 목표 처치수 사전 계산
        /// ① WaveConfig 기반: UseSimpleMobWave=true → SimpleMobWaveData 합산
        ///                    UseSimpleMobWave=false → SpawnGroups 합산
        /// ② 씬 직접 배치 WaveSpawner (방식 A): WaveConfig에서 참조되지 않은 WaveData만 합산
        ///    (WaveConfig.SimpleMobWaveData와 동일한 WaveData는 ①에서 이미 계산 → 제외)
        /// </summary>
        private int CalculateTargetKillCount()
        {
            int totalEnemies = 0;

            // ① WaveConfig 기반 계산 + 중복 방지용 WaveData Set 구성
            var waveDataCountedByConfig = new System.Collections.Generic.HashSet<WaveData>();

            if (StageManager.Instance != null && StageManager.Instance.CurrentStage != null)
            {
                var stageConfig = StageManager.Instance.CurrentStage;

                foreach (var waveConfig in stageConfig.WaveConfigs)
                {
                    if (waveConfig.UseSimpleMobWave && waveConfig.SimpleMobWaveData != null)
                    {
                        // WaveController가 제어하는 SimpleMob 웨이브 (방식 B)
                        foreach (var spawnConfig in waveConfig.SimpleMobWaveData.spawnConfigs)
                            totalEnemies += spawnConfig.spawnCount;

                        waveDataCountedByConfig.Add(waveConfig.SimpleMobWaveData);
                    }
                    else
                    {
                        // 일반 웨이브
                        foreach (var spawnGroup in waveConfig.SpawnGroups)
                            foreach (var monster in spawnGroup.Monsters)
                                totalEnemies += monster.Count;
                    }
                }
            }

            // ② 씬 직접 배치 WaveSpawner (방식 A)
            // - inactive 오브젝트 포함(true)으로 전체 탐색
            // - WaveConfig에서 이미 참조된 WaveData는 제외 (중복 방지)
            foreach (var spawner in FindObjectsOfType<WaveSpawner>(true))
            {
                if (spawner.CurrentWaveData != null
                    && !waveDataCountedByConfig.Contains(spawner.CurrentWaveData))
                {
                    totalEnemies += spawner.TotalSpawnCount;
                }
            }

            return totalEnemies;
        }
        
        /// <summary>
        /// 적 사망 이벤트 구독
        /// </summary>
        private void SubscribeToEnemyEvents()
        {
            if (StageManager.Instance != null && StageManager.Instance.WaveController != null)
                StageManager.Instance.WaveController.OnEnemyDeath += OnEnemyKilled;
        }
        
        /// <summary>
        /// 이벤트 구독 해제
        /// </summary>
        private void OnDestroy()
        {
            if (StageManager.Instance != null && StageManager.Instance.WaveController != null)
                StageManager.Instance.WaveController.OnEnemyDeath -= OnEnemyKilled;
        }
        
        /// <summary>
        /// 적 처치 이벤트 처리 (동시 처치 대응)
        /// </summary>
        private void OnEnemyKilled(GameObject enemy)
        {
            if (!isInitialized) return;
            
            // 킬 카운트 즉시 증가 (애니메이션과 분리)
            currentKillCount++;
            
            // UI 업데이트 (애니메이션 없이 즉시)
            UpdateKillCountDisplay();
            UpdateProgressSlider();
            
            // 목표 달성 체크
            CheckObjectiveComplete();
            
            // 스케일 애니메이션만 실행 (카운트 애니메이션 제거)
            StartCoroutine(ScaleAnimation());
        }
        
        /// <summary>
        /// ❌ 제거: UpdateKillCountWithAnimation 메서드
        /// 동시 처치 시 애니메이션 충돌 방지를 위해 즉시 업데이트로 변경
        /// </summary>
        
        /// <summary>
        /// ❌ 제거: CountUpAnimation 코루틴  
        /// 동시 처치 대응을 위해 카운트 애니메이션 제거
        /// </summary>
        
        /// <summary>
        /// 스케일 애니메이션 코루틴
        /// </summary>
        private IEnumerator ScaleAnimation()
        {
            // 스케일 업
            yield return StartCoroutine(ScaleToTarget(originalScale, scaleUpSize, scaleAnimDuration * 0.5f));
            
            // 스케일 다운
            yield return StartCoroutine(ScaleToTarget(scaleUpSize, originalScale, scaleAnimDuration * 0.5f));
        }
        
        /// <summary>
        /// 스케일 변경 코루틴
        /// </summary>
        private IEnumerator ScaleToTarget(Vector3 fromScale, Vector3 toScale, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                transform.localScale = Vector3.Lerp(fromScale, toScale, progress);
                
                yield return null;
            }
            
            transform.localScale = toScale;
        }
        
        /// <summary>
        /// 킬 카운트 텍스트 업데이트
        /// </summary>
        private void UpdateKillCountDisplay()
        {
            if (killCountText != null)
            {
                killCountText.text = $"적 처치: {currentKillCount}/{targetKillCount}";
                
                // 목표 달성 시 색상 변경
                if (currentKillCount >= targetKillCount)
                {
                    killCountText.color = completeColor;
                }
                else
                {
                    killCountText.color = normalColor;
                }
            }
        }
        
        /// <summary>
        /// 진행도 슬라이더 업데이트
        /// </summary>
        private void UpdateProgressSlider()
        {
            if (progressSlider != null)
            {
                float progress = targetKillCount > 0 ? (float)currentKillCount / targetKillCount : 0f;
                progressSlider.value = progress;
                
                // 진행도 색상 업데이트
                if (progressFill != null)
                {
                    if (currentKillCount >= targetKillCount)
                    {
                        progressFill.color = completeColor;
                    }
                    else
                    {
                        progressFill.color = progressColor;
                    }
                }
            }
        }
        
        /// <summary>
        /// 목표 달성 체크
        /// </summary>
        private void CheckObjectiveComplete()
        {
            if (currentKillCount >= targetKillCount)
            {
                // 목표 달성 이벤트 (필요시 추가)
                OnObjectiveComplete();
            }
        }
        
        /// <summary>
        /// 목표 달성 시 처리
        /// </summary>
        private void OnObjectiveComplete()
        {
            // 완료 효과 (추후 구현)
            // 예: 파티클 효과, 사운드 등
        }
        
        /// <summary>
        /// 킬 카운트 수동 설정 (디버그/치트용)
        /// </summary>
        public void SetKillCount(int killCount)
        {
            currentKillCount = Mathf.Clamp(killCount, 0, targetKillCount);
            UpdateKillCountDisplay();
            UpdateProgressSlider();
        }
        
        // 공개 속성
        public int CurrentKillCount => currentKillCount;
        public int TargetKillCount => targetKillCount;
        public float Progress => targetKillCount > 0 ? (float)currentKillCount / targetKillCount : 0f;
        public bool IsObjectiveComplete => currentKillCount >= targetKillCount;
    }
}
