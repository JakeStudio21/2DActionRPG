using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Systems;
using UI.Components;
using UI.Utils; // ⭐ StatFormatHelper

namespace UI.Popups
{
    /// <summary>
    /// 아이템 상세정보 팝업 컨텍스트
    /// - 컨텍스트에 따라 버튼 표시/동작 변경
    /// </summary>
    public enum ItemDetailContext
    {
        Inventory,      // 보관창고: "착용" 버튼
        Equipment,      // 장비창: "해제" 버튼
        Shop_Sell,      // 상점(판매): "판매" 버튼
        Shop_Buy,       // 상점(구매): "구매" 버튼
        ReadOnly,       // 캐릭터 정보창: 버튼 없음 (읽기 전용)
        Material        // 재료: 버튼 없음 (정보만 표시)
    }

    /// <summary>
    /// 아이템 상세정보 팝업
    /// - 모든 UI에서 공통 사용 (보관창고, 장비창, 상점, 캐릭터 정보)
    /// - 컨텍스트 기반 UI 자동 변경
    /// - 착용/해제/구매/판매 단일 버튼
    /// - 분해/강화/합성 진입 버튼 포함
    /// </summary>
    public class ItemDetailPopup : MonoBehaviour
    {
        #region Serialized Fields
        
        [Header("🎯 팝업 컨테이너")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private GameObject backgroundPanel; // 반투명 배경 (클릭 시 닫기)
        
        [Header("🎨 아이템 정보 UI")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private ItemIconGradeFrame itemIconGradeFrame; // ⭐ 등급별 배경 색상
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemGradeText;
        [SerializeField] private TextMeshProUGUI stat1Text;
        [SerializeField] private TextMeshProUGUI stat2Text;
        [SerializeField] private TextMeshProUGUI stat3Text;
        
        [Header("💰 가격 표시 (상점 전용)")]
        [SerializeField] private GameObject priceGroup;              // 가격 표시 그룹
        [SerializeField] private TextMeshProUGUI priceLabelText;     // "판매 가격:" 라벨
        [SerializeField] private TextMeshProUGUI priceValueText;     // "500" 가격 숫자
        [SerializeField] private TextMeshProUGUI priceUnitText;      // "골드" 단위
        
        [Header("🔘 기본 액션 버튼")]
        [SerializeField] private GameObject primaryActionGroup;      // 기본 액션 버튼 그룹
        [SerializeField] private Button primaryActionButton;         // 단일 액션 버튼 (착용/해제/판매/구매)
        [SerializeField] private TextMeshProUGUI primaryActionButtonText; // 버튼 텍스트
        [SerializeField] private TextMeshProUGUI warningText;        // 경고 메시지 텍스트 (클래스 불일치 등)
        
        [Header("📦 일괄 액션 버튼 (상점 전용)")]
        [SerializeField] private GameObject batchActionGroup;        // 일괄 액션 버튼 그룹
        [SerializeField] private Button batchSellButton;             // "일괄판매 추가" 버튼
        [SerializeField] private TextMeshProUGUI batchSellButtonText; // 버튼 텍스트
        
        [Header("⚡ 고급 액션 버튼")]
        [SerializeField] private GameObject advancedActionGroup;     // 고급 액션 버튼 그룹
        [SerializeField] private Button dismantleButton;             // 분해 버튼
        [SerializeField] private Button enhanceButton;               // 강화 버튼
        [SerializeField] private Button fusionButton;                // 합성 버튼
        
        [Header("🔧 제어 버튼")]
        [SerializeField] private Button closeButton;                 // 닫기 버튼 (X)
        [SerializeField] private Button backgroundButton;            // 배경 클릭 버튼 (닫기)
        
        [Header("📊 디버그")]
        [SerializeField] private bool showDebugLogs = false; // ⭐ 프로덕션 기본값
        
        #endregion
        
        #region Private Fields
        
        // 현재 상태
        private EquipmentData currentItem;
        private MaterialType? currentMaterial; // 📦 재료 모드용
        private ItemDetailContext currentContext;
        private int currentSlotIndex = -1; // 슬롯 인덱스 (인벤토리/장비창용)
        private ItemInstanceID currentItemInstanceID; // V2: 아이템 인스턴스 ID (귀속 체크용)
        
        // ⭐ 원본 UI 색상 저장 (복구용)
        private Color originalPrimaryButtonTextColor;
        private string originalPrimaryButtonText = "착용";
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // 초기 상태: 팝업 숨김
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }
            
            // 경고 메시지 초기 숨김
            if (warningText != null)
            {
                warningText.gameObject.SetActive(false);
            }
            
            // ⭐ 원본 버튼 텍스트 색상 저장 (복구용)
            if (primaryActionButtonText != null)
            {
                originalPrimaryButtonTextColor = primaryActionButtonText.color;
                originalPrimaryButtonText = primaryActionButtonText.text;
                Log($"✅ [ItemDetailPopup] 원본 버튼 색상 저장: {originalPrimaryButtonTextColor}");
            }
            
            // 버튼 이벤트 연결
            SetupButtonEvents();
            
            Log("🎯 [ItemDetailPopup] Awake 완료");
        }
        
        private void OnEnable()
        {
            // PlayerDataManager 이벤트 구독
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnSlotClicked += OnSlotClickedHandler;
                
                Log("✅ [ItemDetailPopup] 이벤트 구독 완료");
            }
        }
        
        private void OnDisable()
        {
            // PlayerDataManager 이벤트 구독 해제
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnSlotClicked -= OnSlotClickedHandler;
                
                Log("❌ [ItemDetailPopup] 이벤트 구독 해제");
            }
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// 버튼 이벤트 설정
        /// </summary>
        private void SetupButtonEvents()
        {
            // 기본 액션 버튼
            if (primaryActionButton != null)
            {
                primaryActionButton.onClick.RemoveAllListeners();
                primaryActionButton.onClick.AddListener(OnPrimaryActionButtonClicked);
            }
            
            // ⭐ 일괄 액션 버튼 (신규)
            if (batchSellButton != null)
            {
                batchSellButton.onClick.RemoveAllListeners();
                batchSellButton.onClick.AddListener(OnBatchSellButtonClicked);
                Log("✅ [ItemDetailPopup] 일괄판매 버튼 이벤트 연결");
            }
            
            // 고급 액션 버튼들
            if (dismantleButton != null)
            {
                dismantleButton.onClick.RemoveAllListeners();
                dismantleButton.onClick.AddListener(OnDismantleButtonClicked);
            }
            
            if (enhanceButton != null)
            {
                enhanceButton.onClick.RemoveAllListeners();
                enhanceButton.onClick.AddListener(OnEnhanceButtonClicked);
            }
            
            if (fusionButton != null)
            {
                fusionButton.onClick.RemoveAllListeners();
                fusionButton.onClick.AddListener(OnFusionButtonClicked);
            }
            
            // 닫기 버튼
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }
            
