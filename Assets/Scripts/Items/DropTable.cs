using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 드롭 정책 종류
/// </summary>
public enum DropPolicy
{
    WeightedPickOne,        // 가중치로 1개 선택
    WeightedPickN,          // 가중치로 N개 선택
    RollEachWithChance      // 각 항목 독립 확률로 드롭
}

/// <summary>
/// 아이템 희귀도
/// </summary>
public enum ItemRarity
{
    Common,     // 일반
    Uncommon,   // 고급
    Rare,       // 희귀
    Epic,       // 영웅
    Legendary   // 전설
}

/// <summary>
/// 개별 드롭 엔트리
/// </summary>
[System.Serializable]
public class DropEntry
{
    [Header("🎯 아이템 정보")]
    [Tooltip("드롭할 아이템 ID")]
    public string itemId = "";
    
    [Tooltip("아이템 희귀도")]
    public ItemRarity rarity = ItemRarity.Common;

    [Header("📊 드롭 확률/가중치")]
    [Tooltip("가중치 방식에서 사용 (WeightedPick)")]
    [Range(0f, 100f)]
    public float weight = 10f;
    
    [Tooltip("독립 확률 방식에서 사용 (RollEach) - 0~1")]
    [Range(0f, 1f)]
    public float chance = 0.3f;

    [Header("📦 드롭 수량")]
    [Tooltip("최소 드롭 개수")]
    public int minQuantity = 1;
    
    [Tooltip("최대 드롭 개수")]
    public int maxQuantity = 1;

    [Header("🎮 조건 설정")]
    [Tooltip("해당 아이템이 등장하는 최소 스테이지 레벨")]
    public int minStageLevel = 1;
    
    [Tooltip("등장하는 최대 스테이지 레벨 (0 = 무제한)")]
    public int maxStageLevel = 0;
    
    [Tooltip("확률 계산 무시하고 반드시 드롭")]
    public bool isGuaranteed = false;

    [Header("🔧 고급 조건 (선택)")]
    [Tooltip("특정 퀘스트/이벤트/플래그 조건")]
    public string[] conditions = new string[0];

    /// <summary>
    /// 현재 스테이지 레벨에서 드롭 가능한지 확인
    /// </summary>
    public bool IsAvailableAtStageLevel(int currentStageLevel)
    {
        if (currentStageLevel < minStageLevel) return false;
        if (maxStageLevel > 0 && currentStageLevel > maxStageLevel) return false;
        return true;
    }

    /// <summary>
    /// 드롭 수량 계산
    /// </summary>
    public int GetRandomQuantity()
    {
        return Random.Range(minQuantity, maxQuantity + 1);
    }
}

/// <summary>
/// 드롭 테이블 ScriptableObject - 중앙 집중식 드롭 관리
/// </summary>
[CreateAssetMenu(fileName = "DropTable", menuName = "Items/Drop Table")]
public class DropTable : ScriptableObject
{
    [Header("🏷️ 메타 정보")]
    [Tooltip("드롭 그룹 고유 ID (예: DG_SLIME_LOW)")]
    public string dropGroupId = "";
    
    [Tooltip("드롭 방식")]
    public DropPolicy dropPolicy = DropPolicy.RollEachWithChance;
    
    [Tooltip("WeightedPickN 방식에서 선택할 개수")]
    [Range(1, 10)]
    public int pickCount = 1;
    
    [TextArea(2, 4)]
    [Tooltip("디자이너 참고 설명")]
    public string description = "";

    [Header("📦 드롭 엔트리")]
    [Tooltip("드롭 가능한 아이템들")]
    public List<DropEntry> dropEntries = new List<DropEntry>();

    [Header("📊 통계 (읽기 전용)")]
    [Tooltip("총 가중치 (자동 계산)")]
    [SerializeField] private float totalWeight = 0f;
    
    [Tooltip("총 확률 (자동 계산)")]
    [SerializeField] private float totalChance = 0f;

    // Public Properties
    public float TotalWeight => totalWeight;
    public float TotalChance => totalChance;

    /// <summary>
    /// 현재 스테이지 레벨에서 유효한 드롭 엔트리들 반환
    /// </summary>
    public List<DropEntry> GetValidEntries(int stageLevel)
    {
        List<DropEntry> validEntries = new List<DropEntry>();
        
        foreach (var entry in dropEntries)
        {
            if (entry.IsAvailableAtStageLevel(stageLevel))
            {
                validEntries.Add(entry);
            }
        }
        
        return validEntries;
    }

    /// <summary>
    /// Inspector에서 통계 자동 계산
    /// </summary>
    private void OnValidate()
    {
        // DropGroupId 검증
        if (string.IsNullOrEmpty(dropGroupId))
        {
            Debug.LogWarning($"[DropTable] {name}: DropGroupId가 설정되지 않았습니다!");
        }

        // 통계 계산
        CalculateStatistics();
        
        // 드롭 엔트리 검증
        ValidateDropEntries();
    }

    /// <summary>
    /// 통계 계산
    /// </summary>
    private void CalculateStatistics()
    {
        totalWeight = 0f;
        totalChance = 0f;

        foreach (var entry in dropEntries)
        {
            totalWeight += entry.weight;
            totalChance += entry.chance;
        }
    }

    /// <summary>
    /// 드롭 엔트리 검증
    /// </summary>
    private void ValidateDropEntries()
    {
        for (int i = 0; i < dropEntries.Count; i++)
        {
            var entry = dropEntries[i];
            
            // 수량 검증
            entry.minQuantity = Mathf.Max(0, entry.minQuantity);
            entry.maxQuantity = Mathf.Max(entry.minQuantity, entry.maxQuantity);
            
            // 스테이지 레벨 검증
            entry.minStageLevel = Mathf.Max(1, entry.minStageLevel);
            if (entry.maxStageLevel > 0)
            {
                entry.maxStageLevel = Mathf.Max(entry.minStageLevel, entry.maxStageLevel);
            }
            
            // ItemId 검증
            if (string.IsNullOrEmpty(entry.itemId))
            {
                Debug.LogWarning($"[DropTable] {name}: Entry #{i}의 ItemId가 비어있습니다!");
            }
        }

        // 확률 총합 경고
        if (dropPolicy == DropPolicy.RollEachWithChance && totalChance > dropEntries.Count)
        {
            Debug.LogWarning($"[DropTable] {name}: 총 확률이 {totalChance:F2}로 높습니다. 조정을 고려해보세요.");
        }
    }

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public string GetDebugInfo()
    {
        return $"[DropTable] ID: {dropGroupId}\n" +
               $"Policy: {dropPolicy}\n" +
               $"Entries: {dropEntries.Count}\n" +
               $"Total Weight: {totalWeight:F1}\n" +
               $"Total Chance: {totalChance:F2}";
    }
}
