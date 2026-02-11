using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Managers;
using Systems;
using UI.Popups;

namespace UI.Workshop
{
    /// <summary>
    /// 분해 UI Controller (View + Controller)
    /// ⭐ WorkshopInventoryUI 공통 LeftSection 사용
    /// - B안: 대표 슬롯 + 개수 표시
    /// - 보상 미리보기 (자동 크기 조절: 3개=150px, 6개=75px, 9개=60px)
    /// - ConfirmationPopup → DismantleSystem → ResultFeedbackPopup 플로우
    /// </summary>
    public class DismantleUI : MonoBehaviour
    {
        [Header("=== 연동 컴포넌트 ===")]
        [SerializeField] private WorkshopInventoryUI workshopInventoryUI; // ⭐ 공통 LeftSection

        [Header("=== Right Section (선택 정보) ===")]
        [SerializeField] private GameObject selectedItemSummary;
        [SerializeField] private InventorySlot representativeSlot;
        [SerializeField] private TextMeshProUGUI countText;
        
        [Header("보상 미리보기 (자동 크기)")]
        [SerializeField] private GameObject rewardPreview;
        [SerializeField] private Transform rewardSlotsContainer; // HorizontalLayoutGroup
        [SerializeField] private GameObject materialSlotPrefab;
        
        [Header("실행 버튼")]
        [SerializeField] private Button dismantleButton;
        [SerializeField] private TextMeshProUGUI dismantleButtonText;
        [SerializeField] private Button cancelButton;

        [Header("=== 팝업 참조 ===")]
        [SerializeField] private ConfirmationPopup confirmationPopup;
        [SerializeField] private ResultFeedbackPopup resultFeedbackPopup;

        [Header("=== 디버그 ===")]
        [SerializeField] private bool showDebugLogs = true;

        // 상태 관리
        private List<ItemInstanceId> selectedItemIds = new List<ItemInstanceId>();

        private void Start()
        {
            // 버튼 이벤트 연결
            if (dismantleButton != null)
                dismantleButton.onClick.AddListener(OnDismantleButtonClicked);
            
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }

        /// <summary>
        /// 분해 UI 초기화 (WorkshopUI에서 호출)
        /// </summary>
        public void Initialize()
        {
            if (showDebugLogs)
                Debug.Log("📦 [DismantleUI] 초기화 시작");
            
            // ⭐ WorkshopInventoryUI 이벤트 구독
            if (workshopInventoryUI != null)
            {
                workshopInventoryUI.OnSelectionChanged -= OnInventorySelectionChanged;
                workshopInventoryUI.OnSelectionChanged += OnInventorySelectionChanged;
                
                if (showDebugLogs)
                    Debug.Log("✅ [DismantleUI] WorkshopInventoryUI 이벤트 구독 완료");
            }
            else
            {
                Debug.LogError("❌ [DismantleUI] workshopInventoryUI가 null입니다! Inspector에서 연결하세요.");
            }
            
            // 초기 UI 상태
            selectedItemIds.Clear();
            UpdateUI();
            
            if (showDebugLogs)
                Debug.Log("✅ [DismantleUI] 초기화 완료");
        }

        private void OnDisable()
        {
            // 이벤트 구독 해제
            if (workshopInventoryUI != null)
            {
                workshopInventoryUI.OnSelectionChanged -= OnInventorySelectionChanged;
            }
        }

        /// <summary>
        /// ⭐ WorkshopInventoryUI 선택 변경 이벤트 핸들러
        /// </summary>
        private void OnInventorySelectionChanged(List<ItemInstanceId> selectedIds)
        {
            if (showDebugLogs)
                Debug.Log($"🔔 [DismantleUI] 선택 변경 이벤트: {selectedIds.Count}개");
            
            selectedItemIds = new List<ItemInstanceId>(selectedIds);
            UpdateUI();
        }

        /// <summary>
        /// 취소 버튼 클릭 (선택 초기화)
        /// </summary>
        private void OnCancelButtonClicked()
        {
            Debug.Log("🚫 [DismantleUI] 취소 버튼 클릭 - 선택 초기화 시작");
            
            if (workshopInventoryUI != null)
            {
                workshopInventoryUI.ClearSelection();
                Debug.Log("✅ [DismantleUI] WorkshopInventoryUI.ClearSelection() 호출 완료");
            }
            else
            {
                Debug.LogError("❌ [DismantleUI] workshopInventoryUI가 null입니다!");
            }
        }

