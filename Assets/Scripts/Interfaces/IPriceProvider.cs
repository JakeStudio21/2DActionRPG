using UnityEngine;

/// <summary>
/// 💰 상점 가격 제공자 인터페이스 (Strategy Pattern)
/// 가격 정책을 추상화하여 확장성 확보
/// </summary>
public interface IPriceProvider
{
    /// <summary>
    /// 아이템 구매 가격 조회
    /// </summary>
    int GetBuyPrice(string itemID);
    
    /// <summary>
    /// 아이템 판매 가격 조회
    /// </summary>
    int GetSellPrice(string itemID);
    
    /// <summary>
    /// 아이템 구매 가능 여부 확인
    /// </summary>
    bool IsItemAvailable(string itemID);
    
    /// <summary>
    /// 한정 판매 아이템 여부 확인
    /// </summary>
    bool IsLimited(string itemID);
    
    /// <summary>
    /// 한정 아이템 재고 수량 조회
    /// </summary>
    int GetStockLimit(string itemID);
}