            // 배경 클릭 시 닫기
            if (backgroundButton != null)
            {
                backgroundButton.onClick.RemoveAllListeners();
                backgroundButton.onClick.AddListener(Hide);
                
                Log("✅ [ItemDetailPopup] 배경 클릭 닫기 기능 활성화");
            }
            else
            {
                Debug.LogWarning("⚠️ [ItemDetailPopup] backgroundButton이 null입니다. Inspector에서 할당해주세요.");
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// 팝업 표시 (컨텍스트 기반)
        /// </summary>
        public void Show(EquipmentData data, ItemDetailContext context, int slotIndex = -1, ItemInstanceID instanceId = default)
        {
            if (data == null)
            {
                Debug.LogError("❌ [ItemDetailPopup] EquipmentData가 null입니다!");
                return;
            }
            
            currentItem = data;
            currentContext = context;
            currentSlotIndex = slotIndex;
            currentItemInstanceID = instanceId; // V2: 아이템 인스턴스 ID 저장
            
            // UI 업데이트
            UpdateItemInfo(data);
            UpdateButtonsByContext();
            
            // ⭐ Background와 PopupPanel 모두 활성화
            if (backgroundPanel != null)
            {
                backgroundPanel.SetActive(true);
            }
            
            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
            }
            
            Log($"🎯 [ItemDetailPopup] 팝업 열기: {data.equipmentName} (컨텍스트: {context}, ID: {(!instanceId.IsEmpty ? instanceId.Value.Substring(0, 8) + "..." : "없음")})");
        }
        
        /// <summary>
        /// 📦 재료 상세 팝업 표시
        /// </summary>
        public void ShowMaterialDetail(MaterialType materialType)
        {
            var materialData = MaterialDatabase.Instance.GetData(materialType);
            if (materialData == null)
            {
                Debug.LogError($"❌ [ItemDetailPopup] MaterialData를 찾을 수 없습니다: {materialType}");
                return;
            }
            
            // 현재 상태 설정
            currentItem = null;
            currentMaterial = materialType;
            currentContext = ItemDetailContext.Material;
            currentSlotIndex = -1;
            currentItemInstanceID = default;
            
            // UI 업데이트
            UpdateMaterialInfo(materialData);
            UpdateButtonsByContext();
            
            // ⭐ Background와 PopupPanel 모두 활성화
            if (backgroundPanel != null)
            {
                backgroundPanel.SetActive(true);
            }
            
            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
            }
            
