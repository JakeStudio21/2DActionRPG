using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
// 🗑️ 제거: IPointerClickHandler 사용 안 함 (Button.onClick 사용)
// using UnityEngine.EventSystems;
using TMPro; // 🆕 추가
using UI.Components; // ⭐ ItemIconGradeFrame


/// 🏠 LobbyInventoryUI - 로비 전용 인벤토리 UI
/// 책임:
/// - 인벤토리 아이템 표시
/// - 아이템 상세 정보 표시 (DetailPanel)
/// - 아이템 착용/해제 기능
/// - 로비 전용 UI 상호작용
/// 
/// 의존성:
/// - PlayerDataManager (데이터 소스)
/// - LobbyInventoryController (제어)
/// </summary>

/// <summary>
/// 🏪 ShopInventoryUI - 상점 전용 인벤토리 UI  
/// 책임:
/// - 판매용 아이템 선택 표시
/// - 상점 거래를 위한 아이템 클릭 처리
/// 
/// 제외 기능:
/// - 아이템 착용 (로비 전용)
/// - 상세 정보 표시 (상점은 DetailPanel 별도)
/// 
/// 의존성:
/// - PlayerDataManager (데이터 소스)
/// - ShopUIController (거래 제어)
/// </summary>

/// <summary>
/// 🎮 IntegratedInventoryController - 인게임 전용 컨트롤러
/// 책임:
/// - 인게임 인벤토리 토글 (I키, 가방 버튼)
/// - 무기 교체 중심 상호작용
/// - ActiveInventory와 연동
/// 
/// 의존성:
/// - PlayerDataManager (데이터 소스)
/// - ActiveInventory (인게임 UI)
/// - ActiveWeapon (무기 교체)

public class InventorySlot : MonoBehaviour  // 🗑️ 제거: IPointerClickHandler (Button.onClick 사용)
{
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = false; // 🆕 추가: 디버그 로그 제어
    
    [Header("🛡️ 장비 데이터 (신규 시스템)")]
    [SerializeField] private EquipmentData equipmentData;
    private ItemInstanceId itemInstanceId;  // 🆕 V2: 아이템 인스턴스 ID
    
    [Header("📦 재료 데이터")]
    private MaterialType? currentMaterial = null; // 재료 타입 (null이면 장비 슬롯)
    
    [Header("🎨 UI 컴포넌트")]
    [SerializeField] private Image slotImage;      // 슬롯 배경
    [SerializeField] private Image itemIconImage;  // 아이템 아이콘
    [SerializeField] private ItemIconGradeFrame itemIconGradeFrame; // ⭐ 등급별 배경 색상
    [SerializeField] private Button slotButton;    // 클릭 버튼
    [SerializeField] private Image bindIcon;       // 🆕 귀속 아이콘
    [SerializeField] private TMP_Text countText;   // 📦 재료 수량 텍스트 ("X999")
    [SerializeField] private Image rarityBorder;   // 📦 재료 등급 테두리

    [Header("🎨 UI 메시지")]
    [SerializeField] private GameObject messagePanel; // 메시지 패널 (생성될 예정)
    [SerializeField] private TextMeshProUGUI messageText; // 메시지 텍스트
    
    // 슬롯 상태
    public bool isEmpty => equipmentData == null;
    public bool isSelected = false;
    
    // 🆕 V2: 귀속 상태 (나중에 ItemInstanceData에서 가져올 예정)
    private bool isBound = false;

