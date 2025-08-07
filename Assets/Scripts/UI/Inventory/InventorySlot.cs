using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 🆕 추가

public class InventorySlot : MonoBehaviour
{
    [Header("🛡️ 장비 데이터 (신규 시스템)")]
    [SerializeField] private EquipmentData equipmentData;
    
    [Header("🎨 UI 컴포넌트")]
    [SerializeField] private Image slotImage;      // 슬롯 배경
    [SerializeField] private Image itemIconImage;  // 아이템 아이콘
    [SerializeField] private Button slotButton;    // 클릭 버튼

    [Header("🎨 UI 메시지")]
    [SerializeField] private GameObject messagePanel; // 메시지 패널 (생성될 예정)
    [SerializeField] private TextMeshProUGUI messageText; // 메시지 텍스트
    
    // 슬롯 상태
    public bool isEmpty => equipmentData == null;
    public bool isSelected = false;

    void Awake()
    {
        // UI 컴포넌트 자동 찾기
        if (slotImage == null) slotImage = GetComponent<Image>(); // 슬롯 배경
        if (slotButton == null) slotButton = GetComponent<Button>();
        
        // 🔑 ItemIcon을 이름으로 정확히 찾기
        if (itemIconImage == null) 
        {
            Transform itemIconTransform = transform.Find("ItemIcon");
            if (itemIconTransform != null)
            {
                itemIconImage = itemIconTransform.GetComponent<Image>();
                Debug.Log($"✅ [InventorySlot] ItemIcon 찾음: {itemIconImage != null}");
            }
            else
            {
                Debug.LogError($"🔴 [InventorySlot] ItemIcon을 찾을 수 없습니다! {gameObject.name}");
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
    
    /// <summary>
    /// 슬롯 클릭 처리
    /// </summary>
    public void OnSlotClicked()
    {
        Debug.Log($"🖱️ [InventorySlot] OnSlotClicked 호출: {gameObject.name}");
        
        // 🆕 빈 슬롯 클릭 방지
        if (equipmentData == null)
        {
            Debug.LogWarning($"⚠️ [InventorySlot] 빈 슬롯 클릭 - 이벤트 무시: {gameObject.name}");
            return;
        }
        
        // 🔧 수정: GetSiblingIndex 대신 정확한 인덱스 계산
        int slotIndex = GetActualSlotIndex();
        Debug.Log($"🖱️ [InventorySlot] 계산된 슬롯 인덱스: {slotIndex}");
        Debug.Log($"🖱️ [InventorySlot] GetSiblingIndex(): {transform.GetSiblingIndex()}");
        Debug.Log($"🖱️ [InventorySlot] equipmentData: {equipmentData.equipmentName}");
        
        // 🎯 단일 진입점: 오직 이벤트 시스템만 사용
        if (PlayerDataManager.Instance != null)
        {
            Debug.Log($"🔗 [InventorySlot] PlayerDataManager.TriggerSlotClicked 호출");
            PlayerDataManager.Instance.TriggerSlotClicked(equipmentData, slotIndex);
            Debug.Log($"✅ [InventorySlot] 단일 이벤트 시스템으로 처리 완료");
        }
        else
        {
            Debug.LogError($"🔴 [InventorySlot] PlayerDataManager를 찾을 수 없습니다!");
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
        }
        else
        {
            // 아이콘 없음 (빈 슬롯)
            itemIconImage.color = Color.clear;
            itemIconImage.sprite = null;
        }
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
    public void SetEquipmentData(EquipmentData data)
    {
        equipmentData = data;
        UpdateSlotVisual();
    }

    /// <summary>
    /// 🆕 정확한 슬롯 인덱스 계산
    /// </summary>
    private int GetActualSlotIndex()
    {
        // ActiveInventory의 자식들 중에서 현재 슬롯의 인덱스 찾기
        var activeInventory = GetComponentInParent<ActiveInventory>();
        if (activeInventory == null) 
        {
            Debug.LogWarning($"⚠️ [InventorySlot] ActiveInventory를 찾을 수 없음 - GetSiblingIndex() 사용");
            return transform.GetSiblingIndex();
        }
        
        Transform activeInventoryTransform = activeInventory.transform;
        
        // ActiveInventory의 직접 자식들만 확인
        for (int i = 0; i < activeInventoryTransform.childCount; i++)
        {
            if (activeInventoryTransform.GetChild(i) == transform)
            {
                Debug.Log($"🔍 [InventorySlot] 정확한 인덱스 찾음: {i} (GetSiblingIndex: {transform.GetSiblingIndex()})");
                return i;
            }
        }
        
        Debug.LogWarning($"⚠️ [InventorySlot] 정확한 인덱스를 찾을 수 없음 - GetSiblingIndex() 사용");
        return transform.GetSiblingIndex();
    }
} 