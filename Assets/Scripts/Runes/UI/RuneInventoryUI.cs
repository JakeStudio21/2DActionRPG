using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ⚠️⚠️⚠️ [DEPRECATED - Phase 6] ⚠️⚠️⚠️
// 이 파일은 Phase 6 UI 리팩토링으로 인해 더 이상 사용되지 않습니다.
// 대체: RunePanelUI.cs
// 
// Phase 6에서는 스킬북 형태의 단일 패널 구조로 전환되었습니다.
// - 기존: 분리된 Inventory + Enhance + Tooltip
// - 신규: 통합된 RunePanelUI + RuneDetailUI
// 
// 이 파일은 참고용으로만 보관되며, 실제 게임에서는 사용되지 않습니다.
// ⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️

/// <summary>
/// [DEPRECATED] 룬 인벤토리 UI (목록 표시)
/// ⚙️ Phase 5-1: 룬 UI 시스템
/// 
/// 역할:
/// - RuneInventoryManager에서 전체 룬 목록 가져오기
/// - RuneSlotUI 프리팹을 동적으로 생성 및 배치
/// - 룬 클릭 시 RuneTooltipUI에 상세 정보 표시
/// - 첫 번째 룬 자동 선택
/// 
/// 중요:
/// ⚠️ 데이터를 수정하지 않고 오직 읽기만!
/// ⚠️ 매니저와 Instance의 데이터를 화면에 표시하는 View 역할만 수행
/// </summary>
[System.Obsolete("Phase 6에서 RunePanelUI로 대체됨. 이 클래스는 더 이상 사용되지 않습니다.", false)]
public class RuneInventoryUI : MonoBehaviour
{
    #region UI 컴포넌트
    