    void Awake()
    {
        // UI 컴포넌트 자동 찾기
        if (slotImage == null) slotImage = GetComponent<Image>(); // 슬롯 배경
        if (slotButton == null) slotButton = GetComponent<Button>();
        
        // 🔑 ItemIcon을 이름으로 정확히 찾기
        if (itemIconImage == null) 
        {
            // 1순위: EffectTarget/ItemIcon (UIButtonClickEffect 구조)
            Transform itemIconTransform = transform.Find("EffectTarget/ItemIcon");
            
            // 2순위: ItemIcon (기존 구조 - fallback)
            if (itemIconTransform == null)
            {
                itemIconTransform = transform.Find("ItemIcon");
            }
            
            if (itemIconTransform != null)
            {
                itemIconImage = itemIconTransform.GetComponent<Image>();
                
                // 경로 정보 추가 (디버그용)
                string path = itemIconTransform.parent != null && itemIconTransform.parent != transform 
                    ? $"{itemIconTransform.parent.name}/{itemIconTransform.name}" 
                    : itemIconTransform.name;
                
                Debug.Log($"✅ [InventorySlot] ItemIcon 찾음: {itemIconImage != null} (경로: {path})");
            }
            else
            {
                Debug.LogError($"🔴 [InventorySlot] ItemIcon을 찾을 수 없습니다! {gameObject.name}");
            }
        }
        
        // 🆕 BindIcon을 이름으로 정확히 찾기
        if (bindIcon == null)
        {
            // 1순위: EffectTarget/BindIcon (UIButtonClickEffect 구조)
            Transform bindIconTransform = transform.Find("EffectTarget/BindIcon");
            
            // 2순위: BindIcon (기존 구조 - fallback)
            if (bindIconTransform == null)
            {
                bindIconTransform = transform.Find("BindIcon");
            }
            
            if (bindIconTransform != null)
            {
                bindIcon = bindIconTransform.GetComponent<Image>();
                
                if (showDebugLogs)
                {
                    string path = bindIconTransform.parent != null && bindIconTransform.parent != transform 
                        ? $"{bindIconTransform.parent.name}/{bindIconTransform.name}" 
                        : bindIconTransform.name;
                    Debug.Log($"✅ [InventorySlot] BindIcon 찾음: {bindIcon != null} (경로: {path})");
                }
            }
            else
            {
                if (showDebugLogs)
                    Debug.LogWarning($"⚠️ [InventorySlot] BindIcon이 없습니다 (선택적 요소). {gameObject.name}");
            }
        }
        
        // 버튼 클릭 이벤트 연결
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }
    }

    void Start()
    {
        UpdateSlotVisual();
    }
    
    // 🗑️ 제거: OnPointerClick (Button.onClick이 처리)
    // Button 컴포넌트가 있으므로 OnPointerClick은 불필요
    // LobbyInventoryUI, ShopInventoryUI가 Button.onClick.AddListener()로 처리
    
    /// <summary>
    /// 🖱️ 슬롯 클릭 이벤트 처리 (Button.onClick에서 호출됨)
    /// </summary>
    public void OnSlotClicked()
    {
        // 📦 재료 슬롯 클릭 처리
        if (currentMaterial.HasValue)
        {
            if (showDebugLogs)
                Debug.Log($"📦 [InventorySlot] 재료 클릭: {currentMaterial.Value.GetDisplayName()}");
            
            // 재료 상세 패널 열기 (TODO: Phase D에서 구현)
            // ItemDetailPopup.ShowMaterialDetail(currentMaterial.Value);
            
            return;
        }
        
        // 빈 슬롯 클릭 방지
        if (equipmentData == null)
        {
            return;
        }
        
        // 🔧 지연 갱신 최적화: 데이터 로드 상태 확인
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.IsLazyLoadRequired())
        {
            if (showDebugLogs)
                Debug.Log("🔄 [InventorySlot] 지연 로드 필요 - 클릭 이벤트 지연 처리");
            
            // 지연 로드 후 클릭 이벤트 재처리
            StartCoroutine(HandleClickWithLazyLoad());
            return;
        }
        
        // 환경별 슬롯 인덱스 계산
        int slotIndex = GetActualSlotIndex();
        
        // 필수 로그만 유지
        if (showDebugLogs)
        {
            string environment = DetectEnvironment();
            Debug.Log($"🖱️ [InventorySlot] {environment} 슬롯 클릭: {equipmentData.equipmentName} (인덱스: {slotIndex})");
        }
        
        // 🎯 PlayerDataManager 이벤트 발생 (모든 UI에서 구독 가능)
        // V2: ItemInstanceId 전달 (귀속 체크용)
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.TriggerSlotClicked(equipmentData, slotIndex, itemInstanceId);
        }
    }
    
    /// <summary>
    /// 슬롯 선택 상태 설정
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateSlotVisual();
    }

    /// <summary>
    /// 슬롯 비주얼 업데이트
    /// </summary>
    public void UpdateSlotVisual()
    {
        if (slotImage == null || itemIconImage == null) return;

        // 선택 상태에 따른 색상 변경
        slotImage.color = isSelected ? Color.yellow : Color.white;

        // 아이템 아이콘 표시
        if (!isEmpty)
        {
            // 아이콘 있음
            itemIconImage.color = Color.white;
            
            // 아이콘 이미지 설정 (EquipmentData 우선)
            if (equipmentData != null)
            {
                if (equipmentData.icon != null)
                {
                    itemIconImage.sprite = equipmentData.icon;
                }
                else
                {
                    // EquipmentData에 아이콘이 없으면 equipmentPrefab의 SpriteRenderer에서 가져오기
                    if (equipmentData.equipmentPrefab != null)
                    {
                        var spriteRenderer = equipmentData.equipmentPrefab.GetComponent<SpriteRenderer>();
                        if (spriteRenderer != null && spriteRenderer.sprite != null)
                        {
                            itemIconImage.sprite = spriteRenderer.sprite;
                            Debug.Log($"🎨 [InventorySlot] 프리팹에서 스프라이트 가져옴: {equipmentData.equipmentName}");
                        }
                        else
                        {
                            // 기본 아이콘 설정 (색상으로 구분)
                            SetDefaultIcon();
                        }
                    }
                    else
                    {
                        SetDefaultIcon();
                    }
                }
            }
            
            // 🆕 귀속 아이콘 표시
            UpdateBindIcon();
        }
        else
        {
            // 아이콘 없음 (빈 슬롯)
            itemIconImage.color = Color.clear;
            itemIconImage.sprite = null;
            
            // 🆕 귀속 아이콘 숨김
            if (bindIcon != null)
            {
                bindIcon.gameObject.SetActive(false);
            }
            
            // ⭐ 등급 프레임 초기화 (빈 슬롯에서 이전 색상 제거)
            if (itemIconGradeFrame != null)
            {
                itemIconGradeFrame.SetGrade(ItemGrade.D); // 기본 등급으로 리셋
            }
        }
    }
    
    /// <summary>
    /// 🆕 귀속 아이콘 업데이트
    /// </summary>
    private void UpdateBindIcon()
    {
        if (bindIcon == null) return;
        
        // 귀속 상태에 따라 아이콘 표시/숨김
        if (isBound)
        {
            bindIcon.gameObject.SetActive(true);
            
            if (showDebugLogs)
                Debug.Log($"🔒 [InventorySlot] 귀속 아이콘 표시: {equipmentData?.equipmentName}");
        }
        else
        {
            bindIcon.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 🆕 귀속 상태 설정 (외부에서 호출)
    /// </summary>
    public void SetBindingStatus(bool bound)
    {
        isBound = bound;
        UpdateBindIcon();
    }
    
    /// <summary>
    /// 기본 아이콘 설정 (임시 색상 구분)
    /// </summary>
    private void SetDefaultIcon()
    {
        itemIconImage.sprite = null;
        
        // 장비 타입별 색상 구분
        if (equipmentData != null)
        {
            switch (equipmentData.equipmentType)
            {
                case EquipmentType.Weapon:
                    itemIconImage.color = Color.red; // 무기는 빨간색
                    break;
                case EquipmentType.Armor:
                    itemIconImage.color = Color.blue; // 방어구는 파란색
                    break;
                case EquipmentType.Accessory:
                    itemIconImage.color = Color.green; // 악세서리는 초록색
                    break;
                default:
                    itemIconImage.color = Color.white;
                    break;
            }
        }
        else
        {
            itemIconImage.color = Color.white;
        }
        
        Debug.Log($"🎨 [InventorySlot] 기본 아이콘 설정: {itemIconImage.color}");
    }

    /// <summary>
    /// 🆕 클래스 호환성 오류 메시지 표시
    /// </summary>
    public void ShowIncompatibilityMessage()
    {
        StartCoroutine(ShowMessageCoroutine("클래스가 다름", 2f));
    }
    
    /// <summary>
    /// 🆕 메시지 표시 코루틴
    /// </summary>
    private IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        // 메시지 패널이 없으면 동적 생성
        if (messagePanel == null)
        {
            CreateMessagePanel();
        }
        
        if (messagePanel != null && messageText != null)
        {
            // 메시지 설정
            messageText.text = message;
            messagePanel.SetActive(true);
            
            Debug.Log($"🎨 [InventorySlot] 메시지 표시: {message}");
            
            // 지정된 시간 대기
            yield return new WaitForSeconds(duration);
            
            // 메시지 숨김
            messagePanel.SetActive(false);
            
            Debug.Log($"🎨 [InventorySlot] 메시지 숨김: {message}");
        }
        else
        {
            Debug.LogError("🔴 [InventorySlot] 메시지 패널 생성 실패!");
        }
    }
    
    /// <summary>
    /// 🆕 메시지 패널 동적 생성
    /// </summary>
    private void CreateMessagePanel()
    {
        // Canvas 찾기
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
        }
        
        if (canvas == null)
        {
            Debug.LogError("🔴 [InventorySlot] Canvas를 찾을 수 없어 메시지 패널을 생성할 수 없습니다!");
            return;
        }
        
        // 메시지 패널 생성
        messagePanel = new GameObject("IncompatibilityMessage");
        messagePanel.transform.SetParent(canvas.transform, false);
        
        // RectTransform 설정
        RectTransform messageRect = messagePanel.AddComponent<RectTransform>();
        messageRect.sizeDelta = new Vector2(120, 30);
        
        // 슬롯 위쪽에 위치 설정
        Vector3 worldPos = transform.position;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform, 
            screenPos + Vector3.up * 50, // 슬롯 위쪽 50픽셀
            canvas.worldCamera, 
            out localPoint);
        messageRect.localPosition = localPoint;
        
        // 배경 이미지 추가
        Image bgImage = messagePanel.AddComponent<Image>();
        bgImage.color = new Color(1f, 0.2f, 0.2f, 0.8f); // 반투명 빨간색
        
        // 텍스트 생성
        GameObject textObj = new GameObject("MessageText");
        textObj.transform.SetParent(messagePanel.transform, false);
        
        messageText = textObj.AddComponent<TextMeshProUGUI>();
        messageText.text = "클래스가 다름";
        messageText.fontSize = 14;
        messageText.color = Color.white;
        messageText.alignment = TextAlignmentOptions.Center;
        
        // 텍스트 RectTransform 설정
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // 초기에는 비활성화
        messagePanel.SetActive(false);
        
        Debug.Log("🎨 [InventorySlot] 메시지 패널 생성 완료");
    }

    // 기존 메서드들 유지
    public EquipmentData GetEquipmentData() 
    {
        Debug.Log($"🔍 [InventorySlot] GetEquipmentData() 호출됨");
        Debug.Log($"🔍 [InventorySlot] equipmentData: {(equipmentData != null ? equipmentData.name : "NULL")}");
        
        if (equipmentData != null)
        {
            Debug.Log($"🔍 [InventorySlot] equipmentData.equipmentName: {equipmentData.equipmentName}");
            Debug.Log($"🔍 [InventorySlot] equipmentData.usableClass: {equipmentData.usableClass}");
            Debug.Log($"🔍 [InventorySlot] equipmentData.WeaponType: {equipmentData.WeaponType}");
        }
        
        return equipmentData;
    }
    
    public bool HasWeapon() {
        return equipmentData != null && equipmentData.IsWeapon;
    }
    
    public string GetWeaponName() {
        if (equipmentData != null && equipmentData.IsWeapon) return equipmentData.equipmentName;
        return "Empty";
    }
    
    public string GetWeaponStats() {
        if (equipmentData != null && equipmentData.IsWeapon)
        {
            return $"타입:{equipmentData.equipmentType}, 등급:{equipmentData.itemGrade}";
        }
        return "능력치 없음";
    }

    /// <summary>
    /// 장비 데이터 설정 (외부에서 호출)
    /// </summary>
    public void SetEquipmentData(EquipmentData data, ItemInstanceId instanceId = default)
    {
        equipmentData = data;
        itemInstanceId = instanceId;  // 🆕 V2: 인스턴스 ID 저장
        
        // ⭐ 재료 모드 해제 (장비 모드로 전환)
        currentMaterial = null;
        
        // ⭐ 재료 전용 UI 숨김
        if (countText != null)
        {
            countText.gameObject.SetActive(false);
        }
        
        if (rarityBorder != null)
        {
            rarityBorder.enabled = false;
            rarityBorder.gameObject.SetActive(false); // ⭐ GameObject도 비활성화
        }
        
        // ⭐ GradeBoard (ItemIconGradeFrame) 활성화 및 등급별 배경 색상 적용
        if (itemIconGradeFrame != null)
        {
            if (data != null)
            {
                itemIconGradeFrame.gameObject.SetActive(true); // ⭐ 장비 모드에서 활성화
                itemIconGradeFrame.SetGrade(data.itemGrade);
            }
            else
            {
                // 빈 슬롯일 경우 비활성화
                itemIconGradeFrame.gameObject.SetActive(false);
            }
        }
        
        UpdateSlotVisual();
    }
    
    /// <summary>
    /// 🆕 V2: 아이템 인스턴스 ID 가져오기
    /// </summary>
    public ItemInstanceId GetItemInstanceId()
    {
        return itemInstanceId;
    }

    /// <summary>
    /// 🔧 수정: 표준화된 슬롯 인덱스 계산 (환경 자동 감지)
    /// </summary>
    private int GetActualSlotIndex()
    {
        // 🆕 1순위: ActiveInventory 환경 (인게임)
        var activeInventory = GetComponentInParent<ActiveInventory>();
        if (activeInventory != null) 
        {
            Transform activeInventoryTransform = activeInventory.transform;
            for (int i = 0; i < activeInventoryTransform.childCount; i++)
            {
                if (activeInventoryTransform.GetChild(i) == transform)
                {
                    if (showDebugLogs)
                        Debug.Log($"🎮 [InventorySlot] ActiveInventory 환경 - 인덱스: {i}");
                    return i;
                }
            }
        }
        
        // 🆕 2순위: LobbyInventoryUI 환경 (로비)
        var lobbyInventoryUI = GetComponentInParent<LobbyInventoryUI>();
        if (lobbyInventoryUI != null)
        {
            // 로비에서는 부모 Container의 자식 순서 사용
            Transform containerTransform = transform.parent;
            if (containerTransform != null)
            {
                for (int i = 0; i < containerTransform.childCount; i++)
                {
                    if (containerTransform.GetChild(i) == transform)
                    {
                        if (showDebugLogs)
                            Debug.Log($"🏠 [InventorySlot] 로비 환경 - 인덱스: {i}");
                        return i;
                    }
                }
            }
        }
        
        // 🆕 3순위: ShopInventoryUI 환경 (상점)
        var shopInventoryUI = GetComponentInParent<ShopInventoryUI>();
        if (shopInventoryUI != null)
        {
            // 상점에서는 부모 Container의 자식 순서 사용
            Transform containerTransform = transform.parent;
            if (containerTransform != null)
            {
                for (int i = 0; i < containerTransform.childCount; i++)
                {
                    if (containerTransform.GetChild(i) == transform)
                    {
                        if (showDebugLogs)
                            Debug.Log($"🏪 [InventorySlot] 상점 환경 - 인덱스: {i}");
                        return i;
                    }
                }
            }
        }
        
        // 🔧 4순위: 기본값 (환경을 감지하지 못한 경우)
        int siblingIndex = transform.GetSiblingIndex();
        if (showDebugLogs)
            Debug.Log($"❓ [InventorySlot] 알 수 없는 환경 - Sibling 인덱스 사용: {siblingIndex}");
        return siblingIndex;
    }

    /// <summary>
    /// 🆕 환경 감지 헬퍼 메서드
    /// </summary>
    private string DetectEnvironment()
    {
        if (GetComponentInParent<ActiveInventory>() != null)
            return "인게임";
        
        if (GetComponentInParent<LobbyInventoryUI>() != null)
            return "로비";
            
        if (GetComponentInParent<ShopInventoryUI>() != null)
            return "상점";
            
        return "알 수 없음";
    }

    /// <summary>
    /// 🆕 지연 로드와 함께 클릭 이벤트 처리
    /// </summary>
    private IEnumerator HandleClickWithLazyLoad()
    {
        // 현재 선택된 슬롯 데이터 로드
        int selectedSlot = PlayerDataManager.Instance.GetSelectedSlotIndex();
        bool loadSuccess = PlayerDataManager.Instance.LazyLoadSlotData(selectedSlot);
        
        if (!loadSuccess)
        {
            Debug.LogError("❌ [InventorySlot] 지연 로드 실패 - 클릭 이벤트 취소");
            yield break;
        }
        
        yield return new WaitForSeconds(0.1f); // 로드 완료 대기
        
        // 클릭 이벤트 재처리
        OnSlotClicked();
    }
    
    // ========================================
    // 📦 재료 관련 메서드
    // ========================================
    
    /// <summary>
    /// 재료 슬롯 설정
    /// </summary>
    public void SetupMaterial(MaterialStack materialStack)
    {
        ClearSlot();
        
        currentMaterial = materialStack.materialType;
        var data = materialStack.GetData();
        
        if (data == null)
        {
            Debug.LogWarning($"[InventorySlot] MaterialData 없음: {materialStack.materialType}");
            return;
        }
        
        // 아이콘 설정
        if (itemIconImage != null)
        {
            itemIconImage.sprite = data.icon;
            itemIconImage.enabled = true;
            itemIconImage.color = Color.white;
        }
        
        // 수량 텍스트
        if (countText != null)
        {
            countText.text = $"X{materialStack.count}";
            countText.gameObject.SetActive(true);
        }
        
        // ⭐ 등급 테두리 (재료 전용)
        if (rarityBorder != null)
        {
            Color borderColor = data.GetBorderColor();
            rarityBorder.color = borderColor;
            rarityBorder.enabled = true;
            rarityBorder.gameObject.SetActive(true); // ⭐ GameObject도 활성화
            
            if (showDebugLogs)
            {
                Debug.Log($"[InventorySlot] 재료 테두리 설정: {materialStack.materialType} " +
                         $"| Rarity: {data.rarity} " +
                         $"| Color: {borderColor} " +
                         $"| Alpha: {borderColor.a} " +
                         $"| GameObject Active: {rarityBorder.gameObject.activeSelf}");
            }
        }
        else
        {
            Debug.LogWarning($"[InventorySlot] rarityBorder가 null입니다! (재료: {materialStack.materialType})");
        }
        
        // ⭐ GradeBoard (ItemIconGradeFrame) 비활성화 (재료 모드에서는 사용 안 함)
        if (itemIconGradeFrame != null)
        {
            itemIconGradeFrame.gameObject.SetActive(false);
        }
        
        // ⭐ 귀속 아이콘 비활성화 (재료는 귀속 개념 없음)
        if (bindIcon != null)
        {
            bindIcon.enabled = false;
        }
        
        // 슬롯 배경 (활성화)
        if (slotImage != null)
        {
            slotImage.enabled = true;
        }
        
        if (showDebugLogs)
            Debug.Log($"📦 [InventorySlot] 재료 설정: {data.displayName} x{materialStack.count}");
    }
    
    /// <summary>
    /// 슬롯 초기화
    /// </summary>
    public void ClearSlot()
    {
        // 장비 데이터 초기화
        equipmentData = null;
        itemInstanceId = default;
        
        // 재료 데이터 초기화
        currentMaterial = null;
        
        // UI 초기화
        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false;
        }
        
        // 수량 텍스트 숨김
        if (countText != null)
        {
            countText.gameObject.SetActive(false);
        }
        
        // 테두리 숨김
        if (rarityBorder != null)
        {
            rarityBorder.enabled = false;
            rarityBorder.gameObject.SetActive(false); // ⭐ GameObject도 비활성화
        }
        
        // ItemIconGradeFrame (GradeBoard) 숨김
        if (itemIconGradeFrame != null)
        {
            itemIconGradeFrame.gameObject.SetActive(false);
        }
        
        // 귀속 아이콘 숨김
        if (bindIcon != null)
        {
            bindIcon.enabled = false;
        }
        
        // 슬롯 배경
        if (slotImage != null)
        {
            slotImage.enabled = true;
        }
        
        isBound = false;
        isSelected = false;
    }
    
    /// <summary>
    /// 재료인지 확인
    /// </summary>
    public bool IsMaterial()
    {
        return currentMaterial.HasValue;
    }
    
    /// <summary>
    /// 현재 재료 타입 가져오기
    /// </summary>
    public MaterialType? GetMaterialType()
    {
        return currentMaterial;
    }
} 