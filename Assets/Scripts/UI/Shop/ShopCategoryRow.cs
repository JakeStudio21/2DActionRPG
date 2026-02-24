using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Shop;

/// <summary>
/// 🏪 상점 카테고리 행 (Phase 2)
/// - 하나의 장비 카테고리 (예: Weapon)를 표시
/// - D → C → B → A 등급 순서로 최대 4개 아이템 가로 표시
/// </summary>
public class ShopCategoryRow : MonoBehaviour
{
    [Header("🎯 카테고리 정보")]
    [SerializeField] private TMP_Text categoryNameText;     // 카테고리 이름 (예: "무기")
    [SerializeField] private Image categoryIcon;            // 카테고리 아이콘 (옵션)
    
    [Header("🎨 카테고리 아이콘 스프라이트 (직접 할당)")]
    [SerializeField] private Sprite weaponIcon;             // 무기 아이콘
    [SerializeField] private Sprite armorIcon;              // 갑옷 아이콘
    [SerializeField] private Sprite bootsIcon;              // 신발 아이콘
    [SerializeField] private Sprite helmetIcon;             // 투구 아이콘
    [SerializeField] private Sprite beltIcon;               // 벨트 아이콘
    [SerializeField] private Sprite glovesIcon;             // 장갑 아이콘
    
    [Header("🎯 아이템 슬롯 컨테이너")]
    [SerializeField] private Transform itemSlotsContainer;  // 아이템 슬롯들이 들어갈 부모
    [SerializeField] private GameObject shopItemSlotPrefab; // ShopItemSlot 프리팹
    [SerializeField] private int maxItemsPerRow = 4;        // 최대 4개 (D/C/B/A)
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = false;
    
    // 내부 상태
    private ShopCategory category;
    private List<ShopItemSlot> itemSlots = new List<ShopItemSlot>();
    private List<ShopItemEntry> currentItems = new List<ShopItemEntry>();
    
    // 이벤트: 아이템 클릭 시
    public event System.Action<ItemInstanceID> OnItemClicked;
    
    /// <summary>
    /// 카테고리 데이터 설정
    /// </summary>
    public void SetupCategory(ShopCategory category, List<ShopItemEntry> items)
    {
        Debug.Log($"🔄 [ShopCategoryRow] SetupCategory({category}) 시작 - 아이템 {items?.Count ?? 0}개");
        
        this.category = category;
        this.currentItems = items ?? new List<ShopItemEntry>();
        
        // 카테고리 이름 표시
        if (categoryNameText != null)
        {
            categoryNameText.text = ShopCategoryHelper.GetCategoryName(category);
            categoryNameText.gameObject.SetActive(true);
            
            if (showDebugLogs)
                Debug.Log($"   ✅ 카테고리 이름 설정: {categoryNameText.text}");
        }
        else
        {
            Debug.LogError("   ❌ categoryNameText가 null!");
        }
        
        // 🎨 카테고리 아이콘 표시 (2가지 방법: 직접 할당 우선, Resources.Load 대체)
        if (categoryIcon != null)
        {
            Sprite iconSprite = GetCategoryIconSprite(category);
            
            if (iconSprite != null)
            {
                categoryIcon.sprite = iconSprite;
                categoryIcon.gameObject.SetActive(true);
                
                Debug.Log($"   ✅ 카테고리 아이콘 설정: {category}");
            }
            else
            {
                // ⚠️ 아이콘을 찾지 못하면 비활성화하지 않고 경고만 출력 (Z-ORDER 방식 유지)
                Debug.LogWarning($"   ⚠️ 카테고리 아이콘을 찾을 수 없음: {category}");
                Debug.LogWarning($"   💡 ShopCategoryRow 프리팹의 Inspector에서 아이콘 스프라이트를 직접 할당해주세요!");
                
                // 텍스트만 표시하도록 아이콘은 숨김 (SetActive 대신 알파값 조정)
                if (categoryIcon.GetComponent<CanvasGroup>() == null)
                {
                    categoryIcon.gameObject.AddComponent<CanvasGroup>();
                }
                categoryIcon.GetComponent<CanvasGroup>().alpha = 0; // 투명하게
            }
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"   ⚠️ categoryIcon이 null (Inspector에서 할당 필요)");
        }
        
        // 슬롯 생성/업데이트
        EnsureSlotsExist();
        
        // 🆕 itemSlotsContainer 강제 활성화
        if (itemSlotsContainer != null)
        {
            itemSlotsContainer.gameObject.SetActive(true);
        }
        
        UpdateSlots();
        
