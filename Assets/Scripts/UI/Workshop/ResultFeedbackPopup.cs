using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI.Components;

namespace UI.Workshop
{
    /// <summary>
    /// 분해/합성 결과 표시 전용 팝업 (순수 View)
    /// 
    /// 책임:
    /// - 데이터 받아서 UI 표시만!
    /// - 팝업 열기/닫기
    /// - 자동 닫기 타이머
    /// 
    /// 책임 아님:
    /// - 판단 로직 (DismantleSystem/FusionSystem)
    /// - 실행 로직 (DismantleSystem/FusionSystem)
    /// - 보상 계산 (DismantleSystem/FusionSystem)
    /// </summary>
    public class ResultFeedbackPopup : MonoBehaviour
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // UI 참조
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        
        [Header("📦 팝업 구조")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private GameObject backgroundDim;
        
        [Header("🎨 타이틀/메시지")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        
        [Header("🎁 보상 표시 (분해용)")]
        [SerializeField] private GameObject rewardPanel;
        [SerializeField] private Transform rewardSlotsContainer;
        [SerializeField] private GameObject materialSlotPrefab; // InventorySlot 프리팹
        
        [Header("⭐ 결과 아이템 표시 (합성용)")]
        [SerializeField] private GameObject resultItemPanel;
        [SerializeField] private Image resultItemIcon;
        [SerializeField] private ItemIconGradeFrame resultItemGradeFrame; // ⭐ 등급 프레임
        [SerializeField] private TMP_Text resultItemNameText;
        [SerializeField] private TMP_Text resultItemGradeText;
        
        [Header("🔘 버튼")]
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text closeButtonText;
        
        [Header("⚙️ 설정")]
        [SerializeField] private float autoCloseDelay = 3f; // 3초 후 자동 닫기 (0이면 비활성화)
        [SerializeField] private bool enableDebugLogs = false;
        
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 내부 상태
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        
        private Coroutine autoCloseCoroutine;
        private List<GameObject> currentRewardSlots = new List<GameObject>();
        
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 초기화
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        
        void Awake()
        {
            // 버튼 이벤트 연결
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
            
            // 초기 상태: 팝업 숨김
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }
            
            if (backgroundDim != null)
            {
                backgroundDim.SetActive(false);
            }
            
            Log("ResultFeedbackPopup 초기화 완료");
        }
        
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Public API (순수 표시)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        
        /// <summary>
        /// 분해 결과 표시 (데이터만 받음)
        /// </summary>
        /// <param name="itemCount">분해한 아이템 개수</param>
        /// <param name="rewards">획득한 재료 (MaterialType → 개수)</param>
        public void ShowDismantleResult(int itemCount, Dictionary<MaterialType, int> rewards)
        {
            Log($"분해 결과 표시: {itemCount}개 아이템, {rewards.Count}종류 재료");
            
            // 1. 팝업 활성화
            ShowPopup();
            
            // 2. 타이틀/메시지 설정
            if (titleText != null)
            {
                titleText.text = "분해 완료!";
                titleText.color = new Color(0.2f, 0.8f, 0.2f); // 초록색
            }
            
            if (messageText != null)
            {
                messageText.text = $"{itemCount}개 아이템 분해 완료";
            }
            
            // 3. ResultItemPanel 숨김 (등급 프레임도 함께)
            if (resultItemPanel != null)
            {
                resultItemPanel.SetActive(false);
            }
            
            // ⭐ 등급 프레임 비활성화 (분해 모드에서는 사용 안 함)
            if (resultItemGradeFrame != null)
            {
                resultItemGradeFrame.gameObject.SetActive(false);
            }
            
            // 4. 재료 슬롯 동적 생성 (RewardPanel 활성화는 DisplayRewards()에서 처리)
            DisplayRewards(rewards);
            
            // 5. 자동 닫기 시작
            StartAutoClose();
        }
        
        /// <summary>
        /// 합성 결과 표시 - 단일 아이템 (데이터만 받음)
        /// </summary>
        /// <param name="resultItem">생성된 아이템 (EquipmentData)</param>
        /// <param name="enhancementLevel">강화 레벨 (기본 +0)</param>
        public void ShowFusionResult(EquipmentData resultItem, int enhancementLevel = 0)
        {
            if (resultItem == null)
            {
                Debug.LogError("[ResultFeedbackPopup] resultItem이 null입니다!");
                return;
            }
            
            Log($"합성 결과 표시: {resultItem.equipmentName} +{enhancementLevel}");
            
            // 1. 팝업 활성화
            ShowPopup();
            
            // 2. 타이틀/메시지 설정
            if (titleText != null)
            {
                titleText.text = "합성 성공!";
                titleText.color = new Color(1f, 0.84f, 0f); // 골드색
            }
            
            if (messageText != null)
            {
                messageText.text = $"{resultItem.itemGrade} 등급 아이템 획득!";
            }
            
            // 3. ResultItemPanel 표시, RewardPanel 숨김
            if (resultItemPanel != null)
            {
                resultItemPanel.SetActive(true);
            }
            
            if (rewardPanel != null)
            {
                rewardPanel.SetActive(false);
            }
            
            // 4. 아이템 정보 표시
            DisplayResultItem(resultItem, enhancementLevel);
            
            // 5. 자동 닫기 시작
            StartAutoClose();
        }
        
        /// <summary>
        /// 합성 결과 표시 - 다중 아이템 (데이터만 받음)
        /// ⭐ FusionUI용: 여러 등급 혼합 합성 시 사용
        /// </summary>
        /// <param name="fusionCount">합성 횟수</param>
        /// <param name="results">획득한 아이템 (templateName → 개수)</param>
        public void ShowFusionResult(int fusionCount, Dictionary<string, int> results)
        {
            Log($"합성 결과 표시 (다중): {fusionCount}회 합성, {results.Count}종류 아이템");
            
            // 1. 팝업 활성화
            ShowPopup();
            
            // 2. 타이틀/메시지 설정
            if (titleText != null)
            {
                titleText.text = "합성 성공!";
                titleText.color = new Color(1f, 0.84f, 0f); // 골드색
            }
            
            if (messageText != null)
            {
                messageText.text = $"{fusionCount}회 합성 완료";
            }
            
            // 3. ResultItemPanel 숨김, RewardPanel 표시
            if (resultItemPanel != null)
            {
                resultItemPanel.SetActive(false);
            }
            
            // ⭐ 등급 프레임 비활성화 (다중 아이템 모드에서는 사용 안 함)
            if (resultItemGradeFrame != null)
            {
                resultItemGradeFrame.gameObject.SetActive(false);
            }
            
            // 4. 결과 아이템 슬롯 동적 생성
            DisplayFusionResults(results);
            
            // 5. 자동 닫기 시작
            StartAutoClose();
        }
        
        /// <summary>
        /// 팝업 닫기
        /// </summary>
        public void Close()
        {
            Log("팝업 닫기");
            
            // 자동 닫기 코루틴 중지
            if (autoCloseCoroutine != null)
            {
                StopCoroutine(autoCloseCoroutine);
                autoCloseCoroutine = null;
            }
            
            // 팝업 비활성화
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }
            
            if (backgroundDim != null)
            {
                backgroundDim.SetActive(false);
            }
            
            // 재료 슬롯 정리
            ClearRewardSlots();
        }
        
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Private 헬퍼 메서드 (순수 UI 표시)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        
        /// <summary>
        /// 팝업 활성화 (공통)
        /// </summary>
        private void ShowPopup()
        {
            // 팝업 활성화
            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
            }
            
