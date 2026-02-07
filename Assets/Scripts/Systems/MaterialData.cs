using UnityEngine;

/// <summary>
/// 재료 데이터 ScriptableObject
/// - 인벤토리 표시용
/// - 몬스터 드롭 설정용
/// - 상세 패널 표시용
/// </summary>
[CreateAssetMenu(fileName = "MaterialData", menuName = "Data/Material", order = 400)]
public class MaterialData : ScriptableObject
{
    [Header("🔑 기본 정보")]
    [Tooltip("MaterialType enum과 연결 (데이터 저장용)")]
    public MaterialType materialType;
    
    [Tooltip("재료 표시 이름 (UI에 표시)")]
    public string displayName = "무기 강화 파편";
    
    [Tooltip("재료 아이콘")]
    public Sprite icon;
    
    [Header("📝 설명")]
    [TextArea(3, 5)]
    [Tooltip("재료 설명 (상세 패널에 표시)")]
    public string description = "무기를 강화할 때 사용하는 파편입니다.\nD/C/B 등급 무기를 분해하면 획득할 수 있습니다.";
    
    [Tooltip("사용 용도 (한 줄)")]
    public string usageHint = "무기 강화 시 사용";
    
    [Tooltip("획득 방법 (한 줄)")]
    public string obtainHint = "무기 분해 또는 몬스터 처치";
    
    [Header("⚙️ 게임 설정")]
    [Tooltip("최대 스택 크기")]
    public int maxStackSize = 9999;
    
    [Tooltip("몬스터가 드롭 가능한가?")]
    public bool canDrop = true;
    
    [Tooltip("재료 등급 (UI 색상용)")]
    public MaterialRarity rarity = MaterialRarity.Common;
    
    [Header("🎨 UI 설정")]
    [Tooltip("아이콘 배경 색상")]
    public Color iconBackgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    
    [Tooltip("등급 테두리 색상 (None이면 등급별 자동)")]
    public Color rarityBorderColor = Color.clear;
    
    [Tooltip("정렬 순서 (낮을수록 앞, 인벤토리 표시 순서)")]
    public int sortOrder = 0;
    
    /// <summary>
    /// 등급별 기본 테두리 색상 반환
    /// </summary>
    public Color GetBorderColor()
    {
        if (rarityBorderColor != Color.clear)
            return rarityBorderColor;
        
        return rarity switch
        {
            MaterialRarity.Common => new Color(0.5f, 0.5f, 0.5f),    // 회색
            MaterialRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),  // 초록
            MaterialRarity.Rare => new Color(0.2f, 0.5f, 1f),        // 파랑
            MaterialRarity.Epic => new Color(0.8f, 0.2f, 1f),        // 보라
            MaterialRarity.Legendary => new Color(1f, 0.6f, 0.2f),   // 주황
            _ => Color.white
        };
    }
    
    /// <summary>
    /// 디버깅용 문자열 표현
    /// </summary>
    public override string ToString()
    {
        return $"{displayName} ({materialType}) [{rarity}]";
    }
}

