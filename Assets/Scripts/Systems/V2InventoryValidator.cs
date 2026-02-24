using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// V2 인벤토리 데이터 무결성 검증 유틸리티
/// - 아이템 인스턴스 중복 감지 (가방/창고/우편함/장착 간)
/// - 귀속 정보 일치성 검사
/// - 개발/커밋 시점 검증용 (성능 고려)
/// </summary>
public static class V2InventoryValidator
{
    /// <summary>
    /// 전체 검증 (개발 빌드에서만 실행 권장)
    /// </summary>
    public static bool ValidateAll(
        AccountData accountData, 
        PlayerSlotData[] allSlots,
        bool throwOnError = false)
    {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
        return true; // ⭐ 릴리즈 빌드에서는 스킵
#else
        bool isValid = true;
        
        // 1. AccountData 중복 검사
        isValid &= ValidateAccountNoDuplicates(accountData, throwOnError);
        
        // 2. PlayerSlotData 중복 검사
        if (allSlots != null)
        {
            foreach (var slot in allSlots)
            {
                if (slot != null)
                {
                    isValid &= ValidateSlotNoDuplicates(slot, throwOnError);
                }
            }
        }
        
        // 3. Account ↔ Slot 간 중복 검사
        if (allSlots != null)
        {
            isValid &= ValidateCrossContainerNoDuplicates(accountData, allSlots, throwOnError);
        }
        
        // 4. 귀속 정보 일치성
        if (allSlots != null)
        {
            isValid &= ValidateBindConsistency(accountData, allSlots, throwOnError);
        }
        
        if (isValid)
        {
            Debug.Log("✅ [V2Validator] 전체 검증 통과");
        }
        else
        {
            Debug.LogError("❌ [V2Validator] 검증 실패 - 데이터 무결성 문제 발견");
        }
        
        return isValid;
#endif
    }
    
    /// <summary>
    /// 커밋 시점 검증 (중요 트랜잭션 완료 후)
    /// 예: 스테이지 종료, 상점 구매, 장착/해제 등
    /// </summary>
    public static void ValidateOnCommit(
        AccountData accountData, 
        PlayerSlotData[] allSlots,
        string context = "")
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"🔍 [V2Validator] 커밋 검증 시작: {context}");
        bool result = ValidateAll(accountData, allSlots, throwOnError: false);
        
        if (!result)
        {
            Debug.LogError($"❌ [V2Validator] 커밋 검증 실패: {context} - 데이터 롤백 필요!");
        }
