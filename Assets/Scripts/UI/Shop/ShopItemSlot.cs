using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 🏪 상점 아이템 슬롯 (개별 상품 표시)
/// 아이템 아이콘, 이름, 가격을 표시하고 클릭 이벤트 처리
/// </summary>
public class ShopItemSlot : MonoBehaviour
{
    [Header("🎯 UI 요소")]
    [SerializeField] private Image itemIcon;              // 아이템 아이콘
    [SerializeField] private TMP_Text itemNameText;       // 아이템 이름
    [SerializeField] private TMP_Text priceText;          // 가격 표시
    [SerializeField] private Button slotButton;           // 클릭 버튼
    [SerializeField] private GameObject soldOutOverlay;   // 품절 오버레이
    [SerializeField] private TMP_Text stockText;          // 재고 수량 표시
    
    [Header("🎨 등급별 색상")]
    [SerializeField] private Image gradeFrame;            // 등급 테두리
    [SerializeField] private Color sRankColor = Color.red;
    [SerializeField] private Color aRankColor = Color.yellow;
    [SerializeField] private Color bRankColor = Color.green;
    [SerializeField] private Color cRankColor = Color.blue;
    [SerializeField] private Color dRankColor = Color.gray;
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 이벤트
    public event Action<string> OnItemClicked;              // Legacy: itemID 기반
    public event Action<ItemInstanceID> OnItemClickedV2;    // 🆕 V2: ItemInstanceID 기반
    
    // 현재 아이템 정보
    private EquipmentData currentEquipment;
    private string currentItemID;
    private int currentPrice;
    private ItemInstanceID currentInstanceId;               // 🆕 V2: 전시용 ItemInstance ID
    
    void Start()
    {
        InitializeSlot();
        
        // 🔧 수정: 초기 상태 강제 설정
        ForceInitialState();
    }

    /// <summary>
    /// 🆕 초기 상태 강제 설정 (UI 요소 기본값)
    /// </summary>
    private void ForceInitialState()
    {
        // ItemIcon 초기 상태
        if (itemIcon != null)
        {
            itemIcon.color = new Color(1f, 1f, 1f, 1f); // White, Alpha 255
        }
        
        // 텍스트 색상 초기화
        if (itemNameText != null)
        {
            itemNameText.color = Color.white;
        }
        
        if (priceText != null)
        {
            priceText.color = Color.white;
        }
        
        if (showDebugLogs)
            Debug.Log($"🔧 [ShopItemSlot] 초기 상태 강제 설정 완료");
    }
    
    /// <summary>
    /// 슬롯 초기화
    /// </summary>
    private void InitializeSlot()
    {
        // 버튼 이벤트 연결
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(HandleSlotClicked);
        }
        
