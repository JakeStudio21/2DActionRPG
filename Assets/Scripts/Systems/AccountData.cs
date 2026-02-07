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
    [Tooltip("모든 캐릭터가 공유하는 창고 (기본 64칸, 8x8 그리드)")]
    public List<ItemInstanceId> sharedInventoryIds = new List<ItemInstanceId>();
    
    [Tooltip("보관창고 최대 크기 (확장 가능: 64 → 128 → 256)")]
    public int maxSharedInventorySize = 64; // 기본 64칸 (8열 x 8행)
    
    [Header("📬 우편함 (창고 넘침 처리)")]
    [Tooltip("창고가 가득 찼을 때 임시 보관 공간")]
    public List<ItemInstanceId> mailboxIds = new List<ItemInstanceId>();
    
    [Header("📦 아이템 인스턴스 메타데이터")]
    [Tooltip("모든 아이템 인스턴스의 실제 데이터 (강화, 커스텀 이름 등)")]
    public List<ItemInstanceData> itemInstances = new List<ItemInstanceData>();
    
    [Header("🔒 귀속 정보")]
    [Tooltip("캐릭터별 아이템 귀속 정보")]
    public List<ItemBindRecord> binds = new List<ItemBindRecord>();
    
    [Header("🎁 재료 재화")]
    [Tooltip("강화파편, 정령석 등 스택 가능한 재료")]
    public List<MaterialStack> materials = new List<MaterialStack>();
    
    // ❌ 캐시 필드는 AccountDataManager로 이동
    // [System.NonSerialized] private Dictionary<ItemInstanceId, ItemInstanceData> _instanceCache;
    
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
        if (maxSharedInventorySize == 64) return 128;   // 64 → 128 (8x16)
        if (maxSharedInventorySize == 128) return 256;  // 128 → 256 (8x32)
        return maxSharedInventorySize; // 이미 최대
    }
    
    /// <summary>
    /// 💰 확장 비용 가져오기 (골드)
    /// </summary>
    public int GetExpansionCost()
    {
        if (maxSharedInventorySize == 64) return 20000;   // 64→128: 20,000 골드
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
        return $"AccountData: Items={itemInstances.Count}, Shared={sharedInventoryIds.Count}/{maxSharedInventorySize}, Mailbox={mailboxIds.Count}, Binds={binds.Count}, Materials={materials.Count}";
    }
}

