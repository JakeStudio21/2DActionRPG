using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// V2 인벤토리 시스템 실제 게임 테스트용 디버그 UI
/// F12 키로 열기/닫기
/// </summary>
public class V2InventoryDebugUI : MonoBehaviour
{
    private bool showUI = false;
    private Vector2 scrollPos = Vector2.zero;
    private Rect windowRect = new Rect(50, 50, 800, 600);
    
    private string newItemTemplate = "Sword_S_Equipment";
    private int newItemEnhancement = 0;
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // Unity 생명주기
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetKeyDown(KeyCode.F12))
        {
            showUI = !showUI;
        }
#endif
    }
    
    private void OnGUI()
    {
        if (!showUI) return;
        
        windowRect = GUI.Window(12345, windowRect, DrawWindow, "🎮 V2 인벤토리 디버그 UI (F12로 닫기)");
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // UI 렌더링
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private void DrawWindow(int windowID)
    {
        GUILayout.BeginVertical();
        
        // 시스템 상태
        DrawSystemStatus();
        
        GUILayout.Space(10);
        
        // 아이템 생성
        DrawItemCreation();
        
        GUILayout.Space(10);
        
        // 스크롤 영역 시작
        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(350));
        
        // 계정 창고
        DrawAccountStorage();
        
        GUILayout.Space(10);
        
        // 캐릭터 가방
        DrawCharacterBag();
        
        GUILayout.Space(10);
        
        // 우편함
        DrawMailbox();
        
        GUILayout.Space(10);
        
        // 장착 아이템
        DrawEquippedItems();
        
        GUILayout.EndScrollView();
        
        GUILayout.Space(10);
        
        // 액션 버튼
        DrawActionButtons();
        
        GUILayout.EndVertical();
        
        // 윈도우 드래그 가능
        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }
    
    private void DrawSystemStatus()
    {
        GUILayout.Label("═════════════ 시스템 상태 ═════════════", GetHeaderStyle());
        
        if (!AccountDataManager.IsInitialized())
        {
            GUILayout.Label("❌ AccountDataManager 미초기화", GetErrorStyle());
            
            if (GUILayout.Button("🔄 AccountDataManager 초기화"))
            {
                AccountDataManager.Initialize();
            }
            
            return;
        }
        
        if (!PlayerDataManager.Instance.IsSlotSelected)
        {
            GUILayout.Label("❌ 캐릭터 슬롯 미선택", GetErrorStyle());
            return;
        }
        
        var playerData = PlayerDataManager.Instance;
        
        GUILayout.Label($"✅ 현재 슬롯: {playerData.CurrentSlotIndex}", GetSuccessStyle());
        GUILayout.Label($"✅ 캐릭터: {playerData.selectedPlayerData.playerName} ({playerData.selectedPlayerData.selectedPlayerType})");
    }
    
    private void DrawItemCreation()
    {
        GUILayout.Label("═════════════ 테스트 아이템 생성 ═════════════", GetHeaderStyle());
        
        if (!CanUseSystem()) return;
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("템플릿:", GUILayout.Width(80));
        newItemTemplate = GUILayout.TextField(newItemTemplate, GUILayout.Width(200));
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("강화:", GUILayout.Width(80));
        newItemEnhancement = (int)GUILayout.HorizontalSlider(newItemEnhancement, 0, 15, GUILayout.Width(150));
        GUILayout.Label($"+{newItemEnhancement}", GUILayout.Width(50));
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("📦 가방에 추가"))
        {
            CreateAndAddToBag();
        }
        
        if (GUILayout.Button("🏦 창고에 추가"))
        {
            CreateAndAddToStorage();
        }
        
        if (GUILayout.Button("⚔️ 장착 (MainWeapon)"))
        {
            CreateAndEquip();
        }
        
        GUILayout.EndHorizontal();
    }
    
    private void DrawAccountStorage()
    {
        GUILayout.Label("═════════════ 🏦 계정 창고 ═════════════", GetHeaderStyle());
        
        if (!CanUseSystem()) return;
        
        var account = AccountDataManager.Instance;
        var accountData = account.GetAccountData();
        var sharedItems = accountData.sharedInventoryIds;
        
        GUILayout.Label($"아이템 수: {sharedItems.Count} / 50");
        
        if (sharedItems.Count == 0)
        {
            GUILayout.Label("(비어있음)");
            return;
        }
        
        foreach (var itemId in sharedItems)
        {
            DrawItemRow(itemId, "창고", () =>
            {
                // ❌ 삭제됨: MoveFromAccountStorage() - V2 시스템에서는 보관창고 → 직접 착용 방식 사용
            });
        }
    }
    
    private void DrawCharacterBag()
    {
        GUILayout.Label("═════════════ 🎒 캐릭터 가방 ═════════════", GetHeaderStyle());
        
        if (!CanUseSystem()) return;
        
        var playerData = PlayerDataManager.Instance;
        var bagItems = playerData.GetCharacterBagV2();
        
        GUILayout.Label($"아이템 수: {bagItems.Count} / {playerData.selectedPlayerData.MaxInventorySize}");
        
        if (bagItems.Count == 0)
        {
            GUILayout.Label("(비어있음)");
            return;
        }
        
        foreach (var itemId in bagItems)
        {
            DrawItemRow(itemId, "가방", () =>
            {
                // 창고로 이동
                if (playerData.MoveToAccountStorage(itemId))
                {
                }
            });
        }
    }
    
    private void DrawMailbox()
    {
        GUILayout.Label("═════════════ 📬 우편함 ═════════════", GetHeaderStyle());
        
        if (!CanUseSystem()) return;
        
        var account = AccountDataManager.Instance;
        var accountData = account.GetAccountData();
        var mailboxItems = accountData.mailboxIds;
        
        GUILayout.Label($"아이템 수: {mailboxItems.Count}");
        
        if (mailboxItems.Count == 0)
        {
            GUILayout.Label("(비어있음)");
            return;
        }
        
        foreach (var itemId in mailboxItems)
        {
            DrawItemRow(itemId, "우편함", () =>
            {
                // 가방으로 수령
                if (PlayerDataManager.Instance.ClaimFromMailbox(itemId))
                {
                }
            });
        }
    }
    
    private void DrawEquippedItems()
    {
        GUILayout.Label("═════════════ ⚔️ 장착 아이템 ═════════════", GetHeaderStyle());
        
        if (!CanUseSystem()) return;
        
        var playerData = PlayerDataManager.Instance;
        var equippedRecords = playerData.GetEquippedRecordsV2();
        
        GUILayout.Label($"장착 수: {equippedRecords.Count}");
        
        if (equippedRecords.Count == 0)
        {
            GUILayout.Label("(비어있음)");
            return;
        }
        
        foreach (var record in equippedRecords)
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            
            GUILayout.Label($"[{record.slot}]", GUILayout.Width(120));
            
            var itemData = AccountDataManager.Instance.GetInstance(record.instanceId);
            if (itemData != null)
            {
                GUILayout.Label($"{itemData.templateName} +{itemData.enhancementLevel}", GUILayout.Width(200));
            }
            else
            {
                GUILayout.Label($"{record.instanceId}", GUILayout.Width(200));
            }
            
            if (GUILayout.Button("해제", GUILayout.Width(60)))
            {
                if (playerData.UnequipV2(record.slot))
                {
                }
            }
            
            GUILayout.EndHorizontal();
        }
    }
    
    private void DrawItemRow(ItemInstanceID itemId, string location, System.Action onMoveClick)
    {
        GUILayout.BeginHorizontal(GUI.skin.box);
        
        var account = AccountDataManager.Instance;
        var itemData = account.GetInstance(itemId);
        
        if (itemData != null)
        {
            // 템플릿 + 강화
            GUILayout.Label($"{itemData.templateName} +{itemData.enhancementLevel}", GUILayout.Width(200));
            
            // 귀속 정보
            var bindInfo = account.GetBindInfo(itemId);
            if (bindInfo.isBound)
            {
                GUILayout.Label($"🔒 슬롯 {bindInfo.characterSlotIndex}", GetBoundStyle(), GUILayout.Width(80));
            }
            else
            {
                GUILayout.Label("", GUILayout.Width(80));
            }
        }
        else
        {
            GUILayout.Label($"{itemId}", GUILayout.Width(200));
            GUILayout.Label("", GUILayout.Width(80));
        }
        
        // 이동 버튼
        if (GUILayout.Button($"이동", GUILayout.Width(60)))
        {
            onMoveClick?.Invoke();
        }
        
        // 삭제 버튼 (Phase 5+ 구현 예정)
        // if (GUILayout.Button("🗑️", GUILayout.Width(40)))
        // {
        //     account.DeleteItem(itemId);
        // }
        
        GUILayout.EndHorizontal();
    }
    
    private void DrawActionButtons()
    {
        GUILayout.Label("═════════════ 액션 ═════════════", GetHeaderStyle());
        
        if (!CanUseSystem()) return;
        
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("🚀 스테이지 종료 시뮬레이션", GUILayout.Height(40)))
        {
            SimulateStageEnd();
        }
        
        if (GUILayout.Button("🔄 데이터 새로고침", GUILayout.Height(40)))
        {
        }
        
        if (GUILayout.Button("💾 강제 저장", GUILayout.Height(40)))
        {
            AccountDataManager.Instance.Save();
            PlayerDataManager.Instance.SaveCurrentSlot();
        }
        
        GUILayout.EndHorizontal();
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 액션 메서드
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private void CreateAndAddToBag()
    {
        if (!CanUseSystem()) return;
        
        var account = AccountDataManager.Instance;
        var playerData = PlayerDataManager.Instance;
        
        // 아이템 생성
        var itemId = account.RegisterNewInstance(newItemTemplate);
        
        // 강화 레벨 설정
        var itemData = account.GetInstance(itemId);
        if (itemData != null)
        {
            itemData.enhancementLevel = newItemEnhancement;
        }
        
        // 가방에 추가
        var slotData = playerData.GetSlotData(playerData.CurrentSlotIndex);
        slotData.characterBagInstanceIds.Add(itemId);
        playerData.SaveSlotData(slotData);
        
        account.Save();
        
    }
    
    private void CreateAndAddToStorage()
    {
        if (!CanUseSystem()) return;
        
        var account = AccountDataManager.Instance;
        
        // 아이템 생성
        var itemId = account.RegisterNewInstance(newItemTemplate);
        
        // 강화 레벨 설정
        var itemData = account.GetInstance(itemId);
        if (itemData != null)
        {
            itemData.enhancementLevel = newItemEnhancement;
        }
        
        // 창고에 추가
        if (account.TryAddToShared(itemId))
        {
            account.Save();
        }
        else
        {
            Debug.LogError("❌ [Debug] 창고 가득 참");
        }
    }
    
    private void CreateAndEquip()
    {
        if (!CanUseSystem()) return;
        
        var account = AccountDataManager.Instance;
        var playerData = PlayerDataManager.Instance;
        
        // 아이템 생성
        var itemId = account.RegisterNewInstance(newItemTemplate);
        
        // 강화 레벨 설정
        var itemData = account.GetInstance(itemId);
        if (itemData != null)
        {
            itemData.enhancementLevel = newItemEnhancement;
        }
        
        // 가방에 추가
        var slotData = playerData.GetSlotData(playerData.CurrentSlotIndex);
        slotData.characterBagInstanceIds.Add(itemId);
        playerData.SaveSlotData(slotData);
        
        // 장착
        if (playerData.EquipV2(itemId, EquipmentSlot.MainWeapon))
        {
        }
        else
        {
            Debug.LogError("❌ [Debug] 장착 실패");
        }
    }
    
    private void SimulateStageEnd()
    {
        if (!CanUseSystem()) return;
        
        // StageEndItemTransfer 컴포넌트 찾기 또는 생성
        var transfer = FindObjectOfType<StageEndItemTransfer>();
        
        if (transfer == null)
        {
            var go = new GameObject("StageEndItemTransfer_Debug");
            transfer = go.AddComponent<StageEndItemTransfer>();
            transfer.enableLogs = true;
        }
        
        // 전송 실행
        transfer.TransferItemsToAccount();
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 헬퍼 메서드
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private bool CanUseSystem()
    {
        return AccountDataManager.IsInitialized() && 
               PlayerDataManager.Instance != null && 
               PlayerDataManager.Instance.IsSlotSelected;
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // GUI 스타일
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    
    private GUIStyle GetHeaderStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 14;
        return style;
    }
    
    private GUIStyle GetSuccessStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.green;
        return style;
    }
    
    private GUIStyle GetErrorStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.red;
        return style;
    }
    
    private GUIStyle GetBoundStyle()
    {
        var style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.yellow;
        return style;
    }
}

