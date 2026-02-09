using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Managers;
using Systems;
using UI.Popups;

namespace UI.Workshop
{
    /// <summary>
    /// 강화 UI (우측 제작 패널)
    /// 
    /// 책임:
    /// - 선택된 아이템 표시
    /// - 필요 재료/골드 계산 및 표시
    /// - 성공 확률 표시
    /// - 강화 실행 처리
    /// 
    /// 연동:
    /// - WorkshopInventoryUI (아이템 선택 이벤트)
    /// - BeforeAfterComparisonUI (제작 전/후 미리보기)
    /// - EnhancementSystem (강화 로직)
    /// </summary>
    public class EnhancementUI : MonoBehaviour
    {
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = false; // 🔧 디버깅 완료
        
        [Header("📌 선택된 아이템 정보")]
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private Image itemIconImage; // ⚠️ BeforeAfterComparisonUI와 중복, 숨김 처리됨
        [SerializeField] private TMP_Text enhancementLevelText;
        
        [Header("📊 성공 확률 표시")]
        [SerializeField] private TMP_Text successRateText;
        [SerializeField] private Image successRateBar;
        [SerializeField] private GameObject successRatePanel;
        
        [Header("📦 필요 재료 슬롯")]
        [SerializeField] private InventorySlot materialSlot1;
        [SerializeField] private InventorySlot materialSlot2; // 추가 재료 (미래 확장용)
        [SerializeField] private InventorySlot materialSlot3; // 추가 재료 (미래 확장용)
        [SerializeField] private TMP_Text goldCostText;        // 필요 골드 표시
        [SerializeField] private TMP_Text playerGoldText;      // 🆕 플레이어 보유 골드 표시
        [SerializeField] private GameObject materialCostPanel;
        
        [Header("⚠️ 실패 경고 패널")]
        [SerializeField] private TMP_Text warningText;
        [SerializeField] private Image warningIcon;
        [SerializeField] private GameObject warningPanel;
        
        [Header("🔘 강화 버튼")]
        [SerializeField] private Button enhanceButton;
        [SerializeField] private TMP_Text enhanceButtonText;
        [SerializeField] private TMP_Text enhanceWarningText; // ⭐ 버튼 외부 경고 텍스트 (조건 불만족 시)
        
        [Header("🔗 연동 컴포넌트")]
        [SerializeField] private BeforeAfterComparisonUI comparisonUI;
        [SerializeField] private WorkshopInventoryUI workshopInventoryUI;
        
        // ========== 상태 ==========
        private ItemInstanceId selectedItemId;
        private ItemInstanceData selectedItemData;
        private EquipmentData selectedEquipmentData;
        private bool isItemSelected => selectedItemId.IsValid();
        
        // ========== 이벤트 ==========
        public event Action<ItemInstanceId> OnItemSelected;
        public event Action<EnhancementResult> OnEnhancementComplete;
        
        // ========================================
        // 초기화
        // ========================================
        
        void OnEnable()
        {
            if (showDebugLogs)
                Debug.Log("🟢 [EnhancementUI] OnEnable() 호출됨");
            
            // 🆕 골드 변경 이벤트 구독
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnGoldChanged -= OnGoldChanged;
                PlayerDataManager.Instance.OnGoldChanged += OnGoldChanged;
                
                if (showDebugLogs)
                    Debug.Log("✅ [EnhancementUI] PlayerDataManager.OnGoldChanged 구독 완료");
            }
            else
            {
                Debug.LogError("❌ [EnhancementUI] PlayerDataManager.Instance가 NULL입니다!");
            }
            
            // 🆕 재료 변경 이벤트 구독
            if (AccountDataManager.Instance != null)
            {
                AccountDataManager.Instance.OnMaterialChanged -= OnMaterialChanged;
                AccountDataManager.Instance.OnMaterialChanged += OnMaterialChanged;
                
                if (showDebugLogs)
                    Debug.Log("✅ [EnhancementUI] AccountDataManager.OnMaterialChanged 구독 완료");
            }
            else
            {
                Debug.LogError("❌ [EnhancementUI] AccountDataManager.Instance가 NULL입니다!");
            }
            
