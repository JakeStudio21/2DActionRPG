using System;
using UnityEngine;

/// <summary>
/// 아이템 인스턴스 고유 ID
/// Dictionary 키로 사용 가능하도록 IEquatable 구현
/// </summary>
[System.Serializable]
public struct ItemInstanceId : IEquatable<ItemInstanceId>
{
    public string id;
    
    /// <summary>
    /// 새로운 고유 ID 생성
    /// </summary>
    public static ItemInstanceId NewId()
    {
        return new ItemInstanceId { id = Guid.NewGuid().ToString() };
    }
    
    /// <summary>
    /// 유효한 ID인지 확인
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(id);
    }
    
    // ========================================
    // IEquatable 구현 (Dictionary 키 사용)
    // ========================================
    
    public bool Equals(ItemInstanceId other)
    {
        return id == other.id;
    }
    
    public override bool Equals(object obj)
    {
        return obj is ItemInstanceId other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return id?.GetHashCode() ?? 0;
    }
    
    // ========================================
    // 연산자 오버로딩
    // ========================================
    
    public static bool operator ==(ItemInstanceId left, ItemInstanceId right)
    {
        return left.Equals(right);
    }
    
    public static bool operator !=(ItemInstanceId left, ItemInstanceId right)
    {
        return !left.Equals(right);
    }
    
    public override string ToString()
    {
        return id ?? "INVALID";
    }
}