            if (backgroundDim != null)
            {
                backgroundDim.SetActive(true);
            }
            
            // Z-Order 최상위로 이동
            transform.SetAsLastSibling();
            
            Log("팝업 표시 (Z-Order 최상위)");
        }
        
        /// <summary>
        /// 재료 슬롯 표시 (분해 결과)
        /// </summary>
        private void DisplayRewards(Dictionary<MaterialType, int> rewards)
        {
            if (rewards == null || rewards.Count == 0)
            {
                Log("보상이 없습니다.");
                return;
            }
            
            // ⭐ 중요: RewardPanel 활성화 **후** 1프레임 대기한 다음 슬롯 생성
            // 이유: Unity UI Layout 시스템이 활성화 직후 재계산하면서 Image.enabled를 false로 설정하는 문제 방지
            if (rewardPanel != null && !rewardPanel.activeSelf)
            {
                Log("🔍 [DisplayRewards] RewardPanel 활성화 → 코루틴으로 1프레임 대기 후 슬롯 생성");
                rewardPanel.SetActive(true);
                StartCoroutine(DisplayRewardsDelayed(rewards));
                return;
            }
            
            // RewardPanel이 이미 활성화된 경우 즉시 생성
            DisplayRewardsImmediate(rewards);
        }
        
        /// <summary>
        /// 합성 결과 아이템 슬롯 표시 (합성 결과 - 다중)
        /// </summary>
        private void DisplayFusionResults(Dictionary<string, int> results)
        {
            if (results == null || results.Count == 0)
            {
                Log("합성 결과가 없습니다.");
                return;
            }
            
            // ⭐ 중요: RewardPanel 활성화 **후** 1프레임 대기한 다음 슬롯 생성
            if (rewardPanel != null && !rewardPanel.activeSelf)
            {
                Log("🔍 [DisplayFusionResults] RewardPanel 활성화 → 코루틴으로 1프레임 대기 후 슬롯 생성");
                rewardPanel.SetActive(true);
                StartCoroutine(DisplayFusionResultsDelayed(results));
                return;
            }
            
            // RewardPanel이 이미 활성화된 경우 즉시 생성
            DisplayFusionResultsImmediate(results);
        }
        
        /// <summary>
        /// ⭐ Unity UI 버그 해결: RewardPanel 활성화 후 1프레임 대기한 다음 슬롯 생성
        /// </summary>
        private IEnumerator DisplayRewardsDelayed(Dictionary<MaterialType, int> rewards)
        {
            // 1프레임 대기 (Unity UI Layout 계산 완료 대기)
            yield return null;
            
            Log("⏰ [DisplayRewards] 1프레임 대기 완료 → 재료 슬롯 생성 시작");
            DisplayRewardsImmediate(rewards);
        }
        
        /// <summary>
        /// ⭐ Unity UI 버그 해결: RewardPanel 활성화 후 1프레임 대기한 다음 슬롯 생성 (합성용)
        /// </summary>
        private IEnumerator DisplayFusionResultsDelayed(Dictionary<string, int> results)
        {
            // 1프레임 대기 (Unity UI Layout 계산 완료 대기)
            yield return null;
            
            Log("⏰ [DisplayFusionResults] 1프레임 대기 완료 → 결과 슬롯 생성 시작");
            DisplayFusionResultsImmediate(results);
        }
        
        /// <summary>
        /// 재료 슬롯 즉시 생성 (코루틴에서 호출)
        /// </summary>
        private void DisplayRewardsImmediate(Dictionary<MaterialType, int> rewards)
        {
            // 이전 슬롯 제거
            ClearRewardSlots();
            
            // 새 슬롯 생성
            foreach (var kvp in rewards)
            {
                CreateMaterialSlot(kvp.Key, kvp.Value);
            }
            
            Log($"재료 슬롯 {currentRewardSlots.Count}개 생성 완료");
        }
        
        /// <summary>
        /// 합성 결과 슬롯 즉시 생성 (코루틴에서 호출)
        /// </summary>
        private void DisplayFusionResultsImmediate(Dictionary<string, int> results)
        {
            // 이전 슬롯 제거
            ClearRewardSlots();
            
            // 새 슬롯 생성
            foreach (var kvp in results)
            {
                CreateEquipmentSlot(kvp.Key, kvp.Value);
            }
            
            Log($"결과 슬롯 {currentRewardSlots.Count}개 생성 완료");
        }
        
        /// <summary>
        /// 재료 슬롯 동적 생성
        /// </summary>
        private void CreateMaterialSlot(MaterialType materialType, int count)
        {
            if (materialSlotPrefab == null || rewardSlotsContainer == null)
            {
                Debug.LogWarning("[ResultFeedbackPopup] materialSlotPrefab 또는 rewardSlotsContainer가 null입니다!");
                return;
            }
            
            // InventorySlot 프리팹 인스턴스화
            GameObject slotObj = Instantiate(materialSlotPrefab, rewardSlotsContainer);
            
            // 슬롯 추적 (먼저 추가)
            currentRewardSlots.Add(slotObj);
            
            // ⭐ Instantiate() 후 1프레임 대기한 다음 SetupMaterial() 호출
            // 이유: 새로 생성된 슬롯이 Layout Group에 추가되고 재계산이 완료된 후 설정해야 Image가 정상 표시됨
            StartCoroutine(SetupMaterialSlotDelayed(slotObj, materialType, count));
            
            Log($"재료 슬롯 생성 예약: {materialType.GetDisplayName()} x{count}");
        }
        
        /// <summary>
        /// ⭐ Instantiate() 후 1프레임 대기한 다음 SetupMaterial() 호출
        /// </summary>
        private IEnumerator SetupMaterialSlotDelayed(GameObject slotObj, MaterialType materialType, int count)
        {
            // 1프레임 대기 (Layout Group 재계산 완료 대기)
            yield return null;
            
            if (slotObj == null)
            {
                Debug.LogError("[ResultFeedbackPopup] slotObj가 null입니다 (코루틴 실행 중 파괴됨)");
                yield break;
            }
            
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            
            if (slot != null)
            {
                // MaterialStack 설정
                var materialStack = new MaterialStack
                {
                    materialType = materialType,
                    count = count
                };
                
                slot.SetupMaterial(materialStack);
                
                Log($"재료 슬롯 설정 완료: {materialType.GetDisplayName()} x{count}");
            }
            else
            {
                Debug.LogError("[ResultFeedbackPopup] InventorySlot 컴포넌트를 찾을 수 없습니다!");
            }
        }
        
        /// <summary>
        /// 장비 슬롯 동적 생성 (합성 결과)
        /// </summary>
        private void CreateEquipmentSlot(string templateName, int count)
        {
            if (materialSlotPrefab == null || rewardSlotsContainer == null)
            {
                Debug.LogWarning("[ResultFeedbackPopup] materialSlotPrefab 또는 rewardSlotsContainer가 null입니다!");
                return;
            }
            
            // InventorySlot 프리팹 인스턴스화
            GameObject slotObj = Instantiate(materialSlotPrefab, rewardSlotsContainer);
            
            // 슬롯 추적 (먼저 추가)
            currentRewardSlots.Add(slotObj);
            
            // ⭐ Instantiate() 후 1프레임 대기한 다음 SetupEquipment() 호출
            StartCoroutine(SetupEquipmentSlotDelayed(slotObj, templateName, count));
            
            Log($"장비 슬롯 생성 예약: {templateName} x{count}");
        }
        
        /// <summary>
        /// ⭐ Instantiate() 후 1프레임 대기한 다음 장비 설정 (합성 결과)
        /// </summary>
        private IEnumerator SetupEquipmentSlotDelayed(GameObject slotObj, string templateName, int count)
        {
            // 1프레임 대기 (Layout Group 재계산 완료 대기)
            yield return null;
            
            if (slotObj == null)
            {
                Debug.LogError("[ResultFeedbackPopup] slotObj가 null입니다 (코루틴 실행 중 파괴됨)");
                yield break;
            }
            
            InventorySlot slot = slotObj.GetComponent<InventorySlot>();
            
            if (slot != null)
            {
                // 장비 데이터 로드
                EquipmentData equipData = ItemTemplateResolver.Load(templateName);
                
                if (equipData != null)
                {
                    // ⭐ 임시 ItemInstanceID (미리보기용)
                    ItemInstanceID previewId = default;
                    
                    slot.SetEquipmentData(equipData, previewId);
                    
                    // TODO: 개수 표시 (InventorySlot에 countText 추가 필요)
                    
                    Log($"장비 슬롯 설정 완료: {equipData.equipmentName} x{count}");
                }
                else
                {
                    Debug.LogError($"[ResultFeedbackPopup] 장비 데이터 로드 실패: {templateName}");
                }
            }
            else
            {
                Debug.LogError("[ResultFeedbackPopup] InventorySlot 컴포넌트를 찾을 수 없습니다!");
            }
        }
        
        /// <summary>
        /// 모든 재료 슬롯 제거
        /// </summary>
        private void ClearRewardSlots()
        {
            foreach (var slot in currentRewardSlots)
            {
                if (slot != null)
                {
                    Destroy(slot);
                }
            }
            
            currentRewardSlots.Clear();
            
            Log("모든 재료 슬롯 제거 완료");
        }
        
        /// <summary>
        /// 결과 아이템 표시 (합성 결과)
        /// </summary>
        private void DisplayResultItem(EquipmentData resultItem, int enhancementLevel)
        {
            if (resultItem == null) return;
            
            // 아이콘
            if (resultItemIcon != null && resultItem.icon != null)
            {
                resultItemIcon.sprite = resultItem.icon;
                resultItemIcon.enabled = true;
            }
            
            // ⭐ 등급 프레임 (ItemIconGradeFrame)
            if (resultItemGradeFrame != null)
            {
                resultItemGradeFrame.gameObject.SetActive(true);
                resultItemGradeFrame.SetGrade(resultItem.itemGrade);
                
                Log($"등급 프레임 설정: {resultItem.itemGrade}");
            }
            else
            {
                Debug.LogWarning("[ResultFeedbackPopup] resultItemGradeFrame이 null입니다! Inspector에서 연결해주세요.");
            }
            
            // 아이템 이름
            if (resultItemNameText != null)
            {
                string displayName = enhancementLevel > 0 
                    ? $"{resultItem.equipmentName} +{enhancementLevel}"
                    : resultItem.equipmentName;
                
                resultItemNameText.text = displayName;
            }
            
            // 등급 텍스트
            if (resultItemGradeText != null)
            {
                resultItemGradeText.text = $"{resultItem.itemGrade} 등급";
                resultItemGradeText.color = GetGradeColor(resultItem.itemGrade);
            }
            
            Log($"결과 아이템 표시: {resultItem.equipmentName} ({resultItem.itemGrade})");
        }
        
        /// <summary>
        /// 등급별 색상 반환
        /// </summary>
        private Color GetGradeColor(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.D => new Color(0.6f, 0.6f, 0.6f),    // 회색
                ItemGrade.C => new Color(0.4f, 1f, 0.4f),      // 연두색
                ItemGrade.B => new Color(0.3f, 0.7f, 1f),      // 파란색
                ItemGrade.A => new Color(0.8f, 0.3f, 1f),      // 보라색
                ItemGrade.S => new Color(1f, 0.84f, 0f),       // 골드
                ItemGrade.SS => new Color(1f, 0.5f, 0f),       // 주황색
                ItemGrade.EX => new Color(1f, 0.2f, 0.2f),     // 빨간색
                ItemGrade.TR => new Color(0f, 1f, 1f),         // 시안
                _ => Color.white
            };
        }
        
        /// <summary>
        /// 자동 닫기 시작
        /// </summary>
        private void StartAutoClose()
        {
            // 이전 코루틴 정지
            if (autoCloseCoroutine != null)
            {
                StopCoroutine(autoCloseCoroutine);
            }
            
            // 자동 닫기 활성화된 경우
            if (autoCloseDelay > 0)
            {
                autoCloseCoroutine = StartCoroutine(AutoCloseCoroutine());
                Log($"자동 닫기 시작 ({autoCloseDelay}초 후)");
            }
            else
            {
                Log("자동 닫기 비활성화 (수동으로만 닫기 가능)");
            }
        }
        
        /// <summary>
        /// 자동 닫기 코루틴
        /// </summary>
        private IEnumerator AutoCloseCoroutine()
        {
            yield return new WaitForSeconds(autoCloseDelay);
            
            Log("자동 닫기 실행");
            Close();
        }
        
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 이벤트 핸들러
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        
        /// <summary>
        /// 닫기 버튼 클릭
        /// </summary>
        private void OnCloseButtonClicked()
        {
            Log("닫기 버튼 클릭");
            Close();
        }
        
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 디버그
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        
        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[ResultFeedbackPopup] {message}");
            }
        }
    }
}

