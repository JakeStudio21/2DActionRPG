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
public enum ItemGrade { S, A, B, C, D } 