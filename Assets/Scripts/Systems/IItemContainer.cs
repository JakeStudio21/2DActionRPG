using System.Collections.Generic;

/// <summary>
/// 아이템 instanceId를 보관하는 모든 참조 컨테이너의 공통 인터페이스
///
/// Architecture (Single Source of Truth):
///   - SoT        : AccountData.itemInstances  → 실제 아이템 데이터 보유
///   - Permanent  : SharedInventory, Mailbox, EquippedSlots → instanceId 영구 참조
///   - Volatile   : CharacterBag → 스테이지 중 임시 버퍼, 종료 시 Shared로 이관
///
/// 사용처: AccountDataManager.AutoCleanup()의 Dead Reference / Orphan GC
/// </summary>
public interface IItemContainer
{
    IEnumerable<string> GetContainedItemIds();
}

// ─────────────────────────────────────────────────────────────────────────────
// 영구 참조 컨테이너 구현체 (Permanent Containers)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>계정 공유 창고 (AccountData.sharedInventoryIds)</summary>
public class SharedInventoryContainer : IItemContainer
{
    private readonly List<ItemInstanceID> _ids;
    public SharedInventoryContainer(List<ItemInstanceID> ids) => _ids = ids;

    public IEnumerable<string> GetContainedItemIds()
    {
        foreach (var id in _ids)
            if (!id.IsEmpty) yield return id.Value;
    }
}

/// <summary>우편함 (AccountData.mailboxIds)</summary>
public class MailboxContainer : IItemContainer
{
    private readonly List<ItemInstanceID> _ids;
    public MailboxContainer(List<ItemInstanceID> ids) => _ids = ids;

    public IEnumerable<string> GetContainedItemIds()
    {
        foreach (var id in _ids)
            if (!id.IsEmpty) yield return id.Value;
    }
}

/// <summary>캐릭터 장착 슬롯 (PlayerSlotData.equippedRecords)</summary>
public class EquippedRecordsContainer : IItemContainer
{
    private readonly List<EquippedRecord> _records;
    public EquippedRecordsContainer(List<EquippedRecord> records) => _records = records;

    public IEnumerable<string> GetContainedItemIds()
    {
        foreach (var rec in _records)
            if (!rec.instanceId.IsEmpty) yield return rec.instanceId.Value;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// 휘발성 버퍼 컨테이너 (Volatile Buffer)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// 인게임 캐릭터 가방 (PlayerSlotData.characterBagInstanceIds)
/// 스테이지 중 임시 보관. 종료 시 SharedInventory로 이관되고 비워짐.
/// Orphan GC 계산에는 포함되지만, Dead Reference 정리 대상은 Permanent만.
/// </summary>
public class CharacterBagContainer : IItemContainer
{
    private readonly List<ItemInstanceID> _ids;
    public CharacterBagContainer(List<ItemInstanceID> ids) => _ids = ids;

    public IEnumerable<string> GetContainedItemIds()
    {
        foreach (var id in _ids)
            if (!id.IsEmpty) yield return id.Value;
    }
}
