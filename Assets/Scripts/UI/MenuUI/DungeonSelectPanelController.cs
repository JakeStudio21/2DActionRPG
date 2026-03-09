using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StageSystem;

/// <summary>
/// 던전 선택 패널 컨트롤러
/// 던전 카테고리 → 개별 던전 2단계 구조 지원
/// </summary>
public class DungeonSelectPanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dungeonSelectPanel;
    [SerializeField] private Button backButton;
    
    [Header("Layer GameObjects")]
    [SerializeField] private GameObject categoryLayer;   // ⭐ CategoryLayer GameObject
    [SerializeField] private GameObject dungeonLayer;    // ⭐ DungeonLayer GameObject
    
    [Header("Category Layer")]
    [SerializeField] private Transform categoryListContainer;
    [SerializeField] private GameObject categoryButtonPrefab;
    
    [Header("Dungeon Layer")]
    [SerializeField] private Transform dungeonListContainer;
    [SerializeField] private GameObject dungeonButtonPrefab;
    
    [Header("Info Panel")]
    [SerializeField] private GameObject dungeonInfoPanel;
    [SerializeField] private TextMeshProUGUI dungeonNameText;
    [SerializeField] private TextMeshProUGUI dungeonDescriptionText;
    [SerializeField] private TextMeshProUGUI recommendedLevelText;
    [SerializeField] private TextMeshProUGUI waveCountText;
    [SerializeField] private TextMeshProUGUI rewardPreviewText;
    [SerializeField] private Button playButton;
    
    [Header("Background Images")]
    [SerializeField] private Sprite categoryBg_DailyBoss;    // 데일리 보스 배경
    [SerializeField] private Sprite categoryBg_WeeklyRaid;   // 주간 레이드 배경
    [SerializeField] private Sprite categoryBg_MaterialFarm; // 재료 파밍 배경
    
    [Header("Dungeon Background Images")]
    [SerializeField] private Sprite dungeonBg_DG01_SB01_Bind;   // 속박저항 정수 던전 ⭐
    [SerializeField] private Sprite dungeonBg_DG01_SB02_Poison; // 독저항 정수 던전 ⭐
    [SerializeField] private Sprite dungeonBg_DG01_SB03_Slow;   // 둔화저항 정수 던전 ⭐
    [SerializeField] private Sprite dungeonBg_DG01_SB04_Burn;   // 화상저항 정수 던전 ⭐
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    // 이벤트 ⭐
    public System.Action<string> OnDungeonPlayButtonClicked; // sceneName 전달
    public System.Action OnDungeonBackButtonClicked;
    
    // 상태
    private string selectedCategoryId = "";
    private string selectedDungeonId = "";
    private List<CategoryInfo> categories = new List<CategoryInfo>();
    private Dictionary<string, List<DungeonInfo>> dungeonsByCategory = new Dictionary<string, List<DungeonInfo>>();
    
    // 카테고리 정보 구조체 ⭐
    [System.Serializable]
    public class CategoryInfo
    {
        public string categoryId;           // "Daily_Boss_Dungeon"
        public string categoryName;         // "데일리 보스 던전"
        public string categoryIcon;         // "📅" (이모지)
        public Sprite backgroundImage;      // 배경 이미지 ⭐
        public bool isUnlocked;            // 해금 여부
        public bool isComingSoon;          // Coming Soon 표시
        public int displayOrder;           // 표시 순서 (1, 2, 3)
    }
    
    // 던전 정보 구조체
    [System.Serializable]
    public class DungeonInfo
    {
        public string dungeonId;
        public string dungeonName;
        public string categoryId;
        public int recommendedLevel;
        public bool isUnlocked;
        public Sprite backgroundImage;     // 배경 이미지 ⭐
        public Sprite iconImage;           // 던전 아이콘 (선택)
    }
    
    // 보상 슬롯 데이터 구조체 ⭐
    [System.Serializable]
    public class RewardSlotData
    {
        public enum RewardType { Equipment, Material }
        
        public RewardType rewardType;
        public EquipmentData equipmentData; // 장비일 경우
        public MaterialStack materialStack; // 재료일 경우
        public int amount;                  // 수량
        public float dropRate;              // 드랍률 (0.0 ~ 1.0)
    }
    
    private void Awake()
    {
        // 🏰 초기 상태: DungeonLayer 전체 비활성화
        if (dungeonLayer != null)
        {
            dungeonLayer.SetActive(false);
        }
        
        if (dungeonInfoPanel != null)
        {
            dungeonInfoPanel.SetActive(false);
        }
        
        if (enableDebugLogs)
            Debug.Log("[DungeonSelect] 초기화: DungeonLayer 비활성화");
    }
    
    /// <summary>
    /// 패널 활성화 시 이벤트 구독 (정석 방식) ✅
    /// </summary>
    private void OnEnable()
    {
        // PlayerDataManager 레벨 변경 이벤트 구독
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnLevelChanged += OnPlayerLevelChanged;
            
            if (enableDebugLogs)
                Debug.Log("[DungeonSelect] OnLevelChanged 이벤트 구독 완료");
        }
    }
    
    /// <summary>
    /// 패널 비활성화 시 이벤트 구독 해제 (메모리 누수 방지) ✅
    /// </summary>
    private void OnDisable()
    {
        // PlayerDataManager 레벨 변경 이벤트 구독 해제
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnLevelChanged -= OnPlayerLevelChanged;
            
            if (enableDebugLogs)
                Debug.Log("[DungeonSelect] OnLevelChanged 이벤트 구독 해제");
        }
    }
    
    private void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);
        
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
        
        InitializeDungeonData();
    }
    
    /// <summary>
    /// 패널 표시
    /// </summary>
    public void ShowPanel()
    {
        if (dungeonSelectPanel != null)
        {
            dungeonSelectPanel.SetActive(true);
            RefreshCategoryList();
            
            if (enableDebugLogs)
                Debug.Log("[DungeonSelect] 패널 표시");
        }
    }
    
    /// <summary>
    /// 패널 숨김
    /// </summary>
    public void HidePanel()
    {
        if (dungeonSelectPanel != null)
        {
            dungeonSelectPanel.SetActive(false);
            
            // 🏰 상태 초기화 (다음 오픈 시 깨끗한 상태)
            if (dungeonLayer != null)
            {
                dungeonLayer.SetActive(false);
            }
            
            if (dungeonInfoPanel != null)
            {
                dungeonInfoPanel.SetActive(false);
            }
            
            selectedCategoryId = "";
            selectedDungeonId = "";
            
            if (enableDebugLogs)
                Debug.Log("[DungeonSelect] 패널 숨김 (상태 초기화)");
        }
    }
    
    /// <summary>
    /// 던전 데이터 초기화 (Phase 1: 하드코딩)
    /// </summary>
    private void InitializeDungeonData()
    {
        categories.Clear();
        dungeonsByCategory.Clear();
        
        // ========================================
        // 🏰 카테고리 3개 정의
        // ========================================
        categories = new List<CategoryInfo>
        {
            new CategoryInfo
            {
                categoryId = "Daily_Boss_Dungeon",
                categoryName = "정령의 가호 던전", // ✅ 변경
                categoryIcon = "📅",
                backgroundImage = categoryBg_DailyBoss,
                isUnlocked = true,
                isComingSoon = false,
                displayOrder = 1
            },
            new CategoryInfo
            {
                categoryId = "Weekly_Raid",
                categoryName = "주간 레이드 던전", // ✅ 변경
                categoryIcon = "🗓️",
                backgroundImage = categoryBg_WeeklyRaid,
                isUnlocked = false,
                isComingSoon = true, // ⭐ Coming Soon!
                displayOrder = 2
            },
            new CategoryInfo
            {
                categoryId = "Material_Farm",
                categoryName = "재료 파밍 던전", // ✅ 변경
                categoryIcon = "💎",
                backgroundImage = categoryBg_MaterialFarm,
                isUnlocked = false,
                isComingSoon = true, // ⭐ Coming Soon!
                displayOrder = 3
            }
        };
        
        // ========================================
        // 🏰 카테고리 1: Daily_Boss_Dungeon (데일리 보스 던전)
        // ========================================
        List<DungeonInfo> dailyBossDungeons = new List<DungeonInfo>
        {
            // 던전 1: 속박저항 정수 던전 (Bind Resistance)
            new DungeonInfo
            {
                dungeonId = "DG01_SB01_Bind",  // ⭐ 네이밍 변경
                dungeonName = "속박저항 정수 던전",
                categoryId = "Daily_Boss_Dungeon",
                recommendedLevel = 10,
                isUnlocked = GetPlayerLevel() >= 10, // ✅ 동적 체크
                backgroundImage = dungeonBg_DG01_SB01_Bind, // ⭐ 배경 이미지
                iconImage = null
            },
            
            // 던전 2: 독저항 정수 던전 (Poison Resistance) ⭐ 신규
            new DungeonInfo
            {
                dungeonId = "DG01_SB02_Poison",
                dungeonName = "독저항 정수 던전",
                categoryId = "Daily_Boss_Dungeon",
                recommendedLevel = 20, // ✅ 10 → 20
                isUnlocked = GetPlayerLevel() >= 20, // ✅ 동적 체크
                backgroundImage = dungeonBg_DG01_SB02_Poison, // ⭐ 배경 이미지
                iconImage = null
            },
            
            // 던전 3: 둔화저항 정수 던전 (Slow Resistance) ⭐ 신규
            new DungeonInfo
            {
                dungeonId = "DG01_SB03_Slow",
                dungeonName = "둔화저항 정수 던전",
                categoryId = "Daily_Boss_Dungeon",
                recommendedLevel = 30, // ✅ 10 → 30
                isUnlocked = GetPlayerLevel() >= 30, // ✅ 동적 체크
                backgroundImage = dungeonBg_DG01_SB03_Slow, // ⭐ 배경 이미지
                iconImage = null
            },
            
            // 던전 4: 화상저항 정수 던전 (Burn Resistance) ⭐ 신규
            new DungeonInfo
            {
                dungeonId = "DG01_SB04_Burn",
                dungeonName = "화상저항 정수 던전",
                categoryId = "Daily_Boss_Dungeon",
                recommendedLevel = 40, // ✅ 10 → 40
                isUnlocked = GetPlayerLevel() >= 40, // ✅ 동적 체크
                backgroundImage = dungeonBg_DG01_SB04_Burn, // ⭐ 배경 이미지
                iconImage = null
            }
        };
        
        dungeonsByCategory["Daily_Boss_Dungeon"] = dailyBossDungeons;
        
        // ========================================
        // 🏰 카테고리 2: Weekly_Raid (주간 레이드) - Coming Soon
        // ========================================
        dungeonsByCategory["Weekly_Raid"] = new List<DungeonInfo>();
        
        // ========================================
        // 🏰 카테고리 3: Material_Farm (재료 파밍) - Coming Soon
        // ========================================
        dungeonsByCategory["Material_Farm"] = new List<DungeonInfo>();
        
        if (enableDebugLogs)
        {
            int totalDungeons = 0;
            foreach (var kvp in dungeonsByCategory)
            {
                totalDungeons += kvp.Value.Count;
            }
            Debug.Log($"[DungeonSelect] 던전 데이터 초기화: {categories.Count}개 카테고리, {totalDungeons}개 던전");
        }
    }
    
    /// <summary>
    /// 카테고리 목록 갱신
    /// </summary>
    private void RefreshCategoryList()
    {
        // ⭐ dungeonNameText 초기화 (카테고리 선택 전)
        if (dungeonNameText != null)
        {
            dungeonNameText.text = ""; // 빈 텍스트로 초기화
        }
        
        // 기존 버튼 제거
        if (categoryListContainer != null)
        {
            foreach (Transform child in categoryListContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        // 카테고리 버튼 생성 (categories List 사용)
        var sortedCategories = categories.OrderBy(c => c.displayOrder).ToList();
        foreach (var categoryInfo in sortedCategories)
        {
            CreateCategoryButton(categoryInfo);
        }
        
        if (enableDebugLogs)
            Debug.Log($"[DungeonSelect] 카테고리 목록 갱신: {categories.Count}개 표시");
        
        // 🏰 DungeonLayer 전체 비활성화 (배경 포함)
        if (dungeonLayer != null)
        {
            dungeonLayer.SetActive(false);
            
            if (enableDebugLogs)
                Debug.Log("[DungeonSelect] DungeonLayer 비활성화 (배경 포함)");
        }
        
        // 정보 패널 숨김
        if (dungeonInfoPanel != null)
        {
            dungeonInfoPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 카테고리 버튼 생성
    /// </summary>
    private void CreateCategoryButton(CategoryInfo categoryInfo)
    {
        if (categoryButtonPrefab == null || categoryListContainer == null)
        {
            Debug.LogError("[DungeonSelect] CategoryButton Prefab 또는 Container가 없습니다!");
            return;
        }
        
        GameObject btnObj = Instantiate(categoryButtonPrefab, categoryListContainer);
        CategoryButtonUI buttonUI = btnObj.GetComponent<CategoryButtonUI>();
        
        if (buttonUI != null)
        {
            // 배경 이미지 설정 ⭐
            if (categoryInfo.backgroundImage != null)
            {
                buttonUI.SetBackgroundImage(categoryInfo.backgroundImage);
            }
            
            // 카테고리 이름 설정
            string displayName = $"{categoryInfo.categoryIcon} {categoryInfo.categoryName}";
            buttonUI.SetName(displayName);
            
            // Coming Soon 처리 ⭐
            if (categoryInfo.isComingSoon)
            {
                buttonUI.SetComingSoon(true);
                buttonUI.SetInteractable(false);
                
                if (enableDebugLogs)
                    Debug.Log($"[DungeonSelect] 카테고리 생성 (Coming Soon): {categoryInfo.categoryName}");
            }
            else if (!categoryInfo.isUnlocked)
            {
                // 잠금 처리 (Coming Soon 아님)
                buttonUI.SetLocked(true);
                buttonUI.SetInteractable(false);
                
                if (enableDebugLogs)
                    Debug.Log($"[DungeonSelect] 카테고리 생성 (잠금): {categoryInfo.categoryName}");
            }
            else
            {
                // 정상 활성화
                buttonUI.SetComingSoon(false);
                buttonUI.SetLocked(false);
                buttonUI.SetInteractable(true);
                
                if (buttonUI.button != null)
                {
                    buttonUI.button.onClick.AddListener(() => OnCategorySelected(categoryInfo.categoryId));
                }
                
                if (enableDebugLogs)
                    Debug.Log($"[DungeonSelect] 카테고리 생성 (활성화): {categoryInfo.categoryName}");
            }
        }
        else
        {
            // Fallback: CategoryButtonUI 없으면 기존 방식
            Debug.LogWarning("[DungeonSelect] CategoryButtonUI 컴포넌트가 없습니다. 기본 버튼으로 처리합니다.");
            
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI text = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            
            if (text != null)
            {
                text.text = $"{categoryInfo.categoryIcon} {categoryInfo.categoryName}";
            }
            
            if (btn != null && categoryInfo.isUnlocked && !categoryInfo.isComingSoon)
            {
                btn.onClick.AddListener(() => OnCategorySelected(categoryInfo.categoryId));
            }
            else if (btn != null)
            {
                btn.interactable = false;
            }
        }
    }
    
    /// <summary>
    /// 카테고리 선택 처리
    /// </summary>
    private void OnCategorySelected(string categoryId)
    {
        selectedCategoryId = categoryId;
        selectedDungeonId = ""; // 던전 선택 초기화
        
        RefreshDungeonList(categoryId);
        
        if (enableDebugLogs)
            Debug.Log($"[DungeonSelect] 카테고리 선택: {categoryId}");
    }
    
    /// <summary>
    /// 던전 목록 갱신
    /// </summary>
    private void RefreshDungeonList(string categoryId)
    {
        if (!dungeonsByCategory.ContainsKey(categoryId))
        {
            Debug.LogError($"[DungeonSelect] 존재하지 않는 카테고리: {categoryId}");
            return;
        }
        
        // ⭐ 카테고리 이름 표시 (DungeonLayer의 dungeonNameText에 표시)
        if (dungeonNameText != null)
        {
            var category = categories.Find(c => c.categoryId == categoryId);
            if (category != null)
            {
                dungeonNameText.text = category.categoryName; // ✅ 카테고리 이름!
                
                if (enableDebugLogs)
                    Debug.Log($"[DungeonSelect] 카테고리 이름 표시: {category.categoryName}");
            }
        }
        
        // 🏰 DungeonLayer 전체 활성화 (배경 포함!)
        if (dungeonLayer != null)
        {
            dungeonLayer.SetActive(true);
            
            if (enableDebugLogs)
                Debug.Log($"[DungeonSelect] DungeonLayer 활성화 (카테고리: {categoryId})");
        }
        
        // 기존 버튼 제거
        if (dungeonListContainer != null)
        {
            foreach (Transform child in dungeonListContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        // 던전 버튼 생성
        List<DungeonInfo> dungeons = dungeonsByCategory[categoryId];
        foreach (var dungeonInfo in dungeons)
        {
            CreateDungeonButton(dungeonInfo);
        }
        
        if (enableDebugLogs)
            Debug.Log($"[DungeonSelect] 던전 목록 갱신: {dungeons.Count}개 던전 표시");
        
        // 정보 패널 숨김 (던전 선택 전까지)
        if (dungeonInfoPanel != null)
        {
            dungeonInfoPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 던전 버튼 생성
    /// </summary>
    private void CreateDungeonButton(DungeonInfo dungeonInfo)
    {
        if (dungeonButtonPrefab == null || dungeonListContainer == null)
        {
            Debug.LogError("[DungeonSelect] DungeonButton Prefab 또는 Container가 없습니다!");
            return;
        }
        
        GameObject btnObj = Instantiate(dungeonButtonPrefab, dungeonListContainer);
        DungeonButtonUI buttonUI = btnObj.GetComponent<DungeonButtonUI>();
        
        if (buttonUI != null)
        {
            // 배경 이미지 설정 ⭐
            if (dungeonInfo.backgroundImage != null)
            {
                buttonUI.SetBackgroundImage(dungeonInfo.backgroundImage);
            }
            
            // 던전 이름 설정
            buttonUI.SetName(dungeonInfo.dungeonName);
            
            // 권장 레벨 설정 (잠금 상태에 따라 다르게 표시)
            if (!dungeonInfo.isUnlocked)
            {
                // 잠긴 던전: "요구레벨 Lv.20" 표시
                buttonUI.SetRecommendedLevel(dungeonInfo.recommendedLevel, isLocked: true);
            }
            else
            {
                // 해금된 던전: "권장 Lv.20" 표시
                buttonUI.SetRecommendedLevel(dungeonInfo.recommendedLevel, isLocked: false);
            }
            
            // ⭐ 보상 슬롯 설정 (신규)
            var config = LoadDungeonConfig(dungeonInfo.dungeonId);
            if (config != null)
            {
                List<RewardSlotData> rewards = ExtractRewardSlots(config);
                buttonUI.SetRewardSlots(rewards);
                
                if (enableDebugLogs)
                    Debug.Log($"[DungeonSelect] 보상 슬롯 설정: {dungeonInfo.dungeonName} - {rewards.Count}개 슬롯");
            }
            
            // 잠금 처리
            if (!dungeonInfo.isUnlocked)
            {
                buttonUI.SetLocked(true);
                buttonUI.SetInteractable(false);
                
                if (enableDebugLogs)
                    Debug.Log($"[DungeonSelect] 던전 생성 (잠금): {dungeonInfo.dungeonName}");
            }
            else
            {
                buttonUI.SetLocked(false);
                buttonUI.SetInteractable(true);
                
                if (buttonUI.button != null)
                {
                    buttonUI.button.onClick.AddListener(() => OnDungeonSelected(dungeonInfo.dungeonId));
                }
                
                if (enableDebugLogs)
                    Debug.Log($"[DungeonSelect] 던전 생성 (활성화): {dungeonInfo.dungeonName}");
            }
        }
        else
        {
            // Fallback: DungeonButtonUI 없으면 기존 방식
            Debug.LogWarning("[DungeonSelect] DungeonButtonUI 컴포넌트가 없습니다. 기본 버튼으로 처리합니다.");
            
            Button btn = btnObj.GetComponent<Button>();
            TextMeshProUGUI text = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            
            if (text != null)
            {
                text.text = dungeonInfo.dungeonName;
            }
            
            if (btn != null)
            {
                if (dungeonInfo.isUnlocked)
                {
                    btn.onClick.AddListener(() => OnDungeonSelected(dungeonInfo.dungeonId));
                }
                else
                {
                    btn.interactable = false;
                    if (text != null)
                    {
                        text.color = Color.gray;
                        text.text = $"🔒 {dungeonInfo.dungeonName}";
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 던전 선택 처리
    /// </summary>
    private void OnDungeonSelected(string dungeonId)
    {
        selectedDungeonId = dungeonId;
        
        // 던전 정보 표시
        DisplayDungeonInfo(dungeonId);
        
        if (enableDebugLogs)
            Debug.Log($"[DungeonSelect] 던전 선택: {dungeonId}");
    }
    
    /// <summary>
    /// 던전 정보 표시
    /// </summary>
    private void DisplayDungeonInfo(string dungeonId)
    {
        var config = LoadDungeonConfig(dungeonId);
        if (config == null)
        {
            Debug.LogError($"[DungeonSelect] DungeonConfig를 찾을 수 없습니다: {dungeonId}");
            return;
        }
        
        // 정보 패널 활성화
        if (dungeonInfoPanel != null)
        {
            dungeonInfoPanel.SetActive(true);
        }
        
        // 던전 정보 표시
        // ⭐ dungeonNameText 업데이트 제거 (카테고리 이름 유지)
        // ❌ 삭제: if (dungeonNameText != null) dungeonNameText.text = config.StageName;
        
        if (dungeonDescriptionText != null)
            dungeonDescriptionText.text = config.Description;
        
        if (recommendedLevelText != null)
            recommendedLevelText.text = $"권장 레벨: Lv.{config.StageBaseLevel}";
        
        if (waveCountText != null)
            waveCountText.text = $"웨이브: {config.WaveCount}개";
        
        // ⭐ 보상 미리보기 (던전 버튼에 이미 표시되므로 생략 가능)
        // 필요하다면 간단한 텍스트만 표시
        if (rewardPreviewText != null)
        {
            rewardPreviewText.text = "보상 아이템은 던전 버튼에 표시됩니다.";
        }
        
        // Play 버튼 활성화
        if (playButton != null)
            playButton.interactable = true;
    }
    
    /// <summary>
    /// 보상 미리보기 텍스트 생성
    /// </summary>
    private string GenerateRewardPreview(StageConfig config)
    {
        List<string> rewardLines = new List<string>();
        
        // 🏅 첫 클리어 보상
        if (config.FirstClearDropTable != null)
        {
            rewardLines.Add("【첫 클리어 보상】");
            
            // 골드
            if (config.FirstClearDropTable.Gold > 0)
            {
                rewardLines.Add($"  • 골드: {config.FirstClearDropTable.Gold}");
            }
            
            // 경험치
            if (config.FirstClearDropTable.Exp > 0)
            {
                rewardLines.Add($"  • 경험치: {config.FirstClearDropTable.Exp}");
            }
            
            // 아이템 (장비 + 재료)
            if (config.FirstClearDropTable.Items != null && config.FirstClearDropTable.Items.Count > 0)
            {
                foreach (var item in config.FirstClearDropTable.Items)
                {
                    string itemName = GetItemDisplayName(item.ItemID);
                    int dropPercent = Mathf.RoundToInt(item.DropRate * 100);
                    rewardLines.Add($"  • {itemName} x{item.Amount} ({dropPercent}%)");
                }
            }
        }
        
        // 🔄 반복 클리어 보상
        if (config.RepeatClearDropTable != null)
        {
            rewardLines.Add("\n【반복 클리어 보상】");
            
            // 골드
            if (config.RepeatClearDropTable.Gold > 0)
            {
                rewardLines.Add($"  • 골드: {config.RepeatClearDropTable.Gold}");
            }
            
            // 경험치
            if (config.RepeatClearDropTable.Exp > 0)
            {
                rewardLines.Add($"  • 경험치: {config.RepeatClearDropTable.Exp}");
            }
            
            // 아이템 (장비 + 재료)
            if (config.RepeatClearDropTable.Items != null && config.RepeatClearDropTable.Items.Count > 0)
            {
                foreach (var item in config.RepeatClearDropTable.Items)
                {
                    string itemName = GetItemDisplayName(item.ItemID);
                    int dropPercent = Mathf.RoundToInt(item.DropRate * 100);
                    rewardLines.Add($"  • {itemName} x{item.Amount} ({dropPercent}%)");
                }
            }
        }
        
        // 보상이 없으면 기본 텍스트
        if (rewardLines.Count == 0)
        {
            return "보상 정보가 없습니다.";
        }
        
        return string.Join("\n", rewardLines);
    }
    
    /// <summary>
    /// 아이템 ID → 표시 이름 변환
    /// </summary>
    private string GetItemDisplayName(string itemId)
    {
        // 1. 장비 아이템 확인
        var equipmentData = ItemTemplateResolver.Load(itemId);
        if (equipmentData != null)
        {
            return equipmentData.equipmentName;
        }
        
        // 2. 재료 아이템 확인
        var materialData = MaterialDatabase.Instance?.GetDataById(itemId);
        if (materialData != null)
        {
            return materialData.displayName;
        }
        
        // 3. 알 수 없는 아이템
        return itemId;
    }
    
    /// <summary>
    /// StageConfig → RewardSlotData 변환 (최대 3개) ⭐
    /// </summary>
    private List<RewardSlotData> ExtractRewardSlots(StageConfig config)
    {
        List<RewardSlotData> rewards = new List<RewardSlotData>();
        
        // FirstClearDropTable 우선 사용
        var dropTable = config.FirstClearDropTable ?? config.RepeatClearDropTable;
        
        if (dropTable == null || dropTable.Items == null || dropTable.Items.Count == 0)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[DungeonSelect] 보상 아이템이 없습니다: {config.StageID}");
            return rewards;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[DungeonSelect] 보상 슬롯 추출 시작: {config.StageID}, 총 {dropTable.Items.Count}개 아이템");
        
        // 최대 3개만 추출
        int maxSlots = Mathf.Min(dropTable.Items.Count, 3);
        
        for (int i = 0; i < maxSlots; i++)
        {
            var item = dropTable.Items[i];
            
            if (enableDebugLogs)
                Debug.Log($"[DungeonSelect] 아이템 {i+1}/{maxSlots}: {item.ItemID} x{item.Amount}");
            
            // 1. 장비 아이템 확인
            var equipmentData = ItemTemplateResolver.Load(item.ItemID);
            if (equipmentData != null)
            {
                rewards.Add(new RewardSlotData
                {
                    rewardType = RewardSlotData.RewardType.Equipment,
                    equipmentData = equipmentData,
                    amount = item.Amount,
                    dropRate = item.DropRate
                });
                
                if (enableDebugLogs)
                    Debug.Log($"  ✅ 장비 아이템: {equipmentData.equipmentName}");
                continue;
            }
            
            // 2. 재료 아이템 확인
            MaterialType materialType = MaterialTypeExtensions.FromItemId(item.ItemID);
            if (materialType != MaterialType.None)
            {
                rewards.Add(new RewardSlotData
                {
                    rewardType = RewardSlotData.RewardType.Material,
                    materialStack = new MaterialStack  // ⭐ 수정: 객체 초기화 구문 사용
                    {
                        materialType = materialType,
                        count = item.Amount,
                        materialTypeName = materialType.ToString(),
                        displayName = materialType.GetDisplayName()
                    },
                    amount = item.Amount,
                    dropRate = item.DropRate
                });
                
                if (enableDebugLogs)
                    Debug.Log($"  ✅ 재료 아이템: {materialType.GetDisplayName()} (MaterialType: {materialType})");
                continue;
            }
            
            // 3. 알 수 없는 아이템 (스킵)
            if (enableDebugLogs)
                Debug.LogWarning($"  ❌ 알 수 없는 아이템 ID: {item.ItemID} (장비도 아니고 재료도 아님)");
        }
        
        if (enableDebugLogs)
            Debug.Log($"[DungeonSelect] 보상 슬롯 추출 완료: {rewards.Count}개 슬롯 생성됨");
        
        return rewards;
    }
    
    /// <summary>
    /// Play 버튼 클릭 처리
    /// </summary>
    private void OnPlayButtonClicked()
    {
        if (string.IsNullOrEmpty(selectedDungeonId))
        {
            Debug.LogWarning("[DungeonSelect] 던전을 선택해주세요!");
            return;
        }
        
        var config = LoadDungeonConfig(selectedDungeonId);
        if (config == null)
        {
            Debug.LogError($"[DungeonSelect] DungeonConfig를 찾을 수 없습니다: {selectedDungeonId}");
            return;
        }
        
        if (enableDebugLogs)
            Debug.Log($"[DungeonSelect] 🏰 던전 입장 이벤트 발행: {selectedDungeonId} -> {config.SceneName}");
        
        // 이벤트 발행 (LobbyUIController가 처리) ⭐
        OnDungeonPlayButtonClicked?.Invoke(config.SceneName);
    }
    
    /// <summary>
    /// 플레이어 레벨 변경 이벤트 핸들러 (정석 방식) ✅
    /// </summary>
    private void OnPlayerLevelChanged(int newLevel)
    {
        if (enableDebugLogs)
            Debug.Log($"🆙 [DungeonSelect] 레벨 변경 감지: Lv.{newLevel} - 던전 해금 상태 갱신 시작");
        
        // 현재 선택된 카테고리의 던전 목록만 갱신
        if (!string.IsNullOrEmpty(selectedCategoryId))
        {
            RefreshDungeonUnlockStates(selectedCategoryId);
        }
    }
    
    /// <summary>
    /// 던전 해금 상태만 효율적으로 갱신 (버튼 재생성 없이) ✅
    /// </summary>
    private void RefreshDungeonUnlockStates(string categoryId)
    {
        if (!dungeonsByCategory.ContainsKey(categoryId))
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[DungeonSelect] 존재하지 않는 카테고리: {categoryId}");
            return;
        }
        
        int currentLevel = GetPlayerLevel();
        List<DungeonInfo> dungeons = dungeonsByCategory[categoryId];
        
        if (enableDebugLogs)
            Debug.Log($"[DungeonSelect] 던전 해금 상태 갱신 시작: 카테고리={categoryId}, 플레이어Lv.{currentLevel}, 던전수={dungeons.Count}");
        
        // dungeonsByCategory의 isUnlocked 값 업데이트
        foreach (var dungeonInfo in dungeons)
        {
            bool wasUnlocked = dungeonInfo.isUnlocked;
            dungeonInfo.isUnlocked = (currentLevel >= dungeonInfo.recommendedLevel);
            
            if (enableDebugLogs && wasUnlocked != dungeonInfo.isUnlocked)
            {
                Debug.Log($"  🔓 [DungeonSelect] 던전 해금 상태 변경: {dungeonInfo.dungeonName} (요구Lv.{dungeonInfo.recommendedLevel}) → {(dungeonInfo.isUnlocked ? "해금" : "잠김")}");
            }
        }
        
        // UI가 현재 표시 중이면 던전 버튼 재생성
        if (dungeonLayer != null && dungeonLayer.activeSelf)
        {
            if (enableDebugLogs)
                Debug.Log($"[DungeonSelect] UI 표시 중 → 던전 버튼 재생성");
            
            RefreshDungeonList(categoryId);
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"[DungeonSelect] UI 비표시 중 → 데이터만 갱신 (다음 ShowPanel()에서 반영됨)");
        }
    }
    
    /// <summary>
    /// 플레이어 현재 레벨 가져오기
    /// </summary>
    private int GetPlayerLevel()
    {
        if (PlayerDataManager.Instance != null)
        {
            int level = PlayerDataManager.Instance.CurrentLevel;
            
            if (enableDebugLogs)
                Debug.Log($"[DungeonSelect] 플레이어 레벨: {level}");
            
            return level;
        }
        
        // Fallback: PlayerDataManager가 없으면 기본값 1
        Debug.LogWarning("[DungeonSelect] PlayerDataManager를 찾을 수 없습니다. 기본 레벨 1 반환");
        return 1;
    }
    
    /// <summary>
    /// 뒤로 가기 버튼 클릭
    /// </summary>
    private void OnBackButtonClicked()
    {
        HidePanel();
        
        if (enableDebugLogs)
            Debug.Log("[DungeonSelect] 뒤로 가기 이벤트 발행");
        
        // 이벤트 발행 (LobbyUIController가 처리) ⭐
        OnDungeonBackButtonClicked?.Invoke();
    }
    
    /// <summary>
    /// 던전 Config 로드
    /// </summary>
    private StageConfig LoadDungeonConfig(string dungeonId)
    {
        string path = $"Stages/Configs/Dungeons/{dungeonId}_Config";
        var config = Resources.Load<StageConfig>(path);
        
        if (config == null && enableDebugLogs)
        {
            Debug.LogWarning($"[DungeonSelect] Config 로드 실패: {path}");
        }
        
        return config;
    }
    
    /// <summary>
    /// 카테고리 표시 이름 변환
    /// </summary>
    private string GetCategoryDisplayName(string categoryId)
    {
        switch (categoryId)
        {
            case "Daily_Boss_Dungeon":
                return "📅 데일리 보스 던전";
            case "Weekly_Raid":
                return "🗓️ 주간 레이드";
            case "Material_Farm":
                return "💎 재료 파밍";
            case "Gold_Farm":
                return "💰 골드 파밍";
            case "Exp_Farm":
                return "⭐ 경험치 파밍";
            default:
                return categoryId;
        }
    }
}

