using UnityEngine;

/// <summary>
/// 상점 관리 매니저 (PlayerManager와 통합)
/// </summary>
public class ShopManager : Singleton<ShopManager>
{
    /// <summary>
    /// 아이템 구매 시도 (PlayerManager와 통합)
    /// </summary>
    public bool TryBuyItem(ItemData item, int price)
    {
        // ⭐ [Phase 1] PlayerDataManager 우선, PlayerManager 백업 사용
        bool hasGoldManager = false;
        
        if (PlayerDataManager.Instance != null)
        {
            hasGoldManager = true;
            
            // PlayerDataManager를 통해 골드 확인 및 소모
            if (PlayerDataManager.Instance.SpendGold(price))
            {
                // 인벤토리에 아이템 추가
                Inventory inventory = FindObjectOfType<Inventory>();
                if (inventory != null)
                {
                    inventory.AddItem(item);
                    Debug.Log($"[ShopManager] {item.itemName} 구매 성공! 남은 골드: {PlayerDataManager.Instance.GetCurrentGold()}");
                    return true;
                }
                else
                {
                    // 인벤토리가 없으면 골드 환불
                    PlayerDataManager.Instance.AddGold(price);
                    Debug.LogError("[ShopManager] 인벤토리를 찾을 수 없어 구매를 취소했습니다.");
                    return false;
                }
            }
            else
            {
                Debug.Log($"[ShopManager] {item.itemName} 구매 실패! 골드 부족 (필요: {price}, 보유: {PlayerDataManager.Instance.GetCurrentGold()})");
                return false;
            }
        }
        else if (PlayerManager.Instance != null)
        {
            hasGoldManager = true;
            
            // 백업: 기존 PlayerManager 사용 (호환성)
            if (PlayerManager.Instance.SpendGold(price))
            {
                Inventory inventory = FindObjectOfType<Inventory>();
                if (inventory != null)
                {
                    inventory.AddItem(item);
                    Debug.Log($"[ShopManager] {item.itemName} 구매 성공! 남은 골드: {PlayerManager.Instance.GetCurrentGold()}");
                    return true;
                }
                else
                {
                    PlayerManager.Instance.AddGold(price);
                    Debug.LogError("[ShopManager] 인벤토리를 찾을 수 없어 구매를 취소했습니다.");
                    return false;
                }
            }
            else
            {
                Debug.Log($"[ShopManager] {item.itemName} 구매 실패! 골드 부족 (필요: {price}, 보유: {PlayerManager.Instance.GetCurrentGold()})");
                return false;
            }
        }
        
        if (!hasGoldManager)
        {
            Debug.LogError("[ShopManager] PlayerDataManager와 PlayerManager를 모두 찾을 수 없습니다!");
            return false;
        }
        
        return false;
    }

    /// <summary>
    /// 현재 플레이어 골드 확인
    /// </summary>
    public int GetPlayerGold()
    {
        // ⭐ [Phase 1] PlayerDataManager 우선, PlayerManager 백업 사용
        if (PlayerDataManager.Instance != null)
        {
            return PlayerDataManager.Instance.GetCurrentGold();
        }
        else if (PlayerManager.Instance != null)
        {
            return PlayerManager.Instance.GetCurrentGold();
        }
        return 0;
    }
} 