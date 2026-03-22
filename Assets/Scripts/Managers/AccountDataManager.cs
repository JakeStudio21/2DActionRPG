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
    /// 아이템을 세상에서 영구 소멸시키는 단일 진출구(Exit Point).
    ///
    /// ✅ 사용 시점: 상점 판매, 강화/합성 소모재, 귀속 아이템 해제 삭제 등
    ///              아이템 생명주기가 완전히 끝날 때 반드시 이 메서드를 호출해야 함.
    /// ❌ 잘못된 사용: 단순 이동(장착, 창고 ↔ 가방)에는 호출하지 말 것.
    ///
    /// AutoCleanup(GC)에 의존하지 않고 즉각적인 삭제를 보장한다.
    /// Save() 호출은 호출자 책임 — 다중 소모 시 모든 제거 후 1회만 Save 가능하도록 분리.
    ///
    /// 수행 작업:
    ///   ① itemInstances 원본 데이터 즉시 제거
    ///   ② 영구 컨테이너(sharedInventoryIds, mailboxIds)에서 참조 제거
    ///   ③ 귀속 정보(binds) 제거
    ///   ④ instanceCache / bindCache 정리
    /// </summary>
    public void DestroyItemInstance(ItemInstanceID id)
    {
        if (id.IsEmpty)
        {
            Debug.LogWarning("⚠️ [AccountDataManager] DestroyItemInstance: 빈 ID 무시");
            return;
        }

        bool wasInShared = accountData.sharedInventoryIds.Remove(id);
        accountData.mailboxIds.Remove(id);
        accountData.itemInstances.RemoveAll(i => i.instanceId == id);
        accountData.binds.RemoveAll(b => b.instanceId == id);

        instanceCache.Remove(id);
        bindCache.Remove(id);

        Debug.Log($"💀 [AccountDataManager] 아이템 영구 소멸: {id.Value.Substring(0, 8)}...");

        if (wasInShared)
            OnSharedInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 아이템 인스턴스 삭제 (분해/합성 재료로 사용)
    /// 내부적으로 DestroyItemInstance()에 위임.
    /// </summary>
    public void RemoveInstance(ItemInstanceID id) => DestroyItemInstance(id);
    
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
    /// 공유 창고에 해당 ID가 존재하는지 확인 (읽기 전용)
    /// </summary>
    public bool IsInSharedInventory(ItemInstanceID id) =>
        !id.IsEmpty && accountData.sharedInventoryIds.Contains(id);

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
    
    #region 데이터 정합성 검증 및 자동 정리 시스템

    // ─────────────────────────────────────────────────────────────────────
    // IItemContainer 수집 헬퍼
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 모든 아이템 참조 컨테이너를 (컨테이너, 슬롯인덱스) 쌍으로 수집.
    /// slotIndex=-1 이면 AccountData 소속 컨테이너.
    /// includeVolatile=true 이면 CharacterBag(휘발성) 포함, false 이면 영구 컨테이너만.
    /// </summary>
    private List<(IItemContainer container, int slotIndex)> CollectAllContainers(bool includeVolatile)
    {
        var result = new List<(IItemContainer, int)>
        {
            (new SharedInventoryContainer(accountData.sharedInventoryIds), -1),
            (new MailboxContainer(accountData.mailboxIds), -1)
        };

        if (PlayerDataManager.Instance != null)
        {
            for (int i = 0; i < 3; i++)
            {
                var slotData = PlayerDataManager.Instance.GetSlotData(i);
                if (slotData == null) continue;

                result.Add((new EquippedRecordsContainer(slotData.equippedRecords), i));
                if (includeVolatile)
                    result.Add((new CharacterBagContainer(slotData.characterBagInstanceIds), i));
            }
        }

        return result;
    }

    /// <summary>
    /// itemInstances에 등록된 유효한 instanceId 문자열 집합을 반환.
    /// </summary>
    private HashSet<string> BuildValidInstanceIdSet()
    {
        var ids = new HashSet<string>();
        foreach (var inst in accountData.itemInstances)
            if (!inst.instanceId.IsEmpty)
                ids.Add(inst.instanceId.Value);
        return ids;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 공개 검증 API (디버깅 / 에디터 도구용)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 📊 데이터 정합성 검증 결과
    /// </summary>
    public class ValidationResult
    {
        public List<ItemInstanceID> orphanedItems     = new List<ItemInstanceID>();
        public List<ItemInstanceID> invalidReferences = new List<ItemInstanceID>();
        public List<ItemBindRecord> invalidBinds       = new List<ItemBindRecord>();
        public Dictionary<string, int> duplicateInstances = new Dictionary<string, int>();
        public List<MaterialType> invalidMaterials    = new List<MaterialType>();

        public int totalIssues => orphanedItems.Count + invalidReferences.Count
                                  + invalidBinds.Count + duplicateInstances.Count
                                  + invalidMaterials.Count;
        public bool IsValid  => totalIssues == 0;
        public bool HasIssues => !IsValid;
    }

    /// <summary>
    /// 🔍 데이터 정합성 검증 (읽기 전용 - 실제 제거는 AutoCleanup에서 수행)
    /// </summary>
    public ValidationResult ValidateDataIntegrity()
    {
        Debug.Log("🔍 [AccountDataManager] 데이터 정합성 검증 시작...");

        var result = new ValidationResult
        {
            orphanedItems     = FindOrphanedItems(),
            invalidReferences = FindInvalidReferences(),
            invalidBinds      = FindInvalidBinds(),
            duplicateInstances= FindDuplicateInstances(),
            invalidMaterials  = FindInvalidMaterials()
        };

        Debug.Log($"📊 [검증 결과] 고아:{result.orphanedItems.Count} / 무효참조:{result.invalidReferences.Count} / 귀속:{result.invalidBinds.Count} / 재료:{result.invalidMaterials.Count}");

        if (result.IsValid)
            Debug.Log("✅ [AccountDataManager] 데이터 정합성 문제 없음");
        else
            Debug.LogWarning($"⚠️ [AccountDataManager] 정합성 문제 {result.totalIssues}건 발견");

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 탐지 메서드 (Find*)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 어떤 컨테이너에서도 참조되지 않는 itemInstances 항목 탐지.
    /// equippedRecords, characterBag 포함한 전체 컨테이너를 기준으로 검사.
    /// </summary>
    private List<ItemInstanceID> FindOrphanedItems()
    {
        var allContainers = CollectAllContainers(includeVolatile: true);
        var allReferenced = new HashSet<string>();
        foreach (var (container, _) in allContainers)
            foreach (var id in container.GetContainedItemIds())
                allReferenced.Add(id);

        var orphaned = new List<ItemInstanceID>();
        foreach (var inst in accountData.itemInstances)
        {
            if (!allReferenced.Contains(inst.instanceId.Value))
            {
                orphaned.Add(inst.instanceId);
                Debug.Log($"   🗑️ 고아 아이템: {inst.templateName} ({inst.instanceId.Value.Substring(0, 8)}...)");
            }
        }
        return orphaned;
    }

    /// <summary>
    /// 모든 컨테이너(equippedRecords 포함) 중 itemInstances에 없는 Dead Reference 탐지.
    /// </summary>
    private List<ItemInstanceID> FindInvalidReferences()
    {
        var validIds   = BuildValidInstanceIdSet();
        var invalidRefs = new List<ItemInstanceID>();

        void CheckList(IEnumerable<ItemInstanceID> list, string label)
        {
            foreach (var id in list)
            {
                if (!id.IsEmpty && !validIds.Contains(id.Value))
                {
                    invalidRefs.Add(id);
                    Debug.LogError($"   ❌ Dead Reference ({label}): ID={id.Value.Substring(0, 8)}...");
                }
            }
        }

        CheckList(accountData.sharedInventoryIds, "공유 창고");
        CheckList(accountData.mailboxIds,          "우편함");

        if (PlayerDataManager.Instance != null)
        {
            for (int i = 0; i < 3; i++)
            {
                var slotData = PlayerDataManager.Instance.GetSlotData(i);
                if (slotData == null) continue;

                // 장착 슬롯 Dead Reference 검사 (신규)
                foreach (var rec in slotData.equippedRecords)
                    if (!rec.instanceId.IsEmpty && !validIds.Contains(rec.instanceId.Value))
                    {
                        invalidRefs.Add(rec.instanceId);
                        Debug.LogError($"   ❌ Dead Reference (equippedRecords 슬롯{i}): ID={rec.instanceId.Value.Substring(0, 8)}...");
                    }

                CheckList(slotData.characterBagInstanceIds, $"캐릭터 가방 슬롯{i}");
            }
        }

        return invalidRefs;
    }

    /// <summary>
    /// 무효 귀속 정보 탐지 (삭제된 캐릭터 / 존재하지 않는 아이템)
    /// </summary>
    private List<ItemBindRecord> FindInvalidBinds()
    {
        var validSlots = new HashSet<int>();
        if (PlayerDataManager.Instance != null)
            for (int i = 0; i < 3; i++)
            {
                var sd = PlayerDataManager.Instance.GetSlotData(i);
                if (sd != null && !string.IsNullOrEmpty(sd.playerName))
                    validSlots.Add(i);
            }

        var validItems = BuildValidInstanceIdSet();
        var invalid    = new List<ItemBindRecord>();

        foreach (var bind in accountData.binds)
        {
            string reason = null;
            if (!validSlots.Contains(bind.characterSlotIndex))
                reason = $"삭제된 캐릭터 (슬롯 {bind.characterSlotIndex})";
            else if (!validItems.Contains(bind.instanceId.Value))
                reason = "존재하지 않는 아이템";

            if (reason != null)
            {
                invalid.Add(bind);
                Debug.Log($"   🔒 무효 귀속: 슬롯{bind.characterSlotIndex} / {bind.instanceId.Value.Substring(0, 8)}... ({reason})");
            }
        }
        return invalid;
    }

    /// <summary>
    /// 중복 instanceId 탐지 (전체 컨테이너 기준)
    /// </summary>
    private Dictionary<string, int> FindDuplicateInstances()
    {
        var allContainers = CollectAllContainers(includeVolatile: true);
        var allReferenced = new HashSet<string>();
        foreach (var (container, _) in allContainers)
            foreach (var id in container.GetContainedItemIds())
                allReferenced.Add(id);

        var duplicates = new Dictionary<string, int>();
        var templateGroups = accountData.itemInstances
            .Where(inst => allReferenced.Contains(inst.instanceId.Value))
            .GroupBy(inst => inst.templateName)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var g in templateGroups)
        {
            duplicates[g.Key] = g.Count();
            Debug.Log($"   📦 중복 템플릿: {g.Key} ({g.Count()}개)");
        }
        return duplicates;
    }

    /// <summary>잘못 저장된 재료 탐지 (Gold 등)</summary>
    private List<MaterialType> FindInvalidMaterials()
    {
        var invalid = new List<MaterialType>();
        foreach (var mat in accountData.materials)
            if (mat.materialType == MaterialType.Gold)
            {
                invalid.Add(mat.materialType);
                Debug.LogError($"   ❌ 잘못된 재료: {mat.materialType.GetDisplayName()} (count:{mat.count}) → AccountData.gold 사용해야 함");
            }
        return invalid;
    }

    // ─────────────────────────────────────────────────────────────────────
    // AutoCleanup - 3-Pass 양방향 정리
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 🧹 자동 정리 실행 (게임 시작 시 호출)
    ///
    /// PASS 0 │ Crash Recovery Transfer — 가방 잔여 아이템/재료 이관
    ///        │ ① characterBagInstanceIds (장비) → sharedInventoryIds / mailboxIds
    ///        │ ② characterBagMaterials   (재료) → AccountData.materials 합산
    ///        │ (이관 후 가방 Clear → 항상 실행, Dry-Run 영향 없음)
    ///        ↓
    /// PASS A │ Dead Reference 정리 — 영구 컨테이너 전용 (하향식)
    ///        │ 공유 창고 / 우편함 / equippedRecords에서 itemInstances에 없는 ID 제거
    ///        │ ※ characterBagInstanceIds 제외: PASS 0 이후 비어있어야 정상
    ///        ↓
    /// PASS B │ 중복 해소 (Sequential Save 보완)
    ///        │ 영구 컨테이너에 이미 있는 가방 ID 제거 (PASS 0 후 잔존 중복 처리)
    ///        ↓
    /// PASS C │ Orphaned Item GC (상향식)
    ///        │ 가방 포함 전체 컨테이너 기준으로 미참조 itemInstances 제거
    ///
    /// [안전장치]
    ///   - Guard   : PlayerDataManager.IsLoaded = false 상태에서 실행 차단
    ///   - Dry-Run : DRY_RUN = true 시 PASS A/C 실제 삭제 없이 경고 로그만 출력
    ///               → 플레이 테스트 모니터링 후 false로 전환
    /// </summary>
    public void AutoCleanup()
    {
        // ── Guard: 슬롯 데이터 미로드 상태에서 GC 실행 차단 ─────────
        if (PlayerDataManager.Instance != null && !PlayerDataManager.Instance.IsLoaded)
        {
            Debug.LogError("🚫 [AutoCleanup] PlayerDataManager 슬롯 미로드 상태 - 실행 거부! (아이템 영구 삭제 방지)");
            return;
        }

        // ── Dry-Run 플래그 ──────────────────────────────────────────
        // true  → PASS A/C 삭제 대상 로그 출력만. 실제 Remove/RemoveAll 안 함 (모니터링 모드)
        // false → 실제 삭제 실행 (정식 운영 모드)
        // ※ PASS 0, PASS B 는 항상 실제 실행 (데이터 이관/복구이므로 Dry-Run 불필요)
        const bool DRY_RUN = true;

        Debug.Log("═══════════════════════════════════════════════════════");
        Debug.Log($"🧹 [AutoCleanup] 시작... (DRY_RUN={DRY_RUN})");
        Debug.Log("═══════════════════════════════════════════════════════");

        // ── 카운터 분리 ─────────────────────────────────────────────
        // crashCount  : PASS 0/B — 항상 실제 실행, DRY_RUN 여부와 무관하게 반드시 저장
        // dryRunCount : PASS A/C — DRY_RUN = true 시 실제 삭제 없이 로그만 출력
        int crashCount  = 0;
        int dryRunCount = 0;
        bool accountDataChanged = false;
        var dirtySlotIndices = new HashSet<int>();

        // ── PASS 0: Crash Recovery Transfer ─────────────────────────
        int crashRecovered = RecoverCrashBagItems(dirtySlotIndices, ref accountDataChanged);
        crashCount += crashRecovered;

        // ── PASS A: Dead Reference 정리 ──────────────────────────────
        var validInstanceIds = BuildValidInstanceIdSet();
        int deadRefs = CleanDeadReferences(validInstanceIds, dirtySlotIndices, ref accountDataChanged, DRY_RUN);
        dryRunCount += deadRefs;

        // ── PASS B: 중복 해소 ─────────────────────────────────────────
        int dupes = CleanDuplicateVolatileItems(dirtySlotIndices);
        crashCount += dupes;

        // ── PASS C: Orphaned Item GC ─────────────────────────────────
        var allContainers = CollectAllContainers(includeVolatile: true);
        int orphans = CleanOrphanedItems(allContainers, DRY_RUN);
        dryRunCount += orphans;
        if (orphans > 0 && !DRY_RUN) accountDataChanged = true;

        // ── 기존 유지: 귀속 정보 / 재료 정리 (Dry-Run 적용 안 함) ──
        var invalidBinds = FindInvalidBinds();
        if (invalidBinds.Count > 0)
        {
            crashCount += RemoveInvalidBinds(invalidBinds);
            accountDataChanged = true;
        }

        var invalidMaterials = FindInvalidMaterials();
        if (invalidMaterials.Count > 0)
        {
            crashCount += RemoveInvalidMaterials(invalidMaterials);
            accountDataChanged = true;
        }

        // ── 저장 ────────────────────────────────────────────────────
        // PASS 0/B(crashCount)의 변경 사항은 DRY_RUN 여부와 무관하게 반드시 저장.
        // PASS A/C(dryRunCount)는 DRY_RUN = true 시 저장 없이 로그만 출력.
        bool hasRealChanges = crashCount > 0;
        bool hasDryRunFindings = DRY_RUN && dryRunCount > 0;

        if (!hasRealChanges && !hasDryRunFindings)
        {
            Debug.Log("✅ [AutoCleanup] 문제 없음 - 정리 불필요");
            Debug.Log("═══════════════════════════════════════════════════════");
            return;
        }

        // 크래시 복구 실제 변경 사항 저장 (DRY_RUN 관계 없이 항상 실행)
        if (hasRealChanges)
        {
            if (accountDataChanged)
                Save();

            foreach (int slotIdx in dirtySlotIndices)
            {
                var sd = PlayerDataManager.Instance?.GetSlotData(slotIdx);
                if (sd != null) PlayerDataManager.Instance.SaveSlotData(sd);
            }

            Debug.Log($"✅ [AutoCleanup] Crash Recovery 완료 - {crashCount}개 복구/정리 저장됨");
        }

        // Dry-Run 감지 결과 로그 출력 (실제 삭제 없음)
        if (hasDryRunFindings)
        {
            Debug.LogWarning($"🔍 [AutoCleanup] DRY-RUN 감지 - {dryRunCount}개 삭제 대상 (실제 삭제는 실행되지 않음)");
            Debug.LogWarning("   → 로그 확인 후 이상 없으면 DRY_RUN = false 로 전환하세요.");
        }

        PrintStats();
        Debug.Log("═══════════════════════════════════════════════════════");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Pass 0: Crash Recovery Transfer — 가방 잔여 아이템 창고 이관
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 비정상 종료(Crash) 복구:
    ///   ① characterBagInstanceIds (장비 아이템) → sharedInventoryIds / mailboxIds 이관
    ///   ② characterBagMaterials   (재료 아이템) → AccountData.materials 합산 이관
    ///
    /// 항상 실제 실행 (Dry-Run 비적용) — 이관/복구이므로 삭제 위험 없음.
    /// </summary>
    private int RecoverCrashBagItems(HashSet<int> dirtySlotIndices, ref bool accountDataChanged)
    {
        if (PlayerDataManager.Instance == null) return 0;

        int totalRecovered = 0;

        for (int i = 0; i < 3; i++)
        {
            var slotData = PlayerDataManager.Instance.GetSlotData(i);
            if (slotData == null) continue;

            // ── ① 장비 아이템 복구 (characterBagInstanceIds) ────────────
            if (slotData.characterBagInstanceIds != null && slotData.characterBagInstanceIds.Count > 0)
            {
                var bagSnapshot = new List<ItemInstanceID>(slotData.characterBagInstanceIds);
                int movedCount = 0;

                foreach (var itemId in bagSnapshot)
                {
                    if (itemId.IsEmpty) continue;

                    // 이미 창고 또는 우편함에 있으면 스킵 (중복 방지)
                    if (accountData.sharedInventoryIds.Contains(itemId) ||
                        accountData.mailboxIds.Contains(itemId)) continue;

                    if (TryAddToShared(itemId))
                        movedCount++;
                    else
                    {
                        MoveToMailbox(itemId);
                        movedCount++;
                        Debug.LogWarning($"   📬 [PASS 0] 창고 만석 → 우편함 이관: {itemId.Value.Substring(0, 8)}...");
                    }
                }

                if (movedCount > 0)
                {
                    slotData.characterBagInstanceIds.Clear();
                    dirtySlotIndices.Add(i);
                    accountDataChanged = true;
                    totalRecovered += movedCount;
                    Debug.LogWarning($"⚡ [AutoCleanup] 비정상 종료 감지: 장비 가방(슬롯{i})에 남은 아이템 {movedCount}개를 창고로 복구했습니다.");
                }
            }

            // ── ② 재료 아이템 복구 (characterBagMaterials) ──────────────
            // 재료는 MaterialType enum + count 구조로 ID가 없음.
            // AddMaterial()이 내부에서 기존 타입에 count 합산하므로 중복 처리 불필요.
            if (slotData.characterBagMaterials != null && slotData.characterBagMaterials.Count > 0)
            {
                int matCount = slotData.characterBagMaterials.Count;
                foreach (var mat in slotData.characterBagMaterials)
                {
                    if (mat.count <= 0) continue;
                    AddMaterial(mat.materialType, mat.count);
                    Debug.LogWarning($"   🪨 [PASS 0] 재료 복구: {mat.GetDisplayName()} x{mat.count} → AccountData.materials");
                }

                slotData.characterBagMaterials.Clear();
                dirtySlotIndices.Add(i);
                accountDataChanged = true;
                totalRecovered += matCount;
                Debug.LogWarning($"⚡ [AutoCleanup] 비정상 종료 감지: 재료 가방(슬롯{i})에 남은 재료 {matCount}종류를 계정으로 복구했습니다.");
            }
        }

        return totalRecovered;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Pass A: Dead Reference 정리 (영구 컨테이너 전용)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// itemInstances에 없는 ID를 영구 참조 컨테이너에서 제거.
    ///
    /// 대상: 공유 창고 / 우편함 / 장착 슬롯(equippedRecords)
    /// 제외: characterBagInstanceIds (휘발성 임시 버퍼)
    ///
    /// dryRun = true  → 삭제 대상 ID를 LogWarning으로 출력만 (실제 제거 없음)
    /// dryRun = false → 실제 RemoveAll 실행
    /// </summary>
    private int CleanDeadReferences(HashSet<string> validIds, HashSet<int> dirtySlotIndices, ref bool accountDataChanged, bool dryRun)
    {
        int detected = 0;

        // ── 영구 AccountData 컨테이너 ────────────────────────────────
        var deadInShared = accountData.sharedInventoryIds
            .FindAll(id => !id.IsEmpty && !validIds.Contains(id.Value));
        if (deadInShared.Count > 0)
        {
            foreach (var id in deadInShared)
                Debug.LogWarning($"   ⚠️ [PASS A] Dead Ref 감지 (공유 창고): {id.Value.Substring(0, 8)}...");
            if (!dryRun)
            {
                accountData.sharedInventoryIds.RemoveAll(id => !id.IsEmpty && !validIds.Contains(id.Value));
                accountDataChanged = true;
                Debug.Log($"   🧹 Dead Ref 제거 (공유 창고): {deadInShared.Count}개");
            }
            detected += deadInShared.Count;
        }

        var deadInMailbox = accountData.mailboxIds
            .FindAll(id => !id.IsEmpty && !validIds.Contains(id.Value));
        if (deadInMailbox.Count > 0)
        {
            foreach (var id in deadInMailbox)
                Debug.LogWarning($"   ⚠️ [PASS A] Dead Ref 감지 (우편함): {id.Value.Substring(0, 8)}...");
            if (!dryRun)
            {
                accountData.mailboxIds.RemoveAll(id => !id.IsEmpty && !validIds.Contains(id.Value));
                accountDataChanged = true;
                Debug.Log($"   🧹 Dead Ref 제거 (우편함): {deadInMailbox.Count}개");
            }
            detected += deadInMailbox.Count;
        }

        // ── 영구 PlayerSlotData 컨테이너: equippedRecords만 ─────────
        if (PlayerDataManager.Instance != null)
        {
            for (int i = 0; i < 3; i++)
            {
                var slotData = PlayerDataManager.Instance.GetSlotData(i);
                if (slotData == null) continue;

                var deadInEquipped = slotData.equippedRecords
                    .FindAll(r => !r.instanceId.IsEmpty && !validIds.Contains(r.instanceId.Value));
                if (deadInEquipped.Count > 0)
                {
                    foreach (var r in deadInEquipped)
                        Debug.LogWarning($"   ⚠️ [PASS A] Dead Ref 감지 (equippedRecords 슬롯{i}): {r.instanceId.Value.Substring(0, 8)}... slot={r.slot}");
                    if (!dryRun)
                    {
                        slotData.equippedRecords.RemoveAll(r => !r.instanceId.IsEmpty && !validIds.Contains(r.instanceId.Value));
                        dirtySlotIndices.Add(i);
                        Debug.LogWarning($"   🧹 Dead Ref 제거 (equippedRecords 슬롯{i}): {deadInEquipped.Count}개");
                    }
                    detected += deadInEquipped.Count;
                }

                // characterBagInstanceIds는 PASS A 대상에서 제외
                // → 가방은 휘발성 버퍼. 정리는 StageEndItemTransfer / PASS B에서 담당.
            }
        }

        return detected;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Pass B: Crash Recovery - 가방 중복 제거
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sequential Save 도중 크래시 발생 시 복구:
    /// AccountData.Save()는 성공(창고에 ID 추가됨)했으나 SlotData.Save()는 실패(가방에 ID 잔존)한
    /// 경우, 가방에 남아있는 중복 ID를 제거한다. 영구 컨테이너(창고/우편함)가 진실 공급원.
    /// </summary>
    private int CleanDuplicateVolatileItems(HashSet<int> dirtySlotIndices)
    {
        // 영구 컨테이너 ID 집합 구축
        var permanentIds = new HashSet<string>();
        foreach (var id in accountData.sharedInventoryIds)
            if (!id.IsEmpty) permanentIds.Add(id.Value);
        foreach (var id in accountData.mailboxIds)
            if (!id.IsEmpty) permanentIds.Add(id.Value);

        if (permanentIds.Count == 0 || PlayerDataManager.Instance == null)
            return 0;

        int removed = 0;
        for (int i = 0; i < 3; i++)
        {
            var slotData = PlayerDataManager.Instance.GetSlotData(i);
            if (slotData?.characterBagInstanceIds == null) continue;

            int before = slotData.characterBagInstanceIds.Count;
            slotData.characterBagInstanceIds.RemoveAll(id => !id.IsEmpty && permanentIds.Contains(id.Value));
            int delta = before - slotData.characterBagInstanceIds.Count;
            if (delta > 0)
            {
                removed += delta;
                dirtySlotIndices.Add(i);
                Debug.LogWarning($"   🔄 Crash Recovery - 가방 중복 ID 제거 (슬롯{i}): {delta}개");
            }
        }
        return removed;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Pass C: Orphaned Item GC
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 어떤 컨테이너(영구+휘발성)에서도 참조되지 않는 itemInstances 항목 제거.
    /// instanceCache도 동기화.
    ///
    /// dryRun = true  → 삭제 대상 ID를 LogWarning으로 출력만 (실제 제거 없음)
    /// dryRun = false → 실제 Remove 실행
    /// </summary>
    private int CleanOrphanedItems(List<(IItemContainer container, int slotIndex)> allContainers, bool dryRun)
    {
        var allReferenced = new HashSet<string>();
        foreach (var (container, _) in allContainers)
            foreach (var id in container.GetContainedItemIds())
                allReferenced.Add(id);

        var toRemove = accountData.itemInstances
            .FindAll(inst => !allReferenced.Contains(inst.instanceId.Value));

        if (toRemove.Count == 0) return 0;

        foreach (var inst in toRemove)
            Debug.LogWarning($"   ⚠️ [PASS C] Orphan 감지: {inst.templateName} ({inst.instanceId.Value.Substring(0, 8)}...)");

        if (!dryRun)
        {
            foreach (var inst in toRemove)
            {
                accountData.itemInstances.Remove(inst);
                instanceCache.Remove(inst.instanceId);
            }
            Debug.Log($"   🗑️ PASS C - Orphaned Items {toRemove.Count}개 제거");
        }

        return toRemove.Count;
    }

    // ─────────────────────────────────────────────────────────────────────
    // 기존 유지 메서드 (귀속 정보 / 재료 정리)
    // ─────────────────────────────────────────────────────────────────────

    private int RemoveInvalidBinds(List<ItemBindRecord> invalidBinds)
    {
        int removed = 0;
        foreach (var bind in invalidBinds)
        {
            if (accountData.binds.Remove(bind))
            {
                removed++;
                Debug.Log($"      🔒 귀속 제거: 슬롯{bind.characterSlotIndex} / {bind.instanceId.Value.Substring(0, 8)}...");
            }
            bindCache.Remove(bind.instanceId);
        }
        return removed;
    }

    private int RemoveInvalidMaterials(List<MaterialType> invalidMaterials)
    {
        int removed = 0;
        foreach (var matType in invalidMaterials)
        {
            var stack = accountData.materials.Find(m => m.materialType == matType);
            if (stack != null && accountData.materials.Remove(stack))
            {
                removed++;
                Debug.Log($"      ❌ 재료 제거: {matType.GetDisplayName()} (count:{stack.count})");
            }
            materialCache.Remove(matType);
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