        if (showDebugLogs)
            Debug.Log($"✅ [ShopCategoryRow] 카테고리 설정 완료: {category} ({currentItems.Count}개 아이템)");
    }
    
    /// <summary>
    /// 필요한 슬롯들이 존재하는지 확인하고 생성
    /// </summary>
    private void EnsureSlotsExist()
    {
        if (showDebugLogs)
            Debug.Log($"[ShopCategoryRow] EnsureSlotsExist() - 슬롯: {itemSlots.Count}/{maxItemsPerRow}");
        
        if (itemSlotsContainer == null)
        {
            Debug.LogError("❌ [ShopCategoryRow] itemSlotsContainer가 null!");
            return;
        }
        
        if (shopItemSlotPrefab == null)
        {
            Debug.LogError("❌ [ShopCategoryRow] shopItemSlotPrefab이 null!");
            return;
        }
        
        // 부족한 슬롯 생성
        while (itemSlots.Count < maxItemsPerRow)
        {
            CreateNewSlot();
        }
        
        if (showDebugLogs)
            Debug.Log($"✅ [ShopCategoryRow] 슬롯 생성 완료 - 총 {itemSlots.Count}개");
    }
    
    /// <summary>
    /// 새 슬롯 생성
    /// </summary>
    private void CreateNewSlot()
    {
        if (shopItemSlotPrefab == null)
        {
            Debug.LogError("❌ [ShopCategoryRow] shopItemSlotPrefab이 null!");
            return;
        }
        
        GameObject slotObj = Instantiate(shopItemSlotPrefab, itemSlotsContainer);
        ShopItemSlot slot = slotObj.GetComponent<ShopItemSlot>();
        
        if (slot != null)
        {
            itemSlots.Add(slot);
            
            // 클릭 이벤트 연결 (ItemInstanceID 버전)
            slot.OnItemClickedV2 += HandleItemClicked;
            
            if (showDebugLogs)
                Debug.Log($"✅ [ShopCategoryRow] 슬롯 생성 완료 - 총 {itemSlots.Count}개");
        }
        else
        {
            Debug.LogError($"❌ [ShopCategoryRow] ShopItemSlot 컴포넌트 없음! ({slotObj.name})");
            Destroy(slotObj);
        }
    }
    
    /// <summary>
    /// 슬롯 업데이트
    /// </summary>
    private void UpdateSlots()
    {
        if (showDebugLogs)
            Debug.Log($"[ShopCategoryRow] UpdateSlots() - 아이템: {currentItems.Count}개");
        
        // 등급순 정렬 (D → C → B → A)
        currentItems.Sort((a, b) => a.grade.CompareTo(b.grade));
        
        // 최대 4개만 표시
        int itemCount = Mathf.Min(currentItems.Count, maxItemsPerRow);
        
        for (int i = 0; i < itemSlots.Count; i++)
        {
            if (i < itemCount && currentItems[i].IsValid())
            {
                // 아이템 데이터 설정 (V2 시스템)
                itemSlots[i].SetEquipmentDataV2(
                    currentItems[i].displayInstanceId,
                    currentItems[i].equipmentData
                );
                itemSlots[i].gameObject.SetActive(true);
            }
            else
            {
                // 빈 슬롯
                itemSlots[i].SetEmpty();
                itemSlots[i].gameObject.SetActive(false);
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"✅ [ShopCategoryRow] UpdateSlots() 완료 - {itemCount}개 표시");
    }
    
    /// <summary>
    /// 아이템 클릭 핸들러
    /// </summary>
    private void HandleItemClicked(ItemInstanceID instanceId)
    {
        if (showDebugLogs)
            Debug.Log($"🛒 [ShopCategoryRow] 아이템 클릭: {instanceId.Value}");
        
        OnItemClicked?.Invoke(instanceId);
    }
    
    /// <summary>
    /// 🎨 카테고리 아이콘 스프라이트 가져오기
    /// - 1순위: Inspector에서 직접 할당된 스프라이트 사용
    /// - 2순위: Resources.Load로 동적 로드
    /// </summary>
    private Sprite GetCategoryIconSprite(ShopCategory category)
    {
        // 1순위: 직접 할당된 스프라이트
        Sprite directSprite = category switch
        {
            ShopCategory.Weapon => weaponIcon,
            ShopCategory.Armor => armorIcon,
            ShopCategory.Boots => bootsIcon,
            ShopCategory.Helmet => helmetIcon,
            ShopCategory.Belt => beltIcon,
            ShopCategory.Gloves => glovesIcon,
            _ => null
        };
        
        if (directSprite != null)
        {
            return directSprite;
        }
        
        // 2순위: Resources.Load (fallback)
        Sprite resourceSprite = ShopCategoryHelper.GetCategoryIcon(category);
        return resourceSprite;
    }
    
    /// <summary>
    /// 카테고리 초기화
    /// </summary>
    public void Clear()
    {
        currentItems.Clear();
        
        foreach (var slot in itemSlots)
        {
            slot.SetEmpty();
            slot.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 정리
    /// </summary>
    private void OnDestroy()
    {
        foreach (var slot in itemSlots)
        {
            if (slot != null)
            {
                slot.OnItemClickedV2 -= HandleItemClicked;
            }
        }
    }
}

