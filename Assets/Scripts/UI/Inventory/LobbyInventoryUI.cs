using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // 🆕 씬 관리를 위한 추가

/// <summary>
/// 🏠 로비 인벤토리 UI (View Only)
/// 표시만 담당, 로직은 InventoryController에서 처리
/// </summary>
public class LobbyInventoryUI : MonoBehaviour
{
    [Header("🎒 로비 인벤토리 설정")]
    [SerializeField] private GameObject inventoryPanel;     // 인벤토리 패널
    [SerializeField] private Button inventoryToggleButton;  // 가방 버튼
    [SerializeField] private Transform slotContainer;       // 슬롯들이 들어갈 컨테이너
    [SerializeField] private GameObject slotPrefab;         // 로비용 슬롯 프리팹
    [SerializeField] private int maxDisplaySlots = 16;      // 표시할 최대 슬롯 수
    
    [Header("🎯 상세 패널 UI")]
    [SerializeField] private GameObject itemDetailPanel;
    [SerializeField] private Image detailItemIcon;
    [SerializeField] private Sprite defaultItemIcon;        // 🆕 기본 아이템 이미지
    [SerializeField] private TMP_Text itemNameText;         // 아이템 이름
    [SerializeField] private TMP_Text itemGradeText;        // 아이템 등급
    [SerializeField] private TMP_Text stat1Text;            // 스탯 1
    [SerializeField] private TMP_Text stat2Text;            // 스탯 2  
    [SerializeField] private TMP_Text stat3Text;            // 스탯 3
    [SerializeField] private Button closePanelButton;       // 🆕 범용 패널 닫기 버튼 (상점/캐릭터정보/인벤토리 → 로비)
    [SerializeField] private Button equipButton;            // 🆕 착용 버튼 추가
    [SerializeField] private TMP_Text equipWarningText;     // 🆕 착용 경고 메시지 텍스트
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 내부 상태
    private List<InventorySlot> lobbySlots = new List<InventorySlot>();

    // 🆕 현재 선택된 아이템 정보 (착용 버튼용)
    private EquipmentData currentSelectedItem;
    private int currentSelectedSlotIndex = -1;

    void Awake()
    {
        Debug.Log($"🚨🚨🚨 [LobbyInventoryUI] 이 로그가 나오면 스크립트가 실행되고 있다는 뜻! GameObject 이름: {gameObject.name}");
        Debug.Log($" [LobbyInventoryUI] Awake() 호출됨");
        SetupEventListeners();
        SetupControllerEvents();
        Debug.Log($"✅ [LobbyInventoryUI] Awake() 완료");
    }

    void Start()
    {
        Debug.Log($"🏠 [LobbyInventoryUI] Start() 호출됨 - 패널 활성화 상태 유지");
        
        // 1. 이벤트 구독
        SetupSlots();
        
        // 🆕 착용 버튼 이벤트 연결
        if (equipButton != null)
        {
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        }
        
        if (showDebugLogs)
            Debug.Log($"✅ [LobbyInventoryUI] Start() 완료");
    }

    /// <summary>
    /// 슬롯 생성 및 초기화
    /// </summary>
    private void SetupSlots()
    {
        if (slotContainer == null || slotPrefab == null)
        {
            Debug.LogError($"🔴 [LobbyInventoryUI] SlotContainer 또는 SlotPrefab이 설정되지 않았습니다!");
            return;
        }

        // 기존 슬롯들 정리
        foreach (Transform child in slotContainer)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        lobbySlots.Clear();

        // 새 슬롯들 생성
        for (int i = 0; i < maxDisplaySlots; i++)
        {
            GameObject slotObject = Instantiate(slotPrefab, slotContainer);
            InventorySlot slot = slotObject.GetComponent<InventorySlot>();
            if (slot != null)
            {
                slot.SetEquipmentData(null); // 빈 슬롯으로 초기화
                lobbySlots.Add(slot);
            }
        }

        if (showDebugLogs)
            Debug.Log($"✅ [LobbyInventoryUI] {lobbySlots.Count}개 슬롯 생성 완료");
    }

