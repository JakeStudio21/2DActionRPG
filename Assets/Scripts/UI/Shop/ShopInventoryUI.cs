using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI.Popups;

/// <summary>
/// 🏪 ShopInventoryUI - 상점 전용 인벤토리 UI
/// 책임:
/// - 판매용 아이템 표시 및 단건/일괄 판매 처리
/// - 등급별 빠른 선택 → 체크마크(플래그) 방식 다중선택
/// - SS급 이상은 일괄판매 제외 (단일판매 전용)
///
/// 단건 판매: 슬롯 클릭 → ItemDetailPopup(Shop_Sell) → 판매
/// 일괄 판매: 등급 버튼 → 체크마크 선택 → 선택판매 버튼 → ConfirmationPopup → 실행
/// </summary>
public class ShopInventoryUI : MonoBehaviour
{
    [Header("🎒 상점 인벤토리 설정")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("🎯 등급별 빠른 선택 버튼 (D~S, SS 이상 제외)")]
    [SerializeField] private Button selectDGradeButton;
    [SerializeField] private Button selectCGradeButton;
    [SerializeField] private Button selectBGradeButton;
    [SerializeField] private Button selectAGradeButton;
    [SerializeField] private Button selectSGradeButton;

    [Header("🔘 일괄 판매 옵션")]
    [SerializeField] private Toggle excludeEquippedToggle;

    [Header("📊 선택 정보 표시")]
    [SerializeField] private TextMeshProUGUI selectionInfoText;   // "선택: N개 | 예상 획득: N 골드"

    [Header("💰 일괄 판매 실행 버튼")]
    [SerializeField] private Button batchSellButton;
    [SerializeField] private TextMeshProUGUI batchSellButtonText;

    [Header("🗑️ 선택 해제 버튼 (선택 시 활성화)")]
    [SerializeField] private Button clearSelectionButton;

    [Header("🔔 확인 팝업")]
    [SerializeField] private ConfirmationPopup confirmationPopup;

    // ─── 내부 상태 ───────────────────────────────────────────────
    private List<InventorySlot> shopInventorySlots = new List<InventorySlot>();
    private List<ItemInstanceID> selectedItemIds   = new List<ItemInstanceID>();
    private bool isMultiSelectMode = false;

    // 단건 판매용 이벤트 (외부 호환성 유지)
    public event Action<EquipmentData, int, ItemInstanceID> OnInventoryItemClicked;

    // ─── Unity 생명주기 ───────────────────────────────────────────

    void Awake()
    {
        SetupEventListeners();
    }

    void Start()
    {
        InitializeShopInventorySlots();
        RefreshInventoryUI();

        // 선택 해제 버튼 초기 비활성 (분해 패널과 동일)
        if (clearSelectionButton != null)
            clearSelectionButton.interactable = false;

        UpdateSelectionInfo();
    }

    void OnEnable()
    {
        if (shopInventorySlots != null && shopInventorySlots.Count > 0)
        {
            RefreshInventoryUI();
        }
    }

    // ─── 초기화 ───────────────────────────────────────────────────

    private void SetupEventListeners()
    {
        if (PlayerDataManager.Instance != null)
            PlayerDataManager.Instance.OnInventoryChanged += RefreshInventoryUI;

        // 등급별 빠른 선택 버튼
        if (selectDGradeButton != null)
            selectDGradeButton.onClick.AddListener(() => SelectAllByGrade(ItemGrade.D));
        if (selectCGradeButton != null)
            selectCGradeButton.onClick.AddListener(() => SelectAllByGrade(ItemGrade.C));
        if (selectBGradeButton != null)
            selectBGradeButton.onClick.AddListener(() => SelectAllByGrade(ItemGrade.B));
        if (selectAGradeButton != null)
            selectAGradeButton.onClick.AddListener(() => SelectAllByGrade(ItemGrade.A));
        if (selectSGradeButton != null)
            selectSGradeButton.onClick.AddListener(() => SelectAllByGrade(ItemGrade.S));

        // 일괄 판매 버튼
        if (batchSellButton != null)
            batchSellButton.onClick.AddListener(OnBatchSellButtonClicked);

        // 선택 해제 버튼
        if (clearSelectionButton != null)
            clearSelectionButton.onClick.AddListener(ClearSelection);
    }

