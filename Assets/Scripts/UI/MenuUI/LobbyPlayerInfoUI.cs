using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyPlayerInfoUI : MonoBehaviour
{
    [Header("UI References")]
    // 🗑️ 삭제: 중복되는 playerNameText 제거
    // [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI expText;
    
    private PlayerDataManager playerDataManager;
    
    void Start()
    {
        InitializePlayerInfoUI();
    }
    
    void OnEnable()
    {
        // ⭐ V2: AccountDataManager 이벤트도 구독
        if (AccountDataManager.Instance != null)
        {
            AccountDataManager.Instance.OnGoldChanged += OnGoldChanged;
        }
        
        // 활성화 시 UI 갱신
        if (playerDataManager != null)
        {
            UpdatePlayerInfo();
        }
    }
    
    void OnDisable()
    {
        // ⭐ V2: AccountDataManager 이벤트 구독 해제
        if (AccountDataManager.Instance != null)
        {
            AccountDataManager.Instance.OnGoldChanged -= OnGoldChanged;
        }
    }
    
    private void InitializePlayerInfoUI()
    {
        // PlayerDataManager 참조
        playerDataManager = PlayerDataManager.Instance;
        
        if (playerDataManager == null)
        {
            Debug.LogError("❌ [LobbyPlayerInfoUI] PlayerDataManager를 찾을 수 없습니다!");
            return;
        }
        
        // 이벤트 구독
        playerDataManager.OnSlotSelected += OnSlotChanged;
        playerDataManager.OnGoldChanged += OnGoldChanged;
        playerDataManager.OnLevelChanged += OnLevelChanged;
        playerDataManager.OnExpChanged += OnExpChanged;
        
        // 초기 UI 업데이트
        UpdatePlayerInfo();
    }
    
    private void UpdatePlayerInfo()
    {
        if (playerDataManager == null || !playerDataManager.IsSlotSelected) 
        {
            // 선택된 슬롯 없으면 기본값 표시
            UpdateUIWithDefaults();
            return;
        }
        
        // 캐릭터 이름
        // if (playerNameText != null) // 이 부분은 삭제되었으므로 주석 처리
        //     playerNameText.text = playerDataManager.selectedPlayerData.playerName;
        
        // 💰 V2: 골드는 AccountDataManager에서 가져옴
        if (goldText != null)
        {
            int currentGold = playerDataManager.CurrentGold;
            goldText.text = currentGold.ToString();
        }
        
        // 레벨
        if (levelText != null)
            levelText.text = playerDataManager.CurrentLevel.ToString();
        
        // 경험치
        UpdateExpUI();
    }
    
    private void UpdateUIWithDefaults()
    {
        // if (playerNameText != null) // 이 부분은 삭제되었으므로 주석 처리
        //     playerNameText.text = "캐릭터를 선택하세요";
        
        if (goldText != null)
            goldText.text = "0";
        
        if (levelText != null)
            levelText.text = "1";
        
        if (expSlider != null)
            expSlider.value = 0f;
        
        if (expText != null)
            expText.text = "0/100";
    }
    
    private void UpdateExpUI()
    {
        if (playerDataManager == null) return;
        
        int currentExp = playerDataManager.CurrentExp;
        int expToNextLevel = playerDataManager.ExpToNextLevel;
        float expRatio = expToNextLevel > 0 ? (float)currentExp / expToNextLevel : 0f;
        
        if (expSlider != null)
        {
            expSlider.value = expRatio;
        }
        
        if (expText != null)
        {
            expText.text = $"{currentExp}/{expToNextLevel}";
        }
    }
    
    // 이벤트 핸들러들
    private void OnSlotChanged(int slotIndex)
    {
        UpdatePlayerInfo();
    }
    
    private void OnGoldChanged(int newGold)
    {
        if (goldText != null)
        {
            goldText.text = newGold.ToString();
        }
    }
    
    private void OnLevelChanged(int newLevel)
    {
        if (levelText != null)
            levelText.text = newLevel.ToString();
    }
    
    private void OnExpChanged(int currentExp, int expToNextLevel)
    {
        UpdateExpUI();
    }
    
    /// <summary>
    /// 슬롯 데이터로부터 직접 UI 업데이트
    /// ⭐ V2: 골드는 AccountDataManager에서 가져옴 (계정 공유)
    /// </summary>
    public void UpdatePlayerInfoFromSlotData(PlayerSlotData slotData)
    {
        if (slotData == null) return;
        
        // 🗑️ 삭제: 중복되는 캐릭터 이름 표시 제거
        // if (playerNameText != null)
        //     playerNameText.text = slotData.playerName;
        
        // ✅ V2: 골드는 AccountDataManager에서 가져옴 (계정 공유)
        if (goldText != null)
        {
            int accountGold = AccountDataManager.Instance?.CurrentGold ?? 0;
            goldText.text = accountGold.ToString();
        }
        
        if (levelText != null)
            levelText.text = slotData.level.ToString(); 
        if (expSlider != null)
        {
            float expRatio = (float)slotData.exp / slotData.expToNextLevel;
            expSlider.value = expRatio;
        }
        
        if (expText != null)
            expText.text = $"{slotData.exp}/{slotData.expToNextLevel}";
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (playerDataManager != null)
        {
            playerDataManager.OnSlotSelected -= OnSlotChanged;
            playerDataManager.OnGoldChanged -= OnGoldChanged;
            playerDataManager.OnLevelChanged -= OnLevelChanged;
            playerDataManager.OnExpChanged -= OnExpChanged;
        }
    }
}
