using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 계정 데이터 관리자
/// ⭐ 싱글톤이지만 Initialize()로 순서 제어 가능
/// ⭐ 캐시는 Manager에서 관리 (AccountData는 순수 DTO)
/// </summary>
public class AccountDataManager
{
    private static AccountDataManager _instance;
    public static AccountDataManager Instance => _instance;
    
    private IStorage storage;
    private AccountData accountData;
    
    // ⭐ 캐시는 Manager에서 관리
    private Dictionary<ItemInstanceId, ItemInstanceData> instanceCache;
    private Dictionary<ItemInstanceId, int> bindCache; // instanceId -> characterSlotIndex
    private Dictionary<string, int> materialCache; // materialId -> count
    
    private const string ACCOUNT_SAVE_KEY = "Account";
    
    // ========================================
    // 초기화 (순서 제어 가능)
    // ========================================
    
    /// <summary>
    /// AccountDataManager 초기화
    /// ⭐ GameManager.Awake()에서 호출하여 순서 제어
    /// </summary>
    public static void Initialize(IStorage customStorage = null)
    {
        if (_instance == null)
        {
            _instance = new AccountDataManager();
            _instance.storage = customStorage ?? new JsonFileStorage();
            _instance.Load();
            
            Debug.Log("✨ [AccountDataManager] 초기화 완료");
        }
        else
        {
            Debug.LogWarning("⚠️ [AccountDataManager] 이미 초기화됨");
        }
    }
    
    /// <summary>
    /// 초기화 여부 확인
    /// </summary>
    public static bool IsInitialized()
    {
        return _instance != null;
    }
    
    // ========================================
    // 저장/로드
    // ========================================
    
    public void Load()
    {
        Debug.Log($"🔄 [AccountDataManager] Load() 시작 - 저장 키: {ACCOUNT_SAVE_KEY}");
        Debug.Log($"🔍 [AccountDataManager] Load() 호출 스택:\n{System.Environment.StackTrace}");
        
        string json = storage.Load(ACCOUNT_SAVE_KEY);
        
        if (string.IsNullOrEmpty(json))
        {
            // 신규 계정
            accountData = new AccountData();
            Debug.Log("✨ [AccountDataManager] 신규 계정 데이터 생성 (저장 파일 없음)");
        }
        else
        {
            accountData = JsonUtility.FromJson<AccountData>(json);
            Debug.Log($"📥 [AccountDataManager] 계정 데이터 로드 완료!");
            Debug.Log($"   - 아이템 인스턴스: {accountData.itemInstances.Count}개");
            Debug.Log($"   - 공유 창고: {accountData.sharedInventoryIds.Count}개");
            Debug.Log($"   - 우편함: {accountData.mailboxIds.Count}개");
            Debug.Log($"   - 귀속 정보: {accountData.binds.Count}개");
            Debug.Log($"🔍 [AccountDataManager] Load() 완료 후 accountData 해시코드: {accountData.GetHashCode()}");
        }
        
        // 캐시 재구축
        RebuildCache();
    }
    
    public void Save()
    {
        Debug.Log($"💾 [AccountDataManager] Save() 시작");
        Debug.Log($"   - 아이템 인스턴스: {accountData.itemInstances.Count}개");
        Debug.Log($"   - 공유 창고: {accountData.sharedInventoryIds.Count}개");
        Debug.Log($"   - 우편함: {accountData.mailboxIds.Count}개");
        Debug.Log($"🔍 [AccountDataManager] Save() 시작 시 accountData 해시코드: {accountData.GetHashCode()}");
        
        string json = JsonUtility.ToJson(accountData, true);
        storage.Save(ACCOUNT_SAVE_KEY, json);
        
        Debug.Log($"✅ [AccountDataManager] 계정 데이터 저장 완료 ({json.Length} bytes)");
        Debug.Log($"🔍 [AccountDataManager] Save() 완료 후 accountData 해시코드: {accountData.GetHashCode()}");
        Debug.Log($"🔍 [AccountDataManager] Save() 완료 후 공유 창고: {accountData.sharedInventoryIds.Count}개");
    }
    
    /// <summary>
    /// 캐시 재구축 (Load 후 + 중요 트랜잭션 후)
    /// </summary>
    private void RebuildCache()
    {
        instanceCache = new Dictionary<ItemInstanceId, ItemInstanceData>();
        bindCache = new Dictionary<ItemInstanceId, int>();
        materialCache = new Dictionary<string, int>();
        
        // 인스턴스 캐시
        foreach (var instance in accountData.itemInstances)
        {
            instanceCache[instance.instanceId] = instance;
        }
        
        // 귀속 캐시
        foreach (var bind in accountData.binds)
        {
            bindCache[bind.instanceId] = bind.characterSlotIndex;
        }
        
        // 재료 캐시
        foreach (var mat in accountData.materials)
        {
            materialCache[mat.materialId] = mat.count;
        }
        
        Debug.Log($"🔄 [AccountDataManager] 캐시 재구축 완료: 인스턴스 {instanceCache.Count}개");
    }
    
