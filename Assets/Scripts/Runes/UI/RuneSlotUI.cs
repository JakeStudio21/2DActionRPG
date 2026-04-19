using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 개별 룬 슬롯 UI (인벤토리 스크롤 뷰용)
/// ⚙️ Phase 6: 확정 해금 방식 (미보유/보유 상태 시각화)
/// 
/// 역할:
/// - 룬 아이콘, 레벨, 한계돌파 상태 표시
/// - 미보유 룬: 흑백 처리, 레벨 숨김
/// - 보유 룬: 컬러 표시, 레벨 및 한돌 표시
/// - 클릭 시 상세 정보 표시 (콜백)
/// 
/// 중요:
/// ⚠️ 데이터를 수정하지 않고 오직 읽기만!
/// ⚠️ RuneInstance의 데이터를 화면에 표시하는 View 역할만 수행
/// </summary>
[RequireComponent(typeof(Button))]
public class RuneSlotUI : MonoBehaviour
{
    #region UI 컴포넌트
    
    [Header("=== 기본 UI 요소 ===")]
    [SerializeField] private Image runeIconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI runeNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject selectedBorder;
    
    [Header("=== 한계돌파 표시 ===")]
    [Tooltip("한계돌파 단계를 표시할 별 이미지 배열 (최대 5개)")]
    [SerializeField] private Image[] limitBreakStars;
    
    [Tooltip("활성화된 별 색상")]
    [SerializeField] private Color activeStarColor = Color.yellow;
    
    [Tooltip("비활성화된 별 색상")]
    [SerializeField] private Color inactiveStarColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    
    [Header("=== 잠금 상태 ===")]
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private Image lockIcon;
    
    [Header("=== 상태별 색상 ===")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.5f, 1f);
    [SerializeField] private Color equippedColor = new Color(0.5f, 0.8f, 1f, 1f);
    
    [Header("=== 디버그 ===")]
    #endregion
    
    #region 내부 데이터
    
    private RuneInstance runeInstance;
    private Action<RuneInstance> onClickCallback;
    private bool isSelected = false;
    private bool isEquipped = false;
    private bool isOwned = true; // [Phase 6] 보유 여부
    
    private Button button;
    
    #endregion
    
    #region 초기화
    
