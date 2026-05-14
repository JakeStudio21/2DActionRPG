using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Managers;
using Systems;
using UI.Popups;
using CueSystem;

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
        
        [Header("📌 선택된 아이템 정보")]
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private Image itemIconImage; // ⚠️ BeforeAfterComparisonUI와 중복, 숨김 처리됨
        [SerializeField] private TMP_Text enhancementLevelText;
        [SerializeField] private TMP_Text statBonusText; // ⭐ 이번 강화 스탯 증가량 (예: "공격력 +1.5%")
        [SerializeField] private TMP_Text totalStatBonusText; // ⭐ 누적 스탯 표시 (예: "현재 누적: +7.5% → +9.0%")
        
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
        [SerializeField] private EnhancementMessageUI enhancementMessageUI;
        
        // ========== 상태 ==========
        private ItemInstanceID selectedItemId;
        private ItemInstanceData selectedItemData;
        private EquipmentData selectedEquipmentData;
        private bool isItemSelected => !selectedItemId.IsEmpty;
        
        // ========== 이벤트 ==========
        public event Action<ItemInstanceID> OnItemSelected;
        public event Action<EnhancementResult> OnEnhancementComplete;
        
        // ========================================
        // 초기화
        // ========================================
        
        void OnEnable()
        {
            
            // 🆕 골드 변경 이벤트 구독
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnGoldChanged -= OnGoldChanged;
                PlayerDataManager.Instance.OnGoldChanged += OnGoldChanged;
                
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
                
            }
            else
            {
                Debug.LogError("❌ [EnhancementUI] AccountDataManager.Instance가 NULL입니다!");
            }
            
            // 초기 골드 표시
            int currentGold = GetPlayerGold();
            
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
            
        }
        
        /// <summary>
        /// ⭐ 강화 탭 활성화 시 호출 (탭 전환 시 상태 초기화)
        /// </summary>
        public void Initialize()
        {
            
            // 선택 상태 초기화
            selectedItemId = default;
            selectedItemData = null;
            selectedEquipmentData = null;
            
            // UI 갱신
            RefreshUI();
            
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
        public void OnSelectedItemChanged(ItemInstanceID itemId)
        {
            selectedItemId = itemId;
            
            if (itemId.IsEmpty)
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
            
                Dbg.Log($"🎯 [EnhancementUI] 아이템 선택: {selectedEquipmentData.equipmentName} +{selectedItemData.enhancementLevel} (ID: {itemId})");
            
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
            
            // ⭐ 스탯 증가량 표시
            if (statBonusText != null)
            {
                UpdateStatBonusDisplay();
            }
        }
        
        /// <summary>
        /// ⭐ 스탯 증가량 계산 및 표시 (이번 강화 + 누적) - 새 SO 기반
        /// </summary>
        private void UpdateStatBonusDisplay()
        {
            if (!isItemSelected) return;
            
            // ⭐ 새 SO 로드
            var curveTable = Resources.Load<EnhanceCurveTableSO>("Data/EnhanceCurveTable");
            if (curveTable == null)
            {
                if (statBonusText != null) statBonusText.text = "";
                if (totalStatBonusText != null) totalStatBonusText.text = "";
                return;
            }
            
            int currentLevel = selectedItemData.enhancementLevel;
            int targetLevel = currentLevel + 1;
            
            // ⭐ 장비의 곡선 그룹 ID
            string curveGroupId = selectedEquipmentData.enhancementCurveGroupId;
            if (string.IsNullOrEmpty(curveGroupId))
            {
                curveGroupId = "CURVE_STANDARD"; // 기본값
            }
            
            // 장비 타입에 따른 스탯 이름
            string statName = GetStatNameByEquipmentType(selectedEquipmentData.equipmentType);
            
            // ⭐ 이번 강화 시 증가하는 스탯 (+1.5% 같은)
            float thisLevelStatRate = curveTable.GetStatRateAdd(curveGroupId, targetLevel);
            
            if (statBonusText != null)
            {
                if (thisLevelStatRate > 0)
                {
                    statBonusText.text = $"<color=#4CAF50>{statName} +{thisLevelStatRate:F1}%</color>";
                }
                else
                {
                    statBonusText.text = "";
                }
            }
            
            // ⭐ 누적 스탯 계산 및 표시
            if (totalStatBonusText != null)
            {
                float currentTotalBonus = curveTable.GetTotalStatBonus(curveGroupId, currentLevel);
                float nextTotalBonus = curveTable.GetTotalStatBonus(curveGroupId, targetLevel);
                
                if (currentLevel > 0 || nextTotalBonus > 0)
                {
                    totalStatBonusText.text = $"<color=#FFC107>현재 누적: +{currentTotalBonus:F1}%</color> → <color=#4CAF50>+{nextTotalBonus:F1}%</color>";
                }
                else
                {
                    totalStatBonusText.text = "";
                }
                
            }
        }
        
        /// <summary>
        /// 장비 타입별 주요 스탯 이름 반환
        /// </summary>
        private string GetStatNameByEquipmentType(EquipmentType equipType)
        {
            return equipType switch
            {
                EquipmentType.Weapon => "공격력",
                EquipmentType.Armor => "방어력",
                EquipmentType.Accessory => "체력",
                _ => "스탯"
            };
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
            
            UpdateMaterialSlotsImmediate();
        }
        
        /// <summary>
        /// Material Slot 즉시 업데이트 (코루틴에서 호출)
        /// </summary>
        private void UpdateMaterialSlotsImmediate()
        {
            // 모든 슬롯 Clear (이전 데이터 제거)
            if (materialSlot1 != null)
                materialSlot1.ClearSlot();
            if (materialSlot2 != null)
                materialSlot2.ClearSlot();
            if (materialSlot3 != null)
                materialSlot3.ClearSlot();
            
            // 필요 재료 계산
            var requiredMaterials = CalculateRequiredMaterials();
            
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
                    
                    targetSlot.SetupMaterial(materialStack);
                    
                    // ⭐ 더 이상 코루틴 불필요: materialCostPanel 활성화 후 1프레임 대기했으므로 Image가 정상 작동
                    
                    // 부족 시 빨간색 표시 (TODO: InventorySlot에 부족 표시 기능 추가 필요)
                    bool isInsufficient = ownedAmount < requiredAmount;
                }
                
                slotIndex++;
            }
            
            // 골드 표시
            int requiredGold = CalculateRequiredGold();
            if (goldCostText != null)
            {
                int ownedGold = GetPlayerGold();
                bool isGoldInsufficient = ownedGold < requiredGold;
                
                
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
        /// 필요 재료 계산 (⭐ 새 SO 기반)
        /// </summary>
        private Dictionary<MaterialType, int> CalculateRequiredMaterials()
        {
            var result = new Dictionary<MaterialType, int>();
            
            if (!isItemSelected) return result;
            
            // ⭐ 새 SO 로드
            var levelTable = Resources.Load<EnhanceLevelTableSO>("Data/EnhanceLevelTable");
            if (levelTable == null)
            {
                Debug.LogError("❌ [EnhancementUI] EnhanceLevelTableSO를 찾을 수 없습니다!");
                return result;
            }
            
            int targetLevel = selectedItemData.enhancementLevel + 1;
            
            // 필요 재료 타입 (등급별 매핑)
            MaterialType materialType = GetRequiredMaterialType(
                selectedEquipmentData.equipmentType,
                selectedEquipmentData.itemGrade
            );
            
            // ⭐ 새 SO: 필요 재료 개수
            int materialAmount = levelTable.GetMaterialCount(targetLevel);
            
            result[materialType] = materialAmount;
            
            
            return result;
        }
        
        /// <summary>
        /// 필요 골드 계산 (⭐ 새 SO 기반)
        /// </summary>
        private int CalculateRequiredGold()
        {
            if (!isItemSelected) return 0;
            
            // ⭐ 새 SO 로드
            var levelTable = Resources.Load<EnhanceLevelTableSO>("Data/EnhanceLevelTable");
            if (levelTable == null) return 0;
            
            int targetLevel = selectedItemData.enhancementLevel + 1;
            
            // ⭐ 새 SO: 골드 비용
            return levelTable.GetGoldCost(targetLevel);
        }
        
        /// <summary>
        /// 장비 타입과 등급에 따른 필요 재료 타입 반환 (헬퍼 메서드)
        /// </summary>
        private MaterialType GetRequiredMaterialType(EquipmentType equipType, ItemGrade grade)
        {
            // ⭐ 장비 타입 × 등급에 따른 재료 매핑
            // 무기: WeaponFragment/Crystal/Core
            // 방어구: ArmorFragment/Crystal/Core
            // 악세사리: AccessoryFragment/Crystal/Core
            
            if (equipType == EquipmentType.Weapon)
            {
                return grade switch
                {
                    ItemGrade.D or ItemGrade.C or ItemGrade.B => MaterialType.WeaponFragment,
                    ItemGrade.A or ItemGrade.S or ItemGrade.SS => MaterialType.WeaponCrystal,
                    ItemGrade.EX or ItemGrade.TR => MaterialType.WeaponCore,
                    _ => MaterialType.WeaponFragment
                };
            }
            else if (equipType == EquipmentType.Armor)
            {
                return grade switch
                {
                    ItemGrade.D or ItemGrade.C or ItemGrade.B => MaterialType.ArmorFragment,
                    ItemGrade.A or ItemGrade.S or ItemGrade.SS => MaterialType.ArmorCrystal,
                    ItemGrade.EX or ItemGrade.TR => MaterialType.ArmorCore,
                    _ => MaterialType.ArmorFragment
                };
            }
            else // Accessory
            {
                return grade switch
                {
                    ItemGrade.D or ItemGrade.C or ItemGrade.B => MaterialType.AccessoryFragment,
                    ItemGrade.A or ItemGrade.S or ItemGrade.SS => MaterialType.AccessoryCrystal,
                    ItemGrade.EX or ItemGrade.TR => MaterialType.AccessoryCore,
                    _ => MaterialType.AccessoryFragment
                };
            }
        }
        
        /// <summary>
        /// 플레이어 골드 조회
        /// </summary>
        private int GetPlayerGold()
        {
            if (PlayerDataManager.Instance != null)
            {
                int gold = PlayerDataManager.Instance.CurrentGold;
                
                
                return gold;
            }
            
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
            int targetLevel = currentLevel + 1;
            
            // ⭐ 새 SO: 실패 처리 규칙
            var levelTable = Resources.Load<EnhanceLevelTableSO>("Data/EnhanceLevelTable");
            if (levelTable == null) return;
            
            var failureType = levelTable.GetFailureType(targetLevel);
            
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
            
            
            // 귀속 경고 체크 (Phase 4에서 구현 예정)
            bool isBound = AccountDataManager.Instance.IsBound(selectedItemId);
            if (!isBound)
            {
                // 귀속되지 않은 아이템 → 귀속 경고 표시 (BindWarningPopup)
                // TODO: Phase 4에서 구현
                
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
            
            
            // 강화 버튼 비활성화 (메세지 연출 중 중복 클릭 방지)
            if (enhanceButton != null)
                enhanceButton.interactable = false;

            // EnhancementSystem.ExecuteEnhancement() 호출
            var result = EnhancementSystem.ExecuteEnhancement(selectedItemId);
            
            // 강화 결과 메세지 연출
            ShowEnhancementMessage(result);
            
            // 이벤트 발생
            OnEnhancementComplete?.Invoke(result);
            
            // 인벤토리 갱신 및 UI 업데이트
            if (workshopInventoryUI != null)
                workshopInventoryUI.RefreshInventoryDisplay();

            if (result.wasDestroyed)
                ClearSelection();
            else
                RefreshUI();
        }

        /// <summary>
        /// 강화 결과에 따라 메세지 연출 재생 및 버튼 재활성화 예약
        /// </summary>
        private void ShowEnhancementMessage(EnhancementResult result)
        {
            if (enhancementMessageUI == null) return;

            var cueContext = new CueContext { position = Vector3.zero, actorType = ActorType.UI };

            if (result.success)
            {
                CueEmitter.Emit("ui.workshop.enhance.success", "UI", cueContext);
                enhancementMessageUI.ShowSuccess(result.previousLevel, result.newLevel);
            }
            else if (result.wasDestroyed)
            {
                CueEmitter.Emit("ui.workshop.enhance.fail", "UI", cueContext);
                enhancementMessageUI.ShowDestroy(result.previousLevel);
            }
            else
            {
                CueEmitter.Emit("ui.workshop.enhance.fail", "UI", cueContext);
                switch (result.failureType)
                {
                    case EnhancementFailureType.Downgrade:
                        enhancementMessageUI.ShowFailDowngrade(result.previousLevel, result.newLevel);
                        break;
                    default:
                        enhancementMessageUI.ShowFailMaintain(result.previousLevel);
                        break;
                }
            }

            // 파괴가 아닌 경우 메세지 연출 후 버튼 재활성화
            if (!result.wasDestroyed)
                StartCoroutine(ReEnableEnhanceButtonAfterDelay());
        }

        /// <summary>
        /// 메세지 연출 시간(displayDuration + fadeOut)이 지난 후 강화 버튼 재활성화
        /// </summary>
        private System.Collections.IEnumerator ReEnableEnhanceButtonAfterDelay()
        {
            // EnhancementMessageUI의 기본 표시 시간(2.0s) + 퇴장(0.4s) + 여유 0.1s
            yield return new WaitForSecondsRealtime(2.5f);

            if (enhanceButton != null)
                UpdateEnhanceButton();
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
            
            
            return equipData;
        }
        
    }
}

