using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 🏪 상점 재고 관리자
/// 상점에서 판매할 아이템 목록을 관리하고 필터링 제공
/// </summary>
public class ShopInventoryManager : MonoBehaviour
{
    public static ShopInventoryManager Instance { get; private set; }
    
    [Header("📦 상점 재고 설정")]
    [SerializeField] private bool loadAllEquipmentData = true;    // 모든 EquipmentData 자동 로드
    [SerializeField] private List<EquipmentData> manualShopItems = new List<EquipmentData>(); // 수동 설정 아이템
    
    [Header("🎯 진열 정책")]
    [SerializeField] private int maxItemsPerClass = 4;           // 클래스당 최대 아이템 수
    [SerializeField] private bool sortByGradeAscending = true;   // 등급 낮은 순으로 정렬
    [SerializeField] private bool showOnlyTradableItems = true;  // 거래 가능한 아이템만 표시
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 캐시된 상점 아이템들
    private Dictionary<EquipmentType, List<EquipmentData>> shopItemsByType = new Dictionary<EquipmentType, List<EquipmentData>>();
    private List<EquipmentData> allShopItems = new List<EquipmentData>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 🗑️ 제거: DontDestroyOnLoad(gameObject);
            // 로비에서만 사용하는 상점은 씬과 함께 파괴되어야 함
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 🔧 수정: 게임 시작 시에는 로드하지 않음 (Lazy Loading)
        if (showDebugLogs)
            Debug.Log("🏪 [ShopInventoryManager] 준비 완료 - 상점 열기 시 로드 예정");
    }
    
    /// <summary>
    /// 🔧 수정: 상점 재고 초기화 (Lazy Loading)
    /// </summary>
    private void InitializeShopInventory()
    {
        LoadShopItems();
        OrganizeItemsByType();
        
        if (showDebugLogs)
        {
            Debug.Log($"✅ [ShopInventoryManager] 상점 재고 초기화 완료");
            Debug.Log($"📦 [ShopInventoryManager] 총 {allShopItems.Count}개 아이템 로드됨");
        }
    }
    
    /// <summary>
    /// 상점 아이템 로드
    /// </summary>
    private void LoadShopItems()
    {
        allShopItems.Clear();
        
        if (loadAllEquipmentData)
        {
            // Resources/Equipment 폴더의 모든 EquipmentData 로드
            EquipmentData[] allEquipment = Resources.LoadAll<EquipmentData>("Equipment");
            
            foreach (var equipment in allEquipment)
            {
                if (IsValidShopItem(equipment))
                {
                    allShopItems.Add(equipment);
                }
            }
            
            if (showDebugLogs)
                Debug.Log($"📦 [ShopInventoryManager] Resources에서 {allEquipment.Length}개 중 {allShopItems.Count}개 선택됨");
        }
        
        // 수동 설정 아이템 추가
        foreach (var item in manualShopItems)
        {
            if (item != null && IsValidShopItem(item) && !allShopItems.Contains(item))
            {
                allShopItems.Add(item);
            }
        }
    }
    
    /// <summary>
    /// 유효한 상점 아이템인지 확인
    /// </summary>
    private bool IsValidShopItem(EquipmentData equipment)
    {
        if (equipment == null) return false;
        
        // 거래 가능한 아이템만 표시 옵션
        if (showOnlyTradableItems && !equipment.isTradable)
        {
            return false;
        }
        
        // 구매 가격이 0 이상인 아이템만
        if (equipment.buyPrice <= 0)
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 아이템을 타입별로 분류
    /// </summary>
    private void OrganizeItemsByType()
    {
        shopItemsByType.Clear();
        
        // 🔧 수정: PlayerDataManager 방식과 동일하게 이름 기반 분류
        var weaponItems = allShopItems.Where(item => item.equipmentType == EquipmentType.Weapon).ToList();

        var armorItems = allShopItems.Where(item => 
            item.equipmentType == EquipmentType.Armor && 
            !item.equipmentName.ToLower().Contains("boots") && 
            !item.equipmentName.ToLower().Contains("신발") && 
            !item.equipmentName.ToLower().Contains("부츠")
        ).ToList();

        var bootsItems = allShopItems.Where(item => 
            item.equipmentType == EquipmentType.Armor && 
            (item.equipmentName.ToLower().Contains("boots") || 
             item.equipmentName.ToLower().Contains("신발") || 
             item.equipmentName.ToLower().Contains("부츠"))
        ).ToList();

        var accessoryItems = allShopItems.Where(item => item.equipmentType == EquipmentType.Accessory).ToList();

        // 정렬 (등급별) - 신발 추가
        if (sortByGradeAscending)
        {
            weaponItems = weaponItems.OrderBy(item => (int)item.itemGrade).ToList();
            armorItems = armorItems.OrderBy(item => (int)item.itemGrade).ToList();
            bootsItems = bootsItems.OrderBy(item => (int)item.itemGrade).ToList();
            accessoryItems = accessoryItems.OrderBy(item => (int)item.itemGrade).ToList();
        }
        else
        {
            weaponItems = weaponItems.OrderByDescending(item => (int)item.itemGrade).ToList();
            armorItems = armorItems.OrderByDescending(item => (int)item.itemGrade).ToList();
            bootsItems = bootsItems.OrderByDescending(item => (int)item.itemGrade).ToList();
            accessoryItems = accessoryItems.OrderByDescending(item => (int)item.itemGrade).ToList();
        }

        shopItemsByType[EquipmentType.Weapon] = weaponItems;
        shopItemsByType[EquipmentType.Armor] = armorItems;
        shopItemsByType[EquipmentType.Accessory] = bootsItems;  // 🔧 신발을 Accessory 탭에 할당

        if (showDebugLogs)
        {
            Debug.Log($"🗂️ [ShopInventoryManager] 무기: {weaponItems.Count}개, 방어구: {armorItems.Count}개, 신발: {bootsItems.Count}개, 악세서리: {accessoryItems.Count}개");
        }
    }
    
    /// <summary>
    /// 특정 타입의 아이템 목록 조회
    /// </summary>
    public List<EquipmentData> GetItemsByType(EquipmentType equipmentType)
    {
        if (shopItemsByType.ContainsKey(equipmentType))
        {
            return shopItemsByType[equipmentType];
        }
        
        return new List<EquipmentData>();
    }
    
    /// <summary>
    /// 클래스별 아이템 필터링 (진열용)
    /// </summary>
    public List<EquipmentData> GetItemsByTypeAndClass(EquipmentType equipmentType, PlayerClass playerClass)
    {
        var typeItems = GetItemsByType(equipmentType);
        
        // 클래스 필터링
        var classItems = typeItems.Where(item => 
            item.usableClass == PlayerClass.None || // 모든 클래스 사용 가능
            item.usableClass == playerClass
        ).ToList();
        
        // 최대 개수 제한
        if (classItems.Count > maxItemsPerClass)
        {
            classItems = classItems.Take(maxItemsPerClass).ToList();
        }
        
        return classItems;
    }
    
    /// <summary>
    /// 진열용 3x4 그리드 데이터 생성
    /// </summary>
    public EquipmentData[,] GetDisplayGrid(EquipmentType equipmentType)
    {
        // 3열(클래스) x 4행(아이템) 그리드
        EquipmentData[,] grid = new EquipmentData[3, 4];
        
        // 각 클래스별 아이템 배치
        PlayerClass[] classes = { PlayerClass.Warrior, PlayerClass.Assasin, PlayerClass.Wizard };
        
        for (int col = 0; col < 3; col++)
        {
            var classItems = GetItemsByTypeAndClass(equipmentType, classes[col]);
            
            for (int row = 0; row < 4; row++)
            {
                if (row < classItems.Count)
                {
                    grid[col, row] = classItems[row];
                }
                else
                {
                    grid[col, row] = null; // 빈 슬롯
                }
            }
        }
        
        return grid;
    }
    
    /// <summary>
    /// 아이템 재고 새로고침 (런타임 업데이트용)
    /// </summary>
    public void RefreshInventory()
    {
        InitializeShopInventory();
        
        if (showDebugLogs)
            Debug.Log("🔄 [ShopInventoryManager] 상점 재고 새로고침 완료");
    }
    
    /// <summary>
    /// 🆕 상점 열기 시 아이템 로드 (성능 최적화)
    /// </summary>
    public void LoadItemsForShop()
    {
        if (allShopItems.Count == 0)
        {
            InitializeShopInventory();
        }
        else
        {
            // 이미 로드된 경우 간단한 갱신만
            RefreshInventory();
        }
        
        if (showDebugLogs)
            Debug.Log($"🏪 [ShopInventoryManager] 상점용 아이템 로드 완료: {allShopItems.Count}개");
    }
    
    /// <summary>
    /// 특정 아이템 조회
    /// </summary>
    public EquipmentData GetItemByID(string itemID)
    {
        return allShopItems.FirstOrDefault(item => item.itemID == itemID);
    }
}
