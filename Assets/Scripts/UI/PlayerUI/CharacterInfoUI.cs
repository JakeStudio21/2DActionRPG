using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 🎮 로비 캐릭터 정보창 UI 시스템
/// EquippedItemsPanel을 활용하여 보유 캐릭터 정보를 표시하고 슬롯 선택 기능 제공
/// </summary>
public class CharacterInfoUI : MonoBehaviour
{
    [Header("🎮 캐릭터 슬롯 버튼들")]
    [SerializeField] private Button[] characterSlotButtons = new Button[3]; // 슬롯 0, 1, 2 버튼 배열
    [SerializeField] private GameObject[] slotPanels = new GameObject[3]; // 각 슬롯 패널
    
    [Header("🎭 캐릭터 슬롯별 UI 요소들")]
    [SerializeField] private TMP_Text[] slotNameTexts = new TMP_Text[3];     // 각 슬롯별 캐릭터 이름
    [SerializeField] private TMP_Text[] slotLevelTexts = new TMP_Text[3];    // 각 슬롯별 레벨
    [SerializeField] private TMP_Text[] slotClassTexts = new TMP_Text[3];    // 각 슬롯별 클래스명
    [SerializeField] private Image[] slotClassIcons = new Image[3];          // 각 슬롯별 클래스 아이콘
    [SerializeField] private GameObject[] slotEmptyPanels = new GameObject[3]; // 빈 슬롯 표시 패널
    
    [Header("🎒 착용 장비 표시 (EquippedItemsPanel 연동)")]
    [SerializeField] private LobbyEquippedItemsUI equippedItemsUI; // 기존 EquippedItemsPanel 활용
    
    [Header("🎮 플레이어 정보 표시 (추가 정보)")]
    [SerializeField] private TMP_Text playerLevelText;     // 🆕 레벨 정보
    [SerializeField] private TMP_Text playerHealthText;    // 🆕 체력 정보
    [SerializeField] private TMP_Text playerNameText;      // 캐릭터명
    [SerializeField] private Image playerClassIcon;        // 클래스 이미지
    
    [Header("🎨 클래스별 아이콘")]
    [SerializeField] private Sprite warriorClassIcon;
    [SerializeField] private Sprite assassinClassIcon; 
    [SerializeField] private Sprite wizardClassIcon;
    [SerializeField] private Sprite emptySlotIcon;
    
    [Header("🔄 패널 제어")]
    [SerializeField] private Button closePanelButton;      // 로비로 돌아가기 버튼
    [SerializeField] private GameObject emptySlotDisplay; // 🆕 빈 슬롯 클릭 시 표시할 이미지 (2개 영역 포함)
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 내부 상태
    private int currentSelectedSlot = -1; // 현재 선택된 슬롯 (-1: 미선택)
    
    private void Start()
    {
        // 🔧 로비에서만 활성화
        if (SceneManager.GetActiveScene().name != "Lobby" && 
            !SceneManager.GetActiveScene().name.Contains("Lobby"))
        {
            this.enabled = false;
            if (showDebugLogs)
                Debug.Log("🔒 [CharacterInfoUI] 인게임에서 비활성화됨");
            return;
        }
        
        InitializeCharacterInfoUI();
    }
    
    private void OnEnable()
    {
        // 🆕 패널이 활성화될 때마다 상태 갱신
        if (PlayerDataManager.Instance != null)
        {
            RefreshAllCharacterSlots();
            
            // 현재 선택된 슬롯 시각적 피드백 복원
            int currentSlot = PlayerDataManager.Instance.CurrentSlotIndex;
            if (currentSlot >= 0)
            {
                UpdateSlotSelectionVisual(currentSlot);
                UpdatePlayerInfoDisplay();
            }
            
            if (showDebugLogs)
                Debug.Log($"🎮 [CharacterInfoUI] 패널 활성화 - 상태 갱신 완료");
        }
    }
    
