using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// ⚠️⚠️⚠️ [DEPRECATED - Phase 6] ⚠️⚠️⚠️
// 이 파일은 Phase 6 UI 리팩토링으로 인해 더 이상 사용되지 않습니다.
// 
// Phase 6에서는 드래그 앤 드롭 방식이 제거되고,
// 스킬 시스템처럼 "장착" 버튼 방식으로 재구현될 예정입니다 (Phase 7).
// 
// 이 파일은 참고용으로만 보관되며, 실제 게임에서는 사용되지 않습니다.
// ⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️⚠️

/// <summary>
/// [DEPRECATED] 룬 드래그 핸들러
/// ⚙️ Phase 5-2: 장착 및 강화 UI
/// 
/// 역할:
/// - 인벤토리 룬 슬롯을 드래그하여 장착 슬롯에 드롭
/// - 드래그 중 임시 아이콘 표시
/// - 드롭 실패 시 원위치 복귀
/// 
/// 중요:
/// ⚠️ 데이터를 수정하지 않고 드래그 이벤트만 처리
/// ⚠️ 실제 장착은 RuneEquipSlotUI의 OnDrop에서 처리
/// </summary>
[System.Obsolete("Phase 6에서 제거됨. 드래그 앤 드롭은 Phase 7에서 버튼 방식으로 재구현 예정.", false)]
public class RuneDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    #region UI 컴포넌트
    
    [Header("=== 드래그 설정 ===")]
    [Tooltip("드래그 중 마우스를 따라다닐 임시 아이콘의 부모 (보통 Canvas)")]
    [SerializeField] private Transform dragParent;
    
    [Tooltip("드래그 중 원본 슬롯의 투명도 (0.0 = 완전 투명, 1.0 = 불투명)")]
    [SerializeField] [Range(0f, 1f)] private float draggedSlotAlpha = 0.3f;
    
    [Header("=== 디버그 ===")]
    [SerializeField] private bool showDebugLogs = false;
    
    #endregion
    
    #region 내부 데이터
    
    private RuneSlotUI runeSlotUI;
    private CanvasGroup canvasGroup;
    private GameObject dragIcon;
    private Vector3 originalPosition;
    private Transform originalParent;
    
    #endregion
    
    #region 초기화
    
    private void Awake()
    {
        runeSlotUI = GetComponent<RuneSlotUI>();
        
        // CanvasGroup 확인 (없으면 추가)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // dragParent가 없으면 최상위 Canvas 찾기
        if (dragParent == null)
        {
            Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (rootCanvas != null)
            {
                dragParent = rootCanvas.transform;
            }
            else
            {
                Debug.LogWarning("[RuneDragHandler] Canvas를 찾을 수 없습니다. 드래그가 제대로 작동하지 않을 수 있습니다.");
            }
        }
    }
    
    #endregion
    
    #region IBeginDragHandler, IDragHandler, IEndDragHandler
    
    /// <summary>
    /// 드래그 시작
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        var rune = runeSlotUI?.GetRuneInstance();
        if (rune == null)
        {
            Debug.LogWarning("[RuneDragHandler] 드래그할 룬이 없습니다.");
            eventData.pointerDrag = null; // 드래그 취소
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RuneDragHandler] 드래그 시작: {rune}");
        }
        
        // 원본 슬롯 반투명 처리
        canvasGroup.alpha = draggedSlotAlpha;
        canvasGroup.blocksRaycasts = false; // 드롭 타겟이 이벤트를 받을 수 있도록
        
        // 드래그용 임시 아이콘 생성
        CreateDragIcon();
    }
    
    /// <summary>
    /// 드래그 중 (마우스 이동)
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            // 마우스 커서를 따라다님
            dragIcon.transform.position = eventData.position;
        }
    }
    
    /// <summary>
    /// 드래그 종료
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        // 원본 슬롯 복구
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        // 임시 아이콘 제거
        if (dragIcon != null)
        {
            Destroy(dragIcon);
            dragIcon = null;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RuneDragHandler] 드래그 종료");
        }
    }
    
    #endregion
    
    #region 드래그 아이콘 생성
    
    /// <summary>
    /// 드래그용 임시 아이콘 생성
    /// </summary>
    private void CreateDragIcon()
    {
        if (dragParent == null)
        {
            Debug.LogWarning("[RuneDragHandler] dragParent가 null입니다.");
            return;
        }
        
        // 원본 슬롯의 이미지 컴포넌트 찾기
        var originalImage = GetComponentInChildren<Image>();
        if (originalImage == null || originalImage.sprite == null)
        {
            Debug.LogWarning("[RuneDragHandler] 원본 이미지를 찾을 수 없습니다.");
            return;
        }
        
        // 임시 GameObject 생성
        dragIcon = new GameObject("DragIcon");
        dragIcon.transform.SetParent(dragParent, false);
        dragIcon.transform.SetAsLastSibling(); // 최상위에 표시
        
        // Image 컴포넌트 추가
        var image = dragIcon.AddComponent<Image>();
        image.sprite = originalImage.sprite;
        image.raycastTarget = false; // 드롭 타겟이 이벤트를 받을 수 있도록
        
        // 크기 설정 (원본과 동일)
        var rectTransform = dragIcon.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100); // 고정 크기
        
        // 반투명 처리
        var canvasGroup = dragIcon.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// 현재 드래그 중인 룬 인스턴스 반환
    /// </summary>
    public RuneInstance GetDraggedRune()
    {
        return runeSlotUI?.GetRuneInstance();
    }
    
    #endregion
}
