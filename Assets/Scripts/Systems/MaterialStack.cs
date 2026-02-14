using UnityEngine;

/// <summary>
/// 재료 스택 (enum + count만 저장)
/// ⭐ ScriptableObject는 저장 ❌
/// ⭐ UI 표시 시에만 GetMaterialData()로 SO 가져오기
/// </summary>
[System.Serializable]
public class MaterialStack
{
    [Tooltip("재료 타입 (enum, JSON 저장용)")]
    public MaterialType materialType;
    
    [Tooltip("재료 타입 이름 (JSON 가독성용, 자동 생성)")]
    public string materialTypeName; // "WeaponFragment"
    
    [Tooltip("재료 표시 이름 (JSON 가독성용, 자동 생성)")]
    public string displayName; // "무기 강화 파편"
    
    [Tooltip("보유 수량")]
    public int count;
    
    /// <summary>
    /// MaterialData ScriptableObject 가져오기 (UI용)
    /// </summary>
    public MaterialData GetData()
    {
        return materialType.GetMaterialData();
    }
    
    /// <summary>
    /// 아이콘 가져오기 (편의 메서드)
    /// </summary>
    public Sprite GetIcon()
    {
        return materialType.GetIcon();
    }
    
    /// <summary>
    /// 표시 이름 가져오기
    /// </summary>
    public string GetDisplayName()
    {
        return materialType.GetDisplayName();
    }
    
    /// <summary>
    /// 재료 등급 가져오기
    /// </summary>
    public MaterialRarity GetRarity()
    {
        var data = GetData();
        return data != null ? data.rarity : MaterialRarity.Common;
    }
    
    /// <summary>
    /// 디버깅용 문자열 표현
    /// </summary>
    public override string ToString()
    {
        return $"{GetDisplayName()} x{count} [{materialType}]";
    }
}

