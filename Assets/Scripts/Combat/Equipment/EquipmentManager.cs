using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 장비 관리자 (싱글톤)
/// - 장비 슬롯 관리
/// - EquipmentData 캐싱
/// - Equip/Unequip 로직
/// - Grade 6+ 귀속 처리
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    #region 싱글톤
    
    public static EquipmentManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    #endregion
    
    #region 장비 슬롯
    
    /// <summary>
    /// 장착된 장비 (슬롯별)
    /// </summary>
    private Dictionary<EquipmentSlot, EquipmentInstance> equippedItems = new Dictionary<EquipmentSlot, EquipmentInstance>();
    
    /// <summary>
    /// PlayerRuntimeStats 참조
    /// </summary>
    private PlayerRuntimeStats playerStats;
    
    /// <summary>
    /// 디버그 로그 출력 여부
    /// </summary>
    
    private void Start()
    {
        // PlayerRuntimeStats 찾기
        playerStats = FindObjectOfType<PlayerRuntimeStats>();
        if (playerStats == null)
        {
            Debug.LogError("[EquipmentManager] PlayerRuntimeStats를 찾을 수 없습니다!");
        }
    }
    
    #endregion
    
    #region 이벤트
    
    /// <summary>
    /// 장비 변경 이벤트
    /// </summary>
    public event Action<EquipmentSlot, EquipmentInstance> OnEquipmentChanged;
    
    /// <summary>
    /// 아이템 귀속 이벤트
    /// </summary>
    public event Action<ItemInstanceID, string> OnItemBound;
    
    #endregion
    
    #region Equip/Unequip
    
    /// <summary>
    /// 장비 착용
    /// </summary>
    public bool EquipItem(EquipmentSlot slot, EquipmentInstance item)
    {
        if (item == null)
        {
            Debug.LogError("[EquipmentManager] item이 null입니다.");
            return false;
        }
        
        if (item.EquipmentData == null)
        {
            Debug.LogError($"[EquipmentManager] {item.instanceId} - EquipmentData가 null입니다.");
            return false;
        }
        
        // TODO: 슬롯 호환성 체크 (EquipmentData에 equipmentType, ArmorType 등으로 추론 필요)
        // 현재는 스킵 (추후 구현 예정)
        
        // 1. 기존 장비 해제
        if (equippedItems.ContainsKey(slot))
        {
            UnequipItem(slot);
        }
        
        // 2. 귀속 처리 (Grade 6+)
        ProcessBinding(item);
        
        // 3. 장착
        equippedItems[slot] = item;
        
        // 4. StatModifier 적용
        ApplyEquipmentStats(item);
        
        // 5. 이벤트 발행
        OnEquipmentChanged?.Invoke(slot, item);
        
        Dbg.Log($"[EquipmentManager] {item} 착용 완료 (슬롯: {slot})");
        
        return true;
    }
    
    /// <summary>
    /// 장비 해제
    /// </summary>
    public bool UnequipItem(EquipmentSlot slot)
    {
        if (!equippedItems.TryGetValue(slot, out var item))
        {
            Debug.LogWarning($"[EquipmentManager] {slot} 슬롯이 비어있습니다.");
            return false;
        }
        
        // 1. StatModifier 제거
        RemoveEquipmentStats(item);
        
        // 2. 슬롯에서 제거
        equippedItems.Remove(slot);
        
        // 3. 이벤트 발행
        OnEquipmentChanged?.Invoke(slot, null);
        
        Dbg.Log($"[EquipmentManager] {item} 해제 완료 (슬롯: {slot})");
        
        return true;
    }
    
    /// <summary>
    /// 장착된 장비 가져오기
    /// </summary>
    public EquipmentInstance GetEquippedItem(EquipmentSlot slot)
    {
        if (equippedItems.TryGetValue(slot, out var item))
        {
            return item;
        }
        return null;
    }
    
    /// <summary>
    /// 모든 장착된 장비 가져오기
    /// </summary>
    public Dictionary<EquipmentSlot, EquipmentInstance> GetAllEquippedItems()
    {
        return new Dictionary<EquipmentSlot, EquipmentInstance>(equippedItems);
    }
    
    #endregion
    
    #region 귀속 처리
    
    /// <summary>
    /// 귀속 처리 (Grade 6+)
    /// </summary>
    private void ProcessBinding(EquipmentInstance item)
    {
        // 이미 귀속된 경우 스킵
        if (item.isBound) return;
        
        // Grade S 이상 체크 (전설 이상 장비는 귀속)
        if (item.EquipmentData.itemGrade >= ItemGrade.S)
        {
            item.isBound = true;
            
            // 이벤트 발행 (UI 알림용)
            OnItemBound?.Invoke(item.instanceId, item.EquipmentData.equipmentName);
            
        }
    }
    
    #endregion
    
    #region StatModifier 연동
    
    /// <summary>
    /// 장비 스탯 적용
    /// </summary>
    private void ApplyEquipmentStats(EquipmentInstance item)
    {
        if (playerStats == null)
        {
            Debug.LogError("[EquipmentManager] PlayerRuntimeStats가 null입니다!");
            return;
        }
        
        var modifiers = item.GetStatModifiers();
        
        
        foreach (var modifier in modifiers)
        {
            playerStats.AddStatModifier(modifier);
        }
        
        // 전투 스탯 재계산
        playerStats.RecalculateAllStats();
        
    }
    
    /// <summary>
    /// 장비 스탯 제거
    /// </summary>
    private void RemoveEquipmentStats(EquipmentInstance item)
    {
        if (playerStats == null)
        {
            Debug.LogError("[EquipmentManager] PlayerRuntimeStats가 null입니다!");
            return;
        }
        
        var modifiers = item.GetStatModifiers();
        
        foreach (var modifier in modifiers)
        {
            playerStats.RemoveStatModifier(modifier);
        }
        
        // 전투 스탯 재계산
        playerStats.RecalculateAllStats();
        
    }
    
    /// <summary>
    /// 모든 장비 스탯 재계산 (강화 후 등)
    /// </summary>
    public void RecalculateAllEquipmentStats()
    {
        if (playerStats == null) return;
        
        // 모든 장비의 캐시 무효화
        foreach (var item in equippedItems.Values)
        {
            item.InvalidateCache();
        }
        
        // 스탯 재계산
        playerStats.RecalculateAllStats();
        
    }
    
    /// <summary>
    /// 장착된 모든 장비의 StatModifier 리스트를 반환
    /// (PlayerRuntimeStats에서 호출)
    /// </summary>
    public List<StatModifier> GetAllStatModifiers()
    {
        List<StatModifier> allModifiers = new List<StatModifier>();
        
        foreach (var kvp in equippedItems)
        {
            if (kvp.Value != null)
            {
                var modifiers = kvp.Value.GetStatModifiers();
                allModifiers.AddRange(modifiers);
                
            }
        }
        
        
        return allModifiers;
    }
    
    #endregion
    
    #region 디버그
    
    /// <summary>
    /// 현재 장착 정보 출력
    /// </summary>
    [ContextMenu("Debug Equipment Info")]
    public void DebugEquipmentInfo()
    {
        
        foreach (var kvp in equippedItems)
        {
        }
        
        if (equippedItems.Count == 0)
        {
        }
    }
    
    #endregion
}

