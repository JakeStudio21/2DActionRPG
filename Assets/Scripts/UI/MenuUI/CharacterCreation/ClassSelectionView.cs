using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 클래스 선택 View 컴포넌트
/// 책임: 클래스 선택 UI 표시 및 사용자 입력 처리
/// </summary>
public class ClassSelectionView : MonoBehaviour
{
    [Header("=== 클래스 선택 버튼 ===")]
    public Button warriorButton;
    public Button assassinButton;
    public Button wizardButton;
    public Button classConfirmButton;
    
    [Header("=== 클래스 정보 표시 ===")]
    public TMP_Text classNameText;
    public TMP_Text attackText;
    public TMP_Text defenseText;
    public TMP_Text speedText;
    public TMP_Text descriptionText;
    
    [Header("=== 패널 참조 ===")]
    public GameObject panel;
    
    // 이벤트
    public event Action<PlayerType> OnClassSelected;
    public event Action<PlayerType> OnClassConfirmed;
    
    // 상태
    private PlayerType selectedClass = PlayerType.Warrior;
    
    void Start()
    {
        // 버튼 이벤트 연결
        if (warriorButton != null) 
            warriorButton.onClick.AddListener(() => SelectClass(PlayerType.Warrior));
        if (assassinButton != null) 
            assassinButton.onClick.AddListener(() => SelectClass(PlayerType.Assasin));
        if (wizardButton != null) 
            wizardButton.onClick.AddListener(() => SelectClass(PlayerType.Wizard));
        if (classConfirmButton != null) 
            classConfirmButton.onClick.AddListener(ConfirmClass);
        
        // 기본 클래스 선택
        SelectClass(PlayerType.Warrior);
    }
    
    /// <summary>
    /// 패널 표시
    /// </summary>
    public void ShowPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
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
        }
    }
    
    /// <summary>
    /// 클래스 선택
    /// </summary>
    private void SelectClass(PlayerType playerType)
    {
        selectedClass = playerType;
        OnClassSelected?.Invoke(playerType);
        
    }
    
    /// <summary>
    /// 클래스 확정
    /// </summary>
    private void ConfirmClass()
    {
        OnClassConfirmed?.Invoke(selectedClass);
    }
    
    /// <summary>
    /// 클래스 정보 업데이트
    /// </summary>
    public void UpdateClassInfo(ClassInfo info)
    {
        if (info == null) return;
        
        if (classNameText != null) classNameText.text = info.className;
        if (attackText != null) attackText.text = $"공격력: {info.attack}";
        if (defenseText != null) defenseText.text = $"방어력: {info.defense}";
        if (speedText != null) speedText.text = $"이동속도: {info.speed}";
        if (descriptionText != null) descriptionText.text = info.description;
    }
    
    /// <summary>
    /// 현재 선택된 클래스 반환
    /// </summary>
    public PlayerType GetSelectedClass()
    {
        return selectedClass;
    }
}

