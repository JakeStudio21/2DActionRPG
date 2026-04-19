using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace UI.Workshop
{
    /// <summary>
    /// 🏭 공방 좌측 인벤토리 UI
    /// 책임:
    /// - 보관창고 아이템 목록 표시
    /// - 다중 선택 모드 관리
    /// - 등급별 필터링
    /// - 장착 아이템 제외 옵션
    /// </summary>
    public class WorkshopInventoryUI : MonoBehaviour
    {
        [Header("📊 디버그")]
        
        [Header("📑 탭 시스템")]
        [SerializeField] private Button equipmentTabButton;
        [SerializeField] private Button consumableTabButton;
        [SerializeField] private Button materialTabButton;
        [SerializeField] private TMP_Text equipmentTabText;
        [SerializeField] private TMP_Text consumableTabText;
        [SerializeField] private TMP_Text materialTabText;
        
        [Header("🔘 다중 선택 시스템")]
        [SerializeField] private Toggle multiSelectModeToggle;
        [SerializeField] private TMP_Text selectionCountText;
        [SerializeField] private Button clearSelectionButton;
        
        [Header("🎯 빠른 필터 버튼")]
        [SerializeField] private Button selectDGradeButton;
        [SerializeField] private Button selectCGradeButton;
        [SerializeField] private Button selectBGradeButton;
        [SerializeField] private Button selectAGradeButton;
        [SerializeField] private Button selectSGradeButton;
        [SerializeField] private Toggle excludeEquippedToggle;
        
        [Header("📦 인벤토리 스크롤뷰")]
        [SerializeField] private ScrollRect inventoryScrollView;
        [SerializeField] private Transform inventoryContent;
        [SerializeField] private GameObject inventorySlotPrefab;
        [SerializeField] private int maxSlots = 100; // 최대 슬롯 수
        
        [Header("📊 하단 정보")]
        [SerializeField] private TMP_Text itemCountText;
        [SerializeField] private TMP_Text playerGoldText; // ⭐ 플레이어 보유 골드 표시
        
        [Header("🔗 연동 컴포넌트")]
        [SerializeField] private EnhancementUI enhancementUI;
        [SerializeField] private WorkshopUI workshopUI; // ⭐ 공방 메인 UI
        
        // 상태
        private InventoryTabType currentTab = InventoryTabType.Equipment;
        private WorkshopUI.WorkshopTabType currentWorkshopTab = WorkshopUI.WorkshopTabType.Enhancement; // ⭐ 현재 공방 탭
        private List<InventorySlot> inventorySlots = new List<InventorySlot>();
        private List<ItemInstanceID> selectedItems = new List<ItemInstanceID>();
        private bool isMultiSelectMode = false;
        private InventorySlot lastSelectedSlot = null; // 단일 선택 시 마지막 선택 슬롯
        
        // ⭐ 선택 모드 추적 (개별 클릭 vs 일괄 선택)
        public bool IsIndividualSelectionMode { get; private set; } = false;
        
        // 이벤트
        public event Action<List<ItemInstanceID>> OnSelectionChanged;
        public event Action<ItemInstanceID> OnSingleItemSelected; // 단일 선택 시
        
        void Awake()
        {
            
            InitializeUI();
            SetupEventListeners();
        }
        
        void Start()
        {
            
            // 초기 표시
            RefreshInventoryDisplay();
        }
        
        /// <summary>
        /// GameObject 활성화 시 자동 갱신 + 이벤트 구독
        /// </summary>
        void OnEnable()
        {
            
            // ⭐ WorkshopUI 탭 변경 이벤트 구독
            if (workshopUI != null)
            {
                workshopUI.OnTabChanged += OnWorkshopTabChanged;
                
                // 현재 탭 가져오기
                currentWorkshopTab = workshopUI.GetCurrentTab();
                
            }
            else
            {
                Debug.LogWarning("⚠️ [WorkshopInventoryUI] workshopUI가 null입니다! Inspector에서 연결하세요.");
            }
            
            // ⭐ 공방 패널이 열릴 때마다 인벤토리 자동 갱신
            // (상점에서 아이템 구매 후 공방에 들어왔을 때 반영되도록)
            if (inventorySlots != null && inventorySlots.Count > 0)
            {
                RefreshInventoryDisplay();
            }
            else
            {
                Debug.LogWarning("⚠️ [WorkshopInventoryUI] OnEnable() - inventorySlots가 없거나 비어있음!");
            }
            
            // ⭐ AccountDataManager 이벤트 구독 (실시간 갱신)
            if (AccountDataManager.Instance != null)
            {
                AccountDataManager.Instance.OnSharedInventoryChanged += OnInventoryChangedHandler;
                AccountDataManager.Instance.OnMaterialChanged += OnMaterialChangedHandler;
                
            }
            else
            {
                Debug.LogWarning("⚠️ [WorkshopInventoryUI] AccountDataManager.Instance가 null입니다!");
            }
            
            // ⭐ 골드 변경 이벤트 구독 (실시간 갱신)
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnGoldChanged += OnGoldChangedHandler;
            }
            else
            {
                Debug.LogError("❌ [WorkshopInventoryUI] AccountDataManager.Instance가 null입니다!");
            }
        }
        
        /// <summary>
        /// GameObject 비활성화 시 이벤트 구독 해제
        /// </summary>
        void OnDisable()
        {
            // ⭐ WorkshopUI 이벤트 구독 해제
            if (workshopUI != null)
            {
                workshopUI.OnTabChanged -= OnWorkshopTabChanged;
                
            }
            
            // ⭐ AccountDataManager 이벤트 구독 해제 (메모리 누수 방지)
            if (AccountDataManager.Instance != null)
            {
                AccountDataManager.Instance.OnSharedInventoryChanged -= OnInventoryChangedHandler;
                AccountDataManager.Instance.OnMaterialChanged -= OnMaterialChangedHandler;
                
            }
            
            // ⭐ 골드 변경 이벤트 구독 해제
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnGoldChanged -= OnGoldChangedHandler;
                
            }
        }
        
        /// <summary>
        /// 공방 탭 변경 이벤트 핸들러
        /// </summary>
        private void OnWorkshopTabChanged(WorkshopUI.WorkshopTabType newTab)
        {
            
            // ⭐ 선택 초기화 먼저 (이전 모드에서 선택 해제)
            ClearSelection();
            
            // 탭 변경
            currentWorkshopTab = newTab;
            
            // ⭐ 탭별 UI 조정 (새 모드 설정)
            UpdateUIForCurrentTab();
            
        }
        
        /// <summary>
        /// 현재 공방 탭에 맞게 UI 조정
        /// </summary>
        private void UpdateUIForCurrentTab()
        {
            bool isEnhancementTab = (currentWorkshopTab == WorkshopUI.WorkshopTabType.Enhancement);
            
            // 강화 탭: 등급 필터 + 다중 선택 토글 숨김
            if (selectDGradeButton != null)
                selectDGradeButton.gameObject.SetActive(!isEnhancementTab);
            if (selectCGradeButton != null)
                selectCGradeButton.gameObject.SetActive(!isEnhancementTab);
            if (selectBGradeButton != null)
                selectBGradeButton.gameObject.SetActive(!isEnhancementTab);
            if (selectAGradeButton != null)
                selectAGradeButton.gameObject.SetActive(!isEnhancementTab);
            if (selectSGradeButton != null)
                selectSGradeButton.gameObject.SetActive(!isEnhancementTab);
            if (excludeEquippedToggle != null)
                excludeEquippedToggle.gameObject.SetActive(!isEnhancementTab);
            
            // ⭐ 다중 선택 토글 항상 숨김 (분해/합성은 자동으로 다중 선택)
            if (multiSelectModeToggle != null)
                multiSelectModeToggle.gameObject.SetActive(false);
            
            // ⭐ 탭에 따라 선택 모드 자동 설정
            if (isEnhancementTab)
            {
                // 강화: 단일 선택 (SelectionHighlight)
                SetMultiSelectMode(false);
            }
            else
            {
                // 분해/합성: 다중 선택 (SelectionCheckbox)
                SetMultiSelectMode(true);
            }
            
            // 모든 슬롯의 체크박스 표시/숨김 갱신
            UpdateAllSlotCheckboxVisibility();
            
        }
        
        /// <summary>
        /// 모든 슬롯의 체크박스 표시/숨김 갱신
        /// </summary>
        private void UpdateAllSlotCheckboxVisibility()
        {
            bool showCheckbox = (currentWorkshopTab != WorkshopUI.WorkshopTabType.Enhancement);
            
            foreach (var slot in inventorySlots)
            {
                if (slot != null)
                {
                    slot.SetCheckboxVisible(showCheckbox);
                }
            }
            
        }
        
        /// <summary>
        /// 공유 창고 인벤토리 변경 이벤트 핸들러
        /// </summary>
        private void OnInventoryChangedHandler()
        {
            
            RefreshInventoryDisplay();
            
        }
        
        /// <summary>
        /// 재료 변경 이벤트 핸들러
        /// </summary>
        private void OnMaterialChangedHandler(MaterialType materialType, int newCount)
        {
            
            // 재료 탭이 열려있으면 갱신
            if (currentTab == InventoryTabType.Material)
            {
                RefreshInventoryDisplay();
            }
        }
        
        /// <summary>
        /// ⭐ 골드 변경 이벤트 핸들러
        /// </summary>
        private void OnGoldChangedHandler(int newGold)
        {
            UpdatePlayerGoldDisplay();
            
        }
        
        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            // 필수 컴포넌트 체크
            if (inventoryScrollView == null) Debug.LogError("[WorkshopInventoryUI] inventoryScrollView 누락!");
            if (inventoryContent == null) Debug.LogError("[WorkshopInventoryUI] inventoryContent 누락!");
            if (inventorySlotPrefab == null) Debug.LogError("[WorkshopInventoryUI] inventorySlotPrefab 누락!");
            
            if (equipmentTabButton == null) Debug.LogError("[WorkshopInventoryUI] equipmentTabButton 누락!");
            if (multiSelectModeToggle == null) Debug.LogError("[WorkshopInventoryUI] multiSelectModeToggle 누락!");
            if (selectionCountText == null) Debug.LogError("[WorkshopInventoryUI] selectionCountText 누락!");
            if (clearSelectionButton == null) Debug.LogError("[WorkshopInventoryUI] clearSelectionButton 누락!");
            
            // 초기 슬롯 생성
            CreateInventorySlots();
            
            // 초기 탭 설정
            SwitchTab(InventoryTabType.Equipment);
            
            // 다중 선택 모드 초기 비활성화
            SetMultiSelectMode(false);
            
            // ⭐ 초기 골드 표시
            UpdatePlayerGoldDisplay();
        }
        
        /// <summary>
        /// 이벤트 리스너 설정
        /// </summary>
        private void SetupEventListeners()
        {
            // 탭 버튼
            if (equipmentTabButton != null)
                equipmentTabButton.onClick.AddListener(() => SwitchTab(InventoryTabType.Equipment));
            
            if (consumableTabButton != null)
                consumableTabButton.onClick.AddListener(() => SwitchTab(InventoryTabType.Consumable));
            
            if (materialTabButton != null)
                materialTabButton.onClick.AddListener(() => SwitchTab(InventoryTabType.Material));
            
            // 다중 선택 토글
            if (multiSelectModeToggle != null)
                multiSelectModeToggle.onValueChanged.AddListener(SetMultiSelectMode);
            
            // 선택 초기화 버튼
            if (clearSelectionButton != null)
                clearSelectionButton.onClick.AddListener(ClearSelection);
            
            // 등급별 필터 버튼
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
            
        }
        
        /// <summary>
        /// 인벤토리 슬롯 생성
        /// </summary>
        private void CreateInventorySlots()
        {
            if (inventorySlotPrefab == null || inventoryContent == null)
            {
                Debug.LogError("❌ [WorkshopInventoryUI] 슬롯 생성 실패 - Prefab 또는 Content 누락");
                return;
            }
            
            // 기존 슬롯 제거
            foreach (Transform child in inventoryContent)
            {
                Destroy(child.gameObject);
            }
            inventorySlots.Clear();
            
            // 새 슬롯 생성
            for (int i = 0; i < maxSlots; i++)
            {
                GameObject slotObj = Instantiate(inventorySlotPrefab, inventoryContent);
                InventorySlot slot = slotObj.GetComponent<InventorySlot>();
                
                if (slot != null)
                {
                    inventorySlots.Add(slot);
                    
                    // 선택 이벤트 구독
                    slot.OnSelectionChanged += OnSlotSelectionChanged;
                    
                    // 초기에는 빈 슬롯
                    slot.ClearSlot();
                }
            }
            
        }
        
        /// <summary>
        /// 탭 전환
        /// </summary>
        public void SwitchTab(InventoryTabType tabType)
        {
            currentTab = tabType;
            
            UpdateTabButtonStates();
            RefreshInventoryDisplay();
            
        }
        
        /// <summary>
        /// 탭 버튼 상태 업데이트
        /// </summary>
        private void UpdateTabButtonStates()
        {
            Color activeColor = Color.white;
            Color inactiveColor = new Color(0.7f, 0.7f, 0.7f);
            
            // 장비 탭
            if (equipmentTabButton != null && equipmentTabText != null)
            {
                bool isActive = (currentTab == InventoryTabType.Equipment);
                equipmentTabButton.image.color = isActive ? activeColor : inactiveColor;
                equipmentTabText.color = isActive ? activeColor : inactiveColor;
            }
            
            // 소모품 탭
            if (consumableTabButton != null && consumableTabText != null)
            {
                bool isActive = (currentTab == InventoryTabType.Consumable);
                consumableTabButton.image.color = isActive ? activeColor : inactiveColor;
                consumableTabText.color = isActive ? activeColor : inactiveColor;
            }
            
            // 재료 탭
            if (materialTabButton != null && materialTabText != null)
            {
                bool isActive = (currentTab == InventoryTabType.Material);
                materialTabButton.image.color = isActive ? activeColor : inactiveColor;
                materialTabText.color = isActive ? activeColor : inactiveColor;
            }
        }
        
        /// <summary>
        /// 인벤토리 표시 갱신
        /// </summary>
        public void RefreshInventoryDisplay()
        {
            
            if (currentTab == InventoryTabType.Equipment)
            {
                RefreshEquipmentTab();
            }
            else if (currentTab == InventoryTabType.Material)
            {
                RefreshMaterialTab();
            }
            else
            {
                // 소모품 탭 (나중에 구현)
                ClearAllSlots();
            }
            
            UpdateItemCountText();
            
            // ⭐ 탭별 UI 조정 (체크박스 표시/숨김 등)
            UpdateUIForCurrentTab();
            
        }
        
        /// <summary>
        /// 장비 탭 갱신
        /// </summary>
        private void RefreshEquipmentTab()
        {
            var filteredItems = GetFilteredEquipmentItems();
            
            
            // 슬롯에 아이템 표시
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                if (i < filteredItems.Count)
                {
                    var itemId = filteredItems[i];
                    var itemData = AccountDataManager.Instance.GetInstance(itemId);
                    
                    if (itemData != null)
                    {
                        var equipmentData = LoadEquipmentData(itemData.templateName);
                        if (equipmentData != null)
                        {
                            inventorySlots[i].SetEquipmentData(equipmentData, itemId);
                            
                            // 귀속 상태 설정
                            bool isBound = AccountDataManager.Instance.IsBound(itemId);
                            inventorySlots[i].SetBindingStatus(isBound);
                        }
                        else
                        {
                            inventorySlots[i].ClearSlot();
                        }
                    }
                    else
                    {
                        inventorySlots[i].ClearSlot();
                    }
                }
                else
                {
                    inventorySlots[i].ClearSlot();
                }
            }
            
        }
        
        /// <summary>
        /// 재료 탭 갱신
        /// </summary>
        private void RefreshMaterialTab()
        {
            var materials = AccountDataManager.Instance.GetAllMaterials();
            
            // 슬롯에 재료 표시
            int slotIndex = 0;
            foreach (var materialStack in materials)
            {
                if (slotIndex >= inventorySlots.Count) break;
                
                inventorySlots[slotIndex].SetupMaterial(materialStack);
                slotIndex++;
            }
            
            // 나머지 슬롯 비우기
            for (int i = slotIndex; i < inventorySlots.Count; i++)
            {
                inventorySlots[i].ClearSlot();
            }
            
        }
        
        /// <summary>
        /// 필터링된 장비 아이템 목록 가져오기
        /// </summary>
        private List<ItemInstanceID> GetFilteredEquipmentItems()
        {
            var accountData = AccountDataManager.Instance.GetAccountData();
            var allItems = accountData.sharedInventoryIds;
            
            
            // 장비만 필터링
            var equipmentItems = allItems.Where(id =>
            {
                var itemData = AccountDataManager.Instance.GetInstance(id);
                if (itemData == null) return false;
                
                var equipmentData = LoadEquipmentData(itemData.templateName);
                return equipmentData != null;
            }).ToList();
            
            
            // 장착 아이템 제외 옵션
            if (excludeEquippedToggle != null && excludeEquippedToggle.isOn)
            {
                var equippedIds = PlayerDataManager.Instance.selectedPlayerData
                    .RuntimeEquippedInstanceIds.Values.ToList();
                
                equipmentItems = equipmentItems.Where(id => !equippedIds.Contains(id)).ToList();
                
            }
            
            return equipmentItems;
        }
        
        /// <summary>
        /// EquipmentData 로드 헬퍼 (ItemTemplateResolver 사용)
        /// </summary>
        private EquipmentData LoadEquipmentData(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                Debug.LogWarning($"⚠️ [WorkshopInventoryUI] LoadEquipmentData() - templateName이 비어있음");
                return null;
            }
            
            // ⭐ ItemTemplateResolver 사용 (LobbyInventoryUI와 동일)
            var equipment = ItemTemplateResolver.Load(templateName);
            
            if (equipment == null)
            {
                Debug.LogWarning($"⚠️ [WorkshopInventoryUI] EquipmentData 로드 실패: {templateName}");
            }
            
            return equipment;
        }
        
        /// <summary>
        /// 모든 슬롯 비우기
        /// </summary>
        private void ClearAllSlots()
        {
            foreach (var slot in inventorySlots)
            {
                slot.ClearSlot();
            }
        }
        
        /// <summary>
        /// 아이템 개수 텍스트 업데이트
        /// </summary>
        private void UpdateItemCountText()
        {
            if (itemCountText == null) return;
            
            int currentCount = 0;
            int maxCount = 100; // AccountDataManager에서 가져올 수도 있음
            
            if (currentTab == InventoryTabType.Equipment)
            {
                currentCount = GetFilteredEquipmentItems().Count;
            }
            else if (currentTab == InventoryTabType.Material)
            {
                currentCount = AccountDataManager.Instance.GetAllMaterials().Count;
            }
            
            itemCountText.text = $"보유 아이템: {currentCount}/{maxCount}";
        }
        
        // ========================================
        // 🔘 다중 선택 시스템
        // ========================================
        
        /// <summary>
        /// 다중 선택 모드 설정
        /// </summary>
        public void SetMultiSelectMode(bool enabled)
        {
            isMultiSelectMode = enabled;
            
            // 모든 슬롯에 다중 선택 모드 적용
            foreach (var slot in inventorySlots)
            {
                slot.SetMultiSelectMode(enabled);
            }
            
            // 모드 해제 시 선택 초기화
            if (!enabled)
            {
                ClearSelection();
                
                // EnhancementUI 초기화
                if (enhancementUI != null)
                {
                    enhancementUI.ClearSelection();
                }
                
                lastSelectedSlot = null;
            }
            
            UpdateSelectionUI();
            
        }
        
        /// <summary>
        /// 슬롯 선택 콜백
        /// </summary>
        private void OnSlotSelectionChanged(ItemInstanceID itemId, bool selected)
        {
            if (selected)
            {
                // ⭐ 합성 탭: 같은 등급 + 같은 타입만 선택 가능 (다중 선택 모드)
                if (isMultiSelectMode && currentWorkshopTab == WorkshopUI.WorkshopTabType.Fusion && selectedItems.Count > 0)
                {
                    // 현재 선택하려는 아이템의 등급 + 타입
                    var newItemInstance = AccountDataManager.Instance.GetInstance(itemId);
                    if (newItemInstance != null)
                    {
                        var newItemData = ItemTemplateResolver.Load(newItemInstance.templateName);
                        if (newItemData != null)
                        {
                            // 기존 선택된 아이템들의 등급 + 타입 확인
                            var firstSelectedId = selectedItems[0];
                            var firstItemInstance = AccountDataManager.Instance.GetInstance(firstSelectedId);
                            if (firstItemInstance != null)
                            {
                                var firstItemData = ItemTemplateResolver.Load(firstItemInstance.templateName);
                                if (firstItemData != null)
                                {
                                    // ⭐ 선택 초기화 여부 판단
                                    bool shouldClear = false;
                                    string clearReason = "";
                                    
                                    if (IsIndividualSelectionMode)
                                    {
                                        // ⭐ 개별 클릭 모드: 등급 또는 세부타입이 다르면 초기화
                                        string newDetailedType = GetDetailedEquipmentType(newItemData);
                                        string firstDetailedType = GetDetailedEquipmentType(firstItemData);
                                        
                                        if (newItemData.itemGrade != firstItemData.itemGrade)
                                        {
                                            shouldClear = true;
                                            clearReason = "다른 등급";
                                        }
                                        else if (newDetailedType != firstDetailedType)
                                        {
                                            shouldClear = true;
                                            clearReason = "다른 세부타입";
                                        }
                                    }
                                    else
                                    {
                                        // ⭐ 일괄 선택 모드: 등급만 체크 (세부타입 무관)
                                        if (newItemData.itemGrade != firstItemData.itemGrade)
                                        {
                                            shouldClear = true;
                                            clearReason = "다른 등급";
                                        }
                                    }
                                    
                                    if (shouldClear)
                                    {
                                        
                                        // ⭐ 완전 초기화: 모든 슬롯 선택 해제 (현재 클릭한 슬롯 포함)
                                        int clearedCount = 0;
                                        foreach (var slot in inventorySlots)
                                        {
                                            if (slot.IsSelected())
                                            {
                                                slot.SetSelected(false, notifyEvent: false);
                                                clearedCount++;
                                            }
                                        }
                                        
                                        // 선택 리스트 완전 초기화
                                        selectedItems.Clear();
                                        
                                        // ⭐ 개별 클릭 모드 유지 (등급 전환도 개별 클릭)
                                        IsIndividualSelectionMode = true;
                                        
                                        
                                        // ⭐ UI 즉시 갱신 (어둡게 처리 해제)
                                        UpdateSelectionUI();
                                        
                                        // ⭐ 현재 클릭한 슬롯만 다시 선택 (notifyEvent: false로 재귀 방지)
                                        InventorySlot currentSlot = FindSlotByItemId(itemId);
                                        if (currentSlot != null)
                                        {
                                            currentSlot.SetSelected(true, notifyEvent: false);
                                            selectedItems.Add(itemId);
                                            Dbg.Log($"   → 새 슬롯 선택 완료: {newItemData.equipmentName}");
                                        }
                                        else
                                        {
                                            Debug.LogError($"   ❌ currentSlot이 null!");
                                        }
                                        
                                        // ⭐ 최종 UI 갱신 및 이벤트 발생
                                        UpdateSelectionUI();
                                        OnSelectionChanged?.Invoke(selectedItems);
                                        
                                        // ⭐ 여기서 메서드 종료 (아래 중복 로직 실행 방지)
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                
                // ⭐ 단일 선택 모드: 이전 선택 해제
                if (!isMultiSelectMode)
                {
                    // ⭐ 개별 클릭 모드 활성화
                    IsIndividualSelectionMode = true;
                    
                    // 이전 선택 슬롯 해제
                    if (lastSelectedSlot != null && lastSelectedSlot.GetItemInstanceID() != itemId)
                    {
                        lastSelectedSlot.SetSelected(false, notifyEvent: false);
                        
                    }
                    
                    // 현재 슬롯 저장
                    InventorySlot currentSlot = FindSlotByItemId(itemId);
                    if (currentSlot != null)
                    {
                        lastSelectedSlot = currentSlot;
                    }
                    
                    // 선택 리스트 초기화 (단일 선택)
                    selectedItems.Clear();
                    selectedItems.Add(itemId);
                    
                // ⭐ EnhancementUI에 전달
                if (enhancementUI != null)
                {
                    enhancementUI.OnSelectedItemChanged(itemId);
                    
                }
                else
                {
                        Debug.LogWarning($"⚠️ [WorkshopInventoryUI] enhancementUI가 null입니다!");
                }
                }
                else
                {
                    // ⭐ 다중 선택 모드: 리스트에 추가 (개별 클릭)
                    // Fusion 탭에서도 개별 클릭 추적!
                    IsIndividualSelectionMode = true;
                    
                    if (!selectedItems.Contains(itemId))
                    {
                        selectedItems.Add(itemId);
                        
                    }
                }
            }
            else
            {
                // 선택 해제
                selectedItems.Remove(itemId);
                
                
                // 단일 선택 모드에서 선택 해제 시 EnhancementUI 초기화
                if (!isMultiSelectMode && selectedItems.Count == 0 && enhancementUI != null)
                {
                    enhancementUI.ClearSelection();
                    lastSelectedSlot = null;
                    
                }
            }
            
            UpdateSelectionUI();
            OnSelectionChanged?.Invoke(selectedItems);
            
            // 단일 선택 이벤트 발생
            if (selected && !isMultiSelectMode)
            {
                OnSingleItemSelected?.Invoke(itemId);
            }
        }
        
        /// <summary>
        /// ItemInstanceID로 슬롯 찾기
        /// </summary>
        private InventorySlot FindSlotByItemId(ItemInstanceID itemId)
        {
            foreach (var slot in inventorySlots)
            {
                if (slot.GetItemInstanceID() == itemId)
                {
                    return slot;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 장비의 세부 타입 문자열 반환 (합성용)
        /// - Weapon: 모든 무기(Sword/Bow/Magic) 통합 취급 (클래스 무관)
        /// - Armor: Helmet/Armor/Boots 등 세부 구분
        /// - Accessory: Ring/Necklace 등 세부 구분
        /// </summary>
        private string GetDetailedEquipmentType(EquipmentData equipData)
        {
            switch (equipData.equipmentType)
            {
                case EquipmentType.Weapon:
                    // ⭐ 무기는 예외: Sword/Bow/Magic 모두 "Weapon"으로 통합
                    // → Warrior 검 + Assassin 활 + Wizard 지팡이 합성 가능!
                    return "Weapon";
                case EquipmentType.Armor:
                    return $"Armor_{equipData.ArmorType}";
                case EquipmentType.Accessory:
                    return $"Accessory_{equipData.AccessoryType}";
                default:
                    return equipData.equipmentType.ToString();
            }
        }
        
        /// <summary>
        /// 선택 정보 UI 갱신
        /// </summary>
        private void UpdateSelectionUI()
        {
            if (selectionCountText != null)
            {
                selectionCountText.text = $"선택: {selectedItems.Count}개";
            }
            
            if (clearSelectionButton != null)
            {
                clearSelectionButton.interactable = selectedItems.Count > 0;
            }

            // ⭐ 합성 탭: 선택된 아이템과 같은 등급 (+ 개별 클릭 시 같은 분류)만 밝게 표시
            if (currentWorkshopTab == WorkshopUI.WorkshopTabType.Fusion && selectedItems.Count > 0)
            {
                // ⭐ 방어 로직: 유효하지 않은 ID를 조용히 제거 (이미 삭제된 아이템 처리)
                var invalidIds = new List<ItemInstanceID>();
                foreach (var id in selectedItems)
                {
                    var instance = AccountDataManager.Instance.GetInstance(id);
                    if (instance == null)
                    {
                        invalidIds.Add(id);
                    }
                }
                
                if (invalidIds.Count > 0)
                {
                    foreach (var invalidId in invalidIds)
                    {
                        selectedItems.Remove(invalidId);
                    }
                    
                    
                    // 모든 아이템이 무효화된 경우 조기 반환
                    if (selectedItems.Count == 0)
                    {
                        foreach (var slot in inventorySlots)
                            slot.SetDimmed(false);
                        return;
                    }
                }

                // 첫 번째 선택 아이템의 등급 + 세부타입 기준
                var firstSelectedId = selectedItems[0];
                var firstItemInstance = AccountDataManager.Instance.GetInstance(firstSelectedId);
                if (firstItemInstance != null)
                {
                    var firstItemData = ItemTemplateResolver.Load(firstItemInstance.templateName);
                    if (firstItemData != null)
                    {
                        ItemGrade targetGrade = firstItemData.itemGrade;
                        string targetDetailedType = GetDetailedEquipmentType(firstItemData); // ⭐ 세부타입 추가


                        // 모든 슬롯 검사
                        foreach (var slot in inventorySlots)
                        {
                            var slotData = slot.GetEquipmentData();
                            if (slotData != null)
                            {
                                bool isMatching = false;

                                // ⭐ 개별 클릭 모드: 등급 + 세부타입 둘 다 체크
                                if (IsIndividualSelectionMode)
                                {
                                    string slotDetailedType = GetDetailedEquipmentType(slotData);
                                    isMatching = (slotData.itemGrade == targetGrade && slotDetailedType == targetDetailedType);
                                }
                                // ⭐ 일괄 선택 모드: 등급만 체크 (여러 타입 허용)
                                else
                                {
                                    isMatching = (slotData.itemGrade == targetGrade);
                                }

                                slot.SetDimmed(!isMatching);
                            }
                        }
                    }
                }
            }
            else
            {
                // 합성 탭이 아니거나 선택이 없으면 모든 슬롯 밝게
                foreach (var slot in inventorySlots)
                {
                    slot.SetDimmed(false);
                }
            }
        }
        
        /// <summary>
        /// 등급별 일괄 선택
        /// </summary>
        public void SelectAllByGrade(ItemGrade grade)
        {
            
            // ⭐ 0단계: 기존 선택 완전 초기화 (다른 등급 선택 시 깔끔하게 시작)
            ClearSelection();
            
            
            // ⭐ 같은 등급 재선택 시 토글(해제)
            bool alreadySelectedAll = true;
            foreach (var slot in inventorySlots)
            {
                var itemData = slot.GetEquipmentData();
                if (itemData != null && itemData.itemGrade == grade)
                {
                    if (!slot.IsSelected())
                    {
                        alreadySelectedAll = false;
                        break;
                    }
                }
            }
            
            if (alreadySelectedAll && selectedItems.Count > 0)
            {
                // 같은 등급 전체가 이미 선택되어 있으면 해제
                
                foreach (var slot in inventorySlots)
                {
                    var itemData = slot.GetEquipmentData();
                    if (itemData != null && itemData.itemGrade == grade)
                    {
                        slot.SetSelected(false, notifyEvent: false);
                        selectedItems.Remove(slot.GetItemInstanceID());
                    }
                }
                
                UpdateSelectionUI();
                OnSelectionChanged?.Invoke(selectedItems);
                
                
                return;
            }
            
            // ⭐ 일괄 선택 모드 활성화
            IsIndividualSelectionMode = false; // 일괄 선택 = 개별 클릭 아님
            
            // 다중 선택 모드 활성화
            if (!isMultiSelectMode)
            {
                SetMultiSelectMode(true);
                if (multiSelectModeToggle != null)
                {
                    multiSelectModeToggle.isOn = true;
                }
            }
            
            bool excludeEquipped = excludeEquippedToggle != null && excludeEquippedToggle.isOn;
            var equippedIds = PlayerDataManager.Instance.selectedPlayerData
                .RuntimeEquippedInstanceIds.Values.ToList();
            
            // ⭐ 합성 탭: 필요 개수의 배수로만 선택
            bool isFusionTab = (currentWorkshopTab == WorkshopUI.WorkshopTabType.Fusion);
            int requiredCount = 0;
            int maxSelectable = int.MaxValue;
            
            if (isFusionTab)
            {
                // FusionRule에서 필요 개수 가져오기
                var fusionRule = Resources.Load<Systems.FusionRule>("Data/FusionRule");
                if (fusionRule != null)
                {
                    requiredCount = fusionRule.GetRequiredCount(grade);
                    
                }
                else
                {
                    Debug.LogWarning("⚠️ [WorkshopInventoryUI] FusionRule을 찾을 수 없습니다! (Resources/Data/FusionRule)");
                }
            }
            
            // ⭐ 1단계: 선택 가능한 아이템 수집
            var candidateSlots = new List<(InventorySlot slot, ItemInstanceID itemId, EquipmentData equipData, int enhancementLevel)>();
            
            foreach (var slot in inventorySlots)
            {
                var itemData = slot.GetEquipmentData();
                if (itemData != null && itemData.itemGrade == grade)
                {
                    var itemId = slot.GetItemInstanceID();
                    
                    // 장착 아이템 제외 옵션 체크
                    if (excludeEquipped && equippedIds.Contains(itemId))
                    {
                        continue;
                    }
                    
                    var itemInstanceData = AccountDataManager.Instance.GetInstance(itemId);
                    if (itemInstanceData != null)
                    {
                        candidateSlots.Add((slot, itemId, itemData, itemInstanceData.enhancementLevel));
                    }
                }
            }
            
            int selectedCount = 0;
            
            // ⭐ 2단계: 합성 탭 - 세부타입별로 그룹핑하여 각 타입별로 선택
            if (isFusionTab && requiredCount > 0)
            {
                // 세부타입별로 그룹핑 (Helmet ≠ Armor)
                var groupedByDetailedType = candidateSlots
                    .GroupBy(c => GetDetailedEquipmentType(c.equipData))
                    .ToList();
                
                
                // ⭐ 모든 세부타입을 순회하면서 각각 배수만큼 선택
                foreach (var typeGroup in groupedByDetailedType)
                {
                    string detailedType = typeGroup.Key;
                    
                    // ⭐ 강화되지 않은 아이템만 자동 선택 (enhancementLevel == 0)
                    var itemsInGroup = typeGroup
                        .Where(c => c.enhancementLevel == 0)  // 비강화만 필터링
                        .ToList();
                    
                    int totalAvailableForType = itemsInGroup.Count;
                    int maxSelectableForType = (totalAvailableForType / requiredCount) * requiredCount;
                    
                    
                    // 배수만큼 선택
                    int selectedInType = 0;
                    foreach (var (slot, itemId, equipData, enhancementLevel) in itemsInGroup)
                    {
                        if (selectedInType >= maxSelectableForType)
                            break;
                        
                        slot.SetSelected(true, notifyEvent: false);
                        if (!selectedItems.Contains(itemId))
                        {
                            selectedItems.Add(itemId);
                        }
                        
                        selectedInType++;
                        selectedCount++;
                    }
                }
            }
            else // 비합성 탭 - 기존 로직
            {
                // ⭐ 강화되지 않은 아이템만 자동 선택 (비합성 탭도 동일)
                candidateSlots = candidateSlots
                    .Where(c => c.enhancementLevel == 0)  // 비강화만 필터링
                    .ToList();
                
                foreach (var (slot, itemId, equipData, enhancementLevel) in candidateSlots)
                {
                    slot.SetSelected(true, notifyEvent: false);
                    if (!selectedItems.Contains(itemId))
                    {
                        selectedItems.Add(itemId);
                    }
                    selectedCount++;
                }
            }
            
            // ⭐ 선택 완료 후 한 번만 UI 갱신 및 이벤트 발생
            UpdateSelectionUI();
            OnSelectionChanged?.Invoke(selectedItems);
            
                    Dbg.Log($"⚗️ [WorkshopInventoryUI] {grade}등급 일괄 선택 완료: {selectedCount}개 (비강화만, 타입별 배수 선택), selectedItems.Count={selectedItems.Count}");
                    Dbg.Log($"🔘 [WorkshopInventoryUI] {grade}등급 일괄 선택 완료: {selectedCount}개 (비강화만), selectedItems.Count={selectedItems.Count}");
        }
        
        /// <summary>
        /// 선택 초기화
        /// </summary>
        public void ClearSelection()
        {
            foreach (var slot in inventorySlots)
            {
                if (slot.IsSelected())
                {
                    slot.SetSelected(false, notifyEvent: false); // ⭐ 개별 이벤트는 발생시키지 않음
                }
            }
            
            selectedItems.Clear();
            
            // ⭐ 선택 모드 리셋
            IsIndividualSelectionMode = false;
            
            UpdateSelectionUI();
            
            // ⭐ 선택 초기화 이벤트 발생 (빈 리스트)
            OnSelectionChanged?.Invoke(new List<ItemInstanceID>());
            
        }
        
        /// <summary>
        /// 선택된 아이템 목록 가져오기
        /// </summary>
        public List<ItemInstanceID> GetSelectedItems()
        {
            return new List<ItemInstanceID>(selectedItems);
        }
        
        /// <summary>
        /// 다중 선택 모드 여부
        /// </summary>
        public bool IsMultiSelectMode()
        {
            return isMultiSelectMode;
        }
        
        /// <summary>
        /// ⭐ 플레이어 보유 골드 표시 업데이트
        /// </summary>
        private void UpdatePlayerGoldDisplay()
        {
            if (playerGoldText != null && PlayerDataManager.Instance != null)
            {
                int currentGold = PlayerDataManager.Instance.CurrentGold;
                playerGoldText.text = $"{currentGold:N0}"; // 천 단위 쉼표 포함
                
            }
            else
            {
                    Debug.LogWarning("⚠️ [WorkshopInventoryUI] playerGoldText가 null입니다! Inspector에서 연결하세요.");
            }
        }
    }
    
    /// <summary>
    /// 인벤토리 탭 타입
    /// </summary>
    public enum InventoryTabType
    {
        Equipment,   // 장비
        Consumable,  // 소모품
        Material     // 재료
    }
}

