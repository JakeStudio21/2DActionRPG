using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 간단한 알림 메시지 시스템
/// 화면 상단에 메시지를 표시하고 자동으로 사라짐
/// </summary>
public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }
    
    [Header("UI 참조")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TMP_Text notificationText;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("설정")]
    [SerializeField] private float displayDuration = 2f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    
    [Header("쿨타임 설정")]
    [SerializeField] private int maxDisplayCount = 2; // 최대 표시 횟수
    [SerializeField] private float cooldownTime = 30f; // 쿨타임 (초)
    
    private Coroutine currentNotificationCoroutine;
    
    // 쿨타임 관리
    private int currentDisplayCount = 0;
    private float lastResetTime = 0f;
    private bool isOnCooldown = false;
    
    private void Awake()
    {
        // 싱글톤 설정 (씬별 독립 인스턴스)
        if (Instance == null)
        {
            Instance = this;
            // ⚠️ DontDestroyOnLoad 제거: 인게임 전용 UI이므로 씬 전환 시 파괴되어야 함
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 초기화
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
        
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    /// <summary>
    /// 알림 메시지 표시 (쿨타임 적용)
    /// </summary>
    public void ShowNotification(string message, bool useCooldown = false)
    {
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("[NotificationManager] 메시지가 비어있습니다.");
            return;
        }
        
        // 쿨타임 확인 (useCooldown이 true인 경우만)
        if (useCooldown)
        {
            CheckAndResetCooldown();
            
            if (isOnCooldown)
            {
                return;
            }
            
            // 표시 횟수 증가
            currentDisplayCount++;
            
            // 최대 횟수 도달 시 쿨타임 시작
            if (currentDisplayCount >= maxDisplayCount)
            {
                isOnCooldown = true;
                lastResetTime = Time.time;
            }
        }
        
        // 이전 알림이 있으면 중단
        if (currentNotificationCoroutine != null)
        {
            StopCoroutine(currentNotificationCoroutine);
        }
        
        currentNotificationCoroutine = StartCoroutine(ShowNotificationCoroutine(message));
    }
    
    /// <summary>
    /// 쿨타임 확인 및 리셋
    /// </summary>
    private void CheckAndResetCooldown()
    {
        if (isOnCooldown && Time.time - lastResetTime >= cooldownTime)
        {
            // 쿨타임 종료 → 리셋
            isOnCooldown = false;
            currentDisplayCount = 0;
        }
    }
    
    private IEnumerator ShowNotificationCoroutine(string message)
    {
        // 텍스트 설정
        if (notificationText != null)
            notificationText.text = message;
        
        // 패널 활성화
        if (notificationPanel != null)
            notificationPanel.SetActive(true);
        
        // Fade In
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }
        
        // 표시 유지
        yield return new WaitForSeconds(displayDuration);
        
        // Fade Out
        if (canvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }
        
        // 패널 비활성화
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
        
        currentNotificationCoroutine = null;
    }
    
    /// <summary>
    /// 인벤토리 가득 찬 경우 알림 (단축 메서드, 쿨타임 적용)
    /// </summary>
    public void ShowInventoryFullNotification()
    {
        ShowNotification("인벤토리가 가득 찼습니다", useCooldown: true);
    }
}

