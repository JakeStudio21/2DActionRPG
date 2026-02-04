using UnityEngine;
using Systems;
using UI.Popups;

/// <summary>
/// 귀속 경고 관리자
/// - 아이템 장착 시 귀속 경고 팝업을 표시하고 사용자 응답을 처리
/// </summary>
public class BindWarningManager : MonoBehaviour
{
    private static BindWarningManager _instance;
    public static BindWarningManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("BindWarningManager");
                _instance = go.AddComponent<BindWarningManager>();
                DontDestroyOnLoad(go);
                Debug.Log("✨ [BindWarningManager] 자동 생성 및 초기화");
            }
            return _instance;
        }
    }
    
    [Header("UI References")]
    [SerializeField] private BindWarningPopup bindWarningPopup;
    
    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        InitializePopup();
    }
    
    /// <summary>
    /// 팝업 초기화
    /// </summary>
    private void InitializePopup()
    {
        if (bindWarningPopup == null)
        {
            bindWarningPopup = FindObjectOfType<BindWarningPopup>();
            
            if (bindWarningPopup == null)
            {
                LogWarning("[BindWarningManager] BindWarningPopup을 찾을 수 없습니다. 씬에 추가해주세요.");
            }
            else
            {
                Log("[BindWarningManager] BindWarningPopup 자동 검색 완료");
            }
        }
    }
    
    /// <summary>
    /// 귀속 경고가 필요한지 확인
    /// </summary>
    public bool ShouldShowWarning(ItemInstanceId itemId)
    {
        // '다시 보지 않기' 설정 확인
        if (BindWarningPopup.IsDontShowAgain())
        {
            Log($"[BindWarningManager] '다시 보지 않기' 설정으로 경고 스킵: {itemId.id.Substring(0, 8)}...");
            return false;
        }
        
        var account = AccountDataManager.Instance;
        var bindInfo = account.GetBindInfo(itemId);
        
        // 이미 귀속된 아이템은 경고 불필요
        if (bindInfo.isBound)
        {
            Log($"[BindWarningManager] 이미 귀속된 아이템: {itemId.id.Substring(0, 8)}... (슬롯 {bindInfo.characterSlotIndex})");
            return false;
        }
        
        // 아직 귀속되지 않은 아이템은 경고 필요
        return true;
    }
    
    /// <summary>
    /// 귀속 경고 표시 및 사용자 응답 대기
    /// </summary>
    public void ShowWarningAndWaitForResponse(BindWarningData warningData, System.Action<bool> onUserResponse)
    {
        if (bindWarningPopup == null)
        {
            InitializePopup();
            
            if (bindWarningPopup == null)
            {
                LogError("[BindWarningManager] BindWarningPopup이 없어 경고를 표시할 수 없습니다. 자동으로 승인합니다.");
                onUserResponse?.Invoke(true);
                return;
            }
        }
        
        Log($"[BindWarningManager] 귀속 경고 표시: {warningData.itemTemplateName}+{warningData.enhancementLevel}");
        bindWarningPopup.Show(warningData, onUserResponse);
    }
    
    /// <summary>
    /// 다른 캐릭터에 귀속된 아이템 장착 시도 경고
    /// </summary>
    public void ShowAlreadyBoundWarning(ItemInstanceId itemId, int boundToSlot, string boundCharacterName)
    {
        string message = $"이 아이템은 이미 <color=red>{boundCharacterName}</color>에게 귀속되어 있습니다.\n\n" +
                        $"다른 캐릭터는 장착할 수 없습니다.";
        
        LogWarning($"[BindWarningManager] 다른 캐릭터 귀속 아이템: {itemId.id.Substring(0, 8)}... → 슬롯 {boundToSlot}");
        
        // TODO: 간단한 알림 팝업 표시 (확인 버튼만)
        Debug.LogWarning(message);
    }
    
    /// <summary>
    /// 귀속 아이템 계정 창고 이동 시도 경고
    /// </summary>
    public void ShowCannotMoveToStorageWarning(ItemInstanceId itemId, string characterName)
    {
        string message = $"이 아이템은 <color=red>{characterName}</color>에게 귀속되어 있습니다.\n\n" +
                        $"귀속된 아이템은 계정 창고로 이동할 수 없습니다.";
        
        LogWarning($"[BindWarningManager] 귀속 아이템 창고 이동 불가: {itemId.id.Substring(0, 8)}...");
        
        // TODO: 간단한 알림 팝업 표시 (확인 버튼만)
        Debug.LogWarning(message);
    }
    
    #region Debug Helpers
    
    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }
    
    private void LogWarning(string message)
    {
        if (enableDebugLogs)
        {
            Debug.LogWarning(message);
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError(message);
    }
    
    #endregion
}

