using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Linq;
using System.IO;

/// <summary>
/// ⭐ [Phase 3] 슬롯 기반 플레이어 데이터 관리자 (완전 새 구조)
/// - PlayerSlotData: JSON 파일 기반 저장/로드
/// - SelectedPlayerData: 런타임 캐시 관리
/// - 단일 책임: 슬롯 관리 + 파일 저장/로드만 담당
/// </summary>
public class PlayerDataManager : Singleton<PlayerDataManager>
{
    [Header("🎮 슬롯 관리 설정")]
    [SerializeField] private int maxSlots = 3; // 최대 캐릭터 슬롯 수
    [SerializeField] private string saveDirectory = "PlayerSlots"; // 저장 폴더명
    
    [Header("🎯 런타임 데이터 캐시")]
    public SelectedPlayerData selectedPlayerData; // ScriptableObject 참조 (public으로 변경)
    
    [Header("📊 슬롯 상태")]
    [SerializeField] private List<PlayerSlotData> playerSlots = new List<PlayerSlotData>(); // 현재 로드된 슬롯들
    [SerializeField] private int currentSlotIndex = -1; // 현재 활성 슬롯 (-1: 미선택)
    
    [Header("🔧 UI 관리")]
    private TMP_Text goldText;
    private const string COIN_AMOUNT_TEXT = "Gold Amount Text";
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = true;
    
    // 이벤트 시스템 (기존 호환성 유지)
    public event Action<int> OnGoldChanged;
    public event Action<int> OnLevelChanged;
    public event Action<int, int> OnExpChanged; // (currentExp, expToNextLevel)
    public event Action<EquipmentData> OnItemAddedToInventory;
    public event Action<EquipmentData> OnItemRemovedFromInventory;
    public event Action<EquipmentSlot, EquipmentData> OnItemEquipped;
    public event Action<EquipmentSlot, EquipmentData> OnItemUnequipped;
    public event Action OnInventoryChanged;
    
    // 슬롯 관리 이벤트
    public event Action<int> OnSlotSelected; // 슬롯 선택 시
    public event Action<PlayerSlotData> OnSlotDataChanged; // 슬롯 데이터 변경 시
    
    // 접근자 프로퍼티 (SelectedPlayerData 위임)
    public int CurrentGold => selectedPlayerData != null ? selectedPlayerData.CurrentGold : 0;
    public int CurrentLevel => selectedPlayerData != null ? selectedPlayerData.CurrentLevel : 1;
    public int CurrentExp => selectedPlayerData != null ? selectedPlayerData.CurrentExp : 0;
    public int ExpToNextLevel => selectedPlayerData != null ? selectedPlayerData.ExpToNextLevel : 100;
    public List<EquipmentData> InventoryItems => selectedPlayerData != null ? selectedPlayerData.InventoryItems : new List<EquipmentData>();
    public Dictionary<EquipmentSlot, EquipmentData> EquippedItems => selectedPlayerData != null ? selectedPlayerData.EquippedItems : new Dictionary<EquipmentSlot, EquipmentData>();
    public int CurrentInventorySize => selectedPlayerData != null ? selectedPlayerData.CurrentInventorySize : 0;
    public int MaxInventorySize => selectedPlayerData != null ? selectedPlayerData.MaxInventorySize : 16;
    public bool IsInventoryFull => selectedPlayerData != null ? selectedPlayerData.IsInventoryFull : false;
    
    // 슬롯 관리 프로퍼티
    public int MaxSlots => maxSlots;
    public int CurrentSlotIndex => currentSlotIndex;
    public bool IsSlotSelected => currentSlotIndex >= 0 && selectedPlayerData != null;
    public PlayerType CurrentPlayerType => selectedPlayerData != null ? selectedPlayerData.selectedPlayerType : PlayerType.None;
    public string SaveDirectoryPath => Path.Combine(Application.persistentDataPath, saveDirectory);
    
    protected override void Awake()
    {
        base.Awake();
        
        // 저장 폴더 생성
        CreateSaveDirectory();
        
        // 씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // SelectedPlayerData ScriptableObject 찾기 또는 생성
        InitializeSelectedPlayerData();
    }
    
