using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StageSystem
{
    /// <summary>
    /// 보스 HP바 표시 UI
    /// 보스 몬스터 등장 시 화면 상단에 HP 표시
    /// </summary>
public class BossHealthUI : MonoBehaviour
{
        [Header("UI 컴포넌트")]
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TextMeshProUGUI hpText;
        [SerializeField] private Image hpFillImage;
        
        [Header("색상 설정")]
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color damagedColor = Color.yellow;
        [SerializeField] private Color criticalColor = Color.red;
        [SerializeField] private float criticalThreshold = 0.3f; // 30% 이하 시 위험
        [SerializeField] private float damagedThreshold = 0.7f;  // 70% 이하 시 주의
        
        [Header("애니메이션 설정")]
        [SerializeField] private float hpChangeAnimSpeed = 2f;
        [SerializeField] private bool enableDamageShake = true;
        [SerializeField] private float shakeIntensity = 5f;
        [SerializeField] private float shakeDuration = 0.2f;
        
        [Header("디버그")]
        [SerializeField] private bool enableDebugLogs = false;
        
        // 보스 상태
        private GameObject currentBoss;
        private EnemyHealth bossHealth;
        private string bossName = "Boss";
        private float maxHealth;
        private float currentHealth;
        private bool isActive = false;
        
        // 애니메이션 참조
        private Coroutine hpAnimCoroutine;
        private Coroutine shakeCoroutine;
        private Vector3 originalPosition;
        
        private void Start()
        {
            // 컴포넌트 자동 찾기
            if (bossNameText == null)
                bossNameText = GetComponentInChildren<TextMeshProUGUI>();
            
            if (hpSlider == null)
                hpSlider = GetComponentInChildren<Slider>();
            
            if (hpFillImage == null && hpSlider != null)
                hpFillImage = hpSlider.fillRect.GetComponent<Image>();
            
            // 원본 위치 저장
            originalPosition = transform.localPosition;
            
            // 초기 비활성화
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 보스 설정 및 UI 활성화
        /// </summary>
        public void SetBoss(GameObject bossObject)
        {
            Debug.Log($"🐲 [BossHealthUI] SetBoss 호출됨: {bossObject?.name}");
            
            if (bossObject == null)
            {
                Debug.LogWarning("🐲 [BossHealthUI] 보스 오브젝트가 null입니다!");
                return;
            }
            
            currentBoss = bossObject;
            bossHealth = bossObject.GetComponent<EnemyHealth>();
            
            if (bossHealth == null)
            {
                Debug.LogError($"🐲 [BossHealthUI] {bossObject.name}에서 EnemyHealth 컴포넌트를 찾을 수 없습니다!");
                return;
            }
            
            // 보스 정보 설정
            bossName = bossObject.name;
            maxHealth = bossHealth.MaxHealth;
            currentHealth = bossHealth.CurrentHealth;
            
            Debug.Log($"🐲 [BossHealthUI] 보스 정보: {bossName}, HP: {currentHealth}/{maxHealth}");
            
            // UI 활성화
            gameObject.SetActive(true);
            isActive = true;
            
            Debug.Log($"🐲 [BossHealthUI] UI 활성화 완료: {gameObject.name}");
            
            // 초기 UI 설정
            UpdateBossNameDisplay();
            UpdateHealthDisplay();
            
            // 보스 HP 변화 이벤트 구독
            SubscribeToBossEvents();
            
            Debug.Log($"🐲 [BossHealthUI] 보스 설정 완료: {bossName} (HP: {currentHealth}/{maxHealth})");
        }
        
        /// <summary>
        /// 보스 이벤트 구독
        /// </summary>
        private void SubscribeToBossEvents()
        {
            if (bossHealth != null)
            {
                // EnemyHealth의 OnHealthChanged 이벤트가 있다면 구독
                // 현재는 Update에서 주기적으로 체크하는 방식 사용
                StartCoroutine(MonitorBossHealth());
            }
        }
        
        /// <summary>
        /// 보스 HP 모니터링 코루틴
        /// </summary>
        private IEnumerator MonitorBossHealth()
        {
            while (isActive && currentBoss != null && bossHealth != null)
            {
                float newHealth = bossHealth.CurrentHealth;  // currentHealth → CurrentHealth
                
                // HP 변화 감지
                if (Mathf.Abs(newHealth - currentHealth) > 0.1f)
                {
                    float previousHealth = currentHealth;
                    currentHealth = newHealth;
                    
                    // HP 감소 시 데미지 효과
                    if (newHealth < previousHealth)
                    {
                        OnBossTakeDamage(previousHealth - newHealth);
                    }
                    
                    // UI 업데이트
                    UpdateHealthDisplay();
                }
                
                // 보스 사망 체크
                if (bossHealth.isDead)
                {
                    OnBossDeath();
                    break;
                }
                
                yield return new WaitForSeconds(0.1f); // 100ms마다 체크
            }
        }
        
        /// <summary>
        /// 보스 이름 표시 업데이트
        /// </summary>
        private void UpdateBossNameDisplay()
        {
            if (bossNameText != null)
            {
                // 보스 이름에서 Clone 제거
                string displayName = bossName.Replace("(Clone)", "").Trim();
                bossNameText.text = displayName;
            }
        }
        
        /// <summary>
        ///HP 표시 업데이트
        /// </summary>
        private void UpdateHealthDisplay()
        {
            if (!isActive) return;
            
            // HP 비율 계산
            float hpRatio = maxHealth > 0 ? currentHealth / maxHealth : 0f;
            
            // 슬라이더 업데이트 (애니메이션)
            if (hpSlider != null)
            {
                if (hpAnimCoroutine != null)
                    StopCoroutine(hpAnimCoroutine);
                
                hpAnimCoroutine = StartCoroutine(AnimateHPSlider(hpSlider.value, hpRatio));
            }
            
            // HP 텍스트 업데이트
            if (hpText != null)
            {
                hpText.text = $"{Mathf.RoundToInt(currentHealth)}/{Mathf.RoundToInt(maxHealth)}";
            }
            
            // HP 색상 업데이트
            UpdateHealthColor(hpRatio);
            
            if (enableDebugLogs)
                Debug.Log($"🐲 [BossHealthUI] HP 업데이트: {currentHealth:F1}/{maxHealth:F1} ({hpRatio:P1})");
        }
        
        /// <summary>
        ///HP 슬라이더 애니메이션
        /// </summary>
        private IEnumerator AnimateHPSlider(float fromValue, float toValue)
        {
            float elapsed = 0f;
            float duration = 1f / hpChangeAnimSpeed;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                if (hpSlider != null)
                    hpSlider.value = Mathf.Lerp(fromValue, toValue, progress);
                
                yield return null;
            }
            
            if (hpSlider != null)
                hpSlider.value = toValue;
        }
        
        /// <summary>
        ///HP 비율에 따른 색상 업데이트
        /// </summary>
        private void UpdateHealthColor(float hpRatio)
        {
            Color targetColor;
            
            if (hpRatio <= criticalThreshold)
            {
                targetColor = criticalColor;
            }
            else if (hpRatio <= damagedThreshold)
            {
                targetColor = damagedColor;
            }
            else
            {
                targetColor = healthyColor;
            }
            
            // HP 바 색상 적용
            if (hpFillImage != null)
                hpFillImage.color = targetColor;
            
            // HP 텍스트 색상 적용
            if (hpText != null)
                hpText.color = targetColor;
        }
        
        /// <summary>
        /// 보스 피격 시 처리
        /// </summary>
        private void OnBossTakeDamage(float damage)
        {
            if (enableDebugLogs)
                Debug.Log($"🐲 [BossHealthUI] 보스 피격: {damage:F1} 데미지");
            
            // 화면 흔들림 효과
            if (enableDamageShake)
            {
                TriggerDamageShake();
            }
        }
        
        /// <summary>
        /// 데미지 흔들림 효과
        /// </summary>
        private void TriggerDamageShake()
        {
            if (shakeCoroutine != null)
                StopCoroutine(shakeCoroutine);
            
            shakeCoroutine = StartCoroutine(ShakeCoroutine());
        }
        
        /// <summary>
        /// 흔들림 코루틴
        /// </summary>
        private IEnumerator ShakeCoroutine()
        {
            float elapsed = 0f;
            
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                
                // 랜덤 오프셋 계산
                Vector3 randomOffset = new Vector3(
                    Random.Range(-shakeIntensity, shakeIntensity),
                    Random.Range(-shakeIntensity, shakeIntensity),
                    0f
                );
                
                transform.localPosition = originalPosition + randomOffset;
                
                yield return null;
            }
            
            // 원래 위치로 복원
            transform.localPosition = originalPosition;
        }
        
        /// <summary>
        /// 보스 사망 시 처리
        /// </summary>
        private void OnBossDeath()
        {
            if (enableDebugLogs)
                Debug.Log($"🐲 [BossHealthUI] 보스 사망: {bossName}");
            
            // UI 비활성화
            StartCoroutine(DeactivateWithDelay(2f)); // 2초 후 비활성화
        }
        
        /// <summary>
        /// 지연 후 비활성화
        /// </summary>
        private IEnumerator DeactivateWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            DeactivateBossUI();
        }
        
        /// <summary>
        /// 보스 UI 비활성화
        /// </summary>
        public void DeactivateBossUI()
        {
            isActive = false;
            
            // 코루틴 정리
            if (hpAnimCoroutine != null)
            {
                StopCoroutine(hpAnimCoroutine);
                hpAnimCoroutine = null;
            }
            
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
            }
            
            // 참조 정리
            currentBoss = null;
            bossHealth = null;
            
            // UI 비활성화
            gameObject.SetActive(false);
            
            if (enableDebugLogs)
                Debug.Log("🐲 [BossHealthUI] 보스 UI 비활성화");
        }
        
        /// <summary>
        /// 보스 강제 설정 (디버그용)
        /// </summary>
        public void SetBossManually(string name, float health, float maxHp)
        {
            bossName = name;
            currentHealth = health;
            maxHealth = maxHp;
            
            gameObject.SetActive(true);
            isActive = true;
            
            UpdateBossNameDisplay();
            UpdateHealthDisplay();
        }
        
        // 공개 속성
        public bool IsActive => isActive;
        public GameObject CurrentBoss => currentBoss;
        public string BossName => bossName;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthRatio => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    }
}
