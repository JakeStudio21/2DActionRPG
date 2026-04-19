using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Shop;

/// <summary>
/// 🏪 상점 구매 패널 (Phase 2)
/// - 클래스 탭 (Warrior/Assassin/Wizard)
/// - 6개 카테고리 세로 리스트
/// - 각 카테고리당 D/C/B/A 4개 아이템 가로 표시
/// </summary>
public class ShopBuyPanel : MonoBehaviour
{
    [Header("🎯 클래스 탭 버튼")]
    [SerializeField] private Button warriorTabButton;
    [SerializeField] private Button assassinTabButton;
    [SerializeField] private Button wizardTabButton;
    
    [Header("🎨 탭 색상")]
    [SerializeField] private Color tabNormalColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color tabSelectedColor = Color.white;
    
    [Header("📦 카테고리 컨테이너")]
    [SerializeField] private Transform categoryListContainer;   // 세로 스크롤 뷰의 Content
    [SerializeField] private GameObject categoryRowPrefab;      // ShopCategoryRow 프리팹
    
    [Header("💰 골드 표시")]
    [SerializeField] private TMP_Text goldText;                 // 플레이어 골드 표시
    
    [Header("📊 디버그")]
    
    // 내부 상태
    private PlayerClass currentClass = PlayerClass.Assasin;   // 기본값: Assasin
    private Dictionary<PlayerClass, Button> classTabButtons = new Dictionary<PlayerClass, Button>();
    private List<ShopCategoryRow> categoryRows = new List<ShopCategoryRow>();
    private bool isInitialized = false;  // 🆕 초기화 완료 플래그
    
    // 이벤트: 아이템 클릭 시
    public event System.Action<ItemInstanceID> OnItemClicked;
    
    void Awake()
    {
        InitializeTabButtons();
    }
    
    void Start()
    {
        SetupEvents();
        
        // ✅ 코루틴으로 지연 로드 (모든 Manager 초기화 대기)
        StartCoroutine(InitialLoadCoroutine());
    }
    
    /// <summary>
    /// 초기 데이터 로드 코루틴 (Manager 초기화 대기)
    /// </summary>
    private System.Collections.IEnumerator InitialLoadCoroutine()
    {
        
        // 1프레임 대기 (모든 Awake() 실행 완료 보장)
        yield return null;
        
        // ShopController 초기화 대기
        int maxRetries = 10;
        int retryCount = 0;
        
        while (ShopController.Instance == null && retryCount < maxRetries)
        {
            Debug.LogWarning($"⏳ [ShopBuyPanel] ShopController 대기 중... ({retryCount + 1}/{maxRetries})");
            yield return new WaitForSeconds(0.1f);
            retryCount++;
        }
        
        if (ShopController.Instance == null)
        {
            Debug.LogError("❌ [ShopBuyPanel] ShopController.Instance가 null입니다!");
            yield break;
        }
        
        // AccountDataManager 확인
        if (AccountDataManager.Instance == null)
        {
            Debug.LogError("❌ [ShopBuyPanel] AccountDataManager.Instance가 null입니다!");
            yield break;
        }
        
        // 데이터 로드
        SwitchToClass(currentClass);
        
        // 🆕 추가 프레임 대기 (모든 ShopItemSlot의 Start() 실행 완료 보장)
        yield return null;
        
        // 🆕 UI 강제 갱신 (Start() 이후 데이터 재설정)
        SwitchToClass(currentClass);
        
        isInitialized = true;
        
    }
    
    void OnEnable()
    {
        
        if (goldText != null)
        {
        }
        
        // ✅ 골드 변경 이벤트 구독 (PlayerDataManager)
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnGoldChanged += UpdateGoldDisplay;
        }
        else
        {
            Debug.LogError($"❌ [ShopBuyPanel] PlayerDataManager.Instance가 NULL입니다!");
        }
        
        // ⭐ V2: AccountDataManager 이벤트도 구독
        if (AccountDataManager.Instance != null)
        {
            AccountDataManager.Instance.OnGoldChanged += UpdateGoldDisplay;
        }
        else
        {
            Debug.LogError($"❌ [ShopBuyPanel] AccountDataManager.Instance가 NULL입니다!");
        }
        
        // ✅ 초기 골드 표시
        int currentGold = PlayerDataManager.Instance?.CurrentGold ?? 0;
        UpdateGoldDisplay(currentGold);
        
