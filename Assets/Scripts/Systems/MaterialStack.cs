using UnityEngine;

/// <summary>
/// 재료 아이템 스택
/// 강화파편, 정령석 등 스택 가능한 재료 관리
/// </summary>
[System.Serializable]
public class MaterialStack
{
    [Tooltip("재료 ID (예: FRAGMENT_ENHANCE, SPIRITSTONE)")]
    public string materialId;
    
    [Tooltip("보유 수량")]
    public int count;
    
    /// <summary>
    /// 디버깅용 문자열 표현
    /// </summary>
    public override string ToString()
    {
        return $"{materialId} x{count}";
    }
}

