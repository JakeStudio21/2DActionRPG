using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Linq;
using System.IO;
using Systems; // Phase 7: EnhancementResult, EnhancementSystem 등

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
    [Tooltip("⭐ 런타임 데이터의 단일 소스 (Single Source of Truth)\n" +
             "모든 런타임 데이터 접근은 이 객체를 통해 수행\n" +
             "변경 후 반드시 MarkDirty() 호출 필수!")]
    public SelectedPlayerData selectedPlayerData; // ScriptableObject 참조 (public으로 변경)
    
    [Header("📊 슬롯 상태")]
    [SerializeField] private List<PlayerSlotData> playerSlots = new List<PlayerSlotData>(); // 현재 로드된 슬롯들
    [SerializeField] private int currentSlotIndex = -1; // 현재 활성 슬롯 (-1: 미선택)
    [SerializeField] private int lastSelectedSlotIndex = 0; // 🆕 마지막 선택된 슬롯 (자동 선택용)
    
    [Header("🔧 UI 관리")]
    private TMP_Text goldText;
    private const string COIN_AMOUNT_TEXT = "Gold Amount Text";
    
    [Header("📊 디버그")]
    [SerializeField] private bool showDebugLogs = false;
    
    [Header("🔧 Dirty Flag 시스템")]
    [SerializeField] private bool isDirty = false;
    [SerializeField] private bool isLoading = false; // 로딩 중 저장 방지
    
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
    public event Action<int> OnSlotSelected; // 슬롯 선택 시 (기존 - 즉시 갱신용)
    public event Action<PlayerSlotData> OnSlotDataChanged; // 슬롯 데이터 변경 시
    public event Action<int> OnCharacterCreated; // 🆕 캐릭터 생성 완료 시
    public event Action<int> OnSlotLazyLoaded; // 🆕 지연 로드 완료 시 (새로운 이벤트)

    public static event System.Action<SelectedPlayerData> OnSelectedPlayerDataChanged;
