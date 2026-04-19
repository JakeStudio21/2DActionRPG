using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 계정 단위 저장 데이터 (순수 DTO)
/// ⭐ 캐시 로직은 AccountDataManager로 이동
/// JSON 직렬화 가능한 List 기반 설계
/// </summary>
[System.Serializable]
public class AccountData
{
    [Header("💰 계정 공유 재화")]
    [Tooltip("모든 캐릭터가 공유하는 골드 (V2 시스템)")]
    public int gold = 0;
    
    [Header("🎒 계정 공유 창고")]
    [Tooltip("모든 캐릭터가 공유하는 창고 (기본 128칸, 8x16 그리드)")]
    public List<ItemInstanceID> sharedInventoryIds = new List<ItemInstanceID>();
    
    [Tooltip("보관창고 최대 크기 (확장 가능: 128 → 256)")]
    public int maxSharedInventorySize = 128; // 기본 128칸 (8열 x 16행)
    
    [Tooltip("현재 공유 창고 아이템 개수 (JSON 가독성용, 자동 생성)")]
    public int currentSharedInventoryCount = 0; // sharedInventoryIds.Count
    
    [Header("📬 우편함 (창고 넘침 처리)")]
    [Tooltip("창고가 가득 찼을 때 임시 보관 공간")]
    public List<ItemInstanceID> mailboxIds = new List<ItemInstanceID>();
    
    [Header("📦 아이템 인스턴스 메타데이터")]
    [Tooltip("모든 아이템 인스턴스의 실제 데이터 (강화, 커스텀 이름 등)")]
    public List<ItemInstanceData> itemInstances = new List<ItemInstanceData>();
    
    [Tooltip("현재 아이템 인스턴스 개수 (JSON 가독성용, 자동 생성)")]
    public int currentItemInstancesCount = 0; // itemInstances.Count
    
    [Header("🔒 귀속 정보")]
    [Tooltip("캐릭터별 아이템 귀속 정보")]
    public List<ItemBindRecord> binds = new List<ItemBindRecord>();
    
    [Header("🎁 재료 재화")]
    [Tooltip("강화파편, 정령석 등 스택 가능한 재료")]
    public List<MaterialStack> materials = new List<MaterialStack>();
    
    [Header("⚡ 스태미나 (계정 공유)")]
    [Tooltip("일반 스테이지 입장 재화 (최대 50)")]
    public int currentStamina = 50;
    
    [Tooltip("마지막 스태미나 회복 시간 (yyyy-MM-dd HH:mm:ss 형식)\n" +
             "스태미나가 MAX 미만으로 떨어질 때 기록 시작, MAX 도달 시 빈 문자열로 초기화")]
    public string lastStaminaUpdateTime = "";
    
    [Header("🏰 던전 카테고리별 입장 제한 (계정 공유)")]
    [Tooltip("BM으로 구매한 추가 던전 티켓")]
    public int dailyDungeonTickets = 0;
    
    [Tooltip("카테고리별 입장 기록 (날짜별 플레이 횟수)\n" +
             "예: '정령의 가호 던전' 카테고리 전체 3회 제한")]
    public List<CategoryEntryData> dungeonCategoryEntries = new List<CategoryEntryData>();
    
    [Header("📚 스킬 & 룬 시스템 (Phase 3) - ⚠️ DEPRECATED")]
    [System.Obsolete("Phase 3.5: 스킬 데이터는 PlayerSlotData로 이동됨. 마이그레이션 후 제거 예정.")]
    [Tooltip("⚠️ DEPRECATED: PlayerSlotData.skills 사용")]
    public List<SkillInstanceSaveData> skills = new List<SkillInstanceSaveData>();
    
    [System.Obsolete("Phase 3.5: 스킬 데이터는 PlayerSlotData로 이동됨. 마이그레이션 후 제거 예정.")]
    [Tooltip("⚠️ DEPRECATED: PlayerSlotData.equippedActiveSkillIds 사용")]
    public string[] equippedActiveSkillIds = new string[2];
    
    [System.Obsolete("Phase 3.5: 스킬 데이터는 PlayerSlotData로 이동됨. 마이그레이션 후 제거 예정.")]
    [Tooltip("⚠️ DEPRECATED: PlayerSlotData.equippedPassiveSkillIds 사용")]
    public string[] equippedPassiveSkillIds = new string[3];
    
    [System.Obsolete("Phase 3.5: SP는 PlayerSlotData로 이동됨. PlayerSlotData.totalSP 사용.")]
    [Tooltip("⚠️ DEPRECATED: PlayerSlotData.totalSP 사용")]
    public int totalSP = 0;
    
    [System.Obsolete("Phase 3.5: SP는 PlayerSlotData로 이동됨. PlayerSlotData.usedSP 사용.")]
    [Tooltip("⚠️ DEPRECATED: PlayerSlotData.usedSP 사용")]
    public int usedSP = 0;
    
    [System.Obsolete("Phase 3.5: PlayerSlotData.level 사용")]
    [Tooltip("⚠️ DEPRECATED: PlayerSlotData.level 사용")]
    public int currentPlayerLevel = 1;
    
    // ❌ 캐시 필드는 AccountDataManager로 이동
    // [System.NonSerialized] private Dictionary<ItemInstanceID, ItemInstanceData> _instanceCache;
    
    /// <summary>
    /// 📦 보관창고 확장 가능 여부 확인
    /// </summary>
    public bool CanExpandInventory()
    {
        return maxSharedInventorySize < 256;
    }
    
    /// <summary>
    /// 📦 다음 확장 크기 가져오기
    /// </summary>
    public int GetNextExpansionSize()
    {
        if (maxSharedInventorySize == 128) return 256;  // 128 → 256 (8x32)
        return maxSharedInventorySize; // 이미 최대
    }
    
    /// <summary>
    /// 💰 확장 비용 가져오기 (골드)
    /// </summary>
    public int GetExpansionCost()
    {
        if (maxSharedInventorySize == 128) return 100000; // 128→256: 100,000 골드
        return 0; // 이미 최대
    }
    
    /// <summary>
    /// 📦 인벤토리 확장 실행
    /// </summary>
    public bool ExecuteExpansion()
    {
        if (!CanExpandInventory()) return false;
        
        maxSharedInventorySize = GetNextExpansionSize();
        return true;
    }
    
    /// <summary>
    /// 디버깅용 문자열 표현
    /// </summary>
    public override string ToString()
    {
        return $"AccountData: Items={itemInstances.Count}, Shared={sharedInventoryIds.Count}/{maxSharedInventorySize}(max256), Mailbox={mailboxIds.Count}, Binds={binds.Count}, Materials={materials.Count}, Stamina={currentStamina}/50";
    }
}

/// <summary>
/// 던전 카테고리별 입장 기록 데이터 (JsonUtility 호환)
/// </summary>
[System.Serializable]
public class CategoryEntryData
{
    [Tooltip("카테고리 ID (예: Daily_Boss_Dungeon)")]
    public string categoryId = "";
    
    [Tooltip("마지막 플레이 날짜 (yyyy-MM-dd 형식)")]
    public string lastPlayedDate = "";
    
    [Tooltip("오늘 카테고리 전체 플레이 횟수 (0~3)\n" +
             "예: 정령의 가호 던전 4개 합쳐서 3회 제한")]
    public int dailyPlayCount = 0;
    
    public CategoryEntryData()
    {
    }
    
    public CategoryEntryData(string categoryId)
    {
        this.categoryId = categoryId;
        this.lastPlayedDate = System.DateTime.Now.ToString("yyyy-MM-dd");
        this.dailyPlayCount = 0;
    }
}

