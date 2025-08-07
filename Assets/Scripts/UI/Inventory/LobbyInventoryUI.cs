using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 🏠 로비 전용 인벤토리 UI 시스템
/// 기존 인게임 인벤토리 로직 재사용 + 로비 전용 기능 추가
/// </summary>
public class LobbyInventoryUI : MonoBehaviour
{
    [Header("🎒 로비 인벤토리 설정")]
    [SerializeField] private GameObject inventoryPanel;     // 인벤토리 패널
    [SerializeField] private Button inventoryToggleButton;  // 가방 버튼
    [SerializeField] private Transform slotContainer;       // 슬롯들이 들어갈 컨테이너
    [SerializeField] private GameObject slotPrefab;         // 로비용 슬롯 프리팹
    [SerializeField] private int maxDisplaySlots = 16;      // 표시할 최대 슬롯 수
    
    [Header("📋 상세 정보 패널")]
    [SerializeField] private GameObject itemDetailPanel;    // 상세 정보 패널
    [SerializeField] private Image itemIconImage;           // 아이템 이미지
    [SerializeField] private Sprite defaultItemIcon;        // 🆕 기본 아이템 이미지
    [SerializeField] private TMP_Text itemNameText;         // 아이템 이름
    [SerializeField] private TMP_Text itemGradeText;        // 아이템 등급
    [SerializeField] private TMP_Text stat1Text;            // 스탯 1
    [SerializeField] private TMP_Text stat2Text;            // 스탯 2  
    [SerializeField] private TMP_Text stat3Text;            // 스탯 3
    [SerializeField] private Button closeDetailButton;     // 상세 패널 닫기 버튼
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 내부 상태
    private List<InventorySlot> lobbySlots = new List<InventorySlot>();
    private bool isInventoryOpen = false;

    void Start()
    {
        InitializeLobbyInventory();
        SetupEventListeners();
        
        // 초기 상태 설정
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
        if (itemDetailPanel != null)
            itemDetailPanel.SetActive(false);
    }

    /// <summary>
    /// 로비 인벤토리 초기화
    /// </summary>
    private void InitializeLobbyInventory()
    {
        if (slotContainer == null || slotPrefab == null)
        {
            Debug.LogError("🔴 [LobbyInventoryUI] 슬롯 컨테이너 또는 프리팹이 설정되지 않았습니다!");
            return;
        }

        // 기존 슬롯들 정리
        for (int i = 0; i < slotContainer.childCount; i++)
        {
            DestroyImmediate(slotContainer.GetChild(i).gameObject);
        }
        lobbySlots.Clear();

        // 로비용 슬롯들 생성
        for (int i = 0; i < maxDisplaySlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            
            if (slot != null)
            {
                lobbySlots.Add(slot);
            }
            else
            {
                Debug.LogError($"🔴 [LobbyInventoryUI] 슬롯 {i}에 InventorySlot 컴포넌트가 없습니다!");
            }
        }

        if (showDebugLogs)
            Debug.Log($"✅ [LobbyInventoryUI] 로비 인벤토리 초기화 완료 - {lobbySlots.Count}개 슬롯 생성");
    }

