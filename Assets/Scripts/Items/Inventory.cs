using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<ItemData> items = new List<ItemData>();

    // 아이템 추가
    public void AddItem(ItemData item)
    {
        items.Add(item);
        Debug.Log($"인벤토리에 아이템 추가: {item.itemName}");
    }

    // 아이템 제거
    public void RemoveItem(ItemData item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
            Debug.Log($"인벤토리에서 아이템 제거: {item.itemName}");
        }
    }

    // 인벤토리 전체 반환
    public List<ItemData> GetAllItems()
    {
        return new List<ItemData>(items);
    }
} 