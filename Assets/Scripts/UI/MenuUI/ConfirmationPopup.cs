using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 범용 확인 팝업 컨트롤러
/// 캐릭터 삭제, 게임 종료 등 확인이 필요한 모든 작업에 재사용 가능
/// </summary>
public class ConfirmationPopup : MonoBehaviour
{
    [Header("=== 팝업 UI 참조 ===")]
    [Tooltip("전체 팝업 패널 (이것을 활성화/비활성화)")]
    public GameObject popupPanel;
    
    [Tooltip("어두운 배경 (다른 UI 클릭 방지)")]
    public GameObject backgroundDim;
    
    [Header("=== 텍스트 UI ===")]
    [Tooltip("팝업 제목 (예: '캐릭터 삭제')")]
    public TMP_Text titleText;
    
    [Tooltip("주요 메시지 (예: '정말 삭제하시겠습니까?')")]
    public TMP_Text messageText;
    
    [Tooltip("상세 정보 (예: '홍길동 (Lv.5, Warrior)')")]
    public TMP_Text detailText;
    
    [Header("=== 버튼 UI ===")]
    [Tooltip("확인 버튼")]
    public Button confirmButton;
    
    [Tooltip("취소 버튼")]
    public Button cancelButton;
    
    // 콜백 함수들
    private Action onConfirmCallback;
    private Action onCancelCallback;
    
    void Start()
    {
        InitializeUI();
    }
    
    /// <summary>
    /// UI 초기화 및 버튼 이벤트 연결
    /// </summary>
    private void InitializeUI()
    {
        Debug.Log("[ConfirmationPopup] 초기화 시작");
        
        // 버튼 이벤트 연결
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
        else
        {
            Debug.LogError("[ConfirmationPopup] ❌ confirmButton이 null입니다! Inspector에서 연결해주세요.");
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }
        else
        {
            Debug.LogError("[ConfirmationPopup] ❌ cancelButton이 null입니다! Inspector에서 연결해주세요.");
        }
        
        // 🎯 초기 상태: 그대로 유지 (Panel_Lobby 바로 뒤에 배치되어 있음)
        // Hide() 호출 불필요 - Hierarchy에서 이미 올바른 위치에 배치됨
        
        Debug.Log("[ConfirmationPopup] ✅ 초기화 완료 (현재 위치 유지)");
    }
    
    /// <summary>
    /// 확인 팝업 표시 (Z-Order 방식 - ShopPanel 패턴)
    /// </summary>
    /// <param name="title">팝업 제목</param>
    /// <param name="message">주요 메시지</param>
    /// <param name="detail">상세 정보 (optional)</param>
    /// <param name="onConfirm">확인 버튼 클릭 시 실행할 콜백</param>
    /// <param name="onCancel">취소 버튼 클릭 시 실행할 콜백 (optional)</param>
    public void Show(string title, string message, string detail, Action onConfirm, Action onCancel = null)
    {
        Debug.Log($"[ConfirmationPopup] 팝업 표시: {title}");
        
        // 텍스트 설정
        if (titleText != null)
        {
            titleText.text = title;
        }
        
        if (messageText != null)
        {
            messageText.text = message;
        }
        
        if (detailText != null)
        {
            // 상세 정보가 비어있으면 텍스트 오브젝트 비활성화
            if (!string.IsNullOrEmpty(detail))
            {
                detailText.text = detail;
                detailText.gameObject.SetActive(true);
            }
            else
            {
                detailText.gameObject.SetActive(false);
            }
        }
        
        // 콜백 등록
        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;
        
        // 🎯 핵심 수정: ConfirmationPopup 자체를 최상위로! (ShopPanel 패턴)
        // BackgroundDim, PopupPanel은 자식이므로 부모만 이동시키면 됨!
        transform.SetAsLastSibling();
        
        Debug.Log($"[ConfirmationPopup] 🔝 ConfirmationPopup 전체를 최상위로 이동 (Sibling Index: {transform.GetSiblingIndex()})");
        Debug.Log("[ConfirmationPopup] ✅ 팝업 표시 완료 (Z-Order 방식)");
    }
    
    /// <summary>
    /// 간단한 확인 팝업 (상세 정보 없이)
    /// </summary>
    public void Show(string title, string message, Action onConfirm, Action onCancel = null)
    {
        Show(title, message, "", onConfirm, onCancel);
    }
    
    /// <summary>
    /// 팝업 숨기기 (콜백 초기화만, Z-Order 이동 없음)
    /// </summary>
    public void Hide()
    {
        Debug.Log("[ConfirmationPopup] 팝업 숨기기");
        
        // 🎯 콜백만 초기화 (Z-Order는 그대로 유지)
        // ShowLobbyPanel() 등이 호출되면 자연스럽게 뒤로 밀림
        onConfirmCallback = null;
        onCancelCallback = null;
        
        Debug.Log("[ConfirmationPopup] ✅ 팝업 콜백 초기화 완료");
    }
    
    /// <summary>
    /// 확인 버튼 클릭 이벤트
    /// </summary>
    private void OnConfirmButtonClicked()
    {
        Debug.Log("[ConfirmationPopup] 확인 버튼 클릭");
        
        // 콜백 실행
        if (onConfirmCallback != null)
        {
            onConfirmCallback.Invoke();
            Debug.Log("[ConfirmationPopup] ✅ 확인 콜백 실행 완료");
        }
        else
        {
            Debug.LogWarning("[ConfirmationPopup] ⚠️ 확인 콜백이 null입니다.");
        }
        
        // 팝업 닫기
        Hide();
    }
    
    /// <summary>
    /// 취소 버튼 클릭 이벤트
    /// </summary>
    private void OnCancelButtonClicked()
    {
        Debug.Log("[ConfirmationPopup] 취소 버튼 클릭");
        
        // 콜백 실행 (있으면)
        if (onCancelCallback != null)
        {
            onCancelCallback.Invoke();
            Debug.Log("[ConfirmationPopup] ✅ 취소 콜백 실행 완료");
        }
        
        // 팝업 닫기
        Hide();
    }
    
    /// <summary>
    /// 🔍 디버그: 현재 팝업 상태 확인
    /// </summary>
    public void DebugCurrentState()
    {
        Debug.Log($"[ConfirmationPopup] === 현재 상태 ===");
        Debug.Log($"popupPanel: {(popupPanel != null ? popupPanel.activeSelf.ToString() : "null")}");
        Debug.Log($"backgroundDim: {(backgroundDim != null ? backgroundDim.activeSelf.ToString() : "null")}");
        Debug.Log($"onConfirmCallback: {(onConfirmCallback != null ? "등록됨" : "null")}");
        Debug.Log($"onCancelCallback: {(onCancelCallback != null ? "등록됨" : "null")}");
    }
}

