using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 스테이지 결과 데이터 (ResultPopupController에서 사용)
/// RewardSystem.RewardResult를 UI에 전달하기 위한 구조체
/// </summary>
[System.Serializable]
public struct StageResultData
{
    public bool isVictory;
    public int goldReward;
    public int expReward;
    public List<ItemRewardData> itemRewards;
    
    /// <summary>
    /// 생성자
    /// </summary>
    public StageResultData(bool victory, int gold, int exp, List<ItemRewardData> items = null)
    {
        isVictory = victory;
        goldReward = gold;
        expReward = exp;
        itemRewards = items ?? new List<ItemRewardData>();
    }
    
    /// <summary>
    /// RewardSystem.RewardResult에서 변환
    /// </summary>
    public static StageResultData FromRewardResult(StageSystem.RewardSystem.RewardResult rewardResult, bool victory)
    {
        // List<DropItemData> → List<ItemRewardData> 변환
        List<ItemRewardData> convertedItems = new List<ItemRewardData>();
        
        if (rewardResult.Items != null)
        {
            foreach (var dropItem in rewardResult.Items)
            {
                convertedItems.Add(ItemRewardData.FromDropItemData(dropItem));
            }
        }
        
        return new StageResultData(
            victory,
            rewardResult.Gold,
            rewardResult.Exp,
            convertedItems
        );
    }
}

/// <summary>
/// 아이템 보상 데이터 (UI 표시용 간소화)
/// </summary>
[System.Serializable]
public struct ItemRewardData
{
    public string itemId;
    public int amount;
    
    public ItemRewardData(string id, int count)
    {
        itemId = id;
        amount = count;
    }
    
    /// <summary>
    /// StageSystem.DropItemData에서 변환
    /// </summary>
    public static ItemRewardData FromDropItemData(StageSystem.DropItemData dropItem)
    {
        return new ItemRewardData(dropItem.ItemID, dropItem.Amount);
    }
}