        /// <summary>
        /// UI 전체 업데이트
        /// </summary>
        private void UpdateUI()
        {
            // B안: 선택 요약 업데이트
            UpdateSelectedItemSummary();
            
            // 보상 미리보기 업데이트 (자동 크기 조절)
            UpdateRewardPreview();
            
            // 분해 버튼 상태
            UpdateDismantleButton();
        }

        /// <summary>
        /// B안: 선택된 아이템 요약 표시 (대표 슬롯 + 개수)
        /// </summary>
        private void UpdateSelectedItemSummary()
        {
            if (selectedItemIds.Count == 0)
            {
                if (selectedItemSummary != null)
                    selectedItemSummary.SetActive(false);
                return;
            }

            if (selectedItemSummary != null)
                selectedItemSummary.SetActive(true);

            // 1. 대표 아이템 선택 (우선순위: 등급 > 강화 > 타입)
            ItemInstanceId representativeItemId = SelectRepresentativeItem(selectedItemIds);
            var account = AccountDataManager.Instance;
            ItemInstanceData instanceData = account.GetInstance(representativeItemId);
            
            if (instanceData == null)
            {
                Debug.LogError($"❌ [DismantleUI] 대표 아이템 데이터 없음: {representativeItemId}");
                if (selectedItemSummary != null)
                    selectedItemSummary.SetActive(false);
                return;
            }

            EquipmentData equipData = ItemTemplateResolver.Load(instanceData.templateName);
            if (equipData == null)
            {
                Debug.LogError($"❌ [DismantleUI] 대표 아이템 템플릿 로드 실패: {instanceData.templateName}");
                if (selectedItemSummary != null)
                    selectedItemSummary.SetActive(false);
                return;
            }

            // 2. 대표 슬롯 설정
            if (representativeSlot != null)
            {
                representativeSlot.SetEquipmentData(equipData, representativeItemId);
            }

            // 3. 개수 텍스트
            if (countText != null)
            {
                if (selectedItemIds.Count == 1)
                {
                    countText.text = "1개 선택됨";
                }
                else
                {
                    countText.text = $"{selectedItemIds.Count}개 선택됨";
                }
            }

            if (showDebugLogs)
                Debug.Log($"📊 [DismantleUI] 선택 요약 - 대표: {equipData.equipmentName}, 총 {selectedItemIds.Count}개");
        }

        /// <summary>
        /// 대표 아이템 선택 (우선순위: 등급 > 강화 > 타입)
        /// </summary>
        private ItemInstanceId SelectRepresentativeItem(List<ItemInstanceId> itemIds)
        {
            if (itemIds == null || itemIds.Count == 0)
                return default;

            if (itemIds.Count == 1)
                return itemIds[0];

            var account = AccountDataManager.Instance;
            ItemInstanceId bestItem = itemIds[0];
            var bestInstance = account.GetInstance(bestItem);
            var bestEquipData = bestInstance != null ? ItemTemplateResolver.Load(bestInstance.templateName) : null;

            if (bestEquipData == null)
                return itemIds[0];

            foreach (var itemId in itemIds)
            {
                var instance = account.GetInstance(itemId);
                if (instance == null) continue;

                var equipData = ItemTemplateResolver.Load(instance.templateName);
                if (equipData == null) continue;

                // 우선순위 1: 등급이 높은 것
                if (equipData.itemGrade > bestEquipData.itemGrade)
                {
                    bestItem = itemId;
                    bestInstance = instance;
                    bestEquipData = equipData;
                    continue;
                }
                else if (equipData.itemGrade < bestEquipData.itemGrade)
                {
                    continue;
                }

                // 우선순위 2: 같은 등급이면 강화가 높은 것
                int currentEnhancement = instance.enhancementLevel;
                int bestEnhancement = bestInstance.enhancementLevel;

                if (currentEnhancement > bestEnhancement)
                {
                    bestItem = itemId;
                    bestInstance = instance;
                    bestEquipData = equipData;
                    continue;
                }
                else if (currentEnhancement < bestEnhancement)
                {
                    continue;
                }

                // 우선순위 3: 같은 강화면 무기 > Armor > 악세사리
                int currentTypePriority = GetEquipmentTypePriority(equipData.equipmentType);
                int bestTypePriority = GetEquipmentTypePriority(bestEquipData.equipmentType);

                if (currentTypePriority < bestTypePriority) // 낮을수록 우선순위 높음
                {
                    bestItem = itemId;
                    bestInstance = instance;
                    bestEquipData = equipData;
                }
            }

            return bestItem;
        }

