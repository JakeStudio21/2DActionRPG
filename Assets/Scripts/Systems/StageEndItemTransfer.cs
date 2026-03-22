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
        var account    = AccountDataManager.Instance;
        int currentSlotIndex = playerData.CurrentSlotIndex;

        var result   = new TransferResult();
        var slotData = playerData.GetSlotData(currentSlotIndex);

        var bagItems = new List<ItemInstanceID>(slotData.characterBagInstanceIds);

        Log($"🔍 가방 아이템: {bagItems.Count}개, 재료: {slotData.characterBagMaterials.Count}개");

        if (bagItems.Count == 0 && slotData.characterBagMaterials.Count == 0)
        {
            Log("📦 가방이 비어있음 - 전송 스킵");
            return result;
        }

        // ── Phase 1: Memory Update ────────────────────────────────────
        // 가방의 모든 아이템 ID를 영구 컨테이너(창고/우편함)로 이동.
        // 아직 파일에는 저장하지 않음.
        foreach (var itemId in bagItems)
        {
            if (itemId.IsEmpty)
            {
                LogWarning($"⚠️ 빈 ID 스킵: {itemId}");
                continue;
            }

            var bindInfo = account.GetBindInfo(itemId);
            if (bindInfo.isBound)
            {
                Log($"🔒 귀속 아이템 유지: {itemId} (슬롯 {bindInfo.characterSlotIndex})");
                result.skippedBound++;
                continue;
            }

            if (account.TryAddToShared(itemId))
            {
                Log($"✅ 창고 이동: {itemId}");
                result.transferredToStorage++;
            }
            else
            {
                account.MoveToMailbox(itemId);
                LogWarning($"📬 창고 만석 → 우편함: {itemId}");
                result.transferredToMailbox++;
            }
        }

        // ── Phase 2: Memory Clear ─────────────────────────────────────
        // 귀속 아이템을 제외한 이동 완료 ID를 가방에서 제거.
        // (귀속 아이템은 가방에 잔존 → skippedBound 로 카운트됨)
        slotData.characterBagInstanceIds.RemoveAll(id =>
            !id.IsEmpty && !account.GetBindInfo(id).isBound);

        // 재료도 동일하게 계정으로 이관 후 메모리에서 제거
        TransferMaterials(slotData, account, result);

        // ── Phase 3: Sequential Save ──────────────────────────────────
        // [중요] AccountData를 반드시 먼저 저장한다.
        //
        // Crash Recovery 설계 의도:
        //   - AccountData.Save() 성공 후 크래시 발생 →
        //       창고에 ID 추가됨, 가방 파일에 ID 잔존 (중복 상태)
        //   → 다음 실행 시 AutoCleanup PASS B (CleanDuplicateVolatileItems)가
        //       영구 컨테이너(창고/우편함)에 이미 있는 가방 ID를 제거하여 자동 복구.
        //
        //   - AccountData.Save() 실패 →
        //       가방 파일은 변경되지 않음, 아이템 보존됨
        //   → 다음 실행 시 가방에 여전히 아이템 존재, StageEndItemTransfer 재시도 가능.
        account.Save();
        playerData.SaveSlotData(slotData);
        playerData.MarkDirty();

        Log($"✅ Sequential Save 완료: 창고 {result.transferredToStorage}개 / 우편함 {result.transferredToMailbox}개 / 귀속유지 {result.skippedBound}개");
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
/// Phase 8-1: 룬 조각 카운터 추가
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

