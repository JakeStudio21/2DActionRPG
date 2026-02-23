using UnityEngine;

/// <summary>
/// 장착 아이템 레코드 (슬롯 + 인스턴스 ID + 템플릿명)
/// JSON 직렬화를 위해 List 저장 가능
/// </summary>
[System.Serializable]
public class EquippedRecord
{
    [Tooltip("장비 슬롯")]
    public EquipmentSlot slot;
    
    [Tooltip("아이템 인스턴스 ID")]
    public ItemInstanceId instanceId;
    
    [Tooltip("아이템 템플릿 이름 (Phase B 수정: AccountData 의존성 제거)")]
    public string templateName;
    
    /// <summary>
    /// 디버깅용 문자열 표현
    /// </summary>
    public override string ToString()
    {
        return $"[{slot}] → {templateName} ({instanceId})";
    }
}

