using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 골드 UI를 관리하는 클래스
/// PlayerDataManager와 분리되어 순수 UI 관리만 담당
/// </summary>
public class GoldUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private bool autoFindText = true;
    
    [Header("표시 설정")]
    [SerializeField] private string goldFormat = "D3"; // 3자리 숫자 (001, 123 등)
    [SerializeField] private string goldPrefix = ""; // 골드 앞에 붙일 텍스트
    [SerializeField] private string goldSuffix = ""; // 골드 뒤에 붙일 텍스트
    
    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 내부 상태
    private bool isInitialized = false;
    private bool isGoldSubscribed = false;
    private PlayerDataManager playerDataManager;
    
    const string GOLD_AMOUNT_TEXT = "Gold Amount Text";
    
    private void Awake()
    {
        InitializeGoldText();
    }
    
    private void Start()
    {
        if (!isInitialized)
        {
            InitializeGoldText();
        }
    }
    
    private void Update()
    {
        // PlayerDataManager가 준비될 때까지 대기 후 이벤트 연결
        if (!isGoldSubscribed && PlayerDataManager.Instance != null)
        {
            ConnectToPlayerDataManager();
        }
    }
    
    /// <summary>
    /// Gold Text 초기화
    /// </summary>
    public void InitializeGoldText()
    {
        // 1. Inspector에서 할당된 텍스트 우선 사용
        if (goldText == null && autoFindText)
        {
            // 2. 자동으로 컴포넌트에서 찾기
            goldText = GetComponent<TextMeshProUGUI>();
            
            if (goldText == null)
            {
                // 3. "Gold Amount Text" 이름으로 찾기 (기존 방식 호환)
                GameObject goldTextObject = GameObject.Find(GOLD_AMOUNT_TEXT);
                if (goldTextObject != null)
                {
                    goldText = goldTextObject.GetComponent<TextMeshProUGUI>();
                }
            }
        }
        
        if (goldText != null)
        {
            isInitialized = true;
            if (showDebugLogs)
            {
                Debug.Log("✅ [GoldUI] Gold Text 초기화 완료");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("⚠️ [GoldUI] Gold Text를 찾을 수 없습니다.");
            }
        }
    }
    
    /// <summary>
    /// PlayerDataManager와 연결
    /// </summary>
    private void ConnectToPlayerDataManager()
    {
        playerDataManager = PlayerDataManager.Instance;
        if (playerDataManager != null)
        {
            // 이벤트 구독
            playerDataManager.OnGoldChanged += UpdateGoldUI;
            
            // 즉시 현재 골드 표시
            UpdateGoldUI(playerDataManager.CurrentGold);
            isGoldSubscribed = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"🔗 [GoldUI] PlayerDataManager 연결 완료: {playerDataManager.CurrentGold} 골드");
            }
        }
    }
    
    /// <summary>
    /// 골드 UI 업데이트 (외부 호출용)
    /// </summary>
    public void UpdateGoldUI(int currentGold)
    {
        if (!isInitialized || goldText == null) return;
        
        string formattedGold = currentGold.ToString(goldFormat);
        goldText.text = $"{goldPrefix}{formattedGold}{goldSuffix}";
        
        if (showDebugLogs)
        {
            Debug.Log($"💰 [GoldUI] 골드 UI 업데이트: {currentGold} → {goldText.text}");
        }
    }
    
    /// <summary>
    /// 골드 형식 설정
    /// </summary>
    public void SetGoldFormat(string format, string prefix = "", string suffix = "")
    {
        goldFormat = format;
        goldPrefix = prefix;
        goldSuffix = suffix;
        
        // 즉시 적용
        if (playerDataManager != null)
        {
            UpdateGoldUI(playerDataManager.CurrentGold);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"🔧 [GoldUI] 골드 형식 변경: {prefix}{format}{suffix}");
        }
    }
    
    /// <summary>
    /// Gold Text가 준비되었는지 확인
    /// </summary>
    public bool IsReady => isInitialized && goldText != null;
    
    /// <summary>
    /// 현재 Gold Text 참조 반환 (읽기 전용)
    /// </summary>
    public TextMeshProUGUI GoldText => goldText;
    
    private void OnDisable()
    {
        if (isGoldSubscribed && playerDataManager != null)
        {
            playerDataManager.OnGoldChanged -= UpdateGoldUI;
        }
        isGoldSubscribed = false;
    }
    
    private void OnDestroy()
    {
        if (isGoldSubscribed && playerDataManager != null)
        {
            playerDataManager.OnGoldChanged -= UpdateGoldUI;
        }
        isGoldSubscribed = false;
    }
}
