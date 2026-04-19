using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 📊 로딩 진행률 + UI 제어 통합 컨트롤러
/// </summary>
public class LoadingProgressController : MonoBehaviour
{
    [Header("🎨 UI 참조")]
    public Slider progressBar;
    public TextMeshProUGUI progressText; // "45%"
    public TextMeshProUGUI loadingMessage; // "게임 데이터 로딩 중..."
    public TextMeshProUGUI tipText; // 로딩 팁
    
    [Header("🎯 로딩 팁")]
    [SerializeField] private string[] loadingTips = {
        "팁: 상점에서 더 강한 장비를 구매하세요!",
        "팁: 인벤토리에서 장비를 교체할 수 있습니다!",
        "팁: 캐릭터 정보창에서 스탯을 확인하세요!",
        "팁: 스테이지를 클리어하면 새로운 지역이 해금됩니다!",
        "팁: 몬스터를 처치하면 골드와 아이템을 획득합니다!"
    };
    
    [Header("🔧 설정")]
    [SerializeField] private float tipChangeInterval = 2f; // 팁 변경 간격
    [SerializeField] private bool enableSmoothProgress = true; // 부드러운 진행률 애니메이션
    [SerializeField] private float progressSmoothSpeed = 2f;
    
    // 내부 변수
    private float targetProgress = 0f;
    private float currentDisplayProgress = 0f;
    private Coroutine tipRotationCoroutine;
    
    private void Start()
    {
        InitializeUI();
        StartTipRotation();
    }
    
    /// <summary>
    /// UI 초기화
    /// </summary>
    private void InitializeUI()
    {
        // 진행률 바 초기 설정
        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 100f;
            progressBar.value = 0f;
        }
        
        // 텍스트 초기 설정
        UpdateProgressText(0f);
        UpdateLoadingMessage("시스템 준비 중...");
        
    }
    
    /// <summary>
    /// 진행률 업데이트
    /// </summary>
    public void UpdateProgress(float progress)
    {
        targetProgress = Mathf.Clamp(progress, 0f, 100f);
        
        if (!enableSmoothProgress)
        {
            currentDisplayProgress = targetProgress;
            ApplyProgressToUI();
        }
    }
    
    /// <summary>
    /// 로딩 메시지 업데이트
    /// </summary>
    public void UpdateLoadingMessage(string message)
    {
        if (loadingMessage != null)
        {
            loadingMessage.text = message;
        }
    }
    
    /// <summary>
    /// 부드러운 진행률 애니메이션
    /// </summary>
    private void Update()
    {
        if (enableSmoothProgress && Mathf.Abs(currentDisplayProgress - targetProgress) > 0.1f)
        {
            currentDisplayProgress = Mathf.Lerp(currentDisplayProgress, targetProgress, 
                progressSmoothSpeed * Time.deltaTime);
            ApplyProgressToUI();
        }
    }
    
    /// <summary>
    /// UI에 진행률 적용
    /// </summary>
    private void ApplyProgressToUI()
    {
        if (progressBar != null)
        {
            progressBar.value = currentDisplayProgress;
        }
        
        UpdateProgressText(currentDisplayProgress);
    }
    
    /// <summary>
    /// 진행률 텍스트 업데이트
    /// </summary>
    private void UpdateProgressText(float progress)
    {
        if (progressText != null)
        {
            progressText.text = $"{progress:F0}%";
        }
    }
    
    /// <summary>
    /// 로딩 팁 순환 표시
    /// </summary>
    private void StartTipRotation()
    {
        if (tipText != null && loadingTips.Length > 0)
        {
            tipRotationCoroutine = StartCoroutine(RotateTips());
        }
    }
    
    /// <summary>
    /// 팁 순환 코루틴
    /// </summary>
    private IEnumerator RotateTips()
    {
        int currentTipIndex = 0;
        
        while (true)
        {
            if (tipText != null)
            {
                tipText.text = loadingTips[currentTipIndex];
                currentTipIndex = (currentTipIndex + 1) % loadingTips.Length;
            }
            
            yield return new WaitForSeconds(tipChangeInterval);
        }
    }
    
    /// <summary>
    /// 로딩 완료 처리
    /// </summary>
    public void OnLoadingComplete()
    {
        if (tipRotationCoroutine != null)
        {
            StopCoroutine(tipRotationCoroutine);
        }
        
        UpdateProgress(100f);
        UpdateLoadingMessage("로비 준비 완료!");
        
        if (tipText != null)
        {
            tipText.text = "게임을 시작하세요!";
        }
        
    }
    
    private void OnDestroy()
    {
        if (tipRotationCoroutine != null)
        {
            StopCoroutine(tipRotationCoroutine);
        }
    }
}