    private void Awake()
    {
        // Button 컴포넌트 가져오기 (RequireComponent로 보장됨)
        button = GetComponent<Button>();
        
        if (button != null)
        {
            button.onClick.AddListener(OnSlotClicked);
        }
        else
        {
            Debug.LogError("[RuneSlotUI] Button 컴포넌트를 찾을 수 없습니다! (RequireComponent 확인 필요)");
        }
        
        // 선택 테두리 초기 상태
        if (selectedBorder != null)
        {
            selectedBorder.SetActive(false);
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// 룬 슬롯 초기화 (보유 룬)
    /// </summary>
    /// <param name="rune">표시할 룬 인스턴스</param>
    /// <param name="onClickCallback">클릭 시 호출할 콜백</param>
    public void Setup(RuneInstance rune, Action<RuneInstance> onClickCallback)
    {
        this.runeInstance = rune;
        this.onClickCallback = onClickCallback;
        this.isOwned = true;
        
        if (rune == null || rune.baseData == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        UpdateUI();
        
        {
        }
    }
    
    /// <summary>
    /// [Phase 6] 룬 슬롯 초기화 (미보유 룬 - RuneData로 표시)
    /// </summary>
    /// <param name="runeData">표시할 룬 데이터</param>
    /// <param name="onClickCallback">클릭 시 호출할 콜백 (RuneData 전달)</param>
    public void SetupAsLocked(RuneData runeData, Action<RuneData> onClickCallback)
    {
        this.runeInstance = null;
        this.onClickCallback = null;
        this.isOwned = false;
        
        if (runeData == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        gameObject.SetActive(true);
        
        // 미보유 상태 UI 표시
        UpdateUIForLockedRune(runeData, onClickCallback);
        
        {
        }
    }
    
    /// <summary>
    /// 선택 상태 설정
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (selectedBorder != null)
        {
            selectedBorder.SetActive(selected);
        }
        
        UpdateBackgroundColor();
    }
    
    /// <summary>
    /// 장착 상태 설정 (외부에서 호출)
    /// </summary>
    public void SetEquipped(bool equipped)
    {
        isEquipped = equipped;
        UpdateBackgroundColor();
    }
    
    /// <summary>
    /// UI 갱신 (외부에서 룬 데이터 변경 시 호출)
    /// </summary>
    public void RefreshUI()
    {
        UpdateUI();
    }
    
    /// <summary>
    /// 현재 룬 인스턴스 반환
    /// </summary>
    public RuneInstance GetRuneInstance()
    {
        return runeInstance;
    }
    
    #endregion
    
    #region UI 업데이트
    
    /// <summary>
    /// UI 전체 업데이트 (보유 룬)
    /// </summary>
    private void UpdateUI()
    {
        if (runeInstance == null || runeInstance.baseData == null)
        {
            return;
        }
        
        // 1. 아이콘 (컬러)
        UpdateIcon();
        
        // 2. 이름
        UpdateName();
        
        // 3. 레벨 (표시)
        UpdateLevel();
        
        // 4. 한계돌파
        UpdateLimitBreakStars();
        
        // 5. 잠금 상태
        UpdateLockState();
        
        // 6. 배경 색상
        UpdateBackgroundColor();
    }
    
    /// <summary>
    /// [Phase 6] UI 업데이트 (미보유 룬 - 흑백 처리)
    /// </summary>
    private void UpdateUIForLockedRune(RuneData runeData, Action<RuneData> onClickCallback)
    {
        // 1. 아이콘 (흑백 또는 어둡게)
        if (runeIconImage != null && runeData.icon != null)
        {
            runeIconImage.sprite = runeData.icon;
            // 흑백 처리 (Saturation = 0)
            runeIconImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }
        
        // 2. 이름
        if (runeNameText != null)
        {
            runeNameText.text = runeData.runeName;
            runeNameText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        }
        
        // 3. 레벨 텍스트 숨김 (미보유 상태)
        if (levelText != null)
        {
            levelText.gameObject.SetActive(false);
        }
        
        // 4. 한계돌파 별 숨김
        if (limitBreakStars != null)
        {
            foreach (var star in limitBreakStars)
            {
                if (star != null)
                {
                    star.gameObject.SetActive(false);
                }
            }
        }
        
        // 5. 잠금 오버레이 표시
        if (lockOverlay != null)
        {
            lockOverlay.SetActive(true);
        }
        
        if (lockIcon != null)
        {
            lockIcon.gameObject.SetActive(true);
        }
        
        // 6. 배경 색상 (어둡게)
        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        }
        
        // 7. 선택 테두리 숨김
        if (selectedBorder != null)
        {
            selectedBorder.SetActive(false);
        }
        
        // 8. 클릭 이벤트 연결 (RuneData 전달)
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                {
                }
                onClickCallback?.Invoke(runeData);
            });
        }
    }
    
    /// <summary>
    /// 아이콘 업데이트
    /// </summary>
    private void UpdateIcon()
    {
        if (runeIconImage != null && runeInstance.baseData.icon != null)
        {
            runeIconImage.sprite = runeInstance.baseData.icon;
        }
    }
    
    /// <summary>
    /// 이름 업데이트
    /// </summary>
    private void UpdateName()
    {
        if (runeNameText != null)
        {
            runeNameText.text = runeInstance.baseData.runeName;
        }
    }
    
    /// <summary>
    /// 레벨 업데이트
    /// </summary>
    private void UpdateLevel()
    {
        if (levelText != null)
        {
            int currentLevel = runeInstance.currentLevel;
            int maxLevel = runeInstance.GetCurrentMaxLevel();
            
            levelText.text = $"Lv.{currentLevel}/{maxLevel}";
            
            // 최대 레벨 도달 시 색상 변경
            if (currentLevel >= maxLevel)
            {
                levelText.color = Color.yellow;
            }
            else
            {
                levelText.color = Color.white;
            }
        }
    }
    
    /// <summary>
    /// 한계돌파 별 업데이트
    /// </summary>
    private void UpdateLimitBreakStars()
    {
        if (limitBreakStars == null || limitBreakStars.Length == 0)
        {
            return;
        }
        
        int currentLimitBreak = runeInstance.currentLimitBreak;
        int maxLimitBreak = runeInstance.baseData.maxLimitBreak;
        
        for (int i = 0; i < limitBreakStars.Length; i++)
        {
            if (limitBreakStars[i] == null) continue;
            
            // i번째 별 활성화 여부 (0-based)
            bool isActive = i < currentLimitBreak;
            
            // 별이 최대 한계돌파 범위 내인지 확인
            if (i < maxLimitBreak)
            {
                limitBreakStars[i].gameObject.SetActive(true);
                limitBreakStars[i].color = isActive ? activeStarColor : inactiveStarColor;
            }
            else
            {
                // 범위 초과 시 숨김
                limitBreakStars[i].gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// 잠금 상태 업데이트
    /// </summary>
    private void UpdateLockState()
    {
        bool isLocked = runeInstance.isLocked;
        
        if (lockOverlay != null)
        {
            lockOverlay.SetActive(isLocked);
        }
        
        if (lockIcon != null)
        {
            lockIcon.gameObject.SetActive(isLocked);
        }
    }
    
    /// <summary>
    /// 배경 색상 업데이트
    /// </summary>
    private void UpdateBackgroundColor()
    {
        if (backgroundImage == null) return;
        
        if (isSelected)
        {
            backgroundImage.color = selectedColor;
        }
        else if (isEquipped)
        {
            backgroundImage.color = equippedColor;
        }
        else
        {
            backgroundImage.color = normalColor;
        }
    }
    
    #endregion
    
    #region 이벤트 처리
    
    /// <summary>
    /// 슬롯 클릭 이벤트
    /// </summary>
    private void OnSlotClicked()
    {
        if (runeInstance == null)
        {
            Debug.LogWarning("[RuneSlotUI] 룬 인스턴스가 null입니다.");
            return;
        }
        
        {
        }
        
        // 콜백 호출 (툴팁 표시)
        onClickCallback?.Invoke(runeInstance);
    }
    
    #endregion
}