public static event System.Action<int> OnPlayerGoldChanged;
public static event System.Action<int> OnPlayerLevelChanged;
public static event System.Action<EquipmentData> OnPlayerInventoryChanged;

    // 🆕 로비-인게임 공용 이벤트 시스템
    /// <summary>
    /// 모든 슬롯 클릭 시 발생 (인게임/로비 공통)
    /// 기본적인 슬롯 클릭 이벤트로, 장착/해제 등 기본 기능에 사용
    /// V2: ItemInstanceID 추가 (귀속 체크용)
    /// </summary>
    public event Action<EquipmentData, int, ItemInstanceID> OnSlotClicked; // (장비데이터, 슬롯인덱스, 인스턴스ID)
    
    /// <summary>
    /// 로비에서만 상세 정보가 필요할 때 발생
    /// 장비의 상세 스탯 정보를 표시하는 패널 요청에 사용
    /// </summary>
    public event Action<EquipmentData> OnItemDetailRequested; // (장비데이터)
    
    // 🆕 스테이지 진행도 이벤트
    public event Action<string> OnStageUnlocked;
    public event Action<string, bool> OnStageCompleted; // stageId, isFirstClear
    public event Action<string> OnFirstClearRewardClaimed;
    
    // 🆕 이벤트 발생 메서드들 (외부에서 호출 가능)
    /// <summary>
    /// 스테이지 해금 이벤트 발생
    /// </summary>
    public void TriggerStageUnlocked(string stageId)
    {
        OnStageUnlocked?.Invoke(stageId);
    }
    
    /// <summary>
    /// 스테이지 완료 이벤트 발생
    /// </summary>
    public void TriggerStageCompleted(string stageId, bool isFirstClear)
    {
        OnStageCompleted?.Invoke(stageId, isFirstClear);
    }
    
    /// <summary>
    /// 첫 클리어 보상 수령 이벤트 발생
    /// </summary>
    public void TriggerFirstClearRewardClaimed(string stageId)
    {
        OnFirstClearRewardClaimed?.Invoke(stageId);
    }
    
    /// <summary>
    /// 레벨 변경 이벤트 발생 (외부 호출용 - 디버그 도구 등) ✅
    /// </summary>
    public void TriggerLevelChanged(int newLevel)
    {
        OnLevelChanged?.Invoke(newLevel);
        
        if (showDebugLogs)
            Debug.Log($"🆙 [PlayerDataManager] 레벨 변경 이벤트 발생: Lv.{newLevel}");
    }
    
    // 접근자 프로퍼티 (AccountDataManager 위임 - V2 계정 공유 골드)
    public int CurrentGold => AccountDataManager.Instance?.CurrentGold ?? 0;
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
    
    // 🆕 앱 생명주기 이벤트 처리 (게임 종료 시 저장)
    private void OnApplicationPause(bool pauseStatus)
    {
        #if UNITY_EDITOR
        // Unity Editor에서는 Pause 이벤트 무시 (Editor UI 조작 시 불필요한 저장 방지)
        if (showDebugLogs)
            Debug.Log($"⏭️ [OnApplicationPause] Unity Editor에서는 무시");
        return;
        #endif
        
        if (pauseStatus && IsSlotSelected)
        {
            SaveOnMeaningfulEvent("ApplicationPause");
        }
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        #if UNITY_EDITOR
        // Unity Editor에서는 Focus 이벤트 무시 (Inspector/Hierarchy 클릭 시 불필요한 저장 방지)
        if (showDebugLogs)
            Debug.Log($"⏭️ [OnApplicationFocus] Unity Editor에서는 무시");
        return;
        #endif
        
        if (!hasFocus && IsSlotSelected)
        {
            SaveOnMeaningfulEvent("ApplicationFocusLost");
        }
    }
    
    private void OnApplicationQuit()
    {
        if (IsSlotSelected)
        {
            SaveOnMeaningfulEvent("ApplicationQuit");
        }
    }
    
    // ============================================
    // 🔧 Dirty Flag 시스템
    // ============================================
    
    /// <summary>
    /// 데이터가 변경되었음을 표시 (저장 필요)
    /// </summary>
    public void MarkDirty()
    {
        if (isLoading)
        {
            if (showDebugLogs)
                Debug.LogWarning("⚠️ [Dirty Flag] 로딩 중에는 Dirty를 켜지 않음");
            return;
        }
        
        isDirty = true;
        if (showDebugLogs)
            Debug.Log($"🔧 [Dirty Flag] 슬롯 {currentSlotIndex} 데이터 변경됨");
    }
    
    /// <summary>
    /// 저장 완료 후 Dirty 플래그 초기화
    /// </summary>
    private void ClearDirty()
    {
        isDirty = false;
        if (showDebugLogs)
            Debug.Log($"✅ [Dirty Flag] 슬롯 {currentSlotIndex} 저장 완료 → Dirty 플래그 초기화");
    }
    
    /// <summary>
    /// lastPlayTime을 현재 시각으로 업데이트 (의미 있는 이벤트에서만 호출)
    /// </summary>
    private void UpdateLastPlayTime()
    {
        if (selectedPlayerData != null)
        {
            selectedPlayerData.lastPlayTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            MarkDirty(); // lastPlayTime 갱신도 변경으로 간주
            if (showDebugLogs)
                Debug.Log($"⏰ [LastPlayTime] 갱신: {selectedPlayerData.lastPlayTime}");
        }
    }
    
    /// <summary>
    /// 의미 있는 이벤트(스테이지 클리어, 앱 종료 등)에서 호출되는 저장 메서드
    /// lastPlayTime을 갱신하고, Dirty가 true일 때만 실제 저장 수행
    /// </summary>
    /// <param name="eventName">이벤트 이름 (디버깅용)</param>
    public void SaveOnMeaningfulEvent(string eventName)
    {
        if (showDebugLogs)
            Debug.Log($"📌 [SaveOnMeaningfulEvent] 이벤트: {eventName}");
        
        // lastPlayTime 갱신 (의미 있는 플레이 종료 시점)
        UpdateLastPlayTime();
        
        // Dirty 체크: 변경사항이 있을 때만 저장
        if (!isDirty)
        {
            if (showDebugLogs)
                Debug.Log($"⏭️ [SaveOnMeaningfulEvent] Dirty가 false → 저장 생략");
            return;
        }
        
        if (showDebugLogs)
            Debug.Log($"💾 [SaveOnMeaningfulEvent] SaveCurrentSlot() 호출 중...");
        
        // 통일된 저장 경로: SaveCurrentSlot() 호출
        bool success = SaveCurrentSlot();
        
        if (showDebugLogs)
            Debug.Log($"💾 [SaveOnMeaningfulEvent] SaveCurrentSlot() 완료 - 성공: {success}");
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
            // ========================================
            // ✅ 원칙: SaveSlotData()가 이미 파일+메모리 동기화 완료
            // - File.WriteAllText() → JSON 저장
            // - playerSlots[index] = slotData → 메모리 캐시 업데이트
            // - LoadAllSlots() 호출 시 파일 시스템 캐시로 인한 동기화 지연 문제 발생 가능
            // ========================================
            
            // 🆕 신규 캐릭터 생성 후 즉시 선택 (핵심 수정)
            bool selectSuccess = SelectSlot(slotIndex);
            if (selectSuccess)
            {
                Debug.Log($"🎯 [PlayerDataManager] 신규 캐릭터 슬롯 {slotIndex} 자동 선택 완료");
                
                // ✅ 추가: 신규 캐릭터도 GameManager 동기화 확인
                if (GameManager.Instance?.selectedPlayerData != null)
                {
                    Debug.Log($"🔗 [PlayerDataManager] 신규 캐릭터 GameManager 동기화 확인: {GameManager.Instance.selectedPlayerData.selectedPlayerType}");
                }
                
                // StageProgressManager는 SelectSlot에서 자동으로 초기화됨
                if (StageSystem.StageProgressManager.Instance != null)
                {
                    StageSystem.StageProgressManager.Instance.InitializeFor(slotIndex);
                    Debug.Log($"🎯 [PlayerDataManager] 신규 캐릭터 슬롯 {slotIndex} StageProgressManager 초기화 완료");
                }
            }
            
            // 🆕 캐릭터 생성 완료 이벤트 발생
            OnCharacterCreated?.Invoke(slotIndex);
            
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
            
            // ========================================
            // ✅ 원칙: StageProgressManager 캐시 강제 초기화
            // - StageProgressManager는 DontDestroyOnLoad 싱글톤이므로 씬 전환 시에도 유지됨
            // - 슬롯 삭제 시 progressCache를 초기화하지 않으면 이전 진행도가 남음
            // - ClearProgressCache()로 캐시만 초기화 (currentSlotIndex는 -1로 유지)
            // ========================================
            if (StageSystem.StageProgressManager.Instance != null)
            {
                // progressCache 강제 초기화
                StageSystem.StageProgressManager.Instance.ClearProgressCache();
                Debug.Log($"🗑️ [PlayerDataManager] StageProgressManager 캐시 초기화 완료");
            }
            
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
                
                // ========================================
                // ✅ Phase 2: 스테이지 진행도 명시적 초기화 (안전장치)
                // ========================================
                selectedPlayerData.stageProgresses.Clear();
                selectedPlayerData.clearedChapters.Clear();
                
                // ========================================
                // ✅ Phase 2: 컷신 시청 기록 명시적 초기화
                // ========================================
                selectedPlayerData.seenChapterStart.Clear();
                selectedPlayerData.seenChapterClear.Clear();
                selectedPlayerData.seenStageEnter.Clear();
                selectedPlayerData.seenStageClear.Clear();
                
                // ========================================
                // ✅ Phase 2: 위치 정보 명시적 초기화
                // ========================================
                selectedPlayerData.currentChapterId = 1;
                selectedPlayerData.lastPlayedStageId = "";
                
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
    /// <summary>
    /// ⭐ 현재 슬롯 저장 (V2 데이터 보존 버전)
    /// SaveToSlotData()는 Legacy 데이터만 저장하므로, V2 데이터를 수동으로 보존!
    /// </summary>
    public bool SaveCurrentSlot()
    {
        if (showDebugLogs)
            Debug.Log($"💾 [SaveCurrentSlot] 시작 - IsSlotSelected: {IsSlotSelected}, isDirty: {isDirty}");
        
        if (!IsSlotSelected)
        {
            if (showDebugLogs)
                Debug.LogError($"❌ [SaveCurrentSlot] 슬롯이 선택되지 않음");
            return false;
        }
        
        // 🔧 Dirty 체크: 변경사항이 없으면 저장 생략
        if (!isDirty)
        {
            if (showDebugLogs)
                Debug.Log("⏭️ [SaveCurrentSlot] Dirty가 false → 저장 생략");
            return true; // 저장 불필요 = 성공으로 간주
        }
        
        if (showDebugLogs)
            Debug.Log($"📦 [SaveCurrentSlot] SaveToSlotData() 호출 중...");
        
        // 1. ⭐ 기존 V2 데이터 백업
        var existingSlotData = GetSlotData(currentSlotIndex);
        var backupBagIds = existingSlotData?.characterBagInstanceIds != null 
            ? new List<ItemInstanceID>(existingSlotData.characterBagInstanceIds)
            : new List<ItemInstanceID>();
        var backupEquippedRecords = existingSlotData?.equippedRecords != null
            ? new List<EquippedRecord>(existingSlotData.equippedRecords)
            : new List<EquippedRecord>();
        var backupBagMaterials = existingSlotData?.characterBagMaterials != null
            ? new List<MaterialStack>(existingSlotData.characterBagMaterials)
            : new List<MaterialStack>();
        
        if (showDebugLogs)
            Debug.Log($"📦 [SaveCurrentSlot] V2 데이터 백업: 가방 {backupBagIds.Count}개, 장착 {backupEquippedRecords.Count}개, 재료 {backupBagMaterials.Count}개");
        
        // 2. Legacy 데이터 저장 (SaveToSlotData)
        var slotData = selectedPlayerData.SaveToSlotData();
        
        // 3. ⭐ V2 데이터 복원 (characterBagInstanceIds + characterBagMaterials, equippedRecords는 SaveToSlotData에서 처리)
        slotData.characterBagInstanceIds = backupBagIds;
        slotData.characterBagMaterials = backupBagMaterials; // ⭐ 재료 복원!
        // ❌ slotData.equippedRecords = backupEquippedRecords; // 제거! SaveToSlotData()가 이미 처리함
        
        if (showDebugLogs)
        {
            Debug.Log($"📦 [SaveCurrentSlot] V2 데이터 복원 완료: 가방 {slotData.characterBagInstanceIds.Count}개");
            Debug.Log($"💾 [SaveCurrentSlot] V2 장착 레코드: {slotData.equippedRecords.Count}개 (SaveToSlotData에서 생성)");
            Debug.Log($"📦 [SaveCurrentSlot] SaveToSlotData() 완료 - 장착 아이템: {slotData.equippedItemNames.Count}개 (Legacy)");
        }
        
        if (showDebugLogs)
            Debug.Log($"💾 [SaveCurrentSlot] SaveSlotData() 호출 중...");
        
        // 4. 저장 실행
        bool success = SaveSlotData(slotData);
        
        if (showDebugLogs)
            Debug.Log($"💾 [SaveCurrentSlot] SaveSlotData() 완료 - 성공: {success}");
        
        // 저장 성공 시 Dirty 플래그 초기화
        if (success)
        {
            ClearDirty();
            if (showDebugLogs)
                Debug.Log($"✅ [SaveCurrentSlot] Dirty 플래그 초기화 완료");
        }
        
        return success;
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
    /// 🔄 슬롯 전환 (완전한 데이터 교체 모드) - 즉시 갱신 방식
    /// SelectedPlayerData를 선택된 슬롯 데이터로 완전히 갱신
    /// 🆕 지연 갱신이 필요한 경우 SetSelectedSlotIndex() + LazyLoadSlotData() 사용 권장
    /// </summary>
    public bool SelectSlot(int slotIndex)
    {
        Debug.Log($"═══════════════════════════════════════════════════════");
        Debug.Log($"🔄 [SelectSlot] 슬롯 {slotIndex} 전환 시작");
        Debug.Log($"   ⏰ 현재 선택된 슬롯: {currentSlotIndex}");
        Debug.Log($"═══════════════════════════════════════════════════════");
        
        if (slotIndex < 0 || slotIndex >= maxSlots) return false;
        
        var slotData = GetSlotData(slotIndex);
        if (slotData == null || !slotData.isSlotUsed)
        {
            Debug.LogError($"❌ [PlayerDataManager] 슬롯 {slotIndex}는 사용되지 않음");
            return false;
        }
        
        // 🆕 디버그: 로드할 슬롯 데이터 상태 확인
        Debug.Log($"📊 [SelectSlot] 슬롯 {slotIndex} 데이터 확인:");
        Debug.Log($"   - 캐릭터: {slotData.playerName} ({slotData.playerType})");
        Debug.Log($"   - 레벨: {slotData.level}, 골드: {slotData.gold}");
        Debug.Log($"   - 인벤토리 아이템: {slotData.inventoryItemNames.Count}개");
        Debug.Log($"   - 장착 아이템: {slotData.equippedItemNames.Count}개");
        Debug.Log($"   - 📚 스킬: {slotData.skills?.Count ?? 0}개, SP: {slotData.usedSP}/{slotData.totalSP}");
        
        for (int i = 0; i < Mathf.Min(slotData.inventoryItemNames.Count, 5); i++)
        {
            Debug.Log($"     📦 인벤토리[{i}]: {slotData.inventoryItemNames[i]}");
        }
        
        foreach (var equipped in slotData.equippedItemNames)
        {
            Debug.Log($"     ⚔️ 장착[{equipped.Key}]: {equipped.Value}");
        }
        
        // 💰 V2 마이그레이션: Slot 골드 → Account 골드 이동
        if (slotData.gold > 0 && AccountDataManager.Instance != null)
        {
            int oldAccountGold = AccountDataManager.Instance.CurrentGold;
            int slotGold = slotData.gold;
            
            // 계정 골드에 추가 (중복 방지: 최초 1회만)
            if (oldAccountGold == 0)
            {
                Debug.Log($"💰 [SelectSlot] V2 마이그레이션: Slot {slotIndex}의 골드 {slotGold} → Account로 이동");
                AccountDataManager.Instance.AddGold(slotGold);
                AccountDataManager.Instance.Save();
                
                // 슬롯 골드 초기화
                slotData.gold = 0;
                SaveSlotData(slotData);
                
                Debug.Log($"✅ [SelectSlot] 골드 마이그레이션 완료: Account 골드 = {AccountDataManager.Instance.CurrentGold}");
            }
            else
            {
                // 이미 Account에 골드가 있으면 슬롯 골드만 초기화
                Debug.Log($"⚠️ [SelectSlot] Account에 이미 골드 존재 ({oldAccountGold}) - Slot 골드 초기화만 진행");
                slotData.gold = 0;
                SaveSlotData(slotData);
            }
        }
        
        // 🔧 로딩 시작: Dirty 플래그 방지
        isLoading = true;
        
        // 🎯 핵심: 기존의 완벽한 LoadFromSlotData 활용
        currentSlotIndex = slotIndex;
        
        // 🆕 마지막 선택 슬롯 저장
        lastSelectedSlotIndex = slotIndex;
        SaveLastSelectedSlotIndex();
        
        if (selectedPlayerData != null)
        {
            Debug.Log($"📥 [SelectSlot] SelectedPlayerData에 로드 시작...");
            selectedPlayerData.LoadFromSlotData(slotData);
            
            // 🆕 디버그: 로드 후 SelectedPlayerData 상태 확인
            Debug.Log($"✅ [SelectSlot] SelectedPlayerData 로드 완료:");
            Debug.Log($"   - selectedSlotIndex: {selectedPlayerData.selectedSlotIndex}");
            Debug.Log($"   - playerName: {selectedPlayerData.playerName}");
            Debug.Log($"   - playerType: {selectedPlayerData.selectedPlayerType}");
            Debug.Log($"   - level: {selectedPlayerData.currentLevel}");
            Debug.Log($"   - gold: {selectedPlayerData.currentGold}");
            Debug.Log($"   - 인벤토리 아이템: {selectedPlayerData.runtimeInventoryItems.Count}개");
            Debug.Log($"   - 장착 아이템 (Data): {selectedPlayerData.RuntimeEquippedItems.Count}개");
            Debug.Log($"   - 장착 아이템 (InstanceIds): {selectedPlayerData.RuntimeEquippedInstanceIds.Count}개");
            
            for (int i = 0; i < Mathf.Min(selectedPlayerData.runtimeInventoryItems.Count, 5); i++)
            {
                var item = selectedPlayerData.runtimeInventoryItems[i];
                Debug.Log($"     📦 selectedPlayerData.inventory[{i}]: {item?.equipmentName ?? "null"}");
            }
            
            foreach (var equipped in selectedPlayerData.RuntimeEquippedItems)
            {
                if (equipped.Value != null)
                    Debug.Log($"     ⚔️ selectedPlayerData.equipped[{equipped.Key}]: {equipped.Value.equipmentName}");
            }
            
            // ✅ 추가: GameManager와 완벽 동기화 (핵심 수정)
            if (GameManager.Instance?.selectedPlayerData != null)
            {
                Debug.Log($"🔗 [SelectSlot] GameManager.selectedPlayerData 동기화 시작...");
                GameManager.Instance.selectedPlayerData.LoadFromSlotData(slotData);
                Debug.Log($"✅ [SelectSlot] GameManager.selectedPlayerData 동기화 완료");
            }
            else
            {
                Debug.LogError($"❌ [PlayerDataManager] GameManager 동기화 실패 - GameManager 또는 selectedPlayerData가 null");
            }
        }
        
        // 이벤트 발생
        Debug.Log($"📢 [SelectSlot] 이벤트 발생: OnSlotSelected({slotIndex})");
        OnSlotSelected?.Invoke(slotIndex);
        TriggerAllUIEvents();
        
        // 🔧 로딩 완료: Dirty 플래그 초기화
        isLoading = false;
        isDirty = false; // 로드 직후는 깨끗한 상태
        
        Debug.Log($"═══════════════════════════════════════════════════════");
        Debug.Log($"✅ [SelectSlot] 슬롯 {slotIndex} 전환 완료");
        Debug.Log($"═══════════════════════════════════════════════════════");
        
        return true;
    }

    /// <summary>
    /// 🔧 지연 갱신: 슬롯 인덱스만 저장 (UI 갱신 없음)
    /// ⚠️ 경고: 이 메서드는 데이터 손상 위험이 있습니다. SelectSlot()을 사용하세요.
    /// </summary>
    [System.Obsolete("SetSelectedSlotIndex는 데이터 손상 위험이 있습니다. SelectSlot()을 사용하세요.", false)]
    public void SetSelectedSlotIndex(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots)
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [PlayerDataManager] 잘못된 슬롯 인덱스: {slotIndex}");
            return;
        }
        
        var slotData = GetSlotData(slotIndex);
        if (slotData == null || !slotData.isSlotUsed)
        {
            if (showDebugLogs)
                Debug.LogWarning($"⚠️ [PlayerDataManager] 슬롯 {slotIndex}는 사용되지 않음 (지연 모드)");
            return;
        }
        
        // 슬롯 인덱스만 저장 (데이터 로드 없음)
        currentSlotIndex = slotIndex;
        lastSelectedSlotIndex = slotIndex;
        SaveLastSelectedSlotIndex();
        
        // 🔧 추가: selectedPlayerData.selectedSlotIndex도 동기화 (버그 수정)
        if (selectedPlayerData != null)
        {
            selectedPlayerData.selectedSlotIndex = slotIndex;
        }
        
        if (showDebugLogs)
            Debug.Log($"🔄 [PlayerDataManager] 슬롯 {slotIndex} 선택 저장 (지연 모드) - {slotData.playerName}");
    }

    /// <summary>
    /// 🔧 지연 갱신: 필요 시에만 슬롯 데이터 완전 로드
    /// </summary>
    public bool LazyLoadSlotData(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots)
        {
            if (showDebugLogs)
                Debug.LogError($"❌ [PlayerDataManager] 잘못된 슬롯 인덱스: {slotIndex}");
            return false;
        }
        
        var slotData = GetSlotData(slotIndex);
        if (slotData == null || !slotData.isSlotUsed)
        {
            if (showDebugLogs)
                Debug.LogError($"❌ [PlayerDataManager] 슬롯 {slotIndex} 데이터 없음");
            return false;
        }
        
        // SelectedPlayerData에 완전 로드
        if (selectedPlayerData != null)
        {
            selectedPlayerData.LoadFromSlotData(slotData);
            if (showDebugLogs)
                Debug.Log($"🔄 [PlayerDataManager] 슬롯 {slotIndex} 지연 로드 완료: {slotData.playerName}({slotData.playerType}) - 골드:{slotData.gold}, 레벨:{slotData.level}, 인벤토리:{slotData.inventoryItemNames.Count}개");
            
            // ✅ 추가: 지연 로드도 GameManager 동기화
            if (GameManager.Instance?.selectedPlayerData != null)
            {
                GameManager.Instance.selectedPlayerData.LoadFromSlotData(slotData);
                Debug.Log($"🔗 [PlayerDataManager] 지연 로드 GameManager 동기화 완료: {slotData.playerType}");
            }
        }
        
        // 현재 슬롯 인덱스 업데이트
        currentSlotIndex = slotIndex;
        
        // 지연 로드 완료 이벤트 발생 (선택적 UI 갱신)
        OnSlotLazyLoaded?.Invoke(slotIndex);
        TriggerAllUIEvents(); // 필요한 UI만 갱신
        
        return true;
    }

    /// <summary>
    /// 🔧 현재 선택된 슬롯 인덱스 반환 (지연 모드용)
    /// </summary>
    public int GetSelectedSlotIndex()
    {
        return currentSlotIndex;
    }

    /// <summary>
    /// 🔧 지연 로드가 필요한지 확인
    /// </summary>
    public bool IsLazyLoadRequired()
    {
        // currentSlotIndex는 설정되어 있지만 selectedPlayerData가 해당 슬롯과 다른 경우
        if (currentSlotIndex >= 0 && selectedPlayerData != null)
        {
            var currentSlotData = GetSlotData(currentSlotIndex);
            if (currentSlotData != null)
            {
                // 간단한 검증: 플레이어 이름이 다르면 로드 필요
                return selectedPlayerData.playerName != currentSlotData.playerName;
            }
        }
        
        return currentSlotIndex >= 0 && (selectedPlayerData == null || string.IsNullOrEmpty(selectedPlayerData.playerName));
    }

    #endregion
    
    #region 💰 골드 관리 (AccountDataManager 위임 - V2 계정 공유)
    
    /// <summary>
    /// 골드 추가 (AccountDataManager로 위임)
    /// ⭐ V2: 계정 전체 공유 골드
    /// </summary>
    public void AddGold(int amount)
    {
        if (amount == 0) return;
        
        if (AccountDataManager.Instance != null)
        {
            AccountDataManager.Instance.AddGold(amount);
            AccountDataManager.Instance.Save(); // 골드 변경 시 즉시 저장
            
            // 이벤트 발행 (UI 동기화)
            OnGoldChanged?.Invoke(AccountDataManager.Instance.CurrentGold);
        }
        else
        {
            Debug.LogError("❌ [PlayerDataManager] AccountDataManager.Instance가 null입니다!");
        }
    }
    
    /// <summary>
    /// 골드 소모 (AccountDataManager로 위임)
    /// ⭐ V2: 계정 전체 공유 골드
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (amount <= 0) return false;
        
        if (AccountDataManager.Instance != null)
        {
            bool success = AccountDataManager.Instance.SpendGold(amount);
            
            if (success)
            {
                AccountDataManager.Instance.Save(); // 골드 변경 시 즉시 저장
                
                // 이벤트 발행 (UI 동기화)
                OnGoldChanged?.Invoke(AccountDataManager.Instance.CurrentGold);
            }
            
            return success;
        }
        else
        {
            Debug.LogError("❌ [PlayerDataManager] AccountDataManager.Instance가 null입니다!");
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
        
        // EXP_GAIN_PERCENT 스탯 적용
        var runtimeStats = UnityEngine.Object.FindObjectOfType<PlayerRuntimeStats>();
        if (runtimeStats != null && runtimeStats.FinalExpGainBonus > 0f)
        {
            int bonusExp = Mathf.RoundToInt(amount * runtimeStats.FinalExpGainBonus);
            amount += bonusExp;
            if (showDebugLogs)
                Debug.Log($"⭐ [PlayerDataManager] 경험치 보너스 +{runtimeStats.FinalExpGainBonus:P1} 적용: +{bonusExp} → 총 {amount}");
        }
        
        int oldLevel = selectedPlayerData.currentLevel;
        
        selectedPlayerData.currentExp += amount;
        MarkDirty();
        
        // 레벨업 체크
        bool leveledUp = false;
        while (selectedPlayerData.currentExp >= selectedPlayerData.expToNextLevel)
        {
            selectedPlayerData.currentExp -= selectedPlayerData.expToNextLevel;
            selectedPlayerData.currentLevel++;
            selectedPlayerData.expToNextLevel = CalculateExpToNextLevel(selectedPlayerData.currentLevel);
            OnLevelChanged?.Invoke(selectedPlayerData.currentLevel);
            leveledUp = true;
            
            // Phase 3.5: 레벨업 시 SP 자동 증가 (totalSP = level, 1:1 동기화)
            var slotData = GetCurrentSlotData();
            if (slotData != null)
            {
                // 순서 중요: 레벨을 먼저 업데이트하고 totalSP 설정
                slotData.level = selectedPlayerData.currentLevel; // 슬롯 데이터 레벨 동기화
                slotData.totalSP = slotData.level; // SP는 레벨과 1:1 동기화
                
                // SelectedPlayerData도 동기화 (메모리 캐시)
                if (selectedPlayerData != null)
                {
                    selectedPlayerData.totalSP = slotData.totalSP;
                    selectedPlayerData.usedSP = slotData.usedSP;
                }
                
                if (showDebugLogs)
                    Debug.Log($"💎 [PlayerDataManager] SP 자동 증가! totalSP: {slotData.totalSP}, level: {slotData.level}");
            }
            
            if (showDebugLogs)
                Debug.Log($"🆙 [PlayerDataManager] 레벨업! {oldLevel} → {selectedPlayerData.currentLevel}");
        }
        
        OnExpChanged?.Invoke(selectedPlayerData.currentExp, selectedPlayerData.expToNextLevel);
        
        // 경험치 획득 시 즉시 저장
        if (leveledUp)
        {
            SaveOnMeaningfulEvent("LevelUp");
        }
        else
        {
            SaveOnMeaningfulEvent("ExpGained");
        }
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
        MarkDirty(); // 🔧 인벤토리 추가 시 데이터 변경 표시
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
            MarkDirty(); // 🔧 성공했을 때만 데이터 변경 표시
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
        
        // 🆕 디버그: 장착 시작 전 상태 기록
        Debug.Log($"⚔️ [PlayerDataManager] EquipItem 시작:");
        Debug.Log($"   - 장착할 아이템: {item.equipmentName}");
        Debug.Log($"   - 대상 슬롯: {targetSlot}");
        Debug.Log($"   - 인벤토리 현재 상태 (장착 전):");
        
        for (int i = 0; i < selectedPlayerData.runtimeInventoryItems.Count; i++)
        {
            var invItem = selectedPlayerData.runtimeInventoryItems[i];
            Debug.Log($"     📦 inventory[{i}]: {invItem?.equipmentName ?? "null"}");
        }
        
        // 기존 장착 아이템 확인
        EquipmentData currentItem = selectedPlayerData.RuntimeEquippedItems[targetSlot];
        Debug.Log($"   - 현재 장착된 아이템: {currentItem?.equipmentName ?? "없음"}");
        
        // 🆕 디버그: 인벤토리에서 아이템 위치 찾기
        int itemIndexInInventory = selectedPlayerData.runtimeInventoryItems.IndexOf(item);
        Debug.Log($"   - 장착할 아이템의 인벤토리 인덱스: {itemIndexInInventory}");
        
        try
        {
            // 새 아이템 장착
            selectedPlayerData.RuntimeEquippedItems[targetSlot] = item;
            Debug.Log($"✅ [PlayerDataManager] 새 아이템 장착 완료: {item.equipmentName} → {targetSlot}");
            
            // 기존 아이템이 있었다면 인벤토리에 추가
            if (currentItem != null)
            {
                Debug.Log($"🔄 [PlayerDataManager] 기존 아이템 인벤토리 추가: {currentItem.equipmentName}");
                selectedPlayerData.runtimeInventoryItems.Add(currentItem);
                
                // 🆕 디버그: 추가 후 인벤토리 상태
                Debug.Log($"   - AddToInventory 후 인벤토리 크기: {selectedPlayerData.runtimeInventoryItems.Count}");
                Debug.Log($"   - 추가된 위치: 인덱스 {selectedPlayerData.runtimeInventoryItems.Count - 1}");
            }
            
            // 새 아이템을 인벤토리에서 제거
            if (itemIndexInInventory >= 0)
            {
                Debug.Log($"🗑️ [PlayerDataManager] 새 아이템 인벤토리에서 제거: 인덱스 {itemIndexInInventory}");
                selectedPlayerData.runtimeInventoryItems.RemoveAt(itemIndexInInventory);
                
                // 🆕 디버그: 제거 후 인벤토리 상태
                Debug.Log($"   - RemoveAt({itemIndexInInventory}) 후 인벤토리 크기: {selectedPlayerData.runtimeInventoryItems.Count}");
                Debug.Log($"   - 제거로 인한 인덱스 시프트:");
                
                for (int i = itemIndexInInventory; i < selectedPlayerData.runtimeInventoryItems.Count; i++)
                {
                    var shiftedItem = selectedPlayerData.runtimeInventoryItems[i];
                    Debug.Log($"     🔄 인덱스 {i+1} → {i}: {shiftedItem?.equipmentName ?? "null"}");
                }
            }
            
            // 🆕 디버그: 최종 인벤토리 상태
            Debug.Log($"📊 [PlayerDataManager] 장착 완료 후 최종 인벤토리 상태:");
            for (int i = 0; i < selectedPlayerData.runtimeInventoryItems.Count; i++)
            {
                var finalItem = selectedPlayerData.runtimeInventoryItems[i];
                Debug.Log($"     📦 inventory[{i}]: {finalItem?.equipmentName ?? "null"}");
            }
            
            // 무기인 경우 ActiveWeapon 업데이트
            if (targetSlot == EquipmentSlot.MainWeapon)
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
            
            MarkDirty(); // 🔧 장비 착용 시 데이터 변경 표시
            if (showDebugLogs)
                Debug.Log($"🔧 [EquipItem] MarkDirty() 호출 완료 - isDirty: {isDirty}");
            
            SaveOnMeaningfulEvent("ItemEquipped"); // 🔧 즉시 저장 (슬롯 전환 시 유지)
            if (showDebugLogs)
                Debug.Log($"💾 [EquipItem] SaveOnMeaningfulEvent() 호출 완료");
            
            OnItemEquipped?.Invoke(targetSlot, item);
            OnInventoryChanged?.Invoke();
            
            if (showDebugLogs)
                Debug.Log($"⚔️ [PlayerDataManager] 장비 착용: {item.name} → {targetSlot}");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🔴 [PlayerDataManager] EquipItem 실패: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 아이템 해제
    /// </summary>
    public bool UnequipItem(EquipmentSlot slot)
    {
        Debug.Log($"🔄 [PlayerDataManager] ============= UnequipItem 시작 =============");
        Debug.Log($"   - 해제할 슬롯: {slot}");
        Debug.Log($"   - IsSlotSelected: {IsSlotSelected}");
        
        if (!IsSlotSelected) 
        {
            Debug.LogError($"🔴 [PlayerDataManager] 슬롯이 선택되지 않음");
            return false;
        }
        
        var item = selectedPlayerData.RuntimeEquippedItems[slot];
        Debug.Log($"   - 해제할 아이템: {item?.equipmentName ?? "null"}");
        
        if (item == null) 
        {
            Debug.LogWarning($"⚠️ [PlayerDataManager] {slot} 슬롯이 이미 비어있음");
            return false;
        }
        
        // 🆕 해제 전 인벤토리 상태 확인
        Debug.Log($"📊 [PlayerDataManager] 해제 전 인벤토리 상태:");
        Debug.Log($"   - 현재 크기: {selectedPlayerData.runtimeInventoryItems.Count}");
        Debug.Log($"   - 최대 크기: {selectedPlayerData.MaxInventorySize}");
        Debug.Log($"   - 가득찬 상태: {selectedPlayerData.IsInventoryFull}");
        
        // 장착 해제
        selectedPlayerData.RuntimeEquippedItems[slot] = null;
        Debug.Log($"✅ [PlayerDataManager] {slot} 슬롯 해제 완료");
        
        // 인벤토리에 추가 (🔧 스마트 추가 방식 사용)
        Debug.Log($"📦 [PlayerDataManager] 인벤토리 추가 시도: {item.equipmentName}");
        if (!AddToInventorySmartly(item)) 
        {
            Debug.LogError($"🔴 [PlayerDataManager] 인벤토리 추가 실패! 장착 상태 복원");
            // 실패 시 다시 장착
            selectedPlayerData.RuntimeEquippedItems[slot] = item;
            return false;
        }
        
        // 🆕 해제 후 인벤토리 상태 확인
        Debug.Log($"📊 [PlayerDataManager] 해제 후 인벤토리 상태:");
        Debug.Log($"   - 현재 크기: {selectedPlayerData.runtimeInventoryItems.Count}");
        Debug.Log($"   - 마지막 아이템: {selectedPlayerData.runtimeInventoryItems[selectedPlayerData.runtimeInventoryItems.Count - 1]?.equipmentName ?? "null"}");
        
        selectedPlayerData.SyncDictionaries();
        MarkDirty(); // 🔧 장비 해제 시 데이터 변경 표시
        if (showDebugLogs)
            Debug.Log($"🔧 [UnequipItem] MarkDirty() 호출 완료 - isDirty: {isDirty}");
        
        SaveOnMeaningfulEvent("ItemUnequipped"); // 🔧 즉시 저장 (슬롯 전환 시 유지)
        if (showDebugLogs)
            Debug.Log($"💾 [UnequipItem] SaveOnMeaningfulEvent() 호출 완료");
        
        OnItemUnequipped?.Invoke(slot, item);
        OnInventoryChanged?.Invoke();
        
        if (showDebugLogs)
            Debug.Log($"⚔️ [PlayerDataManager] 장비 해제: {item.equipmentName} ← {slot}");
        
        Debug.Log($"🔄 [PlayerDataManager] ============= UnequipItem 완료 =============");
        return true;
    }
    
    /// <summary>
    /// ⭐ V2: 장비 해제 (ItemInstanceID 기반, 보관창고로 이동)
    /// </summary>
    public bool UnequipItemV2(EquipmentSlot slot, ItemInstanceID instanceId)
    {
        if (!IsSlotSelected || instanceId.IsEmpty)
        {
            Debug.LogError($"❌ [PlayerDataManager] UnequipItemV2 실패: 슬롯 미선택 또는 ID 무효 (ID: {instanceId.Value})");
            return false;
        }
        
        var account = AccountDataManager.Instance;
        if (account == null)
        {
            Debug.LogError("❌ [PlayerDataManager] AccountDataManager가 null입니다!");
            return false;
        }
        
        // 1. 장비 해제 (equippedItems에서 제거)
        if (!EquippedItems.ContainsKey(slot) || EquippedItems[slot] == null)
        {
            Debug.LogWarning($"⚠️ [PlayerDataManager] {slot} 슬롯이 이미 비어있음");
            return false;
        }
        
        var item = EquippedItems[slot];
        
        // 2. ⭐ V2: RuntimeEquippedItems에서 제거 (UI 갱신용)
        selectedPlayerData.RuntimeEquippedItems[slot] = null;
        
        // 3. ⭐ V2: InstanceId 추적에서 제거
        if (selectedPlayerData.RuntimeEquippedInstanceIds.ContainsKey(slot))
        {
            selectedPlayerData.RuntimeEquippedInstanceIds.Remove(slot);
        }
        
        // 4. 계정 공유 창고로 이동 (sharedInventoryIds)
        // ⭐ V2 시스템: AccountDataManager의 TryAddToShared 사용
        if (!account.TryAddToShared(instanceId))
        {
            Debug.LogWarning($"⚠️ [PlayerDataManager] 창고 추가 실패, 우편함으로 이동: {item.equipmentName}");
            account.MoveToMailbox(instanceId);
        }
        
        // 4. 저장 및 이벤트
        MarkDirty();
        SaveOnMeaningfulEvent("ItemUnequippedV2");
        
        OnItemUnequipped?.Invoke(slot, item);
        OnInventoryChanged?.Invoke();
        
        if (showDebugLogs)
            Debug.Log($"✅ [PlayerDataManager] V2 장비 해제: {item.equipmentName} (ID: {instanceId.Value.Substring(0, 8)}...) → 보관창고");
        
        return true;
    }
    
    /// <summary>
    /// ⭐ V2: 귀속 아이템 해제 및 삭제
    /// </summary>
    public bool UnequipAndDeleteBoundItem(EquipmentSlot slot, ItemInstanceID instanceId)
    {
        if (!IsSlotSelected || instanceId.IsEmpty)
        {
            Debug.LogError($"❌ [PlayerDataManager] UnequipAndDeleteBoundItem 실패: 슬롯 미선택 또는 ID 무효");
            return false;
        }
        
        var account = AccountDataManager.Instance;
        if (account == null)
        {
            Debug.LogError("❌ [PlayerDataManager] AccountDataManager가 null입니다!");
            return false;
        }
        
        // 1. 장비 해제
        if (!EquippedItems.ContainsKey(slot) || EquippedItems[slot] == null)
        {
            Debug.LogWarning($"⚠️ [PlayerDataManager] {slot} 슬롯이 이미 비어있음");
            return false;
        }
        
        var item = EquippedItems[slot];
        
        // 2. ⭐ V2: RuntimeEquippedItems에서 제거 (UI 갱신용)
        selectedPlayerData.RuntimeEquippedItems[slot] = null;
        
        // 3. ⭐ V2: InstanceId 추적에서 제거
        if (selectedPlayerData.RuntimeEquippedInstanceIds.ContainsKey(slot))
        {
            selectedPlayerData.RuntimeEquippedInstanceIds.Remove(slot);
        }
        
        // 4. 아이템 삭제 (AccountDataManager에서 제거)
        account.RemoveInstance(instanceId);
        
        // 3. 저장 및 이벤트
        MarkDirty();
        SaveOnMeaningfulEvent("BoundItemDeleted");
        
        OnItemUnequipped?.Invoke(slot, item);
        OnInventoryChanged?.Invoke();
        
        if (showDebugLogs)
            Debug.Log($"🗑️ [PlayerDataManager] 귀속 아이템 해제 및 삭제: {item.equipmentName} (ID: {instanceId.Value.Substring(0, 8)}...)");
        
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
    /// ⭐ V2: 골드는 AccountDataManager에서 가져옴
    /// </summary>
    private void TriggerAllUIEvents()
    {
        if (!IsSlotSelected) return;
        
        // ⭐ V2: 골드는 AccountDataManager에서 가져옴 (계정 공유)
        int accountGold = AccountDataManager.Instance?.CurrentGold ?? 0;
        OnGoldChanged?.Invoke(accountGold);
        Debug.Log($"💰 [TriggerAllUIEvents] 골드 이벤트 발행: {accountGold}");
        
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
    
    #region 🧹 정리 및 호환성 메서드들
    
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
    /// ⭐ ArmorType을 우선 확인하여 정확한 슬롯 결정
    /// </summary>
    private EquipmentSlot DetermineEquipmentSlot(EquipmentData item)
    {
        switch (item.equipmentType)
        {
            case EquipmentType.Weapon:
                return EquipmentSlot.MainWeapon;
            
            case EquipmentType.Armor:
                // ⭐ ArmorType 우선 확인 (정확한 방법)
                switch (item.ArmorType)
                {
                    case ArmorType.Helmet:
                        return EquipmentSlot.Helmet;
                    case ArmorType.Armor:
                        return EquipmentSlot.Armor;
                    case ArmorType.Gloves:
                        return EquipmentSlot.Gloves;
                    case ArmorType.Boots:
                        return EquipmentSlot.Boots;
                    case ArmorType.Belt:
                        return EquipmentSlot.Belt;
                    default:
                        break;
                }
                
                // Fallback: 아이템 이름으로 판단
                string itemName = item.equipmentName.ToLower();
                if (itemName.Contains("helmet") || itemName.Contains("헬멧") || itemName.Contains("투구"))
                    return EquipmentSlot.Helmet;
                else if (itemName.Contains("gloves") || itemName.Contains("장갑"))
                    return EquipmentSlot.Gloves;
                else if (itemName.Contains("boots") || itemName.Contains("신발") || itemName.Contains("부츠"))
                    return EquipmentSlot.Boots;
                else if (itemName.Contains("belt") || itemName.Contains("허리띠") || itemName.Contains("벨트"))
                    return EquipmentSlot.Belt;
                else
                    return EquipmentSlot.Armor; // 기본값: 상의
                
            case EquipmentType.Accessory:
                // 아이템 이름으로 악세서리 타입 판단
                string accessoryName = item.equipmentName.ToLower();
                
                if (accessoryName.Contains("ring") || accessoryName.Contains("반지"))
                {
                    // Ring1이 비어있으면 Ring1, 아니면 Ring2
                    if (selectedPlayerData.RuntimeEquippedItems[EquipmentSlot.Ring1] == null)
                        return EquipmentSlot.Ring1;
                    else
                        return EquipmentSlot.Ring2;
                }
                else if (accessoryName.Contains("necklace") || accessoryName.Contains("목걸이"))
                {
                    return EquipmentSlot.Necklace;
                }
                else
                {
                    // 기본값: Ring1 우선
                    if (selectedPlayerData.RuntimeEquippedItems[EquipmentSlot.Ring1] == null)
                        return EquipmentSlot.Ring1;
                    else if (selectedPlayerData.RuntimeEquippedItems[EquipmentSlot.Ring2] == null)
                        return EquipmentSlot.Ring2;
                    else
                        return EquipmentSlot.Necklace;
                }
                
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
    
    // 🆕 공용 이벤트 발생 메서드들
    /// <summary>
    /// 슬롯 클릭 이벤트 발생 (인게임/로비 공통)
    /// V2: ItemInstanceID 추가
    /// </summary>
    public void TriggerSlotClicked(EquipmentData equipmentData, int slotIndex, ItemInstanceID instanceId = default)
    {
        if (showDebugLogs)
            Debug.Log($"🖱️ [PlayerDataManager] 슬롯 클릭 이벤트 발생: {(equipmentData?.equipmentName ?? "빈 슬롯")} (인덱스: {slotIndex}, ID: {(!instanceId.IsEmpty ? instanceId.Value.Substring(0, 8) + "..." : "없음")})");
        
        OnSlotClicked?.Invoke(equipmentData, slotIndex, instanceId);
    }
    
    /// <summary>
    /// 아이템 상세 정보 요청 이벤트 발생 (로비 전용)
    /// </summary>
    public void TriggerItemDetailRequested(EquipmentData equipmentData)
    {
        if (equipmentData == null) return;
        
        if (showDebugLogs)
            Debug.Log($"📋 [PlayerDataManager] 아이템 상세 정보 요청: {equipmentData.equipmentName}");
        
        OnItemDetailRequested?.Invoke(equipmentData);
    }

    /// <summary>
    /// 인벤토리 변경 이벤트 발생 (외부 호출용)
    /// </summary>
    public void TriggerInventoryChanged()
    {
        if (showDebugLogs)
            Debug.Log("🔄 [PlayerDataManager] 인벤토리 변경 이벤트 발생 (외부 트리거)");
        
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 🆕 특정 슬롯 인덱스에서 아이템 장착 (정확한 인덱스 사용)
    /// </summary>
    public bool EquipItemFromSlot(EquipmentData item, int slotIndex)
    {
        Debug.Log($"⚔️ [PlayerDataManager] ============= EquipItemFromSlot 시작 (V2) =============");
        Debug.Log($"   - 요청 아이템: {item?.equipmentName ?? "null"}");
        Debug.Log($"   - 요청 슬롯 인덱스: {slotIndex}");
        
        if (item == null || !IsSlotSelected) 
        {
            Debug.LogError($"🔴 [PlayerDataManager] 기본 검증 실패");
            return false;
        }
        
        // ⭐ V2: 캐릭터 가방 아이템 가져오기
        var bagItemIds = GetCharacterBagV2();
        Debug.Log($"   - V2 가방 크기: {bagItemIds.Count}");
        Debug.Log($"   - IsSlotSelected: {IsSlotSelected}");
        
        // 슬롯 인덱스 유효성 검사
        if (slotIndex < 0 || slotIndex >= bagItemIds.Count)
        {
            Debug.LogError($"🔴 [PlayerDataManager] 잘못된 슬롯 인덱스: {slotIndex}");
            Debug.LogError($"   유효 범위: 0 ~ {bagItemIds.Count - 1}");
            return false;
        }
        
        // ⭐ V2: ItemInstanceID로 아이템 확인
        ItemInstanceID itemId = bagItemIds[slotIndex];
        var itemInstance = AccountDataManager.Instance?.GetInstance(itemId);
        
        if (itemInstance == null)
        {
            Debug.LogError($"🔴 [PlayerDataManager] 슬롯 {slotIndex}의 ItemInstance를 찾을 수 없음 (ID: {itemId.Value})");
            return false;
        }
        
        // templateName 일치 확인
        if (itemInstance.templateName != item.name)
        {
            Debug.LogError($"🔴 [PlayerDataManager] 템플릿 이름 불일치!");
            Debug.LogError($"   요청: {item.name}");
            Debug.LogError($"   실제: {itemInstance.templateName}");
            return false;
        }
        
        Debug.Log($"✅ [PlayerDataManager] V2 아이템 확인 완료: {itemInstance.templateName} (ID: {itemId.Value.Substring(0, 8)}...)");
        
        // 적절한 장비 슬롯 결정
        EquipmentSlot targetSlot = DetermineEquipmentSlot(item);
        
        // 기존 장착 아이템 확인
        EquipmentData currentItem = selectedPlayerData.RuntimeEquippedItems[targetSlot];
        Debug.Log($"   - 현재 장착된 아이템: {currentItem?.equipmentName ?? "없음"}");
        
        try
        {
            // 새 아이템 장착
            selectedPlayerData.RuntimeEquippedItems[targetSlot] = item;
            Debug.Log($"✅ [PlayerDataManager] 새 아이템 장착 완료: {item.equipmentName} → {targetSlot}");
            
            // ⭐ V2 전용: Legacy runtimeInventoryItems는 사용하지 않음
            // V2에서는 장착 시스템이 별도로 관리되므로 runtimeInventoryItems 수정 불필요
            Debug.Log($"✅ [PlayerDataManager] V2 시스템: 인벤토리 수정 건너뜀 (V2 가방은 유지)");
            
            // 🆕 디버그: 최종 상태 확인
            Debug.Log($"📊 [PlayerDataManager] 장착 완료 후 상태:");
            Debug.Log($"   - V2 가방 크기: {bagItemIds.Count}");
            Debug.Log($"   - 장착됨: {item.equipmentName} → {targetSlot}");
            
            // 무기인 경우 ActiveWeapon 업데이트
            if (targetSlot == EquipmentSlot.MainWeapon)
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
            OnItemEquipped?.Invoke(targetSlot, item); // targetSlot이 첫 번째
            OnInventoryChanged?.Invoke();
            
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🔴 [PlayerDataManager] EquipItemFromSlot 실패: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 🆕 보관창고에서 직접 장비 착용 (V2)
    /// 계정 공유 창고 → 장비 슬롯 직접 장착
    /// </summary>
    public bool EquipItemFromSharedStorage(ItemInstanceID itemId)
    {
        Debug.Log($"⚔️ [PlayerDataManager] ============= EquipItemFromSharedStorage 시작 (V2) =============");
        Debug.Log($"   - 요청 아이템 ID: {itemId.Value.Substring(0, 8)}...");
        
        if (itemId.IsEmpty || !IsSlotSelected)
        {
            Debug.LogError($"🔴 [PlayerDataManager] 기본 검증 실패 (ID 유효: {!itemId.IsEmpty}, 슬롯 선택: {IsSlotSelected})");
            return false;
        }
        
        // 1️⃣ 보관창고에서 아이템 확인
        var accountData = AccountDataManager.Instance.GetAccountData();
        if (!accountData.sharedInventoryIds.Contains(itemId))
        {
            Debug.LogError($"🔴 [PlayerDataManager] 보관창고에 ID {itemId.Value.Substring(0, 8)}... 아이템 없음");
            return false;
        }
        
        // 2️⃣ ItemInstanceData → EquipmentData 변환
        var instanceData = AccountDataManager.Instance.GetInstance(itemId);
        if (instanceData == null)
        {
            Debug.LogError($"🔴 [PlayerDataManager] 아이템 인스턴스 데이터 없음: {itemId.Value}");
            return false;
        }
        
        var equipment = ItemTemplateResolver.Load(instanceData.templateName);
        if (equipment == null)
        {
            Debug.LogError($"🔴 [PlayerDataManager] 템플릿 로드 실패: {instanceData.templateName}");
            return false;
        }
        
        Debug.Log($"✅ [PlayerDataManager] 보관창고 아이템 확인: {equipment.equipmentName} (템플릿: {instanceData.templateName})");
        
        // 3️⃣ 클래스 호환성 체크
        PlayerClass playerClass = selectedPlayerData.selectedPlayerType switch
        {
            PlayerType.Warrior => PlayerClass.Warrior,
            PlayerType.Assasin => PlayerClass.Assasin,
            PlayerType.Wizard => PlayerClass.Wizard,
            _ => PlayerClass.None
        };
        
        if (!equipment.IsCompatibleWith(playerClass))
        {
            Debug.LogError($"🔴 [PlayerDataManager] {equipment.equipmentName}는 현재 클래스({playerClass})와 호환되지 않음");
            return false;
        }
        
        Debug.Log($"✅ [PlayerDataManager] 클래스 호환성 확인: {playerClass}");
        
        // 4️⃣ 장비 슬롯 결정
        EquipmentSlot targetSlot = DetermineEquipmentSlot(equipment);
        Debug.Log($"   - 타겟 슬롯: {targetSlot}");
        
        try
        {
            // 5️⃣ 기존 장비 해제 (있다면 보관창고로 반환)
            EquipmentData oldEquipment = selectedPlayerData.RuntimeEquippedItems[targetSlot];
            if (oldEquipment != null)
            {
                Debug.Log($"   - 기존 장비 해제: {oldEquipment.equipmentName}");
                
                // ⭐ V2: RuntimeEquippedInstanceIds에서 기존 아이템 ID 확인
                ItemInstanceID oldInstanceId = default;
                if (selectedPlayerData.RuntimeEquippedInstanceIds.ContainsKey(targetSlot))
                {
                    oldInstanceId = selectedPlayerData.RuntimeEquippedInstanceIds[targetSlot];
                }
                
                if (!oldInstanceId.IsEmpty)
                {
                    // V2 아이템 → 보관창고로 반환
                    bool addedToShared = AccountDataManager.Instance.TryAddToShared(oldInstanceId);
                    
                    if (addedToShared)
                    {
                        Debug.Log($"✅ [PlayerDataManager] 기존 장비 보관창고 반환: {oldEquipment.equipmentName} (ID: {oldInstanceId.Value.Substring(0, 8)}...)");
                    }
                    else
                    {
                        // 보관창고 가득 찬 → 우편함으로 이동
                        AccountDataManager.Instance.MoveToMailbox(oldInstanceId);
                        Debug.Log($"📬 [PlayerDataManager] 기존 장비 우편함 이동: {oldEquipment.equipmentName} (보관창고 가득 찬)");
                    }
                    
                    // RuntimeEquippedInstanceIds에서 제거
                    selectedPlayerData.RuntimeEquippedInstanceIds.Remove(targetSlot);
                }
                else
                {
                    // Legacy 아이템 (ItemInstanceID 없음) → 경고
                    Debug.LogWarning($"⚠️ [PlayerDataManager] 기존 장비는 Legacy 아이템: {oldEquipment.equipmentName} (V2 시스템 반환 불가, 사라짐)");
                }
            }
            
            // 6️⃣ 새 장비 착용
            selectedPlayerData.RuntimeEquippedItems[targetSlot] = equipment;
            selectedPlayerData.RuntimeEquippedInstanceIds[targetSlot] = itemId; // ⭐ V2: InstanceId 추적
            Debug.Log($"✅ [PlayerDataManager] 새 장비 착용 완료: {equipment.equipmentName} → {targetSlot} (ID: {itemId.Value.Substring(0, 8)}...)");
            Debug.Log($"🔍 [PlayerDataManager] 착용 후 - RuntimeEquippedInstanceIds.Count: {selectedPlayerData.RuntimeEquippedInstanceIds.Count}");
            
            // 7️⃣ 보관창고에서 제거
            bool removed = AccountDataManager.Instance.RemoveFromShared(itemId);
            if (removed)
            {
                Debug.Log($"✅ [PlayerDataManager] 보관창고에서 제거 완료: {itemId.Value.Substring(0, 8)}...");
            }
            else
            {
                Debug.LogWarning($"⚠️ [PlayerDataManager] 보관창고에서 제거 실패 (이미 제거됨?)");
            }
            
            // 8️⃣ 무기인 경우 ActiveWeapon 업데이트
            if (targetSlot == EquipmentSlot.MainWeapon)
            {
                var activeWeapon = FindObjectOfType<ActiveWeapon>();
                if (activeWeapon != null)
                {
                    activeWeapon.EquipWeapon(equipment);
                    Debug.Log($"🔧 [PlayerDataManager] ActiveWeapon에 무기 적용: {equipment.equipmentName}");
                }
            }
            
            // 9️⃣ 스탯 재계산
            var playerRuntimeStats = FindObjectOfType<PlayerRuntimeStats>();
            if (playerRuntimeStats != null)
            {
                playerRuntimeStats.RecalculateAllStats();
                Debug.Log($"🎯 [PlayerDataManager] PlayerRuntimeStats 스탯 재계산 완료");
            }
            
            // 9️⃣ 저장 및 이벤트
            MarkDirty();
            SaveOnMeaningfulEvent("EquipFromSharedStorage");
            AccountDataManager.Instance.Save();
            
            OnItemEquipped?.Invoke(targetSlot, equipment);
            OnInventoryChanged?.Invoke();
            
            Debug.Log($"🎉 [PlayerDataManager] EquipItemFromSharedStorage 완료!");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"🔴 [PlayerDataManager] EquipItemFromSharedStorage 실패: {ex.Message}");
            Debug.LogError($"   스택 트레이스: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// 🆕 빈 슬롯 우선 인벤토리 추가 (null 슬롯을 먼저 활용)
    /// </summary>
    public bool AddToInventorySmartly(EquipmentData item)
    {
        if (!IsSlotSelected || item == null) return false;
        
        Debug.Log($"🧠 [PlayerDataManager] 스마트 인벤토리 추가: {item.equipmentName}");
        
        // 1. 먼저 빈 슬롯(null) 찾기
        for (int i = 0; i < selectedPlayerData.runtimeInventoryItems.Count; i++)
        {
            if (selectedPlayerData.runtimeInventoryItems[i] == null)
            {
                selectedPlayerData.runtimeInventoryItems[i] = item;
                Debug.Log($"✅ [PlayerDataManager] 빈 슬롯[{i}]에 배치: {item.equipmentName}");
                
                SaveCurrentSlot();
                OnItemAddedToInventory?.Invoke(item);
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        
        // 2. 빈 슬롯이 없으면 기존 방식(맨 뒤에 추가)
        if (selectedPlayerData.CurrentInventorySize < selectedPlayerData.MaxInventorySize)
        {
            selectedPlayerData.runtimeInventoryItems.Add(item);
            Debug.Log($"✅ [PlayerDataManager] 새 슬롯[{selectedPlayerData.runtimeInventoryItems.Count - 1}]에 추가: {item.equipmentName}");
            
            SaveCurrentSlot();
            OnItemAddedToInventory?.Invoke(item);
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        // 3. 진짜 가득 참
        Debug.LogWarning($"⚠️ [PlayerDataManager] 인벤토리가 가득 참! 추가 불가: {item.equipmentName}");
        return false;
    }

    #region 🎯 스테이지 진행도 관리
    
    /// <summary>
    /// 현재 슬롯의 스테이지 진행도 목록 가져오기
    /// </summary>
    public List<StageSystem.StageProgress> GetStageProgresses()
    {
        if (!IsSlotSelected) 
        {
            if (showDebugLogs)
                Debug.LogWarning("[PlayerDataManager] 슬롯이 선택되지 않음");
            return new List<StageSystem.StageProgress>();
        }
        
        var currentSlot = GetSlotData(currentSlotIndex);
        if (currentSlot == null) 
        {
            if (showDebugLogs)
                Debug.LogWarning($"[PlayerDataManager] 슬롯 {currentSlotIndex} 데이터가 null");
            return new List<StageSystem.StageProgress>();
        }
        
        return currentSlot.stageProgresses ?? new List<StageSystem.StageProgress>();
    }
    
    /// <summary>
    /// 스테이지 진행도 업데이트
    /// </summary>
    public void UpdateStageProgresses(List<StageSystem.StageProgress> progresses)
    {
        if (!IsSlotSelected) return;
        
        var currentSlot = GetSlotData(currentSlotIndex);
        if (currentSlot == null) return;
        
        currentSlot.stageProgresses = progresses;
        
        // 🆕 SelectedPlayerData에도 반영 (캐시 동기화)
        if (selectedPlayerData != null)
        {
            selectedPlayerData.stageProgresses = new List<StageSystem.StageProgress>(progresses);
        }
        
        // 🔧 스테이지 진행도 변경 표시
        MarkDirty();
        
        if (showDebugLogs)
            Debug.Log($"💾 [PlayerDataManager] 스테이지 진행도 업데이트: {progresses.Count}개");
    }
    
    /// <summary>
    /// 특정 스테이지 진행도 가져오기
    /// </summary>
    public StageSystem.StageProgress GetStageProgress(string stageId)
    {
        var progresses = GetStageProgresses();
        return progresses.Find(p => p.stageId == stageId);
    }
    
    /// <summary>
    /// 스테이지 해금 상태 확인
    /// </summary>
    public bool IsStageUnlocked(string stageId)
    {
        var progress = GetStageProgress(stageId);
        return progress != null && progress.isUnlocked;
    }
    
    /// <summary>
    /// 스테이지 완료 상태 확인
    /// </summary>
    public bool IsStageCompleted(string stageId)
    {
        var progress = GetStageProgress(stageId);
        return progress != null && progress.isCompleted;
    }
    
    #endregion

    /// <summary>
    /// 🆕 현재 선택된 슬롯 인덱스 반환
    /// </summary>
    public int GetCurrentSlotIndex()
    {
        return currentSlotIndex;
    }
    
    #region 🎯 로비 자동 선택 지원 메서드들
    
    /// <summary>
    /// 유효한 캐릭터가 1명 이상 존재하는지 확인
    /// </summary>
    public bool HasAnyCharacter()
    {
        for (int i = 0; i < maxSlots; i++)
        {
            if (IsSlotValid(i)) return true;
        }
        return false;
    }
    
    /// <summary>
    /// 마지막 선택된 슬롯 인덱스 반환
    /// </summary>
    public int GetLastSelectedSlotIndex()
    {
        LoadLastSelectedSlotIndex();
        return lastSelectedSlotIndex;
    }
    
    /// <summary>
    /// 슬롯에 유효한 캐릭터가 존재하는지 확인
    /// </summary>
    public bool IsSlotValid(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return false;
        
        var slotData = GetSlotData(slotIndex);
        return slotData != null && slotData.isSlotUsed;
    }
    
    /// <summary>
    /// ✅ Phase 1: 현재 선택된 슬롯 데이터 가져오기 (편의 메서드)
    /// </summary>
    /// <summary>
    /// ⚠️ DEPRECATED: 런타임 데이터 접근 시 selectedPlayerData 사용 권장
    /// 이 메서드는 playerSlots[] 배열을 직접 반환하므로 데이터 동기화 문제 발생 가능
    /// </summary>
    [System.Obsolete("런타임 데이터 접근은 selectedPlayerData를 사용하세요. 이 메서드는 UI 표시용으로만 사용됩니다.", false)]
    public PlayerSlotData GetCurrentSlotData()
    {
        if (currentSlotIndex < 0)
        {
            if (showDebugLogs)
                Debug.LogWarning("[PlayerDataManager] 선택된 슬롯이 없습니다.");
            return null;
        }
        
        return GetSlotData(currentSlotIndex);
    }
    
    /// <summary>
    /// 마지막 선택 슬롯 저장
    /// </summary>
    private void SaveLastSelectedSlotIndex()
    {
        string filePath = Path.Combine(SaveDirectoryPath, "LastSelectedSlot.json");
        try
        {
            var data = new LastSelectedSlotData { lastSelectedSlotIndex = this.lastSelectedSlotIndex };
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);
            
            // 🔧 로그 단순화: 디버그 모드에서만 표시
            if (showDebugLogs)
                Debug.Log($"[PlayerDataManager] Last slot saved: {lastSelectedSlotIndex}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerDataManager] 마지막 선택 슬롯 저장 실패: {e.Message}");
        }
    }
    
    /// <summary>
    /// 마지막 선택 슬롯 로드
    /// </summary>
    private void LoadLastSelectedSlotIndex()
    {
        string filePath = Path.Combine(SaveDirectoryPath, "LastSelectedSlot.json");
        if (!File.Exists(filePath)) return;
        
        try
        {
            string json = File.ReadAllText(filePath);
            var data = JsonUtility.FromJson<LastSelectedSlotData>(json);
            lastSelectedSlotIndex = data.lastSelectedSlotIndex;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PlayerDataManager] 마지막 선택 슬롯 로드 실패: {e.Message}");
            lastSelectedSlotIndex = 0; // 기본값
        }
    }
    
    #endregion

    #region 🆕 Step 4-5: 이벤트 알림 시스템

    /// <summary>
    /// 🆕 데이터 변경 시 이벤트 발행
    /// </summary>
    private void NotifyDataChanged()
    {
        OnSelectedPlayerDataChanged?.Invoke(selectedPlayerData);
        if (showDebugLogs)
            Debug.Log($"📢 [PlayerDataManager] 데이터 변경 알림: {selectedPlayerData?.selectedPlayerType} Lv.{selectedPlayerData?.CurrentLevel}");
    }

    private void NotifyGoldChanged(int newGold)
    {
        OnPlayerGoldChanged?.Invoke(newGold);
        OnGoldChanged?.Invoke(newGold); // 기존 이벤트도 유지
        NotifyDataChanged();
    }
    
    /// <summary>
    /// 🆕 인벤토리 변경 알림 (공유 창고 포함)
    /// </summary>
    public void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
        
        if (showDebugLogs)
            Debug.Log($"🔄 [PlayerDataManager] 인벤토리 변경 알림 발생");
    }

    #endregion

    #region 🆕 Phase 3: V2 인벤토리/장착 API (ItemInstanceID 기반)
    
    /// <summary>
    /// V2 아이템 장착 (ItemInstanceID 기반, 원자성 보장)
    /// </summary>
    /// <param name="instanceId">장착할 아이템 인스턴스 ID</param>
    /// <param name="targetSlot">장착할 슬롯</param>
    /// <param name="allowMailboxOnFull">인벤토리 가득 찰 때 우편함 처리 여부</param>
    /// <returns>성공 여부</returns>
    public bool EquipV2(ItemInstanceID instanceId, EquipmentSlot targetSlot, bool allowMailboxOnFull = true)
    {
        if (!IsSlotSelected || instanceId.IsEmpty)
        {
            Debug.LogError("[EquipV2] 슬롯 미선택 또는 잘못된 인스턴스 ID");
            return false;
        }
        
        if (!AccountDataManager.IsInitialized())
        {
            Debug.LogError("[EquipV2] AccountDataManager가 초기화되지 않음");
            return false;
        }
        
        var account = AccountDataManager.Instance;
        var slotData = GetSlotData(currentSlotIndex);
        
        if (slotData == null)
        {
            Debug.LogError($"[EquipV2] 슬롯 {currentSlotIndex} 데이터 없음");
            return false;
        }
        
        // 1. 가방에 아이템이 있는지 확인
        if (!slotData.characterBagInstanceIds.Contains(instanceId))
        {
            Debug.LogError($"[EquipV2] 아이템 {instanceId}가 가방에 없음");
            return false;
        }
        
        // 1.5. 귀속 체크 (Phase 4.4 추가)
        var bindInfo = account.GetBindInfo(instanceId);
        if (bindInfo.isBound && bindInfo.characterSlotIndex != currentSlotIndex)
        {
            // ⭐ Phase 4: 다른 캐릭터 귀속 아이템 경고 표시
            if (BindWarningManager.Instance != null)
            {
                var boundSlotData = GetSlotData(bindInfo.characterSlotIndex);
                string boundCharacterName = boundSlotData?.playerName ?? $"슬롯 {bindInfo.characterSlotIndex}";
                BindWarningManager.Instance.ShowAlreadyBoundWarning(instanceId, bindInfo.characterSlotIndex, boundCharacterName);
            }
            
            Debug.LogError($"[EquipV2] 아이템 {instanceId}는 캐릭터 {bindInfo.characterSlotIndex}에 귀속되어 장착할 수 없습니다.");
            return false;
        }
        
        // 2. 현재 장착된 아이템 확인
        var currentEquipped = slotData.equippedRecords.Find(r => r.slot == targetSlot);
        ItemInstanceID? oldInstanceId = currentEquipped?.instanceId;
        
        // 3. 인벤토리 공간 확인 (기존 아이템이 있고, 가방이 가득 찬 경우)
        bool needsSpace = oldInstanceId.HasValue && !oldInstanceId.Value.IsEmpty;
        bool bagFull = slotData.characterBagInstanceIds.Count >= selectedPlayerData.MaxInventorySize;
        
        if (needsSpace && bagFull)
        {
            if (allowMailboxOnFull)
            {
                // 우편함으로 이동
                account.MoveToMailbox(oldInstanceId.Value);
                Debug.Log($"[EquipV2] 가방 가득 참 → 기존 아이템 우편함 이동: {oldInstanceId.Value}");
            }
            else
            {
                Debug.LogError("[EquipV2] 인벤토리 가득 참 (우편함 처리 거부)");
                return false;
            }
        }
        
        // 4. 장착 실행 (원자적)
        try
        {
            // 가방에서 제거
            slotData.characterBagInstanceIds.Remove(instanceId);
            
            // 기존 아이템을 가방에 추가 (우편함 처리하지 않은 경우만)
            if (needsSpace && !bagFull)
            {
                slotData.characterBagInstanceIds.Add(oldInstanceId.Value);
            }
            
            // 장착 레코드 업데이트
            if (currentEquipped != null)
            {
                currentEquipped.instanceId = instanceId;
            }
            else
            {
                slotData.equippedRecords.Add(new EquippedRecord 
                { 
                    slot = targetSlot, 
                    instanceId = instanceId 
                });
            }
            
            // ⭐ Phase 4.5: 등급별 귀속 설정 (SS/EX/TR만 귀속)
            var instance = account.GetInstance(instanceId);
            var equipment = ItemTemplateResolver.Load(instance.templateName);
            
            if (equipment != null && equipment.itemGrade >= ItemGrade.SS)
            {
                // SS, EX, TR 등급만 귀속
                account.SetBind(instanceId, currentSlotIndex);
                Debug.Log($"⚠️ [EquipV2] {equipment.itemGrade} 등급 아이템 귀속: {equipment.equipmentName} → 슬롯 {currentSlotIndex}");
            }
            else if (equipment != null)
            {
                // D~S 등급은 귀속 없음
                Debug.Log($"✅ [EquipV2] {equipment.itemGrade} 등급 아이템 귀속 없음: {equipment.equipmentName}");
            }
            
            // 저장
            SaveSlotData(slotData);
            account.Save();
            
            // ⭐ 중요: selectedPlayerData 동기화 (RuntimeEquippedItems 업데이트)
            if (selectedPlayerData != null)
            {
                Debug.Log($"🔄 [EquipV2] selectedPlayerData 동기화 중...");
                selectedPlayerData.LoadFromSlotData(slotData);
                Debug.Log($"✅ [EquipV2] selectedPlayerData 동기화 완료");
            }
            
            MarkDirty();
            
            Debug.Log($"✅ [EquipV2] 장착 성공: {instanceId} → {targetSlot}");
            OnInventoryChanged?.Invoke();
            
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[EquipV2] 장착 실패: {ex.Message}");
            // 롤백 필요 시 여기서 처리
            return false;
        }
    }
    
    /// <summary>
    /// V2 아이템 해제 (ItemInstanceID 기반, 원자성 보장)
    /// </summary>
    /// <param name="targetSlot">해제할 슬롯</param>
    /// <param name="allowMailboxOnFull">인벤토리 가득 찰 때 우편함 처리 여부</param>
    /// <returns>성공 여부</returns>
    public bool UnequipV2(EquipmentSlot targetSlot, bool allowMailboxOnFull = true)
    {
        if (!IsSlotSelected)
        {
            Debug.LogError("[UnequipV2] 슬롯 미선택");
            return false;
        }
        
        if (!AccountDataManager.IsInitialized())
        {
            Debug.LogError("[UnequipV2] AccountDataManager가 초기화되지 않음");
            return false;
        }
        
        var account = AccountDataManager.Instance;
        var slotData = GetSlotData(currentSlotIndex);
        
        if (slotData == null)
        {
            Debug.LogError($"[UnequipV2] 슬롯 {currentSlotIndex} 데이터 없음");
            return false;
        }
        
        // 1. 장착된 아이템 확인
        var equippedRecord = slotData.equippedRecords.Find(r => r.slot == targetSlot);
        
        if (equippedRecord == null || equippedRecord.instanceId.IsEmpty)
        {
            Debug.LogWarning($"[UnequipV2] {targetSlot} 슬롯이 비어있음");
            return false;
        }
        
        ItemInstanceID instanceId = equippedRecord.instanceId;
        
        // 2. 인벤토리 공간 확인
        bool bagFull = slotData.characterBagInstanceIds.Count >= selectedPlayerData.MaxInventorySize;
        
        if (bagFull)
        {
            if (allowMailboxOnFull)
            {
                // 우편함으로 이동
                account.MoveToMailbox(instanceId);
                Debug.Log($"[UnequipV2] 가방 가득 참 → 우편함 이동: {instanceId}");
            }
            else
            {
                Debug.LogError("[UnequipV2] 인벤토리 가득 참 (우편함 처리 거부)");
                return false;
            }
        }
        else
        {
            // 가방에 추가
            slotData.characterBagInstanceIds.Add(instanceId);
        }
        
        // 3. 장착 해제
        try
        {
            slotData.equippedRecords.Remove(equippedRecord);
            
            // 저장
            SaveSlotData(slotData);
            account.Save();
            
            MarkDirty();
            
            Debug.Log($"✅ [UnequipV2] 해제 성공: {instanceId} ← {targetSlot}");
            OnInventoryChanged?.Invoke();
            
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UnequipV2] 해제 실패: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 가방 → 계정 창고로 아이템 이동
    /// </summary>
    public bool MoveToAccountStorage(ItemInstanceID instanceId)
    {
        if (!IsSlotSelected || instanceId.IsEmpty)
        {
            Debug.LogError("[MoveToAccountStorage] 슬롯 미선택 또는 잘못된 인스턴스 ID");
            return false;
        }
        
        if (!AccountDataManager.IsInitialized())
        {
            Debug.LogError("[MoveToAccountStorage] AccountDataManager가 초기화되지 않음");
            return false;
        }
        
        var account = AccountDataManager.Instance;
        var slotData = GetSlotData(currentSlotIndex);
        
        if (slotData == null)
        {
            Debug.LogError($"[MoveToAccountStorage] 슬롯 {currentSlotIndex} 데이터 없음");
            return false;
        }
        
        // 1. 가방에 아이템이 있는지 확인
        if (!slotData.characterBagInstanceIds.Contains(instanceId))
        {
            Debug.LogError($"[MoveToAccountStorage] 아이템 {instanceId}가 가방에 없음");
            return false;
        }
        
        // 2. 귀속 확인 (현재 캐릭터 포함)
        var bindInfo = account.GetBindInfo(instanceId);
        if (bindInfo.isBound)
        {
            // 귀속된 아이템은 계정 창고로 이동 불가
            if (BindWarningManager.Instance != null)
            {
                BindWarningManager.Instance.ShowCannotMoveToStorageWarning(instanceId, slotData.playerName);
            }
            
            Debug.LogError($"[MoveToAccountStorage] 아이템 {instanceId}는 {slotData.playerName}에게 귀속됨 (창고 이동 불가)");
            return false;
        }
        
        // 3. 계정 창고에 공간 확인
        if (!account.TryAddToShared(instanceId))
        {
            Debug.LogError($"[MoveToAccountStorage] 계정 창고 가득 참");
            return false;
        }
        
        // 4. 가방에서 제거
        slotData.characterBagInstanceIds.Remove(instanceId);
        
        // 5. 저장
        SaveSlotData(slotData);
        account.Save();
        
        MarkDirty();
        
        Debug.Log($"✅ [MoveToAccountStorage] 창고 이동 성공: {instanceId}");
        OnInventoryChanged?.Invoke();
        
        return true;
    }
    
    // ❌ 삭제됨: MoveFromAccountStorage() - 보관창고 → 가방 기능은 사용하지 않음
    // V2 시스템에서는 보관창고에서 직접 착용하는 방식 사용 (EquipItemFromSharedStorage)
    
    /// <summary>
    /// 우편함 → 가방으로 아이템 이동
    /// </summary>
    public bool ClaimFromMailbox(ItemInstanceID instanceId)
    {
        if (!IsSlotSelected || instanceId.IsEmpty)
        {
            Debug.LogError("[ClaimFromMailbox] 슬롯 미선택 또는 잘못된 인스턴스 ID");
            return false;
        }
        
        if (!AccountDataManager.IsInitialized())
        {
            Debug.LogError("[ClaimFromMailbox] AccountDataManager가 초기화되지 않음");
            return false;
        }
        
        var account = AccountDataManager.Instance;
        var slotData = GetSlotData(currentSlotIndex);
        
        if (slotData == null)
        {
            Debug.LogError($"[ClaimFromMailbox] 슬롯 {currentSlotIndex} 데이터 없음");
            return false;
        }
        
        // 1. 우편함에 아이템이 있는지 확인
        var accountData = account.GetAccountData();
        if (accountData == null || !accountData.mailboxIds.Contains(instanceId))
        {
            Debug.LogError($"[ClaimFromMailbox] 아이템 {instanceId}가 우편함에 없음");
            return false;
        }
        
        // 2. 가방 공간 확인
        if (slotData.characterBagInstanceIds.Count >= selectedPlayerData.MaxInventorySize)
        {
            Debug.LogError("[ClaimFromMailbox] 가방 가득 참");
            return false;
        }
        
        // 3. 우편함에서 제거
        account.RemoveFromMailbox(instanceId);
        
        // 4. 가방에 추가
        slotData.characterBagInstanceIds.Add(instanceId);
        
        // 5. 저장
        SaveSlotData(slotData);
        account.Save();
        
        MarkDirty();
        
        Debug.Log($"✅ [ClaimFromMailbox] 우편함 수령 성공: {instanceId}");
        OnInventoryChanged?.Invoke();
        
        return true;
    }
    
    /// <summary>
    /// 현재 캐릭터의 V2 가방 아이템 목록 가져오기
    /// </summary>
    public List<ItemInstanceID> GetCharacterBagV2()
    {
        if (!IsSlotSelected)
        {
            Debug.Log("[GetCharacterBagV2] 슬롯 미선택 - 빈 리스트 반환");
            return new List<ItemInstanceID>();
        }
        
        var slotData = GetSlotData(currentSlotIndex);
        var bagItems = slotData?.characterBagInstanceIds ?? new List<ItemInstanceID>();
        
        Debug.Log($"🔍 [GetCharacterBagV2] 슬롯 {currentSlotIndex} 가방 아이템: {bagItems.Count}개");
        
        return bagItems;
    }
    
    /// <summary>
    /// 현재 캐릭터의 V2 장착 아이템 목록 가져오기
    /// </summary>
    public List<EquippedRecord> GetEquippedRecordsV2()
    {
        if (!IsSlotSelected)
            return new List<EquippedRecord>();
        
        var slotData = GetSlotData(currentSlotIndex);
        return slotData?.equippedRecords ?? new List<EquippedRecord>();
    }
    
    /// <summary>
    /// V2 아이템 획득 (신규 아이템을 가방에 추가)
    /// - AccountDataManager에 인스턴스 등록
    /// - characterBagInstanceIds에 추가
    /// - 가방 가득 차면 우편함 처리
    /// </summary>
    /// <param name="templateName">아이템 템플릿 이름 (예: "Sword_A_Equipment")</param>
    /// <param name="enhancementLevel">강화 레벨 (기본값: 0)</param>
    /// <param name="allowMailboxOnFull">가방 가득 찰 때 우편함 처리 여부 (기본값: true)</param>
    /// <returns>생성된 아이템 인스턴스 ID (실패 시 invalid ID)</returns>
    public ItemInstanceID AddItemV2(string templateName, int enhancementLevel = 0, bool allowMailboxOnFull = true)
    {
        // ⭐ 디버깅: AddItemV2 호출 추적
        Debug.Log($"═══════════════════════════════════════════════════════");
        Debug.Log($"🔍 [DEBUG] AddItemV2() 호출됨!");
        Debug.Log($"  templateName: {templateName}");
        Debug.Log($"  현재 시간: {Time.time}");
        Debug.Log($"  Stack Trace:");
        Debug.Log(System.Environment.StackTrace);
        Debug.Log($"═══════════════════════════════════════════════════════");
        
        if (!IsSlotSelected)
        {
            Debug.LogError("[AddItemV2] 슬롯 미선택");
            return default(ItemInstanceID);
        }
        
        if (string.IsNullOrEmpty(templateName))
        {
            Debug.LogError("[AddItemV2] 템플릿 이름이 비어있음");
            return default(ItemInstanceID);
        }
        
        if (!AccountDataManager.IsInitialized())
        {
            Debug.LogError("[AddItemV2] AccountDataManager가 초기화되지 않음");
            return default(ItemInstanceID);
        }
        
        var account = AccountDataManager.Instance;
        var slotData = GetSlotData(currentSlotIndex);
        
        if (slotData == null)
        {
            Debug.LogError($"[AddItemV2] 슬롯 {currentSlotIndex} 데이터 없음");
            return default(ItemInstanceID);
        }
        
        // 1. AccountDataManager에 신규 인스턴스 등록
        ItemInstanceID newId = account.RegisterNewInstance(templateName);
        
        // 1.5. 강화 레벨 설정 (0이 아닌 경우)
        if (enhancementLevel > 0)
        {
            var instanceData = account.GetInstance(newId);
            if (instanceData != null)
            {
                instanceData.enhancementLevel = enhancementLevel;
            }
        }
        
        // ⭐ Stage 3: 동적 스탯 생성 및 적용 (드롭 시스템용)
        EquipmentData equipData = ItemTemplateResolver.Load(templateName);
        if (equipData != null)
        {
            // 장비 아이템만 동적 스탯 생성
            EquipmentInstance dynamicInstance = DynamicEquipmentGenerator.Generate(equipData, equipData.itemGrade);
            
            if (dynamicInstance != null)
            {
                ItemInstanceData instanceData = account.GetInstance(newId);
                if (instanceData != null)
                {
                    EquipmentInstanceConverter.ApplyDynamicStats(instanceData, dynamicInstance);
                    Debug.Log($"🎲 [PlayerDataManager.AddItemV2] 동적 스탯 생성 완료: 주옵션={instanceData.finalMainStatValue}, 부옵션={instanceData.randomSubStats.Count}개");
                }
            }
        }
        
        // 2. 가방 공간 확인
        Debug.Log($"🔍 [AddItemV2] 가방 상태 확인: 현재 {slotData.characterBagInstanceIds.Count}개 / 최대 {selectedPlayerData.MaxInventorySize}개");
        
        bool bagFull = slotData.characterBagInstanceIds.Count >= selectedPlayerData.MaxInventorySize;
        
        if (bagFull)
        {
            if (allowMailboxOnFull)
            {
                // 우편함으로 이동
                account.MoveToMailbox(newId);
                Debug.Log($"📬 [AddItemV2] 가방 가득 참 → 우편함 이동: {templateName} (ID: {newId.Value})");
            }
            else
            {
                // 아이템 삭제 (등록 취소)
                // ✅ RemoveInstance() 사용 (캐시 정리 + 모든 참조 제거)
                account.RemoveInstance(newId);
                
                Debug.LogError($"❌ [AddItemV2] 가방 가득 차서 획득 실패: {templateName}");
                return default(ItemInstanceID);
            }
        }
        else
        {
            // 가방에 추가
            Debug.Log($"📦 [AddItemV2] 가방에 추가 시작: {newId.Value}");
            slotData.characterBagInstanceIds.Add(newId);
            Debug.Log($"📦 [AddItemV2] 가방에 추가 완료: 현재 {slotData.characterBagInstanceIds.Count}개");
        }
        
        // 3. 저장 및 메모리 동기화
        try
        {
            Debug.Log($"💾 [AddItemV2] SaveSlotData() 호출 전: 가방 {slotData.characterBagInstanceIds.Count}개");
            bool saved = SaveSlotData(slotData);
            Debug.Log($"💾 [AddItemV2] SaveSlotData() 결과: {(saved ? "성공" : "실패")}");
            
            if (!saved)
            {
                Debug.LogError($"❌ [AddItemV2] SaveSlotData 실패! 아이템 롤백: {templateName}");
                // 가방에서 제거 (롤백)
                slotData.characterBagInstanceIds.Remove(newId);
                return default(ItemInstanceID);
            }
            
            // 3.5. ⭐ 중요: selectedPlayerData도 업데이트 (SaveCurrentSlot 덮어쓰기 방지!)
            if (selectedPlayerData != null)
            {
                Debug.Log($"🔄 [AddItemV2] selectedPlayerData 동기화 중...");
                selectedPlayerData.LoadFromSlotData(slotData);
                Debug.Log($"✅ [AddItemV2] selectedPlayerData 동기화 완료");
            }
            
            // 저장 후 검증
            var verifySlot = GetSlotData(currentSlotIndex);
            Debug.Log($"🔍 [AddItemV2] 저장 후 검증: 가방 {verifySlot?.characterBagInstanceIds.Count ?? 0}개");
            
            account.Save();
            
            MarkDirty();
            
            Debug.Log($"✅ [AddItemV2] 아이템 획득 성공: {templateName} (ID: {newId.Value}, 강화: +{enhancementLevel})");
            
            // ⭐ 이벤트 발생 (가방에 추가된 경우만 OnCharacterBagChanged)
            if (!bagFull)
            {
                OnCharacterBagChanged?.Invoke(); // ⭐ 인게임 가방 UI 갱신
            }
            OnInventoryChanged?.Invoke(); // ⭐ 기존 호환성 유지
            
            return newId;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"💥 [AddItemV2] 예외 발생! {ex.Message}\n{ex.StackTrace}");
            
            // 가방에서 제거 (롤백)
            slotData.characterBagInstanceIds.Remove(newId);
            
            return default(ItemInstanceID);
        }
    }
    
    #endregion
    
    #region 🆕 Phase 4: 귀속 경고 시스템
    
    /// <summary>
    /// V2 아이템 장착 (귀속 경고 포함)
    /// - 귀속되지 않은 아이템은 경고 팝업 표시
    /// - 사용자 확인 후 장착 진행
    /// </summary>
    /// <param name="instanceId">장착할 아이템 인스턴스 ID</param>
    /// <param name="targetSlot">장착할 슬롯</param>
    /// <param name="onComplete">완료 콜백 (성공 여부)</param>
    /// <param name="allowMailboxOnFull">인벤토리 가득 찰 때 우편함 처리 여부</param>
    public void EquipV2WithWarning(ItemInstanceID instanceId, EquipmentSlot targetSlot, 
                                    System.Action<bool> onComplete = null, bool allowMailboxOnFull = true)
    {
        if (!IsSlotSelected || instanceId.IsEmpty)
        {
            Debug.LogError("[EquipV2WithWarning] 슬롯 미선택 또는 잘못된 인스턴스 ID");
            onComplete?.Invoke(false);
            return;
        }
        
        if (!AccountDataManager.IsInitialized())
        {
            Debug.LogError("[EquipV2WithWarning] AccountDataManager가 초기화되지 않음");
            onComplete?.Invoke(false);
            return;
        }
        
        var account = AccountDataManager.Instance;
        var bindInfo = account.GetBindInfo(instanceId);
        
        // 1. 다른 캐릭터에 귀속된 아이템 체크
        if (bindInfo.isBound && bindInfo.characterSlotIndex != currentSlotIndex)
        {
            var slotData = GetSlotData(bindInfo.characterSlotIndex);
            string boundCharacterName = slotData?.playerName ?? $"슬롯 {bindInfo.characterSlotIndex}";
            
            BindWarningManager.Instance.ShowAlreadyBoundWarning(instanceId, bindInfo.characterSlotIndex, boundCharacterName);
            Debug.LogWarning($"[EquipV2WithWarning] 다른 캐릭터 귀속 아이템 장착 시도: {instanceId} → {boundCharacterName}");
            
            onComplete?.Invoke(false);
            return;
        }
        
        // 2. 아직 귀속되지 않은 아이템 → 경고 표시
        if (!bindInfo.isBound && BindWarningManager.Instance.ShouldShowWarning(instanceId))
        {
            var itemData = account.GetInstance(instanceId);
            var currentSlot = GetSlotData(currentSlotIndex);
            
            if (itemData == null || currentSlot == null)
            {
                Debug.LogError($"[EquipV2WithWarning] 아이템 데이터 없음: {instanceId}");
                onComplete?.Invoke(false);
                return;
            }
            
            var warningData = new Systems.BindWarningData(
                instanceId,
                itemData.templateName,
                itemData.enhancementLevel,
                targetSlot,
                currentSlotIndex,
                currentSlot.playerName
            );
            
            Debug.Log($"[EquipV2WithWarning] 귀속 경고 표시: {itemData.templateName}+{itemData.enhancementLevel}");
            
            BindWarningManager.Instance.ShowWarningAndWaitForResponse(warningData, (confirmed) =>
            {
                if (confirmed)
                {
                    // 사용자 확인 → 장착 진행
                    bool success = EquipV2(instanceId, targetSlot, allowMailboxOnFull);
                    Debug.Log($"[EquipV2WithWarning] 사용자 확인 후 장착: {(success ? "성공" : "실패")}");
                    onComplete?.Invoke(success);
                }
                else
                {
                    // 사용자 취소
                    Debug.Log($"[EquipV2WithWarning] 사용자 취소");
                    onComplete?.Invoke(false);
                }
            });
        }
        else
        {
            // 3. 이미 귀속된 아이템 → 즉시 장착
            bool success = EquipV2(instanceId, targetSlot, allowMailboxOnFull);
            Debug.Log($"[EquipV2WithWarning] 이미 귀속된 아이템 즉시 장착: {(success ? "성공" : "실패")}");
            onComplete?.Invoke(success);
        }
    }
    
    // ========================================
    // Phase 5: 분해 시스템
    // ========================================
    
    /// <summary>
    /// 아이템 분해 (경고 팝업 포함)
    /// </summary>
    /// <param name="instanceId">분해할 아이템 ID</param>
    /// <param name="onComplete">완료 콜백 (성공 여부, 획득 재료)</param>
    public void DismantleV2WithWarning(ItemInstanceID instanceId, 
        System.Action<bool, System.Collections.Generic.Dictionary<MaterialType, int>> onComplete)
    {
        if (instanceId.IsEmpty)
        {
            Debug.LogError("[DismantleV2WithWarning] 잘못된 인스턴스 ID");
            onComplete?.Invoke(false, null);
            return;
        }
        
        if (!AccountDataManager.IsInitialized())
        {
            Debug.LogError("[DismantleV2WithWarning] AccountDataManager가 초기화되지 않음");
            onComplete?.Invoke(false, null);
            return;
        }
        
        // 경고 필요 여부 확인 + 표시
        Managers.DismantleWarningManager.Instance.ShowWarningIfNeeded(instanceId, (confirmed) =>
        {
            if (confirmed)
            {
                // 사용자 확인 → 분해 실행
                var rewards = Systems.DismantleSystem.DismantleItem(instanceId);
                bool success = rewards != null && rewards.Count > 0;
                
                if (success)
                {
                    Debug.Log($"✅ [DismantleV2WithWarning] 분해 성공");
                    OnInventoryChanged?.Invoke();
                }
                else
                {
                    Debug.LogError($"[DismantleV2WithWarning] 분해 실패");
                }
                
                onComplete?.Invoke(success, rewards);
            }
            else
            {
                // 사용자 취소
                Debug.Log($"[DismantleV2WithWarning] 사용자 취소");
                onComplete?.Invoke(false, null);
            }
        });
    }
    
    /// <summary>
    /// 아이템 분해 (경고 없이 즉시 실행)
    /// </summary>
    /// <param name="instanceId">분해할 아이템 ID</param>
    /// <returns>획득한 재료 목록</returns>
    public System.Collections.Generic.Dictionary<MaterialType, int> DismantleV2(ItemInstanceID instanceId)
    {
        if (instanceId.IsEmpty)
        {
            Debug.LogError("[DismantleV2] 잘못된 인스턴스 ID");
            return new System.Collections.Generic.Dictionary<MaterialType, int>();
        }
        
        if (!AccountDataManager.IsInitialized())
        {
            Debug.LogError("[DismantleV2] AccountDataManager가 초기화되지 않음");
            return new System.Collections.Generic.Dictionary<MaterialType, int>();
        }
        
        var rewards = Systems.DismantleSystem.DismantleItem(instanceId);
        
        if (rewards != null && rewards.Count > 0)
        {
            Debug.Log($"✅ [DismantleV2] 분해 성공");
            OnInventoryChanged?.Invoke();
        }
        else
        {
            Debug.LogError($"[DismantleV2] 분해 실패");
        }
        
        return rewards;
    }
    
    /// <summary>
    /// 합성 (V2) - 경고 포함
    /// </summary>
    public bool FuseV2WithWarning(System.Collections.Generic.List<ItemInstanceID> materialIds, System.Action<bool, ItemInstanceID> onComplete = null)
    {
        if (!AccountDataManager.IsInitialized())
        {
            Debug.LogError("[FuseV2] AccountDataManager가 초기화되지 않았습니다.");
            onComplete?.Invoke(false, default);
            return false;
        }
        
        // 1. 합성 가능 여부 확인
        if (!Systems.FusionSystem.CanFuse(materialIds, out string reason))
        {
            Debug.LogError($"[FuseV2] 합성 불가: {reason}");
            onComplete?.Invoke(false, default);
            return false;
        }
        
        var account = AccountDataManager.Instance;
        var firstItem = account.GetInstance(materialIds[0]);
        var firstTemplate = ItemTemplateResolver.Load(firstItem.templateName);
        
        // 2. 강화 경고 필요 여부 확인
        if (Systems.FusionSystem.NeedsEnhancementWarning(materialIds, out int maxEnhancementLevel))
        {
            // FusionRule 로드
            var rule = UnityEngine.Resources.Load<Systems.FusionRule>("Data/FusionRule");
            if (rule == null)
            {
                Debug.LogError("[FuseV2] FusionRule을 찾을 수 없습니다.");
                onComplete?.Invoke(false, default);
                return false;
            }
            
            // 경고 데이터 생성
            var warningData = new Systems.FusionWarningData(
                materialIds,
                firstItem.templateName,
                firstTemplate.itemGrade,
                rule.GetNextGrade(firstTemplate.itemGrade),
                materialIds.Count,
                rule.GetFusionCost(firstTemplate.itemGrade),
                maxEnhancementLevel
            );
            
            // 경고 팝업 표시
            Managers.FusionManager.Instance.ShowEnhancementWarning(warningData, (confirmed) =>
            {
                if (confirmed)
                {
                    // 합성 실행
                    bool success = Systems.FusionSystem.ExecuteFusion(materialIds, out ItemInstanceID resultId);
                    
                    if (success)
                    {
                        Debug.Log($"✅ [FuseV2] 합성 성공 (경고 확인 후)");
                        OnInventoryChanged?.Invoke();
                    }
                    
                    onComplete?.Invoke(success, resultId);
                }
                else
                {
                    Debug.Log($"[FuseV2] 합성 취소 (사용자)");
                    onComplete?.Invoke(false, default);
                }
            });
            
            return true; // 팝업 표시 성공
        }
        else
        {
            // 3. 경고 없이 바로 합성
            bool success = Systems.FusionSystem.ExecuteFusion(materialIds, out ItemInstanceID resultId);
            
            if (success)
            {
                Debug.Log($"✅ [FuseV2] 합성 성공");
                OnInventoryChanged?.Invoke();
            }
            
            onComplete?.Invoke(success, resultId);
            return success;
        }
    }
    
    // ========================================
    // Phase 7: 강화 시스템
    // ========================================
    
    /// <summary>
    /// 아이템 강화 (경고 포함)
    /// </summary>
    public void EnhanceV2WithWarning(ItemInstanceID instanceId, System.Action<EnhancementResult> onComplete)
    {
        if (instanceId.IsEmpty)
        {
            Debug.LogError("[EnhanceV2WithWarning] 잘못된 인스턴스 ID");
            onComplete?.Invoke(new EnhancementResult { success = false, errorMessage = "잘못된 아이템 ID" });
            return;
        }
        
        if (!AccountDataManager.IsInitialized())
        {
            Debug.LogError("[EnhanceV2WithWarning] AccountDataManager가 초기화되지 않음");
            onComplete?.Invoke(new EnhancementResult { success = false, errorMessage = "시스템 초기화 안 됨" });
            return;
        }
        
        // 강화 가능 여부 확인
        if (!Systems.EnhancementSystem.CanEnhance(instanceId, out string reason))
        {
            Debug.LogError($"[EnhanceV2WithWarning] 강화 불가: {reason}");
            onComplete?.Invoke(new EnhancementResult { success = false, errorMessage = reason });
            return;
        }
        
        var account = AccountDataManager.Instance;
        var itemData = account.GetInstance(instanceId);
        var template = ItemTemplateResolver.Load(itemData.templateName);
        
        // 경고 필요 여부 확인
        bool needsWarning = Systems.EnhancementSystem.NeedsDestructionWarning(instanceId);
        
        if (needsWarning)
        {
            // 강화 데이터 로드
            var enhanceData = UnityEngine.Resources.Load<Systems.EnhancementData>("Data/EnhancementData");
            if (enhanceData == null)
            {
                Debug.LogError("[EnhanceV2WithWarning] EnhancementData를 찾을 수 없습니다.");
                onComplete?.Invoke(new EnhancementResult { success = false, errorMessage = "강화 데이터 없음" });
                return;
            }
            
            // 경고 데이터 생성
            float successRate = Systems.EnhancementSystem.GetSuccessRate(instanceId);
            var failureType = enhanceData.GetFailureType(itemData.enhancementLevel);
            var materialType = enhanceData.GetRequiredMaterialType(template.equipmentType, template.itemGrade);
            int materialAmount = enhanceData.GetRequiredMaterialAmount(template.itemGrade, itemData.enhancementLevel + 1);
            int goldCost = enhanceData.GetRequiredGold(template.itemGrade, itemData.enhancementLevel + 1);
            
            var warningData = new Systems.EnhancementWarningData(
                instanceId,
                itemData.templateName,
                template.itemGrade,
                itemData.enhancementLevel,
                itemData.enhancementLevel + 1,
                successRate,
                failureType,
                materialAmount,
                materialType,
                goldCost
            );
            
            // 경고 팝업 표시
            Managers.EnhancementManager.Instance.ShowDestructionWarning(warningData, (confirmed) =>
            {
                if (confirmed)
                {
                    // 사용자 확인 → 강화 실행
                    var result = Systems.EnhancementSystem.ExecuteEnhancement(instanceId);
                    
                    if (result.success || result.wasDestroyed)
                    {
                        Debug.Log($"✅ [EnhanceV2WithWarning] 강화 완료 (경고 확인 후)");
                        OnInventoryChanged?.Invoke();
                    }
                    
                    onComplete?.Invoke(result);
                }
                else
                {
                    Debug.Log($"[EnhanceV2WithWarning] 강화 취소 (사용자)");
                    onComplete?.Invoke(new EnhancementResult { success = false, errorMessage = "사용자 취소" });
                }
            });
        }
        else
        {
            // 경고 없이 바로 강화
            var result = Systems.EnhancementSystem.ExecuteEnhancement(instanceId);
            
            if (result.success)
            {
                Debug.Log($"✅ [EnhanceV2WithWarning] 강화 성공");
                OnInventoryChanged?.Invoke();
            }
            
            onComplete?.Invoke(result);
        }
    }
    
    /// <summary>
    /// 아이템 강화 (경고 없이 즉시 실행)
    /// </summary>
    public EnhancementResult EnhanceV2(ItemInstanceID instanceId)
    {
        if (instanceId.IsEmpty)
        {
            Debug.LogError("[EnhanceV2] 잘못된 인스턴스 ID");
            return new EnhancementResult { success = false, errorMessage = "잘못된 아이템 ID" };
        }
        
        if (!AccountDataManager.IsInitialized())
        {
            Debug.LogError("[EnhanceV2] AccountDataManager가 초기화되지 않음");
            return new EnhancementResult { success = false, errorMessage = "시스템 초기화 안 됨" };
        }
        
        var result = Systems.EnhancementSystem.ExecuteEnhancement(instanceId);
        
        if (result.success)
        {
            Debug.Log($"✅ [EnhanceV2] 강화 성공");
            OnInventoryChanged?.Invoke();
        }
        else if (result.wasDestroyed)
        {
            Debug.Log($"💥 [EnhanceV2] 아이템 파괴");
            OnInventoryChanged?.Invoke();
        }
        
        return result;
    }
    
    #endregion
    
    #region 🎒 캐릭터 가방 관리 (인게임 임시 저장소)
    
    // 이벤트
    public event Action OnCharacterBagChanged;
    
    /// <summary>
    /// 캐릭터 가방에 재료 추가 (인게임 임시 저장)
    /// </summary>
    public void AddMaterialToCharacterBag(MaterialType type, int amount)
    {
        var slotData = GetCurrentSlotData();
        if (slotData == null)
        {
            Debug.LogError("[AddMaterialToCharacterBag] 현재 슬롯 데이터가 없습니다");
            return;
        }
        
        // 기존 재료 찾기
        var existing = slotData.characterBagMaterials.Find(m => m.materialType == type);
        
        if (existing != null)
        {
            existing.count += amount;
            
            if (showDebugLogs)
                Debug.Log($"📦 [CharacterBag] 재료 추가: {type.GetDisplayName()} +{amount} (총: {existing.count}개)");
        }
        else
        {
            slotData.characterBagMaterials.Add(new MaterialStack
            {
                materialType = type,
                count = amount
            });
            
            if (showDebugLogs)
                Debug.Log($"📦 [CharacterBag] 신규 재료 추가: {type.GetDisplayName()} x{amount}");
        }
        
        MarkDirty();
        OnCharacterBagChanged?.Invoke();
    }
    
    /// <summary>
    /// 캐릭터 가방의 장비 아이템 가져오기
    /// </summary>
    public List<EquipmentData> GetCharacterBagItems()
    {
        var slotData = GetCurrentSlotData();
        if (slotData == null) return new List<EquipmentData>();
        
        var bagItems = new List<EquipmentData>();
        var account = AccountDataManager.Instance;
        
        if (account == null) return bagItems;
        
        foreach (var instanceId in slotData.characterBagInstanceIds)
        {
            if (instanceId.IsEmpty) continue;
            
            var instance = account.GetInstance(instanceId);
            if (instance == null) continue;
            
            var template = ItemTemplateResolver.Load(instance.templateName);
            if (template != null)
            {
                bagItems.Add(template);
            }
        }
        
        return bagItems;
    }
    
    /// <summary>
    /// 🆕 캐릭터 가방의 장비 아이템 + ItemInstanceID 가져오기
    /// </summary>
    public List<(EquipmentData equipment, ItemInstanceID instanceId)> GetCharacterBagItemsWithIds()
    {
        var slotData = GetCurrentSlotData();
        if (slotData == null) return new List<(EquipmentData, ItemInstanceID)>();
        
        var bagItems = new List<(EquipmentData, ItemInstanceID)>();
        var account = AccountDataManager.Instance;
        
        if (account == null) return bagItems;
        
        foreach (var instanceId in slotData.characterBagInstanceIds)
        {
            if (instanceId.IsEmpty) continue;
            
            var instance = account.GetInstance(instanceId);
            if (instance == null) continue;
            
            var template = ItemTemplateResolver.Load(instance.templateName);
            if (template != null)
            {
                bagItems.Add((template, instanceId));  // ⭐ ItemInstanceID도 함께 반환!
            }
        }
        
        return bagItems;
    }
    
    /// <summary>
    /// 캐릭터 가방 초기화 (로비 복귀 시)
    /// </summary>
    public void ClearCharacterBag()
    {
        var slotData = GetCurrentSlotData();
        if (slotData == null)
        {
            Debug.LogWarning("[ClearCharacterBag] 현재 슬롯 데이터가 없습니다");
            return;
        }
        
        int equipCount = slotData.characterBagInstanceIds.Count;
        int matCount = slotData.characterBagMaterials.Count;
        
        slotData.characterBagInstanceIds.Clear();
        slotData.characterBagMaterials.Clear();
        
        MarkDirty();
        OnCharacterBagChanged?.Invoke();
        
        Debug.Log($"🧹 [CharacterBag] 초기화 완료 (장비: {equipCount}개, 재료: {matCount}개)");
    }
    
    /// <summary>
    /// 캐릭터 가방 → 보관창고 자동 전송 (스테이지 클리어 시)
    /// </summary>
    public void TransferCharacterBagToStorage()
    {
        var slotData = GetCurrentSlotData();
        if (slotData == null)
        {
            Debug.LogError("[TransferCharacterBag] 현재 슬롯 데이터가 없습니다");
            return;
        }
        
        var account = AccountDataManager.Instance;
        if (account == null)
        {
            Debug.LogError("[TransferCharacterBag] AccountDataManager가 초기화되지 않았습니다");
            return;
        }
        
        int equipTransferred = 0;
        int matTransferred = 0;
        
        // 1. 장비 전송 (기존 Phase 3.5)
        foreach (var instanceId in slotData.characterBagInstanceIds)
        {
            if (!instanceId.IsEmpty)
            {
                if (account.TryAddToShared(instanceId))
                {
                    equipTransferred++;
                }
                else
                {
                    Debug.LogWarning($"⚠️ [TransferCharacterBag] 장비 전송 실패: {instanceId.Value}");
                }
            }
        }
        
        // 2. 재료 전송 (신규)
        foreach (var mat in slotData.characterBagMaterials)
        {
            account.AddMaterial(mat.materialType, mat.count);
            matTransferred++;
            Debug.Log($"📦 [TransferCharacterBag] 재료 전송: {mat.materialType.GetDisplayName()} x{mat.count}");
        }
        
        // 3. 캐릭터 가방 초기화
        slotData.characterBagInstanceIds.Clear();
        slotData.characterBagMaterials.Clear();
        
        // 4. 저장
        MarkDirty();
        SaveOnMeaningfulEvent("CharacterBagTransferred");
        account.Save();
        
        Debug.Log($"✅ [TransferCharacterBag] 전송 완료 - 장비: {equipTransferred}개, 재료: {matTransferred}개");
    }
    
    #endregion
    
    #region 🎮 치트 명령어 (디버그용)
    
    /// <summary>
    /// [치트] 경험치 추가 테스트
    /// Unity 상단 메뉴: Tools → Player Cheats → Add Exp 50
    /// </summary>
    [ContextMenu("🎮 치트: 경험치 +50")]
    public void CheatAddExp50()
    {
        if (!IsSlotSelected)
        {
            Debug.LogWarning("⚠️ [치트] 슬롯이 선택되지 않았습니다!");
            return;
        }
        
        AddExp(50);
        Debug.Log($"🎮 [치트] 경험치 +50 추가 완료! 현재: {CurrentExp}/{ExpToNextLevel}");
    }
    
    /// <summary>
    /// [치트] 경험치 대량 추가
    /// </summary>
    [ContextMenu("🎮 치트: 경험치 +500")]
    public void CheatAddExp500()
    {
        if (!IsSlotSelected)
        {
            Debug.LogWarning("⚠️ [치트] 슬롯이 선택되지 않았습니다!");
            return;
        }
        
        AddExp(500);
        Debug.Log($"🎮 [치트] 경험치 +500 추가 완료! 현재: {CurrentExp}/{ExpToNextLevel}");
    }
    
    #endregion
}

/// <summary>
/// 마지막 선택 슬롯 데이터 (JSON 직렬화용)
/// </summary>
[System.Serializable]
public class LastSelectedSlotData
{
    public int lastSelectedSlotIndex;
} 