#endif
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 개별 검증 메서드
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    /// <summary>
    /// AccountData 내부 중복 검사 (창고/우편함)
    /// </summary>
    public static bool ValidateAccountNoDuplicates(AccountData accountData, bool throwOnError = false)
    {
        if (accountData == null) return true;
        
        var allIds = new List<ItemInstanceID>();
        allIds.AddRange(accountData.sharedInventoryIds);
        allIds.AddRange(accountData.mailboxIds);
        
        return CheckDuplicates(allIds, "AccountData (창고+우편함)", throwOnError);
    }
    
    /// <summary>
    /// PlayerSlotData 내부 중복 검사 (가방+장착)
    /// </summary>
    public static bool ValidateSlotNoDuplicates(PlayerSlotData slotData, bool throwOnError = false)
    {
        if (slotData == null) return true;
        
        var allIds = new List<ItemInstanceID>();
        allIds.AddRange(slotData.characterBagInstanceIds);
        
        if (slotData.equippedRecords != null)
        {
            allIds.AddRange(slotData.equippedRecords.Select(r => r.instanceId));
        }
        
        return CheckDuplicates(allIds, "PlayerSlotData (가방+장착)", throwOnError);
    }
    
    /// <summary>
    /// Account ↔ Slot 간 중복 검사
    /// 동일 아이템 인스턴스가 Account와 Slot에 동시 존재하면 안 됨
    /// </summary>
    public static bool ValidateCrossContainerNoDuplicates(
        AccountData accountData, 
        PlayerSlotData[] allSlots,
        bool throwOnError = false)
    {
        if (accountData == null || allSlots == null) return true;
        
        var accountIds = new HashSet<ItemInstanceID>();
        accountIds.UnionWith(accountData.sharedInventoryIds);
        accountIds.UnionWith(accountData.mailboxIds);
        
        foreach (var slot in allSlots)
        {
            if (slot == null) continue;
            
            // 가방 검사
            foreach (var id in slot.characterBagInstanceIds)
            {
                if (accountIds.Contains(id))
                {
                    string error = $"❌ [V2Validator] 중복 감지: {id} (Account와 Slot에 동시 존재)";
                    Debug.LogError(error);
                    
                    if (throwOnError)
                        throw new System.Exception(error);
                    
                    return false;
                }
            }
            
            // 장착 검사
            if (slot.equippedRecords != null)
            {
                foreach (var record in slot.equippedRecords)
                {
                    if (accountIds.Contains(record.instanceId))
                    {
                        string error = $"❌ [V2Validator] 중복 감지: {record.instanceId} (Account와 Slot 장착에 동시 존재)";
                        Debug.LogError(error);
                        
                        if (throwOnError)
                            throw new System.Exception(error);
                        
                        return false;
                    }
                }
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 귀속 정보 일치성 검사
    /// - 귀속된 아이템은 해당 슬롯에만 존재해야 함
    /// - 다른 슬롯이나 Account에 있으면 안 됨
    /// </summary>
    public static bool ValidateBindConsistency(
        AccountData accountData, 
        PlayerSlotData[] allSlots,
        bool throwOnError = false)
    {
        if (accountData == null || accountData.binds == null || allSlots == null)
            return true;
        
        foreach (var bind in accountData.binds)
        {
            int slotIdx = bind.characterSlotIndex;
            var id = bind.instanceId;
            
            // 해당 슬롯 확인
            if (slotIdx < 0 || slotIdx >= allSlots.Length || allSlots[slotIdx] == null)
            {
                Debug.LogWarning($"⚠️ [V2Validator] 귀속 정보 이상: Slot {slotIdx} 없음 (ID: {id})");
                continue;
            }
            
            var targetSlot = allSlots[slotIdx];
            bool foundInSlot = targetSlot.characterBagInstanceIds.Contains(id);
            
            if (targetSlot.equippedRecords != null)
            {
                foundInSlot |= targetSlot.equippedRecords.Any(r => r.instanceId == id);
            }
            
            if (!foundInSlot)
            {
                string error = $"❌ [V2Validator] 귀속 불일치: {id}는 Slot {slotIdx}에 귀속되었으나 존재하지 않음";
                Debug.LogError(error);
                
                if (throwOnError)
                    throw new System.Exception(error);
                
                return false;
            }
            
            // 다른 슬롯에 없는지 검사
            for (int i = 0; i < allSlots.Length; i++)
            {
                if (i == slotIdx || allSlots[i] == null) continue;
                
                var otherSlot = allSlots[i];
                if (otherSlot.characterBagInstanceIds.Contains(id))
                {
                    string error = $"❌ [V2Validator] 귀속 위반: {id}는 Slot {slotIdx}에 귀속되었으나 Slot {i}에 존재";
                    Debug.LogError(error);
                    
                    if (throwOnError)
                        throw new System.Exception(error);
                    
                    return false;
                }
                
                if (otherSlot.equippedRecords != null && 
                    otherSlot.equippedRecords.Any(r => r.instanceId == id))
                {
                    string error = $"❌ [V2Validator] 귀속 위반: {id}는 Slot {slotIdx}에 귀속되었으나 Slot {i}에 장착됨";
                    Debug.LogError(error);
                    
                    if (throwOnError)
                        throw new System.Exception(error);
                    
                    return false;
                }
            }
            
            // Account에 없는지 검사
            if (accountData.sharedInventoryIds.Contains(id) || accountData.mailboxIds.Contains(id))
            {
                string error = $"❌ [V2Validator] 귀속 위반: {id}는 Slot {slotIdx}에 귀속되었으나 Account에 존재";
                Debug.LogError(error);
                
                if (throwOnError)
                    throw new System.Exception(error);
                
                return false;
            }
        }
        
        return true;
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 헬퍼 메서드
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private static bool CheckDuplicates(List<ItemInstanceID> ids, string containerName, bool throwOnError)
    {
        var seen = new HashSet<ItemInstanceID>();
        
        foreach (var id in ids)
        {
            if (id.IsEmpty) continue;
            
            if (seen.Contains(id))
            {
                string error = $"❌ [V2Validator] 중복 감지: {id} ({containerName})";
                Debug.LogError(error);
                
                if (throwOnError)
                    throw new System.Exception(error);
                
                return false;
            }
            
            seen.Add(id);
        }
        
        return true;
    }
}