    [Header("=== 스크롤 뷰 ===")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentTransform;
    
    [Header("=== 프리팹 ===")]
    [SerializeField] private GameObject runeSlotPrefab;
    
    [Header("=== 툴팁 ===")]
    [SerializeField] private RuneTooltipUI runeTooltipUI;
    
    [Header("=== 인벤토리 정보 ===")]
    [SerializeField] private TextMeshProUGUI inventoryCountText;
    
    [Header("=== 디버그 ===")]
    [SerializeField] private bool showDebugLogs = false;
    
    #endregion
    
    #region 내부 데이터
    
    private List<RuneSlotUI> runeSlots = new List<RuneSlotUI>();
    private RuneSlotUI currentSelectedSlot;
    private RuneInventoryManager inventoryManager;
    private RuneManager runeManager;
    
    #endregion
    
    #region 초기화
    
    private void Awake()
    {
        // 매니저 참조
        inventoryManager = RuneInventoryManager.Instance;
        runeManager = RuneManager.Instance;
        
        if (inventoryManager == null)
        {
            Debug.LogError("[RuneInventoryUI] RuneInventoryManager를 찾을 수 없습니다!");
        }
        
        if (runeManager == null)
        {
            Debug.LogError("[RuneInventoryUI] RuneManager를 찾을 수 없습니다!");
        }
    }
    
    private void Start()
    {
        // 초기 인벤토리 표시
        RefreshInventory();
    }
    
    private void OnEnable()
    {
        // UI 활성화 시 인벤토리 갱신
        RefreshInventory();
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// 인벤토리 갱신 (전체 재구성)
    /// </summary>
    public void RefreshInventory()
    {
        if (inventoryManager == null)
        {
            Debug.LogError("[RuneInventoryUI] RuneInventoryManager가 null입니다.");
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[RuneInventoryUI] 인벤토리 갱신 시작");
        }
        
        // 1. 기존 슬롯 제거
        ClearSlots();
        
        // 2. 룬 목록 가져오기
        List<RuneInstance> allRunes = inventoryManager.GetAllRunes();
        
        if (allRunes == null || allRunes.Count == 0)
        {
            Debug.Log("[RuneInventoryUI] 인벤토리가 비어있습니다.");
            UpdateInventoryCount(0);
            
            // 툴팁 숨김
            if (runeTooltipUI != null)
            {
                runeTooltipUI.HideTooltip();
            }
            
            return;
        }
        
        // 3. 슬롯 생성
        foreach (var rune in allRunes)
        {
            CreateSlot(rune);
        }
        
        // 4. 인벤토리 개수 표시
        UpdateInventoryCount(allRunes.Count);
        
        // 5. 첫 번째 룬 자동 선택
        if (runeSlots.Count > 0)
        {
            SelectSlot(runeSlots[0]);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RuneInventoryUI] 인벤토리 갱신 완료: {allRunes.Count}개");
        }
    }
    
    /// <summary>
    /// 특정 룬 선택 (외부에서 호출 가능)
    /// </summary>
    public void SelectRuneByInstance(RuneInstance rune)
    {
        if (rune == null)
        {
            return;
        }
        
        // 해당 룬의 슬롯 찾기
        RuneSlotUI targetSlot = runeSlots.Find(slot => slot.GetRuneInstance() == rune);
        
        if (targetSlot != null)
        {
            SelectSlot(targetSlot);
        }
    }
    
    #endregion
    
    #region 슬롯 관리
    
    /// <summary>
    /// 슬롯 생성
    /// </summary>
    private void CreateSlot(RuneInstance rune)
    {
        if (runeSlotPrefab == null || contentTransform == null)
        {
            Debug.LogError("[RuneInventoryUI] 프리팹 또는 Content Transform이 null입니다.");
            return;
        }
        
        GameObject slotObj = Instantiate(runeSlotPrefab, contentTransform);
        RuneSlotUI slotUI = slotObj.GetComponent<RuneSlotUI>();
        
        if (slotUI == null)
        {
            Debug.LogError("[RuneInventoryUI] RuneSlotUI 컴포넌트를 찾을 수 없습니다.");
            Destroy(slotObj);
            return;
        }
        
        // 슬롯 초기화
        slotUI.Setup(rune, OnSlotClicked);
        
        // 장착 상태 표시
        bool isEquipped = IsRuneEquipped(rune);
        slotUI.SetEquipped(isEquipped);
        
        // 드래그 핸들러 추가 (Phase 5-2)
        var dragHandler = slotObj.GetComponent<RuneDragHandler>();
        if (dragHandler == null)
        {
            slotObj.AddComponent<RuneDragHandler>();
        }
        
        runeSlots.Add(slotUI);
    }
    
    /// <summary>
    /// 기존 슬롯 제거
    /// </summary>
    private void ClearSlots()
    {
        foreach (var slot in runeSlots)
        {
            if (slot != null && slot.gameObject != null)
            {
                Destroy(slot.gameObject);
            }
        }
        
        runeSlots.Clear();
        currentSelectedSlot = null;
    }
    
    /// <summary>
    /// 슬롯 선택
    /// </summary>
    private void SelectSlot(RuneSlotUI slot)
    {
        if (slot == null)
        {
            return;
        }
        
        // 이전 선택 해제
        if (currentSelectedSlot != null)
        {
            currentSelectedSlot.SetSelected(false);
        }
        
        // 새로운 선택
        currentSelectedSlot = slot;
        currentSelectedSlot.SetSelected(true);
        
        // 툴팁 표시
        if (runeTooltipUI != null)
        {
            runeTooltipUI.ShowTooltip(slot.GetRuneInstance());
        }
    }
    
    #endregion
    
    #region 이벤트 처리
    
    /// <summary>
    /// 슬롯 클릭 콜백
    /// </summary>
    private void OnSlotClicked(RuneInstance rune)
    {
        if (rune == null)
        {
            return;
        }
        
        // 클릭된 룬의 슬롯 찾기
        RuneSlotUI clickedSlot = runeSlots.Find(slot => slot.GetRuneInstance() == rune);
        
        if (clickedSlot != null)
        {
            SelectSlot(clickedSlot);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RuneInventoryUI] 슬롯 클릭: {rune}");
        }
    }
    
    #endregion
    
    #region UI 업데이트
    
    /// <summary>
    /// 인벤토리 개수 표시
    /// </summary>
    private void UpdateInventoryCount(int count)
    {
        if (inventoryCountText != null)
        {
            inventoryCountText.text = $"룬 보유: {count}개";
        }
    }
    
    #endregion
    
    #region 유틸리티
    
    /// <summary>
    /// 룬이 현재 장착 중인지 확인
    /// </summary>
    private bool IsRuneEquipped(RuneInstance rune)
    {
        if (runeManager == null || rune == null)
        {
            return false;
        }
        
        var equippedRunes = runeManager.GetEquippedRunes();
        
        foreach (var equippedRune in equippedRunes)
        {
            if (equippedRune == rune)
            {
                return true;
            }
        }
        
        return false;
    }
    
    #endregion
}