        /// <summary>
        /// 장비 타입 우선순위 (낮을수록 우선순위 높음)
        /// </summary>
        private int GetEquipmentTypePriority(EquipmentType type)
        {
            switch (type)
            {
                case EquipmentType.Weapon:
                    return 1; // 최우선
                case EquipmentType.Armor:
                    return 2;
                case EquipmentType.Accessory:
                    return 3;
                default:
                    return 999;
            }
        }

        /// <summary>
        /// 보상 미리보기 업데이트 (자동 크기 조절)
        /// </summary>
        private void UpdateRewardPreview()
        {
            if (selectedItemIds.Count == 0)
            {
                if (rewardPreview != null)
                    rewardPreview.SetActive(false);
                return;
            }

            // 1. 선택된 아이템들로 보상 계산 (재료만, 골드 제외)
            Dictionary<MaterialType, int> rewards = CalculateTotalDismantleRewards(selectedItemIds);
            
            if (showDebugLogs)
            {
                Debug.Log($"💎 [DismantleUI] 보상 계산 완료 - 재료 {rewards.Count}종류");
                foreach (var reward in rewards)
                {
                    Debug.Log($"   ├─ {reward.Key.GetDisplayName()}: {reward.Value}개");
                }
            }

            // 2. 슬롯 크기 계산
            int slotSize = CalculateSlotSize(rewards.Count);
            
            if (showDebugLogs)
                Debug.Log($"📏 [DismantleUI] 슬롯 크기: {slotSize}px (재료 {rewards.Count}종류)");

            // 3. 기존 슬롯 제거
            ClearRewardSlots();

            // 4. ⭐ Phase 0 교훈: Panel 활성화 후 1프레임 대기
            if (rewardPreview != null && !rewardPreview.activeSelf)
            {
                if (showDebugLogs)
                    Debug.Log("🔍 [DismantleUI] rewardPreview 활성화 → 1프레임 대기 후 슬롯 설정");
                
                rewardPreview.SetActive(true);
                StartCoroutine(UpdateRewardPreviewDelayed(rewards, slotSize));
                return;
            }

            // 5. 즉시 슬롯 생성
            UpdateRewardPreviewImmediate(rewards, slotSize);
        }

        /// <summary>
        /// 다중 아이템 분해 보상 계산 (합산)
        /// </summary>
        private Dictionary<MaterialType, int> CalculateTotalDismantleRewards(List<ItemInstanceId> itemIds)
        {
            var totalRewards = new Dictionary<MaterialType, int>();
            
            if (showDebugLogs)
                Debug.Log($"💰 [DismantleUI] 보상 계산 시작 - 아이템 {itemIds.Count}개");

            int processedCount = 0;
            foreach (var itemId in itemIds)
            {
                var itemRewards = DismantleSystem.CalculateDismantleReward(itemId);
                processedCount++;
                
                if (showDebugLogs)
                    Debug.Log($"   [{processedCount}] {itemId} → 재료 {itemRewards.Count}종류");
                
                foreach (var reward in itemRewards)
                {
                    // ⭐ count가 0 이하인 재료는 무시
                    if (reward.Value <= 0)
                        continue;
                    
                    if (totalRewards.ContainsKey(reward.Key))
                    {
                        totalRewards[reward.Key] += reward.Value;
                    }
                    else
                    {
                        totalRewards[reward.Key] = reward.Value;
                    }
                }
            }
            
            if (showDebugLogs)
                Debug.Log($"✅ [DismantleUI] 보상 계산 완료 - 처리된 아이템: {processedCount}개");

            // ⭐ 최종적으로 count가 0 이하인 재료 제거 (안전장치)
            var filteredRewards = new Dictionary<MaterialType, int>();
            foreach (var reward in totalRewards)
            {
                if (reward.Value > 0)
                {
                    filteredRewards[reward.Key] = reward.Value;
                }
            }

            return filteredRewards;
        }

        /// <summary>
        /// 보상 미리보기 지연 업데이트 (1프레임 대기)
        /// </summary>
        private IEnumerator UpdateRewardPreviewDelayed(Dictionary<MaterialType, int> rewards, int slotSize)
        {
            yield return null; // 1프레임 대기 (Layout Group 초기화)
            
            if (showDebugLogs)
                Debug.Log("⏰ [DismantleUI] 1프레임 대기 완료 → 보상 슬롯 생성 시작");
            
            UpdateRewardPreviewImmediate(rewards, slotSize);
        }