        // ✅ Start() 이후에만 데이터 갱신 (상점 재진입 시)
        if (isInitialized)
        {
            SwitchToClass(currentClass);
            
        }
    }
    
    void OnDisable()
    {
        // 골드 변경 이벤트 구독 해제 (PlayerDataManager)
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
        }
        
        // ⭐ V2: AccountDataManager 이벤트 구독 해제
        if (AccountDataManager.Instance != null)
        {
            AccountDataManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
        }
        
    }
    
    /// <summary>
    /// 탭 버튼 초기화
    /// </summary>
    private void InitializeTabButtons()
    {
        classTabButtons[PlayerClass.Warrior] = warriorTabButton;
        classTabButtons[PlayerClass.Assasin] = assassinTabButton;
        classTabButtons[PlayerClass.Wizard] = wizardTabButton;
    }
    
    /// <summary>
    /// 이벤트 설정
    /// </summary>
    private void SetupEvents()
    {
        warriorTabButton?.onClick.AddListener(() => SwitchToClass(PlayerClass.Warrior));
        assassinTabButton?.onClick.AddListener(() => SwitchToClass(PlayerClass.Assasin));
        wizardTabButton?.onClick.AddListener(() => SwitchToClass(PlayerClass.Wizard));
    }
    
    /// <summary>
    /// 클래스 탭 전환
    /// </summary>
    public void SwitchToClass(PlayerClass playerClass)
    {
        currentClass = playerClass;
        
        // 탭 버튼 색상 업데이트
        UpdateTabButtonColors();
        
        // ShopController에서 데이터 가져오기
        LoadShopData(playerClass);
        
    }
    
    /// <summary>
    /// 탭 버튼 색상 업데이트
    /// </summary>
    private void UpdateTabButtonColors()
    {
        foreach (var kvp in classTabButtons)
        {
            Button button = kvp.Value;
            if (button == null) continue;
            
            ColorBlock colors = button.colors;
            colors.normalColor = (kvp.Key == currentClass) ? tabSelectedColor : tabNormalColor;
            button.colors = colors;
        }
    }
    
    /// <summary>
    /// 상점 데이터 로드
    /// </summary>
    private void LoadShopData(PlayerClass playerClass)
    {
        
        if (ShopController.Instance == null)
        {
            Debug.LogError("❌ [ShopBuyPanel] ShopController.Instance가 null입니다!");
            return;
        }
        
        
        // ShopController에서 클래스별 데이터 가져오기
        List<ShopInventoryData> shopData = ShopController.Instance.GetShopDataByClass(playerClass);
        
        
        if (shopData == null || shopData.Count == 0)
        {
            Debug.LogWarning($"⚠️ [ShopBuyPanel] {playerClass} 클래스의 상점 데이터가 없습니다!");
            ClearAllCategories();
            return;
        }
        
        // 각 카테고리의 아이템 개수 확인
        foreach (var data in shopData)
        {
        }
        
        // 카테고리 행 생성/업데이트
        EnsureCategoryRowsExist(shopData.Count);
        
        UpdateCategoryRows(shopData);
        
    }
    
    /// <summary>
    /// 필요한 카테고리 행이 존재하는지 확인하고 생성
    /// </summary>
    private void EnsureCategoryRowsExist(int requiredCount)
    {
        
        if (categoryListContainer == null)
        {
            Debug.LogError("   ❌ categoryListContainer가 null!");
            return;
        }
        
        if (categoryRowPrefab == null)
        {
            Debug.LogError("   ❌ categoryRowPrefab이 null!");
            return;
        }
        
        
        // 부족한 행 생성
        int rowsToCreate = requiredCount - categoryRows.Count;
        
        while (categoryRows.Count < requiredCount)
        {
            CreateNewCategoryRow();
        }
        
        // 초과 행 비활성화
        for (int i = requiredCount; i < categoryRows.Count; i++)
        {
            categoryRows[i].gameObject.SetActive(false);
        }
        
    }
    
    /// <summary>
    /// 새 카테고리 행 생성
    /// </summary>
    private void CreateNewCategoryRow()
    {
        
        if (categoryRowPrefab == null)
        {
            Debug.LogError("      ❌ categoryRowPrefab이 null!");
            return;
        }
        
        if (categoryListContainer == null)
        {
            Debug.LogError("      ❌ categoryListContainer가 null!");
            return;
        }
        
        GameObject rowObj = Instantiate(categoryRowPrefab, categoryListContainer);
        
        ShopCategoryRow row = rowObj.GetComponent<ShopCategoryRow>();
        
        if (row == null)
        {
            Debug.LogError($"      ❌ ShopCategoryRow 컴포넌트를 찾을 수 없습니다! (GameObject: {rowObj.name})");
            Debug.LogError($"      📦 GameObject의 컴포넌트 목록:");
            foreach (Component comp in rowObj.GetComponents<Component>())
            {
                Debug.LogError($"         - {comp.GetType().Name}");
            }
            Destroy(rowObj);
            return;
        }
        
        categoryRows.Add(row);
        
        // 아이템 클릭 이벤트 연결
        row.OnItemClicked += HandleItemClicked;
    }
    
    /// <summary>
    /// 카테고리 행 업데이트
    /// </summary>
    private void UpdateCategoryRows(List<ShopInventoryData> shopData)
    {
        
        // ShopCategory 순서로 정렬 (Weapon → Armor → Boots → Helmet → Belt → Gloves)
        var sortedData = new List<ShopInventoryData>(shopData);
        sortedData.Sort((a, b) => GetCategoryOrder(a.slot).CompareTo(GetCategoryOrder(b.slot)));
        
        for (int i = 0; i < sortedData.Count && i < categoryRows.Count; i++)
        {
            ShopCategory category = EquipmentSlotToShopCategory(sortedData[i].slot);
            
            categoryRows[i].SetupCategory(category, sortedData[i].items);
            categoryRows[i].gameObject.SetActive(true);
            
        }
        
        
        // 🆕 Layout 강제 갱신 (UI가 즉시 표시되도록)
        StartCoroutine(ForceRefreshLayoutNextFrame());
    }
    
    /// <summary>
    /// 다음 프레임에 Layout 강제 갱신
    /// </summary>
    private System.Collections.IEnumerator ForceRefreshLayoutNextFrame()
    {
        yield return null; // 1프레임 대기
        
        if (categoryListContainer != null)
        {
            RectTransform rectTransform = categoryListContainer.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                
                // Canvas도 강제 갱신
                Canvas.ForceUpdateCanvases();
            }
        }
    }
    
    /// <summary>
    /// 카테고리 순서 반환
    /// </summary>
    private int GetCategoryOrder(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.MainWeapon => 0,
            EquipmentSlot.Armor => 1,
            EquipmentSlot.Boots => 2,
            EquipmentSlot.Helmet => 3,
            EquipmentSlot.Belt => 4,
            EquipmentSlot.Gloves => 5,
            _ => 999
        };
    }
    
    /// <summary>
    /// EquipmentSlot → ShopCategory 변환
    /// </summary>
    private ShopCategory EquipmentSlotToShopCategory(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.MainWeapon => ShopCategory.Weapon,
            EquipmentSlot.Armor => ShopCategory.Armor,
            EquipmentSlot.Boots => ShopCategory.Boots,
            EquipmentSlot.Helmet => ShopCategory.Helmet,
            EquipmentSlot.Belt => ShopCategory.Belt,
            EquipmentSlot.Gloves => ShopCategory.Gloves,
            _ => ShopCategory.Weapon
        };
    }
    
    /// <summary>
    /// 아이템 클릭 핸들러
    /// </summary>
    private void HandleItemClicked(ItemInstanceID instanceId)
    {
        
        OnItemClicked?.Invoke(instanceId);
    }
    
    /// <summary>
    /// 모든 카테고리 초기화
    /// </summary>
    private void ClearAllCategories()
    {
        foreach (var row in categoryRows)
        {
            row.Clear();
            row.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// 정리
    /// </summary>
    private void OnDestroy()
    {
        foreach (var row in categoryRows)
        {
            if (row != null)
            {
                row.OnItemClicked -= HandleItemClicked;
            }
        }
        
        // 골드 이벤트 구독 해제 (안전장치)
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
        }
        
        // ⭐ V2: AccountDataManager 이벤트 구독 해제
        if (AccountDataManager.Instance != null)
        {
            AccountDataManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
        }
    }
    
    /// <summary>
    /// 골드 표시 업데이트
    /// </summary>
    private void UpdateGoldDisplay(int gold)
    {
        
        if (goldText != null)
        {
            goldText.text = gold.ToString();
        }
        else
        {
            Debug.LogError($"❌ [ShopBuyPanel] goldText가 NULL입니다! Inspector에서 할당해주세요.");
        }
    }
}