    /// <summary>
    /// 🎮 캐릭터 정보창 초기화
    /// </summary>
    private void InitializeCharacterInfoUI()
    {
        // PlayerDataManager 이벤트 구독
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnSlotSelected += OnPlayerSlotChanged;
        }
        
        // 캐릭터 슬롯 버튼 이벤트 연결
        SetupSlotButtons();
        
        // 닫기 버튼 이벤트 연결
        if (closePanelButton != null)
        {
            closePanelButton.onClick.AddListener(ClosePanel);
        }
        
        // 🆕 빈 슬롯 팝업 닫기 버튼 이벤트 연결
        // 이 부분은 이제 더 이상 사용되지 않으므로 제거
        
        // 🆕 빈 슬롯 팝업 초기 비활성화
        // 이 부분은 이제 더 이상 사용되지 않으므로 제거
        
        // 초기 캐릭터 슬롯 정보 표시
        RefreshAllCharacterSlots();
        
        // 🔧 수정: 현재 선택된 슬롯 표시 (PlayerDataManager.CurrentSlotIndex 사용)
        int currentSlot = PlayerDataManager.Instance?.CurrentSlotIndex ?? -1;
        if (currentSlot >= 0)
        {
            SelectCharacterSlot(currentSlot);
        }
        else
        {
            // 선택된 슬롯이 없으면 첫 번째 유효한 슬롯 자동 선택
            for (int i = 0; i < 3; i++)
            {
                var slotData = PlayerDataManager.Instance?.GetSlotData(i);
                if (slotData != null && slotData.isSlotUsed)
                {
                    SelectCharacterSlot(i);
                    break;
                }
            }
        }
        
