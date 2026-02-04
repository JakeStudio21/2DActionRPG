using UnityEngine;

/// <summary>
/// 아이템 귀속 정보 레코드
/// 특정 캐릭터에게 귀속된 아이템 추적
/// </summary>
[System.Serializable]
public class ItemBindRecord
{
    [Tooltip("아이템 인스턴스 ID")]
    public ItemInstanceId instanceId;
    
    [Tooltip("귀속된 캐릭터 슬롯 인덱스 (0~2)")]
    public int characterSlotIndex;
    
    [Tooltip("귀속 시간 (yyyy-MM-dd HH:mm:ss)")]
    public string bindTime;
    
    /// <summary>
    /// 디버깅용 문자열 표현
    /// </summary>
    public override string ToString()
    {
        return $"Item {instanceId} → Slot {characterSlotIndex} (Bound: {bindTime})";
    }
}