    private void SetupEventListeners()
    {
        // 🔧 UI 이벤트만 처리
        if (inventoryToggleButton != null)
        {
            inventoryToggleButton.onClick.AddListener(() => {
                Debug.Log($"🖱️ [LobbyInventoryUI] 가방버튼 클릭됨!");
                Debug.Log($"🔗 [LobbyInventoryUI] InventoryController.Instance 확인: {(InventoryController.Instance != null ? "있음" : "없음")}");
                InventoryController.Instance?.OpenInventory();
            });
            Debug.Log($"✅ [LobbyInventoryUI] 가방버튼 이벤트 연결 완료");
        }
        else
        {
            Debug.LogError($"🔴 [LobbyInventoryUI] inventoryToggleButton이 null입니다!");
        }

        // 패널 닫기 버튼
        if (closePanelButton != null)
        {
            closePanelButton.onClick.AddListener(ClosePanel);
        }

        // PlayerDataManager 이벤트 구독
        if (PlayerDataManager.Instance != null)
        {
            // 🔑 공용 이벤트 구독
            PlayerDataManager.Instance.OnSlotClicked += OnSlotClicked;
            PlayerDataManager.Instance.OnItemDetailRequested += ShowItemDetailPanel;
            
            // 인벤토리 변경 이벤트 구독
            PlayerDataManager.Instance.OnInventoryChanged += RefreshInventoryUI;
            PlayerDataManager.Instance.OnItemAddedToInventory += OnItemAdded;
            PlayerDataManager.Instance.OnItemRemovedFromInventory += OnItemRemoved;
        }
    }

    /// <summary>
    /// 슬롯 클릭 처리 (공용 이벤트) - 🔧 로비에서는 항상 DetailPanel 표시
    /// </summary>
    private void OnSlotClicked(EquipmentData equipmentData, int slotIndex)
    {
        if (showDebugLogs)
            Debug.Log($"🖱️ [LobbyInventoryUI] 슬롯 {slotIndex} 클릭: {equipmentData?.equipmentName}");
        
        if (equipmentData != null)
        {
            // 🔧 변경: 로비에서는 클릭 시 항상 DetailPanel 표시 (착용 기능 제거)
            ShowItemDetailPanel(equipmentData);
            
            // 🆕 DetailPanel에 현재 선택된 아이템 정보 저장 (착용 버튼용)
            currentSelectedItem = equipmentData;
            currentSelectedSlotIndex = slotIndex;
            
            if (showDebugLogs)
                Debug.Log($"ℹ️ [LobbyInventoryUI] {equipmentData.equipmentName} 상세 정보 표시 + 착용 준비");
        }
    }
    
    /// <summary>
    /// 🆕 아이템 자동 착용 시도 (UI 슬롯 인덱스 직접 사용)
    /// </summary>
    private bool TryEquipItem(EquipmentData equipment, int uiSlotIndex)
    {
        if (PlayerDataManager.Instance == null) return false;
        
        Debug.Log($"🔥 [LobbyInventoryUI] ============= TryEquipItem 시작 =============");
        Debug.Log($"   - 요청 아이템: {equipment.equipmentName}");
        Debug.Log($"   - UI 슬롯 인덱스: {uiSlotIndex}");
        
        // 🆕 착용 전 인벤토리 상태 로깅
        LogInventoryState("착용 전");
        
        // 장비 타입에 따른 슬롯 결정
        EquipmentSlot targetSlot = GetTargetSlot(equipment);
        if ((int)targetSlot == -1)
        {
            if (showDebugLogs)
                Debug.Log($"⚠️ [LobbyInventoryUI] {equipment.equipmentName}는 착용할 수 없는 아이템입니다");
            return false;
        }
        
        Debug.Log($"   - 대상 장비 슬롯: {targetSlot}");
        
        // 플레이어 클래스 호환성 검사
        PlayerClass playerClass = ConvertToPlayerClass(GameManager.Instance?.selectedPlayerData?.selectedPlayerType ?? PlayerType.None);
        if (!equipment.IsCompatibleWith(playerClass))
        {
            if (showDebugLogs)
                Debug.Log($"⚠️ [LobbyInventoryUI] {equipment.equipmentName}는 현재 클래스와 호환되지 않습니다");
            return false;
        }
        
        // 🆕 현재 장착된 아이템 확인
        var equippedItems = PlayerDataManager.Instance.EquippedItems;
        var currentEquippedItem = equippedItems.ContainsKey(targetSlot) ? equippedItems[targetSlot] : null;
        Debug.Log($"   - 현재 장착된 아이템: {currentEquippedItem?.equipmentName ?? "없음"}");
        
        // 🔧 UI 슬롯 인덱스 직접 사용 (FindInventorySlotIndex 제거)
        Debug.Log($"   - 사용할 슬롯 인덱스: {uiSlotIndex}");
        
        bool success = PlayerDataManager.Instance.EquipItemFromSlot(equipment, uiSlotIndex);
        
        // 🆕 착용 후 인벤토리 상태 로깅
        LogInventoryState("착용 후");
        
        if (success && showDebugLogs)
            Debug.Log($"✅ [LobbyInventoryUI] {equipment.equipmentName} → {targetSlot} 착용 완료");
        
        return success;
    }
    