            Log($"📦 [ItemDetailPopup] 재료 팝업 열기: {materialData.displayName}");
        }
        
        /// <summary>
        /// 팝업 닫기
        /// </summary>
        public void Hide()
        {
            // ⭐ 실행 중인 모든 코루틴 정지 (경고 메시지 타이머 등)
            StopAllCoroutines();
            
            // ⭐ UI 상태 즉시 복구 (코루틴 대기 없이 즉시 리셋)
            ResetWarningUI();
            
            // ⭐ 가격 표시 숨김
            if (priceGroup != null)
            {
                priceGroup.SetActive(false);
            }
            
            // ⭐ 일괄판매 버튼 숨김
            if (batchActionGroup != null)
            {
                batchActionGroup.SetActive(false);
            }
            
            // ⭐ PopupPanel과 Background 모두 비활성화
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
            }
            
            if (backgroundPanel != null)
            {
                backgroundPanel.SetActive(false);
            }
            
            // 상태 초기화
            currentItem = null;
            currentMaterial = null; // 📦 재료 상태 초기화
            currentSlotIndex = -1;
            currentItemInstanceID = default; // V2: ItemInstanceID 초기화
            
            Log("🔒 [ItemDetailPopup] 팝업 닫기 (코루틴 정지 + UI 리셋 + 가격/일괄판매 숨김)");
        }
        
        #endregion
        
        #region Private Methods - UI Update
        
        /// <summary>
        /// ⭐ 수정: 아이템 정보 UI 업데이트 (동적 스탯 포함)
        /// </summary>
        private void UpdateItemInfo(EquipmentData data)
        {
            // 아이템 아이콘
            if (itemIcon != null && data.icon != null)
            {
                itemIcon.sprite = data.icon;
                itemIcon.color = Color.white;
                itemIcon.gameObject.SetActive(true);
            }
            
            // 아이템 이름
            if (itemNameText != null)
            {
                itemNameText.text = data.equipmentName;
            }
            
            // 아이템 등급
            if (itemGradeText != null)
            {
                itemGradeText.text = $"등급: {data.itemGrade}";
            }
            
            // ⭐ 등급별 배경 색상 적용
            if (itemIconGradeFrame != null)
            {
                itemIconGradeFrame.SetGrade(data.itemGrade);
            }
            
            // ⭐ 동적 스탯 표시 (V2 시스템)
            UpdateDynamicStats(data);
        }
        
        /// <summary>
        /// ⭐ 수정: 동적 스탯 표시 (주옵션 + 부옵션)
        /// - 상점 전시용: ShopItemPool 캐시 우선 (Lazy Generation)
        /// - 보관창고용: AccountDataManager (저장된 인스턴스)
        /// </summary>
        private void UpdateDynamicStats(EquipmentData data)
        {
            // V2: ItemInstanceID가 있으면 동적 스탯 표시
            if (!currentItemInstanceID.IsEmpty)
            {
                EquipmentInstance instance = null;
                
                // 1순위: ShopItemPool 캐시 확인 (상점 전시용)
                if (currentContext == ItemDetailContext.Shop_Buy && ShopController.Instance?.ItemPool != null)
                {
                    instance = ShopController.Instance.ItemPool.GetOrCreateDynamicStats(currentItemInstanceID);
                    
                    if (instance != null)
                    {
                        Log($"🎲 [ItemDetailPopup] 상점 전시용 동적 스탯 로드 (캐시): {data.equipmentName}");
                        UpdateDynamicStatsFromInstance(instance);
                        return;
                    }
                }
                
                // 2순위: AccountDataManager (보관창고/장비창 등)
                instance = AccountDataManager.Instance?.CreateEquipmentInstance(currentItemInstanceID);
                
                if (instance != null)
                {
                    Log($"📦 [ItemDetailPopup] 보관창고 동적 스탯 로드: {data.equipmentName}");
                    UpdateDynamicStatsFromInstance(instance);
                    return; // 동적 스탯 표시 완료
                }
                else
                {
                    Debug.LogWarning($"⚠️ [ItemDetailPopup] EquipmentInstance 생성 실패: {currentItemInstanceID.Value}");
                }
            }
            
            // Legacy: ItemInstanceID가 없으면 기존 방식 (하드코딩된 스탯)
            UpdateLegacyStats(data);
        }
        
        /// <summary>
        /// 🆕 EquipmentInstance로부터 동적 스탯 표시
        /// </summary>
        private void UpdateDynamicStatsFromInstance(EquipmentInstance instance)
        {
            // ⭐ Stage 5: 주옵션 표시 (강화 증가분 포함)
            if (stat1Text != null)
            {
                // StatPoolDataLoader에서 MainStat 타입 가져오기
                EStatType mainStatType = GetMainStatType(instance.EquipmentData);
                
                if (mainStatType != EStatType.None && instance.finalMainStatValue > 0)
                {
                    // 기본 포맷 (주황색)
                    string mainStatText = StatFormatHelper.FormatMainStat(mainStatType, instance.finalMainStatValue);
                    
                    // 강화 증가분 계산 및 표시
                    if (instance.enhanceLevel > 0)
                    {
                        // 곡선 그룹 ID 가져오기
                        string curveGroupId = !string.IsNullOrEmpty(instance.EquipmentData.enhancementCurveGroupId) 
                            ? instance.EquipmentData.enhancementCurveGroupId 
                            : "CURVE_STANDARD";
                        
                        // 누적 증가율 가져오기
                        float totalBonusPercent = EnhancementSystem.GetTotalStatBonus(curveGroupId, instance.enhanceLevel);
                        
                        if (totalBonusPercent > 0)
                        {
                            // 기본값 역산: finalValue = baseValue × (1 + bonus%)
                            // → baseValue = finalValue / (1 + bonus%)
                            float baseValue = instance.finalMainStatValue / (1f + (totalBonusPercent / 100f));
                            float bonusValue = instance.finalMainStatValue - baseValue;
                            
                            // 강화 증가분 표시 (녹색)
                            mainStatText += $" <color=#4CAF50>(+{bonusValue:F1})</color>";
                        }
                    }
                    
                    stat1Text.text = mainStatText;
                }
                else
                {
                    stat1Text.text = "";
                }
            }
            
            // 부옵션 표시 (stat2Text에 전체 표시, 멀티라인)
            if (stat2Text != null)
            {
                if (instance.randomSubStats != null && instance.randomSubStats.Count > 0)
                {
                    stat2Text.text = StatFormatHelper.FormatSubStats(instance.randomSubStats);
                }
                else
                {
                    stat2Text.text = "";
                }
            }
            
            // stat3Text는 비워둠 (또는 강화 레벨 표시)
            if (stat3Text != null)
            {
                if (instance.enhanceLevel > 0)
                {
                    stat3Text.text = $"+{instance.enhanceLevel}"; // ⭐ 하얀색, 강화 수치만
                }
                else
                {
                    stat3Text.text = "";
                }
            }
        }
        
        /// <summary>
        /// 🆕 Legacy 스탯 표시 (하위 호환)
        /// </summary>
        private void UpdateLegacyStats(EquipmentData data)
        {
            if (stat1Text != null)
            {
                // 무기면 공격력, 방어구면 방어력
                if (data.IsWeapon)
                    stat1Text.text = $"공격력: +{data.attackDamage}";
                else
                    stat1Text.text = $"방어력: +{data.defenseBonus}";
            }
            
            if (stat2Text != null)
            {
                stat2Text.text = $"체력: +{data.healthBonus}";
            }
            
            if (stat3Text != null)
            {
                stat3Text.text = $"이동속도: +{data.speedBonus:F1}";
            }
        }
        
        /// <summary>
        /// 🆕 주옵션 스탯 타입 가져오기 (StatPoolDataLoader 또는 EquipmentData 기반)
        /// </summary>
        private EStatType GetMainStatType(EquipmentData data)
        {
            // 1순위: StatPoolDataLoader에서 가져오기
            string poolId = data.equipmentSlot.ToString();
            if (StatPoolDataLoader.TryGetStatPool(poolId, out EStatType mainStat, out _))
            {
                return mainStat;
            }
            
            // 2순위: EquipmentData의 equipmentType/equipmentSlot 기반 추론
            return InferMainStatType(data);
        }
        
        /// <summary>
        /// 🆕 주옵션 스탯 타입 추론 (Fallback)
        /// </summary>
        private EStatType InferMainStatType(EquipmentData data)
        {
            // 무기: 공격력
            if (data.equipmentType == EquipmentType.Weapon)
            {
                return EStatType.ATK_FLAT;
            }
            
            // 방어구: 방어력
            if (data.equipmentType == EquipmentType.Armor)
            {
                return EStatType.DEF_FLAT;
            }
            
            // 악세서리: 체력
            if (data.equipmentType == EquipmentType.Accessory)
            {
                return EStatType.HP_FLAT;
            }
            
            // 기본값: 공격력
            return EStatType.ATK_FLAT;
        }
        
        /// <summary>
        /// 📦 재료 정보 UI 업데이트
        /// </summary>
        private void UpdateMaterialInfo(MaterialData data)
        {
            // 재료 아이콘
            if (itemIcon != null && data.icon != null)
            {
                itemIcon.sprite = data.icon;
                itemIcon.color = Color.white;
                itemIcon.gameObject.SetActive(true);
            }
            
            // 재료 이름
            if (itemNameText != null)
            {
                itemNameText.text = data.displayName;
            }
            
            // 재료 등급 (MaterialRarity → 한글 표시)
            if (itemGradeText != null)
            {
                string rarityText = data.rarity switch
                {
                    MaterialRarity.Common => "일반",
                    MaterialRarity.Uncommon => "고급",
                    MaterialRarity.Rare => "희귀",
                    MaterialRarity.Epic => "영웅",
                    MaterialRarity.Legendary => "전설",
                    _ => "알 수 없음"
                };
                itemGradeText.text = $"등급: {rarityText}";
            }
            
            // ⭐ 등급별 배경 색상 적용 (MaterialRarity → ItemGrade 매핑)
            if (itemIconGradeFrame != null)
            {
                ItemGrade mappedGrade = data.rarity switch
                {
                    MaterialRarity.Common => ItemGrade.C,
                    MaterialRarity.Uncommon => ItemGrade.B,
                    MaterialRarity.Rare => ItemGrade.A,
                    MaterialRarity.Epic => ItemGrade.S,
                    MaterialRarity.Legendary => ItemGrade.EX,
                    _ => ItemGrade.D
                };
                itemIconGradeFrame.SetGrade(mappedGrade);
            }
            
            // 보유 수량
            if (stat1Text != null)
            {
                int count = AccountDataManager.Instance.GetMaterialCount(data.materialType);
                stat1Text.text = $"보유: {count}개";
            }
            
            // 사용처
            if (stat2Text != null)
            {
                stat2Text.text = $"사용처: {data.usageHint}";
            }
            
            // 획득처
            if (stat3Text != null)
            {
                stat3Text.text = $"획득처: {data.obtainHint}";
            }
        }
        
        /// <summary>
        /// 컨텍스트별 버튼 표시/숨김 및 텍스트 변경
        /// </summary>
        private void UpdateButtonsByContext()
        {
            switch (currentContext)
            {
                case ItemDetailContext.Inventory:
                    // 보관창고: "착용" + 고급 액션 표시
                    SetPrimaryButtonActive(true, "착용");
                    SetBatchActionActive(false);        // ⭐ 일괄판매 숨김
                    SetAdvancedButtonsActive(true);
                    UpdatePriceDisplay(false);          // ⭐ 가격 숨김
                    Log("🎒 [ItemDetailPopup] 보관창고 모드: 착용 + 고급 액션");
                    break;
                    
                case ItemDetailContext.Equipment:
                    // 장비창: "해제" + 고급 액션 표시
                    SetPrimaryButtonActive(true, "해제");
                    SetBatchActionActive(false);        // ⭐ 일괄판매 숨김
                    SetAdvancedButtonsActive(true);
                    UpdatePriceDisplay(false);          // ⭐ 가격 숨김
                    Log("🎒 [ItemDetailPopup] 장비창 모드: 해제 + 고급 액션");
                    break;
                    
                case ItemDetailContext.Shop_Sell:
                    // 상점(판매): "판매" + "일괄판매" + 가격 표시
                    SetPrimaryButtonActive(true, "판매");
                    SetBatchActionActive(true, "일괄판매");  // ⭐ 일괄판매 표시
                    SetAdvancedButtonsActive(false);
                    UpdatePriceDisplay(true, currentItem);        // ⭐ 가격 표시
                    Log("🏪 [ItemDetailPopup] 상점(판매) 모드: 판매 + 일괄판매 + 가격 표시");
                    break;
                    
                case ItemDetailContext.Shop_Buy:
                    // 상점(구매): "구매" + 구매가 표시
                    SetPrimaryButtonActive(true, "구매");
                    SetBatchActionActive(false);        // ⭐ 일괄판매 숨김
                    SetAdvancedButtonsActive(false);
                    UpdatePriceDisplay(true, currentItem, true);  // ⭐ 구매가 표시 (isBuyPrice = true)
                    Log("🏪 [ItemDetailPopup] 상점(구매) 모드: 구매 + 구매가 표시");
                    break;
                    
                case ItemDetailContext.ReadOnly:
                    // 캐릭터 정보창: 모든 버튼 숨김 (읽기 전용)
                    SetPrimaryButtonActive(false, "");
                    SetBatchActionActive(false);        // ⭐ 일괄판매 숨김
                    SetAdvancedButtonsActive(false);
                    UpdatePriceDisplay(false);          // ⭐ 가격 숨김
                    Log("📖 [ItemDetailPopup] 읽기 전용 모드: 정보만 표시");
                    break;
                    
                case ItemDetailContext.Material:
                    // 📦 재료: 모든 버튼 숨김 (정보만 표시)
                    SetPrimaryButtonActive(false, "");
                    SetBatchActionActive(false);
                    SetAdvancedButtonsActive(false);
                    UpdatePriceDisplay(false);
                    Log("📦 [ItemDetailPopup] 재료 모드: 정보만 표시");
                    break;
            }
        }
        
        /// <summary>
        /// 기본 액션 버튼 활성화/비활성화
        /// </summary>
        private void SetPrimaryButtonActive(bool active, string buttonText)
        {
            if (primaryActionGroup != null)
            {
                primaryActionGroup.SetActive(active);
            }
            
            if (primaryActionButtonText != null && active)
            {
                primaryActionButtonText.text = buttonText;
            }
        }
        
        /// <summary>
        /// 고급 액션 버튼 그룹 활성화/비활성화
        /// </summary>
        private void SetAdvancedButtonsActive(bool active)
        {
            if (advancedActionGroup != null)
            {
                advancedActionGroup.SetActive(active);
            }
        }
        
        /// <summary>
        /// ⭐ 일괄 액션 버튼 활성화/비활성화 (상점 전용)
        /// </summary>
        private void SetBatchActionActive(bool active, string buttonText = "")
        {
            if (batchActionGroup != null)
            {
                batchActionGroup.SetActive(active);
            }
            
            if (batchSellButtonText != null && active)
            {
                batchSellButtonText.text = buttonText;
            }
        }
        
        #endregion
        
        #region Event Handlers - Player Data Manager
        
        /// <summary>
        /// 슬롯 클릭 이벤트 핸들러
        /// V2: ItemInstanceID 추가
        /// </summary>
        private void OnSlotClickedHandler(EquipmentData equipmentData, int slotIndex, ItemInstanceID instanceId)
        {
            // 빈 슬롯 클릭 시 무시
            if (equipmentData == null)
            {
                Log("⚠️ [ItemDetailPopup] 빈 슬롯 클릭 - 무시");
                return;
            }
            
            // 컨텍스트 자동 감지
            ItemDetailContext detectedContext = DetectContext();
            
            // 팝업 표시 (V2: ItemInstanceID 전달)
            Show(equipmentData, detectedContext, slotIndex, instanceId);
        }
        
        /// <summary>
        /// 현재 활성화된 UI에 따라 컨텍스트 자동 감지
        /// </summary>
        private ItemDetailContext DetectContext()
        {
            // TODO: 현재 어떤 UI가 열려있는지 감지
            // 현재는 기본값으로 Inventory 반환
            // 추후 LobbyPanelManager 등과 연동하여 현재 활성 패널 감지
            
            // 임시: 항상 Inventory로 처리
            return ItemDetailContext.Inventory;
        }
        
        #endregion
        
        #region Event Handlers - Button Clicks
        
        /// <summary>
        /// 기본 액션 버튼 클릭 (착용/해제/구매/판매)
        /// </summary>
        private void OnPrimaryActionButtonClicked()
        {
            if (currentItem == null)
            {
                Debug.LogError("❌ [ItemDetailPopup] currentItem이 null입니다!");
                return;
            }
            
            // ⭐ 각 액션 메서드가 bool을 반환하여 팝업 닫기 여부 결정
            bool shouldClose = true;
            
            switch (currentContext)
            {
                case ItemDetailContext.Inventory:
                    // 착용 로직 (귀속 팝업이 열리면 false 반환)
                    shouldClose = EquipItem();
                    break;
                    
                case ItemDetailContext.Equipment:
                    // 해제 로직
                    UnequipItem();
                    break;
                    
                case ItemDetailContext.Shop_Sell:
                    // ⭐ 판매 로직 (코루틴에서 팝업 닫기 처리)
                    SellItem();
                    shouldClose = false; // 코루틴에서 자동 닫기
                    break;
                    
                case ItemDetailContext.Shop_Buy:
                    // ⭐ 구매 로직 (코루틴에서 팝업 닫기 처리)
                    BuyItem();
                    shouldClose = false; // 코루틴에서 자동 닫기
                    break;
                    
                case ItemDetailContext.ReadOnly:
                    // 읽기 전용 모드에서는 버튼이 없으므로 도달하지 않음
                    break;
                    
                case ItemDetailContext.Material:
                    // 📦 재료 모드에서는 버튼이 없으므로 도달하지 않음
                    break;
            }
            
            // ⭐ 귀속 팝업이 열린 경우는 닫지 않음 (콜백에서 처리)
            if (shouldClose)
            {
                Hide();
            }
        }
        
        /// <summary>
        /// 분해 버튼 클릭
        /// </summary>
        private void OnDismantleButtonClicked()
        {
            Log($"🔨 [ItemDetailPopup] 분해 버튼 클릭: {currentItem?.equipmentName}");
            
            // 현재 팝업 닫기
            Hide();
            
            // TODO: DismantleUI 팝업 열기 (Phase 5 UI 구현 시)
            Debug.Log($"[TODO] 분해 UI 열기: {currentItem?.equipmentName}");
        }
        
        /// <summary>
        /// 강화 버튼 클릭
        /// </summary>
        private void OnEnhanceButtonClicked()
        {
            Log($"⚡ [ItemDetailPopup] 강화 버튼 클릭: {currentItem?.equipmentName}");
            
            // 현재 팝업 닫기
            Hide();
            
            // TODO: EnhancementUI 팝업 열기 (Phase 7 UI 구현 시)
            Debug.Log($"[TODO] 강화 UI 열기: {currentItem?.equipmentName}");
        }
        
        /// <summary>
        /// 합성 버튼 클릭
        /// </summary>
        private void OnFusionButtonClicked()
        {
            Log($"🔥 [ItemDetailPopup] 합성 버튼 클릭: {currentItem?.equipmentName}");
            
            // 현재 팝업 닫기
            Hide();
            
            // TODO: FusionUI 팝업 열기 (Phase 6 UI 구현 시)
            Debug.Log($"[TODO] 합성 UI 열기: {currentItem?.equipmentName}");
        }
        
        /// <summary>
        /// ⭐ 일괄판매 추가 버튼 클릭
        /// </summary>
        private void OnBatchSellButtonClicked()
        {
            if (currentItem == null || currentItemInstanceID.IsEmpty)
            {
                Debug.LogError("❌ [ItemDetailPopup] 추가할 아이템이 없습니다!");
                return;
            }
            
            // 판매 가능 여부 체크
            if (!currentItem.isTradable)
            {
                Log($"⚠️ {currentItem.equipmentName}은(는) 판매할 수 없는 아이템입니다!");
                
                // ⭐ 판매 불가 메시지 표시 (3초 후 사라짐, 팝업은 유지)
                StartCoroutine(ShowTransactionResult(false, $"{currentItem.equipmentName}은(는) 판매할 수 없습니다."));
                return;
            }
            
            Log($"📦 [ItemDetailPopup] 일괄판매 리스트에 추가: {currentItem.equipmentName} (ID: {currentItemInstanceID.Value.Substring(0, 8)}...)");
            
            // TODO: BatchSellUI 열기 및 아이템 추가 (미래 구현)
            // if (BatchSellUI.Instance != null)
            // {
            //     bool added = BatchSellUI.Instance.AddItem(currentItem, currentItemInstanceID);
            //     if (added)
            //     {
            //         // 리스트 추가 성공 메시지 표시 (1초 후 팝업 자동 닫기)
            //         StartCoroutine(ShowTransactionResult(true, "일괄판매 리스트에 추가됨!"));
            //         BatchSellUI.Instance.Show();
            //     }
            //     else
            //     {
            //         // 리스트 추가 실패 (이미 추가됨 등)
            //         StartCoroutine(ShowTransactionResult(false, "이미 리스트에 추가된 아이템입니다."));
            //     }
            // }
            // else
            // {
            //     Debug.LogError("❌ [ItemDetailPopup] BatchSellUI.Instance를 찾을 수 없습니다!");
            // }
            
            // ⭐ 임시: TODO 구현 전까지는 메시지만 표시하고 팝업 닫기
            Debug.Log($"[TODO] BatchSellUI에 추가: {currentItem.equipmentName}");
            StartCoroutine(ShowTransactionResult(true, "일괄판매 리스트에 추가됨! (TODO)"));
        }
        
        #endregion
        
        #region Action Methods
        
        /// <summary>
        /// 아이템 착용
        /// </summary>
        /// <returns>팝업을 닫을지 여부 (false = 귀속 팝업 대기 중)</returns>
        private bool EquipItem()
        {
            if (PlayerDataManager.Instance == null)
            {
                Debug.LogError("❌ [ItemDetailPopup] PlayerDataManager가 null입니다!");
                return true; // 에러 발생 시 팝업 닫기
            }
            
            if (currentItem == null)
            {
                Debug.LogError("❌ [ItemDetailPopup] currentItem이 null입니다!");
                return true; // 에러 발생 시 팝업 닫기
            }
            
            Log($"🎒 [ItemDetailPopup] 아이템 착용 시도: {currentItem.equipmentName}");
            
            // ⭐ 1. 클래스 호환성 체크 (최우선)
            if (!IsItemCompatibleWithCurrentClass(currentItem, out string requiredClassName))
            {
                Log($"⚠️ [ItemDetailPopup] 클래스 불일치: {currentItem.equipmentName} (필요 클래스: {requiredClassName})");
                
                // 경고 메시지 표시 (3초 후 자동 사라짐)
                StartCoroutine(ShowIncompatibleWarning(requiredClassName));
                
                return false; // ⭐ 팝업 유지 (닫지 않음)
            }
            
            // ⭐ 2. V2: 귀속 체크 (SS, EX, TR 등급만)
            if (!currentItemInstanceID.IsEmpty)
            {
                // 1. 귀속 경고가 필요한지 체크
                if (BindWarningManager.Instance.ShouldShowWarning(currentItemInstanceID))
                {
                    Log($"⚠️ [ItemDetailPopup] 상위 등급 아이템 ({currentItem.itemGrade}) - 귀속 경고 팝업 표시");
                    
                    // 2. BindWarningData 생성
                    var instanceData = AccountDataManager.Instance.GetInstance(currentItemInstanceID);
                    int currentSlot = PlayerDataManager.Instance.GetSelectedSlotIndex();
                    string characterName = PlayerDataManager.Instance.selectedPlayerData?.playerName ?? "Unknown";
                    
                    var warningData = new BindWarningData(
                        currentItemInstanceID,
                        instanceData.templateName,
                        instanceData.enhancementLevel,
                        DetermineEquipmentSlot(currentItem),
                        currentSlot,
                        characterName
                    );
                    
                    // ⭐ 로컬 변수로 데이터 캡처 (Hide() 호출 전)
                    var capturedInstanceId = currentItemInstanceID;
                    var capturedItemName = currentItem.equipmentName;
                    
                    // ⭐ ItemDetailPopup 먼저 닫기 (BindWarningPopup과 겹치지 않도록)
                    Hide();
                    
                    // 3. 귀속 경고 팝업 표시 및 사용자 응답 대기
                    BindWarningManager.Instance.ShowWarningAndWaitForResponse(warningData, (confirmed) =>
                    {
                        if (confirmed)
                        {
                            Log($"✅ [ItemDetailPopup] 사용자 확인 - 착용 진행: {capturedItemName}");
                            
                            // ⭐ 캡처된 데이터로 착용 실행
                            ExecuteEquipItemWithCapturedData(capturedInstanceId, capturedItemName);
                        }
                        else
                        {
                            Log($"❌ [ItemDetailPopup] 사용자 취소 - 착용 중단: {capturedItemName}");
                        }
                    });
                    
                    return true; // ⭐ ItemDetailPopup은 이미 닫힘
                }
            }
            
            // 4. 귀속 경고가 필요 없으면 바로 착용
            ExecuteEquipItem();
            return true; // ⭐ 즉시 착용 완료 - 팝업 닫기
        }
        
        /// <summary>
        /// 실제 착용 실행 (귀속 체크 완료 후)
        /// </summary>
        private void ExecuteEquipItem()
        {
            Log($"⚔️ [ItemDetailPopup] 착용 실행: {currentItem.equipmentName}");
            
            // V2: ItemInstanceID가 있으면 V2 API 사용
            if (!currentItemInstanceID.IsEmpty)
            {
                bool success = PlayerDataManager.Instance.EquipItemFromSharedStorage(currentItemInstanceID);
                
                if (success)
                {
                    Log($"✅ [ItemDetailPopup] 착용 성공 (V2): {currentItem.equipmentName}");
                }
                else
                {
                    Debug.LogError($"❌ [ItemDetailPopup] 착용 실패 (V2): {currentItem.equipmentName}");
                }
            }
            else
            {
                // Legacy: EquipmentData만 있는 경우 (하위 호환)
                PlayerDataManager.Instance.EquipItem(currentItem);
                Log($"✅ [ItemDetailPopup] 착용 성공 (Legacy): {currentItem.equipmentName}");
            }
        }
        
        /// <summary>
        /// 실제 착용 실행 (캡처된 데이터 사용, 귀속 팝업 콜백용)
        /// </summary>
        private void ExecuteEquipItemWithCapturedData(ItemInstanceID capturedInstanceId, string itemName)
        {
            Log($"⚔️ [ItemDetailPopup] 착용 실행 (캡처된 데이터): {itemName}");
            
            if (capturedInstanceId.IsEmpty)
            {
                Debug.LogError($"❌ [ItemDetailPopup] 잘못된 ItemInstanceID: {itemName}");
                return;
            }
            
            bool success = PlayerDataManager.Instance.EquipItemFromSharedStorage(capturedInstanceId);
            
            if (success)
            {
                Log($"✅ [ItemDetailPopup] 착용 성공: {itemName}");
            }
            else
            {
                Debug.LogError($"❌ [ItemDetailPopup] 착용 실패: {itemName}");
            }
        }
        
        /// <summary>
        /// 아이템 해제
        /// </summary>
        private void UnequipItem()
        {
            if (currentItem == null)
            {
                Debug.LogError("❌ [ItemDetailPopup] currentItem이 null입니다!");
                return;
            }
            
            // ⭐ 귀속 아이템인지 확인
            if (IsItemBound())
            {
                // 귀속 해제 경고 팝업 표시
                ShowUnequipBoundItemWarning();
                return; // 팝업 유지 (콜백에서 처리)
            }
            
            // 일반 아이템: 즉시 해제
            ExecuteUnequipItem();
        }
        
        /// <summary>
        /// ⭐ 아이템 귀속 여부 확인
        /// </summary>
        private bool IsItemBound()
        {
            if (currentItemInstanceID.IsEmpty)
                return false;
            
            var account = AccountDataManager.Instance;
            if (account == null)
                return false;
            
            // ⭐ V2 시스템: AccountDataManager의 IsBound() 사용
            return account.IsBound(currentItemInstanceID);
        }
        
        /// <summary>
        /// ⭐ 귀속 해제 경고 팝업 표시
        /// </summary>
        private void ShowUnequipBoundItemWarning()
        {
            if (BindWarningManager.Instance == null)
            {
                Debug.LogError("❌ [ItemDetailPopup] BindWarningManager.Instance가 null입니다!");
                ExecuteUnequipBoundItem(); // fallback: 그냥 삭제
                return;
            }
            
            // ItemInstance 정보 가져오기
            var account = AccountDataManager.Instance;
            var instance = account.GetInstance(currentItemInstanceID);
            var playerData = PlayerDataManager.Instance;
            var slotData = playerData.GetSlotData(playerData.CurrentSlotIndex);
            
            // 경고 데이터 생성 (BindWarningData 재사용)
            var warningData = new Systems.BindWarningData(
                currentItemInstanceID,
                instance.templateName,
                instance.enhancementLevel,
                DetermineEquipmentSlot(currentItem),
                playerData.CurrentSlotIndex,
                slotData?.playerName ?? "Unknown"
            );
            
            // ⭐ 커스텀 메시지로 팝업 표시
            string customMessage = $"귀속 아이템을 해제하면 <color=red>영구적으로 삭제</color>됩니다.\n" +
                                  $"정말 해제하시겠습니까?\n\n" +
                                  $"<b>{currentItem.equipmentName}</b>";
            
            if (instance.enhancementLevel > 0)
            {
                customMessage += $" <color=yellow>(+{instance.enhancementLevel})</color>";
            }
            
            // ItemDetailPopup 먼저 닫기
            Hide();
            
            // 귀속 해제 경고 팝업 표시
            BindWarningManager.Instance.ShowCustomWarning(
                "귀속 아이템 해제 경고",
                customMessage,
                (confirmed) =>
                {
                    if (confirmed)
                    {
                        // 사용자 확인 → 삭제 진행
                        ExecuteUnequipBoundItemWithCapturedData(currentItemInstanceID);
                    }
                    // 취소 시 아무것도 하지 않음 (ItemDetailPopup은 이미 닫혔음)
                });
        }
        
        /// <summary>
        /// ⭐ 일반 아이템 해제 실행
        /// </summary>
        private void ExecuteUnequipItem()
        {
            if (PlayerDataManager.Instance == null)
            {
                Debug.LogError("❌ [ItemDetailPopup] PlayerDataManager가 null입니다!");
                return;
            }
            
            EquipmentSlot targetSlot = DetermineEquipmentSlot(currentItem);
            
            // ⭐ V2 시스템: ItemInstanceID 기반 해제
            bool success = PlayerDataManager.Instance.UnequipItemV2(targetSlot, currentItemInstanceID);
            
            if (success)
            {
                Log($"✅ [ItemDetailPopup] 아이템 해제 성공: {currentItem.equipmentName} → 보관창고");
            }
            else
            {
                Log($"❌ [ItemDetailPopup] 아이템 해제 실패");
            }
        }
        
        /// <summary>
        /// ⭐ 귀속 아이템 해제 실행 (삭제)
        /// </summary>
        private void ExecuteUnequipBoundItem()
        {
            if (PlayerDataManager.Instance == null)
            {
                Debug.LogError("❌ [ItemDetailPopup] PlayerDataManager가 null입니다!");
                return;
            }
            
            EquipmentSlot targetSlot = DetermineEquipmentSlot(currentItem);
            
            // ⭐ 귀속 아이템 삭제 (명예의 전당은 나중에)
            bool success = PlayerDataManager.Instance.UnequipAndDeleteBoundItem(targetSlot, currentItemInstanceID);
            
            if (success)
            {
                Log($"🗑️ [ItemDetailPopup] 귀속 아이템 해제 및 삭제 성공: {currentItem.equipmentName}");
            }
            else
            {
                Log($"❌ [ItemDetailPopup] 귀속 아이템 해제 실패");
            }
        }
        
        /// <summary>
        /// ⭐ 귀속 아이템 해제 실행 (캡처된 데이터 사용)
        /// </summary>
        private void ExecuteUnequipBoundItemWithCapturedData(ItemInstanceID capturedInstanceId)
        {
            if (PlayerDataManager.Instance == null || AccountDataManager.Instance == null)
            {
                Debug.LogError("❌ [ItemDetailPopup] Manager가 null입니다!");
                return;
            }
            
            // 캡처된 ID로 아이템 정보 다시 가져오기
            var instance = AccountDataManager.Instance.GetInstance(capturedInstanceId);
            if (instance == null)
            {
                Debug.LogError($"❌ [ItemDetailPopup] ItemInstance를 찾을 수 없습니다: {capturedInstanceId.Value}");
                return;
            }
            
            // ⭐ V2 시스템: templateName으로 EquipmentData 로드
            EquipmentData equipment = Resources.Load<EquipmentData>($"EquipmentData/{instance.templateName}");
            if (equipment == null)
            {
                Debug.LogError($"❌ [ItemDetailPopup] EquipmentData를 찾을 수 없습니다: {instance.templateName}");
                return;
            }
            
            EquipmentSlot targetSlot = DetermineEquipmentSlot(equipment);
            
            // ⭐ 귀속 아이템 삭제
            bool success = PlayerDataManager.Instance.UnequipAndDeleteBoundItem(
                targetSlot,
                capturedInstanceId);
            
            if (success)
            {
                Log($"🗑️ [ItemDetailPopup] 귀속 아이템 해제 및 삭제 성공 (캡처됨): {equipment.equipmentName}");
            }
            else
            {
                Log($"❌ [ItemDetailPopup] 귀속 아이템 해제 실패 (캡처됨)");
            }
        }
        
        /// <summary>
        /// 아이템 타입에 따라 장비 슬롯 결정
        /// </summary>
        private EquipmentSlot DetermineEquipmentSlot(EquipmentData item)
        {
            if (item == null) return EquipmentSlot.MainWeapon;
            
            // 무기
            if (item.equipmentType == EquipmentType.Weapon)
            {
                return EquipmentSlot.MainWeapon;
            }
            
            // 방어구
            if (item.equipmentType == EquipmentType.Armor)
            {
                switch (item.ArmorType)
                {
                    case ArmorType.Helmet:
                        return EquipmentSlot.Helmet;
                    case ArmorType.Armor:
                        return EquipmentSlot.Armor;
                    case ArmorType.Boots:
                        return EquipmentSlot.Boots;
                    case ArmorType.Gloves:
                        return EquipmentSlot.Gloves;
                    case ArmorType.Belt:
                        return EquipmentSlot.Belt;
                    default:
                        return EquipmentSlot.Armor;
                }
            }
            
            // 악세서리
            if (item.equipmentType == EquipmentType.Accessory)
            {
                // Ring은 현재 착용 상태 확인 필요 (Ring1/Ring2)
                if (item.equipmentName.Contains("Ring"))
                {
                    // 기본값으로 Ring1 반환 (PlayerDataManager에서 자동 처리)
                    return EquipmentSlot.Ring1;
                }
                
                if (item.equipmentName.Contains("Necklace"))
                {
                    return EquipmentSlot.Necklace;
                }
            }
            
            // 기본값
            return EquipmentSlot.MainWeapon;
        }
        
        /// <summary>
        /// 아이템 판매
        /// </summary>
        private void SellItem()
        {
            if (currentItem == null || currentItemInstanceID.IsEmpty)
            {
                Debug.LogError("❌ [ItemDetailPopup] 판매할 아이템이 없습니다!");
                return;
            }
            
            // 판매 가능 여부 체크
            if (!currentItem.isTradable)
            {
                Log($"⚠️ {currentItem.equipmentName}은(는) 판매할 수 없는 아이템입니다!");
                
                // ⭐ 판매 불가 메시지 표시 (3초 후 사라짐, 팝업은 유지)
                StartCoroutine(ShowTransactionResult(false, $"{currentItem.equipmentName}은(는) 판매할 수 없습니다."));
                return;
            }
            
            Log($"🏪 [ItemDetailPopup] 아이템 판매: {currentItem.equipmentName} (ID: {currentItemInstanceID.Value.Substring(0, 8)}...)");
            
            // ShopController를 통해 판매 처리
            if (ShopController.Instance != null)
            {
                // ⭐ 판매 결과 확인
                bool success = ShopController.Instance.TrySellItem(currentItem, currentItemInstanceID);
                
                if (success)
                {
                    // ⭐ 판매 성공 메시지 표시 (1초 후 팝업 자동 닫기)
                    int sellPrice = ShopController.Instance.GetItemSellPrice(currentItem.itemID);
                    StartCoroutine(ShowTransactionResult(true, $"판매 완료! +{sellPrice} 골드"));
                }
                else
                {
                    // ⭐ 판매 실패 메시지 표시 (3초 후 사라짐, 팝업은 유지)
                    StartCoroutine(ShowTransactionResult(false, "판매에 실패했습니다."));
                }
            }
            else
            {
                Debug.LogError("❌ [ItemDetailPopup] ShopController.Instance를 찾을 수 없습니다!");
                StartCoroutine(ShowTransactionResult(false, "상점 시스템 오류"));
            }
        }
        
        /// <summary>
        /// 아이템 구매
        /// </summary>
        private void BuyItem()
        {
            Log($"🏪 [ItemDetailPopup] 아이템 구매: {currentItem.equipmentName}");
            
            if (currentItemInstanceID.IsEmpty)
            {
                Debug.LogError("❌ [ItemDetailPopup] currentItemInstanceID가 유효하지 않습니다!");
                return;
            }
            
            if (ShopController.Instance == null)
            {
                Debug.LogError("❌ [ItemDetailPopup] ShopController.Instance가 null입니다!");
                return;
            }
            
            // ShopController를 통해 구매 처리 (V2 시스템)
            bool success = ShopController.Instance.BuyItemV2(currentItemInstanceID, out PurchaseFailReason failReason);
            
            if (success)
            {
                Log($"✅ [ItemDetailPopup] 구매 성공: {currentItem.equipmentName}");
                
                // 거래 결과 표시 (성공)
                StartCoroutine(ShowTransactionResult(true, "구매 완료!"));
            }
            else
            {
                Log($"❌ [ItemDetailPopup] 구매 실패: {currentItem.equipmentName}, 이유: {failReason}");
                
                // 실패 이유에 따른 메시지 생성
                string failMessage = GetPurchaseFailMessage(failReason);
                
                // 거래 결과 표시 (실패)
                StartCoroutine(ShowTransactionResult(false, failMessage));
            }
        }
        
        /// <summary>
        /// 구매 실패 메시지 생성
        /// </summary>
        private string GetPurchaseFailMessage(PurchaseFailReason reason)
        {
            switch (reason)
            {
                case PurchaseFailReason.InsufficientGold:
                    return "구매 불가\n골드가 부족합니다";
                    
                case PurchaseFailReason.InventoryFull:
                    return "구매 불가\n보관창고가 가득 찼습니다";
                    
                case PurchaseFailReason.InvalidItem:
                    return "구매 불가\n잘못된 아이템입니다";
                    
                case PurchaseFailReason.SystemError:
                    return "구매 불가\n시스템 오류가 발생했습니다";
                    
                default:
                    return "구매 불가";
            }
        }
        
        #endregion
        
        #region Utilities
        
        /// <summary>
        /// ⭐ 경고 UI 즉시 리셋 (코루틴 정지 시 호출)
        /// </summary>
        private void ResetWarningUI()
        {
            // 버튼 텍스트/색상 원래대로 복구
            if (primaryActionButtonText != null)
            {
                // ⭐ 현재 컨텍스트에 맞는 텍스트로 복구
                string contextButtonText = currentContext switch
                {
                    ItemDetailContext.Inventory => "착용",
                    ItemDetailContext.Equipment => "해제",
                    ItemDetailContext.Shop_Sell => "판매",
                    ItemDetailContext.Shop_Buy => "구매",
                    _ => originalPrimaryButtonText
                };
                
                primaryActionButtonText.text = contextButtonText;
                primaryActionButtonText.color = originalPrimaryButtonTextColor; // ⭐ 원본 색상 복구
            }
            
            // 경고 메시지 숨김
            if (warningText != null)
            {
                warningText.gameObject.SetActive(false);
            }
            
            Log($"✅ [ItemDetailPopup] 경고 UI 즉시 리셋 (원본 색상: {originalPrimaryButtonTextColor})");
        }
        
        /// <summary>
        /// ⭐ 가격 표시 업데이트 (상점 전용)
        /// </summary>
        /// <param name="show">표시 여부</param>
        /// <param name="item">아이템 데이터</param>
        private void UpdatePriceDisplay(bool show, EquipmentData item = null, bool isBuyPrice = false)
        {
            // 가격 그룹 표시/숨김
            if (priceGroup != null)
            {
                priceGroup.SetActive(show);
            }
            
            // 숨김 모드이거나 아이템이 없으면 종료
            if (!show || item == null)
            {
                Log($"💰 [ItemDetailPopup] 가격 표시 숨김");
                return;
            }
            
            // 가격 조회 (구매가 또는 판매가)
            int price = 0;
            if (isBuyPrice)
            {
                // 구매가 (EquipmentData.buyPrice 사용)
                price = item.buyPrice;
            }
            else
            {
                // 판매가 (ShopController 또는 EquipmentData.sellPrice 사용)
                if (ShopController.Instance != null)
                {
                    price = ShopController.Instance.GetItemSellPrice(item.itemID);
                }
                else
                {
                    price = item.sellPrice;
                    Debug.LogWarning($"⚠️ [ItemDetailPopup] ShopController가 없어서 EquipmentData.sellPrice 사용: {price}");
                }
            }
            
            // UI 업데이트
            if (priceLabelText != null)
            {
                priceLabelText.text = isBuyPrice ? "구매 가격:" : "판매 가격:";
            }
            
            if (priceValueText != null)
            {
                priceValueText.text = price.ToString();
            }
            
            if (priceUnitText != null)
            {
                priceUnitText.text = "골드";
            }
            
            Log($"💰 [ItemDetailPopup] 판매 가격 표시: {price} 골드 (아이템: {item.equipmentName})");
        }
        
        /// <summary>
        /// 아이템이 현재 클래스와 호환되는지 체크
        /// </summary>
        private bool IsItemCompatibleWithCurrentClass(EquipmentData item, out string requiredClassName)
        {
            requiredClassName = "";
            
            if (item == null || PlayerDataManager.Instance?.selectedPlayerData == null)
            {
                return false;
            }
            
            // 현재 플레이어 클래스
            PlayerClass currentClass = PlayerDataManager.Instance.selectedPlayerData.selectedPlayerType switch
            {
                PlayerType.Warrior => PlayerClass.Warrior,
                PlayerType.Assasin => PlayerClass.Assasin,
                PlayerType.Wizard => PlayerClass.Wizard,
                _ => PlayerClass.Warrior
            };
            
            // 아이템 호환성 체크
            if (item.IsCompatibleWith(currentClass))
            {
                return true;
            }
            
            // 필요한 클래스 이름 추출 (usableClass 사용)
            requiredClassName = item.usableClass switch
            {
                PlayerClass.Warrior => "전사",
                PlayerClass.Assasin => "암살자",
                PlayerClass.Wizard => "마법사",
                PlayerClass.Any => "모든 클래스",
                PlayerClass.None => "모든 클래스",
                _ => "알 수 없음"
            };
            
            return false;
        }
        
        /// <summary>
        /// 클래스 불일치 경고 메시지 표시 (3초 후 자동 사라짐)
        /// </summary>
        /// <summary>
        /// ⭐ 일반 경고 메시지 표시 (판매 불가 등)
        /// </summary>
        private System.Collections.IEnumerator ShowTemporaryWarning(string message, float duration)
        {
            if (warningText != null)
            {
                warningText.text = message;
                warningText.color = new Color(1f, 0.5f, 0f); // 주황색
                warningText.gameObject.SetActive(true);
            }
            
            Log($"⚠️ [ItemDetailPopup] 경고 메시지 표시: {message}");
            
            // 지정된 시간 대기
            yield return new WaitForSeconds(duration);
            
            // 경고 메시지 숨김
            if (warningText != null)
            {
                warningText.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// ⭐ 거래 결과 메시지 표시 (판매 성공/실패)
        /// </summary>
        /// <param name="success">성공 여부</param>
        /// <param name="message">표시할 메시지</param>
        private System.Collections.IEnumerator ShowTransactionResult(bool success, string message)
        {
            if (warningText == null)
                yield break;
            
            // 메시지 색상 설정
            if (success)
            {
                // 성공: 초록색
                warningText.color = new Color(0.3f, 1f, 0.3f); // 밝은 초록색
                Log($"✅ [ItemDetailPopup] 거래 성공: {message}");
            }
            else
            {
                // 실패: 빨간색
                warningText.color = new Color(1f, 0.3f, 0.3f); // 빨간색
                Log($"❌ [ItemDetailPopup] 거래 실패: {message}");
            }
            
            // 메시지 표시
            warningText.text = message;
            warningText.gameObject.SetActive(true);
            
            // 대기 시간
            float waitTime = success ? 1.0f : 3.0f; // 성공: 1초, 실패: 3초
            yield return new WaitForSeconds(waitTime);
            
            // 메시지 숨김
            warningText.gameObject.SetActive(false);
            
            // ⭐ 성공 시에만 팝업 닫기
            if (success)
            {
                Hide();
                Log($"🔒 [ItemDetailPopup] 거래 성공 후 팝업 자동 닫기");
            }
        }
        
        /// <summary>
        /// ⭐ 클래스 불일치 경고 표시
        /// </summary>
        private System.Collections.IEnumerator ShowIncompatibleWarning(string requiredClassName)
        {
            // 버튼 텍스트 변경
            string originalButtonText = primaryActionButtonText?.text ?? "착용";
            
            if (primaryActionButtonText != null)
            {
                primaryActionButtonText.text = "착용불가";
                primaryActionButtonText.color = new Color(1f, 0.3f, 0.3f); // 빨간색
            }
            
            // 경고 메시지 표시
            if (warningText != null)
            {
                warningText.text = $"이 아이템은 {requiredClassName} 전용입니다";
                warningText.color = new Color(1f, 0.5f, 0f); // 주황색
                warningText.gameObject.SetActive(true);
            }
            
            Log($"⚠️ [ItemDetailPopup] 클래스 불일치 경고 표시: {requiredClassName} 전용");
            
            // 3초 대기
            yield return new WaitForSeconds(3f);
            
            // ⭐ 원래대로 복구 (원본 색상 사용)
            if (primaryActionButtonText != null)
            {
                primaryActionButtonText.text = originalButtonText;
                primaryActionButtonText.color = originalPrimaryButtonTextColor; // ⭐ 원본 색상으로 복구
            }
            
            if (warningText != null)
            {
                warningText.gameObject.SetActive(false);
            }
            
            Log($"✅ [ItemDetailPopup] 경고 메시지 자동 사라짐 (원본 색상 복구)");
        }
        
        /// <summary>
        /// 디버그 로그 출력
        /// </summary>
        private void Log(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log(message);
            }
        }
        
        #endregion
    }
}