    /// <summary>
    /// 이벤트 리스너 설정
    /// </summary>
    private void SetupEventListeners()
    {
        // 가방 버튼 클릭 이벤트
        if (inventoryToggleButton != null)
        {
            inventoryToggleButton.onClick.AddListener(ToggleInventoryPanel);
        }

        // 상세 패널 닫기 버튼
        if (closeDetailButton != null)
        {
            closeDetailButton.onClick.AddListener(CloseDetailPanel);
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
    /// 인벤토리 패널 토글
    /// </summary>
    public void ToggleInventoryPanel()
    {
        if (inventoryPanel == null) return;

        isInventoryOpen = !isInventoryOpen;
        inventoryPanel.SetActive(isInventoryOpen);

        if (isInventoryOpen)
        {
            RefreshInventoryUI();
        }
        else
        {
            CloseDetailPanel(); // 인벤토리 닫을 때 상세 패널도 닫기
        }

        if (showDebugLogs)
            Debug.Log($"🎒 [LobbyInventoryUI] 인벤토리 패널 {(isInventoryOpen ? "열림" : "닫힘")}");
    }

    /// <summary>
    /// 슬롯 클릭 처리 (공용 이벤트)
    /// </summary>
    private void OnSlotClicked(EquipmentData equipmentData, int slotIndex)
    {
        if (showDebugLogs)
            Debug.Log($"🖱️ [LobbyInventoryUI] 슬롯 클릭 처리: {(equipmentData?.equipmentName ?? "빈 슬롯")} (인덱스: {slotIndex})");

        // 로비에서는 상세 정보 표시
        if (equipmentData != null && PlayerDataManager.Instance != null)
        {
            if (showDebugLogs)
                Debug.Log($"📋 [LobbyInventoryUI] 상세 정보 요청 시작: {equipmentData.equipmentName}");
            
            PlayerDataManager.Instance.TriggerItemDetailRequested(equipmentData);
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"⚠️ [LobbyInventoryUI] 상세 정보 표시 불가 - 데이터: {(equipmentData != null ? "있음" : "없음")}, 매니저: {(PlayerDataManager.Instance != null ? "있음" : "없음")}");
        }
    }

    /// <summary>
    /// 아이템 상세 정보 패널 표시
    /// </summary>
    private void ShowItemDetailPanel(EquipmentData equipmentData)
    {
        if (showDebugLogs)
            Debug.Log($"📋 [LobbyInventoryUI] ShowItemDetailPanel 호출됨: {(equipmentData?.equipmentName ?? "null")}");
        
        if (itemDetailPanel == null || equipmentData == null) 
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [LobbyInventoryUI] 상세 패널 표시 실패 - 패널: {(itemDetailPanel != null ? "있음" : "없음")}, 데이터: {(equipmentData != null ? "있음" : "없음")}");
            return;
        }

        // 🆕 아이템 이미지 설정 (기본 이미지 지원)
        if (itemIconImage != null)
        {
            if (equipmentData.icon != null)
            {
                itemIconImage.sprite = equipmentData.icon;
                itemIconImage.color = Color.white; // 이미지가 있을 때는 불투명
                if (showDebugLogs)
                    Debug.Log($"🖼️ [LobbyInventoryUI] 아이템 이미지 설정: {equipmentData.equipmentName}");
            }
            else if (defaultItemIcon != null)
            {
                itemIconImage.sprite = defaultItemIcon;
                itemIconImage.color = new Color(1, 1, 1, 0.7f); // 기본 이미지는 약간 반투명
                if (showDebugLogs)
                    Debug.Log($"🖼️ [LobbyInventoryUI] 기본 이미지 사용: {equipmentData.equipmentName}");
            }
            else
            {
                itemIconImage.sprite = null;
                itemIconImage.color = new Color(1, 1, 1, 0.3f);
                if (showDebugLogs)
                    Debug.LogWarning($"⚠️ [LobbyInventoryUI] 표시할 이미지 없음: {equipmentData.equipmentName}");
            }
        }

        // 기본 정보 설정
        if (itemNameText != null)
            itemNameText.text = equipmentData.equipmentName;

        if (itemGradeText != null)
            itemGradeText.text = GetGradeText(equipmentData);

        // 타입별 스탯 정보 설정
        SetStatsByEquipmentType(equipmentData);

        // 패널 활성화
        itemDetailPanel.SetActive(true);

        if (showDebugLogs)
            Debug.Log($"✅ [LobbyInventoryUI] 상세 정보 패널 표시 완료: {equipmentData.equipmentName}");
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
                // 악세서리: 체력, 크리티컬 확률, 크리티컬 데미지
                SetStatText(stat1Text, "체력", equipmentData.healthBonus);
                SetStatText(stat2Text, "치명타 확률", equipmentData.criticalChance);
                SetStatText(stat3Text, "치명타 데미지", equipmentData.criticalDamage);
                break;

            default:
                SetStatText(stat1Text, "스탯1", 0);
                SetStatText(stat2Text, "스탯2", 0);
                SetStatText(stat3Text, "스탯3", 0);
                break;
        }
    }

    /// <summary>
    /// 개별 스탯 텍스트 설정
    /// </summary>
    private void SetStatText(TMP_Text textComponent, string statName, float statValue)
    {
        if (textComponent != null)
        {
            textComponent.text = $"{statName}: +{statValue:F1}";
        }
    }

    /// <summary>
    /// 등급 텍스트 반환
    /// </summary>
    private string GetGradeText(EquipmentData equipmentData)
    {
        // EquipmentData의 실제 itemGrade 사용
        return equipmentData.itemGrade.ToString();
    }

    /// <summary>
    /// 상세 패널 닫기
    /// </summary>
    public void CloseDetailPanel()
    {
        if (itemDetailPanel != null)
        {
            itemDetailPanel.SetActive(false);
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
            Debug.Log($"📦 [LobbyInventoryUI] 아이템 추가됨: {item.equipmentName}");
    }

    /// <summary>
    /// 아이템 제거 이벤트 처리
    /// </summary>
    private void OnItemRemoved(EquipmentData item)
    {
        if (showDebugLogs)
            Debug.Log($"🗑️ [LobbyInventoryUI] 아이템 제거됨: {item.equipmentName}");
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnSlotClicked -= OnSlotClicked;
            PlayerDataManager.Instance.OnItemDetailRequested -= ShowItemDetailPanel;
            PlayerDataManager.Instance.OnInventoryChanged -= RefreshInventoryUI;
            PlayerDataManager.Instance.OnItemAddedToInventory -= OnItemAdded;
            PlayerDataManager.Instance.OnItemRemovedFromInventory -= OnItemRemoved;
        }
    }
}