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
    private Dictionary<ItemInstanceID, ItemInstanceData> instanceCache;
    private Dictionary<ItemInstanceID, int> bindCache; // instanceId -> characterSlotIndex
    private Dictionary<MaterialType, int> materialCache; // ⭐ MaterialType enum 기반
    
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
        
        // Phase 3.5: 스킬 데이터 마이그레이션 (Account.json → PlayerSlotData)
        MigrateSkillDataToSlots();
    }
    
    /// <summary>
    /// Phase 3.5: 스킬 데이터를 PlayerSlotData로 마이그레이션
    /// 기존 Account.json의 스킬 데이터를 각 캐릭터 슬롯으로 이동
    /// </summary>
    private void MigrateSkillDataToSlots()
    {
        #pragma warning disable CS0618 // Obsolete 경고 무시
        
        // 마이그레이션 불필요 조건
        if (accountData.skills == null || accountData.skills.Count == 0)
        {
            return; // 스킬 데이터 없음 (신규 계정 또는 이미 마이그레이션 완료)
        }
        
        Debug.Log("🔄 [AccountDataManager] Phase 3.5 마이그레이션 시작: 스킬 데이터를 PlayerSlotData로 이동...");
        Debug.Log($"   - 마이그레이션할 스킬: {accountData.skills.Count}개");
        Debug.Log($"   - 마이그레이션할 액티브 슬롯: {accountData.equippedActiveSkillIds.Length}개");
        Debug.Log($"   - 마이그레이션할 패시브 슬롯: {accountData.equippedPassiveSkillIds.Length}개");
        Debug.Log($"   - 마이그레이션할 SP: {accountData.usedSP}/{accountData.totalSP}");
        
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogWarning("⚠️ [AccountDataManager] PlayerDataManager가 없어 마이그레이션 건너뜀");
            return;
        }
        
        int migratedCount = 0;
        
        // 모든 슬롯에 동일하게 복사 (임시 방편 - 향후 클래스별 구분 필요)
        for (int i = 0; i < 3; i++)
        {
            var slotData = PlayerDataManager.Instance.GetSlotData(i);
            if (slotData != null && slotData.isSlotUsed)
            {
                // 이미 스킬 데이터가 있으면 스킵 (중복 마이그레이션 방지)
                if (slotData.skills != null && slotData.skills.Count > 0)
                {
                    Debug.Log($"   ⏩ 슬롯 {i} ({slotData.playerName}): 이미 스킬 데이터 있음 - 스킵");
                    continue;
                }
                
                // 스킬 데이터 복사
                slotData.skills = new List<SkillInstanceSaveData>(accountData.skills);
                
                // 장착 슬롯 복사
                slotData.equippedActiveSkillIds = new string[2];
                for (int j = 0; j < Mathf.Min(accountData.equippedActiveSkillIds.Length, 2); j++)
                {
                    slotData.equippedActiveSkillIds[j] = accountData.equippedActiveSkillIds[j];
                }
                
                slotData.equippedPassiveSkillIds = new string[3];
                for (int j = 0; j < Mathf.Min(accountData.equippedPassiveSkillIds.Length, 3); j++)
                {
                    slotData.equippedPassiveSkillIds[j] = accountData.equippedPassiveSkillIds[j];
                }
                
                // SP 복사 (레벨 기반으로 재계산)
                slotData.totalSP = slotData.level; // totalSP = level (1:1 동기화)
                slotData.usedSP = Mathf.Min(accountData.usedSP, slotData.totalSP); // 초과하지 않도록
                
                // 저장
                PlayerDataManager.Instance.SaveSlotData(slotData);
                migratedCount++;
                
                Debug.Log($"   ✅ 슬롯 {i} ({slotData.playerName}): 마이그레이션 완료");
                Debug.Log($"      - 스킬: {slotData.skills.Count}개");
                Debug.Log($"      - SP: {slotData.usedSP}/{slotData.totalSP}");
            }
        }
        
        // 마이그레이션 완료 - Account.json에서 스킬 데이터 제거
        if (migratedCount > 0)
        {
            accountData.skills.Clear();
            accountData.equippedActiveSkillIds = new string[2];
            accountData.equippedPassiveSkillIds = new string[3];
            accountData.totalSP = 0;
            accountData.usedSP = 0;
            accountData.currentPlayerLevel = 1;
            
            Save();
            
            Debug.Log($"✅ [AccountDataManager] Phase 3.5 마이그레이션 완료!");
            Debug.Log($"   - 마이그레이션된 슬롯: {migratedCount}개");
            Debug.Log($"   - Account.json 스킬 데이터 정리 완료");
        }
        
        #pragma warning restore CS0618
    }
    
    public void Save()
    {
        // 📊 저장 전 JSON 가독성 필드 자동 채우기
        UpdateReadabilityFields();
        
        string json = JsonUtility.ToJson(accountData, true);
        storage.Save(ACCOUNT_SAVE_KEY, json);
    }
    
    /// <summary>
    /// JSON 가독성을 위한 필드 자동 채우기 (저장 전)
    /// </summary>
    private void UpdateReadabilityFields()
    {
        // 1. 공유 창고 개수 (sharedInventoryIds.Count)
        accountData.currentSharedInventoryCount = accountData.sharedInventoryIds.Count;
        
        // 2. 아이템 인스턴스 개수 (itemInstances.Count)
        accountData.currentItemInstancesCount = accountData.itemInstances.Count;
        
        // 3. 재료 타입 이름 (materialType → materialTypeName, displayName)
        foreach (var material in accountData.materials)
        {
            material.materialTypeName = material.materialType.ToString(); // "WeaponFragment"
            material.displayName = material.materialType.GetDisplayName(); // "무기 강화 파편"
        }
    }
    
    /// <summary>
    /// 캐시 재구축 (Load 후 + 중요 트랜잭션 후)
    /// </summary>
    private void RebuildCache()
    {
        instanceCache = new Dictionary<ItemInstanceID, ItemInstanceData>();
        bindCache = new Dictionary<ItemInstanceID, int>();
        materialCache = new Dictionary<MaterialType, int>();
        
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
        
        // 재료 캐시 (MaterialType enum 기반)
        foreach (var mat in accountData.materials)
        {
            materialCache[mat.materialType] = mat.count;
        }
        
        Debug.Log($"🔄 [AccountDataManager] 캐시 재구축 완료: 인스턴스 {instanceCache.Count}개, 재료 {materialCache.Count}종류");
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
    public ItemInstanceID RegisterNewInstance(string templateName)
    {
        var newId = ItemInstanceID.Generate();
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
        
        Debug.Log($"✨ [AccountDataManager] 신규 아이템 등록: {templateName} (ID: {newId.Value})");
        return newId;
    }
    
    /// <summary>
    /// 아이템 인스턴스 조회
    /// </summary>
    public ItemInstanceData GetInstance(ItemInstanceID id)
    {
        if (instanceCache.TryGetValue(id, out var instance))
            return instance;
        
        Debug.LogWarning($"⚠️ [AccountDataManager] 아이템 인스턴스를 찾을 수 없음: {id.Value}");
        return null;
    }
    
    /// <summary>
    /// ⭐ Stage 3: ItemInstanceData를 EquipmentInstance로 변환 (동적 스탯 복원)
    /// 장비 착용, 툴팁 표시 등에서 사용
    /// </summary>
    /// <param name="id">아이템 인스턴스 ID</param>
    /// <returns>동적 스탯이 복원된 EquipmentInstance (없으면 null)</returns>
    public EquipmentInstance CreateEquipmentInstance(ItemInstanceID id)
    {
        ItemInstanceData instanceData = GetInstance(id);
        if (instanceData == null)
        {
            Debug.LogWarning($"[AccountDataManager] ItemInstanceData를 찾을 수 없음: {id.Value}");
            return null;
        }
        
        // EquipmentData 로드
        EquipmentData equipData = ItemTemplateResolver.Load(instanceData.templateName);
        if (equipData == null)
        {
            Debug.LogError($"[AccountDataManager] EquipmentData를 찾을 수 없음: {instanceData.templateName}");
            return null;
        }
        
        // EquipmentInstance 생성
        var bindInfo = GetBindInfo(id);
        EquipmentInstance instance = new EquipmentInstance(
            id,
            equipData,
            instanceData.enhancementLevel,
            bindInfo.isBound
        );
        
        // ⭐ 동적 스탯 복원 (ItemInstanceData → EquipmentInstance)
        EquipmentInstanceConverter.RestoreDynamicStats(instance, instanceData);
        
        Debug.Log($"✅ [AccountDataManager] EquipmentInstance 생성 완료: {equipData.equipmentName} (주옵션: {instance.finalMainStatValue}, 부옵션: {instance.randomSubStats.Count}개)");
        return instance;
    }
    
    /// <summary>
    /// 아이템 인스턴스 삭제 (분해/합성 재료로 사용)
    /// </summary>
    public void RemoveInstance(ItemInstanceID id)
    {
        // 모든 컨테이너에서 제거
        bool wasInShared = accountData.sharedInventoryIds.Remove(id);
        accountData.mailboxIds.Remove(id);
        accountData.itemInstances.RemoveAll(i => i.instanceId == id);
        accountData.binds.RemoveAll(b => b.instanceId == id);
        
        instanceCache.Remove(id);
        bindCache.Remove(id);
        
        Debug.Log($"🗑️ [AccountDataManager] 아이템 인스턴스 삭제: {id.Value}");
        
        // ⭐ 이벤트 발생: 공유 창고에 있던 아이템이면 변경 이벤트 발생
        if (wasInShared)
        {
            OnSharedInventoryChanged?.Invoke();
        }
    }
    
    /// <summary>
    /// ⭐ 새로운 아이템 인스턴스 생성 (상점 구매, 드롭 등)
    /// </summary>
    public ItemInstanceID CreateInstance(string templateName, int enhancementLevel = 0)
    {
        // 새 Instance ID 생성
        var newInstanceId = ItemInstanceID.Generate();
        
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
        
        Debug.Log($"✨ [AccountDataManager] 새 아이템 인스턴스 생성: {templateName} (ID: {newInstanceId.Value.Substring(0, 8)}..., 강화: +{enhancementLevel})");
        
        return newInstanceId;
    }
    
    // ========================================
    // 공유 창고 관리
    // ========================================
    
    /// <summary>
    /// 계정 공유 창고에 추가
    /// </summary>
    public bool TryAddToShared(ItemInstanceID id, int? maxSize = null)
    {
        // ✅ maxSize가 지정되지 않으면 AccountData의 maxSharedInventorySize 사용
        int actualMaxSize = maxSize ?? accountData.maxSharedInventorySize;
        
        // 중복 체크
        if (accountData.sharedInventoryIds.Contains(id))
        {
            Debug.LogWarning($"⚠️ [AccountDataManager] 이미 창고에 존재하는 아이템: {id.Value}");
            return false;
        }
        
        // 크기 체크
        if (accountData.sharedInventoryIds.Count >= actualMaxSize)
        {
            Debug.LogWarning($"⚠️ [AccountDataManager] 창고가 가득 참! ({accountData.sharedInventoryIds.Count}/{actualMaxSize}개)");
            return false;
        }
        
        accountData.sharedInventoryIds.Add(id);
        
        // ⭐ 이벤트 발생: 공유 창고 변경됨
        Debug.Log($"🔔 [AccountDataManager] OnSharedInventoryChanged 이벤트 발생! (아이템 추가: {id.Value})");
        OnSharedInventoryChanged?.Invoke();
        
        return true;
    }
    
    /// <summary>
    /// 창고에서 제거
    /// </summary>
    public bool RemoveFromShared(ItemInstanceID id)
    {
        bool removed = accountData.sharedInventoryIds.Remove(id);
        if (removed)
        {
            Debug.Log($"🗑️ [AccountDataManager] 창고 제거: {id.Value}");
            
            // ⭐ 이벤트 발생: 공유 창고 변경됨
            OnSharedInventoryChanged?.Invoke();
        }
        return removed;
    }
    
    /// <summary>
    /// ⭐ 우편함으로 이동 (창고 꽉 찼을 때)
    /// 중복 방지 + shared에서 제거하지 않음
    /// </summary>
    public bool MoveToMailbox(ItemInstanceID id)
    {
        // 중복 체크 (이미 mailbox에 있으면 추가 안 함)
        if (accountData.mailboxIds.Contains(id))
        {
            Debug.LogWarning($"⚠️ [AccountDataManager] 이미 우편함에 존재하는 아이템: {id.Value}");
            return false;
        }
        
        // ❌ shared에서 제거하지 않음! (다른 곳에 있을 수도 있음)
        
        // ✅ 단순히 mailbox에 추가만
        accountData.mailboxIds.Add(id);
        Debug.Log($"📬 [AccountDataManager] 우편함으로 이동: {id.Value}");
        return true;
    }
    
    /// <summary>
    /// 우편함에서 제거
    /// </summary>
    public bool RemoveFromMailbox(ItemInstanceID id)
    {
        bool removed = accountData.mailboxIds.Remove(id);
        if (removed)
            Debug.Log($"📬 [AccountDataManager] 우편함 제거: {id.Value}");
        return removed;
    }
    
    // ========================================
    // 귀속 관리
    // ========================================
    
    /// <summary>
    /// 아이템 귀속 설정
    /// </summary>
    public void SetBind(ItemInstanceID id, int characterSlotIndex)
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
        
        Debug.Log($"🔒 [AccountDataManager] 아이템 귀속: {id.Value} → Slot {characterSlotIndex}");
    }
    
    /// <summary>
    /// 귀속 여부 확인
    /// </summary>
    public bool IsBound(ItemInstanceID id)
    {
        return bindCache.ContainsKey(id);
    }
    
    /// <summary>
    /// 다른 캐릭터에게 귀속되었는지 확인
    /// </summary>
    public bool IsBoundToOther(ItemInstanceID id, int characterSlotIndex)
    {
        if (bindCache.TryGetValue(id, out int boundSlot))
            return boundSlot != characterSlotIndex;
        return false;
    }
    
    /// <summary>
    /// 귀속 정보 조회
    /// </summary>
    public (bool isBound, int characterSlotIndex) GetBindInfo(ItemInstanceID id)
    {
        if (bindCache.TryGetValue(id, out int slotIndex))
            return (true, slotIndex);
        return (false, -1);
    }
    
    /// <summary>
    /// 귀속 해제 (분해 시 사용)
    /// </summary>
    public void RemoveBind(ItemInstanceID id)
    {
        accountData.binds.RemoveAll(b => b.instanceId == id);
        bindCache.Remove(id);
        Debug.Log($"🔓 [AccountDataManager] 귀속 해제: {id.Value}");
    }
    
    // ========================================
    // 재료 관리 (MaterialType enum 기반)
    // ========================================
    
    /// <summary>
    /// 재료 변경 이벤트
    /// </summary>
    public event System.Action<MaterialType, int> OnMaterialChanged;
    
    /// <summary>
    /// 공유 창고 인벤토리 변경 이벤트
    /// (아이템 추가/제거/수정 시 발생)
    /// </summary>
    public event System.Action OnSharedInventoryChanged;
    
    /// <summary>
    /// 재료 추가
    /// </summary>
    public void AddMaterial(MaterialType materialType, int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"⚠️ [AccountDataManager] 잘못된 수량: {materialType} +{amount}");
            return;
        }
        
        // ❌ 골드는 materials에 저장하면 안 됨! AccountData.gold 필드 사용!
        if (materialType == MaterialType.Gold)
        {
            Debug.LogError($"❌ [AccountDataManager] 골드는 AddMaterial()이 아닌 AddGold()를 사용하세요! AddGold({amount}) 호출을 권장합니다.");
            Debug.LogError($"   골드의 트루 소스는 AccountData.gold 필드입니다. materials 리스트에 저장하지 마세요!");
            return;
        }
        
        if (materialCache.TryGetValue(materialType, out int currentCount))
        {
            // 기존 스택 증가
            currentCount += amount;
            materialCache[materialType] = currentCount;
            
            var stack = accountData.materials.Find(m => m.materialType == materialType);
            if (stack != null)
            {
                stack.count = currentCount;
            }
        }
        else
        {
            // 신규 재료
            var stack = new MaterialStack { materialType = materialType, count = amount };
            accountData.materials.Add(stack);
            materialCache[materialType] = amount;
        }
        
        Debug.Log($"🎁 [AccountDataManager] 재료 추가: {materialType.GetDisplayName()} +{amount} (총: {materialCache[materialType]}개)");
        
        // 이벤트 발생
        OnMaterialChanged?.Invoke(materialType, materialCache[materialType]);
    }
    
    /// <summary>
    /// 재료 제거 (소모)
    /// </summary>
    public bool RemoveMaterial(MaterialType materialType, int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"⚠️ [AccountDataManager] 잘못된 수량: {materialType} -{amount}");
            return false;
        }
        
        if (!materialCache.TryGetValue(materialType, out int currentCount))
        {
            Debug.LogWarning($"⚠️ [AccountDataManager] 재료 없음: {materialType.GetDisplayName()}");
            return false;
        }
        
        if (currentCount < amount)
        {
            Debug.LogWarning($"⚠️ [AccountDataManager] 재료 부족: {materialType.GetDisplayName()} (필요: {amount}, 보유: {currentCount})");
            return false;
        }
        
        currentCount -= amount;
        materialCache[materialType] = currentCount;
        
        var stack = accountData.materials.Find(m => m.materialType == materialType);
        if (stack != null)
        {
            stack.count = currentCount;
            
            // 0개가 되면 제거
            if (currentCount == 0)
            {
                accountData.materials.Remove(stack);
                materialCache.Remove(materialType);
            }
        }
        
        Debug.Log($"🎁 [AccountDataManager] 재료 소모: {materialType.GetDisplayName()} -{amount} (남은: {currentCount}개)");
        
        // 이벤트 발생
        OnMaterialChanged?.Invoke(materialType, currentCount);
        
        return true;
    }
    
    /// <summary>
    /// 재료 보유량 조회
    /// </summary>
    public int GetMaterialCount(MaterialType materialType)
    {
        return materialCache.TryGetValue(materialType, out int count) ? count : 0;
    }
    
    /// <summary>
    /// 재료가 충분한지 확인
    /// </summary>
    public bool HasMaterial(MaterialType materialType, int requiredAmount)
    {
        return GetMaterialCount(materialType) >= requiredAmount;
    }
    
    /// <summary>
    /// 모든 재료 가져오기 (정렬됨)
    /// </summary>
    public List<MaterialStack> GetAllMaterials()
    {
        // MaterialType 순서대로 정렬
        var sorted = new List<MaterialStack>(accountData.materials);
        sorted.Sort((a, b) => a.materialType.CompareTo(b.materialType));
        return sorted;
    }
    
    /// <summary>
    /// UI 표시용 재료 리스트 (0개 제외)
    /// </summary>
    public List<MaterialStack> GetMaterialsForDisplay()
    {
        var result = new List<MaterialStack>();
        
        foreach (var mat in accountData.materials)
        {
            if (mat.count > 0)
            {
                result.Add(mat);
            }
        }
        
        // MaterialType 순서대로 정렬
        result.Sort((a, b) => a.materialType.CompareTo(b.materialType));
        
        return result;
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
    /// Account.json 파일 전체 경로 반환
    /// </summary>
    public string GetSavePath()
    {
        if (storage is JsonFileStorage jsonStorage)
        {
            string basePath = jsonStorage.GetBasePath();
            return System.IO.Path.Combine(basePath, ACCOUNT_SAVE_KEY + ".json");
        }
        return "Unknown";
    }
    
    /// <summary>
    /// 통계 정보 출력
    /// </summary>
    public void PrintStats()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"📊 [AccountDataManager] 통계");
        Debug.Log($"   - 💰 골드 (AccountData.gold): {accountData.gold:N0}원 ⭐ 트루 소스!");
        Debug.Log($"   - 아이템 인스턴스: {accountData.itemInstances.Count}개");
        Debug.Log($"   - 공유 창고: {accountData.sharedInventoryIds.Count}개");
        Debug.Log($"   - 우편함: {accountData.mailboxIds.Count}개");
        Debug.Log($"   - 귀속 정보: {accountData.binds.Count}개");
        Debug.Log($"   - 재료: {accountData.materials.Count}종류");
        foreach (var mat in accountData.materials)
        {
            Debug.Log($"     - {mat.GetDisplayName()}: {mat.count}개 [{mat.materialType}]");
        }
        Debug.Log("═══════════════════════════════════════════════════════");
    }
    
    #region 데이터 정합성 검증 시스템 (Phase 2)
    
    /// <summary>
    /// 📊 데이터 정합성 검증 결과
    /// </summary>
    public class ValidationResult
    {
        public List<ItemInstanceID> orphanedItems = new List<ItemInstanceID>();      // 고아 아이템 (itemInstances에만 존재)
        public List<ItemInstanceID> invalidReferences = new List<ItemInstanceID>();  // 무효 참조 (sharedInventoryIds/mailboxIds에만 존재)
        public List<ItemBindRecord> invalidBinds = new List<ItemBindRecord>();       // 무효 귀속 정보
        public Dictionary<string, int> duplicateInstances = new Dictionary<string, int>(); // 중복 Instance (templateName → 개수)
        public List<MaterialType> invalidMaterials = new List<MaterialType>();        // ❌ 잘못 저장된 재료 (Gold 등)
        public int totalIssues => orphanedItems.Count + invalidReferences.Count + invalidBinds.Count + duplicateInstances.Count + invalidMaterials.Count;
        
        public bool IsValid => totalIssues == 0;
        public bool HasIssues => !IsValid;
    }
    
    /// <summary>
    /// 🔍 데이터 정합성 검증
    /// </summary>
    public ValidationResult ValidateDataIntegrity()
    {
        Debug.Log("🔍 [AccountDataManager] 데이터 정합성 검증 시작...");
        
        var result = new ValidationResult();
        
        // 1. 고아 아이템 찾기 (itemInstances에만 존재)
        result.orphanedItems = FindOrphanedItems();
        
        // 2. 무효 참조 찾기 (sharedInventoryIds/mailboxIds에만 존재)
        result.invalidReferences = FindInvalidReferences();
        
        // 3. 무효 귀속 정보 찾기
        result.invalidBinds = FindInvalidBinds();
        
        // 4. 중복 Instance 찾기
        result.duplicateInstances = FindDuplicateInstances();
        
        // 5. ❌ 잘못 저장된 재료 찾기 (Gold 등)
        result.invalidMaterials = FindInvalidMaterials();
        
        // 결과 출력
        Debug.Log($"📊 [검증 결과]");
        Debug.Log($"   - 고아 아이템: {result.orphanedItems.Count}개");
        Debug.Log($"   - ❌ 무효 참조: {result.invalidReferences.Count}개");
        Debug.Log($"   - 무효 귀속 정보: {result.invalidBinds.Count}개");
        Debug.Log($"   - 중복 템플릿: {result.duplicateInstances.Count}개");
        Debug.Log($"   - ❌ 잘못된 재료: {result.invalidMaterials.Count}개");
        
        if (result.IsValid)
        {
            Debug.Log($"✅ [AccountDataManager] 데이터 정합성 검증 완료 - 문제 없음");
        }
        else
        {
            Debug.LogWarning($"⚠️ [AccountDataManager] 데이터 정합성 문제 발견: {result.totalIssues}개");
        }
        
        return result;
    }
    
    /// <summary>
    /// 🧹 고아 아이템 찾기
    /// - itemInstances에는 있지만 어디에도 참조되지 않는 아이템
    /// </summary>
    private List<ItemInstanceID> FindOrphanedItems()
    {
        var orphaned = new List<ItemInstanceID>();
        
        // 모든 유효한 참조 수집
        var validRefs = new HashSet<ItemInstanceID>();
        validRefs.UnionWith(accountData.sharedInventoryIds);
        validRefs.UnionWith(accountData.mailboxIds);
        
        // PlayerDataManager에서 모든 캐릭터의 가방 아이템 수집
        if (PlayerDataManager.Instance != null)
        {
            for (int i = 0; i < 3; i++) // 최대 3개 슬롯
            {
                var slotData = PlayerDataManager.Instance.GetSlotData(i);
                if (slotData != null && slotData.characterBagInstanceIds != null)
                {
                    validRefs.UnionWith(slotData.characterBagInstanceIds);
                }
            }
        }
        
        // itemInstances에서 참조되지 않는 아이템 찾기
        foreach (var instance in accountData.itemInstances)
        {
            if (!validRefs.Contains(instance.instanceId))
            {
                orphaned.Add(instance.instanceId);
                
                Debug.Log($"   🗑️ 고아 아이템 발견: {instance.templateName} (ID: {instance.instanceId.Value.Substring(0, 8)}...)");
            }
        }
        
        return orphaned;
    }
    
    /// <summary>
    /// ❌ 무효 참조 찾기
    /// - sharedInventoryIds/mailboxIds/characterBag에는 있지만 itemInstances에 없는 ID
    /// </summary>
    private List<ItemInstanceID> FindInvalidReferences()
    {
        var invalidRefs = new List<ItemInstanceID>();
        
        // itemInstances의 모든 유효한 ID 수집
        var validInstanceIds = new HashSet<ItemInstanceID>();
        foreach (var instance in accountData.itemInstances)
        {
            validInstanceIds.Add(instance.instanceId);
        }
        
        // 1. sharedInventoryIds 체크
        foreach (var id in accountData.sharedInventoryIds)
        {
            if (!validInstanceIds.Contains(id))
            {
                invalidRefs.Add(id);
                Debug.LogError($"   ❌ 무효 참조 발견 (공유 창고): ID={id.Value.Substring(0, 8)}... (itemInstances에 없음!)");
            }
        }
        
        // 2. mailboxIds 체크
        foreach (var id in accountData.mailboxIds)
        {
            if (!validInstanceIds.Contains(id))
            {
                invalidRefs.Add(id);
                Debug.LogError($"   ❌ 무효 참조 발견 (우편함): ID={id.Value.Substring(0, 8)}... (itemInstances에 없음!)");
            }
        }
        
        // 3. characterBagInstanceIds 체크
        if (PlayerDataManager.Instance != null)
        {
            for (int i = 0; i < 3; i++)
            {
                var slotData = PlayerDataManager.Instance.GetSlotData(i);
                if (slotData != null && slotData.characterBagInstanceIds != null)
                {
                    foreach (var id in slotData.characterBagInstanceIds)
                    {
                        if (!validInstanceIds.Contains(id))
                        {
                            invalidRefs.Add(id);
                            Debug.LogError($"   ❌ 무효 참조 발견 (캐릭터 가방 슬롯{i}): ID={id.Value.Substring(0, 8)}... (itemInstances에 없음!)");
                        }
                    }
                }
            }
        }
        
        return invalidRefs;
    }
    
    /// <summary>
    /// 🔒 무효 귀속 정보 찾기
    /// - 삭제된 캐릭터의 귀속 정보
    /// - 존재하지 않는 아이템의 귀속 정보
    /// </summary>
    private List<ItemBindRecord> FindInvalidBinds()
    {
        var invalid = new List<ItemBindRecord>();
        
        // 유효한 캐릭터 슬롯 확인
        var validSlots = new HashSet<int>();
        if (PlayerDataManager.Instance != null)
        {
            for (int i = 0; i < 3; i++)
            {
                var slotData = PlayerDataManager.Instance.GetSlotData(i);
                if (slotData != null && !string.IsNullOrEmpty(slotData.playerName))
                {
                    validSlots.Add(i);
                }
            }
        }
        
        // 유효한 아이템 확인
        var validItems = new HashSet<ItemInstanceID>();
        foreach (var instance in accountData.itemInstances)
        {
            validItems.Add(instance.instanceId);
        }
        
        // 무효 귀속 정보 찾기
        foreach (var bind in accountData.binds)
        {
            bool isInvalid = false;
            string reason = "";
            
            // 1. 삭제된 캐릭터 체크
            if (!validSlots.Contains(bind.characterSlotIndex))
            {
                isInvalid = true;
                reason = $"삭제된 캐릭터 (슬롯 {bind.characterSlotIndex})";
            }
            // 2. 존재하지 않는 아이템 체크
            else if (!validItems.Contains(bind.instanceId))
            {
                isInvalid = true;
                reason = "존재하지 않는 아이템";
            }
            
            if (isInvalid)
            {
                invalid.Add(bind);
                Debug.Log($"   🔒 무효 귀속 정보: 슬롯{bind.characterSlotIndex}, ID:{bind.instanceId.Value.Substring(0, 8)}... ({reason})");
            }
        }
        
        return invalid;
    }
    
    /// <summary>
    /// 📦 중복 Instance 찾기 (같은 템플릿의 여러 Instance)
    /// - 상점 전시용 아이템 중복 감지
    /// </summary>
    private Dictionary<string, int> FindDuplicateInstances()
    {
        var duplicates = new Dictionary<string, int>();
        
        // 모든 유효한 참조 수집 (고아 아이템 제외)
        var validRefs = new HashSet<ItemInstanceID>();
        validRefs.UnionWith(accountData.sharedInventoryIds);
        validRefs.UnionWith(accountData.mailboxIds);
        
        if (PlayerDataManager.Instance != null)
        {
            for (int i = 0; i < 3; i++)
            {
                var slotData = PlayerDataManager.Instance.GetSlotData(i);
                if (slotData != null && slotData.characterBagInstanceIds != null)
                {
                    validRefs.UnionWith(slotData.characterBagInstanceIds);
                }
            }
        }
        
        // 템플릿별 그룹화 (유효한 참조만)
        var templateGroups = accountData.itemInstances
            .Where(i => validRefs.Contains(i.instanceId))
            .GroupBy(i => i.templateName)
            .Where(g => g.Count() > 1) // 2개 이상만
            .ToList();
        
        foreach (var group in templateGroups)
        {
            duplicates[group.Key] = group.Count();
            Debug.Log($"   📦 중복 템플릿: {group.Key} ({group.Count()}개)");
        }
        
        return duplicates;
    }
    
    /// <summary>
    /// ❌ 잘못 저장된 재료 찾기
    /// - materials 리스트에 저장되면 안 되는 타입 (예: Gold)
    /// </summary>
    private List<MaterialType> FindInvalidMaterials()
    {
        var invalid = new List<MaterialType>();
        
        foreach (var mat in accountData.materials)
        {
            // ❌ Gold는 materials에 저장되면 안 됨!
            if (mat.materialType == MaterialType.Gold)
            {
                invalid.Add(mat.materialType);
                Debug.LogError($"   ❌ 잘못된 재료 발견: {mat.materialType.GetDisplayName()} (count: {mat.count})");
                Debug.LogError($"      → 골드는 AccountData.gold 필드에만 저장되어야 합니다!");
            }
        }
        
        return invalid;
    }
    
    #endregion
    
    #region 자동 정리 시스템 (Phase 3)
    
    /// <summary>
    /// 🧹 자동 정리 실행 (게임 시작 시 호출)
    /// </summary>
    public void AutoCleanup()
    {
        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log("🧹 [AccountDataManager] 자동 정리 시작...");
        Debug.Log("═══════════════════════════════════════════════════════");
        
        int totalCleaned = 0;
        
        // 1. 데이터 정합성 검증
        var validation = ValidateDataIntegrity();
        
        if (validation.IsValid)
        {
            Debug.Log("✅ [AutoCleanup] 데이터 정합성 문제 없음 - 정리 불필요");
            Debug.Log("═══════════════════════════════════════════════════════");
            return;
        }
        
        Debug.Log($"\n⚠️ [AutoCleanup] 정합성 문제 발견 - 정리 시작...");
        Debug.Log($"   - 고아 아이템: {validation.orphanedItems.Count}개");
        Debug.Log($"   - ❌ 무효 참조: {validation.invalidReferences.Count}개");
        Debug.Log($"   - 무효 귀속 정보: {validation.invalidBinds.Count}개");
        Debug.Log($"   - ❌ 잘못된 재료: {validation.invalidMaterials.Count}개");
        
        // 2. 고아 아이템 제거
        if (validation.orphanedItems.Count > 0)
        {
            int removed = RemoveOrphanedItems(validation.orphanedItems);
            totalCleaned += removed;
            Debug.Log($"   ✅ 고아 아이템 제거: {removed}개");
        }
        
        // 3. ❌ 무효 참조 제거
        if (validation.invalidReferences.Count > 0)
        {
            int removed = RemoveInvalidReferences(validation.invalidReferences);
            totalCleaned += removed;
            Debug.Log($"   ✅ 무효 참조 제거: {removed}개");
        }
        
        // 4. 무효 귀속 정보 제거
        if (validation.invalidBinds.Count > 0)
        {
            int removed = RemoveInvalidBinds(validation.invalidBinds);
            totalCleaned += removed;
            Debug.Log($"   ✅ 무효 귀속 정보 제거: {removed}개");
        }
        
        // 5. ❌ 잘못된 재료 제거
        if (validation.invalidMaterials.Count > 0)
        {
            int removed = RemoveInvalidMaterials(validation.invalidMaterials);
            totalCleaned += removed;
            Debug.Log($"   ✅ 잘못된 재료 제거: {removed}개");
        }
        
        // 6. 저장
        if (totalCleaned > 0)
        {
            Save();
            
            Debug.Log($"\n✅ [AutoCleanup] 자동 정리 완료!");
            Debug.Log($"   - 총 정리된 항목: {totalCleaned}개");
            PrintStats();
        }
        
        Debug.Log("═══════════════════════════════════════════════════════");
    }
    
    /// <summary>
    /// 🗑️ 고아 아이템 제거
    /// </summary>
    private int RemoveOrphanedItems(List<ItemInstanceID> orphanedIds)
    {
        int removed = 0;
        
        foreach (var orphanedId in orphanedIds)
        {
            // itemInstances에서 제거
            var instance = accountData.itemInstances.Find(i => i.instanceId == orphanedId);
            if (instance != null)
            {
                accountData.itemInstances.Remove(instance);
                removed++;
                
                Debug.Log($"      🗑️ 제거: {instance.templateName} (ID: {orphanedId.Value.Substring(0, 8)}...)");
            }
            
            // 캐시에서도 제거
            if (instanceCache.ContainsKey(orphanedId))
            {
                instanceCache.Remove(orphanedId);
            }
        }
        
        return removed;
    }
    
    /// <summary>
    /// ❌ 무효 참조 제거
    /// - sharedInventoryIds/mailboxIds/characterBag에서 invalid ID 제거
    /// </summary>
    private int RemoveInvalidReferences(List<ItemInstanceID> invalidRefs)
    {
        int removed = 0;
        
        foreach (var invalidId in invalidRefs)
        {
            // 1. sharedInventoryIds에서 제거
            if (accountData.sharedInventoryIds.Remove(invalidId))
            {
                removed++;
                Debug.Log($"      ❌ 제거 (공유 창고): ID={invalidId.Value.Substring(0, 8)}...");
            }
            
            // 2. mailboxIds에서 제거
            if (accountData.mailboxIds.Remove(invalidId))
            {
                removed++;
                Debug.Log($"      ❌ 제거 (우편함): ID={invalidId.Value.Substring(0, 8)}...");
            }
            
            // 3. characterBagInstanceIds에서 제거
            if (PlayerDataManager.Instance != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var slotData = PlayerDataManager.Instance.GetSlotData(i);
                    if (slotData != null && slotData.characterBagInstanceIds != null)
                    {
                        if (slotData.characterBagInstanceIds.Remove(invalidId))
                        {
                            removed++;
                            Debug.Log($"      ❌ 제거 (캐릭터 가방 슬롯{i}): ID={invalidId.Value.Substring(0, 8)}...");
                        }
                    }
                }
            }
        }
        
        return removed;
    }
    
    /// <summary>
    /// 🔒 무효 귀속 정보 제거
    /// </summary>
    private int RemoveInvalidBinds(List<ItemBindRecord> invalidBinds)
    {
        int removed = 0;
        
        foreach (var bind in invalidBinds)
        {
            if (accountData.binds.Remove(bind))
            {
                removed++;
                Debug.Log($"      🔒 제거: 슬롯{bind.characterSlotIndex}, ID:{bind.instanceId.Value.Substring(0, 8)}...");
            }
            
            // 캐시에서도 제거
            if (bindCache.ContainsKey(bind.instanceId))
            {
                bindCache.Remove(bind.instanceId);
            }
        }
        
        return removed;
    }
    
    /// <summary>
    /// ❌ 잘못 저장된 재료 제거
    /// - materials 리스트에서 Gold 등 잘못된 타입 제거
    /// </summary>
    private int RemoveInvalidMaterials(List<MaterialType> invalidMaterials)
    {
        int removed = 0;
        
        foreach (var matType in invalidMaterials)
        {
            // materials 리스트에서 제거
            var stack = accountData.materials.Find(m => m.materialType == matType);
            if (stack != null && accountData.materials.Remove(stack))
            {
                removed++;
                Debug.Log($"      ❌ 제거: {matType.GetDisplayName()} (count: {stack.count})");
            }
            
            // 캐시에서도 제거
            if (materialCache.ContainsKey(matType))
            {
                materialCache.Remove(matType);
            }
        }
        
        return removed;
    }
    
    #endregion
    
    /// <summary>
    /// 🧹 상점 전시용 고아 아이템 제거 (Legacy 정리)
    /// - 과거에 저장된 상점 전시용 아이템 제거
    /// - 원칙: 상점 전시용 아이템은 메모리 전용, AccountData 저장 안 함
    /// </summary>
    public int CleanupLegacyShopItems()
    {
        Debug.Log("🧹 [AccountDataManager] Legacy 상점 아이템 정리 시작...");
        
        int beforeCount = accountData.itemInstances.Count;
        
        // 1. 보관창고/우편함/캐릭터 가방에 있는 Instance ID 수집 (보호 대상)
        var protectedIds = new HashSet<ItemInstanceID>();
        protectedIds.UnionWith(accountData.sharedInventoryIds);
        protectedIds.UnionWith(accountData.mailboxIds);
        
        if (PlayerDataManager.Instance != null)
        {
            for (int i = 0; i < 3; i++)
            {
                var slotData = PlayerDataManager.Instance.GetSlotData(i);
                if (slotData != null && slotData.characterBagInstanceIds != null)
                {
                    protectedIds.UnionWith(slotData.characterBagInstanceIds);
                }
            }
        }
        
        Debug.Log($"🔒 [CleanupLegacyShopItems] 보호 대상 ID: {protectedIds.Count}개");
        
        // 2. D/C/B/A 등급 상점 장비 템플릿 로드
        var shopTemplates = new HashSet<string>();
        EquipmentData[] allEquipments = Resources.LoadAll<EquipmentData>("Equipment");
        foreach (var eq in allEquipments)
        {
            if ((eq.itemGrade == ItemGrade.D || eq.itemGrade == ItemGrade.C || 
                 eq.itemGrade == ItemGrade.B || eq.itemGrade == ItemGrade.A) &&
                (eq.equipmentType == EquipmentType.Weapon || eq.equipmentType == EquipmentType.Armor))
            {
                shopTemplates.Add(eq.itemID);
            }
        }
        
        Debug.Log($"📦 [CleanupLegacyShopItems] 상점 템플릿: {shopTemplates.Count}개");
        
        // 3. 상점 템플릿이면서 보호 대상이 아닌 아이템 = Legacy 상점 전시용 아이템
        var legacyShopItems = accountData.itemInstances
            .Where(i => shopTemplates.Contains(i.templateName) && !protectedIds.Contains(i.instanceId))
            .ToList();
        
        Debug.Log($"🗑️ [CleanupLegacyShopItems] Legacy 상점 아이템 발견: {legacyShopItems.Count}개");
        
        // 4. 제거
        int removed = 0;
        foreach (var item in legacyShopItems)
        {
            accountData.itemInstances.Remove(item);
            removed++;
            
            // 캐시에서도 제거
            if (instanceCache.ContainsKey(item.instanceId))
            {
                instanceCache.Remove(item.instanceId);
            }
            
            if (removed <= 10) // 처음 10개만 로그 출력
            {
                Debug.Log($"   🗑️ 제거: {item.templateName} (ID: {item.instanceId.Value.Substring(0, 8)}...)");
            }
        }
        
        if (removed > 10)
        {
            Debug.Log($"   ... 외 {removed - 10}개 더");
        }
        
        int afterCount = accountData.itemInstances.Count;
        
        // 5. 저장
        if (removed > 0)
        {
            Save();
            Debug.Log($"✅ [CleanupLegacyShopItems] 정리 완료!");
            Debug.Log($"   - 이전: {beforeCount}개");
            Debug.Log($"   - 이후: {afterCount}개");
            Debug.Log($"   - 제거: {removed}개");
        }
        else
        {
            Debug.Log($"✅ [CleanupLegacyShopItems] Legacy 상점 아이템 없음 (정상)");
        }
        
        return removed;
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
        accountData.skills.Clear();
        
        instanceCache.Clear();
        bindCache.Clear();
        materialCache.Clear();
        
        Debug.Log("🧹 [AccountDataManager] 모든 계정 데이터 초기화 완료");
    }
    
    // ========================================
    // 📚 스킬 시스템 (Phase 3-Revision)
    // ========================================
    
    /// <summary>
    /// 보유 액티브 스킬 목록 반환
    /// </summary>
    public List<SkillInstance> GetUnlockedActiveSkills()
    {
        List<SkillInstance> activeSkills = new List<SkillInstance>();
        
        foreach (var saveData in accountData.skills)
        {
            SkillInstance skill = saveData.ToSkillInstance();
            if (skill != null && skill.IsActiveSkill)
            {
                activeSkills.Add(skill);
            }
        }
        
        return activeSkills;
    }
    
    /// <summary>
    /// 보유 패시브 스킬 목록 반환
    /// </summary>
    public List<SkillInstance> GetUnlockedPassiveSkills()
    {
        List<SkillInstance> passiveSkills = new List<SkillInstance>();
        
        foreach (var saveData in accountData.skills)
        {
            SkillInstance skill = saveData.ToSkillInstance();
            if (skill != null && skill.IsPassiveSkill)
            {
                passiveSkills.Add(skill);
            }
        }
        
        return passiveSkills;
    }
    
    /// <summary>
    /// 장착된 액티브 스킬 반환
    /// </summary>
    public SkillInstance GetEquippedActiveSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= accountData.equippedActiveSkillIds.Length)
            return null;
        
        string skillID = accountData.equippedActiveSkillIds[slotIndex];
        if (string.IsNullOrEmpty(skillID)) return null;
        
        return FindSkillByID(skillID);
    }
    
    /// <summary>
    /// 장착된 패시브 스킬 반환
    /// </summary>
    public SkillInstance GetEquippedPassiveSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= accountData.equippedPassiveSkillIds.Length)
            return null;
        
        string skillID = accountData.equippedPassiveSkillIds[slotIndex];
        if (string.IsNullOrEmpty(skillID)) return null;
        
        return FindSkillByID(skillID);
    }
    
    /// <summary>
    /// 스킬 ID로 SkillInstance 찾기
    /// </summary>
    private SkillInstance FindSkillByID(string skillID)
    {
        var saveData = accountData.skills.Find(s => s.skillID == skillID);
        return saveData?.ToSkillInstance();
    }
    
    /// <summary>
    /// 스킬 레벨업 시도
    /// </summary>
    public bool TryUpgradeSkill(SkillInstance skill, int playerLevel)
    {
        if (skill == null || skill.skillData == null) return false;
        
        // ① 해금 조건
        if (playerLevel < skill.skillData.unlockLevel)
        {
            Debug.LogWarning($"🔒 [{skill.skillData.skillName}] 해금 레벨 부족! (요구: Lv.{skill.skillData.unlockLevel}, 현재: Lv.{playerLevel})");
            return false;
        }
        
        // ② 만렙 조건
        if (skill.IsMaxLevel)
        {
            Debug.LogWarning($"⚠️ [{skill.skillData.skillName}] 이미 최대 레벨입니다! (Lv.{skill.skillData.maxLevel})");
            return false;
        }
        
        // ③ SP 조건
        int requiredSP = skill.GetRequiredSPForNextLevel();
        int availableSP = accountData.totalSP - accountData.usedSP;
        
        if (availableSP < requiredSP)
        {
            Debug.LogWarning($"💎 [{skill.skillData.skillName}] SP 부족! (필요: {requiredSP}, 보유: {availableSP})");
            return false;
        }
        
        // 레벨업 실행
        skill.currentLevel++;
        accountData.usedSP += requiredSP;
        
        // 저장 데이터 업데이트
        UpdateSkillSaveData(skill);
        Save();
        
        Debug.Log($"✅ [{skill.skillData.skillName}] 레벨업 성공!");
        Debug.Log($"   Lv.{skill.currentLevel - 1} → Lv.{skill.currentLevel}");
        Debug.Log($"   소모 SP: {requiredSP}");
        Debug.Log($"   SP 현황: {availableSP - requiredSP}/{accountData.totalSP} (사용: {accountData.usedSP})");
        
        return true;
    }
    
    /// <summary>
    /// 액티브 스킬 장착
    /// </summary>
    public bool EquipActiveSkill(SkillInstance skill, int slotIndex)
    {
        if (skill == null || !skill.IsActiveSkill) return false;
        if (slotIndex < 0 || slotIndex >= accountData.equippedActiveSkillIds.Length) return false;
        
        // 기존 장착 스킬 해제
        string oldSkillID = accountData.equippedActiveSkillIds[slotIndex];
        if (!string.IsNullOrEmpty(oldSkillID))
        {
            UnequipSkillByID(oldSkillID);
        }
        
        // 새 스킬 장착
        skill.isEquipped = true;
        accountData.equippedActiveSkillIds[slotIndex] = skill.skillData.skillID;
        
        // 저장 데이터 업데이트
        UpdateSkillSaveData(skill);
        Save();
        
        Debug.Log($"🎯 [{skill.skillData.skillName}] 액티브 슬롯 {slotIndex}에 장착됨");
        return true;
    }
    
    /// <summary>
    /// 패시브 스킬 장착
    /// </summary>
    public bool EquipPassiveSkill(SkillInstance skill, int slotIndex)
    {
        if (skill == null || !skill.IsPassiveSkill) return false;
        if (slotIndex < 0 || slotIndex >= accountData.equippedPassiveSkillIds.Length) return false;
        
        // 기존 장착 스킬 해제
        string oldSkillID = accountData.equippedPassiveSkillIds[slotIndex];
        if (!string.IsNullOrEmpty(oldSkillID))
        {
            UnequipSkillByID(oldSkillID);
        }
        
        // 새 스킬 장착
        skill.isEquipped = true;
        accountData.equippedPassiveSkillIds[slotIndex] = skill.skillData.skillID;
        
        // 저장 데이터 업데이트
        UpdateSkillSaveData(skill);
        
        // 패시브 스킬은 실시간 스탯 재계산 필요
        // (로비에서는 PlayerRuntimeStats가 없으므로 인게임 진입 시 동기화)
        
        Save();
        
        Debug.Log($"🛡️ [{skill.skillData.skillName}] 패시브 슬롯 {slotIndex}에 장착됨");
        return true;
    }
    
    /// <summary>
    /// 스킬 장착 해제
    /// </summary>
    public void UnequipSkill(SkillInstance skill)
    {
        if (skill == null) return;
        
        skill.isEquipped = false;
        
        // 슬롯에서 제거
        if (skill.IsActiveSkill)
        {
            for (int i = 0; i < accountData.equippedActiveSkillIds.Length; i++)
            {
                if (accountData.equippedActiveSkillIds[i] == skill.skillData.skillID)
                {
                    accountData.equippedActiveSkillIds[i] = null;
                }
            }
        }
        else if (skill.IsPassiveSkill)
        {
            for (int i = 0; i < accountData.equippedPassiveSkillIds.Length; i++)
            {
                if (accountData.equippedPassiveSkillIds[i] == skill.skillData.skillID)
                {
                    accountData.equippedPassiveSkillIds[i] = null;
                }
            }
        }
        
        // 저장 데이터 업데이트
        UpdateSkillSaveData(skill);
        Save();
    }
    
    /// <summary>
    /// skillID로 장착 해제
    /// </summary>
    private void UnequipSkillByID(string skillID)
    {
        var saveData = accountData.skills.Find(s => s.skillID == skillID);
        if (saveData != null)
        {
            saveData.isEquipped = false;
        }
    }
    
    /// <summary>
    /// 스킬 저장 데이터 업데이트
    /// </summary>
    private void UpdateSkillSaveData(SkillInstance skill)
    {
        if (skill == null || skill.skillData == null) return;
        
        var saveData = accountData.skills.Find(s => s.skillID == skill.skillData.skillID);
        
        if (saveData != null)
        {
            // 기존 데이터 업데이트
            saveData.currentLevel = skill.currentLevel;
            saveData.isEquipped = skill.isEquipped;
        }
        else
        {
            // 새 데이터 추가
            accountData.skills.Add(SkillInstanceSaveData.FromSkillInstance(skill));
        }
    }
    
    /// <summary>
    /// 빈 액티브 슬롯 찾기
    /// </summary>
    public int FindEmptyActiveSlot()
    {
        for (int i = 0; i < accountData.equippedActiveSkillIds.Length; i++)
        {
            if (string.IsNullOrEmpty(accountData.equippedActiveSkillIds[i]))
                return i;
        }
        return -1;
    }
    
    /// <summary>
    /// 빈 패시브 슬롯 찾기
    /// </summary>
    public int FindEmptyPassiveSlot()
    {
        for (int i = 0; i < accountData.equippedPassiveSkillIds.Length; i++)
        {
            if (string.IsNullOrEmpty(accountData.equippedPassiveSkillIds[i]))
                return i;
        }
        return -1;
    }
    
    /// <summary>
    /// SP 여유 확인
    /// </summary>
    public bool CanAffordSP(int amount)
    {
        int availableSP = accountData.totalSP - accountData.usedSP;
        return availableSP >= amount;
    }
    
    /// <summary>
    /// 총 SP 반환
    /// </summary>
    public int GetTotalSP()
    {
        return accountData.totalSP;
    }
    
    /// <summary>
    /// 사용한 SP 반환
    /// </summary>
    public int GetUsedSP()
    {
        return accountData.usedSP;
    }
    
    /// <summary>
    /// 현재 플레이어 레벨 반환
    /// </summary>
    public int GetCurrentPlayerLevel()
    {
        return accountData.currentPlayerLevel;
    }
    
    /// <summary>
    /// SP 추가 (레벨업 시)
    /// </summary>
    public void AddSP(int amount)
    {
        accountData.totalSP += amount;
        Save();
        
        Debug.Log($"💎 SP +{amount} 획득! (총: {accountData.totalSP})");
    }
    
    /// <summary>
    /// 플레이어 레벨 설정 (테스트용)
    /// </summary>
    public void SetPlayerLevel(int level)
    {
        accountData.currentPlayerLevel = level;
        Save();
    }
    
    /// <summary>
    /// 스킬 추가 (신규 해금)
    /// </summary>
    public void AddSkill(BaseSkillData skillData)
    {
        if (skillData == null) return;
        
        // 이미 보유 중인지 확인
        var existing = accountData.skills.Find(s => s.skillID == skillData.skillID);
        if (existing != null)
        {
            Debug.LogWarning($"⚠️ [{skillData.skillName}] 이미 보유 중입니다.");
            return;
        }
        
        // 새 스킬 추가
        var newSkill = new SkillInstance(skillData)
        {
            currentLevel = 1, // 해금 시 Lv.1
            isEquipped = false
        };
        
        accountData.skills.Add(SkillInstanceSaveData.FromSkillInstance(newSkill));
        Save();
        
        Debug.Log($"✨ [{skillData.skillName}] 스킬 해금!");
    }
}

