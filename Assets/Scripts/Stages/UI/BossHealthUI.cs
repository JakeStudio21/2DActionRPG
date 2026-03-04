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
        
        [Header("⭐ 페이즈별 색상 설정")]
        [Tooltip("페이즈별 색상 사용 여부")]
        [SerializeField] private bool usePhaseColors = true;
        [Tooltip("Phase 1 색상 (70~100%)")]
        [SerializeField] private Color phase1Color = new Color(0.2f, 1f, 0.2f); // 밝은 초록
        [Tooltip("Phase 2 색상 (30~70%)")]
        [SerializeField] private Color phase2Color = new Color(1f, 0.9f, 0f); // 밝은 노랑
        [Tooltip("Phase 3 색상 (0~30%)")]
        [SerializeField] private Color phase3Color = new Color(1f, 0.2f, 0.2f); // 밝은 빨강
        
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
        private BossPhaseController phaseController; // ⭐ 추가
        private string bossName = "Boss";
        private float maxHealth;
        private float currentHealth;
        private bool isActive = false;
        private int currentPhase = 1; // ⭐ 추가: 현재 페이즈 (1, 2, 3)
        
        // 애니메이션 참조
        private Coroutine hpAnimCoroutine;
        private Coroutine shakeCoroutine;
        private Vector3 originalPosition;
        
        private void Awake()
        {
            // ⭐ Awake로 변경: 비활성화 상태에서도 컴포넌트 찾기 실행
            // 컴포넌트 자동 찾기
            if (bossNameText == null)
                bossNameText = GetComponentInChildren<TextMeshProUGUI>();
            
            if (hpSlider == null)
                hpSlider = GetComponentInChildren<Slider>();
            
            if (hpFillImage == null && hpSlider != null)
                hpFillImage = hpSlider.fillRect.GetComponent<Image>();
            
            // 원본 위치 저장
            originalPosition = transform.localPosition;
            
            // ⭐ 초기 비활성화 필수! (보스 스폰 전까지 숨김)
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 보스 설정 및 UI 활성화
        /// </summary>
        public void SetBoss(GameObject bossObject)
        {
            if (bossObject == null)
            {
                Debug.LogWarning("🐲 [BossHealthUI] 보스 오브젝트가 null입니다!");
                return;
            }
            
            if (enableDebugLogs)
                Debug.Log($"🐲 [BossHealthUI] SetBoss: {bossObject.name}");
            
            currentBoss = bossObject;
            bossHealth = bossObject.GetComponent<EnemyHealth>();
            phaseController = bossObject.GetComponent<BossPhaseController>();
            
            if (bossHealth == null)
            {
                Debug.LogError($"🐲 [BossHealthUI] {bossObject.name}에서 EnemyHealth 컴포넌트를 찾을 수 없습니다!");
                return;
            }
            
            // ⭐ 페이즈 컨트롤러 확인 (선택사항)
            if (phaseController != null)
            {
                // 페이즈 변경 이벤트 구독
                phaseController.OnPhaseChanged += OnPhaseChanged;
                currentPhase = phaseController.CurrentPhaseIndex + 1;
                
                if (enableDebugLogs)
                    Debug.Log($"🐲 [BossHealthUI] BossPhaseController 연결, Phase {currentPhase}");
            }
            
            // ⭐ 보스 정보 설정 - EnemyData의 enemyName 우선 사용
            // NotifyBossSpawned()가 1프레임 대기 후 호출되므로 EnemyData 로드 완료 보장!
            BaseEnemy baseEnemy = bossObject.GetComponent<BaseEnemy>();
            if (baseEnemy != null && baseEnemy.EnemyData != null)
            {
                bossName = baseEnemy.EnemyData.EnemyName; // ⭐ EnemyData의 한글 이름 사용
                Debug.Log($"🐲 [BossHealthUI] EnemyData 이름 사용: {bossName}");
            }
            else
            {
                bossName = bossObject.name; // Fallback: GameObject 이름
                Debug.LogWarning($"⚠️ [BossHealthUI] BaseEnemy 또는 EnemyData가 없어서 GameObject 이름 사용: {bossName}");
            }
            
            maxHealth = bossHealth.MaxHealth;
            currentHealth = bossHealth.CurrentHealth;
            
            Debug.Log($"🐲 [BossHealthUI] 보스 정보 설정 완료 - 이름: {bossName}, HP: {currentHealth}/{maxHealth}");
            
            // UI 활성화
            gameObject.SetActive(true);
            isActive = true;
            
            // 초기 UI 설정
            UpdateBossNameDisplay();
            UpdateHealthDisplay();
            
            // 보스 HP 변화 이벤트 구독
            SubscribeToBossEvents();
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
            if (enableDebugLogs)
                Debug.Log($"🐲 [BossHealthUI] MonitorBossHealth 코루틴 시작!");
            
            while (isActive && currentBoss != null && bossHealth != null)
            {
                float newHealth = bossHealth.CurrentHealth;
                
                // HP 변화 감지
                if (Mathf.Abs(newHealth - currentHealth) > 0.1f)
                {
                    float previousHealth = currentHealth;
                    currentHealth = newHealth;
                    
                    if (enableDebugLogs)
                        Debug.Log($"🐲 [BossHealthUI] HP 변화: {previousHealth:F0} → {newHealth:F0}");
                    
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
        /// ⭐ 보스 이름 한글 매핑
        /// </summary>
        private string GetLocalizedBossName(string englishName)
        {
            // Clone 제거 + 인스턴스 번호 제거 (예: _0, _1, _2)
            englishName = englishName.Replace("(Clone)", "").Trim();
            
            // ⭐ 정규식으로 _숫자 접미사 제거 (예: Boss_ForestElemental_0 → Boss_ForestElemental)
            englishName = System.Text.RegularExpressions.Regex.Replace(englishName, @"_\d+$", "");
            
            // 보스 이름 매핑
            switch (englishName)
            {
                case "Boss_SandElemental":
                    return "사막의 정령";
                
                case "Boss_ForestElemental":
                    return "숲의 정령";
                
                case "Boss_FireDragon":
                    return "화염 드래곤";
                
                case "Boss_IceQueen":
                    return "얼음 여왕";
                
                case "Boss_DarkKnight":
                    return "어둠의 기사";
                
                // 기본값: 영문 이름 그대로
                default:
                    return englishName;
            }
        }
        
        /// <summary>
        /// 보스 이름 표시 업데이트
        /// </summary>
        private void UpdateBossNameDisplay()
        {
            if (bossNameText != null)
            {
                // ⭐ EnemyData에서 이미 한글 이름을 가져왔으므로 그대로 사용
                bossNameText.text = bossName;
                
                Debug.Log($"🐲 [BossHealthUI] 보스 이름 표시: {bossName}");
            }
            else
            {
                Debug.LogWarning($"⚠️ [BossHealthUI] bossNameText가 null입니다! 보스 이름을 표시할 수 없습니다.");
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
                Debug.Log($"🐲 [BossHealthUI] HP 업데이트: {currentHealth:F0}/{maxHealth:F0} ({hpRatio:P0})");
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
        ///HP 비율에 따른 색상 업데이트 (페이즈별 색상 우선)
        /// </summary>
        private void UpdateHealthColor(float hpRatio)
        {
            Color targetColor;
            
            // ⭐ 페이즈별 색상 사용 (BossPhaseController 있을 때)
            if (usePhaseColors && phaseController != null)
            {
                targetColor = GetPhaseColor(currentPhase);
            }
            // HP 비율별 색상 (기본 방식)
            else
            {
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
            }
            
            // HP 바 색상 적용
            if (hpFillImage != null)
                hpFillImage.color = targetColor;
            
            // HP 텍스트 색상 적용
            if (hpText != null)
                hpText.color = targetColor;
        }
        
        /// <summary>
        /// ⭐ 페이즈별 색상 가져오기
        /// </summary>
        private Color GetPhaseColor(int phase)
        {
            switch (phase)
            {
                case 1: return phase1Color;
                case 2: return phase2Color;
                case 3: return phase3Color;
                default: return healthyColor;
            }
        }
        
        /// <summary>
        /// ⭐ 페이즈 변경 이벤트 핸들러
        /// </summary>
        private void OnPhaseChanged(BossPhaseData newPhase)
        {
            if (newPhase == null) return;
            
            // 페이즈 번호 추출 (Phase 1, Phase 2, Phase 3)
            string phaseName = newPhase.phaseName;
            if (phaseName.Contains("1"))
                currentPhase = 1;
            else if (phaseName.Contains("2"))
                currentPhase = 2;
            else if (phaseName.Contains("3"))
                currentPhase = 3;
            
            if (enableDebugLogs)
                Debug.Log($"🐲 [BossHealthUI] 페이즈 변경: Phase {currentPhase}");
            
            // 즉시 색상 업데이트
            float hpRatio = maxHealth > 0 ? currentHealth / maxHealth : 0f;
            UpdateHealthColor(hpRatio);
        }
        
        /// <summary>
        /// 보스 피격 시 처리
        /// </summary>
        private void OnBossTakeDamage(float damage)
        {
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
            
            // ⭐ 이벤트 구독 해제
            if (phaseController != null)
            {
                phaseController.OnPhaseChanged -= OnPhaseChanged;
            }
            
            // 참조 정리
            currentBoss = null;
            bossHealth = null;
            phaseController = null;
            
            // UI 비활성화
            gameObject.SetActive(false);
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
