using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Linq;

/// <summary>
/// ⭐ [Phase 2] 모든 플레이어 데이터를 통합 관리하는 매니저
/// 🆕 캐릭터별 분리 저장 시스템으로 개선
/// </summary>
public class PlayerDataManager : Singleton<PlayerDataManager>
{
    [Header("🎮 플레이어 기본 정보")]
    public int characterIndex = 0; // 현재 선택된 캐릭터 번호
    public string playerName = "Player"; // 플레이어 이름
    
    // 🆕 현재 활성 캐릭터 타입 추가
    [Header("🎯 활성 캐릭터")]
    [SerializeField] private PlayerType currentPlayerType = PlayerType.None;
    
    [Header("💰 재화 관리")]
    [SerializeField] private int currentGold = 0;
    
    [Header("📈 레벨 & 경험치")]
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int currentExp = 0;
    [SerializeField] private int expToNextLevel = 100;
    
    [Header("🎒 인벤토리 & 장비 시스템")]
    [SerializeField] private Dictionary<EquipmentSlot, EquipmentData> equippedItems = new Dictionary<EquipmentSlot, EquipmentData>();
    [SerializeField] private List<EquipmentData> inventoryItems = new List<EquipmentData>();
    [SerializeField] private int maxInventorySize = 50; // 최대 인벤토리 크기
    
    [Header("🔧 UI 관리")]
    private TMP_Text goldText;
    private const string COIN_AMOUNT_TEXT = "Gold Amount Text";
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 이벤트 시스템 - 기존
    public event Action<int> OnGoldChanged;
    public event Action<int> OnLevelChanged;
    public event Action<int, int> OnExpChanged; // (currentExp, expToNextLevel)
    
    // 이벤트 시스템 - 인벤토리 신규
    public event Action<EquipmentData> OnItemAddedToInventory;
    public event Action<EquipmentData> OnItemRemovedFromInventory;
    public event Action<EquipmentSlot, EquipmentData> OnItemEquipped;
    public event Action<EquipmentSlot, EquipmentData> OnItemUnequipped;
    public event Action OnInventoryChanged;
    
    // 접근자 프로퍼티 - 기존
    public int CurrentGold => currentGold;
    public int CurrentLevel => currentLevel;
    public int CurrentExp => currentExp;
    public int ExpToNextLevel => expToNextLevel;
    
    // 접근자 프로퍼티 - 인벤토리 신규
    public List<EquipmentData> InventoryItems => new List<EquipmentData>(inventoryItems);
    public Dictionary<EquipmentSlot, EquipmentData> EquippedItems => new Dictionary<EquipmentSlot, EquipmentData>(equippedItems);
    public int CurrentInventorySize => inventoryItems.Count;
    public int MaxInventorySize => maxInventorySize;
    public bool IsInventoryFull => CurrentInventorySize >= MaxInventorySize;
    
    protected override void Awake()
    {
        base.Awake();
        
        // 딕셔너리 초기화
        InitializeEquipmentSlots();
        
        // 씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void InitializeEquipmentSlots()
    {
        // 모든 장비 슬롯을 null로 초기화
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            equippedItems[slot] = null;
        }
    }

    protected override void OnDestroy()
    {
        // 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        base.OnDestroy();
    }

    private void Start()
    {
        // 게임 시작 시 저장된 데이터 불러오기
        LoadAllPlayerData();
        
        // UI 초기화
        StartCoroutine(InitializeUI());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬 로드시 UI 참조 초기화
        goldText = null;
        
        // UI 초기화 (다음 프레임에 실행)
        StartCoroutine(InitializeUI());
    }

    #region 💰 골드 관리 시스템
    
    /// <summary>
    /// 골드 추가
    /// </summary>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        
        currentGold += amount;
        SavePlayerData();
        OnGoldChanged?.Invoke(currentGold);
        
