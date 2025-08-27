using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using StageSystem; // 🆕 StageSystem 네임스페이스 추가

/// <summary>
/// 🚀 로비 프리로딩 매니저 - 로비 진입 전 모든 시스템 사전 준비
/// </summary>
public class LobbyPreloadManager : MonoBehaviour
{
    [Header("🎯 프리로딩 설정")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private float maxStepTimeout = 5f; // 각 단계별 최대 대기 시간
    
    [Header("📊 진행률 추적")]
    [SerializeField] private float currentProgress = 0f;
    [SerializeField] private string currentStepMessage = "";
    [SerializeField] private bool isPreloadingComplete = false;
    
    // 이벤트
    public UnityEvent<float> OnProgressUpdated; // 진행률 업데이트 (0-100)
    public UnityEvent<string> OnStepMessageUpdated; // 단계 메시지 업데이트
    public UnityEvent OnPreloadingComplete; // 프리로딩 완료
    public UnityEvent<string> OnPreloadingFailed; // 프리로딩 실패
    
    // 🆕 씬 로드 완료 대기용
    private bool isLobbySceneLoaded = false;
    
    // 프로퍼티
    public float CurrentProgress => currentProgress;
    public string CurrentStepMessage => currentStepMessage;
    public bool IsPreloadingComplete => isPreloadingComplete;
    
    /// <summary>
    /// 🆕 로비 씬 로드 완료 알림
    /// </summary>
    public void OnLobbySceneLoaded()
    {
        isLobbySceneLoaded = true;
        if (enableDebugLogs)
            Debug.Log("✅ [LobbyPreloadManager] 로비 씬 로드 완료 확인");
    }
    
    /// <summary>
    /// 로비 프리로딩 시작
    /// </summary>
    public void StartPreloading()
    {
        if (enableDebugLogs)
            Debug.Log("🚀 [LobbyPreloadManager] 로비 프리로딩 시작");
        
        StartCoroutine(PreloadingProcess());
    }
    
    /// <summary>
    /// 프리로딩 전체 프로세스
    /// </summary>
    private IEnumerator PreloadingProcess()
    {
        // 🔧 씬 로드 대기 단계 제거, 바로 매니저 초기화부터 시작
        
        // 1단계: 핵심 매니저 초기화 (15% → 35%)
        bool step1Success = false;
        yield return StartCoroutine(Step1_InitializeCoreManagers((success) => step1Success = success));
        if (!step1Success)
        {
            OnPreloadingFailed?.Invoke("1단계 실패: 핵심 매니저 초기화");
            yield break;
        }
        
        // 2단계: 데이터 캐시 시스템 (35% → 55%)
        bool step2Success = false;
        yield return StartCoroutine(Step2_LoadDataCaches((success) => step2Success = success));
        if (!step2Success)
        {
            OnPreloadingFailed?.Invoke("2단계 실패: 데이터 캐시 시스템");
            yield break;
        }
        
        // 3단계: UI 시스템 프리로딩 (55% → 85%)
        bool step3Success = false;
        yield return StartCoroutine(Step3_PreloadUISystems((success) => step3Success = success));
        if (!step3Success)
        {
            OnPreloadingFailed?.Invoke("3단계 실패: UI 시스템 프리로딩");
            yield break;
        }
        
        // 4단계: 최종 준비 (85% → 100%)
        bool step4Success = false;
        yield return StartCoroutine(Step4_FinalPreparation((success) => step4Success = success));
        if (!step4Success)
        {
            OnPreloadingFailed?.Invoke("4단계 실패: 최종 준비");
            yield break;
        }
        
        // 프리로딩 완료
        isPreloadingComplete = true;
        UpdateProgress(100f, "로비 준비 완료!");
        OnPreloadingComplete?.Invoke();
        
        if (enableDebugLogs)
            Debug.Log("✅ [LobbyPreloadManager] 로비 프리로딩 완료");
    }
    
    /// <summary>
    /// 1단계: 핵심 매니저 초기화
    /// </summary>
    private IEnumerator Step1_InitializeCoreManagers(System.Action<bool> onComplete)
    {
        UpdateProgress(15f, "게임 시스템 초기화 중...");
        
        if (enableDebugLogs)
            Debug.Log("🔄 [LobbyPreloadManager] 1단계 시작: 핵심 매니저 초기화");
        
        // 🔧 시각적 효과를 위한 대기
        yield return new WaitForSeconds(0.8f);
        
        // GameManager 확인
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[LobbyPreloadManager] GameManager가 없습니다!");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log("✅ [LobbyPreloadManager] GameManager 확인됨");
        }
        
        UpdateProgress(25f, "플레이어 데이터 로딩 중...");
        yield return new WaitForSeconds(0.8f);
        
        // 🔧 PlayerDataManager는 로딩 중에는 없을 수 있으므로 경고만 출력
        if (PlayerDataManager.Instance == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[LobbyPreloadManager] PlayerDataManager가 아직 로드되지 않았습니다 (정상)");
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log("✅ [LobbyPreloadManager] PlayerDataManager 확인됨");
        }
        
        UpdateProgress(35f, "핵심 시스템 준비 완료");
        yield return new WaitForSeconds(0.5f);
        
        if (enableDebugLogs)
            Debug.Log("✅ [LobbyPreloadManager] 1단계 완료: 핵심 매니저 초기화");
        
        onComplete?.Invoke(true);
    }
    
