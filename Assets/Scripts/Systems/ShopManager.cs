using UnityEngine;

public class ShopManager : Singleton<ShopManager>
{
    public int playerGold;

    public bool TryBuyItem(ItemData item, int price)
    {
        if (playerGold >= price)
        {
            playerGold -= price;
            // 인벤토리에 아이템 추가 (Inventory 필요)
            Inventory inventory = FindObjectOfType<Inventory>();
            if (inventory != null)
                inventory.AddItem(item);
            Debug.Log($"{item.itemName} 구매 성공! 남은 골드: {playerGold}");
            return true;
        }
        else
        {
            Debug.Log($"{item.itemName} 구매 실패! 골드 부족");
            return false;
        }
    }
} 