        if (showDebugLogs)
            Debug.Log($"💰 [PlayerData] 골드 추가: +{amount}, 현재: {currentGold}");
    }
    
    /// <summary>
    /// 골드 소모
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (amount <= 0) return false;
        
        if (currentGold >= amount)
        {
            currentGold -= amount;
            SavePlayerData();
            OnGoldChanged?.Invoke(currentGold);
            
            if (showDebugLogs)
                Debug.Log($"💰 [PlayerData] 골드 소모: -{amount}, 현재: {currentGold}");
            return true;
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"💰 [PlayerData] 골드 부족! 필요: {amount}, 보유: {currentGold}");
            return false;
        }
    }
    
    /// <summary>
    /// 골드 직접 설정 (호환성 유지)
    /// </summary>
    public void SetGold(int gold)
    {
        currentGold = Mathf.Max(0, gold);
        SavePlayerData();
        OnGoldChanged?.Invoke(currentGold);
        
        if (showDebugLogs)
            Debug.Log($"💰 [PlayerData] 골드 설정: {currentGold}");
    }
    
    #endregion
    
    #region 📈 레벨 & 경험치 관리 시스템
    
    /// <summary>
    /// 경험치 추가 및 레벨업 체크
    /// </summary>
    public void AddExp(int expAmount)
    {
        if (expAmount <= 0) return;
        
        currentExp += expAmount;
        OnExpChanged?.Invoke(currentExp, expToNextLevel);
        
        if (showDebugLogs)
            Debug.Log($"📈 [PlayerData] 경험치 {expAmount} 획득! 현재: {currentExp}/{expToNextLevel}");

        // 레벨업 체크 (여러 레벨업 가능)
        while (currentExp >= expToNextLevel)
        {
            LevelUp();
        }
        
        SavePlayerData();
    }
    
    /// <summary>
    /// 레벨업 처리
    /// </summary>
    private void LevelUp()
    {
        currentExp -= expToNextLevel;
        currentLevel++;
        
        // 다음 레벨 필요 경험치 계산 (1.2배씩 증가)
        expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.2f);
        
        // 이벤트 발생
        OnLevelChanged?.Invoke(currentLevel);
        OnExpChanged?.Invoke(currentExp, expToNextLevel);
        
        if (showDebugLogs)
            Debug.Log($"🆙 [PlayerData] 레벨 업! Lv.{currentLevel} (다음 레벨까지: {expToNextLevel - currentExp})");
        
        // 클래스별 레벨업 보너스 적용
        ApplyLevelUpBonus();
    }
    
    /// <summary>
    /// 레벨 직접 설정 (치트/테스트용)
    /// </summary>
    public void SetLevel(int level, int exp = 0)
    {
        currentLevel = Mathf.Max(1, level);
        currentExp = Mathf.Max(0, exp);
        
        // 레벨에 맞는 필요 경험치 계산
        expToNextLevel = CalculateExpForLevel(currentLevel + 1);
        
        OnLevelChanged?.Invoke(currentLevel);
        OnExpChanged?.Invoke(currentExp, expToNextLevel);
        SavePlayerData();
        
        if (showDebugLogs)
            Debug.Log($"📈 [PlayerData] 레벨 설정: Lv.{currentLevel}, EXP: {currentExp}/{expToNextLevel}");
    }
    
    /// <summary>
    /// 특정 레벨에 필요한 총 경험치 계산
    /// </summary>
    private int CalculateExpForLevel(int targetLevel)
    {
        int baseExp = 100;
        for (int i = 2; i <= targetLevel; i++)
        {
            baseExp = Mathf.RoundToInt(baseExp * 1.2f);
        }
        return baseExp;
    }
    
    /// <summary>
    /// 레벨업 시 클래스별 보너스 적용
    /// </summary>
    private void ApplyLevelUpBonus()
    {
        // 활성 클래스들에게 레벨업 알림
        var activeClasses = FindObjectsOfType<BaseClassBehaviour>();
        foreach (var classComp in activeClasses)
        {
            if (classComp.IsActiveClass)
            {
                classComp.OnLevelUp(currentLevel);
            }
        }
    }
    
    #endregion
    
    #region 🎒 인벤토리 관리 시스템
    
    /// <summary>
    /// 인벤토리에 아이템 추가
    /// </summary>
    public bool AddToInventory(EquipmentData item)
    {
        if (item == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("🎒 [PlayerData] null 아이템을 인벤토리에 추가할 수 없습니다.");
            return false;
        }
        
        if (IsInventoryFull)
        {
            if (showDebugLogs)
                Debug.LogWarning($"🎒 [PlayerData] 인벤토리가 가득참! ({CurrentInventorySize}/{MaxInventorySize})");
            return false;
        }
        
        inventoryItems.Add(item);
        OnItemAddedToInventory?.Invoke(item);
        OnInventoryChanged?.Invoke();
        SavePlayerData();
        
        if (showDebugLogs)
            Debug.Log($"🎒 [PlayerData] 인벤토리에 아이템 추가: {item.equipmentName} ({CurrentInventorySize}/{MaxInventorySize})");
        
        return true;
    }
    
    /// <summary>
    /// 인벤토리에서 아이템 제거
    /// </summary>
    public bool RemoveFromInventory(EquipmentData item)
    {
        if (item == null || !inventoryItems.Contains(item))
        {
            if (showDebugLogs)
                Debug.LogWarning($"🎒 [PlayerData] 인벤토리에 없는 아이템을 제거하려고 함: {item?.equipmentName}");
            return false;
        }
        
        inventoryItems.Remove(item);
        OnItemRemovedFromInventory?.Invoke(item);
        OnInventoryChanged?.Invoke();
        SavePlayerData();
        
        if (showDebugLogs)
            Debug.Log($"🎒 [PlayerData] 인벤토리에서 아이템 제거: {item.equipmentName} ({CurrentInventorySize}/{MaxInventorySize})");
        
        return true;
    }
    
    /// <summary>
    /// 아이템 장착
    /// </summary>
    public bool EquipItem(EquipmentData item)
    {
        if (item == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("🎒 [PlayerData] null 아이템을 장착할 수 없습니다.");
            return false;
        }
        
        // 장착 슬롯 결정
        EquipmentSlot targetSlot = GetEquipmentSlot(item);
        
        // 🔧 수정: 모든 장비 타입에 대해 장착 시도
        return EquipItemToSlot(item, targetSlot);
    }
    
    /// <summary>
    /// 특정 슬롯에 아이템 장착
    /// </summary>
    public bool EquipItemToSlot(EquipmentData item, EquipmentSlot slot)
    {
        if (item == null)
            return false;
            
        // 이미 장착된 아이템이 있으면 해제
        if (equippedItems[slot] != null)
        {
            UnequipItem(slot);
        }
        
        // 인벤토리에서 제거 (장착하면 인벤토리에서 사라짐)
        bool removedFromInventory = RemoveFromInventory(item);
        if (!removedFromInventory && !inventoryItems.Contains(item))
        {
            if (showDebugLogs)
                Debug.LogWarning($"🎒 [PlayerData] 인벤토리에 없는 아이템을 장착하려고 함: {item.equipmentName}");
        }
        
        // 장착 실행
        equippedItems[slot] = item;
        OnItemEquipped?.Invoke(slot, item);
        OnEquipmentChanged(); // 🆕 능력치 재계산
        ApplyPhysicalEquipment(slot, item); // 🆕 물리적 장비 적용
        SavePlayerData();
        
        if (showDebugLogs)
            Debug.Log($"⚔️ [PlayerData] 아이템 장착: {item.equipmentName} → {slot}");
        
        return true;
    }
    
    /// <summary>
    /// 아이템 해제
    /// </summary>
    public bool UnequipItem(EquipmentSlot slot)
    {
        if (!equippedItems.ContainsKey(slot) || equippedItems[slot] == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"🎒 [PlayerData] 슬롯 {slot}에 장착된 아이템이 없습니다.");
            return false;
        }
        
        EquipmentData unequippedItem = equippedItems[slot];
        
        // 인벤토리로 되돌리기
        if (!AddToInventory(unequippedItem))
        {
            if (showDebugLogs)
                Debug.LogWarning($"🎒 [PlayerData] 인벤토리가 가득차서 {unequippedItem.equipmentName}을 해제할 수 없습니다.");
            return false;
        }
        
        // 장착 해제
        equippedItems[slot] = null;
        OnItemUnequipped?.Invoke(slot, unequippedItem);
        OnEquipmentChanged(); // 🆕 능력치 재계산
        RemovePhysicalEquipment(slot, unequippedItem); // 🆕 물리적 장비 해제
        SavePlayerData();
        
        if (showDebugLogs)
            Debug.Log($"🎒 [PlayerData] 아이템 해제: {unequippedItem.equipmentName} ← {slot}");
        
        return true;
    }
    
    /// <summary>
    /// 아이템의 적절한 장착 슬롯 결정
    /// </summary>
    private EquipmentSlot GetEquipmentSlot(EquipmentData item)
    {
        switch (item.equipmentType)
        {
            case EquipmentType.Weapon:
                return EquipmentSlot.MainWeapon;
            case EquipmentType.Armor:
                // 🆕 방어구 타입별 세분화
                switch (item.ArmorType)
                {
                    case ArmorType.Helmet:
                        return EquipmentSlot.Helmet;
                    case ArmorType.Armor:
                        return EquipmentSlot.Armor;
                    case ArmorType.Boots:
                        return EquipmentSlot.Boots;
                    case ArmorType.Shield:
                        return EquipmentSlot.Shield;
                    default:
                        return EquipmentSlot.Armor; // 기본 갑옷 슬롯
                }
            case EquipmentType.Accessory:
                return EquipmentSlot.Ring1; // 기본 반지 슬롯
            default:
                return EquipmentSlot.MainWeapon; // 기본값
        }
    }
    
    /// <summary>
    /// 특정 슬롯에 장착된 아이템 가져오기
    /// </summary>
    public EquipmentData GetEquippedItem(EquipmentSlot slot)
    {
        return equippedItems.ContainsKey(slot) ? equippedItems[slot] : null;
    }
    
    /// <summary>
    /// 현재 장착된 무기 가져오기 (호환성 메서드)
    /// </summary>
    public EquipmentData GetEquippedWeapon()
    {
        return GetEquippedItem(EquipmentSlot.MainWeapon);
    }
    
    /// <summary>
    /// 인벤토리에서 특정 아이템 검색
    /// </summary>
    public EquipmentData FindItemInInventory(string itemName)
    {
        return inventoryItems.FirstOrDefault(item => item.equipmentName == itemName);
    }
    
    /// <summary>
    /// 인벤토리 상태 출력 (디버그용)
    /// </summary>
    [ContextMenu("인벤토리 상태 확인")]
    public void PrintInventoryStatus()
    {
        Debug.Log($"🎒 [PlayerData] 인벤토리 상태 ({CurrentInventorySize}/{MaxInventorySize}):");
        for (int i = 0; i < inventoryItems.Count; i++)
        {
            Debug.Log($"   {i+1}. {inventoryItems[i].equipmentName}");
        }
    }
    
    #endregion
    
    #region 📊 장비 능력치 적용 시스템
    
    /// <summary>
    /// 모든 장착된 장비의 능력치를 플레이어에게 적용
    /// </summary>
    public void ApplyAllEquipmentStats()
    {
        // PlayerController와 PlayerHealth 컴포넌트 찾기
        var playerController = FindObjectOfType<PlayerController>();
        var playerHealth = FindObjectOfType<PlayerHealth>();
        
        if (playerController == null || playerHealth == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("📊 [PlayerData] PlayerController 또는 PlayerHealth를 찾을 수 없어 능력치 적용을 건너뜁니다.");
            return;
        }
        
        // 기본 능력치 값들 (리셋용)
        float baseSpeed = 4f;  // 기본 이동속도
        float baseHealth = 200f; // 기본 체력
        
        // 장비 보너스 계산
        float totalSpeedBonus = 0f;
        float totalHealthBonus = 0f;
        float totalDefenseBonus = 0f;
        
        foreach (var kvp in equippedItems)
        {
            if (kvp.Value != null)
            {
                totalSpeedBonus += kvp.Value.speedBonus;
                totalHealthBonus += kvp.Value.healthBonus;
                totalDefenseBonus += kvp.Value.defenseBonus;
                
                if (showDebugLogs)
                    Debug.Log($"📊 [PlayerData] {kvp.Key}: 속도+{kvp.Value.speedBonus}, 체력+{kvp.Value.healthBonus}, 방어+{kvp.Value.defenseBonus}");
            }
        }
        
        // 능력치 적용
        playerController.SetMoveSpeed(baseSpeed + totalSpeedBonus);
        playerHealth.SetMaxHealth(Mathf.RoundToInt(baseHealth + totalHealthBonus));
        
        if (showDebugLogs)
        {
            Debug.Log($"📊 [PlayerData] 장비 능력치 적용 완료!");
            Debug.Log($"   - 이동속도: {baseSpeed} + {totalSpeedBonus} = {baseSpeed + totalSpeedBonus}");
            Debug.Log($"   - 체력: {baseHealth} + {totalHealthBonus} = {baseHealth + totalHealthBonus}");
            Debug.Log($"   - 방어력: +{totalDefenseBonus} (향후 구현)");
        }
    }
    
    /// <summary>
    /// 장비 변경 시 능력치 재계산
    /// </summary>
    private void OnEquipmentChanged()
    {
        // 0.1초 후 능력치 적용 (컴포넌트 초기화 대기)
        StartCoroutine(ApplyStatsWithDelay());
    }
    
    private System.Collections.IEnumerator ApplyStatsWithDelay()
    {
        yield return new WaitForSeconds(0.1f);
        ApplyAllEquipmentStats();
    }
    
    #endregion
    
    #region 🎮 물리적 장비 처리 시스템
    
    /// <summary>
    /// 장비 착용 시 물리적 표현 적용
    /// </summary>
    private void ApplyPhysicalEquipment(EquipmentSlot slot, EquipmentData item)
    {
        var playerEquipment = FindObjectOfType<PlayerEquipment>();
        if (playerEquipment == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("🎮 [PlayerData] PlayerEquipment 컴포넌트를 찾을 수 없습니다.");
            return;
        }
        
        switch (item.equipmentType)
        {
            case EquipmentType.Weapon:
                // 🔧 수정: ActiveWeapon 시스템 연동 (기존 2줄 → 신규 12줄)
                var activeWeapon = FindObjectOfType<ActiveWeapon>();
                if (activeWeapon != null)
                {
                    activeWeapon.EquipWeapon(item);
                    if (showDebugLogs)
                        Debug.Log($"⚔️ [PlayerData] 무기 물리적 장착 성공: {item.equipmentName}");
                }
                else
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"⚠️ [PlayerData] ActiveWeapon을 찾을 수 없습니다: {item.equipmentName}");
                }
                break;
                
            case EquipmentType.Armor:
                // 갑옷/신발은 PlayerEquipment에서 처리
                bool success = playerEquipment.EquipArmorPrefab(item);
                if (showDebugLogs)
                    Debug.Log($"🛡️ [PlayerData] 방어구 물리적 장착 {(success ? "성공" : "실패")}: {item.equipmentName}");
                break;
                
            case EquipmentType.Accessory:
                // 악세서리는 향후 구현
                if (showDebugLogs)
                    Debug.Log($"💍 [PlayerData] 악세서리 물리적 장착 (향후 구현): {item.equipmentName}");
                break;
        }
    }
    
    /// <summary>
    /// 장비 해제 시 물리적 표현 제거
    /// </summary>
    private void RemovePhysicalEquipment(EquipmentSlot slot, EquipmentData item)
    {
        var playerEquipment = FindObjectOfType<PlayerEquipment>();
        if (playerEquipment == null) return;
        
        switch (item.equipmentType)
        {
            case EquipmentType.Armor:
                bool success = playerEquipment.UnequipArmorPrefab(slot);
                if (showDebugLogs)
                    Debug.Log($"🛡️ [PlayerData] 방어구 물리적 해제 {(success ? "성공" : "실패")}: {item.equipmentName}");
                break;
        }
    }
    
    #endregion
    
    #region 💾 저장/로드 시스템 (확장)
    
    /// <summary>
    /// 🆕 현재 플레이어 타입 설정 (캐릭터 변경 시 호출)
    /// </summary>
    public void SetCurrentPlayerType(PlayerType playerType)
    {
        if (currentPlayerType != playerType)
        {
            // 🔑 기존 캐릭터 데이터 저장
            if (currentPlayerType != PlayerType.None)
            {
                SavePlayerData();
                if (showDebugLogs)
                    Debug.Log($"💾 [PlayerData] {currentPlayerType} 데이터 저장 완료");
            }
            
            // 🔑 새 캐릭터 타입 설정
            currentPlayerType = playerType;
            
            // 🔑 새 캐릭터 데이터 로드
            if (currentPlayerType != PlayerType.None)
            {
                LoadAllPlayerData();
                if (showDebugLogs)
                    Debug.Log($"📁 [PlayerData] {currentPlayerType} 데이터 로드 완료");
            }
        }
    }
    
    /// <summary>
    /// 🆕 현재 플레이어 타입 가져오기
    /// </summary>
    public PlayerType GetCurrentPlayerType()
    {
        // 1순위: 설정된 currentPlayerType
        if (currentPlayerType != PlayerType.None)
            return currentPlayerType;
            
        // 2순위: GameManager에서 가져오기
        if (GameManager.Instance?.selectedPlayerData != null)
        {
            currentPlayerType = GameManager.Instance.selectedPlayerData.selectedPlayerType;
            return currentPlayerType;
        }
        
        // 3순위: 기본값
        return PlayerType.Warrior;
    }
    
    /// <summary>
    /// 🆕 캐릭터별 저장 키 생성
    /// </summary>
    private string GetPlayerDataKey()
    {
        PlayerType playerType = GetCurrentPlayerType();
        return $"PlayerData_{playerType}_{characterIndex}";
    }
    
    /// <summary>
    /// 모든 플레이어 데이터 저장 (캐릭터별 분리)
    /// </summary>
    public void SavePlayerData()
    {
        // ⭐ SaveManager 체크 제거 (더 이상 필요 없음)
        // if (SaveManager.Instance == null) return;
        
        var saveData = new PlayerSaveData
        {
            characterIndex = this.characterIndex,
            playerName = this.playerName,
            playerType = GetCurrentPlayerType(),
            lastPlayTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            
            gold = this.currentGold,
            level = this.currentLevel,
            exp = this.currentExp,
            expToNextLevel = this.expToNextLevel,
            
            // 인벤토리 데이터 저장
            inventoryItemNames = inventoryItems.Select(item => item.name).ToList(),
            equippedItemNames = equippedItems.Where(kvp => kvp.Value != null)
                                           .ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value.name)
        };
        
        string json = saveData.ToJson();
        string key = GetPlayerDataKey();
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
        
        if (showDebugLogs)
            Debug.Log($"💾 [PlayerData] {GetCurrentPlayerType()} 데이터 저장 완료: {saveData}");
    }
    
    /// <summary>
    /// 모든 플레이어 데이터 로드 (캐릭터별 분리)
    /// </summary>
    public void LoadAllPlayerData()
    {
        // ⭐ SaveManager 체크 제거 (더 이상 필요 없음)
        // if (SaveManager.Instance == null) return;
        
        string key = GetPlayerDataKey();
        string json = PlayerPrefs.GetString(key, "");
        
        if (string.IsNullOrEmpty(json))
        {
            // 🆕 캐릭터별 기본값 설정
            InitializeDefaultDataForPlayerType(GetCurrentPlayerType());
            
            if (showDebugLogs)
                Debug.Log($"📁 [PlayerData] {GetCurrentPlayerType()} 저장 데이터 없음. 기본값 사용.");
            return;
        }
        
        var saveData = PlayerSaveData.FromJson(json);
        if (saveData != null)
        {
            this.currentGold = saveData.gold;
            this.currentLevel = saveData.level;
            this.currentExp = saveData.exp;
            this.expToNextLevel = saveData.expToNextLevel;
            this.playerName = saveData.playerName;
            
            // 인벤토리 데이터 로드
            LoadInventoryFromSaveData(saveData);
            
            // 이벤트 발생 (UI 업데이트)
            OnGoldChanged?.Invoke(currentGold);
            OnLevelChanged?.Invoke(currentLevel);
            OnExpChanged?.Invoke(currentExp, expToNextLevel);
            OnInventoryChanged?.Invoke();
            
            if (showDebugLogs)
                Debug.Log($"📁 [PlayerData] {GetCurrentPlayerType()} 데이터 로드 완료: {saveData}");
        }
    }
    
    /// <summary>
    /// 🆕 캐릭터별 기본값 초기화
    /// </summary>
    private void InitializeDefaultDataForPlayerType(PlayerType playerType)
    {
        // 기본 스탯 초기화
        currentGold = 0;
        currentLevel = 1;
        currentExp = 0;
        expToNextLevel = 100;
        
        // 인벤토리 초기화
        inventoryItems.Clear();
        InitializeEquipmentSlots();
        
        // 🆕 캐릭터별 시작 아이템 추가
        switch (playerType)
        {
            case PlayerType.Warrior:
                AddStartingEquipment("Sword_A_Equipment");
                break;
            case PlayerType.Assasin:
                AddStartingEquipment("Bow_A_Equipment");
                break;
            case PlayerType.Wizard:
                AddStartingEquipment("Staff_A_Equipment");
                break;
        }
        
        // 이벤트 발생
        OnGoldChanged?.Invoke(currentGold);
        OnLevelChanged?.Invoke(currentLevel);
        OnExpChanged?.Invoke(currentExp, expToNextLevel);
        OnInventoryChanged?.Invoke();
        
        if (showDebugLogs)
            Debug.Log($"🆕 [PlayerData] {playerType} 기본 데이터 초기화 완료");
    }
    
    /// <summary>
    /// 🆕 시작 장비 추가 (Generated 경로로 수정)
    /// </summary>
    private void AddStartingEquipment(string equipmentName)
    {
        // 🔧 Generated 폴더에서 로드하도록 수정
        EquipmentData startingEquipment = Resources.Load<EquipmentData>($"Generated/Weapons/{equipmentName}");
        if (startingEquipment != null)
        {
            AddToInventory(startingEquipment);
            if (showDebugLogs)
                Debug.Log($"🎒 [PlayerData] 시작 장비 추가: {equipmentName}");
        }
        else
        {
            Debug.LogWarning($"⚠️ [PlayerData] Generated 폴더에서 시작 장비를 찾을 수 없음: {equipmentName}");
            
            // 🔧 백업: 기존 경로에서 시도 (임시 호환성)
            startingEquipment = Resources.Load<EquipmentData>(equipmentName);
            if (startingEquipment != null)
            {
                AddToInventory(startingEquipment);
                Debug.LogWarning($"⚠️ [PlayerData] 기존 경로에서 발견: {equipmentName} (Generated 폴더로 이동 권장)");
            }
        }
    }
    
    /// <summary>
    /// 저장 데이터에서 인벤토리 로드 (Generated 경로로 수정)
    /// </summary>
    private void LoadInventoryFromSaveData(PlayerSaveData saveData)
    {
        // 인벤토리 초기화
        inventoryItems.Clear();
        InitializeEquipmentSlots();
        
        // 인벤토리 아이템 로드 - Generated 폴더 우선
        foreach (string itemName in saveData.inventoryItemNames)
        {
            EquipmentData item = null;
            
            // 🔧 1순위: Generated/Weapons 폴더에서 찾기
            item = Resources.Load<EquipmentData>($"Generated/Weapons/{itemName}");
            
            // 🔧 2순위: Generated 전체에서 찾기
            if (item == null)
            {
                string[] generatedPaths = { "Generated/Weapons", "Generated/Projectiles" };
                foreach (string path in generatedPaths)
                {
                    item = Resources.Load<EquipmentData>($"{path}/{itemName}");
                    if (item != null) break;
                }
            }
            
            // 🔧 3순위: 기존 경로에서 찾기 (호환성)
            if (item == null)
            {
                item = Resources.Load<EquipmentData>(itemName);
                if (item != null)
                {
                    Debug.LogWarning($"⚠️ [PlayerData] 기존 경로에서 발견: {itemName} (Generated 폴더 이전 권장)");
                }
            }
            
            if (item != null)
            {
                inventoryItems.Add(item);
            }
            else
            {
                Debug.LogWarning($"🎒 [PlayerData] 인벤토리 아이템을 찾을 수 없음: {itemName}");
            }
        }
        
        // 장착 아이템 로드 - Generated 폴더 우선
        if (saveData.equippedItemNames != null)
        {
            foreach (var kvp in saveData.equippedItemNames)
            {
                if (System.Enum.TryParse<EquipmentSlot>(kvp.Key, out EquipmentSlot slot))
                {
                    EquipmentData item = null;
                    
                    // 🔧 1순위: Generated/Weapons 폴더
                    item = Resources.Load<EquipmentData>($"Generated/Weapons/{kvp.Value}");
                    
                    // 🔧 2순위: 기존 경로들
                    if (item == null)
                    {
                        string[] fallbackPaths = { 
                            $"EquipmentData/{kvp.Value}", 
                            kvp.Value,
                            $"Equipment/{kvp.Value}"
                        };
                        
                        foreach (string path in fallbackPaths)
                        {
                            item = Resources.Load<EquipmentData>(path);
                            if (item != null)
                            {
                                Debug.LogWarning($"⚠️ [PlayerData] 기존 경로에서 장착 아이템 발견: {path}");
                                break;
                            }
                        }
                    }
                    
                    if (item != null)
                    {
                        equippedItems[slot] = item;
                    }
                    else
                    {
                        Debug.LogWarning($"⚔️ [PlayerData] 장착 아이템을 찾을 수 없음: {kvp.Value}");
                    }
                }
            }
        }
        
        if (showDebugLogs)
            Debug.Log($"🎒 [PlayerData] 인벤토리 로드 완료 - 보관: {inventoryItems.Count}개, 장착: {equippedItems.Count(kvp => kvp.Value != null)}개");
    }
    
    #endregion
    
    #region 🎯 클래스별 세부 데이터 관리 (SaveManager 통합)
    
    /// <summary>
    /// 클래스별 세부 데이터 저장 (SaveManager 기능 통합)
    /// </summary>
    public void SaveClassData(PlayerType classType, BaseClassSaveData data)
    {
        string key = $"ClassData_{classType}_{characterIndex}";
        string json = data.ToJson();
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
        
        if (showDebugLogs)
            Debug.Log($"💾 [PlayerData] {classType} 클래스 데이터 저장 완료: {data}");
    }
    
    /// <summary>
    /// 클래스별 세부 데이터 로드 (SaveManager 기능 통합)
    /// </summary>
    public BaseClassSaveData LoadClassData(PlayerType classType)
    {
        string key = $"ClassData_{classType}_{characterIndex}";
        string json = PlayerPrefs.GetString(key, "");
        
        if (string.IsNullOrEmpty(json))
        {
            if (showDebugLogs)
                Debug.Log($"📁 [PlayerData] {classType} 클래스 데이터 없음, 기본값 생성");
            
            var defaultData = new BaseClassSaveData();
            defaultData.Reset(classType);
            return defaultData;
        }
        
        if (showDebugLogs)
            Debug.Log($"📁 [PlayerData] {classType} 클래스 데이터 로드 완료");
        
        return BaseClassSaveData.FromJson(json);
    }
    
    /// <summary>
    /// 현재 활성 클래스 타입 저장 (SaveManager 기능 통합)
    /// </summary>
    public void SaveActiveClass(PlayerType activeClassType)
    {
        PlayerPrefs.SetInt($"ActiveClass_{characterIndex}", (int)activeClassType);
        PlayerPrefs.Save();
        
        if (showDebugLogs)
            Debug.Log($"💾 [PlayerData] 활성 클래스 저장: {activeClassType}");
    }
    
    /// <summary>
    /// 현재 활성 클래스 타입 불러오기 (SaveManager 기능 통합)
    /// </summary>
    public PlayerType LoadActiveClass()
    {
        int classTypeInt = PlayerPrefs.GetInt($"ActiveClass_{characterIndex}", 0);
        PlayerType classType = (PlayerType)classTypeInt;
        
        if (showDebugLogs)
            Debug.Log($"📁 [PlayerData] 활성 클래스 로드: {classType}");
        
        return classType;
    }
    
    #endregion
    
    #region 🎨 UI 관리 시스템
    
    /// <summary>
    /// UI 초기화
    /// </summary>
    private IEnumerator InitializeUI()
    {
        // UI가 완전히 로드될 때까지 대기
        yield return new WaitForEndOfFrame();
        
        // UI 찾기 및 업데이트
        FindUIElements();
        UpdateAllUI();
    }
    
    /// <summary>
    /// UI 요소 찾기
    /// </summary>
    private void FindUIElements()
    {
        // 골드 텍스트 찾기
        if (goldText == null)
        {
            var goldTextObject = GameObject.Find(COIN_AMOUNT_TEXT);
            if (goldTextObject != null)
            {
                goldText = goldTextObject.GetComponent<TMP_Text>();
            }
        }
        
        // 레벨 텍스트는 LevelUI에서 자동 관리되므로 여기서는 찾지 않음
    }
    
    /// <summary>
    /// 골드 UI 업데이트
    /// </summary>
    private void UpdateGoldUI()
    {
        FindUIElements();
        
        if (goldText != null)
        {
            goldText.text = currentGold.ToString("D3");
        }
    }
    
    /// <summary>
    /// 모든 UI 업데이트
    /// </summary>
    private void UpdateAllUI()
    {
        UpdateGoldUI();
        // 레벨 UI는 이벤트로 자동 업데이트됨
    }
    
    #endregion
    
    #region 🔧 호환성 메서드 (기존 시스템 연동)
    
    /// <summary>
    /// EconomyManager.UpdateCurrentGold() 호환성 메서드
    /// </summary>
    public void UpdateCurrentGold()
    {
        AddGold(1);
    }
    
    /// <summary>
    /// PlayerManager.GetCurrentGold() 호환성 메서드
    /// </summary>
    public int GetCurrentGold()
    {
        return currentGold;
    }
    
    #endregion
    
    #region 📊 디버그 메서드
    
    /// <summary>
    /// 현재 플레이어 상태 출력
    /// </summary>
    public void PrintPlayerStatus()
    {
        Debug.Log($"🎮 [PlayerData] 플레이어 상태:");
        Debug.Log($"   - 캐릭터: {playerName} (#{characterIndex})");
        Debug.Log($"   - 레벨: {currentLevel} ({currentExp}/{expToNextLevel})");
        Debug.Log($"   - 골드: {currentGold}");
    }
    
    /// <summary>
    /// 치트: 골드/경험치 추가 (테스트용)
    /// </summary>
    [ContextMenu("치트: 골드 +100")]
    public void CheatAddGold() => AddGold(100);
    
    [ContextMenu("치트: 경험치 +50")]
    public void CheatAddExp() => AddExp(50);
    
    /// <summary>
    /// 치트: 테스트 아이템 추가 (Generated 경로로 수정)
    /// </summary>
    [ContextMenu("치트: 테스트 아이템 추가")]
    public void CheatAddTestItem()
    {
        // 🔧 Generated 폴더에서 Sword_A_Equipment 찾기
        EquipmentData testItem = Resources.Load<EquipmentData>("Generated/Weapons/Sword_A_Equipment");
        if (testItem != null)
        {
            AddToInventory(testItem);
            Debug.Log($"🎒 [DEBUG] Generated 폴더에서 테스트 아이템 추가: {testItem.equipmentName}");
        }
        else
        {
            // 🔧 백업: 기존 경로에서 시도
            testItem = Resources.Load<EquipmentData>("Sword_A_Equipment");
            if (testItem != null)
            {
                AddToInventory(testItem);
                Debug.LogWarning($"🎒 [DEBUG] 기존 경로에서 테스트 아이템 추가: {testItem.equipmentName}");
            }
            else
            {
                Debug.LogWarning("🎒 [DEBUG] 테스트 아이템을 찾을 수 없습니다! Generated 폴더를 확인하세요.");
            }
        }
    }
    
    /// <summary>
    /// 🗑️ 인벤토리 완전 초기화
    /// </summary>
    [ContextMenu("인벤토리 초기화")]
    public void ClearInventory()
    {
        inventoryItems.Clear();
        OnInventoryChanged?.Invoke();
        SavePlayerData();
        
        if (showDebugLogs)
            Debug.Log("🗑️ [PlayerData] 인벤토리가 완전히 초기화되었습니다.");
    }

    /// <summary>
    /// ⚔️ 장착 아이템 모두 해제
    /// </summary>
    [ContextMenu("장착 아이템 모두 해제")]
    public void UnequipAllItems()
    {
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (equippedItems[slot] != null)
            {
                UnequipItem(slot);
            }
        }
        
        if (showDebugLogs)
            Debug.Log("⚔️ [PlayerData] 모든 장착 아이템이 해제되었습니다.");
    }

    /// <summary>
    /// 🔄 인벤토리 + 장비 완전 리셋
    /// </summary>
    [ContextMenu("인벤토리 & 장비 완전 리셋")]
    public void ResetAllItemData()
    {
        ClearInventory();
        InitializeEquipmentSlots();
        OnInventoryChanged?.Invoke();
        SavePlayerData();
        
        if (showDebugLogs)
            Debug.Log("🔄 [PlayerData] 인벤토리와 장비가 완전히 리셋되었습니다.");
    }
    
    #endregion
}