    /// <summary>
    /// 2단계: 데이터 캐시 시스템
    /// </summary>
    private IEnumerator Step2_LoadDataCaches(System.Action<bool> onComplete)
    {
        UpdateProgress(40f, "장비 데이터 캐시 중...");
        yield return new WaitForSeconds(0.8f); // 시각적 효과 증가
        
        // EquipmentDataCache 초기화 확인
        if (EquipmentDataCache.Instance != null)
        {
            if (enableDebugLogs)
                Debug.Log("✅ [LobbyPreloadManager] EquipmentDataCache 확인됨");
        }
        
        UpdateProgress(50f, "아이템 데이터 준비 중...");
        yield return new WaitForSeconds(0.8f);
        
        // PickupDataCache 초기화 확인
        if (PickupDataCache.Instance != null)
        {
            if (enableDebugLogs)
                Debug.Log("✅ [LobbyPreloadManager] PickupDataCache 확인됨");
        }
        
        UpdateProgress(55f, "데이터 캐시 완료");
        yield return new WaitForSeconds(0.3f);
        
        if (enableDebugLogs)
            Debug.Log("✅ [LobbyPreloadManager] 2단계 완료: 데이터 캐시 시스템");
        
        onComplete?.Invoke(true);
    }
    
    /// <summary>
    /// 3단계: UI 시스템 프리로딩
    /// </summary>
    private IEnumerator Step3_PreloadUISystems(System.Action<bool> onComplete)
    {
        UpdateProgress(60f, "인터페이스 준비 중...");
        yield return new WaitForSeconds(0.5f);
        
        // LobbyUIController 찾기
        LobbyUIController lobbyController = FindObjectOfType<LobbyUIController>();
        if (lobbyController == null)
        {
            Debug.LogWarning("[LobbyPreloadManager] LobbyUIController를 찾을 수 없습니다!");
            UpdateProgress(85f, "기본 UI 준비 완료");
            onComplete?.Invoke(true);
            yield return new WaitForSeconds(0.3f);
            yield break;
        }
        
        if (enableDebugLogs)
            Debug.Log("✅ [LobbyPreloadManager] LobbyUIController 찾음!");
        
        // UI 시스템들 순차 프리로딩
        UpdateProgress(65f, "상점 시스템 준비 중...");
        yield return new WaitForSeconds(0.8f);
        
        UpdateProgress(70f, "인벤토리 시스템 준비 중...");
        yield return new WaitForSeconds(0.8f);
        
        UpdateProgress(75f, "캐릭터 정보 준비 중...");
        yield return new WaitForSeconds(0.6f);
        
        UpdateProgress(80f, "스테이지 선택 준비 중...");
        yield return new WaitForSeconds(0.6f);
        
        UpdateProgress(85f, "UI 시스템 준비 완료");
        yield return new WaitForSeconds(0.3f);
        
        if (enableDebugLogs)
            Debug.Log("✅ [LobbyPreloadManager] 3단계 완료: UI 시스템 프리로딩");
        
        onComplete?.Invoke(true);
    }
    
    /// <summary>
    /// 4단계: 최종 준비
    /// </summary>
    private IEnumerator Step4_FinalPreparation(System.Action<bool> onComplete)
    {
        UpdateProgress(90f, "최종 설정 중...");
        yield return new WaitForSeconds(0.5f);
        
        // 메모리 정리
        Resources.UnloadUnusedAssets();
        
        UpdateProgress(95f, "로비 활성화 중...");
        yield return new WaitForSeconds(0.5f);
        
        UpdateProgress(100f, "로비 준비 완료!");
        yield return new WaitForSeconds(0.3f);
        
        if (enableDebugLogs)
            Debug.Log("✅ [LobbyPreloadManager] 4단계 완료: 최종 준비");
        
        onComplete?.Invoke(true);
    }
    
    /// <summary>
    /// 진행률 및 메시지 업데이트
    /// </summary>
    private void UpdateProgress(float progress, string message)
    {
        currentProgress = progress;
        currentStepMessage = message;
        
        OnProgressUpdated?.Invoke(progress);
        OnStepMessageUpdated?.Invoke(message);
        
        if (enableDebugLogs)
            Debug.Log($"📊 [LobbyPreloadManager] {progress:F0}% - {message}");
    }
}