    private void InitializeShopInventorySlots()
    {
        if (slotContainer == null || slotPrefab == null)
        {
            Debug.LogError("❌ [ShopInventoryUI] slotContainer 또는 slotPrefab이 할당되지 않음");
            return;
        }

        foreach (Transform child in slotContainer)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
        }
        shopInventorySlots.Clear();

        int maxSlots = 64;
        if (AccountDataManager.IsInitialized())
            maxSlots = AccountDataManager.Instance.GetAccountData().maxSharedInventorySize;

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            slotObj.name = $"ShopInventorySlot_{i}";

            InventorySlot inventorySlot = slotObj.GetComponent<InventorySlot>();
            if (inventorySlot != null)
                shopInventorySlots.Add(inventorySlot);
        }

        SetupShopSlotClickEvents();
    }

    private void SetupShopSlotClickEvents()
    {
        for (int i = 0; i < shopInventorySlots.Count; i++)
        {
            if (shopInventorySlots[i] == null) continue;

            int slotIndex = i;
            var button = shopInventorySlots[i].GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    var equipmentData = shopInventorySlots[slotIndex].GetEquipmentData();
                    var instanceId    = shopInventorySlots[slotIndex].GetItemInstanceID();
                    HandleSlotClicked(equipmentData, slotIndex, instanceId);
                });
            }
        }
    }

    // ─── 인벤토리 갱신 ───────────────────────────────────────────

    public void RefreshInventoryUI()
    {
        Vector2 savedScrollPosition = Vector2.zero;
        bool hasScrollRect = scrollRect != null;
        if (hasScrollRect)
            savedScrollPosition = scrollRect.normalizedPosition;

        if (AccountDataManager.Instance == null)
        {
            Debug.LogError("❌ [ShopInventoryUI] AccountDataManager.Instance가 null");
            return;
        }

        var accountData      = AccountDataManager.Instance.GetAccountData();
        var sharedInventoryIds = accountData?.sharedInventoryIds;

        var inventoryItems = new List<(EquipmentData equipment, ItemInstanceID instanceId)>();
        if (sharedInventoryIds != null)
        {
            foreach (var instanceId in sharedInventoryIds)
            {
                try
                {
                    var instanceData = AccountDataManager.Instance.GetInstance(instanceId);
                    if (instanceData == null) continue;

                    var template = ItemTemplateResolver.Load(instanceData.templateName);
                    if (template != null)
                        inventoryItems.Add((template, instanceId));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"❌ [ShopInventoryUI] 아이템 로드 오류: {ex.Message}");
                }
            }
        }

        for (int i = 0; i < shopInventorySlots.Count; i++)
        {
            if (i < inventoryItems.Count)
                shopInventorySlots[i].SetEquipmentData(inventoryItems[i].equipment, inventoryItems[i].instanceId);
            else
                shopInventorySlots[i].SetEquipmentData(null);
        }

        // 인벤 갱신 후 선택 목록에서 사라진 아이템 정리
        RefreshSelectedItemValidity();

        if (hasScrollRect)
            StartCoroutine(RestoreScrollPositionNextFrame(savedScrollPosition));
    }

    private IEnumerator RestoreScrollPositionNextFrame(Vector2 position)
    {
        yield return null;
        if (scrollRect != null)
            scrollRect.normalizedPosition = position;
    }

    // ─── 슬롯 클릭 처리 ──────────────────────────────────────────

    private void HandleSlotClicked(EquipmentData equipmentData, int slotIndex, ItemInstanceID instanceId)
    {
        if (equipmentData == null || instanceId.IsEmpty) return;

        if (isMultiSelectMode)
        {
            // 다중선택 모드: 슬롯 체크마크 토글
            ToggleSlotSelection(shopInventorySlots[slotIndex], instanceId);
        }
        else
        {
            // 단건 판매 모드: ItemDetailPopup 열기
            OnInventoryItemClicked?.Invoke(equipmentData, slotIndex, instanceId);
            ShowItemDetailPopup(equipmentData, slotIndex, instanceId);
        }
    }

    private void ShowItemDetailPopup(EquipmentData equipmentData, int slotIndex, ItemInstanceID instanceId)
    {
        var popup = FindObjectOfType<ItemDetailPopup>(true);
        if (popup == null)
        {
            Debug.LogError("❌ [ShopInventoryUI] ItemDetailPopup을 찾을 수 없습니다!");
            return;
        }
        popup.Show(equipmentData, ItemDetailContext.Shop_Sell, slotIndex, instanceId);
    }

    // ─── 등급별 일괄 선택 ────────────────────────────────────────

    /// <summary>
    /// 등급별 일괄 선택. SS 이상은 제외.
    /// 다른 등급 버튼 클릭 시 이전 선택 초기화 후 새 등급 선택 (분해 패널과 동일한 UX).
    /// 선택 해제는 clearSelectionButton으로 처리.
    /// </summary>
    public void SelectAllByGrade(ItemGrade grade)
    {
        // SS 이상 등급은 일괄판매 불가
        if (grade >= ItemGrade.SS)
        {
            Debug.LogWarning($"[ShopInventoryUI] {grade} 등급은 단일 판매만 가능합니다.");
            return;
        }

        // 이전 선택 초기화 후 해당 등급 선택 (분해 패널 동일 방식)
        ClearSelection();

        bool excludeEquipped = excludeEquippedToggle != null && excludeEquippedToggle.isOn;
        List<ItemInstanceID> equippedIds = new List<ItemInstanceID>();

        if (excludeEquipped && PlayerDataManager.Instance?.selectedPlayerData != null)
        {
            equippedIds = PlayerDataManager.Instance.selectedPlayerData
                .RuntimeEquippedInstanceIds.Values.ToList();
        }

        foreach (var slot in shopInventorySlots)
        {
            var itemData   = slot.GetEquipmentData();
            var instanceId = slot.GetItemInstanceID();

            if (itemData == null || instanceId.IsEmpty) continue;
            if (itemData.itemGrade != grade) continue;
            if (excludeEquipped && equippedIds.Contains(instanceId)) continue;

            slot.SetSelected(true, notifyEvent: false);
            if (!selectedItemIds.Contains(instanceId))
                selectedItemIds.Add(instanceId);
        }

        // 해당 등급 아이템이 없으면 안내 텍스트 표시
        if (selectedItemIds.Count == 0)
        {
            if (selectionInfoText != null)
                selectionInfoText.text = $"{grade}등급 판매 가능한 아이템이 없습니다";
            return;
        }

        // 선택된 아이템이 있을 때만 다중선택 모드 활성화
        SetMultiSelectMode(true);
        UpdateSelectionInfo();
    }

    // ─── 다중선택 모드 제어 ───────────────────────────────────────

    private void SetMultiSelectMode(bool enabled)
    {
        isMultiSelectMode = enabled;
        foreach (var slot in shopInventorySlots)
            slot.SetMultiSelectMode(enabled);
    }

    private void ToggleSlotSelection(InventorySlot slot, ItemInstanceID instanceId)
    {
        bool nowSelected = !slot.IsSelected();
        slot.SetSelected(nowSelected, notifyEvent: false);

        if (nowSelected)
        {
            if (!selectedItemIds.Contains(instanceId))
                selectedItemIds.Add(instanceId);
        }
        else
        {
            selectedItemIds.Remove(instanceId);
        }

        // 선택된 항목이 없으면 다중선택 모드 해제
        if (selectedItemIds.Count == 0)
            SetMultiSelectMode(false);

        UpdateSelectionInfo();
    }

    public void ClearSelection()
    {
        foreach (var slot in shopInventorySlots)
        {
            if (slot.IsSelected())
                slot.SetSelected(false, notifyEvent: false);
        }
        selectedItemIds.Clear();
        SetMultiSelectMode(false);
        UpdateSelectionInfo();
    }

    /// <summary>
    /// 인벤 갱신 후 존재하지 않는 아이템 ID를 선택 목록에서 제거
    /// </summary>
    private void RefreshSelectedItemValidity()
    {
        if (selectedItemIds.Count == 0) return;

        var validIds = shopInventorySlots
            .Where(s => !s.GetItemInstanceID().IsEmpty)
            .Select(s => s.GetItemInstanceID())
            .ToHashSet();

        selectedItemIds.RemoveAll(id => !validIds.Contains(id));

        if (selectedItemIds.Count == 0)
            SetMultiSelectMode(false);

        UpdateSelectionInfo();
    }

    // ─── 선택 정보 UI 갱신 ───────────────────────────────────────

    private void UpdateSelectionInfo()
    {
        int count     = selectedItemIds.Count;
        int totalGold = CalculateTotalSellPrice();

        if (selectionInfoText != null)
        {
            selectionInfoText.text = count > 0
                ? $"선택: {count}개 | 예상 획득: {totalGold:N0} 골드"
                : "등급 버튼을 눌러 아이템을 선택하세요";
        }

        if (batchSellButton != null)
            batchSellButton.interactable = count > 0;

        if (batchSellButtonText != null)
        {
            batchSellButtonText.text = count > 0
                ? $"선택 판매 ({count}개)"
                : "선택 판매";
        }

        // 선택 해제 버튼: 분해 패널과 동일하게 선택 시만 활성화
        if (clearSelectionButton != null)
            clearSelectionButton.interactable = count > 0;
    }

    private int CalculateTotalSellPrice()
    {
        if (ShopController.Instance == null) return 0;

        int total = 0;
        foreach (var instanceId in selectedItemIds)
        {
            var slot = shopInventorySlots.FirstOrDefault(s => s.GetItemInstanceID().Equals(instanceId));
            if (slot == null) continue;

            var itemData = slot.GetEquipmentData();
            if (itemData == null) continue;

            total += ShopController.Instance.GetItemSellPrice(itemData.itemID);
        }
        return total;
    }

    // ─── 일괄 판매 실행 ───────────────────────────────────────────

    private void OnBatchSellButtonClicked()
    {
        if (selectedItemIds.Count == 0) return;

        int count     = selectedItemIds.Count;
        int totalGold = CalculateTotalSellPrice();

        if (confirmationPopup == null)
        {
            Debug.LogError("❌ [ShopInventoryUI] ConfirmationPopup이 연결되지 않았습니다!");
            ExecuteBatchSell();
            return;
        }

        confirmationPopup.Show(
            title   : "아이템 일괄 판매",
            message : $"선택한 {count}개의 아이템을 판매하시겠습니까?",
            detail  : $"예상 획득: {totalGold:N0} 골드",
            onConfirm: ExecuteBatchSell,
            onCancel : null
        );
    }

    private void ExecuteBatchSell()
    {
        if (ShopController.Instance == null)
        {
            Debug.LogError("❌ [ShopInventoryUI] ShopController.Instance가 null입니다!");
            return;
        }

        // 선택된 아이템 데이터 수집
        var itemsToSell = new List<(EquipmentData equipment, ItemInstanceID instanceId)>();
        foreach (var instanceId in selectedItemIds)
        {
            var slot = shopInventorySlots.FirstOrDefault(s => s.GetItemInstanceID().Equals(instanceId));
            if (slot == null) continue;

            var itemData = slot.GetEquipmentData();
            if (itemData != null)
                itemsToSell.Add((itemData, instanceId));
        }

        var (soldCount, totalGold) = ShopController.Instance.TryBatchSellItems(itemsToSell);

        Debug.Log($"✅ [ShopInventoryUI] 일괄 판매 완료: {soldCount}개, {totalGold:N0} 골드 획득");

        ClearSelection();
        RefreshInventoryUI();
    }

    // ─── 외부 호출용 ─────────────────────────────────────────────

    public void ForceRefreshInventory()
    {
        RefreshInventoryUI();
    }

    private void OnSlotLazyLoadedForShop(int slotIndex)
    {
        if (gameObject.activeInHierarchy)
            RefreshInventoryUI();
    }

    void OnDestroy()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnInventoryChanged -= RefreshInventoryUI;
            PlayerDataManager.Instance.OnSlotLazyLoaded   -= OnSlotLazyLoadedForShop;
        }
    }
}