/// <summary>
/// ⭐ [Phase 2] 플레이어 저장 데이터 구조 (캐릭터별 분리)
/// </summary>
[System.Serializable]
public class PlayerSaveData
{
    [Header("기본 정보")]
    public int characterIndex;
    public string playerName = "Player";
    public PlayerType playerType = PlayerType.None; // 🆕 플레이어 타입 추가
    public string lastPlayTime; // DateTime을 string으로 저장
    
    [Header("진행 데이터")]
    public int gold;
    public int level;
    public int exp;
    public int expToNextLevel;
    
    [Header("인벤토리 & 장비")]
    public List<string> inventoryItemNames = new List<string>(); // 인벤토리 아이템들의 이름
    public Dictionary<string, string> equippedItemNames = new Dictionary<string, string>(); // 슬롯별 장착 아이템 이름
    
    /// <summary>
    /// JSON 문자열로 변환
    /// </summary>
    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }
    
    /// <summary>
    /// JSON 문자열에서 복원
    /// </summary>
    public static PlayerSaveData FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;
            
        return JsonUtility.FromJson<PlayerSaveData>(json);
    }
    
    /// <summary>
    /// 디버깅용 문자열 표현
    /// </summary>
    public override string ToString()
    {
        return $"PlayerData[{playerType}:{playerName}] Lv.{level} Gold:{gold} EXP:{exp}/{expToNextLevel} 인벤토리:{inventoryItemNames?.Count ?? 0}개";
    }
} 