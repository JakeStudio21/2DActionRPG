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
        [SerializeField] private bool showDebugLogs = true;
        
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
        private List<ItemInstanceId> selectedItems = new List<ItemInstanceId>();
        private bool isMultiSelectMode = false;
        private InventorySlot lastSelectedSlot = null; // 단일 선택 시 마지막 선택 슬롯
        
        // 이벤트
        public event Action<List<ItemInstanceId>> OnSelectionChanged;
        public event Action<ItemInstanceId> OnSingleItemSelected; // 단일 선택 시
        
        void Awake()
        {
            if (showDebugLogs)
                Debug.Log("🏭 [WorkshopInventoryUI] Awake() - 공방 인벤토리 초기화");
            
            InitializeUI();
            SetupEventListeners();
        }
        
        void Start()
        {
            if (showDebugLogs)
                Debug.Log("✅ [WorkshopInventoryUI] Start() - 준비 완료");
            
            // 초기 표시
            RefreshInventoryDisplay();
        }
        
        /// <summary>
        /// GameObject 활성화 시 자동 갱신 + 이벤트 구독
        /// </summary>
        void OnEnable()
        {
            Debug.Log("🟢 [WorkshopInventoryUI] OnEnable() 호출됨!");
            
            // ⭐ WorkshopUI 탭 변경 이벤트 구독
            if (workshopUI != null)
            {
                workshopUI.OnTabChanged += OnWorkshopTabChanged;
                
                // 현재 탭 가져오기
                currentWorkshopTab = workshopUI.GetCurrentTab();
                
                if (showDebugLogs)
                    Debug.Log($"🔗 [WorkshopInventoryUI] WorkshopUI 이벤트 구독 (현재 탭: {currentWorkshopTab})");
            }
            else
            {
                Debug.LogWarning("⚠️ [WorkshopInventoryUI] workshopUI가 null입니다! Inspector에서 연결하세요.");
            }
            
            // ⭐ 공방 패널이 열릴 때마다 인벤토리 자동 갱신
            // (상점에서 아이템 구매 후 공방에 들어왔을 때 반영되도록)
            if (inventorySlots != null && inventorySlots.Count > 0)
            {
                Debug.Log("🔄 [WorkshopInventoryUI] OnEnable() - 인벤토리 자동 갱신 시작...");
                RefreshInventoryDisplay();
                Debug.Log("✅ [WorkshopInventoryUI] OnEnable() - 인벤토리 자동 갱신 완료");
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
                
                Debug.Log("✅ [WorkshopInventoryUI] AccountDataManager 이벤트 구독 완료");
            }
            else
            {
                Debug.LogWarning("⚠️ [WorkshopInventoryUI] AccountDataManager.Instance가 null입니다!");
            }
            
            // ⭐ 골드 변경 이벤트 구독 (실시간 갱신)
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnGoldChanged += OnGoldChangedHandler;
                Debug.Log("✅ [WorkshopInventoryUI] PlayerDataManager.OnGoldChanged 구독 완료");
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
                
                if (showDebugLogs)
                    Debug.Log("🔄 [WorkshopInventoryUI] WorkshopUI 이벤트 구독 해제");
            }
            
            // ⭐ AccountDataManager 이벤트 구독 해제 (메모리 누수 방지)
            if (AccountDataManager.Instance != null)
            {
                AccountDataManager.Instance.OnSharedInventoryChanged -= OnInventoryChangedHandler;
                AccountDataManager.Instance.OnMaterialChanged -= OnMaterialChangedHandler;
                
                if (showDebugLogs)
                    Debug.Log("🔄 [WorkshopInventoryUI] AccountDataManager 이벤트 구독 해제");
            }
            
            // ⭐ 골드 변경 이벤트 구독 해제
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnGoldChanged -= OnGoldChangedHandler;
                
                if (showDebugLogs)
                    Debug.Log("🔄 [WorkshopInventoryUI] PlayerDataManager 이벤트 구독 해제");
            }
        }
        
        /// <summary>
        /// 공방 탭 변경 이벤트 핸들러
        /// </summary>
        private void OnWorkshopTabChanged(WorkshopUI.WorkshopTabType newTab)
        {
            if (showDebugLogs)
                Debug.Log($"🔄 [WorkshopInventoryUI] 공방 탭 변경: {currentWorkshopTab} → {newTab}");
            
            // ⭐ 선택 초기화 먼저 (이전 모드에서 선택 해제)
            ClearSelection();
            
            // 탭 변경
            currentWorkshopTab = newTab;
            
            // ⭐ 탭별 UI 조정 (새 모드 설정)
            UpdateUIForCurrentTab();
            
            if (showDebugLogs)
                Debug.Log($"✅ [WorkshopInventoryUI] 탭 변경 완료: {newTab}");
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
                if (showDebugLogs)
                    Debug.Log("🔨 [WorkshopInventoryUI] 강화 탭 → 단일 선택 모드");
            }
            else
            {
                // 분해/합성: 다중 선택 (SelectionCheckbox)
                SetMultiSelectMode(true);
                if (showDebugLogs)
                    Debug.Log("🔧 [WorkshopInventoryUI] 분해/합성 탭 → 다중 선택 모드");
            }
            
            // 모든 슬롯의 체크박스 표시/숨김 갱신
            UpdateAllSlotCheckboxVisibility();
            
            if (showDebugLogs)
                Debug.Log($"✅ [WorkshopInventoryUI] 탭별 UI 조정 완료 (강화 탭: {isEnhancementTab})");
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
            
            if (showDebugLogs)
                Debug.Log($"🔄 [WorkshopInventoryUI] 모든 슬롯 체크박스 표시: {showCheckbox}");
        }
        
        /// <summary>
        /// 공유 창고 인벤토리 변경 이벤트 핸들러
        /// </summary>
        private void OnInventoryChangedHandler()
        {
            Debug.Log("🔔🔔🔔 [WorkshopInventoryUI] 공유 창고 변경 감지! 인벤토리 갱신 시작...");
            Debug.Log($"   현재 GameObject 활성화 상태: {gameObject.activeInHierarchy}");
            Debug.Log($"   현재 탭: {currentTab}");
            
            RefreshInventoryDisplay();
            
            Debug.Log("✅ [WorkshopInventoryUI] 인벤토리 갱신 완료!");
        }
        
        /// <summary>
        /// 재료 변경 이벤트 핸들러
        /// </summary>
        private void OnMaterialChangedHandler(MaterialType materialType, int newCount)
        {
            if (showDebugLogs)
                Debug.Log($"🔔 [WorkshopInventoryUI] 재료 변경 감지: {materialType.GetDisplayName()} = {newCount}개");
            
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
            
            if (showDebugLogs)
                Debug.Log($"🔔 [WorkshopInventoryUI] 골드 변경 감지: {newGold:N0}G - 표시 갱신");
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
            
            if (showDebugLogs)
                Debug.Log("✅ [WorkshopInventoryUI] 이벤트 리스너 설정 완료");
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
            
            if (showDebugLogs)
                Debug.Log($"✅ [WorkshopInventoryUI] {maxSlots}개 슬롯 생성 완료");
        }
        
        /// <summary>
        /// 탭 전환
        /// </summary>
        public void SwitchTab(InventoryTabType tabType)
        {
            currentTab = tabType;
            
            UpdateTabButtonStates();
            RefreshInventoryDisplay();
            
            if (showDebugLogs)
                Debug.Log($"🔄 [WorkshopInventoryUI] 탭 전환: {tabType}");
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
            Debug.Log($"🔄 [WorkshopInventoryUI] RefreshInventoryDisplay() 시작 - 현재 탭: {currentTab}");
            
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
            
            Debug.Log("✅ [WorkshopInventoryUI] RefreshInventoryDisplay() 완료");
        }
        
        /// <summary>
        /// 장비 탭 갱신
        /// </summary>
        private void RefreshEquipmentTab()
        {
            var filteredItems = GetFilteredEquipmentItems();
            
            Debug.Log($"📦 [WorkshopInventoryUI] RefreshEquipmentTab() - 필터링된 아이템: {filteredItems.Count}개");
            
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
            
            if (showDebugLogs)
                Debug.Log($"✅ [WorkshopInventoryUI] 장비 탭 갱신: {filteredItems.Count}개 아이템");
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
            
            if (showDebugLogs)
                Debug.Log($"✅ [WorkshopInventoryUI] 재료 탭 갱신: {materials.Count}개 재료");
        }
        
        /// <summary>
        /// 필터링된 장비 아이템 목록 가져오기
        /// </summary>
        private List<ItemInstanceId> GetFilteredEquipmentItems()
        {
            var accountData = AccountDataManager.Instance.GetAccountData();
            var allItems = accountData.sharedInventoryIds;
            
            Debug.Log($"🔍 [WorkshopInventoryUI] GetFilteredEquipmentItems() - 전체 아이템: {allItems.Count}개");
            
            // 장비만 필터링
            var equipmentItems = allItems.Where(id =>
            {
                var itemData = AccountDataManager.Instance.GetInstance(id);
                if (itemData == null) return false;
                
                var equipmentData = LoadEquipmentData(itemData.templateName);
                return equipmentData != null;
            }).ToList();
            
            Debug.Log($"   장비 아이템: {equipmentItems.Count}개");
            
            // 장착 아이템 제외 옵션
            if (excludeEquippedToggle != null && excludeEquippedToggle.isOn)
            {
                var equippedIds = PlayerDataManager.Instance.selectedPlayerData
                    .RuntimeEquippedInstanceIds.Values.ToList();
                
                equipmentItems = equipmentItems.Where(id => !equippedIds.Contains(id)).ToList();
                
                Debug.Log($"   장착 아이템 제외 후: {equipmentItems.Count}개");
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
            
            if (showDebugLogs)
                Debug.Log($"🔘 [WorkshopInventoryUI] 다중 선택 모드: {(enabled ? "활성화" : "비활성화")}");
        }
        
        /// <summary>
        /// 슬롯 선택 콜백
        /// </summary>
        private void OnSlotSelectionChanged(ItemInstanceId itemId, bool selected)
        {
            if (selected)
            {
                // ⭐ 합성 탭: 같은 등급만 선택 가능 (다중 선택 모드)
                if (isMultiSelectMode && currentWorkshopTab == WorkshopUI.WorkshopTabType.Fusion && selectedItems.Count > 0)
                {
                    // 현재 선택하려는 아이템의 등급
                    var newItemInstance = AccountDataManager.Instance.GetInstance(itemId);
                    if (newItemInstance != null)
                    {
                        var newItemData = ItemTemplateResolver.Load(newItemInstance.templateName);
                        if (newItemData != null)
                        {
                            // 기존 선택된 아이템들의 등급 확인
                            var firstSelectedId = selectedItems[0];
                            var firstItemInstance = AccountDataManager.Instance.GetInstance(firstSelectedId);
                            if (firstItemInstance != null)
                            {
                                var firstItemData = ItemTemplateResolver.Load(firstItemInstance.templateName);
                                if (firstItemData != null)
                                {
                                    // 등급이 다르면 이전 선택 모두 초기화
                                    if (newItemData.itemGrade != firstItemData.itemGrade)
                                    {
                                        if (showDebugLogs)
                                            Debug.Log($"⚗️ [WorkshopInventoryUI] 합성 탭 - 다른 등급 선택 감지! {firstItemData.itemGrade} → {newItemData.itemGrade}");
                                        
                                        // 모든 선택 해제
                                        ClearSelection();
                                        
                                        if (showDebugLogs)
                                            Debug.Log($"✅ [WorkshopInventoryUI] 이전 선택 초기화 완료, 새 등급 {newItemData.itemGrade} 선택 시작");
                                    }
                                }
                            }
                        }
                    }
                }
                
                // ⭐ 단일 선택 모드: 이전 선택 해제
                if (!isMultiSelectMode)
                {
                    // 이전 선택 슬롯 해제
                    if (lastSelectedSlot != null && lastSelectedSlot.GetItemInstanceId() != itemId)
                    {
                        lastSelectedSlot.SetSelected(false, notifyEvent: false);
                        
                        if (showDebugLogs)
                            Debug.Log($"🔄 [WorkshopInventoryUI] 이전 선택 해제: {lastSelectedSlot.GetItemInstanceId()}");
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
                    
                    if (showDebugLogs)
                        Debug.Log($"🎯 [WorkshopInventoryUI] EnhancementUI에 아이템 전달: {itemId}");
                }
                else
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"⚠️ [WorkshopInventoryUI] enhancementUI가 null입니다!");
                }
                }
                else
                {
                    // 다중 선택 모드: 리스트에 추가
                    if (!selectedItems.Contains(itemId))
                    {
                        selectedItems.Add(itemId);
                        
                        if (showDebugLogs)
                            Debug.Log($"🔘 [WorkshopInventoryUI] 아이템 선택 (다중): {itemId}");
                    }
                }
            }
            else
            {
                // 선택 해제
                selectedItems.Remove(itemId);
                
                if (showDebugLogs)
                    Debug.Log($"🔘 [WorkshopInventoryUI] 아이템 선택 해제: {itemId}");
                
                // 단일 선택 모드에서 선택 해제 시 EnhancementUI 초기화
                if (!isMultiSelectMode && selectedItems.Count == 0 && enhancementUI != null)
                {
                    enhancementUI.ClearSelection();
                    lastSelectedSlot = null;
                    
                    if (showDebugLogs)
                        Debug.Log($"🔄 [WorkshopInventoryUI] EnhancementUI 초기화");
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
        /// ItemInstanceId로 슬롯 찾기
        /// </summary>
        private InventorySlot FindSlotByItemId(ItemInstanceId itemId)
        {
            foreach (var slot in inventorySlots)
            {
                if (slot.GetItemInstanceId() == itemId)
                {
                    return slot;
                }
            }
            return null;
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
        }
        
        /// <summary>
        /// 등급별 일괄 선택
        /// </summary>
        public void SelectAllByGrade(ItemGrade grade)
        {
            if (showDebugLogs)
                Debug.Log($"🎯 [WorkshopInventoryUI] {grade}등급 일괄 선택 시작");
            
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
                if (showDebugLogs)
                    Debug.Log($"🔄 [WorkshopInventoryUI] {grade}등급 이미 선택됨 → 해제");
                
                foreach (var slot in inventorySlots)
                {
                    var itemData = slot.GetEquipmentData();
                    if (itemData != null && itemData.itemGrade == grade)
                    {
                        slot.SetSelected(false, notifyEvent: false);
                        selectedItems.Remove(slot.GetItemInstanceId());
                    }
                }
                
                UpdateSelectionUI();
                OnSelectionChanged?.Invoke(selectedItems);
                
                if (showDebugLogs)
                    Debug.Log($"✅ [WorkshopInventoryUI] {grade}등급 해제 완료, selectedItems.Count={selectedItems.Count}");
                
                return;
            }
            
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
            
            // ⭐ 합성 탭: 필요 개수 제한 + 강화 레벨 +0만 선택
            bool isFusionTab = (currentWorkshopTab == WorkshopUI.WorkshopTabType.Fusion);
            int requiredCount = 0;
            
            if (isFusionTab)
            {
                // FusionRule에서 필요 개수 가져오기
                var fusionRule = Resources.Load<Systems.FusionRule>("Data/FusionRule");
                if (fusionRule != null)
                {
                    requiredCount = fusionRule.GetRequiredCount(grade);
                    
                    if (showDebugLogs)
                        Debug.Log($"⚗️ [WorkshopInventoryUI] 합성 탭 - {grade}등급 필요 개수: {requiredCount}개 (강화 +0만 선택)");
                }
                else
                {
                    Debug.LogWarning("⚠️ [WorkshopInventoryUI] FusionRule을 찾을 수 없습니다! (Resources/Data/FusionRule)");
                }
            }
            
            int selectedCount = 0;
            
            foreach (var slot in inventorySlots)
            {
                var itemData = slot.GetEquipmentData();
                if (itemData != null && itemData.itemGrade == grade)
                {
                    var itemId = slot.GetItemInstanceId();
                    
                    // 장착 아이템 제외 옵션 체크
                    if (excludeEquipped && equippedIds.Contains(itemId))
                    {
                        continue;
                    }
                    
                    // ⭐ 합성 탭: 강화 레벨 +0만 자동 선택
                    if (isFusionTab)
                    {
                        var itemInstanceData = AccountDataManager.Instance.GetInstance(itemId);
                        if (itemInstanceData == null || itemInstanceData.enhancementLevel > 0)
                        {
                            // 강화된 아이템은 자동 선택에서 제외
                            continue;
                        }
                        
                        // 필요 개수만큼만 선택
                        if (selectedCount >= requiredCount)
                        {
                            break; // 필요 개수 도달, 더 이상 선택 안 함
                        }
                    }
                    
                    // ⭐ 개별 이벤트는 발생시키지 않음 (마지막에 한 번만 발생)
                    slot.SetSelected(true, notifyEvent: false);
                    
                    // ⭐ selectedItems에 수동으로 추가 (이벤트 미발생이므로)
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
            
            if (showDebugLogs)
            {
                if (isFusionTab)
                    Debug.Log($"⚗️ [WorkshopInventoryUI] {grade}등급 일괄 선택 완료: {selectedCount}/{requiredCount}개 (강화 +0만), selectedItems.Count={selectedItems.Count}");
                else
                    Debug.Log($"🔘 [WorkshopInventoryUI] {grade}등급 일괄 선택 완료: {selectedCount}개, selectedItems.Count={selectedItems.Count}");
            }
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
            UpdateSelectionUI();
            
            // ⭐ 선택 초기화 이벤트 발생 (빈 리스트)
            OnSelectionChanged?.Invoke(new List<ItemInstanceId>());
            
            if (showDebugLogs)
                Debug.Log("✅ [WorkshopInventoryUI] 선택 초기화 완료 - 이벤트 발생");
        }
        
        /// <summary>
        /// 선택된 아이템 목록 가져오기
        /// </summary>
        public List<ItemInstanceId> GetSelectedItems()
        {
            return new List<ItemInstanceId>(selectedItems);
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
                
                if (showDebugLogs)
                    Debug.Log($"💰 [WorkshopInventoryUI] 골드 표시 업데이트: {currentGold:N0}G");
            }
            else
            {
                if (playerGoldText == null && showDebugLogs)
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