    /// <summary>
    /// 🔄 PlayerType을 PlayerClass로 변환
    /// </summary>
    private PlayerClass ConvertToPlayerClass(PlayerType playerType)
    {
        switch (playerType)
        {
            case PlayerType.Warrior: return PlayerClass.Warrior;
            case PlayerType.Assasin: return PlayerClass.Assasin;
            case PlayerType.Wizard: return PlayerClass.Wizard;
            case PlayerType.None: return PlayerClass.None;
            default: return PlayerClass.None;
        }
    }
    
    /// <summary>
    /// 🎯 장비 타입에 따른 대상 슬롯 결정
    /// </summary>
    private EquipmentSlot GetTargetSlot(EquipmentData equipment)
    {
        switch (equipment.equipmentType)
        {
            case EquipmentType.Weapon:
                return EquipmentSlot.MainWeapon;
            case EquipmentType.Armor:
                return EquipmentSlot.Armor;
            case EquipmentType.Accessory:
                // 악세서리는 세부 타입에 따라 결정
                if (equipment.equipmentName.Contains("Boots"))
                    return EquipmentSlot.Boots;
                else if (equipment.equipmentName.Contains("Helmet"))
                    return EquipmentSlot.Helmet;
                else if (equipment.equipmentName.Contains("Shield"))
                    return EquipmentSlot.Shield;
                else if (equipment.equipmentName.Contains("Ring"))
                    return GetAvailableRingSlot();
                else if (equipment.equipmentName.Contains("Necklace"))
                    return EquipmentSlot.Necklace;
                break;
        }
        
        return (EquipmentSlot)(-1); // 🔧 None 대신 -1 반환
    }
    
    /// <summary>
    /// 🔍 사용 가능한 반지 슬롯 찾기
    /// </summary>
    private EquipmentSlot GetAvailableRingSlot()
    {
        var equippedItems = PlayerDataManager.Instance.EquippedItems;
        
        // Ring1이 비어있으면 Ring1 사용
        if (!equippedItems.ContainsKey(EquipmentSlot.Ring1) || equippedItems[EquipmentSlot.Ring1] == null)
            return EquipmentSlot.Ring1;
        
        // Ring1이 차있으면 Ring2 사용
        if (!equippedItems.ContainsKey(EquipmentSlot.Ring2) || equippedItems[EquipmentSlot.Ring2] == null)
            return EquipmentSlot.Ring2;
        
        // 둘 다 차있으면 Ring1에 교체
        return EquipmentSlot.Ring1;
    }