    // ========================================
    // 💰 골드 관리 (V2 계정 공유 시스템)
    // ========================================
    
    /// <summary>
    /// 골드 변경 이벤트 (UI 동기화용)
    /// </summary>
    public System.Action<int> OnGoldChanged;
    
    /// <summary>
    /// 현재 골드 조회
    /// </summary>
    public int CurrentGold
    {
        get => accountData?.gold ?? 0;
        private set
        {
            if (accountData != null)
            {
                accountData.gold = value;
                OnGoldChanged?.Invoke(value);
                Debug.Log($"💰 [AccountDataManager] 골드 변경: {value}");
            }
        }
    }
    
    /// <summary>
    /// 골드 추가 (보상, 판매 등)
    /// </summary>
    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"⚠️ [AccountDataManager] 잘못된 골드 추가: {amount}");
            return;
        }
        
        CurrentGold += amount;
        Debug.Log($"💰 [AccountDataManager] 골드 추가: +{amount} → {CurrentGold}");
    }
    
    /// <summary>
    /// 골드 소비 (구매, 강화 등)
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"⚠️ [AccountDataManager] 잘못된 골드 소비: {amount}");
            return false;
        }
        
        if (CurrentGold < amount)
        {
            Debug.LogWarning($"⚠️ [AccountDataManager] 골드 부족: {CurrentGold} < {amount}");
            return false;
        }
        
        CurrentGold -= amount;
        Debug.Log($"💰 [AccountDataManager] 골드 소비: -{amount} → {CurrentGold}");
        return true;
    }
    
    /// <summary>
    /// 골드 충분 여부 확인
    /// </summary>
    public bool HasEnoughGold(int amount)
    {
        return CurrentGold >= amount;
    }
    
    // ========================================
    // 아이템 인스턴스 관리
    // ========================================
    
    /// <summary>
    /// 신규 아이템 인스턴스 등록
    /// </summary>
    public ItemInstanceId RegisterNewInstance(string templateName)
    {
        var newId = ItemInstanceId.NewId();
        var instanceData = new ItemInstanceData
        {
            instanceId = newId,
            templateName = templateName,
            enhancementLevel = 0,
            enhancementAttempts = 0,
            customName = "",
            acquiredTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            craftedFromRecipeId = -1
        };
        
        accountData.itemInstances.Add(instanceData);
        instanceCache[newId] = instanceData;
        
        Debug.Log($"✨ [AccountDataManager] 신규 아이템 등록: {templateName} (ID: {newId.id})");
        return newId;
    }
    
    /// <summary>
    /// 아이템 인스턴스 조회
    /// </summary>
    public ItemInstanceData GetInstance(ItemInstanceId id)
    {
        if (instanceCache.TryGetValue(id, out var instance))
            return instance;
        
        Debug.LogWarning($"⚠️ [AccountDataManager] 아이템 인스턴스를 찾을 수 없음: {id.id}");
        return null;
    }
    
    /// <summary>
    /// 아이템 인스턴스 삭제 (분해/합성 재료로 사용)
    /// </summary>
    public void RemoveInstance(ItemInstanceId id)
    {
        // 모든 컨테이너에서 제거
        accountData.sharedInventoryIds.Remove(id);
        accountData.mailboxIds.Remove(id);
        accountData.itemInstances.RemoveAll(i => i.instanceId == id);
        accountData.binds.RemoveAll(b => b.instanceId == id);
        
        instanceCache.Remove(id);
        bindCache.Remove(id);
        
        Debug.Log($"🗑️ [AccountDataManager] 아이템 인스턴스 삭제: {id.id}");
    }
    
    /// <summary>
    /// ⭐ 새로운 아이템 인스턴스 생성 (상점 구매, 드롭 등)
    /// </summary>
    public ItemInstanceId CreateInstance(string templateName, int enhancementLevel = 0)
    {
        // 새 Instance ID 생성
        var newInstanceId = ItemInstanceId.NewId();
        
        // ItemInstanceData 생성
        var instanceData = new ItemInstanceData
        {
            instanceId = newInstanceId,
            templateName = templateName,
            enhancementLevel = enhancementLevel
        };
        
        // AccountData에 추가
        accountData.itemInstances.Add(instanceData);
        
        // 캐시에 추가
        instanceCache[newInstanceId] = instanceData;
        
        Debug.Log($"✨ [AccountDataManager] 새 아이템 인스턴스 생성: {templateName} (ID: {newInstanceId.id.Substring(0, 8)}..., 강화: +{enhancementLevel})");
        
        return newInstanceId;
    }
    
    // ========================================
    // 공유 창고 관리
    // ========================================
    
    /// <summary>
    /// 계정 공유 창고에 추가
    /// </summary>
    public bool TryAddToShared(ItemInstanceId id, int? maxSize = null)
    {
        // ✅ maxSize가 지정되지 않으면 AccountData의 maxSharedInventorySize 사용
        int actualMaxSize = maxSize ?? accountData.maxSharedInventorySize;
        
        // 중복 체크
        if (accountData.sharedInventoryIds.Contains(id))
        {
            Debug.LogWarning($"⚠️ [AccountDataManager] 이미 창고에 존재하는 아이템: {id.id}");
            return false;
        }
        
        // 크기 체크
        if (accountData.sharedInventoryIds.Count >= actualMaxSize)
        {
            Debug.LogWarning($"⚠️ [AccountDataManager] 창고가 가득 참! ({accountData.sharedInventoryIds.Count}/{actualMaxSize}개)");
            return false;
        }
        
        accountData.sharedInventoryIds.Add(id);
        return true;
    }
    
    /// <summary>
    /// 창고에서 제거
    /// </summary>
    public bool RemoveFromShared(ItemInstanceId id)
    {
        bool removed = accountData.sharedInventoryIds.Remove(id);
        if (removed)
            Debug.Log($"🗑️ [AccountDataManager] 창고 제거: {id.id}");
        return removed;
    }
    
    /// <summary>
    /// ⭐ 우편함으로 이동 (창고 꽉 찼을 때)
    /// 중복 방지 + shared에서 제거하지 않음
    /// </summary>
    public bool MoveToMailbox(ItemInstanceId id)
    {
        // 중복 체크 (이미 mailbox에 있으면 추가 안 함)
        if (accountData.mailboxIds.Contains(id))
        {
            Debug.LogWarning($"⚠️ [AccountDataManager] 이미 우편함에 존재하는 아이템: {id.id}");
            return false;
        }
        
        // ❌ shared에서 제거하지 않음! (다른 곳에 있을 수도 있음)
        
        // ✅ 단순히 mailbox에 추가만
        accountData.mailboxIds.Add(id);
        Debug.Log($"📬 [AccountDataManager] 우편함으로 이동: {id.id}");
        return true;
    }
    
    /// <summary>
    /// 우편함에서 제거
    /// </summary>
    public bool RemoveFromMailbox(ItemInstanceId id)
    {
        bool removed = accountData.mailboxIds.Remove(id);
        if (removed)
            Debug.Log($"📬 [AccountDataManager] 우편함 제거: {id.id}");
        return removed;
    }
    
    // ========================================
    // 귀속 관리
    // ========================================
    
    /// <summary>
    /// 아이템 귀속 설정
    /// </summary>
    public void SetBind(ItemInstanceId id, int characterSlotIndex)
    {
        // 기존 귀속 제거
        accountData.binds.RemoveAll(b => b.instanceId == id);
        
        // 새 귀속 추가
        var bindRecord = new ItemBindRecord
        {
            instanceId = id,
            characterSlotIndex = characterSlotIndex,
            bindTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
        
        accountData.binds.Add(bindRecord);
        bindCache[id] = characterSlotIndex;
        
        Debug.Log($"🔒 [AccountDataManager] 아이템 귀속: {id.id} → Slot {characterSlotIndex}");
    }
    
    /// <summary>
    /// 귀속 여부 확인
    /// </summary>
    public bool IsBound(ItemInstanceId id)
    {
        return bindCache.ContainsKey(id);
    }
    
    /// <summary>
    /// 다른 캐릭터에게 귀속되었는지 확인
    /// </summary>
    public bool IsBoundToOther(ItemInstanceId id, int characterSlotIndex)
    {
        if (bindCache.TryGetValue(id, out int boundSlot))
            return boundSlot != characterSlotIndex;
        return false;
    }
    
    /// <summary>
    /// 귀속 정보 조회
    /// </summary>
    public (bool isBound, int characterSlotIndex) GetBindInfo(ItemInstanceId id)
    {
        if (bindCache.TryGetValue(id, out int slotIndex))
            return (true, slotIndex);
        return (false, -1);
    }
    
    /// <summary>
    /// 귀속 해제 (분해 시 사용)
    /// </summary>
    public void RemoveBind(ItemInstanceId id)
    {
        accountData.binds.RemoveAll(b => b.instanceId == id);
        bindCache.Remove(id);
        Debug.Log($"🔓 [AccountDataManager] 귀속 해제: {id.id}");
    }
    
    // ========================================
    // 재료 관리
    // ========================================
    
    /// <summary>
    /// 재료 추가
    /// </summary>
    public void AddMaterial(string materialId, int amount)
    {
        if (materialCache.TryGetValue(materialId, out int currentCount))
        {
            // 기존 스택 증가
            currentCount += amount;
            materialCache[materialId] = currentCount;
            
            var stack = accountData.materials.Find(m => m.materialId == materialId);
            stack.count = currentCount;
        }
        else
        {
            // 신규 재료
            var stack = new MaterialStack { materialId = materialId, count = amount };
            accountData.materials.Add(stack);
            materialCache[materialId] = amount;
        }
        
        Debug.Log($"🎁 [AccountDataManager] 재료 추가: {materialId} +{amount} (총: {materialCache[materialId]}개)");
    }
    
    /// <summary>
    /// 재료 소모
    /// </summary>
    public bool ConsumeMaterial(string materialId, int amount)
    {
        if (!materialCache.TryGetValue(materialId, out int currentCount))
        {
            Debug.LogWarning($"⚠️ [AccountDataManager] 재료 없음: {materialId}");
            return false;
        }
        
        if (currentCount < amount)
        {
            Debug.LogWarning($"⚠️ [AccountDataManager] 재료 부족: {materialId} (필요: {amount}, 보유: {currentCount})");
            return false;
        }
        
        currentCount -= amount;
        materialCache[materialId] = currentCount;
        
        var stack = accountData.materials.Find(m => m.materialId == materialId);
        stack.count = currentCount;
        
        // 0개가 되면 제거
        if (currentCount == 0)
        {
            accountData.materials.Remove(stack);
            materialCache.Remove(materialId);
        }
        
        Debug.Log($"🎁 [AccountDataManager] 재료 소모: {materialId} -{amount} (남은: {currentCount}개)");
        return true;
    }
    
    /// <summary>
    /// 재료 보유량 조회
    /// </summary>
    public int GetMaterialCount(string materialId)
    {
        return materialCache.TryGetValue(materialId, out int count) ? count : 0;
    }
    
    /// <summary>
    /// 재료 추가 (MaterialType enum 기반)
    /// </summary>
    public void AddMaterial(MaterialType materialType, int amount)
    {
        AddMaterial(materialType.ToString(), amount);
    }
    
    /// <summary>
    /// 재료 소모 (MaterialType enum 기반)
    /// </summary>
    public bool ConsumeMaterial(MaterialType materialType, int amount)
    {
        return ConsumeMaterial(materialType.ToString(), amount);
    }
    
    /// <summary>
    /// 재료 보유량 조회 (MaterialType enum 기반)
    /// </summary>
    public int GetMaterialCount(MaterialType materialType)
    {
        return GetMaterialCount(materialType.ToString());
    }
    
    // ========================================
    // 유틸리티
    // ========================================
    
    /// <summary>
    /// AccountData 직접 접근 (읽기 전용)
    /// </summary>
    public AccountData GetAccountData()
    {
        // 🔍 디버그: 호출 시점 추적
        Debug.Log($"🔍 [AccountDataManager] GetAccountData() 호출됨 - 공유 창고: {accountData?.sharedInventoryIds?.Count ?? 0}개");
        return accountData;
    }
    
    /// <summary>
    /// 저장소 경로 확인 (디버깅용)
    /// </summary>
    public string GetStoragePath()
    {
        if (storage is JsonFileStorage jsonStorage)
            return jsonStorage.GetBasePath();
        return "Unknown";
    }
    
    /// <summary>
    /// 통계 정보 출력
    /// </summary>
    public void PrintStats()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"📊 [AccountDataManager] 통계");
        Debug.Log($"   - 아이템 인스턴스: {accountData.itemInstances.Count}개");
        Debug.Log($"   - 공유 창고: {accountData.sharedInventoryIds.Count}개");
        Debug.Log($"   - 우편함: {accountData.mailboxIds.Count}개");
        Debug.Log($"   - 귀속 정보: {accountData.binds.Count}개");
        Debug.Log($"   - 재료: {accountData.materials.Count}종류");
        foreach (var mat in accountData.materials)
        {
            Debug.Log($"     - {mat.materialId}: {mat.count}개");
        }
        Debug.Log("═══════════════════════════════════════════════════════");
    }
    
    /// <summary>
    /// 모든 계정 데이터 초기화 (테스트용)
    /// </summary>
    public void ClearAllData()
    {
        accountData.sharedInventoryIds.Clear();
        accountData.mailboxIds.Clear();
        accountData.itemInstances.Clear();
        accountData.binds.Clear();
        accountData.materials.Clear();
        
        instanceCache.Clear();
        bindCache.Clear();
        materialCache.Clear();
        
        Debug.Log("🧹 [AccountDataManager] 모든 계정 데이터 초기화 완료");
    }
}

