using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 종료 시 V2 인게임 가방 아이템을 계정 창고로 자동 이동
/// - 귀속 아이템은 가방에 유지
/// - 창고 가득 차면 우편함 처리
/// </summary>
public class StageEndItemTransfer : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("전송 결과 로그 표시")]
    public bool enableLogs = false;
    
    [Tooltip("전송 지연 시간 (초)")]
    public float transferDelay = 0.5f;
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 공개 API
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    /// <summary>
    /// 스테이지 종료 시 아이템 자동 전송 실행
    /// </summary>
    public void TransferItemsToAccount()
    {
        if (!PlayerDataManager.Instance.IsSlotSelected)
        {
            LogWarning("슬롯이 선택되지 않음 - 전송 스킵");
            return;
        }
        
        if (!AccountDataManager.IsInitialized())
        {
            LogError("AccountDataManager가 초기화되지 않음 - 전송 실패");
            return;
        }
        
        Log("═══════════════════════════════════════════════════════");
        Log("🚀 스테이지 종료 아이템 전송 시작");
        Log("═══════════════════════════════════════════════════════");
        
        var result = ExecuteTransfer();
        
        Log("═══════════════════════════════════════════════════════");
        Log($"✅ 전송 완료: 창고 {result.transferredToStorage}개 | 우편함 {result.transferredToMailbox}개 | 유지 {result.skippedBound}개");
        Log("═══════════════════════════════════════════════════════");
        
        // 전송 결과 이벤트 발생 (UI 업데이트용)
        OnTransferCompleted?.Invoke(result);
    }
    
    /// <summary>
    /// 전송 결과 이벤트
    /// </summary>
    public static System.Action<TransferResult> OnTransferCompleted;
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 핵심 로직
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private TransferResult ExecuteTransfer()
    {
        var playerData = PlayerDataManager.Instance;
        var account = AccountDataManager.Instance;
        int currentSlotIndex = playerData.CurrentSlotIndex;
        
        var result = new TransferResult();
        
        // ⭐ 슬롯 데이터 한 번만 가져오기 (여러 번 저장하면 재료 손실!)
        var slotData = playerData.GetSlotData(currentSlotIndex);
        
        // 1. 현재 가방 아이템 목록 가져오기
        var bagItems = new List<ItemInstanceID>(slotData.characterBagInstanceIds);
        
        Log($"🔍 [StageEndItemTransfer] 가방 아이템 목록 가져오기 완료: {bagItems.Count}개");
        for (int i = 0; i < bagItems.Count; i++)
        {
            Log($"  [{i}] {bagItems[i].Value}");
        }
        
        if (bagItems.Count == 0 && slotData.characterBagMaterials.Count == 0)
        {
            Log("📦 가방이 비어있음 - 전송할 아이템 없음");
            return result;
        }
        
        Log($"📦 가방 아이템 수: {bagItems.Count}개, 재료: {slotData.characterBagMaterials.Count}개");
        
        // 3. 각 장비 아이템 처리
        foreach (var itemId in bagItems)
        {
            if (itemId.IsEmpty)
            {
                LogWarning($"⚠️ 잘못된 아이템 ID 스킵: {itemId}");
                continue;
            }
            
            // 귀속 확인
            var bindInfo = account.GetBindInfo(itemId);
            
            if (bindInfo.isBound)
            {
                // 귀속된 아이템은 가방에 유지
                Log($"🔒 귀속 아이템 유지: {itemId} (슬롯 {bindInfo.characterSlotIndex})");
                result.skippedBound++;
                continue;
            }
            
            // 창고로 이동 시도
            bool addedToStorage = account.TryAddToShared(itemId);
            
            if (addedToStorage)
            {
                // ⭐ 메모리에서만 제거 (저장은 나중에 한 번만!)
                slotData.characterBagInstanceIds.Remove(itemId);
                
                Log($"✅ 창고 이동: {itemId}");
                result.transferredToStorage++;
            }
            else
            {
                // 창고 가득 참 → 우편함 처리
                account.MoveToMailbox(itemId);
                
                // ⭐ 메모리에서만 제거 (저장은 나중에 한 번만!)
                slotData.characterBagInstanceIds.Remove(itemId);
                
                LogWarning($"📬 창고 가득 참 → 우편함 이동: {itemId}");
                result.transferredToMailbox++;
            }
        }
        
        // 4. 재료 전송 (신규) ⭐
        TransferMaterials(slotData, account, result);
        
        // 5. 저장 (⭐ 한 번만 저장!)
        account.Save();
        playerData.SaveSlotData(slotData);
        playerData.MarkDirty();
        
        return result;
    }
    
    /// <summary>
    /// 재료 전송 처리
    /// </summary>
    private void TransferMaterials(PlayerSlotData slotData, AccountDataManager account, TransferResult result)
    {
        if (slotData == null || slotData.characterBagMaterials == null || slotData.characterBagMaterials.Count == 0)
        {
            Log("📦 재료 가방이 비어있음");
            return;
        }
        
        Log($"📦 재료 가방: {slotData.characterBagMaterials.Count}종류");
        
        int matTransferred = 0;
        foreach (var mat in slotData.characterBagMaterials)
        {
            account.AddMaterial(mat.materialType, mat.count);
            matTransferred++;
            Log($"✅ 재료 전송: {mat.materialType.GetDisplayName()} x{mat.count}");
        }
        
        // ⭐ 재료 가방 초기화 (메모리에서만, 저장은 ExecuteTransfer()에서!)
        slotData.characterBagMaterials.Clear();
        
        Log($"✅ 재료 전송 완료: {matTransferred}종류");
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 헬퍼 메서드
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private void Log(string message)
    {
        if (enableLogs)
        {
            Debug.Log($"[StageEndItemTransfer] {message}");
        }
    }
    
    private void LogWarning(string message)
    {
        if (enableLogs)
        {
            Debug.LogWarning($"[StageEndItemTransfer] {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[StageEndItemTransfer] {message}");
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 전송 결과 데이터
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

/// <summary>
/// 스테이지 종료 아이템 전송 결과
/// </summary>
[System.Serializable]
public class TransferResult
{
    /// <summary>창고로 이동한 아이템 수</summary>
    public int transferredToStorage = 0;
    
    /// <summary>우편함으로 이동한 아이템 수 (창고 가득 참)</summary>
    public int transferredToMailbox = 0;
    
    /// <summary>귀속으로 인해 가방에 유지된 아이템 수</summary>
    public int skippedBound = 0;
    
    /// <summary>총 처리된 아이템 수</summary>
    public int TotalProcessed => transferredToStorage + transferredToMailbox + skippedBound;
    
    /// <summary>실제 이동된 아이템 수</summary>
    public int TotalTransferred => transferredToStorage + transferredToMailbox;
}