            // 초기 골드 표시
            int currentGold = GetPlayerGold();
            if (showDebugLogs)
                Debug.Log($"🔍 [EnhancementUI] OnEnable() - 현재 골드: {currentGold}");
            
            UpdatePlayerGoldDisplay();
        }
        
        void OnDisable()
        {
            // 골드 변경 이벤트 구독 해제
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnGoldChanged -= OnGoldChanged;
            }
            
            // 재료 변경 이벤트 구독 해제
            if (AccountDataManager.Instance != null)
            {
                AccountDataManager.Instance.OnMaterialChanged -= OnMaterialChanged;
            }
        }
        
        void Start()
        {
            InitializeUI();
            
            // 버튼 이벤트 연결
            if (enhanceButton != null)
            {
                enhanceButton.onClick.AddListener(OnEnhanceButtonClicked);
            }
            
            // WorkshopInventoryUI와 연동 (아이템 선택 이벤트 구독)
            if (workshopInventoryUI != null)
            {
                // 아이템 클릭 이벤트 구독 (WorkshopInventoryUI에서 발생)
                // TODO: WorkshopInventoryUI에서 이벤트 추가 필요
            }
            
            if (showDebugLogs)
                Debug.Log("✅ [EnhancementUI] 초기화 완료");
        }
        
        /// <summary>
        /// UI 초기 상태 설정
        /// </summary>
        private void InitializeUI()
        {
            // ⭐ ItemIcon 숨김 처리 (BeforeAfterComparisonUI와 중복)
            if (itemIconImage != null)
            {
                itemIconImage.gameObject.SetActive(false);
                
                if (showDebugLogs)
                    Debug.Log("🔄 [EnhancementUI] ItemIcon 숨김 처리 (BeforeAfterComparisonUI와 중복)");
            }
            
            // 모든 패널 비활성화
            if (successRatePanel != null)
                successRatePanel.SetActive(false);
            
            if (materialCostPanel != null)
                materialCostPanel.SetActive(false);
            
            if (warningPanel != null)
                warningPanel.SetActive(false);
            
            // 버튼 비활성화
            if (enhanceButton != null)
            {
                enhanceButton.interactable = false;
                
                if (enhanceButtonText != null)
                    enhanceButtonText.text = "아이템선택";
            }
            
            // ⭐ 경고 텍스트 초기화
            if (enhanceWarningText != null)
            {
                enhanceWarningText.text = "";
                enhanceWarningText.gameObject.SetActive(false);
            }
            
            // 재료 슬롯 초기화
            if (materialSlot1 != null)
                materialSlot1.ClearSlot();
            
            if (materialSlot2 != null)
                materialSlot2.ClearSlot();
            
            if (materialSlot3 != null)
                materialSlot3.ClearSlot();
        }
        
        // ========================================
        // Public API
        // ========================================
        
        /// <summary>
        /// 아이템 선택 처리 (외부에서 호출)
        /// </summary>
        public void OnSelectedItemChanged(ItemInstanceId itemId)
        {
            selectedItemId = itemId;
            
            if (!itemId.IsValid())
            {
                ClearSelection();
                return;
            }
            
            // 아이템 데이터 로드
            selectedItemData = AccountDataManager.Instance.GetInstance(itemId);
            if (selectedItemData == null)
            {
                Debug.LogWarning($"⚠️ [EnhancementUI] 아이템 데이터를 찾을 수 없습니다: {itemId}");
                ClearSelection();
                return;
            }
            
            // EquipmentData 로드
            selectedEquipmentData = LoadEquipmentData(selectedItemData.templateName);
            if (selectedEquipmentData == null)
            {
                Debug.LogWarning($"⚠️ [EnhancementUI] EquipmentData를 찾을 수 없습니다: {selectedItemData.templateName}");
                ClearSelection();
                return;
            }
            
            if (showDebugLogs)
                Debug.Log($"🎯 [EnhancementUI] 아이템 선택: {selectedEquipmentData.equipmentName} +{selectedItemData.enhancementLevel} (ID: {itemId})");
            
            // UI 갱신
            RefreshUI();
            
            // BeforeAfterComparisonUI 업데이트
            if (comparisonUI != null)
            {
                comparisonUI.SetupForEnhancement(selectedItemId);
            }
            
            // 이벤트 발생
            OnItemSelected?.Invoke(itemId);
        }
        
        /// <summary>
        /// UI 전체 갱신
        /// </summary>
        public void RefreshUI()
        {
            if (!isItemSelected)
            {
                ClearSelection();
                return;
            }
            
            UpdateItemInfoDisplay();
            UpdateMaterialSlots();
            UpdateSuccessRateDisplay();
            UpdateWarningPanel();
            UpdateEnhanceButton();
        }
        
        /// <summary>
        /// 선택 해제
        /// </summary>
        public void ClearSelection()
        {
            selectedItemId = default;
            selectedItemData = null;
            selectedEquipmentData = null;
            
            InitializeUI();
            
            if (showDebugLogs)
                Debug.Log("🔄 [EnhancementUI] 선택 해제");
        }
        
        // ========================================
        // Phase A: 기본 구조
        // ========================================
        
        /// <summary>
        /// 선택된 아이템 정보 표시
        /// </summary>
        private void UpdateItemInfoDisplay()
        {
            if (!isItemSelected) return;
            
            // 아이템 이름
            if (itemNameText != null)
            {
                string displayName = $"{selectedEquipmentData.equipmentName} +{selectedItemData.enhancementLevel}";
                itemNameText.text = displayName;
            }
            
            // ⚠️ 아이템 아이콘 (BeforeAfterComparisonUI와 중복, 숨김 처리됨)
            // if (itemIconImage != null && selectedEquipmentData.icon != null)
            // {
            //     itemIconImage.sprite = selectedEquipmentData.icon;
            //     itemIconImage.enabled = true;
            // }
            
            // 강화 레벨 표시
            if (enhancementLevelText != null)
            {
                enhancementLevelText.text = $"+{selectedItemData.enhancementLevel} → +{selectedItemData.enhancementLevel + 1}";
            }
        }
        
        // ========================================
        // Phase B: 재료/골드 계산
        // ========================================
        
        /// <summary>
        /// 필요 재료 슬롯 업데이트
        /// </summary>
        private void UpdateMaterialSlots()
        {
            if (!isItemSelected)
            {
                if (materialCostPanel != null)
                    materialCostPanel.SetActive(false);
                return;
            }
            
            // ⭐ 중요: materialCostPanel 활성화 **후** 1프레임 대기한 다음 슬롯 설정
            // 이유: Unity UI Layout 시스템이 활성화 직후 재계산하면서 Image.enabled를 false로 설정하는 문제 방지
            if (materialCostPanel != null && !materialCostPanel.activeSelf)
            {
                Debug.Log("🔍 [EnhancementUI] materialCostPanel 활성화 → 코루틴으로 1프레임 대기 후 슬롯 설정");
                materialCostPanel.SetActive(true);
                StartCoroutine(UpdateMaterialSlotsDelayed());
                return;
            }
            
            // materialCostPanel이 이미 활성화된 경우 즉시 업데이트
            UpdateMaterialSlotsImmediate();
        }
        
        /// <summary>
        /// ⭐ Unity UI 버그 해결: materialCostPanel 활성화 후 1프레임 대기한 다음 슬롯 설정
        /// </summary>
        private System.Collections.IEnumerator UpdateMaterialSlotsDelayed()
        {
            // 1프레임 대기 (Unity UI Layout 계산 완료 대기)
            yield return null;
            
            Debug.Log("⏰ [EnhancementUI] 1프레임 대기 완료 → MaterialSlot 설정 시작");
            UpdateMaterialSlotsImmediate();
        }
        
        /// <summary>
        /// Material Slot 즉시 업데이트 (코루틴에서 호출)
        /// </summary>
        private void UpdateMaterialSlotsImmediate()
        {
            // 모든 슬롯 Clear (이전 데이터 제거)
            Debug.Log("🧹 [EnhancementUI] 모든 MaterialSlot Clear 시작");
            if (materialSlot1 != null)
                materialSlot1.ClearSlot();
            if (materialSlot2 != null)
                materialSlot2.ClearSlot();
            if (materialSlot3 != null)
                materialSlot3.ClearSlot();
            Debug.Log("✅ [EnhancementUI] 모든 MaterialSlot Clear 완료");
            
            // 필요 재료 계산
            var requiredMaterials = CalculateRequiredMaterials();
            if (showDebugLogs)
            {
                foreach (var kvp in requiredMaterials)
                {
                    Debug.Log($"💎 [EnhancementUI] 필요 재료: {kvp.Key.GetDisplayName()} x{kvp.Value}");
                }
            }
            
            // 재료 슬롯 업데이트
            int slotIndex = 0;
            foreach (var kvp in requiredMaterials)
            {
                MaterialType materialType = kvp.Key;
                int requiredAmount = kvp.Value;
                int ownedAmount = AccountDataManager.Instance.GetMaterialCount(materialType);
                
                InventorySlot targetSlot = slotIndex switch
                {
                    0 => materialSlot1,
                    1 => materialSlot2,
                    2 => materialSlot3,
                    _ => null
                };
                
                if (targetSlot != null)
                {
                    // 재료 슬롯 설정
                    var materialStack = new MaterialStack
                    {
                        materialType = materialType,
                        count = requiredAmount
                    };
                    
                    if (showDebugLogs)
                        Debug.Log($"🔧 [EnhancementUI] {targetSlot.name}.SetupMaterial() 호출: {materialType.GetDisplayName()}");
                    targetSlot.SetupMaterial(materialStack);
                    
                    // ⭐ 더 이상 코루틴 불필요: materialCostPanel 활성화 후 1프레임 대기했으므로 Image가 정상 작동
                    
                    // 부족 시 빨간색 표시 (TODO: InventorySlot에 부족 표시 기능 추가 필요)
                    bool isInsufficient = ownedAmount < requiredAmount;
                    if (isInsufficient && showDebugLogs)
                    {
                        Debug.LogWarning($"⚠️ [EnhancementUI] 재료 부족: {materialType.GetDisplayName()} (필요: {requiredAmount}, 보유: {ownedAmount})");
                    }
                }
                
                slotIndex++;
            }
            
            // 골드 표시
            int requiredGold = CalculateRequiredGold();
            if (goldCostText != null)
            {
                int ownedGold = GetPlayerGold();
                bool isGoldInsufficient = ownedGold < requiredGold;
                
                if (showDebugLogs)
                    Debug.Log($"💰 [EnhancementUI] UpdateMaterialSlotsImmediate() - 골드: {requiredGold}, 보유 골드: {ownedGold}, 부족: {isGoldInsufficient}");
                
                // 골드 부족 시 빨간색
                if (isGoldInsufficient)
                {
                    goldCostText.color = Color.red;
                    goldCostText.text = $"골드: {requiredGold:N0}G (부족: {requiredGold - ownedGold:N0}G)";
                }
                else
                {
                    goldCostText.color = Color.white;
                    goldCostText.text = $"골드: {requiredGold:N0}G";
                }
            }
        }
        
        /// <summary>
        /// 필요 재료 계산
        /// </summary>
        private Dictionary<MaterialType, int> CalculateRequiredMaterials()
        {
            var result = new Dictionary<MaterialType, int>();
            
            if (!isItemSelected) return result;
            
            var enhancementData = Resources.Load<EnhancementData>("Data/EnhancementData");
            if (enhancementData == null)
            {
                Debug.LogError("❌ [EnhancementUI] EnhancementData를 찾을 수 없습니다!");
                return result;
            }
            
            int targetLevel = selectedItemData.enhancementLevel + 1;
            
            // 필요 재료 타입
            MaterialType materialType = enhancementData.GetRequiredMaterialType(
                selectedEquipmentData.equipmentType,
                selectedEquipmentData.itemGrade
            );
            
            // 필요 재료 개수
            int materialAmount = enhancementData.GetRequiredMaterialAmount(
                selectedEquipmentData.itemGrade,
                targetLevel
            );
            
            result[materialType] = materialAmount;
            
            if (showDebugLogs)
                Debug.Log($"💎 [EnhancementUI] 필요 재료: {materialType.GetDisplayName()} x{materialAmount}");
            
            return result;
        }
        
        /// <summary>
        /// 필요 골드 계산
        /// </summary>
        private int CalculateRequiredGold()
        {
            if (!isItemSelected) return 0;
            
            var enhancementData = Resources.Load<EnhancementData>("Data/EnhancementData");
            if (enhancementData == null) return 0;
            
            int targetLevel = selectedItemData.enhancementLevel + 1;
            
            return enhancementData.GetRequiredGold(
                selectedEquipmentData.itemGrade,
                targetLevel
            );
        }
        
        /// <summary>
        /// 플레이어 골드 조회
        /// </summary>
        private int GetPlayerGold()
        {
            if (PlayerDataManager.Instance != null)
            {
                int gold = PlayerDataManager.Instance.CurrentGold;
                
                if (showDebugLogs)
                    Debug.Log($"🔍 [EnhancementUI] GetPlayerGold() - {gold}");
                
                return gold;
            }
            
            if (showDebugLogs)
                Debug.LogWarning("⚠️ [EnhancementUI] GetPlayerGold() - PlayerDataManager.Instance가 NULL!");
            
            return 0;
        }
        
        /// <summary>
        /// 재료 부족 여부 확인
        /// </summary>
        private bool CheckMaterialAvailability()
        {
            if (!isItemSelected) return false;
            
            var requiredMaterials = CalculateRequiredMaterials();
            
            foreach (var kvp in requiredMaterials)
            {
                int ownedAmount = AccountDataManager.Instance.GetMaterialCount(kvp.Key);
                if (ownedAmount < kvp.Value)
                {
                    return false; // 재료 부족
                }
            }
            
            // 골드 체크
            int requiredGold = CalculateRequiredGold();
            int ownedGold = GetPlayerGold();
            
            if (ownedGold < requiredGold)
            {
                return false; // 골드 부족
            }
            
            return true; // 모든 재료 충분
        }
        
        // ========================================
        // Phase C: 성공 확률 표시
        // ========================================
        
        /// <summary>
        /// 성공 확률 표시 업데이트
        /// </summary>
        private void UpdateSuccessRateDisplay()
        {
            if (!isItemSelected)
            {
                if (successRatePanel != null)
                    successRatePanel.SetActive(false);
                return;
            }
            
            if (successRatePanel != null)
                successRatePanel.SetActive(true);
            
            // 성공률 계산
            float successRate = EnhancementSystem.GetSuccessRate(selectedItemId);
            
            // 텍스트 업데이트
            if (successRateText != null)
            {
                successRateText.text = $"성공 확률: {successRate:F1}%";
                successRateText.color = GetSuccessRateColor(successRate);
            }
            
            // 게이지 바 업데이트
            if (successRateBar != null)
            {
                successRateBar.fillAmount = successRate / 100f;
                successRateBar.color = GetSuccessRateColor(successRate);
            }
            
            if (showDebugLogs)
                Debug.Log($"📊 [EnhancementUI] 성공 확률: {successRate:F1}%");
        }
        
        /// <summary>
        /// 성공률에 따른 색상 반환
        /// </summary>
        private Color GetSuccessRateColor(float successRate)
        {
            if (successRate >= 70f)
            {
                return new Color(0.2f, 1f, 0.2f); // 초록색 (높은 확률)
            }
            else if (successRate >= 40f)
            {
                return new Color(1f, 0.92f, 0.016f); // 노란색 (중간 확률)
            }
            else
            {
                return new Color(1f, 0.2f, 0.2f); // 빨간색 (낮은 확률)
            }
        }
        
        // ========================================
        // Phase D: 강화 실행
        // ========================================
        
        /// <summary>
        /// 실패 경고 패널 업데이트
        /// </summary>
        private void UpdateWarningPanel()
        {
            if (!isItemSelected)
            {
                if (warningPanel != null)
                    warningPanel.SetActive(false);
                return;
            }
            
            if (warningPanel != null)
                warningPanel.SetActive(true);
            
            int currentLevel = selectedItemData.enhancementLevel;
            
            var enhancementData = Resources.Load<EnhancementData>("Data/EnhancementData");
            if (enhancementData == null) return;
            
            var failureType = enhancementData.GetFailureType(currentLevel);
            
            string warningMessage = failureType switch
            {
                EnhancementFailureType.Maintain => "실패 시: 강화 수치 유지",
                EnhancementFailureType.Downgrade => "⚠️ 실패 시: 강화 수치 -1",
                EnhancementFailureType.Destroy => "🔥 실패 시: 아이템 파괴",
                _ => "실패 시: 알 수 없음"
            };
            
            if (warningText != null)
            {
                warningText.text = warningMessage;
                
                // 실패 타입에 따른 색상
                warningText.color = failureType switch
                {
                    EnhancementFailureType.Maintain => Color.white,
                    EnhancementFailureType.Downgrade => Color.yellow,
                    EnhancementFailureType.Destroy => Color.red,
                    _ => Color.white
                };
            }
            
            if (showDebugLogs)
                Debug.Log($"⚠️ [EnhancementUI] 실패 타입: {failureType} (레벨: +{currentLevel})");
        }
        
        /// <summary>
        /// 강화 버튼 상태 업데이트
        /// </summary>
        private void UpdateEnhanceButton()
        {
            if (!isItemSelected)
            {
                if (enhanceButton != null)
                {
                    enhanceButton.interactable = false;
                    
                    if (enhanceButtonText != null)
                        enhanceButtonText.text = "아이템선택";
                }
                return;
            }
            
            // 강화 가능 여부 체크
            bool canEnhance = EnhancementSystem.CanEnhance(selectedItemId, out string reason);
            
            // ⭐ 버튼 텍스트: 간단하게 상태만 표시
            if (enhanceButton != null)
            {
                enhanceButton.interactable = canEnhance;
                
                if (enhanceButtonText != null)
                {
                    enhanceButtonText.text = canEnhance ? "강화하기" : "강화 불가";
                }
            }
            
            // ⭐ 경고 텍스트: 버튼 외부에 상세 이유 표시
            if (enhanceWarningText != null)
            {
                if (canEnhance)
                {
                    // 강화 가능: 경고 텍스트 숨김
                    enhanceWarningText.gameObject.SetActive(false);
                    enhanceWarningText.text = "";
                }
                else
                {
                    // 강화 불가: 경고 텍스트 표시
                    enhanceWarningText.gameObject.SetActive(true);
                    enhanceWarningText.text = reason;
                    enhanceWarningText.color = new Color(1f, 0.3f, 0.3f); // 빨간색
                }
            }
            
            if (showDebugLogs && !canEnhance)
                Debug.LogWarning($"⚠️ [EnhancementUI] 강화 불가: {reason}");
        }
        
        /// <summary>
        /// 강화 버튼 클릭 이벤트
        /// </summary>
        public void OnEnhanceButtonClicked()
        {
            if (!isItemSelected)
            {
                Debug.LogWarning("⚠️ [EnhancementUI] 아이템이 선택되지 않았습니다!");
                return;
            }
            
            if (showDebugLogs)
                Debug.Log($"🔨 [EnhancementUI] 강화 버튼 클릭: {selectedEquipmentData.equipmentName} +{selectedItemData.enhancementLevel}");
            
            // 귀속 경고 체크 (Phase 4에서 구현 예정)
            bool isBound = AccountDataManager.Instance.IsBound(selectedItemId);
            if (!isBound)
            {
                // 귀속되지 않은 아이템 → 귀속 경고 표시 (BindWarningPopup)
                // TODO: Phase 4에서 구현
                if (showDebugLogs)
                    Debug.Log("⚠️ [EnhancementUI] 귀속 경고 팝업 표시 (미구현)");
                
                // 임시로 바로 강화 진행
                ExecuteEnhancement();
            }
            else
            {
                // 이미 귀속된 아이템 → 바로 강화 진행
                ExecuteEnhancement();
            }
        }
        
        /// <summary>
        /// 강화 실행 (실제 강화 처리)
        /// </summary>
        private void ExecuteEnhancement()
        {
            if (!isItemSelected)
            {
                Debug.LogError("❌ [EnhancementUI] 아이템이 선택되지 않았습니다!");
                return;
            }
            
            if (showDebugLogs)
                Debug.Log($"🔨 [EnhancementUI] 강화 실행 시작: {selectedEquipmentData.equipmentName} +{selectedItemData.enhancementLevel}");
            
            // EnhancementSystem.ExecuteEnhancement() 호출
            var result = EnhancementSystem.ExecuteEnhancement(selectedItemId);
            
            if (result.success)
            {
                Debug.Log($"✨ [EnhancementUI] 강화 성공! +{selectedItemData.enhancementLevel - 1} → +{selectedItemData.enhancementLevel}");
            }
            else
            {
                if (result.wasDestroyed)
                {
                    Debug.Log($"💥 [EnhancementUI] 강화 실패 (파괴): {selectedEquipmentData.equipmentName}");
                }
                else
                {
                    Debug.Log($"⚠️ [EnhancementUI] 강화 실패: {result.errorMessage}");
                }
            }
            
            // 결과 팝업 표시 (Step 2-3에서 구현 예정)
            // TODO: EnhancementResultPopup.Show(result);
            
            // 이벤트 발생
            OnEnhancementComplete?.Invoke(result);
            
            // 인벤토리 갱신
            if (workshopInventoryUI != null)
            {
                workshopInventoryUI.RefreshInventoryDisplay();
            }
            
            // 아이템이 파괴되었으면 선택 해제
            if (result.wasDestroyed)
            {
                ClearSelection();
            }
            else
            {
                // UI 갱신 (강화 레벨 변경 반영)
                RefreshUI();
            }
        }
        
        // ========================================
        // 헬퍼 메서드
        // ========================================
        
        /// <summary>
        /// 🆕 골드 변경 이벤트 핸들러
        /// </summary>
        private void OnGoldChanged(int newGold)
        {
            UpdatePlayerGoldDisplay();
            
            // 아이템이 선택된 경우 재료/골드 부족 표시 갱신
            if (isItemSelected)
            {
                UpdateMaterialSlots();
                UpdateEnhanceButton();
            }
            
            if (showDebugLogs)
                Debug.Log($"💰 [EnhancementUI] 골드 변경: {newGold}G");
        }
        
        /// <summary>
        /// 🆕 재료 변경 이벤트 핸들러
        /// </summary>
        private void OnMaterialChanged(MaterialType materialType, int newCount)
        {
            // 아이템이 선택된 경우 재료/골드 부족 표시 갱신
            if (isItemSelected)
            {
                UpdateMaterialSlots();
                UpdateEnhanceButton();
            }
            
            if (showDebugLogs)
                Debug.Log($"📦 [EnhancementUI] 재료 변경: {materialType.GetDisplayName()} x{newCount}");
        }
        
        /// <summary>
        /// 🆕 플레이어 보유 골드 표시 업데이트 (상점과 동일한 방식)
        /// </summary>
        private void UpdatePlayerGoldDisplay()
        {
            if (playerGoldText != null)
            {
                int currentGold = GetPlayerGold();
                playerGoldText.text = currentGold.ToString(); // ⭐ 상점과 동일: 숫자만 표시
                
                if (showDebugLogs)
                    Debug.Log($"💰 [EnhancementUI] 플레이어 골드 표시 업데이트: {currentGold}");
            }
            else
            {
                Debug.LogWarning("⚠️ [EnhancementUI] playerGoldText가 NULL입니다!");
            }
        }
        
        /// <summary>
        /// EquipmentData 로드 (ItemInstanceData.templateName → EquipmentData)
        /// ⭐ ItemTemplateResolver 사용 (LobbyInventoryUI와 동일)
        /// </summary>
        private EquipmentData LoadEquipmentData(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                Debug.LogWarning("⚠️ [EnhancementUI] templateName이 비어있습니다!");
                return null;
            }
            
            // ⭐ ItemTemplateResolver 사용
            var equipData = ItemTemplateResolver.Load(templateName);
            
            if (equipData == null && showDebugLogs)
            {
                Debug.LogWarning($"⚠️ [EnhancementUI] EquipmentData를 찾을 수 없습니다: {templateName}");
            }
            
            return equipData;
        }
        
    }
}

