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
    [SerializeField] private Sprite dungeonBg_DG01_SB01;     // DG01_SB01 배경
    
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
                categoryName = "데일리 보스 던전",
                categoryIcon = "📅",
                backgroundImage = categoryBg_DailyBoss,
                isUnlocked = true,
                isComingSoon = false,
                displayOrder = 1
            },
            new CategoryInfo
            {
                categoryId = "Weekly_Raid",
                categoryName = "주간 레이드",
                categoryIcon = "🗓️",
                backgroundImage = categoryBg_WeeklyRaid,
                isUnlocked = false,
                isComingSoon = true, // ⭐ Coming Soon!
                displayOrder = 2
            },
            new CategoryInfo
            {
                categoryId = "Material_Farm",
                categoryName = "재료 파밍",
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
            new DungeonInfo
            {
                dungeonId = "DG01_SB01",
                dungeonName = "속박저항 정수 던전",
                categoryId = "Daily_Boss_Dungeon",
                recommendedLevel = 10,
                isUnlocked = true,
                backgroundImage = dungeonBg_DG01_SB01, // ⭐ 배경 이미지
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
            
            // 권장 레벨 설정
            buttonUI.SetRecommendedLevel(dungeonInfo.recommendedLevel);
            
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
        if (dungeonNameText != null)
            dungeonNameText.text = config.StageName;
        
        if (dungeonDescriptionText != null)
            dungeonDescriptionText.text = config.Description;
        
        if (recommendedLevelText != null)
            recommendedLevelText.text = $"권장 레벨: Lv.{config.StageBaseLevel}";
        
        if (waveCountText != null)
            waveCountText.text = $"웨이브: {config.WaveCount}개";
        
        // ⭐ 보상 미리보기 (FirstClearDropTable + RepeatClearDropTable 기반)
        if (rewardPreviewText != null)
        {
            string rewardPreview = GenerateRewardPreview(config);
            rewardPreviewText.text = rewardPreview;
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

