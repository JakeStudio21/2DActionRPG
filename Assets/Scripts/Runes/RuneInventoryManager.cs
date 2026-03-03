using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 룬 인벤토리 관리자 (Singleton)
/// ⚙️ Phase 4-D-2: 인벤토리 및 세이브/로드 시스템
/// 
/// 역할:
/// - 유저가 보유한 모든 RuneInstance를 List로 관리
/// - 룬 추가/삭제/검색 API 제공
/// - JSON 기반 세이브/로드 시스템 (PlayerPrefs 사용)
/// 
/// 핵심 API:
/// - AddRune(RuneData): 새 룬 추가
/// - RemoveRune(string uid): 룬 삭제
/// - GetRuneByUID(string uid): UID로 검색
/// - GetRunesByDataId(string runeId): 동일한 종류의 룬 전체 검색
/// - GetAllRunes(): 전체 룬 목록
/// - SaveInventory(): JSON 저장
/// - LoadInventory(): JSON 로드
/// </summary>
public class RuneInventoryManager : MonoBehaviour
{
    #region Singleton
    
    private static RuneInventoryManager _instance;
    public static RuneInventoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<RuneInventoryManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("RuneInventoryManager");
                    _instance = go.AddComponent<RuneInventoryManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        Initialize();
    }
    
    #endregion
    
    #region 필드
    
    /// <summary>
    /// 보유 중인 모든 룬 인스턴스
    /// </summary>
    [SerializeField]
    private List<RuneInstance> runeInventory = new List<RuneInstance>();
    
    /// <summary>
    /// PlayerPrefs 저장 키 (⚠️ Phase 9: PlayerSlotData로 이동됨)
    /// </summary>
    [System.Obsolete("Phase 9: 룬 데이터는 PlayerSlotData로 이동됨")]
    private const string SAVE_KEY = "RuneInventory_SaveData";
    
    [System.Obsolete("Phase 9: 룬 조각은 AccountData.materials로 이동됨")]
    private const string FRAGMENTS_SAVE_KEY = "RuneFragments_SaveData";
    
    #endregion
    
    #region 이벤트
    
    /// <summary>
    /// 인벤토리 변경 시 발생 (추가/삭제)
    /// </summary>
    public event Action OnInventoryChanged;
    
    /// <summary>
    /// 룬 추가 시 발생
    /// </summary>
    public event Action<RuneInstance> OnRuneAdded;
    
    /// <summary>
    /// 룬 삭제 시 발생
    /// </summary>
    public event Action<string> OnRuneRemoved; // UID
    
    #endregion
    
    #region 초기화
    
    /// <summary>
    /// 초기화
    /// </summary>
    private void Initialize()
    {
        Debug.Log("[RuneInventoryManager] 초기화");
        
        // Phase 9: PlayerSlotData에서 자동 로드
        // LoadInventory는 PlayerSlotDataManager에서 호출됨
    }
    
    #endregion
    
    #region 룬 추가
    
    /// <summary>
    /// 새로운 룬을 인벤토리에 추가
    /// ⚠️ RuneData를 기반으로 새로운 RuneInstance를 생성하여 추가
    /// </summary>
    /// <param name="baseData">원본 RuneData (ScriptableObject)</param>
    /// <returns>생성된 RuneInstance (실패 시 null)</returns>
    public RuneInstance AddRune(RuneData baseData)
    {
        // 유효성 검증
        if (baseData == null)
        {
            Debug.LogError("[RuneInventoryManager] AddRune: RuneData가 null입니다!");
            return null;
        }
        
        if (!baseData.IsValid())
        {
            Debug.LogError($"[RuneInventoryManager] AddRune: 유효하지 않은 RuneData: {baseData.name}");
            return null;
        }
        
        // 새로운 RuneInstance 생성 (Lv.1, 한돌 0)
        RuneInstance newInstance = new RuneInstance(baseData);
        
        // 인벤토리에 추가
        runeInventory.Add(newInstance);
        
        Debug.Log($"[RuneInventoryManager] 룬 추가: {newInstance} | 총 {runeInventory.Count}개");
        
        // 이벤트 발생
        OnRuneAdded?.Invoke(newInstance);
        OnInventoryChanged?.Invoke();
        
        return newInstance;
    }
    
    /// <summary>
    /// 이미 생성된 RuneInstance를 인벤토리에 추가
    /// ⚠️ 주로 로드 시스템에서 사용 (일반적으로 AddRune(RuneData) 사용 권장)
    /// </summary>
    /// <param name="instance">추가할 RuneInstance</param>
    /// <returns>성공 여부</returns>
    public bool AddRuneInstance(RuneInstance instance)
    {
        if (instance == null)
        {
            Debug.LogError("[RuneInventoryManager] AddRuneInstance: RuneInstance가 null입니다!");
            return false;
        }
        
        // 중복 체크 (동일 UID)
        if (runeInventory.Any(r => r.instanceUID == instance.instanceUID))
        {
            Debug.LogWarning($"[RuneInventoryManager] AddRuneInstance: 동일한 UID가 이미 존재합니다: {instance.instanceUID}");
            return false;
        }
        
        runeInventory.Add(instance);
        
        Debug.Log($"[RuneInventoryManager] 룬 인스턴스 추가: {instance} | 총 {runeInventory.Count}개");
        
        OnRuneAdded?.Invoke(instance);
        OnInventoryChanged?.Invoke();
        
        return true;
    }
    
    #endregion
    
    #region 룬 삭제
    
    /// <summary>
    /// 특정 UID의 룬을 인벤토리에서 삭제
    /// ⚠️ 한계돌파 재료 소모, 분해 등에 사용
    /// </summary>
    /// <param name="instanceUID">삭제할 룬의 UID</param>
    /// <returns>삭제 성공 여부</returns>
    public bool RemoveRune(string instanceUID)
    {
        // 빈 문자열 체크
        if (string.IsNullOrEmpty(instanceUID))
        {
            Debug.LogWarning("[RuneInventoryManager] RemoveRune: UID가 비어있습니다.");
            return false;
        }
        
        // 룬 찾기
        RuneInstance targetRune = runeInventory.FirstOrDefault(r => r.instanceUID == instanceUID);
        
        if (targetRune == null)
        {
            Debug.LogWarning($"[RuneInventoryManager] RemoveRune: UID를 찾을 수 없습니다: {instanceUID}");
            return false;
        }
        
        // 잠금 체크
        if (targetRune.isLocked)
        {
            Debug.LogWarning($"[RuneInventoryManager] RemoveRune: 잠긴 룬은 삭제할 수 없습니다: {targetRune}");
            return false;
        }
        
        // 삭제
        runeInventory.Remove(targetRune);
        
        Debug.Log($"[RuneInventoryManager] 룬 삭제: {targetRune} | 남은 룬: {runeInventory.Count}개");
        
        // 이벤트 발생
        OnRuneRemoved?.Invoke(instanceUID);
        OnInventoryChanged?.Invoke();
        
        return true;
    }
    
    /// <summary>
    /// 잠금 상태 무시하고 강제 삭제
    /// ⚠️ 주의: 일반적으로 사용하지 않음 (관리자 기능용)
    /// </summary>
    public bool ForceRemoveRune(string instanceUID)
    {
        if (string.IsNullOrEmpty(instanceUID))
        {
            return false;
        }
        
        RuneInstance targetRune = runeInventory.FirstOrDefault(r => r.instanceUID == instanceUID);
        
        if (targetRune == null)
        {
            return false;
        }
        
        runeInventory.Remove(targetRune);
        
        Debug.LogWarning($"[RuneInventoryManager] 강제 삭제: {targetRune}");
        
        OnRuneRemoved?.Invoke(instanceUID);
        OnInventoryChanged?.Invoke();
        
        return true;
    }
    
    #endregion
    
    #region 룬 검색
    
    /// <summary>
    /// UID로 특정 룬 검색
    /// </summary>
    /// <param name="instanceUID">검색할 UID</param>
    /// <returns>RuneInstance (없으면 null)</returns>
    public RuneInstance GetRuneByUID(string instanceUID)
    {
        if (string.IsNullOrEmpty(instanceUID))
        {
            Debug.LogWarning("[RuneInventoryManager] GetRuneByUID: UID가 비어있습니다.");
            return null;
        }
        
        return runeInventory.FirstOrDefault(r => r.instanceUID == instanceUID);
    }
    
    /// <summary>
    /// 특정 종류의 룬 전체 검색 (같은 RuneData ID를 가진 룬들)
    /// ⚠️ 한계돌파 재료 찾기 등에 사용
    /// </summary>
    /// <param name="runeId">RuneData의 runeId (예: RUNE_BOSS_HUNTER)</param>
    /// <returns>해당 종류의 룬 목록</returns>
    public List<RuneInstance> GetRunesByDataId(string runeId)
    {
        if (string.IsNullOrEmpty(runeId))
        {
            Debug.LogWarning("[RuneInventoryManager] GetRunesByDataId: runeId가 비어있습니다.");
            return new List<RuneInstance>();
        }
        
        return runeInventory.Where(r => r.baseDataId == runeId).ToList();
    }
    
    /// <summary>
    /// 특정 타입의 룬 전체 검색 (Attack1, Survival2 등)
    /// </summary>
    /// <param name="runeType">룬 타입</param>
    /// <returns>해당 타입의 룬 목록</returns>
    public List<RuneInstance> GetRunesByType(RuneType runeType)
    {
        return runeInventory.Where(r => r.baseData != null && r.baseData.runeType == runeType).ToList();
    }
    
    /// <summary>
    /// 보유 중인 전체 룬 목록 가져오기
    /// </summary>
    /// <returns>전체 룬 목록 (읽기 전용 List 반환)</returns>
    public List<RuneInstance> GetAllRunes()
    {
        return new List<RuneInstance>(runeInventory);
    }
    
    /// <summary>
    /// 인벤토리 총 개수
    /// </summary>
    public int GetRuneCount()
    {
        return runeInventory.Count;
    }
    
    /// <summary>
    /// 빈 인벤토리 여부
    /// </summary>
    public bool IsEmpty()
    {
        return runeInventory.Count == 0;
    }
    
    #endregion
    
    #region 세이브 & 로드
    
    /// <summary>
    /// 인벤토리 저장 (JSON → PlayerPrefs)
    /// </summary>
    /// <returns>저장 성공 여부</returns>
    public bool SaveInventory()
    {
        try
        {
            Debug.Log($"[RuneInventoryManager] 저장 시작... (총 {runeInventory.Count}개 룬)");
            
            // RuneInstance → RuneSaveData 변환
            var saveData = new RuneInventorySaveData
            {
                saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                saveVersion = 1,
                runeInstances = new List<RuneSaveData>()
            };
            
            foreach (var instance in runeInventory)
            {
                if (instance == null)
                {
                    Debug.LogWarning("[RuneInventoryManager] null 인스턴스 발견 (스킵)");
                    continue;
                }
                
                // PrepareForSave() 호출 (baseData → baseDataId 변환)
                instance.PrepareForSave();
                
                // RuneSaveData로 변환
                var runeSaveData = new RuneSaveData(instance);
                
                // 유효성 검증
                if (runeSaveData.IsValid())
                {
                    saveData.runeInstances.Add(runeSaveData);
                }
                else
                {
                    Debug.LogWarning($"[RuneInventoryManager] 유효하지 않은 세이브 데이터 (스킵): {instance}");
                }
            }
            
            // JSON 직렬화
            string json = JsonUtility.ToJson(saveData, true);
            
            // PlayerPrefs에 저장
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            
            Debug.Log($"[RuneInventoryManager] ✅ 저장 완료! ({saveData.runeInstances.Count}개 룬)");
            Debug.Log($"  저장 시간: {saveData.saveTimestamp}");
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RuneInventoryManager] ❌ 저장 실패: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 인벤토리 로드 (PlayerPrefs → JSON → RuneInstance 복원)
    /// </summary>
    /// <returns>로드 성공 여부</returns>
    public bool LoadInventory()
    {
        try
        {
            // PlayerPrefs에서 JSON 불러오기
            if (!PlayerPrefs.HasKey(SAVE_KEY))
            {
                Debug.Log("[RuneInventoryManager] 저장된 데이터가 없습니다. (초기 상태)");
                return false;
            }
            
            string json = PlayerPrefs.GetString(SAVE_KEY);
            
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[RuneInventoryManager] 저장된 JSON이 비어있습니다.");
                return false;
            }
            
            Debug.Log("[RuneInventoryManager] 로드 시작...");
            
            // JSON 역직렬화
            var saveData = JsonUtility.FromJson<RuneInventorySaveData>(json);
            
            if (saveData == null || saveData.runeInstances == null)
            {
                Debug.LogError("[RuneInventoryManager] JSON 역직렬화 실패!");
                return false;
            }
            
            Debug.Log($"  저장 시간: {saveData.saveTimestamp}");
            Debug.Log($"  저장 버전: {saveData.saveVersion}");
            Debug.Log($"  룬 개수: {saveData.runeInstances.Count}개");
            
            // 기존 인벤토리 초기화
            runeInventory.Clear();
            
            // RuneSaveData → RuneInstance 복원
            int successCount = 0;
            int failCount = 0;
            
            foreach (var runeSaveData in saveData.runeInstances)
            {
                if (runeSaveData == null || !runeSaveData.IsValid())
                {
                    Debug.LogWarning("[RuneInventoryManager] 유효하지 않은 세이브 데이터 (스킵)");
                    failCount++;
                    continue;
                }
                
                // RuneInstance 재조립 (RuneDatabase를 통해 baseData 재연결)
                RuneInstance restoredInstance = runeSaveData.ToRuneInstance();
                
                if (restoredInstance != null)
                {
                    runeInventory.Add(restoredInstance);
                    successCount++;
                }
                else
                {
                    Debug.LogWarning($"[RuneInventoryManager] RuneInstance 복원 실패: {runeSaveData}");
                    failCount++;
                }
            }
            
            Debug.Log($"[RuneInventoryManager] ✅ 로드 완료!");
            Debug.Log($"  성공: {successCount}개");
            if (failCount > 0)
                Debug.LogWarning($"  실패: {failCount}개");
            
            // 이벤트 발생
            OnInventoryChanged?.Invoke();
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RuneInventoryManager] ❌ 로드 실패: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 저장 데이터 삭제 (초기화)
    /// ⚠️ 주의: 모든 룬 데이터가 영구적으로 삭제됨
    /// </summary>
    public void DeleteSaveData()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            
            Debug.LogWarning("[RuneInventoryManager] 저장 데이터 삭제됨!");
        }
        
        runeInventory.Clear();
        OnInventoryChanged?.Invoke();
    }
    
    #endregion
    
    #region 유틸리티
    
    /// <summary>
    /// 잠금 설정
    /// </summary>
    public bool SetLock(string instanceUID, bool locked)
    {
        var rune = GetRuneByUID(instanceUID);
        if (rune == null)
        {
            Debug.LogWarning($"[RuneInventoryManager] SetLock: UID를 찾을 수 없습니다: {instanceUID}");
            return false;
        }
        
        rune.isLocked = locked;
        Debug.Log($"[RuneInventoryManager] 잠금 설정: {rune} → {(locked ? "🔒" : "🔓")}");
        
        return true;
    }
    
    /// <summary>
    /// 인벤토리 정렬 (레벨 내림차순 → 한계돌파 내림차순)
    /// </summary>
    public void SortInventory()
    {
        runeInventory = runeInventory
            .OrderByDescending(r => r.currentLevel)
            .ThenByDescending(r => r.currentLimitBreak)
            .ToList();
        
        Debug.Log("[RuneInventoryManager] 인벤토리 정렬 완료");
        OnInventoryChanged?.Invoke();
    }
    
    #endregion
    
    #region 룬 조각 관리 (Phase 9: AccountData Material 연동)
    
    /// <summary>
    /// [Phase 9] 특정 룬의 조각 개수 조회 - AccountData Material 연동
    /// </summary>
    public int GetFragmentCount(string runeId)
    {
        // runeId (예: "RUNE_BOSS_HUNTER") → MaterialType (예: RUNE_FRAG_RUNE_BOSS_HUNTER)
        string materialId = $"RUNE_FRAG_{runeId}";
        MaterialType materialType = MaterialTypeExtensions.FromItemId(materialId);
        
        if (materialType == MaterialType.None)
        {
            Debug.LogWarning($"[RuneInventoryManager] GetFragmentCount: MaterialType 변환 실패: {materialId}");
            return 0;
        }
        
        // AccountData에서 실제 보유량 조회
        return AccountDataManager.Instance?.GetMaterialCount(materialType) ?? 0;
    }
    
    /// <summary>
    /// [Phase 9] 특정 룬의 조각 추가 - AccountData Material 연동
    /// ⚠️ Obsolete: 직접 사용하지 말고 StageEndItemTransfer가 자동 처리
    /// </summary>
    [System.Obsolete("Phase 9: AccountData Material 시스템이 자동 처리함")]
    public void AddFragments(string runeId, int amount)
    {
        Debug.LogWarning($"[RuneInventoryManager] AddFragments는 Obsolete입니다. AccountData Material 시스템이 자동 처리합니다.");
    }
    
    /// <summary>
    /// [Phase 9] 룬 조각 획득 (드롭 시스템 연동용)
    /// ⚠️ Obsolete: AccountData Material 시스템이 자동 처리
    /// </summary>
    [System.Obsolete("Phase 9: 드롭 시스템이 AccountData Material로 직접 저장함")]
    public void AddRuneFragment(string runeId, int amount)
    {
        Debug.LogWarning($"[RuneInventoryManager] AddRuneFragment는 Obsolete입니다. 드롭 시스템이 AccountData Material로 직접 저장합니다.");
    }
    
    /// <summary>
    /// [Phase 9] 특정 룬의 조각 소모 시도 - AccountData Material 연동
    /// </summary>
    public bool TryConsumeFragments(string runeId, int amount)
    {
        // runeId → MaterialType 변환
        string materialId = $"RUNE_FRAG_{runeId}";
        MaterialType materialType = MaterialTypeExtensions.FromItemId(materialId);
        
        if (materialType == MaterialType.None)
        {
            Debug.LogWarning($"[RuneInventoryManager] TryConsumeFragments: MaterialType 변환 실패: {materialId}");
            return false;
        }
        
        // AccountData에서 실제 소모
        bool success = AccountDataManager.Instance?.RemoveMaterial(materialType, amount) ?? false;
        
        if (success)
        {
            Debug.Log($"[RuneInventoryManager] 조각 소모: {runeId} -{amount}개 (남은 개수: {GetFragmentCount(runeId)}개)");
            OnInventoryChanged?.Invoke(); // UI 갱신
        }
        else
        {
            int currentCount = GetFragmentCount(runeId);
            Debug.LogWarning($"[RuneInventoryManager] 조각 부족: {runeId} (보유: {currentCount}개, 필요: {amount}개)");
        }
        
        return success;
    }
    
    /// <summary>
    /// [Phase 9] 모든 룬 조각 정보 반환 - AccountData Material 기반
    /// </summary>
    public Dictionary<string, int> GetAllFragments()
    {
        var result = new Dictionary<string, int>();
        
        // Resources/Runes 폴더에서 모든 RuneData 로드
        var allRunes = Resources.LoadAll<RuneData>("Runes");
        
        foreach (var runeData in allRunes)
        {
            int count = GetFragmentCount(runeData.runeId);
            result[runeData.runeId] = count;
        }
        
        return result;
    }
    
    #endregion
    
    #region 디버그
    
    /// <summary>
    /// 인벤토리 전체 출력
    /// </summary>
    [ContextMenu("Print Inventory")]
    public void PrintInventory()
    {
        Debug.Log("========== [RuneInventoryManager] 인벤토리 ==========");
        Debug.Log($"총 {runeInventory.Count}개의 룬:");
        
        if (runeInventory.Count == 0)
        {
            Debug.Log("  (비어있음)");
        }
        else
        {
            for (int i = 0; i < runeInventory.Count; i++)
            {
                var rune = runeInventory[i];
                Debug.Log($"  [{i + 1}] {rune} {(rune.isLocked ? "🔒" : "")}");
            }
        }
        
        Debug.Log("====================================================");
    }
    
    #endregion
    
    #region 디버그 & 테스트 (Phase 8-1)
    
    /// <summary>
    /// [ContextMenu] 테스트용 룬 조각 획득 (보스 사냥꾼 +100)
    /// </summary>
    [ContextMenu("테스트: 보스 사냥꾼 조각 +100")]
    private void DebugAddBossHunterFragments()
    {
        const string testRuneId = "RUNE_BOSS_HUNTER";
        const int testAmount = 100;
        
        AddRuneFragment(testRuneId, testAmount);
        
        Debug.Log($"[DEBUG] {testRuneId} 조각 {testAmount}개 추가 완료!");
    }
    
    /// <summary>
    /// [ContextMenu] 테스트용 모든 룬 조각 +50
    /// </summary>
    [ContextMenu("테스트: 모든 룬 조각 +50")]
    private void DebugAddAllFragments()
    {
        var allFragments = GetAllFragments();
        
        foreach (var runeId in allFragments.Keys)
        {
            AddRuneFragment(runeId, 50);
        }
        
        Debug.Log($"[DEBUG] 모든 룬 조각 50개씩 추가 완료! (총 {allFragments.Count}종류)");
    }
    
    #endregion
}

