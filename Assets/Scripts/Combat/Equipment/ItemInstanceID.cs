using System;
using UnityEngine;

/// <summary>
/// 아이템 고유 인스턴스 ID
/// GUID 기반 유니크 식별자
/// </summary>
[Serializable]
public struct ItemInstanceID : IEquatable<ItemInstanceID>
{
    [SerializeField]
    private string id;
    
    /// <summary>
    /// ID 값
    /// </summary>
    public string Value => id;
    
    /// <summary>
    /// 빈 ID 체크
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(id);
    
    /// <summary>
    /// 생성자
    /// </summary>
    private ItemInstanceID(string id)
    {
        this.id = id;
    }
    
    /// <summary>
    /// 새 ID 생성
    /// </summary>
    public static ItemInstanceID Generate()
    {
        return new ItemInstanceID($"ITEM_{Guid.NewGuid():N}");
    }
    
    /// <summary>
    /// 기존 ID로 생성 (로드용)
    /// </summary>
    public static ItemInstanceID FromString(string idString)
    {
        if (string.IsNullOrEmpty(idString))
        {
            Debug.LogWarning("[ItemInstanceID] 빈 문자열로 ID 생성 시도");
        }
        return new ItemInstanceID(idString);
    }
    
    /// <summary>
    /// 빈 ID
    /// </summary>
    public static ItemInstanceID Empty => new ItemInstanceID(string.Empty);
    
    #region Equality
    
    public bool Equals(ItemInstanceID other)
    {
        return id == other.id;
    }
    
    public override bool Equals(object obj)
    {
        return obj is ItemInstanceID other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return id != null ? id.GetHashCode() : 0;
    }
    
    public static bool operator ==(ItemInstanceID left, ItemInstanceID right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(ItemInstanceID left, ItemInstanceID right)
    {
        return !left.Equals(right);
    }
    
    #endregion
    
    public override string ToString()
    {
        return id ?? "Empty";
    }
}

