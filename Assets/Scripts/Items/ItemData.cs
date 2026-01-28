using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Items/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public ItemType itemType;
    public ItemGrade itemGrade;
    [TextArea]
    public string description;
    public Sprite icon;
}

public enum ItemType { Equipment, Consumable, Quest, Etc }

/// <summary>
/// 아이템 등급 (8등급 체계)
/// </summary>
public enum ItemGrade 
{ 
    D,      // 일반
    C,      // 고급
    B,      // 희귀
    A,      // 영웅
    S,      // 전설
    SS,     // 신화
    EX,     // 고대
    TR      // 초월
} 