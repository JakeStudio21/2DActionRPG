using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// 룬 장착 슬롯 UI
/// ⚙️ Phase 6.5: 3개의 룬 장착 슬롯
/// 
/// 역할:
/// - 장착된 룬 아이콘 표시
/// - 빈 슬롯 표시
/// - 클릭 시 상세 정보 표시
/// 
/// 위치:
/// - RuneSubPanel > LeftPanel > RuneEquipSlot0~2
/// 
/// 참고:
/// - 스킬 시스템의 SkillEquipSlotUI와 동일한 구조
/// </summary>
public class RuneEquipSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("🎯 슬롯 정보")]
    [SerializeField] private int slotIndex = 0;
    
    [Header("🎨 UI 요소 (반드시 연결)")]
    [Tooltip("룬 아이콘 이미지")]
    [SerializeField] public Image runeIcon;
    
    [Tooltip("슬롯 배경 이미지")]
    [SerializeField] public Image backgroundImage;
    
    [Tooltip("슬롯 번호 텍스트 (예: 슬롯 1)")]
    [SerializeField] public TextMeshProUGUI slotNumberText;
    
    [Tooltip("룬 레벨 텍스트 (예: Lv.10)")]
    [SerializeField] public TextMeshProUGUI runeLevelText;
    
    [Tooltip("빈 슬롯 표시 오버레이")]
    [SerializeField] public GameObject emptyOverlay;
    
    [Header("⭐ 한계돌파 표시")]
    [Tooltip("한계돌파 별 이미지 배열 (최대 5개)")]
    [SerializeField] public Image[] limitBreakStars;
    
    [Tooltip("활성화된 별 색상")]
    [SerializeField] public Color activeStarColor = Color.yellow;
    
    [Tooltip("비활성화된 별 색상")]
    [SerializeField] public Color inactiveStarColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    
    [Header("🎨 기본 스프라이트")]
    [Tooltip("빈 슬롯용 기본 스프라이트")]
    [SerializeField] public Sprite emptySlotSprite;
    
    [Header("🎨 상태별 색상")]
    [SerializeField] private Color equippedColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color emptyColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    
    [Header("🔗 데이터")]
    private RuneInstance equippedRune;
    private RunePanelUI panelUI;
    
    [Header("🔧 디버그")]
    public bool showDebugLogs = false;
    
    void Start()
    {
        // 슬롯 번호 표시
        if (slotNumberText != null)
        {
            slotNumberText.text = $"슬롯 {slotIndex + 1}";
        }
        
        UpdateUI();
    }
    
    /// <summary>
    /// 초기화
    /// </summary>
    public void Setup(int index, RunePanelUI controller)
    {
        slotIndex = index;
        panelUI = controller;
        
        UpdateUI();
    }
    
    /// <summary>
    /// 슬롯에 룬 장착
    /// </summary>
    public void SetRune(RuneInstance rune)
    {
        equippedRune = rune;
        UpdateUI();
    }
    
    /// <summary>
    /// UI 갱신
    /// </summary>
    public void UpdateUI()
    {
        if (equippedRune != null && equippedRune.baseData != null)
        {
            // 룬 아이콘 표시
            if (runeIcon != null)
            {
                if (equippedRune.baseData.icon != null)
                {
                    runeIcon.sprite = equippedRune.baseData.icon;
                    runeIcon.color = Color.white;
                }
                else
                {
                    runeIcon.sprite = emptySlotSprite;
                    runeIcon.color = emptyColor;
                }
            }
            
            // 레벨 표시
            if (runeLevelText != null)
            {
                runeLevelText.text = $"Lv.{equippedRune.currentLevel}";
                runeLevelText.gameObject.SetActive(true);
            }
            
            // 한계돌파 별 표시
            UpdateLimitBreakStars(equippedRune.currentLimitBreak);
            
            // Empty 오버레이 끄기
            if (emptyOverlay != null)
                emptyOverlay.SetActive(false);
            
            // 배경 색상 (장착됨)
            if (backgroundImage != null)
                backgroundImage.color = equippedColor;
            
            if (showDebugLogs)
                Debug.Log($"[RuneEquipSlotUI] 슬롯 {slotIndex}: {equippedRune}");
        }
        else
        {
            // 빈 슬롯 표시
            if (runeIcon != null)
            {
                runeIcon.sprite = emptySlotSprite;
                runeIcon.color = emptyColor;
            }
            
            if (runeLevelText != null)
                runeLevelText.gameObject.SetActive(false);
            
            // 한계돌파 별 숨김
            UpdateLimitBreakStars(0);
            
            if (emptyOverlay != null)
                emptyOverlay.SetActive(true);
            
            // 배경 색상 (비어있음)
            if (backgroundImage != null)
                backgroundImage.color = emptyColor;
            
            if (showDebugLogs)
                Debug.Log($"[RuneEquipSlotUI] 슬롯 {slotIndex}: 비어있음");
        }
    }
    
    /// <summary>
    /// 슬롯 클릭 이벤트 (Phase 7-2: 하단창에 상세 정보 표시)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (equippedRune != null && panelUI != null)
        {
            // 장착된 룬 클릭 시 하단창에 상세 정보 표시
            panelUI.ShowEquippedRuneDetail(equippedRune);
            
            if (showDebugLogs)
                Debug.Log($"[RuneEquipSlotUI] 슬롯 {slotIndex} 클릭: {equippedRune}");
        }
        else
        {
            if (showDebugLogs)
                Debug.Log($"[RuneEquipSlotUI] 슬롯 {slotIndex} 클릭: 비어있음");
        }
    }
    
    /// <summary>
    /// 슬롯 비우기
    /// </summary>
    public void Clear()
    {
        equippedRune = null;
        UpdateUI();
    }
    
    /// <summary>
    /// 장착된 룬 반환
    /// </summary>
    public RuneInstance GetEquippedRune()
    {
        return equippedRune;
    }
    
    /// <summary>
    /// 슬롯 인덱스 반환
    /// </summary>
    public int GetSlotIndex()
    {
        return slotIndex;
    }
    
    /// <summary>
    /// 한계돌파 별 업데이트
    /// </summary>
    private void UpdateLimitBreakStars(int limitBreakCount)
    {
        if (limitBreakStars == null || limitBreakStars.Length == 0)
            return;
        
        for (int i = 0; i < limitBreakStars.Length; i++)
        {
            if (limitBreakStars[i] != null)
            {
                if (i < limitBreakCount)
                {
                    // 활성화된 별
                    limitBreakStars[i].color = activeStarColor;
                    limitBreakStars[i].gameObject.SetActive(true);
                }
                else
                {
                    // 비활성화된 별
                    limitBreakStars[i].color = inactiveStarColor;
                    limitBreakStars[i].gameObject.SetActive(false);
                }
            }
        }
    }
    
    /// <summary>
    /// [Deprecated] 하위 호환성을 위한 메서드
    /// UpdateUI()를 호출합니다.
    /// </summary>
    [System.Obsolete("Use UpdateUI() instead")]
    public void RefreshSlot()
    {
        UpdateUI();
    }
}
