using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 드롭 결과
/// </summary>
[System.Serializable]
public class DropResult
{
    public string itemId;
    public int quantity;
    public ItemRarity rarity;
}

/// <summary>
/// 드롭 해결사 - DropTable을 실제 드롭으로 변환
/// </summary>
public static class DropResolver
{
    /// <summary>
    /// DropGroupId로 드롭 테이블을 찾아서 드롭 실행
    /// </summary>
    public static List<DropResult> ResolveDrop(string dropGroupId, int stageLevel)
    {
        DropTable dropTable = LoadDropTable(dropGroupId);
        
        if (dropTable == null)
        {
            Debug.LogWarning($"[DropResolver] DropGroupId '{dropGroupId}'에 해당하는 DropTable을 찾을 수 없습니다!");
            return new List<DropResult>();
        }

        return ResolveDrop(dropTable, stageLevel);
    }

    /// <summary>
    /// DropTable로 직접 드롭 실행
    /// </summary>
    public static List<DropResult> ResolveDrop(DropTable dropTable, int stageLevel)
    {
        if (dropTable == null) return new List<DropResult>();

        List<DropEntry> validEntries = dropTable.GetValidEntries(stageLevel);
        
        if (validEntries.Count == 0)
        {
            Debug.Log($"[DropResolver] {dropTable.dropGroupId}: 스테이지 레벨 {stageLevel}에서 유효한 드롭이 없습니다.");
            return new List<DropResult>();
        }

        switch (dropTable.dropPolicy)
        {
            case DropPolicy.WeightedPickOne:
                return WeightedPickOne(validEntries);
                
            case DropPolicy.WeightedPickN:
                return WeightedPickN(validEntries, dropTable.pickCount);
                
            case DropPolicy.RollEachWithChance:
                return RollEachWithChance(validEntries);
                
            default:
                Debug.LogError($"[DropResolver] 알 수 없는 DropPolicy: {dropTable.dropPolicy}");
                return new List<DropResult>();
        }
    }

    /// <summary>
    /// 가중치로 1개 선택
    /// </summary>
    private static List<DropResult> WeightedPickOne(List<DropEntry> entries)
    {
        List<DropResult> results = new List<DropResult>();
        
        // 보장 드롭 먼저 처리
        foreach (var entry in entries)
        {
            if (entry.isGuaranteed)
            {
                results.Add(CreateDropResult(entry));
            }
        }

        // 가중치 선택
        DropEntry selectedEntry = SelectByWeight(entries);
        if (selectedEntry != null && !selectedEntry.isGuaranteed)
        {
            results.Add(CreateDropResult(selectedEntry));
        }

        return results;
    }

    /// <summary>
    /// 가중치로 N개 선택
    /// </summary>
    private static List<DropResult> WeightedPickN(List<DropEntry> entries, int pickCount)
    {
        List<DropResult> results = new List<DropResult>();
        List<DropEntry> remainingEntries = new List<DropEntry>(entries);

        // 보장 드롭 먼저 처리
        for (int i = remainingEntries.Count - 1; i >= 0; i--)
        {
            if (remainingEntries[i].isGuaranteed)
            {
                results.Add(CreateDropResult(remainingEntries[i]));
                remainingEntries.RemoveAt(i);
            }
        }

        // 나머지 개수만큼 가중치 선택
        for (int i = 0; i < pickCount && remainingEntries.Count > 0; i++)
        {
            DropEntry selectedEntry = SelectByWeight(remainingEntries);
            if (selectedEntry != null)
            {
                results.Add(CreateDropResult(selectedEntry));
                remainingEntries.Remove(selectedEntry);
            }
        }

        return results;
    }

    /// <summary>
    /// 각 항목 독립 확률로 드롭
    /// </summary>
    private static List<DropResult> RollEachWithChance(List<DropEntry> entries)
    {
        List<DropResult> results = new List<DropResult>();

        foreach (var entry in entries)
        {
            bool shouldDrop = entry.isGuaranteed || Random.Range(0f, 1f) <= entry.chance;
            
            if (shouldDrop)
            {
                results.Add(CreateDropResult(entry));
            }
        }

        return results;
    }

    /// <summary>
    /// 가중치 기반 선택
    /// </summary>
    private static DropEntry SelectByWeight(List<DropEntry> entries)
    {
        if (entries.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var entry in entries)
        {
            totalWeight += entry.weight;
        }

        if (totalWeight <= 0f) return null;

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var entry in entries)
        {
            currentWeight += entry.weight;
            if (randomValue <= currentWeight)
            {
                return entry;
            }
        }

        return entries[entries.Count - 1]; // fallback
    }

    /// <summary>
    /// DropEntry에서 DropResult 생성
    /// </summary>
    private static DropResult CreateDropResult(DropEntry entry)
    {
        return new DropResult
        {
            itemId = entry.itemId,
            quantity = entry.GetRandomQuantity(),
            rarity = entry.rarity
        };
    }

    /// <summary>
    /// Resources에서 DropTable 로드
    /// </summary>
    private static DropTable LoadDropTable(string dropGroupId)
    {
        // Resources/DropTables/ 폴더에서 찾기
        DropTable[] allDropTables = Resources.LoadAll<DropTable>("DropTables");
        
        foreach (var dropTable in allDropTables)
        {
            if (dropTable.dropGroupId == dropGroupId)
            {
                return dropTable;
            }
        }

        return null;
    }

    /// <summary>
    /// 디버그용 드롭 결과 출력
    /// </summary>
    public static string GetDropResultsDebugInfo(List<DropResult> results)
    {
        if (results.Count == 0) return "[DropResolver] 드롭 없음";

        string info = $"[DropResolver] 드롭 결과 ({results.Count}개):\n";
        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            info += $"  #{i + 1}: {result.itemId} x{result.quantity} ({result.rarity})\n";
        }

        return info;
    }
}
