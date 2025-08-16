using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StageSystem
{
    /// <summary>
    /// 스테이지 제한시간 표시 UI
    /// 남은 시간 표시 및 경고 효과 처리
    /// </summary>
    public class StageTimerUI : MonoBehaviour
    {
        [Header("UI 컴포넌트")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image timerBackground;
        
        [Header("경고 효과 설정")]
        [SerializeField] private float warningThreshold = 30f; // 30초 미만 시 경고
        [SerializeField] private float criticalThreshold = 10f; // 10초 미만 시 위험
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField] private float blinkSpeed = 2f;
        
        [Header("디버그")]
        [SerializeField] private bool enableDebugLogs = false;
        
        // 타이머 상태
        private float totalTime;
        private float remainingTime;
        private bool isActive = false;
        private bool isBlinking = false;
        
        // 코루틴 참조
        private Coroutine timerCoroutine;
        private Coroutine blinkCoroutine;
        
        private void Start()
        {
            // 초기 설정
            if (timerText == null)
                timerText = GetComponentInChildren<TextMeshProUGUI>();
            
            if (timerBackground == null)
                timerBackground = GetComponent<Image>();
            
            // 초기 비활성화
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 타이머 시작
        /// </summary>
        public void StartTimer(int timeLimitSec)
        {
            totalTime = timeLimitSec;
            remainingTime = timeLimitSec;
            isActive = true;
            
            gameObject.SetActive(true);
            
            if (enableDebugLogs)
                Debug.Log($"⏰ [StageTimerUI] 타이머 시작: {timeLimitSec}초");
            
            // 기존 코루틴 정지
            if (timerCoroutine != null)
                StopCoroutine(timerCoroutine);
            
            // 타이머 코루틴 시작
            timerCoroutine = StartCoroutine(UpdateTimerCoroutine());
        }
        
        /// <summary>
        /// 타이머 정지
        /// </summary>
        public void StopTimer()
        {
            isActive = false;
            
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
            
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
            
            gameObject.SetActive(false);
            
            if (enableDebugLogs)
                Debug.Log("⏰ [StageTimerUI] 타이머 정지");
        }
        
        /// <summary>
        /// 타이머 업데이트 코루틴
        /// </summary>
        private IEnumerator UpdateTimerCoroutine()
        {
            while (isActive && remainingTime > 0)
            {
                // StageManager에서 실제 경과 시간 가져오기
                if (StageManager.Instance != null && StageManager.Instance.IsStageActive)
                {
                    float elapsedTime = StageManager.Instance.StageElapsedTime;
                    remainingTime = totalTime - elapsedTime;
                    
                    // 남은 시간이 0 이하가 되면 정지
                    if (remainingTime <= 0)
                    {
                        remainingTime = 0;
                        break;
                    }
                }
                
                // UI 업데이트
                UpdateTimerDisplay();
                UpdateTimerColor();
                
                yield return new WaitForSeconds(0.1f); // 100ms마다 업데이트
            }
            
            // 시간 종료
            OnTimerExpired();
        }
        
        /// <summary>
        /// 타이머 표시 업데이트
        /// </summary>
        private void UpdateTimerDisplay()
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }
        
        /// <summary>
        /// 타이머 색상 업데이트
        /// </summary>
        private void UpdateTimerColor()
        {
            Color targetColor = normalColor;
            bool shouldBlink = false;
            
            if (remainingTime <= criticalThreshold)
            {
                targetColor = criticalColor;
                shouldBlink = true;
            }
            else if (remainingTime <= warningThreshold)
            {
                targetColor = warningColor;
            }
            
            // 색상 적용
            if (timerText != null)
                timerText.color = targetColor;
            
            if (timerBackground != null)
                timerBackground.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0.3f);
            
            // 깜빡임 효과 처리
            if (shouldBlink && !isBlinking)
            {
                StartBlinking();
            }
            else if (!shouldBlink && isBlinking)
            {
                StopBlinking();
            }
        }
        
        /// <summary>
        /// 깜빡임 효과 시작
        /// </summary>
        private void StartBlinking()
        {
            if (blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);
            
            isBlinking = true;
            blinkCoroutine = StartCoroutine(BlinkCoroutine());
        }
        
        /// <summary>
        /// 깜빡임 효과 정지
        /// </summary>
        private void StopBlinking()
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
            
            isBlinking = false;
        }
        
        /// <summary>
        /// 깜빡임 코루틴
        /// </summary>
        private IEnumerator BlinkCoroutine()
        {
            while (isBlinking)
            {
                // 페이드 아웃
                yield return StartCoroutine(FadeAlpha(1f, 0.3f, blinkSpeed));
                
                // 페이드 인
                yield return StartCoroutine(FadeAlpha(0.3f, 1f, blinkSpeed));
            }
        }
        
        /// <summary>
        /// 알파값 페이드 코루틴
        /// </summary>
        private IEnumerator FadeAlpha(float fromAlpha, float toAlpha, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
                
                if (timerText != null)
                {
                    Color color = timerText.color;
                    color.a = alpha;
                    timerText.color = color;
                }
                
                yield return null;
            }
        }
        
        /// <summary>
        /// 타이머 만료 시 처리
        /// </summary>
        private void OnTimerExpired()
        {
            if (enableDebugLogs)
                Debug.Log("⏰ [StageTimerUI] 제한시간 종료!");
            
            // 타이머 텍스트를 00:00으로 설정
            if (timerText != null)
                timerText.text = "00:00";
        }
        
        // 공개 속성
        public float RemainingTime => remainingTime;
        public float TotalTime => totalTime;
        public bool IsActive => isActive;
        public bool IsInWarningState => remainingTime <= warningThreshold;
        public bool IsInCriticalState => remainingTime <= criticalThreshold;
    }
}
