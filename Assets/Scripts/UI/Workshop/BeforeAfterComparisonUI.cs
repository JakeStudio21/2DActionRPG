using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Workshop
{
    /// <summary>
    /// 🔄 제작 전/후 슬롯 비교 UI
    /// 책임:
    /// - 강화: 1개 → 1개 (레벨 +1)
    /// - 분해: 1개 → 3개 (재료들)
    /// - 합성: 3개 → 1개 (등급 상승)
    /// </summary>
    public class BeforeAfterComparisonUI : MonoBehaviour
    {
        [Header("📊 디버그")]
        [SerializeField] private bool showDebugLogs = true;
        
        [Header("🔗 연동 컴포넌트")]
        [SerializeField] private WorkshopUI workshopUI; // ⭐ 공방 메인 UI
        
        [Header("⬅️ 제작 전 슬롯")]
        [SerializeField] private InventorySlot beforeSlot1;
        [SerializeField] private InventorySlot beforeSlot2; // 합성 전용
        [SerializeField] private InventorySlot beforeSlot3; // 합성 전용
        [SerializeField] private TMP_Text beforeLabel;
        
        [Header("➡️ 제작 후 슬롯")]
        [SerializeField] private InventorySlot afterSlot1;
        [SerializeField] private InventorySlot afterSlot2;  // 분해 전용
        [SerializeField] private InventorySlot afterSlot3;  // 분해 전용
        [SerializeField] private TMP_Text afterLabel;
        
        [Header("🎨 화살표")]
        [SerializeField] private GameObject arrowIcon;
        [SerializeField] private Image arrowImage;
        
        // 현재 모드
        private WorkshopMode currentMode = WorkshopMode.None;
        private WorkshopUI.WorkshopTabType currentTab = WorkshopUI.WorkshopTabType.Enhancement; // ⭐ 현재 탭
        
        void Awake()
        {
            if (showDebugLogs)
                Debug.Log("🔄 [BeforeAfterComparisonUI] Awake() - 제작 전후 비교 UI 초기화");
            
            // 초기 상태: 모든 슬롯 비활성화
            ClearAll();
        }
        
        /// <summary>
        /// GameObject 활성화 시 이벤트 구독
        /// </summary>
        void OnEnable()
        {
            // ⭐ WorkshopUI 탭 변경 이벤트 구독
            if (workshopUI != null)
            {
                workshopUI.OnTabChanged += OnWorkshopTabChanged;
                
                // 현재 탭 가져오기
                currentTab = workshopUI.GetCurrentTab();
                
                // 탭별 슬롯 설정 적용
                SetupForWorkshopTab(currentTab);
                
                if (showDebugLogs)
                    Debug.Log($"🔗 [BeforeAfterComparisonUI] WorkshopUI 이벤트 구독 (현재 탭: {currentTab})");
            }
            else
            {
                Debug.LogWarning("⚠️ [BeforeAfterComparisonUI] workshopUI가 null입니다! Inspector에서 연결하세요.");
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
                    Debug.Log("🔄 [BeforeAfterComparisonUI] WorkshopUI 이벤트 구독 해제");
            }
        }
        
        /// <summary>
        /// 공방 탭 변경 이벤트 핸들러
        /// </summary>
        private void OnWorkshopTabChanged(WorkshopUI.WorkshopTabType newTab)
        {
            currentTab = newTab;
            
            if (showDebugLogs)
                Debug.Log($"🔄 [BeforeAfterComparisonUI] 공방 탭 변경: {newTab}");
            
            // ⭐ 1. 먼저 모든 슬롯 초기화
            ClearAll();
            
            // ⭐ 2. 그 다음 탭별 슬롯 활성화
            SetupForWorkshopTab(newTab);
        }
        
        /// <summary>
        /// 모든 슬롯 초기화
        /// </summary>
        public void ClearAll()
        {
            // 제작 전 슬롯
            if (beforeSlot1 != null)
            {
                beforeSlot1.ClearSlot();
                beforeSlot1.gameObject.SetActive(false);
            }
            
            if (beforeSlot2 != null)
            {
                beforeSlot2.ClearSlot();
                beforeSlot2.gameObject.SetActive(false);
            }
            
            if (beforeSlot3 != null)
            {
                beforeSlot3.ClearSlot();
                beforeSlot3.gameObject.SetActive(false);
            }
            
            // 제작 후 슬롯
            if (afterSlot1 != null)
            {
                afterSlot1.ClearSlot();
                afterSlot1.gameObject.SetActive(false);
            }
            
            if (afterSlot2 != null)
            {
                afterSlot2.ClearSlot();
                afterSlot2.gameObject.SetActive(false);
            }
            
            if (afterSlot3 != null)
            {
                afterSlot3.ClearSlot();
                afterSlot3.gameObject.SetActive(false);
            }
            
            // 화살표 비활성화
            if (arrowIcon != null)
            {
                arrowIcon.SetActive(false);
            }
            
            currentMode = WorkshopMode.None;
            
            if (showDebugLogs)
                Debug.Log("🔄 [BeforeAfterComparisonUI] 모든 슬롯 초기화");
        }
        
        /// <summary>
        /// 탭별 슬롯 개수 설정 (동적 레이아웃)
        /// </summary>
        private void SetupForWorkshopTab(WorkshopUI.WorkshopTabType tab)
        {
            switch (tab)
            {
                case WorkshopUI.WorkshopTabType.Enhancement:
                    // 강화: Before 1개, After 1개
                    EnableSlots(beforeCount: 1, afterCount: 1);
                    
                    if (beforeLabel != null)
                        beforeLabel.text = "강화 전";
                    if (afterLabel != null)
                        afterLabel.text = "강화 후";
                    
                    if (showDebugLogs)
                        Debug.Log("🔨 [BeforeAfterComparisonUI] 강화 탭 - Before 1개, After 1개");
                    break;
                    
                case WorkshopUI.WorkshopTabType.Fusion:
                    // 합성: Before 3~5개 (FusionRule 기반), After 1개
                    // TODO: FusionRule에서 동적으로 가져오기
                    EnableSlots(beforeCount: 3, afterCount: 1);
                    
                    if (beforeLabel != null)
                        beforeLabel.text = "재료";
                    if (afterLabel != null)
                        afterLabel.text = "합성 결과";
                    
                    if (showDebugLogs)
                        Debug.Log("⚗️ [BeforeAfterComparisonUI] 합성 탭 - Before 3개, After 1개");
                    break;
                    
                case WorkshopUI.WorkshopTabType.Dismantle:
                    // 분해: Before 여러 개, After 0개 (재료는 별도 UI에서 표시)
                    EnableSlots(beforeCount: 3, afterCount: 0);
                    
                    if (beforeLabel != null)
                        beforeLabel.text = "분해 대상";
                    if (afterLabel != null)
                        afterLabel.text = "분해 재료";
                    
                    if (showDebugLogs)
                        Debug.Log("🔧 [BeforeAfterComparisonUI] 분해 탭 - Before 3개, After 0개");
                    break;
            }
        }
        
        /// <summary>
        /// 슬롯 활성화/비활성화 (개수 기반)
        /// </summary>
        private void EnableSlots(int beforeCount, int afterCount)
        {
            // Before 슬롯
            if (beforeSlot1 != null)
                beforeSlot1.gameObject.SetActive(beforeCount >= 1);
            if (beforeSlot2 != null)
                beforeSlot2.gameObject.SetActive(beforeCount >= 2);
            if (beforeSlot3 != null)
                beforeSlot3.gameObject.SetActive(beforeCount >= 3);
            
            // After 슬롯
            if (afterSlot1 != null)
                afterSlot1.gameObject.SetActive(afterCount >= 1);
            if (afterSlot2 != null)
                afterSlot2.gameObject.SetActive(afterCount >= 2);
            if (afterSlot3 != null)
                afterSlot3.gameObject.SetActive(afterCount >= 3);
            
            // 화살표 표시 (After가 1개 이상일 때만)
            if (arrowIcon != null)
                arrowIcon.SetActive(afterCount > 0);
            
            if (showDebugLogs)
                Debug.Log($"🎯 [BeforeAfterComparisonUI] 슬롯 활성화 - Before: {beforeCount}개, After: {afterCount}개");
        }
        
        // ========================================
        // 🔨 강화 모드
        // ========================================
        
        /// <summary>
        /// 강화 모드로 설정
        /// </summary>
        public void SetupForEnhancement(ItemInstanceId itemId)
        {
            // ⭐ 활성화된 슬롯만 초기화 (SetupForWorkshopTab에서 설정한 레이아웃 유지)
            ClearActiveSlots();
            currentMode = WorkshopMode.Enhancement;
            
            var itemData = AccountDataManager.Instance.GetInstance(itemId);
            if (itemData == null)
            {
                Debug.LogError($"❌ [BeforeAfterComparisonUI] 아이템 데이터 없음: {itemId}");
                return;
            }
            
            var equipmentData = LoadEquipmentData(itemData.templateName);
            if (equipmentData == null)
            {
                Debug.LogError($"❌ [BeforeAfterComparisonUI] 장비 데이터 없음: {itemData.templateName}");
                return;
            }
            
            // 귀속 상태 확인
            bool isBound = AccountDataManager.Instance.IsBound(itemId);
            
            // ⭐ 제작 전: BeforeSlot1만 사용 (강화는 1:1)
            if (beforeSlot1 != null && beforeSlot1.gameObject.activeSelf)
            {
                beforeSlot1.SetEquipmentData(equipmentData, itemId);
                beforeSlot1.SetBindingStatus(isBound);
            }
            
            // ⭐ 제작 후: AfterSlot1만 사용 (강화 레벨 +1 프리뷰)
            if (afterSlot1 != null && afterSlot1.gameObject.activeSelf)
            {
                var previewData = CreateEnhancementPreview(equipmentData, itemData.enhancementLevel);
                afterSlot1.SetEquipmentData(previewData);
                afterSlot1.SetBindingStatus(isBound);
                afterSlot1.SetEnhancementLevel(itemData.enhancementLevel + 1); // 🆕 프리뷰 강화 레벨 표시
            }
            
            // 화살표는 SetupForWorkshopTab에서 이미 설정됨
            
            if (showDebugLogs)
                Debug.Log($"🔨 [BeforeAfterComparisonUI] 강화 모드 설정: {equipmentData.equipmentName} +{itemData.enhancementLevel} → +{itemData.enhancementLevel + 1}");
        }
        
        /// <summary>
        /// 활성화된 슬롯만 초기화 (레이아웃 유지)
        /// </summary>
        private void ClearActiveSlots()
        {
            // 제작 전 슬롯
            if (beforeSlot1 != null && beforeSlot1.gameObject.activeSelf)
            {
                beforeSlot1.ClearSlot();
            }
            
            if (beforeSlot2 != null && beforeSlot2.gameObject.activeSelf)
            {
                beforeSlot2.ClearSlot();
            }
            
            if (beforeSlot3 != null && beforeSlot3.gameObject.activeSelf)
            {
                beforeSlot3.ClearSlot();
            }
            
            // 제작 후 슬롯
            if (afterSlot1 != null && afterSlot1.gameObject.activeSelf)
            {
                afterSlot1.ClearSlot();
            }
            
            if (afterSlot2 != null && afterSlot2.gameObject.activeSelf)
            {
                afterSlot2.ClearSlot();
            }
            
            if (afterSlot3 != null && afterSlot3.gameObject.activeSelf)
            {
                afterSlot3.ClearSlot();
            }
            
            if (showDebugLogs)
                Debug.Log("🔄 [BeforeAfterComparisonUI] 활성화된 슬롯만 초기화 (레이아웃 유지)");
        }
        
        /// <summary>
        /// 강화 프리뷰 생성
        /// </summary>
        private EquipmentData CreateEnhancementPreview(EquipmentData original, int currentEnhancementLevel)
        {
            // ScriptableObject 복사 (임시 인스턴스)
            var preview = ScriptableObject.CreateInstance<EquipmentData>();
            
            // 원본 데이터 복사
            preview.equipmentName = original.equipmentName;
            preview.icon = original.icon;
            preview.itemGrade = original.itemGrade;
            preview.equipmentType = original.equipmentType;
            preview.usableClass = original.usableClass;
            preview.equipmentPrefab = original.equipmentPrefab;
            
            // 강화 레벨만 +1 (프리뷰이므로 임시)
            // Note: EquipmentData에 enhancementLevel 필드가 없으므로 별도 표시 필요
            // 실제로는 ItemInstanceData.enhancementLevel을 사용
            
            return preview;
        }
        
        // ========================================
        // 🔧 분해 모드
        // ========================================
        
        /// <summary>
        /// 분해 모드로 설정 (단일 아이템)
        /// </summary>
        public void SetupForDismantle(ItemInstanceId itemId)
        {
            ClearAll();
            currentMode = WorkshopMode.Dismantle;
            
            var itemData = AccountDataManager.Instance.GetInstance(itemId);
            if (itemData == null)
            {
                Debug.LogError($"❌ [BeforeAfterComparisonUI] 아이템 데이터 없음: {itemId}");
                return;
            }
            
            var equipmentData = LoadEquipmentData(itemData.templateName);
            if (equipmentData == null)
            {
                Debug.LogError($"❌ [BeforeAfterComparisonUI] 장비 데이터 없음: {itemData.templateName}");
                return;
            }
            
            // 귀속 상태 확인
            bool isBound = AccountDataManager.Instance.IsBound(itemId);
            
            // 제작 전: 분해할 아이템
            beforeSlot1.gameObject.SetActive(true);
            beforeSlot1.SetEquipmentData(equipmentData, itemId);
            beforeSlot1.SetBindingStatus(isBound);
            
            // 제작 후: 획득할 재료들 (프리뷰)
            var rewards = Systems.DismantleSystem.CalculateDismantleReward(itemId);
            
            // 재료 슬롯에 표시 (최대 3개)
            int slotIndex = 0;
            foreach (var reward in rewards)
            {
                if (slotIndex >= 3) break;
                
                InventorySlot targetSlot = null;
                if (slotIndex == 0) targetSlot = afterSlot1;
                else if (slotIndex == 1) targetSlot = afterSlot2;
                else if (slotIndex == 2) targetSlot = afterSlot3;
                
                if (targetSlot != null && reward.Value > 0)
                {
                    targetSlot.gameObject.SetActive(true);
                    
                    // MaterialStack 생성 (public 필드 직접 설정)
                    var materialStack = new MaterialStack
                    {
                        materialType = reward.Key,
                        count = reward.Value
                    };
                    targetSlot.SetupMaterial(materialStack);
                    
                    slotIndex++;
                }
            }
            
            // 화살표 활성화
            if (arrowIcon != null)
            {
                arrowIcon.SetActive(true);
            }
            
            // 라벨 설정
            if (beforeLabel != null) beforeLabel.text = "분해 대상";
            if (afterLabel != null) afterLabel.text = "획득 재료";
            
            if (showDebugLogs)
                Debug.Log($"🔧 [BeforeAfterComparisonUI] 분해 모드 설정: {equipmentData.equipmentName} → 재료 {rewards.Count}종류");
        }
        
        /// <summary>
        /// 분해 모드로 설정 (일괄 분해)
        /// </summary>
        public void SetupForBatchDismantle(List<ItemInstanceId> itemIds)
        {
            if (itemIds == null || itemIds.Count == 0)
            {
                Debug.LogWarning("⚠️ [BeforeAfterComparisonUI] 분해할 아이템 없음");
                return;
            }
            
            ClearAll();
            currentMode = WorkshopMode.Dismantle;
            
            // 대표 아이템 표시 (첫 번째 아이템)
            var firstItemData = AccountDataManager.Instance.GetInstance(itemIds[0]);
            if (firstItemData != null)
            {
                var equipmentData = LoadEquipmentData(firstItemData.templateName);
                if (equipmentData != null)
                {
                    bool isBound = AccountDataManager.Instance.IsBound(itemIds[0]);
                    
                    beforeSlot1.gameObject.SetActive(true);
                    beforeSlot1.SetEquipmentData(equipmentData, itemIds[0]);
                    beforeSlot1.SetBindingStatus(isBound);
                }
            }
            
            // 총 획득 재료 계산
            Dictionary<MaterialType, int> totalRewards = new Dictionary<MaterialType, int>();
            
            foreach (var itemId in itemIds)
            {
                var rewards = Systems.DismantleSystem.CalculateDismantleReward(itemId);
                
                foreach (var reward in rewards)
                {
                    if (totalRewards.ContainsKey(reward.Key))
                        totalRewards[reward.Key] += reward.Value;
                    else
                        totalRewards[reward.Key] = reward.Value;
                }
            }
            
            // 제작 후: 총 획득 재료 (최대 3개 슬롯)
            int slotIndex = 0;
            foreach (var reward in totalRewards)
            {
                if (slotIndex >= 3) break;
                
                InventorySlot targetSlot = null;
                if (slotIndex == 0) targetSlot = afterSlot1;
                else if (slotIndex == 1) targetSlot = afterSlot2;
                else if (slotIndex == 2) targetSlot = afterSlot3;
                
                if (targetSlot != null && reward.Value > 0)
                {
                    targetSlot.gameObject.SetActive(true);
                    
                    // MaterialStack 생성
                    var materialStack = new MaterialStack
                    {
                        materialType = reward.Key,
                        count = reward.Value
                    };
                    targetSlot.SetupMaterial(materialStack);
                    
                    slotIndex++;
                }
            }
            
            // 화살표 활성화
            if (arrowIcon != null)
            {
                arrowIcon.SetActive(true);
            }
            
            // 라벨 설정
            if (beforeLabel != null) beforeLabel.text = $"분해 대상 ({itemIds.Count}개)";
            if (afterLabel != null) afterLabel.text = "획득 재료";
            
            if (showDebugLogs)
                Debug.Log($"🔧 [BeforeAfterComparisonUI] 일괄 분해 모드 설정: {itemIds.Count}개 → 재료 {totalRewards.Count}종류");
        }
        
        // ========================================
        // ⚗️ 합성 모드
        // ========================================
        
        /// <summary>
        /// 합성 모드로 설정
        /// </summary>
        public void SetupForFusion(List<ItemInstanceId> materialIds)
        {
            if (materialIds == null || materialIds.Count < 2)
            {
                Debug.LogError("❌ [BeforeAfterComparisonUI] 합성 재료 부족 (최소 2개 필요)");
                return;
            }
            
            ClearAll();
            currentMode = WorkshopMode.Fusion;
            
            // 제작 전: 재료 아이템들 (최대 3개 표시)
            for (int i = 0; i < Mathf.Min(materialIds.Count, 3); i++)
            {
                var itemData = AccountDataManager.Instance.GetInstance(materialIds[i]);
                if (itemData == null) continue;
                
                var equipmentData = LoadEquipmentData(itemData.templateName);
                if (equipmentData == null) continue;
                
                bool isBound = AccountDataManager.Instance.IsBound(materialIds[i]);
                
                InventorySlot targetSlot = null;
                if (i == 0) targetSlot = beforeSlot1;
                else if (i == 1) targetSlot = beforeSlot2;
                else if (i == 2) targetSlot = beforeSlot3;
                
                if (targetSlot != null)
                {
                    targetSlot.gameObject.SetActive(true);
                    targetSlot.SetEquipmentData(equipmentData, materialIds[i]);
                    targetSlot.SetBindingStatus(isBound);
                }
            }
            
            // 제작 후: 합성 결과 아이템 (프리뷰)
            var firstMaterialData = AccountDataManager.Instance.GetInstance(materialIds[0]);
            if (firstMaterialData != null)
            {
                var originalEquipmentData = LoadEquipmentData(firstMaterialData.templateName);
                if (originalEquipmentData != null)
                {
                    var resultData = CreateFusionPreview(originalEquipmentData);
                    afterSlot1.gameObject.SetActive(true);
                    afterSlot1.SetEquipmentData(resultData);
                    
                    // 합성 결과는 귀속 해제됨
                    afterSlot1.SetBindingStatus(false);
                    
                    // 반짝임 효과
                    // afterSlot1.PlayGlowAnimation();
                }
            }
            
            // 화살표 활성화
            if (arrowIcon != null)
            {
                arrowIcon.SetActive(true);
            }
            
            // 라벨 설정
            if (beforeLabel != null) beforeLabel.text = $"재료 ({materialIds.Count}개)";
            if (afterLabel != null) afterLabel.text = "합성 결과";
            
            if (showDebugLogs)
                Debug.Log($"⚗️ [BeforeAfterComparisonUI] 합성 모드 설정: {materialIds.Count}개 → 1개");
        }
        
        /// <summary>
        /// 합성 프리뷰 생성 (한 등급 상승)
        /// </summary>
        private EquipmentData CreateFusionPreview(EquipmentData material)
        {
            // 한 등급 상위 아이템 찾기
            ItemGrade nextGrade = GetNextGrade(material.itemGrade);
            
            // 실제 구현에서는 FusionSystem에서 결과 아이템을 가져와야 함
            // 여기서는 간단히 등급만 변경한 프리뷰 생성
            var preview = ScriptableObject.CreateInstance<EquipmentData>();
            
            preview.equipmentName = material.equipmentName;
            preview.icon = material.icon;
            preview.itemGrade = nextGrade;
            preview.equipmentType = material.equipmentType;
            preview.usableClass = material.usableClass;
            preview.equipmentPrefab = material.equipmentPrefab;
            
            return preview;
        }
        
        /// <summary>
        /// 다음 등급 가져오기
        /// </summary>
        private ItemGrade GetNextGrade(ItemGrade current)
        {
            switch (current)
            {
                case ItemGrade.D: return ItemGrade.C;
                case ItemGrade.C: return ItemGrade.B;
                case ItemGrade.B: return ItemGrade.A;
                case ItemGrade.A: return ItemGrade.S;
                case ItemGrade.S: return ItemGrade.S; // 최고 등급
                default: return current;
            }
        }
        
        /// <summary>
        /// 현재 모드 가져오기
        /// </summary>
        public WorkshopMode GetCurrentMode()
        {
            return currentMode;
        }
        
        /// <summary>
        /// EquipmentData 로드 헬퍼 (ItemTemplateResolver 사용)
        /// </summary>
        private EquipmentData LoadEquipmentData(string templateName)
        {
            if (string.IsNullOrEmpty(templateName)) return null;
            
            // ⭐ ItemTemplateResolver 사용 (LobbyInventoryUI와 동일)
            var equipment = ItemTemplateResolver.Load(templateName);
            
            if (equipment == null && showDebugLogs)
            {
                Debug.LogWarning($"⚠️ [BeforeAfterComparisonUI] EquipmentData 로드 실패: {templateName}");
            }
            
            return equipment;
        }
    }
    
    /// <summary>
    /// 공방 모드
    /// </summary>
    public enum WorkshopMode
    {
        None,
        Enhancement,  // 강화
        Fusion,       // 합성
        Dismantle     // 분해
    }
}

