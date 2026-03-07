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
    public List<MaterialStack> materialRewards;  // ⭐ 재료 보상 추가
    
    /// <summary>
    /// 생성자
    /// </summary>
    public StageResultData(bool victory, int gold, int exp, List<ItemRewardData> items = null, List<MaterialStack> materials = null)
    {
        isVictory = victory;
        goldReward = gold;
        expReward = exp;
        itemRewards = items ?? new List<ItemRewardData>();
        materialRewards = materials ?? new List<MaterialStack>();  // ⭐ 재료 초기화
    }
    
    /// <summary>
    /// RewardSystem.RewardResult에서 변환
    /// </summary>
    public static StageResultData FromRewardResult(StageSystem.RewardSystem.RewardResult rewardResult, bool victory)
    {
        // ⭐ V2: ItemInstanceIds를 기반으로 변환 (동적 스탯 지원)
        List<ItemRewardData> convertedItems = new List<ItemRewardData>();
        
        if (rewardResult.ItemInstanceIds != null && rewardResult.ItemInstanceIds.Count > 0)
        {
            // ⭐ 생성된 인스턴스 ID 목록으로 변환 (각 인스턴스를 개별 슬롯으로 표시)
            var accountData = AccountDataManager.Instance;
            if (accountData != null)
            {
                foreach (var instanceId in rewardResult.ItemInstanceIds)
                {
                    if (instanceId.IsEmpty) continue;
                    
                    var instance = accountData.GetInstance(instanceId);
                    if (instance != null)
                    {
                        convertedItems.Add(new ItemRewardData(instance.templateName, 1, instanceId));
                    }
                }
            }
        }
        else if (rewardResult.Items != null)
        {
            // Legacy: DropItemData 기반 (ItemInstanceID 없음)
            foreach (var dropItem in rewardResult.Items)
            {
                convertedItems.Add(ItemRewardData.FromDropItemData(dropItem));
            }
        }
        
        return new StageResultData(
            victory,
            rewardResult.Gold,
            rewardResult.Exp,
            convertedItems,
            rewardResult.MaterialRewards  // ⭐ 재료 보상 전달
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
    public ItemInstanceID instanceId;  // ⭐ 추가: 동적 스탯 표시용
    
    public ItemRewardData(string id, int count, ItemInstanceID instance = default)
    {
        itemId = id;
        amount = count;
        instanceId = instance;
    }
    
    /// <summary>
    /// StageSystem.DropItemData에서 변환
    /// </summary>
    public static ItemRewardData FromDropItemData(StageSystem.DropItemData dropItem)
    {
        return new ItemRewardData(dropItem.ItemID, dropItem.Amount);
    }
}

