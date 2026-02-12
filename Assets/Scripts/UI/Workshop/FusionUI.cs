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
    /// 합성 UI Controller (View + Controller)
    /// ⭐ WorkshopInventoryUI 공통 LeftSection 사용
    /// - 혼합 등급 선택 가능 (D+C 동시 선택)
    /// - 등급별 필요 개수의 배수만 선택 가능
    /// - 보상 미리보기 (여러 등급 결과 아이템, 최대 10개)
    /// - ConfirmationPopup → FusionSystem → ResultFeedbackPopup 플로우
    /// </summary>
    public class FusionUI : MonoBehaviour
    {
        [Header("=== 연동 컴포넌트 ===")]
        [SerializeField] private WorkshopInventoryUI workshopInventoryUI;

        [Header("=== Single Fusion UI (단일 합성) ===")]
        [SerializeField] private GameObject singleFusionPanel; // 단일 합성 패널
        [SerializeField] private CanvasGroup singleFusionCanvasGroup; // Fade 애니메이션용
        [SerializeField] private GameObject[] materialSlotGroups = new GameObject[5]; // 슬롯 그룹 (활성화/비활성화)
        [SerializeField] private InventorySlot[] materialSlots = new InventorySlot[5]; // 재료 슬롯
        [SerializeField] private InventorySlot singleResultSlot; // 결과 슬롯
        
        [Header("=== Multi Fusion UI (다중 합성) ===")]
        [SerializeField] private GameObject multiFusionPanel; // 다중 합성 패널 (기존)
        [SerializeField] private CanvasGroup multiFusionCanvasGroup; // Fade 애니메이션용
        [SerializeField] private GameObject selectedItemsPanel;
        [SerializeField] private Transform selectedGroupsContainer; // 등급별 그룹 표시용
        [SerializeField] private GameObject gradeGroupPrefab; // 등급별 그룹 프리팹
        
        [Header("보상 미리보기 (최대 10개)")]
        [SerializeField] private GameObject rewardPreview;
        [SerializeField] private Transform rewardSlotsContainer;
        [SerializeField] private GameObject materialSlotPrefab;
        
        [Header("실행 버튼")]
        [SerializeField] private Button fusionButton;
        [SerializeField] private TextMeshProUGUI fusionButtonText;
        [SerializeField] private TextMeshProUGUI fusionWarningText; // 조건 불만족 경고
        [SerializeField] private Button cancelButton;

        [Header("=== 팝업 참조 ===")]
        [SerializeField] private ConfirmationPopup confirmationPopup;
        [SerializeField] private ResultFeedbackPopup resultFeedbackPopup;

        [Header("=== 디버그 ===")]
        [SerializeField] private bool showDebugLogs = true;

        // 상태 관리
        private List<ItemInstanceId> selectedItemIds = new List<ItemInstanceId>();
        private FusionRule fusionRule;

        private void Start()
        {
            // FusionRule 로드
            fusionRule = Resources.Load<FusionRule>("Data/FusionRule");
            if (fusionRule == null)
            {
                Debug.LogError("❌ [FusionUI] FusionRule을 찾을 수 없습니다! (Resources/Data/FusionRule)");
            }
            
            // 버튼 이벤트 연결
            if (fusionButton != null)
                fusionButton.onClick.AddListener(OnFusionButtonClicked);
            
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }

        /// <summary>
        /// 합성 UI 초기화 (WorkshopUI에서 호출)
        /// </summary>
        public void Initialize()
        {
            if (showDebugLogs)
                Debug.Log("📦 [FusionUI] 초기화 시작");
            
            // ⭐ WorkshopInventoryUI 이벤트 구독
            if (workshopInventoryUI != null)
            {
                workshopInventoryUI.OnSelectionChanged -= OnInventorySelectionChanged;
                workshopInventoryUI.OnSelectionChanged += OnInventorySelectionChanged;
                
                if (showDebugLogs)
                    Debug.Log("✅ [FusionUI] WorkshopInventoryUI 이벤트 구독 완료");
            }
            else
            {
                Debug.LogError("❌ [FusionUI] workshopInventoryUI가 null입니다! Inspector에서 연결하세요.");
            }
            
            // 초기 UI 상태
            selectedItemIds.Clear();
            UpdateUI();
            
            if (showDebugLogs)
                Debug.Log("✅ [FusionUI] 초기화 완료");
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
                Debug.Log($"🔔 [FusionUI] 선택 변경 이벤트: {selectedIds.Count}개");
            
            selectedItemIds = new List<ItemInstanceId>(selectedIds);
            UpdateUI();
        }

        /// <summary>
        /// 취소 버튼 클릭 (선택 초기화)
        /// </summary>
        private void OnCancelButtonClicked()
        {
            Debug.Log("🚫 [FusionUI] 취소 버튼 클릭 - 선택 초기화 시작");
            
            if (workshopInventoryUI != null)
            {
                workshopInventoryUI.ClearSelection();
                Debug.Log("✅ [FusionUI] WorkshopInventoryUI.ClearSelection() 호출 완료");
            }
            else
            {
                Debug.LogError("❌ [FusionUI] workshopInventoryUI가 null입니다!");
            }
        }

        /// <summary>
        /// UI 전체 업데이트
        /// </summary>
        private void UpdateUI()
        {
            // ⭐ 단일 vs 다중 합성 판정
            if (IsSingleFusion())
            {
                // 단일 합성 모드: 슬롯 기반 UI
                ShowSingleFusionUI();
            }
            else
            {
                // 다중 합성 모드: 텍스트 목록 UI (기존)
                ShowMultiFusionUI();
            }
            
            // 합성 버튼 상태
            UpdateFusionButton();
        }

        /// <summary>
        /// 선택된 아이템 패널 업데이트 (등급별 그룹핑)
        /// </summary>
        private void UpdateSelectedItemsPanel()
        {
            if (selectedItemIds.Count == 0)
            {
                if (selectedItemsPanel != null)
                    selectedItemsPanel.SetActive(false);
                return;
            }

            if (selectedItemsPanel != null)
                selectedItemsPanel.SetActive(true);

            // 기존 그룹 제거
            ClearSelectedGroups();

            // 등급별 그룹핑
            var gradeGroups = GroupItemsByGrade(selectedItemIds);

            if (showDebugLogs)
                Debug.Log($"📊 [FusionUI] 등급별 그룹: {gradeGroups.Count}개");

            // 각 등급 그룹 표시
            foreach (var gradeGroup in gradeGroups.OrderByDescending(g => g.Key))
            {
                CreateGradeGroup(gradeGroup.Key, gradeGroup.Value);
            }
        }

        /// <summary>
        /// 등급별 그룹핑
        /// </summary>
        private Dictionary<ItemGrade, List<ItemInstanceId>> GroupItemsByGrade(List<ItemInstanceId> itemIds)
        {
            var groups = new Dictionary<ItemGrade, List<ItemInstanceId>>();
            
            foreach (var itemId in itemIds)
            {
                var instanceData = AccountDataManager.Instance.GetInstance(itemId);
                if (instanceData == null) continue;

                var equipData = ItemTemplateResolver.Load(instanceData.templateName);
                if (equipData == null) continue;

                if (!groups.ContainsKey(equipData.itemGrade))
                {
                    groups[equipData.itemGrade] = new List<ItemInstanceId>();
                }

                groups[equipData.itemGrade].Add(itemId);
            }

            return groups;
        }

        /// <summary>
        /// 등급+세부타입별로 아이템 그룹핑 (합성용)
        /// Helmet과 Armor를 구분하기 위해 WeaponType/ArmorType/AccessoryType까지 포함
        /// </summary>
        private Dictionary<(ItemGrade grade, string detailedType), List<ItemInstanceId>> GroupItemsByGradeAndType(List<ItemInstanceId> itemIds)
        {
            var groups = new Dictionary<(ItemGrade, string), List<ItemInstanceId>>();
            
            foreach (var itemId in itemIds)
            {
                var instanceData = AccountDataManager.Instance.GetInstance(itemId);
                if (instanceData == null) continue;

                var equipData = ItemTemplateResolver.Load(instanceData.templateName);
                if (equipData == null) continue;

                // ⭐ 세부 타입 문자열 생성 (Helmet ≠ Armor)
                string detailedType = GetDetailedEquipmentType(equipData);
                var key = (equipData.itemGrade, detailedType);
                
                if (!groups.ContainsKey(key))
                {
                    groups[key] = new List<ItemInstanceId>();
                }

                groups[key].Add(itemId);
            }

            return groups;
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
        /// 등급 그룹 UI 생성
        /// </summary>
        private void CreateGradeGroup(ItemGrade grade, List<ItemInstanceId> itemIds)
        {
            if (gradeGroupPrefab == null || selectedGroupsContainer == null)
            {
                Debug.LogError("❌ [FusionUI] gradeGroupPrefab 또는 selectedGroupsContainer가 null!");
                return;
            }

            GameObject groupObj = Instantiate(gradeGroupPrefab, selectedGroupsContainer);
            
            // 그룹 UI 설정 (TMP_Text 컴포넌트 찾기)
            var gradeText = groupObj.transform.Find("GradeText")?.GetComponent<TextMeshProUGUI>();
            var countText = groupObj.transform.Find("CountText")?.GetComponent<TextMeshProUGUI>();
            var statusText = groupObj.transform.Find("StatusText")?.GetComponent<TextMeshProUGUI>();

            if (gradeText != null)
            {
                gradeText.text = $"{grade}등급";
                gradeText.color = GetGradeColor(grade);
            }

            int requiredCount = fusionRule != null ? fusionRule.GetRequiredCount(grade) : 3;
            int currentCount = itemIds.Count;
            int fusionTimes = currentCount / requiredCount;
            int remainder = currentCount % requiredCount;

            if (countText != null)
            {
                countText.text = $"{currentCount}개 선택";
            }

            if (statusText != null)
            {
                if (remainder == 0 && currentCount >= requiredCount)
                {
                    // 조건 만족
                    statusText.text = $"✅ {fusionTimes}회 합성 가능";
                    statusText.color = Color.green;
                }
                else
                {
                    // 조건 불만족
                    statusText.text = $"⚠️ {requiredCount}개 배수 필요 (부족: {requiredCount - remainder}개)";
                    statusText.color = Color.red;
                }
            }

            if (showDebugLogs)
                Debug.Log($"📦 [FusionUI] {grade}등급 그룹 생성: {currentCount}개 ({fusionTimes}회 합성, 나머지 {remainder}개)");
        }

        /// <summary>
        /// 등급별 색상 반환
        /// </summary>
        private Color GetGradeColor(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.D => new Color(0.6f, 0.6f, 0.6f), // 회색
                ItemGrade.C => new Color(0.4f, 1f, 0.4f),   // 초록색
                ItemGrade.B => new Color(0.4f, 0.7f, 1f),   // 파란색
                ItemGrade.A => new Color(0.8f, 0.4f, 1f),   // 보라색
                ItemGrade.S => new Color(1f, 0.8f, 0.2f),   // 금색
                ItemGrade.SS => new Color(1f, 0.4f, 0.2f),  // 주황색
                _ => Color.white
            };
        }

        /// <summary>
        /// 기존 등급 그룹 제거
        /// </summary>
        private void ClearSelectedGroups()
        {
            if (selectedGroupsContainer == null) return;

            int childCount = selectedGroupsContainer.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = selectedGroupsContainer.GetChild(i);
                DestroyImmediate(child.gameObject);
            }
            
            if (showDebugLogs)
                Debug.Log($"🧹 [FusionUI] 기존 그룹 {childCount}개 제거 완료");
        }

        /// <summary>
        /// 보상 미리보기 업데이트 (최대 10개)
        /// </summary>
        private void UpdateRewardPreview()
        {
            if (selectedItemIds.Count == 0)
            {
                if (rewardPreview != null)
                    rewardPreview.SetActive(false);
                return;
            }

            // 1. 합성 결과 계산
            var fusionResults = CalculateFusionResults(selectedItemIds);
            
            if (fusionResults.Count == 0)
            {
                if (rewardPreview != null)
                    rewardPreview.SetActive(false);
                return;
            }

            // ⭐ 총 슬롯 개수 계산 (count 합산)
            int totalSlotCount = 0;
            foreach (var result in fusionResults)
            {
                totalSlotCount += result.Value;
            }

            if (showDebugLogs)
            {
                Debug.Log($"💎 [FusionUI] 합성 결과: {fusionResults.Count}종류, 총 {totalSlotCount}개");
                foreach (var result in fusionResults)
                {
                    Debug.Log($"   ├─ {result.Key} x{result.Value}개");
                }
            }

            // 2. 슬롯 크기 계산 (총 개수 기준)
            int slotSize = CalculateSlotSize(totalSlotCount);
            
            if (showDebugLogs)
                Debug.Log($"📏 [FusionUI] 슬롯 크기: {slotSize}px (총 {totalSlotCount}개)");

            // 3. 기존 슬롯 제거
            ClearRewardSlots();

            // 4. ⭐ Phase 0 교훈: Panel 활성화 후 1프레임 대기
            if (rewardPreview != null && !rewardPreview.activeSelf)
            {
                if (showDebugLogs)
                    Debug.Log("🔍 [FusionUI] rewardPreview 활성화 → 1프레임 대기 후 슬롯 설정");
                
                rewardPreview.SetActive(true);
                StartCoroutine(UpdateRewardPreviewDelayed(fusionResults, slotSize));
                return;
            }

            // 5. 즉시 슬롯 생성
            UpdateRewardPreviewImmediate(fusionResults, slotSize);
        }

        /// <summary>
        /// 합성 결과 계산 (등급+타입별 그룹 → 결과 아이템)
        /// </summary>
        private Dictionary<string, int> CalculateFusionResults(List<ItemInstanceId> itemIds)
        {
            var results = new Dictionary<string, int>();
            
            if (fusionRule == null)
            {
                Debug.LogError("❌ [FusionUI] FusionRule이 null입니다!");
                return results;
            }

            // ⭐ 등급+타입별로 그룹핑
            var gradeAndTypeGroups = GroupItemsByGradeAndType(itemIds);

            foreach (var groupKey in gradeAndTypeGroups.Keys)
            {
                ItemGrade currentGrade = groupKey.grade;
                string detailedType = groupKey.detailedType;
                var itemsInGroup = gradeAndTypeGroups[groupKey];
                
                int count = itemsInGroup.Count;
                int requiredCount = fusionRule.GetRequiredCount(currentGrade);

                // 합성 가능 횟수
                int fusionTimes = count / requiredCount;
                
                if (fusionTimes <= 0) continue;

                // 결과 등급
                ItemGrade resultGrade = fusionRule.GetNextGrade(currentGrade);

                // 결과 아이템 템플릿 이름 (그룹의 첫 번째 아이템 기준)
                var firstItemId = itemsInGroup[0];
                var firstInstance = AccountDataManager.Instance.GetInstance(firstItemId);
                if (firstInstance == null) continue;

                var firstEquipData = ItemTemplateResolver.Load(firstInstance.templateName);
                if (firstEquipData == null) continue;

                // ⭐ 템플릿 이름에서 등급만 변경 (클래스 유지)
                // 예: "Helmet_Warrior_D" → "Helmet_Warrior_C"
                // 마지막 언더스코어 이후 등급 문자열 교체 (가장 안전한 방법)
                string templateName = firstInstance.templateName;
                string resultTemplateName = templateName;
                
                int lastUnderscoreIndex = templateName.LastIndexOf('_');
                if (lastUnderscoreIndex >= 0)
                {
                    string prefix = templateName.Substring(0, lastUnderscoreIndex + 1); // "Helmet_Warrior_"
                    resultTemplateName = prefix + resultGrade.ToString(); // "Helmet_Warrior_C"
                }
                else
                {
                    // 언더스코어가 없으면 그냥 뒤에 추가
                    resultTemplateName = templateName + "_" + resultGrade.ToString();
                }

                // 결과에 추가
                if (results.ContainsKey(resultTemplateName))
                {
                    results[resultTemplateName] += fusionTimes;
                }
                else
                {
                    results[resultTemplateName] = fusionTimes;
                }

                if (showDebugLogs)
                    Debug.Log($"💎 [FusionUI] {currentGrade} {detailedType} x{count}개 → {resultGrade} {detailedType} x{fusionTimes}개 ({resultTemplateName})");
            }

            return results;
        }

        /// <summary>
        /// 보상 미리보기 지연 업데이트 (1프레임 대기)
        /// </summary>
        private IEnumerator UpdateRewardPreviewDelayed(Dictionary<string, int> results, int slotSize)
        {
            yield return null; // 1프레임 대기 (Layout Group 초기화)
            
            if (showDebugLogs)
                Debug.Log("⏰ [FusionUI] 1프레임 대기 완료 → 보상 슬롯 생성 시작");
            
            UpdateRewardPreviewImmediate(results, slotSize);
        }

        /// <summary>
        /// 보상 미리보기 즉시 업데이트
        /// </summary>
        private void UpdateRewardPreviewImmediate(Dictionary<string, int> results, int slotSize)
        {
            // ⭐ GridLayoutGroup의 Cell Size 동적 조정
            if (rewardSlotsContainer != null)
            {
                var gridLayout = rewardSlotsContainer.GetComponent<UnityEngine.UI.GridLayoutGroup>();
                if (gridLayout != null)
                {
                    gridLayout.cellSize = new Vector2(slotSize, slotSize);
                    
                    if (showDebugLogs)
                        Debug.Log($"📐 [FusionUI] GridLayoutGroup Cell Size 설정: {slotSize}x{slotSize}");
                }
            }
            
            // ⭐ 각 아이템을 count만큼 슬롯 생성 (아이템은 개수 표시 없으므로)
            int displayCount = 0;
            foreach (var result in results)
            {
                string templateName = result.Key;
                int count = result.Value;
                
                // count만큼 반복하여 슬롯 생성
                for (int i = 0; i < count; i++)
                {
                    CreateResultSlot(templateName, slotSize);
                    displayCount++;
                    
                    if (showDebugLogs)
                        Debug.Log($"📦 [FusionUI] 결과 슬롯 생성: {templateName} ({i+1}/{count})");
                }
            }
            
            if (showDebugLogs)
                Debug.Log($"✅ [FusionUI] 결과 슬롯 {displayCount}개 생성 완료");
        }

        /// <summary>
        /// 결과 슬롯 생성 (⭐ Phase 0 교훈: Instantiate 후 1프레임 대기)
        /// </summary>
        private void CreateResultSlot(string templateName, int slotSize)
        {
            if (materialSlotPrefab == null || rewardSlotsContainer == null)
            {
                Debug.LogError("❌ [FusionUI] materialSlotPrefab 또는 rewardSlotsContainer가 null!");
                return;
            }

            GameObject slotObj = Instantiate(materialSlotPrefab, rewardSlotsContainer);
            
            RectTransform rt = slotObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(slotSize, slotSize);
            }

            // ⭐ Phase 0 교훈: Instantiate 후 1프레임 대기
            StartCoroutine(SetupResultSlotDelayed(slotObj, templateName));
        }

        /// <summary>
        /// 결과 슬롯 설정 지연 (1프레임 대기)
        /// </summary>
        private IEnumerator SetupResultSlotDelayed(GameObject slotObj, string templateName)
        {
            yield return null; // 1프레임 대기 (Layout Group에 추가 완료)
            
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            if (slot == null)
            {
                Debug.LogError("❌ [FusionUI] ResultSlot에 InventorySlot 컴포넌트 없음");
                yield break;
            }

            // 결과 아이템 로드
            var equipData = ItemTemplateResolver.Load(templateName);
            if (equipData == null)
            {
                Debug.LogError($"❌ [FusionUI] 결과 아이템 로드 실패: {templateName}");
                yield break;
            }

            // ⭐ 임시 ItemInstanceId 생성 (미리보기용)
            // 실제 합성 전이므로 유효한 ID는 아님
            ItemInstanceId previewId = default;

            slot.SetEquipmentData(equipData, previewId);
            
            if (showDebugLogs)
                Debug.Log($"📦 [FusionUI] 결과 슬롯 설정 완료: {equipData.equipmentName}");
        }

        /// <summary>
        /// 슬롯 크기 계산 (자동 크기 조절)
        /// </summary>
        private int CalculateSlotSize(int totalCount)
        {
            // ⭐ 총 슬롯 개수 기준으로 크기 결정
            if (totalCount <= 3) return 120;  // 1~3개: 큰 슬롯
            if (totalCount <= 6) return 90;   // 4~6개: 중간 슬롯
            if (totalCount <= 10) return 75;  // 7~10개: 작은 슬롯
            return 60;                        // 11개 이상: 최소 슬롯
        }

        /// <summary>
        /// 기존 보상 슬롯 제거
        /// </summary>
        private void ClearRewardSlots()
        {
            if (rewardSlotsContainer == null) return;

            int childCount = rewardSlotsContainer.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = rewardSlotsContainer.GetChild(i);
                DestroyImmediate(child.gameObject);
            }
            
            if (showDebugLogs)
                Debug.Log($"🧹 [FusionUI] 기존 슬롯 {childCount}개 즉시 제거 완료");
        }

        /// <summary>
        /// 합성 버튼 상태 업데이트
        /// </summary>
        private void UpdateFusionButton()
        {
            bool canFuse = false;
            string warningMessage = "";

            if (selectedItemIds.Count == 0)
            {
                warningMessage = "";
            }
            else
            {
                // ⭐ 등급+타입별 그룹핑
                var gradeAndTypeGroups = GroupItemsByGradeAndType(selectedItemIds);
                
                bool allGroupsValid = true;
                
                foreach (var groupKey in gradeAndTypeGroups.Keys)
                {
                    ItemGrade grade = groupKey.grade;
                    string detailedType = groupKey.detailedType;
                    var itemsInGroup = gradeAndTypeGroups[groupKey];
                    
                    int requiredCount = fusionRule != null ? fusionRule.GetRequiredCount(grade) : 3;
                    int currentCount = itemsInGroup.Count;
                    int remainder = currentCount % requiredCount;

                    if (remainder != 0 || currentCount < requiredCount)
                    {
                        allGroupsValid = false;
                        warningMessage = $"{grade} {detailedType}: {requiredCount}개 배수 필요 (현재 {currentCount}개)";
                        break;
                    }
                }

                canFuse = allGroupsValid && gradeAndTypeGroups.Count > 0;
            }

            // 버튼 상태
            if (fusionButton != null)
            {
                fusionButton.interactable = canFuse;
            }

            if (fusionButtonText != null)
            {
                if (canFuse)
                {
                    int totalFusions = CalculateTotalFusionTimes();
                    fusionButtonText.text = $"합성 ({totalFusions}회)";
                }
                else if (selectedItemIds.Count == 0)
                {
                    fusionButtonText.text = "아이템 선택";
                }
                else
                {
                    fusionButtonText.text = "합성 불가";
                }
            }

            // 경고 텍스트
            if (fusionWarningText != null)
            {
                if (canFuse)
                {
                    fusionWarningText.gameObject.SetActive(false);
                    fusionWarningText.text = "";
                }
                else if (!string.IsNullOrEmpty(warningMessage))
                {
                    fusionWarningText.gameObject.SetActive(true);
                    fusionWarningText.text = warningMessage;
                    fusionWarningText.color = new Color(1f, 0.3f, 0.3f); // 빨간색
                }
                else
                {
                    fusionWarningText.gameObject.SetActive(false);
                    fusionWarningText.text = "";
                }
            }
        }

        /// <summary>
        /// 총 합성 횟수 계산 (등급+타입별)
        /// </summary>
        private int CalculateTotalFusionTimes()
        {
            int totalTimes = 0;
            
            // ⭐ 등급+타입별 그룹핑
            var gradeAndTypeGroups = GroupItemsByGradeAndType(selectedItemIds);

            foreach (var groupKey in gradeAndTypeGroups.Keys)
            {
                ItemGrade grade = groupKey.grade;
                var itemsInGroup = gradeAndTypeGroups[groupKey];
                
                int requiredCount = fusionRule != null ? fusionRule.GetRequiredCount(grade) : 3;
                int currentCount = itemsInGroup.Count;
                totalTimes += currentCount / requiredCount;
            }

            return totalTimes;
        }

        /// <summary>
        /// 합성 버튼 클릭 처리
        /// </summary>
        private void OnFusionButtonClicked()
        {
            if (selectedItemIds.Count == 0)
            {
                Debug.LogWarning("⚠️ [FusionUI] 선택된 아이템 없음");
                return;
            }

            if (showDebugLogs)
                Debug.Log($"🔨 [FusionUI] 합성 버튼 클릭 - {selectedItemIds.Count}개 아이템");
            
            // 강화 경고 체크
            if (FusionSystem.NeedsEnhancementWarning(selectedItemIds, out int maxEnhancementLevel))
            {
                ShowEnhancementWarning(maxEnhancementLevel);
            }
            else
            {
                ShowConfirmation();
            }
        }

        /// <summary>
        /// 강화 경고 팝업 표시
        /// </summary>
        private void ShowEnhancementWarning(int maxEnhancementLevel)
        {
            if (confirmationPopup == null)
            {
                Debug.LogError("❌ [FusionUI] ConfirmationPopup 참조 없음! Inspector에서 연결하세요.");
                return;
            }

            var fusionResults = CalculateFusionResults(selectedItemIds);
            string rewardText = string.Join("\n", fusionResults.Select(r => 
            {
                var equipData = ItemTemplateResolver.Load(r.Key);
                string displayName = equipData != null ? equipData.equipmentName : r.Key;
                return $"• {displayName} x{r.Value}개";
            }));

            string title = "⚠️ 강화 아이템 합성 경고";
            string message = $"선택한 아이템 중 최대 +{maxEnhancementLevel} 강화된 아이템이 포함되어 있습니다.";
            string detail = $"합성 결과:\n{rewardText}\n\n⚠️ 합성 시 모든 강화 수치가 초기화됩니다.\n정말로 합성하시겠습니까?";

            confirmationPopup.Show(
                title: title,
                message: message,
                detail: detail,
                onConfirm: ShowConfirmation, // 경고 확인 후 일반 확인 팝업
                onCancel: OnConfirmationCancelled
            );

            if (showDebugLogs)
                Debug.Log($"💬 [FusionUI] 강화 경고 팝업 표시: 최대 +{maxEnhancementLevel}");
        }

        /// <summary>
        /// 확인 팝업 표시
        /// </summary>
        private void ShowConfirmation()
        {
            if (confirmationPopup == null)
            {
                Debug.LogError("❌ [FusionUI] ConfirmationPopup 참조 없음! Inspector에서 연결하세요.");
                return;
            }

            var fusionResults = CalculateFusionResults(selectedItemIds);
            string rewardText = string.Join("\n", fusionResults.Select(r => 
            {
                var equipData = ItemTemplateResolver.Load(r.Key);
                string displayName = equipData != null ? equipData.equipmentName : r.Key;
                return $"• {displayName} x{r.Value}개";
            }));

            int totalFusions = CalculateTotalFusionTimes();
            string title = "아이템 합성";
            string message = $"선택한 아이템을 합성하시겠습니까? ({totalFusions}회 합성)";
            string detail = $"합성 결과:\n{rewardText}\n\n※ 합성된 아이템은 복구할 수 없습니다.";

            confirmationPopup.Show(
                title: title,
                message: message,
                detail: detail,
                onConfirm: ExecuteFusion,
                onCancel: OnConfirmationCancelled
            );

            if (showDebugLogs)
                Debug.Log($"💬 [FusionUI] 확인 팝업 표시: {selectedItemIds.Count}개 아이템");
        }

        /// <summary>
        /// 확인 팝업에서 취소 버튼 클릭 시 호출
        /// </summary>
        private void OnConfirmationCancelled()
        {
            Debug.Log("🚫 [FusionUI] 확인 팝업 취소 - 선택 초기화");
            
            if (workshopInventoryUI != null)
            {
                workshopInventoryUI.ClearSelection();
            }
            else
            {
                Debug.LogError("❌ [FusionUI] workshopInventoryUI가 null입니다!");
            }
        }

        /// <summary>
        /// 합성 실행 (등급별 그룹 처리)
        /// </summary>
        private void ExecuteFusion()
        {
            if (showDebugLogs)
                Debug.Log($"⚙️ [FusionUI] 합성 실행 시작 - {selectedItemIds.Count}개");

            // ⭐ 등급+타입별로 그룹핑
            var gradeAndTypeGroups = GroupItemsByGradeAndType(selectedItemIds);
            var totalResults = new Dictionary<string, int>();
            int successCount = 0;
            int failCount = 0;

            foreach (var groupKey in gradeAndTypeGroups.Keys)
            {
                ItemGrade grade = groupKey.grade;
                string detailedType = groupKey.detailedType;
                var itemsInGroup = gradeAndTypeGroups[groupKey];
                
                int requiredCount = fusionRule.GetRequiredCount(grade);
                int fusionTimes = itemsInGroup.Count / requiredCount;

                if (showDebugLogs)
                    Debug.Log($"⚙️ [FusionUI] {grade} {detailedType} 그룹: {itemsInGroup.Count}개 → {fusionTimes}회 합성 예정");

                // 각 합성 횟수만큼 실행
                for (int i = 0; i < fusionTimes; i++)
                {
                    // 필요한 만큼의 아이템 선택
                    var materialsForThisFusion = itemsInGroup.Skip(i * requiredCount).Take(requiredCount).ToList();

                    // 합성 실행
                    bool success = FusionSystem.ExecuteFusion(materialsForThisFusion, out ItemInstanceId resultId);

                    if (success)
                    {
                        successCount++;
                        
                        // 결과 아이템 정보
                        var resultInstance = AccountDataManager.Instance.GetInstance(resultId);
                        if (resultInstance != null)
                        {
                            if (totalResults.ContainsKey(resultInstance.templateName))
                            {
                                totalResults[resultInstance.templateName]++;
                            }
                            else
                            {
                                totalResults[resultInstance.templateName] = 1;
                            }
                        }
                    }
                    else
                    {
                        failCount++;
                    }
                }
            }

            if (showDebugLogs)
                Debug.Log($"✅ [FusionUI] 합성 완료 - 성공: {successCount}, 실패: {failCount}");

            // 결과 팝업 표시
            if (resultFeedbackPopup != null && successCount > 0)
            {
                resultFeedbackPopup.ShowFusionResult(successCount, totalResults);
            }
            else if (resultFeedbackPopup == null)
            {
                Debug.LogError("❌ [FusionUI] ResultFeedbackPopup 참조 없음! Inspector에서 연결하세요.");
            }

            // ⭐ 선택 초기화 및 인벤토리 UI 갱신
            if (workshopInventoryUI != null)
            {
                workshopInventoryUI.ClearSelection();
                workshopInventoryUI.RefreshInventoryDisplay();
                
                if (showDebugLogs)
                    Debug.Log("🔄 [FusionUI] 인벤토리 UI 갱신 완료");
            }
            else
            {
                Debug.LogError("❌ [FusionUI] workshopInventoryUI가 null입니다!");
            }
            
            // UI 갱신
            selectedItemIds.Clear();
            UpdateUI();
        }
        
        #region Single Fusion UI (단일 합성 모드)
        
        /// <summary>
        /// 단일 합성 모드 판정
        /// - 조건: 단일 타입 + 정확한 필요 개수
        /// </summary>
        /// <summary>
        /// 단일 합성 UI 사용 여부 판단
        /// 
        /// 조건:
        /// 1. 개별 클릭 모드 (IsIndividualSelectionMode == true)
        /// 2. 단일 타입 (동일 등급 + 동일 세부타입)
        /// 3. requiredCount 이하 (초과 시 Multi로 전환)
        /// 
        /// ⭐ 일괄 선택 모드는 항상 Multi
        /// </summary>
        private bool IsSingleFusion()
        {
            Debug.Log($"🔍 [FusionUI] IsSingleFusion() 체크 시작");
            Debug.Log($"   selectedItemIds.Count: {selectedItemIds.Count}");
            
            if (selectedItemIds.Count == 0)
            {
                Debug.Log($"   ❌ 선택된 아이템 없음 → Multi");
                return false;
            }
            
            // ⭐ 일괄 선택 모드는 항상 MultiFusion
            if (workshopInventoryUI == null)
            {
                Debug.LogError($"   ❌ workshopInventoryUI가 null! → Multi");
                return false;
            }
            
            bool isIndividualMode = workshopInventoryUI.IsIndividualSelectionMode;
            Debug.Log($"   IsIndividualSelectionMode: {isIndividualMode}");
            
            if (!isIndividualMode)
            {
                Debug.Log($"   ❌ 일괄 선택 모드 → Multi");
                return false;
            }
            
            // ⭐ 개별 클릭 모드에서만:
            // 1. 단일 타입이어야 함
            var gradeAndTypeGroups = GroupItemsByGradeAndType(selectedItemIds);
            Debug.Log($"   gradeAndTypeGroups.Count: {gradeAndTypeGroups.Count}");
            
            if (gradeAndTypeGroups.Count != 1)
            {
                Debug.Log($"   ❌ 여러 타입 선택됨 → Multi");
                return false;
            }
            
            // 2. requiredCount 이하여야 함 (초과 시 MultiFusion 전환)
            var firstGroup = gradeAndTypeGroups.First();
            ItemGrade grade = firstGroup.Key.grade;
            int requiredCount = fusionRule != null ? fusionRule.GetRequiredCount(grade) : 3;
            
            Debug.Log($"   등급: {grade}, requiredCount: {requiredCount}, selectedItemIds.Count: {selectedItemIds.Count}");
            
            bool result = selectedItemIds.Count <= requiredCount;
            Debug.Log($"   ✅ 최종 결과: {(result ? "Single" : "Multi")}");
            
            return result;
        }
        
        /// <summary>
        /// 단일 합성 UI 표시
        /// </summary>
        private void ShowSingleFusionUI()
        {
            if (selectedItemIds.Count == 0)
            {
                // ⭐ 선택 없음 → 슬롯 초기화
                ClearSingleFusionSlots();
                
                // 선택 없음 → 모두 숨김
                if (singleFusionPanel != null)
                    singleFusionPanel.SetActive(false);
                if (multiFusionPanel != null)
                    multiFusionPanel.SetActive(false);
                return;
            }
            
            // 패널 전환 (애니메이션)
            StartCoroutine(SwitchToSingleFusionPanel());
            
            // 슬롯 설정
            UpdateSingleFusionSlots();
        }
        
        /// <summary>
        /// 단일 합성 슬롯 초기화 (이전 데이터 제거)
        /// </summary>
        private void ClearSingleFusionSlots()
        {
            // 모든 재료 슬롯 초기화
            for (int i = 0; i < materialSlots.Length; i++)
            {
                if (materialSlots[i] != null)
                {
                    materialSlots[i].ClearSlot();
                    
                    if (showDebugLogs)
                        Debug.Log($"🧹 [FusionUI] MaterialSlot{i+1} 초기화 완료");
                }
            }
            
            // 결과 슬롯 초기화
            if (singleResultSlot != null)
            {
                singleResultSlot.ClearSlot();
                
                if (showDebugLogs)
                    Debug.Log($"🧹 [FusionUI] ResultSlot 초기화 완료");
            }
        }
        
        /// <summary>
        /// 단일 합성 슬롯 업데이트
        /// </summary>
        private void UpdateSingleFusionSlots()
        {
            if (selectedItemIds.Count == 0) return;
            
            // ⭐ 0. 이전 슬롯 데이터 완전 초기화 (A급 → B급 전환 시)
            ClearSingleFusionSlots();
            
            // 1. 등급 확인 (첫 번째 아이템 기준)
            var firstInstance = AccountDataManager.Instance.GetInstance(selectedItemIds[0]);
            if (firstInstance == null) return;
            
            var firstEquipData = ItemTemplateResolver.Load(firstInstance.templateName);
            if (firstEquipData == null) return;
            
            ItemGrade grade = firstEquipData.itemGrade;
            
            // 2. 필요 개수 확인
            int requiredCount = fusionRule != null ? fusionRule.GetRequiredCount(grade) : 3;
            
            // 3. 슬롯 활성화/비활성화
            for (int i = 0; i < 5; i++)
            {
                if (i < materialSlotGroups.Length && materialSlotGroups[i] != null)
                {
                    bool isActive = (i < requiredCount);
                    materialSlotGroups[i].SetActive(isActive);
                }
            }
            
            // 4. 선택된 아이템 표시 (강화 레벨 포함)
            for (int i = 0; i < selectedItemIds.Count && i < requiredCount && i < materialSlots.Length; i++)
            {
                if (materialSlots[i] == null) continue;
                
                var instance = AccountDataManager.Instance.GetInstance(selectedItemIds[i]);
                if (instance == null) continue;
                
                var equipData = ItemTemplateResolver.Load(instance.templateName);
                if (equipData == null) continue;
                
                // ⭐ 슬롯 설정 + 강화 레벨 표시
                materialSlots[i].SetEquipmentData(equipData, selectedItemIds[i]);
                
                if (showDebugLogs)
                    Debug.Log($"📦 [FusionUI] MaterialSlot{i+1}: {equipData.equipmentName} +{instance.enhancementLevel}");
            }
            
            // 5. 결과 아이템 미리보기
            if (singleResultSlot != null)
            {
                ItemGrade resultGrade = fusionRule != null ? fusionRule.GetNextGrade(grade) : grade;
                string resultTemplateName = GetResultTemplateName(firstInstance.templateName, grade, resultGrade);
                
                var resultEquipData = ItemTemplateResolver.Load(resultTemplateName);
                if (resultEquipData != null)
                {
                    singleResultSlot.SetEquipmentData(resultEquipData, default);
                    
                    if (showDebugLogs)
                        Debug.Log($"🎁 [FusionUI] ResultSlot: {resultEquipData.equipmentName} +0 (초기화)");
                }
            }
        }
        
        /// <summary>
        /// 결과 아이템 템플릿 이름 생성
        /// </summary>
        private string GetResultTemplateName(string baseTemplateName, ItemGrade currentGrade, ItemGrade resultGrade)
        {
            // 마지막 언더스코어 이후 등급만 교체
            int lastUnderscoreIndex = baseTemplateName.LastIndexOf('_');
            if (lastUnderscoreIndex >= 0)
            {
                string prefix = baseTemplateName.Substring(0, lastUnderscoreIndex + 1);
                return prefix + resultGrade.ToString();
            }
            else
            {
                return baseTemplateName + "_" + resultGrade.ToString();
            }
        }
        
        /// <summary>
        /// 다중 합성 UI 표시 (기존 로직)
        /// </summary>
        private void ShowMultiFusionUI()
        {
            if (selectedItemIds.Count == 0)
            {
                // ⭐ 선택 없음 → 슬롯 초기화
                ClearSingleFusionSlots();
                
                // 선택 없음 → 모두 숨김
                if (singleFusionPanel != null)
                    singleFusionPanel.SetActive(false);
                if (multiFusionPanel != null)
                    multiFusionPanel.SetActive(false);
                return;
            }
            
            // ⭐ SingleFusion 슬롯 초기화 (Multi로 전환 시 이전 데이터 제거)
            ClearSingleFusionSlots();
            
            // 패널 전환 (애니메이션)
            StartCoroutine(SwitchToMultiFusionPanel());
            
            // 기존 로직
            UpdateSelectedItemsPanel();
            UpdateRewardPreview();
        }
        
        /// <summary>
        /// 단일 합성 패널로 전환 (Fade 애니메이션)
        /// </summary>
        private IEnumerator SwitchToSingleFusionPanel()
        {
            // 1. Multi 패널 Fade Out
            if (multiFusionPanel != null && multiFusionPanel.activeSelf)
            {
                yield return StartCoroutine(FadeOut(multiFusionCanvasGroup));
                multiFusionPanel.SetActive(false);
            }
            
            // 2. Single 패널 Fade In
            if (singleFusionPanel != null)
            {
                singleFusionPanel.SetActive(true);
                yield return StartCoroutine(FadeIn(singleFusionCanvasGroup));
            }
        }
        
        /// <summary>
        /// 다중 합성 패널로 전환 (Fade 애니메이션)
        /// </summary>
        private IEnumerator SwitchToMultiFusionPanel()
        {
            // 1. Single 패널 Fade Out
            if (singleFusionPanel != null && singleFusionPanel.activeSelf)
            {
                yield return StartCoroutine(FadeOut(singleFusionCanvasGroup));
                singleFusionPanel.SetActive(false);
            }
            
            // 2. Multi 패널 Fade In
            if (multiFusionPanel != null)
            {
                multiFusionPanel.SetActive(true);
                yield return StartCoroutine(FadeIn(multiFusionCanvasGroup));
            }
        }
        
        /// <summary>
        /// Fade In 애니메이션 (0.3초)
        /// </summary>
        private IEnumerator FadeIn(CanvasGroup canvasGroup)
        {
            if (canvasGroup == null) yield break;
            
            float duration = 0.3f;
            float elapsed = 0f;
            
            canvasGroup.alpha = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
        
        /// <summary>
        /// Fade Out 애니메이션 (0.3초)
        /// </summary>
        private IEnumerator FadeOut(CanvasGroup canvasGroup)
        {
            if (canvasGroup == null) yield break;
            
            float duration = 0.3f;
            float elapsed = 0f;
            
            canvasGroup.alpha = 1f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
        }
        
        #endregion
    }
}

