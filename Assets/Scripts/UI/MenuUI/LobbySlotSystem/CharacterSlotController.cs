using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 캐릭터 슬롯 관리 컨트롤러
/// 책임: 3개 슬롯 UI 관리, 슬롯 클릭/삭제, 슬롯 갱신
/// </summary>
public class CharacterSlotController : MonoBehaviour
{
    [Header("=== 슬롯 UI 요소 (3개) ===")]
    public CharacterSlotItem[] slotItems = new CharacterSlotItem[3];
    
    [Header("=== 클래스 아이콘 ===")]
    public Sprite warriorIcon;
    public Sprite assassinIcon;
    public Sprite wizardIcon;
    public Sprite emptySlotIcon;
    
    // 이벤트
    public event Action<int> OnSlotClicked;
    public event Action<int> OnDeleteClicked;
    public event Action<int> OnSlotSelected;
    
    // 현재 선택된 슬롯
    private int selectedSlotIndex = -1;
    
    /// <summary>
    /// 초기화
    /// </summary>
    public void Initialize()
    {
        
        // 슬롯 아이템 검증
        for (int i = 0; i < slotItems.Length; i++)
        {
            if (slotItems[i] == null)
            {
                Debug.LogError($"[CharacterSlotController] slotItems[{i}]가 null입니다!");
            }
            else
            {
                // 슬롯 아이템 초기화
                slotItems[i].Initialize(i, this);
            }
        }
        
    }
    
    /// <summary>
    /// 모든 슬롯 UI 새로고침
    /// </summary>
    public void RefreshAllSlots()
    {
        
        for (int i = 0; i < 3; i++)
        {
            RefreshSlot(i);
        }
    }
    
    /// <summary>
    /// 특정 슬롯 UI 새로고침
    /// </summary>
    public void RefreshSlot(int slotIndex)
    {
        if (PlayerDataManager.Instance == null) return;
        if (slotIndex < 0 || slotIndex >= slotItems.Length) return;
        if (slotItems[slotIndex] == null) return;
        
        var slotData = PlayerDataManager.Instance.GetSlotData(slotIndex);
        
        if (slotData != null && slotData.isSlotUsed)
        {
            // 캐릭터 있음 - 캐릭터 정보 표시
            Sprite classIcon = GetClassIcon(slotData.playerType);
            bool isSelected = (selectedSlotIndex == slotIndex);
            slotItems[slotIndex].ShowCharacter(slotData, classIcon, isSelected);
        }
        else
        {
            // 빈 슬롯 - 빈 슬롯 UI 표시
            slotItems[slotIndex].ShowEmpty(emptySlotIcon);
        }
    }
    
    /// <summary>
    /// 슬롯 선택
    /// </summary>
    public void SelectSlot(int slotIndex)
    {
        // 이전 선택 해제
        if (selectedSlotIndex >= 0 && selectedSlotIndex < slotItems.Length)
        {
            RefreshSlot(selectedSlotIndex);
        }
        
        // 새 슬롯 선택
        selectedSlotIndex = slotIndex;
        RefreshSlot(slotIndex);
        
        // 이벤트 발행
        OnSlotSelected?.Invoke(slotIndex);
        
    }
    
    /// <summary>
    /// 현재 선택된 슬롯 인덱스 반환
    /// </summary>
    public int GetSelectedSlotIndex()
    {
        return selectedSlotIndex;
    }
    
    /// <summary>
    /// 클래스별 아이콘 가져오기
    /// </summary>
    private Sprite GetClassIcon(PlayerType playerType)
    {
        switch (playerType)
        {
            case PlayerType.Warrior: return warriorIcon;
            case PlayerType.Assasin: return assassinIcon;
            case PlayerType.Wizard: return wizardIcon;
            default: return emptySlotIcon;
        }
    }
    
    /// <summary>
    /// 슬롯 클릭 이벤트 (CharacterSlotItem에서 호출)
    /// </summary>
    public void HandleSlotClicked(int slotIndex)
    {
        OnSlotClicked?.Invoke(slotIndex);
    }
    
    /// <summary>
    /// 삭제 버튼 클릭 이벤트 (CharacterSlotItem에서 호출)
    /// </summary>
    public void HandleDeleteClicked(int slotIndex)
    {
        OnDeleteClicked?.Invoke(slotIndex);
    }
}