    /// <summary>
    /// 상세 정보 패널 표시 - 🆕 착용 버튼 포함
    /// </summary>
    private void ShowItemDetailPanel(EquipmentData equipmentData)
    {
        if (itemDetailPanel == null || equipmentData == null) return;

        // 패널 활성화
        itemDetailPanel.SetActive(true);

        // 🔧 아이템 이미지 설정 (우선순위: equipmentData.icon)
        if (detailItemIcon != null)
        {
            if (equipmentData.icon != null)
            {
                detailItemIcon.sprite = equipmentData.icon;
                detailItemIcon.color = Color.white;
                detailItemIcon.gameObject.SetActive(true);
                if (showDebugLogs)
                    Debug.Log($"🖼️ [LobbyInventoryUI] 아이템 이미지 설정 성공: {equipmentData.equipmentName}");
            }
            else if (defaultItemIcon != null)
            {
                detailItemIcon.sprite = defaultItemIcon;
                detailItemIcon.color = new Color(1, 1, 1, 0.7f);
                detailItemIcon.gameObject.SetActive(true);
                if (showDebugLogs)
                    Debug.Log($"🖼️ [LobbyInventoryUI] 기본 이미지 사용: {equipmentData.equipmentName}");
            }
            else
            {
                detailItemIcon.gameObject.SetActive(false);
                if (showDebugLogs)
                    Debug.LogWarning($"⚠️ [LobbyInventoryUI] 표시할 이미지 없음: {equipmentData.equipmentName}");
            }
        }

        // 🆕 착용 버튼 및 경고 메시지 제어
        bool isCompatible = IsItemCompatibleWithCurrentClass(equipmentData);
        SetupEquipButton(isCompatible);

        // 기본 정보 설정
        if (itemNameText != null)
            itemNameText.text = equipmentData.equipmentName;

        if (itemGradeText != null)
            itemGradeText.text = GetGradeText(equipmentData);

        // 타입별 스탯 정보 설정
        SetStatsByEquipmentType(equipmentData);

        if (showDebugLogs)
            Debug.Log($"✅ [LobbyInventoryUI] 상세 정보 패널 표시 완료: {equipmentData.equipmentName}");
    }
    
