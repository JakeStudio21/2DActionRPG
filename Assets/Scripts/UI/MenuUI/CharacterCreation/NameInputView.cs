using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 이름 입력 View 컴포넌트
/// 책임: 이름 입력 UI 표시, 에러 메시지 표시, 사용자 입력 처리
/// </summary>
public class NameInputView : MonoBehaviour
{
    [Header("=== 이름 입력 UI ===")]
    public TMP_InputField nameInputField;
    public Button nameConfirmButton;
    public Button nameCancelButton;
    
    [Header("=== 오류 메시지 UI ===")]
    public GameObject messageError;
    public Image errorIcon;
    public TMP_Text errorText;
    
    [Header("=== 패널 참조 ===")]
    public GameObject panel;
    
    // 이벤트
    public event Action<string> OnNameConfirmed;
    public event Action OnNameCancelled;
    
    void Start()
    {
        // 버튼 이벤트 연결
        if (nameConfirmButton != null) 
            nameConfirmButton.onClick.AddListener(ConfirmName);
        if (nameCancelButton != null) 
            nameCancelButton.onClick.AddListener(CancelName);
        
        // 초기 에러 메시지 숨김
        ClearError();
    }
    
    /// <summary>
    /// 패널 표시
    /// </summary>
    public void ShowPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
            
            // 입력 필드 초기화 및 포커스
            if (nameInputField != null)
            {
                nameInputField.text = "";
                nameInputField.Select();
            }
            
            ClearError();
            Debug.Log("[NameInputView] 패널 표시");
        }
    }
    
    /// <summary>
    /// 패널 숨기기
    /// </summary>
    public void HidePanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
            ClearError();
            Debug.Log("[NameInputView] 패널 숨김");
        }
    }
    
    /// <summary>
    /// 입력된 이름 반환
    /// </summary>
    public string GetInputName()
    {
        return nameInputField?.text?.Trim() ?? "";
    }
    
    /// <summary>
    /// 이름 확정
    /// </summary>
    private void ConfirmName()
    {
        string inputName = GetInputName();
        OnNameConfirmed?.Invoke(inputName);
        Debug.Log($"[NameInputView] 이름 확정: '{inputName}'");
    }
    
    /// <summary>
    /// 이름 입력 취소
    /// </summary>
    private void CancelName()
    {
        OnNameCancelled?.Invoke();
        Debug.Log("[NameInputView] 이름 입력 취소");
    }
    
    /// <summary>
    /// 에러 메시지 표시
    /// </summary>
    public void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
        }
        
        if (messageError != null)
        {
            messageError.SetActive(true);
        }
        
        Debug.LogWarning($"[NameInputView] 에러 표시: {message}");
    }
    
    /// <summary>
    /// 에러 메시지 지우기
    /// </summary>
    public void ClearError()
    {
        if (errorText != null)
        {
            errorText.text = "";
        }
        
        if (messageError != null)
        {
            messageError.SetActive(false);
        }
    }
    
    /// <summary>
    /// 입력 필드에 포커스
    /// </summary>
    public void FocusInput()
    {
        if (nameInputField != null)
        {
            nameInputField.Select();
        }
    }
}

