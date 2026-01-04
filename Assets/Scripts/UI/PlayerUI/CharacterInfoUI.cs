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
        // �� 패널이 활성화될 때마다 상태 갱신
        if (PlayerDataManager.Instance != null)
        {
            // 캐릭터 슬롯 정보 갱신
            RefreshAllCharacterSlots();
            
            // 현재 선택된 슬롯 복원
            int currentSlot = PlayerDataManager.Instance.CurrentSlotIndex;
            if (currentSlot >= 0)
            {
                UpdateSlotSelectionVisual(currentSlot);
                UpdatePlayerInfoDisplay();
                
                // EquippedItemsPanel도 갱신
                if (equippedItemsUI != null)
                {
                    equippedItemsUI.ForceRefreshEquippedItems();
                }
            }
            
            if (showDebugLogs)
                Debug.Log($"🎮 [CharacterInfoUI] 패널 활성화 - 상태 갱신 완료");
        }
    }

    /// <summary>
    /// 🎮 캐릭터 정보창 초기화 (지연 갱신 지원)
    /// </summary>
    private void InitializeCharacterInfoUI()
    {
        // PlayerDataManager 이벤트 구독 (지연 갱신 지원)
        if (PlayerDataManager.Instance != null)
        {
            // 🔧 지연 갱신: OnSlotSelected 이벤트 구독을 조건부로 변경
            // 캐릭터 정보창이 활성화된 상태에서만 실시간 갱신
            // PlayerDataManager.Instance.OnSlotSelected += OnPlayerSlotChanged; // 제거
            
            // 🆕 지연 로드 완료 이벤트 구독
            PlayerDataManager.Instance.OnSlotLazyLoaded += OnSlotLazyLoadedForCharacterInfo;
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
        
        // 초기 캐릭터 슬롯 정보 표시 (지연 갱신 지원)
        StartCoroutine(InitializeCharacterSlotsWithLazyLoad());
        
        if (showDebugLogs)
            Debug.Log("🎮 [CharacterInfoUI] 캐릭터 정보창 초기화 완료 (지연 갱신 지원)");
    }

    /// <summary>
    /// 🆕 지연 로드 지원으로 캐릭터 슬롯 초기화
    /// </summary>
    private IEnumerator InitializeCharacterSlotsWithLazyLoad()
    {
        // 캐릭터 데이터 로드 상태 확인
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsLazyLoadRequired())
        {
            Debug.Log("🔄 [CharacterInfoUI] 지연 로드 필요 - 캐릭터 데이터 로드 중...");
            
            int selectedSlot = PlayerDataManager.Instance.GetSelectedSlotIndex();
            bool loadSuccess = PlayerDataManager.Instance.LazyLoadSlotData(selectedSlot);
            
            if (!loadSuccess)
            {
                Debug.LogError("❌ [CharacterInfoUI] 캐릭터 데이터 로드 실패");
                yield break;
            }
            
            yield return new WaitForSeconds(0.1f); // 로드 완료 대기
        }
        
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
    }

    /// <summary>
    /// 🆕 지연 로드 완료 시 캐릭터 정보 갱신
    /// </summary>
    private void OnSlotLazyLoadedForCharacterInfo(int slotIndex)
    {
        if (showDebugLogs)
            Debug.Log($"🔄 [CharacterInfoUI] 슬롯 {slotIndex} 지연 로드 완료 - 캐릭터 정보 갱신");
        
        // 캐릭터 정보창이 활성화된 상태에서만 갱신
        if (gameObject.activeInHierarchy)
        {
            RefreshAllCharacterSlots();
            UpdateSlotSelectionVisual(slotIndex);
            UpdatePlayerInfoDisplay();
        }
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
        // 🆕 EquippedItemsPanel 빈 상태로 초기화
        if (equippedItemsUI != null)
        {
            equippedItemsUI.ShowEmptySlotState();
            
            if (showDebugLogs)
                Debug.Log($"🎒 [CharacterInfoUI] 빈 슬롯 {slotIndex} - EquippedItemsPanel 빈 상태로 초기화");
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
                playerLevelText.text = "Lv.-";
                
            if (playerHealthText != null)
                playerHealthText.text = "Health -";
                
            if (playerNameText != null)
                playerNameText.text = "빈 슬롯";
                
            if (playerClassIcon != null)
                playerClassIcon.sprite = emptySlotIcon;
            
            if (showDebugLogs)
                Debug.Log($"🖼️ [CharacterInfoUI] 빈 슬롯 {slotIndex} 정보 표시 완료");
        }
    }
    
    /// <summary>
    /// 🔧 수정: 캐릭터 슬롯 선택 처리 (EquippedItemsPanel 갱신 포함)
    /// </summary>
    private void SelectCharacterSlot(int slotIndex)
    {
        Debug.Log($"═══════════════════════════════════════════════════════");
        Debug.Log($"🎮 [CharacterInfoUI] SelectCharacterSlot({slotIndex}) 호출");
        Debug.Log($"═══════════════════════════════════════════════════════");
        
        if (PlayerDataManager.Instance == null) 
        {
            Debug.LogError($"❌ [CharacterInfoUI] PlayerDataManager.Instance가 null");
            return;
        }
        
        // 🆕 EmptySlotDisplay 숨김 (다른 캐릭터 선택 시)
        if (emptySlotDisplay != null)
        {
            emptySlotDisplay.SetActive(false);
        }
        
        // 🆕 디버그: 슬롯 선택 전 상태 확인
        Debug.Log($"📊 [CharacterInfoUI] 슬롯 선택 전 상태:");
        Debug.Log($"   - 현재 선택된 슬롯: {currentSelectedSlot}");
        Debug.Log($"   - 요청된 슬롯: {slotIndex}");
        
        // 🔧 수정: 완전 로드로 변경하여 데이터 손상 방지
        Debug.Log($"🔄 [CharacterInfoUI] PlayerDataManager.SelectSlot({slotIndex}) 호출 중...");
        PlayerDataManager.Instance.SelectSlot(slotIndex);
        currentSelectedSlot = slotIndex;
        
        // 🆕 디버그: 슬롯 선택 후 SelectedPlayerData 상태 확인
        if (PlayerDataManager.Instance.selectedPlayerData != null)
        {
            Debug.Log($"📊 [CharacterInfoUI] 슬롯 선택 후 SelectedPlayerData 상태:");
            Debug.Log($"   - selectedSlotIndex: {PlayerDataManager.Instance.selectedPlayerData.selectedSlotIndex}");
            Debug.Log($"   - playerName: {PlayerDataManager.Instance.selectedPlayerData.playerName}");
            Debug.Log($"   - playerType: {PlayerDataManager.Instance.selectedPlayerData.selectedPlayerType}");
            Debug.Log($"   - 인벤토리 아이템: {PlayerDataManager.Instance.selectedPlayerData.runtimeInventoryItems.Count}개");
            Debug.Log($"   - 장착 아이템: {PlayerDataManager.Instance.selectedPlayerData.RuntimeEquippedItems.Count}개");
            
            foreach (var equipped in PlayerDataManager.Instance.selectedPlayerData.RuntimeEquippedItems)
            {
                if (equipped.Value != null)
                    Debug.Log($"     ⚔️ 장착[{equipped.Key}]: {equipped.Value.equipmentName}");
            }
        }
        
        // 🆕 슬롯 선택 시각적 피드백 (즉시 적용)
        UpdateSlotSelectionVisual(slotIndex);
        
        // 🔧 지연 갱신: 캐릭터 정보창에서는 즉시 정보 표시 (사용자 경험 향상)
        UpdatePlayerInfoDisplay();
        
        // 🆕 EquippedItemsPanel도 즉시 갱신
        if (equippedItemsUI != null)
        {
            Debug.Log($"🎒 [CharacterInfoUI] EquippedItemsPanel 갱신 중...");
            equippedItemsUI.ForceRefreshEquippedItems();
            Debug.Log($"✅ [CharacterInfoUI] EquippedItemsPanel 갱신 완료");
        }
        
        Debug.Log($"═══════════════════════════════════════════════════════");
        Debug.Log($"✅ [CharacterInfoUI] 슬롯 {slotIndex} 선택 완료 (장비 포함)");
        Debug.Log($"═══════════════════════════════════════════════════════");
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
            playerLevelText.text = $"Lv.{slotData.level}";
        }
        
        // 🔧 수정: PlayerSlotData.playerType 사용
        if (playerHealthText != null)
        {
            int baseHealth = GetBaseHealthByClass(slotData.playerType);
            int totalHealth = baseHealth + (slotData.level - 1) * 10; // 레벨당 체력 +10
            playerHealthText.text = $"Health {totalHealth}";
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
    /// 🔄 플레이어 슬롯 변경 이벤트 처리 (지연 갱신 지원)
    /// </summary>
    private void OnPlayerSlotChanged(int newSlotIndex)
    {
        // 🔧 지연 갱신: 캐릭터 정보창이 활성화된 상태에서만 즉시 갱신
        if (!gameObject.activeInHierarchy)
        {
            if (showDebugLogs)
                Debug.Log($"🔄 [CharacterInfoUI] 캐릭터 정보창 비활성화 상태 - 갱신 지연");
            return;
        }
        
        currentSelectedSlot = newSlotIndex;
        UpdateSlotSelectionVisual(newSlotIndex);
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
        
        // 🔧 수정: 캐릭터 선택 상태 유지 (UI 상태만 초기화)
        // currentSelectedSlot = -1; // ❌ 제거: 캐릭터 선택 해제하지 않음
        
        // 3. 슬롯 선택 시각적 피드백 초기화 (선택 상태는 유지)
        // ResetSlotSelectionVisual(); // ❌ 제거: 시각적 선택 상태도 유지
        
        // 🆕 현재 선택된 슬롯 유지 (PlayerDataManager와 동기화)
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsSlotSelected)
        {
            currentSelectedSlot = PlayerDataManager.Instance.CurrentSlotIndex;
            if (showDebugLogs)
                Debug.Log($"🔧 [CharacterInfoUI] 캐릭터 선택 상태 유지: 슬롯 {currentSelectedSlot}");
        }
        
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
            PlayerDataManager.Instance.OnSlotLazyLoaded -= OnSlotLazyLoadedForCharacterInfo; // 🆕 추가
        }
    }

    /// <summary>
    /// 🆕 현재 선택된 슬롯으로 강제 갱신 (LobbyEquippedItemsUI 포함)
    /// </summary>
    public void ForceRefreshWithCurrentSlot(int currentSlot)
    {
        if (showDebugLogs)
            Debug.Log($"🔄 [CharacterInfoUI] 현재 슬롯 {currentSlot}로 강제 갱신 (UI만)");
        
        // 모든 캐릭터 슬롯 정보 갱신
        RefreshAllCharacterSlots();
        
        // ✅ UI만 갱신 (데이터 재로딩 금지!)
        if (currentSlot >= 0 && currentSlot < 3)
        {
            // ❌ 금지: SelectCharacterSlot(currentSlot) → SelectSlot() → LoadFromSlotData() 호출
            // ✅ 올바름: UI만 다시 그리기
            currentSelectedSlot = currentSlot;
            UpdateSlotSelectionVisual(currentSlot);
            UpdatePlayerInfoDisplay();
        }
        
        // ✅ 장비창 UI만 Refresh (SelectedPlayerData에서 읽기)
        if (equippedItemsUI != null)
        {
            Debug.Log($"🎒 [CharacterInfoUI] 장비창 UI Refresh 시작...");
            equippedItemsUI.ForceRefreshEquippedItems();
            Debug.Log($"✅ [CharacterInfoUI] 장비창 UI Refresh 완료");
        }
        
        if (showDebugLogs)
            Debug.Log($"✅ [CharacterInfoUI] 슬롯 {currentSlot} UI 갱신 완료 (데이터 재로딩 없음)");
    }
}