    /// <summary>
    /// 🆕 착용 버튼 설정 (경고 메시지는 버튼 클릭 시에만)
    /// </summary>
    private void SetupEquipButton(bool isCompatible)
    {
        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(true);
            
            // 버튼 텍스트 설정
            var buttonText = equipButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                if (isCompatible)
                {
                    buttonText.text = "착용";
                    buttonText.color = Color.white;
                }
                else
                {
                    buttonText.text = "착용 불가";
                    buttonText.color = Color.gray;
                }
            }
        }
        
        // 🔧 변경: 경고 메시지는 초기에 숨김 (버튼 클릭 시에만 표시)
        if (equipWarningText != null)
        {
            equipWarningText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 🔍 아이템이 현재 클래스와 호환되는지 확인
    /// </summary>
    private bool IsItemCompatibleWithCurrentClass(EquipmentData equipment)
    {
        if (PlayerDataManager.Instance?.selectedPlayerData == null) return false;
        
        var currentClass = PlayerDataManager.Instance.selectedPlayerData.selectedPlayerType;
        PlayerClass playerClass = currentClass switch
        {
            PlayerType.Warrior => PlayerClass.Warrior,
            PlayerType.Assasin => PlayerClass.Assasin, // 🔧 수정: Assassin → Assasin
            PlayerType.Wizard => PlayerClass.Wizard,
            _ => PlayerClass.Warrior
        };
        
        return equipment.IsCompatibleWith(playerClass);
    }
    
    /// <summary>
    /// 🆕 착용 버튼 클릭 처리
    /// </summary>
    private void OnEquipButtonClicked()
    {
        if (currentSelectedItem == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("⚠️ [LobbyInventoryUI] 선택된 아이템이 없습니다!");
            return;
        }
        
        if (showDebugLogs)
            Debug.Log($"🎯 [LobbyInventoryUI] 착용 버튼 클릭: {currentSelectedItem.equipmentName}");
        
        // 클래스 호환성 체크
        if (!IsItemCompatibleWithCurrentClass(currentSelectedItem))
        {
            // 🔧 착용 불가능한 클래스 - 일시적 경고 메시지 표시
            if (showDebugLogs)
                Debug.Log($"🚫 [LobbyInventoryUI] {currentSelectedItem.equipmentName}는 현재 클래스가 사용할 수 없는 장비입니다!");
            
            // 🆕 착용 실패 메시지 표시 (착용 성공과 동일한 방식)
            if (equipWarningText != null)
            {
                StartCoroutine(ShowTemporaryMessage("클래스가 다릅니다", Color.red, 2f));
            }
            return;
        }
        
        // 🔧 착용 가능한 클래스 - 착용 시도
        bool equipped = TryEquipItem(currentSelectedItem, currentSelectedSlotIndex);
        
        if (equipped)
        {
            if (showDebugLogs)
                Debug.Log($"✅ [LobbyInventoryUI] {currentSelectedItem.equipmentName} 착용 성공! (DetailPanel 유지)");
                
            // 🆕 착용 성공 메시지 표시
            if (equipWarningText != null)
            {
                StartCoroutine(ShowTemporaryMessage("착용 완료!", Color.green, 1.5f));
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"❌ [LobbyInventoryUI] {currentSelectedItem.equipmentName} 착용 실패!");
                
            // 🆕 착용 실패 메시지 표시
            if (equipWarningText != null)
            {
                StartCoroutine(ShowTemporaryMessage("착용 실패!", Color.red, 2f));
            }
        }
    }
    
    /// <summary>
    /// 🆕 통합된 일시적 메시지 표시 (성공/실패 공통)
    /// </summary>
    private IEnumerator ShowTemporaryMessage(string message, Color color, float duration)
    {
        if (equipWarningText == null) yield break;
        
        // 메시지 표시
        equipWarningText.gameObject.SetActive(true);
        equipWarningText.text = message;
        equipWarningText.color = color;
        equipWarningText.fontSize = 8f;
        
        if (showDebugLogs)
            Debug.Log($"📢 [LobbyInventoryUI] 메시지 표시: {message}");
        
        // 지정된 시간만큼 대기
        yield return new WaitForSeconds(duration);
        
        // 메시지 숨김
        equipWarningText.gameObject.SetActive(false);
        
        if (showDebugLogs)
            Debug.Log($"📢 [LobbyInventoryUI] 메시지 숨김: {message}");
    }

    /// <summary>
    /// 장비 타입별 스탯 정보 설정
    /// </summary>
    private void SetStatsByEquipmentType(EquipmentData equipmentData)
    {
        switch (equipmentData.equipmentType)
        {
            case EquipmentType.Weapon:
                // 무기: 공격력, 공격속도, 치명타율
                SetStatText(stat1Text, "공격력", equipmentData.attackDamage);
                SetStatText(stat2Text, "공격속도", equipmentData.attackSpeed);
                SetStatText(stat3Text, "치명타율", equipmentData.criticalChance);
                break;

            case EquipmentType.Armor:
                // 방어구: 방어력, 체력, 이동속도
                SetStatText(stat1Text, "방어력", equipmentData.defenseBonus);
                SetStatText(stat2Text, "체력", equipmentData.healthBonus);
                SetStatText(stat3Text, "이동속도", equipmentData.speedBonus);
                break;

            case EquipmentType.Accessory:
                // 악세서리: 다양한 효과
                SetStatText(stat1Text, "방어력", equipmentData.defenseBonus);
                SetStatText(stat2Text, "체력", equipmentData.healthBonus);
                SetStatText(stat3Text, "이동속도", equipmentData.speedBonus);
                break;

            default:
                // 기타 아이템
                SetStatText(stat1Text, "효과", 0);
                SetStatText(stat2Text, "", 0);
                SetStatText(stat3Text, "", 0);
                break;
        }
    }

    /// <summary>
    /// 개별 스탯 텍스트 설정
    /// </summary>
    private void SetStatText(TMP_Text textComponent, string statName, float value)
    {
        if (textComponent != null)
        {
            if (string.IsNullOrEmpty(statName))
            {
                textComponent.text = "";
            }
            else if (value > 0)
            {
                textComponent.text = $"{statName}: +{value:F1}";
            }
            else
            {
                textComponent.text = $"{statName}: {value:F1}";
            }
        }
    }

    /// <summary>
    /// 등급 텍스트 생성
    /// </summary>
    private string GetGradeText(EquipmentData equipmentData)
    {
        // 장비 이름에서 등급 추출 또는 기본값 반환
        if (equipmentData.equipmentName.Contains("_S_"))
            return "S급";
        else if (equipmentData.equipmentName.Contains("_A_"))
            return "A급";
        else if (equipmentData.equipmentName.Contains("_B_"))
            return "B급";
        else if (equipmentData.equipmentName.Contains("_C_"))
            return "C급";
        else
            return "일반";
    }

    /// <summary>
    /// 🏠 범용 패널 닫기 버튼 (현재 패널을 닫고 로비로 돌아가기)
    /// 상점, 캐릭터 정보창, 인벤토리 등 모든 패널에서 사용 가능
    /// </summary>
    public void ClosePanel()
    {
        if (showDebugLogs)
            Debug.Log($"🏠 [LobbyInventoryUI] ClosePanel 호출 - 로비로 전환 시작");
        
        // 🆕 1. InventoryController 상태 동기화 (중요!)
        if (InventoryController.Instance != null && InventoryController.Instance.IsInventoryOpen)
        {
            if (showDebugLogs)
                Debug.Log($"🔄 [LobbyInventoryUI] InventoryController 상태 동기화 - CloseInventory() 호출");
            
            InventoryController.Instance.CloseInventory();
        }
        
        // 2. LobbyUIController를 찾아서 로비 전환 요청
        LobbyUIController lobbyUIController = FindObjectOfType<LobbyUIController>();
        if (lobbyUIController != null)
        {
            if (showDebugLogs)
                Debug.Log($"🏠 [LobbyInventoryUI] 로비로 전환 요청 - LobbyUIController.OnBackToLobby() 호출");
            
            lobbyUIController.OnBackToLobby();
        }
        else
        {
            Debug.LogError($"🔴 [LobbyInventoryUI] LobbyUIController를 찾을 수 없습니다!");
            
            // Fallback: IntegratedInventoryController로 직접 인벤토리 닫기
            IntegratedInventoryController inventoryController = FindObjectOfType<IntegratedInventoryController>();
            if (inventoryController != null)
            {
                if (showDebugLogs)
                    Debug.Log($"🔄 [LobbyInventoryUI] Fallback: IntegratedInventoryController.CloseInventoryPanel() 호출");
                
                inventoryController.CloseInventoryPanel();
            }
        }
    }
    
    /// <summary>
    /// 🔒 상세 패널만 닫기 (내부용 - 기존 기능 유지)
    /// </summary>
    public void CloseDetailPanel()
    {
        if (itemDetailPanel != null && itemDetailPanel.activeSelf)
        {
            itemDetailPanel.SetActive(false);
            if (showDebugLogs)
                Debug.Log($"🔒 [LobbyInventoryUI] 상세 패널 닫힘");
        }
    }

    /// <summary>
    /// 인벤토리 UI 새로고침
    /// </summary>
    private void RefreshInventoryUI()
    {
        if (PlayerDataManager.Instance == null) return;

        var inventoryItems = PlayerDataManager.Instance.InventoryItems;

        for (int i = 0; i < lobbySlots.Count; i++)
        {
            if (i < inventoryItems.Count && inventoryItems[i] != null)
            {
                lobbySlots[i].SetEquipmentData(inventoryItems[i]);
            }
            else
            {
                lobbySlots[i].SetEquipmentData(null);
            }
        }

        if (showDebugLogs)
            Debug.Log($"🔄 [LobbyInventoryUI] UI 새로고침 완료 - {inventoryItems.Count}개 아이템 표시");
    }

    /// <summary>
    /// 아이템 추가 이벤트 처리
    /// </summary>
    private void OnItemAdded(EquipmentData item)
    {
        if (showDebugLogs)
            Debug.Log($"➕ [LobbyInventoryUI] 아이템 추가됨: {item.equipmentName}");
        RefreshInventoryUI();
    }

    /// <summary>
    /// 아이템 제거 이벤트 처리
    /// </summary>
    private void OnItemRemoved(EquipmentData item)
    {
        if (showDebugLogs)
            Debug.Log($"➖ [LobbyInventoryUI] 아이템 제거됨: {item.equipmentName}");
        RefreshInventoryUI();
    }

    private void SetupControllerEvents()
    {
        // InventoryController 이벤트 구독 시도
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryStateChanged += OnInventoryStateChanged;
            Debug.Log($"✅ [LobbyInventoryUI] InventoryController 이벤트 구독 완료");
        }
        else
        {
            Debug.LogWarning($"⚠️ [LobbyInventoryUI] InventoryController가 아직 초기화되지 않음 - 재시도 예약");
            StartCoroutine(RetrySetupControllerEvents());
        }
    }

    /// <summary>
    /// InventoryController 초기화 대기 후 재시도
    /// </summary>
    private IEnumerator RetrySetupControllerEvents()
    {
        Debug.Log($"⏰ [LobbyInventoryUI] InventoryController 초기화 대기 중...");
        
        // 최대 5초간 대기
        float timeout = 5f;
        float elapsed = 0f;
        
        while (InventoryController.Instance == null && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryStateChanged += OnInventoryStateChanged;
            Debug.Log($"✅ [LobbyInventoryUI] InventoryController 이벤트 구독 완료 (재시도 성공)");
        }
        else
        {
            Debug.LogError($"🔴 [LobbyInventoryUI] InventoryController 초기화 타임아웃!");
        }
    }

    /// <summary>
    /// 인벤토리 상태 변경 이벤트 (InventoryController에서 발생)
    /// </summary>
    private void OnInventoryStateChanged(bool isOpen)
    {
        Debug.Log($"🎯 [LobbyInventoryUI] 이벤트 수신됨! isOpen={isOpen}");
        Debug.Log($"📩 [LobbyInventoryUI] OnInventoryStateChanged 받음: {isOpen}");
        Debug.Log($"📦 [LobbyInventoryUI] inventoryPanel 상태: {(inventoryPanel != null ? inventoryPanel.activeSelf.ToString() : "null")}");
        
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isOpen);
            Debug.Log($"🔧 [LobbyInventoryUI] inventoryPanel.SetActive({isOpen}) 실행 완료");
        }
        
        if (isOpen)
        {
            Debug.Log($"🔄 [LobbyInventoryUI] RefreshInventoryUI 호출 시작");
            
            // 🔧 간단한 지연 새로고침 (패널 활성화 후 실행)
            Invoke(nameof(RefreshInventoryUI), 0.1f);
            
            if (itemDetailPanel != null)
            {
                itemDetailPanel.SetActive(true);
                Debug.Log($"🔧 [LobbyInventoryUI] DetailPanel 활성화 완료");
            }
        }
    }

    /// <summary>
    /// 🔍 인벤토리에서 아이템의 슬롯 인덱스 찾기
    /// </summary>
    private int FindInventorySlotIndex(EquipmentData equipment)
    {
        if (PlayerDataManager.Instance?.InventoryItems == null) return -1;
        
        var inventoryItems = PlayerDataManager.Instance.InventoryItems;
        for (int i = 0; i < inventoryItems.Count; i++)
        {
            if (inventoryItems[i] == equipment)
            {
                return i;
            }
        }
        
        return -1; // 찾지 못함
    }

    /// <summary>
    /// 🔍 인벤토리 상태 로깅 (디버깅용)
    /// </summary>
    private void LogInventoryState(string phase)
    {
        if (!showDebugLogs) return;
        
        Debug.Log($"📊 [LobbyInventoryUI] 인벤토리 상태 ({phase}):");
        
        if (PlayerDataManager.Instance?.InventoryItems != null)
        {
            var items = PlayerDataManager.Instance.InventoryItems;
            for (int i = 0; i < Mathf.Min(items.Count, 8); i++)
            {
                Debug.Log($"   [{i}]: {items[i]?.equipmentName ?? "빈 슬롯"}");
            }
            Debug.Log($"   총 {items.Count}개 아이템");
        }
        else
        {
            Debug.Log("   인벤토리 없음");
        }
    }
}