    protected override void OnDestroy()
    {
        // 현재 데이터 저장
        if (IsSlotSelected)
            SaveCurrentSlot();
        
        // 이벤트 구독 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        base.OnDestroy();
    }
    
    private void Start()
    {
        // 모든 슬롯 로드
        LoadAllSlots();
        
        // 🆕 SelectedPlayerData maxInventorySize 강제 동기화
        if (selectedPlayerData != null)
        {
            selectedPlayerData.maxInventorySize = 16; // 강제로 16으로 설정
            Debug.Log($"🔧 [PlayerDataManager] maxInventorySize 강제 동기화: {selectedPlayerData.maxInventorySize}");
        }
        
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
    
    #region 🗂️ 슬롯 관리 시스템
    
    /// <summary>
    /// 모든 슬롯 데이터 로드
    /// </summary>
    public void LoadAllSlots()
    {
        playerSlots.Clear();
        
        for (int i = 0; i < maxSlots; i++)
        {
            var slotData = LoadSlotData(i);
            if (slotData == null)
            {
                // 빈 슬롯 생성
                slotData = new PlayerSlotData
                {
                    slotIndex = i,
                    isSlotUsed = false
                };
            }
            playerSlots.Add(slotData);
        }
        
        if (showDebugLogs)
            Debug.Log($"📁 [PlayerDataManager] {maxSlots}개 슬롯 로드 완료. 사용중: {GetUsedSlotCount()}개");
    }
    
    /// <summary>
    /// 특정 슬롯 데이터 로드
    /// </summary>
    public PlayerSlotData LoadSlotData(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return null;
        
        string filePath = GetSlotFilePath(slotIndex);
        
        if (!File.Exists(filePath))
        {
            if (showDebugLogs)
                Debug.Log($"📁 [PlayerDataManager] 슬롯 {slotIndex} 파일 없음: {filePath}");
            return null;
        }
        
        try
        {
            string json = File.ReadAllText(filePath);
            var slotData = PlayerSlotData.FromJson(json);
            
            if (slotData != null)
            {
                if (showDebugLogs)
                    Debug.Log($"📁 [PlayerDataManager] 슬롯 {slotIndex} 로드 성공: {slotData}");
                return slotData;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"💥 [PlayerDataManager] 슬롯 {slotIndex} 로드 실패: {ex.Message}");
        }
        
        return null;
    }
    
    /// <summary>
    /// 특정 슬롯 데이터 저장
    /// </summary>
    public bool SaveSlotData(PlayerSlotData slotData)
    {
        if (slotData == null || slotData.slotIndex < 0 || slotData.slotIndex >= maxSlots) 
            return false;
        
        try
        {
            string filePath = GetSlotFilePath(slotData.slotIndex);
            string json = slotData.ToJson();
            
            File.WriteAllText(filePath, json);
            
            // 메모리 내 슬롯 데이터도 업데이트
            if (slotData.slotIndex < playerSlots.Count)
                playerSlots[slotData.slotIndex] = slotData;
            
            OnSlotDataChanged?.Invoke(slotData);
            
            if (showDebugLogs)
                Debug.Log($"💾 [PlayerDataManager] 슬롯 {slotData.slotIndex} 저장 완료: {slotData}");
                
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"💥 [PlayerDataManager] 슬롯 {slotData.slotIndex} 저장 실패: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 새 캐릭터 슬롯 생성
    /// </summary>
    public bool CreateNewSlot(int slotIndex, PlayerType playerType, string playerName = null)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return false;
        
        var existingSlot = GetSlotData(slotIndex);
        if (existingSlot != null && existingSlot.isSlotUsed)
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [PlayerDataManager] 슬롯 {slotIndex}는 이미 사용중");
            return false;
        }
        
        // 새 슬롯 데이터 생성
        var newSlot = PlayerSlotData.CreateDefaultSlot(slotIndex, playerType);
        if (!string.IsNullOrEmpty(playerName))
            newSlot.playerName = playerName;
        
        // 저장
        if (SaveSlotData(newSlot))
        {
            // 🆕 추가: 새 슬롯 생성 후 메모리에서도 갱신
            LoadAllSlots();
            
            if (showDebugLogs)
                Debug.Log($"✨ [PlayerDataManager] 새 슬롯 {slotIndex} 생성 완료: {newSlot}");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 슬롯 삭제
    /// </summary>
    public bool DeleteSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return false;
        
        // 🆕 추가: 삭제할 슬롯이 현재 선택된 슬롯인지 확인
        bool isDeletingCurrentSlot = (currentSlotIndex == slotIndex);
        
        // 현재 선택된 슬롯이라면 선택 해제
        if (currentSlotIndex == slotIndex)
        {
            currentSlotIndex = -1;
            selectedPlayerData.Reset();
            
            // 🆕 추가: SelectedPlayerData ScriptableObject 완전 초기화
            if (selectedPlayerData != null)
            {
                selectedPlayerData.selectedSlotIndex = -1;
                selectedPlayerData.playerName = "Player";
                selectedPlayerData.selectedPlayerType = PlayerType.None;
                selectedPlayerData.weaponName = "";
                selectedPlayerData.currentLevel = 1;
                selectedPlayerData.currentGold = 0;
                selectedPlayerData.currentExp = 0;
                selectedPlayerData.expToNextLevel = 100;
                selectedPlayerData.classLevel = 1;
                selectedPlayerData.maxInventorySize = 16;
                
                // 🆕 추가: Unity 에디터에서 ScriptableObject 상태 강제 갱신
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(selectedPlayerData);
                #endif
            }
        }
        
        try
        {
            string filePath = GetSlotFilePath(slotIndex);
            if (File.Exists(filePath))
                File.Delete(filePath);
            
            // 메모리 내 슬롯도 초기화
            if (slotIndex < playerSlots.Count)
            {
                playerSlots[slotIndex] = new PlayerSlotData
                {
                    slotIndex = slotIndex,
                    isSlotUsed = false
                };
            }
            
            // 🆕 추가: 삭제 완료 로그 개선
            if (showDebugLogs)
            {
                Debug.Log($"🗑️ [PlayerDataManager] 슬롯 {slotIndex} 삭제 완료");
                if (isDeletingCurrentSlot)
                {
                    Debug.Log($"🔄 [PlayerDataManager] 현재 선택 슬롯 삭제됨 - SelectedPlayerData 완전 초기화");
                }
            }
                
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"💥 [PlayerDataManager] 슬롯 {slotIndex} 삭제 실패: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 현재 선택된 슬롯 저장
    /// </summary>
    public bool SaveCurrentSlot()
    {
        if (!IsSlotSelected) return false;
        
        var slotData = selectedPlayerData.SaveToSlotData();
        return SaveSlotData(slotData);
    }
    
    /// <summary>
    /// 특정 슬롯 데이터 가져오기
    /// </summary>
    public PlayerSlotData GetSlotData(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= playerSlots.Count) return null;
        return playerSlots[slotIndex];
    }
    
    /// <summary>
    /// 사용중인 슬롯 개수
    /// </summary>
    public int GetUsedSlotCount()
    {
        return playerSlots.Count(slot => slot.isSlotUsed);
    }
    
    /// <summary>
    /// 빈 슬롯 인덱스 찾기
    /// </summary>
    public int GetEmptySlotIndex()
    {
        for (int i = 0; i < playerSlots.Count; i++)
        {
            if (!playerSlots[i].isSlotUsed)
                return i;
        }
        return -1; // 빈 슬롯 없음
    }
    
    /// <summary>
    /// 🔄 슬롯 전환 (완전한 데이터 교체 모드)
    /// SelectedPlayerData를 선택된 슬롯 데이터로 완전히 갱신
    /// </summary>
    public bool SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return false;
        
        var slotData = GetSlotData(slotIndex);
        if (slotData == null || !slotData.isSlotUsed)
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [PlayerDataManager] 슬롯 {slotIndex}는 사용되지 않음");
            return false;
        }
        
        // 🎯 핵심: 기존의 완벽한 LoadFromSlotData 활용
        currentSlotIndex = slotIndex;
        
        if (selectedPlayerData != null)
        {
            selectedPlayerData.LoadFromSlotData(slotData);
            Debug.Log($"🔄 [PlayerDataManager] 슬롯 {slotIndex} 완전 전환: {slotData.playerName}({slotData.playerType}) - 골드:{slotData.gold}, 레벨:{slotData.level}, 인벤토리:{slotData.inventoryItemNames.Count}개");
        }
        
        // 이벤트 발생
        OnSlotSelected?.Invoke(slotIndex);
        TriggerAllUIEvents();
        
        return true;
    }

    #endregion
    
    #region 💰 기존 호환성 메서드들 (SelectedPlayerData 위임)
    
    /// <summary>
    /// 골드 추가
    /// </summary>
    public void AddGold(int amount)
    {
        if (!IsSlotSelected || amount <= 0) return;
        
        selectedPlayerData.currentGold += amount;
        SaveCurrentSlot();
        OnGoldChanged?.Invoke(selectedPlayerData.currentGold);
        
        if (showDebugLogs)
            Debug.Log($"💰 [PlayerDataManager] 골드 추가: +{amount}, 현재: {selectedPlayerData.currentGold}");
    }
    
    /// <summary>
    /// 골드 소모
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (!IsSlotSelected || amount <= 0) return false;
        
        if (selectedPlayerData.currentGold >= amount)
        {
            selectedPlayerData.currentGold -= amount;
            SaveCurrentSlot();
            OnGoldChanged?.Invoke(selectedPlayerData.currentGold);
            
            if (showDebugLogs)
                Debug.Log($"💰 [PlayerDataManager] 골드 소모: -{amount}, 현재: {selectedPlayerData.currentGold}");
            return true;
        }
        else
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [PlayerDataManager] 골드 부족: 필요 {amount}, 보유 {selectedPlayerData.currentGold}");
            return false;
        }
    }
    
    /// <summary>
    /// 경험치 추가
    /// </summary>
    public void AddExp(int amount)
    {
        if (!IsSlotSelected)
        {
            if (showDebugLogs)
                Debug.LogWarning("⚠️ [PlayerDataManager] 슬롯이 선택되지 않아 경험치 추가 불가");
            return;
        }
        
        if (amount <= 0) return;
        
        int oldExp = selectedPlayerData.currentExp;
        int oldLevel = selectedPlayerData.currentLevel;
        
        selectedPlayerData.currentExp += amount;
        
        // 레벨업 체크
        while (selectedPlayerData.currentExp >= selectedPlayerData.expToNextLevel)
        {
            selectedPlayerData.currentExp -= selectedPlayerData.expToNextLevel;
            selectedPlayerData.currentLevel++;
            selectedPlayerData.expToNextLevel = CalculateExpToNextLevel(selectedPlayerData.currentLevel);
            OnLevelChanged?.Invoke(selectedPlayerData.currentLevel);
            
            if (showDebugLogs)
                Debug.Log($"🆙 [PlayerDataManager] 레벨업! 새 레벨: {selectedPlayerData.currentLevel}");
        }
        
        SaveCurrentSlot();
        OnExpChanged?.Invoke(selectedPlayerData.currentExp, selectedPlayerData.expToNextLevel);
        
        if (showDebugLogs)
            Debug.Log($"✨ [PlayerDataManager] 경험치 추가: +{amount} ({oldExp}→{selectedPlayerData.currentExp}) 레벨: {oldLevel}→{selectedPlayerData.currentLevel}");
    }
    
    /// <summary>
    /// 인벤토리에 아이템 추가
    /// </summary>
    public bool AddToInventory(EquipmentData item)
    {
        if (!IsSlotSelected || item == null) return false;
        
        // 🆕 인벤토리 상태 디버그
        Debug.Log($"📊 [PlayerDataManager] 인벤토리 상태: {selectedPlayerData.CurrentInventorySize}/{selectedPlayerData.MaxInventorySize}");
        
        if (selectedPlayerData.IsInventoryFull)
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [PlayerDataManager] 인벤토리가 가득 참! 최대 {selectedPlayerData.MaxInventorySize}개까지만 보관 가능합니다.");
            return false;
        }
        
        selectedPlayerData.runtimeInventoryItems.Add(item);
        SaveCurrentSlot();
        OnItemAddedToInventory?.Invoke(item);
        OnInventoryChanged?.Invoke();
        
        if (showDebugLogs)
            Debug.Log($"📦 [PlayerDataManager] 인벤토리 추가: {item.name} ({selectedPlayerData.CurrentInventorySize}/{selectedPlayerData.MaxInventorySize})");
        return true;
    }
    
    /// <summary>
    /// 인벤토리에서 아이템 제거
    /// </summary>
    public bool RemoveFromInventory(EquipmentData item)
    {
        if (!IsSlotSelected || item == null) return false;
        
        if (selectedPlayerData.runtimeInventoryItems.Remove(item))
        {
            SaveCurrentSlot();
            OnItemRemovedFromInventory?.Invoke(item);
            OnInventoryChanged?.Invoke();
            
            if (showDebugLogs)
                Debug.Log($"📦 [PlayerDataManager] 인벤토리 제거: {item.name}");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 아이템 장착
    /// </summary>
    public bool EquipItem(EquipmentData item, EquipmentSlot targetSlot)
    {
        if (!IsSlotSelected || item == null) return false;
        
        // 기존 장착 아이템이 있다면 인벤토리로
        var currentItem = selectedPlayerData.RuntimeEquippedItems[targetSlot];
        if (currentItem != null)
        {
            if (!AddToInventory(currentItem)) return false;
        }
        
        // 새 아이템 장착
        selectedPlayerData.RuntimeEquippedItems[targetSlot] = item;
        RemoveFromInventory(item);
        selectedPlayerData.SyncDictionaries();
        
        // 🆕 무기 장착 시 ActiveWeapon에도 실제 적용
        if (targetSlot == EquipmentSlot.MainWeapon && item.equipmentType == EquipmentType.Weapon)
        {
            var activeWeapon = FindObjectOfType<ActiveWeapon>();
            if (activeWeapon != null)
            {
                activeWeapon.EquipWeapon(item);
                if (showDebugLogs)
                    Debug.Log($"🔧 [PlayerDataManager] ActiveWeapon에 무기 적용: {item.equipmentName}");
            }
            else
            {
                Debug.LogWarning("⚠️ [PlayerDataManager] ActiveWeapon을 찾을 수 없어 물리적 무기 교체 실패!");
            }
        }
        
        // 🆕 장비 변경 시 PlayerRuntimeStats 스탯 재계산
        var playerRuntimeStats = FindObjectOfType<PlayerRuntimeStats>();
        if (playerRuntimeStats != null)
        {
            playerRuntimeStats.RecalculateAllStats();
            if (showDebugLogs)
                Debug.Log($"🎯 [PlayerDataManager] PlayerRuntimeStats 스탯 재계산 완료");
        }
        else
        {
            Debug.LogWarning("⚠️ [PlayerDataManager] PlayerRuntimeStats를 찾을 수 없어 스탯 재계산 실패!");
        }
        
        SaveCurrentSlot();
        OnItemEquipped?.Invoke(targetSlot, item);
        OnInventoryChanged?.Invoke();
        
        if (showDebugLogs)
            Debug.Log($"⚔️ [PlayerDataManager] 장비 착용: {item.name} → {targetSlot}");
        return true;
    }
    
    /// <summary>
    /// 아이템 해제
    /// </summary>
    public bool UnequipItem(EquipmentSlot slot)
    {
        if (!IsSlotSelected) return false;
        
        var item = selectedPlayerData.RuntimeEquippedItems[slot];
        if (item == null) return false;
        
        selectedPlayerData.RuntimeEquippedItems[slot] = null;
        if (!AddToInventory(item)) return false;
        
        selectedPlayerData.SyncDictionaries();
        SaveCurrentSlot();
        OnItemUnequipped?.Invoke(slot, item);
        OnInventoryChanged?.Invoke();
        
        if (showDebugLogs)
            Debug.Log($"⚔️ [PlayerDataManager] 장비 해제: {item.name} ← {slot}");
        return true;
    }
    
    #endregion
    
    #region 🔧 유틸리티 메서드
    
    /// <summary>
    /// 저장 폴더 생성
    /// </summary>
    private void CreateSaveDirectory()
    {
        try
        {
            if (!Directory.Exists(SaveDirectoryPath))
            {
                Directory.CreateDirectory(SaveDirectoryPath);
                if (showDebugLogs)
                    Debug.Log($"📁 [PlayerDataManager] 저장 폴더 생성: {SaveDirectoryPath}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"💥 [PlayerDataManager] 저장 폴더 생성 실패: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 슬롯 파일 경로 생성
    /// </summary>
    private string GetSlotFilePath(int slotIndex)
    {
        return Path.Combine(SaveDirectoryPath, $"slot_{slotIndex}.json");
    }
    
    /// <summary>
    /// 다음 레벨까지 필요한 경험치 계산
    /// </summary>
    private int CalculateExpToNextLevel(int level)
    {
        return 100 + (level - 1) * 50; // 기본 100 + 레벨당 50씩 증가
    }
    
    /// <summary>
    /// SelectedPlayerData ScriptableObject 초기화
    /// </summary>
    private void InitializeSelectedPlayerData()
    {
        if (selectedPlayerData == null)
        {
            // Resources에서 찾기 시도
            selectedPlayerData = Resources.Load<SelectedPlayerData>("SelectedPlayerData");
            
            if (selectedPlayerData == null)
            {
                Debug.LogWarning("⚠️ [PlayerDataManager] SelectedPlayerData ScriptableObject를 찾을 수 없습니다. Resources 폴더에 생성해주세요.");
                // 런타임에 생성 (에디터에서만 가능)
                #if UNITY_EDITOR
                selectedPlayerData = ScriptableObject.CreateInstance<SelectedPlayerData>();
                #endif
            }
        }
    }
    
    /// <summary>
    /// 모든 UI 이벤트 트리거
    /// </summary>
    private void TriggerAllUIEvents()
    {
        if (!IsSlotSelected) return;
        
        OnGoldChanged?.Invoke(selectedPlayerData.currentGold);
        OnLevelChanged?.Invoke(selectedPlayerData.currentLevel);
        OnExpChanged?.Invoke(selectedPlayerData.currentExp, selectedPlayerData.expToNextLevel);
        OnInventoryChanged?.Invoke();
    }
    
    /// <summary>
    /// UI 초기화 코루틴
    /// </summary>
    private IEnumerator InitializeUI()
    {
        yield return new WaitForEndOfFrame();
        
        // 골드 텍스트 찾기
        if (goldText == null)
        {
            var goldObject = GameObject.Find(COIN_AMOUNT_TEXT);
            if (goldObject != null)
                goldText = goldObject.GetComponent<TMP_Text>();
        }
        
        // UI 업데이트
        if (goldText != null && IsSlotSelected)
            goldText.text = selectedPlayerData.currentGold.ToString();
    }
    
    #endregion
    
    #region 🧹 정리 및 호환성 메서드
    
    /// <summary>
    /// 기존 PlayerPrefs 데이터 마이그레이션 (한 번만 실행)
    /// </summary>
    [ContextMenu("기존 PlayerPrefs 데이터 마이그레이션")]
    public void MigrateFromPlayerPrefs()
    {
        // 기존 PlayerPrefs에서 새 슬롯 시스템으로 마이그레이션하는 로직
        // 필요시 구현
        if (showDebugLogs)
            Debug.Log("🔄 [PlayerDataManager] PlayerPrefs 마이그레이션은 필요시 구현 예정");
    }
    
    /// <summary>
    /// 모든 슬롯 데이터 완전 삭제 (디버그용)
    /// </summary>
    [ContextMenu("모든 슬롯 데이터 삭제")]
    public void DeleteAllSlots()
    {
        try
        {
            if (Directory.Exists(SaveDirectoryPath))
            {
                Directory.Delete(SaveDirectoryPath, true);
                CreateSaveDirectory();
            }
            
            currentSlotIndex = -1;
            selectedPlayerData.Reset();
            LoadAllSlots();
            
            if (showDebugLogs)
                Debug.Log("🗑️ [PlayerDataManager] 모든 슬롯 데이터 삭제 완료");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"💥 [PlayerDataManager] 데이터 삭제 실패: {ex.Message}");
        }
    }
    
    #endregion

    #region 🔧 기존 호환성 메서드들 (SelectedPlayerData 위임) 에 추가

    /// <summary>
    /// 🔄 기존 호환성: 현재 플레이어 타입 설정
    /// </summary>
    public void SetCurrentPlayerType(PlayerType playerType)
    {
        if (!IsSlotSelected) 
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [PlayerDataManager] 슬롯이 선택되지 않아 플레이어 타입 설정 불가: {playerType}");
            return;
        }
        
        selectedPlayerData.selectedPlayerType = playerType;
        selectedPlayerData.weaponName = playerType.GetDefaultWeapon(); // 기존 호환성
        SaveCurrentSlot();
        
        if (showDebugLogs)
            Debug.Log($"🎯 [PlayerDataManager] 플레이어 타입 설정: {playerType}");
    }

    /// <summary>
    /// 🔄 기존 호환성: 현재 플레이어 타입 가져오기
    /// </summary>
    public PlayerType GetCurrentPlayerType()
    {
        if (IsSlotSelected)
            return selectedPlayerData.selectedPlayerType;
            
        // Fallback: GameManager에서 가져오기
        if (GameManager.Instance?.selectedPlayerData != null)
            return GameManager.Instance.selectedPlayerData.selectedPlayerType;
            
        return PlayerType.Warrior; // 기본값
    }

    /// <summary>
    /// 🔄 기존 호환성: 현재 골드 가져오기
    /// </summary>
    public int GetCurrentGold()
    {
        return CurrentGold;
    }

    /// <summary>
    /// 🔄 기존 호환성: 장비 타입에 따른 자동 슬롯 결정 장착
    /// </summary>
    public bool EquipItem(EquipmentData item)
    {
        if (!IsSlotSelected || item == null) return false;
        
        // 장비 타입에 따라 적절한 슬롯 결정
        EquipmentSlot targetSlot = DetermineEquipmentSlot(item);
        
        return EquipItem(item, targetSlot);
    }

    /// <summary>
    /// 장비 데이터로부터 적절한 장비 슬롯 결정
    /// </summary>
    private EquipmentSlot DetermineEquipmentSlot(EquipmentData item)
    {
        switch (item.equipmentType)
        {
            case EquipmentType.Weapon:
                return EquipmentSlot.MainWeapon;
            
            case EquipmentType.Armor:
                // 방어구의 경우 아이템 이름이나 다른 조건으로 세부 슬롯 결정
                string itemName = item.equipmentName.ToLower();
                if (itemName.Contains("helmet") || itemName.Contains("헬멧"))
                    return EquipmentSlot.Helmet;
                else if (itemName.Contains("boots") || itemName.Contains("신발") || itemName.Contains("부츠"))
                    return EquipmentSlot.Boots;
                else if (itemName.Contains("shield") || itemName.Contains("방패"))
                    return EquipmentSlot.Shield;
                else
                    return EquipmentSlot.Armor; // 기본값: 갑옷
                
            case EquipmentType.Accessory:
                // Ring1이 비어있으면 Ring1, 아니면 Ring2, 둘 다 차있으면 Necklace
                if (selectedPlayerData.RuntimeEquippedItems[EquipmentSlot.Ring1] == null)
                    return EquipmentSlot.Ring1;
                else if (selectedPlayerData.RuntimeEquippedItems[EquipmentSlot.Ring2] == null)
                    return EquipmentSlot.Ring2;
                else
                    return EquipmentSlot.Necklace;
                
            default:
                return EquipmentSlot.MainWeapon; // 기본값
        }
    }

    /// <summary>
    /// 🔄 기존 호환성: 클래스별 세부 데이터 저장 (더 이상 사용되지 않음)
    /// </summary>
    [System.Obsolete("SaveClassData는 더 이상 사용되지 않습니다. SelectedPlayerData를 직접 사용하세요.", false)]
    public void SaveClassData(PlayerType classType, object data)
    {
        if (showDebugLogs)
            Debug.LogWarning($"⚠️ [PlayerDataManager] SaveClassData는 deprecated입니다. {classType} 데이터는 SelectedPlayerData로 관리됩니다.");
        
        // 현재 슬롯 저장
        SaveCurrentSlot();
    }

    /// <summary>
    /// 🔄 기존 호환성: 클래스별 세부 데이터 로드 (더 이상 사용되지 않음)
    /// </summary>
    [System.Obsolete("LoadClassData는 더 이상 사용되지 않습니다. SelectedPlayerData를 직접 사용하세요.", false)]
    public object LoadClassData(PlayerType classType)
    {
        if (showDebugLogs)
            Debug.LogWarning($"⚠️ [PlayerDataManager] LoadClassData는 deprecated입니다. {classType} 데이터는 SelectedPlayerData에서 확인하세요.");
        
        return null; // 더 이상 사용되지 않음
    }

    #endregion

    #region 🧪 테스트 및 디버그 메서드

    /// <summary>
    /// 테스트용 기본 슬롯 생성
    /// </summary>
    [ContextMenu("테스트 슬롯 생성")]
    public void CreateTestSlots()
    {
        // 슬롯 0: Warrior
        CreateNewSlot(0, PlayerType.Warrior, "전사테스트");
        
        // 슬롯 1: Assasin  
        CreateNewSlot(1, PlayerType.Assasin, "어쌔신테스트");
        
        // 슬롯 2: Wizard
        CreateNewSlot(2, PlayerType.Wizard, "마법사테스트");
        
        // 첫 번째 슬롯 선택
        SelectSlot(0);
        
        if (showDebugLogs)
            Debug.Log("🧪 [PlayerDataManager] 테스트 슬롯 3개 생성 완료!");
    }

    /// <summary>
    /// 현재 슬롯 상태 출력
    /// </summary>
    [ContextMenu("슬롯 상태 확인")]
    public void PrintSlotStatus()
    {
        Debug.Log($"📊 [PlayerDataManager] === 슬롯 상태 ===");
        Debug.Log($"최대 슬롯: {maxSlots}, 사용중: {GetUsedSlotCount()}개, 현재 선택: {currentSlotIndex}");
        
        for (int i = 0; i < playerSlots.Count; i++)
        {
            var slot = playerSlots[i];
            if (slot.isSlotUsed)
            {
                Debug.Log($"슬롯 {i}: {slot}");
            }
            else
            {
                Debug.Log($"슬롯 {i}: 비어있음");
            }
        }
        
        if (IsSlotSelected)
        {
            Debug.Log($"🎯 현재 활성 데이터: {selectedPlayerData}");
        }
    }

    /// <summary>
    /// 저장 폴더 경로 확인
    /// </summary>
    [ContextMenu("저장 폴더 열기")]
    public void OpenSaveDirectory()
    {
        Debug.Log($"📁 [PlayerDataManager] 저장 폴더: {SaveDirectoryPath}");
        
        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            System.Diagnostics.Process.Start("explorer.exe", SaveDirectoryPath.Replace('/', '\\'));
        }
        else
        {
            System.Diagnostics.Process.Start("open", SaveDirectoryPath);
        }
    }

    #endregion

    /// <summary>
    /// 🆕 추가: 게임 시작 전 SelectedPlayerData 유효성 검증
    /// </summary>
    public bool ValidateSelectedPlayerData()
    {
        if (selectedPlayerData == null) return false;
        
        // 현재 선택된 슬롯이 실제로 존재하는지 확인
        if (currentSlotIndex >= 0)
        {
            var slotData = GetSlotData(currentSlotIndex);
            if (slotData == null || !slotData.isSlotUsed)
            {
                Debug.LogWarning($"⚠️ [PlayerDataManager] 선택된 슬롯 {currentSlotIndex}이 유효하지 않음 - 데이터 초기화");
                currentSlotIndex = -1;
                selectedPlayerData.Reset();
                return false;
            }
        }
        
        // SelectedPlayerData가 유효한 캐릭터 정보를 가지고 있는지 확인
        if (selectedPlayerData.selectedPlayerType == PlayerType.None || 
            selectedPlayerData.selectedSlotIndex < 0)
        {
            Debug.LogWarning($"⚠️ [PlayerDataManager] SelectedPlayerData가 유효하지 않음 - 초기화 필요");
            selectedPlayerData.Reset();
            return false;
        }
        
        return true;
    }
} 