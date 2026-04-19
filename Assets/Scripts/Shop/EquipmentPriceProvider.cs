using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 💰 EquipmentData 기반 가격 제공자 (기본 구현체)
/// Resources/Equipment 폴더의 EquipmentData를 기준으로 가격 제공
/// </summary>
public class EquipmentPriceProvider : MonoBehaviour, IPriceProvider
{
    [Header("📊 가격 정책 설정")]
    [SerializeField] private bool enableDynamicPricing = false;  // 동적 가격 조정 여부
    [SerializeField] private float discountRate = 0f;            // 할인율 (0.1 = 10% 할인)
    
    [Header("📦 캐시 설정")]
    [SerializeField] private bool enableCaching = true;          // 캐싱 활성화
    [SerializeField] private int maxCacheSize = 100;             // 최대 캐시 크기
    
    
    // 캐싱 시스템
    private Dictionary<string, EquipmentData> equipmentCache = new Dictionary<string, EquipmentData>();
    
    public int GetBuyPrice(string itemID)
    {
        var equipment = GetEquipmentData(itemID);
        if (equipment == null) return 0;
        
        int basePrice = equipment.buyPrice;
        
        // 동적 가격 조정 (할인 등)
        if (enableDynamicPricing && discountRate > 0)
        {
            basePrice = Mathf.RoundToInt(basePrice * (1f - discountRate));
        }
        
            
        return basePrice;
    }
    
    public int GetSellPrice(string itemID)
    {
        var equipment = GetEquipmentData(itemID);
        if (equipment == null) return 0;
        
        int sellPrice = equipment.sellPrice;
        
            
        return sellPrice;
    }
    
    public bool IsItemAvailable(string itemID)
    {
        var equipment = GetEquipmentData(itemID);
        return equipment != null && equipment.isTradable;
    }
    
    public bool IsLimited(string itemID)
    {
        var equipment = GetEquipmentData(itemID);
        return equipment != null && equipment.isLimited;
    }
    
    public int GetStockLimit(string itemID)
    {
        var equipment = GetEquipmentData(itemID);
        return equipment?.quantityLimit ?? 0;
    }
    
    /// <summary>
    /// EquipmentData 조회 (캐싱 지원)
    /// </summary>
    private EquipmentData GetEquipmentData(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return null;
        
        // 캐시 확인
        if (enableCaching && equipmentCache.ContainsKey(itemID))
        {
            return equipmentCache[itemID];
        }
        
        // Resources 폴더에서 로드
        EquipmentData equipment = Resources.Load<EquipmentData>($"Equipment/{itemID}_Equipment");
        
        // 캐시에 저장
        if (enableCaching && equipment != null)
        {
            if (equipmentCache.Count >= maxCacheSize)
            {
                equipmentCache.Clear(); // 캐시 크기 제한
            }
            equipmentCache[itemID] = equipment;
        }
        
        return equipment;
    }
    
    /// <summary>
    /// 할인율 설정 (런타임 이벤트용)
    /// </summary>
    public void SetDiscountRate(float rate)
    {
        discountRate = Mathf.Clamp01(rate);
    }
}