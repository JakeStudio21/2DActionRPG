using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 개별 스테이지 버튼 UI 컴포넌트
/// Phase 6 Step 2: 스테이지 버튼 시각화 및 상태 관리
/// </summary>
public class StageButtonUI : MonoBehaviour
{
    [Header("=== UI 요소 ===")]
    [Tooltip("버튼 컴포넌트")]
    public Button button;
    
    [Tooltip("버튼 배경 이미지 (색상 변경용)")]
    public Image buttonImage;
    
    [Tooltip("스테이지 번호 텍스트 (1~10)")]
    public TMP_Text stageNumberText;
    
    [Header("=== 상태 아이콘 ===")]
    [Tooltip("잠금 아이콘 (해금되지 않은 스테이지)")]
    public GameObject lockIcon;
    
    [Tooltip("클리어 아이콘 (클리어한 스테이지)")]
    public GameObject clearIcon;
    
    [Tooltip("보스 스테이지 아이콘 (Stage 10)")]
    public GameObject bossIcon;
    
    [Header("=== 색상 설정 ===")]
    [Tooltip("잠금 상태 색상")]
    public Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    
    [Tooltip("해금 상태 색상")]
    public Color unlockedColor = Color.white;
    
    [Tooltip("클리어 상태 색상")]
    public Color clearedColor = new Color(0.8f, 1f, 0.8f, 1f);
    
    [Tooltip("선택 상태 색상")]
    public Color selectedColor = new Color(1f, 1f, 0.6f, 1f);
    
    [Header("=== 디버그 ===")]
    
    // 내부 상태
    private string stageId;
    private int stageIndex;
    private bool isUnlocked;
    private bool isCleared;
    private bool isSelected;
    
    // 이벤트
    public System.Action<string> OnStageSelected;
    
    /// <summary>
    /// 버튼 초기화
    /// </summary>
    /// <param name="stageId">스테이지 ID (예: CH01_ST01)</param>
    /// <param name="stageIndex">스테이지 번호 (1~10)</param>
    /// <param name="isUnlocked">해금 여부</param>
    /// <param name="isCleared">클리어 여부</param>
    /// <param name="isBoss">보스 스테이지 여부</param>
    public void Setup(string stageId, int stageIndex, bool isUnlocked, bool isCleared, bool isBoss)
    {
        this.stageId = stageId;
        this.stageIndex = stageIndex;
        this.isUnlocked = isUnlocked;
        this.isCleared = isCleared;
        
        // 스테이지 번호 텍스트
        if (stageNumberText != null)
        {
            stageNumberText.text = stageIndex.ToString();
        }
        
        // 버튼 클릭 이벤트
        if (button != null)
        {
            button.interactable = isUnlocked;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnButtonClicked());
        }
        
        // 보스 아이콘 표시
        if (bossIcon != null)
        {
            bossIcon.SetActive(isBoss);
        }
        
        // 시각적 상태 업데이트
        UpdateVisualState(isUnlocked, isCleared, false);
        
    }
    
    /// <summary>
    /// 시각적 상태 업데이트
    /// </summary>
    /// <param name="isUnlocked">해금 여부</param>
    /// <param name="isCleared">클리어 여부</param>
    /// <param name="isSelected">선택 여부</param>
    public void UpdateVisualState(bool isUnlocked, bool isCleared, bool isSelected)
    {
        this.isUnlocked = isUnlocked;
        this.isCleared = isCleared;
        this.isSelected = isSelected;
        
        // 잠금 아이콘
        if (lockIcon != null)
        {
            lockIcon.SetActive(!isUnlocked);
        }
        
        // 클리어 아이콘
        if (clearIcon != null)
        {
            clearIcon.SetActive(isCleared);
        }
        
        // 버튼 색상
        if (buttonImage != null)
        {
            if (isSelected)
            {
                buttonImage.color = selectedColor;
            }
            else if (isCleared)
            {
                buttonImage.color = clearedColor;
            }
            else if (isUnlocked)
            {
                buttonImage.color = unlockedColor;
            }
            else
            {
                buttonImage.color = lockedColor;
            }
        }
        
        // 버튼 상호작용 가능 여부
        if (button != null)
        {
            button.interactable = isUnlocked;
        }
        
    }
    
    /// <summary>
    /// 버튼 클릭 처리
    /// </summary>
    private void OnButtonClicked()
    {
        if (!isUnlocked)
        {
            return;
        }
        
        
        // 이벤트 발생
        OnStageSelected?.Invoke(stageId);
    }
    
    /// <summary>
    /// 선택 상태 설정
    /// </summary>
    public void SetSelected(bool selected)
    {
        UpdateVisualState(isUnlocked, isCleared, selected);
    }
    
    /// <summary>
    /// 현재 스테이지 ID 가져오기
    /// </summary>
    public string GetStageId()
    {
        return stageId;
    }
    
    /// <summary>
    /// 현재 스테이지 번호 가져오기
    /// </summary>
    public int GetStageIndex()
    {
        return stageIndex;
    }
    
    /// <summary>
    /// 해금 여부 가져오기
    /// </summary>
    public bool IsUnlocked()
    {
        return isUnlocked;
    }
    
    /// <summary>
    /// 클리어 여부 가져오기
    /// </summary>
    public bool IsCleared()
    {
        return isCleared;
    }
    
    #region Unity Editor Helper
    
    private void OnValidate()
    {
#if UNITY_EDITOR
        // 필수 컴포넌트 체크
        if (button == null)
        {
            button = GetComponent<Button>();
        }
        
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }
#endif
    }
    
    #endregion
}