        if (showDebugLogs)
            Debug.Log("🎮 [CharacterInfoUI] 캐릭터 정보창 초기화 완료");
    }
    
    /// <summary>
    /// 🖱️ 캐릭터 슬롯 버튼 이벤트 설정
    /// </summary>
    private void SetupSlotButtons()
    {
        for (int i = 0; i < characterSlotButtons.Length; i++)
        {
            if (characterSlotButtons[i] != null)
            {
                int slotIndex = i; // 람다 캡처 문제 방지
                characterSlotButtons[i].onClick.AddListener(() => OnCharacterSlotClicked(slotIndex));
                
                if (showDebugLogs)
                    Debug.Log($"🖱️ [CharacterInfoUI] 슬롯 {i} 버튼 이벤트 연결 완료");
            }
        }
    }
    
    /// <summary>
    /// 🔄 모든 캐릭터 슬롯 정보 새로고침
    /// </summary>
    private void RefreshAllCharacterSlots()
    {
        if (PlayerDataManager.Instance == null) return;
        
        for (int i = 0; i < 3; i++)
        {
            var slotData = PlayerDataManager.Instance.GetSlotData(i);
            UpdateCharacterSlotDisplay(i, slotData);
        }
        
        if (showDebugLogs)
            Debug.Log("🔄 [CharacterInfoUI] 모든 캐릭터 슬롯 새로고침 완료");
    }
    
    /// <summary>
    /// 🎭 캐릭터 슬롯 표시 업데이트
    /// </summary>
    private void UpdateCharacterSlotDisplay(int slotIndex, PlayerSlotData slotData)
    {
        if (slotIndex < 0 || slotIndex >= slotPanels.Length) return;
        
        GameObject slotPanel = slotPanels[slotIndex];
        if (slotPanel == null) return;
        
        // 슬롯에 캐릭터가 있는지 확인
        bool hasCharacter = slotData != null && slotData.isSlotUsed;
        
        // 🆕 실제 UI 요소 업데이트 구현
        if (hasCharacter)
        {
            // 캐릭터가 있는 경우
            if (slotNameTexts[slotIndex] != null)
                slotNameTexts[slotIndex].text = slotData.playerName;
                
            if (slotLevelTexts[slotIndex] != null)
                slotLevelTexts[slotIndex].text = $"Lv.{slotData.level}";
                
            if (slotClassTexts[slotIndex] != null)
                slotClassTexts[slotIndex].text = GetClassDisplayName(slotData.playerType);
                
            if (slotClassIcons[slotIndex] != null)
                slotClassIcons[slotIndex].sprite = GetClassIcon(slotData.playerType);
                
            // 빈 슬롯 패널 비활성화
            if (slotEmptyPanels[slotIndex] != null)
                slotEmptyPanels[slotIndex].SetActive(false);
        }
        else
        {
            // 빈 슬롯인 경우
            if (slotNameTexts[slotIndex] != null)
                slotNameTexts[slotIndex].text = "빈 슬롯";
                
            if (slotLevelTexts[slotIndex] != null)
                slotLevelTexts[slotIndex].text = "";
                
            if (slotClassTexts[slotIndex] != null)
                slotClassTexts[slotIndex].text = "";
                
            if (slotClassIcons[slotIndex] != null)
                slotClassIcons[slotIndex].sprite = emptySlotIcon;
                
            // 빈 슬롯 패널 활성화
            if (slotEmptyPanels[slotIndex] != null)
                slotEmptyPanels[slotIndex].SetActive(true);
        }
        
        if (showDebugLogs)
            Debug.Log($"🎭 [CharacterInfoUI] 슬롯 {slotIndex} 표시 업데이트 - 캐릭터 존재: {hasCharacter}");
    }
    
    /// <summary>
    /// 🖱️ 캐릭터 슬롯 클릭 이벤트
    /// </summary>
    public void OnCharacterSlotClicked(int slotIndex)
    {
        if (showDebugLogs)
            Debug.Log($"🖱️ [CharacterInfoUI] 슬롯 {slotIndex} 클릭됨");
        
        if (PlayerDataManager.Instance == null) return;
        
        var slotData = PlayerDataManager.Instance.GetSlotData(slotIndex);
        
        if (slotData != null && slotData.isSlotUsed)
        {
            // 캐릭터 있음 - 슬롯 선택
            SelectCharacterSlot(slotIndex);
        }
        else
        {
            // 🆕 빈 슬롯 클릭 - EmptySlotDisplay 표시
            if (showDebugLogs)
                Debug.Log($"🖼️ [CharacterInfoUI] 빈 슬롯 {slotIndex} 클릭 - EmptySlotDisplay 표시");
            
            ShowEmptySlotDisplay();
        }
    }
    
    /// <summary>
    /// 🖼️ 빈 슬롯 정보를 장비 영역에 표시
    /// </summary>
    private void ShowEmptySlotInEquipmentArea(int slotIndex)
    {
        // 기존 장비 정보 숨김 (있다면)
        if (equippedItemsUI != null)
        {
            // EquippedItemsUI의 표시를 일시적으로 숨기거나 빈 상태로 만들기
            // 이 부분은 EquippedItemsUI의 구현에 따라 조정 필요
        }
        
        // 선택된 슬롯의 EmptyPanel 정보 표시
        if (slotIndex >= 0 && slotIndex < slotEmptyPanels.Length && slotEmptyPanels[slotIndex] != null)
        {
            // 현재 선택된 슬롯 업데이트
            currentSelectedSlot = slotIndex;
            
            // 슬롯 선택 시각적 피드백
            UpdateSlotSelectionVisual(slotIndex);
            
            // 빈 슬롯 정보 표시 (레벨, 체력 등을 기본값으로)
            if (playerLevelText != null)
                playerLevelText.text = "레벨: -";
                
            if (playerHealthText != null)
                playerHealthText.text = "체력: -";
                
            if (playerNameText != null)
                playerNameText.text = "빈 슬롯";
                
            if (playerClassIcon != null)
                playerClassIcon.sprite = emptySlotIcon;
            
            if (showDebugLogs)
                Debug.Log($"🖼️ [CharacterInfoUI] 빈 슬롯 {slotIndex} 정보 표시 완료");
        }
    }
    
    /// <summary>
    /// ✅ 캐릭터 슬롯 선택 처리
    /// </summary>
    private void SelectCharacterSlot(int slotIndex)
    {
        if (PlayerDataManager.Instance == null) return;
        
        // 🆕 EmptySlotDisplay 숨김 (다른 캐릭터 선택 시)
        if (emptySlotDisplay != null)
        {
            emptySlotDisplay.SetActive(false);
        }
        
        // PlayerDataManager에서 슬롯 선택
        PlayerDataManager.Instance.SelectSlot(slotIndex);
        currentSelectedSlot = slotIndex;
        
        // 🆕 슬롯 선택 시각적 피드백
        UpdateSlotSelectionVisual(slotIndex);
        
        // EquippedItemsPanel 정보 업데이트 (자동으로 OnPlayerSlotChanged 이벤트 발생)
        UpdatePlayerInfoDisplay();
        
        if (showDebugLogs)
            Debug.Log($"✅ [CharacterInfoUI] 슬롯 {slotIndex} 선택 완료");
    }
    
    /// <summary>
    /// 🎯 슬롯 선택 시각적 피드백
    /// </summary>
    private void UpdateSlotSelectionVisual(int selectedSlot)
    {
        for (int i = 0; i < characterSlotButtons.Length; i++)
        {
            if (characterSlotButtons[i] != null)
            {
                // 선택된 슬롯과 다른 슬롯들의 시각적 구분
                var colors = characterSlotButtons[i].colors;
                if (i == selectedSlot)
                {
                    colors.normalColor = new Color(1f, 1f, 0.7f, 1f); // 연한 노란색
                    colors.highlightedColor = new Color(1f, 1f, 0.5f, 1f);
                }
                else
                {
                    colors.normalColor = Color.white;
                    colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
                }
                characterSlotButtons[i].colors = colors;
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"🎯 [CharacterInfoUI] 슬롯 {selectedSlot} 선택 시각적 피드백 적용");
    }
    
    /// <summary>
    /// 📊 플레이어 정보 표시 업데이트 (레벨, 체력 추가)
    /// </summary>
    private void UpdatePlayerInfoDisplay()
    {
        if (PlayerDataManager.Instance == null || currentSelectedSlot < 0) return;
        
        var slotData = PlayerDataManager.Instance.GetSlotData(currentSelectedSlot);
        if (slotData == null || !slotData.isSlotUsed) return;
        
        // 🔧 수정: PlayerSlotData.level 사용
        if (playerLevelText != null)
        {
            playerLevelText.text = $"레벨: {slotData.level}";
        }
        
        // 🔧 수정: PlayerSlotData.playerType 사용
        if (playerHealthText != null)
        {
            int baseHealth = GetBaseHealthByClass(slotData.playerType);
            int totalHealth = baseHealth + (slotData.level - 1) * 10; // 레벨당 체력 +10
            playerHealthText.text = $"체력: {totalHealth}";
        }
        
        // 캐릭터명 표시
        if (playerNameText != null)
        {
            playerNameText.text = slotData.playerName;
        }
        
        // 🔧 수정: PlayerSlotData.playerType 사용
        if (playerClassIcon != null)
        {
            playerClassIcon.sprite = GetClassIcon(slotData.playerType);
        }
        
        if (showDebugLogs)
            Debug.Log($"📊 [CharacterInfoUI] 플레이어 정보 업데이트 완료 - 슬롯: {currentSelectedSlot}");
    }
    
    /// <summary>
    /// 💚 클래스별 기본 체력 반환
    /// </summary>
    private int GetBaseHealthByClass(PlayerType playerType)
    {
        switch (playerType)
        {
            case PlayerType.Warrior: return 250;   // 전사 - 높은 체력
            case PlayerType.Assasin: return 180;   // 어쌔신 - 낮은 체력
            case PlayerType.Wizard: return 200;    // 마법사 - 중간 체력
            default: return 200;
        }
    }
    
    /// <summary>
    /// 🎨 클래스별 아이콘 반환
    /// </summary>
    private Sprite GetClassIcon(PlayerType playerType)
    {
        switch (playerType)
        {
            case PlayerType.Warrior: return warriorClassIcon;
            case PlayerType.Assasin: return assassinClassIcon;
            case PlayerType.Wizard: return wizardClassIcon;
            default: return emptySlotIcon;
        }
    }
    
    /// <summary>
    /// 🏷️ 클래스별 표시 이름 반환
    /// </summary>
    private string GetClassDisplayName(PlayerType playerType)
    {
        switch (playerType)
        {
            case PlayerType.Warrior: return "전사";
            case PlayerType.Assasin: return "어쌔신";
            case PlayerType.Wizard: return "마법사";
            default: return "";
        }
    }
    
    /// <summary>
    /// 🔄 플레이어 슬롯 변경 이벤트 처리
    /// </summary>
    private void OnPlayerSlotChanged(int newSlotIndex)
    {
        currentSelectedSlot = newSlotIndex;
        UpdateSlotSelectionVisual(newSlotIndex); // 🆕 시각적 피드백 추가
        UpdatePlayerInfoDisplay();
        
        if (showDebugLogs)
            Debug.Log($"🔄 [CharacterInfoUI] 플레이어 슬롯 변경됨: {newSlotIndex}");
    }
    
    /// <summary>
    /// 🔄 슬롯 선택 시각적 피드백 초기화
    /// </summary>
    private void ResetSlotSelectionVisual()
    {
        for (int i = 0; i < characterSlotButtons.Length; i++)
        {
            if (characterSlotButtons[i] != null)
            {
                // 모든 슬롯을 기본 상태로 복원
                var colors = characterSlotButtons[i].colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
                characterSlotButtons[i].colors = colors;
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"🔄 [CharacterInfoUI] 슬롯 선택 시각적 피드백 초기화 완료");
    }
    
    /// <summary>
    /// 🖼️ 빈 슬롯 클릭 시 EmptySlotDisplay 표시
    /// </summary>
    private void ShowEmptySlotDisplay()
    {
        if (emptySlotDisplay != null)
        {
            emptySlotDisplay.SetActive(true);
            
            if (showDebugLogs)
                Debug.Log($"🖼️ [CharacterInfoUI] EmptySlotDisplay 표시");
        }
        else
        {
            Debug.LogWarning($"⚠️ [CharacterInfoUI] emptySlotDisplay가 할당되지 않았습니다!");
        }
    }
    
    /// <summary>
    /// 🔒 빈 슬롯 팝업 닫기
    /// </summary>
    public void CloseEmptySlotPopup()
    {
        // 이 메서드는 이제 더 이상 사용되지 않으므로 제거
    }
    
    /// <summary>
    /// 🏠 패널 닫기 (로비로 돌아가기)
    /// </summary>
    public void ClosePanel()
    {
        if (showDebugLogs)
            Debug.Log($"🏠 [CharacterInfoUI] ClosePanel 호출 - 로비로 전환 시작");
        
        // 🆕 1. EmptySlotDisplay 숨김 (열려있다면)
        if (emptySlotDisplay != null)
        {
            emptySlotDisplay.SetActive(false);
        }
        
        // 2. CharacterInfoUI 상태 정리
        currentSelectedSlot = -1; // 선택 상태 초기화
        
        // 3. 슬롯 선택 시각적 피드백 초기화
        ResetSlotSelectionVisual();
        
        // 4. LobbyUIController를 찾아서 로비 전환 요청
        LobbyUIController lobbyUIController = FindObjectOfType<LobbyUIController>();
        if (lobbyUIController != null)
        {
            if (showDebugLogs)
                Debug.Log($"🏠 [CharacterInfoUI] 로비로 전환 요청 - LobbyUIController.OnBackToLobby() 호출");
            
            lobbyUIController.OnBackToLobby();
        }
        else
        {
            Debug.LogError($"🔴 [CharacterInfoUI] LobbyUIController를 찾을 수 없습니다!");
        }
    }
    
    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnSlotSelected -= OnPlayerSlotChanged;
        }
    }
}
