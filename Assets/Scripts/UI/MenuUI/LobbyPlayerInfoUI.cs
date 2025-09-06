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
        
        Debug.Log("✅ [LobbyPlayerInfoUI] 초기화 완료");
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
        
        // 골드
        if (goldText != null)
            goldText.text = playerDataManager.CurrentGold.ToString();
        
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
        if (expSlider != null)
        {
            float expRatio = (float)playerDataManager.CurrentExp / playerDataManager.ExpToNextLevel;
            expSlider.value = expRatio;
        }
        
        if (expText != null)
            expText.text = $"{playerDataManager.CurrentExp}/{playerDataManager.ExpToNextLevel}";
    }
    
    // 이벤트 핸들러들
    private void OnSlotChanged(int slotIndex)
    {
        UpdatePlayerInfo();
    }
    
    private void OnGoldChanged(int newGold)
    {
        if (goldText != null)
            goldText.text = newGold.ToString();
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
    /// </summary>
    public void UpdatePlayerInfoFromSlotData(PlayerSlotData slotData)
    {
        if (slotData == null) return;
        
        // 🗑️ 삭제: 중복되는 캐릭터 이름 표시 제거
        // if (playerNameText != null)
        //     playerNameText.text = slotData.playerName;
        
        // ✅ 유지: 골드, 레벨, 경험치만 표시
        if (goldText != null)
            goldText.text = slotData.gold.ToString();
        
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