        /// <summary>
        /// 보상 미리보기 즉시 업데이트
        /// </summary>
        private void UpdateRewardPreviewImmediate(Dictionary<MaterialType, int> rewards, int slotSize)
        {
            // ⭐ GridLayoutGroup의 Cell Size 동적 조정
            if (rewardSlotsContainer != null)
            {
                var gridLayout = rewardSlotsContainer.GetComponent<UnityEngine.UI.GridLayoutGroup>();
                if (gridLayout != null)
                {
                    gridLayout.cellSize = new Vector2(slotSize, slotSize);
                    
                    if (showDebugLogs)
                        Debug.Log($"📐 [DismantleUI] GridLayoutGroup Cell Size 설정: {slotSize}x{slotSize}");
                }
            }
            
            foreach (var reward in rewards)
            {
                CreateMaterialSlot(reward.Key, reward.Value, slotSize);
            }
            
            if (showDebugLogs)
                Debug.Log($"✅ [DismantleUI] 보상 슬롯 {rewards.Count}개 생성 완료");
        }

        /// <summary>
        /// 재료 슬롯 생성 (⭐ Phase 0 교훈: Instantiate 후 1프레임 대기)
        /// </summary>
        private void CreateMaterialSlot(MaterialType materialType, int count, int slotSize)
        {
            if (materialSlotPrefab == null || rewardSlotsContainer == null)
            {
                Debug.LogError("❌ [DismantleUI] materialSlotPrefab 또는 rewardSlotsContainer가 null!");
                return;
            }

            GameObject slotObj = Instantiate(materialSlotPrefab, rewardSlotsContainer);
            
            // ⚠️ [참고] GridLayoutGroup 사용 시 개별 sizeDelta는 무시됨
            // → UpdateRewardPreviewImmediate()에서 gridLayout.cellSize로 일괄 설정
            // → HorizontalLayoutGroup 사용 시에는 아래 코드가 동작
            RectTransform rt = slotObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(slotSize, slotSize);
            }

            // ⭐ Phase 0 교훈: Instantiate 후 1프레임 대기
            StartCoroutine(SetupMaterialSlotDelayed(slotObj, materialType, count));
        }

        /// <summary>
        /// 재료 슬롯 설정 지연 (1프레임 대기)
        /// </summary>
        private IEnumerator SetupMaterialSlotDelayed(GameObject slotObj, MaterialType materialType, int count)
        {
            yield return null; // 1프레임 대기 (Layout Group에 추가 완료)
            
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            if (slot == null)
            {
                Debug.LogError("❌ [DismantleUI] MaterialSlot에 InventorySlot 컴포넌트 없음");
                yield break;
            }

            // 재료 설정
            MaterialStack materialStack = new MaterialStack
            {
                materialType = materialType,
                count = count
            };

            slot.SetupMaterial(materialStack);
            
            if (showDebugLogs)
                Debug.Log($"📦 [DismantleUI] 재료 슬롯 설정 완료: {materialType.GetDisplayName()} x{count}");
        }

        /// <summary>
        /// 슬롯 크기 계산 (자동 크기 조절)
        /// </summary>
        private int CalculateSlotSize(int materialCount)
        {
            if (materialCount <= 3) return 120; // 1~3개: 큰 슬롯
            return 90;                          // 4~9개: 작은 슬롯
        }

        /// <summary>
        /// 기존 보상 슬롯 제거
        /// </summary>
        private void ClearRewardSlots()
        {
            if (rewardSlotsContainer == null) return;

            // ⭐ Destroy()는 프레임 끝에 실행되므로 DestroyImmediate() 사용
            // → 새 슬롯 생성 전에 확실히 정리
            int childCount = rewardSlotsContainer.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = rewardSlotsContainer.GetChild(i);
                DestroyImmediate(child.gameObject);
            }
            