        // 초기 상태를 빈 슬롯으로 설정
        SetEmpty();
    }
    
    /// <summary>
    /// 아이템 데이터 설정 (Legacy)
    /// </summary>
    public void SetEquipmentData(EquipmentData equipment)
    {
        currentEquipment = equipment;
        currentInstanceId = default; // Legacy 모드에서는 ItemInstanceID 없음
        
        if (equipment != null)
        {
            currentItemID = equipment.itemID;
            currentPrice = equipment.buyPrice;
            
            UpdateSlotVisuals();
            SetInteractable(true);
            
            if (showDebugLogs)
                Debug.Log($"✅ [ShopItemSlot] 아이템 설정 완료: {equipment.equipmentName}");
        }
        else
        {
            SetEmpty();
        }
    }
    
    /// <summary>
    /// 🆕 V2: 아이템 데이터 설정 (ItemInstanceID 기반)
    /// </summary>
    public void SetEquipmentDataV2(ItemInstanceID instanceId, EquipmentData equipment)
    {
        currentInstanceId = instanceId;
        currentEquipment = equipment;
        
        if (equipment != null && !instanceId.IsEmpty)
        {
            currentItemID = equipment.itemID;
            currentPrice = equipment.buyPrice;
            
            UpdateSlotVisuals();
            SetInteractable(true);
            
            if (showDebugLogs)
                Debug.Log($"✅ [ShopItemSlot] V2 아이템 설정 완료: {equipment.equipmentName} (ID: {instanceId.Value.Substring(0, 8)}...)");
        }
        else
        {
            SetEmpty();
        }
    }
    
    /// <summary>
    /// 🔧 수정: 슬롯 시각적 업데이트
    /// </summary>
    private void UpdateSlotVisuals()
    {
        if (currentEquipment == null) 
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [ShopItemSlot] currentEquipment가 null입니다.");
            return;
        }
        
        Debug.Log($"🔧 [ShopItemSlot] UpdateSlotVisuals 시작: {currentEquipment.equipmentName}");
        
        // 🆕 UI 요소 null 체크 강화 (강제 디버그)
        Debug.Log($"🔍 [ShopItemSlot] UI 요소 상태 체크:");
        
        // 아이콘 설정
        if (itemIcon != null)
        {
            if (currentEquipment.icon != null)
            {
                itemIcon.sprite = currentEquipment.icon;
                itemIcon.color = new Color(1f, 1f, 1f, 1f);
                itemIcon.gameObject.SetActive(true);
            }
            else
            {
                itemIcon.color = Color.clear;
                if (showDebugLogs)
                    Debug.LogWarning($"⚠️ [ShopItemSlot] 아이콘 없음: {currentEquipment.equipmentName}");
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.LogError($"❌ [ShopItemSlot] itemIcon이 null입니다!");
        }
        
        // 이름 설정
        if (itemNameText != null)
        {
            itemNameText.text = currentEquipment.equipmentName;
            itemNameText.color = Color.white;
            itemNameText.gameObject.SetActive(true);
            
            if (showDebugLogs)
                Debug.Log($"✅ [ShopItemSlot] 아이템 이름 설정: {currentEquipment.equipmentName}");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogError($"❌ [ShopItemSlot] itemNameText가 null!");
        }
        
        // 가격 설정
        if (priceText != null)
        {
            priceText.text = currentPrice.ToString();
            priceText.gameObject.SetActive(true);
            
            if (showDebugLogs)
                Debug.Log($"✅ [ShopItemSlot] 가격 설정: {currentPrice}");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogError($"❌ [ShopItemSlot] priceText가 null!");
        }
        
        // 등급별 테두리 색상 설정
        UpdateGradeFrame();
        
        // 재고 정보 업데이트
        UpdateStockInfo();
        
        if (showDebugLogs)
            Debug.Log($"✅ [ShopItemSlot] UI 업데이트 완료: {currentEquipment.equipmentName}");
    }
    
    /// <summary>
    /// 🔧 수정: 등급별 테두리 색상 업데이트
    /// </summary>
    private void UpdateGradeFrame()
    {
        if (gradeFrame == null)
        {
            if (showDebugLogs)
                Debug.LogError($"❌ [ShopItemSlot] gradeFrame이 null입니다!");
            return;
        }
        
        if (currentEquipment == null)
        {
            if (showDebugLogs)
                Debug.LogError($"❌ [ShopItemSlot] currentEquipment가 null입니다!");
            return;
        }
        
        Color frameColor = Color.white;
        
        switch (currentEquipment.itemGrade)
        {
            case ItemGrade.S:
                frameColor = sRankColor;
                break;
            case ItemGrade.A:
                frameColor = aRankColor;
                break;
            case ItemGrade.B:
                frameColor = bRankColor;
                break;
            case ItemGrade.C:
                frameColor = cRankColor;
                break;
            case ItemGrade.D:
                frameColor = dRankColor;
                break;
            default:
                frameColor = Color.white;
                break;
        }
        
        gradeFrame.color = frameColor;
        gradeFrame.gameObject.SetActive(true);
        
        // 🆕 활성화 상태 확인
        Debug.Log($"   🔍 gradeFrame.gameObject.SetActive(true) 호출 완료");
        Debug.Log($"      - activeInHierarchy: {gradeFrame.gameObject.activeInHierarchy}");
        Debug.Log($"      - activeSelf: {gradeFrame.gameObject.activeSelf}");
        
        // 🔧 수정: 과도한 디버깅 로그 제거
        // Debug.Log($"🌈 [ShopItemSlot] 등급 프레임 색상 설정: {currentEquipment.itemGrade} → {frameColor}");
    }
    
    /// <summary>
    /// 재고 정보 업데이트
    /// </summary>
    private void UpdateStockInfo()
    {
        if (currentEquipment == null) return;
        
        // 한정 아이템인 경우 재고 표시
        if (currentEquipment.isLimited && stockText != null)
        {
            stockText.text = $"한정 {currentEquipment.quantityLimit}개";
            stockText.gameObject.SetActive(true);
        }
        else if (stockText != null)
        {
            stockText.gameObject.SetActive(false);
        }
        
        // 품절 처리 (추후 동적 재고 관리 시 사용)
        bool isSoldOut = currentEquipment.isLimited && currentEquipment.quantityLimit <= 0;
        if (soldOutOverlay != null)
        {
            soldOutOverlay.SetActive(isSoldOut);
        }
        
        SetInteractable(!isSoldOut);
    }
    
    /// <summary>
    /// 빈 슬롯으로 설정
    /// </summary>
    public void SetEmpty()
    {
        currentEquipment = null;
        currentItemID = "";
        currentPrice = 0;
        currentInstanceId = default; // 🆕 V2: ItemInstanceID 초기화
        
        // UI 초기화
        if (itemIcon != null)
            itemIcon.color = Color.clear;
        if (itemNameText != null)
            itemNameText.text = "";
        if (priceText != null)
            priceText.text = "";
        if (stockText != null)
            stockText.gameObject.SetActive(false);
        if (soldOutOverlay != null)
            soldOutOverlay.SetActive(false);
        if (gradeFrame != null)
            gradeFrame.color = Color.white;
        
        SetInteractable(false);
    }
    
    /// <summary>
    /// 슬롯 상호작용 가능 여부 설정
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (slotButton != null)
        {
            slotButton.interactable = interactable;
        }
        
        // 비활성화 시 시각적 피드백
        float alpha = interactable ? 1f : 0.5f;  // 🔧 수정: Color → float
        if (itemIcon != null)
        {
            var color = itemIcon.color;  // 🔧 수정: Color 타입으로 받기
            color.a = alpha;
            itemIcon.color = color;
        }
    }
    
    /// <summary>
    /// 슬롯 클릭 처리
    /// </summary>
    private void HandleSlotClicked()
    {
        if (showDebugLogs)
            Debug.Log($"🔥 [ShopItemSlot] HandleSlotClicked 호출됨!");
        
        if (!string.IsNullOrEmpty(currentItemID) && currentEquipment != null)
        {
            if (showDebugLogs)
                Debug.Log($"🛒 [ShopItemSlot] 상점 아이템 클릭됨: {currentEquipment.equipmentName} (ID: {currentItemID}, 가격: {currentPrice})");
            
            // V2 이벤트 우선 발생
            if (!currentInstanceId.IsEmpty)
            {
                OnItemClickedV2?.Invoke(currentInstanceId);
                
                if (showDebugLogs)
                    Debug.Log($"📤 [ShopItemSlot] OnItemClickedV2 이벤트 발생 완료 (InstanceId: {currentInstanceId.Value.Substring(0, 8)}...)");
            }
            
            // Legacy 이벤트도 발생 (하위 호환성)
            OnItemClicked?.Invoke(currentItemID);
            
            if (showDebugLogs)
                Debug.Log($"📤 [ShopItemSlot] OnItemClicked 이벤트 발생 완료");
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [ShopItemSlot] 클릭했지만 아이템 정보가 없음: ItemID='{currentItemID}', Equipment={currentEquipment != null}");
        }
    }
    
    // 현재 아이템 정보 접근자
    public EquipmentData CurrentEquipment => currentEquipment;
    public string CurrentItemID => currentItemID;
    public int CurrentPrice => currentPrice;
}