using UnityEngine;

/// <summary>
/// 장착 아이템 레코드 - 슬롯 위치와 instanceId 참조만 보유 (정규화)
/// 아이템의 실제 데이터(templateName 등)는 AccountData.itemInstances가 유일한 진실 공급원(SoT)
/// </summary>
[System.Serializable]
public class EquippedRecord
{
    [Tooltip("장비 슬롯")]
    public EquipmentSlot slot;

    [Tooltip("아이템 인스턴스 ID (AccountData.itemInstances의 키)")]
    public ItemInstanceID instanceId;

    public override string ToString() => $"[{slot}] → {instanceId}";
}