            if (showDebugLogs)
                Debug.Log($"🧹 [DismantleUI] 기존 슬롯 {childCount}개 즉시 제거 완료");
        }

        /// <summary>
        /// 분해 버튼 상태 업데이트
        /// </summary>
        private void UpdateDismantleButton()
        {
            bool canDismantle = selectedItemIds.Count > 0;
            
            if (dismantleButton != null)
                dismantleButton.interactable = canDismantle;

            if (dismantleButtonText != null)
            {
                if (canDismantle)
                {
                    dismantleButtonText.text = $"분해 ({selectedItemIds.Count}개)";
                }
                else
                {
                    dismantleButtonText.text = "아이템 선택";
                }
            }
        }

        /// <summary>
        /// 분해 버튼 클릭 처리
        /// </summary>
        private void OnDismantleButtonClicked()
        {
            if (selectedItemIds.Count == 0)
            {
                Debug.LogWarning("⚠️ [DismantleUI] 선택된 아이템 없음");
                return;
            }

            if (showDebugLogs)
                Debug.Log($"🔨 [DismantleUI] 분해 버튼 클릭 - {selectedItemIds.Count}개 아이템");
            
            ShowConfirmation();
        }

        /// <summary>
        /// 확인 팝업 표시
        /// </summary>
        private void ShowConfirmation()
        {
            if (confirmationPopup == null)
            {
                Debug.LogError("❌ [DismantleUI] ConfirmationPopup 참조 없음! Inspector에서 연결하세요.");
                return;
            }

            // 보상 계산 (재료만)
            Dictionary<MaterialType, int> rewards = CalculateTotalDismantleRewards(selectedItemIds);
            
            // 보상 텍스트 생성
            string rewardText = string.Join("\n", rewards.Select(r => $"• {r.Key.GetDisplayName()}: {r.Value}개"));

            string title = "아이템 분해";
            string message = $"선택한 {selectedItemIds.Count}개의 아이템을 분해하시겠습니까?";
            string detail = $"획득 가능 재료:\n{rewardText}\n\n※ 분해된 아이템은 복구할 수 없습니다.";

            confirmationPopup.Show(
                title: title,
                message: message,
                detail: detail,
                onConfirm: ExecuteDismantle,
                onCancel: OnConfirmationCancelled // ⭐ 취소 시 선택 초기화
            );

            if (showDebugLogs)
                Debug.Log($"💬 [DismantleUI] 확인 팝업 표시: {selectedItemIds.Count}개 아이템");
        }

        /// <summary>
        /// 확인 팝업에서 취소 버튼 클릭 시 호출
        /// </summary>
        private void OnConfirmationCancelled()
        {
            Debug.Log("🚫 [DismantleUI] 확인 팝업 취소 - 선택 초기화");
            
            if (workshopInventoryUI != null)
            {
                workshopInventoryUI.ClearSelection();
            }
            else
            {
                Debug.LogError("❌ [DismantleUI] workshopInventoryUI가 null입니다!");
            }
        }

        /// <summary>
        /// 분해 실행 (다중 아이템)
        /// </summary>
        private void ExecuteDismantle()
        {
            if (showDebugLogs)
                Debug.Log($"⚙️ [DismantleUI] 분해 실행 시작 - {selectedItemIds.Count}개");

            // 다중 아이템 분해
            var totalRewards = new Dictionary<MaterialType, int>();
            int successCount = 0;
            int failCount = 0;

            foreach (var itemId in selectedItemIds.ToList()) // ToList()로 복사본 순회
            {
                var rewards = DismantleSystem.DismantleItem(itemId);
                
                if (rewards.Count > 0)
                {
                    successCount++;
                    
                    // 보상 합산
                    foreach (var reward in rewards)
                    {
                        if (totalRewards.ContainsKey(reward.Key))
                        {
                            totalRewards[reward.Key] += reward.Value;
                        }
                        else
                        {
                            totalRewards[reward.Key] = reward.Value;
                        }
                    }
                }
                else
                {
                    failCount++;
                }
            }

            if (showDebugLogs)
                Debug.Log($"✅ [DismantleUI] 분해 완료 - 성공: {successCount}, 실패: {failCount}, 재료 {totalRewards.Count}종류 획득");

            // 결과 팝업 표시
            if (resultFeedbackPopup != null && successCount > 0)
            {
                resultFeedbackPopup.ShowDismantleResult(successCount, totalRewards);
            }
            else if (resultFeedbackPopup == null)
            {
                Debug.LogError("❌ [DismantleUI] ResultFeedbackPopup 참조 없음! Inspector에서 연결하세요.");
            }

            // ⭐ 선택 초기화 및 인벤토리 UI 갱신
            if (workshopInventoryUI != null)
            {
                workshopInventoryUI.ClearSelection();
                workshopInventoryUI.RefreshInventoryDisplay(); // ⭐ 분해된 아이템 즉시 UI에서 제거
                
                if (showDebugLogs)
                    Debug.Log("🔄 [DismantleUI] 인벤토리 UI 갱신 완료");
            }
            else
            {
                Debug.LogError("❌ [DismantleUI] workshopInventoryUI가 null입니다!");
            }
            
            // UI 갱신
            selectedItemIds.Clear();
            UpdateUI();
        }
    }
}
